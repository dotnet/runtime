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
static pthread_key_t error_key;
static mono_once_t error_key_once=MONO_ONCE_INIT;

static void socket_close (gpointer handle, gpointer data);

struct _WapiHandleOps _wapi_socket_ops = {
	socket_close,		/* close */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
};

static mono_once_t socket_ops_once=MONO_ONCE_INIT;

static void socket_ops_init (void)
{
	/* No capabilities to register */
}

static void socket_close (gpointer handle, gpointer data G_GNUC_UNUSED)
{
	int ret;

#ifdef DEBUG
	g_message ("%s: closing socket handle %p", __func__, handle);
#endif

	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return;
	}

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
	data->wHighVersion = MAKEWORD(2,0);
	
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

static void error_init(void)
{
	int ret;
	
	ret = pthread_key_create (&error_key, NULL);
	g_assert (ret == 0);
}

void WSASetLastError(int error)
{
	int ret;
	
	mono_once (&error_key_once, error_init);
	ret = pthread_setspecific (error_key, GINT_TO_POINTER(error));
	g_assert (ret == 0);
}

int WSAGetLastError(void)
{
	int err;
	void *errptr;
	
	mono_once (&error_key_once, error_init);
	errptr = pthread_getspecific (error_key);
	err = GPOINTER_TO_INT(errptr);
	
	return(err);
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
	int new_fd;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(INVALID_SOCKET);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
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

	new_handle = _wapi_handle_new_fd (WAPI_HANDLE_SOCKET, new_fd, NULL);
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
	int ret;
	gint errnum;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	do {
		ret = connect (fd, serv_addr, addrlen);
	} while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());

	if (ret == -1 && errno == EACCES) {
		/* Try setting SO_BROADCAST and connecting again, but
		 * keep the original errno
		 */
		int true=1;
		
		errnum = errno;

		ret = setsockopt (fd, SOL_SOCKET, SO_BROADCAST, &true,
				  sizeof(true));
		if (ret == 0) {
			do {
				ret = connect (fd, serv_addr, addrlen);
			} while (ret==-1 && errno==EINTR &&
				 !_wapi_thread_cur_apc_pending());
		}
	} else if (ret == -1) {
		errnum = errno;
	}
	
	if (ret == -1) {
#ifdef DEBUG
		g_message ("%s: connect error: %s", __func__,
			   strerror (errnum));
#endif
		errnum = errno_to_WSA (errnum, __func__);
		if (errnum == WSAEINPROGRESS)
			errnum = WSAEWOULDBLOCK; /* see bug #73053 */

		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
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

	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	tmp_val = optval;
	if (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO) {
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

	if (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO) {
		*((int *) optval)  = tv.tv_sec * 1000 + tv.tv_usec;
		*optlen = sizeof (int);
	}

	if (optname == SO_ERROR) {
		if (*((int *)optval) != 0) {
			*((int *) optval) = errno_to_WSA (*((int *)optval),
							  __func__);
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
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);	// old SIGPIPE handler
#endif
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
	
#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret = recvfrom (fd, buf, len, recv_flags | MSG_NOSIGNAL, from,
				fromlen);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	do {
		ret = recvfrom (fd, buf, len, recv_flags, from, fromlen);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());
	signal (SIGPIPE, old_sigpipe);
#endif

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
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);	// old SIGPIPE handler
#endif
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

#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret = send (fd, msg, len, send_flags | MSG_NOSIGNAL);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	do {
		ret = send (fd, msg, len, send_flags);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());
	signal (SIGPIPE, old_sigpipe);
#endif
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
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);	// old SIGPIPE handler
#endif
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
	
#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret = sendto (fd, msg, len, send_flags | MSG_NOSIGNAL, to,
			      tolen);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	do {
		ret = sendto (fd, msg, len, send_flags, to, tolen);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending ());
	signal (SIGPIPE, old_sigpipe);
#endif
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
	if (optname == SO_RCVTIMEO || optname == SO_SNDTIMEO) {
		int ms = *((int *) optval);
		tv.tv_sec = ms / 1000;
		tv.tv_usec = ms % 1000;
		tmp_val = &tv;
		optlen = sizeof (tv);
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
	
	return(ret);
}

int _wapi_shutdown(guint32 fd, int how)
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
	gpointer handle;
	int fd;
	
	fd = socket (domain, type, protocol);
	if (fd == -1 && domain == AF_INET && type == SOCK_RAW &&
	    protocol == 0) {
		/* Retry with protocol == 4 (see bug #54565) */
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
		g_message ("%s: File descriptor is too big", __func__);
#endif

		WSASetLastError (WSASYSCALLFAILURE);
		close (fd);
		
		return(INVALID_SOCKET);
	}
	
	
	mono_once (&socket_ops_once, socket_ops_init);
	
	handle = _wapi_handle_new_fd (WAPI_HANDLE_SOCKET, fd, NULL);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating socket handle", __func__);
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
		 */
		i_len = (i_len > o_len) ? o_len : i_len;
		memcpy (output, buffer, i_len);
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

	if (command != FIONBIO &&
	    command != FIONREAD &&
	    command != SIOCATMARK) {
		/* Not listed in the MSDN specs, but ioctl(2) returns
		 * this if command is invalid
		 */
		WSASetLastError (WSAEINVAL);
		return(SOCKET_ERROR);
	}

#ifdef O_NONBLOCK
	/* This works better than ioctl(...FIONBIO...) on Linux (it causes
	 * connect to return EINPROGRESS, but the ioctl doesn't seem to)
	 */
	if (command == FIONBIO) {
		ret = fcntl (fd, F_GETFL, 0);
		if (ret != -1) {
			if (*(gboolean *)arg) {
				ret |= O_NONBLOCK;
			} else {
				ret &= ~O_NONBLOCK;
			}
			ret = fcntl (fd, F_SETFL, ret);
		}
	} else
#endif /* O_NONBLOCK */
	{
		ret = ioctl (fd, command, arg);
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
	int ret;
	
	if (startup_count == 0) {
		WSASetLastError (WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}

	do {
		ret = select(getdtablesize (), readfds, writefds, exceptfds,
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
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return;
	}

	FD_CLR (fd, set);
}

int _wapi_FD_ISSET(guint32 fd, fd_set *set)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(0);
	}

	return(FD_ISSET (fd, set));
}

void _wapi_FD_SET(guint32 fd, fd_set *set)
{
	gpointer handle = GUINT_TO_POINTER (fd);
	
	if (_wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return;
	}

	FD_SET (fd, set);
}

