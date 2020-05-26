#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * EventPipeEventInstance.
 */

bool
ep_event_instance_ensure_consistency (const EventPipeEventInstance *ep_event_instance)
{
#ifdef EP_CHECKED_BUILD
	EP_ASSERT (ep_event_instance_get_debug_event_start (ep_event_instance) == 0xDEADBEEF);
	EP_ASSERT (ep_event_instance_get_debug_event_end (ep_event_instance) == 0xCAFEBABE);
#endif

	return true;
}

uint32_t
ep_event_instance_get_aligned_total_size (
	const EventPipeEventInstance *ep_event_instance,
	EventPipeSerializationFormat format)
{
	// Calculate the size of the total payload so that it can be written to the file.
	uint32_t payload_len = 0;

	if (format == EP_SERIALIZATION_FORMAT_NETPERF_V3) {
		payload_len =
			// Metadata ID
			ep_event_instance_sizeof_metadata_id (ep_event_instance) +
			// Thread ID
			sizeof (int32_t) +
			// TimeStamp
			ep_event_instance_sizeof_timestamp (ep_event_instance) +
			// Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Related Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Data payload length
			ep_event_instance_sizeof_data_len (ep_event_instance) +
			// Event payload data
			ep_event_instance_get_data_len (ep_event_instance) +
			// Prepended stack payload size in bytes
			sizeof (uint32_t) +
			// Stack payload size
			ep_stack_contents_get_size (ep_event_instance_get_stack_contents_cref (ep_event_instance));
	} else if (format == EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		payload_len =
			// Metadata ID
			ep_event_instance_sizeof_metadata_id (ep_event_instance) +
			// Sequence number (implied by the buffer containing the event instance)
			sizeof (uint32_t) +
			// Thread ID
			sizeof (int32_t) +
			// Capture Thread ID (implied by the buffer containing the event instance)
			sizeof (uint64_t) +
			// ProcNumber
			ep_event_instance_sizeof_proc_num (ep_event_instance) +
			// Stack intern table id
			sizeof (uint32_t) +
			// TimeStamp
			ep_event_instance_sizeof_timestamp (ep_event_instance) +
			// Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Related Activity ID
			EP_ACTIVITY_ID_SIZE +
			// Data payload length
			ep_event_instance_sizeof_data_len (ep_event_instance) +
			// Event payload data
			ep_event_instance_get_data_len (ep_event_instance);
	} else {
		EP_ASSERT (!"Unrecognized format");
	}

	// round up to FAST_SERIALIZER_ALIGNMENT_SIZE bytes
	if (payload_len % FAST_SERIALIZER_ALIGNMENT_SIZE != 0)
		payload_len += FAST_SERIALIZER_ALIGNMENT_SIZE - (payload_len % FAST_SERIALIZER_ALIGNMENT_SIZE);

	return payload_len;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_event_instance;
const char quiet_linker_empty_file_warning_eventpipe_event_instance = 0;
#endif
