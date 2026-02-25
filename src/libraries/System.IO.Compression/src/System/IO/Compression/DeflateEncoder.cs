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

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the default quality.
        /// </summary>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder()
            : this(ZLibNative.DefaultQuality)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the specified quality.
        /// </summary>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not in the valid range (0-9).</exception>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder(int quality)
            : this(quality, ZLibNative.DefaultWindowLog)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the specified options.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder(ZLibCompressionOptions options)
            : this(options, CompressionFormat.Deflate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateEncoder"/> class using the specified quality and window size.
        /// </summary>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <param name="windowLog">The base-2 logarithm of the window size (8-15). Larger values result in better compression at the expense of memory usage.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not in the valid range (0-9), or <paramref name="windowLog"/> is not in the valid range (8-15).</exception>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateEncoder"/> instance.</exception>
        public DeflateEncoder(int quality, int windowLog)
            : this(quality, windowLog, CompressionFormat.Deflate)
        {
        }

        /// <summary>
        /// Internal constructor that accepts quality, windowLog (8-15), and format.
        /// Validates both parameters and transforms windowLog to windowBits based on format.
        /// </summary>
        internal DeflateEncoder(int quality, int windowLog, CompressionFormat format)
        {
            ValidateQuality(quality);
            ValidateWindowLog(windowLog);

            _disposed = false;
            _finished = false;

            // Compute windowBits based on the compression format:
            // - Deflate: negative windowLog produces raw deflate (no header/trailer)
            // - ZLib: positive windowLog produces zlib format
            // - GZip: windowLog + 16 produces gzip format
            int windowBits = format switch
            {
                CompressionFormat.Deflate => -windowLog,
                CompressionFormat.ZLib => windowLog,
                CompressionFormat.GZip => windowLog + 16,
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };

            _state = ZLibNative.ZLibStreamHandle.CreateForDeflate(
                (ZLibNative.CompressionLevel)quality,
                windowBits,
                ZLibNative.Deflate_DefaultMemLevel,
                ZLibNative.CompressionStrategy.DefaultStrategy);
        }

        /// <summary>
        /// Internal constructor that accepts ZLibCompressionOptions and format.
        /// </summary>
        internal DeflateEncoder(ZLibCompressionOptions options, CompressionFormat format)
        {
            ArgumentNullException.ThrowIfNull(options);

            _disposed = false;
            _finished = false;

            // -1 means use the default window log
            int windowLog = options.WindowLog == -1 ? ZLibNative.DefaultWindowLog : options.WindowLog;

            // Compute windowBits based on the compression format:
            int windowBits = format switch
            {
                CompressionFormat.Deflate => -windowLog,
                CompressionFormat.ZLib => windowLog,
                CompressionFormat.GZip => windowLog + 16,
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };

            _state = ZLibNative.ZLibStreamHandle.CreateForDeflate(
                (ZLibNative.CompressionLevel)options.CompressionLevel,
                windowBits,
                ZLibNative.Deflate_DefaultMemLevel,
                (ZLibNative.CompressionStrategy)options.CompressionStrategy);
        }

        private static void ValidateQuality(int quality)
        {
            if (quality != -1)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(quality, ZLibNative.MinQuality, nameof(quality));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, ZLibNative.MaxQuality, nameof(quality));
            }
        }

        private static void ValidateWindowLog(int windowLog)
        {
            if (windowLog != -1)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(windowLog, ZLibNative.MinWindowLog, nameof(windowLog));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(windowLog, ZLibNative.MaxWindowLog, nameof(windowLog));
            }
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

        /// <summary>
        /// Gets the maximum expected compressed length for the provided input size.
        /// </summary>
        /// <param name="inputLength">The input size to get the maximum expected compressed length from.</param>
        /// <returns>A number representing the maximum compressed length for the provided input size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputLength"/> is negative or exceeds <see cref="uint.MaxValue"/>.</exception>
        public static long GetMaxCompressedLength(long inputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(inputLength);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(inputLength, uint.MaxValue);

            return (long)Interop.ZLib.compressBound((uint)inputLength);
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
            EnsureNotDisposed();
            Debug.Assert(_state is not null);

            bytesConsumed = 0;
            bytesWritten = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            if (source.IsEmpty && !isFinalBlock)
            {
                return OperationStatus.Done;
            }

            if (destination.IsEmpty && (source.Length > 0 || isFinalBlock))
            {
                return OperationStatus.DestinationTooSmall;
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
                        ZLibNative.ErrorCode.Ok when isFinalBlock => OperationStatus.DestinationTooSmall,
                        ZLibNative.ErrorCode.Ok => _state.AvailIn == 0
                            ? OperationStatus.Done
                            : OperationStatus.DestinationTooSmall,
                        ZLibNative.ErrorCode.StreamEnd => OperationStatus.Done,
                        ZLibNative.ErrorCode.BufError => _state.AvailOut == 0
                            ? OperationStatus.DestinationTooSmall
                            : OperationStatus.Done,
                        _ => throw new ZLibException(SR.ZLibErrorUnexpected, "deflate", (int)errorCode, _state.GetErrorMessage())
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
            EnsureNotDisposed();
            Debug.Assert(_state is not null);

            bytesWritten = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            unsafe
            {
                fixed (byte* outputPtr = &MemoryMarshal.GetReference(destination))
                {
                    _state.NextIn = IntPtr.Zero;
                    _state.AvailIn = 0;
                    _state.NextOut = (IntPtr)outputPtr;
                    _state.AvailOut = (uint)destination.Length;

                    ZLibNative.ErrorCode errorCode = _state.Deflate(ZLibNative.FlushCode.SyncFlush);

                    bytesWritten = destination.Length - (int)_state.AvailOut;

                    return errorCode switch
                    {
                        ZLibNative.ErrorCode.Ok => OperationStatus.Done,
                        ZLibNative.ErrorCode.StreamEnd => OperationStatus.Done,
                        ZLibNative.ErrorCode.BufError => _state.AvailOut == 0
                            ? OperationStatus.DestinationTooSmall
                            : OperationStatus.Done,
                        _ => throw new ZLibException(SR.ZLibErrorUnexpected, "deflate", (int)errorCode, _state.GetErrorMessage())
                    };
                }
            }
        }

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the default quality.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            => TryCompress(source, destination, out bytesWritten, ZLibNative.DefaultQuality, ZLibNative.DefaultWindowLog);

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the specified quality.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality)
            => TryCompress(source, destination, out bytesWritten, quality, ZLibNative.DefaultWindowLog);

        /// <summary>
        /// Tries to compress a source byte span into a destination span using the specified quality and window size.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="quality">The compression quality value between 0 (no compression) and 9 (maximum compression).</param>
        /// <param name="windowLog">The base-2 logarithm of the window size (8-15). Larger values result in better compression at the expense of memory usage.</param>
        /// <returns><see langword="true"/> if the compression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog)
        {
            using var encoder = new DeflateEncoder(quality, windowLog);
            OperationStatus status = encoder.Compress(source, destination, out int consumed, out bytesWritten, isFinalBlock: true);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}
