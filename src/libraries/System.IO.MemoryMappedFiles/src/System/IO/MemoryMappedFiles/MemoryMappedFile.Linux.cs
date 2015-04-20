// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        private static FileStream CreateSharedBackingObject(
            Interop.libc.MemoryMappedProtections protections, long capacity,
            out string mapName, out SafeMemoryMappedFileHandle.FileStreamSource fileStreamSource)
        {
            // The POSIX shared memory object name must begin with '/'.  After that we just want something short and unique.
            mapName = "/" + MemoryMapObjectFilePrefix + Guid.NewGuid().ToString("N");
            fileStreamSource = SafeMemoryMappedFileHandle.FileStreamSource.ManufacturedSharedMemory;

            // Determine the flags to use when creating the shared memory object
            Interop.libc.OpenFlags flags = (protections & Interop.libc.MemoryMappedProtections.PROT_WRITE) != 0 ?
                Interop.libc.OpenFlags.O_RDWR :
                Interop.libc.OpenFlags.O_RDONLY;
            flags |= Interop.libc.OpenFlags.O_CREAT | Interop.libc.OpenFlags.O_EXCL; // CreateNew

            // Determine the permissions with which to create the file
            Interop.libc.Permissions perms = default(Interop.libc.Permissions);
            if ((protections & Interop.libc.MemoryMappedProtections.PROT_READ) != 0)
                perms |= Interop.libc.Permissions.S_IRUSR;
            if ((protections & Interop.libc.MemoryMappedProtections.PROT_WRITE) != 0)
                perms |= Interop.libc.Permissions.S_IWUSR;
            if ((protections & Interop.libc.MemoryMappedProtections.PROT_EXEC) != 0)
                perms |= Interop.libc.Permissions.S_IXUSR;

            // Create the shared memory object. Then enlarge it to the requested capacity.
            int fd;
            Interop.CheckIo(fd = Interop.libc.shm_open(mapName, flags, (int)perms), mapName);
            SafeFileHandle fileHandle = new SafeFileHandle((IntPtr)fd, ownsHandle: true);

            // Wrap the handle in a stream and return it.
            var fs = new FileStream(fileHandle, TranslateProtectionsToFileAccess(protections));
            fs.SetLength(capacity);
            return fs;
        }
    }
}
