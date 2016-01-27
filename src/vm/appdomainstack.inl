// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*============================================================
**
** Header:  AppDomainStack.inl
**
** Purpose: Implements ADStack inline functions
**


**
===========================================================*/
#ifndef _APPDOMAINSTACK_INL
#define _APPDOMAINSTACK_INL

#include "threads.h"
#include "appdomain.hpp"
#include "appdomainstack.h"
#include "security.h"


#ifndef DACCESS_COMPILE

#ifdef _DEBUG
#define LogADStackUpdateIfDebug LogADStackUpdate()
inline void AppDomainStack::LogADStackUpdate(void)
{
    LIMITED_METHOD_CONTRACT;
    for (int i=m_numEntries-1; i >= 0; i--) {
        AppDomainStackEntry* pEntry = __GetEntryPtr(i);
           
        LOG((LF_APPDOMAIN, LL_INFO100, "    stack[%d]: AppDomain id[%d] Overrides[%d] Asserts[%d] \n", i, 
            pEntry->m_domainID.m_dwId, pEntry->m_dwOverridesCount, pEntry->m_dwAsserts));
    }
}

#else
#define LogADStackUpdateIfDebug 
#endif

inline void AppDomainStack::AddMoreDomains(void)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Need to allocate a bigger block for pMoreDomains
    AppDomainStackEntry *tmp = m_pExtraStack;
    m_pExtraStack = new AppDomainStackEntry[m_ExtraStackSize + ADSTACK_BLOCK_SIZE];
    memcpy(m_pExtraStack, tmp, sizeof(AppDomainStackEntry)*(m_ExtraStackSize));
    FillEntries((m_pExtraStack+m_ExtraStackSize), ADSTACK_BLOCK_SIZE);
    m_ExtraStackSize+= ADSTACK_BLOCK_SIZE;
    delete[] tmp; // free the old block
    
}
inline void AppDomainStack::PushDomain(ADID pDomain)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    LOG((LF_APPDOMAIN, LL_INFO100, "Thread::PushDomain (%d), count now %d\n", pDomain.m_dwId, m_numEntries+1));

    //
    // When entering a new AppDomain, we need to update the thread wide
    // state with the intersection of the current and the new AppDomains flags.
    // This is because the old AppDomain could have loaded new assemblies
    // that are not yet reflected in the thread wide state, and the thread
    // could then execute code in that new Assembly.
    // We save the old thread wide state in the AppDomainStackEntry so we
    // can restore it when we pop the stack entry.
    //

    // The pushed domain could be the default AppDomain (which is the starting
    // AppDomain for all threads), in which case we don't need to intersect
    // with the flags from the previous AppDomain.
    Thread* pThread = GetThread();
    if (pThread)
        m_dwThreadWideSpecialFlags &= pThread->GetDomain()->GetSecurityDescriptor()->GetDomainWideSpecialFlag();

    if (m_numEntries == ADSTACK_BLOCK_SIZE + m_ExtraStackSize)
    {
        AddMoreDomains();
    }

    _ASSERTE(m_numEntries < ADSTACK_BLOCK_SIZE + m_ExtraStackSize);
    if (m_numEntries < ADSTACK_BLOCK_SIZE)
    {
        m_pStack[m_numEntries].m_domainID = pDomain;
        m_pStack[m_numEntries].m_dwAsserts = 0;
        m_pStack[m_numEntries].m_dwOverridesCount = 0;
        m_pStack[m_numEntries].m_dwPreviousThreadWideSpecialFlags = m_dwThreadWideSpecialFlags;
    }
    else
    {
        m_pExtraStack[m_numEntries-ADSTACK_BLOCK_SIZE].m_domainID = pDomain ;
        m_pExtraStack[m_numEntries-ADSTACK_BLOCK_SIZE].m_dwAsserts = 0;
        m_pExtraStack[m_numEntries-ADSTACK_BLOCK_SIZE].m_dwOverridesCount = 0;
        m_pExtraStack[m_numEntries-ADSTACK_BLOCK_SIZE].m_dwPreviousThreadWideSpecialFlags = m_dwThreadWideSpecialFlags;
    }

    if (pThread) {
        AppDomainFromIDHolder pAppDomain(pDomain, TRUE);
        if (!pAppDomain.IsUnloaded())
            m_dwThreadWideSpecialFlags &= pAppDomain->GetSecurityDescriptor()->GetDomainWideSpecialFlag();
    }

    m_numEntries++;

    LogADStackUpdateIfDebug;
}

inline ADID AppDomainStack::PopDomain()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ADID pRet = (ADID)INVALID_APPDOMAIN_ID;
    _ASSERTE(m_numEntries > 0);
    if (m_numEntries > 0)
    {
        m_numEntries--;
        AppDomainStackEntry ret_entry;
        const AppDomainStackEntry reset_entry = {ADID(INVALID_APPDOMAIN_ID), 0, 0};

        if (m_numEntries < ADSTACK_BLOCK_SIZE)
        {
            ret_entry = m_pStack[m_numEntries];
            m_pStack[m_numEntries] = reset_entry;
        }
        else
        {
            ret_entry = m_pExtraStack[m_numEntries-ADSTACK_BLOCK_SIZE];
            m_pExtraStack[m_numEntries-ADSTACK_BLOCK_SIZE] = reset_entry;
        }
        pRet=ret_entry.m_domainID;

        LOG((LF_APPDOMAIN, LL_INFO100, "PopDomain: Popping pRet.m_dwId [%d] m_dwAsserts:%d ret_entry.m_dwAsserts:%d. New m_dwAsserts:%d\n",
            pRet.m_dwId, m_dwAsserts,ret_entry.m_dwAsserts, (m_dwAsserts-ret_entry.m_dwAsserts)));
        
        m_dwAsserts -= ret_entry.m_dwAsserts;
        m_dwOverridesCount -= ret_entry.m_dwOverridesCount;
#ifdef _DEBUG    
        CheckOverridesAssertCounts();
#endif    

        //
        // When leaving an AppDomain, we need to update the thread wide state by 
        // restoring to the state we were in before entering the AppDomain
        //

        m_dwThreadWideSpecialFlags = ret_entry.m_dwPreviousThreadWideSpecialFlags;

        LOG((LF_APPDOMAIN, LL_INFO100, "Thread::PopDomain popping [%d] count now %d\n", 
            pRet.m_dwId , m_numEntries));
    }
    else
    {
        LOG((LF_APPDOMAIN, LL_INFO100, "Thread::PopDomain count now %d (error pop)\n", m_numEntries));
    }

    LogADStackUpdateIfDebug;
    return pRet;
}
#endif // DACCESS_COMPILE

inline DWORD   AppDomainStack::GetNumDomains() const
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_numEntries >= 1);
    return m_numEntries;
}

inline DWORD AppDomainStack::GetThreadWideSpecialFlag() const
{
    LIMITED_METHOD_CONTRACT;
    return m_dwThreadWideSpecialFlags;
}

inline DWORD AppDomainStack::IncrementOverridesCount()
{
    
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;// Yes, we update global state here, but at worst we have an incorrect overrides count that will be updated the next
    }CONTRACTL_END; // time we run any code that leads to UpdateOverrides. And I don't see even how that can happen: it doesn't look possible
                    // for use to take an SO between the update and when we return to managed code.
    AppDomainStackEntry *pEntry = ReadTopOfStack();
    _ASSERTE(pEntry->m_domainID.m_dwId != INVALID_APPDOMAIN_ID);
    ++(pEntry->m_dwOverridesCount);
    return ++m_dwOverridesCount;
}
inline DWORD AppDomainStack::DecrementOverridesCount()
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        SO_TOLERANT;
    }CONTRACTL_END;
    AppDomainStackEntry *pEntry = ReadTopOfStack();
    _ASSERTE(pEntry->m_domainID.m_dwId != INVALID_APPDOMAIN_ID);
    _ASSERTE(pEntry->m_dwOverridesCount > 0);
    _ASSERTE(m_dwOverridesCount > 0);
    if (pEntry->m_dwOverridesCount > 0 && m_dwOverridesCount > 0)
    {
        --(pEntry->m_dwOverridesCount);
        return --m_dwOverridesCount;
    }
    
    return 0;
}
inline DWORD AppDomainStack::GetOverridesCount()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;
#ifdef _DEBUG    
    CheckOverridesAssertCounts();
#endif    
    return m_dwOverridesCount;
}

inline DWORD AppDomainStack::GetInnerAppDomainOverridesCount()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;
#ifdef _DEBUG    
    CheckOverridesAssertCounts();
#endif
    AppDomainStackEntry *pEntry = ReadTopOfStack();
    _ASSERTE(pEntry->m_domainID.m_dwId != INVALID_APPDOMAIN_ID);

    return pEntry->m_dwOverridesCount;
}

inline DWORD AppDomainStack::IncrementAssertCount()
{
    LIMITED_METHOD_CONTRACT;
    AppDomainStackEntry *pEntry = ReadTopOfStack();
    _ASSERTE(pEntry->m_domainID.m_dwId != INVALID_APPDOMAIN_ID);
    LOG((LF_APPDOMAIN, LL_INFO100, "IncrementAssertCount: m_dwAsserts:%d ADID:%d pEntry:%p pEntry->m_dwAsserts:%d.\n",
        m_dwAsserts, pEntry->m_domainID.m_dwId, pEntry, pEntry->m_dwAsserts));    
    ++(pEntry->m_dwAsserts);
    return ++m_dwAsserts;
}
inline DWORD AppDomainStack::DecrementAssertCount()
{
    LIMITED_METHOD_CONTRACT;
    AppDomainStackEntry *pEntry = ReadTopOfStack();
    _ASSERTE(pEntry->m_domainID.m_dwId != INVALID_APPDOMAIN_ID);
    _ASSERTE(pEntry->m_dwAsserts > 0);
    _ASSERTE(m_dwAsserts > 0);
    LOG((LF_APPDOMAIN, LL_INFO100, "DecrementAssertCount: m_dwAsserts:%d ADID:%d pEntry:%p pEntry->m_dwAsserts:%d.\n",
        m_dwAsserts, pEntry->m_domainID.m_dwId, pEntry, pEntry->m_dwAsserts));        
    --(pEntry->m_dwAsserts);
    return --m_dwAsserts;
}

inline DWORD AppDomainStack::GetAssertCount()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;
#ifdef _DEBUG    
    CheckOverridesAssertCounts();
#endif

    return m_dwAsserts;
}

inline DWORD AppDomainStack::GetInnerAppDomainAssertCount()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;
#ifdef _DEBUG    
    CheckOverridesAssertCounts();
#endif
    AppDomainStackEntry *pEntry = ReadTopOfStack();
    _ASSERTE(pEntry->m_domainID.m_dwId != INVALID_APPDOMAIN_ID);

    return pEntry->m_dwAsserts;
}

inline void AppDomainStack::InitDomainIteration(DWORD *pIndex) const
{
    LIMITED_METHOD_CONTRACT;
    *pIndex = m_numEntries;
}

inline ADID AppDomainStack::GetNextDomainOnStack(DWORD *pIndex, DWORD *pOverrides, DWORD *pAsserts) const
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    _ASSERTE(*pIndex > 0 && *pIndex <= m_numEntries);
    (*pIndex) --;
    const AppDomainStackEntry *pEntry = __GetEntryPtr(*pIndex);
    if (pOverrides != NULL)
        *pOverrides = pEntry->m_dwOverridesCount;
    if (pAsserts != NULL)
        *pAsserts = pEntry->m_dwAsserts;
    return (ADID)pEntry->m_domainID.m_dwId;
}

inline AppDomainStackEntry* AppDomainStack::GetCurrentDomainEntryOnStack(DWORD pIndex)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    _ASSERTE(pIndex >=0 && pIndex < m_numEntries);
    return __GetEntryPtr(pIndex);
}

inline AppDomainStackEntry* AppDomainStack::GetNextDomainEntryOnStack(DWORD *pIndex)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    _ASSERTE(*pIndex >0 && *pIndex <= m_numEntries);
    (*pIndex) --;
    return __GetEntryPtr(*pIndex);
}

inline void AppDomainStack::UpdateDomainOnStack(DWORD pIndex, DWORD asserts, DWORD overrides)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    AppDomainStackEntry* entry;
    _ASSERTE(pIndex >=0 && pIndex < m_numEntries);
    entry = __GetEntryPtr(pIndex);
    _ASSERTE(entry->m_domainID.m_dwId != INVALID_APPDOMAIN_ID);
    entry->m_dwAsserts = asserts;
    entry->m_dwOverridesCount = overrides;
    UpdateStackFromEntries();
    
}


inline void AppDomainStack::UpdateStackFromEntries()
{
    LIMITED_METHOD_CONTRACT;
    DWORD   dwAppDomainIndex = 0;
    DWORD dwOverrides = 0;
    DWORD dwAsserts = 0;
    AppDomainStackEntry *pEntry = NULL;
    for(dwAppDomainIndex=0;dwAppDomainIndex<m_numEntries;dwAppDomainIndex++)
    {
        pEntry = __GetEntryPtr(dwAppDomainIndex);
        dwOverrides += pEntry->m_dwOverridesCount;
        dwAsserts += pEntry->m_dwAsserts;
    }
    LOG((LF_APPDOMAIN, LL_INFO100, "UpdateStackFromEntries: m_dwAsserts:%d Calculated dwAsserts:%d.\n",m_dwAsserts,dwAsserts));    

    m_dwAsserts = dwAsserts;
    m_dwOverridesCount = dwOverrides;
    return;
}

inline AppDomainStackEntry* AppDomainStack::ReadTopOfStack()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_numEntries > 0);
    AppDomainStackEntry* pEntry = NULL;
    if (m_numEntries <= ADSTACK_BLOCK_SIZE)
    {
        pEntry = &(m_pStack[m_numEntries-1]);
    }
    else
    {
        pEntry = &(m_pExtraStack[m_numEntries-ADSTACK_BLOCK_SIZE-1]);
    }
    return pEntry;
}

inline bool AppDomainStack::IsDefaultSecurityInfo() const
{
    LIMITED_METHOD_CONTRACT;
    return (m_numEntries == 1 && m_pStack[0].m_domainID == ADID(DefaultADID) &&
            m_pStack[0].m_dwAsserts == 0 && m_pStack[0].m_dwOverridesCount == 0);
}
inline void AppDomainStack::ClearDomainStack()
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }CONTRACTL_END;
    m_dwThreadWideSpecialFlags = 0xFFFFFFFF;
    m_numEntries = 1;
    FillEntries(m_pStack, ADSTACK_BLOCK_SIZE);
    if (m_pExtraStack != NULL)
        delete[] m_pExtraStack;
    m_pExtraStack = NULL;
    m_ExtraStackSize = 0;
    m_dwOverridesCount = 0;
    LOG((LF_APPDOMAIN, LL_INFO100, "ClearDomainStack: m_dwAsserts:%d setting to 0\n",m_dwAsserts));
    m_dwAsserts = 0;
    m_pStack[0].m_domainID = ADID(DefaultADID);
}

#endif
