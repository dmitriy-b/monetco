using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;
using AutoMapper;
using Monetco;
using Newtonsoft.Json;
using System.Reflection;
using System.Net.Http;
using Monetco.Host.Domain;
using Microsoft.Extensions.Options;
using Monetco.Host;

namespace Monetco.Controllers
{

    /// <summary>
    /// Simulator controller
    /// </summary>
    [Route("api/")]
    public class SimulatorController : Controller
    {
        private readonly ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private readonly IResponseService _service;
        private readonly IMapper _mapper;
        private readonly Scopes _configuration;

        /// <summary>
        /// Create controller for responses` simulation
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="loggerFactory">loggerFactory</param>
        /// <param name="mapper">Automapper</param>
        /// <param name="simulatorResponse">SimulatorResponse service</param>
        /// <param name="configuration">appsettings values</param>
        public SimulatorController(ILogger<SimulatorController> logger, ILoggerFactory loggerFactory, 
            IMapper mapper, IResponseService simulatorResponse, IOptions<Scopes> configuration)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _mapper = mapper;
            _service = simulatorResponse;
            _configuration = configuration.Value;
        }

        /// <summary>
        /// Get all responses
        /// </summary>
        /// <returns>List of responses</returns>
        [HttpGet]
        public string Get()
        {
            var json = JsonConvert.SerializeObject(_service, Formatting.Indented);
            var result = json.Replace("\\r\\n    ", "").Replace("\\r\\n", "");
            return result;
        }

        /// <summary>
        /// Get options. MFM service method
        /// </summary>
        /// <returns>Status code 200</returns>
        [HttpOptions("/")]
        public async Task<IActionResult> GetOptions()
        {
            return await Task.Run(() => new StatusCodeResult(200));
        }

        /// <summary>
        /// Get all requests for all users
        /// </summary>
        /// <returns></returns>
        [HttpGet("requests")]
        public Dictionary<string, List<AppRequest>> GetRequests()
        {
            var req = _service.GetAppRequests();
            return req;
        }

        /// <summary>
        /// Get all responses for chosen ID
        /// </summary>
        /// <param name="id">additional ID from URL</param>
        /// <returns>List of responses</returns>
        //[SwaggerOperation(Tags = new[] { "Test" })]
        [HttpGet("{id}/responses")]
        public List<SimulatorResponse> GetById([FromRoute]string id)
        {
            if (_service.GetResponsesById(id) != null)
            {
                return _service.GetResponsesById(id);
            }
            return new List<SimulatorResponse>();
        }

        /// <summary>
        /// Get all MFM requests for chosen ID
        /// </summary>
        /// <param name="id">additional ID from URL</param>
        /// <returns>List of requests</returns>
        [HttpGet("{id}/requests")]
        public List<AppRequest> GetRequestsById([FromRoute]string id)
        {
            if (_service.GetRequestsById(id) != null)
            {
                var req =  _service.GetRequestsById(id);
                return req;
            }
            return new List<AppRequest>();
        }

        
        [HttpPost("{id}/set/list")]
        public IActionResult SetList([FromBody]List<SimulatorRequest> result, [FromRoute]string id)
        {
            var value = new List<SimulatorResponse>();
            result.ForEach(resp => {
                var test = _mapper.Map<SimulatorRequest, SimulatorResponse>(resp);
                if (test != null)
                {
                    value.Add(test);
                }
            });

            if (value.Any(val => val.Scope.Name == "config"))
            {
                var configs = value.First(val => val.Scope.Name == "config").Content;
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(configs);
                values.Add("{ss.guid}", Guid.NewGuid().ToString());
                values.Add("{ss.timestamp}", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                values.Add("{ss.id}", id);
                value.FindAll(vals => vals.Scope.Name != "config").ForEach(el =>
                {
                    foreach (var item in values)
                    {
                        el.Content = el.Content.Replace(item.Key, item.Value);
                        if (el.Url != null)
                        {
                            el.Url = el.Url.Replace(item.Key, item.Value);
                        }
                    }
                });
            }
            if (_service.GetNewResponses().ContainsKey(id))
            {
                _service.GetNewResponses()[id] = value;
            }
            else
            {
                _service.GetNewResponses().Add(id, value);
            }
            return StatusCode(201);
        }

        /// <summary>
        /// Clear responses for current ID
        /// </summary>
        /// <param name="id">User ID from path</param>
        [HttpDelete("{id}")]
        public void Delete([FromRoute]string id)
        {
            _service.ClearResponses(id);
        }

        /// <summary>
        /// Clear requests for current ID
        /// </summary>
        /// <param name="id">User ID from path</param>
        [HttpDelete("{id}/requests")]
        public void ClearRequests([FromRoute]string id)
        {
            _service.ClearRequests(id);
        }

        /// <summary>
        /// Delete all users` responses
        /// </summary>
        [HttpDelete("/all")]
        public void Delete()
        {
            _service.GetNewResponses().Clear();
        }

        /// <summary>
        /// Export responses for current ID as json file
        /// </summary>
        /// <remarks>File name "export_"{id}".json". Encoding UTF-8.</remarks>
        /// <param name="id">User ID from path</param>
        /// <returns>Sheduled responses in json format</returns>
        [HttpGet("{id}/export")]
        public FileResult Export([FromRoute]string id)
        {
            var list = _service.GetResponsesById(id);
            HttpContext.Response.ContentType = "application/json";
            List<byte> byteList = new List<byte>();
            var begin = Encoding.UTF8.GetBytes("[");
            byteList.AddRange(begin);
            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    list[i].Content = null;
                    var bytes = Encoding.UTF8.GetBytes(list[i].ToJson());
                    byteList.AddRange(bytes);
                    if (i != list.Count - 1)
                    {
                        byteList.AddRange(Encoding.UTF8.GetBytes(","));
                    }
                }
            }
            
            var end = Encoding.UTF8.GetBytes("]");
            byteList.AddRange(end);
            FileContentResult results = new FileContentResult(byteList.ToArray(), "application/json")
            {
                FileDownloadName = "export_" + id + ".json"
            };
            return results;
        }

        
        /// <summary>
        /// Import responses from json array and replace for current ID
        /// </summary>
        /// <remarks>String must be in json format</remarks>
        /// <param name="id">User ID from path</param>
        /// <param name="value">Json string to import</param>
        /// <response code="412">Wrong value format</response>
        /// <response code="201">Responses were success imported</response>
        /// <returns>Status code 201</returns>
        [HttpPost("{id}/replace")]
        public IActionResult Replace([FromRoute]string id, [FromBody]List<SimulatorResponse> value)
        {
            if (value.Any(val => val.Scope.Name == "config"))
            {
                var configs = value.First(val => val.Scope.Name == "config").Content;
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(configs);
                values.Add("{ss.guid}", Guid.NewGuid().ToString());
                values.Add("{ss.timestamp}", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                values.Add("{ss.id}", id);
                value.FindAll(vals => vals.Scope.Name != "config").ForEach(el =>
                {
                    foreach(var item in values)
                    {
                        el.Content = el.Content.Replace(item.Key, item.Value);
                    }                
                });
            }
            value.ForEach(s => { s.Scope = Scopes.GetScopeFromName(s.Scope.Name);});
            if (_service.GetNewResponses().ContainsKey(id))
            {
                _service.GetNewResponses()[id] = value;
            } else
            {
                _service.GetNewResponses().Add(id, value);
            }
            return StatusCode(201);
        }
        //TODO: Move to manage controller
        /// <summary>
        /// Get latest version and build date
        /// </summary>
        /// <response code="200">Success</response>
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var result = new ContentResult();
            var res = typeof(Monetco.Host.Program).GetTypeInfo()
                .Assembly.GetName().Version.ToString();
            Assembly assembly = typeof(Monetco.Host.Program).GetTypeInfo().Assembly;
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(assembly.Location);
            DateTime lastModified = fileInfo.LastWriteTime;

            var ip = HttpContext.Connection.LocalIpAddress;
            result.Content = "version " + res + ", by " + lastModified + "; IP - " + ip;
            return result;
        }

        /// <summary>
        /// Make a GET request and send other request to chosen URL (from ResponseUrl). 
        /// </summary>
        /// <param name="id">SS ID</param>
        /// <param name="scope">redirect scope</param>
        /// <param name="method">Chosen method (e.g. GET or POST)</param>
        /// <returns>IActionResult 200 or 400 if responses were not loaded</returns>
        [ApiExplorerSettings(GroupName = "EmulatedServices")]
        [HttpGet("{id}/{scope}/request/{method}")]
        public IActionResult RedirectResult([FromRoute]string id, [FromRoute]string scope, [FromRoute]string method)
        {
            var req = _service.AddRequest(id, Request);
            //TODO: need to add a new scope redirect
            var response = _service.GetResponseByScope(id, scope);
            if (response != null)
            {
                using (var client = new HttpClient())
                {
                    var message = new HttpRequestMessage();
                    message.Method = new HttpMethod(method);
                    message.Content = new StringContent(response.Content);
                    message.Content.Headers.ContentType.MediaType = response.ContentType;
                    message.RequestUri = new Uri(response.Url);
                    var result = client.SendAsync(message).Result.Content.ReadAsStringAsync().Result;
                    return Ok("Done");
                }
            }
            return NotFound("Failed to found response");
        }

        [ApiExplorerSettings(GroupName = "EmulatedServices")]
        //[AcceptVerbs("GET","POST")]
        [Route("{id}/{scope}/" + Constants.PATH)]
        public async Task<IActionResult> GetScopeResult([FromRoute]string id, [FromRoute]string scope)
        {
            var t = await Task.Run(() =>
            {     
                var result = new SimulatorResponse(scope).SendResponse(id, _service, HttpContext, null);
                return result;
            });
            return t;
        }

        [HttpPost("{id}/set/scopes")]
        public IActionResult SetScopes([FromRoute]string id, [FromBody]List<SimulatorRequest> result)
        {
            var value = new List<SimulatorResponse>();
            if (result == null)
            {
                return StatusCode(412);
            }
            result.ForEach(resp => {
                var test = _mapper.Map<SimulatorRequest, SimulatorResponse>(resp);
                if (test != null)
                {
                    value.Add(test);
                }
            });

            if (value.Any(val => val.Scope.Name == "config"))
            {
                var configs = value.First(val => val.Scope.Name == "config").Content;
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(configs);
                values.Add("{ss.guid}", Guid.NewGuid().ToString());
                values.Add("{ss.timestamp}", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                values.Add("{ss.id}", id);
                value.FindAll(vals => vals.Scope.Name != "config").ForEach(el =>
                {
                    foreach (var item in values)
                    {
                        el.Content = el.Content.Replace(item.Key, item.Value);
                        if (el.Url != null)
                        {
                            el.Url = el.Url.Replace(item.Key, item.Value);
                        }
                    }
                });
            }
            if (_service.GetNewResponses().ContainsKey(id))
            {
                _service.GetNewResponses()[id] = value;
            }
            else
            {
                _service.GetNewResponses().Add(id, value);
            }
            return StatusCode(201);
        }

        [HttpPost("{id}/set/json")]
        public IActionResult SetRequest([FromBody]SimulatorRequest result, [FromRoute]string id)
        {
            var test = _mapper.Map<SimulatorRequest, SimulatorResponse>(result);
            if (test != null)
            {
                _service.AddResponse(id, test);
                return StatusCode(201);
            }
            return StatusCode(412, "Wrong format. Please, check if response body has json format");
        }

        [ApiExplorerSettings(GroupName = "EmulatedServices")]
        [Route("{id}/{url}")]
        public async Task<IActionResult> GetBaseUrlResult ([FromRoute]string id, [FromRoute]string url)
        {
            var t = await Task.Run(() =>
            {
                var result = new SimulatorResponse(s => !string.IsNullOrEmpty(s.Url) && s.Url.EndsWith(url))
                    .SendResponse(id, _service, HttpContext, null);
                return result;
            });
            return t;
        }
    }
}
