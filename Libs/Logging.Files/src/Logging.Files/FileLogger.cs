using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NSW.Logging.Files
{
    internal class FileLogger : ILogger
    {
        public LoggerProvider Provider { get; }
        public string Category { get; }

        public FileLogger(LoggerProvider provider, string category)
        {
            Provider = provider;
            Category = category;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => Provider.ScopeProvider.Push(state);

        bool ILogger.IsEnabled(LogLevel logLevel) => Provider.IsEnabled(logLevel);

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if ((this as ILogger).IsEnabled(logLevel))
            {
                var info = new LogEntry
                {
                    Category = Category,
                    Level = logLevel,
                    Text = formatter != null ? formatter(state, exception) : (exception?.Message ?? state?.ToString()),
                    Exception = exception,
                    EventId = eventId,
                    State = state
                };

                switch (state)
                {
                    // well, you never know what it really is
                    case string str:
                        info.StateText = str;
                        break;
                    case IEnumerable<KeyValuePair<string, object>> properties:
                    {
                        info.StateProperties = new Dictionary<string, object>();

                        foreach (var item in properties)
                        {
                            info.StateProperties[item.Key] = item.Value;
                        }

                        break;
                    }
                }

                // gather info about scope(s), if any
                Provider.ScopeProvider?.ForEachScope((value, loggingProps) =>
                    {
                        if (info.Scopes == null)
                            info.Scopes = new List<LogScopeInfo>();

                        var scope = new LogScopeInfo();
                        info.Scopes.Add(scope);

                        if (value is string)
                        {
                            scope.Text = value.ToString();
                        }
                        else if (value is IEnumerable<KeyValuePair<string, object>> props)
                        {
                            if (scope.Properties == null)
                                scope.Properties = new Dictionary<string, object>();

                            foreach (var pair in props)
                            {
                                scope.Properties[pair.Key] = pair.Value;
                            }
                        }
                    }, state);

                Provider.WriteLog(info);
            }
        }
    }
}