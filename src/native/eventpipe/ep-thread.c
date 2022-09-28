#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_THREAD_GETTER_SETTER
#include "ep-buffer.h"
#include "ep-buffer-manager.h"
#include "ep-thread.h"
#include "ep-session.h"
#include "ep-rt.h"

static ep_rt_spin_lock_handle_t _ep_threads_lock = {0};
static ep_rt_thread_list_t _ep_threads = {0};

/*
 * Forward declares of all static functions.
 */

static
void
thread_holder_fini (EventPipeThreadHolder *thread_holder);

/*
 * EventPipeThread.
 */

EventPipeThread *
ep_thread_alloc (void)
{
	EventPipeThread *instance = ep_rt_object_alloc (EventPipeThread);
	ep_raise_error_if_nok (instance != NULL);

	ep_rt_spin_lock_alloc (&instance->rt_lock);
	ep_raise_error_if_nok (ep_rt_spin_lock_is_valid (&instance->rt_lock));

	instance->os_thread_id = ep_rt_thread_id_t_to_uint64_t (ep_rt_current_thread_get_id ());
	memset (instance->session_state, 0, sizeof (instance->session_state));

	instance->writing_event_in_progress = UINT32_MAX;
	instance->unregistered = 0;

ep_on_exit:
	return instance;

ep_on_error:
	ep_thread_free (instance);
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

void
ep_thread_addref (EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	ep_rt_atomic_inc_int32_t (&thread->ref_count);
}

void
ep_thread_release (EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	if (ep_rt_atomic_dec_int32_t (&thread->ref_count) == 0)
		ep_thread_free (thread);
}

void
ep_thread_init (void)
{
	ep_rt_spin_lock_alloc (&_ep_threads_lock);
	if (!ep_rt_spin_lock_is_valid (&_ep_threads_lock))
		EP_UNREACHABLE ("Failed to allocate threads lock.");

	ep_rt_thread_list_alloc (&_ep_threads);
	if (!ep_rt_thread_list_is_valid (&_ep_threads))
		EP_UNREACHABLE ("Failed to allocate threads list.");
}

void
ep_thread_fini (void)
{
	// If threads are still included in list (depending on runtime shutdown order),
	// don't clean up since TLS destructor migh callback freeing items, no new
	// threads should however not be added to list at this stage.
	if (ep_rt_thread_list_is_empty (&_ep_threads)) {
		ep_rt_thread_list_free (&_ep_threads, NULL);
		ep_rt_spin_lock_free (&_ep_threads_lock);
	}
}

bool
ep_thread_register (EventPipeThread *thread)
{
	ep_rt_spin_lock_requires_lock_not_held (&_ep_threads_lock);

	ep_return_false_if_nok (thread != NULL);

	bool result = false;

	ep_thread_addref (thread);

	ep_rt_spin_lock_acquire (&_ep_threads_lock);
		result = ep_rt_thread_list_append (&_ep_threads, thread);
	ep_rt_spin_lock_release (&_ep_threads_lock);

	if (!result)
		ep_thread_release (thread);

	ep_rt_spin_lock_requires_lock_not_held (&_ep_threads_lock);

	return result;
}

bool
ep_thread_unregister (EventPipeThread *thread)
{
	ep_rt_spin_lock_requires_lock_not_held (&_ep_threads_lock);

	ep_return_false_if_nok (thread != NULL);

	bool found = false;
	EP_SPIN_LOCK_ENTER (&_ep_threads_lock, section1)
		// Remove ourselves from the global list
		ep_rt_thread_list_iterator_t iterator = ep_rt_thread_list_iterator_begin (&_ep_threads);
		while (!ep_rt_thread_list_iterator_end (&_ep_threads, &iterator)) {
			if (ep_rt_thread_list_iterator_value (&iterator) == thread) {
				ep_rt_thread_list_remove (&_ep_threads, thread);
				ep_rt_volatile_store_uint32_t(&thread->unregistered, 1);
				ep_thread_release (thread);
				found = true;
				break;
			}
			ep_rt_thread_list_iterator_next (&iterator);
		}
	EP_SPIN_LOCK_EXIT (&_ep_threads_lock, section1)

	EP_ASSERT (found || !"We couldn't find ourselves in the global thread list");

ep_on_exit:
	ep_rt_spin_lock_requires_lock_not_held (&_ep_threads_lock);
	return found;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeThread *
ep_thread_get (void)
{
	return ep_rt_thread_get ();
}

EventPipeThread *
ep_thread_get_or_create (void)
{
	return ep_rt_thread_get_or_create ();
}

void
ep_thread_get_threads (ep_rt_thread_array_t *threads)
{
	EP_ASSERT (threads != NULL);

	EP_SPIN_LOCK_ENTER (&_ep_threads_lock, section1)
		ep_rt_thread_list_iterator_t threads_iterator = ep_rt_thread_list_iterator_begin (&_ep_threads);
		while (!ep_rt_thread_list_iterator_end (&_ep_threads, &threads_iterator)) {
			EventPipeThread *thread = ep_rt_thread_list_iterator_value (&threads_iterator);
			if (thread) {
				// Add ref so the thread doesn't disappear when we release the lock
				ep_thread_addref (thread);
				ep_rt_thread_array_append (threads, thread);
			}
			ep_rt_thread_list_iterator_next (&threads_iterator);
		}
	EP_SPIN_LOCK_EXIT (&_ep_threads_lock, section1)

ep_on_exit:
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_thread_set_session_write_in_progress (
	EventPipeThread *thread,
	uint32_t session_index)
{
	EP_ASSERT (thread != NULL);
	EP_ASSERT (session_index < EP_MAX_NUMBER_OF_SESSIONS || session_index == UINT32_MAX);

	ep_rt_volatile_store_uint32_t (&thread->writing_event_in_progress, session_index);
}

uint32_t
ep_thread_get_session_write_in_progress (const EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	return ep_rt_volatile_load_uint32_t (&thread->writing_event_in_progress);
}

EventPipeThreadSessionState *
ep_thread_get_or_create_session_state (
	EventPipeThread *thread,
	EventPipeSession *session)
{
	EP_ASSERT (thread != NULL);
	EP_ASSERT (session != NULL);
	EP_ASSERT (ep_session_get_index (session) < EP_MAX_NUMBER_OF_SESSIONS);

	ep_thread_requires_lock_held (thread);

	EventPipeThreadSessionState *state = thread->session_state [ep_session_get_index (session)];
	if (!state) {
		state = ep_thread_session_state_alloc (thread, session, ep_session_get_buffer_manager (session));
		thread->session_state [ep_session_get_index (session)] = state;
	}

	return state;
}

EventPipeThreadSessionState *
ep_thread_get_session_state (
	const EventPipeThread *thread,
	EventPipeSession *session)
{
	EP_ASSERT (thread != NULL);
	EP_ASSERT (session != NULL);
	EP_ASSERT (ep_session_get_index (session) < EP_MAX_NUMBER_OF_SESSIONS);

	ep_thread_requires_lock_held (thread);

	EP_ASSERT (thread->session_state [ep_session_get_index (session)] != NULL);
	return thread->session_state [ep_session_get_index (session)];
}

void
ep_thread_delete_session_state (
	EventPipeThread *thread,
	EventPipeSession *session)
{
	EP_ASSERT (thread != NULL);
	EP_ASSERT (session != NULL);

	ep_thread_requires_lock_held (thread);

	uint32_t index = ep_session_get_index (session);
	EP_ASSERT (index < EP_MAX_NUMBER_OF_SESSIONS);

	EP_ASSERT (thread->session_state [index] != NULL);
	ep_thread_session_state_free (thread->session_state [index]);
	thread->session_state [index] = NULL;
}

#ifdef EP_CHECKED_BUILD
void
ep_thread_requires_lock_held (const EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	ep_rt_spin_lock_requires_lock_held (&thread->rt_lock);
}

void
ep_thread_requires_lock_not_held (const EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	ep_rt_spin_lock_requires_lock_not_held (&thread->rt_lock);
}
#endif

/*
 * EventPipeThreadHolder.
 */

static
void
thread_holder_fini (EventPipeThreadHolder *thread_holder)
{
	EP_ASSERT (thread_holder != NULL);
	EP_ASSERT (thread_holder->thread != NULL);
	ep_thread_release (thread_holder->thread);
}

EventPipeThreadHolder *
ep_thread_holder_alloc (EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);

	EventPipeThreadHolder *instance = ep_rt_object_alloc (EventPipeThreadHolder);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_thread_holder_init (instance, thread) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_thread_holder_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeThreadHolder *
ep_thread_holder_init (
	EventPipeThreadHolder *thread_holder,
	EventPipeThread *thread)
{
	EP_ASSERT (thread_holder != NULL);
	EP_ASSERT (thread != NULL);

	thread_holder->thread = thread;
	ep_thread_addref (thread_holder->thread);

	return thread_holder;
}

void
ep_thread_holder_fini (EventPipeThreadHolder *thread_holder)
{
	ep_return_void_if_nok (thread_holder != NULL && thread_holder->thread != NULL);
	thread_holder_fini (thread_holder);
}

void
ep_thread_holder_free (EventPipeThreadHolder *thread_holder)
{
	ep_return_void_if_nok (thread_holder != NULL && thread_holder->thread != NULL);
	thread_holder_fini (thread_holder);
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
	ep_thread_session_state_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

uint32_t
ep_thread_session_state_get_buffer_count_estimate(const EventPipeThreadSessionState *thread_session_state)
{
	// this is specifically unprotected and allowed to be incorrect due to memory ordering
	//
	// buffer_list won't become NULL after getting this reference in the scope of this function.
	//
	// buffer_list is only set to NULL when the session is being freed
	// when this code won't be called.
	EventPipeBufferList *buffer_list = thread_session_state->buffer_list;
	return buffer_list == NULL ? 0 : buffer_list->buffer_count;
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

	EP_ASSERT ((thread_session_state->write_buffer == NULL) || (ep_rt_volatile_load_uint32_t (ep_buffer_get_state_cref (thread_session_state->write_buffer)) == EP_BUFFER_STATE_WRITABLE));
	return thread_session_state->write_buffer;
}

void
ep_thread_session_state_set_write_buffer (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBuffer *new_buffer)
{
	EP_ASSERT (thread_session_state != NULL);

	ep_thread_requires_lock_held (thread_session_state->thread_holder.thread);

	EP_ASSERT ((new_buffer == NULL) || (ep_rt_volatile_load_uint32_t (ep_buffer_get_state_cref (new_buffer)) == EP_BUFFER_STATE_WRITABLE));
	EP_ASSERT ((thread_session_state->write_buffer == NULL) || (ep_rt_volatile_load_uint32_t (ep_buffer_get_state_cref (thread_session_state->write_buffer)) == EP_BUFFER_STATE_WRITABLE));

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

	ep_rt_volatile_store_uint32_t (&thread_session_state->sequence_number, ep_rt_volatile_load_uint32_t (&thread_session_state->sequence_number) + 1);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_thread;
const char quiet_linker_empty_file_warning_eventpipe_thread = 0;
#endif
