using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace NSW.Logging.Files
{
    public class FileLoggerOptions
    {
        private string _folder;
        private string _fileName;
        private int _maxFileSizeInMB;
        private int _retainPolicyFileCount;
 
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public LogTemplate Template { get; set; } = LogTemplate.Application;

        public bool AlwaysAddExceptions { get; set; } = true;

        public string Folder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_folder))
                    _folder = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Directory.GetCurrentDirectory();
                return _folder;
            }
            set => _folder = value;
        }

        public string FileName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_fileName))
                    _fileName = Assembly.GetEntryAssembly()?.GetName().Name ?? "log";
                return _fileName;
            }
            set => _fileName = value;
        }


        public int MaxFileSizeInMB
        {
            get => _maxFileSizeInMB > 0 ? _maxFileSizeInMB : 2;
            set => _maxFileSizeInMB = value;
        }
 
        public int RetainPolicyFileCount
        {
            get => _retainPolicyFileCount < 5 ? 5 : _retainPolicyFileCount;
            set => _retainPolicyFileCount = value;
        }
    }

    internal class FileLoggerOptionsSetup : ConfigureFromConfigurationOptions<FileLoggerOptions>
    {
        public FileLoggerOptionsSetup(ILoggerProviderConfiguration<FileLoggerProvider>  providerConfiguration)
            : base(providerConfiguration.Configuration)
        {
        }
    }
}