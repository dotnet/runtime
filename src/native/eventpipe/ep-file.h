#ifndef __EVENTPIPE_FILE_H__
#define __EVENTPIPE_FILE_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-stream.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_FILE_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeFile.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_FILE_GETTER_SETTER)
struct _EventPipeFile {
#else
struct _EventPipeFile_Internal {
#endif
	FastSerializableObject fast_serializable_object;
	// The system time when the file was opened.
	EventPipeSystemTime file_open_system_time;
	// The frequency of the timestamps used for this file.
	int64_t timestamp_frequency;
	StreamWriter *stream_writer;
	// The object responsible for serialization.
	FastSerializer *fast_serializer;
	EventPipeEventBlock *event_block;
	EventPipeMetadataBlock *metadata_block;
	EventPipeStackBlock *stack_block;
	// Hashtable of metadata labels.
	ep_rt_metadata_labels_hash_map_t metadata_ids;
	ep_rt_stack_hash_map_t stack_hash;
	// The timestamp when the file was opened.  Used for calculating file-relative timestamps.
	ep_timestamp_t file_open_timestamp;
#ifdef EP_CHECKED_BUILD
	ep_timestamp_t last_sorted_timestamp;
#endif
	uint32_t pointer_size;
	uint32_t current_process_id;
	uint32_t number_of_processors;
	uint32_t sampling_rate_in_ns;
	uint32_t stack_id_counter;
	volatile uint32_t metadata_id_counter;
	volatile uint32_t initialized;
	// The format to serialize.
	EventPipeSerializationFormat format;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_FILE_GETTER_SETTER)
struct _EventPipeFile {
	uint8_t _internal [sizeof (struct _EventPipeFile_Internal)];
};
#endif

EP_DEFINE_GETTER(EventPipeFile *, file, FastSerializer *, fast_serializer)
EP_DEFINE_GETTER(EventPipeFile *, file, EventPipeSerializationFormat, format)

static
inline
bool
ep_file_has_errors (EventPipeFile *file)
{
	return (ep_file_get_fast_serializer (file) == NULL) || ep_fast_serializer_get_write_error_encountered (ep_file_get_fast_serializer (file));
}

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

/*
 * StackHashKey.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_FILE_GETTER_SETTER)
struct _StackHashKey {
#else
struct _StackHashKey_Internal {
#endif
	uint8_t *stack_bytes;
	uint32_t hash;
	uint32_t stack_size_in_bytes;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_FILE_GETTER_SETTER)
struct _StackHashKey {
	uint8_t _internal [sizeof (struct _StackHashKey_Internal)];
};
#endif

EP_DEFINE_GETTER(StackHashKey *, stack_hash_key, uint32_t, hash)

StackHashKey *
ep_stack_hash_key_init (
	StackHashKey *key,
	const EventPipeStackContentsInstance *stack_contents);

void
ep_stack_hash_key_fini (StackHashKey *key);

uint32_t
ep_stack_hash_key_hash (const void *key);

bool
ep_stack_hash_key_equal (const void *key1, const void *key2);

/*
 * StackHashEntry.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_FILE_GETTER_SETTER)
struct _StackHashEntry {
#else
struct _StackHashEntry_Internal {
#endif
	StackHashKey key;
	uint32_t id;
	// This is the first byte of StackSizeInBytes bytes of stack data
	uint8_t stack_bytes[1];
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_FILE_GETTER_SETTER)
struct _StackHashEntry {
	uint8_t _internal [sizeof (struct _StackHashEntry_Internal)];
};
#endif

EP_DEFINE_GETTER_REF(StackHashEntry *, stack_hash_entry, StackHashKey *, key)
EP_DEFINE_GETTER(StackHashEntry *, stack_hash_entry, uint32_t, id)

StackHashKey *
ep_stack_hash_entry_get_key (StackHashEntry *stack_hash_entry);

StackHashEntry *
ep_stack_hash_entry_alloc (
	const EventPipeStackContentsInstance *stack_contents,
	uint32_t id,
	uint32_t hash);

void
ep_stack_hash_entry_free (StackHashEntry *stack_hash_entry);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_FILE_H__ */
