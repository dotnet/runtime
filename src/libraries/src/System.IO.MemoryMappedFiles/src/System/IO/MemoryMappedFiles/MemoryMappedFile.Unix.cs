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
                // Named maps are not supported in our Unix implementation.  We could support named maps on Linux using 
                // shared memory segments (shmget/shmat/shmdt/shmctl/etc.), but that doesn't work on OSX by default due
                // to very low default limits on OSX for the size of such objects; it also doesn't support behaviors
                // like copy-on-write or the ability to control handle inheritability, and reliably cleaning them up
                // relies on some non-conforming behaviors around shared memory IDs remaining valid even after they've
                // been marked for deletion (IPC_RMID).  We could also support named maps using the current implementation
                // by not unlinking after creating the backing store, but then the backing stores would remain around
                // and accessible even after process exit, with no good way to appropriately clean them up.
                // (File-backed maps may still be used for cross-process communication.)
                throw CreateNamedMapsNotSupportedException();
            }

            bool ownsFileStream = false;
            if (fileStream != null)
            {
                // This map is backed by a file.  Make sure the file's size is increased to be
                // at least as big as the requested capacity of the map.
                if (fileStream.Length < capacity)
                {
                    try
                    {
                        fileStream.SetLength(capacity);
                    }
                    catch (ArgumentException exc)
                    {
                        // If the capacity is too large, we'll get an ArgumentException from SetLength, 
                        // but on Windows this same condition is represented by an IOException.
                        throw new IOException(exc.Message, exc);
                    }
                }
            }
            else
            {
                // This map is backed by memory-only.  With files, multiple views over the same map
                // will end up being able to share data through the same file-based backing store;
                // for anonymous maps, we need a similar backing store, or else multiple views would logically 
                // each be their own map and wouldn't share any data.  To achieve this, we create a backing object
                // (either memory or on disk, depending on the system) and use its file descriptor as the file handle.  
                // However, we only do this when the permission is more than read-only.  We can't change the size 
                // of an object that has read-only permissions, but we also don't need to worry about sharing
                // views over a read-only, anonymous, memory-backed map, because the data will never change, so all views
                // will always see zero and can't change that.  In that case, we just use the built-in anonymous support of
                // the map by leaving fileStream as null.
                Interop.libc.MemoryMappedProtections protections = MemoryMappedView.GetProtections(access, forVerification: false);
                if ((protections & Interop.libc.MemoryMappedProtections.PROT_WRITE) != 0 && capacity > 0)
                {
                    ownsFileStream = true;
                    fileStream = CreateSharedBackingObject(protections, capacity);
                }
            }

            return new SafeMemoryMappedFileHandle(fileStream, ownsFileStream, inheritability, access, options, capacity);
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
        
        private static FileAccess TranslateProtectionsToFileAccess(Interop.libc.MemoryMappedProtections protections)
        {
            return
                (protections & (Interop.libc.MemoryMappedProtections.PROT_READ | Interop.libc.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.ReadWrite :
                (protections & (Interop.libc.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.Write :
                FileAccess.Read;
        }

    }
}
