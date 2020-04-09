// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.MemoryMappedFiles
{
    public static class MemoryMappedFileAcl
    {
        public static unsafe MemoryMappedFile CreateFromFile(
            FileStream fileStream,
            string? mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability,
            bool leaveOpen) => throw new PlatformNotSupportedException();

        public static unsafe MemoryMappedFile CreateNew(
            string mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability) => throw new PlatformNotSupportedException();

        public static unsafe MemoryMappedFile CreateOrOpen(
            string mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability)
        {
            throw new PlatformNotSupportedException();
        }

        public static MemoryMappedFileSecurity GetAccessControl(this MemoryMappedFile memoryMappedFile) => throw new PlatformNotSupportedException();

        public static void SetAccessControl(this MemoryMappedFile memoryMappedFile, MemoryMappedFileSecurity memoryMappedFileSecurity) => throw new PlatformNotSupportedException();
    }
}
