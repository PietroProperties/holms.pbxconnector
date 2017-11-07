using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace HOLMS.PBXConnector.Support {
    class RegistryConfigurationProviderTests {
        ILogger _log;

        [SetUp]
        public void Init() {
            _log = new Mock<ILogger>().Object;
        }

        [Test]
        public void PBXConnectionStringOfShortLengthThrows() {
            Assert.Throws<ArgumentException>(() => RegistryConfigurationProvider.BuildConfiguration(_log, "8080"));
        }

        [Test]
        public void PBXConnectionStringWithoutPBXPrefixThrows() {
            Assert.Throws<ArgumentException>(() => RegistryConfigurationProvider.BuildConfiguration(_log, "tcp:192.168.1.1:8080"));
        }

        [Test]
        public void ValidTCPPBXConnectionStringCreatesConfiguration() {
            var pbxConfig = RegistryConfigurationProvider.BuildConfiguration(_log, "pbx://tcp:192.168.1.1:8080");
            Assert.IsFalse(pbxConfig.SerialEnabled);
            Assert.IsTrue(pbxConfig.TCPEnabled);
            Assert.IsNull(pbxConfig.SerialPort);
            Assert.AreEqual("192.168.1.1", pbxConfig.TCPHost);
            Assert.AreEqual(8080, pbxConfig.TCPPort);
        }

        [Test]
        public void TCPPBXConnectionStringWithInvalidPortThrows() {
            Assert.Throws<ArgumentException>(() => RegistryConfigurationProvider.BuildConfiguration(_log, "pbx://tcp:192.168.1.1:80800"));
        }

        [Test]
        public void ValidTCP_IPv6PBXConnectionStringCreatesConfiguration() {
            var pbxConfig = RegistryConfigurationProvider.BuildConfiguration(_log, "pbx://tcp:2001:0db8:85a3:0000:0000:8a2e:0370:7334:8080");
            Assert.IsFalse(pbxConfig.SerialEnabled);
            Assert.IsTrue(pbxConfig.TCPEnabled);
            Assert.IsNull(pbxConfig.SerialPort);
            Assert.AreEqual("2001:0db8:85a3:0000:0000:8a2e:0370:7334", pbxConfig.TCPHost);
            Assert.AreEqual(8080, pbxConfig.TCPPort);
        }

        [Test]
        public void ValidSerialPBXConnectionStringCreatesConfiguration() {
            var pbxConfig = RegistryConfigurationProvider.BuildConfiguration(_log, "pbx://serial:COM1");
            Assert.IsTrue(pbxConfig.SerialEnabled);
            Assert.IsFalse(pbxConfig.TCPEnabled);
            Assert.AreEqual("COM1", pbxConfig.SerialPort);
            Assert.IsNull(pbxConfig.TCPHost);
            Assert.AreEqual(0, pbxConfig.TCPPort);
        }

        [Test]
        public void ValidEmptyConnectionStringCreatesConfiguration() {
            var pbxConfig = RegistryConfigurationProvider.BuildConfiguration(_log, "pbx://none");
            Assert.IsFalse(pbxConfig.SerialEnabled);
            Assert.IsFalse(pbxConfig.TCPEnabled);
            Assert.IsNull(pbxConfig.SerialPort);
            Assert.IsNull(pbxConfig.TCPHost);
            Assert.AreEqual(0, pbxConfig.TCPPort);
        }
    }
}
