// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static Interop.Kernel32;

namespace System.IO.Tests
{
    internal class SectorAlignedMemory<T> : MemoryManager<T>
    {
        private bool disposed = false;
        private int refCount = 0;
        private IntPtr memory;
        private int length;

        private unsafe SectorAlignedMemory(void* memory, int length)
        {
            this.memory = (IntPtr)memory;
            this.length = length;
        }

        ~SectorAlignedMemory()
        {
            Dispose(false);
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

        public bool IsDisposed => disposed;

        public unsafe override Span<T> GetSpan() => new Span<T>((void*)memory, length);

        protected bool IsRetained => refCount > 0;

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            unsafe
            {
                Retain();
                if ((uint)elementIndex > length) throw new ArgumentOutOfRangeException(nameof(elementIndex));
                void* pointer = Unsafe.Add<T>((void*)memory, elementIndex);
                return new MemoryHandle(pointer, default, this);
            }
        }

        public bool Release()
        {
            int newRefCount = Interlocked.Decrement(ref refCount);

            if (newRefCount < 0)
            {
                throw new InvalidOperationException("Unmatched Release/Retain");
            }

            return newRefCount != 0;
        }

        public void Retain()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SectorAlignedMemory<T>));
            }

            Interlocked.Increment(ref refCount);
        }

        protected override unsafe void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            VirtualAlloc(
                memory.ToPointer(),
                new UIntPtr((uint)(Marshal.SizeOf<T>() * length)),
                MemOptions.MEM_FREE,
                PageOptions.PAGE_READWRITE);
            memory = IntPtr.Zero;

            disposed = true;
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
