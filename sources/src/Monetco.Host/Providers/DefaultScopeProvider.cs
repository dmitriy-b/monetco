using System.Collections.Generic;
using System.Linq;
using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Monetco.Host.Providers
{
    public class DefaultScopeProvider : IScopeProvider
    {
        //TODO: rename to filter?
        public IActionResult Provide(string id, IResponseService service, Scope scope, 
            HttpContext context, IDictionary<string, object> additional)
        {
            var req = service.AddRequest(id, context.Request);
            //scope.Url = req.Result.Url;
            var responses = service.Filter(id, scope, context);
            if (responses.Count == 0)
            {
                return new StatusCodeResult(412);
            }
            var last = responses.Last();

            return last;
        }
    }
}
