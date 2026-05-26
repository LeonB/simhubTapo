using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LeonB.Tapo
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public Tapoer Plugin { get; }

        private string _editingName = null;

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(Tapoer plugin) : this()
        {
            Plugin = plugin;
            tbUser.Text = Plugin.Settings.Username;
            tbPassword.Password = Plugin.Settings.Password;
            NormalizeDeviceSettings();
            RefreshDeviceList();
        }

        private void tbUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            Plugin.Settings.Username = tbUser.Text;
        }

        private void tbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Plugin.Settings.Password = tbPassword.Password;
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

            NormalizeDeviceSettings();

            if (_editingName != null)
            {
                var existing = Plugin.Settings.Devices.FirstOrDefault(d => string.Equals(d.Name, _editingName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    Plugin.UnregisterDeviceActions(existing.Name);
                    existing.Name = name;
                    existing.IP = ip;
                    existing.OnStartup = GetSelectedComboBoxText(cbAddOnStartup);
                    existing.OnShutdown = GetSelectedComboBoxText(cbAddOnShutdown);
                    Plugin.RegisterDeviceActions(existing.Name, existing.IP);
                }
            }
            else if (!Plugin.Settings.Devices.Any(d => string.Equals(d.IP, ip, StringComparison.OrdinalIgnoreCase)))
            {
                var device = new TapoDeviceConfig
                {
                    Name = name,
                    IP = ip,
                    OnStartup = GetSelectedComboBoxText(cbAddOnStartup),
                    OnShutdown = GetSelectedComboBoxText(cbAddOnShutdown)
                };
                Plugin.Settings.Devices.Add(device);
                Plugin.RegisterDeviceActions(device.Name, device.IP);
            }

            SyncLegacyFields();
            ResetAddForm();
            RefreshDeviceList();
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
            SyncLegacyFields();
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
            tbName.Text = "";
            tbNameError.Visibility = Visibility.Collapsed;
            tbIP.Text = "";
            tbIPError.Visibility = Visibility.Collapsed;
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

        private void NormalizeDeviceSettings()
        {
            if (Plugin.Settings.Devices == null)
            {
                Plugin.Settings.Devices = new List<TapoDeviceConfig>();
            }

            // Migration from legacy DeviceIPs if Devices is empty (safety net — plugin init does this too)
            if (!Plugin.Settings.Devices.Any())
            {
                var legacyIps = new List<string>();
                if (Plugin.Settings.DeviceIPs != null)
                {
                    legacyIps.AddRange(Plugin.Settings.DeviceIPs.Select(NormalizeIp).Where(ip => !string.IsNullOrWhiteSpace(ip)));
                }
                var legacyIp = NormalizeIp(Plugin.Settings.IP);
                if (!string.IsNullOrWhiteSpace(legacyIp) && !legacyIps.Any(ip => string.Equals(ip, legacyIp, StringComparison.OrdinalIgnoreCase)))
                {
                    legacyIps.Add(legacyIp);
                }
                foreach (var ip in legacyIps.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    Plugin.Settings.Devices.Add(new TapoDeviceConfig
                    {
                        IP = ip,
                        OnStartup = Plugin.Settings.OnStartup ?? "",
                        OnShutdown = Plugin.Settings.OnShutdown ?? ""
                    });
                }
            }

            Plugin.Settings.Devices = Plugin.Settings.Devices
                .Select(d => new TapoDeviceConfig { Name = d.Name ?? "", IP = NormalizeIp(d.IP), OnStartup = d.OnStartup ?? "", OnShutdown = d.OnShutdown ?? "" })
                .Where(d => !string.IsNullOrWhiteSpace(d.IP))
                .GroupBy(d => d.IP, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            SyncLegacyFields();
        }

        private void RefreshDeviceList()
        {
            lbDevices.ItemsSource = null;
            lbDevices.ItemsSource = Plugin.Settings.Devices;
        }

        private void SyncLegacyFields()
        {
            Plugin.Settings.DeviceIPs = Plugin.Settings.Devices.Select(d => d.IP).ToList();
            Plugin.Settings.IP = Plugin.Settings.DeviceIPs.FirstOrDefault() ?? "";
        }

        private static string NormalizeIp(string ip)
        {
            return string.IsNullOrWhiteSpace(ip) ? "" : ip.Trim();
        }
    }
}
