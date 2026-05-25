using System.Collections.Generic;

namespace LeonB.Tapo
{
    public class TapoDeviceConfig
    {
        public string IP { get; set; } = "";
        public string OnStartup { get; set; } = "";
        public string OnShutdown { get; set; } = "";

        public override string ToString()
        {
            var startup = string.IsNullOrEmpty(OnStartup) ? "-" : OnStartup;
            var shutdown = string.IsNullOrEmpty(OnShutdown) ? "-" : OnShutdown;
            return IP + "  (Startup: " + startup + ", Shutdown: " + shutdown + ")";
        }
    }

    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DataPluginDemoSettings
    {
        public string Username = "";
        public string Password = "";
        // Legacy fields kept for migration and backward compatibility
        public string IP = "";
        public List<string> DeviceIPs = new List<string>();
        public string OnStartup = "";
        public string OnShutdown = "";
        // Per-device configuration
        public List<TapoDeviceConfig> Devices = new List<TapoDeviceConfig>();
    }
}
