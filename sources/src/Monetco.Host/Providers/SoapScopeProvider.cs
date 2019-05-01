using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Monetco.Host.Providers
{
    public class SoapScopeProvider : IScopeProvider
    {
        public IActionResult Provide(string id, IResponseService service, Scope scope, 
            HttpContext context, IDictionary<string, object> additional)
        {
            var req = service.AddRequest(id, context.Request);
            scope.UseRegexp = true;
            var responses = service.Filter(id, scope, context);
            if (responses.Count == 0)
            {
                return new StatusCodeResult(412);
            }
            var last = responses.Last();

            var text = context.RequestBodyToString();
            if (text.Contains("RequestId"))
            {
                string pattern = @"Id>\S*Req";
                var name = last.Content;
                var replacement = Regex.Match(text, pattern).Value;
                var result = Regex.Replace(name, pattern, replacement);
                last.Content = result;
            }           
            return last;
        }
    }
}
