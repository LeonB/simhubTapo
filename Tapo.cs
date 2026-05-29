using GameReaderCommon;
using Microsoft.Win32;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using TapoDevices;

namespace LeonB.Tapo
{
    [PluginDescription("Tapo smart plugs Plugin")]
    [PluginAuthor("LeonB")]
    [PluginName("Tapo")]
    public class Tapoer : IPlugin, IDataPlugin, IWPFSettings
    {

        public DataPluginDemoSettings Settings;

        private TapoDevices.TapoDeviceFactory tapo;

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            ExecuteDeviceLifecycleActionsAndWait("shutdown", TimeSpan.FromSeconds(10));

            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            Settings = this.ReadCommonSettings<DataPluginDemoSettings>("GeneralSettings", () => new DataPluginDemoSettings());
            NormalizeDeviceSettings();

            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            foreach (var device in Settings.Devices)
            {
                RegisterDeviceActions(device.Name, device.IP);
                _ = ExecuteDeviceActionAsync("startup", device.OnStartup, device.IP);
            }

            _ = CheckAllDevicesReachabilityAsync();
        }

        private void OnPowerModeChanged(object _, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                SimHub.Logging.Current.Info("Tapo: system suspending, running shutdown actions.");
                ExecuteDeviceLifecycleActionsAndWait("sleep", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
            }
            else if (e.Mode == PowerModes.Resume)
            {
                _ = ExecuteOnResumeAsync();
            }
        }

        internal void RegisterDeviceActions(string name, string ip)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                SimHub.Logging.Current.Warn("Tapo device " + ip + " has no name; skipping action registration.");
                return;
            }

            PluginManager.AddAction(name + " On", GetType(),
                async (mgr, arg) => await ExecuteDeviceActionAsync("manual", "On", ip).ConfigureAwait(false));
            PluginManager.AddAction(name + " Off", GetType(),
                async (mgr, arg) => await ExecuteDeviceActionAsync("manual", "Off", ip).ConfigureAwait(false));
            PluginManager.AddAction(name + " Toggle", GetType(),
                async (mgr, arg) => await ExecuteDeviceActionAsync("manual", "Toggle", ip).ConfigureAwait(false));
        }

        internal void UnregisterDeviceActions(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            PluginManager.ClearActions(GetType(), name + " On");
            PluginManager.ClearActions(GetType(), name + " Off");
            PluginManager.ClearActions(GetType(), name + " Toggle");
        }

        private async Task ExecuteDeviceActionAsync(string context, string action, string deviceIp)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            try
            {
                SimHub.Logging.Current.Info("Tapo " + context + ": " + action + " for " + deviceIp);
                await EnsureFactoryAndExecuteForDeviceAsync(action, deviceIp).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("Tapo " + context + " failed for " + deviceIp, ex);
            }
        }

        private void ExecuteDeviceLifecycleActionsAndWait(string lifecycleName, TimeSpan timeout, TimeSpan connectionTimeout = default)
        {
            if (connectionTimeout == default)
                connectionTimeout = TimeSpan.FromSeconds(3);

            var devicesWithAction = Settings.Devices
                .Where(d => !string.IsNullOrWhiteSpace(d.OnShutdown))
                .ToList();

            if (!devicesWithAction.Any())
            {
                return;
            }

            var task = Task.Run(async () =>
            {
                foreach (var device in devicesWithAction)
                {
                    try
                    {
                        SimHub.Logging.Current.Info("Running Tapo " + lifecycleName + " action: " + device.OnShutdown + " for " + device.IP);
                        await EnsureFactoryAndExecuteForDeviceAsync(device.OnShutdown, device.IP, connectionTimeout).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        SimHub.Logging.Current.Error("Tapo " + lifecycleName + " action failed for " + device.IP, ex);
                    }
                }
            });

            try
            {
                if (!task.Wait(timeout))
                {
                    SimHub.Logging.Current.Warn("Tapo " + lifecycleName + " action did not finish within " + timeout.TotalSeconds + " seconds.");
                }
            }
            catch (AggregateException ex)
            {
                SimHub.Logging.Current.Error("Tapo " + lifecycleName + " actions failed", ex.GetBaseException());
            }
        }

        private async Task ExecuteOnResumeAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            foreach (var device in Settings.Devices)
                await ExecuteDeviceActionAsync("wake", device.OnStartup, device.IP).ConfigureAwait(false);
        }

        private async Task EnsureFactoryAndExecuteForDeviceAsync(string action, string deviceIp, TimeSpan connectionTimeout = default)
        {
            if (connectionTimeout == default)
                connectionTimeout = TimeSpan.FromSeconds(3);

            if (string.IsNullOrWhiteSpace(Settings.Username) ||
                string.IsNullOrWhiteSpace(Settings.Password))
            {
                SimHub.Logging.Current.Warn("Tapo action skipped because username or password is missing.");
                return;
            }

            tapo = new TapoDevices.TapoDeviceFactory(Settings.Username, Settings.Password);
            await ExecutePlugActionForDeviceAsync(action, deviceIp, connectionTimeout).ConfigureAwait(false);
        }

        private async Task ExecutePlugActionForDeviceAsync(string action, string deviceIp, TimeSpan connectionTimeout = default)
        {
            if (connectionTimeout == default)
                connectionTimeout = TimeSpan.FromSeconds(3);

            using (var plug = await ConnectPlugAsync(deviceIp, connectionTimeout).ConfigureAwait(false))
            {
                if (string.Equals(action, "On", StringComparison.OrdinalIgnoreCase))
                {
                    SimHub.Logging.Current.Info("Turning on Tapo plug at " + deviceIp);
                    await plug.TurnOnAsync().ConfigureAwait(false);
                    return;
                }

                if (string.Equals(action, "Off", StringComparison.OrdinalIgnoreCase))
                {
                    SimHub.Logging.Current.Info("Turning off Tapo plug at " + deviceIp);
                    await plug.TurnOffAsync().ConfigureAwait(false);
                    return;
                }

                if (string.Equals(action, "Toggle", StringComparison.OrdinalIgnoreCase))
                {
                    var info = await plug.GetInfoAsync().ConfigureAwait(false);

                    if (info.DeviceOn)
                    {
                        SimHub.Logging.Current.Info("Turning off Tapo plug at " + deviceIp);
                        await plug.TurnOffAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        SimHub.Logging.Current.Info("Turning on Tapo plug at " + deviceIp);
                        await plug.TurnOnAsync().ConfigureAwait(false);
                    }

                    return;
                }

                SimHub.Logging.Current.Warn("Unknown Tapo action: " + action);
            }
        }

        private async Task<TapoPlug> ConnectPlugAsync(string deviceIp, TimeSpan connectionTimeout = default)
        {
            if (connectionTimeout == default)
                connectionTimeout = TimeSpan.FromSeconds(3);

            var plug = tapo.CreatePlug(deviceIp, connectionTimeout);
            Exception klapException = null;

            try
            {
                await plug.ConnectAsync().ConfigureAwait(false);
                return plug;
            }
            catch (Exception ex)
            {
                klapException = ex;
                plug.Dispose();

                if (IsForbiddenResponse(ex))
                {
                    throw new InvalidOperationException(
                        "Tapo KLAP handshake was rejected with HTTP 403 for " + deviceIp + ". Enable Third-Party Compatibility/Local Access for this device in the Tapo app, make sure the plug has internet access on newer firmware such as P115 1.4.0, then check that the configured Tapo email, password, and IP address are correct.",
                        ex);
                }

                SimHub.Logging.Current.Warn("Tapo KLAP connection failed, retrying with legacy protocol: " + ex.Message);
            }

            plug = tapo.CreatePlug(deviceIp, connectionTimeout);
            try
            {
                await plug.ConnectOldAsync().ConfigureAwait(false);
                return plug;
            }
            catch (Exception legacyException)
            {
                plug.Dispose();
                throw new InvalidOperationException(
                    "Tapo connection failed with both KLAP and legacy protocols. KLAP: " +
                    klapException.Message +
                    " Legacy: " +
                    legacyException.Message,
                    legacyException);
            }
        }

        internal static async Task<bool> IsDeviceReachableAsync(string ip)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    var connectTask = client.ConnectAsync(ip, 80);
                    if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                        return false;
                    return client.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task CheckAllDevicesReachabilityAsync()
        {
            if (Settings?.Devices == null) return;
            var tasks = Settings.Devices
                .Where(d => !string.IsNullOrEmpty(d.IP))
                .Select(async d =>
                {
                    d.Reachability = await IsDeviceReachableAsync(d.IP)
                        ? ReachabilityStatus.Reachable
                        : ReachabilityStatus.Unreachable;
                })
                .ToList();
            await Task.WhenAll(tasks);
        }

        internal static bool IsForbiddenResponse(Exception ex)
        {
            while (ex != null)
            {
                if (ex.Message.Contains("403") ||
                    ex.Message.Contains("Forbidden"))
                {
                    return true;
                }

                ex = ex.InnerException;
            }

            return false;
        }

        internal void NormalizeDeviceSettings()
        {
            if (Settings.Devices == null)
            {
                Settings.Devices = new List<TapoDeviceConfig>();
            }

            // Migrate legacy DeviceIPs + global OnStartup/OnShutdown to per-device Devices list
            if (!Settings.Devices.Any())
            {
                var legacyIps = new List<string>();
                if (Settings.DeviceIPs != null)
                {
                    legacyIps.AddRange(Settings.DeviceIPs);
                }
                if (!string.IsNullOrWhiteSpace(Settings.IP))
                {
                    legacyIps.Add(Settings.IP);
                }

                var distinctLegacyIps = legacyIps
                    .Select(ip => string.IsNullOrWhiteSpace(ip) ? "" : ip.Trim())
                    .Where(ip => !string.IsNullOrWhiteSpace(ip))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var ip in distinctLegacyIps)
                {
                    Settings.Devices.Add(new TapoDeviceConfig
                    {
                        IP = ip,
                        OnStartup = Settings.OnStartup ?? "",
                        OnShutdown = Settings.OnShutdown ?? ""
                    });
                }
            }

            // Normalize and deduplicate
            Settings.Devices = Settings.Devices
                .Select(d => new TapoDeviceConfig
                {
                    Name = d.Name ?? "",
                    IP = string.IsNullOrWhiteSpace(d.IP) ? "" : d.IP.Trim(),
                    MAC = d.MAC ?? "",
                    OnStartup = d.OnStartup ?? "",
                    OnShutdown = d.OnShutdown ?? ""
                })
                .Where(d => !string.IsNullOrWhiteSpace(d.IP))
                .GroupBy(d => d.IP, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            SyncLegacyFields();
        }

        internal void SyncLegacyFields()
        {
            Settings.DeviceIPs = Settings.Devices.Select(d => d.IP).ToList();
            Settings.IP = Settings.DeviceIPs.FirstOrDefault() ?? "";
        }

    }
}
