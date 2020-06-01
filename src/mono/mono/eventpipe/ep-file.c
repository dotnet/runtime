#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * Forward declares of all static functions.
 */

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
void
file_save_metadata_id (
	EventPipeFile *file,
	EventPipeEvent *ep_event,
	uint32_t metadata_id);

/*
 * EventPipeFile.
 */

static
uint32_t
file_get_stack_id (
	EventPipeFile *file,
	EventPipeEventInstance * event_instance)
{
	EP_ASSERT (file != NULL && event_instance != NULL);
	EP_ASSERT (ep_file_get_format (file) >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);
	EP_ASSERT (ep_file_get_stack_block (file) != NULL);

	uint32_t stack_id = 0;
	EventPipeStackContents *stack_contents = ep_event_instance_get_stack_contents_ref (event_instance);
	EventPipeStackBlock *stack_block = ep_file_get_stack_block (file);
	ep_rt_stack_hash_map_t *stack_hash = ep_file_get_stack_hash_ref (file);
	StackHashEntry *entry = NULL;
	StackHashKey key;
	ep_stack_hash_key_init (&key, stack_contents);
	if (!ep_rt_stack_hash_lookup (stack_hash, &key, &entry)) {
		stack_id = ep_file_get_stack_id_counter (file) + 1;
		ep_file_set_stack_id_counter (file, stack_id);
		entry = ep_stack_hash_entry_alloc (stack_contents, stack_id, ep_stack_hash_key_get_hash (&key));
		if (entry)
			ep_rt_stack_hash_add (stack_hash, ep_stack_hash_entry_get_key_ref (entry), entry);

		if (!ep_stack_block_write_stack (stack_block, stack_id, stack_contents)) {
			// we can't write this stack to the current block (it's full)
			// so we write what we have in the block to the serializer
			ep_file_flush (file, EP_FILE_FLUSH_FLAGS_STACK_BLOCK);
			bool result = ep_stack_block_write_stack (stack_block, stack_id, stack_contents);
			EP_ASSERT (result == true); // we should never fail to add event to a clear block (if we do the max size is too small)
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
	EP_ASSERT (file != NULL && ep_event != NULL);
	EP_ASSERT (ep_file_get_metadata_ids_cref (file) != NULL);

	uint32_t metadata_ids;
	if (ep_rt_metadata_labels_lookup (ep_file_get_metadata_ids_cref (file), ep_event, &metadata_ids)) {
		EP_ASSERT (metadata_ids != 0);
		return metadata_ids;
	}

	return 0;
}

static
uint32_t
file_generate_metadata_id (EventPipeFile *file)
{
	return ep_rt_atomic_inc_uint32_t (ep_file_get_metadata_id_counter_ref (file));
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
	EP_ASSERT (file != NULL && event_instance != NULL);
	EP_ASSERT (ep_file_get_event_block (file) != NULL);
	EP_ASSERT (ep_file_get_metadata_block (file) != NULL);

	ep_event_instance_set_metadata_id (event_instance, metadata_id);

	// If we are flushing events we need to flush metadata and stacks as well
	// to ensure referenced metadata/stacks were written to the file before the
	// event which referenced them.
	EventPipeFileFlushFlags flags = EP_FILE_FLUSH_FLAGS_ALL_BLOCKS;
	EventPipeEventBlockBase *block = (EventPipeEventBlockBase *)ep_file_get_event_block (file);
	if(metadata_id == 0 && ep_file_get_format (file) >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		flags = EP_FILE_FLUSH_FLAGS_METADATA_BLOCK;
		block = (EventPipeEventBlockBase *)ep_file_get_metadata_block (file);
	}

	if (ep_event_block_base_write_event (block, event_instance, capture_thread_id, sequence_number, stack_id, is_sotred_event))
		return; // the block is not full, we added the event and continue

	// we can't write this event to the current block (it's full)
	// so we write what we have in the block to the serializer
	ep_file_flush (file, flags);

	bool result = ep_event_block_base_write_event (block, event_instance, capture_thread_id, sequence_number, stack_id, is_sotred_event);
	EP_ASSERT (result == true); // we should never fail to add event to a clear block (if we do the max size is too small)
}

static
void
file_save_metadata_id (
	EventPipeFile *file,
	EventPipeEvent *ep_event,
	uint32_t metadata_id)
{
	EP_ASSERT (file != NULL && ep_event != NULL);
	EP_ASSERT (metadata_id > 0);
	EP_ASSERT (ep_file_get_metadata_ids_cref (file) != NULL);

	// If a pre-existing metadata label exists, remove it.
	uint32_t old_id;
	if (ep_rt_metadata_labels_lookup (ep_file_get_metadata_ids_cref (file), ep_event, &old_id))
		ep_rt_metadata_labels_remove (ep_file_get_metadata_ids_ref (file), ep_event);

	// Add the metadata label.
	ep_rt_metadata_labels_add (ep_file_get_metadata_ids_ref (file), ep_event, metadata_id);
}

bool
ep_file_initialize_file (EventPipeFile *file)
{
	ep_return_false_if_nok (file != NULL);

	EP_ASSERT (ep_file_get_stream_writer (file) != NULL);
	EP_ASSERT (ep_file_get_fast_serializer (file) == NULL);

	bool success = true;
	if (ep_file_get_format (file) >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
		const ep_char8_t header[] = "Nettrace";
		const uint32_t bytes_to_write = EP_ARRAY_SIZE (header) - 1;
		uint32_t bytes_written = 0;
		success = ep_stream_writer_write (ep_file_get_stream_writer (file), (const uint8_t *)header, bytes_to_write, &bytes_written) && bytes_written == bytes_to_write;
	}

	if (success) {
		// Create the file stream and write the FastSerialization header.
		ep_file_set_fast_serializer (file, ep_fast_serializer_alloc (ep_file_get_stream_writer (file)));

		// Write the first object to the file.
		if (ep_file_get_fast_serializer (file))
			ep_fast_serializer_write_object (ep_file_get_fast_serializer (file), (FastSerializableObject *)file);
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
	ep_return_void_if_nok (file != NULL && event_instance != NULL);
	EP_ASSERT (ep_file_get_fast_serializer (file) != NULL);

	EventPipeEventMetadataEvent *metadata_instance = NULL;

#ifdef EP_CHECKED_BUILD
	EP_ASSERT (ep_event_instance_get_timestamp (event_instance) >= ep_file_get_last_sorted_timestamp (file));
	if (is_sorted_event)
		ep_file_set_last_sorted_timestamp (file, ep_event_instance_get_timestamp (event_instance));
#endif

	uint32_t stack_id = 0;
	if (ep_file_get_format (file) >= EP_SERIALIZATION_FORMAT_NETTRACE_V4)
		stack_id = file_get_stack_id (file, event_instance);

	// Check to see if we've seen this event type before.
	// If not, then write the event metadata to the event stream first.
	unsigned int metadata_id = file_get_metadata_id (file, ep_event_instance_get_ep_event (event_instance));
	if(metadata_id == 0) {
		metadata_id = file_generate_metadata_id (file);

		metadata_instance = ep_build_event_metadata_event (event_instance, metadata_id);
		ep_raise_error_if_nok (metadata_instance != NULL);

		file_write_event_to_block (file, (EventPipeEventInstance *)metadata_instance, 0, 0, 0, 0, true); // metadataId=0 breaks recursion and represents the metadata event.
		file_save_metadata_id (file, ep_event_instance_get_ep_event (event_instance), metadata_id);
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
	ep_return_void_if_nok (file != NULL && sequence_point != NULL);
	EP_ASSERT (ep_file_get_fast_serializer (file) != NULL);

	if (ep_file_get_format (file) < EP_SERIALIZATION_FORMAT_NETTRACE_V4)
		return; // sequence points aren't used in NetPerf format

	ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);
	EventPipeSequencePointBlock sequence_point_block;

	ep_sequence_point_block_init (&sequence_point_block, sequence_point);
	ep_fast_serializer_write_object (ep_file_get_fast_serializer (file), (FastSerializableObject *)&sequence_point_block);
	ep_sequence_point_block_fini (&sequence_point_block);

	// stack cache resets on sequence points
	ep_file_set_stack_id_counter (file, 0);
	ep_rt_stack_hash_remove_all (ep_file_get_stack_hash_ref (file));
}

void
ep_file_flush (
	EventPipeFile *file,
	EventPipeFileFlushFlags flags)
{
	// Write existing buffer to the stream/file regardless of whether it is full or not.
	ep_return_void_if_nok (file != NULL && ep_file_get_fast_serializer (file) != NULL && ep_file_get_metadata_block (file) != NULL &&
		ep_file_get_stack_block (file) != NULL && ep_file_get_event_block (file) != NULL);

	if ((ep_metadata_block_get_bytes_written (ep_file_get_metadata_block (file)) != 0) && ((flags & EP_FILE_FLUSH_FLAGS_METADATA_BLOCK) != 0)) {
		EP_ASSERT (ep_file_get_format (file) >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);
		ep_metadata_block_serialize (ep_file_get_metadata_block (file), ep_file_get_fast_serializer (file));
		ep_metadata_block_clear (ep_file_get_metadata_block (file));
	}

	if ((ep_stack_block_get_bytes_written (ep_file_get_stack_block (file)) != 0) && ((flags & EP_FILE_FLUSH_FLAGS_STACK_BLOCK) != 0)) {
		EP_ASSERT (ep_file_get_format (file) >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);
		ep_stack_block_serialize (ep_file_get_stack_block (file), ep_file_get_fast_serializer (file));
		ep_stack_block_clear (ep_file_get_stack_block (file));
	}

	if ((ep_event_block_get_bytes_written (ep_file_get_event_block (file)) != 0) && ((flags & EP_FILE_FLUSH_FLAGS_EVENT_BLOCK) != 0)) {
		ep_event_block_serialize (ep_file_get_event_block (file), ep_file_get_fast_serializer (file));
		ep_event_block_clear (ep_file_get_event_block (file));
	}
}

int32_t
ep_file_get_file_version (EventPipeSerializationFormat format)
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

int32_t
ep_file_get_file_minimum_version (EventPipeSerializationFormat format)
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

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_file;
const char quiet_linker_empty_file_warning_eventpipe_file = 0;
#endif
