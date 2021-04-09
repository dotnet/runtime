// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class HmacTests
    {
        // RFC2202 defines the test vectors for HMACMD5 and HMACSHA1
        // RFC4231 defines the test vectors for HMACSHA{224,256,384,512}
        // They share the same datasets for cases 1-5, but cases 6 and 7 differ.
        private readonly byte[][] _testKeys;
        private readonly byte[][] _testData;

        protected HmacTests(byte[][] testKeys, byte[][] testData)
        {
            _testKeys = testKeys;
            _testData = testData;
        }

        protected abstract HMAC Create();

        protected abstract HashAlgorithm CreateHashAlgorithm();

        protected abstract int BlockSize { get; }

        protected void VerifyHmac(
            int testCaseId,
            string digest,
            int truncateSize = -1)
        {
            byte[] digestBytes = ByteUtils.HexToByteArray(digest);
            byte[] computedDigest;

            using (HMAC hmac = Create())
            {
                Assert.True(hmac.HashSize > 0);

                byte[] key = (byte[])_testKeys[testCaseId].Clone();
                hmac.Key = key;

                // make sure the getter returns different objects each time
                Assert.NotSame(key, hmac.Key);
                Assert.NotSame(hmac.Key, hmac.Key);

                // make sure the setter didn't cache the exact object we passed in
                key[0] = (byte)(key[0] + 1);
                Assert.NotEqual<byte>(key, hmac.Key);

                computedDigest = hmac.ComputeHash(_testData[testCaseId]);
            }

            if (truncateSize != -1)
            {
                byte[] tmp = new byte[truncateSize];
                Array.Copy(computedDigest, tmp, truncateSize);
                computedDigest = tmp;
            }

            Assert.Equal(digestBytes, computedDigest);
        }

        protected void VerifyHmac_KeyAlreadySet(
            HMAC hmac,
            int testCaseId,
            string digest)
        {
            byte[] digestBytes = ByteUtils.HexToByteArray(digest);
            byte[] computedDigest;

            computedDigest = hmac.ComputeHash(_testData[testCaseId]);
            Assert.Equal(digestBytes, computedDigest);
        }

        protected void VerifyHmacRfc2104_2()
        {
            // Ensure that keys shorter than the threshold don't get altered.
            using (HMAC hmac = Create())
            {
                byte[] key = new byte[BlockSize];
                hmac.Key = key;
                byte[] retrievedKey = hmac.Key;
                Assert.Equal<byte>(key, retrievedKey);
            }

            // Ensure that keys longer than the threshold are adjusted via Rfc2104 Section 2.
            using (HMAC hmac = Create())
            {
                byte[] overSizedKey = new byte[BlockSize + 1];
                hmac.Key = overSizedKey;
                byte[] actualKey = hmac.Key;
                byte[] expectedKey = CreateHashAlgorithm().ComputeHash(overSizedKey);
                Assert.Equal<byte>(expectedKey, actualKey);

                // Also ensure that the hashing operation uses the adjusted key.
                byte[] data = new byte[100];
                hmac.Key = expectedKey;
                byte[] expectedHash = hmac.ComputeHash(data);

                hmac.Key = overSizedKey;
                byte[] actualHash = hmac.ComputeHash(data);
                Assert.Equal<byte>(expectedHash, actualHash);
            }
        }

        [Fact]
        public void InvalidInput_Null()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash(null, 0, 0));
                Assert.Throws<NullReferenceException>(() => hash.ComputeHash((Stream)null));
            }
        }

        [Fact]
        public void InvalidInput_NegativeOffset()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => hash.ComputeHash(Array.Empty<byte>(), -1, 0));
            }
        }

        [Fact]
        public void InvalidInput_NegativeCount()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, -1));
            }
        }

        [Fact]
        public void InvalidInput_TooBigOffset()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 1, 0));
            }
        }

        [Fact]
        public void InvalidInput_TooBigCount()
        {
            byte[] nonEmpty = new byte[53];

            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 0, nonEmpty.Length + 1));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 1, nonEmpty.Length));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 2, nonEmpty.Length - 1));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, 1));
            }
        }

        [Fact]
        public void BoundaryCondition_Count0()
        {
            byte[] nonEmpty = new byte[53];

            using (HMAC hash = Create())
            {
                byte[] emptyHash = hash.ComputeHash(Array.Empty<byte>());
                byte[] shouldBeEmptyHash = hash.ComputeHash(nonEmpty, nonEmpty.Length, 0);

                Assert.Equal(emptyHash, shouldBeEmptyHash);

                shouldBeEmptyHash = hash.ComputeHash(nonEmpty, 0, 0);
                Assert.Equal(emptyHash, shouldBeEmptyHash);

                nonEmpty[0] = 0xFF;
                nonEmpty[nonEmpty.Length - 1] = 0x77;

                shouldBeEmptyHash = hash.ComputeHash(nonEmpty, nonEmpty.Length, 0);
                Assert.Equal(emptyHash, shouldBeEmptyHash);

                shouldBeEmptyHash = hash.ComputeHash(nonEmpty, 0, 0);
                Assert.Equal(emptyHash, shouldBeEmptyHash);
            }
        }

        [Fact]
        public void OffsetAndCountRespected()
        {
            byte[] dataA = { 1, 1, 2, 3, 5, 8 };
            byte[] dataB = { 0, 1, 1, 2, 3, 5, 8, 13 };

            using (HMAC hash = Create())
            {
                byte[] baseline = hash.ComputeHash(dataA);

                // Skip the 0 byte, and stop short of the 13.
                byte[] offsetData = hash.ComputeHash(dataB, 1, dataA.Length);

                Assert.Equal(baseline, offsetData);
            }
        }

        [Fact]
        public void InvalidKey_ThrowArgumentNullException()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => hash.Key = null);
            }
        }
    }
}
