// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    // Tests that apply to all [Try]Sign overloads.
    public abstract class CoseSign1MessageTests_Sign<T> where T : AsymmetricAlgorithm
    {
        internal CoseAlgorithm DefaultAlgorithm => CoseAlgorithms[0];
        internal T DefaultKey => GetKeyHashPair<T>(CoseAlgorithms[0]).Key;
        internal HashAlgorithmName DefaultHash => GetKeyHashPair<T>(CoseAlgorithms[0]).Hash;
        internal abstract List<CoseAlgorithm> CoseAlgorithms { get; }
        internal abstract byte[] Sign(byte[] content, T key, HashAlgorithmName hashAlgorithm, bool isDetached = false);
        internal abstract bool Verify(CoseSign1Message msg, T key);
        internal abstract bool Verify(CoseSign1Message msg, T key, ReadOnlySpan<byte> content);

        internal IEnumerable<(T Key, HashAlgorithmName Hash, CoseAlgorithm Algorithm)> GetKeyHashAlgorithmTriplet(bool useNonPrivateKey = false)
        {
            foreach (var algorithm in CoseAlgorithms)
            {
                var keyHashPair = GetKeyHashPair<T>(algorithm, useNonPrivateKey);
                yield return (keyHashPair.Key, keyHashPair.Hash, algorithm);
            }
        }

        [Fact]
        public void SignVerify()
        {
            foreach ((T key, HashAlgorithmName hashAlgorithm, CoseAlgorithm algorithm) in GetKeyHashAlgorithmTriplet())
            {
                ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, key, hashAlgorithm);
                AssertSign1Message(encodedMsg, s_sampleContent, key, algorithm);

                CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
                Assert.True(Verify(msg, key));
            }
        }

        [Fact]
        public void SignWithNullContent()
        {
            Assert.Throws<ArgumentNullException>("content", () => Sign(null!, DefaultKey, DefaultHash));
        }

        [Theory]
        [InlineData(ContentTestCase.Empty)]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void SignWithValidContent(ContentTestCase @case)
        {
            byte[] content = GetDummyContent(@case);
            ReadOnlySpan<byte> encodedMsg = Sign(content, DefaultKey, DefaultHash);
            AssertSign1Message(encodedMsg, content, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNullKey()
        {
            Assert.Throws<ArgumentNullException>("key", () => Sign(s_sampleContent, null!, DefaultHash));
        }

        [Fact]
        public void SignWithNonPrivateKey()
        {
            foreach ((T nonPrivateKey, HashAlgorithmName hashAlgorithm, _) in GetKeyHashAlgorithmTriplet(useNonPrivateKey: true))
            {
                Assert.ThrowsAny<CryptographicException>(() => Sign(s_sampleContent, nonPrivateKey, hashAlgorithm));
            }
        }

        [Theory]
        [InlineData("SHA1")]
        [InlineData("FOO")]
        public void SignWithUnsupportedHashAlgorithm(string hashAlgorithm)
        {
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, new HashAlgorithmName(hashAlgorithm)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SignWithIsDetached(bool isDetached)
        {
            ReadOnlySpan<byte> messageEncoded = CoseSign1Message.Sign(s_sampleContent, DefaultKey, DefaultHash, isDetached: isDetached);
            AssertSign1Message(messageEncoded, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedDetachedContent: isDetached);

            messageEncoded = Sign(s_sampleContent, DefaultKey, DefaultHash, isDetached: isDetached);
            AssertSign1Message(messageEncoded, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedDetachedContent: isDetached);
        }
    }

    public class CoseSign1MessageTests_Sign_ECDsa : CoseSign1MessageTests_Sign<ECDsa>
    {
        internal override List<CoseAlgorithm> CoseAlgorithms => new() { CoseAlgorithm.ES256, CoseAlgorithm.ES384, CoseAlgorithm.ES512 };

        internal override byte[] Sign(byte[] content, ECDsa key, HashAlgorithmName hashAlgorithm, bool isDetached = false)
            => CoseSign1Message.Sign(content, key, hashAlgorithm, isDetached: isDetached);
        internal override bool Verify(CoseSign1Message msg, ECDsa key)
            => msg.Verify(key);
        internal override bool Verify(CoseSign1Message msg, ECDsa key, ReadOnlySpan<byte> content)
            => msg.Verify(key, content);
    }

    public class CoseSign1MessageTests_Sign_RSA : CoseSign1MessageTests_Sign<RSA>
    {
        internal override List<CoseAlgorithm> CoseAlgorithms => new() { CoseAlgorithm.PS256, CoseAlgorithm.PS384, CoseAlgorithm.PS512 };

        internal override byte[] Sign(byte[] content, RSA key, HashAlgorithmName hashAlgorithm, bool isDetached = false)
            => CoseSign1Message.Sign(content, key, hashAlgorithm, isDetached: isDetached);
        internal override bool Verify(CoseSign1Message msg, RSA key)
            => msg.Verify(key);
        internal override bool Verify(CoseSign1Message msg, RSA key, ReadOnlySpan<byte> content)
            => msg.Verify(key, content);
    }
}
