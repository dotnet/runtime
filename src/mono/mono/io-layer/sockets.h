/*
 * sockets.h:  Socket handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_SOCKETS_H_
#define _WAPI_SOCKETS_H_

#include <sys/types.h>
#include <sys/socket.h>
#include <sys/ioctl.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <netdb.h>
#include <arpa/inet.h>

#include "mono/io-layer/wapi.h"

#define WSADESCRIPTION_LEN 256
#define WSASYS_STATUS_LEN 128

typedef struct 
{
	guint16 wVersion;
	guint16 wHighVersion;
	char szDescription[WSADESCRIPTION_LEN+1];
	char szSystemStatus[WSASYS_STATUS_LEN+1];
	guint16 iMaxSockets;
	guint16 iMaxUdpDg;
	guchar *lpVendorInfo;
} WapiWSAData;

#define INVALID_SOCKET (guint32)(~0)
#define SOCKET_ERROR -1

extern int WSAStartup(guint32 requested, WapiWSAData *data);
extern int WSACleanup(void);
extern void WSASetLastError(int error);
extern int WSAGetLastError(void);
extern int closesocket(guint32 handle);

#ifndef _WAPI_BUILDING
#define accept _wapi_accept
#define bind _wapi_bind
#define connect _wapi_connect
#define getpeername _wapi_getpeername
#define getsockname _wapi_getsockname
#define getsockopt _wapi_getsockopt
#define listen _wapi_listen
#define recv _wapi_recv
#define recvfrom _wapi_recvfrom
#define send _wapi_send
#define sendto _wapi_sendto
#define setsockopt _wapi_setsockopt
#define shutdown _wapi_shutdown
#define socket _wapi_socket
#define gethostbyname _wapi_gethostbyname
#define select _wapi_select

#ifdef FD_CLR
#undef FD_CLR
#endif

#ifdef FD_ISSET
#undef FD_ISSET
#endif

#ifdef FD_SET
#undef FD_SET
#endif

/* No need to wrap FD_ZERO because it doesnt involve file
 * descriptors
*/
#define FD_CLR _wapi_FD_CLR
#define FD_ISSET _wapi_FD_ISSET
#define FD_SET _wapi_FD_SET

#endif /* _WAPI_BUILDING */

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
extern guint32 _wapi_socket(int domain, int type, int protocol);;
extern struct hostent *_wapi_gethostbyname(const char *hostname);
extern int _wapi_select(int nfds, fd_set *readfds, fd_set *writefds,
			fd_set *exceptfds, struct timeval *timeout);
extern void _wapi_FD_CLR(guint32 handle, fd_set *set);
extern int _wapi_FD_ISSET(guint32 handle, fd_set *set);
extern void _wapi_FD_SET(guint32 handle, fd_set *set);

extern int ioctlsocket(guint32 handle, gint32 command, gpointer arg);

#endif /* _WAPI_SOCKETS_H_ */
