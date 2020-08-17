// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

using Xunit;

namespace System.Buffers
{
    public static partial class BoundedMemory
    {
        private unsafe static UnixImplementation<T> AllocateWithoutDataPopulationUnix<T>(int elementCount, PoisonPagePlacement placement) where T : unmanaged
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

            void* ptr = UnsafeNativeMethods.mmap64(
                addr: null,
                length: (nuint)totalBytesToAllocate /* cast throws OverflowException if out of range */,
                prot: MemoryMappedProtections.PROT_NONE,
                flags: MemoryMappedFlags.MAP_PRIVATE | MemoryMappedFlags.MAP_ANONYMOUS,
                fd: -1,
                offset: 0);

            if (ptr == null || ptr == (void*)UIntPtr.MaxValue)
            {
                throw new Win32Exception();
            }

            // Done allocating! Now carve out a READWRITE section bookended by the NOACCESS
            // pages and return that carved-out section to the caller. Since memory protection
            // flags only apply at page-level granularity, we need to "left-align" or "right-
            // align" the section we carve out so that it's guaranteed adjacent to one of
            // the NOACCESS bookend pages.
            MemoryMappedHandle handle = new((IntPtr)ptr, (nuint)totalBytesToAllocate);
            return new UnixImplementation<T>(
                handle,
                byteOffsetIntoHandle: (placement == PoisonPagePlacement.Before)
                    ? SystemPageSize /* just after leading poison page */
                    : checked((int)(totalBytesToAllocate - SystemPageSize - cb)) /* just before trailing poison page */,
                elementCount: elementCount)
            {
                Protection = MemoryMappedProtections.PROT_READ | MemoryMappedProtections.PROT_WRITE
            };
        }

        private unsafe sealed class UnixImplementation<T> : BoundedMemory<T> where T : unmanaged
        {
            private readonly MemoryMappedHandle _handle;
            private readonly int _byteOffsetIntoHandle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;
            private MemoryMappedProtections _protection;

            internal UnixImplementation(MemoryMappedHandle handle, int byteOffsetIntoHandle, int elementCount)
            {
                _handle = handle;
                _byteOffsetIntoHandle = byteOffsetIntoHandle;
                _elementCount = elementCount;
                _memoryManager = new BoundedMemoryManager(this);
            }

            public override bool IsReadonly => (Protection == MemoryMappedProtections.PROT_READ);

            internal MemoryMappedProtections Protection
            {
                // Not sure if there's a easy way to retrieve the protection status for a given page.
                // Instead we'll save whatever was set in the setter and return that.
                get => _protection;

                set
                {
                    if (_elementCount > 0)
                    {
                        bool refAdded = false;
                        try
                        {
                            _handle.DangerousAddRef(ref refAdded);
                            // mprotect requires the pointer to be page size aligned.
                            // mmap guarantees that the addresses are page-size aligned - but we'll just make sure.
                            Debug.Assert((nuint)(nint)(_handle.DangerousGetHandle() + _byteOffsetIntoHandle) % (nuint)SystemPageSize == 0);
                            if (UnsafeNativeMethods.mprotect(
                                addr: (void*)(_handle.DangerousGetHandle() + _byteOffsetIntoHandle),
                                len: (nuint)(&((T*)null)[_elementCount]),
                                prot: value) != 0)
                            {
                                throw new Win32Exception();
                            }
                            _protection = value;
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
                Protection = MemoryMappedProtections.PROT_READ;
            }

            public override void MakeWriteable()
            {
                Protection = MemoryMappedProtections.PROT_READ | MemoryMappedProtections.PROT_WRITE;
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

                public override Span<T> GetSpan()
                {
                    throw new NotImplementedException();
                }

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

        private unsafe sealed class MemoryMappedHandle : SafeHandle
        {
            private nuint _length;

            private MemoryMappedHandle()
                : base(IntPtr.Zero, ownsHandle: true)
            {
            }

            public MemoryMappedHandle(IntPtr handle, nuint length) : base(handle, ownsHandle: true)
            {
                _length = length;
            }

            // Do not provide a finalizer - SafeHandle's critical finalizer will
            // call ReleaseHandle for you.

            public override bool IsInvalid => (handle == IntPtr.Zero);

            protected override bool ReleaseHandle() => UnsafeNativeMethods.munmap((void*)handle, _length) == 0;
        }

        [Flags]
        internal enum MemoryMappedProtections
        {
            PROT_NONE = 0x0,
            PROT_READ = 0x1,
            PROT_WRITE = 0x2,
            PROT_EXEC = 0x4
        }

        [Flags]
        internal enum MemoryMappedFlags
        {
            MAP_SHARED = 0x01,
            MAP_PRIVATE = 0x02,
            MAP_ANONYMOUS = 0x10,
        }

        private unsafe static partial class UnsafeNativeMethods
        {
            private const string Libc = "libc";

            // Not exactly sure if we can use mmap instead? the type of the last offset param is off_t
            // which doesn't seem particularly rigidly defined. (depends on some compile time variables)
            // We need to use mmap instead of other allocation functions because...
            // "POSIX says that the behavior of mprotect() is unspecified if it is applied to a region of memory...
            // ...that was not obtained via mmap(2)." (man-pages, Release 5.05, mprotect(2))
            [DllImport(Libc, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            public static extern void* mmap64(void* addr, nuint length, MemoryMappedProtections prot, MemoryMappedFlags flags, int fd, long offset);

            [DllImport(Libc, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            public static extern int munmap(void* addr, nuint length);

            [DllImport(Libc, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            public static extern int mprotect(void* addr, nuint len, MemoryMappedProtections prot);
        }
    }
}
