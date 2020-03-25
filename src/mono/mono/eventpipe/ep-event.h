#ifndef __EVENTPIPE_EVENT_H__
#define __EVENTPIPE_EVENT_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeEvent.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeEvent {
#else
struct _EventPipeEvent_Internal {
#endif
	EventPipeProvider *provider;
	uint64_t keywords;
	uint32_t event_id;
	uint32_t event_version;
	EventPipeEventLevel level;
	bool need_stack;
	volatile int64_t enabled_mask;
	uint8_t *metadata;
	uint32_t metadata_len;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeEvent {
	uint8_t _internal [sizeof (struct _EventPipeEvent_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeEvent *, event, EventPipeProvider *, provider)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint64_t, keywords)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint32_t, event_id)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint32_t, event_version)
EP_DEFINE_GETTER(EventPipeEvent *, event, EventPipeEventLevel, level)
EP_DEFINE_GETTER(EventPipeEvent *, event, bool, need_stack)
EP_DEFINE_GETTER(EventPipeEvent *, event, int64_t, enabled_mask)
EP_DEFINE_SETTER(EventPipeEvent *, event, int64_t, enabled_mask)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint8_t *, metadata)
EP_DEFINE_SETTER(EventPipeEvent *, event, uint8_t *, metadata)
EP_DEFINE_GETTER(EventPipeEvent *, event, uint32_t, metadata_len)
EP_DEFINE_SETTER(EventPipeEvent *, event, uint32_t, metadata_len)

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
#endif /** __EVENTPIPE_EVENT_H__ **/
