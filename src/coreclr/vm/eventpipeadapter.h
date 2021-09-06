// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPE_ADAPTER_H__
#define __EVENTPIPE_ADAPTER_H__

#if defined(FEATURE_PERFTRACING)

#include "ep.h"
#include "ep-provider.h"
#include "ep-config.h"
#include "ep-event.h"
#include "ep-event-instance.h"
#include "ep-session.h"
#include "ep-session-provider.h"
#include "ep-metadata-generator.h"
#include "ep-event-payload.h"
#include "ep-buffer-manager.h"


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
		// 	&& offsetof(EventPipeProviderConfiguration, keywords) == offsetof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG, keywords)
		// 	&& offsetof(EventPipeProviderConfiguration, logging_level) == offsetof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG, loggingLevel)
		// 	&& offsetof(EventPipeProviderConfiguration, filter_data) == offsetof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG, filterData)
		// 	&& sizeof(EventPipeProviderConfiguration) == sizeof(COR_PRF_EVENTPIPE_PROVIDER_CONFIG),
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

class EventPipeParameterDescAdapter final
{
public:
	EventPipeParameterDescAdapter(COR_PRF_EVENTPIPE_PARAM_DESC *params, uint32_t paramsLen)
	{
		STATIC_CONTRACT_NOTHROW;

#ifdef EP_INLINE_GETTER_SETTER
		static_assert(offsetof(EventPipeParameterDesc, type) == offsetof(COR_PRF_EVENTPIPE_PARAM_DESC, type)
			&& offsetof(EventPipeParameterDesc, element_type) == offsetof(COR_PRF_EVENTPIPE_PARAM_DESC, elementType)
			&& offsetof(EventPipeParameterDesc, name) == offsetof(COR_PRF_EVENTPIPE_PARAM_DESC, name)
			&& sizeof(EventPipeParameterDesc) == sizeof(COR_PRF_EVENTPIPE_PARAM_DESC),
			"Layouts of EventPipeParameterDesc type and COR_PRF_EVENTPIPE_PARAM_DESC type do not match!");
#endif
		m_params = reinterpret_cast<EventPipeParameterDesc *>(params);
		m_paramsLen = paramsLen;
	}

	inline const EventPipeParameterDesc * GetParams() const
	{
		STATIC_CONTRACT_NOTHROW;
		return m_params;
	}

	inline uint32_t GetParamsLen() const
	{
		STATIC_CONTRACT_NOTHROW;
		return m_paramsLen;
	}

private:
	EventPipeParameterDesc *m_params;
	uint32_t m_paramsLen;
};

class EventDataAdapter final
{
public:
	EventDataAdapter(COR_PRF_EVENT_DATA *data, uint32_t dataLen)
	{
		STATIC_CONTRACT_NOTHROW;

#ifdef EP_INLINE_GETTER_SETTER
		static_assert(offsetof(EventData, ptr) == offsetof(COR_PRF_EVENT_DATA, ptr)
			&& offsetof(EventData, size) == offsetof(COR_PRF_EVENT_DATA, size)
			&& sizeof(EventData) == sizeof(COR_PRF_EVENT_DATA),
			"Layouts of EventData type and COR_PRF_EVENT_DATA type do not match!");
#endif
		m_data = reinterpret_cast<EventData *>(data);
		m_dataLen = dataLen;
	}

	inline const EventData * GetData() const
	{
		STATIC_CONTRACT_NOTHROW;
		return m_data;
	}

	inline uint32_t GetDataLen() const
	{
		STATIC_CONTRACT_NOTHROW;
		return m_dataLen;
	}

private:
	EventData *m_data;
	uint32_t m_dataLen;
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
		CONTRACTL
		{
			NOTHROW;
			GC_TRIGGERS;
			MODE_ANY;
		}
		CONTRACTL_END;

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
		CONTRACTL
		{
			NOTHROW;
			GC_TRIGGERS;
			MODE_PREEMPTIVE;
		}
		CONTRACTL_END;

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

	static inline HANDLE GetWaitHandle(EventPipeSessionID id)
	{
		STATIC_CONTRACT_NOTHROW;
		return reinterpret_cast<HANDLE>(ep_get_wait_handle(id));
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

	static inline LONGLONG GetSessionStartTimestamp(EventPipeSession *session)
	{
		STATIC_CONTRACT_NOTHROW;

		_ASSERTE(session != NULL);
		return ep_session_get_session_start_timestamp(session);
	}

	static inline void AddProviderToSession(EventPipeSessionProvider *provider, EventPipeSession *session)
	{
		CONTRACTL
		{
			NOTHROW;
			GC_TRIGGERS;
			MODE_PREEMPTIVE;
		}
		CONTRACTL_END;

		ep_add_provider_to_session (provider, session);
	}

	static inline EventPipeProvider * CreateProvider(const SString &providerName, EventPipeCallback callback)
	{
		CONTRACTL
		{
			NOTHROW;
			GC_TRIGGERS;
			MODE_ANY;
		}
		CONTRACTL_END;

		ep_char8_t *providerNameUTF8 = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerName.GetUnicode ()), -1);
		EventPipeProvider * provider = ep_create_provider (providerNameUTF8, callback, NULL, NULL);
		ep_rt_utf8_string_free (providerNameUTF8);
		return provider;
	}

	static inline void DeleteProvider (EventPipeProvider * provider)
	{
		CONTRACTL
		{
			NOTHROW;
			GC_TRIGGERS;
			MODE_ANY;
		}
		CONTRACTL_END;

		ep_delete_provider (provider);
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

	static EventPipeSessionProvider * CreateSessionProvider(const EventPipeProviderConfigurationAdapter &providerConfig)
	{
		CONTRACTL
		{
			NOTHROW;
			GC_NOTRIGGER;
			MODE_ANY;
		}
		CONTRACTL_END;

		_ASSERTE (providerConfig.GetProviderConfigs() != NULL && providerConfig.GetProviderConfigsLen() == 1);
		const EventPipeProviderConfiguration *config = providerConfig.GetProviderConfigs();
		if (!config)
			return NULL;

		return ep_session_provider_alloc (
			ep_provider_config_get_provider_name (&config[0]),
			ep_provider_config_get_keywords (&config[0]),
			(EventPipeEventLevel)ep_provider_config_get_logging_level (&config[0]),
			ep_provider_config_get_filter_data (&config[0]));
	}

	static HRESULT GetProviderName(const EventPipeProvider *provider, ULONG numNameChars, ULONG *numNameCharsOut, LPWSTR name)
	{
		CONTRACTL
		{
			NOTHROW;
			GC_NOTRIGGER;
			MODE_ANY;
		}
		CONTRACTL_END;

		_ASSERTE(provider != NULL);

		HRESULT hr = S_OK;
		const ep_char16_t *providerName = ep_provider_get_provider_name_utf16 (provider);
		if (providerName) {
			uint32_t numProviderNameChars = (uint32_t)(ep_rt_utf16_string_len (providerName) + 1);
			if (numNameCharsOut)
				*numNameCharsOut = numProviderNameChars;
			if (numProviderNameChars >= numNameChars)
				hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
			else if (name)
				memcpy (name, providerName, numProviderNameChars * sizeof (ep_char16_t));
		}
		return hr;
	}

	static EventPipeEvent * AddEvent(
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
		EventPipeEvent *realEvent = NULL;
		uint8_t *metadata = ep_metadata_generator_generate_event_metadata (
			eventID,
			reinterpret_cast<const ep_char16_t *>(eventName),
			keywords,
			eventVersion,
			level,
			opcode,
			(EventPipeParameterDesc *)params.GetParams(),
			params.GetParamsLen(),
			&metadataLen);
		if (metadata) {
			realEvent = ep_provider_add_event(
				provider,
				eventID,
				keywords,
				eventVersion,
				level,
				needStack,
				metadata,
				(uint32_t)metadataLen);
			ep_rt_byte_array_free(metadata);
		}
		return realEvent;
	}

	static inline EventPipeEvent * AddEvent(
		EventPipeProvider *provider,
		uint32_t eventID,
		int64_t keywords,
		uint32_t eventVersion,
		EventPipeEventLevel level,
		bool needStack,
		BYTE *metadata = NULL,
		uint32_t metadataLen = 0)
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

	static inline void WriteEvent(
		EventPipeEvent *ep_event,
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
			ep_event,
			data,
			dataLen,
			reinterpret_cast<const uint8_t*>(activityId),
			reinterpret_cast<const uint8_t*>(relatedActivityId));
	}

	static inline void WriteEvent(
		EventPipeEvent *ep_event,
		EventDataAdapter &data,
		LPCGUID activityId,
		LPCGUID relatedActivityId)
	{
		WriteEvent(
			ep_event,
			(EventData*)data.GetData(),
			data.GetDataLen(),
			activityId,
			relatedActivityId);
	}

	static inline bool EventIsEnabled (const EventPipeEvent *epEvent)
	{
		STATIC_CONTRACT_NOTHROW;
		return ep_event_is_enabled(epEvent);
	}

	static inline EventPipeEventInstance * GetNextEvent (EventPipeSessionID id)
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

	static inline LPCGUID GetEventActivityID (EventPipeEventInstance *eventInstance)
	{
		STATIC_CONTRACT_NOTHROW;
		static_assert(sizeof(GUID) == EP_ACTIVITY_ID_SIZE, "Size missmatch, sizeof(GUID) should be equal to EP_ACTIVITY_ID_SIZE");
		return reinterpret_cast<LPCGUID>(ep_event_instance_get_activity_id_cref(eventInstance));
	}

	static inline LPCGUID GetEventRelativeActivityID (EventPipeEventInstance *eventInstance)
	{
		STATIC_CONTRACT_NOTHROW;
		static_assert(sizeof(GUID) == EP_ACTIVITY_ID_SIZE, "Size missmatch, sizeof(GUID) should be equal to EP_ACTIVITY_ID_SIZE");
		return reinterpret_cast<LPCGUID>(ep_event_instance_get_related_activity_id_cref(eventInstance));
	}

	static inline const BYTE * GetEventData (EventPipeEventInstance *eventInstance)
	{
		STATIC_CONTRACT_NOTHROW;
		return ep_event_instance_get_data(eventInstance);
	}

	static inline uint32_t GetEventDataLen (EventPipeEventInstance *eventInstance)
	{
		STATIC_CONTRACT_NOTHROW;
		return ep_event_instance_get_data_len(eventInstance);
	}

	static inline void ResumeSession (EventPipeSession *session)
	{
		STATIC_CONTRACT_NOTHROW;
		ep_session_resume (session);
	}

	static inline void PauseSession (EventPipeSession *session)
	{
		STATIC_CONTRACT_NOTHROW;
		ep_session_pause (session);
	}
};

#endif // FEATURE_PERFTRACING
#endif // __EVENTPIPE_ADAPTER_H__
