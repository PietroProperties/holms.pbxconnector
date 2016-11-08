using System;

namespace HOLMS.PBXConnector.Transport {
    public interface ITransport {
        event EventHandler<TransportDataEventArgs> DataArrived;
        void Send(byte[] bytes);
        void BlockingReadData();
        void Stop();
    }
}
