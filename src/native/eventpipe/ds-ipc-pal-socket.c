// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_PERFTRACING_STANDALONE_PAL
#define EP_NO_RT_DEPENDENCY
#endif

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING

#define DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER
#include "ds-ipc-pal-socket.h"

#if defined (DS_IPC_PAL_TCP)
#define DS_IPC_PAL_AF_INET
#define DS_IPC_PAL_AF_INET6
#elif defined (DS_IPC_PAL_UDS)
#define DS_IPC_PAL_AF_UNIX
#else
#error "Unsupported PAL socket configuration"
#endif

#ifndef HOST_WIN32
#include <assert.h>
#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <sys/un.h>
#include <sys/stat.h>

#if __GNUC__
#include <poll.h>
#else
#include <sys/poll.h>
#endif

#ifdef DS_IPC_PAL_TCP
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <netdb.h>
#endif
#endif

#ifdef HOST_WIN32
#define DS_IPC_INVALID_SOCKET INVALID_SOCKET
#define DS_IPC_SOCKET_ERROR SOCKET_ERROR
#define DS_IPC_SOCKET_ERROR_WOULDBLOCK WSAEWOULDBLOCK
#define DS_IPC_SOCKET_ERROR_TIMEDOUT WSAETIMEDOUT
#define DS_IPC_ERROR_INTERRUPT WSAEINTR
typedef ADDRINFOA ds_ipc_addrinfo_t;
typedef WSAPOLLFD ds_ipc_pollfd_t;
typedef int ds_ipc_mode_t;
#else
#define DS_IPC_INVALID_SOCKET -1
#define DS_IPC_SOCKET_ERROR -1
#define DS_IPC_SOCKET_ERROR_WOULDBLOCK EINPROGRESS
#define DS_IPC_SOCKET_ERROR_TIMEDOUT ETIMEDOUT
#define DS_IPC_ERROR_INTERRUPT EINTR
typedef struct addrinfo ds_ipc_addrinfo_t;
typedef struct pollfd ds_ipc_pollfd_t;
typedef mode_t ds_ipc_mode_t;
#endif

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
int
ipc_socket_connect (
	ds_ipc_socket_t s,
	ds_ipc_socket_address_t *address,
	int address_len,
	uint32_t timeout_ms);

static
bool
ipc_socket_recv (
	ds_ipc_socket_t s,
	uint8_t * buffer,
	ssize_t bytes_to_read,
	ssize_t *bytes_read);

static
bool
ipc_socket_send (
	ds_ipc_socket_t s,
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
ipc_alloc_uds_address (
	DiagnosticsIpc *ipc,
	DiagnosticsIpcConnectionMode mode,
	const ep_char8_t *ipc_name);

static
DiagnosticsIpc *
ipc_alloc_tcp_address (
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
	int client_socket,
	DiagnosticsIpcConnectionMode mode);

/*
 * DiagnosticsIpc Socket PAL.
 */

static
inline
int
ipc_get_last_error (void)
{
#ifdef HOST_WIN32
	return WSAGetLastError ();
#else
	return errno;
#endif
}

static
inline
void
ipc_set_last_error (int error)
{
#ifdef HOST_WIN32
	WSASetLastError (error);
#else
	errno = error;
#endif
}

static
inline
int
ipc_get_last_socket_error (ds_ipc_socket_t s)
{
	int opt_value = DS_IPC_SOCKET_ERROR;
	int result_getsockopt;
#ifdef HOST_WIN32
	int opt_value_len = sizeof (opt_value);
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_getsockopt = getsockopt (s, SOL_SOCKET, SO_ERROR, (char *)&opt_value, &opt_value_len);
	DS_EXIT_BLOCKING_PAL_SECTION;
#else
	socklen_t opt_value_len = sizeof (opt_value);
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_getsockopt = getsockopt (s, SOL_SOCKET, SO_ERROR, &opt_value, &opt_value_len);
	DS_EXIT_BLOCKING_PAL_SECTION;
#endif
	return result_getsockopt == 0 ? opt_value : result_getsockopt;
}

#ifndef EP_NO_RT_DEPENDENCY
static
inline
bool
ipc_retry_syscall (int result)
{
	return result == -1 && ipc_get_last_error () == DS_IPC_ERROR_INTERRUPT && !ep_rt_process_shutdown ();
}
#else
static
inline
bool
ipc_retry_syscall (int result)
{
	return result == -1 && ipc_get_last_error () == DS_IPC_ERROR_INTERRUPT;
}
#endif

static
inline
ds_ipc_mode_t
ipc_socket_set_default_umask (void)
{
#if defined(DS_IPC_PAL_AF_UNIX) && (defined(__APPLE__) || defined(__FreeBSD__))
	// This will set the default permission bit to 600
	return umask (~(S_IRUSR | S_IWUSR));
#else
	return 0;
#endif
}

static
inline
void
ipc_socket_reset_umask (ds_ipc_mode_t mode)
{
#if defined(DS_IPC_PAL_AF_UNIX) && (defined(__APPLE__) || defined(__FreeBSD__))
	umask (mode);
#endif
}

static
inline
ds_ipc_socket_t
ipc_socket_create_uds (DiagnosticsIpc *ipc)
{
#if defined(DS_IPC_PAL_AF_UNIX)
	EP_ASSERT (ipc->server_address_family == AF_UNIX);

	ds_ipc_socket_t new_socket = DS_IPC_INVALID_SOCKET;
	int socket_type = SOCK_STREAM;
#ifdef SOCK_CLOEXEC
	socket_type |= SOCK_CLOEXEC;
#endif // SOCK_CLOEXEC
	DS_ENTER_BLOCKING_PAL_SECTION;
	new_socket = socket (ipc->server_address_family, socket_type, 0);
#ifndef SOCK_CLOEXEC
	if (new_socket != DS_IPC_INVALID_SOCKET)
		fcntl (new_socket, F_SETFD, FD_CLOEXEC); // ignore any failures; this is best effort
#endif // SOCK_CLOEXEC
	DS_EXIT_BLOCKING_PAL_SECTION;
	return new_socket;
#endif // DS_IPC_PAL_AF_UNIX
	return DS_IPC_INVALID_SOCKET;
}

static
inline
ds_ipc_socket_t
ipc_socket_create_tcp (DiagnosticsIpc *ipc)
{
#if defined(DS_IPC_PAL_AF_INET) || defined(DS_IPC_PAL_AF_INET6)
#if defined(DS_IPC_PAL_AF_INET) && defined(DS_IPC_PAL_AF_INET6)
	EP_ASSERT (ipc->server_address_family == AF_INET || ipc->server_address_family == AF_INET6);
#else
	EP_ASSERT (ipc->server_address_family == AF_INET);
#endif

	ds_ipc_socket_t new_socket = DS_IPC_INVALID_SOCKET;
	int socket_type = SOCK_STREAM;
#ifdef SOCK_CLOEXEC
	socket_type |= SOCK_CLOEXEC;
#endif // SOCK_CLOEXEC
	DS_ENTER_BLOCKING_PAL_SECTION;
	new_socket = socket (ipc->server_address_family, socket_type, IPPROTO_TCP);
	if (new_socket != DS_IPC_INVALID_SOCKET) {
#ifndef SOCK_CLOEXEC
#ifndef HOST_WIN32
		fcntl (new_socket, F_SETFD, FD_CLOEXEC); // ignore any failures; this is best effort
#endif // HOST_WIN32
#endif // SOCK_CLOEXEC
		int option_value = 1;
		setsockopt (new_socket, IPPROTO_TCP, TCP_NODELAY, (const char*)&option_value, sizeof (option_value));
		if (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN) {
			setsockopt (new_socket, SOL_SOCKET, SO_REUSEADDR, (const char*)&option_value, sizeof (option_value));
#ifdef DS_IPC_PAL_AF_INET6
			if (ipc->is_dual_mode) {
				option_value = 0;
				setsockopt (new_socket, IPPROTO_IPV6, IPV6_V6ONLY, (const char*)&option_value, sizeof (option_value));
			}
		}
#endif
	}
	DS_EXIT_BLOCKING_PAL_SECTION;
	return new_socket;
#endif
	return DS_IPC_INVALID_SOCKET;
}

static
inline
ds_ipc_socket_t
ipc_socket_create (DiagnosticsIpc *ipc)
{
#if defined(DS_IPC_PAL_AF_UNIX)
	return ipc_socket_create_uds (ipc);
#elif defined(DS_IPC_PAL_AF_INET) || defined(DS_IPC_PAL_AF_INET6)
	return ipc_socket_create_tcp (ipc);
#else
	#error "Unknown address family"
#endif
}

static
inline
int
ipc_socket_close (ds_ipc_socket_t s)
{
	int result_close;
	DS_ENTER_BLOCKING_PAL_SECTION;
#ifdef HOST_WIN32
	result_close = closesocket (s);
#else
	do {
		result_close = close (s);
	} while (ipc_retry_syscall (result_close));
#endif
	DS_EXIT_BLOCKING_PAL_SECTION;
	return result_close;
}

static
inline
int
ipc_socket_set_permission (ds_ipc_socket_t s)
{
#if defined(DS_IPC_PAL_AF_UNIX) && !(defined(__APPLE__) || defined(__FreeBSD__))
	int result_fchmod;
	DS_ENTER_BLOCKING_PAL_SECTION;
	do {
		result_fchmod = fchmod (s, S_IRUSR | S_IWUSR);
	} while (ipc_retry_syscall (result_fchmod));
	DS_EXIT_BLOCKING_PAL_SECTION;
	return result_fchmod;
#else
	return 0;
#endif
}

static
inline
int
ipc_socket_set_blocking (
	ds_ipc_socket_t s,
	bool blocking)
{
	int result = DS_IPC_SOCKET_ERROR;
	DS_ENTER_BLOCKING_PAL_SECTION;
#ifdef HOST_WIN32
	u_long blocking_mode = blocking ? 0 : 1;
	result = ioctlsocket (s, FIONBIO, &blocking_mode);
#else
	result = fcntl (s, F_GETFL, 0);
	if (result != -1)
		result = fcntl (s, F_SETFL, blocking ? (result & (~O_NONBLOCK)) : (result | (O_NONBLOCK)));
#endif
	DS_EXIT_BLOCKING_PAL_SECTION;
	return result;
}

static
inline
int
ipc_poll_fds (
	ds_ipc_pollfd_t *fds,
	size_t nfds,
	uint32_t timeout)
{
	int result_poll;
	DS_ENTER_BLOCKING_PAL_SECTION;
#ifdef HOST_WIN32
	result_poll = WSAPoll (fds, (ULONG)nfds, (INT)timeout);
#else
#ifndef EP_NO_RT_DEPENDENCY
	int64_t start = 0;
	int64_t stop = 0;
	bool retry_poll = false;
	do {
		if (timeout != EP_INFINITE_WAIT)
			start = ep_rt_perf_counter_query ();

		result_poll = poll (fds, nfds, (int)timeout);
		retry_poll = ipc_retry_syscall (result_poll);

		if (retry_poll && timeout != EP_INFINITE_WAIT) {
			stop = ep_rt_perf_counter_query ();
			uint32_t waited_ms = (uint32_t)(((stop - start) * 1000) / ep_rt_perf_frequency_query ());
			timeout = (waited_ms < timeout) ? timeout - waited_ms : 0;
		}

		if (retry_poll && timeout == 0)
			result_poll = 0; // Return time out.

	} while (retry_poll && timeout != 0);
#else
	do {
		result_poll = poll (fds, nfds, (int)timeout);
	} while (ipc_retry_syscall (result_poll));
#endif
#endif
	DS_EXIT_BLOCKING_PAL_SECTION;
	return result_poll;
}

static
inline
int
ipc_socket_bind (
	ds_ipc_socket_t s,
	const ds_ipc_socket_address_t *address,
	ds_ipc_socket_len_t address_len)
{
	int result_bind;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_bind = bind (s, address, address_len);
	DS_EXIT_BLOCKING_PAL_SECTION;
	return result_bind;
}

static
inline
int
ipc_socket_listen (
	ds_ipc_socket_t s,
	int backlog)
{
	int result_listen;
	DS_ENTER_BLOCKING_PAL_SECTION;
	result_listen = listen (s, backlog);
	DS_EXIT_BLOCKING_PAL_SECTION;
	return result_listen;
}

static
inline
ds_ipc_socket_t
ipc_socket_accept (
	ds_ipc_socket_t s,
	ds_ipc_socket_address_t *address,
	ds_ipc_socket_len_t *address_len)
{
	ds_ipc_socket_t client_socket;
	DS_ENTER_BLOCKING_PAL_SECTION;
	do {
#if HAVE_ACCEPT4 && defined(SOCK_CLOEXEC)
    	client_socket = accept4 (s, address, address_len, SOCK_CLOEXEC);
#else
    	client_socket = accept (s, address, address_len);
#endif
	} while (ipc_retry_syscall (client_socket));

#if !HAVE_ACCEPT4 || !defined(SOCK_CLOEXEC)
#if defined(FD_CLOEXEC)
		if (client_socket != -1)
		{
			// ignore any failures; this is best effort
			fcntl (client_socket, F_SETFD, FD_CLOEXEC);
		}
#endif
#endif
	DS_EXIT_BLOCKING_PAL_SECTION;
	return client_socket;
}

static
int
ipc_socket_connect (
	ds_ipc_socket_t s,
	ds_ipc_socket_address_t *address,
	int address_len,
	uint32_t timeout_ms)
{
	int result_connect;

	// We don't expect this to block on Unix Domain Socket.  `connect` may block until the
	// TCP handshake is complete for TCP/IP sockets. On UDS `connect` will return even if
	// the server hasn't called `accept`, so no need to check for timeout or connect error.

#if defined(DS_IPC_PAL_AF_INET) || defined(DS_IPC_PAL_AF_INET6)
	if (timeout_ms != DS_IPC_TIMEOUT_INFINITE) {
		// Set socket to none blocking.
		ipc_socket_set_blocking (s, false);
	}
#endif

	DS_ENTER_BLOCKING_PAL_SECTION;
	do {
		result_connect = connect (s, address, address_len);
	} while (ipc_retry_syscall (result_connect));
	DS_EXIT_BLOCKING_PAL_SECTION;

#if defined(DS_IPC_PAL_AF_INET) || defined(DS_IPC_PAL_AF_INET6)
	if (timeout_ms != DS_IPC_TIMEOUT_INFINITE) {
		if (result_connect == DS_IPC_SOCKET_ERROR) {
			if (ipc_get_last_error () == DS_IPC_SOCKET_ERROR_WOULDBLOCK) {
				ds_ipc_pollfd_t pfd;
				pfd.fd = s;
				pfd.events = POLLOUT;
				int result_poll = ipc_poll_fds (&pfd, 1, timeout_ms);
				if (result_poll == 0) {
					// timeout
					ipc_set_last_error (DS_IPC_SOCKET_ERROR_TIMEDOUT);
					result_connect = DS_IPC_SOCKET_ERROR;
				} else if (result_poll < 0 || !(pfd.revents & POLLOUT)) {
					// error
					result_connect = DS_IPC_SOCKET_ERROR;
				} else {
					// success, check non-blocking connect result.
					result_connect = ipc_get_last_socket_error (s);
					if (result_connect != 0 && result_connect != DS_IPC_SOCKET_ERROR) {
						ipc_set_last_error (result_connect);
						result_connect = DS_IPC_SOCKET_ERROR;
					}
				}
			}
		}
	}

	if (timeout_ms != DS_IPC_TIMEOUT_INFINITE) {
		// Reset socket to blocking.
		int last_error = ipc_get_last_error ();
		ipc_socket_set_blocking (s, true);
		ipc_set_last_error (last_error);
	}
#endif

	return result_connect;
}

static
bool
ipc_socket_recv (
	ds_ipc_socket_t s,
	uint8_t * buffer,
	ssize_t bytes_to_read,
	ssize_t *bytes_read)
{
	uint8_t *buffer_cursor = (uint8_t*)buffer;
	ssize_t current_bytes_read = 0;
	ssize_t total_bytes_read = 0;
	bool continue_recv = true;

	DS_ENTER_BLOCKING_PAL_SECTION;
	while (continue_recv && bytes_to_read - total_bytes_read > 0) {
		current_bytes_read = recv (
			s,
			buffer_cursor,
			bytes_to_read - total_bytes_read,
			0);
		if (ipc_retry_syscall (current_bytes_read))
			continue;
		continue_recv = current_bytes_read > 0;
		if (!continue_recv)
			break;
		total_bytes_read += current_bytes_read;
		buffer_cursor += current_bytes_read;
	}
	DS_EXIT_BLOCKING_PAL_SECTION;

	*bytes_read = total_bytes_read;
	return continue_recv;
}

static
bool
ipc_socket_send (
	ds_ipc_socket_t s,
	const uint8_t *buffer,
	ssize_t bytes_to_write,
	ssize_t *bytes_written)
{
	uint8_t *buffer_cursor = (uint8_t*)buffer;
	ssize_t current_bytes_written = 0;
	ssize_t total_bytes_written = 0;
	bool continue_send = true;

	DS_ENTER_BLOCKING_PAL_SECTION;
	while (continue_send && bytes_to_write - total_bytes_written > 0) {
		current_bytes_written = send (
			s,
			buffer_cursor,
			bytes_to_write - total_bytes_written,
			0);
		if (ipc_retry_syscall (current_bytes_written))
			continue;
		continue_send = current_bytes_written != DS_IPC_SOCKET_ERROR;
		if (!continue_send)
			break;
		total_bytes_written += current_bytes_written;
		buffer_cursor += current_bytes_written;
	}
	DS_EXIT_BLOCKING_PAL_SECTION;

	*bytes_written = total_bytes_written;
	return continue_send;
}

/*
 * DiagnosticsIpc.
 */

static
inline
bool
ipc_transport_get_default_name (
	ep_char8_t *name,
	int32_t name_len)
{
#ifdef DS_IPC_PAL_AF_UNIX
#ifndef EP_NO_RT_DEPENDENCY
	return ds_rt_transport_get_default_name (
		name,
		name_len,
		"dotnet-diagnostic",
		ep_rt_current_process_get_id (),
		NULL,
		"socket");
#elif defined (EP_NO_RT_DEPENDENCY) && defined (FEATURE_CORECLR)
	// generate the default socket name in TMP Path
	const ProcessDescriptor pd = ProcessDescriptor::FromCurrentProcess();
	PAL_GetTransportName(
		name_len,
		name,
		"dotnet-diagnostic",
		pd.m_Pid,
		pd.m_ApplicationGroupId,
		"socket");
	return true;
#else
	return false;
#endif
#else
	return false;
#endif
}

static
bool
ipc_init_listener (
	DiagnosticsIpc *ipc,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);

	bool success = false;
	ds_ipc_mode_t prev_mode = ipc_socket_set_default_umask ();

	ds_ipc_socket_t server_socket;
	server_socket = ipc_socket_create (ipc);
	if (server_socket == DS_IPC_INVALID_SOCKET) {
		if (callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());

		EP_ASSERT (!"Failed to create diagnostics IPC socket.");
		ep_raise_error ();
	}

	int result_set_permission;
	result_set_permission = ipc_socket_set_permission (server_socket);
	if (result_set_permission == DS_IPC_SOCKET_ERROR) {
		if (callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
		EP_ASSERT (!"Failed to set permissions on diagnostics IPC socket.");
		ep_raise_error ();
	}

	int result_bind;
	result_bind = ipc_socket_bind (server_socket, ipc->server_address, ipc->server_address_len);
	if (result_bind == DS_IPC_SOCKET_ERROR) {
		if (callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());

		int result_close;
		result_close = ipc_socket_close (server_socket);
		if (result_close == DS_IPC_SOCKET_ERROR) {
			if (callback)
				callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
		}

		ep_raise_error ();
	}

	ipc->server_socket = server_socket;
	success = true;

ep_on_exit:
	ipc_socket_reset_umask (prev_mode);
	return success;

ep_on_error:
	success = false;
	ep_exit_error_handler ();
}

static
DiagnosticsIpc *
ipc_alloc_uds_address (
	DiagnosticsIpc *ipc,
	DiagnosticsIpcConnectionMode mode,
	const ep_char8_t *ipc_name)
{
#ifdef DS_IPC_PAL_AF_UNIX
	EP_ASSERT (ipc != NULL);

	struct sockaddr_un *server_address = ep_rt_object_alloc (struct sockaddr_un);
	ep_return_null_if_nok (server_address != NULL);

	server_address->sun_family = AF_UNIX;

	if (ipc_name) {
		int32_t result = snprintf (
			server_address->sun_path,
			sizeof (server_address->sun_path),
			"%s",
			ipc_name);
		if (result <= 0 || result >= (int32_t)(sizeof (server_address->sun_path)))
			server_address->sun_path [0] = '\0';
	} else {
		// generate the default socket name
		ipc_transport_get_default_name (
			server_address->sun_path,
			sizeof (server_address->sun_path));
	}

	ipc->server_address = (ds_ipc_socket_address_t *)server_address;
	ipc->server_address_len = sizeof (struct sockaddr_un);
	ipc->server_address_family = server_address->sun_family;

	return ipc;
#else
	return NULL;
#endif
}

static
DiagnosticsIpc *
ipc_alloc_tcp_address (
	DiagnosticsIpc *ipc,
	DiagnosticsIpcConnectionMode mode,
	const ep_char8_t *ipc_name)
{
#if defined(DS_IPC_PAL_AF_INET) || defined(DS_IPC_PAL_AF_INET6)
	EP_ASSERT (ipc != NULL);
	ep_return_null_if_nok (ipc_name != NULL);

#ifdef DS_IPC_PAL_AF_INET
	struct sockaddr_in *server_address = NULL;
#endif
#ifdef DS_IPC_PAL_AF_INET6
	struct sockaddr_in6 *server_address6 = NULL;
#endif
	ds_ipc_addrinfo_t hints = {0};
	ds_ipc_addrinfo_t *info = NULL;
	ep_char8_t *address = NULL;
	int32_t port = 0;

	address = ep_rt_utf8_string_dup (ipc_name);
	ep_raise_error_if_nok (address != NULL);

	const ep_char8_t *host_address = address;
	const ep_char8_t *host_port = strrchr (address, ':');

	if (host_port && host_port != host_address) {
		size_t host_address_len = host_port - address;
		address [host_address_len] = 0;
		port = atoi (host_port + 1);
	}

	ipc->server_address = NULL;

	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_flags = (mode == DS_IPC_CONNECTION_MODE_LISTEN) ? AI_PASSIVE : 0;

	int result_getaddrinfo = -1;
	DS_ENTER_BLOCKING_PAL_SECTION;
	if (mode == DS_IPC_CONNECTION_MODE_LISTEN && *host_address == '*') {
#ifdef DS_IPC_PAL_AF_INET6
		hints.ai_family = AF_INET6;
		result_getaddrinfo = getaddrinfo ("[::]", NULL, &hints, &info);
		if (!result_getaddrinfo)
			ipc->is_dual_mode = true;
#endif
		if (result_getaddrinfo != 0) {
			hints.ai_family = AF_INET;
			result_getaddrinfo = getaddrinfo ("0.0.0.0", NULL, &hints, &info);
		}
	} else {
		result_getaddrinfo = getaddrinfo (host_address, NULL, &hints, &info);
	}
	DS_EXIT_BLOCKING_PAL_SECTION;

	if (!result_getaddrinfo && info) {
#ifdef DS_IPC_PAL_AF_INET
		if (!ipc->server_address && info->ai_family == AF_INET) {
			server_address = ep_rt_object_alloc (struct sockaddr_in);
			if (server_address) {
				server_address->sin_family = (uint8_t) info->ai_family;
				server_address->sin_port = htons (port);
				server_address->sin_addr = ((struct sockaddr_in*)info->ai_addr)->sin_addr;
				ipc->server_address = (ds_ipc_socket_address_t *)server_address;
				ipc->server_address_len = sizeof (struct sockaddr_in);
				ipc->server_address_family = server_address->sin_family;
				server_address = NULL;
			}
		}
#endif
#ifdef DS_IPC_PAL_AF_INET6
		if (!ipc->server_address && info->ai_family == AF_INET6) {
			server_address6 = ep_rt_object_alloc (struct sockaddr_in6);
			if (server_address6) {
				server_address6->sin6_family = (uint8_t) info->ai_family;
				server_address6->sin6_port = htons (port);
				server_address6->sin6_addr = ((struct sockaddr_in6*)info->ai_addr)->sin6_addr;
				ipc->server_address = (ds_ipc_socket_address_t *)server_address6;
				ipc->server_address_len = sizeof (struct sockaddr_in6);
				ipc->server_address_family = server_address6->sin6_family;
				server_address6 = NULL;
			}
		}
#endif
	}

	ep_raise_error_if_nok (ipc->server_address != NULL);

ep_on_exit:
	if (info)
		freeaddrinfo (info);
	ep_rt_utf8_string_free (address);
	return ipc;

ep_on_error:
#ifdef DS_IPC_PAL_AF_INET
	ep_rt_object_free (server_address);
#endif
#ifdef DS_IPC_PAL_AF_INET6
	ep_rt_object_free (server_address6);
#endif
	ipc = NULL;
	ep_exit_error_handler ();
#else
	return NULL;
#endif
}

static
inline
DiagnosticsIpc *
ipc_alloc_address (
	DiagnosticsIpc *ipc,
	DiagnosticsIpcConnectionMode mode,
	const ep_char8_t *ipc_name)
{
#ifdef DS_IPC_PAL_AF_UNIX
	return ipc_alloc_uds_address (ipc, mode, ipc_name);
#elif defined(DS_IPC_PAL_AF_INET) || defined(DS_IPC_PAL_AF_INET6)
	return ipc_alloc_tcp_address (ipc, mode, ipc_name);
#else
	#error "Unknown address family"
#endif
}

static
inline
void
ipc_free_uds_address (DiagnosticsIpc *ipc)
{
#ifdef DS_IPC_PAL_AF_UNIX
	if (ipc->server_address_family == AF_UNIX) {
		ep_rt_object_free ((struct sockaddr_un *)ipc->server_address);
	}
#endif
}

static
inline
void
ipc_free_tcp_address (DiagnosticsIpc *ipc)
{
#ifdef DS_IPC_PAL_AF_INET
	if (ipc->server_address_family == AF_INET) {
		ep_rt_object_free ((struct sockaddr_in *)ipc->server_address);
	}
#endif
#ifdef DS_IPC_PAL_AF_INET6
	if (ipc->server_address_family == AF_INET6) {
		ep_rt_object_free ((struct sockaddr_in6 *)ipc->server_address);
	}
#endif
}

static
inline
void
ipc_free_address (DiagnosticsIpc *ipc)
{
	EP_ASSERT (ipc != NULL);
#ifdef DS_IPC_PAL_AF_UNIX
	ipc_free_uds_address (ipc);
#elif defined(DS_IPC_PAL_AF_INET) || defined(DS_IPC_PAL_AF_INET6)
	ipc_free_tcp_address (ipc);
#else
	#error "Unknown address family"
#endif
}

bool
ds_ipc_pal_init (void)
{
#ifdef HOST_WIN32
	if (!_ipc_pal_socket_init) {
		WSADATA wsaData;
		if (!WSAStartup(MAKEWORD(2, 2), &wsaData))
			_ipc_pal_socket_init = true;
	}
#else
	_ipc_pal_socket_init = true;
#endif
	return _ipc_pal_socket_init;
}

bool
ds_ipc_pal_shutdown (void)
{
#ifdef HOST_WIN32
	if (_ipc_pal_socket_init)
		WSACleanup ();
#endif
	_ipc_pal_socket_init = false;
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

	instance->mode = mode;
	instance->server_socket = DS_IPC_INVALID_SOCKET;
	instance->is_closed = false;
	instance->is_listening = false;

	ep_raise_error_if_nok (ipc_alloc_address (instance, mode, ipc_name) != NULL);

	if (mode == DS_IPC_CONNECTION_MODE_LISTEN)
		ep_raise_error_if_nok (ipc_init_listener (instance, callback) == true);

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
	ipc_free_address (ipc);
	ep_rt_object_free (ipc);
}

int32_t
ds_ipc_poll (
	DiagnosticsIpcPollHandle *poll_handles_data,
	size_t poll_handles_data_len,
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (poll_handles_data != NULL);

	int32_t result = DS_IPC_SOCKET_ERROR;

	// prepare the pollfd structs
	ds_ipc_pollfd_t *poll_fds = ep_rt_object_array_alloc (ds_ipc_pollfd_t, poll_handles_data_len);
	ep_raise_error_if_nok (poll_fds != NULL);

	for (uint32_t i = 0; i < poll_handles_data_len; ++i) {
		poll_handles_data [i].events = 0; // ignore any input on events.
		ds_ipc_socket_t fd = DS_IPC_INVALID_SOCKET;
		if (poll_handles_data [i].ipc) {
			// SERVER
			EP_ASSERT (poll_handles_data [i].ipc->mode == DS_IPC_CONNECTION_MODE_LISTEN);
			fd = poll_handles_data [i].ipc->server_socket;
		} else {
			// CLIENT
			EP_ASSERT (poll_handles_data [i].stream != NULL);
			fd = poll_handles_data [i].stream->client_socket;
		}

		poll_fds [i].fd = fd;
		poll_fds [i].events = POLLIN;
	}

	int result_poll;
	result_poll = ipc_poll_fds (poll_fds, poll_handles_data_len, timeout_ms);

	// Check results
	if (result_poll < 0) {
		// If poll() returns with an error, including one due to an interrupted call, the fds
		// array will be unmodified and last error will be set to indicate the error.
		// - POLL(2)
		if (callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
		ep_raise_error ();
	} else if (result_poll == 0) {
		// we timed out
		result = 0;
		ep_raise_error ();
	}

	for (uint32_t i = 0; i < poll_handles_data_len; ++i) {
		if (poll_fds [i].revents != 0) {
			// error check FIRST
			if (poll_fds [i].revents & POLLHUP) {
				// check for hangup first because a closed socket
				// will technically meet the requirements for POLLIN
				// i.e., a call to recv/read won't block
				poll_handles_data [i].events = (uint8_t)DS_IPC_POLL_EVENTS_HANGUP;
			} else if ((poll_fds [i].revents & (POLLERR|POLLNVAL))) {
				if (callback)
					callback ("Poll error", (uint32_t)poll_fds [i].revents);
				poll_handles_data [i].events = (uint8_t)DS_IPC_POLL_EVENTS_ERR;
			} else if (poll_fds [i].revents & (POLLIN|POLLPRI)) {
				poll_handles_data [i].events = (uint8_t)DS_IPC_POLL_EVENTS_SIGNALED;
			} else {
				poll_handles_data [i].events = (uint8_t)DS_IPC_POLL_EVENTS_UNKNOWN;
				if (callback)
					callback ("unknown poll response", (uint32_t)poll_fds [i].revents);
			}
		}
	}

	result = 1;

ep_on_exit:
	ep_rt_object_array_free (poll_fds);
	return result;

ep_on_error:
	if (result != 0)
		result = DS_IPC_SOCKET_ERROR;

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
			callback ("Cannot call Listen on a client connection", DS_IPC_SOCKET_ERROR);
		return false;
	}

	if (ipc->is_listening)
		return true;

	EP_ASSERT (ipc->server_socket != DS_IPC_INVALID_SOCKET);

	int result_listen;
	result_listen = ipc_socket_listen (ipc->server_socket, /* backlog */ 255);
	if (result_listen == DS_IPC_SOCKET_ERROR) {
		if (callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());

#ifdef DS_IPC_PAL_AF_UNIX
		int result_unlink;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_unlink = unlink (((struct sockaddr_un *)ipc->server_address)->sun_path);
		DS_EXIT_BLOCKING_PAL_SECTION;

		EP_ASSERT (result_unlink != -1);
		if (result_unlink == -1) {
			if (callback)
				callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
		}
#endif

		int result_close;
		result_close = ipc_socket_close (ipc->server_socket);
		if (result_close == DS_IPC_SOCKET_ERROR) {
			if (callback)
				callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
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

	ds_ipc_socket_t client_socket;
	client_socket = ipc_socket_accept (ipc->server_socket, NULL, NULL);
	if (client_socket == DS_IPC_SOCKET_ERROR) {
		if (callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
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
	uint32_t timeout_ms,
	ds_ipc_error_callback_func callback,
	bool *timed_out)
{
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (timed_out != NULL);

	DiagnosticsIpcStream *stream = NULL;

	EP_ASSERT (ipc->mode == DS_IPC_CONNECTION_MODE_CONNECT);

	ds_ipc_socket_t client_socket;
	client_socket = ipc_socket_create (ipc);
	if (client_socket == DS_IPC_SOCKET_ERROR) {
		if (callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
		ep_raise_error ();
	}

	int result_connect;
	result_connect = ipc_socket_connect (client_socket, ipc->server_address, ipc->server_address_len, timeout_ms);
	if (result_connect < 0) {
		if (callback && ipc_get_last_error () != DS_IPC_SOCKET_ERROR_TIMEDOUT)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
		else if (ipc_get_last_error () == DS_IPC_SOCKET_ERROR_TIMEDOUT)
			*timed_out = true;

		int result_close;
		result_close = ipc_socket_close (client_socket);
		if (result_close < 0 && callback)
			callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());

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
	if (ipc->is_closed)
		return;

	ipc->is_closed = true;

	if (ipc->server_socket != DS_IPC_INVALID_SOCKET) {
		// only close the socket if not shutting down, let the OS handle it in that case
		if (!is_shutdown) {
			int close_result = ipc_socket_close (ipc->server_socket);
			if (close_result == DS_IPC_SOCKET_ERROR) {
				if (callback)
					callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
				EP_ASSERT (!"Failed to close socket.");
			}
		}

#ifdef DS_IPC_PAL_AF_UNIX
		// N.B. - it is safe to unlink the unix domain socket file while the server
		// is still alive:
		// "The usual UNIX close-behind semantics apply; the socket can be unlinked
		// at any time and will be finally removed from the file system when the last
		// reference to it is closed." - unix(7) man page
		int result_unlink;
		DS_ENTER_BLOCKING_PAL_SECTION;
		result_unlink = unlink (((struct sockaddr_un *)ipc->server_address)->sun_path);
		DS_EXIT_BLOCKING_PAL_SECTION;

		if (result_unlink == -1) {
			if (callback)
				callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
			EP_ASSERT (!"Failed to unlink server address.");
		}
#endif
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

	int32_t result = snprintf (buffer, buffer_len, "{ server_socket = %d }", (int32_t)(size_t)ipc->server_socket);
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
	ssize_t total_bytes_read = 0;

	if (timeout_ms != DS_IPC_TIMEOUT_INFINITE) {
		ds_ipc_pollfd_t pfd;
		pfd.fd = ipc_stream->client_socket;
		pfd.events = POLLIN;

		int result_poll;
		result_poll = ipc_poll_fds (&pfd, 1, timeout_ms);
		if (result_poll <= 0 || !(pfd.revents & POLLIN)) {
			// timeout or error
			ep_raise_error ();
		}
		// else fallthrough
	}

	success = ipc_socket_recv (ipc_stream->client_socket, buffer, bytes_to_read, &total_bytes_read);
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
	ssize_t total_bytes_written = 0;

	if (timeout_ms != DS_IPC_TIMEOUT_INFINITE) {
		ds_ipc_pollfd_t pfd;
		pfd.fd = ipc_stream->client_socket;
		pfd.events = POLLOUT;

		int result_poll;
		result_poll = ipc_poll_fds (&pfd, 1, timeout_ms);
		if (result_poll <= 0 || !(pfd.revents & POLLOUT)) {
			// timeout or error
			ep_raise_error ();
		}
		// else fallthrough
	}

	success = ipc_socket_send (ipc_stream->client_socket, buffer, bytes_to_write, &total_bytes_written);
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

	instance->stream.vtable = &ipc_stream_vtable;
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

		int result_close = ipc_socket_close (ipc_stream->client_socket);
		if (result_close == DS_IPC_SOCKET_ERROR) {
			if (callback)
				callback (strerror (ipc_get_last_error ()), ipc_get_last_error ());
		}

		ipc_stream->client_socket = DS_IPC_INVALID_SOCKET;
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
