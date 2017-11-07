using System;
using HOLMS.Messaging;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Support {
    public class PBXApplicationClientConfigurationProvider : IPBXApplicationConfiguration {
        public string RabbitHost { get; }
        public string AppSvcHostname { get; }
        public ushort AppSvcPort { get; }
        public string Username { get; }
        public string Password { get; }

        public PBXConfiguration PMSConnection { get; }
        public PBXConfiguration SMDRConnection { get; }

        public Guid ClientInstanceId { get; }
        
        public PBXApplicationClientConfigurationProvider(IRegistryConfigurationProvider config) {
            RabbitHost = config.RabbitHost;
            AppSvcHostname = config.AppSvcHostname;
            AppSvcPort = config.AppSvcPort;
            PMSConnection = config.PMSConnection;
            SMDRConnection = config.SMDRConnection;
            Username = config.Username;
            Password = config.Password;
            ClientInstanceId = config.ClientInstanceId;
        }

        public IMessageConnectionFactory BuildMCF(ILogger logger) {
            return new MessageConnectionFactory(logger,
                new MessagingConfiguration($"amqp://{RabbitHost}:5672"));
        }
    }
}
