// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>
        /// Reads from both standard output and standard error pipes using Unix poll-based multiplexing
        /// with non-blocking reads.
        /// </summary>
        private static void ReadPipes(
            SafeFileHandle outputHandle,
            SafeFileHandle errorHandle,
            int timeoutMs,
            ref byte[] outputBuffer,
            ref int outputBytesRead,
            ref byte[] errorBuffer,
            ref int errorBytesRead)
        {
            int outputFd = outputHandle.DangerousGetHandle().ToInt32();
            int errorFd = errorHandle.DangerousGetHandle().ToInt32();

            if (Interop.Sys.Fcntl.DangerousSetIsNonBlocking((IntPtr)outputFd, 1) != 0 || Interop.Sys.Fcntl.DangerousSetIsNonBlocking((IntPtr)errorFd, 1) != 0)
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
                    SafeFileHandle currentHandle = isError ? errorHandle : outputHandle;
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
        private static unsafe int ReadNonBlocking(SafeFileHandle handle, byte[] buffer, int offset)
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
