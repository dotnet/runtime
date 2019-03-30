// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal sealed class Utf8StringStream : Stream
    {
        private readonly Utf8String _content;
        private int _position;

        public Utf8StringStream(Utf8String content)
        {
            _content = content ?? Utf8String.Empty;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length => _content.Length;

        public override long Position
        {
            get => _position;
            set
            {
                if ((ulong)value > (uint)_content.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _position = (int)value;
            }
        }

        public override void Flush()
        {
            /* no-op */
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            /* no-op */
            return Task.CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            ReadOnlySpan<byte> contentToWrite = _content.AsBytes(_position);
            if (buffer.Length < contentToWrite.Length)
            {
                contentToWrite = contentToWrite.Slice(buffer.Length);
            }

            contentToWrite.CopyTo(buffer);
            _position += contentToWrite.Length;

            return contentToWrite.Length;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read(new Span<byte>(buffer, offset, count)));
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(Read(buffer.Span));
        }

        public override int ReadByte()
        {
            int position = _position;
            if ((uint)position >= (uint)_content.Length)
            {
                return -1;
            }

            _position++;
            return _content[position];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;
                case SeekOrigin.Current:
                    offset += _position;
                    break;
                case SeekOrigin.End:
                    offset += _content.Length;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            if ((ulong)offset > (uint)_content.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _position = (int)offset;
            return offset;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public override void WriteByte(byte value) => throw new NotSupportedException();
    }
}
