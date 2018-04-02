using System.Threading.Tasks;
using HOLMS.PBXConnector.Support.Test;
using NUnit.Framework;
using Moq;
using HOLMS.Types.PBXConnector;
using System.Text;
using System.Linq;
using HOLMS.Platform.Client;
using HOLMS.Platform.Support.Time;
using HOLMS.Platform.Types.Topics;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Connector {
    class PBXConnectorTCPTests {
        private FakeTCPConfiguration _lc;
        private Mock<ILogger> _log;

        private TestTCPServer _server;
        private PBXConnection _connector;

        [SetUp]
        public void Init() {
            _log = new Mock<ILogger>();
            _lc = new FakeTCPConfiguration();

            _server = new TestTCPServer(_log.Object, _lc.PMSConnection.TCPPort);
            _connector = new PBXConnection(_log.Object, _lc,
                new MockApplicationClient(new Mock<ILogger>().Object), new RealClock());
        }

        [TearDown]
        public void TearDown() {
            _connector.Stop();
            _server.Teardown();
        }

        private async Task StartConnection() {
            var connectionTask = _server.ConnectToClient();
            _connector.Start();
            await connectionTask;
        }

        [Test]
        public async Task TCPServerAcknowledgesEnquire() {
            await StartConnection();

            _server.WriteToClient(new byte[] { 0x05 });
            var response = _server.ReadIncomingBuffer();
            Assert.AreEqual(response[0], 0x06);
        }

        [Test]
        public async Task TCPServerPassesWellFormedLineToParser() {
            await StartConnection();

            _server.WriteToClient(new byte[] { 0x02, 0x6c, 0x6f, 0x6c, 0x6a, 0x6b, 0x03 });
        }

        [Test]
        public async Task ConnectorSendsRabbitMessageForRoomStatusChange() {
            await StartConnection();

            var statusString = "STS2 107  ";
            var byteArray = Encoding.ASCII.GetBytes(statusString);

            var fullArray = new byte[] { 0x02 }.Concat(byteArray).Concat(new byte[] { 0x03 }).ToArray();
            _server.WriteToClient(fullArray);

            await Task.Delay(100);   // Parser running on separate thread. Race condition may cause false negative

            var connections = _lc.FakeRCF.Connections;
            var publications = connections.SelectMany(c => c.GetChannels().SelectMany(ch => ch.Publications));
            Assert.AreEqual(1, publications.Count());
            var message = publications.First().Message as TCPRoomStatusMessage;
            Assert.AreEqual(PBXConnectorTopics.TcpRoomStatusTopic, publications.First().Routingkey);
            Assert.AreEqual(107.ToString(), message.TerminalIdentifier);
            Assert.AreEqual(2.ToString(), message.StatusCode);
        }
    }
}
