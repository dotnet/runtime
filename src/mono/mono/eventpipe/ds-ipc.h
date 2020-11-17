#ifndef __DIAGNOSTICS_IPC_H__
#define __DIAGNOSTICS_IPC_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ds-rt-config.h"
#include "ds-types.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

#define DS_IPC_MAX_TO_STRING_LEN 128

/*
 * IpcStreamFactory.
 */

bool
ds_ipc_stream_factory_init (void);

void
ds_ipc_stream_factory_fini (void);

bool
ds_ipc_stream_factory_configure (ds_ipc_error_callback_func callback);

DiagnosticsIpcStream *
ds_ipc_stream_factory_get_next_available_stream (ds_ipc_error_callback_func callback);

void
ds_ipc_stream_factory_resume_current_port (void);

bool
ds_ipc_stream_factory_any_suspended_ports (void);

bool
ds_ipc_stream_factory_has_active_ports (void);

void
ds_ipc_stream_factory_close_ports (ds_ipc_error_callback_func callback);

bool
ds_ipc_stream_factory_shutdown (ds_ipc_error_callback_func callback);

/*
 * DiagnosticsPort.
 */

typedef void (*DiagnosticsPortFreeFunc)(void *object);
typedef bool (*DiagnosticsPortGetIPCPollHandleFunc)(void *object, DiagnosticsIpcPollHandle *handle, ds_ipc_error_callback_func callback);
typedef DiagnosticsIpcStream *(*DiagnosticsPortGetConnectedStreamFunc)(void *object, ds_ipc_error_callback_func callback);
typedef void (*DiagnosticsPortResetFunc)(void *object, ds_ipc_error_callback_func callback);

struct _DiagnosticsPortVtable {
	DiagnosticsPortFreeFunc free_func;
	DiagnosticsPortGetIPCPollHandleFunc get_ipc_poll_handle_func;
	DiagnosticsPortGetConnectedStreamFunc get_connected_stream_func;
	DiagnosticsPortResetFunc reset_func;
};

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsPort {
#else
struct _DiagnosticsPort_Internal {
#endif
	DiagnosticsPortVtable *vtable;
	DiagnosticsIpc *ipc;
	DiagnosticsIpcStream *stream;
	bool has_resumed_runtime;
	DiagnosticsPortSuspendMode suspend_mode;
	DiagnosticsPortType type;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsPort {
	uint8_t _internal [sizeof (struct _DiagnosticsPort_Internal)];
};
#endif

DiagnosticsPort *
ds_port_init (
	DiagnosticsPort *port,
	DiagnosticsPortVtable *vtable,
	DiagnosticsIpc *ipc,
	DiagnosticsPortBuilder *builder);

void
ds_port_fini (DiagnosticsPort *port);

void
ds_port_free_vcall (DiagnosticsPort *port);

// returns a pollable handle and performs any preparation required
// e.g., as a side-effect, will connect and advertise on reverse connections
bool
ds_port_get_ipc_poll_handle_vcall (
	DiagnosticsPort *port,
	DiagnosticsIpcPollHandle *handle,
	ds_ipc_error_callback_func callback);

// Returns the signaled stream in a usable state
DiagnosticsIpcStream *
ds_port_get_connected_stream_vcall (
	DiagnosticsPort * port,
	ds_ipc_error_callback_func callback);

// Resets the connection in the event of a hangup
void
ds_port_reset_vcall (
	DiagnosticsPort * port,
	ds_ipc_error_callback_func callback);

// closes the underlying connections
// only performs minimal cleanup if isShutdown==true
void
ds_port_close (
	DiagnosticsPort * port,
	bool is_shutdown,
	ds_ipc_error_callback_func callback);

/*
 * DiagnosticsPortBuilder.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsPortBuilder {
#else
struct _DiagnosticsPortBuilder_Internal {
#endif
	ep_char8_t *path;
	DiagnosticsPortSuspendMode suspend_mode;
	DiagnosticsPortType type;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsPortBuilder {
	uint8_t _internal [sizeof (struct _DiagnosticsPortBuilder_Internal)];
};
#endif

DiagnosticsPortBuilder *
ds_port_builder_init (DiagnosticsPortBuilder *builder);

void
ds_port_builder_fini (DiagnosticsPortBuilder *builder);

void
ds_port_builder_set_tag (
	DiagnosticsPortBuilder *builder,
	ep_char8_t *tag);

/*
 * DiagnosticsConnectPort.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsConnectPort {
#else
struct _DiagnosticsConnectPort_Internal {
#endif
	DiagnosticsPort port;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsConnectPort {
	uint8_t _internal [sizeof (struct _DiagnosticsConnectPort_Internal)];
};
#endif

DiagnosticsConnectPort *
ds_connect_port_alloc (
	DiagnosticsIpc *ipc,
	DiagnosticsPortBuilder *builder);

void
ds_connect_port_free (DiagnosticsConnectPort *listen_port);

/*
 * DiagnosticsListenPort.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsListenPort {
#else
struct _DiagnosticsListenPort_Internal {
#endif
	DiagnosticsPort port;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_GETTER_SETTER)
struct _DiagnosticsListenPort {
	uint8_t _internal [sizeof (struct _DiagnosticsListenPort_Internal)];
};
#endif

DiagnosticsListenPort *
ds_listen_port_alloc (
	DiagnosticsIpc *ipc,
	DiagnosticsPortBuilder *builder);

void
ds_listen_port_free (DiagnosticsListenPort *listen_port);

// PAL.

/*
 * DiagnosticsIpc.
 */

int32_t
ds_ipc_get_handle_int32_t (DiagnosticsIpc *ipc);

DiagnosticsIpc *
ds_ipc_alloc (
	const ep_char8_t *ipc_name,
	DiagnosticsIpcConnectionMode mode,
	ds_ipc_error_callback_func callback);

void
ds_ipc_free (DiagnosticsIpc *ipc);

// Poll
// Parameters:
// - IpcPollHandle * poll_handles: Array of IpcPollHandles to poll
// - uint32_t timeout_ms: The timeout in milliseconds for the poll (-1 == infinite)
// Returns:
// int32_t: -1 on error, 0 on timeout, >0 on successful poll
// Remarks:
// Check the events returned in revents for each IpcPollHandle to find the signaled handle.
// Signaled DiagnosticsIpcs can call accept() without blocking.
// Signaled IpcStreams can call read(...) without blocking.
// The caller is responsible for cleaning up "hung up" connections.
int32_t
ds_ipc_poll (
	ds_rt_ipc_poll_handle_array_t *poll_handles,
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback);

// puts the DiagnosticsIpc into Listening Mode
// Re-entrant safe
bool
ds_ipc_listen (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback);

// produces a connected stream from a server-mode DiagnosticsIpc.
// Blocks until a connection is available.
DiagnosticsIpcStream *
ds_ipc_accept (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback);

// Connect to a server and returns a connected stream
DiagnosticsIpcStream *
ds_ipc_connect (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback);

// Closes an open IPC.
// Only attempts minimal cleanup if is_shutdown==true, i.e.,
// unlinks Unix Domain Socket on Linux, no-op on Windows
void
ds_ipc_close (
	DiagnosticsIpc *ipc,
	bool is_shutdown,
	ds_ipc_error_callback_func callback);

int32_t
ds_ipc_to_string (
	DiagnosticsIpc *ipc,
	ep_char8_t *buffer,
	uint32_t buffer_len);
/*
 * DiagnosticsIpcStream.
 */

int32_t
ds_ipc_stream_get_handle_int32_t (DiagnosticsIpcStream *ipc_stream);

IpcStream *
ds_ipc_stream_get_stream_ref (DiagnosticsIpcStream *ipc_stream);

void
ds_ipc_stream_free (DiagnosticsIpcStream *ipc_stream);

bool
ds_ipc_stream_read (
	DiagnosticsIpcStream *ipc_stream,
	uint8_t *buffer,
	uint32_t bytes_to_read,
	uint32_t *bytes_read,
	uint32_t timeout_ms);

bool
ds_ipc_stream_write (
	DiagnosticsIpcStream *ipc_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written,
	uint32_t timeout_ms);

bool
ds_ipc_stream_flush (DiagnosticsIpcStream *ipc_stream);

bool
ds_ipc_stream_close (
	DiagnosticsIpcStream *ipc_stream,
	ds_ipc_error_callback_func callback);

int32_t
ds_ipc_stream_to_string (
	DiagnosticsIpcStream *ipc_stream,
	ep_char8_t *buffer,
	uint32_t buffer_len);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_IPC_H__ */
