// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_CONFIGURATION_H__
#define __EVENTPIPE_CONFIGURATION_H__

#ifdef FEATURE_PERFTRACING

#include "slist.h"

class EventPipeEvent;
class EventPipeEventInstance;
class EventPipeProvider;

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

    // Enable the event pipe.
    void Enable();

    // Disable the event pipe.
    void Disable();

    // Get the event used to write metadata to the event stream.
    EventPipeEventInstance* BuildEventMetadataEvent(EventPipeEvent &sourceEvent, BYTE *pPayloadData = NULL, unsigned int payloadLength = 0);

private:

    // Get the provider without taking the lock.
    EventPipeProvider* GetProviderNoLock(const GUID &providerID);

    // The list of event pipe providers.
    SList<SListElem<EventPipeProvider*>> *m_pProviderList;

    // The provider used to write configuration events to the event stream.
    EventPipeProvider *m_pConfigProvider;

    // The event used to write event information to the event stream.
    EventPipeEvent *m_pMetadataEvent;

    // The provider ID for the configuration event pipe provider.
    // This provider is used to emit configuration events.
    static const GUID s_configurationProviderID;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_CONFIGURATION_H__
