#ifndef __EVENTPIPE_STREAM_H__
#define __EVENTPIPE_STREAM_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_STREAM_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

// the enumeration has a specific set of values to keep it compatible with consumer library
// it's sibling is defined in https://github.com/Microsoft/perfview/blob/10d1f92b242c98073b3817ac5ee6d98cd595d39b/src/FastSerialization/FastSerialization.cs#L2295
typedef enum
{
	FAST_SERIALIZER_TAGS_ERROR                  = 0, // To improve debugability, 0 is an illegal tag.
	FAST_SERIALIZER_TAGS_NULL_REFERENCE         = 1, // Tag for a null object forwardReference.
	FAST_SERIALIZER_TAGS_OBJECT_REFERENCE       = 2, // Followed by StreamLabel
	                                                 // 3 used to belong to ForwardReference, which got removed in V3
	FAST_SERIALIZER_TAGS_BEGIN_OBJECT           = 4, // Followed by Type object, object data, tagged EndObject
	FAST_SERIALIZER_TAGS_BEGIN_PRIVATE_OBJECT   = 5, // Like beginObject, but not placed in interning table on deserialiation
	FAST_SERIALIZER_TAGS_END_OBJECT             = 6, // Placed after an object to mark its end.
	                                                 // 7 used to belong to ForwardDefinition, which got removed in V3
	FAST_SERIALIZER_TAGS_BYTE                   = 8,
	FAST_SERIALIZER_TAGS_INT16,
	FAST_SERIALIZER_TAGS_INT32,
	FAST_SERIALIZER_TAGS_INT64,
	FAST_SERIALIZER_TAGS_SKIP_REGION,
	FAST_SERIALIZER_TAGS_STRING,
	FAST_SERIALIZER_TAGS_BLOB,
	FAST_SERIALIZER_TAGS_LIMIT                       // Just past the last valid tag, used for asserts.
} FastSerializerTags;

/*
 * StreamWriter.
 */

typedef void (*StreamWriterFreeFunc)(void *stream);
typedef bool (*StreamWriterWriteFunc)(void *stream, const uint8_t *buffer, const uint32_t bytes_to_write, uint32_t *bytes_written);

struct _StreamWriterVtable {
	StreamWriterFreeFunc free_func;
	StreamWriterWriteFunc write_func;
};

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _StreamWriter {
#else
struct _StreamWriter_Internal {
#endif
	StreamWriterVtable *vtable;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _StreamWriter {
	uint8_t _internal [sizeof (struct _StreamWriter_Internal)];
};
#endif

StreamWriter *
ep_stream_writer_init (
	StreamWriter *stream_writer,
	StreamWriterVtable *vtable);

void
ep_stream_writer_fini (StreamWriter *stream_writer);

bool
ep_stream_writer_write (
	StreamWriter *stream_writer,
	const uint8_t *buffer,
	const uint32_t bytes_to_write,
	uint32_t *bytes_written);

void
ep_stream_writer_free_vcall (StreamWriter *stream_writer);

bool
ep_stream_writer_write_vcall (
	StreamWriter *stream_writer,
	const uint8_t *buffer,
	const uint32_t bytes_to_write,
	uint32_t *bytes_written);

/*
 * FastSerializableObject.
 */

typedef void (*FastSerializableObjectFreeFunc)(void *object);
typedef void (*FastSerializableObjectFastSerializeFunc)(void *object, FastSerializer *fast_serializer);
typedef const ep_char8_t * (*FastSerializableObjectGetTypeNameFunc)(void *object);

struct _FastSerializableObjectVtable {
	FastSerializableObjectFreeFunc free_func;
	FastSerializableObjectFastSerializeFunc fast_serialize_func;
	FastSerializableObjectGetTypeNameFunc get_type_name_func;
};

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FastSerializableObject {
#else
struct _FastSerializableObject_Internal {
#endif
	FastSerializableObjectVtable *vtable;
	int32_t object_version;
	int32_t min_reader_version;
	bool is_private;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FastSerializableObject {
	uint8_t _internal [sizeof (struct _FastSerializableObject_Internal)];
};
#endif

EP_DEFINE_GETTER(FastSerializableObject *, fast_serializable_object, FastSerializableObjectVtable *, vtable)

FastSerializableObject *
ep_fast_serializable_object_init (
	FastSerializableObject *fast_serializable_object,
	FastSerializableObjectVtable *vtable,
	int32_t object_version,
	int32_t min_reader_version,
	bool is_private);

void
ep_fast_serializable_object_fini (FastSerializableObject *fast_serializable_object);

void
ep_fast_serializable_object_fast_serialize (
	FastSerializableObject *fast_serializable_ojbect,
	FastSerializer *fast_serializer);

const ep_char8_t *
ep_fast_serializable_object_get_type_name (FastSerializableObject *fast_serializable_ojbect);

void
ep_fast_serializable_object_free_vcall (FastSerializableObject *fast_serializable_ojbect);

const ep_char8_t *
ep_fast_serializable_object_get_type_name_vcall (FastSerializableObject *fast_serializable_ojbect);

void
ep_fast_serializable_object_fast_serialize_vcall (
	FastSerializableObject *fast_serializable_ojbect,
	FastSerializer *fast_serializer);

/*
 * FastSerializer.
 */

#define FAST_SERIALIZER_ALIGNMENT_SIZE 4

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FastSerializer {
#else
struct _FastSerializer_Internal {
#endif
	StreamWriter *stream_writer;
	uint32_t required_padding;
	bool write_error_encountered;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FastSerializer {
	uint8_t _internal [sizeof (struct _FastSerializer_Internal)];
};
#endif

EP_DEFINE_GETTER(FastSerializer *, fast_serializer, StreamWriter *, stream_writer)
EP_DEFINE_GETTER(FastSerializer *, fast_serializer, uint32_t, required_padding)
EP_DEFINE_GETTER(FastSerializer *, fast_serializer, bool, write_error_encountered)

FastSerializer *
ep_fast_serializer_alloc (StreamWriter *stream_writer);

void
ep_fast_serializer_free (FastSerializer *fast_serializer);

void
ep_fast_serializer_write_buffer (
	FastSerializer *fast_serializer,
	const uint8_t *buffer,
	uint32_t buffer_len);

void
ep_fast_serializer_write_object (
	FastSerializer *fast_serializer,
	FastSerializableObject *fast_serializable_ojbect);

void
ep_fast_serializer_write_string (
	FastSerializer *fast_serializer,
	const ep_char8_t *contents,
	uint32_t contents_len);

void
ep_fast_serializer_write_tag (
	FastSerializer *fast_serializer,
	FastSerializerTags tag,
	const uint8_t *payload,
	uint32_t payload_len);

/*
* FileStream.
*/

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FileStream {
#else
struct _FileStream_Internal {
#endif
	ep_rt_file_handle_t rt_file;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FileStream {
	uint8_t _internal [sizeof (struct _FileStream_Internal)];
};
#endif

EP_DEFINE_GETTER(FileStream *, file_stream, ep_rt_file_handle_t, rt_file)
EP_DEFINE_SETTER(FileStream *, file_stream, ep_rt_file_handle_t, rt_file)

FileStream *
ep_file_stream_alloc (void);

void
ep_file_stream_free (FileStream *file_stream);

bool
ep_file_stream_open_write (
	FileStream *file_stream,
	const ep_char8_t *path);

bool
ep_file_stream_close (FileStream *file_stream);

bool
ep_file_stream_write (
	FileStream *file_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written);

/*
 * FileStreamWriter.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FileStreamWriter {
#else
struct _FileStreamWriter_Internal {
#endif
	StreamWriter stream_writer;
	FileStream *file_stream;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _FileStreamWriter {
	uint8_t _internal [sizeof (struct _FileStreamWriter_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(FileStreamWriter *, file_stream_writer, StreamWriter *, stream_writer)
EP_DEFINE_GETTER(FileStreamWriter *, file_stream_writer, FileStream *, file_stream)

FileStreamWriter *
ep_file_stream_writer_alloc (const ep_char8_t *output_file_path);

void
ep_file_stream_writer_free (FileStreamWriter *file_stream_writer);

bool
ep_file_stream_writer_write (
	FileStreamWriter *file_stream_writer,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written);

/*
 * IpcStreamWriter.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _IpcStreamWriter {
#else
struct _IpcStreamWriter_Internal {
#endif
	StreamWriter stream_writer;
	IpcStream *ipc_stream;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_STREAM_GETTER_SETTER)
struct _IpcStreamWriter {
	uint8_t _internal [sizeof (struct _IpcStreamWriter_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(IpcStreamWriter *, ipc_stream_writer, StreamWriter *, stream_writer)
EP_DEFINE_GETTER(IpcStreamWriter *, ipc_stream_writer, IpcStream *, ipc_stream)

IpcStreamWriter *
ep_ipc_stream_writer_alloc (
	uint64_t id,
	IpcStream *stream);

void
ep_ipc_stream_writer_free (IpcStreamWriter *ipc_stream_writer);

bool
ep_ipc_stream_writer_write (
	IpcStreamWriter *ipc_stream_writer,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_STREAM_H__ */
