// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net
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
            _activeStart = 0;
            _availableStart = 0;

            byte[] array = _bytes;
            _bytes = null!;

            if (_usePool && array != null)
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public int ActiveLength => _availableStart - _activeStart;
        public Span<byte> ActiveSpan => new Span<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public ReadOnlySpan<byte> ActiveReadOnlySpan => new ReadOnlySpan<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public Memory<byte> ActiveMemory => new Memory<byte>(_bytes, _activeStart, _availableStart - _activeStart);

        public int AvailableLength => _bytes.Length - _availableStart;
        public Span<byte> AvailableSpan => _bytes.AsSpan(_availableStart);
        public Memory<byte> AvailableMemory => _bytes.AsMemory(_availableStart);
        public Memory<byte> AvailableMemorySliced(int length) => new Memory<byte>(_bytes, _availableStart, length);

        public int Capacity => _bytes.Length;

        public void Discard(int byteCount)
        {
            Debug.Assert(byteCount <= ActiveLength, $"Expected {byteCount} <= {ActiveLength}");
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
                Buffer.BlockCopy(_bytes, _activeStart, _bytes, 0, ActiveLength);
                _availableStart = ActiveLength;
                _activeStart = 0;
                Debug.Assert(byteCount <= AvailableLength);
                return;
            }

            // Double the size of the buffer until we have enough space.
            int desiredSize = ActiveLength + byteCount;
            int newSize = _bytes.Length;
            do
            {
                newSize *= 2;
            } while (newSize < desiredSize);

            byte[] newBytes = _usePool ?
                ArrayPool<byte>.Shared.Rent(newSize) :
                new byte[newSize];
            byte[] oldBytes = _bytes;

            if (ActiveLength != 0)
            {
                Buffer.BlockCopy(oldBytes, _activeStart, newBytes, 0, ActiveLength);
            }

            _availableStart = ActiveLength;
            _activeStart = 0;

            _bytes = newBytes;
            if (_usePool)
            {
                ArrayPool<byte>.Shared.Return(oldBytes);
            }

            Debug.Assert(byteCount <= AvailableLength);
        }

        // Ensure at least [byteCount] bytes to write to, up to the specified limit
        public void TryEnsureAvailableSpaceUpToLimit(int byteCount, int limit)
        {
            if (byteCount <= AvailableLength)
            {
                return;
            }

            int totalFree = _activeStart + AvailableLength;
            if (byteCount <= totalFree)
            {
                // We can free up enough space by just shifting the bytes down, so do so.
                Buffer.BlockCopy(_bytes, _activeStart, _bytes, 0, ActiveLength);
                _availableStart = ActiveLength;
                _activeStart = 0;
                Debug.Assert(byteCount <= AvailableLength);
                return;
            }

            if (_bytes.Length >= limit)
            {
                // Already at limit, can't grow further.
                return;
            }

            // Double the size of the buffer until we have enough space, or we hit the limit
            int desiredSize = Math.Min(ActiveLength + byteCount, limit);
            int newSize = _bytes.Length;
            do
            {
                newSize = Math.Min(newSize * 2, limit);
            } while (newSize < desiredSize);

            byte[] newBytes = _usePool ?
                ArrayPool<byte>.Shared.Rent(newSize) :
                new byte[newSize];
            byte[] oldBytes = _bytes;

            if (ActiveLength != 0)
            {
                Buffer.BlockCopy(oldBytes, _activeStart, newBytes, 0, ActiveLength);
            }

            _availableStart = ActiveLength;
            _activeStart = 0;

            _bytes = newBytes;
            if (_usePool)
            {
                ArrayPool<byte>.Shared.Return(oldBytes);
            }

            Debug.Assert(byteCount <= AvailableLength || desiredSize == limit);
        }

        public void Grow()
        {
            EnsureAvailableSpace(AvailableLength + 1);
        }
    }
}
