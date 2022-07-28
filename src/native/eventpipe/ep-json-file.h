#ifndef __EVENTPIPE_JSON_FILE_H__
#define __EVENTPIPE_JSON_FILE_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ep-rt.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_JSON_FILE_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

#ifdef EP_CHECKED_BUILD

/*
 * EventPipeJsonFile.
 */

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_JSON_FILE_GETTER_SETTER)
struct _EventPipeJsonFile {
#else
struct _EventPipeJsonFile_Internal {
#endif
	ep_rt_file_handle_t file_stream;
	ep_timestamp_t file_open_timestamp;
	bool write_error_encountered;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_JSON_FILE_GETTER_SETTER)
struct _EventPipeJsonFile {
	uint8_t _internal [sizeof (struct _EventPipeJsonFile_Internal)];
};
#endif

EventPipeJsonFile *
ep_json_file_alloc (const ep_char8_t *out_file_path);

void
ep_json_file_free (EventPipeJsonFile *json_file);

void
ep_json_file_write_event (
	EventPipeJsonFile *json_file,
	EventPipeEventInstance *instance);

void
ep_json_file_write_event_data (
	EventPipeJsonFile *json_file,
	ep_timestamp_t timestamp,
	ep_rt_thread_id_t thread_id,
	const ep_char8_t *message,
	EventPipeStackContentsInstance *stack_contents);

#endif /* EP_CHECKED_BUILD */
#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_JSON_FILE_H__ */
