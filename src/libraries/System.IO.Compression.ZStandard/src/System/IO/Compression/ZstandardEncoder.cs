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
        internal SafeZStdCompressHandle? _state;
        private bool _disposed;

        /// <summary>Initializes a new instance of the <see cref="ZStandardEncoder"/> struct with the specified quality and window size.</summary>
        /// <param name="quality">The compression quality level.</param>
        /// <param name="window">The window size for compression.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not between the minimum and maximum allowed values, or <paramref name="window"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZStandardEncoder"/> instance.</exception>
        public ZStandardEncoder(int quality, int window)
        {
            _disposed = false;
            _state = Interop.Zstd.ZSTD_createCCtx();
            if (_state.IsInvalid)
                throw new IOException(SR.ZStandardEncoder_Create);

            SetQuality(quality);
            SetWindow(window);
        }

        /// <summary>Initializes a new instance of the <see cref="ZStandardEncoder"/> struct with the specified dictionary and window size.</summary>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <param name="window">The window size for compression.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="window"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZStandardEncoder"/> instance.</exception>
        public ZStandardEncoder(ZStandardDictionary dictionary, int window)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            if (dictionary.CompressionDictionary == null)
                throw new ArgumentException(SR.ZStandardEncoder_InvalidDictionary);

            _disposed = false;
            _state = Interop.Zstd.ZSTD_createCCtx();
            if (_state.IsInvalid)
                throw new IOException(SR.ZStandardEncoder_Create);

            SetWindow(window);

            // Attach the dictionary to the compression context
            nuint result = Interop.Zstd.ZSTD_CCtx_refCDict(_state, dictionary.CompressionDictionary);
            if (Interop.Zstd.ZSTD_isError(result) != 0)
            {
                _state.Dispose();
                throw new IOException(SR.ZStandardEncoder_DictionaryAttachFailed);
            }
        }

        /// <summary>
        /// Performs a lazy initialization of the native encoder using the default Quality and Window values.
        /// </summary>
        internal void InitializeEncoder()
        {
            EnsureNotDisposed();
            _state = Interop.Zstd.ZSTD_createCCtx();
            if (_state.IsInvalid)
                throw new IOException(SR.ZStandardEncoder_Create);
        }

        internal void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_state == null)
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
                isFinalBlock ? Interop.ZStdEndDirective.ZSTD_e_end : Interop.ZStdEndDirective.ZSTD_e_continue);
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
                Interop.ZStdEndDirective.ZSTD_e_flush);
        }

        private readonly OperationStatus CompressCore(ReadOnlySpan<byte> source, Span<byte> destination,
            out int bytesConsumed, out int bytesWritten, Interop.ZStdEndDirective endDirective)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    var input = new Interop.ZStdInBuffer
                    {
                        src = (IntPtr)inBytes,
                        size = (nuint)source.Length,
                        pos = 0
                    };

                    var output = new Interop.ZStdOutBuffer
                    {
                        dst = (IntPtr)outBytes,
                        size = (nuint)destination.Length,
                        pos = 0
                    };

                    nuint result = Interop.Zstd.ZSTD_compressStream2(_state!, ref output, ref input, endDirective);

                    if (ZStandardUtils.IsError(result))
                    {
                        throw new IOException(string.Format(SR.ZStandardEncoder_CompressError, ZStandardUtils.GetErrorMessage(result)));
                    }

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
            if (ZStandardUtils.IsError(result))
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
            return TryCompress(source, destination, out bytesWritten, ZStandardUtils.Quality_Default, ZStandardUtils.WindowBits_Default);
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
            if (quality < ZStandardUtils.Quality_Min || quality > ZStandardUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), string.Format(SR.ZStandardEncoder_InvalidQuality, ZStandardUtils.Quality_Min, ZStandardUtils.Quality_Max));
            }

            if (window < ZStandardUtils.WindowBits_Min || window > ZStandardUtils.WindowBits_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(window), string.Format(SR.ZStandardEncoder_InvalidWindow, ZStandardUtils.WindowBits_Min, ZStandardUtils.WindowBits_Max));
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

                    if (ZStandardUtils.IsError(result))
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
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZStandardDictionary dictionary, int window)
        {
            using ZStandardEncoder encoder = new ZStandardEncoder(dictionary, window);
            return encoder.Compress(source, destination, out _, out bytesWritten, isFinalBlock: true) == OperationStatus.Done;
        }

        /// <summary>Releases all resources used by the <see cref="ZStandardEncoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _state?.Dispose();
        }

        private readonly void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ZStandardEncoder));
        }

        internal void SetQuality(int quality)
        {
            EnsureNotDisposed();
            if (quality < ZStandardUtils.Quality_Min || quality > ZStandardUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), string.Format(SR.ZStandardEncoder_InvalidQuality, ZStandardUtils.Quality_Min, ZStandardUtils.Quality_Max));
            }
            if (_state == null || _state.IsInvalid || _state.IsClosed)
            {
                InitializeEncoder();
            }
            nuint result = Interop.Zstd.ZSTD_CCtx_setParameter(_state!, Interop.ZStdCParameter.ZSTD_c_compressionLevel, quality);
            if (ZStandardUtils.IsError(result))
            {
                throw new IOException(string.Format(SR.ZStandardEncoder_CompressError, ZStandardUtils.GetErrorMessage(result)));
            }
        }

        internal void SetWindow(int window)
        {
            EnsureNotDisposed();
            if (window < ZStandardUtils.WindowBits_Min || window > ZStandardUtils.WindowBits_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(window), string.Format(SR.ZStandardEncoder_InvalidWindow, ZStandardUtils.WindowBits_Min, ZStandardUtils.WindowBits_Max));
            }
            if (_state == null || _state.IsInvalid || _state.IsClosed)
            {
                InitializeEncoder();
            }
            nuint result = Interop.Zstd.ZSTD_CCtx_setParameter(_state!, Interop.ZStdCParameter.ZSTD_c_windowLog, window);
            if (ZStandardUtils.IsError(result))
            {
                throw new IOException(string.Format(SR.ZStandardEncoder_CompressError, ZStandardUtils.GetErrorMessage(result)));
            }
        }
    }
}
