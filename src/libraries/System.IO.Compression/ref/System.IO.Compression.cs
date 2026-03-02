// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Compression
{
    public enum CompressionLevel
    {
        Optimal = 0,
        Fastest = 1,
        NoCompression = 2,
        SmallestSize = 3,
    }
    public enum CompressionMode
    {
        Decompress = 0,
        Compress = 1,
    }
    public partial class DeflateStream : System.IO.Stream
    {
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public DeflateStream(System.IO.Stream stream, System.IO.Compression.ZLibCompressionOptions compressionOptions, bool leaveOpen = false) { }
        public System.IO.Stream BaseStream { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw null; }
        public override void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public partial class GZipStream : System.IO.Stream
    {
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public GZipStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public GZipStream(System.IO.Stream stream, System.IO.Compression.ZLibCompressionOptions compressionOptions, bool leaveOpen = false) { }
        public System.IO.Stream BaseStream { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw null; }
        public override void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public partial class ZipArchive : System.IAsyncDisposable, System.IDisposable
    {
        public ZipArchive(System.IO.Stream stream) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode, bool leaveOpen) { }
        public ZipArchive(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode, bool leaveOpen, System.Text.Encoding? entryNameEncoding) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Comment { get { throw null; } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.IO.Compression.ZipArchiveEntry> Entries { get { throw null; } }
        public System.IO.Compression.ZipArchiveMode Mode { get { throw null; } }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchive> CreateAsync(System.IO.Stream stream, System.IO.Compression.ZipArchiveMode mode, bool leaveOpen, System.Text.Encoding? entryNameEncoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.IO.Compression.ZipArchiveEntry CreateEntry(string entryName) { throw null; }
        public System.IO.Compression.ZipArchiveEntry CreateEntry(string entryName, System.IO.Compression.CompressionLevel compressionLevel) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public System.IO.Compression.ZipArchiveEntry? GetEntry(string entryName) { throw null; }
    }
    public partial class ZipArchiveEntry
    {
        internal ZipArchiveEntry() { }
        public System.IO.Compression.ZipArchive Archive { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Comment { get { throw null; } set { } }
        public long CompressedLength { get { throw null; } }
        public System.IO.Compression.ZipCompressionMethod CompressionMethod { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public uint Crc32 { get { throw null; } }
        public int ExternalAttributes { get { throw null; } set { } }
        public string FullName { get { throw null; } }
        public bool IsEncrypted { get { throw null; } }
        public System.DateTimeOffset LastWriteTime { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public string Name { get { throw null; } }
        public void Delete() { }
        public System.IO.Stream Open() { throw null; }
        public System.IO.Stream Open(FileAccess access) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenAsync(System.IO.FileAccess access, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum ZipArchiveMode
    {
        Read = 0,
        Create = 1,
        Update = 2,
    }
    public enum ZipCompressionMethod
    {
        Stored = 0,
        Deflate = 8,
        Deflate64 = 9,
        ZStandard = 93,
    }
    public sealed partial class ZLibCompressionOptions
    {
        public ZLibCompressionOptions() { }
        public int CompressionLevel { get { throw null; } set { } }
        public System.IO.Compression.ZLibCompressionStrategy CompressionStrategy { get { throw null; } set { } }
    }
    public enum ZLibCompressionStrategy
    {
        Default = 0,
        Filtered = 1,
        HuffmanOnly = 2,
        RunLengthEncoding = 3,
        Fixed = 4,
    }
    public sealed partial class ZLibStream : System.IO.Stream
    {
        public ZLibStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public ZLibStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public ZLibStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public ZLibStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public ZLibStream(System.IO.Stream stream, System.IO.Compression.ZLibCompressionOptions compressionOptions, bool leaveOpen = false) { }
        public System.IO.Stream BaseStream { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? asyncCallback, object? asyncState) { throw null; }
        public override void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("wasi")]
    public sealed partial class ZstandardCompressionOptions
    {
        public ZstandardCompressionOptions() { }
        public bool AppendChecksum { get { throw null; } set { } }
        public static int DefaultQuality { get { throw null; } }
        public static int DefaultWindowLog { get { throw null; } }
        public System.IO.Compression.ZstandardDictionary? Dictionary { get { throw null; } set { } }
        public bool EnableLongDistanceMatching { get { throw null; } set { } }
        public static int MaxQuality { get { throw null; } }
        public static int MaxWindowLog { get { throw null; } }
        public static int MinQuality { get { throw null; } }
        public static int MinWindowLog { get { throw null; } }
        public int Quality { get { throw null; } set { } }
        public int TargetBlockSize { get { throw null; } set { } }
        public int WindowLog { get { throw null; } set { } }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("wasi")]
    public sealed partial class ZstandardDecoder : System.IDisposable
    {
        public ZstandardDecoder() { }
        public ZstandardDecoder(int maxWindowLog) { }
        public ZstandardDecoder(System.IO.Compression.ZstandardDictionary dictionary) { }
        public ZstandardDecoder(System.IO.Compression.ZstandardDictionary dictionary, int maxWindowLog) { }
        public System.Buffers.OperationStatus Decompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten) { throw null; }
        public void Dispose() { }
        public void Reset() { }
        public void SetPrefix(System.ReadOnlyMemory<byte> prefix) { }
        public static bool TryDecompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryDecompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, System.IO.Compression.ZstandardDictionary dictionary) { throw null; }
        public static bool TryGetMaxDecompressedLength(System.ReadOnlySpan<byte> data, out long length) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("wasi")]
    public sealed partial class ZstandardDictionary : System.IDisposable
    {
        internal ZstandardDictionary() { }
        public System.ReadOnlyMemory<byte> Data { get { throw null; } }
        public static System.IO.Compression.ZstandardDictionary Create(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static System.IO.Compression.ZstandardDictionary Create(System.ReadOnlySpan<byte> buffer, int quality) { throw null; }
        public void Dispose() { }
        public static System.IO.Compression.ZstandardDictionary Train(System.ReadOnlySpan<byte> samples, System.ReadOnlySpan<int> sampleLengths, int maxDictionarySize) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("wasi")]
    public sealed partial class ZstandardEncoder : System.IDisposable
    {
        public ZstandardEncoder() { }
        public ZstandardEncoder(int quality) { }
        public ZstandardEncoder(int quality, int windowLog) { }
        public ZstandardEncoder(System.IO.Compression.ZstandardCompressionOptions compressionOptions) { }
        public ZstandardEncoder(System.IO.Compression.ZstandardDictionary dictionary) { }
        public ZstandardEncoder(System.IO.Compression.ZstandardDictionary dictionary, int windowLog) { }
        public System.Buffers.OperationStatus Compress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) { throw null; }
        public void Dispose() { }
        public System.Buffers.OperationStatus Flush(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static long GetMaxCompressedLength(long inputLength) { throw null; }
        public void Reset() { }
        public void SetPrefix(System.ReadOnlyMemory<byte> prefix) { }
        public void SetSourceLength(long length) { }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, int quality, int windowLog) { throw null; }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, System.IO.Compression.ZstandardDictionary dictionary, int windowLog) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("wasi")]
    public sealed partial class ZstandardStream : System.IO.Stream
    {
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, System.IO.Compression.ZstandardDictionary dictionary, bool leaveOpen = false) { }
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.ZstandardCompressionOptions compressionOptions, bool leaveOpen = false) { }
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.ZstandardDecoder decoder, bool leaveOpen = false) { }
        public ZstandardStream(System.IO.Stream stream, System.IO.Compression.ZstandardEncoder encoder, bool leaveOpen = false) { }
        public System.IO.Stream BaseStream { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public void SetSourceLength(long length) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
}
