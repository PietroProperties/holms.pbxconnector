using HOLMS.Application.Client;
using HOLMS.Messaging;
using HOLMS.PBXConnector.Support;
using HOLMS.PBXConnector.Transport;
using HOLMS.PBXConnector.Transport.Serial;
using HOLMS.PBXConnector.Transport.TCP;
using System;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Protocol {
    internal abstract class PBXParser {
        protected readonly ILogger Log;
        protected readonly IMessageConnectionFactory CF;
        protected readonly IApplicationClient AC;
        protected ITransport Transport;
        protected ByteStreamLexer Lexer;
        protected IMessageConnection CN;
        protected IMessageChannel CH;

        protected abstract string ProtocolName { get; }
        protected abstract void ParseLine(object sender, string line);

        protected PBXParser(PBXConfiguration config, ILogger log, IMessageConnectionFactory cf, IApplicationClient ac) {
            Log = log;
            CF = cf;
            AC = ac;
            Transport = GetConfiguredTransport(log, config);
            Transport.DataArrived += TransportDataArrived;
        }

        private void TransportDataArrived(object sender, TransportDataEventArgs e) {
            Lexer.Lex(e.Data, e.Count);
        }

        protected void Error(object sender, byte errorByte, string lineBufferContents) {
            Log.LogWarning($"Lexer was reset after bad line. Last byte received: 0x{errorByte.ToString("X2")}. Line buffer contents: {lineBufferContents}");
        }

        protected void BindEventHandlers() {
            Lexer.LineReceived += ParseLine;
            Lexer.Error += Error;
        }

        public void Start() {
            Log.LogInformation($"{ProtocolName}: Starting");
            Log.LogInformation($"{ProtocolName}: Connecting to rabbitmq (host: {CF.Hostname})...");
            StartRabbitMQ();

            Log.LogInformation($"{ProtocolName}: Waiting for input lines...");
            Transport.BlockingReadData();
        }

        public void Stop() {
            Log.LogInformation($"{ProtocolName}: received stop signal, cleaning up");
            Transport.Stop();
            CN.Close();
        }

        protected void StartRabbitMQ() {
            CN = CF.OpenConnection();
            CH = CN.GetChannel();
        }

        protected void RegisterLexer(ByteStreamLexer lexer) {
            Lexer = lexer;
            BindEventHandlers();
        }

        private ITransport GetConfiguredTransport(ILogger log, PBXConfiguration config) {
            if (config.TCPEnabled) {
                // This part reads via a tcp connection and pumps decoded bytes into a line lexer
                // This part takes whole lines from the lexer, parses them, and publishes 
                // messages describing the call traffic to RabbitMQ
                Log.LogInformation($"{ProtocolName} configured for TCP at {config.TCPHost}:{config.TCPPort}");
                return new TCPTransport(config.TCPHost, config.TCPPort, Log);
            }
            else if (config.SerialEnabled) {
                // This part reads the serial port and pumps decoded bytes into a line lexer
                // This part takes whole lines from the lexer, parses them, and publishes 
                // messages describing the call traffic to RabbitMQ
                Log.LogInformation($"{ProtocolName} configured for Serial on port {config.SerialPort}");
                return new SerialTransport(Log, config);
            }
            log.LogError("Configuration with no protocols enabled was used to start a PBX parser");
            throw new ArgumentException();
        }
    }
}
