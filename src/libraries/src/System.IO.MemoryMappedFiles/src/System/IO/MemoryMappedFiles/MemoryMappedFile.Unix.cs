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
        /// memory mapped file should not be associated with an exsiting file on disk (ie start
        /// out empty).
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle CreateCore(
            SafeFileHandle fileHandle, String mapName, HandleInheritability inheritability,
            MemoryMappedFileAccess access, MemoryMappedFileOptions options, Int64 capacity)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle OpenCore(
            String mapName, HandleInheritability inheritability, MemoryMappedFileAccess access, bool createOrOpen)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle OpenCore(
            String mapName, HandleInheritability inheritability, MemoryMappedFileRights rights, bool createOrOpen)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>
        /// Used by the CreateOrOpen factory method groups.
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle CreateOrOpenCore(
            String mapName, HandleInheritability inheritability, MemoryMappedFileAccess access, 
            MemoryMappedFileOptions options, Int64 capacity)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
