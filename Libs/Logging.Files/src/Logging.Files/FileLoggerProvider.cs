using System;
using System.Collections.Concurrent;
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
        private long _counter = 0;
        private string _filePath;
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

        private void BeginFile()
        {
            Directory.CreateDirectory(Settings.Folder);
            _filePath = Path.Combine(Settings.Folder, Settings.FileName + "-" + DateTime.Now.ToString("yyMMdd-HHmmss") + ".log");

            if (Settings.AddTitles)
            {
                var sb = new StringBuilder();
                foreach (var column in Settings.Template)
                {
                    sb.Append(Pad(column.Value.Name, column.Value.Size));
                }

                sb.AppendLine("Text");
                File.WriteAllText(_filePath, sb.ToString());
            }
            ApplyRetainPolicy();
        }

        private void WriteLogLine()
        {
            if (_infoQueue.TryDequeue(out var info))
            {
                var sb = new StringBuilder();

                foreach (var column in Settings.Template)
                {
                    sb.Append(Pad(column.Value.Formatter(info), column.Value.Size));
                }
                
                string text = info.Text;

                if (info.Exception != null && Settings.AlwaysAddExceptions)
                {
                    text += $" Exception: {info.Exception}";
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