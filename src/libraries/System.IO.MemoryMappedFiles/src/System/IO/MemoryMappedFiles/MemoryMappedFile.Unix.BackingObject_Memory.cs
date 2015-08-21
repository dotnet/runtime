// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        private static FileStream CreateSharedBackingObject(
            Interop.libc.MemoryMappedProtections protections, long capacity)
        {
            // The POSIX shared memory object name must begin with '/'.  After that we just want something short and unique.
            string mapName = "/corefx_map_" + Guid.NewGuid().ToString("N");

            // Determine the flags to use when creating the shared memory object
            Interop.Sys.OpenFlags flags = (protections & Interop.libc.MemoryMappedProtections.PROT_WRITE) != 0 ?
                Interop.Sys.OpenFlags.O_RDWR :
                Interop.Sys.OpenFlags.O_RDONLY;
            flags |= Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL; // CreateNew

            // Determine the permissions with which to create the file
            Interop.libc.Permissions perms = default(Interop.libc.Permissions);
            if ((protections & Interop.libc.MemoryMappedProtections.PROT_READ) != 0)
                perms |= Interop.libc.Permissions.S_IRUSR;
            if ((protections & Interop.libc.MemoryMappedProtections.PROT_WRITE) != 0)
                perms |= Interop.libc.Permissions.S_IWUSR;
            if ((protections & Interop.libc.MemoryMappedProtections.PROT_EXEC) != 0)
                perms |= Interop.libc.Permissions.S_IXUSR;

            // Create the shared memory object.
            int fd;
            Interop.CheckIo(fd = Interop.Sys.ShmOpen(mapName, flags, (int)perms), mapName);
            SafeFileHandle fileHandle = new SafeFileHandle((IntPtr)fd, ownsHandle: true);
            try
            {
                // Unlink the shared memory object immediatley so that it'll go away once all handles 
                // to it are closed (as with opened then unlinked files, it'll remain usable via
                // the open handles even though it's unlinked and can't be opened anew via its name).
                Interop.CheckIo(Interop.Sys.ShmUnlink(mapName));

                // Give it the right capacity.  We do this directly with ftruncate rather
                // than via FileStream.SetLength after the FileStream is created because, on some systems,
                // lseek fails on shared memory objects, causing the FileStream to think it's unseekable,
                // causing it to preemptively throw from SetLength.
                Interop.CheckIo(Interop.libc.ftruncate(fd, capacity));

                // Wrap the file descriptor in a stream and return it.
                return new FileStream(fileHandle, TranslateProtectionsToFileAccess(protections));
            }
            catch
            {
                fileHandle.Dispose();
                throw;
            }
        }
    }
}
