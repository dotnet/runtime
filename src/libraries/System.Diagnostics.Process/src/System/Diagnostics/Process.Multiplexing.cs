// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
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
        private const int MaxEncodingBytesLength = 4;

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
        /// Reads all standard output and standard error of the process as lines of text,
        /// interleaving them as they become available.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the streams to be fully read.
        /// When <see langword="null" />, waits indefinitely.
        /// </param>
        /// <returns>
        /// An enumerable of <see cref="ProcessOutputLine"/> instances representing the lines
        /// read from standard output and standard error.
        /// </returns>
        /// <remarks>
        /// Lines from standard output and standard error are yielded as they become available.
        /// When data is available in both standard output and standard error, standard error
        /// is processed first.
        /// </remarks>
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
        public IEnumerable<ProcessOutputLine> ReadAllLines(TimeSpan? timeout = default)
        {
            ValidateReadAllState();

            int timeoutMs = timeout.HasValue
                ? ProcessUtils.ToTimeoutMilliseconds(timeout.Value)
                : Timeout.Infinite;

            Encoding outputEncoding = _startInfo?.StandardOutputEncoding ?? GetStandardOutputEncoding();
            Encoding errorEncoding = _startInfo?.StandardErrorEncoding ?? GetStandardOutputEncoding();

            return ReadPipesToLines(timeoutMs, outputEncoding, errorEncoding);
        }

        /// <summary>
        /// Decodes bytes from the byte buffer using the <paramref name="decoder"/> and appends the
        /// resulting characters to the char buffer, growing it if necessary.
        /// To flush the decoder at EOF, pass an empty byte array with <paramref name="flush"/> set to
        /// <see langword="true"/>.
        /// </summary>
        private static void DecodeAndAppendChars(
            Decoder decoder,
            ReadOnlySpan<byte> byteBuffer,
            bool flush,
            ref char[] charBuffer,
            ref int charStartIndex,
            ref int charEndIndex)
        {
            int charCount = decoder.GetCharCount(byteBuffer, flush);

            // If there isn't enough room at the end, compact the consumed space at the start first
            // so that if growth is still needed, RentLargerBuffer copies only the unconsumed data.
            if (charEndIndex + charCount > charBuffer.Length && charStartIndex > 0)
            {
                int remaining = charEndIndex - charStartIndex;
                Array.Copy(charBuffer, charStartIndex, charBuffer, 0, remaining);
                charStartIndex = 0;
                charEndIndex = remaining;
            }

            while (charEndIndex + charCount > charBuffer.Length)
            {
                RentLargerBuffer(ref charBuffer, charEndIndex);
            }

            int decoded = decoder.GetChars(byteBuffer, charBuffer.AsSpan(charEndIndex), flush);
            charEndIndex += decoded;
        }

        /// <summary>
        /// Checks for the encoding's preamble or a BOM from a different encoding at the start of
        /// the byte buffer, mimicking <see cref="StreamReader"/> behavior.
        /// If the encoding's own preamble is found, returns the number of bytes to skip.
        /// If a different encoding's BOM is detected, updates <paramref name="encoding"/> and
        /// <paramref name="decoder"/> and returns the BOM length to skip.
        /// </summary>
        private static int SkipPreambleOrDetectEncoding(ReadOnlySpan<byte> byteBuffer, ref Encoding encoding, ref Decoder decoder)
        {
            // Check for the encoding's own preamble first (like StreamReader.IsPreamble).
            ReadOnlySpan<byte> preamble = encoding.Preamble;
            if (preamble.Length > 0 && byteBuffer.Length >= preamble.Length
                && byteBuffer.Slice(0, preamble.Length).SequenceEqual(preamble))
            {
                return preamble.Length;
            }

            // No preamble match — check for BOM from other encodings (like StreamReader.DetectEncoding).
            if (byteBuffer.Length >= 2)
            {
                ushort firstTwoBytes = BinaryPrimitives.ReadUInt16LittleEndian(byteBuffer);

                if (firstTwoBytes == 0xFFFE)
                {
                    // Big Endian Unicode
                    encoding = Encoding.BigEndianUnicode;
                    decoder = encoding.GetDecoder();
                    return 2;
                }

                if (firstTwoBytes == 0xFEFF)
                {
                    if (byteBuffer.Length >= 4 && byteBuffer[2] == 0 && byteBuffer[3] == 0)
                    {
                        encoding = Encoding.UTF32;
                        decoder = encoding.GetDecoder();
                        return 4;
                    }

                    encoding = Encoding.Unicode;
                    decoder = encoding.GetDecoder();
                    return 2;
                }

                if (byteBuffer.Length >= 3 && firstTwoBytes == 0xBBEF && byteBuffer[2] == 0xBF)
                {
                    encoding = Encoding.UTF8;
                    decoder = encoding.GetDecoder();
                    return 3;
                }

                if (byteBuffer.Length >= 4 && firstTwoBytes == 0 && byteBuffer[2] == 0xFE && byteBuffer[3] == 0xFF)
                {
                    encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
                    decoder = encoding.GetDecoder();
                    return 4;
                }
            }

            return 0;
        }

        /// <summary>
        /// Scans the char buffer from <paramref name="startIndex"/> to <paramref name="endIndex"/> for complete
        /// lines (delimited by <c>\r</c>, <c>\n</c>, or <c>\r\n</c>), adds each as a
        /// <see cref="ProcessOutputLine"/> to <paramref name="lines"/>, and advances
        /// <paramref name="startIndex"/> past the consumed data.
        /// This matches <see cref="StreamReader.ReadLine"/> behavior used by the async path.
        /// </summary>
        private static void ParseLinesFromCharBuffer(
            char[] buffer,
            ref int startIndex,
            int endIndex,
            bool standardError,
            List<ProcessOutputLine> lines)
        {
            while (startIndex < endIndex)
            {
                int remaining = endIndex - startIndex;
                int lineEnd = buffer.AsSpan(startIndex, remaining).IndexOfAny('\r', '\n');
                if (lineEnd == -1)
                {
                    break;
                }

                char terminator = buffer[startIndex + lineEnd];

                // If we found '\r', we need to check for a following '\n' to treat \r\n as one terminator.
                // If '\n' isn't available yet (end of current data), stop and wait for more data.
                if (terminator == '\r')
                {
                    if (startIndex + lineEnd + 1 >= endIndex)
                    {
                        // The '\r' is at the very end of available data — we can't tell yet
                        // whether it's a standalone '\r' or part of '\r\n'. Wait for more data.
                        break;
                    }

                    lines.Add(new ProcessOutputLine(
                        new string(buffer, startIndex, lineEnd),
                        standardError));

                    // Skip \r and also \n if it immediately follows.
                    startIndex += lineEnd + 1;
                    if (startIndex < endIndex && buffer[startIndex] == '\n')
                    {
                        startIndex++;
                    }
                }
                else
                {
                    // terminator == '\n'
                    lines.Add(new ProcessOutputLine(
                        new string(buffer, startIndex, lineEnd),
                        standardError));

                    startIndex += lineEnd + 1;
                }
            }
        }

        /// <summary>
        /// Emits any remaining characters in the buffer as a final line when an EOF is reached.
        /// A trailing <c>\r</c> is stripped to match <see cref="StreamReader.ReadLine"/> behavior.
        /// </summary>
        private static void EmitRemainingCharsAsLine(
            char[] buffer,
            ref int startIndex,
            ref int endIndex,
            bool standardError,
            List<ProcessOutputLine> lines)
        {
            if (startIndex < endIndex)
            {
                int length = endIndex - startIndex;
                if (length > 0 && buffer[startIndex + length - 1] == '\r')
                {
                    length--;
                }

                lines.Add(new ProcessOutputLine(
                    new string(buffer, startIndex, length),
                    standardError));

                startIndex = 0;
                endIndex = 0;
            }
        }

        private static void DecodeBytesAndParseLines(Decoder decoder, ReadOnlySpan<byte> byteBuffer, ref char[] charBuffer, ref int charStart, ref int charEnd, bool standardError, List<ProcessOutputLine> lines)
        {
            DecodeAndAppendChars(decoder, byteBuffer, flush: false, ref charBuffer, ref charStart, ref charEnd);
            ParseLinesFromCharBuffer(charBuffer, ref charStart, charEnd, standardError, lines);
        }

        private static bool FlushDecoderAndEmitRemainingChars(bool preambleChecked, Encoding encoding, Decoder decoder, ReadOnlySpan<byte> unconsumedBytes, ref char[] charBuffer, ref int charStart, ref int charEnd, bool standardError, List<ProcessOutputLine> lines)
        {
            if (!preambleChecked && unconsumedBytes.Length > 0)
            {
                unconsumedBytes = unconsumedBytes.Slice(SkipPreambleOrDetectEncoding(unconsumedBytes, ref encoding, ref decoder));
            }

            DecodeAndAppendChars(decoder, unconsumedBytes, flush: true, ref charBuffer, ref charStart, ref charEnd);
            EmitRemainingCharsAsLine(charBuffer, ref charStart, ref charEnd, standardError, lines);
            return true;
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
        /// Rents a larger buffer from the array pool, copies existing data, and returns the old buffer to the pool.
        /// </summary>
        private static void RentLargerBuffer<T>(ref T[] buffer, int dataLength)
        {
            int newSize = (int)Math.Min((long)buffer.Length * 2, Array.MaxLength);
            newSize = Math.Max(buffer.Length + 1, newSize);
            T[] newBuffer = ArrayPool<T>.Shared.Rent(newSize);
            Array.Copy(buffer, newBuffer, dataLength);
            T[] oldBuffer = buffer;
            buffer = newBuffer;
            ArrayPool<T>.Shared.Return(oldBuffer);
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
