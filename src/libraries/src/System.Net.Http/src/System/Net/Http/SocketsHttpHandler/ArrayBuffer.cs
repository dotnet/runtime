// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http
{
    // Warning: Mutable struct!
    // The purpose of this struct is to simplify buffer management.
    // It manages a "read position" and a "write position" into a single array buffer,
    // and provides some support for resizing/shifting the buffer when necessary.

    // Invariants:
    // 0 <= _readPosition <= _writePosition <= bytes.Length
    // Bytes between _writePosition and bytes.Length are available to write to.
    // Bytes between _readPosition and _writePosition are available to read from.
    // Bytes between 0 and _readPosition are dead, and should be ignored.
    internal struct ArrayBuffer
    {
        private byte[] _bytes;
        private int _readPosition;
        private int _writePosition;

        public ArrayBuffer(int initialSize)
        {
            _bytes = new byte[initialSize];

            _readPosition = 0;
            _writePosition = 0;
        }

        public Span<byte> ReadSpan => new Span<byte>(_bytes, _readPosition, _writePosition - _readPosition);
        public Span<byte> WriteSpan => new Span<byte>(_bytes, _writePosition, _bytes.Length - _writePosition);
        public Memory<byte> ReadMemory => new Memory<byte>(_bytes, _readPosition, _writePosition - _readPosition);
        public Memory<byte> WriteMemory => new Memory<byte>(_bytes, _writePosition, _bytes.Length - _writePosition);
        public int BufferSize => _bytes.Length;

        // Advance the read position.
        public void Consume(int byteCount)
        {
            Debug.Assert(byteCount <= ReadSpan.Length);
            _readPosition += byteCount;

            if (_readPosition == _writePosition)
            {
                _readPosition = 0;
                _writePosition = 0;
            }
        }

        // Advance the write position.
        public void Commit(int byteCount)
        {
            Debug.Assert(byteCount <= WriteSpan.Length);
            _writePosition += byteCount;
        }

        // Ensure at least [byteCount] bytes to write to.
        public void EnsureWriteSpace(int byteCount)
        {
            if (byteCount <= WriteSpan.Length)
            {
                return;
            }

            int totalFree = _readPosition + WriteSpan.Length;
            if (byteCount <= totalFree)
            {
                // We can free up enough space by just shifting the bytes down, so do so.
                Buffer.BlockCopy(_bytes, _readPosition, _bytes, 0, ReadSpan.Length);
                _writePosition = ReadSpan.Length;
                _readPosition = 0;
                Debug.Assert(byteCount <= WriteSpan.Length);
                return;
            }

            // Double the size of the buffer until we have enough space.
            int desiredSize = ReadSpan.Length + byteCount;
            int newSize = _bytes.Length;
            do
            {
                newSize *= 2;
            } while (newSize < desiredSize);

            byte[] newBytes = new byte[newSize];

            if (ReadSpan.Length != 0)
            {
                Buffer.BlockCopy(_bytes, _readPosition, newBytes, 0, ReadSpan.Length);
            }

            _writePosition = ReadSpan.Length;
            _readPosition = 0;
            _bytes = newBytes;

            Debug.Assert(byteCount <= WriteSpan.Length);
        }
    }
}
