// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: ShimEvents.cpp
// 

//
// The V3 ICD debugging APIs have a lower abstraction level than V2.
// This provides V2 ICD debugging functionality on top of the V3 debugger object.
//*****************************************************************************

#include "stdafx.h"

#include "safewrap.h"
#include "check.h" 

#include <limits.h>
#include "shimpriv.h"

//---------------------------------------------------------------------------------------
// Need virtual dtor since this is a base class.
// Derived classes will do real work
//---------------------------------------------------------------------------------------
ManagedEvent::~ManagedEvent()
{
}

#ifdef _DEBUG
//---------------------------------------------------------------------------------------
// For debugging, get a pointer value that can identify the type of this event.
// 
// Returns:
//    persistent pointer value that can be used as cookie to identify this event type.
//---------------------------------------------------------------------------------------
void * ManagedEvent::GetDebugCookie()
{
    // Return vtable, first void* in the structure.
    return *(reinterpret_cast<void**> (this));
}
#endif

//---------------------------------------------------------------------------------------
// Ctor for DispatchArgs
// 
// Arguments:
//    pCallback1 - 1st callback, for debug events in V1.0, V1.1
//    pCallback2 - 2nd callback, for debug events added in V2
//
// Notes:
//    We'll have a lot of derived classes of ManagedEvent, and so encapsulating the arguments
//    for the Dispatch() function lets us juggle them around easily without hitting every signature.
//---------------------------------------------------------------------------------------
ManagedEvent::DispatchArgs::DispatchArgs(ICorDebugManagedCallback * pCallback1, ICorDebugManagedCallback2 * pCallback2, ICorDebugManagedCallback3 * pCallback3, ICorDebugManagedCallback4 * pCallback4)
{
    m_pCallback1 = pCallback1;
    m_pCallback2 = pCallback2;
    m_pCallback3 = pCallback3;
    m_pCallback4 = pCallback4;
}


// trivial accessor to get Callback 1
ICorDebugManagedCallback * ManagedEvent::DispatchArgs::GetCallback1()
{
    return m_pCallback1;
}

// trivial accessor to get callback 2
ICorDebugManagedCallback2 * ManagedEvent::DispatchArgs::GetCallback2()
{
    return m_pCallback2;
}

// trivial accessor to get callback 3
ICorDebugManagedCallback3 * ManagedEvent::DispatchArgs::GetCallback3()
{
    return m_pCallback3;
}

// trivial accessor to get callback 4
ICorDebugManagedCallback4 * ManagedEvent::DispatchArgs::GetCallback4()
{
    return m_pCallback4;
}

// Returns OS Thread Id that this event occurred on, 0 if no thread affinity.
DWORD ManagedEvent::GetOSTid()
{
    return m_dwThreadId;
}

//---------------------------------------------------------------------------------------
// Constructore for events with thread affinity
// 
// Arguments:
//     pThread -  thread that this event is associated with. 
//
// Notes:
//     Thread affinity is used with code:ManagedEventQueue::HasQueuedCallbacks
//     This includes event callbacks that have a thread parameter
//---------------------------------------------------------------------------------------
ManagedEvent::ManagedEvent(ICorDebugThread * pThread)
{
    m_dwThreadId = 0;
    if (pThread != NULL)
    {
        pThread->GetID(&m_dwThreadId);
    }        
    
    m_pNext = NULL;
}

//---------------------------------------------------------------------------------------
// Constructor for events with no thread affinity
//---------------------------------------------------------------------------------------
ManagedEvent::ManagedEvent()
{
    m_dwThreadId = 0;
    m_pNext = NULL;
}

    




// Ctor
ManagedEventQueue::ManagedEventQueue()
{
    m_pFirstEvent = NULL;
    m_pLastEvent = NULL;
    m_pLock = NULL;
}

//---------------------------------------------------------------------------------------
// Initialize
//
// Arguments:
//    pLock - lock that protects this event queue. This takes a weak ref to the lock,
//            so caller ensures lock stays alive for lifespan of this object
// 
// Notes:
//    Event queue locks itself using this lock.
//    Only call this once. 
//---------------------------------------------------------------------------------------
void ManagedEventQueue::Init(RSLock * pLock)
{
    _ASSERTE(m_pLock == NULL);
    m_pLock = pLock;
}

//---------------------------------------------------------------------------------------    
// Remove event from the top. 
//
// Returns: 
//    Event that was just dequeued. 
//
// Notes:
//    Caller then takes ownership of Event and will call Delete on it.
//    If IsEmpty() function returns NULL.
//    
//    It is an error to call Dequeue when the only elements in the queue are suspended.
//    Suspending the queue implies there are going to be new events added which should come before
//    the elements that are suspended.  Trying to deqeue when there are only suspended elements
//    left is error-prone - if it were allowed, the order may be non-deterministic.
//    In practice we could probably ban calling Dequeue at all when any elements are suspended,
//    but this seems overly restrictive - there is nothing wrong with allowing these "new"
//    events to be dequeued since we know they come first (you can't nest suspensions).
//---------------------------------------------------------------------------------------
ManagedEvent * ManagedEventQueue::Dequeue()
{
    RSLockHolder lockHolder(m_pLock);
    if (m_pFirstEvent == NULL)
    {
        return NULL;
    }
    
    ManagedEvent * pEvent = m_pFirstEvent;
    m_pFirstEvent = m_pFirstEvent->m_pNext;
    if (m_pFirstEvent == NULL)
    {
        m_pLastEvent = NULL;
    }

    pEvent->m_pNext = NULL;
    return pEvent;
}

//---------------------------------------------------------------------------------------
// Append the event to the end of the queue.
// Queue owns the event and will delete it (unless it's dequeued first).
// 
// Note that this can be called when a suspended queue is active.  Events are pushed onto 
// the currently active queue (ahead of the suspended queue).
//
// Arguments:
//     pEvent - event to queue.
//
//---------------------------------------------------------------------------------------
void ManagedEventQueue::QueueEvent(ManagedEvent * pEvent)
{
    RSLockHolder lockHolder(m_pLock);
    _ASSERTE(pEvent != NULL);
    _ASSERTE(pEvent->m_pNext == NULL);
    
    if (m_pLastEvent == NULL)
    {
        _ASSERTE(m_pFirstEvent == NULL);
        m_pFirstEvent = m_pLastEvent = pEvent;
    }
    else
    {
        m_pLastEvent->m_pNext = pEvent;
        m_pLastEvent = pEvent;
    }
}


//---------------------------------------------------------------------------------------
// Returns true iff the event queue is empty (including any suspended queue elements)
//---------------------------------------------------------------------------------------
bool ManagedEventQueue::IsEmpty()
{
    RSLockHolder lockHolder(m_pLock);
    if (m_pFirstEvent != NULL)
    {
        _ASSERTE(m_pLastEvent != NULL);
        return false;
    }

    _ASSERTE(m_pLastEvent == NULL);
    return true;
}


//---------------------------------------------------------------------------------------
// Delete all events and empty the queue (including any suspended queue elements)
//
// Notes:
//    This is like calling { while(!IsEmpty()) delete Dequeue(); }
//---------------------------------------------------------------------------------------
void ManagedEventQueue::DeleteAll()
{
    RSLockHolder lockHolder(m_pLock);

    while (m_pFirstEvent != NULL)
    {
        // verify that the last event in the queue is actually the one stored as the last event
        _ASSERTE( m_pFirstEvent->m_pNext != NULL || m_pFirstEvent == m_pLastEvent );

        ManagedEvent * pNext = m_pFirstEvent->m_pNext;
        delete m_pFirstEvent;
        m_pFirstEvent = pNext;
    }
    m_pLastEvent = NULL;

    _ASSERTE(IsEmpty());
};

//---------------------------------------------------------------------------------------
// Worker to implement ICorDebugProcess::HasQueuedCallbacks for shim
//---------------------------------------------------------------------------------------
BOOL ManagedEventQueue::HasQueuedCallbacks(ICorDebugThread * pThread)
{
    // This is from the public paths of ICorDebugProcess::HasQueuedCallbacks.
    // In V2, this would fail in cases, notably including if the process is not synchronized.
    // In arrowhead, it always succeeds.

    // No thread - look process wide.
    if (pThread == NULL)
    {
        return !IsEmpty();
    }

    // If we have a thread, look for events with thread affinity.
    DWORD dwThreadID = 0;
    HRESULT hr = pThread->GetID(&dwThreadID);
    (void)hr; //prevent "unused variable" error from GCC
    SIMPLIFYING_ASSUMPTION(SUCCEEDED(hr));

    // Don't take lock until after we don't call any ICorDebug APIs.
    RSLockHolder lockHolder(m_pLock);

    ManagedEvent * pCurrent = m_pFirstEvent;
    while (pCurrent != NULL)
    {
        if (pCurrent->GetOSTid() == dwThreadID)
        {
            return true;
        }
        pCurrent = pCurrent->m_pNext;
    }
    return false;
}




