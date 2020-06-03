#ifndef __EVENTPIPE_THREAD_H__
#define __EVENTPIPE_THREAD_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeThread.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeThread {
#else
struct _EventPipeThread_Internal {
#endif
	EventPipeThreadSessionState *session_state [EP_MAX_NUMBER_OF_SESSIONS];
	uint8_t activity_id [EP_ACTIVITY_ID_SIZE];
	EventPipeSession *rundown_session;
	size_t os_thread_id;
	ep_rt_thread_handle_t rt_thread;
	ep_rt_spin_lock_handle_t rt_lock;
	int32_t ref_count;
	volatile uint32_t writing_event_in_progress;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeThread {
	uint8_t _internal [sizeof (struct _EventPipeThread_Internal)];
};
#endif

EP_DEFINE_GETTER_ARRAY_REF(EventPipeThread *, thread, EventPipeThreadSessionState **, EventPipeThreadSessionState *const*, session_state, session_state[0]);
EP_DEFINE_GETTER_ARRAY_REF(EventPipeThread *, thread, uint8_t *, const uint8_t *, activity_id, activity_id[0]);
EP_DEFINE_GETTER(EventPipeThread *, thread, EventPipeSession *, rundown_session);
EP_DEFINE_SETTER(EventPipeThread *, thread, EventPipeSession *, rundown_session);
EP_DEFINE_GETTER(EventPipeThread *, thread, size_t, os_thread_id);
EP_DEFINE_GETTER_REF(EventPipeThread *, thread, ep_rt_spin_lock_handle_t *, rt_lock);
EP_DEFINE_GETTER_REF(EventPipeThread *, thread, int32_t *, ref_count);
EP_DEFINE_GETTER_REF(EventPipeThread *, thread, volatile uint32_t *, writing_event_in_progress);

EventPipeThread *
ep_thread_alloc (void);

void
ep_thread_free (EventPipeThread *thread);

void
ep_thread_addref (EventPipeThread *thread);

void
ep_thread_release (EventPipeThread *thread);

EventPipeThread *
ep_thread_get (void);

EventPipeThread *
ep_thread_get_or_create (void);

void
ep_thread_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len);

void
ep_thread_get_activity_id (
	EventPipeThread *thread,
	uint8_t *activity_id,
	uint32_t activity_id_len);

void
ep_thread_set_activity_id (
	EventPipeThread *thread,
	const uint8_t *activity_id,
	uint32_t activity_id_len);

static
inline
void
ep_thread_set_as_rundown_thread (
	EventPipeThread *thread,
	EventPipeSession *session)
{
	EP_ASSERT (thread != NULL);
	ep_thread_set_rundown_session (thread, session);
}

static
inline
bool
ep_thread_is_rundown_thread (const EventPipeThread *thread)
{
	EP_ASSERT (thread != NULL);
	return (ep_thread_get_rundown_session (thread) != NULL);
}

#ifdef EP_CHECKED_BUILD
void
ep_thread_requires_lock_held (const EventPipeThread *thread);

void
ep_thread_requires_lock_not_held (const EventPipeThread *thread);
#else
#define ep_thread_requires_lock_held(x)
#define ep_thread_requires_lock_not_held(x)
#endif

void
ep_thread_set_session_write_in_progress (
	EventPipeThread *thread,
	uint32_t session_index);

uint32_t
ep_thread_get_session_write_in_progress (const EventPipeThread *thread);

EventPipeThreadSessionState *
ep_thread_get_or_create_session_state (
	EventPipeThread *thread,
	EventPipeSession *session);

EventPipeThreadSessionState *
ep_thread_get_session_state (
	const EventPipeThread *thread,
	EventPipeSession *session);

void
ep_thread_delete_session_state (
	EventPipeThread *thread,
	EventPipeSession *session);

/*
 * EventPipeThreadHolder.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeThreadHolder {
#else
struct _EventPipeThreadHolder_Internal {
#endif
	EventPipeThread *thread;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeThreadHolder {
	uint8_t _internal [sizeof (struct _EventPipeThreadHolder_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeThreadHolder *, thread_holder, EventPipeThread *, thread)

EventPipeThreadHolder *
ep_thread_holder_alloc (EventPipeThread *thread);

EventPipeThreadHolder *
ep_thread_holder_init (
	EventPipeThreadHolder *thread_holder,
	EventPipeThread *thread);

void
ep_thread_holder_fini (EventPipeThreadHolder *thread_holder);

void
ep_thread_holder_free (EventPipeThreadHolder *thread_holder);

/*
 * EventPipeThreadSessionState.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeThreadSessionState {
#else
struct _EventPipeThreadSessionState_Internal {
#endif
	EventPipeThreadHolder thread_holder;
	EventPipeSession *session;
	EventPipeBuffer *write_buffer;
	EventPipeBufferList *buffer_list;
#ifdef EP_CHECKED_BUILD
	EventPipeBufferManager *buffer_manager;
#endif
	volatile uint32_t sequence_number;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeThreadSessionState {
	uint8_t _internal [sizeof (struct _EventPipeThreadSessionState_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(EventPipeThreadSessionState *, thread_session_state, EventPipeThreadHolder *, thread_holder)
EP_DEFINE_GETTER(EventPipeThreadSessionState *, thread_session_state, EventPipeSession *, session)

EventPipeThreadSessionState *
ep_thread_session_state_alloc (
	EventPipeThread *thread,
	EventPipeSession *session,
	EventPipeBufferManager *buffer_manager);

void
ep_thread_session_state_free (EventPipeThreadSessionState *thread_session_state);

EventPipeThread *
ep_thread_session_state_get_thread (const EventPipeThreadSessionState *thread_session_state);

EventPipeBuffer *
ep_thread_session_state_get_write_buffer (const EventPipeThreadSessionState *thread_session_state);

void
ep_thread_session_state_set_write_buffer (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBuffer *new_buffer);

EventPipeBufferList *
ep_thread_session_state_get_buffer_list (const EventPipeThreadSessionState *thread_session_state);

void
ep_thread_session_state_set_buffer_list (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBufferList *new_buffer_list);

uint32_t
ep_thread_session_state_get_volatile_sequence_number (const EventPipeThreadSessionState *thread_session_state);

uint32_t
ep_thread_session_state_get_sequence_number (const EventPipeThreadSessionState *thread_session_state);

void
ep_thread_session_state_increment_sequence_number (EventPipeThreadSessionState *thread_session_state);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_THREAD_H__ **/
