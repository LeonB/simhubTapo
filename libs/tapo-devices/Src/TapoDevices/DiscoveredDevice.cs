namespace TapoDevices
{
    public class DiscoveredDevice
    {
        public string IP { get; }
        public string MAC { get; }
        public string DeviceId { get; }
        public string Model { get; }

        public DiscoveredDevice(string ip, string mac, string deviceId, string model)
        {
            IP = ip;
            MAC = mac;
            DeviceId = deviceId;
            Model = model;
        }
    }
}
