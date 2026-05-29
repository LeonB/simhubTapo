using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TapoDevices;

namespace LeonB.Tapo
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public Tapoer Plugin { get; }

        private string _editingName = null;
        private string _pendingMac = null;
        private string _pendingMacForIp = null;
        private DispatcherTimer _credentialCheckTimer;
        private CancellationTokenSource _credentialCheckCts;

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(Tapoer plugin) : this()
        {
            Plugin = plugin;
            tbUser.Text = Plugin.Settings.Username;
            tbPassword.Password = Plugin.Settings.Password;
            Plugin.NormalizeDeviceSettings();
            RefreshDeviceList();
        }

        private void tbUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            Plugin.Settings.Username = tbUser.Text;
            ScheduleCredentialCheck();
        }

        private void tbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Plugin.Settings.Password = tbPassword.Password;
            ScheduleCredentialCheck();
        }

        private void ScheduleCredentialCheck()
        {
            if (_credentialCheckTimer == null)
            {
                _credentialCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                _credentialCheckTimer.Tick += (s, e) => { _credentialCheckTimer.Stop(); CheckCredentialsAsync(); };
            }
            _credentialCheckTimer.Stop();
            _credentialCheckTimer.Start();
            tbCredentialStatus.Text = "";
        }

        private async void CheckCredentialsAsync()
        {
            var device = Plugin?.Settings?.Devices?.FirstOrDefault();
            if (device == null)
            {
                tbCredentialStatus.Text = "";
                return;
            }

            _credentialCheckCts?.Cancel();
            _credentialCheckCts = new CancellationTokenSource();
            var token = _credentialCheckCts.Token;

            tbCredentialStatus.Text = "Checking...";
            tbCredentialStatus.Foreground = new SolidColorBrush(Colors.Gray);

            var factory = new TapoDeviceFactory(Plugin.Settings.Username, Plugin.Settings.Password);
            var plug = factory.CreatePlug(device.IP, TimeSpan.FromSeconds(3));

            bool success = false;
            bool authFailed = false;
            bool connectionFailed = false;

            try
            {
                await plug.ConnectAsync();
                success = true;
            }
            catch (Exception klapEx)
            {
                plug.Dispose();
                plug = null;
                if (IsForbiddenResponse(klapEx))
                {
                    authFailed = true;
                }
                else
                {
                    plug = factory.CreatePlug(device.IP, TimeSpan.FromSeconds(3));
                    try
                    {
                        await plug.ConnectOldAsync();
                        success = true;
                    }
                    catch (Exception legacyEx)
                    {
                        plug.Dispose();
                        plug = null;
                        if (IsForbiddenResponse(legacyEx))
                            authFailed = true;
                        else
                            connectionFailed = true;
                    }
                }
            }

            plug?.Dispose();

            if (token.IsCancellationRequested) return;

            if (success)
            {
                tbCredentialStatus.Text = "✓ Credentials OK";
                tbCredentialStatus.Foreground = new SolidColorBrush(Colors.Green);
            }
            else if (authFailed)
            {
                tbCredentialStatus.Text = "✗ Authentication failed";
                tbCredentialStatus.Foreground = new SolidColorBrush(Colors.Red);
            }
            else if (connectionFailed)
            {
                tbCredentialStatus.Text = "✗ Could not connect — check credentials";
                tbCredentialStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                tbCredentialStatus.Text = "";
            }
        }

        private void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            var name = tbName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                tbNameError.Text = "Name is required.";
                tbNameError.Visibility = Visibility.Visible;
                return;
            }

            var nameInUse = Plugin.Settings.Devices.Any(d =>
                string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(d.Name, _editingName, StringComparison.OrdinalIgnoreCase));
            if (nameInUse)
            {
                tbNameError.Text = "Name is already in use.";
                tbNameError.Visibility = Visibility.Visible;
                return;
            }

            tbNameError.Visibility = Visibility.Collapsed;

            var ip = NormalizeIp(tbIP.Text);
            if (string.IsNullOrWhiteSpace(ip))
            {
                tbIPError.Text = "IP is required.";
                tbIPError.Visibility = Visibility.Visible;
                return;
            }

            if (!IsValidIpv4(ip))
            {
                tbIPError.Text = "Enter a valid IPv4 address (e.g. 192.168.1.100).";
                tbIPError.Visibility = Visibility.Visible;
                return;
            }

            var ipInUse = Plugin.Settings.Devices.Any(d =>
                string.Equals(d.IP, ip, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(d.Name, _editingName, StringComparison.OrdinalIgnoreCase));
            if (ipInUse)
            {
                tbIPError.Text = "IP is already in use.";
                tbIPError.Visibility = Visibility.Visible;
                return;
            }

            tbIPError.Visibility = Visibility.Collapsed;

            Plugin.NormalizeDeviceSettings();

            var knownMac = (_pendingMacForIp != null && string.Equals(_pendingMacForIp, ip, StringComparison.OrdinalIgnoreCase))
                ? (_pendingMac ?? "")
                : "";

            if (_editingName != null)
            {
                var existing = Plugin.Settings.Devices.FirstOrDefault(d => string.Equals(d.Name, _editingName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    Plugin.UnregisterDeviceActions(existing.Name);
                    existing.Name = name;
                    existing.IP = ip;
                    if (!string.IsNullOrEmpty(knownMac))
                        existing.MAC = knownMac;
                    existing.OnStartup = GetSelectedComboBoxText(cbAddOnStartup);
                    existing.OnShutdown = GetSelectedComboBoxText(cbAddOnShutdown);
                    Plugin.RegisterDeviceActions(existing.Name, existing.IP);
                    if (string.IsNullOrEmpty(existing.MAC))
                        FetchAndUpdateMacAsync(existing);
                    _ = CheckDeviceReachabilityAsync(existing);
                }
            }
            else if (!Plugin.Settings.Devices.Any(d => string.Equals(d.IP, ip, StringComparison.OrdinalIgnoreCase)))
            {
                var device = new TapoDeviceConfig
                {
                    Name = name,
                    IP = ip,
                    MAC = knownMac,
                    OnStartup = GetSelectedComboBoxText(cbAddOnStartup),
                    OnShutdown = GetSelectedComboBoxText(cbAddOnShutdown)
                };
                Plugin.Settings.Devices.Add(device);
                Plugin.RegisterDeviceActions(device.Name, device.IP);
                if (string.IsNullOrEmpty(device.MAC))
                    FetchAndUpdateMacAsync(device);
                _ = CheckDeviceReachabilityAsync(device);
            }

            Plugin.SyncLegacyFields();
            ResetAddForm();
            RefreshDeviceList();
        }

        private async void Discover_Click(object sender, RoutedEventArgs e)
        {
            btnDiscover.IsEnabled = false;
            lbDiscovered.Visibility = Visibility.Collapsed;

            var timeout = TimeSpan.FromSeconds(5);
            var discoverTask = TapoDiscovery.DiscoverAsync(timeout);
            var start = DateTime.UtcNow;

            while (!discoverTask.IsCompleted)
            {
                var remaining = (int)Math.Ceiling(Math.Max(0, (timeout - (DateTime.UtcNow - start)).TotalSeconds));
                tbDiscoverStatus.Text = $"Scanning... ({remaining}s)";
                await Task.Delay(200);
            }

            List<DiscoveredDevice> rawDevices;
            try
            {
                rawDevices = await discoverTask;
            }
            catch (Exception ex)
            {
                tbDiscoverStatus.Text = "Scan error: " + ex.Message;
                btnDiscover.IsEnabled = true;
                return;
            }

            if (rawDevices.Count == 0)
            {
                tbDiscoverStatus.Text = "No devices found.";
                btnDiscover.IsEnabled = true;
                return;
            }

            tbDiscoverStatus.Text = $"Found {rawDevices.Count} device(s), fetching info...";

            var factory = new TapoDeviceFactory(Plugin.Settings.Username, Plugin.Settings.Password);
            var fetchResults = await Task.WhenAll(rawDevices.Select(raw => FetchPlugInfoAsync(factory, raw)));
            var allItems = fetchResults.Where(r => r.Plug != null).Select(r => r.Plug).ToList();

            if (allItems.Count == 0)
            {
                tbDiscoverStatus.Text = "No smart plugs found.";
            }
            else
            {
                var confirmed = allItems.Count(p => !p.AuthFailed);
                var needCreds = allItems.Count(p => p.AuthFailed);

                if (needCreds == 0)
                    tbDiscoverStatus.Text = $"Found {confirmed} plug(s). Click one to fill the form:";
                else if (confirmed == 0)
                    tbDiscoverStatus.Text = $"Found {needCreds} plug(s) — fix credentials to see names. Click one to fill the form:";
                else
                    tbDiscoverStatus.Text = $"Found {confirmed} plug(s), {needCreds} more need credentials. Click one to fill the form:";

                lbDiscovered.ItemsSource = allItems;
                lbDiscovered.Visibility = Visibility.Visible;
            }

            btnDiscover.IsEnabled = true;
        }

        private static async Task<FetchResult> FetchPlugInfoAsync(TapoDeviceFactory factory, DiscoveredDevice raw)
        {
            TapoPlug plug = null;

            var klapPlug = factory.CreatePlug(raw.IP, TimeSpan.FromSeconds(3));
            try
            {
                await klapPlug.ConnectAsync().ConfigureAwait(false);
                plug = klapPlug;
            }
            catch (Exception klapEx)
            {
                klapPlug.Dispose();
                if (IsForbiddenResponse(klapEx))
                    return AuthFailedResult(raw);

                var legacyPlug = factory.CreatePlug(raw.IP, TimeSpan.FromSeconds(3));
                try
                {
                    await legacyPlug.ConnectOldAsync().ConfigureAwait(false);
                    plug = legacyPlug;
                }
                catch (Exception legacyEx)
                {
                    legacyPlug.Dispose();
                    if (IsForbiddenResponse(legacyEx))
                        return AuthFailedResult(raw);
                    return LooksLikePlug(raw.Model) ? AuthFailedResult(raw) : new FetchResult();
                }
            }

            using (plug)
            {
                try
                {
                    var info = await plug.GetInfoAsync().ConfigureAwait(false);
                    if (info.Type == null || info.Type.IndexOf("PLUG", StringComparison.OrdinalIgnoreCase) < 0)
                        return new FetchResult();

                    var name = string.IsNullOrWhiteSpace(info.Nickname) ? info.Model : info.Nickname;
                    return new FetchResult { Plug = new DiscoveredPlugInfo { IP = raw.IP, Name = name, Model = info.Model, MAC = info.MacAddress ?? "" } };
                }
                catch
                {
                    return new FetchResult();
                }
            }
        }

        private static FetchResult AuthFailedResult(DiscoveredDevice raw)
        {
            return new FetchResult
            {
                AuthFailed = true,
                Plug = new DiscoveredPlugInfo { IP = raw.IP, Name = raw.Model, Model = raw.Model, MAC = raw.MAC ?? "", AuthFailed = true }
            };
        }

        private static bool LooksLikePlug(string model)
        {
            return !string.IsNullOrEmpty(model) && model.Length >= 2
                && (model[0] == 'P' || model[0] == 'p') && char.IsDigit(model[1]);
        }

        private static bool IsForbiddenResponse(Exception ex)
        {
            while (ex != null)
            {
                if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                    return true;
                ex = ex.InnerException;
            }
            return false;
        }

        private class FetchResult
        {
            public DiscoveredPlugInfo Plug { get; set; }
            public bool AuthFailed { get; set; }
        }

        private void lbDiscovered_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(lbDiscovered.SelectedItem is DiscoveredPlugInfo device))
                return;

            tbIP.Text = device.IP;
            if (string.IsNullOrWhiteSpace(tbName.Text) && _editingName == null)
                tbName.Text = device.Name;

            _pendingMac = device.MAC;
            _pendingMacForIp = device.IP;
            UpdateMacDisplay(device.MAC);
        }

        private class DiscoveredPlugInfo
        {
            public string IP { get; set; }
            public string Name { get; set; }
            public string Model { get; set; }
            public string MAC { get; set; }
            public bool AuthFailed { get; set; }
        }

        private void DeleteDevice_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin == null) return;

            var device = (sender as Button)?.Tag as TapoDeviceConfig;
            if (device == null) return;

            var result = MessageBox.Show($"Remove device {device.Name}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            Plugin.UnregisterDeviceActions(device.Name);
            Plugin.Settings.Devices.RemoveAll(d => string.Equals(d.IP, device.IP, StringComparison.OrdinalIgnoreCase));
            Plugin.SyncLegacyFields();
            ResetAddForm();
            RefreshDeviceList();
        }

        private void lbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(lbDevices.SelectedItem is TapoDeviceConfig device))
            {
                ResetAddForm();
                return;
            }

            _editingName = device.Name;
            tbName.Text = device.Name;
            tbIP.Text = device.IP;
            SelectComboBoxItem(cbAddOnStartup, device.OnStartup);
            SelectComboBoxItem(cbAddOnShutdown, device.OnShutdown);
            UpdateMacDisplay(device.MAC);
            btnAddDevice.Content = "Update Device";
            btnCancelEdit.Visibility = Visibility.Visible;
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            lbDevices.SelectedItem = null;
            ResetAddForm();
        }

        private void ResetAddForm()
        {
            _editingName = null;
            _pendingMac = null;
            _pendingMacForIp = null;
            tbName.Text = "";
            tbNameError.Visibility = Visibility.Collapsed;
            tbIP.Text = "";
            tbIPError.Visibility = Visibility.Collapsed;
            ellFormReachability.Fill = Brushes.DarkGray;
            UpdateMacDisplay("");
            SelectComboBoxItem(cbAddOnStartup, "");
            SelectComboBoxItem(cbAddOnShutdown, "");
            btnAddDevice.Content = "Add Device";
            btnCancelEdit.Visibility = Visibility.Collapsed;
        }

        private static void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (string.Equals(item.Content?.ToString() ?? "", value ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private static string GetSelectedComboBoxText(ComboBox comboBox)
        {
            var item = comboBox.SelectedItem as ComboBoxItem;
            return item == null || item.Content == null ? "" : item.Content.ToString();
        }

        private async void FetchAndUpdateMacAsync(TapoDeviceConfig device)
        {
            var factory = new TapoDeviceFactory(Plugin.Settings.Username, Plugin.Settings.Password);
            TapoPlug plug = null;

            var klapPlug = factory.CreatePlug(device.IP, TimeSpan.FromSeconds(5));
            try
            {
                await klapPlug.ConnectAsync();
                plug = klapPlug;
            }
            catch
            {
                klapPlug.Dispose();
                var legacyPlug = factory.CreatePlug(device.IP, TimeSpan.FromSeconds(5));
                try
                {
                    await legacyPlug.ConnectOldAsync();
                    plug = legacyPlug;
                }
                catch
                {
                    legacyPlug.Dispose();
                    return;
                }
            }

            using (plug)
            {
                try
                {
                    var info = await plug.GetInfoAsync();
                    if (!string.IsNullOrWhiteSpace(info.MacAddress))
                    {
                        device.MAC = info.MacAddress;
                        RefreshDeviceList();
                        if (_editingName != null && string.Equals(_editingName, device.Name, StringComparison.OrdinalIgnoreCase))
                            UpdateMacDisplay(device.MAC);
                    }
                }
                catch { }
            }
        }

        private void UpdateMacDisplay(string mac)
        {
            if (string.IsNullOrEmpty(mac))
            {
                pnlMac.Visibility = Visibility.Collapsed;
                tbMacDisplay.Text = "";
            }
            else
            {
                tbMacDisplay.Text = mac;
                pnlMac.Visibility = Visibility.Visible;
            }
        }

        private void RefreshDeviceList()
        {
            lbDevices.ItemsSource = null;
            lbDevices.ItemsSource = Plugin.Settings.Devices;
        }

        private static string NormalizeIp(string ip)
        {
            return string.IsNullOrWhiteSpace(ip) ? "" : ip.Trim();
        }

        private static bool IsValidIpv4(string ip)
        {
            var parts = ip.Split('.');
            if (parts.Length != 4) return false;
            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var n) || n < 0 || n > 255) return false;
            }
            return true;
        }

        private static async Task CheckDeviceReachabilityAsync(TapoDeviceConfig device)
        {
            device.Reachability = ReachabilityStatus.Unknown;
            device.Reachability = await Tapoer.IsDeviceReachableAsync(device.IP)
                ? ReachabilityStatus.Reachable
                : ReachabilityStatus.Unreachable;
        }

        private async void TestDevice_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.Tag is TapoDeviceConfig device)) return;
            await CheckDeviceReachabilityAsync(device);
        }

        private async void TestFormDevice_Click(object sender, RoutedEventArgs e)
        {
            var ip = tbIP.Text.Trim();
            if (string.IsNullOrEmpty(ip)) return;
            ellFormReachability.Fill = Brushes.DarkGray;
            var reachable = await Tapoer.IsDeviceReachableAsync(ip);
            ellFormReachability.Fill = reachable ? Brushes.LimeGreen : Brushes.Red;
        }
    }

    internal class ReachabilityToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is ReachabilityStatus status)) return Brushes.DarkGray;
            switch (status)
            {
                case ReachabilityStatus.Reachable:   return Brushes.LimeGreen;
                case ReachabilityStatus.Unreachable: return Brushes.Red;
                default:                             return Brushes.DarkGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
