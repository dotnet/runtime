// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides non-allocating, performant decompression methods for data compressed using the Deflate, ZLib, or GZip data format specification.
    /// </summary>
    public sealed class ZlibDecoder : IDisposable
    {
        private ZLibNative.ZLibStreamHandle? _state;
        private bool _disposed;
        private bool _finished;

        // Store construction parameters for Reset()
        private readonly int _windowBits;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibDecoder"/> class using the specified format.
        /// </summary>
        /// <param name="format">The compression format to decompress.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a valid <see cref="ZlibCompressionFormat"/> value.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="ZlibDecoder"/> instance.</exception>
        public ZlibDecoder(ZlibCompressionFormat format)
        {
            _disposed = false;
            _finished = false;
            _windowBits = GetWindowBits(format);

            _state = ZLibNative.ZLibStreamHandle.CreateForInflate(_windowBits);
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
                throw new InvalidOperationException(SR.ZlibDecoder_NotInitialized);
            }
        }

        /// <summary>
        /// Decompresses data that was compressed using the Deflate, ZLib, or GZip algorithm.
        /// </summary>
        /// <param name="source">A buffer containing the compressed data.</param>
        /// <param name="destination">When this method returns, a byte span containing the decompressed data.</param>
        /// <param name="bytesConsumed">The total number of bytes that were read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">The total number of bytes that were written in the <paramref name="destination"/>.</param>
        /// <returns>One of the enumeration values that indicates the status of the decompression operation.</returns>
        /// <remarks>
        /// The return value can be as follows:
        /// - <see cref="OperationStatus.Done"/>: <paramref name="source"/> was successfully and completely decompressed into <paramref name="destination"/>.
        /// - <see cref="OperationStatus.DestinationTooSmall"/>: There is not enough space in <paramref name="destination"/> to decompress <paramref name="source"/>.
        /// - <see cref="OperationStatus.NeedMoreData"/>: The decompression action is partially done. At least one more byte is required to complete the decompression task.
        /// - <see cref="OperationStatus.InvalidData"/>: The data in <paramref name="source"/> is invalid and could not be decompressed.
        /// </remarks>
        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            EnsureInitialized();
            Debug.Assert(_state is not null);

            bytesConsumed = 0;
            bytesWritten = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            if (source.IsEmpty)
            {
                return OperationStatus.NeedMoreData;
            }

            unsafe
            {
                fixed (byte* inputPtr = &MemoryMarshal.GetReference(source))
                fixed (byte* outputPtr = &MemoryMarshal.GetReference(destination))
                {
                    _state.NextIn = (IntPtr)inputPtr;
                    _state.AvailIn = (uint)source.Length;
                    _state.NextOut = (IntPtr)outputPtr;
                    _state.AvailOut = (uint)destination.Length;

                    ZLibNative.ErrorCode errorCode = _state.Inflate(ZLibNative.FlushCode.NoFlush);

                    bytesConsumed = source.Length - (int)_state.AvailIn;
                    bytesWritten = destination.Length - (int)_state.AvailOut;

                    OperationStatus status = errorCode switch
                    {
                        ZLibNative.ErrorCode.Ok => _state.AvailOut == 0
                            ? OperationStatus.DestinationTooSmall
                            : _state.AvailIn == 0
                                ? OperationStatus.NeedMoreData
                                : OperationStatus.Done,
                        ZLibNative.ErrorCode.StreamEnd => OperationStatus.Done,
                        ZLibNative.ErrorCode.BufError => _state.AvailOut == 0
                            ? OperationStatus.DestinationTooSmall
                            : OperationStatus.NeedMoreData,
                        ZLibNative.ErrorCode.DataError => OperationStatus.InvalidData,
                        _ => OperationStatus.InvalidData
                    };

                    // Track if decompression is finished
                    if (errorCode == ZLibNative.ErrorCode.StreamEnd)
                    {
                        _finished = true;
                    }

                    return status;
                }
            }
        }

        /// <summary>
        /// Resets the decoder to its initial state, allowing it to be reused for a new decompression operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
        public void Reset()
        {
            EnsureNotDisposed();

            _finished = false;

            // Dispose the old state and create a new one
            _state?.Dispose();
            _state = ZLibNative.ZLibStreamHandle.CreateForInflate(_windowBits);
        }

        /// <summary>
        /// Attempts to decompress data.
        /// </summary>
        /// <param name="source">A buffer containing the compressed data.</param>
        /// <param name="destination">When this method returns, a byte span containing the decompressed data.</param>
        /// <param name="bytesWritten">The total number of bytes that were written in the <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> on success; <see langword="false"/> otherwise.</returns>
        /// <remarks>If this method returns <see langword="false"/>, <paramref name="destination"/> may be empty or contain partially decompressed data, with <paramref name="bytesWritten"/> being zero or greater than zero but less than the expected total.</remarks>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return TryDecompress(source, destination, out bytesWritten, ZlibCompressionFormat.Deflate);
        }

        /// <summary>
        /// Attempts to decompress data using the specified format.
        /// </summary>
        /// <param name="source">A buffer containing the compressed data.</param>
        /// <param name="destination">When this method returns, a byte span containing the decompressed data.</param>
        /// <param name="bytesWritten">The total number of bytes that were written in the <paramref name="destination"/>.</param>
        /// <param name="format">The compression format to decompress.</param>
        /// <returns><see langword="true"/> on success; <see langword="false"/> otherwise.</returns>
        /// <remarks>If this method returns <see langword="false"/>, <paramref name="destination"/> may be empty or contain partially decompressed data, with <paramref name="bytesWritten"/> being zero or greater than zero but less than the expected total.</remarks>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, ZlibCompressionFormat format)
        {
            using var decoder = new ZlibDecoder(format);
            OperationStatus status = decoder.Decompress(source, destination, out int consumed, out bytesWritten);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}
