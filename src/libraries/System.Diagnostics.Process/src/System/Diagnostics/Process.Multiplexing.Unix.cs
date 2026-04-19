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

                Decoder outputDecoder = outputEncoding.GetDecoder();
                Decoder errorDecoder = errorEncoding.GetDecoder();

                long deadline = timeoutMs >= 0 ? Environment.TickCount64 + timeoutMs : long.MaxValue;

                int outputCharStart = 0, outputCharEnd = 0;
                int errorCharStart = 0, errorCharEnd = 0;
                bool outputDone = false, errorDone = false;

                List<ProcessOutputLine> lines = new();

                while (!outputDone || !errorDone)
                {
                    int numFds = 0;
                    int outputIndex = -1;
                    int errorIndex = -1;

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

                    if (!TryGetRemainingTimeout(deadline, timeoutMs, out int pollTimeout))
                    {
                        throw new TimeoutException();
                    }

                    Interop.Error pollError = PollPipes(pollFds, numFds, pollTimeout, out uint triggered);
                    if (pollError != Interop.Error.SUCCESS)
                    {
                        if (pollError == Interop.Error.EINTR)
                        {
                            continue;
                        }

                        throw new Win32Exception(Interop.Sys.ConvertErrorPalToPlatform(pollError));
                    }

                    if (triggered == 0)
                    {
                        throw new TimeoutException();
                    }

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
                            int bytesRead = ReadNonBlocking(currentHandle, errorByteBuffer, 0);
                            if (bytesRead > 0)
                            {
                                DecodeAndAppendChars(errorDecoder, errorByteBuffer, 0, bytesRead, flush: false, ref errorCharBuffer, ref errorCharEnd);
                                ParseLinesFromCharBuffer(errorCharBuffer, ref errorCharStart, errorCharEnd, true, lines);
                                CompactOrGrowCharBuffer(ref errorCharBuffer, ref errorCharStart, ref errorCharEnd);
                            }
                            else if (bytesRead == 0)
                            {
                                DecodeAndAppendChars(errorDecoder, Array.Empty<byte>(), 0, 0, flush: true, ref errorCharBuffer, ref errorCharEnd);
                                EmitRemainingCharsAsLine(errorCharBuffer, ref errorCharStart, ref errorCharEnd, true, lines);
                                errorDone = true;
                            }
                            // bytesRead < 0 means EAGAIN — nothing available yet, let poll retry.
                        }
                        else
                        {
                            int bytesRead = ReadNonBlocking(currentHandle, outputByteBuffer, 0);
                            if (bytesRead > 0)
                            {
                                DecodeAndAppendChars(outputDecoder, outputByteBuffer, 0, bytesRead, flush: false, ref outputCharBuffer, ref outputCharEnd);
                                ParseLinesFromCharBuffer(outputCharBuffer, ref outputCharStart, outputCharEnd, false, lines);
                                CompactOrGrowCharBuffer(ref outputCharBuffer, ref outputCharStart, ref outputCharEnd);
                            }
                            else if (bytesRead == 0)
                            {
                                DecodeAndAppendChars(outputDecoder, Array.Empty<byte>(), 0, 0, flush: true, ref outputCharBuffer, ref outputCharEnd);
                                EmitRemainingCharsAsLine(outputCharBuffer, ref outputCharStart, ref outputCharEnd, false, lines);
                                outputDone = true;
                            }
                            // bytesRead < 0 means EAGAIN — nothing available yet, let poll retry.
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
        /// Calls poll(2) on the provided array of poll events.
        /// </summary>
        private static unsafe Interop.Error PollPipes(Interop.PollEvent[] pollFds, int numFds, int timeoutMs, out uint triggered)
        {
            uint localTriggered = 0;
            Interop.Error result;
            fixed (Interop.PollEvent* pPollFds = pollFds)
            {
                result = Interop.Sys.Poll(pPollFds, (uint)numFds, timeoutMs, &localTriggered);
            }

            triggered = localTriggered;
            return result;
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
                int numFds = 0;

                int outputIndex = -1;
                int errorIndex = -1;

                if (!outputDone)
                {
                    outputIndex = numFds;
                    pollFds[numFds].FileDescriptor = outputFd;
                    pollFds[numFds].Events = Interop.PollEvents.POLLIN;
                    pollFds[numFds].TriggeredEvents = Interop.PollEvents.POLLNONE;
                    numFds++;
                }

                if (!errorDone)
                {
                    errorIndex = numFds;
                    pollFds[numFds].FileDescriptor = errorFd;
                    pollFds[numFds].Events = Interop.PollEvents.POLLIN;
                    pollFds[numFds].TriggeredEvents = Interop.PollEvents.POLLNONE;
                    numFds++;
                }

                int pollTimeout;
                if (!TryGetRemainingTimeout(deadline, timeoutMs, out pollTimeout))
                {
                    throw new TimeoutException();
                }

                unsafe
                {
                    uint triggered;
                    fixed (Interop.PollEvent* pPollFds = pollFds)
                    {
                        Interop.Error error = Interop.Sys.Poll(pPollFds, (uint)numFds, pollTimeout, &triggered);
                        if (error != Interop.Error.SUCCESS)
                        {
                            if (error == Interop.Error.EINTR)
                            {
                                // We don't re-issue the poll immediately because we need to check
                                // if we've already exceeded the overall timeout.
                                continue;
                            }

                            throw new Win32Exception(Interop.Sys.ConvertErrorPalToPlatform(error));
                        }

                        if (triggered == 0)
                        {
                            throw new TimeoutException();
                        }
                    }
                }

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
