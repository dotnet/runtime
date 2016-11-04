// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class SafeFileHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeFileHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base(default(bool)) { }
        public override bool IsInvalid { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.IO
{
    public static partial class Directory
    {
        public static System.IO.DirectoryInfo CreateDirectory(string path) { throw null; }
        public static void Delete(string path) { }
        public static void Delete(string path, bool recursive) { }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path, string searchPattern) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path, string searchPattern) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static bool Exists(string path) { throw null; }
        public static System.DateTime GetCreationTime(string path) { throw null; }
        public static System.DateTime GetCreationTimeUtc(string path) { throw null; }
        public static string GetCurrentDirectory() { throw null; }
        public static string[] GetDirectories(string path) { throw null; }
        public static string[] GetDirectories(string path, string searchPattern) { throw null; }
        public static string[] GetDirectories(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static string GetDirectoryRoot(string path) { throw null; }
        public static string[] GetFiles(string path) { throw null; }
        public static string[] GetFiles(string path, string searchPattern) { throw null; }
        public static string[] GetFiles(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static string[] GetFileSystemEntries(string path) { throw null; }
        public static string[] GetFileSystemEntries(string path, string searchPattern) { throw null; }
        public static string[] GetFileSystemEntries(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static System.DateTime GetLastAccessTime(string path) { throw null; }
        public static System.DateTime GetLastAccessTimeUtc(string path) { throw null; }
        public static System.DateTime GetLastWriteTime(string path) { throw null; }
        public static System.DateTime GetLastWriteTimeUtc(string path) { throw null; }
        public static string[] GetLogicalDrives() { throw null; }
        public static System.IO.DirectoryInfo GetParent(string path) { throw null; }
        public static void Move(string sourceDirName, string destDirName) { }
        public static void SetCreationTime(string path, System.DateTime creationTime) { }
        public static void SetCreationTimeUtc(string path, System.DateTime creationTimeUtc) { }
        public static void SetCurrentDirectory(string path) { }
        public static void SetLastAccessTime(string path, System.DateTime lastAccessTime) { }
        public static void SetLastAccessTimeUtc(string path, System.DateTime lastAccessTimeUtc) { }
        public static void SetLastWriteTime(string path, System.DateTime lastWriteTime) { }
        public static void SetLastWriteTimeUtc(string path, System.DateTime lastWriteTimeUtc) { }
    }
    public sealed partial class DirectoryInfo : System.IO.FileSystemInfo
    {
        public DirectoryInfo(string path) { }
        public override bool Exists { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.IO.DirectoryInfo Parent { get { throw null; } }
        public System.IO.DirectoryInfo Root { get { throw null; } }
        public void Create() { }
        public System.IO.DirectoryInfo CreateSubdirectory(string path) { throw null; }
        public override void Delete() { }
        public void Delete(bool recursive) { }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories() { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(string searchPattern) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles() { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles(string searchPattern) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos() { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos(string searchPattern) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.IO.DirectoryInfo[] GetDirectories() { throw null; }
        public System.IO.DirectoryInfo[] GetDirectories(string searchPattern) { throw null; }
        public System.IO.DirectoryInfo[] GetDirectories(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.IO.FileInfo[] GetFiles() { throw null; }
        public System.IO.FileInfo[] GetFiles(string searchPattern) { throw null; }
        public System.IO.FileInfo[] GetFiles(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.IO.FileSystemInfo[] GetFileSystemInfos() { throw null; }
        public System.IO.FileSystemInfo[] GetFileSystemInfos(string searchPattern) { throw null; }
        public System.IO.FileSystemInfo[] GetFileSystemInfos(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public void MoveTo(string destDirName) { }
        public override string ToString() { throw null; }
    }
    public static partial class File
    {
        public static void AppendAllLines(string path, System.Collections.Generic.IEnumerable<string> contents) { }
        public static void AppendAllLines(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding) { }
        public static void AppendAllText(string path, string contents) { }
        public static void AppendAllText(string path, string contents, System.Text.Encoding encoding) { }
        public static System.IO.StreamWriter AppendText(string path) { throw null; }
        public static void Copy(string sourceFileName, string destFileName) { }
        public static void Copy(string sourceFileName, string destFileName, bool overwrite) { }
        public static System.IO.FileStream Create(string path) { throw null; }
        public static System.IO.FileStream Create(string path, int bufferSize) { throw null; }
        public static System.IO.FileStream Create(string path, int bufferSize, System.IO.FileOptions options) { throw null; }
        public static System.IO.StreamWriter CreateText(string path) { throw null; }
        public static void Delete(string path) { }
        public static void Decrypt(string path) { }
        public static void Encrypt(string path) { }
        public static bool Exists(string path) { throw null; }
        public static System.IO.FileAttributes GetAttributes(string path) { throw null; }
        public static System.DateTime GetCreationTime(string path) { throw null; }
        public static System.DateTime GetCreationTimeUtc(string path) { throw null; }
        public static System.DateTime GetLastAccessTime(string path) { throw null; }
        public static System.DateTime GetLastAccessTimeUtc(string path) { throw null; }
        public static System.DateTime GetLastWriteTime(string path) { throw null; }
        public static System.DateTime GetLastWriteTimeUtc(string path) { throw null; }
        public static void Move(string sourceFileName, string destFileName) { }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode) { throw null; }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access) { throw null; }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { throw null; }
        public static System.IO.FileStream OpenRead(string path) { throw null; }
        public static System.IO.StreamReader OpenText(string path) { throw null; }
        public static System.IO.FileStream OpenWrite(string path) { throw null; }
        public static byte[] ReadAllBytes(string path) { throw null; }
        public static string[] ReadAllLines(string path) { throw null; }
        public static string[] ReadAllLines(string path, System.Text.Encoding encoding) { throw null; }
        public static string ReadAllText(string path) { throw null; }
        public static string ReadAllText(string path, System.Text.Encoding encoding) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> ReadLines(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> ReadLines(string path, System.Text.Encoding encoding) { throw null; }
        public static void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName) { }
        public static void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors) { }
        public static void SetAttributes(string path, System.IO.FileAttributes fileAttributes) { }
        public static void SetCreationTime(string path, System.DateTime creationTime) { }
        public static void SetCreationTimeUtc(string path, System.DateTime creationTimeUtc) { }
        public static void SetLastAccessTime(string path, System.DateTime lastAccessTime) { }
        public static void SetLastAccessTimeUtc(string path, System.DateTime lastAccessTimeUtc) { }
        public static void SetLastWriteTime(string path, System.DateTime lastWriteTime) { }
        public static void SetLastWriteTimeUtc(string path, System.DateTime lastWriteTimeUtc) { }
        public static void WriteAllBytes(string path, byte[] bytes) { }
        public static void WriteAllLines(string path, string[] contents) { }
        public static void WriteAllLines(string path, System.Collections.Generic.IEnumerable<string> contents) { }
        public static void WriteAllLines(string path, string[] contents, System.Text.Encoding encoding) { }
        public static void WriteAllLines(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding) { }
        public static void WriteAllText(string path, string contents) { }
        public static void WriteAllText(string path, string contents, System.Text.Encoding encoding) { }
    }
    public sealed partial class FileInfo : System.IO.FileSystemInfo
    {
        public FileInfo(string fileName) { }
        public System.IO.DirectoryInfo Directory { get { throw null; } }
        public string DirectoryName { get { throw null; } }
        public override bool Exists { get { throw null; } }
        public bool IsReadOnly { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.IO.StreamWriter AppendText() { throw null; }
        public System.IO.FileInfo CopyTo(string destFileName) { throw null; }
        public System.IO.FileInfo CopyTo(string destFileName, bool overwrite) { throw null; }
        public System.IO.FileStream Create() { throw null; }
        public System.IO.StreamWriter CreateText() { throw null; }
        public override void Delete() { }
        public void Decrypt() { }
        public void Encrypt() { }
        public void MoveTo(string destFileName) { }
        public System.IO.FileStream Open(System.IO.FileMode mode) { throw null; }
        public System.IO.FileStream Open(System.IO.FileMode mode, System.IO.FileAccess access) { throw null; }
        public System.IO.FileStream Open(System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { throw null; }
        public System.IO.FileStream OpenRead() { throw null; }
        public System.IO.StreamReader OpenText() { throw null; }
        public System.IO.FileStream OpenWrite() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum FileOptions
    {
        Asynchronous = 1073741824,
        DeleteOnClose = 67108864,
        Encrypted = 16384,
        None = 0,
        RandomAccess = 268435456,
        SequentialScan = 134217728,
        WriteThrough = -2147483648,
    }
    public partial class FileStream : System.IO.Stream
    {
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, System.IO.FileAccess access) { }
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, System.IO.FileAccess access, int bufferSize) { }
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, System.IO.FileAccess access, int bufferSize, bool isAsync) { }
        public FileStream(string path, System.IO.FileMode mode) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize, bool useAsync) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize, System.IO.FileOptions options) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }

        [Obsolete("This property has been deprecated.  Please use FileStream's SafeFileHandle property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual System.IntPtr Handle { get { throw null; } }
        public virtual bool IsAsync { get { throw null; } }
        public override long Length { get { throw null; } }
        public string Name { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public virtual Microsoft.Win32.SafeHandles.SafeFileHandle SafeFileHandle { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        ~FileStream() { }
        public override void Flush() { }
        public virtual void Flush(bool flushToDisk) { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] array, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { throw null; }
        public override int EndRead(IAsyncResult asyncResult) { throw null; }
        public virtual void Lock(long position, long length) { }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { throw null; }
        public override void EndWrite(IAsyncResult asyncResult) { }
        public override void WriteByte(byte value) { }
        public virtual void Unlock(long position, long length) { }
    }
    public abstract partial class FileSystemInfo : System.MarshalByRefObject, System.Runtime.Serialization.ISerializable
    {
        protected string FullPath;
        protected string OriginalPath;
        protected FileSystemInfo() { }
        protected FileSystemInfo(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.IO.FileAttributes Attributes { get { throw null; } set { } }
        public System.DateTime CreationTime { get { throw null; } set { } }
        public System.DateTime CreationTimeUtc { get { throw null; } set { } }
        public abstract bool Exists { get; }
        public string Extension { get { throw null; } }
        public virtual string FullName { get { throw null; } }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.DateTime LastAccessTime { get { throw null; } set { } }
        public System.DateTime LastAccessTimeUtc { get { throw null; } set { } }
        public System.DateTime LastWriteTime { get { throw null; } set { } }
        public System.DateTime LastWriteTimeUtc { get { throw null; } set { } }
        public abstract string Name { get; }
        public abstract void Delete();
        public void Refresh() { }
    }
    public enum SearchOption
    {
        AllDirectories = 1,
        TopDirectoryOnly = 0,
    }
}
