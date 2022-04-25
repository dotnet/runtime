#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_METADATA_GENERATOR_GETTER_SETTER
#include "ep-metadata-generator.h"
#include "ep-rt.h"

/*
 * Forward declares of all static functions.
 */

static
bool
metadata_generator_has_v2_param_types (
	EventPipeParameterDesc *params,
	uint32_t params_len);

static
void
metadata_generator_get_metadata_len (
	uint32_t event_id,
	const ep_char16_t *event_name,
	size_t event_name_len,
	uint64_t keywords,
	uint32_t version,
	EventPipeEventLevel level,
	uint8_t opcode,
	EventPipeParameterDesc *params,
	uint32_t params_len,
	size_t *total_len,
	size_t *v2_len);

static
void
metadata_generator_write_uint8_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	uint8_t value);

static
void
metadata_generator_write_uint32_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	uint32_t value);

static
void
metadata_generator_write_int64_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	int64_t value);

static
void
metadata_generator_write_string_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	const ep_char16_t *value);

/*
 * EventPipeMetadataGenerator.
 */

static
bool
metadata_generator_has_v2_param_types (
	EventPipeParameterDesc *params,
	uint32_t params_len)
{
	EP_ASSERT (params != NULL || params_len == 0);

	for (uint32_t i = 0; i < params_len; ++i) {
		if (params [i].type == EP_PARAMETER_TYPE_ARRAY)
			return true;
	}

	return false;
}

static
void
metadata_generator_get_metadata_len (
	uint32_t event_id,
	const ep_char16_t *event_name,
	size_t event_name_len,
	uint64_t keywords,
	uint32_t version,
	EventPipeEventLevel level,
	uint8_t opcode,
	EventPipeParameterDesc *params,
	uint32_t params_len,
	size_t *total_len,
	size_t *v2_len)
{
	EP_ASSERT (event_name != NULL);
	EP_ASSERT (params_len == 0 || params != NULL);
	EP_ASSERT (total_len != NULL);
	EP_ASSERT (v2_len != NULL);

	bool has_v2_types = metadata_generator_has_v2_param_types (params, params_len);
	*v2_len = 0;

	// eventID              : 4 bytes
	// eventName            : (eventName.Length + 1) * 2 bytes
	// keywords             : 8 bytes
	// eventVersion         : 4 bytes
	// level                : 4 bytes
	// parameterCount       : 4 bytes
	*total_len = 24 + ((event_name_len + 1) * sizeof (ep_char16_t));

	if (opcode != 0)
		// Size of the opcode tag
		*total_len += 6;

	if (has_v2_types) {
		// need 4 bytes for the length of the tag
		// 1 byte for the tag identifier
		// and 4 bytes for the count of params
		*total_len += 9;
		// The metadata tag length does not include the required
		// length and tag fields
		*v2_len = 4;

		// Each parameter has an optional array identifier and then a 4 byte
		// TypeCode + the field name (parameterName.Length + 1) * 2 bytes.
		for(uint32_t i = 0; i < params_len; ++i) {
			EP_ASSERT(params [i].name != NULL);
			// For v2 metadata, fields start with a length (4 bytes) and then the field name
			size_t param_size = (4 + ((ep_rt_utf16_string_len (params [i].name) + 1) * sizeof (ep_char16_t)));

			if (params [i].type == EP_PARAMETER_TYPE_ARRAY)
				// If it's an array type we write the array descriptor (4 bytes)
				param_size += 4;

			// Then the typecode
			param_size += 4;

			*total_len += param_size;
			*v2_len += param_size;
		}
	} else {
		// Each parameter has a 4 byte TypeCode + (parameterName.Length + 1) * 2 bytes.
		for (uint32_t i = 0; i < params_len; ++i) {
			EP_ASSERT (params [i].name != NULL);
			*total_len += (4 + ((ep_rt_utf16_string_len (params [i].name) + 1) * sizeof (ep_char16_t)));
		}
	}
}

static
void
metadata_generator_write_uint8_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	uint8_t value)
{
	EP_ASSERT ((*offset + sizeof (value)) <= buffer_len);
	memcpy (buffer + *offset, &value, sizeof (value));
	*offset += sizeof (value);
}

static
void
metadata_generator_write_uint32_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	uint32_t value)
{
	EP_ASSERT ((*offset + sizeof (value)) <= buffer_len);
	memcpy (buffer + *offset, &value, sizeof (value));
	*offset += sizeof (value);
}

static
void
metadata_generator_write_int64_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	int64_t value)
{
	EP_ASSERT ((*offset + sizeof (value)) <= buffer_len);
	memcpy (buffer + *offset, &value, sizeof (value));
	*offset += sizeof (value);
}

static
void
metadata_generator_write_string_to_buffer (
	uint8_t *buffer,
	size_t buffer_len,
	size_t *offset,
	const ep_char16_t *value)
{
	size_t const value_len = ep_rt_utf16_string_len (value);
	EP_ASSERT ((*offset + ((value_len + 1) * sizeof (ep_char16_t))) <= buffer_len);
	memcpy (buffer + *offset, value, (value_len + 1) * sizeof (ep_char16_t));
	*offset += (value_len + 1) * sizeof (ep_char16_t);
}

uint8_t *
ep_metadata_generator_generate_event_metadata (
	uint32_t event_id,
	const ep_char16_t *event_name,
	uint64_t keywords,
	uint32_t version,
	EventPipeEventLevel level,
	uint8_t opcode,
	EventPipeParameterDesc *params,
	uint32_t params_len,
	size_t *metadata_len)
{
	EP_ASSERT (event_name != NULL);
	EP_ASSERT (params_len == 0 || params != NULL);
	EP_ASSERT (metadata_len != NULL);

	size_t total_metadata_len = 0;
	size_t v2_metadata_len = 0;
	size_t offset = 0;
	uint8_t *buffer = NULL;

	size_t event_name_len = ep_rt_utf16_string_len (event_name);

	metadata_generator_get_metadata_len (
		event_id,
		event_name,
		event_name_len,
		keywords,
		version,
		level,
		opcode,
		params,
		params_len,
		&total_metadata_len,
		&v2_metadata_len);

	bool has_v2_types = v2_metadata_len > 0;
	*metadata_len = total_metadata_len;

	// Allocate a metadata blob.
	buffer = ep_rt_byte_array_alloc (*metadata_len);
	ep_raise_error_if_nok (buffer != NULL);

	// Write the event ID.
	metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, event_id);

	// Write the event name.
	metadata_generator_write_string_to_buffer (buffer, total_metadata_len, &offset, event_name);

	// Write the keywords.
	metadata_generator_write_int64_to_buffer (buffer, total_metadata_len, &offset, keywords);

	// Write the version.
	metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, version);

	// Write the level.
	metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)level);

	if (has_v2_types) {
		// If we have V2 metadata types, we need to have 0 params for V1
		metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, 0);
	} else {
		EP_ASSERT (!has_v2_types);

		// Write the parameter count.
		metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, params_len);

		// Write the parameter descriptions.
		for (uint32_t i = 0; i < params_len; ++i) {
			metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)params [i].type);
			metadata_generator_write_string_to_buffer (buffer, total_metadata_len, &offset, params [i].name);
		}
	}

	// Now we write optional V2 metadata, if there is any

	if (opcode != 0) {
		// Size of opcode
		metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, 1);
		// opcode tag
		metadata_generator_write_uint8_to_buffer (buffer, total_metadata_len, &offset, (uint8_t)EP_METADATA_TAG_OPCODE);
		// opcode value
		metadata_generator_write_uint8_to_buffer (buffer, total_metadata_len, &offset, opcode);
	}

	if (has_v2_types) {
		// size of V2 metadata payload
		metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)v2_metadata_len);
		// v2 param tag
		metadata_generator_write_uint8_to_buffer (buffer, total_metadata_len, &offset, (uint8_t)EP_METADATA_TAG_PARAMETER_PAYLOAD);
		// Write the parameter count.
		metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, params_len);
		// Write the parameter descriptions.
		for (uint32_t i = 0; i < params_len; ++i) {
			size_t param_name_len = ep_rt_utf16_string_len (params [i].name);
			size_t param_name_bytes = ((param_name_len + 1 ) * sizeof (ep_char16_t));
			if (params [i].type == EP_PARAMETER_TYPE_ARRAY) {
				// For an array type, length is 12 (4 bytes length field, 4 bytes array descriptor, 4 bytes typecode)
				// + name length
				metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)(12 + param_name_bytes));
				// Now write the event name
				metadata_generator_write_string_to_buffer (buffer, total_metadata_len, &offset, params [i].name);
				// And there is the array descriptor
				metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)EP_PARAMETER_TYPE_ARRAY);
				// Now write the underlying type
				metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)params [i].element_type);
			} else {
				// For a non array type, length is 8 (4 bytes length field, 4 bytes typecode)
				// + name length
				metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)(8 + param_name_bytes));
				// Now write the event name
				metadata_generator_write_string_to_buffer (buffer, total_metadata_len, &offset, params [i].name);
				// And then the type
				metadata_generator_write_uint32_to_buffer (buffer, total_metadata_len, &offset, (uint32_t)params [i].type);
			}
		}
	}

	EP_ASSERT (*metadata_len == offset);

ep_on_exit:
	return buffer;

ep_on_error:
	ep_rt_byte_array_free (buffer);
	buffer = NULL;
	*metadata_len = 0;
	ep_exit_error_handler ();
}

/*
 * EventPipeParameterDesc.
 */

EventPipeParameterDesc *
ep_parameter_desc_init (
	EventPipeParameterDesc *parameter_desc,
	EventPipeParameterType type,
	const ep_char16_t *name)
{
	EP_ASSERT (parameter_desc != NULL);

	parameter_desc->type = type;
	parameter_desc->element_type = EP_PARAMETER_TYPE_EMPTY;
	parameter_desc->name = name;

	return parameter_desc;
}

void
ep_parameter_desc_fini (EventPipeParameterDesc *parameter_desc)
{
	;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_metadata_generator;
const char quiet_linker_empty_file_warning_eventpipe_metadata_generator = 0;
#endif
