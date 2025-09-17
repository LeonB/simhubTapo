using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;
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

        }

        private async void TapoToggle(PluginManager arg1, string arg2)
        {
            string username = Settings.Username;
            string password = Settings.Password;
            string ip = Settings.IP;

            SimHub.Logging.Current.Info("username: " + username);
            SimHub.Logging.Current.Info("password: " + password);
            SimHub.Logging.Current.Info("ip: " + ip);

            tapo =  new TapoDevices.TapoDeviceFactory(username, password);

            // Connect to device with specified IP address
            var plug = tapo.CreatePlug(ip, TimeSpan.FromSeconds(3));
            await plug.ConnectAsync();

            // Read and display device information
            var info = await plug.GetInfoAsync();

            if (info.DeviceOn) {
                SimHub.Logging.Current.Info("Turning off Tapo plug");
                await plug.TurnOffAsync();
            } else {
                SimHub.Logging.Current.Info("Turning on Tapo plug");
                await plug.TurnOnAsync();
            }
        }

        private async void TapoOn(PluginManager arg1, string arg2)
        {
            string username = Settings.Username;
            string password = Settings.Password;
            string ip = Settings.IP;

            SimHub.Logging.Current.Info("username: " + username);
            SimHub.Logging.Current.Info("password: " + password);
            SimHub.Logging.Current.Info("ip: " + ip);

            tapo =  new TapoDevices.TapoDeviceFactory(username, password);

            // Connect to device with specified IP address
            var plug = tapo.CreatePlug(ip, TimeSpan.FromSeconds(3));
            await plug.ConnectAsync();

            await plug.TurnOnAsync();
        }
        
        private async void TapoOff(PluginManager arg1, string arg2)
        {
            string username = Settings.Username;
            string password = Settings.Password;
            string ip = Settings.IP;

            tapo =  new TapoDevices.TapoDeviceFactory(username, password);

            // Connect to device with specified IP address
            var plug = tapo.CreatePlug(ip, TimeSpan.FromSeconds(3));
            await plug.ConnectAsync();
            
            await plug.TurnOffAsync();
        }
    }
}