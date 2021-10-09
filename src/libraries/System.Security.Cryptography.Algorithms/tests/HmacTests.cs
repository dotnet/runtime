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
        private readonly byte[][] _testMacs;

        protected HmacTests(byte[][] testKeys, byte[][] testData, byte[][] testMacs)
        {
            _testKeys = testKeys;
            _testData = testData;
            _testMacs = testMacs;
        }

        protected abstract HMAC Create();

        protected abstract HashAlgorithm CreateHashAlgorithm();
        protected abstract byte[] HashDataOneShot(byte[] key, byte[] source);
        protected abstract byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source);
        protected abstract int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination);
        protected abstract bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written);

        protected abstract int BlockSize { get; }
        protected abstract int MacSize { get; }

        protected void VerifyHmac(int testCaseId, byte[] digestBytes)
        {
            byte[] data = _testData[testCaseId];
            byte[] computedDigest;
            int truncateSize = digestBytes.Length;
            AssertExtensions.LessThanOrEqualTo(truncateSize, MacSize);

            using (HMAC hmac = Create())
            {
                Assert.Equal(MacSize, hmac.HashSize / 8);

                byte[] key = (byte[])_testKeys[testCaseId].Clone();
                hmac.Key = key;

                // make sure the getter returns different objects each time
                Assert.NotSame(key, hmac.Key);
                Assert.NotSame(hmac.Key, hmac.Key);

                // make sure the setter didn't cache the exact object we passed in
                key[0] = (byte)(key[0] + 1);
                Assert.NotEqual<byte>(key, hmac.Key);

                computedDigest = hmac.ComputeHash(data);
            }

            computedDigest = Truncate(computedDigest, truncateSize);
            Assert.Equal(digestBytes, computedDigest);

            using (HMAC hmac = Create())
            {
                byte[] key = (byte[])_testKeys[testCaseId].Clone();
                hmac.Key = key;

                hmac.TransformBlock(data, 0, data.Length, null, 0);
                hmac.Initialize();
                hmac.TransformBlock(data, 0, data.Length, null, 0);
                hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                computedDigest = hmac.Hash;
            }

            computedDigest = Truncate(computedDigest, truncateSize);
            Assert.Equal(digestBytes, computedDigest);

            // One shot - allocating and byte array inputs
            computedDigest = HashDataOneShot(_testKeys[testCaseId], data);

            computedDigest = Truncate(computedDigest, truncateSize);
            Assert.Equal(digestBytes, computedDigest);

            static byte[] Truncate(byte[] digest, int truncateSize)
            {
                if (truncateSize == -1)
                    return digest;

                return digest.AsSpan(0, truncateSize).ToArray();
            }
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

        [Fact]
        public void OneShot_NullKey_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () =>
                HashDataOneShot(key: (byte[])null, source: Array.Empty<byte>()));
        }

        [Fact]
        public void OneShot_NullSource_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () =>
                HashDataOneShot(key: Array.Empty<byte>(), source: (byte[])null));
        }

        [Fact]
        public void OneShot_ExistingBuffer_TooSmall()
        {
            byte[] buffer = new byte[MacSize - 1];
            byte[] key = _testKeys[1];
            byte[] data = _testData[1];

            AssertExtensions.Throws<ArgumentException>("destination", () =>
                HashDataOneShot(key, data, buffer));

            AssertExtensions.FilledWith<byte>(0, buffer);
        }

        [Fact]
        public void OneShot_TryExistingBuffer_TooSmall()
        {
            byte[] buffer = new byte[MacSize - 1];
            byte[] key = _testKeys[1];
            byte[] data = _testData[1];

            Assert.False(TryHashDataOneShot(key, data, buffer, out int written));
            Assert.Equal(0, written);
            AssertExtensions.FilledWith<byte>(0, buffer);
        }

        [Fact]
        public void OneShot_TryExistingBuffer_Exact()
        {
            for (int caseId = 1; caseId <= 7; caseId++)
            {
                byte[] buffer = new byte[MacSize];
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];

                Assert.True(TryHashDataOneShot(key, data, buffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedBuffer = buffer.AsSpan(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedBuffer);
            }
        }

        [Fact]
        public void OneShot_TryExistingBuffer_Larger()
        {
            for (int caseId = 1; caseId <= 7; caseId++)
            {
                Span<byte> buffer = new byte[MacSize + 20];
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];

                buffer.Fill(0xCC);
                Span<byte> writeBuffer = buffer.Slice(10, MacSize);

                Assert.True(TryHashDataOneShot(key, data, writeBuffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedWriteBuffer = writeBuffer.Slice(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedWriteBuffer);
                AssertExtensions.FilledWith<byte>(0xCC, buffer[..10]);
                AssertExtensions.FilledWith<byte>(0xCC, buffer[^10..]);
            }
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 0)]
        [InlineData(10, 20)]
        public void OneShot_TryExistingBuffer_OverlapsKey(int keyOffset, int bufferOffset)
        {
            for (int caseId = 1; caseId <= 7; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                Span<byte> buffer = new byte[Math.Max(key.Length, MacSize) + Math.Max(keyOffset, bufferOffset)];

                Span<byte> writeBuffer = buffer.Slice(bufferOffset, MacSize);
                Span<byte> keyBuffer = buffer.Slice(keyOffset, key.Length);
                key.AsSpan().CopyTo(keyBuffer);

                Assert.True(TryHashDataOneShot(keyBuffer, data, writeBuffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedWriteBuffer = writeBuffer.Slice(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedWriteBuffer);
            }
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 0)]
        [InlineData(10, 20)]
        public void OneShot_TryExistingBuffer_OverlapsSource(int sourceOffset, int bufferOffset)
        {
            for (int caseId = 1; caseId <= 7; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                Span<byte> buffer = new byte[Math.Max(data.Length, MacSize) + Math.Max(sourceOffset, bufferOffset)];

                Span<byte> writeBuffer = buffer.Slice(bufferOffset, MacSize);
                Span<byte> dataBuffer = buffer.Slice(sourceOffset, data.Length);
                data.AsSpan().CopyTo(dataBuffer);

                Assert.True(TryHashDataOneShot(key, dataBuffer, writeBuffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedWriteBuffer = writeBuffer.Slice(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedWriteBuffer);
            }
        }

        [Theory]
        [InlineData(new byte[0], new byte[] { 1 })]
        [InlineData(new byte[] { 1 }, new byte[0])]
        public void OneShot_Empty_Matches_Instances(byte[] key, byte[] source)
        {
            using (HMAC hash = Create())
            {
                hash.Key = key;
                byte[] mac = hash.ComputeHash(source, 0, source.Length);

                byte[] oneShot = HashDataOneShot(key, source);
                Assert.Equal(mac, oneShot);
            }
        }
    }
}
