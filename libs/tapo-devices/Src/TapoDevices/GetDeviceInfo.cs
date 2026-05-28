using System;
using System.Text;
using Newtonsoft.Json;

namespace TapoDevices
{
    public class GetDeviceInfo
    {
        public class Params
        {

        }

        public class Result
        {
            [JsonProperty("device_id")]
            public string DeviceId { get; set; }

            [JsonProperty("fw_ver")]
            public string FirmwareVersion { get; set; }

            [JsonProperty("hw_ver")]
            public string HardwareVersion { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("mac")]
            public string MacAddress { get; set; }

            [JsonProperty("hw_id")]
            public string HardwareId { get; set; }

            [JsonProperty("fw_id")]
            public string FirmwareId { get; set; }

            [JsonProperty("oem_id")]
            public string OemId { get; set; }

            [JsonProperty("overheated")]
            public bool Overheated { get; set; }

            [JsonProperty("ip")]
            public string IPAddress { get; set; }

            [JsonProperty("time_diff")]
            public int TimeDifference { get; set; }

            [JsonProperty("ssid")]
            public string SSIDEncoded { get; set; }

            [JsonIgnore]
            public string SSID => Encoding.UTF8.GetString(Convert.FromBase64String(this.SSIDEncoded));

            [JsonProperty("rssi")]
            public int Rssi { get; set; }

            [JsonProperty("signal_level")]
            public int SignalLevel { get; set; }

            [JsonProperty("latitude")]
            public double Latitude { get; set; }

            [JsonProperty("longitude")]
            public double Longitude { get; set; }

            [JsonProperty("lang")]
            public string Language { get; set; }

            [JsonProperty("avatar")]
            public string Avatar { get; set; }

            [JsonProperty("region")]
            public string Region { get; set; }

            [JsonProperty("specs")]
            public string Specs { get; set; }

            [JsonProperty("nickname")]
            public string NicknameEncoded { get; set; }

            [JsonIgnore]
            public string Nickname => Encoding.UTF8.GetString(Convert.FromBase64String(this.NicknameEncoded));

            [JsonProperty("has_set_location_info")]
            public bool HasSetLocationInfo { get; set; }

            [JsonProperty("device_on")]
            public bool DeviceOn { get; set; }
        }

        public class ResultBulb : Result
        {
            [JsonProperty("brightness")]
            public int Brightness { get; set; }

            [JsonProperty("hue")]
            public int Hue { get; set; }

            [JsonProperty("saturation")]
            public int Saturation { get; set; }

            [JsonProperty("color_temp")]
            public int ColorTemperature { get; set; }

            [JsonProperty("color_temp_range")]
            public int[] ColorTemperatureRange { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest() =>
            Utils.CreateTapoRequest<Params>("get_device_info", null);
    }
}
