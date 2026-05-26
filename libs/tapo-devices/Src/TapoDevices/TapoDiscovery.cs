using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TapoDevices
{
    public static class TapoDiscovery
    {
        // UDP broadcast magic payload for Tapo device discovery (port 20002)
        private static readonly byte[] DiscoveryPayload =
        {
            0x02, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x46, 0x3c, 0xb5, 0xd3
        };

        private const int DiscoveryPort = 20002;
        private const int HeaderLength = 16;

        public static async Task<List<DiscoveredDevice>> DiscoverAsync(TimeSpan timeout)
        {
            var devices = new List<DiscoveredDevice>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var udp = new UdpClient())
            {
                udp.EnableBroadcast = true;
                udp.Send(DiscoveryPayload, DiscoveryPayload.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

                var deadline = DateTime.UtcNow + timeout;

                while (true)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;

                    var receiveTask = udp.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(remaining)).ConfigureAwait(false) != receiveTask)
                        break;

                    UdpReceiveResult packet;
                    try { packet = receiveTask.Result; }
                    catch { continue; }

                    var device = TryParseResponse(packet.Buffer, packet.RemoteEndPoint.Address.ToString());
                    if (device != null && seen.Add(device.IP))
                        devices.Add(device);
                }
            }

            return devices;
        }

        private static DiscoveredDevice TryParseResponse(byte[] buffer, string senderIp)
        {
            if (buffer.Length <= HeaderLength)
                return null;

            try
            {
                var jsonBytes = new byte[buffer.Length - HeaderLength];
                Array.Copy(buffer, HeaderLength, jsonBytes, 0, jsonBytes.Length);

                var response = Utils.Deserialize<DiscoveryResponse>(jsonBytes);
                if (response == null || response.ErrorCode != 0 || response.Result == null)
                    return null;

                var r = response.Result;
                var ip = string.IsNullOrEmpty(r.IP) ? senderIp : r.IP;

                return new DiscoveredDevice(
                    ip: ip,
                    mac: r.MAC ?? "",
                    deviceId: r.DeviceId ?? "",
                    model: r.DeviceModel ?? ""
                );
            }
            catch
            {
                return null;
            }
        }

        private class DiscoveryResponse
        {
            [JsonPropertyName("error_code")]
            public int ErrorCode { get; set; }

            [JsonPropertyName("result")]
            public DiscoveryResult Result { get; set; }
        }

        private class DiscoveryResult
        {
            [JsonPropertyName("ip")]
            public string IP { get; set; }

            [JsonPropertyName("mac")]
            public string MAC { get; set; }

            [JsonPropertyName("device_id")]
            public string DeviceId { get; set; }

            [JsonPropertyName("device_model")]
            public string DeviceModel { get; set; }
        }
    }
}
