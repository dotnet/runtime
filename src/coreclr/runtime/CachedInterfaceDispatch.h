// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ==--==
//
// Shared (non-architecture specific) portions of a mechanism to perform interface dispatch using an alternate
// mechanism to VSD that does not require runtime generation of code.
//
// ============================================================================

#ifndef __CACHEDINTERFACEDISPATCH_H__
#define __CACHEDINTERFACEDISPATCH_H__

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

// Interface dispatch caches contain an array of these entries. An instance of a cache is paired with a stub
// that implicitly knows how many entries are contained. These entries must be aligned to twice the alignment
// of a pointer due to the synchonization mechanism used to update them at runtime.
struct InterfaceDispatchCacheEntry
{
    MethodTable *    m_pInstanceType;    // Potential type of the object instance being dispatched on
    PCODE            m_pTargetCode;      // Method to dispatch to if the actual instance type matches the above
};

// The interface dispatch cache itself. As well as the entries we include the cache size (since logic such as
// cache miss processing needs to determine this value in a synchronized manner, so it can't be contained in
// the owning interface dispatch indirection cell) and a list entry used to link the caches in one of a couple
// of lists related to cache reclamation.

#pragma warning(push)
#pragma warning(disable:4200) // nonstandard extension used: zero-sized array in struct/union
struct InterfaceDispatchCell;
struct InterfaceDispatchCache
{
    InterfaceDispatchCacheHeader m_cacheHeader;
    union
    {
        InterfaceDispatchCache *    m_pNextFree;    // next in free list
#ifdef INTERFACE_DISPATCH_CACHE_HAS_CELL_BACKPOINTER
        // On ARM and x86 the slow path in the stubs needs to reload the cell pointer from the cache due to the lack
        // of available (volatile non-argument) registers.
        InterfaceDispatchCell  *    m_pCell;        // pointer back to interface dispatch cell
#endif
    };
    uint32_t                      m_cEntries;
    InterfaceDispatchCacheEntry m_rgEntries[];
};
#pragma warning(pop)

bool InterfaceDispatch_Initialize();
PCODE InterfaceDispatch_UpdateDispatchCellCache(InterfaceDispatchCell * pCell, PCODE pTargetCode, MethodTable* pInstanceType, DispatchCellInfo *pNewCellInfo);
void InterfaceDispatch_ReclaimUnusedInterfaceDispatchCaches();
void InterfaceDispatch_DiscardCache(InterfaceDispatchCache * pCache);
inline void InterfaceDispatch_DiscardCacheHeader(InterfaceDispatchCacheHeader * pCache)
{
    return InterfaceDispatch_DiscardCache((InterfaceDispatchCache*)pCache);
}

inline PCODE InterfaceDispatch_SearchDispatchCellCache(InterfaceDispatchCell * pCell, MethodTable* pInstanceType)
{
    // This function must be implemented in native code so that we do not take a GC while walking the cache
    InterfaceDispatchCache * pCache = (InterfaceDispatchCache*)pCell->GetCache();
    if (pCache != NULL)
    {
        InterfaceDispatchCacheEntry * pCacheEntry = pCache->m_rgEntries;
        for (uint32_t i = 0; i < pCache->m_cEntries; i++, pCacheEntry++)
            if (pCacheEntry->m_pInstanceType == pInstanceType)
                return pCacheEntry->m_pTargetCode;
    }

    return (PCODE)nullptr;
}

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

#endif // __CACHEDINTERFACEDISPATCH_H__