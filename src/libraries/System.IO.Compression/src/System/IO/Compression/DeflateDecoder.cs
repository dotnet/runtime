// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides methods and static methods to decode data compressed in the Deflate data format in a streamless, non-allocating, and performant manner.
    /// </summary>
    public sealed class DeflateDecoder : IDisposable
    {
        private ZLibNative.ZLibStreamHandle? _state;
        private bool _disposed;
        private bool _finished;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateDecoder"/> class.
        /// </summary>
        /// <exception cref="IOException">Failed to create the <see cref="DeflateDecoder"/> instance.</exception>
        public DeflateDecoder()
            : this(ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        internal DeflateDecoder(int windowBits)
        {
            _disposed = false;
            _finished = false;
            _state = ZLibNative.ZLibStreamHandle.CreateForInflate(windowBits);
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
        /// Decompresses a read-only byte span into a destination span.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the compressed source data.</param>
        /// <param name="destination">When this method returns, a byte span where the decompressed data is stored.</param>
        /// <param name="bytesConsumed">When this method returns, the total number of bytes that were read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns>One of the enumeration values that describes the status with which the span-based operation finished.</returns>
        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            EnsureNotDisposed();
            Debug.Assert(_state is not null);

            bytesConsumed = 0;
            bytesWritten = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            if (destination.IsEmpty && source.Length > 0)
            {
                return OperationStatus.DestinationTooSmall;
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
                        ZLibNative.ErrorCode.Ok => _state.AvailIn == 0 && _state.AvailOut > 0
                            ? OperationStatus.NeedMoreData
                            : _state.AvailOut == 0
                                ? OperationStatus.DestinationTooSmall
                                : OperationStatus.NeedMoreData,
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
        /// Tries to decompress a source byte span into a destination span.
        /// </summary>
        /// <param name="source">A read-only span of bytes containing the compressed source data.</param>
        /// <param name="destination">When this method returns, a span of bytes where the decompressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the decompression operation was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            using var decoder = new DeflateDecoder();
            OperationStatus status = decoder.Decompress(source, destination, out int consumed, out bytesWritten);

            return status == OperationStatus.Done && consumed == source.Length;
        }
    }
}
