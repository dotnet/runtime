// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using size_t = System.IntPtr;

namespace System.IO.Compression
{
    public struct BrotliDecoder : IDisposable
    {
        private SafeBrotliDecoderHandle _state;
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
            bytesConsumed = 0;
            bytesWritten = 0;
            if (Interop.Brotli.BrotliDecoderIsFinished(_state))
                return OperationStatus.Done;
            size_t availableOutput = (size_t)destination.Length;
            size_t availableInput = (size_t)source.Length;
            unsafe
            {
                // We can freely cast between int and size_t for two reasons: 
                // 1. Interop Brotli functions will always return an availableInput/Output value lower or equal to the one passed to the function
                // 2. Span's have a maximum length of the int boundary.
                while ((int)availableOutput > 0)
                {
                    fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                    fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                    {
                        int brotliResult = Interop.Brotli.BrotliDecoderDecompressStream(_state, ref availableInput, &inBytes, ref availableOutput, &outBytes, out size_t totalOut);
                        if (brotliResult == 0) // Error
                        {
                            return OperationStatus.InvalidData;
                        }
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

        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    size_t availableOutput = (size_t)destination.Length;
                    bool success = Interop.Brotli.BrotliDecoderDecompress((size_t)source.Length, inBytes, ref availableOutput, outBytes);
                    bytesWritten = (int)availableOutput;
                    return success;
                }
            }
        }
    }
}
