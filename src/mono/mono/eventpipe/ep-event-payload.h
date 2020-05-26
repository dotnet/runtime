#ifndef __EVENTPIPE_EVENT_PAYLOAD_H__
#define __EVENTPIPE_EVENT_PAYLOAD_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventData.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventData {
#else
struct _EventData_Internal {
#endif
	uint64_t ptr;
	uint32_t size;
	uint32_t reserved;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventData {
	uint8_t _internal [sizeof (struct _EventData_Internal)];
};
#endif

EP_DEFINE_GETTER(EventData *, event_data, uint64_t, ptr)
EP_DEFINE_GETTER(EventData *, event_data, uint32_t, size)
EP_DEFINE_GETTER(EventData *, event_data, uint32_t, reserved)

EventData *
ep_event_data_alloc (
	uint64_t ptr,
	uint32_t size,
	uint32_t reserved);

EventData *
ep_event_data_init (
	EventData *event_data,
	uint64_t ptr,
	uint32_t size,
	uint32_t reserved);

void
ep_event_data_fini (EventData *event_data);

void
ep_event_data_free (EventData *event_data);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_EVENT_PAYLOAD_H__ **/
