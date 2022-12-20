#ifndef __EVENTPIPE_EVENT_INSTANCE_H__
#define __EVENTPIPE_EVENT_INSTANCE_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-stack-contents.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_EVENT_INSTANCE_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeEventInstance.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EVENT_INSTANCE_GETTER_SETTER)
struct _EventPipeEventInstance {
#else
struct _EventPipeEventInstance_Internal {
#endif
	uint8_t activity_id [EP_ACTIVITY_ID_SIZE];
	uint8_t related_activity_id [EP_ACTIVITY_ID_SIZE];
	uint64_t thread_id;
	ep_timestamp_t timestamp;
	EventPipeEvent *ep_event;
	const uint8_t *data;
	uint32_t metadata_id;
	uint32_t proc_num;
	uint32_t data_len;
#ifdef EP_CHECKED_BUILD
	uint32_t debug_event_start;
	uint32_t debug_event_end;
#endif
	EventPipeStackContentsInstance stack_contents_instance;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EVENT_INSTANCE_GETTER_SETTER)
struct _EventPipeEventInstance {
	uint8_t _internal [sizeof (struct _EventPipeEventInstance_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, EventPipeEvent *, ep_event)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, metadata_id)
EP_DEFINE_SETTER(EventPipeEventInstance *, event_instance, uint32_t, metadata_id)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, proc_num)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint64_t, thread_id)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, ep_timestamp_t, timestamp)
EP_DEFINE_SETTER(EventPipeEventInstance *, event_instance, ep_timestamp_t, timestamp)
EP_DEFINE_GETTER_ARRAY_REF(EventPipeEventInstance *, event_instance, uint8_t *, const uint8_t *, activity_id, activity_id[0])
EP_DEFINE_GETTER_ARRAY_REF(EventPipeEventInstance *, event_instance, uint8_t *, const uint8_t *, related_activity_id, related_activity_id[0])
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, const uint8_t *, data)
EP_DEFINE_GETTER(EventPipeEventInstance *, event_instance, uint32_t, data_len)
EP_DEFINE_GETTER_REF(EventPipeEventInstance *, event_instance, EventPipeStackContentsInstance *, stack_contents_instance)

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

void
ep_event_instance_serialize_to_json_file (
	EventPipeEventInstance *ep_event_instance,
	EventPipeJsonFile *json_file);

static
inline
uint32_t
ep_event_instance_get_flattened_size (const EventPipeEventInstance *ep_event_instance)
{
	EP_ASSERT (ep_event_instance != NULL);
	return ep_event_instance_get_data (ep_event_instance) ?
		sizeof (*ep_event_instance) - sizeof (ep_event_instance->stack_contents_instance.stack_frames) + ep_stack_contents_instance_get_full_size (ep_event_instance_get_stack_contents_instance_cref (ep_event_instance)) + ep_event_instance_get_data_len (ep_event_instance) :
		sizeof (*ep_event_instance) - sizeof (ep_event_instance->stack_contents_instance.stack_frames) + ep_stack_contents_instance_get_full_size (ep_event_instance_get_stack_contents_instance_cref (ep_event_instance));
}

/*
 * EventPipeSequencePoint.
 */

// A point in time marker that is used as a boundary when emitting events.
// The events in a Nettrace file are not emitted in a fully sorted order
// but we do guarantee that all events before a sequence point are emitted
// prior to any events after the sequence point.
#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_EVENT_INSTANCE_GETTER_SETTER)
struct _EventPipeSequencePoint {
#else
struct _EventPipeSequencePoint_Internal {
#endif
	ep_rt_thread_sequence_number_hash_map_t thread_sequence_numbers;
	ep_timestamp_t timestamp;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_EVENT_INSTANCE_GETTER_SETTER)
struct _EventPipeSequencePoint {
	uint8_t _internal [sizeof (struct _EventPipeSequencePoint_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(EventPipeSequencePoint *, sequence_point, ep_rt_thread_sequence_number_hash_map_t *, thread_sequence_numbers)
EP_DEFINE_GETTER(EventPipeSequencePoint *, sequence_point, ep_timestamp_t, timestamp)
EP_DEFINE_SETTER(EventPipeSequencePoint *, sequence_point, ep_timestamp_t, timestamp)

EventPipeSequencePoint *
ep_sequence_point_alloc (void);

EventPipeSequencePoint *
ep_sequence_point_init (EventPipeSequencePoint *sequence_point);

void
ep_sequence_point_fini (EventPipeSequencePoint *sequence_point);

void
ep_sequence_point_free (EventPipeSequencePoint *sequence_point);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_EVENT_INSTANCE_H__ */
