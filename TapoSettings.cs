using System.Collections.Generic;
using System.ComponentModel;

namespace LeonB.Tapo
{
    public enum ReachabilityStatus { Unknown, Reachable, Unreachable }

    public class TapoDeviceConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ReachabilityStatus _reachability;
        public ReachabilityStatus Reachability
        {
            get { return _reachability; }
            set
            {
                if (_reachability == value) return;
                _reachability = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Reachability"));
            }
        }

        public string Name { get; set; } = "";
        public string IP { get; set; } = "";
        public string MAC { get; set; } = "";
        public string OnStartup { get; set; } = "";
        public string OnShutdown { get; set; } = "";

        public string Details
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(OnStartup)) parts.Add("Startup: " + OnStartup);
                if (!string.IsNullOrEmpty(OnShutdown)) parts.Add("Shutdown: " + OnShutdown);
                if (!string.IsNullOrEmpty(MAC)) parts.Add("MAC: " + MAC);
                return string.Join("   ", parts);
            }
        }

        public override string ToString()
        {
            var startup = string.IsNullOrEmpty(OnStartup) ? "-" : OnStartup;
            var shutdown = string.IsNullOrEmpty(OnShutdown) ? "-" : OnShutdown;
            var mac = string.IsNullOrEmpty(MAC) ? "" : "  MAC: " + MAC;
            return Name + " (" + IP + ")" + mac + "  Startup: " + startup + ", Shutdown: " + shutdown;
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
