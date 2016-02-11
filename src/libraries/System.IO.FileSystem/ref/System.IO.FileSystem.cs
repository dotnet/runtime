// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class SafeFileHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeFileHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base(default(System.IntPtr), default(bool)) { }
        [System.Security.SecurityCriticalAttribute]
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
namespace System.IO
{
    public static partial class Directory
    {
        public static System.IO.DirectoryInfo CreateDirectory(string path) { return default(System.IO.DirectoryInfo); }
        public static void Delete(string path) { }
        public static void Delete(string path, bool recursive) { }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path, string searchPattern) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path, string searchPattern, System.IO.SearchOption searchOption) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path, string searchPattern) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.SearchOption searchOption) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, System.IO.SearchOption searchOption) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static bool Exists(string path) { return default(bool); }
        public static System.DateTime GetCreationTime(string path) { return default(System.DateTime); }
        public static System.DateTime GetCreationTimeUtc(string path) { return default(System.DateTime); }
        public static string GetCurrentDirectory() { return default(string); }
        public static string[] GetDirectories(string path) { return default(string[]); }
        public static string[] GetDirectories(string path, string searchPattern) { return default(string[]); }
        public static string[] GetDirectories(string path, string searchPattern, System.IO.SearchOption searchOption) { return default(string[]); }
        public static string GetDirectoryRoot(string path) { return default(string); }
        public static string[] GetFiles(string path) { return default(string[]); }
        public static string[] GetFiles(string path, string searchPattern) { return default(string[]); }
        public static string[] GetFiles(string path, string searchPattern, System.IO.SearchOption searchOption) { return default(string[]); }
        public static string[] GetFileSystemEntries(string path) { return default(string[]); }
        public static string[] GetFileSystemEntries(string path, string searchPattern) { return default(string[]); }
        public static string[] GetFileSystemEntries(string path, string searchPattern, System.IO.SearchOption searchOption) { return default(string[]); }
        public static System.DateTime GetLastAccessTime(string path) { return default(System.DateTime); }
        public static System.DateTime GetLastAccessTimeUtc(string path) { return default(System.DateTime); }
        public static System.DateTime GetLastWriteTime(string path) { return default(System.DateTime); }
        public static System.DateTime GetLastWriteTimeUtc(string path) { return default(System.DateTime); }
        public static System.IO.DirectoryInfo GetParent(string path) { return default(System.IO.DirectoryInfo); }
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
        public override bool Exists { get { return default(bool); } }
        public override string Name { get { return default(string); } }
        public System.IO.DirectoryInfo Parent { get { return default(System.IO.DirectoryInfo); } }
        public System.IO.DirectoryInfo Root { get { return default(System.IO.DirectoryInfo); } }
        public void Create() { }
        public System.IO.DirectoryInfo CreateSubdirectory(string path) { return default(System.IO.DirectoryInfo); }
        public override void Delete() { }
        public void Delete(bool recursive) { }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories() { return default(System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(string searchPattern) { return default(System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(string searchPattern, System.IO.SearchOption searchOption) { return default(System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles() { return default(System.Collections.Generic.IEnumerable<System.IO.FileInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles(string searchPattern) { return default(System.Collections.Generic.IEnumerable<System.IO.FileInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles(string searchPattern, System.IO.SearchOption searchOption) { return default(System.Collections.Generic.IEnumerable<System.IO.FileInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos() { return default(System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos(string searchPattern) { return default(System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo>); }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, System.IO.SearchOption searchOption) { return default(System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo>); }
        public System.IO.DirectoryInfo[] GetDirectories() { return default(System.IO.DirectoryInfo[]); }
        public System.IO.DirectoryInfo[] GetDirectories(string searchPattern) { return default(System.IO.DirectoryInfo[]); }
        public System.IO.DirectoryInfo[] GetDirectories(string searchPattern, System.IO.SearchOption searchOption) { return default(System.IO.DirectoryInfo[]); }
        public System.IO.FileInfo[] GetFiles() { return default(System.IO.FileInfo[]); }
        public System.IO.FileInfo[] GetFiles(string searchPattern) { return default(System.IO.FileInfo[]); }
        public System.IO.FileInfo[] GetFiles(string searchPattern, System.IO.SearchOption searchOption) { return default(System.IO.FileInfo[]); }
        public System.IO.FileSystemInfo[] GetFileSystemInfos() { return default(System.IO.FileSystemInfo[]); }
        public System.IO.FileSystemInfo[] GetFileSystemInfos(string searchPattern) { return default(System.IO.FileSystemInfo[]); }
        public System.IO.FileSystemInfo[] GetFileSystemInfos(string searchPattern, System.IO.SearchOption searchOption) { return default(System.IO.FileSystemInfo[]); }
        public void MoveTo(string destDirName) { }
        public override string ToString() { return default(string); }
    }
    public static partial class File
    {
        public static void AppendAllLines(string path, System.Collections.Generic.IEnumerable<string> contents) { }
        public static void AppendAllLines(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding) { }
        public static void AppendAllText(string path, string contents) { }
        public static void AppendAllText(string path, string contents, System.Text.Encoding encoding) { }
        public static System.IO.StreamWriter AppendText(string path) { return default(System.IO.StreamWriter); }
        public static void Copy(string sourceFileName, string destFileName) { }
        public static void Copy(string sourceFileName, string destFileName, bool overwrite) { }
        public static System.IO.FileStream Create(string path) { return default(System.IO.FileStream); }
        public static System.IO.FileStream Create(string path, int bufferSize) { return default(System.IO.FileStream); }
        public static System.IO.FileStream Create(string path, int bufferSize, System.IO.FileOptions options) { return default(System.IO.FileStream); }
        public static System.IO.StreamWriter CreateText(string path) { return default(System.IO.StreamWriter); }
        public static void Delete(string path) { }
        public static bool Exists(string path) { return default(bool); }
        public static System.IO.FileAttributes GetAttributes(string path) { return default(System.IO.FileAttributes); }
        public static System.DateTime GetCreationTime(string path) { return default(System.DateTime); }
        public static System.DateTime GetCreationTimeUtc(string path) { return default(System.DateTime); }
        public static System.DateTime GetLastAccessTime(string path) { return default(System.DateTime); }
        public static System.DateTime GetLastAccessTimeUtc(string path) { return default(System.DateTime); }
        public static System.DateTime GetLastWriteTime(string path) { return default(System.DateTime); }
        public static System.DateTime GetLastWriteTimeUtc(string path) { return default(System.DateTime); }
        public static void Move(string sourceFileName, string destFileName) { }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode) { return default(System.IO.FileStream); }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access) { return default(System.IO.FileStream); }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { return default(System.IO.FileStream); }
        public static System.IO.FileStream OpenRead(string path) { return default(System.IO.FileStream); }
        public static System.IO.StreamReader OpenText(string path) { return default(System.IO.StreamReader); }
        public static System.IO.FileStream OpenWrite(string path) { return default(System.IO.FileStream); }
        public static byte[] ReadAllBytes(string path) { return default(byte[]); }
        public static string[] ReadAllLines(string path) { return default(string[]); }
        public static string[] ReadAllLines(string path, System.Text.Encoding encoding) { return default(string[]); }
        public static string ReadAllText(string path) { return default(string); }
        public static string ReadAllText(string path, System.Text.Encoding encoding) { return default(string); }
        public static System.Collections.Generic.IEnumerable<string> ReadLines(string path) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static System.Collections.Generic.IEnumerable<string> ReadLines(string path, System.Text.Encoding encoding) { return default(System.Collections.Generic.IEnumerable<string>); }
        public static void SetAttributes(string path, System.IO.FileAttributes fileAttributes) { }
        public static void SetCreationTime(string path, System.DateTime creationTime) { }
        public static void SetCreationTimeUtc(string path, System.DateTime creationTimeUtc) { }
        public static void SetLastAccessTime(string path, System.DateTime lastAccessTime) { }
        public static void SetLastAccessTimeUtc(string path, System.DateTime lastAccessTimeUtc) { }
        public static void SetLastWriteTime(string path, System.DateTime lastWriteTime) { }
        public static void SetLastWriteTimeUtc(string path, System.DateTime lastWriteTimeUtc) { }
        public static void WriteAllBytes(string path, byte[] bytes) { }
        public static void WriteAllLines(string path, System.Collections.Generic.IEnumerable<string> contents) { }
        public static void WriteAllLines(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding) { }
        public static void WriteAllText(string path, string contents) { }
        public static void WriteAllText(string path, string contents, System.Text.Encoding encoding) { }
    }
    public sealed partial class FileInfo : System.IO.FileSystemInfo
    {
        public FileInfo(string fileName) { }
        public System.IO.DirectoryInfo Directory { get { return default(System.IO.DirectoryInfo); } }
        public string DirectoryName { get { return default(string); } }
        public override bool Exists { get { return default(bool); } }
        public bool IsReadOnly { get { return default(bool); } set { } }
        public long Length { get { return default(long); } }
        public override string Name { get { return default(string); } }
        public System.IO.StreamWriter AppendText() { return default(System.IO.StreamWriter); }
        public System.IO.FileInfo CopyTo(string destFileName) { return default(System.IO.FileInfo); }
        public System.IO.FileInfo CopyTo(string destFileName, bool overwrite) { return default(System.IO.FileInfo); }
        public System.IO.FileStream Create() { return default(System.IO.FileStream); }
        public System.IO.StreamWriter CreateText() { return default(System.IO.StreamWriter); }
        public override void Delete() { }
        public void MoveTo(string destFileName) { }
        public System.IO.FileStream Open(System.IO.FileMode mode) { return default(System.IO.FileStream); }
        public System.IO.FileStream Open(System.IO.FileMode mode, System.IO.FileAccess access) { return default(System.IO.FileStream); }
        public System.IO.FileStream Open(System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { return default(System.IO.FileStream); }
        public System.IO.FileStream OpenRead() { return default(System.IO.FileStream); }
        public System.IO.StreamReader OpenText() { return default(System.IO.StreamReader); }
        public System.IO.FileStream OpenWrite() { return default(System.IO.FileStream); }
        public override string ToString() { return default(string); }
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
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public virtual bool IsAsync { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public string Name { get { return default(string); } }
        public override long Position { get { return default(long); } set { } }
        public virtual Microsoft.Win32.SafeHandles.SafeFileHandle SafeFileHandle { get { return default(Microsoft.Win32.SafeHandles.SafeFileHandle); } }
        protected override void Dispose(bool disposing) { }
        ~FileStream() { }
        public override void Flush() { }
        public virtual void Flush(bool flushToDisk) { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override int Read(byte[] array, int offset, int count) { array = default(byte[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override int ReadByte() { return default(int); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override void WriteByte(byte value) { }
    }
    public abstract partial class FileSystemInfo
    {
        protected string FullPath;
        protected string OriginalPath;
        protected FileSystemInfo() { }
        public System.IO.FileAttributes Attributes { get { return default(System.IO.FileAttributes); } set { } }
        public System.DateTime CreationTime { get { return default(System.DateTime); } set { } }
        public System.DateTime CreationTimeUtc { get { return default(System.DateTime); } set { } }
        public abstract bool Exists { get; }
        public string Extension { get { return default(string); } }
        public virtual string FullName { get { return default(string); } }
        public System.DateTime LastAccessTime { get { return default(System.DateTime); } set { } }
        public System.DateTime LastAccessTimeUtc { get { return default(System.DateTime); } set { } }
        public System.DateTime LastWriteTime { get { return default(System.DateTime); } set { } }
        public System.DateTime LastWriteTimeUtc { get { return default(System.DateTime); } set { } }
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
