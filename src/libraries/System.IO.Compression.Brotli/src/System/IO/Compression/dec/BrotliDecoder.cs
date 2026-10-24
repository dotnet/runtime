// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.IO.Compression
{
    /// <summary>Provides non-allocating, performant Brotli decompression methods. The methods decompress in a single pass without using a <see cref="System.IO.Compression.BrotliStream" /> instance.</summary>
    public struct BrotliDecoder : IDisposable
    {
        private BrotliDecoderState? _state;
        private bool _disposed;

        internal void InitializeDecoder()
        {
            _state = new BrotliDecoderState();
        }

        internal void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_state == null)
                InitializeDecoder();
            else if (_state.IsDisposed) // disposed through a copy of this struct
                throw new ObjectDisposedException(nameof(BrotliDecoder), SR.BrotliDecoder_Disposed);
        }

        /// <summary>Releases all resources used by the current Brotli decoder instance.</summary>
        public void Dispose()
        {
            _disposed = true;
            _state?.Dispose();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BrotliDecoder), SR.BrotliDecoder_Disposed);
        }

        /// <summary>Decompresses data that was compressed using the Brotli algorithm.</summary>
        /// <param name="source">A buffer containing the compressed data.</param>
        /// <param name="destination">When this method returns, a byte span containing the decompressed data.</param>
        /// <param name="bytesConsumed">The total number of bytes that were read from <paramref name="source" />.</param>
        /// <param name="bytesWritten">The total number of bytes that were written in the <paramref name="destination" />.</param>
        /// <returns>One of the enumeration values that indicates the status of the decompression operation.</returns>
        /// <remarks>The return value can be as follows:
        /// - <see cref="System.Buffers.OperationStatus.Done" />: <paramref name="source" /> was successfully and completely decompressed into <paramref name="destination" />.
        /// - <see cref="System.Buffers.OperationStatus.DestinationTooSmall" />: There is not enough space in <paramref name="destination" /> to decompress <paramref name="source" />.
        /// - <see cref="System.Buffers.OperationStatus.NeedMoreData" />: The decompression action is partially done at least one more byte is required to complete the decompression task. This method should be called again with more input to decompress.
        /// - <see cref="System.Buffers.OperationStatus.InvalidData" />: The data in <paramref name="source" /> is invalid and could not be decompressed.</remarks>
        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            EnsureInitialized();
            Debug.Assert(_state != null);

            bytesConsumed = 0;
            bytesWritten = 0;
            if (_state.IsFinished)
                return OperationStatus.Done;

            while (destination.Length > 0)
            {
                BrotliDecoderResult result = _state.DecompressStream(source, destination, out int consumed, out int written);
                if (result == BrotliDecoderResult.Error)
                {
                    return OperationStatus.InvalidData;
                }

                Debug.Assert(consumed <= source.Length);
                Debug.Assert(written <= destination.Length);

                bytesConsumed += consumed;
                bytesWritten += written;

                switch (result)
                {
                    case BrotliDecoderResult.Success:
                        return OperationStatus.Done;
                    case BrotliDecoderResult.NeedsMoreOutput:
                        return OperationStatus.DestinationTooSmall;
                    case BrotliDecoderResult.NeedsMoreInput:
                    default:
                        source = source.Slice(consumed);
                        destination = destination.Slice(written);
                        if (result == BrotliDecoderResult.NeedsMoreInput && source.Length == 0)
                            return OperationStatus.NeedMoreData;
                        break;
                }
            }

            return OperationStatus.DestinationTooSmall;
        }

        /// <summary>Attempts to decompress data that was compressed with the Brotli algorithm.</summary>
        /// <param name="source">A buffer containing the compressed data.</param>
        /// <param name="destination">When this method returns, a byte span containing the decompressed data.</param>
        /// <param name="bytesWritten">The total number of bytes that were written in the <paramref name="destination" />.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> otherwise.</returns>
        /// <remarks>If this method returns <see langword="false" />, <paramref name="destination" /> may be empty or contain partially decompressed data, with <paramref name="bytesWritten" /> being zero or greater than zero but less than the expected total.</remarks>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            bool success = BrotliDecoderState.Decompress(source, destination, out bytesWritten);
            Debug.Assert(bytesWritten <= destination.Length);
            return success;
        }
    }
}
