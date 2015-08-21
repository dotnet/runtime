// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        private static FileStream CreateSharedBackingObject(Interop.libc.MemoryMappedProtections protections, long capacity)
        {
            Directory.CreateDirectory(s_tempMapsDirectory);
            string path = Path.Combine(s_tempMapsDirectory, Guid.NewGuid().ToString("N"));
            
            FileAccess access =
                (protections & (Interop.libc.MemoryMappedProtections.PROT_READ | Interop.libc.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.ReadWrite :
                (protections & (Interop.libc.MemoryMappedProtections.PROT_WRITE)) != 0 ? FileAccess.Write :
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

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static readonly string s_tempMapsDirectory = PersistedFiles.GetTempFeatureDirectory("maps");
    }
}
