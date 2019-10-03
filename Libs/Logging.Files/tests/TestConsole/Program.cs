using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSW.Logging.Files;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddLogging(cfg => cfg.AddFileLogger(o=>o.LogLevel = LogLevel.Debug ));
            using var provider = services.BuildServiceProvider();

            var loggerFactory = provider.GetService<ILoggerProvider>();

            var log = loggerFactory.CreateLogger(nameof(Program));

            log.LogInformation("Hello!");
            log.LogError("Error here!");
            log.LogCritical(new Exception("Critical problem "), "Critical problem occured!");
            log.LogDebug("We just debug");

            //Console.ReadLine();
        }
    }
}
