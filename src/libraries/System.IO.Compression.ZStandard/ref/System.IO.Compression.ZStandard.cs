// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Compression
{
    public sealed partial class ZStandardCompressionOptions
    {
        public ZStandardCompressionOptions() { }
        public ZStandardCompressionOptions(int quality) { }
        public ZStandardCompressionOptions(System.IO.Compression.CompressionLevel level) { }
        public ZStandardCompressionOptions(System.IO.Compression.ZStandardDictionary dictionary) { }
        public static int DefaultQuality { get { throw null; } }
        public System.IO.Compression.ZStandardDictionary? Dictionary { get { throw null; } }
        public static int MaxQuality { get { throw null; } }
        public static int MinQuality { get { throw null; } }
        public int Quality { get { throw null; } }
    }
    public partial struct ZStandardDecoder : System.IDisposable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public ZStandardDecoder(System.IO.Compression.ZStandardDictionary dictionary) { throw null; }
        public readonly System.Buffers.OperationStatus Decompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten) { throw null; }
        public void Dispose() { }
        public static int GetMaxDecompressedLength(System.ReadOnlySpan<byte> data) { throw null; }
        public static bool TryDecompress(System.ReadOnlySpan<byte> source, System.IO.Compression.ZStandardDictionary dictionary, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryDecompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class ZStandardDictionary : System.IDisposable
    {
        internal ZStandardDictionary() { }
        public static System.IO.Compression.ZStandardDictionary Create(System.ReadOnlyMemory<byte> buffer) { throw null; }
        public static System.IO.Compression.ZStandardDictionary Create(System.ReadOnlyMemory<byte> buffer, int quality) { throw null; }
        public void Dispose() { }
    }
    public partial struct ZStandardEncoder : System.IDisposable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public ZStandardEncoder(int quality, int window) { throw null; }
        public ZStandardEncoder(System.IO.Compression.ZStandardDictionary dictionary, int window) { throw null; }
        public readonly System.Buffers.OperationStatus Compress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) { throw null; }
        public void Dispose() { }
        public readonly System.Buffers.OperationStatus Flush(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static int GetMaxCompressedLength(int inputSize) { throw null; }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, int quality, int window) { throw null; }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, System.IO.Compression.ZStandardDictionary dictionary, int window) { throw null; }
    }
    public sealed partial class ZStandardStream : System.IO.Stream
    {
        public ZStandardStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public ZStandardStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public ZStandardStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public ZStandardStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public ZStandardStream(System.IO.Stream stream, System.IO.Compression.ZStandardCompressionOptions compressionOptions, bool leaveOpen = false) { }
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
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
}
