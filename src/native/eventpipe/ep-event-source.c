#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_EVENT_SOURCE_GETTER_SETTER
#include "ep.h"
#include "ep-event-source.h"
#include "ep-event-payload.h"
#include "ep-metadata-generator.h"
#include "ep-session.h"
#include "ep-session-provider.h"
#include "ep-rt.h"

#if defined(HOST_WINDOWS) || defined(HOST_WIN32)
const ep_char8_t* _ep_os_info = "Windows";
#elif defined(HOST_IOS)
const ep_char8_t* _ep_os_info = "iOS";
#elif defined(HOST_WATCHOS)
const ep_char8_t* _ep_os_info = "WatchOS";
#elif defined(HOST_TVOS)
const ep_char8_t* _ep_os_info = "tvOS";
#elif defined(__APPLE__)
const ep_char8_t* _ep_os_info = "macOS";
#elif defined(HOST_ANDROID)
const ep_char8_t* _ep_os_info = "Android";
#elif defined(__linux__)
const ep_char8_t* _ep_os_info = "Linux";
#else
const ep_char8_t* _ep_os_info = "Unknown";
#endif

#if defined(TARGET_X86)
const ep_char8_t* _ep_arch_info = "x86";
#elif defined(TARGET_AMD64)
const ep_char8_t* _ep_arch_info = "x64";
#elif defined(TARGET_ARM)
const ep_char8_t* _ep_arch_info = "arm32";
#elif defined(TARGET_ARM64)
const ep_char8_t* _ep_arch_info = "arm64";
#elif defined(TARGET_S390X)
const ep_char8_t* _ep_arch_info = "s390x";
#else
const ep_char8_t* _ep_arch_info = "Unknown";
#endif

EventPipeEventSource _ep_event_source_instance = { 0 };

/*
 * Forward declares of all static functions.
 */

static
void
event_source_fini (EventPipeEventSource *event_source);

/*
 * EventPipeEventSource.
 */

static
void
event_source_fini (EventPipeEventSource *event_source)
{
	ep_delete_provider (event_source->provider);
}

EventPipeEventSource *
ep_event_source_alloc (void)
{
	EventPipeEventSource *instance = ep_rt_object_alloc (EventPipeEventSource);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_event_source_init (instance) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_source_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeEventSource *
ep_event_source_init (EventPipeEventSource *event_source)
{
	ep_char16_t *command_line_arg_utf16 = NULL;
	ep_char16_t *os_info_arg_utf16 = NULL;
	ep_char16_t *arch_info_arg_utf16 = NULL;
	ep_char16_t *event_name_utf16  = NULL;
	uint8_t *metadata = NULL;

	EP_ASSERT (event_source != NULL);

	event_source->provider = ep_create_provider (ep_provider_get_default_name_utf8 (), NULL, NULL, NULL);
	ep_raise_error_if_nok (event_source->provider != NULL);

	event_source->provider_name = ep_provider_get_default_name_utf8 ();

	// Generate metadata.
	EventPipeParameterDesc params [3];
	uint32_t params_len;
	params_len = (uint32_t)EP_ARRAY_SIZE (params);

	command_line_arg_utf16 = ep_rt_utf8_to_utf16_string ("CommandLine", -1);
	ep_raise_error_if_nok (command_line_arg_utf16 != NULL);
	ep_parameter_desc_init (&params[0], EP_PARAMETER_TYPE_STRING, command_line_arg_utf16);

	os_info_arg_utf16 = ep_rt_utf8_to_utf16_string ("OSInformation", -1);
	ep_raise_error_if_nok (os_info_arg_utf16 != NULL);
	ep_parameter_desc_init (&params[1], EP_PARAMETER_TYPE_STRING, os_info_arg_utf16);

	arch_info_arg_utf16 = ep_rt_utf8_to_utf16_string ("ArchInformation", -1);
	ep_raise_error_if_nok (arch_info_arg_utf16 != NULL);
	ep_parameter_desc_init (&params[2], EP_PARAMETER_TYPE_STRING, arch_info_arg_utf16);

	event_name_utf16 = ep_rt_utf8_to_utf16_string ("ProcessInfo", -1);
	ep_raise_error_if_nok (event_name_utf16 != NULL);

	size_t metadata_len;
	metadata_len = 0;
	metadata = ep_metadata_generator_generate_event_metadata (
		1,		/* eventID */
		event_name_utf16,
		0,		/* keywords */
		1,		/* version */
		EP_EVENT_LEVEL_LOGALWAYS,
		0,		/* opcode */
		params,
		params_len,
		&metadata_len);

	ep_raise_error_if_nok (metadata != NULL);

	// Add the event.
	event_source->process_info_event = ep_provider_add_event (
		event_source->provider,
		1,		/* eventID */
		0,		/* keywords */
		0,		/* eventVersion */
		EP_EVENT_LEVEL_LOGALWAYS,
		false,  /* needStack */
		metadata,
		(uint32_t)metadata_len);

	ep_raise_error_if_nok (event_source->process_info_event);

ep_on_exit:
	// Delete the metadata after the event is created.
	// The metadata blob will be copied into EventPipe-owned memory.
	ep_rt_byte_array_free (metadata);

	// Delete the strings after the event is created.
	// The strings will be copied into EventPipe-owned memory.
	ep_rt_utf16_string_free (event_name_utf16);
	ep_rt_utf16_string_free (arch_info_arg_utf16);
	ep_rt_utf16_string_free (os_info_arg_utf16);
	ep_rt_utf16_string_free (command_line_arg_utf16);

	return event_source;

ep_on_error:
	ep_event_source_free (event_source);

	event_source = NULL;
	ep_exit_error_handler ();
}

void
ep_event_source_fini (EventPipeEventSource *event_source)
{
	ep_return_void_if_nok (event_source);
	event_source_fini (event_source);
}

void
ep_event_source_free (EventPipeEventSource *event_source)
{
	ep_return_void_if_nok (event_source);
	event_source_fini (event_source);
	ep_rt_object_free (event_source);
}

bool
ep_event_source_enable (
	EventPipeEventSource *event_source,
	EventPipeSession *session)
{
	EP_ASSERT (event_source != NULL);
	EP_ASSERT (session != NULL);

	ep_requires_lock_held ();

	bool result = true;
	EventPipeSessionProvider *session_provider = ep_session_provider_alloc (event_source->provider_name, (uint64_t)-1, EP_EVENT_LEVEL_LOGALWAYS, NULL);
	if (session_provider != NULL)
		result = ep_session_add_session_provider (session, session_provider);
	return result;
}

void
ep_event_source_send_process_info (
	EventPipeEventSource * event_source,
	const ep_char8_t *command_line)
{
	EP_ASSERT (event_source != NULL);

	ep_char16_t *command_line_utf16 = NULL;
	ep_char16_t *os_info_utf16 = NULL;
	ep_char16_t *arch_info_utf16 = NULL;

	command_line_utf16 = ep_rt_utf8_to_utf16_string (command_line, -1);
	os_info_utf16 = ep_rt_utf8_to_utf16_string (ep_event_source_get_os_info (), -1);
	arch_info_utf16 = ep_rt_utf8_to_utf16_string (ep_event_source_get_arch_info (), -1);

	EventData data [3] = { { 0 } };
	if (command_line_utf16)
		ep_event_data_init (&data[0], (uint64_t)command_line_utf16, (uint32_t)((ep_rt_utf16_string_len (command_line_utf16) + 1) * sizeof (ep_char16_t)), 0);
	if (os_info_utf16)
		ep_event_data_init (&data[1], (uint64_t)os_info_utf16, (uint32_t)((ep_rt_utf16_string_len (os_info_utf16) + 1) * sizeof (ep_char16_t)), 0);
	if (arch_info_utf16)
		ep_event_data_init (&data[2], (uint64_t)arch_info_utf16, (uint32_t)((ep_rt_utf16_string_len (arch_info_utf16) + 1) * sizeof (ep_char16_t)), 0);

	ep_write_event_2 (event_source->process_info_event, data, (uint32_t)EP_ARRAY_SIZE (data), NULL, NULL);

	ep_rt_utf16_string_free (arch_info_utf16);
	ep_rt_utf16_string_free (os_info_utf16);
	ep_rt_utf16_string_free (command_line_utf16);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_event_source;
const char quiet_linker_empty_file_warning_eventpipe_event_source = 0;
#endif
