// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static partial class SignerInfoTests
    {
        [Fact]
        public static void SignerInfo_SignedAttributes_Cached_WhenEmpty()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            SignerInfo signer = cms.SignerInfos[0];

            CryptographicAttributeObjectCollection attrs = signer.SignedAttributes;
            CryptographicAttributeObjectCollection attrs2 = signer.SignedAttributes;
            Assert.Same(attrs, attrs2);
            Assert.Empty(attrs);
        }

        [Fact]
        public static void SignerInfo_SignedAttributes_Cached_WhenNonEmpty()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPssDocument);
            SignerInfo signer = cms.SignerInfos[0];

            CryptographicAttributeObjectCollection attrs = signer.SignedAttributes;
            CryptographicAttributeObjectCollection attrs2 = signer.SignedAttributes;
            Assert.Same(attrs, attrs2);
            Assert.Equal(4, attrs.Count);
        }

        [Fact]
        public static void SignerInfo_UnsignedAttributes_Cached_WhenEmpty()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            SignerInfo signer = cms.SignerInfos[0];

            CryptographicAttributeObjectCollection attrs = signer.UnsignedAttributes;
            CryptographicAttributeObjectCollection attrs2 = signer.UnsignedAttributes;
            Assert.Same(attrs, attrs2);
            Assert.Empty(attrs);
            Assert.Empty(attrs2);
        }

        [Fact]
        public static void SignerInfo_UnsignedAttributes_Cached_WhenNonEmpty()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);
            SignerInfo signer = cms.SignerInfos[0];

            CryptographicAttributeObjectCollection attrs = signer.UnsignedAttributes;
            CryptographicAttributeObjectCollection attrs2 = signer.UnsignedAttributes;
            Assert.Same(attrs, attrs2);
            Assert.Single(attrs);
        }

        [Fact]
        public static void SignerInfo_CounterSignerInfos_UniquePerCall_WhenEmpty()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            SignerInfo signer = cms.SignerInfos[0];

            SignerInfoCollection counterSigners = signer.CounterSignerInfos;
            SignerInfoCollection counterSigners2 = signer.CounterSignerInfos;
            Assert.NotSame(counterSigners, counterSigners2);
            Assert.Empty(counterSigners);
            Assert.Empty(counterSigners2);
        }

        [Fact]
        public static void SignerInfo_CounterSignerInfos_UniquePerCall_WhenNonEmpty()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);
            SignerInfo signer = cms.SignerInfos[0];

            SignerInfoCollection counterSigners = signer.CounterSignerInfos;
            SignerInfoCollection counterSigners2 = signer.CounterSignerInfos;
            Assert.NotSame(counterSigners, counterSigners2);
            Assert.Single(counterSigners);
            Assert.Single(counterSigners2);

            for (int i = 0; i < counterSigners.Count; i++)
            {
                SignerInfo counterSigner = counterSigners[i];
                SignerInfo counterSigner2 = counterSigners2[i];

                Assert.NotSame(counterSigner, counterSigner2);
                Assert.NotSame(counterSigner.Certificate, counterSigner2.Certificate);
                Assert.Equal(counterSigner.Certificate, counterSigner2.Certificate);

#if NETCOREAPP
                byte[] signature = counterSigner.GetSignature();
                byte[] signature2 = counterSigner2.GetSignature();

                Assert.NotSame(signature, signature2);
                Assert.Equal(signature, signature2);
#endif
            }
        }

#if NETCOREAPP
        [Fact]
        public static void SignerInfo_GetSignature_UniquePerCall()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);
            SignerInfo signer = cms.SignerInfos[0];

            byte[] signature = signer.GetSignature();
            byte[] signature2 = signer.GetSignature();

            Assert.NotSame(signature, signature2);
            Assert.Equal(signature, signature2);
        }
#endif

        [Fact]
        public static void SignerInfo_DigestAlgorithm_NotSame()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);
            SignerInfo signer = cms.SignerInfos[0];

            Oid oid = signer.DigestAlgorithm;
            Oid oid2 = signer.DigestAlgorithm;

            Assert.NotSame(oid, oid2);
        }

#if NETCOREAPP
        [Fact]
        public static void SignerInfo_SignatureAlgorithm_NotSame()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);
            SignerInfo signer = cms.SignerInfos[0];

            Oid oid = signer.SignatureAlgorithm;
            Oid oid2 = signer.SignatureAlgorithm;

            Assert.NotSame(oid, oid2);
        }
#endif

        [Fact]
        public static void SignerInfo_Certificate_Same()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);
            SignerInfo signer = cms.SignerInfos[0];

            X509Certificate2 cert = signer.Certificate;
            X509Certificate2 cert2 = signer.Certificate;

            Assert.Same(cert, cert2);
        }

        [Fact]
        public static void CheckSignature_ThrowsOnNullStore()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPssDocument);
            SignerInfo signer = cms.SignerInfos[0];

            AssertExtensions.Throws<ArgumentNullException>(
                "extraStore",
                () => signer.CheckSignature(null, true));

            AssertExtensions.Throws<ArgumentNullException>(
                "extraStore",
                () => signer.CheckSignature(null, false));
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void CheckSignature_ExtraStore_IsAdditional()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            SignerInfo signer = cms.SignerInfos[0];
            Assert.NotNull(signer.Certificate);

            // Assert.NotThrows
            signer.CheckSignature(true);

            // Assert.NotThrows
            signer.CheckSignature(new X509Certificate2Collection(), true);
        }

        [Fact]
        public static void CheckSignature_MD5WithRSA()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.MD5WithRSADigestAlgorithm);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Equal(Oids.RsaPkcs1Md5, signer.DigestAlgorithm.Value);
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            //Assert.NotThrows
            signer.CheckSignature(true);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void CheckSignature_SHA1WithRSA()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.SHA1WithRSADigestAlgorithm);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Equal(Oids.RsaPkcs1Sha1, signer.DigestAlgorithm.Value);
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            //Assert.NotThrows
            signer.CheckSignature(true);
        }

        [Fact]
        public static void CheckSignature_SHA256WithRSA()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.SHA256WithRSADigestAlgorithm);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Equal(Oids.RsaPkcs1Sha256, signer.DigestAlgorithm.Value);
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            //Assert.NotThrows
            signer.CheckSignature(true);
        }

        [Fact]
        public static void CheckSignature_SHA384WithRSA()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.SHA384WithRSADigestAlgorithm);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Equal(Oids.RsaPkcs1Sha384, signer.DigestAlgorithm.Value);
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            //Assert.NotThrows
            signer.CheckSignature(true);
        }

        [Fact]
        public static void CheckSignature_SHA512WithRSA()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.SHA512WithRSADigestAlgorithm);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Equal(Oids.RsaPkcs1Sha512, signer.DigestAlgorithm.Value);
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            //Assert.NotThrows
            signer.CheckSignature(true);
        }

        [Fact]
        public static void CheckSignature_SHA256WithRSADigest_And_RSA256WithRSASignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.SHA256withRSADigestAndSHA256WithRSASignatureAlgorithm);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Equal(Oids.RsaPkcs1Sha256, signer.DigestAlgorithm.Value);
            Assert.Equal(Oids.RsaPkcs1Sha256, signer.SignatureAlgorithm.Value);

            //Assert.NotThrows
            signer.CheckSignature(true);
        }

        [Fact]
        public static void CheckSignature_ECDSA256SignedWithRSASha256HashIdentifier()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.SHA256ECDSAWithRsaSha256DigestIdentifier);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Equal(Oids.RsaPkcs1Sha256, signer.DigestAlgorithm.Value);
            Assert.Equal(Oids.EcdsaSha256, signer.SignatureAlgorithm.Value);

            // Assert.NotThrows
            signer.CheckSignature(true);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetFx bug in matching logic")]
        public static void RemoveCounterSignature_MatchesIssuerAndSerialNumber()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signerInfo = cms.SignerInfos[0];
            SignerInfo counterSigner = signerInfo.CounterSignerInfos[1];

            Assert.Equal(
                SubjectIdentifierType.IssuerAndSerialNumber,
                counterSigner.SignerIdentifier.Type);

            int countBefore = cms.Certificates.Count;
            Assert.NotEqual(signerInfo.Certificate, counterSigner.Certificate);

            signerInfo.RemoveCounterSignature(counterSigner);
            Assert.Single(cms.SignerInfos);

            // Removing a CounterSigner doesn't update the current object, it updates
            // the underlying SignedCms object, and a new signer has to be retrieved.
            Assert.Equal(2, signerInfo.CounterSignerInfos.Count);
            Assert.Single(cms.SignerInfos[0].CounterSignerInfos);

            Assert.Equal(countBefore, cms.Certificates.Count);

            // Assert.NotThrows
            cms.CheckSignature(true);
            cms.CheckHash();
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetFx bug in matching logic")]
        public static void RemoveCounterSignature_MatchesSubjectKeyIdentifier()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signerInfo = cms.SignerInfos[0];
            SignerInfo counterSigner = signerInfo.CounterSignerInfos[0];

            Assert.Equal(
                SubjectIdentifierType.SubjectKeyIdentifier,
                counterSigner.SignerIdentifier.Type);

            int countBefore = cms.Certificates.Count;
            Assert.Equal(signerInfo.Certificate, counterSigner.Certificate);

            signerInfo.RemoveCounterSignature(counterSigner);
            Assert.Single(cms.SignerInfos);

            // Removing a CounterSigner doesn't update the current object, it updates
            // the underlying SignedCms object, and a new signer has to be retrieved.
            Assert.Equal(2, signerInfo.CounterSignerInfos.Count);
            Assert.Single(cms.SignerInfos[0].CounterSignerInfos);

            // This certificate is still in use, since we counter-signed ourself,
            // and the remaining countersigner is us.
            Assert.Equal(countBefore, cms.Certificates.Count);

            // Assert.NotThrows
            cms.CheckSignature(true);
            cms.CheckHash();
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetFx bug in matching logic")]
        public static void RemoveCounterSignature_MatchesNoSignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1CounterSignedWithNoSignature);
            SignerInfo signerInfo = cms.SignerInfos[0];
            SignerInfo counterSigner = signerInfo.CounterSignerInfos[0];

            Assert.Single(signerInfo.CounterSignerInfos);
            Assert.Equal(SubjectIdentifierType.NoSignature, counterSigner.SignerIdentifier.Type);

            int countBefore = cms.Certificates.Count;

            // cms.CheckSignature fails because there's a NoSignature countersigner:
            Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));

            signerInfo.RemoveCounterSignature(counterSigner);

            // Removing a CounterSigner doesn't update the current object, it updates
            // the underlying SignedCms object, and a new signer has to be retrieved.
            Assert.Single(signerInfo.CounterSignerInfos);
            Assert.Empty(cms.SignerInfos[0].CounterSignerInfos);

            // This certificate is still in use, since we counter-signed ourself,
            // and the remaining countersigner is us.
            Assert.Equal(countBefore, cms.Certificates.Count);

            // And we succeed now, because we got rid of the NoSignature signer.
            cms.CheckSignature(true);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetFx bug in matching logic")]
        public static void RemoveCounterSignature_UsesLiveState()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signerInfo = cms.SignerInfos[0];
            SignerInfo counterSigner = signerInfo.CounterSignerInfos[0];

            Assert.Equal(
                SubjectIdentifierType.SubjectKeyIdentifier,
                counterSigner.SignerIdentifier.Type);

            int countBefore = cms.Certificates.Count;
            Assert.Equal(signerInfo.Certificate, counterSigner.Certificate);

            signerInfo.RemoveCounterSignature(counterSigner);
            Assert.Single(cms.SignerInfos);

            // Removing a CounterSigner doesn't update the current object, it updates
            // the underlying SignedCms object, and a new signer has to be retrieved.
            Assert.Equal(2, signerInfo.CounterSignerInfos.Count);
            Assert.Single(cms.SignerInfos[0].CounterSignerInfos);
            Assert.Equal(countBefore, cms.Certificates.Count);

            // Even though the CounterSignerInfos collection still contains this, the live
            // document doesn't.
            Assert.Throws<CryptographicException>(
                () => signerInfo.RemoveCounterSignature(counterSigner));

            // Assert.NotThrows
            cms.CheckSignature(true);
            cms.CheckHash();
        }

        [Fact]
        public static void RemoveCounterSignature_WithNoMatch()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signerInfo = cms.SignerInfos[0];

            // Even though we counter-signed ourself, the counter-signer version of us
            // is SubjectKeyIdentifier, and we're IssuerAndSerialNumber, so no match.
            Assert.Throws<CryptographicException>(
                () => signerInfo.RemoveCounterSignature(signerInfo));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetFx bug")]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void RemoveCounterSignature_EncodedInSingleAttribute(int indexToRemove)
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1TwoCounterSignaturesInSingleAttribute);
            SignerInfo signerInfo = cms.SignerInfos[0];

            Assert.Equal(2, signerInfo.CounterSignerInfos.Count);
            signerInfo.RemoveCounterSignature(indexToRemove);
            Assert.Equal(1, signerInfo.CounterSignerInfos.Count);

            cms.CheckSignature(true);
        }

        [Fact]
        public static void RemoveCounterSignature_Null()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos.Count);

            AssertExtensions.Throws<ArgumentNullException>(
                "counterSignerInfo",
                () => cms.SignerInfos[0].RemoveCounterSignature(null));

            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos.Count);
        }

        [Fact]
        public static void RemoveCounterSignature_Negative()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signer = cms.SignerInfos[0];

            ArgumentOutOfRangeException ex = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "index",
                "childIndex",
                () => signer.RemoveCounterSignature(-1));

            Assert.Null(ex.ActualValue);
        }

        [Fact]
        public static void RemoveCounterSignature_TooBigByValue()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signer = cms.SignerInfos[0];

            Assert.Throws<CryptographicException>(
                () => signer.RemoveCounterSignature(2));

            signer.RemoveCounterSignature(1);
            Assert.Equal(2, signer.CounterSignerInfos.Count);

            Assert.Throws<CryptographicException>(
                () => signer.RemoveCounterSignature(1));
        }

        [Fact]
        public static void RemoveCounterSignature_TooBigByValue_Past0()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signer = cms.SignerInfos[0];

            signer.RemoveCounterSignature(0);
            signer.RemoveCounterSignature(0);
            Assert.Equal(2, signer.CounterSignerInfos.Count);

            Assert.Throws<CryptographicException>(
                () => signer.RemoveCounterSignature(0));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetFx bug in matching logic")]
        public static void RemoveCounterSignature_TooBigByMatch()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signer = cms.SignerInfos[0];
            SignerInfo counterSigner = signer.CounterSignerInfos[1];

            // This succeeds, but reduces the real count to 1.
            signer.RemoveCounterSignature(counterSigner);
            Assert.Equal(2, signer.CounterSignerInfos.Count);
            Assert.Single(cms.SignerInfos[0].CounterSignerInfos);

            Assert.Throws<CryptographicException>(
                () => signer.RemoveCounterSignature(counterSigner));
        }

        [Fact]
        public static void RemoveCounterSignature_BySignerInfo_OnRemovedSigner()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signer = cms.SignerInfos[0];
            SignerInfo counterSigner = signer.CounterSignerInfos[0];

            cms.RemoveSignature(signer);
            Assert.NotEmpty(signer.CounterSignerInfos);

            Assert.Throws<CryptographicException>(
                () => signer.RemoveCounterSignature(counterSigner));
        }

        [Fact]
        public static void RemoveCounterSignature_ByIndex_OnRemovedSigner()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);
            SignerInfo signer = cms.SignerInfos[0];

            cms.RemoveSignature(signer);
            Assert.NotEmpty(signer.CounterSignerInfos);

            Assert.Throws<CryptographicException>(
                () => signer.RemoveCounterSignature(0));
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void AddCounterSigner_DuplicateCert_RSA()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            Assert.Single(cms.Certificates);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            using (X509Certificate2 signerCert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signerCert);
                firstSigner.ComputeCounterSignature(signer);
            }

            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            SignerInfo firstSigner2 = cms.SignerInfos[0];
            Assert.Single(firstSigner2.CounterSignerInfos);
            Assert.Single(firstSigner2.UnsignedAttributes);

            SignerInfo counterSigner = firstSigner2.CounterSignerInfos[0];

            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, counterSigner.SignerIdentifier.Type);

            // On .NET Framework there will be two attributes, because Windows emits the
            // content-type attribute even for counter-signers.
            int expectedAttrCount = 1;
            // One of them is a V3 signer.
#if NETFRAMEWORK
            expectedAttrCount = 2;
#endif
            Assert.Equal(expectedAttrCount, counterSigner.SignedAttributes.Count);
            Assert.Equal(Oids.MessageDigest, counterSigner.SignedAttributes[expectedAttrCount - 1].Oid.Value);

            Assert.Equal(firstSigner2.Certificate, counterSigner.Certificate);
            Assert.Single(cms.Certificates);

            counterSigner.CheckSignature(true);
            firstSigner2.CheckSignature(true);
            cms.CheckSignature(true);
        }

        [ConditionalTheory(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier)]
        public static void AddCounterSigner_RSA(SubjectIdentifierType identifierType)
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            Assert.Single(cms.Certificates);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            using (X509Certificate2 signerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(identifierType, signerCert);
                firstSigner.ComputeCounterSignature(signer);
            }

            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            SignerInfo firstSigner2 = cms.SignerInfos[0];
            Assert.Single(firstSigner2.CounterSignerInfos);
            Assert.Single(firstSigner2.UnsignedAttributes);

            SignerInfo counterSigner = firstSigner2.CounterSignerInfos[0];

            Assert.Equal(identifierType, counterSigner.SignerIdentifier.Type);

            // On .NET Framework there will be two attributes, because Windows emits the
            // content-type attribute even for counter-signers.
            int expectedCount = 1;
#if NETFRAMEWORK
            expectedCount = 2;
#endif
            Assert.Equal(expectedCount, counterSigner.SignedAttributes.Count);
            Assert.Equal(Oids.MessageDigest, counterSigner.SignedAttributes[expectedCount - 1].Oid.Value);

            Assert.NotEqual(firstSigner2.Certificate, counterSigner.Certificate);
            Assert.Equal(2, cms.Certificates.Count);

            counterSigner.CheckSignature(true);
            firstSigner2.CheckSignature(true);
            cms.CheckSignature(true);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not supported by crypt32")]
        public static void AddCounterSignerToUnsortedAttributeSignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.DigiCertTimeStampToken);

            // Assert.NoThrows
            cms.CheckSignature(true);

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Equal(1, signers.Count);
            SignerInfo signerInfo = signers[0];

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                signerInfo.ComputeCounterSignature(
                    new CmsSigner(
                        SubjectIdentifierType.IssuerAndSerialNumber,
                        cert));

                signerInfo.ComputeCounterSignature(
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

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void AddCounterSigner_DSA()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            Assert.Single(cms.Certificates);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            using (X509Certificate2 signerCert = Certificates.Dsa1024.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signerCert);
                signer.IncludeOption = X509IncludeOption.EndCertOnly;
                // Best compatibility for DSA is SHA-1 (FIPS 186-2)
                signer.DigestAlgorithm = new Oid(Oids.Sha1, Oids.Sha1);
                firstSigner.ComputeCounterSignature(signer);
            }

            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            SignerInfo firstSigner2 = cms.SignerInfos[0];
            Assert.Single(firstSigner2.CounterSignerInfos);
            Assert.Single(firstSigner2.UnsignedAttributes);

            Assert.Single(cms.SignerInfos);
            Assert.Equal(2, cms.Certificates.Count);

            SignerInfo counterSigner = firstSigner2.CounterSignerInfos[0];

            Assert.Equal(1, counterSigner.Version);

            // On .NET Framework there will be two attributes, because Windows emits the
            // content-type attribute even for counter-signers.
            int expectedCount = 1;
#if NETFRAMEWORK
            expectedCount = 2;
#endif
            Assert.Equal(expectedCount, counterSigner.SignedAttributes.Count);
            Assert.Equal(Oids.MessageDigest, counterSigner.SignedAttributes[expectedCount - 1].Oid.Value);

            Assert.NotEqual(firstSigner2.Certificate, counterSigner.Certificate);
            Assert.Equal(2, cms.Certificates.Count);

#if NETCOREAPP
            byte[] signature = counterSigner.GetSignature();
            Assert.NotEmpty(signature);
            // DSA PKIX signature format is a DER SEQUENCE.
            Assert.Equal(0x30, signature[0]);
#endif

            cms.CheckSignature(true);
            byte[] encoded = cms.Encode();
            cms.Decode(encoded);
            cms.CheckSignature(true);
        }

        [ConditionalTheory(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, Oids.Sha1)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, Oids.Sha1)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, Oids.Sha256)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, Oids.Sha256)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, Oids.Sha384)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, Oids.Sha384)]
        [InlineData(SubjectIdentifierType.IssuerAndSerialNumber, Oids.Sha512)]
        [InlineData(SubjectIdentifierType.SubjectKeyIdentifier, Oids.Sha512)]
        public static void AddCounterSigner_ECDSA(SubjectIdentifierType identifierType, string digestOid)
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);
            Assert.Single(cms.Certificates);

            SignerInfo firstSigner = cms.SignerInfos[0];
            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            using (X509Certificate2 signerCert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(identifierType, signerCert);
                signer.IncludeOption = X509IncludeOption.EndCertOnly;
                signer.DigestAlgorithm = new Oid(digestOid, digestOid);
                firstSigner.ComputeCounterSignature(signer);
            }

            Assert.Empty(firstSigner.CounterSignerInfos);
            Assert.Empty(firstSigner.UnsignedAttributes);

            SignerInfo firstSigner2 = cms.SignerInfos[0];
            Assert.Single(firstSigner2.CounterSignerInfos);
            Assert.Single(firstSigner2.UnsignedAttributes);

            Assert.Single(cms.SignerInfos);
            Assert.Equal(2, cms.Certificates.Count);

            SignerInfo counterSigner = firstSigner2.CounterSignerInfos[0];

            int expectedVersion = identifierType == SubjectIdentifierType.IssuerAndSerialNumber ? 1 : 3;
            Assert.Equal(expectedVersion, counterSigner.Version);

            // On .NET Framework there will be two attributes, because Windows emits the
            // content-type attribute even for counter-signers.
            int expectedCount = 1;
#if NETFRAMEWORK
            expectedCount = 2;
#endif
            Assert.Equal(expectedCount, counterSigner.SignedAttributes.Count);
            Assert.Equal(Oids.MessageDigest, counterSigner.SignedAttributes[expectedCount - 1].Oid.Value);

            Assert.NotEqual(firstSigner2.Certificate, counterSigner.Certificate);
            Assert.Equal(2, cms.Certificates.Count);

#if NETCOREAPP
            byte[] signature = counterSigner.GetSignature();
            Assert.NotEmpty(signature);
            // DSA PKIX signature format is a DER SEQUENCE.
            Assert.Equal(0x30, signature[0]);

            // ECDSA Oids are all under 1.2.840.10045.4.
            Assert.StartsWith("1.2.840.10045.4.", counterSigner.SignatureAlgorithm.Value);
#endif

            cms.CheckSignature(true);
            byte[] encoded = cms.Encode();
            cms.Decode(encoded);
            cms.CheckSignature(true);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void AddFirstCounterSigner_NoSignature_NoPrivateKey()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            SignerInfo firstSigner = cms.SignerInfos[0];

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                Action sign = () =>
                    firstSigner.ComputeCounterSignature(
                        new CmsSigner(
                            SubjectIdentifierType.NoSignature,
                            cert)
                        {
                            IncludeOption = X509IncludeOption.None,
                        });

                if (PlatformDetection.IsNetFramework)
                {
                    Assert.ThrowsAny<CryptographicException>(sign);
                }
                else
                {
                    sign();
                    cms.CheckHash();
                    Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
                    firstSigner.CheckSignature(true);
                }
            }
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void AddFirstCounterSigner_NoSignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            SignerInfo firstSigner = cms.SignerInfos[0];

            // A certificate shouldn't really be required here, but on .NET Framework
            // it will prompt for the counter-signer's certificate if it's null,
            // even if the signature type is NoSignature.
            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                firstSigner.ComputeCounterSignature(
                    new CmsSigner(
                        SubjectIdentifierType.NoSignature,
                        cert)
                    {
                        IncludeOption = X509IncludeOption.None,
                    });
            }

            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            cms.CheckHash();

            byte[] encoded = cms.Encode();
            cms = new SignedCms();
            cms.Decode(encoded);
            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            cms.CheckHash();

            firstSigner = cms.SignerInfos[0];
            firstSigner.CheckSignature(verifySignatureOnly: true);
            Assert.ThrowsAny<CryptographicException>(() => firstSigner.CheckHash());

            SignerInfo firstCounterSigner = firstSigner.CounterSignerInfos[0];
            Assert.ThrowsAny<CryptographicException>(() => firstCounterSigner.CheckSignature(true));

            if (PlatformDetection.IsNetFramework)
            {
                // NetFX's CheckHash only looks at top-level SignerInfos to find the
                // crypt32 CMS signer ID, so it fails on any check from a countersigner.
                Assert.ThrowsAny<CryptographicException>(() => firstCounterSigner.CheckHash());
            }
            else
            {
                firstCounterSigner.CheckHash();
            }
        }

        [ConditionalTheory(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddSecondCounterSignature_NoSignature_WithCert(bool addExtraCert)
        {
            AddSecondCounterSignature_NoSignature(withCertificate: true, addExtraCert);
        }

        [ConditionalTheory(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        // On .NET Framework it will prompt for the counter-signer's certificate if it's null,
        // even if the signature type is NoSignature, so don't run the test there.
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddSecondCounterSignature_NoSignature_WithoutCert(bool addExtraCert)
        {
            AddSecondCounterSignature_NoSignature(withCertificate: false, addExtraCert);
        }

        private static void AddSecondCounterSignature_NoSignature(bool withCertificate, bool addExtraCert)
        {
            X509Certificate2Collection certs;
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            SignerInfo firstSigner = cms.SignerInfos[0];

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            using (X509Certificate2 cert2 = Certificates.DHKeyAgree1.GetCertificate())
            {
                firstSigner.ComputeCounterSignature(
                    new CmsSigner(cert)
                    {
                        IncludeOption = X509IncludeOption.None,
                    });

                CmsSigner counterSigner;

                if (withCertificate)
                {
                    counterSigner = new CmsSigner(SubjectIdentifierType.NoSignature, cert);
                }
                else
                {
                    counterSigner = new CmsSigner(SubjectIdentifierType.NoSignature);
                }

                if (addExtraCert)
                {
                    counterSigner.Certificates.Add(cert2);
                }

                firstSigner.ComputeCounterSignature(counterSigner);

                certs = cms.Certificates;

                if (addExtraCert)
                {
                    Assert.Equal(2, certs.Count);
                    Assert.NotEqual(cert2.RawData, certs[0].RawData);
                    Assert.Equal(cert2.RawData, certs[1].RawData);
                }
                else
                {
                    Assert.Equal(1, certs.Count);
                    Assert.NotEqual(cert2.RawData, certs[0].RawData);
                }
            }

            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            cms.CheckHash();

            byte[] encoded = cms.Encode();
            cms = new SignedCms();
            cms.Decode(encoded);
            Assert.ThrowsAny<CryptographicException>(() => cms.CheckSignature(true));
            cms.CheckHash();

            firstSigner = cms.SignerInfos[0];
            firstSigner.CheckSignature(verifySignatureOnly: true);
            Assert.ThrowsAny<CryptographicException>(() => firstSigner.CheckHash());

            // The NoSignature CounterSigner sorts first.
            SignerInfo firstCounterSigner = firstSigner.CounterSignerInfos[0];
            Assert.Equal(SubjectIdentifierType.NoSignature, firstCounterSigner.SignerIdentifier.Type);
            Assert.ThrowsAny<CryptographicException>(() => firstCounterSigner.CheckSignature(true));

            if (PlatformDetection.IsNetFramework)
            {
                // NetFX's CheckHash only looks at top-level SignerInfos to find the
                // crypt32 CMS signer ID, so it fails on any check from a countersigner.
                Assert.ThrowsAny<CryptographicException>(() => firstCounterSigner.CheckHash());
            }
            else
            {
                firstCounterSigner.CheckHash();
            }

            certs = cms.Certificates;

            if (addExtraCert)
            {
                Assert.Equal(2, certs.Count);
                Assert.Equal("CN=DfHelleKeyAgreement1", certs[1].SubjectName.Name);
            }
            else
            {
                Assert.Equal(1, certs.Count);
            }

            Assert.Equal("CN=RSAKeyTransferCapi1", certs[0].SubjectName.Name);
        }

        [Fact]
        public static void EnsureExtraCertsAdded()
        {
            SignedCms cms = new SignedCms();

            // DSA is not supported on mobile Apple platforms, so use ECDsa signed document instead
            if (PlatformDetection.UsesMobileAppleCrypto)
            {
                cms.Decode(SignedDocuments.SHA256ECDSAWithRsaSha256DigestIdentifier);
            }
            else
            {
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
                cms.SignerInfos[0].ComputeCounterSignature(signer);

                bool ExpectCopyRemoved =
#if NETFRAMEWORK
                    false
#else
                    true
#endif
                    ;

                int expectedAddedCount = 4;

                if (!ExpectCopyRemoved)
                {
                    expectedAddedCount++;
                }

                // Since adding a counter-signer DER-normalizes the document the certificates
                // get rewritten to be smallest cert first.
                X509Certificate2Collection certs = cms.Certificates;
                List<X509Certificate2> certList = new List<X509Certificate2>(certs.OfType<X509Certificate2>());

                int lastSize = -1;

                for (int i = 0; i < certList.Count; i++)
                {
                    byte[] rawData = certList[i].RawData;

                    Assert.True(
                        rawData.Length >= lastSize,
                        $"Certificate {i} has an encoded size ({rawData.Length}) no smaller than its predecessor ({lastSize})");
                }

                Assert.Contains(unrelated1, certList);
                Assert.Contains(unrelated2, certList);
                Assert.Contains(unrelated3, certList);
                Assert.Contains(signerCert, certList);

                Assert.Equal(ExpectCopyRemoved ? 1 : 2, certList.Count(c => c.Equals(unrelated1)));
            }

            cms.CheckSignature(true);
        }
    }
}
