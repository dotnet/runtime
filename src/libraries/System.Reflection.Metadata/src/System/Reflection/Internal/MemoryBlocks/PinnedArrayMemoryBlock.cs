// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Represents a memory block backed by a pinned byte array allocated on the pinned object heap.
    /// Provides both managed memory access (ReadOnlyMemory) and a valid pointer.
    /// </summary>
    internal sealed class PinnedArrayMemoryBlock : AbstractMemoryBlock
    {
        private byte[]? _array;
        private readonly int _offset;
        private readonly int _size;
#if FEATURE_CER
        private GCHandle _handle;
#endif

        internal PinnedArrayMemoryBlock(int size)
        {
            if (size == 0)
            {
                _array = Array.Empty<byte>();
            }
            else
            {
#if !FEATURE_CER
                _array = GC.AllocateArray<byte>(size, pinned: true);
#else
                _array = new byte[size];
                _handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
#endif
            }
            _offset = 0;
            _size = size;
        }

        internal PinnedArrayMemoryBlock(byte[] pinnedArray, int offset, int size)
        {
            _array = pinnedArray;
            _offset = offset;
            _size = size;
#if FEATURE_CER
            _handle = default;
#endif
        }

        public override void Dispose()
        {
#if FEATURE_CER
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
#endif
            _array = null;
        }

        public override unsafe byte* Pointer
        {
            get
            {
                byte[]? array = _array;
                if (array is null)
                {
                    return null;
                }

                if (array.Length == 0)
                {
                    return null;
                }

                return (byte*)Unsafe.AsPointer(ref array[_offset]);
            }
        }

        public override int Size => _size;

        public override ReadOnlyMemory<byte> GetMemory()
        {
            byte[]? array = _array;
            if (array is null)
            {
                return default;
            }

            return new ReadOnlyMemory<byte>(array, _offset, _size);
        }

        public override ImmutableArray<byte> GetContentUnchecked(int start, int length)
        {
            byte[]? array = _array;
            if (array is null)
            {
                return ImmutableArray<byte>.Empty;
            }

            return ImmutableArray.Create(array, _offset + start, length);
        }

        /// <summary>
        /// Gets the underlying array for direct writing (used during stream reads).
        /// </summary>
        internal byte[] GetArray() => _array!;
    }
}
