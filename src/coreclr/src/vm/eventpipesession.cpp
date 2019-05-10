// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeprovider.h"
#include "eventpipesession.h"
#include "eventpipesessionprovider.h"

#ifdef FEATURE_PERFTRACING

EventPipeSession::EventPipeSession(
    EventPipeSessionType sessionType,
    unsigned int circularBufferSizeInMB,
    const EventPipeProviderConfiguration *pProviders,
    uint32_t numProviders)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(circularBufferSizeInMB > 0);
        PRECONDITION(numProviders > 0 && pProviders != nullptr);
    }
    CONTRACTL_END;

    m_sessionType = sessionType;
    m_circularBufferSizeInBytes = (size_t)circularBufferSizeInMB << 20; // 1MB;
    m_rundownEnabled = false;
    m_pProviderList = new EventPipeSessionProviderList(pProviders, numProviders);
    GetSystemTimeAsFileTime(&m_sessionStartTime);
    QueryPerformanceCounter(&m_sessionStartTimeStamp);
}

EventPipeSession::~EventPipeSession()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pProviderList != NULL)
    {
        delete m_pProviderList;
        m_pProviderList = NULL;
    }
}

bool EventPipeSession::IsValid() const
{
    LIMITED_METHOD_CONTRACT;

    if((m_pProviderList == NULL) || (m_pProviderList->IsEmpty()))
    {
        return false;
    }

    return true;
}

void EventPipeSession::AddSessionProvider(EventPipeSessionProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pProviderList->AddSessionProvider(pProvider);
}

EventPipeSessionProvider* EventPipeSession::GetSessionProvider(EventPipeProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return m_pProviderList->GetSessionProvider(pProvider);
}

#endif // FEATURE_PERFTRACING
