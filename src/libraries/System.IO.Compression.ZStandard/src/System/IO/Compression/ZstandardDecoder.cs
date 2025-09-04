// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to decompress data using ZStandard decompression.</summary>
    public struct ZStandardDecoder : IDisposable
    {
        private readonly SafeZStdDecompressHandle _context;
        private bool _disposed;

        /// <summary>Initializes a new instance of the <see cref="ZStandardDecoder"/> struct with the specified dictionary.</summary>
        /// <param name="dictionary">The decompression dictionary to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public ZStandardDecoder(ZStandardDictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _disposed = false;
            _context = Interop.Zstd.ZSTD_createDCtx();

            throw new NotImplementedException();
        }

        /// <summary>Decompresses the specified data.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesConsumed">The number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
        public readonly OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            ThrowIfDisposed();

            bytesConsumed = 0;
            bytesWritten = 0;

            if (source.IsEmpty)
                return OperationStatus.Done;

            throw new NotImplementedException();
        }

        /// <summary>Gets the maximum decompressed length for the specified compressed data.</summary>
        /// <param name="data">The compressed data.</param>
        /// <returns>The maximum decompressed length in bytes.</returns>
        public static int GetMaxDecompressedLength(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                return 0;

            throw new NotImplementedException();
        }

        /// <summary>Attempts to decompress the specified data.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>True if decompression was successful; otherwise, false.</returns>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;

            if (source.IsEmpty)
                return true;

            throw new NotImplementedException();
        }

        /// <summary>Attempts to decompress the specified data using the specified dictionary.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="dictionary">The decompression dictionary to use.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>True if decompression was successful; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public static bool TryDecompress(ReadOnlySpan<byte> source, ZStandardDictionary dictionary, Span<byte> destination, out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            bytesWritten = 0;

            if (source.IsEmpty)
                return true;

            throw new NotImplementedException();
        }

        /// <summary>Releases all resources used by the <see cref="ZStandardDecoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _context.Dispose();
        }

        private readonly void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ZStandardDecoder));
        }
    }
}
