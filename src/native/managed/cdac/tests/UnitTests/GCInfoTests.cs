// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GCInfoTests
{
    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64, false, new byte[] { 0x0A, 0x80, 0x50, 0x1B, 0xE2, 0x38, 0x21, 0x8C, 0x0B, 0x62, 0x1C, 0x88, 0x38 })]
    [InlineData(RuntimeInfoArchitecture.Arm, false, new byte[] { 0x0A, 0x80, 0x80, 0x31, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.Arm64, false, new byte[] { 0x0A, 0x00, 0xA1, 0x36, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, false, new byte[] { 0x0A, 0x00, 0xA1, 0x36, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.RiscV64, false, new byte[] { 0x0A, 0x00, 0xA1, 0x36, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.Wasm, false, new byte[] { 0x84, 0x00, 0xC2, 0x78, 0x87, 0x78, 0x46, 0x78, 0x03, 0x5E, 0x00 })]
    [InlineData(RuntimeInfoArchitecture.X64, true, new byte[] { 0x0A, 0x40, 0xA8, 0x0D, 0x71, 0x9C, 0x10, 0xC6, 0x05, 0x31, 0x0E, 0x44, 0x1C })]
    public void StackSlotsUseFlagsThenBaseThenOffset(RuntimeInfoArchitecture architecture, bool interpreter, byte[] encoded)
    {
        IGCInfoDecoder decoder = CreateDecoder(architecture, interpreter, encoded, 6);
        IReadOnlyList<GCSlotLifetime> slots = decoder.GetSlotLifetimes();
        Assert.Equal(8, slots.Count);
        int slotSize = interpreter ? 1 : architecture is RuntimeInfoArchitecture.Arm or RuntimeInfoArchitecture.Wasm ? 4 : 8;
        for (int i = 0; i < slots.Count; i++)
        {
            Assert.False(slots[i].IsRegister);
            int offset = architecture == RuntimeInfoArchitecture.Wasm ? -8 + i % 2 : i % 2 == 0 ? 8 : -8;
            Assert.Equal(offset * slotSize, slots[i].SpOffset);
            Assert.Equal(architecture == RuntimeInfoArchitecture.Wasm ? 2u : (uint)(1 + i % 2), slots[i].BaseRegister);
            Assert.Equal((uint)(3 - i / 2) | 4u, slots[i].GcFlags);
            Assert.Equal(0u, slots[i].BeginOffset);
            Assert.Equal(decoder.GetCodeLength(), slots[i].EndOffset);
        }
    }

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64, 5)]
    [InlineData(RuntimeInfoArchitecture.X64, 6)]
    [InlineData(RuntimeInfoArchitecture.Arm, 5)]
    [InlineData(RuntimeInfoArchitecture.Arm, 6)]
    [InlineData(RuntimeInfoArchitecture.Arm64, 5)]
    [InlineData(RuntimeInfoArchitecture.Arm64, 6)]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, 5)]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, 6)]
    [InlineData(RuntimeInfoArchitecture.RiscV64, 5)]
    [InlineData(RuntimeInfoArchitecture.RiscV64, 6)]
    public void RegisterSlotsUseFlagsFirstAndBoundedIntegers(RuntimeInfoArchitecture architecture, int version)
    {
        bool arm = architecture == RuntimeInfoArchitecture.Arm;
        bool x64 = architecture == RuntimeInfoArchitecture.X64;
        int registerBits = arm ? 4 : 5;
        int absoluteBase = arm ? 2 : 3;
        int deltaBase = arm ? 1 : 2;
        List<(int Register, int Flags)> registers = [];
        for (int flags = 3; flags >= 0; flags--)
        {
            foreach (int register in new[] { 0, 1, 3, 4, 7, 8, 15, 16, 23, 24, 30, 31 })
            {
                if (register >= (1 << registerBits) || flags == 3 && register < 8)
                    continue;
                registers.Add((register, flags));
            }
        }
        List<byte> bytes = [];
        int bitCount = 0;
        Write(0, 1);
        Write(1, 1);
        WriteUnsigned(2, arm ? 7 : 8);
        WriteUnsigned(1, x64 ? 2 : 3);
        Write(0, 1); // Safe point at normalized offset zero.
        Write(1, 1);
        WriteUnsigned(registers.Count, x64 || arm ? 2 : 3);
        Write(0, 1);
        for (int i = 0; i < registers.Count; i++)
        {
            (int register, int flags) = registers[i];
            bool emitFlags = i == 0 || registers[i - 1].Flags != 0;
            bool absolute = version < 6 ? emitFlags : i == 0 || flags != registers[i - 1].Flags;
            int value = absolute ? register : register - registers[i - 1].Register - 1;
            int encodingBase = absolute ? absoluteBase : deltaBase;
            if (version >= 6)
            {
                if (emitFlags)
                    Write(flags, i == 0 || (registers[i - 1].Flags & 2) != 0 ? 2 : 1);
                bool extended = value >= (1 << encodingBase);
                Write((value & ((1 << encodingBase) - 1)) | (extended ? 1 << encodingBase : 0), encodingBase + 1);
                if (extended)
                    Write(value >> encodingBase, registerBits - encodingBase);
            }
            else
            {
                WriteUnsigned(value, encodingBase);
                if (emitFlags)
                    Write(flags, 2);
            }
        }
        Write(0, 1); // Direct live-state bitmap.
        foreach ((int register, int flags) in registers)
            Write(1, 1);

        IGCInfoDecoder decoder = CreateDecoder(architecture, false, bytes.ToArray(), (uint)version);
        IReadOnlyList<GCSlotLifetime> slots = decoder.GetSlotLifetimes();
        Assert.Equal(registers.Count, slots.Count);
        for (int i = 0; i < registers.Count; i++)
        {
            Assert.True(slots[i].IsRegister);
            Assert.Equal((uint)registers[i].Register, slots[i].RegisterNumber);
            Assert.Equal((uint)registers[i].Flags, slots[i].GcFlags);
            Assert.Equal(0u, slots[i].BeginOffset);
            Assert.Equal(1u, slots[i].EndOffset);
        }

        void Write(int value, int count)
        {
            for (int i = 0; i < count; i++, bitCount++)
            {
                if (bitCount % 8 == 0)
                    bytes.Add(0);
                bytes[bitCount / 8] |= (byte)(((value >> i) & 1) << (bitCount % 8));
            }
        }

        void WriteUnsigned(int value, int encodingBase)
        {
            do
            {
                int chunk = value & ((1 << encodingBase) - 1);
                value >>>= encodingBase;
                Write(chunk | (value == 0 ? 0 : 1 << encodingBase), encodingBase + 1);
            } while (value != 0);
        }
    }

    [Theory]
    [InlineData(false, new byte[] { 0x0A, 0x80, 0xB0, 0x10, 0x88, 0x00 })]
    [InlineData(true, new byte[] { 0x0A, 0x40, 0x58, 0x08, 0x44 })]
    public void NonWasmVersion5StackSlotsRemainReadable(bool interpreter, byte[] encoded)
    {
        IGCInfoDecoder decoder = CreateDecoder(RuntimeInfoArchitecture.X64, interpreter, encoded, 5);
        IReadOnlyList<GCSlotLifetime> slots = decoder.GetSlotLifetimes();

        Assert.Equal(2u, decoder.GetCodeLength());
        Assert.Equal(2, slots.Count);
        Assert.Equal(interpreter ? 8 : 64, slots[0].SpOffset);
        Assert.Equal(1u, slots[0].BaseRegister);
        Assert.Equal(interpreter ? 16 : 128, slots[1].SpOffset);
        Assert.Equal(2u, slots[1].BaseRegister);
        Assert.All(slots, slot =>
        {
            Assert.False(slot.IsRegister);
            Assert.Equal(0u, slot.GcFlags);
            Assert.Equal(0u, slot.BeginOffset);
            Assert.Equal(2u, slot.EndOffset);
        });
    }

    [Theory]
    [InlineData(new byte[] { 0x04 }, 0, 0u, 0u)]
    [InlineData(new byte[] { 0x21, 0x08, 0x02, 0x00 }, 8, 1u, 0u)]
    [InlineData(new byte[] { 0x41, 0x08, 0x02, 0x00 }, 8, 1u, 0u)]
    [InlineData(new byte[] { 0x01, 0x08, 0x01 }, 0, 0u, 1u)]
    public void WasmHeadersDecodePrologAndFrameRegister(byte[] encoded, int genericContextOffset,
        uint prologSize, uint safePoints)
    {
        IGCInfoDecoder decoder = CreateDecoder(RuntimeInfoArchitecture.Wasm, false, encoded, 6);
        GCInfoHeader header = decoder.GetHeader();

        Assert.Equal(2u, header.CodeSize);
        Assert.Equal(2u, header.StackBaseRegister);
        Assert.Equal(prologSize, header.PrologSize);
        Assert.Equal(genericContextOffset == 0 ? null : new SpecialSlot(genericContextOffset), header.GenericsInstContext);
        Assert.Equal((int)safePoints, decoder.GetSafePoints().Count);
        if (safePoints != 0)
            Assert.Equal(0u, Assert.Single(decoder.GetSafePoints()));
        Assert.Empty(decoder.GetSlotLifetimes());
    }

    private static IGCInfoDecoder CreateDecoder(RuntimeInfoArchitecture architecture, bool interpreter, byte[] encoded, uint version)
    {
        MockTarget.Architecture arch = new()
        {
            IsLittleEndian = true,
            Is64Bit = architecture is not (RuntimeInfoArchitecture.Arm or RuntimeInfoArchitecture.Wasm),
        };
        TestPlaceholderTarget.Builder builder = new(arch);
        const ulong GcInfoAddress = 0x10000;
        byte[] data = new byte[encoded.Length + 8];
        encoded.CopyTo(data, 0);
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GcInfoAddress,
            Data = data,
            Name = "GC slot flag runs",
        });
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(c => c.GetTargetArchitecture()).Returns(architecture);
        Target target = builder.AddMockContract(runtimeInfo).Build();
        TargetPointer address = new(GcInfoAddress);
        return interpreter
            ? new GcInfoDecoder<InterpreterGCInfoTraits>(target, address, version)
            : architecture switch
            {
                RuntimeInfoArchitecture.X64 => new GcInfoDecoder<AMD64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.Arm => new GcInfoDecoder<ARMGCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.Arm64 => new GcInfoDecoder<ARM64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.LoongArch64 => new GcInfoDecoder<LoongArch64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.RiscV64 => new GcInfoDecoder<RISCV64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.Wasm => new GcInfoDecoder<WasmGCInfoTraits>(target, address, version),
                _ => throw new InvalidOperationException(),
            };
    }
}
