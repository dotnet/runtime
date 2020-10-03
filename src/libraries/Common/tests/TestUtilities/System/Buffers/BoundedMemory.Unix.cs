// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

using Microsoft.Win32.SafeHandles;

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

            MemoryMappedFile map = MemoryMappedFile.CreateNew(null, totalBytesToAllocate,
                MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, IO.HandleInheritability.None);

            MemoryMappedViewAccessor accessor = map.CreateViewAccessor();
            SafeMemoryMappedViewHandle handle = accessor.SafeMemoryMappedViewHandle;

            bool refAdded = false;
            try
            {
                handle.DangerousAddRef(ref refAdded);
                IntPtr ptr = handle.DangerousGetHandle();
                // mprotect requires the addresses to be page-size aligned.
                Debug.Assert((nuint)(nint)ptr % (nuint)SystemPageSize == 0);
                if (UnsafeNativeMethods.mprotect((void*)ptr, checked((nuint)totalBytesToAllocate), MemoryProtections.PROT_NONE) != 0)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                if (refAdded)
                {
                    handle.DangerousRelease();
                }
            }

            // Done allocating! Now carve out a READWRITE section bookended by the NOACCESS
            // pages and return that carved-out section to the caller. Since memory protection
            // flags only apply at page-level granularity, we need to "left-align" or "right-
            // align" the section we carve out so that it's guaranteed adjacent to one of
            // the NOACCESS bookend pages.
            return new UnixImplementation<T>(
                handle,
                byteOffsetIntoHandle: (placement == PoisonPagePlacement.Before)
                    ? SystemPageSize /* just after leading poison page */
                    : checked((int)(totalBytesToAllocate - SystemPageSize - cb)) /* just before trailing poison page */,
                elementCount: elementCount)
            {
                Protection = MemoryProtections.PROT_READ | MemoryProtections.PROT_WRITE
            };
        }

        private unsafe sealed class UnixImplementation<T> : BoundedMemory<T> where T : unmanaged
        {
            private readonly SafeMemoryMappedViewHandle _handle;
            private readonly int _byteOffsetIntoHandle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;
            private MemoryProtections _protection;

            internal UnixImplementation(SafeMemoryMappedViewHandle handle, int byteOffsetIntoHandle, int elementCount)
            {
                _handle = handle;
                _byteOffsetIntoHandle = byteOffsetIntoHandle;
                _elementCount = elementCount;
                _memoryManager = new BoundedMemoryManager(this);
            }

            public override bool IsReadonly => (Protection == MemoryProtections.PROT_READ);

            internal MemoryProtections Protection
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
                                addr: (void*)((nint)(_handle.DangerousGetHandle() + _byteOffsetIntoHandle) % SystemPageSize),
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
                Protection = MemoryProtections.PROT_READ;
            }

            public override void MakeWriteable()
            {
                Protection = MemoryProtections.PROT_READ | MemoryProtections.PROT_WRITE;
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

        [Flags]
        internal enum MemoryProtections
        {
            PROT_NONE = 0x0,
            PROT_READ = 0x1,
            PROT_WRITE = 0x2,
            PROT_EXEC = 0x4
        }

        private unsafe static partial class UnsafeNativeMethods
        {
            private const string Libc = "libc";

            [DllImport(Libc, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            public static extern int mprotect(void* addr, nuint len, MemoryProtections prot);
        }
    }
}
