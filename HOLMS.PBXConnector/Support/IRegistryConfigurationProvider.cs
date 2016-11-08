using HOLMS.Messaging;
using System;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Support {
    public interface IRegistryConfigurationProvider {
        string AppSvcHostname { get; }
        ushort AppSvcPort { get; }
        string RabbitHost { get; }
        string Username { get; }
        string Password { get; }

        PBXConfiguration PMSConnection { get; }
        PBXConfiguration SMDRConnection { get; }

        Guid ClientInstanceId { get; }
        IMessageConnectionFactory BuildMCF(ILogger logger);
    }
}