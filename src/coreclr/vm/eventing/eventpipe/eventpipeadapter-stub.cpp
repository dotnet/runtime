// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeadapter.h"

EventPipeProviderConfigurationAdapter::EventPipeProviderConfigurationAdapter(
    const COR_PRF_EVENTPIPE_PROVIDER_CONFIG *,
    uint32_t)
    : m_providerConfigs(nullptr),
      m_providerConfigsLen(0)
{
    STATIC_CONTRACT_NOTHROW;
}

EventPipeProviderConfigurationAdapter::~EventPipeProviderConfigurationAdapter()
{
    STATIC_CONTRACT_NOTHROW;
}

void EventPipeAdapter::Initialize()
{
    STATIC_CONTRACT_NOTHROW;
}

void EventPipeAdapter::FinishInitialize()
{
    STATIC_CONTRACT_NOTHROW;
}

void EventPipeAdapter::Shutdown()
{
    STATIC_CONTRACT_NOTHROW;
}

bool EventPipeAdapter::Enabled()
{
    STATIC_CONTRACT_NOTHROW;
    return false;
}

EventPipeSessionID EventPipeAdapter::Enable(
    LPCWSTR,
    uint32_t,
    const EventPipeProviderConfigurationAdapter &,
    EventPipeSessionType,
    EventPipeSerializationFormat,
    const bool,
    IpcStream *const,
    EventPipeSessionSynchronousCallback,
    void *)
{
    STATIC_CONTRACT_NOTHROW;
    return 0;
}

void EventPipeAdapter::Disable(EventPipeSessionID)
{
    STATIC_CONTRACT_NOTHROW;
}

void EventPipeAdapter::StartStreaming(EventPipeSessionID)
{
    STATIC_CONTRACT_NOTHROW;
}

EventPipeSession * EventPipeAdapter::GetSession(EventPipeSessionID)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

bool EventPipeAdapter::SignalSession(EventPipeSessionID)
{
    STATIC_CONTRACT_NOTHROW;
    return false;
}

bool EventPipeAdapter::WaitForSessionSignal(EventPipeSessionID, INT32)
{
    STATIC_CONTRACT_NOTHROW;
    return false;
}

#if defined(TARGET_WASI) && defined(PERFTRACING_DISABLE_THREADS)
bool EventPipeAdapter::RunWasiJobs()
{
    STATIC_CONTRACT_NOTHROW;
    return false;
}
#endif // TARGET_WASI && PERFTRACING_DISABLE_THREADS

FILETIME EventPipeAdapter::GetSessionStartTime(EventPipeSession *)
{
    STATIC_CONTRACT_NOTHROW;
    return {};
}

LONGLONG EventPipeAdapter::GetSessionStartTimestamp(EventPipeSession *)
{
    STATIC_CONTRACT_NOTHROW;
    return 0;
}

void EventPipeAdapter::AddProviderToSession(EventPipeSessionProvider *, EventPipeSession *)
{
    STATIC_CONTRACT_NOTHROW;
}

EventPipeProvider * EventPipeAdapter::CreateProvider(const WCHAR *, EventPipeCallback, void *)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

void EventPipeAdapter::DeleteProvider(EventPipeProvider *)
{
    STATIC_CONTRACT_NOTHROW;
}

EventPipeProvider * EventPipeAdapter::GetProvider(LPCWSTR)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

EventPipeSessionProvider * EventPipeAdapter::CreateSessionProvider(const EventPipeProviderConfigurationAdapter &)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

HRESULT EventPipeAdapter::GetProviderName(const EventPipeProvider *, ULONG, ULONG *numNameCharsOut, LPWSTR)
{
    STATIC_CONTRACT_NOTHROW;
    if (numNameCharsOut != nullptr)
    {
        *numNameCharsOut = 0;
    }
    return E_NOTIMPL;
}

EventPipeEvent * EventPipeAdapter::AddEvent(
    EventPipeProvider *,
    uint32_t,
    LPCWSTR,
    int64_t,
    uint32_t,
    EventPipeEventLevel,
    uint8_t,
    const EventPipeParameterDescAdapter &,
    bool)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

EventPipeEvent * EventPipeAdapter::AddEvent(
    EventPipeProvider *,
    uint32_t,
    int64_t,
    uint32_t,
    EventPipeEventLevel,
    bool,
    BYTE *,
    uint32_t)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

void EventPipeAdapter::WriteEvent(EventPipeEvent *, BYTE *, uint32_t, LPCGUID, LPCGUID)
{
    STATIC_CONTRACT_NOTHROW;
}

void EventPipeAdapter::WriteEvent(EventPipeEvent *, EventData *, uint32_t, LPCGUID, LPCGUID)
{
    STATIC_CONTRACT_NOTHROW;
}

void EventPipeAdapter::WriteEvent(EventPipeEvent *, EventDataAdapter &, LPCGUID, LPCGUID)
{
    STATIC_CONTRACT_NOTHROW;
}

bool EventPipeAdapter::EventIsEnabled(const EventPipeEvent *)
{
    STATIC_CONTRACT_NOTHROW;
    return false;
}

EventPipeEventInstance * EventPipeAdapter::GetNextEvent(EventPipeSessionID)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

EventPipeProvider * EventPipeAdapter::GetEventProvider(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

uint32_t EventPipeAdapter::GetEventID(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return 0;
}

uint64_t EventPipeAdapter::GetEventThreadID(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return 0;
}

int64_t EventPipeAdapter::GetEventTimestamp(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return 0;
}

LPCGUID EventPipeAdapter::GetEventActivityID(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

LPCGUID EventPipeAdapter::GetEventRelativeActivityID(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

const BYTE * EventPipeAdapter::GetEventData(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return nullptr;
}

uint32_t EventPipeAdapter::GetEventDataLen(EventPipeEventInstance *)
{
    STATIC_CONTRACT_NOTHROW;
    return 0;
}

void EventPipeAdapter::ResumeSession(EventPipeSession *)
{
    STATIC_CONTRACT_NOTHROW;
}

void EventPipeAdapter::PauseSession(EventPipeSession *)
{
    STATIC_CONTRACT_NOTHROW;
}
