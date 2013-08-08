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

#if defined(__WIN32__) || defined(_WIN32)
/* Native win32 */
#define __USE_W32_SOCKETS
#if (_WIN32_WINNT < 0x0502)
/* GetProcessId is available on Windows XP SP1 and later.
 * Windows SDK declares it unconditionally.
 * MinGW declares for Windows XP and later.
 * Declare as __GetProcessId for unsupported targets. */
#define GetProcessId __GetProcessId
#endif
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
#include <shlobj.h>
#include <mswsock.h>
#if (_WIN32_WINNT < 0x0502)
#undef GetProcessId
#endif
#else	/* EVERYONE ELSE */
#include "mono/io-layer/wapi.h"
#include "mono/io-layer/uglify.h"
#endif /* HOST_WIN32 */

#ifdef __native_client__
#include "mono/metadata/nacl-stub.h"
#endif

#endif /* _MONO_IOLAYER_IOLAYER_H_ */
