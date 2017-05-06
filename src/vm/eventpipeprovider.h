// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_PROVIDER_H__
#define __EVENTPIPE_PROVIDER_H__

#ifdef FEATURE_PERFTRACING

#include "slist.h"

class EventPipeEvent;

// Define the event pipe callback to match the ETW callback signature.
typedef void (*EventPipeCallback)(
    LPCGUID SourceID,
    ULONG IsEnabled,
    UCHAR Level,
    ULONGLONG MatchAnyKeywords,
    ULONGLONG MatchAllKeywords,
    void *FilterData,
    void *CallbackContext);

enum class EventPipeEventLevel
{
    LogAlways,
    Critical,
    Error,
    Warning,
    Informational,
    Verbose
};

class EventPipeProvider
{
    // Declare friends.
    friend class EventPipeConfiguration;

private:
    // The GUID of the provider.
    GUID m_providerID;

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

public:

    EventPipeProvider(const GUID &providerID);
    ~EventPipeProvider();

    // Get the provider ID.
    const GUID& GetProviderID() const;

    // Determine if the provider is enabled.
    bool Enabled() const;

    // Determine if the specified keywords are enabled.
    bool EventEnabled(INT64 keywords) const;

    // Determine if the specified keywords and level match the configuration.
    bool EventEnabled(INT64 keywords, EventPipeEventLevel eventLevel) const;

    // Create a new event.
    EventPipeEvent* AddEvent(INT64 keywords, unsigned int eventID, unsigned int eventVersion, EventPipeEventLevel level, bool needStack);

    // Register a callback with the provider to be called on state change.
    void RegisterCallback(EventPipeCallback pCallbackFunction, void *pData);

    // Unregister a callback.
    void UnregisterCallback(EventPipeCallback pCallbackFunction);

private:

    // Add an event to the provider.
    void AddEvent(EventPipeEvent &event);

    // Set the provider configuration (enable and disable sets of events).
    // This is called by EventPipeConfiguration.
    void SetConfiguration(bool providerEnabled, INT64 keywords, EventPipeEventLevel providerLevel);

    // Refresh the runtime state of all events.
    void RefreshAllEvents();

    // Invoke the provider callback.
    void InvokeCallback();
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROVIDER_H__
