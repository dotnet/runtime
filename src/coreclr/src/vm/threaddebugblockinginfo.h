// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ThreadDebugBlockingInfo.h
//

//
//
// Threads.h was getting so bloated that it seemed time to refactor a little. Rather than shove in yet another
// 50 lines of random definitions of things which happened to be per-thread I separated it out here.
#ifndef __ThreadBlockingInfo__
#define __ThreadBlockingInfo__
#include "mscoree.h"

// Different ways thread can block that the debugger will expose
enum DebugBlockingItemType
{
    DebugBlock_MonitorCriticalSection,
    DebugBlock_MonitorEvent,
};

typedef DPTR(struct DebugBlockingItem) PTR_DebugBlockingItem;

// Represents something a thread blocked on that is exposed via the debugger
struct DebugBlockingItem
{
    // right now we only do monitor locks but this could be
    // expanded to other pieces of data if we want to expose
    // other things that we block on
    PTR_AwareLock pMonitor;
    // The app domain of the object we are blocking on
    PTR_AppDomain pAppDomain;
    // Indicates how the thread is blocked on the item
    DebugBlockingItemType type;
    // blocking timeout in milliseconds or INFINTE for no timeout
    DWORD dwTimeout;
    // next pointer for a linked list of these items
    PTR_DebugBlockingItem pNext;
};

// A visitor function used when enumerating DebuggableBlockingItems
typedef VOID (*DebugBlockingItemVisitor)(PTR_DebugBlockingItem item, VOID* pUserData);

// Maintains a stack of DebugBlockingItems that a thread is currently waiting on
// It is a stack rather than a single item because we wait interruptibly. During the interruptible
// wait we can run more code for an APC or to handle a windows message that could again block on another lock
class ThreadDebugBlockingInfo
{
private:
    // head of the linked list which is our stack
    PTR_DebugBlockingItem m_firstBlockingItem;

public:
    ThreadDebugBlockingInfo();
    ~ThreadDebugBlockingInfo();

#ifndef DACCESS_COMPILE
    // Adds a new blocking item at the front of the list
    VOID PushBlockingItem(DebugBlockingItem *pItem);
    // Removes the most recently added item (FIFO)
    VOID PopBlockingItem();
#else
    // Calls the visitor function on each item in the stack from front to back
    VOID VisitBlockingItems(DebugBlockingItemVisitor vistorFunc, VOID* pUserData);
#endif  //DACCESS_COMPILE
};

#ifndef DACCESS_COMPILE
class DebugBlockingItemHolder
{
private:
    Thread *m_pThread;

public:
    DebugBlockingItemHolder(Thread *pThread, DebugBlockingItem *pItem);
    ~DebugBlockingItemHolder();
};
#endif //!DACCESS_COMPILE

#endif // __ThreadBlockingInfo__
