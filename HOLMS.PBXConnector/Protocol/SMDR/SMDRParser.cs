using System;
using System.Text.RegularExpressions;
using HOLMS.Messaging;
using HOLMS.Types.Extensions.Support;
using HOLMS.Types.PBXConnector;
using HOLMS.Application.Client;
using HOLMS.PBXConnector.Support;
using HOLMS.Platform.Types.Topics;
using Microsoft.Extensions.Logging;

namespace HOLMS.PBXConnector.Protocol.SMDR {
    internal class SMDRParser : PBXParser {
        private readonly Regex _dialedCallReport = new Regex(@"^.(\d{2})/(\d{2}) (\d{2}):(\d{2})(.) (\d{2}):(\d{2}):(\d{2}) (.{4}) .([0-9*# ]{4})([0-9*# ]{26})..(.{4})\s{15}$");
        private readonly Regex _roomStatusRegex = new Regex(@"^.{19}RS .{26}$");
        protected override string ProtocolName => "MitelSMDRParser";

        public SMDRParser(PBXConfiguration config, ILogger log, IMessageConnectionFactory cf, IApplicationClient ac) :
            base(config, log, cf, ac) {
            RegisterLexer(new CRLFByteSreamLineLexer());
        }

        protected override void ParseLine(object sender, string line) {
            Log.LogDebug($"{ProtocolName}: Received full line from lexer: {line}");
            // Provided as a test hook -- tests inject input here
            var dialedCallMatch = _dialedCallReport.Match(line);
            if (dialedCallMatch.Success) {
                var dc = new DialedCallReport(dialedCallMatch, line);
                ParseAndPublishDialedCall(dc);
                return;
            }

            var roomStatusMatch = _roomStatusRegex.Match(line);
            if (roomStatusMatch.Success) {
                Log.LogDebug($"Disregarding room status message \"{line}\"");
                return;
            }

            Log.LogWarning($"Ignoring unrecognized input line {line}");
        }

        private void ParseAndPublishDialedCall(DialedCallReport rp) {
            var call = rp.ToProtobuf();

            var msg = new PhoneCallEnded {
                JWToken = AC.SC.AccessToken,
                MsgReceivedAt = DateTime.Now.ToTS(),
                RawMsg = rp.RawLine,
                MitelCallEnded = call,
            };

            CH.Publish(PBXConnectorTopics.PbxCallCompletedTopic, msg);
        }
    }
}
