using Monetco.Host.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Monetco;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Monetco
{
    public class SimulatorRequest
    {
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("scope")]
        public string Scope { get; set; }
        [JsonProperty("regexp")]
        public string Regexp { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("scheduled")]
        public bool IsScheduled { get; set; }
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }
        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonExtensionData()]
        public Dictionary<string, JToken> Json { get; set; }

        public string ToJson()
        {
            string jsonString = JsonConvert.SerializeObject(this);
            return jsonString;
        }

        public static string ToJson(List<SimulatorRequest> list)
        {
            string jsonString = JsonConvert.SerializeObject(list);
            return jsonString;
        }

        public static SimulatorRequest FromJson(string json)
        {
            SimulatorRequest jsonResult = JsonConvert.DeserializeObject<SimulatorRequest>(json);
            return jsonResult;
        }

        public async static Task<SimulatorRequest> ReadFromRequest(Stream body)
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                await body.CopyToAsync(memstream);
                memstream.Position = 0;
                string text = "";
                using (StreamReader reader = new StreamReader(memstream))
                {
                    lock (text = await reader.ReadToEndAsync())
                    {
                        if (text.ValidateJSON())
                        {
                            var result = SimulatorRequest.FromJson(text);
                            return result;
                        }
                        return null;
                    }
                }
            }

        }
        
        public Scope GetScope()
        {
            var scope = Scopes.GetScopeFromName(Scope);
            if (scope == null)
            {
                return new Scope() { Name = Scope };
            }
            return scope;
        }

    }
}
