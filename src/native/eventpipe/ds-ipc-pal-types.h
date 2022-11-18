#ifndef __DIAGNOSTICS_IPC_PAL_TYPES_H__
#define __DIAGNOSTICS_IPC_PAL_TYPES_H__

#ifdef ENABLE_PERFTRACING
#include "ep-ipc-pal-types.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_PAL_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
 * Diagnostics IPC PAL Structs.
 */

typedef struct _DiagnosticsIpc DiagnosticsIpc;
typedef struct _DiagnosticsIpcPollHandle DiagnosticsIpcPollHandle;
typedef struct _DiagnosticsIpcStream DiagnosticsIpcStream;

/*
 * Diagnostics IPC PAL Enums.
 */

typedef enum {
	DS_IPC_POLL_EVENTS_NONE = 0x00, // no events
	DS_IPC_POLL_EVENTS_SIGNALED = 0x01, // ready for use
	DS_IPC_POLL_EVENTS_HANGUP = 0x02, // connection remotely closed
	DS_IPC_POLL_EVENTS_ERR = 0x04, // error
	DS_IPC_POLL_EVENTS_UNKNOWN = 0x80 // unknown state
} DiagnosticsIpcPollEvents;

typedef enum {
	DS_IPC_CONNECTION_MODE_CONNECT,
	DS_IPC_CONNECTION_MODE_LISTEN
} DiagnosticsIpcConnectionMode;

#define DS_IPC_MAX_TO_STRING_LEN 128
#define DS_IPC_TIMEOUT_INFINITE (uint32_t)-1

#define DS_IPC_POLL_TIMEOUT_FALLOFF_FACTOR (float)1.25
#define DS_IPC_POLL_TIMEOUT_MIN_MS (uint32_t)10
#define DS_IPC_POLL_TIMEOUT_MAX_MS (uint32_t)500

/*
 * DiagnosticsIpcPollHandle.
 */

// The bookkeeping struct used for polling on server and client structs
#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_NAMEDPIPE_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpcPollHandle {
#else
struct _DiagnosticsIpcPollHandle_Internal {
#endif
	// Only one of these will be non-null, treat as a union
	DiagnosticsIpc *ipc;
	DiagnosticsIpcStream *stream;

	// contains some set of PollEvents
	// will be set by Poll
	// Any values here are ignored by Poll
	uint8_t events;

	// a cookie assignable by upstream users for additional bookkeeping
	void *user_data;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_NAMEDPIPE_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpcPollHandle {
	uint8_t _internal [sizeof (struct _DiagnosticsIpcPollHandle_Internal)];
};
#endif

DS_DEFINE_GETTER(DiagnosticsIpcPollHandle *, ipc_poll_handle, DiagnosticsIpc *, ipc)
DS_DEFINE_GETTER(DiagnosticsIpcPollHandle *, ipc_poll_handle, DiagnosticsIpcStream *, stream)
DS_DEFINE_GETTER(DiagnosticsIpcPollHandle *, ipc_poll_handle, uint8_t, events)
DS_DEFINE_SETTER(DiagnosticsIpcPollHandle *, ipc_poll_handle, uint8_t, events)
DS_DEFINE_GETTER(DiagnosticsIpcPollHandle *, ipc_poll_handle, void *, user_data)
DS_DEFINE_SETTER(DiagnosticsIpcPollHandle *, ipc_poll_handle, void *, user_data)

typedef void (*ds_ipc_error_callback_func)(
	const ep_char8_t *message,
	uint32_t code);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_IPC_PAL_TYPES_H__ */
