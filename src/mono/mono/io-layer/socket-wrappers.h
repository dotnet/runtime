/*
 * Special header file to be included only in selected C files.
 * We need to use the _wapi_ equivalents of the socket API when
 * working with io-layer handles. On windows we define the wrappers to use
 * the normal win32 functions.
 */

#include <config.h>
#ifdef HAVE_SYS_SELECT_H
#  include <sys/select.h>
#endif
#ifdef HAVE_SYS_TIME_H
#  include <sys/time.h>
#endif
#ifdef HAVE_SYS_SOCKET_H
#  include <sys/socket.h>
#endif

#ifndef HAVE_SOCKLEN_T
#define socklen_t int
#endif

#ifdef HOST_WIN32
#define _wapi_accept accept 
#define _wapi_bind bind 
#define _wapi_connect connect 
#define _wapi_getpeername getpeername 
#define _wapi_getsockname getsockname 
#define _wapi_getsockopt getsockopt 
#define _wapi_listen listen 
#define _wapi_recv recv 
#define _wapi_recvfrom recvfrom 
#define _wapi_send send 
#define _wapi_sendto sendto 
#define _wapi_setsockopt setsockopt 
#define _wapi_shutdown shutdown 
#define _wapi_socket WSASocket 
#define _wapi_gethostbyname gethostbyname 
#define _wapi_select select 

/* No need to wrap FD_ZERO because it doesnt involve file
 * descriptors
*/
#define _wapi_FD_CLR FD_CLR
#define _wapi_FD_ISSET FD_ISSET
#define _wapi_FD_SET FD_SET

#else

#define WSA_FLAG_OVERLAPPED           0x01

extern guint32 _wapi_accept(guint32 handle, struct sockaddr *addr,
			    socklen_t *addrlen);
extern int _wapi_bind(guint32 handle, struct sockaddr *my_addr,
		      socklen_t addrlen);
extern int _wapi_connect(guint32 handle, const struct sockaddr *serv_addr,
			 socklen_t addrlen);
extern int _wapi_getpeername(guint32 handle, struct sockaddr *name,
			     socklen_t *namelen);
extern int _wapi_getsockname(guint32 handle, struct sockaddr *name,
			     socklen_t *namelen);
extern int _wapi_getsockopt(guint32 handle, int level, int optname,
			    void *optval, socklen_t *optlen);
extern int _wapi_listen(guint32 handle, int backlog);
extern int _wapi_recv(guint32 handle, void *buf, size_t len, int recv_flags);
extern int _wapi_recvfrom(guint32 handle, void *buf, size_t len,
			  int recv_flags, struct sockaddr *from,
			  socklen_t *fromlen);
extern int _wapi_send(guint32 handle, const void *msg, size_t len,
		      int send_flags);
extern int _wapi_sendto(guint32 handle, const void *msg, size_t len,
			int send_flags, const struct sockaddr *to,
			socklen_t tolen);
extern int _wapi_setsockopt(guint32 handle, int level, int optname,
			    const void *optval, socklen_t optlen);
extern int _wapi_shutdown(guint32 handle, int how);
extern guint32 _wapi_socket(int domain, int type, int protocol, void *unused,
			    guint32 unused2, guint32 flags);
extern struct hostent *_wapi_gethostbyname(const char *hostname);

#ifdef HAVE_SYS_SELECT_H
extern int _wapi_select(int nfds, fd_set *readfds, fd_set *writefds,
			fd_set *exceptfds, struct timeval *timeout);

extern void _wapi_FD_CLR(guint32 handle, fd_set *set);
extern int _wapi_FD_ISSET(guint32 handle, fd_set *set);
extern void _wapi_FD_SET(guint32 handle, fd_set *set);
#endif

#endif /* HOST_WIN32 */

