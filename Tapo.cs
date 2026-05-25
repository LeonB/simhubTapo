using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.IO;
using TapoDevices;

namespace LeonB.Tapo
{
    [PluginDescription("Tapo smart plugs Plugin")]
    [PluginAuthor("LeonB")]
    [PluginName("Tapo")]
    public class Tapoer : IPlugin, IDataPlugin, IWPFSettings
    {

        public DataPluginDemoSettings Settings;

        private static TapoDevices.TapoDeviceFactory tapo;

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
            return;
            //// Define the value of our property (declared in init)
            //pluginManager.SetPropertyValue("CurrentDateTime", this.GetType(), DateTime.Now);

            //if (data.GameRunning)
            //{
            //    if (data.OldData != null && data.NewData != null)
            //    {
            //        if (data.OldData.SpeedKmh < Settings.SpeedWarningLevel && data.OldData.SpeedKmh >= Settings.SpeedWarningLevel)
            //        {
            //            // Trigger an event
            //            pluginManager.TriggerEvent("SpeedWarning", this.GetType());
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
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
            pluginManager.AddAction("TapoToggle", this.GetType(), TapoToggle);
            pluginManager.AddAction("TapoOn", this.GetType(), TapoOn);
            pluginManager.AddAction("TapoOff", this.GetType(), TapoOff);

            foreach (var device in Settings.Devices)
            {
                ExecuteDeviceLifecycleAction("startup", device.OnStartup, device.IP);
            }
        }

        private async void TapoToggle(PluginManager arg1, string arg2)
        {
            await ExecutePlugActionWithLoggingAsync("manual", "Toggle").ConfigureAwait(false);
        }

        private async void TapoOn(PluginManager arg1, string arg2)
        {
            await ExecutePlugActionWithLoggingAsync("manual", "On").ConfigureAwait(false);
        }

        private async void TapoOff(PluginManager arg1, string arg2)
        {
            await ExecutePlugActionWithLoggingAsync("manual", "Off").ConfigureAwait(false);
        }

        private async void ExecuteDeviceLifecycleAction(string lifecycleName, string action, string deviceIp)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            try
            {
                SimHub.Logging.Current.Info("Running Tapo " + lifecycleName + " action: " + action + " for " + deviceIp);
                await EnsureFactoryAndExecuteForDeviceAsync(action, deviceIp).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("Tapo " + lifecycleName + " action failed for " + deviceIp, ex);
            }
        }

        private void ExecuteDeviceLifecycleActionsAndWait(string lifecycleName, TimeSpan timeout)
        {
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
                        await EnsureFactoryAndExecuteForDeviceAsync(device.OnShutdown, device.IP).ConfigureAwait(false);
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

        private async Task EnsureFactoryAndExecuteForDeviceAsync(string action, string deviceIp)
        {
            if (string.IsNullOrWhiteSpace(Settings.Username) ||
                string.IsNullOrWhiteSpace(Settings.Password))
            {
                SimHub.Logging.Current.Warn("Tapo action skipped because username or password is missing.");
                return;
            }

            tapo = new TapoDevices.TapoDeviceFactory(Settings.Username, Settings.Password);
            await ExecutePlugActionForDeviceAsync(action, deviceIp).ConfigureAwait(false);
        }

        private async Task ExecutePlugActionWithLoggingAsync(string actionSource, string action)
        {
            try
            {
                SimHub.Logging.Current.Info("Running Tapo " + actionSource + " action: " + action);
                await ExecutePlugActionAsync(action).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("Tapo " + actionSource + " action failed", ex);
            }
        }

        private async Task ExecutePlugActionAsync(string action)
        {
            if (string.IsNullOrWhiteSpace(Settings.Username) ||
                string.IsNullOrWhiteSpace(Settings.Password))
            {
                SimHub.Logging.Current.Warn("Tapo action skipped because username or password is missing.");
                return;
            }

            var deviceIps = GetConfiguredDeviceIps().ToList();
            if (deviceIps.Count == 0)
            {
                SimHub.Logging.Current.Warn("Tapo action skipped because no device IPs are configured.");
                return;
            }

            tapo = new TapoDevices.TapoDeviceFactory(Settings.Username, Settings.Password);

            foreach (var deviceIp in deviceIps)
            {
                try
                {
                    await ExecutePlugActionForDeviceAsync(action, deviceIp).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    SimHub.Logging.Current.Error("Tapo action failed for device " + deviceIp, ex);
                }
            }
        }

        private async Task ExecutePlugActionForDeviceAsync(string action, string deviceIp)
        {
            var plug = await ConnectPlugAsync(deviceIp).ConfigureAwait(false);

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

        private async Task<TapoPlug> ConnectPlugAsync(string deviceIp)
        {
            var plug = tapo.CreatePlug(deviceIp, TimeSpan.FromSeconds(3));
            Exception klapException = null;

            try
            {
                await plug.ConnectAsync().ConfigureAwait(false);
                return plug;
            }
            catch (Exception ex)
            {
                klapException = ex;

                if (IsForbiddenResponse(ex))
                {
                    throw new InvalidOperationException(
                        "Tapo KLAP handshake was rejected with HTTP 403 for " + deviceIp + ". Enable Third-Party Compatibility/Local Access for this device in the Tapo app, make sure the plug has internet access on newer firmware such as P115 1.4.0, then check that the configured Tapo email, password, and IP address are correct.",
                        ex);
                }

                SimHub.Logging.Current.Warn("Tapo KLAP connection failed, retrying with legacy protocol: " + ex.Message);
            }

            plug = tapo.CreatePlug(deviceIp, TimeSpan.FromSeconds(3));
            try
            {
                await plug.ConnectOldAsync().ConfigureAwait(false);
                return plug;
            }
            catch (Exception legacyException)
            {
                throw new InvalidOperationException(
                    "Tapo connection failed with both KLAP and legacy protocols. KLAP: " +
                    klapException.Message +
                    " Legacy: " +
                    legacyException.Message,
                    legacyException);
            }
        }

        private static bool IsForbiddenResponse(Exception ex)
        {
            while (ex != null)
            {
                if (ex.Message.IndexOf("403", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ex.Message.IndexOf("Forbidden", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                ex = ex.InnerException;
            }

            return false;
        }

        private void NormalizeDeviceSettings()
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
                    IP = string.IsNullOrWhiteSpace(d.IP) ? "" : d.IP.Trim(),
                    OnStartup = d.OnStartup ?? "",
                    OnShutdown = d.OnShutdown ?? ""
                })
                .Where(d => !string.IsNullOrWhiteSpace(d.IP))
                .GroupBy(d => d.IP, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            // Keep legacy fields in sync
            Settings.DeviceIPs = Settings.Devices.Select(d => d.IP).ToList();
            Settings.IP = Settings.DeviceIPs.FirstOrDefault() ?? "";
        }

        private IEnumerable<string> GetConfiguredDeviceIps()
        {
            if (Settings.Devices == null) return Enumerable.Empty<string>();
            return Settings.Devices
                .Select(d => string.IsNullOrWhiteSpace(d.IP) ? "" : d.IP.Trim())
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }
}
