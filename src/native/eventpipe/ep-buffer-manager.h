#ifndef __EVENTPIPE_BUFFERMANAGER_H__
#define __EVENTPIPE_BUFFERMANAGER_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_BUFFER_MANAGER_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeBufferList.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BUFFER_MANAGER_GETTER_SETTER)
struct _EventPipeBufferList {
#else
struct _EventPipeBufferList_Internal {
#endif
	// The thread which writes to the buffers in this list.
	EventPipeThreadHolder thread_holder;
	// The buffer manager that owns this list.
	EventPipeBufferManager *manager;
	// Buffers are stored in an intrusive linked-list from oldest to newest.
	// Head is the oldest buffer. Tail is the newest (and currently used) buffer.
	EventPipeBuffer *head_buffer;
	EventPipeBuffer *tail_buffer;
	// The number of buffers in the list.
	uint32_t buffer_count;
	// The sequence number of the last event that was read, only
	// updated/read by the reader thread.
	uint32_t last_read_sequence_number;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BUFFER_MANAGER_GETTER_SETTER)
struct _EventPipeBufferList {
	uint8_t _internal [sizeof (struct _EventPipeBufferList_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(EventPipeBufferList *, buffer_list, EventPipeThreadHolder *, thread_holder);

static
inline
EventPipeThread *
ep_buffer_list_get_thread (EventPipeBufferList *buffer_list)
{
	return ep_thread_holder_get_thread (ep_buffer_list_get_thread_holder_cref (buffer_list));
}

EventPipeBufferList *
ep_buffer_list_alloc (
	EventPipeBufferManager *manager,
	EventPipeThread *thread);

EventPipeBufferList *
ep_buffer_list_init (
	EventPipeBufferList *buffer_list,
	EventPipeBufferManager *manager,
	EventPipeThread *thread);

void
ep_buffer_list_fini (EventPipeBufferList *buffer_list);

void
ep_buffer_list_free (EventPipeBufferList *buffer_list);

void
ep_buffer_list_insert_tail (
	EventPipeBufferList *buffer_list,
	EventPipeBuffer *buffer);

EventPipeBuffer *
ep_buffer_list_get_and_remove_head (EventPipeBufferList *buffer_list);

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_list_ensure_consistency (EventPipeBufferList *buffer_list);
#endif

/*
 * EventPipeBufferManagerEventHeap
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BUFFER_MANAGER_GETTER_SETTER)
struct _EventPipeBufferManagerEventHeap {
#else
struct _EventPipeBufferManagerEventHeap_Internal {
#endif
	// The underlying min heap consisting of EventPipeBufferManagerEventHeapNode pointers
	dn_vector_ptr_t *heap;
	// The set of weak reference EventPipeThreadSessionStates whose EventPipeBuffer is currently owned by the heap.
	// It allows the reader thread to quickly iterate over the buffer_manager's thread_session_state_list
	// to discover new buffers to add to the min heap, since only one buffer per EventPipeThreadSessionState's
	// BufferList needs to be read to grab the earliest EventPipeEventInstance.
	// These are weak references to facilitate EventPipeThreadSessionState cleanup.
	// An EventPipeThreadSessionState is untracked once its corresponding buffer is exhausted and removed from the heap.
	// An EventPipeThreadSessionState is added once its EventPipeBuffer is transferred to the heap
	dn_umap_t *tracked_thread_session_states;
	// The last time the min-heap consulted the buffer_manager's thread_session_state_list for new nodes.
	ep_timestamp_t last_update;
};

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BUFFER_MANAGER_GETTER_SETTER)
struct _EventPipeBufferManagerEventHeapNode {
#else
struct _EventPipeBufferManagerEventHeapNode_Internal {
#endif
	// The earliest EventPipeBuffer written by an EventPipeThreadSessionState in the buffer manager's list.
	// Ownership is transferred from the corresponding EventPipeThreadSessionState's EventPipeBufferList.
	// The buffer's current_read_event's timestamp is used to heapify the min-heap.
	// Once this node is at the root of the heap, the current_read_event remains valid until the following call to
	// ep_buffer_manager_get_next_event which advances this buffer's read cursor and one of the following occurs:
	// 1) The buffer contains another event, so this buffer is reheapified
	// 2) The buffer is exhausted
	//    - The EventPipeThreadSessionState is removed from tracked_thread_session_states
	//    - Extract min frees this node, freeing the EventPipeBuffer and allowing the buffer manager to reclaim the buffer size
	//    - The heap is reaheapified
	EventPipeBuffer *buffer;
	// Unique identifier for the EventPipeThread that wrote into the buffer, tracked by tracked_thread_session_states
	// so the reader thread can quickly scan the buffer manager's ThreadSessionState list for new candidate nodes.
	// This is a weak reference as the EventPipeThreadSessionState may be deleted while this node exists.
	EventPipeThreadSessionState *thread_session_state;
};

/*
 * EventPipeBufferManager.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BUFFER_MANAGER_GETTER_SETTER)
struct _EventPipeBufferManager {
#else
struct _EventPipeBufferManager_Internal {
#endif
	// A list of per-thread session state
	// Each entry in this list represents the session state owned by a single thread
	// which includes the list of buffers the thread has written and its current
	// event sequence number. The EventPipeThread object also has a pointer to the
	// session state contained in this list. This ensures that each thread can access
	// its own data, while at the same time, ensuring that when a thread is destroyed,
	// we keep the buffers around without having to perform any migration or
	// book-keeping.
	dn_list_t *thread_session_state_list;
	// A queue of sequence points.
	dn_list_t *sequence_points;
	// Event for synchronizing real time reading.
	ep_rt_wait_event_handle_t rt_wait_event;
	// Lock to protect access to the per-thread buffer list and total allocation size.
	ep_rt_spin_lock_handle_t rt_lock;
	// The session this buffer manager belongs to.
	EventPipeSession *session;
	// Iterator state for reader thread.
	// These are not protected by rt_lock and expected to only be used on the reader thread.
	EventPipeEventInstance *current_event;
	EventPipeBuffer *current_buffer;
	EventPipeBufferList *current_buffer_list;
	// The Event min heap for the reader thread
	EventPipeBufferManagerEventHeap *event_heap;
	// The total allocation size of buffers under management.
	volatile size_t size_of_all_buffers;
	// The maximum allowable size of buffers under management.
	// Attempted allocations above this threshold result in
	// dropped events.
	size_t max_size_of_all_buffers;
	// The amount of allocations we can do at this moment before
	// triggering a sequence point
	size_t remaining_sequence_point_alloc_budget;
	// The total amount of allocations we can do after one sequence
	// point before triggering the next one
	size_t sequence_point_alloc_budget;
	// number of times an event was dropped due to it being too
	// large to fit in the 64KB size limit
	volatile int64_t num_oversized_events_dropped;

#ifdef EP_CHECKED_BUILD
	volatile int64_t num_events_stored;
	volatile int64_t num_events_dropped;
	int64_t num_events_written;
	uint32_t num_buffers_allocated;
	uint32_t num_buffers_stolen;
	uint32_t num_buffers_leaked;
#endif
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BUFFER_MANAGER_GETTER_SETTER)
struct _EventPipeBufferManager {
	uint8_t _internal [sizeof (struct _EventPipeBufferManager_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(EventPipeBufferManager *, buffer_manager, ep_rt_wait_event_handle_t *, rt_wait_event)

EventPipeBufferManager *
ep_buffer_manager_alloc (
	EventPipeSession *session,
	size_t max_size_of_all_buffers,
	size_t sequence_point_allocation_budget);

void
ep_buffer_manager_free (EventPipeBufferManager *buffer_manager);

#ifdef EP_CHECKED_BUILD
void
ep_buffer_manager_requires_lock_held (const EventPipeBufferManager *buffer_manager);

void
ep_buffer_manager_requires_lock_not_held (const EventPipeBufferManager *buffer_manager);
#else
#define ep_buffer_manager_requires_lock_held(buffer_manager)
#define ep_buffer_manager_requires_lock_not_held(buffer_manager)
#endif

// Inits a sequence point that has the list of current threads and sequence numbers.
void
ep_buffer_manager_init_sequence_point_thread_list (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint *sequence_point);

// Write an event to the input thread's current event buffer.
// An optional event_thread can be provided for sample profiler events.
// This is because the thread that writes the events is not the same as the "event thread".
// An optional stack trace can be provided for sample profiler events.
// Otherwise, if a stack trace is needed, one will be automatically collected.
bool
ep_buffer_manager_write_event (
	EventPipeBufferManager *buffer_manager,
	ep_rt_thread_handle_t thread,
	EventPipeSession *session,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	ep_rt_thread_handle_t event_thread,
	EventPipeStackContents *stack);

// READ_ONLY state and no new EventPipeBuffers or EventPipeBufferLists can be created. Calls to
// write_event that start during the suspension period or were in progress but hadn't yet recorded
// their event into a buffer before the start of the suspension period will return false and the
// event will not be recorded. Any events that not recorded as a result of this suspension will be
// treated the same as events that were not recorded due to configuration.
// EXPECTED USAGE: First the caller will disable all events via configuration, then call
// suspend_write_event () to force any write_event calls that may still be in progress to either
// finish or cancel. After that all BufferLists and Buffers can be safely drained and/or deleted.
// _Requires_lock_held (ep)
void
ep_buffer_manager_suspend_write_event (
	EventPipeBufferManager *buffer_manager,
	uint32_t session_index);

// Write the contents of the managed buffers to the specified file.
// The stop_timeStamp is used to determine when tracing was stopped to ensure that we
// skip any events that might be partially written due to races when tracing is stopped.
void
ep_buffer_manager_write_all_buffers_to_file (
	EventPipeBufferManager *buffer_manager,
	EventPipeFile *file,
	ep_timestamp_t stop_timestamp,
	bool *events_written);

void
ep_buffer_manager_write_all_buffers_to_file_v3 (
	EventPipeBufferManager *buffer_manager,
	EventPipeFile *file,
	ep_timestamp_t stop_timestamp,
	bool *events_written);

void
ep_buffer_manager_write_all_buffers_to_file_v4 (
	EventPipeBufferManager *buffer_manager,
	EventPipeFile *file,
	ep_timestamp_t stop_timestamp,
	bool *events_written);

// Get next event. This is used to dispatch events to EventListener.
EventPipeEventInstance *
ep_buffer_manager_get_next_event (EventPipeBufferManager *buffer_manager);

// Attempt to de-allocate resources as best we can.  It is possible for some buffers to leak because
// threads can be in the middle of a write operation and get blocked, and we may not get an opportunity
// to free their buffer for a very long time.
void
ep_buffer_manager_deallocate_buffers (EventPipeBufferManager *buffer_manager);

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_manager_ensure_consistency (EventPipeBufferManager *buffer_manager);
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_BUFFERMANAGER_H__ */
