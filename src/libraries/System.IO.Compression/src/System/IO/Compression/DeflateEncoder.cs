// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides methods and static methods to encode data in a streamless, non-allocating, and performant manner using the Deflate data format specification.
    /// </summary>
    public sealed class DeflateEncoder : IDisposable
    {
        private ZLibNative.ZLibStreamHandle? _state;
        private bool _disposed;
        private bool _finished;

        // Store construction parameters for encoder initialization
        private readonly ZLibNative.CompressionLevel _zlibCompressionLevel;
        private readonly ZLibCompressionStrategy _strategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the default compression level.
        /// </summary>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder()
            : this(CompressionLevel.Optimal, ZLibCompressionStrategy.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the specified compression level.
        /// </summary>
        /// <param name="compressionLevel">The compression level to use.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not a valid <see cref="CompressionLevel"/> value.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder(CompressionLevel compressionLevel)
            : this(compressionLevel, ZLibCompressionStrategy.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the specified compression level and strategy.
        /// </summary>
        /// <param name="compressionLevel">The compression level to use.</param>
        /// <param name="strategy">The compression strategy to use.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not a valid <see cref="CompressionLevel"/> value.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder(CompressionLevel compressionLevel, ZLibCompressionStrategy strategy)
            : this(compressionLevel, strategy, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the specified options.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder(ZLibCompressionOptions options)
            : this(options, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        /// <summary>
        /// Internal constructor to specify windowBits for different compression formats.
        /// </summary>
        internal DeflateEncoder(CompressionLevel compressionLevel, ZLibCompressionStrategy strategy, int windowBits)
        {
            ValidateCompressionLevel(compressionLevel);

            _disposed = false;
            _finished = false;
            _zlibCompressionLevel = GetZLibNativeCompressionLevel(compressionLevel);
            _strategy = strategy;

            _state = ZLibNative.ZLibStreamHandle.CreateForDeflate(
                _zlibCompressionLevel,
                windowBits,
                ZLibNative.Deflate_DefaultMemLevel,
                (ZLibNative.CompressionStrategy)_strategy);
        }

        /// <summary>
        /// Internal constructor to specify windowBits with options.
        /// </summary>
        internal DeflateEncoder(ZLibCompressionOptions options, int windowBits)
        {
            ArgumentNullException.ThrowIfNull(options);

            _disposed = false;
            _finished = false;
            _zlibCompressionLevel = (ZLibNative.CompressionLevel)options.CompressionLevel;
            _strategy = options.CompressionStrategy;

            _state = ZLibNative.ZLibStreamHandle.CreateForDeflate(
                _zlibCompressionLevel,
                windowBits,
                ZLibNative.Deflate_DefaultMemLevel,
                (ZLibNative.CompressionStrategy)_strategy);
        }

        private static void ValidateCompressionLevel(CompressionLevel compressionLevel)
        {
            if (compressionLevel < CompressionLevel.Optimal || compressionLevel > CompressionLevel.SmallestSize)
            {
                throw new ArgumentOutOfRangeException(nameof(compressionLevel));
            }
        }

        private static ZLibNative.CompressionLevel GetZLibNativeCompressionLevel(CompressionLevel compressionLevel) =>
            compressionLevel switch
            {
                CompressionLevel.Optimal => ZLibNative.CompressionLevel.DefaultCompression,
                CompressionLevel.Fastest => ZLibNative.CompressionLevel.BestSpeed,
                CompressionLevel.NoCompression => ZLibNative.CompressionLevel.NoCompression,
                CompressionLevel.SmallestSize => ZLibNative.CompressionLevel.BestCompression,
                _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel)),
            };

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
                throw new InvalidOperationException(SR.DeflateEncoder_NotInitialized);
            }
        }

        /// <summary>
        /// Gets the maximum expected compressed length for the provided input size.
        /// </summary>
        /// <param name="inputSize">The input size to get the maximum expected compressed length from.</param>
        /// <returns>A number representing the maximum compressed length for the provided input size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputSize"/> is negative.</exception>
        public static long GetMaxCompressedLength(long inputSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(inputSize);

            // ZLib's compressBound formula: inputSize + (inputSize >> 12) + (inputSize >> 14) + (inputSize >> 25) + 13
            // We use a conservative estimate
            return inputSize + (inputSize >> 12) + (inputSize >> 14) + (inputSize >> 25) + 18;
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
        /// Tries to compress a source byte span into a destination span using the default compression level.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return TryCompress(source, destination, out bytesWritten, CompressionLevel.Optimal);
        }

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
            ValidateCompressionLevel(compressionLevel);

            using var encoder = new DeflateEncoder(compressionLevel);
            OperationStatus status = encoder.Compress(source, destination, out int consumed, out bytesWritten, isFinalBlock: true);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}
