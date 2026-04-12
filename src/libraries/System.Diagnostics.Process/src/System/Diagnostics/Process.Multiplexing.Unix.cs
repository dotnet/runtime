// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>
        /// Reads from both standard output and standard error pipes using Unix poll-based multiplexing.
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
            bool outputDone = false;
            bool errorDone = false;

            Interop.PollEvent[] pollFds = new Interop.PollEvent[2];

            long deadline = timeoutMs >= 0
                ? Environment.TickCount64 + timeoutMs
                : long.MaxValue;

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
                if (timeoutMs >= 0)
                {
                    long remaining = deadline - Environment.TickCount64;
                    if (remaining <= 0)
                    {
                        throw new TimeoutException();
                    }

                    pollTimeout = (int)Math.Min(remaining, int.MaxValue);
                }
                else
                {
                    pollTimeout = -1; // Infinite
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
                                continue;
                            }

                            throw new Win32Exception(Marshal.GetLastPInvokeError());
                        }

                        if (triggered == 0)
                        {
                            throw new TimeoutException();
                        }
                    }
                }

                for (int i = 0; i < numFds; i++)
                {
                    if ((pollFds[i].TriggeredEvents & (Interop.PollEvents.POLLIN | Interop.PollEvents.POLLHUP | Interop.PollEvents.POLLERR)) == Interop.PollEvents.POLLNONE)
                    {
                        continue;
                    }

                    bool isError = i == errorIndex;
                    SafeFileHandle currentHandle = isError ? errorHandle : outputHandle;
                    ref byte[] currentBuffer = ref (isError ? ref errorBuffer : ref outputBuffer);
                    ref int currentBytesRead = ref (isError ? ref errorBytesRead : ref outputBytesRead);
                    ref bool currentDone = ref (isError ? ref errorDone : ref outputDone);

                    int bytesRead = RandomAccess.Read(currentHandle, currentBuffer.AsSpan(currentBytesRead), fileOffset: 0);
                    if (bytesRead > 0)
                    {
                        currentBytesRead += bytesRead;

                        if (currentBytesRead == currentBuffer.Length)
                        {
                            RentLargerBuffer(ref currentBuffer, currentBytesRead);
                        }
                    }
                    else
                    {
                        currentDone = true;
                    }
                }
            }
        }
    }
}
