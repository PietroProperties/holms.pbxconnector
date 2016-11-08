using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using log4net;

/**
 * Note (DL 9/6/16): This class is designed for debugging purposes only. 
 * DO NOT COPY AND REUSE THIS CODE - POOR SCALING AND PERFORMANCE.
 */
namespace HOLMS.PBX.MitelPBXSimulator.ConsoleRunner {
    //Currently based on https://web.archive.org/web/20090720052829/http://www.switchonthecode.com/tutorials/csharp-tutorial-simple-threaded-tcp-server

    public class TCPServer {
        private readonly TcpListener _tcpListener;
        private readonly Thread _listenThread;
        private readonly ILog _logger;

        public TCPServer(ILog log, int port) {
            _logger = log;
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _listenThread = new Thread(new ThreadStart(ListenForClients));
            _listenThread.Start();
        }

        private void ListenForClients() {
            _tcpListener.Start();

            while (true) {
                //This is a blocking call
                TcpClient client = _tcpListener.AcceptTcpClient();
                _logger.Info("Connected new TCP client");

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleTCPClient));
                clientThread.Start(client);
            }
        }

        private void HandleTCPClient(object client) {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            //Note (DL - 9/9/16) - DO NOT COPY THIS CODE. KNOWN BUFFER OVERFLOW VULNERABILITY
            byte[] message = new byte[4096];
            int bytesRead;

            while (true) {
                bytesRead = 0;

                try {
                    //This is a blocking call
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch (Exception ex) {
                    _logger.Error(ex);
                    break;
                }

                if (bytesRead == 0) {
                    break;
                }
                
                ASCIIEncoding encoder = new ASCIIEncoding();
                _logger.Info($"Received message from client {encoder.GetString(message, 0, bytesRead)}");
            }
            _logger.Info("TCP connection closed");
            tcpClient.Close();
        }

        private void WriteStringToClient(TcpClient client, string content) {
            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = encoder.GetBytes(content);

            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }
    }
}
