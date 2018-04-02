using System;
using System.Diagnostics;
using System.ServiceProcess;
using HOLMS.PBXConnector.Support;
using HOLMS.PBXConnector.Connector;
using HOLMS.Platform.Client;
using HOLMS.Platform.Support.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace HOLMS.PBXConnector.ServiceRunner {
    public partial class PBXConnectorService : ServiceBase {
        private readonly ILogger _log;
        private readonly RealClock _c = new RealClock();

        private RegistryConfigurationProvider _config;
        private PBXConnection _connection;
        private ApplicationClient _ac;

        public PBXConnectorService() {
            ServiceName = "HOLMS.PBXConnector.ServiceRunner";
            _log = GetProductionLogger();
        }

        protected override void OnStart(string[] args) {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            _log.LogInformation($"Starting PBXConnector service {fvi.FileVersion}");

            try {
                _config = new RegistryConfigurationProvider(_log);
            } catch (Exception ex) {
                _log.LogError($"Error starting PBXConnectorService: {ex}");
                throw;
            }
            _log.LogInformation($"Completed Configuration Read");

            _ac = new ApplicationClient(new PBXApplicationClientConfigurationProvider(_config), 
                _log, "CJASDBYCOKYIWBWNFPQHOBGIQPEJUBSYNEOUEKJZTOSWWCPGCRWNYGBOOUZE");
            _connection = new PBXConnection(_log, _config, _ac, _c);
            _log.LogInformation("Initialized PBX Connection Object");
            _connection.Start();
        }

        protected override void OnStop() {
            _connection.Stop();
            _ac?.Dispose();
            _ac = null;
        }

        public static ILogger GetProductionLogger() {
            var lf = new LoggerFactory();
            var els = new EventLogSettings {
                LogName = "HOLMS",
                SourceName = "PBXConnector"
            };
            lf.AddEventLog(els);

            return lf.CreateLogger("Scheduler");
        }
    }
}
