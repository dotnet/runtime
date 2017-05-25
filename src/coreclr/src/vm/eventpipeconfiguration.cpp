// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeeventinstance.h"
#include "eventpipeprovider.h"

#ifdef FEATURE_PERFTRACING

// {5291C09C-2660-4D6A-83A3-C383FD020DEC}
const GUID EventPipeConfiguration::s_configurationProviderID =
    { 0x5291c09c, 0x2660, 0x4d6a, { 0x83, 0xa3, 0xc3, 0x83, 0xfd, 0x2, 0xd, 0xec } };

EventPipeConfiguration::EventPipeConfiguration()
{
    STANDARD_VM_CONTRACT;

    m_enabled = false;
    m_rundownEnabled = false;
    m_circularBufferSizeInBytes = 1024 * 1024 * 1000; // Default to 1000MB.
    m_pEnabledProviderList = NULL;
    m_pProviderList = new SList<SListElem<EventPipeProvider*>>();
}

EventPipeConfiguration::~EventPipeConfiguration()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pEnabledProviderList != NULL)
    {
        delete(m_pEnabledProviderList);
        m_pEnabledProviderList = NULL;
    }

    if(m_pProviderList != NULL)
    {
        delete(m_pProviderList);
        m_pProviderList = NULL;
    }
}

void EventPipeConfiguration::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Create the configuration provider.
    m_pConfigProvider = EventPipe::CreateProvider(s_configurationProviderID);

    // Create the metadata event.
    m_pMetadataEvent = m_pConfigProvider->AddEvent(
        0,      /* eventID */
        0,      /* keywords */
        0,      /* eventVersion */
        EventPipeEventLevel::LogAlways,
        false); /* needStack */
}

bool EventPipeConfiguration::RegisterProvider(EventPipeProvider &provider)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the lock before manipulating the provider list.
    CrstHolder _crst(EventPipe::GetLock());

    // See if we've already registered this provider.
    EventPipeProvider *pExistingProvider = GetProviderNoLock(provider.GetProviderID());
    if(pExistingProvider != NULL)
    {
        return false;
    }

    // The provider has not been registered, so register it.
    m_pProviderList->InsertTail(new SListElem<EventPipeProvider*>(&provider));

    // Set the provider configuration and enable it if we know anything about the provider before it is registered.
    if(m_pEnabledProviderList != NULL)
    {
        EventPipeEnabledProvider *pEnabledProvider = m_pEnabledProviderList->GetEnabledProvider(&provider);
        if(pEnabledProvider != NULL)
        {
            provider.SetConfiguration(
                true /* providerEnabled */,
                pEnabledProvider->GetKeywords(),
                pEnabledProvider->GetLevel());
        }
    }

    return true;
}

bool EventPipeConfiguration::UnregisterProvider(EventPipeProvider &provider)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the lock before manipulating the provider list.
    CrstHolder _crst(EventPipe::GetLock());

    // Find the provider.
    SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
    while(pElem != NULL)
    {
        if(pElem->GetValue() == &provider)
        {
            break;
        }

        pElem = m_pProviderList->GetNext(pElem);
    }

    // If we found the provider, remove it.
    if(pElem != NULL)
    {
        if(m_pProviderList->FindAndRemove(pElem) != NULL)
        {
            return true;
        }
    }

    return false;
}

EventPipeProvider* EventPipeConfiguration::GetProvider(const GUID &providerID)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the lock before touching the provider list to ensure no one tries to
    // modify the list.
    CrstHolder _crst(EventPipe::GetLock());

    return GetProviderNoLock(providerID);
}

EventPipeProvider* EventPipeConfiguration::GetProviderNoLock(const GUID &providerID)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
    while(pElem != NULL)
    {
        EventPipeProvider *pProvider = pElem->GetValue();
        if(pProvider->GetProviderID() == providerID)
        {
            return pProvider;
        }

        pElem = m_pProviderList->GetNext(pElem);
    }

    return NULL;
}

size_t EventPipeConfiguration::GetCircularBufferSize() const
{
    LIMITED_METHOD_CONTRACT;

    return m_circularBufferSizeInBytes;
}

void EventPipeConfiguration::SetCircularBufferSize(size_t circularBufferSize)
{
    LIMITED_METHOD_CONTRACT;
    
    if(!m_enabled)
    {
        m_circularBufferSizeInBytes = circularBufferSize;
    }
}

void EventPipeConfiguration::Enable(
    uint circularBufferSizeInMB,
    EventPipeProviderConfiguration *pProviders,
    int numProviders)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        // Lock must be held by EventPipe::Enable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    m_circularBufferSizeInBytes = circularBufferSizeInMB * 1024 * 1024;
    m_pEnabledProviderList = new EventPipeEnabledProviderList(pProviders, static_cast<unsigned int>(numProviders));
    m_enabled = true;

    SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
    while(pElem != NULL)
    {
        EventPipeProvider *pProvider = pElem->GetValue();

        // Enable the provider if it has been configured.
        EventPipeEnabledProvider *pEnabledProvider = m_pEnabledProviderList->GetEnabledProvider(pProvider);
        if(pEnabledProvider != NULL)
        {
            pProvider->SetConfiguration(
                true /* providerEnabled */,
                pEnabledProvider->GetKeywords(),
                pEnabledProvider->GetLevel());
        }

        pElem = m_pProviderList->GetNext(pElem);
    }

}

void EventPipeConfiguration::Disable()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
    while(pElem != NULL)
    {
        EventPipeProvider *pProvider = pElem->GetValue();
        pProvider->SetConfiguration(false /* providerEnabled */, 0 /* keywords */, EventPipeEventLevel::Critical /* level */);

        pElem = m_pProviderList->GetNext(pElem);
    }

    m_enabled = false;
    m_rundownEnabled = false;

    // Free the enabled providers list.
    if(m_pEnabledProviderList != NULL)
    {
        delete(m_pEnabledProviderList);
        m_pEnabledProviderList = NULL;
    }
}

bool EventPipeConfiguration::Enabled() const
{
    LIMITED_METHOD_CONTRACT;
    return m_enabled;
}

bool EventPipeConfiguration::RundownEnabled() const
{
    LIMITED_METHOD_CONTRACT;
    return m_rundownEnabled;
}

void EventPipeConfiguration::EnableRundown()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Build the rundown configuration.
    _ASSERTE(m_pEnabledProviderList == NULL);
    const unsigned int numRundownProviders = 2;
    EventPipeProviderConfiguration rundownProviders[numRundownProviders];
    rundownProviders[0] = EventPipeProviderConfiguration(W("e13c0d23-ccbc-4e12-931b-d9cc2eee27e4"), 0x80020138, static_cast<unsigned int>(EventPipeEventLevel::Verbose)); // Public provider.
    rundownProviders[1] = EventPipeProviderConfiguration(W("a669021c-c450-4609-a035-5af59af4df18"), 0x80020138, static_cast<unsigned int>(EventPipeEventLevel::Verbose)); // Rundown provider.

    // Enable rundown.
    m_rundownEnabled = true;

    // Enable tracing.  The circular buffer size doesn't matter because we're going to write all events synchronously during rundown.
    Enable(1 /* circularBufferSizeInMB */, rundownProviders, numRundownProviders);
}

EventPipeEventInstance* EventPipeConfiguration::BuildEventMetadataEvent(EventPipeEventInstance &sourceInstance)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // The payload of the event should contain:
    // - GUID ProviderID.
    // - unsigned int EventID.
    // - unsigned int EventVersion.
    // - Optional event description payload.

    // Calculate the size of the event.
    EventPipeEvent &sourceEvent = *sourceInstance.GetEvent();
    const GUID &providerID = sourceEvent.GetProvider()->GetProviderID();
    unsigned int eventID = sourceEvent.GetEventID();
    unsigned int eventVersion = sourceEvent.GetEventVersion();
    BYTE *pPayloadData = sourceEvent.GetMetadata();
    unsigned int payloadLength = sourceEvent.GetMetadataLength();
    unsigned int instancePayloadSize = sizeof(providerID) + sizeof(eventID) + sizeof(eventVersion) + sizeof(payloadLength) + payloadLength;

    // Allocate the payload.
    BYTE *pInstancePayload = new BYTE[instancePayloadSize];

    // Fill the buffer with the payload.
    BYTE *currentPtr = pInstancePayload;

    // Write the provider ID.
    memcpy(currentPtr, (BYTE*)&providerID, sizeof(providerID));
    currentPtr += sizeof(providerID);

    // Write the event ID.
    memcpy(currentPtr, &eventID, sizeof(eventID));
    currentPtr += sizeof(eventID);

    // Write the event version.
    memcpy(currentPtr, &eventVersion, sizeof(eventVersion));
    currentPtr += sizeof(eventVersion);

    // Write the size of the metadata.
    memcpy(currentPtr, &payloadLength, sizeof(payloadLength));
    currentPtr += sizeof(payloadLength);

    // Write the incoming payload data.
    memcpy(currentPtr, pPayloadData, payloadLength);

    // Construct the event instance.
    EventPipeEventInstance *pInstance = new EventPipeEventInstance(
        *m_pMetadataEvent,
        GetCurrentThreadId(),
        pInstancePayload,
        instancePayloadSize,
        NULL /* pActivityId */,
        NULL /* pRelatedActivityId */);

    // Set the timestamp to match the source event, because the metadata event
    // will be emitted right before the source event.
    pInstance->SetTimeStamp(sourceInstance.GetTimeStamp());

    return pInstance;
}

void EventPipeConfiguration::DeleteDeferredProviders()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());

    }
    CONTRACTL_END;

    SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
    while(pElem != NULL)
    {
        EventPipeProvider *pProvider = pElem->GetValue();
        if(pProvider->GetDeleteDeferred())
        {
            // The act of deleting the provider unregisters it and removes it from the list.
            delete(pProvider);
        }

        pElem = m_pProviderList->GetNext(pElem);
    }
}

EventPipeEnabledProviderList::EventPipeEnabledProviderList(
    EventPipeProviderConfiguration *pConfigs,
    unsigned int numConfigs)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pProviders = NULL;
    m_pCatchAllProvider = NULL;
    m_numProviders = 0;

    // Test COMPLUS variable to enable tracing at start-up.
    // If tracing is enabled at start-up create the catch-all provider and always return it.
    if((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerformanceTracing) & 1) == 1)
    {
        m_pCatchAllProvider = new EventPipeEnabledProvider();
        m_pCatchAllProvider->Set(NULL, 0xFFFFFFFFFFFFFFFF, EventPipeEventLevel::Verbose);
        return;
    }

    m_pCatchAllProvider = NULL;
    m_numProviders = numConfigs;
    if(m_numProviders == 0)
    {
        return;
    }

    m_pProviders = new EventPipeEnabledProvider[m_numProviders];
    for(int i=0; i<m_numProviders; i++)
    {
        m_pProviders[i].Set(
            pConfigs[i].GetProviderName(),
            pConfigs[i].GetKeywords(),
            (EventPipeEventLevel)pConfigs[i].GetLevel());
    }
}

EventPipeEnabledProviderList::~EventPipeEnabledProviderList()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pProviders != NULL)
    {
        delete[] m_pProviders;
        m_pProviders = NULL;
    }
    if(m_pCatchAllProvider != NULL)
    {
        delete(m_pCatchAllProvider);
        m_pCatchAllProvider = NULL;
    }
}

EventPipeEnabledProvider* EventPipeEnabledProviderList::GetEnabledProvider(
    EventPipeProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If tracing was enabled on start-up, all events should be on (this is a diagnostic config).
    if(m_pCatchAllProvider != NULL)
    {
        return m_pCatchAllProvider;
    }

    if(m_pProviders == NULL)
    {
        return NULL;
    }

    // TEMPORARY: Convert the provider GUID to a string.
    const unsigned int guidSize = 39;
    WCHAR wszProviderID[guidSize];
    if(!StringFromGUID2(pProvider->GetProviderID(), wszProviderID, guidSize))
    {
        wszProviderID[0] = '\0';
    }

    // Strip off the {}.
    SString providerNameStr(&wszProviderID[1], guidSize-3);
    LPCWSTR providerName = providerNameStr.GetUnicode();

    EventPipeEnabledProvider *pEnabledProvider = NULL;
    for(int i=0; i<m_numProviders; i++)
    {
        EventPipeEnabledProvider *pCandidate = &m_pProviders[i];
        if(pCandidate != NULL)
        {
            if(wcscmp(providerName, pCandidate->GetProviderName()) == 0)
            {
                pEnabledProvider = pCandidate;
                break;
            }
        }
    }

    return pEnabledProvider;
}

EventPipeEnabledProvider::EventPipeEnabledProvider()
{
    LIMITED_METHOD_CONTRACT;
    m_pProviderName = NULL;
    m_keywords = 0;
}

EventPipeEnabledProvider::~EventPipeEnabledProvider()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pProviderName != NULL)
    {
        delete[] m_pProviderName;
        m_pProviderName = NULL;
    }
}

void EventPipeEnabledProvider::Set(LPCWSTR providerName, UINT64 keywords, EventPipeEventLevel loggingLevel)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pProviderName != NULL)
    {
        delete(m_pProviderName);
        m_pProviderName = NULL;
    }

    if(providerName != NULL)
    {
        unsigned int bufSize = wcslen(providerName) + 1;
        m_pProviderName = new WCHAR[bufSize];
        wcscpy_s(m_pProviderName, bufSize, providerName);
    }
    m_keywords = keywords;
    m_loggingLevel = loggingLevel;
}

LPCWSTR EventPipeEnabledProvider::GetProviderName() const
{
    LIMITED_METHOD_CONTRACT;
    return m_pProviderName;
}

UINT64 EventPipeEnabledProvider::GetKeywords() const
{
    LIMITED_METHOD_CONTRACT;
    return m_keywords;
}

EventPipeEventLevel EventPipeEnabledProvider::GetLevel() const
{
    LIMITED_METHOD_CONTRACT;
    return m_loggingLevel;
}

#endif // FEATURE_PERFTRACING
