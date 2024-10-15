// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static Interop.Kernel32;

namespace System.IO.Tests
{
    internal sealed class SectorAlignedMemory<T> : MemoryManager<T>
    {
        private bool _disposed;
        private int _refCount;
        private IntPtr _memory;
        private int _length;

        private unsafe SectorAlignedMemory(void* memory, int length)
        {
            _memory = (IntPtr)memory;
            _length = length;
        }

        public static unsafe SectorAlignedMemory<T> Allocate(int length)
        {
            void* memory = VirtualAlloc(
                IntPtr.Zero.ToPointer(),
                new UIntPtr((uint)(Marshal.SizeOf<T>() * length)),
                MemOptions.MEM_COMMIT | MemOptions.MEM_RESERVE,
                PageOptions.PAGE_READWRITE);

            return new SectorAlignedMemory<T>(memory, length);
        }

        public bool IsDisposed => _disposed;

        public unsafe override Span<T> GetSpan() => new Span<T>((void*)_memory, _length);

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            unsafe
            {
                Retain();
                if ((uint)elementIndex > _length) throw new ArgumentOutOfRangeException(nameof(elementIndex));
                void* pointer = Unsafe.Add<T>((void*)_memory, elementIndex);
                return new MemoryHandle(pointer, default, this);
            }
        }

        private bool Release()
        {
            int newRefCount = Interlocked.Decrement(ref _refCount);

            if (newRefCount < 0)
            {
                throw new InvalidOperationException("Unmatched Release/Retain");
            }

            return newRefCount != 0;
        }

        private void Retain()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SectorAlignedMemory<T>));
            }

            Interlocked.Increment(ref _refCount);
        }

        protected override unsafe void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            VirtualAlloc(
                _memory.ToPointer(),
                new UIntPtr((uint)(Marshal.SizeOf<T>() * _length)),
                MemOptions.MEM_FREE,
                PageOptions.PAGE_READWRITE);
            _memory = IntPtr.Zero;

            _disposed = true;
        }

        protected override bool TryGetArray(out ArraySegment<T> arraySegment)
        {
            // cannot expose managed array
            arraySegment = default;
            return false;
        }

        public override void Unpin()
        {
            Release();
        }
    }
}
