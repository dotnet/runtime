// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;


internal readonly struct RangeSectionLookupAlgorithm
{
    private int MapLevels { get; }
    private int BitsPerLevel { get; } = 8;
    private int MaxSetBit { get; }
    private int EntriesPerMapLevel { get; } = 256;

// "ExecutionManagerPointer": a pointer to a RangeFragment and RangeSection.
// The pointers have a collectible flag on the lowest bit
internal struct ExMgrPtr
{
    public readonly TargetPointer RawValue;

    public TargetPointer Address => RawValue & ~1ul;

    public bool IsNull => Address == TargetPointer.Null;

    public static ExMgrPtr Null => new ExMgrPtr(TargetPointer.Null);

    public ExMgrPtr(TargetPointer value)
    {
        RawValue = value;
    }

    public ExMgrPtr Offset(int stride, int idx)
    {
        return new ExMgrPtr(RawValue.Value + (ulong)(stride * idx));
    }

    public T Load<T>(Target target) where T : Data.IData<T>
    {
        return target.ProcessedData.GetOrAdd<T>(Address);
    }

    public ExMgrPtr LoadPointer(Target target)
    {
        return new ExMgrPtr(target.ReadPointer(Address));
    }
}

    internal struct Cursor
    {
        public TargetPointer LevelMap { get; }
        public int Level { get; }
        public int Index { get; }

        public Cursor(TargetPointer levelMap, int level, int index)
        {
            LevelMap = levelMap;
            Level = level;
            Index = index;
        }

        public TargetPointer GetSlot(Target target) => LevelMap + (ulong)(Index * target.PointerSize);

        public ExMgrPtr LoadValue(Target target) => new ExMgrPtr(target.ReadPointer(GetSlot(target)));

        public bool IsLeaf => Level == 1;

    }

    internal Cursor GetTopCursor(TargetPointer topMap, TargetCodePointer jittedCodeAddress)
    {
        int index = EffectiveBitsForLevel(jittedCodeAddress, MapLevels);
        return new Cursor(topMap, MapLevels, index);
    }

    internal Cursor GetNextCursor(Target target, Cursor cursor, TargetCodePointer jittedCodeAddress)
    {
        int nextLevel = cursor.Level - 1;
        int nextIndex = EffectiveBitsForLevel(jittedCodeAddress, nextLevel);
        TargetPointer nextMap = cursor.LoadValue(target).Address;
        return new Cursor(nextMap, nextLevel, nextIndex);
    }

    private RangeSectionLookupAlgorithm(int mapLevels, int maxSetBit)
    {
        MapLevels = mapLevels;
        MaxSetBit = maxSetBit;
    }
    public static RangeSectionLookupAlgorithm Create(Target target)
    {
        if (target.PointerSize == 4)
        {
            return new(mapLevels: 2, maxSetBit: 31); // 0 indexed
        }
        else if (target.PointerSize == 8)
        {
            return new(mapLevels: 5, maxSetBit: 56); // 0 indexed
        }
        else
        {
            throw new InvalidOperationException("Invalid pointer size");
        }
    }

    // note: level is 1-indexed
    private int EffectiveBitsForLevel(TargetCodePointer address, int level)
    {
        ulong addressAsInt = address.Value;
        ulong addressBitsUsedInMap = addressAsInt >> (MaxSetBit + 1 - (MapLevels * BitsPerLevel));
        ulong addressBitsShifted = addressBitsUsedInMap >> ((level - 1) * BitsPerLevel);
        ulong addressBitsUsedInLevel = (ulong)(EntriesPerMapLevel - 1) & addressBitsShifted;
        return checked((int)addressBitsUsedInLevel);
    }

    public ExMgrPtr /*PTR_RangeSectionFragment*/ FindFragment(Target target, Data.RangeSectionMap topRangeSectionMap, TargetCodePointer jittedCodeAddress)
    {
        ExMgrPtr top = new ExMgrPtr(topRangeSectionMap.TopLevelData);
        return FindFragmentInternal(target, top, jittedCodeAddress);
    }

    internal ExMgrPtr FindFragmentInternal(Target target, ExMgrPtr top, TargetCodePointer jittedCodeAddress)
    {
#if false
        /* The outer levels are all pointer arrays to the next level down.  Level 1 is an array of pointers to a RangeSectionFragment */
        int topLevelIndex = EffectiveBitsForLevel(jittedCodeAddress, MapLevels);

        ExMgrPtr nextLevelAddress = top.Offset(target.PointerSize, topLevelIndex);
        for (int level = MapLevels - 1; level >= 1; level--)
        {
            ExMgrPtr rangeSectionL = nextLevelAddress.LoadPointer(target);
            if (rangeSectionL.IsNull)
                return ExMgrPtr.Null;
            nextLevelAddress = rangeSectionL.Offset(target.PointerSize, EffectiveBitsForLevel(jittedCodeAddress, level));
        }
        return nextLevelAddress;
#else
        Cursor cursor = GetTopCursor(top.Address, jittedCodeAddress);
        while (!cursor.IsLeaf)
        {
            ExMgrPtr nextLevel = cursor.LoadValue(target);
            if (nextLevel.IsNull)
                return ExMgrPtr.Null;
            cursor = GetNextCursor(target, cursor, jittedCodeAddress);
        }
        return new ExMgrPtr(cursor.GetSlot(target)); // FIXME: update client code to use a TargetPointer and rename this function  to FindFragmentSlot
#endif
    }
}
