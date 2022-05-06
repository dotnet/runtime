#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_FILE_GETTER_SETTER
#include "ep.h"
#include "ep-block.h"
#include "ep-config.h"
#include "ep-event-instance.h"
#include "ep-file.h"
#include "ep-sample-profiler.h"
#include "ep-rt.h"

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
void
stack_hash_value_free_func (void *entry);

static
uint32_t
file_get_stack_id (
	EventPipeFile *file,
	EventPipeEventInstance *event_instance);

static
uint32_t
file_generate_metadata_id (EventPipeFile *file);

static
uint32_t
file_get_metadata_id (
	EventPipeFile *file,
	EventPipeEvent *ep_event);

static
void
file_write_event_to_block (
	EventPipeFile *file,
	EventPipeEventInstance *event_instance,
	uint32_t metadata_id,
	uint64_t capture_thread_id,
	uint32_t sequence_number,
	uint32_t stack_id,
	bool is_sotred_event);

static
bool
file_save_metadata_id (
	EventPipeFile *file,
	EventPipeEvent *ep_event,
	uint32_t metadata_id);

static
uint32_t
file_get_file_version (EventPipeSerializationFormat format);

static
uint32_t
file_get_file_minimum_version (EventPipeSerializationFormat format);

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
	EP_ASSERT (object != NULL);
	EP_ASSERT (fast_serializer != NULL);

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
void
stack_hash_value_free_func (void *entry)
{
	ep_stack_hash_entry_free ((StackHashEntry *)entry);
}

static
void
file_write_end (EventPipeFile *file)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (file->fast_serializer != NULL);

	ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);

	// "After the last EventBlock is emitted, the stream is ended by emitting a NullReference Tag which indicates that there are no more objects in the stream to read."
	// see https://github.com/Microsoft/perfview/blob/master/src/TraceEvent/EventPipe/EventPipeFormat.md for more
	ep_fast_serializer_write_tag (file->fast_serializer, FAST_SERIALIZER_TAGS_NULL_REFERENCE, NULL, 0);
}

static
uint32_t
file_get_stack_id (
	EventPipeFile *file,
	EventPipeEventInstance * event_instance)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (event_instance != NULL);
	EP_ASSERT (file->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);
	EP_ASSERT (file->stack_block != NULL);

	uint32_t stack_id = 0;
	EventPipeStackContents *stack_contents = ep_event_instance_get_stack_contents_ref (event_instance);
	EventPipeStackBlock *stack_block = file->stack_block;
	ep_rt_stack_hash_map_t *stack_hash = &file->stack_hash;
	StackHashEntry *entry = NULL;
	StackHashKey key;
	ep_stack_hash_key_init (&key, stack_contents);
	if (!ep_rt_stack_hash_lookup (stack_hash, &key, &entry)) {
		stack_id = file->stack_id_counter + 1;
		file->stack_id_counter = stack_id;
		entry = ep_stack_hash_entry_alloc (stack_contents, stack_id, ep_stack_hash_key_get_hash (&key));
		if (entry) {
			if (!ep_rt_stack_hash_add (stack_hash, ep_stack_hash_entry_get_key_ref (entry), entry))
				ep_stack_hash_entry_free (entry);
			entry = NULL;
		}

		if (!ep_stack_block_write_stack (stack_block, stack_id, stack_contents)) {
			// we can't write this stack to the current block (it's full)
			// so we write what we have in the block to the serializer
			ep_file_flush (file, EP_FILE_FLUSH_FLAGS_STACK_BLOCK);
			bool result = ep_stack_block_write_stack (stack_block, stack_id, stack_contents);
			if (!result)
				EP_UNREACHABLE ("Should never fail to add event to a clear block. If we do the max size is too small.");
		}
	} else {
		stack_id = ep_stack_hash_entry_get_id (entry);
	}

	ep_stack_hash_key_fini (&key);
	return stack_id;
}

static
uint32_t
file_get_metadata_id (
	EventPipeFile *file,
	EventPipeEvent *ep_event)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (ep_event != NULL);

	uint32_t metadata_ids;
	if (ep_rt_metadata_labels_hash_lookup (&file->metadata_ids, ep_event, &metadata_ids)) {
		EP_ASSERT (metadata_ids != 0);
		return metadata_ids;
	}

	return 0;
}

static
uint32_t
file_generate_metadata_id (EventPipeFile *file)
{
	return ep_rt_atomic_inc_uint32_t (&file->metadata_id_counter);
}

static
void
file_write_event_to_block (
	EventPipeFile *file,
	EventPipeEventInstance *event_instance,
	uint32_t metadata_id,
	uint64_t capture_thread_id,
	uint32_t sequence_number,
	uint32_t stack_id,
	bool is_sotred_event)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (event_instance != NULL);
	EP_ASSERT (file->event_block != NULL);
	EP_ASSERT (file->metadata_block != NULL);

	ep_event_instance_set_metadata_id (event_instance, metadata_id);

	// If we are flushing events we need to flush metadata and stacks as well
	// to ensure referenced metadata/stacks were written to the file before the
	// event which referenced them.
	EventPipeFileFlushFlags flags = EP_FILE_FLUSH_FLAGS_ALL_BLOCKS;
	EventPipeEventBlockBase *block = (EventPipeEventBlockBase *)file->event_block;
	if(metadata_id == 0 && file->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		flags = EP_FILE_FLUSH_FLAGS_METADATA_BLOCK;
		block = (EventPipeEventBlockBase *)file->metadata_block;
	}

	if (ep_event_block_base_write_event (block, event_instance, capture_thread_id, sequence_number, stack_id, is_sotred_event))
		return; // the block is not full, we added the event and continue

	// we can't write this event to the current block (it's full)
	// so we write what we have in the block to the serializer
	ep_file_flush (file, flags);

	bool result = ep_event_block_base_write_event (block, event_instance, capture_thread_id, sequence_number, stack_id, is_sotred_event);
	if (!result)
		EP_UNREACHABLE ("Should never fail to add event to a clear block. If we do the max size is too small.");
}

static
bool
file_save_metadata_id (
	EventPipeFile *file,
	EventPipeEvent *ep_event,
	uint32_t metadata_id)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (ep_event != NULL);
	EP_ASSERT (metadata_id > 0);

	// If a pre-existing metadata label exists, remove it.
	uint32_t old_id;
	if (ep_rt_metadata_labels_hash_lookup (&file->metadata_ids, ep_event, &old_id))
		ep_rt_metadata_labels_hash_remove (&file->metadata_ids, ep_event);

	// Add the metadata label.
	return ep_rt_metadata_labels_hash_add (&file->metadata_ids, ep_event, metadata_id);
}

static
uint32_t
file_get_file_version (EventPipeSerializationFormat format)
{
	switch (format) {
	case EP_SERIALIZATION_FORMAT_NETPERF_V3 :
		return 3;
	case EP_SERIALIZATION_FORMAT_NETTRACE_V4 :
		return 4;
	default :
		EP_ASSERT (!"Unrecognized EventPipeSerializationFormat");
		return 0;
	}
}

static
uint32_t
file_get_file_minimum_version (EventPipeSerializationFormat format)
{
	switch (format) {
	case EP_SERIALIZATION_FORMAT_NETPERF_V3 :
		return 0;
	case EP_SERIALIZATION_FORMAT_NETTRACE_V4 :
		return 4;
	default :
		EP_ASSERT (!"Unrecognized EventPipeSerializationFormat");
		return 0;
	}
}

EventPipeFile *
ep_file_alloc (
	StreamWriter *stream_writer,
	EventPipeSerializationFormat format)
{
	EventPipeFile *instance = ep_rt_object_alloc (EventPipeFile);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_fast_serializable_object_init (
		&instance->fast_serializable_object,
		&file_vtable,
		file_get_file_version (format),
		file_get_file_minimum_version (format),
		format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) != NULL);

	instance->stream_writer = stream_writer;
	instance->format = format;

	instance->event_block = ep_event_block_alloc (100 * 1024, format);
	ep_raise_error_if_nok (instance->event_block != NULL);

	instance->metadata_block = ep_metadata_block_alloc (100 * 1024);
	ep_raise_error_if_nok (instance->metadata_block);

	instance->stack_block = ep_stack_block_alloc (100 * 1024);
	ep_raise_error_if_nok (instance->stack_block != NULL);

	// File start time information.
	ep_system_time_get (&instance->file_open_system_time);
	instance->file_open_timestamp = ep_perf_timestamp_get ();
	instance->timestamp_frequency = ep_perf_frequency_query ();

	instance->pointer_size = sizeof (void*);
	instance->current_process_id = ep_rt_current_process_get_id ();
	instance->number_of_processors = ep_rt_processors_get_count ();

	instance->sampling_rate_in_ns = (uint32_t)ep_sample_profiler_get_sampling_rate ();

	ep_rt_metadata_labels_hash_alloc (&instance->metadata_ids, NULL, NULL, NULL, NULL);
	ep_raise_error_if_nok (ep_rt_metadata_labels_hash_is_valid (&instance->metadata_ids));

	ep_rt_stack_hash_alloc (&instance->stack_hash, ep_stack_hash_key_hash, ep_stack_hash_key_equal, NULL, stack_hash_value_free_func);
	ep_raise_error_if_nok (ep_rt_stack_hash_is_valid (&instance->stack_hash));

	// Start at 0 - The value is always incremented prior to use, so the first ID will be 1.
	ep_rt_volatile_store_uint32_t (&instance->metadata_id_counter, 0);

	// Start at 0 - The value is always incremented prior to use, so the first ID will be 1.
	instance->stack_id_counter = 0;

	ep_rt_volatile_store_uint32_t (&instance->initialized, 0);

#ifdef EP_CHECKED_BUILD
	instance->last_sorted_timestamp = ep_perf_timestamp_get ();
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
	ep_rt_metadata_labels_hash_free (&file->metadata_ids);
	ep_rt_stack_hash_free (&file->stack_hash);

	// If file has not been initialized, stream_writer ownership
	// have not been passed along and needs to be freed by file.
	if (ep_rt_volatile_load_uint32_t (&file->initialized) == 0)
		ep_stream_writer_free_vcall (file->stream_writer);

	ep_fast_serializable_object_fini (&file->fast_serializable_object);
	ep_rt_object_free (file);
}

bool
ep_file_initialize_file (EventPipeFile *file)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (file->stream_writer != NULL);
	EP_ASSERT (file->fast_serializer == NULL);

	bool success = true;
	if (file->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		const ep_char8_t header[] = "Nettrace";
		const uint32_t bytes_to_write = (uint32_t)(STRING_LENGTH (header));
		uint32_t bytes_written = 0;
		success = ep_stream_writer_write (file->stream_writer, (const uint8_t *)header, bytes_to_write, &bytes_written) && bytes_written == bytes_to_write;
	}

	if (success) {
		ep_rt_volatile_store_uint32_t (&file->initialized, 1);
		// Create the file stream and write the FastSerialization header.
		file->fast_serializer = ep_fast_serializer_alloc (file->stream_writer);

		// Write the first object to the file.
		if (file->fast_serializer)
			ep_fast_serializer_write_object (file->fast_serializer, (FastSerializableObject *)file);
	}

	return success;
}

void
ep_file_write_event (
	EventPipeFile *file,
	EventPipeEventInstance *event_instance,
	uint64_t capture_thread_id,
	uint32_t sequence_number,
	bool is_sorted_event)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (event_instance != NULL);

	ep_return_void_if_nok (!ep_file_has_errors (file));

	EventPipeEventMetadataEvent *metadata_instance = NULL;

#ifdef EP_CHECKED_BUILD
	EP_ASSERT (ep_event_instance_get_timestamp (event_instance) >= file->last_sorted_timestamp);
	if (is_sorted_event)
		file->last_sorted_timestamp = ep_event_instance_get_timestamp (event_instance);
#endif

	uint32_t stack_id = 0;
	if (file->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4)
		stack_id = file_get_stack_id (file, event_instance);

	// Check to see if we've seen this event type before.
	// If not, then write the event metadata to the event stream first.
	unsigned int metadata_id = file_get_metadata_id (file, ep_event_instance_get_ep_event (event_instance));
	if(metadata_id == 0) {
		metadata_id = file_generate_metadata_id (file);

		metadata_instance = ep_build_event_metadata_event (event_instance, metadata_id);
		ep_raise_error_if_nok (metadata_instance != NULL);

		file_write_event_to_block (file, (EventPipeEventInstance *)metadata_instance, 0, 0, 0, 0, true); // metadataId=0 breaks recursion and represents the metadata event.
		ep_raise_error_if_nok (file_save_metadata_id (file, ep_event_instance_get_ep_event (event_instance), metadata_id));
	}

	file_write_event_to_block (file, event_instance, metadata_id, capture_thread_id, sequence_number, stack_id, is_sorted_event);

ep_on_exit:
	ep_event_metdata_event_free (metadata_instance);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_file_write_sequence_point (
	EventPipeFile *file,
	EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (file != NULL);
	EP_ASSERT (sequence_point != NULL);

	if (file->format < EP_SERIALIZATION_FORMAT_NETTRACE_V4)
		return; // sequence points aren't used in NetPerf format

	ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);
	ep_raise_error_if_nok (!ep_file_has_errors (file));

	EP_ASSERT (file->fast_serializer != NULL);

	EventPipeSequencePointBlock sequence_point_block;
	ep_sequence_point_block_init (&sequence_point_block, sequence_point);
	ep_fast_serializer_write_object (file->fast_serializer, (FastSerializableObject *)&sequence_point_block);
	ep_sequence_point_block_fini (&sequence_point_block);

	// stack cache resets on sequence points
	file->stack_id_counter = 0;
	ep_rt_stack_hash_remove_all (&file->stack_hash);

ep_on_exit:
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_file_flush (
	EventPipeFile *file,
	EventPipeFileFlushFlags flags)
{
	// Write existing buffer to the stream/file regardless of whether it is full or not.
	EP_ASSERT (file != NULL);
	EP_ASSERT (file->metadata_block != NULL);
	EP_ASSERT (file->stack_block != NULL);
	EP_ASSERT (file->event_block != NULL);

	ep_return_void_if_nok (!ep_file_has_errors (file));

	// we write current blocks to the disk, whether they are full or not
	if ((ep_metadata_block_get_bytes_written (file->metadata_block) != 0) && ((flags & EP_FILE_FLUSH_FLAGS_METADATA_BLOCK) != 0)) {
		EP_ASSERT (file->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);
		ep_metadata_block_serialize (file->metadata_block, file->fast_serializer);
		ep_metadata_block_clear (file->metadata_block);
	}

	if ((ep_stack_block_get_bytes_written (file->stack_block) != 0) && ((flags & EP_FILE_FLUSH_FLAGS_STACK_BLOCK) != 0)) {
		EP_ASSERT (file->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);
		ep_stack_block_serialize (file->stack_block, file->fast_serializer);
		ep_stack_block_clear (file->stack_block);
	}

	if ((ep_event_block_get_bytes_written (file->event_block) != 0) && ((flags & EP_FILE_FLUSH_FLAGS_EVENT_BLOCK) != 0)) {
		ep_event_block_serialize (file->event_block, file->fast_serializer);
		ep_event_block_clear (file->event_block);
	}
}

/*
 * StackHashEntry.
 */

StackHashKey *
ep_stack_hash_entry_get_key (StackHashEntry *stack_hash_entry)
{
	return ep_stack_hash_entry_get_key_ref (stack_hash_entry);
}

StackHashEntry *
ep_stack_hash_entry_alloc (
	const EventPipeStackContents *stack_contents,
	uint32_t id,
	uint32_t hash)
{
	EP_ASSERT (stack_contents != NULL);

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
	EP_ASSERT (stack_contents != NULL);

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

uint32_t
ep_stack_hash_key_hash (const void *key)
{
	EP_ASSERT (key != NULL);
	return ((const StackHashKey *)key)->hash;
}

bool
ep_stack_hash_key_equal (const void *key1, const void *key2)
{
	EP_ASSERT (key1 != NULL);
	EP_ASSERT (key2 != NULL);

	const StackHashKey * stack_hash_key1 = (const StackHashKey *)key1;
	const StackHashKey * stack_hash_key2 = (const StackHashKey *)key2;

	return stack_hash_key1->stack_size_in_bytes == stack_hash_key2->stack_size_in_bytes &&
		!memcmp (stack_hash_key1->stack_bytes, stack_hash_key2->stack_bytes, stack_hash_key1->stack_size_in_bytes);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_file;
const char quiet_linker_empty_file_warning_eventpipe_file = 0;
#endif
