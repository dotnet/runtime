// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter : IDisposable
    {
        // TODO : determine if CryptoPool is more appropriate
        private static readonly ArrayPool<byte> s_bufferPool = ArrayPool<byte>.Create();

        private byte[] _buffer = null!;
        private int _offset = 0;

        public CborWriter()
        {

        }

        private void CheckDisposed()
        {
            if (_offset < 0)
            {
                throw new ObjectDisposedException(nameof(CborWriter));
            }
        }

        private void EnsureWriteCapacity(int pendingCount)
        {
            CheckDisposed();

            if (pendingCount < 0)
            {
                throw new OverflowException();
            }

            if (_buffer == null || _buffer.Length - _offset < pendingCount)
            {
                const int BlockSize = 1024;
                // While the ArrayPool may have similar logic, make sure we don't run into a lot of
                // "grow a little" by asking in 1k steps.
                int blocks = checked(_offset + pendingCount + (BlockSize - 1)) / BlockSize;
                byte[]? oldBytes = _buffer;
                _buffer = s_bufferPool.Rent(BlockSize * blocks);

                if (oldBytes != null)
                {
                    Buffer.BlockCopy(oldBytes, 0, _buffer, 0, _offset);
                    s_bufferPool.Return(oldBytes, clearArray: true);
                }
            }
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                s_bufferPool.Return(_buffer, clearArray: true);
                _buffer = null!;
            }

            _offset = -1;
        }

        public byte[] ToArray()
        {
            CheckDisposed();

            return (_offset == 0) ? Array.Empty<byte>() : _buffer.AsSpan(0, _offset).ToArray();
        }
    }
}
