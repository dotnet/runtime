#include <config.h>

#ifdef ENABLE_PERFTRACING
#ifndef HOST_WIN32
#include "ds-rt-config.h"
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_IPC_POSIX_GETTER_SETTER
#include "ds-ipc-posix.h"
#include "ds-protocol.h"
#include "ds-rt.h"

#include <stdio.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/stat.h>

#if __GNUC__
#include <poll.h>
#else
#include <sys/poll.h>
#endif // __GNUC__

#ifdef __APPLE__
#define APPLICATION_CONTAINER_BASE_PATH_SUFFIX "/Library/Group Containers/"

// Not much to go with, but Max semaphore length on Mac is 31 characters. In a sandbox, the semaphore name
// must be prefixed with an application group ID. This will be 10 characters for developer ID and extra 2
// characters for group name. For example ABCDEFGHIJ.MS. We still need some characters left
// for the actual semaphore names.
#define MAX_APPLICATION_GROUP_ID_LENGTH 13
#endif // __APPLE__

/*
 * Forward declares of all static functions.
 */

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
	int client_socket,
	DiagnosticsIpcConnectionMode mode);

static
bool
ipc_init_listener (
	DiagnosticsIpc *ipc,
	struct sockaddr *server_address,
	size_t server_address_len,
	ds_ipc_error_callback_func callback);

/*
 * DiagnosticsIpc.
 */

#ifdef __APPLE__
static
bool
ipc_init_listener (
	DiagnosticsIpc *ipc,
	struct sockaddr *server_address,
	size_t server_address_len,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);

	bool success = false;

	// This will set the default permission bit to 600
	mode_t prev_mask = umask (~(S_IRUSR | S_IWUSR));

	int server_socket;
	DS_ENTER_BLOCKING_PAL_SECTION;
	server_socket = socket (AF_UNIX, SOCK_STREAM, 0 );
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (server_socket == -1) {
		if (callback)
			callback (strerror (errno), errno);
		umask(prev_mask);
		EP_ASSERT (!"Failed to create diagnostics IPC socket.");
		ep_raise_error ();
	}

	int result_bind;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_bind = bind (server_socket, server_address, server_address_len);
	DS_EXIT_BLOCKING_PAL_SECTION;

	EP_ASSERT (result_bind != -1);
	if (result_bind == -1) {
		if (callback)
			callback (strerror (errno), errno);

		int result_close;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_close = close (server_socket);
		DS_EXIT_BLOCKING_PAL_SECTION;

		EP_ASSERT (result_close != -1);
		if (result_close == -1) {
			if (callback)
				callback (strerror (errno), errno);
		}

		umask (prev_mask);
		ep_raise_error ();
	}

	umask (prev_mask);

	ipc->server_socket = server_socket;
	success = true;

ep_on_exit:
	return success;

ep_on_error:
	success = false;
	ep_exit_error_handler ();
}
#else
static
bool
ipc_init_listener (
	DiagnosticsIpc *ipc,
	struct sockaddr *server_address,
	size_t server_address_len,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);

	bool success = false;

	int server_socket;
	DS_ENTER_BLOCKING_PAL_SECTION;
	server_socket = socket (AF_UNIX, SOCK_STREAM, 0);
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (server_socket == -1) {
		if (callback)
			callback (strerror (errno), errno);
		EP_ASSERT (!"Failed to create diagnostics IPC socket.");
		ep_raise_error ();
	}

	int result_fchmod;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_fchmod = fchmod (server_socket, S_IRUSR | S_IWUSR);
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (result_fchmod == -1) {
		if (callback)
			callback (strerror (errno), errno);
		EP_ASSERT (!"Failed to set permissions on diagnostics IPC socket.");
		ep_raise_error ();
	}

	int result_bind;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_bind = bind (server_socket, server_address, server_address_len);
	DS_EXIT_BLOCKING_PAL_SECTION;

	EP_ASSERT (result_bind != -1);
	if (result_bind == -1) {
		if (callback)
			callback (strerror (errno), errno);

		int result_close;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_close = close (server_socket);
		DS_EXIT_BLOCKING_PAL_SECTION;

		EP_ASSERT (result_close != -1);
		if (result_close == -1) {
			if (callback)
				callback (strerror (errno), errno);
		}

		ep_raise_error ();
	}

	ipc->server_socket = server_socket;
	success = true;

ep_on_exit:
	return success;

ep_on_error:
	success = false;
	ep_exit_error_handler ();
}
#endif

DiagnosticsIpc *
ds_ipc_alloc (
	const ep_char8_t *pipe_name,
	DiagnosticsIpcConnectionMode mode,
	ds_ipc_error_callback_func callback)
{
	DiagnosticsIpc *instance = NULL;
	struct sockaddr_un *server_address = ep_rt_object_alloc (struct sockaddr_un);
	ep_raise_error_if_nok (server_address != NULL);

	server_address->sun_family = AF_UNIX;

	if (pipe_name) {
		int32_t result = ep_rt_utf8_string_snprintf (
			server_address->sun_path,
			sizeof (server_address->sun_path),
			"%s",
			pipe_name);
		if (result <= 0 || result >= (int32_t)(sizeof (server_address->sun_path)))
			server_address->sun_path [0] = '\0';
	} else {
		// generate the default socket name
		ds_rt_transport_get_default_name (
			server_address->sun_path,
			sizeof (server_address->sun_path),
			"dotnet-diagnostic",
			ep_rt_current_process_get_id (),
			NULL,
			"socket");
	}

	instance = ep_rt_object_alloc (DiagnosticsIpc);
	ep_raise_error_if_nok (instance != NULL);

	instance->mode = mode;
	instance->server_socket = -1;
	instance->server_address = server_address;
	instance->is_closed = false;
	instance->is_listening = false;

	// Ownership transfered.
	server_address = NULL;

	if (mode == DS_IPC_CONNECTION_MODE_LISTEN)
		ep_raise_error_if_nok (ipc_init_listener (instance, (struct sockaddr *)instance->server_address, sizeof (*instance->server_address), callback) == true);

ep_on_exit:
	return instance;

ep_on_error:
	ep_rt_object_free (server_address);
	ds_ipc_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ds_ipc_free (DiagnosticsIpc *ipc)
{
	ep_return_void_if_nok (ipc != NULL);

	ds_ipc_close (ipc, false, NULL);
	ep_rt_object_free (ipc->server_address);
	ep_rt_object_free (ipc);
}

int32_t
ds_ipc_poll (
	ds_rt_ipc_poll_handle_array_t *poll_handles,
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (poll_handles);

	int32_t result = -1;
	DiagnosticsIpcPollHandle * poll_handles_data = ds_rt_ipc_poll_handle_array_data (poll_handles);
	size_t poll_handles_data_len = ds_rt_ipc_poll_handle_array_size (poll_handles);

	// prepare the pollfd structs
	struct pollfd *poll_fds = ep_rt_object_array_alloc (struct pollfd, poll_handles_data_len);
	ep_raise_error_if_nok (poll_fds != NULL);

	for (uint32_t i = 0; i < poll_handles_data_len; ++i) {
		ds_ipc_poll_handle_set_events (&poll_handles_data [i], 0); // ignore any input on events.
		int fd = -1;
		if (ds_ipc_poll_handle_get_ipc (&poll_handles_data [i])) {
			// SERVER
			EP_ASSERT (ds_ipc_poll_handle_get_ipc (&(poll_handles_data [i]))->mode == DS_IPC_CONNECTION_MODE_LISTEN);
			fd = ds_ipc_poll_handle_get_ipc (&poll_handles_data [i])->server_socket;
		} else {
			// CLIENT
			EP_ASSERT (poll_handles_data [i].stream != NULL);
			fd = ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->client_socket;
		}

		poll_fds [i].fd = fd;
		poll_fds [i].events = POLLIN;
	}

	int result_poll;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_poll = poll (poll_fds, poll_handles_data_len, timeout_ms);
	DS_EXIT_BLOCKING_PAL_SECTION;

	// Check results
	if (result_poll < 0) {
		// If poll() returns with an error, including one due to an interrupted call, the fds
		// array will be unmodified and the global variable errno will be set to indicate the error.
		// - POLL(2)
		if (callback)
			callback (strerror (errno), errno);
		ep_raise_error ();
	} else if (result_poll == 0) {
		// we timed out
		result = 0;
		ep_raise_error ();
	}

	for (uint32_t i = 0; i < poll_handles_data_len; ++i) {
		if (poll_fds [i].revents != 0) {
			if (callback)
				callback ("IpcStream::DiagnosticsIpc::Poll - poll revents", (uint32_t)poll_fds [i].revents);
			// error check FIRST
			if (poll_fds [i].revents & POLLHUP) {
				// check for hangup first because a closed socket
				// will technically meet the requirements for POLLIN
				// i.e., a call to recv/read won't block
				ds_ipc_poll_handle_set_events (&poll_handles_data [i], (uint8_t)DS_IPC_POLL_EVENTS_HANGUP);
			} else if ((poll_fds [i].revents & (POLLERR|POLLNVAL))) {
				if (callback)
					callback ("Poll error", (uint32_t)poll_fds [i].revents);
				ds_ipc_poll_handle_set_events (&poll_handles_data [i], (uint8_t)DS_IPC_POLL_EVENTS_ERR);
			} else if (poll_fds [i].revents & (POLLIN|POLLPRI)) {
				ds_ipc_poll_handle_set_events (&poll_handles_data [i], (uint8_t)DS_IPC_POLL_EVENTS_SIGNALED);
			} else {
				ds_ipc_poll_handle_set_events (&poll_handles_data [i], (uint8_t)DS_IPC_POLL_EVENTS_UNKNOWN);
				if (callback)
					callback ("unkown poll response", (uint32_t)poll_fds [i].revents);
			}
		}
	}

	result = 1;

ep_on_exit:
	ep_rt_object_array_free (poll_fds);
	return result;

ep_on_error:
	if (result != 0)
		result = -1;

	ep_exit_error_handler ();
}

bool
ds_ipc_listen (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback)
{
	bool result = false;
	EP_ASSERT (ipc != NULL);

	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);
	if (ipc->mode != DS_IPC_CONNECTION_MODE_LISTEN) {
		if (callback)
			callback ("Cannot call Listen on a client connection", -1);
		return false;
	}

	if (ipc->is_listening)
		return true;

	EP_ASSERT (ipc->server_socket != -1);

	int result_listen;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_listen = listen (ipc->server_socket, /* backlog */ 255);
	DS_EXIT_BLOCKING_PAL_SECTION;

	EP_ASSERT (result_listen != -1);
	if (result_listen == -1) {
		if (callback)
			callback (strerror (errno), errno);

		int result_unlink;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_unlink = unlink (ipc->server_address->sun_path);
		DS_EXIT_BLOCKING_PAL_SECTION;

		EP_ASSERT (result_unlink != -1);
		if (result_unlink == -1) {
			if (callback)
				callback (strerror (errno), errno);
		}

		int result_close;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_close = close (ipc->server_socket);
		DS_EXIT_BLOCKING_PAL_SECTION;

		EP_ASSERT (result_close != -1);
		if (result_close == -1) {
			if (callback)
				callback (strerror (errno), errno);
		}

		ep_raise_error ();
	}

	ipc->is_listening = true;
	result = true;

ep_on_exit:
	return result;

ep_on_error:
	result = false;
	ep_exit_error_handler ();
}

DiagnosticsIpcStream *
ds_ipc_accept (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);
	DiagnosticsIpcStream *stream = NULL;

	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);
	EP_ASSERT (ipc->is_listening);

	struct sockaddr_un from;
	socklen_t from_len = sizeof (from);

	int client_socket;
	DS_ENTER_BLOCKING_PAL_SECTION;
	client_socket = accept (ipc->server_socket, (struct sockaddr *)&from, &from_len);
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (client_socket == -1) {
		if (callback)
			callback (strerror (errno), errno);
		ep_raise_error ();
	}

	stream = ipc_stream_alloc (client_socket, ipc->mode);

ep_on_exit:
	return stream;

ep_on_error:
	ds_ipc_stream_free (stream);
	stream = NULL;
	ep_exit_error_handler ();
}

DiagnosticsIpcStream *
ds_ipc_connect (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);
	DiagnosticsIpcStream *stream = NULL;

	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_CONNECT);

	struct sockaddr_un client_address;
	client_address.sun_family = AF_UNIX;

	int client_socket;
	DS_ENTER_BLOCKING_PAL_SECTION;
	client_socket = socket (AF_UNIX, SOCK_STREAM, 0);
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (client_socket == -1) {
		if (callback)
			callback (strerror (errno), errno);
		ep_raise_error ();
	}

	// We don't expect this to block since this is a Unix Domain Socket.  `connect` may block until the
	// TCP handshake is complete for TCP/IP sockets, but UDS don't use TCP.  `connect` will return even if
	// the server hasn't called `accept`.
	int result_connect;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_connect = connect (client_socket, (struct sockaddr *)ipc->server_address, sizeof(*(ipc->server_address)));
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (result_connect < 0) {
		if (callback)
			callback (strerror (errno), errno);

		int result_close;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_close = close (client_socket);
		DS_EXIT_BLOCKING_PAL_SECTION;

		if (result_close < 0 && callback)
			callback (strerror (errno), errno);

		ep_raise_error ();
	}

	stream = ipc_stream_alloc (client_socket, DS_IPC_CONNECTION_MODE_CONNECT);

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
	ep_return_void_if_nok (ipc->is_closed == false);

	ipc->is_closed = true;

	if (ipc->server_socket != -1) {
		// only close the socket if not shutting down, let the OS handle it in that case
		if (!is_shutdown) {
			int close_result;
			DS_ENTER_BLOCKING_PAL_SECTION;
			close_result = close (ipc->server_socket);
			DS_EXIT_BLOCKING_PAL_SECTION;

			if (close_result == -1) {
				if (callback)
					callback (strerror (errno), errno);
				EP_ASSERT (!"Failed to close unix domain socket.");
			}
		}

		// N.B. - it is safe to unlink the unix domain socket file while the server
		// is still alive:
		// "The usual UNIX close-behind semantics apply; the socket can be unlinked
		// at any time and will be finally removed from the file system when the last
		// reference to it is closed." - unix(7) man page
		int result_unlink;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_unlink = unlink (ipc->server_address->sun_path);
		DS_EXIT_BLOCKING_PAL_SECTION;

		if (result_unlink == -1) {
			if (callback)
				callback (strerror (errno), errno);
			EP_ASSERT (!"Failed to unlink server address.");
		}
	}
}

int32_t
ds_ipc_to_string (
	DiagnosticsIpc *ipc,
	ep_char8_t *buffer,
	uint32_t buffer_len)
{
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len <= DS_IPC_MAX_TO_STRING_LEN);

	int32_t result = ep_rt_utf8_string_snprintf (buffer, buffer_len, "{ server_socket = %d }", ipc->server_socket);
	return (result > 0 && result < (int32_t)buffer_len) ? result : 0;
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
	uint8_t *buffer_cursor = (uint8_t*)buffer;
	ssize_t current_bytes_read = 0;
	ssize_t total_bytes_read = 0;
	bool continue_recv = true;

	if (timeout_ms != DS_IPC_STREAM_TIMEOUT_INFINITE) {
		struct pollfd pfd;
		pfd.fd = ipc_stream->client_socket;
		pfd.events = POLLIN;

		int result_poll;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_poll = poll (&pfd, 1, timeout_ms);
		DS_EXIT_BLOCKING_PAL_SECTION;

		if (result_poll <= 0 || !(pfd.revents & POLLIN)) {
			// timeout or error
			ep_raise_error ();
		}
		// else fallthrough
	}

	DS_ENTER_BLOCKING_PAL_SECTION;
	while (continue_recv && bytes_to_read - total_bytes_read > 0) {
		current_bytes_read = recv (
			ipc_stream->client_socket,
			buffer_cursor,
			bytes_to_read - total_bytes_read,
			0);
		continue_recv = current_bytes_read > 0;
		if (!continue_recv)
			break;
		total_bytes_read += current_bytes_read;
		buffer_cursor += current_bytes_read;
	}
	DS_EXIT_BLOCKING_PAL_SECTION;

	ep_raise_error_if_nok (continue_recv == true);
	success = true;

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
	uint8_t *buffer_cursor = (uint8_t*)buffer;
	ssize_t current_bytes_written = 0;
	ssize_t total_bytes_written = 0;
	bool continue_send = true;

	if (timeout_ms != DS_IPC_STREAM_TIMEOUT_INFINITE) {
		struct pollfd pfd;
		pfd.fd = ipc_stream->client_socket;
		pfd.events = POLLOUT;

		int result_poll;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_poll = poll (&pfd, 1, timeout_ms);
		DS_EXIT_BLOCKING_PAL_SECTION;

		if (result_poll <= 0 || !(pfd.revents & POLLOUT)) {
			// timeout or error
			ep_raise_error ();
		}
		// else fallthrough
	}

	DS_ENTER_BLOCKING_PAL_SECTION;
	while (continue_send && bytes_to_write - total_bytes_written > 0) {
		current_bytes_written = send (
			ipc_stream->client_socket,
			buffer_cursor,
			bytes_to_write - total_bytes_written,
			0);
		continue_send = current_bytes_written != -1;
		if (!continue_send)
			break;
		total_bytes_written += current_bytes_written;
		buffer_cursor += current_bytes_written;
	}
	DS_EXIT_BLOCKING_PAL_SECTION;

	ep_raise_error_if_nok (continue_send == true);
	success = true;

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
	// fsync - http://man7.org/linux/man-pages/man2/fsync.2.html ???
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
	int client_socket,
	DiagnosticsIpcConnectionMode mode)
{
	DiagnosticsIpcStream *instance = ep_rt_object_alloc (DiagnosticsIpcStream);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_ipc_stream_init (&instance->stream, &ipc_stream_vtable) != NULL);

	instance->client_socket = client_socket;
	instance->mode = mode;

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
	ep_return_void_if_nok (ipc_stream != NULL);
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

	if (ipc_stream->client_socket != -1) {
		ds_ipc_stream_flush (ipc_stream);

		int result_close;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_close = close (ipc_stream->client_socket);
		DS_EXIT_BLOCKING_PAL_SECTION;

		EP_ASSERT (result_close != -1);
		if (result_close == -1) {
			if (callback)
				callback (strerror (errno), errno);
		}

		ipc_stream->client_socket = -1;
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

	int32_t result = ep_rt_utf8_string_snprintf (buffer, buffer_len, "{ client_socket = %d }", ipc_stream->client_socket);
	return (result > 0 && result < (int32_t)buffer_len) ? result : 0;
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* !HOST_WIN32 */
#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_ipc_posix;
const char quiet_linker_empty_file_warning_diagnostics_ipc_posix = 0;
#endif
