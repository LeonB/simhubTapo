using System;
using System.Text;
using System.Threading.Tasks;
using TapoDevices;
using Xunit;

namespace TapoDevices.Tests
{
    public class TapoDiscoveryTests
    {
        // Builds a fake UDP response buffer: 16-byte header followed by JSON payload.
        private static byte[] BuildPacket(string json)
        {
            var payload = Encoding.UTF8.GetBytes(json);
            var buffer = new byte[16 + payload.Length];
            Array.Copy(payload, 0, buffer, 16, payload.Length);
            return buffer;
        }

        [Fact]
        public void ParseResponse_ValidPacket_ReturnsDevice()
        {
            var packet = BuildPacket(@"{""error_code"":0,""result"":{""ip"":""192.168.1.100"",""mac"":""AA:BB:CC:DD:EE:FF"",""device_id"":""abc123"",""device_model"":""P115""}}");

            var device = TapoDiscovery.TryParseResponse(packet, "10.0.0.1");

            Assert.NotNull(device);
            Assert.Equal("192.168.1.100", device.IP);
            Assert.Equal("AA:BB:CC:DD:EE:FF", device.MAC);
            Assert.Equal("abc123", device.DeviceId);
            Assert.Equal("P115", device.Model);
        }

        [Fact]
        public void ParseResponse_EmptyIpInResponse_FallsBackToSenderIp()
        {
            var packet = BuildPacket(@"{""error_code"":0,""result"":{""ip"":"""",""mac"":""AA:BB:CC:DD:EE:FF"",""device_id"":""abc123"",""device_model"":""P115""}}");

            var device = TapoDiscovery.TryParseResponse(packet, "10.0.0.1");

            Assert.NotNull(device);
            Assert.Equal("10.0.0.1", device.IP);
        }

        [Fact]
        public void ParseResponse_NonZeroErrorCode_ReturnsNull()
        {
            var packet = BuildPacket(@"{""error_code"":-1501,""result"":{""ip"":""192.168.1.100"",""mac"":"""",""device_id"":"""",""device_model"":""""}}");

            var device = TapoDiscovery.TryParseResponse(packet, "192.168.1.100");

            Assert.Null(device);
        }

        [Fact]
        public void ParseResponse_MissingResult_ReturnsNull()
        {
            var packet = BuildPacket(@"{""error_code"":0}");

            var device = TapoDiscovery.TryParseResponse(packet, "192.168.1.100");

            Assert.Null(device);
        }

        [Fact]
        public void ParseResponse_MalformedJson_ReturnsNull()
        {
            var packet = BuildPacket("not json at all {{{");

            var device = TapoDiscovery.TryParseResponse(packet, "192.168.1.100");

            Assert.Null(device);
        }

        [Fact]
        public void ParseResponse_PacketTooShort_ReturnsNull()
        {
            var packet = new byte[10]; // less than 16-byte header

            var device = TapoDiscovery.TryParseResponse(packet, "192.168.1.100");

            Assert.Null(device);
        }

        [Fact]
        public void ParseResponse_ExactlyHeaderSize_ReturnsNull()
        {
            var packet = new byte[16]; // header only, no JSON

            var device = TapoDiscovery.TryParseResponse(packet, "192.168.1.100");

            Assert.Null(device);
        }

        [Fact]
        public void ParseResponse_MissingOptionalFields_ReturnsDeviceWithEmptyStrings()
        {
            var packet = BuildPacket(@"{""error_code"":0,""result"":{""ip"":""192.168.1.100""}}");

            var device = TapoDiscovery.TryParseResponse(packet, "10.0.0.1");

            Assert.NotNull(device);
            Assert.Equal("192.168.1.100", device.IP);
            Assert.Equal("", device.MAC);
            Assert.Equal("", device.DeviceId);
            Assert.Equal("", device.Model);
        }

        // Integration test — requires Tapo devices on the local network.
        // Run with: dotnet test --filter "Category=Integration"
        [Fact(Skip = "Requires Tapo devices on the local network")]
        [Trait("Category", "Integration")]
        public async Task DiscoverAsync_ReturnsAtLeastOneDevice()
        {
            var devices = await TapoDiscovery.DiscoverAsync(TimeSpan.FromSeconds(5));

            Assert.NotEmpty(devices);
            foreach (var d in devices)
            {
                Assert.False(string.IsNullOrEmpty(d.IP));
            }
        }
    }
}
