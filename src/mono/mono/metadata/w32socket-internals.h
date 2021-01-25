/**
 * \file
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_W32SOCKET_INTERNALS_H__
#define __MONO_METADATA_W32SOCKET_INTERNALS_H__

#include <config.h>
#include <glib.h>

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_SYS_SOCKET_H
#include <sys/socket.h>
#endif

#include <mono/utils/w32api.h>

#ifndef HAVE_SOCKLEN_T
#define socklen_t int
#endif

#include <mono/utils/w32subset.h>

#if defined(HOST_WIN32) && (HAVE_API_SUPPORT_WIN32_TRANSMIT_FILE || HAVE_API_SUPPORT_WIN32_DISCONNECT_EX)
#include <mswsock.h>
#else
typedef struct {
	gpointer Head;
	guint32 HeadLength;
	gpointer Tail;
	guint32 TailLength;
} TRANSMIT_FILE_BUFFERS, *LPTRANSMIT_FILE_BUFFERS;
#endif

#ifndef HOST_WIN32
#define TF_DISCONNECT 0x01
#define TF_REUSE_SOCKET 0x02

typedef struct {
	guint32 Data1;
	guint16 Data2;
	guint16 Data3;
	guint8 Data4[8];
} GUID;

typedef struct {
	guint32 Internal;
	guint32 InternalHigh;
	guint32 Offset;
	guint32 OffsetHigh;
	gpointer hEvent;
	gpointer handle1;
	gpointer handle2;
} OVERLAPPED;
#endif

void
mono_w32socket_initialize (void);

void
mono_w32socket_cleanup (void);

SOCKET
mono_w32socket_accept (SOCKET s, struct sockaddr *addr, socklen_t *addrlen, gboolean blocking);

int
mono_w32socket_connect (SOCKET s, const struct sockaddr *name, socklen_t namelen, gboolean blocking);

int
mono_w32socket_recv (SOCKET s, char *buf, int len, int flags, gboolean blocking);

int
mono_w32socket_recvfrom (SOCKET s, char *buf, int len, int flags, struct sockaddr *from, socklen_t *fromlen, gboolean blocking);

int
mono_w32socket_recvbuffers (SOCKET s, LPWSABUF lpBuffers, guint32 dwBufferCount, guint32 *lpNumberOfBytesRecvd, guint32 *lpFlags, gpointer lpOverlapped, gpointer lpCompletionRoutine, gboolean blocking);

int
mono_w32socket_send (SOCKET s, void *buf, int len, int flags, gboolean blocking);

int
mono_w32socket_sendto (SOCKET s, const char *buf, int len, int flags, const struct sockaddr *to, int tolen, gboolean blocking);

int
mono_w32socket_sendbuffers (SOCKET s, LPWSABUF lpBuffers, guint32 dwBufferCount, guint32 *lpNumberOfBytesRecvd, guint32 lpFlags, gpointer lpOverlapped, gpointer lpCompletionRoutine, gboolean blocking);

BOOL
mono_w32socket_transmit_file (SOCKET hSocket, gpointer hFile, gpointer lpTransmitBuffers, guint32 dwReserved, gboolean blocking);


#ifndef HOST_WIN32

SOCKET
mono_w32socket_socket (int domain, int type, int protocol);

gint
mono_w32socket_bind (SOCKET sock, struct sockaddr *addr, socklen_t addrlen);

gint
mono_w32socket_getpeername (SOCKET sock, struct sockaddr *name, socklen_t *namelen);

gint
mono_w32socket_getsockname (SOCKET sock, struct sockaddr *name, socklen_t *namelen);

gint
mono_w32socket_getsockopt (SOCKET sock, gint level, gint optname, gpointer optval, socklen_t *optlen);

gint
mono_w32socket_setsockopt (SOCKET sock, gint level, gint optname, gconstpointer optval, socklen_t optlen);

gint
mono_w32socket_listen (SOCKET sock, gint backlog);

gint
mono_w32socket_shutdown (SOCKET sock, gint how);

gint
mono_w32socket_ioctl (SOCKET sock, gint32 command, gchar *input, gint inputlen, gchar *output, gint outputlen, glong *written);

gboolean
mono_w32socket_close (SOCKET sock);

#endif /* HOST_WIN32 */

gint
mono_w32socket_disconnect (SOCKET sock, gboolean reuse);

gint
mono_w32socket_set_blocking (SOCKET socket, gboolean blocking);

gint
mono_w32socket_get_available (SOCKET socket, guint64 *amount);

void
mono_w32socket_set_last_error (gint32 error);

gint32
mono_w32socket_get_last_error (void);

gint32
mono_w32socket_convert_error (gint error);

gboolean
mono_w32socket_duplicate (gpointer handle, gint32 targetProcessId, gpointer *duplicate_handle);

#endif // __MONO_METADATA_W32SOCKET_INTERNALS_H__
