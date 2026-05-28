using Newtonsoft.Json;

namespace TapoDevices
{
    public class GetCountdownRules
    {
        public class Params
        {

        }

        public class ResultStates
        {
            [JsonProperty("on")]
            public bool On { get; set; }
        }

        public class Result
        {
            [JsonProperty("enable")]
            public bool Enable { get; set; }

            [JsonProperty("countdown_rule_max_count")]
            public int RulesMaxCount { get; set; }

            [JsonProperty("rule_list")]
            public ResultRule[] Rules { get; set; }
        }

        public class ResultRule
        {
            [JsonProperty("enable")]
            public bool Enable { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            /// <summary>
            /// Initially set delay before changing state, in seconds.
            /// </summary>
            [JsonProperty("delay")]
            public int Delay { get; set; }

            /// <summary>
            /// Currently remaining time before changing state, in seconds.
            /// </summary>
            [JsonProperty("remain")]
            public int Remain { get; set; }

            [JsonProperty("desired_states")]
            public ResultStates DesiredStates { get; set; }
        }

        internal static TapoRequest<Params> CreateRequest() =>
            Utils.CreateTapoRequest<Params>("get_countdown_rules", null);
    }
}
