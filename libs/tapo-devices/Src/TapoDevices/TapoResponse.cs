using Newtonsoft.Json;

namespace TapoDevices
{
    class TapoResponse<TResult>
    {
        [JsonProperty("error_code")]
        public int ErrorCode { get; set; } // TODO: enum with error codes

        [JsonProperty("result")]
        public TResult Result { get; set; }
    }
}
