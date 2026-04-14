// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Standard output or standard error has not been redirected.
        /// -or-
        /// A redirected stream has already been used for synchronous or asynchronous reading.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation did not complete within the specified <paramref name="timeout" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The process has been disposed.
        /// </exception>
        public (string StandardOutput, string StandardError) ReadAllText(TimeSpan? timeout = default)
        {
            ValidateReadAllState();

            byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            byte[] errorBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            int outputBytesRead = 0;
            int errorBytesRead = 0;

            try
            {
                ReadPipesToBuffers(timeout, ref outputBuffer, ref outputBytesRead, ref errorBuffer, ref errorBytesRead);

                Encoding outputEncoding = _startInfo?.StandardOutputEncoding ?? GetStandardOutputEncoding();
                Encoding errorEncoding = _startInfo?.StandardErrorEncoding ?? GetStandardOutputEncoding();

                string standardOutput = outputBytesRead > 0
                    ? outputEncoding.GetString(outputBuffer, 0, outputBytesRead)
                    : string.Empty;

                string standardError = errorBytesRead > 0
                    ? errorEncoding.GetString(errorBuffer, 0, errorBytesRead)
                    : string.Empty;

                return (standardOutput, standardError);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(outputBuffer);
                ArrayPool<byte>.Shared.Return(errorBuffer);
            }
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
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Standard output or standard error has not been redirected.
        /// -or-
        /// A redirected stream has already been used for synchronous or asynchronous reading.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation did not complete within the specified <paramref name="timeout" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The process has been disposed.
        /// </exception>
        public (byte[] StandardOutput, byte[] StandardError) ReadAllBytes(TimeSpan? timeout = default)
        {
            ValidateReadAllState();

            byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            byte[] errorBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            int outputBytesRead = 0;
            int errorBytesRead = 0;

            try
            {
                ReadPipesToBuffers(timeout, ref outputBuffer, ref outputBytesRead, ref errorBuffer, ref errorBytesRead);

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
        /// Validates that the process is not disposed, both stdout and stderr are redirected,
        /// and neither stream has been used (mode must be Undefined). Sets both streams to sync mode.
        /// </summary>
        private void ValidateReadAllState()
        {
            CheckDisposed();

            if (_standardOutput is null)
            {
                throw new InvalidOperationException(SR.CantGetStandardOut);
            }
            else if (_standardError is null)
            {
                throw new InvalidOperationException(SR.CantGetStandardError);
            }
            else if (_outputStreamReadMode != StreamReadMode.Undefined)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }
            else if (_errorStreamReadMode != StreamReadMode.Undefined)
            {
                throw new InvalidOperationException(SR.CantMixSyncAsyncOperation);
            }

            _outputStreamReadMode = StreamReadMode.SyncMode;
            _errorStreamReadMode = StreamReadMode.SyncMode;
        }

        /// <summary>
        /// Obtains handles and reads both stdout and stderr pipes into the provided buffers.
        /// The caller is responsible for calling <see cref="ValidateReadAllState"/> before renting buffers,
        /// and for renting and returning the buffers.
        /// </summary>
        private void ReadPipesToBuffers(
            TimeSpan? timeout,
            ref byte[] outputBuffer,
            ref int outputBytesRead,
            ref byte[] errorBuffer,
            ref int errorBytesRead)
        {
            int timeoutMs = timeout.HasValue
                ? ToTimeoutMilliseconds(timeout.Value)
                : Timeout.Infinite;

            SafeFileHandle outputHandle = GetSafeFileHandleFromStreamReader(_standardOutput!, out SafeHandle outputOwner);
            SafeFileHandle errorHandle = GetSafeFileHandleFromStreamReader(_standardError!, out SafeHandle errorOwner);

            bool outputRefAdded = false;
            bool errorRefAdded = false;

            try
            {
                outputOwner.DangerousAddRef(ref outputRefAdded);
                errorOwner.DangerousAddRef(ref errorRefAdded);

                ReadPipes(outputHandle, errorHandle, timeoutMs,
                    ref outputBuffer, ref outputBytesRead,
                    ref errorBuffer, ref errorBytesRead);
            }
            finally
            {
                if (outputRefAdded)
                {
                    outputOwner.DangerousRelease();
                }

                if (errorRefAdded)
                {
                    errorOwner.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Obtains the <see cref="SafeFileHandle"/> from the underlying stream of a <see cref="StreamReader"/>.
        /// On Unix, the stream is an <see cref="System.IO.Pipes.AnonymousPipeClientStream"/> and the handle is obtained via the pipe handle.
        /// On Windows, the stream is a <see cref="FileStream"/> opened for async IO.
        /// </summary>
        private static SafeFileHandle GetSafeFileHandleFromStreamReader(StreamReader reader, out SafeHandle owner)
        {
            Stream baseStream = reader.BaseStream;

            if (baseStream is FileStream fileStream)
            {
                owner = fileStream.SafeFileHandle;
                return fileStream.SafeFileHandle;
            }

            if (baseStream is System.IO.Pipes.AnonymousPipeClientStream pipeStream)
            {
                owner = pipeStream.SafePipeHandle;
                return new SafeFileHandle(pipeStream.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            }

            throw new UnreachableException();
        }

        /// <summary>
        /// Rents a larger buffer from the array pool and copies the existing data to it.
        /// </summary>
        private static void RentLargerBuffer(ref byte[] buffer, int bytesRead)
        {
            int newSize = (int)Math.Min((long)buffer.Length * 2, Array.MaxLength);
            newSize = Math.Max(buffer.Length + 1, newSize);
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, bytesRead);
            byte[] oldBuffer = buffer;
            buffer = newBuffer;
            ArrayPool<byte>.Shared.Return(oldBuffer);
        }

        private static bool TryGetRemainingTimeout(long deadline, int originalTimeout, out int remainingTimeoutMs)
        {
            if (originalTimeout < 0)
            {
                remainingTimeoutMs = Timeout.Infinite;
                return true;
            }

            long remaining = deadline - Environment.TickCount64;
            if (remaining <= 0)
            {
                remainingTimeoutMs = 0;
                return false;
            }

            remainingTimeoutMs = (int)Math.Min(remaining, int.MaxValue);
            return true;
        }
    }
}
