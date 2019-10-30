// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class CounterSigningDerOrder
    {
        [Fact]
        public static void CounterSigningReindexes()
        {
            ContentInfo content = new ContentInfo(new byte[] { 7 });
            SignedCms cms = new SignedCms(content);

            using (X509Certificate2 cert1 = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            using (X509Certificate2 cert2 = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert1);
                cms.ComputeSignature(signer);

                SignerProfile yellow = new SignerProfile(
                    cert1,
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    hasSignedAttrs: false,
                    hasUnsignedAttrs: false,
                    hasCounterSigners: false);

                AssertSignerTraits(cms.SignerInfos[0], yellow);

                signer.SignedAttributes.Add(new Pkcs9SigningTime());
                signer.SignerIdentifierType = SubjectIdentifierType.IssuerAndSerialNumber;
                cms.ComputeSignature(signer);

                SignerProfile green = new SignerProfile(
                    cert1,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    hasSignedAttrs: true,
                    hasUnsignedAttrs: false,
                    hasCounterSigners: false);

                // No reordering.  0 stayed 0, new entry becomes 1.
                AssertSignerTraits(cms.SignerInfos[0], yellow);
                AssertSignerTraits(cms.SignerInfos[1], green);

                signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert2);
                cms.ComputeSignature(signer);

                SignerProfile grey = new SignerProfile(
                    cert2,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    hasSignedAttrs: false,
                    hasUnsignedAttrs: false,
                    hasCounterSigners: false);

                // No reordering.  0 stayed 0, 1 stays 1, new entry is 2.
                AssertSignerTraits(cms.SignerInfos[0], yellow);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], grey);

                CmsSigner counterSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert1);
                SignerInfo toCounterSign = cms.SignerInfos[0];
                toCounterSign.ComputeCounterSignature(counterSigner);

                // Reordering just happened.
                // We counter-signed the first element, so it gets bigger by ~a signerinfo, or 100% bigger.
                // The sizes of the three were
                //   yellow: 311 bytes
                //   green: 455 bytes (IssuerAndSerialNumber takes more bytes, and it has attributes)
                //   grey: 212 bytes (1024-bit RSA signature instead of 2048-bit)
                //
                // Because yellow also contains cyan (444) bytes (then also some overhead) it has grown
                // to 763 bytes.  So the size-sorted order (DER SET-OF sorting) is { grey, green, yellow }.

                // Record that yellow gained a countersigner (and thus an unsigned attribute)
                yellow.HasUnsignedAttrs = true;
                yellow.HasCounterSigners = true;

                SignerProfile cyan = new SignerProfile(
                    cert1,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    hasSignedAttrs: true,
                    hasUnsignedAttrs: false,
                    hasCounterSigners: false);

                AssertSignerTraits(cms.SignerInfos[0], grey);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], yellow);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[0], cyan);

                counterSigner.UnsignedAttributes.Add(new Pkcs9SigningTime());
                toCounterSign.ComputeCounterSignature(counterSigner);

                SignerProfile red = new SignerProfile(
                    cert1,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    hasSignedAttrs: true,
                    hasUnsignedAttrs: true,
                    hasCounterSigners: false);

                // Since "red" has one more attribute than "cyan", but they're otherwise the same
                // it will sort later.

                AssertSignerTraits(cms.SignerInfos[0], grey);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], yellow);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[0], cyan);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[1], red);

                counterSigner.SignerIdentifierType = SubjectIdentifierType.SubjectKeyIdentifier;
                toCounterSign.ComputeCounterSignature(counterSigner);

                SignerProfile clear = new SignerProfile(
                    cert1,
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    hasSignedAttrs: true,
                    hasUnsignedAttrs: true,
                    hasCounterSigners: false);

                // By changing from IssuerAndSerialNumber to SubjectKeyIdentifier, this copy will
                // sort higher.  It saves so many bytes, in this specific case, that it goes first.

                AssertSignerTraits(cms.SignerInfos[0], grey);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], yellow);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[0], clear);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[1], cyan);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[2], red);

                // Now start removing things.

                cms.SignerInfos[2].RemoveCounterSignature(1);

                // Fairly predictable.
                AssertSignerTraits(cms.SignerInfos[0], grey);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], yellow);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[0], clear);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[1], red);

                cms.SignerInfos[2].RemoveCounterSignature(1);

                // Fairly predictable.
                AssertSignerTraits(cms.SignerInfos[0], grey);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], yellow);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[0], clear);

                cms.SignerInfos[2].RemoveCounterSignature(0);

                // We have removed the last counter-signer.
                // yellow is now smaller than grey.
                // But the document only re-normalizes on addition.

                yellow.HasCounterSigners = false;
                yellow.HasUnsignedAttrs = false;

                AssertSignerTraits(cms.SignerInfos[0], grey);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], yellow);

                // Export/import to normalize. Everyone's back to their original size,
                // but they were unsorted before. { grey, yellow, green } was the right order.
                cms.Decode(cms.Encode());

                AssertSignerTraits(cms.SignerInfos[0], grey);
                AssertSignerTraits(cms.SignerInfos[1], yellow);
                AssertSignerTraits(cms.SignerInfos[2], green);

                cms.SignerInfos[0].ComputeCounterSignature(counterSigner);

                // Move to the end of the line, Mr. Grey
                grey.HasUnsignedAttrs = true;
                grey.HasCounterSigners = true;

                AssertSignerTraits(cms.SignerInfos[0], yellow);
                AssertSignerTraits(cms.SignerInfos[1], green);
                AssertSignerTraits(cms.SignerInfos[2], grey);
                AssertSignerTraits(cms.SignerInfos[2].CounterSignerInfos[0], clear);
            }
        }

        private static void AssertSignerTraits(SignerInfo signerInfo, SignerProfile profile)
        {
            Assert.Equal(profile.Type, signerInfo.SignerIdentifier.Type);
            Assert.Equal(profile.Cert, signerInfo.Certificate);
            AssertMaybeEmpty(profile.HasSignedAttrs, signerInfo.SignedAttributes);
            AssertMaybeEmpty(profile.HasUnsignedAttrs, signerInfo.UnsignedAttributes);
            AssertMaybeEmpty(profile.HasCounterSigners, signerInfo.CounterSignerInfos);
        }

        private static void AssertMaybeEmpty(bool shouldHaveData, IEnumerable collection)
        {
            if (shouldHaveData)
                Assert.NotEmpty(collection);
            else
                Assert.Empty(collection);
        }

        private class SignerProfile
        {
            public X509Certificate2 Cert { get; }
            public SubjectIdentifierType Type { get; }
            public bool HasSignedAttrs { get; }
            public bool HasUnsignedAttrs { get; set; }
            public bool HasCounterSigners { get; set; }

            internal SignerProfile(
                X509Certificate2 cert,
                SubjectIdentifierType type,
                bool hasSignedAttrs,
                bool hasUnsignedAttrs,
                bool hasCounterSigners)
            {
                Cert = cert;
                Type = type;
                HasSignedAttrs = hasSignedAttrs;
                HasUnsignedAttrs = hasUnsignedAttrs;
                HasCounterSigners = hasCounterSigners;
            }
        }
    }
}
