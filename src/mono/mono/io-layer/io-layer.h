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

#if defined(__WIN32__)
/* Native win32 */
#define UNICODE
#define _UNICODE
#define __USE_W32_SOCKETS
#include <w32api.h>
#include <windows.h>
#include <ws2tcpip.h>
#include <psapi.h>
#else	/* EVERYONE ELSE */
#include "mono/io-layer/wapi.h"
#include "mono/io-layer/uglify.h"
#endif /* PLATFORM_WIN32 */

#endif /* _MONO_IOLAYER_IOLAYER_H_ */
