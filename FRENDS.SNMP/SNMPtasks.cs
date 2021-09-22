using Lextm.SharpSnmpLib.BouncyCastle;
using Lextm.SharpSnmpLib.BouncyCastle.Messaging;
using Lextm.SharpSnmpLib.BouncyCastle.Security;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FRENDS.Community.SNMP
{
    public class SNMPtasks
    {

        public class ByteMapping
        {
            public String ByteName { get; set; }
            [DefaultValue(true)]
            public Boolean IsHexaDecimal { get; set; }
        }
        public class OIDMapping
        {
            public String OID { get; set; }

            public String Name { get; set; }

            public ByteMapping[] ByteMapping { get; set; }
        }
        public class Input
        {
            public OIDMapping[] OIDs { get; set; }

            [DisplayFormat(DataFormatString = "Text")]
            public String IPAddress { get; set; }

            public Int32 Port { get; set; }
        }
        public enum SNMPVersion { V3, V2, V1 }
        public enum AuthProtocol { MD5, SHA1, SHA256 }
        public enum PrivacyProtocol { DES, TripleDES, AES, AES192, AES256 }

        public class Options
        {

            [DefaultValue(6000)]
            public Int32 Timeout { get; set; }

            [DisplayFormat(DataFormatString = "Text")]
            public String Community { get; set; }

            [DefaultValue("V3")]
            public SNMPVersion SNMPVersion { get; set; }

            public String AuthenticationPassword { get; set; }

            public String PrivacyPassword { get; set; }

            [DefaultValue("MD5")]
            public AuthProtocol AuthProtocol { get; set; }

            [DefaultValue("DES")]
            public PrivacyProtocol PrivacyProtocol { get; set; }
        }


        /// <summary>
        /// Get information from device.
        /// Documentation: https://github.com/CommunityHiQ/Frends.Community.SNMP
        /// </summary>
        /// <param name="input">Connection information.</param>
        /// <param name="options">Optional parameters.</param>
        /// <returns>Object including array of oids</returns>

        public static object SNMPGETNEXT([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            List<Variable> variablesList = new List<Variable>();

            foreach (OIDMapping mapping in input.OIDs)
            {
                variablesList.Add(new Variable(new ObjectIdentifier(mapping.OID)));
            }

            GetNextRequestMessage message;

            switch (options.SNMPVersion)
            {
                case SNMPVersion.V3:
                    Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                    ReportMessage report = discovery.GetResponse(options.Timeout, new IPEndPoint(IPAddress.Parse(input.IPAddress), input.Port));
                    cancellationToken.ThrowIfCancellationRequested();
                    if (report.Pdu().ErrorStatus.ToInt32() != 0)
                    {
                        throw ErrorException.Create(
                            "error in ReportMessage response",
                            IPAddress.Parse(input.IPAddress),
                            report);
                    }

                    IAuthenticationProvider auth = null;
                    switch (options.AuthProtocol)
                    {
                        case AuthProtocol.MD5:
                            auth = new MD5AuthenticationProvider(new OctetString(options.AuthenticationPassword));
                            break;
                        case AuthProtocol.SHA1:
                            auth = new SHA1AuthenticationProvider(new OctetString(options.AuthenticationPassword));
                            break;
                        case AuthProtocol.SHA256:
                            auth = new SHA256AuthenticationProvider(new OctetString(options.AuthenticationPassword));
                            break;
                    }
                    IPrivacyProvider priv = null;
                    switch (options.PrivacyProtocol)
                    {
                        case PrivacyProtocol.DES:
                            priv = new DESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            break;
                        case PrivacyProtocol.TripleDES:
                            priv = new TripleDESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            break;
                        case PrivacyProtocol.AES:
                            if (AESPrivacyProvider.IsSupported)
                            {
                                priv = new AESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            }
                            else
                                priv = new Samples.BouncyCastle.BouncyCastleAESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);                           
                            break;
                        case PrivacyProtocol.AES192:
                            if (AESPrivacyProvider.IsSupported)
                            {
                                priv = new AES192PrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            }
                            else
                                priv = new Samples.BouncyCastle.BouncyCastleAES192PrivacyProvider(new OctetString(options.PrivacyPassword), auth);                            
                            break;
                        case PrivacyProtocol.AES256:
                            if (AESPrivacyProvider.IsSupported)
                            {
                                priv = new AES256PrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            }
                            else
                                priv = new Samples.BouncyCastle.BouncyCastleAES256PrivacyProvider(new OctetString(options.PrivacyPassword), auth);                            
                            break;
                    }

                    message = new GetNextRequestMessage(VersionCode.V3,
                                                        Messenger.NextMessageId,
                                                        Messenger.NextRequestId,
                                                        new OctetString(options.Community),
                                                        variablesList,
                                                        priv,
                                                        Messenger.MaxMessageSize,
                                                        report);

                    cancellationToken.ThrowIfCancellationRequested();
                    if (message.Pdu().ErrorStatus.ToInt32() != 0)
                    {
                        throw ErrorException.Create(
                            "error in ReportMessage response",
                            IPAddress.Parse(input.IPAddress),
                            report);
                    }
                    break;
                case SNMPVersion.V2:
                    {
                        message = new GetNextRequestMessage(0,
                                                            VersionCode.V2,
                                                            new OctetString(options.Community),
                                                            variablesList);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                case SNMPVersion.V1:
                    {
                        message = new GetNextRequestMessage(0,
                                                            VersionCode.V1,
                                                            new OctetString(options.Community),
                                                            variablesList);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ISnmpMessage response = message.GetResponse(options.Timeout, new IPEndPoint(IPAddress.Parse(input.IPAddress), input.Port));
            cancellationToken.ThrowIfCancellationRequested();

            if (response.Pdu().ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in GetNextRequestMessage response",
                    IPAddress.Parse(input.IPAddress),
                    response);
            }

            JArray mappingList = new JArray();
            dynamic responseObject = new JObject();

            try
            {
                foreach (Variable var in response.Pdu().Variables)
                {
                    foreach (OIDMapping mapping in input.OIDs)
                    {
                        if (var.Id.ToString().Contains(mapping.OID))
                        {
                            responseObject[mapping.Name] = var.Data.ToString();

                            if (mapping.ByteMapping.Length > 0)
                            {
                                String[] bytesArray = var.Data.ToString().Split(' ');
                                for (var i = 0; i < mapping.ByteMapping.Length; i++)
                                {
                                    if (mapping.ByteMapping[i].IsHexaDecimal)
                                    {
                                        responseObject[mapping.ByteMapping[i].ByteName] = Convert.ToInt32(bytesArray[i].ToString(), 16);
                                    }
                                    else
                                    {
                                        responseObject[mapping.ByteMapping[i].ByteName] = bytesArray[i].ToString();
                                    }
                                }
                            }

                            dynamic mappingObject = new JObject();
                            mappingObject.Name = mapping.Name;
                            mappingObject.OID = var.Id.ToString();

                            mappingList.Add(JToken.FromObject(mappingObject));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            responseObject.TimeStamp = DateTime.Now;
            responseObject.MappingList = mappingList;

            return responseObject;
        }

        public static object SNMPSINGLEGET([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            List<Variable> variablesList = new List<Variable>();

            foreach (OIDMapping mapping in input.OIDs)
            {
                variablesList.Add(new Variable(new ObjectIdentifier(mapping.OID)));
            }

            GetRequestMessage message;
            
            switch (options.SNMPVersion)
            {
                case SNMPVersion.V3:
                    Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                    ReportMessage report = discovery.GetResponse(options.Timeout, new IPEndPoint(IPAddress.Parse(input.IPAddress), input.Port));
                    cancellationToken.ThrowIfCancellationRequested();
                    if (report.Pdu().ErrorStatus.ToInt32() != 0)
                    {
                        throw ErrorException.Create(
                            "error in ReportMessage response",
                            IPAddress.Parse(input.IPAddress),
                            report);
                    }

                    IAuthenticationProvider auth = null;
                    switch (options.AuthProtocol)
                    {
                        case AuthProtocol.MD5:
                            auth = new MD5AuthenticationProvider(new OctetString(options.AuthenticationPassword));
                            break;
                        case AuthProtocol.SHA1:
                            auth = new SHA1AuthenticationProvider(new OctetString(options.AuthenticationPassword));
                            break;
                        case AuthProtocol.SHA256:
                            auth = new SHA256AuthenticationProvider(new OctetString(options.AuthenticationPassword));
                            break;
                    }
                    IPrivacyProvider priv = null;
                    switch (options.PrivacyProtocol)
                    {
                        case PrivacyProtocol.DES:
                            priv = new DESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            break;
                        case PrivacyProtocol.TripleDES:
                            priv = new TripleDESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            break;
                        case PrivacyProtocol.AES:
                            if (AESPrivacyProvider.IsSupported)
                            {
                                priv = new AESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            }
                            else
                                priv = new Samples.BouncyCastle.BouncyCastleAESPrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            break;
                        case PrivacyProtocol.AES192:
                            if (AESPrivacyProvider.IsSupported)
                            {
                                priv = new AES192PrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            }
                            else
                                priv = new Samples.BouncyCastle.BouncyCastleAES192PrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            break;
                        case PrivacyProtocol.AES256:
                            if (AESPrivacyProvider.IsSupported)
                            {
                                priv = new AES256PrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            }
                            else
                                priv = new Samples.BouncyCastle.BouncyCastleAES256PrivacyProvider(new OctetString(options.PrivacyPassword), auth);
                            break;
                    }

                    message = new GetRequestMessage(VersionCode.V3,
                                                        Messenger.NextMessageId,
                                                        Messenger.NextRequestId,
                                                        new OctetString(options.Community),
                                                        variablesList,
                                                        priv,
                                                        Messenger.MaxMessageSize,
                                                        report);

                    cancellationToken.ThrowIfCancellationRequested();
                    if (message.Pdu().ErrorStatus.ToInt32() != 0)
                    {
                        throw ErrorException.Create(
                            "error in ReportMessage response",
                            IPAddress.Parse(input.IPAddress),
                            report);
                    }
                    break;
                case SNMPVersion.V2:
                    {
                        message = new GetRequestMessage(0,
                                                            VersionCode.V2,
                                                            new OctetString(options.Community),
                                                            variablesList);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                case SNMPVersion.V1:
                    {
                        message = new GetRequestMessage(0,
                                                            VersionCode.V1,
                                                            new OctetString(options.Community),
                                                            variablesList);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ISnmpMessage response = message.GetResponse(options.Timeout, new IPEndPoint(IPAddress.Parse(input.IPAddress), input.Port));
            cancellationToken.ThrowIfCancellationRequested();

            if (response.Pdu().ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in GetNextRequestMessage response",
                    IPAddress.Parse(input.IPAddress),
                    response);
            }

            JArray mappingList = new JArray();
            dynamic responseObject = new JObject();

            try
            {
                foreach (Variable var in response.Pdu().Variables)
                {
                    foreach (OIDMapping mapping in input.OIDs)
                    {
                        if (var.Id.ToString().Contains(mapping.OID))
                        {
                            responseObject[mapping.Name] = var.Data.ToString();

                            if (mapping.ByteMapping.Length > 0)
                            {
                                String[] bytesArray = var.Data.ToString().Split(' ');
                                for (var i = 0; i < mapping.ByteMapping.Length; i++)
                                {
                                    if (mapping.ByteMapping[i].IsHexaDecimal)
                                    {
                                        responseObject[mapping.ByteMapping[i].ByteName] = Convert.ToInt32(bytesArray[i].ToString(), 16);
                                    }
                                    else
                                    {
                                        responseObject[mapping.ByteMapping[i].ByteName] = bytesArray[i].ToString();
                                    }
                                }
                            }

                            dynamic mappingObject = new JObject();
                            mappingObject.Name = mapping.Name;
                            mappingObject.OID = var.Id.ToString();

                            mappingList.Add(JToken.FromObject(mappingObject));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            responseObject.TimeStamp = DateTime.Now;
            responseObject.MappingList = mappingList;

            return responseObject;
        }
    }
}
