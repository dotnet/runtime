// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using Amd64Decoder = Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.GcInfoDecoder<Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.AMD64GCInfoTraits>;

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

    [Fact]
    public void ReadyToRunAccessorsExposeHeaderAndSlotTable()
    {
        // Fat AMD64 header: stack base register RBP, EnC preserved area 2, reverse P/Invoke slot -7,
        // then RAX (interior), sp+8 and an untracked pinned caller.sp-16.
        Amd64Decoder decoder = (Amd64Decoder)CreateDecoder(RuntimeInfoArchitecture.X64,
            [0x81, 0x46, 0x00, 0x22, 0x07, 0x60, 0x26, 0xA8, 0x00, 0xF8, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00]);

        Assert.False(decoder.IsSlimHeader);
        Assert.True(decoder.HasStackBaseRegister);
        Assert.Equal(2u, decoder.SizeOfEditAndContinuePreservedArea);
        Assert.Equal(-56, decoder.ReversePInvokeFrameStackSlot);
        Assert.Equal(1u, decoder.NumRegisters);
        Assert.Equal(1u, decoder.NumUntrackedSlots);
        Assert.Equal(2u, decoder.NumTrackedSlots);
        Assert.Equal(new Amd64Decoder.GcSlotDesc[]
        {
            Amd64Decoder.GcSlotDesc.CreateRegisterSlot(0, Amd64Decoder.GcSlotFlags.GC_SLOT_INTERIOR),
            Amd64Decoder.GcSlotDesc.CreateStackSlot(8, Amd64Decoder.GcStackSlotBase.GC_SP_REL, Amd64Decoder.GcSlotFlags.GC_SLOT_BASE),
            Amd64Decoder.GcSlotDesc.CreateStackSlot(-16, Amd64Decoder.GcStackSlotBase.GC_CALLER_SP_REL, Amd64Decoder.GcSlotFlags.GC_SLOT_PINNED),
        }, decoder.Slots);
    }

    [Fact]
    public void ReadyToRunAccessorsReportAbsentOptionalFields()
    {
        Amd64Decoder decoder = (Amd64Decoder)CreateDecoder(RuntimeInfoArchitecture.X64,
            [0x03, 0x40, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00]);

        Assert.False(decoder.HasStackBaseRegister);
        Assert.Null(decoder.SizeOfEditAndContinuePreservedArea);
        Assert.Null(decoder.ReversePInvokeFrameStackSlot);
        Assert.Empty(decoder.Slots);
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
