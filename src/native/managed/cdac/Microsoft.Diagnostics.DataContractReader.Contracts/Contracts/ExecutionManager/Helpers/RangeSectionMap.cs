// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;


/// <summary>
/// Algorithm for lookups in a multi-level map that covers a large address space
/// </summary>
/// <remarks><![CDATA[
/// The range section map logically partitions the entire 32-bit or 64-bit addressable space into chunks.
/// The map is implemented with multiple levels, where the bits of an address are used as indices into an array of
/// pointers.  The upper levels of the map point to the next level down. At the lowest level of the map, the pointers
/// point to the first range section fragment containing addresses in the chunk.
///
/// On 32-bit targets a 2 level map is used
///
/// | 31-24 | 23-16 | 15-0 |
/// |:----:|:----:|:----:|
/// | L2 | L1 | chunk |
///
/// That is, level 2 in the map has 256 entries pointing to level 1 maps (or null if there's nothing allocated), each
/// level 1 map has 256 entries covering a 64 KiB chunk and pointing to a linked list of range section fragments that
/// fall within that 64 KiB chunk.
///
/// On 64-bit targets, we take advantage of the fact that most architectures don't support a full 64-bit addressable
/// space: arm64 supports 52 bits of addressable memory and x86-64 supports 57 bits.  The runtime ignores the top
/// bits 63-57 and uses 5 levels of mapping
///
/// | 63-57 | 56-49 | 48-41 | 40-33 | 32-25 | 24-17 | 16-0 |
/// |:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|:----:|
/// | unused | L5 | L4 | L3 | L2 | L1 | chunk |
///
/// That is, level 5 has 256 entires pointing to level 4 maps (or nothing if there's no code allocated in that
/// address range), level 4 entires point to level 3 maps and so on.  Each level 1 map has 256 entries covering
/// a 128 KiB chunk and pointing to a linked list of range section fragments that fall within that 128 KiB chunk.
/// ]]></remarks>
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
        int index = GetIndexForLevel(jittedCodeAddress, MapLevels);
        return new Cursor(topMap, MapLevels, index);
    }

    internal Cursor GetNextCursor(Target target, Cursor cursor, TargetCodePointer jittedCodeAddress)
    {
        int nextLevel = cursor.Level - 1;
        int nextIndex = GetIndexForLevel(jittedCodeAddress, nextLevel);
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
    internal int GetIndexForLevel(TargetCodePointer address, int level)
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
