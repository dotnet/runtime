#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_BUFFER_MANAGER_GETTER_SETTER
#include "ep.h"
#include "ep-buffer.h"
#include "ep-buffer-manager.h"
#include "ep-event.h"
#include "ep-event-instance.h"
#include "ep-event-payload.h"
#include "ep-file.h"
#include "ep-session.h"
#include "ep-stack-contents.h"

#define EP_MAX(a,b) (((a) > (b)) ? (a) : (b))
#define EP_MIN(a,b) (((a) < (b)) ? (a) : (b))
#define EP_CLAMP(min,value,max) (EP_MIN(EP_MAX(min, value), max))

/*
 * Forward declares of all static functions.
 */

static
void
buffer_list_fini (EventPipeBufferList *buffer_list);

// _Requires_lock_held (buffer_manager)
static
bool
buffer_manager_enqueue_sequence_point (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint *sequence_point);

// _Requires_lock_held (buffer_manager)
static
void
buffer_manager_init_sequence_point_thread_list (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint *sequence_point);

// _Requires_lock_held (buffer_manager)
static
void
buffer_manager_dequeue_sequence_point (EventPipeBufferManager *buffer_manager);

// _Requires_lock_held (buffer_manager)
static
bool
buffer_manager_try_peek_sequence_point (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint **sequence_point);

// Allocate a new buffer for the specified thread.
// This function will store the buffer in the thread's buffer list for future use and also return it here.
// A NULL return value means that a buffer could not be allocated.
static
EventPipeBuffer *
buffer_manager_allocate_buffer_for_thread (
	EventPipeBufferManager *buffer_manager,
	EventPipeThreadSessionState *thread_session_state,
	uint32_t request_size,
	bool *write_suspended);

static
void
buffer_manager_deallocate_buffer (
	EventPipeBufferManager *buffer_manager,
	EventPipeBuffer *buffer);

// Attempt to reserve space for a buffer
static
bool
buffer_manager_try_reserve_buffer (
	EventPipeBufferManager *buffer_manager,
	uint32_t request_size);

// Release a reserved buffer budget
static
void
buffer_manager_release_buffer (
	EventPipeBufferManager *buffer_manager,
	uint32_t size);

// An iterator that can enumerate all the events which have been written into this buffer manager.
// Initially the iterator starts uninitialized and get_current_event () returns NULL. The iterator
// operates on the buffer_manager's thread_session_state_list_snapshot, and calling move_next_xxx ()
// attempts to advance the cursor to the next event. If the current thread has no event prior to
// the snapshot_timestamp, the current_thread_session_state is removed from the snapshot list and
// get_current_event () returns NULL. The event pointer returned by get_current_event() is valid
// until move_next_xxx() is called again. Once all events in a buffer have been read the iterator
// will delete that buffer from the pool. Once all buffers from a thread have been read and the
// thread has been unregistered, the EventPipeThreadSessionState is cleaned up and removed.

static
void
buffer_manager_snapshot_threads (
	EventPipeBufferManager *buffer_manager,
	ep_timestamp_t snapshot_timestamp);

// Moves to the next oldest event searching across all snapshot threads. If there is no event older than
// the buffer_manager->snapshot_timestamp then get_current_event() will return NULL.
static
void
buffer_manager_move_next_event_any_snapshot_thread (EventPipeBufferManager *buffer_manager);

// Moves to the next oldest event from the same thread as the current event. If there is no event
// older than snapshot_timestamp then GetCurrentEvent() will return NULL. This should only be called
// when GetCurrentEvent() is non-null (because we need to know what thread's events to iterate)
static
void
buffer_manager_move_next_event_same_thread (EventPipeBufferManager *buffer_manager);

// Finds the first buffer in an EventPipeThreadSessionState's buffer list with an event prior to before_timestamp.
// Should any empty buffers be encountered along the way, they are actively removed, and callers are notified
// if the EventPipeThreadSessionState is exhausted.
static
EventPipeBuffer *
buffer_manager_advance_to_non_empty_buffer (
	EventPipeBufferManager *buffer_manager,
	EventPipeThreadSessionState *thread_session_state,
	ep_timestamp_t before_timestamp,
	bool *thread_session_state_exhausted);

// Detaches this buffer from an active writer thread and marks it read-only so that the reader
// thread can use it.
static
void
buffer_manager_convert_buffer_to_read_only (
	EventPipeBufferManager *buffer_manager,
	EventPipeBuffer *new_read_buffer);

static
void
buffer_manager_remove_and_delete_thread_session_state (
	EventPipeBufferManager *buffer_manager,
	EventPipeThreadSessionState *thread_session_state);

/*
 * EventPipeBufferList.
 */

static
void
buffer_list_fini (EventPipeBufferList *buffer_list)
{
	EP_ASSERT (buffer_list != NULL);
}

EventPipeBufferList *
ep_buffer_list_alloc (EventPipeBufferManager *manager)
{
	EventPipeBufferList *instance = ep_rt_object_alloc (EventPipeBufferList);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_buffer_list_init (instance, manager) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_buffer_list_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeBufferList *
ep_buffer_list_init (
	EventPipeBufferList *buffer_list,
	EventPipeBufferManager *manager)
{
	EP_ASSERT (buffer_list != NULL);
	EP_ASSERT (manager != NULL);

	buffer_list->manager = manager;
	buffer_list->head_buffer = NULL;
	buffer_list->tail_buffer = NULL;
	buffer_list->buffer_count = 0;

	return buffer_list;
}

void
ep_buffer_list_fini (EventPipeBufferList *buffer_list)
{
	ep_return_void_if_nok (buffer_list != NULL);
	buffer_list_fini (buffer_list);
}

void
ep_buffer_list_free (EventPipeBufferList *buffer_list)
{
	ep_return_void_if_nok (buffer_list != NULL);
	buffer_list_fini (buffer_list);
	ep_rt_object_free (buffer_list);
}

void
ep_buffer_list_insert_tail (
	EventPipeBufferList *buffer_list,
	EventPipeBuffer *buffer)
{
	ep_return_void_if_nok (buffer_list != NULL);

	EP_ASSERT (buffer != NULL);
	EP_ASSERT (ep_buffer_list_ensure_consistency (buffer_list));

	// Ensure that the input buffer didn't come from another list that was improperly cleaned up.
	EP_ASSERT ((ep_buffer_get_next_buffer (buffer) == NULL) && (ep_buffer_get_prev_buffer (buffer) == NULL));

	// First node in the list.
	if (buffer_list->tail_buffer == NULL) {
		buffer_list->head_buffer = buffer_list->tail_buffer = buffer;
	} else {
		// Set links between the old and new tail nodes.
		ep_buffer_set_next_buffer (buffer_list->tail_buffer, buffer);
		ep_buffer_set_prev_buffer (buffer, buffer_list->tail_buffer);

		// Set the new tail node.
		buffer_list->tail_buffer = buffer;
	}

	buffer_list->buffer_count++;

	EP_ASSERT (ep_buffer_list_ensure_consistency (buffer_list));
}

EventPipeBuffer *
ep_buffer_list_get_and_remove_head (EventPipeBufferList *buffer_list)
{
	ep_return_null_if_nok (buffer_list != NULL);

	EP_ASSERT (ep_buffer_list_ensure_consistency (buffer_list));

	EventPipeBuffer *ret_buffer = NULL;
	if (buffer_list->head_buffer != NULL)
	{
		// Save the head node.
		ret_buffer = buffer_list->head_buffer;

		// Set the new head node.
		buffer_list->head_buffer = ep_buffer_get_next_buffer (buffer_list->head_buffer);

		// Update the head node's previous pointer.
		if (buffer_list->head_buffer != NULL)
			ep_buffer_set_prev_buffer (buffer_list->head_buffer, NULL);
		else
			// We just removed the last buffer from the list.
			// Make sure both head and tail pointers are NULL.
			buffer_list->tail_buffer = NULL;

		// Clear the next pointer of the old head node.
		ep_buffer_set_next_buffer (ret_buffer, NULL);

		// Ensure that the old head node has no dangling references.
		EP_ASSERT ((ep_buffer_get_next_buffer (ret_buffer) == NULL) && (ep_buffer_get_prev_buffer (ret_buffer) == NULL));

		// Decrement the count of buffers in the list.
		buffer_list->buffer_count--;
	}

	EP_ASSERT (ep_buffer_list_ensure_consistency (buffer_list));

	return ret_buffer;
}

bool
buffer_manager_try_reserve_buffer (
	EventPipeBufferManager *buffer_manager,
	uint32_t request_size)
{
	uint64_t iters = 0;
	size_t old_size_of_all_buffers;
	size_t new_size_of_all_buffers;
	do {
		old_size_of_all_buffers = buffer_manager->size_of_all_buffers;
		new_size_of_all_buffers = old_size_of_all_buffers + request_size;
		iters++;
		if (iters % 64 == 0) {
			ep_rt_thread_sleep (0); // yield the thread to the scheduler in case we're in high contention
		}
	} while (new_size_of_all_buffers <= buffer_manager->max_size_of_all_buffers && ep_rt_atomic_compare_exchange_size_t (&buffer_manager->size_of_all_buffers, old_size_of_all_buffers, new_size_of_all_buffers) != old_size_of_all_buffers);

	return new_size_of_all_buffers <= buffer_manager->max_size_of_all_buffers;
}

void
buffer_manager_release_buffer (
	EventPipeBufferManager *buffer_manager,
	uint32_t size)
{
	uint64_t iters = 0;
	size_t old_size_of_all_buffers;
	size_t new_size_of_all_buffers;
	do {
		old_size_of_all_buffers = buffer_manager->size_of_all_buffers;
		new_size_of_all_buffers = old_size_of_all_buffers - size;
		iters++;
		if (iters % 64 == 0) {
			ep_rt_thread_sleep (0); // yield the thread to the scheduler in case we're in high contention
		}
	} while (new_size_of_all_buffers >= 0 && ep_rt_atomic_compare_exchange_size_t (&buffer_manager->size_of_all_buffers, old_size_of_all_buffers, new_size_of_all_buffers) != old_size_of_all_buffers);
}

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_list_ensure_consistency (EventPipeBufferList *buffer_list)
{
	// Either the head and tail nodes are both NULL or both are non-NULL.
	EP_ASSERT ((buffer_list->head_buffer == NULL && buffer_list->tail_buffer == NULL) ||
		(buffer_list->head_buffer != NULL && buffer_list->tail_buffer != NULL));

	// If the list is NULL, check the count and return.
	if (buffer_list->head_buffer == NULL) {
		EP_ASSERT (buffer_list->buffer_count == 0);
		return true;
	}

	// If the list is non-NULL, walk the list forward until we get to the end.
	uint32_t node_count = (buffer_list->head_buffer != NULL) ? 1 : 0;
	EventPipeBuffer *iterator = buffer_list->head_buffer;
	while (ep_buffer_get_next_buffer (iterator) != NULL) {
		iterator = ep_buffer_get_next_buffer (iterator);
		node_count++;

		// Check for consistency of the buffer itself.
		// NOTE: We can't check the last buffer because the owning thread could
		// be writing to it, which could result in false asserts.
		if (ep_buffer_get_next_buffer (iterator) != NULL)
			EP_ASSERT (ep_buffer_ensure_consistency (iterator));

		// Check for cycles.
		EP_ASSERT (node_count <= buffer_list->buffer_count);
	}

	// When we're done with the walk, pIter must point to the tail node.
	EP_ASSERT (iterator == buffer_list->tail_buffer);

	// Node count must equal the buffer count.
	EP_ASSERT (node_count == buffer_list->buffer_count);

	// Now, walk the list in reverse.
	iterator = buffer_list->tail_buffer;
	node_count = (buffer_list->tail_buffer != NULL) ? 1 : 0;
	while (ep_buffer_get_prev_buffer (iterator) != NULL) {
		iterator = ep_buffer_get_prev_buffer (iterator);
		node_count++;

		// Check for cycles.
		EP_ASSERT (node_count <= buffer_list->buffer_count);
	}

	// When we're done with the reverse walk, pIter must point to the head node.
	EP_ASSERT (iterator == buffer_list->head_buffer);

	// Node count must equal the buffer count.
	EP_ASSERT (node_count == buffer_list->buffer_count);

	// We're done.
	return true;
}
#endif

/*
 * EventPipeBufferManager.
 */

static
bool
buffer_manager_enqueue_sequence_point (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (sequence_point != NULL);

	ep_buffer_manager_requires_lock_held (buffer_manager);

	return dn_list_push_back (buffer_manager->sequence_points, sequence_point);
}

static
void
buffer_manager_init_sequence_point_thread_list (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (sequence_point != NULL);

	ep_buffer_manager_requires_lock_held (buffer_manager);

	DN_LIST_FOREACH_BEGIN (EventPipeThreadSessionState *, thread_session_state, buffer_manager->thread_session_state_list) {
		EventPipeThread *thread = ep_thread_session_state_get_thread (thread_session_state);

		// The sequence number captured here is not guaranteed to be the most recent sequence number, nor
		// is it guaranteed to match the number of events we would observe in the thread's write buffer
		// memory. This is only used as a lower bound on the number of events the thread has attempted to
		// write at the timestamp we will capture below.
		//
		// The sequence number is the value that will be used by the next event, so the last written
		// event is one less. Sequence numbers are allowed to overflow, so going backwards is allowed to
		// underflow.
		uint32_t sequence_number = ep_thread_session_state_get_volatile_sequence_number (thread_session_state) - 1;

		dn_umap_ptr_uint32_insert (ep_sequence_point_get_thread_sequence_numbers (sequence_point), thread, sequence_number);
		ep_thread_addref (thread);
	} DN_LIST_FOREACH_END;

	// This needs to come after querying the thread sequence numbers to ensure that any recorded
	// sequence number is <= the actual sequence number at this timestamp
	ep_buffer_manager_requires_lock_held (buffer_manager);
	ep_sequence_point_set_timestamp (sequence_point, ep_perf_timestamp_get ());
}

static
void
buffer_manager_dequeue_sequence_point (EventPipeBufferManager *buffer_manager)
{
	EP_ASSERT (buffer_manager != NULL);

	ep_buffer_manager_requires_lock_held (buffer_manager);

	ep_return_void_if_nok (!dn_list_empty (buffer_manager->sequence_points));

	EventPipeSequencePoint *value = *dn_list_front_t (buffer_manager->sequence_points, EventPipeSequencePoint *);
	dn_list_pop_front (buffer_manager->sequence_points);

	ep_sequence_point_free (value);
}

static
bool
buffer_manager_try_peek_sequence_point (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint **sequence_point)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (sequence_point != NULL);

	ep_buffer_manager_requires_lock_held (buffer_manager);

	ep_return_false_if_nok (!dn_list_empty (buffer_manager->sequence_points));

	*sequence_point = *dn_list_front_t (buffer_manager->sequence_points, EventPipeSequencePoint *);
	return *sequence_point != NULL;
}

static
EventPipeBuffer *
buffer_manager_allocate_buffer_for_thread (
	EventPipeBufferManager *buffer_manager,
	EventPipeThreadSessionState *thread_session_state,
	uint32_t request_size,
	bool *write_suspended)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (thread_session_state != NULL);
	EP_ASSERT (request_size > 0);

	EventPipeBuffer *new_buffer = NULL;
	EventPipeSequencePoint* sequence_point = NULL;
	uint32_t sequence_number = 0;

	// Pick a buffer size by multiplying the base buffer size by the number of buffers already allocated for this thread.
	uint32_t size_multiplier = ep_thread_session_state_get_buffer_count_estimate(thread_session_state) + 1;
	EP_ASSERT(size_multiplier > 0);

	// Pick the base buffer size.  Checked builds have a smaller size to stress the allocate path more.
#ifdef EP_CHECKED_BUILD
	uint32_t base_buffer_size = 30 * 1024; // 30K
#else
	uint32_t base_buffer_size = 100 * 1024; // 100K
#endif
	uint32_t buffer_size = base_buffer_size * size_multiplier;
	EP_ASSERT(buffer_size > 0);


	buffer_size = EP_MAX (request_size, buffer_size);

	// Don't allow the buffer size to exceed 1MB.
	const uint32_t max_buffer_size = 1024 * 1024;
	buffer_size = EP_MIN (buffer_size, max_buffer_size);


	// Make sure that buffer size >= request size so that the buffer size does not
	// determine the max event size.
	EP_ASSERT (request_size <= buffer_size);

	// Make the buffer size fit into with pagesize-aligned block, since ep_rt_valloc0 expects page-aligned sizes to be passed as arguments
	buffer_size = (buffer_size + ep_rt_system_get_alloc_granularity () - 1) & ~(uint32_t)(ep_rt_system_get_alloc_granularity () - 1);

	// Attempt to reserve the necessary buffer size
	EP_ASSERT(buffer_size > 0);
	ep_return_null_if_nok(buffer_manager_try_reserve_buffer (buffer_manager, buffer_size));

	// The sequence counter is exclusively mutated on this thread so this is a thread-local read.
	sequence_number = ep_thread_session_state_get_volatile_sequence_number (thread_session_state);
	new_buffer = ep_buffer_alloc (buffer_size, ep_thread_session_state_get_thread (thread_session_state), sequence_number);
	ep_raise_error_if_nok (new_buffer != NULL);

	// Allocating a new EventPipeSequencePoint and adding to the sequence_point list requires the buffer manager lock.
	// Adding a buffer to the buffer list requires us to take the lock.
	EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
		if (buffer_manager->sequence_point_alloc_budget != 0) {
			// sequence point bookkeeping
			if (buffer_size >= buffer_manager->remaining_sequence_point_alloc_budget) {
				sequence_point = ep_sequence_point_alloc ();
				if (sequence_point) {
					buffer_manager_init_sequence_point_thread_list (buffer_manager, sequence_point);
					ep_raise_error_if_nok_holding_spin_lock (buffer_manager_enqueue_sequence_point (buffer_manager, sequence_point), section1);
					sequence_point = NULL;
				}
				buffer_manager->remaining_sequence_point_alloc_budget = buffer_manager->sequence_point_alloc_budget;
			} else {
				buffer_manager->remaining_sequence_point_alloc_budget -= buffer_size;
			}
		}
#ifdef EP_CHECKED_BUILD
		buffer_manager->num_buffers_allocated++;
#endif // EP_CHECKED_BUILD

		// Set the buffer on the thread.
		if (new_buffer != NULL) {
			ep_buffer_list_insert_tail (ep_thread_session_state_get_buffer_list (thread_session_state), new_buffer);
			ep_thread_session_state_set_write_buffer (thread_session_state, new_buffer);
		}
	EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)

ep_on_exit:

	return new_buffer;

ep_on_error:
	ep_sequence_point_free (sequence_point);
	sequence_point = NULL;

	ep_buffer_free (new_buffer);
	new_buffer = NULL;

	buffer_manager_release_buffer (buffer_manager, buffer_size);

	ep_exit_error_handler ();
}

static
void
buffer_manager_deallocate_buffer (
	EventPipeBufferManager *buffer_manager,
	EventPipeBuffer *buffer)
{
	EP_ASSERT (buffer_manager != NULL);

	if (buffer) {
		buffer_manager_release_buffer (buffer_manager, ep_buffer_get_size (buffer));
		ep_buffer_free (buffer);
#ifdef EP_CHECKED_BUILD
		buffer_manager->num_buffers_allocated--;
#endif
	}
}

static
void
buffer_manager_snapshot_threads (
	EventPipeBufferManager *buffer_manager,
	ep_timestamp_t snapshot_timestamp)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (dn_list_empty (buffer_manager->thread_session_state_list_snapshot));
	EP_ASSERT (snapshot_timestamp != 0);

	ep_buffer_manager_requires_lock_held (buffer_manager);

	buffer_manager->snapshot_timestamp = snapshot_timestamp;

	DN_LIST_FOREACH_BEGIN (EventPipeThreadSessionState *, thread_session_state, buffer_manager->thread_session_state_list) {
		EventPipeBufferList *buffer_list = ep_thread_session_state_get_buffer_list (thread_session_state);
		EventPipeBuffer *buffer = buffer_list->head_buffer;
		if (buffer && ep_buffer_get_creation_timestamp (buffer) < snapshot_timestamp) {
			// Ownership is not transferred, the thread_session_state_list still owns the EventPipeThreadSessionState
			dn_list_push_back (buffer_manager->thread_session_state_list_snapshot, thread_session_state);
		}
	} DN_LIST_FOREACH_END;

	ep_buffer_manager_requires_lock_held (buffer_manager);
}

static
void
buffer_manager_move_next_event_any_snapshot_thread (EventPipeBufferManager *buffer_manager)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (buffer_manager->thread_session_state_list_snapshot != NULL);
	EP_ASSERT (buffer_manager->snapshot_timestamp != 0);

	ep_buffer_manager_requires_lock_not_held (buffer_manager);

	if (buffer_manager->current_event != NULL)
		ep_buffer_move_next_read_event (buffer_manager->current_buffer);

	buffer_manager->current_event = NULL;
	buffer_manager->current_buffer = NULL;
	buffer_manager->current_thread_session_state = NULL;

	// Iterate the snapshot list to find the one with the oldest event. This may require
	// converting some of the live buffers from writable to readable. We may need to wait outside the
	// buffer_manager lock for a writer thread to finish an in progress write to the buffer.
	ep_timestamp_t stop_timestamp = buffer_manager->snapshot_timestamp;
	ep_timestamp_t oldest_timestamp = stop_timestamp;

	EventPipeBuffer *buffer;
	for (dn_list_it_t it = dn_list_begin (buffer_manager->thread_session_state_list_snapshot); !dn_list_it_end (it); ) {
		EventPipeThreadSessionState *thread_session_state = *dn_list_it_data_t (it, EventPipeThreadSessionState *);
		EP_ASSERT (thread_session_state != NULL);

		dn_list_it_t next_it = dn_list_it_next (it);

		bool live_thread_session_state_exhausted = false;
		bool snapshot_thread_session_state_exhausted = true;
		buffer = buffer_manager_advance_to_non_empty_buffer (buffer_manager, thread_session_state, stop_timestamp, &live_thread_session_state_exhausted);
		if (buffer) {
			EventPipeEventInstance *next_event = ep_buffer_get_current_read_event (buffer);
			if (next_event) {
				ep_timestamp_t next_event_timestamp = ep_event_instance_get_timestamp (next_event);
				if (next_event_timestamp < stop_timestamp)
					snapshot_thread_session_state_exhausted = false;
				if (next_event_timestamp < oldest_timestamp) {
					buffer_manager->current_event = next_event;
					buffer_manager->current_buffer = buffer;
					buffer_manager->current_thread_session_state = thread_session_state;
					oldest_timestamp = next_event_timestamp;
				}
			}
		}

		if (snapshot_thread_session_state_exhausted)
			dn_list_erase (it);

		if (live_thread_session_state_exhausted) {
			EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1);
				buffer_manager_remove_and_delete_thread_session_state (buffer_manager, thread_session_state);
			EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1);
		}

		it = next_it;
	}

	// The current event will be read, so update the last read sequence number for that thread session state.
	if (buffer_manager->current_event != NULL)
	{
		uint32_t sequence_number = ep_buffer_get_current_sequence_number (buffer_manager->current_buffer);
		ep_thread_session_state_set_last_read_sequence_number (buffer_manager->current_thread_session_state, sequence_number);
	}

ep_on_exit:
	ep_buffer_manager_requires_lock_not_held (buffer_manager);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

static
void
buffer_manager_move_next_event_same_thread (EventPipeBufferManager *buffer_manager)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (buffer_manager->current_event != NULL);
	EP_ASSERT (buffer_manager->current_buffer != NULL);
	EP_ASSERT (buffer_manager->current_thread_session_state != NULL);

	ep_buffer_manager_requires_lock_not_held (buffer_manager);

	//advance past the current event
	buffer_manager->current_event = NULL;
	ep_buffer_move_next_read_event (buffer_manager->current_buffer);

	// Find the first buffer in the list, if any, which has an event in it
	EventPipeThreadSessionState *current_thread_session_state = buffer_manager->current_thread_session_state;
	ep_timestamp_t stop_timestamp = buffer_manager->snapshot_timestamp;
	bool live_thread_session_state_exhausted = false;
	bool snapshot_thread_session_state_exhausted = true;
	buffer_manager->current_buffer = buffer_manager_advance_to_non_empty_buffer (buffer_manager, current_thread_session_state, stop_timestamp, &live_thread_session_state_exhausted);

	if (buffer_manager->current_buffer) {
		// get the event from that buffer
		EventPipeEventInstance *next_event = ep_buffer_get_current_read_event (buffer_manager->current_buffer);
		ep_timestamp_t next_timestamp = ep_event_instance_get_timestamp (next_event);
		if (next_timestamp >= stop_timestamp) {
			// event exists, but isn't early enough
			buffer_manager->current_event = NULL;
			buffer_manager->current_buffer = NULL;
			buffer_manager->current_thread_session_state = NULL;
		} else {
			// event is early enough, set the new cursor
			buffer_manager->current_event = next_event;
			EP_ASSERT (buffer_manager->current_buffer != NULL);
			EP_ASSERT (buffer_manager->current_thread_session_state != NULL);
			snapshot_thread_session_state_exhausted = false;
			// The current event will be read, so update the last read sequence number for that thread session state.
			uint32_t sequence_number = ep_buffer_get_current_sequence_number (buffer_manager->current_buffer);
			ep_thread_session_state_set_last_read_sequence_number (current_thread_session_state, sequence_number);
		}
	} else {
		// no more buffers prior to before_timestamp
		EP_ASSERT (buffer_manager->current_event == NULL);
		EP_ASSERT (buffer_manager->current_buffer == NULL);
		buffer_manager->current_thread_session_state = NULL;
	}

	if (snapshot_thread_session_state_exhausted)
		dn_list_remove (buffer_manager->thread_session_state_list_snapshot, current_thread_session_state);

	if (live_thread_session_state_exhausted) {
		EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
			buffer_manager_remove_and_delete_thread_session_state (buffer_manager, current_thread_session_state);
		EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)
	}

ep_on_exit:
	return;

ep_on_error:
	ep_exit_error_handler ();
}

static
EventPipeBuffer *
buffer_manager_advance_to_non_empty_buffer (
	EventPipeBufferManager *buffer_manager,
	EventPipeThreadSessionState *thread_session_state,
	ep_timestamp_t before_timestamp,
	bool *thread_session_state_exhausted)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (thread_session_state != NULL);

	ep_buffer_manager_requires_lock_not_held (buffer_manager);

	EventPipeBufferList *buffer_list = ep_thread_session_state_get_buffer_list (thread_session_state);
	EP_ASSERT (buffer_list != NULL);
	EventPipeBuffer *current_buffer = buffer_list->head_buffer;
	EP_ASSERT (current_buffer != NULL);
	*thread_session_state_exhausted = false;
	bool done = false;
	while (!done) {
		buffer_manager_convert_buffer_to_read_only (buffer_manager, current_buffer);
		if (ep_buffer_get_current_read_event (current_buffer) != NULL) {
			// found a non-empty buffer
			done = true;
		} else {
			EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
				// delete the empty buffer
				EventPipeBuffer *removed_buffer = ep_buffer_list_get_and_remove_head (buffer_list);
				EP_ASSERT (current_buffer == removed_buffer);
				buffer_manager_deallocate_buffer (buffer_manager, removed_buffer);

				// get the next buffer
				current_buffer = buffer_list->head_buffer;

				if (!current_buffer && (ep_rt_volatile_load_uint32_t_without_barrier (ep_thread_get_unregistered_ref (ep_thread_session_state_get_thread (thread_session_state))) > 0))
					*thread_session_state_exhausted = true;

				if (!current_buffer || ep_buffer_get_creation_timestamp (current_buffer) >= before_timestamp) {
					// no more buffers in the list before this timestamp, we're done
					current_buffer = NULL;
					done = true;
				}
			EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)
		}
	}

ep_on_exit:
	ep_buffer_manager_requires_lock_not_held (buffer_manager);
	return current_buffer;

ep_on_error:
	current_buffer = NULL;
	ep_exit_error_handler ();
}

static
void
buffer_manager_convert_buffer_to_read_only (
	EventPipeBufferManager *buffer_manager,
	EventPipeBuffer *new_read_buffer)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (new_read_buffer != NULL);

	ep_buffer_manager_requires_lock_not_held (buffer_manager);

	// if already readable, nothing to do
	if (ep_buffer_get_volatile_state (new_read_buffer) == EP_BUFFER_STATE_READ_ONLY)
		return;

	bool wait_for_writer_thread = false;
	EventPipeThreadSessionState *thread_session_state = NULL;

	// if not yet readable, unpublish this buffer from the writer thread
	EventPipeThread *thread = ep_buffer_get_writer_thread (new_read_buffer);
	EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
		thread_session_state = ep_thread_get_session_state (thread, buffer_manager->session);
		EP_ASSERT(thread_session_state != NULL);
		if (ep_thread_session_state_get_write_buffer (thread_session_state) == new_read_buffer) {
			ep_thread_session_state_set_write_buffer (thread_session_state, NULL);
			wait_for_writer_thread = true;
		}
	EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)

	if (wait_for_writer_thread) {
		uint32_t index = ep_session_get_index (buffer_manager->session);
		// Wait for the writer thread to finish any pending writes to this buffer and release any
		// cached pointers it may have. The writer thread might need the buffer manager lock to make progress so we can't hold it while waiting.
		// Both session_use_in_progress and write_buffer are accessed via atomic operations. By setting the write_buffer to NULL above while holding
		// the buffer manager lock, the wait is correct only because the writer thread sets session_use_in_progress before caching the write_buffer
		// and resets it after it's done using the cached write_buffer.
		EP_YIELD_WHILE (ep_thread_get_session_use_in_progress (thread) == index &&
						ep_thread_session_state_get_volatile_write_buffer (thread_session_state) == NULL);
	}

	// Now that the writer thread has released its cached pointer to this buffer, we can safely
	// mark it as read-only.
	// For buffer_manager's coordination of event writing and reading operations, defensively transition the buffer state under the buffer manager lock.
	EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section2)
		ep_buffer_convert_to_read_only (new_read_buffer);
	EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section2)

ep_on_exit:
	ep_buffer_manager_requires_lock_not_held (buffer_manager);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

EventPipeBufferManager *
ep_buffer_manager_alloc (
	EventPipeSession *session,
	size_t max_size_of_all_buffers,
	size_t sequence_point_allocation_budget)
{
	EventPipeBufferManager *instance = ep_rt_object_alloc (EventPipeBufferManager);
	ep_raise_error_if_nok (instance != NULL);

	instance->thread_session_state_list = dn_list_alloc ();
	ep_raise_error_if_nok (instance->thread_session_state_list != NULL);

	instance->sequence_points = dn_list_alloc ();
	ep_raise_error_if_nok (instance->sequence_points != NULL);

	ep_rt_spin_lock_alloc (&instance->rt_lock);
	ep_raise_error_if_nok (ep_rt_spin_lock_is_valid (&instance->rt_lock));

	ep_rt_wait_event_alloc (&instance->rt_wait_event, false, true);
	ep_raise_error_if_nok (ep_rt_wait_event_is_valid (&instance->rt_wait_event));

	instance->thread_session_state_list_snapshot = dn_list_alloc ();
	ep_raise_error_if_nok (instance->thread_session_state_list_snapshot != NULL);
	instance->snapshot_timestamp = 0;

	instance->session = session;
	instance->size_of_all_buffers = 0;
	instance->num_oversized_events_dropped = 0;

#ifdef EP_CHECKED_BUILD
	instance->num_buffers_allocated = 0;
	instance->num_buffers_stolen = 0;
	instance->num_buffers_leaked = 0;
	instance->num_events_stored = 0;
	ep_rt_volatile_store_int64_t (&instance->num_events_dropped, 0);
	ep_rt_volatile_store_int64_t (&instance->num_events_written, 0);
#endif

	instance->current_event = NULL;
	instance->current_buffer = NULL;
	instance->current_thread_session_state = NULL;

	instance->max_size_of_all_buffers = EP_CLAMP ((size_t)100 * 1024, max_size_of_all_buffers, (size_t)UINT32_MAX);

	if (sequence_point_allocation_budget == 0) {
		// sequence points disabled
		instance->sequence_point_alloc_budget = 0;
		instance->remaining_sequence_point_alloc_budget = 0;
	} else {
		instance->sequence_point_alloc_budget = EP_CLAMP ((size_t)1024 * 1024, sequence_point_allocation_budget, (size_t)1024 * 1024 * 1024);
		instance->remaining_sequence_point_alloc_budget = sequence_point_allocation_budget;
	}

ep_on_exit:
	return instance;

ep_on_error:
	ep_buffer_manager_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ep_buffer_manager_free (EventPipeBufferManager * buffer_manager)
{
	ep_return_void_if_nok (buffer_manager != NULL);

	dn_list_free (buffer_manager->sequence_points);

	EP_ASSERT (dn_list_empty (buffer_manager->thread_session_state_list_snapshot));

	dn_list_free (buffer_manager->thread_session_state_list_snapshot);

	EP_ASSERT (dn_list_empty (buffer_manager->thread_session_state_list));

	dn_list_free (buffer_manager->thread_session_state_list);

	ep_rt_wait_event_free (&buffer_manager->rt_wait_event);

	ep_rt_spin_lock_free (&buffer_manager->rt_lock);

	ep_rt_object_free (buffer_manager);
}

#ifdef EP_CHECKED_BUILD
void
ep_buffer_manager_requires_lock_held (const EventPipeBufferManager *buffer_manager)
{
	ep_rt_spin_lock_requires_lock_held (&buffer_manager->rt_lock);
}

void
ep_buffer_manager_requires_lock_not_held (const EventPipeBufferManager *buffer_manager)
{
	ep_rt_spin_lock_requires_lock_not_held (&buffer_manager->rt_lock);
}
#endif

void
ep_buffer_manager_init_sequence_point_thread_list (
	EventPipeBufferManager *buffer_manager,
	EventPipeSequencePoint *sequence_point)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (sequence_point != NULL);

	ep_buffer_manager_requires_lock_not_held (buffer_manager);

	EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
		buffer_manager_init_sequence_point_thread_list (buffer_manager, sequence_point);
	EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)

ep_on_exit:
	ep_buffer_manager_requires_lock_not_held (buffer_manager);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

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
	EventPipeStackContents *stack)
{
	bool result = false;
	bool alloc_new_buffer = false;
	EventPipeBuffer *buffer = NULL;
	EventPipeThreadSessionState *session_state = NULL;
	EventPipeStackContents stack_contents;
	EventPipeStackContents *current_stack_contents = NULL;

	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (ep_event != NULL);

	// The input thread must match the current thread because no lock is taken on the buffer.
	EP_ASSERT (thread == ep_rt_thread_get_handle ());

	// Before we pick a buffer, make sure the event is enabled.
	ep_return_false_if_nok (ep_event_is_enabled (ep_event));

	// Check to see if an event thread was specified. If not, then use the current thread.
	if (event_thread == NULL)
		event_thread = thread;

	EventPipeThread *current_thread;
	current_thread = ep_thread_get ();
	ep_raise_error_if_nok (current_thread != NULL);

	// session_state won't be freed if use_in_progress is set.
	EP_ASSERT (ep_thread_get_session_use_in_progress (current_thread) == ep_session_get_index (session));
	session_state = ep_thread_get_volatile_session_state (current_thread, session);
	if (session_state == NULL) {
		// slow path should only happen once per thread per session
		EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
			// No need to re-check session_state inside the lock, this thread is the only one allowed to create it.
			session_state = ep_thread_session_state_alloc (current_thread, session, buffer_manager);
			ep_raise_error_if_nok_holding_spin_lock (session_state != NULL, section1);
			ep_raise_error_if_nok_holding_spin_lock (dn_list_push_back (buffer_manager->thread_session_state_list, session_state), section1);
			ep_thread_set_session_state (current_thread, session, session_state);
		EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)
	}

	// Check that the payload size is less than 64 KB (max size for ETW events)
	if (ep_event_payload_get_size (payload) > 64 * 1024)
	{
		ep_rt_atomic_inc_int64_t (&buffer_manager->num_oversized_events_dropped);
		ep_thread_session_state_increment_sequence_number (session_state);
		return false;
	}

	current_stack_contents = ep_stack_contents_init (&stack_contents);
	if (stack == NULL && ep_session_get_enable_stackwalk (session) && ep_event_get_need_stack (ep_event) && !ep_session_get_rundown_enabled (session)) {
		ep_walk_managed_stack_for_current_thread (current_stack_contents);
		stack = current_stack_contents;
	}

	// buffer won't be converted to read-only if use_in_progress is set
	EP_ASSERT (ep_thread_get_session_use_in_progress (current_thread) == ep_session_get_index(session));
	buffer = ep_thread_session_state_get_volatile_write_buffer (session_state);
	if (!buffer) {
		alloc_new_buffer = true;
	} else {
		// Attempt to write the event to the buffer. If this fails, we should allocate a new buffer.
		if (ep_buffer_write_event (buffer, event_thread, session, ep_event, payload, activity_id, related_activity_id, stack))
			ep_thread_session_state_increment_sequence_number (session_state);
		else
			alloc_new_buffer = true;
	}

	// alloc_new_buffer is reused below to detect if overflow happened, so cache it here to see if we should
	// signal the reader thread
	bool should_signal_reader_thread;
	should_signal_reader_thread = alloc_new_buffer;

	// Check to see if we need to allocate a new buffer, and if so, do it here.
	if (alloc_new_buffer) {
		uint32_t request_size = sizeof (EventPipeEventInstance) + ep_event_payload_get_size (payload);
		bool write_suspended = false;
		buffer = buffer_manager_allocate_buffer_for_thread (buffer_manager, session_state, request_size, &write_suspended);
		if (!buffer) {
			// We treat this as the write_event call occurring after this session stopped listening for events, effectively the
			// same as if ep_event_is_enabled test above returned false.
			ep_raise_error_if_nok (!write_suspended);
			ep_thread_session_state_increment_sequence_number (session_state);
		} else {
			current_thread = ep_thread_get ();
			EP_ASSERT (current_thread != NULL);

			// Try to write the event after we allocated a buffer.
			// This is the first time if the thread had no buffers before the call to this function.
			// This is the second time if this thread did have one or more buffers, but they were full.
			alloc_new_buffer = !ep_buffer_write_event (buffer, event_thread, session, ep_event, payload, activity_id, related_activity_id, stack);
			EP_ASSERT(!alloc_new_buffer);
			ep_thread_session_state_increment_sequence_number (session_state);
		}
	}

	if (should_signal_reader_thread)
		// Indicate that there is new data to be read
		ep_rt_wait_event_set (&buffer_manager->rt_wait_event);

#ifdef EP_CHECKED_BUILD
	if (!alloc_new_buffer)
		ep_rt_atomic_inc_int64_t (&buffer_manager->num_events_stored);
	else
		ep_rt_atomic_inc_int64_t (&buffer_manager->num_events_dropped);
#endif

	result = !alloc_new_buffer;

ep_on_exit:
	ep_stack_contents_fini (current_stack_contents);
	return result;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_buffer_manager_write_all_buffers_to_file (
	EventPipeBufferManager *buffer_manager,
	EventPipeFile *file,
	ep_timestamp_t stop_timestamp,
	bool *events_written)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (file != NULL);
	EP_ASSERT (buffer_manager->current_event == NULL);

	// The V4 format doesn't require full event sorting as V3 did
	// See the comments in WriteAllBufferToFileV4 for more details
	if (ep_file_get_format (file) >= EP_SERIALIZATION_FORMAT_NETTRACE_V4)
		ep_buffer_manager_write_all_buffers_to_file_v4 (buffer_manager, file, stop_timestamp, events_written);
	else
		ep_buffer_manager_write_all_buffers_to_file_v3 (buffer_manager, file, stop_timestamp, events_written);
}

void
ep_buffer_manager_write_all_buffers_to_file_v3 (
	EventPipeBufferManager *buffer_manager,
	EventPipeFile *file,
	ep_timestamp_t stop_timestamp,
	bool *events_written)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (file != NULL);
	EP_ASSERT (buffer_manager->thread_session_state_list_snapshot != NULL);
	EP_ASSERT (buffer_manager->current_event == NULL);
	EP_ASSERT (events_written != NULL);

	*events_written = false;

	EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
		buffer_manager_snapshot_threads (buffer_manager, stop_timestamp);
	EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1);

	// Naively walk the circular buffer, writing the event stream in timestamp order.
	buffer_manager_move_next_event_any_snapshot_thread (buffer_manager);
	while (buffer_manager->current_event != NULL) {
		*events_written = true;
		ep_file_write_event (file, buffer_manager->current_event, /*CaptureThreadId=*/0, /*sequenceNumber=*/0, /*IsSorted=*/true);
		buffer_manager_move_next_event_any_snapshot_thread (buffer_manager);
	}
	ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);

	EP_ASSERT (dn_list_empty (buffer_manager->thread_session_state_list_snapshot));

ep_on_exit:
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_buffer_manager_write_all_buffers_to_file_v4 (
	EventPipeBufferManager *buffer_manager,
	EventPipeFile *file,
	ep_timestamp_t stop_timestamp,
	bool *events_written)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (file != NULL);
	EP_ASSERT (buffer_manager->current_event == NULL);
	EP_ASSERT (events_written != NULL);

	//
	// In V3 of the format this code does a full timestamp order sort on the events which made the file easier to consume,
	// but the perf implications for emitting the file are less desirable. Imagine an application with 500 threads emitting
	// 10 events per sec per thread (granted this is a questionable number of threads to use in an app, but that isn't
	// under our control). A naive sort of 500 ordered lists is going to pull the oldest event from each of 500 lists,
	// compare all the timestamps, then emit the oldest one. This could easily add a thousand CPU cycles per-event. A
	// better implementation could maintain a min-heap so that we scale O(log(N)) instead of O(N)but fundamentally sorting
	// has a cost and we didn't want a file format that forces the runtime to pay it on every event.
	//
	// We minimize sorting using two mechanisms:
	// 1) Explicit sequence points - Every X MB of buffer space that is distributed to threads we record the current
	// timestamp. We ensure when writing events in the file that all events before the sequence point time are written
	// prior to the sequence point and all events with later timestamps are written afterwards. For example assume
	// two threads emitted events like this(B_14 = event on thread B with timestamp 14):
	//
	//                    Time --->
	//   Thread A events: A_1     A_4     A_9 A_10 A_11 A_12 A_13      A_15
	//   Thread B events:     B_2     B_6                         B_14      B_20
	//                                             /|\.
	//                                              |
	//                                            Assume sequence point was triggered here
	// Then we promise that events A_1, A_4, A_9, A_10, B_2_ and B_6 will be written in one or more event blocks,
	// (not necessarily in sorted order) then a sequence point block is written, then events A_11, A_12, A_13, B_14,
	// A_15, and B_20 will be written. The reader can cache all the events between sequence points, sort them, and
	// then emit them in a total order. Triggering sequence points based on buffer allocation ensures that we won't
	// need an arbitrarily large cache in the reader to store all the events, however there is a fair amount of slop
	// in the current scheme. In the worst case you could imagine N threads, each of which was already allocated a
	// max size buffer (currently 1MB) but only an insignificant portion has been used. Even if the trigger
	// threshold is a modest amount such as 10MB, the threads could first write 1MB * N bytes to the stream
	// beforehand. I'm betting on these extreme cases being very rare and even something like 1GB isn't an unreasonable
	// amount of virtual memory to use on to parse an extreme trace. However if I am wrong we can control
	// both the allocation policy and the triggering instrumentation. Nothing requires us to give out 1MB buffers to
	// 1000 threads simultaneously, nor are we prevented from observing buffer usage at finer granularity than we
	// allocated.
	//
	// 2) We mark which events are the oldest ones in the stream at the time we emit them and we do this at regular
	// intervals of time. When we emit all the events every X ms, there will be at least one event in there with
	// a marker showing that all events older than that one have already been emitted. As soon as the reader sees
	// this it can sort the events which have older timestamps and emit them.
	//
	// Why have both mechanisms? The sequence points in #1 worked fine to guarantee that given the whole trace you
	// could  sort it with a bounded cache, but it doesn't help much for real-time usage. Imagine that we have two
	// threads emitting 1KB/sec of events and sequence points occur every 10MB. The reader would need to wait for
	// 10,000 seconds to accumulate all the events before it could sort and process them. On the other hand if we
	// only had mechanism #2 the reader can generate the sort quickly in real-time, but it is messy to do the buffer
	// management. The reader reads in a bunch of event block buffers and starts emitting events from sub-sections
	// of each of them and needs to know when each buffer can be released. The explicit sequence point makes that
	// very easy - every sequence point all buffers can be released and no further bookkeeping is required.

	*events_written = false;

	EventPipeSequencePoint *sequence_point = NULL;

	DN_DEFAULT_LOCAL_ALLOCATOR (allocator, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_init_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t session_states_to_delete;
	ep_raise_error_if_nok (dn_vector_ptr_custom_init (&session_states_to_delete, &params));

	// loop across sequence points
	while (true) {
		ep_timestamp_t current_timestamp_boundary = stop_timestamp;
		EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
			if (buffer_manager_try_peek_sequence_point (buffer_manager, &sequence_point))
				current_timestamp_boundary = EP_MIN (current_timestamp_boundary, ep_sequence_point_get_timestamp (sequence_point));

			buffer_manager_snapshot_threads (buffer_manager, current_timestamp_boundary);
		EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1);

		// loop across events within a sequence point boundary
		while (true) {
			// pick the thread that has the oldest event
			buffer_manager_move_next_event_any_snapshot_thread (buffer_manager);
			if (buffer_manager->current_event == NULL)
				break;

			uint64_t capture_thread_id = ep_thread_get_os_thread_id (ep_buffer_get_writer_thread (buffer_manager->current_buffer));

			// loop across events on this thread
			bool events_written_for_thread = false;

			uint32_t sequence_number = 0;

			while (buffer_manager->current_event != NULL) {
				// The first event emitted on each thread (detected by !events_written_for_thread) is guaranteed to
				// be the oldest event cached in our buffers so we mark it. This implements mechanism #2
				// in the big comment above.
				sequence_number = ep_buffer_get_current_sequence_number (buffer_manager->current_buffer);
				ep_file_write_event (file, buffer_manager->current_event, capture_thread_id, sequence_number, !events_written_for_thread);
				events_written_for_thread = true;
				buffer_manager_move_next_event_same_thread (buffer_manager);
			}

			// Have we written events in any sequence point?
			*events_written = events_written_for_thread || *events_written;
		}
		EP_ASSERT (dn_list_empty (buffer_manager->thread_session_state_list_snapshot));

		// This finishes any current partially filled EventPipeBlock, and flushes it to the stream
		ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);

		// there are no more events prior to snapshot_timestamp
		if (buffer_manager->snapshot_timestamp == stop_timestamp)
			break;

		// stopped at sequence point case

		// the sequence point captured a lower bound for sequence number on each thread, but iterating
		// through the events we may have observed that a higher numbered event was recorded. If so we
		// should adjust the sequence numbers upwards to ensure the data in the stream is consistent.
		// EventPipeThreadSessionStates removed during move_next_event_any|same_thread, will already
		// have had their sequence numbers updated.
		EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section2)
			DN_LIST_FOREACH_BEGIN (EventPipeThreadSessionState *, session_state, buffer_manager->thread_session_state_list) {
				EventPipeThread *thread = ep_thread_session_state_get_thread (session_state);
				dn_umap_it_t found = dn_umap_ptr_uint32_find (ep_sequence_point_get_thread_sequence_numbers (sequence_point), thread);
				uint32_t thread_sequence_number = !dn_umap_it_end (found) ? dn_umap_it_value_uint32_t (found) : 0;
				uint32_t last_read_sequence_number = ep_thread_session_state_get_last_read_sequence_number (session_state);
				// Sequence numbers can overflow so we can't use a direct last_read > sequence_number comparison
				// If a thread is able to drop more than 0x80000000 events in between sequence points then we will
				// miscategorize it, but that seems unlikely.
				uint32_t last_read_delta = last_read_sequence_number - thread_sequence_number;
				if (0 < last_read_delta && last_read_delta < 0x80000000) {
					dn_umap_ptr_uint32_insert_or_assign (ep_sequence_point_get_thread_sequence_numbers (sequence_point), thread, last_read_sequence_number);
					if (dn_umap_it_end (found))
						ep_thread_addref (thread);
				}
			} DN_LIST_FOREACH_END;
		EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section2)

		// emit the sequence point into the file
		ep_file_write_sequence_point (file, sequence_point);

		// we are done with the sequence point
		EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section3)
			buffer_manager_dequeue_sequence_point (buffer_manager);
		EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section3)
	}

ep_on_exit:
	dn_vector_ptr_dispose (&session_states_to_delete);
	return;
ep_on_error:
	ep_exit_error_handler ();
}

// Former `buffer_manager_move_next_event_any_thread (buffer_manager, ep_perf_timestamp_get ())` perf comment
// PERF: This may be too aggressive? If this method is being called frequently enough to keep pace with the
// writing threads we could be in a state of high lock contention and lots of churning buffers. Each writer
// would take several locks, allocate a new buffer, write one event into it, then the reader would take the
// lock, convert the buffer to read-only and read the single event out of it. Allowing more events to accumulate
// in the buffers before converting between writable and read-only amortizes a lot of the overhead. One way
// to achieve that would be picking a stop_timestamp that was Xms in the past. This would let Xms of events
// to accumulate in the write buffer before we converted it and forced the writer to allocate another. Other more
// sophisticated approaches would probably build a low overhead synchronization mechanism to read and write the
// buffer at the same time.
//
// As a slight optimization for many writer threads concurrently writing events while the reader thread attempts
// to convert all head buffers to read-only, we take a snapshot of known thread session states and iterate
// until all events before the snapshot timestamp have been read before taking a new snapshot.
EventPipeEventInstance *
ep_buffer_manager_get_next_event (EventPipeBufferManager *buffer_manager)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (buffer_manager->thread_session_state_list_snapshot != NULL);

	ep_requires_lock_not_held ();

	if (dn_list_empty (buffer_manager->thread_session_state_list_snapshot)) {
		ep_timestamp_t snapshot_timestamp = ep_perf_timestamp_get ();

		EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
			buffer_manager_snapshot_threads (buffer_manager, snapshot_timestamp);
		EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)
	}

	buffer_manager_move_next_event_any_snapshot_thread (buffer_manager);

ep_on_exit:
	return buffer_manager->current_event;

ep_on_error:
	buffer_manager->current_event = NULL;
	ep_exit_error_handler ();
}

void
ep_buffer_manager_close (EventPipeBufferManager *buffer_manager)
{
	EP_ASSERT (buffer_manager != NULL);

	dn_list_clear (buffer_manager->thread_session_state_list_snapshot);

	// Take the buffer manager manipulation lock
	EP_SPIN_LOCK_ENTER (&buffer_manager->rt_lock, section1)
		EP_ASSERT (ep_buffer_manager_ensure_consistency (buffer_manager));

		DN_LIST_FOREACH_BEGIN (EventPipeThreadSessionState *, thread_session_state, buffer_manager->thread_session_state_list) {
			// Get the list and determine if we can free it.
			EventPipeBufferList *buffer_list = ep_thread_session_state_get_buffer_list (thread_session_state);

			// Iterate over all nodes in the buffer list and deallocate them.
			EventPipeBuffer *buffer = ep_buffer_list_get_and_remove_head (buffer_list);
			while (buffer) {
				buffer_manager_deallocate_buffer(buffer_manager, buffer);
				buffer = ep_buffer_list_get_and_remove_head (buffer_list);
			}

			// clear the thread session state slot
			ep_thread_set_session_state (ep_thread_session_state_get_thread (thread_session_state), ep_thread_session_state_get_session (thread_session_state), NULL);
			// delete the thread session state and transitively the buffer list
			ep_thread_session_state_free (thread_session_state);

		} DN_LIST_FOREACH_END;

		// Clear thread session state list.
		dn_list_clear (buffer_manager->thread_session_state_list);

	EP_SPIN_LOCK_EXIT (&buffer_manager->rt_lock, section1)

ep_on_exit:
	return;

ep_on_error:
	ep_exit_error_handler ();
}

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_manager_ensure_consistency (EventPipeBufferManager *buffer_manager)
{
	EP_ASSERT (buffer_manager != NULL);

	DN_LIST_FOREACH_BEGIN (EventPipeThreadSessionState *, thread_session_state, buffer_manager->thread_session_state_list) {
		EP_ASSERT (ep_buffer_list_ensure_consistency (ep_thread_session_state_get_buffer_list (thread_session_state)));
	} DN_LIST_FOREACH_END;

	return true;
}
#endif

// While deleting a thread session state, this is the last chance to update sequence points
// mapped thread sequence numbers. The current sequence point needs to have the latest read
// sequence number, where as future sequence points can drop the thread entry.
static
void
buffer_manager_remove_and_delete_thread_session_state (
	EventPipeBufferManager *buffer_manager,
	EventPipeThreadSessionState *thread_session_state)
{
	EP_ASSERT (buffer_manager != NULL);
	EP_ASSERT (thread_session_state != NULL);

	ep_buffer_manager_requires_lock_held (buffer_manager);

	EventPipeSequencePoint *current_sequence_point = NULL;
	if (buffer_manager_try_peek_sequence_point (buffer_manager, &current_sequence_point)) {
		DN_LIST_FOREACH_BEGIN (EventPipeSequencePoint *, sequence_point, buffer_manager->sequence_points) {
			EventPipeThread *thread = ep_thread_session_state_get_thread (thread_session_state);
			dn_umap_t *thread_sequence_numbers = ep_sequence_point_get_thread_sequence_numbers (sequence_point);
			dn_umap_it_t found = dn_umap_ptr_uint32_find (thread_sequence_numbers, thread);
			if (current_sequence_point != sequence_point) {
				if (!dn_umap_it_end (found)) {
					ep_thread_release (thread);
					dn_umap_erase_key (thread_sequence_numbers, thread);
				}
				continue;
			}

			uint32_t thread_sequence_number = !dn_umap_it_end (found) ? dn_umap_it_value_uint32_t (found) : 0;
			// Sequence numbers can overflow so we can't use a direct last_read > sequence_number comparison
			// If a thread is able to drop more than 0x80000000 events in between sequence points then we will
			// miscategorize it, but that seems unlikely.
			uint32_t sequence_number = ep_thread_session_state_get_last_read_sequence_number (thread_session_state);
			uint32_t last_read_delta = sequence_number - thread_sequence_number;
			if (0 < last_read_delta && last_read_delta < 0x80000000) {
				dn_umap_ptr_uint32_insert_or_assign (thread_sequence_numbers, thread, sequence_number);
				if (dn_umap_it_end (found))
					ep_thread_addref (thread);
			}
		} DN_LIST_FOREACH_END;
	}

	dn_list_remove (buffer_manager->thread_session_state_list, thread_session_state);
	ep_thread_set_session_state (ep_thread_session_state_get_thread (thread_session_state), ep_thread_session_state_get_session (thread_session_state), NULL);
	ep_thread_session_state_free (thread_session_state);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_buffer_manager;
const char quiet_linker_empty_file_warning_eventpipe_buffer_manager = 0;
#endif
