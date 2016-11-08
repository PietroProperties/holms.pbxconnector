using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Support.Test {

    public class TestTCPServer {
        private readonly TcpListener _tcpListener;
        private readonly ILogger _logger;
        private TcpClient _client;

        public TestTCPServer(ILogger log, int port) {
            _logger = log;
            _tcpListener = new TcpListener(IPAddress.Any, port);
        }

        public async Task ConnectToClient() {
            _client = await Task.Run(() => {
                _tcpListener.Start();
                return _tcpListener.AcceptTcpClient();
            });
        }

        public byte[] ReadIncomingBuffer() {
            var b = new byte[1024];
            _client.Client.Receive(b);
            return b;
        }

        public void WriteToClient(byte[] b) {
            _client.Client.Send(b);
        }

        public void Teardown() {
            _tcpListener.Stop();
        }
    }
}
