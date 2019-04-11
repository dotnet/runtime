// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

    [StructLayout(LayoutKind.Auto)]
    internal struct ArrayBuffer : IDisposable
    {
        private readonly bool _usePool;
        private byte[] _bytes;
        private int _activeStart;
        private int _availableStart;

        // Invariants:
        // 0 <= _activeStart <= _availableStart <= bytes.Length

        public ArrayBuffer(int initialSize, bool usePool = false)
        {
            _usePool = usePool;
            _bytes = usePool ? ArrayPool<byte>.Shared.Rent(initialSize) : new byte[initialSize];
            _activeStart = 0;
            _availableStart = 0;
        }

        public void Dispose()
        {
            if (_usePool)
            {
                _activeStart = _availableStart = 0;

                byte[] array = _bytes;
                _bytes = null;

                if (array != null)
                {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }

        public Span<byte> ActiveSpan => new Span<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public int AvailableLength => _bytes.Length - _availableStart;
        public Span<byte> AvailableSpan => new Span<byte>(_bytes, _availableStart, AvailableLength);
        public Memory<byte> ActiveMemory => new Memory<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public Memory<byte> AvailableMemory => new Memory<byte>(_bytes, _availableStart, _bytes.Length - _availableStart);

        public int Capacity => _bytes.Length;

        public void Discard(int byteCount)
        {
            Debug.Assert(byteCount <= ActiveSpan.Length, $"Expected {byteCount} <= {ActiveSpan.Length}");
            _activeStart += byteCount;

            if (_activeStart == _availableStart)
            {
                _activeStart = 0;
                _availableStart = 0;
            }
        }

        public void Commit(int byteCount)
        {
            Debug.Assert(byteCount <= AvailableLength);
            _availableStart += byteCount;
        }

        // Ensure at least [byteCount] bytes to write to.
        public void EnsureAvailableSpace(int byteCount)
        {
            if (byteCount <= AvailableLength)
            {
                return;
            }

            int totalFree = _activeStart + AvailableLength;
            if (byteCount <= totalFree)
            {
                // We can free up enough space by just shifting the bytes down, so do so.
                Buffer.BlockCopy(_bytes, _activeStart, _bytes, 0, ActiveSpan.Length);
                _availableStart = ActiveSpan.Length;
                _activeStart = 0;
                Debug.Assert(byteCount <= AvailableLength);
                return;
            }

            // Double the size of the buffer until we have enough space.
            int desiredSize = ActiveSpan.Length + byteCount;
            int newSize = _bytes.Length;
            do
            {
                newSize *= 2;
            } while (newSize < desiredSize);

            byte[] newBytes = _usePool ?
                ArrayPool<byte>.Shared.Rent(newSize) :
                new byte[newSize];
            byte[] oldBytes = _bytes;

            if (ActiveSpan.Length != 0)
            {
                Buffer.BlockCopy(oldBytes, _activeStart, newBytes, 0, ActiveSpan.Length);
            }

            _availableStart = ActiveSpan.Length;
            _activeStart = 0;

            _bytes = newBytes;
            if (_usePool)
            {
                ArrayPool<byte>.Shared.Return(oldBytes);
            }

            Debug.Assert(byteCount <= AvailableLength);
        }
    }
}
