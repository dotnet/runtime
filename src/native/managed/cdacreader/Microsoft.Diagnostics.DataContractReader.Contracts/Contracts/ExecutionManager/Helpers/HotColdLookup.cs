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

    public uint GetHotFunctionIndex(uint numHotColdMap, TargetPointer hotColdMap, uint runtimeFunctionIndex)
    {
        int lookupIndex = LookupHotColdMappingForMethod(numHotColdMap, hotColdMap, runtimeFunctionIndex);

        // If runtime function is in the cold part, get the associated hot part
        if (lookupIndex != -1 && (lookupIndex & 1) == 1)
            runtimeFunctionIndex = _target.Read<uint>(hotColdMap + (ulong)lookupIndex * sizeof(uint));

        return runtimeFunctionIndex;
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

    public bool TryGetColdFunctionIndex(uint numHotColdMap, TargetPointer hotColdMap, uint runtimeFunctionIndex, out uint lookupIndex, out uint coldFunctionIndex)
    {
        Debug.Assert(!IsColdCode(numHotColdMap, hotColdMap, runtimeFunctionIndex));

        lookupIndex = ~0u;
        coldFunctionIndex = ~0u;
        int lookupIndexLocal = LookupHotColdMappingForMethod(numHotColdMap, hotColdMap, runtimeFunctionIndex);

        // Runtime function has no cold part
        if (lookupIndexLocal == -1)
            return false;

        Debug.Assert((lookupIndexLocal & 1) == 0);
        lookupIndex = (uint)lookupIndexLocal;
        coldFunctionIndex = _target.Read<uint>(hotColdMap + (ulong)lookupIndexLocal * sizeof(uint));
        return true;
    }

    // Look up a runtime function index in the hot/cold map
    // If the runtime function index is:
    //  - cold and in the map, returns the index of the hot part in the hot/cold map
    //  - hot and in the map, returns the index of the cold part in the hot/cold map
    //  - not in the map, returns -1
    private int LookupHotColdMappingForMethod(uint numHotColdMap, TargetPointer hotColdMap, uint runtimeFunctionIndex)
    {
        // HotColdMappingLookupTable::LookupMappingForMethod
        if (numHotColdMap == 0)
            return -1;

        // Each method is represented by a pair of unsigned 32-bit integers. First is the runtime
        // function index of the cold part, second is the runtime function index of the hot part.
        // HotColdMap is these pairs as an array, so the logical size is half the array size.
        uint start = 0;
        uint end = (numHotColdMap - 1) / 2;

        bool isColdCode = IsColdCode(numHotColdMap, hotColdMap, runtimeFunctionIndex);
        int indexCorrection = isColdCode ? 0 : 1;

        // Entries are sorted by the hot part runtime function indices. This also means they are sorted
        // by the cold part indices, as the cold part is emitted in the same order as hot parts.
        // Binary search until we get to 10 or fewer items.
        while (end - start > 10)
        {
            uint middle = start + (end - start) / 2;
            long index = middle * 2 + indexCorrection;

            if (runtimeFunctionIndex < _target.Read<uint>(hotColdMap + (ulong)(index * sizeof(uint))))
            {
                end = middle - 1;
            }
            else
            {
                start = middle;
            }
        }

        // Find the hot/cold map index corresponding to the cold/hot runtime function index
        for (uint i = start; i <= end; ++i)
        {
            uint index = i * 2;

            uint value = _target.Read<uint>(hotColdMap + (ulong)(index + indexCorrection) * sizeof(uint));
            if (value == runtimeFunctionIndex)
            {
                return isColdCode
                    ? (int)index + 1
                    : (int)index;
            }
            else if (isColdCode && runtimeFunctionIndex > _target.Read<uint>(hotColdMap + (ulong)index * sizeof(uint)))
            {
                // If function index is a cold funclet from a cold block, the above check for equality will fail.
                // To get its corresponding hot block, find the cold block containing the funclet,
                // then use the lookup table.
                // The cold funclet's function index will be greater than its cold block's function index,
                // but less than the next cold block's function index in the lookup table.
                bool isFuncletIndex = index + 2 == numHotColdMap
                    || runtimeFunctionIndex < _target.Read<uint>(hotColdMap + (ulong)(index + 2) * sizeof(uint));
                if (isFuncletIndex)
                {
                    return (int)index + 1;
                }
            }
        }

        return -1;
    }
}
