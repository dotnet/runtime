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

#ifndef PLATFORM_WIN32
#ifdef HAVE_AIO_H
#include <aio.h>
#define USE_AIO	1
#elif defined(HAVE_SYS_AIO_H)
#include <sys/aio.h>
#define USE_AIO 1
#else
#undef USE_AIO
#endif
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/socket-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/socket-wrappers.h>

#undef DEBUG

static guint32 startup_count=0;
static GPtrArray *sockets=NULL;
static pthread_key_t error_key;
static mono_once_t error_key_once=MONO_ONCE_INIT;

static void socket_close_private (gpointer handle);

struct _WapiHandleOps _wapi_socket_ops = {
	NULL,			/* close_shared */
	socket_close_private,	/* close_private */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
};

static mono_once_t socket_ops_once=MONO_ONCE_INIT;

static void socket_ops_init (void)
{
	/* No capabilities to register */
}

static void socket_close_private (gpointer handle)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing socket handle %p",
		  handle);
#endif

	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return;
	}

	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle %p", handle);
		WSASetLastError(WSAENOTSOCK);
		return;
	}

	g_ptr_array_remove_fast(sockets, GUINT_TO_POINTER (handle));

	if (socket_private_handle->fd_mapped.assigned == TRUE) {
		/* Blank out the mapping, to make catching errors easier */
		_wapi_handle_fd_offset_store (socket_private_handle->fd_mapped.fd, NULL);

		do {
			ret=close(socket_private_handle->fd_mapped.fd);
		}
		while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
	
		if(ret==-1) {
			gint errnum = errno;
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION ": close error: %s",
				  strerror(errno));
#endif
			errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
			WSASetLastError (errnum);

			return;
		}
	} else {
		WSASetLastError(WSAENOTSOCK);
	}
}

int WSAStartup(guint32 requested, WapiWSAData *data)
{
	if(data==NULL) {
		return(WSAEFAULT);
	}

	/* Insist on v2.0+ */
	if(requested < MAKEWORD(2,0)) {
		return(WSAVERNOTSUPPORTED);
	}

	if(startup_count==0) {
		sockets=g_ptr_array_new();
	}
	
	startup_count++;

	/* I've no idea what is the minor version of the spec I read */
	data->wHighVersion=MAKEWORD(2,0);
	
	data->wVersion=requested < data->wHighVersion? requested:
		data->wHighVersion;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": high version 0x%x",
		  data->wHighVersion);
#endif
	
	strncpy(data->szDescription, "WAPI", WSADESCRIPTION_LEN);
	strncpy(data->szSystemStatus, "groovy", WSASYS_STATUS_LEN);
	
	return(0);
}

int WSACleanup(void)
{
	guint32 i;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": cleaning up");
#endif

	if(--startup_count) {
		/* Do nothing */
		return(0);
	}
	
	/* Close down all sockets */
	for(i=0; i<sockets->len; i++) {
		gpointer handle;

		handle=g_ptr_array_index(sockets, i);
		_wapi_handle_ops_close_private (handle);
	}

	g_ptr_array_free(sockets, FALSE);
	sockets=NULL;
	
	return(0);
}

static void error_init(void)
{
	int ret;
	
	ret = pthread_key_create(&error_key, NULL);
	g_assert (ret == 0);
}

void WSASetLastError(int error)
{
	int ret;
	
	mono_once(&error_key_once, error_init);
	ret = pthread_setspecific(error_key, GINT_TO_POINTER(error));
	g_assert (ret == 0);
}

int WSAGetLastError(void)
{
	int err;
	void *errptr;
	
	mono_once(&error_key_once, error_init);
	errptr=pthread_getspecific(error_key);
	err=GPOINTER_TO_INT(errptr);
	
	return(err);
}

int closesocket(guint32 fd_handle)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd_handle));
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return(0);
	}
	
	_wapi_handle_unref (handle);
	return(0);
}

guint32 _wapi_accept(guint32 fd, struct sockaddr *addr, socklen_t *addrlen)
{
	struct _WapiHandlePrivate_socket *new_socket_private_handle;
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	gpointer new_handle;
	gboolean ok;
	int new_fd;
	int thr_ret;
	guint32 ret = INVALID_SOCKET;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(INVALID_SOCKET);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(INVALID_SOCKET);
	}
	
	do {
		new_fd=accept(fd, addr, addrlen);
	}
	while (new_fd==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());

	if(new_fd==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": accept error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(INVALID_SOCKET);
	}

	if (new_fd >= _wapi_fd_offset_table_size) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": File descriptor is too big");
#endif

		WSASetLastError (WSASYSCALLFAILURE);
		
		close (new_fd);
		
		return(INVALID_SOCKET);
	}

	new_handle=_wapi_handle_new (WAPI_HANDLE_SOCKET);
	if(new_handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating socket handle");
		WSASetLastError (ERROR_GEN_FAILURE);
		return(INVALID_SOCKET);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      new_handle);
	thr_ret = _wapi_handle_lock_handle (new_handle);
	g_assert (thr_ret == 0);
	
	ok=_wapi_lookup_handle (new_handle, WAPI_HANDLE_SOCKET, NULL,
				(gpointer *)&new_socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up new socket handle %p",
			   new_handle);
		goto cleanup;
	}

	_wapi_handle_fd_offset_store (new_fd, new_handle);
	ret = new_fd;
	
	new_socket_private_handle->fd_mapped.fd = new_fd;
	new_socket_private_handle->fd_mapped.assigned = TRUE;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": returning newly accepted socket handle %p with fd %d",
		  new_handle, new_socket_private_handle->fd_mapped.fd);
#endif

cleanup:
	thr_ret = _wapi_handle_unlock_handle (new_handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(ret);
}

int _wapi_bind(guint32 fd, struct sockaddr *my_addr, socklen_t addrlen)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}

	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=bind(fd, my_addr, addrlen);
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": bind error: %s",
			  strerror(errno));
#endif
		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_connect(guint32 fd, const struct sockaddr *serv_addr,
		  socklen_t addrlen)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	gint errnum;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	do {
		ret=connect(fd, serv_addr, addrlen);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());

	if(ret==-1 && errno==EACCES) {
		/* Try setting SO_BROADCAST and connecting again, but
		 * keep the original errno
		 */
		int true=1;
		
		errnum = errno;

		ret=setsockopt (fd, SOL_SOCKET, SO_BROADCAST, &true,
				sizeof(true));
		if(ret==0) {
			do {
				ret=connect (fd, serv_addr, addrlen);
			}
			while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
		}
	} else if (ret==-1) {
		errnum = errno;
	}
	
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": connect error: %s",
			  strerror(errnum));
#endif
		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_getpeername(guint32 fd, struct sockaddr *name, socklen_t *namelen)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	ret=getpeername(fd, name, namelen);
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": getpeername error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);

		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_getsockname(guint32 fd, struct sockaddr *name, socklen_t *namelen)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	ret=getsockname(fd, name, namelen);
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": getsockname error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);

		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_getsockopt(guint32 fd, int level, int optname, void *optval,
		     socklen_t *optlen)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=getsockopt(fd, level, optname, optval, optlen);
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": getsockopt error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_listen(guint32 fd, int backlog)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=listen(fd, backlog);
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": listen error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);

		return(SOCKET_ERROR);
	}

	return(0);
}

int _wapi_recv(guint32 fd, void *buf, size_t len, int recv_flags)
{
	return(_wapi_recvfrom(fd, buf, len, recv_flags, NULL, 0));
}

int _wapi_recvfrom(guint32 fd, void *buf, size_t len, int recv_flags,
		   struct sockaddr *from, socklen_t *fromlen)
{
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);	// old SIGPIPE handler
#endif
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret=recvfrom(fd, buf, len, recv_flags | MSG_NOSIGNAL, from,
			     fromlen);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
#else
	old_sigpipe = signal(SIGPIPE, SIG_IGN);
	do {
		ret=recvfrom(fd, buf, len, recv_flags, from, fromlen);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
	signal(SIGPIPE, old_sigpipe);
#endif

	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": recv error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
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
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret=send(fd, msg, len, send_flags | MSG_NOSIGNAL);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
#else
	old_sigpipe = signal(SIGPIPE, SIG_IGN);
	do {
		ret=send(fd, msg, len, send_flags);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
	signal(SIGPIPE, old_sigpipe);
#endif
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": send error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
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
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret=sendto(fd, msg, len, send_flags | MSG_NOSIGNAL, to, tolen);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
#else
	old_sigpipe = signal(SIGPIPE, SIG_IGN);
	do {
		ret=sendto(fd, msg, len, send_flags, to, tolen);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
	signal(SIGPIPE, old_sigpipe);
#endif
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": send error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_setsockopt(guint32 fd, int level, int optname,
		     const void *optval, socklen_t optlen)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=setsockopt(fd, level, optname, optval, optlen);
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": setsockopt error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_shutdown(guint32 fd, int how)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=shutdown(fd, how);
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": shutdown error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	
	return(ret);
}

guint32 _wapi_socket(int domain, int type, int protocol, void *unused, guint32 unused2, guint32 unused3)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gpointer handle;
	gboolean ok;
	int fd;
	int thr_ret;
	guint32 ret = INVALID_SOCKET;
	
	fd=socket(domain, type, protocol);
	if (fd==-1 && domain == AF_INET && type == SOCK_RAW && protocol == 0) {
		/* Retry with protocol == 4 (see bug #54565) */
		fd = socket (AF_INET, SOCK_RAW, 4);
	}
	
	if (fd == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": socket error: %s", strerror(errno));
#endif
		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);

		return(INVALID_SOCKET);
	}

	if (fd >= _wapi_fd_offset_table_size) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": File descriptor is too big");
#endif

		WSASetLastError (WSASYSCALLFAILURE);
		close (fd);
		
		return(INVALID_SOCKET);
	}
	
	
	mono_once (&socket_ops_once, socket_ops_init);
	
	handle=_wapi_handle_new (WAPI_HANDLE_SOCKET);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating socket handle");
		return(INVALID_SOCKET);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET, NULL,
				(gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle %p", handle);
		goto cleanup;
	}

	_wapi_handle_fd_offset_store (fd, handle);
	ret = fd;
	
	socket_private_handle->fd_mapped.fd = fd;
	socket_private_handle->fd_mapped.assigned = TRUE;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": returning socket handle %p with fd %d", handle,
		  socket_private_handle->fd_mapped.fd);
#endif

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(ret);
}

struct hostent *_wapi_gethostbyname(const char *hostname)
{
	struct hostent *he;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(NULL);
	}

	he=gethostbyname(hostname);
	if(he==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": gethostbyname error: %s",
			  strerror(h_errno));
#endif

		switch(h_errno) {
		case HOST_NOT_FOUND:
			WSASetLastError(WSAHOST_NOT_FOUND);
			break;
#if NO_ADDRESS != NO_DATA
		case NO_ADDRESS:
#endif
		case NO_DATA:
			WSASetLastError(WSANO_DATA);
			break;
		case NO_RECOVERY:
			WSASetLastError(WSANO_RECOVERY);
			break;
		case TRY_AGAIN:
			WSASetLastError(WSATRY_AGAIN);
			break;
		default:
			g_warning (G_GNUC_PRETTY_FUNCTION ": Need to translate %d into winsock error", h_errno);
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
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	gchar *buffer = NULL;

	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}

	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return SOCKET_ERROR;
	}

	if (i_len > 0)
		buffer = g_memdup (input, i_len);

	ret = ioctl (fd, command, buffer);
	if (ret == -1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": WSAIoctl error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		g_free (buffer);
		
		return SOCKET_ERROR;
	}

	if (buffer == NULL) {
		*written = 0;
	} else {
		/* We just copy the buffer to the output. Some ioctls
		 * don't even output any data, but, well... */
		i_len = (i_len > o_len) ? o_len : i_len;
		memcpy (output, buffer, i_len);
		g_free (buffer);
		*written = i_len;
	}

	return 0;
}

int ioctlsocket(guint32 fd, gint32 command, gpointer arg)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	if(command!=FIONBIO &&
	   command!=FIONREAD &&
	   command!=SIOCATMARK) {
		/* Not listed in the MSDN specs, but ioctl(2) returns
		 * this if command is invalid
		 */
		WSASetLastError(WSAEINVAL);
		return(SOCKET_ERROR);
	}

#ifdef O_NONBLOCK
	/* This works better than ioctl(...FIONBIO...) on Linux (it causes
	 * connect to return EINPROGRESS, but the ioctl doesn't seem to)
	 */
	if(command==FIONBIO) {
		ret=fcntl(fd, F_GETFL, 0);
		if(ret!=-1) {
			if(*(gboolean *)arg) {
				ret &= ~O_NONBLOCK;
			} else {
				ret |= O_NONBLOCK;
			}
			ret=fcntl(fd, F_SETFL, ret);
		}
	} else
#endif /* O_NONBLOCK */
	{
		ret=ioctl(fd, command, arg);
	}
	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": ioctl error: %s",
			  strerror(errno));
#endif

		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}
	
	return(0);
}

int _wapi_select(int nfds G_GNUC_UNUSED, fd_set *readfds, fd_set *writefds,
		 fd_set *exceptfds, struct timeval *timeout)
{
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}

	do {
		ret=select(getdtablesize(), readfds, writefds, exceptfds, timeout);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());

	if(ret==-1) {
		gint errnum = errno;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": select error: %s",
			  strerror(errno));
#endif
		errnum = errno_to_WSA (errnum, G_GNUC_PRETTY_FUNCTION);
		WSASetLastError (errnum);
		
		return(SOCKET_ERROR);
	}

	return(ret);
}

void _wapi_FD_CLR(guint32 fd, fd_set *set)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return;
	}

	FD_CLR(fd, set);
}

int _wapi_FD_ISSET(guint32 fd, fd_set *set)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return(0);
	}

	return(FD_ISSET(fd, set));
}

void _wapi_FD_SET(guint32 fd, fd_set *set)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (GUINT_TO_POINTER (fd));
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError(WSAENOTSOCK);
		return;
	}

	FD_SET(fd, set);
}

#ifdef USE_AIO

typedef struct {
	struct aiocb *aio;
	gpointer ares;
	SocketAsyncCB callback;
} notifier_data_t;

#define SIGPTR(a) a.SIGVAL_PTR

static void
async_notifier (union sigval sig)
{
	notifier_data_t *ndata = SIGPTR (sig);
	guint32 error;
	guint32 numbytes;

	error = aio_return (ndata->aio);
	if (error < 0) {
		error = _wapi_get_win32_file_error (error);
		numbytes = 0;
	} else {
		numbytes = error;
		error = 0;
	}

	ndata->callback (error, numbytes, ndata->ares);
	g_free (ndata->aio);
	g_free (ndata);
}

static gboolean
do_aio_call (gboolean is_read, gpointer fd_handle, gpointer buffer,
		guint32 numbytes, guint32 *out_bytes,
		gpointer ares,
		SocketAsyncCB callback)
{
	gpointer handle = _wapi_handle_fd_offset_to_handle (fd_handle);
	int fd = GPOINTER_TO_UINT (fd_handle);
	struct aiocb *aio;
	int result;
	notifier_data_t *ndata;
	
	if (handle == NULL ||
	    _wapi_handle_type (handle) != WAPI_HANDLE_SOCKET) {
		WSASetLastError (WSAENOTSOCK);
		return FALSE;
	}

	ndata = g_new0 (notifier_data_t, 1);
	aio = g_new0 (struct aiocb, 1);
	ndata->ares = ares;
	ndata->aio = aio;
	ndata->callback = callback;

	aio->aio_fildes = fd;
	aio->aio_lio_opcode = (is_read) ? LIO_READ : LIO_WRITE;
	aio->aio_nbytes = numbytes;
	aio->aio_offset = 0;
	aio->aio_buf = buffer;
	aio->aio_sigevent.sigev_notify = SIGEV_THREAD;
	aio->aio_sigevent.sigev_notify_function = async_notifier;
	SIGPTR (aio->aio_sigevent.sigev_value) = ndata;

	if (is_read) {
		result = aio_read (aio);
	} else {
		result = aio_write (aio);
	}

	if (result == -1) {
		WSASetLastError (errno_to_WSA (errno, "do_aio_call"));
		return FALSE;
	}

	result = aio_error (aio);
	if (result == 0) {
		numbytes = aio_return (aio);
	} else {
		WSASetLastError (errno_to_WSA (result, "do_aio_call"));
		return FALSE;
	}

	if (out_bytes)
		*out_bytes = numbytes;

	return TRUE;
}

gboolean _wapi_socket_async_read (gpointer handle, gpointer buffer,
				  guint32 numbytes,
				  guint32 *bytesread, gpointer ares,
				  SocketAsyncCB callback)
{
	return do_aio_call (TRUE, handle, buffer, numbytes, bytesread, ares, callback);
}

gboolean _wapi_socket_async_write (gpointer handle, gpointer buffer,
				  guint32 numbytes,
				  guint32 *byteswritten, gpointer ares,
				  SocketAsyncCB callback)
{
	return do_aio_call (FALSE, handle, buffer, numbytes, byteswritten, ares, callback);
}

#endif /* USE_AIO */

