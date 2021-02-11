// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __EventStore_hpp
#define __EventStore_hpp

#include "synch.h"

class SyncBlock;
struct SLink;
struct WaitEventLink;

typedef DPTR(WaitEventLink) PTR_WaitEventLink;

// Used inside Thread class to chain all events that a thread is waiting for by Object::Wait
struct WaitEventLink {
    SyncBlock         *m_WaitSB;
    CLREvent          *m_EventWait;
    PTR_Thread         m_Thread;       // Owner of this WaitEventLink.
    PTR_WaitEventLink  m_Next;         // Chain to the next waited SyncBlock.
    SLink              m_LinkSB;       // Chain to the next thread waiting on the same SyncBlock.
    DWORD              m_RefCount;     // How many times Object::Wait is called on the same SyncBlock.
};

CLREvent* GetEventFromEventStore();
void StoreEventToEventStore(CLREvent* hEvent);
void InitEventStore();
void TerminateEventStore();

#endif
