#ifndef _MONO_IOLAYER_IOLAYER_H_
#define _MONO_IOLAYER_IOLAYER_H_

#include <config.h>

#if defined(PLATFORM_WIN32_NATIVE)
/* Native win32 */
#define UNICODE
#define _UNICODE
#include <windows.h>
#elif defined(PLATFORM_WIN32)
/* Cygwin */
#define UNICODE
#define _UNICODE
#include <w32api/windows.h>
#else	/* EVERYONE ELSE */
#include "mono/io-layer/wapi.h"
#include "mono/io-layer/uglify.h"
#endif /* PLATFORM_WIN32_NATIVE */

#endif /* _MONO_IOLAYER_IOLAYER_H_ */
