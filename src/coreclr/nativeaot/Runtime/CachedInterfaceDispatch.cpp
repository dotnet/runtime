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

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "DebugMacrosExt.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "Crst.h"
#include "RedhawkWarnings.h"
#include "TargetPtrs.h"
#include "MethodTable.h"
#include "Range.h"
#include "allocheap.h"
#include "rhbinder.h"
#include "ObjectLayout.h"
#include "gcrhinterface.h"
#include "shash.h"
#include "RWLock.h"
#include "TypeManager.h"
#include "RuntimeInstance.h"
#include "MethodTable.inl"
#include "CommonMacros.inl"

#include "CachedInterfaceDispatch.h"

// We always allocate cache sizes with a power of 2 number of entries. We have a maximum size we support,
// defined below.
#define CID_MAX_CACHE_SIZE_LOG2 6
#define CID_MAX_CACHE_SIZE      (1 << CID_MAX_CACHE_SIZE_LOG2)

//#define FEATURE_CID_STATS 1

#ifdef FEATURE_CID_STATS

// Some counters used for debugging and profiling the algorithms.
extern "C"
{
    uint32_t CID_g_cLoadVirtFunc = 0;
    uint32_t CID_g_cCacheMisses = 0;
    uint32_t CID_g_cCacheSizeOverflows = 0;
    uint32_t CID_g_cCacheOutOfMemory = 0;
    uint32_t CID_g_cCacheReallocates = 0;
    uint32_t CID_g_cCacheAllocates = 0;
    uint32_t CID_g_cCacheDiscards = 0;
    uint32_t CID_g_cInterfaceDispatches = 0;
    uint32_t CID_g_cbMemoryAllocated = 0;
    uint32_t CID_g_rgAllocatesBySize[CID_MAX_CACHE_SIZE_LOG2 + 1] = { 0 };
};

#define CID_COUNTER_INC(_counter_name) CID_g_c##_counter_name++

#else

#define CID_COUNTER_INC(_counter_name)

#endif // FEATURE_CID_STATS

// Helper function for updating two adjacent pointers (which are aligned on a double pointer-sized boundary)
// atomically.
//
// This is used to update interface dispatch cache entries and also the stub/cache pair in
// interface dispatch indirection cells. The cases have slightly different semantics: cache entry updates
// (fFailOnNonNull == true) require that the existing values in the location are both NULL whereas indirection
// cell updates have no such restriction. In both cases we'll try the update once; on failure we'll return the
// new value of the second pointer and on success we'll the old value of the second pointer.
//
// This suits the semantics of both callers. For indirection cell updates the caller needs to know the address
// of the cache that can now be scheduled for release and the cache pointer is the second one in the pair. For
// cache entry updates the caller only needs a success/failure indication: on success the return value will be
// NULL and on failure non-NULL.
static void * UpdatePointerPairAtomically(void * pPairLocation,
                                          void * pFirstPointer,
                                          void * pSecondPointer,
                                          bool fFailOnNonNull)
{
#if defined(HOST_64BIT)
    // The same comments apply to the AMD64 version. The CompareExchange looks a little different since the
    // API was refactored in terms of int64_t to avoid creating a 128-bit integer type.

    int64_t rgComparand[2] = { 0 , 0 };
    if (!fFailOnNonNull)
    {
        rgComparand[0] = *(int64_t volatile *)pPairLocation;
        rgComparand[1] = *((int64_t volatile *)pPairLocation + 1);
    }

    uint8_t bResult = PalInterlockedCompareExchange128((int64_t*)pPairLocation, (int64_t)pSecondPointer, (int64_t)pFirstPointer, rgComparand);
    if (bResult == 1)
    {
        // Success, return old value of second pointer (rgComparand is updated by
        // PalInterlockedCompareExchange128 with the old pointer values in this case).
        return (void*)rgComparand[1];
    }

    // Failure, return the new second pointer value.
    return pSecondPointer;
#else
    // Stuff the two pointers into a 64-bit value as the proposed new value for the CompareExchange64 below.
    int64_t iNewValue = (int64_t)((uint64_t)(uintptr_t)pFirstPointer | ((uint64_t)(uintptr_t)pSecondPointer << 32));

    // Read the old value in the location. If fFailOnNonNull is set we just assume this was zero and we'll
    // fail below if that's not the case.
    int64_t iOldValue = fFailOnNonNull ? 0 : *(int64_t volatile *)pPairLocation;

    int64_t iUpdatedOldValue = PalInterlockedCompareExchange64((int64_t*)pPairLocation, iNewValue, iOldValue);
    if (iUpdatedOldValue == iOldValue)
    {
        // Successful update. Return the previous value of the second pointer. For cache entry updates
        // (fFailOnNonNull == true) this is guaranteed to be NULL in this case and the result being being
        // NULL in the success case is all the caller cares about. For indirection cell updates the second
        // pointer represents the old cache and the caller needs this data so they can schedule the cache
        // for deletion once it becomes safe to do so.
        return (void*)(uint32_t)(iOldValue >> 32);
    }

    // The update failed due to a racing update to the same location. Return the new value of the second
    // pointer (either a new cache that lost the race or a non-NULL pointer in the cache entry update case).
    return pSecondPointer;
#endif // HOST_64BIT
}

// Helper method for updating an interface dispatch cache entry atomically. See comments by the usage of
// this method for the details of why we need this. If a racing update is detected false is returned and the
// update abandoned. This is necessary since it's not safe to update a valid cache entry (one with a non-NULL
// m_pInstanceType field) outside of a GC.
static bool UpdateCacheEntryAtomically(InterfaceDispatchCacheEntry *pEntry,
                                       MethodTable * pInstanceType,
                                       void * pTargetCode)
{
    C_ASSERT(sizeof(InterfaceDispatchCacheEntry) == (sizeof(void*) * 2));
    C_ASSERT(offsetof(InterfaceDispatchCacheEntry, m_pInstanceType) < offsetof(InterfaceDispatchCacheEntry, m_pTargetCode));

    return UpdatePointerPairAtomically(pEntry, pInstanceType, pTargetCode, true) == NULL;
}

// Helper method for updating an interface dispatch indirection cell's stub and cache pointer atomically.
// Returns the value of the cache pointer that is not referenced by the cell after this operation. This can be
// NULL on the initial cell update, the value of the old cache pointer or the value of the new cache pointer
// supplied (in the case where another thread raced with us for the update and won). In any case, if the
// returned pointer is non-NULL it represents a cache that should be scheduled for release.
static InterfaceDispatchCache * UpdateCellStubAndCache(InterfaceDispatchCell * pCell,
                                                       void * pStub,
                                                       uintptr_t newCacheValue)
{
    C_ASSERT(offsetof(InterfaceDispatchCell, m_pStub) == 0);
    C_ASSERT(offsetof(InterfaceDispatchCell, m_pCache) == sizeof(void*));

    uintptr_t oldCacheValue = (uintptr_t)UpdatePointerPairAtomically(pCell, pStub, (void*)newCacheValue, false);

    if (InterfaceDispatchCell::IsCache(oldCacheValue))
    {
        return (InterfaceDispatchCache *)oldCacheValue;
    }
    else
    {
        return nullptr;
    }
}

//
// Cache allocation logic.
//
// We use the existing AllocHeap mechanism as our base allocator for cache blocks. This is because it can
// provide the required 16-byte alignment with no padding or heap header costs. The downside is that there is
// no deallocation support (which would be hard to implement without implementing a cache block compaction
// scheme, which is certainly possible but not necessarily needed at this point).
//
// Instead, much like the original VSD algorithm, we keep discarded cache blocks and use them to satisfy new
// allocation requests before falling back on AllocHeap.
//
// We can't re-use discarded cache blocks immediately since there may be code that is still using them.
// Instead we link them into a global list and then at the next GC (when no code can hold a reference to these
// any more) we can place them on one of several free lists based on their size.
//

#if defined(HOST_AMD64) || defined(HOST_ARM64)

// Head of the list of discarded cache blocks that can't be re-used just yet.
InterfaceDispatchCache * g_pDiscardedCacheList; // for AMD64 and ARM64, m_pCell is not used and we can link the discarded blocks themselves

#else // defined(HOST_AMD64) || defined(HOST_ARM64)

struct DiscardedCacheBlock
{
    DiscardedCacheBlock *       m_pNext;        // for x86 and ARM, we are short of registers, thus need the m_pCell back pointers
    InterfaceDispatchCache *    m_pCache;       // and thus need this auxiliary list
};

// Head of the list of discarded cache blocks that can't be re-used just yet.
static DiscardedCacheBlock * g_pDiscardedCacheList = NULL;

// Free list of DiscardedCacheBlock items
static DiscardedCacheBlock * g_pDiscardedCacheFree = NULL;

#endif // defined(HOST_AMD64) || defined(HOST_ARM64)

// Free lists for each cache size up to the maximum. We allocate from these in preference to new memory.
static InterfaceDispatchCache * g_rgFreeLists[CID_MAX_CACHE_SIZE_LOG2 + 1];

// Lock protecting both g_pDiscardedCacheList and g_rgFreeLists. We don't use the OS SLIST support here since
// it imposes too much space overhead on list entries on 64-bit (each is actually 16 bytes).
static CrstStatic g_sListLock;

// The base memory allocator.
static AllocHeap * g_pAllocHeap = NULL;

// Each cache size has an associated stub used to perform lookup over that cache.
extern "C" void RhpInterfaceDispatch1();
extern "C" void RhpInterfaceDispatch2();
extern "C" void RhpInterfaceDispatch4();
extern "C" void RhpInterfaceDispatch8();
extern "C" void RhpInterfaceDispatch16();
extern "C" void RhpInterfaceDispatch32();
extern "C" void RhpInterfaceDispatch64();

extern "C" void RhpVTableOffsetDispatch();

typedef void (*InterfaceDispatchStub)();

static void * g_rgDispatchStubs[CID_MAX_CACHE_SIZE_LOG2 + 1] = {
    (void *)&RhpInterfaceDispatch1,
    (void *)&RhpInterfaceDispatch2,
    (void *)&RhpInterfaceDispatch4,
    (void *)&RhpInterfaceDispatch8,
    (void *)&RhpInterfaceDispatch16,
    (void *)&RhpInterfaceDispatch32,
    (void *)&RhpInterfaceDispatch64,
};

// Map a cache size into a linear index.
static uint32_t CacheSizeToIndex(uint32_t cCacheEntries)
{
    switch (cCacheEntries)
    {
    case 1:
        return 0;
    case 2:
        return 1;
    case 4:
        return 2;
    case 8:
        return 3;
    case 16:
        return 4;
    case 32:
        return 5;
    case 64:
        return 6;
    default:
        UNREACHABLE();
    }
}

// Allocates and initializes new cache of the given size. If given a previous version of the cache (guaranteed
// to be smaller) it will also pre-populate the new cache with the contents of the old. Additionally the
// address of the interface dispatch stub associated with this size of cache is returned.
static uintptr_t AllocateCache(uint32_t cCacheEntries, InterfaceDispatchCache * pExistingCache, const DispatchCellInfo *pNewCellInfo, void ** ppStub)
{
    if (pNewCellInfo->CellType == DispatchCellType::VTableOffset)
    {
        ASSERT(pNewCellInfo->VTableOffset < InterfaceDispatchCell::IDC_MaxVTableOffsetPlusOne);
        *ppStub = (void *)&RhpVTableOffsetDispatch;
        ASSERT(!InterfaceDispatchCell::IsCache(pNewCellInfo->VTableOffset));
        return pNewCellInfo->VTableOffset;
    }

    ASSERT((cCacheEntries >= 1) && (cCacheEntries <= CID_MAX_CACHE_SIZE));
    ASSERT((pExistingCache == NULL) || (pExistingCache->m_cEntries < cCacheEntries));

    InterfaceDispatchCache * pCache = NULL;

    // Transform cache size back into a linear index.
    uint32_t idxCacheSize = CacheSizeToIndex(cCacheEntries);

    // Attempt to allocate the head of the free list of the correct cache size.
    if (g_rgFreeLists[idxCacheSize] != NULL)
    {
        CrstHolder lh(&g_sListLock);

        pCache = g_rgFreeLists[idxCacheSize];
        if (pCache != NULL)
        {
            g_rgFreeLists[idxCacheSize] = pCache->m_pNextFree;
            CID_COUNTER_INC(CacheReallocates);
        }
    }

    if (pCache == NULL)
    {
        // No luck with the free list, allocate the cache from via the AllocHeap.
        pCache = (InterfaceDispatchCache*)g_pAllocHeap->AllocAligned(sizeof(InterfaceDispatchCache) +
                                                                     (sizeof(InterfaceDispatchCacheEntry) * cCacheEntries),
                                                                     sizeof(void*) * 2);
        if (pCache == NULL)
            return (uintptr_t)NULL;

        CID_COUNTER_INC(CacheAllocates);
#ifdef FEATURE_CID_STATS
        CID_g_cbMemoryAllocated += sizeof(InterfaceDispatchCacheEntry) * cCacheEntries;
        CID_g_rgAllocatesBySize[idxCacheSize]++;
#endif
    }

    // We have a cache block, now initialize it.
    pCache->m_pNextFree = NULL;
    pCache->m_cEntries = cCacheEntries;
    pCache->m_cacheHeader.Initialize(pNewCellInfo);

    // Copy over entries from previous version of the cache (if any) and zero the rest.
    if (pExistingCache)
    {
        memcpy(pCache->m_rgEntries,
               pExistingCache->m_rgEntries,
               sizeof(InterfaceDispatchCacheEntry) * pExistingCache->m_cEntries);
        memset(&pCache->m_rgEntries[pExistingCache->m_cEntries],
               0,
               (cCacheEntries - pExistingCache->m_cEntries) * sizeof(InterfaceDispatchCacheEntry));
    }
    else
    {
        memset(pCache->m_rgEntries,
               0,
               cCacheEntries * sizeof(InterfaceDispatchCacheEntry));
    }

    // Pass back the stub the corresponds to this cache size.
    *ppStub = g_rgDispatchStubs[idxCacheSize];

    return (uintptr_t)pCache;
}

// Discards a cache by adding it to a list of caches that may still be in use but will be made available for
// re-allocation at the next GC.
static void DiscardCache(InterfaceDispatchCache * pCache)
{
    CID_COUNTER_INC(CacheDiscards);

    CrstHolder lh(&g_sListLock);

#if defined(HOST_AMD64) || defined(HOST_ARM64)

    // on AMD64 and ARM64, we can thread the list through the blocks directly
    pCache->m_pNextFree = g_pDiscardedCacheList;
    g_pDiscardedCacheList = pCache;

#else // defined(HOST_AMD64) || defined(HOST_ARM64)

    // on other architectures, we cannot overwrite pCache->m_pNextFree yet
    // because it shares storage with m_pCell which may still be used as a back
    // pointer to the dispatch cell.

    // instead, allocate an auxiliary node (with its own auxiliary free list)
    DiscardedCacheBlock * pDiscardedCacheBlock = g_pDiscardedCacheFree;
    if (pDiscardedCacheBlock != NULL)
        g_pDiscardedCacheFree = pDiscardedCacheBlock->m_pNext;
    else
        pDiscardedCacheBlock = (DiscardedCacheBlock *)g_pAllocHeap->Alloc(sizeof(DiscardedCacheBlock));

    if (pDiscardedCacheBlock != NULL) // if we did NOT get the memory, we leak the discarded block
    {
        pDiscardedCacheBlock->m_pNext = g_pDiscardedCacheList;
        pDiscardedCacheBlock->m_pCache = pCache;

        g_pDiscardedCacheList = pDiscardedCacheBlock;
    }
#endif // defined(HOST_AMD64) || defined(HOST_ARM64)
}

// Called during a GC to empty the list of discarded caches (which we can now guarantee aren't being accessed)
// and sort the results into the free lists we maintain for each cache size.
void ReclaimUnusedInterfaceDispatchCaches()
{
    // No need for any locks, we're not racing with any other threads any more.

    // Walk the list of discarded caches.
#if defined(HOST_AMD64) || defined(HOST_ARM64)

    // on AMD64, this is threaded directly through the cache blocks
    InterfaceDispatchCache * pCache = g_pDiscardedCacheList;
    while (pCache)
    {
        InterfaceDispatchCache * pNextCache = pCache->m_pNextFree;

        // Transform cache size back into a linear index.
        uint32_t idxCacheSize = CacheSizeToIndex(pCache->m_cEntries);

        // Insert the cache onto the head of the correct free list.
        pCache->m_pNextFree = g_rgFreeLists[idxCacheSize];
        g_rgFreeLists[idxCacheSize] = pCache;

        pCache = pNextCache;
    }

#else // defined(HOST_AMD64) || defined(HOST_ARM64)

    // on other architectures, we use an auxiliary list instead
    DiscardedCacheBlock * pDiscardedCacheBlock = g_pDiscardedCacheList;
    while (pDiscardedCacheBlock)
    {
        InterfaceDispatchCache * pCache = pDiscardedCacheBlock->m_pCache;

        // Transform cache size back into a linear index.
        uint32_t idxCacheSize = CacheSizeToIndex(pCache->m_cEntries);

        // Insert the cache onto the head of the correct free list.
        pCache->m_pNextFree = g_rgFreeLists[idxCacheSize];
        g_rgFreeLists[idxCacheSize] = pCache;

        // Insert the container to its own free list
        DiscardedCacheBlock * pNextDiscardedCacheBlock = pDiscardedCacheBlock->m_pNext;
        pDiscardedCacheBlock->m_pNext = g_pDiscardedCacheFree;
        g_pDiscardedCacheFree = pDiscardedCacheBlock;
        pDiscardedCacheBlock = pNextDiscardedCacheBlock;
    }

#endif // defined(HOST_AMD64) || defined(HOST_ARM64)

    // We processed all the discarded entries, so we can simply NULL the list head.
    g_pDiscardedCacheList = NULL;
}

// One time initialization of interface dispatch.
bool InitializeInterfaceDispatch()
{
    g_pAllocHeap = new (nothrow) AllocHeap();
    if (g_pAllocHeap == NULL)
        return false;

    if (!g_pAllocHeap->Init())
        return false;

    g_sListLock.Init(CrstInterfaceDispatchGlobalLists, CRST_DEFAULT);

    return true;
}

COOP_PINVOKE_HELPER(PTR_Code, RhpUpdateDispatchCellCache, (InterfaceDispatchCell * pCell, PTR_Code pTargetCode, MethodTable* pInstanceType, DispatchCellInfo *pNewCellInfo))
{
    // Attempt to update the cache with this new mapping (if we have any cache at all, the initial state
    // is none).
    InterfaceDispatchCache * pCache = (InterfaceDispatchCache*)pCell->GetCache();
    uint32_t cOldCacheEntries = 0;
    if (pCache != NULL)
    {
        InterfaceDispatchCacheEntry * pCacheEntry = pCache->m_rgEntries;
        for (uint32_t i = 0; i < pCache->m_cEntries; i++, pCacheEntry++)
        {
            if (pCacheEntry->m_pInstanceType == NULL)
            {
                if (UpdateCacheEntryAtomically(pCacheEntry, pInstanceType, pTargetCode))
                    return (PTR_Code)pTargetCode;
            }
        }

        cOldCacheEntries = pCache->m_cEntries;
    }

    // Failed to update an existing cache, we need to allocate a new cache. The old one, if any, might
    // still be in use so we can't simply reclaim it. Instead we keep it around until the next GC at which
    // point we know no code is holding a reference to it. Particular cache sizes are associated with a
    // (globally shared) stub which implicitly knows the size of the cache.

    if (cOldCacheEntries == CID_MAX_CACHE_SIZE)
    {
        // We already reached the maximum cache size we wish to allocate. For now don't attempt to cache
        // the mapping we just did: there's no safe way to update the existing cache right now if it
        // doesn't have an empty entries. There are schemes that would let us do this at the next GC point
        // but it's not clear whether we should do this or re-tune the cache max size, we need to measure
        // this.
        CID_COUNTER_INC(CacheSizeOverflows);
        return (PTR_Code)pTargetCode;
    }

    uint32_t cNewCacheEntries = cOldCacheEntries ? cOldCacheEntries * 2 : 1;
    void *pStub;
    uintptr_t newCacheValue = AllocateCache(cNewCacheEntries, pCache, pNewCellInfo, &pStub);
    if (newCacheValue == 0)
    {
        CID_COUNTER_INC(CacheOutOfMemory);
        return (PTR_Code)pTargetCode;
    }

    if (InterfaceDispatchCell::IsCache(newCacheValue))
    {
        pCache = (InterfaceDispatchCache*)newCacheValue;
#if !defined(HOST_AMD64) && !defined(HOST_ARM64)
        // Set back pointer to interface dispatch cell for non-AMD64 and non-ARM64
        // for AMD64 and ARM64, we have enough registers to make this trick unnecessary
        pCache->m_pCell = pCell;
#endif // !defined(HOST_AMD64) && !defined(HOST_ARM64)

        // Add entry to the first unused slot.
        InterfaceDispatchCacheEntry * pCacheEntry = &pCache->m_rgEntries[cOldCacheEntries];
        pCacheEntry->m_pInstanceType = pInstanceType;
        pCacheEntry->m_pTargetCode = pTargetCode;
    }

    // Publish the new cache by atomically updating both the cache and stub pointers in the indirection
    // cell. This returns us a cache to discard which may be NULL (no previous cache), the previous cache
    // value or the cache we just allocated (another thread performed an update first).
    InterfaceDispatchCache * pDiscardedCache = UpdateCellStubAndCache(pCell, pStub, newCacheValue);
    if (pDiscardedCache)
        DiscardCache(pDiscardedCache);

    return (PTR_Code)pTargetCode;
}

COOP_PINVOKE_HELPER(PTR_Code, RhpSearchDispatchCellCache, (InterfaceDispatchCell * pCell, MethodTable* pInstanceType))
{
    // This function must be implemented in native code so that we do not take a GC while walking the cache
    InterfaceDispatchCache * pCache = (InterfaceDispatchCache*)pCell->GetCache();
    if (pCache != NULL)
    {
        InterfaceDispatchCacheEntry * pCacheEntry = pCache->m_rgEntries;
        for (uint32_t i = 0; i < pCache->m_cEntries; i++, pCacheEntry++)
            if (pCacheEntry->m_pInstanceType == pInstanceType)
                return (PTR_Code)pCacheEntry->m_pTargetCode;
    }

    return nullptr;
}

// Given a dispatch cell, get the type and slot associated with it. This function MUST be implemented
// in cooperative native code, as the m_pCache field on the cell is unsafe to access from managed
// code due to its use of the GC state as a lock, and as lifetime control
COOP_PINVOKE_HELPER(void, RhpGetDispatchCellInfo, (InterfaceDispatchCell * pCell, DispatchCellInfo* pDispatchCellInfo))
{
    *pDispatchCellInfo = pCell->GetDispatchCellInfo();
}

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
