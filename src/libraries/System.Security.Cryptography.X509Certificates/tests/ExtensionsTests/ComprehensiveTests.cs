// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class ComprehensiveTests
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
    }
}
