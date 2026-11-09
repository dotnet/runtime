// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GCInfoHeaderTests
{
    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64, 24u)]
    [InlineData(RuntimeInfoArchitecture.Arm, 12u)]
    [InlineData(RuntimeInfoArchitecture.Arm64, 24u)]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, 24u)]
    [InlineData(RuntimeInfoArchitecture.RiscV64, 24u)]
    public void GetHeaderDecodesOutgoingAreaBeforeSlotLifetimes(RuntimeInfoArchitecture architecture, uint expectedSize)
    {
        byte[] encoded = architecture == RuntimeInfoArchitecture.Arm
            ? [0x03, 0x40, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00]
            : [0x03, 0x40, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00];
        IGCInfoDecoder decoder = CreateDecoder(architecture, encoded);

        GCInfoHeader header = decoder.GetHeader();
        Assert.Equal(expectedSize, header.SizeOfStackParameterArea);
        Assert.Empty(decoder.GetSlotLifetimes());
        Assert.Equal(expectedSize, decoder.GetHeader().SizeOfStackParameterArea);
    }

    [Fact]
    public void GenericContextLookupStopsBeforeOutgoingArea()
    {
        // The outgoing-area encoding is deliberately unterminated.
        IGCInfoDecoder decoder = CreateDecoder(RuntimeInfoArchitecture.X64,
            [0x03, 0x40, 0x80, 0x88, 0x88, 0x88, 0x88, 0x88]);

        Assert.False(decoder.TryGetGenericContextStorage(GenericContextLoc.ThisPtr, 0, out _));
    }

    [Fact]
    public void SlimHeaderStopsBeforeSafePointCount()
    {
        // The safe-point count is deliberately unterminated.
        IGCInfoDecoder decoder = CreateDecoder(RuntimeInfoArchitecture.X64, [0x22, 0xF8]);

        GCInfoHeader header = decoder.GetHeader();
        Assert.Equal(8u, header.CodeSize);
        Assert.Equal(0u, header.SizeOfStackParameterArea);
    }

    private static IGCInfoDecoder CreateDecoder(RuntimeInfoArchitecture architecture, byte[] encoded)
    {
        MockTarget.Architecture arch = new()
        {
            IsLittleEndian = true,
            Is64Bit = architecture != RuntimeInfoArchitecture.Arm,
        };
        TestPlaceholderTarget.Builder builder = new(arch);
        const ulong GCInfoAddress = 0x10000;
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GCInfoAddress,
            Data = encoded,
            Name = "GCInfo header",
        });
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(c => c.GetTargetArchitecture()).Returns(architecture);
        Target target = builder.AddMockContract(runtimeInfo).Build();
        TargetPointer address = new(GCInfoAddress);
        return architecture switch
        {
            RuntimeInfoArchitecture.X64 => new GcInfoDecoder<AMD64GCInfoTraits>(target, address, 5),
            RuntimeInfoArchitecture.Arm => new GcInfoDecoder<ARMGCInfoTraits>(target, address, 5),
            RuntimeInfoArchitecture.Arm64 => new GcInfoDecoder<ARM64GCInfoTraits>(target, address, 5),
            RuntimeInfoArchitecture.LoongArch64 => new GcInfoDecoder<LoongArch64GCInfoTraits>(target, address, 5),
            RuntimeInfoArchitecture.RiscV64 => new GcInfoDecoder<RISCV64GCInfoTraits>(target, address, 5),
            _ => throw new System.InvalidOperationException(),
        };
    }
}
