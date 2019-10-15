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
        internal SafeMemoryMappedFileHandle() : base (default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
        protected override bool ReleaseHandle() { throw null; }
    }
    public sealed partial class SafeMemoryMappedViewHandle : System.Runtime.InteropServices.SafeBuffer
    {
        internal SafeMemoryMappedViewHandle() : base (default(bool)) { }
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
        ReadWrite = 0,
        Read = 1,
        Write = 2,
        CopyOnWrite = 3,
        ReadExecute = 4,
        ReadWriteExecute = 5,
    }
    [System.FlagsAttribute]
    public enum MemoryMappedFileOptions
    {
        None = 0,
        DelayAllocatePages = 67108864,
    }
    [System.FlagsAttribute]
    public enum MemoryMappedFileRights
    {
        CopyOnWrite = 1,
        Write = 2,
        Read = 4,
        ReadWrite = 6,
        Execute = 8,
        ReadExecute = 12,
        ReadWriteExecute = 14,
        Delete = 65536,
        ReadPermissions = 131072,
        ChangePermissions = 262144,
        TakeOwnership = 524288,
        FullControl = 983055,
        AccessSystemSecurity = 16777216,
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
