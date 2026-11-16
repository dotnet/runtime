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

struct InterfaceDispatchCell;

// The global dispatch cache is a hashtable keyed by (dispatch cell, MethodTable) that holds the dispatch
// targets that don't fit in the monomorphic cache of the dispatch cell. Its layout matches
// GenericCache<Key, nint> in CoreLib, which is what the NativeAOT dispatch helpers probe.
struct DispatchCacheEntry
{
    // Seqlock version. In entry 0 of a table, the low two bytes hold the hash shift and the victim counter.
    uint32_t    m_version;
    TADDR       m_pCell;
    TADDR       m_pInstanceType;
    PCODE       m_pTargetCode;
};

#pragma warning(push)
#pragma warning(disable:4200) // nonstandard extension used: zero-sized array in struct/union
struct DispatchCacheTable
{
    // Laid out like a managed Entry[]: the dispatch helpers read the length at the same offset and
    // index the entries from the same base.
    TADDR       m_unused;
    uint32_t    m_length;
#ifdef TARGET_64BIT
    uint32_t    m_padding;
#endif
    DispatchCacheEntry m_entries[];
};
#pragma warning(pop)

// Points at the variable that holds the current DispatchCacheTable.
extern "C" void * g_pDispatchCache;

bool InterfaceDispatch_Initialize();

// Caches pTargetCode as the target of dispatching through pCell on an object of type pInstanceType.
// The monomorphic cache in the cell is used if it is still empty and fCacheInCell is set, the global
// dispatch cache otherwise, if fCacheInTable is set.
void InterfaceDispatch_UpdateDispatchCellCache(InterfaceDispatchCell * pCell, PCODE pTargetCode, MethodTable * pInstanceType, bool fCacheInCell, bool fCacheInTable);

// Drops all entries of the global dispatch cache. Used when dispatch cells are reused for another call site.
void InterfaceDispatch_FlushDispatchCache();

// Called during a GC to free the dispatch cache tables that were replaced since the previous GC.
void InterfaceDispatch_ReclaimUnusedInterfaceDispatchCaches();

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

#endif // __CACHEDINTERFACEDISPATCH_H__
