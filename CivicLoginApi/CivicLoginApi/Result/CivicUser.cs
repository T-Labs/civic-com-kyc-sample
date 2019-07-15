using Newtonsoft.Json;
using System.Collections.Generic;

namespace CivicLoginApi.Result
{
    public class CivicUser
    {
       [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("isOwner")]
        public bool IsOwner { get; set; }

        [JsonProperty ("isValid")]
        public bool IsValid { get; set; }
    }
   
}
