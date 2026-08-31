#ifndef __EVENTPIPE_BLOCK_H__
#define __EVENTPIPE_BLOCK_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-stream.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_BLOCK_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeBlock
 */

typedef void (*EventPipeBlockClearFunc)(void *object);
typedef uint32_t (*EventPipeBlockGetHeaderSizeFunc)(void *object);
typedef void (*EventPipeBlockSerializeHeaderFunc)(void *object, FastSerializer *fast_serializer);

struct _EventPipeBlockVtable {
	FastSerializableObjectVtable fast_serializable_object_vtable;
	EventPipeBlockClearFunc clear_func;
	EventPipeBlockGetHeaderSizeFunc get_header_size_func;
	EventPipeBlockSerializeHeaderFunc serialize_header_func;
};

// The base type for all file blocks in the Nettrace file format
// This class handles memory management to buffer the block data,
// bookkeeping, block version numbers, and serializing the data
// to the file with correct alignment.
// Sub-types decide the format of the block contents and how
// the blocks are named.
#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeBlock {
#else
struct _EventPipeBlock_Internal {
#endif
	FastSerializableObject fast_serializer_object;
	uint8_t *block;
	uint8_t *write_pointer;
	uint8_t *end_of_the_buffer;
	EventPipeSerializationFormat format;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeBlock {
	uint8_t _internal [sizeof (struct _EventPipeBlock_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeBlock *, block, uint8_t*, block)
EP_DEFINE_GETTER(EventPipeBlock *, block, uint8_t*, write_pointer)
EP_DEFINE_SETTER(EventPipeBlock *, block, uint8_t*, write_pointer)
EP_DEFINE_GETTER(EventPipeBlock *, block, uint8_t*, end_of_the_buffer)
EP_DEFINE_GETTER(EventPipeBlock *, block, EventPipeSerializationFormat, format)

static
inline
uint32_t
ep_block_get_bytes_written (const EventPipeBlock *block)
{
	return block == NULL ? 0 : (uint32_t)(ep_block_get_write_pointer (block) - ep_block_get_block (block));
}

EventPipeBlock *
ep_block_init (
	EventPipeBlock *block,
	EventPipeBlockVtable *vtable,
	uint32_t max_block_size,
	EventPipeSerializationFormat format);

void
ep_block_fini (EventPipeBlock *block);

void
ep_block_clear (EventPipeBlock *block);

uint32_t
ep_block_get_header_size (EventPipeBlock *block);

void
ep_block_serialize_header (
	EventPipeBlock *block,
	FastSerializer *fast_serializer);

void
ep_block_fast_serialize (
	EventPipeBlock *block,
	FastSerializer *fast_serializer);

void
ep_block_clear_vcall (EventPipeBlock *block);

uint32_t
ep_block_get_header_size_vcall (EventPipeBlock *block);

void
ep_block_serialize_header_vcall (
	EventPipeBlock *block,
	FastSerializer *fast_serializer);

void
ep_block_fast_serialize_vcall (
	EventPipeBlock *block,
	FastSerializer *fast_serializer);

/*
 * EventPipeEventHeader.
 */

struct _EventPipeEventHeader {
	uint8_t activity_id [EP_ACTIVITY_ID_SIZE];
	uint8_t related_activity_id [EP_ACTIVITY_ID_SIZE];
	ep_timestamp_t timestamp;
	uint64_t thread_id;
	uint64_t capture_thread_id;
	uint32_t metadata_id;
	uint32_t sequence_number;
	uint32_t capture_proc_number;
	uint32_t stack_id;
	uint32_t data_len;
};

/*
 * EventPipeEventBlockBase
 */

// The base type for blocks that contain events (EventBlock and EventMetadataBlock).
#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeEventBlockBase {
#else
struct _EventPipeEventBlockBase_Internal {
#endif
	EventPipeBlock block;
	EventPipeEventHeader last_header;
	uint8_t compressed_header [100];
	ep_timestamp_t min_timestamp;
	ep_timestamp_t max_timestamp;
	bool use_header_compression;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeEventBlockBase {
	uint8_t _internal [sizeof (struct _EventPipeEventBlockBase_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(EventPipeEventBlockBase *, event_block_base, EventPipeBlock *, block)
EP_DEFINE_GETTER_REF(EventPipeEventBlockBase *, event_block_base, EventPipeEventHeader *, last_header)
EP_DEFINE_GETTER(EventPipeEventBlockBase *, event_block_base, ep_timestamp_t, min_timestamp)
EP_DEFINE_SETTER(EventPipeEventBlockBase *, event_block_base, ep_timestamp_t, min_timestamp)
EP_DEFINE_GETTER(EventPipeEventBlockBase *, event_block_base, ep_timestamp_t, max_timestamp)
EP_DEFINE_SETTER(EventPipeEventBlockBase *, event_block_base, ep_timestamp_t, max_timestamp)
EP_DEFINE_GETTER(EventPipeEventBlockBase *, event_block_base, bool, use_header_compression)
EP_DEFINE_GETTER_ARRAY_REF(EventPipeEventBlockBase *, event_block_base, uint8_t *, const uint8_t *, compressed_header, compressed_header[0])

EventPipeEventBlockBase *
ep_event_block_base_init (
	EventPipeEventBlockBase *event_block_base,
	EventPipeBlockVtable *vtable,
	uint32_t max_block_size,
	EventPipeSerializationFormat format,
	bool use_header_compression);

void
ep_event_block_base_fini (EventPipeEventBlockBase *event_block_base);

void
ep_event_block_base_clear (EventPipeEventBlockBase *event_block_base);

uint32_t
ep_event_block_base_get_header_size (const EventPipeEventBlockBase *event_block_base);

void
ep_event_block_base_serialize_header (
	EventPipeEventBlockBase *event_block_base,
	FastSerializer *fast_serializer);

bool
ep_event_block_base_write_event (
	EventPipeEventBlockBase *event_block_base,
	EventPipeEventInstance *event_instance,
	uint64_t capture_thread_id,
	uint32_t sequence_number,
	uint32_t stack_id,
	bool is_sorted_event);

/*
 * EventPipeEventBlock.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeEventBlock {
#else
struct _EventPipeEventBlock_Internal {
#endif
	EventPipeEventBlockBase event_block_base;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeEventBlock {
	uint8_t _internal [sizeof (struct _EventPipeEventBlock_Internal)];
};
#endif

EventPipeEventBlock *
ep_event_block_alloc (
	uint32_t max_block_size,
	EventPipeSerializationFormat format);

void
ep_event_block_free (EventPipeEventBlock *event_block);

static
inline
uint32_t
ep_event_block_get_bytes_written (EventPipeEventBlock *event_block)
{
	return ep_block_get_bytes_written ((const EventPipeBlock *)event_block);
}

static
inline
void
ep_event_block_serialize (EventPipeEventBlock *event_block, FastSerializer *fast_serializer)
{
	ep_fast_serializer_write_object (fast_serializer, (FastSerializableObject*)event_block);
}

static
inline
void
ep_event_block_clear (EventPipeEventBlock *event_block)
{
	ep_block_clear_vcall ((EventPipeBlock *)event_block);
}

/*
 * EventPipeMetadataBlock.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeMetadataBlock {
#else
struct _EventPipeMetadataBlock_Internal {
#endif
	EventPipeEventBlockBase event_block_base;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeMetadataBlock {
	uint8_t _internal [sizeof (struct _EventPipeMetadataBlock_Internal)];
};
#endif

EventPipeMetadataBlock *
ep_metadata_block_alloc (uint32_t max_block_size);

void
ep_metadata_block_free (EventPipeMetadataBlock *metadata_block);

static
inline
uint32_t
ep_metadata_block_get_bytes_written (EventPipeMetadataBlock *metadata_block)
{
	return ep_block_get_bytes_written ((const EventPipeBlock *)metadata_block);
}

static
inline
void
ep_metadata_block_serialize (EventPipeMetadataBlock *metadata_block, FastSerializer *fast_serializer)
{
	ep_fast_serializer_write_object (fast_serializer, (FastSerializableObject *)metadata_block);
}

static
inline
void
ep_metadata_block_clear (EventPipeMetadataBlock *metadata_block)
{
	ep_block_clear_vcall ((EventPipeBlock *)metadata_block);
}

/*
 * EventPipeSequencePointBlock.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeSequencePointBlock {
#else
struct _EventPipeSequencePointBlock_Internal {
#endif
	EventPipeBlock block;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeSequencePointBlock {
	uint8_t _internal [sizeof (struct _EventPipeSequencePointBlock_Internal)];
};
#endif

EventPipeSequencePointBlock *
ep_sequence_point_block_alloc (EventPipeSequencePoint *sequence_point);

EventPipeSequencePointBlock *
ep_sequence_point_block_init (
	EventPipeSequencePointBlock *sequence_point_block,
	EventPipeSequencePoint *sequence_point);

void
ep_sequence_point_block_fini (EventPipeSequencePointBlock *sequence_point_block);

void
ep_sequence_point_block_free (EventPipeSequencePointBlock *sequence_point_block);

/*
 * EventPipeStackBlock.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeStackBlock {
#else
struct _EventPipeStackBlock_Internal {
#endif
	EventPipeBlock block;
	uint32_t initial_index;
	uint32_t count;
	bool has_initial_index;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_BLOCK_GETTER_SETTER)
struct _EventPipeStackBlock {
	uint8_t _internal [sizeof (struct _EventPipeStackBlock_Internal)];
};
#endif

EventPipeStackBlock *
ep_stack_block_alloc (uint32_t max_block_size);

void
ep_stack_block_free (EventPipeStackBlock *stack_block);

bool
ep_stack_block_write_stack (
	EventPipeStackBlock *stack_block,
	uint32_t stack_id,
	EventPipeStackContentsInstance *stack);

static
inline
uint32_t
ep_stack_block_get_bytes_written (EventPipeStackBlock *stack_block)
{
	return ep_block_get_bytes_written ((const EventPipeBlock *)stack_block);
}

static
inline
void
ep_stack_block_serialize (EventPipeStackBlock *stack_block, FastSerializer *fast_serializer)
{
	ep_fast_serializer_write_object (fast_serializer, (FastSerializableObject *)stack_block);
}

static
inline
void
ep_stack_block_clear (EventPipeStackBlock *stack_block)
{
	ep_block_clear_vcall ((EventPipeBlock *)stack_block);
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_BLOCK_H__ */
