using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace HOLMS.PBXConnector.Protocol.SMDR {
    class CRLFByteStreamLineLexerTests {
        private CRLFByteSreamLineLexer _lexer;
        private int _errorCount;
        private List<string> _lines;
        const byte CR = 0x0d;
        const byte LF = 0x0a;
        private readonly byte[] _lineTrailer = { CR, LF };

        [SetUp]
        public void Init() {
            _lexer = new CRLFByteSreamLineLexer();
            _errorCount = 0;
            _lines = new List<string>();

            _lexer.Error += (sender, errorByte, line) => {
                ++_errorCount;
            };

            _lexer.LineReceived += (sender, line) => {
                _lines.Add(line);
            };
        }

        [Test]
        public void InvalidLineRaisesErrorEvent() {
            Assert.AreEqual(0, _errorCount);
            var input = new byte[] {0x0d, 0x0d};
            _lexer.Lex(input, 2);
            Assert.AreEqual(1, _errorCount);
        }

        [Test]
        public void WellFormedSingleLineReturnsSingleLine() {
            var textBytes = Encoding.ASCII.GetBytes("line");
            var bytes = new byte[textBytes.Length + _lineTrailer.Length];
            textBytes.CopyTo(bytes, 0);
            _lineTrailer.CopyTo(bytes, textBytes.Length);

            Assert.AreEqual(0, _lines.Count);
            _lexer.Lex(bytes, textBytes.Length + _lineTrailer.Length);

            Assert.AreEqual(1, _lines.Count);
            Assert.AreEqual("line", _lines.First());
        }

        [Test]
        public void PartialLexOfOneLineReturnsSingleLine() {
            var firstBytes = Encoding.ASCII.GetBytes("li");
            _lexer.Lex(firstBytes, firstBytes.Length);

            var nextBytes = Encoding.ASCII.GetBytes("ne");
            _lexer.Lex(nextBytes, nextBytes.Length);

            Assert.AreEqual(0, _lines.Count);
            Assert.AreEqual(0, _errorCount);

            _lexer.Lex(_lineTrailer, 2);
            Assert.AreEqual(1, _lines.Count);
            Assert.AreEqual("line", _lines.First());
        }

        [Test]
        public void LexingTwoLinesSucceeds() {
            var textBytes = Encoding.ASCII.GetBytes("line");
            var bytes = new byte[textBytes.Length + _lineTrailer.Length];
            textBytes.CopyTo(bytes, 0);
            _lineTrailer.CopyTo(bytes, textBytes.Length);

            Assert.AreEqual(0, _lines.Count);
            _lexer.Lex(bytes, textBytes.Length + _lineTrailer.Length);
            _lexer.Lex(bytes, textBytes.Length + _lineTrailer.Length);

            Assert.AreEqual(2, _lines.Count);
            Assert.AreEqual("line", _lines.First());
        }

        [Test]
        public void LexerRecoversAfterErrors() {
            _lexer.Lex(CR);
            Assert.AreEqual(0, _errorCount);
            Assert.AreEqual(0, _lines.Count);

            _lexer.Lex(LF);
            Assert.AreEqual(0, _errorCount);    // End of good line
            Assert.AreEqual(1, _lines.Count);

            _lexer.Lex(CR);
            Assert.AreEqual(0, _errorCount);    // Starts from 0
            Assert.AreEqual(1, _lines.Count);

            _lexer.Lex(CR);
            Assert.AreEqual(1, _errorCount);    // End of bad line
            Assert.AreEqual(1, _lines.Count);

            // But gets back up again
            var textBytes = Encoding.ASCII.GetBytes("line");
            var bytes = new byte[textBytes.Length + _lineTrailer.Length];
            textBytes.CopyTo(bytes, 0);
            _lineTrailer.CopyTo(bytes, textBytes.Length);
            _lexer.Lex(bytes, textBytes.Length + _lineTrailer.Length);

            Assert.AreEqual(1, _errorCount);
            Assert.AreEqual(2, _lines.Count);
            Assert.AreEqual("line", _lines.Last());

        }
    }
}
