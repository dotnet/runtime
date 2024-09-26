#ifndef __EVENTPIPE_THREAD_H__
#define __EVENTPIPE_THREAD_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_THREAD_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeThread.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_THREAD_GETTER_SETTER)
struct _EventPipeThread {
#else
struct _EventPipeThread_Internal {
#endif
	// Per-session state.
	// The pointers in this array are only read/written under rt_lock
	// Some of the data within the ThreadSessionState object can be accessed
	// without rt_lock however, see the fields of that type for details.
	EventPipeThreadSessionState *session_state [EP_MAX_NUMBER_OF_SESSIONS];
#ifdef EP_THREAD_INCLUDE_ACTIVITY_ID
	uint8_t activity_id [EP_ACTIVITY_ID_SIZE];
#endif
	EventPipeSession *rundown_session;
	// This lock is designed to have low contention. Normally it is only taken by this thread,
	// but occasionally it may also be taken by another thread which is trying to collect and drain
	// buffers from all threads.
	ep_rt_spin_lock_handle_t rt_lock;
	// This is initialized when the Thread object is first constructed and remains
	// immutable afterwards.
	uint64_t os_thread_id;
	// The EventPipeThreadHolder maintains one count while the thread is alive
	// and each session's EventPipeBufferList maintains one count while it
	// exists.
	int32_t ref_count;
	// If this is set to a valid id before the corresponding entry of sessions is set to null,
	// that pointer will be protected from deletion. See ep_disable () and
	// ep_write () for more detail.
	volatile uint32_t writing_event_in_progress;
	// This is set to non-zero when the thread is unregistered from the global list of EventPipe threads.
	// This should happen when a physical thread is ending.
	// This is a convenience marker to prevent us from having to search the global list.
	// defaults to false.
	volatile uint32_t unregistered;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_THREAD_GETTER_SETTER)
struct _EventPipeThread {
	uint8_t _internal [sizeof (struct _EventPipeThread_Internal)];
};
#endif

#ifdef EP_THREAD_INCLUDE_ACTIVITY_ID
EP_DEFINE_GETTER_ARRAY_REF(EventPipeThread *, thread, uint8_t *, const uint8_t *, activity_id, activity_id[0]);
#else
static
inline
const uint8_t *
ep_thread_get_activity_id_cref (ep_rt_thread_activity_id_handle_t activity_id_handle)
{
	return ep_rt_thread_get_activity_id_cref (activity_id_handle);
}
#endif

EP_DEFINE_GETTER(EventPipeThread *, thread, EventPipeSession *, rundown_session);
EP_DEFINE_SETTER(EventPipeThread *, thread, EventPipeSession *, rundown_session);
EP_DEFINE_GETTER_REF(EventPipeThread *, thread, ep_rt_spin_lock_handle_t *, rt_lock);
EP_DEFINE_GETTER(EventPipeThread *, thread, uint64_t, os_thread_id);
EP_DEFINE_GETTER_REF(EventPipeThread *, thread, int32_t *, ref_count);
EP_DEFINE_GETTER_REF(EventPipeThread *, thread, volatile uint32_t *, unregistered);

EventPipeThread *
ep_thread_alloc (void);

void
ep_thread_free (EventPipeThread *thread);

void
ep_thread_addref (EventPipeThread *thread);

void
ep_thread_release (EventPipeThread *thread);

void
ep_thread_init (void);

bool
ep_thread_register (EventPipeThread *thread);

bool
ep_thread_unregister (EventPipeThread *thread);

EventPipeThread *
ep_thread_get (void);

EventPipeThread *
ep_thread_get_or_create (void);

void
ep_thread_get_threads (dn_vector_ptr_t *threads);

static
inline
void
ep_thread_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	ep_rt_create_activity_id (activity_id, activity_id_len);
}

static
inline
ep_rt_thread_activity_id_handle_t
ep_thread_get_activity_id_handle (void)
{
	return ep_rt_thread_get_activity_id_handle ();
}

static
inline
void
ep_thread_get_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	ep_rt_thread_get_activity_id (activity_id_handle, activity_id, activity_id_len);
}

static
inline
void
ep_thread_set_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	const uint8_t *activity_id,
	uint32_t activity_id_len)
{
	ep_rt_thread_set_activity_id (activity_id_handle, activity_id, activity_id_len);
}

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
#define ep_thread_requires_lock_held(thread)
#define ep_thread_requires_lock_not_held(thread)
#endif

void
ep_thread_set_session_write_in_progress (
	EventPipeThread *thread,
	uint32_t session_index);

uint32_t
ep_thread_get_session_write_in_progress (const EventPipeThread *thread);

// _Requires_lock_held (thread)
EventPipeThreadSessionState *
ep_thread_get_or_create_session_state (
	EventPipeThread *thread,
	EventPipeSession *session);

// _Requires_lock_held (thread)
EventPipeThreadSessionState *
ep_thread_get_session_state (
	const EventPipeThread *thread,
	EventPipeSession *session);

// _Requires_lock_held (thread)
void
ep_thread_delete_session_state (
	EventPipeThread *thread,
	EventPipeSession *session);

/*
 * EventPipeThreadHolder.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_THREAD_GETTER_SETTER)
struct _EventPipeThreadHolder {
#else
struct _EventPipeThreadHolder_Internal {
#endif
	EventPipeThread *thread;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_THREAD_GETTER_SETTER)
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

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_THREAD_GETTER_SETTER)
struct _EventPipeThreadSessionState {
#else
struct _EventPipeThreadSessionState_Internal {
#endif
	// immutable.
	EventPipeThreadHolder thread_holder;
	// immutable.
	EventPipeSession *session;
	// The buffer this thread is allowed to write to if non-null, it must
	// match the tail of buffer_list
	// protected by thread_holder->thread.rt_lock
	EventPipeBuffer *write_buffer;
	// The list of buffers that were written to by this thread. This
	// is populated lazily the first time a thread tries to allocate
	// a buffer for this session. It is set back to null when
	// event writing is suspended during session disable.
	// protected by the buffer manager lock.
	// This field can be read outside the lock when
	// the buffer allocation logic is estimating how many
	// buffers a given thread has used (see: ep_thread_session_state_get_buffer_count_estimate and its uses).
	EventPipeBufferList *buffer_list;
#ifdef EP_CHECKED_BUILD
	// protected by the buffer manager lock.
	EventPipeBufferManager *buffer_manager;
#endif
	// The number of events that were attempted to be written by this
	// thread. Each event was either successfully recorded in a buffer
	// or it was dropped.
	//
	// Only updated by the current thread under thread_holder->thread.rt_lock. Other
	// event writer threads are allowed to do unsynchronized reads when
	// capturing a sequence point but this does not provide any consistency
	// guarantee. In particular there is no promise that the other thread
	// is observing the most recent sequence number, nor is there a promise
	// that the observable number of events in the write buffer matches the
	// sequence number. A writer thread will always update the sequence
	// number in tandem with an event write or drop, but without a write
	// barrier between those memory writes they might observed out-of-order
	// by the thread capturing the sequence point. The only utility this
	// unsynchronized read has is that if some other thread observes a sequence
	// number X, it knows this thread must have attempted to write at least
	// X events prior to the moment in time when the read occurred. If the event
	// buffers are later read and there are fewer than X events timestamped
	// prior to the sequence point we can be certain the others were dropped.
	volatile uint32_t sequence_number;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_THREAD_GETTER_SETTER)
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

uint32_t
ep_thread_session_state_get_buffer_count_estimate(const EventPipeThreadSessionState *thread_session_state);

// _Requires_lock_held (thread)
EventPipeBuffer *
ep_thread_session_state_get_write_buffer (const EventPipeThreadSessionState *thread_session_state);

// _Requires_lock_held (thread)
void
ep_thread_session_state_set_write_buffer (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBuffer *new_buffer);

// _Requires_lock_held (buffer_manager)
EventPipeBufferList *
ep_thread_session_state_get_buffer_list (const EventPipeThreadSessionState *thread_session_state);

// _Requires_lock_held (buffer_manager)
void
ep_thread_session_state_set_buffer_list (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBufferList *new_buffer_list);

uint32_t
ep_thread_session_state_get_volatile_sequence_number (const EventPipeThreadSessionState *thread_session_state);

// _Requires_lock_held (thread)
uint32_t
ep_thread_session_state_get_sequence_number (const EventPipeThreadSessionState *thread_session_state);

// _Requires_lock_held (thread)
void
ep_thread_session_state_increment_sequence_number (EventPipeThreadSessionState *thread_session_state);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_THREAD_H__ */
