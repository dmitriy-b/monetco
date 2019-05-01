using Monetco.Host.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Monetco.Host.Domain
{
    public class SimulatorResponse : ContentResult
    {
        public Scope Scope { get; set; }
        public string Url { get; set; }
        public string Regexp { get; set; }

        public SimulatorResponse (string scopeName) : base()
        {
            Scope = Scopes.GetScopeFromName(scopeName);
            if (Scope == null)
            {
                Scope = new Scope() { Name = scopeName };
            }
        }

        public SimulatorResponse(Func<Scope, bool> func) : base()
        {
            Scope = Scopes.GetScopeFrom(func);
            if (Scope == null)
            {
                Scope = new Scope() { Name = "default" };
            }
        }

        public SimulatorResponse() : base()
        {

        }

        public IActionResult SendResponse(string id, 
            IResponseService service, HttpContext context, 
            IDictionary<string, object> additional)
        {
            IActionResult result;
            //Need for reading body stream for few times
            context.Request.EnableRewind();

            if (!string.IsNullOrEmpty(Scope.Provider))
            {
                result = CreateScopeFactory(Scope.Provider).Provide(id, service, Scope, context, additional);
            } else
            {
                result = CreateScopeFactory("DefaultScopeProvider")
                    .Provide(id, service, Scope, context, additional);
            }
            return result;
        }

        private IScopeProvider CreateScopeFactory(string scope)
        {
            //TODO: generate providers with reflaction
            switch (scope)
            {
                case "FileScopeProvider":
                    return new FileScopeProvider();
                case "DefaultScopeProvider":
                    return new DefaultScopeProvider();
                case "MirrorScopeProvider":
                    return new MirrorScopeProvider();
                case "RedirectScopeProvider":
                    return new RedirectScopeProvider();
                case "SoapScopeProvider":
                    return new SoapScopeProvider();
                case "ScheduledScopeProvider":
                    return new ScheduledScopeProvider();
                default:
                    return new DefaultScopeProvider();
            }
        }

        public string ToJson()
        {
            string jsonString = JsonConvert.SerializeObject(this);
            return jsonString;
        }

        public static string ToJson(List<SimulatorResponse> list)
        {
            string jsonString = JsonConvert.SerializeObject(list);
            return jsonString;
        }

        public static List<SimulatorResponse> FromJson(string json)
        {
            List<SimulatorResponse> jsonResult = JsonConvert.DeserializeObject<List<SimulatorResponse>>(json);
            return jsonResult;
        }

        public async static Task<List<SimulatorResponse>> ReadFromRequest(Stream body)
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
                            var result = SimulatorResponse.FromJson(text);
                            return result;
                        }
                        return null;
                    }
                }
            }
        }

        public override void ExecuteResult(ActionContext context)
        {
            foreach (var header in Scope.Headers)
            {
                foreach (var item in header)
                {
                    if (item.Key.ToLower() == "content-type")
                    {
                        if (!ContentType.Contains("text/plain"))
                        {
                            continue;
                        }
                    }
                    context.HttpContext.Response.Headers.Add(item.Key, item.Value);
                }                  
            }

            base.ExecuteResult(context);
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            foreach (var header in Scope.Headers)
            {
                foreach (var item in header)
                {
                    if (item.Key.ToLower() == "content-type")
                    {
                        if (ContentType != null)
                        {
                            continue;
                        }
                    }
                    context.HttpContext.Response.Headers.Add(item.Key, item.Value);
                }
            }
            return base.ExecuteResultAsync(context);
        }
    }
}
