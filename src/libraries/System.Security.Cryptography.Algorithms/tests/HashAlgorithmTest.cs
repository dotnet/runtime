// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public abstract partial class HashAlgorithmTest
    {
        protected abstract HashAlgorithm Create();

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

        // https://github.com/dotnet/corefx/issues/18863
        private void ReinitilizeHashIfNetfx(HashAlgorithm hash)
        {
            if (PlatformDetection.IsFullFramework)
            {
                hash.Initialize();
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

                ReinitilizeHashIfNetfx(hash);
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Assert.Equal(hash.Hash, expectedEmpty);

                // Verify Hash
                ReinitilizeHashIfNetfx(hash);
                hash.TransformBlock(block1, 0, block1.Length, null, 0);
                hash.TransformFinalBlock(block2, 0, block2.Length);
                Assert.Equal(expected, hash.Hash);
                Assert.Equal(expected, hash.Hash); // .Hash doesn't clear hash

                // Verify bad State
                ReinitilizeHashIfNetfx(hash);
                hash.TransformBlock(block1, 0, block1.Length, null, 0);
                // Can't access hash until TransformFinalBlock is called
                Assert.Throws<CryptographicUnexpectedOperationException>(() => hash.Hash);
                hash.TransformFinalBlock(block2, 0, block2.Length);
                Assert.Equal(expected, hash.Hash);

                // Verify clean State
                ReinitilizeHashIfNetfx(hash);
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
                ReinitilizeHashIfNetfx(hash);
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

        partial void Verify_Span(byte[] input, string output);

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
