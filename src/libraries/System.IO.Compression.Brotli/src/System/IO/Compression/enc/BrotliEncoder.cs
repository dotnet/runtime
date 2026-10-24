// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.IO.Compression
{
    /// <summary>Provides methods and static methods to encode and decode data in a streamless, non-allocating, and performant manner using the Brotli data format specification.</summary>
    public partial struct BrotliEncoder : IDisposable
    {
        internal BrotliEncoderState? _state;
        private bool _disposed;

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.BrotliEncoder" /> structure using the specified quality and window.</summary>
        /// <param name="quality">A number representing quality of the Brotli compression. 0 is the minimum (no compression), 11 is the maximum.</param>
        /// <param name="window">A number representing the encoder window bits. The minimum value is 10, and the maximum value is 24.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="quality" /> is not between the minimum value of 0 and the maximum value of 11.
        /// -or-
        /// <paramref name="window" /> is not between the minimum value of 10 and the maximum value of 24.</exception>
        public BrotliEncoder(int quality, int window)
        {
            _disposed = false;
            _state = new BrotliEncoderState();
            SetQuality(quality);
            SetWindow(window);
        }

        /// <summary>
        /// Performs a lazy initialization of the encoder using the default Quality and Window values:
        /// BROTLI_DEFAULT_WINDOW 22
        /// BROTLI_DEFAULT_QUALITY 11
        /// </summary>
        internal void InitializeEncoder()
        {
            EnsureNotDisposed();
            _state = new BrotliEncoderState();
        }

        internal void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_state == null)
            {
                InitializeEncoder();
            }
            else if (_state.IsDisposed) // disposed through a copy of this struct
            {
                throw new ObjectDisposedException(nameof(BrotliEncoder), SR.BrotliEncoder_Disposed);
            }
        }

        /// <summary>Releases all resources used by the current Brotli encoder instance.</summary>
        public void Dispose()
        {
            _disposed = true;
            _state?.Dispose();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BrotliEncoder), SR.BrotliEncoder_Disposed);
        }

        internal void SetQuality(int quality)
        {
            EnsureNotDisposed();
            if (_state == null || _state.IsDisposed)
            {
                InitializeEncoder();
                Debug.Assert(_state != null && !_state.IsDisposed);
            }
            if (quality < BrotliUtils.Quality_Min || quality > BrotliUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), SR.Format(SR.BrotliEncoder_Quality, quality, 0, BrotliUtils.Quality_Max));
            }
            if (!_state.SetParameter(BrotliEncoderParameter.Quality, (uint)quality))
            {
                throw new InvalidOperationException(SR.Format(SR.BrotliEncoder_InvalidSetParameter, "Quality"));
            }
        }

        internal void SetWindow(int window)
        {
            EnsureNotDisposed();
            if (_state == null || _state.IsDisposed)
            {
                InitializeEncoder();
                Debug.Assert(_state != null && !_state.IsDisposed);
            }
            if (window < BrotliUtils.WindowBits_Min || window > BrotliUtils.WindowBits_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(window), SR.Format(SR.BrotliEncoder_Window, window, BrotliUtils.WindowBits_Min, BrotliUtils.WindowBits_Max));
            }
            if (!_state.SetParameter(BrotliEncoderParameter.LGWin, (uint)window))
            {
                throw new InvalidOperationException(SR.Format(SR.BrotliEncoder_InvalidSetParameter, "Window"));
            }
        }

        /// <summary>Gets the maximum expected compressed length for the provided input size.</summary>
        /// <param name="inputSize">The input size to get the maximum expected compressed length from. Must be greater than or equal to 0 and small enough that the result is less than or equal to <see cref="int.MaxValue"/>.</param>
        /// <returns>A number representing the maximum compressed length for the provided input size.</returns>
        /// <remarks>Returns 2 if <paramref name="inputSize" /> is 0.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="inputSize" /> is less than 0, the minimum allowed input size, or large enough that the result would be larger than <see cref="int.MaxValue"/>.</exception>
        public static int GetMaxCompressedLength(int inputSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(inputSize);

            nuint result = BrotliEncoderState.MaxCompressedSize((nuint)inputSize);
            if (result > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(inputSize));
            }

            return (int)result;
        }

        internal OperationStatus Flush(Memory<byte> destination, out int bytesWritten) => Flush(destination.Span, out bytesWritten);

        /// <summary>Compresses an empty read-only span of bytes into its destination, which ensures that output is produced for all the processed input. An actual flush is performed when the source is depleted and there is enough space in the destination for the remaining data.</summary>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data will be stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination" />.</param>
        /// <returns>One of the enumeration values that describes the status with which the operation finished.</returns>
        public OperationStatus Flush(Span<byte> destination, out int bytesWritten) => Compress(ReadOnlySpan<byte>.Empty, destination, out _, out bytesWritten, BrotliEncoderOperation.Flush);

        internal OperationStatus Compress(ReadOnlyMemory<byte> source, Memory<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) => Compress(source.Span, destination.Span, out bytesConsumed, out bytesWritten, isFinalBlock);

        /// <summary>Compresses a read-only byte span into a destination span.</summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a byte span where the compressed is stored.</param>
        /// <param name="bytesConsumed">When this method returns, the total number of bytes that were read from <paramref name="source" />.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination" />.</param>
        /// <param name="isFinalBlock"><see langword="true" /> to finalize the internal stream, which prevents adding more input data when this method returns; <see langword="false" /> to allow the encoder to postpone the production of output until it has processed enough input.</param>
        /// <returns>One of the enumeration values that describes the status with which the span-based operation finished.</returns>
        public OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) => Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock ? BrotliEncoderOperation.Finish : BrotliEncoderOperation.Process);

        internal OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, BrotliEncoderOperation operation)
        {
            EnsureInitialized();
            Debug.Assert(_state != null);

            bytesWritten = 0;
            bytesConsumed = 0;
            while (destination.Length > 0)
            {
                if (!_state.CompressStream(operation, source, destination, out int consumed, out int written))
                {
                    return OperationStatus.InvalidData;
                }

                Debug.Assert(consumed <= source.Length);
                Debug.Assert(written <= destination.Length);

                bytesConsumed += consumed;
                bytesWritten += written;

                // no bytes written, no remaining input to give to the encoder, and no output in need of retrieving means we are Done
                if (written == 0 && !_state.HasMoreOutput && consumed == source.Length)
                {
                    return OperationStatus.Done;
                }

                source = source.Slice(consumed);
                destination = destination.Slice(written);
            }

            return OperationStatus.DestinationTooSmall;
        }

        /// <summary>Tries to compress a source byte span into a destination span.</summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination" />.</param>
        /// <returns><see langword="true" /> if the compression operation was successful; <see langword="false" /> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => TryCompress(source, destination, out bytesWritten, BrotliUtils.Quality_Default, BrotliUtils.WindowBits_Default);

        /// <summary>Tries to compress a source byte span into a destination byte span, using the provided compression quality leven and encoder window bits.</summary>
        /// <param name="source">A read-only span of bytes containing the source data to compress.</param>
        /// <param name="destination">When this method returns, a span of bytes where the compressed data is stored.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes that were written to <paramref name="destination" />.</param>
        /// <param name="quality">A number representing quality of the Brotli compression. 0 is the minimum (no compression), 11 is the maximum.</param>
        /// <param name="window">A number representing the encoder window bits. The minimum value is 10, and the maximum value is 24.</param>
        /// <returns><see langword="true" /> if the compression operation was successful; <see langword="false" /> otherwise.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int window)
        {
            if (quality < 0 || quality > BrotliUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), SR.Format(SR.BrotliEncoder_Quality, quality, 0, BrotliUtils.Quality_Max));
            }
            if (window < BrotliUtils.WindowBits_Min || window > BrotliUtils.WindowBits_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(window), SR.Format(SR.BrotliEncoder_Window, window, BrotliUtils.WindowBits_Min, BrotliUtils.WindowBits_Max));
            }

            bool success = BrotliEncoderState.Compress(quality, window, EncoderMode.Generic, source, destination, out bytesWritten);
            Debug.Assert(success ? bytesWritten <= destination.Length : bytesWritten == 0);
            return success;
        }
    }
}
