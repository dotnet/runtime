// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class SOSDacInterface8Tests
{
    private const int S_OK = 0;
    private const int S_FALSE = 1;

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNumberGenerations_ReturnsCorrectCount(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0xAA00_0000, AllocationStart = 0xAA00_1000, AllocContextPointer = 0xAA00_2000, AllocContextLimit = 0xAA00_3000 },
            new() { StartSegment = 0xBB00_0000, AllocationStart = 0xBB00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xCC00_0000, AllocationStart = 0xCC00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xDD00_0000, AllocationStart = 0xDD00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000];

        ISOSDacInterface8 dac8 = new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapWks(gc => gc
                    .SetGenerations(generations)
                    .SetFillPointers(fillPointers))
                .Build(),
            legacyObj: null);

        uint numGenerations;
        int hr = dac8.GetNumberGenerations(&numGenerations);
        Assert.Equal(S_OK, hr);
        Assert.Equal(4u, numGenerations);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNumberGenerations_WithFiveGenerations(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0xA000_0000, AllocationStart = 0xA000_1000, AllocContextPointer = 0xA000_2000, AllocContextLimit = 0xA000_3000 },
            new() { StartSegment = 0xB000_0000, AllocationStart = 0xB000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xC000_0000, AllocationStart = 0xC000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xD000_0000, AllocationStart = 0xD000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xE000_0000, AllocationStart = 0xE000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1001, 0x2002, 0x3003, 0x4004, 0x5005, 0x6006, 0x7007];

        ISOSDacInterface8 dac8 = new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapWks(gc => gc
                    .SetGenerations(generations)
                    .SetFillPointers(fillPointers))
                .Build(),
            legacyObj: null);

        uint numGenerations;
        int hr = dac8.GetNumberGenerations(&numGenerations);
        Assert.Equal(S_OK, hr);
        Assert.Equal(5u, numGenerations);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGenerationTable_ReturnsCorrectData(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0x1A00_0000, AllocationStart = 0x1A00_1000, AllocContextPointer = 0x1A00_2000, AllocContextLimit = 0x1A00_3000 },
            new() { StartSegment = 0x1B00_0000, AllocationStart = 0x1B00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0x1C00_0000, AllocationStart = 0x1C00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0x1D00_0000, AllocationStart = 0x1D00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000];

        ISOSDacInterface8 dac8 = new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapWks(gc => gc
                    .SetGenerations(generations)
                    .SetFillPointers(fillPointers))
                .Build(),
            legacyObj: null);

        // First call with cGenerations=0 to query needed count
        uint needed;
        int hr = dac8.GetGenerationTable(0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal(4u, needed);

        // Second call with sufficient buffer
        DacpGenerationData* genData = stackalloc DacpGenerationData[4];
        hr = dac8.GetGenerationTable(4, genData, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < generations.Length; i++)
        {
            ulong expectedStartSeg = SignExtend(generations[i].StartSegment, arch);
            ulong expectedAllocStart = SignExtend(generations[i].AllocationStart, arch);
            ulong expectedAllocCtxPtr = SignExtend(generations[i].AllocContextPointer, arch);
            ulong expectedAllocCtxLim = SignExtend(generations[i].AllocContextLimit, arch);
            Assert.Equal(expectedStartSeg, (ulong)genData[i].start_segment);
            Assert.Equal(expectedAllocStart, (ulong)genData[i].allocation_start);
            Assert.Equal(expectedAllocCtxPtr, (ulong)genData[i].allocContextPtr);
            Assert.Equal(expectedAllocCtxLim, (ulong)genData[i].allocContextLimit);
        }
    }

    private static ulong SignExtend(ulong value, MockTarget.Architecture arch)
    {
        if (arch.Is64Bit)
            return value;

        return (ulong)(long)(int)(uint)value;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFinalizationFillPointers_ReturnsCorrectData(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0xAA00_0000, AllocationStart = 0xAA00_1000, AllocContextPointer = 0xAA00_2000, AllocContextLimit = 0xAA00_3000 },
            new() { StartSegment = 0xBB00_0000, AllocationStart = 0xBB00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xCC00_0000, AllocationStart = 0xCC00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xDD00_0000, AllocationStart = 0xDD00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1111, 0x2222, 0x3333, 0x4444, 0x5555, 0x6666, 0x7777];

        ISOSDacInterface8 dac8 = new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapWks(gc => gc
                    .SetGenerations(generations)
                    .SetFillPointers(fillPointers))
                .Build(),
            legacyObj: null);

        // First call with cFillPointers=0 to query needed count
        uint needed;
        int hr = dac8.GetFinalizationFillPointers(0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal(7u, needed);

        // Second call with sufficient buffer
        ClrDataAddress* ptrs = stackalloc ClrDataAddress[7];
        hr = dac8.GetFinalizationFillPointers(7, ptrs, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < fillPointers.Length; i++)
        {
            Assert.Equal(SignExtend(fillPointers[i], arch), (ulong)ptrs[i]);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGenerationTableSvr_ReturnsCorrectData(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0x1A00_0000, AllocationStart = 0x1A00_1000, AllocContextPointer = 0x1A00_2000, AllocContextLimit = 0x1A00_3000 },
            new() { StartSegment = 0x1B00_0000, AllocationStart = 0x1B00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0x1C00_0000, AllocationStart = 0x1C00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0x1D00_0000, AllocationStart = 0x1D00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000];

        ISOSDacInterface8 dac8 = new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapSvr(gc => gc
                    .SetGenerations(generations)
                    .SetFillPointers(fillPointers), out var heapAddr)
                .Build(),
            legacyObj: null);

        uint needed;
        int hr = dac8.GetGenerationTableSvr((ClrDataAddress)heapAddr, 0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal(4u, needed);

        DacpGenerationData* genData = stackalloc DacpGenerationData[4];
        hr = dac8.GetGenerationTableSvr((ClrDataAddress)heapAddr, 4, genData, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < generations.Length; i++)
        {
            Assert.Equal(SignExtend(generations[i].StartSegment, arch), (ulong)genData[i].start_segment);
            Assert.Equal(SignExtend(generations[i].AllocationStart, arch), (ulong)genData[i].allocation_start);
            Assert.Equal(SignExtend(generations[i].AllocContextPointer, arch), (ulong)genData[i].allocContextPtr);
            Assert.Equal(SignExtend(generations[i].AllocContextLimit, arch), (ulong)genData[i].allocContextLimit);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFinalizationFillPointersSvr_ReturnsCorrectData(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0x1A00_0000, AllocationStart = 0x1A00_1000, AllocContextPointer = 0x1A00_2000, AllocContextLimit = 0x1A00_3000 },
            new() { StartSegment = 0x1B00_0000, AllocationStart = 0x1B00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0x1C00_0000, AllocationStart = 0x1C00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0x1D00_0000, AllocationStart = 0x1D00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1111, 0x2222, 0x3333, 0x4444, 0x5555, 0x6666, 0x7777];

        ISOSDacInterface8 dac8 = new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapSvr(gc => gc
                    .SetGenerations(generations)
                    .SetFillPointers(fillPointers), out var heapAddr)
                .Build(),
            legacyObj: null);

        uint needed;
        int hr = dac8.GetFinalizationFillPointersSvr((ClrDataAddress)heapAddr, 0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal(7u, needed);

        ClrDataAddress* ptrs = stackalloc ClrDataAddress[7];
        hr = dac8.GetFinalizationFillPointersSvr((ClrDataAddress)heapAddr, 7, ptrs, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < fillPointers.Length; i++)
        {
            Assert.Equal(SignExtend(fillPointers[i], arch), (ulong)ptrs[i]);
        }
    }
}
