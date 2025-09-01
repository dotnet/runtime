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
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support asymmetric cryptography")]
    public static class CrlBuilderTests
    {
        private const string CertParam = "issuerCertificate";

        public enum CertKind
        {
            ECDsa,
            MLDsa,
            RsaPkcs1,
            RsaPss,
            RsaPssWithCustomSaltLength,
            RsaPssWithMaxSaltLength,
            RsaPssWithZeroSaltLength,
            SlhDsa,
        }

        public static IEnumerable<object[]> SupportedCertKinds()
        {
            yield return new object[] { CertKind.ECDsa };

            if (MLDsa.IsSupported)
            {
                yield return new object[] { CertKind.MLDsa };
            }

            yield return new object[] { CertKind.RsaPkcs1 };
            yield return new object[] { CertKind.RsaPss };
            yield return new object[] { CertKind.RsaPssWithCustomSaltLength };
            yield return new object[] { CertKind.RsaPssWithMaxSaltLength };
            yield return new object[] { CertKind.RsaPssWithZeroSaltLength };

            if (SlhDsa.IsSupported)
            {
                yield return new object[] { CertKind.SlhDsa };
            }
        }

        public static IEnumerable<object[]> NoHashAlgorithmCertKinds()
        {
            if (MLDsa.IsSupported)
            {
                yield return new object[] { CertKind.MLDsa };
            }

            if (SlhDsa.IsSupported)
            {
                yield return new object[] { CertKind.SlhDsa };
            }
        }

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

        [Theory]
        [MemberData(nameof(SupportedCertKinds))]
        public static void BuildWithNoHashAlgorithm(CertKind certKind)
        {
            BuildCertificateAndRun(
                certKind,
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (certKind, cert, now) =>
                {
                    HashAlgorithmName hashAlg = default;
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    Action certBuild = () => builder.Build(cert, 0, now.AddMinutes(5), hashAlg, null, now);

                    if (RequiresHashAlgorithm(certKind))
                    {
                        Assert.Throws<ArgumentNullException>("hashAlgorithm", certBuild);
                    }
                    else
                    {
                        // Assert.NoThrow
                        certBuild();
                    }

                    X509SignatureGenerator gen = GetSignatureGenerator(certKind, cert, out IDisposable key);

                    using (key)
                    {
                        X500DistinguishedName dn = cert.SubjectName;
                        X509AuthorityKeyIdentifierExtension akid =
                            X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, true, false);

                        Action genBuild = () => builder.Build(dn, gen, 0, now.AddMinutes(5), hashAlg, akid, now);

                        if (RequiresHashAlgorithm(certKind))
                        {
                            Assert.Throws<ArgumentNullException>("hashAlgorithm", genBuild);
                        }
                        else
                        {
                            // Assert.NoThrow
                            genBuild();
                        }
                    }
                });
        }

        [Theory]
        [MemberData(nameof(SupportedCertKinds))]
        public static void BuildWithEmptyHashAlgorithm(CertKind certKind)
        {
            BuildCertificateAndRun(
                certKind,
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (certKind, cert, now) =>
                {
                    HashAlgorithmName hashAlg = new HashAlgorithmName("");
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    Action certAction = () => builder.Build(cert, 0, now.AddMinutes(5), hashAlg, null, now);

                    if (RequiresHashAlgorithm(certKind))
                    {
                        Exception e = AssertExtensions.Throws<ArgumentException>("hashAlgorithm", certAction);

                        Assert.Contains("empty", e.Message);
                    }
                    else
                    {
                        // Assert.NoThrow
                        certAction();
                    }

                    X509SignatureGenerator gen = GetSignatureGenerator(certKind, cert, out IDisposable key);

                    using (key)
                    {
                        X500DistinguishedName dn = cert.SubjectName;
                        X509AuthorityKeyIdentifierExtension akid =
                            X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, true, false);

                        Action genAction = () => builder.Build(dn, gen, 0, now.AddMinutes(5), hashAlg, akid, now);

                        if (RequiresHashAlgorithm(certKind))
                        {
                            Assert.Throws<ArgumentException>("hashAlgorithm", genAction);
                        }
                        else
                        {
                            // Assert.NoThrow
                            genAction();
                        }
                    }
                });
        }

        [Theory]
        [MemberData(nameof(NoHashAlgorithmCertKinds))]
        public static void BuildPqcWithHashAlgorithm(CertKind certKind)
        {
            BuildCertificateAndRun(
                certKind,
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                static (certKind, cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    // Assert.NoThrow
                    builder.Build(cert, 0, now.AddMinutes(5), HashAlgorithmName.SHA256);

                    X509SignatureGenerator gen = GetSignatureGenerator(certKind, cert, out IDisposable key);

                    using (key)
                    {
                        X500DistinguishedName dn = cert.SubjectName;
                        X509AuthorityKeyIdentifierExtension akid =
                            X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, true, false);

                        // Assert.NoThrow
                        builder.Build(dn, gen, 0, now.AddMinutes(5), HashAlgorithmName.SHA256, akid);
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
        public static void BuildEmptyRsaPkcs1()
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
                },
                callerName: "BuildEmpty");
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

        [Theory]
        [MemberData(nameof(SupportedCertKinds))]
        public static void BuildEmpty(CertKind certKind)
        {
            BuildCertificateAndRun(
                certKind,
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                (certKind, cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                    DateTimeOffset nextUpdate = now.AddHours(1);
                    HashAlgorithmName hashAlg = RequiresHashAlgorithm(certKind) ? HashAlgorithmName.SHA256 : default;

                    byte[] crl = builder.Build(cert, 2, nextUpdate, hashAlg, GetRsaPadding(certKind));

                    AsnReader reader = new AsnReader(crl, AsnEncodingRules.DER);
                    reader = reader.ReadSequence();
                    ReadOnlyMemory<byte> tbs = reader.ReadEncodedValue();
                    // signatureAlgorithm
                    reader.ReadEncodedValue();
                    byte[] signature = reader.ReadBitString(out _);
                    reader.ThrowIfNotEmpty();

                    VerifySignature(certKind, cert, tbs.Span, signature, hashAlg);

                    VerifyCrlFields(
                        crl,
                        cert.SubjectName,
                        thisUpdate: null,
                        nextUpdate,
                        X509AuthorityKeyIdentifierExtension.CreateFromCertificate(cert, true, false),
                        2);
                });
        }

        [Theory]
        [MemberData(nameof(SupportedCertKinds))]
        public static void BuildEmpty_NoSubjectKeyIdentifier(CertKind certKind)
        {
            BuildCertificateAndRun(
                certKind,
                new X509Extension[]
                {
                    X509BasicConstraintsExtension.CreateForCertificateAuthority(),
                },
                (certKind, cert, now) =>
                {
                    CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();
                    DateTimeOffset nextUpdate = now.AddHours(1);
                    DateTimeOffset thisUpdate = now;
                    HashAlgorithmName hashAlg = RequiresHashAlgorithm(certKind) ? HashAlgorithmName.SHA256 : default;

                    byte[] crl = builder.Build(
                        cert,
                        2,
                        nextUpdate,
                        hashAlg,
                        GetRsaPadding(certKind),
                        thisUpdate);

                    AsnReader reader = new AsnReader(crl, AsnEncodingRules.DER);
                    reader = reader.ReadSequence();
                    ReadOnlyMemory<byte> tbs = reader.ReadEncodedValue();
                    // signatureAlgorithm
                    reader.ReadEncodedValue();
                    byte[] signature = reader.ReadBitString(out _);
                    reader.ThrowIfNotEmpty();

                    VerifySignature(certKind, cert, tbs.Span, signature, hashAlg);

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
                        "308201CA3081B3020101300D06092A864886F70D01010B05003025312330210603550403131A427" +
                        "5696C6453696E676C65456E74727957697468526561736F6E170D3133303430363037353830395A" +
                        "170D3133303430363038303330395A3029302702080101020305080C15170D31333034303630373" +
                        "23735375A300C300A0603551D1504030A0101A02F302D301F0603551D230418301680144498BCC0" +
                        "CAA53DF3BC936988508E72EA5D7BA9FE300A0603551D14040302017B300D06092A864886F70D010" +
                        "10B05000382010100A87085F14CB17262DB4DF19F4E2F4577B692287F6FA8DD2A63761EBE045058" +
                        "4FF47C462ADEC002921B55CF89114438698AF7B611AB5E6FE30357DBD60F5ED2538FDBDE11A12B3" +
                        "C3C79F267C6F7AFC5A9048E5B6CEA9A191A52CF2AE1641EE2E4A5A5FB89254B5809575E03C3EEBE" +
                        "6018F4DB416F1264BFC84452034A097F3F600F22BB666B7F6C77ABA71ECCEF02529155B84441AF4" +
                        "AE17AEEC8765AF2AEEC50D6EF6CFC1E5B0C5188ADD9442E034819734A80A6607FBF4D8A31C49688" +
                        "E909A053279C5A9B0228E6630D46F0C608C929C706CBBD0B208C2C434E4292084D88D1ECA440C3E" +
                        "B7F5B36A60B79FCA2059E4BFA79385F5A88B56669F7F9238EEFA22C").HexToByteArray();

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
                        "308201B430819D020101300D06092A864886F70D01010B0500301D311B301906035504031312416" +
                        "464547769636552656D6F76654F6E6365170D3133303430363037353830395A170D313330343036" +
                        "3038303330395A301B301902080101020305080C15170D3133303430363037323735375AA02F302" +
                        "D301F0603551D230418301680144498BCC0CAA53DF3BC936988508E72EA5D7BA9FE300A0603551D" +
                        "14040302017B300D06092A864886F70D01010B05000382010100920A460578DE4F6675B96BB4E20" +
                        "E6379E8C0B6C306886FA1BB90D30C2F3BF1CFDB2BCD7A8AD398D933E939C7CDB3ABCDE00241E17A" +
                        "E46D137DB1F9BF64EEDC004E98A5987E74B7A3A090E0B0AC74F837DA12165CA9AD94BC0A07CEE26" +
                        "F247E9369AF3EE547AB67F19CBD387608236BED2D07E0716718A31780F1AEA1FBAB60324FDBDED1" +
                        "35F92BC208E33529A0C0680B06EBDCD8D55DD9DCD28B00A0F89A1C6B1A0D081AD009E0AC8D9A57E" +
                        "BFC62D2C428B6E22E65A0457D669C3527E816485152F5B2EACF182FEC081006050E0AF544D14FCE" +
                        "06DB5BA065E38553DC0F49C25EDEC9F8F1A9A5D59AAB2E2FD1CE4619D24B3C83AF9C7227EB55362" +
                        "401D94F14A3").HexToByteArray();

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
                        "308201A330818C020101300D06092A864886F70D01010B05003027312530230603550403131C546" +
                        "86973557064617465323034394E65787455706461746532303530170D3439313233313233353935" +
                        "395A180F32303530303130313030303435395AA02F302D301F0603551D230418301680144498BCC" +
                        "0CAA53DF3BC936988508E72EA5D7BA9FE300A0603551D14040302017B300D06092A864886F70D01" +
                        "010B05000382010100A38CB2CA867B087990B63F3C5B190BF627B7C90A75CB951EB691BECAF307F" +
                        "102A1B941744FEEBDA1B349A153F7D56C2AB48F0263D90CD615D52F4E913F4C17AD1C407545345A" +
                        "A4176920B1E1F5DFDA2E9F9F0065B12EA396C1EEFAFE29730F0D71EBB96D0FC77C00DAB4C18F18C" +
                        "408ACB3BC1468C7350D2B1F31BF3206215F5C38E40EB2AAE1116E6B35B4AD588AAF272A60C055F7" +
                        "F76B77B857E2B54591D607E539AC28A134F82A30D7ABDC5D5FA27FF3F39D88FFCDA259D97688CC0" +
                        "F28F1DC5E83DB9D58F35615A5C93E5506677BE4F710103BDB81F2CABFA5D81F8F9B1D5B78F2916C" +
                        "B1CBC0F38C31AE1D5B2BC6412F5F5C28C2DD87C8345E1EB84EC8484E").HexToByteArray();

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
                        "30820197308180020101300D06092A864886F70D01010B05003019311730150603550403130E546" +
                        "8697355706461746532303530180F32303530303130313030303030305A180F3230353030313031" +
                        "3030303530305AA02F302D301F0603551D230418301680144498BCC0CAA53DF3BC936988508E72E" +
                        "A5D7BA9FE300A0603551D14040302017B300D06092A864886F70D01010B05000382010100990FD5" +
                        "25C8EE57209F56F7050E8EB98E90C05BEF5C352DFD0C71C63BA41A79BDFEAAB295175997A733990" +
                        "DB888BBDDC7B12B2A1A9527EDE3DF3F2FE069A56BEC850599EE1B1FF4093C76293787A29A18BBC2" +
                        "7B9F6D3EE95DD67F2C32E64201D21840FA828BAC09757727B8766E77F89F7D4250CDDCEC78D300E" +
                        "20789830364ED68863D6B5A099FA427C2E92B706C0DC09E26D1124134B33495790D3D75271DE0D8" +
                        "8EBF151CDCF0C0BFA0DEB160B950CB03AE62ED9FEB5385456DBECD7D2215C62D1B2B18FA35C6548" +
                        "ACCC35782D108E550FA6F6A81F9CA750B65D84315B516B13F34771143331DCAF1194B7290B91E03" +
                        "ACFF1C89498706DE2B6BE05BCEFFB768").HexToByteArray();

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
                        "308201D23081BB020101300D06092A864886F70D01010C0500302C312A302806035504031321416" +
                        "464456E74727957697468526561736F6E46726F6D4365727469666963617465170D313330343036" +
                        "3037353830395A170D3133303430363038303330395A302A3028020900AB740A714AA83C92170D3" +
                        "133303430363037343032335A300C300A0603551D1504030A0109A02F302D301F0603551D230418" +
                        "301680144498BCC0CAA53DF3BC936988508E72EA5D7BA9FE300A0603551D14040302017B300D060" +
                        "92A864886F70D01010C0500038201010089CFB618655460C4C7FDC4A5CBBC8E84E0CE16603BC367" +
                        "0E4DCCFDCBBDD1022144C921F11147CE5D0252E92A81FD2BB41BA0A312DA55732C71EAFDF215E36" +
                        "EFA1F77A1586859D2ECCC7608598DEDB20A7B57275687327D4DC6E1E64C66D8C7B91CA77480EDDC" +
                        "91F870955994065AD6C97657630B7385CE0C147542FC4D58494B975B0C081972B7BE41A09FB08C7" +
                        "D12B36C62610F4814FB74911BE39B4855BDE1DE3067F8A18721CAA223021BB314C7A5D418AF3EF9" +
                        "8CFB4DBD3A627B0C4071BCBC3A57B16382BCD48CB6C7A3564D621D8D8D55314B3AC2E6EA2A6BE22" +
                        "BAE3FF476E5F1B5BC82CB6230200C8EE480FE365289EBC520DEDE28BB2B5459F2ADB365").HexToByteArray();

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
                        "308201F83081E1020101300D06092A864886F70D01010B0500301A311830160603550403130F4C6" +
                        "F6164416464416E644275696C64180F32303533303930383037353634315A180F32303533303930" +
                        "393037353634315A30503028020900AB740A714AA83C92170D3133303430363037343032335A300" +
                        "C300A0603551D1504030A0109302402030A0304180F32303533303930383037353633395A300C30" +
                        "0A0603551D1504030A0101A03D303B301F0603551D230418301680144498BCC0CAA53DF3BC93698" +
                        "8508E72EA5D7BA9FE30180603551D140411020F03F4407DDE4753F848A9D7F9A00001300D06092A" +
                        "864886F70D01010B05000382010100AFB81D5A3C422121D768D8FFBB74E536496FC01068B447B68" +
                        "B67BBF71B2ED666D216DC45620FB0038693EFD7C36695D08485E444E44E9AFD28AFE6EC02A084F7" +
                        "623C3CD0AA20799760887D2999A312B9C08ABF42BD9DFEA7B1B6A2B49FD02CBC1A25515B1820216" +
                        "A343C831C0174318E345600B8D82BDA21D26E366955DF1D1B0628BF25950670637EF5A110C61DD3" +
                        "92CF4AFA4D5D0C222867EC5EA47BBDA78B4D99657AB714142AEC0C0EA34F9BF34C8BFBDC1185A20" +
                        "922EBB7B524A605A8AB40B5C5A4DCB09383D3793CBAB3AE2522120CF3AC27E79AF312319BD58A35" +
                        "EF82E17425D1BA98E0AF62457C7D5935E6A823ED0BEF33E26E8EEE31E544F10A48BF").HexToByteArray();

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
                        "308202273082010F020101300D06092A864886F70D01010B05003029312730250603550403131E4" +
                        "C6F6164507265736572766573556E6B6E6F776E457874656E73696F6E73180F3139343930393038" +
                        "3037353634315A170D3530303930383037353634315A3071301A020900AB740A714AA83C92170D3" +
                        "133303430363037343032335A303A02030A0304180F32303533303930383037353633395A302230" +
                        "0A0603551D1504030A0101301406092A864886F70D01010B0407300504000201033017020415845" +
                        "71B180F31393439303930383037353633395AA03D303B301F0603551D230418301680144498BCC0" +
                        "CAA53DF3BC936988508E72EA5D7BA9FE30180603551D140411020F03F4407DDE4753F848A9D7F9A" +
                        "0000D300D06092A864886F70D01010B05000382010100B923EB74EB2A888EF645FE903D2A4A0E1A" +
                        "4AC634A27AF5CAACC342F2498435F6C7B7C7811D3747AA6AD41202CAA067DBD262E0E9727BBA68E" +
                        "B3F203921E842E78D6A8718BA3D32AEE1C54E5652961DE301F86D36B35533CDDCABAABBE60EA39F" +
                        "113DFC6AF6B6FD7D31D00C7D75A727791EC4645724DCC4E1F5D30A20B618EE557C2F2B6FC64CDD9" +
                        "54B158CD75CE4C02F6C1482F53DBDF11B7C1FAC782A533FFCC9E578E644877EB8584A276AB0F927" +
                        "D3B4D33C97A4BA7CBB2E8C9825A6762B7783DF532884DB61A4F77D5319C1D5992D628A9B85ACCE8" +
                        "D27F1EFF1DC975A31F8492EC07DE4B45B08076ACDC3B5E7671E9C7CCA1F0DC9E0F04FE2898BF52A" +
                        "C24F").HexToByteArray();

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
            CertKind certKind,
            IEnumerable<X509Extension> extensions,
            Action<CertKind, X509Certificate2, DateTimeOffset> action,
            bool addSubjectKeyIdentifier = true,
            [CallerMemberName] string callerName = null)
        {
            string subjectName = $"CN=\"{callerName}\"";
            CertificateRequest req;
            IDisposable key = null;

            try
            {
                if (certKind == CertKind.ECDsa)
                {
                    ECDsa ecdsa = ECDsa.Create();
                    key = ecdsa;
                    req = new CertificateRequest(subjectName, ecdsa, HashAlgorithmName.SHA384);
                }
                else if (certKind == CertKind.RsaPkcs1 || certKind == CertKind.RsaPss)
                {
                    RSA rsa = RSA.Create();
                    rsa.ImportFromPem(TestData.RsaPkcs8Key);
                    key = rsa;
                    req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA384, GetRsaPadding(certKind));
                }
                else if (certKind == CertKind.RsaPssWithMaxSaltLength)
                {
                    var rsa = RSA.Create();
                    key = rsa;
                    req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA384, RSASignaturePadding.CreatePss(RSASignaturePadding.PssSaltLengthMax));
                }
                else if (certKind == CertKind.RsaPssWithCustomSaltLength)
                {
                    var rsa = RSA.Create();
                    key = rsa;
                    req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA384, RSASignaturePadding.CreatePss(200));
                }
                else if (certKind == CertKind.RsaPssWithZeroSaltLength)
                {
                    var rsa = RSA.Create();
                    key = rsa;
                    req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA384, RSASignaturePadding.CreatePss(0));
                }
                else if (certKind == CertKind.MLDsa)
                {
                    MLDsa mldsa = MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa44);
                    key = mldsa;
                    req = new CertificateRequest(subjectName, mldsa);
                }
                else if (certKind == CertKind.SlhDsa)
                {
                    SlhDsa slhDsa = SlhDsa.GenerateKey(SlhDsaAlgorithm.SlhDsaSha2_128f);
                    key = slhDsa;
                    req = new CertificateRequest(subjectName, slhDsa);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported CertKind: {certKind}");
                }

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
                    action(certKind, cert, now);
                }
            }
            finally
            {
                key?.Dispose();
            }
        }

        private static void BuildCertificateAndRun(
            IEnumerable<X509Extension> extensions,
            Action<X509Certificate2, DateTimeOffset> action,
            bool addSubjectKeyIdentifier = true,
            [CallerMemberName] string callerName = null)
        {
            BuildCertificateAndRun(
                CertKind.ECDsa,
                extensions,
                (certKind, cert, now) => action(cert, now),
                addSubjectKeyIdentifier,
                callerName);
        }

        private static void BuildRsaCertificateAndRun(
            IEnumerable<X509Extension> extensions,
            Action<X509Certificate2, DateTimeOffset> action,
            bool addSubjectKeyIdentifier = true,
            [CallerMemberName] string callerName = null)
        {
            BuildCertificateAndRun(
                CertKind.RsaPkcs1,
                extensions,
                (certKind, cert, now) => action(cert, now),
                addSubjectKeyIdentifier,
                callerName);
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

        private static X509SignatureGenerator GetSignatureGenerator(
            CertKind certKind,
            X509Certificate2 cert,
            out IDisposable key)
        {
            if (certKind == CertKind.RsaPkcs1 || certKind == CertKind.RsaPss || certKind == CertKind.RsaPssWithZeroSaltLength || certKind == CertKind.RsaPssWithCustomSaltLength || certKind == CertKind.RsaPssWithMaxSaltLength)
            {
                RSA rsa = cert.GetRSAPrivateKey();
                key = rsa;
                return X509SignatureGenerator.CreateForRSA(rsa, GetRsaPadding(certKind));
            }
            else if (certKind == CertKind.ECDsa)
            {
                ECDsa ecdsa = cert.GetECDsaPrivateKey();
                key = ecdsa;
                return X509SignatureGenerator.CreateForECDsa(ecdsa);
            }
            else if (certKind == CertKind.MLDsa)
            {
                MLDsa mldsa = cert.GetMLDsaPrivateKey();
                key = mldsa;
                return X509SignatureGenerator.CreateForMLDsa(mldsa);
            }
            else if (certKind == CertKind.SlhDsa)
            {
                SlhDsa slhDsa = cert.GetSlhDsaPrivateKey();
                key = slhDsa;
                return X509SignatureGenerator.CreateForSlhDsa(slhDsa);
            }
            else
            {
                throw new NotSupportedException($"Unsupported CertKind: {certKind}");
            }
        }

        private static void VerifySignature(
            CertKind certKind,
            X509Certificate2 cert,
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm)
        {
            bool signatureValid;

            if (certKind == CertKind.RsaPkcs1 || certKind == CertKind.RsaPss || certKind == CertKind.RsaPssWithZeroSaltLength || certKind == CertKind.RsaPssWithCustomSaltLength || certKind == CertKind.RsaPssWithMaxSaltLength)
            {
                using RSA rsa = cert.GetRSAPublicKey();
                signatureValid = rsa.VerifyData(data, signature, hashAlgorithm, GetRsaPadding(certKind));
            }
            else if (certKind == CertKind.ECDsa)
            {
                using ECDsa ecdsa = cert.GetECDsaPublicKey();
                signatureValid = ecdsa.VerifyData(data, signature, hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
            }
            else if (certKind == CertKind.MLDsa)
            {
                using MLDsa mldsa = cert.GetMLDsaPublicKey();
                signatureValid = mldsa.VerifyData(data, signature);
            }
            else if (certKind == CertKind.SlhDsa)
            {
                using SlhDsa slhDsa = cert.GetSlhDsaPublicKey();
                signatureValid = slhDsa.VerifyData(data, signature);
            }
            else
            {
                throw new NotSupportedException($"Unsupported CertKind: {certKind}");
            }

            if (!signatureValid)
            {
                Assert.Fail($"{certKind} signature validation failed when it should have succeeded.");
            }
        }

        private static bool RequiresHashAlgorithm(CertKind certKind)
        {
            return certKind switch
            {
                CertKind.ECDsa or CertKind.RsaPkcs1 or CertKind.RsaPss or CertKind.RsaPssWithCustomSaltLength or CertKind.RsaPssWithMaxSaltLength or CertKind.RsaPssWithZeroSaltLength => true,
                CertKind.MLDsa or CertKind.SlhDsa => false,
                _ => throw new NotSupportedException(certKind.ToString())
            };
        }

        private static RSASignaturePadding GetRsaPadding(CertKind certKind)
        {
            return certKind switch
            {
                CertKind.RsaPkcs1 => RSASignaturePadding.Pkcs1,
                CertKind.RsaPss => RSASignaturePadding.Pss,
                CertKind.RsaPssWithCustomSaltLength => RSASignaturePadding.CreatePss(200),
                CertKind.RsaPssWithZeroSaltLength => RSASignaturePadding.CreatePss(0),
                CertKind.RsaPssWithMaxSaltLength => RSASignaturePadding.CreatePss(RSASignaturePadding.PssSaltLengthMax),
                _ => null,
            };
        }

        private static ReadOnlySpan<byte> BuildEmptyExpectedCrl => new byte[]
        {
            0x30, 0x82, 0x01, 0x8E, 0x30, 0x78, 0x02, 0x01, 0x01, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86,
            0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x30, 0x15, 0x31, 0x13, 0x30, 0x11, 0x06, 0x03, 0x55, 0x04,
            0x03, 0x13, 0x0A, 0x42, 0x75, 0x69, 0x6C, 0x64, 0x45, 0x6D, 0x70, 0x74, 0x79, 0x17, 0x0D, 0x31, 0x33,
            0x30, 0x34, 0x30, 0x36, 0x30, 0x37, 0x35, 0x38, 0x30, 0x39, 0x5A, 0x17, 0x0D, 0x31, 0x33, 0x30, 0x34,
            0x30, 0x36, 0x30, 0x38, 0x30, 0x33, 0x30, 0x39, 0x5A, 0xA0, 0x2F, 0x30, 0x2D, 0x30, 0x1F, 0x06, 0x03,
            0x55, 0x1D, 0x23, 0x04, 0x18, 0x30, 0x16, 0x80, 0x14, 0x44, 0x98, 0xBC, 0xC0, 0xCA, 0xA5, 0x3D, 0xF3,
            0xBC, 0x93, 0x69, 0x88, 0x50, 0x8E, 0x72, 0xEA, 0x5D, 0x7B, 0xA9, 0xFE, 0x30, 0x0A, 0x06, 0x03, 0x55,
            0x1D, 0x14, 0x04, 0x03, 0x02, 0x01, 0x7B, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D,
            0x01, 0x01, 0x0B, 0x05, 0x00, 0x03, 0x82, 0x01, 0x01, 0x00, 0x21, 0x59, 0x7F, 0x0F, 0xC6, 0xE2, 0x3D,
            0xE6, 0xDF, 0xF4, 0xFF, 0x80, 0x09, 0xCE, 0xE8, 0xD5, 0xBF, 0x58, 0x3B, 0x8C, 0x81, 0xEE, 0x80, 0x24,
            0x07, 0x5E, 0x74, 0x2E, 0x39, 0xA2, 0x33, 0xF7, 0x68, 0xD7, 0x10, 0x06, 0x90, 0x25, 0x22, 0x4D, 0xA6,
            0xC6, 0x0D, 0x41, 0x00, 0xC3, 0x2D, 0x87, 0x33, 0xBF, 0x5E, 0x48, 0xE0, 0xEC, 0x3F, 0x2F, 0xE2, 0xD6,
            0x77, 0xC4, 0xD2, 0x2E, 0xAE, 0xCE, 0x2F, 0x14, 0x82, 0xEB, 0xCC, 0xDD, 0xB2, 0xF0, 0xE3, 0x43, 0x10,
            0xC7, 0xA0, 0x98, 0x3B, 0x28, 0x2D, 0xA6, 0xFD, 0x75, 0x1B, 0xEE, 0x8D, 0x23, 0x3E, 0x58, 0xE7, 0x46,
            0x2C, 0x92, 0xC0, 0x81, 0xEF, 0xF4, 0x94, 0x40, 0x13, 0x1F, 0x95, 0x31, 0x06, 0xF0, 0x3E, 0x1E, 0x23,
            0x0F, 0xC8, 0x45, 0x63, 0x9C, 0xC0, 0x56, 0x85, 0x6C, 0x1B, 0x9C, 0x38, 0x88, 0x53, 0xD9, 0x1C, 0xDB,
            0x89, 0xF8, 0xE8, 0xF3, 0x97, 0x7C, 0x2D, 0x3E, 0x56, 0x03, 0xBE, 0xA9, 0xF7, 0x91, 0x31, 0xFE, 0x75,
            0x5D, 0xE2, 0x68, 0x65, 0xE3, 0x32, 0xBB, 0x6D, 0x61, 0xB5, 0xE8, 0xB7, 0x28, 0x84, 0xE7, 0x13, 0x5D,
            0xE8, 0x4A, 0x11, 0x7E, 0xDA, 0xBC, 0x7A, 0x71, 0x55, 0xBB, 0x4E, 0x91, 0x49, 0xFA, 0x11, 0x32, 0x8B,
            0xCA, 0x02, 0x09, 0x1F, 0x08, 0xC4, 0x84, 0xC4, 0xBA, 0x2F, 0xF2, 0x20, 0x79, 0x7E, 0x13, 0xB3, 0xB4,
            0x52, 0xD8, 0xBC, 0xAF, 0x96, 0x79, 0x5A, 0xE5, 0xC9, 0xE7, 0x2C, 0xF8, 0x10, 0x5E, 0x40, 0x91, 0xD8,
            0x36, 0x0B, 0xC8, 0x85, 0x5C, 0x2F, 0x67, 0x9A, 0x92, 0xE7, 0xF8, 0xE4, 0xE9, 0x85, 0xD8, 0xBA, 0x65,
            0x05, 0xBC, 0x70, 0x6F, 0x9E, 0xCB, 0x1D, 0x6D, 0x39, 0xA0, 0xEF, 0x24, 0x08, 0x02, 0x85, 0x06, 0xB0,
            0xEA, 0xAC, 0x89, 0xF2, 0x3C, 0x2A, 0xA4, 0x7B, 0x2B, 0xBE, 0x79,
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
            0x80, 0x14, 0x44, 0x98, 0xBC, 0xC0, 0xCA, 0xA5, 0x3D, 0xF3, 0xBC, 0x93, 0x69, 0x88, 0x50, 0x8E, 0x72,
            0xEA, 0x5D, 0x7B, 0xA9, 0xFE, 0x30, 0x0A, 0x06, 0x03, 0x55, 0x1D, 0x14, 0x04, 0x03, 0x02, 0x01, 0x7A,
            0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0D, 0x05, 0x00, 0x03, 0x82,
            0x01, 0x01, 0x00, 0x52, 0xF6, 0xD1, 0x1B, 0x96, 0xBF, 0x94, 0x5D, 0xB0, 0x1C, 0x45, 0x85, 0x72, 0xDC,
            0x82, 0xE2, 0x4F, 0x58, 0xCF, 0x10, 0xBD, 0x4D, 0x43, 0x6C, 0xFC, 0x1E, 0x77, 0xEA, 0x0A, 0x7A, 0x58,
            0xE5, 0x5D, 0x72, 0x07, 0x32, 0x44, 0x72, 0x8C, 0xEE, 0xAF, 0x76, 0x38, 0xD4, 0x11, 0xCB, 0x2E, 0x81,
            0x9D, 0x6B, 0x69, 0x24, 0xB1, 0x8F, 0xC6, 0x58, 0xF5, 0x46, 0x1D, 0x69, 0x6D, 0x67, 0x22, 0x87, 0xB4,
            0x41, 0xBB, 0x4D, 0x7F, 0x71, 0x15, 0x22, 0x17, 0x0D, 0x75, 0x2C, 0xFC, 0x9B, 0xF7, 0xAE, 0xC0, 0x87,
            0x6C, 0xD3, 0xF6, 0x90, 0x47, 0x44, 0x5E, 0x9C, 0x2B, 0x41, 0x71, 0x2D, 0xB5, 0x39, 0xC1, 0x31, 0xE7,
            0x1A, 0xF8, 0x7A, 0x48, 0x14, 0xE6, 0xBF, 0x7B, 0xF4, 0xB9, 0x07, 0x2B, 0x63, 0xEA, 0xB0, 0x37, 0xF1,
            0xD9, 0xC5, 0xEB, 0xF9, 0xD3, 0x61, 0x1B, 0x90, 0x87, 0x45, 0x96, 0xC7, 0x03, 0x44, 0x12, 0x12, 0x12,
            0x47, 0xD0, 0x0E, 0xCE, 0x15, 0xBE, 0x37, 0x97, 0xEF, 0x96, 0xE0, 0x4C, 0xAB, 0x93, 0x3E, 0x82, 0xB2,
            0x29, 0x90, 0xE9, 0xEF, 0xB8, 0x55, 0x7E, 0x99, 0xE1, 0x43, 0x21, 0x56, 0x63, 0x5C, 0x24, 0xED, 0xC0,
            0x93, 0xC6, 0x8E, 0x5F, 0x62, 0x96, 0x01, 0x10, 0x6A, 0x15, 0xEE, 0x3F, 0xDB, 0xA6, 0x23, 0xE2, 0xEB,
            0xFE, 0x18, 0xEF, 0x90, 0xAA, 0xCE, 0xAF, 0x3E, 0x48, 0x84, 0x95, 0xB8, 0x8F, 0x24, 0x18, 0xDB, 0xC7,
            0x03, 0x9B, 0xBF, 0xB0, 0xD3, 0xFE, 0x47, 0xFA, 0x31, 0x15, 0x2D, 0xD7, 0x21, 0xE9, 0x65, 0xBB, 0xA7,
            0x2C, 0x46, 0x1C, 0x33, 0x90, 0xD2, 0xF0, 0x3E, 0x8D, 0x96, 0x03, 0x49, 0x66, 0x83, 0x28, 0xDA, 0x67,
            0x1C, 0x9E, 0x08, 0x94, 0x7F, 0xC0, 0x19, 0x90, 0xDA, 0xE4, 0xB2, 0x60, 0x32, 0xC9, 0xC6, 0xE9, 0xD0,
            0xBB, 0x0C, 0xEA, 0x21,
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
            0x06, 0x03, 0x55, 0x1D, 0x23, 0x04, 0x18, 0x30, 0x16, 0x80, 0x14, 0x44, 0x98, 0xBC, 0xC0, 0xCA, 0xA5,
            0x3D, 0xF3, 0xBC, 0x93, 0x69, 0x88, 0x50, 0x8E, 0x72, 0xEA, 0x5D, 0x7B, 0xA9, 0xFE, 0x30, 0x0A, 0x06,
            0x03, 0x55, 0x1D, 0x14, 0x04, 0x03, 0x02, 0x01, 0x7B, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86,
            0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x03, 0x82, 0x01, 0x01, 0x00, 0x65, 0x30, 0x14, 0xF1, 0x65,
            0x11, 0xEB, 0x52, 0x6B, 0xDB, 0xC3, 0xFD, 0xE3, 0x8A, 0x6B, 0xCA, 0x65, 0x38, 0x0B, 0x27, 0x4A, 0x45,
            0xE6, 0x54, 0xB6, 0xE9, 0xC6, 0x5E, 0xE0, 0x87, 0x39, 0x31, 0x43, 0xD9, 0x44, 0xC9, 0x0E, 0xB6, 0x8C,
            0x94, 0x6A, 0xB0, 0xE6, 0x90, 0xF0, 0xAC, 0x55, 0x27, 0x3F, 0x53, 0x90, 0x52, 0x2F, 0x7C, 0x24, 0x93,
            0x82, 0x95, 0x70, 0x8D, 0x03, 0x81, 0x46, 0x71, 0xEB, 0x0A, 0xB7, 0x9E, 0x7E, 0xC9, 0x0D, 0x52, 0x80,
            0x71, 0xBA, 0x8C, 0xB7, 0xC5, 0x43, 0x98, 0xC1, 0xC3, 0x37, 0xC1, 0x3A, 0xB3, 0xA7, 0xD8, 0xB3, 0x3A,
            0xB9, 0x3D, 0x1C, 0x7A, 0xF4, 0x1C, 0x8C, 0xDF, 0x56, 0x93, 0x3E, 0x18, 0xC8, 0x28, 0xE3, 0xFD, 0x00,
            0x3B, 0x32, 0x7B, 0xDF, 0x6E, 0xC7, 0xD3, 0x70, 0xA1, 0x20, 0x43, 0xA1, 0xD1, 0x48, 0x11, 0xEE, 0x6A,
            0xA1, 0x59, 0xFD, 0x0D, 0xA4, 0x5D, 0x83, 0x53, 0x0E, 0x78, 0xBA, 0x44, 0xB9, 0x54, 0xEC, 0x4E, 0x1D,
            0x5F, 0xEF, 0xDD, 0x6E, 0x6B, 0xF4, 0xC1, 0xE2, 0x01, 0x09, 0xAC, 0x33, 0x5C, 0x96, 0x60, 0x10, 0xCA,
            0xFA, 0x44, 0x16, 0xEC, 0xD9, 0x38, 0x83, 0x24, 0xF1, 0x2C, 0x60, 0x88, 0x3B, 0x33, 0x45, 0xEF, 0x93,
            0x23, 0xBF, 0x85, 0xED, 0x56, 0x49, 0x43, 0xC7, 0xE6, 0xCB, 0x8F, 0x74, 0xF6, 0x43, 0xCD, 0xD6, 0x80,
            0x6C, 0x80, 0xC1, 0xF2, 0x80, 0x8D, 0xF1, 0xD0, 0xAE, 0xD4, 0xF4, 0x0B, 0xD9, 0x21, 0xB1, 0x9D, 0xBB,
            0x4C, 0xF5, 0xCB, 0x9F, 0xE5, 0xB0, 0xD4, 0x6A, 0x79, 0x55, 0xBC, 0x5C, 0xAA, 0x03, 0x69, 0xB2, 0x70,
            0xAC, 0x55, 0x9A, 0x68, 0x41, 0x5C, 0x16, 0x8E, 0x76, 0x7E, 0x1A, 0x28, 0xDD, 0x48, 0xC4, 0x5E, 0xDA,
            0x7A, 0x4B, 0xEC, 0x4F, 0xC3, 0x56, 0x90, 0x80, 0x68, 0x37, 0xF0, 0xEB, 0xFD,
        };
    }
}
