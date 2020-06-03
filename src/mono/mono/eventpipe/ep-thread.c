#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep.h"

/*
 * EventPipeThread.
 */

void
ep_thread_addref (EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	ep_rt_atomic_inc_int32_t (ep_thread_get_ref_count_ref (thread));
}

void
ep_thread_release (EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	if (ep_rt_atomic_dec_int32_t (ep_thread_get_ref_count_ref (thread)) == 0)
		ep_thread_free (thread);
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
ep_thread_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	ep_return_void_if_nok (activity_id != NULL);
	ep_rt_create_activity_id (activity_id, activity_id_len);
}

void
ep_thread_get_activity_id (
	EventPipeThread *thread,
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	ep_return_void_if_nok (thread != NULL && activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);
	memcpy (activity_id, ep_thread_get_activity_id_cref (thread), EP_ACTIVITY_ID_SIZE);
}

void
ep_thread_set_activity_id (
	EventPipeThread *thread,
	const uint8_t *activity_id,
	uint32_t activity_id_len)
{
	ep_return_void_if_nok (thread != NULL && activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);
	memcpy (ep_thread_get_activity_id_ref (thread), activity_id, EP_ACTIVITY_ID_SIZE);
}

void
ep_thread_set_session_write_in_progress (
	EventPipeThread *thread,
	uint32_t session_index)
{
	ep_return_void_if_nok (thread != NULL);
	ep_rt_volatile_store_uint32_t (ep_thread_get_writing_event_in_progress_ref (thread), session_index);
}

uint32_t
ep_thread_get_session_write_in_progress (const EventPipeThread *thread)
{
	ep_return_zero_if_nok (thread != NULL);
	return ep_rt_volatile_load_uint32_t (ep_thread_get_writing_event_in_progress_cref (thread));
}

EventPipeThreadSessionState *
ep_thread_get_or_create_session_state (
	EventPipeThread *thread,
	EventPipeSession *session)
{
	ep_return_null_if_nok (thread != NULL && session != NULL);
	EP_ASSERT (ep_session_get_index (session) < EP_MAX_NUMBER_OF_SESSIONS);
	ep_thread_requires_lock_held (thread);

	EventPipeThreadSessionState *state = ep_thread_get_session_state_ref (thread)[ep_session_get_index (session)];
	if (!state) {
		state = ep_thread_session_state_alloc (thread, session, ep_session_get_buffer_manager (session));
		ep_thread_get_session_state_ref (thread)[ep_session_get_index (session)] = state;
	}

	return state;
}

EventPipeThreadSessionState *
ep_thread_get_session_state (
	const EventPipeThread *thread,
	EventPipeSession *session)
{
	ep_return_null_if_nok (thread != NULL && session != NULL);
	EP_ASSERT (ep_session_get_index (session) < EP_MAX_NUMBER_OF_SESSIONS);
	ep_thread_requires_lock_held (thread);

	EventPipeThreadSessionState *const state = ep_thread_get_session_state_cref (thread)[ep_session_get_index (session)];
	EP_ASSERT (state != NULL);
	return state;
}

void
ep_thread_delete_session_state (
	EventPipeThread *thread,
	EventPipeSession *session)
{
	ep_return_void_if_nok (thread != NULL && session != NULL);
	ep_thread_requires_lock_held (thread);

	uint32_t index = ep_session_get_index (session);
	EP_ASSERT (index < EP_MAX_NUMBER_OF_SESSIONS);
	EventPipeThreadSessionState *state = ep_thread_get_session_state_ref (thread)[index];
	EP_ASSERT (state != NULL);
	ep_thread_session_state_free (state);
	ep_thread_get_session_state_ref (thread)[index] = NULL;
}

#ifdef EP_CHECKED_BUILD
void
ep_thread_requires_lock_held (const EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	ep_rt_spin_lock_requires_lock_held (ep_thread_get_rt_lock_cref (thread));
}

void
ep_thread_requires_lock_not_held (const EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	ep_rt_spin_lock_requires_lock_not_held (ep_thread_get_rt_lock_cref (thread));
}
#endif

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#ifndef EP_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_eventpipe_thread;
const char quiet_linker_empty_file_warning_eventpipe_thread = 0;
#endif
