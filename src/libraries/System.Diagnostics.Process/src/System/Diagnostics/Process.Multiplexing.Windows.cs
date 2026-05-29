// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        private static SafeFileHandle GetSafeHandleFromStreamReader(StreamReader reader) => ((FileStream)reader.BaseStream).SafeFileHandle;

        /// <summary>
        /// Reads from both standard output and standard error pipes as lines of text using Windows
        /// overlapped IO with wait handles for single-threaded synchronous multiplexing.
        /// Buffers are rented from the pool and returned when enumeration completes.
        /// </summary>
        private IEnumerable<ProcessOutputLine> ReadPipesToLines(
            int timeoutMs,
            Encoding outputEncoding,
            Encoding errorEncoding)
        {
            SafeFileHandle outputHandle = GetSafeHandleFromStreamReader(_standardOutput!);
            SafeFileHandle errorHandle = GetSafeHandleFromStreamReader(_standardError!);

            byte[] outputByteBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            byte[] errorByteBuffer = ArrayPool<byte>.Shared.Rent(InitialReadAllBufferSize);
            char[] outputCharBuffer = ArrayPool<char>.Shared.Rent(InitialReadAllBufferSize);
            char[] errorCharBuffer = ArrayPool<char>.Shared.Rent(InitialReadAllBufferSize);
            PinnedGCHandle<byte[]> outputPin = default, errorPin = default;
            // NativeOverlapped* can't be used as iterator state machine fields (pointers aren't
            // allowed in managed types). Store as nint and cast back inside scoped unsafe blocks.
            nint outputOverlappedNint = 0, errorOverlappedNint = 0;
            EventWaitHandle? outputEvent = null, errorEvent = null;
            bool outputDone = true, errorDone = true;

            try
            {
                outputPin = new PinnedGCHandle<byte[]>(outputByteBuffer);
                errorPin = new PinnedGCHandle<byte[]>(errorByteBuffer);

                outputEvent = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);
                errorEvent = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);

                unsafe
                {
                    outputOverlappedNint = (nint)AllocateOverlapped(outputEvent);
                    errorOverlappedNint = (nint)AllocateOverlapped(errorEvent);
                }

                // Error output gets index 0 so WaitAny services it first when both are signaled.
                WaitHandle[] waitHandles = [errorEvent, outputEvent];

                Decoder outputDecoder = outputEncoding.GetDecoder();
                Decoder errorDecoder = errorEncoding.GetDecoder();
                int outputCharStart = 0, outputCharEnd = 0;
                int errorCharStart = 0, errorCharEnd = 0;
                int unconsumedOutputBytesCount = 0, unconsumedErrorBytesCount = 0;
                bool outputPreambleChecked = false, errorPreambleChecked = false;

                unsafe
                {
                    outputDone = !QueueRead(outputHandle, outputPin.GetAddressOfArrayData(),
                        outputByteBuffer.Length, (NativeOverlapped*)outputOverlappedNint, outputEvent);
                    errorDone = !QueueRead(errorHandle, errorPin.GetAddressOfArrayData(),
                        errorByteBuffer.Length, (NativeOverlapped*)errorOverlappedNint, errorEvent);
                }

                long deadline = timeoutMs >= 0 ? Environment.TickCount64 + timeoutMs : long.MaxValue;
                List<ProcessOutputLine> lines = new();

                while (!outputDone || !errorDone)
                {
                    int waitResult = TryGetRemainingTimeout(deadline, timeoutMs, out int remainingMilliseconds)
                        ? WaitHandle.WaitAny(waitHandles, remainingMilliseconds)
                        : WaitHandle.WaitTimeout;

                    if (waitResult == WaitHandle.WaitTimeout)
                    {
                        throw new TimeoutException();
                    }

                    bool isError = waitResult == 0;
                    nint currentOverlappedNint = isError ? errorOverlappedNint : outputOverlappedNint;
                    SafeFileHandle currentHandle = isError ? errorHandle : outputHandle;
                    EventWaitHandle currentEvent = isError ? errorEvent! : outputEvent!;

                    int bytesRead;
                    unsafe
                    {
                        bytesRead = GetOverlappedResultForPipe(currentHandle, (NativeOverlapped*)currentOverlappedNint);
                    }

                    if (bytesRead > 0)
                    {
                        ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(
                            isError ? errorByteBuffer : outputByteBuffer,
                            0,
                            (isError ? unconsumedErrorBytesCount : unconsumedOutputBytesCount) + bytesRead);

                        ref bool preambleChecked = ref (isError ? ref errorPreambleChecked : ref outputPreambleChecked);
                        ref Encoding currentEncoding = ref (isError ? ref errorEncoding : ref outputEncoding);
                        ref Decoder currentDecoder = ref (isError ? ref errorDecoder : ref outputDecoder);
                        ref int unconsumedBytesCount = ref (isError ? ref unconsumedErrorBytesCount : ref unconsumedOutputBytesCount);

                        if (!preambleChecked)
                        {
                            if (bytes.Length >= MaxEncodingBytesLength)
                            {
                                bytes = bytes.Slice(SkipPreambleOrDetectEncoding(bytes, ref currentEncoding, ref currentDecoder));
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
                            DecodeBytesAndParseLines(currentDecoder, bytes,
                                ref isError ? ref errorCharBuffer : ref outputCharBuffer,
                                ref isError ? ref errorCharStart : ref outputCharStart,
                                ref isError ? ref errorCharEnd : ref outputCharEnd,
                                isError, lines);
                        }

                        unsafe
                        {
                            ResetOverlapped(currentEvent, (NativeOverlapped*)currentOverlappedNint);

                            byte* pinPointer = isError
                                ? (errorPin.GetAddressOfArrayData() + unconsumedErrorBytesCount)
                                : (outputPin.GetAddressOfArrayData() + unconsumedOutputBytesCount);
                            int currentByteLength = isError
                                ? errorByteBuffer.Length - unconsumedErrorBytesCount
                                : outputByteBuffer.Length - unconsumedOutputBytesCount;

                            if (!QueueRead(currentHandle, pinPointer,
                                currentByteLength,
                                (NativeOverlapped*)currentOverlappedNint, currentEvent))
                            {
                                bytesRead = 0; // EOF during QueueRead
                            }
                        }
                    }

                    if (bytesRead == 0) // EOF
                    {
                        if (isError)
                        {
                            errorDone = FlushDecoderAndEmitRemainingChars(errorPreambleChecked, errorEncoding, errorDecoder, errorByteBuffer.AsSpan(0, unconsumedErrorBytesCount),
                                ref errorCharBuffer, ref errorCharStart, ref errorCharEnd, isError, lines);
                        }
                        else
                        {
                            outputDone = FlushDecoderAndEmitRemainingChars(outputPreambleChecked, outputEncoding, outputDecoder, outputByteBuffer.AsSpan(0, unconsumedOutputBytesCount),
                                ref outputCharBuffer, ref outputCharStart, ref outputCharEnd, isError, lines);
                        }

                        currentEvent.Reset();
                    }

                    // Yield parsed lines outside of any unsafe or ref-local scope.
                    foreach (ProcessOutputLine line in lines)
                    {
                        yield return line;
                    }

                    lines.Clear();
                }
            }
            finally
            {
                unsafe
                {
                    if (outputOverlappedNint != 0)
                    {
                        CancelPendingIOIfNeeded(outputHandle, outputDone, (NativeOverlapped*)outputOverlappedNint);
                        NativeMemory.Free((void*)outputOverlappedNint);
                    }

                    if (errorOverlappedNint != 0)
                    {
                        CancelPendingIOIfNeeded(errorHandle, errorDone, (NativeOverlapped*)errorOverlappedNint);
                        NativeMemory.Free((void*)errorOverlappedNint);
                    }
                }

                outputEvent?.Dispose();
                errorEvent?.Dispose();
                outputPin.Dispose();
                errorPin.Dispose();

                ArrayPool<byte>.Shared.Return(outputByteBuffer);
                ArrayPool<byte>.Shared.Return(errorByteBuffer);
                ArrayPool<char>.Shared.Return(outputCharBuffer);
                ArrayPool<char>.Shared.Return(errorCharBuffer);
            }
        }

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
            PinnedGCHandle<byte[]> outputPin = default, errorPin = default;
            NativeOverlapped* outputOverlapped = null, errorOverlapped = null;
            EventWaitHandle? outputEvent = null, errorEvent = null;

            try
            {
                outputPin = new PinnedGCHandle<byte[]>(outputBuffer);
                errorPin = new PinnedGCHandle<byte[]>(errorBuffer);

                outputEvent = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);
                errorEvent = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);

                outputOverlapped = AllocateOverlapped(outputEvent);
                errorOverlapped = AllocateOverlapped(errorEvent);

                // Error output gets index 0 so WaitAny services it first when both are signaled.
                WaitHandle[] waitHandles = [errorEvent, outputEvent];

                // Issue initial reads.
                bool outputDone = !QueueRead(outputHandle, outputPin.GetAddressOfArrayData(), outputBuffer.Length, outputOverlapped, outputEvent);
                bool errorDone = !QueueRead(errorHandle, errorPin.GetAddressOfArrayData(), errorBuffer.Length, errorOverlapped, errorEvent);

                long deadline = timeoutMs >= 0
                    ? Environment.TickCount64 + timeoutMs
                    : long.MaxValue;

                while (!outputDone || !errorDone)
                {

                    int waitResult = TryGetRemainingTimeout(deadline, timeoutMs, out int remainingMilliseconds)
                        ? WaitHandle.WaitAny(waitHandles, remainingMilliseconds)
                        : WaitHandle.WaitTimeout;

                    if (waitResult == WaitHandle.WaitTimeout)
                    {
                        CancelPendingIOIfNeeded(outputHandle, outputDone, outputOverlapped);
                        CancelPendingIOIfNeeded(errorHandle, errorDone, errorOverlapped);

                        throw new TimeoutException();
                    }

                    bool isError = waitResult == 0;
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
                            ref PinnedGCHandle<byte[]> currentPin = ref (isError ? ref errorPin : ref outputPin);

                            RentLargerBuffer(ref currentBuffer, totalBytesRead);

                            currentPin.Target = currentBuffer;
                        }

                        // Reset the event and overlapped for next read.
                        ResetOverlapped(currentEvent, currentOverlapped);

                        byte* pinPointer = isError ? errorPin.GetAddressOfArrayData() : outputPin.GetAddressOfArrayData();
                        if (!QueueRead(currentHandle, pinPointer + totalBytesRead,
                            currentBuffer.Length - totalBytesRead, currentOverlapped, currentEvent))
                        {
                            if (isError)
                            {
                                errorDone = true;
                            }
                            else
                            {
                                outputDone = true;
                            }

                            // Ensure WaitAny won't trigger on this stale handle.
                            currentEvent.Reset();
                        }
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
            overlapped->EventHandle = SetLowOrderBit(waitHandle);

            return overlapped;
        }

        private static unsafe void ResetOverlapped(EventWaitHandle waitHandle, NativeOverlapped* overlapped)
        {
            waitHandle.Reset();

            overlapped->InternalHigh = IntPtr.Zero;
            overlapped->InternalLow = IntPtr.Zero;
            overlapped->OffsetHigh = 0;
            overlapped->OffsetLow = 0;
            overlapped->EventHandle = SetLowOrderBit(waitHandle);
        }

        private static unsafe int GetOverlappedResultForPipe(SafeFileHandle handle, NativeOverlapped* overlapped)
        {
            int bytesRead = 0;
            if (!Interop.Kernel32.GetOverlappedResult(handle, overlapped, ref bytesRead, bWait: false))
            {
                int errorCode = Marshal.GetLastPInvokeError();
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                    case Interop.Errors.ERROR_BROKEN_PIPE: // For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
                    case Interop.Errors.ERROR_PIPE_NOT_CONNECTED: // Named pipe server has disconnected, return 0 to match NamedPipeClientStream behaviour
                        return 0; // EOF!
                    default:
                        throw new Win32Exception(errorCode);
                }
            }

            return bytesRead;
        }

        /// <summary>
        /// Cancels a pending overlapped I/O and waits for completion before returning.
        /// </summary>
        private static unsafe void CancelPendingIOIfNeeded(SafeFileHandle handle, bool done, NativeOverlapped* overlapped)
        {
            if (done)
            {
                return;
            }

            // CancelIoEx marks matching outstanding I/O requests for cancellation.
            // It does not wait for all canceled operations to complete.
            // When CancelIoEx returns true, it means that the cancel request was successfully queued.
            if (!Interop.Kernel32.CancelIoEx(handle, overlapped))
            {
                // Failure has two common meanings:
                // ERROR_NOT_FOUND (extremely common). It means:
                // - The I/O already completed.
                // - Or it never existed.
                // - Or it completed between your decision and the call.
                // Other errors indicate real failures (invalid handle, driver limitation, etc.).
                int errorCode = Marshal.GetLastPInvokeError();
                Debug.Assert(errorCode == Interop.Errors.ERROR_NOT_FOUND, $"CancelIoEx failed with {errorCode}.");
            }

            // We must observe completion before freeing the OVERLAPPED in all the above scenarios.
            // Use bWait: true to ensure the I/O operation completes before we free the OVERLAPPED structure.
            // Per MSDN: "Do not reuse or free the OVERLAPPED structure until GetOverlappedResult returns."
            int bytesRead = 0;
            if (!Interop.Kernel32.GetOverlappedResult(handle, overlapped, ref bytesRead, bWait: true))
            {
                int errorCode = Marshal.GetLastPInvokeError();
                Debug.Assert(errorCode is Interop.Errors.ERROR_OPERATION_ABORTED or Interop.Errors.ERROR_BROKEN_PIPE, $"GetOverlappedResult failed with {errorCode}.");
            }
        }

        /// <summary>
        /// Returns the event handle with the low-order bit set.
        /// Per https://learn.microsoft.com/windows/win32/api/ioapiset/nf-ioapiset-getqueuedcompletionstatus,
        /// setting the low-order bit of hEvent in the OVERLAPPED structure prevents the I/O completion
        /// from being queued to a completion port bound to the same file object. The kernel masks off
        /// the bit when signaling, so the event still works normally.
        /// </summary>
        private static nint SetLowOrderBit(EventWaitHandle waitHandle)
            => waitHandle.SafeWaitHandle.DangerousGetHandle() | 1;

        private static unsafe bool QueueRead(
            SafeFileHandle handle,
            byte* buffer,
            int bufferLength,
            NativeOverlapped* overlapped,
            EventWaitHandle waitHandle)
        {
            if (Interop.Kernel32.ReadFile(handle, buffer, bufferLength, IntPtr.Zero, overlapped) != 0)
            {
                waitHandle.Set();
                return true;
            }

            int error = Marshal.GetLastPInvokeError();
            if (error == Interop.Errors.ERROR_IO_PENDING)
            {
                return true;
            }

            if (error == Interop.Errors.ERROR_BROKEN_PIPE || error == Interop.Errors.ERROR_HANDLE_EOF)
            {
                return false;
            }

            throw new Win32Exception(error);
        }
    }
}
