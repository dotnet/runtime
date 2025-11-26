// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Compression
{
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
    public sealed partial class ZstandardDictionary : System.IDisposable
    {
        internal ZstandardDictionary() { }
        public System.ReadOnlyMemory<byte> Data { get { throw null; } }
        public static System.IO.Compression.ZstandardDictionary Create(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static System.IO.Compression.ZstandardDictionary Create(System.ReadOnlySpan<byte> buffer, int quality) { throw null; }
        public void Dispose() { }
        public static System.IO.Compression.ZstandardDictionary Train(System.ReadOnlySpan<byte> samples, System.ReadOnlySpan<int> sampleLengths, int maxDictionarySize) { throw null; }
    }
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
