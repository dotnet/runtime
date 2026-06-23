// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Provides memory blocks backed by a pinned byte array.
    /// Used for prefetched images read from streams into pinned GC heap memory.
    /// </summary>
    internal sealed class PinnedArrayMemoryBlockProvider : MemoryBlockProvider
    {
        private byte[]? _array;
        private readonly int _size;
#if FEATURE_CER
        private GCHandle _handle;
#endif

        public PinnedArrayMemoryBlockProvider(byte[] pinnedArray, int size)
        {
            Debug.Assert(pinnedArray != null && size <= pinnedArray.Length);
            _array = pinnedArray;
            _size = size;
#if FEATURE_CER
            _handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
#endif
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(disposing);
#if FEATURE_CER
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
#endif
            _array = null;
        }

        public override int Size => _size;

        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            return new PinnedArrayMemoryBlock(_array!, start, size);
        }

        public unsafe byte* Pointer
        {
            get
            {
                byte[]? array = _array;
                if (array is null || array.Length == 0)
                {
                    return null;
                }

                return (byte*)Unsafe.AsPointer(ref array[0]);
            }
        }
    }
}
