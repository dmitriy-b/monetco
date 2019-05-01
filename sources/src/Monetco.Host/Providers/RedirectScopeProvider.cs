using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Monetco.Host.Providers
{
    public class RedirectScopeProvider : IScopeProvider
    {
        public IActionResult Provide(string id, IResponseService service, 
            Scope scope, HttpContext context, IDictionary<string, object> additional)
        {
            var req = service.AddRequest(id, context.Request);
            var response = service.GetResponseByScope(id, scope.Name);
            if (response != null)
            {
                using (var client = new HttpClient())
                {
                    var message = new HttpRequestMessage
                    {
                        Method = new HttpMethod(context.Request.Method),
                        Content = new StringContent(response.Content)
                    };
                    message.Content.Headers.ContentType.MediaType = response.ContentType;
                    message.RequestUri = new Uri(response.Url);
                    var result = client.SendAsync(message).Result.Content.ReadAsStringAsync().Result;
                    return new StatusCodeResult(200);
                }
            }
            return new StatusCodeResult(404);
        }
    }
}
