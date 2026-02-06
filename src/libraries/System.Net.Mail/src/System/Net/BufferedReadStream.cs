// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace System.Net
{
    internal sealed class BufferedReadStream : DelegatedStream
    {
        private byte[]? _storedBuffer;
        private int _storedLength;
        private int _storedOffset;
        private readonly bool _readMore;

        internal BufferedReadStream(Stream stream) : this(stream, false)
        {
        }

        internal BufferedReadStream(Stream stream, bool readMore) : base(stream)
        {
            _readMore = readMore;
        }

        public override bool CanWrite => false;
        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => false;

        protected override int ReadInternal(Span<byte> buffer)
        {
            if (_storedOffset < _storedLength)
            {
                int read = Math.Min(buffer.Length, _storedLength - _storedOffset);
                _storedBuffer.AsSpan(_storedOffset, read).CopyTo(buffer);
                _storedOffset += read;
                if (read == buffer.Length || !_readMore)
                {
                    return read;
                }

                // Need to read more from the underlying stream
                return read + BaseStream.Read(buffer.Slice(read));
            }

            return BaseStream.Read(buffer);
        }

        protected override ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_storedOffset >= _storedLength)
            {
                return BaseStream.ReadAsync(buffer, cancellationToken);
            }

            int read = Math.Min(buffer.Length, _storedLength - _storedOffset);
            _storedBuffer.AsMemory(_storedOffset, read).CopyTo(buffer);
            _storedOffset += read;
            if (read == buffer.Length || !_readMore)
            {
                return new ValueTask<int>(read);
            }

            // Need to read more from the underlying stream
            return ReadMoreAsync(read, buffer.Slice(read), cancellationToken);
        }

        private async ValueTask<int> ReadMoreAsync(int bytesAlreadyRead, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int returnValue = await BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            return bytesAlreadyRead + returnValue;
        }

        protected override void WriteInternal(ReadOnlySpan<byte> buffer)
        {
            throw new NotImplementedException();
        }

        protected override ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // adds additional content to the beginning of the buffer
        // so the layout of the storedBuffer will be
        // <buffer><existingBuffer>
        // after calling push
        internal void Push(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 0)
                return;

            int count = buffer.Length;

            if (_storedOffset == _storedLength)
            {
                if (_storedBuffer == null || _storedBuffer.Length < count)
                {
                    _storedBuffer = new byte[count];
                }
                _storedOffset = 0;
                _storedLength = count;
            }
            else
            {
                // if there's room to just insert before existing data
                if (count <= _storedOffset)
                {
                    _storedOffset -= count;
                }
                // if there's room in the buffer but need to shift things over
                else if (count <= _storedBuffer!.Length - _storedLength + _storedOffset)
                {
                    Buffer.BlockCopy(_storedBuffer, _storedOffset, _storedBuffer, count, _storedLength - _storedOffset);
                    _storedLength += count - _storedOffset;
                    _storedOffset = 0;
                }
                else
                {
                    byte[] newBuffer = new byte[count + _storedLength - _storedOffset];
                    Buffer.BlockCopy(_storedBuffer, _storedOffset, newBuffer, count, _storedLength - _storedOffset);
                    _storedLength += count - _storedOffset;
                    _storedOffset = 0;
                    _storedBuffer = newBuffer;
                }
            }

            buffer.CopyTo(_storedBuffer.AsSpan(_storedOffset));
        }
    }
}
