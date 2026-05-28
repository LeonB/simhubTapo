using Newtonsoft.Json;

namespace TapoDevices
{
    class LoginDevice
    {
        public class Params
        {
            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }
        }

        public class Result
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest(Params parameters) =>
            Utils.CreateTapoRequest("login_device", parameters);
    }
}
