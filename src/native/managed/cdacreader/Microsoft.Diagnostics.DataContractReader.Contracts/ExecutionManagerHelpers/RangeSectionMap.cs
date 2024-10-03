// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;


internal readonly struct RangeSectionMap
{
    private int MapLevels { get; }
    private int BitsPerLevel { get; } = 8;
    private int MaxSetBit { get; }
    private int EntriesPerMapLevel { get; } = 256;

    /// <summary>
    /// The entries in the non-leaf levels of the map are pointers to the next level of the map together
    /// with a collectible flag on the lowest bit.
    /// </summary>
    internal struct InteriorMapValue
    {
        public readonly TargetPointer RawValue;

        public TargetPointer Address => RawValue & ~1ul;

        public bool IsNull => Address == TargetPointer.Null;

        public InteriorMapValue(TargetPointer value)
        {
            RawValue = value;
        }
    }

    /// <summary>
    /// Points at a slot in the RangeSectionMap that corresponds to an address range that includes a particular address.
    /// </summary>
    /// <remarks>
    /// Represented as a pointer to the current map level and an index into that map level.
    /// </remarks>
    internal readonly struct Cursor
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

        /// <summary>
        /// Gets the address of the slot in the current level's map that the cursor points to
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public TargetPointer GetSlot(Target target) => LevelMap + (ulong)(Index * target.PointerSize);

        // the values at the non-leaf levels are pointers with the last bit stolen for the collectible flag
        public InteriorMapValue LoadValue(Target target) => new InteriorMapValue(target.ReadPointer(GetSlot(target)));

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

    private RangeSectionMap(int mapLevels, int maxSetBit)
    {
        MapLevels = mapLevels;
        MaxSetBit = maxSetBit;
    }
    public static RangeSectionMap Create(Target target)
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

    /// <summary>
    /// Find the location of the fragment that contains the given jitted code address.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="topRangeSectionMap"></param>
    /// <param name="jittedCodeAddress"></param>
    /// <returns>TargetPointer.Null if the map doesn't contain a fragment that could cover this address range</returns>
    public TargetPointer /*PTR_RangeSectionFragment*/ FindFragment(Target target, Data.RangeSectionMap topRangeSectionMap, TargetCodePointer jittedCodeAddress)
    {
        return FindFragmentInternal(target, topRangeSectionMap.TopLevelData, jittedCodeAddress)?.LoadValue(target).Address ?? TargetPointer.Null;
    }

    internal Cursor? FindFragmentInternal(Target target, TargetPointer topMap, TargetCodePointer jittedCodeAddress)
    {
        Cursor cursor = GetTopCursor(topMap, jittedCodeAddress);
        while (!cursor.IsLeaf)
        {
            InteriorMapValue nextLevel = cursor.LoadValue(target);
            if (nextLevel.IsNull)
                return null;
            cursor = GetNextCursor(target, cursor, jittedCodeAddress);
        }
        return cursor;
    }
}
