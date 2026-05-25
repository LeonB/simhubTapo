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
            ExecuteLifecycleActionAndWait("shutdown", Settings.OnShutdown, TimeSpan.FromSeconds(10));

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
            pluginManager.AddAction("TapoToggle", this.GetType(), TapoToggle);
            pluginManager.AddAction("TapoOn", this.GetType(), TapoOn);
            pluginManager.AddAction("TapoOff", this.GetType(), TapoOff);

            ExecuteLifecycleAction("startup", Settings.OnStartup);
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

        private async void ExecuteLifecycleAction(string lifecycleName, string action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            await ExecutePlugActionWithLoggingAsync(lifecycleName, action).ConfigureAwait(false);
        }

        private void ExecuteLifecycleActionAndWait(string lifecycleName, string action, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            var task = Task.Run(() => ExecutePlugActionWithLoggingAsync(lifecycleName, action));
            try
            {
                if (!task.Wait(timeout))
                {
                    SimHub.Logging.Current.Warn("Tapo " + lifecycleName + " action did not finish within " + timeout.TotalSeconds + " seconds.");
                }
            }
            catch (AggregateException ex)
            {
                SimHub.Logging.Current.Error("Tapo " + lifecycleName + " action failed", ex.GetBaseException());
            }
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
                string.IsNullOrWhiteSpace(Settings.Password) ||
                string.IsNullOrWhiteSpace(Settings.IP))
            {
                SimHub.Logging.Current.Warn("Tapo action skipped because username, password, or IP is missing.");
                return;
            }

            tapo = new TapoDevices.TapoDeviceFactory(Settings.Username, Settings.Password);

            var plug = await ConnectPlugAsync().ConfigureAwait(false);

            if (string.Equals(action, "On", StringComparison.OrdinalIgnoreCase))
            {
                SimHub.Logging.Current.Info("Turning on Tapo plug");
                await plug.TurnOnAsync().ConfigureAwait(false);
                return;
            }

            if (string.Equals(action, "Off", StringComparison.OrdinalIgnoreCase))
            {
                SimHub.Logging.Current.Info("Turning off Tapo plug");
                await plug.TurnOffAsync().ConfigureAwait(false);
                return;
            }

            if (string.Equals(action, "Toggle", StringComparison.OrdinalIgnoreCase))
            {
                var info = await plug.GetInfoAsync().ConfigureAwait(false);

                if (info.DeviceOn)
                {
                    SimHub.Logging.Current.Info("Turning off Tapo plug");
                    await plug.TurnOffAsync().ConfigureAwait(false);
                }
                else
                {
                    SimHub.Logging.Current.Info("Turning on Tapo plug");
                    await plug.TurnOnAsync().ConfigureAwait(false);
                }

                return;
            }

            SimHub.Logging.Current.Warn("Unknown Tapo action: " + action);
        }

        private async Task<TapoPlug> ConnectPlugAsync()
        {
            var plug = tapo.CreatePlug(Settings.IP, TimeSpan.FromSeconds(3));
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
                        "Tapo KLAP handshake was rejected with HTTP 403. Enable Third-Party Compatibility/Local Access for this device in the Tapo app, make sure the plug has internet access on newer firmware such as P115 1.4.0, then check that the configured Tapo email, password, and IP address are correct.",
                        ex);
                }

                SimHub.Logging.Current.Warn("Tapo KLAP connection failed, retrying with legacy protocol: " + ex.Message);
            }

            plug = tapo.CreatePlug(Settings.IP, TimeSpan.FromSeconds(3));
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
    }
}
