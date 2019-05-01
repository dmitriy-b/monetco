using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Monetco.Host.Domain
{
    
    public class XmlCommand
    {
        public string Id { get; set; }

        public string Process { get; set; }
        public string Arguments { get; set; }
        public OS OS { get; set; }
        public string Message { get; set; }
    }

    public class XmlCommands
    {
        public List<XmlCommand> Commands { get; set; }
    }

    public enum OS
    {
        Windows, Mac
    }
}
