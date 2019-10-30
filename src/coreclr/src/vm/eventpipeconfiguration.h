// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_CONFIGURATION_H__
#define __EVENTPIPE_CONFIGURATION_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "slist.h"

class EventPipeSessionProvider;
class EventPipeEvent;
class EventPipeEventInstance;
class EventPipeProvider;
class EventPipeSession;

class EventPipeConfiguration
{
public:
    // Perform initialization that cannot be performed in the constructor.
    void Initialize();

    // Perform cleanup that cannot be performed in the destructor.
    void Shutdown();

    // Create a new provider.
    EventPipeProvider *CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData, EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue);

    // Delete a provider.
    void DeleteProvider(EventPipeProvider *pProvider);

    // Register a provider.
    bool RegisterProvider(EventPipeProvider &provider, EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue);

    // Unregister a provider.
    bool UnregisterProvider(EventPipeProvider &provider);

    // Get the provider with the specified provider ID if it exists.
    EventPipeProvider *GetProvider(const SString &providerID);

    // Enable a session in the event pipe.
    void Enable(
        EventPipeSession &session,
        EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue);

    // Disable a session in the event pipe.
    void Disable(
        const EventPipeSession &session,
        EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue);

    // Get the event used to write metadata to the event stream.
    EventPipeEventInstance *BuildEventMetadataEvent(EventPipeEventInstance &sourceInstance, unsigned int metdataId);

    // Delete deferred providers.
    void DeleteDeferredProviders();
    
    // Compute the enabled bit mask, the ith bit is 1 iff an event with the given (provider, keywords, eventLevel) is enabled for the ith session.
    INT64 ComputeEventEnabledMask(const EventPipeProvider& provider, INT64 keywords, EventPipeEventLevel eventLevel) const;
private:
    // Get the provider without taking the lock.
    EventPipeProvider *GetProviderNoLock(const SString &providerID);

    // Get the enabled provider.
    EventPipeSessionProvider *GetSessionProvider(const EventPipeSession &session, const EventPipeProvider *pProvider) const;

    // Compute the keyword union and maximum level for a provider across all sessions
    void ComputeKeywordAndLevel(const EventPipeProvider& provider, INT64& keywordsForAllSessions, EventPipeEventLevel& levelForAllSessions) const;

    // The list of event pipe providers.
    SList<SListElem<EventPipeProvider *>> *m_pProviderList = nullptr;

    // The provider used to write configuration events to the event stream.
    EventPipeProvider *m_pConfigProvider = nullptr;

    // The event used to write event information to the event stream.
    EventPipeEvent *m_pMetadataEvent = nullptr;

    // The provider name for the configuration event pipe provider.
    // This provider is used to emit configuration events.
    const static WCHAR *s_configurationProviderName;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_CONFIGURATION_H__
