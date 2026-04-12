// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>Initial buffer size for reading process output.</summary>
        private const int InitialReadAllBufferSize = 4096;

        /// <summary>
        /// Reads all standard output and standard error of the process as text.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the streams to be fully read.
        /// When <see langword="null" />, waits indefinitely.
        /// </param>
        /// <returns>
        /// A tuple containing the standard output and standard error text.
        /// When a stream was not redirected, the corresponding value is an empty string.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Neither standard output nor standard error has been redirected.
        /// -or-
        /// A redirected stream is already being read asynchronously.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation did not complete within the specified <paramref name="timeout" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The process has been disposed.
        /// </exception>
        public (string StandardOutput, string StandardError) ReadAllText(TimeSpan? timeout = default)
        {
            (byte[] StandardOutput, byte[] StandardError) bytes = ReadAllBytes(timeout);

            Encoding outputEncoding = _startInfo?.StandardOutputEncoding ?? GetStandardOutputEncoding();
            Encoding errorEncoding = _startInfo?.StandardErrorEncoding ?? GetStandardOutputEncoding();

            string standardOutput = bytes.StandardOutput.Length > 0
                ? outputEncoding.GetString(bytes.StandardOutput)
                : string.Empty;

            string standardError = bytes.StandardError.Length > 0
                ? errorEncoding.GetString(bytes.StandardError)
                : string.Empty;

            return (standardOutput, standardError);
        }

        /// <summary>
        /// Reads all standard output and standard error of the process as byte arrays.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the streams to be fully read.
        /// When <see langword="null" />, waits indefinitely.
        /// </param>
        /// <returns>
        /// A tuple containing the standard output and standard error bytes.
        /// When a stream was not redirected, the corresponding value is an empty array.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Neither standard output nor standard error has been redirected.
        /// -or-
        /// A redirected stream is already being read asynchronously.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation did not complete within the specified <paramref name="timeout" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The process has been disposed.
        /// </exception>
        public (byte[] StandardOutput, byte[] StandardError) ReadAllBytes(TimeSpan? timeout = default)
        {
            CheckDisposed();

            bool hasOutput = _standardOutput is not null;
            bool hasError = _standardError is not null;

            if (!hasOutput && !hasError)
            {
                throw new InvalidOperationException(SR.CantGetStandardOut);
            }

            if (hasOutput && _outputStreamReadMode == StreamReadMode.AsyncMode)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }

            if (hasError && _errorStreamReadMode == StreamReadMode.AsyncMode)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }

            if (hasOutput)
            {
                _outputStreamReadMode = StreamReadMode.SyncMode;
            }

            if (hasError)
            {
                _errorStreamReadMode = StreamReadMode.SyncMode;
            }

            int timeoutMs = timeout.HasValue
                ? (int)timeout.Value.TotalMilliseconds
                : -1; // Infinite

            byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            byte[] errorBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            int outputBytesRead = 0;
            int errorBytesRead = 0;

            try
            {
                SafeFileHandle? outputHandle = hasOutput ? GetSafeFileHandleFromStreamReader(_standardOutput!) : null;
                SafeFileHandle? errorHandle = hasError ? GetSafeFileHandleFromStreamReader(_standardError!) : null;

                ReadPipes(outputHandle, errorHandle, timeoutMs,
                    ref outputBuffer, ref outputBytesRead,
                    ref errorBuffer, ref errorBytesRead);

                byte[] outputResult = outputBytesRead > 0
                    ? outputBuffer.AsSpan(0, outputBytesRead).ToArray()
                    : Array.Empty<byte>();

                byte[] errorResult = errorBytesRead > 0
                    ? errorBuffer.AsSpan(0, errorBytesRead).ToArray()
                    : Array.Empty<byte>();

                return (outputResult, errorResult);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(outputBuffer);
                ArrayPool<byte>.Shared.Return(errorBuffer);
            }
        }

        /// <summary>
        /// Obtains the <see cref="SafeFileHandle"/> from the underlying stream of a <see cref="StreamReader"/>.
        /// On Unix, the stream is an <see cref="System.IO.Pipes.AnonymousPipeClientStream"/> and the handle is obtained via the pipe handle.
        /// On Windows, the stream is a <see cref="FileStream"/> opened for async IO.
        /// </summary>
        private static SafeFileHandle GetSafeFileHandleFromStreamReader(StreamReader reader)
        {
            Stream baseStream = reader.BaseStream;

            if (baseStream is FileStream fileStream)
            {
                return fileStream.SafeFileHandle;
            }

            if (baseStream is System.IO.Pipes.AnonymousPipeClientStream pipeStream)
            {
                return new SafeFileHandle(pipeStream.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Reads from one or both standard output and standard error pipes using platform-specific multiplexing.
        /// </summary>
        private static partial void ReadPipes(
            SafeFileHandle? outputHandle,
            SafeFileHandle? errorHandle,
            int timeoutMs,
            ref byte[] outputBuffer,
            ref int outputBytesRead,
            ref byte[] errorBuffer,
            ref int errorBytesRead);

        /// <summary>
        /// Rents a larger buffer from the array pool and copies the existing data to it.
        /// </summary>
        private static void RentLargerBuffer(ref byte[] buffer, int bytesRead)
        {
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, bytesRead);
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = newBuffer;
        }
    }
}
