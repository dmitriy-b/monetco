using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Monetco;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monetco.Host.Misc
{
    public class Utils
    {

        public static int Execute(string filename, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                FileName = filename,
                Arguments = arguments,
                RedirectStandardError = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    process.Start();
                    var s = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    if (!string.IsNullOrEmpty(s))
                    {
                        return -1;
                    }
                    return process.ExitCode;
                }

                catch (Exception exception)
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }

                    Console.WriteLine($"Cmd could not execute command {filename} {arguments}:\n{exception.Message}");
                    return process.ExitCode;
                }
            }
        }

        public static string ExecuteWithError(string filename, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                FileName = filename,
                Arguments = arguments,
                RedirectStandardError = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    process.Start();
                    var s = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    if (!string.IsNullOrEmpty(s))
                    {
                        return s;
                    }
                    return s;
                }

                catch (Exception exception)
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }

                    return $"Cmd could not execute command {filename} {arguments}:\n{exception.Message}";
                }
            }
        }

        public static string ReadStringFromRequest(HttpContext context)
        {
            string textResult = string.Empty;
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            if (!context.Request.HasFormContentType)
            {
                using (var mem = new MemoryStream())
                using (var reader = new StreamReader(mem))
                {
                    context.Request.Body.CopyTo(mem);

                    mem.Seek(0, SeekOrigin.Begin);

                    var body = reader.ReadToEnd();
                    return body;
                }

            }
            else
            {
                string form = "";
                var b = context.Request.Form.ToList();
                for (int i = 0; i < b.Count; i++)
                {
                    form += b.ElementAt(i).Key + "=" + b.ElementAt(i).Value;
                    if (i % 2 == 0 && i != b.Count - 1)
                    {
                        form += "&";
                    }
                }
                textResult = form;
                return textResult;
            }
        }

    }
}
