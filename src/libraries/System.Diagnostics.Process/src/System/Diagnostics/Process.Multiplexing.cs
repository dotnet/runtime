// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
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
            (ArraySegment<byte> standardOutput, ArraySegment<byte> standardError) = await ReadAllBytesIntoRentedArraysAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                Encoding outputEncoding = _startInfo?.StandardOutputEncoding ?? GetStandardOutputEncoding();
                Encoding errorEncoding = _startInfo?.StandardErrorEncoding ?? GetStandardOutputEncoding();

                return (outputEncoding.GetString(standardOutput.AsSpan()), errorEncoding.GetString(standardError.AsSpan()));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(standardOutput.Array!);
                ArrayPool<byte>.Shared.Return(standardError.Array!);
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
            (ArraySegment<byte> standardOutput, ArraySegment<byte> standardError) = await ReadAllBytesIntoRentedArraysAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return (standardOutput.AsSpan().ToArray(), standardError.AsSpan().ToArray());
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(standardOutput.Array!);
                ArrayPool<byte>.Shared.Return(standardError.Array!);
            }
        }

        private async Task<(ArraySegment<byte> StandardOutput, ArraySegment<byte> StandardError)> ReadAllBytesIntoRentedArraysAsync(CancellationToken cancellationToken)
        {
            ValidateReadAllState();

            Task<ArraySegment<byte>> outputTask = ReadPipeToBufferAsync(_standardOutput!.BaseStream, cancellationToken);
            Task<ArraySegment<byte>> errorTask = ReadPipeToBufferAsync(_standardError!.BaseStream, cancellationToken);

            Task whenAll = Task.WhenAll(outputTask, errorTask);

            try
            {
                await whenAll.ConfigureAwait(false);
            }
            catch
            {
                // It's possible that one of the tasks has failed and the other has succeeded.
                // In such case, we need to return the array to the pool.
                if (outputTask.IsCompletedSuccessfully)
                {
                    ArrayPool<byte>.Shared.Return(outputTask.Result.Array!);
                }

                if (errorTask.IsCompletedSuccessfully)
                {
                    ArrayPool<byte>.Shared.Return(errorTask.Result.Array!);
                }

                // If there is an AggregateException with multiple exceptions, throw it.
                if (whenAll.Exception?.InnerExceptions.Count > 1)
                {
                    throw whenAll.Exception;
                }

                throw;
            }

            // If we got here, Task.WhenAll has succeeded and both results are available.
            return (outputTask.Result, errorTask.Result);
        }

        /// <summary>
        /// Asynchronously reads the entire content of a stream into a pooled buffer.
        /// The caller is responsible for returning the buffer to the pool after use.
        /// </summary>
        private static async Task<ArraySegment<byte>> ReadPipeToBufferAsync(Stream stream, CancellationToken cancellationToken)
        {
            int bytesRead = 0;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);

            try
            {
                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(bytesRead), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    bytesRead += read;
                    if (bytesRead == buffer.Length)
                    {
                        RentLargerBuffer(ref buffer, bytesRead);
                    }
                }

                return new ArraySegment<byte>(buffer, 0, bytesRead);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously reads all standard output and standard error of the process as lines of text,
        /// interleaving them as they become available.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// An async enumerable of <see cref="ProcessOutputLine"/> instances representing the lines
        /// read from standard output and standard error.
        /// </returns>
        /// <remarks>
        /// Lines from standard output and standard error are yielded as they become available.
        /// When the consumer stops enumerating early (for example, by breaking out of
        /// <see langword="await foreach" />), any pending read operations are canceled.
        /// </remarks>
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
        public async IAsyncEnumerable<ProcessOutputLine> ReadAllLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ValidateReadAllState();

            StreamReader outputReader = _standardOutput!;
            StreamReader errorReader = _standardError!;

            Channel<ProcessOutputLine> channel = Channel.CreateBounded<ProcessOutputLine>(0);
            bool firstCompleted = false;

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task outputTask = ReadToChannelAsync(outputReader, standardError: false, linkedCts.Token);
            Task errorTask = ReadToChannelAsync(errorReader, standardError: true, linkedCts.Token);

            try
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out ProcessOutputLine line))
                    {
                        yield return line;
                    }
                }
            }
            finally
            {
                linkedCts.Cancel();

                // Ensure both tasks complete before disposing the CancellationTokenSource.
                // The tasks handle all exceptions internally, so they always run to completion.
                await outputTask.ConfigureAwait(false);
                await errorTask.ConfigureAwait(false);

                linkedCts.Dispose();
            }

            async Task ReadToChannelAsync(StreamReader reader, bool standardError, CancellationToken ct)
            {
                try
                {
                    while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is string line)
                    {
                        await channel.Writer.WriteAsync(new ProcessOutputLine(line, standardError), ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    channel.Writer.TryComplete(ex);
                    return;
                }

                if (Interlocked.Exchange(ref firstCompleted, true))
                {
                    channel.Writer.TryComplete();
                }
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
                ? ProcessUtils.ToTimeoutMilliseconds(timeout.Value)
                : Timeout.Infinite;

            var outputHandle = GetSafeHandleFromStreamReader(_standardOutput!);
            var errorHandle = GetSafeHandleFromStreamReader(_standardError!);

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
