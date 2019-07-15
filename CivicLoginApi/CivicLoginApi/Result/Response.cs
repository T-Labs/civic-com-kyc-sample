using Newtonsoft.Json;
namespace CivicLoginApi.Result
{
    public class Response
    {
        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("encrypted")]
        public string Encrypted { get; set; }

        [JsonProperty("alg")]
        public string Alg { get; set; }

        [JsonProperty("processed")]
        public bool Processed { get; set; }
    }
}
