// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO.IsolatedStorage
{
    public partial class IsolatedStorageException : System.Exception
    {
        public IsolatedStorageException() { }
        public IsolatedStorageException(string message) { }
        public IsolatedStorageException(string message, System.Exception inner) { }
    }
    public sealed partial class IsolatedStorageFile : System.IDisposable
    {
        internal IsolatedStorageFile() { }
        public void CopyFile(string sourceFileName, string destinationFileName) { }
        public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite) { }
        public void CreateDirectory(string dir) { }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream CreateFile(string path) { return default(System.IO.IsolatedStorage.IsolatedStorageFileStream); }
        public void DeleteDirectory(string dir) { }
        public void DeleteFile(string file) { }
        public bool DirectoryExists(string path) { return default(bool); }
        public void Dispose() { }
        public bool FileExists(string path) { return default(bool); }
        public System.DateTimeOffset GetCreationTime(string path) { return default(System.DateTimeOffset); }
        public string[] GetDirectoryNames() { return default(string[]); }
        public string[] GetDirectoryNames(string searchPattern) { return default(string[]); }
        public string[] GetFileNames() { return default(string[]); }
        public string[] GetFileNames(string searchPattern) { return default(string[]); }
        public System.DateTimeOffset GetLastAccessTime(string path) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset GetLastWriteTime(string path) { return default(System.DateTimeOffset); }
        public static System.IO.IsolatedStorage.IsolatedStorageFile GetUserStoreForApplication() { return default(System.IO.IsolatedStorage.IsolatedStorageFile); }
        public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName) { }
        public void MoveFile(string sourceFileName, string destinationFileName) { }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream OpenFile(string path, System.IO.FileMode mode) { return default(System.IO.IsolatedStorage.IsolatedStorageFileStream); }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access) { return default(System.IO.IsolatedStorage.IsolatedStorageFileStream); }
        public System.IO.IsolatedStorage.IsolatedStorageFileStream OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { return default(System.IO.IsolatedStorage.IsolatedStorageFileStream); }
    }
    public partial class IsolatedStorageFileStream : System.IO.Stream
    {
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, System.IO.IsolatedStorage.IsolatedStorageFile isf) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.IsolatedStorage.IsolatedStorageFile isf) { }
        public IsolatedStorageFileStream(string path, System.IO.FileMode mode, System.IO.IsolatedStorage.IsolatedStorageFile isf) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override int Read(byte[] buffer, int offset, int count) { return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override int ReadByte() { return default(int); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override void WriteByte(byte value) { }
    }
}
