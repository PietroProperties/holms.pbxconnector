using System.IO;
using System.IO.Ports;
using HOLMS.PBXConnector.Support;
using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Transport.Serial {
    public class SerialTransport : ITransport {
        const int SerialReadBufSize = 4096;

        private readonly ILogger _log;
        private readonly PBXConfiguration _config;

        private SerialPort _sp;
        private volatile bool _stopRequested;

        public event EventHandler<TransportDataEventArgs> DataArrived;

        public SerialTransport(ILogger log, PBXConfiguration config) {
            _log = log;
            _config = config;
        }

        public void Stop() {
            _stopRequested = true;
            _log.LogInformation("Stopping serial transport");
            _sp.Close();
            _sp.Dispose();
            _log.LogInformation("Serial transport stopped");
        }

        private static SerialPort GetSerialPort(ILogger log, string portName) {
            log.LogInformation($"Creating serial port object. Requested port: {portName}");

            var ports = SerialPort.GetPortNames();
            log.LogInformation($"Found {ports.Length} serial ports on system. Names: {string.Join(" ", ports)}");

            var port = new SerialPort(portName) {
                // "1200-8-N-1"
                BaudRate = 1200,
                DataBits = 8,
                Handshake = Handshake.None, // no flow control
                StopBits = StopBits.One
            };

            return port;
        }

        public void Send(byte[] bytes) {
            _sp.Write(bytes, 0, bytes.Length);
        }

        public void BlockingReadData() {
            _sp = GetSerialPort(_log, _config.SerialPort);
            _log.LogInformation($"Opening port {_config.SerialPort}");
            _sp.Open();
            byte[] rawRxBuf = new byte[SerialReadBufSize];
            while (!_stopRequested) {
                try {
                    var bytesRead = _sp.Read(rawRxBuf, 0, rawRxBuf.Length);
                    if (bytesRead > 0) {
                        DataArrived?.Invoke(this, new TransportDataEventArgs(rawRxBuf, bytesRead));
                        //_log.LogDebug($"Received message: {Encoding.ASCII.GetString(rawRxBuf, 0, bytesRead)}");
                    }
                }
                catch (IOException) {
                    _log.LogInformation("Threaded serial lexer was stopped due to (expected) I/O exception");
                }
            }
        }
    }
}
