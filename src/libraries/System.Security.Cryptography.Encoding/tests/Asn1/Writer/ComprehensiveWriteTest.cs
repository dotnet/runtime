// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

using X509KeyUsageCSharpStyle = System.Security.Cryptography.Tests.Asn1.ReadNamedBitList.X509KeyUsageCSharpStyle;

namespace System.Security.Cryptography.Tests.Asn1
{
    public static class ComprehensiveWriteTest
    {
        [Fact]
        public static void WriteMicrosoftDotComCert()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                // Certificate
                writer.PushSequence();

                // tbsCertificate
                writer.PushSequence();

                // version ([0] EXPLICIT INTEGER)
                Asn1Tag context0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
                writer.PushSequence(context0);
                writer.WriteInteger(2);
                writer.PopSequence(context0);

                BigInteger serialValue = BigInteger.Parse("82365655871428336739211871484630851433");
                writer.WriteInteger(serialValue);

                // signature (algorithm)
                writer.PushSequence();
                writer.WriteObjectIdentifier("1.2.840.113549.1.1.11");
                writer.WriteNull();
                writer.PopSequence();

                // issuer
                writer.PushSequence();
                WriteRdn(writer, "2.5.4.6", "US", UniversalTagNumber.PrintableString);
                WriteRdn(writer, "2.5.4.10", "Symantec Corporation", UniversalTagNumber.PrintableString);
                WriteRdn(writer, "2.5.4.11", "Symantec Trust Network", UniversalTagNumber.PrintableString);
                WriteRdn(writer, "2.5.4.3", "Symantec Class 3 EV SSL CA - G3", UniversalTagNumber.PrintableString);
                writer.PopSequence();

                // validity
                writer.PushSequence();
                writer.WriteUtcTime(new DateTimeOffset(2014, 10, 15, 0, 0, 0, TimeSpan.Zero));
                writer.WriteUtcTime(new DateTimeOffset(2016, 10, 15, 23, 59, 59, TimeSpan.Zero));
                writer.PopSequence();

                // subject
                writer.PushSequence();
                WriteRdn(writer, "1.3.6.1.4.1.311.60.2.1.3", "US", UniversalTagNumber.PrintableString);
                WriteRdn(writer, "1.3.6.1.4.1.311.60.2.1.2", "Washington", UniversalTagNumber.UTF8String);
                WriteRdn(writer, "2.5.4.15", "Private Organization", UniversalTagNumber.PrintableString);
                WriteRdn(writer, "2.5.4.5", "600413485", UniversalTagNumber.PrintableString);
                WriteRdn(writer, "2.5.4.6", "US", UniversalTagNumber.PrintableString);
                WriteRdn(writer, "2.5.4.17", "98052", UniversalTagNumber.UTF8String);
                WriteRdn(writer, "2.5.4.8", "Washington", UniversalTagNumber.UTF8String);
                WriteRdn(writer, "2.5.4.7", "Redmond", UniversalTagNumber.UTF8String);
                WriteRdn(writer, "2.5.4.9", "1 Microsoft Way", UniversalTagNumber.UTF8String);
                WriteRdn(writer, "2.5.4.10", "Microsoft Corporation", UniversalTagNumber.UTF8String);
                WriteRdn(writer, "2.5.4.11", "MSCOM", UniversalTagNumber.UTF8String);
                WriteRdn(writer, "2.5.4.3", "www.microsoft.com", UniversalTagNumber.UTF8String);
                writer.PopSequence();

                // subjectPublicKeyInfo
                writer.PushSequence();
                // subjectPublicKeyInfo.algorithm
                writer.PushSequence();
                writer.WriteObjectIdentifier("1.2.840.113549.1.1.1");
                writer.WriteNull();
                writer.PopSequence();

                using (AsnWriter publicKeyWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    publicKeyWriter.PushSequence();
                    BigInteger modulus = BigInteger.Parse(
                        "207545550571844404676608632512851454930111394466749205318948660756381" +
                        "523214360115124048083611193260260272384440199925180817531535965931647" +
                        "037093368608713442955529617501657176146245891571745113402870077189045" +
                        "116705181899983704226178882882602868159586789723579670915035003754974" +
                        "985730226756711782751711104985926458681071638525996766798322809764200" +
                        "941677343791419428587801897366593842552727222686457866144928124161967" +
                        "521735393182823375650694786333059783380738262856873316471830589717911" +
                        "537307419734834201104082715701367336140572971505716740825623220507359" +
                        "42929758463490933054115079473593821332264673455059897928082590541");
                    publicKeyWriter.WriteInteger(modulus);
                    publicKeyWriter.WriteInteger(65537);
                    publicKeyWriter.PopSequence();

                    // subjectPublicKeyInfo.subjectPublicKey
                    writer.WriteBitString(publicKeyWriter.Encode());
                    writer.PopSequence();
                }

                // extensions ([3] EXPLICIT Extensions)
                Asn1Tag context3 = new Asn1Tag(TagClass.ContextSpecific, 3);
                writer.PushSequence(context3);
                writer.PushSequence();

                Asn1Tag dnsName = new Asn1Tag(TagClass.ContextSpecific, 2);

                using (AsnWriter extensionValueWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteCharacterString(dnsName, UniversalTagNumber.IA5String, "www.microsoft.com");
                    extensionValueWriter.WriteCharacterString(dnsName, UniversalTagNumber.IA5String, "wwwqa.microsoft.com");
                    extensionValueWriter.PopSequence();

                    writer.PushSequence();
                    writer.WriteObjectIdentifier("2.5.29.17");
                    writer.WriteOctetString(extensionValueWriter.Encode());
                    writer.PopSequence();
                }

                writer.PushSequence();
                writer.WriteObjectIdentifier("2.5.29.19");
                // Empty sequence as the payload for a non-CA basic constraint.
                writer.WriteOctetString(new byte[] { 0x30, 0x00 });
                writer.PopSequence();

                using (AsnWriter extensionValueWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    // This extension doesn't use a sequence at all, just Named Bit List.
                    extensionValueWriter.WriteNamedBitList(
                        X509KeyUsageCSharpStyle.DigitalSignature | X509KeyUsageCSharpStyle.KeyEncipherment);

                    writer.PushSequence();
                    writer.WriteObjectIdentifier("2.5.29.15");
                    // critical: true
                    writer.WriteBoolean(true);
                    writer.WriteOctetString(extensionValueWriter.Encode());
                    writer.PopSequence();
                }

                using (AsnWriter extensionValueWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteObjectIdentifier("1.3.6.1.5.5.7.3.1");
                    extensionValueWriter.WriteObjectIdentifier("1.3.6.1.5.5.7.3.2");
                    extensionValueWriter.PopSequence();

                    writer.PushSequence();
                    writer.WriteObjectIdentifier("2.5.29.37");
                    writer.WriteOctetString(extensionValueWriter.Encode());
                    writer.PopSequence();
                }

                using (AsnWriter extensionValueWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteObjectIdentifier("2.16.840.1.113733.1.7.23.6");
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteObjectIdentifier("1.3.6.1.5.5.7.2.1");
                    extensionValueWriter.WriteCharacterString(UniversalTagNumber.IA5String, "https://d.symcb.com/cps");
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteObjectIdentifier("1.3.6.1.5.5.7.2.2");
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteCharacterString(UniversalTagNumber.VisibleString, "https://d.symcb.com/rpa");
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PopSequence();

                    writer.PushSequence();
                    writer.WriteObjectIdentifier("2.5.29.32");
                    writer.WriteOctetString(extensionValueWriter.Encode());
                    writer.PopSequence();
                }

                byte[] authorityKeyIdentifier = "0159ABE7DD3A0B59A66463D6CF200757D591E76A".HexToByteArray();
                Asn1Tag keyIdentifier = context0;
                using (AsnWriter extensionValueWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteOctetString(keyIdentifier, authorityKeyIdentifier);
                    extensionValueWriter.PopSequence();

                    writer.PushSequence();
                    writer.WriteObjectIdentifier("2.5.29.35");
                    writer.WriteOctetString(extensionValueWriter.Encode());
                    writer.PopSequence();
                }

                Asn1Tag distributionPointChoice = context0;
                Asn1Tag fullNameChoice = context0;
                Asn1Tag generalNameUriChoice = new Asn1Tag(TagClass.ContextSpecific, 6);
                using (AsnWriter extensionValueWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.PushSequence(distributionPointChoice);
                    extensionValueWriter.PushSequence(fullNameChoice);
                    extensionValueWriter.WriteCharacterString(
                        generalNameUriChoice,
                        UniversalTagNumber.IA5String,
                        "http://sr.symcb.com/sr.crl");
                    extensionValueWriter.PopSequence(fullNameChoice);
                    extensionValueWriter.PopSequence(distributionPointChoice);
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PopSequence();

                    writer.PushSequence();
                    writer.WriteObjectIdentifier("2.5.29.31");
                    writer.WriteOctetString(extensionValueWriter.Encode());
                    writer.PopSequence();
                }

                using (AsnWriter extensionValueWriter = new AsnWriter(AsnEncodingRules.DER))
                {
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteObjectIdentifier("1.3.6.1.5.5.7.48.1");
                    extensionValueWriter.WriteCharacterString(
                        generalNameUriChoice,
                        UniversalTagNumber.IA5String,
                        "http://sr.symcd.com");
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PushSequence();
                    extensionValueWriter.WriteObjectIdentifier("1.3.6.1.5.5.7.48.2");
                    extensionValueWriter.WriteCharacterString(
                        generalNameUriChoice,
                        UniversalTagNumber.IA5String,
                        "http://sr.symcb.com/sr.crt");
                    extensionValueWriter.PopSequence();
                    extensionValueWriter.PopSequence();

                    writer.PushSequence();
                    writer.WriteObjectIdentifier("1.3.6.1.5.5.7.1.1");
                    writer.WriteOctetString(extensionValueWriter.Encode());
                    writer.PopSequence();
                }

                writer.PopSequence();
                writer.PopSequence(context3);

                // tbsCertificate
                writer.PopSequence();

                // signatureAlgorithm
                writer.PushSequence();
                writer.WriteObjectIdentifier("1.2.840.113549.1.1.11");
                writer.WriteNull();
                writer.PopSequence();

                // signature
                byte[] containsSignature = (
                    "010203040506070809" +
                    "15F8505B627ED7F9F96707097E93A51E7A7E05A3D420A5C258EC7A1CFE1843EC" +
                    "20ACF728AAFA7A1A1BC222A7CDBF4AF90AA26DEEB3909C0B3FB5C78070DAE3D6" +
                    "45BFCF840A4A3FDD988C7B3308BFE4EB3FD66C45641E96CA3352DBE2AEB4488A" +
                    "64A9C5FB96932BA70059CE92BD278B41299FD213471BD8165F924285AE3ECD66" +
                    "6C703885DCA65D24DA66D3AFAE39968521995A4C398C7DF38DFA82A20372F13D" +
                    "4A56ADB21B5822549918015647B5F8AC131CC5EB24534D172BC60218A88B65BC" +
                    "F71C7F388CE3E0EF697B4203720483BB5794455B597D80D48CD3A1D73CBBC609" +
                    "C058767D1FF060A609D7E3D4317079AF0CD0A8A49251AB129157F9894A036487" +
                    "090807060504030201").HexToByteArray();

                writer.WriteBitString(containsSignature.AsReadOnlySpan().Slice(9, 256));

                // certificate
                writer.PopSequence();

                Assert.Equal(
                    ComprehensiveReadTests.MicrosoftDotComSslCertBytes.ByteArrayToHex(),
                    writer.Encode().ByteArrayToHex());
            }
        }

        private static void WriteRdn(AsnWriter writer, string oid, string value, UniversalTagNumber valueType)
        {
            writer.PushSetOf();
            writer.PushSequence();
            writer.WriteObjectIdentifier(oid);
            writer.WriteCharacterString(valueType, value);
            writer.PopSequence();
            writer.PopSetOf();
        }
    }
}
