// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.MemoryMappedFiles
{
    public static class MemoryMappedFileAcl
    {
        public static MemoryMappedFile CreateFromFile(
            FileStream fileStream,
            string mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability,
            bool leaveOpen)
        {
            return MemoryMappedFile.CreateFromFile(fileStream, mapName, capacity, access, memoryMappedFileSecurity, inheritability, leaveOpen);
        }

        public static MemoryMappedFile CreateNew(
            string mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability)
        {
            return MemoryMappedFile.CreateNew(mapName, capacity, access, options, memoryMappedFileSecurity, inheritability);
        }

        public static MemoryMappedFile CreateOrOpen(
            string mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability)
        {
            return MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, memoryMappedFileSecurity, inheritability);
        }

        public static MemoryMappedFileSecurity GetAccessControl(this MemoryMappedFile memoryMappedFile)
        {
            return memoryMappedFile.GetAccessControl();
        }

        public static void SetAccessControl(this MemoryMappedFile memoryMappedFile, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            memoryMappedFile.SetAccessControl(memoryMappedFileSecurity);
        }
    }
}
