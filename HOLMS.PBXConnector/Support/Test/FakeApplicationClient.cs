using HOLMS.Application.Client;
using HOLMS.Types.Primitive;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Support.Test {
    class FakeApplicationClient : ApplicationClient {
        public FakeApplicationClient(IApplicationClientConfig config, ILogger logger, string clientID) : base(config, logger, clientID) {
            SS = new SessionService(
                new SessionContext {
                    AccessToken = "FakeToken",
                });
        }
    }
}
