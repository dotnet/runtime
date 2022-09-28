#ifndef __EVENTPIPE_EVENT_PAYLOAD_H__
#define __EVENTPIPE_EVENT_PAYLOAD_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_EVENT_PAYLOAD_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventData.
 */

//NOTE, layout needs to match COR_PRF_EVENT_DATA.
#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EVENT_PAYLOAD_GETTER_SETTER)
struct _EventData {
#else
struct _EventData_Internal {
#endif
	uint64_t ptr;
	uint32_t size;
	uint32_t reserved;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EVENT_PAYLOAD_GETTER_SETTER)
struct _EventData {
	uint8_t _internal [sizeof (struct _EventData_Internal)];
};
#endif

EP_DEFINE_GETTER(EventData *, event_data, uint64_t, ptr)
EP_DEFINE_GETTER(EventData *, event_data, uint32_t, size)

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

/*
 * EventPipeEventPayload.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EVENT_PAYLOAD_GETTER_SETTER)
struct _EventPipeEventPayload {
#else
struct _EventPipeEventPayload_Internal {
#endif
	uint8_t *data;
	EventData *event_data;
	uint32_t event_data_len;
	uint32_t size;
	bool allocated_data;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EVENT_PAYLOAD_GETTER_SETTER)
struct _EventPipeEventPayload {
	uint8_t _internal [sizeof (struct _EventPipeEventPayload_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeEventPayload *, event_payload, uint8_t *, data)
EP_DEFINE_GETTER(EventPipeEventPayload *, event_payload, uint32_t, size)

static
inline
bool
ep_event_payload_is_flattened (const EventPipeEventPayload *event_payload)
{
	return (ep_event_payload_get_data (event_payload) != NULL);
}

// Build this payload with a flat buffer inside.
EventPipeEventPayload *
ep_event_payload_init (
	EventPipeEventPayload *event_payload,
	uint8_t *data,
	uint32_t len);

// Build this payload to contain an array of EventData objects.
EventPipeEventPayload *
ep_event_payload_init_2 (
	EventPipeEventPayload *event_payload,
	EventData *event_data,
	uint32_t event_data_len);

// If a buffer was allocated internally, delete it.
void
ep_event_payload_fini (EventPipeEventPayload *event_payload);

// Copy the data (whether flat or array of objects) into a flat buffer at dst
// Assumes that dst points to an appropriately sized buffer.
void
ep_event_payload_copy_data (
	EventPipeEventPayload *event_payload,
	uint8_t *dst);

// If the data is stored only as an array of EventData objects,
// create a flat buffer and copy into it
void
ep_event_payload_flatten (EventPipeEventPayload *event_payload);

// Get the flat formatted data in this payload.
// This method will allocate a buffer if it does not already contain flattened data.
// This method will return NULL on OOM if a buffer needed to be allocated.
uint8_t *
ep_event_payload_get_flat_data (EventPipeEventPayload *event_payload);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_EVENT_PAYLOAD_H__ */
