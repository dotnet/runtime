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
file_fast_serialize_func (void *object, FastSerializer *fast_serializer);

static
const ep_char8_t *
file_get_type_name_func (void *object);

static
void
file_free_func (void *object);

static
uint32_t
stack_hash_key_hash_func (const void *key);

static
bool
stack_hash_key_eq_func (const void *key1, const void *key2);

static
void
stack_hash_value_free_func (void *entry);

/*
 * EventPipeFile.
 */

static
void
file_free_func (void *object)
{
	ep_file_free ((EventPipeFile*)object);
}

static
void
file_fast_serialize_func (void *object, FastSerializer *fast_serializer)
{
	EP_ASSERT (object != NULL && fast_serializer != NULL);

	EventPipeFile *file = (EventPipeFile *)object;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&file->file_open_system_time, sizeof (file->file_open_system_time));
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&file->file_open_timestamp, sizeof (file->file_open_timestamp));
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&file->timestamp_frequency, sizeof (file->timestamp_frequency));

	// the beginning of V3
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&file->pointer_size, sizeof (file->pointer_size));
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&file->current_process_id, sizeof (file->current_process_id));
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&file->number_of_processors, sizeof (file->number_of_processors));
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&file->sampling_rate_in_ns, sizeof (file->sampling_rate_in_ns));
}

static
const ep_char8_t *
file_get_type_name_func (void *object)
{
	EP_ASSERT (object != NULL);
	return "Trace";
}

static
FastSerializableObjectVtable
file_vtable = {
	file_free_func,
	file_fast_serialize_func,
	file_get_type_name_func };

static
uint32_t
stack_hash_key_hash_func (const void *key)
{
	EP_ASSERT (key != NULL);
	return ((const StackHashKey *)key)->hash;
}

static
bool
stack_hash_key_eq_func (const void *key1, const void *key2)
{
	EP_ASSERT (key1 != NULL && key2 != NULL);

	const StackHashKey * stack_hash_key1 = (const StackHashKey *)key1;
	const StackHashKey * stack_hash_key2 = (const StackHashKey *)key2;

	return stack_hash_key1->stack_size_in_bytes == stack_hash_key2->stack_size_in_bytes &&
		!memcmp (stack_hash_key1->stack_bytes, stack_hash_key2->stack_bytes, stack_hash_key1->stack_size_in_bytes);
}

static
void
stack_hash_value_free_func (void *entry)
{
	ep_stack_hash_entry_free ((StackHashEntry *)entry);
}

static
void
file_write_end (EventPipeFile *file)
{
	EP_ASSERT (file != NULL && ep_file_get_fast_serializer (file) != NULL);

	ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);

	// "After the last EventBlock is emitted, the stream is ended by emitting a NullReference Tag which indicates that there are no more objects in the stream to read."
	// see https://github.com/Microsoft/perfview/blob/master/src/TraceEvent/EventPipe/EventPipeFormat.md for more
	ep_fast_serializer_write_tag (ep_file_get_fast_serializer (file), FAST_SERIALIZER_TAGS_NULL_REFERENCE, NULL, 0);
}

EventPipeFile *
ep_file_alloc (
	StreamWriter *stream_writer,
	EventPipeSerializationFormat format)
{
	EventPipeFile *instance = ep_rt_object_alloc (EventPipeFile);
	ep_raise_error_if_nok (instance != NULL);

	ep_fast_serializable_object_init (
		&instance->fast_serializable_object,
		&file_vtable,
		ep_file_get_file_version (format),
		ep_file_get_file_minimum_version (format),
		format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);

	instance->stream_writer = stream_writer;
	instance->format = format;

	instance->event_block = ep_event_block_alloc (100 * 1024, format);
	ep_raise_error_if_nok (instance->event_block != NULL);

	instance->metadata_block = ep_metadata_block_alloc (100 * 1024);
	ep_raise_error_if_nok (instance->metadata_block);

	instance->stack_block = ep_stack_block_alloc (100 * 1024);
	ep_raise_error_if_nok (instance->stack_block != NULL);

	// File start time information.
	instance->file_open_system_time = ep_rt_system_time_get ();
	instance->file_open_timestamp = ep_perf_counter_query ();
	instance->timestamp_frequency = ep_perf_frequency_query ();

	instance->pointer_size = SIZEOF_VOID_P;
	instance->current_process_id = ep_rt_current_process_get_id ();
	instance->number_of_processors = ep_rt_processors_get_count ();

	instance->sampling_rate_in_ns = ep_rt_sample_profiler_get_sampling_rate ();

	ep_rt_metadata_labels_alloc (&instance->metadata_ids, NULL, NULL, NULL, NULL);
	ep_raise_error_if_nok (instance->metadata_ids.table);

	ep_rt_stack_hash_alloc (&instance->stack_hash, stack_hash_key_hash_func, stack_hash_key_eq_func, NULL, stack_hash_value_free_func);
	ep_raise_error_if_nok (instance->stack_hash.table);

	// Start at 0 - The value is always incremented prior to use, so the first ID will be 1.
	ep_rt_volatile_store_uint32_t (&instance->metadata_id_counter, 0);

	// Start at 0 - The value is always incremented prior to use, so the first ID will be 1.
	instance->stack_id_counter = 0;

#ifdef EP_CHECKED_BUILD
	instance->last_sorted_timestamp = ep_perf_counter_query ();
#endif

ep_on_exit:
	return instance;

ep_on_error:
	ep_file_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_file_free (EventPipeFile *file)
{
	ep_return_void_if_nok (file != NULL);

	if (file->event_block != NULL && file->fast_serializer != NULL)
		file_write_end (file);

	ep_event_block_free (file->event_block);
	ep_metadata_block_free (file->metadata_block);
	ep_stack_block_free (file->stack_block);
	ep_fast_serializer_free (file->fast_serializer);
	ep_rt_metadata_labels_free (&file->metadata_ids);
	ep_rt_stack_hash_free (&file->stack_hash);

	// If there's no fast_serializer, stream_writer ownership
	// have not been passed along and needs to be freed by file.
	if (!file->fast_serializer)
		ep_stream_writer_free_vcall (file->stream_writer);

	ep_fast_serializable_object_fini (&file->fast_serializable_object);
	ep_rt_object_free (file);
}

/*
 * StackHashEntry.
 */

StackHashEntry *
ep_stack_hash_entry_alloc (
	const EventPipeStackContents *stack_contents,
	uint32_t id,
	uint32_t hash)
{
	ep_return_null_if_nok (stack_contents != NULL);

	uint32_t stack_size = ep_stack_contents_get_size (stack_contents);
	StackHashEntry *entry = (StackHashEntry *)ep_rt_byte_array_alloc (offsetof (StackHashEntry, stack_bytes) + stack_size);
	ep_raise_error_if_nok (entry != NULL);

	entry->id = id;
	entry->key.hash = hash;
	entry->key.stack_size_in_bytes = stack_size;
	entry->key.stack_bytes = entry->stack_bytes;
	memcpy (entry->stack_bytes, ep_stack_contents_get_pointer (stack_contents), stack_size);

ep_on_exit:
	return entry;

ep_on_error:
	ep_stack_hash_entry_free (entry);

	entry = NULL;
	ep_exit_error_handler ();
}

void
ep_stack_hash_entry_free (StackHashEntry *stack_hash_entry)
{
	ep_return_void_if_nok (stack_hash_entry != NULL);
	ep_rt_byte_array_free ((uint8_t *)stack_hash_entry);
}

/*
 * StackHashKey.
 */

static
inline
uint32_t
hash_bytes (const uint8_t *data, size_t data_len)
{
	EP_ASSERT (data != NULL);

	uint32_t hash = 5381;
	const uint8_t *data_end = data + data_len;
	for (/**/ ; data < data_end; data++)
		hash = ((hash << 5) + hash) ^ *data;
	return hash;
}

StackHashKey *
ep_stack_hash_key_init (
	StackHashKey *key,
	const EventPipeStackContents *stack_contents)
{
	EP_ASSERT (key != NULL);
	ep_return_null_if_nok (stack_contents != NULL);

	key->stack_bytes = ep_stack_contents_get_pointer (stack_contents);
	key->stack_size_in_bytes = ep_stack_contents_get_size (stack_contents);
	key->hash = hash_bytes (key->stack_bytes, key->stack_size_in_bytes);

	return key;
}

void
ep_stack_hash_key_fini (StackHashKey *key)
{
	;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_file_internals;
const char quiet_linker_empty_file_warning_eventpipe_file_internals = 0;
#endif
