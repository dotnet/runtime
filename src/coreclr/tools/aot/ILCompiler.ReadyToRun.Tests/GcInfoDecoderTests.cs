// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using ILCompiler.Reflection.ReadyToRun;
using ILCompiler.Reflection.ReadyToRun.Amd64;
using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class GcInfoDecoderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Amd64SlotTableRetainsRegisterPresenceBit(int rootFlags)
    {
        byte[] encoded = [0x08, 0xC0, (byte)(rootFlags << 7), (byte)(rootFlags >> 1), 0x00, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, Machine.Amd64, 5);

        Assert.Equal(1u, info.SlotTable.NumRegisters);
        Assert.Equal(0u, info.SlotTable.NumStackSlots);
        Assert.Equal(0u, info.SlotTable.NumUntracked);
        GcSlotTable.GcSlot slot = Assert.Single(info.SlotTable.GcSlots);
        Assert.Equal(0, slot.RegisterNumber);
        Assert.Null(slot.StackSlot);
        Assert.Equal((GcSlotFlags)rootFlags, slot.Flags);
    }

    [Theory]
    [InlineData(-1, 8, false, 0x02)]
    [InlineData(0, 8, false, 0x02)]
    [InlineData(1, 8, false, 0x02)]
    [InlineData(2, 8, false, 0x02)]
    [InlineData(3, 8, false, 0x02)]
    [InlineData(0, -8, false, 0x06)]
    [InlineData(0, 124, false, 0x3F)]
    [InlineData(0, -128, false, 0x48)]
    [InlineData(-1, 8, true, 0x02)]
    [InlineData(0, 8, true, 0x02)]
    [InlineData(1, 8, true, 0x02)]
    [InlineData(2, 8, true, 0x02)]
    [InlineData(3, 8, true, 0x02)]
    [InlineData(0, -8, true, 0x06)]
    [InlineData(0, 124, true, 0x3F)]
    [InlineData(0, -128, true, 0x48)]
    public void WasmSlotTableUsesImplicitFrameBaseAndPointerUnits(int rootFlags, int offset, bool slimHeader, int encodedOffset)
    {
        int descriptor = rootFlags | (encodedOffset << 2);
        byte[] encoded = (slimHeader, rootFlags < 0) switch
        {
            (true, true) => [0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            (false, true) => [0x81, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            (true, false) => [0x0A, 0x40, 0x20, (byte)(descriptor << 3), (byte)(descriptor >> 5), 0x00, 0x00, 0x00],
            (false, false) => [0x81, 0x10, 0x00, 0x08, 0x04, (byte)descriptor, (byte)(descriptor >> 8), 0x00],
        };
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, WasmMachine.Wasm32, 5);

        Assert.Equal(2, info.CodeLength);
        Assert.Equal(2u, info.StackBaseRegister);
        Assert.Equal(0u, info.SlotTable.NumRegisters);
        Assert.Equal(0u, info.SlotTable.NumStackSlots);
        Assert.Equal(rootFlags < 0 ? 0u : 1u, info.SlotTable.NumUntracked);
        if (rootFlags < 0)
        {
            Assert.Empty(info.SlotTable.GcSlots);
            Assert.Equal(slimHeader ? 2 : 4, info.Size);
        }
        else
        {
            GcSlotTable.GcSlot slot = Assert.Single(info.SlotTable.GcSlots);
            Assert.Equal(offset, slot.StackSlot.SpOffset);
            Assert.Equal(GcStackSlotBase.GC_FRAMEREG_REL, slot.StackSlot.Base);
            Assert.Equal((GcSlotFlags)rootFlags | GcSlotFlags.GC_SLOT_UNTRACKED, slot.Flags);
            Assert.Equal(slimHeader ? 5 : encodedOffset < 16 ? 6 : 7, info.Size);
        }
    }

    [Theory]
    [InlineData(0, 0x08, 0x00)]
    [InlineData(4, 0x08, 0x01)]
    [InlineData(8, 0x08, 0x06)]
    [InlineData(15, 0xC8, 0x07)]
    [InlineData(16, 0x08, 0x0A)]
    public void WasmSlotOffsetsUseUnsignedPointerUnitDeltas(int delta, byte firstByte, byte secondByte)
    {
        byte[] encoded = [0x81, 0x10, 0x00, 0x08, 0x08, firstByte, secondByte, 0x00];
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, WasmMachine.Wasm32, 5);

        Assert.Equal(0u, info.SlotTable.NumRegisters);
        Assert.Equal(0u, info.SlotTable.NumStackSlots);
        Assert.Equal(2u, info.SlotTable.NumUntracked);
        Assert.Equal(2, info.SlotTable.GcSlots.Count);
        Assert.Equal(7, info.Size);
        for (int i = 0; i < info.SlotTable.GcSlots.Count; i++)
        {
            GcSlotTable.GcSlot slot = info.SlotTable.GcSlots[i];
            Assert.Equal(i == 0 ? 8 : 8 + delta * 4, slot.StackSlot.SpOffset);
            Assert.Equal(GcStackSlotBase.GC_FRAMEREG_REL, slot.StackSlot.Base);
            Assert.Equal(GcSlotFlags.GC_SLOT_UNTRACKED, slot.Flags);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void WasmSlotOffsetsUseDeltasForAllFlagTypes(int rootFlags)
    {
        byte[] encoded = [0x01, (byte)(0x01 | (rootFlags << 5)), (byte)(rootFlags == 0 ? 0x09 : 0x21 | (rootFlags << 3)), 0x00];
        GcInfo info = DecodeWasmSlotTable(encoded);
        GcSlotTable table = info.SlotTable;

        Assert.Equal(2u, table.NumUntracked);
        Assert.Equal(5, info.Size);
        for (int i = 0; i < table.GcSlots.Count; i++)
        {
            Assert.Equal(8 + i * 4, table.GcSlots[i].StackSlot.SpOffset);
            Assert.Equal((GcSlotFlags)rootFlags | GcSlotFlags.GC_SLOT_UNTRACKED, table.GcSlots[i].Flags);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WasmSlotOffsetsResetAtSectionAndFlagBoundaries(bool splitSections)
    {
        byte[] encoded = splitSections
            ? [0x83, 0x80, 0x41, 0x00]
            : [0x01, 0x64, 0x39, 0x34, 0x27, 0x64, 0x28, 0x40, 0x0A];
        int[] offsets = splitSections ? [12, 8] : [8, 12, -8, 4, 128, 132, -128, -124];
        int[] flags = splitSections ? [0, 0] : [3, 3, 2, 2, 1, 1, 0, 0];
        GcInfo info = DecodeWasmSlotTable(encoded);
        GcSlotTable table = info.SlotTable;

        Assert.Equal(splitSections ? 1u : 0u, table.NumStackSlots);
        Assert.Equal(splitSections ? 1u : 8u, table.NumUntracked);
        Assert.Equal(splitSections ? 6 : 11, info.Size);
        Assert.Equal(offsets.Length, table.GcSlots.Count);
        for (int i = 0; i < offsets.Length; i++)
        {
            GcSlotFlags expectedFlags = (GcSlotFlags)flags[i];
            if (!splitSections || i != 0)
            {
                expectedFlags |= GcSlotFlags.GC_SLOT_UNTRACKED;
            }
            Assert.Equal(offsets[i], table.GcSlots[i].StackSlot.SpOffset);
            Assert.Equal(expectedFlags, table.GcSlots[i].Flags);
        }
    }

    [Theory]
    [InlineData(Machine.Amd64, true, 5u)]
    [InlineData(Machine.Amd64, false, uint.MaxValue)]
    [InlineData(WasmMachine.Wasm32, true, 2u)]
    [InlineData(WasmMachine.Wasm32, false, uint.MaxValue)]
    public void SlimHeaderUsesImplicitDefaultFrameRegister(Machine machine, bool hasFrameRegister, uint frameRegister)
    {
        byte[] encoded = [hasFrameRegister ? (byte)0x0A : (byte)0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, machine, 5);

        Assert.Equal(2, info.CodeLength);
        Assert.Equal(frameRegister, info.StackBaseRegister);
        Assert.Empty(info.SlotTable.GcSlots);
        Assert.Equal(2, info.Size);
    }

    [Theory]
    [InlineData(0xA1)]
    [InlineData(0xC1)]
    public void WasmFatHeaderPreservesGenericContext(byte header)
    {
        byte[] encoded = [header, 0x10, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, WasmMachine.Wasm32, 5);

        Assert.Equal(2, info.CodeLength);
        Assert.Equal(2u, info.StackBaseRegister);
        Assert.Equal(8, info.GenericsInstContextStackSlot);
        Assert.Equal(1u, info.ValidRangeStart);
        Assert.Empty(info.SlotTable.GcSlots);
        Assert.Equal(5, info.Size);
    }

    [Theory]
    [InlineData(0x00, 0x00, 0u, 2)]
    [InlineData(0x0E, 0x00, 224u, 2)]
    [InlineData(0x7F, 0x00, 2032u, 2)]
    [InlineData(0x80, 0x01, 2048u, 3)]
    public void WasmUnwindFrameSizeUsesSixteenByteUnits(byte firstByte, byte secondByte, uint frameSize, int size)
    {
        byte[] encoded = size == 2
            ? [firstByte, 0x02]
            : [firstByte, secondByte, 0x02];
        using MemoryStream stream = new(encoded);
        ILCompiler.Reflection.ReadyToRun.Wasm32.UnwindInfo info = new(new NativeReader(stream), 0);

        Assert.Equal(frameSize, info.BytesUnwind);
        Assert.Equal(4u, info.VirtualIPCount);
        Assert.Equal(size, info.Size);
    }

    [Theory]
    [InlineData(0, 0x00000000u, 0x00001234u)]
    [InlineData(1, 0x80000004u, 0x00005678u)]
    [InlineData(2, 0x00010008u, 0x00009ABCu)]
    public void WasmRuntimeFunctionBytePlanesPreserveWords(int entryIndex, uint virtualIP, uint unwindRva)
    {
        byte[] encoded =
        [
            0x55, 0xAA,
            0x00, 0x04, 0x08,
            0x00, 0x00, 0x00,
            0x00, 0x00, 0x01,
            0x00, 0x80, 0x00,
            0x34, 0x78, 0xBC,
            0x12, 0x56, 0x9A,
            0x00, 0x00, 0x00,
            0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x02, 0x01, 0x00, 0x00,
        ];
        using MemoryStream stream = new(encoded);
        NativeReader reader = new(stream);

        Assert.Equal(virtualIP, reader.ReadUInt32FromBytePlanes(2, 3, entryIndex, 0));
        Assert.Equal(unwindRva, reader.ReadUInt32FromBytePlanes(2, 3, entryIndex, 4));
        int offset = 26;
        Assert.Equal(uint.MaxValue, reader.ReadUInt32(ref offset));
        Assert.Equal(258u, reader.ReadUInt32(ref offset));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 1, -1, 0)]
    [InlineData(0, 1, 1, 0)]
    [InlineData(0, 1, 0, -1)]
    [InlineData(0, 1, 0, 5)]
    [InlineData(-1, 1, 0, 0)]
    public void WasmRuntimeFunctionBytePlanesRejectInvalidIndices(int offset, int count, int index, int plane)
    {
        using MemoryStream stream = new(new byte[8]);
        NativeReader reader = new(stream);

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadUInt32FromBytePlanes(offset, count, index, plane));
    }

    private static GcInfo DecodeWasmSlotTable(byte[] slotTable)
    {
        byte[] encoded = new byte[slotTable.Length + 2];
        encoded[0] = 0x0A;
        for (int i = 0; i < slotTable.Length; i++)
        {
            encoded[i + 1] |= (byte)(slotTable[i] << 6);
            encoded[i + 2] |= (byte)(slotTable[i] >> 2);
        }
        using MemoryStream stream = new(encoded);

        return new GcInfo(new NativeReader(stream), 0, WasmMachine.Wasm32, 5);
    }
}
