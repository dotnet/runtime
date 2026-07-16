// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class SOSDacInterfaceHeapAllocDataTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };

    [Fact]
    public void GetHeapAllocData_WorkstationUsesFourDacGenerationSlots()
    {
        GCHeapData heap = new() { GenerationTable = CreateGenerations(100) };
        var gc = new Mock<IGC>();
        gc.Setup(g => g.GetGCIdentifiers()).Returns([GCIdentifiers.Workstation]);
        gc.Setup(g => g.GetHeapData()).Returns(heap);
        ISOSDacInterface sos = CreateSOSDac(gc);

        uint needed;
        Assert.Equal(HResults.S_OK, sos.GetHeapAllocData(0, null, &needed));
        Assert.Equal(1u, needed);
        gc.Verify(g => g.GetHeapData(), Times.Never);

        DacpGenerationAllocData data = default;
        Assert.Equal(HResults.S_OK, sos.GetHeapAllocData(1, &data, &needed));
        for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS; i++)
        {
            Assert.Equal((ulong)(100 + i), (ulong)data[i].allocBytes);
            Assert.Equal((ulong)(200 + i), (ulong)data[i].allocBytesLoh);
        }

        data[0].allocBytes = new ClrDataAddress(0xfeed);
        Assert.Equal(HResults.S_OK, sos.GetHeapAllocData(0, &data, &needed));
        Assert.Equal(0xfeedul, (ulong)data[0].allocBytes);
        Assert.Equal(HResults.E_INVALIDARG, sos.GetHeapAllocData(0, null, null));
    }

    [Fact]
    public void GetHeapAllocData_ServerReturnsOneRecordPerHeap()
    {
        TargetPointer firstHeap = new(0x3000);
        TargetPointer secondHeap = new(0x4000);
        var gc = new Mock<IGC>();
        gc.Setup(g => g.GetGCIdentifiers()).Returns([GCIdentifiers.Server]);
        gc.Setup(g => g.GetGCHeaps()).Returns([firstHeap, secondHeap]);
        gc.Setup(g => g.GetHeapData(firstHeap)).Returns(new GCHeapData { GenerationTable = CreateGenerations(1000) });
        gc.Setup(g => g.GetHeapData(secondHeap)).Returns(new GCHeapData { GenerationTable = CreateGenerations(2000) });
        ISOSDacInterface sos = CreateSOSDac(gc);

        uint needed;
        Assert.Equal(HResults.S_OK, sos.GetHeapAllocData(0, null, &needed));
        Assert.Equal(2u, needed);
        gc.Verify(g => g.GetHeapData(It.IsAny<TargetPointer>()), Times.Never);

        DacpGenerationAllocData* data = stackalloc DacpGenerationAllocData[2];
        Assert.Equal(HResults.S_OK, sos.GetHeapAllocData(2, data, &needed));
        Assert.Equal(1000ul, (ulong)data[0][0].allocBytes);
        Assert.Equal(1003ul, (ulong)data[0][3].allocBytes);
        Assert.Equal(2000ul, (ulong)data[1][0].allocBytes);
        Assert.Equal(2003ul, (ulong)data[1][3].allocBytes);

        DacpGenerationAllocData* partial = stackalloc DacpGenerationAllocData[2];
        Assert.Equal(HResults.S_OK, sos.GetHeapAllocData(1, partial, &needed));
        Assert.Equal(2u, needed);
        Assert.Equal(1000ul, (ulong)partial[0][0].allocBytes);
        Assert.Equal(2000ul, (ulong)partial[1][0].allocBytes);
    }

    private static ISOSDacInterface CreateSOSDac(Mock<IGC> gc)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(gc)
            .Build();
        return new SOSDacImpl(target, legacyObj: null);
    }

    private static GCGenerationData[] CreateGenerations(long start)
        =>
        [
            new() { AllocationBytes = start, AllocationBytesLoh = start + 100 },
            new() { AllocationBytes = start + 1, AllocationBytesLoh = start + 101 },
            new() { AllocationBytes = start + 2, AllocationBytesLoh = start + 102 },
            new() { AllocationBytes = start + 3, AllocationBytesLoh = start + 103 },
            new() { AllocationBytes = start + 9999, AllocationBytesLoh = start + 19999 },
        ];
}
