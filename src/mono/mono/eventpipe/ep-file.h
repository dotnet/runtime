#ifndef __EVENTPIPE_FILE_H__
#define __EVENTPIPE_FILE_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"

/*
 * EventPipeFile.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeFile {
#else
struct _EventPipeFile_Internal {
#endif
	FastSerializableObject fast_serializable_object;
	StreamWriter *stream_writer;
	FastSerializer *fast_serializer;
	EventPipeEventBlock *event_block;
	EventPipeMetadataBlock *metadata_block;
	EventPipeStackBlock *stack_block;
	ep_rt_metadata_labels_hash_map_t metadata_ids;
	ep_rt_stack_hash_map_t stack_hash;
	uint64_t file_open_system_time;
	uint64_t file_open_timestamp;
	uint64_t timestamp_frequency;
#ifdef EP_CHECKED_BUILD
	uint64_t last_sorted_timestamp;
#endif
	uint32_t pointer_size;
	uint32_t current_process_id;
	uint32_t number_of_processors;
	uint32_t sampling_rate_in_ns;
	uint32_t stack_id_counter;
	volatile uint32_t metadata_id_counter;
	EventPipeSerializationFormat format;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _EventPipeFile {
	uint8_t _internal [sizeof (struct _EventPipeFile_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeFile *, file, StreamWriter *, stream_writer)
EP_DEFINE_GETTER(EventPipeFile *, file, FastSerializer *, fast_serializer)
EP_DEFINE_SETTER(EventPipeFile *, file, FastSerializer *, fast_serializer)
EP_DEFINE_GETTER(EventPipeFile *, file, EventPipeEventBlock *, event_block)
EP_DEFINE_GETTER(EventPipeFile *, file, EventPipeMetadataBlock *, metadata_block)
EP_DEFINE_GETTER_REF(EventPipeFile *, file, ep_rt_metadata_labels_hash_map_t *, metadata_ids)
EP_DEFINE_GETTER_REF(EventPipeFile *, file, ep_rt_stack_hash_map_t *, stack_hash)
EP_DEFINE_GETTER(EventPipeFile *, file, EventPipeStackBlock *, stack_block)
EP_DEFINE_GETTER(EventPipeFile *, file, EventPipeSerializationFormat, format)
EP_DEFINE_GETTER(EventPipeFile *, file, uint32_t, stack_id_counter);
EP_DEFINE_SETTER(EventPipeFile *, file, uint32_t, stack_id_counter);
EP_DEFINE_GETTER_REF(EventPipeFile *, file, volatile uint32_t *, metadata_id_counter)
#ifdef EP_CHECKED_BUILD
EP_DEFINE_GETTER(EventPipeFile *, file, uint64_t, last_sorted_timestamp)
EP_DEFINE_SETTER(EventPipeFile *, file, uint64_t, last_sorted_timestamp)
#endif

EventPipeFile *
ep_file_alloc (
	StreamWriter *stream_writer,
	EventPipeSerializationFormat format);

void
ep_file_free (EventPipeFile *file);

bool
ep_file_initialize_file (EventPipeFile *file);

void
ep_file_write_event (
	EventPipeFile *file,
	EventPipeEventInstance * event_instance,
	uint64_t capture_thread_id,
	uint32_t sequence_number,
	bool is_sorted_event);

void
ep_file_write_sequence_point (
	EventPipeFile *file,
	EventPipeSequencePoint *sequence_point);

void
ep_file_flush (
	EventPipeFile *file,
	EventPipeFileFlushFlags flags);

int32_t
ep_file_get_file_version (EventPipeSerializationFormat format);

int32_t
ep_file_get_file_minimum_version (EventPipeSerializationFormat format);

/*
 * StackHashKey.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _StackHashKey {
#else
struct _StackHashKey_Internal {
#endif
	uint8_t *stack_bytes;
	uint32_t hash;
	uint32_t stack_size_in_bytes;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _StackHashKey {
	uint8_t _internal [sizeof (struct _StackHashKey_Internal)];
};
#endif

EP_DEFINE_GETTER(StackHashKey *, stack_hash_key, uint32_t, hash)

StackHashKey *
ep_stack_hash_key_init (
	StackHashKey *key,
	const EventPipeStackContents *stack_contents);

void
ep_stack_hash_key_fini (StackHashKey *key);

/*
 * StackHashEntry.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_GETTER_SETTER)
struct _StackHashEntry {
#else
struct _StackHashEntry_Internal {
#endif
	StackHashKey key;
	uint32_t id;
	// This is the first byte of StackSizeInBytes bytes of stack data
	uint8_t stack_bytes[1];
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_GETTER_SETTER)
struct _StackHashEntry {
	uint8_t _internal [sizeof (struct _StackHashEntry_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(StackHashEntry *, stack_hash_entry, StackHashKey *, key)
EP_DEFINE_GETTER(StackHashEntry *, stack_hash_entry, uint32_t, id)

StackHashEntry *
ep_stack_hash_entry_alloc (
	const EventPipeStackContents *stack_contents,
	uint32_t id,
	uint32_t hash);

void
ep_stack_hash_entry_free (StackHashEntry *stack_hash_entry);

#endif /* ENABLE_PERFTRACING */
#endif /** __EVENTPIPE_FILE_H__ **/
