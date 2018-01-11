// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

using X509KeyUsageCSharpStyle=System.Security.Cryptography.Tests.Asn1.ReadNamedBitList.X509KeyUsageCSharpStyle;

namespace System.Security.Cryptography.Tests.Asn1
{
    public static class ComprehensiveReadTests
    {
        [Fact]
        public static void ReadMicrosoftComCert()
        {
            byte[] bytes = MicrosoftDotComSslCertBytes;
            AsnReader fileReader = new AsnReader(bytes, AsnEncodingRules.DER);

            AsnReader certReader = fileReader.ReadSequence();
            Assert.False(fileReader.HasData, "fileReader.HasData");

            AsnReader tbsCertReader = certReader.ReadSequence();
            AsnReader sigAlgReader = certReader.ReadSequence();

            Assert.True(
                certReader.TryGetPrimitiveBitStringValue(
                    out int unusedBitCount,
                    out ReadOnlyMemory<byte> signature),
                "certReader.TryGetBitStringBytes");

            Assert.Equal(0, unusedBitCount);
            AssertRefSame(signature, ref bytes[1176], "Signature is a ref to bytes[1176]");

            Assert.False(certReader.HasData, "certReader.HasData");

            AsnReader versionExplicitWrapper = tbsCertReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            Assert.True(versionExplicitWrapper.TryReadInt32(out int certVersion));
            Assert.Equal(2, certVersion);
            Assert.False(versionExplicitWrapper.HasData, "versionExplicitWrapper.HasData");

            ReadOnlyMemory<byte> serialBytes = tbsCertReader.GetIntegerBytes();
            AssertRefSame(serialBytes, ref bytes[15], "Serial number starts at bytes[15]");

            AsnReader tbsSigAlgReader = tbsCertReader.ReadSequence();
            Assert.Equal("1.2.840.113549.1.1.11", tbsSigAlgReader.ReadObjectIdentifierAsString());
            Assert.True(tbsSigAlgReader.HasData, "tbsSigAlgReader.HasData before ReadNull");
            tbsSigAlgReader.ReadNull();
            Assert.False(tbsSigAlgReader.HasData, "tbsSigAlgReader.HasData after ReadNull");

            AsnReader issuerReader = tbsCertReader.ReadSequence();
            Asn1Tag printableString = new Asn1Tag(UniversalTagNumber.PrintableString);
            AssertRdn(issuerReader, "2.5.4.6", 57, printableString, bytes, "issuer[C]");
            AssertRdn(issuerReader, "2.5.4.10", 70, printableString, bytes, "issuer[O]");
            AssertRdn(issuerReader, "2.5.4.11", 101, printableString, bytes, "issuer[OU]");
            AssertRdn(issuerReader, "2.5.4.3", 134, printableString, bytes, "issuer[CN]");
            Assert.False(issuerReader.HasData, "issuerReader.HasData");

            AsnReader validityReader = tbsCertReader.ReadSequence();
            Assert.Equal(new DateTimeOffset(2014, 10, 15, 0, 0, 0, TimeSpan.Zero), validityReader.GetUtcTime());
            Assert.Equal(new DateTimeOffset(2016, 10, 15, 23, 59, 59, TimeSpan.Zero), validityReader.GetUtcTime());
            Assert.False(validityReader.HasData, "validityReader.HasData");

            AsnReader subjectReader = tbsCertReader.ReadSequence();
            Asn1Tag utf8String = new Asn1Tag(UniversalTagNumber.UTF8String);
            AssertRdn(subjectReader, "1.3.6.1.4.1.311.60.2.1.3", 220, printableString, bytes, "subject[EV Country]");
            AssertRdn(subjectReader, "1.3.6.1.4.1.311.60.2.1.2", 241, utf8String, bytes, "subject[EV State]", "Washington");
            AssertRdn(subjectReader, "2.5.4.15", 262, printableString, bytes, "subject[Business Category]");
            AssertRdn(subjectReader, "2.5.4.5", 293, printableString, bytes, "subject[Serial Number]");
            AssertRdn(subjectReader, "2.5.4.6", 313, printableString, bytes, "subject[C]");
            AssertRdn(subjectReader, "2.5.4.17", 326, utf8String, bytes, "subject[Postal Code]", "98052");
            AssertRdn(subjectReader, "2.5.4.8", 342, utf8String, bytes, "subject[ST]", "Washington");
            AssertRdn(subjectReader, "2.5.4.7", 363, utf8String, bytes, "subject[L]", "Redmond");
            AssertRdn(subjectReader, "2.5.4.9", 381, utf8String, bytes, "subject[Street Address]", "1 Microsoft Way");
            AssertRdn(subjectReader, "2.5.4.10", 407, utf8String, bytes, "subject[O]", "Microsoft Corporation");
            AssertRdn(subjectReader, "2.5.4.11", 439, utf8String, bytes, "subject[OU]", "MSCOM");
            AssertRdn(subjectReader, "2.5.4.3", 455, utf8String, bytes, "subject[CN]", "www.microsoft.com");
            Assert.False(subjectReader.HasData, "subjectReader.HasData");

            AsnReader subjectPublicKeyInfo = tbsCertReader.ReadSequence();
            AsnReader spkiAlgorithm = subjectPublicKeyInfo.ReadSequence();
            Assert.Equal("1.2.840.113549.1.1.1", spkiAlgorithm.ReadObjectIdentifierAsString());
            spkiAlgorithm.ReadNull();
            Assert.False(spkiAlgorithm.HasData, "spkiAlgorithm.HasData");

            Assert.True(
                subjectPublicKeyInfo.TryGetPrimitiveBitStringValue(
                    out unusedBitCount,
                    out ReadOnlyMemory<byte> encodedPublicKey),
                "subjectPublicKeyInfo.TryGetBitStringBytes");

            Assert.Equal(0, unusedBitCount);
            AssertRefSame(encodedPublicKey, ref bytes[498], "Encoded public key starts at byte 498");

            Assert.False(subjectPublicKeyInfo.HasData, "subjectPublicKeyInfo.HasData");

            AsnReader publicKeyReader = new AsnReader(encodedPublicKey, AsnEncodingRules.DER);
            AsnReader rsaPublicKeyReader = publicKeyReader.ReadSequence();
            AssertRefSame(rsaPublicKeyReader.GetIntegerBytes(), ref bytes[506], "RSA Modulus is at bytes[502]");
            Assert.True(rsaPublicKeyReader.TryReadInt32(out int rsaExponent));
            Assert.Equal(65537, rsaExponent);
            Assert.False(rsaPublicKeyReader.HasData, "rsaPublicKeyReader.HasData");
            Assert.False(publicKeyReader.HasData, "publicKeyReader.HasData");

            AsnReader extensionsContainer = tbsCertReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
            AsnReader extensions = extensionsContainer.ReadSequence();
            Assert.False(extensionsContainer.HasData, "extensionsContainer.HasData");

            AsnReader sanExtension = extensions.ReadSequence();
            Assert.Equal("2.5.29.17", sanExtension.ReadObjectIdentifierAsString());
            Assert.True(sanExtension.TryGetPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> sanExtensionBytes));
            Assert.False(sanExtension.HasData, "sanExtension.HasData");

            AsnReader sanExtensionPayload = new AsnReader(sanExtensionBytes, AsnEncodingRules.DER);
            AsnReader sanExtensionValue = sanExtensionPayload.ReadSequence();
            Assert.False(sanExtensionPayload.HasData, "sanExtensionPayload.HasData");
            Asn1Tag dnsName = new Asn1Tag(TagClass.ContextSpecific, 2);
            Assert.Equal("www.microsoft.com", sanExtensionValue.GetCharacterString(dnsName, UniversalTagNumber.IA5String));
            Assert.Equal("wwwqa.microsoft.com", sanExtensionValue.GetCharacterString(dnsName, UniversalTagNumber.IA5String));
            Assert.False(sanExtensionValue.HasData, "sanExtensionValue.HasData");

            AsnReader basicConstraints = extensions.ReadSequence();
            Assert.Equal("2.5.29.19", basicConstraints.ReadObjectIdentifierAsString());
            Assert.True(basicConstraints.TryGetPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> basicConstraintsBytes));

            AsnReader basicConstraintsPayload = new AsnReader(basicConstraintsBytes, AsnEncodingRules.DER);
            AsnReader basicConstraintsValue = basicConstraintsPayload.ReadSequence();
            Assert.False(basicConstraintsValue.HasData, "basicConstraintsValue.HasData");
            Assert.False(basicConstraintsPayload.HasData, "basicConstraintsPayload.HasData");

            AsnReader keyUsageExtension = extensions.ReadSequence();
            Assert.Equal("2.5.29.15", keyUsageExtension.ReadObjectIdentifierAsString());
            Assert.True(keyUsageExtension.ReadBoolean(), "keyUsageExtension.ReadBoolean() (IsCritical)");
            Assert.True(keyUsageExtension.TryGetPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> keyUsageBytes));

            AsnReader keyUsagePayload = new AsnReader(keyUsageBytes, AsnEncodingRules.DER);

            Assert.Equal(
                X509KeyUsageCSharpStyle.DigitalSignature | X509KeyUsageCSharpStyle.KeyEncipherment,
                keyUsagePayload.GetNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.False(keyUsagePayload.HasData, "keyUsagePayload.HasData");

            AssertExtension(extensions, "2.5.29.37", false, 863, bytes);
            AssertExtension(extensions, "2.5.29.32", false, 894, bytes);
            AssertExtension(extensions, "2.5.29.35", false, 998, bytes);
            AssertExtension(extensions, "2.5.29.31", false, 1031, bytes);
            AssertExtension(extensions, "1.3.6.1.5.5.7.1.1", false, 1081, bytes);
            Assert.False(extensions.HasData, "extensions.HasData");

            Assert.Equal("1.2.840.113549.1.1.11", sigAlgReader.ReadObjectIdentifierAsString());
            sigAlgReader.ReadNull();
            Assert.False(sigAlgReader.HasData);
        }

        private static void AssertExtension(AsnReader extensions, string oid, bool critical, int index, byte[] bytes)
        {
            AsnReader extension = extensions.ReadSequence();
            Assert.Equal(oid, extension.ReadObjectIdentifierAsString());

            if (critical)
            {
                Assert.True(extension.ReadBoolean(), $"{oid} is critical");
            }

            Assert.True(extension.TryGetPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> extensionBytes));
            AssertRefSame(extensionBytes, ref bytes[index], $"{oid} extension value is at byte {index}");
        }

        private static void AssertRdn(
            AsnReader reader,
            string atvOid,
            int offset,
            Asn1Tag valueTag,
            byte[] bytes,
            string label,
            string stringValue=null)
        {
            AsnReader rdn = reader.ReadSetOf();
            AsnReader attributeTypeAndValue = rdn.ReadSequence();
            Assert.Equal(atvOid, attributeTypeAndValue.ReadObjectIdentifierAsString());

            ReadOnlyMemory<byte> value = attributeTypeAndValue.GetEncodedValue();
            ReadOnlySpan<byte> valueSpan = value.Span;

            Assert.True(Asn1Tag.TryParse(valueSpan, out Asn1Tag actualTag, out int bytesRead));
            Assert.Equal(1, bytesRead);
            Assert.Equal(valueTag, actualTag);

            AssertRefSame(
                ref MemoryMarshal.GetReference(valueSpan),
                ref bytes[offset],
                $"{label} is at bytes[{offset}]");

            if (stringValue != null)
            {
                AsnReader valueReader = new AsnReader(value, AsnEncodingRules.DER);
                Assert.Equal(stringValue, valueReader.GetCharacterString((UniversalTagNumber)valueTag.TagValue));
                Assert.False(valueReader.HasData, "valueReader.HasData");
            }

            Assert.False(attributeTypeAndValue.HasData, $"attributeTypeAndValue.HasData ({label})");
            Assert.False(rdn.HasData, $"rdn.HasData ({label})");
        }

        private static void AssertRefSame(ReadOnlyMemory<byte> a, ref byte b, string msg)
        {
            AssertRefSame(ref MemoryMarshal.GetReference(a.Span), ref b, msg);
        }

        private static void AssertRefSame(ref byte a, ref byte b, string msg)
        {
            Assert.True(Unsafe.AreSame(ref a, ref b), msg);
        }

        internal static readonly byte[] MicrosoftDotComSslCertBytes = (
            "308205943082047CA00302010202103DF70C5D9903F8D8868B9B8CCF20DF6930" +
            "0D06092A864886F70D01010B05003077310B3009060355040613025553311D30" +
            "1B060355040A131453796D616E74656320436F72706F726174696F6E311F301D" +
            "060355040B131653796D616E746563205472757374204E6574776F726B312830" +
            "260603550403131F53796D616E74656320436C61737320332045562053534C20" +
            "4341202D204733301E170D3134313031353030303030305A170D313631303135" +
            "3233353935395A3082010F31133011060B2B0601040182373C02010313025553" +
            "311B3019060B2B0601040182373C0201020C0A57617368696E67746F6E311D30" +
            "1B060355040F131450726976617465204F7267616E697A6174696F6E31123010" +
            "06035504051309363030343133343835310B3009060355040613025553310E30" +
            "0C06035504110C0539383035323113301106035504080C0A57617368696E6774" +
            "6F6E3110300E06035504070C075265646D6F6E643118301606035504090C0F31" +
            "204D6963726F736F667420576179311E301C060355040A0C154D6963726F736F" +
            "667420436F72706F726174696F6E310E300C060355040B0C054D53434F4D311A" +
            "301806035504030C117777772E6D6963726F736F66742E636F6D30820122300D" +
            "06092A864886F70D01010105000382010F003082010A0282010100A46861FA9D" +
            "5DB763633BF5A64EF6E7C2C2367F48D2D46643A22DFCFCCB24E58A14D0F06BDC" +
            "956437F2A56BA4BEF70BA361BF12964A0D665AFD84B0F7494C8FA4ABC5FCA2E0" +
            "17C06178AEF2CDAD1B5F18E997A14B965C074E8F564970607276B00583932240" +
            "FE6E2DD013026F9AE13D7C91CC07C4E1E8E87737DC06EF2B575B89D62EFE4685" +
            "9F8255A123692A706C68122D4DAFE11CB205A7B3DE06E553F7B95F978EF8601A" +
            "8DF819BF32040BDF92A0DE0DF269B4514282E17AC69934E8440A48AB9D1F5DF8" +
            "9A502CEF6DFDBE790045BD45E0C94E5CA8ADD76A013E9C978440FC8A9E2A9A49" +
            "40B2460819C3E302AA9C9F355AD754C86D3ED77DDAA3DA13810B4D0203010001" +
            "A38201803082017C30310603551D11042A302882117777772E6D6963726F736F" +
            "66742E636F6D821377777771612E6D6963726F736F66742E636F6D3009060355" +
            "1D1304023000300E0603551D0F0101FF0404030205A0301D0603551D25041630" +
            "1406082B0601050507030106082B0601050507030230660603551D20045F305D" +
            "305B060B6086480186F84501071706304C302306082B06010505070201161768" +
            "747470733A2F2F642E73796D63622E636F6D2F637073302506082B0601050507" +
            "020230191A1768747470733A2F2F642E73796D63622E636F6D2F727061301F06" +
            "03551D230418301680140159ABE7DD3A0B59A66463D6CF200757D591E76A302B" +
            "0603551D1F042430223020A01EA01C861A687474703A2F2F73722E73796D6362" +
            "2E636F6D2F73722E63726C305706082B06010505070101044B3049301F06082B" +
            "060105050730018613687474703A2F2F73722E73796D63642E636F6D30260608" +
            "2B06010505073002861A687474703A2F2F73722E73796D63622E636F6D2F7372" +
            "2E637274300D06092A864886F70D01010B0500038201010015F8505B627ED7F9" +
            "F96707097E93A51E7A7E05A3D420A5C258EC7A1CFE1843EC20ACF728AAFA7A1A" +
            "1BC222A7CDBF4AF90AA26DEEB3909C0B3FB5C78070DAE3D645BFCF840A4A3FDD" +
            "988C7B3308BFE4EB3FD66C45641E96CA3352DBE2AEB4488A64A9C5FB96932BA7" +
            "0059CE92BD278B41299FD213471BD8165F924285AE3ECD666C703885DCA65D24" +
            "DA66D3AFAE39968521995A4C398C7DF38DFA82A20372F13D4A56ADB21B582254" +
            "9918015647B5F8AC131CC5EB24534D172BC60218A88B65BCF71C7F388CE3E0EF" +
            "697B4203720483BB5794455B597D80D48CD3A1D73CBBC609C058767D1FF060A6" +
            "09D7E3D4317079AF0CD0A8A49251AB129157F9894A036487").HexToByteArray();
    }
}
