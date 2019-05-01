using Monetco.Host.Domain;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monetco
{
    public interface IResponseService
    {
        Dictionary<string, List<AppRequest>> GetAppRequests();
        Dictionary<string, List<SimulatorResponse>> GetNewResponses();
        Task<AppRequest> AddRequest(string id, HttpRequest body);
        Task AddRequest(string id, string url, string method, string ContentType, string body);
        void ClearResponses(string id);
        void ClearRequests(string id);
        List<AppRequest> GetRequestsById(string id);
        List<SimulatorResponse> Filter(string id, Scope scope, HttpContext contex);
        void AddResponse(string id, SimulatorResponse response);
        SimulatorResponse GetOfxResponseOptional(string id, HttpContext body);
        SimulatorResponse GetLastOrShchedule(List<SimulatorResponse> list, string id, bool needSchedule);
        List<SimulatorResponse> GetResponsesById(string id);
        SimulatorResponse GetResponseByScope(string id, string scope);
        SimulatorResponse GetResponseByUrl(string id, string scope, string url);
        SimulatorResponse GetResponseByRegexp(string id, string scope, string request);
    }
}
