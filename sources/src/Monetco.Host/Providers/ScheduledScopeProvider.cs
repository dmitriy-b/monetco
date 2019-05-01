using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Monetco.Host.Providers
{
    public class ScheduledScopeProvider : IScopeProvider
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

            var result = service.GetLastOrShchedule(responses, id, true);
            return result;
        }
    }
}
