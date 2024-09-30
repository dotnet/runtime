// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

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

internal readonly struct RangeSectionLookupAlgorithm
{
    private int MapLevels { get; }
    private int BitsPerLevel { get; } = 8;
    private int MaxSetBit { get; }
    private int EntriesPerMapLevel { get; } = 256;

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

    internal ExMgrPtr /*PTR_RangeSectionFragment*/ FindFragment(Target target, Data.RangeSectionMap topRangeSectionMap, TargetCodePointer jittedCodeAddress)
    {
        /* The outer levels are all pointer arrays to the next level down.  Level 1 is an array of pointers to a RangeSectionFragment */
        int topLevelIndex = EffectiveBitsForLevel(jittedCodeAddress, MapLevels);

        ExMgrPtr top = new ExMgrPtr(topRangeSectionMap.TopLevelData);

        ExMgrPtr nextLevelAddress = top.Offset(target.PointerSize, topLevelIndex);
        for (int level = MapLevels - 1; level >= 1; level--)
        {
            ExMgrPtr rangeSectionL = nextLevelAddress.LoadPointer(target);
            if (rangeSectionL.IsNull)
                return ExMgrPtr.Null;
            nextLevelAddress = rangeSectionL.Offset(target.PointerSize, EffectiveBitsForLevel(jittedCodeAddress, level));
        }
        return nextLevelAddress;
    }
}
