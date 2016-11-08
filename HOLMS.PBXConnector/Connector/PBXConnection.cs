using HOLMS.PBXConnector.Support;
using System.Threading;
using System;
using HOLMS.Types.IAM.RPC;
using HOLMS.Application.Client;
using HOLMS.PBXConnector.Protocol.SMDR;
using HOLMS.PBXConnector.Protocol.PMS;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Connector {
    public class PBXConnection {
        // Basic primitives
        private readonly ILogger _log;
        private readonly IRegistryConfigurationProvider _config;
        private readonly IApplicationClient _ac;

        // This responds to "are you alive" messages from the application server delivered via
        // RabbitMQ. It is always enabled (no configuration possible to disable it) and runs
        // on a thread, basically just echoing back the server's "ping cookie" ASAP
        private PingResponder _pingResponder;
        private Thread _pingResponderThread;

        // The next two sections are independent, and enabled via configuration

        // This part handles RS-232 SMDR protocol
        private SMDRParser _smdrParser;
        private Thread _smdrParserThread;
        
        // This part handles the "Property Management System Protocol" (PMS Protocol)
        private PMSParser _pmsParser;
        private Thread _pmsParserThread;
        
        public PBXConnection(ILogger log, IRegistryConfigurationProvider config, IApplicationClient ac) {
            _log = log;
            _config = config;
            _ac = ac;
        }

        public void Start() {
            _log.LogInformation("Attempting Connection to HOLMS Server");
            TryLogin();
            _log.LogInformation("Successful login to HOLMS Server");
            
            var mcf = _config.BuildMCF(_log);

            _pingResponder = new PingResponder(_log, mcf);
            _pingResponderThread = new Thread(_pingResponder.Start);
            _pingResponderThread.Start();

            if (_config.SMDRConnection.SerialEnabled || _config.SMDRConnection.TCPEnabled) {
                _log.LogInformation("Starting SMDR Listener");
                _smdrParser = new SMDRParser(_config.SMDRConnection, _log, mcf, _ac);
                _smdrParserThread = new Thread(_smdrParser.Start);
                _smdrParserThread.Start();
            } else {
                _log.LogInformation("Configuration disables SMDR protocol connector; not starting");
            }

            if (_config.PMSConnection.TCPEnabled || _config.PMSConnection.SerialEnabled) {
                _log.LogInformation($"Starting PMS Listener");
                _pmsParser = new PMSParser(_config.PMSConnection, _log, mcf, _ac);
                _pmsParserThread = new Thread(_pmsParser.Start);
                _pmsParserThread.Start();
            } else {
                _log.LogInformation("Configuration disables PMS protocol connector; not starting");
            }
        }

        private void TryLogin() {
            try {
                var ssr = _ac.StartSession(_config.Username, _config.Password);
                switch (ssr.Result) {
                    case SessionSvcStartSessionResult.Success:
                        break;
                    case SessionSvcStartSessionResult.Failure:
                        _log.LogError("Invalid login credentials");
                        throw new ArgumentException("Invalid login credentials");
                    case SessionSvcStartSessionResult.Inactive:
                        _log.LogError("Inactive user");
                        throw new ArgumentException("Inactive user");
                }
            } catch (Exception ex) {
                _log.LogError("Could not connect to application server", ex);
                throw new Exception("Could not connect to application server");
            }
        }

        public void Stop() {
            _log.LogInformation("Stopping PBXConnector service");
            _pingResponder?.Stop();

            _smdrParserThread?.Join();
            _pingResponderThread?.Join();
            _pmsParserThread?.Join(5);

            _smdrParser = null;
            _pingResponder = null;
            _smdrParserThread = _pmsParserThread = _pingResponderThread = null;
            _log.LogInformation("PBX connector shutdown complete");
        }
    }
}
