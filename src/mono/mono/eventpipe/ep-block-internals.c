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
block_base_fast_serialize_func (
	void *object,
	FastSerializer *fast_serializer);

static
void
block_base_clear_func (void *object);

static
void
block_base_serialize_header_func (
	void *object,
	FastSerializer *fast_serializer);

static
uint32_t
block_base_get_header_size_func (void *object);

static
const ep_char8_t *
event_block_get_type_name_func (void *object);

static
void
event_block_free_func (void *object);

static
void
metadata_block_free_func (void *object);

static
const ep_char8_t *
metadata_block_get_type_name_func (void *object);

static
int32_t
sequence_point_get_block_size (EventPipeSequencePoint *sequence_point);

static
void
sequence_point_block_free_func (void *object);

static
const ep_char8_t *
sequence_point_block_get_type_name_func (void *object);

static
void
stack_block_free_func (void *object);

static
const ep_char8_t *
stack_block_get_type_name_func (void *object);

static
void
stack_block_clear_func (void *object);

static
uint32_t
stack_block_get_header_size_func (void *object);

static
void
stack_block_serialize_header_func (void *object, FastSerializer *fast_serializer);

/*
 * EventPipeBlock
 */

EventPipeBlock *
ep_block_init (
	EventPipeBlock *block,
	EventPipeBlockVtable *vtable,
	uint32_t max_block_size,
	EventPipeSerializationFormat format)
{
	EP_ASSERT (block != NULL && vtable != NULL);

	ep_raise_error_if_nok (ep_fast_serializable_object_init (
		&block->fast_serializer_object,
		(FastSerializableObjectVtable *)vtable,
		ep_file_get_file_version (format),
		ep_file_get_file_minimum_version (format),
		format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) != NULL);

	block->block = ep_rt_byte_array_alloc (max_block_size);
	ep_raise_error_if_nok (block->block != NULL);

	memset (block->block, 0, max_block_size);
	block->write_pointer = block->block;
	block->end_of_the_buffer = block->block + max_block_size;
	block->format = format;

ep_on_exit:
	return block;

ep_on_error:
	block = NULL;
	ep_exit_error_handler ();
}

void
ep_block_fini (EventPipeBlock *block)
{
	ep_return_void_if_nok (block != NULL);
	ep_rt_byte_array_free (block->block);
}

void
ep_block_clear_vcall (EventPipeBlock *block)
{
	EP_ASSERT (block != NULL && block->fast_serializer_object.vtable != NULL);
	EventPipeBlockVtable *vtable = (EventPipeBlockVtable *)block->fast_serializer_object.vtable;

	EP_ASSERT (vtable->clear_func != NULL);
	vtable->clear_func (block);
}

uint32_t
ep_block_get_header_size_vcall (EventPipeBlock *block)
{
	EP_ASSERT (block != NULL && block->fast_serializer_object.vtable != NULL);
	EventPipeBlockVtable *vtable = (EventPipeBlockVtable *)block->fast_serializer_object.vtable;

	EP_ASSERT (vtable->get_header_size_func != NULL);
	return vtable->get_header_size_func (block);
}

void
ep_block_serialize_header_vcall (
	EventPipeBlock *block,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (block != NULL && block->fast_serializer_object.vtable != NULL);
	EventPipeBlockVtable *vtable = (EventPipeBlockVtable *)block->fast_serializer_object.vtable;

	EP_ASSERT (vtable->serialize_header_func != NULL);
	vtable->serialize_header_func (block, fast_serializer);
}

void
ep_block_fast_serialize_vcall (
	EventPipeBlock *block,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (block != NULL);
	ep_fast_serializable_object_fast_serialize_vcall (&block->fast_serializer_object, fast_serializer);
}

/*
 * EventPipeEventBlockBase
 */

static
void
block_base_fast_serialize_func (
	void *object,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (object != NULL && fast_serializer != NULL);
	ep_block_fast_serialize (&((EventPipeEventBlockBase *)object)->block, fast_serializer);
}

static
void
block_base_clear_func (void *object)
{
	EP_ASSERT (object != NULL);
	ep_event_block_base_clear ((EventPipeEventBlockBase *)object);
}

static
void
block_base_serialize_header_func (
	void *object,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (object != NULL && fast_serializer != NULL);
	ep_event_block_base_serialize_header ((EventPipeEventBlockBase *)object, fast_serializer);
}

static
uint32_t
block_base_get_header_size_func (void *object)
{
	EP_ASSERT (object != NULL);
	return ep_event_block_base_get_header_size ((EventPipeEventBlockBase *)object);
}

EventPipeEventBlockBase *
ep_event_block_base_init (
	EventPipeEventBlockBase *event_block_base,
	EventPipeBlockVtable *vtable,
	uint32_t max_block_size,
	EventPipeSerializationFormat format,
	bool use_header_compression)
{
	EP_ASSERT (event_block_base != NULL && vtable != NULL);

	ep_raise_error_if_nok (ep_block_init (
		&event_block_base->block,
		vtable,
		max_block_size,
		format) != NULL);

	event_block_base->use_header_compression = use_header_compression;

	memset (event_block_base->compressed_header, 0, EP_ARRAY_SIZE (event_block_base->compressed_header));
	ep_event_block_base_clear (event_block_base);

ep_on_exit:
	return event_block_base;

ep_on_error:
	event_block_base = NULL;
	ep_exit_error_handler ();
}

void
ep_event_block_base_fini (EventPipeEventBlockBase *event_block_base)
{
	ep_return_void_if_nok (event_block_base != NULL);
	ep_block_fini (&event_block_base->block);
}

/*
 * EventPipeEventBlock
 */

static
const ep_char8_t *
event_block_get_type_name_func (void *object)
{
	EP_ASSERT (object != NULL);
	return "EventBlock";
}

static
void
event_block_free_func (void *object)
{
	ep_event_block_free ((EventPipeEventBlock *)object);
}

static EventPipeBlockVtable event_block_vtable = {
	{
		event_block_free_func,
		block_base_fast_serialize_func,
		event_block_get_type_name_func },
	block_base_clear_func,
	block_base_get_header_size_func,
	block_base_serialize_header_func };

EventPipeEventBlock *
ep_event_block_alloc (
	uint32_t max_block_size,
	EventPipeSerializationFormat format)
{
	EventPipeEventBlock *instance = ep_rt_object_alloc (EventPipeEventBlock);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_event_block_base_init (
		&instance->event_block_base,
		&event_block_vtable,
		max_block_size,
		format,
		format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_event_block_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_event_block_free (EventPipeEventBlock *event_block)
{
	ep_return_void_if_nok (event_block != NULL);

	ep_event_block_base_fini (&event_block->event_block_base);
	ep_rt_object_free (event_block);
}

/*
 * EventPipeMetadataBlock
 */

static
void
metadata_block_free_func (void *object)
{
	ep_metadata_block_free ((EventPipeMetadataBlock *)object);
}

static
const ep_char8_t *
metadata_block_get_type_name_func (void *object)
{
	EP_ASSERT (object != NULL);
	return "MetadataBlock";
}

static EventPipeBlockVtable metadata_block_vtable = {
	{
		metadata_block_free_func,
		block_base_fast_serialize_func,
		metadata_block_get_type_name_func },
	block_base_clear_func,
	block_base_get_header_size_func,
	block_base_serialize_header_func };

EventPipeMetadataBlock *
ep_metadata_block_alloc (uint32_t max_block_size)
{
	EventPipeMetadataBlock *instance = ep_rt_object_alloc (EventPipeMetadataBlock);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_event_block_base_init (
		&instance->event_block_base,
		&metadata_block_vtable,
		max_block_size,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		true) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_metadata_block_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_metadata_block_free (EventPipeMetadataBlock *metadata_block)
{
	ep_return_void_if_nok (metadata_block != NULL);

	ep_event_block_base_fini (&metadata_block->event_block_base);
	ep_rt_object_free (metadata_block);
}

/*
 * EventPipeSequencePointBlock.
 */

static
int32_t
sequence_point_get_block_size (EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (sequence_point != NULL);

	const uint32_t size_of_sequence_number =
		sizeof (uint64_t) + //thread id
		sizeof (uint32_t); //sequence number

	const uint32_t thread_count = ep_rt_thread_sequence_number_map_count (ep_sequence_point_get_thread_sequence_numbers_cref (sequence_point));

	return sizeof (sequence_point->timestamp) +
		sizeof (uint32_t) + //thread count
		thread_count * size_of_sequence_number;
}

static
void
sequence_point_block_free_func (void *object)
{
	ep_sequence_point_block_free ((EventPipeSequencePointBlock *)object);
}

static
const ep_char8_t *
sequence_point_block_get_type_name_func (void *object)
{
	EP_ASSERT (object != NULL);
	return "SPBlock";
}

static EventPipeBlockVtable sequence_point_block_vtable = {
	{
		sequence_point_block_free_func,
		block_base_fast_serialize_func,
		sequence_point_block_get_type_name_func },
	block_base_clear_func,
	block_base_get_header_size_func,
	block_base_serialize_header_func };

EventPipeSequencePointBlock *
ep_sequence_point_block_alloc (EventPipeSequencePoint *sequence_point)
{
	ep_return_null_if_nok (sequence_point != NULL);

	EventPipeSequencePointBlock *instance = ep_rt_object_alloc (EventPipeSequencePointBlock);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_sequence_point_block_init (instance, sequence_point) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_sequence_point_block_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeSequencePointBlock *
ep_sequence_point_block_init (
	EventPipeSequencePointBlock *sequence_point_block,
	EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (sequence_point_block != NULL);
	ep_return_null_if_nok (sequence_point != NULL);

	ep_raise_error_if_nok (ep_event_block_base_init (
		&sequence_point_block->event_block_base,
		&sequence_point_block_vtable,
		sequence_point_get_block_size (sequence_point),
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		true) != NULL);

	const int64_t timestamp = sequence_point->timestamp;
	memcpy (sequence_point_block->event_block_base.block.write_pointer, &timestamp, sizeof (timestamp));
	sequence_point_block->event_block_base.block.write_pointer += sizeof (timestamp);

	const uint32_t thread_count = ep_rt_thread_sequence_number_map_count (ep_sequence_point_get_thread_sequence_numbers_cref (sequence_point));
	memcpy (sequence_point_block->event_block_base.block.write_pointer, &thread_count, sizeof (thread_count));
	sequence_point_block->event_block_base.block.write_pointer += sizeof (thread_count);

	ep_rt_thread_sequence_number_hash_map_iterator_t iterator;
	for (ep_rt_thread_sequence_number_map_iterator_begin (&sequence_point->thread_sequence_numbers, &iterator);
		!ep_rt_thread_sequence_number_map_iterator_end (&sequence_point->thread_sequence_numbers, &iterator);
		ep_rt_thread_sequence_number_map_iterator_next (&sequence_point->thread_sequence_numbers, &iterator)) {

		const EventPipeThreadSessionState *key = ep_rt_thread_sequence_number_map_iterator_key (&iterator);

		const uint64_t thread_id = ep_thread_get_os_thread_id (ep_thread_session_state_get_thread (key));
		memcpy (sequence_point_block->event_block_base.block.write_pointer, &thread_id, sizeof (thread_id));
		sequence_point_block->event_block_base.block.write_pointer += sizeof (thread_id);

		const uint32_t sequence_number = ep_rt_thread_sequence_number_map_iterator_value (&iterator);
		memcpy (sequence_point_block->event_block_base.block.write_pointer, &sequence_number, sizeof (sequence_number));
		sequence_point_block->event_block_base.block.write_pointer += sizeof (sequence_number);
	}

ep_on_exit:
	return sequence_point_block;

ep_on_error:
	sequence_point_block = NULL;
	ep_exit_error_handler ();
}

void
ep_sequence_point_block_fini (EventPipeSequencePointBlock *sequence_point_block)
{
	ep_return_void_if_nok (sequence_point_block != NULL);
	ep_event_block_base_fini (&sequence_point_block->event_block_base);
}

void
ep_sequence_point_block_free (EventPipeSequencePointBlock *sequence_point_block)
{
	ep_return_void_if_nok (sequence_point_block != NULL);

	ep_sequence_point_block_fini (sequence_point_block);
	ep_rt_object_free (sequence_point_block);
}

/*
 * EventPipeStackBlock.
 */

static
void
stack_block_free_func (void *object)
{
	ep_stack_block_free ((EventPipeStackBlock *)object);
}

static
const ep_char8_t *
stack_block_get_type_name_func (void *object)
{
	EP_ASSERT (object != NULL);
	return "StackBlock";
}

static
void
stack_block_clear_func (void *object)
{
	EP_ASSERT (object != NULL);

	EventPipeStackBlock *stack_block = (EventPipeStackBlock *)object;

	stack_block->has_initial_index = false;
	stack_block->has_initial_index = 0;
	stack_block->count = 0;

	ep_block_clear (&stack_block->event_block_base.block);
}

static
uint32_t
stack_block_get_header_size_func (void *object)
{
	EP_ASSERT (object != NULL);
	return	sizeof (uint32_t) + // start index
			sizeof (uint32_t); // count of indices
}

static
void
stack_block_serialize_header_func (void *object, FastSerializer *fast_serializer)
{
	EP_ASSERT (object != NULL && fast_serializer != NULL);

	EventPipeStackBlock *stack_block = (EventPipeStackBlock *)object;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&stack_block->initial_index, sizeof (stack_block->initial_index));
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&stack_block->count, sizeof (stack_block->count));
}

static EventPipeBlockVtable stack_block_vtable = {
	{
		stack_block_free_func,
		block_base_fast_serialize_func,
		stack_block_get_type_name_func },
	stack_block_clear_func,
	stack_block_get_header_size_func,
	stack_block_serialize_header_func };

EventPipeStackBlock *
ep_stack_block_alloc (uint32_t max_block_size)
{
	EventPipeStackBlock *instance = ep_rt_object_alloc (EventPipeStackBlock);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_event_block_base_init (
		&instance->event_block_base,
		&stack_block_vtable,
		max_block_size,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		true) != NULL);

	stack_block_clear_func (instance);

ep_on_exit:
	return instance;

ep_on_error:
	ep_stack_block_free (instance);

	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_stack_block_free (EventPipeStackBlock *stack_block)
{
	ep_return_void_if_nok (stack_block != NULL);

	ep_event_block_base_fini (&stack_block->event_block_base);
	ep_rt_object_free (stack_block);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_block_internals;
const char quiet_linker_empty_file_warning_eventpipe_block_internals = 0;
#endif
