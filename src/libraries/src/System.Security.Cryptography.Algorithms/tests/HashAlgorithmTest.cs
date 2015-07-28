// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
