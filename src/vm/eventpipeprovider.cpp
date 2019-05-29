// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeevent.h"
#include "eventpipeprovider.h"
#include "sha1.h"

#ifdef FEATURE_PERFTRACING

EventPipeProvider::EventPipeProvider(
    EventPipeConfiguration *pConfig,
    const SString &providerName,
    EventPipeCallback pCallbackFunction,
    void *pCallbackData) : m_providerName(providerName),
                           m_sessions(0)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pConfig != NULL);
    }
    CONTRACTL_END;

    m_deleteDeferred = false;
    m_keywords = 0;
    m_providerLevel = EventPipeEventLevel::Critical;
    m_pEventList = new SList<SListElem<EventPipeEvent *>>();
    m_pCallbackFunction = pCallbackFunction;
    m_pCallbackData = pCallbackData;
    m_pConfig = pConfig;
    m_deleteDeferred = false;
}

EventPipeProvider::~EventPipeProvider()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Free all of the events.
    if (m_pEventList != NULL)
    {
        // We swallow exceptions here because the HOST_BREAKABLE
        // lock may throw and this destructor gets called in throw
        // intolerant places. If that happens the event list will leak
        EX_TRY
        {
            // Take the lock before manipulating the list.
            CrstHolder _crst(EventPipe::GetLock());

            SListElem<EventPipeEvent *> *pElem = m_pEventList->GetHead();
            while (pElem != NULL)
            {
                EventPipeEvent *pEvent = pElem->GetValue();
                delete pEvent;

                SListElem<EventPipeEvent *> *pCurElem = pElem;
                pElem = m_pEventList->GetNext(pElem);
                delete pCurElem;
            }

            delete m_pEventList;
        }
        EX_CATCH {}
        EX_END_CATCH(SwallowAllExceptions);

        m_pEventList = NULL;
    }
}

const SString &EventPipeProvider::GetProviderName() const
{
    LIMITED_METHOD_CONTRACT;
    return m_providerName;
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

EventPipeProviderCallbackData EventPipeProvider::SetConfiguration(
    uint64_t sessionId,
    INT64 keywords,
    EventPipeEventLevel providerLevel,
    LPCWSTR pFilterData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(sessionId != 0);
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    m_sessions |= sessionId;

    // Set Keywords to be the union of all keywords
    m_keywords |= keywords;

    // Set the provider level to "Log Always" or the biggest verbosity.
    m_providerLevel = (providerLevel < m_providerLevel) ? m_providerLevel : providerLevel;

    RefreshAllEvents(sessionId, keywords, providerLevel);
    return PrepareCallbackData(pFilterData);
}

EventPipeProviderCallbackData EventPipeProvider::UnsetConfiguration(uint64_t sessionId)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION((m_sessions & sessionId) != 0);
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (m_sessions & sessionId)
        m_sessions &= ~sessionId;
    return PrepareCallbackData(nullptr);
}

EventPipeEvent *EventPipeProvider::AddEvent(unsigned int eventID, INT64 keywords, unsigned int eventVersion, EventPipeEventLevel level, bool needStack, BYTE *pMetadata, unsigned int metadataLength)
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

    m_pEventList->InsertTail(new SListElem<EventPipeEvent *>(&event));
    event.RefreshState();
}

/* static */ void EventPipeProvider::InvokeCallback(EventPipeProviderCallbackData eventPipeProviderCallbackData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    LPCWSTR pFilterData = eventPipeProviderCallbackData.pFilterData;
    EventPipeCallback pCallbackFunction = eventPipeProviderCallbackData.pCallbackFunction;
    bool enabled = eventPipeProviderCallbackData.enabled;
    INT64 keywords = eventPipeProviderCallbackData.keywords;
    EventPipeEventLevel providerLevel = eventPipeProviderCallbackData.providerLevel;
    void *pCallbackData = eventPipeProviderCallbackData.pCallbackData;

    bool isEventFilterDescriptorInitialized = false;
    EventFilterDescriptor eventFilterDescriptor{};
    CQuickArrayBase<char> buffer;
    buffer.Init();

    if (pFilterData != NULL)
    {
        // The callback is expecting that filter data to be a concatenated list
        // of pairs of null terminated strings. The first member of the pair is
        // the key and the second is the value.
        // To convert to this format we need to convert all '=' and ';'
        // characters to '\0'.
        SString dstBuffer;
        SString(pFilterData).ConvertToUTF8(dstBuffer);

        const COUNT_T BUFFER_SIZE = dstBuffer.GetCount() + 1;
        buffer.AllocThrows(BUFFER_SIZE);
        for (COUNT_T i = 0; i < BUFFER_SIZE; ++i)
            buffer[i] = (dstBuffer[i] == '=' || dstBuffer[i] == ';') ? '\0' : dstBuffer[i];

        eventFilterDescriptor.Ptr = reinterpret_cast<ULONGLONG>(buffer.Ptr());
        eventFilterDescriptor.Size = static_cast<ULONG>(BUFFER_SIZE);
        eventFilterDescriptor.Type = 0; // EventProvider.cs: `internal enum ControllerCommand.Update`
        isEventFilterDescriptorInitialized = true;
    }

    if (pCallbackFunction != NULL && !g_fEEShutDown)
    {
        (*pCallbackFunction)(
            NULL, /* providerId */
            enabled,
            (UCHAR)providerLevel,
            keywords,
            0 /* matchAllKeywords */,
            isEventFilterDescriptorInitialized ? &eventFilterDescriptor : NULL,
            pCallbackData /* CallbackContext */);
    }

    buffer.Destroy();
}

EventPipeProviderCallbackData EventPipeProvider::PrepareCallbackData(LPCWSTR pFilterData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    EventPipeProviderCallbackData result;
    result.pFilterData = pFilterData;
    result.pCallbackFunction = m_pCallbackFunction;
    result.enabled = (m_sessions != 0);
    result.providerLevel = m_providerLevel;
    result.keywords = m_keywords;
    result.pCallbackData = m_pCallbackData;
    return result;
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

void EventPipeProvider::RefreshAllEvents(
    uint64_t sessionId,
    INT64 keywords,
    EventPipeEventLevel providerLevel)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    SListElem<EventPipeEvent *> *pElem = m_pEventList->GetHead();
    while (pElem != NULL)
    {
        EventPipeEvent *pEvent = pElem->GetValue();
        pEvent->RefreshState();
        pElem = m_pEventList->GetNext(pElem);
    }
}

#endif // FEATURE_PERFTRACING
