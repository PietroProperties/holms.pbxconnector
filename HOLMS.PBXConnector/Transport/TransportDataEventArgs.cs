using System;

namespace HOLMS.PBXConnector.Transport {
    public class TransportDataEventArgs : EventArgs {
        public readonly byte[] Data;
        public readonly int Count;
        public TransportDataEventArgs(byte[] data, int count) {
            Data = data;
            Count = count;
        }
    }
}
