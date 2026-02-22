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
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Addr = (ulong)(nuint)buffer;
            sqe->Len = length;
            sqe->RwFlags = rwFlags;
            sqe->UserData = userData;
        }

        /// <summary>Writes a recv SQE to the submission ring entry.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteRecvSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            byte* buffer,
            uint length,
            uint rwFlags)
        {
            sqe->Opcode = IoUringOpcodes.Recv;
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Ioprio = 0;
            sqe->Addr = (ulong)(nuint)buffer;
            sqe->Len = length;
            sqe->RwFlags = rwFlags;
            sqe->BufIndex = 0;
            sqe->UserData = userData;
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
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Ioprio = 0;
            sqe->Addr = (ulong)(nuint)buffer;
            sqe->Len = length;
            // For non-seekable sockets, offset is ignored; -1 matches "current position" semantics.
            sqe->Off = ulong.MaxValue;
            sqe->RwFlags = 0;
            sqe->BufIndex = bufferIndex;
            sqe->UserData = userData;
        }

        /// <summary>
        /// Writes a one-shot recv SQE using provided-buffer selection.
        /// The kernel chooses a buffer from the specified buffer group.
        /// </summary>
        private static void WriteProvidedBufferRecvSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            uint requestedLength,
            uint rwFlags,
            ushort bufferGroupId)
        {
            sqe->Opcode = IoUringOpcodes.Recv;
            sqe->Fd = sqeFd;
            sqe->Flags = (byte)(sqeFlags | IoUringConstants.SqeBufferSelect);
            sqe->Ioprio = 0;
            sqe->Addr = 0;
            sqe->Len = requestedLength;
            sqe->RwFlags = rwFlags;
            sqe->BufIndex = bufferGroupId;
            sqe->UserData = userData;
        }

        /// <summary>
        /// Writes a multishot recv SQE to the submission ring entry.
        /// The kernel selects buffers from a provided buffer ring (IOSQE_BUFFER_SELECT).
        /// </summary>
        private static void WriteMultishotRecvSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            ushort bufferGroupId)
        {
            sqe->Opcode = IoUringOpcodes.Recv;
            sqe->Fd = sqeFd;
            sqe->Flags = (byte)(sqeFlags | IoUringConstants.SqeBufferSelect);
            sqe->Ioprio = IoUringConstants.RecvMultishot;
            sqe->Addr = 0;
            sqe->Len = 0;
            sqe->RwFlags = 0;
            sqe->BufIndex = bufferGroupId;
            sqe->UserData = userData;
        }

        /// <summary>Writes an accept SQE to the submission ring entry.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteAcceptSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            byte* socketAddress,
            IntPtr socketAddressLengthPtr)
        {
            sqe->Opcode = IoUringOpcodes.Accept;
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Addr = (ulong)(nuint)socketAddress;
            // Kernel accept prep aliases addr2 at sqe->off.
            sqe->Off = (ulong)(nuint)socketAddressLengthPtr;
            sqe->RwFlags = IoUringConstants.AcceptFlags;
            sqe->UserData = userData;
        }

        /// <summary>Writes a multishot accept SQE to the submission ring entry.</summary>
        private static unsafe void WriteMultishotAcceptSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            byte* socketAddress,
            IntPtr socketAddressLengthPtr)
        {
            sqe->Opcode = IoUringOpcodes.Accept;
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Ioprio = IoUringConstants.AcceptMultishot;
            sqe->Addr = (ulong)(nuint)socketAddress;
            // accept4 prep aliases addr2 at sqe->off for addrlen pointer
            sqe->Off = (ulong)(nuint)socketAddressLengthPtr;
            sqe->RwFlags = IoUringConstants.AcceptFlags;
            sqe->UserData = userData;
        }

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
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Addr = (ulong)(nuint)messageHeader;
            sqe->Len = 1;
            sqe->RwFlags = rwFlags;
            sqe->UserData = userData;
        }

        /// <summary>Writes a recvmsg SQE to the submission ring entry.</summary>
        private static void WriteRecvMsgSqe(
            IoUringSqe* sqe,
            int sqeFd,
            byte sqeFlags,
            ulong userData,
            IntPtr messageHeader,
            uint rwFlags)
        {
            sqe->Opcode = IoUringOpcodes.RecvMsg;
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Addr = (ulong)(nuint)messageHeader;
            sqe->Len = 1;
            sqe->RwFlags = rwFlags;
            sqe->UserData = userData;
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
            sqe->Fd = sqeFd;
            sqe->Flags = sqeFlags;
            sqe->Addr = (ulong)(nuint)socketAddress;
            // Kernel connect prep aliases addrlen at sqe->off and requires len=0.
            sqe->Off = (uint)socketAddressLen;
            sqe->Len = 0;
            sqe->UserData = userData;
        }

        /// <summary>Writes an ASYNC_CANCEL SQE targeting the specified user_data.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteAsyncCancelSqe(IoUringSqe* sqe, ulong userData)
        {
            sqe->Opcode = IoUringOpcodes.AsyncCancel;
            sqe->Fd = -1;
            Debug.Assert((byte)(userData >> IoUringUserDataTagShift) == IoUringConstants.TagReservedCompletion);
            sqe->Addr = userData;
            sqe->UserData = 0;
        }


    }
}
