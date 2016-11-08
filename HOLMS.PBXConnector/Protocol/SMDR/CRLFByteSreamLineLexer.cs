using System;

namespace HOLMS.PBXConnector.Protocol.SMDR {
    /// <summary>
    /// Separates input into lines, delimited by CR LF (Windows line break convention).
    /// Buffers incoming bytes using a ring buffer. Fires LineReceived on receipt
    /// of a complete line (drops CR/LF delimiters) and Error on error.
    /// </summary>
    public class CRLFByteSreamLineLexer : ByteStreamLexer {
        
        public CRLFByteSreamLineLexer() : base() { }

        public override void Lex(byte b) {
            // Implement a simple lexer which splits the incoming text into lines
            // Based on observation, the mitel system writes 0x0d 0x0a between lines
            switch (LexerState) {
                case 0:
                    // Receiving mid-line
                    if (b == 0x0d) {
                        LexerState = 1;
                    } else if (b == 0x0a) {
                        NotifyErrorAndReset(b);
                    } else {
                        AppendByteToLine(b);
                    }
                    break;
                case 1:
                    // Received 0x0d, expecting 0x0a
                    if (b == 0x0a) {
                        NotifyLineReceived();
                    } else {
                        NotifyErrorAndReset(b);
                    }
                    break;
                default:
                    throw new InvalidOperationException("CRLFByteStreamLexer got into invalid state");
            }
        }
    }
}
