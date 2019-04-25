// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_PROVIDER_H__
#define __EVENTPIPE_PROVIDER_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "slist.h"

class EventPipeEvent;

class EventPipeProvider
{
    // Declare friends.
    friend class EventPipe;
    friend class EventPipeConfiguration;

private:
    // The name of the provider.
    SString m_providerName;

    // True if the provider is enabled.
    bool m_enabled;

    // Bit vector containing the currently enabled keywords.
    INT64 m_keywords;

    // The current verbosity of the provider.
    EventPipeEventLevel m_providerLevel;

    // List of every event currently associated with the provider.
    // New events can be added on-the-fly.
    SList<SListElem<EventPipeEvent*>> *m_pEventList;

    // The optional provider callback.
    EventPipeCallback m_pCallbackFunction;

    // The optional provider callback data pointer.
    void *m_pCallbackData;

    // The configuration object.
    EventPipeConfiguration *m_pConfig;

    // True if the provider has been deleted, but that deletion
    // has been deferred until tracing is stopped.
    bool m_deleteDeferred;

    // Private constructor because all providers are created through EventPipe::CreateProvider.
    EventPipeProvider(EventPipeConfiguration *pConfig, const SString &providerName, EventPipeCallback pCallbackFunction = NULL, void *pCallbackData = NULL);

public:

    ~EventPipeProvider();

    // Get the provider Name.
    const SString& GetProviderName() const;

    // Determine if the provider is enabled.
    bool Enabled() const;

    // Determine if the specified keywords are enabled.
    bool EventEnabled(INT64 keywords) const;

    // Determine if the specified keywords and level match the configuration.
    bool EventEnabled(INT64 keywords, EventPipeEventLevel eventLevel) const;

    // Create a new event.
    EventPipeEvent* AddEvent(unsigned int eventID, INT64 keywords, unsigned int eventVersion, EventPipeEventLevel level, bool needStack, BYTE *pMetadata = NULL, unsigned int metadataLength = 0);

private:

    // Add an event to the provider.
    void AddEvent(EventPipeEvent &event);

    // Set the provider configuration (enable and disable sets of events).
    // This is called by EventPipeConfiguration.
    EventPipeProviderCallbackData SetConfiguration(bool providerEnabled, INT64 keywords, EventPipeEventLevel providerLevel, LPCWSTR pFilterData);

    // Refresh the runtime state of all events.
    void RefreshAllEvents();

    // Prepare the data required for invoking callback
    EventPipeProviderCallbackData PrepareCallbackData(LPCWSTR pFilterData);

    // Invoke the provider callback.
    static void InvokeCallback(EventPipeProviderCallbackData eventPipeProviderCallbackData);

    // Specifies whether or not the provider was deleted, but that deletion
    // was deferred until after tracing is stopped.
    bool GetDeleteDeferred() const;

    // Defer deletion of the provider.
    void SetDeleteDeferred();
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROVIDER_H__
