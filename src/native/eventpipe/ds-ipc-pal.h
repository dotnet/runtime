#ifndef __DIAGNOSTICS_IPC_PAL_H__
#define __DIAGNOSTICS_IPC_PAL_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ds-ipc-pal-types.h"
#include "ep-ipc-stream.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_PAL_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
 * DiagnosticsIpc.
 */

bool
ds_ipc_pal_init (void);

bool
ds_ipc_pal_shutdown (void);

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
// - IpcPollHandle * poll_handles_data: Array of IpcPollHandles to poll
// - uint32_t timeout_ms: The timeout in milliseconds for the poll ((uint32_t)-1 == infinite)
// Returns:
// int32_t: -1 on error, 0 on timeout, >0 on successful poll
// Remarks:
// Check the events returned in revents for each IpcPollHandle to find the signaled handle.
// Signaled DiagnosticsIpcs can call accept() without blocking.
// Signaled IpcStreams can call read(...) without blocking.
// The caller is responsible for cleaning up "hung up" connections.
int32_t
ds_ipc_poll (
	DiagnosticsIpcPollHandle *poll_handles_data,
	size_t poll_handles_data_len,
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
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback,
	bool *timed_out);

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
#endif /* __DIAGNOSTICS_IPC_PAL_H__ */
