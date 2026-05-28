using Newtonsoft.Json;

namespace TapoDevices
{
    public class TimeUsage
    {
        /// <summary>
        /// Today.
        /// </summary>
        [JsonProperty("today")]
        public int Today { get; set; }

        /// <summary>
        /// Past 7 days.
        /// </summary>
        [JsonProperty("past7")]
        public int Past7 { get; set; }

        /// <summary>
        /// Past 30 days.
        /// </summary>
        [JsonProperty("past30")]
        public int Past30 { get; set; }
    }
}
