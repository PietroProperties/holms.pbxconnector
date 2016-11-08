using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;

namespace HOLMS.PBX.MitelPBXSimulator.ConsoleRunner {
    static class Program {

        private const int PORT = 6060;

        static void Main() {
            Console.WriteLine("Starting HOLMS.PBX.MitelPBXSimulator");

            var log = GetProductionLogger();
            log.Info("Logging with log4net logger");

            var pbxServer = new TCPServer(log, PORT);
        }

        private static ILog GetProductionLogger() {
            var assembly = Assembly.GetExecutingAssembly();

            XmlConfigurator.Configure(
                new FileInfo(
                    Path.Combine(
                        Path.GetDirectoryName(assembly.Location),
                        "log4net.config"
                    )
                )
            );
            return LogManager.GetLogger("ConsoleAppender");
        }
    }
}
