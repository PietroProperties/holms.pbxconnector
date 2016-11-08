using System;
using HOLMS.Messaging;
using log4net;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Support {
    public class RegistryConfigurationProvider : IRegistryConfigurationProvider {
        private const string PBXPrefix = "pbx://";
        private const string TCPProtocol = "tcp:";
        private const string SerialProtocol = "serial:";
        private const string NoProtocol = "none";

        public string RabbitHost { get; }
        public string AppSvcHostname { get; }
        public ushort AppSvcPort { get; }

        public PBXConfiguration PMSConnection { get; }
        public PBXConfiguration SMDRConnection { get; }
        
        public string Username { get; }
        public string Password { get; }
        public Guid ClientInstanceId { get; }

        public RegistryConfigurationProvider(ILogger l) {
            var appSvcIpPort = NativeMethods.GetStringRegistryEntry("AppSvcIPPort");
            var tokens = appSvcIpPort.Split(":".ToCharArray());
            RabbitHost = NativeMethods.GetStringRegistryEntry("RabbitHost");

            if (tokens == null || tokens.Length != 2) {
                throw new ArgumentException("Missing or incorrect application server hostname and port");
            }

            ushort appSvcPort;
            AppSvcHostname = tokens[0];
            if (!ushort.TryParse(tokens[1], out appSvcPort)) {
                l.LogError($"Failed to parse AppSvcIPPort TCP port {tokens[1]} as valid port number (integer between 1 and 65535");
                throw new ArgumentException();
            }
            AppSvcPort = appSvcPort;

            var smdrConnectionString = NativeMethods.GetStringRegistryEntry("SMDRConnectionString");
            SMDRConnection = BuildConfiguration(l, smdrConnectionString);
            var pmsConnectionString = NativeMethods.GetStringRegistryEntry("PMSConnectionString");
            PMSConnection = BuildConfiguration(l, pmsConnectionString);

            Username = NativeMethods.GetStringRegistryEntry("ServiceUsername");
            Password = NativeMethods.GetStringRegistryEntry("ServicePassword");

            ClientInstanceId = NativeMethods.GetWindowsMachineGuid();
        }

        public IMessageConnectionFactory BuildMCF(ILogger logger) {
            return new MessageConnectionFactory(logger, RabbitHost);
        }

        //https://shortbar.atlassian.net/wiki/display/PMC/HOLMS+PBX+Service
        public static PBXConfiguration BuildConfiguration(ILogger l, string pbxConnectionString) {
            var config = new PBXConfiguration();
            try {
                var protocolString = pbxConnectionString.Substring(PBXPrefix.Length);

                // e.g. pbx://tcp:192.168.0.1:5400
                if (protocolString.StartsWith(TCPProtocol)) {
                    config.TCPEnabled = true;
                    ushort tcpPort;
                    var ipPort = protocolString.Substring(TCPProtocol.Length);
                    var lastColon = ipPort.LastIndexOf(':');
                    config.TCPHost = ipPort.Substring(0, lastColon);
                    var port = ipPort.Substring(lastColon + 1);
                    if (!ushort.TryParse(port, out tcpPort)) {
                        l.LogError($"Failed to parse PBXConnectionString port {tcpPort} as valid port number (integer between 1 and 65535");
                        throw new ArgumentException();
                    }
                    config.TCPPort = tcpPort;
                }
                // e.g. pbx://serial:COM1
                else if (protocolString.StartsWith(SerialProtocol)) {
                    config.SerialEnabled = true;
                    config.SerialPort = protocolString.Substring(SerialProtocol.Length);
                }
                // e.g. pbx://none
                else if (protocolString.StartsWith(NoProtocol)) {
                }
                else {
                    l.LogError($"PBXConnectionString {pbxConnectionString} does not specify a valid connection.");
                    throw new ArgumentException();
                }
                return config;
            }
            catch (ArgumentOutOfRangeException) {
                l.LogError($"PBXConnectionString {pbxConnectionString} does not specify a valid connection.");
                throw new ArgumentException();
            }
        }
    }

    public struct PBXConfiguration {
        public string TCPHost;
        public ushort TCPPort;
        public string SerialPort;
        public bool TCPEnabled;
        public bool SerialEnabled;
    }
}
