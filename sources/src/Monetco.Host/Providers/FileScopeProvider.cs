using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Monetco.Host.Providers
{
    public class FileScopeProvider : IScopeProvider
    {
        public IActionResult Provide(string id, IResponseService service, Scope scope, 
            HttpContext context, IDictionary<string, object> additional)
        {
            var req = service.AddRequest(id, context.Request);
            var responses = service.Filter(id, scope, context);
            if (responses.Count == 0)
            {
                return new StatusCodeResult(412);
            }
            var last = responses.Last();

            byte[] bytes = Convert.FromBase64String(last.Content);
            return new FileContentResult(bytes, last.ContentType);
        }
    }
}
