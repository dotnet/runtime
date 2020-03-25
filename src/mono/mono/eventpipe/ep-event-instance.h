#ifndef __EVENTPIPE_EVENTINSTANCE_H__
#define __EVENTPIPE_EVENTINSTANCE_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeEventInstance.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeEventInstance {
#else
struct _EventPipeEventInstance_Internal {
#endif
#ifdef EP_CHECKED_BUILD
	uint32_t debug_event_start;
	uint32_t debug_event_end;
#endif
	EventPipeEvent *ep_event;
	uint32_t metadata_id;
	uint32_t proc_num;
	uint64_t thread_id;
	uint64_t timestamp;
	uint8_t activity_id [EP_ACTIVITY_ID_SIZE];
	uint8_t related_activity_id [EP_ACTIVITY_ID_SIZE];
	const uint8_t *data;
	uint32_t data_len;
	EventPipeStackContents stack_contents;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeEventInstance {
	uint8_t _internal [sizeof (struct _EventPipeEventInstance_Internal)];
};
#endif

#ifdef EP_CHECKED_BUILD
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, debug_event_start)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, debug_event_end)
#endif
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, EventPipeEvent *, ep_event)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, metadata_id)
EP_DEFINE_SETTER(EventPipeEventInstance *, event_instance, uint32_t, metadata_id)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, proc_num)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint64_t, thread_id)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint64_t, timestamp)
EP_DEFINE_SETTER(EventPipeEventInstance *, event_instance, uint64_t, timestamp)
EP_DEFINE_GETTER_ARRAY_REF(EventPipeEventInstance *, event_instance, uint8_t *, const uint8_t *, activity_id, activity_id[0])
EP_DEFINE_GETTER_ARRAY_REF(EventPipeEventInstance *, event_instance, uint8_t *, const uint8_t *, related_activity_id, related_activity_id[0])
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, const uint8_t *, data)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, data_len)
EP_DEFINE_GETTER_REF(EventPipeEventInstance *, event_instance, EventPipeStackContents *, stack_contents)

EventPipeEventInstance *
ep_event_instance_alloc (
	EventPipeEvent *ep_event,
	uint32_t proc_num,
	uint64_t thread_id,
	const uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

EventPipeEventInstance *
ep_event_instance_init (
	EventPipeEventInstance *ep_event_instance,
	EventPipeEvent *ep_event,
	uint32_t proc_num,
	uint64_t thread_id,
	const uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

void
ep_event_instance_fini (EventPipeEventInstance *ep_event_instance);

void
ep_event_instance_free (EventPipeEventInstance *ep_event_instance);

bool
ep_event_instance_ensure_consistency (const EventPipeEventInstance *ep_event_instance);

uint32_t
ep_event_instance_get_aligned_total_size (
	const EventPipeEventInstance *ep_event_instance,
	EventPipeSerializationFormat format);

/*
 * EventPipeSequencePoint.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeSequencePoint {
#else
struct _EventPipeSequencePoint_Internal {
#endif
	uint64_t timestamp;
	ep_rt_thread_sequence_number_hash_map_t thread_sequence_numbers;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeSequencePoint {
	uint8_t _internal [sizeof (struct _EventPipeSequencePoint_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeSequencePoint *, sequence_point, uint64_t, timestamp)
EP_DEFINE_GETTER_REF(EventPipeSequencePoint *, sequence_point, ep_rt_thread_sequence_number_hash_map_t *, thread_sequence_numbers)

EventPipeSequencePoint *
ep_sequence_point_init (EventPipeSequencePoint *sequence_point);

void
ep_sequence_point_fini (EventPipeSequencePoint *sequence_point);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_EVENTINSTANCE_H__ **/
