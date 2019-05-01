using Monetco.Host.Domain;
using Monetco.Host;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Monetco.Tests
{
    public class ControllerTest
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings() {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private readonly IResponseService _responses = new ResponseService();
        string json = @"{
	        ""json"": {

                ""token"": ""IfRH -6UHhdvB4zhCDT"",
		        ""originalCredentials"": false,
		        ""clientUid"": 16

            },
	        ""scope"": ""mw""
        }";
        string jsonRegexp = @"{
	        ""json"": {

                ""token"": ""IfRH -6UHhdvB4zhCDT"",
		        ""originalCredentials"": false,
		        ""clientUid"": 16

            },
            ""regexp"": ""test"",
	        ""scope"": ""mw""
        }";

        public static IWebHostBuilder BuildWebHost(string[] args, IConfiguration config) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseConfiguration(config)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
            .UseUrls("http://*:5000");

        public ControllerTest()
        {
            // Arrange
            var config = new ConfigurationBuilder()
#if RELEASE
                .AddCommandLine(args)
#endif
                //.AddJsonFile("hosting.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddJsonFile("appsettings.json")
                .Build();
            _server = new TestServer(BuildWebHost(null, config));
            _client = _server.CreateClient();

            //TODO: Create TestStartup to fix some Automapper tests
            //var someOptions = Options.Create(new Scopes());
        }

        [Fact]
        public async Task GetAllEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            string jsonString = JsonConvert.SerializeObject(_responses, Formatting.Indented, settings);
            // Assert
            Assert.Equal("{\r\n  \"AppRequests\": {},\r\n  \"NewResponses\": {}\r\n}",
                responseString);
            Assert.Equal(jsonString.ToLower(), responseString.ToLower());
        }     

        [Fact]
        public async Task GetResponsesCustomEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/123/responses");
            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode; 
            string jsonString = JsonConvert.SerializeObject(new List<SimulatorResponse>());

            // Assert
            Assert.Equal(jsonString, responseString);
            Assert.Equal("[]", responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);
        }

        [Fact]
        public async Task GetRequestsCustomEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/123/requests");
            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;
            string jsonString = JsonConvert.SerializeObject(new List<SimulatorResponse>());

            // Assert
            Assert.Equal(jsonString, responseString);
            Assert.Equal("[]", responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);
        }

        [Fact]
        public async Task GetRequestsDefaultEmpty()
        {
            // Act
            var response = await _client.GetAsync("api/requests");
            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;
            string jsonString = "{}";

            // Assert
            Assert.Equal(jsonString, responseString);
            Assert.Equal("{}", responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);

        }
     
        [Fact]
        public async Task PostMWCustomEmpty()
        {
            await GetDefaultResponse("/api/123/api/v1/rest/Tokenservice/Token", "");
        }
        
        [Fact]
        public async Task ContentTypeCheck()
        {
            // Act
            List<SimulatorResponse> ls = new List<SimulatorResponse>();
            SimulatorRequest jsonString = JsonConvert.DeserializeObject<SimulatorRequest>(json);
            ls.Add(new SimulatorResponse() { Scope = new Scope() { Name = "mw" }, Content = jsonString.Json["json"].ToString(), ContentType = "" });
            ls.Add(new SimulatorResponse() { Scope = new Scope() { Name = "mw" }, Content = jsonString.Json["json"].ToString(), Url = "/Image1" });
            ls.Add(new SimulatorResponse() { Scope = new Scope() { Name = "mw" },
                Content = jsonString.Json["json"].ToString(),
                ContentType = "image/jpeg", Url = "/Image" });
            

            string lsJson = SimulatorResponse.ToJson(ls);
            var text = new StringContent(lsJson);
            text.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var request = await _client.PostAsync("/api/123/replace", text);

            request.EnsureSuccessStatusCode();
            text = new StringContent(json);
            var response = await _client.PostAsync("/api/123/mw/Image", text);

            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;

            // Assert
            Assert.Equal(jsonString.Json["json"].ToString(), responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType.MediaType);

            text = new StringContent(json);
            response = await _client.PostAsync("/api/123/mw/Token3", text);

            responseString = await response.Content.ReadAsStringAsync();
            responseCode = response.StatusCode;

            // Assert
//            Assert.Equal(jsonString.Json["json"].ToString(), responseString);
            Assert.Equal(System.Net.HttpStatusCode.PreconditionFailed, responseCode);
//            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

            text = new StringContent(json);
            response = await _client.PostAsync("/api/123/mw/Image1", text);

            responseString = await response.Content.ReadAsStringAsync();
            responseCode = response.StatusCode;

            // Assert
            Assert.Equal(jsonString.Json["json"].ToString(), responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        }    

        [Fact]
        public async Task TestGetOptions()
        {
            // Act
            var address = _server.BaseAddress;
            var message = new HttpRequestMessage() { Method = HttpMethod.Options,
                RequestUri = address };

            
            var response = await _client.SendAsync(message);
            //response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;

            // Assert
            Assert.Equal("", responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);
        }

       

        #region Service Methods
        public async Task SetAsyncRequest(String requestPath, String responsePath, 
            string additionalText = null)
        {
            // Arange
            var content = additionalText;
            if (String.IsNullOrEmpty(additionalText))
            {
                var random = new Random().NextDouble();
                content = "<SIGNONMSGSRSV" + random + ">";
            } 
            
            var text = new StringContent(content);
            var request = await _client.PostAsync(requestPath, text);

            // Act
            text = new StringContent(content);
            var response = await _client.PostAsync(responsePath, text);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;

            // Assert
            Assert.Equal(content, responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);
        }

        public async Task GetDefaultResponse(String responsePath, String content)
        {
            // Act
            var response = await _client.PostAsync(responsePath, 
                new StringContent(content));

            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;

            // Assert
            Assert.Equal(content, responseString);
            Assert.Equal(System.Net.HttpStatusCode.PreconditionFailed, responseCode);
        }

        public async Task GetDefaultRequestWithResponses(String requestPath, String responsePath, 
            String content)
        {
            // Act
            var text = new StringContent(content);
            var request = await _client.PostAsync(requestPath, text);

            request.EnsureSuccessStatusCode();
            text = new StringContent(content);
            var response = await _client.PostAsync(responsePath, text);

            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;

            // Assert
            Assert.Equal(content, responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);
        }

        public async Task GetDefaultJsonWithResponses(String responsePath,
    String content, String requestPath = "api/123/set/scopes")
        {
            // Act
            var text = new StringContent(content);
            text.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var request = await _client.PostAsync(requestPath, text);

            request.EnsureSuccessStatusCode();
            text = new StringContent(content);
            var response = await _client.PostAsync(responsePath, text);

            var responseString = await response.Content.ReadAsStringAsync();
            System.Net.HttpStatusCode responseCode = response.StatusCode;
            SimulatorRequest jsonString = JsonConvert.DeserializeObject<SimulatorRequest>(content);

            // Assert
            Assert.Equal(jsonString.Json["json"].ToString(), responseString);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseCode);

            response = await _client.GetAsync("/");
            var responseSt = await response.Content.ReadAsStringAsync();
            string jsonSt = JsonConvert.SerializeObject(_responses, Formatting.None, settings);
        }


        #endregion
        [Fact]
        public async Task GetVersion()
        {
            var res = typeof(Monetco.Host.Program).GetTypeInfo()
                .Assembly.GetName().Version.ToString();
            var response = await _client.GetAsync("/api/version");
            response.EnsureSuccessStatusCode();
            System.Reflection.Assembly assembly = typeof(Monetco.Host.Program).GetTypeInfo()
                .Assembly;
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(assembly.Location);
            DateTime lastModified = fileInfo.LastWriteTime;
            
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("version " + res + ", by " + lastModified, responseString);
        }
    }
}
