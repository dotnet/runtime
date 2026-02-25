// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides methods and static methods to encode data in a streamless, non-allocating, and performant manner using the GZip data format specification.
    /// </summary>
    public sealed class GZipEncoder : IDisposable
    {
        private readonly DeflateEncoder _deflateEncoder;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GZipEncoder"/> class using the default quality.
        /// </summary>
        /// <exception cref="IOException">Failed to create the <see cref="GZipEncoder"/> instance.</exception>
        public GZipEncoder()
            : this(ZLibCompressionOptions.DefaultQuality, ZLibCompressionOptions.DefaultWindowLog)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GZipEncoder"/> class using the specified quality.
        /// </summary>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not in the valid range (0-9).</exception>
        /// <exception cref="IOException">Failed to create the <see cref="GZipEncoder"/> instance.</exception>
        public GZipEncoder(int quality)
            : this(quality, ZLibCompressionOptions.DefaultWindowLog)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GZipEncoder"/> class using the specified options.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="GZipEncoder"/> instance.</exception>
        public GZipEncoder(ZLibCompressionOptions options)
        {
            _deflateEncoder = new DeflateEncoder(options, CompressionFormat.GZip);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GZipEncoder"/> class using the specified quality and window size.
        /// </summary>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <param name="windowLog">The base-2 logarithm of the window size (8-15). Larger values result in better compression at the expense of memory usage.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not in the valid range (0-9), or <paramref name="windowLog"/> is not in the valid range (8-15).</exception>
        /// <exception cref="IOException">Failed to create the <see cref="GZipEncoder"/> instance.</exception>
        public GZipEncoder(int quality, int windowLog)
        {
            _deflateEncoder = new DeflateEncoder(quality, windowLog, CompressionFormat.GZip);
        }

        /// <summary>
        /// Frees and disposes unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            _deflateEncoder.Dispose();
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        /// Gets the maximum expected compressed length for the provided input size.
        /// </summary>
        /// <param name="inputLength">The input size to get the maximum expected compressed length from.</param>
        /// <returns>A number representing the maximum compressed length for the provided input size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputLength"/> is negative or exceeds <see cref="uint.MaxValue"/>.</exception>
        public static long GetMaxCompressedLength(long inputLength)
        {
            // GZip has a larger header than raw deflate, so add extra overhead
            long baseLength = DeflateEncoder.GetMaxCompressedLength(inputLength);

            // GZip adds 18 bytes: 10-byte header + 8-byte trailer (CRC32 + original size)
            return baseLength + 18;
        }

        /// <summary>
        /// Compresses a read-only byte span into a destination span.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a byte span where the compressed data is stored.</param>
        /// <param name="bytesConsumed">When this method returns, the total number of bytes that were read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="isFinalBlock"><see langword="true"/> to finalize the internal stream, which prevents adding more input data when this method returns; <see langword="false"/> to allow the encoder to postpone the production of output until it has processed enough input.</param>
        /// <returns>One of the enumeration values that describes the status with which the span-based operation finished.</returns>
        public OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            EnsureNotDisposed();
            return _deflateEncoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        /// <summary>
        /// Compresses an empty read-only span of bytes into its destination, ensuring that output is produced for all the processed input.
        /// </summary>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data will be stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns>One of the enumeration values that describes the status with which the operation finished.</returns>
        public OperationStatus Flush(Span<byte> destination, out int bytesWritten)
        {
            EnsureNotDisposed();
            return _deflateEncoder.Flush(destination, out bytesWritten);
        }

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the default quality.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            => TryCompress(source, destination, out bytesWritten, ZLibCompressionOptions.DefaultQuality, ZLibCompressionOptions.DefaultWindowLog);

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the specified quality.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality)
            => TryCompress(source, destination, out bytesWritten, quality, ZLibCompressionOptions.DefaultWindowLog);

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the specified quality and window size.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <param name="windowLog">The base-2 logarithm of the window size (8-15). Larger values result in better compression at the expense of memory usage.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog)
        {
            using var encoder = new GZipEncoder(quality, windowLog);
            OperationStatus status = encoder.Compress(source, destination, out int consumed, out bytesWritten, isFinalBlock: true);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}
