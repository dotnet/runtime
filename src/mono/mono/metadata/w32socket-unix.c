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
#include <arpa/inet.h>
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
#ifndef HAVE_MSG_NOSIGNAL
#include <signal.h>
#endif
#ifdef HAVE_SYS_SENDFILE_H
#include <sys/sendfile.h>
#endif
#include <sys/stat.h>

#include "w32socket.h"
#include "w32socket-internals.h"
#include "w32error.h"
#include "w32handle.h"
#include "utils/mono-logger-internals.h"
#include "utils/mono-poll.h"

typedef struct {
	int domain;
	int type;
	int protocol;
	int saved_error;
	int still_readable;
} MonoW32HandleSocket;

static guint32 in_cleanup = 0;

static void
socket_close (gpointer handle, gpointer data)
{
	int ret;
	MonoW32HandleSocket *socket_handle = (MonoW32HandleSocket *)data;
	MonoThreadInfo *info = mono_thread_info_current ();

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: closing socket handle %p", __func__, handle);

	/* Shutdown the socket for reading, to interrupt any potential
	 * receives that may be blocking for data.  See bug 75705. */
	shutdown (GPOINTER_TO_UINT (handle), SHUT_RD);

	do {
		ret = close (GPOINTER_TO_UINT(handle));
	} while (ret == -1 && errno == EINTR &&
		 !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: close error: %s", __func__, g_strerror (errno));
		if (!in_cleanup)
			mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
	}

	if (!in_cleanup)
		socket_handle->saved_error = 0;
}

static void
socket_details (gpointer data)
{
	/* FIXME: do something */
}

static const gchar*
socket_typename (void)
{
	return "Socket";
}

static gsize
socket_typesize (void)
{
	return sizeof (MonoW32HandleSocket);
}

static MonoW32HandleOps ops = {
	socket_close,    /* close */
	NULL,            /* signal */
	NULL,            /* own */
	NULL,            /* is_owned */
	NULL,            /* special_wait */
	NULL,            /* prewait */
	socket_details,  /* details */
	socket_typename, /* typename */
	socket_typesize, /* typesize */
};

void
mono_w32socket_initialize (void)
{
	mono_w32handle_register_ops (MONO_W32HANDLE_SOCKET, &ops);
}

static gboolean
cleanup_close (gpointer handle, gpointer data, gpointer user_data)
{
	if (mono_w32handle_get_type (handle) == MONO_W32HANDLE_SOCKET)
		mono_w32handle_force_close (handle, data);

	return FALSE;
}

void
mono_w32socket_cleanup (void)
{
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: cleaning up", __func__);

	in_cleanup = 1;
	mono_w32handle_foreach (cleanup_close, NULL);
	in_cleanup = 0;
}

SOCKET
mono_w32socket_accept (SOCKET sock, struct sockaddr *addr, socklen_t *addrlen, gboolean blocking)
{
	gpointer handle;
	gpointer new_handle;
	MonoW32HandleSocket *socket_handle;
	MonoW32HandleSocket new_socket_handle;
	SOCKET new_fd;
	MonoThreadInfo *info;

	if (addr != NULL && *addrlen < sizeof(struct sockaddr)) {
		mono_w32socket_set_last_error (WSAEFAULT);
		return INVALID_SOCKET;
	}

	handle = GUINT_TO_POINTER (sock);
	if (!mono_w32handle_lookup (handle, MONO_W32HANDLE_SOCKET, (gpointer *)&socket_handle)) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return INVALID_SOCKET;
	}

	info = mono_thread_info_current ();

	do {
		new_fd = accept (sock, addr, addrlen);
	} while (new_fd == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	if (new_fd == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: accept error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return INVALID_SOCKET;
	}

	if (new_fd >= mono_w32handle_fd_reserve) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: File descriptor is too big", __func__);

		mono_w32socket_set_last_error (WSASYSCALLFAILURE);

		close (new_fd);

		return INVALID_SOCKET;
	}

	new_socket_handle.domain = socket_handle->domain;
	new_socket_handle.type = socket_handle->type;
	new_socket_handle.protocol = socket_handle->protocol;
	new_socket_handle.still_readable = 1;

	new_handle = mono_w32handle_new_fd (MONO_W32HANDLE_SOCKET, new_fd,
					  &new_socket_handle);
	if(new_handle == INVALID_HANDLE_VALUE) {
		g_warning ("%s: error creating socket handle", __func__);
		mono_w32socket_set_last_error (ERROR_GEN_FAILURE);
		return INVALID_SOCKET;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: returning newly accepted socket handle %p with",
		   __func__, new_handle);

	return new_fd;
}

int
mono_w32socket_connect (SOCKET sock, const struct sockaddr *addr, int addrlen, gboolean blocking)
{
	gpointer handle;
	MonoW32HandleSocket *socket_handle;

	handle = GUINT_TO_POINTER (sock);
	if (!mono_w32handle_lookup (handle, MONO_W32HANDLE_SOCKET, (gpointer *)&socket_handle)) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (connect (sock, addr, addrlen) == -1) {
		MonoThreadInfo *info;
		mono_pollfd fds;
		gint errnum, so_error;
		socklen_t len;

		errnum = errno;

		if (errno != EINTR) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: connect error: %s", __func__,
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
				socket_handle->saved_error = errnum;
			}
			return SOCKET_ERROR;
		}

		info = mono_thread_info_current ();

		fds.fd = sock;
		fds.events = MONO_POLLOUT;
		while (mono_poll (&fds, 1, -1) == -1 && !mono_thread_info_is_interrupt_state (info)) {
			if (errno != EINTR) {
				gint errnum = errno;
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: connect poll error: %s", __func__, g_strerror (errno));
				mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
				return SOCKET_ERROR;
			}
		}

		len = sizeof(so_error);
		if (getsockopt (sock, SOL_SOCKET, SO_ERROR, &so_error, &len) == -1) {
			gint errnum = errno;
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: connect getsockopt error: %s", __func__, g_strerror (errno));
			mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
			return SOCKET_ERROR;
		}

		if (so_error != 0) {
			gint errnum = mono_w32socket_convert_error (so_error);

			/* Need to save this socket error */
			socket_handle->saved_error = errnum;

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: connect getsockopt returned error: %s",
				   __func__, g_strerror (so_error));

			mono_w32socket_set_last_error (errnum);
			return SOCKET_ERROR;
		}
	}

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
	gpointer handle;
	MonoW32HandleSocket *socket_handle;
	int ret;
	MonoThreadInfo *info;

	handle = GUINT_TO_POINTER (sock);
	if (!mono_w32handle_lookup (handle, MONO_W32HANDLE_SOCKET, (gpointer *)&socket_handle)) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	do {
		ret = recvfrom (sock, buf, len, flags, from, fromlen);
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
		if (socket_handle->still_readable != 1) {
			ret = -1;
			errno = EINTR;
		}
	}

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: recv error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}
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
	MonoW32HandleSocket *socket_handle;
	MonoThreadInfo *info;
	gpointer handle;
	gint ret;
	struct msghdr hdr;

	g_assert (overlapped == NULL);
	g_assert (complete == NULL);

	handle = GUINT_TO_POINTER (sock);
	if (!mono_w32handle_lookup (handle, MONO_W32HANDLE_SOCKET, (gpointer *)&socket_handle)) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	wsabuf_to_msghdr (buffers, count, &hdr);

	do {
		ret = recvmsg (sock, &hdr, *flags);
	} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	msghdr_iov_free (&hdr);

	if (ret == 0) {
		/* see mono_w32socket_recvfrom */
		if (socket_handle->still_readable != 1) {
			ret = -1;
			errno = EINTR;
		}
	}

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: recvmsg error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	*received = ret;
	*flags = hdr.msg_flags;

	return 0;
}

int
mono_w32socket_send (SOCKET sock, char *buf, int len, int flags, gboolean blocking)
{
	gpointer handle;
	int ret;
	MonoThreadInfo *info;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	do {
		ret = send (sock, buf, len, flags);
	} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: send error: %s", __func__, g_strerror (errno));

#ifdef O_NONBLOCK
		/* At least linux returns EAGAIN/EWOULDBLOCK when the timeout has been set on
		 * a blocking socket. See bug #599488 */
		if (errnum == EAGAIN) {
			ret = fcntl (sock, F_GETFL, 0);
			if (ret != -1 && (ret & O_NONBLOCK) == 0)
				errnum = ETIMEDOUT;
		}
#endif /* O_NONBLOCK */
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}
	return ret;
}

int
mono_w32socket_sendto (SOCKET sock, const char *buf, int len, int flags, const struct sockaddr *to, int tolen, gboolean blocking)
{
	gpointer handle;
	int ret;
	MonoThreadInfo *info;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	do {
		ret = sendto (sock, buf, len, flags, to, tolen);
	} while (ret == -1 && errno == EINTR &&  !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: send error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}
	return ret;
}

int
mono_w32socket_sendbuffers (SOCKET sock, WSABUF *buffers, guint32 count, guint32 *sent, guint32 flags, gpointer overlapped, gpointer complete, gboolean blocking)
{
	struct msghdr hdr;
	MonoThreadInfo *info;
	gpointer handle;
	gint ret;

	g_assert (overlapped == NULL);
	g_assert (complete == NULL);

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	info = mono_thread_info_current ();

	wsabuf_to_msghdr (buffers, count, &hdr);

	do {
		ret = sendmsg (sock, &hdr, flags);
	} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	msghdr_iov_free (&hdr);

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: sendmsg error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	*sent = ret;
	return 0;
}

#define SF_BUFFER_SIZE	16384

BOOL
mono_w32socket_transmit_file (SOCKET sock, gpointer file_handle, TRANSMIT_FILE_BUFFERS *buffers, guint32 flags, gboolean blocking)
{
	MonoThreadInfo *info;
	gpointer handle;
	gint file;
	gssize ret;
#if defined(HAVE_SENDFILE) && (defined(__linux__) || defined(DARWIN))
	struct stat statbuf;
#else
	gchar *buffer;
#endif

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return FALSE;
	}

	/* Write the header */
	if (buffers != NULL && buffers->Head != NULL && buffers->HeadLength > 0) {
		ret = mono_w32socket_send (sock, buffers->Head, buffers->HeadLength, 0, FALSE);
		if (ret == SOCKET_ERROR)
			return FALSE;
	}

	info = mono_thread_info_current ();

	file = GPOINTER_TO_INT (file_handle);

#if defined(HAVE_SENDFILE) && (defined(__linux__) || defined(DARWIN))
	ret = fstat (file, &statbuf);
	if (ret == -1) {
		gint errnum = errno;
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	do {
#ifdef __linux__
		ret = sendfile (sock, file, NULL, statbuf.st_size);
#elif defined(DARWIN)
		/* TODO: header/tail could be sent in the 5th argument */
		/* TODO: Might not send the entire file for non-blocking sockets */
		ret = sendfile (file, sock, 0, &statbuf.st_size, NULL, 0);
#endif
	} while (ret != -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));
#else
	buffer = g_malloc (SF_BUFFER_SIZE);

	do {
		do {
			ret = read (file, buffer, SF_BUFFER_SIZE);
		} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

		if (ret == -1 || ret == 0)
			break;

		do {
			ret = send (sock, buffer, ret, 0); /* short sends? enclose this in a loop? */
		} while (ret == -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));
	} while (ret != -1 && errno == EINTR && !mono_thread_info_is_interrupt_state (info));

	g_free (buffer);
#endif

	if (ret == -1) {
		gint errnum = errno;
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return FALSE;
	}

	/* Write the tail */
	if (buffers != NULL && buffers->Tail != NULL && buffers->TailLength > 0) {
		ret = mono_w32socket_send (sock, buffers->Tail, buffers->TailLength, 0, FALSE);
		if (ret == SOCKET_ERROR)
			return FALSE;
	}

	if ((flags & TF_DISCONNECT) == TF_DISCONNECT)
		mono_w32handle_close (handle);

	return TRUE;
}

SOCKET
mono_w32socket_socket (int domain, int type, int protocol)
{
	MonoW32HandleSocket socket_handle = {0};
	gpointer handle;
	SOCKET sock;

	socket_handle.domain = domain;
	socket_handle.type = type;
	socket_handle.protocol = protocol;
	socket_handle.still_readable = 1;

	sock = socket (domain, type, protocol);
	if (sock == -1 && domain == AF_INET && type == SOCK_RAW &&
	    protocol == 0) {
		/* Retry with protocol == 4 (see bug #54565) */
		// https://bugzilla.novell.com/show_bug.cgi?id=MONO54565
		socket_handle.protocol = 4;
		sock = socket (AF_INET, SOCK_RAW, 4);
	}

	if (sock == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: socket error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return INVALID_SOCKET;
	}

	if (sock >= mono_w32handle_fd_reserve) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: File descriptor is too big (%d >= %d)",
			   __func__, sock, mono_w32handle_fd_reserve);

		mono_w32socket_set_last_error (WSASYSCALLFAILURE);
		close (sock);

		return INVALID_SOCKET;
	}

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
		int ret, true_ = 1;

		ret = setsockopt (sock, SOL_SOCKET, SO_REUSEADDR, &true_, sizeof (true_));
		if (ret == -1) {
			close (sock);
			gint errnum = errno;
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Error setting SO_REUSEADDR", __func__);
			mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
			return INVALID_SOCKET;
		}
	}


	handle = mono_w32handle_new_fd (MONO_W32HANDLE_SOCKET, sock, &socket_handle);
	if (handle == INVALID_HANDLE_VALUE) {
		g_warning ("%s: error creating socket handle", __func__);
		mono_w32socket_set_last_error (WSASYSCALLFAILURE);
		close (sock);
		return INVALID_SOCKET;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: returning socket handle %p", __func__, handle);

	return sock;
}

gint
mono_w32socket_bind (SOCKET sock, struct sockaddr *addr, socklen_t addrlen)
{
	gpointer handle;
	int ret;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	ret = bind (sock, addr, addrlen);
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: bind error: %s", __func__, g_strerror(errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	return 0;
}

gint
mono_w32socket_getpeername (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	gpointer handle;
	gint ret;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	ret = getpeername (sock, name, namelen);
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: getpeername error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	return 0;
}

gint
mono_w32socket_getsockname (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	gpointer handle;
	gint ret;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	ret = getsockname (sock, name, namelen);
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: getsockname error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	return 0;
}

gint
mono_w32socket_getsockopt (SOCKET sock, gint level, gint optname, gpointer optval, socklen_t *optlen)
{
	gpointer handle;
	gint ret;
	struct timeval tv;
	gpointer tmp_val;
	MonoW32HandleSocket *socket_handle;

	handle = GUINT_TO_POINTER (sock);
	if (!mono_w32handle_lookup (handle, MONO_W32HANDLE_SOCKET, (gpointer *)&socket_handle)) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	tmp_val = optval;
	if (level == SOL_SOCKET &&
	    (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		tmp_val = &tv;
		*optlen = sizeof (tv);
	}

	ret = getsockopt (sock, level, optname, tmp_val, optlen);
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: getsockopt error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	if (level == SOL_SOCKET && (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		*((int *) optval) = tv.tv_sec * 1000 + (tv.tv_usec / 1000);	// milli from micro
		*optlen = sizeof (int);
	}

	if (optname == SO_ERROR) {
		if (*((int *)optval) != 0) {
			*((int *) optval) = mono_w32socket_convert_error (*((int *)optval));
			socket_handle->saved_error = *((int *)optval);
		} else {
			*((int *)optval) = socket_handle->saved_error;
		}
	}

	return 0;
}

gint
mono_w32socket_setsockopt (SOCKET sock, gint level, gint optname, const gpointer optval, socklen_t optlen)
{
	gpointer handle;
	gint ret;
	gpointer tmp_val;
#if defined (__linux__)
	/* This has its address taken so it cannot be moved to the if block which uses it */
	gint bufsize = 0;
#endif
	struct timeval tv;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
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

	ret = setsockopt (sock, level, optname, tmp_val, optlen);
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: setsockopt error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

#if defined (SO_REUSEPORT)
	/* BSD's and MacOS X multicast sockets also need SO_REUSEPORT when SO_REUSEADDR is requested.  */
	if (level == SOL_SOCKET && optname == SO_REUSEADDR) {
		int type;
		socklen_t type_len = sizeof (type);

		if (!getsockopt (sock, level, SO_TYPE, &type, &type_len)) {
			if (type == SOCK_DGRAM || type == SOCK_STREAM)
				setsockopt (sock, level, SO_REUSEPORT, tmp_val, optlen);
		}
	}
#endif

	return ret;
}

gint
mono_w32socket_listen (SOCKET sock, gint backlog)
{
	gpointer handle;
	gint ret;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	ret = listen (sock, backlog);
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: listen error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	return 0;
}

gint
mono_w32socket_shutdown (SOCKET sock, gint how)
{
	MonoW32HandleSocket *socket_handle;
	gpointer handle;
	gint ret;

	handle = GUINT_TO_POINTER (sock);
	if (!mono_w32handle_lookup (handle, MONO_W32HANDLE_SOCKET, (gpointer *)&socket_handle)) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (how == SHUT_RD || how == SHUT_RDWR)
		socket_handle->still_readable = 0;

	ret = shutdown (sock, how);
	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: shutdown error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	return ret;
}

gint
mono_w32socket_disconnect (SOCKET sock, gboolean reuse)
{
	MonoW32HandleSocket *socket_handle;
	gpointer handle;
	SOCKET newsock;
	gint ret;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: called on socket %d!", __func__, sock);

	/* We could check the socket type here and fail unless its
	 * SOCK_STREAM, SOCK_SEQPACKET or SOCK_RDM (according to msdn)
	 * if we really wanted to */

	handle = GUINT_TO_POINTER (sock);
	if (!mono_w32handle_lookup (handle, MONO_W32HANDLE_SOCKET, (gpointer *)&socket_handle)) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	newsock = socket (socket_handle->domain, socket_handle->type, socket_handle->protocol);
	if (newsock == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: socket error: %s", __func__, g_strerror (errnum));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	/* According to Stevens "Advanced Programming in the UNIX
	 * Environment: UNIX File I/O" dup2() is atomic so there
	 * should not be a race condition between the old fd being
	 * closed and the new socket fd being copied over */
	do {
		ret = dup2 (newsock, sock);
	} while (ret == -1 && errno == EAGAIN);

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: dup2 error: %s", __func__, g_strerror (errnum));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	close (newsock);

	return 0;
}

static gboolean
extension_disconect (SOCKET sock, OVERLAPPED *overlapped, guint32 flags, guint32 reserved)
{
	return mono_w32socket_disconnect (sock, flags & TF_REUSE_SOCKET) == 0;
}

static gboolean
extension_transmit_file (SOCKET sock, gpointer file_handle, guint32 bytes_to_write, guint32 bytes_per_send,
	OVERLAPPED *ol, TRANSMIT_FILE_BUFFERS *buffers, guint32 flags)
{
	return mono_w32socket_transmit_file (sock, file_handle, buffers, flags, FALSE);
}

static struct {
	GUID guid;
	gpointer func;
} extension_functions[] = {
	{ {0x7fda2e11,0x8630,0x436f,{0xa0,0x31,0xf5,0x36,0xa6,0xee,0xc1,0x57}} /* WSAID_DISCONNECTEX */, extension_disconect },
	{ {0xb5367df0,0xcbac,0x11cf,{0x95,0xca,0x00,0x80,0x5f,0x48,0xa1,0x92}} /* WSAID_TRANSMITFILE */, extension_transmit_file },
	{ {0} , NULL },
};

gint
mono_w32socket_ioctl (SOCKET sock, gint32 command, gchar *input, gint inputlen, gchar *output, gint outputlen, glong *written)
{
	gpointer handle;
	gint ret;
	gchar *buffer;

	handle = GUINT_TO_POINTER (sock);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
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
			return SOCKET_ERROR;
		}

		if (outputlen < sizeof(gpointer)) {
			/* Or here... */
			mono_w32socket_set_last_error (WSAEINVAL);
			return SOCKET_ERROR;
		}

		if (output == NULL) {
			/* Or here */
			mono_w32socket_set_last_error (WSAEINVAL);
			return SOCKET_ERROR;
		}

		guid = (GUID*) input;
		for (i = 0; extension_functions[i].func; i++) {
			if (memcmp (guid, &extension_functions[i].guid, sizeof(GUID)) == 0) {
				memcpy (output, &extension_functions[i].func, sizeof(gpointer));
				*written = sizeof(gpointer);
				return 0;
			}
		}

		mono_w32socket_set_last_error (WSAEINVAL);
		return SOCKET_ERROR;
	}

	if (command == 0x98000004 /* SIO_KEEPALIVE_VALS */) {
		guint32 onoff;

		if (inputlen < 3 * sizeof (guint32)) {
			mono_w32socket_set_last_error (WSAEINVAL);
			return SOCKET_ERROR;
		}

		onoff = *((guint32*) input);

		ret = setsockopt (sock, SOL_SOCKET, SO_KEEPALIVE, &onoff, sizeof (guint32));
		if (ret < 0) {
			mono_w32socket_set_last_error (mono_w32socket_convert_error (errno));
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
			ret = setsockopt (sock, IPPROTO_TCP, TCP_KEEPIDLE, &keepalivetime, sizeof (guint32));
			if (ret == 0) {
				rem = keepaliveinterval % 1000;
				keepaliveinterval /= 1000;
				if (keepaliveinterval == 0 || rem >= 500)
					keepaliveinterval++;
				ret = setsockopt (sock, IPPROTO_TCP, TCP_KEEPINTVL, &keepaliveinterval, sizeof (guint32));
			}
			if (ret != 0) {
				mono_w32socket_set_last_error (mono_w32socket_convert_error (errno));
				return SOCKET_ERROR;
			}

			return 0;
		}
#endif

		return 0;
	}

	buffer = inputlen > 0 ? (gchar*) g_memdup (input, inputlen) : NULL;

	ret = ioctl (sock, command, buffer);
	if (ret == -1) {
		g_free (buffer);

		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: WSAIoctl error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	if (!buffer) {
		*written = 0;
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

	return 0;
}

gboolean
mono_w32socket_close (SOCKET sock)
{
	return mono_w32handle_close (GINT_TO_POINTER (sock));
}

gint
mono_w32socket_set_blocking (SOCKET socket, gboolean blocking)
{
#ifdef O_NONBLOCK
	gint ret;
	gpointer handle;

	handle = GINT_TO_POINTER (socket);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	/* This works better than ioctl(...FIONBIO...)
	 * on Linux (it causes connect to return
	 * EINPROGRESS, but the ioctl doesn't seem to) */
	ret = fcntl (socket, F_GETFL, 0);
	if (ret != -1)
		ret = fcntl (socket, F_SETFL, blocking ? (ret & (~O_NONBLOCK)) : (ret | (O_NONBLOCK)));

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: ioctl error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

	return 0;
#else
	mono_w32socket_set_last_error (ERROR_NOT_SUPPORTED);
	return SOCKET_ERROR;
#endif /* O_NONBLOCK */
}

gint
mono_w32socket_get_available (SOCKET socket, guint64 *amount)
{
	gint ret;
	gpointer handle;

	handle = GINT_TO_POINTER (socket);
	if (mono_w32handle_get_type (handle) != MONO_W32HANDLE_SOCKET) {
		mono_w32socket_set_last_error (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

#if defined (PLATFORM_MACOSX)
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
	ret = getsockopt (socket, SOL_SOCKET, SO_NREAD, (gulong*) amount, &optlen);
#else
	ret = ioctl (socket, FIONREAD, (gulong*) amount);
#endif

	if (ret == -1) {
		gint errnum = errno;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: ioctl error: %s", __func__, g_strerror (errno));
		mono_w32socket_set_last_error (mono_w32socket_convert_error (errnum));
		return SOCKET_ERROR;
	}

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
	case EFAULT: return WSAEFAULT;
#ifdef EHOSTUNREACH
	case EHOSTUNREACH: return WSAEHOSTUNREACH;
#endif
#ifdef EINPROGRESS
	case EINPROGRESS: return WSAEINPROGRESS;
#endif
	case EINTR: return WSAEINTR;
	case EINVAL: return WSAEINVAL;
	/*FIXME: case EIO: return WSAE????; */
#ifdef EISCONN
	case EISCONN: return WSAEISCONN;
#endif
	/* FIXME: case ELOOP: return WSA????; */
	case EMFILE: return WSAEMFILE;
#ifdef EMSGSIZE
	case EMSGSIZE: return WSAEMSGSIZE;
#endif
	/* FIXME: case ENAMETOOLONG: return WSAEACCES; */
#ifdef ENETUNREACH
	case ENETUNREACH: return WSAENETUNREACH;
#endif
#ifdef ENOBUFS
	case ENOBUFS: return WSAENOBUFS; /* not documented */
#endif
	/* case ENOENT: return WSAE????; */
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
	/*FIXME: case ENOTDIR: return WSAE????; */
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
	default:
		g_error ("%s: no translation into winsock error for (%d) \"%s\"", __func__, error, g_strerror (error));
	}
}

gboolean
ves_icall_System_Net_Sockets_Socket_SupportPortReuse (MonoProtocolType proto)
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
