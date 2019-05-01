using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Monetco
{
    public class AppRequest
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
        [JsonProperty("content-type")]
        public string ContentType { get; set; }

        [JsonExtensionData()]
        public Dictionary<string, JToken> Json { get; set; }

        public string ToJson()
        {
            string jsonString = JsonConvert.SerializeObject(this);
            return jsonString;
        }

        public static AppRequest FromJson(string json)
        {
            AppRequest jsonResult = JsonConvert.DeserializeObject<AppRequest>(json);
            return jsonResult;
        }

        public async static Task<AppRequest> CreateFromRequest(HttpRequest body)
        {
            var request = new AppRequest() { Url = body.Path, Method = body.Method,
                ContentType =  body.ContentType};
            
            var text = await ReadFromRequest(request, body);
            return text;
        }


        private async static Task<AppRequest> ReadFromRequest(AppRequest request, HttpRequest body)
        {
            if (!body.HasFormContentType)
            {
                using (MemoryStream memstream = new MemoryStream())
                {
                    await body.Body.CopyToAsync(memstream);
                    memstream.Position = 0;
                    string text = "";
                    using (StreamReader reader = new StreamReader(memstream))
                    {
                        lock (text = await reader.ReadToEndAsync())
                        {
                            if (text.ValidateJSON())
                            {
                                request.Body = text;
                            }
                            request.Body = text;
                            return request;
                        }
                    }
                }
            
            } else
            {
                string form = "";
                var b = body.Form.ToList();
                for(int i =0; i < b.Count; i++)
                {
                    form += b.ElementAt(i).Key + "=" + b.ElementAt(i).Value; 
                    if (i % 2 == 0 && i != b.Count - 1)
                    {
                        form += "&";
                    }
                }
                request.Body = form;
                return request;
            }

        }
    }
}
