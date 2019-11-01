// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeeventinstance.h"
#include "eventpipesessionprovider.h"
#include "eventpipeprovider.h"
#include "eventpipesession.h"

#ifdef FEATURE_PERFTRACING

const WCHAR *EventPipeConfiguration::s_configurationProviderName = W("Microsoft-DotNETCore-EventPipeConfiguration");

void EventPipeConfiguration::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(m_pProviderList == nullptr);
        PRECONDITION(m_pConfigProvider == nullptr);
        PRECONDITION(m_pMetadataEvent == nullptr);
    }
    CONTRACTL_END;

    m_pProviderList = new SList<SListElem<EventPipeProvider *>>();

    EventPipe::RunWithCallbackPostponed([&](EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue) {
        // Create the configuration provider.
        m_pConfigProvider = CreateProvider(SL(s_configurationProviderName), NULL, NULL, pEventPipeProviderCallbackDataQueue);
    });

    // Create the metadata event.
    m_pMetadataEvent = m_pConfigProvider->AddEvent(
        0, /* eventID */
        0, /* keywords */
        0, /* eventVersion */
        EventPipeEventLevel::LogAlways,
        false); /* needStack */
}

void EventPipeConfiguration::Shutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pConfigProvider != nullptr)
    {
        // This unregisters the provider, which takes a
        // HOST_BREAKABLE lock
        EX_TRY
        {
            DeleteProvider(m_pConfigProvider);
        }
        EX_CATCH {}
        EX_END_CATCH(SwallowAllExceptions);

        m_pConfigProvider = nullptr;
    }

    if (m_pProviderList != nullptr)
    {
        // We swallow exceptions here because the HOST_BREAKABLE
        // lock may throw and this destructor gets called in throw
        // intolerant places. If that happens the provider list will leak
        EX_TRY
        {
            // Take the lock before manipulating the list.
            CrstHolder _crst(EventPipe::GetLock());

            SListElem<EventPipeProvider *> *pElem = m_pProviderList->GetHead();
            while (pElem != nullptr)
            {
                // We don't delete provider itself because it can be in-use
                SListElem<EventPipeProvider *> *pCurElem = pElem;
                pElem = m_pProviderList->GetNext(pElem);
                delete pCurElem;
            }

            delete m_pProviderList;
        }
        EX_CATCH {}
        EX_END_CATCH(SwallowAllExceptions);

        m_pProviderList = nullptr;
    }
}

EventPipeProvider *EventPipeConfiguration::CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData, EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Allocate a new provider.
    EventPipeProvider *pProvider = new EventPipeProvider(this, providerName, pCallbackFunction, pCallbackData);

    // Register the provider with the configuration system.
    RegisterProvider(*pProvider, pEventPipeProviderCallbackDataQueue);

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
        return;

    // Unregister the provider.
    UnregisterProvider(*pProvider);

    // Free the provider itself.
    delete pProvider;
}

bool EventPipeConfiguration::RegisterProvider(EventPipeProvider &provider, EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // See if we've already registered this provider.
    EventPipeProvider *pExistingProvider = GetProviderNoLock(provider.GetProviderName());
    if (pExistingProvider != nullptr)
        return false;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != nullptr)
    {
        // The provider has not been registered, so register it.
        m_pProviderList->InsertTail(new SListElem<EventPipeProvider *>(&provider));
    }

    INT64 keywordForAllSessions;
    EventPipeEventLevel levelForAllSessions;
    ComputeKeywordAndLevel(provider, /* out */ keywordForAllSessions, /* out */ levelForAllSessions);

    EventPipe::ForEachSession([&](EventPipeSession &session) {
        // Set the provider configuration and enable it if it has been requested by a session.
        EventPipeSessionProvider *pSessionProvider = GetSessionProvider(session, &provider);
        if (pSessionProvider == nullptr)
            return;

        EventPipeProviderCallbackData eventPipeProviderCallbackData = provider.SetConfiguration(
            keywordForAllSessions,
            levelForAllSessions,
            session.GetMask(),
            pSessionProvider->GetKeywords(),
            pSessionProvider->GetLevel(),
            pSessionProvider->GetFilterData());
        pEventPipeProviderCallbackDataQueue->Enqueue(&eventPipeProviderCallbackData);
    });

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
        SListElem<EventPipeProvider *> *pElem = m_pProviderList->GetHead();
        while (pElem != NULL)
        {
            if (pElem->GetValue() == &provider)
            {
                break;
            }

            pElem = m_pProviderList->GetNext(pElem);
        }

        // If we found the provider, remove it.
        if (pElem != NULL)
        {
            if (m_pProviderList->FindAndRemove(pElem) != NULL)
            {
                delete pElem;
                return true;
            }
        }
    }

    return false;
}

EventPipeProvider *EventPipeConfiguration::GetProvider(const SString &providerName)
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

EventPipeProvider *EventPipeConfiguration::GetProviderNoLock(const SString &providerName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider *> *pElem = m_pProviderList->GetHead();
        while (pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();
            if (pProvider->GetProviderName().Equals(providerName))
            {
                return pProvider;
            }

            pElem = m_pProviderList->GetNext(pElem);
        }
    }

    return NULL;
}

EventPipeSessionProvider *EventPipeConfiguration::GetSessionProvider(const EventPipeSession &session, const EventPipeProvider *pProvider) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    return session.GetSessionProvider(pProvider);
}

void EventPipeConfiguration::ComputeKeywordAndLevel(const EventPipeProvider& provider, INT64& keywordForAllSessions, EventPipeEventLevel& levelForAllSessions) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;
    keywordForAllSessions = 0;
    levelForAllSessions = EventPipeEventLevel::LogAlways;
    EventPipe::ForEachSession([&](EventPipeSession &session) {
        EventPipeSessionProvider *pSessionProvider = GetSessionProvider(session, &provider);
        if (pSessionProvider != nullptr)
        {
            INT64 sessionKeyword = pSessionProvider->GetKeywords();
            EventPipeEventLevel sessionLevel = pSessionProvider->GetLevel();
            keywordForAllSessions = keywordForAllSessions | sessionKeyword;
            levelForAllSessions = (sessionLevel > levelForAllSessions) ? sessionLevel : levelForAllSessions;
        }
    });
}

INT64 EventPipeConfiguration::ComputeEventEnabledMask(const EventPipeProvider& provider, INT64 keywords, EventPipeEventLevel eventLevel) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;
    INT64 result = 0;
    EventPipe::ForEachSession([&](EventPipeSession &session) {
        EventPipeSessionProvider *pSessionProvider = GetSessionProvider(session, &provider);
        if (pSessionProvider != nullptr)
        {
            INT64 sessionKeyword = pSessionProvider->GetKeywords();
            EventPipeEventLevel sessionLevel = pSessionProvider->GetLevel();
            // The event is enabled if:
            //  - The provider is enabled.
            //  - The event keywords are unspecified in the manifest (== 0) or when masked with the enabled config are != 0.
            //  - The event level is LogAlways or the provider's verbosity level is set to greater than the event's verbosity level in the manifest.
            bool providerEnabled = provider.Enabled();
            bool keywordEnabled = (keywords == 0) || ((sessionKeyword & keywords) != 0);
            bool levelEnabled = ((eventLevel == EventPipeEventLevel::LogAlways) || (sessionLevel >= eventLevel));
            if (providerEnabled && keywordEnabled && levelEnabled)
            {
                result = result | session.GetMask();
            }
        }
    });
    return result;
}

void EventPipeConfiguration::Enable(EventPipeSession &session, EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider *> *pElem = m_pProviderList->GetHead();
        while (pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();

            // Enable the provider if it has been configured.
            EventPipeSessionProvider *pSessionProvider = GetSessionProvider(session, pProvider);
            if (pSessionProvider != NULL)
            {
                INT64 keywordForAllSessions;
                EventPipeEventLevel levelForAllSessions;
                ComputeKeywordAndLevel(*pProvider, /* out */ keywordForAllSessions, /* out */ levelForAllSessions);

                EventPipeProviderCallbackData eventPipeProviderCallbackData =
                    pProvider->SetConfiguration(
                        keywordForAllSessions,
                        levelForAllSessions,
                        session.GetMask(),
                        pSessionProvider->GetKeywords(),
                        pSessionProvider->GetLevel(),
                        pSessionProvider->GetFilterData());
                pEventPipeProviderCallbackDataQueue->Enqueue(&eventPipeProviderCallbackData);
            }

            pElem = m_pProviderList->GetNext(pElem);
        }
    }
}

void EventPipeConfiguration::Disable(const EventPipeSession &session, EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider *> *pElem = m_pProviderList->GetHead();
        while (pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();
            if (pProvider->IsEnabled(session.GetMask()))
            {
                EventPipeSessionProvider *pSessionProvider = GetSessionProvider(session, pProvider);
                if (pSessionProvider != nullptr)
                {
                    INT64 keywordForAllSessions;
                    EventPipeEventLevel levelForAllSessions;
                    ComputeKeywordAndLevel(*pProvider, /* out */ keywordForAllSessions, /* out */ levelForAllSessions);

                    EventPipeProviderCallbackData eventPipeProviderCallbackData = pProvider->UnsetConfiguration(
                        keywordForAllSessions,
                        levelForAllSessions,
                        session.GetMask(),
                        pSessionProvider->GetKeywords(),
                        pSessionProvider->GetLevel(),
                        pSessionProvider->GetFilterData());
                    pEventPipeProviderCallbackDataQueue->Enqueue(&eventPipeProviderCallbackData);
                }
            }

            pElem = m_pProviderList->GetNext(pElem);
        }
    }
}

EventPipeEventInstance *EventPipeConfiguration::BuildEventMetadataEvent(EventPipeEventInstance &sourceInstance, unsigned int metadataId)
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

    memcpy(currentPtr, (BYTE *)providerName.GetUnicode(), providerNameLength);
    currentPtr += providerNameLength;

    // Write the incoming payload data.
    memcpy(currentPtr, pPayloadData, payloadLength);

    // Construct the event instance.
    EventPipeEventInstance *pInstance = new EventPipeEventInstance(
        *m_pMetadataEvent,
        EventPipe::GetCurrentProcessorNumber(),
#ifdef FEATURE_PAL
        PAL_GetCurrentOSThreadId(),
#else
        GetCurrentThreadId(),
#endif
        pInstancePayload,
        instancePayloadSize,
        NULL /* pActivityId */,
        NULL /* pRelatedActivityId */);
    _ASSERTE(!m_pMetadataEvent->NeedStack());

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
        MODE_PREEMPTIVE;
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // The provider list should be non-NULL, but can be NULL on shutdown.
    if (m_pProviderList != NULL)
    {
        SListElem<EventPipeProvider *> *pElem = m_pProviderList->GetHead();
        while (pElem != NULL)
        {
            EventPipeProvider *pProvider = pElem->GetValue();
            pElem = m_pProviderList->GetNext(pElem);
            if (pProvider->GetDeleteDeferred())
            {
                DeleteProvider(pProvider);
            }
        }
    }
}
#endif // FEATURE_PERFTRACING
