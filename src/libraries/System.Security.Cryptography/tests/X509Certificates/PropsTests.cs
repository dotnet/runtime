// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class PropsTests
    {
        [Fact]
        public static void TestIssuer()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(
                    "CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    c.Issuer);
            }
        }

        [Fact]
        public static void TestSubject()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(
                    "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    c.Subject);
            }
        }

        [Fact]
        public static void TestSerialBytes()
        {
            byte[] expectedSerialBytes = "b00000000100dd9f3bd08b0aaf11b000000033".HexToByteArray();
            const string ExpectedSerialString = "33000000B011AF0A8BD03B9FDD0001000000B0";

            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] serial = c.GetSerialNumber();
                Assert.Equal(expectedSerialBytes, serial);

                Assert.Equal(ExpectedSerialString, c.SerialNumber);
                Assert.Equal(ExpectedSerialString, c.SerialNumberBytes.ByteArrayToHex());

                ReadOnlyMemory<byte> serial1 = c.SerialNumberBytes;
                ReadOnlyMemory<byte> serial2 = c.SerialNumberBytes;
                Assert.True(serial1.Span == serial2.Span, "Two calls to SerialNumberBytes return the same buffer");
            }
        }

        [Fact]
        public static void SerialNumberBytes_LifetimeIndependentOfCert()
        {
            const string ExpectedSerialString = "33000000B011AF0A8BD03B9FDD0001000000B0";

            ReadOnlyMemory<byte> serial;

            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                serial = cert.SerialNumberBytes;
            }

            Assert.Equal(ExpectedSerialString, serial.ByteArrayToHex());
        }

        [Theory]
        // Nice, normal serial number.
        [InlineData(TestFiles.MicrosoftRootCertFileName, "2A98A8770374E7B34195EBE04D9B17F6")]
        // Positive serial number which requires a padding byte to be interpreted positive.
        [InlineData(TestFiles.TestCertFileName, "00D01E4090000046520000000100000004")]
        // Negative serial number.
        //   RFC 2459: INTEGER
        //   RFC 3280: INTEGER, MUST be positive.
        //   RFC 5280: INTEGER, MUST be positive, MUST be 20 bytes or less.
        //       Readers SHOULD handle negative values.
        //       (Presumably readers also "should" handle long values created under the previous rules)
        [InlineData(TestFiles.MyCertFileName, "D5B5BC1C458A558845BFF51CB4DFF31C")]
        public static void TestSerialString(string fileName, string expectedSerial)
        {
            using (var c = new X509Certificate2(Path.Combine(TestFiles.TestDataFolder, fileName)))
            {
                Assert.Equal(expectedSerial, c.SerialNumber);
            }
        }

        [Fact]
        public static void TestThumbprint()
        {
            byte[] expectedThumbprint = "108e2ba23632620c427c570b6d9db51ac31387fe".HexToByteArray();
            string expectedThumbPrintString = "108E2BA23632620C427C570B6D9DB51AC31387FE";

            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] thumbPrint = c.GetCertHash();
                Assert.Equal(expectedThumbprint, thumbPrint);

                Assert.Equal(expectedThumbPrintString, c.Thumbprint);
            }
        }

        [Fact]
        public static void TestGetFormat()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                string format = c.GetFormat();
                Assert.Equal("X509", format);  // Only one format is supported so this is very predictable api...
            }
        }

        [Fact]
        public static void TestGetKeyAlgorithm()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                string keyAlgorithm = c.GetKeyAlgorithm();
                Assert.Equal("1.2.840.113549.1.1.1", keyAlgorithm);
            }
        }

        [Fact]
        public static void TestGetKeyAlgorithmParameters()
        {
            string expected = "0500";

            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] keyAlgorithmParameters = c.GetKeyAlgorithmParameters();
                Assert.Equal(expected.HexToByteArray(), keyAlgorithmParameters);

                string keyAlgorithmParametersString = c.GetKeyAlgorithmParametersString();
                Assert.Equal(expected, keyAlgorithmParametersString);
            }
        }

        [Fact]
        public static void TestGetPublicKey()
        {
            byte[] expectedPublicKey = (
                "3082010a0282010100e8af5ca2200df8287cbc057b7fadeeeb76ac28533f3adb" +
                "407db38e33e6573fa551153454a5cfb48ba93fa837e12d50ed35164eef4d7adb" +
                "137688b02cf0595ca9ebe1d72975e41b85279bf3f82d9e41362b0b40fbbe3bba" +
                "b95c759316524bca33c537b0f3eb7ea8f541155c08651d2137f02cba220b10b1" +
                "109d772285847c4fb91b90b0f5a3fe8bf40c9a4ea0f5c90a21e2aae3013647fd" +
                "2f826a8103f5a935dc94579dfb4bd40e82db388f12fee3d67a748864e162c425" +
                "2e2aae9d181f0e1eb6c2af24b40e50bcde1c935c49a679b5b6dbcef9707b2801" +
                "84b82a29cfbfa90505e1e00f714dfdad5c238329ebc7c54ac8e82784d37ec643" +
                "0b950005b14f6571c50203010001").HexToByteArray();

            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] publicKey = c.GetPublicKey();
                Assert.Equal(expectedPublicKey, publicKey);
            }
        }

        [Fact]
        public static void GetPublicKey_X509Signature_NoParameters()
        {
            // Normally RSA signature AlgorithmIdentifiers get represented as
            //
            // SEQUENCE(
            //    algorithm: OID(1.2.840.113549.1.1.5),
            //    parameters: NULL)
            //
            // where parameters: NULL is the byte sequence 05 00.
            //
            // This certificate has omitted the parameters section completely,
            // which while RFC compliant (it's labelled OPTIONAL) isn't what everyone
            // else does.  So this test ensures that we can read such a cert.
            const string PemEncodedCert = @"
-----BEGIN CERTIFICATE-----
MIIE4jCCAsygAwIBAgIEMTI0NjALBgkqhkiG9w0BAQUwgZgxCzAJBgNVBAYTAlVT
MQswCQYDVQQIDAJOWTEbMBkGA1UECgwSUVogSW5kdXN0cmllcywgTExDMRswGQYD
VQQLDBJRWiBJbmR1c3RyaWVzLCBMTEMxGTAXBgNVBAMMEHF6aW5kdXN0cmllcy5j
b20xJzAlBgkqhkiG9w0BCQEWGHN1cHBvcnRAcXppbmR1c3RyaWVzLmNvbTAeFw0x
NjA0MDYyMTAwMDBaFw0xNzA0MDcyMTAwMDBaMIGtMQswCQYDVQQGDAJDWjEXMBUG
A1UECAwOQ3plY2ggUmVwdWJsaWMxDTALBgNVBAcMBEJybm8xGTAXBgNVBAoMEHNt
c3RpY2tldCBzLnIuby4xGTAXBgNVBAsMEHNtc3RpY2tldCBzLnIuby4xHjAcBgNV
BAMMFXBva2xhZG5hLnNtc3RpY2tldC5jejEgMB4GCSqGSIb3DQEJAQwRaW5mb0Bz
bXN0aWNrZXQuY3owggEgMAsGCSqGSIb3DQEBAQOCAQ8AMIIBCgKCAQEAsDh05CAX
Wp29GTbjk3gzeCCe/1t7V3aNTwbtzkUtLZnbS9tge/+Iaqsz10IOWk3SndLhPIfa
KUvX/pnkq5CXIVyTTyRoFpyYrDfNoRmZ/3uTmMG50urk0Rg/+e4f2k32BfFTfB0W
3V169+QQ6Xvvuoyh62cppfi1msgFJ6WGmEF1r73Q6tK1vxfuA9wJfMWTl4Sg8nEf
9NXsTc9VAwGKRJbmTUN1b0xsqFvlFbxvaxPGwxNM29lXWlez5KEsh0sfUyTGQuTB
tu5JMC57TGvL0/TwgwrtOxQL5+N4lJAWnUQ+z3XXL694eSsuKlgw2yasO2ZwWnyz
eap2vnN/CifUgwIDAQABoyMwITAfBgNVHSMEGDAWgBSQplC3hNS56l/yBYQTeEXo
qXVUXDALBgkqhkiG9w0BAQUDggIBAHMNLagyKZYla3gR0KOhxiUWuFG2gU7uB2v+
zeqmIh6XxG4/39r6SJgUIimZ2aVQjYLa/fgrn5FRXhDqMumLJ3rWp8GB05evmdWl
WMQrb6E39jsFXuCzev6mCjiHxzGC2I7SRvFmnCj5fvOF81V5dLjU2PnCNqPym9Aw
XbEHVXTxpM9okSeq/EoeuTA5NHl/EySwYiGoexz0Ia51M5cw5W5go2Abmtqs4bbz
7OFeZKP9fd1p+C/ZnekgKq+3SJ9qbEiJxoPir3rG2N0mw7iI5pwvbCixY9irZh5o
Lrc5RvH4hdpygNSm4MYEuBykEW0tizkcVanGCUmGdjxM22Y9XdPgKitS04rVk/2U
C1Gszv9KvtmQ2P3/HWWWiOQgljc3SFqBltt6TqJTGCtLEbWRw6V+sw3SALoafvLg
tIsyWUsjM5LunRkUQ+HIsmKo42943TmgUvgRuuo0nsEFI5TS7Jh0iC/2gQEt7XGh
wzOTZ0HzM3oNnTphlXFLBwL9MUgWKbhu5Fg486dDMeQmZmhztW/+F/uHHYFisk+1
tmr2prSh5i4fD71t4p+EGJJQxM4wCiXRLzggIVGUAIrzynxO2vjYiMQxAUH3tdsX
JI6fq+e/mFZOE2XQmYu3/hQEw8/2F6usF1lyvwMZt2TgQZF1/g8gFVQUY2mGLM1z
Wry5FNNo
-----END CERTIFICATE-----";

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(PemEncodedCert);

            using (X509Certificate2 cert = new X509Certificate2(bytes))
            {
                Assert.Equal(Array.Empty<byte>(), cert.GetKeyAlgorithmParameters());
                Assert.Equal("1.2.840.113549.1.1.1", cert.GetKeyAlgorithm());
            }
        }

        [Fact]
        public static void TestNotBefore()
        {
            DateTime expected = new DateTime(2013, 1, 24, 22, 33, 39, DateTimeKind.Utc).ToLocalTime();

            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(expected, c.NotBefore);
            }
        }

        [Fact]
        public static void TestNotAfter()
        {
            DateTime expected = new DateTime(2014, 4, 24, 22, 33, 39, DateTimeKind.Utc).ToLocalTime();

            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(expected, c.NotAfter);
            }
        }

        [Fact]
        public static void TestRawData()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(TestData.MsCertificate, c.RawData);
            }
        }

        [Fact]
        public static void TestSignatureAlgorithm()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal("1.2.840.113549.1.1.5", c.SignatureAlgorithm.Value);
            }
        }

        [Fact]
        public static void TestHasPrivateKey()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.False(c.HasPrivateKey);
            }
        }

        [Fact]
        public static void TestPrivateKey()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Null(c.PrivateKey);
            }
        }

        [Fact]
        public static void TestVersion()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(3, c.Version);
            }
        }

        [Fact]
        public static void TestContentType()
        {
            X509ContentType ct = X509Certificate2.GetCertContentType(TestData.MsCertificate);
            Assert.Equal(X509ContentType.Cert, ct);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // MsCertificate not supported on Unix
        public static void TestArchive_Windows()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.False(c.Archived);

                c.Archived = true;
                Assert.True(c.Archived);

                c.Archived = false;
                Assert.False(c.Archived);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // MsCertificate not supported on Unix
        public static void TestArchive_Unix()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.False(c.Archived);

                Assert.Throws<PlatformNotSupportedException>(() => c.Archived = true);
                Assert.False(c.Archived);

                Assert.Throws<PlatformNotSupportedException>(() => c.Archived = false);
                Assert.False(c.Archived);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // MsCertificate not supported on Unix
        public static void TestFriendlyName_Windows()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(string.Empty, c.FriendlyName);

                c.FriendlyName = "This is a friendly name.";
                Assert.Equal("This is a friendly name.", c.FriendlyName);

                c.FriendlyName = null;
                Assert.Equal(string.Empty, c.FriendlyName);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // MsCertificate not supported on Unix
        public static void TestFriendlyName_Unix()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(string.Empty, c.FriendlyName);

                Assert.Throws<PlatformNotSupportedException>(() => c.FriendlyName = "This is a friendly name.");
                Assert.Equal(string.Empty, c.FriendlyName);

                Assert.Throws<PlatformNotSupportedException>(() => c.FriendlyName = null);
                Assert.Equal(string.Empty, c.FriendlyName);

                Assert.Throws<PlatformNotSupportedException>(() => c.FriendlyName = string.Empty);
                Assert.Equal(string.Empty, c.FriendlyName);
            }
        }

        [Fact]
        public static void TestSubjectName()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(
                    "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    c.SubjectName.Name);
            }
        }

        [Fact]
        public static void TestIssuerName()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Equal(
                    "CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    c.IssuerName.Name);

            }
        }

        [Fact]
        public static void TestGetNameInfo()
        {
            using (var c = new X509Certificate2(TestData.MsCertificate))
            {
                string s;

                s = c.GetNameInfo(X509NameType.SimpleName, false);
                Assert.Equal("Microsoft Corporation", s);
                s = c.GetNameInfo(X509NameType.SimpleName, true);
                Assert.Equal("Microsoft Code Signing PCA", s);

                s = c.GetNameInfo(X509NameType.EmailName, false);
                Assert.Equal("", s);
                s = c.GetNameInfo(X509NameType.EmailName, true);
                Assert.Equal("", s);

                s = c.GetNameInfo(X509NameType.UpnName, false);
                Assert.Equal("", s);
                s = c.GetNameInfo(X509NameType.UpnName, true);
                Assert.Equal("", s);

                s = c.GetNameInfo(X509NameType.UrlName, false);
                Assert.Equal("", s);
                s = c.GetNameInfo(X509NameType.UrlName, true);
                Assert.Equal("", s);

                s = c.GetNameInfo(X509NameType.DnsName, false);
                Assert.Equal("Microsoft Corporation", s);
                s = c.GetNameInfo(X509NameType.DnsName, true);
                Assert.Equal("Microsoft Code Signing PCA", s);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void GetNameInfo_HandlesUtf8Encoding(bool issuer)
        {
            using (X509Certificate2 c = new X509Certificate2(TestData.CertificateWithUtf8))
            {
                // Russian word for "potato" in Cyrillic, kartoshka.
                string expected = "\u043A\u0430\u0440\u0442\u043E\u0448\u043A\u0430";
                string cn = c.GetNameInfo(X509NameType.SimpleName, issuer);
                Assert.Equal(expected, cn);
            }
        }

        [Fact]
        public static void ComplexGetNameInfo_SimpleName_Cert()
        {
            TestComplexGetNameInfo("cn.subject.example.org", X509NameType.SimpleName, false);
        }

        [Fact]
        public static void ComplexGetNameInfo_SimpleName_Issuer()
        {
            TestComplexGetNameInfo("cn.issuer.example.org", X509NameType.SimpleName, true);
        }

        [Fact]
        public static void ComplexGetNameInfo_EmailName_Cert()
        {
            TestComplexGetNameInfo("sanemail1@example.org", X509NameType.EmailName, false);
        }

        [Fact]
        public static void ComplexGetNameInfo_EmailName_Issuer()
        {
            TestComplexGetNameInfo("ianemail1@example.org", X509NameType.EmailName, true);
        }

        [Fact]
        public static void ComplexGetNameInfo_UpnName_Cert()
        {
            TestComplexGetNameInfo("subjectupn1@example.org", X509NameType.UpnName, false);
        }

        [Fact]
        public static void ComplexGetNameInfo_UpnName_Issuer()
        {
            TestComplexGetNameInfo("issuerupn1@example.org", X509NameType.UpnName, true);
        }

        [Fact]
        public static void ComplexGetNameInfo_DnsName_Cert()
        {
            TestComplexGetNameInfo("dns1.subject.example.org", X509NameType.DnsName, false);
        }

        [Fact]
        public static void ComplexGetNameInfo_DnsName_Issuer()
        {
            TestComplexGetNameInfo("dns1.issuer.example.org", X509NameType.DnsName, true);
        }

        [Fact]
        public static void ComplexGetNameInfo_DnsFromAlternativeName_Cert()
        {
            TestComplexGetNameInfo("dns1.subject.example.org", X509NameType.DnsFromAlternativeName, false);
        }

        [Fact]
        public static void ComplexGetNameInfo_DnsFromAlternativeName_Issuer()
        {
            TestComplexGetNameInfo("dns1.issuer.example.org", X509NameType.DnsFromAlternativeName, true);
        }

        [Fact]
        public static void ComplexGetNameInfo_UrlName_Cert()
        {
            TestComplexGetNameInfo("http://uri1.subject.example.org/", X509NameType.UrlName, false);
        }

        [Fact]
        public static void ComplexGetNameInfo_UrlName_Issuer()
        {
            TestComplexGetNameInfo("http://uri1.issuer.example.org/", X509NameType.UrlName, true);
        }

        private static void TestComplexGetNameInfo(string expected, X509NameType nameType, bool forIssuer)
        {
            // ComplexNameInfoCert has the following characteristics:
            //   Subject: E=subjectemail@example.org, CN=cn.subject.example.org, OU=ExampleOU, O=ExampleO, L=Locality, ST=State, C=Country
            //   Issuer: E=issueremail@example.org, CN=cn.issuer.example.org, OU=ExampleOU, O=ExampleO, L=Locality, ST=State, C=Country
            //   Subject Alternative Names:
            //     DNS Name=dns1.subject.example.org
            //     DNS Name=dns2.subject.example.org
            //     RFC822 Name=sanemail1@example.org
            //     RFC822 Name=sanemail2@example.org
            //     Other Name:
            //       Principal Name=subjectupn1@example.org
            //     Other Name:
            //       Principal Name=subjectupn2@example.org
            //     URL=http://uri1.subject.example.org/
            //     URL=http://uri2.subject.example.org/
            //   Issuer Alternative Names:
            //     DNS Name=dns1.issuer.example.org
            //     DNS Name=dns2.issuer.example.org
            //     RFC822 Name=ianemail1@example.org
            //     RFC822 Name=ianemail2@example.org
            //     Other Name:
            //       Principal Name=issuerupn1@example.org
            //     Other Name:
            //       Principal Name=issuerupn2@example.org
            //     URL=http://uri1.issuer.example.org/
            //     URL=http://uri2.issuer.example.org/

            string result;

            using (var cert = new X509Certificate2(TestData.ComplexNameInfoCert))
            {
                result = cert.GetNameInfo(nameType, forIssuer);
            }

            Assert.Equal(expected, result);
        }
    }
}
