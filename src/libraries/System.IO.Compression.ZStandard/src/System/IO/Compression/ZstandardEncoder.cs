// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to compress data using ZStandard compression.</summary>
    public struct ZStandardEncoder : IDisposable
    {
        private readonly SafeZStdCompressHandle _context;
        private bool _disposed;

        /// <summary>Initializes a new instance of the <see cref="ZStandardEncoder"/> struct with the specified quality and window size.</summary>
        /// <param name="quality">The compression quality level.</param>
        /// <param name="window">The window size for compression.</param>
        public ZStandardEncoder(int quality, int window)
        {
            _disposed = false;
            _context = Interop.Zstd.ZSTD_createCCtx();

            throw new NotImplementedException();
        }

        /// <summary>Initializes a new instance of the <see cref="ZStandardEncoder"/> struct with the specified dictionary and window size.</summary>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <param name="window">The window size for compression.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public ZStandardEncoder(ZStandardDictionary dictionary, int window)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _disposed = false;
            _context = Interop.Zstd.ZSTD_createCCtx();

            throw new NotImplementedException();
        }

        /// <summary>Compresses the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesConsumed">The number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <param name="isFinalBlock">True if this is the final block of data to compress.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        public readonly OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            ThrowIfDisposed();

            bytesConsumed = 0;
            bytesWritten = 0;

            if (source.IsEmpty && !isFinalBlock)
                return OperationStatus.Done;

            throw new NotImplementedException();
        }

        /// <summary>Flushes any remaining data to the destination buffer.</summary>
        /// <param name="destination">The buffer to write the flushed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        public readonly OperationStatus Flush(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            bytesWritten = 0;

            throw new NotImplementedException();
        }

        /// <summary>Gets the maximum compressed size for the specified input size.</summary>
        /// <param name="inputSize">The size of the input data.</param>
        /// <returns>The maximum possible compressed size.</returns>
        public static int GetMaxCompressedLength(int inputSize)
        {
            throw new NotImplementedException();
        }

        /// <summary>Attempts to compress the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>True if compression was successful; otherwise, false.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;

            if (source.IsEmpty)
                return true;

            throw new NotImplementedException();
        }

        /// <summary>Attempts to compress the specified data with the specified quality and window size.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <param name="quality">The compression quality level.</param>
        /// <param name="window">The window size for compression.</param>
        /// <returns>True if compression was successful; otherwise, false.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int window)
        {
            bytesWritten = 0;

            if (source.IsEmpty)
                return true;

            throw new NotImplementedException();
        }

        /// <summary>Attempts to compress the specified data with the specified dictionary and window size.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <param name="window">The window size for compression.</param>
        /// <returns>True if compression was successful; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZStandardDictionary dictionary, int window)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            bytesWritten = 0;

            if (source.IsEmpty)
                return true;

            throw new NotImplementedException();
        }

        /// <summary>Releases all resources used by the <see cref="ZStandardEncoder"/>.</summary>
        public void Dispose()
        {
            _context.Dispose();
            _disposed = true;
        }

        private readonly void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ZStandardEncoder));
        }
    }
}
