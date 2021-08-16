#ifndef __EVENTPIPE_EVENT_H__
#define __EVENTPIPE_EVENT_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-rt.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_EVENT_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeEvent.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EVENT_GETTER_SETTER)
struct _EventPipeEvent {
#else
struct _EventPipeEvent_Internal {
#endif
	// Bit vector containing the keywords that enable the event.
	uint64_t keywords;
	// The ith bit is 1 iff the event is enabled for the ith session.
	volatile int64_t enabled_mask;
	// Metadata
	uint8_t *metadata;
	// The provider that contains the event.
	EventPipeProvider *provider;
	// The ID (within the provider) of the event.
	uint32_t event_id;
	// The version of the event.
	uint32_t event_version;
	// Metadata length;
	uint32_t metadata_len;
	// The verbosity of the event.
	EventPipeEventLevel level;
	// True if a call stack should be captured when writing the event.
	bool need_stack;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EVENT_GETTER_SETTER)
struct _EventPipeEvent {
	uint8_t _internal [sizeof (struct _EventPipeEvent_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeEvent *, event, uint64_t, keywords)
EP_DEFINE_GETTER_REF(EventPipeEvent *, event, volatile int64_t *, enabled_mask)
EP_DEFINE_SETTER(EventPipeEvent *, event, int64_t, enabled_mask)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint8_t *, metadata)
EP_DEFINE_GETTER(EventPipeEvent *, event, EventPipeProvider *, provider)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint32_t, event_id)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint32_t, event_version)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint32_t, metadata_len)
EP_DEFINE_GETTER(EventPipeEvent *, event, EventPipeEventLevel, level)
EP_DEFINE_GETTER(EventPipeEvent *, event, bool, need_stack)

static
inline
bool
ep_event_is_enabled (const EventPipeEvent *ep_event)
{
	return (ep_rt_volatile_load_int64_t (ep_event_get_enabled_mask_cref (ep_event)) != 0);
}

static
inline
bool
ep_event_is_enabled_by_mask (
	const EventPipeEvent *ep_event,
	uint64_t session_mask)
{
	EP_ASSERT (ep_event_get_provider (ep_event) != NULL);
	return (ep_provider_is_enabled_by_mask (ep_event_get_provider (ep_event), session_mask) && ((ep_rt_volatile_load_int64_t (ep_event_get_enabled_mask_cref (ep_event)) & session_mask) != 0));
}

EventPipeEvent *
ep_event_alloc (
	EventPipeProvider *provider,
	uint64_t keywords,
	uint32_t event_id,
	uint32_t event_version,
	EventPipeEventLevel level,
	bool need_stack,
	const uint8_t *metadata,
	uint32_t metadata_len);

void
ep_event_free (EventPipeEvent * ep_event);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_EVENT_H__ */
