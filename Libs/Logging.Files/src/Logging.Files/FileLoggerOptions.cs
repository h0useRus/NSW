using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace NSW.Logging.Files
{
    public class FileLoggerOptions
    {
        string _folder;
        string _fileName;
        int _maxFileSizeInMB;
        int _retainPolicyFileCount;
 
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public LogTemplate Template { get; set; } = LogTemplate.Application;

        public bool AlwaysAddExceptions { get; set; } = true;

        public string Folder
        {
            get => !string.IsNullOrWhiteSpace(_folder) ? _folder : System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            set => _folder = value;
        }

        public string FileName
        {
            get => !string.IsNullOrWhiteSpace(_fileName) ? _fileName : Assembly.GetExecutingAssembly().GetName().Name;
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