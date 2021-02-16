// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib decompression API.
    /// </summary>
    internal sealed class Inflater : IDisposable
    {
        private readonly ZLibNative.ZLibStreamHandle _handle;

        internal Inflater(int windowBits)
        {
            ZLibNative.ErrorCode error;
            try
            {
                error = ZLibNative.CreateZLibStreamForInflate(out _handle, windowBits);
            }
            catch (Exception exception) // could not load the ZLib dll
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, exception);
            }

            switch (error)
            {
                case ZLibNative.ErrorCode.Ok:           // Successful initialization
                    return;

                case ZLibNative.ErrorCode.MemError:     // Not enough memory
                    throw new WebSocketException(SR.ZLibErrorNotEnoughMemory);

                case ZLibNative.ErrorCode.VersionError: //zlib library is incompatible with the version assumed
                    throw new WebSocketException(SR.ZLibErrorVersionMismatch);

                case ZLibNative.ErrorCode.StreamError:  // Parameters are invalid
                    throw new WebSocketException(SR.ZLibErrorIncorrectInitParameters);

                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)error));
            }
        }

        internal unsafe void Inflate(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written)
        {
            fixed (byte* fixedInput = input)
            fixed (byte* fixedOutput = &MemoryMarshal.GetReference(output))
            {
                _handle.NextIn = (IntPtr)fixedInput;
                _handle.AvailIn = (uint)input.Length;

                _handle.NextOut = (IntPtr)fixedOutput;
                _handle.AvailOut = (uint)output.Length;

                Inflate(ZLibNative.FlushCode.NoFlush);

                consumed = input.Length - (int)_handle.AvailIn;
                written = output.Length - (int)_handle.AvailOut;
            }
        }

        public void Dispose() => _handle.Dispose();

        /// <summary>
        /// Wrapper around the ZLib inflate function
        /// </summary>
        private ZLibNative.ErrorCode Inflate(ZLibNative.FlushCode flushCode)
        {
            ZLibNative.ErrorCode errorCode;
            try
            {
                errorCode = _handle.Inflate(flushCode);
            }
            catch (Exception cause) // could not load the Zlib DLL correctly
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
            }
            switch (errorCode)
            {
                case ZLibNative.ErrorCode.Ok:           // progress has been made inflating
                case ZLibNative.ErrorCode.StreamEnd:    // The end of the input stream has been reached
                    return errorCode;

                case ZLibNative.ErrorCode.BufError:     // No room in the output buffer - inflate() can be called again with more space to continue
                    return errorCode;

                case ZLibNative.ErrorCode.MemError:     // Not enough memory to complete the operation
                    throw new WebSocketException(SR.ZLibErrorNotEnoughMemory);

                case ZLibNative.ErrorCode.DataError:    // The input data was corrupted (input stream not conforming to the zlib format or incorrect check value)
                    throw new WebSocketException(SR.UnsupportedCompression);

                case ZLibNative.ErrorCode.StreamError:  //the stream structure was inconsistent (for example if next_in or next_out was NULL),
                    throw new WebSocketException(SR.ZLibErrorInconsistentStream);

                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)errorCode));
            }
        }
    }
}
