using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NSW.Logging.Files
{
    [ProviderAlias("File")]
    public sealed class FileLoggerProvider : LoggerProvider
    {
        private readonly object _lock = new object();

        private bool _terminated;
        private int _counter = 0;
        private string _filePath;
        private readonly Dictionary<string, int> _lengths = new Dictionary<string, int>();
        private readonly ConcurrentQueue<LogEntry> _infoQueue = new ConcurrentQueue<LogEntry>();

        private void ApplyRetainPolicy()
        {
            try
            {
                var fileList = new DirectoryInfo(Settings.Folder)
                .GetFiles("*.log", SearchOption.TopDirectoryOnly)
                .OrderBy(fi => fi.CreationTime)
                .ToList();

                while (fileList.Count >= Settings.RetainPolicyFileCount)
                {
                    var fileInfo = fileList.First();
                    fileInfo.Delete();
                    fileList.Remove(fileInfo);
                }
            }
            catch
            {
                // ignored
            }
        }

        private void WriteLine(string text)
        {
            // check the file size after any 100 writes
            _counter++;
            if (_counter % 100 == 0)
            {
                var fileInfo = new FileInfo(_filePath);
                if (fileInfo.Length > (1024 * 1024 * Settings.MaxFileSizeInMB))
                {
                    BeginFile();
                }
            }

            File.AppendAllText(_filePath, text);
        }

        private static string Pad(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "".PadRight(maxLength);

            return text.Length > maxLength ? text.Substring(0, maxLength) : text.PadRight(maxLength);
        }

        private void PrepareLengths()
        {
            if (Settings.Template == LogTemplate.Service)
            {
                _lengths["Time"] = 24;
                _lengths["Host"] = 16;
                _lengths["User"] = 16;
                _lengths["Level"] = 6;
                _lengths["EventId"] = 32;
                _lengths["Category"] = 60;
                _lengths["Scope"] = 64;
            }
            else
            {
                _lengths["Time"] = 24;
                _lengths["Level"] = 6;
                _lengths["EventId"] = 10;
                _lengths["Category"] = 60;
            }
        }

        private void BeginFile()
        {
            Directory.CreateDirectory(Settings.Folder);
            var sourceName = Settings.Template == LogTemplate.Service ? LogEntry.StaticHostName : Settings.FileName;
            _filePath = Path.Combine(Settings.Folder, sourceName + "-" + DateTime.Now.ToString("yyMMdd-HHmmss") + ".log");

            // titles
            var sb = new StringBuilder();
            if (Settings.Template == LogTemplate.Service)
            {
                sb.Append(Pad("Time", _lengths["Time"]));
                sb.Append(Pad("Host", _lengths["Host"]));
                sb.Append(Pad("User", _lengths["User"]));
                sb.Append(Pad("Level", _lengths["Level"]));
                sb.Append(Pad("EventId", _lengths["EventId"]));
                sb.Append(Pad("Category", _lengths["Category"]));
                sb.Append(Pad("Scope", _lengths["Scope"]));
                sb.AppendLine("Text");
            }
            else
            {
                sb.Append(Pad("Time", _lengths["Time"]));
                sb.Append(Pad("Level", _lengths["Level"]));
                sb.Append(Pad("EventId", _lengths["EventId"]));
                sb.Append(Pad("Category", _lengths["Category"]));
                sb.AppendLine("Text");
            }
            File.WriteAllText(_filePath, sb.ToString());
            ApplyRetainPolicy();
        }

        private void WriteLogLine()
        {
            if (_infoQueue.TryDequeue(out var info))
            {
                var sb = new StringBuilder();
                sb.Append(Pad(info.TimeStampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"), _lengths["Time"]));
                if (Settings.Template == LogTemplate.Service)
                {
                    sb.Append(Pad(info.HostName, _lengths["Host"]));
                    sb.Append(Pad(info.UserName, _lengths["User"]));
                }
                sb.Append(Pad(ToShortString(info.Level), _lengths["Level"]));
                sb.Append(Pad(info.EventId.ToString(), _lengths["EventId"]));
                sb.Append(Pad(info.Category, _lengths["Category"]));

                if (Settings.Template == LogTemplate.Service && info.Scopes != null && info.Scopes.Count > 0)
                {
                    var str = "";
                    var si = info.Scopes.Last();
                    if (!string.IsNullOrWhiteSpace(si.Text))
                    {
                        str = si.Text;
                    }
                    sb.Append(Pad(str, _lengths["Scope"]));
                }

                string text = info.Text;

                if (info.Exception != null && Settings.AlwaysAddExceptions)
                {
                    text += info.Exception.ToString();
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.Append(text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " "));
                }

                sb.AppendLine();
                WriteLine(sb.ToString());
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

        private void ThreadProc() =>
            Task.Run(() =>
            {

                while (!_terminated)
                {
                    try
                    {
                        WriteLogLine();
                        System.Threading.Thread.Sleep(100);
                    }
                    catch 
                    {
                        // ignored
                    }
                }
            });


        private void Flush()
        {
            lock (_lock)
            {
                while (!_infoQueue.IsEmpty)
                {
                    WriteLogLine();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            _terminated = true;
            Flush();
            base.Dispose(disposing);
        }


        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> settings) : this(settings.CurrentValue)
        {
            SettingsChangeToken = settings.OnChange(s => { Settings = s; });
        }

        public FileLoggerProvider(FileLoggerOptions settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            PrepareLengths();
            BeginFile();
            ThreadProc();
        }

        public override bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None
            && Settings.LogLevel != LogLevel.None
            && Convert.ToInt32(logLevel) >= Convert.ToInt32(Settings.LogLevel);

        public override void WriteLog(LogEntry info) => _infoQueue.Enqueue(info);

        internal FileLoggerOptions Settings { get; private set; }
    }
}