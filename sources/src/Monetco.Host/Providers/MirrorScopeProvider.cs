using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Monetco.Host.Providers
{
    public class MirrorScopeProvider : IScopeProvider
    {
        public IActionResult Provide(string id, IResponseService service, Scope scope, 
            HttpContext context, IDictionary<string, object> additional)
        {
            var req = service.AddRequest(id, context.Request).Result;
            return new SimulatorResponse(scope.Name) { Content = $" $Body: {req.Body}; " +
                $"Content-Type: {req.ContentType}; Method: ${req.Method}",
                StatusCode = 200};
        }
    }
}
