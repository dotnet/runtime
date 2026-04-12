// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>
        /// Reads from both standard output and standard error pipes using Windows overlapped IO
        /// with wait handles for single-threaded synchronous multiplexing.
        /// </summary>
        private static unsafe void ReadPipes(
            SafeFileHandle outputHandle,
            SafeFileHandle errorHandle,
            int timeoutMs,
            ref byte[] outputBuffer,
            ref int outputBytesRead,
            ref byte[] errorBuffer,
            ref int errorBytesRead)
        {
            MemoryHandle outputPin = default;
            MemoryHandle errorPin = default;
            NativeOverlapped* outputOverlapped = null;
            NativeOverlapped* errorOverlapped = null;
            EventWaitHandle? outputEvent = null;
            EventWaitHandle? errorEvent = null;

            try
            {
                outputPin = outputBuffer.AsMemory().Pin();
                errorPin = errorBuffer.AsMemory().Pin();

                outputEvent = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);
                errorEvent = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);

                outputOverlapped = AllocateOverlapped(outputEvent);
                errorOverlapped = AllocateOverlapped(errorEvent);

                bool outputDone = false;
                bool errorDone = false;

                WaitHandle[] waitHandles = [outputEvent, errorEvent];

                // Issue initial reads.
                Interop.Kernel32.ReadFile(outputHandle, (byte*)outputPin.Pointer + outputBytesRead,
                    outputBuffer.Length - outputBytesRead, IntPtr.Zero, outputOverlapped);

                Interop.Kernel32.ReadFile(errorHandle, (byte*)errorPin.Pointer + errorBytesRead,
                    errorBuffer.Length - errorBytesRead, IntPtr.Zero, errorOverlapped);

                long deadline = timeoutMs >= 0
                    ? Environment.TickCount64 + timeoutMs
                    : long.MaxValue;

                while (!outputDone || !errorDone)
                {
                    int waitTimeout;
                    if (timeoutMs >= 0)
                    {
                        long remaining = deadline - Environment.TickCount64;
                        if (remaining <= 0)
                        {
                            CancelPendingIOIfNeeded(outputHandle, outputDone, outputOverlapped);
                            CancelPendingIOIfNeeded(errorHandle, errorDone, errorOverlapped);
                            throw new TimeoutException();
                        }

                        waitTimeout = (int)Math.Min(remaining, int.MaxValue);
                    }
                    else
                    {
                        waitTimeout = Timeout.Infinite;
                    }

                    int waitResult = WaitHandle.WaitAny(waitHandles, waitTimeout);

                    if (waitResult == WaitHandle.WaitTimeout)
                    {
                        CancelPendingIOIfNeeded(outputHandle, outputDone, outputOverlapped);
                        CancelPendingIOIfNeeded(errorHandle, errorDone, errorOverlapped);
                        throw new TimeoutException();
                    }

                    bool isError = waitResult == 1;
                    NativeOverlapped* currentOverlapped = isError ? errorOverlapped : outputOverlapped;
                    SafeFileHandle currentHandle = isError ? errorHandle : outputHandle;
                    ref int totalBytesRead = ref (isError ? ref errorBytesRead : ref outputBytesRead);
                    ref byte[] currentBuffer = ref (isError ? ref errorBuffer : ref outputBuffer);
                    EventWaitHandle currentEvent = isError ? errorEvent : outputEvent;

                    int bytesRead = GetOverlappedResultForPipe(currentHandle, currentOverlapped);

                    if (bytesRead > 0)
                    {
                        totalBytesRead += bytesRead;

                        if (totalBytesRead == currentBuffer.Length)
                        {
                            ref MemoryHandle currentPin = ref (isError ? ref errorPin : ref outputPin);
                            currentPin.Dispose();

                            RentLargerBuffer(ref currentBuffer, totalBytesRead);

                            currentPin = currentBuffer.AsMemory().Pin();
                        }

                        // Reset the event and overlapped for next read.
                        ResetOverlapped(currentEvent, currentOverlapped);

                        byte* pinPointer = isError ? (byte*)errorPin.Pointer : (byte*)outputPin.Pointer;
                        Interop.Kernel32.ReadFile(currentHandle, pinPointer + totalBytesRead,
                            currentBuffer.Length - totalBytesRead, IntPtr.Zero, currentOverlapped);
                    }
                    else
                    {
                        // EOF: pipe write end was closed.
                        if (isError)
                        {
                            errorDone = true;
                        }
                        else
                        {
                            outputDone = true;
                        }

                        // Reset the event so WaitAny won't trigger on this stale handle.
                        currentEvent.Reset();
                    }
                }
            }
            finally
            {
                if (outputOverlapped is not null)
                {
                    NativeMemory.Free(outputOverlapped);
                }

                if (errorOverlapped is not null)
                {
                    NativeMemory.Free(errorOverlapped);
                }

                outputEvent?.Dispose();
                errorEvent?.Dispose();
                outputPin.Dispose();
                errorPin.Dispose();
            }
        }

        private static unsafe NativeOverlapped* AllocateOverlapped(EventWaitHandle waitHandle)
        {
            NativeOverlapped* overlapped = (NativeOverlapped*)NativeMemory.AllocZeroed((nuint)sizeof(NativeOverlapped));
            overlapped->EventHandle = waitHandle.SafeWaitHandle.DangerousGetHandle();
            return overlapped;
        }

        private static unsafe void ResetOverlapped(EventWaitHandle waitHandle, NativeOverlapped* overlapped)
        {
            waitHandle.Reset();

            overlapped->InternalHigh = IntPtr.Zero;
            overlapped->InternalLow = IntPtr.Zero;
            overlapped->OffsetHigh = 0;
            overlapped->OffsetLow = 0;
            overlapped->EventHandle = waitHandle.SafeWaitHandle.DangerousGetHandle();
        }

        private static unsafe int GetOverlappedResultForPipe(SafeFileHandle handle, NativeOverlapped* overlapped)
        {
            int bytesRead = 0;
            if (!Interop.Kernel32.GetOverlappedResult(handle, overlapped, ref bytesRead, bWait: false))
            {
                int errorCode = Marshal.GetLastPInvokeError();
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_HANDLE_EOF:
                    case Interop.Errors.ERROR_BROKEN_PIPE:
                    case Interop.Errors.ERROR_PIPE_NOT_CONNECTED:
                        return 0;
                    default:
                        throw new Win32Exception(errorCode);
                }
            }

            return bytesRead;
        }

        private static unsafe void CancelPendingIOIfNeeded(SafeFileHandle handle, bool done, NativeOverlapped* overlapped)
        {
            if (done)
            {
                return;
            }

            // CancelIoEx marks matching outstanding I/O requests for cancellation.
            Interop.Kernel32.CancelIoEx(handle, overlapped);

            // We must observe completion before freeing the OVERLAPPED.
            int bytesRead = 0;
            Interop.Kernel32.GetOverlappedResult(handle, overlapped, ref bytesRead, bWait: true);
        }
    }
}
