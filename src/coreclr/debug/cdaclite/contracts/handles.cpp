// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// handles.cpp
//
// Implementation of the GC handle-table walk declared in handles.h.
//*****************************************************************************

#include "handles.h"
#include "runtimetypes.h"

#include <string>
#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // GC handle-table globals (src/coreclr/gc/datadescriptor/datadescriptor.inc,
        // merged into the target from the GC sub-descriptor).
        const char* const GlobalHandleTableMap = "HandleTableMap";           // address of g_HandleTableMap
        const char* const GlobalBucketCount = "InitialHandleTableArraySize"; // direct: bucket array length
        const char* const GlobalSegmentSize = "HandleSegmentSize";           // direct: bytes per segment
        const char* const GlobalTotalCpuCount = "TotalCpuCount";             // pointer: server table count
        const char* const GlobalGCIdentifiers = "GCIdentifiers";

        const int MaxMaps = 100000;
        const int MaxSegments = 1000000;
        const uint32_t MaxBuckets = 4096;
        const uint32_t MaxSegmentSize = 64u * 1024 * 1024;
    }

    int EnumerateHandleRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        uint64_t mapAddr = 0;
        if (!target.TryGetGlobalValue(GlobalHandleTableMap, mapAddr) || mapAddr == 0)
        {
            return -1;
        }

        uint64_t bucketCount64 = 0;
        if (!target.TryGetGlobalValue(GlobalBucketCount, bucketCount64) || bucketCount64 == 0 || bucketCount64 > MaxBuckets)
        {
            return -1;
        }
        uint32_t bucketCount = (uint32_t)bucketCount64;

        uint64_t segmentSize64 = 0;
        target.TryGetGlobalValue(GlobalSegmentSize, segmentSize64);
        uint32_t segmentSize = (segmentSize64 > 0 && segmentSize64 <= MaxSegmentSize) ? (uint32_t)segmentSize64 : 0;
        if (segmentSize == 0)
        {
            return -1; // without a segment size we can't report meaningful regions
        }

        // Handle tables per bucket: 1 for workstation GC, TotalCpuCount for server GC.
        uint32_t tableCount = 1;
        std::string identifiers;
        if (target.TryGetGlobalString(GlobalGCIdentifiers, identifiers) &&
            identifiers.find("server") != std::string::npos)
        {
            uint32_t cpuCount = 0;
            if (target.TryReadGlobalUInt32(GlobalTotalCpuCount, cpuCount) && cpuCount > 0)
            {
                tableCount = cpuCount;
            }
        }

        const uint64_t ptrSize = target.PointerSize();
        std::set<uint64_t> visited;
        int count = 0;

        uint64_t currentMap = mapAddr;
        for (int m = 0; currentMap != 0 && m < MaxMaps; m++)
        {
            data::HandleTableMap map;
            if (!target.TryRead(currentMap, map))
            {
                break;
            }

            // Emit the bucket pointer array the cDAC re-reads (HandleTableMap.OnInit walks
            // [BucketsPtr, BucketsPtr + bucketCount*ptrSize)). EnumMem only captures the map
            // struct itself; this array is separate memory.
            target.EmitMemory(map.BucketsPtr, bucketCount * (uint32_t)ptrSize);

            for (uint32_t b = 0; b < bucketCount; b++)
            {
                uint64_t bucketPtr = 0;
                if (!target.TryReadPointer(map.BucketsPtr + b * ptrSize, bucketPtr) || bucketPtr == 0)
                {
                    continue;
                }

                data::HandleTableBucket bucket;
                if (!target.TryRead(bucketPtr, bucket))
                {
                    continue;
                }

                // Emit the per-bucket handle-table pointer array ([Table, Table + tableCount*ptrSize)).
                target.EmitMemory(bucket.Table, tableCount * (uint32_t)ptrSize);

                for (uint32_t t = 0; t < tableCount; t++)
                {
                    uint64_t handleTablePtr = 0;
                    if (!target.TryReadPointer(bucket.Table + t * ptrSize, handleTablePtr) || handleTablePtr == 0)
                    {
                        continue;
                    }

                    data::HandleTable handleTable;
                    if (!target.TryRead(handleTablePtr, handleTable))
                    {
                        continue;
                    }

                    uint64_t segment = handleTable.SegmentList;
                    for (int s = 0; segment != 0 && s < MaxSegments; s++)
                    {
                        if (!visited.insert(segment).second)
                        {
                            break; // cycle
                        }

                        data::TableSegment tableSegment;
                        if (!target.TryRead(segment, tableSegment))
                        {
                            break;
                        }

                        sink(sinkContext, "handle-segment", segment, segmentSize);
                        count++;

                        segment = tableSegment.NextSegment;
                    }
                }
            }

            currentMap = map.Next;
        }

        return count;
    }
}
} // namespace contracts
