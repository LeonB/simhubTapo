using Newtonsoft.Json;

namespace TapoDevices
{
    public class GetDeviceTime
    {
        public class Params
        {

        }

        public class Result
        {
            /// <summary>
            /// Device time, in Unix seconds.
            /// </summary>
            [JsonProperty("timestamp")]
            public long Timestamp { get; set; }

            /// <summary>
            /// Offset from UTC, in minutes.
            /// </summary>
            [JsonProperty("time_diff")]
            public int TimeDiff { get; set; }

            /// <summary>
            /// Timezone name.
            /// </summary>
            [JsonProperty("region")]
            public string Region { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest() =>
            Utils.CreateTapoRequest<Params>("get_device_time", null);
    }
}
