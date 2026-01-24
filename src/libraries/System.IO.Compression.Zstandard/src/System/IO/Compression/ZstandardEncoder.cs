// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to compress data using Zstandard compression.</summary>
    public sealed class ZstandardEncoder : IDisposable
    {
        internal SafeZstdCompressHandle _context;
        private bool _disposed;

        /// <summary>
        /// True if we finished compressing the entire input.
        /// </summary>
        private bool _finished;

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> class with default settings.</summary>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder()
        {
            _disposed = false;
            InitializeEncoder();
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> class with the specified quality level.</summary>
        /// <param name="quality">The compression quality level.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder(int quality)
        {
            _disposed = false;
            InitializeEncoder();

            try
            {
                if (quality != 0)
                {
                    SetQuality(_context, quality);
                }
            }
            catch
            {
                _context.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> class with the specified dictionary.</summary>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder(ZstandardDictionary dictionary)
        {
            _disposed = false;
            InitializeEncoder();

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

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> class with the specified quality and window size.</summary>
        /// <param name="quality">The compression quality level.</param>
        /// <param name="windowLog">The window size for compression, expressed as base 2 logarithm.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not between the minimum and maximum allowed values, or <paramref name="windowLog"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder(int quality, int windowLog)
        {
            _disposed = false;
            InitializeEncoder();

            try
            {
                if (quality != 0)
                {
                    SetQuality(_context, quality);
                }
                if (windowLog != 0)
                {
                    SetWindowLog(_context, windowLog);
                }
            }
            catch
            {
                _context.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> class with the specified dictionary and window size.</summary>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <param name="windowLog">The window size for compression, expressed as base 2 logarithm.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowLog"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder(ZstandardDictionary dictionary, int windowLog)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _disposed = false;
            InitializeEncoder();

            try
            {
                SetDictionary(dictionary);

                if (windowLog != 0)
                {
                    SetWindowLog(_context, windowLog);
                }
            }
            catch
            {
                _context.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> class with the specified compression options.</summary>
        /// <param name="compressionOptions">The compression options to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="compressionOptions"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A parameter from <paramref name="compressionOptions"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder(ZstandardCompressionOptions compressionOptions)
        {
            ArgumentNullException.ThrowIfNull(compressionOptions);

            _disposed = false;
            InitializeEncoder();

            try
            {
                if (compressionOptions.Dictionary is not null)
                {
                    SetDictionary(compressionOptions.Dictionary);
                }
                else
                {
                    SetQuality(_context, compressionOptions.Quality);
                }

                if (compressionOptions.WindowLog != 0)
                {
                    SetWindowLog(_context, compressionOptions.WindowLog);
                }

                if (compressionOptions.AppendChecksum)
                {
                    SetParameter(_context, Interop.Zstd.ZstdCParameter.ZSTD_c_checksumFlag, 1);
                }

                if (compressionOptions.EnableLongDistanceMatching)
                {
                    SetParameter(_context, Interop.Zstd.ZstdCParameter.ZSTD_c_enableLongDistanceMatching, 1);
                }

                if (compressionOptions.TargetBlockSize != 0)
                {
                    SetParameter(_context, Interop.Zstd.ZstdCParameter.ZSTD_c_targetCBlockSize, compressionOptions.TargetBlockSize);
                }
            }
            catch
            {
                _context.Dispose();
                throw;
            }
        }

        [MemberNotNull(nameof(_context))]
        private void InitializeEncoder()
        {
            _context = Interop.Zstd.ZSTD_createCCtx();
            if (_context.IsInvalid)
            {
                throw new OutOfMemoryException();
            }
        }

        /// <summary>Compresses the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the destination.</param>
        /// <param name="isFinalBlock"><see langword="true" /> if this is the final block of data to compress.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        /// <exception cref="IOException">An error occurred during compression.</exception>
        public OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            EnsureNotDisposed();

            bytesConsumed = 0;
            bytesWritten = 0;

            if (source.IsEmpty && !isFinalBlock)
            {
                return OperationStatus.Done;
            }

            return CompressCore(source, destination, out bytesConsumed, out bytesWritten,
                isFinalBlock ? Interop.Zstd.ZstdEndDirective.ZSTD_e_end : Interop.Zstd.ZstdEndDirective.ZSTD_e_continue);
        }

        /// <summary>Flushes any remaining processed data to the destination buffer.</summary>
        /// <param name="destination">The buffer to write the flushed data to.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        /// <exception cref="IOException">An error occurred during the operation.</exception>
        public OperationStatus Flush(Span<byte> destination, out int bytesWritten)
        {
            EnsureNotDisposed();

            return CompressCore(ReadOnlySpan<byte>.Empty, destination, out _, out bytesWritten,
                Interop.Zstd.ZstdEndDirective.ZSTD_e_flush);
        }

        private OperationStatus CompressCore(ReadOnlySpan<byte> source, Span<byte> destination,
            out int bytesConsumed, out int bytesWritten, Interop.Zstd.ZstdEndDirective endDirective)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    var input = new Interop.Zstd.ZstdInBuffer
                    {
                        src = inBytes,
                        size = (nuint)source.Length,
                        pos = 0
                    };

                    var output = new Interop.Zstd.ZstdOutBuffer
                    {
                        dst = outBytes,
                        size = (nuint)destination.Length,
                        pos = 0
                    };

                    nuint result = Interop.Zstd.ZSTD_compressStream2(_context, ref output, ref input, endDirective);

                    if (ZstandardUtils.IsError(result, out var error))
                    {
                        if (error == Interop.Zstd.ZSTD_error.srcSize_wrong)
                        {
                            return OperationStatus.InvalidData;
                        }

                        ZstandardUtils.Throw(error);
                    }

                    bytesConsumed = (int)input.pos;
                    bytesWritten = (int)output.pos;

                    if (input.pos == input.size)
                    {
                        _finished |= endDirective == Interop.Zstd.ZstdEndDirective.ZSTD_e_end;

                        return result == 0 ? OperationStatus.Done : OperationStatus.DestinationTooSmall;
                    }
                    else
                    {
                        return OperationStatus.DestinationTooSmall;
                    }
                }
            }
        }

        /// <summary>Gets the maximum compressed size for the specified input length.</summary>
        /// <param name="inputLength">The length of the input data.</param>
        /// <returns>The maximum possible compressed size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputLength"/> is less than 0.</exception>
        public static long GetMaxCompressedLength(long inputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(inputLength);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(inputLength, nint.MaxValue);

            nuint result = Interop.Zstd.ZSTD_compressBound((nuint)inputLength);
            if (ZstandardUtils.IsError(result))
            {
                throw new ArgumentOutOfRangeException(nameof(inputLength));
            }

            if (result > long.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(inputLength));
            }

            return (long)result;
        }

        /// <summary>Attempts to compress the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> if the destination buffer is too small.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return TryCompress(source, destination, out bytesWritten, ZstandardUtils.Quality_Default, 0);
        }

        /// <summary>Attempts to compress the specified data with the specified quality and window size.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <param name="quality">The compression quality level.</param>
        /// <param name="windowLog">The window size for compression, expressed as base 2 logarithm.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> if the destination buffer is too small.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> or <paramref name="windowLog"/> is out of the valid range.</exception>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog)
        {
            return TryCompressCore(source, destination, out bytesWritten, quality, windowLog, null);
        }

        /// <summary>Attempts to compress the specified data with the specified dictionary and window size.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <param name="windowLog">The window size for compression, expressed as base 2 logarithm.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> if the destination buffer is too small.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowLog"/> is out of the valid range.</exception>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZstandardDictionary dictionary, int windowLog)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            return TryCompressCore(source, destination, out bytesWritten, 0, windowLog, dictionary);
        }

        internal static bool TryCompressCore(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog, ZstandardDictionary? dictionary)
        {
            bytesWritten = 0;

            using SafeZstdCompressHandle ctx = Interop.Zstd.ZSTD_createCCtx();
            if (ctx.IsInvalid)
            {
                throw new OutOfMemoryException();
            }

            if (dictionary != null)
            {
                ctx.SetDictionary(dictionary.CompressionDictionary);
            }
            else
            {
                SetQuality(ctx, quality);
            }

            if (windowLog != 0)
            {
                SetWindowLog(ctx, windowLog);
            }

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    nuint result = Interop.Zstd.ZSTD_compress2(ctx, outBytes, (nuint)destination.Length, inBytes, (nuint)source.Length);

                    if (ZstandardUtils.IsError(result, out var error))
                    {
                        return false;
                    }

                    bytesWritten = (int)result;
                    return true;
                }
            }
        }

        /// <summary>Resets the encoder session, allowing reuse for the next compression operation.</summary>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        /// <exception cref="IOException">Failed to reset the encoder session.</exception>
        public void Reset()
        {
            EnsureNotDisposed();

            _finished = false;
            _context.Reset();
        }

        /// <summary>References a prefix for the next compression operation.</summary>
        /// <remarks>The prefix will be used only for the next compression frame and will be removed when <see cref="Reset"/> is called. The referenced data must remain valid and unmodified for the duration of the compression operation.</remarks>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The encoder is in an invalid state for setting a prefix.</exception>
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

        /// <summary>Sets the length of the uncompressed data for the next compression operation.</summary>
        /// <param name="length">The length of the source data in bytes.</param>
        /// <remarks>
        /// Setting the source length is optional. If set, the information is stored in the header of the compressed data. This method can be called only before the first <see cref="Compress"/> method call, or after <see cref="Reset"/>.
        /// Calling <see cref="Reset"/> clears the length. The length is validated during compression and if the value is disrespected,
        /// the operation status is <see cref="OperationStatus.InvalidData"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        public void SetSourceLength(long length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            EnsureNotDisposed();

            if (_finished)
            {
                throw new InvalidOperationException(SR.ZstandardEncoderDecoder_InvalidState);
            }

            if (ZstandardUtils.IsError(Interop.Zstd.ZSTD_CCtx_setPledgedSrcSize(_context, (ulong)length), out var error))
            {
                ZstandardUtils.Throw(error);
            }
        }

        /// <summary>Releases all resources used by the <see cref="ZstandardEncoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _context.Dispose();
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ZstandardEncoder));
        }

        internal static void SetQuality(SafeZstdCompressHandle handle, int quality)
        {
            if (quality != 0)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(quality, ZstandardUtils.Quality_Min, nameof(quality));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, ZstandardUtils.Quality_Max, nameof(quality));
            }

            SetParameter(handle, Interop.Zstd.ZstdCParameter.ZSTD_c_compressionLevel, quality);
        }

        internal static void SetWindowLog(SafeZstdCompressHandle handle, int windowLog)
        {
            if (windowLog != 0)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(windowLog, ZstandardUtils.WindowLog_Min, nameof(windowLog));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(windowLog, ZstandardUtils.WindowLog_Max, nameof(windowLog));
            }

            SetParameter(handle, Interop.Zstd.ZstdCParameter.ZSTD_c_windowLog, windowLog);
        }

        internal void SetDictionary(ZstandardDictionary dictionary)
        {
            Debug.Assert(_context != null);
            ArgumentNullException.ThrowIfNull(dictionary);

            _context.SetDictionary(dictionary.CompressionDictionary);
        }

        internal static void SetParameter(SafeZstdCompressHandle handle, Interop.Zstd.ZstdCParameter parameter, int value)
        {
            Debug.Assert(handle != null);

            nuint result = Interop.Zstd.ZSTD_CCtx_setParameter(handle, parameter, value);
            ZstandardUtils.ThrowIfError(result);
        }
    }
}
