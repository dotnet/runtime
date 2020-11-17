// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    public partial struct BrotliEncoder : IDisposable
    {
        internal SafeBrotliEncoderHandle? _state;
        private bool _disposed;

        public BrotliEncoder(int quality, int window)
        {
            _disposed = false;
            _state = Interop.Brotli.BrotliEncoderCreateInstance(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_state.IsInvalid)
                throw new IOException(SR.BrotliEncoder_Create);
            SetQuality(quality);
            SetWindow(window);
        }

        /// <summary>
        /// Performs a lazy initialization of the native encoder using the default Quality and Window values:
        /// BROTLI_DEFAULT_WINDOW 22
        /// BROTLI_DEFAULT_QUALITY 11
        /// </summary>
        internal void InitializeEncoder()
        {
            EnsureNotDisposed();
            _state = Interop.Brotli.BrotliEncoderCreateInstance(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_state.IsInvalid)
                throw new IOException(SR.BrotliEncoder_Create);
        }

        internal void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_state == null)
            {
                InitializeEncoder();
            }
        }

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
            if (_state == null || _state.IsInvalid || _state.IsClosed)
            {
                InitializeEncoder();
                Debug.Assert(_state != null && !_state.IsInvalid && !_state.IsClosed);
            }
            if (quality < BrotliUtils.Quality_Min || quality > BrotliUtils.Quality_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), SR.Format(SR.BrotliEncoder_Quality, quality, 0, BrotliUtils.Quality_Max));
            }
            if (!Interop.Brotli.BrotliEncoderSetParameter(_state, BrotliEncoderParameter.Quality, (uint)quality))
            {
                throw new InvalidOperationException(SR.Format(SR.BrotliEncoder_InvalidSetParameter, "Quality"));
            }
        }

        internal void SetWindow(int window)
        {
            EnsureNotDisposed();
            if (_state == null || _state.IsInvalid || _state.IsClosed)
            {
                InitializeEncoder();
                Debug.Assert(_state != null && !_state.IsInvalid && !_state.IsClosed);
            }
            if (window < BrotliUtils.WindowBits_Min || window > BrotliUtils.WindowBits_Max)
            {
                throw new ArgumentOutOfRangeException(nameof(window), SR.Format(SR.BrotliEncoder_Window, window, BrotliUtils.WindowBits_Min, BrotliUtils.WindowBits_Max));
            }
            if (!Interop.Brotli.BrotliEncoderSetParameter(_state, BrotliEncoderParameter.LGWin, (uint)window))
            {
                throw new InvalidOperationException(SR.Format(SR.BrotliEncoder_InvalidSetParameter, "Window"));
            }
        }

        public static int GetMaxCompressedLength(int inputSize)
        {
            if (inputSize < 0 || inputSize > BrotliUtils.MaxInputSize)
            {
                throw new ArgumentOutOfRangeException(nameof(inputSize));
            }
            if (inputSize == 0)
                return 1;
            int numLargeBlocks = inputSize >> 24;
            int tail = inputSize & 0xFFFFFF;
            int tailOverhead = (tail > (1 << 20)) ? 4 : 3;
            int overhead = 2 + (4 * numLargeBlocks) + tailOverhead + 1;
            int result = inputSize + overhead;
            return result;
        }

        internal OperationStatus Flush(Memory<byte> destination, out int bytesWritten) => Flush(destination.Span, out bytesWritten);

        public OperationStatus Flush(Span<byte> destination, out int bytesWritten) => Compress(ReadOnlySpan<byte>.Empty, destination, out int bytesConsumed, out bytesWritten, BrotliEncoderOperation.Flush);

        internal OperationStatus Compress(ReadOnlyMemory<byte> source, Memory<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) => Compress(source.Span, destination.Span, out bytesConsumed, out bytesWritten, isFinalBlock);

        public OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) => Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock ? BrotliEncoderOperation.Finish : BrotliEncoderOperation.Process);

        internal OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, BrotliEncoderOperation operation)
        {
            EnsureInitialized();
            Debug.Assert(_state != null);

            bytesWritten = 0;
            bytesConsumed = 0;
            nuint availableOutput = (nuint)destination.Length;
            nuint availableInput = (nuint)source.Length;
            unsafe
            {
                // We can freely cast between int and nuint (.NET size_t equivalent) for two reasons:
                // 1. Interop Brotli functions will always return an availableInput/Output value lower or equal to the one passed to the function
                // 2. Span's have a maximum length of the int boundary.
                while ((int)availableOutput > 0)
                {
                    fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                    fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                    {
                        if (!Interop.Brotli.BrotliEncoderCompressStream(_state, operation, ref availableInput, &inBytes, ref availableOutput, &outBytes, out _))
                        {
                            return OperationStatus.InvalidData;
                        }

                        Debug.Assert(availableInput <= (nuint)source.Length);
                        Debug.Assert(availableOutput <= (nuint)destination.Length);

                        bytesConsumed += source.Length - (int)availableInput;
                        bytesWritten += destination.Length - (int)availableOutput;

                        // no bytes written, no remaining input to give to the encoder, and no output in need of retrieving means we are Done
                        if ((int)availableOutput == destination.Length && !Interop.Brotli.BrotliEncoderHasMoreOutput(_state) && availableInput == 0)
                        {
                            return OperationStatus.Done;
                        }

                        source = source.Slice(source.Length - (int)availableInput);
                        destination = destination.Slice(destination.Length - (int)availableOutput);
                    }
                }

                return OperationStatus.DestinationTooSmall;
            }
        }

        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => TryCompress(source, destination, out bytesWritten, BrotliUtils.Quality_Default, BrotliUtils.WindowBits_Default);

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

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    nuint availableOutput = (nuint)destination.Length;
                    bool success = Interop.Brotli.BrotliEncoderCompress(quality, window, /*BrotliEncoderMode*/ 0, (nuint)source.Length, inBytes, ref availableOutput, outBytes);

                    Debug.Assert(success ? availableOutput <= (nuint)destination.Length : availableOutput == 0);

                    bytesWritten = (int)availableOutput;
                    return success;
                }
            }
        }
    }
}
