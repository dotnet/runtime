// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using ILCompiler.Reflection.ReadyToRun;
using ILCompiler.Reflection.ReadyToRun.Cdac;
using Xunit;
using X86TransitionPointer = Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.X86.GcTransitionPointer;

namespace ILCompiler.ReadyToRun.Tests;

public class GcInfoRegressionTests
{
    [Theory]
    [InlineData(Machine.Amd64, 3, 5u, "RBP", 1)]
    [InlineData(Machine.Amd64, 5, 5u, "RBP", 1)]
    [InlineData(Machine.ArmThumb2, 5, 11u, "R11", 2)]
    [InlineData(Machine.Arm64, 5, 29u, "X29", 4)]
    [InlineData(Machine.LoongArch64, 5, 22u, "Fp", 4)]
    [InlineData(Machine.RiscV64, 5, 8u, "Fp", 2)]
    [InlineData(WasmMachine.Wasm32, 5, 0u, "0", 1)]
    public void SlimHeaderDenormalizesFrameRegister(
        Machine machine, int version, uint expectedRegister, string registerName, int instructionSize)
    {
        int codeLengthEncoding = machine switch
        {
            Machine.ArmThumb2 => 7,
            WasmMachine.Wasm32 => 6,
            _ => 8,
        };
        int safePointCountEncoding = machine switch
        {
            Machine.Amd64 => 2,
            WasmMachine.Wasm32 => 4,
            _ => 3,
        };

        foreach (bool hasStackBaseRegister in new[] { false, true })
        {
            BitWriter writer = new();
            writer.Write(0, 1);
            writer.Write(hasStackBaseRegister ? 1 : 0, 1);
            if (version is 2 or 3)
                writer.Write(0, 2);
            writer.WriteUnsigned(8, codeLengthEncoding);
            writer.WriteUnsigned(1, safePointCountEncoding);
            writer.Write(3, 3);
            writer.Write(0, 2);

            CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, machine, version));
            Assert.Equal(hasStackBaseRegister ? expectedRegister : null, info.StackBaseRegister);
            Assert.Equal(8 * instructionSize, info.CodeLength);
            Assert.Equal((uint)(3 * instructionSize + (version < 4 ? 1 : 0)), Assert.Single(info.SafePoints));
            Assert.Empty(info.Slots);
            Assert.Equal(writer.ByteCount, info.Size);
            if (hasStackBaseRegister)
                Assert.Contains($"StackBaseRegister: {registerName}", info.ToString());
            else
                Assert.DoesNotContain("StackBaseRegister:", info.ToString());
        }
    }

    [Theory]
    [InlineData(Machine.LoongArch64, 0, 22u)]
    [InlineData(Machine.LoongArch64, 1, 3u)]
    [InlineData(Machine.RiscV64, 0, 8u)]
    [InlineData(Machine.RiscV64, 1, 2u)]
    [InlineData(Machine.RiscV64, 10, 2u)]
    public void FatHeaderDenormalizesFrameRegister(Machine machine, int encodedRegister, uint expectedRegister)
    {
        BitWriter writer = new();
        WriteFatHeader(writer, machine, 0x41);
        writer.WriteUnsigned(encodedRegister, 2);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, 1);
        writer.Write(0, 2);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, machine));
        Assert.Equal(expectedRegister, info.StackBaseRegister);
        Assert.Empty(info.Slots);
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(33)]
    public void Arm64EnCHeaderConsumesFixedFrameSize(int fixedFrameSize)
    {
        BitWriter writer = new();
        WriteFatHeader(writer, Machine.Arm64, 0x101);
        writer.WriteUnsigned(1, 4);
        writer.WriteUnsigned(fixedFrameSize, 4);
        writer.WriteUnsigned(3, 3);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, 1);
        writer.Write(0, 2);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, Machine.Arm64));
        Assert.Equal(1u, info.SizeOfEditAndContinuePreservedArea);
        Assert.Equal((uint)fixedFrameSize, info.SizeOfEditAndContinueFixedStackFrame);
        Assert.Equal(24u, info.Header.SizeOfStackParameterArea);
        Assert.Empty(info.SafePoints);
        Assert.Empty(info.InterruptibleRanges);
        Assert.Empty(info.Slots);
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Theory]
    [InlineData(Machine.Amd64, false, 8)]
    [InlineData(Machine.Amd64, true, 8)]
    [InlineData(Machine.ArmThumb2, false, 4)]
    [InlineData(Machine.ArmThumb2, true, 4)]
    public void StackSlotDeltasAreUnsigned(Machine machine, bool untracked, int slotSize)
    {
        const int SlotCount = 4;
        BitWriter writer = new();
        writer.Write(0, 1);
        writer.Write(1, 1);
        writer.WriteUnsigned(32, machine == Machine.ArmThumb2 ? 7 : 8);
        writer.WriteUnsigned(1, machine == Machine.Amd64 ? 2 : 3);
        writer.Write(3, 5);
        writer.Write(0, 1);
        writer.Write(1, 1);
        writer.WriteUnsigned(untracked ? 0 : SlotCount, machine == Machine.ArmThumb2 ? 3 : 2);
        writer.WriteUnsigned(untracked ? SlotCount : 0, machine == Machine.ArmThumb2 ? 3 : 1);
        writer.Write((int)GcStackSlotBase.GC_FRAMEREG_REL, 2);
        writer.WriteSigned(-16, 6);
        writer.Write(0, 2);
        foreach (int delta in new[] { 7, 8, 128 })
        {
            writer.Write((int)GcStackSlotBase.GC_FRAMEREG_REL, 2);
            writer.WriteUnsigned(delta, 4);
        }
        if (!untracked)
        {
            writer.Write(0, 1);
            writer.Write(0b1111, SlotCount);
        }

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, machine));
        Assert.Equal(SlotCount, info.Slots.Count);
        int[] normalizedOffsets = [-16, -9, -1, 127];
        for (int i = 0; i < SlotCount; i++)
        {
            CdacGcSlot slot = info.Slots[i];
            Assert.Equal(normalizedOffsets[i] * slotSize, slot.StackSlot.SpOffset);
            Assert.Equal(GcStackSlotBase.GC_FRAMEREG_REL, slot.StackSlot.Base);
            Assert.Equal(untracked ? GcSlotFlags.GC_SLOT_UNTRACKED : GcSlotFlags.GC_SLOT_BASE, slot.Flags);
        }
        if (!untracked)
        {
            List<BaseGcSlot> liveSlots = Assert.Single(info.LiveSlotsAtSafepoints);
            Assert.Equal(SlotCount, liveSlots.Count);
            for (int i = 0; i < SlotCount; i++)
                Assert.Same(info.Slots[i], liveSlots[i]);
        }
        Assert.Equal(writer.ByteCount, info.Size);
    }

    public static IEnumerable<object[]> TransitionCases()
    {
        foreach ((Machine machine, int instructionSize) in new[]
        {
            (Machine.Amd64, 1),
            (Machine.ArmThumb2, 2),
            (Machine.Arm64, 4),
            (Machine.LoongArch64, 4),
            (Machine.RiscV64, 2),
        })
        {
            yield return [machine, instructionSize, new[] { (8, 16) }, 4, 12];
            foreach ((int transition, int expected) in new[] { (3, 3), (4, 20), (9, 41) })
                yield return [machine, instructionSize, new[] { (0, 4), (20, 24), (40, 44) }, transition, expected];
        }
    }

    [Theory]
    [MemberData(nameof(TransitionCases))]
    public void TransitionsMapNormalizedOffsetsAcrossRanges(
        Machine machine, int instructionSize, (int Start, int Stop)[] ranges, int transition, int expected)
    {
        BitWriter writer = new();
        WriteFatHeader(writer, machine, 0, codeLength: 64);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, machine == Machine.Amd64 ? 2 : 3);
        writer.WriteUnsigned(ranges.Length, machine == Machine.ArmThumb2 ? 2 : 1);
        int previousStop = 0;
        foreach ((int start, int stop) in ranges)
        {
            writer.WriteUnsigned(start - previousStop, machine == Machine.ArmThumb2 ? 4 : 6);
            writer.WriteUnsigned(stop - start - 1, 6);
            previousStop = stop;
        }
        writer.Write(1, 1);
        writer.WriteUnsigned(1, machine is Machine.Amd64 or Machine.ArmThumb2 ? 2 : 3);
        writer.Write(0, 1);
        writer.WriteUnsigned(0, machine == Machine.ArmThumb2 ? 2 : 3);
        writer.Write(0, 2);
        writer.WriteUnsigned(1, 3);
        writer.Write(1, 1);
        writer.Align();
        writer.Write(0, 1); // Simple could-be-live bitmap.
        writer.Write(1, 1);
        writer.Write(1, 1); // Final state is live.
        writer.Write(1, 1);
        writer.Write(transition, 6);
        writer.Write(0, 1);

        BaseGcInfo info = DecodeCdac(writer, machine);
        KeyValuePair<int, List<BaseGcTransition>> entry = Assert.Single(info.Transitions);
        Assert.Equal(expected * instructionSize, entry.Key);
        Assert.Equal(entry.Key, Assert.Single(entry.Value).CodeOffset);
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IndirectSafePointSizeIncludesEveryReferencedState(bool runLengthEncoded, bool highestStateLast)
    {
        BitWriter writer = new();
        writer.Write(0, 1);
        writer.Write(1, 1);
        writer.WriteUnsigned(128, 8);
        writer.WriteUnsigned(64, 2);
        for (int i = 1; i <= 64; i++)
            writer.Write(i, 7);
        writer.Write(0, 1);
        writer.Write(1, 1);
        writer.WriteUnsigned(32, 2);
        writer.WriteUnsigned(0, 1);
        for (int slot = 0; slot < 32; slot++)
        {
            writer.Write(1, 2); // SP-relative stack slot.
            writer.WriteUnsigned(slot == 0 ? 0 : 1, slot == 0 ? 6 : 4);
            if (slot == 0)
                writer.Write(0, 2);
        }
        writer.Write(1, 1);
        writer.WriteUnsigned(5, 3); // Six-bit indirect state offsets.
        int firstStateSize = runLengthEncoded ? 12 : 33;
        for (int i = 0; i < 64; i++)
            writer.Write(i == (highestStateLast ? 63 : 0) ? firstStateSize : 0, 6);
        writer.Align();
        for (int state = 0; state < 2; state++)
        {
            writer.Write(runLengthEncoded ? 1 : 0, 1);
            if (runLengthEncoded)
            {
                writer.Write(0, 1);
                writer.WriteUnsigned(state == 0 ? 32 : 0, 4);
                if (state != 0)
                    writer.WriteUnsigned(31, 2);
            }
            else
            {
                for (int slot = 0; slot < 32; slot++)
                    writer.Write((slot + state) % 2, 1);
            }
        }

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, Machine.Amd64));
        Assert.Equal(64, info.LiveSlotsAtSafepoints.Count);
        if (runLengthEncoded)
        {
            Assert.Equal(highestStateLast ? 0 : 32, info.LiveSlotsAtSafepoints[0].Count);
            Assert.Equal(highestStateLast ? 32 : 0, info.LiveSlotsAtSafepoints[63].Count);
        }
        else
        {
            Assert.Equal(16, info.LiveSlotsAtSafepoints[0].Count);
            Assert.Equal(16, info.LiveSlotsAtSafepoints[63].Count);
            Assert.Same(info.Slots[highestStateLast ? 1 : 0], info.LiveSlotsAtSafepoints[0][0]);
        }
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Fact]
    public void CdacReversePInvokeFrameSlotIsDenormalized()
    {
        BitWriter writer = new();
        WriteFatHeader(writer, Machine.Amd64, 0x200);
        writer.WriteSigned(-7, 6);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, 2);
        writer.WriteUnsigned(0, 1);
        writer.Write(0, 2);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, Machine.Amd64));
        Assert.Equal(-56, info.ReversePInvokeFrameStackSlot);
        Assert.Contains("ReversePInvokeFrameStackSlot: -56", info.ToString());
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Fact]
    public void CdacTransitionsAtAdjacentRangeStartUseFirstRange()
    {
        BitWriter writer = new();
        WriteFatHeader(writer, Machine.Amd64, 0, codeLength: 64);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, 2);
        writer.WriteUnsigned(2, 1);
        writer.WriteUnsigned(8, 6); // [8, 16)
        writer.WriteUnsigned(7, 6);
        writer.WriteUnsigned(0, 6); // [16, 24)
        writer.WriteUnsigned(7, 6);
        writer.Write(1, 1);
        writer.WriteUnsigned(1, 2);
        writer.Write(0, 1);
        writer.WriteUnsigned(0, 3); // RAX
        writer.Write(0, 2);
        writer.WriteUnsigned(1, 3);
        writer.Write(1, 1);
        writer.Align();
        writer.Write(0, 1);
        writer.Write(1, 1);
        writer.Write(0, 1); // Final state is dead.
        writer.Write(1, 1);
        writer.Write(8, 6);
        writer.Write(0, 1);

        BaseGcInfo info = DecodeCdac(writer, Machine.Amd64);
        Assert.Equal(new[] { 8, 16 }, info.Transitions.Keys);
        Assert.Equal("RAX is live", Assert.Single(info.Transitions[8]).ToString());
        Assert.Equal("RAX is dead", Assert.Single(info.Transitions[16]).ToString());
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Fact]
    public void CdacStackSlotsNameTheirBase()
    {
        BitWriter writer = new();
        WriteFatHeader(writer, Machine.Amd64, 0x40);
        writer.WriteUnsigned(0, 3); // RBP
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, 2);
        writer.WriteUnsigned(1, 1);
        writer.WriteUnsigned(0, 6); // [0, 8)
        writer.WriteUnsigned(7, 6);
        writer.Write(0, 1);
        writer.Write(1, 1);
        writer.WriteUnsigned(3, 2);
        writer.WriteUnsigned(0, 1);
        writer.Write(0, 2); // caller.sp+8
        writer.WriteSigned(1, 6);
        writer.Write(0, 2);
        writer.Write(1, 2); // sp+16
        writer.WriteUnsigned(1, 4);
        writer.Write(2, 2); // rbp+24
        writer.WriteUnsigned(1, 4);
        writer.WriteUnsigned(1, 3);
        writer.Write(1, 1);
        writer.Align();
        writer.Write(0, 1);
        writer.Write(7, 3);
        writer.Write(7, 3);
        writer.Write(0, 3);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, Machine.Amd64));
        Assert.Equal(new[] { "caller.sp+8", "sp+16", "rbp+24" }, info.Slots.ConvertAll(s => s.Location));
        Assert.Equal(new[] { "caller.sp+8 is live", "sp+16 is live", "rbp+24 is live" },
            Assert.Single(info.Transitions).Value.ConvertAll(t => t.ToString()));
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Fact]
    public void CdacStackSlotDeltaIsUnsigned()
    {
        BitWriter writer = new();
        WriteFatHeader(writer, Machine.Amd64, 0);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, 2);
        writer.WriteUnsigned(0, 1);
        writer.Write(0, 1);
        writer.Write(1, 1);
        writer.WriteUnsigned(2, 2);
        writer.WriteUnsigned(0, 1);
        writer.Write(1, 2);
        writer.WriteSigned(1, 6);
        writer.Write(0, 2);
        writer.Write(1, 2);
        writer.WriteUnsigned(8, 4);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, Machine.Amd64));
        Assert.Equal(new[] { 8, 72 }, info.Slots.ConvertAll(s => s.StackSlot.SpOffset));
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Fact]
    public void CdacDecodesGcInfoLargerThanDecoderWindow()
    {
        const int SafePointCount = 5000;
        BitWriter writer = new();
        WriteFatHeader(writer, Machine.Amd64, 0, codeLength: 1 << 20);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(SafePointCount, 2);
        writer.WriteUnsigned(0, 1);
        for (int i = 0; i < SafePointCount; i++)
            writer.Write(i * 8, 20);
        writer.Write(0, 1);
        writer.Write(0, 1);
        Assert.True(writer.ByteCount > 10000);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, Machine.Amd64));
        Assert.Equal(SafePointCount, info.SafePoints.Count);
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Fact]
    public void CdacX86EbpFrameCallOffsetsAccumulate()
    {
        BaseGcInfo info = DecodeCdacX86([64, 0x81, 0xBE, 0x3F, 0x03, 0x04, 0xFF]);
        Assert.Equal(new[] { 3, 7 }, info.Transitions.Keys);
        Assert.Equal(7, info.Size);
    }

    [Fact]
    public void CdacX86ArgumentPushKeepsThisAndInteriorFlags()
    {
        BaseGcInfo info = DecodeCdacX86([64, 0x81, 0x40, 0xBC, 0xBF, 0x81, 0xC9, 0xFF]);
        X86TransitionPointer push = Assert.IsType<X86TransitionPointer>(
            Assert.IsType<CdacX86GcTransition>(Assert.Single(info.Transitions[1])).Transition);
        Assert.True(push.IsThis);
        Assert.True(push.Iptr);
        Assert.Equal(0u, push.ArgOffset);
        X86TransitionPointer pop = Assert.IsType<X86TransitionPointer>(
            Assert.IsType<CdacX86GcTransition>(Assert.Single(info.Transitions[2])).Transition);
        Assert.Equal(1u, pop.ArgOffset);
        Assert.Equal(0u, pop.ArgCount);
        Assert.Equal(8, info.Size);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void CdacX86TrackedEbpSlotsUseSignedOffsets(int version)
    {
        // One VarPtr entry: EBP-8, live in [2, 6).
        CdacX86GcInfo info = Assert.IsType<CdacX86GcInfo>(
            DecodeCdacX86([64, 0x81, 0x82, 0xBE, 0xBF, 0x46, 1, 3, 8, 2, 4, 0xFF], version));
        CdacX86GcSlot slot = Assert.Single(info.Slots);
        Assert.Equal("[EBP-8]", slot.Location);
        Assert.Equal(2u, slot.BeginOffset);
        Assert.Equal(6u, slot.EndOffset);
        Assert.Contains("[EBP-8]", info.ToString());
        Assert.Equal(12, info.Size);
    }

    [Theory]
    [InlineData(Machine.Amd64, 1)]
    [InlineData(Machine.Amd64, 2)]
    [InlineData(Machine.Amd64, 3)]
    [InlineData(Machine.ArmThumb2, 2)]
    [InlineData(Machine.Arm64, 3)]
    public void CdacDecodesLegacyFatHeader(Machine machine, int version)
    {
        bool isArm = machine == Machine.ArmThumb2;
        int codeLengthScale = machine switch { Machine.ArmThumb2 => 2, Machine.Arm64 => 4, _ => 1 };
        int stackSlotScale = isArm ? 4 : 8;

        BitWriter writer = new();
        writer.Write(1, 1);
        writer.Write(0xE, version == 1 ? 9 : 10); // Security object, GS cookie and PSPSym.
        if (version >= 2)
            writer.Write(1, isArm ? 2 : 4); // ReturnKind
        writer.WriteUnsigned(20, isArm ? 7 : 8);
        writer.WriteUnsigned(3, 5); // Prolog size - 1.
        writer.WriteUnsigned(2, 3); // Epilog size.
        writer.WriteSigned(-2, isArm ? 5 : 6);
        writer.WriteSigned(-3, isArm ? 5 : 6);
        writer.WriteSigned(-4, isArm ? 5 : 6);
        writer.WriteUnsigned(0, 3);
        writer.WriteUnsigned(0, machine == Machine.Amd64 ? 2 : 3);
        writer.WriteUnsigned(0, isArm ? 2 : 1);
        writer.Write(0, 2);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, machine, version));
        // Code offsets are not normalized before version 4; the code length is.
        Assert.Equal(20 * codeLengthScale, info.CodeLength);
        Assert.Equal(4u, info.ValidRangeStart);
        Assert.Equal((uint)info.CodeLength - 2, info.ValidRangeEnd);
        Assert.Equal(version >= 2 ? ReturnKinds.RT_Object : null, info.ReturnKind);
        Assert.Equal(-2 * stackSlotScale, info.SecurityObjectStackSlot);
        Assert.Equal(-3 * stackSlotScale, info.Header.GSCookie?.SpOffset);
        Assert.Equal(-4 * stackSlotScale, info.Header.PSPSym?.SpOffset);
        Assert.Contains($"PSPSymStackSlot: caller.sp{-4 * stackSlotScale}", info.ToString());
        Assert.Equal(writer.ByteCount, info.Size);
    }

    [Theory]
    [InlineData(Machine.Amd64, 3)]
    [InlineData(Machine.ArmThumb2, 2)]
    public void CdacLegacySafePointsAreEncodedWithMinusOneAdjustment(Machine machine, int version)
    {
        bool isArm = machine == Machine.ArmThumb2;

        BitWriter writer = new();
        writer.Write(0, 1); // Slim header without a stack base register.
        writer.Write(0, 1);
        writer.Write(0, 2); // ReturnKind
        writer.WriteUnsigned(16, isArm ? 7 : 8);
        writer.WriteUnsigned(2, isArm ? 3 : 2);
        int bitsPerOffset = isArm ? 5 : 4; // CeilOfLog2 of the unnormalized code length.
        writer.Write(3, bitsPerOffset);
        writer.Write(9, bitsPerOffset);
        writer.Write(1, 1); // One register slot.
        writer.WriteUnsigned(1, 2);
        writer.Write(0, 1);
        writer.WriteUnsigned(3, isArm ? 2 : 3);
        writer.Write(0, 2);
        writer.Write(0, 1); // Direct live state bitmaps.
        writer.Write(1, 1);
        writer.Write(0, 1);

        CdacGcInfo info = Assert.IsType<CdacGcInfo>(DecodeCdac(writer, machine, version));
        Assert.Equal(new uint[] { 4, 10 }, info.SafePoints);
        Assert.Same(info.Slots[0], Assert.Single(info.LiveSlotsAtSafepoints[0]));
        Assert.Empty(info.LiveSlotsAtSafepoints[1]);
        Assert.Equal(writer.ByteCount, info.Size);
    }

    private static BaseGcInfo DecodeCdac(BitWriter writer, Machine machine, int version = 5)
    {
        using MemoryStream stream = new(writer.ToArray());

        return CdacGcInfo.Decode(new NativeReader(stream), 0, machine, version);
    }

    private static BaseGcInfo DecodeCdacX86(byte[] encoded, int version = 5)
    {
        using MemoryStream stream = new([.. encoded, .. new byte[8]]);

        return CdacGcInfo.Decode(new NativeReader(stream), 0, Machine.I386, version);
    }

    private static void WriteFatHeader(BitWriter writer, Machine machine, int flags, int codeLength = 8)
    {
        writer.Write(1, 1);
        writer.Write(flags, 10);
        writer.WriteUnsigned(codeLength, machine == Machine.ArmThumb2 ? 7 : 8);
    }

    private sealed class BitWriter
    {
        private readonly List<byte> _bytes = [];
        private int _bitCount;

        public int ByteCount => (_bitCount + 7) / 8;

        public void Write(int value, int count)
        {
            for (int i = 0; i < count; i++, _bitCount++)
            {
                if (_bitCount % 8 == 0)
                    _bytes.Add(0);
                _bytes[^1] |= (byte)(((value >> i) & 1) << (_bitCount % 8));
            }
        }

        public void WriteUnsigned(int value, int encodingBase)
        {
            do
            {
                int chunk = value & ((1 << encodingBase) - 1);
                value >>>= encodingBase;
                Write(chunk | (value == 0 ? 0 : 1 << encodingBase), encodingBase + 1);
            } while (value != 0);
        }

        public void WriteSigned(int value, int encodingBase)
        {
            int signBit = 1 << (encodingBase - 1);
            while (true)
            {
                int chunk = value & ((1 << encodingBase) - 1);
                value >>= encodingBase;
                bool done = (value == 0 && (chunk & signBit) == 0) || (value == -1 && (chunk & signBit) != 0);
                Write(chunk | (done ? 0 : 1 << encodingBase), encodingBase + 1);
                if (done)
                    return;
            }
        }

        public void Align()
        {
            while (_bitCount % 8 != 0)
                Write(0, 1);
        }

        public byte[] ToArray() => [.. _bytes, .. new byte[8]];
    }
}
