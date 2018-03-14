using System;
using System.Diagnostics;
using HOLMS.PBXConnector.Support;
using HOLMS.PBXConnector.Connector;
using HOLMS.Application.Client;
using Microsoft.Extensions.Logging;

/**
 * Note (DL 9/5/16): This class is designed for debugging purposes only. This does not contain a proper shutdown mechanism,
 * and so is not safe to use in production environments.
 */
namespace HOLMS.PBXConnector.ConsoleRunner {
    class Program {
        static void Main(string[] args) {
            var log = GetProductionLogger();
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            log.LogInformation($"Starting PBXConnector Console Runner {fvi.FileVersion}");

            RegistryConfigurationProvider config;
            try {
                config = new RegistryConfigurationProvider(log);
            }
            catch (Exception ex) {
                log.LogError("Error starting PBXConnector Console Runner", ex);
                throw;
            }
            var ac = new ApplicationClient(
                new PBXApplicationClientConfigurationProvider(config),
                log, "CJASDBYCOKYIWBWNFPQHOBGIQPEJUBSYNEOUEKJZTOSWWCPGCRWNYGBOOUZE");
            var connector = new PBXConnection(log, config, ac);
            connector.Start();
        }

        private static ILogger GetProductionLogger() {
            var lf = new LoggerFactory();
            lf.AddConsole();

            return lf.CreateLogger("HOLMS.PBXConnector.ConsoleRunner");
        }
    }
}
