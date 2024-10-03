// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class RangeSectionLookupAlgorithmTests
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
        const ulong topLevelAddress = 0x0000_1000u;
        private readonly MockMemorySpace.Builder _builder;
        private readonly MockTarget.Architecture _arch;
        private readonly TargetTestHelpers _targetTestHelpers;
        private readonly int _levels;
        private readonly int _maxSetBit;
        private ulong _nextMapAddress;
        public Builder(MockTarget.Architecture arch)
        {
            _arch = arch;
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

        public ExMgrPtr TopLevel => new ExMgrPtr(topLevelAddress);

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

        private void WritePointer(TargetPointer address, ExMgrPtr value)
        {
            Span<byte> dest = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
            _targetTestHelpers.WritePointer(dest, value.RawValue);
        }

        private ExMgrPtr ReadPointer (TargetPointer address)
        {
            ReadOnlySpan<byte> src = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
            return new ExMgrPtr(_targetTestHelpers.ReadPointer(src));
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

        private TargetPointer Offset(TargetPointer levelStart, int index)
        {
            return new TargetPointer(levelStart.Value + (ulong)(index * _targetTestHelpers.PointerSize));
        }

        // gets the slot in the outerLevel map that corresponds to the given code address
        // if the slot was empty, allocates a new level and inserts it into the slot.
        // after this method returns, reading from TargetPointer will return a non-null level map
        private TargetPointer GetOrAddLevelSlot(TargetCodePointer address, TargetPointer outerLevel, int curLevel, bool collectible = false)
        {
            // codeman.h RangeSectionMap::EnsureLevel
            // TODO
            // read from the outer level and if its null, allocate a new level.
            // then return the address of the entry in that level that corresponds to the given address
            int index = EffectiveBitsForLevel(address, curLevel);
            TargetPointer nextLevelAddress = Offset(outerLevel, index);
            ExMgrPtr curEntry = ReadPointer(nextLevelAddress);
            if (curEntry.IsNull)
            {
                curEntry = new (AllocateMapLevel(curLevel - 1).Address);
                if (collectible)
                {
                    curEntry = new (curEntry.RawValue | 1);
                }
                WritePointer(nextLevelAddress, curEntry);
            }
            return nextLevelAddress;
        }

        // ensures that the maps for all the levels for the given address are allocated.
        // returns the address of the slot in the last level that corresponds to the given address
        TargetPointer EnsureLevelsForAddress(TargetCodePointer address, bool collectible = false)
        {
            TargetPointer curSlot = TargetPointer.Null;
            TargetPointer map = TopLevel.Address;
            for (int curLevel = _levels; curLevel > 0; curLevel--)
            {
                curSlot = GetOrAddLevelSlot(address, map, curLevel, collectible);
                if (curLevel > 1)
                    map = ReadPointer(curSlot).Address;
            }
            return curSlot;
        }
        public void InsertAddressRange(TargetCodePointer start, uint length, ulong value, bool collectible = false)
        {
            TargetCodePointer cur = start;
            ulong end = start.Value + length;
            do {
                TargetPointer lastSlot = EnsureLevelsForAddress(cur, collectible);
                WritePointer(lastSlot, new ExMgrPtr(value));
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

        var rsla = RangeSectionLookupAlgorithm.Create(target);

        var inputPC = new TargetCodePointer(0x007f_0000);
        var result = rsla.FindFragmentInternal(target, builder.TopLevel, inputPC);
        Assert.Equal(ExMgrPtr.Null, result);
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

        var rsla = RangeSectionLookupAlgorithm.Create(target);

        var resultSlot = rsla.FindFragmentInternal(target, builder.TopLevel, inputPC);
        var result = resultSlot.LoadPointer(target);
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

}
