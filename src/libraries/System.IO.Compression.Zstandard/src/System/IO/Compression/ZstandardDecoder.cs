// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to decompress data using Zstandard decompression.</summary>
    public struct ZstandardDecoder : IDisposable
    {
        private SafeZstdDecompressHandle? _context;
        private bool _disposed;
        // True if we finished decompressing the entire input.
        private bool _finished;

        /// <summary>Initializes a new instance of the <see cref="ZstandardDecoder"/> struct with the specified dictionary.</summary>
        /// <param name="dictionary">The decompression dictionary to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public ZstandardDecoder(ZstandardDictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _disposed = false;

            InitializeDecoder();

            try
            {
                _context!.SetDictionary(dictionary.DecompressionDictionary);
            }
            catch
            {
                _context!.Dispose();
                throw;
            }
        }

        internal void InitializeDecoder()
        {
            _context = Interop.Zstd.ZSTD_createDCtx();
            if (_context.IsInvalid)
                throw new Interop.Zstd.ZstdNativeException(SR.ZstandardDecoder_Create);
        }

        internal void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_context == null)
                InitializeDecoder();
        }

        /// <summary>Decompresses the specified data.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesConsumed">The number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            if (_finished)
                return OperationStatus.Done;

            EnsureInitialized();

            if (destination.IsEmpty)
                return OperationStatus.DestinationTooSmall;

            unsafe
            {
                fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
                fixed (byte* destPtr = &MemoryMarshal.GetReference(destination))
                {
                    var input = new Interop.Zstd.ZstdInBuffer
                    {
                        src = (IntPtr)sourcePtr,
                        size = (nuint)source.Length,
                        pos = 0
                    };

                    var output = new Interop.Zstd.ZstdOutBuffer
                    {
                        dst = (IntPtr)destPtr,
                        size = (nuint)destination.Length,
                        pos = 0
                    };

                    nuint result = Interop.Zstd.ZSTD_decompressStream(_context!, ref output, ref input);

                    if (Interop.Zstd.ZSTD_isError(result) != 0)
                        return OperationStatus.InvalidData;

                    bytesConsumed = (int)input.pos;
                    bytesWritten = (int)output.pos;

                    if (result == 0)
                    {
                        _finished = true;
                        return OperationStatus.Done;
                    }
                    else if (output.pos == output.size)
                    {
                        return OperationStatus.DestinationTooSmall;
                    }
                    else
                    {
                        return OperationStatus.NeedMoreData;
                    }
                }
            }
        }

        /// <summary>Gets the maximum decompressed length for the specified compressed data.</summary>
        /// <param name="data">The compressed data.</param>
        /// <returns>The maximum decompressed length in bytes.</returns>
        public static int GetMaxDecompressedLength(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                return 0;

            unsafe
            {
                fixed (byte* dataPtr = &MemoryMarshal.GetReference(data))
                {
                    ulong frameContentSize = Interop.Zstd.ZSTD_getFrameContentSize((IntPtr)dataPtr, (nuint)data.Length);

                    // ZSTD_CONTENTSIZE_UNKNOWN = (0ULL - 1)
                    // ZSTD_CONTENTSIZE_ERROR = (0ULL - 2)
                    if (frameContentSize == ulong.MaxValue || frameContentSize == (ulong.MaxValue - 1))
                        return -1;

                    return frameContentSize > int.MaxValue ? int.MaxValue : (int)frameContentSize;
                }
            }
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
                return false;

            unsafe
            {
                fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
                fixed (byte* destPtr = &MemoryMarshal.GetReference(destination))
                {
                    nuint result = Interop.Zstd.ZSTD_decompress(
                        (IntPtr)destPtr, (nuint)destination.Length,
                        (IntPtr)sourcePtr, (nuint)source.Length);

                    if (Interop.Zstd.ZSTD_isError(result) != 0)
                        return false;

                    bytesWritten = (int)result;
                    return true;
                }
            }
        }

        /// <summary>Attempts to decompress the specified data using the specified dictionary.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="dictionary">The decompression dictionary to use.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>True if decompression was successful; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public static bool TryDecompress(ReadOnlySpan<byte> source, ZstandardDictionary dictionary, Span<byte> destination, out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            bytesWritten = 0;

            if (source.IsEmpty)
                return false;

            using var dctx = Interop.Zstd.ZSTD_createDCtx();
            if (dctx.IsInvalid)
                return false;

            unsafe
            {
                fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
                fixed (byte* destPtr = &MemoryMarshal.GetReference(destination))
                {
                    nuint result = Interop.Zstd.ZSTD_decompress_usingDDict(
                        dctx, (IntPtr)destPtr, (nuint)destination.Length,
                        (IntPtr)sourcePtr, (nuint)source.Length, dictionary.DecompressionDictionary);

                    if (Interop.Zstd.ZSTD_isError(result) != 0)
                        return false;

                    bytesWritten = (int)result;
                    return true;
                }
            }
        }

        /// <summary>Resets the decoder session, allowing reuse for the next decompression operation.</summary>
        /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
        /// <exception cref="IOException">Failed to reset the decoder session.</exception>
        public void Reset()
        {
            EnsureNotDisposed();

            if (_context is null)
                return;

            nuint result = Interop.Zstd.ZSTD_DCtx_reset(_context, Interop.Zstd.ZstdResetDirective.ZSTD_reset_session_only);
            if (Interop.Zstd.ZSTD_isError(result) != 0)
            {
                throw new IOException(string.Format(SR.ZstandardDecoder_DecompressError, ZstandardUtils.GetErrorMessage(result)));
            }

            _finished = false;
        }

        /// <summary>Releases all resources used by the <see cref="ZstandardDecoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _context?.Dispose();
        }

        private readonly void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private readonly void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ZstandardDecoder), SR.ZstandardDecoder_Disposed);
        }
    }
}
