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

extern int ioctlsocket(guint32 handle, gint32 command, gpointer arg);
extern int WSAIoctl (guint32 handle, gint32 command,
		     gchar *input, gint i_len,
		     gchar *output, gint o_len, gint32 *written,
		     void *unused1, void *unused2);


#endif /* _WAPI_SOCKETS_H_ */
