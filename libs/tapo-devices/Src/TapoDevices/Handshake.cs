using Newtonsoft.Json;

namespace TapoDevices
{
    class Handshake
    {
        public class Params
        {
            [JsonProperty("key")]
            public string Key { get; set; }
        }

        public class Result
        {
            [JsonProperty("key")]
            public string Key { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest(Params parameters) =>
            Utils.CreateTapoRequest("handshake", parameters);
    }
}
