// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

                string standardOutput = outputEncoding.GetString(outputBuffer.AsSpan(0, outputBytesRead));
                string standardError = errorEncoding.GetString(errorBuffer.AsSpan(0, errorBytesRead));

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

                byte[] outputResult = outputBuffer.AsSpan(0, outputBytesRead).ToArray();
                byte[] errorResult = errorBuffer.AsSpan(0, errorBytesRead).ToArray();

                return (outputResult, errorResult);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(outputBuffer);
                ArrayPool<byte>.Shared.Return(errorBuffer);
            }
        }

        /// <summary>
        /// Asynchronously reads all standard output and standard error of the process as text.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the task contains
        /// a tuple with the standard output and standard error text.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Standard output or standard error has not been redirected.
        /// -or-
        /// A redirected stream has already been used for synchronous or asynchronous reading.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The <paramref name="cancellationToken" /> was canceled.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The process has been disposed.
        /// </exception>
        public async Task<(string StandardOutput, string StandardError)> ReadAllTextAsync(CancellationToken cancellationToken = default)
        {
            ValidateReadAllState();

            (ArraySegment<byte> output, ArraySegment<byte> error) = await ReadPipesToBuffersAsync(
                _standardOutput!.BaseStream, _standardError!.BaseStream, cancellationToken).ConfigureAwait(false);

            Debug.Assert(output.Array is not null && error.Array is not null);

            try
            {

                Encoding outputEncoding = _startInfo?.StandardOutputEncoding ?? GetStandardOutputEncoding();
                Encoding errorEncoding = _startInfo?.StandardErrorEncoding ?? GetStandardOutputEncoding();

                string standardOutput = outputEncoding.GetString(output.AsSpan());
                string standardError = errorEncoding.GetString(error.AsSpan());

                return (standardOutput, standardError);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(output.Array);
                ArrayPool<byte>.Shared.Return(error.Array);
            }
        }

        /// <summary>
        /// Asynchronously reads all standard output and standard error of the process as byte arrays.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the task contains
        /// a tuple with the standard output and standard error bytes.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Standard output or standard error has not been redirected.
        /// -or-
        /// A redirected stream has already been used for synchronous or asynchronous reading.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The <paramref name="cancellationToken" /> was canceled.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The process has been disposed.
        /// </exception>
        public async Task<(byte[] StandardOutput, byte[] StandardError)> ReadAllBytesAsync(CancellationToken cancellationToken = default)
        {
            ValidateReadAllState();

            (ArraySegment<byte> output, ArraySegment<byte> error) = await ReadPipesToBuffersAsync(
                _standardOutput!.BaseStream, _standardError!.BaseStream, cancellationToken).ConfigureAwait(false);

            Debug.Assert(output.Array is not null && error.Array is not null);

            try
            {
                return (output.AsSpan().ToArray(), error.AsSpan().ToArray());
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(output.Array);
                ArrayPool<byte>.Shared.Return(error.Array);
            }
        }

        /// <summary>
        /// Asynchronously reads from both standard output and standard error streams into the provided buffers
        /// using <see cref="Task.WhenAny(Task, Task)"/> for multiplexing.
        /// Returns the (possibly resized) buffers along with the byte counts, since async methods cannot use ref parameters.
        /// </summary>
        private static async Task<(ArraySegment<byte> output, ArraySegment<byte> error)> ReadPipesToBuffersAsync(
            Stream outputStream, Stream errorStream, CancellationToken cancellationToken)
        {
            int outputBytesRead = 0;
            int errorBytesRead = 0;

            byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            byte[] errorBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);

            try
            {
                Task<int>? outputRead = outputStream.ReadAsync(outputBuffer, outputBytesRead, outputBuffer.Length - outputBytesRead, cancellationToken);
                Task<int>? errorRead = errorStream.ReadAsync(errorBuffer, errorBytesRead, errorBuffer.Length - errorBytesRead, cancellationToken);

                while (outputRead is not null || errorRead is not null)
                {
                    Task<int> finished;
                    if (outputRead is not null && errorRead is not null)
                    {
                        finished = await Task.WhenAny(outputRead, errorRead).ConfigureAwait(false);
                    }
                    else
                    {
                        finished = (outputRead ?? errorRead)!;
                    }

                    bool isError = finished == errorRead;
                    int bytesRead = await finished.ConfigureAwait(false);

                    if (bytesRead > 0)
                    {
                        if (isError)
                        {
                            errorBytesRead += bytesRead;
                            if (errorBytesRead == errorBuffer.Length)
                            {
                                RentLargerBuffer(ref errorBuffer, errorBytesRead);
                            }
                            errorRead = errorStream.ReadAsync(errorBuffer, errorBytesRead, errorBuffer.Length - errorBytesRead, cancellationToken);
                        }
                        else
                        {
                            outputBytesRead += bytesRead;
                            if (outputBytesRead == outputBuffer.Length)
                            {
                                RentLargerBuffer(ref outputBuffer, outputBytesRead);
                            }
                            outputRead = outputStream.ReadAsync(outputBuffer, outputBytesRead, outputBuffer.Length - outputBytesRead, cancellationToken);
                        }
                    }
                    else
                    {
                        // EOF: pipe write end was closed.
                        if (isError)
                        {
                            errorRead = null;
                        }
                        else
                        {
                            outputRead = null;
                        }
                    }
                }

                return (new ArraySegment<byte>(outputBuffer, 0, outputBytesRead), new ArraySegment<byte>(errorBuffer, 0, errorBytesRead));
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(outputBuffer);
                ArrayPool<byte>.Shared.Return(errorBuffer);

                throw;
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

            var outputHandle = GetSafeFileHandleFromStreamReader(_standardOutput!);
            var errorHandle = GetSafeFileHandleFromStreamReader(_standardError!);

            bool outputRefAdded = false;
            bool errorRefAdded = false;

            try
            {
                outputHandle.DangerousAddRef(ref outputRefAdded);
                errorHandle.DangerousAddRef(ref errorRefAdded);

                ReadPipes(outputHandle, errorHandle, timeoutMs,
                    ref outputBuffer, ref outputBytesRead,
                    ref errorBuffer, ref errorBytesRead);
            }
            finally
            {
                if (outputRefAdded)
                {
                    outputHandle.DangerousRelease();
                }

                if (errorRefAdded)
                {
                    errorHandle.DangerousRelease();
                }
            }
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
