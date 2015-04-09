// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        /// <summary>
        /// Used by the 2 Create factory method groups.  A null fileHandle specifies that the 
        /// memory mapped file should not be associated with an existing file on disk (ie start
        /// out empty).
        /// </summary>
        [SecurityCritical]
        private static unsafe SafeMemoryMappedFileHandle CreateCore(
            FileStream fileStream, string mapName, 
            HandleInheritability inheritability, MemoryMappedFileAccess access, 
            MemoryMappedFileOptions options, long capacity)
        {
            if (mapName != null)
            {
                // TODO: We currently do not support named maps.  We could possibly support 
                // named maps in the future by using shm_open / shm_unlink, as we do for
                // giving internal names to anonymous maps.  Issues to work through will include 
                // dealing with permissions, passing information from the creator of the 
                // map to another opener of it, etc.
                throw CreateNamedMapsNotSupportedException();
            }

            SafeFileHandle fileHandle = null;
            if (fileStream != null)
            {
                // This map is backed by a file.  Make sure the file's size is increased to be
                // at least as big as the requested capacity of the map.
                fileHandle = fileStream.SafeFileHandle;
                if (fileStream.Length < capacity)
                {
                    fileStream.SetLength(capacity);
                }
            }
            else
            {
                // This map is backed by memory-only.  With files, multiple views over the same map
                // will end up being able to share data through the same file-based backing store;
                // for anonymous maps, we need a similar backing store, or else multiple views would logically 
                // each be their own map and wouldn't share any data.  To achieve this, we create a POSIX shared 
                // memory object and use its file descriptor as the file handle.  However, we only do this when the 
                // permission is more than read-only.  We can't ftruncate to increase the size of a shared memory 
                // object that has read-only permissions, but we also don't need to worry about sharing
                // views over a read-only, anonymous, memory-backed map, because the data will never change, so all views
                // will always see zero and can't change that.  In that case, we just use the built-in anonymous support of
                // the map by leaving fileHandle as null.
                Interop.libc.MemoryMappedProtections protections = MemoryMappedView.GetProtections(access, forVerification: false);
                if ((protections & Interop.libc.MemoryMappedProtections.PROT_WRITE) != 0 && capacity > 0)
                {
                    mapName = "/AnonCoreFxMemMap_" + Guid.NewGuid().ToString("N"); // unique name must start with "/" and be < NAME_MAX length
                    fileHandle = CreateNewSharedMemoryObject(mapName, protections, capacity);
                }
            }

            return new SafeMemoryMappedFileHandle(mapName, fileHandle, inheritability, access, options, capacity);
        }

        /// <summary>
        /// Used by the CreateOrOpen factory method groups.
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle CreateOrOpenCore(
            string mapName, 
            HandleInheritability inheritability, MemoryMappedFileAccess access,
            MemoryMappedFileOptions options, long capacity)
        {
            // Since we don't support mapName != null, CreateOrOpenCore can't
            // be used to Open an existing map, and thus is identical to CreateCore.
            return CreateCore(null, mapName, inheritability, access, options, capacity);
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle OpenCore(
            string mapName, HandleInheritability inheritability, MemoryMappedFileAccess access, bool createOrOpen)
        {
            throw CreateNamedMapsNotSupportedException();
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle OpenCore(
            string mapName, HandleInheritability inheritability, MemoryMappedFileRights rights, bool createOrOpen)
        {
            throw CreateNamedMapsNotSupportedException();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Gets an exception indicating that named maps are not supported on this platform.</summary>
        private static Exception CreateNamedMapsNotSupportedException()
        {
            return new PlatformNotSupportedException(SR.PlatformNotSupported_NamedMaps);
        }

        private static SafeFileHandle CreateNewSharedMemoryObject(
            string mapName, Interop.libc.MemoryMappedProtections protections, long capacity)
        {
            // Determine the flags to use when creating the shared memory object
            Interop.libc.OpenFlags flags = (protections & Interop.libc.MemoryMappedProtections.PROT_WRITE) != 0 ?
                Interop.libc.OpenFlags.O_RDWR :
                Interop.libc.OpenFlags.O_RDONLY;
            flags |= Interop.libc.OpenFlags.O_CREAT | Interop.libc.OpenFlags.O_EXCL; // CreateNew

            // Create the shared memory object
            int fd;
            Interop.CheckIo(fd = Interop.libc.shm_open(mapName, flags, (int)Interop.libc.Permissions.S_IRWXU), mapName);
            SafeFileHandle fileHandle = new SafeFileHandle((IntPtr)fd, ownsHandle: true);

            // Then enlarge it to the requested capacity
            bool gotFd = false;
            fileHandle.DangerousAddRef(ref gotFd);
            try
            {
                while (Interop.CheckIo(Interop.libc.ftruncate((int)fileHandle.DangerousGetHandle(), capacity), mapName)) ;
            }
            finally
            {
                if (gotFd)
                {
                    fileHandle.DangerousRelease();
                }
            }

            // Return the fd for the object
            return fileHandle;
        }

    }
}
