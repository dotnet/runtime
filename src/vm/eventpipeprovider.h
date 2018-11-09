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
    friend class SampleProfiler;

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
    EventPipeEvent* AddEvent(unsigned int eventID, INT64 keywords, unsigned int eventVersion, EventPipeEventLevel level, BYTE *pMetadata = NULL, unsigned int metadataLength = 0);

  private:

    // Create a new event, but allow needStack to be specified.
    // In general, we want stack walking to be controlled by the consumer and not the producer of events.
    // However, there are a couple of cases that we know we don't want to do a stackwalk that would affect performance significantly:
    // 1. Sample profiler events: The sample profiler already does a stack walk of the target thread.  Doing one of the sampler thread is a waste.
    // 2. Metadata events: These aren't as painful but because we have to keep this functionality around, might as well use it.
    EventPipeEvent* AddEvent(unsigned int eventID, INT64 keywords, unsigned int eventVersion, EventPipeEventLevel level, bool needStack, BYTE *pMetadata = NULL, unsigned int metadataLength = 0);

    // Add an event to the provider.
    void AddEvent(EventPipeEvent &event);

    // Set the provider configuration (enable and disable sets of events).
    // This is called by EventPipeConfiguration.
    void SetConfiguration(bool providerEnabled, INT64 keywords, EventPipeEventLevel providerLevel, LPCWSTR pFilterData);

    // Refresh the runtime state of all events.
    void RefreshAllEvents();

    // Invoke the provider callback.
    void InvokeCallback(LPCWSTR pFilterData);

    // Specifies whether or not the provider was deleted, but that deletion
    // was deferred until after tracing is stopped.
    bool GetDeleteDeferred() const;

    // Defer deletion of the provider.
    void SetDeleteDeferred();
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROVIDER_H__
