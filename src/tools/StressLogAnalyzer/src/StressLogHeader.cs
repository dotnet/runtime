// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StressLogAnalyzer;

[StructLayout(LayoutKind.Sequential)]
internal struct StressLogHeader
{
    public nint headerSize;

    [InlineArray(4)]
    public struct Magic
    {
        private byte b;
    }
    public Magic magic;

    public uint version;
    public nuint memoryBase;
    public nuint memoryCur;
    public nuint memoryLimit;
    public nuint logs;
    public ulong tickFrequency;
    public ulong startTimeStamp;
    public uint threadsWithNoLog;
    private uint reserved1;

    [InlineArray(15)]
    private struct ReservedSpace
    {
        private ulong reserved;
    }
    private ReservedSpace reserved2;

    public struct ModuleDesc
    {
        public nuint baseAddr;
        public nuint size;
    }

    [InlineArray(5)]
    public struct ModuleTable
    {
        private ModuleDesc moduleDesc;
    }

    public ModuleTable moduleTable;

    public const int ModuleImageDataSize = 64 * 1024 * 1024;

    [InlineArray(ModuleImageDataSize)]
    public struct ModuleImageData
    {
        private byte b;
    }

    public ModuleImageData moduleImageData;
}
