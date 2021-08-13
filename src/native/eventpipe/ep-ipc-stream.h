#ifndef __EVENTPIPE_IPC_STREAM_H__
#define __EVENTPIPE_IPC_STREAM_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-ipc-pal-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_IPC_STREAM_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * IpcStream.
 */

typedef void (*IpcStreamFreeFunc)(void *object);
typedef bool (*IpcStreamReadFunc)(void *object, uint8_t *buffer, uint32_t bytes_to_read, uint32_t *bytes_read, uint32_t timeout_ms);
typedef bool (*IpcStreamWriteFunc)(void *object, const uint8_t *buffer, uint32_t bytes_to_write, uint32_t *bytes_written, uint32_t timeout_ms);
typedef bool (*IpcStreamFlushFunc)(void *object);
typedef bool (*IpcStreamCloseFunc)(void *object);

struct _IpcStreamVtable {
	IpcStreamFreeFunc free_func;
	IpcStreamReadFunc read_func;
	IpcStreamWriteFunc write_func;
	IpcStreamFlushFunc flush_func;
	IpcStreamCloseFunc close_func;
};

#if defined(EP_INLINE_GETTER_SETTER) || defined(EP_IMPL_IPC_STREAM_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_NAMEDPIPE_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _IpcStream {
#else
struct _IpcStream_Internal {
#endif
	IpcStreamVtable *vtable;
};

#if !defined(EP_INLINE_GETTER_SETTER) && !defined(EP_IMPL_IPC_STREAM_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_NAMEDPIPE_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _IpcStream {
	uint8_t _internal [sizeof (struct _IpcStream_Internal)];
};
#endif

IpcStream *
ep_ipc_stream_init (
	IpcStream *ipc_stream,
	IpcStreamVtable *vtable);

void
ep_ipc_stream_fini (IpcStream *ipc_stream);

void
ep_ipc_stream_free_vcall (IpcStream *ipc_stream);

bool
ep_ipc_stream_read_vcall (
	IpcStream *ipc_stream,
	uint8_t *buffer,
	uint32_t bytes_to_read,
	uint32_t *bytes_read,
	uint32_t timeout_ms);

bool
ep_ipc_stream_write_vcall (
	IpcStream *ipc_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written,
	uint32_t timeout_ms);

bool
ep_ipc_stream_flush_vcall (IpcStream *ipc_stream);

bool
ep_ipc_stream_close_vcall (IpcStream *ipc_stream);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_IPC_STREAM_H__ */
