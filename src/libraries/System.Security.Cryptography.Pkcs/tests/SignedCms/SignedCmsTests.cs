// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography.SLHDsa.Tests;
using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static partial class SignedCmsTests
    {
        // TODO: Windows does not support draft 10 PKCS#8 format yet. Remove this and use MLDsa.IsSupported when it does.
        public static bool SupportsDraft10Pkcs8 => MLDsa.IsSupported && !PlatformDetection.IsWindows;

        [Fact]
        public static void DefaultStateBehavior()
        {
            SignedCms cms = new SignedCms();

            Assert.Equal(0, cms.Version);
            Assert.False(cms.Detached, "cms.Detached");

            X509Certificate2Collection certificates = cms.Certificates;
            X509Certificate2Collection certificates2 = cms.Certificates;

            Assert.NotSame(certificates, certificates2);
            Assert.Equal(0, certificates.Count);
            Assert.Equal(0, certificates2.Count);

            ContentInfo content = cms.ContentInfo;
            ContentInfo content2 = cms.ContentInfo;
            Assert.Same(content, content2);

            Assert.Equal("1.2.840.113549.1.7.1", content.ContentType.Value);
            Assert.Equal(Array.Empty<byte>(), content.Content);

            SignerInfoCollection signers = cms.SignerInfos;
            SignerInfoCollection signers2 = cms.SignerInfos;

            Assert.NotSame(signers, signers2);
            Assert.Equal(0, signers.Count);
            Assert.Equal(0, signers2.Count);

            Assert.Throws<InvalidOperationException>(() => cms.CheckSignature(true));
            Assert.Throws<InvalidOperationException>(() => cms.CheckHash());
            Assert.Throws<InvalidOperationException>(() => cms.RemoveSignature(0));
            Assert.Throws<InvalidOperationException>(() => cms.RemoveSignature(-1));
            Assert.Throws<InvalidOperationException>(() => cms.RemoveSignature(10000));
            Assert.Throws<InvalidOperationException>(() => cms.Encode());
        }

        [Fact]
        public static void DecodeNull()
        {
            SignedCms cms = new SignedCms();
            Assert.Throws<ArgumentNullException>(() => cms.Decode(null));
        }

        [Theory]
        [InlineData("Empty", "")]
        [InlineData("Not a sequence", "010100")]
        [InlineData("Too-long BER length", "3005")]
        public static void DecodeInvalid(string description, string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();

            SignedCms cms = new SignedCms();
            Assert.Throws<CryptographicException>(() => cms.Decode(inputData));
        }

        [Fact]
        public static void Decode_WrongContentType()
        {
            const string InputHex =
                "3080" +
                  "0609608648016503040201" +
                  "A080" +
                    "3002" +
                      "0500" +
                    "0000" +
                  "0000";

            byte[] inputData = InputHex.HexToByteArray();

            SignedCms cms = new SignedCms();
            Assert.Throws<CryptographicException>(() => cms.Decode(inputData));
        }

        [Fact]
        public static void Decode_OverwritesAttachedContentInfo()
        {
            ContentInfo original = new ContentInfo(new byte [] { 1, 2, 3, 4, 5 });
            SignedCms cms = new SignedCms(original, false);
            Assert.False(cms.Detached);

            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            Assert.False(cms.Detached);

            ContentInfo newInfo = cms.ContentInfo;
            ContentInfo newInfo2 = cms.ContentInfo;

            Assert.NotSame(original, newInfo);
            Assert.Same(newInfo, newInfo2);
            Assert.NotEqual(original.Content, newInfo.Content);
        }

        [Fact]
        public static void Decode_PreservesDetachedContentInfo()
        {
            ContentInfo original = new ContentInfo(new byte[] { 1, 2, 3, 4, 5 });
            SignedCms cms = new SignedCms(original, true);
            Assert.True(cms.Detached);

            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            Assert.True(cms.Detached);

            ContentInfo newInfo = cms.ContentInfo;
            ContentInfo newInfo2 = cms.ContentInfo;

            Assert.Same(original, newInfo);
            Assert.Same(newInfo, newInfo2);
        }

        [Fact]
        public static void SignedCms_SignerInfos_UniquePerCall()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            SignerInfoCollection signers = cms.SignerInfos;
            SignerInfoCollection signers2 = cms.SignerInfos;

            Assert.NotSame(signers, signers2);
            Assert.Single(signers);
            Assert.Single(signers2);
            Assert.NotSame(signers[0], signers2[0]);

            Assert.NotSame(signers[0].Certificate, signers2[0].Certificate);
            Assert.Equal(signers[0].Certificate, signers2[0].Certificate);
        }

        [Fact]
        public static void SignedCms_Certificates_UniquePerCall()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPssDocument);

            X509Certificate2Collection certs = cms.Certificates;
            X509Certificate2Collection certs2 = cms.Certificates;
            Assert.NotSame(certs, certs2);
            Assert.Single(certs);
            Assert.Single(certs2);

            Assert.NotSame(certs[0], certs2[0]);
            Assert.Equal(certs[0], certs2[0]);
        }

        [Fact]
        public static void CheckSignature_ThrowsOnNullStore()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPssDocument);

            AssertExtensions.Throws<ArgumentNullException>(
                "extraStore",
                () => cms.CheckSignature(null, true));

            AssertExtensions.Throws<ArgumentNullException>(
                "extraStore",
                () => cms.CheckSignature(null, false));
        }

        [Fact]
        public static void Ctor_NoContent_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SignedCms(null));
            Assert.Throws<ArgumentNullException>(() => new SignedCms(null, false));
            Assert.Throws<ArgumentNullException>(() => new SignedCms(null, true));

            Assert.Throws<ArgumentNullException>(
                () => new SignedCms(SubjectIdentifierType.SubjectKeyIdentifier, null));
            Assert.Throws<ArgumentNullException>(
                () => new SignedCms(SubjectIdentifierType.SubjectKeyIdentifier, null, false));
            Assert.Throws<ArgumentNullException>(
                () => new SignedCms(SubjectIdentifierType.SubjectKeyIdentifier, null, true));
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void CheckSignature_ExtraStore_IsAdditional()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            // Assert.NotThrows
            cms.CheckSignature(true);

            // Assert.NotThrows
            cms.CheckSignature(new X509Certificate2Collection(), true);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void Decode_IgnoresExtraData()
        {
            byte[] basis = SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber;
            byte[] data = new byte[basis.Length + 60];
            data.AsSpan(basis.Length).Fill(0x5E);
            basis.AsSpan().CopyTo(data);

            SignedCms cms = new SignedCms();
            cms.Decode(data);

            // Assert.NotThrows
            cms.CheckSignature(true);

            byte[] encoded = cms.Encode();

            Assert.Equal(basis.Length, encoded.Length);
            Assert.Equal(basis.ByteArrayToHex(), encoded.ByteArrayToHex());
        }

        [Fact]
        public static void CheckSignatures_AllRemoved()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            Assert.Single(cms.SignerInfos);

            cms.RemoveSignature(0);
            Assert.Empty(cms.SignerInfos);

            Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
        }

        [Fact]
        public static void CheckHash_AllRemoved()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            Assert.Single(cms.SignerInfos);

            cms.RemoveSignature(0);
            Assert.Empty(cms.SignerInfos);

            Assert.Throws<CryptographicException>(() => cms.CheckHash());
        }

        [Fact]
        public static void RemoveSignature_MatchesIssuerAndSerialNumber()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            Assert.Single(cms.SignerInfos);

            SignerInfo signerInfo = cms.SignerInfos[0];
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, signerInfo.SignerIdentifier.Type);

            int certCount = cms.Certificates.Count;
            cms.RemoveSignature(signerInfo);
            Assert.Empty(cms.SignerInfos);
            Assert.Equal(certCount, cms.Certificates.Count);
        }

        [Fact]
        public static void RemoveSignature_MatchesSubjectKeyIdentifier()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPssDocument);
            Assert.Single(cms.SignerInfos);

            SignerInfo signerInfo = cms.SignerInfos[0];
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, signerInfo.SignerIdentifier.Type);

            int certCount = cms.Certificates.Count;
            cms.RemoveSignature(signerInfo);
            Assert.Empty(cms.SignerInfos);
            Assert.Equal(certCount, cms.Certificates.Count);
        }

        [Fact]
        public static void RemoveSignature_MatchesNoSignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.NoSignatureSignedWithAttributesAndCounterSignature);
            Assert.Single(cms.SignerInfos);

            SignerInfo signerInfo = cms.SignerInfos[0];
            Assert.Equal(SubjectIdentifierType.NoSignature, signerInfo.SignerIdentifier.Type);

            cms.RemoveSignature(signerInfo);
            Assert.Empty(cms.SignerInfos);
        }

        [Fact]
        public static void RemoveSignature_WithNoMatch()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            SignerInfo wrongSignerInfo = cms.SignerInfos[0];
            cms.Decode(SignedDocuments.RsaPssDocument);
            Assert.Single(cms.SignerInfos);

            Assert.Throws<CryptographicException>(() => cms.RemoveSignature(wrongSignerInfo));
            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);
        }

        [Fact]
        public static void RemoveSignature_Null()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            AssertExtensions.Throws<ArgumentNullException>(
                "signerInfo",
                () => cms.RemoveSignature(null));

            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);
        }

        [Fact]
        public static void RemoveSignature_OutOfRange()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            ArgumentOutOfRangeException ex = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "index",
                () => cms.RemoveSignature(-1));

            Assert.Null(ex.ActualValue);
            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);

            ex = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "index",
                () => cms.RemoveSignature(1));

            Assert.Null(ex.ActualValue);

            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);
        }

        [Fact]
        public static void DetachedContent_ConcatEmbeddedContent()
        {
            // 1: Prove the document works.
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.NoSignatureWithNoAttributes);
            cms.SignerInfos[0].CheckHash();

            ContentInfo save = cms.ContentInfo;

            // 2: Using the empty detached content, see that the document still works.
            cms = new SignedCms(new ContentInfo(Array.Empty<byte>()), true);
            cms.Decode(SignedDocuments.NoSignatureWithNoAttributes);
            cms.SignerInfos[0].CheckHash();

            //// 3: Using the saved content, prove that the document no longer works.
            cms = new SignedCms(save, true);
            cms.Decode(SignedDocuments.NoSignatureWithNoAttributes);
            Assert.Throws<CryptographicException>(() => cms.SignerInfos[0].CheckHash());

            // 4: Modify the contained hash, see that it previously didn't work for the "right" reason.
            string inputHex = SignedDocuments.NoSignatureWithNoAttributes.ByteArrayToHex();
            inputHex = inputHex.Replace(
                // SHA1("Microsoft Corporation")
                "A5F085E7F326F3D6CA3BFD6280A3DE8EBC2EA60E",
                // SHA1("Microsoft CorporationMicrosoft Corporation")
                "346804FD67B37C27A203CD514B267711CFB39118");

            cms = new SignedCms(save, true);
            cms.Decode(inputHex.HexToByteArray());
            cms.SignerInfos[0].CheckHash();
        }

        [Theory]
        [InlineData(SubjectIdentifierType.Unknown)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier)]
        [InlineData((SubjectIdentifierType)76)]
        public static void ZeroArgComputeSignature(SubjectIdentifierType identifierType)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(identifierType, contentInfo);

            Assert.Throws<InvalidOperationException>(() => cms.ComputeSignature());

            cms = new SignedCms(identifierType, contentInfo, detached: true);
            Assert.Throws<InvalidOperationException>(() => cms.ComputeSignature());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void ZeroArgComputeSignature_NoSignature(bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(SubjectIdentifierType.NoSignature, contentInfo, detached);

            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<NullReferenceException>(() => cms.ComputeSignature());
            }
            else
            {
                cms.ComputeSignature();

                SignerInfoCollection signers = cms.SignerInfos;
                Assert.Equal(1, signers.Count);
                Assert.Equal(SubjectIdentifierType.NoSignature, signers[0].SignerIdentifier.Type);
                cms.CheckHash();
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
            }
        }

        [Theory]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, false)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, true)]
        // NoSignature is a different test, because it succeeds (CoreFX) or fails differently (NetFX)
        public static void SignSilentWithNoCertificate(SubjectIdentifierType identifierType, bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            Assert.Throws<InvalidOperationException>(
                () => cms.ComputeSignature(new CmsSigner(identifierType), silent: true));
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, false)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, true)]
        // NoSignature is a different test, because it succeeds (CoreFX) or fails differently (NetFX)
        public static void SignNoisyWithNoCertificate_NotSupported(
            SubjectIdentifierType identifierType,
            bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            Assert.Throws<PlatformNotSupportedException>(
                () => cms.ComputeSignature(new CmsSigner(identifierType), silent: false));
        }

        [Theory]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, false)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, true)]
        public static void AddFirstSigner_RSA(SubjectIdentifierType identifierType, bool detached)
        {
            AssertAddFirstSigner(
                identifierType,
                detached,
                cms =>
                {
                    using (X509Certificate2 signerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
                    {
                        CmsSigner signer = new CmsSigner(identifierType, signerCert);
                        cms.ComputeSignature(signer);
                    }
                },
                firstSigner => { /* No additional asserts */ },
                roundtrippedFirstSigner => { /* No additional asserts */  });
        }

        [Fact]
        public static void AddSignerWithNegativeSerial()
        {
            const string expectedSerial = "FD319CB1514B06AF49E00522277E43C8";

            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, false);

            using (X509Certificate2 cert = Certificates.NegativeSerialNumber.TryGetCertificateWithPrivateKey())
            {
                Assert.Equal(expectedSerial, cert.SerialNumber);

                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                signer.IncludeOption = X509IncludeOption.EndCertOnly;

                cms.ComputeSignature(signer);
            }

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Equal(1, signers.Count);

            SignerInfo signerInfo = signers[0];
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, signerInfo.SignerIdentifier.Type);

            X509IssuerSerial issuerSerial = (X509IssuerSerial)signerInfo.SignerIdentifier.Value;
            Assert.Equal(expectedSerial, issuerSerial.SerialNumber);

            Assert.NotNull(signerInfo.Certificate);
            // Assert.NoThrow
            cms.CheckSignature(true);
        }

        [Theory]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true)]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void AddFirstSigner_DSA(SubjectIdentifierType identifierType, bool detached)
        {
#if NET
            byte[]? signature = null;
#endif

            AssertAddFirstSigner(
                identifierType,
                detached,
                cms =>
                {
                    using (X509Certificate2 signerCert = Certificates.Dsa1024.TryGetCertificateWithPrivateKey())
                    {
                        CmsSigner signer = new CmsSigner(identifierType, signerCert);
                        signer.IncludeOption = X509IncludeOption.EndCertOnly;
                        // Best compatibility for DSA is SHA-1 (FIPS 186-2)
                        signer.DigestAlgorithm = new Oid(Oids.Sha1, Oids.Sha1);
                        cms.ComputeSignature(signer);
                    }
                },
                firstSigner =>
                {
#if NET
                    // Store signature for comparison after roundtrip.
                    signature = firstSigner.GetSignature();
                    Assert.NotEmpty(signature);
                    // DSA PKIX signature format is a DER SEQUENCE.
                    Assert.Equal(0x30, signature[0]);
#endif
                },
                roundtrippedFirstSigner =>
                {
#if NET
                    byte[] sig2 = roundtrippedFirstSigner.GetSignature();
                    Assert.Equal(signature, sig2);
#endif
                });
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false, Oids.Sha256)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true, Oids.Sha256)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, false, Oids.Sha256)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, true, Oids.Sha256)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false, Oids.Sha1)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, true, Oids.Sha1)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true, Oids.Sha384)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, false, Oids.Sha384)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false, Oids.Sha512)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, true, Oids.Sha512)]
        public static void AddFirstSigner_ECDSA(SubjectIdentifierType identifierType, bool detached, string digestOid)
        {
#if NET
            byte[]? signature = null;
#endif

            AssertAddFirstSigner(
                identifierType,
                detached,
                cms =>
                {
                    using (X509Certificate2 signerCert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
                    {
                        CmsSigner signer = new CmsSigner(identifierType, signerCert);
                        signer.IncludeOption = X509IncludeOption.EndCertOnly;
                        signer.DigestAlgorithm = new Oid(digestOid, digestOid);
                        cms.ComputeSignature(signer);
                    }
                },
                firstSigner =>
                {
#if NET
                    // Store signature for comparison after roundtrip.
                    signature = firstSigner.GetSignature();
                    Assert.NotEmpty(signature);
                    // ECDSA PKIX signature format is a DER SEQUENCE.
                    Assert.Equal(0x30, signature[0]);

                    // ECDSA Oids are all under 1.2.840.10045.4.
                    Assert.StartsWith("1.2.840.10045.4.", firstSigner.SignatureAlgorithm.Value);
#endif
                },
                roundtrippedFirstSigner =>
                {
#if NET
                    byte[] sig2 = roundtrippedFirstSigner.GetSignature();
                    Assert.Equal(signature, sig2);
#endif
                });
        }

        public static IEnumerable<object[]> AddFirstSignerSlhDsaTestData =>
            from sit in new[] { SubjectIdentifierType.IssuerAndSerialNumber, SubjectIdentifierType.SubjectKeyIdentifier }
            from detached in new[] { false, true }
            from algorithms in new (SlhDsaAlgorithm signAlgorithm, string hashAlgorithm)[]
            {
                (SlhDsaAlgorithm.SlhDsaSha2_128s, Oids.Sha256),
                (SlhDsaAlgorithm.SlhDsaShake128f, Oids.Shake128),
                (SlhDsaAlgorithm.SlhDsaSha2_256f, Oids.Sha512),
                (SlhDsaAlgorithm.SlhDsaShake256f, Oids.Shake256),
            }
            from SlhDsaTestData.SlhDsaGeneratedKeyInfo info in SlhDsaTestData.GeneratedKeyInfosRaw
            where info.Algorithm == algorithms.signAlgorithm // Find the matching test data for the algorithm
            select new object[] { sit, detached, algorithms.hashAlgorithm, info };

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(AddFirstSignerSlhDsaTestData))]
        public static void AddFirstSigner_SlhDsa(SubjectIdentifierType identifierType, bool detached, string digestOid, SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            byte[]? signature = null;

            AssertAddFirstSigner(
                identifierType,
                detached,
                cms =>
                {
                    CertLoader loader = Certificates.SlhDsaGeneratedCerts.Single(cert => cert.CerData.SequenceEqual(info.Certificate));
                    using (X509Certificate2 signerCert = loader.TryGetCertificateWithPrivateKey())
                    {
                        CmsSigner signer = new CmsSigner(identifierType, signerCert);
                        signer.IncludeOption = X509IncludeOption.EndCertOnly;
                        signer.DigestAlgorithm = new Oid(digestOid, digestOid);
                        cms.ComputeSignature(signer);
                    }
                },
                firstSigner =>
                {
                    // Store signature for comparison after roundtrip.
                    signature = firstSigner.GetSignature();
                    Assert.NotEmpty(signature);

                    // SLH-DSA Oids are all under 2.16.840.1.101.3.4.3.
                    Assert.StartsWith("2.16.840.1.101.3.4.3.", firstSigner.SignatureAlgorithm.Value);
                },
                roundtrippedFirstSigner =>
                {
                    byte[] sig2 = roundtrippedFirstSigner.GetSignature();
                    Assert.Equal(signature, sig2);
                });
        }

        public static IEnumerable<object[]> AddFirstSignerMLDsaTestData =>
            from sit in new[] { SubjectIdentifierType.IssuerAndSerialNumber, SubjectIdentifierType.SubjectKeyIdentifier }
            from detached in new[] { false, true }
            from data in new (MLDsaAlgorithm algorithm, string hashAlgorithm)[]
            {
                (MLDsaAlgorithm.MLDsa44, Oids.Shake128),
                (MLDsaAlgorithm.MLDsa65, Oids.Sha512),
                (MLDsaAlgorithm.MLDsa87, Oids.Shake256),
            }
            select new object[] { sit, detached, data.hashAlgorithm, data.algorithm };

        [ConditionalTheory(nameof(SupportsDraft10Pkcs8))]
        [MemberData(nameof(AddFirstSignerMLDsaTestData))]
        public static void AddFirstSigner_MLDsa(SubjectIdentifierType identifierType, bool detached, string digestOid, MLDsaAlgorithm algorithm)
        {
            byte[]? signature = null;

            AssertAddFirstSigner(
                identifierType,
                detached,
                cms =>
                {
                    using (X509Certificate2 signerCert = Certificates.MLDsaIetf[algorithm].TryGetCertificateWithPrivateKey())
                    {
                        CmsSigner signer = new CmsSigner(identifierType, signerCert);
                        signer.IncludeOption = X509IncludeOption.EndCertOnly;
                        signer.DigestAlgorithm = new Oid(digestOid, digestOid);
                        cms.ComputeSignature(signer);
                    }
                },
                firstSigner =>
                {
                    // Store signature for comparison after roundtrip.
                    signature = firstSigner.GetSignature();
                    Assert.NotEmpty(signature);

                    // ML-DSA Oids are all under 2.16.840.1.101.3.4.3.
                    Assert.StartsWith("2.16.840.1.101.3.4.3.", firstSigner.SignatureAlgorithm.Value);
                },
                roundtrippedFirstSigner =>
                {
                    byte[] sig2 = roundtrippedFirstSigner.GetSignature();
                    Assert.Equal(signature, sig2);
                });
        }

        private static void AssertAddFirstSigner(
            SubjectIdentifierType identifierType,
            bool detached,
            Action<SignedCms> signCms,
            Action<SignerInfo> assertFirstSigner,
            Action<SignerInfo> assertRoundtrippedFirstSigner)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            signCms(cms);

            Assert.Same(contentInfo.Content, cms.ContentInfo.Content);
            Assert.Single(cms.SignerInfos);

            // Currently the test assumes only a single certificate is added.
            // If this assertion fails in newly added tests, update the provided signing
            // callback to use EndCertOnly.
            Assert.Single(cms.Certificates);

            int expectedVersion = identifierType == SubjectIdentifierType.SubjectKeyIdentifier ? 3 : 1;
            Assert.Equal(expectedVersion, cms.Version);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Equal(identifierType, firstSigner.SignerIdentifier.Type);
            Assert.NotNull(firstSigner.Certificate);
            Assert.NotSame(cms.Certificates[0], firstSigner.Certificate);
            Assert.Equal(cms.Certificates[0], firstSigner.Certificate);

            assertFirstSigner(firstSigner);

            cms.CheckSignature(true);
            byte[] encoded = cms.Encode();

            cms = new SignedCms();
            cms.Decode(encoded);

            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);
            Assert.Equal(expectedVersion, cms.Version);
            Assert.Equal(identifierType, cms.SignerInfos[0].SignerIdentifier.Type);
            Assert.Equal(firstSigner.Certificate, cms.SignerInfos[0].Certificate);

            assertRoundtrippedFirstSigner(cms.SignerInfos[0]);

            if (detached)
            {
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
                cms = new SignedCms(contentInfo, detached);
                cms.Decode(encoded);
            }

            cms.CheckSignature(true);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public static void AddFirstSigner_NoSignature(bool detached, bool includeExtraCert)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);
            X509Certificate2Collection certs;

            // A certificate shouldn't really be required here, but on .NET Framework
            // it will encounter throw a NullReferenceException.
            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            using (X509Certificate2 cert2 = Certificates.DHKeyAgree1.GetCertificate())
            {
                CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.NoSignature, cert);

                if (includeExtraCert)
                {
                    cmsSigner.Certificates.Add(cert2);
                }

                cms.ComputeSignature(cmsSigner);

                certs = cms.Certificates;

                if (includeExtraCert)
                {
                    Assert.Equal(1, certs.Count);
                    Assert.Equal(cert2.RawData, certs[0].RawData);
                }
                else
                {
                    Assert.Equal(0, certs.Count);
                }
            }

            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            cms.CheckHash();

            byte[] encoded = cms.Encode();

            if (detached)
            {
                cms = new SignedCms(contentInfo, detached);
            }
            else
            {
                cms = new SignedCms();
            }

            cms.Decode(encoded);
            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            cms.CheckHash();

            SignerInfoCollection signerInfos = cms.SignerInfos;
            Assert.Equal(1, signerInfos.Count);

            SignerInfo firstSigner = signerInfos[0];
            Assert.ThrowsAny<CryptographicException>(() => firstSigner.CheckSignature(true));
            firstSigner.CheckHash();

            certs = cms.Certificates;

            if (includeExtraCert)
            {
                Assert.Equal(1, certs.Count);
                Assert.Equal("CN=DfHelleKeyAgreement1", certs[0].SubjectName.Name);
            }
            else
            {
                Assert.Equal(0, certs.Count);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddFirstSigner_NoSignature_NoCert(bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            Action sign = () =>
                cms.ComputeSignature(
                    new CmsSigner(SubjectIdentifierType.NoSignature)
                    {
                        IncludeOption = X509IncludeOption.None,
                    });

            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<NullReferenceException>(sign);
            }
            else
            {
                sign();
                Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
                cms.CheckHash();
                Assert.Equal(1, cms.SignerInfos.Count);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddSecondSigner_NoSignature(bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                cms.ComputeSignature(
                    new CmsSigner(cert)
                    {
                        IncludeOption = X509IncludeOption.None,
                    });

                Assert.Throws<CryptographicException>(
                    () =>
                        cms.ComputeSignature(
                            new CmsSigner(SubjectIdentifierType.NoSignature)
                            {
                                IncludeOption = X509IncludeOption.None,
                            }));
            }

            Assert.Equal(1, cms.SignerInfos.Count);
            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            cms.CheckHash();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddSecondSigner_NoSignature_AfterRemove(bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                cms.ComputeSignature(
                    new CmsSigner(cert)
                    {
                        IncludeOption = X509IncludeOption.None,
                    });

                Assert.Throws<CryptographicException>(
                    () =>
                        cms.ComputeSignature(
                            new CmsSigner(SubjectIdentifierType.NoSignature)
                            {
                                IncludeOption = X509IncludeOption.None,
                            }));

                cms.RemoveSignature(0);

                // Because the document was already initialized (when initially signed),
                // the "NoSignature must be the first signer" exception is thrown, even
                // though there are no signers.
                Assert.Throws<CryptographicException>(
                    () =>
                        cms.ComputeSignature(
                            new CmsSigner(SubjectIdentifierType.NoSignature)
                            {
                                IncludeOption = X509IncludeOption.None,
                            }));
            }

            Assert.Equal(0, cms.SignerInfos.Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddSecondSigner_NoSignature_LoadUnsigned(bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                cms.ComputeSignature(
                    new CmsSigner(cert)
                    {
                        IncludeOption = X509IncludeOption.None,
                    });

                Assert.Throws<CryptographicException>(
                    () =>
                        cms.ComputeSignature(
                            new CmsSigner(SubjectIdentifierType.NoSignature)
                            {
                                IncludeOption = X509IncludeOption.None,
                            }));

                cms.RemoveSignature(0);

                // Reload the document now that it has no signatures.
                byte[] encoded = cms.Encode();

                if (detached)
                {
                    cms = new SignedCms(contentInfo, detached);
                }
                else
                {
                    cms = new SignedCms();
                }

                cms.Decode(encoded);

                // Because the document was already initialized (when loaded),
                // the "NoSignature must be the first signer" exception is thrown, even
                // though there are no signers.
                Assert.Throws<CryptographicException>(
                    () =>
                        cms.ComputeSignature(
                            new CmsSigner(SubjectIdentifierType.NoSignature)
                            {
                                IncludeOption = X509IncludeOption.None,
                            }));
            }

            Assert.Equal(0, cms.SignerInfos.Count);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public static void AddSigner_DuplicateCert_RSA(bool skidFirst, bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            SubjectIdentifierType first;
            SubjectIdentifierType second;
            int expectedInitialVersion;

            if (skidFirst)
            {
                first = SubjectIdentifierType.SubjectKeyIdentifier;
                second = SubjectIdentifierType.IssuerAndSerialNumber;
                expectedInitialVersion = 3;
            }
            else
            {
                first = SubjectIdentifierType.IssuerAndSerialNumber;
                second = SubjectIdentifierType.SubjectKeyIdentifier;
                expectedInitialVersion = 1;
            }

            byte[] firstEncoding;

            using (X509Certificate2 signerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(first, signerCert);
                cms.ComputeSignature(signer);

                Assert.Single(cms.Certificates);
                Assert.Single(cms.SignerInfos);
                Assert.Equal(expectedInitialVersion, cms.Version);

                Assert.Equal(first, cms.SignerInfos[0].SignerIdentifier.Type);
                Assert.Equal(expectedInitialVersion, cms.SignerInfos[0].Version);

                firstEncoding = cms.Encode();

                CmsSigner signer2 = new CmsSigner(second, signerCert);
                cms.ComputeSignature(signer2);
            }

            Assert.Single(cms.Certificates);
            Assert.Equal(2, cms.SignerInfos.Count);

            // One of them is a V3 signer, so the whole document is V3.
#if NETFRAMEWORK
            // Windows CMS computes the version on the first signer, and doesn't
            // seem to lift it on the second one.
            // It encoded the message as
            // SignedData.version=1,
            //   SignedData.SignerInfos[0].version=3
            //   SignedData.SignerInfos[1].version=1
            if (skidFirst)
            {
#endif
            Assert.Equal(3, cms.Version);
#if NETFRAMEWORK
            }
#endif

            Assert.Equal(first, cms.SignerInfos[0].SignerIdentifier.Type);
            Assert.Equal(second, cms.SignerInfos[1].SignerIdentifier.Type);
            Assert.Equal(cms.SignerInfos[0].Certificate, cms.SignerInfos[1].Certificate);

            cms.CheckSignature(true);

            byte[] secondEncoding = cms.Encode();
            Assert.True(secondEncoding.Length > firstEncoding.Length);
        }

        [Fact]
        public static void CannotSignEmptyContent()
        {
            SignedCms cms = new SignedCms();

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert);

                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void EncodeDoesNotPreserveOrder_DecodeDoes()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.UnsortedSignerInfos);

            // The document here was built by the CounterSigningDerOrder tests,
            // then editing the binary to flip the one-counter-signer "yellow"
            // into the first position.

            Assert.Equal(3, cms.SignerInfos.Count);
            // Enough data to prove the order.
            Assert.Single(cms.SignerInfos[0].CounterSignerInfos);

            Assert.Empty(cms.SignerInfos[1].CounterSignerInfos);
            Assert.Empty(cms.SignerInfos[1].SignedAttributes);

            Assert.Empty(cms.SignerInfos[2].CounterSignerInfos);
            Assert.NotEmpty(cms.SignerInfos[2].SignedAttributes);

            cms.Decode(cms.Encode());

            // { 0, 1, 2 } => { 1, 2, 0 }

            Assert.Empty(cms.SignerInfos[0].CounterSignerInfos);
            Assert.Empty(cms.SignerInfos[0].SignedAttributes);

            Assert.Empty(cms.SignerInfos[1].CounterSignerInfos);
            Assert.NotEmpty(cms.SignerInfos[1].SignedAttributes);

            Assert.Single(cms.SignerInfos[2].CounterSignerInfos);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void EnsureExtraCertsAdded(bool newDocument)
        {
            SignedCms cms;

            if (newDocument)
            {
                ContentInfo data = new ContentInfo(new byte[] { 1, 2, 3 });
                cms = new SignedCms(data, false);
            }
            else
            {
                cms = new SignedCms();

                // DSA is not supported on mobile Apple platforms, so use ECDsa signed document instead
                if (PlatformDetection.UsesMobileAppleCrypto)
                {
                    cms.Decode(SignedDocuments.SHA256ECDSAWithRsaSha256DigestIdentifier);
                }
                else
                {
                    cms.Decode(SignedDocuments.OneDsa1024);
                }
            }

            int preCount = cms.Certificates.Count;

            using (X509Certificate2 unrelated1 = Certificates.DHKeyAgree1.GetCertificate())
            using (X509Certificate2 unrelated1Copy = Certificates.DHKeyAgree1.GetCertificate())
            using (X509Certificate2 unrelated2 = Certificates.RSAKeyTransfer2.GetCertificate())
            using (X509Certificate2 unrelated3 = Certificates.RSAKeyTransfer3.GetCertificate())
            using (X509Certificate2 signerCert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signerCert);
                signer.Certificates.Add(unrelated1);
                signer.Certificates.Add(unrelated2);
                signer.Certificates.Add(unrelated3);
                signer.Certificates.Add(unrelated1Copy);
                cms.ComputeSignature(signer);

                bool ExpectCopyRemoved =
#if !NETFRAMEWORK
                    true
#else
                    false
#endif
                    ;

                int expectedAddedCount = 4;

                if (!ExpectCopyRemoved)
                {
                    expectedAddedCount++;
                }

                // In .NET Framework adding (document) signers adds certificates at the end
                // EXCEPT for the first signer, which triggers an internal Decode(Encode())
                // which is only observable if there were multiple certificates.
                int u1Idx;
                int u1CopyIdx;
                int u2Idx;
                int u3Idx;
                int sIdx;

                if (newDocument)
                {
                    // These indices are manually computable by observing the certificate sizes.
                    // But they'll be stable unless a cert changes.
                    u1Idx = 3;
                    u1CopyIdx = 4;
                    u2Idx = 0;
                    u3Idx = 1;
                    sIdx = 2;
                }
                else
                {
                    u1Idx = 0;
                    u1CopyIdx = 3;
                    u2Idx = 1;
                    u3Idx = 2;
                    sIdx = ExpectCopyRemoved ? 3 : 4;
                }

                X509Certificate2Collection certs = cms.Certificates;
                Assert.Equal(preCount + expectedAddedCount, certs.Count);

                Assert.Equal(unrelated1, certs[preCount + u1Idx]);
                Assert.NotSame(unrelated1, certs[preCount + u1Idx]);

                Assert.Equal(unrelated2, certs[preCount + u2Idx]);
                Assert.NotSame(unrelated2, certs[preCount + u2Idx]);

                Assert.Equal(unrelated3, certs[preCount + u3Idx]);
                Assert.NotSame(unrelated3, certs[preCount + u3Idx]);

                if (!ExpectCopyRemoved)
                {
                    Assert.Equal(unrelated1, certs[preCount + u1CopyIdx]);
                    Assert.NotSame(unrelated1, certs[preCount + u1CopyIdx]);
                }

                Assert.Equal(signerCert, certs[preCount + sIdx]);
                Assert.NotSame(signerCert, certs[preCount + sIdx]);
            }

            cms.CheckSignature(true);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void UntrustedCertFails_WhenTrustChecked()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            // Assert.NoThrow
            cms.CheckSignature(true);

            Assert.Throws<CryptographicException>(() => cms.CheckSignature(false));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void EnsureDataIsolation_NewDocument(bool detached)
        {
            byte[] contentBytes = { 9, 8, 7, 6, 5 };
            ContentInfo contentInfo = new ContentInfo(contentBytes);
            SignedCms cms = new SignedCms(contentInfo, detached);

            SubjectIdentifierType firstType = SubjectIdentifierType.IssuerAndSerialNumber;
            SubjectIdentifierType secondType = SubjectIdentifierType.SubjectKeyIdentifier;

            using (X509Certificate2 signerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(firstType, signerCert);
                signer.SignedAttributes.Add(new Pkcs9SigningTime());
                cms.ComputeSignature(signer);
            }

            // CheckSignature doesn't read the public mutable data
            contentInfo.Content[0] ^= 0xFF;
#if !NET
            contentInfo.ContentType.Value = Oids.Pkcs7Hashed;
#endif
            cms.CheckSignature(true);

            using (X509Certificate2 signerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(secondType, signerCert);
                signer.SignedAttributes.Add(new Pkcs9SigningTime());

                // A second ComputeSignature uses the content value from the first one.
                cms.ComputeSignature(signer);
            }

            // They should have the same content digests.
            AsnEncodedData firstDigest = cms.SignerInfos[0].SignedAttributes
                .OfType<CryptographicAttributeObject>().First(cao => cao.Oid.Value == Oids.MessageDigest).Values[0];

            AsnEncodedData secondDigest = cms.SignerInfos[1].SignedAttributes
                .OfType<CryptographicAttributeObject>().First(cao => cao.Oid.Value == Oids.MessageDigest).Values[0];

            Assert.Equal(firstDigest.RawData.ByteArrayToHex(), secondDigest.RawData.ByteArrayToHex());

            byte[] encoded = cms.Encode();

            if (detached)
            {
                cms.Decode(encoded);

                // Because Decode leaves ContentInfo alone, and Decode resets the
                // "known" content, this will fail due to the tampered content.
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));

                // So put it back.
                cms.ContentInfo.Content[0] ^= 0xFF;
            }

            cms.Decode(encoded);

            if (detached)
            {
                // And break it again.
                cms.ContentInfo.Content[0] ^= 0xFF;
            }

            // Destroy the content that just got decoded.
            encoded.AsSpan().Fill(0x55);
            cms.CheckSignature(true);
        }

        [Fact]
        public static void SignWithImplicitSubjectKeyIdentifier()
        {
            byte[] contentBytes = { 9, 8, 7, 6, 5 };
            ContentInfo contentInfo = new ContentInfo(contentBytes);
            SignedCms cms = new SignedCms(contentInfo, false);

            using (X509Certificate2 signerCert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                // This cert has no Subject Key Identifier extension.
                Assert.Null(signerCert.Extensions[Oids.SubjectKeyIdentifier]);

                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, signerCert);
                cms.ComputeSignature(signer);
            }

            Assert.Equal(
                "6B4A6B92FDED07EE0119F3674A96D1A70D2A588D",
                (string)cms.SignerInfos[0].SignerIdentifier.Value);

            // Assert.NoThrow
            cms.CheckSignature(true);
        }

        [Fact]
        public static void SignerInfoCollection_Indexer_MinusOne ()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            Assert.Throws<ArgumentOutOfRangeException>(() => cms.SignerInfos[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => cms.SignerInfos[1]);
        }

        [Theory]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier)]
        public static void SignEnveloped(SubjectIdentifierType signerType)
        {
            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                EnvelopedCms envelopedCms = new EnvelopedCms(new ContentInfo(new byte[] { 3 }));
                envelopedCms.Encrypt(new CmsRecipient(signerType, cert));

                SignedCms signedCms = new SignedCms(
                    new ContentInfo(new Oid(Oids.Pkcs7Enveloped), envelopedCms.Encode()));

                signedCms.ComputeSignature(new CmsSigner(cert));
                signedCms.CheckSignature(true);

                SignerInfoCollection signers = signedCms.SignerInfos;
                Assert.Equal(1, signers.Count);

                CryptographicAttributeObjectCollection attrs = signers[0].SignedAttributes;
                Assert.Equal(2, attrs.Count);

                CryptographicAttributeObject firstAttrSet = attrs[0];
                Assert.Equal(Oids.ContentType, firstAttrSet.Oid.Value);
                Assert.Equal(1, firstAttrSet.Values.Count);
                Assert.Equal(Oids.ContentType, firstAttrSet.Values[0].Oid.Value);
                Assert.Equal("06092A864886F70D010703", firstAttrSet.Values[0].RawData.ByteArrayToHex());

                CryptographicAttributeObject secondAttrSet = attrs[1];
                Assert.Equal(Oids.MessageDigest, secondAttrSet.Oid.Value);
                Assert.Equal(1, secondAttrSet.Values.Count);
                Assert.Equal(Oids.MessageDigest, secondAttrSet.Values[0].Oid.Value);
            }
        }

        [Theory]
        [InlineData(Oids.Pkcs7Data, "0102", false)]
        // .NET Framework PKCS7: The length exceeds the payload, so this fails.
        [InlineData("0.0", "0102", true)]
        [InlineData("0.0", "04020102", false)]
        // .NET Framework PKCS7: The payload exceeds the length, so this fails.
        [InlineData("0.0", "0402010203", true)]
        [InlineData("0.0", "010100", false)]
        [InlineData(Oids.Pkcs7Hashed, "010100", false)]
        [InlineData(Oids.Pkcs7Hashed, "3000", false)]
        public static void SignIdentifiedContent(string oidValue, string contentHex, bool netfxProblem)
        {
            SignedCms signedCms = new SignedCms(
                new ContentInfo(new Oid(oidValue, "Some Friendly Name"), contentHex.HexToByteArray()));

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                try
                {
                    signedCms.ComputeSignature(new CmsSigner(cert));
                }
                catch (CryptographicException) when (netfxProblem)
                {
                    // When no signed or unsigned attributes are present and the signer uses
                    // IssuerAndSerial as the identifier type, .NET Framework uses an older PKCS7 encoding
                    // of the current CMS one.  The older encoding fails on these inputs because of a
                    // difference in PKCS7 vs CMS encoding of values using types other than Pkcs7Data.
                    return;
                }

                byte[] encoded = signedCms.Encode();
                signedCms.Decode(encoded);
            }

            // Assert.NoThrows
            signedCms.CheckSignature(true);

            Assert.Equal(oidValue, signedCms.ContentInfo.ContentType.Value);
            Assert.Equal(contentHex, signedCms.ContentInfo.Content.ByteArrayToHex());
        }

        [Fact]
        public static void VerifyUnsortedAttributeSignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.DigiCertTimeStampToken);

            // Assert.NoThrows
            cms.CheckSignature(true);
        }

        [Fact]
        public static void VerifyUnsortedAttributeSignature_ImportExportImport()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.DigiCertTimeStampToken);

            // Assert.NoThrows
            cms.CheckSignature(true);

            byte[] exported = cms.Encode();
            cms = new SignedCms();
            cms.Decode(exported);

            // Assert.NoThrows
            cms.CheckSignature(true);
        }

        [Fact]
        public static void AddSignerToUnsortedAttributeSignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.DigiCertTimeStampToken);

            // Assert.NoThrows
            cms.CheckSignature(true);

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                cms.ComputeSignature(
                    new CmsSigner(
                        SubjectIdentifierType.IssuerAndSerialNumber,
                        cert));

                cms.ComputeSignature(
                    new CmsSigner(
                        SubjectIdentifierType.SubjectKeyIdentifier,
                        cert));
            }

            // Assert.NoThrows
            cms.CheckSignature(true);

            byte[] exported = cms.Encode();
            cms = new SignedCms();
            cms.Decode(exported);

            // Assert.NoThrows
            cms.CheckSignature(true);
        }

        [Fact]
        public static void CheckSignature_Pkcs1_RsaWithSha256()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(SignedDocuments.RsaPkcs1Sha256WithRsa);

            // Assert.NoThrows
            signedCms.CheckSignature(true);
        }

        [Fact]
        public static void CheckSignature_Pkcs1_Sha1_Declared_Sha256WithRsa()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(SignedDocuments.RsaPkcs1SignedSha1DeclaredSha256WithRsa);

            Assert.Throws<CryptographicException>(() => {
                signedCms.CheckSignature(true);
            });
        }

        [Theory]
        [InlineData(null, "0102", Oids.Pkcs7Data)]
        [InlineData(null, "010100", Oids.Pkcs7Data)]
        [InlineData("potato", "010100", null)]
        [InlineData(" 1.1", "010100", null)]
        [InlineData("1.1 ", "010100", null)]
        [InlineData("1 1", "010100", null)]
        public static void SignIdentifiedContent_BadOid(string oidValueIn, string contentHex, string oidValueOut)
        {
            SignedCms signedCms = new SignedCms(
                new ContentInfo(new Oid(oidValueIn, "Some Friendly Name"), contentHex.HexToByteArray()));

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                Action signAction = () => signedCms.ComputeSignature(new CmsSigner(cert));

                if (oidValueOut == null)
                {
                    Assert.ThrowsAny<CryptographicException>(signAction);
                    return;
                }

                signAction();

                byte[] encoded = signedCms.Encode();
                signedCms.Decode(encoded);
            }

            // Assert.NoThrows
            signedCms.CheckSignature(true);

            Assert.Equal(oidValueOut, signedCms.ContentInfo.ContentType.Value);
            Assert.Equal(contentHex, signedCms.ContentInfo.Content.ByteArrayToHex());
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void CheckSignedEncrypted_IssuerSerial_FromNetFx()
        {
            CheckSignedEncrypted(
                SignedDocuments.SignedCmsOverEnvelopedCms_IssuerSerial_NetFx,
                SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void CheckSignedEncrypted_SKID_FromNetFx()
        {
            CheckSignedEncrypted(
                SignedDocuments.SignedCmsOverEnvelopedCms_SKID_NetFx,
                SubjectIdentifierType.SubjectKeyIdentifier);
        }

        [Fact]
        public static void CheckSignedEncrypted_IssuerSerial_FromCoreFx()
        {
            CheckSignedEncrypted(
                SignedDocuments.SignedCmsOverEnvelopedCms_IssuerSerial_CoreFx,
                SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [Fact]
        public static void CheckSignedEncrypted_SKID_FromCoreFx()
        {
            CheckSignedEncrypted(
                SignedDocuments.SignedCmsOverEnvelopedCms_SKID_CoreFx,
                SubjectIdentifierType.SubjectKeyIdentifier);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void ReadAndWriteDocumentWithIndefiniteLengthContent(bool checkSignature)
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.IndefiniteLengthContentDocument);

            if (checkSignature)
            {
                cms.CheckSignature(true);
            }

            cms.Encode();
        }

        private static void CheckSignedEncrypted(byte[] docBytes, SubjectIdentifierType expectedType)
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(docBytes);

            Assert.Equal(Oids.Pkcs7Enveloped, signedCms.ContentInfo.ContentType.Value);

            SignerInfoCollection signers = signedCms.SignerInfos;
            Assert.Equal(1, signers.Count);
            Assert.Equal(expectedType, signers[0].SignerIdentifier.Type);

            // Assert.NotThrows
            signedCms.CheckSignature(true);

            EnvelopedCms envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(signedCms.ContentInfo.Content);

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                envelopedCms.Decrypt(new X509Certificate2Collection(cert));
            }

            Assert.Equal("42", envelopedCms.ContentInfo.Content.ByteArrayToHex());
        }

        [Fact]
        public static void CheckNoSignature_FromNetFx()
        {
            byte[] encoded = (
                "30819F06092A864886F70D010702A0819130818E020101310F300D0609608648" +
                "0165030402010500301406092A864886F70D010701A007040509080706053162" +
                "3060020101301C3017311530130603550403130C44756D6D79205369676E6572" +
                "020100300D06096086480165030402010500300C06082B060105050706020500" +
                "0420AF5F6F5C5967C377E49193ECA1EE0B98300A171CD3165C9A2410E8FB7C02" +
                "8674"
            ).HexToByteArray();

            CheckNoSignature(encoded);
        }

        [Fact]
        public static void CheckNoSignature_FromNetFx_TamperSignatureOid()
        {
            // CheckNoSignature_FromNetFx with the algorithm identifier changed from
            // 1.3.6.1.5.5.7.6.2 to 10.3.6.1.5.5.7.6.10
            byte[] encoded = (
                "30819F06092A864886F70D010702A0819130818E020101310F300D0609608648" +
                "0165030402010500301406092A864886F70D010701A007040509080706053162" +
                "3060020101301C3017311530130603550403130C44756D6D79205369676E6572" +
                "020100300D06096086480165030402010500300C06082B0601050507060A0500" +
                "0420AF5F6F5C5967C377E49193ECA1EE0B98300A171CD3165C9A2410E8FB7C02" +
                "8674"
            ).HexToByteArray();

            CheckNoSignature(encoded, badOid: true);
        }

        [Fact]
        public static void CheckNoSignature_FromCoreFx()
        {
            byte[] encoded = (
                "30819906092A864886F70D010702A0818B308188020101310D300B0609608648" +
                "016503040201301406092A864886F70D010701A00704050908070605315E305C" +
                "020101301C3017311530130603550403130C44756D6D79205369676E65720201" +
                "00300B0609608648016503040201300A06082B060105050706020420AF5F6F5C" +
                "5967C377E49193ECA1EE0B98300A171CD3165C9A2410E8FB7C028674"
            ).HexToByteArray();

            CheckNoSignature(encoded);
        }

        [Fact]
        public static void CheckNoSignature_FromCoreFx_TamperSignatureOid()
        {
            // CheckNoSignature_FromCoreFx with the algorithm identifier changed from
            // 1.3.6.1.5.5.7.6.2 to 10.3.6.1.5.5.7.6.10
            byte[] encoded = (
                "30819906092A864886F70D010702A0818B308188020101310D300B0609608648" +
                "016503040201301406092A864886F70D010701A00704050908070605315E305C" +
                "020101301C3017311530130603550403130C44756D6D79205369676E65720201" +
                "00300B0609608648016503040201300A06082B0601050507060A0420AF5F6F5C" +
                "5967C377E49193ECA1EE0B98300A171CD3165C9A2410E8FB7C028674"
            ).HexToByteArray();

            CheckNoSignature(encoded, badOid: true);
        }

        [Fact]
        public static void CheckNoSignature_FromCoreFx_TamperIssuerName()
        {
            // CheckNoSignature_FromCoreFx with the issuer name changed from "Dummy Cert"
            // to "Dumny Cert" (m => n / 0x6D => 0x6E)
            byte[] encoded = (
                "30819906092A864886F70D010702A0818B308188020101310D300B0609608648" +
                "016503040201301406092A864886F70D010701A00704050908070605315E305C" +
                "020101301C3017311530130603550403130C44756D6E79205369676E65720201" +
                "00300B0609608648016503040201300A06082B060105050706020420AF5F6F5C" +
                "5967C377E49193ECA1EE0B98300A171CD3165C9A2410E8FB7C028674"
            ).HexToByteArray();

            SignedCms cms = new SignedCms();
            cms.Decode(encoded);
            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Equal(1, signers.Count);
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, signers[0].SignerIdentifier.Type);
            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            Assert.ThrowsAny<CryptographicException>(() => signers[0].CheckSignature(true));

            // Assert.NoThrow
            cms.CheckHash();
            signers[0].CheckHash();
        }

        [Fact]
        public static void Decode_CanDecodeWithAttributeCertificate()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.TstWithAttributeCertificate);
            Assert.Equal(2, cms.Certificates.Count);
            cms.CheckSignature(verifySignatureOnly: true);
        }

        // Ed25519 certificate from https://datatracker.ietf.org/doc/html/rfc8410#section-10.2
        private const string UnknownAlgorithmCert =
            """
            MIIBLDCB36ADAgECAghWAUdKKo3DMDAFBgMrZXAwGTEXMBUGA1UEAwwOSUVURiBUZX
            N0IERlbW8wHhcNMTYwODAxMTIxOTI0WhcNNDAxMjMxMjM1OTU5WjAZMRcwFQYDVQQD
            DA5JRVRGIFRlc3QgRGVtbzAqMAUGAytlbgMhAIUg8AmJMKdUdIt93LQ+91oNvzoNJj
            ga9OukqY6qm05qo0UwQzAPBgNVHRMBAf8EBTADAQEAMA4GA1UdDwEBAAQEAwIDCDAg
            BgNVHQ4BAQAEFgQUmx9e7e0EM4Xk97xiPFl1uQvIuzswBQYDK2VwA0EAryMB/t3J5v
            /BzKc9dNZIpDmAgs3babFOTQbs+BolzlDUwsPrdGxO3YNGhW7Ibz3OGhhlxXrCe1Cg
            w1AH9efZBw==
            """;

        [Fact]
        public static void ComputeSignature_UnknownAlgorithm_NoSignature()
        {
            using X509Certificate2 cert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(UnknownAlgorithmCert));

            byte[] message = "Hello World!"u8.ToArray();
            SignedCms cms = new SignedCms(new ContentInfo(message));
            CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.NoSignature, cert)
            {
                DigestAlgorithm = new Oid(Oids.Sha256, Oids.Sha256)
            };

            cms.ComputeSignature(cmsSigner);

            // "NoSignature" OID
            Assert.Equal("1.3.6.1.5.5.7.6.2", cms.SignerInfos[0].SignatureAlgorithm.Value);

            byte[] messageHash = Convert.FromBase64String("f4OxZX/x/FO5LcGBSKHWXfwtSx+j1ncoSt3SABJtkGk=");
            byte[] signature = cms.SignerInfos[0].GetSignature();

            Assert.Equal(messageHash, signature);

            SignedCms cmsNoCert = new SignedCms(new ContentInfo(message));
            cmsNoCert.Decode(cms.Encode());

            // Assert.NoThrow
            cmsNoCert.SignerInfos[0].CheckHash();

            // Assert.NoThrow
            cmsNoCert.CheckHash();
        }

        [Fact]
        public static void ComputeSignature_Rsa_NoSignature_DefaultDigest()
        {
            ComputeSignature_NoSignature_DefaultDigest(Certificates.RSAKeyTransferCapi1.GetCertificate);
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void ComputeSignature_MLDsa_NoSignature_DefaultDigest()
        {
            ComputeSignature_NoSignature_DefaultDigest(Certificates.MLDsaIetf[MLDsaAlgorithm.MLDsa65].GetCertificate);
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void ComputeSignature_SlhDsa_NoSignature_DefaultDigest()
        {
            ComputeSignature_NoSignature_DefaultDigest(Certificates.SlhDsaSha2_128s_Ietf.GetCertificate);
        }

        private static void ComputeSignature_NoSignature_DefaultDigest(Func<X509Certificate2> getCert)
        {
            using X509Certificate2 cert = getCert();

            byte[] message = "Hello World!"u8.ToArray();
            SignedCms cms = new SignedCms(new ContentInfo(message));

            // Use default value for DigestAlgorithm
            CmsSigner signer = new CmsSigner(SubjectIdentifierType.NoSignature, cert);

            cms.ComputeSignature(signer);

            bool defaultHashIsSha1 = IsNetFramework471OrLower;
            byte[] expectedMessageHash = Convert.FromBase64String(
                defaultHashIsSha1
                    ? "Lve95gjOVATpfV8EL5X4nxwjKHE="                    // Sha1
                    : "f4OxZX/x/FO5LcGBSKHWXfwtSx+j1ncoSt3SABJtkGk=");  // Sha256

            Assert.Equal(expectedMessageHash, cms.SignerInfos[0].GetSignature());
        }

        private static void CheckNoSignature(byte[] encoded, bool badOid=false)
        {
            SignedCms cms = new SignedCms();
            cms.Decode(encoded);
            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Equal(1, signers.Count);
            Assert.Equal(SubjectIdentifierType.NoSignature, signers[0].SignerIdentifier.Type);
            Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));

            if (badOid)
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.CheckHash());
            }
            else
            {
                // Assert.NoThrow
                cms.CheckHash();
            }
        }

        [ConditionalFact(nameof(SupportsDraft10Pkcs8))]
        public static void ComputeSignature_MLDsa_DefaultDigest()
        {
#if !NETFRAMEWORK
            // Test signer with public certificate and private key
            AssertSignerHasCorrectDefaultDigest(
                useSigner =>
                {
                    using (X509Certificate2 cert = Certificates.MLDsaIetf[MLDsaAlgorithm.MLDsa65].GetCertificate())
                    using (MLDsa key = MLDsa.ImportMLDsaPrivateSeed(MLDsaAlgorithm.MLDsa65, MLDsaTestsData.IetfMLDsa65.PrivateSeed))
                    {
                        useSigner(new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key));
                    }
                });

            // Test signer with private certificate
            AssertSignerHasCorrectDefaultDigest(
                useSigner =>
                {
                    using (X509Certificate2 cert = Certificates.MLDsaIetf[MLDsaAlgorithm.MLDsa65].TryGetCertificateWithPrivateKey())
                    {
                        useSigner(new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, (MLDsa?)null));
                    }
                });
#endif

            // Test signer with private certificate
            AssertSignerHasCorrectDefaultDigest(
                useSigner =>
                {
                    using (X509Certificate2 cert = Certificates.MLDsaIetf[MLDsaAlgorithm.MLDsa65].TryGetCertificateWithPrivateKey())
                    {
                        useSigner(new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert));
                    }
                });
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void ComputeSignature_SlhDsa_DefaultDigest()
        {
#if !NETFRAMEWORK
            // Test signer with public certificate and private key
            AssertSignerHasCorrectDefaultDigest(
                useSigner =>
                {
                    using (X509Certificate2 cert = Certificates.SlhDsaSha2_128s_Ietf.GetCertificate())
                    using (SlhDsa key = SlhDsa.ImportSlhDsaPrivateKey(SlhDsaAlgorithm.SlhDsaSha2_128s, SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue))
                    {
                        useSigner(new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key));
                    }
                });

            // Test signer with private certificate
            AssertSignerHasCorrectDefaultDigest(
                useSigner =>
                {
                    using (X509Certificate2 cert = Certificates.SlhDsaSha2_128s_Ietf.TryGetCertificateWithPrivateKey())
                    {
                        useSigner(new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, (SlhDsa?)null));
                    }
                });
#endif

            // Test signer with private certificate
            AssertSignerHasCorrectDefaultDigest(
                useSigner =>
                {
                    using (X509Certificate2 cert = Certificates.SlhDsaSha2_128s_Ietf.TryGetCertificateWithPrivateKey())
                    {
                        useSigner(new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert));
                    }
                });
        }

        private static void AssertSignerHasCorrectDefaultDigest(Action<Action<CmsSigner>> test)
        {
            // DigestAlgorithm property on new signer has correct default value
            test(static signer => Assert.Equal(DefaultHashForPlatform.Value, signer.DigestAlgorithm.Value));

            // Signer signs with correct digest value
            test(
                static signer =>
                {
                    byte[] message = "Hello World!"u8.ToArray();
                    SignedCms cms = new SignedCms(new ContentInfo(message));

                    cms.ComputeSignature(signer);

                    Assert.Equal(DefaultHashForPlatform.Value, cms.SignerInfos[0].DigestAlgorithm.Value);

                    // Assert.NoThrow
                    cms.SignerInfos[0].CheckSignature(verifySignatureOnly: true);
                });
        }

        private static bool IsNetFramework471OrLower { get; } =
            PlatformDetection.IsNetFramework && new FrameworkName(AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName).Version <= new Version(4, 7, 1);

        private static Oid DefaultHashForPlatform = IsNetFramework471OrLower ? new Oid(Oids.Sha1, Oids.Sha1) : new Oid(Oids.Sha256, Oids.Sha256);
    }
}
