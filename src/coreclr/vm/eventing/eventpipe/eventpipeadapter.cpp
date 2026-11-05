// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeadapter.h"

EventPipeProviderConfigurationAdapter::EventPipeProviderConfigurationAdapter(
    const COR_PRF_EVENTPIPE_PROVIDER_CONFIG *providerConfigs,
    uint32_t providerConfigsLen)
    : m_providerConfigs(new (nothrow) EventPipeProviderConfiguration[providerConfigsLen]),
      m_providerConfigsLen(providerConfigsLen)
{
    STATIC_CONTRACT_NOTHROW;

    if (m_providerConfigs != nullptr)
    {
        for (uint32_t i = 0; i < providerConfigsLen; ++i)
        {
            ep_char8_t *providerName = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerConfigs[i].providerName));
            ep_char8_t *filterData = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerConfigs[i].filterData));
            ep_provider_config_init(
                &m_providerConfigs[i],
                providerName,
                providerConfigs[i].keywords,
                static_cast<EventPipeEventLevel>(providerConfigs[i].loggingLevel),
                filterData);
            ep_rt_utf8_string_free(providerName);
            ep_rt_utf8_string_free(filterData);
        }
    }
}

EventPipeProviderConfigurationAdapter::~EventPipeProviderConfigurationAdapter()
{
    STATIC_CONTRACT_NOTHROW;

    if (m_providerConfigs != nullptr)
    {
        for (uint32_t i = 0; i < m_providerConfigsLen; ++i)
        {
            ep_provider_config_fini(&m_providerConfigs[i]);
        }
        delete[] m_providerConfigs;
    }
}

void EventPipeAdapter::Initialize()
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    ep_init();
}

void EventPipeAdapter::FinishInitialize()
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    ep_finish_init();
}

void EventPipeAdapter::Shutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ep_shutdown();
}

bool EventPipeAdapter::Enabled()
{
    STATIC_CONTRACT_NOTHROW;
    return ep_enabled();
}

EventPipeSessionID EventPipeAdapter::Enable(
    LPCWSTR outputPath,
    uint32_t circularBufferSizeInMB,
    const EventPipeProviderConfigurationAdapter &providerConfigs,
    EventPipeSessionType sessionType,
    EventPipeSerializationFormat format,
    const bool rundownRequested,
    IpcStream *const stream,
    EventPipeSessionSynchronousCallback callback,
    void *callbackAdditionalData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ep_char8_t *outputPathUTF8 = nullptr;
    if (outputPath != nullptr)
    {
        outputPathUTF8 = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(outputPath));
    }
    EventPipeSessionID result = ep_init_session(
        outputPathUTF8,
        circularBufferSizeInMB,
        providerConfigs.GetProviderConfigs(),
        providerConfigs.GetProviderConfigsLen(),
        sessionType,
        format,
        rundownRequested,
        stream,
        callback,
        callbackAdditionalData);
    ep_rt_utf8_string_free(outputPathUTF8);
    return result;
}

void EventPipeAdapter::Disable(EventPipeSessionID id)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ep_disable(id);
}

void EventPipeAdapter::StartStreaming(EventPipeSessionID id)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ep_start_session(id);
}

EventPipeSession * EventPipeAdapter::GetSession(EventPipeSessionID id)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_get_session(id);
}

bool EventPipeAdapter::SignalSession(EventPipeSessionID id)
{
    STATIC_CONTRACT_NOTHROW;

    EventPipeSession *const session = ep_get_session(id);
    return session != nullptr && ep_rt_wait_event_set(ep_session_get_wait_event(session));
}

bool EventPipeAdapter::WaitForSessionSignal(EventPipeSessionID id, INT32 timeoutMs)
{
    STATIC_CONTRACT_NOTHROW;

    EventPipeSession *const session = ep_get_session(id);
    return session != nullptr && !ep_rt_wait_event_wait(ep_session_get_wait_event(session), static_cast<uint32_t>(timeoutMs), false);
}

#if defined(TARGET_WASI) && defined(PERFTRACING_DISABLE_THREADS)
bool EventPipeAdapter::RunWasiJobs()
{
    STATIC_CONTRACT_NOTHROW;
    return ep_rt_coreclr_wasi_run_jobs();
}
#endif // TARGET_WASI && PERFTRACING_DISABLE_THREADS

FILETIME EventPipeAdapter::GetSessionStartTime(EventPipeSession *session)
{
    STATIC_CONTRACT_NOTHROW;

    _ASSERTE(session != nullptr);
    LARGE_INTEGER largeValue;
    largeValue.QuadPart = ep_session_get_session_start_time(session);

    FILETIME fileTime;
    fileTime.dwLowDateTime = largeValue.u.LowPart;
    fileTime.dwHighDateTime = largeValue.u.HighPart;
    return fileTime;
}

LONGLONG EventPipeAdapter::GetSessionStartTimestamp(EventPipeSession *session)
{
    STATIC_CONTRACT_NOTHROW;

    _ASSERTE(session != nullptr);
    return ep_session_get_session_start_timestamp(session);
}

void EventPipeAdapter::AddProviderToSession(EventPipeSessionProvider *provider, EventPipeSession *session)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ep_add_provider_to_session(provider, session);
}

EventPipeProvider * EventPipeAdapter::CreateProvider(
    const WCHAR *providerName,
    EventPipeCallback callback,
    void *callbackContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ep_char8_t *providerNameUTF8 = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerName));
    EventPipeProvider *provider = ep_create_provider(providerNameUTF8, callback, callbackContext);
    ep_rt_utf8_string_free(providerNameUTF8);
    return provider;
}

void EventPipeAdapter::DeleteProvider(EventPipeProvider *provider)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ep_delete_provider(provider);
}

EventPipeProvider * EventPipeAdapter::GetProvider(LPCWSTR providerName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (providerName == nullptr)
    {
        return nullptr;
    }

    ep_char8_t *providerNameUTF8 = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerName));
    EventPipeProvider *provider = ep_get_provider(providerNameUTF8);
    ep_rt_utf8_string_free(providerNameUTF8);
    return provider;
}

EventPipeSessionProvider * EventPipeAdapter::CreateSessionProvider(
    const EventPipeProviderConfigurationAdapter &providerConfig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(providerConfig.GetProviderConfigs() != nullptr && providerConfig.GetProviderConfigsLen() == 1);
    const EventPipeProviderConfiguration *config = providerConfig.GetProviderConfigs();
    if (config == nullptr)
    {
        return nullptr;
    }

    return ep_session_provider_alloc(
        ep_provider_config_get_provider_name(&config[0]),
        ep_provider_config_get_keywords(&config[0]),
        static_cast<EventPipeEventLevel>(ep_provider_config_get_logging_level(&config[0])),
        ep_provider_config_get_filter_data(&config[0]),
        nullptr,
        nullptr);
}

HRESULT EventPipeAdapter::GetProviderName(
    const EventPipeProvider *provider,
    ULONG numNameChars,
    ULONG *numNameCharsOut,
    LPWSTR name)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(provider != nullptr);

    HRESULT hr = S_OK;
    const ep_char16_t *providerName = ep_provider_get_provider_name_utf16(provider);
    if (providerName != nullptr)
    {
        uint32_t numProviderNameChars = static_cast<uint32_t>(ep_rt_utf16_string_len(providerName) + 1);
        if (numNameCharsOut != nullptr)
        {
            *numNameCharsOut = numProviderNameChars;
        }
        if (numProviderNameChars >= numNameChars)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }
        else if (name != nullptr)
        {
            memcpy(name, providerName, numProviderNameChars * sizeof(ep_char16_t));
        }
    }
    return hr;
}

EventPipeEvent * EventPipeAdapter::AddEvent(
    EventPipeProvider *provider,
    uint32_t eventID,
    LPCWSTR eventName,
    int64_t keywords,
    uint32_t eventVersion,
    EventPipeEventLevel level,
    uint8_t opcode,
    const EventPipeParameterDescAdapter &params,
    bool needStack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    size_t metadataLen = 0;
    uint8_t *metadata = ep_metadata_generator_generate_event_metadata(
        eventID,
        reinterpret_cast<const ep_char16_t *>(eventName),
        keywords,
        eventVersion,
        level,
        opcode,
        const_cast<EventPipeParameterDesc *>(params.GetParams()),
        params.GetParamsLen(),
        &metadataLen);
    if (metadata == nullptr)
    {
        return nullptr;
    }

    EventPipeEvent *realEvent = ep_provider_add_event(
        provider,
        eventID,
        keywords,
        eventVersion,
        level,
        needStack,
        metadata,
        static_cast<uint32_t>(metadataLen));
    ep_rt_byte_array_free(metadata);
    return realEvent;
}

EventPipeEvent * EventPipeAdapter::AddEvent(
    EventPipeProvider *provider,
    uint32_t eventID,
    int64_t keywords,
    uint32_t eventVersion,
    EventPipeEventLevel level,
    bool needStack,
    BYTE *metadata,
    uint32_t metadataLen)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    return ep_provider_add_event(provider, eventID, keywords, eventVersion, level, needStack, metadata, metadataLen);
}

void EventPipeAdapter::WriteEvent(
    EventPipeEvent *epEvent,
    BYTE *data,
    uint32_t dataLen,
    LPCGUID activityId,
    LPCGUID relatedActivityId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ep_write_event(
        epEvent,
        data,
        dataLen,
        reinterpret_cast<const uint8_t *>(activityId),
        reinterpret_cast<const uint8_t *>(relatedActivityId));
}

void EventPipeAdapter::WriteEvent(
    EventPipeEvent *epEvent,
    EventData *data,
    uint32_t dataLen,
    LPCGUID activityId,
    LPCGUID relatedActivityId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ep_write_event_2(
        epEvent,
        data,
        dataLen,
        reinterpret_cast<const uint8_t *>(activityId),
        reinterpret_cast<const uint8_t *>(relatedActivityId));
}

void EventPipeAdapter::WriteEvent(
    EventPipeEvent *epEvent,
    EventDataAdapter &data,
    LPCGUID activityId,
    LPCGUID relatedActivityId)
{
    WriteEvent(
        epEvent,
        const_cast<EventData *>(data.GetData()),
        data.GetDataLen(),
        activityId,
        relatedActivityId);
}

bool EventPipeAdapter::EventIsEnabled(const EventPipeEvent *epEvent)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_event_is_enabled(epEvent);
}

EventPipeEventInstance * EventPipeAdapter::GetNextEvent(EventPipeSessionID id)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    return ep_get_next_event(id);
}

EventPipeProvider * EventPipeAdapter::GetEventProvider(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_event_get_provider(ep_event_instance_get_ep_event(eventInstance));
}

uint32_t EventPipeAdapter::GetEventID(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_event_get_event_id(ep_event_instance_get_ep_event(eventInstance));
}

uint64_t EventPipeAdapter::GetEventThreadID(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_event_instance_get_thread_id(eventInstance);
}

int64_t EventPipeAdapter::GetEventTimestamp(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_event_instance_get_timestamp(eventInstance);
}

LPCGUID EventPipeAdapter::GetEventActivityID(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    static_assert(sizeof(GUID) == EP_ACTIVITY_ID_SIZE, "Size mismatch, sizeof(GUID) should be equal to EP_ACTIVITY_ID_SIZE");
    return reinterpret_cast<LPCGUID>(ep_event_instance_get_activity_id_cref(eventInstance));
}

LPCGUID EventPipeAdapter::GetEventRelativeActivityID(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    static_assert(sizeof(GUID) == EP_ACTIVITY_ID_SIZE, "Size mismatch, sizeof(GUID) should be equal to EP_ACTIVITY_ID_SIZE");
    return reinterpret_cast<LPCGUID>(ep_event_instance_get_related_activity_id_cref(eventInstance));
}

const BYTE * EventPipeAdapter::GetEventData(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_event_instance_get_data(eventInstance);
}

uint32_t EventPipeAdapter::GetEventDataLen(EventPipeEventInstance *eventInstance)
{
    STATIC_CONTRACT_NOTHROW;
    return ep_event_instance_get_data_len(eventInstance);
}

void EventPipeAdapter::ResumeSession(EventPipeSession *session)
{
    STATIC_CONTRACT_NOTHROW;
    ep_session_resume(session);
}

void EventPipeAdapter::PauseSession(EventPipeSession *session)
{
    STATIC_CONTRACT_NOTHROW;
    ep_session_pause(session);
}
