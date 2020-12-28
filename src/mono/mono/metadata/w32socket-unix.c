/**
 * \file
 * Unix specific socket code.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include <pthread.h>
#include <string.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#ifdef HAVE_SYS_IOCTL_H
#include <sys/ioctl.h>
#endif
#include <netinet/in.h>
#include <netinet/tcp.h>
#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif
#ifdef HAVE_NETINET_TCP_H
#include <arpa/inet.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <fcntl.h>
#ifdef HAVE_SYS_UIO_H
#include <sys/uio.h>
#endif
#ifdef HAVE_SYS_IOCTL_H
#include <sys/ioctl.h>
#endif
#ifdef HAVE_SYS_FILIO_H
#include <sys/filio.h>     /* defines FIONBIO and FIONREAD */
#endif
#ifdef HAVE_SYS_SOCKIO_H
#include <sys/sockio.h>    /* defines SIOCATMARK */
#endif
#include <signal.h>
#ifdef HAVE_SYS_SENDFILE_H
#include <sys/sendfile.h>
#endif
#include <sys/stat.h>

#include "w32socket.h"
#include "w32socket-internals.h"
#include "w32error.h"
#include "fdhandle.h"
#include "utils/mono-logger-internals.h"
#include "utils/mono-poll.h"
#include "utils/mono-compiler.h"
#include "icall-decl.h"
#include "utils/mono-errno.h"

typedef struct {
	MonoFDHandle fdhandle;
	gint domain;
	gint type;
	gint protocol;
	gint saved_error;
	gint still_readable;
} SocketHandle;

static SocketHandle*
socket_data_create (MonoFDType type, gint fd)
{
	SocketHandle *sockethandle;

	sockethandle = g_new0 (SocketHandle, 1);
	mono_fdhandle_init ((MonoFDHandle*) sockethandle, type, fd);

	return sockethandle;
}

static void
socket_data_close (MonoFDHandle *fdhandle)
{
	MonoThreadInfo *info;
	SocketHandle* sockethandle;
	gint ret;

	sockethandle = (SocketHandle*) fdhandle;
	g_assert (sockethandle);

	info = mono_thread_info_current ();

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: closing fd %d", __func__, ((MonoFDHandle*) sockethandle)->fd);

	/* Shutdown the socket for reading, to interrupt any potential
	 * receives that may be blocking for data.  See bug 75705. */
	MONO_ENTER_GC_SAFE;
	shutdown (((MonoFDHandle*) sockethandle)->fd, SHUT_RD);
	MONO_EXIT_GC_SAFE;

retry_close:
	MONO_ENTER_GC_SAFE;
	ret = close (((MonoFDHandle*) sockethandle)->fd);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		if (errno == EINTR && !mono_thread_info_is_interrupt_state (info))
			goto retry_close;
	}

	sockethandle->saved_error = 0;
}

static void
socket_data_destroy (MonoFDHandle *fdhandle)
{
	SocketHandle *sockethandle;

	sockethandle = (SocketHandle*) fdhandle;
	g_assert (sockethandle);

	g_free (sockethandle);
}

void
mono_w32socket_initialize (void)
{
	MonoFDHandleCallback socket_data_callbacks;
	memset (&socket_data_callbacks, 0, sizeof (socket_data_callbacks));
	socket_data_callbacks.close = socket_data_close;
	socket_data_callbacks.destroy = socket_data_destroy;

	mono_fdhandle_register (MONO_FDTYPE_SOCKET, &socket_data_callbacks);
}

void
mono_w32socket_cleanup (void)
{
}

SOCKET
mono_w32socket_accept (SOCKET sock, struct sockaddr *addr, socklen_t *addrlen, gboolean blocking)
{
	SocketHandle *sockethandle, *accepted_socket_data;
	MonoThreadInfo *info;
	gint accepted_fd;

	if (addr != NULL && *addrlen < sizeof(struct sockaddr)) {
		mono_w32socket_set_last_error (WSAEFAULT);
		return INVALID_SOCKET;
	}

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return INVALID_SOCKET;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return INVALID_SOCKET;
	}

	info = mono_thread_info_current ();

	do {
		MONO_ENTER_GC_SAFE;
		accepted_fd = accept (((MonoFDHandle*) sockethandle)->fd, addr, addrlen);
		MONO_EXIT_GC_SAFE;
	} while (accepted_fd == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	if (accepted_fd == -1) {
		gint error = mono_w32socket_convert_error (errno);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: accept error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (error);
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return INVALID_SOCKET;
	}

	accepted_socket_data = socket_data_create (MONO_FDTYPE_SOCKET, accepted_fd);
	accepted_socket_data->domain = sockethandle->domain;
	accepted_socket_data->type = sockethandle->type;
	accepted_socket_data->protocol = sockethandle->protocol;
	accepted_socket_data->still_readable = 1;

	mono_fdhandle_insert ((MonoFDHandle*) accepted_socket_data);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: returning accepted handle %p", __func__, GINT_TO_POINTER(((MonoFDHandle*) accepted_socket_data)->fd));

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return ((MonoFDHandle*) accepted_socket_data)->fd;
}

int
mono_w32socket_connect (SOCKET sock, const struct sockaddr *addr, socklen_t addrlen, gboolean blocking)
{
	SocketHandle *sockethandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	MONO_ENTER_GC_SAFE;
	ret = connect (((MonoFDHandle*) sockethandle)->fd, addr, addrlen);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		MonoThreadInfo *info;
		mono_pollfd fds;
		gint errnum, so_error;
		socklen_t len;

		errnum = errno;

		if (errno != EINTR) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: connect error: %s", __func__,
				   g_strerror (errnum));

			errnum = mono_w32socket_convert_error (errnum);
			if (errnum == WSAEINPROGRESS)
				errnum = WSAEWOULDBLOCK; /* see bug #73053 */

			mono_w32socket_set_last_error (errnum);

			/*
			 * On solaris x86 getsockopt (SO_ERROR) is not set after
			 * connect () fails so we need to save this error.
			 *
			 * But don't do this for EWOULDBLOCK (bug 317315)
			 */
			if (errnum != WSAEWOULDBLOCK) {
				/* ECONNRESET means the socket was closed by another thread */
				/* Async close on mac raises ECONNABORTED. */
				sockethandle->saved_error = errnum;
			}
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}

		info = mono_thread_info_current ();

		fds.fd = ((MonoFDHandle*) sockethandle)->fd;
		fds.events = MONO_POLLOUT;
		for (;;) {
			MONO_ENTER_GC_SAFE;
			ret = mono_poll (&fds, 1, -1);
			MONO_EXIT_GC_SAFE;
			if (ret != -1 || mono_thread_info_is_interrupt_state (info))
				break;

			if (errno != EINTR) {
				gint errnum = errno;
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: connect poll error: %s", __func__, g_strerror (errno));
				mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
				mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
				return SOCKET_ERROR;
			}
		}

		len = sizeof(so_error);
		MONO_ENTER_GC_SAFE;
		ret = getsockopt (((MonoFDHandle*) sockethandle)->fd, SOL_SOCKET, SO_ERROR, &so_error, &len);
		MONO_EXIT_GC_SAFE;
		if (ret == -1) {
			gint errnum = errno;
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: connect getsockopt error: %s", __func__, g_strerror (errno));
			mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}

		if (so_error != 0) {
			gint errnum = mono_w32socket_convert_error (so_error);

			/* Need to save this socket error */
			sockethandle->saved_error = errnum;

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: connect getsockopt returned error: %s",
				   __func__, g_strerror (so_error));

			mono_w32socket_set_last_error (errnum);
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

int
mono_w32socket_recv (SOCKET sock, char *buf, int len, int flags, gboolean blocking)
{
	return mono_w32socket_recvfrom (sock, buf, len, flags, NULL, 0, blocking);
}

int
mono_w32socket_recvfrom (SOCKET sock, char *buf, int len, int flags, struct sockaddr *from, socklen_t *fromlen, gboolean blocking)
{
	SocketHandle *sockethandle;
	int ret;
	MonoThreadInfo *info;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	do {
		MONO_ENTER_GC_SAFE;
		ret = recvfrom (((MonoFDHandle*) sockethandle)->fd, buf, len, flags, from, fromlen);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	if (ret == 0 && len > 0) {
		/* According to the Linux man page, recvfrom only
		 * returns 0 when the socket has been shut down
		 * cleanly.  Turn this into an EINTR to simulate win32
		 * behaviour of returning EINTR when a socket is
		 * closed while the recvfrom is blocking (we use a
		 * shutdown() in socket_close() to trigger this.) See
		 * bug 75705.
		 */
		/* Distinguish between the socket being shut down at
		 * the local or remote ends, and reads that request 0
		 * bytes to be read
		 */

		/* If this returns FALSE, it means the socket has been
		 * closed locally.  If it returns TRUE, but
		 * still_readable != 1 then shutdown
		 * (SHUT_RD|SHUT_RDWR) has been called locally.
		 */
		if (sockethandle->still_readable != 1) {
			ret = -1;
			mono_set_errno (EINTR);
		}
	}

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: recv error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}
	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return ret;
}

static void
wsabuf_to_msghdr (WSABUF *buffers, guint32 count, struct msghdr *hdr)
{
	guint32 i;

	memset (hdr, 0, sizeof (struct msghdr));
	hdr->msg_iovlen = count;
	hdr->msg_iov = g_new0 (struct iovec, count);
	for (i = 0; i < count; i++) {
		hdr->msg_iov [i].iov_base = buffers [i].buf;
		hdr->msg_iov [i].iov_len  = buffers [i].len;
	}
}

static void
msghdr_iov_free (struct msghdr *hdr)
{
	g_free (hdr->msg_iov);
}

int
mono_w32socket_recvbuffers (SOCKET sock, WSABUF *buffers, guint32 count, guint32 *received, guint32 *flags, gpointer overlapped, gpointer complete, gboolean blocking)
{
	SocketHandle *sockethandle;
	MonoThreadInfo *info;
	gint ret;
	struct msghdr hdr;

	g_assert (overlapped == NULL);
	g_assert (complete == NULL);

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	wsabuf_to_msghdr (buffers, count, &hdr);

	do {
		MONO_ENTER_GC_SAFE;
		ret = recvmsg (((MonoFDHandle*) sockethandle)->fd, &hdr, *flags);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	msghdr_iov_free (&hdr);

	if (ret == 0) {
		/* see mono_w32socket_recvfrom */
		if (sockethandle->still_readable != 1) {
			ret = -1;
			mono_set_errno (EINTR);
		}
	}

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: recvmsg error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	*received = ret;
	*flags = hdr.msg_flags;

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

int
mono_w32socket_send (SOCKET sock, void *buf, int len, int flags, gboolean blocking)
{
	SocketHandle *sockethandle;
	int ret;
	MonoThreadInfo *info;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	do {
		MONO_ENTER_GC_SAFE;
		ret = send (((MonoFDHandle*) sockethandle)->fd, buf, len, flags);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: send error: %s", __func__, g_strerror (errno));

#ifdef O_NONBLOCK
		/* At least linux returns EAGAIN/EWOULDBLOCK when the timeout has been set on
		 * a blocking socket. See bug #599488 */
		if (errnum == EAGAIN) {
			MONO_ENTER_GC_SAFE;
			ret = fcntl (((MonoFDHandle*) sockethandle)->fd, F_GETFL, 0);
			MONO_EXIT_GC_SAFE;
			if (ret != -1 && (ret & O_NONBLOCK) == 0)
				errnum = ETIMEDOUT;
		}
#endif /* O_NONBLOCK */
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}
	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return ret;
}

int
mono_w32socket_sendto (SOCKET sock, const char *buf, int len, int flags, const struct sockaddr *to, int tolen, gboolean blocking)
{
	SocketHandle *sockethandle;
	int ret;
	MonoThreadInfo *info;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	do {
		MONO_ENTER_GC_SAFE;
		ret = sendto (((MonoFDHandle*) sockethandle)->fd, buf, len, flags, to, tolen);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR &&  !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: send error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}
	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return ret;
}

int
mono_w32socket_sendbuffers (SOCKET sock, WSABUF *buffers, guint32 count, guint32 *sent, guint32 flags, gpointer overlapped, gpointer complete, gboolean blocking)
{
	struct msghdr hdr;
	MonoThreadInfo *info;
	SocketHandle *sockethandle;
	gint ret;

	g_assert (overlapped == NULL);
	g_assert (complete == NULL);

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	wsabuf_to_msghdr (buffers, count, &hdr);

	do {
		MONO_ENTER_GC_SAFE;
		ret = sendmsg (((MonoFDHandle*) sockethandle)->fd, &hdr, flags);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	msghdr_iov_free (&hdr);

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: sendmsg error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	*sent = ret;
	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

#define SF_BUFFER_SIZE	16384

BOOL
mono_w32socket_transmit_file (SOCKET sock, gpointer file_handle, gpointer lpTransmitBuffers, guint32 flags, gboolean blocking)
{
	MonoThreadInfo *info;
	SocketHandle *sockethandle;
	gint file;
	gssize ret;
#if defined(HAVE_SENDFILE) && (defined(__linux__) || defined(DARWIN))
	struct stat statbuf;
#else
	gpointer buffer;
#endif
	TRANSMIT_FILE_BUFFERS *buffers = (TRANSMIT_FILE_BUFFERS *)lpTransmitBuffers;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return FALSE;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return FALSE;
	}

	/* Write the header */
	if (buffers != NULL && buffers->Head != NULL && buffers->HeadLength > 0) {
		ret = mono_w32socket_send (((MonoFDHandle*) sockethandle)->fd, buffers->Head, buffers->HeadLength, 0, FALSE);
		if (ret == SOCKET_ERROR) {
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return FALSE;
		}
	}

	info = mono_thread_info_current ();

	file = GPOINTER_TO_INT (file_handle);

#if defined(HAVE_SENDFILE) && (defined(__linux__) || defined(DARWIN))
	MONO_ENTER_GC_SAFE;
	ret = fstat (file, &statbuf);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = errno;
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return FALSE;
	}

	do {
		MONO_ENTER_GC_SAFE;
#ifdef __linux__
		ret = sendfile (((MonoFDHandle*) sockethandle)->fd, file, NULL, statbuf.st_size);
#elif defined(DARWIN)
		/* TODO: header/tail could be sent in the 5th argument */
		/* TODO: Might not send the entire file for non-blocking sockets */
		ret = sendfile (file, ((MonoFDHandle*) sockethandle)->fd, 0, &statbuf.st_size, NULL, 0);
#endif
		MONO_EXIT_GC_SAFE;
	} while (ret != -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));
#else
	buffer = g_malloc (SF_BUFFER_SIZE);

	do {
		do {
			MONO_ENTER_GC_SAFE;
			ret = read (file, buffer, SF_BUFFER_SIZE);
			MONO_EXIT_GC_SAFE;
		} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

		if (ret == -1 || ret == 0)
			break;

		do {
			MONO_ENTER_GC_SAFE;
			ret = send (((MonoFDHandle*) sockethandle)->fd, buffer, ret, 0); /* short sends? enclose this in a loop? */
			MONO_EXIT_GC_SAFE;
		} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));
	} while (ret != -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	g_free (buffer);
#endif

	if (ret == -1) {
		gint errnum = errno;
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return FALSE;
	}

	/* Write the tail */
	if (buffers != NULL && buffers->Tail != NULL && buffers->TailLength > 0) {
		ret = mono_w32socket_send (((MonoFDHandle*) sockethandle)->fd, buffers->Tail, buffers->TailLength, 0, FALSE);
		if (ret == SOCKET_ERROR) {
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return FALSE;
		}
	}

	if ((flags & TF_DISCONNECT) == TF_DISCONNECT)
		mono_w32socket_close (((MonoFDHandle*) sockethandle)->fd);

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return TRUE;
}

SOCKET
mono_w32socket_socket (int domain, int type, int protocol)
{
	SocketHandle *sockethandle;
	gint fd;

retry_socket:
	MONO_ENTER_GC_SAFE;
	fd = socket (domain, type, protocol);
	MONO_EXIT_GC_SAFE;
	if (fd == -1) {
		if (domain == AF_INET && type == SOCK_RAW && protocol == 0) {
			/* Retry with protocol == 4 (see bug #54565) */
			// https://bugzilla.novell.com/show_bug.cgi?id=MONO54565
			protocol = 4;
			goto retry_socket;
		}

		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: socket error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return INVALID_SOCKET;
	}

	sockethandle = socket_data_create(MONO_FDTYPE_SOCKET, fd);
	sockethandle->domain = domain;
	sockethandle->type = type;
	sockethandle->protocol = protocol;
	sockethandle->still_readable = 1;

	/* .net seems to set this by default for SOCK_STREAM, not for
	 * SOCK_DGRAM (see bug #36322)
	 * https://bugzilla.novell.com/show_bug.cgi?id=MONO36322
	 *
	 * It seems winsock has a rather different idea of what
	 * SO_REUSEADDR means.  If it's set, then a new socket can be
	 * bound over an existing listening socket.  There's a new
	 * windows-specific option called SO_EXCLUSIVEADDRUSE but
	 * using that means the socket MUST be closed properly, or a
	 * denial of service can occur.  Luckily for us, winsock
	 * behaves as though any other system would when SO_REUSEADDR
	 * is true, so we don't need to do anything else here.  See
	 * bug 53992.
	 * https://bugzilla.novell.com/show_bug.cgi?id=MONO53992
	 */
	{
		int ret;
		const int true_ = 1;

		MONO_ENTER_GC_SAFE;
		ret = setsockopt (((MonoFDHandle*) sockethandle)->fd, SOL_SOCKET, SO_REUSEADDR, &true_, sizeof (true_));
		MONO_EXIT_GC_SAFE;
		if (ret == -1) {
			gint errnum = errno;
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: Error setting SO_REUSEADDR", __func__);
			mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));

			MONO_ENTER_GC_SAFE;
			close (((MonoFDHandle*) sockethandle)->fd);
			MONO_EXIT_GC_SAFE;

			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return INVALID_SOCKET;
		}
	}

	mono_fdhandle_insert ((MonoFDHandle*) sockethandle);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: returning socket handle %p", __func__, GINT_TO_POINTER(((MonoFDHandle*) sockethandle)->fd));

	return ((MonoFDHandle*) sockethandle)->fd;
}

gint
mono_w32socket_bind (SOCKET sock, struct sockaddr *addr, socklen_t addrlen)
{
	SocketHandle *sockethandle;
	int ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	MONO_ENTER_GC_SAFE;
	ret = bind (((MonoFDHandle*) sockethandle)->fd, addr, addrlen);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: bind error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

gint
mono_w32socket_getpeername (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	SocketHandle *sockethandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

#ifdef HAVE_GETPEERNAME
	MONO_ENTER_GC_SAFE;
	ret = getpeername (((MonoFDHandle*) sockethandle)->fd, name, namelen);
	MONO_EXIT_GC_SAFE;
#else
	ret = -1;
#endif
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: getpeername error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

gint
mono_w32socket_getsockname (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	SocketHandle *sockethandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	MONO_ENTER_GC_SAFE;
	ret = getsockname (((MonoFDHandle*) sockethandle)->fd, name, namelen);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: getsockname error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

gint
mono_w32socket_getsockopt (SOCKET sock, gint level, gint optname, gpointer optval, socklen_t *optlen)
{
	SocketHandle *sockethandle;
	gint ret;
	struct timeval tv;
	gpointer tmp_val;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	tmp_val = optval;
	if (level == SOL_SOCKET &&
	    (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		tmp_val = &tv;
		*optlen = sizeof (tv);
	}

	MONO_ENTER_GC_SAFE;
	ret = getsockopt (((MonoFDHandle*) sockethandle)->fd, level, optname, tmp_val, optlen);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: getsockopt error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	if (level == SOL_SOCKET && (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		*((int *) optval) = tv.tv_sec * 1000 + (tv.tv_usec / 1000);	// milli from micro
		*optlen = sizeof (int);
	}

	if (optname == SO_ERROR) {
		if (*((int *)optval) != 0) {
			*((int *) optval) = mono_w32socket_convert_error (*((int *)optval));
			sockethandle->saved_error = *((int *)optval);
		} else {
			*((int *)optval) = sockethandle->saved_error;
		}
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

gint
mono_w32socket_setsockopt (SOCKET sock, gint level, gint optname, gconstpointer optval, socklen_t optlen)
{
	SocketHandle *sockethandle;
	gint ret;
	gconstpointer tmp_val;
#if defined (__linux__)
	/* This has its address taken so it cannot be moved to the if block which uses it */
	gint bufsize = 0;
#endif
	struct timeval tv;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	tmp_val = optval;
	if (level == SOL_SOCKET &&
	    (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		int ms = *((int *) optval);
		tv.tv_sec = ms / 1000;
		tv.tv_usec = (ms % 1000) * 1000;	// micro from milli
		tmp_val = &tv;
		optlen = sizeof (tv);
	}
#if defined (__linux__)
	else if (level == SOL_SOCKET &&
		   (optname == SO_SNDBUF || optname == SO_RCVBUF)) {
		/* According to socket(7) the Linux kernel doubles the
		 * buffer sizes "to allow space for bookkeeping
		 * overhead."
		 */
		bufsize = *((int *) optval);

		bufsize /= 2;
		tmp_val = &bufsize;
	}
#endif

	MONO_ENTER_GC_SAFE;
	ret = setsockopt (((MonoFDHandle*) sockethandle)->fd, level, optname, tmp_val, optlen);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: setsockopt error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

#if defined (SO_REUSEPORT)
	/* BSD's and MacOS X multicast sockets also need SO_REUSEPORT when SO_REUSEADDR is requested.  */
	if (level == SOL_SOCKET && optname == SO_REUSEADDR) {
		int type;
		socklen_t type_len = sizeof (type);

		MONO_ENTER_GC_SAFE;
		ret = getsockopt (((MonoFDHandle*) sockethandle)->fd, level, SO_TYPE, &type, &type_len);
		MONO_EXIT_GC_SAFE;
		if (!ret) {
			if (type == SOCK_DGRAM || type == SOCK_STREAM) {
				MONO_ENTER_GC_SAFE;
				setsockopt (((MonoFDHandle*) sockethandle)->fd, level, SO_REUSEPORT, tmp_val, optlen);
				MONO_EXIT_GC_SAFE;
			}
		}
	}
#endif

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return ret;
}

gint
mono_w32socket_listen (SOCKET sock, gint backlog)
{
	SocketHandle *sockethandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	MONO_ENTER_GC_SAFE;
	ret = listen (((MonoFDHandle*) sockethandle)->fd, backlog);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: listen error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

gint
mono_w32socket_shutdown (SOCKET sock, gint how)
{
	SocketHandle *sockethandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (how == SHUT_RD || how == SHUT_RDWR)
		sockethandle->still_readable = 0;

	MONO_ENTER_GC_SAFE;
	ret = shutdown (((MonoFDHandle*) sockethandle)->fd, how);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: shutdown error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return ret;
}

gint
mono_w32socket_disconnect (SOCKET sock, gboolean reuse)
{
	SocketHandle *sockethandle;
	SOCKET newsock;
	gint ret;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: called on socket %d!", __func__, sock);

	/* We could check the socket type here and fail unless its
	 * SOCK_STREAM, SOCK_SEQPACKET or SOCK_RDM (according to msdn)
	 * if we really wanted to */

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	MONO_ENTER_GC_SAFE;
	newsock = socket (sockethandle->domain, sockethandle->type, sockethandle->protocol);
	MONO_EXIT_GC_SAFE;
	if (newsock == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: socket error: %s", __func__, g_strerror (errnum));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	/* According to Stevens "Advanced Programming in the UNIX
	 * Environment: UNIX File I/O" dup2() is atomic so there
	 * should not be a race condition between the old fd being
	 * closed and the new socket fd being copied over */
	do {
		MONO_ENTER_GC_SAFE;
		ret = dup2 (newsock, ((MonoFDHandle*) sockethandle)->fd);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EAGAIN);

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: dup2 error: %s", __func__, g_strerror (errnum));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	MONO_ENTER_GC_SAFE;
	close (newsock);
	MONO_EXIT_GC_SAFE;

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

static gboolean
extension_disconect (SOCKET sock, OVERLAPPED *overlapped, guint32 flags, guint32 reserved)
{
	gboolean ret;
	MONO_ENTER_GC_UNSAFE;
	ret = mono_w32socket_disconnect (sock, flags & TF_REUSE_SOCKET) == 0;
	MONO_EXIT_GC_UNSAFE;
	return ret;
}

static gboolean
extension_transmit_file (SOCKET sock, gpointer file_handle, guint32 bytes_to_write, guint32 bytes_per_send,
	OVERLAPPED *ol, TRANSMIT_FILE_BUFFERS *buffers, guint32 flags)
{
	gboolean ret;
	MONO_ENTER_GC_UNSAFE;
	ret = mono_w32socket_transmit_file (sock, file_handle, buffers, flags, FALSE);
	MONO_EXIT_GC_UNSAFE;
	return ret;
}

const
static struct {
	GUID guid;
	gpointer func;
} extension_functions[] = {
	{ {0x7fda2e11,0x8630,0x436f,{0xa0,0x31,0xf5,0x36,0xa6,0xee,0xc1,0x57}} /* WSAID_DISCONNECTEX */, (gpointer)extension_disconect },
	{ {0xb5367df0,0xcbac,0x11cf,{0x95,0xca,0x00,0x80,0x5f,0x48,0xa1,0x92}} /* WSAID_TRANSMITFILE */, (gpointer)extension_transmit_file },
	{ {0} , NULL },
};

gint
mono_w32socket_ioctl (SOCKET sock, gint32 command, gchar *input, gint inputlen, gchar *output, gint outputlen, glong *written)
{
	SocketHandle *sockethandle;
	gint ret;
	gpointer buffer;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (command == 0xC8000006 /* SIO_GET_EXTENSION_FUNCTION_POINTER */) {
		gint i;
		GUID *guid;

		if (inputlen < sizeof(GUID)) {
			/* As far as I can tell, windows doesn't
			 * actually set an error here...
			 */
			mono_w32socket_set_last_error (WSAEINVAL);
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}

		if (outputlen < sizeof(gpointer)) {
			/* Or here... */
			mono_w32socket_set_last_error (WSAEINVAL);
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}

		if (output == NULL) {
			/* Or here */
			mono_w32socket_set_last_error (WSAEINVAL);
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}

		guid = (GUID*) input;
		for (i = 0; extension_functions[i].func; i++) {
			if (memcmp (guid, &extension_functions[i].guid, sizeof(GUID)) == 0) {
				memcpy (output, &extension_functions[i].func, sizeof(gpointer));
				*written = sizeof(gpointer);
				mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
				return 0;
			}
		}

		mono_w32socket_set_last_error (WSAEINVAL);
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	if (command == 0x98000004 /* SIO_KEEPALIVE_VALS */) {
		guint32 onoff;

		if (inputlen < 3 * sizeof (guint32)) {
			mono_w32socket_set_last_error (WSAEINVAL);
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}

		onoff = *((guint32*) input);

		MONO_ENTER_GC_SAFE;
		ret = setsockopt (((MonoFDHandle*) sockethandle)->fd, SOL_SOCKET, SO_KEEPALIVE, &onoff, sizeof (guint32));
		MONO_EXIT_GC_SAFE;
		if (ret < 0) {
			mono_w32socket_set_last_error (mono_w32socket_convert_error (errno));
			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return SOCKET_ERROR;
		}

#if defined(TCP_KEEPIDLE) && defined(TCP_KEEPINTVL)
		if (onoff != 0) {
			/* Values are in ms, but we need s */
			guint32 keepalivetime, keepaliveinterval, rem;

			keepalivetime = *(((guint32*) input) + 1);
			keepaliveinterval = *(((guint32*) input) + 2);

			/* keepalivetime and keepaliveinterval are > 0 (checked in managed code) */
			rem = keepalivetime % 1000;
			keepalivetime /= 1000;
			if (keepalivetime == 0 || rem >= 500)
				keepalivetime++;
			MONO_ENTER_GC_SAFE;
			ret = setsockopt (((MonoFDHandle*) sockethandle)->fd, IPPROTO_TCP, TCP_KEEPIDLE, &keepalivetime, sizeof (guint32));
			MONO_EXIT_GC_SAFE;
			if (ret == 0) {
				rem = keepaliveinterval % 1000;
				keepaliveinterval /= 1000;
				if (keepaliveinterval == 0 || rem >= 500)
					keepaliveinterval++;
				MONO_ENTER_GC_SAFE;
				ret = setsockopt (((MonoFDHandle*) sockethandle)->fd, IPPROTO_TCP, TCP_KEEPINTVL, &keepaliveinterval, sizeof (guint32));
				MONO_EXIT_GC_SAFE;
			}
			if (ret != 0) {
				mono_w32socket_set_last_error (mono_w32socket_convert_error (errno));
				mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
				return SOCKET_ERROR;
			}

			mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
			return 0;
		}
#endif

		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return 0;
	}

	buffer = inputlen > 0 ? (gchar*) g_memdup (input, inputlen) : NULL;

	MONO_ENTER_GC_SAFE;
	ret = ioctl (((MonoFDHandle*) sockethandle)->fd, command, buffer);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		g_free (buffer);

		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: WSAIoctl error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	if (!buffer) {
		*written = 0;
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return 0;
	}

	/* We just copy the buffer to the output. Some ioctls
	 * don't even output any data, but, well...
	 *
	 * NB windows returns WSAEFAULT if outputlen is too small */
	inputlen = (inputlen > outputlen) ? outputlen : inputlen;

	if (inputlen > 0 && output != NULL)
		memcpy (output, buffer, inputlen);

	g_free (buffer);
	*written = inputlen;

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

gboolean
mono_w32socket_close (SOCKET sock)
{
	if (!mono_fdhandle_close (sock)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	return TRUE;
}

gint
mono_w32socket_set_blocking (SOCKET sock, gboolean blocking)
{
#ifdef O_NONBLOCK
	SocketHandle *sockethandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	/* This works better than ioctl(...FIONBIO...)
	 * on Linux (it causes connect to return
	 * EINPROGRESS, but the ioctl doesn't seem to) */
	MONO_ENTER_GC_SAFE;
	ret = fcntl (((MonoFDHandle*) sockethandle)->fd, F_GETFL, 0);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = mono_w32socket_convert_error (errno);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: fcntl(F_GETFL) error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (errnum);
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	MONO_ENTER_GC_SAFE;
	ret = fcntl (((MonoFDHandle*) sockethandle)->fd, F_SETFL, blocking ? (ret & (~O_NONBLOCK)) : (ret | (O_NONBLOCK)));
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = mono_w32socket_convert_error (errno);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: fcntl(F_SETFL) error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (errnum);
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
#else
	mono_w32socket_set_last_error (ERROR_NOT_SUPPORTED);
	return SOCKET_ERROR;
#endif /* O_NONBLOCK */
}

gint
mono_w32socket_get_available (SOCKET sock, guint64 *amount)
{
	SocketHandle *sockethandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(sock, (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

#if defined (HOST_DARWIN)
	// ioctl (socket, FIONREAD, XXX) returns the size of
	// the UDP header as well on Darwin.
	//
	// Use getsockopt SO_NREAD instead to get the
	// right values for TCP and UDP.
	//
	// ai_canonname can be null in some cases on darwin,
	// where the runtime assumes it will be the value of
	// the ip buffer.

	socklen_t optlen = sizeof (int);
	MONO_ENTER_GC_SAFE;
	ret = getsockopt (((MonoFDHandle*) sockethandle)->fd, SOL_SOCKET, SO_NREAD, (gulong*) amount, &optlen);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = mono_w32socket_convert_error (errno);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: getsockopt error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (errnum);
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}
#else
	MONO_ENTER_GC_SAFE;
	ret = ioctl (((MonoFDHandle*) sockethandle)->fd, FIONREAD, (gulong*) amount);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		gint errnum = mono_w32socket_convert_error (errno);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_SOCKET, "%s: ioctl error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (errnum);
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		return SOCKET_ERROR;
	}
#endif

	mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
	return 0;
}

void
mono_w32socket_set_last_error (gint32 error)
{
	mono_w32error_set_last (error);
}

gint32
mono_w32socket_get_last_error (void)
{
	return mono_w32error_get_last ();
}

gint32
mono_w32socket_convert_error (gint error)
{
	switch (error) {
	case 0: return ERROR_SUCCESS;
	case EACCES: return WSAEACCES;
#ifdef EADDRINUSE
	case EADDRINUSE: return WSAEADDRINUSE;
#endif
#ifdef EAFNOSUPPORT
	case EAFNOSUPPORT: return WSAEAFNOSUPPORT;
#endif
#if EAGAIN != EWOULDBLOCK
	case EAGAIN: return WSAEWOULDBLOCK;
#endif
#ifdef EALREADY
	case EALREADY: return WSAEALREADY;
#endif
	case EBADF: return WSAENOTSOCK;
#ifdef ECONNABORTED
	case ECONNABORTED: return WSAENETDOWN;
#endif
#ifdef ECONNREFUSED
	case ECONNREFUSED: return WSAECONNREFUSED;
#endif
#ifdef ECONNRESET
	case ECONNRESET: return WSAECONNRESET;
#endif
	case EDOM: return WSAEINVAL; /* not a precise match, best wecan do. */
	case EFAULT: return WSAEFAULT;
#ifdef EHOSTUNREACH
	case EHOSTUNREACH: return WSAEHOSTUNREACH;
#endif
#ifdef EINPROGRESS
	case EINPROGRESS: return WSAEINPROGRESS;
#endif
	case EINTR: return WSAEINTR;
	case EINVAL: return WSAEINVAL;
	case EIO: return WSA_INVALID_HANDLE; /* not a precise match, best we can do. */
#ifdef EISCONN
	case EISCONN: return WSAEISCONN;
#endif
	case ELOOP: return WSAELOOP;
	case ENFILE: return WSAEMFILE; /* not a precise match, best we can do. */
	case EMFILE: return WSAEMFILE;
#ifdef EMSGSIZE
	case EMSGSIZE: return WSAEMSGSIZE;
#endif
	case ENAMETOOLONG: return WSAENAMETOOLONG;
#ifdef ENETUNREACH
	case ENETUNREACH: return WSAENETUNREACH;
#endif
#ifdef ENOBUFS
	case ENOBUFS: return WSAENOBUFS; /* not documented */
#endif
	case ENOMEM: return WSAENOBUFS;
#ifdef ENOPROTOOPT
	case ENOPROTOOPT: return WSAENOPROTOOPT;
#endif
#ifdef ENOSR
	case ENOSR: return WSAENETDOWN;
#endif
#ifdef ENOTCONN
	case ENOTCONN: return WSAENOTCONN;
#endif
	case ENOTDIR: return WSA_INVALID_PARAMETER; /* not a precise match, best we can do. */
#ifdef ENOTSOCK
	case ENOTSOCK: return WSAENOTSOCK;
#endif
	case ENOTTY: return WSAENOTSOCK;
#ifdef EOPNOTSUPP
	case EOPNOTSUPP: return WSAEOPNOTSUPP;
#endif
	case EPERM: return WSAEACCES;
	case EPIPE: return WSAESHUTDOWN;
#ifdef EPROTONOSUPPORT
	case EPROTONOSUPPORT: return WSAEPROTONOSUPPORT;
#endif
#if ERESTARTSYS
	case ERESTARTSYS: return WSAENETDOWN;
#endif
	/*FIXME: case EROFS: return WSAE????; */
#ifdef ESOCKTNOSUPPORT
	case ESOCKTNOSUPPORT: return WSAESOCKTNOSUPPORT;
#endif
#ifdef ETIMEDOUT
	case ETIMEDOUT: return WSAETIMEDOUT;
#endif
#ifdef EWOULDBLOCK
	case EWOULDBLOCK: return WSAEWOULDBLOCK;
#endif
#ifdef EADDRNOTAVAIL
	case EADDRNOTAVAIL: return WSAEADDRNOTAVAIL;
#endif
	/* This might happen with unix sockets */
	case ENOENT: return WSAECONNREFUSED;
#ifdef EDESTADDRREQ
	case EDESTADDRREQ: return WSAEDESTADDRREQ;
#endif
#ifdef EHOSTDOWN
	case EHOSTDOWN: return WSAEHOSTDOWN;
#endif
#ifdef ENETDOWN
	case ENETDOWN: return WSAENETDOWN;
#endif
	case ENODEV: return WSAENETDOWN;
#ifdef EPROTOTYPE
	case EPROTOTYPE: return WSAEPROTOTYPE;
#endif
#ifdef ENXIO
	case ENXIO: return WSAENXIO;
#endif
#ifdef ENONET
	case ENONET: return WSAENETUNREACH;
#endif
#ifdef ENOKEY
	case ENOKEY: return WSAENETUNREACH;
#endif
	default:
		g_error ("%s: no translation into winsock error for (%d) \"%s\"", __func__, error, g_strerror (error));
	}
}

#ifndef ENABLE_NETCORE
MonoBoolean
ves_icall_System_Net_Sockets_Socket_SupportPortReuse_icall (MonoProtocolType proto)
{
#if defined (SO_REUSEPORT)
	return TRUE;
#else
#ifdef __linux__
	/* Linux always supports double binding for UDP, even on older kernels. */
	if (proto == ProtocolType_Udp)
		return TRUE;
#endif
	return FALSE;
#endif
}
#endif

gboolean
mono_w32socket_duplicate (gpointer handle, gint32 targetProcessId, gpointer *duplicate_handle)
{
	SocketHandle *sockethandle;

	if (!mono_fdhandle_lookup_and_ref (GPOINTER_TO_INT(handle), (MonoFDHandle**) &sockethandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (((MonoFDHandle*) sockethandle)->type != MONO_FDTYPE_SOCKET) {
		mono_fdhandle_unref ((MonoFDHandle*) sockethandle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	*duplicate_handle = handle;
	return TRUE;
}
