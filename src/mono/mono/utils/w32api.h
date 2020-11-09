/**
 * \file
 */

#ifndef __MONO_UTILS_W32API_H__
#define __MONO_UTILS_W32API_H__

#include <glib.h>

#define WAIT_TOO_MANY_POSTS      ((gint) 0x0000012A)
#define WAIT_NOT_OWNED_BY_CALLER ((gint) 0x0000012B)

#ifndef HOST_WIN32

#define WAIT_FAILED        ((gint) 0xFFFFFFFF)
#define WAIT_OBJECT_0      ((gint) 0x00000000)
#define WAIT_ABANDONED_0   ((gint) 0x00000080)
#define WAIT_TIMEOUT       ((gint) 0x00000102)
#define WAIT_IO_COMPLETION ((gint) 0x000000C0)

#define WINAPI

typedef guint32 DWORD;
typedef gboolean BOOL;
typedef gint32 LONG;
typedef guint32 ULONG;
typedef guint UINT;

typedef gpointer HANDLE;
typedef gpointer HMODULE;

#else

#define __USE_W32_SOCKETS
#include <winsock2.h>
#include <windows.h>
#include <winbase.h>
/* The mingw version says: /usr/i686-pc-mingw32/sys-root/mingw/include/ws2tcpip.h:38:2: error: #error "ws2tcpip.h is not compatible with winsock.h. Include winsock2.h instead." */
#ifdef _MSC_VER
#include <ws2tcpip.h>
#endif
#include <psapi.h>

/* Workaround for missing WSAPOLLFD typedef in mingw's winsock2.h
 * that is required for mswsock.h below. Remove once
 * http://sourceforge.net/p/mingw/bugs/1980/ is fixed. */
#if defined(__MINGW_MAJOR_VERSION) && __MINGW_MAJOR_VERSION == 4
typedef struct pollfd {
	SOCKET fd;
	short  events;
	short  revents;
} WSAPOLLFD, *PWSAPOLLFD, *LPWSAPOLLFD;
#endif

#endif /* HOST_WIN32 */

#endif /* __MONO_UTILS_W32API_H__ */
