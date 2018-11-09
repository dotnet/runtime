// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeeventinstance.h"
#include "eventpipeprovider.h"
#include "eventpipesession.h"

#ifdef FEATURE_PERFTRACING

const WCHAR* EventPipeConfiguration::s_configurationProviderName = W("Microsoft-DotNETCore-EventPipeConfiguration");

EventPipeConfiguration::EventPipeConfiguration()
{
    STANDARD_VM_CONTRACT;

    m_enabled = false;
    m_rundownEnabled = false;
    m_pRundownThread = NULL;
    m_pConfigProvider = NULL;
    m_pSession = NULL;
    m_pProviderList = new SList<SListElem<EventPipeProvider*>>();
}

EventPipeConfiguration::~EventPipeConfiguration()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pConfigProvider != NULL)
    {
        // This unregisters the provider, which takes a
        // HOST_BREAKABLE lock
        EX_TRY
        {
          DeleteProvider(m_pConfigProvider);
          m_pConfigProvider = NULL;
        }
        EX_CATCH { }
        EX_END_CATCH(SwallowAllExceptions);
    }
    if(m_pSession != NULL)
    {
        DeleteSession(m_pSession);
        m_pSession = NULL;
    }

    if(m_pProviderList != NULL)
    {
        // We swallow exceptions here because the HOST_BREAKABLE
        // lock may throw and this destructor gets called in throw
        // intolerant places. If that happens the provider list will leak
        EX_TRY
        {
            // Take the lock before manipulating the list.
            CrstHolder _crst(EventPipe::GetLock());

            SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
            while(pElem != NULL)
            {
                // We don't delete provider itself because it can be in-use
                SListElem<EventPipeProvider*> *pCurElem = pElem;
                pElem = m_pProviderList->GetNext(pElem);
                delete(pCurElem);
            }

            delete(m_pProviderList);
        }
        EX_CATCH { }
        EX_END_CATCH(SwallowAllExceptions);

        m_pProviderList = NULL;
    }
}

void EventPipeConfiguration::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Create the configuration provider.
    m_pConfigProvider = CreateProvider(SL(s_configurationProviderName), NULL, NULL);

    // Create the metadata event.
    m_pMetadataEvent = m_pConfigProvider->AddEvent(
        0,      /* eventID */
        0,      /* keywords */
        0,      /* eventVersion */
        EventPipeEventLevel::LogAlways,
        false); /* needStack */
}

EventPipeProvider* EventPipeConfiguration::CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Allocate a new provider.
    EventPipeProvider *pProvider = new EventPipeProvider(this, providerName, pCallbackFunction, pCallbackData);

    // Register the provider with the configuration system.
    RegisterProvider(*pProvider);

    return pProvider;
}

void EventPipeConfiguration::DeleteProvider(EventPipeProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pProvider != NULL);
    }
    CONTRACTL_END;

    if (pProvider == NULL)
    {
        return;
    }

    // Unregister the provider.
    UnregisterProvider(*pProvider);

    // Free the provider itself.
    delete(pProvider);
}


bool EventPipeConfiguration::RegisterProvider(EventPipeProvider &provider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the lock before manipulating the provider list.
    CrstHolder _crst(EventPipe::GetLock());

    // See if we've already registered this provider.
    EventPipeProvider *pExistingProvider = GetProviderNoLock(provider.GetProviderName());
    if(pExistingProvider != NULL)
    {
        return false;
    }

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        // The provider has not been registered, so register it.
        m_pProviderList->InsertTail(new SListElem<EventPipeProvider*>(&provider));
    }

    // Set the provider configuration and enable it if it has been requested by a session.
    if(m_pSession != NULL)
    {
        EventPipeSessionProvider *pSessionProvider = GetSessionProvider(m_pSession, &provider);
        if(pSessionProvider != NULL)
        {
            provider.SetConfiguration(
                true /* providerEnabled */,
                pSessionProvider->GetKeywords(),
                pSessionProvider->GetLevel(),
                pSessionProvider->GetFilterData());
        }
    }

    return true;
}

bool EventPipeConfiguration::UnregisterProvider(EventPipeProvider &provider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the lock before manipulating the provider list.
    CrstHolder _crst(EventPipe::GetLock());

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
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
                delete(pElem);
                return true;
            }
        }
    }

    return false;
}

EventPipeProvider* EventPipeConfiguration::GetProvider(const SString &providerName)
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

    return GetProviderNoLock(providerName);
}

EventPipeProvider* EventPipeConfiguration::GetProviderNoLock(const SString &providerName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
        while(pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();
            if(pProvider->GetProviderName().Equals(providerName))
            {
                return pProvider;
            }

            pElem = m_pProviderList->GetNext(pElem);
        }
    }

    return NULL;
}

EventPipeSessionProvider* EventPipeConfiguration::GetSessionProvider(EventPipeSession *pSession, EventPipeProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    EventPipeSessionProvider *pRet = NULL;
    if(pSession != NULL)
    {
       pRet = pSession->GetSessionProvider(pProvider);
    }
    return pRet;
}

size_t EventPipeConfiguration::GetCircularBufferSize() const
{
    LIMITED_METHOD_CONTRACT;

    size_t ret = 0;
    if(m_pSession != NULL)
    {
        ret = m_pSession->GetCircularBufferSize();
    }
    return ret;
}

EventPipeSession* EventPipeConfiguration::CreateSession(EventPipeSessionType sessionType, unsigned int circularBufferSizeInMB, EventPipeProviderConfiguration *pProviders, unsigned int numProviders, UINT64 multiFileTraceLengthInSeconds)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return new EventPipeSession(sessionType, circularBufferSizeInMB, pProviders, numProviders, multiFileTraceLengthInSeconds);
}

void EventPipeConfiguration::DeleteSession(EventPipeSession *pSession)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pSession != NULL);
        PRECONDITION(m_enabled == false);
    }
    CONTRACTL_END;

    // TODO: Multiple session support will require individual enabled bits.
    if(pSession != NULL && !m_enabled)
    {
        delete(pSession);
    }
}

void EventPipeConfiguration::Enable(EventPipeSession *pSession)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pSession != NULL);
        // Lock must be held by EventPipe::Enable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    m_pSession = pSession;
    m_enabled = true;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
        while(pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();

            // Enable the provider if it has been configured.
            EventPipeSessionProvider *pSessionProvider = GetSessionProvider(m_pSession, pProvider);
            if(pSessionProvider != NULL)
            {
                pProvider->SetConfiguration(
                    true /* providerEnabled */,
                    pSessionProvider->GetKeywords(),
                    pSessionProvider->GetLevel(),
                    pSessionProvider->GetFilterData());
            }

            pElem = m_pProviderList->GetNext(pElem);
        }
    }
}

void EventPipeConfiguration::Disable(EventPipeSession *pSession)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        // TODO: Multiple session support will require that the session be specified.
        PRECONDITION(pSession != NULL);
        PRECONDITION(pSession == m_pSession);
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
        while(pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();
            pProvider->SetConfiguration(
                false /* providerEnabled */,
                0 /* keywords */,
                EventPipeEventLevel::Critical /* level */,
                NULL /* filterData */);

            pElem = m_pProviderList->GetNext(pElem);
        }
    }

    m_enabled = false;
    m_rundownEnabled = false;
    m_pRundownThread = NULL;
    m_pSession = NULL;
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

void EventPipeConfiguration::EnableRundown(EventPipeSession *pSession)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pSession != NULL);
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Build the rundown configuration.
    _ASSERTE(m_pSession == NULL);

    // Enable rundown and keep track of the rundown thread.
    // TODO: Move this into EventPipeSession once Enable takes an EventPipeSession object.
    m_pRundownThread = GetThread();
    _ASSERTE(m_pRundownThread != NULL);
    m_rundownEnabled = true;

    // Enable tracing.
    Enable(pSession);
}

EventPipeEventInstance* EventPipeConfiguration::BuildEventMetadataEvent(EventPipeEventInstance &sourceInstance, unsigned int metadataId)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // The payload of the event should contain:
    // - Metadata ID
    // - GUID ProviderID.
    // - Optional event description payload.

    // Calculate the size of the event.
    EventPipeEvent &sourceEvent = *sourceInstance.GetEvent();
    const SString &providerName = sourceEvent.GetProvider()->GetProviderName();
    BYTE *pPayloadData = sourceEvent.GetMetadata();
    unsigned int payloadLength = sourceEvent.GetMetadataLength();
    unsigned int providerNameLength = (providerName.GetCount() + 1) * sizeof(WCHAR);
    unsigned int instancePayloadSize = sizeof(metadataId) + providerNameLength + payloadLength;

    // Allocate the payload.
    BYTE *pInstancePayload = new BYTE[instancePayloadSize];

    // Fill the buffer with the payload.
    BYTE *currentPtr = pInstancePayload;

    memcpy(currentPtr, &metadataId, sizeof(metadataId));
    currentPtr += sizeof(metadataId);

    memcpy(currentPtr, (BYTE*)providerName.GetUnicode(), providerNameLength);
    currentPtr += providerNameLength;

    // Write the incoming payload data.
    memcpy(currentPtr, pPayloadData, payloadLength);

    // Construct the event instance.
    EventPipeEventInstance *pInstance = new EventPipeEventInstance(
        *EventPipe::s_pSession,
        *m_pMetadataEvent,
        GetCurrentThreadId(),
        pInstancePayload,
        instancePayloadSize,
        NULL /* pActivityId */,
        NULL /* pRelatedActivityId */);

    // Set the timestamp to match the source event, because the metadata event
    // will be emitted right before the source event.
    pInstance->SetTimeStamp(*sourceInstance.GetTimeStamp());

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

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider*> *pElem = m_pProviderList->GetHead();
        while(pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();
            pElem = m_pProviderList->GetNext(pElem);
            if(pProvider->GetDeleteDeferred())
            {
                DeleteProvider(pProvider);
            }
        }
    }
}
#endif // FEATURE_PERFTRACING
