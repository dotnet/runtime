// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// Abstract base class implementation of a hash table suitable for efficient serialization into an ngen image.
// See NgenHash.h for a more detailed description.
//

// Our implementation embeds entry data supplied by the hash sub-class into a larger entry structure
// containing NgenHash metadata. We often end up returning pointers to the inner entry to sub-class code and
// doing this in a DAC-friendly fashion involves some DAC gymnastics. The following couple of macros factor
// those complexities out.
#define VALUE_FROM_VOLATILE_ENTRY(_ptr) dac_cast<DPTR(VALUE)>(PTR_TO_MEMBER_TADDR(VolatileEntry, (_ptr), m_sValue))
#define VALUE_FROM_PERSISTED_ENTRY(_ptr) dac_cast<DPTR(VALUE)>(PTR_TO_MEMBER_TADDR(PersistedEntry, (_ptr), m_sValue))

// We provide a mechanism for the sub-class to extend per-entry operations via a callback mechanism where the
// sub-class implements methods with a certain name and signature (details in the module header for
// NgenHash.h). We could have used virtual methods, but this adds a needless indirection since all the details
// are known statically. In order to have a base class call a method defined only in a sub-class however we
// need a little pointer trickery. The following macro hides that.
#define DOWNCALL(_method) ((FINAL_CLASS*)this)->_method

#ifndef DACCESS_COMPILE

// Base constructor. Call this from your derived constructor to provide the owning module, loader heap and
// initial number of buckets (which must be non-zero). Module must be provided if this hash is to be
// serialized into an ngen image. It is exposed to the derived hash class (many need it) but otherwise is only
// used to locate a loader heap for allocating bucket lists and entries unless an alternative heap is
// provided. Note that the heap provided is not serialized (so you'll allocate from that heap at ngen-time,
// but revert to allocating from the module's heap at runtime). If no Module pointer is supplied (non-ngen'd
// hash table) you must provide a direct heap pointer.
template <NGEN_HASH_PARAMS>
NgenHashTable<NGEN_HASH_ARGS>::NgenHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // An invariant in the code is that we always have a non-zero number of warm buckets.
    _ASSERTE(cInitialBuckets > 0);

    // At least one of module or heap must have been specified or we won't know how to allocate entries and
    // buckets.
    _ASSERTE(pModule || pHeap);
    m_pModule = pModule;
    m_pHeap = pHeap;

    S_SIZE_T cbBuckets = S_SIZE_T(sizeof(VolatileEntry*)) * S_SIZE_T(cInitialBuckets);

    m_cWarmEntries = 0;
    m_cWarmBuckets = cInitialBuckets;
    m_pWarmBuckets = (PTR_VolatileEntry*)(void*)GetHeap()->AllocMem(cbBuckets);

    // Note: Memory allocated on loader heap is zero filled
    // memset(m_pWarmBuckets, 0, sizeof(VolatileEntry*) * cInitialBuckets);

#ifdef FEATURE_PREJIT
    memset(&m_sHotEntries, 0, sizeof(PersistedEntries));
    memset(&m_sColdEntries, 0, sizeof(PersistedEntries));
    m_cInitialBuckets = cInitialBuckets;
#endif // FEATURE_PREJIT
}

// Allocate an uninitialized entry for the hash table (it's not inserted). The AllocMemTracker is optional and
// may be specified as NULL for untracked allocations. This is split from the hash insertion logic so that
// callers can pre-allocate entries and then perform insertions which cannot fault.
template <NGEN_HASH_PARAMS>
VALUE *NgenHashTable<NGEN_HASH_ARGS>::BaseAllocateEntry(AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Faults are forbidden in BaseInsertEntry. Make the table writeable now that the faults are still allowed.
    EnsureWritablePages(this);
    EnsureWritablePages(this->m_pWarmBuckets, m_cWarmBuckets * sizeof(PTR_VolatileEntry));

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
template <NGEN_HASH_PARAMS>
LoaderHeap *NgenHashTable<NGEN_HASH_ARGS>::GetHeap()
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
    return m_pModule->GetAssembly()->GetLowFrequencyHeap();
}

// Insert an entry previously allocated via BaseAllocateEntry (you cannot allocated entries in any other
// manner) and associated with the given hash value. The entry should have been initialized prior to
// insertion.
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::BaseInsertEntry(NgenHashValue iHash, VALUE *pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We are always guaranteed at least one warm bucket (which is important here: some hash table sub-classes
    // require entry insertion to be fault free).
    _ASSERTE(m_cWarmBuckets > 0);

    // Recover the volatile entry pointer from the sub-class entry pointer passed to us. In debug builds
    // attempt to validate that this transform is really valid and the caller didn't attempt to allocate the
    // entry via some other means than BaseAllocateEntry().
    PTR_VolatileEntry pVolatileEntry = (PTR_VolatileEntry)((BYTE*)pEntry - offsetof(VolatileEntry, m_sValue));
    _ASSERTE(pVolatileEntry->m_pNextEntry == (VolatileEntry*)0x12345678);

    // Remember the entry hash code.
    pVolatileEntry->m_iHashValue = iHash;

    // Compute which bucket the entry belongs in based on the hash.
    DWORD dwBucket = iHash % m_cWarmBuckets;

    // Prepare to link the new entry at the head of the bucket chain.
    pVolatileEntry->m_pNextEntry = m_pWarmBuckets[dwBucket];

    // Make sure that all writes to the entry are visible before publishing the entry.
    MemoryBarrier();

    // Publish the entry by pointing the bucket at it.
    m_pWarmBuckets[dwBucket] = pVolatileEntry;

    m_cWarmEntries++;

    // If the insertion pushed the table load over our limit then attempt to grow the bucket list. Note that
    // we ignore any failure (this is a performance operation and is not required for correctness).
    if (m_cWarmEntries > (2 * m_cWarmBuckets))
        GrowTable();
}

// Increase the size of the bucket list in order to reduce the size of bucket chains. Does nothing on failure
// to allocate (since this impacts perf, not correctness).
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::GrowTable()
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
    DWORD cNewBuckets = NextLargestPrime(m_cWarmBuckets * SCALE_FACTOR);
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
    for (DWORD i = 0; i < m_cWarmBuckets; i++)
    {
        PTR_VolatileEntry pEntry = m_pWarmBuckets[i];

        // Try to lock out readers from scanning this bucket. This is obviously a race which may fail.
        // However, note that it's OK if somebody is already in the list - it's OK if we mess with the bucket
        // groups, as long as we don't destroy anything. The lookup function will still do appropriate
        // comparison even if it wanders aimlessly amongst entries while we are rearranging things. If a
        // lookup finds a match under those circumstances, great. If not, they will have to acquire the lock &
        // try again anyway.
        m_pWarmBuckets[i] = NULL;

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
    m_pWarmBuckets = pNewBuckets;

    // The new number of buckets has to be published last (prior to this readers may miscalculate a bucket
    // index, but the result will always be in range and they'll simply walk the wrong chain and get a miss,
    // prompting a retry under the lock). If we let the count become visible unordered wrt to the bucket array
    // itself a reader could potentially read buckets from beyond the end of the old bucket list).
    MemoryBarrier();
    m_cWarmBuckets = cNewBuckets;
}

// Returns the next prime larger (or equal to) than the number given.
template <NGEN_HASH_PARAMS>
DWORD NgenHashTable<NGEN_HASH_ARGS>::NextLargestPrime(DWORD dwNumber)
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
template <NGEN_HASH_PARAMS>
DWORD NgenHashTable<NGEN_HASH_ARGS>::BaseGetElementCount()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_cWarmEntries
#ifdef FEATURE_PREJIT
        + m_sHotEntries.m_cEntries + m_sColdEntries.m_cEntries
#endif
        ;
}

// Find first entry matching a given hash value (returns NULL on no match). Call BaseFindNextEntryByHash to
// iterate the remaining matches (until it returns NULL). The LookupContext supplied by the caller is
// initialized by BaseFindFirstEntryByHash and read/updated by BaseFindNextEntryByHash to keep track of where
// we are.
template <NGEN_HASH_PARAMS>
DPTR(VALUE) NgenHashTable<NGEN_HASH_ARGS>::BaseFindFirstEntryByHash(NgenHashValue iHash, LookupContext *pContext)
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

    DPTR(VALUE) pEntry;

#ifdef FEATURE_PREJIT
    // Look in the hot entries first.
    pEntry = FindPersistedEntryByHash(&m_sHotEntries, iHash, pContext);
    if (pEntry)
        return pEntry;
#endif // FEATURE_PREJIT

    // Then the warm entries.
    pEntry = FindVolatileEntryByHash(iHash, pContext);
    if (pEntry)
        return pEntry;

#ifdef FEATURE_PREJIT
    // And finally the cold entries.
    return FindPersistedEntryByHash(&m_sColdEntries, iHash, pContext);
#else // FEATURE_PREJIT
    return NULL;
#endif // FEATURE_PREJIT
}

// Find first entry matching a given hash value (returns NULL on no match). Call BaseFindNextEntryByHash to
// iterate the remaining matches (until it returns NULL). The LookupContext supplied by the caller is
// initialized by BaseFindFirstEntryByHash and read/updated by BaseFindNextEntryByHash to keep track of where
// we are.
template <NGEN_HASH_PARAMS>
DPTR(VALUE) NgenHashTable<NGEN_HASH_ARGS>::BaseFindNextEntryByHash(LookupContext *pContext)
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

    NgenHashValue iHash;

    switch (pContext->m_eType)
    {
#ifdef FEATURE_PREJIT
    case Hot:
    case Cold:
    {
        // Fetch the entry we were looking at last from the context and remember the corresponding hash code.
        PTR_PersistedEntry pPersistedEntry = dac_cast<PTR_PersistedEntry>(pContext->m_pEntry);
        iHash = pPersistedEntry->m_iHashValue;

        // Iterate while there are still entries left in the bucket chain.
        while (pContext->m_cRemainingEntries)
        {
            // Advance to next entry, reducing the number of entries left to scan.
            pPersistedEntry++;
            pContext->m_cRemainingEntries--;

            if (pPersistedEntry->m_iHashValue == iHash)
            {
                // Found a match on hash code. Update our find context to indicate where we got to and return
                // a pointer to the sub-class portion of the entry.
                pContext->m_pEntry = dac_cast<TADDR>(pPersistedEntry);
                return VALUE_FROM_PERSISTED_ENTRY(pPersistedEntry);
            }
        }

        // We didn't find a match.
        if (pContext->m_eType == Hot)
        {
            // If we were searching the hot entries then we should try the warm entries next.
            DPTR(VALUE) pNext = FindVolatileEntryByHash(iHash, pContext);
            if (pNext)
                return pNext;

            // If that didn't work try the cold entries.
            return FindPersistedEntryByHash(&m_sColdEntries, iHash, pContext);
        }

        // We were already searching cold entries, a failure here means the entry is not in the table.
        return NULL;
    }
#endif // FEATURE_PREJIT

    case Warm:
    {
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

        // We didn't find a match, fall through to the cold entries.
#ifdef FEATURE_PREJIT
        return FindPersistedEntryByHash(&m_sColdEntries, iHash, pContext);
#else
        return NULL;
#endif
    }

    default:
        _ASSERTE(!"Unknown NgenHashTable entry type");
        return NULL;
    }
}

#ifdef FEATURE_PREJIT

// Allocate and initialize a new list with the given count of buckets and configured to hold no more than the
// given number of entries or have a bucket chain longer than the specified maximum. These two maximums allow
// the implementation to choose an optimal data format for the bucket list at runtime and are enforced by
// asserts in the debug build.
// static
template <NGEN_HASH_PARAMS>
typename NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList *NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList::CreateList(DWORD cBuckets, DWORD cEntries, DWORD cMaxEntriesInBucket)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // The size of each bucket depends on the number of entries we need to store and how big a bucket chain
    // ever gets.
    DWORD cbBucket = GetBucketSize(cEntries, cMaxEntriesInBucket);

    // Allocate enough memory to store the bucket list header and bucket array.
    S_SIZE_T cbBuckets = S_SIZE_T(sizeof(PersistedBucketList)) + (S_SIZE_T(cbBucket) * S_SIZE_T(cBuckets));
    if (cbBuckets.IsOverflow())
        COMPlusThrowOM();
    PersistedBucketList *pBucketList = (PersistedBucketList*)(new BYTE[cbBuckets.Value()]);

#ifdef _DEBUG
    // In debug builds we store all the input parameters to validate subsequent requests. In retail none of
    // this data is needed.
    pBucketList->m_cBuckets = cBuckets;
    pBucketList->m_cEntries = cEntries;
    pBucketList->m_cMaxEntriesInBucket = cMaxEntriesInBucket;
#endif // _DEBUG

    pBucketList->m_cbBucket = cbBucket;
    pBucketList->m_dwEntryCountShift = BitsRequired(cEntries);
    pBucketList->m_dwInitialEntryMask = (1 << pBucketList->m_dwEntryCountShift) - 1;

    // Zero all the buckets (empty all the bucket chains).
    memset(pBucketList + 1, 0, cBuckets * cbBucket);

    return pBucketList;
}

// Get the size in bytes of this entire bucket list (need to pass in the bucket count since we save space by
// not storing it here, but we do validate this in debug mode).
template <NGEN_HASH_PARAMS>
size_t NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList::GetSize(DWORD cBuckets)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(cBuckets == m_cBuckets);
    return sizeof(PersistedBucketList) + (cBuckets * m_cbBucket);
}

// Get the initial entry index and entry count for the given bucket. Initial entry index value is undefined
// when count comes back as zero.
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList::GetBucket(DWORD dwIndex, DWORD *pdwFirstEntry, DWORD *pdwCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(dwIndex < m_cBuckets);

    // Find the start of the bucket we're interested in based on the index and size chosen for buckets in this
    // list instance.
    TADDR pBucket = dac_cast<TADDR>(this) + sizeof(PersistedBucketList) + (dwIndex * m_cbBucket);

    // Handle each format of bucket separately. In all cases read the correct number of bytes to form one
    // bitfield, extract the low order bits to retrieve the initial entry index and shift down the remaining
    // bits to obtain the entry count.
    switch (m_cbBucket)
    {
    case 2:
    {
        _ASSERTE(m_dwEntryCountShift < 16 && m_dwInitialEntryMask < 0xffff);

        WORD wBucketContents = *dac_cast<PTR_WORD>(pBucket);

        *pdwFirstEntry = wBucketContents & m_dwInitialEntryMask;
        *pdwCount = wBucketContents >> m_dwEntryCountShift;

        break;
    }

    case 4:
    {
        _ASSERTE(m_dwEntryCountShift < 32 && m_dwInitialEntryMask < 0xffffffff);

        DWORD dwBucketContents = *dac_cast<PTR_DWORD>(pBucket);

        *pdwFirstEntry = dwBucketContents & m_dwInitialEntryMask;
        *pdwCount = dwBucketContents >> m_dwEntryCountShift;

        break;
    }

    case 8:
    {
        _ASSERTE(m_dwEntryCountShift < 64);

        ULONG64 qwBucketContents = *dac_cast<PTR_ULONG64>(pBucket);

        *pdwFirstEntry = (DWORD)(qwBucketContents & m_dwInitialEntryMask);
        *pdwCount = (DWORD)(qwBucketContents >> m_dwEntryCountShift);

        break;
    }

    default:
#ifdef DACCESS_COMPILE
        // Minidumps don't guarantee this will work - memory may not have been dumped, target corrupted, etc.
        *pdwFirstEntry = 0;
        *pdwCount = 0;
#else
        _ASSERTE(!"Invalid bucket list bucket size");
#endif
    }

    _ASSERTE((*pdwFirstEntry < m_cEntries) || (*pdwCount == 0));
    _ASSERTE(*pdwCount <= m_cMaxEntriesInBucket);
}

// Simplified initial entry index when you don't need the count (don't call this for buckets with zero
// entries).
template <NGEN_HASH_PARAMS>
DWORD NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList::GetInitialEntry(DWORD dwIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DWORD dwInitialEntry, dwEntryCount;
    GetBucket(dwIndex, &dwInitialEntry, &dwEntryCount);

    _ASSERTE(dwEntryCount > 0);

    return dwInitialEntry;
}

// For the given bucket set the index of the initial entry and the count of entries in the chain. If the count
// is zero the initial entry index is meaningless and ignored.
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList::SetBucket(DWORD dwIndex, DWORD dwFirstEntry, DWORD cEntries)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(dwIndex < m_cBuckets);
    _ASSERTE(cEntries <= m_cMaxEntriesInBucket);
    if (cEntries > 0)
    {
        _ASSERTE(dwFirstEntry < m_cEntries);
        _ASSERTE(dwFirstEntry <= m_dwInitialEntryMask);
    }

    // Find the start of the bucket we're interested in based on the index and size chosen for buckets in this
    // list instance.
    BYTE *pbBucket = (BYTE*)this + sizeof(PersistedBucketList) + (dwIndex * m_cbBucket);

    // Handle each format of bucket separately. In all cases form a single bitfield with low-order bits
    // specifying the initial entry index and higher bits containing the entry count. Write this into the
    // bucket entry using the correct number of bytes.
    ULONG64 qwBucketBits = dwFirstEntry | (cEntries << m_dwEntryCountShift);
    switch (m_cbBucket)
    {
    case 2:
    {
        _ASSERTE(m_dwEntryCountShift < 16 && m_dwInitialEntryMask < 0xffff);
        *(WORD*)pbBucket = (WORD)qwBucketBits;
        break;
    }

    case 4:
    {
        _ASSERTE(m_dwEntryCountShift < 32 && m_dwInitialEntryMask < 0xffffffff);
        *(DWORD*)pbBucket = (DWORD)qwBucketBits;
        break;
    }

    case 8:
    {
        _ASSERTE(m_dwEntryCountShift < 64);
        *(ULONG64*)pbBucket = qwBucketBits;
        break;
    }

    default:
        _ASSERTE(!"Invalid bucket list bucket size");
    }
}

// Return the number of bits required to express a unique ID for the number of entities given.
//static
template <NGEN_HASH_PARAMS>
DWORD NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList::BitsRequired(DWORD cEntities)
{
    LIMITED_METHOD_CONTRACT;

    // Starting with a bit-mask of the most significant bit and iterating over masks for successively less
    // significant bits, stop as soon as the mask co-incides with a set bit in the value. Simultaneously we're
    // counting down the bits required to express the range of values implied by seeing the corresponding bit
    // set in the value (e.g. when we're testing the high bit we know we'd need 32-bits to encode the range of
    // values that have this bit set). Stop when we get to one bit (we never return 0 bits required, even for
    // an input value of 0).
    DWORD dwMask = 0x80000000;
    DWORD cBits = 32;
    while (cBits > 1)
    {
        if (cEntities & dwMask)
            return cBits;

        dwMask >>= 1;
        cBits--;
    }

    return 1;
}

// Return the minimum size (in bytes) of each bucket list entry that can express all buckets given the max
// count of entries and entries in a single bucket chain.
// static
template <NGEN_HASH_PARAMS>
DWORD NgenHashTable<NGEN_HASH_ARGS>::PersistedBucketList::GetBucketSize(DWORD cEntries, DWORD cMaxEntriesInBucket)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We need enough bits to express a start entry index (related to the total number of entries in the
    // table) and a chain count (so take the maximum chain length into consideration).
    DWORD cTotalBits = BitsRequired(cEntries) + BitsRequired(cMaxEntriesInBucket);

    // Rather than support complete flexibility (an arbitrary number of bytes to express the combination of
    // the two bitfields above) we'll just pull out the most useful selection (which simplifies the access
    // code and potentially might give us a perf edge over the more generalized algorithm).

    // We want naturally aligned bucket entries for access perf, 1 byte entries aren't all that interesting
    // (most tables won't be small enough to be expressed this way and those that are won't get much benefit
    // from the extra compression of the bucket list). We also don't believe we'll ever need more than 64
    // bits. This leaves us with 2, 4 and 8 byte entries. The tables in the current desktop CLR for mscorlib
    // will fit in the 2-byte category and will give us substantial space saving over the naive implementation
    // of a bucket with two DWORDs.

    if (cTotalBits <= 16)
        return 2;

    if (cTotalBits <= 32)
        return 4;

    // Invariant guaranteed by BitsRequired above.
    _ASSERTE(cTotalBits <= 64);
    return 8;
}

#ifndef DACCESS_COMPILE

// Call during ngen to save hash table data structures into the ngen image. Calls derived-class
// implementations of ShouldSave to determine which entries should be serialized, IsHotEntry to hot/cold split
// the entries and SaveEntry to allow per-entry extension of the saving process.
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::BaseSave(DataImage *pImage, CorProfileData *pProfileData)
{
    STANDARD_VM_CONTRACT;

    // This is a fairly long and complex process but at its heart it's fairly linear. We perform multiple
    // passes over the data in sequence which might seem slow but everything is arranged to avoid any O(N^2)
    // algorithms.

    // Persisted hashes had better have supplied an owning module at creation time (otherwise we won't know
    // how to find a loader heap for further allocations at runtime: we don't know how to serialize a loader
    // heap pointer).
    _ASSERTE(m_pModule != NULL);

    // We can only save once during ngen so the hot and cold sections of the hash cannot have been populated
    // yet.
    _ASSERTE(m_sHotEntries.m_cEntries == 0 && m_sColdEntries.m_cEntries == 0);

    DWORD i;

    // As we re-arrange volatile warm entries into hot and cold sets of persisted entries we need to keep lots
    // of intermediate tracking information. We also need to provide a subset of this mapping information to
    // the sub-class (so it can fix up cross entry references for example). The temporary structure allocated
    // below performs that function (it will be destructed automatically at the end of this method).
    EntryMappingTable sEntryMap;
    sEntryMap.m_cEntries = m_cWarmEntries;
#ifdef _PREFAST_
#pragma warning(suppress:6211) // Suppress bogus prefast warning about memory leak (EntryMappingTable acts as a holder)
#endif

    // The 'typename' keyword shouldn't be necessary, but g++ gets confused without it.
    sEntryMap.m_pEntries = new typename EntryMappingTable::Entry[m_cWarmEntries];

    //
    // PHASE 1
    //
    //  Iterate all the current warm entries, ask the sub-class which of them should be saved into the image
    //  and of those which are hot and which are cold.
    //

    DWORD cHotEntries = 0;
    DWORD cColdEntries = 0;

    // Visit each warm bucket.
    for (i = 0; i < m_cWarmBuckets; i++)
    {
        // Iterate through the chain of warm entries for this bucket.
        VolatileEntry *pOldEntry = m_pWarmBuckets[i];
        while (pOldEntry)
        {
            // Is the current entry being saved into the image?
            if (DOWNCALL(ShouldSave)(pImage, &pOldEntry->m_sValue))
            {
                // Yes, so save the details into the next available slot in the entry map. At this stage we
                // know the original entry address, the hash value and whether the entry is hot or cold.
                DWORD dwCurrentEntry = cHotEntries + cColdEntries;
                sEntryMap.m_pEntries[dwCurrentEntry].m_pOldEntry = &pOldEntry->m_sValue;
                sEntryMap.m_pEntries[dwCurrentEntry].m_iHashValue = pOldEntry->m_iHashValue;

                // Is the entry hot? When given no profile data we assume cold.
                if (pProfileData != NULL && DOWNCALL(IsHotEntry)(&pOldEntry->m_sValue, pProfileData))
                {
                    cHotEntries++;
                    sEntryMap.m_pEntries[dwCurrentEntry].m_fHot = true;
                }
                else
                {
                    cColdEntries++;
                    sEntryMap.m_pEntries[dwCurrentEntry].m_fHot = false;
                }
            }

            pOldEntry = pOldEntry->m_pNextEntry;
        }
    }

    // Set size of the entry map based on the real number of entries we're going to save.
    _ASSERTE((cHotEntries + cColdEntries) <= m_cWarmEntries);
    sEntryMap.m_cEntries = cHotEntries + cColdEntries;

    //
    // PHASE 2
    //
    //  Determine the layout of the new hot and cold tables (if applicable). We pick new bucket list sizes
    //  based on the number of entries to go in each table and from that we can calculate the length of each
    //  entry chain off each bucket (which is important both to derive a maximum chain length used when
    //  picking an optimized encoding for the bucket list and allows us to layout the new entries in linear
    //  time).
    //
    // We need a couple of extra arrays to track bucket chain sizes until we have enough info to allocate the
    // new bucket lists themselves.
    //

    // We'll allocate half as many buckets as entries (with at least 1 bucket, or zero if there are no entries
    // in this section of the hash).
    DWORD cHotBuckets = cHotEntries ? NextLargestPrime(cHotEntries / 2) : 0;
    DWORD cColdBuckets = cColdEntries ? NextLargestPrime(cColdEntries / 2) : 0;    

    // Allocate arrays to track bucket chain lengths for each hot or cold bucket list (as needed).
    DWORD *pHotBucketSizes = cHotBuckets ? new DWORD[cHotBuckets] : NULL;
    memset(pHotBucketSizes, 0, cHotBuckets * sizeof(DWORD));

    DWORD *pColdBucketSizes = cColdBuckets ? new DWORD[cColdBuckets] : NULL;
    memset(pColdBucketSizes, 0, cColdBuckets * sizeof(DWORD));

    // We'll calculate the maximum bucket chain length separately for hot and cold sections (each has its own
    // bucket list that might be optimized differently).
    DWORD cMaxHotChain = 0;
    DWORD cMaxColdChain = 0;

    // Iterate through all the entries to be saved (linear scan through the entry map we built in phase 1).
    for (i = 0; i < sEntryMap.m_cEntries; i++)
    {
        // The 'typename' keyword shouldn't be necessary, but g++ gets confused without it.
        typename EntryMappingTable::Entry *pMapEntry = &sEntryMap.m_pEntries[i];

        // For each entry calculate which bucket it will end up in under the revised bucket list. Also record
        // its order in the bucket chain (first come, first served). Recording this ordinal now is what allows
        // us to lay out entries into their final order using a linear algorithm in a later phase.
        if (pMapEntry->m_fHot)
        {
            pMapEntry->m_dwNewBucket = pMapEntry->m_iHashValue % cHotBuckets;
            pMapEntry->m_dwChainOrdinal = pHotBucketSizes[pMapEntry->m_dwNewBucket]++;
            if (pHotBucketSizes[pMapEntry->m_dwNewBucket] > cMaxHotChain)
                cMaxHotChain = pHotBucketSizes[pMapEntry->m_dwNewBucket];
        }
        else
        {
            // The C++ compiler is currently complaining that cColdBuckets could be zero in the modulo
            // operation below. It cannot due to the logic in this method (if we have a cold entry we'll have
            // at least one cold bucket, see the assignments above) but the flow is far too complex for the
            // C++ compiler to follow. Unfortunately it won't be told (the warning can't be disabled and even
            // an __assume won't work) so we take the hit of generating the useless extra if below.
            if (cColdBuckets > 0)
            {
                pMapEntry->m_dwNewBucket = pMapEntry->m_iHashValue % cColdBuckets;
                pMapEntry->m_dwChainOrdinal = pColdBucketSizes[pMapEntry->m_dwNewBucket]++;
                if (pColdBucketSizes[pMapEntry->m_dwNewBucket] > cMaxColdChain)
                    cMaxColdChain = pColdBucketSizes[pMapEntry->m_dwNewBucket];
            }
            else
                _ASSERTE(!"Should be unreachable, see comment above");
        }
    }

    //
    // PHASE 3
    //
    //  Allocate the new hot and cold bucket lists and entry arrays (as needed). The bucket lists have
    //  optimized layout based on knowledge of the entries they will map (total number of entries and the size
    //  of the largest single bucket chain).
    //

    if (cHotEntries)
    {
        m_sHotEntries.m_cEntries = cHotEntries;
        m_sHotEntries.m_cBuckets = cHotBuckets;
        m_sHotEntries.m_pEntries = new PersistedEntry[cHotEntries];
        m_sHotEntries.m_pBuckets = PersistedBucketList::CreateList(cHotBuckets, cHotEntries, cMaxHotChain);
        memset(m_sHotEntries.m_pEntries, 0, cHotEntries * sizeof(PersistedEntry));      // NGen determinism
    }

    if (cColdEntries)
    {
        m_sColdEntries.m_cEntries = cColdEntries;
        m_sColdEntries.m_cBuckets = cColdBuckets;
        m_sColdEntries.m_pEntries = new PersistedEntry[cColdEntries];
        m_sColdEntries.m_pBuckets = PersistedBucketList::CreateList(cColdBuckets, cColdEntries, cMaxColdChain);
        memset(m_sColdEntries.m_pEntries, 0, cColdEntries * sizeof(PersistedEntry));    // NGen determinism
    }

    //
    // PHASE 4
    //
    //  Initialize the bucket lists. We need to set an initial entry index (index into the entry array) and
    //  entry count for each bucket. The counts we already computed in phase 2 and since we're free to order
    //  the entry array however we like, we can compute the initial entry index for each bucket in turn
    //  trivially by laying out all entries for bucket 0 first followed by all entries for bucket 1 etc.
    //
    // This also has the nice effect of placing entries in the same bucket chain contiguously (and in the
    // order that a full hash traversal will take).
    //

    DWORD dwNextId = 0; // This represents the index of the next entry to start a bucket chain
    for (i = 0; i < cHotBuckets; i++)
    {
        m_sHotEntries.m_pBuckets->SetBucket(i, dwNextId, pHotBucketSizes[i]);
        dwNextId += pHotBucketSizes[i];
    }
    _ASSERTE(dwNextId == m_sHotEntries.m_cEntries);

    dwNextId = 0; // Reset index for the cold entries (remember they have their own table of entries)
    for (i = 0; i < cColdBuckets; i++)
    {
        m_sColdEntries.m_pBuckets->SetBucket(i, dwNextId, pColdBucketSizes[i]);
        dwNextId += pColdBucketSizes[i];
    }
    _ASSERTE(dwNextId == m_sColdEntries.m_cEntries);

    //
    // PHASE 5
    //
    //  Determine new addresses for each entry. This is relatively simple since we know the bucket index, the
    //  index of the first entry for that bucket and how far into that chain each entry is located.
    //

    for (i = 0; i < sEntryMap.m_cEntries; i++)
    {
        // The 'typename' keyword shouldn't be necessary, but g++ gets confused without it.
        typename EntryMappingTable::Entry *pMapEntry = &sEntryMap.m_pEntries[i];

        // Entry block depends on whether this entry is hot or cold.
        PersistedEntries *pEntries = pMapEntry->m_fHot ? &m_sHotEntries : &m_sColdEntries;

        // We already know the new bucket this entry will go into. Retrieve the index of the first entry in
        // that bucket chain.
        DWORD dwBaseChainIndex = pEntries->m_pBuckets->GetInitialEntry(pMapEntry->m_dwNewBucket);

        // This entry will be located at some offset from the index above (we calculated this ordinal in phase
        // 2).
        PersistedEntry *pNewEntry = &pEntries->m_pEntries[dwBaseChainIndex + pMapEntry->m_dwChainOrdinal];

        // Record the address of the embedded sub-class hash entry in the map entry (sub-classes will use this
        // info to map old entry addresses to their new locations).
        sEntryMap.m_pEntries[i].m_pNewEntry = &pNewEntry->m_sValue;

        // Initialize the new entry. Note that a simple bit-copy is performed on the sub-classes embedded
        // entry. If fixups are needed they can be performed in the call to SaveEntry in the next phase.
        pNewEntry->m_sValue = *pMapEntry->m_pOldEntry;
        pNewEntry->m_iHashValue = pMapEntry->m_iHashValue;
    }

    //
    // PHASE 6
    //
    //  For each entry give the hash sub-class a chance to perform any additional saving or fixups. We pass
    //  both the old and new address of each entry, plus the mapping table so they can map other entry
    //  addresses (if, for example, they have cross-entry pointer fields in their data).
    //
    //  We ask for each entry whether the saved data will be immutable. This is an optimization: if all
    //  entries turn out to be immutable we will save the entire entry array in a read-only (shareable)
    //  section.
    //

    bool fAllEntriesImmutable = true;
    for (i = 0; i < sEntryMap.m_cEntries; i++)
        if (!DOWNCALL(SaveEntry)(pImage, pProfileData, sEntryMap.m_pEntries[i].m_pOldEntry, sEntryMap.m_pEntries[i].m_pNewEntry, &sEntryMap))
            fAllEntriesImmutable = false;

    // We're mostly done. Now just some cleanup and the actual DataImage storage operations.

    // We don't need the bucket size tracking arrays any more.
    delete [] pHotBucketSizes;
    delete [] pColdBucketSizes;

    // If there are any hot entries store the entry array and bucket list.
    if (cHotEntries)
    {
        pImage->StoreStructure(m_sHotEntries.m_pEntries,
                               static_cast<ULONG>(sizeof(PersistedEntry) * cHotEntries), 
                               fAllEntriesImmutable ? DataImage::ITEM_NGEN_HASH_ENTRIES_RO_HOT : DataImage::ITEM_NGEN_HASH_ENTRIES_HOT);

        pImage->StoreStructure(m_sHotEntries.m_pBuckets,
                               static_cast<ULONG>(m_sHotEntries.m_pBuckets->GetSize(m_sHotEntries.m_cBuckets)),
                               DataImage::ITEM_NGEN_HASH_BUCKETLIST_HOT);
    }

    // If there are any cold entries store the entry array and bucket list.
    if (cColdEntries)
    {
        pImage->StoreStructure(m_sColdEntries.m_pEntries,
                               static_cast<ULONG>(sizeof(PersistedEntry) * cColdEntries), 
                               fAllEntriesImmutable ? DataImage::ITEM_NGEN_HASH_ENTRIES_RO_COLD : DataImage::ITEM_NGEN_HASH_ENTRIES_COLD);

        pImage->StoreStructure(m_sColdEntries.m_pBuckets,
                               static_cast<ULONG>(m_sColdEntries.m_pBuckets->GetSize(m_sColdEntries.m_cBuckets)),
                               DataImage::ITEM_NGEN_HASH_BUCKETLIST_COLD);
    }

    // Store the root data structure itself.
    pImage->StoreStructure(this, sizeof(FINAL_CLASS), cHotEntries ?
                           DataImage::ITEM_NGEN_HASH_HOT : DataImage::ITEM_NGEN_HASH_COLD);

    // We've moved the warm entries to hot and cold sections, so reset the warm section of the table. We only
    // do this on the copy of the table that's going to be saved into the ngen image. This is important since
    // (especially in the case of generics) we might continue to access this table throughout the rest of the
    // save/arrange/fixup process. Leaving two copies of saved entries in the table (hot or cold plus warm)
    // doesn't have any real impact, but removing the warm entries could be problematic where the entry was
    // culled from the ngen image. In those cases we'll get a miss on the lookup with the result that the
    // caller might try to add the type back to the table, something that is prohibited in the debug build
    // during the ngen save/arrange/fixup phases.

    // Reset the warm buckets to their original size or a fairly restrictive cap. These (empty) buckets will
    // be saved into the ngen image and form the basis for further entries added at runtime. Thus we have a
    // trade-off between storing dead space in the ngen image and having to re-size the bucket list at
    // runtime. Note that we can't save a zero sized bucket list: the invariant we have is that there are
    // always a non-zero number of buckets available when we come to do an insertion (since insertions cannot
    // fail). An alternative strategy would be to initialize these buckets at ngen image load time.
    _ASSERTE(m_cWarmBuckets >= m_cInitialBuckets);
    DWORD cNewWarmBuckets = min(m_cInitialBuckets, 11);

    // Create the ngen version of the warm buckets.
    pImage->StoreStructure(m_pWarmBuckets,
                           cNewWarmBuckets * sizeof(VolatileEntry*),
                           DataImage::ITEM_NGEN_HASH_HOT);

    // Reset the ngen-version of the table to have no warm entries and the reduced warm bucket count.
    NgenHashTable<NGEN_HASH_ARGS> *pNewTable = (NgenHashTable<NGEN_HASH_ARGS>*)pImage->GetImagePointer(this);
    pNewTable->m_cWarmEntries = 0;
    pNewTable->m_cWarmBuckets = cNewWarmBuckets;

    // Zero-out the ngen version of the warm buckets.
    VolatileEntry *pNewBuckets = (VolatileEntry*)pImage->GetImagePointer(m_pWarmBuckets);
    memset(pNewBuckets, 0, cNewWarmBuckets * sizeof(VolatileEntry*));
}

// Call during ngen to register fixups for hash table data structure fields. Calls derived-class
// implementation of FixupEntry to allow per-entry extension of the fixup process.
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::BaseFixup(DataImage *pImage)
{
    STANDARD_VM_CONTRACT;

    DWORD i;

    // Fixup the module pointer.
    pImage->FixupPointerField(this, offsetof(NgenHashTable<NGEN_HASH_ARGS>, m_pModule));

    // Throw away the heap pointer, we can't serialize it into the image. We'll rely on the loader heap
    // associated with the module above at runtime.
    pImage->ZeroPointerField(this, offsetof(NgenHashTable<NGEN_HASH_ARGS>, m_pHeap));

    // Give the hash sub-class a chance to fixup any pointers in its entries. We provide the pointer to the
    // hot or cold entry block and the offset into that block for this entry since we don't save individual
    // zap nodes for each entry; just a single node covering the entire array. As a result all fixups have to
    // be relative to the base of this array.

    for (i = 0; i < m_sHotEntries.m_cEntries; i++)
        DOWNCALL(FixupEntry)(pImage, &m_sHotEntries.m_pEntries[i].m_sValue, m_sHotEntries.m_pEntries, i * sizeof(PersistedEntry));

    for (i = 0; i < m_sColdEntries.m_cEntries; i++)
        DOWNCALL(FixupEntry)(pImage, &m_sColdEntries.m_pEntries[i].m_sValue, m_sColdEntries.m_pEntries, i * sizeof(PersistedEntry));

    // Fixup the warm (empty) bucket list.
    pImage->FixupPointerField(this, offsetof(NgenHashTable<NGEN_HASH_ARGS>, m_pWarmBuckets));

    // Fixup the hot entry array and bucket list.
    pImage->FixupPointerField(this,
                              offsetof(NgenHashTable<NGEN_HASH_ARGS>, m_sHotEntries) +
                              offsetof(PersistedEntries, m_pEntries));
    pImage->FixupPointerField(this,
                              offsetof(NgenHashTable<NGEN_HASH_ARGS>, m_sHotEntries) +
                              offsetof(PersistedEntries, m_pBuckets));

    // Fixup the cold entry array and bucket list.
    pImage->FixupPointerField(this,
                              offsetof(NgenHashTable<NGEN_HASH_ARGS>, m_sColdEntries) +
                              offsetof(PersistedEntries, m_pEntries));
    pImage->FixupPointerField(this,
                              offsetof(NgenHashTable<NGEN_HASH_ARGS>, m_sColdEntries) +
                              offsetof(PersistedEntries, m_pBuckets));
}
#endif // !DACCESS_COMPILE
#endif // FEATURE_PREJIT

#ifdef DACCESS_COMPILE

// Call during DAC enumeration of memory regions to save all hash table data structures. Calls derived-class
// implementation of EnumMemoryRegionsForEntry to allow additional per-entry memory to be reported.
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::BaseEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // Save the base data structure itself (can't use DAC_ENUM_DTHIS() since the size to save is based on a
    // sub-class).
    DacEnumMemoryRegion(dac_cast<TADDR>(this), sizeof(FINAL_CLASS));

    // Save the warm bucket list.
    DacEnumMemoryRegion(dac_cast<TADDR>(m_pWarmBuckets), m_cWarmBuckets * sizeof(VolatileEntry*));

    // Save all the warm entries.
    if (m_pWarmBuckets.IsValid())
    {
        for (DWORD i = 0; i < m_cWarmBuckets; i++)
        {
            PTR_VolatileEntry pEntry = m_pWarmBuckets[i];
            while (pEntry.IsValid())
            {
                pEntry.EnumMem();

                // Ask the sub-class whether each entry points to further data to be saved.
                DOWNCALL(EnumMemoryRegionsForEntry)(VALUE_FROM_VOLATILE_ENTRY(pEntry), flags);

                pEntry = pEntry->m_pNextEntry;
            }
        }
    }

#ifdef FEATURE_PREJIT
    // Save hot buckets and entries.
    if (m_sHotEntries.m_cEntries > 0)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(m_sHotEntries.m_pEntries), m_sHotEntries.m_cEntries * sizeof(PersistedEntry));
        DacEnumMemoryRegion(dac_cast<TADDR>(m_sHotEntries.m_pBuckets), m_sHotEntries.m_pBuckets->GetSize(m_sHotEntries.m_cBuckets));
        for (DWORD i = 0; i < m_sHotEntries.m_cEntries; i++)
            DOWNCALL(EnumMemoryRegionsForEntry)(VALUE_FROM_PERSISTED_ENTRY(dac_cast<PTR_PersistedEntry>(&m_sHotEntries.m_pEntries[i])), flags);
    }

    // Save cold buckets and entries.
    if (m_sColdEntries.m_cEntries > 0)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(m_sColdEntries.m_pEntries), m_sColdEntries.m_cEntries * sizeof(PersistedEntry));
        DacEnumMemoryRegion(dac_cast<TADDR>(m_sColdEntries.m_pBuckets), m_sColdEntries.m_pBuckets->GetSize(m_sColdEntries.m_cBuckets));
        for (DWORD i = 0; i < m_sColdEntries.m_cEntries; i++)
            DOWNCALL(EnumMemoryRegionsForEntry)(VALUE_FROM_PERSISTED_ENTRY(dac_cast<PTR_PersistedEntry>(&m_sColdEntries.m_pEntries[i])), flags);
    }
#endif // FEATURE_PREJIT

    // Save the module if present.
    if (m_pModule.IsValid())
        m_pModule->EnumMemoryRegions(flags, true);
}
#endif // DACCESS_COMPILE

#ifdef FEATURE_PREJIT

// Find the first persisted entry (hot or cold based on pEntries) that matches the given hash. Looks only in
// the persisted block given (i.e. searches only hot *or* cold entries). Returns NULL on failure. Otherwise
// returns pointer to the derived class portion of the entry and initializes the provided LookupContext to
// allow enumeration of any further matches.
template <NGEN_HASH_PARAMS>
DPTR(VALUE) NgenHashTable<NGEN_HASH_ARGS>::FindPersistedEntryByHash(PersistedEntries *pEntries, NgenHashValue iHash, LookupContext *pContext)
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
    if (pEntries->m_cEntries == 0)
        return NULL;

    // Since there is at least one entry there must be at least one bucket.
    _ASSERTE(pEntries->m_cBuckets > 0);

    // Get the first entry and count of entries for the bucket which contains all entries with the given hash
    // code.
    DWORD dwEntryIndex, cEntriesLeft;
    pEntries->m_pBuckets->GetBucket(iHash % pEntries->m_cBuckets, &dwEntryIndex, &cEntriesLeft);

    // Determine the address of the first entry in the chain by indexing into the entry array.
    PTR_PersistedEntry pEntry = dac_cast<PTR_PersistedEntry>(&pEntries->m_pEntries[dwEntryIndex]);

    // Iterate while we've still got entries left to check in this chain.
    while (cEntriesLeft--)
    {
        if (pEntry->m_iHashValue == iHash)
        {
            // We've found our match.

            // Record our current search state into the provided context so that a subsequent call to
            // BaseFindNextEntryByHash can pick up the search where it left off.
            pContext->m_pEntry = dac_cast<TADDR>(pEntry);
            pContext->m_eType = pEntries == &m_sHotEntries ? Hot : Cold;
            pContext->m_cRemainingEntries = cEntriesLeft;

            // Return the address of the sub-classes' embedded entry structure.
            return VALUE_FROM_PERSISTED_ENTRY(pEntry);
        }

        // Move to the next entry in the chain.
        pEntry++;
    }

    // If we get here then none of the entries in the target bucket matched the hash code and we have a miss
    // (for this section of the table at least).
    return NULL;
}

#ifndef DACCESS_COMPILE
template <NGEN_HASH_PARAMS>
NgenHashTable<NGEN_HASH_ARGS>::EntryMappingTable::~EntryMappingTable()
{
    LIMITED_METHOD_CONTRACT;

    delete [] m_pEntries;
}

// Given an old entry address (pre-BaseSave) return the address of the entry relocated ready for saving to
// disk. Note that this address is the (ngen) runtime address, not the disk image address you can further
// obtain by calling DataImage::GetImagePointer().
template <NGEN_HASH_PARAMS>
VALUE *NgenHashTable<NGEN_HASH_ARGS>::EntryMappingTable::GetNewEntryAddress(VALUE *pOldEntry)
{
    LIMITED_METHOD_CONTRACT;

    // Perform a simple linear search. If this proves to be a bottleneck in ngen production (the only scenario
    // in which it's called) we can replace this with something faster such as a hash lookup.
    for (DWORD i = 0; i < m_cEntries; i++)
        if (m_pEntries[i].m_pOldEntry == pOldEntry)
            return m_pEntries[i].m_pNewEntry;

    _ASSERTE(!"Couldn't map old hash entry to new entry");
    return NULL;
}
#endif // !DACCESS_COMPILE
#endif // FEATURE_PREJIT

// Find the first volatile (warm) entry that matches the given hash. Looks only at warm entries. Returns NULL
// on failure. Otherwise returns pointer to the derived class portion of the entry and initializes the
// provided LookupContext to allow enumeration of any further matches.
template <NGEN_HASH_PARAMS>
DPTR(VALUE) NgenHashTable<NGEN_HASH_ARGS>::FindVolatileEntryByHash(NgenHashValue iHash, LookupContext *pContext)
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
    if (m_cWarmEntries == 0)
        return NULL;

    // Since there is at least one entry there must be at least one bucket.
    _ASSERTE(m_cWarmBuckets > 0);

    // Point at the first entry in the bucket chain which would contain any entries with the given hash code.
    PTR_VolatileEntry pEntry = m_pWarmBuckets[iHash % m_cWarmBuckets];

    // Walk the bucket chain one entry at a time.
    while (pEntry)
    {
        if (pEntry->m_iHashValue == iHash)
        {
            // We've found our match.

            // Record our current search state into the provided context so that a subsequent call to
            // BaseFindNextEntryByHash can pick up the search where it left off.
            pContext->m_pEntry = dac_cast<TADDR>(pEntry);
            pContext->m_eType = Warm;

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

// Initializes the iterator context passed by the caller to make it ready to walk every entry in the table in
// an arbitrary order. Call pIterator->Next() to retrieve the first entry.
template <NGEN_HASH_PARAMS>
void NgenHashTable<NGEN_HASH_ARGS>::BaseInitIterator(BaseIterator *pIterator)
{
    LIMITED_METHOD_DAC_CONTRACT;

    pIterator->m_pTable = this;
    pIterator->m_pEntry = NULL;
#ifdef FEATURE_PREJIT
    pIterator->m_eType = Hot;
    pIterator->m_cRemainingEntries = m_sHotEntries.m_cEntries;
#else
    pIterator->m_eType = Warm;
    pIterator->m_dwBucket = 0;
#endif
}

// Returns a pointer to the next entry in the hash table or NULL once all entries have been enumerated. Once
// NULL has been return the only legal operation is to re-initialize the iterator with BaseInitIterator.
template <NGEN_HASH_PARAMS>
DPTR(VALUE) NgenHashTable<NGEN_HASH_ARGS>::BaseIterator::Next()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // We might need to re-iterate our algorithm if we fall off the end of one hash table section (Hot or
    // Warm) and need to move onto the next.
    while (true)
    {
        // What type of section are we walking (Hot, Warm or Cold)?
        switch (m_eType)
        {
#ifdef FEATURE_PREJIT
        case Hot:
        {
            if (m_cRemainingEntries)
            {
                // There's at least one more entry in the hot section to report.

                if (m_pEntry == NULL)
                {
                    // This is our first lookup in the hot section, return the first entry in the hot array.
                    m_pEntry = dac_cast<TADDR>(m_pTable->m_sHotEntries.m_pEntries);
                }
                else
                {
                    // This is not our first lookup, return the entry immediately after the last one we
                    // reported.
                    m_pEntry = (TADDR)(m_pEntry + sizeof(PersistedEntry));
                }

                // There's one less entry to report in the future.
                m_cRemainingEntries--;

                // Return the pointer to the embedded sub-class entry in the entry we found.
                return VALUE_FROM_PERSISTED_ENTRY(dac_cast<PTR_PersistedEntry>(m_pEntry));
            }

            // We ran out of hot entries. Set up to search the warm section next and go round the loop again.
            m_eType = Warm;
            m_pEntry = NULL;
            m_dwBucket = 0;
            break;
        }
#endif // FEATURE_PREJIT

        case Warm:
        {
            if (m_pEntry == NULL)
            {
                // This is our first lookup in the warm section for a particular bucket, return the first
                // entry in that bucket.
                m_pEntry = dac_cast<TADDR>(m_pTable->m_pWarmBuckets[m_dwBucket]);
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
            if (m_dwBucket < m_pTable->m_cWarmBuckets)
                break;

            // Othwerwise we should move onto the cold section (if we have one).

#ifdef FEATURE_PREJIT
            m_eType = Cold;
            m_pEntry = NULL;
            m_cRemainingEntries = m_pTable->m_sColdEntries.m_cEntries;
            break;
#else
            return NULL;
#endif // FEATURE_PREJIT
        }

#ifdef FEATURE_PREJIT
        case Cold:
        {
            if (m_cRemainingEntries)
            {
                // There's at least one more entry in the cold section to report.

                if (m_pEntry == NULL)
                {
                    // This is our first lookup in the cold section, return the first entry in the cold array.
                    m_pEntry = dac_cast<TADDR>(m_pTable->m_sColdEntries.m_pEntries);
                }
                else
                {
                    // This is not our first lookup, return the entry immediately after the last one we
                    // reported.
                    m_pEntry = (TADDR)(m_pEntry + sizeof(PersistedEntry));
                }

                // There's one less entry to report in the future.
                m_cRemainingEntries--;

                // Return the pointer to the embedded sub-class entry in the entry we found.
                return VALUE_FROM_PERSISTED_ENTRY(dac_cast<PTR_PersistedEntry>(m_pEntry));
            }

            // If there are no more entries in the cold section that's it, the whole table has been scanned.
            return NULL;
        }
#endif // FEATURE_PREJIT

        default:
            _ASSERTE(!"Invalid hash entry type");
        }
    }
}

// Get a pointer to the referenced entry.
template <NGEN_HASH_PARAMS>
DPTR(VALUE) NgenHashEntryRef<NGEN_HASH_ARGS>::Get()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Short-cut the NULL case, it's a lot cheaper than the code below when compiling for DAC.
    if (m_rpEntryRef.IsNull())
        return NULL;

    // Note that the following code uses a special DAC lookup for an interior pointer (i.e. "this" isn't a
    // host address corresponding to a DAC marshalled instance, it's some host address within such an
    // instance). These lookups are a little slower than the regular kind since we have to search for the
    // containing instance.

    // @todo: The following causes gcc to choke on Mac 10.4 at least (complains that offsetof is being passed
    // four arguments instead of two). Expanding the top-level macro manually fixes this.
    // TADDR pBase = PTR_HOST_INT_MEMBER_TADDR(NgenHashEntryRef<NGEN_HASH_ARGS>, this, m_rpEntryRef);
    TADDR pBase = PTR_HOST_INT_TO_TADDR(this) + (TADDR)offsetof(NgenHashEntryRef<NGEN_HASH_ARGS>, m_rpEntryRef);

    return m_rpEntryRef.GetValue(pBase);
}

#ifndef DACCESS_COMPILE

// Set the reference to point to the given entry.
template <NGEN_HASH_PARAMS>
void NgenHashEntryRef<NGEN_HASH_ARGS>::Set(VALUE *pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_rpEntryRef.SetValueMaybeNull(pEntry);
}

#ifdef FEATURE_PREJIT

// Call this during the ngen Fixup phase to adjust the relative pointer to account for ngen image layout.
template <NGEN_HASH_PARAMS>
void NgenHashEntryRef<NGEN_HASH_ARGS>::Fixup(DataImage *pImage, NgenHashTable<NGEN_HASH_ARGS> *pTable)
{
    STANDARD_VM_CONTRACT;

    // No fixup required for null pointers.
    if (m_rpEntryRef.IsNull())
        return;

    // Location is the field containing the entry reference. We need to determine the ngen zap node that
    // contains this field (it'll be part of either the hot or cold entry arrays). Then we can determine the
    // offset of the field from the beginning of the node.
    BYTE *pLocation = (BYTE*)&m_rpEntryRef;
    BYTE *pLocationBase;
    DWORD cbLocationOffset;

    if (pLocation >= (BYTE*)pTable->m_sHotEntries.m_pEntries &&
        pLocation < (BYTE*)(pTable->m_sHotEntries.m_pEntries + pTable->m_sHotEntries.m_cEntries))
    {
        // The field is in a hot entry.
        pLocationBase = (BYTE*)pTable->m_sHotEntries.m_pEntries;
    }
    else if (pLocation >= (BYTE*)pTable->m_sColdEntries.m_pEntries &&
             pLocation < (BYTE*)(pTable->m_sColdEntries.m_pEntries + pTable->m_sColdEntries.m_cEntries))
    {
        // The field is in a cold entry.
        pLocationBase = (BYTE*)pTable->m_sColdEntries.m_pEntries;
    }
    else
    {
        // The field doesn't lie in one of the entry arrays. The caller has passed us an NgenHashEntryRef that
        // wasn't embedded as a field in one of this hash's entries.
        _ASSERTE(!"NgenHashEntryRef must be a field in an NgenHashTable entry for Fixup to work");
        return;
    }
    cbLocationOffset = static_cast<DWORD>(pLocation - pLocationBase);

    // Target is the address of the entry that this reference points to. Go through the same kind of logic to
    // determine which section the target entry lives in, hot or cold.
    BYTE *pTarget = (BYTE*)m_rpEntryRef.GetValue();
    BYTE *pTargetBase;
    DWORD cbTargetOffset;

    if (pTarget >= (BYTE*)pTable->m_sHotEntries.m_pEntries &&
        pTarget < (BYTE*)(pTable->m_sHotEntries.m_pEntries + pTable->m_sHotEntries.m_cEntries))
    {
        // The target is a hot entry.
        pTargetBase = (BYTE*)pTable->m_sHotEntries.m_pEntries;
    }
    else if (pTarget >= (BYTE*)pTable->m_sColdEntries.m_pEntries &&
             pTarget < (BYTE*)(pTable->m_sColdEntries.m_pEntries + pTable->m_sColdEntries.m_cEntries))
    {
        // The target is a cold entry.
        pTargetBase = (BYTE*)pTable->m_sColdEntries.m_pEntries;
    }
    else
    {
        // The target doesn't lie in one of the entry arrays. The caller has passed us an NgenHashEntryRef that
        // points to an entry (or other memory) not in our hash table.
        _ASSERTE(!"NgenHashEntryRef must refer to an entry in the same hash table");
        return;
    }
    cbTargetOffset = static_cast<DWORD>(pTarget - pTargetBase);

    // Now we have enough data to ask for a fixup to be generated for this field. The fixup type
    // IMAGE_REL_BASED_RELPTR means we won't actually get a base relocation fixup (an entry in the ngen image
    // that causes a load-time fixup to be applied). Instead this record will just adjust the relative value
    // in the field once the ngen image layout is finalized and it knows the final locations of the field and
    // target zap nodes.
    pImage->FixupField(pLocationBase, cbLocationOffset, pTargetBase, cbTargetOffset, IMAGE_REL_BASED_RELPTR);
}
#endif // FEATURE_PREJIT
#endif // !DACCESS_COMPILE
