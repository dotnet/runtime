// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class ExtensionsTests
    {
        [Fact]
        public static void ReadExtensions()
        {
            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificate))
            {
                X509ExtensionCollection exts = c.Extensions;
                int count = exts.Count;
                Assert.Equal(6, count);

                X509Extension[] extensions = new X509Extension[count];
                exts.CopyTo(extensions, 0);
                extensions = extensions.OrderBy(e => e.Oid.Value).ToArray();

                // There are an awful lot of magic-looking values in this large test.
                // These values are embedded within the certificate, and the test is
                // just verifying the object interpretation. In the event the test data
                // (TestData.MsCertificate) is replaced, this whole body will need to be
                // redone.

                {
                    // Authority Information Access
                    X509Extension aia = extensions[0];
                    Assert.Equal("1.3.6.1.5.5.7.1.1", aia.Oid.Value);
                    Assert.False(aia.Critical);

                    byte[] expectedDer = (
                        "304c304a06082b06010505073002863e687474703a2f2f7777772e6d" +
                        "6963726f736f66742e636f6d2f706b692f63657274732f4d6963436f" +
                        "645369675043415f30382d33312d323031302e637274").HexToByteArray();

                    Assert.Equal(expectedDer, aia.RawData);
                }

                {
                    // Subject Key Identifier
                    X509Extension skid = extensions[1];
                    Assert.Equal("2.5.29.14", skid.Oid.Value);
                    Assert.False(skid.Critical);

                    byte[] expected = "04145971a65a334dda980780ff841ebe87f9723241f2".HexToByteArray();
                    Assert.Equal(expected, skid.RawData);

                    Assert.True(skid is X509SubjectKeyIdentifierExtension);
                    X509SubjectKeyIdentifierExtension rich = (X509SubjectKeyIdentifierExtension)skid;
                    Assert.Equal("5971A65A334DDA980780FF841EBE87F9723241F2", rich.SubjectKeyIdentifier);
                }

                {
                    // Subject Alternative Names
                    X509Extension sans = extensions[2];
                    Assert.Equal("2.5.29.17", sans.Oid.Value);
                    Assert.False(sans.Critical);

                    byte[] expected = (
                        "3048a4463044310d300b060355040b13044d4f505231333031060355" +
                        "0405132a33313539352b34666166306237312d616433372d34616133" +
                        "2d613637312d373662633035323334346164").HexToByteArray();

                    Assert.Equal(expected, sans.RawData);
                }

                {
                    // CRL Distribution Points
                    X509Extension cdps = extensions[3];
                    Assert.Equal("2.5.29.31", cdps.Oid.Value);
                    Assert.False(cdps.Critical);

                    byte[] expected = (
                        "304d304ba049a0478645687474703a2f2f63726c2e6d6963726f736f" +
                        "66742e636f6d2f706b692f63726c2f70726f64756374732f4d696343" +
                        "6f645369675043415f30382d33312d323031302e63726c").HexToByteArray();

                    Assert.Equal(expected, cdps.RawData);
                }

                {
                    // Authority Key Identifier
                    X509Extension akid = extensions[4];
                    Assert.Equal("2.5.29.35", akid.Oid.Value);
                    Assert.False(akid.Critical);

                    byte[] expected = "30168014cb11e8cad2b4165801c9372e331616b94c9a0a1f".HexToByteArray();
                    Assert.Equal(expected, akid.RawData);
                }

                {
                    // Extended Key Usage (X.509/2000 says Extended, Win32/NetFX say Enhanced)
                    X509Extension eku = extensions[5];
                    Assert.Equal("2.5.29.37", eku.Oid.Value);
                    Assert.False(eku.Critical);

                    byte[] expected = "300a06082b06010505070303".HexToByteArray();
                    Assert.Equal(expected, eku.RawData);

                    Assert.True(eku is X509EnhancedKeyUsageExtension);
                    X509EnhancedKeyUsageExtension rich = (X509EnhancedKeyUsageExtension)eku;

                    OidCollection usages = rich.EnhancedKeyUsages;
                    Assert.Equal(1, usages.Count);

                    Oid oid = usages[0];
                    // Code Signing
                    Assert.Equal("1.3.6.1.5.5.7.3.3", oid.Value);
                }
            }
        }

        [Fact]
        public static void KeyUsageExtensionDefaultCtor()
        {
            X509KeyUsageExtension e = new X509KeyUsageExtension();
            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.15", oidValue);
            byte[] r = e.RawData;
            Assert.Null(r);
            X509KeyUsageFlags keyUsages = e.KeyUsages;
            Assert.Equal(X509KeyUsageFlags.None, keyUsages);
        }

        [Fact]
        public static void KeyUsageExtension_CrlSign()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.CrlSign, false, "03020102".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_DataEncipherment()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.DataEncipherment, false, "03020410".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_DecipherOnly()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.DecipherOnly, false, "0303070080".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_DigitalSignature()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false, "03020780".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_EncipherOnly()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.EncipherOnly, false, "03020001".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_KeyAgreement()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.KeyAgreement, false, "03020308".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_KeyCertSign()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.KeyCertSign, false, "03020204".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_KeyEncipherment()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, false, "03020520".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_None()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.None, false, "030100".HexToByteArray());
        }

        [Fact]
        public static void KeyUsageExtension_NonRepudiation()
        {
            TestKeyUsageExtension(X509KeyUsageFlags.NonRepudiation, false, "03020640".HexToByteArray());
        }

        [Fact]
        public static void BasicConstraintsExtensionDefault()
        {
            X509BasicConstraintsExtension e = new X509BasicConstraintsExtension();
            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.19", oidValue);

            byte[] rawData = e.RawData;
            Assert.Null(rawData);

            Assert.False(e.CertificateAuthority);
            Assert.False(e.HasPathLengthConstraint);
            Assert.Equal(0, e.PathLengthConstraint);
        }

        [Theory]
        [MemberData(nameof(BasicConstraintsData))]
        public static void BasicConstraintsExtensionEncode(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint,
            bool critical,
            string expectedDerString)
        {
            X509BasicConstraintsExtension ext = new X509BasicConstraintsExtension(
                certificateAuthority,
                hasPathLengthConstraint,
                pathLengthConstraint,
                critical);

            byte[] expectedDer = expectedDerString.HexToByteArray();
            Assert.Equal(expectedDer, ext.RawData);
        }

        [Theory]
        [MemberData(nameof(BasicConstraintsData))]
        public static void BasicConstraintsExtensionDecode(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint,
            bool critical,
            string rawDataString)
        {
            byte[] rawData = rawDataString.HexToByteArray();
            int expectedPathLengthConstraint = hasPathLengthConstraint ? pathLengthConstraint : 0;

            X509BasicConstraintsExtension ext = new X509BasicConstraintsExtension(new AsnEncodedData(rawData), critical);
            Assert.Equal(certificateAuthority, ext.CertificateAuthority);
            Assert.Equal(hasPathLengthConstraint, ext.HasPathLengthConstraint);
            Assert.Equal(expectedPathLengthConstraint, ext.PathLengthConstraint);
        }

        public static object[][] BasicConstraintsData = new object[][]
        {
            new object[] { false, false, 0, false, "3000" },
            new object[] { false, false, 121, false, "3000" },
            new object[] { true, false, 0, false, "30030101ff" },
            new object[] { false, true, 0, false, "3003020100" },
            new object[] { false, true, 7654321, false, "3005020374cbb1" },
            new object[] { true, true, 559, false, "30070101ff0202022f" },
        };

        [Fact]
        public static void EnhancedKeyUsageExtensionDefault()
        {
            X509EnhancedKeyUsageExtension e = new X509EnhancedKeyUsageExtension();
            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.37", oidValue);

            byte[] rawData = e.RawData;
            Assert.Null(rawData);

            OidCollection usages = e.EnhancedKeyUsages;
            Assert.Equal(0, usages.Count);
        }

        [Fact]
        public static void EnhancedKeyUsageExtension_Empty()
        {
            OidCollection usages = new OidCollection();
            TestEnhancedKeyUsageExtension(usages, false, "3000".HexToByteArray());
        }

        [Fact]
        public static void EnhancedKeyUsageExtension_2Oids()
        {
            Oid oid1 = new Oid("1.3.6.1.5.5.7.3.1");
            Oid oid2 = new Oid("1.3.6.1.4.1.311.10.3.1");
            OidCollection usages = new OidCollection();
            usages.Add(oid1);
            usages.Add(oid2);

            TestEnhancedKeyUsageExtension(usages, false, "301606082b06010505070301060a2b0601040182370a0301".HexToByteArray());
        }

        [Theory]
        [InlineData("1")]
        [InlineData("3.0")]
        [InlineData("Invalid Value")]
        public static void EnhancedKeyUsageExtension_InvalidOid(string invalidOidValue)
        {
            OidCollection oids = new OidCollection
            {
                new Oid(invalidOidValue)
            };

            Assert.ThrowsAny<CryptographicException>(() => new X509EnhancedKeyUsageExtension(oids, false));
        }

        [Fact]
        public static void SubjectKeyIdentifierExtensionDefault()
        {
            X509SubjectKeyIdentifierExtension e = new X509SubjectKeyIdentifierExtension();

            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.14", oidValue);

            byte[] rawData = e.RawData;
            Assert.Null(rawData);

            string skid = e.SubjectKeyIdentifier;
            Assert.Null(skid);
        }

        [Fact]
        public static void SubjectKeyIdentifierExtension_Bytes()
        {
            byte[] sk = { 1, 2, 3, 4 };
            X509SubjectKeyIdentifierExtension e = new X509SubjectKeyIdentifierExtension(sk, false);

            byte[] rawData = e.RawData;
            Assert.Equal("040401020304".HexToByteArray(), rawData);

            e = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), false);
            string skid = e.SubjectKeyIdentifier;
            Assert.Equal("01020304", skid);
        }

        [Fact]
        public static void SubjectKeyIdentifierExtension_String()
        {
            string sk = "01ABcd";
            X509SubjectKeyIdentifierExtension e = new X509SubjectKeyIdentifierExtension(sk, false);

            byte[] rawData = e.RawData;
            Assert.Equal("040301abcd".HexToByteArray(), rawData);

            e = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), false);
            string skid = e.SubjectKeyIdentifier;
            Assert.Equal("01ABCD", skid);
        }

        [Fact]
        public static void SubjectKeyIdentifierExtension_PublicKey()
        {
            PublicKey pk;

            using (var cert = new X509Certificate2(TestData.MsCertificate))
            {
                pk = cert.PublicKey;
            }

            X509SubjectKeyIdentifierExtension e = new X509SubjectKeyIdentifierExtension(pk, false);

            byte[] rawData = e.RawData;
            Assert.Equal("04145971a65a334dda980780ff841ebe87f9723241f2".HexToByteArray(), rawData);

            e = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), false);
            string skid = e.SubjectKeyIdentifier;
            Assert.Equal("5971A65A334DDA980780FF841EBE87F9723241F2", skid);
        }

        [Fact]
        public static void SubjectKeyIdentifierExtension_PublicKeySha1()
        {
            TestSubjectKeyIdentifierExtension(
                TestData.MsCertificate,
                X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                false,
                "04145971a65a334dda980780ff841ebe87f9723241f2".HexToByteArray(),
                "5971A65A334DDA980780FF841EBE87F9723241F2");
        }

        [Fact]
        public static void SubjectKeyIdentifierExtension_PublicKeyShortSha1()
        {
            TestSubjectKeyIdentifierExtension(
                TestData.MsCertificate,
                X509SubjectKeyIdentifierHashAlgorithm.ShortSha1,
                false,
                "04084ebe87f9723241f2".HexToByteArray(),
                "4EBE87F9723241F2");
        }

        [Fact]
        public static void SubjectKeyIdentifierExtension_PublicKeyCapiSha1()
        {
            TestSubjectKeyIdentifierExtension(
                TestData.MsCertificate,
                X509SubjectKeyIdentifierHashAlgorithm.CapiSha1,
                false,
                "0414a260a870be1145ed71e2bb5aa19463a4fe9dcc41".HexToByteArray(),
                "A260A870BE1145ED71E2BB5AA19463A4FE9DCC41");
        }

        [Fact]
        public static void ReadInvalidExtension_KeyUsage()
        {
            X509KeyUsageExtension keyUsageExtension =
                new X509KeyUsageExtension(new AsnEncodedData(Array.Empty<byte>()), false);

            Assert.ThrowsAny<CryptographicException>(() => keyUsageExtension.KeyUsages);
        }

        private static void TestKeyUsageExtension(X509KeyUsageFlags flags, bool critical, byte[] expectedDer)
        {
            X509KeyUsageExtension ext = new X509KeyUsageExtension(flags, critical);
            byte[] rawData = ext.RawData;
            Assert.Equal(expectedDer, rawData);

            // Assert that format doesn't crash
            string s = ext.Format(false);

            // Rebuild it from the RawData.
            ext = new X509KeyUsageExtension(new AsnEncodedData(rawData), critical);
            Assert.Equal(flags, ext.KeyUsages);
        }

        private static void TestEnhancedKeyUsageExtension(
            OidCollection usages,
            bool critical,
            byte[] expectedDer)
        {
            X509EnhancedKeyUsageExtension ext = new X509EnhancedKeyUsageExtension(usages, critical);
            byte[] rawData = ext.RawData;
            Assert.Equal(expectedDer, rawData);

            ext = new X509EnhancedKeyUsageExtension(new AsnEncodedData(rawData), critical);
            OidCollection actualUsages = ext.EnhancedKeyUsages;

            Assert.Equal(usages.Count, actualUsages.Count);

            for (int i = 0; i < usages.Count; i++)
            {
                Assert.Equal(usages[i].Value, actualUsages[i].Value);
            }
        }

        private static void TestSubjectKeyIdentifierExtension(
            byte[] certBytes,
            X509SubjectKeyIdentifierHashAlgorithm algorithm,
            bool critical,
            byte[] expectedDer,
            string expectedIdentifier)
        {
            PublicKey pk;

            using (var cert = new X509Certificate2(certBytes))
            {
                pk = cert.PublicKey;
            }

            X509SubjectKeyIdentifierExtension ext =
                new X509SubjectKeyIdentifierExtension(pk, algorithm, critical);

            byte[] rawData = ext.RawData;
            Assert.Equal(expectedDer, rawData);

            ext = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), critical);
            Assert.Equal(expectedIdentifier, ext.SubjectKeyIdentifier);
        }
    }
}
