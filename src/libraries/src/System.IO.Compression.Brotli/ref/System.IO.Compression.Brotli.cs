// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Compression
{
    public partial struct BrotliDecoder : System.IDisposable
    {
        public System.Buffers.OperationStatus Decompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten) { bytesConsumed = default(int); bytesWritten = default(int); throw null; }
        public static bool TryDecompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { bytesWritten = default(int); throw null; }
        public void Dispose() { }
    }
    public partial struct BrotliEncoder : System.IDisposable
    {
        public BrotliEncoder(int quality, int window) { }
        public System.Buffers.OperationStatus Compress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) { bytesConsumed = default(int); bytesWritten = default(int); throw null; }
        public System.Buffers.OperationStatus Flush(System.Span<byte> destination, out int bytesWritten) { bytesWritten = default(int); throw null; }
        public void Dispose() { }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { bytesWritten = default(int); throw null; }
        public static bool TryCompress(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten, int quality, int window) { bytesWritten = default(int); throw null; }
        public static int GetMaxCompressedLength(int inputSize) { throw null; }
    }
    public sealed partial class BrotliStream : System.IO.Stream
    {
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel) { }
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionLevel compressionLevel, bool leaveOpen) { }
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode) { }
        public BrotliStream(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, bool leaveOpen) { }
        public System.IO.Stream BaseStream { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override int EndRead(IAsyncResult asyncResult) { throw null; }
        public override int Read(byte[] array, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override IAsyncResult BeginWrite(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override void EndWrite(IAsyncResult asyncResult) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
