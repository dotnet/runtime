// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class HmacTests<THmacTrait> where THmacTrait : IHmacTrait
    {
        public static bool IsSupported => THmacTrait.IsSupported;
        public static bool IsNotSupported => !IsSupported;

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
            AssertExtensions.SequenceEqual(expected, destination);
        }

        protected void VerifyHashDataStream_CryptographicOperations(ReadOnlySpan<byte> key, Stream stream, string output)
        {
            Span<byte> destination = stackalloc byte[MacSize];
            byte[] expected = ByteUtils.HexToByteArray(output);
            int written = CryptographicOperations.HmacData(HashAlgorithm, key, stream, destination);

            Assert.Equal(MacSize, written);
            AssertExtensions.SequenceEqual(expected, destination);
        }

        protected async Task VerifyHashDataStreamAsync(ReadOnlyMemory<byte> key, Stream stream, string output)
        {
            Memory<byte> destination = new byte[MacSize];
            byte[] expected = ByteUtils.HexToByteArray(output);
            int written = await HashDataOneShotAsync(key, stream, destination, cancellationToken: default);

            Assert.Equal(MacSize, written);
            AssertExtensions.SequenceEqual(expected, destination.Span);
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
            AssertExtensions.SequenceEqual(expected, destination.Span);
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

        [ConditionalFact(nameof(IsSupported))]
        public void InvalidInput_Null()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash(null, 0, 0));
                Assert.Throws<NullReferenceException>(() => hash.ComputeHash((Stream)null));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void InvalidInput_NegativeOffset()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => hash.ComputeHash(Array.Empty<byte>(), -1, 0));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void InvalidInput_NegativeCount()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, -1));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void InvalidInput_TooBigOffset()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 1, 0));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
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

        [ConditionalFact(nameof(IsSupported))]
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

        [ConditionalFact(nameof(IsSupported))]
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

        [ConditionalFact(nameof(IsSupported))]
        public void InvalidKey_ThrowArgumentNullException()
        {
            using (HMAC hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => hash.Key = null);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void OneShot_NullKey_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () =>
                HashDataOneShot(key: (byte[])null, source: Array.Empty<byte>()));

            AssertExtensions.Throws<ArgumentNullException>("key", () =>
                CryptographicOperations.HmacData(HashAlgorithm, key: (byte[])null, source: Array.Empty<byte>()));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void OneShot_NullSource_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () =>
                HashDataOneShot(key: Array.Empty<byte>(), source: (byte[])null));

            AssertExtensions.Throws<ArgumentNullException>("source", () =>
                CryptographicOperations.HmacData(HashAlgorithm, key: Array.Empty<byte>(), source: (byte[])null));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void OneShot_ExistingBuffer_TooSmall()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void OneShot_TryExistingBuffer_TooSmall()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void OneShot_TryExistingBuffer_Exact()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void OneShot_TryExistingBuffer_Larger()
        {
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

        [ConditionalTheory(nameof(IsSupported))]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 0)]
        [InlineData(10, 20)]
        public void OneShot_TryExistingBuffer_OverlapsKey(int keyOffset, int bufferOffset)
        {
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

        [ConditionalTheory(nameof(IsSupported))]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 0)]
        [InlineData(10, 20)]
        public void OneShot_TryExistingBuffer_OverlapsSource(int sourceOffset, int bufferOffset)
        {
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

        [ConditionalTheory(nameof(IsSupported))]
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

                byte[] cryptographicOperationsOneShot = CryptographicOperations.HmacData(HashAlgorithm, key, source);
                Assert.Equal(mac, cryptographicOperationsOneShot);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_Source_Null()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_Source_Null_Async()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_ByteKey_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => HashDataOneShot((byte[])null, Stream.Null));

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => CryptographicOperations.HmacData(HashAlgorithm, (byte[])null, Stream.Null));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_ByteKey_Null_Async()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => HashDataOneShotAsync((byte[])null, Stream.Null, default));

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => CryptographicOperations.HmacDataAsync(HashAlgorithm, (byte[])null, Stream.Null, default));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_DestinationTooSmall()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_DestinationTooSmall_Async()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_NotReadable()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_Cancelled()
        {
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

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Stream_Allocating_Cancelled()
        {
            CancellationToken cancelledToken = new CancellationToken(canceled: true);
            ValueTask<byte[]> waitable = HashDataOneShotAsync(ReadOnlyMemory<byte>.Empty, Stream.Null, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));

            waitable = CryptographicOperations.HmacDataAsync(HashAlgorithm, ReadOnlyMemory<byte>.Empty, Stream.Null, cancelledToken);
            Assert.True(waitable.IsCanceled, nameof(waitable.IsCanceled));
        }

        [ConditionalFact(nameof(IsNotSupported))]
        public void Ctor_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Create());
            Assert.Throws<PlatformNotSupportedException>(() => Create(new byte[42]));
        }

        [ConditionalFact(nameof(IsNotSupported))]
        public async Task HashData_NotSupported()
        {
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
    }

    public interface IHmacTrait
    {
        static abstract bool IsSupported { get; }
        static abstract int HashSizeInBytes { get; }
    }
}
