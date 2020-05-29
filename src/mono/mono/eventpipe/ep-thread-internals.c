#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_GETTER_SETTER
#include "ep.h"

/*
 * EventPipeThread.
 */

EventPipeThread *
ep_thread_alloc (void)
{
	EventPipeThread *instance = ep_rt_object_alloc (EventPipeThread);
	ep_raise_error_if_nok (instance != NULL);

	ep_rt_spin_lock_alloc (&instance->rt_lock);
	ep_raise_error_if_nok (instance->rt_lock.lock != NULL);

	instance->os_thread_id = ep_rt_current_thread_get_id ();
	memset (instance->session_state, 0, sizeof (instance->session_state));

ep_on_exit:
	return instance;

ep_on_error:
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_thread_free (EventPipeThread *thread)
{
	ep_return_void_if_nok (thread != NULL);

	EP_ASSERT (ep_rt_volatile_load_uint32_t ((const volatile uint32_t *)&thread->ref_count) == 0);

#ifdef EP_CHECKED_BUILD
	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i) {
		EP_ASSERT (thread->session_state [i] == NULL);
	}
#endif

	ep_rt_spin_lock_free (&thread->rt_lock);
	ep_rt_object_free (thread);
}

/*
 * EventPipeThreadHolder.
 */

EventPipeThreadHolder *
ep_thread_holder_alloc (EventPipeThread *thread)
{
	ep_return_null_if_nok (thread != NULL);

	EventPipeThreadHolder *instance = ep_rt_object_alloc (EventPipeThreadHolder);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_thread_holder_init (instance, thread) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeThreadHolder *
ep_thread_holder_init (
	EventPipeThreadHolder *thread_holder,
	EventPipeThread *thread)
{
	ep_return_null_if_nok (thread_holder != NULL && thread != NULL);

	thread_holder->thread = thread;
	ep_thread_addref (thread_holder->thread);

	return thread_holder;
}

void
ep_thread_holder_fini (EventPipeThreadHolder *thread_holder)
{
	ep_return_void_if_nok (thread_holder != NULL && thread_holder->thread);
	ep_thread_release (thread_holder->thread);
}

void
ep_thread_holder_free (EventPipeThreadHolder *thread_holder)
{
	ep_return_void_if_nok (thread_holder != NULL);
	ep_thread_holder_fini (thread_holder);
	ep_rt_object_free (thread_holder);
}

/*
 * EventPipeThreadSessionState.
 */

EventPipeThreadSessionState *
ep_thread_session_state_alloc (
	EventPipeThread *thread,
	EventPipeSession *session,
	EventPipeBufferManager *buffer_manager)
{
	EventPipeThreadSessionState *instance = ep_rt_object_alloc (EventPipeThreadSessionState);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_thread_holder_init (&instance->thread_holder, thread) != NULL);

	instance->session = session;
	instance->sequence_number = 1;

#ifdef EP_CHECKED_BUILD
	instance->buffer_manager = buffer_manager;
#endif

ep_on_exit:
	return instance;

ep_on_error:
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_thread_session_state_free (EventPipeThreadSessionState *thread_session_state)
{
	ep_return_void_if_nok (thread_session_state != NULL);
	ep_thread_holder_fini (&thread_session_state->thread_holder);
	ep_rt_object_free (thread_session_state);
}

EventPipeThread *
ep_thread_session_state_get_thread (const EventPipeThreadSessionState *thread_session_state)
{
	EP_ASSERT (thread_session_state != NULL);
	return thread_session_state->thread_holder.thread;
}

EventPipeBuffer *
ep_thread_session_state_get_write_buffer (const EventPipeThreadSessionState *thread_session_state)
{
	EP_ASSERT (thread_session_state != NULL);
	ep_thread_requires_lock_held (thread_session_state->thread_holder.thread);

	EP_ASSERT ((thread_session_state->write_buffer == NULL) || (ep_rt_volatile_load_uint32_t (&thread_session_state->write_buffer->state) == EP_BUFFER_STATE_WRITABLE));
	return thread_session_state->write_buffer;
}

void
ep_thread_session_state_set_write_buffer (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBuffer *new_buffer)
{
	EP_ASSERT (thread_session_state != NULL);
	ep_thread_requires_lock_held (thread_session_state->thread_holder.thread);

	EP_ASSERT ((new_buffer == NULL) || (ep_rt_volatile_load_uint32_t (&thread_session_state->write_buffer->state) == EP_BUFFER_STATE_WRITABLE));
	EP_ASSERT ((thread_session_state->write_buffer == NULL) || (ep_rt_volatile_load_uint32_t (&thread_session_state->write_buffer->state) == EP_BUFFER_STATE_WRITABLE));

	if (thread_session_state->write_buffer)
		ep_buffer_convert_to_read_only (thread_session_state->write_buffer);

	thread_session_state->write_buffer = new_buffer;
}

EventPipeBufferList *
ep_thread_session_state_get_buffer_list (const EventPipeThreadSessionState *thread_session_state)
{
	EP_ASSERT (thread_session_state != NULL);
	ep_buffer_manager_requires_lock_held (thread_session_state->buffer_manager);
	return thread_session_state->buffer_list;
}

void
ep_thread_session_state_set_buffer_list (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBufferList *new_buffer_list)
{
	EP_ASSERT (thread_session_state != NULL);
	ep_buffer_manager_requires_lock_held (thread_session_state->buffer_manager);
	thread_session_state->buffer_list = new_buffer_list;
}

uint32_t
ep_thread_session_state_get_volatile_sequence_number (const EventPipeThreadSessionState *thread_session_state)
{
	EP_ASSERT (thread_session_state != NULL);
	return ep_rt_volatile_load_uint32_t_without_barrier (&thread_session_state->sequence_number);
}

uint32_t
ep_thread_session_state_get_sequence_number (const EventPipeThreadSessionState *thread_session_state)
{
	EP_ASSERT (thread_session_state != NULL);
	ep_thread_requires_lock_held (thread_session_state->thread_holder.thread);
	return ep_rt_volatile_load_uint32_t_without_barrier (&thread_session_state->sequence_number);
}

void
ep_thread_session_state_increment_sequence_number (EventPipeThreadSessionState *thread_session_state)
{
	EP_ASSERT (thread_session_state != NULL);
	ep_thread_requires_lock_held (thread_session_state->thread_holder.thread);
	thread_session_state->sequence_number++;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_thread_internals;
const char quiet_linker_empty_file_warning_eventpipe_thread_internals = 0;
#endif
