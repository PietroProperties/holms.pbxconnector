using System;

namespace HOLMS.PBXConnector.Protocol.PMS {
    /// <summary>
    /// Stateful lexer for lexing a byte stream from a Mitel SX-200 ICP Property Management System Interface
    /// The interface uses a literal interpretation of ASCII encoding. Before transmitting a record, it issues an
    /// enquery character (0x05), and waits for an acknowledgement character (0x06) before transmitting the record.
    /// The record itself is preceeded by a start transmission (0x02) and followed by an end transmission (0x03).
    /// More information can be found in the official Mitel document:
    /// https://shortbar.atlassian.net/wiki/download/attachments/6520858/pms.xps?api=v2
    /// </summary>
    public class PMSByteStreamLexer : ByteStreamLexer {
        //protected int LexerState;            // 0: Expecting enquire character or transmission start character
                                                // 1: Expecting line characters or end transmission character
                                                
        public event EnquireRecievedHandler EnquireRecieved;
        public delegate void EnquireRecievedHandler();

        public PMSByteStreamLexer() : base() { }

        public override void Lex(byte b) {
            switch(LexerState) {
                case 0:
                    if (b == 0x05) {
                        // Received enquire character
                        NotifyEnquireReceived();
                    } else if (b == 0x02) {
                        // Received start transmission character
                        LexerState = 1;
                    } else if (b == 0x00) {
                        // Ignore null characters
                        break;
                    } else {
                        // Unexpected character. 
                        NotifyErrorAndReset(b);
                    }
                    break;
                case 1:
                    if(b == 0x03) {
                        // Transmission end character recieved, signifying end-of-line
                        NotifyLineReceived();
                    } else if(b == 0x05 || b == 0x02) {
                        // Unexpected enquire or start transmission character. Those should only happen in state 0
                        NotifyErrorAndReset(b);
                    } else {
                        // Character is part of line transmission.
                        AppendByteToLine(b);
                    }
                    break;
                default:
                    throw new InvalidOperationException("PMSByteStreamLexer got into invalid state");
            }
        }

        private void NotifyEnquireReceived() {
            EnquireRecieved?.Invoke();
        }
    }
}
