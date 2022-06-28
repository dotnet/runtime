#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_STREAM_GETTER_SETTER
#define EP_IMPL_IPC_STREAM_GETTER_SETTER
#include "ep-stream.h"
#include "ep-ipc-stream.h"
#include "ep-rt.h"

/*
 * Forward declares of all static functions.
 */

static
void
file_stream_writer_free_func (void *stream);

static
bool
file_stream_writer_write_func (
	void *stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written);

static
void
file_write_end (EventPipeFile *file);

static
void
ipc_stream_writer_free_func (void *stream);

static
bool
ipc_stream_writer_write_func (
	void *stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written);

static
void
fast_serializer_write_serialization_type (
	FastSerializer *fast_serializer,
	FastSerializableObject *fast_serializable_ojbect);

/*
 * FastSerializableObject.
 */

FastSerializableObject *
ep_fast_serializable_object_init (
	FastSerializableObject *fast_serializable_object,
	FastSerializableObjectVtable *vtable,
	int32_t object_version,
	int32_t min_reader_version,
	bool is_private)
{
	EP_ASSERT (fast_serializable_object != NULL);
	EP_ASSERT (vtable != NULL);

	fast_serializable_object->vtable = vtable;
	fast_serializable_object->object_version = object_version;
	fast_serializable_object->min_reader_version = min_reader_version;
	fast_serializable_object->is_private = is_private;

	return fast_serializable_object;
}

void
ep_fast_serializable_object_fini (FastSerializableObject *fast_serializable_ojbect)
{
	;
}

void
ep_fast_serializable_object_free_vcall (FastSerializableObject *fast_serializable_ojbect)
{
	ep_return_void_if_nok (fast_serializable_ojbect != NULL);

	EP_ASSERT (fast_serializable_ojbect->vtable != NULL);
	FastSerializableObjectVtable *vtable = fast_serializable_ojbect->vtable;

	EP_ASSERT (vtable->free_func != NULL);
	vtable->free_func (fast_serializable_ojbect);
}

void
ep_fast_serializable_object_fast_serialize_vcall (
	FastSerializableObject *fast_serializable_ojbect,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (fast_serializable_ojbect != NULL);
	EP_ASSERT (fast_serializable_ojbect->vtable != NULL);

	FastSerializableObjectVtable *vtable = fast_serializable_ojbect->vtable;

	EP_ASSERT (vtable->fast_serialize_func != NULL);
	vtable->fast_serialize_func (fast_serializable_ojbect, fast_serializer);
}

const ep_char8_t *
ep_fast_serializable_object_get_type_name_vcall (FastSerializableObject *fast_serializable_ojbect)
{
	EP_ASSERT (fast_serializable_ojbect != NULL);
	EP_ASSERT (fast_serializable_ojbect->vtable != NULL);

	FastSerializableObjectVtable *vtable = fast_serializable_ojbect->vtable;

	EP_ASSERT (vtable->get_type_name_func != NULL);
	return vtable->get_type_name_func (fast_serializable_ojbect);
}

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
	EP_ASSERT (fast_serializable_ojbect != NULL);

	// Write the BeginObject tag.
	ep_fast_serializer_write_tag (fast_serializer, fast_serializable_ojbect->is_private ? FAST_SERIALIZER_TAGS_BEGIN_PRIVATE_OBJECT : FAST_SERIALIZER_TAGS_BEGIN_OBJECT, NULL, 0);

	// Write a NullReferenceTag, which implies that the following fields belong to SerializationType.
	ep_fast_serializer_write_tag (fast_serializer, FAST_SERIALIZER_TAGS_NULL_REFERENCE, NULL, 0);

	// Write the SerializationType version fields.
	int32_t serialization_type [2];
	serialization_type [0] = fast_serializable_ojbect->object_version;
	serialization_type [1] = fast_serializable_ojbect->min_reader_version;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)serialization_type, sizeof (serialization_type));

	// Write the SerializationType TypeName field.
	const ep_char8_t *type_name = ep_fast_serializable_object_get_type_name_vcall (fast_serializable_ojbect);
	if (type_name)
		ep_fast_serializer_write_string (fast_serializer, type_name, (uint32_t)strlen (type_name));

	// Write the EndObject tag.
	ep_fast_serializer_write_tag (fast_serializer, FAST_SERIALIZER_TAGS_END_OBJECT, NULL, 0);
}

FastSerializer *
ep_fast_serializer_alloc (StreamWriter *stream_writer)
{
	EP_ASSERT (stream_writer != NULL);

	const ep_char8_t signature[] = "!FastSerialization.1"; // the consumer lib expects exactly the same string, it must not be changed
	uint32_t signature_len = (uint32_t)(STRING_LENGTH (signature));

	FastSerializer *instance = ep_rt_object_alloc (FastSerializer);
	ep_raise_error_if_nok (instance != NULL);

	// Ownership transfered.
	instance->stream_writer = stream_writer;
	instance->required_padding = 0;
	instance->write_error_encountered = false;

	ep_fast_serializer_write_string (instance, signature, signature_len);

ep_on_exit:
	return instance;

ep_on_error:
	ep_fast_serializer_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_fast_serializer_free (FastSerializer *fast_serializer)
{
	ep_return_void_if_nok (fast_serializer != NULL);

	EP_ASSERT (fast_serializer->stream_writer != NULL);
	ep_stream_writer_free_vcall (fast_serializer->stream_writer);

	ep_rt_object_free (fast_serializer);
}

void
ep_fast_serializer_write_buffer (
	FastSerializer *fast_serializer,
	const uint8_t *buffer,
	uint32_t buffer_len)
{
	EP_ASSERT (fast_serializer != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len > 0);

	ep_return_void_if_nok (!fast_serializer->write_error_encountered && fast_serializer->stream_writer != NULL);

	uint32_t bytes_written = 0;
	bool result = ep_stream_writer_write (fast_serializer->stream_writer, buffer, buffer_len, &bytes_written);

	uint32_t required_padding = fast_serializer->required_padding;
	required_padding = (FAST_SERIALIZER_ALIGNMENT_SIZE + required_padding - (bytes_written % FAST_SERIALIZER_ALIGNMENT_SIZE)) % FAST_SERIALIZER_ALIGNMENT_SIZE;
	fast_serializer->required_padding = required_padding;

	// This will cause us to stop writing to the file.
	// The file will still remain open until shutdown so that we don't
	// have to take a lock at this level when we touch the file stream.
	fast_serializer->write_error_encountered = ((buffer_len != bytes_written) || !result);
}

void
ep_fast_serializer_write_object (
	FastSerializer *fast_serializer,
	FastSerializableObject *fast_serializable_ojbect)
{
	EP_ASSERT (fast_serializer != NULL);
	EP_ASSERT (fast_serializable_ojbect != NULL);

	ep_fast_serializer_write_tag (fast_serializer, fast_serializable_ojbect->is_private ? FAST_SERIALIZER_TAGS_BEGIN_PRIVATE_OBJECT : FAST_SERIALIZER_TAGS_BEGIN_OBJECT, NULL, 0);

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
	// Write the string length.
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
	uint8_t tag_as_byte = (uint8_t)tag;
	ep_fast_serializer_write_buffer (fast_serializer, &tag_as_byte, sizeof (tag_as_byte));
	if (payload != NULL) {
		EP_ASSERT (payload_len > 0);
		ep_fast_serializer_write_buffer (fast_serializer, payload, payload_len);
	}
}

/*
* FileStream.
*/

FileStream *
ep_file_stream_alloc (void)
{
	return ep_rt_object_alloc (FileStream);
}

void
ep_file_stream_free (FileStream *file_stream)
{
	ep_return_void_if_nok (file_stream != NULL);

	ep_file_stream_close (file_stream);
	ep_rt_object_free (file_stream);
}

bool
ep_file_stream_open_write (
	FileStream *file_stream,
	const ep_char8_t *path)
{
	EP_ASSERT (file_stream != NULL);

	ep_rt_file_handle_t rt_file = ep_rt_file_open_write (path);
	ep_raise_error_if_nok (rt_file != NULL);

	file_stream->rt_file = rt_file;
	return true;

ep_on_error:
	return false;
}

bool
ep_file_stream_close (FileStream *file_stream)
{
	ep_return_false_if_nok (file_stream != NULL);
	bool result = ep_rt_file_close (file_stream->rt_file);
	file_stream->rt_file = NULL;
	return result;
}

bool
ep_file_stream_write (
	FileStream *file_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	EP_ASSERT (file_stream != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (bytes_to_write > 0);
	EP_ASSERT (bytes_written != NULL);

	return ep_rt_file_write (file_stream->rt_file, buffer, bytes_to_write, bytes_written);
}

/*
 * FileStreamWriter.
 */

static
void
file_stream_writer_free_func (void *stream)
{
	ep_file_stream_writer_free ((FileStreamWriter *)stream);
}

static
bool
file_stream_writer_write_func (
	void *stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	EP_ASSERT (stream != NULL);

	return ep_file_stream_writer_write (
		(FileStreamWriter *)stream,
		buffer,
		bytes_to_write,
		bytes_written);
}

static StreamWriterVtable file_stream_writer_vtable = {
	file_stream_writer_free_func,
	file_stream_writer_write_func };

FileStreamWriter *
ep_file_stream_writer_alloc (const ep_char8_t *output_file_path)
{
	EP_ASSERT (output_file_path != NULL);

	FileStreamWriter *instance = ep_rt_object_alloc (FileStreamWriter);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_stream_writer_init (
		&instance->stream_writer,
		&file_stream_writer_vtable) != NULL);

	instance->file_stream = ep_file_stream_alloc ();
	ep_raise_error_if_nok (instance->file_stream != NULL);

	ep_raise_error_if_nok (ep_file_stream_open_write (instance->file_stream, output_file_path));

ep_on_exit:
	return instance;

ep_on_error:
	ep_file_stream_writer_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_file_stream_writer_free (FileStreamWriter *file_stream_writer)
{
	ep_return_void_if_nok (file_stream_writer != NULL);

	ep_file_stream_free (file_stream_writer->file_stream);
	ep_stream_writer_fini (&file_stream_writer->stream_writer);
	ep_rt_object_free (file_stream_writer);
}

bool
ep_file_stream_writer_write (
	FileStreamWriter *file_stream_writer,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	EP_ASSERT (file_stream_writer != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (bytes_to_write > 0);
	EP_ASSERT (bytes_written != NULL);

	bool result = false;

	ep_raise_error_if_nok (ep_file_stream_writer_get_file_stream (file_stream_writer) != NULL);
	result = ep_file_stream_write (ep_file_stream_writer_get_file_stream (file_stream_writer), buffer, bytes_to_write, bytes_written);

ep_on_exit:
	return result;

ep_on_error:
	*bytes_written = 0;
	ep_exit_error_handler ();
}

/*
* IpcStream.
*/

IpcStream *
ep_ipc_stream_init (
	IpcStream *ipc_stream,
	IpcStreamVtable *vtable)
{
	EP_ASSERT (ipc_stream != NULL);
	EP_ASSERT (vtable != NULL);

	ipc_stream->vtable = vtable;
	return ipc_stream;
}

void
ep_ipc_stream_fini (IpcStream *ipc_stream)
{
	return;
}

void
ep_ipc_stream_free_vcall (IpcStream *ipc_stream)
{
	ep_return_void_if_nok (ipc_stream != NULL);

	EP_ASSERT (ipc_stream->vtable != NULL);
	IpcStreamVtable *vtable = ipc_stream->vtable;

	ep_ipc_stream_flush_vcall (ipc_stream);
	ep_ipc_stream_close_vcall (ipc_stream);

	EP_ASSERT (vtable->free_func != NULL);
	vtable->free_func (ipc_stream);
}

bool
ep_ipc_stream_read_vcall (
	IpcStream *ipc_stream,
	uint8_t *buffer,
	uint32_t bytes_to_read,
	uint32_t *bytes_read,
	uint32_t timeout_ms)
{
	EP_ASSERT (ipc_stream != NULL);

	EP_ASSERT (ipc_stream->vtable != NULL);
	IpcStreamVtable *vtable = ipc_stream->vtable;

	EP_ASSERT (vtable->read_func != NULL);
	return vtable->read_func (ipc_stream, buffer, bytes_to_read, bytes_read, timeout_ms);
}

bool
ep_ipc_stream_write_vcall (
	IpcStream *ipc_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written,
	uint32_t timeout_ms)
{
	EP_ASSERT (ipc_stream != NULL);

	EP_ASSERT (ipc_stream->vtable != NULL);
	IpcStreamVtable *vtable = ipc_stream->vtable;

	EP_ASSERT (vtable->write_func != NULL);
	return vtable->write_func (ipc_stream, buffer, bytes_to_write, bytes_written, timeout_ms);
}

bool
ep_ipc_stream_flush_vcall (IpcStream *ipc_stream)
{
	EP_ASSERT (ipc_stream != NULL);

	EP_ASSERT (ipc_stream->vtable != NULL);
	IpcStreamVtable *vtable = ipc_stream->vtable;

	EP_ASSERT (vtable->flush_func != NULL);
	return vtable->flush_func (ipc_stream);
}

bool
ep_ipc_stream_close_vcall (IpcStream *ipc_stream)
{
	EP_ASSERT (ipc_stream != NULL);

	EP_ASSERT (ipc_stream->vtable != NULL);
	IpcStreamVtable *vtable = ipc_stream->vtable;

	EP_ASSERT (vtable->close_func != NULL);
	return vtable->close_func (ipc_stream);
}

/*
 * IpcStreamWriter.
 */

static
void
ipc_stream_writer_free_func (void *stream)
{
	ep_ipc_stream_writer_free ((IpcStreamWriter *)stream);
}

static
bool
ipc_stream_writer_write_func (
	void *stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	EP_ASSERT (stream != NULL);

	return ep_ipc_stream_writer_write (
		(IpcStreamWriter *)stream,
		buffer,
		bytes_to_write,
		bytes_written);
}

static StreamWriterVtable ipc_stream_writer_vtable = {
	ipc_stream_writer_free_func,
	ipc_stream_writer_write_func };

IpcStreamWriter *
ep_ipc_stream_writer_alloc (
	uint64_t id,
	IpcStream *stream)
{
	IpcStreamWriter *instance = ep_rt_object_alloc (IpcStreamWriter);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_stream_writer_init (
		&instance->stream_writer,
		&ipc_stream_writer_vtable) != NULL);

	//Ownership transfered.
	instance->ipc_stream = stream;

ep_on_exit:
	return instance;

ep_on_error:
	ep_ipc_stream_writer_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_ipc_stream_writer_free (IpcStreamWriter *ipc_stream_writer)
{
	ep_return_void_if_nok (ipc_stream_writer != NULL);

	ep_ipc_stream_free_vcall (ipc_stream_writer->ipc_stream);
	ep_stream_writer_fini (&ipc_stream_writer->stream_writer);
	ep_rt_object_free (ipc_stream_writer);
}

bool
ep_ipc_stream_writer_write (
	IpcStreamWriter *ipc_stream_writer,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	EP_ASSERT (ipc_stream_writer != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (bytes_to_write > 0);
	EP_ASSERT (bytes_written != NULL);

	ep_return_false_if_nok (buffer != NULL && bytes_to_write != 0);

	bool result = false;

	ep_raise_error_if_nok (ep_ipc_stream_writer_get_ipc_stream (ipc_stream_writer) != NULL);
	result = ep_ipc_stream_write_vcall (ep_ipc_stream_writer_get_ipc_stream (ipc_stream_writer), buffer, bytes_to_write, bytes_written, EP_INFINITE_WAIT);

ep_on_exit:
	return result;

ep_on_error:
	*bytes_written = 0;
	ep_exit_error_handler ();
}

/*
 * StreamWriter.
 */

StreamWriter *
ep_stream_writer_init (
	StreamWriter *stream_writer,
	StreamWriterVtable *vtable)
{
	EP_ASSERT (stream_writer != NULL);
	EP_ASSERT (vtable != NULL);

	stream_writer->vtable = vtable;

	return stream_writer;
}

void
ep_stream_writer_fini (StreamWriter *stream_writer)
{
	;
}

void
ep_stream_writer_free_vcall (StreamWriter *stream_writer)
{
	ep_return_void_if_nok (stream_writer != NULL);

	EP_ASSERT (stream_writer->vtable != NULL);
	StreamWriterVtable *vtable = stream_writer->vtable;

	EP_ASSERT (vtable->free_func != NULL);
	vtable->free_func (stream_writer);
}

bool
ep_stream_writer_write_vcall (
	StreamWriter *stream_writer,
	const uint8_t *buffer,
	const uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	EP_ASSERT (stream_writer != NULL);
	EP_ASSERT (stream_writer->vtable != NULL);

	StreamWriterVtable *vtable = stream_writer->vtable;

	EP_ASSERT (vtable->write_func != NULL);
	return vtable->write_func (stream_writer, buffer, bytes_to_write, bytes_written);
}

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

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_stream;
const char quiet_linker_empty_file_warning_eventpipe_stream = 0;
#endif
