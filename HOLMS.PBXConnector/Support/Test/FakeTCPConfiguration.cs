using HOLMS.Messaging;
using HOLMS.Messaging.Tests;
using System;
using HOLMS.Platform.Client;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Support.Test {
    public class FakeTCPConfiguration : IRegistryConfigurationProvider, IApplicationClientConfig {
        FakeRabbitConnectionFactory _frcf = new FakeRabbitConnectionFactory();

        public FakeRabbitConnectionFactory FakeRCF => _frcf;

        public string RabbitHost => null;
        public string SerialPort => null;

        public PBXConfiguration PMSConnection => new PBXConfiguration {
            TCPEnabled = true,
            TCPHost = "127.0.0.1",
            TCPPort = 6060,
        };
        public PBXConfiguration SMDRConnection => new PBXConfiguration {
            SerialEnabled = false,
        };
        
        public string Username => "User";
        public string Password => "Password";

        public string AppSvcHostname => "127.0.0.1";
        public ushort AppSvcPort => 8080;

        public Guid ClientInstanceId => Guid.NewGuid();

        public IMessageConnectionFactory BuildMCF(ILogger logger) {
            return _frcf;
        }
    }
}
