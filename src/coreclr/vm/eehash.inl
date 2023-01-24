// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


#ifndef _EE_HASH_INL
#define _EE_HASH_INL

#ifdef _DEBUG_IMPL
template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::OwnLock()
{
    WRAPPER_NO_CONTRACT;
    if (m_CheckThreadSafety == FALSE)
        return TRUE;

    if (m_pfnLockOwner == NULL) {
        return m_writerThreadId.IsCurrentThread();
    }
    else {
        BOOL ret = m_pfnLockOwner(m_lockData);
        if (!ret) {
            if (Debug_IsLockedViaThreadSuspension()) {
                ret = TRUE;
            }
        }
        return ret;
    }
}
#endif  // _DEBUG_IMPL

#ifndef DACCESS_COMPILE
template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
void EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::Destroy()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    if (m_pVolatileBucketTable && m_pVolatileBucketTable->m_pBuckets != NULL)
    {
        DWORD i;

        for (i = 0; i < m_pVolatileBucketTable->m_dwNumBuckets; i++)
        {
            EEHashEntry_t *pEntry, *pNext;

            for (pEntry = m_pVolatileBucketTable->m_pBuckets[i]; pEntry != NULL; pEntry = pNext)
            {
                pNext = pEntry->pNext;
                Helper::DeleteEntry(pEntry, m_Heap);
            }
        }

        delete[] (m_pVolatileBucketTable->m_pBuckets-1);

		m_pVolatileBucketTable = NULL;
    }

}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
void EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::ClearHashTable()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    //_ASSERTE (OwnLock());

    // Transition to COOP mode. This is need because EEHashTable is lock free and it can be read
    // from multiple threads without taking locks. On rehash, you want to get rid of the old copy
    // of table. You can only get rid of it once nobody is using it. That's a problem because
    // there is no lock to tell when the last reader stopped using the old copy of the table.
    // The solution to this problem is to access the table in cooperative mode, and to get rid of
    // the old copy of the table when we are suspended for GC. When we are suspended for GC,
    // we know that nobody is using the old copy of the table anymore.
    // BROKEN: This is called sometimes from the CorMap hash before the EE is started up
    GCX_COOP_NO_THREAD_BROKEN();

    if (m_pVolatileBucketTable->m_pBuckets != NULL)
    {
        DWORD i;

        for (i = 0; i < m_pVolatileBucketTable->m_dwNumBuckets; i++)
        {
            EEHashEntry_t *pEntry, *pNext;

            for (pEntry = m_pVolatileBucketTable->m_pBuckets[i]; pEntry != NULL; pEntry = pNext)
            {
                pNext = pEntry->pNext;
                Helper::DeleteEntry(pEntry, m_Heap);
            }
        }

        delete[] (m_pVolatileBucketTable->m_pBuckets-1);
        m_pVolatileBucketTable->m_pBuckets = NULL;
    }

    m_pVolatileBucketTable->m_dwNumBuckets = 0;
#ifdef TARGET_64BIT
    m_pVolatileBucketTable->m_dwNumBucketsMul = 0;
#endif
    m_dwNumEntries = 0;
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
void EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::EmptyHashTable()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE (OwnLock());

    // Transition to COOP mode. This is need because EEHashTable is lock free and it can be read
    // from multiple threads without taking locks. On rehash, you want to get rid of the old copy
    // of table. You can only get rid of it once nobody is using it. That's a problem because
    // there is no lock to tell when the last reader stopped using the old copy of the table.
    // The solution to this problem is to access the table in cooperative mode, and to get rid of
    // the old copy of the table when we are suspended for GC. When we are suspended for GC,
    // we know that nobody is using the old copy of the table anymore.
    // BROKEN: This is called sometimes from the CorMap hash before the EE is started up
    GCX_COOP_NO_THREAD_BROKEN();

    if (m_pVolatileBucketTable->m_pBuckets != NULL)
    {
        DWORD i;

        for (i = 0; i < m_pVolatileBucketTable->m_dwNumBuckets; i++)
        {
            EEHashEntry_t *pEntry, *pNext;

            for (pEntry = m_pVolatileBucketTable->m_pBuckets[i]; pEntry != NULL; pEntry = pNext)
            {
                pNext = pEntry->pNext;
                Helper::DeleteEntry(pEntry, m_Heap);
            }

            m_pVolatileBucketTable->m_pBuckets[i] = NULL;
        }
    }

    m_dwNumEntries = 0;
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>

BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::Init(DWORD dwNumBuckets, LockOwner *pLock, AllocationHeap pHeap, BOOL CheckThreadSafety)
{
    CONTRACTL
    {
        WRAPPER(NOTHROW);
        WRAPPER(GC_NOTRIGGER);
        INJECT_FAULT(return FALSE;);

#ifndef DACCESS_COMPILE
        PRECONDITION(m_pVolatileBucketTable.Load() == NULL && "EEHashTable::Init() called twice.");
#endif

    }
    CONTRACTL_END

    m_pVolatileBucketTable = &m_BucketTable[0];

    DWORD dwNumBucketsPlusOne;

    // Prefast overflow sanity check the addition
    if (!ClrSafeInt<DWORD>::addition(dwNumBuckets, 1, dwNumBucketsPlusOne))
        return FALSE;

    S_SIZE_T safeSize(sizeof(EEHashEntry_t *));
    safeSize *= dwNumBucketsPlusOne;
    if (safeSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);
    SIZE_T cbAlloc = safeSize.Value();

    m_pVolatileBucketTable->m_pBuckets = (EEHashEntry_t **) new (nothrow) BYTE[cbAlloc];

    if (m_pVolatileBucketTable->m_pBuckets == NULL)
        return FALSE;

    memset(m_pVolatileBucketTable->m_pBuckets, 0, cbAlloc);

    // The first slot links to the next list.
    m_pVolatileBucketTable->m_pBuckets++;
    m_pVolatileBucketTable->m_dwNumBuckets = dwNumBuckets;
#ifdef TARGET_64BIT
    m_pVolatileBucketTable->m_dwNumBucketsMul = dwNumBuckets == 0 ? 0 : GetFastModMultiplier(dwNumBuckets);
#endif

    m_Heap = pHeap;

#ifdef _DEBUG
    if (pLock == NULL) {
        m_lockData = NULL;
        m_pfnLockOwner = NULL;
    }
    else {
        m_lockData = pLock->lock;
        m_pfnLockOwner = pLock->lockOwnerFunc;
    }

    if (m_pfnLockOwner == NULL) {
        m_writerThreadId.SetToCurrentThread();
    }
    m_CheckThreadSafety = CheckThreadSafety;
#endif

    return TRUE;
}


// Does not handle duplicates!

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
void EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::InsertValue(KeyType pKey, HashDatum Data, BOOL bDeepCopyKey)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    _ASSERTE (OwnLock());

    // Transition to COOP mode. This is need because EEHashTable is lock free and it can be read
    // from multiple threads without taking locks. On rehash, you want to get rid of the old copy
    // of table. You can only get rid of it once nobody is using it. That's a problem because
    // there is no lock to tell when the last reader stopped using the old copy of the table.
    // The solution to this problem is to access the table in cooperative mode, and to get rid of
    // the old copy of the table when we are suspended for GC. When we are suspended for GC,
    // we know that nobody is using the old copy of the table anymore.
    // BROKEN: This is called sometimes from the CorMap hash before the EE is started up
    GCX_COOP_NO_THREAD_BROKEN();

    _ASSERTE(m_pVolatileBucketTable->m_dwNumBuckets != 0);

    if  (m_dwNumEntries > m_pVolatileBucketTable->m_dwNumBuckets*2)
    {
        if (!GrowHashTable()) COMPlusThrowOM();
    }

    DWORD dwHash = (DWORD)Helper::Hash(pKey);
    DWORD dwBucket = dwHash % m_pVolatileBucketTable->m_dwNumBuckets;
    EEHashEntry_t * pNewEntry;

    pNewEntry = Helper::AllocateEntry(pKey, bDeepCopyKey, m_Heap);
    if (!pNewEntry)
    {
        COMPlusThrowOM();
    }

    // Fill in the information for the new entry.
    pNewEntry->pNext        = m_pVolatileBucketTable->m_pBuckets[dwBucket];
    pNewEntry->Data         = Data;
    pNewEntry->dwHashValue  = dwHash;

    // Insert at head of bucket
    // need volatile write to avoid write reordering problem in IA
    VolatileStore(&m_pVolatileBucketTable->m_pBuckets[dwBucket], pNewEntry);;

    m_dwNumEntries++;
}


// Similar to the above, except that the HashDatum is a pointer to key.
template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
void EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::InsertKeyAsValue(KeyType pKey, BOOL bDeepCopyKey)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    _ASSERTE (OwnLock());

    // Transition to COOP mode. This is need because EEHashTable is lock free and it can be read
    // from multiple threads without taking locks. On rehash, you want to get rid of the old copy
    // of table. You can only get rid of it once nobody is using it. That's a problem because
    // there is no lock to tell when the last reader stopped using the old copy of the table.
    // The solution to this problem is to access the table in cooperative mode, and to get rid of
    // the old copy of the table when we are suspended for GC. When we are suspended for GC,
    // we know that nobody is using the old copy of the table anymore.
    // BROKEN: This is called sometimes from the CorMap hash before the EE is started up
    GCX_COOP_NO_THREAD_BROKEN();

    _ASSERTE(m_pVolatileBucketTable->m_dwNumBuckets != 0);

    if  (m_dwNumEntries > m_pVolatileBucketTable->m_dwNumBuckets*2)
    {
        if (!GrowHashTable()) COMPlusThrowOM();
    }

    DWORD           dwHash = Helper::Hash(pKey);
    DWORD           dwBucket = dwHash % m_pVolatileBucketTable->m_dwNumBuckets;
    EEHashEntry_t * pNewEntry;

    pNewEntry = Helper::AllocateEntry(pKey, bDeepCopyKey, m_Heap);
    if (!pNewEntry)
    {
        COMPlusThrowOM();
    }

    // Fill in the information for the new entry.
    pNewEntry->pNext        = m_pVolatileBucketTable->m_pBuckets[dwBucket];
    pNewEntry->dwHashValue  = dwHash;
    pNewEntry->Data         = *((LPUTF8 *)pNewEntry->Key);

    // Insert at head of bucket
    // need volatile write to avoid write reordering problem in IA
    VolatileStore(&m_pVolatileBucketTable->m_pBuckets[dwBucket], pNewEntry);

    m_dwNumEntries++;
}


template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::DeleteValue(KeyType pKey)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE (OwnLock());

    Thread *pThread = GetThreadNULLOk();
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(pThread ? !(pThread->m_StateNC & Thread::TSNC_UnsafeSkipEnterCooperative) : FALSE);

    _ASSERTE(m_pVolatileBucketTable->m_dwNumBuckets != 0);

    DWORD           dwHash = Helper::Hash(pKey);
    DWORD           dwBucket = dwHash % m_pVolatileBucketTable->m_dwNumBuckets;
    EEHashEntry_t * pSearch;
    EEHashEntry_t **ppPrev = &m_pVolatileBucketTable->m_pBuckets[dwBucket];

    for (pSearch = m_pVolatileBucketTable->m_pBuckets[dwBucket]; pSearch; pSearch = pSearch->pNext)
    {
        if (pSearch->dwHashValue == dwHash && Helper::CompareKeys(pSearch, pKey))
        {
            *ppPrev = pSearch->pNext;
            Helper::DeleteEntry(pSearch, m_Heap);

            // Do we ever want to shrink?
            m_dwNumEntries--;

            return TRUE;
        }

        ppPrev = &pSearch->pNext;
    }

    return FALSE;
}


template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::ReplaceValue(KeyType pKey, HashDatum Data)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE (OwnLock());

    EEHashEntry_t *pItem = FindItem(pKey);

    if (pItem != NULL)
    {
        // Required to be atomic
        pItem->Data = Data;
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}


template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::ReplaceKey(KeyType pOldKey, KeyType pNewKey)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE (OwnLock());

    EEHashEntry_t *pItem = FindItem(pOldKey);

    if (pItem != NULL)
    {
        Helper::ReplaceKey (pItem, pNewKey);
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}
#endif // !DACCESS_COMPILE

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
DWORD EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::GetHash(KeyType pKey)
{
    WRAPPER_NO_CONTRACT;
    return Helper::Hash(pKey);
}


template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::GetValue(KeyType pKey, HashDatum *pData)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    EEHashEntry_t *pItem = FindItem(pKey);

    if (pItem != NULL)
    {
        *pData = pItem->Data;
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::GetValue(KeyType pKey, HashDatum *pData, DWORD hashValue)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    EEHashEntry_t *pItem = FindItem(pKey, hashValue);

    if (pItem != NULL)
    {
        *pData = pItem->Data;
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
FORCEINLINE BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::GetValueSpeculative(KeyType pKey, HashDatum *pData)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
#ifdef MODE_COOPERATIVE     // This header file sees contract.h, not eecontract.h - what a kludge!
        MODE_COOPERATIVE;
#endif
    }
    CONTRACTL_END

    EEHashEntry_t *pItem = FindItemSpeculative(pKey, Helper::Hash(pKey));

    if (pItem != NULL)
    {
        *pData = pItem->Data;
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
FORCEINLINE BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::GetValueSpeculative(KeyType pKey, HashDatum *pData, DWORD hashValue)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
#ifdef MODE_COOPERATIVE     // This header file sees contract.h, not eecontract.h - what a kludge!
        MODE_COOPERATIVE;
#endif
    }
    CONTRACTL_END

    EEHashEntry_t *pItem = FindItemSpeculative(pKey, hashValue);

    if (pItem != NULL)
    {
        *pData = pItem->Data;
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
EEHashEntry_t *EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::FindItem(KeyType pKey)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    return FindItem(pKey, Helper::Hash(pKey));
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
EEHashEntry_t *EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::FindItem(KeyType pKey, DWORD dwHash)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    // Transition to COOP mode. This is need because EEHashTable is lock free and it can be read
    // from multiple threads without taking locks. On rehash, you want to get rid of the old copy
    // of table. You can only get rid of it once nobody is using it. That's a problem because
    // there is no lock to tell when the last reader stopped using the old copy of the table.
    // The solution to this problem is to access the table in cooperative mode, and to get rid of
    // the old copy of the table when we are suspended for GC. When we are suspended for GC,
    // we know that nobody is using the old copy of the table anymore.
    //
#ifndef DACCESS_COMPILE
   GCX_COOP_NO_THREAD_BROKEN();
#endif

    // Atomic transaction. In any other point of this method or ANY of the callees of this function you can not read
    // from m_pVolatileBucketTable!!!!!!! A racing condition would occur.
    DWORD dwOldNumBuckets;

#ifndef DACCESS_COMPILE
    DWORD nTry = 0;
    DWORD dwSwitchCount = 0;
#endif

    do
    {
        BucketTable* pBucketTable=(BucketTable*)(PTR_BucketTable)m_pVolatileBucketTable.Load();
        dwOldNumBuckets = pBucketTable->m_dwNumBuckets;

        _ASSERTE(pBucketTable->m_dwNumBuckets != 0);

        DWORD           dwBucket = dwHash % pBucketTable->m_dwNumBuckets;
        EEHashEntry_t * pSearch;

        for (pSearch = pBucketTable->m_pBuckets[dwBucket]; pSearch; pSearch = pSearch->pNext)
        {
            if (pSearch->dwHashValue == dwHash && Helper::CompareKeys(pSearch, pKey))
                return pSearch;
        }

        // There is a race in EEHash Table: when we grow the hash table, we will nuke out
        // the old bucket table. Readers might be looking up in the old table, they can
        // fail to find an existing entry. The workaround is to retry the search process
        // if we are called grow table during the search process.
#ifndef DACCESS_COMPILE
        nTry ++;
        if (nTry == 20) {
            __SwitchToThread(0, ++dwSwitchCount);
            nTry = 0;
        }
#endif // #ifndef DACCESS_COMPILE
    }
    while ( m_bGrowing || dwOldNumBuckets != m_pVolatileBucketTable->m_dwNumBuckets);

    return NULL;
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
FORCEINLINE EEHashEntry_t *EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::FindItemSpeculative(KeyType pKey, DWORD dwHash)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
#ifdef MODE_COOPERATIVE     // This header file sees contract.h, not eecontract.h - what a kludge!
        MODE_COOPERATIVE;
#endif
    }
    CONTRACTL_END

    // Atomic transaction. In any other point of this method or ANY of the callees of this function you can not read
    // from m_pVolatileBucketTable!!!!!!! A racing condition would occur.
    DWORD dwOldNumBuckets;

    BucketTable* pBucketTable=m_pVolatileBucketTable;
    dwOldNumBuckets = pBucketTable->m_dwNumBuckets;

    _ASSERTE(pBucketTable->m_dwNumBuckets != 0);

    DWORD dwBucket;
#ifdef TARGET_64BIT
    _ASSERTE(pBucketTable->m_dwNumBucketsMul != 0);
    dwBucket = FastMod(dwHash, pBucketTable->m_dwNumBuckets, pBucketTable->m_dwNumBucketsMul);
#else
    dwBucket = dwHash % pBucketTable->m_dwNumBuckets;
#endif
    EEHashEntry_t * pSearch;

    for (pSearch = pBucketTable->m_pBuckets[dwBucket]; pSearch; pSearch = pSearch->pNext)
    {
        if (pSearch->dwHashValue == dwHash && Helper::CompareKeys(pSearch, pKey))
            return pSearch;
    }

    return NULL;
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::IsEmpty()
{
    LIMITED_METHOD_CONTRACT;
    return m_dwNumEntries == 0;
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
DWORD EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::GetCount()
{
    LIMITED_METHOD_CONTRACT;
    return m_dwNumEntries;
}

#ifndef DACCESS_COMPILE

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::GrowHashTable()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END

#if defined(_DEBUG)
    Thread * pThread = GetThreadNULLOk();
    _ASSERTE(!g_fEEStarted || (pThread == NULL) || (pThread->PreemptiveGCDisabled()));
#endif

    // Make the new bucket table 4 times bigger
    //
    DWORD dwNewNumBuckets;
    DWORD dwNewNumBucketsPlusOne;
    {
        S_UINT32 safeSize(m_pVolatileBucketTable->m_dwNumBuckets);

        safeSize *= 4;

        if (safeSize.IsOverflow())
            return FALSE;

        dwNewNumBuckets = safeSize.Value();

        safeSize += 1;  // Allocate one extra

        if (safeSize.IsOverflow())
            return FALSE;

        dwNewNumBucketsPlusOne = safeSize.Value();
    }

    // On resizes, we still have an array of old pointers we need to worry about.
    // We can't free these old pointers, for we may hit a race condition where we're
    // resizing and reading from the array at the same time. We need to keep track of these
    // old arrays of pointers, so we're going to use the last item in the array to "link"
    // to previous arrays, so that they may be freed at the end.
    //

    SIZE_T cbAlloc;
    {
        S_SIZE_T safeSize(sizeof(EEHashEntry_t *));

        safeSize *= dwNewNumBucketsPlusOne;

        if (safeSize.IsOverflow())
            return FALSE;

        cbAlloc = safeSize.Value();
    }

    EEHashEntry_t **pNewBuckets = (EEHashEntry_t **) new (nothrow) BYTE[cbAlloc];

    if (pNewBuckets == NULL)
        return FALSE;

    memset(pNewBuckets, 0, cbAlloc);

    // The first slot is linked to next list.
    pNewBuckets++;

    // Run through the old table and transfer all the entries

    // Be sure not to mess with the integrity of the old table while
    // we are doing this, as there can be concurrent readers!  Note that
    // it is OK if the concurrent reader misses out on a match, though -
    // they will have to acquire the lock on a miss & try again.
    InterlockedExchange( (LONG *) &m_bGrowing, 1);
    for (DWORD i = 0; i < m_pVolatileBucketTable->m_dwNumBuckets; i++)
    {
        EEHashEntry_t * pEntry = m_pVolatileBucketTable->m_pBuckets[i];

        // Try to lock out readers from scanning this bucket.  This is
        // obviously a race which may fail. However, note that it's OK
        // if somebody is already in the list - it's OK if we mess
        // with the bucket groups, as long as we don't destroy
        // anything.  The lookup function will still do appropriate
        // comparison even if it wanders aimlessly amongst entries
        // while we are rearranging things.  If a lookup finds a match
        // under those circumstances, great.  If not, they will have
        // to acquire the lock & try again anyway.

        m_pVolatileBucketTable->m_pBuckets[i] = NULL;

        while (pEntry != NULL)
        {
            DWORD           dwNewBucket = pEntry->dwHashValue % dwNewNumBuckets;
            EEHashEntry_t * pNextEntry   = pEntry->pNext;

            pEntry->pNext = pNewBuckets[dwNewBucket];
            pNewBuckets[dwNewBucket] = pEntry;
            pEntry = pNextEntry;
        }
    }


    // Finally, store the new number of buckets and the new bucket table
    BucketTable* pNewBucketTable = (m_pVolatileBucketTable == &m_BucketTable[0]) ?
                    &m_BucketTable[1]:
                    &m_BucketTable[0];

    pNewBucketTable->m_pBuckets = pNewBuckets;
    pNewBucketTable->m_dwNumBuckets = dwNewNumBuckets;
#ifdef TARGET_64BIT
    pNewBucketTable->m_dwNumBucketsMul = dwNewNumBuckets == 0 ? 0 : GetFastModMultiplier(dwNewNumBuckets);
#endif

    // Add old table to the to free list. Note that the SyncClean thing will only
    // delete the buckets at a safe point
    //
    SyncClean::AddEEHashTable (m_pVolatileBucketTable->m_pBuckets);

    // Note that the SyncClean:AddEEHashTable performs at least one Interlock operation
    // So we do not need to use an Interlocked operation to write m_pVolatileBucketTable
    // Swap the double buffer, this is an atomic operation (the assignment)
    //
    m_pVolatileBucketTable = pNewBucketTable;

    InterlockedExchange( (LONG *) &m_bGrowing, 0);

    return TRUE;
}

#endif // DACCESS_COMPILE

// Walk through all the entries in the hash table, in meaningless order, without any
// synchronization.
//
//           IterateStart()
//           while (IterateNext())
//              GetKey();
//
template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
void EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::
            IterateStart(EEHashTableIteration *pIter)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE_IMPL(OwnLock());
    pIter->m_dwBucket = -1;
    pIter->m_pEntry = NULL;

#ifdef _DEBUG
    pIter->m_pTable = this;
#endif
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
BOOL EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::
            IterateNext(EEHashTableIteration *pIter)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE_IMPL(OwnLock());

    Thread *pThread = GetThreadNULLOk();
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(pThread ? !(pThread->m_StateNC & Thread::TSNC_UnsafeSkipEnterCooperative) : FALSE);

    _ASSERTE(pIter->m_pTable == (void *) this);

    // If we haven't started iterating yet, or if we are at the end of a particular
    // chain, advance to the next chain.
    while (pIter->m_pEntry == NULL || pIter->m_pEntry->pNext == NULL)
    {
        if (++pIter->m_dwBucket >= m_pVolatileBucketTable->m_dwNumBuckets)
        {
            // advanced beyond the end of the table.
            _ASSERTE(pIter->m_dwBucket == m_pVolatileBucketTable->m_dwNumBuckets);   // client keeps asking?
            return FALSE;
        }
        pIter->m_pEntry = m_pVolatileBucketTable->m_pBuckets[pIter->m_dwBucket];

        // If this bucket has no chain, keep advancing.  Otherwise we are done
        if (pIter->m_pEntry)
            return TRUE;
    }

    // We are within a chain.  Advance to the next entry
    pIter->m_pEntry = pIter->m_pEntry->pNext;

    _ASSERTE(pIter->m_pEntry);
    return TRUE;
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
KeyType EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::
            IterateGetKey(EEHashTableIteration *pIter)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE(pIter->m_pTable == (void *) this);
    _ASSERTE(pIter->m_dwBucket < m_pVolatileBucketTable->m_dwNumBuckets && pIter->m_pEntry);
    return Helper::GetKey(pIter->m_pEntry);
}

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
HashDatum EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>::
            IterateGetValue(EEHashTableIteration *pIter)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pIter->m_pTable == (void *) this);
    _ASSERTE(pIter->m_dwBucket < m_pVolatileBucketTable->m_dwNumBuckets && pIter->m_pEntry);
    return pIter->m_pEntry->Data;
}

#endif /* _EE_HASH_INL */
