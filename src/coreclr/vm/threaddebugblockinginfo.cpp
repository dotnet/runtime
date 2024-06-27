// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ThreadDebugBlockingInfo.cpp
//

//
//
#include "common.h"
#include "threaddebugblockinginfo.h"

//Constructor
ThreadDebugBlockingInfo::ThreadDebugBlockingInfo()
{
    m_firstBlockingItem = NULL;
}

//Destructor
ThreadDebugBlockingInfo::~ThreadDebugBlockingInfo()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_firstBlockingItem == NULL);
}

// Adds a new blocking item at the front of the list
// The caller is responsible for allocating the memory this points to and keeping it alive until
// after PopBlockingItem is called
#ifndef DACCESS_COMPILE
VOID ThreadDebugBlockingInfo::PushBlockingItem(DebugBlockingItem *pItem)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pItem != NULL);
    pItem->pNext = m_firstBlockingItem;
    m_firstBlockingItem = pItem;
}
#endif //!DACCESS_COMPILE

// Removes the most recently added item (FIFO)
#ifndef DACCESS_COMPILE
VOID ThreadDebugBlockingInfo::PopBlockingItem()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_firstBlockingItem != NULL);
    m_firstBlockingItem = m_firstBlockingItem->pNext;
}
#endif //!DACCESS_COMPILE

// Calls the visitor function on each item in the stack from front to back
#ifdef DACCESS_COMPILE
VOID ThreadDebugBlockingInfo::VisitBlockingItems(DebugBlockingItemVisitor visitorFunc, VOID* pUserData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SUPPORTS_DAC;

    PTR_DebugBlockingItem pItem = m_firstBlockingItem;
    while(pItem != NULL)
    {
        visitorFunc(pItem, pUserData);
        pItem = pItem->pNext;
    }
}
#endif //DACCESS_COMPILE

// Holder constructor pushes a blocking item on the blocking info stack
#ifndef DACCESS_COMPILE
DebugBlockingItemHolder::DebugBlockingItemHolder(Thread *pThread, DebugBlockingItem *pItem) :
    m_pThread(pThread), m_ppFirstBlockingInfo(nullptr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    m_ppFirstBlockingInfo = (ThreadBlockingInfo**)&t_ThreadStatics.ThreadBlockingInfo_First;
#ifdef _DEBUG
    // Try to verify the address of the thread-local slot for the managed ThreadBlockingInfo.t_first matches the address of the native thread static
    EX_TRY
    {
        FieldDesc *pFD = CoreLibBinder::GetField(FIELD__THREAD_BLOCKING_INFO__FIRST);
        _ASSERTE(m_ppFirstBlockingInfo == (ThreadBlockingInfo **)Thread::GetStaticFieldAddress(pFD));
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions);
#endif

    // Push info for the managed ThreadBlockingInfo
    m_blockingInfo.objectPtr = pItem->pMonitor;
    m_blockingInfo.objectKind = (ThreadBlockingInfo::ObjectKind)pItem->type;
    m_blockingInfo.timeoutMs = (INT32)pItem->dwTimeout;
    m_blockingInfo.next = *m_ppFirstBlockingInfo;
    *m_ppFirstBlockingInfo = &m_blockingInfo;

    pThread->DebugBlockingInfo.PushBlockingItem(pItem);
}
#endif //DACCESS_COMPILE

// Holder destructor pops a blocking item off the blocking info stack
#ifndef DACCESS_COMPILE
DebugBlockingItemHolder::~DebugBlockingItemHolder()
{
    LIMITED_METHOD_CONTRACT;

    m_pThread->DebugBlockingInfo.PopBlockingItem();

    // Pop info for the managed ThreadBlockingInfo
    _ASSERTE(*m_ppFirstBlockingInfo == &m_blockingInfo);
    *m_ppFirstBlockingInfo = m_blockingInfo.next;
}
#endif //DACCESS_COMPILE
