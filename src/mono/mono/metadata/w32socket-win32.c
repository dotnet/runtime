/**
 * \file
 * Windows specific socket code.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include <string.h>
#include <stdlib.h>
#include <ws2tcpip.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>

#include <sys/types.h>

#include "w32socket.h"
#include "w32socket-internals.h"

#include "utils/w32api.h"
#include "utils/mono-os-wait.h"

#define LOGDEBUG(...)  

void
mono_w32socket_initialize (void)
{
}

void
mono_w32socket_cleanup (void)
{
}

static gboolean set_blocking (SOCKET sock, gboolean block)
{
	u_long non_block = block ? 0 : 1;
	return ioctlsocket (sock, FIONBIO, &non_block) != SOCKET_ERROR;
}

static DWORD get_socket_timeout (SOCKET sock, int optname)
{
	DWORD timeout = 0;
	int optlen = sizeof (DWORD);
	if (getsockopt (sock, SOL_SOCKET, optname, (char *)&timeout, &optlen) == SOCKET_ERROR) {
		WSASetLastError (0);
		return WSA_INFINITE;
	}
	if (timeout == 0)
		timeout = WSA_INFINITE; // 0 means infinite
	return timeout;
}

/*
* Performs an alertable wait for the specified event (FD_ACCEPT_BIT,
* FD_CONNECT_BIT, FD_READ_BIT, FD_WRITE_BIT) on the specified socket.
* Returns TRUE if the event is fired without errors. Calls WSASetLastError()
* with WSAEINTR and returns FALSE if the thread is alerted. If the event is
* fired but with an error WSASetLastError() is called to set the error and the
* function returns FALSE.
*/
static gboolean alertable_socket_wait (SOCKET sock, int event_bit)
{
	static char *EVENT_NAMES[] = { "FD_READ", "FD_WRITE", NULL /*FD_OOB*/, "FD_ACCEPT", "FD_CONNECT", "FD_CLOSE" };
	gboolean success = FALSE;
	int error = -1;
	DWORD timeout = WSA_INFINITE;
	if (event_bit == FD_READ_BIT || event_bit == FD_WRITE_BIT) {
		timeout = get_socket_timeout (sock, event_bit == FD_READ_BIT ? SO_RCVTIMEO : SO_SNDTIMEO);
	}
	WSASetLastError (0);
	WSAEVENT event = WSACreateEvent ();
	if (event != WSA_INVALID_EVENT) {
		if (WSAEventSelect (sock, event, (1 << event_bit) | FD_CLOSE) != SOCKET_ERROR) {
			LOGDEBUG (g_message ("%06d - Calling mono_win32_wsa_wait_for_multiple_events () on socket %d", GetCurrentThreadId (), sock));
			DWORD ret = mono_win32_wsa_wait_for_multiple_events (1, &event, TRUE, timeout, TRUE);
			if (ret == WSA_WAIT_IO_COMPLETION) {
				LOGDEBUG (g_message ("%06d - mono_win32_wsa_wait_for_multiple_events () returned WSA_WAIT_IO_COMPLETION for socket %d", GetCurrentThreadId (), sock));
				error = WSAEINTR;
			} else if (ret == WSA_WAIT_TIMEOUT) {
				error = WSAETIMEDOUT;
			} else {
				g_assert (ret == WSA_WAIT_EVENT_0);
				WSANETWORKEVENTS ne = { 0 };
				if (WSAEnumNetworkEvents (sock, event, &ne) != SOCKET_ERROR) {
					if (ne.lNetworkEvents & (1 << event_bit) && ne.iErrorCode[event_bit]) {
						LOGDEBUG (g_message ("%06d - %s error %d on socket %d", GetCurrentThreadId (), EVENT_NAMES[event_bit], ne.iErrorCode[event_bit], sock));
						error = ne.iErrorCode[event_bit];
					} else if (ne.lNetworkEvents & FD_CLOSE_BIT && ne.iErrorCode[FD_CLOSE_BIT]) {
						LOGDEBUG (g_message ("%06d - FD_CLOSE error %d on socket %d", GetCurrentThreadId (), ne.iErrorCode[FD_CLOSE_BIT], sock));
						error = ne.iErrorCode[FD_CLOSE_BIT];
					} else {
						LOGDEBUG (g_message ("%06d - WSAEnumNetworkEvents () finished successfully on socket %d", GetCurrentThreadId (), sock));
						success = TRUE;
						error = 0;
					}
				}
			}
			WSAEventSelect (sock, NULL, 0);
		}
		WSACloseEvent (event);
	}
	if (error != -1) {
		WSASetLastError (error);
	}
	return success;
}

#define ALERTABLE_SOCKET_CALL(event_bit, blocking, repeat, ret, op, sock, ...) \
	LOGDEBUG (g_message ("%06d - Performing %s " #op " () on socket %d", GetCurrentThreadId (), blocking ? "blocking" : "non-blocking", sock)); \
	if (blocking) { \
		if (set_blocking(sock, FALSE)) { \
			while (-1 == (int) (ret = op (sock, __VA_ARGS__))) { \
				int _error = WSAGetLastError ();\
				if (_error != WSAEWOULDBLOCK && _error != WSA_IO_PENDING) \
					break; \
				if (!alertable_socket_wait (sock, event_bit) || !repeat) \
					break; \
			} \
			int _saved_error = WSAGetLastError (); \
			set_blocking (sock, TRUE); \
			WSASetLastError (_saved_error); \
		} \
	} else { \
		ret = op (sock, __VA_ARGS__); \
	} \
	int _saved_error = WSAGetLastError (); \
	LOGDEBUG (g_message ("%06d - Finished %s " #op " () on socket %d (ret = %d, WSAGetLastError() = %d)", GetCurrentThreadId (), \
		blocking ? "blocking" : "non-blocking", sock, ret, _saved_error)); \
	WSASetLastError (_saved_error);

SOCKET mono_w32socket_accept (SOCKET s, struct sockaddr *addr, socklen_t *addrlen, gboolean blocking)
{
	MonoInternalThread *curthread = mono_thread_internal_current ();
	SOCKET newsock = INVALID_SOCKET;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_ACCEPT_BIT, blocking, TRUE, newsock, accept, s, addr, addrlen);
	MONO_EXIT_GC_SAFE;
	return newsock;
}

int mono_w32socket_connect (SOCKET s, const struct sockaddr *name, int namelen, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_CONNECT_BIT, blocking, FALSE, ret, connect, s, name, namelen);
	ret = WSAGetLastError () != 0 ? SOCKET_ERROR : 0;
	MONO_EXIT_GC_SAFE;
	return ret;
}

int mono_w32socket_recv (SOCKET s, char *buf, int len, int flags, gboolean blocking)
{
	MonoInternalThread *curthread = mono_thread_internal_current ();
	int ret = SOCKET_ERROR;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_READ_BIT, blocking, TRUE, ret, recv, s, buf, len, flags);
	MONO_EXIT_GC_SAFE;
	return ret;
}

int mono_w32socket_recvfrom (SOCKET s, char *buf, int len, int flags, struct sockaddr *from, socklen_t *fromlen, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_READ_BIT, blocking, TRUE, ret, recvfrom, s, buf, len, flags, from, fromlen);
	MONO_EXIT_GC_SAFE;
	return ret;
}

int mono_w32socket_recvbuffers (SOCKET s, WSABUF *lpBuffers, guint32 dwBufferCount, guint32 *lpNumberOfBytesRecvd, guint32 *lpFlags, gpointer lpOverlapped, gpointer lpCompletionRoutine, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_READ_BIT, blocking, TRUE, ret, WSARecv, s, lpBuffers, dwBufferCount, lpNumberOfBytesRecvd, lpFlags, lpOverlapped, lpCompletionRoutine);
	MONO_EXIT_GC_SAFE;
	return ret;
}

int mono_w32socket_send (SOCKET s, char *buf, int len, int flags, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_WRITE_BIT, blocking, FALSE, ret, send, s, buf, len, flags);
	MONO_EXIT_GC_SAFE;
	return ret;
}

int mono_w32socket_sendto (SOCKET s, const char *buf, int len, int flags, const struct sockaddr *to, int tolen, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_WRITE_BIT, blocking, FALSE, ret, sendto, s, buf, len, flags, to, tolen);
	MONO_EXIT_GC_SAFE;
	return ret;
}

int mono_w32socket_sendbuffers (SOCKET s, WSABUF *lpBuffers, guint32 dwBufferCount, guint32 *lpNumberOfBytesRecvd, guint32 lpFlags, gpointer lpOverlapped, gpointer lpCompletionRoutine, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	MONO_ENTER_GC_SAFE;
	ALERTABLE_SOCKET_CALL (FD_WRITE_BIT, blocking, FALSE, ret, WSASend, s, lpBuffers, dwBufferCount, lpNumberOfBytesRecvd, lpFlags, lpOverlapped, lpCompletionRoutine);
	MONO_EXIT_GC_SAFE;
	return ret;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)
BOOL mono_w32socket_transmit_file (SOCKET hSocket, gpointer hFile, TRANSMIT_FILE_BUFFERS *lpTransmitBuffers, guint32 dwReserved, gboolean blocking)
{
	LOGDEBUG (g_message ("%06d - Performing %s TransmitFile () on socket %d", GetCurrentThreadId (), blocking ? "blocking" : "non-blocking", hSocket));

	int error = 0, ret;

	MONO_ENTER_GC_SAFE;

	if (blocking) {
		OVERLAPPED overlapped = { 0 };
		overlapped.hEvent = WSACreateEvent ();
		if (overlapped.hEvent == WSA_INVALID_EVENT) {
			ret = FALSE;
			goto done;
		}
		if (!TransmitFile (hSocket, hFile, 0, 0, &overlapped, lpTransmitBuffers, dwReserved)) {
			error = WSAGetLastError ();
			if (error == WSA_IO_PENDING) {
				error = 0;
				// NOTE: .NET's Socket.SendFile() doesn't honor the Socket's SendTimeout so we shouldn't either
				DWORD ret = mono_win32_wait_for_single_object_ex (overlapped.hEvent, INFINITE, TRUE);
				if (ret == WAIT_IO_COMPLETION) {
					LOGDEBUG (g_message ("%06d - mono_win32_wait_for_single_object_ex () returned WSA_WAIT_IO_COMPLETION for socket %d", GetCurrentThreadId (), hSocket));
					error = WSAEINTR;
				} else if (ret == WAIT_TIMEOUT) {
					error = WSAETIMEDOUT;
				} else if (ret != WAIT_OBJECT_0) {
					error = GetLastError ();
				}
			}
		}
		WSACloseEvent (overlapped.hEvent);
	} else {
		if (!TransmitFile (hSocket, hFile, 0, 0, NULL, lpTransmitBuffers, dwReserved)) {
			error = WSAGetLastError ();
		}
	}

	LOGDEBUG (g_message ("%06d - Finished %s TransmitFile () on socket %d (ret = %d, WSAGetLastError() = %d)", GetCurrentThreadId (), \
		blocking ? "blocking" : "non-blocking", hSocket, error == 0, error));
	WSASetLastError (error);

	ret = error == 0;

done:
	MONO_EXIT_GC_SAFE;
	return ret;
}
#endif /* #if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)
gint
mono_w32socket_disconnect (SOCKET sock, gboolean reuse)
{
	LPFN_DISCONNECTEX disconnect;
	LPFN_TRANSMITFILE transmit_file;
	DWORD output_bytes;
	gint ret;

	MONO_ENTER_GC_SAFE;

	/* Use the SIO_GET_EXTENSION_FUNCTION_POINTER to determine
	 * the address of the disconnect method without taking
	 * a hard dependency on a single provider
	 *
	 * For an explanation of why this is done, you can read the
	 * article at http://www.codeproject.com/internet/jbsocketserver3.asp
	 *
	 * I _think_ the extension function pointers need to be looked
	 * up for each socket.
	 *
	 * FIXME: check the best way to store pointers to functions in
	 * managed objects that still works on 64bit platforms. */

	GUID disconnect_guid = WSAID_DISCONNECTEX;
	ret = WSAIoctl (sock, SIO_GET_EXTENSION_FUNCTION_POINTER, &disconnect_guid, sizeof (GUID), &disconnect, sizeof (LPFN_DISCONNECTEX), &output_bytes, NULL, NULL);
	if (ret == 0) {
		if (!disconnect (sock, NULL, reuse ? TF_REUSE_SOCKET : 0, 0)) {
			ret = WSAGetLastError ();
			goto done;
		}

		ret = 0;
		goto done;
	}

	GUID transmit_file_guid = WSAID_TRANSMITFILE;
	ret = WSAIoctl (sock, SIO_GET_EXTENSION_FUNCTION_POINTER, &transmit_file_guid, sizeof (GUID), &transmit_file, sizeof (LPFN_TRANSMITFILE), &output_bytes, NULL, NULL);
	if (ret == 0) {
		if (!transmit_file (sock, NULL, 0, 0, NULL, NULL, TF_DISCONNECT | (reuse ? TF_REUSE_SOCKET : 0))) {
			ret = WSAGetLastError ();
			goto done;
		}

		ret = 0;
		goto done;
	}

	ret = ERROR_NOT_SUPPORTED;

done:
	MONO_EXIT_GC_SAFE;
	return ret;
}
#endif /* #if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

gint
mono_w32socket_set_blocking (SOCKET sock, gboolean blocking)
{
	gint ret;
	gulong nonblocking_long = !blocking;
	MONO_ENTER_GC_SAFE;
	ret = ioctlsocket (sock, FIONBIO, &nonblocking_long);
	MONO_EXIT_GC_SAFE;
	return ret;
}

gint
mono_w32socket_get_available (SOCKET sock, guint64 *amount)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = ioctlsocket (sock, FIONREAD, (int*) amount);
	MONO_EXIT_GC_SAFE;
	return ret;
}

void
mono_w32socket_set_last_error (gint32 error)
{
	WSASetLastError (error);
}

gint32
mono_w32socket_get_last_error (void)
{
	return WSAGetLastError ();
}

gint32
mono_w32socket_convert_error (gint error)
{
	return (error > 0 && error < WSABASEERR) ? error + WSABASEERR : error;
}

gboolean
ves_icall_System_Net_Sockets_Socket_SupportPortReuse (MonoProtocolType proto, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_w32socket_duplicate (gpointer handle, gint32 targetProcessId, gpointer *duplicate_handle)
{
	gboolean ret;

	MONO_ENTER_GC_SAFE;
	ret = DuplicateHandle (GetCurrentProcess(), handle, GINT_TO_POINTER(targetProcessId), duplicate_handle, 0, 0, 0x00000002 /* DUPLICATE_SAME_ACCESS */);
	MONO_EXIT_GC_SAFE;

	return ret;
}
