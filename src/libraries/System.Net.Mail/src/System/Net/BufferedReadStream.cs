// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End<int>(asyncResult);

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            if (_storedOffset < _storedLength)
            {
                read = Math.Min(count, _storedLength - _storedOffset);
                Buffer.BlockCopy(_storedBuffer!, _storedOffset, buffer, offset, read);
                _storedOffset += read;
                if (read == count || !_readMore)
                {
                    return read;
                }

                offset += read;
                count -= read;
            }
            return read + base.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read;
            if (_storedOffset >= _storedLength)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            read = Math.Min(count, _storedLength - _storedOffset);
            Buffer.BlockCopy(_storedBuffer!, _storedOffset, buffer, offset, read);
            _storedOffset += read;
            if (read == count || !_readMore)
            {
                return Task.FromResult<int>(read);
            }

            offset += read;
            count -= read;

            return ReadMoreAsync(read, buffer, offset, count, cancellationToken);
        }

        private async Task<int> ReadMoreAsync(int bytesAlreadyRead, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int returnValue = await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            return bytesAlreadyRead + returnValue;
        }

        public override int ReadByte()
        {
            if (_storedOffset < _storedLength)
            {
                return _storedBuffer![_storedOffset++];
            }
            else
            {
                return base.ReadByte();
            }
        }

        // adds additional content to the beginning of the buffer
        // so the layout of the storedBuffer will be
        // <buffer><existingBuffer>
        // after calling push
        internal void Push(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return;

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

            Buffer.BlockCopy(buffer, offset, _storedBuffer!, _storedOffset, count);
        }
    }
}
