// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.MemoryMappedFiles
{
    public class MemoryMappedFileSecurity : System.Security.AccessControl.ObjectSecurity<System.IO.MemoryMappedFiles.MemoryMappedFileRights>
    {
        public MemoryMappedFileSecurity() : base(default(bool), default(System.Security.AccessControl.ResourceType)) { }
        internal MemoryMappedFileSecurity(Microsoft.Win32.SafeHandles.SafeMemoryMappedFileHandle safeHandle, System.Security.AccessControl.AccessControlSections includeSections) : base(default(bool), default(System.Security.AccessControl.ResourceType), safeHandle, includeSections) { }
        internal void PersistHandle(System.Runtime.InteropServices.SafeHandle handle) { }
    }
    public static class MemoryMappedFileAcl
    {
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(System.IO.FileStream fileStream, string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity, System.IO.HandleInheritability inheritability, bool leaveOpen) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateNew(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileOptions options, System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity, System.IO.HandleInheritability inheritability) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateOrOpen(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileOptions options, System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity, System.IO.HandleInheritability inheritability) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFileSecurity GetAccessControl(this System.IO.MemoryMappedFiles.MemoryMappedFile memoryMappedFile) { throw null; }
        public static void SetAccessControl(this System.IO.MemoryMappedFiles.MemoryMappedFile memoryMappedFile, System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity) { }
    }
}
