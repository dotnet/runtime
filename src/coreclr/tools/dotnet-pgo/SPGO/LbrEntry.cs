// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LbrEntry32
    {
        public uint FromAddress;
        public uint ToAddress;
        public uint Reserved;

        public override string ToString() => $"{FromAddress:x} -> {ToAddress:x}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LbrEntry64
    {
        public ulong FromAddress;
        public ulong ToAddress;
        public ulong Reserved;

        public override string ToString() => $"{FromAddress:x} -> {ToAddress:x}";
    }

    [Flags]
    public enum LbrOptionFlags
    {
        FilterKernel = 1 << 0,
        FilterUser = 1 << 1,
        FilterJcc = 1 << 2,
        FilterNearRelCall = 1 << 3,
        FilterNearIndCall = 1 << 4,
        FilterNearRet = 1 << 5,
        FilterNearIndJmp = 1 << 6,
        FilterNearRelJmp = 1 << 7,
        FilterFarBranch = 1 << 8,
        CallstackEnable = 1 << 9,
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LbrTraceEventData32
    {
        public ulong TimeStamp;
        public uint ProcessId;
        public uint ThreadId;
        public LbrOptionFlags Options;
        private LbrEntry32 _entries;

        public Span<LbrEntry32> Entries(int totalSize)
        {
            IntPtr entriesOffset = Unsafe.ByteOffset(ref Unsafe.As<LbrTraceEventData32, byte>(ref this), ref Unsafe.As<LbrEntry32, byte>(ref _entries));
            return MemoryMarshal.CreateSpan(ref _entries, (totalSize - (int)entriesOffset) / sizeof(LbrEntry32));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LbrTraceEventData64
    {
        public ulong TimeStamp;
        public uint ProcessId;
        public uint ThreadId;
        public LbrOptionFlags Options;
        private LbrEntry64 _entries;

        public Span<LbrEntry64> Entries(int totalSize)
        {
            IntPtr entriesOffset = Unsafe.ByteOffset(ref Unsafe.As<LbrTraceEventData64, byte>(ref this), ref Unsafe.As<LbrEntry64, byte>(ref _entries));
            return MemoryMarshal.CreateSpan(ref _entries, (totalSize - (int)entriesOffset) / sizeof(LbrEntry64));
        }
    }
}
