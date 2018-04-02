using HOLMS.Messaging;
using HOLMS.Platform.Client;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Support {
    public interface IPBXApplicationConfiguration : IApplicationClientConfig {
        string RabbitHost { get; }

        PBXConfiguration PMSConnection { get; }
        PBXConfiguration SMDRConnection { get; }

        string Username { get; }
        string Password { get; }
        IMessageConnectionFactory BuildMCF(ILogger logger);
    }
}
