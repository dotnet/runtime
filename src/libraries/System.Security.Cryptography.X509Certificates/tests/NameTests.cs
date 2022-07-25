// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class NameTests
    {
        [Fact]
        public static void TestEncode()
        {
            byte[] expectedEncoding = "300e310c300a06035504031303466f6f".HexToByteArray();

            X500DistinguishedName n = new X500DistinguishedName("CN=Foo");
            string s = n.Name;
            Assert.Equal("CN=Foo", s);
            byte[] rawData = n.RawData;
            Assert.Equal(expectedEncoding, rawData);
        }

        [Fact]
        public static void TestDecode()
        {
            byte[] encoding = "300e310c300a06035504031303466f6f".HexToByteArray();

            X500DistinguishedName n = new X500DistinguishedName(encoding);
            string s = n.Name;
            Assert.Equal("CN=Foo", s);
            byte[] rawData = n.RawData;
            Assert.Equal(encoding, rawData);
        }

        [Theory]
        [InlineData(X500DistinguishedNameFlags.UseCommas)]
        [InlineData(X500DistinguishedNameFlags.UseSemicolons)]
        [InlineData(X500DistinguishedNameFlags.UseNewLines)]
        public static void TestDecodeFormats(X500DistinguishedNameFlags format)
        {
            // The Issuer field from the Microsoft.com test cert.
            byte[] encoding = (
                "3077310B3009060355040613025553311D301B060355040A131453796D616E74" +
                "656320436F72706F726174696F6E311F301D060355040B131653796D616E7465" +
                "63205472757374204E6574776F726B312830260603550403131F53796D616E74" +
                "656320436C61737320332045562053534C204341202D204733").HexToByteArray();

            X500DistinguishedName name = new X500DistinguishedName(encoding);
            string delimiter;

            switch (format)
            {
                case X500DistinguishedNameFlags.UseCommas:
                    delimiter = ", ";
                    break;
                case X500DistinguishedNameFlags.UseSemicolons:
                    delimiter = "; ";
                    break;
                case X500DistinguishedNameFlags.UseNewLines:
                    delimiter = Environment.NewLine;
                    break;
                default:
                    throw new InvalidOperationException("No handler for format: " + format);
            }

            string expected = string.Format(
                "C=US{0}O=Symantec Corporation{0}OU=Symantec Trust Network{0}CN=Symantec Class 3 EV SSL CA - G3",
                delimiter);

            string actual = name.Decode(format);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void ForceUtf8EncodingWithFlagWhenEncoding()
        {
            // Even though these values are encodable as a PrintableString, the flag should force them to be encoded
            // as UTF8String.
            X500DistinguishedName name = new X500DistinguishedName(
                "CN=potato, O=jicama",
                X500DistinguishedNameFlags.ForceUTF8Encoding);

            ReadOnlySpan<byte> expectedDer = new byte[]
            {
                0x30, 0x22,
                0x31, 0x0F,
                0x30, 0x0D,
                    0x06, 0x03, 0x55, 0x04, 0x03, // id-at-commonName OID
                    0x0C, 0x06, 0x70, 0x6F, 0x74, 0x61, 0x74, 0x6F, // 0x0C is UTF8String
                0x31, 0x0F,
                0x30, 0x0D,
                    0x06, 0x03, 0x55, 0x04, 0x0A,  // id-at-organizatioName OID
                    0x0C, 0x06, 0x6A, 0x69, 0x63, 0x61, 0x6D, 0x61, // 0x0C is UTF8String
            };

            AssertExtensions.SequenceEqual(expectedDer, name.RawData);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestFormat(bool multiLine)
        {
            // The Issuer field from the Microsoft.com test cert.
            byte[] encoding = (
                "3077310B3009060355040613025553311D301B060355040A131453796D616E74" +
                "656320436F72706F726174696F6E311F301D060355040B131653796D616E7465" +
                "63205472757374204E6574776F726B312830260603550403131F53796D616E74" +
                "656320436C61737320332045562053534C204341202D204733").HexToByteArray();

            X500DistinguishedName name = new X500DistinguishedName(encoding);
            string formatted = name.Format(multiLine);
            string expected;

            if (multiLine)
            {
                expected = string.Format(
                    "C=US{0}O=Symantec Corporation{0}OU=Symantec Trust Network{0}CN=Symantec Class 3 EV SSL CA - G3{0}",
                    Environment.NewLine);
            }
            else
            {
                expected = "C=US, O=Symantec Corporation, OU=Symantec Trust Network, CN=Symantec Class 3 EV SSL CA - G3";
            }

            Assert.Equal(expected, formatted);
        }
    }
}
