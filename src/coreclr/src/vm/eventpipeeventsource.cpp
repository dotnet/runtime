// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeeventpayload.h"
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

#if defined(HOST_WINDOWS)
const WCHAR* EventPipeEventSource::s_pOSInformation = W("Windows");
#elif defined(__APPLE__)
const WCHAR* EventPipeEventSource::s_pOSInformation = W("macOS");
#elif defined(__linux__)
const WCHAR* EventPipeEventSource::s_pOSInformation = W("Linux");
#else
const WCHAR* EventPipeEventSource::s_pOSInformation = W("Unknown");
#endif

#if defined(TARGET_X86)
const WCHAR* EventPipeEventSource::s_pArchInformation = W("x86");
#elif defined(TARGET_AMD64)
const WCHAR* EventPipeEventSource::s_pArchInformation = W("x64");
#elif defined(TARGET_ARM)
const WCHAR* EventPipeEventSource::s_pArchInformation = W("arm32");
#elif defined(TARGET_ARM64)
const WCHAR* EventPipeEventSource::s_pArchInformation = W("arm64");
#else
const WCHAR* EventPipeEventSource::s_pArchInformation = W("Unknown");
#endif

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
    const unsigned int numParams = 3;
    EventPipeParameterDesc params[numParams];
    params[0].Type = EventPipeParameterType::String;
    params[0].Name = W("CommandLine");
    params[1].Type = EventPipeParameterType::String;
    params[1].Name = W("OSInformation");
    params[2].Type = EventPipeParameterType::String;
    params[2].Name = W("ArchInformation");

    size_t metadataLength = 0;
    BYTE *pMetadata = EventPipeMetadataGenerator::GenerateEventMetadata(
        1,      /* eventID */
        s_pProcessInfoEventName,
        0,      /* keywords */
        1,      /* version */
        EventPipeEventLevel::LogAlways,
        0,      /* opcode */
        params,
        numParams,
        &metadataLength);

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

    EventData data[3];
    data[0].Ptr = (UINT64) pCommandLine;
    data[0].Size = (unsigned int)(wcslen(pCommandLine) + 1) * 2;
    data[0].Reserved = 0;
    data[1].Ptr = (UINT64)s_pOSInformation;
    data[1].Size = (unsigned int)(wcslen(s_pOSInformation) + 1) * 2;
    data[1].Reserved = 0;
    data[2].Ptr = (UINT64)s_pArchInformation;
    data[2].Size = (unsigned int)(wcslen(s_pArchInformation) + 1) * 2;
    data[2].Reserved = 0;

    EventPipe::WriteEvent(*m_pProcessInfoEvent, data, 3);
}

#endif // FEATURE_PERFTRACING
