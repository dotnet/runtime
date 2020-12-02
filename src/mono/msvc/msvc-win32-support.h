#pragma once

#ifdef _MSC_VER

#ifndef WINVER
#define WINVER 0x0A00
#endif

#include <SDKDDKVer.h>

#if _WIN32_WINNT < 0x0601
#error "Mono requires Windows 7 or later."
#endif /* _WIN32_WINNT < 0x0601 */

#ifndef HAVE_WINAPI_FAMILY_SUPPORT

#define HAVE_WINAPI_FAMILY_SUPPORT

/* WIN API Family support */
#include <winapifamily.h>

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
	#define HAVE_CLASSIC_WINAPI_SUPPORT 1
	#define HAVE_UWP_WINAPI_SUPPORT 0
#elif WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_APP)
	#define HAVE_CLASSIC_WINAPI_SUPPORT 0
	#define HAVE_UWP_WINAPI_SUPPORT 1
#else
	#define HAVE_CLASSIC_WINAPI_SUPPORT 0
	#define HAVE_UWP_WINAPI_SUPPORT 0
#ifndef HAVE_EXTERN_DEFINED_WINAPI_SUPPORT
	#error Unsupported WINAPI family
#endif
#endif

#endif

#endif
