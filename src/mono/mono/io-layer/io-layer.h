#ifndef _MONO_IOLAYER_IOLAYER_H_
#define _MONO_IOLAYER_IOLAYER_H_

#include <config.h>

#if defined(PLATFORM_WIN32)
/* Native win32 */
#define UNICODE
#define _UNICODE
#define __USE_W32_SOCKETS
#include <w32api.h>
#include <windows.h>
#include <ws2tcpip.h>
#else	/* EVERYONE ELSE */
#include "mono/io-layer/wapi.h"
#include "mono/io-layer/uglify.h"
#endif /* PLATFORM_WIN32 */

#ifdef HAVE_SYS_FILIO_H
#include <sys/filio.h>     /* defines FIONBIO and FIONREAD */
#endif
#ifdef HAVE_SYS_SOCKIO_H
#include <sys/sockio.h>    /* defines SIOCATMARK */
#endif

#endif /* _MONO_IOLAYER_IOLAYER_H_ */
