// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    public static unsafe partial class BoundedMemory
    {
        private static UnixImplementation<T> AllocateWithoutDataPopulationUnix<T>(int elementCount, PoisonPagePlacement placement) where T : unmanaged
        {
            return new UnixImplementation<T>(elementCount, placement);
        }

        private sealed class UnixImplementation<T> : BoundedMemory<T> where T : unmanaged
        {
            private readonly AllocHGlobalHandle _handle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;

            public UnixImplementation(int elementCount, PoisonPagePlacement placement)
            {
                _handle = AllocHGlobalHandle.Allocate(checked(elementCount * (nint)sizeof(T)), placement);
                _elementCount = elementCount;
                _memoryManager = new BoundedMemoryManager(this);
            }

            public override bool IsReadonly => false;

            public override int Length => _elementCount;

            public override Memory<T> Memory => _memoryManager.Memory;

            public override Span<T> Span
            {
                get
                {
                    bool refAdded = false;
                    try
                    {
                        _handle.DangerousAddRef(ref refAdded);
                        return new Span<T>((void*)_handle.DangerousGetHandle(), _elementCount);
                    }
                    finally
                    {
                        if (refAdded)
                        {
                            _handle.DangerousRelease();
                        }
                    }
                }
            }

            public override void Dispose()
            {
                _handle.Dispose();
            }

            public override void MakeReadonly()
            {
                // no-op
            }

            public override void MakeWriteable()
            {
                // no-op
            }

            private sealed class BoundedMemoryManager : MemoryManager<T>
            {
                private readonly UnixImplementation<T> _impl;

                public BoundedMemoryManager(UnixImplementation<T> impl)
                {
                    _impl = impl;
                }

                public override Memory<T> Memory => CreateMemory(_impl._elementCount);

                protected override void Dispose(bool disposing)
                {
                    // no-op; the handle will be disposed separately
                }

                public override Span<T> GetSpan() => _impl.Span;

                public override MemoryHandle Pin(int elementIndex)
                {
                    if ((uint)elementIndex > (uint)_impl._elementCount)
                    {
                        throw new ArgumentOutOfRangeException(paramName: nameof(elementIndex));
                    }

                    bool refAdded = false;
                    try
                    {
                        _impl._handle.DangerousAddRef(ref refAdded);
                        return new MemoryHandle((T*)_impl._handle.DangerousGetHandle() + elementIndex);
                    }
                    finally
                    {
                        if (refAdded)
                        {
                            _impl._handle.DangerousRelease();
                        }
                    }
                }

                public override void Unpin()
                {
                    // no-op - we don't unpin native memory
                }
            }
        }

        private sealed class AllocHGlobalHandle : SafeHandle
        {
            private IntPtr buffer;
            private Int32 allocationSize;

            // Called by P/Invoke when returning SafeHandles
            private AllocHGlobalHandle(IntPtr buffer, Int32 allocationSize)
                : base(IntPtr.Zero, ownsHandle: true)
            {
                this.buffer = buffer;
                this.allocationSize = allocationSize;
            }

            internal static AllocHGlobalHandle Allocate(nint byteLength, PoisonPagePlacement placement)
            {

                // Allocate number of pages to incorporate required (byteLength bytes of) memory and an additional page to create a poison page.
                Int32 pageSize = Environment.SystemPageSize;
                Int32 allocationSize = (Int32)((byteLength % pageSize) + 2) * pageSize;
                IntPtr buffer = memalign(pageSize, allocationSize);

                if (buffer == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Memory allocation failed.");
                }

                // Depending on the PoisonPagePlacement requirement (before/after) initialise the baseAddress and poisonPageAddress to point to the location
                // in the buffer. Here the baseAddress points to the first valid allocation and poisonPageAddress points to the first invalid location.
                // For PoisonPagePlacement.Before the first page is made inaccessible using mprotect and baseAddress points to the start of the second page.
                // The allocation and protection is at the granularity of a page. Thus, `PoisonPagePlacement.Before` configuration has an additional accessible
                // memory at the end of the page (bytes equivalent to `pageSize - (byteLength % pageSize)`).
                // For `PoisonPagePlacement.After`, we adjust the baseAddress so that inaccessible memory is at the `byteLength` offset from the baseAddress.
                IntPtr baseAddress = buffer + pageSize;
                IntPtr poisonPageAddress = buffer;
                if (placement == PoisonPagePlacement.After)
                {
                    baseAddress = buffer + (allocationSize - pageSize - byteLength);
                    poisonPageAddress = buffer + (allocationSize - pageSize);
                }

                // Protect the page before/after based on the poison page placement.
                if (mprotect(poisonPageAddress, (ulong)pageSize, PROT_NONE) == -1)
                {
                    throw new InvalidOperationException("Failed to mark page as a poison page using mprotect.");
                }

                AllocHGlobalHandle retVal = new AllocHGlobalHandle(buffer, allocationSize);
                retVal.SetHandle(baseAddress); // this base address would be used as the start of Span that is used during unit testing.
                return retVal;
            }

            public override bool IsInvalid => (handle == IntPtr.Zero);

            protected override bool ReleaseHandle()
            {
                // Reset the protection on the allocated memory.
                if (mprotect(buffer, (ulong)allocationSize, PROT_READ | PROT_WRITE) == -1)
                {
                    throw new InvalidOperationException("Failed to reset memory protection using mprotect.");
                }
                free(buffer);
                return true;
            }

            const int PROT_NONE = 0x0;
            const int PROT_READ = 0x1;
            const int PROT_WRITE = 0x2;

            [DllImport("libc", SetLastError = true)]
            static extern IntPtr memalign(int alignment, int size);

            [DllImport("libc", SetLastError = true)]
            static extern int mprotect(IntPtr addr, ulong len, int prot);

            [DllImport("libc", SetLastError = true)]
            static extern void free(IntPtr ptr);
        }
    }
}
