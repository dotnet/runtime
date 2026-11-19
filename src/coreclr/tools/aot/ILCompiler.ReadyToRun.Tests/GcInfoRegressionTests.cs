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

            GcInfo info = Decode(writer, machine, version);
            Assert.Equal(hasStackBaseRegister ? expectedRegister : uint.MaxValue, info.StackBaseRegister);
            Assert.Equal(8 * instructionSize, info.CodeLength);
            Assert.Equal((uint)(3 * instructionSize), Assert.Single(info.SafePointOffsets).Value);
            Assert.Empty(info.SlotTable.GcSlots);
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

        GcInfo info = Decode(writer, machine);
        Assert.Equal(expectedRegister, info.StackBaseRegister);
        Assert.Empty(info.SlotTable.GcSlots);
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

        GcInfo info = Decode(writer, Machine.Arm64);
        Assert.Equal(1u, info.SizeOfEditAndContinuePreservedArea);
        Assert.Equal(24u, info.SizeOfStackOutgoingAndScratchArea);
        Assert.Equal(0u, info.NumSafePoints);
        Assert.Equal(0u, info.NumInterruptibleRanges);
        Assert.Empty(info.SlotTable.GcSlots);
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
            yield return [machine, instructionSize, Array.Empty<(int, int)>(), 4, 4];
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

        GcInfo info = Decode(writer, machine);
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

        GcInfo info = Decode(writer, Machine.Amd64);
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
            Assert.Same(info.SlotTable.GcSlots[highestStateLast ? 1 : 0], info.LiveSlotsAtSafepoints[0][0]);
        }
        Assert.Equal(writer.ByteCount, info.Size);
    }

    private static void WriteFatHeader(BitWriter writer, Machine machine, int flags, int codeLength = 8)
    {
        writer.Write(1, 1);
        writer.Write(flags, 10);
        writer.WriteUnsigned(codeLength, machine == Machine.ArmThumb2 ? 7 : 8);
    }

    private static GcInfo Decode(BitWriter writer, Machine machine, int version = 5)
    {
        using MemoryStream stream = new(writer.ToArray());

        return new GcInfo(new NativeReader(stream), 0, machine, version);
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

        public void Align()
        {
            while (_bitCount % 8 != 0)
                Write(0, 1);
        }

        public byte[] ToArray() => [.. _bytes, .. new byte[8]];
    }
}
