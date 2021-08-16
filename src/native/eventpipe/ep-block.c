#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_BLOCK_GETTER_SETTER
#include "ep-block.h"
#include "ep-event-instance.h"
#include "ep-file.h"
#include "ep-rt.h"

/*
 * Forward declares of all static functions.
 */

static
int32_t
block_get_file_version (EventPipeSerializationFormat format);

static
int32_t
block_get_file_minimum_version (EventPipeSerializationFormat format);

static
void
block_fast_serialize_func (
	void *object,
	FastSerializer *fast_serializer);

static
void
block_clear_func (void *object);

static
void
block_serialize_header_func (
	void *object,
	FastSerializer *fast_serializer);

static
uint32_t
block_get_header_size_func (void *object);

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
uint8_t *
event_block_base_write_var_uint32 (
	uint8_t * write_pointer,
	uint32_t value);

static
uint8_t *
event_block_base_write_var_uint64 (
	uint8_t * write_pointer,
	uint64_t value);

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
sequence_point_block_fini (EventPipeSequencePointBlock *sequence_point_block);

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

static
uint32_t
block_get_block_version (EventPipeSerializationFormat format)
{
	switch (format) {
	case EP_SERIALIZATION_FORMAT_NETPERF_V3 :
		return 1;
	case EP_SERIALIZATION_FORMAT_NETTRACE_V4 :
		return 2;
	default :
		EP_ASSERT (!"Unrecognized EventPipeSerializationFormat");
		return 0;
	}
}

static
uint32_t
block_get_block_minimum_version (EventPipeSerializationFormat format)
{
	switch (format) {
	case EP_SERIALIZATION_FORMAT_NETPERF_V3 :
		return 0;
	case EP_SERIALIZATION_FORMAT_NETTRACE_V4 :
		return 2;
	default :
		EP_ASSERT (!"Unrecognized EventPipeSerializationFormat");
		return 0;
	}
}

static
void
block_fast_serialize_func (
	void *object,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (object != NULL);
	EP_ASSERT (fast_serializer != NULL);

	ep_block_fast_serialize ((EventPipeBlock *)object, fast_serializer);
}

static
void
block_clear_func (void *object)
{
	EP_ASSERT (object != NULL);
	ep_block_clear ((EventPipeBlock *)object);
}

static
void
block_serialize_header_func (
	void *object,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (object != NULL);
	EP_ASSERT (fast_serializer != NULL);
}

static
uint32_t
block_get_header_size_func (void *object)
{
	EP_ASSERT (object != NULL);
	return 0;
}

EventPipeBlock *
ep_block_init (
	EventPipeBlock *block,
	EventPipeBlockVtable *vtable,
	uint32_t max_block_size,
	EventPipeSerializationFormat format)
{
	EP_ASSERT (block != NULL);
	EP_ASSERT (vtable != NULL);

	ep_raise_error_if_nok (ep_fast_serializable_object_init (
		&block->fast_serializer_object,
		(FastSerializableObjectVtable *)vtable,
		block_get_block_version (format),
		block_get_block_minimum_version (format),
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
	ep_block_fini (block);
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
	EP_ASSERT (block != NULL);
	EP_ASSERT (ep_fast_serializable_object_get_vtable (&block->fast_serializer_object) != NULL);

	EventPipeBlockVtable *vtable = (EventPipeBlockVtable *)ep_fast_serializable_object_get_vtable (&block->fast_serializer_object);

	EP_ASSERT (vtable->clear_func != NULL);
	vtable->clear_func (block);
}

uint32_t
ep_block_get_header_size_vcall (EventPipeBlock *block)
{
	EP_ASSERT (block != NULL);
	EP_ASSERT (ep_fast_serializable_object_get_vtable (&block->fast_serializer_object) != NULL);

	EventPipeBlockVtable *vtable = (EventPipeBlockVtable *)ep_fast_serializable_object_get_vtable (&block->fast_serializer_object);

	EP_ASSERT (vtable->get_header_size_func != NULL);
	return vtable->get_header_size_func (block);
}

void
ep_block_serialize_header_vcall (
	EventPipeBlock *block,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (block != NULL);
	EP_ASSERT (ep_fast_serializable_object_get_vtable (&block->fast_serializer_object) != NULL);

	EventPipeBlockVtable *vtable = (EventPipeBlockVtable *)ep_fast_serializable_object_get_vtable (&block->fast_serializer_object);

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

void
ep_block_clear (EventPipeBlock *block)
{
	EP_ASSERT (block != NULL);

	ep_return_void_if_nok (block->block != NULL);

	EP_ASSERT (block->write_pointer <= block->end_of_the_buffer);

	memset (block->block, 0, block->end_of_the_buffer - block->block);
	block->write_pointer = block->block;
}

uint32_t
ep_block_get_header_size (EventPipeBlock *block)
{
	return 0;
}

void
ep_block_serialize_header (
	EventPipeBlock *block,
	FastSerializer *fast_serializer)
{
	;
}

void
ep_block_fast_serialize (
	EventPipeBlock *block,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (block != NULL);
	EP_ASSERT (fast_serializer != NULL);

	ep_return_void_if_nok (block->block != NULL);

	uint32_t data_size = ep_block_get_bytes_written (block);
	EP_ASSERT (data_size != 0);

	uint32_t header_size =  ep_block_get_header_size_vcall (block);
	uint32_t total_size = data_size + header_size;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&total_size, sizeof (total_size));

	uint32_t required_padding = ep_fast_serializer_get_required_padding (fast_serializer);
	if (required_padding != 0) {
		uint8_t max_padding [FAST_SERIALIZER_ALIGNMENT_SIZE - 1] = { 0 }; // it's longest possible padding, we are going to use only part of it

		EP_ASSERT (required_padding <= FAST_SERIALIZER_ALIGNMENT_SIZE - 1);
		ep_fast_serializer_write_buffer (fast_serializer, max_padding, required_padding); // we write zeros here, the reader is going to always read from the first aligned address of the serialized content

		EP_ASSERT (ep_fast_serializer_get_write_error_encountered (fast_serializer) || (ep_fast_serializer_get_required_padding (fast_serializer) == 0));
	}

	ep_block_serialize_header_vcall (block, fast_serializer);
	ep_fast_serializer_write_buffer (fast_serializer, block->block, data_size);
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
	EP_ASSERT (object != NULL);
	EP_ASSERT (fast_serializer != NULL);

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
	EP_ASSERT (object != NULL);
	EP_ASSERT (fast_serializer != NULL);
	ep_event_block_base_serialize_header ((EventPipeEventBlockBase *)object, fast_serializer);
}

static
uint32_t
block_base_get_header_size_func (void *object)
{
	EP_ASSERT (object != NULL);
	return ep_event_block_base_get_header_size ((EventPipeEventBlockBase *)object);
}

static
uint8_t *
event_block_base_write_var_uint32 (
	uint8_t * write_pointer,
	uint32_t value)
{
	EP_ASSERT (write_pointer != NULL);

	while (value >= 0x80) {
		*write_pointer = (uint8_t)(value | 0x80);
		write_pointer++;
		value >>= 7;
	}
	*write_pointer = (uint8_t)value;
	write_pointer++;
	return write_pointer;
}

static
uint8_t *
event_block_base_write_var_uint64 (
	uint8_t * write_pointer,
	uint64_t value)
{
	EP_ASSERT (write_pointer != NULL);

	while (value >= 0x80) {
		*write_pointer = (uint8_t)(value | 0x80);
		write_pointer++;
		value >>= 7;
	}
	*write_pointer = (uint8_t)value;
	write_pointer++;
	return write_pointer;
}

EventPipeEventBlockBase *
ep_event_block_base_init (
	EventPipeEventBlockBase *event_block_base,
	EventPipeBlockVtable *vtable,
	uint32_t max_block_size,
	EventPipeSerializationFormat format,
	bool use_header_compression)
{
	EP_ASSERT (event_block_base != NULL);
	EP_ASSERT (vtable != NULL);

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
	ep_event_block_base_fini (event_block_base);
	event_block_base = NULL;
	ep_exit_error_handler ();
}

void
ep_event_block_base_fini (EventPipeEventBlockBase *event_block_base)
{
	ep_return_void_if_nok (event_block_base != NULL);
	ep_block_fini (&event_block_base->block);
}

void
ep_event_block_base_clear (EventPipeEventBlockBase *event_block_base)
{
	EP_ASSERT (event_block_base != NULL);

	ep_block_clear (&event_block_base->block);
	memset (&event_block_base->last_header, 0, sizeof (EventPipeEventHeader));
	event_block_base->min_timestamp = INT64_MAX;
	event_block_base->max_timestamp = INT64_MIN;
}

uint32_t
ep_event_block_base_get_header_size (const EventPipeEventBlockBase *event_block_base)
{
	EP_ASSERT (event_block_base != NULL);

	ep_return_zero_if_nok (((EventPipeBlock *)event_block_base)->format != EP_SERIALIZATION_FORMAT_NETPERF_V3);

	return	sizeof(uint16_t) + // header size
			sizeof(uint16_t) + // flags
			sizeof(ep_timestamp_t)  + // min timestamp
			sizeof(ep_timestamp_t);   // max timestamp
}

void
ep_event_block_base_serialize_header (
	EventPipeEventBlockBase *event_block_base,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (event_block_base != NULL);
	EP_ASSERT (fast_serializer != NULL);

	ep_return_void_if_nok (((EventPipeBlock *)event_block_base)->format != EP_SERIALIZATION_FORMAT_NETPERF_V3);

	const uint16_t header_size = (uint16_t)ep_block_get_header_size_vcall ((EventPipeBlock *)event_block_base);
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&header_size, sizeof (header_size));

	const uint16_t flags = event_block_base->use_header_compression ? 1 : 0;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&flags, sizeof (flags));

	ep_timestamp_t min_timestamp = event_block_base->min_timestamp;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&min_timestamp, sizeof (min_timestamp));

	ep_timestamp_t max_timestamp = event_block_base->max_timestamp;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&max_timestamp, sizeof (max_timestamp));
}

bool
ep_event_block_base_write_event (
	EventPipeEventBlockBase *event_block_base,
	EventPipeEventInstance *event_instance,
	uint64_t capture_thread_id,
	uint32_t sequence_number,
	uint32_t stack_id,
	bool is_sorted_event)
{
	bool result = true;

	EP_ASSERT (event_block_base != NULL);
	EP_ASSERT (event_instance != NULL);
	EP_ASSERT (is_sorted_event || ((EventPipeBlock *)event_block_base)->format >= EP_SERIALIZATION_FORMAT_NETTRACE_V4);

	ep_return_false_if_nok (ep_block_get_block (ep_event_block_base_get_block_ref (event_block_base)) != NULL);

	uint32_t data_len = 0;
	uint8_t * aligned_end = NULL;
	uint32_t capture_proc_number = ep_event_instance_get_proc_num (event_instance);
	EventPipeBlock *block = &event_block_base->block;
	uint8_t * write_pointer = block->write_pointer;

	if (!event_block_base->use_header_compression) {
		uint32_t total_size = ep_event_instance_get_aligned_total_size (event_instance, block->format);
		ep_raise_error_if_nok (write_pointer + total_size < block->end_of_the_buffer);

		aligned_end = write_pointer + total_size + sizeof (total_size);

		memcpy (write_pointer, &total_size, sizeof (total_size));
		write_pointer += sizeof (total_size);

		uint32_t metadata_id = ep_event_instance_get_metadata_id (event_instance);
		EP_ASSERT ((metadata_id & (1 << 31)) == 0);

		metadata_id |= (!is_sorted_event ? 1 << 31 : 0);
		memcpy (write_pointer, &metadata_id, sizeof (metadata_id));
		write_pointer += sizeof (metadata_id);

		if (block->format == EP_SERIALIZATION_FORMAT_NETPERF_V3) {
			uint32_t thread_id = (uint32_t)ep_event_instance_get_thread_id (event_instance);
			memcpy (write_pointer, &thread_id, sizeof (thread_id));
			write_pointer += sizeof (thread_id);
		} else if (block->format == EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
			memcpy (write_pointer, &sequence_number, sizeof (sequence_number));
			write_pointer += sizeof (sequence_number);

			uint64_t thread_id = ep_event_instance_get_thread_id (event_instance);
			memcpy (write_pointer, &thread_id, sizeof (thread_id));
			write_pointer += sizeof (thread_id);

			memcpy (write_pointer, &capture_thread_id, sizeof (capture_thread_id));
			write_pointer += sizeof (capture_thread_id);

			memcpy (write_pointer, &capture_proc_number, sizeof (capture_proc_number));
			write_pointer += sizeof (capture_proc_number);

			memcpy (write_pointer, &stack_id, sizeof (stack_id));
			write_pointer += sizeof (stack_id);
		}

		ep_timestamp_t timestamp = ep_event_instance_get_timestamp (event_instance);
		memcpy (write_pointer, &timestamp, sizeof (timestamp));
		write_pointer += sizeof (timestamp);

		const uint8_t *activity_id = ep_event_instance_get_activity_id_cref (event_instance);
		memcpy (write_pointer, activity_id, EP_ACTIVITY_ID_SIZE);
		write_pointer += EP_ACTIVITY_ID_SIZE;

		const uint8_t *relative_activity_id = ep_event_instance_get_related_activity_id_cref (event_instance);
		memcpy (write_pointer, relative_activity_id, EP_ACTIVITY_ID_SIZE);
		write_pointer += EP_ACTIVITY_ID_SIZE;

		data_len = ep_event_instance_get_data_len (event_instance);
		memcpy (write_pointer, &data_len, sizeof (data_len));
		write_pointer += sizeof (data_len);
	} else { // using header compression
		uint8_t flags = 0;
		uint8_t *header_write_pointer = &event_block_base->compressed_header[0];
		EventPipeEventHeader *last_header = &event_block_base->last_header;

		if (ep_event_instance_get_metadata_id (event_instance) != last_header->metadata_id) {
			header_write_pointer = event_block_base_write_var_uint32 (header_write_pointer, ep_event_instance_get_metadata_id (event_instance));
			flags |= 1;
		}

		if (is_sorted_event) {
			flags |= (1 << 6);
		}

		if (last_header->sequence_number + (ep_event_instance_get_metadata_id (event_instance) != 0 ? 1 : 0) != sequence_number ||
			last_header->capture_thread_id != capture_thread_id || last_header->capture_proc_number != capture_proc_number) {
			header_write_pointer = event_block_base_write_var_uint32 (header_write_pointer, sequence_number - last_header->sequence_number - 1);
			header_write_pointer = event_block_base_write_var_uint64 (header_write_pointer, capture_thread_id);
			header_write_pointer = event_block_base_write_var_uint32 (header_write_pointer, capture_proc_number);
			flags |= (1 << 1);
		}

		if (last_header->thread_id != ep_event_instance_get_thread_id (event_instance)) {
			header_write_pointer = event_block_base_write_var_uint64 (header_write_pointer, ep_event_instance_get_thread_id (event_instance));
			flags |= (1 << 2);
		}

		if (last_header->stack_id != stack_id) {
			header_write_pointer = event_block_base_write_var_uint32 (header_write_pointer, stack_id);
			flags |= (1 << 3);
		}

		ep_timestamp_t timestamp = ep_event_instance_get_timestamp (event_instance);
		header_write_pointer = event_block_base_write_var_uint64 (header_write_pointer, timestamp - last_header->timestamp);

		if (memcmp (&last_header->activity_id, ep_event_instance_get_activity_id_cref (event_instance), EP_ACTIVITY_ID_SIZE) != 0) {
			memcpy (header_write_pointer, ep_event_instance_get_activity_id_cref (event_instance), EP_ACTIVITY_ID_SIZE );
			header_write_pointer += EP_ACTIVITY_ID_SIZE;
			flags |= (1 << 4);
		}

		if (memcmp (&last_header->related_activity_id, ep_event_instance_get_related_activity_id_cref (event_instance), EP_ACTIVITY_ID_SIZE) != 0) {
			memcpy (header_write_pointer, ep_event_instance_get_related_activity_id_cref (event_instance), EP_ACTIVITY_ID_SIZE );
			header_write_pointer += EP_ACTIVITY_ID_SIZE;
			flags |= (1 << 5);
		}

		data_len = ep_event_instance_get_data_len (event_instance);
		if (last_header->data_len != data_len) {
			header_write_pointer = event_block_base_write_var_uint32 (header_write_pointer, data_len);
			flags |= (1 << 7);
		}

		uint32_t bytes_written = (uint32_t)(header_write_pointer - &event_block_base->compressed_header[0]);
		uint32_t total_size = 1 + bytes_written + data_len;

		if (write_pointer + total_size >= block->end_of_the_buffer) {
			// TODO: Orignal EP updates blocks write pointer continiously, doing the same here before
			//bailing out. Question is if that is intentional or just a side effect of directly updating
			//the member.
			block->write_pointer = write_pointer;
			ep_raise_error ();
		}

		last_header->metadata_id = ep_event_instance_get_metadata_id (event_instance);
		last_header->sequence_number = sequence_number;
		last_header->thread_id = ep_event_instance_get_thread_id (event_instance);
		last_header->capture_thread_id = capture_thread_id;
		last_header->capture_proc_number = capture_proc_number;
		last_header->stack_id = stack_id;
		last_header->timestamp = timestamp;
		memcpy (&last_header->activity_id, ep_event_instance_get_activity_id_cref (event_instance), EP_ACTIVITY_ID_SIZE);
		memcpy (&last_header->related_activity_id, ep_event_instance_get_related_activity_id_cref (event_instance), EP_ACTIVITY_ID_SIZE);
		last_header->data_len = data_len;

		aligned_end = write_pointer + total_size;
		*write_pointer = flags;
		write_pointer++;
		memcpy (write_pointer, &event_block_base->compressed_header[0], bytes_written);
		write_pointer += bytes_written;
	}

	if (data_len > 0) {
		memcpy (write_pointer, ep_event_instance_get_data (event_instance), data_len);
		write_pointer += data_len;
	}

	if (block->format == EP_SERIALIZATION_FORMAT_NETPERF_V3) {
		uint32_t stack_size = ep_stack_contents_get_size (ep_event_instance_get_stack_contents_ref (event_instance));
		memcpy (write_pointer, &stack_size, sizeof (stack_size));
		write_pointer += sizeof (stack_size);

		if (stack_size > 0) {
			memcpy (write_pointer, ep_stack_contents_get_pointer (ep_event_instance_get_stack_contents_ref (event_instance)), stack_size);
			write_pointer += stack_size;
		}
	}

	while (write_pointer < aligned_end)
		*write_pointer++ = (uint8_t)0; // put padding at the end to get 4 bytes alignment of the payload

	EP_ASSERT (write_pointer == aligned_end);

	ep_timestamp_t instance_timestamp;
	instance_timestamp = ep_event_instance_get_timestamp (event_instance);
	if (event_block_base->min_timestamp > instance_timestamp)
		event_block_base->min_timestamp = instance_timestamp;
	if (event_block_base->max_timestamp < instance_timestamp)
		event_block_base->max_timestamp = instance_timestamp;

	block->write_pointer = write_pointer;

	EP_ASSERT (result);

ep_on_exit:
	return result;

ep_on_error:
	result = false;
	ep_exit_error_handler ();
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

	return (int32_t)(ep_sequence_point_sizeof_timestamp (sequence_point) +
		sizeof (uint32_t) + //thread count
		thread_count * size_of_sequence_number);
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

static
void
sequence_point_block_fini (EventPipeSequencePointBlock *sequence_point_block)
{
	EP_ASSERT (sequence_point_block != NULL);
	ep_block_fini (&sequence_point_block->block);
}

static EventPipeBlockVtable sequence_point_block_vtable = {
	{
		sequence_point_block_free_func,
		block_fast_serialize_func,
		sequence_point_block_get_type_name_func },
	block_clear_func,
	block_get_header_size_func,
	block_serialize_header_func };

EventPipeSequencePointBlock *
ep_sequence_point_block_alloc (EventPipeSequencePoint *sequence_point)
{
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
	EP_ASSERT (sequence_point != NULL);

	ep_return_null_if_nok (ep_block_init (
		&sequence_point_block->block,
		&sequence_point_block_vtable,
		sequence_point_get_block_size (sequence_point),
		EP_SERIALIZATION_FORMAT_NETTRACE_V4) != NULL);

	const ep_timestamp_t timestamp = ep_sequence_point_get_timestamp (sequence_point);
	memcpy (sequence_point_block->block.write_pointer, &timestamp, sizeof (timestamp));
	sequence_point_block->block.write_pointer += sizeof (timestamp);

	const uint32_t thread_count = ep_rt_thread_sequence_number_map_count (ep_sequence_point_get_thread_sequence_numbers_cref (sequence_point));
	memcpy (sequence_point_block->block.write_pointer, &thread_count, sizeof (thread_count));
	sequence_point_block->block.write_pointer += sizeof (thread_count);

	for (ep_rt_thread_sequence_number_hash_map_iterator_t iterator = ep_rt_thread_sequence_number_map_iterator_begin (ep_sequence_point_get_thread_sequence_numbers_cref (sequence_point));
		!ep_rt_thread_sequence_number_map_iterator_end (ep_sequence_point_get_thread_sequence_numbers_cref (sequence_point), &iterator);
		ep_rt_thread_sequence_number_map_iterator_next (&iterator)) {

		const EventPipeThreadSessionState *key = ep_rt_thread_sequence_number_map_iterator_key (&iterator);

		const uint64_t thread_id = ep_thread_get_os_thread_id (ep_thread_session_state_get_thread (key));
		memcpy (sequence_point_block->block.write_pointer, &thread_id, sizeof (thread_id));
		sequence_point_block->block.write_pointer += sizeof (thread_id);

		const uint32_t sequence_number = ep_rt_thread_sequence_number_map_iterator_value (&iterator);
		memcpy (sequence_point_block->block.write_pointer, &sequence_number, sizeof (sequence_number));
		sequence_point_block->block.write_pointer += sizeof (sequence_number);
	}

	return sequence_point_block;
}

void
ep_sequence_point_block_fini (EventPipeSequencePointBlock *sequence_point_block)
{
	ep_return_void_if_nok (sequence_point_block != NULL);
	sequence_point_block_fini (sequence_point_block);
}

void
ep_sequence_point_block_free (EventPipeSequencePointBlock *sequence_point_block)
{
	ep_return_void_if_nok (sequence_point_block != NULL);

	sequence_point_block_fini (sequence_point_block);
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

	ep_block_clear (&stack_block->block);
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
stack_block_serialize_header_func (
	void *object,
	FastSerializer *fast_serializer)
{
	EP_ASSERT (object != NULL);
	EP_ASSERT (fast_serializer != NULL);

	EventPipeStackBlock *stack_block = (EventPipeStackBlock *)object;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&stack_block->initial_index, sizeof (stack_block->initial_index));
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&stack_block->count, sizeof (stack_block->count));
}

static EventPipeBlockVtable stack_block_vtable = {
	{
		stack_block_free_func,
		block_fast_serialize_func,
		stack_block_get_type_name_func },
	stack_block_clear_func,
	stack_block_get_header_size_func,
	stack_block_serialize_header_func };

EventPipeStackBlock *
ep_stack_block_alloc (uint32_t max_block_size)
{
	EventPipeStackBlock *instance = ep_rt_object_alloc (EventPipeStackBlock);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_block_init (
		&instance->block,
		&stack_block_vtable,
		max_block_size,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4) != NULL);

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

	ep_block_fini (&stack_block->block);
	ep_rt_object_free (stack_block);
}

bool
ep_stack_block_write_stack (
	EventPipeStackBlock *stack_block,
	uint32_t stack_id,
	EventPipeStackContents *stack)
{
	bool result = true;

	EP_ASSERT (stack_block != NULL);

	uint32_t stack_size = ep_stack_contents_get_size (stack);
	uint32_t total_size = sizeof (stack_size) + stack_size;
	EventPipeBlock *block = &stack_block->block;
	uint8_t *write_pointer = block->write_pointer;

	ep_raise_error_if_nok (write_pointer + total_size < block->end_of_the_buffer);

	if (!stack_block->has_initial_index) {
		stack_block->has_initial_index = true;
		stack_block->initial_index = stack_id;
	}

	stack_block->count++;

	memcpy (write_pointer, &stack_size, sizeof (stack_size));
	write_pointer += sizeof (stack_size);

	if (stack_size > 0) {
		memcpy (write_pointer, ep_stack_contents_get_pointer (stack), stack_size);
		write_pointer += stack_size;
	}

	block->write_pointer = write_pointer;

	EP_ASSERT (result);

ep_on_exit:
	return result;

ep_on_error:
	result = false;
	ep_exit_error_handler ();
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_block;
const char quiet_linker_empty_file_warning_eventpipe_block = 0;
#endif
