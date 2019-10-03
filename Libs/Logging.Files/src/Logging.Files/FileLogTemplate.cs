using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace NSW.Logging.Files
{
    public class FileLogTemplate : IReadOnlyDictionary<string, FileLogColumn>
    {
        private readonly Dictionary<string, FileLogColumn> _columns = new Dictionary<string, FileLogColumn>();

        public void AddColumn(string name, int size, Func<LogEntry, string> formatter)
        {
            if(string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if(size<=0) throw new ArgumentOutOfRangeException(nameof(size));
            if(formatter == null) throw new ArgumentNullException(nameof(formatter));

            _columns[name] = new FileLogColumn {Name = name, Size = size, Formatter = formatter};
        }

        public static FileLogTemplate Application
        {
            get
            {
                var template = new FileLogTemplate();
                template.AddColumn("Time", 24, e => e.TimeStampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
                template.AddColumn("Level", 6, e => ToShortString(e.Level));
                template.AddColumn("EventId", 10, e => e.EventId.ToString());
                template.AddColumn("Category", 30, e => e.Category);
                return template;
            }
        }

        public static FileLogTemplate Service
        {
            get
            {
                var template = new FileLogTemplate();
                template.AddColumn("Time", 24, e => e.TimeStampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
                template.AddColumn("Host", 16, e => e.HostName);
                template.AddColumn("User", 16, e => e.UserName);
                template.AddColumn("Level", 6, e => ToShortString(e.Level));
                template.AddColumn("EventId", 10, e => e.EventId.ToString());
                template.AddColumn("Scope", 64, e =>
                {
                    var str = string.Empty;
                    if (e.Scopes != null && e.Scopes.Count > 0)
                    {
                        var si = e.Scopes.Last();
                        if (!string.IsNullOrWhiteSpace(si.Text))
                        {
                            str = si.Text;
                        }
                    }
                    return str;
                });
                template.AddColumn("Category", 30, e => e.Category);
                return template;
            }
        }

        private static string ToShortString(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.Critical    => "crit",
                LogLevel.Debug       => "debug",
                LogLevel.Error       => "error",
                LogLevel.Information => "info",
                LogLevel.Trace       => "trace",
                LogLevel.Warning     => "warn",
                _                    => "none"
            };

        public IEnumerator<KeyValuePair<string, FileLogColumn>> GetEnumerator() => _columns.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _columns.Count;
        public bool ContainsKey(string key) => _columns.ContainsKey(key);

        public bool TryGetValue(string key, out FileLogColumn value) => _columns.TryGetValue(key, out value);

        public FileLogColumn this[string key] => _columns[key];

        public IEnumerable<string> Keys => _columns.Keys;
        public IEnumerable<FileLogColumn> Values => _columns.Values;
    }
}