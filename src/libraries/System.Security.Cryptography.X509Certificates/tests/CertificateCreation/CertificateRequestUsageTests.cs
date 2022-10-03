// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            string csrPem;
            string autoCsrPem;

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
                autoCsrPem = request.CreateSigningRequestPem();

                X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);
                csr = request.CreateSigningRequest(generator);
                csrPem = request.CreateSigningRequestPem(generator);
            }

            AssertExtensions.SequenceEqual(TestData.BigExponentPkcs10Bytes, autoCsr);
            AssertExtensions.SequenceEqual(TestData.BigExponentPkcs10Bytes, csr);
            Assert.Equal(TestData.BigExponentPkcs10Pem, autoCsrPem);
            Assert.Equal(TestData.BigExponentPkcs10Pem, csrPem);
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
        [InlineData("00FF", "00FF")]
        [InlineData("00000080", "0080")]
        [InlineData("00008008", "008008")]
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
                byte[] serialNumber = desiredSerial.HexToByteArray();

                // byte[] serialNumber
                X509Certificate2 cert = request.Create(
                    request.SubjectName,
                    generator,
                    now,
                    now.AddDays(1),
                    serialNumber);

                using (cert)
                {
                    Assert.Equal(expectedSerial, cert.SerialNumber);
                }

                // ReadOnlySpan<byte> serialNumber
                cert = request.Create(
                    request.SubjectName,
                    generator,
                    now,
                    now.AddDays(1),
                    serialNumber.AsSpan());

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
            _ = caseName;

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
            _ = caseName;

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
#if NETCOREAPP
            if (CultureInfo.CurrentCulture.Name == "en-US")
            {
                Assert.Contains("ASN1", exception.Message, StringComparison.OrdinalIgnoreCase);
            }
#endif
        }

        [Fact]
        public static void ECDSA_Signing_ECDH()
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            CertificateRequest issuerRequest = new CertificateRequest(
                new X500DistinguishedName("CN=root"),
                ecdsa,
                HashAlgorithmName.SHA256);

            issuerRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            CertificateRequest request = new CertificateRequest(
                new X500DistinguishedName("CN=test"),
                new PublicKey(ecdh),
                HashAlgorithmName.SHA256);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.KeyAgreement, true));

            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = notBefore.AddDays(30);
            byte[] serial = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using X509Certificate2 issuer = issuerRequest.CreateSelfSigned(notBefore, notAfter);
            using X509Certificate2 cert = request.Create(issuer, notBefore, notAfter, serial);
            using ECDiffieHellman publicCertKey = cert.GetECDiffieHellmanPublicKey();

            Assert.NotNull(publicCertKey);
            Assert.Equal(ecdh.ExportSubjectPublicKeyInfo(), publicCertKey.ExportSubjectPublicKeyInfo());

            Assert.Null(cert.GetECDsaPublicKey());
        }

        [Fact]
        public static void ECDSA_Signing_ECDH_NoKeyUsageValidForECDSAAndECDH()
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            CertificateRequest issuerRequest = new CertificateRequest(
                new X500DistinguishedName("CN=root"),
                ecdsa,
                HashAlgorithmName.SHA256);

            issuerRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            CertificateRequest request = new CertificateRequest(
                new X500DistinguishedName("CN=test"),
                new PublicKey(ecdh),
                HashAlgorithmName.SHA256);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));

            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = notBefore.AddDays(30);
            byte[] serial = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using X509Certificate2 issuer = issuerRequest.CreateSelfSigned(notBefore, notAfter);
            using X509Certificate2 cert = request.Create(issuer, notBefore, notAfter, serial);
            using ECDiffieHellman publicCertEcDhKey = cert.GetECDiffieHellmanPublicKey();
            using ECDsa publicCertEcDsaKey = cert.GetECDsaPublicKey();
            byte[] expectedSubjectPublicKeyInfo = ecdh.ExportSubjectPublicKeyInfo();

            Assert.NotNull(publicCertEcDhKey);
            Assert.NotNull(publicCertEcDsaKey);
            Assert.Equal(expectedSubjectPublicKeyInfo, publicCertEcDhKey.ExportSubjectPublicKeyInfo());
            Assert.Equal(expectedSubjectPublicKeyInfo, publicCertEcDsaKey.ExportSubjectPublicKeyInfo());
        }

        [Fact]
        public static void ECDSA_Signing_ECDSA_NoKeyUsageValidForECDSAAndECDH()
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using ECDsa ecdsaLeaf = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            CertificateRequest issuerRequest = new CertificateRequest(
                new X500DistinguishedName("CN=root"),
                ecdsa,
                HashAlgorithmName.SHA256);

            issuerRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            CertificateRequest request = new CertificateRequest(
                new X500DistinguishedName("CN=test"),
                ecdsaLeaf,
                HashAlgorithmName.SHA256);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));

            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = notBefore.AddDays(30);
            byte[] serial = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using X509Certificate2 issuer = issuerRequest.CreateSelfSigned(notBefore, notAfter);
            using X509Certificate2 cert = request.Create(issuer, notBefore, notAfter, serial);
            using ECDiffieHellman publicCertEcDhKey = cert.GetECDiffieHellmanPublicKey();
            using ECDsa publicCertEcDsaKey = cert.GetECDsaPublicKey();
            byte[] expectedSubjectPublicKeyInfo = ecdsaLeaf.ExportSubjectPublicKeyInfo();

            Assert.NotNull(publicCertEcDhKey);
            Assert.NotNull(publicCertEcDsaKey);
            Assert.Equal(expectedSubjectPublicKeyInfo, publicCertEcDhKey.ExportSubjectPublicKeyInfo());
            Assert.Equal(expectedSubjectPublicKeyInfo, publicCertEcDsaKey.ExportSubjectPublicKeyInfo());
        }

        [Fact]
        public static void ECDSA_Signing_UnknownPublicKeyAlgorithm()
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            PublicKey gostRPublicKey = PublicKey.CreateFromSubjectPublicKeyInfo(
                TestData.GostR3410SubjectPublicKeyInfo,
                out _);

            CertificateRequest issuerRequest = new CertificateRequest(
                new X500DistinguishedName("CN=root"),
                ecdsa,
                HashAlgorithmName.SHA256);

            issuerRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            CertificateRequest request = new CertificateRequest(
                new X500DistinguishedName("CN=test"),
                gostRPublicKey,
                HashAlgorithmName.SHA256);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));

            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = notBefore.AddDays(30);
            byte[] serial = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using X509Certificate2 issuer = issuerRequest.CreateSelfSigned(notBefore, notAfter);

            X509SignatureGenerator ecdsaGenerator = X509SignatureGenerator.CreateForECDsa(ecdsa);
            using X509Certificate2 cert = request.Create(issuer.SubjectName, ecdsaGenerator, notBefore, notAfter, serial);

            Assert.Null(cert.GetECDsaPublicKey());
            Assert.Null(cert.GetECDiffieHellmanPublicKey());
            Assert.Equal("1.2.643.2.2.19", cert.PublicKey.Oid.Value);
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void FractionalSecondsNotWritten(bool selfSigned)
        {
            using (X509Certificate2 savedCert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            using (RSA rsa = savedCert.GetRSAPrivateKey())
            {
                X500DistinguishedName subjectName = new X500DistinguishedName("CN=Test");

                var request = new CertificateRequest(
                    subjectName,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // notBefore is a date before 2050 UTC (encoded using UTC TIME),
                // notAfter is a date after 2050 UTC (encoded using GENERALIZED TIME).

                DateTimeOffset notBefore = new DateTimeOffset(2049, 3, 4, 5, 6, 7, 89, TimeSpan.Zero);
                DateTimeOffset notAfter = notBefore.AddYears(2);
                Assert.NotEqual(0, notAfter.Millisecond);

                DateTimeOffset normalizedBefore = notBefore.AddMilliseconds(-notBefore.Millisecond);
                DateTimeOffset normalizedAfter = notAfter.AddMilliseconds(-notAfter.Millisecond);
                byte[] manualSerialNumber = { 3, 2, 1 };
                X509Certificate2 cert;

                if (selfSigned)
                {
                    cert = request.CreateSelfSigned(notBefore, notAfter);
                }
                else
                {
                    cert = request.Create(
                        subjectName,
                        X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                        notBefore,
                        notAfter,
                        manualSerialNumber);
                }

                using (cert)
                {
                    Assert.Equal(normalizedBefore.DateTime.ToLocalTime(), cert.NotBefore);
                    Assert.Equal(normalizedAfter.DateTime.ToLocalTime(), cert.NotAfter);

                    if (selfSigned)
                    {
                        // The serial number used in CreateSelfSigned is random, so find the issuer name,
                        // and the validity period is the next 34 bytes.  Verify it was encoded as expected.
                        //
                        // Since the random serial number is at most 9 bytes and the subjectName encoded
                        // value is 17 bytes, there's no chance of an early false match.
                        byte[] encodedCert = cert.RawData;
                        byte[] needle = subjectName.RawData;

                        int index = encodedCert.AsSpan().IndexOf(needle);
                        Assert.Equal(
                            "3020170D3439303330343035303630375A180F32303531303330343035303630375A",
                            encodedCert.AsSpan(index + needle.Length, 34).ByteArrayToHex());
                    }
                    else
                    {
                        // The entire encoding is deterministic in this mode.
                        Assert.Equal(
                            "308201953081FFA0030201020203030201300D06092A864886F70D01010B0500" +
                            "300F310D300B06035504031304546573743020170D3439303330343035303630" +
                            "375A180F32303531303330343035303630375A300F310D300B06035504031304" +
                            "5465737430819F300D06092A864886F70D010101050003818D00308189028181" +
                            "00B11E30EA87424A371E30227E933CE6BE0E65FF1C189D0D888EC8FF13AA7B42" +
                            "B68056128322B21F2B6976609B62B6BC4CF2E55FF5AE64E9B68C78A3C2DACC91" +
                            "6A1BC7322DD353B32898675CFB5B298B176D978B1F12313E3D865BC53465A11C" +
                            "CA106870A4B5D50A2C410938240E92B64902BAEA23EB093D9599E9E372E48336" +
                            "730203010001300D06092A864886F70D01010B0500038181000095ABC7CC7B01" +
                            "9C2A88A7891165B6ACCDBC5137D80C0A5151B11FD4D789CCE808412ABF05FFB1" +
                            "D9BE097776147A6D4C3EE177E5F9C2C9E8C005D72A6473F9904185B95634BFB4" +
                            "EA80B232B271DC1BF20A2FDC46FC93771636B618F29417C31D5F602236FDB414" +
                            "CDC1BEDE700E31E80DC5E7BB7D3F367420B72925605C916BDA",
                            cert.RawData.ByteArrayToHex());
                    }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateSigningRequestWithAttributes(bool reversed)
        {
            // Generated by `openssl req -new -keyin bigexponent.pem` where bigexponent.pem
            // represents the PKCS8 of TestData.RsaBigExponentParams
            // All default values were taken, except
            // Challenge password: 1234 (vs unspecified)
            // An optional company name: Fabrikam (vs unspecified)
            const string ExpectedPem =
                "-----BEGIN CERTIFICATE REQUEST-----\n" +
                "MIICujCCAaICAQAwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUx\n" +
                "ITAfBgNVBAoMGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDCCASQwDQYJKoZIhvcN\n" +
                "AQEBBQADggERADCCAQwCggEBAK+BwcvYID9iSlOe1mCBdTcjk6KDfUiQ5IoZ3tNp\n" +
                "cxFWIJaNa+DT2qOKp3e+Au4La5O3JOjcwStjK0+oC7ySW85iT0ynzGBjBrOUA+KM\n" +
                "ky0k3VRv/k72o38QdwsiFeqMu1v0J+jE2Jt56zODdRAMX4PlXem0Rm3fvu5CU5rv\n" +
                "M+8Ye3dgw7GhshA8LYFEVkoMEDmgnIXPa1l061FvyNZiPJSuOloLs7THkpV9QyOR\n" +
                "Vmzz4qUq+wwUK54GgbiXJnGvK4LdOQo5uTnPcZVoaH5JkKYwUMp3aNzWs3iELxj9\n" +
                "sfbZ/wlrr3vrmNz5MNZvz9UD9Y1Bv/RiEuJOOvxF6kK9iEcCBQIAAARBoC4wEwYJ\n" +
                "KoZIhvcNAQkHMQYMBDEyMzQwFwYJKoZIhvcNAQkCMQoMCEZhYnJpa2FtMA0GCSqG\n" +
                "SIb3DQEBCwUAA4IBAQCr1X8D+ZkJqBmuZVEYqLPvNvie+KBycxgiJ08ZaV/dyndZ\n" +
                "cudn6G9K0hiIwwGrfI5gbIb7QdPi64g3l9VdIrdH3yvQ6AcOZ644paiUUpe3u93l\n" +
                "DTY+BGN7C0reJwL7ehalIrtS7hLKAQerg/qS7JO9aLRTbIXR52BQIUs9htYeATC5\n" +
                "VHHssrOZpIHqIN4oaZbE0BwZm0ap6RVD80Oexko8pjiz9XNmtUWadeXXtezuOWTb\n" +
                "duuJlh31kITIrbWVoMawMRq6JwNTPAFyiDMB/EFIvjxpUoS5yJe14bT8Hw2XvAFK\n" +
                "Z9jOhHEPmAsasfRRSwr6CXyIKqo1HVT1ARPgHKHX\n" +
                "-----END CERTIFICATE REQUEST-----";

            string builtPem;

            using (RSA key = RSA.Create(TestData.RsaBigExponentParams))
            {
                X500DistinguishedNameBuilder nameBuilder = new();
                nameBuilder.AddOrganizationName("Internet Widgits Pty Ltd");
                nameBuilder.AddStateOrProvinceName("Some-State");
                nameBuilder.AddCountryOrRegion("AU");

                CertificateRequest req = new CertificateRequest(
                    nameBuilder.Build(),
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Unstructured Name: UTF8String("Fabrikam")
                AsnEncodedData unstructuredNameAttr = new AsnEncodedData(
                    new Oid("1.2.840.113549.1.9.2", null),
                    "0C0846616272696B616D".HexToByteArray());

                // ChallengePassword
                AsnEncodedData cpAttr = new AsnEncodedData(
                    new Oid("1.2.840.113549.1.9.7", null),
                    "0C0431323334".HexToByteArray());

                // Request attributes are in a SET OF, which means they get sorted.
                // So both orders here produce the same output.
                if (reversed)
                {
                    req.OtherRequestAttributes.Add(unstructuredNameAttr);
                    req.OtherRequestAttributes.Add(cpAttr);
                }
                else
                {
                    req.OtherRequestAttributes.Add(cpAttr);
                    req.OtherRequestAttributes.Add(unstructuredNameAttr);
                }

                builtPem = req.CreateSigningRequestPem();
            }

            Assert.Equal(ExpectedPem, builtPem);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateSigningRequestWithDuplicateAttributes(bool reversed)
        {
            const string ExpectedPem =
                "-----BEGIN CERTIFICATE REQUEST-----\n" +
                "MIIC/TCCAeUCAQAwgYoxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u\n" +
                "MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp\n" +
                "b24xIDAeBgNVBAsTFy5ORVQgRnJhbWV3b3JrIChDb3JlRlgpMRIwEAYDVQQDEwls\n" +
                "b2NhbGhvc3QwggEkMA0GCSqGSIb3DQEBAQUAA4IBEQAwggEMAoIBAQCvgcHL2CA/\n" +
                "YkpTntZggXU3I5Oig31IkOSKGd7TaXMRViCWjWvg09qjiqd3vgLuC2uTtyTo3MEr\n" +
                "YytPqAu8klvOYk9Mp8xgYwazlAPijJMtJN1Ub/5O9qN/EHcLIhXqjLtb9CfoxNib\n" +
                "eeszg3UQDF+D5V3ptEZt377uQlOa7zPvGHt3YMOxobIQPC2BRFZKDBA5oJyFz2tZ\n" +
                "dOtRb8jWYjyUrjpaC7O0x5KVfUMjkVZs8+KlKvsMFCueBoG4lyZxryuC3TkKObk5\n" +
                "z3GVaGh+SZCmMFDKd2jc1rN4hC8Y/bH22f8Ja69765jc+TDWb8/VA/WNQb/0YhLi\n" +
                "Tjr8RepCvYhHAgUCAAAEQaArMBMGCSqGSIb3DQEJBzEGDAQxMjM0MBQGCSqGSIb3\n" +
                "DQEJBzEHDAUxMjM0NTANBgkqhkiG9w0BAQsFAAOCAQEAB3lwd8z6XGmX6mbOo3Xm\n" +
                "+ZyW4glQtJ51FAXA1zy83y5Uqyf85ZtTFl6UPw970x8KlSlY/9eMhyo/LORAwQql\n" +
                "J8oga5ho2clJF62IJX9/Ih6JlmcMfyi9qEQaqsY/Og4IBSvxQo39SGzGFLv9mhxa\n" +
                "R1YWoVggsbs638ph/T8Upz/GKb/0tBnGBThRZJip7HLugzzvSJGnirpp0fZhnwWM\n" +
                "l1IlddN5/AdZ86j/r5RNlDKDHlwqI3UJ5Olb1iVFt00d/vwVRM09V1ZNIpiCmPv6\n" +
                "MJG3L+NUKOpSUDXn9qtCxB0pd1MaZVit5EvJI98sKZhILRz3S5KXTxf+kBjNxC98\n" +
                "AQ==\n" +
                "-----END CERTIFICATE REQUEST-----";

            string output;

            using (RSA key = RSA.Create(TestData.RsaBigExponentParams))
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // 1234
                AsnEncodedData cpAttr1 = new AsnEncodedData(
                    new Oid("1.2.840.113549.1.9.7", null),
                    "0C0431323334".HexToByteArray());

                // 12345
                AsnEncodedData cpAttr2 = new AsnEncodedData(
                    new Oid("1.2.840.113549.1.9.7", null),
                    "0C053132333435".HexToByteArray());

                if (reversed)
                {
                    req.OtherRequestAttributes.Add(cpAttr2);
                    req.OtherRequestAttributes.Add(cpAttr1);
                }
                else
                {
                    req.OtherRequestAttributes.Add(cpAttr1);
                    req.OtherRequestAttributes.Add(cpAttr2);
                }

                // ChallengePassword is defined as SINGLE VALUE TRUE,
                // so if we understood it as a rich concept we would block it.
                // But, we don't, so this problem gets passed on to the entity reading the request.
                output = req.CreateSigningRequestPem();
            }

            Assert.Equal(ExpectedPem, output);
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
