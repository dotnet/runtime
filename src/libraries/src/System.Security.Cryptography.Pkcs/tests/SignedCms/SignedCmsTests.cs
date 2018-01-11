// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class SignedCmsTests
    {
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

        [Fact]
        public static void CheckSignature_ExtraStore_IsAdditional()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            // Assert.NotThrows
            cms.CheckSignature(true);

            // Assert.NotThrows
            cms.CheckSignature(new X509Certificate2Collection(), true);
        }

        [Fact]
        public static void Decode_IgnoresExtraData()
        {
            byte[] basis = SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber;
            byte[] data = new byte[basis.Length + 60];
            data.AsSpan().Slice(basis.Length).Fill(0x5E);
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

            Assert.Equal(null, ex.ActualValue);
            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);

            ex = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "index",
                () => cms.RemoveSignature(1));

            Assert.Equal(null, ex.ActualValue);

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
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, false)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, true)]
        public static void AddFirstSigner_RSA(SubjectIdentifierType identifierType, bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            using (X509Certificate2 signerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(identifierType, signerCert);
                cms.ComputeSignature(signer);
            }

            Assert.Same(contentInfo.Content, cms.ContentInfo.Content);
            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);

            int expectedVersion = identifierType == SubjectIdentifierType.SubjectKeyIdentifier ? 3 : 1;
            Assert.Equal(expectedVersion, cms.Version);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Equal(identifierType, firstSigner.SignerIdentifier.Type);
            Assert.NotNull(firstSigner.Certificate);
            Assert.NotSame(cms.Certificates[0], firstSigner.Certificate);
            Assert.Equal(cms.Certificates[0], firstSigner.Certificate);

            cms.CheckSignature(true);
            byte[] encoded = cms.Encode();

            cms = new SignedCms();
            cms.Decode(encoded);
            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);
            Assert.Equal(expectedVersion, cms.Version);
            Assert.Equal(identifierType, cms.SignerInfos[0].SignerIdentifier.Type);
            Assert.Equal(firstSigner.Certificate, cms.SignerInfos[0].Certificate);

            if (detached)
            {
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
                cms = new SignedCms(contentInfo, detached);
                cms.Decode(encoded);
            }

            cms.CheckSignature(true);
        }

        [Theory]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, false)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, true)]
        public static void AddFirstSigner_DSA(SubjectIdentifierType identifierType, bool detached)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            using (X509Certificate2 signerCert = Certificates.Dsa1024.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(identifierType, signerCert);
                signer.IncludeOption = X509IncludeOption.EndCertOnly;
                // Best compatibility for DSA is SHA-1 (FIPS 186-2)
                signer.DigestAlgorithm = new Oid(Oids.Sha1, Oids.Sha1);
                cms.ComputeSignature(signer);
            }

            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);

            int expectedVersion = identifierType == SubjectIdentifierType.SubjectKeyIdentifier ? 3 : 1;
            Assert.Equal(expectedVersion, cms.Version);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Equal(identifierType, firstSigner.SignerIdentifier.Type);
            Assert.NotNull(firstSigner.Certificate);
            Assert.NotSame(cms.Certificates[0], firstSigner.Certificate);
            Assert.Equal(cms.Certificates[0], firstSigner.Certificate);

#if netcoreapp
            byte[] signature = firstSigner.GetSignature();
            Assert.NotEmpty(signature);
            // DSA PKIX signature format is a DER SEQUENCE.
            Assert.Equal(0x30, signature[0]);
#endif

            cms.CheckSignature(true);
            byte[] encoded = cms.Encode();

            cms = new SignedCms();
            cms.Decode(encoded);

            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);
            Assert.Equal(expectedVersion, cms.Version);
            Assert.Equal(identifierType, cms.SignerInfos[0].SignerIdentifier.Type);
            Assert.Equal(firstSigner.Certificate, cms.SignerInfos[0].Certificate);

#if netcoreapp
            byte[] sig2 = cms.SignerInfos[0].GetSignature();
            Assert.Equal(signature, sig2);
#endif

            if (detached)
            {
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
                cms = new SignedCms(contentInfo, detached);
                cms.Decode(encoded);
            }

            cms.CheckSignature(true);
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
            ContentInfo contentInfo = new ContentInfo(new byte[] { 9, 8, 7, 6, 5 });
            SignedCms cms = new SignedCms(contentInfo, detached);

            using (X509Certificate2 signerCert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(identifierType, signerCert);
                signer.IncludeOption = X509IncludeOption.EndCertOnly;
                signer.DigestAlgorithm = new Oid(digestOid, digestOid);
                cms.ComputeSignature(signer);
            }

            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);

            int expectedVersion = identifierType == SubjectIdentifierType.SubjectKeyIdentifier ? 3 : 1;
            Assert.Equal(expectedVersion, cms.Version);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Equal(identifierType, firstSigner.SignerIdentifier.Type);
            Assert.NotNull(firstSigner.Certificate);
            Assert.NotSame(cms.Certificates[0], firstSigner.Certificate);
            Assert.Equal(cms.Certificates[0], firstSigner.Certificate);

#if netcoreapp
            byte[] signature = firstSigner.GetSignature();
            Assert.NotEmpty(signature);
            // ECDSA PKIX signature format is a DER SEQUENCE.
            Assert.Equal(0x30, signature[0]);

            // ECDSA Oids are all under 1.2.840.10045.4.
            Assert.StartsWith("1.2.840.10045.4.", firstSigner.SignatureAlgorithm.Value);
#endif

            cms.CheckSignature(true);
            byte[] encoded = cms.Encode();

            cms = new SignedCms();
            cms.Decode(encoded);

            Assert.Single(cms.SignerInfos);
            Assert.Single(cms.Certificates);
            Assert.Equal(expectedVersion, cms.Version);
            Assert.Equal(identifierType, cms.SignerInfos[0].SignerIdentifier.Type);
            Assert.Equal(firstSigner.Certificate, cms.SignerInfos[0].Certificate);

#if netcoreapp
            byte[] sig2 = cms.SignerInfos[0].GetSignature();
            Assert.Equal(signature, sig2);
#endif

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
#if netfx
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
#if netfx
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
                cms.Decode(SignedDocuments.OneDsa1024);
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
#if !netfx
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
                    // These indicies are manually computable by observing the certificate sizes.
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

        [Fact]
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
            contentInfo.ContentType.Value = Oids.Pkcs7Hashed;
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
    }
}
