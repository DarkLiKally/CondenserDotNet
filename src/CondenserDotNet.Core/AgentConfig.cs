using System;
using System.Collections.Generic;
using System.Text;

namespace CondenserDotNet.Core
{
    public class AgentConfig
    {
        public string Address { get; set; } = "localhost";
        public string Scheme { get; set; } = "http";
        public int Port { get; set; } = 8500;
        public string AccessControlToken { get; set; } = null;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(6);
    }
}
