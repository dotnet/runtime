// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: ListLock.cpp
// 

// 
// ===========================================================================
// This file decribes the list lock and deadlock aware list lock.
// ===========================================================================


#include "common.h"
#include "listlock.h"
#include "listlock.inl"

ListLockEntry::ListLockEntry(ListLock *pList, void *pData, const char *description)
  : m_deadlock(description),
    m_pList(pList),
    m_pData(pData),
    m_Crst(CrstListLock,
           (CrstFlags)(CRST_REENTRANCY | (pList->IsHostBreakable()?CRST_HOST_BREAKABLE:0))),
    m_pszDescription(description),
    m_pNext(NULL),
    m_dwRefCount(1),
    m_hrResultCode(S_FALSE),
    m_hInitException(NULL),
    m_pLoaderAllocator(NULL)
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    ,
    m_CorruptionSeverity(NotCorrupting)
#endif // FEATURE_CORRUPTING_EXCEPTIONS
{
    WRAPPER_NO_CONTRACT;
}

ListLockEntry *ListLockEntry::Find(ListLock* pLock, LPVOID pPointer, const char *description)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pLock->HasLock());

    ListLockEntry *pEntry = pLock->Find(pPointer);
    if (pEntry==NULL)
    {
        pEntry = new ListLockEntry(pLock, pPointer, description);
        pLock->AddElement(pEntry);
    }
    else
        pEntry->AddRef();

    return pEntry;
};

void ListLockEntry::AddRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(this));
    }
    CONTRACTL_END;

    FastInterlockIncrement((LONG*)&m_dwRefCount);
}

void ListLockEntry::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(this));
    }
    CONTRACTL_END;

    ListLockHolder lock(m_pList);

    if (FastInterlockDecrement((LONG*)&m_dwRefCount) == 0)
    {
        // Remove from list
        m_pList->Unlink(this);
        delete this;
    }
};

