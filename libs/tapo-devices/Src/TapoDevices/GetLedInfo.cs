using Newtonsoft.Json;

namespace TapoDevices
{
    public class GetLedInfo
    {
        public class Params
        {

        }

        public class Result
        {

        }

        public class ResultPlug : Result
        {
            /// <summary>
            /// Current status of LED (on/off).
            /// </summary>
            [JsonProperty("led_status")]
            public bool LedStatus { get; set; }

            /// <summary>
            /// LED rule ("always", "never", "night_mode").
            /// </summary>
            [JsonProperty("led_rule")]
            public string LedRule { get; set; }

            [JsonProperty("night_mode")]
            public NightMode NightMode { get; set; }
        }

        public class NightMode
        {
            /// <summary>
            /// Night mode type ("sunrise_sunset", "custom").
            /// </summary>
            [JsonProperty("night_mode_type")]
            public string NightModeType { get; set; }

            /// <summary>
            /// Start time, in minutes from day start.
            /// </summary>
            [JsonProperty("start_time")]
            public int StartTime { get; set; }

            /// <summary>
            /// End time, in minutes from day start.
            /// </summary>
            [JsonProperty("end_time")]
            public int EndTime { get; set; }

            [JsonProperty("sunrise_offset")]
            public int SunriseOffset { get; set; }

            [JsonProperty("sunset_offset")]
            public int SunsetOffset { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest() =>
            Utils.CreateTapoRequest<Params>("get_led_info", null);
    }
}
