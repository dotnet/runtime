// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Test.Cryptography;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Security.Cryptography.Tests
{
    public abstract class HmacTests<THmacTrait> where THmacTrait : IHmacTrait
    {
        public static bool IsSupported => THmacTrait.IsSupported;
        public static bool IsNotSupported => !IsSupported;

        private static void CheckIsSupported()
        {
            if (!IsSupported)
                throw new SkipTestException(nameof(IsSupported));
        }

        private static void CheckIsNotSupported()
        {
            if (!IsNotSupported)
                throw new SkipTestException(nameof(IsNotSupported));
        }

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
        protected abstract HMAC Create(byte[] key);

        protected abstract HashAlgorithm CreateHashAlgorithm();
        protected abstract byte[] HashDataOneShot(byte[] key, byte[] source);
        protected abstract byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source);
        protected abstract int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination);
        protected abstract bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written);
        protected abstract byte[] HashDataOneShot(ReadOnlySpan<byte> key, Stream source);
        protected abstract byte[] HashDataOneShot(byte[] key, Stream source);
        protected abstract int HashDataOneShot(ReadOnlySpan<byte> key, Stream source, Span<byte> destination);

        protected abstract bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, ReadOnlySpan<byte> hash);
        protected abstract bool Verify(byte[] key, byte[] source, byte[] hash);
        protected abstract bool Verify(ReadOnlySpan<byte> key, Stream source, ReadOnlySpan<byte> hash);
        protected abstract bool Verify(byte[] key, Stream source, byte[] hash);

        protected virtual ValueTask<bool> VerifyAsync(ReadOnlyMemory<byte> key, Stream source, ReadOnlyMemory<byte> hash, CancellationToken cancellationToken) => throw new NotImplementedException();
        protected virtual ValueTask<bool> VerifyAsync(byte[] key, Stream source, byte[] hash, CancellationToken cancellationToken) => throw new NotImplementedException();

        protected abstract ValueTask<int> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken);

        protected abstract ValueTask<byte[]> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken);

        protected abstract ValueTask<byte[]> HashDataOneShotAsync(
            byte[] key,
            Stream source,
            CancellationToken cancellationToken);

        protected abstract int BlockSize { get; }
        protected abstract int MacSize { get; }
        protected abstract HashAlgorithmName HashAlgorithm { get; }

        protected void VerifyRepeating(string input, int repeatCount, string hexKey, string output)
        {
            byte[] key = ByteUtils.HexToByteArray(hexKey);

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyHashDataStreamAllocating(key, stream, output, spanKey: true);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyHashDataStreamAllocating(key, stream, output, spanKey: false);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyHashDataStream(key, stream, output);
            }
            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyHashDataStreamAllocating_CryptographicOperations(key, stream, output, spanKey: true);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyHashDataStreamAllocating_CryptographicOperations(key, stream, output, spanKey: false);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyHashDataStream_CryptographicOperations(key, stream, output);
            }
        }

        protected async Task VerifyRepeatingAsync(string input, int repeatCount, string hexKey, string output)
        {
            byte[] key = ByteUtils.HexToByteArray(hexKey);

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                await VerifyHashDataStreamAllocatingAsync(key, stream, output, memoryKey: true);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                await VerifyHashDataStreamAllocatingAsync(key, stream, output, memoryKey: false);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                await VerifyHashDataStreamAsync(key, stream, output);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                await VerifyHashDataStreamAllocatingAsync_CryptographicOperations(key, stream, output, memoryKey: true);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                await VerifyHashDataStreamAllocatingAsync_CryptographicOperations(key, stream, output, memoryKey: false);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                await VerifyHashDataStreamAsync_CryptographicOperations(key, stream, output);
            }
        }

        protected void VerifyHashDataStream(ReadOnlySpan<byte> key, Stream stream, string output)
        {
            Span<byte> destination = stackalloc byte[MacSize];
            byte[] expected = ByteUtils.HexToByteArray(output);
            int written = HashDataOneShot(key, stream, destination);

            Assert.Equal(MacSize, written);
            AssertExtensions.SequenceEqual(expected.AsSpan(), destination);
        }

        protected void VerifyHashDataStream_CryptographicOperations(ReadOnlySpan<byte> key, Stream stream, string output)
        {
            Span<byte> destination = stackalloc byte[MacSize];
            byte[] expected = ByteUtils.HexToByteArray(output);
            int written = CryptographicOperations.HmacData(HashAlgorithm, key, stream, destination);

            Assert.Equal(MacSize, written);
            AssertExtensions.SequenceEqual(expected.AsSpan(), destination);
        }

        protected async Task VerifyHashDataStreamAsync(ReadOnlyMemory<byte> key, Stream stream, string output)
        {
            Memory<byte> destination = new byte[MacSize];
            byte[] expected = ByteUtils.HexToByteArray(output);
            int written = await HashDataOneShotAsync(key, stream, destination, cancellationToken: default);

            Assert.Equal(MacSize, written);
            AssertExtensions.SequenceEqual(expected.AsSpan(), destination.Span);
        }

        protected async Task VerifyHashDataStreamAsync_CryptographicOperations(ReadOnlyMemory<byte> key, Stream stream, string output)
        {
            Memory<byte> destination = new byte[MacSize];
            byte[] expected = ByteUtils.HexToByteArray(output);
            int written = await CryptographicOperations.HmacDataAsync(
                HashAlgorithm,
                key,
                stream,
                destination,
                cancellationToken: default);

            Assert.Equal(MacSize, written);
            AssertExtensions.SequenceEqual(expected.AsSpan(), destination.Span);
        }

        protected void VerifyHashDataStreamAllocating(byte[] key, Stream stream, string output, bool spanKey)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] hmac;

            if (spanKey)
            {
                hmac = HashDataOneShot(key.AsSpan(), stream);
            }
            else
            {
                hmac = HashDataOneShot(key, stream);
            }

            Assert.Equal(expected, hmac);
        }

        protected void VerifyHashDataStreamAllocating_CryptographicOperations(byte[] key, Stream stream, string output, bool spanKey)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] hmac;

            if (spanKey)
            {
                hmac = CryptographicOperations.HmacData(HashAlgorithm, key.AsSpan(), stream);
            }
            else
            {
                hmac = CryptographicOperations.HmacData(HashAlgorithm, key, stream);
            }

            Assert.Equal(expected, hmac);
        }

        protected async Task VerifyHashDataStreamAllocatingAsync(byte[] key, Stream stream, string output, bool memoryKey)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] hmac;

            if (memoryKey)
            {
                hmac = await HashDataOneShotAsync(new ReadOnlyMemory<byte>(key), stream, cancellationToken: default);
            }
            else
            {
                hmac = await HashDataOneShotAsync(key, stream, cancellationToken: default);
            }

            Assert.Equal(expected, hmac);
        }

        protected async Task VerifyHashDataStreamAllocatingAsync_CryptographicOperations(byte[] key, Stream stream, string output, bool memoryKey)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] hmac;

            if (memoryKey)
            {
                hmac = await CryptographicOperations.HmacDataAsync(HashAlgorithm, new ReadOnlyMemory<byte>(key), stream, cancellationToken: default);
            }
            else
            {
                hmac = await CryptographicOperations.HmacDataAsync(HashAlgorithm, key, stream, cancellationToken: default);
            }

            Assert.Equal(expected, hmac);
        }

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

            // CryptographicOperations one shot
            computedDigest = CryptographicOperations.HmacData(HashAlgorithm, _testKeys[testCaseId], data);
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
                byte[] expectedKey;
                using (HashAlgorithm hash = CreateHashAlgorithm())
                {
                    expectedKey = hash.ComputeHash(overSizedKey);
                }
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

        [ConditionalFact]
        public void InvalidInput_Null()
        {
            CheckIsSupported();
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash(null, 0, 0));
                Assert.Throws<NullReferenceException>(() => hash.ComputeHash((Stream)null));
            }
        }

        [ConditionalFact]
        public void InvalidInput_NegativeOffset()
        {
            CheckIsSupported();
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => hash.ComputeHash(Array.Empty<byte>(), -1, 0));
            }
        }

        [ConditionalFact]
        public void InvalidInput_NegativeCount()
        {
            CheckIsSupported();
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, -1));
            }
        }

        [ConditionalFact]
        public void InvalidInput_TooBigOffset()
        {
            CheckIsSupported();
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 1, 0));
            }
        }

        [ConditionalFact]
        public void InvalidInput_TooBigCount()
        {
            CheckIsSupported();
            byte[] nonEmpty = new byte[53];

            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 0, nonEmpty.Length + 1));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 1, nonEmpty.Length));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 2, nonEmpty.Length - 1));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, 1));
            }
        }

        [ConditionalFact]
        public void BoundaryCondition_Count0()
        {
            CheckIsSupported();
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

        [ConditionalFact]
        public void OffsetAndCountRespected()
        {
            CheckIsSupported();
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

        [ConditionalFact]
        public void InvalidKey_ThrowArgumentNullException()
        {
            CheckIsSupported();
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => hash.Key = null);
            }
        }

        [ConditionalFact]
        public void OneShot_NullKey_ArgumentNullException()
        {
            CheckIsSupported();
            AssertExtensions.Throws<ArgumentNullException>("key", () =>
                HashDataOneShot(key: (byte[])null, source: Array.Empty<byte>()));

            AssertExtensions.Throws<ArgumentNullException>("key", () =>
                CryptographicOperations.HmacData(HashAlgorithm, key: (byte[])null, source: Array.Empty<byte>()));
        }

        [ConditionalFact]
        public void OneShot_NullSource_ArgumentNullException()
        {
            CheckIsSupported();
            AssertExtensions.Throws<ArgumentNullException>("source", () =>
                HashDataOneShot(key: Array.Empty<byte>(), source: (byte[])null));

            AssertExtensions.Throws<ArgumentNullException>("source", () =>
                CryptographicOperations.HmacData(HashAlgorithm, key: Array.Empty<byte>(), source: (byte[])null));
        }

        [ConditionalFact]
        public void OneShot_ExistingBuffer_TooSmall()
        {
            CheckIsSupported();
            byte[] buffer = new byte[MacSize - 1];
            byte[] key = _testKeys[1];
            byte[] data = _testData[1];

            AssertExtensions.Throws<ArgumentException>("destination", () =>
                HashDataOneShot(key, data, buffer));

            AssertExtensions.FilledWith<byte>(0, buffer);

            AssertExtensions.Throws<ArgumentException>("destination", () =>
                CryptographicOperations.HmacData(HashAlgorithm, key, data, buffer));

            AssertExtensions.FilledWith<byte>(0, buffer);
        }

        [ConditionalFact]
        public void OneShot_TryExistingBuffer_TooSmall()
        {
            CheckIsSupported();
            byte[] buffer = new byte[MacSize - 1];
            byte[] key = _testKeys[1];
            byte[] data = _testData[1];

            Assert.False(TryHashDataOneShot(key, data, buffer, out int written));
            Assert.Equal(0, written);
            AssertExtensions.FilledWith<byte>(0, buffer);

            Assert.False(CryptographicOperations.TryHmacData(HashAlgorithm, key, data, buffer, out written));
            Assert.Equal(0, written);
            AssertExtensions.FilledWith<byte>(0, buffer);
        }

        [ConditionalFact]
        public void OneShot_TryExistingBuffer_Exact()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
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

            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] buffer = new byte[MacSize];
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];

                Assert.True(CryptographicOperations.TryHmacData(HashAlgorithm, key, data, buffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedBuffer = buffer.AsSpan(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedBuffer);
            }
        }

        [ConditionalFact]
        public void OneShot_TryExistingBuffer_Larger()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
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

            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                Span<byte> buffer = new byte[MacSize + 20];
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];

                buffer.Fill(0xCC);
                Span<byte> writeBuffer = buffer.Slice(10, MacSize);

                Assert.True(CryptographicOperations.TryHmacData(HashAlgorithm, key, data, writeBuffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedWriteBuffer = writeBuffer.Slice(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedWriteBuffer);
                AssertExtensions.FilledWith<byte>(0xCC, buffer[..10]);
                AssertExtensions.FilledWith<byte>(0xCC, buffer[^10..]);
            }
        }

        [ConditionalTheory]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 0)]
        [InlineData(10, 20)]
        public void OneShot_TryExistingBuffer_OverlapsKey(int keyOffset, int bufferOffset)
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
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

            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                Span<byte> buffer = new byte[Math.Max(key.Length, MacSize) + Math.Max(keyOffset, bufferOffset)];

                Span<byte> writeBuffer = buffer.Slice(bufferOffset, MacSize);
                Span<byte> keyBuffer = buffer.Slice(keyOffset, key.Length);
                key.AsSpan().CopyTo(keyBuffer);

                Assert.True(CryptographicOperations.TryHmacData(HashAlgorithm, keyBuffer, data, writeBuffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedWriteBuffer = writeBuffer.Slice(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedWriteBuffer);
            }
        }

        [ConditionalTheory]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 0)]
        [InlineData(10, 20)]
        public void OneShot_TryExistingBuffer_OverlapsSource(int sourceOffset, int bufferOffset)
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
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

            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                Span<byte> buffer = new byte[Math.Max(data.Length, MacSize) + Math.Max(sourceOffset, bufferOffset)];

                Span<byte> writeBuffer = buffer.Slice(bufferOffset, MacSize);
                Span<byte> dataBuffer = buffer.Slice(sourceOffset, data.Length);
                data.AsSpan().CopyTo(dataBuffer);

                Assert.True(CryptographicOperations.TryHmacData(HashAlgorithm, key, dataBuffer, writeBuffer, out int written));
                Assert.Equal(MacSize, written);

                ReadOnlySpan<byte> expectedMac = _testMacs[caseId];
                Span<byte> truncatedWriteBuffer = writeBuffer.Slice(0, expectedMac.Length);
                AssertExtensions.SequenceEqual(expectedMac, truncatedWriteBuffer);
            }
        }

        [ConditionalTheory]
        [InlineData(new byte[0], new byte[] { 1 })]
        [InlineData(new byte[] { 1 }, new byte[0])]
        public void OneShot_Empty_Matches_Instances(byte[] key, byte[] source)
        {
            CheckIsSupported();
            using (HMAC hash = Create())
            {
                hash.Key = key;
                byte[] mac = hash.ComputeHash(source, 0, source.Length);

                byte[] oneShot = HashDataOneShot(key, source);
                Assert.Equal(mac, oneShot);

                byte[] cryptographicOperationsOneShot = CryptographicOperations.HmacData(HashAlgorithm, key, source);
                Assert.Equal(mac, cryptographicOperationsOneShot);
            }
        }

        [ConditionalFact]
        public void HashData_Stream_Source_Null()
        {
            CheckIsSupported();
            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => HashDataOneShot(ReadOnlySpan<byte>.Empty, (Stream)null));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => HashDataOneShot(Array.Empty<byte>(), (Stream)null));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => CryptographicOperations.HmacData(HashAlgorithm, Array.Empty<byte>(), (Stream)null));
        }

        [ConditionalFact]
        public void HashData_Stream_Source_Null_Async()
        {
            CheckIsSupported();
            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => HashDataOneShotAsync(ReadOnlyMemory<byte>.Empty, (Stream)null, default));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => HashDataOneShotAsync(Array.Empty<byte>(), (Stream)null, default));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => CryptographicOperations.HmacDataAsync(HashAlgorithm, Array.Empty<byte>(), (Stream)null, default));
        }

        [ConditionalFact]
        public void HashData_Stream_ByteKey_Null()
        {
            CheckIsSupported();
            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => HashDataOneShot((byte[])null, Stream.Null));

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => CryptographicOperations.HmacData(HashAlgorithm, (byte[])null, Stream.Null));
        }

        [ConditionalFact]
        public void HashData_Stream_ByteKey_Null_Async()
        {
            CheckIsSupported();
            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => HashDataOneShotAsync((byte[])null, Stream.Null, default));

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => CryptographicOperations.HmacDataAsync(HashAlgorithm, (byte[])null, Stream.Null, default));
        }

        [ConditionalFact]
        public void HashData_Stream_DestinationTooSmall()
        {
            CheckIsSupported();
            byte[] destination = new byte[MacSize - 1];

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => HashDataOneShot(Array.Empty<byte>(), Stream.Null, destination));
            AssertExtensions.FilledWith<byte>(0, destination);

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => HashDataOneShot(ReadOnlySpan<byte>.Empty, Stream.Null, destination));
            AssertExtensions.FilledWith<byte>(0, destination);

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => CryptographicOperations.HmacData(HashAlgorithm, ReadOnlySpan<byte>.Empty, Stream.Null, destination));
            AssertExtensions.FilledWith<byte>(0, destination);
        }

        [ConditionalFact]
        public void HashData_Stream_DestinationTooSmall_Async()
        {
            CheckIsSupported();
            byte[] destination = new byte[MacSize - 1];

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => HashDataOneShotAsync(Array.Empty<byte>(), Stream.Null, destination, default));
            AssertExtensions.FilledWith<byte>(0, destination);

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => HashDataOneShotAsync(ReadOnlyMemory<byte>.Empty, Stream.Null, destination, default));
            AssertExtensions.FilledWith<byte>(0, destination);

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => CryptographicOperations.HmacDataAsync(HashAlgorithm, ReadOnlyMemory<byte>.Empty, Stream.Null, destination, default));
            AssertExtensions.FilledWith<byte>(0, destination);
        }

        [ConditionalFact]
        public void HashData_Stream_NotReadable()
        {
            CheckIsSupported();
            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => HashDataOneShot(Array.Empty<byte>(), UntouchableStream.Instance));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => HashDataOneShot(ReadOnlySpan<byte>.Empty, UntouchableStream.Instance));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => CryptographicOperations.HmacData(HashAlgorithm, ReadOnlySpan<byte>.Empty, UntouchableStream.Instance));
        }

        [ConditionalFact]
        public void HashData_Stream_Cancelled()
        {
            CheckIsSupported();
            Memory<byte> buffer = new byte[512 / 8];
            CancellationToken cancelledToken = new CancellationToken(canceled: true);
            ValueTask<int> waitable = HashDataOneShotAsync(ReadOnlyMemory<byte>.Empty, Stream.Null, buffer, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));
            AssertExtensions.FilledWith<byte>(0, buffer.Span);

            waitable = HashDataOneShotAsync(Array.Empty<byte>(), Stream.Null, buffer, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));
            AssertExtensions.FilledWith<byte>(0, buffer.Span);

            waitable = CryptographicOperations.HmacDataAsync(HashAlgorithm, ReadOnlyMemory<byte>.Empty, Stream.Null, buffer, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));
            AssertExtensions.FilledWith<byte>(0, buffer.Span);

            waitable = CryptographicOperations.HmacDataAsync(HashAlgorithm, Array.Empty<byte>(), Stream.Null, buffer, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));
            AssertExtensions.FilledWith<byte>(0, buffer.Span);
        }

        [ConditionalFact]
        public void HashData_Stream_Allocating_Cancelled()
        {
            CheckIsSupported();
            CancellationToken cancelledToken = new CancellationToken(canceled: true);
            ValueTask<byte[]> waitable = HashDataOneShotAsync(ReadOnlyMemory<byte>.Empty, Stream.Null, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));

            waitable = CryptographicOperations.HmacDataAsync(HashAlgorithm, ReadOnlyMemory<byte>.Empty, Stream.Null, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));
        }

        [ConditionalTheory]
        [InlineData(-1)]
        [InlineData(1)]
        public void Verify_ArgValidation_WrongHashSize(int sizeOffset)
        {
            CheckIsSupported();
            byte[] key = new byte[1];
            Assert.Throws<ArgumentException>("hash", () =>
                Verify(key, Array.Empty<byte>(), new byte[THmacTrait.HashSizeInBytes + sizeOffset]));
            Assert.Throws<ArgumentException>("hash", () =>
                Verify(new ReadOnlySpan<byte>(key), ReadOnlySpan<byte>.Empty, new byte[THmacTrait.HashSizeInBytes + sizeOffset]));

            Assert.Throws<ArgumentException>("hash", () =>
                Verify(key, UntouchableStream.Instance, new byte[THmacTrait.HashSizeInBytes + sizeOffset]));
            Assert.Throws<ArgumentException>("hash", () =>
                Verify(new ReadOnlySpan<byte>(key), UntouchableStream.Instance, new byte[THmacTrait.HashSizeInBytes + sizeOffset]));

            Assert.Throws<ArgumentException>("hash", () =>
                VerifyAsync(key, UntouchableStream.Instance, new byte[THmacTrait.HashSizeInBytes + sizeOffset], default(CancellationToken)));
            Assert.Throws<ArgumentException>("hash", () =>
                VerifyAsync(new ReadOnlyMemory<byte>(key), UntouchableStream.Instance, new byte[THmacTrait.HashSizeInBytes + sizeOffset], default(CancellationToken)));
        }

        [ConditionalTheory]
        [InlineData(-1)]
        [InlineData(1)]
        public void Verify_CryptographicOperations_ArgValidation_WrongHashSize(int sizeOffset)
        {
            CheckIsSupported();
            byte[] key = new byte[1];
            Assert.Throws<ArgumentException>("hash", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    key,
                    Array.Empty<byte>(),
                    new byte[THmacTrait.HashSizeInBytes + sizeOffset]));

            Assert.Throws<ArgumentException>("hash", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    new ReadOnlySpan<byte>(key),
                    ReadOnlySpan<byte>.Empty,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes + sizeOffset])));

            Assert.Throws<ArgumentException>("hash", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    key,
                    UntouchableStream.Instance,
                    new byte[THmacTrait.HashSizeInBytes + sizeOffset]));

            Assert.Throws<ArgumentException>("hash", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    new ReadOnlySpan<byte>(key),
                    UntouchableStream.Instance,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes + sizeOffset])));
        }

        [ConditionalFact]
        public void Verify_CryptographicOperations_ArgValidation_Null()
        {
            CheckIsSupported();
            Assert.Throws<ArgumentNullException>("key", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    null,
                    Array.Empty<byte>(),
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentNullException>("source", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    Array.Empty<byte>(),
                    (byte[])null,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentNullException>("hash", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    null));

            Assert.Throws<ArgumentNullException>("source", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    ReadOnlySpan<byte>.Empty,
                    (Stream)null,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentNullException>("key", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    null,
                    UntouchableStream.Instance,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentNullException>("source", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    Array.Empty<byte>(),
                    (Stream)null,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentNullException>("hash", () =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    Array.Empty<byte>(),
                    UntouchableStream.Instance,
                    null));
        }

        [ConditionalFact]
        public void Verify_CryptographicOperations_ArgValidation_HashName_Invalid()
        {
            CheckIsSupported();
            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    default(HashAlgorithmName),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    default(HashAlgorithmName),
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName(""),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName(""),
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    default(HashAlgorithmName),
                    Array.Empty<byte>(),
                    UntouchableStream.Instance,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    default(HashAlgorithmName),
                    ReadOnlySpan<byte>.Empty,
                    UntouchableStream.Instance,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName(""),
                    Array.Empty<byte>(),
                    UntouchableStream.Instance,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName(""),
                    ReadOnlySpan<byte>.Empty,
                    UntouchableStream.Instance,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));
        }

        [Fact]
        public void Verify_CryptographicOperations_HashName_Unknown()
        {
            Assert.Throws<CryptographicException>(() =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName("POTATO256"),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<CryptographicException>(() =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName("POTATO256"),
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));

            Assert.Throws<CryptographicException>(() =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName("POTATO256"),
                    Array.Empty<byte>(),
                    UntouchableStream.Instance,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<CryptographicException>(() =>
                CryptographicOperations.VerifyHmac(
                    new HashAlgorithmName("POTATO256"),
                    ReadOnlySpan<byte>.Empty,
                    UntouchableStream.Instance,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));
        }

        [ConditionalFact]
        public void Verify_CryptographicOperations_HashName_NotSupported()
        {
            CheckIsNotSupported();
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));

            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    Array.Empty<byte>(),
                    UntouchableStream.Instance,
                    new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.VerifyHmac(
                    HashAlgorithm,
                    ReadOnlySpan<byte>.Empty,
                    UntouchableStream.Instance,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));
        }

        [ConditionalFact]
        public void Verify_ArgValidation_Null()
        {
            CheckIsSupported();
            byte[] key = new byte[1];
            Assert.Throws<ArgumentNullException>("key", () =>
                Verify(null, Array.Empty<byte>(), new byte[THmacTrait.HashSizeInBytes]));
            Assert.Throws<ArgumentNullException>("source", () =>
                Verify(key, (byte[])null, new byte[THmacTrait.HashSizeInBytes]));
            Assert.Throws<ArgumentNullException>("hash", () =>
                Verify(key, Array.Empty<byte>(), null));

            Assert.Throws<ArgumentNullException>("key", () =>
                Verify(null, UntouchableStream.Instance, new byte[THmacTrait.HashSizeInBytes]));
            Assert.Throws<ArgumentNullException>("source", () =>
                Verify(key, (Stream)null, new byte[THmacTrait.HashSizeInBytes]));
            Assert.Throws<ArgumentNullException>("hash", () =>
                Verify(key, UntouchableStream.Instance, null));

            Assert.Throws<ArgumentNullException>("source", () =>
                Verify(
                    new ReadOnlySpan<byte>(key),
                    (Stream)null,
                    new ReadOnlySpan<byte>(new byte[THmacTrait.HashSizeInBytes])));

            Assert.Throws<ArgumentNullException>("key", () =>
                VerifyAsync(null, UntouchableStream.Instance, new byte[THmacTrait.HashSizeInBytes], default(CancellationToken)));
            Assert.Throws<ArgumentNullException>("source", () =>
                VerifyAsync(key, (Stream)null, new byte[THmacTrait.HashSizeInBytes], default(CancellationToken)));
            Assert.Throws<ArgumentNullException>("hash", () =>
                VerifyAsync(key, UntouchableStream.Instance, null, default(CancellationToken)));

            Assert.Throws<ArgumentNullException>("source", () =>
                VerifyAsync(
                    new ReadOnlyMemory<byte>(key),
                    (Stream)null,
                    new ReadOnlyMemory<byte>(new byte[THmacTrait.HashSizeInBytes]), default(CancellationToken)));
        }

        [ConditionalFact]
        public void Verify_Match()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId];

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                ReadOnlySpan<byte> keySpan = key;
                ReadOnlySpan<byte> dataSpan = data;
                ReadOnlySpan<byte> macSpan = mac;

                // Array
                AssertExtensions.TrueExpression(Verify(key, data, mac));

                // Span
                AssertExtensions.TrueExpression(Verify(keySpan, dataSpan, macSpan));

                // Stream, arrays
                AssertExtensions.TrueExpression(Verify(key, new MemoryStream(data), mac));

                // Stream, spans
                AssertExtensions.TrueExpression(Verify(keySpan, new MemoryStream(data), macSpan));
            }
        }

        [ConditionalFact]
        public async Task VerifyAsync_Match()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId];

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                ReadOnlyMemory<byte> keyMemory = key;
                ReadOnlyMemory<byte> macMemory = mac;

                // Array
                AssertExtensions.TrueExpression(
                    await VerifyAsync(key, new MemoryStream(data), mac, default(CancellationToken)));

                // Memory
                AssertExtensions.TrueExpression(
                    await VerifyAsync(keyMemory, new MemoryStream(data), macMemory, default(CancellationToken)));
            }
        }

        [ConditionalFact]
        public void Verify_Mismatch()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId].AsSpan().ToArray();

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                FlipRandomBit(mac);
                ReadOnlySpan<byte> keySpan = key;
                ReadOnlySpan<byte> dataSpan = data;
                ReadOnlySpan<byte> macSpan = mac;

                // Array
                AssertExtensions.FalseExpression(Verify(key, data, mac));

                // Span
                AssertExtensions.FalseExpression(Verify(keySpan, dataSpan, macSpan));

                // Stream, arrays
                AssertExtensions.FalseExpression(Verify(key, new MemoryStream(data), mac));

                // Stream, spans
                AssertExtensions.FalseExpression(Verify(keySpan, new MemoryStream(data), macSpan));
            }
        }

        [ConditionalFact]
        public async Task VerifyAsync_Mismatch()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId].AsSpan().ToArray();

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                FlipRandomBit(mac);
                ReadOnlyMemory<byte> keyMemory = key;
                ReadOnlyMemory<byte> macMemory = mac;

                // Array
                AssertExtensions.FalseExpression(
                    await VerifyAsync(key, new MemoryStream(data), mac, default(CancellationToken)));

                // Memory
                AssertExtensions.FalseExpression(
                    await VerifyAsync(keyMemory, new MemoryStream(data), macMemory, default(CancellationToken)));
            }
        }

        [ConditionalFact]
        public async Task VerifyAsync_Cancelled()
        {
            CheckIsSupported();
            CancellationToken cancelledToken = new(true);
            byte[] hash = new byte[THmacTrait.HashSizeInBytes];

            ValueTask<bool> arrayVerify = VerifyAsync(Array.Empty<byte>(), Stream.Null, hash, cancelledToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await arrayVerify);

            ValueTask<bool> memoryVerify = VerifyAsync(
                ReadOnlyMemory<byte>.Empty,
                Stream.Null,
                new ReadOnlyMemory<byte>(hash),
                cancelledToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await memoryVerify);
        }

        [ConditionalFact]
        public async Task VerifyHmacAsync_CryptographicOperations_Cancelled()
        {
            CheckIsSupported();
            CancellationToken cancelledToken = new(true);
            byte[] hash = new byte[THmacTrait.HashSizeInBytes];


            ValueTask<bool> arrayVerify = CryptographicOperations.VerifyHmacAsync(
                HashAlgorithm,
                Array.Empty<byte>(),
                Stream.Null,
                hash,
                cancelledToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await arrayVerify);

            ValueTask<bool> memoryVerify = CryptographicOperations.VerifyHmacAsync(
                HashAlgorithm,
                ReadOnlyMemory<byte>.Empty,
                Stream.Null,
                new ReadOnlyMemory<byte>(hash),
                cancelledToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await memoryVerify);
        }

        [ConditionalFact]
        public void Verify_CryptographicOperations_Match()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId];

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                ReadOnlySpan<byte> keySpan = key;
                ReadOnlySpan<byte> dataSpan = data;
                ReadOnlySpan<byte> macSpan = mac;

                // Array
                AssertExtensions.TrueExpression(CryptographicOperations.VerifyHmac(HashAlgorithm, key, data, mac));

                // Span
                AssertExtensions.TrueExpression(CryptographicOperations.VerifyHmac(HashAlgorithm, keySpan, dataSpan, macSpan));

                // Stream, arrays
                AssertExtensions.TrueExpression(Verify(key, new MemoryStream(data), mac));

                // Stream, spans
                AssertExtensions.TrueExpression(Verify(keySpan, new MemoryStream(data), macSpan));
            }
        }

        [ConditionalFact]
        public void Verify_CryptographicOperations_Mismatch()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId].AsSpan().ToArray();

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                FlipRandomBit(mac);
                ReadOnlySpan<byte> keySpan = key;
                ReadOnlySpan<byte> dataSpan = data;
                ReadOnlySpan<byte> macSpan = mac;

                // Array
                AssertExtensions.FalseExpression(CryptographicOperations.VerifyHmac(HashAlgorithm, key, data, mac));

                // Span
                AssertExtensions.FalseExpression(CryptographicOperations.VerifyHmac(HashAlgorithm, keySpan, dataSpan, macSpan));

                // Stream, arrays
                AssertExtensions.FalseExpression(Verify(key, new MemoryStream(data), mac));

                // Stream, spans
                AssertExtensions.FalseExpression(Verify(keySpan, new MemoryStream(data), macSpan));
            }
        }

        [ConditionalFact]
        public async Task VerifyAsync_CryptographicOperations_Match()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId];

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                ReadOnlyMemory<byte> keyMemory = key;
                ReadOnlyMemory<byte> macMemory = mac;

                // Stream, arrays
                AssertExtensions.TrueExpression(
                    await CryptographicOperations.VerifyHmacAsync(HashAlgorithm, key, new MemoryStream(data), mac));

                // Stream, memory
                AssertExtensions.TrueExpression(
                    await CryptographicOperations.VerifyHmacAsync(HashAlgorithm, keyMemory, new MemoryStream(data), macMemory));
            }
        }

        [ConditionalFact]
        public async Task VerifyAsync_CryptographicOperations_Mismatch()
        {
            CheckIsSupported();
            for (int caseId = 1; caseId < _testKeys.Length; caseId++)
            {
                byte[] key = _testKeys[caseId];
                byte[] data = _testData[caseId];
                byte[] mac = _testMacs[caseId].AsSpan().ToArray();

                if (mac.Length != THmacTrait.HashSizeInBytes)
                {
                    // Some test vectors are truncated MACs. Skip them since Verify does not support truncated values.
                    continue;
                }

                FlipRandomBit(mac);
                ReadOnlyMemory<byte> keyMemory = key;
                ReadOnlyMemory<byte> macMemory = mac;

                // Stream, arrays
                AssertExtensions.FalseExpression(
                    await CryptographicOperations.VerifyHmacAsync(
                        HashAlgorithm,
                        key,
                        new MemoryStream(data),
                        mac));

                // Stream, spans
                AssertExtensions.FalseExpression(
                    await CryptographicOperations.VerifyHmacAsync(
                        HashAlgorithm,
                        new ReadOnlyMemory<byte>(key),
                        new MemoryStream(data),
                        new ReadOnlyMemory<byte>(mac)));
            }
        }

        [ConditionalFact]
        public void Ctor_NotSupported()
        {
            CheckIsNotSupported();
            Assert.Throws<PlatformNotSupportedException>(() => Create());
            Assert.Throws<PlatformNotSupportedException>(() => Create(new byte[42]));
        }

        [ConditionalFact]
        public async Task HashData_NotSupported()
        {
            CheckIsNotSupported();
            byte[] key = new byte[1];
            byte[] buffer = new byte[THmacTrait.HashSizeInBytes];
            Assert.Throws<PlatformNotSupportedException>(() => HashDataOneShot(key, Array.Empty<byte>()));
            Assert.Throws<PlatformNotSupportedException>(() => HashDataOneShot(key, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => HashDataOneShot(key, ReadOnlySpan<byte>.Empty, buffer));
            Assert.Throws<PlatformNotSupportedException>(() => TryHashDataOneShot(key, ReadOnlySpan<byte>.Empty, buffer, out _));

            Assert.Throws<PlatformNotSupportedException>(() => HashDataOneShot(key, Stream.Null));
            Assert.Throws<PlatformNotSupportedException>(() => HashDataOneShot(key, Stream.Null, buffer));
            await Assert.ThrowsAsync<PlatformNotSupportedException>(async () =>
                await HashDataOneShotAsync(key, Stream.Null, default(CancellationToken)));
            await Assert.ThrowsAsync<PlatformNotSupportedException>(async () =>
                await HashDataOneShotAsync(key, Stream.Null, buffer, default(CancellationToken)));

            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacData(HashAlgorithm, key, Array.Empty<byte>()));
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacData(HashAlgorithm, key, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacData(HashAlgorithm, key, ReadOnlySpan<byte>.Empty, buffer));
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.TryHmacData(HashAlgorithm, key, ReadOnlySpan<byte>.Empty, buffer, out _));

            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacData(HashAlgorithm, key, Stream.Null));
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacData(HashAlgorithm, new ReadOnlySpan<byte>(key), Stream.Null));
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacData(HashAlgorithm, key, Stream.Null, buffer));

            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacDataAsync(HashAlgorithm, key, Stream.Null));
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacDataAsync(HashAlgorithm, new ReadOnlyMemory<byte>(key), Stream.Null));
            Assert.Throws<PlatformNotSupportedException>(() =>
                CryptographicOperations.HmacDataAsync(HashAlgorithm, key, Stream.Null, buffer));
        }

        [ConditionalFact]
        public void Verify_NotSupported()
        {
            CheckIsNotSupported();
            byte[] key = new byte[1];
            Assert.Throws<PlatformNotSupportedException>(() =>
                Verify(key, Array.Empty<byte>(), new byte[THmacTrait.HashSizeInBytes]));
            Assert.Throws<PlatformNotSupportedException>(() =>
                Verify(new ReadOnlySpan<byte>(key), ReadOnlySpan<byte>.Empty, new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() =>
                Verify(key, Stream.Null, new byte[THmacTrait.HashSizeInBytes]));
            Assert.Throws<PlatformNotSupportedException>(() =>
                Verify(new ReadOnlySpan<byte>(key), Stream.Null, new byte[THmacTrait.HashSizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() =>
                VerifyAsync(key, Stream.Null, new byte[THmacTrait.HashSizeInBytes], default(CancellationToken)));

            Assert.Throws<PlatformNotSupportedException>(() =>
                VerifyAsync(
                    new ReadOnlyMemory<byte>(key),
                    Stream.Null,
                    new byte[THmacTrait.HashSizeInBytes],
                    default(CancellationToken)));
        }

        private static void FlipRandomBit(Span<byte> input)
        {
            int index = Random.Shared.Next(0, input.Length);
            input[index] = (byte)(input[index] ^ 0b_10000000);
        }
    }

    public interface IHmacTrait
    {
        static abstract bool IsSupported { get; }
        static abstract int HashSizeInBytes { get; }
    }
}
