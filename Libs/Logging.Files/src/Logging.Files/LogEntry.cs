using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NSW.Logging.Files
{
    public class LogEntry
    { 
        public LogEntry()
        {
            TimeStampUtc = DateTime.UtcNow;
            UserName = Environment.UserName;
        }
 
        public static string StaticHostName { get; } = System.Net.Dns.GetHostName();
 
        public string UserName { get; }
        public string HostName => StaticHostName;
        public DateTime TimeStampUtc { get; }
        public string Category { get; set; }
        public LogLevel Level { get; set; }
        public string Text { get; set; }
        public Exception Exception { get; set; }
        public EventId EventId { get; set; }
        public object State { get; set; }
        public string StateText { get; set; }
        public Dictionary<string, object> StateProperties { get; set; }
        public List<LogScopeInfo> Scopes { get; set; }
    }

    public class LogScopeInfo
    { 
        public string Text { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}