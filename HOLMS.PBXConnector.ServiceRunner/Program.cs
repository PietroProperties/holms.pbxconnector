using System;
using System.IO;
using System.ServiceProcess;

namespace HOLMS.PBXConnector.ServiceRunner {
    static class Program {
        private const string GrpcDefaultSslRootsEnvVar = "GRPC_DEFAULT_SSL_ROOTS_FILE_PATH";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main() {
            ConfigureGrpcTrust();

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new PBXConnectorService()
            };
            ServiceBase.Run(ServicesToRun);
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
