// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    internal partial class MemoryMappedView
    {
        [SecurityCritical]
        public unsafe static MemoryMappedView CreateView(
            SafeMemoryMappedFileHandle memMappedFileHandle, MemoryMappedFileAccess access, 
            long requestedOffset, long requestedSize)
        {
            if (requestedOffset > memMappedFileHandle._capacity)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (requestedSize > MaxProcessAddressSpace)
            {
                throw new IOException(SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }
            if (requestedOffset + requestedSize > memMappedFileHandle._capacity)
            {
                throw new UnauthorizedAccessException();
            }
            if (memMappedFileHandle.IsClosed)
            {
                throw new ObjectDisposedException(typeof(MemoryMappedFile).Name);
            }

            if (requestedSize == MemoryMappedFile.DefaultSize)
            {
                requestedSize = memMappedFileHandle._capacity - requestedOffset;
            }

            // mmap can only create views that start at a multiple of the page size. As on Windows,
            // we hide this restriction form the user by creating larger views than the user requested and hiding the parts
            // that the user did not request.  extraMemNeeded is the amount of extra memory we allocate before the start of the 
            // requested view. (mmap may round up the actual length such that it is also page-aligned; we hide that by using
            // the right size and not extending the size to be page-aligned.)
            ulong nativeSize, extraMemNeeded, nativeOffset;
            int pageSize = Interop.libc.sysconf(Interop.libc.SysConfNames._SC_PAGESIZE);
            Debug.Assert(pageSize > 0);
            ValidateSizeAndOffset(
                requestedSize, requestedOffset, pageSize, 
                out nativeSize, out extraMemNeeded, out nativeOffset);

            bool gotRefOnHandle = false;
            try
            {
                // Determine whether to create the pages as private or as shared; the former is used for copy-on-write.
                Interop.libc.MemoryMappedFlags flags = 
                    (memMappedFileHandle._access == MemoryMappedFileAccess.CopyOnWrite || access == MemoryMappedFileAccess.CopyOnWrite) ?
                    Interop.libc.MemoryMappedFlags.MAP_PRIVATE :
                    Interop.libc.MemoryMappedFlags.MAP_SHARED;

                // If we have a file handle, get the file descriptor from it.  If the handle is null,
                // we'll use an anonymous backing store for the map.
                int fd;
                if (memMappedFileHandle._fileStream != null)
                {
                    // Get the file descriptor from the SafeFileHandle
                    memMappedFileHandle._fileStream.SafeFileHandle.DangerousAddRef(ref gotRefOnHandle);
                    Debug.Assert(gotRefOnHandle);
                    fd = (int)memMappedFileHandle._fileStream.SafeFileHandle.DangerousGetHandle();
                    Debug.Assert(fd >= 0);
                }
                else
                {
                    Debug.Assert(!gotRefOnHandle);
                    fd = -1;
                    flags |= Interop.libc.MemoryMappedFlags.MAP_ANONYMOUS;
                }

                // Nothing to do for options.DelayAllocatePages, since we're only creating the map
                // with mmap when creating the view.

                // Verify that the requested view permissions don't exceed the map's permissions
                Interop.libc.MemoryMappedProtections viewProtForVerification = GetProtections(access, forVerification: true);
                Interop.libc.MemoryMappedProtections mapProtForVerification = GetProtections(memMappedFileHandle._access, forVerification: true);
                if ((viewProtForVerification & mapProtForVerification) != viewProtForVerification)
                {
                    throw new UnauthorizedAccessException();
                }

                // viewProtections is strictly less than mapProtections, so use viewProtections for actually creating the map.
                Interop.libc.MemoryMappedProtections viewProtForCreation = GetProtections(access, forVerification: false);

                // Create the map
                IntPtr addr = IntPtr.Zero;
                if (nativeSize > 0)
                {
                    addr = Interop.libc.mmap(
                        IntPtr.Zero,         // don't specify an address; let the system choose one
                        (IntPtr)nativeSize,  // specify the rounded-size we computed so as to page align; size + extraMemNeeded
                        viewProtForCreation,
                        flags,
                        fd,                  // mmap adds a ref count to the fd, so there's no need to dup it.
                        (long)nativeOffset); // specify the rounded-offset we computed so as to page align; offset - extraMemNeeded
                }
                else
                {
                    // There are some corner cases where the .NET API allows the requested size to be zero, e.g. the caller is 
                    // creating a map at the end of the capacity.  We can't pass 0 to mmap, as that'll fail with EINVAL, nor can 
                    // we create a map that extends beyond the end of the underlying file, as that'll fail on some platforms at the 
                    // time of the map's creation.  Instead, since there's no data to be read/written, it doesn't actually matter 
                    // what backs the view, so we just create an anonymous mapping.
                    addr = Interop.libc.mmap(
                        IntPtr.Zero,
                        (IntPtr)1, // any length that's greater than zero will suffice
                        viewProtForCreation,
                        flags | Interop.libc.MemoryMappedFlags.MAP_ANONYMOUS,
                        -1,        // ignore the actual fd even if there was one
                        0);
                    requestedSize = 0;
                    extraMemNeeded = 0;
                }
                if ((long)addr < 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }

                // Based on the HandleInheritability, try to prevent the memory-mapped region 
                // from being inherited by a forked process
                if (memMappedFileHandle._inheritability == HandleInheritability.None)
                {
                    DisableForkingIfPossible(addr, (IntPtr)nativeSize);
                }

                // Create and return the view handle
                var viewHandle = new SafeMemoryMappedViewHandle(addr, ownsHandle: true);
                viewHandle.Initialize(nativeSize);
                return new MemoryMappedView(
                    viewHandle, 
                    (long)extraMemNeeded, // the view points to offset - extraMemNeeded, so we need to shift back by extraMemNeeded
                    requestedSize,        // only allow access to the actual size requested
                    access);
            }
            finally
            {
                if (gotRefOnHandle)
                {
                    memMappedFileHandle._fileStream.SafeFileHandle.DangerousRelease();
                }
            }
        }

        [SecurityCritical]
        public unsafe void Flush(UIntPtr capacity)
        {
            if (capacity == UIntPtr.Zero)
                return;

            byte* ptr = null;
            try
            {
                _viewHandle.AcquirePointer(ref ptr);
                int result = Interop.libc.msync(
                    (IntPtr)ptr, (IntPtr)(long)capacity, 
                    Interop.libc.MemoryMappedSyncFlags.MS_SYNC | Interop.libc.MemoryMappedSyncFlags.MS_INVALIDATE);
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

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Attempt to prevent the specified pages from being copied into forked processes.</summary>
        /// <param name="addr">The starting address.</param>
        /// <param name="length">The length.</param>
        static partial void DisableForkingIfPossible(IntPtr addr, IntPtr length);

        /// <summary>
        /// The Windows implementation limits maps to the size of the logical address space.
        /// We use the same value here.
        /// </summary>
        private const long MaxProcessAddressSpace = 8192L * 1000 * 1000 * 1000;

        /// <summary>Maps a MemoryMappedFileAccess to the associated MemoryMappedProtections.</summary>
        internal static Interop.libc.MemoryMappedProtections GetProtections(
            MemoryMappedFileAccess access, bool forVerification)
        {
            switch (access)
            {
                default:
                case MemoryMappedFileAccess.Read:
                    return Interop.libc.MemoryMappedProtections.PROT_READ;

                case MemoryMappedFileAccess.Write:
                    return Interop.libc.MemoryMappedProtections.PROT_WRITE;

                case MemoryMappedFileAccess.ReadWrite:
                    return
                        Interop.libc.MemoryMappedProtections.PROT_READ |
                        Interop.libc.MemoryMappedProtections.PROT_WRITE;

                case MemoryMappedFileAccess.ReadExecute:
                    return
                        Interop.libc.MemoryMappedProtections.PROT_READ |
                        Interop.libc.MemoryMappedProtections.PROT_EXEC;

                case MemoryMappedFileAccess.ReadWriteExecute:
                    return
                        Interop.libc.MemoryMappedProtections.PROT_READ |
                        Interop.libc.MemoryMappedProtections.PROT_WRITE |
                        Interop.libc.MemoryMappedProtections.PROT_EXEC;

                case MemoryMappedFileAccess.CopyOnWrite:
                    return forVerification ?
                        Interop.libc.MemoryMappedProtections.PROT_READ :
                        Interop.libc.MemoryMappedProtections.PROT_READ | Interop.libc.MemoryMappedProtections.PROT_WRITE;
            }
        }
        
    }
}
