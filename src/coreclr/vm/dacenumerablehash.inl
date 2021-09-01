// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Abstract base class implementation of a hash table suitable for each DAC memory enumeration.
// See DacEnumerableHash.h for a more detailed description.
//

#include "clr_std/type_traits"

// Our implementation embeds entry data supplied by the hash sub-class into a larger entry structure
// containing DacEnumerableHash metadata. We often end up returning pointers to the inner entry to sub-class code and
// doing this in a DAC-friendly fashion involves some DAC gymnastics. The following couple of macros factor
// those complexities out.
#define VALUE_FROM_VOLATILE_ENTRY(_ptr) dac_cast<DPTR(VALUE)>(PTR_TO_MEMBER_TADDR(VolatileEntry, (_ptr), m_sValue))

#ifndef DACCESS_COMPILE

// Base constructor. Call this from your derived constructor to provide the owning module, loader heap and
// initial number of buckets (which must be non-zero). Module is only
// used to locate a loader heap for allocating bucket lists and entries unless an alternative heap is
// provided. If no Module pointer is supplied you must provide a direct heap pointer.
template <DAC_ENUM_HASH_PARAMS>
DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::DacEnumerableHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // An invariant in the code is that we always have a non-zero number of buckets.
    _ASSERTE(cInitialBuckets > 0);

    // At least one of module or heap must have been specified or we won't know how to allocate entries and
    // buckets.
    _ASSERTE(pModule || pHeap);
    m_pModule = pModule;
    m_pHeap = pHeap;

    S_SIZE_T cbBuckets = S_SIZE_T(sizeof(VolatileEntry*)) * S_SIZE_T(cInitialBuckets);

    m_cEntries = 0;
    m_cBuckets = cInitialBuckets;
    m_pBuckets = (PTR_VolatileEntry*)(void*)GetHeap()->AllocMem(cbBuckets);

    // Note: Memory allocated on loader heap is zero filled
}

// Allocate an uninitialized entry for the hash table (it's not inserted). The AllocMemTracker is optional and
// may be specified as NULL for untracked allocations. This is split from the hash insertion logic so that
// callers can pre-allocate entries and then perform insertions which cannot fault.
template <DAC_ENUM_HASH_PARAMS>
VALUE *DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseAllocateEntry(AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    TaggedMemAllocPtr pMemory = GetHeap()->AllocMem(S_SIZE_T(sizeof(VolatileEntry)));

    VolatileEntry *pEntry;
    if (pamTracker)
        pEntry = (VolatileEntry*)pamTracker->Track(pMemory);
    else
        pEntry = pMemory.cast<VolatileEntry*>();

#ifdef _DEBUG
    // In debug builds try and catch cases where code attempts to use entries not allocated via this method.
    pEntry->m_pNextEntry = (VolatileEntry*)0x12345678;
#endif

    return &pEntry->m_sValue;
}

// Determine loader heap to be used for allocation of entries and bucket lists.
template <DAC_ENUM_HASH_PARAMS>
LoaderHeap *DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::GetHeap()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Explicitly provided heap takes priority.
    if (m_pHeap)
        return m_pHeap;

    // If not specified then we fall back to the owning module's heap (a module must have been specified in
    // this case).
    _ASSERTE(m_pModule != NULL);
    return GetModule()->GetAssembly()->GetLowFrequencyHeap();
}

// Insert an entry previously allocated via BaseAllocateEntry (you cannot allocated entries in any other
// manner) and associated with the given hash value. The entry should have been initialized prior to
// insertion.
template <DAC_ENUM_HASH_PARAMS>
void DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseInsertEntry(DacEnumerableHashValue iHash, VALUE *pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We are always guaranteed at least one bucket (which is important here: some hash table sub-classes
    // require entry insertion to be fault free).
    _ASSERTE(m_cBuckets > 0);

    // Recover the volatile entry pointer from the sub-class entry pointer passed to us. In debug builds
    // attempt to validate that this transform is really valid and the caller didn't attempt to allocate the
    // entry via some other means than BaseAllocateEntry().
    PTR_VolatileEntry pVolatileEntry = (PTR_VolatileEntry)((BYTE*)pEntry - offsetof(VolatileEntry, m_sValue));
    _ASSERTE(pVolatileEntry->m_pNextEntry == (VolatileEntry*)0x12345678);

    // Remember the entry hash code.
    pVolatileEntry->m_iHashValue = iHash;

    // Compute which bucket the entry belongs in based on the hash.
    DWORD dwBucket = iHash % m_cBuckets;

    // Prepare to link the new entry at the head of the bucket chain.
    pVolatileEntry->m_pNextEntry = (GetBuckets())[dwBucket];

    // Make sure that all writes to the entry are visible before publishing the entry.
    MemoryBarrier();

    // Publish the entry by pointing the bucket at it.
    (GetBuckets())[dwBucket] = pVolatileEntry;

    m_cEntries++;

    // If the insertion pushed the table load over our limit then attempt to grow the bucket list. Note that
    // we ignore any failure (this is a performance operation and is not required for correctness).
    if (m_cEntries > (2 * m_cBuckets))
        GrowTable();
}

// Increase the size of the bucket list in order to reduce the size of bucket chains. Does nothing on failure
// to allocate (since this impacts perf, not correctness).
template <DAC_ENUM_HASH_PARAMS>
void DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::GrowTable()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If we can't increase the number of buckets, we lose perf but not correctness. So we won't report this
    // error to our caller.
    FAULT_NOT_FATAL();

    // Make the new bucket table larger by the scale factor requested by the subclass (but also prime).
    DWORD cNewBuckets = NextLargestPrime(m_cBuckets * SCALE_FACTOR);
    S_SIZE_T cbNewBuckets = S_SIZE_T(cNewBuckets) * S_SIZE_T(sizeof(PTR_VolatileEntry));
    PTR_VolatileEntry *pNewBuckets = (PTR_VolatileEntry*)(void*)GetHeap()->AllocMem_NoThrow(cbNewBuckets);
    if (!pNewBuckets)
        return;

    // All buckets are initially empty.
    // Note: Memory allocated on loader heap is zero filled
    // memset(pNewBuckets, 0, cNewBuckets * sizeof(PTR_VolatileEntry));

    // Run through the old table and transfer all the entries. Be sure not to mess with the integrity of the
    // old table while we are doing this, as there can be concurrent readers! Note that it is OK if the
    // concurrent reader misses out on a match, though - they will have to acquire the lock on a miss & try
    // again.
    for (DWORD i = 0; i < m_cBuckets; i++)
    {
        PTR_VolatileEntry pEntry = (GetBuckets())[i];

        // Try to lock out readers from scanning this bucket. This is obviously a race which may fail.
        // However, note that it's OK if somebody is already in the list - it's OK if we mess with the bucket
        // groups, as long as we don't destroy anything. The lookup function will still do appropriate
        // comparison even if it wanders aimlessly amongst entries while we are rearranging things. If a
        // lookup finds a match under those circumstances, great. If not, they will have to acquire the lock &
        // try again anyway.
        (GetBuckets())[i] = NULL;

        while (pEntry != NULL)
        {
            DWORD dwNewBucket = pEntry->m_iHashValue % cNewBuckets;
            PTR_VolatileEntry pNextEntry  = pEntry->m_pNextEntry;

            pEntry->m_pNextEntry = pNewBuckets[dwNewBucket];
            pNewBuckets[dwNewBucket] = pEntry;

            pEntry = pNextEntry;
        }
    }

    // Make sure that all writes are visible before publishing the new array.
    MemoryBarrier();
    m_pBuckets = pNewBuckets;

    // The new number of buckets has to be published last (prior to this readers may miscalculate a bucket
    // index, but the result will always be in range and they'll simply walk the wrong chain and get a miss,
    // prompting a retry under the lock). If we let the count become visible unordered wrt to the bucket array
    // itself a reader could potentially read buckets from beyond the end of the old bucket list).
    MemoryBarrier();
    m_cBuckets = cNewBuckets;
}

// Returns the next prime larger (or equal to) than the number given.
template <DAC_ENUM_HASH_PARAMS>
DWORD DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::NextLargestPrime(DWORD dwNumber)
{
    for (DWORD i = 0; i < COUNTOF(g_rgPrimes); i++)
        if (g_rgPrimes[i] >= dwNumber)
        {
            dwNumber = g_rgPrimes[i];
            break;
        }

    return dwNumber;
}
#endif // !DACCESS_COMPILE

// Return the number of entries held in the table (does not include entries allocated but not inserted yet).
template <DAC_ENUM_HASH_PARAMS>
DWORD DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseGetElementCount()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_cEntries;
}

// Find first entry matching a given hash value (returns NULL on no match). Call BaseFindNextEntryByHash to
// iterate the remaining matches (until it returns NULL). The LookupContext supplied by the caller is
// initialized by BaseFindFirstEntryByHash and read/updated by BaseFindNextEntryByHash to keep track of where
// we are.
template <DAC_ENUM_HASH_PARAMS>
DPTR(VALUE) DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseFindFirstEntryByHash(DacEnumerableHashValue iHash, LookupContext *pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pContext));
    }
    CONTRACTL_END;

    // No point looking if there are no entries.
    if (m_cEntries == 0)
        return NULL;

    // Since there is at least one entry there must be at least one bucket.
    _ASSERTE(m_cBuckets > 0);

    // Compute which bucket the entry belongs in based on the hash.
    DWORD dwBucket = iHash % VolatileLoad(&m_cBuckets);

    // Point at the first entry in the bucket chain which would contain any entries with the given hash code.
    PTR_VolatileEntry pEntry = (GetBuckets())[dwBucket];

    // Walk the bucket chain one entry at a time.
    while (pEntry)
    {
        if (pEntry->m_iHashValue == iHash)
        {
            // We've found our match.

            // Record our current search state into the provided context so that a subsequent call to
            // BaseFindNextEntryByHash can pick up the search where it left off.
            pContext->m_pEntry = dac_cast<TADDR>(pEntry);

            // Return the address of the sub-classes' embedded entry structure.
            return VALUE_FROM_VOLATILE_ENTRY(pEntry);
        }

        // Move to the next entry in the chain.
        pEntry = pEntry->m_pNextEntry;
    }

    // If we get here then none of the entries in the target bucket matched the hash code and we have a miss
    // (for this section of the table at least).
    return NULL;
}

// Find first entry matching a given hash value (returns NULL on no match). Call BaseFindNextEntryByHash to
// iterate the remaining matches (until it returns NULL). The LookupContext supplied by the caller is
// initialized by BaseFindFirstEntryByHash and read/updated by BaseFindNextEntryByHash to keep track of where
// we are.
template <DAC_ENUM_HASH_PARAMS>
DPTR(VALUE) DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseFindNextEntryByHash(LookupContext *pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pContext));
    }
    CONTRACTL_END;

    DacEnumerableHashValue iHash;

    // Fetch the entry we were looking at last from the context and remember the corresponding hash code.
    PTR_VolatileEntry pVolatileEntry = dac_cast<PTR_VolatileEntry>(pContext->m_pEntry);
    iHash = pVolatileEntry->m_iHashValue;

    // Iterate over the bucket chain.
    while (pVolatileEntry->m_pNextEntry)
    {
        // Advance to the next entry.
        pVolatileEntry = pVolatileEntry->m_pNextEntry;
        if (pVolatileEntry->m_iHashValue == iHash)
        {
            // Found a match on hash code. Update our find context to indicate where we got to and return
            // a pointer to the sub-class portion of the entry.
            pContext->m_pEntry = dac_cast<TADDR>(pVolatileEntry);
            return VALUE_FROM_VOLATILE_ENTRY(pVolatileEntry);
        }
    }

    return NULL;
}

#ifdef DACCESS_COMPILE

namespace HashTableDetail
{
    // Use the C++ detection idiom (https://isocpp.org/blog/2017/09/detection-idiom-a-stopgap-for-concepts-simon-brand) to call the
    // derived table's EnumMemoryRegionsForEntry method if it defines one.
    template<typename...>
    using void_t = void;

    template<typename B>
    struct negation : std::integral_constant<bool, !bool(B::value)> { };
    template <DAC_ENUM_HASH_PARAMS, typename = void>
    struct HasCustomEntryMemoryRegionEnum : std::false_type {};

    template <DAC_ENUM_HASH_PARAMS>
    struct HasCustomEntryMemoryRegionEnum<DAC_ENUM_HASH_ARGS, void_t<decltype(std::declval<FINAL_CLASS>().EnumMemoryRegionsForEntry(std::declval<DPTR(VALUE)>(), std::declval<CLRDataEnumMemoryFlags>()))>> : std::true_type {};

    template<DAC_ENUM_HASH_PARAMS, typename std::enable_if<HasCustomEntryMemoryRegionEnum<DAC_ENUM_HASH_ARGS>::value>::type* = nullptr>
    void EnumMemoryRegionsForEntry(FINAL_CLASS* hashTable, DPTR(VALUE) pEntry, CLRDataEnumMemoryFlags flags)
    {
        hashTable->EnumMemoryRegionsForEntry(pEntry, flags);
    }

    template<DAC_ENUM_HASH_PARAMS, typename std::enable_if<negation<HasCustomEntryMemoryRegionEnum<DAC_ENUM_HASH_ARGS>>::value>::type* = nullptr>
    void EnumMemoryRegionsForEntry(FINAL_CLASS* hashTable, DPTR(VALUE) pEntry, CLRDataEnumMemoryFlags flags)
    {
        // The supplied hashTable doesn't provide an EnumMemoryRegionsForEntry() implementation.
    }
}

// Call during DAC enumeration of memory regions to save all hash table data structures. Calls derived-class
// implementation of EnumMemoryRegionsForEntry to allow additional per-entry memory to be reported.
template <DAC_ENUM_HASH_PARAMS>
void DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // Save the base data structure itself (can't use DAC_ENUM_DTHIS() since the size to save is based on a
    // sub-class).
    DacEnumMemoryRegion(dac_cast<TADDR>(this), sizeof(FINAL_CLASS));

    // Save the bucket list.
    DacEnumMemoryRegion(dac_cast<TADDR>(GetBuckets()), m_cBuckets * sizeof(VolatileEntry*));

    // Save all the entries.
    if (GetBuckets().IsValid())
    {
        for (DWORD i = 0; i < m_cBuckets; i++)
        {
            PTR_VolatileEntry pEntry = (GetBuckets())[i];
            while (pEntry.IsValid())
            {
                pEntry.EnumMem();

                // Ask the sub-class whether each entry points to further data to be saved.
                HashTableDetail::EnumMemoryRegionsForEntry<DAC_ENUM_HASH_ARGS>((FINAL_CLASS*)this, VALUE_FROM_VOLATILE_ENTRY(pEntry), flags);

                pEntry = pEntry->m_pNextEntry;
            }
        }
    }

    // Save the module if present.
    if (GetModule().IsValid())
        GetModule()->EnumMemoryRegions(flags, true);
}
#endif // DACCESS_COMPILE

// Initializes the iterator context passed by the caller to make it ready to walk every entry in the table in
// an arbitrary order. Call pIterator->Next() to retrieve the first entry.
template <DAC_ENUM_HASH_PARAMS>
void DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseInitIterator(BaseIterator *pIterator)
{
    LIMITED_METHOD_DAC_CONTRACT;

    pIterator->m_pTable = dac_cast<DPTR(DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>)>(this);
    pIterator->m_pEntry = NULL;
    pIterator->m_dwBucket = 0;
}

// Returns a pointer to the next entry in the hash table or NULL once all entries have been enumerated. Once
// NULL has been return the only legal operation is to re-initialize the iterator with BaseInitIterator.
template <DAC_ENUM_HASH_PARAMS>
DPTR(VALUE) DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseIterator::Next()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    while (m_dwBucket < m_pTable->m_cBuckets)
    {
        if (m_pEntry == NULL)
        {
            // This is our first lookup for a particular bucket, return the first
            // entry in that bucket.
            m_pEntry = dac_cast<TADDR>((m_pTable->GetBuckets())[m_dwBucket]);
        }
        else
        {
            // This is not our first lookup, return the entry immediately after the last one we
            // reported.
            m_pEntry = dac_cast<TADDR>(dac_cast<PTR_VolatileEntry>(m_pEntry)->m_pNextEntry);
        }

        // If we found an entry in the last step return with it.
        if (m_pEntry)
            return VALUE_FROM_VOLATILE_ENTRY(dac_cast<PTR_VolatileEntry>(m_pEntry));

        // Othwerwise we found the end of a bucket chain. Increment the current bucket and, if there are
        // buckets left to scan go back around again.
        m_dwBucket++;
    }
    return NULL;
}
