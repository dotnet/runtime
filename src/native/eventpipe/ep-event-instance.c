#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_EVENT_INSTANCE_GETTER_SETTER
#include "ep.h"
#include "ep-event.h"
#include "ep-event-instance.h"
#include "ep-stream.h"
#include "ep-rt.h"

/*
 * Forward declares of all static functions.
 */

static
void
sequence_point_fini (EventPipeSequencePoint *sequence_point);

/*
 * EventPipeEventInstance.
 */

EventPipeEventInstance *
ep_event_instance_alloc (
	EventPipeEvent *ep_event,
	uint32_t proc_num,
	uint64_t thread_id,
	const uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EventPipeEventInstance *instance = ep_rt_object_alloc (EventPipeEventInstance);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_event_instance_init (
		instance,
		ep_event,
		proc_num,
		thread_id,
		data,
		data_len,
		activity_id,
		related_activity_id) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_instance_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeEventInstance *
ep_event_instance_init (
	EventPipeEventInstance *event_instance,
	EventPipeEvent *ep_event,
	uint32_t proc_num,
	uint64_t thread_id,
	const uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (event_instance != NULL);

#ifdef EP_CHECKED_BUILD
	event_instance->debug_event_start = 0xDEADBEEF;
	event_instance->debug_event_end = 0xC0DEC0DE;
#endif

	event_instance->ep_event = ep_event;
	event_instance->proc_num = proc_num;
	event_instance->thread_id = thread_id;

	if (activity_id)
		memcpy (&(event_instance->activity_id), activity_id, EP_ACTIVITY_ID_SIZE);

	if (related_activity_id)
		memcpy (&(event_instance->related_activity_id), related_activity_id, EP_ACTIVITY_ID_SIZE);

	event_instance->data = data;
	event_instance->data_len = data_len;

	event_instance->timestamp = ep_perf_timestamp_get ();
	EP_ASSERT (event_instance->timestamp > 0);

	ep_event_instance_ensure_consistency (event_instance);

	return event_instance;
}

void
ep_event_instance_fini (EventPipeEventInstance *ep_event_instance)
{
	;
}

void
ep_event_instance_free (EventPipeEventInstance *ep_event_instance)
{
	ep_return_void_if_nok (ep_event_instance != NULL);

	ep_event_instance_fini (ep_event_instance);
	ep_rt_object_free (ep_event_instance);
}

bool
ep_event_instance_ensure_consistency (const EventPipeEventInstance *ep_event_instance)
{
#ifdef EP_CHECKED_BUILD
	EP_ASSERT (ep_event_instance->debug_event_start == 0xDEADBEEF);
	EP_ASSERT (ep_event_instance->debug_event_end == 0xC0DEC0DE);
#endif

	return true;
}

uint32_t
ep_event_instance_get_aligned_total_size (
	const EventPipeEventInstance *ep_event_instance,
	EventPipeSerializationFormat format)
{
	EP_ASSERT (ep_event_instance != NULL);

	// Calculate the size of the total payload so that it can be written to the file.
	uint32_t payload_len = 0;

	if (format == EP_SERIALIZATION_FORMAT_NETPERF_V3) {
		payload_len =
			// Metadata ID
			sizeof (ep_event_instance->metadata_id) +
			// Thread ID
			sizeof (uint32_t) +
			// TimeStamp
			sizeof (ep_event_instance->timestamp) +
			// Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Related Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Data payload length
			sizeof (ep_event_instance->data_len) +
			// Event payload data
			ep_event_instance->data_len +
			// Prepended stack payload size in bytes
			sizeof (uint32_t) +
			// Stack payload size
			ep_stack_contents_instance_get_size (&ep_event_instance->stack_contents_instance);
	} else if (format == EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		payload_len =
			// Metadata ID
			sizeof (ep_event_instance->metadata_id) +
			// Sequence number (implied by the buffer containing the event instance)
			sizeof (uint32_t) +
			// Thread ID
			sizeof (ep_event_instance->thread_id) +
			// Capture Thread ID (implied by the buffer containing the event instance)
			sizeof (uint64_t) +
			// ProcNumber
			sizeof (ep_event_instance->proc_num) +
			// Stack intern table id
			sizeof (uint32_t) +
			// TimeStamp
			sizeof (ep_event_instance->timestamp) +
			// Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Related Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Data payload length
			sizeof (ep_event_instance->data_len) +
			// Event payload data
			ep_event_instance->data_len;
	} else {
		EP_ASSERT (!"Unrecognized format");
	}

	// round up to FAST_SERIALIZER_ALIGNMENT_SIZE bytes
	if (payload_len % FAST_SERIALIZER_ALIGNMENT_SIZE != 0)
		payload_len += FAST_SERIALIZER_ALIGNMENT_SIZE - (payload_len % FAST_SERIALIZER_ALIGNMENT_SIZE);

	return payload_len;
}

#ifdef EP_CHECKED_BUILD
#include "ep-json-file.h"
#define MAX_JSON_FILE_MESSAGE_BUFFER_SIZE 512
void
ep_event_instance_serialize_to_json_file (
	EventPipeEventInstance *ep_event_instance,
	EventPipeJsonFile *json_file)
{
	ep_return_void_if_nok (ep_event_instance != NULL);
	ep_return_void_if_nok (json_file != NULL);


	ep_char8_t buffer [MAX_JSON_FILE_MESSAGE_BUFFER_SIZE];
	int32_t characters_written = -1;
	characters_written = ep_rt_utf8_string_snprintf (
		buffer,
		ARRAY_SIZE (buffer),
		"Provider=%s/EventID=%d/Version=%d",
		ep_provider_get_provider_name (ep_event_get_provider (ep_event_instance->ep_event)),
		ep_event_get_event_id (ep_event_instance->ep_event),
		ep_event_get_event_version (ep_event_instance->ep_event));

	if (characters_written > 0 && characters_written < (int32_t)ARRAY_SIZE (buffer))
		ep_json_file_write_event_data (json_file, ep_event_instance->timestamp, ep_rt_uint64_t_to_thread_id_t (ep_event_instance->thread_id), buffer, &ep_event_instance->stack_contents_instance);
}
#else
void
ep_event_instance_serialize_to_json_file (
	EventPipeEventInstance *ep_event_instance,
	EventPipeJsonFile *json_file)
{
	;
}
#endif

/*
 * EventPipeSequencePoint.
 */

static
void
sequence_point_fini (EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (sequence_point != NULL);

	// Each entry in the map owns a ref-count on the corresponding thread
	if (ep_rt_thread_sequence_number_map_count (&sequence_point->thread_sequence_numbers) != 0) {
		for (ep_rt_thread_sequence_number_hash_map_iterator_t iterator = ep_rt_thread_sequence_number_map_iterator_begin (&sequence_point->thread_sequence_numbers);
			!ep_rt_thread_sequence_number_map_iterator_end (&sequence_point->thread_sequence_numbers, &iterator);
			ep_rt_thread_sequence_number_map_iterator_next (&iterator)) {

			EventPipeThreadSessionState *key = ep_rt_thread_sequence_number_map_iterator_key (&iterator);
			ep_thread_release (ep_thread_session_state_get_thread (key));
		}
	}

	ep_rt_thread_sequence_number_map_free (&sequence_point->thread_sequence_numbers);
}


EventPipeSequencePoint *
ep_sequence_point_alloc (void)
{
	EventPipeSequencePoint *instance = ep_rt_object_alloc (EventPipeSequencePoint);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_sequence_point_init (instance) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_sequence_point_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeSequencePoint *
ep_sequence_point_init (EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (sequence_point != NULL);

	sequence_point->timestamp = 0;
	ep_rt_thread_sequence_number_map_alloc (&sequence_point->thread_sequence_numbers, NULL, NULL, NULL, NULL);
	return ep_rt_thread_sequence_number_map_is_valid (&sequence_point->thread_sequence_numbers) ? sequence_point : NULL;
}

void
ep_sequence_point_fini (EventPipeSequencePoint *sequence_point)
{
	ep_return_void_if_nok (sequence_point != NULL);
	sequence_point_fini (sequence_point);
}

void
ep_sequence_point_free (EventPipeSequencePoint *sequence_point)
{
	ep_return_void_if_nok (sequence_point != NULL);

	sequence_point_fini (sequence_point);
	ep_rt_object_free (sequence_point);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_event_instance;
const char quiet_linker_empty_file_warning_eventpipe_event_instance = 0;
#endif
