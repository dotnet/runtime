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
	// An array of slots to hold per-session per-thread state.
	// A slot in this array should be non-NULL iff the same ThreadSessionState object is also contained in the
	// session->buffer_manager->thread_session_state_list.
	//
	// Thread-safety notes:
	// The pointers in this array are only modified under the buffer manager lock for whichever enabled session currently owns the slot.
	// (session->index == slot_index)
	// Both the per-thread slots here and the global slots in _ep_sessions can only be non-NULL during the time period when the
	// session is enabled. ep_disable() won't exit the global EP lock until all the slots for that session are NULL and
	// threads have exited code regions where they may have been using locally cached pointers retrieved through
	// the slots. This ensures threads neither retain stale pointers to a disabled session, nor will they be able to acquire a
	// stale pointer from the slot later. It also means no thread will be trying to synchronize using a stale session lock across
	// the transition when a new session takes over ownership of the slot.
	//
	// Writing a non-NULL pointer into the slot only occurs on the OS thread associated with this EventPipeThread object when
	// trying to write the first event into a session.
	// Writing a NULL pointer into the slot could occur on either:
	// 1. The session event flushing thread when is has finished flushing all the events for a thread that already exited.
	// 2. The thread which calls ep_disable() to disable the session.
	// 
	// Reading from this slot can either be done by taking the buffer manager lock, or by doing a volatile read of the pointer. The volatile
	// read should only occur when running on the OS thread associated with this EventPipeThread object. When reading under the lock the
	// pointer should not be retained past the scope of the lock. When using the volatile read, the pointer should not be retained
	// outside the period where session_use_in_progress == slot_number.
	volatile EventPipeThreadSessionState *session_state [EP_MAX_NUMBER_OF_SESSIONS];

#ifdef EP_THREAD_INCLUDE_ACTIVITY_ID
	uint8_t activity_id [EP_ACTIVITY_ID_SIZE];
#endif
	EventPipeSession *rundown_session;
	// This is initialized when the Thread object is first constructed and remains
	// immutable afterwards.
	uint64_t os_thread_id;
	// The EventPipeThreadHolder maintains one count while the thread is alive
	// Each EventPipeThreadSessionState maintains one count throughout its lifetime
	// Every SequencePoint tracking this thread also maintains one count
	int32_t ref_count;
	// If this is set to a valid id before the corresponding entry of sessions is set to null,
	// that pointer will be protected from deletion. See ep_disable () and
	// ep_write () for more detail.
	volatile uint32_t session_use_in_progress;
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
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	minipal_guid_v4_create ((GUID *)(activity_id));
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

void
ep_thread_set_session_use_in_progress (
	EventPipeThread *thread,
	uint32_t session_index);

uint32_t
ep_thread_get_session_use_in_progress (const EventPipeThread *thread);

// _Requires_lock_held (buffer_manager)
EventPipeThreadSessionState *
ep_thread_get_session_state (
	const EventPipeThread *thread,
	EventPipeSession *session);

EventPipeThreadSessionState *
ep_thread_get_volatile_session_state (
	const EventPipeThread *thread,
	EventPipeSession *session);

// _Requires_lock_held (buffer_manager)
void
ep_thread_set_session_state (
	EventPipeThread *thread,
	EventPipeSession *session,
	EventPipeThreadSessionState *thread_session_state);

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
	// The buffer this thread is allowed to write to. If non-null, it must
	// match the tail of buffer_list.
	// Modifications always occur under the buffer manager lock.
	// Non-null writes only occur on the thread this state belongs to.
	// Null writes may occur on the buffer manager event flushing thread.
	// Lock-free reads may occur only on the thread this state belongs to.
	EventPipeBuffer *write_buffer;
	// The list of buffers that were written to by this thread.
	// immutable
	EventPipeBufferList *buffer_list;
	// The sequence number of the last event that was read, only
	// updated/read by the reader thread.
	uint32_t last_read_sequence_number;
	// The number of events that were attempted to be written by this
	// thread. Each event was either successfully recorded in a buffer
	// or it was dropped.
	//
	// Only updated by the current thread.
	// Other event writer threads are allowed to do unsynchronized reads when
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

EP_DEFINE_GETTER(EventPipeThreadSessionState *, thread_session_state, EventPipeBufferList *, buffer_list)
EP_DEFINE_GETTER(EventPipeThreadSessionState *, thread_session_state, EventPipeSession *, session)
EP_DEFINE_GETTER(EventPipeThreadSessionState *, thread_session_state, uint32_t, last_read_sequence_number);
EP_DEFINE_SETTER(EventPipeThreadSessionState *, thread_session_state, uint32_t, last_read_sequence_number);

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

// _Requires_lock_held (buffer_manager)
EventPipeBuffer *
ep_thread_session_state_get_write_buffer (const EventPipeThreadSessionState *thread_session_state);

EventPipeBuffer *
ep_thread_session_state_get_volatile_write_buffer (const EventPipeThreadSessionState *thread_session_state);

// _Requires_lock_held (buffer_manager)
void
ep_thread_session_state_set_write_buffer (
	EventPipeThreadSessionState *thread_session_state,
	EventPipeBuffer *new_buffer);

uint32_t
ep_thread_session_state_get_volatile_sequence_number (const EventPipeThreadSessionState *thread_session_state);

void
ep_thread_session_state_increment_sequence_number (EventPipeThreadSessionState *thread_session_state);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_THREAD_H__ */
