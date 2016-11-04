// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeMemoryMappedFileHandle() : base(default(bool)) { }
        public override bool IsInvalid { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        protected override bool ReleaseHandle() { throw null; }
    }
    public sealed partial class SafeMemoryMappedViewHandle : System.Runtime.InteropServices.SafeBuffer
    {
        internal SafeMemoryMappedViewHandle() : base(default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile : System.IDisposable
    {
        internal MemoryMappedFile() { }
        public Microsoft.Win32.SafeHandles.SafeMemoryMappedFileHandle SafeMemoryMappedFileHandle { get { throw null; } }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(System.IO.FileStream fileStream, string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.HandleInheritability inheritability, bool leaveOpen) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode, string mapName) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode, string mapName, long capacity) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode, string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateNew(string mapName, long capacity) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateNew(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateNew(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileOptions options, System.IO.HandleInheritability inheritability) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateOrOpen(string mapName, long capacity) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateOrOpen(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateOrOpen(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileOptions options, System.IO.HandleInheritability inheritability) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewAccessor CreateViewAccessor() { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewAccessor CreateViewAccessor(long offset, long size) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewAccessor CreateViewAccessor(long offset, long size, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewStream CreateViewStream() { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewStream CreateViewStream(long offset, long size) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewStream CreateViewStream(long offset, long size, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile OpenExisting(string mapName) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile OpenExisting(string mapName, System.IO.MemoryMappedFiles.MemoryMappedFileRights desiredAccessRights) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile OpenExisting(string mapName, System.IO.MemoryMappedFiles.MemoryMappedFileRights desiredAccessRights, System.IO.HandleInheritability inheritability) { throw null; }
    }
    public enum MemoryMappedFileAccess
    {
        CopyOnWrite = 3,
        Read = 1,
        ReadExecute = 4,
        ReadWrite = 0,
        ReadWriteExecute = 5,
        Write = 2,
    }
    [System.FlagsAttribute]
    public enum MemoryMappedFileOptions
    {
        DelayAllocatePages = 67108864,
        None = 0,
    }
    [System.FlagsAttribute]
    public enum MemoryMappedFileRights
    {
        AccessSystemSecurity = 16777216,
        ChangePermissions = 262144,
        CopyOnWrite = 1,
        Delete = 65536,
        Execute = 8,
        FullControl = 983055,
        Read = 4,
        ReadExecute = 12,
        ReadPermissions = 131072,
        ReadWrite = 6,
        ReadWriteExecute = 14,
        TakeOwnership = 524288,
        Write = 2,
    }
    public sealed partial class MemoryMappedViewAccessor : System.IO.UnmanagedMemoryAccessor
    {
        internal MemoryMappedViewAccessor() { }
        public long PointerOffset { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public void Flush() { }
    }
    public sealed partial class MemoryMappedViewStream : System.IO.UnmanagedMemoryStream
    {
        internal MemoryMappedViewStream() { }
        public long PointerOffset { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override void SetLength(long value) { }
    }
}
