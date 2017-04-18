// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class GeneralNameEncoder
    {
        private enum GeneralNameTag : byte
        {
            // Constructed
            OtherName = DerSequenceReader.ContextSpecificConstructedTag0,

            // Primitive
            Rfc822Name = DerSequenceReader.ContextSpecificTagFlag | 1,
            DnsName = DerSequenceReader.ContextSpecificTagFlag | 2,
            X400Address = DerSequenceReader.ContextSpecificTagFlag | 3,
            DirectoryName = DerSequenceReader.ContextSpecificTagFlag | 4,
            EdiPartyName = DerSequenceReader.ContextSpecificTagFlag | 5,
            Uri = DerSequenceReader.ContextSpecificTagFlag | 6,
            IpAddress = DerSequenceReader.ContextSpecificTagFlag | 7,
            RegisteredId = DerSequenceReader.ContextSpecificTagFlag | 8,
        }

        private readonly IdnMapping _idnMapping = new IdnMapping();

        internal byte[][] EncodeEmailAddress(string emailAddress)
        {
            byte[][] rfc822NameTlv = DerEncoder.SegmentedEncodeIA5String(emailAddress.ToCharArray());
            rfc822NameTlv[0][0] = (byte)GeneralNameTag.Rfc822Name;

            return rfc822NameTlv;
        }

        internal byte[][] EncodeDnsName(string dnsName)
        {
            string idnaName = _idnMapping.GetAscii(dnsName);
            byte[][] dnsNameTlv = DerEncoder.SegmentedEncodeIA5String(idnaName.ToCharArray());
            dnsNameTlv[0][0] = (byte)GeneralNameTag.DnsName;

            return dnsNameTlv;
        }

        internal byte[][] EncodeUri(Uri uri)
        {
            byte[][] uriTlv = DerEncoder.SegmentedEncodeIA5String(uri.AbsoluteUri.ToCharArray());
            uriTlv[0][0] = (byte)GeneralNameTag.Uri;

            return uriTlv;
        }

        internal byte[][] EncodeIpAddress(IPAddress address)
        {
            byte[] addressBytes = address.GetAddressBytes();

            byte[][] ipAddressTlv = DerEncoder.SegmentedEncodeOctetString(addressBytes);
            ipAddressTlv[0][0] = (byte)GeneralNameTag.IpAddress;

            return ipAddressTlv;
        }

        internal byte[][] EncodeUserPrincipalName(string upn)
        {
            // AnotherName ::= SEQUENCE {
            //   type-id    OBJECT IDENTIFIER,
            //   value[0] EXPLICIT ANY DEFINED BY type-id
            // }

            byte[][] upnUtf8 = DerEncoder.SegmentedEncodeUtf8String(upn.ToCharArray());

            // [0] EXPLICIT
            byte[][] payloadTlv = DerEncoder.ConstructSegmentedSequence(upnUtf8);
            payloadTlv[0][0] = DerSequenceReader.ContextSpecificConstructedTag0;

            byte[][] anotherNameTlv = DerEncoder.ConstructSegmentedSequence(
                DerEncoder.SegmentedEncodeOid(Oids.UserPrincipalName),
                payloadTlv);

            anotherNameTlv[0][0] = (byte)GeneralNameTag.OtherName;

            return anotherNameTlv;
        }
    }
}
