using System;
using System.Text.RegularExpressions;
using HOLMS.Types.Extensions.Support;
using HOLMS.Types.PBXConnector;
using Google.Protobuf.WellKnownTypes;

namespace HOLMS.PBXConnector.Protocol.SMDR {
    public class DialedCallReport {
        private readonly Match _m;
        public string RawLine { get; protected set; }

        public DialedCallReport(Match m, string line) {
            _m = m;
            RawLine = line;
        }

        public MitelCallEnded ToProtobuf() {
            var callStartTime = GetStartDateFromSMDR(
                _m.Groups[1].Value, _m.Groups[2].Value,
                _m.Groups[3].Value, _m.Groups[4].Value,
                _m.Groups[5].Value);
            var duration = new TimeSpan(Convert.ToInt32(_m.Groups[6].Value),
                Convert.ToInt32(_m.Groups[7].Value), Convert.ToInt32(_m.Groups[8].Value));
            var originCircuit = DecodeCircuitDescriptor(_m.Groups[9].Value);
            var destinationCircuit = DecodeCircuitDescriptor(_m.Groups[12].Value);

            return new MitelCallEnded {
                StartTime = callStartTime.ToTS(),
                Duration = duration.ToDuration(),
                CallOrigin = originCircuit,
                LeadingDigitsDialed = _m.Groups[10].Value.Trim(),
                MainDigitsDialed = _m.Groups[11].Value.Trim(),
                CallDestination = destinationCircuit
            };
        }

        private DateTime GetStartDateFromSMDR(string month, string day,
            string hour, string minute, string pmDesignator) {
            //12 or 24-hour clock. If 12-hour, pm indicated by "p" after time
            var hourInt = Convert.ToInt32(hour) + (pmDesignator.ToLower() == "p" ? 12 : 0);
            var minuteInt = Convert.ToInt32(minute);

            var monthInt = Convert.ToInt32(month);
            var dayInt = Convert.ToInt32(day);

            var now = DateTime.Now;
            return new DateTime(now.Year, monthInt, dayInt, hourInt, minuteInt, 0, DateTimeKind.Local);
        }

        private MitelPhoneCircuit DecodeCircuitDescriptor(string caller) {
            switch (caller[0]) {
                case 'T':
                    return new MitelPhoneCircuit {
                        CircuitIdentifier = string.Format($"{caller[1]}{caller[2]}{caller[3]}").Trim(),
                        CircuitType = MitelPhoneCircuitType.CoTrunkNumber
                    };
                case 'X':
                    return new MitelPhoneCircuit {
                        CircuitIdentifier = string.Format($"{caller[1]}{caller[2]}{caller[3]}").Trim(),
                        CircuitType = MitelPhoneCircuitType.NonCoTrunkNumber
                    };
                default:
                    return new MitelPhoneCircuit {
                        CircuitIdentifier = string.Format($"{caller[0]}{caller[1]}{caller[2]}{caller[3]}").Trim(),
                        CircuitType = MitelPhoneCircuitType.InternalExtension
                    };
            }
        }
    }
}
