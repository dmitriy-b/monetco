using System;
using System.Collections.Generic;
using System.Linq;

namespace Monetco.Host.Domain
{

    public class Scope
    {
        private IList<IDictionary<string, string>> _headers = new List<IDictionary<string, string>>();

        public string Name { get; set; }
        public IList<IDictionary<string, string>> Headers { get { return _headers; } set { _headers = value; } }
        public string Provider { get; set; }
        public bool IsScheduled { get; set; }
        public bool UseUrl { get; set; }
        public bool UseRegexp { get; set; }
        public string Url { get; set; }
        public string Regexp { get; set; }
    }

    public class Scopes
    {
        public static IList<Scope> ScopesList { get; set; }

        public static Scope GetScopeFromName(string name)
        {
            if (!ScopesList.Any(s => s.Name == name))
                return null;
            return ScopesList.First(s => s.Name == name);
        }

        public static Scope GetScopeFrom(Func<Scope, bool> func)
        {
            if (!ScopesList.Any(func))
                return null;
            return ScopesList.First(func);
        }
    }
}
