#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * Forward declares of all static functions.
 */

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

/*
 * EventPipeBlock
 */

void
ep_block_clear (EventPipeBlock *block)
{
	ep_return_void_if_nok (block != NULL);
	ep_return_void_if_nok (ep_block_get_block (block) != NULL);

	EP_ASSERT (ep_block_get_write_pointer (block) <= ep_block_get_end_of_the_buffer (block));

	memset (ep_block_get_block (block), 0, ep_block_get_end_of_the_buffer (block) - ep_block_get_block (block));
	ep_block_set_write_pointer (block, ep_block_get_block (block));
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
	ep_return_void_if_nok (block != NULL && fast_serializer != NULL && ep_block_get_block (block) != NULL);

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

		EP_ASSERT (ep_fast_serializer_get_write_error_encountered (fast_serializer) || ep_fast_serializer_get_required_padding (fast_serializer));
	}

	ep_block_serialize_header_vcall (block, fast_serializer);
	ep_fast_serializer_write_buffer (fast_serializer, ep_block_get_block (block), data_size);
}

/*
 * EventPipeEventBlockBase
 */

static
uint8_t *
event_block_base_write_var_uint32 (
	uint8_t * write_pointer,
	uint32_t value)
{
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
	while (value >= 0x80) {
		*write_pointer = (uint8_t)(value | 0x80);
		write_pointer++;
		value >>= 7;
	}
	*write_pointer = (uint8_t)value;
	write_pointer++;
	return write_pointer;
}

void
ep_event_block_base_clear (EventPipeEventBlockBase *event_block_base)
{
	ep_return_void_if_nok (event_block_base != NULL);

	ep_block_clear (ep_event_block_base_get_block_ref (event_block_base));
	memset (ep_event_block_base_get_last_header_ref (event_block_base), 0, sizeof (EventPipeEventHeader));
	ep_event_block_base_set_min_timestamp (event_block_base, INT64_MAX);
	ep_event_block_base_set_max_timestamp (event_block_base, INT64_MIN);
}

uint32_t
ep_event_block_base_get_header_size (const EventPipeEventBlockBase *event_block_base)
{
	ep_return_zero_if_nok (event_block_base != NULL && ep_block_get_format ((EventPipeBlock *)event_block_base) != EP_SERIALIZATION_FORMAT_NETPERF_V3);

	return	sizeof(uint16_t) + // header size
			sizeof(uint16_t) + // flags
			sizeof(int64_t)  + // min timestamp
			sizeof(int64_t);   // max timestamp
}

void
ep_event_block_base_serialize_header (
	EventPipeEventBlockBase *event_block_base,
	FastSerializer *fast_serializer)
{
	ep_return_void_if_nok (event_block_base != NULL && ep_block_get_format ((EventPipeBlock *)event_block_base) != EP_SERIALIZATION_FORMAT_NETPERF_V3 && fast_serializer != NULL);

	const uint16_t header_size = (uint16_t)ep_block_get_header_size_vcall ((EventPipeBlock *)event_block_base);
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&header_size, sizeof (header_size));

	const uint16_t flags = ep_event_block_base_get_use_header_compression (event_block_base) ? 1 : 0;
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&flags, sizeof (flags));

	uint64_t min_timestamp = ep_event_block_base_get_min_timestamp (event_block_base);
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&min_timestamp, sizeof (min_timestamp));

	uint64_t max_timestamp = ep_event_block_base_get_max_timestamp (event_block_base);
	ep_fast_serializer_write_buffer (fast_serializer, (const uint8_t *)&max_timestamp, sizeof (max_timestamp));
}

bool
ep_event_block_base_write_event (
	EventPipeEventBlockBase *event_block_base,
	EventPipeEventInstance *event_instance,
	uint64_t capture_thread_id,
	uint32_t sequence_number,
	int32_t stack_id,
	bool is_sorted_event)
{
	ep_return_false_if_nok (event_block_base != NULL && event_instance != NULL);

	EventPipeBlock *block = ep_event_block_base_get_block_ref (event_block_base);
	ep_raise_error_if_nok (ep_block_get_block (block) != NULL);

	uint32_t data_len = 0;
	uint8_t * aligned_end = NULL;
	uint32_t capture_proc_number = ep_event_instance_get_proc_num (event_instance);
	uint8_t * write_pointer = ep_block_get_write_pointer (block);

	if (!ep_event_block_base_get_use_header_compression (event_block_base)) {
		uint32_t total_size = ep_event_instance_get_aligned_total_size (event_instance, ep_block_get_format (block));
		ep_raise_error_if_nok (write_pointer + total_size < ep_block_get_end_of_the_buffer (block));

		aligned_end = write_pointer + total_size + sizeof (total_size);

		memcpy (write_pointer, &total_size, sizeof (total_size));
		write_pointer += sizeof (total_size);

		uint32_t metadata_id = ep_event_instance_get_metadata_id (event_instance);
		EP_ASSERT ((metadata_id & (1 << 31)) == 0);

		metadata_id |= (!is_sorted_event ? 1 << 31 : 0);
		memcpy (write_pointer, &metadata_id, sizeof (metadata_id));
		write_pointer += sizeof (metadata_id);

		if (ep_block_get_format (block) == EP_SERIALIZATION_FORMAT_NETPERF_V3) {
			int32_t thread_id = (int32_t)ep_event_instance_get_thread_id (event_instance);
			memcpy (write_pointer, &thread_id, sizeof (thread_id));
			write_pointer += sizeof (thread_id);
		} else if (ep_block_get_format (block) == EP_SERIALIZATION_FORMAT_NETTRACE_V4) {
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

		int64_t timestamp = ep_event_instance_get_timestamp (event_instance);
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
		uint8_t *header_write_pointer = ep_event_block_base_get_compressed_header_ref (event_block_base);
		EventPipeEventHeader *last_header = ep_event_block_base_get_last_header_ref (event_block_base);

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
			header_write_pointer = event_block_base_write_var_uint32 (header_write_pointer, (uint32_t)capture_thread_id);
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

		int64_t timestamp = ep_event_instance_get_timestamp (event_instance);
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

		uint32_t bytes_written = (uint32_t)(header_write_pointer - ep_event_block_base_get_compressed_header_cref (event_block_base));
		uint32_t total_size = 1 + bytes_written + data_len;

		if (write_pointer + total_size >= ep_block_get_end_of_the_buffer (block)) {
			//TODO: Orignal EP updates blocks write pointer continiously, doing the same here before
			//bailing out. Question is if that is intentional or just a side effect of directly updating
			//the member.
			ep_block_set_write_pointer (block, write_pointer);
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
		memcpy (write_pointer, ep_event_block_base_get_compressed_header_cref (event_block_base), bytes_written);
		write_pointer += bytes_written;
	}

	if (data_len > 0) {
		memcpy (write_pointer, ep_event_instance_get_data (event_instance), data_len);
		write_pointer += data_len;
	}

	if (ep_block_get_format (block) == EP_SERIALIZATION_FORMAT_NETPERF_V3) {
		uint32_t stack_size = ep_stack_contents_get_size (ep_event_instance_get_stack_contents_ref (event_instance));
		memcpy (write_pointer, &stack_size, sizeof (stack_size));
		write_pointer += sizeof (stack_size);

		if (stack_size > 0) {
			memcpy (write_pointer, ep_event_instance_get_stack_contents_ref (event_instance), stack_size);
			write_pointer += stack_size;
		}
	}

	while (write_pointer < aligned_end)
		*write_pointer++ = (uint8_t)0; // put padding at the end to get 4 bytes alignment of the payload

	EP_ASSERT (write_pointer == aligned_end);

	int64_t instance_timestamp = ep_event_instance_get_timestamp (event_instance);
	if (ep_event_block_base_get_min_timestamp (event_block_base) > instance_timestamp)
		ep_event_block_base_set_min_timestamp (event_block_base, instance_timestamp);
	if (ep_event_block_base_get_max_timestamp (event_block_base) > instance_timestamp)
		ep_event_block_base_set_max_timestamp (event_block_base, instance_timestamp);

	ep_block_set_write_pointer (block, write_pointer);
	return true;

ep_on_error:
	return false;
}

/*
 * EventPipeStackBlock.
 */

bool
ep_stack_block_write_stack (
	EventPipeStackBlock *stack_block,
	int32_t stack_id,
	EventPipeStackContents *stack)
{
	ep_return_false_if_nok (stack_block != NULL);

	EventPipeBlock *block = ep_event_block_base_get_block_ref (ep_stack_block_get_event_block_base_ref (stack_block));
	ep_raise_error_if_nok (block != NULL && ep_block_get_block (block) != NULL);

	uint32_t stack_size = ep_stack_contents_get_size (stack);
	uint32_t total_size = sizeof (stack_size) + stack_size;
	uint8_t *write_pointer = ep_block_get_write_pointer (block);

	ep_raise_error_if_nok (write_pointer + total_size < ep_block_get_end_of_the_buffer (block));

	if (!ep_stack_block_get_has_initial_index (stack_block)) {
		ep_stack_block_set_has_initial_index (stack_block, true);
		ep_stack_block_set_initial_index (stack_block, stack_id);
	}

	ep_stack_block_set_count (stack_block, ep_stack_block_get_count (stack_block) + 1);

	memcpy (write_pointer, &stack_size, sizeof (stack_size));
	write_pointer += sizeof (stack_size);

	if (stack_size > 0) {
		memcpy (write_pointer, ep_stack_contents_get_pointer (stack), stack_size);
		write_pointer += stack_size;
	}

	ep_block_set_write_pointer (block, write_pointer);
	return true;

ep_on_error:
	return false;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_block;
const char quiet_linker_empty_file_warning_eventpipe_block = 0;
#endif
