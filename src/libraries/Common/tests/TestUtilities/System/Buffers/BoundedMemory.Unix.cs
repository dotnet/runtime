// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.MemoryMappedFiles;
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
            private readonly MmfHandle _handle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;

            public UnixImplementation(int elementCount, PoisonPagePlacement placement)
            {
                _handle = MmfHandle.Allocate(checked(elementCount * (nint)sizeof(T)), placement);
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

        private sealed class MmfHandle : SafeHandle
        {
            private MemoryMappedFile mmf;
            private MemoryMappedViewAccessor view;
            private IntPtr buffer;
            private ulong allocationSize;

            // Called by P/Invoke when returning SafeHandles
            private MmfHandle(MemoryMappedFile mmf, MemoryMappedViewAccessor view, IntPtr buffer, ulong allocationSize)
                : base(IntPtr.Zero, ownsHandle: true)
            {
                this.mmf = mmf;
                this.view = view;
                this.buffer = buffer;
                this.allocationSize = allocationSize;
            }

            internal static MmfHandle Allocate(nint byteLength, PoisonPagePlacement placement)
            {
                // Allocate number of pages to incorporate required (byteLength bytes of) memory and an additional page to create a poison page.
                int pageSize = Environment.SystemPageSize;
                int allocationSize = (int)(((byteLength / pageSize) + ((byteLength % pageSize) == 0 ? 0 : 1) + 1) * pageSize);

                // WILL REMOVE THIS, just need to test CI to make sure we can call it.
                var ptr = AllocWithGuard((nuint)allocationSize);
                Free(ptr, (nuint)allocationSize);
                
                var mmf = MemoryMappedFile.CreateNew(null, (long)allocationSize, MemoryMappedFileAccess.ReadWrite);
                var view = mmf.CreateViewAccessor();
                IntPtr buffer = view.SafeMemoryMappedViewHandle.DangerousGetHandle();

                // Depending on the PoisonPagePlacement requirement (before/after) initialise the baseAddress and poisonPageAddress to point to the location
                // in the buffer. Here the baseAddress points to the first valid allocation and poisonPageAddress points to the first invalid location.
                // For `PoisonPagePlacement.Before` the first page is made inaccessible using mprotect and baseAddress points to the start of the second page.
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
                    throw new InvalidOperationException($"Failed to mark page as a poison page using mprotect with error :{Marshal.GetLastPInvokeError()}.");
                }

                MmfHandle retVal = new MmfHandle(mmf, view, buffer, (ulong)allocationSize);
                retVal.SetHandle(baseAddress); // this base address would be used as the start of Span that is used during unit testing.
                return retVal;
            }

            public override bool IsInvalid => (handle == IntPtr.Zero);

            protected override bool ReleaseHandle()
            {
                view.Dispose();
                mmf.Dispose();
                return true;
            }

            // Defined in <sys/mman.h>
            public const int PROT_NONE = 0x0;
            public const int PROT_READ = 0x1;
            public const int PROT_WRITE = 0x2;

            [DllImport("XplatVirtualAlloc")]
            public static extern byte* AllocWithGuard(nuint size);


            [DllImport("XplatVirtualAlloc")]
            public static extern void Free(byte* ptr, nuint size);

            private static class Linux
            {

                [DllImport("libc", SetLastError = true)]
                public static extern int mprotect(IntPtr address, ulong length, int prot);
            }

            private static class Osx
            {

                [DllImport("libSystem", SetLastError = true)]
                public static extern int mprotect(IntPtr address, ulong length, int prot);
            }

            public static int mprotect(IntPtr address, ulong length, int prot)
            {
                if (OperatingSystem.IsLinux())
                {
                    return Linux.mprotect(address, length, prot);
                }

                return Osx.mprotect(address, length, prot);
            }
        }
    }
}
