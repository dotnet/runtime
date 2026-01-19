// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides methods and static methods to encode data in a streamless, non-allocating, and performant manner using the ZLib data format specification.
    /// </summary>
    public sealed class ZLibEncoder : IDisposable
    {
        private readonly DeflateEncoder _deflateEncoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibEncoder"/> class using the default compression level.
        /// </summary>
        /// <exception cref="IOException">Failed to create the <see cref="ZLibEncoder"/> instance.</exception>
        public ZLibEncoder()
            : this(CompressionLevel.Optimal, ZLibCompressionStrategy.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibEncoder"/> class using the specified compression level.
        /// </summary>
        /// <param name="compressionLevel">The compression level to use.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not a valid <see cref="CompressionLevel"/> value.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZLibEncoder"/> instance.</exception>
        public ZLibEncoder(CompressionLevel compressionLevel)
            : this(compressionLevel, ZLibCompressionStrategy.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibEncoder"/> class using the specified compression level and strategy.
        /// </summary>
        /// <param name="compressionLevel">The compression level to use.</param>
        /// <param name="strategy">The compression strategy to use.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not a valid <see cref="CompressionLevel"/> value.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZLibEncoder"/> instance.</exception>
        public ZLibEncoder(CompressionLevel compressionLevel, ZLibCompressionStrategy strategy)
        {
            _deflateEncoder = new DeflateEncoder(compressionLevel, strategy, ZLibNative.ZLib_DefaultWindowBits);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibEncoder"/> class using the specified options.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZLibEncoder"/> instance.</exception>
        public ZLibEncoder(ZLibCompressionOptions options)
        {
            _deflateEncoder = new DeflateEncoder(options, ZLibNative.ZLib_DefaultWindowBits);
        }

        /// <summary>
        /// Frees and disposes unmanaged resources.
        /// </summary>
        public void Dispose() => _deflateEncoder.Dispose();

        /// <summary>
        /// Gets the maximum expected compressed length for the provided input size.
        /// </summary>
        /// <param name="inputSize">The input size to get the maximum expected compressed length from.</param>
        /// <returns>A number representing the maximum compressed length for the provided input size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputSize"/> is negative.</exception>
        public static int GetMaxCompressedLength(int inputSize) => DeflateEncoder.GetMaxCompressedLength(inputSize);

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
            => _deflateEncoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        /// <summary>
        /// Compresses an empty read-only span of bytes into its destination, ensuring that output is produced for all the processed input.
        /// </summary>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data will be stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns>One of the enumeration values that describes the status with which the operation finished.</returns>
        public OperationStatus Flush(Span<byte> destination, out int bytesWritten)
            => _deflateEncoder.Flush(destination, out bytesWritten);

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the default compression level.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            => TryCompress(source, destination, out bytesWritten, CompressionLevel.Optimal);

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the specified compression level.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="compressionLevel">The compression level to use.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, CompressionLevel compressionLevel)
        {
            using var encoder = new ZLibEncoder(compressionLevel);
            OperationStatus status = encoder.Compress(source, destination, out int consumed, out bytesWritten, isFinalBlock: true);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}