#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

EventPipeConfiguration _ep_config = { { 0 }, 0 };

/*
 * EventPipeEventMetadataEvent.
 */

EventPipeEventMetadataEvent *
ep_event_metdata_event_alloc (
	EventPipeEvent *ep_event,
	uint32_t proc_num,
	uint64_t thread_id,
	uint8_t *data,
	uint32_t data_len,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EventPipeEventMetadataEvent *instance = ep_rt_object_alloc (EventPipeEventMetadataEvent);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_event_instance_init (
		&instance->event_instance,
		ep_event,
		proc_num,
		thread_id,
		data,
		data_len,
		activity_id,
		related_activity_id) != NULL);

	instance->payload_buffer = data;
	instance->payload_buffer_len = data_len;

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_metdata_event_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_event_metdata_event_free (EventPipeEventMetadataEvent *metadata_event)
{
	ep_return_void_if_nok (metadata_event != NULL);

	ep_event_instance_fini (&metadata_event->event_instance);
	ep_rt_byte_array_free (metadata_event->payload_buffer);
	ep_rt_object_free (metadata_event);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_configuration_internals;
const char quiet_linker_empty_file_warning_eventpipe_configuration_internals = 0;
#endif
