// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPE_ADAPTER_H__
#define __EVENTPIPE_ADAPTER_H__

#if defined(FEATURE_PERFTRACING)

#include <eventpipe/ep.h>
#include <eventpipe/ep-provider.h>
#include <eventpipe/ep-config.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-event-instance.h>
#include <eventpipe/ep-session.h>
#include <eventpipe/ep-session-provider.h>
#include <eventpipe/ep-metadata-generator.h>
#include <eventpipe/ep-event-payload.h>
#include <eventpipe/ep-buffer-manager.h>

#include "gcenv.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "SpinLock.h"

typedef /* [public][public][public] */ struct __MIDL___MIDL_itf_corprof_0000_0000_0015
{
const WCHAR *providerName;
uint64_t keywords;
uint32_t loggingLevel;
const WCHAR *filterData;
}   COR_PRF_EVENTPIPE_PROVIDER_CONFIG;


class EventPipeProviderConfigurationAdapter final
{
public:
    EventPipeProviderConfigurationAdapter(const COR_PRF_EVENTPIPE_PROVIDER_CONFIG *providerConfigs, uint32_t providerConfigsLen)
    {
        STATIC_CONTRACT_NOTHROW;

        // This static_assert will fail because EventPipeProviderConfiguration uses char8_t strings rather than char16_t strings.
        // This method takes the COR_PRF variant and converts to char8_t strings, so it should be fine.
        // Leaving the assert commented out here for posterity.
        //
        // static_assert(offsetof(EventPipeProviderConfiguration, provider_name) == offsetof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG, providerName)
        //     && offsetof(EventPipeProviderConfiguration, keywords) == offsetof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG, keywords)
        //     && offsetof(EventPipeProviderConfiguration, logging_level) == offsetof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG, loggingLevel)
        //     && offsetof(EventPipeProviderConfiguration, filter_data) == offsetof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG, filterData)
        //     && sizeof(EventPipeProviderConfiguration) == sizeof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG),
        // "Layouts of EventPipeProviderConfiguration type and COR_PRF_EVENTPIPE_PROVIDER_CONFIG type do not match!");

        m_providerConfigs = new (nothrow) EventPipeProviderConfiguration[providerConfigsLen];
        m_providerConfigsLen = providerConfigsLen;
        if (m_providerConfigs) {
            for (uint32_t i = 0; i < providerConfigsLen; ++i) {
                ep_provider_config_init (
                    &m_providerConfigs[i],
                    ep_rt_utf16_to_utf8_string (reinterpret_cast<const ep_char16_t *>(providerConfigs[i].providerName), -1),
                    providerConfigs[i].keywords,
                    static_cast<EventPipeEventLevel>(providerConfigs[i].loggingLevel),
                    ep_rt_utf16_to_utf8_string (reinterpret_cast<const ep_char16_t *>(providerConfigs[i].filterData), -1));
            }
        }
    }

    ~EventPipeProviderConfigurationAdapter()
    {
        STATIC_CONTRACT_NOTHROW;
        if (m_providerConfigs) {
            for (uint32_t i = 0; i < m_providerConfigsLen; ++i) {
                ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_provider_name (&m_providerConfigs[i]));
                ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_filter_data (&m_providerConfigs[i]));
            }
            delete [] m_providerConfigs;
        }
    }

    inline const EventPipeProviderConfiguration * GetProviderConfigs() const
    {
        STATIC_CONTRACT_NOTHROW;
        return m_providerConfigs;
    }

    inline uint32_t GetProviderConfigsLen() const
    {
        STATIC_CONTRACT_NOTHROW;
        return m_providerConfigsLen;
    }

private:
    EventPipeProviderConfiguration *m_providerConfigs;
    uint32_t m_providerConfigsLen;
};


class EventPipeAdapter final
{
public:
    static inline void Initialize()
    {
        CONTRACTL
        {
            NOTHROW;
        }
        CONTRACTL_END;

        ep_init();
    }

    static inline EventPipeProvider * CreateProvider(LPCWSTR providerName, EventPipeCallback callback, void* pCallbackContext = nullptr)
    {
        ep_char8_t *providerNameUTF8 = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerName), -1);
        EventPipeProvider * provider = ep_create_provider (providerNameUTF8, callback, pCallbackContext);
        ep_rt_utf8_string_free (providerNameUTF8);
        return provider;
    }

    static inline void DeleteProvider (EventPipeProvider * provider)
    {
        ep_delete_provider (provider);
    }


    static inline void FinishInitialize()
    {
        CONTRACTL
        {
            NOTHROW;
        }
        CONTRACTL_END;

        ep_finish_init();
    }

    static inline void Shutdown()
    {
        ep_shutdown();
    }

    static inline bool Enabled()
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_enabled();
    }

    static inline EventPipeSessionID Enable(
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
        STATIC_CONTRACT_NOTHROW;

        ep_char8_t *outputPathUTF8 = NULL;
        if (outputPath)
            outputPathUTF8 = ep_rt_utf16_to_utf8_string (reinterpret_cast<const ep_char16_t *>(outputPath), -1);
        EventPipeSessionID result = ep_enable (
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
        ep_rt_utf8_string_free (outputPathUTF8);
        return result;
    }

    static inline void Disable(EventPipeSessionID id)
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

    static inline void StartStreaming(EventPipeSessionID id)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        ep_start_streaming(id);
    }

    static inline EventPipeSession * GetSession(EventPipeSessionID id)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_get_session(id);
    }

    static inline bool SignalSession(EventPipeSessionID id)
    {
        STATIC_CONTRACT_NOTHROW;

        EventPipeSession *const session = ep_get_session (id);
        if (!session)
            return false;

        return ep_rt_wait_event_set (ep_session_get_wait_event (session));
    }

    static inline bool WaitForSessionSignal(EventPipeSessionID id, int32_t timeoutMs)
    {
        STATIC_CONTRACT_NOTHROW;

        EventPipeSession *const session = ep_get_session (id);
        if (!session)
            return false;

        return !ep_rt_wait_event_wait (ep_session_get_wait_event (session), (uint32_t)timeoutMs, false) ? true : false;
    }

    static inline FILETIME GetSessionStartTime(EventPipeSession *session)
    {
        STATIC_CONTRACT_NOTHROW;

        FILETIME fileTime;
        LARGE_INTEGER largeValue;

        _ASSERTE(session != NULL);
        largeValue.QuadPart = ep_session_get_session_start_time(session);
        fileTime.dwLowDateTime = largeValue.u.LowPart;
        fileTime.dwHighDateTime = largeValue.u.HighPart;
        return fileTime;
    }

    static inline int64_t GetSessionStartTimestamp(EventPipeSession *session)
    {
        STATIC_CONTRACT_NOTHROW;

        _ASSERTE(session != NULL);
        return ep_session_get_session_start_timestamp(session);
    }

    static inline EventPipeProvider * GetProvider (LPCWSTR providerName)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (!providerName)
            return NULL;

        ep_char8_t *providerNameUTF8 = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerName), -1);
        EventPipeProvider * provider = ep_get_provider (providerNameUTF8);
        ep_rt_utf8_string_free(providerNameUTF8);
        return provider;
    }

    static inline EventPipeEvent * AddEvent(
        EventPipeProvider *provider,
        uint32_t eventID,
        int64_t keywords,
        uint32_t eventVersion,
        EventPipeEventLevel level,
        bool needStack,
        uint8_t *metadata = NULL,
        uint32_t metadataLen = 0)
    {
        return ep_provider_add_event(provider, eventID, keywords, eventVersion, level, needStack, metadata, metadataLen);
    }

    static inline void WriteEvent(
        EventPipeEvent *ep_event,
        uint8_t *data,
        uint32_t dataLen,
        const GUID * activityId,
        const GUID * relatedActivityId)
    {
        ep_write_event(
            ep_event,
            data,
            dataLen,
            reinterpret_cast<const uint8_t*>(activityId),
            reinterpret_cast<const uint8_t*>(relatedActivityId));
    }

    static inline void WriteEvent(
        EventPipeEvent *ep_event,
        EventData *data,
        uint32_t dataLen,
        const GUID * activityId,
        const GUID * relatedActivityId)
    {
        ep_write_event_2(
            ep_event,
            data,
            dataLen,
            reinterpret_cast<const uint8_t*>(activityId),
            reinterpret_cast<const uint8_t*>(relatedActivityId));
    }

    static inline bool EventIsEnabled (const EventPipeEvent *epEvent)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_event_is_enabled(epEvent);
    }

    static inline EventPipeEventInstance * GetNextEvent (EventPipeSessionID id)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_get_next_event(id);
    }

    static inline EventPipeProvider * GetEventProvider (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_event_get_provider(ep_event_instance_get_ep_event(eventInstance));
    }

    static inline uint32_t GetEventID (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_event_get_event_id(ep_event_instance_get_ep_event(eventInstance));
    }

    static inline uint64_t GetEventThreadID (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_event_instance_get_thread_id(eventInstance);
    }

    static inline int64_t GetEventTimestamp (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_event_instance_get_timestamp(eventInstance);
    }

    static inline const GUID * GetEventActivityID (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        static_assert(sizeof(GUID) == EP_ACTIVITY_ID_SIZE, "Size mismatch, sizeof(GUID) should be equal to EP_ACTIVITY_ID_SIZE");
        return reinterpret_cast<const GUID *>(ep_event_instance_get_activity_id_cref(eventInstance));
    }

    static inline const GUID * GetEventRelativeActivityID (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        static_assert(sizeof(GUID) == EP_ACTIVITY_ID_SIZE, "Size mismatch, sizeof(GUID) should be equal to EP_ACTIVITY_ID_SIZE");
        return reinterpret_cast<const GUID *>(ep_event_instance_get_related_activity_id_cref(eventInstance));
    }

    static inline const uint8_t * GetEventData (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_event_instance_get_data(eventInstance);
    }

    static inline uint32_t GetEventDataLen (EventPipeEventInstance *eventInstance)
    {
        STATIC_CONTRACT_NOTHROW;
        return ep_event_instance_get_data_len(eventInstance);
    }
};

#endif // FEATURE_PERFTRACING
#endif // __EVENTPIPE_ADAPTER_H__
