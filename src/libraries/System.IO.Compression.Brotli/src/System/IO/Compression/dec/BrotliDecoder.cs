// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides non-allocating, performant Brotli decompression methods. The methods decompress in a single pass without using a <see cref="System.IO.Compression.BrotliStream" /> instance.</summary>
    public struct BrotliDecoder : IDisposable
    {
        private SafeBrotliDecoderHandle? _state;
        private bool _disposed;

        internal void InitializeDecoder()
        {
            _state = Interop.Brotli.BrotliDecoderCreateInstance(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_state.IsInvalid)
                throw new IOException(SR.BrotliDecoder_Create);
        }

        internal void EnsureInitialized()
        {
            EnsureNotDisposed();
            if (_state == null)
                InitializeDecoder();
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
            if (Interop.Brotli.BrotliDecoderIsFinished(_state) != Interop.BOOL.FALSE)
                return OperationStatus.Done;
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
                        int brotliResult = Interop.Brotli.BrotliDecoderDecompressStream(_state, ref availableInput, &inBytes, ref availableOutput, &outBytes, out _);
                        if (brotliResult == 0) // Error
                        {
                            return OperationStatus.InvalidData;
                        }

                        Debug.Assert(availableInput <= (nuint)source.Length);
                        Debug.Assert(availableOutput <= (nuint)destination.Length);

                        bytesConsumed += source.Length - (int)availableInput;
                        bytesWritten += destination.Length - (int)availableOutput;

                        switch (brotliResult)
                        {
                            case 1: // Success
                                return OperationStatus.Done;
                            case 3: // NeedsMoreOutput
                                return OperationStatus.DestinationTooSmall;
                            case 2: // NeedsMoreInput
                            default:
                                source = source.Slice(source.Length - (int)availableInput);
                                destination = destination.Slice(destination.Length - (int)availableOutput);
                                if (brotliResult == 2 && source.Length == 0)
                                    return OperationStatus.NeedMoreData;
                                break;
                        }
                    }
                }
                return OperationStatus.DestinationTooSmall;
            }
        }

        /// <summary>Attempts to decompress data that was compressed with the Brotli algorithm.</summary>
        /// <param name="source">A buffer containing the compressed data.</param>
        /// <param name="destination">When this method returns, a byte span containing the decompressed data.</param>
        /// <param name="bytesWritten">The total number of bytes that were written in the <paramref name="destination" />.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> otherwise.</returns>
        /// <remarks>If this method returns <see langword="false" />, <paramref name="destination" /> may be empty or contain partially decompressed data, with <paramref name="bytesWritten" /> being zero or greater than zero but less than the expected total.</remarks>
        public static unsafe bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
            {
                nuint availableOutput = (nuint)destination.Length;
                bool success = Interop.Brotli.BrotliDecoderDecompress((nuint)source.Length, inBytes, &availableOutput, outBytes) != Interop.BOOL.FALSE;

                Debug.Assert(success ? availableOutput <= (nuint)destination.Length : availableOutput == 0);

                bytesWritten = (int)availableOutput;
                return success;
            }
        }
    }
}
