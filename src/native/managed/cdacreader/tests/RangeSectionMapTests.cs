// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using System.Collections.Generic;

using InteriorMapValue = Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers.RangeSectionMap.InteriorMapValue;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class RangeSectionMapTests
{
    const int EntriesPerMapLevel = 256; // for now its fixed at 256, see codeman.h RangeSectionMap::entriesPerMapLevel
    const int BitsPerLevel = 8;
    internal class RSLATestTarget : TestPlaceholderTarget
    {
        private readonly MockTarget.Architecture _arch;
        private readonly MockMemorySpace.ReadContext _readContext;
        public RSLATestTarget(MockTarget.Architecture arch, MockMemorySpace.ReadContext readContext)
            : base (arch)
        {
            _arch = arch;
            _readContext = readContext;
            SetDataReader(_readContext.ReadFromTarget);
        }
    }

    internal class Builder
    {
        const ulong topLevelAddress = 0x0000_1000u; // arbitrary
        private readonly MockMemorySpace.Builder _builder;
        private readonly TargetTestHelpers _targetTestHelpers;
        private readonly int _levels;
        private readonly int _maxSetBit;
        private ulong _nextMapAddress;
        public Builder(MockTarget.Architecture arch)
        {
            _targetTestHelpers = new TargetTestHelpers(arch);
            _builder = new MockMemorySpace.Builder(_targetTestHelpers);
            _levels = arch.Is64Bit ? 5 : 2;
            _maxSetBit = arch.Is64Bit ? 56 : 31; // 0 indexed
            MockMemorySpace.HeapFragment top = new MockMemorySpace.HeapFragment
            {
                Address = new TargetPointer(topLevelAddress),
                Data = new byte[EntriesPerMapLevel * _targetTestHelpers.PointerSize],
                Name = $"Map Level {_levels}"
            };
            _nextMapAddress = topLevelAddress + (ulong)top.Data.Length;
            _builder.AddHeapFragment(top);
        }

        public TargetPointer TopLevel => topLevelAddress;

        private int EffectiveBitsForLevel(ulong address, int level)
        {
            ulong addressBitsUsedInMap = address >> (_maxSetBit + 1 - (_levels * BitsPerLevel));
            ulong addressBitsShifted = addressBitsUsedInMap >> ((level - 1) * BitsPerLevel);
            int addressBitsUsedInLevel = checked((int)((EntriesPerMapLevel - 1) & addressBitsShifted));
            return addressBitsUsedInLevel;
        }

        // This is how much of the address space is covered by each entry in the last level of the map
        private int BytesAtLastLevel => checked (1 << BitsAtLastLevel);
        private int BitsAtLastLevel => _maxSetBit - (BitsPerLevel * _levels) + 1;

        private TargetPointer CursorAddress(RangeSectionMap.Cursor cursor)
        {
            return cursor.LevelMap + (ulong)(cursor.Index * _targetTestHelpers.PointerSize);
        }

        private void WritePointer(RangeSectionMap.Cursor cursor, InteriorMapValue value)
        {
            TargetPointer address = CursorAddress(cursor);
            Span<byte> dest = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
            _targetTestHelpers.WritePointer(dest, value.RawValue);
        }

        private InteriorMapValue LoadCursorValue (RangeSectionMap.Cursor cursor)
        {
            TargetPointer address = CursorAddress(cursor);
            ReadOnlySpan<byte> src = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
            return new InteriorMapValue(_targetTestHelpers.ReadPointer(src));
        }

        private MockMemorySpace.HeapFragment AllocateMapLevel(int level)
        {
            MockMemorySpace.HeapFragment mapLevel = new MockMemorySpace.HeapFragment
            {
                Address = new TargetPointer(_nextMapAddress),
                Data = new byte[EntriesPerMapLevel * _targetTestHelpers.PointerSize],
                Name = $"Map Level {level}"
            };
            _nextMapAddress += (ulong)mapLevel.Data.Length;
            _builder.AddHeapFragment(mapLevel);
            return mapLevel;
        }


        // computes the cursor for the next level down from the given cursor
        // if the slot for the next level does not exist, it is created
        private RangeSectionMap.Cursor GetOrAddLevelSlot(TargetCodePointer address, RangeSectionMap.Cursor cursor, bool collectible = false)
        {
            int nextLevel = cursor.Level - 1;
            int nextIndex = EffectiveBitsForLevel(address, nextLevel);
            InteriorMapValue nextLevelMap = LoadCursorValue(cursor);
            if (nextLevelMap.IsNull)
            {
                nextLevelMap = new (AllocateMapLevel(nextLevel).Address);
                if (collectible)
                {
                    nextLevelMap = new (nextLevelMap.RawValue | 1);
                }
                WritePointer(cursor, nextLevelMap);
            }
            return new RangeSectionMap.Cursor(nextLevelMap.Address, nextLevel, nextIndex);
        }

        // ensures that the maps for all the levels for the given address are allocated.
        // returns the address of the slot in the last level that corresponds to the given address
        RangeSectionMap.Cursor EnsureLevelsForAddress(TargetCodePointer address, bool collectible = false)
        {
            int topIndex = EffectiveBitsForLevel(address, _levels);
            RangeSectionMap.Cursor cursor = new RangeSectionMap.Cursor(TopLevel, _levels, topIndex);
            while (!cursor.IsLeaf)
            {
                cursor = GetOrAddLevelSlot(address, cursor, collectible);
            }
            return cursor;
        }
        public void InsertAddressRange(TargetCodePointer start, uint length, ulong value, bool collectible = false)
        {
            TargetCodePointer cur = start;
            ulong end = start.Value + length;
            do {
                RangeSectionMap.Cursor lastCursor = EnsureLevelsForAddress(cur, collectible);
                WritePointer(lastCursor, new InteriorMapValue(value));
                cur = new TargetCodePointer(cur.Value + (ulong)BytesAtLastLevel); // FIXME: round ?
            } while (cur.Value < end);
        }
        public void MarkCreated()
        {
            _builder.MarkCreated();
        }

        public MockMemorySpace.ReadContext GetReadContext()
        {
            return _builder.GetReadContext();
        }
    }


    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestLookupFail(MockTarget.Architecture arch)
    {
        var builder = new Builder(arch);
        builder.MarkCreated();
        var target = new RSLATestTarget(arch, builder.GetReadContext());

        var rsla = RangeSectionMap.Create(target);

        var inputPC = new TargetCodePointer(0x007f_0000);
        var result = rsla.FindFragmentInternal(target, builder.TopLevel, inputPC);
        Assert.False(result.HasValue);
    }

    [Theory]
    //[ClassData(typeof(MockTarget.StdArch))]
    [MemberData(nameof(Something), 10)]
    public void TestLookupOne(MockTarget.Architecture arch, int seed)
    {
        var rng = new Random(seed);
        var builder = new Builder(arch);
        var inputPC = new TargetCodePointer(GoodAddress(/*0x007f_0000*/ rng, arch));
        var length = 0x1000u;
        var value = 0x0a0a_0a0au;
        builder.InsertAddressRange(inputPC, length, value);
        builder.MarkCreated();
        var target = new RSLATestTarget(arch, builder.GetReadContext());

        var rsla = RangeSectionMap.Create(target);

        var cursor = rsla.FindFragmentInternal(target, builder.TopLevel, inputPC);
        Assert.True(cursor.HasValue);
        var result = cursor.Value.LoadValue(target);
        Assert.Equal(value, result.Address.Value);
    }

    private static ulong GoodAddress(Random rng, MockTarget.Architecture arch)
    {
        ulong address;
        do
        {
            address = (ulong)rng.Next();
            if (arch.Is64Bit)
            {
                address <<= 32;
                address |= (uint)rng.Next();
            }
            address &= ~0xFu; // align to 16 bytes
            if (address < 0x8000)
                continue; // retry if address is too low - wan't to avoid the map fragments themselves
        } while (false);
        return address;
    }

    public static IEnumerable<object[]> Something(int numRandomSeeds) {
        foreach (object[] arch in new MockTarget.StdArch())
        {
            for (int seed = 1; seed <= numRandomSeeds; seed++)
            {
                yield return new object[] { arch[0], seed };
            }
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestEffectiveBitsForLevel(MockTarget.Architecture arch)
    {
        // Exhaustively test EffectiveBitsForLevel for all possible values of the byte for each level
        var target = new RSLATestTarget(arch, new MockMemorySpace.ReadContext());
        var rsla = RangeSectionMap.Create(target);
        int numLevels = arch.Is64Bit ? 5 : 2;
        // the bits 0..effectiveRange - 1 are not handled the map and are irrelevant
        // the bits from effectiveRange..maxSetBit are used by the map, the bits above maxSetBit are irrelevant
        int maxSetBit = arch.Is64Bit ? 56 : 31;
        int effectiveRange = maxSetBit + 1 - (numLevels * 8);
        ulong irrelevantLowBits = 0xcccccccc_ccccccccul & ((1ul << (effectiveRange - 1)) - 1);
        for (int i = 0; i < 256; i++) {
            for (int level = 1; level <= numLevels; level++) {
                ulong address;
                // Set address to 0xfffff_{i}_0000_cccc
                // where all the unused bits above the current level (including bits above maxSetBit) are
                // set to 1 and all the lower bits are set to 0
                // and the current level bits are set to i
                // and the bits that will be handled by the fragment linked list are set to 0xccc
                ulong upperBits = ~0ul << (effectiveRange + 8 * level);
                ulong payload = (ulong)i << (8 * (level - 1));
                ulong workingBits = payload << effectiveRange;
                address = upperBits | workingBits | irrelevantLowBits;
                int expected = i;
                int actual = rsla.EffectiveBitsForLevel(new TargetCodePointer(address), level);
                Assert.Equal(expected, actual);
            }
        }
    }

}
