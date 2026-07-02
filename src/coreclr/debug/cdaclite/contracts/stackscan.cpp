// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// stackscan.cpp
//
// Implementation of the Normal-tier conservative stack scan declared in
// stackscan.h. Ports the ExecutionManager RangeSectionMap radix lookup and the
// EEJitManager NibbleMap (IP -> method code) so that, for each code pointer found
// on a thread stack, only that method's code + header + MethodDesc is captured --
// avoiding the bulk emission of every JIT/loader heap.
//*****************************************************************************

#include "stackscan.h"
#include "runtimetypes.h"

#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        const char* const GlobalThreadStore = "ThreadStore";
        const char* const GlobalCodeRangeMap = "ExecutionManagerCodeRangeMapAddress";
        const char* const GlobalStubCodeBlockLast = "StubCodeBlockLast";

        const int MaxThreads = 100000;
        const int MaxFragments = 100000;

        // RangeSection.Flags
        const int32_t RangeSectionFlag_CodeHeap = 0x02;

        // NibbleMap constants (see NibbleMapHelpers.cs).
        const uint64_t MapUnitSizeInBytes = 4;
        const uint64_t MapUnitSizeInNibbles = 8;
        const uint64_t BytesPerBucket = 8 * MapUnitSizeInBytes; // 32
        const uint32_t NibbleMask = 0x0Fu;

        // Bound how much code we emit around a stack IP (covers the method body + code header).
        const uint32_t MaxMethodCodeEmit = 64 * 1024;
        // Conservative fixed emission for a MethodDesc (its exact size is chunk-dependent).
        const uint32_t MethodDescEmit = 512;

        // --- RangeSectionMap radix lookup (ExecutionManagerHelpers.RangeSectionMap) ---------

        int MapLevels(const Target& target) { return target.PointerSize() == 8 ? 5 : 2; }
        int MaxSetBit(const Target& target) { return target.PointerSize() == 8 ? 56 : 31; }

        int GetIndexForLevel(const Target& target, uint64_t address, int level)
        {
            const int bitsPerLevel = 8;
            uint64_t used = address >> (MaxSetBit(target) + 1 - (MapLevels(target) * bitsPerLevel));
            uint64_t shifted = used >> ((level - 1) * bitsPerLevel);
            return (int)(255u & shifted);
        }

        // Walks the radix map for 'addr' and returns the RangeSection covering it, or 0.
        // Emits each map-node pointer slot it reads (the reader re-walks the same slots).
        uint64_t FindRangeSection(const Target& target, uint64_t topMap, uint64_t addr,
                                  RegionCallback sink, void* sinkContext)
        {
            const uint64_t ptrSize = target.PointerSize();
            uint64_t levelMap = topMap;
            int level = MapLevels(target);
            uint64_t firstFragment = 0;
            for (;;)
            {
                int index = GetIndexForLevel(target, addr, level);
                uint64_t slot = levelMap + (uint64_t)index * ptrSize;
                uint64_t value = 0;
                if (!target.TryReadPointer(slot, value))
                {
                    return 0;
                }
                // The reader reads this same slot during its own lookup.
                sink(sinkContext, "code-rangemap", slot, (uint64_t)ptrSize);
                value &= ~1ull; // low bit is the collectible flag
                if (level == 1)
                {
                    firstFragment = value;
                    break;
                }
                if (value == 0)
                {
                    return 0;
                }
                levelMap = value;
                level--;
            }

            // Walk the fragment list at the leaf to find the covering RangeSection.
            uint64_t fragment = firstFragment;
            for (int i = 0; fragment != 0 && i < MaxFragments; i++)
            {
                data::RangeSectionFragment f;
                if (!target.TryRead(fragment, f)) // struct read -> captured by EnumMem
                {
                    break;
                }
                if (addr >= f.RangeBegin && addr < f.RangeEndOpen)
                {
                    return f.RangeSection;
                }
                fragment = f.Next;
            }
            return 0;
        }

        // --- NibbleMap: IP -> start of the containing method's code (NibbleMapConstantLookup) --

        uint32_t ReadMapUnit(const Target& target, uint64_t mapStart, uint64_t mapIdx,
                             RegionCallback sink, void* sinkContext)
        {
            uint64_t unitAddr = mapStart + (mapIdx / MapUnitSizeInNibbles) * MapUnitSizeInBytes;
            uint32_t value = 0;
            target.TryReadUInt32(unitAddr, value);
            // The reader re-reads the same map units.
            sink(sinkContext, "code-nibblemap", unitAddr, (uint64_t)MapUnitSizeInBytes);
            return value;
        }

        bool IsPointerUnit(uint32_t unit) { return (unit & NibbleMask) > 8; }

        uint64_t DecodePointer(uint64_t baseAddress, uint32_t unit)
        {
            uint32_t nibble = unit & NibbleMask;
            uint32_t relativePointer = (unit & ~NibbleMask) + ((nibble - 9) << 2);
            return baseAddress + relativePointer;
        }

        uint32_t GetNibbleShift(uint64_t mapIdx)
        {
            uint32_t nibbleIndexInMapUnit = (uint32_t)(mapIdx & (MapUnitSizeInNibbles - 1));
            return 28 - (nibbleIndexInMapUnit * 4);
        }

        uint64_t GetAbsoluteAddress(uint64_t baseAddress, uint64_t mapIdx, uint32_t nibble)
        {
            uint64_t mapIdxByteOffset = mapIdx * BytesPerBucket;
            uint64_t nibbleByteOffset = (uint64_t)(nibble - 1) * MapUnitSizeInBytes;
            return baseAddress + mapIdxByteOffset + nibbleByteOffset;
        }

        // Returns the code start address for 'currentPC', or 0. Faithful port of
        // NibbleMapConstantLookup.FindMethodCode.
        uint64_t NibbleMapFindMethodCode(const Target& target, uint64_t mapBase, uint64_t mapStart,
                                         uint64_t currentPC, RegionCallback sink, void* sinkContext)
        {
            uint64_t relativeAddress = currentPC - mapBase;
            uint64_t mapIdx = relativeAddress / BytesPerBucket;
            uint32_t bucketByteIndex = (uint32_t)((relativeAddress & (BytesPerBucket - 1)) / MapUnitSizeInBytes) + 1;

            uint32_t t = ReadMapUnit(target, mapStart, mapIdx, sink, sinkContext);
            if (IsPointerUnit(t))
            {
                return DecodePointer(mapBase, t);
            }

            // Focus on the indexed nibble.
            t = t >> GetNibbleShift(mapIdx);
            uint32_t nibble = t & NibbleMask;
            if (nibble != 0 && nibble <= bucketByteIndex)
            {
                return GetAbsoluteAddress(mapBase, mapIdx, nibble);
            }

            // Search backwards through the current map unit.
            t = t >> 4; // shift to next nibble
            if (t != 0)
            {
                if (mapIdx == 0)
                {
                    return 0;
                }
                mapIdx = mapIdx - 1;
                while ((t & NibbleMask) == 0)
                {
                    t = t >> 4;
                    if (mapIdx == 0)
                    {
                        break;
                    }
                    mapIdx = mapIdx - 1;
                }
                return GetAbsoluteAddress(mapBase, mapIdx, t & NibbleMask);
            }

            // We finished the current map unit; if we were in the first, stop.
            if (mapIdx < MapUnitSizeInNibbles)
            {
                return 0;
            }

            // Align down to the map unit, then move back one nibble into the previous unit.
            mapIdx = mapIdx & (~(MapUnitSizeInNibbles - 1));
            mapIdx = mapIdx - 1;

            t = ReadMapUnit(target, mapStart, mapIdx, sink, sinkContext);
            if (t == 0)
            {
                return 0;
            }
            if (IsPointerUnit(t))
            {
                return DecodePointer(mapBase, t);
            }

            while (mapIdx != 0 && (t & NibbleMask) == 0)
            {
                t = t >> 4;
                mapIdx = mapIdx - 1;
            }
            return GetAbsoluteAddress(mapBase, mapIdx, t & NibbleMask);
        }

        // Resolves a code pointer 'ip' to its method and emits the code + header + MethodDesc.
        // Returns the MethodDesc address (deduped by the caller), or 0 if not resolved.
        uint64_t ResolveCodeIP(const Target& target, uint64_t codeRangeMap, uint64_t stubCodeBlockLast,
                               uint64_t ip, RegionCallback sink, void* sinkContext)
        {
            uint64_t rangeSectionAddr = FindRangeSection(target, codeRangeMap, ip, sink, sinkContext);
            if (rangeSectionAddr == 0)
            {
                return 0;
            }

            data::RangeSection rangeSection;
            if (!target.TryRead(rangeSectionAddr, rangeSection))
            {
                return 0;
            }

            // Only EEJit code heaps are resolved here; R2R code + method info live in the module
            // image (available to the reader from the on-disk binary).
            if (((int32_t)rangeSection.Flags & RangeSectionFlag_CodeHeap) == 0 || rangeSection.HeapList == 0)
            {
                return 0;
            }

            data::CodeHeapListNode heapNode;
            if (!target.TryRead(rangeSection.HeapList, heapNode))
            {
                return 0;
            }
            if (ip < heapNode.StartAddress || ip > heapNode.EndAddress)
            {
                return 0;
            }

            uint64_t codeStart = NibbleMapFindMethodCode(target, heapNode.MapBase, heapNode.HeaderMap, ip, sink, sinkContext);
            if (codeStart == 0 || codeStart > ip)
            {
                return 0;
            }

            // The code header pointer is stored one pointer before the code start.
            uint64_t codeHeaderIndirect = codeStart - target.PointerSize();
            uint64_t codeHeaderAddress = 0;
            if (!target.TryReadPointer(codeHeaderIndirect, codeHeaderAddress) || codeHeaderAddress == 0)
            {
                return 0;
            }
            sink(sinkContext, "code-header-ptr", codeHeaderIndirect, (uint64_t)target.PointerSize());

            // Stub code blocks have a small sentinel value instead of a real header.
            if (codeHeaderAddress <= stubCodeBlockLast)
            {
                return 0;
            }

            data::RealCodeHeader codeHeader;
            if (!target.TryRead(codeHeaderAddress, codeHeader)) // captured by EnumMem
            {
                return 0;
            }

            // Emit the method's code (code start through the IP, bounded).
            uint64_t emitEnd = ip + target.PointerSize();
            uint64_t codeSize = emitEnd - codeStart;
            if (codeSize > MaxMethodCodeEmit)
            {
                codeSize = MaxMethodCodeEmit;
            }
            sink(sinkContext, "jit-code", codeStart, codeSize);

            // Emit the MethodDesc (its exact size is chunk-dependent; a bounded blob suffices).
            if (codeHeader.MethodDesc != 0)
            {
                sink(sinkContext, "methoddesc", codeHeader.MethodDesc, MethodDescEmit);
            }

            return codeHeader.MethodDesc;
        }
    }

    int EnumerateStackScanRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        uint64_t codeRangeMapPtr = 0;
        if (!target.TryReadGlobalPointer(GlobalCodeRangeMap, codeRangeMapPtr) || codeRangeMapPtr == 0)
        {
            return -1;
        }
        // The global points at the RangeSectionMap; its TopLevelData is the radix map root.
        data::RangeSectionMap rangeSectionMap;
        if (!target.TryRead(codeRangeMapPtr, rangeSectionMap) || rangeSectionMap.TopLevelData == 0)
        {
            return -1;
        }
        uint64_t topMap = rangeSectionMap.TopLevelData;

        uint64_t stubCodeBlockLast = 0;
        target.TryGetGlobalValue(GlobalStubCodeBlockLast, stubCodeBlockLast);

        uint64_t threadStoreAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalThreadStore, threadStoreAddr) || threadStoreAddr == 0)
        {
            return -1;
        }
        data::ThreadStore threadStore;
        if (!target.TryRead(threadStoreAddr, threadStore))
        {
            return -1;
        }

        const uint64_t ptrSize = target.PointerSize();
        std::set<uint64_t> visitedThreads;
        std::set<uint64_t> capturedMethods;

        uint64_t threadAddr = threadStore.FirstThreadLink;
        for (int i = 0; threadAddr != 0 && i < MaxThreads; i++)
        {
            if (!visitedThreads.insert(threadAddr).second)
            {
                break;
            }
            data::Thread thread;
            if (!target.TryRead(threadAddr, thread))
            {
                break;
            }

            uint64_t stackLow = thread.CachedStackLimit;
            uint64_t stackHigh = thread.CachedStackBase;
            if (stackLow != 0 && stackHigh > stackLow)
            {
                // Conservatively scan every pointer-aligned slot for code pointers.
                for (uint64_t sp = stackLow; sp + ptrSize <= stackHigh; sp += ptrSize)
                {
                    uint64_t value = 0;
                    if (!target.TryReadPointer(sp, value) || value == 0)
                    {
                        continue;
                    }
                    uint64_t methodDesc = ResolveCodeIP(target, topMap, stubCodeBlockLast, value, sink, sinkContext);
                    if (methodDesc != 0)
                    {
                        capturedMethods.insert(methodDesc);
                    }
                }
            }

            threadAddr = thread.LinkNext;
        }

        return (int)capturedMethods.size();
    }
}
} // namespace contracts
