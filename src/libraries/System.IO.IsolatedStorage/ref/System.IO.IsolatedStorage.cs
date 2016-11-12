// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.IsolatedStorage
{
    public partial interface INormalizeForIsolatedStorage
    {
        object Normalize();
    }

    public abstract partial class IsolatedStorage : System.MarshalByRefObject
    {
        protected IsolatedStorage() { }
        public object ApplicationIdentity { get { throw null; } }
        public object AssemblyIdentity { get { throw null; } }
        public virtual long AvailableFreeSpace { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("IsolatedStorage.CurrentSize has been deprecated because it is not CLS Compliant.  To get the current size use IsolatedStorage.UsedSize")]
        public virtual ulong CurrentSize { get { throw null; } }
        public object DomainIdentity { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("IsolatedStorage.MaximumSize has been deprecated because it is not CLS Compliant.  To get the maximum size use IsolatedStorage.Quota")]
        public virtual ulong MaximumSize { get { throw null; } }
        public virtual long Quota { get { throw null; } }
        public System.IO.IsolatedStorage.IsolatedStorageScope Scope { get { throw null; } }
        protected virtual char SeparatorExternal { get { throw null; } }
        protected virtual char SeparatorInternal { get { throw null; } }
        public virtual long UsedSize { get { throw null; } }
        public virtual bool IncreaseQuotaTo(long newQuotaSize) { throw null; }
        
        protected void InitStore(System.IO.IsolatedStorage.IsolatedStorageScope scope, System.Type appEvidenceType) { }
        
        protected void InitStore(System.IO.IsolatedStorage.IsolatedStorageScope scope, System.Type domainEvidenceType, System.Type assemblyEvidenceType) { }
        public abstract void Remove();
    }

    public partial class IsolatedStorageException : System.Exception
    {
        public IsolatedStorageException() { }
        public IsolatedStorageException(string message) { }
        public IsolatedStorageException(string message, System.Exception inner) { }
        protected IsolatedStorageException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public sealed partial class IsolatedStorageFile : System.IO.IsolatedStorage.IsolatedStorage, System.IDisposable
    {
        internal IsolatedStorageFile() { }
        public override long AvailableFreeSpace { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("IsolatedStorageFile.CurrentSize has been deprecated because it is not CLS Compliant.  To get the current size use IsolatedStorageFile.UsedSize")]
        public override ulong CurrentSize { get { throw null; } }
        public static bool IsEnabled { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("IsolatedStorageFile.MaximumSize has been deprecated because it is not CLS Compliant.  To get the maximum size use IsolatedStorageFile.Quota")]
        public override ulong MaximumSize { get { throw null; } }
        public override long Quota { get { throw null; } }
        public override long UsedSize { get { throw null; } }
        public void Close() { }
        public void CopyFile(string sourceFileName, string destinationFileName) { }
        public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite) { }
        public void CreateDirectory(string dir) { }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream CreateFile(string path) { throw null; }
        public void DeleteDirectory(string dir) { }
        public void DeleteFile(string file) { }
        public bool DirectoryExists(string path) { throw null; }
        public void Dispose() { }
        public bool FileExists(string path) { throw null; }
        ~IsolatedStorageFile() { }
        public System.DateTimeOffset GetCreationTime(string path) { throw null; }
        public string[] GetDirectoryNames() { throw null; }
        public string[] GetDirectoryNames(string searchPattern) { throw null; }
        public static System.Collections.IEnumerator GetEnumerator(System.IO.IsolatedStorage.IsolatedStorageScope scope) { throw null; }
        public string[] GetFileNames() { throw null; }
        public string[] GetFileNames(string searchPattern) { throw null; }
        public System.DateTimeOffset GetLastAccessTime(string path) { throw null; }
        public System.DateTimeOffset GetLastWriteTime(string path) { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetMachineStoreForApplication() { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetMachineStoreForAssembly() { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetMachineStoreForDomain() { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetStore(System.IO.IsolatedStorage.IsolatedStorageScope scope, object applicationIdentity) { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetStore(System.IO.IsolatedStorage.IsolatedStorageScope scope, object domainIdentity, object assemblyIdentity) { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetStore(System.IO.IsolatedStorage.IsolatedStorageScope scope, System.Type applicationEvidenceType) { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetStore(System.IO.IsolatedStorage.IsolatedStorageScope scope, System.Type domainEvidenceType, System.Type assemblyEvidenceType) { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetUserStoreForApplication() { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetUserStoreForAssembly() { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetUserStoreForDomain() { throw null; }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetUserStoreForSite() { throw null; }
        public override bool IncreaseQuotaTo(long newQuotaSize) { throw null; }
        public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName) { }
        public void MoveFile(string sourceFileName, string destinationFileName) { }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream OpenFile(string path, System.IO.FileMode mode) { throw null; }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access) { throw null; }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { throw null; }
        public override void Remove() { }
        public static void Remove(System.IO.IsolatedStorage.IsolatedStorageScope scope) { }
    }

    public partial class IsolatedStorageFileStream : System.IO.FileStream
    {
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode) : base(default(string), default(System.IO.FileMode)) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access) : base(default(string), default(System.IO.FileMode)) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) : base(default(string), default(System.IO.FileMode)) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize) : base(default(string), default(System.IO.FileMode)) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize, System.IO.IsolatedStorage.IsolatedStorageFile isf) : base(default(string), default(System.IO.FileMode)) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, System.IO.IsolatedStorage.IsolatedStorageFile isf) : base(default(string), default(System.IO.FileMode)) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.IsolatedStorage.IsolatedStorageFile isf) : base(default(string), default(System.IO.FileMode)) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.IsolatedStorage.IsolatedStorageFile isf) : base(default(string), default(System.IO.FileMode)) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated.  Please use IsolatedStorageFileStream's SafeFileHandle property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public override System.IntPtr Handle { get { throw null; } }
        public override bool IsAsync { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override Microsoft.Win32.SafeHandles.SafeFileHandle SafeFileHandle { get { throw null; } }

        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int numBytes, System.AsyncCallback userCallback, object stateObject) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int numBytes, System.AsyncCallback userCallback, object stateObject) { throw null; }

        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override void Flush(bool flushToDisk) { }
        public override void Lock(long position, long length) { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Unlock(long position, long length) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void WriteByte(byte value) { }
    }

    [System.FlagsAttribute]
    public enum IsolatedStorageScope
    {
        Application = 32,
        Assembly = 4,
        Domain = 2,
        Machine = 16,
        None = 0,
        Roaming = 8,
        User = 1,
    }
}


