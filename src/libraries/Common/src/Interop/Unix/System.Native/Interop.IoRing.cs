// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        // Mirrors the native IoRingOp enum in pal_io.h.
        internal enum IoRingOp : int
        {
            Read = 0,
            Write = 1,
            ReadV = 2,
            WriteV = 3,
            Accept = 4,
            Connect = 5,
            Recv = 6,
            Send = 7,
        }

        // Mirrors the native IoRingRequest struct in pal_io.h.
        // Fd is a raw file descriptor (not a SafeHandle): the caller is responsible for keeping
        // the owning SafeHandle ref-counted/alive for as long as the request may be in flight.
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IoRingRequest
        {
            public IoRingOp OpCode;
            public IntPtr Fd;
            public long Offset; // -1 for non-positional ops
            public byte* Buffer; // used by Read/Write/Recv/Send
            public int BufferLength;
            public IOVector* Vectors; // used by ReadV/WriteV
            public int VectorCount;
            public int Flags; // MSG_* flags for Recv/Send; accept flags for Accept
            public byte* SockAddr; // used by Accept (output, peer address) / Connect (input, destination address)
            public int* SockAddrLen; // in/out length of SockAddr
            public ulong UserData;
        }

        // Mirrors the native IoRingCompletion struct in pal_io.h.
        [StructLayout(LayoutKind.Sequential)]
        internal struct IoRingCompletion
        {
            public ulong UserData;
            public int Result;
            public uint Flags;
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoRingIsAvailable")]
        internal static partial int IoRingIsAvailable();

        // Pass singleIssuer: 1 to request IORING_SETUP_SINGLE_ISSUER + IORING_SETUP_DEFER_TASKRUN: every
        // subsequent IoRingSubmit/IoRingKick/IoRingWaitForCompletions call for the returned ring must then
        // come from the exact same OS thread that called this method (not merely the first thread to call
        // one of those - confirmed empirically) for the ring's whole lifetime, including
        // IoRingWaitForCompletions calls with nothing to submit; any other thread's call fails with
        // -EEXIST. Pass 0 for a plain ring that can be freely shared/rotated across threads instead.
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoRingCreate", SetLastError = true)]
        internal static partial int IoRingCreate(int submissionQueueDepth, int completionQueueDepth, int singleIssuer, out IntPtr ringHandle);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoRingSubmit", SetLastError = true)]
        internal static unsafe partial int IoRingSubmit(IntPtr ringHandle, IoRingRequest* requests, int requestCount, out int submittedCount);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoRingKick", SetLastError = true)]
        internal static partial int IoRingKick(IntPtr ringHandle);

        // Creates an eventfd and registers it with the ring (IORING_REGISTER_EVENTFD): the kernel then
        // bumps its counter whenever a CQE is posted. The returned fd is also safe for any other thread
        // to write to directly via EventFdWrite, piggybacking its own wake-up onto the same fd a single
        // waiter is blocked on in EventFdWait - see PortableThreadPool.IoUring.Unix.cs.
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoRingRegisterEventFd", SetLastError = true)]
        internal static partial int IoRingRegisterEventFd(IntPtr ringHandle);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_EventFdWrite", SetLastError = true)]
        internal static partial int EventFdWrite(int eventFd);

        // Real kernel-blocking wait (poll(2)-based - no userland spin-before-blocking), unlike
        // ManualResetEventSlim.Wait. Returns 1 if the fd became readable (and drains it), 0 on timeout.
        // Pass -1 to block indefinitely.
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_EventFdWait", SetLastError = true)]
        internal static partial int EventFdWait(int eventFd, int timeoutMilliseconds);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoRingWaitForCompletions", SetLastError = true)]
        internal static unsafe partial int IoRingWaitForCompletions(IntPtr ringHandle, IoRingCompletion* completions, int maxCompletions, int minComplete, out int completedCount);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoRingClose", SetLastError = true)]
        internal static partial int IoRingClose(IntPtr ringHandle);
    }
}
