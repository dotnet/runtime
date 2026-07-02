// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// interop.cpp
//
// Implementation of the COM interop enumeration declared in interop.h.
//*****************************************************************************

#include "interop.h"
#include "runtimetypes.h"

#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // COM interop globals (src/coreclr/vm/datadescriptor/datadescriptor.inc).
        const char* const GlobalRCWCleanupList = "RCWCleanupList"; // &g_pRCWCleanupList (ptr-ptr)

        const int MaxBuckets = 1000000;
        const int MaxRCWsPerBucket = 1000000;
    }

    int EnumerateInteropRegions(const Target& target)
    {
        // The cleanup list head: ReadPointer(ReadGlobalPointer(RCWCleanupList)).
        uint64_t listAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalRCWCleanupList, listAddr))
        {
            return -1;
        }
        if (listAddr == 0)
        {
            return 0; // no cleanup list allocated
        }

        data::RCWCleanupList list;
        if (!target.TryRead(listAddr, list))
        {
            return -1;
        }

        std::set<uint64_t> visited;
        int count = 0;

        uint64_t bucketPtr = list.FirstBucket;
        for (int b = 0; bucketPtr != 0 && b < MaxBuckets; b++)
        {
            if (!visited.insert(bucketPtr).second)
            {
                break;
            }
            data::RCW bucket;
            if (!target.TryRead(bucketPtr, bucket))
            {
                break;
            }

            // GetSTAThread reads the bucket's CtxEntry; capture it too.
            if (bucket.CtxEntry != 0)
            {
                data::CtxEntry ctxEntry;
                target.TryRead(bucket.CtxEntry, ctxEntry);
            }

            // Each bucket heads a chain of RCWs linked by NextRCW (starting with the bucket itself).
            uint64_t rcwPtr = bucketPtr;
            for (int r = 0; rcwPtr != 0 && r < MaxRCWsPerBucket; r++)
            {
                data::RCW rcw;
                if (!target.TryRead(rcwPtr, rcw))
                {
                    break;
                }
                count++;
                if (rcwPtr != bucketPtr && !visited.insert(rcwPtr).second)
                {
                    break;
                }
                rcwPtr = rcw.NextRCW;
            }

            bucketPtr = bucket.NextCleanupBucket;
        }

        return count;
    }
}
} // namespace contracts
