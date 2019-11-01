// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeevent.h"
#include "eventpipeprovider.h"

#ifdef FEATURE_PERFTRACING

EventPipeEvent::EventPipeEvent(
    EventPipeProvider &provider,
    INT64 keywords,
    unsigned int eventID,
    unsigned int eventVersion,
    EventPipeEventLevel level,
    bool needStack,
    BYTE *pMetadata,
    unsigned int metadataLength) : m_pProvider(&provider),
                                   m_keywords(keywords),
                                   m_eventID(eventID),
                                   m_eventVersion(eventVersion),
                                   m_level(level),
                                   m_needStack(needStack),
                                   m_enabledMask(0),
                                   m_pMetadata(nullptr)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(&provider != nullptr);
    }
    CONTRACTL_END;

    if (pMetadata != nullptr)
    {
        m_pMetadata = new BYTE[metadataLength];
        memcpy(m_pMetadata, pMetadata, metadataLength);
        m_metadataLength = metadataLength;
    }
    else
    {
        // if metadata is not provided, we have to build the minimum version. It's required by the serialization contract
        m_pMetadata = BuildMinimumMetadata();
        m_metadataLength = MinimumMetadataLength;
    }
}

EventPipeEvent::~EventPipeEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    delete[] m_pMetadata;
}

BYTE *EventPipeEvent::BuildMinimumMetadata()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BYTE *minmumMetadata = new BYTE[MinimumMetadataLength];
    BYTE *currentPtr = minmumMetadata;

    // the order of fields is defined in coreclr\src\mscorlib\shared\System\Diagnostics\Tracing\EventSource.cs DefineEventPipeEvents method
    memcpy(currentPtr, &m_eventID, sizeof(m_eventID));
    currentPtr += sizeof(m_eventID);

    SString eventName = SString::Empty();
    unsigned int eventNameSize = (eventName.GetCount() + 1) * sizeof(WCHAR);
    memcpy(currentPtr, (BYTE *)eventName.GetUnicode(), eventNameSize);
    currentPtr += eventNameSize;

    memcpy(currentPtr, &m_keywords, sizeof(m_keywords));
    currentPtr += sizeof(m_keywords);

    memcpy(currentPtr, &m_eventVersion, sizeof(m_eventVersion));
    currentPtr += sizeof(m_eventVersion);

    memcpy(currentPtr, &m_level, sizeof(m_level));
    currentPtr += sizeof(m_level);

    unsigned int noParameters = 0;
    memcpy(currentPtr, &noParameters, sizeof(noParameters));
    currentPtr += sizeof(noParameters);

    return minmumMetadata;
}

EventPipeProvider *EventPipeEvent::GetProvider() const
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
    return m_enabledMask != 0;
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
    _ASSERTE(EventPipe::IsLockOwnedByCurrentThread());
    m_enabledMask = m_pProvider->ComputeEventEnabledMask(m_keywords, m_level);
}

bool EventPipeEvent::IsEnabled(uint64_t sessionMask) const
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pProvider != nullptr);
    return (m_pProvider->IsEnabled(sessionMask) && (m_enabledMask & sessionMask) != 0);
}

#endif // FEATURE_PERFTRACING
