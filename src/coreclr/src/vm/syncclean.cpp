//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include "common.h"

#include "syncclean.hpp"
#include "virtualcallstub.h"
#include "threadsuspend.h"

VolatilePtr<Bucket> SyncClean::m_HashMap = NULL;
VolatilePtr<EEHashEntry*> SyncClean::m_EEHashTable;

void SyncClean::Terminate()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    CleanUp();
}

void SyncClean::AddHashMap (Bucket *bucket)
{
    WRAPPER_NO_CONTRACT;

    if (!g_fEEStarted) {
        delete [] bucket;
        return;
    }

    BEGIN_GETTHREAD_ALLOWED
    _ASSERTE (GetThread() == NULL || GetThread()->PreemptiveGCDisabled());
    END_GETTHREAD_ALLOWED

    Bucket * pTempBucket = NULL;
    do
    {
        pTempBucket = (Bucket *)m_HashMap;
        NextObsolete (bucket) = pTempBucket;
    }
    while (FastInterlockCompareExchangePointer(m_HashMap.GetPointer(), bucket, pTempBucket) != pTempBucket);
}

void SyncClean::AddEEHashTable (EEHashEntry** entry)
{
    WRAPPER_NO_CONTRACT;

    if (!g_fEEStarted) {
        delete [] (entry-1);
        return;
    }

    BEGIN_GETTHREAD_ALLOWED
    _ASSERTE (GetThread() == NULL || GetThread()->PreemptiveGCDisabled());
    END_GETTHREAD_ALLOWED

    EEHashEntry ** pTempHashEntry = NULL;
    do
    {
        pTempHashEntry = (EEHashEntry**)m_EEHashTable;
        entry[-1] = (EEHashEntry *)pTempHashEntry;
    }
    while (FastInterlockCompareExchangePointer(m_EEHashTable.GetPointer(), entry, pTempHashEntry) != pTempHashEntry);
}

void SyncClean::CleanUp ()
{
    LIMITED_METHOD_CONTRACT;

    // Only GC thread can call this.
    _ASSERTE (g_fProcessDetach || 
              IsGCSpecialThread() ||
              (GCHeap::IsGCInProgress()  && GetThread() == ThreadSuspend::GetSuspensionThread()));
    if (m_HashMap)
    {
        Bucket * pTempBucket = FastInterlockExchangePointer(m_HashMap.GetPointer(), NULL);
        
        while (pTempBucket) 
        {
            Bucket* pNextBucket = NextObsolete (pTempBucket);
            delete [] pTempBucket;
            pTempBucket = pNextBucket;
        }
    }

    if (m_EEHashTable)
    {
        EEHashEntry ** pTempHashEntry = FastInterlockExchangePointer(m_EEHashTable.GetPointer(), NULL);

        while (pTempHashEntry) {
            EEHashEntry **pNextHashEntry = (EEHashEntry **)pTempHashEntry[-1];
            pTempHashEntry --;
            delete [] pTempHashEntry;
            pTempHashEntry = pNextHashEntry;
        }        
    }    

    // Give others we want to reclaim during the GC sync point a chance to do it
    VirtualCallStubManager::ReclaimAll();
}
