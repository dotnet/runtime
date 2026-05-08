// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        private static SafePipeHandle GetSafeHandleFromStreamReader(StreamReader reader) => ((AnonymousPipeClientStream)reader.BaseStream).SafePipeHandle;

        /// <summary>
        /// Reads from both standard output and standard error pipes as lines of text using Unix
        /// poll-based multiplexing with non-blocking reads.
        /// Buffers are rented from the pool and returned when enumeration completes.
        /// </summary>
        private IEnumerable<ProcessOutputLine> ReadPipesToLines(
            int timeoutMs,
            Encoding outputEncoding,
            Encoding errorEncoding)
        {
            SafePipeHandle outputHandle = GetSafeHandleFromStreamReader(_standardOutput!);
            SafePipeHandle errorHandle = GetSafeHandleFromStreamReader(_standardError!);

            byte[] outputByteBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            byte[] errorByteBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            char[] outputCharBuffer = ArrayPool<char>.Shared.Rent(InitialReadAllBufferSize);
            char[] errorCharBuffer = ArrayPool<char>.Shared.Rent(InitialReadAllBufferSize);
            bool outputRefAdded = false, errorRefAdded = false;

            try
            {
                outputHandle.DangerousAddRef(ref outputRefAdded);
                errorHandle.DangerousAddRef(ref errorRefAdded);

                int outputFd = outputHandle.DangerousGetHandle().ToInt32();
                int errorFd = errorHandle.DangerousGetHandle().ToInt32();

                if (Interop.Sys.Fcntl.DangerousSetIsNonBlocking(outputFd, 1) != 0
                    || Interop.Sys.Fcntl.DangerousSetIsNonBlocking(errorFd, 1) != 0)
                {
                    throw new Win32Exception();
                }

                // Cannot use stackalloc in an iterator method; use a regular array.
                Interop.PollEvent[] pollFds = new Interop.PollEvent[2];

                long deadline = timeoutMs >= 0 ? Environment.TickCount64 + timeoutMs : long.MaxValue;

                Decoder outputDecoder = outputEncoding.GetDecoder();
                Decoder errorDecoder = errorEncoding.GetDecoder();
                int outputCharStart = 0, outputCharEnd = 0;
                int errorCharStart = 0, errorCharEnd = 0;
                int unconsumedOutputBytesCount = 0, unconsumedErrorBytesCount = 0;
                bool outputDone = false, errorDone = false;
                bool outputPreambleChecked = false, errorPreambleChecked = false;

                List<ProcessOutputLine> lines = new();

                while (!outputDone || !errorDone)
                {
                    int numFds = PollForPipeActivity(pollFds, errorFd, outputFd, errorDone, outputDone, deadline, timeoutMs, out int errorIndex, out int outputIndex);

                    // Process error pipe first (lower index) when both have data available.
                    for (int i = 0; i < numFds; i++)
                    {
                        if (pollFds[i].TriggeredEvents == Interop.PollEvents.POLLNONE)
                        {
                            continue;
                        }

                        bool isError = i == errorIndex;
                        SafePipeHandle currentHandle = isError ? errorHandle : outputHandle;

                        // Use explicit branching to avoid ref locals across yield points.
                        if (isError)
                        {
                            HandlePipeLineRead(currentHandle, ref errorDecoder, ref errorEncoding,
                                errorByteBuffer, ref unconsumedErrorBytesCount,
                                ref errorCharBuffer, ref errorCharStart, ref errorCharEnd,
                                ref errorPreambleChecked, ref errorDone, isError, lines);
                        }
                        else
                        {
                            HandlePipeLineRead(currentHandle, ref outputDecoder, ref outputEncoding,
                                outputByteBuffer, ref unconsumedOutputBytesCount,
                                ref outputCharBuffer, ref outputCharStart, ref outputCharEnd,
                                ref outputPreambleChecked, ref outputDone, isError, lines);
                        }
                    }

                    // Yield parsed lines outside of any ref-local scope.
                    foreach (ProcessOutputLine line in lines)
                    {
                        yield return line;
                    }

                    lines.Clear();
                }
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

                ArrayPool<byte>.Shared.Return(outputByteBuffer);
                ArrayPool<byte>.Shared.Return(errorByteBuffer);
                ArrayPool<char>.Shared.Return(outputCharBuffer);
                ArrayPool<char>.Shared.Return(errorCharBuffer);
            }
        }

        /// <summary>
        /// Populates the poll fd array with the active pipe file descriptors.
        /// Error is added first so it gets serviced first when both have data.
        /// Returns the number of active file descriptors.
        /// </summary>
        private static int PreparePollFds(
            Span<Interop.PollEvent> pollFds,
            int errorFd, int outputFd,
            bool errorDone, bool outputDone,
            out int errorIndex, out int outputIndex)
        {
            int numFds = 0;
            errorIndex = -1;
            outputIndex = -1;

            if (!errorDone)
            {
                errorIndex = numFds;
                pollFds[numFds].FileDescriptor = errorFd;
                pollFds[numFds].Events = Interop.PollEvents.POLLIN;
                pollFds[numFds].TriggeredEvents = Interop.PollEvents.POLLNONE;
                numFds++;
            }

            if (!outputDone)
            {
                outputIndex = numFds;
                pollFds[numFds].FileDescriptor = outputFd;
                pollFds[numFds].Events = Interop.PollEvents.POLLIN;
                pollFds[numFds].TriggeredEvents = Interop.PollEvents.POLLNONE;
                numFds++;
            }

            return numFds;
        }

        /// <summary>
        /// Prepares the poll fd array, checks the remaining timeout, calls poll(2), and handles
        /// errors. Returns the number of polled fds, or 0 if poll was interrupted (EINTR) and
        /// the caller should retry.
        /// </summary>
        private static int PollForPipeActivity(
            Span<Interop.PollEvent> pollFds,
            int errorFd, int outputFd,
            bool errorDone, bool outputDone,
            long deadline, int timeoutMs,
            out int errorIndex, out int outputIndex)
        {
            int numFds = PreparePollFds(pollFds, errorFd, outputFd, errorDone, outputDone, out errorIndex, out outputIndex);

            if (!TryGetRemainingTimeout(deadline, timeoutMs, out int pollTimeout))
            {
                throw new TimeoutException();
            }

            uint triggered = 0;
            Interop.Error pollError;
            unsafe
            {
                fixed (Interop.PollEvent* pPollFds = pollFds)
                {
                    pollError = Interop.Sys.Poll(pPollFds, (uint)numFds, pollTimeout, &triggered);
                }
            }

            if (pollError != Interop.Error.SUCCESS)
            {
                if (pollError == Interop.Error.EINTR)
                {
                    return 0;
                }

                throw new Win32Exception(Interop.Sys.ConvertErrorPalToPlatform(pollError));
            }

            if (triggered == 0)
            {
                throw new TimeoutException();
            }

            return numFds;
        }

        /// <summary>
        /// Handles a poll notification for a single pipe: reads bytes, decodes to chars,
        /// strips BOM on first decode, parses lines, compacts the char buffer, and sets
        /// <paramref name="done"/> to <see langword="true"/> on EOF.
        /// </summary>
        private static void HandlePipeLineRead(
            SafePipeHandle handle,
            ref Decoder decoder,
            ref Encoding encoding,
            byte[] byteBuffer,
            ref int unconsumedBytesCount,
            ref char[] charBuffer,
            ref int charStart,
            ref int charEnd,
            ref bool preambleChecked,
            ref bool done,
            bool standardError,
            List<ProcessOutputLine> lines)
        {
            int bytesRead = ReadNonBlocking(handle, byteBuffer, offset: unconsumedBytesCount);
            if (bytesRead > 0)
            {
                ReadOnlySpan<byte> bytes = byteBuffer.AsSpan(0, unconsumedBytesCount + bytesRead);

                if (!preambleChecked)
                {
                    if (bytes.Length >= MaxEncodingBytesLength)
                    {
                        bytes = bytes.Slice(SkipPreambleOrDetectEncoding(bytes, ref encoding, ref decoder));
                        preambleChecked = true;
                        unconsumedBytesCount = 0;
                    }
                    else
                    {
                        unconsumedBytesCount += bytesRead;
                    }
                }

                if (preambleChecked)
                {
                    DecodeBytesAndParseLines(decoder, bytes, ref charBuffer, ref charStart, ref charEnd, standardError, lines);
                }
            }
            else if (bytesRead == 0)
            {
                done = FlushDecoderAndEmitRemainingChars(preambleChecked, encoding, decoder, byteBuffer.AsSpan(0, unconsumedBytesCount),
                    ref charBuffer, ref charStart, ref charEnd, standardError, lines);
            }
            // bytesRead < 0 means EAGAIN — nothing available yet, let poll retry.
        }

        /// <summary>
        /// Reads from both standard output and standard error pipes using Unix poll-based multiplexing
        /// with non-blocking reads.
        /// </summary>
        private static void ReadPipes(
            SafePipeHandle outputHandle,
            SafePipeHandle errorHandle,
            int timeoutMs,
            ref byte[] outputBuffer,
            ref int outputBytesRead,
            ref byte[] errorBuffer,
            ref int errorBytesRead)
        {
            int outputFd = outputHandle.DangerousGetHandle().ToInt32();
            int errorFd = errorHandle.DangerousGetHandle().ToInt32();

            if (Interop.Sys.Fcntl.DangerousSetIsNonBlocking(outputFd, 1) != 0 || Interop.Sys.Fcntl.DangerousSetIsNonBlocking(errorFd, 1) != 0)
            {
                throw new Win32Exception();
            }

            Span<Interop.PollEvent> pollFds = stackalloc Interop.PollEvent[2];

            long deadline = timeoutMs >= 0
                ? Environment.TickCount64 + timeoutMs
                : long.MaxValue;

            bool outputDone = false, errorDone = false;
            while (!outputDone || !errorDone)
            {
                int numFds = PollForPipeActivity(pollFds, errorFd, outputFd, errorDone, outputDone, deadline, timeoutMs, out int errorIndex, out int outputIndex);

                for (int i = 0; i < numFds; i++)
                {
                    if (pollFds[i].TriggeredEvents == Interop.PollEvents.POLLNONE)
                    {
                        continue;
                    }

                    bool isError = i == errorIndex;
                    SafePipeHandle currentHandle = isError ? errorHandle : outputHandle;
                    ref byte[] currentBuffer = ref (isError ? ref errorBuffer : ref outputBuffer);
                    ref int currentBytesRead = ref (isError ? ref errorBytesRead : ref outputBytesRead);
                    ref bool currentDone = ref (isError ? ref errorDone : ref outputDone);

                    int bytesRead = ReadNonBlocking(currentHandle, currentBuffer, currentBytesRead);
                    if (bytesRead > 0)
                    {
                        currentBytesRead += bytesRead;

                        if (currentBytesRead == currentBuffer.Length)
                        {
                            RentLargerBuffer(ref currentBuffer, currentBytesRead);
                        }
                    }
                    else if (bytesRead == 0)
                    {
                        // EOF: pipe write end was closed.
                        currentDone = true;
                    }
                    // bytesRead < 0 means EAGAIN — nothing available yet, let poll retry.
                }
            }
        }

        /// <summary>
        /// Performs a non-blocking read from the given handle into the buffer starting at the specified offset.
        /// Returns the number of bytes read, 0 for EOF, or -1 for EAGAIN (nothing available yet).
        /// </summary>
        private static unsafe int ReadNonBlocking(SafePipeHandle handle, byte[] buffer, int offset)
        {
            fixed (byte* pBuffer = buffer)
            {
                int bytesRead = Interop.Sys.Read(handle, pBuffer + offset, buffer.Length - offset);
                if (bytesRead < 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error == Interop.Error.EAGAIN)
                    {
                        return -1;
                    }

                    throw new Win32Exception(errorInfo.RawErrno);
                }

                return bytesRead;
            }
        }
    }
}
