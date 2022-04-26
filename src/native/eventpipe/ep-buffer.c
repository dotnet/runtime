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
	uint32_t event_sequence_number)
{
	EventPipeBuffer *instance = ep_rt_object_alloc (EventPipeBuffer);
	ep_raise_error_if_nok (instance != NULL);

	instance->writer_thread = writer_thread;
	instance->event_sequence_number = event_sequence_number;

	instance->buffer = ep_rt_valloc0 (buffer_size);
	ep_raise_error_if_nok (instance->buffer);

	instance->limit = instance->buffer + buffer_size;
	instance->current = ep_buffer_get_next_aligned_address (instance, instance->buffer);

	instance->creation_timestamp = ep_perf_timestamp_get ();
	EP_ASSERT (instance->creation_timestamp > 0);

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

	// Calculate the size of the event.
	uint32_t event_size = sizeof (EventPipeEventInstance) + ep_event_payload_get_size (payload);

	// Make sure we have enough space to write the event.
	if(buffer->current + event_size > buffer->limit)
		ep_raise_error ();

	// Calculate the location of the data payload.
	uint8_t *data_dest;
	data_dest = (ep_event_payload_get_size (payload) == 0 ? NULL : buffer->current + sizeof(EventPipeEventInstance));

	EventPipeStackContents stack_contents;
	EventPipeStackContents *current_stack_contents;
	current_stack_contents = ep_stack_contents_init (&stack_contents);
	if (stack == NULL && ep_event_get_need_stack (ep_event) && !ep_session_get_rundown_enabled (session)) {
		ep_walk_managed_stack_for_current_thread (current_stack_contents);
		stack = current_stack_contents;
	}

	uint32_t proc_number;
	proc_number = ep_rt_current_processor_get_number ();
	EventPipeEventInstance *instance;
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
		ep_stack_contents_copyto (stack, ep_event_instance_get_stack_contents_ref (instance));

	// Write the event payload data to the buffer.
	if (ep_event_payload_get_size (payload) > 0)
		ep_event_payload_copy_data (payload, data_dest);

	EP_ASSERT (success);

	// Advance the current pointer past the event.
	buffer->current = ep_buffer_get_next_aligned_address (buffer, buffer->current + event_size);

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
		if (((uint8_t *)buffer->current_read_event < buffer->buffer) || ((uint8_t *)buffer->current_read_event >= buffer->current)) {
			EP_ASSERT (!"Input pointer is out of range.");
			buffer->current_read_event = NULL;
		} else {
			if (ep_event_instance_get_data (buffer->current_read_event))
				// We have a pointer within the bounds of the buffer.
				// Find the next event by skipping the current event with it's data payload immediately after the instance.
				buffer->current_read_event = (EventPipeEventInstance *)ep_buffer_get_next_aligned_address (buffer, (uint8_t *)(ep_event_instance_get_data (buffer->current_read_event) + ep_event_instance_get_data_len (buffer->current_read_event)));
			else
				// In case we do not have a payload, the next instance is right after the current instance
				buffer->current_read_event = (EventPipeEventInstance *)ep_buffer_get_next_aligned_address (buffer, (uint8_t *)(buffer->current_read_event + 1));

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
	uint8_t *first_aligned_instance = ep_buffer_get_next_aligned_address (buffer, buffer->buffer);
	if (buffer->current > first_aligned_instance)
		buffer->current_read_event = (EventPipeEventInstance*)first_aligned_instance;
	else
		buffer->current_read_event = NULL;
}

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_ensure_consistency (const EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer != NULL);

	uint8_t *ptr = ep_buffer_get_next_aligned_address (buffer, buffer->buffer);

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
		ptr = ep_buffer_get_next_aligned_address (buffer, ptr + sizeof (EventPipeEventInstance) + ep_event_instance_get_data_len (instance));
	}

	// When we're done walking the filled portion of the buffer,
	// ptr should be the same as m_pCurrent.
	EP_ASSERT (ptr == buffer->current);

	// Walk the rest of the buffer, making sure it is properly zeroed.
	while (ptr < buffer->limit) {
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
