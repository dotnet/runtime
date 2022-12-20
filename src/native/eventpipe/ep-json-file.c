#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_JSON_FILE_GETTER_SETTER
#include "ep-json-file.h"
#include "ep.h"
#include "ep-event-instance.h"
#include "ep-rt.h"

#ifdef HAVE_INTTYPES_H
#include <inttypes.h>
#else
#ifndef PRIu64
#define PRIu64 "llu"
#endif
#endif

#ifdef EP_CHECKED_BUILD

#define MAX_ASSEMBLY_NAME_LEN 256
#define MAX_METHOD_NAME_LEN 256
#define MAX_BUFFER_SIZE (MAX_ASSEMBLY_NAME_LEN + MAX_METHOD_NAME_LEN + 32)

/*
 * Forward declares of all static functions.
 */

/*
 * EventPipeJsonFile.
 */

static
inline
void
json_file_write_string (
	EventPipeJsonFile *json_file,
	const ep_char8_t *string)
{
	uint32_t bytes_written;
	ep_rt_file_write (json_file->file_stream, (const uint8_t *)string, (uint32_t)strlen (string), &bytes_written);
}

EventPipeJsonFile *
ep_json_file_alloc (const ep_char8_t *out_file_path)
{
	EventPipeJsonFile *instance = ep_rt_object_alloc (EventPipeJsonFile);
	ep_raise_error_if_nok (instance != NULL);

	instance->write_error_encountered = false;

	instance->file_stream = ep_rt_file_open_write (out_file_path);
	ep_raise_error_if_nok (instance->file_stream != NULL);

	instance->file_open_timestamp = ep_perf_timestamp_get ();

	json_file_write_string (instance, "{\n\"StackSource\" : {\n\"Samples\" : [\n");

ep_on_exit:
	return instance;

ep_on_error:
	ep_json_file_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_json_file_free (EventPipeJsonFile *json_file)
{
	ep_return_void_if_nok (json_file != NULL);

	if (json_file->file_stream != NULL) {
		if (!json_file->write_error_encountered)
			json_file_write_string (json_file, "]}}");
		ep_rt_file_close (json_file->file_stream);
		json_file->file_stream = NULL;
	}

	ep_rt_object_free (json_file);
}

void
ep_json_file_write_event (
	EventPipeJsonFile *json_file,
	EventPipeEventInstance *instance)
{
	ep_event_instance_serialize_to_json_file (instance, json_file);
}

void
ep_json_file_write_event_data (
	EventPipeJsonFile *json_file,
	ep_timestamp_t timestamp,
	ep_rt_thread_id_t thread_id,
	const ep_char8_t *message,
	EventPipeStackContentsInstance *stack_contents)
{
	ep_return_void_if_nok (json_file != NULL);
	ep_return_void_if_nok (json_file->file_stream != NULL && !json_file->write_error_encountered );

	// Convert the timestamp from a QPC value to a trace-relative timestamp.
	double millis_since_trace_start = 0.0;
	if (timestamp != json_file->file_open_timestamp) {
		ep_timestamp_t elapsed_nanos;
		elapsed_nanos = timestamp - json_file->file_open_timestamp;
		millis_since_trace_start = elapsed_nanos / 1000000.0;
	}

	ep_char8_t buffer [MAX_BUFFER_SIZE];
	int32_t characters_written = -1;

	characters_written = ep_rt_utf8_string_snprintf (buffer, ARRAY_SIZE (buffer), "{\"Time\" : \"%f\", \"Metric\" : \"1\",\n\"Stack\": [\n\"", millis_since_trace_start);
	if (characters_written > 0 && characters_written < (int32_t)ARRAY_SIZE (buffer))
		json_file_write_string (json_file, buffer);

	if (message)
		json_file_write_string (json_file, message);

	json_file_write_string (json_file, "\",\n");

	ep_char8_t assembly_name [MAX_ASSEMBLY_NAME_LEN];
	ep_char8_t method_name [MAX_METHOD_NAME_LEN];

	for (uint32_t i = 0; i < ep_stack_contents_instance_get_length (stack_contents); ++i) {
		ep_rt_method_desc_t *method = ep_stack_contents_instance_get_method (stack_contents, i);

		if (!ep_rt_method_get_simple_assembly_name (method, assembly_name, ARRAY_SIZE (assembly_name))) {
			assembly_name [0] = '?';
			assembly_name [1] = 0;
		}

		if (!ep_rt_method_get_full_name (method, method_name, ARRAY_SIZE (method_name))) {
			method_name [0] = '?';
			method_name [1] = 0;
		}

		characters_written = ep_rt_utf8_string_snprintf (buffer, ARRAY_SIZE (buffer), "\"%s!%s\",\n", assembly_name, method_name);
		if (characters_written > 0 && characters_written < (int32_t)ARRAY_SIZE (buffer))
			json_file_write_string (json_file, buffer);
	}

	characters_written = ep_rt_utf8_string_snprintf (buffer, ARRAY_SIZE (buffer), "\"Thread (%" PRIu64 ")\"]},", ep_rt_thread_id_t_to_uint64_t (thread_id));
	if (characters_written > 0 && characters_written < (int32_t)ARRAY_SIZE (buffer))
		json_file_write_string (json_file, buffer);
}

#endif /* EP_CHECKED_BUILD */
#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_json_file;
const char quiet_linker_empty_file_warning_eventpipe_json_file = 0;
#endif
