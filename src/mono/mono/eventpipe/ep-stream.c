#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * Forward declares of all static functions.
 */

static
void
fast_serializer_write_serialization_type (
	FastSerializer *fast_serializer,
	FastSerializableObject *fast_serializable_ojbect);

/*
 * FastSerializableObject.
 */

void
ep_fast_serializable_object_fast_serialize (
	FastSerializableObject *fast_serializable_ojbect,
	FastSerializer *fast_serializer)
{
	ep_fast_serializable_object_fast_serialize_vcall (fast_serializable_ojbect, fast_serializer);
}

const ep_char8_t *
ep_fast_serializable_object_get_type_name (FastSerializableObject *fast_serializable_ojbect)
{
	return ep_fast_serializable_object_get_type_name_vcall (fast_serializable_ojbect);
}

/*
 * FastSerializer.
 */

static
void
fast_serializer_write_serialization_type (
	FastSerializer *fast_serializer,
	FastSerializableObject *fast_serializable_ojbect)
{
	ep_return_void_if_nok (fast_serializable_ojbect != NULL);

	// Write the BeginObject tag.
	ep_fast_serializer_write_tag (fast_serializer, ep_fast_serializable_object_get_is_private (fast_serializable_ojbect) ? FAST_SERIALIZER_TAGS_BEGIN_PRIVATE_OBJECT : FAST_SERIALIZER_TAGS_BEGIN_OBJECT, NULL, 0);

	// Write a NullReferenceTag, which implies that the following fields belong to SerializationType.
	ep_fast_serializer_write_tag (fast_serializer, FAST_SERIALIZER_TAGS_NULL_REFERENCE, NULL, 0);

	// Write the SerializationType version fields.
	int32_t serialization_type [2];
	serialization_type [0] = ep_fast_serializable_object_get_object_version (fast_serializable_ojbect);
	serialization_type [1] = ep_fast_serializable_object_get_min_reader_version (fast_serializable_ojbect);
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)serialization_type, sizeof (serialization_type));

	// Write the SerializationType TypeName field.
	const ep_char8_t *type_name = ep_fast_serializable_object_get_type_name_vcall (fast_serializable_ojbect);
	if (type_name)
		ep_fast_serializer_write_string (fast_serializer, type_name, (uint32_t)ep_rt_utf8_string_len (type_name));

	// Write the EndObject tag.
	ep_fast_serializer_write_tag (fast_serializer, FAST_SERIALIZER_TAGS_END_OBJECT, NULL, 0);
}

void
ep_fast_serializer_write_buffer (
	FastSerializer *fast_serializer,
	const uint8_t *buffer,
	uint32_t buffer_len)
{
	ep_return_void_if_nok (fast_serializer != NULL && buffer != NULL && buffer_len > 0);
	ep_return_void_if_nok (ep_fast_serializer_get_write_error_encountered (fast_serializer) != true && ep_fast_serializer_get_stream_writer (fast_serializer) != NULL);

	uint32_t bytes_written = 0;
	bool result = ep_stream_writer_write (ep_fast_serializer_get_stream_writer (fast_serializer), buffer, buffer_len, &bytes_written);

	uint32_t required_padding = ep_fast_serializer_get_required_padding (fast_serializer);
	required_padding = (FAST_SERIALIZER_ALIGNMENT_SIZE + required_padding - (bytes_written & FAST_SERIALIZER_ALIGNMENT_SIZE)) % FAST_SERIALIZER_ALIGNMENT_SIZE;
	ep_fast_serializer_set_required_padding (fast_serializer, required_padding);

	// This will cause us to stop writing to the file.
	// The file will still remain open until shutdown so that we don't
	// have to take a lock at this level when we touch the file stream.
	ep_fast_serializer_set_write_error_encountered (fast_serializer, ((buffer_len != bytes_written) || !result));
}

void
ep_fast_serializer_write_object (
	FastSerializer *fast_serializer,
	FastSerializableObject *fast_serializable_ojbect)
{
	ep_return_void_if_nok (fast_serializer != NULL && fast_serializable_ojbect != NULL);

	ep_fast_serializer_write_tag (fast_serializer, ep_fast_serializable_object_get_is_private (fast_serializable_ojbect) ? FAST_SERIALIZER_TAGS_BEGIN_PRIVATE_OBJECT : FAST_SERIALIZER_TAGS_BEGIN_OBJECT, NULL, 0);

	fast_serializer_write_serialization_type (fast_serializer, fast_serializable_ojbect);

	// Ask the object to serialize itself using the current serializer.
	ep_fast_serializable_object_fast_serialize_vcall (fast_serializable_ojbect, fast_serializer);

	ep_fast_serializer_write_tag (fast_serializer, FAST_SERIALIZER_TAGS_END_OBJECT, NULL, 0);
}

void
ep_fast_serializer_write_string (
	FastSerializer *fast_serializer,
	const ep_char8_t *contents,
	uint32_t contents_len)
{
	// Write teh string length.
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&contents_len, sizeof (contents_len));

	//Wirte the string contents.
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)contents, contents_len);
}

void
ep_fast_serializer_write_tag (
	FastSerializer *fast_serializer,
	FastSerializerTags tag,
	const uint8_t *payload,
	uint32_t payload_len)
{
	uint8_t tag_as_byte = tag;
	ep_fast_serializer_write_buffer (fast_serializer, &tag_as_byte, sizeof (tag_as_byte));
	if (payload != NULL) {
		EP_ASSERT (payload_len > 0);
		ep_fast_serializer_write_buffer (fast_serializer, payload, payload_len);
	}
}

/*
* FileStream.
*/

bool
ep_file_stream_open_write (
	FileStream *file_stream,
	const ep_char8_t *path)
{
	ep_return_false_if_nok (file_stream != NULL);

	ep_rt_file_handle_t rt_file = ep_rt_file_open_write (path);
	ep_raise_error_if_nok (rt_file != NULL);

	ep_file_stream_set_rt_file (file_stream, rt_file);
	return true;

ep_on_error:
	return false;
}

bool
ep_file_stream_close (FileStream *file_stream)
{
	ep_return_false_if_nok (file_stream != NULL);
	return ep_rt_file_close (ep_file_stream_get_rt_file (file_stream));
}

bool
ep_file_stream_write (
	FileStream *file_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	ep_return_false_if_nok (file_stream != NULL && buffer != NULL && bytes_to_write > 0 && bytes_written != NULL);
	return ep_rt_file_write (ep_file_stream_get_rt_file (file_stream), buffer, bytes_to_write, bytes_written);
}

/*
 * FileStreamWriter.
 */

bool
ep_file_stream_writer_write (
	FileStreamWriter *file_stream_writer,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	ep_return_false_if_nok (file_stream_writer != NULL && buffer != NULL && bytes_to_write > 0 && bytes_written != NULL);

	ep_raise_error_if_nok (ep_file_stream_writer_get_file_stream (file_stream_writer) != NULL);

	return ep_file_stream_write (ep_file_stream_writer_get_file_stream (file_stream_writer), buffer, bytes_to_write, bytes_written);

ep_on_error:
	*bytes_written = 0;
	return false;
}

/*
* IpcStream.
*/

bool
ep_ipc_stream_flush (IpcStream *ipc_stream)
{
	//TODO: Implement.
	return false;
}

bool
ep_ipc_stream_disconnect (IpcStream *ipc_stream)
{
	//TODO: Implement.
	return false;
}

bool
ep_ipc_stream_close (IpcStream *ipc_stream)
{
	//TODO: Implement.
	return false;
}

bool
ep_ipc_stream_write (
	IpcStream *ipc_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	//TODO: Implement.
	return false;
}

/*
 * IpcStreamWriter.
 */

bool
ep_ipc_stream_writer_write (
	IpcStreamWriter *ipc_stream_writer,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	ep_return_false_if_nok (ipc_stream_writer != NULL && buffer != NULL && bytes_to_write > 0 && bytes_written != NULL);

	ep_raise_error_if_nok (ep_ipc_stream_writer_get_ipc_stream (ipc_stream_writer) != NULL);

	return ep_ipc_stream_write (ep_ipc_stream_writer_get_ipc_stream (ipc_stream_writer), buffer, bytes_to_write, bytes_written);

ep_on_error:
	*bytes_written = 0;
	return false;
}

/*
 * StreamWriter.
 */

bool
ep_stream_writer_write (
	StreamWriter *stream_writer,
	const uint8_t *buffer,
	const uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	return ep_stream_writer_write_vcall (
		stream_writer,
		buffer,
		bytes_to_write,
		bytes_written);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_stream;
const char quiet_linker_empty_file_warning_eventpipe_stream = 0;
#endif
