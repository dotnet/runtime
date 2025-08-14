#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_BUFFER_GETTER_SETTER
#include "ep.h"
#include "ep-buffer.h"
#include "ep-event.h"
#include "ep-event-instance.h"
#include "ep-event-payload.h"
#include "ep-session.h"

/*
 * EventPipeBuffer.
 */

EventPipeBuffer *
ep_buffer_alloc (
	uint32_t buffer_size,
	EventPipeThread *writer_thread,
	uint32_t event_sequence_number,
	EventPipeBufferGuardLevel buffer_guard_level)
{
	EventPipeBuffer *instance = ep_rt_object_alloc (EventPipeBuffer);
	ep_raise_error_if_nok (instance != NULL);

	instance->writer_thread = writer_thread;
	instance->event_sequence_number = event_sequence_number;

	instance->buffer = ep_rt_valloc0 (buffer_size);
	ep_raise_error_if_nok (instance->buffer);

	instance->limit = instance->buffer + buffer_size;

	instance->creation_timestamp = ep_perf_timestamp_get ();
	EP_ASSERT (instance->creation_timestamp > 0);

	instance->buffer_guard_level = buffer_guard_level;
	if (instance->buffer_guard_level > EP_BUFFER_GUARD_LEVEL_NONE) {
		EP_ASSERT (sizeof(EventPipeBufferHeaderGuard) == EP_BUFFER_HEADER_GUARD_SIZE);
		// Initialize header guard via struct fields
		EventPipeBufferHeaderGuard *header = (EventPipeBufferHeaderGuard *)instance->buffer;
		memset (header, 0, EP_BUFFER_HEADER_GUARD_SIZE);
		header->magic = EP_BUFFER_HDR_MAGIC;
		header->creation_timestamp = (uint64_t)instance->creation_timestamp;
		// Store the writer thread pointer value in a stable 64-bit slot for checksum purposes
		header->writer_thread = (uint64_t)(uintptr_t)instance->writer_thread;
		header->first_event_sequence_number = event_sequence_number;

		EP_ASSERT (sizeof(EventPipeBufferFooterGuard) == EP_BUFFER_FOOTER_GUARD_SIZE);
		// Footer signature layout (placed at end of buffer):
		// [ uint64 magic2 ][ uint64 magic2_inv ][ uint64 checksum ] and remaining bytes (if any) left as 0xEB.
		uint8_t *footer_base_bytes = instance->limit - EP_BUFFER_FOOTER_GUARD_SIZE;
		EventPipeBufferFooterGuard *footer = (EventPipeBufferFooterGuard *)footer_base_bytes;
		memset (footer, 0xEB, EP_BUFFER_FOOTER_GUARD_SIZE);
		footer->magic = EP_BUFFER_FOOTER_MAGIC;
		footer->magic_inv = ~EP_BUFFER_FOOTER_MAGIC;
		footer->checksum = (uint64_t)instance->creation_timestamp ^ header->writer_thread ^ header->first_event_sequence_number ^ EP_BUFFER_CHECKSUM_SALT;

		if ((instance->buffer_guard_level >= EP_BUFFER_GUARD_LEVEL_PROTECT_OUTSIDE_WRITES) &&
			!ep_rt_vprotect (instance->buffer, instance->limit - instance->buffer, EP_PAGE_PROTECTION_READONLY))
			ep_rt_fatal_error_with_message ("Failed to add read-only protection to EventPipeBuffer");

		instance->first_event_address = ep_buffer_get_next_aligned_address (instance, instance->buffer + EP_BUFFER_HEADER_GUARD_SIZE);
		instance->write_limit = footer_base_bytes;
	} else {
		instance->first_event_address = ep_buffer_get_next_aligned_address (instance, instance->buffer);
		instance->write_limit = instance->limit;
	}

	instance->current = instance->first_event_address;

	instance->current_read_event = NULL;
	instance->prev_buffer = NULL;
	instance->next_buffer = NULL;

	ep_rt_volatile_store_uint32_t (&instance->state, (uint32_t)EP_BUFFER_STATE_WRITABLE);

ep_on_exit:
	return instance;

ep_on_error:
	ep_buffer_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_buffer_free (EventPipeBuffer *buffer)
{
	ep_return_void_if_nok (buffer != NULL);

	// We should never be deleting a buffer that a writer thread might still try to write to
	EP_ASSERT (ep_rt_volatile_load_uint32_t (&buffer->state) == (uint32_t)EP_BUFFER_STATE_READ_ONLY);

	ep_rt_vfree (buffer->buffer, buffer->limit - buffer->buffer);
	buffer->buffer = NULL;
	buffer->limit = NULL;
	ep_rt_object_free (buffer);
}

bool
ep_buffer_write_event (
	EventPipeBuffer *buffer,
	ep_rt_thread_handle_t thread,
	EventPipeSession *session,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	EventPipeStackContents *stack)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (payload != NULL);
	EP_ASSERT (((size_t)buffer->current % EP_BUFFER_ALIGNMENT_SIZE) == 0);

	// We should never try to write to a buffer that isn't expecting to be written to.
	EP_ASSERT ((EventPipeBufferState)buffer->state == EP_BUFFER_STATE_WRITABLE);

	bool success = true;
	EventPipeEventInstance *instance = NULL;
	uint8_t *data_dest;
	uint32_t proc_number;
	uint32_t event_size;

	// Don't write if the guards are not valid, helps narrow time windows for buffer corruption
	if (buffer->buffer_guard_level != EP_BUFFER_GUARD_LEVEL_NONE)
		ep_buffer_ensure_guard_consistency (buffer);

	if ((buffer->buffer_guard_level >= EP_BUFFER_GUARD_LEVEL_PROTECT_OUTSIDE_WRITES) &&
		!ep_rt_vprotect (buffer->buffer, buffer->limit - buffer->buffer, EP_PAGE_PROTECTION_READWRITE))
		ep_rt_fatal_error_with_message ("Failed to add read-write protection to EventPipeBuffer");

	// Calculate the location of the data payload.
	data_dest = (ep_event_payload_get_size (payload) == 0 ? NULL : buffer->current + sizeof (*instance) - sizeof (instance->stack_contents_instance.stack_frames) + ep_stack_contents_get_full_size (stack));

	// Calculate the size of the event.
	event_size = sizeof (*instance) - sizeof (instance->stack_contents_instance.stack_frames) + ep_stack_contents_get_full_size (stack) + ep_event_payload_get_size (payload);

	// Make sure we have enough space to write the event.
	ep_raise_error_if_nok (buffer->current + event_size <= buffer->write_limit);

	proc_number = ep_rt_current_processor_get_number ();
	instance = ep_event_instance_init (
		(EventPipeEventInstance *)buffer->current,
		ep_event,
		proc_number,
		ep_rt_thread_id_t_to_uint64_t((thread == NULL) ? ep_rt_current_thread_get_id () : ep_rt_thread_get_id (thread)),
		data_dest,
		ep_event_payload_get_size (payload),
		(thread == NULL) ? NULL : activity_id,
		related_activity_id);
	ep_raise_error_if_nok (instance != NULL);

	// Copy the stack if a separate stack trace was provided.
	if (stack != NULL)
		ep_stack_contents_flatten (stack, ep_event_instance_get_stack_contents_instance_ref (instance));

	// Write the event payload data to the buffer.
	if (ep_event_payload_get_size (payload) > 0)
		ep_event_payload_copy_data (payload, data_dest);

	EP_ASSERT (success);

	if ((buffer->buffer_guard_level >= EP_BUFFER_GUARD_LEVEL_PROTECT_OUTSIDE_WRITES) &&
		!ep_rt_vprotect (buffer->buffer, buffer->limit - buffer->buffer, EP_PAGE_PROTECTION_READONLY)) {
		ep_rt_fatal_error_with_message ("Failed to add read-only protection to EventPipeBuffer");

		// While the buffer was READWRITE, the buffer could have been corrupted
		ep_buffer_ensure_guard_consistency (buffer);
	}

	// Advance the current pointer past the event.
	buffer->current = ep_buffer_get_next_aligned_address (buffer, buffer->current + event_size);
	EP_ASSERT (buffer->current <= buffer->write_limit);

ep_on_exit:
	return success;

ep_on_error:
	success = false;
	ep_exit_error_handler ();
}

void
ep_buffer_move_next_read_event (EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (ep_rt_volatile_load_uint32_t (&buffer->state) == (uint32_t)EP_BUFFER_STATE_READ_ONLY);

	// If current_read_event is NULL we've reached the end of the events
	if (buffer->current_read_event != NULL) {
		// Confirm that current_read_event is within the used range of the buffer.
		if (((uint8_t *)buffer->current_read_event < buffer->first_event_address) || ((uint8_t *)buffer->current_read_event >= buffer->current)) {
			EP_ASSERT (!"Input pointer is out of range.");
			buffer->current_read_event = NULL;
		} else {
			buffer->current_read_event = (EventPipeEventInstance *)ep_buffer_get_next_aligned_address (buffer, (uint8_t *)buffer->current_read_event + ep_event_instance_get_flattened_size (buffer->current_read_event));

			// this may roll over and that is fine
			buffer->event_sequence_number++;

			// Check to see if we've reached the end of the written portion of the buffer.
			if ((uint8_t *)buffer->current_read_event >= buffer->current)
				buffer->current_read_event = NULL;
		}
	}

	// Ensure that the timestamp is valid.  The buffer is zero'd before use, so a zero timestamp is invalid.
#ifdef EP_CHECKED_BUILD
	if (buffer->current_read_event != NULL) {
		ep_timestamp_t next_timestamp = ep_event_instance_get_timestamp (buffer->current_read_event);
		EP_ASSERT (next_timestamp != 0);
	}
#endif
}

EventPipeEventInstance *
ep_buffer_get_current_read_event (const EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (ep_rt_volatile_load_uint32_t (&buffer->state) == (uint32_t)EP_BUFFER_STATE_READ_ONLY);
	return buffer->current_read_event;
}

uint32_t
ep_buffer_get_current_sequence_number (const EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (ep_rt_volatile_load_uint32_t (&buffer->state) == (uint32_t)EP_BUFFER_STATE_READ_ONLY);
	return buffer->event_sequence_number;
}

EventPipeBufferState
ep_buffer_get_volatile_state (const EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);
	return (EventPipeBufferState)ep_rt_volatile_load_uint32_t (&buffer->state);
}

void
ep_buffer_convert_to_read_only (EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer->current_read_event == NULL);

	ep_thread_requires_lock_held (buffer->writer_thread);

	ep_rt_volatile_store_uint32_t (&buffer->state, (uint32_t)EP_BUFFER_STATE_READ_ONLY);

	// If this buffer contains an event, select it.
	uint8_t *first_aligned_instance = buffer->first_event_address;
	if (buffer->current > first_aligned_instance)
		buffer->current_read_event = (EventPipeEventInstance*)first_aligned_instance;
	else
		buffer->current_read_event = NULL;

	if (buffer->buffer_guard_level != EP_BUFFER_GUARD_LEVEL_NONE)
		ep_buffer_ensure_guard_consistency (buffer);

	if ((buffer->buffer_guard_level >= EP_BUFFER_GUARD_LEVEL_PROTECT_ON_READONLY) &&
		!ep_rt_vprotect (buffer->buffer, buffer->limit - buffer->buffer, EP_PAGE_PROTECTION_READONLY))
		ep_rt_fatal_error_with_message ("Failed to add read-only protection to EventPipeBuffer");
}

void
ep_buffer_ensure_guard_consistency (const EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer->buffer_guard_level != EP_BUFFER_GUARD_LEVEL_NONE);

	const char *const header_err = "EventPipeBuffer header guard is corrupted";
	const char *const footer_err = "EventPipeBuffer footer guard is corrupted";

	if (buffer->first_event_address != ep_buffer_get_next_aligned_address (buffer, buffer->buffer + EP_BUFFER_HEADER_GUARD_SIZE))
		ep_rt_fatal_error_with_message (header_err);

	const EventPipeBufferHeaderGuard *header = (const EventPipeBufferHeaderGuard *)buffer->buffer;
	if (header->magic != EP_BUFFER_HDR_MAGIC)
		ep_rt_fatal_error_with_message (header_err);

	// Header trailing padding bytes should still be 0x00
	for (size_t i = 0; i < sizeof(header->padding); i++) {
		if (header->padding[i] != 0x00)
			ep_rt_fatal_error_with_message ("EventPipeBuffer header guard padding is corrupted");
	}

	uint8_t *footer_base = buffer->write_limit;
	if (footer_base != buffer->limit - EP_BUFFER_FOOTER_GUARD_SIZE)
		ep_rt_fatal_error_with_message (footer_err);

	const EventPipeBufferFooterGuard *footer = (const EventPipeBufferFooterGuard *)footer_base;
	if (footer->magic != EP_BUFFER_FOOTER_MAGIC)
		ep_rt_fatal_error_with_message (footer_err);
	if (footer->magic_inv != ~EP_BUFFER_FOOTER_MAGIC)
		ep_rt_fatal_error_with_message (footer_err);

	// Footer checksum must match header fields
	uint64_t expected_checksum = header->creation_timestamp ^ header->writer_thread ^ header->first_event_sequence_number ^ EP_BUFFER_CHECKSUM_SALT;
	if (footer->checksum != expected_checksum)
		ep_rt_fatal_error_with_message ("EventPipeBuffer header guard and footer checksum do not match, buffer is corrupted");

	// Verify the remaining bytes in the footer guard are the fill value 0xEB.
	for (size_t i = 0; i < sizeof(footer->padding); i++) {
		if (footer->padding[i] != 0xEB)
			ep_rt_fatal_error_with_message ("EventPipeBuffer footer guard padding is corrupted");
	}

}

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_ensure_consistency (const EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);

	if (buffer->buffer_guard_level != EP_BUFFER_GUARD_LEVEL_NONE)
		ep_buffer_ensure_guard_consistency (buffer);

	uint8_t *ptr = buffer->first_event_address;

	// Check to see if the buffer is empty.
	if (ptr == buffer->current)
		// Make sure that the buffer size is greater than zero.
		EP_ASSERT (buffer->buffer != buffer->limit);

	// Validate the contents of the filled portion of the buffer.
	while (ptr < buffer->current) {
		// Validate the event.
		EventPipeEventInstance *instance = (EventPipeEventInstance *)ptr;
		EP_ASSERT (ep_event_instance_ensure_consistency (instance));

		// Validate that payload and length match.
		EP_ASSERT (
			(ep_event_instance_get_data (instance) != NULL && ep_event_instance_get_data_len (instance) > 0) ||
			(ep_event_instance_get_data (instance) == NULL && ep_event_instance_get_data_len (instance) == 0)
		);

		// Skip the event.
		ptr = ep_buffer_get_next_aligned_address (buffer, ptr + ep_event_instance_get_flattened_size (instance));
	}

	// When we're done walking the filled portion of the buffer,
	// ptr should be the same as m_pCurrent.
	EP_ASSERT (ptr == buffer->current);

	// Walk the rest of the writable portion of the buffer, making sure it is properly zeroed.
	while (ptr < buffer->write_limit) {
		EP_ASSERT (*ptr == 0);
		ptr++;
	}

	return true;
}
#endif

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_buffer;
const char quiet_linker_empty_file_warning_eventpipe_buffer = 0;
#endif
