#include <config.h>

#ifdef ENABLE_PERFTRACING
#ifdef HOST_WIN32
#include "ds-rt-config.h"
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_IPC_WIN32_GETTER_SETTER
#include "ds-ipc-win32.h"
#include "ds-protocol.h"
#include "ds-rt.h"

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
	HANDLE pipe,
	DiagnosticsIpcConnectionMode mode);

/*
 * DiagnosticsIpc.
 */

DiagnosticsIpc *
ds_ipc_alloc (
	const ep_char8_t *pipe_name,
	DiagnosticsIpcConnectionMode mode,
	ds_ipc_error_callback_func callback)
{
	int32_t characters_written = -1;

	DiagnosticsIpc *instance = ep_rt_object_alloc (DiagnosticsIpc);
	ep_raise_error_if_nok (instance != NULL);

	instance->mode = mode;
	instance->is_listening = false;

	// All memory zeroed on alloc.
	//memset (&instance->overlap, 0, sizeof (instance->overlap));

	instance->overlap.hEvent = INVALID_HANDLE_VALUE;
	instance->pipe = INVALID_HANDLE_VALUE;

	if (pipe_name) {
		characters_written = ep_rt_utf8_string_snprintf (
			&instance->pipe_name,
			DS_IPC_WIN32_MAX_NAMED_PIPE_LEN,
			"\\\\.\\pipe\\%s",
			pipe_name);
	} else {
		characters_written = ep_rt_utf8_string_snprintf (
			&instance->pipe_name,
			DS_IPC_WIN32_MAX_NAMED_PIPE_LEN,
			"\\\\.\\pipe\\dotnet-diagnostic-%d",
			ep_rt_current_process_get_id ());
	}

	if (characters_written == -1) {
		if (callback)
			callback ("Failed to generate the named pipe name", characters_written);
		ep_raise_error ();
	}

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
	ep_return_void_if_nok (ipc != NULL);

	ds_ipc_close (ipc, false, NULL);
	ep_rt_object_free (ipc);
}

int32_t
ds_ipc_poll (
	ds_rt_ipc_poll_handle_array_t *poll_handles,
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (poll_handles);

	int32_t result = 1;
	DiagnosticsIpcPollHandle * poll_handles_data = ds_rt_ipc_poll_handle_array_data (poll_handles);
	size_t poll_handles_data_len = ds_rt_ipc_poll_handle_array_size (poll_handles);

	HANDLE handles [MAXIMUM_WAIT_OBJECTS];
	for (size_t i = 0; i < poll_handles_data_len; ++i) {
		ds_ipc_poll_handle_set_events (&poll_handles_data [i], 0); // ignore any input on events.
		if (ds_ipc_poll_handle_get_ipc (&poll_handles_data [i])) {
			// SERVER
			EP_ASSERT (poll_handles_data [i].ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);
			handles [i] = ds_ipc_poll_handle_get_ipc (&poll_handles_data [i])->overlap.hEvent;
		} else {
			// CLIENT
			bool success = true;
			DWORD bytes_read = 1;
			if (!ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->is_test_reading) {
				// check for data by doing an asynchronous 0 byte read.
				// This will signal if the pipe closes (hangup) or the server
				// sends new data
				success = ReadFile (
					ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->pipe,         // handle
					NULL,                                                                 // null buffer
					0,                                                                    // read 0 bytesd
					&bytes_read,                                                          // dummy variable
					&ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->overlap);    // overlap object to use

				ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->is_test_reading = true;
				if (!success) {
					DWORD error = GetLastError ();
					switch (error) {
					case ERROR_IO_PENDING:
						handles [i] = ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->overlap.hEvent;
						break;
					case ERROR_PIPE_NOT_CONNECTED:
						ds_ipc_poll_handle_set_events (&poll_handles_data [i], (uint8_t)DS_IPC_POLL_EVENTS_HANGUP);
						result = -1;
						ep_raise_error ();
					default:
						if (callback)
							callback ("0 byte async read on client connection failed", error);
						result = -1;
						ep_raise_error ();
					}
				} else {
					// there's already data to be read
					handles [i] = ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->overlap.hEvent;
				}
			} else {
				handles [i] = ds_ipc_poll_handle_get_stream (&poll_handles_data [i])->overlap.hEvent;
			}
		}
	}

	// call wait for multiple obj
	DWORD wait = WAIT_FAILED;
	DS_ENTER_BLOCKING_PAL_SECTION;
	wait = WaitForMultipleObjects (
		poll_handles_data_len,      // count
		handles,                    // handles
		false,                      // don't wait all
		timeout_ms);
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (wait == WAIT_TIMEOUT) {
		// we timed out
		result = 0;
		ep_raise_error ();
	}

	if (wait == WAIT_FAILED) {
		// we errored
		if (callback)
			callback ("WaitForMultipleObjects failed", GetLastError());
		result = -1;
		ep_raise_error ();
	}

	// determine which of the streams signaled
	DWORD index = wait - WAIT_OBJECT_0;
	// error check the index
	if (index < 0 || index > (poll_handles_data_len - 1)) {
		// check if we abandoned something
		DWORD abandonedIndex = wait - WAIT_ABANDONED_0;
		if (abandonedIndex > 0 || abandonedIndex < (poll_handles_data_len - 1)) {
			ds_ipc_poll_handle_set_events( &poll_handles_data [abandonedIndex], (uint8_t)DS_IPC_POLL_EVENTS_HANGUP);
			result = -1;
			ep_raise_error ();
		} else {
			if (callback)
				callback ("WaitForMultipleObjects failed", GetLastError());
			result = -1;
			ep_raise_error ();
		}
	}

	// Set revents depending on what signaled the stream
	if (!ds_ipc_poll_handle_get_ipc (&poll_handles_data [index])) {
		// CLIENT
		// check if the connection got hung up
		// Start with quick none blocking completion check.
		DWORD dummy = 0;
		BOOL success = GetOverlappedResult(
			ds_ipc_poll_handle_get_stream(&poll_handles_data [index])->pipe,
			&ds_ipc_poll_handle_get_stream(&poll_handles_data [index])->overlap,
			&dummy,
			false);
		if (!success && GetLastError () == ERROR_IO_INCOMPLETE) {
			// IO still incomplete, wait for completion.
			dummy = 0;
			DS_ENTER_BLOCKING_PAL_SECTION;
			success = GetOverlappedResult(
				ds_ipc_poll_handle_get_stream(&poll_handles_data [index])->pipe,
				&ds_ipc_poll_handle_get_stream(&poll_handles_data [index])->overlap,
				&dummy,
				true);
			DS_EXIT_BLOCKING_PAL_SECTION;
		}
		ds_ipc_poll_handle_get_stream(&poll_handles_data [index])->is_test_reading = false;
		if (!success) {
			DWORD error = GetLastError();
			if (error == ERROR_PIPE_NOT_CONNECTED || error == ERROR_BROKEN_PIPE) {
				ds_ipc_poll_handle_set_events(&poll_handles_data [index], (uint8_t)DS_IPC_POLL_EVENTS_HANGUP);
			} else {
				if (callback)
					callback ("Client connection error", error);
				ds_ipc_poll_handle_set_events (&poll_handles_data [index], (uint8_t)DS_IPC_POLL_EVENTS_ERR);
				result = -1;
				ep_raise_error ();
			}
		} else {
			ds_ipc_poll_handle_set_events (&poll_handles_data [index], (uint8_t)DS_IPC_POLL_EVENTS_SIGNALED);
		}
	} else {
		// SERVER
		ds_ipc_poll_handle_set_events (&poll_handles_data [index], (uint8_t)DS_IPC_POLL_EVENTS_SIGNALED);
	}

	result = 1;

ep_on_exit:
	return result;

ep_on_error:

	if (result == 1)
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

	EP_ASSERT (ipc->pipe == INVALID_HANDLE_VALUE);

	const uint32_t in_buffer_size = 16 * 1024;
	const uint32_t out_buffer_size = 16 * 1024;

	DS_ENTER_BLOCKING_PAL_SECTION;
	ipc->pipe = CreateNamedPipeA (
		ipc->pipe_name,                                             // pipe name
		PIPE_ACCESS_DUPLEX |                                        // read/write access
		FILE_FLAG_OVERLAPPED,                                       // async listening
		PIPE_TYPE_BYTE | PIPE_WAIT | PIPE_REJECT_REMOTE_CLIENTS,    // message type pipe, message-read and blocking mode
		PIPE_UNLIMITED_INSTANCES,                                   // max. instances
		out_buffer_size,                                            // output buffer size
		in_buffer_size,                                             // input buffer size
		0,                                                          // default client time-out
		NULL);                                                      // default security attribute
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (ipc->pipe == INVALID_HANDLE_VALUE) {
		if (callback)
			callback ("Failed to create an instance of a named pipe.", GetLastError());
		ep_raise_error ();
	}

	EP_ASSERT (ipc->overlap.hEvent == INVALID_HANDLE_VALUE);

	ipc->overlap.hEvent = CreateEvent (NULL, true, false, NULL);
	if (!ipc->overlap.hEvent) {
		if (callback)
			callback ("Failed to create overlap event", GetLastError());
		ep_raise_error ();
	}

	if (ConnectNamedPipe (ipc->pipe, &ipc->overlap) == FALSE) {
		const DWORD error_code = GetLastError ();
		switch (error_code) {
		case ERROR_IO_PENDING:
			// There was a pending connection that can be waited on (will happen in poll)
		case ERROR_PIPE_CONNECTED:
			// Occurs when a client connects before the function is called.
			// In this case, there is a connection between client and
			// server, even though the function returned zero.
			break;

		default:
			if (callback)
				callback ("A client process failed to connect.", error_code);
			ep_raise_error ();
		}
	}

	ipc->is_listening = true;
	result = true;

ep_on_exit:
	return result;

ep_on_error:
	ds_ipc_close (ipc, false, callback);
	result = false;
	ep_exit_error_handler ();
}

DiagnosticsIpcStream *
ds_ipc_accept (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);

	DiagnosticsIpcStream *stream = NULL;

	// Start with quick none blocking completion check.
	DWORD dummy = 0;
	BOOL success = GetOverlappedResult (
		ipc->pipe,      // handle
		&ipc->overlap,  // overlapped
		&dummy,         // throw-away dword
		false);         // wait till event signals

	if (!success && GetLastError () == ERROR_IO_INCOMPLETE) {
		// IO still incomplete, wait for completion.
		dummy = 0;
		DS_ENTER_BLOCKING_PAL_SECTION;
		success = GetOverlappedResult (
			ipc->pipe,      // handle
			&ipc->overlap,  // overlapped
			&dummy,         // throw-away dword
			true);          // wait till event signals
		DS_EXIT_BLOCKING_PAL_SECTION;
	}

	if (!success) {
		if (callback)
			callback ("Failed to GetOverlappedResults for NamedPipe server", GetLastError());
		ep_raise_error ();
	}

	// create new IpcStream using handle and reset the Server object so it can listen again
	stream = ipc_stream_alloc (ipc->pipe, DS_IPC_CONNECTION_MODE_LISTEN);
	ep_raise_error_if_nok (stream != NULL);

	// reset the server
	ipc->pipe = INVALID_HANDLE_VALUE;
	ipc->is_listening = false;
	CloseHandle (ipc->overlap.hEvent);
	memset(&ipc->overlap, 0, sizeof(OVERLAPPED)); // clear the overlapped objects state
	ipc->overlap.hEvent = INVALID_HANDLE_VALUE;

	ep_raise_error_if_nok (ds_ipc_listen (ipc, callback) == true);

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
	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_CONNECT);

	DiagnosticsIpcStream *stream = NULL;
	HANDLE pipe = INVALID_HANDLE_VALUE;

	if (ipc->mode != DS_IPC_CONNECTION_MODE_CONNECT) {
		if (callback)
			callback ("Cannot call connect on a server connection", 0);
		ep_raise_error ();
	}

	DS_ENTER_BLOCKING_PAL_SECTION;
	pipe = CreateFileA(
		ipc->pipe_name,         // pipe name
		PIPE_ACCESS_DUPLEX,     // read/write access
		0,                      // no sharing
		NULL,                   // default security attributes
		OPEN_EXISTING,          // opens existing pipe
		FILE_FLAG_OVERLAPPED,   // overlapped
		NULL);                  // no template file
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (pipe == INVALID_HANDLE_VALUE) {
		if (callback)
			callback ("Failed to connect to named pipe.", GetLastError ());
		ep_raise_error ();
	}

	stream = ipc_stream_alloc (pipe, ipc->mode);
	ep_raise_error_if_nok (stream);

	pipe = INVALID_HANDLE_VALUE;

ep_on_exit:
	return stream;

ep_on_error:
	ds_ipc_stream_free (stream);
	stream = NULL;

	if (pipe != INVALID_HANDLE_VALUE) {
		CloseHandle (pipe);
	}

	ep_exit_error_handler ();
}

void
ds_ipc_close (
	DiagnosticsIpc *ipc,
	bool is_shutdown,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);

	// don't attempt cleanup on shutdown and let the OS handle it
	if (is_shutdown) {
		if (callback)
			callback ("Closing without cleaning underlying handles", 100);
		return;
	}

	if (ipc->pipe != INVALID_HANDLE_VALUE) {
		if (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN) {
			BOOL success_disconnect = FALSE;
			DS_ENTER_BLOCKING_PAL_SECTION;
			success_disconnect = DisconnectNamedPipe (ipc->pipe);
			DS_EXIT_BLOCKING_PAL_SECTION;
			if (success_disconnect != TRUE && callback)
				callback ("Failed to disconnect NamedPipe", GetLastError());
		}

		const BOOL success_close_pipe = CloseHandle (ipc->pipe);
		if (success_close_pipe != TRUE && callback)
			callback ("Failed to close pipe handle", GetLastError());
		ipc->pipe = INVALID_HANDLE_VALUE;
	}

	if (ipc->overlap.hEvent != INVALID_HANDLE_VALUE) {
		const BOOL success_close_event = CloseHandle (ipc->overlap.hEvent);
		if (success_close_event != TRUE && callback)
			callback ("Failed to close overlap event handle", GetLastError());
		memset(&ipc->overlap, 0, sizeof(OVERLAPPED)); // clear the overlapped objects state
		ipc->overlap.hEvent = INVALID_HANDLE_VALUE;
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
	return ep_rt_utf8_string_snprintf (buffer, buffer_len, "{ _hPipe = %d, _oOverlap.hEvent = %d }", ipc->pipe, ipc->overlap.hEvent);
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

	DiagnosticsIpcStream *ipc_stream = (DiagnosticsIpcStream *)object;
	DWORD read = 0;
	LPOVERLAPPED overlap = &ipc_stream->overlap;

	bool success = ReadFile (
		ipc_stream->pipe,   // handle to pipe
		buffer,             // buffer to receive data
		bytes_to_read,      // size of buffer
		&read,              // number of bytes read
		overlap) != FALSE;  // overlapped I/O

	if (!success) {
		DWORD error = GetLastError ();
		if (error == ERROR_IO_PENDING) {
			// if we're waiting infinitely, only make one syscall
			if (timeout_ms == DS_IPC_WIN32_INFINITE_TIMEOUT) {
				DS_ENTER_BLOCKING_PAL_SECTION;
				success = GetOverlappedResult (
					ipc_stream->pipe,   // pipe
					overlap,            // overlapped
					&read,              // out actual number of bytes read
					true) != FALSE;     // block until async IO completes
				DS_EXIT_BLOCKING_PAL_SECTION;
			} else {
				// Wait on overlapped IO event (triggers when async IO is complete regardless of success)
				// or timeout
				DS_ENTER_BLOCKING_PAL_SECTION;
				DWORD wait = WaitForSingleObject (ipc_stream->overlap.hEvent, (DWORD)timeout_ms);
				if (wait == WAIT_OBJECT_0) {
					// async IO compelted, get the result
					success = GetOverlappedResult (
						ipc_stream->pipe,   // pipe
						overlap,            // overlapped
						&read,              // out actual number of bytes read
						true) != FALSE;     // block until async IO completes
				} else {
					// We either timed out or something else went wrong.
					// For any error, attempt to cancel IO and ensure the cancel happened
					if (CancelIoEx (ipc_stream->pipe, overlap) != FALSE) {
						// check if the async write beat the cancellation
						success = GetOverlappedResult (
							ipc_stream->pipe,   // pipe
							overlap,            // overlapped
							&read,              // out actual number of bytes read
							true) != FALSE;     // block until async IO completes
						// Failure here isn't recoverable, so return as such
					}
				}
				DS_EXIT_BLOCKING_PAL_SECTION;
			}
		}
	}

	*bytes_read = (uint32_t)read;
	return success;
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

	DiagnosticsIpcStream *ipc_stream = (DiagnosticsIpcStream *)object;
	DWORD written = 0;
	LPOVERLAPPED overlap = &ipc_stream->overlap;

	bool success = WriteFile (
		ipc_stream->pipe,   // handle to pipe
		buffer,             // buffer to write from
		bytes_to_write,     // number of bytes to write
		&written,           // number of bytes written
		overlap) != FALSE;  // overlapped I/O

	if (!success) {
		DWORD error = GetLastError ();
		if (error == ERROR_IO_PENDING) {
			// if we're waiting infinitely, only make one syscall
			if (timeout_ms == DS_IPC_WIN32_INFINITE_TIMEOUT) {
				DS_ENTER_BLOCKING_PAL_SECTION;
				success = GetOverlappedResult (
					ipc_stream->pipe,   // pipe
					overlap,            // overlapped
					&written,           // out actual number of bytes written
					true) != FALSE;     // block until async IO completes
				DS_EXIT_BLOCKING_PAL_SECTION;
			} else {
				// Wait on overlapped IO event (triggers when async IO is complete regardless of success)
				// or timeout
				DS_ENTER_BLOCKING_PAL_SECTION;
				DWORD wait = WaitForSingleObject (ipc_stream->overlap.hEvent, (DWORD)timeout_ms);
				if (wait == WAIT_OBJECT_0) {
					// async IO compelted, get the result
					success = GetOverlappedResult (
						ipc_stream->pipe,   // pipe
						overlap,            // overlapped
						&written,           // out actual number of bytes written
						true) != FALSE;         // block until async IO completes
				} else {
					// We either timed out or something else went wrong.
					// For any error, attempt to cancel IO and ensure the cancel happened
					if (CancelIoEx (ipc_stream->pipe, overlap) != FALSE) {
						// check if the async write beat the cancellation
						success = GetOverlappedResult (
							ipc_stream->pipe,   // pipe
							overlap,            // overlapped
							&written,           // out actual number of bytes written
							true) != FALSE;         // block until async IO completes
						// Failure here isn't recoverable, so return as such
					}
				}
				DS_EXIT_BLOCKING_PAL_SECTION;
			}
		}
	}

	*bytes_written = (uint32_t)written;
	return success;
}

static
bool
ipc_stream_flush_func (void *object)
{
	EP_ASSERT (object != NULL);

	DiagnosticsIpcStream *ipc_stream = (DiagnosticsIpcStream *)object;
	bool success = false;

	DS_ENTER_BLOCKING_PAL_SECTION;
	success = FlushFileBuffers (ipc_stream->pipe) != FALSE;
	DS_EXIT_BLOCKING_PAL_SECTION;

	// TODO: Add error handling.
	return success;
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
	HANDLE pipe,
	DiagnosticsIpcConnectionMode mode)
{
	DiagnosticsIpcStream *instance = ep_rt_object_alloc (DiagnosticsIpcStream);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ep_ipc_stream_init (&instance->stream, &ipc_stream_vtable) != NULL);

	instance->pipe = pipe;
	instance->mode = mode;

	// All memory zeroed on alloc.
	//memset (&instance->overlap, 0, sizeof (OVERLAPPED));

	instance->overlap.hEvent = CreateEvent (NULL, true, false, NULL);

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

	if (ipc_stream->pipe != INVALID_HANDLE_VALUE) {
		ds_ipc_stream_flush (ipc_stream);
		if (ipc_stream->mode == DS_IPC_CONNECTION_MODE_LISTEN) {
			BOOL success_disconnect = FALSE;
			DS_ENTER_BLOCKING_PAL_SECTION;
			success_disconnect = DisconnectNamedPipe (ipc_stream->pipe);
			DS_EXIT_BLOCKING_PAL_SECTION;
			if (success_disconnect != TRUE && callback)
				callback ("Failed to disconnect NamedPipe", GetLastError());
		}

		const BOOL success_close_pipe = CloseHandle (ipc_stream->pipe);
		if (success_close_pipe != TRUE && callback)
			callback ("Failed to close pipe handle", GetLastError());
		ipc_stream->pipe = INVALID_HANDLE_VALUE;
	}

	if (ipc_stream->overlap.hEvent != INVALID_HANDLE_VALUE) {
		const BOOL success_close_event = CloseHandle (ipc_stream->overlap.hEvent);
		if (success_close_event != TRUE && callback)
			callback ("Failed to close overlapped event handle", GetLastError());
		memset(&ipc_stream->overlap, 0, sizeof(OVERLAPPED)); // clear the overlapped objects state
		ipc_stream->overlap.hEvent = INVALID_HANDLE_VALUE;
	}

	ipc_stream->is_test_reading = false;

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
	return ep_rt_utf8_string_snprintf (buffer, buffer_len, "{ _hPipe = %d, _oOverlap.hEvent = %d }", ipc_stream->pipe, ipc_stream->overlap.hEvent);
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* HOST_WIN32 */
#endif /* ENABLE_PERFTRACING */

#ifndef DS_INCLUDE_SOURCE_FILES
extern const char quiet_linker_empty_file_warning_diagnostics_ipc_win32;
const char quiet_linker_empty_file_warning_diagnostics_ipc_win32 = 0;
#endif
