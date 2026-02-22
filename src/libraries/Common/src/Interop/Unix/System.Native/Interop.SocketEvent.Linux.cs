// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>Derived SQ ring state computed after mmap, used by the managed submission path.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct IoUringSqRingInfo
        {
            public IntPtr SqeBase;
            public IntPtr SqTailPtr;
            public IntPtr SqHeadPtr;
            public uint SqMask;
            public uint SqEntries;
            public uint SqeSize;
            public byte UsesNoSqArray;
            public int RingFd;
            public int RegisteredRingFd;
            public byte UsesEnterExtArg;
            public byte UsesRegisteredFiles;
        }

        /// <summary>Mirrors kernel <c>struct io_sqring_offsets</c> (40 bytes). Fields at offset 28+ (resv1, user_addr) are unused.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 40)]
        internal struct IoUringSqOffsets
        {
            [FieldOffset(0)]  public uint Head;
            [FieldOffset(4)]  public uint Tail;
            [FieldOffset(8)]  public uint RingMask;
            [FieldOffset(12)] public uint RingEntries;
            [FieldOffset(16)] public uint Flags;
            [FieldOffset(20)] public uint Dropped;
            [FieldOffset(24)] public uint Array;
            // resv1 at 28, user_addr at 32 - not needed by managed code
        }

        /// <summary>Mirrors kernel <c>struct io_cqring_offsets</c> (40 bytes). Fields at offset 28+ (resv1, user_addr) are unused.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 40)]
        internal struct IoUringCqOffsets
        {
            [FieldOffset(0)]  public uint Head;
            [FieldOffset(4)]  public uint Tail;
            [FieldOffset(8)]  public uint RingMask;
            [FieldOffset(12)] public uint RingEntries;
            [FieldOffset(16)] public uint Overflow;
            [FieldOffset(20)] public uint Cqes;
            [FieldOffset(24)] public uint Flags;
            // resv1 at 28, user_addr at 32 - not needed by managed code
        }

        /// <summary>Mirrors kernel <c>struct io_uring_params</c> (120 bytes), passed to io_uring_setup.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 120)]
        internal struct IoUringParams
        {
            [FieldOffset(0)]  public uint SqEntries;
            [FieldOffset(4)]  public uint CqEntries;
            [FieldOffset(8)]  public uint Flags;
            [FieldOffset(12)] public uint SqThreadCpu;
            [FieldOffset(16)] public uint SqThreadIdle;
            [FieldOffset(20)] public uint Features;
            [FieldOffset(24)] public uint WqFd;
            // resv[3] at 28-39
            [FieldOffset(40)] public IoUringSqOffsets SqOff;
            [FieldOffset(80)] public IoUringCqOffsets CqOff;
        }

        /// <summary>Mirrors kernel <c>struct io_uring_cqe</c> (16 bytes), read from the CQ ring.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        internal struct IoUringCqe
        {
            [FieldOffset(0)]  public ulong UserData;
            [FieldOffset(8)]  public int Result;
            [FieldOffset(12)] public uint Flags;
        }

        /// <summary>Mirrors kernel <c>struct io_uring_buf</c> (16 bytes), used by provided-buffer rings.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        internal struct IoUringBuf
        {
            [FieldOffset(0)]  public ulong Address;
            [FieldOffset(8)]  public uint Length;
            [FieldOffset(12)] public ushort BufferId;
            [FieldOffset(14)] public ushort Reserved;
        }

        /// <summary>
        /// Mirrors the header overlay of kernel <c>struct io_uring_buf_ring</c> (16 bytes).
        /// In UAPI this shares offset 0 with the first <c>io_uring_buf</c> entry via a union.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        internal struct IoUringBufRingHeader
        {
            [FieldOffset(0)]  public ulong Reserved1;
            [FieldOffset(8)]  public uint Reserved2;
            [FieldOffset(12)] public ushort Reserved3;
            [FieldOffset(14)] public ushort Tail;
        }

        /// <summary>Mirrors kernel <c>struct io_uring_buf_reg</c> (40 bytes), used for pbuf ring registration.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 40)]
        internal struct IoUringBufReg
        {
            [FieldOffset(0)]  public ulong RingAddress;
            [FieldOffset(8)]  public uint RingEntries;
            [FieldOffset(12)] public ushort BufferGroupId;
            [FieldOffset(14)] public ushort Padding;
            [FieldOffset(16)] public ulong Reserved0;
            [FieldOffset(24)] public ulong Reserved1;
            [FieldOffset(32)] public ulong Reserved2;
        }

        /// <summary>Derived CQ ring state computed after mmap, used by the managed completion drain path.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct IoUringCqRingInfo
        {
            public IntPtr CqeBase;       // io_uring_cqe* base of CQE array
            public IntPtr CqTailPtr;     // uint32_t* kernel writes CQ tail
            public IntPtr CqHeadPtr;     // uint32_t* managed advances CQ head
            public uint CqMask;          // CqEntries - 1
            public uint CqEntries;       // number of CQ slots
            public uint CqeSize;         // sizeof(io_uring_cqe) = 16
            public IntPtr CqOverflowPtr; // uint32_t* kernel CQ overflow counter
        }

        /// <summary>Mirrors kernel <c>struct io_uring_getevents_arg</c>, used with IORING_ENTER_EXT_ARG.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct IoUringGeteventsArg
        {
            public ulong Sigmask;
            public uint SigmaskSize;
            public uint MinWaitUsec;
            public ulong Ts;
        }

        /// <summary>Mirrors kernel <c>struct __kernel_timespec</c>, used for io_uring timeout arguments.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct IoUringKernelTimespec
        {
            public long TvSec;
            public long TvNsec;
        }

    }
}
