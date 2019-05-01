using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using Monetco.Host.Misc;
using Monetco.Host.Domain;
using Microsoft.Extensions.Options;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Monetco.Controllers
{
    /// <summary>
    /// Service controller
    /// </summary>
    [Route("api/[controller]")]
    public class ManageController : Controller
    {
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly XmlCommands _commands;

        /// <summary>
        /// Service controller
        /// </summary>
        /// <param name="applicationLifetime">Need for allication stoping</param>
        /// <param name="configuration">XML commands from config</param>
        public ManageController(IApplicationLifetime applicationLifetime, 
            IOptions<XmlCommands> configuration)
        {
            _applicationLifetime = applicationLifetime;
            _commands = configuration.Value;
        }

        /// <summary>
        /// Stop 
        /// </summary>
        [HttpGet("stop")]
        public void StopHost()
        {
            byte[] data = Encoding.UTF8.GetBytes("Shutdown!");
            Response.ContentType = "text/html";
            Response.Body.Write(data, 0, data.Length);
            _applicationLifetime.StopApplication();
        }

        /// <summary>
        /// Execute windows cmd.exe command
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <returns>Status code</returns>
        [HttpPost("batch")]
        public int ExecuteBatch([FromBody]string command)
        {
            var commandName = "C:\\Windows\\system32\\cmd.exe";
            return Utils.Execute(commandName, $"/C {command}");
        }

        /// <summary>
        /// Execute bash command
        /// </summary>
        /// <param name="command"></param>
        /// <returns>Status code</returns>
        [HttpPost("bash")]
        public int ExecuteBash([FromBody]string command)
        {
            var escapedArgs = command.Replace("\"", "\\\"");
            var commandName = "/bin/bash";
            return Utils.Execute(commandName, $"-c {escapedArgs}");
        }

        /// <summary>
        /// Executed command from appsettings
        /// </summary>
        /// <param name="id">Command id</param>
        /// <returns>Message from appsettings.json</returns>
        [HttpGet("command/{id}")]
        public string RunCommand([FromRoute]string id)
        {
            var command = _commands.Commands.Find(c => c.Id == id);
            if (command == null)
            {
                return $"Command with id {id} not found";
            }
            string args = "";
            var commandName = command.Process;
            string exitCode = "";

            if (command.OS == OS.Windows)
            {
                if (string.IsNullOrEmpty(command.Process) || command.Process.Contains("cmd"))
                {
                    commandName = "C:\\Windows\\system32\\cmd.exe";
                    args += "/C ";
                }
                args += command.Arguments;
                exitCode = Utils.ExecuteWithError(commandName, args);
            } else
            {
                if (string.IsNullOrEmpty(command.Process) || command.Process.Contains("bash"))
                {
                    commandName = "/bin/bash";
                    args += "-c ";
                }
                args = command.Arguments.Replace("\"", "\\\"");
                exitCode = Utils.ExecuteWithError(commandName, args);
            }
            if (string.IsNullOrEmpty(exitCode))
            {
                exitCode = "SUCCESS";
            }
            return $"Command {command.Id} executed with result: {exitCode} Message: {command.Message}";
        }

        /// <summary>
        /// Get commands list
        /// </summary>
        /// <returns>XmlCommands from appsettings.json</returns>
        [HttpGet("command/all")]
        public XmlCommands GetCommands()
        {
            return _commands;
        }
    }
}
