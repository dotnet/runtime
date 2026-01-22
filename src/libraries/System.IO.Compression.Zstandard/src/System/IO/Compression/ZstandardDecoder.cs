// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to decompress data using Zstandard decompression.</summary>
    public sealed class ZstandardDecoder : IDisposable
    {
        private SafeZstdDecompressHandle _context;
        private bool _disposed;

        /// <summary>
        /// True if we finished decompressing the entire input.
        /// </summary>
        private bool _finished;

        /// <summary>Initializes a new instance of the <see cref="ZstandardDecoder"/> class with default settings.</summary>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardDecoder"/> instance.</exception>
        public ZstandardDecoder()
        {
            _disposed = false;
            _finished = false;
            InitializeDecoder();
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardDecoder"/> class with the specified maximum window size.</summary>
        /// <param name="maxWindowLog">The maximum window size to use for decompression, expressed as base 2 logarithm.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxWindowLog"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardDecoder"/> instance.</exception>
        public ZstandardDecoder(int maxWindowLog)
        {
            _disposed = false;

            InitializeDecoder();

            try
            {
                if (maxWindowLog != 0)
                {
                    SetWindowLog(maxWindowLog);
                }
            }
            catch
            {
                _context.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardDecoder"/> class with the specified dictionary.</summary>
        /// <param name="dictionary">The decompression dictionary to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardDecoder"/> instance.</exception>
        public ZstandardDecoder(ZstandardDictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _disposed = false;

            InitializeDecoder();

            try
            {
                SetDictionary(dictionary);
            }
            catch
            {
                _context.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardDecoder"/> class with the specified dictionary and maximum window size.</summary>
        /// <param name="dictionary">The decompression dictionary to use.</param>
        /// <param name="maxWindowLog">The maximum window size to use for decompression, expressed as base 2 logarithm.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxWindowLog"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardDecoder"/> instance.</exception>
        public ZstandardDecoder(ZstandardDictionary dictionary, int maxWindowLog)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _disposed = false;

            InitializeDecoder();

            try
            {
                if (maxWindowLog != 0)
                {
                    SetWindowLog(maxWindowLog);
                }

                SetDictionary(dictionary);
            }
            catch
            {
                _context.Dispose();
                throw;
            }
        }

        [MemberNotNull(nameof(_context))]
        private void InitializeDecoder()
        {
            _context = Interop.Zstd.ZSTD_createDCtx();
            if (_context.IsInvalid)
            {
                throw new OutOfMemoryException();
            }
        }

        /// <summary>Decompresses the specified data.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
        /// <exception cref="IOException">An error occurred during decompression.</exception>
        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            bytesWritten = 0;
            bytesConsumed = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            EnsureNotDisposed();

            if (destination.IsEmpty)
            {
                return OperationStatus.DestinationTooSmall;
            }

            unsafe
            {
                fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
                fixed (byte* destPtr = &MemoryMarshal.GetReference(destination))
                {
                    var input = new Interop.Zstd.ZstdInBuffer
                    {
                        src = sourcePtr,
                        size = (nuint)source.Length,
                        pos = 0
                    };

                    var output = new Interop.Zstd.ZstdOutBuffer
                    {
                        dst = destPtr,
                        size = (nuint)destination.Length,
                        pos = 0
                    };

                    nuint result = Interop.Zstd.ZSTD_decompressStream(_context, ref output, ref input);

                    if (ZstandardUtils.IsError(result, out var error))
                    {
                        switch (error)
                        {
                            // These specific errors are actionable by the caller and don't imply
                            // that the data itself is corrupt.
                            case Interop.Zstd.ZSTD_error.frameParameter_windowTooLarge:
                            case Interop.Zstd.ZSTD_error.dictionary_wrong:
                                ZstandardUtils.Throw(error);
                                break;

                            default:
                                return OperationStatus.InvalidData;
                        }
                    }

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

        /// <summary>Attempts to get the maximum decompressed length for the specified compressed data.</summary>
        /// <param name="data">The compressed data.</param>
        /// <param name="length">When this method returns <see langword="true" />, contains the maximum decompressed length.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> otherwise.</returns>
        public static bool TryGetMaxDecompressedLength(ReadOnlySpan<byte> data, out long length)
        {
            if (data.IsEmpty)
            {
                length = 0;
                return true;
            }

            unsafe
            {
                fixed (byte* dataPtr = &MemoryMarshal.GetReference(data))
                {
                    ulong frameContentSize = Interop.Zstd.ZSTD_decompressBound(dataPtr, (nuint)data.Length);

                    const ulong ZSTD_CONTENTSIZE_UNKNOWN = unchecked(0UL - 1);
                    const ulong ZSTD_CONTENTSIZE_ERROR = unchecked(0UL - 2);
                    if (frameContentSize == ZSTD_CONTENTSIZE_UNKNOWN || frameContentSize == ZSTD_CONTENTSIZE_ERROR || frameContentSize > int.MaxValue)
                    {
                        length = 0;
                        return false;
                    }

                    length = (long)frameContentSize;
                    return true;
                }
            }
        }

        /// <summary>Attempts to decompress the specified data.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> otherwise.</returns>
        /// <remarks>If this method returns <see langword="false" />, <paramref name="destination" /> may be empty or contain partially decompressed data, and <paramref name="bytesWritten" /> might be zero or greater than zero but less than the expected total.</remarks>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;

            if (source.IsEmpty)
            {
                return false;
            }

            unsafe
            {
                fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
                fixed (byte* destPtr = &MemoryMarshal.GetReference(destination))
                {
                    nuint result = Interop.Zstd.ZSTD_decompress(
                        destPtr, (nuint)destination.Length,
                        sourcePtr, (nuint)source.Length);

                    if (ZstandardUtils.IsError(result, out var error))
                    {
                        return false;
                    }

                    bytesWritten = (int)result;
                    return true;
                }
            }
        }

        /// <summary>Attempts to decompress the specified data using the specified dictionary.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="dictionary">The decompression dictionary to use.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <remarks>If this method returns <see langword="false" />, <paramref name="destination" /> may be empty or contain partially decompressed data, and <paramref name="bytesWritten" /> might be zero or greater than zero but less than the expected total.</remarks>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZstandardDictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            bytesWritten = 0;

            if (source.IsEmpty)
            {
                return false;
            }

            using var dctx = Interop.Zstd.ZSTD_createDCtx();
            if (dctx.IsInvalid)
            {
                throw new OutOfMemoryException();
            }

            unsafe
            {
                fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
                fixed (byte* destPtr = &MemoryMarshal.GetReference(destination))
                {
                    nuint result = Interop.Zstd.ZSTD_decompress_usingDDict(
                        dctx, destPtr, (nuint)destination.Length,
                        sourcePtr, (nuint)source.Length, dictionary.DecompressionDictionary);

                    if (ZstandardUtils.IsError(result, out var error))
                    {
                        return false;
                    }

                    bytesWritten = checked((int)result);
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

            _context.Reset();
            _finished = false;
        }

        /// <summary>References a prefix for the next decompression operation.</summary>
        /// <remarks>The prefix will be used only for the next decompression frame and will be removed when <see cref="Reset"/> is called. The referenced data must remain valid and unmodified for the duration of the decompression operation.</remarks>
        /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The decoder is in an invalid state for setting a prefix.</exception>
        public void SetPrefix(ReadOnlyMemory<byte> prefix)
        {
            EnsureNotDisposed();

            if (_finished)
            {
                throw new InvalidOperationException(SR.ZstandardEncoderDecoder_InvalidState);
            }

            nuint result = _context.SetPrefix(prefix);

            if (ZstandardUtils.IsError(result, out var error))
            {
                ZstandardUtils.Throw(error);
            }
        }

        /// <summary>Releases all resources used by the <see cref="ZstandardDecoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _context.Dispose();
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        internal void SetWindowLog(int maxWindowLog)
        {
            Debug.Assert(_context != null);

            if (maxWindowLog != 0)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(maxWindowLog, ZstandardUtils.WindowLog_Min, nameof(maxWindowLog));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(maxWindowLog, ZstandardUtils.WindowLog_Max, nameof(maxWindowLog));
            }

            nuint result = Interop.Zstd.ZSTD_DCtx_setParameter(_context, Interop.Zstd.ZstdDParameter.ZSTD_d_windowLogMax, maxWindowLog);
            ZstandardUtils.ThrowIfError(result);
        }

        internal void SetDictionary(ZstandardDictionary dictionary)
        {
            Debug.Assert(_context != null);
            ArgumentNullException.ThrowIfNull(dictionary);

            _context.SetDictionary(dictionary.DecompressionDictionary);
        }
    }
}
