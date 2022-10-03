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
        private const string TokenAttributeOid = "1.2.840.113549.1.9.16.2.14";

        [Fact]
        public static void SignerInfo_AddUnsignedAttribute_Adds()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            Assert.Equal(0, cms.SignerInfos[0].UnsignedAttributes.Count);

            AsnEncodedData attribute1 = CreateTimestampToken(1);
            cms.SignerInfos[0].AddUnsignedAttribute(attribute1);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);
            VerifyAttributesAreEqual(cms.SignerInfos[0].UnsignedAttributes[0].Values[0], attribute1);

            ReReadSignedCms(ref cms);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);
            VerifyAttributesAreEqual(cms.SignerInfos[0].UnsignedAttributes[0].Values[0], attribute1);

            AsnEncodedData attribute2 = CreateTimestampToken(2);

            cms.SignerInfos[0].AddUnsignedAttribute(attribute2);

            var expectedAttributes = new List<AsnEncodedData>();
            expectedAttributes.Add(attribute1);
            expectedAttributes.Add(attribute2);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);
            VerifyAttributesContainsAll(cms.SignerInfos[0].UnsignedAttributes, expectedAttributes);

            ReReadSignedCms(ref cms);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);
            VerifyAttributesContainsAll(cms.SignerInfos[0].UnsignedAttributes, expectedAttributes);
        }

        [Fact]
        public static void SignerInfo_RemoveUnsignedAttribute_RemoveCounterSignature()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos.Count);
            byte[] secondSignerCounterSignature = cms.SignerInfos[0].CounterSignerInfos[1].GetSignature();

            cms.SignerInfos[0].RemoveUnsignedAttribute(cms.SignerInfos[0].UnsignedAttributes[0].Values[0]);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos.Count);
            Assert.Equal(secondSignerCounterSignature, cms.SignerInfos[0].CounterSignerInfos[0].GetSignature());

            ReReadSignedCms(ref cms);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos.Count);
            Assert.Equal(secondSignerCounterSignature, cms.SignerInfos[0].CounterSignerInfos[0].GetSignature());
        }

        [Fact]
        public static void SignerInfo_RemoveUnsignedAttributeFromCounterSigner_Removes()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            AsnEncodedData attribute1 = CreateTimestampToken(1);
            AsnEncodedData attribute2 = CreateTimestampToken(2);

            SignerInfo counterSigner = cms.SignerInfos[0].CounterSignerInfos[0];
            counterSigner.AddUnsignedAttribute(attribute1);
            counterSigner.AddUnsignedAttribute(attribute2);

            ReReadSignedCms(ref cms);

            SignerInfo signer = cms.SignerInfos[0];

            // attributes got sorted, therefore number changed
            Assert.Equal(2, signer.CounterSignerInfos.Count);
            Assert.Equal(0, signer.CounterSignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, signer.CounterSignerInfos[1].UnsignedAttributes.Count);
            Assert.Equal(2, signer.CounterSignerInfos[1].UnsignedAttributes[0].Values.Count);

            counterSigner = signer.CounterSignerInfos[1];
            counterSigner.RemoveUnsignedAttribute(counterSigner.UnsignedAttributes[0].Values[0]);

            // We didn't modify existing instances
            Assert.Equal(2, signer.CounterSignerInfos.Count);
            Assert.Equal(1, counterSigner.UnsignedAttributes.Count);
            Assert.Equal(2, counterSigner.UnsignedAttributes[0].Values.Count);

            // parent got updated
            Assert.Equal(0, signer.CounterSignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, signer.CounterSignerInfos[1].UnsignedAttributes.Count);

            // We did modify the state
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes[0].Values.Count);

            ReReadSignedCms(ref cms);

            counterSigner = cms.SignerInfos[0].CounterSignerInfos[1];
            Assert.Equal(1, counterSigner.UnsignedAttributes.Count);
            Assert.Equal(1, counterSigner.UnsignedAttributes[0].Values.Count);
            counterSigner.RemoveUnsignedAttribute(counterSigner.UnsignedAttributes[0].Values[0]);

            // We didn't modify current instance
            Assert.Equal(1, counterSigner.UnsignedAttributes.Count);
            Assert.Equal(1, counterSigner.UnsignedAttributes[0].Values.Count);

            // We did modify the state
            Assert.Equal(0, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes.Count);
        }

        [Theory]
        [MemberData(nameof(SignedDocumentsWithAttributesTestData))]
        public static void SignerInfo_RemoveUnsignedAttributes_RemoveAllAttributesFromBeginning(byte[] document)
        {
            SignedCms cms = new SignedCms();
            cms.Decode(document);

            List<AsnEncodedData> attributes = GetAllAsnEncodedDataFromAttributes(cms.SignerInfos[0].UnsignedAttributes);
            Assert.True(attributes.Count > 0);

            for (int i = 0; i < attributes.Count; i++)
            {
                AsnEncodedData attribute = attributes[i];
                cms.SignerInfos[0].RemoveUnsignedAttribute(attribute);
                attributes.RemoveAt(0);

                VerifyAttributesContainsAll(cms.SignerInfos[0].UnsignedAttributes, attributes);

                ReReadSignedCms(ref cms);
                VerifyAttributesContainsAll(cms.SignerInfos[0].UnsignedAttributes, attributes);
            }
        }

        [Theory]
        [MemberData(nameof(SignedDocumentsWithAttributesTestData))]
        public static void SignerInfo_RemoveUnsignedAttributes_RemoveAllAttributesFromEnd(byte[] document)
        {
            SignedCms cms = new SignedCms();
            cms.Decode(document);

            List<AsnEncodedData> attributes = GetAllAsnEncodedDataFromAttributes(cms.SignerInfos[0].UnsignedAttributes);
            Assert.True(attributes.Count > 0);

            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                AsnEncodedData attribute = attributes[i];
                cms.SignerInfos[0].RemoveUnsignedAttribute(attribute);
                attributes.RemoveAt(i);

                VerifyAttributesContainsAll(cms.SignerInfos[0].UnsignedAttributes, attributes);

                ReReadSignedCms(ref cms);
                VerifyAttributesContainsAll(cms.SignerInfos[0].UnsignedAttributes, attributes);
            }
        }

        [Fact]
        public static void SignerInfo_RemoveUnsignedAttributes_RemoveWithNonMatchingOid()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            int numberOfAttributes = cms.SignerInfos[0].UnsignedAttributes.Count;
            Assert.NotEqual(0, numberOfAttributes);

            AsnEncodedData fakeAttribute = new AsnEncodedData(new Oid("1.2.3.4", "1.2.3.4"), cms.SignerInfos[0].UnsignedAttributes[0].Values[0].RawData);
            Assert.Throws<CryptographicException>(() => cms.SignerInfos[0].RemoveUnsignedAttribute(fakeAttribute));

            Assert.Equal(numberOfAttributes, cms.SignerInfos[0].UnsignedAttributes.Count);
        }

        [Fact]
        public static void SignerInfo_RemoveUnsignedAttributes_RemoveWithNonMatchingData()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            int numberOfAttributes = cms.SignerInfos[0].UnsignedAttributes.Count;
            Assert.NotEqual(0, numberOfAttributes);

            AsnEncodedData fakeAttribute = new AsnEncodedData(
                cms.SignerInfos[0].UnsignedAttributes[0].Oid,
                cms.SignerInfos[0].UnsignedAttributes[0].Values[0].RawData.Skip(1).ToArray());
            Assert.Throws<CryptographicException>(() => cms.SignerInfos[0].RemoveUnsignedAttribute(fakeAttribute));

            Assert.Equal(numberOfAttributes, cms.SignerInfos[0].UnsignedAttributes.Count);
        }

        [Fact]
        public static void SignerInfo_RemoveUnsignedAttributes_MultipleAttributeValues()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            Assert.Equal(0, cms.SignerInfos[0].UnsignedAttributes.Count);

            AsnEncodedData attribute1 = CreateTimestampToken(1);
            AsnEncodedData attribute2 = CreateTimestampToken(2);
            cms.SignerInfos[0].AddUnsignedAttribute(attribute1);
            cms.SignerInfos[0].AddUnsignedAttribute(attribute2);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);

            cms.SignerInfos[0].RemoveUnsignedAttribute(attribute1);
            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);
            Assert.True(AsnEncodedDataEqual(attribute2, cms.SignerInfos[0].UnsignedAttributes[0].Values[0]));

            cms.SignerInfos[0].RemoveUnsignedAttribute(attribute2);
            Assert.Equal(0, cms.SignerInfos[0].UnsignedAttributes.Count);
        }

        [Fact]
        public static void SignerInfo_AddRemoveUnsignedAttributes_JoinCounterSignaturesAttributesIntoOne()
        {
            byte[] message = { 1, 2, 3, 4, 5 };
            ContentInfo content = new ContentInfo(message);
            SignedCms cms = new SignedCms(content);

            using (X509Certificate2 signerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signerCert);
                cms.ComputeSignature(signer);
            }

            // DSA is not supported on mobile Apple platforms, so use ECDsa key instead
            X509Certificate2 counterSigner1cert =
                PlatformDetection.UsesMobileAppleCrypto ?
                Certificates.ECDsaP521Win.TryGetCertificateWithPrivateKey() :
                Certificates.Dsa1024.TryGetCertificateWithPrivateKey();

            using (counterSigner1cert)
            {
                CmsSigner counterSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, counterSigner1cert);
                counterSigner.IncludeOption = X509IncludeOption.EndCertOnly;
                counterSigner.DigestAlgorithm = new Oid(Oids.Sha1, Oids.Sha1);
                cms.SignerInfos[0].ComputeCounterSignature(counterSigner);
            }

            using (X509Certificate2 counterSigner2cert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
            {
                CmsSigner counterSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, counterSigner2cert);
                cms.SignerInfos[0].ComputeCounterSignature(counterSigner);
            }

            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);
            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes[1].Values.Count);
            cms.CheckSignature(true);

            AsnEncodedData counterSignature = cms.SignerInfos[0].UnsignedAttributes[0].Values[0];
            cms.SignerInfos[0].RemoveUnsignedAttribute(counterSignature);
            cms.SignerInfos[0].AddUnsignedAttribute(counterSignature);

            Assert.Equal(1, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes[0].Values.Count);
            cms.CheckSignature(true);
        }

        [Fact]
        public static void SignerInfo_RemoveNonMatchingUnsignedAttributesFromCounterSigner_Throws()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            int numberOfAttributes = cms.SignerInfos[0].UnsignedAttributes.Count;
            Assert.NotEqual(0, numberOfAttributes);

            AsnEncodedData fakeAttribute = new AsnEncodedData(
                cms.SignerInfos[0].UnsignedAttributes[0].Oid,
                cms.SignerInfos[0].UnsignedAttributes[0].Values[0].RawData.Skip(1).ToArray());
            Assert.Throws<CryptographicException>(() => cms.SignerInfos[0].CounterSignerInfos[0].RemoveUnsignedAttribute(fakeAttribute));

            Assert.Equal(numberOfAttributes, cms.SignerInfos[0].UnsignedAttributes.Count);
        }

        [Fact]
        public static void SignerInfo_AddOneUnsignedAttributeToCounterSigner_Adds()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            SignerInfo signer = cms.SignerInfos[0];
            Assert.Equal(2, signer.UnsignedAttributes.Count);
            Assert.Equal(2, signer.CounterSignerInfos.Count);

            SignerInfo counterSigner0 = signer.CounterSignerInfos[0];
            SignerInfo counterSigner1 = signer.CounterSignerInfos[1];

            Assert.Equal(0, counterSigner0.UnsignedAttributes.Count);
            Assert.Equal(0, counterSigner1.UnsignedAttributes.Count);

            AsnEncodedData attribute = CreateTimestampToken(1);
            counterSigner0.AddUnsignedAttribute(attribute);

            // we didn't modify existing instances
            Assert.Equal(2, signer.CounterSignerInfos.Count);
            Assert.Equal(0, counterSigner0.UnsignedAttributes.Count);
            Assert.Equal(0, counterSigner1.UnsignedAttributes.Count);

            // inner counter signers did get updated
            Assert.Equal(1, signer.CounterSignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, signer.CounterSignerInfos[0].UnsignedAttributes[0].Values.Count);
            Assert.Equal(0, signer.CounterSignerInfos[1].UnsignedAttributes.Count);

            // verify we didn't modify anything we shouldn't
            Assert.Equal(1, cms.SignerInfos.Count);
            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos.Count);

            // note: counter signers got sorted when encoded
            Assert.Equal(0, cms.SignerInfos[0].CounterSignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes[0].Values.Count);
            VerifyAttributesAreEqual(cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes[0].Values[0], attribute);

            ReReadSignedCms(ref cms);

            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos.Count);
            Assert.Equal(0, cms.SignerInfos[0].CounterSignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes[0].Values.Count);
            VerifyAttributesAreEqual(cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes[0].Values[0], attribute);
        }

        [Fact]
        public static void SignerInfo_AddMultipleUnsignedAttributeToCounterSigner_Adds()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            SignerInfo signer = cms.SignerInfos[0];
            Assert.Equal(2, signer.UnsignedAttributes.Count);
            Assert.Equal(2, signer.CounterSignerInfos.Count);

            SignerInfo counterSigner0 = signer.CounterSignerInfos[0];
            SignerInfo counterSigner1 = signer.CounterSignerInfos[1];

            Assert.Equal(0, counterSigner0.UnsignedAttributes.Count);
            Assert.Equal(0, counterSigner1.UnsignedAttributes.Count);

            AsnEncodedData attribute1 = CreateTimestampToken(1);
            AsnEncodedData attribute2 = CreateTimestampToken(2);

            // note: using same instance to add
            counterSigner0.AddUnsignedAttribute(attribute1);
            SignerInfo signerAfterFirstCall = cms.SignerInfos[0];
            counterSigner0.AddUnsignedAttribute(attribute2);

            List<AsnEncodedData> expectedAttributes = new List<AsnEncodedData>()
            {
                attribute1,
                attribute2
            };

            // we didn't modify existing instances
            Assert.Equal(2, signer.CounterSignerInfos.Count);
            Assert.Equal(0, counterSigner0.UnsignedAttributes.Count);
            Assert.Equal(0, counterSigner1.UnsignedAttributes.Count);

            // inner counter signers did get updated
            Assert.Equal(1, signer.CounterSignerInfos[0].UnsignedAttributes.Count);

            // this is rather less obvious:
            //   - on first Add call we update the parent so the `signer` gets updated
            //   - on second Add call we also update the parent but `signer` is not actually the parent anymore
            Assert.Equal(1, signer.CounterSignerInfos[0].UnsignedAttributes[0].Values.Count);
            VerifyAttributesAreEqual(signer.CounterSignerInfos[0].UnsignedAttributes[0].Values[0], attribute1);
            Assert.Equal(0, signer.CounterSignerInfos[1].UnsignedAttributes.Count);

            Assert.Equal(0, signerAfterFirstCall.CounterSignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, signerAfterFirstCall.CounterSignerInfos[1].UnsignedAttributes.Count);
            Assert.Equal(2, signerAfterFirstCall.CounterSignerInfos[1].UnsignedAttributes[0].Values.Count);
            VerifyAttributesContainsAll(signerAfterFirstCall.CounterSignerInfos[1].UnsignedAttributes, expectedAttributes);

            // verify we didn't modify anything we shouldn't
            Assert.Equal(1, cms.SignerInfos.Count);
            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos.Count);

            // note: counter signers got sorted when encoded
            counterSigner0 = cms.SignerInfos[0].CounterSignerInfos[0];
            counterSigner1 = cms.SignerInfos[0].CounterSignerInfos[1];

            Assert.Equal(0, counterSigner0.UnsignedAttributes.Count);

            // attributes should be merged into a single one with two values
            Assert.Equal(1, counterSigner1.UnsignedAttributes.Count);
            Assert.Equal(2, counterSigner1.UnsignedAttributes[0].Values.Count);

            VerifyAttributesContainsAll(counterSigner1.UnsignedAttributes, expectedAttributes);

            ReReadSignedCms(ref cms);

            Assert.Equal(2, cms.SignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos.Count);
            Assert.Equal(0, cms.SignerInfos[0].CounterSignerInfos[0].UnsignedAttributes.Count);
            Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes.Count);
            Assert.Equal(2, cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes[0].Values.Count);
            VerifyAttributesContainsAll(cms.SignerInfos[0].CounterSignerInfos[1].UnsignedAttributes, expectedAttributes);
        }

        [Fact]
        public static void SignerInfo_AddRemoveUnsignedAttributeWithDetachtedCounterSigner_Throws()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            // Detach one counter signer
            SignerInfo counterSigner = cms.SignerInfos[0].CounterSignerInfos[0];
            cms.SignerInfos[0].RemoveCounterSignature(0);

            // we shouldn't throw
            Assert.Equal(0, counterSigner.UnsignedAttributes.Count);

            AsnEncodedData attribute = CreateTimestampToken(1);
            Assert.Throws<CryptographicException>(() => counterSigner.AddUnsignedAttribute(attribute));
            Assert.Throws<CryptographicException>(() => counterSigner.RemoveUnsignedAttribute(attribute));
        }

        [Fact]
        public static void SignerInfo_AddRemoveUnsignedAttributeWithDetachtedSigner_Throws()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            // Detach signer (and its counter signers)
            SignerInfo counterSigner = cms.SignerInfos[0].CounterSignerInfos[0];
            cms.RemoveSignature(0);

            // we shouldn't throw
            Assert.Equal(0, counterSigner.UnsignedAttributes.Count);

            AsnEncodedData attribute = CreateTimestampToken(1);
            Assert.Throws<CryptographicException>(() => counterSigner.AddUnsignedAttribute(attribute));
            Assert.Throws<CryptographicException>(() => counterSigner.RemoveUnsignedAttribute(attribute));
        }

        private static void VerifyAttributesContainsAll(CryptographicAttributeObjectCollection attributes, List<AsnEncodedData> expectedAttributes)
        {
            var indices = new HashSet<int>();
            foreach (CryptographicAttributeObject attribute in attributes)
            {
                foreach (AsnEncodedData attributeValue in attribute.Values)
                {
                    int idx = FindAsnEncodedData(expectedAttributes, attributeValue);
                    Assert.NotEqual(-1, idx);
                    indices.Add(idx);
                }
            }

            Assert.Equal(expectedAttributes.Count, indices.Count);
        }

        private static int FindAsnEncodedData(List<AsnEncodedData> array, AsnEncodedData data)
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (AsnEncodedDataEqual(array[i], data))
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<AsnEncodedData> GetAllAsnEncodedDataFromAttributes(CryptographicAttributeObjectCollection attributes)
        {
            var ret = new List<AsnEncodedData>();
            foreach (CryptographicAttributeObject attribute in attributes)
            {
                foreach (AsnEncodedData attributeValue in attribute.Values)
                {
                    ret.Add(attributeValue);
                }
            }

            return ret;
        }

        private static bool AsnEncodedDataEqual(AsnEncodedData a, AsnEncodedData b)
        {
            return a.Oid.Value == b.Oid.Value && a.RawData.SequenceEqual(b.RawData);
        }

        private static void ReReadSignedCms(ref SignedCms cms)
        {
            byte[] bytes = cms.Encode();

            cms = new SignedCms();
            cms.Decode(bytes);
        }

        private static AsnEncodedData CreateTimestampToken(byte serial)
        {
            Oid tokenOid = new Oid(TokenAttributeOid, TokenAttributeOid);

            Oid policyId = new Oid("0.0", "0.0");
            Oid hashAlgorithmId = new Oid(Oids.Sha256);

            var tokenInfo = new Rfc3161TimestampTokenInfo(
                policyId,
                hashAlgorithmId,
                new byte[256 / 8],
                new byte[] { (byte)serial },
                DateTimeOffset.UtcNow);

            return new AsnEncodedData(tokenOid, tokenInfo.Encode());
        }

        private static void VerifyAttributesAreEqual(AsnEncodedData actual, AsnEncodedData expected)
        {
            Assert.NotSame(expected.Oid, actual.Oid);
            Assert.Equal(expected.Oid.Value, actual.Oid.Value);

            // We need to decode bytes because DER and BER may encode the same information slightly differently
            Rfc3161TimestampTokenInfo expectedToken;
            Assert.True(Rfc3161TimestampTokenInfo.TryDecode(expected.RawData, out expectedToken, out _));

            Rfc3161TimestampTokenInfo actualToken;
            Assert.True(Rfc3161TimestampTokenInfo.TryDecode(actual.RawData, out actualToken, out _));

            Assert.Equal(expectedToken.GetSerialNumber().ByteArrayToHex(), actualToken.GetSerialNumber().ByteArrayToHex());
            Assert.Equal(expectedToken.Timestamp, actualToken.Timestamp);
            Assert.Equal(Oids.Sha256, expectedToken.HashAlgorithmId.Value);
            Assert.Equal(expectedToken.HashAlgorithmId.Value, actualToken.HashAlgorithmId.Value);
        }

        public static IEnumerable<object[]> SignedDocumentsWithAttributesTestData()
        {
            yield return new object[] { SignedDocuments.CounterSignedRsaPkcs1OneSigner };
            yield return new object[] { SignedDocuments.NoSignatureSignedWithAttributesAndCounterSignature };
            yield return new object[] { SignedDocuments.OneRsaSignerTwoRsaCounterSigners };
            yield return new object[] { SignedDocuments.RsaPkcs1CounterSignedWithNoSignature };
            yield return new object[] { SignedDocuments.UnsortedSignerInfos};
        }
    }
}
