using Newtonsoft.Json;

namespace TapoDevices
{
    public class SetDeviceInfo
    {
        public class Params
        {
            [JsonProperty("device_on", NullValueHandling = NullValueHandling.Ignore)]
            public bool? DeviceOn { get; set; }
        }

        public class ParamsBulb : Params
        {
            /// <summary>
            /// Brightness.
            /// </summary>
            /// <remarks>
            /// Range 1..100.
            /// </remarks>
            [JsonProperty("brightness", NullValueHandling = NullValueHandling.Ignore)]
            public int? Brightness { get; set; }

            /// <summary>
            /// Hue.
            /// </summary>
            /// <remarks>Range 0..359.</remarks>
            [JsonProperty("hue", NullValueHandling = NullValueHandling.Ignore)]
            public int? Hue { get; set; }

            /// <summary>
            /// Saturation.
            /// </summary>
            /// <remarks>Range 0..100.</remarks>
            [JsonProperty("saturation", NullValueHandling = NullValueHandling.Ignore)]
            public int? Saturation { get; set; }

            /// <summary>
            /// Color temperature, Kelvins.
            /// </summary>
            /// <remarks>
            /// Range 2500..6500. Set to 0 to apply specified hue and saturation.
            /// </remarks>
            [JsonProperty("color_temp", NullValueHandling = NullValueHandling.Ignore)]
            public int? ColorTemperature { get; set; }
        }

        public class Result
        {

        }

        internal static TapoRequest<Params> CreateRequest(Params parameters) =>
            Utils.CreateTapoRequest("set_device_info", parameters);

        internal static TapoRequest<ParamsBulb> CreateRequest(ParamsBulb parameters) =>
            Utils.CreateTapoRequest("set_device_info", parameters);
    }
}
