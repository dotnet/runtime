// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO
{
    public static partial class Directory
    {
        public static System.IO.DirectoryInfo CreateDirectory(string path) { throw null; }
        public static void Delete(string path) { }
        public static void Delete(string path, bool recursive) { }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path, string searchPattern) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path, string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateDirectories(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path, string searchPattern) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static bool Exists(string? path) { throw null; }
        public static System.DateTime GetCreationTime(string path) { throw null; }
        public static System.DateTime GetCreationTimeUtc(string path) { throw null; }
        public static string GetCurrentDirectory() { throw null; }
        public static string[] GetDirectories(string path) { throw null; }
        public static string[] GetDirectories(string path, string searchPattern) { throw null; }
        public static string[] GetDirectories(string path, string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public static string[] GetDirectories(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static string GetDirectoryRoot(string path) { throw null; }
        public static string[] GetFiles(string path) { throw null; }
        public static string[] GetFiles(string path, string searchPattern) { throw null; }
        public static string[] GetFiles(string path, string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public static string[] GetFiles(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static string[] GetFileSystemEntries(string path) { throw null; }
        public static string[] GetFileSystemEntries(string path, string searchPattern) { throw null; }
        public static string[] GetFileSystemEntries(string path, string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public static string[] GetFileSystemEntries(string path, string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public static System.DateTime GetLastAccessTime(string path) { throw null; }
        public static System.DateTime GetLastAccessTimeUtc(string path) { throw null; }
        public static System.DateTime GetLastWriteTime(string path) { throw null; }
        public static System.DateTime GetLastWriteTimeUtc(string path) { throw null; }
        public static string[] GetLogicalDrives() { throw null; }
        public static System.IO.DirectoryInfo? GetParent(string path) { throw null; }
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
        public System.IO.DirectoryInfo? Parent { get { throw null; } }
        public System.IO.DirectoryInfo Root { get { throw null; } }
        public void Create() { }
        public System.IO.DirectoryInfo CreateSubdirectory(string path) { throw null; }
        public override void Delete() { }
        public void Delete(bool recursive) { }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories() { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(string searchPattern) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles() { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles(string searchPattern) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles(string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileInfo> EnumerateFiles(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos() { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos(string searchPattern) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public System.Collections.Generic.IEnumerable<System.IO.FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.IO.DirectoryInfo[] GetDirectories() { throw null; }
        public System.IO.DirectoryInfo[] GetDirectories(string searchPattern) { throw null; }
        public System.IO.DirectoryInfo[] GetDirectories(string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public System.IO.DirectoryInfo[] GetDirectories(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.IO.FileInfo[] GetFiles() { throw null; }
        public System.IO.FileInfo[] GetFiles(string searchPattern) { throw null; }
        public System.IO.FileInfo[] GetFiles(string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public System.IO.FileInfo[] GetFiles(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public System.IO.FileSystemInfo[] GetFileSystemInfos() { throw null; }
        public System.IO.FileSystemInfo[] GetFileSystemInfos(string searchPattern) { throw null; }
        public System.IO.FileSystemInfo[] GetFileSystemInfos(string searchPattern, System.IO.EnumerationOptions enumerationOptions) { throw null; }
        public System.IO.FileSystemInfo[] GetFileSystemInfos(string searchPattern, System.IO.SearchOption searchOption) { throw null; }
        public void MoveTo(string destDirName) { }
        public override string ToString() { throw null; }
    }
    public partial class EnumerationOptions
    {
        public EnumerationOptions() { }
        public System.IO.FileAttributes AttributesToSkip { get { throw null; } set { } }
        public int BufferSize { get { throw null; } set { } }
        public bool IgnoreInaccessible { get { throw null; } set { } }
        public System.IO.MatchCasing MatchCasing { get { throw null; } set { } }
        public System.IO.MatchType MatchType { get { throw null; } set { } }
        public bool RecurseSubdirectories { get { throw null; } set { } }
        public bool ReturnSpecialDirectories { get { throw null; } set { } }
    }
    public static partial class File
    {
        public static void AppendAllLines(string path, System.Collections.Generic.IEnumerable<string> contents) { }
        public static void AppendAllLines(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding) { }
        public static System.Threading.Tasks.Task AppendAllLinesAsync(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task AppendAllLinesAsync(string path, System.Collections.Generic.IEnumerable<string> contents, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static void AppendAllText(string path, string? contents) { }
        public static void AppendAllText(string path, string? contents, System.Text.Encoding encoding) { }
        public static System.Threading.Tasks.Task AppendAllTextAsync(string path, string? contents, System.Text.Encoding encoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task AppendAllTextAsync(string path, string? contents, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.IO.StreamWriter AppendText(string path) { throw null; }
        public static void Copy(string sourceFileName, string destFileName) { }
        public static void Copy(string sourceFileName, string destFileName, bool overwrite) { }
        public static System.IO.FileStream Create(string path) { throw null; }
        public static System.IO.FileStream Create(string path, int bufferSize) { throw null; }
        public static System.IO.FileStream Create(string path, int bufferSize, System.IO.FileOptions options) { throw null; }
        public static System.IO.StreamWriter CreateText(string path) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void Decrypt(string path) { }
        public static void Delete(string path) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void Encrypt(string path) { }
        public static bool Exists(string? path) { throw null; }
        public static System.IO.FileAttributes GetAttributes(string path) { throw null; }
        public static System.DateTime GetCreationTime(string path) { throw null; }
        public static System.DateTime GetCreationTimeUtc(string path) { throw null; }
        public static System.DateTime GetLastAccessTime(string path) { throw null; }
        public static System.DateTime GetLastAccessTimeUtc(string path) { throw null; }
        public static System.DateTime GetLastWriteTime(string path) { throw null; }
        public static System.DateTime GetLastWriteTimeUtc(string path) { throw null; }
        public static void Move(string sourceFileName, string destFileName) { }
        public static void Move(string sourceFileName, string destFileName, bool overwrite) { }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode) { throw null; }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access) { throw null; }
        public static System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { throw null; }
        public static System.IO.FileStream OpenRead(string path) { throw null; }
        public static System.IO.StreamReader OpenText(string path) { throw null; }
        public static System.IO.FileStream OpenWrite(string path) { throw null; }
        public static byte[] ReadAllBytes(string path) { throw null; }
        public static System.Threading.Tasks.Task<byte[]> ReadAllBytesAsync(string path, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static string[] ReadAllLines(string path) { throw null; }
        public static string[] ReadAllLines(string path, System.Text.Encoding encoding) { throw null; }
        public static System.Threading.Tasks.Task<string[]> ReadAllLinesAsync(string path, System.Text.Encoding encoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<string[]> ReadAllLinesAsync(string path, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static string ReadAllText(string path) { throw null; }
        public static string ReadAllText(string path, System.Text.Encoding encoding) { throw null; }
        public static System.Threading.Tasks.Task<string> ReadAllTextAsync(string path, System.Text.Encoding encoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<string> ReadAllTextAsync(string path, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> ReadLines(string path) { throw null; }
        public static System.Collections.Generic.IEnumerable<string> ReadLines(string path, System.Text.Encoding encoding) { throw null; }
        public static void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName) { }
        public static void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors) { }
        public static void SetAttributes(string path, System.IO.FileAttributes fileAttributes) { }
        public static void SetCreationTime(string path, System.DateTime creationTime) { }
        public static void SetCreationTimeUtc(string path, System.DateTime creationTimeUtc) { }
        public static void SetLastAccessTime(string path, System.DateTime lastAccessTime) { }
        public static void SetLastAccessTimeUtc(string path, System.DateTime lastAccessTimeUtc) { }
        public static void SetLastWriteTime(string path, System.DateTime lastWriteTime) { }
        public static void SetLastWriteTimeUtc(string path, System.DateTime lastWriteTimeUtc) { }
        public static void WriteAllBytes(string path, byte[] bytes) { }
        public static System.Threading.Tasks.Task WriteAllBytesAsync(string path, byte[] bytes, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static void WriteAllLines(string path, System.Collections.Generic.IEnumerable<string> contents) { }
        public static void WriteAllLines(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding) { }
        public static void WriteAllLines(string path, string[] contents) { }
        public static void WriteAllLines(string path, string[] contents, System.Text.Encoding encoding) { }
        public static System.Threading.Tasks.Task WriteAllLinesAsync(string path, System.Collections.Generic.IEnumerable<string> contents, System.Text.Encoding encoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task WriteAllLinesAsync(string path, System.Collections.Generic.IEnumerable<string> contents, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static void WriteAllText(string path, string? contents) { }
        public static void WriteAllText(string path, string? contents, System.Text.Encoding encoding) { }
        public static System.Threading.Tasks.Task WriteAllTextAsync(string path, string? contents, System.Text.Encoding encoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task WriteAllTextAsync(string path, string? contents, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class FileInfo : System.IO.FileSystemInfo
    {
        public FileInfo(string fileName) { }
        public System.IO.DirectoryInfo? Directory { get { throw null; } }
        public string? DirectoryName { get { throw null; } }
        public override bool Exists { get { throw null; } }
        public bool IsReadOnly { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.IO.StreamWriter AppendText() { throw null; }
        public System.IO.FileInfo CopyTo(string destFileName) { throw null; }
        public System.IO.FileInfo CopyTo(string destFileName, bool overwrite) { throw null; }
        public System.IO.FileStream Create() { throw null; }
        public System.IO.StreamWriter CreateText() { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public void Decrypt() { }
        public override void Delete() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public void Encrypt() { }
        public void MoveTo(string destFileName) { }
        public void MoveTo(string destFileName, bool overwrite) { }
        public System.IO.FileStream Open(System.IO.FileMode mode) { throw null; }
        public System.IO.FileStream Open(System.IO.FileMode mode, System.IO.FileAccess access) { throw null; }
        public System.IO.FileStream Open(System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { throw null; }
        public System.IO.FileStream OpenRead() { throw null; }
        public System.IO.StreamReader OpenText() { throw null; }
        public System.IO.FileStream OpenWrite() { throw null; }
        public System.IO.FileInfo Replace(string destinationFileName, string? destinationBackupFileName) { throw null; }
        public System.IO.FileInfo Replace(string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors) { throw null; }
        public override string ToString() { throw null; }
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
        public System.DateTime LastAccessTime { get { throw null; } set { } }
        public System.DateTime LastAccessTimeUtc { get { throw null; } set { } }
        public System.DateTime LastWriteTime { get { throw null; } set { } }
        public System.DateTime LastWriteTimeUtc { get { throw null; } set { } }
        public abstract string Name { get; }
        public abstract void Delete();
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void Refresh() { }
        public override string ToString() { throw null; }
    }
    public enum MatchCasing
    {
        PlatformDefault = 0,
        CaseSensitive = 1,
        CaseInsensitive = 2,
    }
    public enum MatchType
    {
        Simple = 0,
        Win32 = 1,
    }
    public enum SearchOption
    {
        TopDirectoryOnly = 0,
        AllDirectories = 1,
    }
}
namespace System.IO.Enumeration
{
    public ref partial struct FileSystemEntry
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.IO.FileAttributes Attributes { get { throw null; } }
        public System.DateTimeOffset CreationTimeUtc { get { throw null; } }
        public readonly System.ReadOnlySpan<char> Directory { get { throw null; } }
        public System.ReadOnlySpan<char> FileName { get { throw null; } }
        public bool IsDirectory { get { throw null; } }
        public bool IsHidden { get { throw null; } }
        public System.DateTimeOffset LastAccessTimeUtc { get { throw null; } }
        public System.DateTimeOffset LastWriteTimeUtc { get { throw null; } }
        public long Length { get { throw null; } }
        public readonly System.ReadOnlySpan<char> OriginalRootDirectory { get { throw null; } }
        public readonly System.ReadOnlySpan<char> RootDirectory { get { throw null; } }
        public System.IO.FileSystemInfo ToFileSystemInfo() { throw null; }
        public string ToFullPath() { throw null; }
        public string ToSpecifiedFullPath() { throw null; }
    }
    public partial class FileSystemEnumerable<TResult> : System.Collections.Generic.IEnumerable<TResult>, System.Collections.IEnumerable
    {
        public FileSystemEnumerable(string directory, System.IO.Enumeration.FileSystemEnumerable<TResult>.FindTransform transform, System.IO.EnumerationOptions? options = null) { }
        public System.IO.Enumeration.FileSystemEnumerable<TResult>.FindPredicate? ShouldIncludePredicate { get { throw null; } set { } }
        public System.IO.Enumeration.FileSystemEnumerable<TResult>.FindPredicate? ShouldRecursePredicate { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerator<TResult> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public delegate bool FindPredicate(ref System.IO.Enumeration.FileSystemEntry entry);
        public delegate TResult FindTransform(ref System.IO.Enumeration.FileSystemEntry entry);
    }
    public abstract partial class FileSystemEnumerator<TResult> : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, System.Collections.Generic.IEnumerator<TResult>, System.Collections.IEnumerator, System.IDisposable
    {
        public FileSystemEnumerator(string directory, System.IO.EnumerationOptions? options = null) { }
        public TResult Current { get { throw null; } }
        object? System.Collections.IEnumerator.Current { get { throw null; } }
        protected virtual bool ContinueOnError(int error) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public bool MoveNext() { throw null; }
        protected virtual void OnDirectoryFinished(System.ReadOnlySpan<char> directory) { }
        public void Reset() { }
        protected virtual bool ShouldIncludeEntry(ref System.IO.Enumeration.FileSystemEntry entry) { throw null; }
        protected virtual bool ShouldRecurseIntoEntry(ref System.IO.Enumeration.FileSystemEntry entry) { throw null; }
        protected abstract TResult TransformEntry(ref System.IO.Enumeration.FileSystemEntry entry);
    }
    public static partial class FileSystemName
    {
        public static bool MatchesSimpleExpression(System.ReadOnlySpan<char> expression, System.ReadOnlySpan<char> name, bool ignoreCase = true) { throw null; }
        public static bool MatchesWin32Expression(System.ReadOnlySpan<char> expression, System.ReadOnlySpan<char> name, bool ignoreCase = true) { throw null; }
        public static string TranslateWin32Expression(string? expression) { throw null; }
    }
}
