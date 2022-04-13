using Newtonsoft.Json;

namespace EscapeBot
{
    public struct ConfigJson
    {
        //struct that can convert the json config file to strings to be used at start
        [JsonProperty("token")]
        public string token { get; private set; }
        [JsonProperty("prefix")]
        public string prefix { get; private set; }
    }
}
