/*
 * io-layer.h: Include the right files depending on platform.  This
 * file is the only entry point into the io-layer library.
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _MONO_IOLAYER_IOLAYER_H_
#define _MONO_IOLAYER_IOLAYER_H_

#include <config.h>
#include <glib.h>

#if defined(__WIN32__) || defined(_WIN32)
/* Native win32 */
#define __USE_W32_SOCKETS
#include <winsock2.h>
#include <windows.h>
#include <winbase.h>
/*
 * The mingw version says:
 * /usr/i686-pc-mingw32/sys-root/mingw/include/ws2tcpip.h:38:2: error: #error "ws2tcpip.h is not compatible with winsock.h. Include winsock2.h instead."
 */
#ifdef _MSC_VER
#include <ws2tcpip.h>
#endif
#include <psapi.h>

 /*
 * Workaround for missing WSAPOLLFD typedef in mingw's winsock2.h that is required for mswsock.h below.
 * Remove once http://sourceforge.net/p/mingw/bugs/1980/ is fixed.
 */
#if defined(__MINGW_MAJOR_VERSION) && __MINGW_MAJOR_VERSION == 4 
typedef struct pollfd {
  SOCKET fd;
  short  events;
  short  revents;
} WSAPOLLFD, *PWSAPOLLFD, *LPWSAPOLLFD;
#endif

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#include <mswsock.h>
#endif

#else	/* EVERYONE ELSE */
#include "mono/io-layer/wapi.h"
#endif /* HOST_WIN32 */

#ifdef __native_client__
#include "mono/metadata/nacl-stub.h"
#endif

#endif /* _MONO_IOLAYER_IOLAYER_H_ */
