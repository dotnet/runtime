// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using ILCompiler.Reflection.ReadyToRun;
using ILCompiler.Reflection.ReadyToRun.Amd64;
using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class GcInfoDecoderTests
{
    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 5)]
    [InlineData(2, 5)]
    [InlineData(3, 5)]
    [InlineData(0, 6)]
    [InlineData(1, 6)]
    [InlineData(2, 6)]
    [InlineData(3, 6)]
    public void Amd64SlotTableRetainsRegisterPresenceBit(int rootFlags, int version)
    {
        byte[] encoded = version < 6
            ? [0x08, 0xC0, (byte)(rootFlags << 7), (byte)(rootFlags >> 1), 0x00, 0x00, 0x00, 0x00]
            : [0x08, 0xC0, (byte)(rootFlags << 3), 0x00, 0x00, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, Machine.Amd64, version);

        Assert.Equal(1u, info.SlotTable.NumRegisters);
        Assert.Equal(0u, info.SlotTable.NumStackSlots);
        Assert.Equal(0u, info.SlotTable.NumUntracked);
        GcSlotTable.GcSlot slot = Assert.Single(info.SlotTable.GcSlots);
        Assert.Equal(0, slot.RegisterNumber);
        Assert.Null(slot.StackSlot);
        Assert.Equal((GcSlotFlags)rootFlags, slot.Flags);
    }

    public static IEnumerable<object[]> RegisterSlotCases()
    {
        foreach (Machine machine in new[] { Machine.Amd64, Machine.ArmThumb2, Machine.Arm64,
            Machine.I386, Machine.LoongArch64, Machine.RiscV64 })
        {
            foreach (int version in new[] { 5, 6 })
            {
                for (int flags = 0; flags < 4; flags++)
                    yield return [machine, version, flags];
            }
        }
    }

    [Theory]
    [MemberData(nameof(RegisterSlotCases))]
    public void RegisterSlotsUseFlagsFirstAndBoundedIntegers(Machine machine, int version, int flags)
    {
        int maxRegister = machine switch
        {
            Machine.I386 => 7,
            Machine.ArmThumb2 => 15,
            _ => 31,
        };
        for (int first = 0; first <= maxRegister; first++)
        {
            for (int last = first; last <= maxRegister; last++)
            {
                List<(int Register, int Flags)> registers = [(first, flags)];
                if (last != first)
                    registers.Add((last, flags));
                if (flags != 0)
                {
                    registers.Add((0, flags - 1));
                    registers.Add((maxRegister, flags - 1));
                }
                AssertRegisterSlots(machine, version, registers);
            }
        }

        List<(int Register, int Flags)> allRegisters = [];
        for (int runFlags = 3; runFlags >= 0; runFlags--)
        {
            for (int register = 0; register <= maxRegister; register++)
                allRegisters.Add((register, runFlags));
        }
        AssertRegisterSlots(machine, version, allRegisters);
    }

    private static void AssertRegisterSlots(Machine machine, int version, List<(int Register, int Flags)> registers)
    {
        SlotTableWriter writer = new(machine);
        writer.WriteCounts(0, 0, registers.Count);
        writer.WriteRegisters(registers, version);
        writer.WriteUnsigned(0, 3);
        using MemoryStream stream = new(writer.ToArray());
        GcInfo info = new(new NativeReader(stream), 0, machine, version);
        Assert.Equal((uint)registers.Count, info.SlotTable.NumRegisters);
        Assert.Equal(registers.Count, info.SlotTable.GcSlots.Count);
        for (int i = 0; i < registers.Count; i++)
        {
            Assert.Equal(registers[i].Register, info.SlotTable.GcSlots[i].RegisterNumber);
            Assert.Equal((GcSlotFlags)registers[i].Flags, info.SlotTable.GcSlots[i].Flags);
            Assert.Null(info.SlotTable.GcSlots[i].StackSlot);
        }
        Assert.Equal((writer.BitCount + 7) / 8, info.Size);
    }

    public static IEnumerable<object[]> StackSlotFlagCases()
    {
        Machine[] machines = [Machine.Amd64, Machine.ArmThumb2, Machine.Arm64, Machine.I386,
            Machine.LoongArch64, Machine.RiscV64, WasmMachine.Wasm32];
        foreach (Machine machine in machines)
        {
            foreach (bool tracked in new[] { false, true })
            {
                foreach (int count in new[] { 1, 2, 3, 4, 31, 32, 33, 65 })
                {
                    for (int mask = 0; mask < 16; mask++)
                    {
                        yield return [machine, tracked, count, mask];
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(StackSlotFlagCases))]
    public void StackSlotsUseFlagsThenBaseThenOffset(Machine machine, bool tracked, int count, int mask)
    {
        SlotTableWriter writer = new(machine);
        int[] counts = new int[4];
        int numSlots = 0;
        for (int flags = 0; flags < counts.Length; flags++)
        {
            counts[flags] = (mask & (1 << flags)) != 0 ? count : 0;
            numSlots += counts[flags];
        }
        writer.WriteCounts(tracked ? numSlots : 0, tracked ? 0 : numSlots);
        writer.WriteStackSlots(counts);
        if (tracked && numSlots != 0)
            writer.WriteUnsigned(0, 3); // No lifetime chunk pointers.

        using MemoryStream stream = new(writer.ToArray());
        GcInfo info = new(new NativeReader(stream), 0, machine, 6);
        Assert.Equal(0u, info.SlotTable.NumRegisters);
        Assert.Equal(tracked ? (uint)numSlots : 0u, info.SlotTable.NumStackSlots);
        Assert.Equal(tracked ? 0u : (uint)numSlots, info.SlotTable.NumUntracked);
        Assert.Equal(numSlots, info.SlotTable.GcSlots.Count);
        Assert.Equal((writer.BitCount + 7) / 8, info.Size);

        int slotIndex = 0;
        for (int flags = 3; flags >= 0; flags--)
        {
            for (int i = 0; i < counts[flags]; i++)
            {
                GcSlotTable.GcSlot slot = info.SlotTable.GcSlots[slotIndex++];
                Assert.Equal(writer.DenormalizeOffset(writer.GetStackOffset(i, count)), slot.StackSlot.SpOffset);
                Assert.Equal(writer.GetStackBase(i, count), slot.StackSlot.Base);
                Assert.Equal((GcSlotFlags)flags | (tracked ? 0 : GcSlotFlags.GC_SLOT_UNTRACKED), slot.Flags);
            }
        }
    }

    [Theory]
    [InlineData(Machine.Amd64)]
    [InlineData(Machine.ArmThumb2)]
    [InlineData(Machine.Arm64)]
    [InlineData(Machine.I386)]
    [InlineData(Machine.LoongArch64)]
    [InlineData(Machine.RiscV64)]
    public void LegacyStackSlotFlagsRemainReadable(Machine machine)
    {
        SlotTableWriter writer = new(machine);
        writer.WriteCounts(8, 8);
        writer.WriteLegacySlots();
        writer.WriteLegacySlots();
        writer.WriteUnsigned(0, 3);

        using MemoryStream stream = new(writer.ToArray());
        GcInfo info = new(new NativeReader(stream), 0, machine, 5);
        Assert.Equal(8u, info.SlotTable.NumStackSlots);
        Assert.Equal(8u, info.SlotTable.NumUntracked);
        Assert.Equal(16, info.SlotTable.GcSlots.Count);
        for (int i = 0; i < 16; i++)
        {
            GcSlotTable.GcSlot slot = info.SlotTable.GcSlots[i];
            Assert.Equal(writer.DenormalizeOffset(-8 + i % 8), slot.StackSlot.SpOffset);
            Assert.Equal((GcStackSlotBase)(i % 8 % 3), slot.StackSlot.Base);
            Assert.Equal((GcSlotFlags)(3 - i % 8 / 2) | (i < 8 ? 0 : GcSlotFlags.GC_SLOT_UNTRACKED), slot.Flags);
        }
        Assert.Equal((writer.BitCount + 7) / 8, info.Size);
    }

    [Theory]
    [InlineData(Machine.Amd64)]
    [InlineData(WasmMachine.Wasm32)]
    public void StackSlotFlagsAndBasesResetAtTrackedUntrackedBoundary(Machine machine)
    {
        SlotTableWriter writer = new(machine);
        writer.WriteCounts(2, 2);
        writer.WriteStackSlots([0, 2, 0, 0]);
        writer.WriteStackSlots([0, 2, 0, 0]);
        writer.WriteUnsigned(0, 3);

        using MemoryStream stream = new(writer.ToArray());
        GcInfo info = new(new NativeReader(stream), 0, machine, 6);
        Assert.Equal(4, info.SlotTable.GcSlots.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(writer.DenormalizeOffset(writer.GetStackOffset(i % 2, 2)), info.SlotTable.GcSlots[i].StackSlot.SpOffset);
            Assert.Equal(writer.GetStackBase(i % 2, 2), info.SlotTable.GcSlots[i].StackSlot.Base);
            Assert.Equal(GcSlotFlags.GC_SLOT_INTERIOR | (i < 2 ? 0 : GcSlotFlags.GC_SLOT_UNTRACKED), info.SlotTable.GcSlots[i].Flags);
        }
        Assert.Equal((writer.BitCount + 7) / 8, info.Size);
    }

    [Theory]
    [InlineData(Machine.Amd64, 0)]
    [InlineData(Machine.Amd64, 1)]
    [InlineData(Machine.Amd64, 2)]
    [InlineData(Machine.Amd64, 3)]
    [InlineData(WasmMachine.Wasm32, 0)]
    [InlineData(WasmMachine.Wasm32, 1)]
    [InlineData(WasmMachine.Wasm32, 2)]
    [InlineData(WasmMachine.Wasm32, 3)]
    public void StackSlotDeltasAllowZeroAndLargeGaps(Machine machine, int flags)
    {
        SlotTableWriter writer = new(machine);
        writer.WriteCounts(0, 4);
        int[] counts = new int[4];
        counts[flags] = 4;
        writer.WriteStackSlots(counts, [0, 0, 15, 256]);

        using MemoryStream stream = new(writer.ToArray());
        GcInfo info = new(new NativeReader(stream), 0, machine, 6);
        Assert.Equal(4, info.SlotTable.GcSlots.Count);
        int[] offsets = [0, 0, 15, 256];
        for (int i = 0; i < offsets.Length; i++)
            Assert.Equal(writer.DenormalizeOffset(offsets[i]), info.SlotTable.GcSlots[i].StackSlot.SpOffset);
        Assert.Equal((writer.BitCount + 7) / 8, info.Size);
    }

    [Fact]
    public void NonWasmVersion5StackSlotsRemainReadable()
    {
        byte[] encoded = [0x0A, 0x80, 0xB0, 0x10, 0x88, 0x00];
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, Machine.Amd64, 5);

        Assert.Equal(2, info.CodeLength);
        Assert.Equal(2u, info.SlotTable.NumUntracked);
        Assert.Equal(64, info.SlotTable.GcSlots[0].StackSlot.SpOffset);
        Assert.Equal(GcStackSlotBase.GC_SP_REL, info.SlotTable.GcSlots[0].StackSlot.Base);
        Assert.Equal(128, info.SlotTable.GcSlots[1].StackSlot.SpOffset);
        Assert.Equal(GcStackSlotBase.GC_FRAMEREG_REL, info.SlotTable.GcSlots[1].StackSlot.Base);
        Assert.All(info.SlotTable.GcSlots, slot => Assert.Equal(GcSlotFlags.GC_SLOT_UNTRACKED, slot.Flags));
        Assert.Equal(encoded.Length, info.Size);
    }

    [Theory]
    [InlineData(Machine.Amd64, 5, 0x0A, 5u)]
    [InlineData(Machine.Amd64, 5, 0x08, uint.MaxValue)]
    [InlineData(Machine.Amd64, 6, 0x0A, 5u)]
    [InlineData(Machine.Amd64, 6, 0x08, uint.MaxValue)]
    [InlineData(WasmMachine.Wasm32, 6, 0x04, 2u)]
    public void SlimHeaderUsesImplicitDefaultFrameRegister(Machine machine, int version, byte header, uint frameRegister)
    {
        byte[] encoded = [header, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, machine, version);

        Assert.Equal(2, info.CodeLength);
        Assert.Equal(frameRegister, info.StackBaseRegister);
        Assert.Empty(info.SlotTable.GcSlots);
        Assert.Equal(machine == WasmMachine.Wasm32 ? 1 : 2, info.Size);
    }

    [Theory]
    [InlineData(new byte[] { 0x21, 0x08, 0x02, 0x00 })]
    [InlineData(new byte[] { 0x41, 0x08, 0x02, 0x00 })]
    public void WasmFatHeaderPreservesGenericContext(byte[] encoded)
    {
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, WasmMachine.Wasm32, 6);

        Assert.Equal(2, info.CodeLength);
        Assert.Equal(2u, info.StackBaseRegister);
        Assert.Equal(8, info.GenericsInstContextStackSlot);
        Assert.Equal(1u, info.ValidRangeStart);
        Assert.Empty(info.SlotTable.GcSlots);
        Assert.Equal(encoded.Length, info.Size);
    }

    [Theory]
    [InlineData(new byte[] { 0x09, 0x08, 0x20, 0x00 }, 8, uint.MaxValue, -1, 0u)]
    [InlineData(new byte[] { 0x01, 0x09, 0x01, 0x00 }, -1, 1u, -1, 0u)]
    [InlineData(new byte[] { 0x01, 0x0A, 0x03, 0x00 }, -1, uint.MaxValue, 3, 0u)]
    [InlineData(new byte[] { 0x01, 0x08, 0x01 }, -1, uint.MaxValue, -1, 1u)]
    public void WasmFatHeaderPreservesOptionalFields(byte[] encoded, int cookieOffset, uint preservedArea,
        int reversePInvokeOffset, uint safePoints)
    {
        using MemoryStream stream = new(encoded);
        GcInfo info = new(new NativeReader(stream), 0, WasmMachine.Wasm32, 6);

        Assert.Equal(2, info.CodeLength);
        Assert.Equal(2u, info.StackBaseRegister);
        Assert.Equal(cookieOffset, info.GSCookieStackSlot);
        Assert.Equal(preservedArea, info.SizeOfEditAndContinuePreservedArea);
        Assert.Equal(reversePInvokeOffset, info.ReversePInvokeFrameStackSlot);
        Assert.Equal(safePoints, info.NumSafePoints);
        Assert.Equal((int)safePoints, info.SafePointOffsets.Count);
        if (safePoints != 0)
            Assert.Equal(0u, Assert.Single(info.SafePointOffsets).Value);
        if (cookieOffset != -1)
        {
            Assert.Equal(1u, info.ValidRangeStart);
            Assert.Equal(2u, info.ValidRangeEnd);
        }
        Assert.Empty(info.SlotTable.GcSlots);
        Assert.Equal(encoded.Length, info.Size);
    }

    [Theory]
    [InlineData(new byte[] { 0x00 }, 0u, 0u)]
    [InlineData(new byte[] { 0x11 }, 16u, 2u)]
    [InlineData(new byte[] { 0x7F }, 240u, 14u)]
    [InlineData(new byte[] { 0xF0, 0x02 }, 256u, 14u)]
    [InlineData(new byte[] { 0x8F, 0x01 }, 240u, 16u)]
    [InlineData(new byte[] { 0x80, 0x80, 0x84, 0x80, 0x10 }, 4096u, 512u)]
    [InlineData(new byte[] { 0x81, 0x80, 0x80, 0x80, 0x10 }, 16u, 512u)]
    [InlineData(new byte[] { 0x9F, 0x9E, 0xFC, 0x7F }, 0xFFFFF0u, 2u)]
    [InlineData(new byte[] { 0x8F, 0xBE, 0xFC, 0xFF, 0x0F }, 0xFFFFFF0u, 32u)]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x3F }, 0xFFFFFF0u, 0x1FFFFFEu)]
    [InlineData(new byte[] { 0xF0, 0xE1, 0x83, 0x80, 0xF0, 0xFF, 0x3F }, 0u, 0x1FFFFFEu)]
    public void WasmUnwindInfoInterleavesFrameSizeAndVirtualIPCount(byte[] encoded, uint frameSize, uint virtualIPCount)
    {
        using MemoryStream stream = new(encoded);
        ILCompiler.Reflection.ReadyToRun.Wasm32.UnwindInfo info = new(new NativeReader(stream), 0);

        Assert.Equal(frameSize, info.BytesUnwind);
        Assert.Equal(virtualIPCount, info.VirtualIPCount);
        Assert.Equal(encoded.Length, info.Size);
    }

    private sealed class SlotTableWriter
    {
        private readonly Machine _machine;
        private readonly List<byte> _bytes = [];
        public int BitCount { get; private set; }

        public SlotTableWriter(Machine machine)
        {
            _machine = machine;
            Write(0, 1);
            if (machine != WasmMachine.Wasm32)
                Write(1, 1);
            WriteUnsigned(2, machine switch
            {
                WasmMachine.Wasm32 => 5,
                Machine.I386 => 6,
                Machine.ArmThumb2 => 7,
                _ => 8,
            });
            if (machine != WasmMachine.Wasm32)
            {
                WriteUnsigned(0, machine switch
                {
                    Machine.Amd64 => 2,
                    Machine.I386 => 4,
                    _ => 3,
                });
            }
        }

        public int DenormalizeOffset(int offset) => offset * (_machine switch
        {
            WasmMachine.Wasm32 or Machine.ArmThumb2 => 4,
            Machine.I386 => 1,
            _ => 8,
        });

        public void WriteCounts(int tracked, int untracked, int registers = 0)
        {
            if (_machine != WasmMachine.Wasm32)
            {
                Write(registers == 0 ? 0 : 1, 1);
                if (registers != 0)
                    WriteUnsigned(registers, _machine is Machine.Amd64 or Machine.ArmThumb2 ? 2 : 3);
            }
            Write(tracked + untracked == 0 ? 0 : 1, 1);
            if (tracked + untracked == 0)
                return;
            WriteUnsigned(tracked, _machine switch
            {
                WasmMachine.Wasm32 or Machine.I386 => 5,
                Machine.ArmThumb2 => 3,
                _ => 2,
            });
            WriteUnsigned(untracked, _machine switch
            {
                WasmMachine.Wasm32 => 7,
                Machine.I386 => 5,
                Machine.ArmThumb2 => 3,
                _ => 1,
            });
        }

        public void WriteRegisters(List<(int Register, int Flags)> registers, int version)
        {
            int absoluteBase = _machine == Machine.ArmThumb2 ? 2 : 3;
            int deltaBase = _machine switch
            {
                Machine.ArmThumb2 => 1,
                Machine.I386 => 3,
                _ => 2,
            };
            int registerBits = _machine switch
            {
                Machine.I386 => 3,
                Machine.ArmThumb2 => 4,
                _ => 5,
            };
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
        }

        public GcStackSlotBase GetStackBase(int index, int count)
        {
            return _machine != WasmMachine.Wasm32 && index < (count + 1) / 2
                ? GcStackSlotBase.GC_SP_REL : GcStackSlotBase.GC_FRAMEREG_REL;
        }

        public int GetStackOffset(int index, int count)
        {
            return GetStackBase(index, count) == GcStackSlotBase.GC_FRAMEREG_REL
                ? -8 + index - (_machine == WasmMachine.Wasm32 ? 0 : (count + 1) / 2) : 8 + index;
        }

        public void WriteStackSlots(int[] counts, int[]? offsets = null)
        {
            int previousFlags = -1;
            GcStackSlotBase previousBase = GcStackSlotBase.GC_FRAMEREG_REL;
            int previousOffset = 0;
            for (int flags = 3; flags >= 0; flags--)
            {
                int count = counts[flags];
                for (int i = 0; i < count; i++)
                {
                    if (previousFlags != 0)
                        Write(flags, previousFlags < 0 || (previousFlags & 2) != 0 ? 2 : 1);
                    GcStackSlotBase slotBase = GetStackBase(i, count);
                    if (_machine != WasmMachine.Wasm32)
                        Write(slotBase == GcStackSlotBase.GC_FRAMEREG_REL ? 1 : 0, 1);
                    int offset = offsets is null ? GetStackOffset(i, count) : offsets[i];
                    if (flags != previousFlags || slotBase != previousBase)
                        WriteSigned(offset, _machine == WasmMachine.Wasm32 ? 3 : 6);
                    else
                        WriteUnsigned(offset - previousOffset, _machine == WasmMachine.Wasm32 ? 3 : 4);
                    previousFlags = flags;
                    previousBase = slotBase;
                    previousOffset = offset;
                }
            }
        }

        public void WriteLegacySlots()
        {
            for (int i = 0; i < 8; i++)
            {
                Write(i % 3, 2);
                if (i < 7)
                {
                    WriteSigned(-8 + i, 6);
                    Write(3 - i / 2, 2);
                }
                else
                {
                    WriteUnsigned(1, 4);
                }
            }
        }

        public void WriteUnsigned(int value, int encodingBase)
        {
            int mask = (1 << encodingBase) - 1;
            do
            {
                int chunk = value & mask;
                value >>>= encodingBase;
                Write(chunk | (value == 0 ? 0 : 1 << encodingBase), encodingBase + 1);
            } while (value != 0);
        }

        private void WriteSigned(int value, int encodingBase)
        {
            while (true)
            {
                int chunk = value & ((1 << encodingBase) - 1);
                value >>= encodingBase;
                bool done = value == 0 && (chunk & (1 << (encodingBase - 1))) == 0
                    || value == -1 && (chunk & (1 << (encodingBase - 1))) != 0;
                Write(chunk | (done ? 0 : 1 << encodingBase), encodingBase + 1);
                if (done)
                    return;
            }
        }

        private void Write(int value, int count)
        {
            for (int i = 0; i < count; i++, BitCount++)
            {
                if (BitCount % 8 == 0)
                    _bytes.Add(0);
                _bytes[BitCount / 8] |= (byte)(((value >> i) & 1) << (BitCount % 8));
            }
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[_bytes.Count + 8];
            _bytes.CopyTo(result);

            return result;
        }
    }
}
