using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HOLMS.PBXConnector.Protocol.PMS {
    class PMSByteStreamLexerTests {
        private PMSByteStreamLexer _lexer;
        private int _errorCount;
        private int _enquireCount;
        private List<string> _lines;
        const byte ENQ = 0x05;
        const byte ACK = 0x06;
        const byte STX = 0x02;
        const byte ETX = 0x03;

        [SetUp]
        public void Init() {
            _lexer = new PMSByteStreamLexer();
            _errorCount = 0;
            _lines = new List<string>();

            _lexer.Error += (sender, errorByte, line) => {
                ++_errorCount;
            };

            _lexer.LineReceived += (sender, line) => {
                _lines.Add(line);
            };

            _lexer.EnquireRecieved += () => {
                ++_enquireCount;
            };
        }

        [Test]
        public void NonSpecialCharacterOnStartFlagsError() {
            Assert.AreEqual(0, _errorCount);
            var input = new byte[] { 0x20 };
            _lexer.Lex(input, 1);
            Assert.AreEqual(1, _errorCount);
        }

        [Test]
        public void WellFormedSingleLineReturnsSingleLine() {
            var textBytes = Encoding.ASCII.GetBytes("line");

            Assert.AreEqual(0, _lines.Count);
            _lexer.Lex(STX);
            _lexer.Lex(textBytes, textBytes.Length);
            _lexer.Lex(ETX);

            Assert.AreEqual(1, _lines.Count);
            Assert.AreEqual("line", _lines.First());
        }

        [Test]
        public void EnquireCharacterRaisesEnquireEvent() {
            Assert.AreEqual(0, _enquireCount);
            _lexer.Lex(ENQ);
            Assert.AreEqual(1, _enquireCount);
        }

        [Test]
        public void EnquireCharacterDuringLineModeRaisesErrorEvent() {
            Assert.AreEqual(0, _errorCount);

            _lexer.Lex(STX);
            _lexer.Lex(ENQ);
            Assert.AreEqual(1, _errorCount);
        }

        [Test]
        public void STXCharacterDuringLineModeRaisesErrorEvent() {
            Assert.AreEqual(0, _errorCount);

            _lexer.Lex(STX);
            _lexer.Lex(STX);
            Assert.AreEqual(1, _errorCount);
        }
    }
}
