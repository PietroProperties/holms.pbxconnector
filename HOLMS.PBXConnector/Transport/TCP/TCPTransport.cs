using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Transport.TCP {
    //Code taken from https://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.110).aspx

    public class TCPTransport : ITransport {
        private readonly ILogger _log;
        private readonly ushort _port;
        private readonly IPAddress _ipAddress;

        private Socket _client;
        private int _timeoutSeconds = 1;
        private bool _running = true;

        private const int MillisecondsPerSecond = 1000;

        public event EventHandler<TransportDataEventArgs> DataArrived;

        public TCPTransport(string ipAddress, ushort port, ILogger log) {
            _port = port;
            _log = log;
            _ipAddress = IPAddress.Parse(ipAddress);
        }
        
        private void WaitAndDestroyBrokenSocket() {
            _log.LogWarning($"Disconnected. Waiting {_timeoutSeconds} seconds before reconnection attempt.");
            Thread.Sleep(MillisecondsPerSecond * _timeoutSeconds);
            //Cap exponential backoff at 180s
            _timeoutSeconds = Math.Min(180, _timeoutSeconds * 2);
            if (_client.Connected) {
                // Destroy the socket
                _client.Shutdown(SocketShutdown.Both);
                _client.Disconnect(true);
            }
        }

        public void Stop() {
            _running = false;
            // Destroy the socket
            _client?.Shutdown(SocketShutdown.Both);
            _client?.Close();
        }

        public void Send(byte[] bytes) {
            _client.Send(bytes);
        }

        public void BlockingReadData() {
            // Data buffer for incoming data. Used in example on MSDN - see link at top of class
            byte[] bytes = new byte[1024];
            while (_running) {
                try {
                    // Create a TCP/IP socket.
                    _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var pbxEndpoint = new IPEndPoint(_ipAddress, _port);
                    _client.Connect(pbxEndpoint);
                    _log.LogInformation($"Socket connected to {_client.RemoteEndPoint}");

                    // Encode the data string into a byte array.
                    while (_client.Connected) {
                        int bytesRec = _client.Receive(bytes);
                        DataArrived?.Invoke(this, new TransportDataEventArgs(bytes, bytesRec));
                        //_log.LogDebug($"Received message: {Encoding.ASCII.GetString(bytes, 0, bytesRec)}");
                    }
                }
                catch (SocketException ex) {
                    _log.LogWarning($"Unexpected Socket Disconnection: {ex}");
                }
                catch (Exception ex) {
                    _log.LogWarning($"Unexpected Exception: {ex}");
                }
                WaitAndDestroyBrokenSocket();
            }
        }
    }
}
