// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace System.Buffers
{
#if !NETFRAMEWORK
    public static unsafe partial class BoundedMemory
    {
        private static UnixImplementation<T> AllocateWithoutDataPopulationUnix<T>(int elementCount, PoisonPagePlacement placement) where T : unmanaged
        {
            return new UnixImplementation<T>(elementCount, placement);
        }

        private sealed class UnixImplementation<T> : BoundedMemory<T> where T : unmanaged
        {
            private readonly MMapHandle _handle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;

            public UnixImplementation(int elementCount, PoisonPagePlacement placement)
            {
                _handle = MMapHandle.Allocate(checked(elementCount * (nint)sizeof(T)), placement);
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

        private sealed class MMapHandle : SafeHandle
        {
            private IntPtr buffer;
            private ulong allocationSize;

            // Called by P/Invoke when returning SafeHandles
            private MMapHandle(IntPtr buffer, ulong allocationSize)
                : base(IntPtr.Zero, ownsHandle: true)
            {
                this.buffer = buffer;
                this.allocationSize = allocationSize;
            }

            internal static MMapHandle Allocate(nint byteLength, PoisonPagePlacement placement)
            {
                // Allocate number of pages to incorporate required (byteLength bytes of) memory and an additional page to create a poison page.
                int pageSize = Environment.SystemPageSize;
                int allocationSize = (int)(((byteLength / pageSize) + ((byteLength % pageSize) == 0 ? 0 : 1) + 1) * pageSize);
                IntPtr buffer = MMap(0, (ulong)allocationSize, (int)(MemoryMappedProtections.PROT_READ | MemoryMappedProtections.PROT_WRITE), (int)(MemoryMappedFlags.MAP_PRIVATE | MemoryMappedFlags.MAP_ANONYMOUS), -1, 0);

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
                if (MProtect(poisonPageAddress, (ulong)pageSize, (int)MemoryMappedProtections.PROT_NONE) == -1)
                {
                    throw new InvalidOperationException($"Failed to mark page as a poison page using mprotect with error :{Marshal.GetLastPInvokeError()}.");
                }

                MMapHandle retVal = new MMapHandle(buffer, (ulong)allocationSize);
                retVal.SetHandle(baseAddress); // this base address would be used as the start of Span that is used during unit testing.
                return retVal;
            }

            public override bool IsInvalid => (handle == IntPtr.Zero);

            protected override bool ReleaseHandle()
            {
                MUnmap(buffer, allocationSize);
                return true;
            }

            [Flags]
            private enum MemoryMappedProtections
            {
                PROT_NONE = 0x0,
                PROT_READ = 0x1,
                PROT_WRITE = 0x2,
                PROT_EXEC = 0x4
            }

            [Flags]
            private enum MemoryMappedFlags
            {
                MAP_SHARED = 0x01,
                MAP_PRIVATE = 0x02,
                MAP_ANONYMOUS = 0x10,
            }

            private const string SystemNative = "libSystem.Native";

            // NOTE: Shim returns null pointer on failure, not non-null MAP_FAILED sentinel.
            [DllImport(SystemNative, EntryPoint = "SystemNative_MMap", SetLastError = true)]
            private static extern IntPtr MMap(IntPtr addr, ulong len, int prot, int flags, IntPtr fd, long offset);

            [DllImport(SystemNative, EntryPoint = "SystemNative_MProtect", SetLastError = true)]
            private static extern int MProtect(IntPtr addr, ulong len, int prot);

            [DllImport(SystemNative, EntryPoint = "SystemNative_MUnmap", SetLastError = true)]
            internal static extern int MUnmap(IntPtr addr, ulong len);
        }
    }
#endif
}
