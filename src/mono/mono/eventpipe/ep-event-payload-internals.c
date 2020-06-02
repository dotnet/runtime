#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

/*
 * EventData.
 */

EventData *
ep_event_data_alloc (
	uint64_t ptr,
	uint32_t size,
	uint32_t reserved)
{
	EventData *instance = ep_rt_object_alloc (EventData);
	ep_raise_error_if_nok (ep_event_data_init (instance, ptr, size,reserved));

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_data_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

EventData *
ep_event_data_init (
	EventData *event_data,
	uint64_t ptr,
	uint32_t size,
	uint32_t reserved)
{
	EP_ASSERT (event_data != NULL);

	event_data->ptr = ptr;
	event_data->size = size;
	event_data->reserved = reserved;

	return event_data;
}

void
ep_event_data_fini (EventData *event_data)
{
	;
}

void
ep_event_data_free (EventData *event_data)
{
	ep_return_void_if_nok (event_data != NULL);

	ep_event_data_fini (event_data);
	ep_rt_object_free (event_data);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_event_payload_internals;
const char quiet_linker_empty_file_warning_eventpipe_event_payload_internals = 0;
#endif
