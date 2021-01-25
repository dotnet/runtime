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
#include "icall-decl.h"

#define LOGDEBUG(...)  

void
mono_w32socket_initialize (void)
{
}

void
mono_w32socket_cleanup (void)
{
}

// See win32_wait_interrupt_handler for details.
static void
win32_io_interrupt_handler(gpointer ignored)
{
}

#define INTERRUPTABLE_SOCKET_CALL(blocking, ret, op, sock, ...) \
	MonoThreadInfo *info = mono_thread_info_current (); \
	gboolean alerted = FALSE; \
	if (blocking && info) { \
		mono_thread_info_install_interrupt (win32_io_interrupt_handler, NULL, &alerted); \
		if (alerted) { \
			WSASetLastError (WSAEINTR); \
		} else { \
			mono_win32_enter_blocking_io_call (info, (HANDLE)sock); \
		} \
	} \
	if (!alerted) { \
		MONO_ENTER_GC_SAFE; \
		if (blocking && info && mono_thread_info_is_interrupt_state (info)) { \
			WSASetLastError (WSAEINTR); \
		} else { \
			ret = op (sock, __VA_ARGS__); \
		} \
		MONO_EXIT_GC_SAFE; \
	} \
	if (blocking && info && !alerted) { \
		mono_win32_leave_blocking_io_call (info, (HANDLE)sock); \
		mono_thread_info_uninstall_interrupt (&alerted); \
	}

SOCKET mono_w32socket_accept (SOCKET s, struct sockaddr *addr, socklen_t *addrlen, gboolean blocking)
{
	SOCKET ret = INVALID_SOCKET;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, accept, s, addr, addrlen);
	return ret;
}

int mono_w32socket_connect (SOCKET s, const struct sockaddr *name, int namelen, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, connect, s, name, namelen);
	return ret;
}

int mono_w32socket_recv (SOCKET s, char *buf, int len, int flags, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, recv, s, buf, len, flags);
	return ret;
}

int mono_w32socket_recvfrom (SOCKET s, char *buf, int len, int flags, struct sockaddr *from, socklen_t *fromlen, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, recvfrom, s, buf, len, flags, from, fromlen);
	return ret;
}

int mono_w32socket_recvbuffers (SOCKET s, WSABUF *lpBuffers, guint32 dwBufferCount, guint32 *lpNumberOfBytesRecvd, guint32 *lpFlags, gpointer lpOverlapped, gpointer lpCompletionRoutine, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, WSARecv, s, lpBuffers, dwBufferCount, (LPDWORD)lpNumberOfBytesRecvd, (LPDWORD)lpFlags, (LPWSAOVERLAPPED)lpOverlapped, (LPWSAOVERLAPPED_COMPLETION_ROUTINE)lpCompletionRoutine);
	return ret;
}

int mono_w32socket_send (SOCKET s, void *buf, int len, int flags, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, send, s, (const char *)buf, len, flags);
	return ret;
}

int mono_w32socket_sendto (SOCKET s, const char *buf, int len, int flags, const struct sockaddr *to, int tolen, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, sendto, s, buf, len, flags, to, tolen);
	return ret;
}

int mono_w32socket_sendbuffers (SOCKET s, WSABUF *lpBuffers, guint32 dwBufferCount, guint32 *lpNumberOfBytesRecvd, guint32 lpFlags, gpointer lpOverlapped, gpointer lpCompletionRoutine, gboolean blocking)
{
	int ret = SOCKET_ERROR;
	INTERRUPTABLE_SOCKET_CALL (blocking, ret, WSASend, s, lpBuffers, dwBufferCount, (LPDWORD)lpNumberOfBytesRecvd, lpFlags, (LPWSAOVERLAPPED)lpOverlapped, (LPWSAOVERLAPPED_COMPLETION_ROUTINE)lpCompletionRoutine);
	return ret;
}

#if HAVE_API_SUPPORT_WIN32_TRANSMIT_FILE
static gint
internal_w32socket_transmit_file (SOCKET sock, gpointer file, TRANSMIT_FILE_BUFFERS *lpTransmitBuffers, guint32 dwReserved, gboolean blocking)
{
	gint ret = ERROR_NOT_SUPPORTED;
	LPFN_TRANSMITFILE transmit_file;
	GUID transmit_file_guid = WSAID_TRANSMITFILE;
	DWORD output_bytes;

	if (!WSAIoctl (sock, SIO_GET_EXTENSION_FUNCTION_POINTER, &transmit_file_guid, sizeof (GUID), &transmit_file, sizeof (LPFN_TRANSMITFILE), &output_bytes, NULL, NULL)) {
		MonoThreadInfo *info = mono_thread_info_current ();
		gboolean alerted = FALSE;

		if (blocking && info) {
			mono_thread_info_install_interrupt (win32_io_interrupt_handler, NULL, &alerted);
			if (alerted) {
				WSASetLastError (WSAEINTR);
			} else {
				mono_win32_enter_blocking_io_call (info, (HANDLE)sock);
			}
		}

		if (!alerted) {
			MONO_ENTER_GC_SAFE;
			if (blocking && info && mono_thread_info_is_interrupt_state (info)) {
				WSASetLastError (WSAEINTR);
			} else {
				if (transmit_file (sock, file, 0, 0, NULL, lpTransmitBuffers, dwReserved))
					ret = 0;
			}
			MONO_EXIT_GC_SAFE;
		}

		if (blocking && info && !alerted) {
			mono_win32_leave_blocking_io_call (info, (HANDLE)sock);
			mono_thread_info_uninstall_interrupt (&alerted);
		}
	}

	if (ret != 0)
		ret = WSAGetLastError ();

	return ret;
}
#endif /* HAVE_API_SUPPORT_WIN32_TRANSMIT_FILE */

#if HAVE_API_SUPPORT_WIN32_DISCONNECT_EX
static gint
internal_w32socket_disconnect (SOCKET sock, gboolean reuse, gboolean blocking)
{
	gint ret = ERROR_NOT_SUPPORTED;
	LPFN_DISCONNECTEX disconnect;
	GUID disconnect_guid = WSAID_DISCONNECTEX;
	DWORD output_bytes;

	if (!WSAIoctl (sock, SIO_GET_EXTENSION_FUNCTION_POINTER, &disconnect_guid, sizeof (GUID), &disconnect, sizeof (LPFN_DISCONNECTEX), &output_bytes, NULL, NULL)) {
		MonoThreadInfo *info = mono_thread_info_current ();
		gboolean alerted = FALSE;

		if (blocking && info) {
			mono_thread_info_install_interrupt (win32_io_interrupt_handler, NULL, &alerted);
			if (alerted) {
				WSASetLastError (WSAEINTR);
			} else {
				mono_win32_enter_blocking_io_call (info, (HANDLE)sock);
			}
		}

		if (!alerted) {
			MONO_ENTER_GC_SAFE;
			if (blocking && info && mono_thread_info_is_interrupt_state (info)) {
				WSASetLastError (WSAEINTR);
			} else {
				if (disconnect (sock, NULL, reuse ? TF_REUSE_SOCKET : 0, 0))
					ret = 0;
			}
			MONO_EXIT_GC_SAFE;
		}

		if (blocking && info && !alerted) {
			mono_win32_leave_blocking_io_call (info, (HANDLE)sock);
			mono_thread_info_uninstall_interrupt (&alerted);
		}
	}

	if (ret != 0)
		ret = WSAGetLastError ();

	return ret;
}
#endif /* HAVE_API_SUPPORT_WIN32_DISCONNECT_EX */

#if HAVE_API_SUPPORT_WIN32_TRANSMIT_FILE
BOOL mono_w32socket_transmit_file (SOCKET hSocket, gpointer hFile, gpointer lpTransmitBuffers, guint32 dwReserved, gboolean blocking)
{
	return internal_w32socket_transmit_file (hSocket, hFile, (LPTRANSMIT_FILE_BUFFERS)lpTransmitBuffers, dwReserved, blocking) == 0 ? TRUE : FALSE;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_TRANSMIT_FILE
BOOL mono_w32socket_transmit_file (SOCKET hSocket, gpointer hFile, gpointer lpTransmitBuffers, guint32 dwReserved, gboolean blocking)
{
	g_unsupported_api ("TransmitFile");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_TRANSMIT_FILE */

#if HAVE_API_SUPPORT_WIN32_DISCONNECT_EX
gint
mono_w32socket_disconnect (SOCKET sock, gboolean reuse)
{
	gint ret = SOCKET_ERROR;

	ret = internal_w32socket_disconnect (sock, reuse, TRUE);
#if HAVE_API_SUPPORT_WIN32_TRANSMIT_FILE
	if (ret == 0)
		ret = internal_w32socket_transmit_file (sock, NULL, NULL, TF_DISCONNECT | (reuse ? TF_REUSE_SOCKET : 0), TRUE);
#endif /* HAVE_API_SUPPORT_WIN32_TRANSMIT_FILE */

	return ret;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_DISCONNECT_EX
gint
mono_w32socket_disconnect (SOCKET sock, gboolean reuse)
{
	g_unsupported_api ("DisconnectEx");
	SetLastError (ERROR_NOT_SUPPORTED);
	return ERROR_NOT_SUPPORTED;
}
#endif /* HAVE_API_SUPPORT_WIN32_DISCONNECT_EX */

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
	u_long amount_long = 0;
	MONO_ENTER_GC_SAFE;
	ret = ioctlsocket (sock, FIONREAD, &amount_long);
	*amount = amount_long;
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

MonoBoolean
ves_icall_System_Net_Sockets_Socket_SupportPortReuse_icall (MonoProtocolType proto)
{
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
