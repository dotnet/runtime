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

// Diagnostics: reserve space for a per-buffer header guard and a footer region.
// These regions allow post-mortem detection of memory corruption and simple use-after-free.
// Header: first 32 bytes (EP_BUFFER_HEADER_GUARD_SIZE) at buffer start.
// Footer: last 32 bytes (EP_BUFFER_FOOTER_GUARD_SIZE) at buffer end; first 24 bytes structured, remainder padding.
#define EP_BUFFER_HEADER_GUARD_SIZE 32   /* Bytes reserved at buffer start for guard/metadata */
#define EP_BUFFER_FOOTER_GUARD_SIZE 32   /* Bytes reserved at buffer end for guard + padding */

// Header (when guards enabled) uses 32 bytes.
// Layout (little-endian offsets):
//  0x00: uint32 HeaderMagic (ASCII "EPBFSTRT")
//  0x08: uint64 creation_timestamp
//  0x10: void*  writer_thread
//  0x18: uint32 first_event_sequence_number
//  0x1C-0x1F: 0x00 padding before first aligned EventPipeEventInstance
#define EP_BUFFER_HDR_OFFSET_MAGIC     0x00
#define EP_BUFFER_HDR_OFFSET_TIMESTAMP 0x08
#define EP_BUFFER_HDR_OFFSET_THREADPTR 0x10
#define EP_BUFFER_HDR_OFFSET_SEQNO     0x18
#define EP_BUFFER_HDR_MAGIC            0x5452545346425045ULL /* "EPBFSTRT" little-endian */

// Footer (EP_BUFFER_FOOTER_GUARD_SIZE bytes) layout when EventPipeBufferGuardLevel is enabled:
//   0x00: uint64 FooterMagic (ASCII "EPBFEND!")
//   0x08: uint64 ~FooterMagic (bitwise inverse for quick integrity check)
//   0x10: uint64 Checksum (creation_timestamp ^ writer_thread_pointer ^ first_event_sequence_number ^ EP_BUFFER_CHECKSUM_SALT)
//   0x18..0x1F: Padding bytes (0xEF) retained as small overrun guard / visual marker
// The checksum excludes the magic values (they are validated independently) and uses a fixed salt.
#define EP_BUFFER_FOOTER_OFFSET_MAGIC     0x00
#define EP_BUFFER_FOOTER_OFFSET_MAGIC_INV 0x08
#define EP_BUFFER_FOOTER_OFFSET_CHECKSUM  0x10
#define EP_BUFFER_FOOTER_MAGIC            0x21444E4546425045ULL  /* "EPBFEND!" little-endian */
#define EP_BUFFER_CHECKSUM_SALT           0x544C415346425045ULL  /* "EPBFSALT" little-endian salt */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BUFFER_GETTER_SETTER)
struct _EventPipeBufferHeaderGuard {
#else
struct _EventPipeBufferHeaderGuard_Internal {
#endif
	uint64_t magic;
	uint64_t creation_timestamp;
	uint64_t writer_thread;
	uint32_t first_event_sequence_number;
	uint8_t padding[4];
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BUFFER_GETTER_SETTER)
struct _EventPipeBufferHeaderGuard {
	uint8_t _internal [sizeof (struct _EventPipeBufferHeaderGuard_Internal)];
};
#endif

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BUFFER_GETTER_SETTER)
struct _EventPipeBufferFooterGuard {
#else
struct _EventPipeBufferFooterGuard_Internal {
#endif
	uint64_t magic;
	uint64_t magic_inv;
	uint64_t checksum;
	uint8_t padding[8];
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BUFFER_GETTER_SETTER)
struct _EventPipeBufferFooterGuard {
	uint8_t _internal [sizeof (struct _EventPipeBufferFooterGuard_Internal)];
};
#endif

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
	// The end of the buffer.
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
	// Represents the effective start of the writable region, aligned to the next 8-byte boundary.
	// This is where the first event would be written.
	uint8_t *first_event_address;
	// Represents the effective end of the writable region.
	uint8_t *write_limit;
	// Level of guard protection applied to the buffer.
	EventPipeBufferGuardLevel buffer_guard_level;
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
	uint32_t event_sequence_number,
	EventPipeBufferGuardLevel buffer_guard_level);

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

void
ep_buffer_ensure_guard_consistency (const EventPipeBuffer *buffer);

#ifdef EP_CHECKED_BUILD
bool
ep_buffer_ensure_consistency (const EventPipeBuffer *buffer);
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_BUFFER_H__ */
