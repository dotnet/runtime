// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct IoEvent
        {
            internal ulong Data;
            internal ulong Obj;
            internal long Res;
            internal long Res2;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IoControlBlock
        {
            internal ulong AioData;

            // these fields swap for big endian
            // https://github.com/torvalds/linux/blob/0a679e13ea30f85a1aef0669ee0c5a9fd7860b34/include/uapi/linux/aio_abi.h#L77-L83
            private uint _swappedField_1;
            private uint _swappedField_2;

            internal ushort AioLioOpcode;
            internal short AioReqprio;
            internal uint AioFildes;

            internal ulong AioBuf;
            internal ulong AioNbytes;
            internal long AioOffset;

            internal ulong AioReserved2;

            internal uint AioFlags;
            internal uint AioResfd;

            internal uint AioKey
            {
                get => BitConverter.IsLittleEndian ? _swappedField_1 : _swappedField_2;
                set
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        _swappedField_1 = value;
                    }
                    else
                    {
                        _swappedField_2 = value;
                    }
                }
            }
            internal int AioRwFlags
            {
                get => BitConverter.IsLittleEndian ? (int)_swappedField_2 : (int)_swappedField_1;
                set
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        _swappedField_2 = (uint)value;
                    }
                    else
                    {
                        _swappedField_1 = (uint)value;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AioRing
        {
            internal uint Id;
            internal uint Nr;
            internal uint Head;
            internal uint Tail;
            internal uint Magic;
            internal uint CompatFeatures;
            internal uint IncompatFeatures;
            internal uint HeaderLength;

            internal static unsafe IoEvent* IoEvents(AioRing* ring, int idx)
            {
                IoEvent* ev = (IoEvent*)(ring + 1);
                return ev + idx;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AioContext
        {
            internal unsafe AioRing* Ring;
        }

        internal static class IoControlBlockFlags
        {
            internal const int IOCB_CMD_PREAD   = 0;
            internal const int IOCB_CMD_PWRITE  = 1;
            internal const int IOCB_CMD_FSYNC   = 2;
            internal const int IOCB_CMD_FDSYNC  = 3;
            // 4 was the experimental IOCB_CMD_PREADX
            internal const int IOCB_CMD_POLL    = 5;
            internal const int IOCB_CMD_NOOP    = 6;
            internal const int IOCB_CMD_PREADV  = 7;
            internal const int IOCB_CMD_PWRITEV = 8;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_IsAioSupported")]
        internal static extern bool IsAioSupported();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoSetup", SetLastError = true)]
        internal static extern unsafe int IoSetup(uint eventsCount, AioContext* context);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoDestroy", SetLastError = true)]
        internal static extern unsafe int IoDestroy(AioRing* ring);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoSubmit", SetLastError = true)]
        internal static extern unsafe int IoSubmit(AioRing* ring, long controlBlocksCount, IoControlBlock** ioControlBlocks);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoGetEvents", SetLastError = true)]
        internal static extern unsafe int IoGetEvents(AioRing* ring, long minimumEventsCount, long maximumEventsCount, IoEvent* ioEvents);
    }
}
