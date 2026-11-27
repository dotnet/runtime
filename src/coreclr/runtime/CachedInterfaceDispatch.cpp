// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ==--==
//
// Shared (non-architecture specific) portions of a mechanism to perform interface dispatch using an alternate
// mechanism to VSD that does not require runtime generation of code.
//
// ============================================================================
#include "common.h"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
#include "CachedInterfaceDispatchPal.h"
#include "CachedInterfaceDispatch.h"

// This is a port of the insertion side of GenericCache<TKey, TValue> (see GenericCache.cs). The lookup side is
// implemented by the RhpInterfaceDispatch helpers, so the hashing and the probing sequence must not diverge.

#ifdef _DEBUG
// use smaller numbers to hit resizing/preempting logic in debug
static const uint32_t DISPATCH_CACHE_INITIAL_SIZE = 8;
static const uint32_t DISPATCH_CACHE_MAX_SIZE = 512;
#else
static const uint32_t DISPATCH_CACHE_INITIAL_SIZE = 128;
static const uint32_t DISPATCH_CACHE_MAX_SIZE = 128 * 1024;
#endif

static const int      VERSION_NUM_SIZE = 29;
static const uint32_t VERSION_NUM_MASK = (1 << VERSION_NUM_SIZE) - 1;
static const int      BUCKET_SIZE = 8;
static const uint32_t SENTINEL_TABLE_SIZE = 2;

static DispatchCacheTable * volatile g_pDispatchCacheTable;
void * g_pDispatchCache = (void*)&g_pDispatchCacheTable;

// A trivial 2-element table that never contains anything. It is installed when the cache is flushed.
static DispatchCacheTable * g_pDispatchCacheSentinelTable;
static uint32_t g_dispatchCacheLastFlushSize = DISPATCH_CACHE_INITIAL_SIZE;

// Tables that were replaced and may still be read by the dispatch helpers. They are freed at the next GC,
// linked through m_unused.
static DispatchCacheTable * volatile g_pDiscardedDispatchCacheTables;

static uint8_t & HashShift(DispatchCacheTable * pTable)
{
    return reinterpret_cast<uint8_t*>(&pTable->m_entries[0].m_version)[0];
}

static uint8_t & VictimCounter(DispatchCacheTable * pTable)
{
    return reinterpret_cast<uint8_t*>(&pTable->m_entries[0].m_version)[1];
}

static uint32_t TableMask(DispatchCacheTable * pTable)
{
    // element 0 is used for embedded aux data
    return pTable->m_length - 2;
}

static DispatchCacheEntry * Element(DispatchCacheTable * pTable, uint32_t index)
{
    // element 0 is used for embedded aux data, skip it
    return &pTable->m_entries[index + 1];
}

static uint32_t CacheElementCount(DispatchCacheTable * pTable)
{
    return pTable->m_length - 1;
}

static bool IsSentinel(DispatchCacheTable * pTable)
{
    return pTable->m_length == SENTINEL_TABLE_SIZE + 1;
}

static int32_t GetDispatchCacheHashCode(TADDR pCell, TADDR pInstanceType)
{
    // pointers will likely match and cancel out in the upper bits
    // we will rotate context by 16 bit to keep more varying bits in the hash
    const int bits = sizeof(TADDR) * 8;
    TADDR value = ((pCell << 16) | (pCell >> (bits - 16))) ^ pInstanceType;
#ifdef TARGET_64BIT
    return (int32_t)(uint32_t)value ^ (int32_t)(uint32_t)(value >> 32);
#else
    return (int32_t)value;
#endif
}

static uint32_t HashToBucket(DispatchCacheTable * pTable, int32_t hash)
{
    uint8_t hashShift = HashShift(pTable);
#ifdef TARGET_64BIT
    return (uint32_t)(((uint64_t)(int64_t)hash * 11400714819323198485ull) >> hashShift);
#else
    return (uint32_t)(((uint32_t)hash * 2654435769u) >> hashShift);
#endif
}

static DispatchCacheTable * CreateDispatchCacheTable(uint32_t size)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // size must be a power of two
    _ASSERTE((size > 1) && ((size & (size - 1)) == 0));

    size_t cbTable = offsetof(DispatchCacheTable, m_entries) + (size + 1) * sizeof(DispatchCacheEntry);
    DispatchCacheTable * pTable = (DispatchCacheTable*)new (nothrow) BYTE[cbTable];
    if (pTable == NULL)
        return NULL;

    memset(pTable, 0, cbTable);
    pTable->m_length = size + 1;

    // Fibonacci hash reduces the value into desired range by shifting right by the number of leading zeroes in 'size-1'
    uint8_t log2Size = 0;
    while ((1u << log2Size) < size)
        log2Size++;
    HashShift(pTable) = (uint8_t)(sizeof(TADDR) * 8 - log2Size);

    return pTable;
}

static void DiscardDispatchCacheTable(DispatchCacheTable * pTable)
{
    LIMITED_METHOD_CONTRACT;

    if (IsSentinel(pTable))
        return;

    DispatchCacheTable * pHead;
    do
    {
        pHead = g_pDiscardedDispatchCacheTables;
        pTable->m_unused = (TADDR)pHead;
    }
    while (InterlockedCompareExchangeT(&g_pDiscardedDispatchCacheTables, pTable, pHead) != pHead);
}

static void ReplaceDispatchCacheTable(DispatchCacheTable * pOldTable, DispatchCacheTable * pNewTable)
{
    LIMITED_METHOD_CONTRACT;

    if (InterlockedCompareExchangeT(&g_pDispatchCacheTable, pNewTable, pOldTable) == pOldTable)
    {
        DiscardDispatchCacheTable(pOldTable);
    }
    else if (pNewTable != g_pDispatchCacheSentinelTable)
    {
        // Another thread replaced the table first. The new table was never published.
        delete[] (BYTE*)pNewTable;
    }
}

static bool MaybeReplaceCacheWithLarger(DispatchCacheTable * pOldTable, uint32_t size)
{
    LIMITED_METHOD_CONTRACT;

    DispatchCacheTable * pNewTable = CreateDispatchCacheTable(size);
    if (pNewTable == NULL)
        return false;

    ReplaceDispatchCacheTable(pOldTable, pNewTable);
    return true;
}

static void FlushCurrentCache(DispatchCacheTable * pTable)
{
    LIMITED_METHOD_CONTRACT;

    uint32_t lastSize = CacheElementCount(pTable);
    if (lastSize < DISPATCH_CACHE_INITIAL_SIZE)
        lastSize = DISPATCH_CACHE_INITIAL_SIZE;

    // store the last size to use when creating a new table
    // it is just a hint, not needed for correctness, so no synchronization
    // with the writing of the table
    g_dispatchCacheLastFlushSize = lastSize;
    // flushing is just replacing the table with a sentinel.
    ReplaceDispatchCacheTable(pTable, g_pDispatchCacheSentinelTable);
}

static bool TryGrow(DispatchCacheTable * pTable)
{
    LIMITED_METHOD_CONTRACT;

    uint32_t newSize = CacheElementCount(pTable) * 2;
    if (newSize <= DISPATCH_CACHE_MAX_SIZE)
    {
        return MaybeReplaceCacheWithLarger(pTable, newSize);
    }

    return false;
}

static bool TryClaimEntry(DispatchCacheTable * pTable, DispatchCacheEntry * pEntry, uint32_t distance, TADDR pCell, TADDR pInstanceType, PCODE pTargetCode, bool fOnlyIfCloser)
{
    LIMITED_METHOD_CONTRACT;

    uint32_t version = VolatileLoadWithoutBarrier(&pEntry->m_version);

    // mask the lower version bit to make it even.
    // This way we will detect both if version is changing (odd) or has changed (even, but different).
    version &= ~1u;

    if ((version & VERSION_NUM_MASK) >= (VERSION_NUM_MASK - 2))
    {
        // If exactly VERSION_NUM_MASK updates happens between here and publishing, we may not recognize a race.
        // It is extremely unlikely, but to not worry about the possibility, lets not allow version to go this high and just get a new cache.
        // This will not happen often.
        FlushCurrentCache(pTable);
        return true;
    }

    if (fOnlyIfCloser && !(version == 0 || (version >> VERSION_NUM_SIZE) > distance))
        return false;

    uint32_t newVersion = (distance << VERSION_NUM_SIZE) + (version & VERSION_NUM_MASK) + 1;
    uint32_t versionOrig = (uint32_t)InterlockedCompareExchange((LONG*)&pEntry->m_version, (LONG)newVersion, (LONG)version);
    if (versionOrig != version)
    {
        // someone snatched the entry.
        return false;
    }

    pEntry->m_pCell = pCell;
    pEntry->m_pInstanceType = pInstanceType;
    pEntry->m_pTargetCode = pTargetCode;

    // entry is in inconsistent state and cannot be read or written to until we
    // update the version, which is the last thing we do here
    VolatileStore(&pEntry->m_version, newVersion + 1);
    return true;
}

static void DispatchCacheTrySet(TADDR pCell, TADDR pInstanceType, PCODE pTargetCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    int32_t hash = GetDispatchCacheHashCode(pCell, pInstanceType);
    uint32_t bucket;
    DispatchCacheTable * pTable;

    do
    {
        pTable = g_pDispatchCacheTable;
        if (IsSentinel(pTable))
        {
            // sentinel table is used to indicate that
            // we did not allocate a real table yet or have flushed it.
            // try replacing the table, but do not insert anything.
            MaybeReplaceCacheWithLarger(pTable, g_dispatchCacheLastFlushSize);
            return;
        }

        bucket = HashToBucket(pTable, hash);
        uint32_t index = bucket;

        for (uint32_t i = 0; i < BUCKET_SIZE;)
        {
            // claim the entry if unused or is more distant than us from its origin.
            DispatchCacheEntry * pEntry = Element(pTable, index & TableMask(pTable));
            if (TryClaimEntry(pTable, pEntry, i, pCell, pInstanceType, pTargetCode, true /* fOnlyIfCloser */))
                return;

            if (pEntry->m_pCell == pCell && pEntry->m_pInstanceType == pInstanceType)
            {
                // looks like we already have an entry for this.
                // duplicate entries are harmless, but a bit of a waste.
                return;
            }

            // quadratic reprobe
            i++;
            index += i;
        }

        // bucket is full.
    } while (TryGrow(pTable));

    // reread the table after TryGrow.
    pTable = g_pDispatchCacheTable;

    if (IsSentinel(pTable))
    {
        // do not insert into a sentinel.
        return;
    }

    // pick a victim somewhat randomly within a bucket
    // NB: ++ is not interlocked. We are ok if we lose counts here. It is just a number that changes.
    uint32_t victimDistance = (uint32_t)(VictimCounter(pTable)++ & (BUCKET_SIZE - 1));
    // position the victim in a quadratic reprobe bucket
    uint32_t victim = (victimDistance * victimDistance + victimDistance) / 2;

    TryClaimEntry(pTable, Element(pTable, (bucket + victim) & TableMask(pTable)), victimDistance, pCell, pInstanceType, pTargetCode, false /* fOnlyIfCloser */);
}

bool InterfaceDispatch_Initialize()
{
    STANDARD_VM_CONTRACT;

    g_pDispatchCacheSentinelTable = CreateDispatchCacheTable(SENTINEL_TABLE_SIZE);
    if (g_pDispatchCacheSentinelTable == NULL)
        return false;

    DispatchCacheTable * pTable = NULL;
#ifndef _DEBUG
    // Initialize to the sentinel in DEBUG as if just flushed, to ensure the sentinel can be handled in TrySet.
    pTable = CreateDispatchCacheTable(DISPATCH_CACHE_INITIAL_SIZE);
#endif
    g_pDispatchCacheTable = (pTable != NULL) ? pTable : g_pDispatchCacheSentinelTable;

    return true;
}

void InterfaceDispatch_UpdateDispatchCellCache(InterfaceDispatchCell * pCell, PCODE pTargetCode, MethodTable * pInstanceType, bool fCacheInCell, bool fCacheInTable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // If the dispatch cell doesn't cache anything yet, cache in the dispatch cell
    if (fCacheInCell && InterlockedCompareExchangeT(&pCell->m_pCode, pTargetCode, (PCODE)NULL) == (PCODE)NULL)
    {
        // Use release semantics so the reader's acquire-load of m_pMethodTable
        // guarantees the m_pCode store is visible.
        pCell->m_pMethodTable = (TADDR)pInstanceType;
    }
    else if (fCacheInTable)
    {
        // Otherwise cache in the hashtable
        DispatchCacheTrySet((TADDR)pCell, (TADDR)pInstanceType, pTargetCode);
    }
}

void InterfaceDispatch_FlushDispatchCache()
{
    LIMITED_METHOD_CONTRACT;

    FlushCurrentCache(g_pDispatchCacheTable);
}

void InterfaceDispatch_ReclaimUnusedInterfaceDispatchCaches()
{
    LIMITED_METHOD_CONTRACT;

    // The dispatch helpers don't contain GC safe points, so no thread is reading a table that was replaced
    // before this GC started.
    DispatchCacheTable * pTable = InterlockedExchangeT(&g_pDiscardedDispatchCacheTables, (DispatchCacheTable*)NULL);
    while (pTable != NULL)
    {
        DispatchCacheTable * pNext = (DispatchCacheTable*)pTable->m_unused;
        delete[] (BYTE*)pTable;
        pTable = pNext;
    }
}

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
