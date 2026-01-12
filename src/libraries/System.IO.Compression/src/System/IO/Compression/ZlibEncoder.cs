// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides methods and static methods to encode and decode data in a streamless, non-allocating, and performant manner using the Deflate, ZLib, or GZip data format specification.
    /// </summary>
    public sealed class ZlibEncoder : IDisposable
    {
        private ZLibNative.ZLibStreamHandle? _state;
        private bool _disposed;
        private bool _finished;

        // Store construction parameters for Reset()
        private readonly int _compressionLevel;
        private readonly int _windowBits;
        private readonly ZLibCompressionStrategy _strategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibEncoder"/> class using the specified compression level and format.
        /// </summary>
        /// <param name="compressionLevel">A number representing compression level. -1 is default, 0 is no compression, 1 is best speed, 9 is best compression.</param>
        /// <param name="format">The compression format to use.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not between -1 and 9.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZlibEncoder"/> instance.</exception>
        public ZlibEncoder(int compressionLevel, ZlibCompressionFormat format)
            : this(compressionLevel, format, ZLibCompressionStrategy.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibEncoder"/> class using the specified compression level, format, and strategy.
        /// </summary>
        /// <param name="compressionLevel">A number representing compression level. -1 is default, 0 is no compression, 1 is best speed, 9 is best compression.</param>
        /// <param name="format">The compression format to use.</param>
        /// <param name="strategy">The compression strategy to use.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not between -1 and 9.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZlibEncoder"/> instance.</exception>
        public ZlibEncoder(int compressionLevel, ZlibCompressionFormat format, ZLibCompressionStrategy strategy)
        {
            ValidateCompressionLevel(compressionLevel);

            _disposed = false;
            _finished = false;
            _compressionLevel = compressionLevel;
            _windowBits = GetWindowBits(format);
            _strategy = strategy;

            _state = ZLibNative.ZLibStreamHandle.CreateForDeflate(
                (ZLibNative.CompressionLevel)_compressionLevel,
                _windowBits,
                ZLibNative.Deflate_DefaultMemLevel,
                (ZLibNative.CompressionStrategy)_strategy);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibEncoder"/> class using the specified options.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZlibEncoder"/> instance.</exception>
        public ZlibEncoder(ZlibEncoderOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _disposed = false;
            _finished = false;
            _compressionLevel = options.CompressionLevel;
            _windowBits = GetWindowBits(options.Format);
            _strategy = options.CompressionStrategy;

            _state = ZLibNative.ZLibStreamHandle.CreateForDeflate(
                (ZLibNative.CompressionLevel)_compressionLevel,
                _windowBits,
                ZLibNative.Deflate_DefaultMemLevel,
                (ZLibNative.CompressionStrategy)_strategy);
        }

        private static void ValidateCompressionLevel(int compressionLevel)
        {
            if (compressionLevel < -1 || compressionLevel > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(compressionLevel), SR.Format(SR.ZlibEncoder_CompressionLevel, compressionLevel, -1, 9));
            }
        }

        private static int GetWindowBits(ZlibCompressionFormat format)
        {
            return format switch
            {
                ZlibCompressionFormat.Deflate => ZLibNative.Deflate_DefaultWindowBits,
                ZlibCompressionFormat.ZLib => ZLibNative.ZLib_DefaultWindowBits,
                ZlibCompressionFormat.GZip => ZLibNative.GZip_DefaultWindowBits,
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

        /// <summary>
        /// Frees and disposes unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            _state?.Dispose();
            _state = null;
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_state is null)
            {
                throw new InvalidOperationException(SR.ZlibEncoder_NotInitialized);
            }
        }

        /// <summary>
        /// Gets the maximum expected compressed length for the provided input size.
        /// </summary>
        /// <param name="inputSize">The input size to get the maximum expected compressed length from.</param>
        /// <returns>A number representing the maximum compressed length for the provided input size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputSize"/> is negative.</exception>
        public static int GetMaxCompressedLength(int inputSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(inputSize);

            // ZLib's compressBound formula: inputSize + (inputSize >> 12) + (inputSize >> 14) + (inputSize >> 25) + 13
            // For GZip, add 18 bytes for header/trailer
            // We use a conservative estimate that works for all formats
            long result = inputSize + (inputSize >> 12) + (inputSize >> 14) + (inputSize >> 25) + 32;

            if (result > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(inputSize));
            }

            return (int)result;
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
            EnsureInitialized();
            Debug.Assert(_state is not null);

            bytesConsumed = 0;
            bytesWritten = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            ZLibNative.FlushCode flushCode = isFinalBlock ? ZLibNative.FlushCode.Finish : ZLibNative.FlushCode.NoFlush;

            unsafe
            {
                fixed (byte* inputPtr = &MemoryMarshal.GetReference(source))
                fixed (byte* outputPtr = &MemoryMarshal.GetReference(destination))
                {
                    _state.NextIn = (IntPtr)inputPtr;
                    _state.AvailIn = (uint)source.Length;
                    _state.NextOut = (IntPtr)outputPtr;
                    _state.AvailOut = (uint)destination.Length;

                    ZLibNative.ErrorCode errorCode = _state.Deflate(flushCode);

                    bytesConsumed = source.Length - (int)_state.AvailIn;
                    bytesWritten = destination.Length - (int)_state.AvailOut;

                    OperationStatus status = errorCode switch
                    {
                        ZLibNative.ErrorCode.Ok => _state.AvailIn == 0 && _state.AvailOut > 0
                            ? OperationStatus.Done
                            : _state.AvailOut == 0
                                ? OperationStatus.DestinationTooSmall
                                : OperationStatus.Done,
                        ZLibNative.ErrorCode.StreamEnd => OperationStatus.Done,
                        ZLibNative.ErrorCode.BufError => _state.AvailOut == 0
                            ? OperationStatus.DestinationTooSmall
                            : OperationStatus.NeedMoreData,
                        ZLibNative.ErrorCode.DataError => OperationStatus.InvalidData,
                        _ => OperationStatus.InvalidData
                    };

                    // Track if compression is finished
                    if (isFinalBlock && errorCode == ZLibNative.ErrorCode.StreamEnd)
                    {
                        _finished = true;
                    }

                    return status;
                }
            }
        }

        /// <summary>
        /// Compresses an empty read-only span of bytes into its destination, ensuring that output is produced for all the processed input.
        /// </summary>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data will be stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns>One of the enumeration values that describes the status with which the operation finished.</returns>
        public OperationStatus Flush(Span<byte> destination, out int bytesWritten)
        {
            return Compress(ReadOnlySpan<byte>.Empty, destination, out _, out bytesWritten, isFinalBlock: false);
        }

        /// <summary>
        /// Resets the encoder to its initial state, allowing it to be reused for a new compression operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        public void Reset()
        {
            EnsureNotDisposed();

            _finished = false;

            // Dispose the old state and create a new one
            _state?.Dispose();
            _state = ZLibNative.ZLibStreamHandle.CreateForDeflate(
                (ZLibNative.CompressionLevel)_compressionLevel,
                _windowBits,
                ZLibNative.Deflate_DefaultMemLevel,
                (ZLibNative.CompressionStrategy)_strategy);
        }

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the default compression level.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return TryCompress(source, destination, out bytesWritten, compressionLevel: -1, ZlibCompressionFormat.Deflate);
        }

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the specified compression level and format.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="compressionLevel">A number representing compression level. -1 is default, 0 is no compression, 1 is best speed, 9 is best compression.</param>
        /// <param name="format">The compression format to use.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int compressionLevel, ZlibCompressionFormat format)
        {
            ValidateCompressionLevel(compressionLevel);

            using var encoder = new ZlibEncoder(compressionLevel, format);
            OperationStatus status = encoder.Compress(source, destination, out int consumed, out bytesWritten, isFinalBlock: true);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}
