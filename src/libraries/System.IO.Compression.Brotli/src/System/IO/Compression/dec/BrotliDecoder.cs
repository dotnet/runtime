// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
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

        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            EnsureInitialized();
            Debug.Assert(_state != null);

            bytesConsumed = 0;
            bytesWritten = 0;
            if (Interop.Brotli.BrotliDecoderIsFinished(_state))
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

        public static unsafe bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
            {
                nuint availableOutput = (nuint)destination.Length;
                bool success = Interop.Brotli.BrotliDecoderDecompress((nuint)source.Length, inBytes, ref availableOutput, outBytes);

                Debug.Assert(success ? availableOutput <= (nuint)destination.Length : availableOutput == 0);

                bytesWritten = (int)availableOutput;
                return success;
            }
        }
    }
}
