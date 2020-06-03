#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

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
	EP_ASSERT (fast_serializable_object != NULL && vtable != NULL);

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
	EP_ASSERT (fast_serializable_ojbect != NULL && fast_serializable_ojbect->vtable != NULL);
	FastSerializableObjectVtable *vtable = fast_serializable_ojbect->vtable;

	EP_ASSERT (vtable->fast_serialize_func != NULL);
	vtable->fast_serialize_func (fast_serializable_ojbect, fast_serializer);
}

const ep_char8_t *
ep_fast_serializable_object_get_type_name_vcall (FastSerializableObject *fast_serializable_ojbect)
{
	EP_ASSERT (fast_serializable_ojbect != NULL && fast_serializable_ojbect->vtable != NULL);
	FastSerializableObjectVtable *vtable = fast_serializable_ojbect->vtable;

	EP_ASSERT (vtable->get_type_name_func != NULL);
	return vtable->get_type_name_func (fast_serializable_ojbect);
}

/*
 * FastSerializer.
 */
FastSerializer *
ep_fast_serializer_alloc (StreamWriter *stream_writer)
{
	ep_return_null_if_nok (stream_writer != NULL);

	const ep_char8_t signature[] = "!FastSerialization.1"; // the consumer lib expects exactly the same string, it must not be changed
	uint32_t signature_len = EP_ARRAY_SIZE (signature) - 1;

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
	ep_return_null_if_nok (output_file_path != NULL);

	FileStreamWriter *instance = ep_rt_object_alloc (FileStreamWriter);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_stream_writer_init (
		&instance->stream_writer,
		&file_stream_writer_vtable) != NULL);

	instance->file_stream = ep_file_stream_alloc ();
	ep_raise_error_if_nok (instance->file_stream != NULL);

	if (!ep_file_stream_open_write (instance->file_stream, output_file_path)) {
		EP_ASSERT (!"Unable to open file for write.");
		ep_raise_error ();
	}

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

/*
 * IpcStream.
 */

IpcStream *
ep_ipc_stream_alloc (ep_rt_ipc_handle_t rt_ipc)
{
	IpcStream *instance = ep_rt_object_alloc (IpcStream);
	ep_raise_error_if_nok (instance != NULL);

	//Transfer ownership.
	instance->rt_ipc = rt_ipc;

ep_on_exit:
	return instance;

ep_on_error:
	ep_ipc_stream_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_ipc_stream_free (IpcStream *ipc_stream)
{
	ep_return_void_if_nok (ipc_stream != NULL);

	ep_ipc_stream_flush (ipc_stream);
	ep_ipc_stream_disconnect (ipc_stream);
	ep_ipc_stream_close (ipc_stream);

	ep_rt_object_free (ipc_stream);
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

	ep_ipc_stream_free (ipc_stream_writer->ipc_stream);
	ep_stream_writer_fini (&ipc_stream_writer->stream_writer);
	ep_rt_object_free (ipc_stream_writer);
}

/*
 * StreamWriter.
 */

StreamWriter *
ep_stream_writer_init (
	StreamWriter *stream_writer,
	StreamWriterVtable *vtable)
{
	EP_ASSERT (stream_writer != NULL && vtable != NULL);

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
	EP_ASSERT (stream_writer != NULL && stream_writer->vtable != NULL);
	StreamWriterVtable *vtable = stream_writer->vtable;

	EP_ASSERT (vtable->write_func != NULL);
	return vtable->write_func (stream_writer, buffer, bytes_to_write, bytes_written);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_stream_internals;
const char quiet_linker_empty_file_warning_eventpipe_stream_internals = 0;
#endif
