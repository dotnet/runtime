#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

/*
 * EventPipeEventSource.
 */

EventPipeEventSource *
ep_event_source_alloc (void)
{
	ep_char16_t *command_line_arg_utf16 = NULL;
	ep_char16_t *event_name_utf16  = NULL;
	uint8_t *metadata = NULL;

	EventPipeEventSource *instance = ep_rt_object_alloc (EventPipeEventSource);
	ep_raise_error_if_nok (instance != NULL);

	instance->provider = ep_create_provider (ep_provider_get_default_name_utf8 (), NULL, NULL);
	ep_raise_error_if_nok (instance->provider != NULL);

	// Generate metadata.
	EventPipeParameterDesc params [1];
	uint32_t params_len;
	params_len = EP_ARRAY_SIZE (params);

	command_line_arg_utf16 = ep_rt_utf8_to_utf16_string ("CommandLine", -1);
	ep_raise_error_if_nok (command_line_arg_utf16 != NULL);

	ep_parameter_desc_init (params, EP_PARAMETER_TYPE_STRING, command_line_arg_utf16);

	event_name_utf16 = ep_rt_utf8_to_utf16_string ("ProcessInfo", -1);
	ep_raise_error_if_nok (event_name_utf16 != NULL);

	size_t metadata_len;
	metadata_len = 0;
	metadata = ep_metadata_generator_generate_event_metadata (
		1,		/* eventID */
		event_name_utf16,
		0,		/* keywords */
		0,		/* version */
		EP_EVENT_LEVEL_LOG_ALWAYS,
		params,
		params_len,
		&metadata_len);

	ep_raise_error_if_nok (metadata != NULL);

	// Add the event.
	instance->process_info_event = ep_provider_add_event (
		instance->provider,
		1,		/* eventID */
		0,		/* keywords */
		0,		/* eventVersion */
		EP_EVENT_LEVEL_LOG_ALWAYS,
		false,  /* needStack */
		metadata,
		(uint32_t)metadata_len);

	ep_raise_error_if_nok (instance->process_info_event);

	// Delete the metadata after the event is created.
	// The metadata blob will be copied into EventPipe-owned memory.
	ep_rt_byte_array_free (metadata);

	// Delete the strings after the event is created.
	// The strings will be copied into EventPipe-owned memory.
	ep_rt_utf16_string_free (event_name_utf16);
	ep_rt_utf16_string_free (command_line_arg_utf16);

ep_on_exit:
	return instance;

ep_on_error:
	ep_rt_byte_array_free (metadata);
	ep_rt_utf16_string_free (event_name_utf16);
	ep_rt_utf16_string_free (command_line_arg_utf16);
	ep_event_source_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_event_source_free (EventPipeEventSource *event_source)
{
	ep_provider_free (event_source->provider);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_event_source_internals;
const char quiet_linker_empty_file_warning_eventpipe_event_source_internals = 0;
#endif
