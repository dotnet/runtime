#ifndef __EVENTPIPE_BUFFER_H__
#define __EVENTPIPE_BUFFER_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-rt.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_BUFFER_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeBuffer.
 */

// Synchronization
//
// EventPipeBuffer starts off writable and accumulates events in a buffer, then at some point converts to be readable and a second thread can
// read back the events which have accumulated. The transition occurs when calling convert_to_read_only (). Write methods will assert if the buffer
// isn't writable and read-related methods will assert if it isn't readable. Methods that have no asserts should have immutable results that
// can be used at any point during the buffer's lifetime. The buffer has no internal locks so it is the caller's responsibility to synchronize
// their usage.
// Writing into the buffer and calling convert_to_read_only() is always done with EventPipeThread rt_lock held. The eventual reader thread can do
// a few different things to ensure it sees a consistent state:
// 1) Take the writer's EventPipeThread rt_lock at least once after the last time the writer writes events
// 2) Use a memory barrier that prevents reader loads from being re-ordered earlier, such as the one that will occur implicitly by evaluating
//    ep_buffer_get_volatile_state ()

// Instances of EventPipeEventInstance in the buffer must be 8-byte aligned.
// It is OK for the data payloads to be unaligned because they are opaque blobs that are copied via memcpy.
#define EP_BUFFER_ALIGNMENT_SIZE 8

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BUFFER_GETTER_SETTER)
struct _EventPipeBuffer {
#else
struct _EventPipeBuffer_Internal {
#endif
	// The timestamp the buffer was created. If our clock source
	// is monotonic then all events in the buffer should have
	// timestamp >= this one. If not then all bets are off.
	ep_timestamp_t creation_timestamp;
	// Thread that is/was allowed to write into this buffer when state == WRITABLE.
	EventPipeThread *writer_thread;
	// A pointer to the actual buffer.
	uint8_t *buffer;
	// The current write pointer.
	uint8_t *current;
	// The max write pointer (end of the buffer).
	uint8_t *limit;
	// Pointer to the current event being read.
	EventPipeEventInstance *current_read_event;
	// Each buffer will become part of a per-thread linked list of buffers.
	// The linked list is invasive, thus we declare the pointers here.
	EventPipeBuffer *prev_buffer;
	EventPipeBuffer *next_buffer;
	// State transition WRITABLE -> READ_ONLY only occurs while holding the writer_thread->rt_lock;
	// It can be read at any time
	volatile uint32_t state;
	// The sequence number corresponding to current_read_event
	// Prior to read iteration it is the sequence number of the first event in the buffer
	uint32_t event_sequence_number;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BUFFER_GETTER_SETTER)
struct _EventPipeBuffer {
	uint8_t _internal [sizeof (struct _EventPipeBuffer_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeBuffer *, buffer, ep_timestamp_t, creation_timestamp)
EP_DEFINE_GETTER(EventPipeBuffer *, buffer, uint8_t *, buffer)
EP_DEFINE_GETTER(EventPipeBuffer *, buffer, uint8_t *, limit)
EP_DEFINE_GETTER_REF(EventPipeBuffer *, buffer, volatile uint32_t *, state)
EP_DEFINE_GETTER(EventPipeBuffer *, buffer, EventPipeBuffer *, prev_buffer)
EP_DEFINE_SETTER(EventPipeBuffer *, buffer, EventPipeBuffer *, prev_buffer)
EP_DEFINE_GETTER(EventPipeBuffer *, buffer, EventPipeBuffer *, next_buffer)
EP_DEFINE_SETTER(EventPipeBuffer *, buffer, EventPipeBuffer *, next_buffer)
EP_DEFINE_GETTER(EventPipeBuffer *, buffer, EventPipeThread *, writer_thread)

EventPipeBuffer *
ep_buffer_alloc (
	uint32_t buffer_size,
	EventPipeThread *writer_thread,
	uint32_t event_sequence_number);

void
ep_buffer_free (EventPipeBuffer *buffer);

static
inline
uint32_t
ep_buffer_get_size (const EventPipeBuffer *buffer)
{
	return (uint32_t)(ep_buffer_get_limit (buffer) - ep_buffer_get_buffer (buffer));
}

static
EP_ALWAYS_INLINE
uint8_t *
ep_buffer_get_next_aligned_address (const EventPipeBuffer *buffer, uint8_t *address)
{
	EP_ASSERT (ep_buffer_get_buffer (buffer) <= address && address <= ep_buffer_get_limit (buffer));
	address = (uint8_t *)EP_ALIGN_UP (address, EP_BUFFER_ALIGNMENT_SIZE);
	EP_ASSERT ((size_t)address % EP_BUFFER_ALIGNMENT_SIZE == 0);
	return address;
}

// Write an event to the buffer.
// An optional stack trace can be provided for sample profiler events.
// Otherwise, if a stack trace is needed, one will be automatically collected.
// Returns:
//  - true: The write succeeded.
//  - false: The write failed.  In this case, the buffer should be considered full.
bool
ep_buffer_write_event (
	EventPipeBuffer *buffer,
	ep_rt_thread_handle_t thread,
	EventPipeSession *session,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	EventPipeStackContents *stack);

// Advances read cursor to the next event or NULL if there aren't any more. When the
// buffer is first made readable the cursor is automatically positioned on the first
// event or NULL if there are no events in the buffer.
void
ep_buffer_move_next_read_event (EventPipeBuffer *buffer);

// Returns the event at the current read cursor. The returned event pointer is valid
// until the buffer is deleted.
EventPipeEventInstance *
ep_buffer_get_current_read_event (const EventPipeBuffer *buffer);

// Gets the sequence number of the event corresponding to get_current_read_event ().
uint32_t
ep_buffer_get_current_sequence_number (const EventPipeBuffer *buffer);

// Check the state of the buffer.
EventPipeBufferState
ep_buffer_get_volatile_state (const EventPipeBuffer *buffer);

// Convert the buffer writable to readable.
// _Requires_lock_held (thread)
void
ep_buffer_convert_to_read_only (EventPipeBuffer *buffer);

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_ensure_consistency (const EventPipeBuffer *buffer);
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_BUFFER_H__ */
