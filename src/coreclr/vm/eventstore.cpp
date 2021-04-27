// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "eventstore.hpp"
#include "synch.h"

// A class to maintain a pool of available events.

const int EventStoreLength = 8;
class EventStore
{
public:
    // Note: No constructors/destructors - global instance

    void Init()
    {
        WRAPPER_NO_CONTRACT;

        m_EventStoreCrst.Init(CrstEventStore, CRST_UNSAFE_ANYMODE);
        m_Store = NULL;
    }

    void Destroy()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE (g_fEEShutDown);

        m_EventStoreCrst.Destroy();

        EventStoreElem *walk;
        EventStoreElem *next;
        
        walk = m_Store;
        while (walk) {
            next = walk->next;
            delete (walk);
            walk = next;
        }
    }

    void StoreHandleForEvent (CLREvent* handle)
    {
        CONTRACTL {
            THROWS;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        _ASSERTE (handle);
        CrstHolder ch(&m_EventStoreCrst);
        if (m_Store == NULL) {
            m_Store = new EventStoreElem ();
        }
        EventStoreElem *walk;
#ifdef _DEBUG
        // See if we have some leakage.
        LONG count = 0;
        for (walk = m_Store; walk; walk = walk->next) {
            count += walk->AvailableEventCount();
        }
        // The number of events stored in the pool should be small.
        _ASSERTE (count <= ThreadStore::s_pThreadStore->ThreadCountInEE() * 2 + 10);
#endif
        for (walk = m_Store; walk; walk = walk->next) {
            if (walk->StoreHandleForEvent (handle) )
                return;
            if (walk->next == NULL) {
                break;
            }
        }
        if (walk != NULL)
        {
            walk->next = new EventStoreElem ();
            walk->next->hArray[0] = handle;
        }
    }

    CLREvent* GetHandleForEvent ()
    {
        CONTRACTL {
            THROWS;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        CLREvent* handle;
        CrstHolder ch(&m_EventStoreCrst);
        EventStoreElem *walk = m_Store;
        while (walk) {
            handle = walk->GetHandleForEvent();
            if (handle != NULL) {
                return handle;
            }
            walk = walk->next;
        }
        handle = new CLREvent;
        _ASSERTE (handle != NULL);
        handle->CreateManualEvent(TRUE);
        return handle;
    }

private:
    struct EventStoreElem
    {
        CLREvent *hArray[EventStoreLength];
        EventStoreElem *next;

        EventStoreElem ()
        {
            LIMITED_METHOD_CONTRACT;

            next = NULL;
            for (int i = 0; i < EventStoreLength; i ++) {
                hArray[i] = NULL;
            }
        }

        ~EventStoreElem ()
        {
            LIMITED_METHOD_CONTRACT;

            for (int i = 0; i < EventStoreLength; i++) {
                if (hArray[i]) {
                    delete hArray[i];
                    hArray[i] = NULL;
                }
            }
        }

        // Store a handle in the current EventStoreElem.  Return TRUE if succeessful.
        // Return FALSE if failed due to no free slot.
        BOOL StoreHandleForEvent (CLREvent* handle)
        {
            LIMITED_METHOD_CONTRACT;

            for (int i = 0; i < EventStoreLength; i++) {
                if (hArray[i] == NULL) {
                    hArray[i] = handle;
                    return TRUE;
                }
            }
            return FALSE;
        }

        // Get a handle from the current EventStoreElem.
        CLREvent* GetHandleForEvent ()
        {
            LIMITED_METHOD_CONTRACT;

            for (int i = 0; i < EventStoreLength; i++) {
                if (hArray[i] != NULL) {
                    CLREvent* handle = hArray[i];
                    hArray[i] = NULL;
                    return handle;
                }
            }

            return NULL;
        }

#ifdef _DEBUG
        LONG AvailableEventCount ()
        {
            LIMITED_METHOD_CONTRACT;

            LONG count = 0;
            for (int i = 0; i < EventStoreLength; i++) {
                if (hArray[i] != NULL) {
                    count ++;
                }
            }
            return count;
        }
#endif
    };

    EventStoreElem  *m_Store;

    // Critical section for adding and removing event used for Object::Wait
    CrstStatic      m_EventStoreCrst;
};

static EventStore s_EventStore;

CLREvent* GetEventFromEventStore()
{
    WRAPPER_NO_CONTRACT;

    return s_EventStore.GetHandleForEvent();
}

void StoreEventToEventStore(CLREvent* hEvent)
{
    WRAPPER_NO_CONTRACT;

    s_EventStore.StoreHandleForEvent(hEvent);
}

void InitEventStore()
{
    WRAPPER_NO_CONTRACT;

    s_EventStore.Init();
}

void TerminateEventStore()
{
    WRAPPER_NO_CONTRACT;

    s_EventStore.Destroy();
}
