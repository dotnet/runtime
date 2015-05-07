//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
m_pThread(pThread)
{
    LIMITED_METHOD_CONTRACT;
    pThread->DebugBlockingInfo.PushBlockingItem(pItem);
}
#endif //DACCESS_COMPILE

// Holder destructor pops a blocking item off the blocking info stack
// NOTE: optimizations are disabled to work around a codegen bug on x86
#ifndef DACCESS_COMPILE
#ifdef _TARGET_X86_
#pragma optimize( "", off )
#endif // _TARGET_X86_
DebugBlockingItemHolder::~DebugBlockingItemHolder()
{
    LIMITED_METHOD_CONTRACT;
    m_pThread->DebugBlockingInfo.PopBlockingItem();
}
#ifdef _TARGET_X86_
#pragma optimize( "", on )
#endif // _TARGET_X86_
#endif //DACCESS_COMPILE
