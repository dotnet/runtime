// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        private static FileStream CreateSharedBackingObject(
            Interop.libc.MemoryMappedProtections protections, long capacity,
            out string mapName, out SafeMemoryMappedFileHandle.FileStreamSource fileHandleSource)
        {
            mapName = MemoryMapObjectFilePrefix + Guid.NewGuid().ToString("N") + ".tmp";
            fileHandleSource = SafeMemoryMappedFileHandle.FileStreamSource.ManufacturedFile;

            FileAccess access =
                (protections & (Interop.libc.MemoryMappedProtections.PROT_READ | Interop.libc.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.ReadWrite :
                (protections & (Interop.libc.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.Write :
                FileAccess.Read;

            var fs = new FileStream(Path.Combine(Path.GetTempPath(), mapName),
                FileMode.CreateNew, TranslateProtectionsToFileAccess(protections), FileShare.ReadWrite,
                0x1000, FileOptions.DeleteOnClose);
            fs.SetLength(capacity);
            return fs;
        }
    }
}
