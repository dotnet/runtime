// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeevent.h"
#include "eventpipeprovider.h"

#ifdef FEATURE_PERFTRACING

EventPipeProvider::EventPipeProvider(const GUID &providerID, EventPipeCallback pCallbackFunction, void *pCallbackData)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_providerID = providerID;
    m_enabled = false;
    m_keywords = 0;
    m_providerLevel = EventPipeEventLevel::Critical;
    m_pEventList = new SList<SListElem<EventPipeEvent*>>();
    m_pCallbackFunction = pCallbackFunction;
    m_pCallbackData = pCallbackData;
    m_pConfig = EventPipe::GetConfiguration();
    _ASSERTE(m_pConfig != NULL);

    // Register the provider.
    m_pConfig->RegisterProvider(*this);
}

EventPipeProvider::~EventPipeProvider()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Unregister the provider.
    // This call is re-entrant.
    // NOTE: We don't use the cached event pipe configuration pointer
    // in case this runs during shutdown and the configuration has already
    // been freed.
    EventPipeConfiguration* pConfig = EventPipe::GetConfiguration();
    _ASSERTE(pConfig != NULL);
    pConfig->UnregisterProvider(*this);

    // Free all of the events.
    if(m_pEventList != NULL)
    {
        // Take the lock before manipulating the list.
        CrstHolder _crst(EventPipe::GetLock());

        SListElem<EventPipeEvent*> *pElem = m_pEventList->GetHead();
        while(pElem != NULL)
        {
            EventPipeEvent *pEvent = pElem->GetValue();
            delete pEvent;

            pElem = m_pEventList->GetNext(pElem);
        }

        delete m_pEventList;
        m_pEventList = NULL;
    }
}

const GUID& EventPipeProvider::GetProviderID() const
{
    LIMITED_METHOD_CONTRACT;

    return m_providerID;
}

bool EventPipeProvider::Enabled() const
{
    LIMITED_METHOD_CONTRACT;

    return (m_pConfig->Enabled() && m_enabled);
}

bool EventPipeProvider::EventEnabled(INT64 keywords) const
{
    LIMITED_METHOD_CONTRACT;

    // The event is enabled if:
    //  - The provider is enabled.
    //  - The event keywords are unspecified in the manifest (== 0) or when masked with the enabled config are != 0.
    return (Enabled() && ((keywords == 0) || ((m_keywords & keywords) != 0)));
}

bool EventPipeProvider::EventEnabled(INT64 keywords, EventPipeEventLevel eventLevel) const
{
    LIMITED_METHOD_CONTRACT;

    // The event is enabled if:
    //  - The provider is enabled.
    //  - The event keywords are unspecified in the manifest (== 0) or when masked with the enabled config are != 0.
    //  - The event level is LogAlways or the provider's verbosity level is set to greater than the event's verbosity level in the manifest.
    return (EventEnabled(keywords) &&
        ((eventLevel == EventPipeEventLevel::LogAlways) || (m_providerLevel >= eventLevel)));
}

void EventPipeProvider::SetConfiguration(bool providerEnabled, INT64 keywords, EventPipeEventLevel providerLevel)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    m_enabled = providerEnabled;
    m_keywords = keywords;
    m_providerLevel = providerLevel;

    RefreshAllEvents();
    InvokeCallback();
}

EventPipeEvent* EventPipeProvider::AddEvent(unsigned int eventID, INT64 keywords, unsigned int eventVersion, EventPipeEventLevel level, BYTE *pMetadata, unsigned int metadataLength)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return AddEvent(eventID, keywords, eventVersion, level, true /* needStack */, pMetadata, metadataLength);
}

EventPipeEvent* EventPipeProvider::AddEvent(unsigned int eventID, INT64 keywords, unsigned int eventVersion, EventPipeEventLevel level, bool needStack, BYTE *pMetadata, unsigned int metadataLength)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Create the event.
    EventPipeEvent *pEvent = new EventPipeEvent(
        *this,
        keywords,
        eventID,
        eventVersion,
        level,
        needStack,
        pMetadata,
        metadataLength);

    // Add it to the list of events.
    AddEvent(*pEvent);
    return pEvent;
}

void EventPipeProvider::AddEvent(EventPipeEvent &event)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the config lock before inserting a new event.
    CrstHolder _crst(EventPipe::GetLock());

    m_pEventList->InsertTail(new SListElem<EventPipeEvent*>(&event));
    event.RefreshState();
}

void EventPipeProvider::InvokeCallback()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    if(m_pCallbackFunction != NULL && !g_fEEShutDown)
    {
        (*m_pCallbackFunction)(
            &m_providerID,
            m_enabled,
            (UCHAR) m_providerLevel,
            m_keywords,
            0 /* matchAllKeywords */,
            NULL /* FilterData */,
            m_pCallbackData /* CallbackContext */);
    }
}

bool EventPipeProvider::GetDeleteDeferred() const
{
    LIMITED_METHOD_CONTRACT;
    return m_deleteDeferred;
}

void EventPipeProvider::SetDeleteDeferred()
{
    LIMITED_METHOD_CONTRACT;
    m_deleteDeferred = true;
}

void EventPipeProvider::RefreshAllEvents()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    SListElem<EventPipeEvent*> *pElem = m_pEventList->GetHead();
    while(pElem != NULL)
    {
        EventPipeEvent *pEvent = pElem->GetValue();
        pEvent->RefreshState();

        pElem = m_pEventList->GetNext(pElem);
    }
}

#endif // FEATURE_PERFTRACING
