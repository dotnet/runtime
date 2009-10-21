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

#include "mono/io-layer/wapi.h"

G_BEGIN_DECLS

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

#define WSAID_DISCONNECTEX {0x7fda2e11,0x8630,0x436f,{0xa0, 0x31, 0xf5, 0x36, 0xa6, 0xee, 0xc1, 0x57}}
#define WSAID_TRANSMITFILE {0xb5367df0,0xcbac,0x11cf,{0x95,0xca,0x00,0x80,0x5f,0x48,0xa1,0x92}}

typedef struct
{
	guint32 Data1;
	guint16 Data2;
	guint16 Data3;
	guint8 Data4[8];
} WapiGuid;

typedef struct
{
	gpointer Head;
	guint32 HeadLength;
	gpointer Tail;
	guint32 TailLength;
} WapiTransmitFileBuffers;

typedef enum {
	TF_USE_DEFAULT_WORKER	= 0,
	TF_DISCONNECT		= 0x01,
	TF_REUSE_SOCKET		= 0x02,
	TF_WRITE_BEHIND		= 0x04,
	TF_USE_SYSTEM_THREAD	= 0x10,
	TF_USE_KERNEL_APC	= 0x20
} WapiTransmitFileFlags;

typedef struct
{
	guint32 len;
	gpointer buf;
} WapiWSABuf;

/* If we need to support more WSAIoctl commands then define these
 * using the bitfield flags method
 */
#define SIO_GET_EXTENSION_FUNCTION_POINTER 0xC8000006

typedef gboolean (*WapiDisconnectExFn)(guint32, WapiOverlapped *, guint32,
					WapiTransmitFileFlags);
typedef gboolean (*WapiTransmitFileFn)(guint32, gpointer, guint32, guint32,
					WapiOverlapped *,
					WapiTransmitFileBuffers *,
					WapiTransmitFileFlags);


extern int WSAStartup(guint32 requested, WapiWSAData *data);
extern int WSACleanup(void);
extern void WSASetLastError(int error);
extern int WSAGetLastError(void);
extern int closesocket(guint32 handle);

extern int ioctlsocket(guint32 handle, gint32 command, gpointer arg);
extern int WSAIoctl (guint32 handle, gint32 command,
		     gchar *input, gint i_len,
		     gchar *output, gint o_len, glong *written,
		     void *unused1, void *unused2);
extern int WSARecv (guint32 handle, WapiWSABuf *buffers, guint32 count,
		    guint32 *received, guint32 *flags,
		    WapiOverlapped *overlapped, WapiOverlappedCB *complete);
extern int WSASend (guint32 handle, WapiWSABuf *buffers, guint32 count,
		    guint32 *sent, guint32 flags,
		    WapiOverlapped *overlapped, WapiOverlappedCB *complete);

gboolean TransmitFile (guint32 socket, gpointer file, guint32 bytes_to_write, guint32 bytes_per_send, WapiOverlapped *ol,
			WapiTransmitFileBuffers *tb, guint32 flags);
G_END_DECLS
#endif /* _WAPI_SOCKETS_H_ */
