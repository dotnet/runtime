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

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/socket-private.h>
#include <mono/io-layer/handles-private.h>

#undef DEBUG

static guint32 startup_count=0;
static GPtrArray *sockets=NULL;
static pthread_key_t error_key;
static pthread_once_t error_key_once=PTHREAD_ONCE_INIT;

static void socket_close(gpointer handle);

static struct _WapiHandleOps socket_ops = {
	socket_close,		/* close */
	NULL,			/* getfiletype */
	NULL,			/* readfile */
	NULL,			/* writefile */
	NULL,			/* flushfile */
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* getfiletime */
	NULL,			/* setfiletime */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
};

static pthread_once_t socket_ops_once=PTHREAD_ONCE_INIT;

static void socket_ops_init (void)
{
	_wapi_handle_register_ops (WAPI_HANDLE_SOCKET, &socket_ops);
	/* No capabilities to register */
}

static void socket_close(gpointer handle)
{
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing socket handle 0x%x",
		  handle);
#endif

	closesocket(GPOINTER_TO_UINT (handle));
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
		_wapi_handle_ops_close(handle);
	}

	g_ptr_array_free(sockets, FALSE);
	sockets=NULL;
	
	return(0);
}

static void error_init(void)
{
	pthread_key_create(&error_key, NULL);
}

void WSASetLastError(int error)
{
	pthread_once(&error_key_once, error_init);
	pthread_setspecific(error_key, GINT_TO_POINTER(error));
}

int WSAGetLastError(void)
{
	int err;
	void *errptr;
	
	pthread_once(&error_key_once, error_init);
	errptr=pthread_getspecific(error_key);
	err=GPOINTER_TO_INT(errptr);
	
	return(err);
}

int closesocket(guint32 handle)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	g_ptr_array_remove_fast(sockets, GUINT_TO_POINTER (handle));
	
	ret=close(socket_private_handle->fd);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": close error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EINTR:
			WSASetLastError(WSAEINTR);
			break;
		case EIO:	
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

guint32 _wapi_accept(guint32 handle, struct sockaddr *addr,
		     socklen_t *addrlen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	struct _WapiHandlePrivate_socket *new_socket_private_handle;
	gpointer new_handle;
	gboolean ok;
	int fd;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(INVALID_SOCKET);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(INVALID_SOCKET);
	}
	
	fd=accept(socket_private_handle->fd, addr, addrlen);
	if(fd==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": accept error: %s",
			  strerror(errno));
#endif

		switch(errno) {
#if EAGAIN != EWOULDBLOCK
		case EAGAIN:
#endif
		case EWOULDBLOCK:
			WSASetLastError(WSAEWOULDBLOCK);
			break;
		case EBADF:
			break;
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EOPNOTSUPP:
			WSASetLastError(WSAEOPNOTSUPP);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		case EPERM:
			WSASetLastError(WSAENETDOWN);
			break;
		case ENOBUFS:
		case ENOMEM:
			WSASetLastError(WSAENOBUFS);
			break;
		case EMFILE:
			WSASetLastError(WSAEMFILE);
			break;
		case EINVAL:
			WSASetLastError(WSAEINVAL);
			break;
#ifdef ENOSR
		case ENOSR:
#endif
		case ECONNABORTED:
		case ESOCKTNOSUPPORT:
		case EPROTONOSUPPORT:
		case ETIMEDOUT:
#ifdef ERESTARTSYS
		case ERESTARTSYS:
#endif
			WSASetLastError(WSAENETDOWN);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(INVALID_SOCKET);
	}

	new_handle=_wapi_handle_new (WAPI_HANDLE_SOCKET);
	if(new_handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating socket handle");
		return(INVALID_SOCKET);
	}

	_wapi_handle_lock_handle (new_handle);
	
	ok=_wapi_lookup_handle (new_handle, WAPI_HANDLE_SOCKET, NULL,
				(gpointer *)&new_socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		_wapi_handle_unlock_handle (new_handle);
		return(INVALID_SOCKET);
	}
	
	new_socket_private_handle->fd=fd;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": returning newly accepted socket handle %p with fd %d",
		  new_handle, new_socket_private_handle->fd);
#endif

	_wapi_handle_unlock_handle (new_handle);

	return(GPOINTER_TO_UINT (new_handle));
}

int _wapi_bind(guint32 handle, struct sockaddr *my_addr, socklen_t addrlen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=bind(socket_private_handle->fd, my_addr, addrlen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": bind error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EINVAL:
			WSASetLastError(WSAEINVAL);
			break;
		case EACCES:
			WSASetLastError(WSAEACCES);
			break;
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
			
			/* The following apply to Unix domain sockets */
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		case EROFS:
		case ENAMETOOLONG:
		case ENOENT:
		case ENOMEM:
		case ENOTDIR:
		case ELOOP:
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_connect(guint32 handle, const struct sockaddr *serv_addr,
		  socklen_t addrlen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=connect(socket_private_handle->fd, serv_addr, addrlen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": bind error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		case EISCONN:
			WSASetLastError(WSAEISCONN);
			break;
		case ECONNREFUSED:
			WSASetLastError(WSAECONNREFUSED);
			break;
		case ETIMEDOUT:
			WSASetLastError(WSAETIMEDOUT);
			break;
		case ENETUNREACH:
			WSASetLastError(WSAENETUNREACH);
			break;
		case EADDRINUSE:
			WSASetLastError(WSAEADDRINUSE);
			break;
		case EINPROGRESS:
			WSASetLastError(WSAEINPROGRESS);
			break;
		case EALREADY:
			WSASetLastError(WSAEALREADY);
			break;
		case EAFNOSUPPORT:
			WSASetLastError(WSAEAFNOSUPPORT);
			break;
		case EACCES:
		case EPERM:
			WSASetLastError(WSAEACCES);
			break;
		case EAGAIN:
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_getpeername(guint32 handle, struct sockaddr *name,
		      socklen_t *namelen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	ret=getpeername(socket_private_handle->fd, name, namelen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": getpeername error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case ENOTCONN:
			WSASetLastError(WSAENOTCONN);
			break;
		case ENOBUFS:
			/* not documented */
			WSASetLastError(WSAENOBUFS);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}

		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_getsockname(guint32 handle, struct sockaddr *name,
		      socklen_t *namelen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}

	ret=getsockname(socket_private_handle->fd, name, namelen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": getsockname error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case ENOBUFS:
			/* not documented */
			WSASetLastError(WSAENOBUFS);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}

		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_getsockopt(guint32 handle, int level, int optname, void *optval,
		     socklen_t *optlen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=getsockopt(socket_private_handle->fd, level, optname, optval,
		       optlen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": getsockopt error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case ENOPROTOOPT:
			WSASetLastError(WSAENOPROTOOPT);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_listen(guint32 handle, int backlog)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=listen(socket_private_handle->fd, backlog);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": listen error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EADDRINUSE:
			WSASetLastError(WSAEADDRINUSE);
			break;
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EOPNOTSUPP:
			WSASetLastError(WSAEOPNOTSUPP);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}

		return(SOCKET_ERROR);
	}

	return(0);
}

int _wapi_recv(guint32 handle, void *buf, size_t len, int recv_flags)
{
	return(_wapi_recvfrom(handle, buf, len, recv_flags, NULL, 0));
}

int _wapi_recvfrom(guint32 handle, void *buf, size_t len, int recv_flags,
		   struct sockaddr *from, socklen_t *fromlen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=recvfrom(socket_private_handle->fd, buf, len, recv_flags, from,
		     fromlen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": recv error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case ECONNREFUSED:
			/* Not documented */
			WSASetLastError(WSAECONNREFUSED);
			break;
		case ENOTCONN:
			WSASetLastError(WSAENOTCONN);
			break;
		case EAGAIN:
			WSASetLastError(WSAEWOULDBLOCK);
			break;
		case EINTR:
			WSASetLastError(WSAEINTR);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		case EINVAL:
			WSASetLastError(WSAEINVAL);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_send(guint32 handle, const void *msg, size_t len, int send_flags)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=send(socket_private_handle->fd, msg, len, send_flags);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": send error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		case EMSGSIZE:
			WSASetLastError(WSAEMSGSIZE);
			break;
#if EAGAIN != EWOULDBLOCK
		case EAGAIN:
#endif
		case EWOULDBLOCK:
			WSASetLastError(WSAEWOULDBLOCK);
			break;
		case ENOBUFS:
			WSASetLastError(WSAENOBUFS);
			break;
		case EINTR:
			WSASetLastError(WSAEINTR);
			break;
		case EINVAL:
			WSASetLastError(WSAEINVAL);
			break;
		case EPIPE:
			WSASetLastError(WSAESHUTDOWN);
			break;
		case ENOMEM:
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_sendto(guint32 handle, const void *msg, size_t len, int send_flags,
		 const struct sockaddr *to, socklen_t tolen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=sendto(socket_private_handle->fd, msg, len, send_flags, to, tolen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": send error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		case EMSGSIZE:
			WSASetLastError(WSAEMSGSIZE);
			break;
#if EAGAIN != EWOULDBLOCK
		case EAGAIN:
#endif
		case EWOULDBLOCK:
			WSASetLastError(WSAEWOULDBLOCK);
			break;
		case ENOBUFS:
			WSASetLastError(WSAENOBUFS);
			break;
		case EINTR:
			WSASetLastError(WSAEINTR);
			break;
		case EINVAL:
			WSASetLastError(WSAEINVAL);
			break;
		case EPIPE:
			WSASetLastError(WSAESHUTDOWN);
			break;
		case ENOMEM:
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	return(ret);
}

int _wapi_setsockopt(guint32 handle, int level, int optname,
		     const void *optval, socklen_t optlen)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=setsockopt(socket_private_handle->fd, level, optname, optval,
		       optlen);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": setsockopt error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case ENOPROTOOPT:
			WSASetLastError(WSAENOPROTOOPT);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	
	return(ret);
}

int _wapi_shutdown(guint32 handle, int how)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(SOCKET_ERROR);
	}
	
	ret=shutdown(socket_private_handle->fd, how);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": shutdown error: %s",
			  strerror(errno));
#endif

		switch(errno) {
		case EBADF:
		case ENOTSOCK:
			WSASetLastError(WSAENOTSOCK);
			break;
		case ENOTCONN:
			WSASetLastError(WSAENOTCONN);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}
	
	return(ret);
}

guint32 _wapi_socket(int domain, int type, int protocol)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gpointer handle;
	gboolean ok;
	int fd;
	
	fd=socket(domain, type, protocol);
	if(fd==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": socket error: %s", strerror(errno));
#endif

		return(INVALID_SOCKET);
	}
	
	pthread_once (&socket_ops_once, socket_ops_init);
	
	handle=_wapi_handle_new (WAPI_HANDLE_SOCKET);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating socket handle");
		return(INVALID_SOCKET);
	}

	_wapi_handle_lock_handle (handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_SOCKET, NULL,
				(gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle %p", handle);
		_wapi_handle_unlock_handle (handle);
		return(INVALID_SOCKET);
	}
	
	socket_private_handle->fd=fd;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": returning socket handle %p with fd %d", handle,
		  socket_private_handle->fd);
#endif

	_wapi_handle_unlock_handle (handle);

	return(GPOINTER_TO_UINT (handle));
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
		g_message(G_GNUC_PRETTY_FUNCTION ": listen error: %s",
			  strerror(errno));
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
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
	}
	
	return(he);
}

int ioctlsocket(guint32 handle, gint32 command, gpointer arg)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	int ret;
	
	if(startup_count==0) {
		WSASetLastError(WSANOTINITIALISED);
		return(SOCKET_ERROR);
	}
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
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

	ret=ioctl(socket_private_handle->fd, command, arg);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": ioctl error: %s",
			  strerror(errno));
#endif
		switch(errno) {
		case ENOTTY:
		case EBADF:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EFAULT:
			WSASetLastError(WSAEFAULT);
			break;
		case EINVAL:
			WSASetLastError(WSAEINVAL);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
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

	ret=select(getdtablesize(), readfds, writefds, exceptfds, timeout);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": select error: %s",
			  strerror(errno));
#endif
		switch(errno) {
		case EBADF:
			WSASetLastError(WSAENOTSOCK);
			break;
		case EINTR:
			WSASetLastError(WSAEINTR);
			break;
		case EINVAL:
			WSASetLastError(WSAEINVAL);
			break;
		case ENOMEM:
			WSASetLastError(WSAEFAULT);
			break;
		default:
			g_warning(G_GNUC_PRETTY_FUNCTION ": Need to translate [%s] into winsock error", strerror(errno));
			break;
		}
		
		return(SOCKET_ERROR);
	}

	return(ret);
}

void _wapi_FD_CLR(guint32 handle, fd_set *set)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return;
	}

	FD_CLR(socket_private_handle->fd, set);
}

int _wapi_FD_ISSET(guint32 handle, fd_set *set)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return(0);
	}

	return(FD_ISSET(socket_private_handle->fd, set));
}

void _wapi_FD_SET(guint32 handle, fd_set *set)
{
	struct _WapiHandlePrivate_socket *socket_private_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (GUINT_TO_POINTER (handle), WAPI_HANDLE_SOCKET,
				NULL, (gpointer *)&socket_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up socket handle 0x%x", handle);
		WSASetLastError(WSAENOTSOCK);
		return;
	}

	FD_SET(socket_private_handle->fd, set);
}

