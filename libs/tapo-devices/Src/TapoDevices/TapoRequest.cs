using Newtonsoft.Json;

namespace TapoDevices
{
    class TapoRequest<TParams>
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public TParams Parameters { get; set; }

        [JsonProperty("requestTimeMils")]
        public long RequestTimeMilliseconds { get; set; }
    }
}
