// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public abstract class HashAlgorithmTest
    {
        protected abstract HashAlgorithm Create();

        protected void Verify(string input, string output)
        {
            Verify(ByteUtils.AsciiBytes(input), output);
        }

        protected void Verify(Stream input, string output)
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

        protected void Verify(byte[] input, string output)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] actual;

            using (HashAlgorithm hash = Create())
            {
                Assert.True(hash.HashSize > 0);
                actual = hash.ComputeHash(input, 0, input.Length);
            }

            Assert.Equal(expected, actual);
        }

        protected void VerifyRepeating(string input, int repeatCount, string output)
        {
            using (Stream stream = new DataRepeatingStream(input, repeatCount))
            {
                Verify(stream, output);
            }
        }

        [Fact]
        public void InvalidInput_Null()
        {
            using (HashAlgorithm hash = Create())
            {
                Assert.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash((byte[])null));
                Assert.Throws<ArgumentNullException>("buffer", () => hash.ComputeHash(null, 0, 0));
                Assert.Throws<NullReferenceException>(() => hash.ComputeHash((Stream)null));
            }
        }

        [Fact]
        public void InvalidInput_NegativeOffset()
        {
            using (HashAlgorithm hash = Create())
            {
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => hash.ComputeHash(Array.Empty<byte>(), -1, 0));
            }
        }

        [Fact]
        public void InvalidInput_NegativeCount()
        {
            using (HashAlgorithm hash = Create())
            {
                Assert.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, -1));
            }
        }

        [Fact]
        public void InvalidInput_TooBigOffset()
        {
            using (HashAlgorithm hash = Create())
            {
                Assert.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 1, 0));
            }
        }

        [Fact]
        public void InvalidInput_TooBigCount()
        {
            byte[] nonEmpty = new byte[53];

            using (HashAlgorithm hash = Create())
            {
                Assert.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 0, nonEmpty.Length + 1));
                Assert.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 1, nonEmpty.Length));
                Assert.Throws<ArgumentException>(null, () => hash.ComputeHash(nonEmpty, 2, nonEmpty.Length - 1));
                Assert.Throws<ArgumentException>(null, () => hash.ComputeHash(Array.Empty<byte>(), 0, 1));
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

                if (count < _data.Length)
                {
                    throw new InvalidOperationException();
                }

                if (_remaining == 0)
                {
                    return 0;
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
