using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Monetco.Host.Providers
{
    public interface IScopeProvider
    {
        IActionResult Provide(string id, IResponseService service, Scope scope, 
            HttpContext context, IDictionary<string, object> additional);
    }
}
