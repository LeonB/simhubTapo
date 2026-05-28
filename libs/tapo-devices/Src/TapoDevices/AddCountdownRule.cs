using Newtonsoft.Json;

namespace TapoDevices
{
    class AddCountdownRule
    {
        public class Params
        {
            /// <summary>
            /// Is rule enabled.
            /// </summary>
            [JsonProperty("enable")]
            public bool Enable { get; set; }

            /// <summary>
            /// Delay before changing state, in seconds.
            /// </summary>
            [JsonProperty("delay")]
            public int Delay { get; set; }

            [JsonProperty("desired_states")]
            public ParamsStates DesiredStates { get; set; }
        }

        public class ParamsStates
        {
            [JsonProperty("on")]
            public bool On { get; set; }
        }

        public class Result
        {

        }

        internal static TapoRequest<Params> CreateRequest(Params parameters) =>
            Utils.CreateTapoRequest("add_countdown_rule", parameters);
    }
}
