// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// syncblock.cpp
//
// Implementation of the sync-block enumeration declared in syncblock.h.
//*****************************************************************************

#include "syncblock.h"
#include "runtimetypes.h"

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Sync-block globals (src/coreclr/vm/datadescriptor/datadescriptor.inc).
        const char* const GlobalSyncTableEntries = "SyncTableEntries"; // &g_pSyncTable (ptr-ptr)
        const char* const GlobalSyncBlockCache = "SyncBlockCache";     // &s_pSyncBlockCache (ptr-ptr)

        const uint32_t MaxSyncBlocks = 4u * 1024 * 1024;
    }

    int EnumerateSyncBlockRegions(const Target& target)
    {
        // The sync-block cache holds the number of used entries (FreeSyncTableIndex).
        uint64_t cacheAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalSyncBlockCache, cacheAddr) || cacheAddr == 0)
        {
            return -1;
        }
        data::SyncBlockCache cache;
        if (!target.TryRead(cacheAddr, cache))
        {
            return -1;
        }

        uint32_t freeIndex = (uint32_t)cache.FreeSyncTableIndex;
        if (freeIndex == 0)
        {
            return 0;
        }
        uint32_t count = freeIndex - 1;
        if (count > MaxSyncBlocks)
        {
            count = MaxSyncBlocks;
        }

        // The sync table itself: an array of SyncTableEntry indexed by sync-block index.
        uint64_t tableAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalSyncTableEntries, tableAddr) || tableAddr == 0)
        {
            return -1;
        }
        uint32_t entrySize = 0;
        if (!target.TryGetTypeSize(data::SyncTableEntry().TypeName(), entrySize) || entrySize == 0)
        {
            return -1;
        }

        // Emit the whole entry array (indices 0..count) that the cDAC indexes into.
        target.EmitMemory(tableAddr, (count + 1) * entrySize);

        int inUse = 0;
        for (uint32_t index = 1; index <= count; index++)
        {
            uint64_t entryAddr = tableAddr + (uint64_t)index * entrySize;
            data::SyncTableEntry entry;
            if (!target.TryRead(entryAddr, entry))
            {
                continue;
            }

            // Low bit of Object set => the entry is free.
            if ((entry.Object & 1) != 0 || entry.SyncBlock == 0)
            {
                continue;
            }

            data::SyncBlock syncBlock;
            if (target.TryRead(entry.SyncBlock, syncBlock))
            {
                inUse++;
            }
        }

        return inUse;
    }
}
} // namespace contracts
