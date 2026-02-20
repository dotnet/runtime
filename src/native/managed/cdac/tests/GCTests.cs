// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GCTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHeapData_ReturnsCorrectGenerationTable(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0xAA00_0000, AllocationStart = 0xAA00_1000, AllocContextPointer = 0xAA00_2000, AllocContextLimit = 0xAA00_3000 },
            new() { StartSegment = 0xBB00_0000, AllocationStart = 0xBB00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xCC00_0000, AllocationStart = 0xCC00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xDD00_0000, AllocationStart = 0xDD00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc => gc
                .SetGenerations(generations)
                .SetFillPointers(fillPointers))
            .Build();
        IGC gc = target.Contracts.GC;

        GCHeapData heapData = gc.GetHeapData();

        Assert.Equal(generations.Length, heapData.GenerationTable.Count);
        for (int i = 0; i < generations.Length; i++)
        {
            Assert.Equal(generations[i].StartSegment, (ulong)heapData.GenerationTable[i].StartSegment);
            Assert.Equal(generations[i].AllocationStart, (ulong)heapData.GenerationTable[i].AllocationStart);
            Assert.Equal(generations[i].AllocContextPointer, (ulong)heapData.GenerationTable[i].AllocationContextPointer);
            Assert.Equal(generations[i].AllocContextLimit, (ulong)heapData.GenerationTable[i].AllocationContextLimit);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHeapData_ReturnsCorrectFillPointers(MockTarget.Architecture arch)
    {
        var generations = new GCHeapBuilder.GenerationInput[]
        {
            new() { StartSegment = 0xAA00_0000, AllocationStart = 0xAA00_1000, AllocContextPointer = 0xAA00_2000, AllocContextLimit = 0xAA00_3000 },
            new() { StartSegment = 0xBB00_0000, AllocationStart = 0xBB00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xCC00_0000, AllocationStart = 0xCC00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xDD00_0000, AllocationStart = 0xDD00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1111, 0x2222, 0x3333, 0x4444, 0x5555, 0x6666, 0x7777];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc => gc
                .SetGenerations(generations)
                .SetFillPointers(fillPointers))
            .Build();
        IGC gc = target.Contracts.GC;

        GCHeapData heapData = gc.GetHeapData();

        Assert.Equal(fillPointers.Length, heapData.FillPointers.Count);
        for (int i = 0; i < fillPointers.Length; i++)
        {
            Assert.Equal(fillPointers[i], (ulong)heapData.FillPointers[i]);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHeapData_WithFiveGenerations(MockTarget.Architecture arch)
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

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc => gc
                .SetGenerations(generations)
                .SetFillPointers(fillPointers))
            .Build();
        IGC gc = target.Contracts.GC;

        GCHeapData heapData = gc.GetHeapData();

        Assert.Equal(5, heapData.GenerationTable.Count);
        for (int i = 0; i < generations.Length; i++)
        {
            Assert.Equal(generations[i].StartSegment, (ulong)heapData.GenerationTable[i].StartSegment);
            Assert.Equal(generations[i].AllocationStart, (ulong)heapData.GenerationTable[i].AllocationStart);
        }

        Assert.Equal(fillPointers.Length, heapData.FillPointers.Count);
        for (int i = 0; i < fillPointers.Length; i++)
        {
            Assert.Equal(fillPointers[i], (ulong)heapData.FillPointers[i]);
        }
    }
}
