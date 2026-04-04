// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("wasi")]
    public sealed class ZstandardCompressionOptions
    {
        public ZstandardCompressionOptions() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public bool AppendChecksum { get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); }
        public static int DefaultQuality => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static int DefaultWindowLog => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardDictionary? Dictionary { get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); }
        public bool EnableLongDistanceMatching { get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); }
        public static int MaxQuality => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static int MaxWindowLog => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static int MinQuality => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static int MinWindowLog => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public int Quality { get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); }
        public int TargetBlockSize { get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); }
        public int WindowLog { get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); }
    }

    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("wasi")]
    public sealed class ZstandardDecoder : IDisposable
    {
        public ZstandardDecoder() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardDecoder(int maxWindowLog) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardDecoder(ZstandardDictionary dictionary) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardDecoder(ZstandardDictionary dictionary, int maxWindowLog) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public System.Buffers.OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void Dispose() { }
        public void Reset() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void SetPrefix(ReadOnlyMemory<byte> prefix) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZstandardDictionary dictionary) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static bool TryGetMaxDecompressedLength(ReadOnlySpan<byte> data, out long length) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
    }

    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("wasi")]
    public sealed class ZstandardDictionary : IDisposable
    {
        internal ZstandardDictionary() { }
        public ReadOnlyMemory<byte> Data => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static ZstandardDictionary Create(ReadOnlySpan<byte> buffer) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static ZstandardDictionary Create(ReadOnlySpan<byte> buffer, int quality) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void Dispose() { }
        public static ZstandardDictionary Train(ReadOnlySpan<byte> samples, ReadOnlySpan<int> sampleLengths, int maxDictionarySize) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
    }

    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("wasi")]
    public sealed class ZstandardEncoder : IDisposable
    {
        public ZstandardEncoder() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardEncoder(int quality) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardEncoder(int quality, int windowLog) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardEncoder(ZstandardCompressionOptions compressionOptions) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardEncoder(ZstandardDictionary dictionary) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardEncoder(ZstandardDictionary dictionary, int windowLog) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public System.Buffers.OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void Dispose() { }
        public System.Buffers.OperationStatus Flush(Span<byte> destination, out int bytesWritten) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static long GetMaxCompressedLength(long inputLength) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void Reset() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void SetPrefix(ReadOnlyMemory<byte> prefix) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void SetSourceLength(long length) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZstandardDictionary dictionary, int windowLog) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
    }

    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("wasi")]
    public sealed partial class ZstandardStream : Stream
    {
        public ZstandardStream(Stream stream, CompressionLevel compressionLevel) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardStream(Stream stream, CompressionMode mode) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardStream(Stream stream, CompressionMode mode, bool leaveOpen) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardStream(Stream stream, CompressionMode mode, ZstandardDictionary dictionary, bool leaveOpen = false) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardStream(Stream stream, ZstandardCompressionOptions compressionOptions, bool leaveOpen = false) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardStream(Stream stream, ZstandardDecoder decoder, bool leaveOpen = false) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public ZstandardStream(Stream stream, ZstandardEncoder encoder, bool leaveOpen = false) : base() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public Stream BaseStream => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override bool CanRead => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override bool CanSeek => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override bool CanWrite => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override long Length => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override long Position { get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression); }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() => default;
        public override int EndRead(IAsyncResult asyncResult) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override void EndWrite(IAsyncResult asyncResult) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override void Flush() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override int Read(byte[] buffer, int offset, int count) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override int Read(Span<byte> buffer) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override int ReadByte() => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override long Seek(long offset, SeekOrigin origin) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override void SetLength(long value) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public void SetSourceLength(long length) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override void Write(byte[] buffer, int offset, int count) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override void Write(ReadOnlySpan<byte> buffer) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override System.Threading.Tasks.ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
        public override void WriteByte(byte value) => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ZstandardCompression);
    }
}
