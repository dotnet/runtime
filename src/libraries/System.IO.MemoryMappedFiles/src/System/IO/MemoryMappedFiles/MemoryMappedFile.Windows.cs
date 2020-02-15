// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile
    {
        /// <summary>
        /// Used by the 2 Create factory method groups.  A null fileHandle specifies that the
        /// memory mapped file should not be associated with an existing file on disk (i.e. start
        /// out empty).
        /// </summary>
        private static SafeMemoryMappedFileHandle CreateCore(
            FileStream? fileStream,
            string? mapName,
            HandleInheritability inheritability,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            long capacity)
        {
            return MemoryMappedFileInternal.CreateCore(fileStream, mapName, inheritability, access, options, capacity);
        }

        /// <summary>
        /// Used by the CreateOrOpen factory method groups.
        /// </summary>
        private static SafeMemoryMappedFileHandle CreateOrOpenCore(
            string mapName,
            HandleInheritability inheritability,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            long capacity)
        {
            return MemoryMappedFileInternal.CreateOrOpenCore(mapName, inheritability, access, options, capacity);
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        private static SafeMemoryMappedFileHandle OpenCore(
            string mapName,
            HandleInheritability inheritability,
            MemoryMappedFileAccess access,
            bool createOrOpen)
        {
            return MemoryMappedFileInternal.OpenCore(mapName, inheritability, MemoryMappedFileInternal.GetFileMapAccess(access), createOrOpen);
        }

        /// <summary>
        /// Used by the OpenExisting factory method group and by CreateOrOpen if access is write.
        /// We'll throw an ArgumentException if the file mapping object didn't exist and the
        /// caller used CreateOrOpen since Create isn't valid with Write access
        /// </summary>
        private static SafeMemoryMappedFileHandle OpenCore(
            string mapName,
            HandleInheritability inheritability,
            MemoryMappedFileRights rights,
            bool createOrOpen)
        {
            return MemoryMappedFileInternal.OpenCore(mapName, inheritability, MemoryMappedFileInternal.GetFileMapAccess(rights), createOrOpen);
        }
    }
}
