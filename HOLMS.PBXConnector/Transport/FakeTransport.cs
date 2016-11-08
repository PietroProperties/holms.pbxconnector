
using System;

namespace HOLMS.PBXConnector.Transport {
    class FakeTransport : ITransport {
        public event EventHandler<TransportDataEventArgs> DataArrived;

        public void BlockingReadData() {
            throw new NotImplementedException();
        }

        public void Send(byte[] bytes) {
            
        }

        public void Stop() {
            throw new NotImplementedException();
        }
    }
}
