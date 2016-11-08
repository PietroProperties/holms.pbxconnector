using System.Text;

namespace HOLMS.PBXConnector.Protocol {
    public abstract class ByteStreamLexer {
        private const int BufferSize = 4096;

        private readonly byte[] _ringBuf;
        private int _stringHead;
        private int _stringLength;
        protected int LexerState;

        public delegate void LexerErrorArgs(object sender, byte errorByte, string lineBufferContents);
        public delegate void LexerLineReceivedArgs(object sender, string line);
        public event LexerErrorArgs Error;
        public event LexerLineReceivedArgs LineReceived;

        public ByteStreamLexer() {
            _ringBuf = new byte[BufferSize];
            _stringHead = 0;
            _stringLength = 0;
        }

        public virtual void Lex(byte[] b, int count) {
            for (int i = 0; i < count; ++i) {
                Lex(b[i]);
            }
        }

        public abstract void Lex(byte b);

        protected void AppendByteToLine(byte b) {
            _ringBuf[(_stringHead + _stringLength) % BufferSize] = b;
            ++_stringLength;
        }

        protected void ResetLine() {
            _stringHead = (_stringHead + _stringLength) % BufferSize;
            _stringLength = 0;
        }

        protected string GetLineBufferContents() {
            var receivedLine = new byte[_stringLength];
            for (int i = 0; i < _stringLength; ++i) {
                receivedLine[i] = _ringBuf[(_stringHead + i) % BufferSize];
            }

            return Encoding.ASCII.GetString(receivedLine);
        }

        protected void NotifyLineReceived() {
            LineReceived?.Invoke(this, GetLineBufferContents());
            ResetLine();
            LexerState = 0;
        }

        protected void NotifyErrorAndReset(byte rxByte) {
            Error?.Invoke(this, rxByte, GetLineBufferContents());
            ResetLine();
            LexerState = 0;
        }
    }
}
