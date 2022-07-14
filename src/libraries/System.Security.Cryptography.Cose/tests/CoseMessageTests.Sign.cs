// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Test.Cryptography;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    // Tests that apply to all [Try]Sign overloads using one single signer.
    public abstract class CoseMessageTests_Sign<T> where T : AsymmetricAlgorithm
    {
        internal virtual bool OnlySupportsDetachedContent => false;
        internal CoseAlgorithm DefaultAlgorithm => CoseAlgorithms[CoseAlgorithms.Count - 1];
        internal T DefaultKey => GetKeyHashPaddingTriplet<T>(CoseAlgorithms[CoseAlgorithms.Count - 1]).Key;
        internal HashAlgorithmName DefaultHash => GetKeyHashPaddingTriplet<T>(CoseAlgorithms[CoseAlgorithms.Count - 1]).Hash;
        internal abstract List<CoseAlgorithm> CoseAlgorithms { get; }
        internal abstract CoseMessageKind MessageKind { get; }

        internal abstract void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null);

        internal abstract CoseMessage Decode(ReadOnlySpan<byte> cborPayload);

        internal abstract byte[] Sign(byte[] content,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            byte[]? associatedData = null,
            bool isDetached = false);

        internal abstract bool Verify(CoseMessage msg, T key, byte[] content, byte[]? associatedData = null);

        internal IEnumerable<(T Key, HashAlgorithmName Hash, CoseAlgorithm Algorithm, RSASignaturePadding? Padding)> GetKeyHashAlgorithmPaddingQuadruplet(bool useNonPrivateKey = false)
        {
            foreach (var algorithm in CoseAlgorithms)
            {
                var keyHashTriplet = GetKeyHashPaddingTriplet<T>(algorithm, useNonPrivateKey);
                yield return (keyHashTriplet.Key, keyHashTriplet.Hash, algorithm, keyHashTriplet.Padding);
            }
        }

        internal void AssertCoseSignMessage(
            ReadOnlySpan<byte> encodedMsg,
            ReadOnlySpan<byte> expectedContent,
            AsymmetricAlgorithm key,
            CoseAlgorithm algorithm,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedProtectedHeaders = null,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedUnprotectedHeaders = null,
            bool? expectedDetachedContent = null,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedMultiSignBodyProtectedHeaders = null,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedMultiSignBodyUnprotectedHeaders = null,
            int expectedSignatures = 1)
        {
            if (OnlySupportsDetachedContent && expectedDetachedContent != null)
            {
                throw new InvalidOperationException($"Don't specify {nameof(expectedDetachedContent)}, {GetType()} only supports detached content.");
            }

            if (MessageKind == CoseMessageKind.Sign1)
            {
                Assert.Null(expectedMultiSignBodyProtectedHeaders);
                Assert.Null(expectedMultiSignBodyUnprotectedHeaders);
                AssertSign1MessageCore(
                    encodedMsg,
                    expectedContent,
                    key,
                    algorithm,
                    expectedProtectedHeaders,
                    expectedUnprotectedHeaders,
                    expectedDetachedContent ?? OnlySupportsDetachedContent);
            }
            else if (MessageKind == CoseMessageKind.MultiSign)
            {
                AssertMultiSignMessageCore(
                    encodedMsg,
                    expectedContent,
                    key,
                    algorithm,
                    expectedSignatures,
                    expectedMultiSignBodyProtectedHeaders,
                    expectedMultiSignBodyUnprotectedHeaders,
                    expectedProtectedHeaders,
                    expectedUnprotectedHeaders,
                    expectedDetachedContent ?? OnlySupportsDetachedContent);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        [Fact]
        public void SignVerify()
        {
            foreach ((T key, HashAlgorithmName hashAlgorithm, CoseAlgorithm algorithm, RSASignaturePadding? padding)
                in GetKeyHashAlgorithmPaddingQuadruplet())
            {
                var signer = GetCoseSigner(key, hashAlgorithm, padding: padding);
                ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, signer);
                AssertCoseSignMessage(encodedMsg, s_sampleContent, key, algorithm);

                CoseMessage msg = Decode(encodedMsg);
                Assert.True(Verify(msg, key, s_sampleContent));
            }
        }

        [Fact]
        public void SignWithNullContent()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => Sign(null!, GetCoseSigner(DefaultKey, DefaultHash)));
            Assert.True(ex.ParamName == "embeddedContent" || ex.ParamName == "detachedContent");
        }

        [Theory]
        [InlineData(ContentTestCase.Empty)]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void SignWithValidContent(ContentTestCase @case)
        {
            byte[] content = GetDummyContent(@case);
            ReadOnlySpan<byte> encodedMsg = Sign(content, GetCoseSigner(DefaultKey, DefaultHash));
            AssertCoseSignMessage(encodedMsg, content, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNonPrivateKey()
        {
            foreach ((T nonPrivateKey, HashAlgorithmName hashAlgorithm, _, RSASignaturePadding? padding)
                in GetKeyHashAlgorithmPaddingQuadruplet(useNonPrivateKey: true))
            {
                Assert.ThrowsAny<CryptographicException>(() => Sign(s_sampleContent, GetCoseSigner(nonPrivateKey, hashAlgorithm, padding: padding)));
            }
        }

        [Theory]
        [InlineData("SHA1")]
        [InlineData("FOO")]
        public void SignWithUnsupportedHashAlgorithm(string hashAlgorithm)
        {
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, GetCoseSigner(DefaultKey, new HashAlgorithmName(hashAlgorithm))));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SignWithIsDetached(bool isDetached)
        {
            if (OnlySupportsDetachedContent)
            {
                return;
            }

            ReadOnlySpan<byte> messageEncoded = Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash), isDetached: isDetached);
            AssertCoseSignMessage(messageEncoded, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedDetachedContent: isDetached);
        }

        [Fact]
        public void SignVerifyWithAssociatedData()
        {
            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            byte[] associatedData = ByteUtils.HexToByteArray("11aa22bb33cc44dd55006699");
            byte[]? encodedMsg = Sign(s_sampleContent, signer, associatedData: associatedData);

            CoseMessage msg = Decode(encodedMsg);
            Assert.False(Verify(msg, DefaultKey, s_sampleContent));
            Assert.True(Verify(msg, DefaultKey, s_sampleContent, associatedData));
        }

        [Fact]
        public void MultiSign_AddSignature()
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            CoseMessage msg = Decode(Sign(s_sampleContent, signer));
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;

            signer = GetCoseSigner(DefaultKey, DefaultHash);
            AddSignature(multiSignMsg, s_sampleContent, signer);
            Assert.Equal(2, signatures.Count);

            // Encode/Decode
            ReadOnlySpan<byte> encodedMsg = msg.Encode();
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedSignatures: 2);
            multiSignMsg = Assert.IsType<CoseMultiSignMessage>(Decode(encodedMsg));

            // Verify
            MultiSignVerify(multiSignMsg, DefaultKey, s_sampleContent, expectedSignatures: 2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultiSign_RemoveSignature(bool useIndex)
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            CoseMessage msg = Decode(Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash)));
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(1, signatures.Count);

            RemoveSignature(multiSignMsg, signatures[0], useIndex);
            Assert.Equal(0, signatures.Count);

            // You can't create a message without signatures.
            Assert.Throws<CryptographicException>(multiSignMsg.Encode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultiSign_RemoveThenAddSignature(bool useIndex)
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            CoseMessage msg = Decode(Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash)));
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;

            RemoveSignature(multiSignMsg, signatures[0], useIndex);
            Assert.Equal(0, signatures.Count);

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            AddSignature(multiSignMsg, s_sampleContent, signer);
            Assert.Equal(1, signatures.Count);

            // Encode/Decode
            ReadOnlySpan<byte> encodedMsg = msg.Encode();
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedSignatures: 1);
            multiSignMsg = Assert.IsType<CoseMultiSignMessage>(Decode(encodedMsg));

            // Verify
            MultiSignVerify(multiSignMsg, DefaultKey, s_sampleContent, expectedSignatures: 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultiSign_AddThenRemoveSignature(bool useIndex)
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            CoseMessage msg = Decode(Sign(s_sampleContent, signer));
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;

            signer = GetCoseSigner(DefaultKey, DefaultHash);
            AddSignature(multiSignMsg, s_sampleContent, signer);
            Assert.Equal(2, signatures.Count);

            RemoveSignature(multiSignMsg, signatures[0], useIndex);
            Assert.Equal(1, signatures.Count);

            // Encode/Decode
            ReadOnlySpan<byte> encodedMsg = msg.Encode();
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedSignatures: 1);
            multiSignMsg = Assert.IsType<CoseMultiSignMessage>(Decode(encodedMsg));

            // Verify
            MultiSignVerify(multiSignMsg, DefaultKey, s_sampleContent, expectedSignatures: 1);
        }

        private void RemoveSignature(CoseMultiSignMessage msg, CoseSignature signature, bool useIndex)
        {
            if (useIndex)
            {
                msg.RemoveSignature(msg.Signatures.IndexOf(signature));
            }
            else
            {
                msg.RemoveSignature(signature);
            }
        }
    }
}
