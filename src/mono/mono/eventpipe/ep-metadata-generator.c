#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * EventPipeMetadataGenerator.
 */

uint8_t *
ep_metadata_generator_generate_event_metadata (
	uint32_t event_id,
	const ep_char16_t *event_name,
	uint64_t keywords,
	uint32_t version,
	EventPipeEventLevel level,
	EventPipeParameterDesc *params,
	uint32_t params_len,
	size_t *metadata_len)
{
	ep_return_null_if_nok (event_name != NULL);
	ep_return_null_if_nok (params_len == 0 || params != NULL);
	ep_return_null_if_nok (metadata_len != NULL);

	uint8_t *buffer = NULL;
	uint8_t * current = NULL;

	// The order of fields is defined in coreclr\src\mscorlib\shared\System\Diagnostics\Tracing\EventSource.cs DefineEventPipeEvents method
	// eventID			: 4 bytes
	// eventName		: (eventName.Length + 1) * 2 bytes
	// keywords			: 8 bytes
	// eventVersion		: 4 bytes
	// level			: 4 bytes
	// parameterCount	: 4 bytes
	size_t event_name_len = ep_rt_utf16_string_len (event_name);
	*metadata_len = 24 + ((event_name_len + 1) * sizeof (ep_char16_t));

	// Each parameter has a 4 byte TypeCode + (parameterName.Length + 1) * 2 bytes.
	for (uint32_t i = 0; i < params_len; ++i) {
		EP_ASSERT (ep_parameter_desc_get_name (&params [i]) != NULL);
		*metadata_len += (4 + ((ep_rt_utf16_string_len (ep_parameter_desc_get_name (&params [i])) + 1) * sizeof (ep_char16_t)));
	}

	// Allocate a metadata blob.
	buffer = ep_rt_byte_array_alloc (*metadata_len);
	ep_raise_error_if_nok (buffer != NULL);

	current = buffer;

	// Write the event ID.
	memcpy (current, &event_id, sizeof (event_id));
	current += sizeof (event_id);

	// Write the event name.
	memcpy (current, event_name, (event_name_len + 1) * sizeof (ep_char16_t));
	current += (event_name_len + 1) * sizeof (ep_char16_t);

	// Write the keywords.
	memcpy (current, &keywords, sizeof (keywords));
	current += sizeof (keywords);

	// Write the version.
	memcpy (current, &version, sizeof (version));
	current += sizeof (version);

	// Write the level.
	memcpy (current, &level, sizeof (level));
	current += sizeof (level);

	// Write the parameter count.
	memcpy(current, &params_len, sizeof (params_len));
	current += sizeof (params_len);

	// Write the parameter descriptions.
	for (uint32_t i = 0; i < params_len; ++i) {
		EventPipeParameterType const param_type = ep_parameter_desc_get_type (&params [i]);
		const ep_char16_t *const param_name = ep_parameter_desc_get_name (&params [i]);
		size_t const param_name_len = ep_rt_utf16_string_len (param_name);

		memcpy (current, &param_type, sizeof (param_type));
		current += sizeof (param_type);

		memcpy (current, param_name, (param_name_len + 1) * sizeof (ep_char16_t));
		current += (param_name_len + 1) * sizeof (ep_char16_t);
	}

	EP_ASSERT (*metadata_len == (size_t)(current - buffer));

ep_on_exit:
	return buffer;

ep_on_error:
	ep_rt_byte_array_free (buffer);
	*metadata_len = 0;

	buffer = NULL;
	ep_exit_error_handler ();
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_metadata_generator;
const char quiet_linker_empty_file_warning_eventpipe_metadata_generator = 0;
#endif
