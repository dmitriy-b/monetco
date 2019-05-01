using Microsoft.Extensions.Logging;
using Monetco;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Monetco.Host.Domain;
using Monetco.Host.Misc;

namespace Monetco
{
    public class ResponseService : IResponseService
    {  
        public volatile Dictionary<string, List<AppRequest>> AppRequests = new Dictionary<string, List<AppRequest>>();
        public volatile Dictionary<string, List<SimulatorResponse>> NewResponses = new Dictionary<string, List<SimulatorResponse>>();
        //TODO: Add logging to AddRequest method with event attributes (id, scopes, newstatuscode)
        public async Task<AppRequest> AddRequest(string id, HttpRequest body)
        {
            var result = AppRequest.CreateFromRequest(body);
            result.Wait();
            var req = result.Result;
            await Task.Run(() =>
            {
                if (!AppRequests.ContainsKey(id))
                {
                    var list = new List<AppRequest>
                            {
                                {  req }
                            };

                    AppRequests.Add(id, list);
                }
                else
                {
                    AppRequests[id].Add(req);
                }
                return req;
            });
            return req;
        }
       
        public void ClearResponses(string id)
        {
            if (NewResponses.ContainsKey(id))
            {
                NewResponses[id] = new List<SimulatorResponse>();
            } 
            else
            {
                var list = new List<SimulatorResponse>
                {
                    
                };
                NewResponses.Add(id, list);
            }
        }

        public void ClearRequests(string id)
        {
            if (AppRequests.ContainsKey(id))
            {
                AppRequests[id] = new List<AppRequest>();
            }
            else
            {
                AppRequests.Add(id, new List<AppRequest>());
            }
        }


        public List<SimulatorResponse> GetResponsesById(string id)
        {
            if (NewResponses.ContainsKey(id))
            {
                return NewResponses[id];
            }
            return null;
        }

        public List<AppRequest> GetRequestsById(string id)
        {
            if (AppRequests.ContainsKey(id))
            {
                return AppRequests[id];
            }
            return new List<AppRequest>();
        }

        public Dictionary<string, List<AppRequest>> GetAppRequests()
        {
            return AppRequests;
        }

        public Dictionary<string, List<SimulatorResponse>> GetResponses()
        {
            return NewResponses;
        }

        public async Task AddRequest(string id, string url, string method, string ContentType, string body)
        {
            var req = new AppRequest() { Body = body,
                ContentType = ContentType,
                Method = method, Url = url};
            await Task.Run(() =>
            {
                if (!AppRequests.ContainsKey(id))
                {
                    var list = new List<AppRequest>
                            {
                                {  req }
                            };

                    AppRequests.Add(id, list);
                }
                else
                {
                    AppRequests[id].Add(req);
                }
            });
        }


        public Dictionary<string, List<SimulatorResponse>> GetNewResponses()
        {
            return NewResponses;
        }

        public List<SimulatorResponse> Filter(string id, Scope scope, HttpContext context)
        {
            var responses = GetResponsesById(id);
            responses = responses.FilterByScope(scope.Name);
            if (scope.UseUrl)
            {
                responses = responses.FilterByUrl(scope.Name, context.Request.Path);
            }
            if (scope.UseRegexp)
            {
                var regexp = context.RequestBodyToString();
                responses = responses.FilterByRegexp(regexp);
            }
            return responses;
        }

        public void AddResponse(string id, SimulatorResponse response)
        {
            if (!NewResponses.ContainsKey(id))
            {
                var list = new List<SimulatorResponse>
                {
                    response
                };
                NewResponses.Add(id, list);
            }
            else
            {
                NewResponses[id].Add(response);
            }
        }

        public SimulatorResponse GetOfxResponseOptional(string id, HttpContext body)
        {
            var text = Utils.ReadStringFromRequest(body);

            if (NewResponses.ContainsKey(id))
            {
                var matchList = NewResponses[id].FindAll(resp => resp.Scope.Name == "ofx");
                if (matchList.Count == 0)
                {
                    return null;
                }
                var exist = NewResponses[id].Exists(response => response.Scope.Name == "ofx"
                    && Regex.IsMatch(text, response.Url));
                if (exist)
                {
                    return NewResponses[id].Last(response => response.Scope.Name == "ofx" &&
                        Regex.IsMatch(text, response.Url));
                }
                else
                {
                    return NewResponses[id].Last(response => response.Scope.Name == "ofx");
                }
            }
            return null;
        }

        public SimulatorResponse GetLastOrShchedule(List<SimulatorResponse> list, string id, 
            bool needSchedule)
        {
            SimulatorResponse last = list.Last();
            if (list.Count > 1)
            {
                last = NewResponses[id].Last(el => el.Equals(last));
                if (needSchedule)
                {
                    if (NewResponses[id].Count(el => el.Equals(last)) > 0)
                    {
                        NewResponses[id].Remove(last);
                    }
                    return last;
                }
            }
            return last;
        }

        public SimulatorResponse GetResponseByScope(string id, string scope)
        {
            if (NewResponses.ContainsKey(id))
            {
                //var exist = NewResponses[id].Exists(response => response.Scope == null);
                //if (exist)
                //{
                //    NewResponses[id].ForEach(r => r.SetScope());
                //}
                var result = NewResponses[id].FindAll(response => response.Scope.Name.Contains(scope));
                if (result.Count > 0)
                {
                    return result.Last();
                }
            }
            return null;
        }

        public SimulatorResponse GetResponseByUrl(string id, string scope, string url)
        {
            if (NewResponses.ContainsKey(id))
            {
                var result = NewResponses[id].FindAll(response => response.Scope.Name == scope);
                if (result.Count > 0)
                {
                    result = result.FindAll(response => response.Url != null);
                }
                if (result.Count > 0)
                {
                    var last = url.Replace(id + "/", "");
                    if (String.Equals(id, "1") && String.Equals(scope, "mw"))
                    {
                        last = url.Replace("/" + id + "/", "/");
                    }
                    var exist = result.Exists(response => last.EndsWith(response.Url));
                    //TODO: check response.ResponseUrl != null
                    return exist ? NewResponses[id].Last(response => response.Scope.Name.Contains(scope) 
                    && response.Url != null && last.EndsWith(response.Url))
                        : GetResponseByScope(id, scope);
                }
                return GetResponseByScope(id, scope);
            }
            return null;
        }

        public SimulatorResponse GetResponseByRegexp(string id, string scope, string request)
        {
            if (NewResponses.ContainsKey(id))
            {
                var matchList = NewResponses[id].FindAll(resp => resp.Scope.Name.Contains(scope));
                if (matchList.Count == 0)
                {
                    return null;
                }
                var match = matchList.FindAll(resp => !string.IsNullOrEmpty(resp.Url)).ToList<SimulatorResponse>();
                if (match.Count > 0)
                {
                    var exist = match.Exists(response => Regex.IsMatch(request, response.Url));
                    if (exist)
                    {
                        return match.Last(response => Regex.IsMatch(request, response.Url));
                    }
                }

                else
                {
                    return NewResponses[id].Last(response => response.Scope.Name.Contains(scope));
                }
            }
            return null;
        }
    }
}
