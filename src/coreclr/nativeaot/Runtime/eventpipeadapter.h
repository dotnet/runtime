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
};

#endif // FEATURE_PERFTRACING
#endif // __EVENTPIPE_ADAPTER_H__
