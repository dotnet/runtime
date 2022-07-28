// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Test.Cryptography;

namespace System.Security.Cryptography.Tests
{
    internal class DataRepeatingStream : Stream
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

            // For (about) half of the reads, we'll read one less byte
            // than was asked for. This is to make sure stream readers
            // conform to the expectation that Read MAY return less than
            // was asked for.
            if (count > 1 && Random.Shared.Next(2) == 0)
            {
                count--;
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

    internal class UntouchableStream : Stream
    {
        public static Stream Instance { get; } = new UntouchableStream();

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
    }
}
