using System;
using System.Diagnostics;
using System.IO;
using HOLMS.PBXConnector.Support;
using HOLMS.PBXConnector.Connector;
using HOLMS.Platform.Client;
using HOLMS.Platform.Support.Time;
using Microsoft.Extensions.Logging;

/**
 * Note (DL 9/5/16): This class is designed for debugging purposes only. This does not contain a proper shutdown mechanism,
 * and so is not safe to use in production environments.
 */
namespace HOLMS.PBXConnector.ConsoleRunner {
    class Program {
        private const string GrpcDefaultSslRootsEnvVar = "GRPC_DEFAULT_SSL_ROOTS_FILE_PATH";

        static void Main(string[] args) {
            ConfigureGrpcTrust();

            var log = GetProductionLogger();
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var c = new RealClock();
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
            var connector = new PBXConnection(log, config, ac, c);
            connector.Start();
        }

        private static ILogger GetProductionLogger() {
            var lf = new LoggerFactory();
            lf.AddConsole();

            return lf.CreateLogger("HOLMS.PBXConnector.ConsoleRunner");
        }

        private static void ConfigureGrpcTrust() {
            var standardRootCaBundlePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "HOLMS",
                "Certificates",
                "holms-root-ca.pem");

            if (!File.Exists(standardRootCaBundlePath)) {
                return;
            }

            Environment.SetEnvironmentVariable(
                GrpcDefaultSslRootsEnvVar,
                standardRootCaBundlePath,
                EnvironmentVariableTarget.Process);
        }
    }
}
