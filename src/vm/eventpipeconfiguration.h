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
struct EventPipeProviderConfiguration;
class EventPipeSession;
enum class EventPipeSessionType;
class EventPipeSessionProvider;

enum class EventPipeEventLevel
{
    LogAlways,
    Critical,
    Error,
    Warning,
    Informational,
    Verbose
};

class EventPipeConfiguration
{
public:

    EventPipeConfiguration();
    ~EventPipeConfiguration();

    // Perform initialization that cannot be performed in the constructor.
    void Initialize();

    // Create a new provider.
    EventPipeProvider* CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData);

    // Delete a provider.
    void DeleteProvider(EventPipeProvider *pProvider);

    // Register a provider.
    bool RegisterProvider(EventPipeProvider &provider);

    // Unregister a provider.
    bool UnregisterProvider(EventPipeProvider &provider);

    // Get the provider with the specified provider ID if it exists.
    EventPipeProvider* GetProvider(const SString &providerID);

    // Create a new session.
    EventPipeSession* CreateSession(EventPipeSessionType sessionType, unsigned int circularBufferSizeInMB, EventPipeProviderConfiguration *pProviders, unsigned int numProviders, UINT64 multiFileTraceLengthInSeconds = 0);

    // Delete a session.
    void DeleteSession(EventPipeSession *pSession);

    // Get the configured size of the circular buffer.
    size_t GetCircularBufferSize() const;

    // Enable a session in the event pipe.
    void Enable(EventPipeSession *pSession);

    // Disable a session in the event pipe.
    void Disable(EventPipeSession *pSession);

    // Get the status of the event pipe.
    bool Enabled() const;

    // Determine if rundown is enabled.
    bool RundownEnabled() const;

    // Enable rundown using the specified configuration.
    void EnableRundown(EventPipeSession *pSession);

    // Get the event used to write metadata to the event stream.
    EventPipeEventInstance* BuildEventMetadataEvent(EventPipeEventInstance &sourceInstance, unsigned int metdataId);

    // Delete deferred providers.
    void DeleteDeferredProviders();

    // Determine if the specified thread is the rundown thread.
    // Used during rundown to ignore events from all other threads so that we don't corrupt the trace file.
    inline bool IsRundownThread(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;

        return (pThread == m_pRundownThread);
    }

private:

    // Get the provider without taking the lock.
    EventPipeProvider* GetProviderNoLock(const SString &providerID);

    // Get the enabled provider.
    EventPipeSessionProvider* GetSessionProvider(EventPipeSession *pSession, EventPipeProvider *pProvider);

    // The one and only EventPipe session.
    EventPipeSession *m_pSession;

    // Determines whether or not the event pipe is enabled.
    Volatile<bool> m_enabled;

    // The list of event pipe providers.
    SList<SListElem<EventPipeProvider*>> *m_pProviderList;

    // The provider used to write configuration events to the event stream.
    EventPipeProvider *m_pConfigProvider;

    // The event used to write event information to the event stream.
    EventPipeEvent *m_pMetadataEvent;

    // The provider name for the configuration event pipe provider.
    // This provider is used to emit configuration events.
    const static WCHAR* s_configurationProviderName;

    // True if rundown is enabled.
    Volatile<bool> m_rundownEnabled;

    // The rundown thread.  If rundown is not enabled, this is NULL.
    Thread *m_pRundownThread;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_CONFIGURATION_H__
