/*
 * sockets.c:  Socket handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/ioctl.h>
#include <sys/poll.h>
#ifdef HAVE_SYS_FILIO_H
#include <sys/filio.h>     /* defines FIONBIO and FIONREAD */
#endif
#ifdef HAVE_SYS_SOCKIO_H
#include <sys/sockio.h>    /* defines SIOCATMARK */
#endif
#include <unistd.h>
#include <fcntl.h>

#ifndef HAVE_MSG_NOSIGNAL
#include <signal.h>
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/socket-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/socket-wrappers.h>

#undef DEBUG

static guint32 startup_count=0;

static void socket_close (gpointer handle, gpointer data);

struct _WapiHandleOps _wapi_socket_ops = {
	socket_close,		/* close */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

static mono_once_t socket_ops_once=MONO_ONCE_INIT;

static void socket_ops_init (void)
{
	/* No capabilities to register */
}

static void socket_close (gpointer handle, gpointer data)
{
	int ret;
	struct _WapiHandle_socket *socket_handle = (struct _WapiHandle_socket *)data;

#ifdef DEBUG
	g_message ("%s: closing socket handle %p", __func__, handle);
#endif

	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return;
	}

	/* Shutdown the socket for reading, to interrupt any potential
	 * receives that may be blocking for data.  See bug 75705.
	 */
	shutdown (GPOINTER_TO_UINT (handle), SHUT_RD);
	
	do {
		ret = close (GPOINTER_TO_UINT(handle));
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());
	
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: close error: %s", __func__, strerror (errno));
#endif
		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
	}

	socket_handle->saved_error = 0;
}

int WSAStartup(guint32 requested, WapiWSAData *data)
{
	if (data == NULL) {
		return(WSAEFAULT);
	}

	/* Insist on v2.0+ */
	if (requested < MAKEWORD(2,0)) {
		return(WSAVERNOTSUPPORTED);
	}

	startup_count++;

	/* I've no idea what is the minor version of the spec I read */
	data->wHighVersion = MAKEWORD(2,2);
	
	data->wVersion = requested < data->wHighVersion? requested:
		data->wHighVersion;

#ifdef DEBUG
	g_message ("%s: high version 0x%x", __func__, data->wHighVersion);
#endif
	
	strncpy (data->szDescription, "WAPI", WSADESCRIPTION_LEN);
	strncpy (data->szSystemStatus, "groovy", WSASYS_STATUS_LEN);
	
	return(0);
}

static gboolean
cleanup_close (gpointer handle, gpointer data)
{
	_wapi_handle_ops_close (handle, NULL);
	return TRUE;
}

int WSACleanup(void)
{
#ifdef DEBUG
	g_message ("%s: cleaning up", __func__);
#endif

	if (--startup_count) {
		/* Do nothing */
		return(0);
	}

	_wapi_handle_foreach (WAPI_HANDLE_SOCKET, cleanup_close, NULL);
	return(0);
}

void WSASetLastError(int error)
{
	SetLastError (error);
}

int WSAGetLastError(void)
{
	return(GetLastError ());
}

int closesocket(guint32 fd)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(0);
	}
	
	_wapi_handle_unref (handle);
	return(0);
}

guint32 _wapi_accept(guint32 fd, struct sockaddr *addr, socklen_t *addrlen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	gpointer new_handle;
	struct _WapiHandle_socket *socket_handle;
	struct _WapiHandle_socket new_socket_handle = {0};
	gboolean ok;
	int new_fd;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(INVALID_SOCKET);
	}

	if (addr != NULL && *addrlen < sizeof(struct sockaddr)) {
		WSASetLastError (WSAEFAULT);
		return(INVALID_SOCKET);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(INVALID_SOCKET);
	}
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET,
				  (gpointer *)&socket_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up socket handle %p",
			   __func__, handle);
		WSASetLastError (WSAENOTSOCK);
		return(INVALID_SOCKET);
	}
	
	do {
		new_fd = accept (fd, addr, addrlen);
	} while (new_fd == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending());

	if (new_fd == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: accept error: %s", __func__, strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(INVALID_SOCKET);
	}

	if (new_fd >= _wapi_fd_reserve) {
#ifdef DEBUG
		g_message ("%s: File descriptor is too big", __func__);
#endif

		WSASetLastError (WSASYSCALLFAILURE);
		
		close (new_fd);
		
		return(INVALID_SOCKET);
	}

	new_socket_handle.domain = socket_handle->domain;
	new_socket_handle.type = socket_handle->type;
	new_socket_handle.protocol = socket_handle->protocol;
	new_socket_handle.still_readable = 1;

	new_handle = _wapi_handle_new_fd (WAPI_HANDLE_SOCKET, new_fd,
					  &new_socket_handle);
	if(new_handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating socket handle", __func__);
		WSASetLastError (ERROR_GEN_FAILURE);
		return(INVALID_SOCKET);
	}

#ifdef DEBUG
	g_message ("%s: returning newly accepted socket handle %p with",
		   __func__, new_handle);
#endif
	
	return(new_fd);
}

int _wapi_bind(guint32 fd, struct sockaddr *my_addr, socklen_t addrlen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}

	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret = bind (fd, my_addr, addrlen);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: bind error: %s", __func__, strerror(errno));
#endif
		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_connect(guint32 fd, const struct sockaddr *serv_addr,
		  socklen_t addrlen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	struct _WapiHandle_socket *socket_handle;
	gboolean ok;
	gint errnum;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	if (connect (fd, serv_addr, addrlen) == -1) {
		struct pollfd fds;
		int so_error;
		socklen_t len;
		
		errnum = errno;
		
		if (errno != EINTR) {
#ifdef DEBUG
			g_message ("%s: connect error: %s", __func__,
				   strerror (errnum));
#endif

			errnum = errno_to_WSA (errnum, __func__);
			if (errnum == WSAEINPROGRESS)
				errnum = WSAEWOULDBLOCK; /* see bug #73053 */

			WSASetLastError (errnum);

			/* 
			 * On solaris x86 getsockopt (SO_ERROR) is not set after 
			 * connect () fails so we need to save this error.
			 *
			 * But don't do this for EWOULDBLOCK (bug 317315)
			 */
			if (errnum != WSAEWOULDBLOCK) {
				ok = _wapi_lookup_handle (handle,
							  WAPI_HANDLE_SOCKET,
							  (gpointer *)&socket_handle);
				if (ok == FALSE) {
					g_warning ("%s: error looking up socket handle %p", __func__, handle);
				} else {
					socket_handle->saved_error = errnum;
				}
			}
			return(SOCKET_ERROR);
		}

		fds.fd = fd;
		fds.events = POLLOUT;
		while (poll (&fds, 1, -1) == -1 &&
		       !_wapi_thread_cur_apc_pending ()) {
			if (errno != EINTR) {
				errnum = errno_to_WSA (errno, __func__);

#ifdef DEBUG
				g_message ("%s: connect poll error: %s",
					   __func__, strerror (errno));
#endif

				WSASetLastError (errnum);
				return(SOCKET_ERROR);
			}
		}

		len = sizeof(so_error);
		if (getsockopt (fd, SOL_SOCKET, SO_ERROR, &so_error,
				&len) == -1) {
			errnum = errno_to_WSA (errno, __func__);

#ifdef DEBUG
			g_message ("%s: connect getsockopt error: %s",
				   __func__, strerror (errno));
#endif

			WSASetLastError (errnum);
			return(SOCKET_ERROR);
		}
		
		if (so_error != 0) {
			errnum = errno_to_WSA (so_error, __func__);

			/* Need to save this socket error */
			ok = _wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET,
						  (gpointer *)&socket_handle);
			if (ok == FALSE) {
				g_warning ("%s: error looking up socket handle %p", __func__, handle);
			} else {
				socket_handle->saved_error = errnum;
			}
			
#ifdef DEBUG
			g_message ("%s: connect getsockopt returned error: %s",
				   __func__, strerror (so_error));
#endif

			WSASetLastError (errnum);
			return(SOCKET_ERROR);
		}
	}
		
	return(0);
}

int _wapi_getpeername(guint32 fd, struct sockaddr *name, socklen_t *namelen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	ret = getpeername (fd, name, namelen);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: getpeername error: %s", __func__,
			   strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);

		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_getsockname(guint32 fd, struct sockaddr *name, socklen_t *namelen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	ret = getsockname (fd, name, namelen);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: getsockname error: %s", __func__,
			   strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);

		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_getsockopt(guint32 fd, int level, int optname, void *optval,
		     socklen_t *optlen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	struct timeval tv;
	void *tmp_val;
	struct _WapiHandle_socket *socket_handle;
	gboolean ok;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	tmp_val = optval;
	if (level == SOL_SOCKET &&
	    (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		tmp_val = &tv;
		*optlen = sizeof (tv);
	}

	ret = getsockopt (fd, level, optname, tmp_val, optlen);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: getsockopt error: %s", __func__,
			   strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}

	if (level == SOL_SOCKET &&
	    (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		*((int *) optval)  = tv.tv_sec * 1000 + (tv.tv_usec / 1000);	// milli from micro
		*optlen = sizeof (int);
	}

	if (optname == SO_ERROR) {
		ok = _wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET,
					  (gpointer *)&socket_handle);
		if (ok == FALSE) {
			g_warning ("%s: error looking up socket handle %p",
				   __func__, handle);

			/* can't extract the last error */
			*((int *) optval) = errno_to_WSA (*((int *)optval),
							  __func__);
		} else {
			if (*((int *)optval) != 0) {
				*((int *) optval) = errno_to_WSA (*((int *)optval),
								  __func__);
				socket_handle->saved_error = *((int *)optval);
			} else {
				*((int *)optval) = socket_handle->saved_error;
			}
		}
	}
	
	return(ret);
}

int _wapi_listen(guint32 fd, int backlog)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret = listen (fd, backlog);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: listen error: %s", __func__, strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);

		return(SOCKET_ERROR);
	}

	return(0);
}

int _wapi_recv(guint32 fd, void *buf, size_t len, int recv_flags)
{
	return(_wapi_recvfrom (fd, buf, len, recv_flags, NULL, 0));
}

int _wapi_recvfrom(guint32 fd, void *buf, size_t len, int recv_flags,
		   struct sockaddr *from, socklen_t *fromlen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	struct _WapiHandle_socket *socket_handle;
	gboolean ok;
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	do {
		ret = recvfrom (fd, buf, len, recv_flags, from, fromlen);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());

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
		ok = _wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET,
					  (gpointer *)&socket_handle);
		if (ok == FALSE || socket_handle->still_readable != 1) {
			ret = -1;
			errno = EINTR;
		}
	}
	
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: recv error: %s", __func__, strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_send(guint32 fd, const void *msg, size_t len, int send_flags)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	do {
		ret = send (fd, msg, len, send_flags);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());

	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: send error: %s", __func__, strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_sendto(guint32 fd, const void *msg, size_t len, int send_flags,
		 const struct sockaddr *to, socklen_t tolen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	do {
		ret = sendto (fd, msg, len, send_flags, to, tolen);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());

	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: send error: %s", __func__, strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_setsockopt(guint32 fd, int level, int optname,
		     const void *optval, socklen_t optlen)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	const void *tmp_val;
	struct timeval tv;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	tmp_val = optval;
	if (level == SOL_SOCKET &&
	    (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO)) {
		int ms = *((int *) optval);
		tv.tv_sec = ms / 1000;
		tv.tv_usec = (ms % 1000) * 1000;	// micro from milli
		tmp_val = &tv;
		optlen = sizeof (tv);
#if defined (__linux__)
	} else if (level == SOL_SOCKET &&
		   (optname == SO_SNDBUF || optname == SO_RCVBUF)) {
		/* According to socket(7) the Linux kernel doubles the
		 * buffer sizes "to allow space for bookkeeping
		 * overhead."
		 */
		int bufsize = *((int *) optval);

		bufsize /= 2;
		tmp_val = &bufsize;
#endif
	}
		
	ret = setsockopt (fd, level, optname, tmp_val, optlen);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: setsockopt error: %s", __func__,
			   strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}

#if defined(__FreeBSD__)
	/* FreeBSD's multicast sockets also need SO_REUSEPORT when SO_REUSEADDR is requested.  */
	if (level == SOL_SOCKET && optname == SO_REUSEADDR) {
		int type;
		int type_len = sizeof (type);

		if (!getsockopt (fd, level, SO_TYPE, &type, &type_len)) {
			if (type == SOCK_DGRAM)
				setsockopt (fd, level, SO_REUSEPORT, tmp_val, optlen);
		}
	}
#endif
	
	return(ret);
}

int _wapi_shutdown(guint32 fd, int how)
{
	struct _WapiHandle_socket *socket_handle;
	gboolean ok;
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	if (how == SHUT_RD ||
	    how == SHUT_RDWR) {
		ok = _wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET,
					  (gpointer *)&socket_handle);
		if (ok == FALSE) {
			g_warning ("%s: error looking up socket handle %p",
				   __func__, handle);
			WSASetLastError (WSAENOTSOCK);
			return(SOCKET_ERROR);
		}
		
		socket_handle->still_readable = 0;
	}
	
	ret = shutdown (fd, how);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: shutdown error: %s", __func__,
			   strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	
	return(ret);
}

guint32 _wapi_socket(int domain, int type, int protocol, void *unused,
		     guint32 unused2, guint32 unused3)
{
	struct _WapiHandle_socket socket_handle = {0};
	gpointer handle;
	int fd;
	
	socket_handle.domain = domain;
	socket_handle.type = type;
	socket_handle.protocol = protocol;
	socket_handle.still_readable = 1;
	
	fd = socket (domain, type, protocol);
	if (fd == -1 && domain == AF_INET && type == SOCK_RAW &&
	    protocol == 0) {
		/* Retry with protocol == 4 (see bug #54565) */
		socket_handle.protocol = 4;
		fd = socket (AF_INET, SOCK_RAW, 4);
	}
	
	if (fd == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: socket error: %s", __func__, strerror (errno));
#endif
		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);

		return(INVALID_SOCKET);
	}

	if (fd >= _wapi_fd_reserve) {
#ifdef DEBUG
		g_message ("%s: File descriptor is too big (%d >= %d)",
			   __func__, fd, _wapi_fd_reserve);
#endif

		WSASetLastError (WSASYSCALLFAILURE);
		close (fd);
		
		return(INVALID_SOCKET);
	}

	/* .net seems to set this by default for SOCK_STREAM, not for
	 * SOCK_DGRAM (see bug #36322)
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
	 */
	{
		int ret, true = 1;
	
		ret = setsockopt (fd, SOL_SOCKET, SO_REUSEADDR, &true,
				  sizeof (true));
		if (ret == -1) {
			int errnum = errno;

#ifdef DEBUG
			g_message ("%s: Error setting SO_REUSEADDR", __func__);
#endif
			
			errnum = errno_to_WSA (errnum, __func__);
			WSASetLastError (errnum);

			close (fd);

			return(INVALID_SOCKET);			
		}
	}
	
	
	mono_once (&socket_ops_once, socket_ops_init);
	
	handle = _wapi_handle_new_fd (WAPI_HANDLE_SOCKET, fd, &socket_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating socket handle", __func__);
		WSASetLastError (WSASYSCALLFAILURE);
		close (fd);
		return(INVALID_SOCKET);
	}

#ifdef DEBUG
	g_message ("%s: returning socket handle %p", __func__, handle);
#endif

	return(fd);
}

struct hostent *_wapi_gethostbyname(const char *hostname)
{
	struct hostent *he;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(NULL);
	}

	he = gethostbyname (hostname);
	if (he == NULL) {
#ifdef DEBUG
		g_message ("%s: gethostbyname error: %s", __func__,
			   strerror (h_errno));
#endif

		switch(h_errno) {
		case HOST_NOT_FOUND:
			WSASetLastError (WSAHOST_NOT_FOUND);
			break;
#if NO_ADDRESS != NO_DATA
		case NO_ADDRESS:
#endif
		case NO_DATA:
			WSASetLastError (WSANO_DATA);
			break;
		case NO_RECOVERY:
			WSASetLastError (WSANO_RECOVERY);
			break;
		case TRY_AGAIN:
			WSASetLastError (WSATRY_AGAIN);
			break;
		default:
			g_warning ("%s: Need to translate %d into winsock error", __func__, h_errno);
			break;
		}
	}
	
	return(he);
}

static gboolean socket_disconnect (guint32 fd)
{
	struct _WapiHandle_socket *socket_handle;
	gboolean ok;
	gpointer handle = GUINT_TO_POINTER (fd);
	int newsock, ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET,
				  (gpointer *)&socket_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up socket handle %p", __func__,
			   handle);
		WSASetLastError (WSAENOTSOCK);
		return(FALSE);
	}
	
	newsock = socket (socket_handle->domain, socket_handle->type,
			  socket_handle->protocol);
	if (newsock == -1) {
		gint errnum = errno;

#ifdef DEBUG
		g_message ("%s: socket error: %s", __func__, strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(FALSE);
	}

	/* According to Stevens "Advanced Programming in the UNIX
	 * Environment: UNIX File I/O" dup2() is atomic so there
	 * should not be a race condition between the old fd being
	 * closed and the new socket fd being copied over
	 */
	do {
		ret = dup2 (newsock, fd);
	} while (ret == -1 && errno == EAGAIN);
	
	if (ret == -1) {
		gint errnum = errno;
		
#ifdef DEBUG
		g_message ("%s: dup2 error: %s", __func__, strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(FALSE);
	}

	close (newsock);
	
	return(TRUE);
}

static gboolean wapi_disconnectex (guint32 fd, WapiOverlapped *overlapped,
				   guint32 flags, guint32 reserved)
{
#ifdef DEBUG
	g_message ("%s: called on socket %d!", __func__, fd);
#endif
	
	if (reserved != 0) {
		WSASetLastError (WSAEINVAL);
		return(FALSE);
	}

	/* We could check the socket type here and fail unless its
	 * SOCK_STREAM, SOCK_SEQPACKET or SOCK_RDM (according to msdn)
	 * if we really wanted to
	 */

	return(socket_disconnect (fd));
}

/* NB only supports NULL file handle, NULL buffers and
 * TF_DISCONNECT|TF_REUSE_SOCKET flags to disconnect the socket fd.
 * Shouldn't actually ever need to be called anyway though, because we
 * have DisconnectEx ().
 */
static gboolean wapi_transmitfile (guint32 fd, gpointer file,
				   guint32 num_write, guint32 num_per_send,
				   WapiOverlapped *overlapped,
				   WapiTransmitFileBuffers *buffers,
				   WapiTransmitFileFlags flags)
{
#ifdef DEBUG
	g_message ("%s: called on socket %d!", __func__, fd);
#endif
	
	g_assert (file == NULL);
	g_assert (overlapped == NULL);
	g_assert (buffers == NULL);
	g_assert (num_write == 0);
	g_assert (num_per_send == 0);
	g_assert (flags == (TF_DISCONNECT | TF_REUSE_SOCKET));

	return(socket_disconnect (fd));
}

static struct 
{
	WapiGuid guid;
	gpointer func;
} extension_functions[] = {
	{WSAID_DISCONNECTEX, wapi_disconnectex},
	{WSAID_TRANSMITFILE, wapi_transmitfile},
	{{0}, NULL},
};

int
WSAIoctl (guint32 fd, gint32 command,
	  gchar *input, gint i_len,
	  gchar *output, gint o_len, glong *written,
	  void *unused1, void *unused2)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	gchar *buffer = NULL;

	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}

	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (command == SIO_GET_EXTENSION_FUNCTION_POINTER) {
		int i = 0;
		WapiGuid *guid = (WapiGuid *)input;
		
		if (i_len < sizeof(WapiGuid)) {
			/* As far as I can tell, windows doesn't
			 * actually set an error here...
			 */
			WSASetLastError (WSAEINVAL);
			return(SOCKET_ERROR);
		}

		if (o_len < sizeof(gpointer)) {
			/* Or here... */
			WSASetLastError (WSAEINVAL);
			return(SOCKET_ERROR);
		}

		if (output == NULL) {
			/* Or here */
			WSASetLastError (WSAEINVAL);
			return(SOCKET_ERROR);
		}
		
		while(extension_functions[i].func != NULL) {
			if (!memcmp (guid, &extension_functions[i].guid,
				     sizeof(WapiGuid))) {
				memcpy (output, &extension_functions[i].func,
					sizeof(gpointer));
				*written = sizeof(gpointer);
				return(0);
			}

			i++;
		}
		
		WSASetLastError (WSAEINVAL);
		return(SOCKET_ERROR);
	}

	if (i_len > 0) {
		buffer = g_memdup (input, i_len);
	}

	ret = ioctl (fd, command, buffer);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message("%s: WSAIoctl error: %s", __func__,
			  strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		g_free (buffer);
		
		return(SOCKET_ERROR);
	}

	if (buffer == NULL) {
		*written = 0;
	} else {
		/* We just copy the buffer to the output. Some ioctls
		 * don't even output any data, but, well...
		 *
		 * NB windows returns WSAEFAULT if o_len is too small
		 */
		i_len = (i_len > o_len) ? o_len : i_len;

		if (i_len > 0 && output != NULL) {
			memcpy (output, buffer, i_len);
		}
		
		g_free (buffer);
		*written = i_len;
	}

	return(0);
}

int ioctlsocket(guint32 fd, gint32 command, gpointer arg)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	switch(command){
		case FIONBIO:
#ifdef O_NONBLOCK
			/* This works better than ioctl(...FIONBIO...) 
			 * on Linux (it causes connect to return
			 * EINPROGRESS, but the ioctl doesn't seem to)
			 */
			ret = fcntl(fd, F_GETFL, 0);
			if (ret != -1) {
				if (*(gboolean *)arg) {
					ret |= O_NONBLOCK;
				} else {
					ret &= ~O_NONBLOCK;
				}
				ret = fcntl(fd, F_SETFL, ret);
			}
			break;
#endif /* O_NONBLOCK */
		case FIONREAD:
		case SIOCATMARK:
			ret = ioctl (fd, command, arg);
			break;
		default:
			WSASetLastError (WSAEINVAL);
			return(SOCKET_ERROR);
	}

	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: ioctl error: %s", __func__, strerror (errno));
#endif

		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	
	return(0);
}

int _wapi_select(int nfds G_GNUC_UNUSED, fd_set *readfds, fd_set *writefds,
		 fd_set *exceptfds, struct timeval *timeout)
{
	int ret, maxfd;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}

	for (maxfd = FD_SETSIZE-1; maxfd >= 0; maxfd--) {
		if ((readfds && FD_ISSET (maxfd, readfds)) ||
		    (writefds && FD_ISSET (maxfd, writefds)) ||
		    (exceptfds && FD_ISSET (maxfd, exceptfds))) {
			break;
		}
	}

	if (maxfd == -1) {
		WSASetLastError (WSAEINVAL);
		return(SOCKET_ERROR);
	}

	do {
		ret = select(maxfd + 1, readfds, writefds, exceptfds,
			     timeout);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());

	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message ("%s: select error: %s", __func__, strerror (errno));
#endif
		errnum = errno_to_WSA (errnum, __func__);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}

	return(ret);
}

void _wapi_FD_CLR(guint32 fd, fd_set *set)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	
	if (fd >= FD_SETSIZE) {
		WSASetLastError (WSAEINVAL);
		return;
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return;
	}

	FD_CLR (fd, set);
}

int _wapi_FD_ISSET(guint32 fd, fd_set *set)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	
	if (fd >= FD_SETSIZE) {
		WSASetLastError (WSAEINVAL);
		return(0);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(0);
	}

	return(FD_ISSET (fd, set));
}

void _wapi_FD_SET(guint32 fd, fd_set *set)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	
	if (fd >= FD_SETSIZE) {
		WSASetLastError (WSAEINVAL);
		return;
	}

	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return;
	}

	FD_SET (fd, set);
}

