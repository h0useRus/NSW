using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace NSW.Logging.Files
{
    public abstract class LoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
        private IExternalScopeProvider _scopeProvider;
        protected IDisposable SettingsChangeToken;
 
        void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        ILogger ILoggerProvider.CreateLogger(string category) =>
            _loggers.GetOrAdd(category, c => new FileLogger(this, c));

        void IDisposable.Dispose()
        {
            if (!IsDisposed)
            {
                try
                {
                    Dispose(true);
                }
                catch
                {
                    // ignored
                }

                IsDisposed = true;
                GC.SuppressFinalize(this);  // instructs GC not bother to call the destructor   
            }
        }
 
        protected virtual void Dispose(bool disposing)
        {
            if (SettingsChangeToken != null)
            {
                SettingsChangeToken.Dispose();
                SettingsChangeToken = null;
            }
        }

        ~LoggerProvider()
        {
            if (!IsDisposed)
            {
                Dispose(false);
            }
        }
 
        public abstract bool IsEnabled(LogLevel logLevel);
 
        public abstract void WriteLog(LogEntry info);
 
        internal IExternalScopeProvider ScopeProvider => _scopeProvider ??= new LoggerExternalScopeProvider();

        public bool IsDisposed { get; protected set; }
    }
}