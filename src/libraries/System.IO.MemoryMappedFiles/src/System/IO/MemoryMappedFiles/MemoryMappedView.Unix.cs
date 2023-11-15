// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    internal sealed partial class MemoryMappedView
    {
        public static MemoryMappedView CreateView(
            SafeMemoryMappedFileHandle memMappedFileHandle, MemoryMappedFileAccess access,
            long requestedOffset, long requestedSize)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(requestedOffset, memMappedFileHandle._capacity, "offset");
            if (requestedSize > MaxProcessAddressSpace)
            {
                throw new IOException(SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }
            if (requestedOffset + requestedSize > memMappedFileHandle._capacity)
            {
                throw new UnauthorizedAccessException();
            }
            ObjectDisposedException.ThrowIf(memMappedFileHandle.IsClosed, memMappedFileHandle);

            if (requestedSize == MemoryMappedFile.DefaultSize)
            {
                requestedSize = memMappedFileHandle._capacity - requestedOffset;
            }

            // mmap can only create views that start at a multiple of the page size. As on Windows,
            // we hide this restriction form the user by creating larger views than the user requested and hiding the parts
            // that the user did not request.  extraMemNeeded is the amount of extra memory we allocate before the start of the
            // requested view. (mmap may round up the actual length such that it is also page-aligned; we hide that by using
            // the right size and not extending the size to be page-aligned.)
            ulong nativeSize;
            long extraMemNeeded, nativeOffset;
            long pageSize = Interop.Sys.SysConf(Interop.Sys.SysConfName._SC_PAGESIZE);
            Debug.Assert(pageSize > 0);
            ValidateSizeAndOffset(
                requestedSize, requestedOffset, pageSize,
                out nativeSize, out extraMemNeeded, out nativeOffset);

            // Determine whether to create the pages as private or as shared; the former is used for copy-on-write.
            Interop.Sys.MemoryMappedFlags flags =
                (memMappedFileHandle._access == MemoryMappedFileAccess.CopyOnWrite || access == MemoryMappedFileAccess.CopyOnWrite) ?
                Interop.Sys.MemoryMappedFlags.MAP_PRIVATE :
                Interop.Sys.MemoryMappedFlags.MAP_SHARED;

            // If we have a file handle, get the file descriptor from it.  If the handle is null,
            // we'll use an anonymous backing store for the map.
            SafeFileHandle fd;
            if (memMappedFileHandle._fileStreamHandle != null)
            {
                // Get the file descriptor from the SafeFileHandle
                fd = memMappedFileHandle._fileStreamHandle;
                Debug.Assert(!fd.IsInvalid);
            }
            else
            {
                fd = new SafeFileHandle(new IntPtr(-1), false);
                flags |= Interop.Sys.MemoryMappedFlags.MAP_ANONYMOUS;
            }

            // Nothing to do for options.DelayAllocatePages, since we're only creating the map
            // with mmap when creating the view.

            // Verify that the requested view permissions don't exceed the map's permissions
            Interop.Sys.MemoryMappedProtections viewProtForVerification = GetProtections(access, forVerification: true);
            Interop.Sys.MemoryMappedProtections mapProtForVerification = GetProtections(memMappedFileHandle._access, forVerification: true);
            if ((viewProtForVerification & mapProtForVerification) != viewProtForVerification)
            {
                throw new UnauthorizedAccessException();
            }

            // viewProtections is strictly less than mapProtections, so use viewProtections for actually creating the map.
            Interop.Sys.MemoryMappedProtections viewProtForCreation = GetProtections(access, forVerification: false);

            // Create the map
            IntPtr addr;
            if (nativeSize > 0)
            {
                addr = Interop.Sys.MMap(
                    IntPtr.Zero,         // don't specify an address; let the system choose one
                    nativeSize,          // specify the rounded-size we computed so as to page align; size + extraMemNeeded
                    viewProtForCreation,
                    flags,
                    fd,                  // mmap adds a ref count to the fd, so there's no need to dup it.
                    nativeOffset);       // specify the rounded-offset we computed so as to page align; offset - extraMemNeeded
            }
            else
            {
                // There are some corner cases where the .NET API allows the requested size to be zero, e.g. the caller is
                // creating a map at the end of the capacity.  We can't pass 0 to mmap, as that'll fail with EINVAL, nor can
                // we create a map that extends beyond the end of the underlying file, as that'll fail on some platforms at the
                // time of the map's creation.  Instead, since there's no data to be read/written, it doesn't actually matter
                // what backs the view, so we just create an anonymous mapping.
                addr = Interop.Sys.MMap(
                    IntPtr.Zero,
                    1,         // any length that's greater than zero will suffice
                    viewProtForCreation,
                    flags | Interop.Sys.MemoryMappedFlags.MAP_ANONYMOUS,
                    new SafeFileHandle(new IntPtr(-1), false),        // ignore the actual fd even if there was one
                    0);
                requestedSize = 0;
                extraMemNeeded = 0;
            }
            if (addr == IntPtr.Zero) // note that shim uses null pointer, not non-null MAP_FAILED sentinel
            {
                throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
            }

            // Based on the HandleInheritability, try to prevent the memory-mapped region
            // from being inherited by a forked process
            if (memMappedFileHandle._inheritability == HandleInheritability.None)
            {
                DisableForkingIfPossible(addr, nativeSize);
            }

            // Create and return the view handle
            var viewHandle = new SafeMemoryMappedViewHandle(addr, ownsHandle: true);
            viewHandle.Initialize((ulong)nativeSize);
            return new MemoryMappedView(
                viewHandle,
                extraMemNeeded,       // the view points to offset - extraMemNeeded, so we need to shift back by extraMemNeeded
                requestedSize,        // only allow access to the actual size requested
                access);
        }

        public unsafe void Flush(UIntPtr capacity)
        {
            if (capacity == UIntPtr.Zero)
                return;

            byte* ptr = null;
            try
            {
                _viewHandle.AcquirePointer(ref ptr);
                int result = Interop.Sys.MSync(
                    (IntPtr)ptr, (ulong)capacity,
                    Interop.Sys.MemoryMappedSyncFlags.MS_SYNC | Interop.Sys.MemoryMappedSyncFlags.MS_INVALIDATE);
                if (result < 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }
            }
            finally
            {
                if (ptr != null)
                {
                    _viewHandle.ReleasePointer();
                }
            }
        }

        /// <summary>Attempt to prevent the specified pages from being copied into forked processes.</summary>
        /// <param name="addr">The starting address.</param>
        /// <param name="length">The length.</param>
        private static void DisableForkingIfPossible(IntPtr addr, ulong length)
        {
            if (length > 0)
            {
                Interop.Sys.MAdvise(addr, length, Interop.Sys.MemoryAdvice.MADV_DONTFORK);
                // Intentionally ignore error code -- it's just a hint and it's not supported on all systems.
            }
        }

        /// <summary>
        /// The Windows implementation limits maps to the size of the logical address space.
        /// We use the same value here.
        /// </summary>
        private const long MaxProcessAddressSpace = 8192L * 1000 * 1000 * 1000;

        /// <summary>Maps a MemoryMappedFileAccess to the associated MemoryMappedProtections.</summary>
        internal static Interop.Sys.MemoryMappedProtections GetProtections(
            MemoryMappedFileAccess access, bool forVerification)
        {
            switch (access)
            {
                default:
                case MemoryMappedFileAccess.Read:
                    return Interop.Sys.MemoryMappedProtections.PROT_READ;

                case MemoryMappedFileAccess.Write:
                    return Interop.Sys.MemoryMappedProtections.PROT_WRITE;

                case MemoryMappedFileAccess.ReadWrite:
                    return
                        Interop.Sys.MemoryMappedProtections.PROT_READ |
                        Interop.Sys.MemoryMappedProtections.PROT_WRITE;

                case MemoryMappedFileAccess.ReadExecute:
                    return
                        Interop.Sys.MemoryMappedProtections.PROT_READ |
                        Interop.Sys.MemoryMappedProtections.PROT_EXEC;

                case MemoryMappedFileAccess.ReadWriteExecute:
                    return
                        Interop.Sys.MemoryMappedProtections.PROT_READ |
                        Interop.Sys.MemoryMappedProtections.PROT_WRITE |
                        Interop.Sys.MemoryMappedProtections.PROT_EXEC;

                case MemoryMappedFileAccess.CopyOnWrite:
                    return forVerification ?
                        Interop.Sys.MemoryMappedProtections.PROT_READ :
                        Interop.Sys.MemoryMappedProtections.PROT_READ | Interop.Sys.MemoryMappedProtections.PROT_WRITE;
            }
        }
    }
}
