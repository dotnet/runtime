// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeevent.h"
#include "eventpipeprovider.h"

#ifdef FEATURE_PERFTRACING

EventPipeEvent::EventPipeEvent(EventPipeProvider &provider, INT64 keywords, unsigned int eventID, unsigned int eventVersion, EventPipeEventLevel level, bool needStack, BYTE *pMetadata, unsigned int metadataLength)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pProvider = &provider;
    m_keywords = keywords;
    m_eventID = eventID;
    m_eventVersion = eventVersion;
    m_level = level;
    m_needStack = needStack;
    m_enabled = false;
    if (pMetadata != NULL)
    {
        m_pMetadata = new BYTE[metadataLength];
        memcpy(m_pMetadata, pMetadata, metadataLength);
        m_metadataLength = metadataLength;
    }
    else
    {
        m_pMetadata = NULL;
        m_metadataLength = 0;
    }
}

EventPipeEvent::~EventPipeEvent()
{
    if (m_pMetadata != NULL)
    {
        delete[] m_pMetadata;
        m_pMetadata = NULL;
    }
}

EventPipeProvider* EventPipeEvent::GetProvider() const
{
    LIMITED_METHOD_CONTRACT;

    return m_pProvider;
}

INT64 EventPipeEvent::GetKeywords() const
{
    LIMITED_METHOD_CONTRACT;

    return m_keywords;
}

unsigned int EventPipeEvent::GetEventID() const
{
    LIMITED_METHOD_CONTRACT;

    return m_eventID;
}

unsigned int EventPipeEvent::GetEventVersion() const
{
    LIMITED_METHOD_CONTRACT;

    return m_eventVersion;
}

EventPipeEventLevel EventPipeEvent::GetLevel() const
{
    LIMITED_METHOD_CONTRACT;

    return m_level;
}

bool EventPipeEvent::NeedStack() const
{
    LIMITED_METHOD_CONTRACT;

    return m_needStack;
}

bool EventPipeEvent::IsEnabled() const
{
    LIMITED_METHOD_CONTRACT;

    return m_enabled;
}

BYTE *EventPipeEvent::GetMetadata() const
{
    LIMITED_METHOD_CONTRACT;

    return m_pMetadata;
}

unsigned int EventPipeEvent::GetMetadataLength() const
{
    LIMITED_METHOD_CONTRACT;

    return m_metadataLength;
}

void EventPipeEvent::RefreshState()
{
    LIMITED_METHOD_CONTRACT;

    m_enabled = m_pProvider->EventEnabled(m_keywords, m_level);
}

#endif // FEATURE_PERFTRACING
