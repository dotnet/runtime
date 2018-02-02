// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_EVENT_H__
#define __EVENTPIPE_EVENT_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipeprovider.h"

class EventPipeEvent
{
    // Declare friends.
    friend class EventPipeProvider;

private:

    // The provider that contains the event.
    EventPipeProvider *m_pProvider;

    // Bit vector containing the keywords that enable the event.
    INT64 m_keywords;

    // The ID (within the provider) of the event.
    unsigned int m_eventID;

    // The version of the event.
    unsigned int m_eventVersion;

    // The verbosity of the event.
    EventPipeEventLevel m_level;

    // True if a call stack should be captured when writing the event.
    bool m_needStack;

    // True if the event is current enabled.
    Volatile<bool> m_enabled;

    // Metadata
    BYTE *m_pMetadata;

    // Metadata length;
    unsigned int m_metadataLength;

    // Refreshes the runtime state for this event.
    // Called by EventPipeProvider when the provider configuration changes.
    void RefreshState();

    // Only EventPipeProvider can create events.
    // The provider is responsible for allocating and freeing events.
    EventPipeEvent(EventPipeProvider &provider, INT64 keywords, unsigned int eventID, unsigned int eventVersion, EventPipeEventLevel level, bool needStack, BYTE *pMetadata = NULL, unsigned int metadataLength = 0);

  public:
    ~EventPipeEvent();

    // Get the provider associated with this event.
    EventPipeProvider* GetProvider() const;

    // Get the keywords that enable the event.
    INT64 GetKeywords() const;

    // Get the ID (within the provider) of the event.
    unsigned int GetEventID() const;

    // Get the version of the event.
    unsigned int GetEventVersion() const;

    // Get the verbosity of the event.
    EventPipeEventLevel GetLevel() const;

    // True if a call stack should be captured when writing the event.
    bool NeedStack() const;

    // True if the event is currently enabled.
    bool IsEnabled() const;

    BYTE *GetMetadata() const;

    unsigned int GetMetadataLength() const;

  private:
    // used when Metadata is not provided
    BYTE *BuildMinimumMetadata();

    unsigned int GetMinimumMetadataLength();
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_EVENT_H__
