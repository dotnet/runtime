// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class CrlBuilderTests
    {
        private const string CertParam = "issuerCertificate";

        [Fact]
        public static void AddEntryArgumentValidation()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

            Assert.Throws<ArgumentNullException>("serialNumber", () => builder.AddEntry((byte[])null));
            Assert.Throws<ArgumentNullException>("serialNumber", () => builder.AddEntry((byte[])null, now));
            Assert.Throws<ArgumentNullException>("certificate", () => builder.AddEntry((X509Certificate2)null));
            Assert.Throws<ArgumentNullException>("certificate", () => builder.AddEntry((X509Certificate2)null, now));
            Assert.Throws<ArgumentException>("serialNumber", () => builder.AddEntry(Array.Empty<byte>()));
            Assert.Throws<ArgumentException>("serialNumber", () => builder.AddEntry(Array.Empty<byte>(), now));
            Assert.Throws<ArgumentException>("serialNumber", () => builder.AddEntry(ReadOnlySpan<byte>.Empty));
            Assert.Throws<ArgumentException>("serialNumber", () => builder.AddEntry(ReadOnlySpan<byte>.Empty, now));
        }

        [Fact]
        public static void BuildWithNullCertificate()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

            Assert.Throws<ArgumentNullException>(CertParam, () => builder.Build(null, 0, now, HashAlgorithmName.SHA256));
        }

        [Fact]
        public static void BuildWithNoPrivateKeyCertificate()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificatePemBytes))
            {
                ArgumentException e = Assert.Throws<ArgumentException>(
                    CertParam,
                    () => builder.Build(cert, 0, now, HashAlgorithmName.SHA256));

                Assert.Contains("private key", e.Message);
            }
        }

        [Fact]
        public static void BuildWithCertificateWithNoBasicConstraints()
        {
            BuildCertificateAndRun(
                Enumerable.Empty<X509Extension>(),
                static (cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    ArgumentException e = Assert.Throws<ArgumentException>(
                        CertParam,
                        () => builder.Build(cert, 0, now.AddMinutes(5), HashAlgorithmName.SHA256));

                    Assert.Contains("Basic Constraints", e.Message);
                    Assert.DoesNotContain("appropriate", e.Message);
                });
        }

        [Fact]
        public static void BuildWithCertificateWithBadBasicConstraints()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForEndEntity(),
                },
                static (cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    ArgumentException e = Assert.Throws<ArgumentException>(
                        CertParam,
                        () => builder.Build(cert, 0, now.AddMinutes(5), HashAlgorithmName.SHA256));

                    Assert.Contains("Basic Constraints", e.Message);
                    Assert.Contains("appropriate", e.Message);
                });
        }

        [Fact]
        public static void BuildWithCertificateWithBadKeyUsage()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                    new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true),
                },
                static (cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    ArgumentException e = Assert.Throws<ArgumentException>(
                        CertParam,
                        () => builder.Build(cert, 0, now.AddMinutes(5), HashAlgorithmName.SHA256));

                    Assert.Contains("CrlSign", e.Message);
                });
        }

        [Fact]
        public static void BuildWithNextUpdateBeforeThisUpdate()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                        true),
                },
                static (cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    ArgumentException e = Assert.Throws<ArgumentException>(
                        () => builder.Build(cert, 0, now, HashAlgorithmName.SHA256, null, now.AddSeconds(1)));

                    Assert.Null(e.ParamName);
                    Assert.Contains("thisUpdate", e.Message);
                    Assert.Contains("nextUpdate", e.Message);

                    using (ECDsa key = cert.GetECDsaPrivateKey())
                    {
                        X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                        X500DistinguishedName dn = cert.SubjectName;

                        e = Assert.Throws<ArgumentException>(
                            () => builder.Build(dn, gen, 0, now, HashAlgorithmName.SHA256, null, now.AddSeconds(1)));

                        Assert.Null(e.ParamName);
                        Assert.Contains("thisUpdate", e.Message);
                        Assert.Contains("nextUpdate", e.Message);
                    }
                });
        }

        [Fact]
        public static void BuildWithNoHashAlgorithm()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, now) =>
                {
                    HashAlgorithmName hashAlg = default;
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    Assert.Throws<ArgumentNullException>(
                        "hashAlgorithm",
                        () => builder.Build(cert, 0, now.AddMinutes(5), hashAlg, null, now));

                    using (ECDsa key = cert.GetECDsaPrivateKey())
                    {
                        X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                        X500DistinguishedName dn = cert.SubjectName;

                        Assert.Throws<ArgumentNullException>(
                            "hashAlgorithm",
                            () => builder.Build(dn, gen, 0, now.AddMinutes(5), hashAlg, null, now));
                    }
                });
        }

        [Fact]
        public static void BuildWithEmptyHashAlgorithm()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, now) =>
                {
                    HashAlgorithmName hashAlg = new HashAlgorithmName("");
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    ArgumentException e = Assert.Throws<ArgumentException>(
                        "hashAlgorithm",
                        () => builder.Build(cert, 0, now.AddMinutes(5), hashAlg, null, now));

                    Assert.Contains("empty", e.Message);

                    using (ECDsa key = cert.GetECDsaPrivateKey())
                    {
                        X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                        X500DistinguishedName dn = cert.SubjectName;

                        e = Assert.Throws<ArgumentException>(
                            "hashAlgorithm",
                            () => builder.Build(dn, gen, 0, now.AddMinutes(5), hashAlg, null, now));

                        Assert.Contains("empty", e.Message);
                    }
                });
        }

        [Fact]
        public static void BuildWithNegativeCrlNumber()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, now) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    Assert.Throws<ArgumentOutOfRangeException>(
                        "crlNumber",
                        () => builder.Build(cert, -1, now.AddMinutes(5), hashAlg, null, now));

                    using (ECDsa key = cert.GetECDsaPrivateKey())
                    {
                        X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                        X500DistinguishedName dn = cert.SubjectName;

                        Assert.Throws<ArgumentOutOfRangeException>(
                            "crlNumber",
                            () => builder.Build(dn, gen, -1, now.AddMinutes(5), hashAlg, null, now));
                    }
                });
        }

        [Fact]
        public static void BuildWithGeneratorNullName()
        {
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Assert.Throws<ArgumentNullException>(
                "issuerName",
                () => builder.Build(null, null, 0, now.AddMinutes(5), HashAlgorithmName.SHA256, null, now));
        }

        [Fact]
        public static void BuildWithGeneratorNullGenerator()
        {
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            X500DistinguishedName dn = new X500DistinguishedName("CN=Name");

            Assert.Throws<ArgumentNullException>(
                "generator",
                () => builder.Build(dn, null, 0, now.AddMinutes(5), HashAlgorithmName.SHA256, null, now));
        }

        [Fact]
        public static void BuildWithGeneratorNullAkid()
        {
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            X500DistinguishedName dn = new X500DistinguishedName("CN=Name");

            using (RSA rsa = RSA.Create(TestData.RsaBigExponentParams))
            {
                X509SignatureGenerator gen = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);

                Assert.Throws<ArgumentNullException>(
                    "authorityKeyIdentifier",
                    () => builder.Build(dn, gen, 0, now.AddMinutes(5), HashAlgorithmName.SHA256, null, now));
            }
        }

        [Fact]
        public static void BuildWithRSACertificateAndNoPadding()
        {
            using (RSA key = RSA.Create(TestData.RsaBigExponentParams))
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=RSA Test",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
                req.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForCertificateAuthority());

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = req.CreateSelfSigned(now.AddMonths(-1), now.AddMonths(1)))
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    ArgumentException e;

                    e = Assert.Throws<ArgumentException>(
                        () => builder.Build(cert, 0, now.AddMinutes(5), HashAlgorithmName.SHA256, null, now));

                    Assert.Null(e.ParamName);
                    Assert.Contains(nameof(RSASignaturePadding), e.Message);
                }
            }
        }

        [Fact]
        public static void BuildWithGeneratorArgumentValidation()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset thisUpdate = now;
            DateTimeOffset nextUpdate = now.AddMinutes(1);

            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

            Assert.Throws<ArgumentNullException>(
                "issuerName",
                () => builder.Build((X500DistinguishedName)null, default, 0, nextUpdate, default, default, thisUpdate));

            X500DistinguishedName issuerName = new X500DistinguishedName("CN=Bad CA");

            Assert.Throws<ArgumentNullException>(
                "generator",
                () => builder.Build(issuerName, default, 0, nextUpdate, default, default, thisUpdate));

            using (ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP384))
            {
                X509SignatureGenerator generator = X509SignatureGenerator.CreateForECDsa(key);

                Assert.Throws<ArgumentOutOfRangeException>(
                    "crlNumber",
                    () => builder.Build(issuerName, generator, -1, nextUpdate, default, default, thisUpdate));

                ArgumentException ex = Assert.Throws<ArgumentException>(
                    () => builder.Build(issuerName, generator, 0, now.AddYears(-10), default, default, thisUpdate));
                Assert.Null(ex.ParamName);
                Assert.Contains("thisUpdate", ex.Message);
                Assert.Contains("nextUpdate", ex.Message);
            }
        }

        [Fact]
        public static void BuildEmpty()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);

                    byte[] built = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // The length of the output depends on a number of factors, but they're all stable
                    // for this test (since it doesn't use ECDSA's variable-length, non-deterministic, signature)
                    //
                    // In fact, because RSASSA-PKCS1 is a deterministic algorithm, we can check it for a fixed output.
                    
                    AssertExtensions.SequenceEqual(BuildEmptyExpectedCrl, built);
                });
        }

        [Theory]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void BuildEmptyRsaPss(string hashName)
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                (cert, now) =>
                {
                    HashAlgorithmName hashAlg = new HashAlgorithmName(hashName);
                    RSASignaturePadding pad = RSASignaturePadding.Pss;
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    DateTimeOffset thisUpdate = now;
                    DateTimeOffset nextUpdate = now.AddHours(2);
                    int crlNumber = RandomNumberGenerator.GetInt32(1066, 1813);

                    byte[] crl = builder.Build(cert, crlNumber, nextUpdate, hashAlg, pad, thisUpdate);

                    AsnReader reader = new AsnReader(crl, AsnEncodingRules.DER).ReadSequence();
                    ReadOnlyMemory<byte> tbs = reader.ReadEncodedValue();
                    // sigAlg
                    reader.ReadEncodedValue();
                    byte[] signature = reader.ReadBitString(out _);

                    using (RSA pubKey = cert.GetRSAPublicKey())
                    {
                        Assert.True(
                            pubKey.VerifyData(tbs.Span, signature, hashAlg, pad),
                            "Certificate's public key verifies the signature");
                    }

                    VerifyCrlFields(
                        crl,
                        cert.SubjectName,
                        thisUpdate,
                        nextUpdate,
                        X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, true, false),
                        crlNumber);
                });
        }

        [Fact]
        public static void BuildEmptyEcdsa()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                (cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    DateTimeOffset nextUpdate = now.AddHours(1);
                    byte[] crl = builder.Build(cert, 2, nextUpdate, HashAlgorithmName.SHA256);

                    AsnReader reader = new AsnReader(crl, AsnEncodingRules.DER);
                    reader = reader.ReadSequence();
                    ReadOnlyMemory<byte> tbs = reader.ReadEncodedValue();
                    // signatureAlgorithm
                    reader.ReadEncodedValue();
                    byte[] signature = reader.ReadBitString(out _);
                    reader.ThrowIfNotEmpty();

                    using (ECDsa pubKey = cert.GetECDsaPublicKey())
                    {
                        Assert.True(
                            pubKey.VerifyData(
                                tbs.Span,
                                signature,
                                HashAlgorithmName.SHA256,
                                DSASignatureFormat.Rfc3279DerSequence),
                            "Certificate public key verifies CRL");
                    }

                    VerifyCrlFields(
                        crl,
                        cert.SubjectName,
                        thisUpdate: null,
                        nextUpdate,
                        X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, true, false),
                        2);
                });
        }

        [Fact]
        public static void BuildEmptyEcdsa_NoSubjectKeyIdentifier()
        {
            BuildCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                (cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    DateTimeOffset nextUpdate = now.AddHours(1);
                    DateTimeOffset thisUpdate = now;

                    byte[] crl = builder.Build(
                        cert,
                        2,
                        nextUpdate,
                        HashAlgorithmName.SHA256,
                        thisUpdate: thisUpdate);

                    AsnReader reader = new AsnReader(crl, AsnEncodingRules.DER);
                    reader = reader.ReadSequence();
                    ReadOnlyMemory<byte> tbs = reader.ReadEncodedValue();
                    // signatureAlgorithm
                    reader.ReadEncodedValue();
                    byte[] signature = reader.ReadBitString(out _);
                    reader.ThrowIfNotEmpty();

                    using (ECDsa pubKey = cert.GetECDsaPublicKey())
                    {
                        Assert.True(
                            pubKey.VerifyData(
                                tbs.Span,
                                signature,
                                HashAlgorithmName.SHA256,
                                DSASignatureFormat.Rfc3279DerSequence),
                            "Certificate public key verifies CRL");
                    }

                    VerifyCrlFields(
                        crl,
                        cert.SubjectName,
                        thisUpdate,
                        nextUpdate,
                        X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, false, true),
                        2);
                },
                addSubjectKeyIdentifier: false);
        }

        [Fact]
        public static void BuildSingleEntry()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    ReadOnlySpan<byte> serialToAdd =
                        new byte[] { 0x01, 0x01, 0x02, 0x03, 0x05, 0x08, 0x0C, 0x15 };

                    builder.AddEntry(serialToAdd, now.AddSeconds(-1812));

                    byte[] crl = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // The length of the output depends on a number of factors, but they're all stable
                    // for this test (since it doesn't use ECDSA's variable-length, non-deterministic, signature)
                    //
                    // In fact, because RSASSA-PKCS1 is a deterministic algorithm, we can check it for a fixed output.
                    AssertExtensions.SequenceEqual(BuildSingleEntryExpectedCrl, crl);
                });
        }

        [Fact]
        public static void BuildSingleEntryWithReason()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    builder.AddEntry(
                        stackalloc byte[] { 0x01, 0x01, 0x02, 0x03, 0x05, 0x08, 0x0C, 0x15 },
                        now.AddSeconds(-1812),
                        X509RevocationReason.KeyCompromise);

                    byte[] explicitUpdateTime = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // The length of the output depends on a number of factors, but they're all stable
                    // for this test (since it doesn't use ECDSA's variable-length, non-deterministic, signature)
                    //
                    // In fact, because RSASSA-PKCS1 is a deterministic algorithm, we can check it for a fixed output.

                    byte[] expected = (
                        "308201CA3081B3020101300D06092A864886F70D01010B050030253123302106" +
                        "03550403131A4275696C6453696E676C65456E74727957697468526561736F6E" +
                        "170D3133303430363037353830395A170D3133303430363038303330395A3029" +
                        "302702080101020305080C15170D3133303430363037323735375A300C300A06" +
                        "03551D1504030A0101A02F302D301F0603551D2304183016801478A5C75D5166" +
                        "7331D5A96924114C9B5FA00D7BCB300A0603551D14040302017B300D06092A86" +
                        "4886F70D01010B0500038201010055283C97666765D19AABFFDAA36112781957" +
                        "1FCA3CE68AA00DAFDFF784F8F34D0EFF4EC8659A26A254DDDC9BBD7D664E0160" +
                        "4D3696209B5A4B0FFF57102BC8AA17FED0D33AD3452BE5E22269E78BB4084698" +
                        "28E2814EA8E6B8003EBB7AC727DAD912580F941C6D2616195C083218F997D682" +
                        "966CC6EEB810B815ABA991135469E2CD2915EE7C0FCB387C0B6169E0F1F2CFD8" +
                        "2274D134DB2C27826E04138FF8C7AB4B8678AF53C3904C09F1F9589D5325E5D4" +
                        "3F2A7F2EF81BD19DE5362181B9E0603DE98F664F98A6599A3BFB9AAFA2DC3491" +
                        "9305B8812BC11BFA06C6550A257396766B750D10B6C6BEA7A193E4D3F4C2FEFD" +
                        "FC2B875D1A2BFDB849EBBCFC767B").HexToByteArray();

                    AssertExtensions.SequenceEqual(expected, explicitUpdateTime);
                });
        }

        [Fact]
        public static void AddTwiceRemoveOnce()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    ReadOnlySpan<byte> serial = new byte[] { 0x01, 0x01, 0x02, 0x03, 0x05, 0x08, 0x0C, 0x15 };

                    builder.AddEntry(serial, now.AddSeconds(-1812));

                    // This entry will have a revocation time based on DateTimeOffset.UtcNow, so it's unpredictable.
                    // But, that's OK, because we're going to remove it.
                    builder.AddEntry(serial);

                    // Remove only the last one.
                    Assert.True(builder.RemoveEntry(serial), "builder.RemoveEntry(serial)");

                    byte[] explicitUpdateTime = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // The length of the output depends on a number of factors, but they're all stable
                    // for this test (since it doesn't use ECDSA's variable-length, non-deterministic, signature)
                    //
                    // In fact, because RSASSA-PKCS1 is a deterministic algorithm, we can check it for a fixed output.

                    byte[] expected = (
                        "308201B430819D020101300D06092A864886F70D01010B0500301D311B301906" +
                        "035504031312416464547769636552656D6F76654F6E6365170D313330343036" +
                        "3037353830395A170D3133303430363038303330395A301B3019020801010203" +
                        "05080C15170D3133303430363037323735375AA02F302D301F0603551D230418" +
                        "3016801478A5C75D51667331D5A96924114C9B5FA00D7BCB300A0603551D1404" +
                        "0302017B300D06092A864886F70D01010B050003820101005B959102271A96F4" +
                        "4EF37B6C7D1BC566875C6CB2B45B5F32CE474155890047EAD9CF74A97E89CA4B" +
                        "2139417167B0EDC537300A5271F399820E1D2B326DF85FD4F3249B4D0AE0B067" +
                        "5662986E44E2041E1DADC4A3F557FFE6E50DB12E12BE5A6734BD3EBD537D348D" +
                        "DD454C2310AEFC586722730252AA63F20CCF8E5127E5A2E5FDD0F16E1296E831" +
                        "03730D6ACA32584D33DC51B6075000507A808EDC012C982BF9969970C115D0BB" +
                        "BEDB56089C5E3A51FD1E6180088BDEC343976E42BE4F04798E19B043D5295E1D" +
                        "A9C0371F6E62CED8626E65804E13A9D28D5A9458AAE6DEC3E06B43E236EDEA55" +
                        "6AAA7E7A32930C2E8289D62E1CBF7AFAB632FF260B1B49F9").HexToByteArray();

                    AssertExtensions.SequenceEqual(expected, explicitUpdateTime);
                });
        }

        [Fact]
        public static void UnsupportedRevocationReasons()
        {
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
            byte[] serial = { 1, 2, 3 };
            const string ParamName = "reason";

            Assert.Throws<ArgumentOutOfRangeException>(
                ParamName,
                () => builder.AddEntry(serial, reason: (X509RevocationReason)(-1)));

            Assert.Throws<ArgumentOutOfRangeException>(
                ParamName,
                () => builder.AddEntry(serial, reason: (X509RevocationReason)(-2)));

            Assert.Throws<ArgumentOutOfRangeException>(
                ParamName,
                () => builder.AddEntry(serial, reason: (X509RevocationReason)7));

            Assert.Throws<ArgumentOutOfRangeException>(
                ParamName,
                () => builder.AddEntry(serial, reason: (X509RevocationReason)12));

            Assert.Throws<ArgumentOutOfRangeException>(
                ParamName,
                () => builder.AddEntry(serial, reason: X509RevocationReason.AACompromise));

            Assert.Throws<ArgumentOutOfRangeException>(
                ParamName,
                () => builder.AddEntry(serial, reason: X509RevocationReason.RemoveFromCrl));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
        public static void DsaNotDirectlySupported()
        {
            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            X500DistinguishedName dn = new X500DistinguishedName("CN=DSA CA");

            using (DSA key = DSA.Create(TestData.GetDSA1024Params()))
            {
                DSAX509SignatureGenerator gen = new DSAX509SignatureGenerator(key);

                CertificateRequest req = new CertificateRequest(
                    dn,
                    gen.PublicKey,
                    HashAlgorithmName.SHA1);

                req.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForCertificateAuthority());

                byte[] serial = { 1, 2, 3, };

                using (X509Certificate2 certPub = req.Create(dn, gen, now.AddMonths(-1), now.AddYears(1), serial))
                using (X509Certificate2 cert = certPub.CopyWithPrivateKey(key))
                {
                    ArgumentException ex = Assert.Throws<ArgumentException>(
                        "issuerCertificate",
                        () => builder.Build(
                            cert,
                            BigInteger.One,
                            DateTimeOffset.UtcNow.AddMonths(3),
                            HashAlgorithmName.SHA1));

                    Assert.Contains("key algorithm", ex.Message);

                    X509AuthorityKeyIdentifierExtension akid =
                        X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, false, true);

                    // Rewrite it as critical
                    akid = new X509AuthorityKeyIdentifierExtension(akid.RawData, critical: true);
                    DateTimeOffset nextUpdate = DateTimeOffset.UtcNow.AddMonths(3);
                    int crlNumber = RandomNumberGenerator.GetInt32(0, 1 + short.MaxValue);

                    byte[] crl = builder.Build(
                        cert.SubjectName,
                        gen,
                        crlNumber,
                        nextUpdate,
                        HashAlgorithmName.SHA1,
                        akid);

                    AsnReader reader = new AsnReader(crl, AsnEncodingRules.DER);
                    reader = reader.ReadSequence();
                    ReadOnlyMemory<byte> tbs = reader.ReadEncodedValue();
                    // signatureAlgorithm
                    reader.ReadEncodedValue();
                    byte[] signature = reader.ReadBitString(out _);
                    reader.ThrowIfNotEmpty();

                    Assert.True(
                        key.VerifyData(tbs.Span, signature, HashAlgorithmName.SHA1, DSASignatureFormat.Rfc3279DerSequence),
                        "Signing key verifies the CRL");

                    VerifyCrlFields(
                        crl,
                        cert.SubjectName,
                        null,
                        nextUpdate,
                        akid,
                        crlNumber);
                }
            }
        }

        [Fact]
        public static void AddInvalidSerial()
        {
            byte[] invalidPositive = new byte[] { 0x00, 0x7F };
            byte[] invalidNegative = new byte[] { 0xFF, 0x80 };

            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

            Assert.Throws<ArgumentException>("serialNumber", () => builder.AddEntry(invalidPositive));
            Assert.Throws<ArgumentException>("serialNumber", () => builder.AddEntry(invalidNegative));

            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => builder.AddEntry(new ReadOnlySpan<byte>(invalidPositive)));
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => builder.AddEntry(new ReadOnlySpan<byte>(invalidNegative)));
        }

        [Fact]
        public static void RemoveMissing_ReturnsFalse()
        {
            byte[] needle = { 0x03, 0x02, 0x01 };

            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

            Assert.False(builder.RemoveEntry(needle), "builder.RemoveEntry(array)");
            Assert.False(builder.RemoveEntry(needle.AsSpan()), "builder.RemoveEntry(span)");

            builder.AddEntry(needle);

            Array.Reverse(needle);
            Assert.False(builder.RemoveEntry(needle), "builder.RemoveEntry(array)");
            Assert.False(builder.RemoveEntry(needle.AsSpan()), "builder.RemoveEntry(span)");
        }

        [Fact]
        public static void RemovePresent_ReturnsTrue()
        {
            byte[] needle = { 0x03, 0x02, 0x01 };

            CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

            builder.AddEntry(needle.AsSpan());
            Assert.True(builder.RemoveEntry(needle), "builder.RemoveEntry(array)");
            Assert.False(builder.RemoveEntry(needle.AsSpan()), "builder.RemoveEntry(span) when missing");

            builder.AddEntry(needle);
            Assert.True(builder.RemoveEntry(needle.AsSpan()), "builder.RemoveEntry(span)");
            Assert.False(builder.RemoveEntry(needle), "builder.RemoveEntry(array) when missing");
        }

        [Fact]
        public static void ThisUpdate2049NextUpdate2050()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    DateTimeOffset now = new DateTimeOffset(2049, 12, 31, 23, 59, 59, TimeSpan.Zero);

                    byte[] built = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // RSASSA-PKCS1, so we can check for exact bytes.

                    byte[] expected = (
                        "308201A330818C020101300D06092A864886F70D01010B050030273125302306" +
                        "03550403131C54686973557064617465323034394E6578745570646174653230" +
                        "3530170D3439313233313233353935395A180F32303530303130313030303435" +
                        "395AA02F302D301F0603551D2304183016801478A5C75D51667331D5A9692411" +
                        "4C9B5FA00D7BCB300A0603551D14040302017B300D06092A864886F70D01010B" +
                        "0500038201010030CAB8946944FFD3958A42AA94851D3EEDB533516926A0661B" +
                        "91D41F6526876A345021377F9FFC0372EC744C85CF2FD51458D898EC8D26A0C5" +
                        "C3FE0C616AB1EC1E8E90A45BA7543D9009B21AE2EC98DF55497DB299B6DD2363" +
                        "2619C1E0FB29AA0F85C9DB59901D2A995C6B56D6CAD74E7840EDC3F09A3D6FD9" +
                        "455569F554CF4CDB04BDC3775C9E7C48EBC85B818D00DB55B6FDF62CC22427A5" +
                        "DE1BF178C18FE28726A853D89B4299FA241328F8CDD801843B8F24128217020E" +
                        "2F7D2E2B5F4993F82E6B33B5C515D576BE78F55847A544FC8869B4FB9DF2E66D" +
                        "3D222B9A3BA511E6AF3CBDBC54F5B44C49571F2432E5C6CA4F11510C822BA808" +
                        "0C87EBEAD6728B").HexToByteArray();

                    Assert.Equal(expected, built);
                });
        }

        [Fact]
        public static void ThisUpdate2050()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    DateTimeOffset now = new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero);

                    byte[] built = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // RSASSA-PKCS1, so we can check for exact bytes.

                    byte[] expected = (
                        "30820197308180020101300D06092A864886F70D01010B050030193117301506" +
                        "03550403130E5468697355706461746532303530180F32303530303130313030" +
                        "303030305A180F32303530303130313030303530305AA02F302D301F0603551D" +
                        "2304183016801478A5C75D51667331D5A96924114C9B5FA00D7BCB300A060355" +
                        "1D14040302017B300D06092A864886F70D01010B05000382010100ACB629E3A6" +
                        "CCF5B32204E149FD5A193657EB687942B93153275BC32D8E92C318BE5484EA53" +
                        "609BEC03F6BE62CF06BE11EF203A3A8F296D635D265202AD285EDFDB286DD814" +
                        "33ED645D1093CE70AB77F840658C6A219ACF35E394A1A4E05334E6B27FAC8288" +
                        "D37EB75F31540CB7C3AD05178C4F7552AAA59472C9D457C26B2D4D37A3E394AF" +
                        "00577D174B6015C2673E951B34720E6D1CCB97D1B4A70B88C0B89CDC27B56D9D" +
                        "A3D8974B1B4B37CFC4EBFAA9DC9466ACE56D0835CD848DB112918523A74AD398" +
                        "0CEE5F70C8C5C2610111C6EC72A68CAC314C4F516D697C3B52F16A109CFC526A" +
                        "2F26EDF7B69DD7D630266BC82B5A5AB265E06847280B5A2C658F3E").HexToByteArray();

                    Assert.Equal(expected, built);
                });
        }

        [Fact]
        public static void AddEntryFromCertificate()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA512;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2016, 3, 8, 4, 59, 7, TimeSpan.Zero);
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    // This certificate certainly wasn't issued by the CA cert we just built for this test,
                    // but that's OK, because CRLBuilder doesn't check.
                    using (X509Certificate2 toRevoke = new X509Certificate2(TestData.Dsa1024Cert))
                    {
                        builder.AddEntry(toRevoke, now.AddSeconds(-1066));
                    }

                    byte[] crl = builder.Build(cert, 122, now.AddMinutes(5), hashAlg, pad, now);

                    // RSASSA-PKCS1, so we can check for exact bytes.
                    AssertExtensions.SequenceEqual(AddEntryFromCertificateExpectedCrl, crl);
                });
        }

        [Fact]
        public static void AddEntryWithReasonFromCertificate()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA384;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    // This certificate certainly wasn't issued by the CA cert we just built for this test,
                    // but that's OK, because CRLBuilder doesn't check.
                    using (X509Certificate2 toRevoke = new X509Certificate2(TestData.Dsa1024Cert))
                    {
                        builder.AddEntry(
                            toRevoke,
                            now.AddSeconds(-1066),
                            X509RevocationReason.PrivilegeWithdrawn);
                    }

                    byte[] crl = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // RSASSA-PKCS1, so we can check for exact bytes.
                    byte[] expected = (
                        "308201D23081BB020101300D06092A864886F70D01010C0500302C312A302806" +
                        "035504031321416464456E74727957697468526561736F6E46726F6D43657274" +
                        "69666963617465170D3133303430363037353830395A170D3133303430363038" +
                        "303330395A302A3028020900AB740A714AA83C92170D31333034303630373430" +
                        "32335A300C300A0603551D1504030A0109A02F302D301F0603551D2304183016" +
                        "801478A5C75D51667331D5A96924114C9B5FA00D7BCB300A0603551D14040302" +
                        "017B300D06092A864886F70D01010C050003820101005E3A2471601767B6C257" +
                        "D1AEE84ABAE16FD40EF129F3F0CD5F4B1B42B152FB21E750032AF87C415E738C" +
                        "C0757FA3A4CEE841955EF863EDE8B84E5429950D612E5AF53D5113EE5F96FB14" +
                        "28768C71B43ED143CAAD5F21DA554E589F73D94F236F3DC51A562CE5897745E4" +
                        "1C99537352D3442F120D7BFA45C35DCE5872E4E35EFA31D1B31BC17426312D52" +
                        "1FF4AA79FC6E0BE28B840F736DDBD9171733667554F473B37C092B1BECC21256" +
                        "78943A94C254CD539146F01449772EEBD9FC8FCDE9CE0E8305532E193A2FF761" +
                        "73B030AC291AE0B9471A3A2A4E15299AEF9CD89D20F802444DAC6BD277C3D45C" +
                        "F7AF4A60117A20431AF0ABCBAFA0A52E6EF5E47793CD").HexToByteArray();

                    AssertExtensions.SequenceEqual(expected, crl);
                });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void LoadRemoveAndBuild(bool oversized)
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);

                    ReadOnlySpan<byte> toLoad = AddEntryFromCertificateExpectedCrl;

                    if (oversized)
                    {
                        byte[] arr = new byte[toLoad.Length + 23];
                        Span<byte> write = arr.AsSpan(1);
                        toLoad.CopyTo(write);
                        toLoad = write;
                    }

                    CertificateRevocationListBuilder builder = CertificateRevocationListBuilder.Load(
                        AddEntryFromCertificateExpectedCrl,
                        out BigInteger currentCrlNumber,
                        out int bytesConsumed);

                    Assert.Equal(AddEntryFromCertificateExpectedCrl.Length, bytesConsumed);
                    Assert.Equal(122, currentCrlNumber);

                    using (X509Certificate2 toUnRevoke = new X509Certificate2(TestData.Dsa1024Cert))
                    {
                        Assert.True(builder.RemoveEntry(toUnRevoke.SerialNumberBytes.Span));
                    }

                    currentCrlNumber++;

                    byte[] built = builder.Build(cert, currentCrlNumber, now.AddMinutes(5), hashAlg, pad, now);

                    // RSASSA-PKCS1, so we can check for exact bytes.
                    //
                    // We're using a different signing algorithm, a different date, and a different
                    // CRL number than the CRL we loaded.  Because of all that, this looks exactly
                    // like the CRL from the BuildEmpty test.
                    AssertExtensions.SequenceEqual(BuildEmptyExpectedCrl, built);
                },
                // Use the same cert subject name as the BuildEmpty test.
                callerName: nameof(BuildEmpty));
        }

        [Fact]
        public static void LoadAddAndBuild()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;

                    // There is one entry in this CRL, SN:00AB740A714AA83C92 was revoked
                    // 2013-04-06T07:40:23Z and one extension (revoked for reason 9).
                    // The nextUpdate field (OPTIONAL in the spec) is not present.
                    // The signature is completely invalid (it's 0 bits long),
                    // but we don't check that, since we don't know what key to use.
                    byte[] toLoad = (
                        "3081B030819B020101300D06092A864886F70D01010C0500301B311930170603" +
                        "5504031610496E76616C69646C79205369676E6564170D313330343036303735" +
                        "3830395A302A3028020900AB740A714AA83C92170D3133303430363037343032" +
                        "335A300C300A0603551D1504030A0109A02F302D301F0603551D230418301680" +
                        "1478A5C75D51667331D5A96924114C9B5FA00D7BCB300A0603551D1404030201" +
                        "7B300D06092A864886F70D01010C0500030100").HexToByteArray();

                    DateTimeOffset now = new DateTimeOffset(2053, 9, 8, 7, 56, 41, TimeSpan.Zero);

                    CertificateRevocationListBuilder builder = CertificateRevocationListBuilder.Load(
                        toLoad,
                        out BigInteger currentCrlNumber);

                    Assert.Equal(123, currentCrlNumber);

                    ReadOnlySpan<byte> nextToRevoke = new byte[] { 0x0A, 0x03, 0x04 };
                    builder.AddEntry(nextToRevoke, now.AddSeconds(-2), X509RevocationReason.KeyCompromise);

                    BigInteger nextCrlNumber = BigInteger.Parse("20530908075641000000000000000000001");
                    byte[] built = builder.Build(cert, nextCrlNumber, now.AddDays(1), hashAlg, pad, now);

                    // RSASSA-PKCS1, so we can check for exact bytes.
                    byte[] expected = (
                        "308201F83081E1020101300D06092A864886F70D01010B0500301A3118301606" +
                        "03550403130F4C6F6164416464416E644275696C64180F323035333039303830" +
                        "37353634315A180F32303533303930393037353634315A30503028020900AB74" +
                        "0A714AA83C92170D3133303430363037343032335A300C300A0603551D150403" +
                        "0A0109302402030A0304180F32303533303930383037353633395A300C300A06" +
                        "03551D1504030A0101A03D303B301F0603551D2304183016801478A5C75D5166" +
                        "7331D5A96924114C9B5FA00D7BCB30180603551D140411020F03F4407DDE4753" +
                        "F848A9D7F9A00001300D06092A864886F70D01010B05000382010100041092BF" +
                        "3931B87B3756111412763E0612BC3DBE52366904F4558C316901A724790BF5D6" +
                        "201AC99E79ABDE56AEAF019E78398B230D669F6DA3A3E8607971729D85E83EDE" +
                        "9F7626333032E4785377C0C04C2E5F5B25D0D79B7A0F1AA34DA23AEAAFB578E2" +
                        "AB41CD1DFAB4AD19CA049851FF941DA37173BA974B68A9469D7CF7987C97BB29" +
                        "D16889749177D284ADB629BEBC5C7AC872E63F7D4A02A6E8B9BA05B1476C5711" +
                        "263A124BAF87B84F3A4B064929A2679E5C8D41B6C39DDDAFEB2E9092A3CAF13B" +
                        "31D6CD8C4EF88E09BE44EE8EA896315A0C6E8A79DD13ACD78B92E349514866C7" +
                        "69A28F554C7BE6FDEC59ADD39D607F548E2F05C086FEDF9439862720").HexToByteArray();

                    AssertExtensions.SequenceEqual(expected, built);
                });
        }

        [Fact]
        public static void LoadPreservesUnknownExtensions()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;

                    // There are two entries in this CRL:
                    //   * SN:00AB740A714AA83C92 was revoked 2013-04-06T07:40:23Z
                    //     and has no extensions (no revocation reason).
                    //   * SN:0A0304 was revoked 2053-09-08T07:56:39Z and has two extensions:
                    //     * Revocation reason (1)
                    //     * rsaEncryptionWithSha256 (yep, nonsense) with a nonsense payload
                    // The signature is completely invalid (it's 1 bit long),
                    // but we don't check that, since we don't know what key to use.
                    // It also uses a different signature algorithm in the TBS and the signature,
                    // one of which says the algorithm was CRLReason.
                    byte[] toLoad = (
                        "3081F53081E5020101300D06092A864886F70D01010B05003016311430120603" +
                        "550403130B5374696C6C205765697264180F3230353330393038303735363431" +
                        "5A180F32303533303930393037353634315A3058301A020900AB740A714AA83C" +
                        "92170D3133303430363037343032335A303A02030A0304180F32303533303930" +
                        "383037353633395A3022300A0603551D1504030A0101301406092A864886F70D" +
                        "01010B040730050400020103A03D303B301F0603551D2304183016801478A5C7" +
                        "5D51667331D5A96924114C9B5FA00D7BCB30180603551D140411020F03F4407D" +
                        "DE4753F848A9D7F9A0000130070603551D14050003020780").HexToByteArray();

                    DateTimeOffset now = new DateTimeOffset(1949, 9, 8, 7, 56, 41, TimeSpan.Zero);

                    CertificateRevocationListBuilder builder = CertificateRevocationListBuilder.Load(
                        toLoad,
                        out BigInteger currentCrlNumber);

                    BigInteger expectedCrlNumber = BigInteger.Parse("20530908075641000000000000000000001");

                    Assert.Equal(expectedCrlNumber, currentCrlNumber);

                    ReadOnlySpan<byte> nextToRevoke = new byte[] { 0x15, 0x84, 0x57, 0x1B };
                    builder.AddEntry(nextToRevoke, now.AddSeconds(-2));

                    BigInteger nextCrlNumber = currentCrlNumber + 12;
                    byte[] built = builder.Build(cert, nextCrlNumber, now.AddYears(1), hashAlg, pad, now);

                    // RSASSA-PKCS1, so we can check for exact bytes.
                    byte[] expected = (
                        "308202273082010F020101300D06092A864886F70D01010B0500302931273025" +
                        "0603550403131E4C6F6164507265736572766573556E6B6E6F776E457874656E" +
                        "73696F6E73180F31393439303930383037353634315A170D3530303930383037" +
                        "353634315A3071301A020900AB740A714AA83C92170D31333034303630373430" +
                        "32335A303A02030A0304180F32303533303930383037353633395A3022300A06" +
                        "03551D1504030A0101301406092A864886F70D01010B04073005040002010330" +
                        "1702041584571B180F31393439303930383037353633395AA03D303B301F0603" +
                        "551D2304183016801478A5C75D51667331D5A96924114C9B5FA00D7BCB301806" +
                        "03551D140411020F03F4407DDE4753F848A9D7F9A0000D300D06092A864886F7" +
                        "0D01010B05000382010100359B39840DF4516EEC6F02757B0B9A4638AA6B59A6" +
                        "B159785EB3ABC03AB1F71807657C6AEC488C0E7103D5D7C936B704B727F8DCF1" +
                        "C1E88920C200A9EE36522ED50AF0E1D9C404101007E65D52359AA46A52044195" +
                        "4BE506C2217810888865BD8EBED1F87144EC5364E082EDAC23F197EAD135225C" +
                        "343483FE671B2849A9D4F83B75B6A70D7DA8DD12CC7561D1FA059A636D5F1272" +
                        "9C18D3FFED99F0E3D9A2EBADBE452A4D127777D52538BAFDDD9F828CC3060A30" +
                        "366831CD9D8E92DECA397527CAEE1133FFF6F9F0648E6D86AE86FC19B1CB551B" +
                        "3CFA0F490B7AFDD6286C3C99B83C4BD1D1B8509E332C2212CB22D5BCD2532741" +
                        "34875DDE2FE7062BA2F2B6"
                    ).HexToByteArray();

                    AssertExtensions.SequenceEqual(expected, built);
                });
        }

        [Fact]
        public static void LoadInvalid()
        {
            byte[] invalid = { 0x3 };
            BigInteger crlNumber = BigInteger.MinusOne;
            int bytesConsumed = -1;

            Assert.Throws<CryptographicException>(
                () => CertificateRevocationListBuilder.Load(invalid, out crlNumber));

            Assert.Equal(BigInteger.MinusOne, crlNumber);

            Assert.Throws<CryptographicException>(
                () => CertificateRevocationListBuilder.Load(
                    new ReadOnlySpan<byte>(invalid),
                    out crlNumber,
                    out bytesConsumed));

            Assert.Equal(BigInteger.MinusOne, crlNumber);
            Assert.Equal(-1, bytesConsumed);
        }

        [Fact]
        public static void LoadEmpty()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);

                    CertificateRevocationListBuilder builder = CertificateRevocationListBuilder.Load(
                        BuildEmptyExpectedCrl,
                        out BigInteger currentCrlNumber,
                        out int bytesConsumed);

                    Assert.Equal(BuildEmptyExpectedCrl.Length, bytesConsumed);
                    Assert.Equal(123, currentCrlNumber);

                    ReadOnlySpan<byte> serialToAdd =
                        new byte[] { 0x01, 0x01, 0x02, 0x03, 0x05, 0x08, 0x0C, 0x15 };

                    builder.AddEntry(serialToAdd, now.AddSeconds(-1812));

                    byte[] crl = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // RSASSA-PKCS1, and built to look just like BuildSingleEntry.
                    AssertExtensions.SequenceEqual(BuildSingleEntryExpectedCrl, crl);
                },
                callerName: nameof(BuildSingleEntry));
        }

        [Fact]
        public static void LoadEmptyPem()
        {
            BuildRsaCertificateAndRun(
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (cert, notNow) =>
                {
                    HashAlgorithmName hashAlg = HashAlgorithmName.SHA256;
                    RSASignaturePadding pad = RSASignaturePadding.Pkcs1;
                    DateTimeOffset now = new DateTimeOffset(2013, 4, 6, 7, 58, 9, TimeSpan.Zero);

                    string toLoad = $@"
This is text before the PRE-EB.
-----BEGIN RANDOM-----
-----END RANDOM-----
-----BEGIN X509 CRL-----
This is text that is poorly encapsulated, so we skip it.
-----END CERTIFICATE-----
More random text.
{PemEncoding.WriteString("X509 CRL", BuildEmptyExpectedCrl)}
The next entry is invalid, but we don't read it.
-----BEGIN X509 CRL-----
AQAB
-----END X509 CRL-----";

                    CertificateRevocationListBuilder builder = CertificateRevocationListBuilder.LoadPem(
                        toLoad,
                        out BigInteger currentCrlNumber);

                    Assert.Equal(123, currentCrlNumber);

                    ReadOnlySpan<byte> serialToAdd =
                        new byte[] { 0x01, 0x01, 0x02, 0x03, 0x05, 0x08, 0x0C, 0x15 };

                    builder.AddEntry(serialToAdd, now.AddSeconds(-1812));

                    byte[] crl = builder.Build(cert, 123, now.AddMinutes(5), hashAlg, pad, now);

                    // RSASSA-PKCS1, and built to look just like BuildSingleEntry.
                    AssertExtensions.SequenceEqual(BuildSingleEntryExpectedCrl, crl);
                },
                callerName: nameof(BuildSingleEntry));
        }

        [Fact]
        public static void LoadOversizedArray()
        {
            byte[] oversized = new byte[AddEntryFromCertificateExpectedCrl.Length + 1];
            AddEntryFromCertificateExpectedCrl.CopyTo(oversized);

            BigInteger crlNumber = BigInteger.MinusOne;

            Assert.Throws<CryptographicException>(
                () => CertificateRevocationListBuilder.Load(oversized, out crlNumber));

            Assert.Equal(BigInteger.MinusOne, crlNumber);
        }

        [Fact]
        public static void LoadPem_InvalidBeforeValid()
        {
            string pem = $@"
-----BEGIN X509 CRL-----
AQAB
-----END X509 CRL-----
{PemEncoding.WriteString("X509 CRL", BuildSingleEntryExpectedCrl)}";

            BigInteger currentCrlNumber = BigInteger.MinusOne;

            Assert.Throws<CryptographicException>(
                () => CertificateRevocationListBuilder.LoadPem(pem, out currentCrlNumber));

            Assert.Equal(BigInteger.MinusOne, currentCrlNumber);
        }

        [Fact]
        public static void LoadPem_NoCrls()
        {
            BigInteger currentCrlNumber = BigInteger.MinusOne;

            Assert.Throws<CryptographicException>(
                () => CertificateRevocationListBuilder.LoadPem(
                    System.Text.Encoding.ASCII.GetString(TestData.Pkcs7ChainPemBytes),
                    out currentCrlNumber));

            Assert.Equal(BigInteger.MinusOne, currentCrlNumber);
        }

        [Fact]
        public static void LoadAndResignPublicCrl()
        {
            const string ExistingCrl = @"
-----BEGIN X509 CRL-----
MIIE3TCCAsUCAQEwDQYJKoZIhvcNAQEMBQAwWTELMAkGA1UEBhMCVVMxHjAcBgNV
BAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEqMCgGA1UEAxMhTWljcm9zb2Z0IEF6
dXJlIFRMUyBJc3N1aW5nIENBIDA2Fw0yMjA0MTExMzA4MjJaFw0yMjA0MjExMzI4
MjJaMIIB1DAyAhMzACC5qJDtqzKZrS9eAAAAILmoFw0yMTExMTcxODQ4NTNaMAww
CgYDVR0VBAMKAQQwMgITMwAdIZwN8oowgaBVuwAAAB0hnBcNMjExMDI5MjI0MjM5
WjAMMAoGA1UdFQQDCgEBMDICEzMAHEsDMKz+x2Q4pB0AAAAcSwMXDTIxMTAyOTIy
NDIzOVowDDAKBgNVHRUEAwoBATAyAhMzABv5eo1EVElhuLhdAAAAG/l6Fw0yMTEw
MjkyMjQyMzlaMAwwCgYDVR0VBAMKAQEwMgITMwAVsCZAqBt1i9Ox9AAAABWwJhcN
MjExMDI5MjI0MjM5WjAMMAoGA1UdFQQDCgEBMDICEzMAFEFG+rIYXhcZLCAAAAAU
QUYXDTIxMTAyOTIyNDIzOVowDDAKBgNVHRUEAwoBATAyAhMzABKV+sL8KCADS9O+
AAAAEpX6Fw0yMTEwMjkyMjQyMzlaMAwwCgYDVR0VBAMKAQEwMgITMwAQVfwHnnKI
5upijgAAABBV/BcNMjExMDI5MjI0MjM5WjAMMAoGA1UdFQQDCgEBMDICEzMAGLfg
D+AWIpVdxQ8AAAAYt+AXDTIxMTAyODIzNDQyMlowDDAKBgNVHRUEAwoBAaBgMF4w
HwYDVR0jBBgwFoAU1cFnOsKjnfR3UltZEjgp5lVou6UwEAYJKwYBBAGCNxUBBAMC
AQAwCwYDVR0UBAQCAgu4MBwGCSsGAQQBgjcVBAQPFw0yMjA0MTYxMzE4MjJaMA0G
CSqGSIb3DQEBDAUAA4ICAQCXu0H7d26rPD/FLZUVoQNkLFZDef2mVL5MVhMGCTXf
f0Bg2IYWOPYonmtUTFjXDhBDM9mh5aKY37s46tT4p+H3A6N4tneSkZ8990IIOh75
PlC/dBT6tikF98Y6fB/Z0ZurlazJQDH3tKBjxcKtjYLjzCFN8uYumtoyBteJj1gt
JgElPT6S7R+xvydrChjKLCRbmc2B3AggVHz0TVvUX9if8qiPb25a1Uu24nVKDaz9
gWsEZohsETi+qSRRViKs+ZFy/WBqySwW/6Qv4JbPhY4VhPyZxkvWzSh7HbqTUA3/
TahuDpwexc/KfebeG2XKGxVEOBYncRaeEISEBnan+LeUtK9Jf8kShTeJeyiV3bgu
N3/vBCOLV2/dX47iATPeYv/ou7L+i6u0U1MUcLO9ZJW3pwzBuIEeGZvnrTKCcWDV
G65Yc7rKKcqPdbYzIPsGCc1/Jo6qK9cYxt/OEz88VKe/ruu/Stce9bcPjo8YDkmx
zisDKD0EGD3iSR7gm9xEwYgh6hMPeqZVU9T5/0Efw3wKr9AGVW1zTi3486DyanHW
h8FDgbPDXKXNpzkD4a2usnEeHX9YGPyIiFSqsv5vWBIT4mpMYfkCs7IPadt79N6Z
PMzkCtzeqlHvuzIHHNcS1aNvlb94Tg8tPR5u/deYDrNg4NkbsqpG/QUMWse4T1Q7
+w==
-----END X509 CRL-----";

            using (RSA rsa = RSA.Create(4096))
            {
                X500DistinguishedName parentDn = new X500DistinguishedName("CN=Parent");
                X500DistinguishedName dn = new X500DistinguishedName(
                    "CN=Microsoft Azure TLS Issuing CA 06, O=Microsoft Corporation, C=US");

                CertificateRequest req = new CertificateRequest(
                    dn,
                    rsa,
                    HashAlgorithmName.SHA384,
                    RSASignaturePadding.Pkcs1);

                req.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForCertificateAuthority());
                req.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(
                        "D5C1673AC2A39DF477525B59123829E65568BBA5".HexToByteArray(), critical: false));

                DateTimeOffset thisUpdate = new DateTimeOffset(2022, 4, 11, 13, 8, 22, TimeSpan.Zero);
                DateTimeOffset nextUpdate = new DateTimeOffset(2022, 4, 21, 13, 28, 22, TimeSpan.Zero);

                X509Certificate2 pubCert = req.Create(
                    parentDn,
                    X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                    thisUpdate.AddMinutes(-1),
                    nextUpdate.AddDays(3),
                    new byte[] { 0x01, 0x02, 0x03, 0x05 });

                using (X509Certificate2 cert = pubCert.CopyWithPrivateKey(rsa))
                {
                    pubCert.Dispose();

                    CertificateRevocationListBuilder builder =
                        CertificateRevocationListBuilder.LoadPem(ExistingCrl, out BigInteger currentCrlNumber);

                    Assert.Equal(3000, currentCrlNumber);

                    byte[] crl = builder.Build(
                        cert,
                        currentCrlNumber,
                        nextUpdate,
                        HashAlgorithmName.SHA384,
                        RSASignaturePadding.Pkcs1,
                        thisUpdate);

                    PemFields pemFields = PemEncoding.Find(ExistingCrl);
                    byte[] currentCrl = Convert.FromBase64String(ExistingCrl[pemFields.Base64Data]);

                    AsnReader ourReader = new AsnReader(crl, AsnEncodingRules.DER);
                    AsnReader theirReader = new AsnReader(currentCrl, AsnEncodingRules.DER);

                    // Move into the CRL SEQUENCE
                    ourReader = ourReader.ReadSequence();
                    theirReader = theirReader.ReadSequence();

                    // TBS
                    ourReader = ourReader.ReadSequence();
                    theirReader = theirReader.ReadSequence();

                    // Version (same)
                    AssertExtensions.SequenceEqual(
                        theirReader.ReadEncodedValue().Span,
                        ourReader.ReadEncodedValue().Span);

                    // Signature Algorithm (same)
                    AssertExtensions.SequenceEqual(
                        theirReader.ReadEncodedValue().Span,
                        ourReader.ReadEncodedValue().Span);

                    // Issuer (same)
                    AssertExtensions.SequenceEqual(
                        theirReader.ReadEncodedValue().Span,
                        ourReader.ReadEncodedValue().Span);

                    // thisUpdate (same)
                    AssertExtensions.SequenceEqual(
                        theirReader.ReadEncodedValue().Span,
                        ourReader.ReadEncodedValue().Span);

                    // nextUpdate (same)
                    AssertExtensions.SequenceEqual(
                        theirReader.ReadEncodedValue().Span,
                        ourReader.ReadEncodedValue().Span);

                    // revokedCertificates (same)
                    AssertExtensions.SequenceEqual(
                        theirReader.ReadEncodedValue().Span,
                        ourReader.ReadEncodedValue().Span);

                    // Their CRL has two extensions we don't:
                    // * 1.3.6.1.4.1.311.21.1
                    // * 1.3.6.1.4.1.311.21.4
                    //
                    // This makes their extensions group bigger, and why the TBS couldn't be fully compared.
                }
            }
        }

        private static void BuildCertificateAndRun(
            IEnumerable<X509Extension> extensions,
            Action<X509Certificate2, DateTimeOffset> action,
            bool addSubjectKeyIdentifier = true,
            [CallerMemberName] string callerName = null)
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    $"CN=\"{callerName}\"",
                    key,
                    HashAlgorithmName.SHA384);

                if (addSubjectKeyIdentifier)
                {
                    req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
                }

                foreach (X509Extension ext in extensions)
                {
                    req.CertificateExtensions.Add(ext);
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = req.CreateSelfSigned(now.AddMonths(-1), now.AddMonths(1)))
                {
                    action(cert, now);
                }
            }
        }

        private static void BuildRsaCertificateAndRun(
            IEnumerable<X509Extension> extensions,
            Action<X509Certificate2, DateTimeOffset> action,
            bool addSubjectKeyIdentifier = true,
            [CallerMemberName] string callerName = null)
        {
            using (RSA key = RSA.Create(TestData.RsaBigExponentParams))
            {
                CertificateRequest req = new CertificateRequest(
                    $"CN=\"{callerName}\"",
                    key,
                    HashAlgorithmName.SHA384,
                    RSASignaturePadding.Pkcs1);

                if (addSubjectKeyIdentifier)
                {
                    req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
                }

                foreach (X509Extension ext in extensions)
                {
                    req.CertificateExtensions.Add(ext);
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = req.CreateSelfSigned(now.AddMonths(-1), now.AddMonths(1)))
                {
                    action(cert, now);
                }
            }
        }

        private static void VerifyCrlFields(
            ReadOnlyMemory<byte> crl,
            X500DistinguishedName crlIssuerName,
            DateTimeOffset? thisUpdate,
            DateTimeOffset nextUpdate,
            X509AuthorityKeyIdentifierExtension expectedAkid,
            BigInteger expectedCrlNumber)
        {
            AsnReader reader = new AsnReader(crl, AsnEncodingRules.DER);
            reader = reader.ReadSequence();
            AsnReader tbs = reader.ReadSequence();
            // signatureAlgorithm
            reader.ReadEncodedValue();
            // signature
            reader.ReadEncodedValue();
            reader.ThrowIfNotEmpty();

            reader = tbs;
            // CRL Version
            Assert.Equal(BigInteger.One, reader.ReadInteger());
            // signature algorithm identifier
            reader.ReadEncodedValue();

            AssertExtensions.SequenceEqual(crlIssuerName.RawData, reader.ReadEncodedValue().Span);

            if (thisUpdate.HasValue)
            {
                Assert.Equal(ToTheSecond(thisUpdate.Value), ReadX509Time(reader));
                Assert.Equal(ToTheSecond(nextUpdate), ReadX509Time(reader));
            }
            else
            {
                // The thisUpdate value was implicit DateTimeOffset.UtcNow,
                // but it'll still be less than nextUpdate.
                DateTimeOffset thisUpd = ReadX509Time(reader);
                Assert.Equal(ToTheSecond(nextUpdate), ReadX509Time(reader));
                AssertExtensions.LessThan(thisUpd, nextUpdate);
            }

            AsnReader wrap = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            reader.ThrowIfNotEmpty();
            AsnReader extensions = wrap.ReadSequence();
            wrap.ThrowIfNotEmpty();

            AsnReader akidExt = extensions.ReadSequence();
            Assert.Equal("2.5.29.35", akidExt.ReadObjectIdentifier());

            if (expectedAkid.Critical)
            {
                Assert.True(akidExt.ReadBoolean(), "AKID.Critical");
            }

            byte[] akidBytes = akidExt.ReadOctetString();
            akidExt.ThrowIfNotEmpty();
            Assert.Equal(expectedAkid.RawData, akidBytes);

            AsnReader crlNumberExt = extensions.ReadSequence();
            Assert.Equal("2.5.29.20", crlNumberExt.ReadObjectIdentifier());
            byte[] crlNumberBytes = crlNumberExt.ReadOctetString();
            crlNumberExt.ThrowIfNotEmpty();
            reader = new AsnReader(crlNumberBytes, AsnEncodingRules.DER);
            Assert.Equal(expectedCrlNumber, reader.ReadInteger());
            reader.ThrowIfNotEmpty();

            extensions.ThrowIfNotEmpty();

            static DateTimeOffset ToTheSecond(DateTimeOffset input)
            {
                long totalTicks = input.Ticks;
                long unwantedTicks = totalTicks % TimeSpan.TicksPerSecond;
                long truncatedTicks = totalTicks - unwantedTicks;
                return new DateTimeOffset(truncatedTicks, input.Offset);
            }
        }

        private static DateTimeOffset ReadX509Time(AsnReader reader)
        {
            if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.UtcTime))
            {
                return reader.ReadUtcTime();
            }

            return reader.ReadGeneralizedTime();
        }

        private static ReadOnlySpan<byte> BuildEmptyExpectedCrl => new byte[]
        {
            0x30, 0x82, 0x01, 0x8E, 0x30, 0x78, 0x02, 0x01, 0x01, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86,
            0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x30, 0x15, 0x31, 0x13, 0x30, 0x11, 0x06, 0x03, 0x55, 0x04,
            0x03, 0x13, 0x0A, 0x42, 0x75, 0x69, 0x6C, 0x64, 0x45, 0x6D, 0x70, 0x74, 0x79, 0x17, 0x0D, 0x31, 0x33,
            0x30, 0x34, 0x30, 0x36, 0x30, 0x37, 0x35, 0x38, 0x30, 0x39, 0x5A, 0x17, 0x0D, 0x31, 0x33, 0x30, 0x34,
            0x30, 0x36, 0x30, 0x38, 0x30, 0x33, 0x30, 0x39, 0x5A, 0xA0, 0x2F, 0x30, 0x2D, 0x30, 0x1F, 0x06, 0x03,
            0x55, 0x1D, 0x23, 0x04, 0x18, 0x30, 0x16, 0x80, 0x14, 0x78, 0xA5, 0xC7, 0x5D, 0x51, 0x66, 0x73, 0x31,
            0xD5, 0xA9, 0x69, 0x24, 0x11, 0x4C, 0x9B, 0x5F, 0xA0, 0x0D, 0x7B, 0xCB, 0x30, 0x0A, 0x06, 0x03, 0x55,
            0x1D, 0x14, 0x04, 0x03, 0x02, 0x01, 0x7B, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D,
            0x01, 0x01, 0x0B, 0x05, 0x00, 0x03, 0x82, 0x01, 0x01, 0x00, 0x05, 0xA2, 0x49, 0x67, 0x1E, 0x2E, 0xE8,
            0x78, 0x0C, 0xBE, 0x17, 0x18, 0x04, 0x93, 0x09, 0x4A, 0x5E, 0xB5, 0x76, 0x46, 0x5A, 0x9B, 0x26, 0x74,
            0x66, 0x6B, 0x76, 0x21, 0x84, 0xAD, 0x99, 0x28, 0x32, 0x55, 0x6B, 0x36, 0xCC, 0x83, 0x20, 0x26, 0x4F,
            0xE4, 0x5A, 0x6B, 0x49, 0x81, 0x43, 0x9E, 0xD9, 0xCF, 0xB8, 0x7E, 0xAD, 0x10, 0xD4, 0xA9, 0x57, 0x69,
            0x71, 0x3A, 0x04, 0x42, 0xB2, 0xD3, 0xA5, 0xFD, 0x20, 0x48, 0x7D, 0xA5, 0xB3, 0x3B, 0xCF, 0xBE, 0x10,
            0xED, 0x92, 0x1C, 0x8B, 0x98, 0x96, 0xB6, 0x9E, 0xA4, 0x43, 0xD8, 0xD9, 0xF0, 0xAF, 0x5E, 0x0E, 0xB7,
            0x89, 0x36, 0x16, 0x55, 0xC8, 0x0E, 0xC3, 0xC7, 0xC7, 0xC8, 0x4F, 0x51, 0x27, 0xC6, 0xA2, 0x9C, 0x27,
            0xBE, 0x84, 0x37, 0xCE, 0x01, 0x82, 0xBD, 0x16, 0xCF, 0x69, 0x71, 0x69, 0x12, 0x1C, 0x2B, 0xBF, 0xAA,
            0xDC, 0x4E, 0xDE, 0x17, 0xC8, 0xBB, 0x76, 0x94, 0x9D, 0x25, 0x37, 0x6F, 0x27, 0x39, 0xE0, 0x3C, 0xDA,
            0x06, 0x09, 0xD0, 0x3C, 0x02, 0x4C, 0xD5, 0xA9, 0x11, 0xB3, 0x42, 0x57, 0x1F, 0x38, 0x5B, 0x3B, 0x8A,
            0x78, 0x2B, 0x62, 0xC5, 0x37, 0x5E, 0x1D, 0x67, 0x4E, 0x43, 0x44, 0x7F, 0xE2, 0xEB, 0x9E, 0xFF, 0xCA,
            0xF7, 0x1C, 0xCC, 0xEC, 0xBA, 0xE6, 0x00, 0xC7, 0x4F, 0x6F, 0xD6, 0xCB, 0x36, 0xA8, 0x7C, 0x57, 0x86,
            0x60, 0x35, 0x01, 0xEA, 0x43, 0x79, 0x41, 0x44, 0x14, 0x2E, 0x85, 0x57, 0xEC, 0x2E, 0xBC, 0x2F, 0x73,
            0x57, 0xDB, 0x05, 0x04, 0x40, 0xFD, 0x97, 0xF2, 0x33, 0x44, 0x1E, 0x2B, 0xE9, 0x81, 0xED, 0x63, 0x09,
            0xCE, 0x7C, 0x8B, 0x1C, 0x97, 0xBC, 0xE6, 0x58, 0xFC, 0xEC, 0x6B, 0xD6, 0x30, 0x04, 0xA1, 0xD3, 0xD4,
            0xEA, 0x00, 0x43, 0x78, 0x3E, 0x55, 0xE7, 0xEC, 0xBC, 0xF6, 0xE6,
        };

        // See AddEntryFromCertificate for the characteristics of this CRL.
        private static ReadOnlySpan<byte> AddEntryFromCertificateExpectedCrl => new byte[]
        {
            0x30, 0x82, 0x01, 0xBA, 0x30, 0x81, 0xA3, 0x02, 0x01, 0x01, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48,
            0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0D, 0x05, 0x00, 0x30, 0x22, 0x31, 0x20, 0x30, 0x1E, 0x06, 0x03, 0x55,
            0x04, 0x03, 0x13, 0x17, 0x41, 0x64, 0x64, 0x45, 0x6E, 0x74, 0x72, 0x79, 0x46, 0x72, 0x6F, 0x6D, 0x43,
            0x65, 0x72, 0x74, 0x69, 0x66, 0x69, 0x63, 0x61, 0x74, 0x65, 0x17, 0x0D, 0x31, 0x36, 0x30, 0x33, 0x30,
            0x38, 0x30, 0x34, 0x35, 0x39, 0x30, 0x37, 0x5A, 0x17, 0x0D, 0x31, 0x36, 0x30, 0x33, 0x30, 0x38, 0x30,
            0x35, 0x30, 0x34, 0x30, 0x37, 0x5A, 0x30, 0x1C, 0x30, 0x1A, 0x02, 0x09, 0x00, 0xAB, 0x74, 0x0A, 0x71,
            0x4A, 0xA8, 0x3C, 0x92, 0x17, 0x0D, 0x31, 0x36, 0x30, 0x33, 0x30, 0x38, 0x30, 0x34, 0x34, 0x31, 0x32,
            0x31, 0x5A, 0xA0, 0x2F, 0x30, 0x2D, 0x30, 0x1F, 0x06, 0x03, 0x55, 0x1D, 0x23, 0x04, 0x18, 0x30, 0x16,
            0x80, 0x14, 0x78, 0xA5, 0xC7, 0x5D, 0x51, 0x66, 0x73, 0x31, 0xD5, 0xA9, 0x69, 0x24, 0x11, 0x4C, 0x9B,
            0x5F, 0xA0, 0x0D, 0x7B, 0xCB, 0x30, 0x0A, 0x06, 0x03, 0x55, 0x1D, 0x14, 0x04, 0x03, 0x02, 0x01, 0x7A,
            0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0D, 0x05, 0x00, 0x03, 0x82,
            0x01, 0x01, 0x00, 0xA1, 0xD2, 0xEC, 0xA5, 0x52, 0x8F, 0x04, 0x34, 0x4D, 0x05, 0x0E, 0xCD, 0x2E, 0xF8,
            0xF1, 0x83, 0x0C, 0xC5, 0x48, 0x78, 0xC5, 0x97, 0xE1, 0x42, 0x5A, 0xDB, 0x87, 0x19, 0x7D, 0x33, 0xBE,
            0x2B, 0x96, 0xFF, 0xAF, 0x25, 0xA8, 0x6E, 0xDA, 0x19, 0x71, 0x1C, 0xA7, 0x04, 0x36, 0xF6, 0x0D, 0xF1,
            0x73, 0x71, 0xA0, 0xF6, 0xFA, 0x81, 0xE4, 0xF2, 0xDC, 0x1E, 0xB7, 0x0D, 0x96, 0x78, 0x8B, 0x9F, 0xE0,
            0x2B, 0xDC, 0xD1, 0xD8, 0x25, 0xB4, 0xF9, 0x92, 0xA8, 0x84, 0x4A, 0x9E, 0x68, 0x2A, 0x92, 0x32, 0x10,
            0x73, 0x10, 0x60, 0x9A, 0x9C, 0xF6, 0xCA, 0xE7, 0x14, 0x00, 0x66, 0x20, 0x5C, 0xE8, 0xB1, 0x77, 0xE1,
            0x74, 0x53, 0x6B, 0x50, 0x48, 0xED, 0x64, 0xDE, 0xDC, 0x9F, 0x1A, 0x85, 0x2C, 0x48, 0xB5, 0x82, 0xFA,
            0x10, 0xAE, 0xC9, 0x48, 0xEE, 0xDA, 0x7A, 0x48, 0xA8, 0x8E, 0xEF, 0x3E, 0x31, 0x3E, 0x6C, 0x61, 0xC1,
            0x0A, 0x19, 0x5B, 0xD6, 0xB6, 0xF1, 0x37, 0xF8, 0x81, 0xA7, 0x2D, 0x7D, 0x93, 0x9B, 0xD6, 0x43, 0x46,
            0xBC, 0x60, 0x9B, 0xD0, 0xFB, 0xF2, 0xF6, 0xC5, 0x09, 0x60, 0x63, 0x36, 0x16, 0xEB, 0xCC, 0xCD, 0x35,
            0xEB, 0x6F, 0xD4, 0x00, 0xAF, 0xD9, 0xD2, 0xFE, 0x5B, 0x19, 0x0A, 0x22, 0x28, 0x17, 0xDA, 0x0C, 0xB7,
            0xFD, 0xEB, 0x99, 0x8B, 0x76, 0xDD, 0x63, 0x34, 0xC4, 0x0A, 0x61, 0x5A, 0xE0, 0xB6, 0x7E, 0x9D, 0x3C,
            0xD0, 0x4E, 0xAB, 0x68, 0xD8, 0x1B, 0x2E, 0x95, 0x43, 0xFD, 0x5E, 0x58, 0x04, 0x29, 0x78, 0x7C, 0x39,
            0xC7, 0x21, 0xE5, 0xE0, 0x7D, 0x28, 0xCE, 0xAF, 0x32, 0x1A, 0x90, 0x72, 0x94, 0x77, 0xED, 0xAF, 0xCD,
            0x22, 0x3A, 0xC8, 0x2A, 0x68, 0xAC, 0xB1, 0x08, 0x26, 0x97, 0xA2, 0xD0, 0xC0, 0x98, 0x56, 0x31, 0x7D,
            0x5C, 0x3E, 0xD3, 0x09,
        };

        private static ReadOnlySpan<byte> BuildSingleEntryExpectedCrl => new byte[]
        {
            0x30, 0x82, 0x01, 0xB2, 0x30, 0x81, 0x9B, 0x02, 0x01, 0x01, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48,
            0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x30, 0x1B, 0x31, 0x19, 0x30, 0x17, 0x06, 0x03, 0x55,
            0x04, 0x03, 0x13, 0x10, 0x42, 0x75, 0x69, 0x6C, 0x64, 0x53, 0x69, 0x6E, 0x67, 0x6C, 0x65, 0x45, 0x6E,
            0x74, 0x72, 0x79, 0x17, 0x0D, 0x31, 0x33, 0x30, 0x34, 0x30, 0x36, 0x30, 0x37, 0x35, 0x38, 0x30, 0x39,
            0x5A, 0x17, 0x0D, 0x31, 0x33, 0x30, 0x34, 0x30, 0x36, 0x30, 0x38, 0x30, 0x33, 0x30, 0x39, 0x5A, 0x30,
            0x1B, 0x30, 0x19, 0x02, 0x08, 0x01, 0x01, 0x02, 0x03, 0x05, 0x08, 0x0C, 0x15, 0x17, 0x0D, 0x31, 0x33,
            0x30, 0x34, 0x30, 0x36, 0x30, 0x37, 0x32, 0x37, 0x35, 0x37, 0x5A, 0xA0, 0x2F, 0x30, 0x2D, 0x30, 0x1F,
            0x06, 0x03, 0x55, 0x1D, 0x23, 0x04, 0x18, 0x30, 0x16, 0x80, 0x14, 0x78, 0xA5, 0xC7, 0x5D, 0x51, 0x66,
            0x73, 0x31, 0xD5, 0xA9, 0x69, 0x24, 0x11, 0x4C, 0x9B, 0x5F, 0xA0, 0x0D, 0x7B, 0xCB, 0x30, 0x0A, 0x06,
            0x03, 0x55, 0x1D, 0x14, 0x04, 0x03, 0x02, 0x01, 0x7B, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86,
            0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x03, 0x82, 0x01, 0x01, 0x00, 0xA9, 0xE1, 0xD0, 0x35, 0x71,
            0xB1, 0xE4, 0xBF, 0x76, 0x70, 0xEC, 0x32, 0x45, 0x9A, 0x74, 0xB1, 0x14, 0x82, 0x74, 0x1F, 0xD9, 0x73,
            0xFF, 0x50, 0x40, 0xD5, 0x7B, 0x13, 0x3B, 0x5B, 0x6C, 0x78, 0x3D, 0xC9, 0xED, 0x10, 0x5C, 0x4C, 0xF5,
            0xDD, 0xE8, 0xFC, 0x8B, 0x76, 0x7C, 0x60, 0x34, 0x25, 0x3D, 0x74, 0x9A, 0x83, 0x46, 0x22, 0x03, 0x4A,
            0x66, 0x9A, 0xA4, 0xC6, 0xEF, 0xDB, 0x93, 0xC8, 0x2E, 0xB1, 0x5B, 0x69, 0xE6, 0xDC, 0x43, 0xF0, 0x5B,
            0xAE, 0x7E, 0x9E, 0x21, 0xB0, 0x35, 0x1A, 0x72, 0x0C, 0x5E, 0x79, 0xF3, 0xBE, 0x65, 0x30, 0x46, 0x58,
            0xEB, 0xDF, 0xE1, 0x96, 0x26, 0x9B, 0xC2, 0x85, 0xD6, 0x53, 0xE7, 0xAC, 0xD9, 0x78, 0x11, 0xD6, 0x4E,
            0x08, 0x79, 0x20, 0x34, 0xB4, 0x7D, 0x83, 0xBF, 0x9D, 0x37, 0x85, 0x11, 0x16, 0x02, 0x3B, 0xDF, 0x74,
            0x60, 0xC5, 0xBF, 0x14, 0x92, 0xCF, 0xA4, 0x86, 0xAD, 0x7B, 0x2F, 0x27, 0x78, 0x70, 0x82, 0xE6, 0xA3,
            0xC0, 0x5C, 0x0E, 0x43, 0xBB, 0x7D, 0x62, 0xB2, 0x34, 0xC0, 0xE6, 0xC5, 0xBA, 0x2E, 0x01, 0x03, 0xE1,
            0xCC, 0xBD, 0xAE, 0x15, 0xF9, 0xCD, 0x6D, 0xB9, 0x89, 0xDE, 0xD6, 0x87, 0x09, 0x15, 0xAB, 0x16, 0x4E,
            0xB2, 0xFC, 0x2A, 0xDA, 0x00, 0xD4, 0x98, 0x05, 0x74, 0xFC, 0x2C, 0x3C, 0x09, 0x05, 0xC1, 0xBF, 0xC9,
            0xF4, 0x2D, 0xBF, 0x0F, 0x80, 0x0F, 0xF7, 0xF9, 0xD9, 0x2C, 0x1F, 0x99, 0xC4, 0x43, 0xEF, 0xC3, 0x25,
            0x93, 0xC7, 0x49, 0xE1, 0x8C, 0x41, 0x28, 0x2E, 0x0E, 0xF2, 0x32, 0x64, 0x38, 0x46, 0xD2, 0x04, 0xA6,
            0xBC, 0x23, 0xC5, 0x56, 0x05, 0x29, 0x92, 0x25, 0x63, 0x23, 0xF7, 0xBD, 0x75, 0xDE, 0xE7, 0x33, 0xC9,
            0xFD, 0x01, 0x1B, 0x6D, 0x3B, 0x85, 0x39, 0x54, 0x22, 0x04, 0x6B, 0x55, 0x73,
        };
    }
}
