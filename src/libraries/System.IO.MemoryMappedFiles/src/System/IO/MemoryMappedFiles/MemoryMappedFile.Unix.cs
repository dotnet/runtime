// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        /// <summary>
        /// Used by the 2 Create factory method groups.  A -1 fileHandle specifies that the 
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
        /// Used by the CreateOrOpen factory method groups.  A -1 fileHandle specifies that the 
        /// memory mapped file should not be associated with an existing file on disk (ie start
        /// out empty).
        ///
        /// Try to open the file if it exists -- this requires a bit more work. Loop until we can
        /// either create or open a memory mapped file up to a timeout. CreateFileMapping may fail
        /// if the file exists and we have non-null security attributes, in which case we need to
        /// use OpenFileMapping.  But, there exists a race condition because the memory mapped file
        /// may have closed inbetween the two calls -- hence the loop. 
        /// 
        /// This uses similar retry/timeout logic as in performance counter. It increases the wait
        /// time each pass through the loop and times out in approximately 1.4 minutes. If after 
        /// retrying, a MMF handle still hasn't been opened, throw an InvalidOperationException.
        /// </summary>
        [SecurityCritical]
        private static SafeMemoryMappedFileHandle CreateOrOpenCore(
            SafeFileHandle fileHandle, String mapName, HandleInheritability inheritability, 
            MemoryMappedFileAccess access, MemoryMappedFileOptions options, Int64 capacity)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
