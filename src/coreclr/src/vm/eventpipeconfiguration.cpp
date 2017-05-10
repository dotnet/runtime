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
    m_pConfigProvider = new EventPipeProvider(s_configurationProviderID);

    // Create the metadata event.
    m_pMetadataEvent = m_pConfigProvider->AddEvent(
        0,      /* keywords */
        0,      /* eventID */
        0,      /* eventVersion */
        EventPipeEventLevel::Critical,
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

    // TODO: Set the provider configuration and enable it if we know
    // anything about the provider before it is registered.
    provider.SetConfiguration(true /* providerEnabled */, 0xFFFFFFFFFFFFFFFF /* keywords */, EventPipeEventLevel::Verbose /* level */);

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

void EventPipeConfiguration::Enable()
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

    SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
    while(pElem != NULL)
    {
        // TODO: Only enable the providers that have been explicitly enabled with specified keywords/level.
        EventPipeProvider *pProvider = pElem->GetValue();
        pProvider->SetConfiguration(true /* providerEnabled */, 0xFFFFFFFFFFFFFFFF /* keywords */, EventPipeEventLevel::Verbose /* level */);

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
}

EventPipeEventInstance* EventPipeConfiguration::BuildEventMetadataEvent(EventPipeEvent &sourceEvent, BYTE *pPayloadData, unsigned int payloadLength)
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
    const GUID &providerID = sourceEvent.GetProvider()->GetProviderID();
    unsigned int eventID = sourceEvent.GetEventID();
    unsigned int eventVersion = sourceEvent.GetEventVersion();
    unsigned int instancePayloadSize = sizeof(providerID) + sizeof(eventID) + sizeof(eventVersion) + payloadLength;

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

    // Write the incoming payload data.
    memcpy(currentPtr, pPayloadData, payloadLength);

    // Construct the event instance.
    EventPipeEventInstance *pInstance = new EventPipeEventInstance(
        *m_pMetadataEvent,
        GetCurrentThreadId(),
        pInstancePayload,
        instancePayloadSize);

    return pInstance;
}

#endif // FEATURE_PERFTRACING
