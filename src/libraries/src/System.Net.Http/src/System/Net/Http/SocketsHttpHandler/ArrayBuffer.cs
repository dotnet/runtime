// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http
{
    // Warning: Mutable struct!
    // The purpose of this struct is to simplify buffer management.
    // It manages a sliding buffer where bytes can be added at the end and removed at the beginning.
    // [ActiveSpan/Memory] contains the current buffer contents; these bytes will be preserved
    // (copied, if necessary) on any call to EnsureAvailableBytes.
    // [AvailableSpan/Memory] contains the available bytes past the end of the current content,
    // and can be written to in order to add data to the end of the buffer.
    // Commit(byteCount) will extend the ActiveSpan by [byteCount] bytes into the AvailableSpan.
    // Discard(byteCount) will discard [byteCount] bytes as the beginning of the ActiveSpan.

    // TODO: ISSUE 31300: Use ArrayPool to pool buffers.

    internal struct ArrayBuffer
    {
        private byte[] _bytes;
        private int _activeStart;
        private int _availableStart;

        // Invariants:
        // 0 <= _activeStart <= _availableStart <= bytes.Length

        public ArrayBuffer(int initialSize)
        {
            _bytes = new byte[initialSize];

            _activeStart = 0;
            _availableStart = 0;
        }

        public Span<byte> ActiveSpan => new Span<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public Span<byte> AvailableSpan => new Span<byte>(_bytes, _availableStart, _bytes.Length - _availableStart);
        public Memory<byte> ActiveMemory => new Memory<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public Memory<byte> AvailableMemory => new Memory<byte>(_bytes, _availableStart, _bytes.Length - _availableStart);

        public void Discard(int byteCount)
        {
            Debug.Assert(byteCount <= ActiveSpan.Length);
            _activeStart += byteCount;

            if (_activeStart == _availableStart)
            {
                _activeStart = 0;
                _availableStart = 0;
            }
        }

        public void Commit(int byteCount)
        {
            Debug.Assert(byteCount <= AvailableSpan.Length);
            _availableStart += byteCount;
        }

        // Ensure at least [byteCount] bytes to write to.
        public void EnsureAvailableSpace(int byteCount)
        {
            if (byteCount <= AvailableSpan.Length)
            {
                return;
            }

            int totalFree = _activeStart + AvailableSpan.Length;
            if (byteCount <= totalFree)
            {
                // We can free up enough space by just shifting the bytes down, so do so.
                Buffer.BlockCopy(_bytes, _activeStart, _bytes, 0, ActiveSpan.Length);
                _availableStart = ActiveSpan.Length;
                _activeStart = 0;
                Debug.Assert(byteCount <= AvailableSpan.Length);
                return;
            }

            // Double the size of the buffer until we have enough space.
            int desiredSize = ActiveSpan.Length + byteCount;
            int newSize = _bytes.Length;
            do
            {
                newSize *= 2;
            } while (newSize < desiredSize);

            byte[] newBytes = new byte[newSize];

            if (ActiveSpan.Length != 0)
            {
                Buffer.BlockCopy(_bytes, _activeStart, newBytes, 0, ActiveSpan.Length);
            }

            _availableStart = ActiveSpan.Length;
            _activeStart = 0;
            _bytes = newBytes;

            Debug.Assert(byteCount <= AvailableSpan.Length);
        }
    }
}
