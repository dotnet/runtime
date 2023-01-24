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

    // two extra slots - slot [0] contains the length of the table,
    //                   slot [1] will contain the next version of the table if it resizes
    S_SIZE_T cbBuckets = S_SIZE_T(sizeof(VolatileEntry*)) * (S_SIZE_T(cInitialBuckets) + S_SIZE_T(SKIP_SPECIAL_SLOTS));

    m_cEntries = 0;
    PTR_VolatileEntry* pBuckets = (PTR_VolatileEntry*)(void*)GetHeap()->AllocMem(cbBuckets);
    ((size_t*)pBuckets)[SLOT_LENGTH] = cInitialBuckets;

    // publish after setting the length
    VolatileStore(&m_pBuckets, pBuckets);

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

    // Recover the volatile entry pointer from the sub-class entry pointer passed to us. In debug builds
    // attempt to validate that this transform is really valid and the caller didn't attempt to allocate the
    // entry via some other means than BaseAllocateEntry().
    PTR_VolatileEntry pVolatileEntry = (PTR_VolatileEntry)((BYTE*)pEntry - offsetof(VolatileEntry, m_sValue));
    _ASSERTE(pVolatileEntry->m_pNextEntry == (VolatileEntry*)0x12345678);

    // Remember the entry hash code.
    pVolatileEntry->m_iHashValue = iHash;

    DPTR(PTR_VolatileEntry) curBuckets = GetBuckets();
    DWORD cBuckets = GetLength(curBuckets);

    // Compute which bucket the entry belongs in based on the hash. (+2 to skip "length" and "next" slots)
    DWORD dwBucket = iHash % cBuckets + SKIP_SPECIAL_SLOTS;

    // Prepare to link the new entry at the head of the bucket chain.
    pVolatileEntry->m_pNextEntry = curBuckets[dwBucket];

    // Publish the entry by pointing the bucket at it.
    // Make sure that all writes to the entry are visible before publishing the entry.
    VolatileStore(&curBuckets[dwBucket], pVolatileEntry);

    m_cEntries++;

    // If the insertion pushed the table load over our limit then attempt to grow the bucket list. Note that
    // we ignore any failure (this is a performance operation and is not required for correctness).
    if (m_cEntries > (2 * cBuckets))
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

    DPTR(PTR_VolatileEntry) curBuckets = GetBuckets();
    DWORD cBuckets = GetLength(curBuckets);

    // Make the new bucket table larger by the scale factor requested by the subclass (but also prime).
    DWORD cNewBuckets = NextLargestPrime(cBuckets * SCALE_FACTOR);
    // two extra slots - slot [0] contains the length of the table,
    //                   slot [1] will contain the next version of the table if it resizes
    S_SIZE_T cbNewBuckets = (S_SIZE_T(cNewBuckets) + S_SIZE_T(SKIP_SPECIAL_SLOTS)) * S_SIZE_T(sizeof(PTR_VolatileEntry));

    PTR_VolatileEntry *pNewBuckets = (PTR_VolatileEntry*)(void*)GetHeap()->AllocMem_NoThrow(cbNewBuckets);
    if (!pNewBuckets)
        return;

    // element 0 stores the length of the table
    ((size_t*)pNewBuckets)[SLOT_LENGTH] = cNewBuckets;
    // element 1 stores the next version of the table (after length is written)
    // NOTE: DAC does not call add/grow, so this cast is ok.
    VolatileStore(&((PTR_VolatileEntry**)curBuckets)[SLOT_NEXT], pNewBuckets);

    // All buckets are initially empty.
    // Note: Memory allocated on loader heap is zero filled
    // memset(pNewBuckets, 0, cNewBuckets * sizeof(PTR_VolatileEntry));

    // Run through the old table and transfer all the entries. Be sure not to mess with the integrity of the
    // old table while we are doing this, as there can be concurrent readers!
    // IMPORTANT: every entry must be reachable from the old bucket
    //            only after an entry appears in the correct bucket in the new table we can remove it from the old.
    //            it is ok to appear temporarily in a wrong bucket.
    for (DWORD i = 0; i < cBuckets; i++)
    {
        // +2 to skip "length" and "next" slots
        DWORD dwCurBucket = i + SKIP_SPECIAL_SLOTS;
        PTR_VolatileEntry pEntry = curBuckets[dwCurBucket];

        while (pEntry != NULL)
        {
            DWORD dwNewBucket = (pEntry->m_iHashValue % cNewBuckets) + SKIP_SPECIAL_SLOTS;
            PTR_VolatileEntry pNextEntry  = pEntry->m_pNextEntry;

            PTR_VolatileEntry pTail = pNewBuckets[dwNewBucket];

            // make the pEntry reachable in the new bucket, together with all the chain (that is temporary and ok)
            if (pTail == NULL)
            {
                pNewBuckets[dwNewBucket] = pEntry;
            }
            else
            {
                while (pTail->m_pNextEntry)
                {
                    pTail = pTail->m_pNextEntry;
                }

                pTail->m_pNextEntry = pEntry;
            }

            // skip pEntry in the old bucket after it appears in the new.
            VolatileStore(&curBuckets[dwCurBucket], pNextEntry);

            // drop the rest of the bucket after old table starts referring to it
            VolatileStore(&pEntry->m_pNextEntry, (PTR_VolatileEntry)NULL);

            pEntry = pNextEntry;
        }
    }

    // Make sure that all writes are visible before publishing the new array.
    VolatileStore(&m_pBuckets, pNewBuckets);
}

// Returns the next prime larger (or equal to) than the number given.
template <DAC_ENUM_HASH_PARAMS>
DWORD DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::NextLargestPrime(DWORD dwNumber)
{
    for (DWORD i = 0; i < ARRAY_SIZE(g_rgPrimes); i++)
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

    DPTR(PTR_VolatileEntry) curBuckets = GetBuckets();
    return BaseFindFirstEntryByHashCore(curBuckets, iHash, pContext);
}

template <DAC_ENUM_HASH_PARAMS>
DPTR(VALUE) DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>::BaseFindFirstEntryByHashCore(DPTR(PTR_VolatileEntry) curBuckets, DacEnumerableHashValue iHash, LookupContext* pContext)
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

    do
    {
        DWORD cBuckets = GetLength(curBuckets);

        // Compute which bucket the entry belongs in based on the hash.
        // +2 to skip "length" and "next" slots
        DWORD dwBucket = iHash % cBuckets + SKIP_SPECIAL_SLOTS;

        // Point at the first entry in the bucket chain that stores entries with the given hash code.
        PTR_VolatileEntry pEntry = VolatileLoadWithoutBarrier(&curBuckets[dwBucket]);

        // Walk the bucket chain one entry at a time.
        while (pEntry)
        {
            if (pEntry->m_iHashValue == iHash)
            {
                // We've found our match.

                // Record our current search state into the provided context so that a subsequent call to
                // BaseFindNextEntryByHash can pick up the search where it left off.
                pContext->m_pEntry = dac_cast<TADDR>(pEntry);
                pContext->m_curBuckets = curBuckets;

                // Return the address of the sub-classes' embedded entry structure.
                return VALUE_FROM_VOLATILE_ENTRY(pEntry);
            }

            // Move to the next entry in the chain.
            pEntry = VolatileLoadWithoutBarrier(&pEntry->m_pNextEntry);
        }

        // in a rare case if resize is in progress, look in the new table as well.
        // if existing entry is not in the old table, it must be in the new
        // since we unlink it from old only after linking into the new.
        // check for next table must happen after we looked through the current.
        VolatileLoadBarrier();
        curBuckets = GetNext(curBuckets);
    } while (curBuckets != nullptr);

    // If we get here then none of the entries in the target bucket matched the hash code and we have a miss
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

    // Iterate over the rest ot the bucket chain.
    while ((pVolatileEntry = VolatileLoadWithoutBarrier(&pVolatileEntry->m_pNextEntry)) != nullptr)
    {
        if (pVolatileEntry->m_iHashValue == iHash)
        {
            // Found a match on hash code. Update our find context to indicate where we got to and return
            // a pointer to the sub-class portion of the entry.
            pContext->m_pEntry = dac_cast<TADDR>(pVolatileEntry);
            return VALUE_FROM_VOLATILE_ENTRY(pVolatileEntry);
        }
    }

    // check for next table must happen after we looked through the current.
    VolatileLoadBarrier();

    // in a case if resize is in progress, look in the new table as well.
    DPTR(PTR_VolatileEntry) nextBuckets = GetNext(pContext->m_curBuckets);
    if (nextBuckets != nullptr)
    {
        return BaseFindFirstEntryByHashCore(nextBuckets, iHash, pContext);
    }

    return NULL;
}

#ifdef DACCESS_COMPILE

namespace HashTableDetail
{
    // Use the C++ detection idiom (https://isocpp.org/blog/2017/09/detection-idiom-a-stopgap-for-concepts-simon-brand) to call the
    // derived table's EnumMemoryRegionsForEntry method if it defines one.
    template <class... > struct make_void { using type = void; };
    template <class... T> using void_t = typename make_void<T...>::type;

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

    DPTR(PTR_VolatileEntry) curBuckets = GetBuckets();
    DWORD cBuckets = GetLength(curBuckets);

    // Save the bucket list.
    DacEnumMemoryRegion(dac_cast<TADDR>(curBuckets), cBuckets * sizeof(VolatileEntry*));

    // Save all the entries.
    if (GetBuckets().IsValid())
    {
        for (DWORD i = 0; i < cBuckets; i++)
        {
            //+2 to skip "length" and "next" slots
            PTR_VolatileEntry pEntry = curBuckets[i + SKIP_SPECIAL_SLOTS];
            while (pEntry.IsValid())
            {
                pEntry.EnumMem();

                // Ask the sub-class whether each entry points to further data to be saved.
                HashTableDetail::EnumMemoryRegionsForEntry<DAC_ENUM_HASH_ARGS>((FINAL_CLASS*)this, VALUE_FROM_VOLATILE_ENTRY(pEntry), flags);

                pEntry = pEntry->m_pNextEntry;
            }
        }
    }

    // we should not be resizing while enumerating memory regions.
    _ASSERTE(GetNext(curBuckets) == NULL);

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
    //+2 to skip "length" and "next" slots
    pIterator->m_dwBucket = SKIP_SPECIAL_SLOTS;
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

    DPTR(PTR_VolatileEntry) curBuckets = m_pTable->GetBuckets();
    DWORD cBuckets = GetLength(curBuckets);

    while (m_dwBucket < cBuckets + SKIP_SPECIAL_SLOTS)
    {
        if (m_pEntry == NULL)
        {
            // This is our first lookup for a particular bucket, return the first
            // entry in that bucket.
            m_pEntry = dac_cast<TADDR>(curBuckets[m_dwBucket]);
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

        // Otherwise we found the end of a bucket chain. Increment the current bucket and, if there are
        // buckets left to scan go back around again.
        m_dwBucket++;
    }

    // we should not be resizing while iterating.
    _ASSERTE(GetNext(curBuckets) == NULL);

    return NULL;
}
