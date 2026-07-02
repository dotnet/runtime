// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// gc.cpp
//
// Implementation of the GC heap-region walk declared in gc.h.
//*****************************************************************************

#include "gc.h"
#include "runtimetypes.h"

#include <stdio.h>
#include <string>
#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // GC globals (src/coreclr/gc/datadescriptor/datadescriptor.inc).
        const char* const GlobalGCIdentifiers = "GCIdentifiers";
        const char* const GlobalTotalGenerationCount = "TotalGenerationCount";
        const char* const GlobalNumHeaps = "NumHeaps";
        const char* const GlobalHeaps = "Heaps";
        const char* const GlobalGenerationTable = "GCHeapGenerationTable";
        const char* const GlobalEphemeralHeapSegment = "GCHeapEphemeralHeapSegment";
        const char* const GlobalAllocAllocated = "GCHeapAllocAllocated";
        const char* const GlobalFinalizeQueue = "GCHeapFinalizeQueue";
        const char* const GlobalFillPointersLength = "CFinalizeFillPointersLength";

        // Guard against runaway walks over corrupt segment lists.
        const int MaxSegmentsPerList = 1024 * 1024;
        const uint32_t DefaultGenerationCount = 5; // gen0, gen1, gen2, LOH, POH

        // Per-heap parameters gathered either from globals (WKS) or heap fields (SVR).
        struct HeapContext
        {
            uint64_t generationTableBase = 0;
            uint64_t ephemeralSegment = 0;
            uint64_t allocAllocated = 0;
            uint64_t finalizeQueue = 0;
        };

        // Reads the CFinalize struct at 'finalizeQueueAddr' (captured via EnumMem) and emits the
        // inline fill-pointers array the cDAC re-reads in GetFillPointers. Mirrors GetGCHeapDataFromHeap.
        void EnumFinalizeQueue(const Target& target, uint64_t finalizeQueueAddr)
        {
            if (finalizeQueueAddr == 0)
            {
                return;
            }
            data::CFinalize cfinalize;
            if (!target.TryRead(finalizeQueueAddr, cfinalize))
            {
                return;
            }
            uint32_t fillPointersLength = 0;
            uint64_t len = 0;
            if (target.TryGetGlobalValue(GlobalFillPointersLength, len))
            {
                fillPointersLength = (uint32_t)len;
            }
            if (fillPointersLength > 0)
            {
                target.EmitMemory(cfinalize.FillPointers, fillPointersLength * (uint32_t)target.PointerSize());
            }
        }

        // Emits a WKS gc_heap data array (InterestingData/CompactReasons/etc.). 'pointerGlobal' is a
        // GLOBAL_POINTER whose resolved value is the inline array's address (GCHeapWKS reads it via
        // ReadGlobalPointer with no extra deref); 'lengthGlobal' is a direct-value length. Each element
        // is pointer-sized (TargetNUInt). Mirrors GetGCHeapDataFromHeap.ReadGCHeapDataArray.
        void EnumDataArray(const Target& target, const char* pointerGlobal, const char* lengthGlobal)
        {
            uint64_t arrayStart = 0;
            if (!target.TryGetGlobalValue(pointerGlobal, arrayStart) || arrayStart == 0)
            {
                return;
            }
            uint64_t length = 0;
            if (target.TryGetGlobalValue(lengthGlobal, length) && length > 0)
            {
                target.EmitMemory(arrayStart, (uint32_t)length * (uint32_t)target.PointerSize());
            }
        }

        // Walks a HeapSegment.Next list from 'start', reading each segment (captured via EnumMem).
        // Mirrors GC_1.AddSegmentList. Bounded by MaxSegmentsPerList.
        void EnumSegmentList(const Target& target, uint64_t start, std::set<uint64_t>& visited)
        {
            uint64_t curr = start;
            for (int i = 0; curr != 0 && i < MaxSegmentsPerList; i++)
            {
                if (!visited.insert(curr).second)
                {
                    break;
                }
                data::HeapSegment segment;
                if (!target.TryRead(curr, segment))
                {
                    break;
                }
                curr = segment.Next;
            }
        }

        // Reads a RegionFreeList at 'freeListAddr' (captured via EnumMem) and walks its
        // HeadFreeRegion segment list. Mirrors GC_1.AddFreeList.
        void EnumFreeList(const Target& target, uint64_t freeListAddr, std::set<uint64_t>& visited)
        {
            if (freeListAddr == 0)
            {
                return;
            }
            data::RegionFreeList freeList;
            if (!target.TryRead(freeListAddr, freeList))
            {
                return;
            }
            if (freeList.HeadFreeRegion != 0)
            {
                EnumSegmentList(target, freeList.HeadFreeRegion, visited);
            }
        }

        // Enumerates the GC bookkeeping card-table info list. Mirrors GC_1.GetGCBookkeepingMemoryRegions:
        // read CardTableInfo at bookkeeping_start, then walk NextCardTable (each node at next - size).
        void EnumBookkeeping(const Target& target)
        {
            uint64_t bookkeepingStart = 0;
            if (!target.TryReadGlobalPointer("BookkeepingStart", bookkeepingStart) || bookkeepingStart == 0)
            {
                return;
            }
            uint64_t cardTableInfoSize = 0;
            target.TryGetGlobalValue("CardTableInfoSize", cardTableInfoSize);

            data::CardTableInfo first;
            if (!target.TryRead(bookkeepingStart, first))
            {
                return;
            }
            uint64_t next = first.NextCardTable;
            uint64_t firstNext = next;
            for (int i = 0; next != 0 && next > cardTableInfoSize && i < 4096; i++)
            {
                uint64_t ctAddr = next - cardTableInfoSize;
                data::CardTableInfo ct;
                if (!target.TryRead(ctAddr, ct))
                {
                    break;
                }
                next = ct.NextCardTable;
                if (next == firstNext)
                {
                    break;
                }
            }
        }

        // Enumerates GC free regions (global + per-heap). Mirrors GC_1.GetGCFreeRegions: reads the
        // RegionFreeList arrays and freeable segment lists the cDAC traverses.
        void EnumFreeRegions(const Target& target, bool isServer, uint64_t heapTable, uint32_t numHeaps,
                             std::set<uint64_t>& visited)
        {
            uint64_t countFreeRegionKinds64 = 0;
            target.TryGetGlobalValue("CountFreeRegionKinds", countFreeRegionKinds64);
            uint32_t countFreeRegionKinds = (countFreeRegionKinds64 > 16) ? 16 : (uint32_t)countFreeRegionKinds64;

            uint32_t regionFreeListSize = 0;
            target.TryGetTypeSize("RegionFreeList", regionFreeListSize);

            // Global free huge regions (a single inline RegionFreeList).
            uint64_t hugeBase = 0;
            if (target.TryGetGlobalValue("GlobalFreeHugeRegions", hugeBase) && hugeBase != 0)
            {
                EnumFreeList(target, hugeBase, visited);
            }

            // Global regions to decommit (an inline array of countFreeRegionKinds RegionFreeLists).
            uint64_t decommitBase = 0;
            if (target.TryGetGlobalValue("GlobalRegionsToDecommit", decommitBase) && decommitBase != 0 && regionFreeListSize != 0)
            {
                for (uint32_t i = 0; i < countFreeRegionKinds; i++)
                {
                    EnumFreeList(target, decommitBase + (uint64_t)i * regionFreeListSize, visited);
                }
            }

            if (isServer)
            {
                for (uint32_t h = 0; h < numHeaps; h++)
                {
                    uint64_t heapAddress = 0;
                    if (!target.TryReadPointer(heapTable + (uint64_t)h * target.PointerSize(), heapAddress) || heapAddress == 0)
                    {
                        continue;
                    }
                    data::GCHeap gcHeap;
                    if (!target.TryRead(heapAddress, gcHeap))
                    {
                        continue;
                    }
                    if (gcHeap.FreeRegions != 0 && regionFreeListSize != 0)
                    {
                        for (uint32_t j = 0; j < countFreeRegionKinds; j++)
                        {
                            EnumFreeList(target, gcHeap.FreeRegions + (uint64_t)j * regionFreeListSize, visited);
                        }
                    }
                    if (gcHeap.FreeableSohSegment != 0)
                    {
                        EnumSegmentList(target, gcHeap.FreeableSohSegment, visited);
                    }
                    if (gcHeap.FreeableUohSegment != 0)
                    {
                        EnumSegmentList(target, gcHeap.FreeableUohSegment, visited);
                    }
                }
            }
            else
            {
                // Workstation GC: free regions from globals (inline RegionFreeList array).
                uint64_t freeRegionsBase = 0;
                if (target.TryGetGlobalValue("GCHeapFreeRegions", freeRegionsBase) && freeRegionsBase != 0 && regionFreeListSize != 0)
                {
                    for (uint32_t i = 0; i < countFreeRegionKinds; i++)
                    {
                        EnumFreeList(target, freeRegionsBase + (uint64_t)i * regionFreeListSize, visited);
                    }
                }
                // Freeable SOH/UOH segments (pointer globals -> deref to segment).
                uint64_t soh = 0;
                if (target.TryReadGlobalPointer("GCHeapFreeableSohSegment", soh) && soh != 0)
                {
                    EnumSegmentList(target, soh, visited);
                }
                uint64_t uoh = 0;
                if (target.TryReadGlobalPointer("GCHeapFreeableUohSegment", uoh) && uoh != 0)
                {
                    EnumSegmentList(target, uoh, visited);
                }
            }
        }

        bool GcIdentifiersContains(const std::string& identifiers, const char* token)
        {
            // GCIdentifiers is a comma-separated list, e.g. "workstation, segments".
            std::string needle(token);
            size_t pos = 0;
            while (pos < identifiers.size())
            {
                size_t comma = identifiers.find(',', pos);
                size_t end = (comma == std::string::npos) ? identifiers.size() : comma;

                size_t start = pos;
                while (start < end && (identifiers[start] == ' ' || identifiers[start] == '\t')) { start++; }
                size_t stop = end;
                while (stop > start && (identifiers[stop - 1] == ' ' || identifiers[stop - 1] == '\t')) { stop--; }

                if (identifiers.compare(start, stop - start, needle) == 0)
                {
                    return true;
                }
                if (comma == std::string::npos) { break; }
                pos = comma + 1;
            }
            return false;
        }

        // Walks one generation's segment list (following HeapSegment.Next), reporting
        // each segment's used range. 'visited' de-duplicates segments shared across
        // generation lists.
        int WalkSegmentList(const Target& target, const HeapContext& heap, unsigned generation, uint64_t startSegment,
                            std::set<uint64_t>& visited, RegionCallback sink, void* sinkContext)
        {
            char kind[16];
            snprintf(kind, sizeof(kind), "gc-gen%u", generation);

            int count = 0;
            uint64_t seg = startSegment;
            for (int i = 0; seg != 0 && i < MaxSegmentsPerList; i++)
            {
                if (!visited.insert(seg).second)
                {
                    break; // already seen (or a cycle)
                }

                data::HeapSegment segment;
                if (!target.TryRead(seg, segment))
                {
                    break;
                }

                // The ephemeral segment's live end is alloc_allocated, not the segment's
                // own allocated pointer.
                uint64_t end = (seg == heap.ephemeralSegment && heap.allocAllocated != 0)
                    ? heap.allocAllocated
                    : segment.Allocated;
                if (segment.Mem != 0 && end > segment.Mem)
                {
                    // Extend the low bound by one pointer to include the first object's header
                    // (the sync-block-index DWORD sits just below segment.Mem; the cDAC reads it
                    // via Object.TryGetHashCode -> ObjectHeader at object-sizeof(header)).
                    uint64_t start = segment.Mem - target.PointerSize();
                    sink(sinkContext, kind, start, end - start);
                    count++;
                }

                seg = segment.Next;
            }
            return count;
        }

        int WalkHeap(const Target& target, const HeapContext& heap, uint32_t generationCount,
                     std::set<uint64_t>& visited, RegionCallback sink, void* sinkContext)
        {
            uint32_t generationSize = 0;
            if (!target.TryGetTypeSize(data::Generation().TypeName(), generationSize) || generationSize == 0)
            {
                return 0;
            }

            int count = 0;
            for (uint32_t i = 0; i < generationCount; i++)
            {
                uint64_t generationAddress = heap.generationTableBase + (uint64_t)i * generationSize;
                data::Generation generation;
                if (!target.TryRead(generationAddress, generation))
                {
                    continue;
                }
                count += WalkSegmentList(target, heap, i, generation.StartSegment, visited, sink, sinkContext);
            }
            return count;
        }
    }

    int EnumerateGCHeapRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        std::string identifiers;
        if (!target.TryGetGlobalString(GlobalGCIdentifiers, identifiers))
        {
            return -1;
        }

        bool isServer = GcIdentifiersContains(identifiers, "server");
        bool isWorkstation = GcIdentifiersContains(identifiers, "workstation");
        if (!isServer && !isWorkstation)
        {
            return -1;
        }

        uint32_t generationCount = DefaultGenerationCount;
        uint64_t generationCount64 = 0;
        if (target.TryGetGlobalValue(GlobalTotalGenerationCount, generationCount64) &&
            generationCount64 != 0 && generationCount64 <= 64)
        {
            generationCount = (uint32_t)generationCount64;
        }

        std::set<uint64_t> visited;
        int total = 0;

        if (isWorkstation)
        {
            HeapContext heap;
            if (!target.TryGetGlobalValue(GlobalGenerationTable, heap.generationTableBase))
            {
                return -1;
            }
            target.TryReadGlobalPointer(GlobalEphemeralHeapSegment, heap.ephemeralSegment);
            target.TryReadGlobalPointer(GlobalAllocAllocated, heap.allocAllocated);

            // The finalize queue's CFinalize* value is at the GCHeapFinalizeQueue global.
            uint64_t finalizeQueue = 0;
            target.TryReadGlobalPointer(GlobalFinalizeQueue, finalizeQueue);
            EnumFinalizeQueue(target, finalizeQueue);

            // WKS gc_heap data arrays re-read by GetGCHeapDataFromHeap.
            EnumDataArray(target, "GCHeapInterestingData", "InterestingDataLength");
            EnumDataArray(target, "GCHeapCompactReasons", "CompactReasonsLength");
            EnumDataArray(target, "GCHeapExpandMechanisms", "ExpandMechanismsLength");
            EnumDataArray(target, "GCHeapInterestingMechanismBits", "InterestingMechanismBitsLength");

            total += WalkHeap(target, heap, generationCount, visited, sink, sinkContext);

            // Bookkeeping card tables + free regions (GetGCBookkeepingMemoryRegions / GetGCFreeRegions).
            EnumBookkeeping(target);
            EnumFreeRegions(target, /*isServer*/ false, /*heapTable*/ 0, /*numHeaps*/ 0, visited);
        }
        else
        {
            // Server GC: read NumHeaps and the Heaps array of GCHeap*.
            uint32_t numHeaps = 0;
            if (!target.TryReadGlobalUInt32(GlobalNumHeaps, numHeaps) || numHeaps == 0)
            {
                return total;
            }

            uint64_t heapTable = 0;
            if (!target.TryReadGlobalPointer(GlobalHeaps, heapTable))
            {
                return total;
            }

            // Emit the Heaps pointer array the cDAC re-reads (GetGCHeaps / GetGCFreeRegions).
            target.EmitMemory(heapTable, numHeaps * (uint32_t)target.PointerSize());

            for (uint32_t i = 0; i < numHeaps; i++)
            {
                uint64_t heapAddress = 0;
                if (!target.TryReadPointer(heapTable + (uint64_t)i * target.PointerSize(), heapAddress) || heapAddress == 0)
                {
                    continue;
                }

                data::GCHeap gcHeap;
                if (!target.TryRead(heapAddress, gcHeap))
                {
                    continue;
                }

                HeapContext heap;
                heap.generationTableBase = gcHeap.GenerationTable;
                heap.ephemeralSegment = gcHeap.EphemeralHeapSegment;
                heap.allocAllocated = gcHeap.AllocAllocated;
                heap.finalizeQueue = gcHeap.FinalizeQueue;

                EnumFinalizeQueue(target, heap.finalizeQueue);
                total += WalkHeap(target, heap, generationCount, visited, sink, sinkContext);
            }

            // Bookkeeping card tables + free regions (GetGCBookkeepingMemoryRegions / GetGCFreeRegions).
            EnumBookkeeping(target);
            EnumFreeRegions(target, /*isServer*/ true, heapTable, numHeaps, visited);
        }

        return total;
    }
}
} // namespace contracts
