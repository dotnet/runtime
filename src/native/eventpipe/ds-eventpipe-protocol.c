#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_EVENTPIPE_PROTOCOL_GETTER_SETTER
#include "ds-protocol.h"
#include "ds-eventpipe-protocol.h"
#include "ep.h"
#include "ds-rt.h"

/*
 * Forward declares of all static functions.
 */

static
bool
eventpipe_protocol_helper_send_stop_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id);

static
bool
eventpipe_protocol_helper_send_start_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id);

static
bool
eventpipe_collect_tracing_command_try_parse_serialization_format (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeSerializationFormat *format);

static
bool
eventpipe_collect_tracing_command_try_parse_circular_buffer_size (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t *circular_buffer);

static
bool
eventpipe_collect_tracing_command_try_parse_rundown_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *rundown_requested);

static
bool
eventpipe_collect_tracing_command_try_parse_rundown_keyword (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *rundown_keyword);

static
bool
eventpipe_collect_tracing_command_try_parse_stackwalk_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *stackwalk_requested);

static
bool
eventpipe_collect_tracing_command_try_parse_event_filter (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeEventFilter *event_filter);

static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	ProviderTracepointConfiguration *tracepoint_config);

static
bool
eventpipe_collect_tracing_command_try_parse_one_provider_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *keywords,
	EventPipeEventLevel *logging_level,
	ep_char8_t **provider_name,
	ep_char8_t **filter_data,
	EventPipeEventFilter *event_filter,
	ProviderTracepointConfiguration *tracepoint_config);

static
bool
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	dn_vector_t **result);

static
bool
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	dn_vector_t **result,
	uint32_t output_format);

static
uint8_t *
eventpipe_collect_tracing_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing2_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing3_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing4_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
uint8_t *
eventpipe_collect_tracing5_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len);

static
bool
eventpipe_protocol_helper_stop_tracing (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

static
bool
eventpipe_protocol_helper_collect_tracing (
	EventPipeCollectTracingCommandPayload *payload,
	DiagnosticsIpcStream *stream);

static
bool
eventpipe_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

/*
* EventPipeCollectTracingCommandPayload
*/

static
inline
bool
eventpipe_collect_tracing_command_try_parse_serialization_format (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeSerializationFormat *format)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (format != NULL);

	uint32_t serialization_format;
	bool can_parse = ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &serialization_format);

	*format = (EventPipeSerializationFormat)serialization_format;
	return can_parse && (0 <= (int32_t)serialization_format) && ((int32_t)serialization_format < (int32_t)EP_SERIALIZATION_FORMAT_COUNT);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_circular_buffer_size (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint32_t *circular_buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (circular_buffer != NULL);

	bool can_parse = ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, circular_buffer);
	return can_parse && (*circular_buffer > 0);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_rundown_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *rundown_requested)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (rundown_requested != NULL);

	return ds_ipc_message_try_parse_bool (buffer, buffer_len, rundown_requested);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_rundown_keyword (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *rundown_keyword)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (rundown_keyword != NULL);

	return ds_ipc_message_try_parse_uint64_t (buffer, buffer_len, rundown_keyword);
}

static
inline
bool
eventpipe_collect_tracing_command_try_parse_stackwalk_requested (
	uint8_t **buffer,
	uint32_t *buffer_len,
	bool *stackwalk_requested)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (stackwalk_requested != NULL);

	return ds_ipc_message_try_parse_bool (buffer, buffer_len, stackwalk_requested);
}

static
bool
eventpipe_collect_tracing_command_try_parse_event_filter (
	uint8_t **buffer,
	uint32_t *buffer_len,
	EventPipeEventFilter *event_filter)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (event_filter != NULL);

	bool result = false;
	uint32_t event_id_array_len = 0;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_bool (buffer, buffer_len, &event_filter->allow));

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id_array_len));
	if (event_id_array_len > 0) {
		dn_vector_custom_alloc_params_t event_id_array_params = {0, };
		event_id_array_params.capacity = event_id_array_len;
		event_filter->event_ids = dn_vector_custom_alloc_t (&event_id_array_params, uint32_t);
		ep_raise_error_if_nok (event_filter->event_ids != NULL);

		for (uint32_t i = 0; i < event_id_array_len; ++i) {
			uint32_t event_id = 0;
			ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id));
			ep_raise_error_if_nok (dn_vector_push_back (event_filter->event_ids, event_id));
		}
	} else {
		event_filter->event_ids = NULL;
	}

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	if (event_filter->event_ids != NULL) {
		dn_vector_free (event_filter->event_ids);
		event_filter->event_ids = NULL;
	}

	ep_exit_error_handler ();
}

static
bool
eventpipe_collect_tracing_command_try_parse_tracepoint_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	ProviderTracepointConfiguration *tracepoint_config)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (tracepoint_config != NULL);

	bool result = false;

	uint8_t *default_tracepoint_name_byte_array = NULL;
	uint32_t default_tracepoint_name_byte_array_len = 0;

	uint8_t *tracepoint_set_name_byte_array = NULL;
	uint32_t tracepoint_set_name_byte_array_len = 0;

	uint32_t tracepoint_set_array_len = 0;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &default_tracepoint_name_byte_array, &default_tracepoint_name_byte_array_len));

	if (default_tracepoint_name_byte_array) {
		tracepoint_config->default_tracepoint_name = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)default_tracepoint_name_byte_array);
		ep_rt_byte_array_free (default_tracepoint_name_byte_array);
		default_tracepoint_name_byte_array = NULL;
	} else {
		tracepoint_config->default_tracepoint_name = NULL;
	}

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &tracepoint_set_array_len));

	if (tracepoint_set_array_len > 0) {
		dn_vector_custom_alloc_params_t tracepoint_set_array_params = {0, };
		tracepoint_set_array_params.capacity = tracepoint_set_array_len;
		tracepoint_config->tracepoints = dn_vector_custom_alloc_t (&tracepoint_set_array_params, ProviderTracepointSet);
		ep_raise_error_if_nok (tracepoint_config->tracepoints != NULL);

		for (uint32_t i = 0; i < tracepoint_set_array_len; ++i) {
			ProviderTracepointSet *tracepoint_set = ep_rt_object_alloc (ProviderTracepointSet);
			ep_raise_error_if_nok (tracepoint_set != NULL);

			ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &tracepoint_set_name_byte_array, &tracepoint_set_name_byte_array_len));

			tracepoint_set->tracepoint_name = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)tracepoint_set_name_byte_array);
			ep_raise_error_if_nok (tracepoint_set->tracepoint_name != NULL);
			ep_rt_byte_array_free (tracepoint_set_name_byte_array);
			tracepoint_set_name_byte_array = NULL;

			uint32_t event_id_array_len = 0;
			ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id_array_len));

			if (event_id_array_len > 0) {
				dn_vector_custom_alloc_params_t event_id_array_params = {0, };
				event_id_array_params.capacity = event_id_array_len;
				tracepoint_set->event_ids = dn_vector_custom_alloc_t (&event_id_array_params, uint32_t);
				ep_raise_error_if_nok (tracepoint_set->event_ids != NULL);

				for (uint32_t j = 0; j < event_id_array_len; ++j) {
					uint32_t event_id = 0;
					ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &event_id));
					ep_raise_error_if_nok (dn_vector_push_back (tracepoint_set->event_ids, event_id));
				}
			}

			ep_raise_error_if_nok (dn_vector_push_back (tracepoint_config->tracepoints, tracepoint_set));
		}
	} else {
		tracepoint_config->tracepoints = NULL;
	}

	ep_raise_error_if_nok (tracepoint_config->default_tracepoint_name != NULL || tracepoint_config->tracepoints != NULL);

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ep_rt_byte_array_free (default_tracepoint_name_byte_array);
	ep_rt_byte_array_free (tracepoint_set_name_byte_array);
	if (tracepoint_config->tracepoints != NULL) {
		// Free the tracepoint set names
		for (uint32_t i = 0; i < tracepoint_config->tracepoints->size; ++i) {
			ProviderTracepointSet *tracepoint_set = *dn_vector_index_t (tracepoint_config->tracepoints, ProviderTracepointSet *, i);
			if (tracepoint_set != NULL) {
				dn_vector_free (tracepoint_set->event_ids);
				ep_rt_object_free (tracepoint_set);
				ep_rt_utf8_string_free ((ep_char8_t *)tracepoint_set->tracepoint_name);
				tracepoint_set->tracepoint_name = NULL;
			}
		}
		dn_vector_free (tracepoint_config->tracepoints);
		tracepoint_config->tracepoints = NULL;
	}
	ep_rt_utf8_string_free ((ep_char8_t *)tracepoint_config->default_tracepoint_name);
	tracepoint_config->default_tracepoint_name = NULL;

	ep_exit_error_handler ();
}

/*
 * eventpipe_collect_tracing_command_try_parse_one_provider_config
 *
 * The Provider Configuration format varies depending on CollectTracing version
 *
 * keywords: Always deserialized
 * log_level: Always deserialized
 * provider_name: Always deserialized, cannot be 0 length
 * filter_data: Always deserialized, may be 0 length
 * event_filter: Opt-in deserialization
 * tracepoint_config: Opt-in deserialization
 */
static
bool
eventpipe_collect_tracing_command_try_parse_one_provider_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	uint64_t *keywords,
	EventPipeEventLevel *logging_level,
	ep_char8_t **provider_name,
	ep_char8_t **filter_data,
	EventPipeEventFilter *event_filter,
	ProviderTracepointConfiguration *tracepoint_config)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (provider_name != NULL);
	EP_ASSERT (keywords != NULL);
	EP_ASSERT (logging_level != NULL);

	bool result = false;

	uint8_t *provider_name_byte_array = NULL;
	uint8_t *filter_data_byte_array = NULL;

	uint32_t provider_name_byte_array_len = 0;
	uint32_t filter_data_byte_array_len = 0;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (buffer, buffer_len, keywords));

	uint32_t log_level;
	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &log_level));
	*logging_level = (EventPipeEventLevel)log_level;
	ep_raise_error_if_nok (*logging_level <= EP_EVENT_LEVEL_VERBOSE);

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &provider_name_byte_array, &provider_name_byte_array_len));
	*provider_name = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)provider_name_byte_array);
	ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty (*provider_name));
	ep_rt_byte_array_free (provider_name_byte_array);
	provider_name_byte_array = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &filter_data_byte_array, &filter_data_byte_array_len));
	if (filter_data_byte_array) {
		*filter_data = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)filter_data_byte_array);
		ep_raise_error_if_nok (*filter_data != NULL);
		ep_rt_byte_array_free (filter_data_byte_array);
		filter_data_byte_array = NULL;
	}

	if (event_filter != NULL) {
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_event_filter (buffer, buffer_len, event_filter));
	}

	if (tracepoint_config != NULL) {
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_tracepoint_config (buffer, buffer_len, tracepoint_config));
	}

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ep_rt_byte_array_free (provider_name_byte_array);
	ep_rt_utf8_string_free (*provider_name);
	ep_rt_byte_array_free (filter_data_byte_array);
	ep_rt_utf8_string_free (*filter_data);
	ep_exit_error_handler ();
}

static
bool
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	dn_vector_t **result)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (result != NULL);

	// Picking an arbitrary upper bound,
	// This should be larger than any reasonable client request.
	// TODO: This might be too large.
	const uint32_t max_count_configs = 1000;
	uint32_t count_configs = 0;

	uint8_t *provider_name_byte_array = NULL;
	uint8_t *filter_data_byte_array = NULL;

	ep_char8_t *provider_name_utf8 = NULL;
	ep_char8_t *filter_data_utf8 = NULL;

	dn_vector_custom_alloc_params_t params = {0, };

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &count_configs));
	ep_raise_error_if_nok (count_configs <= max_count_configs);

	params.capacity = count_configs;

	*result = dn_vector_custom_alloc_t (&params, EventPipeProviderConfiguration);
	ep_raise_error_if_nok (*result);

	for (uint32_t i = 0; i < count_configs; ++i) {
		uint64_t keywords = 0;
		ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (buffer, buffer_len, &keywords));

		uint32_t log_level = 0;
		ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &log_level));
		ep_raise_error_if_nok (log_level <= EP_EVENT_LEVEL_VERBOSE);

		uint32_t provider_name_byte_array_len = 0;
		ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &provider_name_byte_array, &provider_name_byte_array_len));

		provider_name_utf8 = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)provider_name_byte_array);
		ep_raise_error_if_nok (provider_name_utf8 != NULL);

		ep_raise_error_if_nok (!ep_rt_utf8_string_is_null_or_empty (provider_name_utf8));

		ep_rt_byte_array_free (provider_name_byte_array);
		provider_name_byte_array = NULL;

		uint32_t filter_data_byte_array_len = 0;
		ep_raise_error_if_nok (ds_ipc_message_try_parse_string_utf16_t_byte_array_alloc (buffer, buffer_len, &filter_data_byte_array, &filter_data_byte_array_len));

		// This parameter is optional.
		if (filter_data_byte_array) {
			filter_data_utf8 = ep_rt_utf16le_to_utf8_string ((const ep_char16_t *)filter_data_byte_array);
			ep_raise_error_if_nok (filter_data_utf8 != NULL);

			ep_rt_byte_array_free (filter_data_byte_array);
			filter_data_byte_array = NULL;
		}

		EventPipeProviderConfiguration provider_config;
		if (ep_provider_config_init (&provider_config, provider_name_utf8, keywords, (EventPipeEventLevel)log_level, filter_data_utf8)) {
			if (dn_vector_push_back (*result, provider_config)) {
				// Ownership transferred.
				provider_name_utf8 = NULL;
				filter_data_utf8 = NULL;
			}
			ep_provider_config_fini (&provider_config);
		}
		ep_raise_error_if_nok (provider_name_utf8 == NULL && filter_data_utf8 == NULL);
	}

ep_on_exit:
	return (count_configs > 0);

ep_on_error:
	count_configs = 0;
	ep_rt_byte_array_free (provider_name_byte_array);
	ep_rt_utf8_string_free (provider_name_utf8);
	ep_rt_byte_array_free (filter_data_byte_array);
	ep_rt_utf8_string_free (filter_data_utf8);
	ep_exit_error_handler ();
}

static
bool
eventpipe_collect_tracing_command_try_parse_config (
	uint8_t **buffer,
	uint32_t *buffer_len,
	dn_vector_t **result,
	uint32_t output_format)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len != NULL);
	EP_ASSERT (result != NULL);

	// Picking an arbitrary upper bound,
	// This should be larger than any reasonable client request.
	// TODO: This might be too large.
	const uint32_t max_count_configs = 1000;
	uint32_t count_configs = 0;

	ep_char8_t *provider_name_utf8 = NULL;
	ep_char8_t *filter_data_utf8 = NULL;

	dn_vector_custom_alloc_params_t params = {0, };

	EventPipeEventFilter *event_filter = NULL;
	ProviderTracepointConfiguration *tracepoint_config = NULL;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (buffer, buffer_len, &count_configs));
	ep_raise_error_if_nok (count_configs <= max_count_configs);

	params.capacity = count_configs;

	*result = dn_vector_custom_alloc_t (&params, EventPipeProviderConfiguration);
	ep_raise_error_if_nok (*result);

	for (uint32_t i = 0; i < count_configs; ++i) {
		uint64_t keywords = 0;
		EventPipeEventLevel logging_level;
		event_filter = ep_rt_object_alloc (EventPipeEventFilter);
		ep_raise_error_if_nok (event_filter != NULL);

		tracepoint_config = ep_rt_object_alloc (ProviderTracepointConfiguration);
		ep_raise_error_if_nok (tracepoint_config != NULL);
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_one_provider_config (
			buffer,
			buffer_len,
			&keywords,
			&logging_level,
			&provider_name_utf8,
			&filter_data_utf8,
			event_filter,
			output_format == 1 ? tracepoint_config : NULL));

		EventPipeProviderConfiguration provider_config;
		if (ep_provider_config_init (&provider_config, provider_name_utf8, keywords, logging_level, filter_data_utf8, event_filter, tracepoint_config)) {
			if (dn_vector_push_back (*result, provider_config)) {
				// Ownership transferred.
				provider_name_utf8 = NULL;
				filter_data_utf8 = NULL;
				event_filter = NULL;
				tracepoint_config = NULL;
			}
			ep_provider_config_fini (&provider_config);
		}
		ep_raise_error_if_nok (provider_name_utf8 == NULL && filter_data_utf8 == NULL);
	}

ep_on_exit:
	return (count_configs > 0);

ep_on_error:
	count_configs = 0;
	ep_rt_utf8_string_free (provider_name_utf8);
	ep_rt_utf8_string_free (filter_data_utf8);
	ep_rt_object_free (tracepoint_config);
	ep_rt_object_free (event_filter);
	dn_vector_free (*result);
	*result = NULL;
	ep_exit_error_handler ();
}

EventPipeCollectTracingCommandPayload *
ds_eventpipe_collect_tracing_command_payload_alloc (void)
{
	return ep_rt_object_alloc (EventPipeCollectTracingCommandPayload);
}

void
ds_eventpipe_collect_tracing_command_payload_free (EventPipeCollectTracingCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free (payload->incoming_buffer);

	DN_VECTOR_FOREACH_BEGIN (EventPipeProviderConfiguration, config, payload->provider_configs) {
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_provider_name (&config));
		ep_rt_utf8_string_free ((ep_char8_t *)ep_provider_config_get_filter_data (&config));
	} DN_VECTOR_FOREACH_END;

	ep_rt_object_free (payload);
}

/*
* EventPipeCollectTracingCommandPayload
*/

static
uint8_t *
eventpipe_collect_tracing_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload * instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, &instance->provider_configs))
		ep_raise_error ();
	instance->rundown_requested = true;
	instance->stackwalk_requested = true;
	instance->rundown_keyword = ep_default_rundown_keyword;
	instance->output_format = 0;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
uint8_t *
eventpipe_collect_tracing2_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_rundown_requested (&buffer_cursor, &buffer_cursor_len, &instance->rundown_requested) ||
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_keyword = instance->rundown_requested ? ep_default_rundown_keyword : 0;

	instance->stackwalk_requested = true;
	instance->output_format = 0;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
uint8_t *
eventpipe_collect_tracing3_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_rundown_requested (&buffer_cursor, &buffer_cursor_len, &instance->rundown_requested) ||
		!eventpipe_collect_tracing_command_try_parse_stackwalk_requested (&buffer_cursor, &buffer_cursor_len, &instance->stackwalk_requested) ||
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_keyword = instance->rundown_requested ? ep_default_rundown_keyword : 0;
	instance->output_format = 0;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
uint8_t *
eventpipe_collect_tracing4_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	if (!eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb ) ||
		!eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format) ||
		!eventpipe_collect_tracing_command_try_parse_rundown_keyword (&buffer_cursor, &buffer_cursor_len, &instance->rundown_keyword) ||
		!eventpipe_collect_tracing_command_try_parse_stackwalk_requested (&buffer_cursor, &buffer_cursor_len, &instance->stackwalk_requested) ||
		!eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, &instance->provider_configs))
		ep_raise_error ();

	instance->rundown_requested = instance->rundown_keyword != 0;
	instance->output_format = 0;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

static
uint8_t *
eventpipe_collect_tracing5_command_try_parse_payload (
	uint8_t *buffer,
	uint16_t buffer_len)
{
	EP_ASSERT (buffer != NULL);

	uint8_t * buffer_cursor = buffer;
	uint32_t buffer_cursor_len = buffer_len;

	EventPipeCollectTracingCommandPayload *instance = ds_eventpipe_collect_tracing_command_payload_alloc ();
	ep_raise_error_if_nok (instance != NULL);

	instance->incoming_buffer = buffer;

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint32_t (&buffer_cursor, &buffer_cursor_len, &instance->output_format));

	if (instance->output_format > 1) {
		// Output format not supported
		ep_raise_error ();
	}

	if (instance->output_format == 0) {
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_circular_buffer_size (&buffer_cursor, &buffer_cursor_len, &instance->circular_buffer_size_in_mb));
		ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_serialization_format (&buffer_cursor, &buffer_cursor_len, &instance->serialization_format));
	} else if (instance->output_format == 1) {
		instance->circular_buffer_size_in_mb = 0;
		instance->serialization_format = EP_SERIALIZATION_FORMAT_NETTRACE_V4; // Need to update to V6
	}

	ep_raise_error_if_nok (ds_ipc_message_try_parse_uint64_t (&buffer_cursor, &buffer_cursor_len, &instance->rundown_keyword));

	if (instance->output_format == 0) {
		ep_raise_error_if_nok (ds_ipc_message_try_parse_bool (&buffer_cursor, &buffer_cursor_len, &instance->stackwalk_requested));
	} else if (instance->output_format == 1) {
		instance->stackwalk_requested = false;
	}

	ep_raise_error_if_nok (eventpipe_collect_tracing_command_try_parse_config (&buffer_cursor, &buffer_cursor_len, &instance->provider_configs, instance->output_format));

	instance->rundown_requested = instance->rundown_keyword != 0;

ep_on_exit:
	return (uint8_t *)instance;

ep_on_error:
	ds_eventpipe_collect_tracing_command_payload_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

/*
* EventPipeProtocolHelper
*/

static
bool
eventpipe_protocol_helper_send_stop_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id)
{
	EP_ASSERT (stream != NULL);

	bool result = false;
	DiagnosticsIpcMessage success_message;
	if (ds_ipc_message_init (&success_message)) {
		result = ds_ipc_message_initialize_header_uint64_t_payload (&success_message, ds_ipc_header_get_generic_success (), (uint64_t)session_id);
		if (result)
			result = ds_ipc_message_send (&success_message, stream);
		ds_ipc_message_fini (&success_message);
	}
	return result;
}

static
bool
eventpipe_protocol_helper_send_start_tracing_success (
	DiagnosticsIpcStream *stream,
	EventPipeSessionID session_id)
{
	EP_ASSERT (stream != NULL);

	bool result = false;
	DiagnosticsIpcMessage success_message;
	if (ds_ipc_message_init (&success_message)) {
		result = ds_ipc_message_initialize_header_uint64_t_payload (&success_message, ds_ipc_header_get_generic_success (), (uint64_t)session_id);
		if (result)
			result = ds_ipc_message_send (&success_message, stream);
		ds_ipc_message_fini (&success_message);
	}
	return result;
}

static
bool
eventpipe_protocol_helper_stop_tracing (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (message != NULL && stream != NULL);

	bool result = false;
	EventPipeStopTracingCommandPayload *payload;
	payload = (EventPipeStopTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, NULL);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		ep_raise_error ();
	}

	ep_disable (payload->session_id);

	eventpipe_protocol_helper_send_stop_tracing_success (stream, payload->session_id);
	ds_ipc_stream_flush (stream);

	result = true;

ep_on_exit:
	ds_eventpipe_stop_tracing_command_payload_free (payload);
	ds_ipc_stream_free (stream);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

static
bool
eventpipe_protocol_helper_collect_tracing (
	EventPipeCollectTracingCommandPayload *payload,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (stream != NULL);

	if (!payload) {
		ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
		return false;
	}

	uint32_t user_events_data_fd = -1;
	if (payload->output_format == 1) {
		// Extract file descriptor from IpcStream
	}

	EventPipeSessionOptions options;
	ep_session_options_init(
		&options,
		NULL,
		payload->circular_buffer_size_in_mb,
		dn_vector_data_t (payload->provider_configs, EventPipeProviderConfiguration),
		dn_vector_size (payload->provider_configs),
		EP_SESSION_TYPE_IPCSTREAM,
		payload->serialization_format,
		payload->rundown_keyword,
		payload->stackwalk_requested,
		ds_ipc_stream_get_stream_ref (stream),
		NULL,
		NULL);

	EventPipeSessionID session_id = 0;
	bool result = false;
	session_id = ep_enable_3(&options);

	if (session_id == 0) {
		ds_ipc_message_send_error (stream, DS_IPC_E_FAIL);
		ep_raise_error ();
	} else {
		eventpipe_protocol_helper_send_start_tracing_success (stream, session_id);
		ep_start_streaming (session_id);
	}

	result = true;

ep_on_exit:
	ep_session_options_fini(&options);
	ds_eventpipe_collect_tracing_command_payload_free (payload);
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ds_ipc_stream_free (stream);
	ep_exit_error_handler ();
}

static
bool
eventpipe_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)", ds_ipc_header_get_commandset (ds_ipc_message_get_header_cref (message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	ds_ipc_stream_free (stream);
	return true;
}

void
ds_eventpipe_stop_tracing_command_payload_free (EventPipeStopTracingCommandPayload *payload)
{
	ep_return_void_if_nok (payload != NULL);
	ep_rt_byte_array_free ((uint8_t *)payload);
}

bool
ds_eventpipe_protocol_helper_handle_ipc_message (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	ep_return_false_if_nok (message != NULL && stream != NULL);

	bool result = false;
	EventPipeCollectTracingCommandPayload* payload = NULL;

	switch ((EventPipeCommandId)ds_ipc_header_get_commandid (ds_ipc_message_get_header_cref (message))) {
	case EP_COMMANDID_COLLECT_TRACING:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_2:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing2_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_3:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing3_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_4:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing4_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_COLLECT_TRACING_5:
		payload = (EventPipeCollectTracingCommandPayload *)ds_ipc_message_try_parse_payload (message, eventpipe_collect_tracing5_command_try_parse_payload);
		result = eventpipe_protocol_helper_collect_tracing (payload, stream);
		break;
	case EP_COMMANDID_STOP_TRACING:
		result = eventpipe_protocol_helper_stop_tracing (message, stream);
		break;
	default:
		result = eventpipe_protocol_helper_unknown_command (message, stream);
		break;
	}

	return result;
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(DS_INCLUDE_SOURCE_FILES) && !defined(DS_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_diagnostics_eventpipe_protocol;
const char quiet_linker_empty_file_warning_diagnostics_eventpipe_protocol = 0;
#endif
