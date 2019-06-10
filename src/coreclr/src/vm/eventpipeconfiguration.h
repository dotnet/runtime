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

    // Get the status of the event pipe.
    bool Enabled() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_activeSessions != 0);
    }

    // Get the event used to write metadata to the event stream.
    EventPipeEventInstance *BuildEventMetadataEvent(EventPipeEventInstance &sourceInstance, unsigned int metdataId);

    // Delete deferred providers.
    void DeleteDeferredProviders();

    // Create a new session.
    EventPipeSession *CreateSession(
        LPCWSTR strOutputPath,
        IpcStream *const pStream,
        EventPipeSessionType sessionType,
        EventPipeSerializationFormat format,
        unsigned int circularBufferSizeInMB,
        const EventPipeProviderConfiguration *pProviders,
        uint32_t numProviders,
        bool rundownEnabled = false);

    // Delete a session.
    void DeleteSession(EventPipeSession *pSession);

    // Check that a single bit is set.
    static bool IsValidId(EventPipeSessionID id)
    {
        return (id > 0) && ((id & (id - 1)) == 0);
    }

    // Check that a session Id is enabled.
    bool IsSessionIdValid(EventPipeSessionID id)
    {
        return IsValidId(id) && (m_activeSessions & id);
    }

private:
    // Helper function used to locate a free index in the range 0 - EventPipe::MaxNumberOfSessions
    // Returns EventPipe::MaxNumberOfSessions if there are no free indexes
    unsigned int GenerateSessionIndex() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(EventPipe::MaxNumberOfSessions == 64);
        uint64_t id = 1;
        for (unsigned int i = 0; i < 64; ++i, id <<= i)
            if ((m_activeSessions & id) == 0)
                return i;
        return EventPipe::MaxNumberOfSessions;
    }

    // Get the provider without taking the lock.
    EventPipeProvider *GetProviderNoLock(const SString &providerID);

    // Get the enabled provider.
    EventPipeSessionProvider *GetSessionProvider(EventPipeSession &session, EventPipeProvider *pProvider);

    // The list of event pipe providers.
    SList<SListElem<EventPipeProvider *>> *m_pProviderList = nullptr;

    // The provider used to write configuration events to the event stream.
    EventPipeProvider *m_pConfigProvider = nullptr;

    // The event used to write event information to the event stream.
    EventPipeEvent *m_pMetadataEvent = nullptr;

    // The provider name for the configuration event pipe provider.
    // This provider is used to emit configuration events.
    const static WCHAR *s_configurationProviderName;

    // Bitmask tracking EventPipe active sessions.
    uint64_t m_activeSessions = 0;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_CONFIGURATION_H__
