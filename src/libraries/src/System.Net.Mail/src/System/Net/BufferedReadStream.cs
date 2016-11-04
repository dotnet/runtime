// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal class BufferedReadStream : DelegatedStream
    {
        private byte[] _storedBuffer;
        private int _storedLength;
        private int _storedOffset;
        private bool _readMore;

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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            ReadAsyncResult result = new ReadAsyncResult(this, callback, state);
            result.Read(buffer, offset, count);
            return result;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            int read = ReadAsyncResult.End(asyncResult);
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            if (_storedOffset < _storedLength)
            {
                read = Math.Min(count, _storedLength - _storedOffset);
                Buffer.BlockCopy(_storedBuffer, _storedOffset, buffer, offset, read);
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
            int read = 0;
            if (_storedOffset >= _storedLength)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            read = Math.Min(count, _storedLength - _storedOffset);
            Buffer.BlockCopy(_storedBuffer, _storedOffset, buffer, offset, read);
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
            int returnValue = await base.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            return bytesAlreadyRead + returnValue;
        }

        public override int ReadByte()
        {
            if (_storedOffset < _storedLength)
            {
                return (int)_storedBuffer[_storedOffset++];
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
                else if (count <= _storedBuffer.Length - _storedLength + _storedOffset)
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

            Buffer.BlockCopy(buffer, offset, _storedBuffer, _storedOffset, count);
        }

        // adds additional content to the end of the buffer
        // so the layout of the storedBuffer will be
        // <existingBuffer><buffer>
        // after calling append
        internal void Append(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return;

            int newBufferPosition;
            if (_storedOffset == _storedLength)
            {
                if (_storedBuffer == null || _storedBuffer.Length < count)
                {
                    _storedBuffer = new byte[count];
                }
                _storedOffset = 0;
                _storedLength = count;
                newBufferPosition = 0;
            }
            else
            {
                // if there's room to just insert after existing data
                if (count <= _storedBuffer.Length - _storedLength)
                {
                    //no preperation necessary
                    newBufferPosition = _storedLength;
                    _storedLength += count;
                }
                // if there's room in the buffer but need to shift things over
                else if (count <= _storedBuffer.Length - _storedLength + _storedOffset)
                {
                    Buffer.BlockCopy(_storedBuffer, _storedOffset, _storedBuffer, 0, _storedLength - _storedOffset);
                    newBufferPosition = _storedLength - _storedOffset;
                    _storedOffset = 0;
                    _storedLength = count + newBufferPosition;
                }
                else
                {
                    // the buffer is too small
                    // allocate new buffer
                    byte[] newBuffer = new byte[count + _storedLength - _storedOffset];
                    // and prepopulate the remaining content of the original buffer
                    Buffer.BlockCopy(_storedBuffer, _storedOffset, newBuffer, 0, _storedLength - _storedOffset);
                    newBufferPosition = _storedLength - _storedOffset;
                    _storedOffset = 0;
                    _storedLength = count + newBufferPosition;
                    _storedBuffer = newBuffer;
                }
            }

            Buffer.BlockCopy(buffer, offset, _storedBuffer, newBufferPosition, count);
        }

        private class ReadAsyncResult : LazyAsyncResult
        {
            private BufferedReadStream _parent;
            private int _read;
            private static AsyncCallback s_onRead = new AsyncCallback(OnRead);

            internal ReadAsyncResult(BufferedReadStream parent, AsyncCallback callback, object state) : base(null, state, callback)
            {
                _parent = parent;
            }

            internal void Read(byte[] buffer, int offset, int count)
            {
                if (_parent._storedOffset < _parent._storedLength)
                {
                    _read = Math.Min(count, _parent._storedLength - _parent._storedOffset);
                    Buffer.BlockCopy(_parent._storedBuffer, _parent._storedOffset, buffer, offset, _read);
                    _parent._storedOffset += _read;
                    if (_read == count || !_parent._readMore)
                    {
                        InvokeCallback();
                        return;
                    }

                    count -= _read;
                    offset += _read;
                }
                IAsyncResult result = _parent.BaseStream.BeginRead(buffer, offset, count, s_onRead, this);
                if (result.CompletedSynchronously)
                {
                    _read += _parent.BaseStream.EndRead(result);
                    InvokeCallback();
                }
            }

            internal static int End(IAsyncResult result)
            {
                ReadAsyncResult thisPtr = (ReadAsyncResult)result;
                thisPtr.InternalWaitForCompletion();
                return thisPtr._read;
            }

            private static void OnRead(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ReadAsyncResult thisPtr = (ReadAsyncResult)result.AsyncState;
                    try
                    {
                        thisPtr._read += thisPtr._parent.BaseStream.EndRead(result);
                        thisPtr.InvokeCallback();
                    }
                    catch (Exception e)
                    {
                        if (thisPtr.IsCompleted)
                            throw;
                        thisPtr.InvokeCallback(e);
                    }
                }
            }
        }
    }
}
