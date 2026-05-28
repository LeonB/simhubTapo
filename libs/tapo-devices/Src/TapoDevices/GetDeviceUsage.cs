using Newtonsoft.Json;

namespace TapoDevices
{
    public class GetDeviceUsage
    {
        public class Params
        {

        }

        public class Result
        {

        }

        public class ResultBulb : Result
        {
            [JsonProperty("time_usage")]
            public TimeUsage TimeUsage { get; set; }

            [JsonProperty("power_usage")]
            public TimeUsage PowerUsage { get; set; }

            [JsonProperty("saved_power")]
            public TimeUsage SavedPower { get; set; }
        }

        public class ResultPlug : Result
        {
            [JsonProperty("time_usage")]
            public TimeUsage TimeUsage { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest() =>
            Utils.CreateTapoRequest<Params>("get_device_usage", null);
    }
}
