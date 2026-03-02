// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        /// <summary>Converts SocketFlags to the kernel msg_flags representation for io_uring.</summary>
        private static bool TryConvertIoUringPrepareSocketFlags(SocketFlags flags, out uint rwFlags)
        {
            const SocketFlags SupportedIoUringFlags =
                SocketFlags.OutOfBand |
                SocketFlags.Peek |
                SocketFlags.DontRoute;

            if ((flags & ~SupportedIoUringFlags) != 0)
            {
                rwFlags = 0;
                return false;
            }

            rwFlags = (uint)(int)flags;
            return true;
        }

        /// <summary>Writes a send/recv-like SQE (send, send_zc, recv) with a user-supplied buffer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteSendLikeSqe(
            IoUringSqe* sqe,
            byte opcode,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            byte* buffer,
            uint length,
            uint rwFlags)
        {
            sqe->Opcode = opcode;
            sqe->Flags = sqeFlags;
            sqe->Ioprio = 0; // Not used by send/recv opcodes.
            sqe->Fd = sqeFd;
            sqe->Off = 0; // Not used by send/recv opcodes.
            sqe->Addr = (ulong)(nuint)buffer;
            sqe->Len = length;
            sqe->RwFlags = rwFlags;
            sqe->UserData = userData;
            // BufIndex, Personality, SpliceFdIn, Addr3: zeroed by TryGetNextManagedSqe.
        }

        /// <summary>Writes a read-fixed SQE for registered-buffer receive.</summary>
        private static unsafe void WriteReadFixedSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            byte* buffer,
            uint length,
            ushort bufferIndex)
        {
            sqe->Opcode = IoUringOpcodes.ReadFixed;
            sqe->Flags = sqeFlags;
            sqe->Ioprio = 0; // Not used by READ_FIXED.
            sqe->Fd = sqeFd;
            sqe->Addr = (ulong)(nuint)buffer;
            sqe->Len = length;
            sqe->RwFlags = 0; // No special read flags.
            // For non-seekable sockets, offset is ignored; -1 matches "current position" semantics.
            sqe->Off = ulong.MaxValue;
            sqe->BufIndex = bufferIndex;
            sqe->UserData = userData;
            // Personality, SpliceFdIn, Addr3: zeroed by TryGetNextManagedSqe.
        }

        /// <summary>
        /// Writes a recv SQE using provided-buffer selection (one-shot or multishot).
        /// The kernel chooses a buffer from the specified buffer group.
        /// For multishot, set <paramref name="ioprio"/> to <see cref="IoUringConstants.RecvMultishot"/>.
        /// </summary>
        private static void WriteProvidedBufferRecvSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            uint requestedLength,
            uint rwFlags,
            ushort bufferGroupId,
            ushort ioprio = 0)
        {
            sqe->Opcode = IoUringOpcodes.Recv;
            sqe->Flags = (byte)(sqeFlags | IoUringConstants.SqeBufferSelect);
            sqe->Fd = sqeFd;
            sqe->Ioprio = ioprio;
            sqe->Off = 0; // Not used by provided-buffer recv.
            sqe->Addr = 0; // No user buffer; kernel selects from buffer group.
            sqe->Len = requestedLength;
            sqe->RwFlags = rwFlags;
            sqe->BufIndex = bufferGroupId;
            sqe->UserData = userData;
            // Personality, SpliceFdIn, Addr3: zeroed by TryGetNextManagedSqe.
        }

        /// <summary>Writes an accept SQE (one-shot or multishot) to the submission ring entry.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteAcceptSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            byte* socketAddress,
            IntPtr socketAddressLengthPtr,
            bool multishot = false)
        {
            sqe->Opcode = IoUringOpcodes.Accept;
            sqe->Flags = sqeFlags;
            sqe->Fd = sqeFd;
            // Explicit write for defensive clarity; multishot and one-shot accept must not
            // inherit stale ioprio bits from previous SQE occupants.
            sqe->Ioprio = multishot ? IoUringConstants.AcceptMultishot : (ushort)0;
            sqe->Addr = (ulong)(nuint)socketAddress;
            // Kernel accept prep aliases addr2 at sqe->off.
            sqe->Off = (ulong)(nuint)socketAddressLengthPtr;
            sqe->Len = 0; // Not used by accept.
            sqe->RwFlags = IoUringConstants.AcceptFlags;
            sqe->UserData = userData;
            // BufIndex, Personality, SpliceFdIn, Addr3: zeroed by TryGetNextManagedSqe.
        }

        /// <summary>Writes a sendmsg/sendmsg_zc/recvmsg SQE to the submission ring entry.</summary>
        private static void WriteSendMsgLikeSqe(
            IoUringSqe* sqe,
            byte opcode,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            IntPtr messageHeader,
            uint rwFlags)
        {
            sqe->Opcode = opcode;
            sqe->Flags = sqeFlags;
            sqe->Ioprio = 0; // Not used by sendmsg/recvmsg.
            sqe->Fd = sqeFd;
            sqe->Off = 0; // Not used by sendmsg/recvmsg.
            sqe->Addr = (ulong)(nuint)messageHeader;
            sqe->Len = 1;
            sqe->RwFlags = rwFlags;
            sqe->UserData = userData;
            // BufIndex, Personality, SpliceFdIn, Addr3: zeroed by TryGetNextManagedSqe.
        }

        /// <summary>Writes a connect SQE to the submission ring entry.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteConnectSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            byte* socketAddress,
            int socketAddressLen)
        {
            sqe->Opcode = IoUringOpcodes.Connect;
            sqe->Flags = sqeFlags;
            sqe->Ioprio = 0; // Not used by connect.
            sqe->Fd = sqeFd;
            sqe->Addr = (ulong)(nuint)socketAddress;
            // Kernel connect prep aliases addrlen at sqe->off and requires len=0.
            sqe->Off = (uint)socketAddressLen;
            sqe->Len = 0; // Kernel requires len=0 for connect.
            sqe->RwFlags = 0; // No special flags for connect.
            sqe->UserData = userData;
            // BufIndex, Personality, SpliceFdIn, Addr3: zeroed by TryGetNextManagedSqe.
        }

        /// <summary>Writes an ASYNC_CANCEL SQE targeting the specified user_data.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteAsyncCancelSqe(IoUringSqe* sqe, ulong userData)
        {
            sqe->Opcode = IoUringOpcodes.AsyncCancel;
            sqe->Flags = 0; // No SQE flags for cancel.
            sqe->Ioprio = 0; // Not used by ASYNC_CANCEL.
            sqe->Fd = -1;
            sqe->Off = 0; // Not used by ASYNC_CANCEL.
            Debug.Assert((byte)(userData >> IoUringUserDataTagShift) == IoUringConstants.TagReservedCompletion);
            sqe->Addr = userData;
            sqe->Len = 0; // Not used by ASYNC_CANCEL.
            sqe->RwFlags = 0; // Not used by ASYNC_CANCEL.
            sqe->UserData = 0;
            // BufIndex, Personality, SpliceFdIn, Addr3: zeroed by TryGetNextManagedSqe.
        }
    }
}
