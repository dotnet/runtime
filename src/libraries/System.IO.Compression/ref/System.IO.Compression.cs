// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO.Compression
{
    public enum CompressionLevel
    {
        Fastest = 1,
        NoCompression = 2,
        Optimal = 0,
    }
    public enum CompressionMode
    {
        Compress = 1,
        Decompress = 0,
    }
    public partial class DeflateStream : System.IO.Stream
    {
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public System.IO.Stream BaseStream { get { return default(System.IO.Stream); } }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override int Read(byte[] array, int offset, int count) { return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
    public partial class GZipStream : System.IO.Stream
    {
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public System.IO.Stream BaseStream { get { return default(System.IO.Stream); } }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override int Read(byte[] array, int offset, int count) { return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
    public partial class ZipArchive : System.IDisposable
    {
        public ZipArchive(System.IO.Stream stream) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode, bool leaveOpen) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode, bool leaveOpen, System.Text.Encoding entryNameEncoding) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.IO.Compression.ZipArchiveEntry> Entries { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.IO.Compression.ZipArchiveEntry>); } }
        public System.IO.Compression.ZipArchiveMode Mode { get { return default(System.IO.Compression.ZipArchiveMode); } }
        public System.IO.Compression.ZipArchiveEntry CreateEntry(string entryName) { return default(System.IO.Compression.ZipArchiveEntry); }
        public System.IO.Compression.ZipArchiveEntry CreateEntry(string entryName, System.IO.Compression.CompressionLevel compressionLevel) { return default(System.IO.Compression.ZipArchiveEntry); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.IO.Compression.ZipArchiveEntry GetEntry(string entryName) { return default(System.IO.Compression.ZipArchiveEntry); }
    }
    public partial class ZipArchiveEntry
    {
        internal ZipArchiveEntry() { }
        public System.IO.Compression.ZipArchive Archive { get { return default(System.IO.Compression.ZipArchive); } }
        public long CompressedLength { get { return default(long); } }
        public string FullName { get { return default(string); } }
        public System.DateTimeOffset LastWriteTime { get { return default(System.DateTimeOffset); } set { } }
        public long Length { get { return default(long); } }
        public string Name { get { return default(string); } }
        public void Delete() { }
        public System.IO.Stream Open() { return default(System.IO.Stream); }
        public override string ToString() { return default(string); }
    }
    public enum ZipArchiveMode
    {
        Create = 1,
        Read = 0,
        Update = 2,
    }
}
