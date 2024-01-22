// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    public static unsafe partial class BoundedMemory
    {
        private static readonly int SystemPageSize = Environment.SystemPageSize;

        private static WindowsImplementation<T> AllocateWithoutDataPopulationWindows<T>(int elementCount, PoisonPagePlacement placement) where T : unmanaged
        {
            long cb, totalBytesToAllocate;
            checked
            {
                cb = elementCount * sizeof(T);
                totalBytesToAllocate = cb;

                // We only need to round the count up if it's not an exact multiple
                // of the system page size.

                long leftoverBytes = totalBytesToAllocate % SystemPageSize;
                if (leftoverBytes != 0)
                {
                    totalBytesToAllocate += SystemPageSize - leftoverBytes;
                }

                // Finally, account for the poison pages at the front and back.

                totalBytesToAllocate += 2 * SystemPageSize;
            }

            // Reserve and commit the entire range as NOACCESS.

            VirtualAllocHandle handle = UnsafeNativeMethods.VirtualAlloc(
                lpAddress: IntPtr.Zero,
                dwSize: (IntPtr)totalBytesToAllocate /* cast throws OverflowException if out of range */,
                flAllocationType: VirtualAllocAllocationType.MEM_RESERVE | VirtualAllocAllocationType.MEM_COMMIT,
                flProtect: VirtualAllocProtection.PAGE_NOACCESS);

            if (handle == null || handle.IsInvalid)
            {
                int lastError = Marshal.GetHRForLastWin32Error();
                handle?.Dispose();
                Marshal.ThrowExceptionForHR(lastError);
                throw new InvalidOperationException("VirtualAlloc failed unexpectedly.");
            }

            // Done allocating! Now carve out a READWRITE section bookended by the NOACCESS
            // pages and return that carved-out section to the caller. Since memory protection
            // flags only apply at page-level granularity, we need to "left-align" or "right-
            // align" the section we carve out so that it's guaranteed adjacent to one of
            // the NOACCESS bookend pages.

            return new WindowsImplementation<T>(
                handle: handle,
                byteOffsetIntoHandle: (placement == PoisonPagePlacement.Before)
                    ? SystemPageSize /* just after leading poison page */
                    : checked((int)(totalBytesToAllocate - SystemPageSize - cb)) /* just before trailing poison page */,
                elementCount: elementCount)
            {
                Protection = VirtualAllocProtection.PAGE_READWRITE
            };
        }

        private sealed class WindowsImplementation<T> : BoundedMemory<T> where T : unmanaged
        {
            private readonly VirtualAllocHandle _handle;
            private readonly int _byteOffsetIntoHandle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;

            internal WindowsImplementation(VirtualAllocHandle handle, int byteOffsetIntoHandle, int elementCount)
            {
                _handle = handle;
                _byteOffsetIntoHandle = byteOffsetIntoHandle;
                _elementCount = elementCount;
                _memoryManager = new BoundedMemoryManager(this);
            }

            public override bool IsReadonly => (Protection != VirtualAllocProtection.PAGE_READWRITE);

            public override int Length => _elementCount;

            internal VirtualAllocProtection Protection
            {
                get
                {
                    bool refAdded = false;
                    try
                    {
                        _handle.DangerousAddRef(ref refAdded);
                        if (UnsafeNativeMethods.VirtualQuery(
                            lpAddress: _handle.DangerousGetHandle() + _byteOffsetIntoHandle,
                            lpBuffer: out MEMORY_BASIC_INFORMATION memoryInfo,
                            dwLength: (IntPtr)sizeof(MEMORY_BASIC_INFORMATION)) == IntPtr.Zero)
                        {
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                            throw new InvalidOperationException("VirtualQuery failed unexpectedly.");
                        }
                        return memoryInfo.Protect;
                    }
                    finally
                    {
                        if (refAdded)
                        {
                            _handle.DangerousRelease();
                        }
                    }
                }
                set
                {
                    if (_elementCount > 0)
                    {
                        bool refAdded = false;
                        try
                        {
                            _handle.DangerousAddRef(ref refAdded);
                            if (!UnsafeNativeMethods.VirtualProtect(
                                lpAddress: _handle.DangerousGetHandle() + _byteOffsetIntoHandle,
                                dwSize: (IntPtr)(&((T*)null)[_elementCount]),
                                flNewProtect: value,
                                lpflOldProtect: out _))
                            {
                                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                                throw new InvalidOperationException("VirtualProtect failed unexpectedly.");
                            }
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
            }

            public override Memory<T> Memory => _memoryManager.Memory;

            public override Span<T> Span
            {
                get
                {
                    bool refAdded = false;
                    try
                    {
                        _handle.DangerousAddRef(ref refAdded);
                        return new Span<T>((void*)(_handle.DangerousGetHandle() + _byteOffsetIntoHandle), _elementCount);
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
                Protection = VirtualAllocProtection.PAGE_READONLY;
            }

            public override void MakeWriteable()
            {
                Protection = VirtualAllocProtection.PAGE_READWRITE;
            }

            private sealed class BoundedMemoryManager : MemoryManager<T>
            {
                private readonly WindowsImplementation<T> _impl;

                public BoundedMemoryManager(WindowsImplementation<T> impl)
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
                        return new MemoryHandle((T*)(_impl._handle.DangerousGetHandle() + _impl._byteOffsetIntoHandle) + elementIndex);
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

        // from winnt.h
        [Flags]
        private enum VirtualAllocAllocationType : uint
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_DECOMMIT = 0x4000,
            MEM_RELEASE = 0x8000,
            MEM_FREE = 0x10000,
            MEM_PRIVATE = 0x20000,
            MEM_MAPPED = 0x40000,
            MEM_RESET = 0x80000,
            MEM_TOP_DOWN = 0x100000,
            MEM_WRITE_WATCH = 0x200000,
            MEM_PHYSICAL = 0x400000,
            MEM_ROTATE = 0x800000,
            MEM_LARGE_PAGES = 0x20000000,
            MEM_4MB_PAGES = 0x80000000,
        }

        // from winnt.h
        [Flags]
        private enum VirtualAllocProtection : uint
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public VirtualAllocProtection AllocationProtect;
            public IntPtr RegionSize;
            public VirtualAllocAllocationType State;
            public VirtualAllocProtection Protect;
            public VirtualAllocAllocationType Type;
        };

        private sealed class VirtualAllocHandle : SafeHandle
        {
            // Called by P/Invoke when returning SafeHandles
            public VirtualAllocHandle()
                : base(IntPtr.Zero, ownsHandle: true)
            {
            }

            // Do not provide a finalizer - SafeHandle's critical finalizer will
            // call ReleaseHandle for you.

            public override bool IsInvalid => (handle == IntPtr.Zero);

            protected override bool ReleaseHandle() =>
                UnsafeNativeMethods.VirtualFree(handle, IntPtr.Zero, VirtualAllocAllocationType.MEM_RELEASE);
        }

        private static partial class UnsafeNativeMethods
        {
            private const string KERNEL32_LIB = "kernel32.dll";

            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa366887(v=vs.85).aspx
            [LibraryImport(KERNEL32_LIB, SetLastError = true)]
            public static partial VirtualAllocHandle VirtualAlloc(
                IntPtr lpAddress,
                IntPtr dwSize,
                VirtualAllocAllocationType flAllocationType,
                VirtualAllocProtection flProtect);

            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa366892(v=vs.85).aspx
            [LibraryImport(KERNEL32_LIB, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool VirtualFree(
                IntPtr lpAddress,
                IntPtr dwSize,
                VirtualAllocAllocationType dwFreeType);

            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa366898(v=vs.85).aspx
            [LibraryImport(KERNEL32_LIB, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool VirtualProtect(
                IntPtr lpAddress,
                IntPtr dwSize,
                VirtualAllocProtection flNewProtect,
                out VirtualAllocProtection lpflOldProtect);

            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa366902(v=vs.85).aspx
            [LibraryImport(KERNEL32_LIB, SetLastError = true)]
            public static partial IntPtr VirtualQuery(
                IntPtr lpAddress,
                out MEMORY_BASIC_INFORMATION lpBuffer,
                IntPtr dwLength);
        }
    }
}
