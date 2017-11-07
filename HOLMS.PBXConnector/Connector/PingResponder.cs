using HOLMS.Messaging;
using HOLMS.Types.PBXConnector;
using System;
using HOLMS.Platform.Types.Topics;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Connector {
    public class PingResponder {
        private readonly IMessageConnectionFactory _cf;
        private readonly ILogger _log;
        private IMessageChannel _ch;
        private IMessageConnection _cn;
        private IMessageListener _ml;

        public PingResponder(ILogger log, IMessageConnectionFactory cf) {
            _log = log;
            _cf = cf;
        }

        public void Start() {
            _log.LogInformation("PingResponder Starting.");
            try {
                _log.LogInformation("Opening connection to RabbitMQ host.");
                _cn = _cf.OpenConnection();
                _ch = _cn.GetChannel();

                _ml = _ch.CreateListenerForTopics(OnMessage,
                    new[] {
                        PBXConnectorTopics.PbxPingRequest
                    });
                _ml.Start();
            }
            //Catching generic exception to not load dependencies from Messaging
            catch (Exception ex) {
                _log.LogError($"Failed to connect to RabbitMQ broker at [{_cf.Hostname}] with error [{ex.Message}]");
                throw;
            }
        }

        public void Stop() {
            _log.LogInformation("PingResponder: received stop signal, cleaning up...");

            _ml.Stop();
            _ml = null;

            _ch.Close();
            _ch = null;

            _cn.Close();
            _cn = null;
        }


        private void OnMessage(string routingKey, byte[] msg) {
            _log.LogInformation("Received ping request in pbx listener");
            var req = PingRequest.Parser.ParseFrom(msg);
            var resp = new PingResponse {
                PingCookie = req.PingCookie
            };

            _ch.Publish(PBXConnectorTopics.PbxPingResponse, resp);
        }
    }
}
