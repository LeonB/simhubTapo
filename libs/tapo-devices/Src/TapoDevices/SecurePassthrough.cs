using Newtonsoft.Json;

namespace TapoDevices
{
    class SecurePassthrough
    {
        public class Params
        {
            [JsonProperty("request")]
            public string Request { get; set; }
        }

        public class Result
        {
            [JsonProperty("response")]
            public string Response { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest(Params parameters) =>
            Utils.CreateTapoRequest("securePassthrough", parameters);
    }
}
