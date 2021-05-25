// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public abstract class HashAlgorithmTest
    {
        protected abstract HashAlgorithm Create();
        protected abstract bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten);
        protected abstract byte[] HashData(byte[] source);
        protected abstract byte[] HashData(ReadOnlySpan<byte> source);
        protected abstract int HashData(ReadOnlySpan<byte> source, Span<byte> destination);

        protected void Verify(string input, string output)
        {
            Verify(ByteUtils.AsciiBytes(input), output);
        }

        private void VerifyComputeHashStream(Stream input, string output)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] actual;

            using (HashAlgorithm hash = Create())
            {
                Assert.True(hash.HashSize > 0);
                actual = hash.ComputeHash(input);
            }

            Assert.Equal(expected, actual);
        }

        private void VerifyICryptoTransformStream(Stream input, string output)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] actual;

            using (HashAlgorithm hash = Create())
            using (CryptoStream cryptoStream = new CryptoStream(input, hash, CryptoStreamMode.Read))
            {
                byte[] buffer = new byte[1024]; // A different default than HashAlgorithm which uses 4K
                int bytesRead;
                while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // CryptoStream will build up the hash
                }

                actual = hash.Hash;
            }

            Assert.Equal(expected, actual);
        }

        protected void VerifyMultiBlock(string block1, string block2, string expectedHash, string emptyHash)
        {
            byte[] block1_bytes = ByteUtils.AsciiBytes(block1);
            byte[] block2_bytes = ByteUtils.AsciiBytes(block2);
            byte[] expected_bytes = ByteUtils.HexToByteArray(expectedHash);
            byte[] emptyHash_bytes = ByteUtils.HexToByteArray(emptyHash);

            VerifyTransformBlockOutput(block1_bytes, block2_bytes);
            VerifyTransformBlockHash(block1_bytes, block2_bytes, expected_bytes, emptyHash_bytes);
            VerifyTransformBlockComputeHashInteraction(block1_bytes, block2_bytes, expected_bytes, emptyHash_bytes);
        }

        private void VerifyTransformBlockOutput(byte[] block1, byte[] block2)
        {
            using (HashAlgorithm hash = Create())
            {
                byte[] actualBlock1 = new byte[block1.Length];
                int byteCount = hash.TransformBlock(block1, 0, block1.Length, actualBlock1, 0);
                Assert.Equal(block1.Length, byteCount);
                Assert.Equal(block1, actualBlock1);

                byte[] actualBlock2 = hash.TransformFinalBlock(block2, 0, block2.Length);
                Assert.Equal(block2, actualBlock2);
            }
        }

        private void VerifyTransformBlockHash(byte[] block1, byte[] block2, byte[] expected, byte[] expectedEmpty)
        {
            using (HashAlgorithm hash = Create())
            {
                // Verify Empty Hash
                hash.TransformBlock(Array.Empty<byte>(), 0, 0, null, 0);
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Assert.Equal(hash.Hash, expectedEmpty);

                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Assert.Equal(hash.Hash, expectedEmpty);

                // Verify Hash
                hash.TransformBlock(block1, 0, block1.Length, null, 0);
                hash.TransformFinalBlock(block2, 0, block2.Length);
                Assert.Equal(expected, hash.Hash);
                Assert.Equal(expected, hash.Hash); // .Hash doesn't clear hash

                // Verify bad State
                hash.TransformBlock(block1, 0, block1.Length, null, 0);
                // Can't access hash until TransformFinalBlock is called
                Assert.Throws<CryptographicUnexpectedOperationException>(() => hash.Hash);
                hash.TransformFinalBlock(block2, 0, block2.Length);
                Assert.Equal(expected, hash.Hash);

                // Verify clean State
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Assert.Equal(hash.Hash, expectedEmpty);
            }
        }

        private void VerifyTransformBlockComputeHashInteraction(byte[] block1, byte[] block2, byte[] expected, byte[] expectedEmpty)
        {
            using (HashAlgorithm hash = Create())
            {
                // TransformBlock + ComputeHash
                hash.TransformBlock(block1, 0, block1.Length, null, 0);
                byte[] actual = hash.ComputeHash(block2, 0, block2.Length);
                Assert.Equal(expected, actual);

                // ComputeHash does not reset State variable
                Assert.Throws<CryptographicUnexpectedOperationException>(() => hash.Hash);
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Assert.Equal(expectedEmpty, hash.Hash);
                actual = hash.ComputeHash(Array.Empty<byte>(), 0, 0);
                Assert.Equal(expectedEmpty, actual);

                // TransformBlock + TransformBlock + ComputeHash(empty)
                hash.TransformBlock(block1, 0, block1.Length, null, 0);
                hash.TransformBlock(block2, 0, block2.Length, null, 0);
                actual = hash.ComputeHash(Array.Empty<byte>(), 0, 0);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void HashData_ByteArray_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => HashData(null));
        }

        [Fact]
        public void HashData_BufferTooSmall()
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => HashData(default, default));
        }

        [Fact]
        public void VerifyObjectDisposedException()
        {
            HashAlgorithm hash = Create();
            hash.Dispose();
            Assert.Throws<ObjectDisposedException>(() => hash.Hash);
            Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(Array.Empty<byte>()));
            Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(Array.Empty<byte>(), 0, 0));
            Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash((Stream)null));
            Assert.Throws<ObjectDisposedException>(() => hash.TransformBlock(Array.Empty<byte>(), 0, 0, null, 0));
            Assert.Throws<ObjectDisposedException>(() => hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0));
        }

        [Fact]
        public void VerifyHashNotYetFinalized()
        {
            using (HashAlgorithm hash = Create())
            {
                hash.TransformBlock(Array.Empty<byte>(), 0, 0, null, 0);
                Assert.Throws<CryptographicUnexpectedOperationException>(() => hash.Hash);
            }
        }

        [Fact]
        public void InvalidInput_ComputeHash()
        {
            using (HashAlgorithm hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash(null, 0, 0));
            }
        }

        [Fact]
        public void InvalidInput_TransformBlock()
        {
            using (HashAlgorithm hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => hash.TransformBlock(null, 0, 0, null, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => hash.TransformBlock(Array.Empty<byte>(), -1, 0, null, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.TransformBlock(Array.Empty<byte>(), 0, 1, null, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.TransformBlock(Array.Empty<byte>(), 1, 0, null, 0));
            }
        }

        [Fact]
        public void InvalidInput_TransformFinalBlock()
        {
            using (HashAlgorithm hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => hash.TransformFinalBlock(null, 0, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => hash.TransformFinalBlock(Array.Empty<byte>(), -1, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.TransformFinalBlock(Array.Empty<byte>(), 1, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.TransformFinalBlock(Array.Empty<byte>(), 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.TransformFinalBlock(Array.Empty<byte>(), 0, 1));
            }
        }

        protected void Verify(byte[] input, string output)
        {
            Verify_Array(input, output);
            Verify_Span(input, output);
            Verify_OneShot(input, output);
        }

        private void Verify_OneShot(byte[] input, string output)
        {
            Span<byte> destination = stackalloc byte[1024 / 8];
            byte[] expectedArray = ByteUtils.HexToByteArray(output);
            ReadOnlySpan<byte> expected = expectedArray;

            // big enough
            bool result = TryHashData(input, destination, out int bytesWritten);
            Assert.True(result, "TryHashData true");
            Assert.True(expected.SequenceEqual(destination.Slice(0, bytesWritten)), "expected equals destination");

            //too small
            result = TryHashData(input, default, out bytesWritten);
            Assert.False(result, "TryHashData false");
            Assert.Equal(0, bytesWritten);

            Span<byte> inputOutput = new byte[Math.Max(input.Length, expected.Length) + 1];
            input.AsSpan().CopyTo(inputOutput);

            // overlapping
            result = TryHashData(inputOutput.Slice(0, input.Length), inputOutput, out bytesWritten);
            Assert.True(result, "TryHashData true");
            Assert.True(expected.SequenceEqual(inputOutput.Slice(0, bytesWritten)), "expected equals destination");

            // partial overlapping forward
            input.AsSpan().CopyTo(inputOutput);
            result = TryHashData(inputOutput.Slice(0, input.Length), inputOutput.Slice(1), out bytesWritten);
            Assert.True(result, "TryHashData true");
            Assert.True(expected.SequenceEqual(inputOutput.Slice(1, bytesWritten)), "expected equals destination");

            // partial overlapping backward
            input.AsSpan().CopyTo(inputOutput.Slice(1));
            result = TryHashData(inputOutput.Slice(1, input.Length), inputOutput, out bytesWritten);
            Assert.True(result, "TryHashData true");
            Assert.True(expected.SequenceEqual(inputOutput.Slice(0, bytesWritten)), "expected equals destination");

            // throwing span one-shot
            bytesWritten = HashData(input, destination);
            Assert.True(expected.SequenceEqual(destination.Slice(0, bytesWritten)), "expected equals destination");

            // byte array allocating one-shot
            byte[] allocatingArrayResult = HashData(input);
            Assert.Equal(expectedArray, allocatingArrayResult);

            // byte span allocating one-shot
            byte[] allocatingSpanResult = HashData(new ReadOnlySpan<byte>(input));
            Assert.Equal(expectedArray, allocatingSpanResult);
        }

        private void Verify_Array(byte[] input, string output)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] actual;

            using (HashAlgorithm hash = Create())
            {
                Assert.True(hash.HashSize > 0);
                actual = hash.ComputeHash(input, 0, input.Length);

                Assert.Equal(expected, actual);

                actual = hash.Hash;
                Assert.Equal(expected, actual);
            }
        }

        private void Verify_Span(byte[] input, string output)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] actual;
            int bytesWritten;

            using (HashAlgorithm hash = Create())
            {
                // Too small
                actual = new byte[expected.Length - 1];
                Assert.False(hash.TryComputeHash(input, actual, out bytesWritten));
                Assert.Equal(0, bytesWritten);

                // Just right
                actual = new byte[expected.Length];
                Assert.True(hash.TryComputeHash(input, actual, out bytesWritten));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, actual);

                // Bigger than needed
                actual = new byte[expected.Length + 1];
                actual[actual.Length - 1] = 42;
                Assert.True(hash.TryComputeHash(input, actual, out bytesWritten));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, actual.AsSpan(0, expected.Length).ToArray());
                Assert.Equal(42, actual[actual.Length - 1]);
            }
        }

        protected void VerifyRepeating(string input, int repeatCount, string output)
        {
            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyComputeHashStream(stream, output);
            }

            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                VerifyICryptoTransformStream(stream, output);
            }
        }

        [Fact]
        public void InvalidInput_Null()
        {
            using (HashAlgorithm hash = Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash(null, 0, 0));
                Assert.Throws<NullReferenceException>(() => hash.ComputeHash((Stream)null));
            }
        }

        [Fact]
        public void InvalidInput_NegativeOffset()
        {
            using (HashAlgorithm hash = Create())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => hash.ComputeHash(Array.Empty<byte>(), -1, 0));
            }
        }

        [Fact]
        public void InvalidInput_NegativeCount()
        {
            using (HashAlgorithm hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, -1));
            }
        }

        [Fact]
        public void InvalidInput_TooBigOffset()
        {
            using (HashAlgorithm hash = Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 1, 0));
            }
        }

        [Fact]
        public void InvalidInput_TooBigCount()
        {
            byte[] nonEmpty = new byte[53];

            using (HashAlgorithm hash = Create())
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

            using (HashAlgorithm hash = Create())
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

            using (HashAlgorithm hash = Create())
            {
                byte[] baseline = hash.ComputeHash(dataA);

                // Skip the 0 byte, and stop short of the 13.
                byte[] offsetData = hash.ComputeHash(dataB, 1, dataA.Length);

                Assert.Equal(baseline, offsetData);
            }
        }

        [Fact]
        public void ComputeHash_TryComputeHash_HashSetExplicitlyByBoth()
        {
            using (HashAlgorithm hash = Create())
            {
                byte[] input = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

                byte[] computeHashResult = hash.ComputeHash(input);
                Assert.NotNull(computeHashResult);
                Assert.NotNull(hash.Hash);
                Assert.NotSame(computeHashResult, hash.Hash);
                Assert.Equal(computeHashResult, hash.Hash);

                Assert.True(hash.TryComputeHash(input, computeHashResult, out int bytesWritten));
                Assert.Equal(computeHashResult.Length, bytesWritten);
                Assert.Null(hash.Hash);
            }
        }

        [Fact]
        public void Dispose_TryComputeHash_ThrowsException()
        {
            HashAlgorithm hash = Create();
            hash.Dispose();
            Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(new byte[1]));
            Assert.Throws<ObjectDisposedException>(() => hash.TryComputeHash(new byte[1], new byte[1], out int bytesWritten));
        }

        [Fact]
        public void Initialize_TransformBlock()
        {
            byte[] hashInput = new byte[] { 1, 2, 3, 4, 5 };
            byte[] expectedDigest;

            using (HashAlgorithm hash = Create())
            {
                expectedDigest = hash.ComputeHash(hashInput);
            }

            using (HashAlgorithm hash = Create())
            {
                hash.TransformBlock(hashInput, 0, hashInput.Length, hashInput, 0);
                hash.Initialize();
                hash.TransformBlock(hashInput, 0, hashInput.Length, hashInput, 0);
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                Assert.Equal(expectedDigest, hash.Hash);
            }
        }

        [Fact]
        public void Initialize_TransformBlock_Unused()
        {
            byte[] hashInput = new byte[] { 1, 2, 3, 4, 5 };
            byte[] expectedDigest;

            using (HashAlgorithm hash = Create())
            {
                expectedDigest = hash.ComputeHash(hashInput);
            }

            using (HashAlgorithm hash = Create())
            {
                hash.Initialize();
                hash.TransformBlock(hashInput, 0, hashInput.Length, hashInput, 0);
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                Assert.Equal(expectedDigest, hash.Hash);
            }
        }

        protected class DataRepeatingStream : Stream
        {
            private int _remaining;
            private byte[] _data;

            public DataRepeatingStream(string data, int repeatCount)
            {
                _remaining = repeatCount;
                _data = ByteUtils.AsciiBytes(data);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!CanRead)
                {
                    throw new NotSupportedException();
                }

                if (_remaining == 0)
                {
                    return 0;
                }

                if (count < _data.Length)
                {
                    throw new InvalidOperationException();
                }

                int multiple = count / _data.Length;

                if (multiple > _remaining)
                {
                    multiple = _remaining;
                }

                int localOffset = offset;

                for (int i = 0; i < multiple; i++)
                {
                    Buffer.BlockCopy(_data, 0, buffer, localOffset, _data.Length);
                    localOffset += _data.Length;
                }

                _remaining -= multiple;
                return _data.Length * multiple;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _data = null;
                }
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead { get { return _data != null; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }
        }
    }
}
