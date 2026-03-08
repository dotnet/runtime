// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides methods and static methods to decode data compressed in the ZLib data format in a streamless, non-allocating, and performant manner.
    /// </summary>
    public sealed class ZLibDecoder : IDisposable
    {
        private readonly DeflateDecoder _deflateDecoder;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibDecoder"/> class.
        /// </summary>
        /// <exception cref="IOException">Failed to create the <see cref="ZLibDecoder"/> instance.</exception>
        public ZLibDecoder()
        {
            _deflateDecoder = new DeflateDecoder(ZLibNative.ZLib_DefaultWindowBits);
        }

        /// <summary>
        /// Frees and disposes unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            _deflateDecoder.Dispose();
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        /// Decompresses a read-only byte span into a destination span.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the compressed source data.</param>
        /// <param name="destination">When this method returns, a byte span where the decompressed data is stored.</param>
        /// <param name="bytesConsumed">When this method returns, the total number of bytes that were read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns>One of the enumeration values that describes the status with which the span-based operation finished.</returns>
        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            EnsureNotDisposed();
            return _deflateDecoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);
        }

        /// <summary>
        /// Tries to decompress a source byte span into a destination span.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the compressed source data.</param>
        /// <param name="destination">When this method returns, a span of bytes where the decompressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the decompression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            using var decoder = new ZLibDecoder();
            OperationStatus status = decoder.Decompress(source, destination, out int consumed, out bytesWritten);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}
