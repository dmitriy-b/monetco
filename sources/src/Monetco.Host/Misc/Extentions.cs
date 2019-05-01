using Monetco.Host.Domain;
using Monetco.Host.Misc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Monetco
{
    public static class Extentions
    {
        public static bool ValidateJSON(this string s)
        {
            try
            {
                JToken.Parse(s);
                return true;
            }
            catch (JsonReaderException)
            {
                //Trace.WriteLine(ex);
                return false;
            }
        }

        public static IApplicationBuilder UseLogRequestMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogRequestMiddleware>();
        }

        public static IApplicationBuilder UseLogResponseMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogResponseMiddleware>();
        }


        public static List<SimulatorResponse> FilterByRegexp(this List<SimulatorResponse> list, string regexp)
        {
            if (string.IsNullOrEmpty(regexp))
            {
                return list;
            }
            var match = list.FindAll(resp => !string.IsNullOrEmpty(resp.Regexp)).ToList<SimulatorResponse>();
            if (match.Count > 0)
            {
                var exist = match.Exists(response => Regex.IsMatch(regexp, response.Regexp));
                if (exist)
                {
                    return match.FindAll(response => Regex.IsMatch(regexp, response.Regexp));
                } else
                {
                    match = list.FindAll(response => !string.IsNullOrEmpty(response.Scope.Regexp));
                    exist = match.Exists(response => Regex.IsMatch(regexp, response.Scope.Regexp));
                    return match.FindAll(response => Regex.IsMatch(regexp, response.Regexp));
                }
            }
            return match;
        }

        public static List<SimulatorResponse> FilterByUrl(this List<SimulatorResponse> list, string begin, string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return list;
            }
            if (list == null)
            {
                return list;
            }
            var result = list.FindAll(response => !string.IsNullOrEmpty(response.Url));
            if (result.Count > 0)
            {
                var last = url.Replace(begin + "/", "");
                var exist = result.Any(response => last.EndsWith(response.Url) || (last + "/").EndsWith(response.Url));
                if (!exist)
                {
                    result = list.FindAll(response => response.Scope.Url != null);
                    exist = result.Any(response => last.EndsWith(response.Scope.Url));
                    return exist ? result.FindAll(response => last.EndsWith(response.Scope.Url)) : result;
                }
                return exist ? list.FindAll(response => response.Url != null && (last.EndsWith(response.Url) || (last + "/").EndsWith(response.Url))) : result;
            } else
            {
                result = list.FindAll(response => !string.IsNullOrEmpty(response.Scope.Url));
                if (result.Count > 0)
                {
                    var last = url.Replace(begin + "/", "");
                    result = list.FindAll(response => response.Scope.Url != null);
                    var exist = result.Any(response => last.EndsWith(response.Scope.Url));
                    return exist ? result.FindAll(response => last.EndsWith(response.Scope.Url)) : result;
                }
                return result;
            }
        }

        public static List<SimulatorResponse> FilterByScope(this List<SimulatorResponse> list, string scope)
        {
            if (list == null)
            {
                return new List<SimulatorResponse>();
            }
            list.ForEach(s => { if (s.Scope.Name == null) {
                    s.Scope = Scopes.GetScopeFromName(scope); } });
            return list.FindAll(response => response.Scope.Name.Contains(scope));
        }

        public static string RequestBodyToString(this HttpContext request)
        {
            return Utils.ReadStringFromRequest(request);
        }
    }
}
