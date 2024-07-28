// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    public static unsafe partial class BoundedMemory
    {
        private static UnixImplementation<T> AllocateWithoutDataPopulationUnix<T>(int elementCount, PoisonPagePlacement placement) where T : unmanaged
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

            MMapHandle handle = MMapHandle.Allocate(
                address: IntPtr.Zero, 
                length: checked((nuint)totalBytesToAllocate),
                prot: UnsafeNativeMethods.PROT_NONE,
                flags: UnsafeNativeMethods.MAP_PRIVATE | UnsafeNativeMethods.MAP_ANONYMOUS);

            if (handle == null || handle.IsInvalid)
            {
                int lastError = Marshal.GetLastPInvokeError();
                handle?.Dispose();
                throw new InvalidOperationException($"mmap failed unexpectedly with {lastError}.");
            }

            // Done allocating! Now carve out a READWRITE section bookended by the NOACCESS
            // pages and return that carved-out section to the caller. Since memory protection
            // flags only apply at page-level granularity, we need to "left-align" or "right-
            // align" the section we carve out so that it's guaranteed adjacent to one of
            // the NOACCESS bookend pages.

            return new UnixImplementation<T>(
                handle: handle,
                byteOffsetIntoHandle: (placement == PoisonPagePlacement.Before)
                    ? SystemPageSize /* just after leading poison page */
                    : checked((int)(totalBytesToAllocate - SystemPageSize - cb)) /* just before trailing poison page */,
                elementCount: elementCount)
            {
                Protection = UnsafeNativeMethods.PROT_WRITE | UnsafeNativeMethods.PROT_READ
            };
        }

        private sealed class UnixImplementation<T> : BoundedMemory<T> where T : unmanaged
        {
            private readonly MMapHandle _handle;
            private readonly int _byteOffsetIntoHandle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;
            private int _prot;

            internal UnixImplementation(MMapHandle handle, int byteOffsetIntoHandle, int elementCount)
            {
                _handle = handle;
                _byteOffsetIntoHandle = byteOffsetIntoHandle;
                _elementCount = elementCount;
                _memoryManager = new BoundedMemoryManager(this);
                _prot = UnsafeNativeMethods.PROT_NONE;
            }

            public override bool IsReadonly => (Protection != (UnsafeNativeMethods.PROT_WRITE | UnsafeNativeMethods.PROT_READ));

            public override int Length => _elementCount;

            internal int Protection
            {
                get
                {
                    return _prot;
                }
                set
                {
                    bool refAdded = false;
                    try
                    {
                        _handle.DangerousAddRef(ref refAdded);
                        if (UnsafeNativeMethods.mprotect(_handle.DangerousGetHandle(), _handle.Length, value) != 0)
                        {
                            throw new InvalidOperationException($"mprotected failed with {Marshal.GetLastPInvokeError()}.");
                        }
                        _prot = value;
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
                Protection = UnsafeNativeMethods.PROT_READ;
            }

            public override void MakeWriteable()
            {
                Protection = UnsafeNativeMethods.PROT_WRITE | UnsafeNativeMethods.PROT_READ;
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

        private sealed class MMapHandle : SafeHandle
        {
            public nuint Length { get; private set; }

            // Called by P/Invoke when returning SafeHandles
            public MMapHandle(nuint length)
                : base(IntPtr.Zero, ownsHandle: true)
            {
                Length = length;
            }

            internal static MMapHandle Allocate(IntPtr address, nuint length, int prot, int flags)
            {
                MMapHandle retVal = new MMapHandle(length);
                retVal.SetHandle(UnsafeNativeMethods.mmap(address, length, prot, flags, -1, 0));
                return retVal;
            }

            // Do not provide a finalizer - SafeHandle's critical finalizer will
            // call ReleaseHandle for you.

            public override bool IsInvalid => (handle == IntPtr.Zero);

            protected override bool ReleaseHandle() =>
                UnsafeNativeMethods.munmap(handle, Length) == 0;
        }

        private static partial class UnsafeNativeMethods
        {
            // Defined in <sys/mman.h>
            public const int MAP_PRIVATE = 0x2;
            public static readonly int MAP_ANONYMOUS = OperatingSystem.IsLinux() ? 0x20 : 0x1000;
            public const int PROT_NONE = 0x0;
            public const int PROT_READ = 0x1;
            public const int PROT_WRITE = 0x2;

            private static class Linux
            {
                [DllImport("libc", SetLastError = true)]
                public static extern IntPtr mmap(IntPtr address, ulong length, int prot, int flags, int fd, int offset);

                [DllImport("libc", SetLastError = true)]
                public static extern IntPtr munmap(IntPtr address, ulong length);

                [DllImport("libc", SetLastError = true)]
                public static extern int mprotect(IntPtr address, ulong length, int prot);
            }

            private static class Osx
            {
                [DllImport("libSystem", SetLastError = true)]
                public static extern IntPtr mmap(IntPtr address, ulong length, int prot, int flags, int fd, int offset);

                [DllImport("libSystem", SetLastError = true)]
                public static extern IntPtr munmap(IntPtr address, ulong length);

                [DllImport("libSystem", SetLastError = true)]
                public static extern int mprotect(IntPtr address, ulong length, int prot);
            }

            public static IntPtr mmap(IntPtr address, ulong length, int prot, int flags, int fd, int offset)
            {
                if (OperatingSystem.IsLinux())
                {
                    return Linux.mmap(address, length, prot, flags, fd, offset);
                }

                return Osx.mmap(address, length, prot, flags, fd, offset);
            }

            public static IntPtr munmap(IntPtr address, ulong length)
            {
                if (OperatingSystem.IsLinux())
                {
                    return Linux.munmap(address, length);
                }

                return Osx.munmap(address, length);
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
