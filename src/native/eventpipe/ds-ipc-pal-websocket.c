// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_PERFTRACING_STANDALONE_PAL
#define EP_NO_RT_DEPENDENCY
#endif

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING

#define DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER
#include "ds-ipc-pal-websocket.h"

#define DS_IPC_INVALID_SOCKET -1

#ifndef EP_NO_RT_DEPENDENCY
#include "ds-rt.h"
#else
#ifdef FEATURE_CORECLR
#include <pal.h>
#include "processdescriptor.h"
#endif

#ifndef ep_return_null_if_nok
#define ep_return_null_if_nok(expr) do { if (!(expr)) return NULL; } while (0)
#endif

#ifndef ep_raise_error_if_nok
#define ep_raise_error_if_nok(expr) do { if (!(expr)) goto ep_on_error; } while (0)
#endif

#ifndef ep_raise_error
#define ep_raise_error() do { goto ep_on_error; } while (0)
#endif

#ifndef ep_exit_error_handler
#define ep_exit_error_handler() do { goto ep_on_exit; } while (0)
#endif

#ifndef EP_ASSERT
#define EP_ASSERT assert
#endif

#ifndef DS_ENTER_BLOCKING_PAL_SECTION
#define DS_ENTER_BLOCKING_PAL_SECTION
#endif

#ifndef DS_EXIT_BLOCKING_PAL_SECTION
#define DS_EXIT_BLOCKING_PAL_SECTION
#endif

#undef ep_rt_object_alloc
#define ep_rt_object_alloc(obj_type) ((obj_type *)calloc(1, sizeof(obj_type)))

static
inline
void
ep_rt_object_free (void *ptr)
{
	if (ptr)
		free (ptr);
}

#undef ep_rt_object_array_alloc
#define ep_rt_object_array_alloc(obj_type,size) ((obj_type *)calloc (size, sizeof(obj_type)))

static
inline
void
ep_rt_object_array_free (void *ptr)
{
	if (ptr)
		free (ptr);
}
#endif

static bool _ipc_pal_socket_init = false;

/*
 * Forward declares of all static functions.
 */

static
bool
ipc_socket_recv (
	ds_ipc_websocket_t s,
	uint8_t * buffer,
	ssize_t bytes_to_read,
	ssize_t *bytes_read);

static
bool
ipc_socket_send (
	ds_ipc_websocket_t s,
	const uint8_t *buffer,
	ssize_t bytes_to_write,
	ssize_t *bytes_written);

static
bool
ipc_transport_get_default_name (
	ep_char8_t *name,
	int32_t name_len);

static
bool
ipc_init_listener (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback);

static
DiagnosticsIpc *
ipc_alloc_ws_address (
	DiagnosticsIpc *ipc,
	DiagnosticsIpcConnectionMode mode,
	const ep_char8_t *ipc_name);

static
void
ipc_stream_free_func (void *object);

static
bool
ipc_stream_read_func (
	void *object,
	uint8_t *buffer,
	uint32_t bytes_to_read,
	uint32_t *bytes_read,
	uint32_t timeout_ms);

static
bool
ipc_stream_write_func (
	void *object,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written,
	uint32_t timeout_ms);

static
bool
ipc_stream_flush_func (void *object);

static
bool
ipc_stream_close_func (void *object);

static
DiagnosticsIpcStream *
ipc_stream_alloc (
	int client_socket);
/*
 * Implementation
 */


static
DiagnosticsIpc *
ipc_alloc_ws_address (
	DiagnosticsIpc *ipc,
	DiagnosticsIpcConnectionMode mode,
	const ep_char8_t *ipc_name)
{
	EP_ASSERT (ipc != NULL);
	ep_return_null_if_nok (ipc_name != NULL);

	ipc->server_url = ep_rt_utf8_string_dup (ipc_name);

	ep_raise_error_if_nok (ipc->server_url != NULL);

ep_on_exit:
	return ipc;

ep_on_error:
	ipc = NULL;
	ep_exit_error_handler ();
}


/*
 * DiagnosticsIpc Socket PAL.
 */

bool
ds_ipc_pal_init (void)
{
	return true;
}

bool
ds_ipc_pal_shutdown (void)
{
	return true;
}

DiagnosticsIpc *
ds_ipc_alloc (
	const ep_char8_t *ipc_name,
	DiagnosticsIpcConnectionMode mode,
	ds_ipc_error_callback_func callback)
{
	DiagnosticsIpc *instance = NULL;

	instance = ep_rt_object_alloc (DiagnosticsIpc);
	ep_raise_error_if_nok (instance != NULL);

	instance->server_socket = DS_IPC_INVALID_SOCKET;
	instance->is_closed = false;

	ep_raise_error_if_nok (ipc_alloc_ws_address (instance, mode, ipc_name) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ds_ipc_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ds_ipc_free (DiagnosticsIpc *ipc)
{
	if (!ipc)
		return;

	ds_ipc_close (ipc, false, NULL);
	ep_rt_object_free (ipc->server_url);
	ep_rt_object_free (ipc);
}

void
ds_ipc_reset (DiagnosticsIpc *ipc)
{
}

int32_t
ds_ipc_poll (
	DiagnosticsIpcPollHandle *poll_handles_data,
	size_t poll_handles_data_len,
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback)
{
	for (uint32_t i = 0; i < poll_handles_data_len; ++i) {
		// CLIENT
		EP_ASSERT (poll_handles_data [i].stream != NULL);
		int client_socket = poll_handles_data [i].stream->client_socket;
		int pending = ds_rt_websocket_poll (client_socket);
		if (pending < 0){
			poll_handles_data [i].events = (uint8_t)DS_IPC_POLL_EVENTS_ERR;
			return 1;
		}
		if (pending > 0){
			poll_handles_data [i].events = (uint8_t)DS_IPC_POLL_EVENTS_SIGNALED;
			return 1;
		}
	}

	return 0;
}

bool
ds_ipc_listen (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback)
{
	EP_UNREACHABLE ("Not supported by browser WebSocket");
	// NOT SUPPORTED
	return false;
}

DiagnosticsIpcStream *
ds_ipc_accept (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback)
{
	EP_UNREACHABLE ("Not supported by browser WebSocket");
	// NOT SUPPORTED
	return NULL;
}

DiagnosticsIpcStream *
ds_ipc_connect (
	DiagnosticsIpc *ipc,
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback,
	bool *timed_out)
{
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (timed_out != NULL);

	DiagnosticsIpcStream *stream = NULL;

	bool success = false;
	*timed_out = false;
	int client_socket = ds_rt_websocket_create (ipc->server_url);

	success = client_socket > 0;
	ep_raise_error_if_nok (success == true);

	stream = ipc_stream_alloc ((ds_ipc_websocket_t)client_socket);

ep_on_exit:
	return stream;

ep_on_error:
	ds_ipc_stream_free (stream);
	stream = NULL;

	ep_exit_error_handler ();
}

void
ds_ipc_close (
	DiagnosticsIpc *ipc,
	bool is_shutdown,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);
	if (ipc->is_closed)
		return;

	ipc->is_closed = true;
}

int32_t
ds_ipc_to_string (
	DiagnosticsIpc *ipc,
	ep_char8_t *buffer,
	uint32_t buffer_len)
{
	EP_UNREACHABLE ("Not supported by browser WebSocket");
	// NOT SUPPORTED
	return 0;
}

/*
 * DiagnosticsIpcStream.
 */

static
void
ipc_stream_free_func (void *object)
{
	EP_ASSERT (object != NULL);
	DiagnosticsIpcStream *ipc_stream = (DiagnosticsIpcStream *)object;
	ds_ipc_stream_free (ipc_stream);
}

static
bool
ipc_stream_read_func (
	void *object,
	uint8_t *buffer,
	uint32_t bytes_to_read,
	uint32_t *bytes_read,
	uint32_t timeout_ms)
{
	EP_ASSERT (object != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (bytes_read != NULL);

	bool success = false;
	DiagnosticsIpcStream *ipc_stream = (DiagnosticsIpcStream *)object;
	int total_bytes_read = ds_rt_websocket_recv (ipc_stream->client_socket, buffer, bytes_to_read);

	success = total_bytes_read >= 0;
	ep_raise_error_if_nok (success == true);

ep_on_exit:
	*bytes_read = (uint32_t)total_bytes_read;
	return success;

ep_on_error:
	total_bytes_read = 0;
	success = false;
	ep_exit_error_handler ();
}

static
bool
ipc_stream_write_func (
	void *object,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written,
	uint32_t timeout_ms)
{
	EP_ASSERT (object != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (bytes_written != NULL);

	bool success = false;
	DiagnosticsIpcStream *ipc_stream = (DiagnosticsIpcStream *)object;

	int total_bytes_written = ds_rt_websocket_send (ipc_stream->client_socket, buffer, bytes_to_write);
	success = total_bytes_written >= 0;
	
	ep_raise_error_if_nok (success == true);

ep_on_exit:
	*bytes_written = (uint32_t)total_bytes_written;
	return success;

ep_on_error:
	total_bytes_written = 0;
	success = false;
	ep_exit_error_handler ();
}

static
bool
ipc_stream_flush_func (void *object)
{
	return true;
}

static
bool
ipc_stream_close_func (void *object)
{
	EP_ASSERT (object != NULL);
	DiagnosticsIpcStream *ipc_stream = (DiagnosticsIpcStream *)object;
	return ds_ipc_stream_close (ipc_stream, NULL);
}

static IpcStreamVtable ipc_stream_vtable = {
	ipc_stream_free_func,
	ipc_stream_read_func,
	ipc_stream_write_func,
	ipc_stream_flush_func,
	ipc_stream_close_func };

static
DiagnosticsIpcStream *
ipc_stream_alloc (
	int client_socket)
{
	DiagnosticsIpcStream *instance = ep_rt_object_alloc (DiagnosticsIpcStream);
	ep_raise_error_if_nok (instance != NULL);

	instance->stream.vtable = &ipc_stream_vtable;
	instance->client_socket = client_socket;

ep_on_exit:
	return instance;

ep_on_error:
	ds_ipc_stream_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

IpcStream *
ds_ipc_stream_get_stream_ref (DiagnosticsIpcStream *ipc_stream)
{
	return &ipc_stream->stream;
}

int32_t
ds_ipc_stream_get_handle_int32_t (DiagnosticsIpcStream *ipc_stream)
{
	return (int32_t)ipc_stream->client_socket;
}

void
ds_ipc_stream_free (DiagnosticsIpcStream *ipc_stream)
{
	if(!ipc_stream)
		return;

	ds_ipc_stream_close (ipc_stream, NULL);
	ep_rt_object_free (ipc_stream);
}

bool
ds_ipc_stream_read (
	DiagnosticsIpcStream *ipc_stream,
	uint8_t *buffer,
	uint32_t bytes_to_read,
	uint32_t *bytes_read,
	uint32_t timeout_ms)
{
	return ipc_stream_read_func (
		ipc_stream,
		buffer,
		bytes_to_read,
		bytes_read,
		timeout_ms);
}

bool
ds_ipc_stream_write (
	DiagnosticsIpcStream *ipc_stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written,
	uint32_t timeout_ms)
{
	return ipc_stream_write_func (
		ipc_stream,
		buffer,
		bytes_to_write,
		bytes_written,
		timeout_ms);
}

bool
ds_ipc_stream_flush (DiagnosticsIpcStream *ipc_stream)
{
	return ipc_stream_flush_func (ipc_stream);
}

bool
ds_ipc_stream_close (
	DiagnosticsIpcStream *ipc_stream,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc_stream != NULL);

	if (ipc_stream->client_socket != DS_IPC_INVALID_SOCKET) {
		ds_ipc_stream_flush (ipc_stream);

		int res = ds_rt_websocket_close (ipc_stream->client_socket);

		ipc_stream->client_socket = DS_IPC_INVALID_SOCKET;

		return res == 0;
	}

	return true;
}

int32_t
ds_ipc_stream_to_string (
	DiagnosticsIpcStream *ipc_stream,
	ep_char8_t *buffer,
	uint32_t buffer_len)
{
	EP_ASSERT (ipc_stream != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len <= DS_IPC_MAX_TO_STRING_LEN);

	int32_t result = snprintf (buffer, buffer_len, "{ client_socket = %d }", (int32_t)(size_t)ipc_stream->client_socket);
	return (result > 0 && result < (int32_t)buffer_len) ? result : 0;
}

#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_ipc_pal_socket;
const char quiet_linker_empty_file_warning_diagnostics_ipc_pal_socket = 0;
#endif
