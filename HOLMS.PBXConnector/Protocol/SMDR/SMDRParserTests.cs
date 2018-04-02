using System;
using System.Linq;
using HOLMS.Messaging;
using HOLMS.Messaging.Tests;
using HOLMS.Types.PBXConnector;
using Moq;
using NUnit.Framework;
using HOLMS.PBXConnector.Support.Test;
using HOLMS.Platform.Support.Time;
using HOLMS.Platform.Types.Topics;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace HOLMS.PBXConnector.Protocol.SMDR {
    class SMDRParserTestShim : SMDRParser {
        public SMDRParserTestShim(FakeTCPConfiguration config, ILogger log, IMessageConnectionFactory cf, IClock c)
            : base(config.PMSConnection, log, cf, new FakeApplicationClient(config, log, "fake client id"), c) {
            StartRabbitMQ();
        }

        public void InjectLine(string s) {
            ParseLine(null, s);
        }
    }

    class SMDRParserTests {
        private FakeRabbitConnectionFactory _fcf;
        private Mock<ILogger> _logMock;
        private FakeClock _c;
        private SMDRParserTestShim _mp;
        private DateTime _localNow;

        const string ChuckCallingToMobilePhoneFromHotel =  " 08/02 14:12  00:00:24 402   9   14155596887                 T074               ";
        const string ChuckSignalingStatusAtColumbus =      " 03/30  3:06P 00:00:04 102   9   601                         T092               ";
        const string SyntheticCallFromCOTrunk =            " 08/02 14:12  00:00:24 T402  9   601                         T074               ";
        const string SyntheticCallFromNonCOTrunk =         " 08/02 14:13  00:00:06 X101  9   601                         T086               ";
        const string DisregardedRoomStatusMessage =        "127   08/03 10:28  RS CLEAN                     ";

        [SetUp]
        public void Init() {
            _fcf = new FakeRabbitConnectionFactory();
            _logMock = new Mock<ILogger>();
            _c = new FakeClock();
            _mp = new SMDRParserTestShim(new FakeTCPConfiguration(), _logMock.Object, _fcf, _c);
            var now = DateTime.Now;
            _localNow = now.ToLocalTime();
            _c.StopAt(Instant.FromDateTimeUtc(now.ToUniversalTime()));
        }

        private RecordedPublication AssertOnePublicationAndRetrieve() {
            var ch = _fcf.Connections.First().GetChannel(0);
            Assert.AreEqual(1, ch.Publications.Count);
            return ch.Publications.First();
        }

        [Test]
        public void CorrectParsePublishesCorrectOuterMessage() {
            _mp.InjectLine(ChuckCallingToMobilePhoneFromHotel);
            var pub = AssertOnePublicationAndRetrieve();
            Assert.AreEqual(PBXConnectorTopics.PbxCallCompletedTopic, pub.Routingkey);

            var call = (PhoneCallEnded)pub.Message;
            Assert.AreEqual(call.RawMsg, ChuckCallingToMobilePhoneFromHotel);
            Assert.IsInstanceOf<MitelCallEnded>(call.MitelCallEnded);
        }

        [Test]
        public void TwelveHourStartTimeAndDurationParsedCorrectly() {
            _mp.InjectLine(ChuckCallingToMobilePhoneFromHotel);
            var pub = AssertOnePublicationAndRetrieve();
            var mc = ((PhoneCallEnded)pub.Message).MitelCallEnded;
            
            // We transmit this thing in UTC but it's specified in local time
            var start = mc.StartTime.ToDateTime().ToLocalTime();
            Assert.AreEqual(_localNow.Year, start.Year);
            Assert.AreEqual(8, start.Month);
            Assert.AreEqual(2, start.Day);
            Assert.AreEqual(14, start.Hour);
            Assert.AreEqual(12, start.Minute);
            Assert.AreEqual(0, start.Second);

            var length = mc.Duration.ToTimeSpan();
            Assert.AreEqual(0, length.Days);
            Assert.AreEqual(0, length.Hours);
            Assert.AreEqual(0, length.Minutes);
            Assert.AreEqual(24, length.Seconds);
        }

        [Test]
        public void OutgoingCallFromInternalCallerParsesToExtensionCaller() {
            _mp.InjectLine(ChuckCallingToMobilePhoneFromHotel);

            var pub = AssertOnePublicationAndRetrieve();
            var caller = ((PhoneCallEnded)pub.Message).MitelCallEnded.CallOrigin;
            Assert.AreEqual("402", caller.CircuitIdentifier);
            Assert.AreEqual(MitelPhoneCircuitType.InternalExtension, caller.CircuitType);
        }

        [Test]
        public void IncomingCallFromCOTrunkToInternalGetsCOTrunkOrigin() {
            _mp.InjectLine(SyntheticCallFromCOTrunk);

            var pub = AssertOnePublicationAndRetrieve();
            var caller = ((PhoneCallEnded)pub.Message).MitelCallEnded.CallOrigin;
            Assert.AreEqual("402", caller.CircuitIdentifier);
            Assert.AreEqual(MitelPhoneCircuitType.CoTrunkNumber, caller.CircuitType);
        }

        [Test]
        public void IncomingCallFromNonCOTrunkToInternalGetsNonCOTrunkCaller() {
            _mp.InjectLine(SyntheticCallFromNonCOTrunk);

            var pub = AssertOnePublicationAndRetrieve();
            var caller = ((PhoneCallEnded)pub.Message).MitelCallEnded.CallOrigin;
            Assert.AreEqual("101", caller.CircuitIdentifier);
            Assert.AreEqual(MitelPhoneCircuitType.NonCoTrunkNumber, caller.CircuitType);
        }

        [Test]
        public void DialOutToMobilePhoneFromInternalCapturesLeadingDigitsAndDialedNumber() {
            var ch = _fcf.Connections.First().GetChannel(0);

            _mp.InjectLine(ChuckCallingToMobilePhoneFromHotel);
            var p0 = ch.Publications[0];
            var c0 = ((PhoneCallEnded)p0.Message).MitelCallEnded;
            Assert.AreEqual("9", c0.LeadingDigitsDialed);
            Assert.AreEqual("14155596887", c0.MainDigitsDialed);

            _mp.InjectLine(SyntheticCallFromCOTrunk);
            var p1 = ch.Publications[1];
            var c1 = ((PhoneCallEnded)p1.Message).MitelCallEnded;
            Assert.AreEqual("9", c1.LeadingDigitsDialed);
            Assert.AreEqual("601", c1.MainDigitsDialed);

            _mp.InjectLine(SyntheticCallFromNonCOTrunk);
            var p2 = ch.Publications[2];
            var c2 = ((PhoneCallEnded)p2.Message).MitelCallEnded;
            Assert.AreEqual("9", c2.LeadingDigitsDialed);
            Assert.AreEqual("601", c2.MainDigitsDialed);
        }

        [Test]
        public void StatusSignalingFromColumbusParsedWith12HourTime() {
            _mp.InjectLine(ChuckSignalingStatusAtColumbus);
            var pub = AssertOnePublicationAndRetrieve();
            var mc = ((PhoneCallEnded)pub.Message).MitelCallEnded;

            // We transmit this thing in UTC but it's specified in local time
            var start = mc.StartTime.ToDateTime().ToLocalTime();
            Assert.AreEqual(_localNow.Year, start.Year);
            Assert.AreEqual(3, start.Month);
            Assert.AreEqual(30, start.Day);
            Assert.AreEqual(15, start.Hour);
            Assert.AreEqual(6, start.Minute);
            Assert.AreEqual(0, start.Second);

            var length = mc.Duration.ToTimeSpan();
            Assert.AreEqual(0, length.Days);
            Assert.AreEqual(0, length.Hours);
            Assert.AreEqual(0, length.Minutes);
            Assert.AreEqual(4, length.Seconds);
        }

        [Test]
        public void RoomStatusMessageDoesNotPublish() {
            _mp.InjectLine(DisregardedRoomStatusMessage);

            var ch = _fcf.Connections.First().GetChannel(0);
            Assert.AreEqual(0, ch.Publications.Count);
        }

        [Test]
        public void GarbageInputIgnoredWithoutPublication() {
            _mp.InjectLine("garbage");

            var ch = _fcf.Connections.First().GetChannel(0);
            Assert.AreEqual(0, ch.Publications.Count);
        }
    }
}
