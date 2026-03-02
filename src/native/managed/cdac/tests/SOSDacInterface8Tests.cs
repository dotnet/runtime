// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class SOSDacInterface8Tests
{
    private const int S_OK = 0;
    private const int S_FALSE = 1;

    private static readonly GCHeapBuilder.GenerationInput[] s_generations =
    [
        new() { StartSegment = 0x1A00_0000, AllocationStart = 0x1A00_1000, AllocContextPointer = 0x1A00_2000, AllocContextLimit = 0x1A00_3000 },
        new() { StartSegment = 0x1B00_0000, AllocationStart = 0x1B00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        new() { StartSegment = 0x1C00_0000, AllocationStart = 0x1C00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        new() { StartSegment = 0x1D00_0000, AllocationStart = 0x1D00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
    ];

    private static readonly ulong[] s_fillPointers = [0x1111, 0x2222, 0x3333, 0x4444, 0x5555, 0x6666, 0x7777];

    private static ISOSDacInterface8 CreateWksDac8(MockTarget.Architecture arch)
    {
        return new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapWks(gc =>
                {
                    gc.Generations = s_generations;
                    gc.FillPointers = s_fillPointers;
                })
                .Build(),
            legacyObj: null);
    }

    private static ISOSDacInterface8 CreateSvrDac8(MockTarget.Architecture arch, out ulong heapAddr)
    {
        return new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapSvr(gc =>
                {
                    gc.Generations = s_generations;
                    gc.FillPointers = s_fillPointers;
                }, out heapAddr)
                .Build(),
            legacyObj: null);
    }

    private static ulong SignExtend(ulong value, MockTarget.Architecture arch)
    {
        if (arch.Is64Bit)
            return value;

        return (ulong)(long)(int)(uint)value;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNumberGenerations_ReturnsCorrectCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateWksDac8(arch);

        uint numGenerations;
        int hr = dac8.GetNumberGenerations(&numGenerations);
        Assert.Equal(S_OK, hr);
        Assert.Equal((uint)s_generations.Length, numGenerations);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNumberGenerations_WithFiveGenerations(MockTarget.Architecture arch)
    {
        var fiveGenerations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0xA000_0000, AllocationStart = 0xA000_1000, AllocContextPointer = 0xA000_2000, AllocContextLimit = 0xA000_3000 },
            new() { StartSegment = 0xB000_0000, AllocationStart = 0xB000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xC000_0000, AllocationStart = 0xC000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xD000_0000, AllocationStart = 0xD000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xE000_0000, AllocationStart = 0xE000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ISOSDacInterface8 dac8 = new SOSDacImpl(
            new TestPlaceholderTarget.Builder(arch)
                .AddGCHeapWks(gc =>
                {
                    gc.Generations = fiveGenerations;
                    gc.FillPointers = s_fillPointers;
                })
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
        ISOSDacInterface8 dac8 = CreateWksDac8(arch);

        uint needed;
        int hr = dac8.GetGenerationTable(0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_generations.Length, needed);

        DacpGenerationData* genData = stackalloc DacpGenerationData[(int)needed];
        hr = dac8.GetGenerationTable(needed, genData, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < s_generations.Length; i++)
        {
            Assert.Equal(SignExtend(s_generations[i].StartSegment, arch), (ulong)genData[i].start_segment);
            Assert.Equal(SignExtend(s_generations[i].AllocationStart, arch), (ulong)genData[i].allocation_start);
            Assert.Equal(SignExtend(s_generations[i].AllocContextPointer, arch), (ulong)genData[i].allocContextPtr);
            Assert.Equal(SignExtend(s_generations[i].AllocContextLimit, arch), (ulong)genData[i].allocContextLimit);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGenerationTable_InsufficientBuffer_ReturnsSFalseAndNeededCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateWksDac8(arch);

        uint needed;
        DacpGenerationData* smallBuffer = stackalloc DacpGenerationData[2];
        int hr = dac8.GetGenerationTable(2, smallBuffer, &needed);

        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_generations.Length, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFinalizationFillPointers_ReturnsCorrectData(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateWksDac8(arch);

        uint needed;
        int hr = dac8.GetFinalizationFillPointers(0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_fillPointers.Length, needed);

        ClrDataAddress* ptrs = stackalloc ClrDataAddress[(int)needed];
        hr = dac8.GetFinalizationFillPointers(needed, ptrs, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < s_fillPointers.Length; i++)
        {
            Assert.Equal(SignExtend(s_fillPointers[i], arch), (ulong)ptrs[i]);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFinalizationFillPointers_InsufficientBuffer_ReturnsSFalseAndNeededCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateWksDac8(arch);

        uint needed;
        ClrDataAddress* smallBuffer = stackalloc ClrDataAddress[3];
        int hr = dac8.GetFinalizationFillPointers(3, smallBuffer, &needed);

        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_fillPointers.Length, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGenerationTableSvr_ReturnsCorrectData(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateSvrDac8(arch, out ulong heapAddr);

        uint needed;
        int hr = dac8.GetGenerationTableSvr((ClrDataAddress)heapAddr, 0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_generations.Length, needed);

        DacpGenerationData* genData = stackalloc DacpGenerationData[(int)needed];
        hr = dac8.GetGenerationTableSvr((ClrDataAddress)heapAddr, needed, genData, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < s_generations.Length; i++)
        {
            Assert.Equal(SignExtend(s_generations[i].StartSegment, arch), (ulong)genData[i].start_segment);
            Assert.Equal(SignExtend(s_generations[i].AllocationStart, arch), (ulong)genData[i].allocation_start);
            Assert.Equal(SignExtend(s_generations[i].AllocContextPointer, arch), (ulong)genData[i].allocContextPtr);
            Assert.Equal(SignExtend(s_generations[i].AllocContextLimit, arch), (ulong)genData[i].allocContextLimit);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGenerationTableSvr_InsufficientBuffer_ReturnsSFalseAndNeededCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateSvrDac8(arch, out ulong heapAddr);

        uint needed;
        DacpGenerationData* smallBuffer = stackalloc DacpGenerationData[2];
        int hr = dac8.GetGenerationTableSvr((ClrDataAddress)heapAddr, 2, smallBuffer, &needed);

        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_generations.Length, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFinalizationFillPointersSvr_ReturnsCorrectData(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateSvrDac8(arch, out ulong heapAddr);

        uint needed;
        int hr = dac8.GetFinalizationFillPointersSvr((ClrDataAddress)heapAddr, 0, null, &needed);
        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_fillPointers.Length, needed);

        ClrDataAddress* ptrs = stackalloc ClrDataAddress[(int)needed];
        hr = dac8.GetFinalizationFillPointersSvr((ClrDataAddress)heapAddr, needed, ptrs, &needed);
        Assert.Equal(S_OK, hr);

        for (int i = 0; i < s_fillPointers.Length; i++)
        {
            Assert.Equal(SignExtend(s_fillPointers[i], arch), (ulong)ptrs[i]);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFinalizationFillPointersSvr_InsufficientBuffer_ReturnsSFalseAndNeededCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface8 dac8 = CreateSvrDac8(arch, out ulong heapAddr);

        uint needed;
        ClrDataAddress* smallBuffer = stackalloc ClrDataAddress[3];
        int hr = dac8.GetFinalizationFillPointersSvr((ClrDataAddress)heapAddr, 3, smallBuffer, &needed);

        Assert.Equal(S_FALSE, hr);
        Assert.Equal((uint)s_fillPointers.Length, needed);
    }
}
