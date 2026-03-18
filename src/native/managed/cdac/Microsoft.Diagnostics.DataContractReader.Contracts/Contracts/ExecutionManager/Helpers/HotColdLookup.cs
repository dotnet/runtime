// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal sealed class HotColdLookup
{
    public static HotColdLookup Create(Target target)
        => new HotColdLookup(target);

    private readonly Target _target;

    private HotColdLookup(Target target)
    {
        _target = target;
    }

    public bool TryGetColdFunctionIndex(
        uint numHotColdMap,
        TargetPointer hotColdMap,
        uint runtimeFunctionIndex,
        uint numRuntimeFunctions,
        out uint coldStart,
        out uint coldEnd)
    {
        coldStart = 0;
        coldEnd = 0;

        // The first entry in the HotColdMap is the runtime function index of the first cold part.
        if (TryLookupHotColdMappingForMethod(numHotColdMap, hotColdMap, runtimeFunctionIndex, out uint _, out uint coldIndex))
        {
            coldStart = _target.Read<uint>(hotColdMap + (ulong)coldIndex * sizeof(uint));
            coldEnd = (coldIndex + 2) < numHotColdMap
                ? _target.Read<uint>(hotColdMap + (ulong)(coldIndex + 2) * sizeof(uint)) - 1
                : numRuntimeFunctions - 1;
            return true;
        }
        return false;
    }

    public uint GetHotFunctionIndex(uint numHotColdMap, TargetPointer hotColdMap, uint runtimeFunctionIndex)
    {
        if (!IsColdCode(numHotColdMap, hotColdMap, runtimeFunctionIndex))
            return runtimeFunctionIndex;

        uint hotIndex;
        if (!TryLookupHotColdMappingForMethod(numHotColdMap, hotColdMap, runtimeFunctionIndex, out hotIndex, out _))
            return runtimeFunctionIndex;

        // If runtime function is in the cold part, get the associated hot part
        Debug.Assert(hotIndex % 2 != 0, "Hot part index should be an odd number");
        return _target.Read<uint>(hotColdMap + (ulong)hotIndex * sizeof(uint));
    }

    public bool TryGetColdFunctionIndex(uint numHotColdMap, TargetPointer hotColdMap, uint runtimeFunctionIndex, out uint functionIndex)
    {
        functionIndex = ~0u;

        uint coldIndex;
        if (!TryLookupHotColdMappingForMethod(numHotColdMap, hotColdMap, runtimeFunctionIndex, out uint _, out coldIndex))
            return false;

        Debug.Assert(coldIndex % 2 == 0, "Cold part index should be an even number");
        functionIndex = _target.Read<uint>(hotColdMap + (ulong)coldIndex * sizeof(uint));
        return true;
    }

    private bool IsColdCode(uint numHotColdMap, TargetPointer hotColdMap, uint runtimeFunctionIndex)
    {
        if (numHotColdMap == 0)
            return false;

        // Determine if the method index represents a hot or cold part by comparing against the first
        // cold part index (hot < cold).
        uint firstColdRuntimeFunctionIndex = _target.Read<uint>(hotColdMap);
        return runtimeFunctionIndex >= firstColdRuntimeFunctionIndex;
    }

    // Look up a runtime function index in the hot/cold map. If the function is in the
    // hot/cold map, return whether the function corresponds to cold code and the hot
    // and cold lookup indexes for the function.
    private bool TryLookupHotColdMappingForMethod(
        uint numHotColdMap,
        TargetPointer hotColdMap,
        uint runtimeFunctionIndex,
        out uint hotIndex,
        out uint coldIndex)
    {
        hotIndex = ~0u;
        coldIndex = ~0u;

        // HotColdMappingLookupTable::LookupMappingForMethod
        if (numHotColdMap == 0)
            return false;

        // Each method is represented by a pair of unsigned 32-bit integers. First is the runtime
        // function index of the cold part, second is the runtime function index of the hot part.
        // HotColdMap is these pairs as an array, so the logical size is half the array size.
        uint start = 0;
        uint end = (numHotColdMap - 1) / 2;

        bool isColdCode = IsColdCode(numHotColdMap, hotColdMap, runtimeFunctionIndex);
        uint indexCorrection = isColdCode ? 0u : 1u;

        // Index used for the search is the logical index of hot/cold pairs. We double it to index
        // into the HotColdMap array.
        if (BinaryThenLinearSearch.Search(start, end, Compare, Match, out uint index))
        {
            hotIndex = index * 2 + 1;
            coldIndex = index * 2;
            return true;
        }

        return false;

        bool Compare(uint index)
        {
            index = index * 2 + indexCorrection;
            return runtimeFunctionIndex < _target.Read<uint>(hotColdMap + (index * sizeof(uint)));
        }

        bool Match(uint index)
        {
            index *= 2;

            uint value = _target.Read<uint>(hotColdMap + (ulong)(index + indexCorrection) * sizeof(uint));
            if (value == runtimeFunctionIndex)
                return true;

            // If function index is a cold funclet from a cold block, the above check for equality will fail.
            // To get its corresponding hot block, find the cold block containing the funclet,
            // then use the lookup table.
            // The cold funclet's function index will be greater than its cold block's function index,
            // but less than the next cold block's function index in the lookup table.
            if (isColdCode && runtimeFunctionIndex > _target.Read<uint>(hotColdMap + (ulong)index * sizeof(uint)))
            {
                bool isFuncletIndex = index + 2 == numHotColdMap
                    || runtimeFunctionIndex < _target.Read<uint>(hotColdMap + (ulong)(index + 2) * sizeof(uint));
                if (isFuncletIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
