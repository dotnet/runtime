// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeevent.h"
#include "eventpipeprovider.h"

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

INT64 EventPipeProvider::ComputeEventEnabledMask(INT64 keywords, EventPipeEventLevel eventLevel) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    return m_pConfig->ComputeEventEnabledMask((*this), keywords, eventLevel);
}

EventPipeProviderCallbackData EventPipeProvider::SetConfiguration(
    INT64 keywordsForAllSessions,
    EventPipeEventLevel providerLevelForAllSessions,
    uint64_t sessionMask,
    INT64 keywords,
    EventPipeEventLevel providerLevel,
    LPCWSTR pFilterData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION((m_sessions & sessionMask) == 0);
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    m_sessions |= sessionMask;

    m_keywords = keywordsForAllSessions;
    m_providerLevel = providerLevelForAllSessions;

    RefreshAllEvents();
    return PrepareCallbackData(m_keywords, m_providerLevel, pFilterData);
}

EventPipeProviderCallbackData EventPipeProvider::UnsetConfiguration(
    INT64 keywordsForAllSessions,
    EventPipeEventLevel providerLevelForAllSessions,
    uint64_t sessionMask,
    INT64 keywords,
    EventPipeEventLevel providerLevel,
    LPCWSTR pFilterData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION((m_sessions & sessionMask) != 0);
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (m_sessions & sessionMask)
        m_sessions &= ~sessionMask;

    m_keywords = keywordsForAllSessions;
    m_providerLevel = providerLevelForAllSessions;

    RefreshAllEvents();
    return PrepareCallbackData(m_keywords, m_providerLevel, pFilterData);
}

EventPipeEvent *EventPipeProvider::AddEvent(unsigned int eventID, INT64 keywords, unsigned int eventVersion, EventPipeEventLevel level, bool needStack,
    BYTE *pMetadata, unsigned int metadataLength)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
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
        GC_TRIGGERS;
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
        // characters to '\0', except when in a quoted string.
        SString dstBuffer;
        SString(pFilterData).ConvertToUTF8(dstBuffer);

        const COUNT_T BUFFER_SIZE = dstBuffer.GetCount() + 1;
        buffer.AllocThrows(BUFFER_SIZE);
        BOOL isQuotedValue = false;
        COUNT_T j = 0;
        for (COUNT_T i = 0; i < BUFFER_SIZE; ++i)
        {
            // if a value is a quoted string, leave the quotes out from the destination
            // and don't replace `=` or `;` characters until leaving the quoted section
            // e.g., key="a;value=";foo=bar --> { key\0a;value=\0foo\0bar\0 }
            if (dstBuffer[i] == '"')
            {
                isQuotedValue = !isQuotedValue;
                continue;
            }
            buffer[j++] = ((dstBuffer[i] == '=' || dstBuffer[i] == ';') && !isQuotedValue) ? '\0' : dstBuffer[i];
        }

        // In case we skipped over quotes in the filter string, shrink the buffer size accordingly
        if (j < dstBuffer.GetCount())
            buffer.Shrink(j + 1);

        eventFilterDescriptor.Ptr = reinterpret_cast<ULONGLONG>(buffer.Ptr());
        eventFilterDescriptor.Size = static_cast<ULONG>(buffer.Size());
        eventFilterDescriptor.Type = 0; // EventProvider.cs: `internal enum ControllerCommand.Update`
        isEventFilterDescriptorInitialized = true;
    }

    // NOTE: When we call the callback, we pass in enabled (which is either 1 or 0) as the ControlCode.
    // If we want to add new ControlCode, we have to make corresponding change in eventtrace.cpp:EtwCallbackCommon
    // to address this. See https://github.com/dotnet/runtime/pull/36733 for more discussions on this.
    if (pCallbackFunction != NULL && !g_fEEShutDown)
    {
        (*pCallbackFunction)(
            NULL, /* providerId */
            enabled, /* ControlCode */
            (UCHAR)providerLevel,
            keywords,
            0 /* matchAllKeywords */,
            isEventFilterDescriptorInitialized ? &eventFilterDescriptor : NULL,
            pCallbackData /* CallbackContext */);
    }

    buffer.Destroy();
}

EventPipeProviderCallbackData EventPipeProvider::PrepareCallbackData(
        INT64 keywords,
        EventPipeEventLevel providerLevel,
        LPCWSTR pFilterData)
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
    result.providerLevel = providerLevel;
    result.keywords = keywords;
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
    // EventSources will be collected once they ungregister themselves,
    // so we can't call back in to them.
    m_pCallbackFunction = NULL;
}

void EventPipeProvider::RefreshAllEvents()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
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
