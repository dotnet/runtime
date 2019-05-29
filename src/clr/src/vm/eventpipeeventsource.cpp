// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeeventsource.h"
#include "eventpipe.h"
#include "eventpipeevent.h"
#include "eventpipemetadatagenerator.h"
#include "eventpipeprovider.h"
#include "eventpipesession.h"
#include "eventpipesessionprovider.h"

#ifdef FEATURE_PERFTRACING

const WCHAR* EventPipeEventSource::s_pProviderName = W("Microsoft-DotNETCore-EventPipe");
const WCHAR* EventPipeEventSource::s_pProcessInfoEventName = W("ProcessInfo");

EventPipeEventSource::EventPipeEventSource()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pProvider = EventPipe::CreateProvider(SL(s_pProviderName), NULL, NULL);

    // Generate metadata.
    const unsigned int numParams = 1;
    EventPipeParameterDesc params[numParams];
    params[0].Type = EventPipeParameterType::String;
    params[0].Name = W("CommandLine");

    size_t metadataLength = 0;
    BYTE *pMetadata = EventPipeMetadataGenerator::GenerateEventMetadata(
        1,      /* eventID */
        s_pProcessInfoEventName,
        0,      /* keywords */
        0,      /* version */
        EventPipeEventLevel::LogAlways,
        params,
        numParams,
        metadataLength);

    // Add the event.
    m_pProcessInfoEvent = m_pProvider->AddEvent(
        1,      /* eventID */
        0,      /* keywords */
        0,      /* eventVersion */
        EventPipeEventLevel::LogAlways,
        false,  /* needStack */
        pMetadata,
        (unsigned int)metadataLength);

    // Delete the metadata after the event is created.
    // The metadata blob will be copied into EventPipe-owned memory.
    delete [] pMetadata;
}

EventPipeEventSource::~EventPipeEventSource()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Delete the provider and associated events.
    // This is called in the shutdown path which can't throw.
    // Catch exceptions and ignore failures.
    EX_TRY
    {
        EventPipe::DeleteProvider(m_pProvider);
    }
    EX_CATCH { }
    EX_END_CATCH(SwallowAllExceptions);
}

void EventPipeEventSource::Enable(EventPipeSession *pSession)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pSession != NULL);
    }
    CONTRACTL_END;

    if (pSession == nullptr)
        return;

    pSession->AddSessionProvider(new EventPipeSessionProvider(
        s_pProviderName,
        static_cast<UINT64>(-1),
        EventPipeEventLevel::LogAlways,
        NULL));
}

void EventPipeEventSource::SendProcessInfo(LPCWSTR pCommandLine)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    EventData data[1];
    data[0].Ptr = (UINT64) pCommandLine;
    data[0].Size = (unsigned int)(wcslen(pCommandLine) + 1) * 2;
    data[0].Reserved = 0;

    EventPipe::WriteEvent(*m_pProcessInfoEvent, data, 1);
}

#endif // FEATURE_PERFTRACING
