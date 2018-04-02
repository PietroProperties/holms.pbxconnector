using HOLMS.Messaging;
using HOLMS.PBXConnector.Support;
using HOLMS.Types.PBXConnector;
using System.Text.RegularExpressions;
using HOLMS.Platform.Client;
using HOLMS.Platform.Types.Topics;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace HOLMS.PBXConnector.Protocol.PMS {
    /// <summary>
    /// Parser for room status messages issued by the Mitel SX-200 ICP Property Management System Interface
    /// parser interprets complete records lexed by PMSByteStreamLexer. A record consists
    /// of "STS" followed by a single digit code that was sent by a phone terminal. These 
    /// four characters are followed by a space, and then the 1-4 digit unique identifier of 
    /// the phone terminal that issued the message. Documentation on the PMS inteface can be found
    /// https://shortbar.atlassian.net/wiki/download/attachments/6520858/pms.xps?api=v2
    /// </summary>
    internal class PMSParser : PBXParser {
        private readonly Regex _roomStatusUpdate = new Regex(@"^STS([\d ]) (\d{1,4})\s+$");
        private const byte ACK = 0x06;

        protected override string ProtocolName => "MitelPMSParser";
        protected const string ReceivedMessageStringFormat = "Recieved room status message code: {0}, terminal number: {1}";


        public PMSParser(PBXConfiguration config, ILogger log, IMessageConnectionFactory cf,
                IApplicationClient ac, IClock c) :
            base(config, log, cf, ac, c) {
            var lexer = new PMSByteStreamLexer();
            lexer.EnquireRecieved += EnquireReceived;
            RegisterLexer(lexer);
        }

        private void EnquireReceived() {
            Transport.Send(new[] { ACK });
        }

        protected override void ParseLine(object sender, string line) {
            Log.LogDebug($"{ProtocolName}: Received full line from lexer: {line}");
            Transport.Send(new[] { ACK });
            var roomStatusUpdateMatch = _roomStatusUpdate.Match(line);
            if (roomStatusUpdateMatch.Success) {
                ParseAndPublishRoomStatus(roomStatusUpdateMatch);
            } else {
                Log.LogWarning($"Ignoring unrecognized input line {line}");
            }
        }

        private void ParseAndPublishRoomStatus(Match regexMatch) {
            var message = new TCPRoomStatusMessage() {
                JWToken = AC.SC.AccessToken,
                StatusCode = regexMatch.Groups[1].Value,
                TerminalIdentifier = regexMatch.Groups[2].Value
            };

            CH.Publish(PBXConnectorTopics.TcpRoomStatusTopic, message);

            Log.LogDebug(string.Format(ReceivedMessageStringFormat, regexMatch.Groups[1], regexMatch.Groups[2]));
        }
    }
}
