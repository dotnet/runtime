#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * EventPipeEventSource.
 */

static EventPipeEventSource *event_source_instance;

void
ep_event_source_enable (
	EventPipeEventSource *event_source,
	EventPipeSession *session)
{
	ep_return_void_if_nok (event_source != NULL);
	ep_return_void_if_nok (session != NULL);

	EventPipeSessionProvider *session_provider = ep_session_provider_alloc (ep_event_source_get_provider_name (event_source), (uint64_t)-1, EP_EVENT_LEVEL_LOG_ALWAYS, NULL);
	if (session_provider != NULL)
		ep_session_add_session_provider (session, session_provider);
}

void
ep_event_source_send_process_info (
	EventPipeEventSource * event_source,
	const ep_char16_t *command_line)
{
	ep_return_void_if_nok (event_source != NULL);

	EventData data [1];
	ep_event_data_init (data, (uint64_t)command_line, (uint32_t)((ep_rt_utf16_string_len (command_line) + 1) * sizeof (ep_char16_t)), 0);
	ep_write_event (ep_event_source_get_process_info_event (event_source), data, EP_ARRAY_SIZE (data), NULL, NULL);
}

EventPipeEventSource *
ep_event_source_get (void)
{
	return event_source_instance;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_event_source;
const char quiet_linker_empty_file_warning_eventpipe_event_source = 0;
#endif
