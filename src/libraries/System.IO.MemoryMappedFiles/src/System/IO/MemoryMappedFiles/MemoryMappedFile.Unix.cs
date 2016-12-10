// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        /// <summary>
        /// Used by the 2 Create factory method groups.  A null fileHandle specifies that the 
        /// memory mapped file should not be associated with an existing file on disk (i.e. start
        /// out empty).
        /// </summary>
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
                Interop.Sys.MemoryMappedProtections protections = MemoryMappedView.GetProtections(access, forVerification: false);
                if ((protections & Interop.Sys.MemoryMappedProtections.PROT_WRITE) != 0 && capacity > 0)
                {
                    ownsFileStream = true;
                    fileStream = CreateSharedBackingObject(protections, capacity);

                    // If the MMF handle should not be inherited, mark the backing object fd as O_CLOEXEC.
                    if (inheritability == HandleInheritability.None)
                    {
                        Interop.CheckIo(Interop.Sys.Fcntl.SetCloseOnExec(fileStream.SafeFileHandle));
                    }
                }
            }

            return new SafeMemoryMappedFileHandle(fileStream, ownsFileStream, inheritability, access, options, capacity);
        }

        /// <summary>
        /// Used by the CreateOrOpen factory method groups.
        /// </summary>
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
        
        private static FileAccess TranslateProtectionsToFileAccess(Interop.Sys.MemoryMappedProtections protections)
        {
            return
                (protections & (Interop.Sys.MemoryMappedProtections.PROT_READ | Interop.Sys.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.ReadWrite :
                (protections & (Interop.Sys.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.Write :
                FileAccess.Read;
        }

        private static FileStream CreateSharedBackingObject(Interop.Sys.MemoryMappedProtections protections, long capacity)
        {
            return CreateSharedBackingObjectUsingMemory(protections, capacity)
                ?? CreateSharedBackingObjectUsingFile(protections, capacity);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static FileStream CreateSharedBackingObjectUsingMemory(
           Interop.Sys.MemoryMappedProtections protections, long capacity)
        {
            // The POSIX shared memory object name must begin with '/'.  After that we just want something short and unique.
            string mapName = "/corefx_map_" + Guid.NewGuid().ToString("N");

            // Determine the flags to use when creating the shared memory object
            Interop.Sys.OpenFlags flags = (protections & Interop.Sys.MemoryMappedProtections.PROT_WRITE) != 0 ?
                Interop.Sys.OpenFlags.O_RDWR :
                Interop.Sys.OpenFlags.O_RDONLY;
            flags |= Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL; // CreateNew

            // Determine the permissions with which to create the file
            Interop.Sys.Permissions perms = default(Interop.Sys.Permissions);
            if ((protections & Interop.Sys.MemoryMappedProtections.PROT_READ) != 0)
                perms |= Interop.Sys.Permissions.S_IRUSR;
            if ((protections & Interop.Sys.MemoryMappedProtections.PROT_WRITE) != 0)
                perms |= Interop.Sys.Permissions.S_IWUSR;
            if ((protections & Interop.Sys.MemoryMappedProtections.PROT_EXEC) != 0)
                perms |= Interop.Sys.Permissions.S_IXUSR;

            // Create the shared memory object.
            SafeFileHandle fd = Interop.Sys.ShmOpen(mapName, flags, (int)perms);
            if (fd.IsInvalid)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.ENOTSUP)
                {
                    // If ShmOpen is not supported, fall back to file backing object.
                    // Note that the System.Native shim will force this failure on platforms where
                    // the result of native shm_open does not work well with our subsequent call
                    // to mmap.
                    return null;
                }

                throw Interop.GetExceptionForIoErrno(errorInfo);
            }

            try
            {
                // Unlink the shared memory object immediately so that it'll go away once all handles 
                // to it are closed (as with opened then unlinked files, it'll remain usable via
                // the open handles even though it's unlinked and can't be opened anew via its name).
                Interop.CheckIo(Interop.Sys.ShmUnlink(mapName));

                // Give it the right capacity.  We do this directly with ftruncate rather
                // than via FileStream.SetLength after the FileStream is created because, on some systems,
                // lseek fails on shared memory objects, causing the FileStream to think it's unseekable,
                // causing it to preemptively throw from SetLength.
                Interop.CheckIo(Interop.Sys.FTruncate(fd, capacity));

                // Wrap the file descriptor in a stream and return it.
                return new FileStream(fd, TranslateProtectionsToFileAccess(protections));
            }
            catch
            {
                fd.Dispose();
                throw;
            }
        }

        private static FileStream CreateSharedBackingObjectUsingFile(Interop.Sys.MemoryMappedProtections protections, long capacity)
        {
            // We create a temporary backing file in TMPDIR.  We don't bother putting it into subdirectories as the file exists
            // extremely briefly: it's opened/created and then immediately unlinked.
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            FileAccess access =
                (protections & (Interop.Sys.MemoryMappedProtections.PROT_READ | Interop.Sys.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.ReadWrite :
                (protections & (Interop.Sys.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.Write :
                FileAccess.Read;

            // Create the backing file, then immediately unlink it so that it'll be cleaned up when no longer in use.
            // Then enlarge it to the requested capacity.
            const int DefaultBufferSize = 0x1000;
            var fs = new FileStream(path, FileMode.CreateNew, TranslateProtectionsToFileAccess(protections), FileShare.ReadWrite, DefaultBufferSize);
            try
            {
                Interop.CheckIo(Interop.Sys.Unlink(path));
                fs.SetLength(capacity);
            }
            catch
            {
                fs.Dispose();
                throw;
            }
            return fs;
        }
    }
}
