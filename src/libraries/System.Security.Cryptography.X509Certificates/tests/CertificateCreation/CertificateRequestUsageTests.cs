// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class CertificateRequestUsageTests
    {
        [Fact]
        public static void ReproduceBigExponentCsr()
        {
            X509Extension sanExtension = new X509Extension(
                "2.5.29.17",
                "302387047F00000187100000000000000000000000000000000182096C6F63616C686F7374".HexToByteArray(),
                false);

            byte[] autoCsr;
            byte[] csr;

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(TestData.RsaBigExponentParams);

                CertificateRequest request = new CertificateRequest(
                    "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(sanExtension);

                autoCsr = request.CreateSigningRequest();

                X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);
                csr = request.CreateSigningRequest(generator);
            }

            Assert.Equal(TestData.BigExponentPkcs10Bytes.ByteArrayToHex(), autoCsr.ByteArrayToHex());
            Assert.Equal(TestData.BigExponentPkcs10Bytes.ByteArrayToHex(), csr.ByteArrayToHex());
        }

        [Fact]
        public static void ReproduceBigExponentCert()
        {
            DateTimeOffset notBefore = new DateTimeOffset(2016, 3, 2, 1, 48, 0, TimeSpan.Zero);
            DateTimeOffset notAfter = new DateTimeOffset(2017, 3, 2, 1, 48, 0, TimeSpan.Zero);
            byte[] serialNumber = "9B5DE6C15126A58B".HexToByteArray();

            var subject = new X500DistinguishedName(
                "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US");

            X509Extension skidExtension = new X509SubjectKeyIdentifierExtension(
                "78A5C75D51667331D5A96924114C9B5FA00D7BCB",
                false);

            X509Extension akidExtension = new X509Extension(
                "2.5.29.35",
                "3016801478A5C75D51667331D5A96924114C9B5FA00D7BCB".HexToByteArray(),
                false);

            X509Extension basicConstraints = new X509BasicConstraintsExtension(true, false, 0, false);

            X509Certificate2 cert;

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(TestData.RsaBigExponentParams);

                var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(skidExtension);
                request.CertificateExtensions.Add(akidExtension);
                request.CertificateExtensions.Add(basicConstraints);

                var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);

                cert = request.Create(subject, signatureGenerator, notBefore, notAfter, serialNumber);
            }

            const string expectedHex =
                "308203EB308202D3A0030201020209009B5DE6C15126A58B300D06092A864886" +
                "F70D01010B050030818A310B3009060355040613025553311330110603550408" +
                "130A57617368696E67746F6E3110300E060355040713075265646D6F6E64311E" +
                "301C060355040A13154D6963726F736F667420436F72706F726174696F6E3120" +
                "301E060355040B13172E4E4554204672616D65776F726B2028436F7265465829" +
                "31123010060355040313096C6F63616C686F7374301E170D3136303330323031" +
                "343830305A170D3137303330323031343830305A30818A310B30090603550406" +
                "13025553311330110603550408130A57617368696E67746F6E3110300E060355" +
                "040713075265646D6F6E64311E301C060355040A13154D6963726F736F667420" +
                "436F72706F726174696F6E3120301E060355040B13172E4E4554204672616D65" +
                "776F726B2028436F726546582931123010060355040313096C6F63616C686F73" +
                "7430820124300D06092A864886F70D010101050003820111003082010C028201" +
                "0100AF81C1CBD8203F624A539ED6608175372393A2837D4890E48A19DED36973" +
                "115620968D6BE0D3DAA38AA777BE02EE0B6B93B724E8DCC12B632B4FA80BBC92" +
                "5BCE624F4CA7CC606306B39403E28C932D24DD546FFE4EF6A37F10770B2215EA" +
                "8CBB5BF427E8C4D89B79EB338375100C5F83E55DE9B4466DDFBEEE42539AEF33" +
                "EF187B7760C3B1A1B2103C2D8144564A0C1039A09C85CF6B5974EB516FC8D662" +
                "3C94AE3A5A0BB3B4C792957D432391566CF3E2A52AFB0C142B9E0681B8972671" +
                "AF2B82DD390A39B939CF719568687E4990A63050CA7768DCD6B378842F18FDB1" +
                "F6D9FF096BAF7BEB98DCF930D66FCFD503F58D41BFF46212E24E3AFC45EA42BD" +
                "884702050200000441A350304E301D0603551D0E0416041478A5C75D51667331" +
                "D5A96924114C9B5FA00D7BCB301F0603551D2304183016801478A5C75D516673" +
                "31D5A96924114C9B5FA00D7BCB300C0603551D13040530030101FF300D06092A" +
                "864886F70D01010B0500038201010077756D05FFA6ADFED5B6D4AFB540840C6D" +
                "01CF6B3FA6C973DFD61FCAA0A814FA1E2469019D94B1D856D07DD2B95B8550DF" +
                "D2085953A494B99EFCBAA7982CE771984F9D4A445FFEE062E8A049736A39FD99" +
                "4E1FDA0A5DC2B5B0E57A0B10C41BC7FE6A40B24F85977302593E60B98DD4811D" +
                "47D948EDF8D6E6B5AF80A1827496E20BFD240E467674504D4E4703331D64705C" +
                "36FB6E14BABFD9CBEEC44B33A8D7B36479900F3C5BBAB69C5E453D180783E250" +
                "8051B998C038E4622571D2AB891D898E5458828CF18679517D28DBCABF72E813" +
                "07BFD721B73DDB1751123F99D8FC0D533798C4DBD14719D5D8A85B00A144A367" +
                "677B48891A9B56F045334811BACB7A";

            using (cert)
            {
                Assert.Equal(expectedHex, cert.RawData.ByteArrayToHex());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void SimpleSelfSign_RSA(bool exportPfx)
        {
            using (RSA rsa = RSA.Create())
            {
                SimpleSelfSign(
                    new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                    "1.2.840.113549.1.1.1",
                    exportPfx);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void SimpleSelfSign_ECC(bool exportPfx)
        {
            using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP521))
            {
                SimpleSelfSign(
                    new CertificateRequest("CN=localhost", ecdsa, HashAlgorithmName.SHA512),
                    "1.2.840.10045.2.1",
                    exportPfx);
            }
        }

        private static void SimpleSelfSign(CertificateRequest request, string expectedKeyOid, bool exportPfx)
        {
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

            DateTimeOffset now = DateTimeOffset.UtcNow;

            using (X509Certificate2 newCert = request.CreateSelfSigned(now, now.AddDays(90)))
            {
                Assert.True(newCert.HasPrivateKey);

                Assert.Equal("CN=localhost", newCert.Subject);
                Assert.Equal(expectedKeyOid, newCert.GetKeyAlgorithm());
                Assert.Equal(1, newCert.Extensions.Count);

                X509Extension extension = newCert.Extensions["2.5.29.37"];
                Assert.NotNull(extension);

                X509EnhancedKeyUsageExtension ekuExtension = (X509EnhancedKeyUsageExtension)extension;
                Assert.Equal(1, ekuExtension.EnhancedKeyUsages.Count);
                Assert.Equal("1.3.6.1.5.5.7.3.1", ekuExtension.EnhancedKeyUsages[0].Value);

                // Ideally the serial number is 8 bytes.  But maybe it accidentally started with 0x00 (1/256),
                // or 0x0000 (1/32768), or even 0x00000000 (1/4 billion). But that's where we draw the line.
                string serialNumber = newCert.SerialNumber;
                // Using this construct so the value gets printed in a failure, instead of just the length.
                Assert.True(
                    serialNumber.Length >= 8 && serialNumber.Length <= 18,
                    $"Serial number ({serialNumber}) should be between 4 and 9 bytes, inclusive");

                if (exportPfx)
                {
                    byte[] pfx = newCert.Export(X509ContentType.Pkcs12, nameof(SimpleSelfSign));
                    Assert.InRange(pfx.Length, 100, int.MaxValue);
                }
            }
        }

        [Fact]
        public static void SelfSign_RSA_UseCertKeys()
        {
            X509Certificate2 cert;
            RSAParameters pubParams;

            RSA priv2;

            using (RSA rsa = RSA.Create())
            {
                pubParams = rsa.ExportParameters(false);

                CertificateRequest request = new CertificateRequest(
                    "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                cert = request.CreateSelfSigned(now, now.AddDays(90));
            }

            using (cert)
            using (priv2 = cert.GetRSAPrivateKey())
            using (RSA pub = RSA.Create())
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                Assert.NotNull(priv2);

                byte[] sig = priv2.SignData(pubParams.Modulus, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

                pub.ImportParameters(pubParams);

                Assert.True(
                    pub.VerifyData(pubParams.Modulus, sig, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
                    "Cert signature validates with public key");
            }
        }

        [Fact]
        public static void SelfSign_ECC_UseCertKeys()
        {
            X509Certificate2 cert;
            ECParameters pubParams;

            ECDsa priv2;

            using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                pubParams = ecdsa.ExportParameters(false);

                CertificateRequest request = new CertificateRequest(
                    "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    ecdsa,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                cert = request.CreateSelfSigned(now, now.AddDays(90));
            }

            using (cert)
            using (priv2 = cert.GetECDsaPrivateKey())
            using (ECDsa pub = ECDsa.Create(pubParams))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                Assert.NotNull(priv2);

                byte[] sig = priv2.SignData(pubParams.Q.X, HashAlgorithmName.SHA384);

                Assert.True(
                    pub.VerifyData(pubParams.Q.X, sig, HashAlgorithmName.SHA384),
                    "Cert signature validates with public key");
            }
        }

        [Fact]
        public static void SelfSign_ECC_DiminishedPoint_UseCertKeys()
        {
            X509Certificate2 cert;
            ECParameters pubParams;

            ECDsa priv2;

            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp521r1_DiminishedPublic_Data.KeyParameters))
            {
                pubParams = ecdsa.ExportParameters(false);

                CertificateRequest request = new CertificateRequest(
                    "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    ecdsa,
                    HashAlgorithmName.SHA512);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                cert = request.CreateSelfSigned(now, now.AddDays(90));

                priv2 = cert.GetECDsaPrivateKey();
            }

            using (cert)
            using (priv2)
            using (ECDsa pub = ECDsa.Create(pubParams))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                Assert.NotNull(priv2);

                byte[] sig = priv2.SignData(pubParams.Q.X, HashAlgorithmName.SHA384);

                Assert.True(
                    pub.VerifyData(pubParams.Q.X, sig, HashAlgorithmName.SHA384),
                    "Cert signature validates with public key");
            }
        }

        [Theory]
        [InlineData("80", "0080")]
        [InlineData("0080", "0080")]
        [InlineData("00000080", "0080")]
        [InlineData("00000000", "00")]
        public static void SerialNumber_AlwaysPositive(string desiredSerial, string expectedSerial)
        {
            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp521r1_DiminishedPublic_Data.KeyParameters))
            {
                var generator = X509SignatureGenerator.CreateForECDsa(ecdsa);

                CertificateRequest request = new CertificateRequest(
                    new X500DistinguishedName("CN=Test Cert"),
                    generator.PublicKey,
                    HashAlgorithmName.SHA512);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                X509Certificate2 cert = request.Create(
                    request.SubjectName,
                    generator,
                    now,
                    now.AddDays(1),
                    desiredSerial.HexToByteArray());

                using (cert)
                {
                    Assert.Equal(expectedSerial, cert.SerialNumber);
                }
            }
        }

        [Fact]
        public static void AlwaysVersion3()
        {
            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                CertificateRequest request = new CertificateRequest("CN=Test Cert", ecdsa, HashAlgorithmName.SHA384);
                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddHours(1)))
                {
                    Assert.Equal(3, cert.Version);
                }

                request.CertificateExtensions.Add(null);

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddHours(1)))
                {
                    Assert.Equal(3, cert.Version);
                    Assert.Equal(0, cert.Extensions.Count);
                }

                request.CertificateExtensions.Clear();
                request.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(
                        request.PublicKey,
                        X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                        false));

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddHours(1)))
                {
                    Assert.Equal(3, cert.Version);
                    Assert.Equal(1, cert.Extensions.Count);
                }
            }
        }

        [Fact]
        public static void UniqueExtensions()
        {
            using (RSA rsa = RSA.Create())
            {
                CertificateRequest request = new CertificateRequest(
                    "CN=Double Extension Test",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                DateTimeOffset now = DateTimeOffset.UtcNow;

                Assert.Throws<InvalidOperationException>(() => request.CreateSelfSigned(now, now.AddDays(1)));
            }
        }

        [Fact]
        public static void CheckTimeNested()
        {
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

            using (RSA rsa = RSA.Create(TestData.RsaBigExponentParams))
            {
                CertificateRequest request = new CertificateRequest("CN=Issuer", rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-10);
                DateTimeOffset notAfter = now.AddMinutes(10);

                if (notAfter.Millisecond > 900)
                {
                    // We're only going to add 1, but let's be defensive.
                    notAfter -= TimeSpan.FromMilliseconds(200);
                }

                using (X509Certificate2 issuer = request.CreateSelfSigned(notBefore, notAfter))
                {
                    request = new CertificateRequest("CN=Leaf", rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                    byte[] serial = { 3, 1, 4, 1, 5, 9 };

                    // Boundary case, exact match: Issue+Dispose
                    request.Create(issuer, notBefore, notAfter, serial).Dispose();

                    DateTimeOffset truncatedNotBefore = new DateTimeOffset(
                        notBefore.Year,
                        notBefore.Month,
                        notBefore.Day,
                        notBefore.Hour,
                        notBefore.Minute,
                        notBefore.Second,
                        notBefore.Offset);

                    // Boundary case, the notBefore rounded down: Issue+Dispose
                    request.Create(issuer, truncatedNotBefore, notAfter, serial).Dispose();

                    // Boundary case, the notAfter plus a millisecond, same second rounded down.
                    request.Create(issuer, notBefore, notAfter.AddMilliseconds(1), serial).Dispose();//);

                    // The notBefore value a whole second earlier:
                    AssertExtensions.Throws<ArgumentException>("notBefore", () =>
                    {
                        request.Create(issuer, notBefore.AddSeconds(-1), notAfter, serial).Dispose();
                    });

                    // The notAfter value bumped past the second mark:
                    DateTimeOffset tooLate = notAfter.AddMilliseconds(1000 - notAfter.Millisecond);
                    AssertExtensions.Throws<ArgumentException>( "notAfter", () =>
                    {
                        request.Create(issuer, notBefore, tooLate, serial).Dispose();
                    });

                    // And ensure that both out of range isn't magically valid again
                    AssertExtensions.Throws<ArgumentException>("notBefore", () =>
                    {
                        request.Create(issuer, notBefore.AddDays(-1), notAfter.AddDays(1), serial).Dispose();
                    });
                }
            }
        }

        [Theory]
        [InlineData("Length Exceeds Payload", "0501")]
        [InlineData("Leftover Data", "0101FF00")]
        [InlineData("Constructed Null", "2500")]
        [InlineData("Primitive Sequence", "1000")]
        // SEQUENCE
        // 30 10
        //   SEQUENCE
        //   30 02
        //     OCTET STRING
        //       04 01 -- Length exceeds data for inner SEQUENCE, but not the outer one.
        //   OCTET STRING
        //   04 04
        //      00 00 00 00
        [InlineData("Big Length, Nested", "3010" + "30020401" + "040400000000")]
        [InlineData("Big Length, Nested - Context Specific", "A010" + "30020401" + "040400000000")]
        [InlineData("Tag Only", "05")]
        [InlineData("Empty", "")]
        [InlineData("Reserved Tag", "0F00")]
        [InlineData("Zero Tag", "0000")]
        public static void InvalidPublicKeyEncoding(string caseName, string parametersHex)
        {
            var generator = new InvalidSignatureGenerator(
                Array.Empty<byte>(),
                parametersHex.HexToByteArray(),
                new byte[] { 0x05, 0x00 });

            CertificateRequest request = new CertificateRequest(
                new X500DistinguishedName("CN=Test"),
                generator.PublicKey,
                HashAlgorithmName.SHA256);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            Exception exception = Assert.Throws<CryptographicException>(
                () => request.Create(request.SubjectName, generator, now, now.AddDays(1), new byte[1]));

            if (CultureInfo.CurrentCulture.Name == "en-US")
            {
                Assert.Contains("ASN1", exception.Message);
            }
        }

        [Theory]
        [InlineData("Empty", "")]
        [InlineData("Empty Sequence", "3000")]
        [InlineData("Empty OID", "30020600")]
        [InlineData("Non-Nested Data", "300206035102013001")]
        [InlineData("Indefinite Encoding", "3002060351020130800000")]
        [InlineData("Dangling LengthLength", "300206035102013081")]
        public static void InvalidSignatureAlgorithmEncoding(string caseName, string sigAlgHex)
        {
            var generator = new InvalidSignatureGenerator(
                Array.Empty<byte>(),
                new byte[] { 0x05, 0x00 },
                sigAlgHex.HexToByteArray());

            CertificateRequest request = new CertificateRequest(
                new X500DistinguishedName("CN=Test"),
                generator.PublicKey,
                HashAlgorithmName.SHA256);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            Exception exception = Assert.Throws<CryptographicException>(
                () =>
                request.Create(request.SubjectName, generator, now, now.AddDays(1), new byte[1]));
#if netcoreapp
            if (CultureInfo.CurrentCulture.Name == "en-US")
            {
                Assert.Contains("ASN1", exception.Message, StringComparison.OrdinalIgnoreCase);
            }
#endif
        }

        [Fact]
        public static void ECDSA_Signing_RSA()
        {
            using (RSA rsa = RSA.Create())
            using (ECDsa ecdsa = ECDsa.Create())
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName("CN=Test"),
                    ecdsa,
                    HashAlgorithmName.SHA256);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                {
                    X509SignatureGenerator generator =
                        X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);

                    request = new CertificateRequest(
                        new X500DistinguishedName("CN=Leaf"),
                        rsa,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    byte[] serialNumber = { 1, 1, 2, 3, 5, 8, 13 };

                    AssertExtensions.Throws<ArgumentException>("issuerCertificate", () => request.Create(cert, now, now.AddHours(3), serialNumber));


                    // Passes with the generator
                    using (request.Create(cert.SubjectName, generator, now, now.AddHours(3), serialNumber))
                    {
                    }
                }
            }
        }

        [Fact]
        public static void ECDSA_Signing_RSAPublicKey()
        {
            using (RSA rsa = RSA.Create())
            using (ECDsa ecdsa = ECDsa.Create())
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName("CN=Test"),
                    ecdsa,
                    HashAlgorithmName.SHA256);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                {
                    X509SignatureGenerator rsaGenerator =
                        X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);

                    request = new CertificateRequest(
                        new X500DistinguishedName("CN=Leaf"),
                        rsaGenerator.PublicKey,
                        HashAlgorithmName.SHA256);

                    byte[] serialNumber = { 1, 1, 2, 3, 5, 8, 13 };

                    AssertExtensions.Throws<ArgumentException>("issuerCertificate", () => request.Create(cert, now, now.AddHours(3), serialNumber));

                    X509SignatureGenerator ecdsaGenerator =
                        X509SignatureGenerator.CreateForECDsa(ecdsa);

                    // Passes with the generator
                    using (request.Create(cert.SubjectName, ecdsaGenerator, now, now.AddHours(3), serialNumber))
                    {
                    }
                }
            }
        }

        [Fact]
        public static void RSA_Signing_ECDSA()
        {
            using (RSA rsa = RSA.Create())
            using (ECDsa ecdsa = ECDsa.Create())
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName("CN=Test"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                {
                    request = new CertificateRequest(
                        new X500DistinguishedName("CN=Leaf"),
                        ecdsa,
                        HashAlgorithmName.SHA256);

                    byte[] serialNumber = { 1, 1, 2, 3, 5, 8, 13 };

                    AssertExtensions.Throws<ArgumentException>("issuerCertificate", () => request.Create(cert, now, now.AddHours(3), serialNumber));

                    X509SignatureGenerator generator =
                        X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);

                    // Passes with the generator
                    using (request.Create(cert.SubjectName, generator, now, now.AddHours(3), serialNumber))
                    {
                    }
                }
            }
        }

        [Fact]
        public static void RSACertificateNoPaddingMode()
        {
            using (RSA rsa = RSA.Create())
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName("CN=Test"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                {
                    request = new CertificateRequest(
                        new X500DistinguishedName("CN=Leaf"),
                        cert.PublicKey,
                        HashAlgorithmName.SHA256);

                    byte[] serialNumber = { 1, 1, 2, 3, 5, 8, 13 };

                    Assert.Throws<InvalidOperationException>(
                        () => request.Create(cert, now, now.AddHours(3), serialNumber));

                    X509SignatureGenerator generator =
                        X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);

                    // Passes with the generator
                    using (request.Create(cert.SubjectName, generator, now, now.AddHours(3), serialNumber))
                    {
                    }
                }
            }
        }

        private class InvalidSignatureGenerator : X509SignatureGenerator
        {
            private readonly byte[] _signatureAlgBytes;
            private readonly PublicKey _publicKey;

            internal InvalidSignatureGenerator(byte[] keyBytes, byte[] parameterBytes, byte[] sigAlgBytes)
            {
                _signatureAlgBytes = sigAlgBytes;

                Oid oid = new Oid("2.1.2.1", "DER");
                _publicKey = new PublicKey(
                    oid,
                    parameterBytes == null ? null : new AsnEncodedData(oid, parameterBytes),
                    new AsnEncodedData(oid, keyBytes));
            }

            protected override PublicKey BuildPublicKey()
            {
                return _publicKey;
            }

            public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
            {
                return _signatureAlgBytes;
            }

            public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
            {
                throw new InvalidOperationException("The test should not have made it this far");
            }
        }
    }
}
