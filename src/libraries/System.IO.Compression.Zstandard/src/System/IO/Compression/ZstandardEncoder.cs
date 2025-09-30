// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to compress data using Zstandard compression.</summary>
    public struct ZstandardEncoder : IDisposable
    {
        internal SafeZstdCompressHandle? _context;
        private bool _disposed;

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> struct with the specified quality and window size.</summary>
        /// <param name="quality">The compression quality level.</param>
        /// <param name="window">The window size for compression.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not between the minimum and maximum allowed values, or <paramref name="window"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder(int quality, int window)
        {
            _disposed = false;
            InitializeEncoder();

            try
            {
                SetQuality(quality);
                SetWindow(window);
            }
            catch
            {
                _context!.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> struct with the specified dictionary and window size.</summary>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <param name="window">The window size for compression.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="window"/> is not between the minimum and maximum allowed values.</exception>
        public ZstandardEncoder(ZstandardDictionary dictionary, int window)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            if (dictionary.CompressionDictionary == null)
                throw new ArgumentException(SR.ZstandardEncoder_InvalidDictionary);

            _disposed = false;
            InitializeEncoder();

            try
            {
                SetWindow(window);
                _context!.SetDictionary(dictionary.CompressionDictionary);
            }
            catch
            {
                _context!.DangerousRelease();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardEncoder"/> struct with the specified compression options.</summary>
        /// <param name="options">The compression options to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The quality or window size in <paramref name="options"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZstandardEncoder"/> instance.</exception>
        public ZstandardEncoder(ZstandardCompressionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _disposed = false;
            InitializeEncoder();

            try
            {
                if (options.Dictionary is not null)
                {
                    if (options.Dictionary.CompressionDictionary is null)
                        throw new ArgumentException(SR.ZstandardEncoder_InvalidDictionary);

                    _context!.SetDictionary(options.Dictionary.CompressionDictionary);
                }
                else
                {
                    SetQuality(options.Quality);
                }

                if (options.Window != 0)
                {
                    SetWindow(options.Window);
                }

                if (options.AppendChecksum)
                {
                    SetFlag(Interop.Zstd.ZstdCParameter.ZSTD_c_checksumFlag);
                }

                if (options.EnableLongDistanceMatching)
                {
                    SetFlag(Interop.Zstd.ZstdCParameter.ZSTD_c_enableLongDistanceMatching);
                }
            }
            catch
            {
                _context!.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Performs a lazy initialization of the native encoder using the default Quality and Window values.
        /// </summary>
        internal void InitializeEncoder()
        {
            EnsureNotDisposed();

            _context = Interop.Zstd.ZSTD_createCCtx();
            if (_context.IsInvalid)
                throw new Interop.Zstd.ZstdNativeException(SR.ZstandardEncoder_Create);
        }

        internal void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_context == null)
            {
                InitializeEncoder();
            }
        }

        /// <summary>Compresses the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesConsumed">The number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <param name="isFinalBlock">True if this is the final block of data to compress.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        public OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            EnsureInitialized();

            bytesConsumed = 0;
            bytesWritten = 0;

            if (source.IsEmpty && !isFinalBlock)
                return OperationStatus.Done;

            return CompressCore(source, destination, out bytesConsumed, out bytesWritten,
                isFinalBlock ? Interop.Zstd.ZstdEndDirective.ZSTD_e_end : Interop.Zstd.ZstdEndDirective.ZSTD_e_continue);
        }

        /// <summary>Flushes any remaining data to the destination buffer.</summary>
        /// <param name="destination">The buffer to write the flushed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        public OperationStatus Flush(Span<byte> destination, out int bytesWritten)
        {
            EnsureInitialized();

            return CompressCore(ReadOnlySpan<byte>.Empty, destination, out _, out bytesWritten,
                Interop.Zstd.ZstdEndDirective.ZSTD_e_flush);
        }

        private readonly OperationStatus CompressCore(ReadOnlySpan<byte> source, Span<byte> destination,
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
                        src = (IntPtr)inBytes,
                        size = (nuint)source.Length,
                        pos = 0
                    };

                    var output = new Interop.Zstd.ZstdOutBuffer
                    {
                        dst = (IntPtr)outBytes,
                        size = (nuint)destination.Length,
                        pos = 0
                    };

                    nuint result = Interop.Zstd.ZSTD_compressStream2(_context!, ref output, ref input, endDirective);
                    Interop.Zstd.ZstdNativeException.ThrowIfError(result, SR.ZstandardEncoder_CompressError);

                    bytesConsumed = (int)input.pos;
                    bytesWritten = (int)output.pos;

                    if (input.pos == input.size)
                    {
                        return result == 0 ? OperationStatus.Done : OperationStatus.DestinationTooSmall;
                    }
                    else
                    {
                        return OperationStatus.DestinationTooSmall;
                    }
                }
            }
        }

        /// <summary>Gets the maximum compressed size for the specified input size.</summary>
        /// <param name="inputSize">The size of the input data.</param>
        /// <returns>The maximum possible compressed size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputSize"/> is less than 0.</exception>
        public static int GetMaxCompressedLength(int inputSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(inputSize);

            nuint result = Interop.Zstd.ZSTD_compressBound((nuint)inputSize);
            if (ZstandardUtils.IsError(result))
            {
                throw new ArgumentOutOfRangeException(nameof(inputSize));
            }

            if (result > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(inputSize));
            }

            return (int)result;
        }

        /// <summary>Attempts to compress the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <returns>True if compression was successful; otherwise, false.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return TryCompress(source, destination, out bytesWritten, ZstandardUtils.Quality_Default, ZstandardUtils.WindowBits_Default);
        }

        /// <summary>Attempts to compress the specified data with the specified quality and window size.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <param name="quality">The compression quality level.</param>
        /// <param name="window">The window size for compression.</param>
        /// <returns>True if compression was successful; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> or <paramref name="window"/> is out of the valid range.</exception>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int window)
        {
            if (quality < ZstandardUtils.Quality_Min || quality > ZstandardUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), string.Format(SR.ZstandardEncoder_InvalidQuality, ZstandardUtils.Quality_Min, ZstandardUtils.Quality_Max));
            }

            if (window < ZstandardUtils.WindowBits_Min || window > ZstandardUtils.WindowBits_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(window), string.Format(SR.ZstandardEncoder_InvalidWindow, ZstandardUtils.WindowBits_Min, ZstandardUtils.WindowBits_Max));
            }

            bytesWritten = 0;

            if (source.IsEmpty)
                return true;

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    nuint result = Interop.Zstd.ZSTD_compress((IntPtr)outBytes, (nuint)destination.Length, (IntPtr)inBytes, (nuint)source.Length, quality);

                    if (ZstandardUtils.IsError(result))
                    {
                        return false;
                    }

                    bytesWritten = (int)result;
                    return true;
                }
            }
        }

        /// <summary>Attempts to compress the specified data with the specified dictionary and window size.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">The number of bytes written to the destination.</param>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <param name="window">The window size for compression.</param>
        /// <returns>True if compression was successful; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="window"/> is out of the valid range.</exception>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZstandardDictionary dictionary, int window)
        {
            using ZstandardEncoder encoder = new ZstandardEncoder(dictionary, window);
            return encoder.Compress(source, destination, out _, out bytesWritten, isFinalBlock: true) == OperationStatus.Done;
        }

        /// <summary>Resets the encoder session, allowing reuse for the next compression operation.</summary>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        /// <exception cref="IOException">Failed to reset the encoder session.</exception>
        public void Reset()
        {
            EnsureNotDisposed();

            _context?.Reset();
        }

        /// <summary>References a prefix for the next compression operation.</summary>
        /// <remarks>The prefix will be used only for the next compression frame and will be removed when <see cref="Reset"/> is called.</remarks>
        public void ReferencePrefix(ReadOnlyMemory<byte> prefix)
        {
            EnsureInitialized();

            _context!.SetPrefix(prefix);
        }

        /// <summary>Releases all resources used by the <see cref="ZstandardEncoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _context?.Dispose();
        }

        private readonly void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ZstandardEncoder));
        }

        internal void SetQuality(int quality)
        {
            EnsureNotDisposed();
            if (quality < ZstandardUtils.Quality_Min || quality > ZstandardUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), string.Format(SR.ZstandardEncoder_InvalidQuality, ZstandardUtils.Quality_Min, ZstandardUtils.Quality_Max));
            }
            if (_context == null || _context.IsInvalid || _context.IsClosed)
            {
                InitializeEncoder();
            }
            nuint result = Interop.Zstd.ZSTD_CCtx_setParameter(_context!, Interop.Zstd.ZstdCParameter.ZSTD_c_compressionLevel, quality);
            if (ZstandardUtils.IsError(result))
            {
                throw new IOException(string.Format(SR.ZstandardEncoder_CompressError, ZstandardUtils.GetErrorMessage(result)));
            }
        }

        internal void SetWindow(int window)
        {
            EnsureNotDisposed();
            if (window < ZstandardUtils.WindowBits_Min || window > ZstandardUtils.WindowBits_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(window), string.Format(SR.ZstandardEncoder_InvalidWindow, ZstandardUtils.WindowBits_Min, ZstandardUtils.WindowBits_Max));
            }
            if (_context == null || _context.IsInvalid || _context.IsClosed)
            {
                InitializeEncoder();
            }
            nuint result = Interop.Zstd.ZSTD_CCtx_setParameter(_context!, Interop.Zstd.ZstdCParameter.ZSTD_c_windowLog, window);
            if (ZstandardUtils.IsError(result))
            {
                throw new IOException(string.Format(SR.ZstandardEncoder_CompressError, ZstandardUtils.GetErrorMessage(result)));
            }
        }

        internal void SetFlag(Interop.Zstd.ZstdCParameter parameter)
        {
            nuint result = Interop.Zstd.ZSTD_CCtx_setParameter(_context!, parameter, 1);
            if (ZstandardUtils.IsError(result))
            {
                throw new IOException(string.Format(SR.ZstandardEncoder_CompressError, ZstandardUtils.GetErrorMessage(result)));
            }
        }
    }
}
