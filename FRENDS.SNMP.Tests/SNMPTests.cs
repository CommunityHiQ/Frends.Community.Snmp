using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FRENDS.Community.SNMP.Tests
{
    [TestFixture]
    class SNMPTests
    {

        [Test]
        public void InvalidIPAddressTestCase()
        {
            var oid = new SNMPtasks.OIDMapping() { OID = "1.2.3", Name = "abc" };
            SNMPtasks.OIDMapping[] oids = { oid };
            var input = new SNMPtasks.Input() { IPAddress = "", OIDs = oids, Port = 161 };
            var option = new SNMPtasks.Options() { Timeout = 1000, Community = "string", SNMPVersion = SNMPtasks.SNMPVersion.V1 };

            try
            {
                var res = SNMPtasks.SNMPGETNEXT(input, option, new CancellationToken());
                Assert.Fail("An exception should have been thrown");
            }
            catch (FormatException fe)
            {
                Assert.AreEqual("An invalid IP address was specified.", fe.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(
                     string.Format("Unexpected exception of type {0} caught: {1}",
                                    e.GetType(), e.Message)
                );
            }

        }
        [Test]
        public void TimeOutTestCase()
        {
            var oid = new SNMPtasks.OIDMapping() { OID = "1.2.3", Name = "abc" };
            SNMPtasks.OIDMapping[] oids = { oid };
            var input = new SNMPtasks.Input() { IPAddress = "1.1.1.1", OIDs = oids, Port = 161 };
            var option = new SNMPtasks.Options() { Timeout = 1000, Community = "string", SNMPVersion = SNMPtasks.SNMPVersion.V1 };

            try
            {
                var res = SNMPtasks.SNMPGETNEXT(input, option, new CancellationToken());
                Assert.Fail("An exception should have been thrown");
            }
            catch (Lextm.SharpSnmpLib.BouncyCastle.Messaging.TimeoutException te)
            {
                Assert.AreEqual("Request timed out after 1000-ms.", te.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(
                     string.Format("Unexpected exception of type {0} caught: {1}",
                                    e.GetType(), e.Message)
                );
            }

        }

    }
}
