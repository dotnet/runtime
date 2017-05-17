// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_CONFIGURATION_H__
#define __EVENTPIPE_CONFIGURATION_H__

#ifdef FEATURE_PERFTRACING

#include "slist.h"

class EventPipeEnabledProvider;
class EventPipeEnabledProviderList;
class EventPipeEvent;
class EventPipeEventInstance;
class EventPipeProvider;
struct EventPipeProviderConfiguration;

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

    // Register a provider.
    bool RegisterProvider(EventPipeProvider &provider);

    // Unregister a provider.
    bool UnregisterProvider(EventPipeProvider &provider);

    // Get the provider with the specified provider ID if it exists.
    EventPipeProvider* GetProvider(const GUID &providerID);

    // Get the configured size of the circular buffer.
    size_t GetCircularBufferSize() const;

    // Set the configured size of the circular buffer.
    void SetCircularBufferSize(size_t circularBufferSize);

    // Enable the event pipe.
    void Enable(
        uint circularBufferSizeInMB,
        EventPipeProviderConfiguration *pProviders,
        int numProviders);

    // Disable the event pipe.
    void Disable();

    // Get the status of the event pipe.
    bool Enabled() const;

    // Determine if rundown is enabled.
    bool RundownEnabled() const;

    // Enable the well-defined symbolic rundown configuration.
    void EnableRundown();

    // Get the event used to write metadata to the event stream.
    EventPipeEventInstance* BuildEventMetadataEvent(EventPipeEventInstance &sourceInstance);

    // Delete deferred providers.
    void DeleteDeferredProviders();

private:

    // Get the provider without taking the lock.
    EventPipeProvider* GetProviderNoLock(const GUID &providerID);

    // Determines whether or not the event pipe is enabled.
    Volatile<bool> m_enabled;

    // The configured size of the circular buffer.
    size_t m_circularBufferSizeInBytes;

    // EventPipeConfiguration only supports a single session.
    // This is the set of configurations for each enabled provider.
    EventPipeEnabledProviderList *m_pEnabledProviderList;

    // The list of event pipe providers.
    SList<SListElem<EventPipeProvider*>> *m_pProviderList;

    // The provider used to write configuration events to the event stream.
    EventPipeProvider *m_pConfigProvider;

    // The event used to write event information to the event stream.
    EventPipeEvent *m_pMetadataEvent;

    // The provider ID for the configuration event pipe provider.
    // This provider is used to emit configuration events.
    static const GUID s_configurationProviderID;

    // True if rundown is enabled.
    Volatile<bool> m_rundownEnabled;
};

class EventPipeEnabledProviderList
{

private:

    // The number of providers in the list.
    unsigned int m_numProviders;

    // The list of providers.
    EventPipeEnabledProvider *m_pProviders;

    // A catch-all provider used when tracing is enabled at start-up
    // under (COMPlus_PerformanceTracing & 1) == 1.
    EventPipeEnabledProvider *m_pCatchAllProvider;

public:

    // Create a new list based on the input.
    EventPipeEnabledProviderList(EventPipeProviderConfiguration *pConfigs, unsigned int numConfigs);
    ~EventPipeEnabledProviderList();

    // Get the enabled provider for the specified provider.
    // Return NULL if one doesn't exist.
    EventPipeEnabledProvider* GetEnabledProvider(EventPipeProvider *pProvider);
};

class EventPipeEnabledProvider
{
private:

    // The provider name.
    WCHAR *m_pProviderName;

    // The enabled keywords.
    UINT64 m_keywords;

    // The loging level.
    EventPipeEventLevel m_loggingLevel;

public:

    EventPipeEnabledProvider();
    ~EventPipeEnabledProvider();

    void Set(LPCWSTR providerName, UINT64 keywords, EventPipeEventLevel loggingLevel);

    LPCWSTR GetProviderName() const;

    UINT64 GetKeywords() const;

    EventPipeEventLevel GetLevel() const;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_CONFIGURATION_H__
