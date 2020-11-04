#pragma once

#ifdef _MSC_VER

/* Building Mono runtime under MSVC uses this template for it's config.h since autogen.sh can't */
/* generate a config.h that is suitable for MSVC builds. The below template will still get */
/* some dynamic configuration from autogen.sh config.h, if one exists. */
#include <msvc/msvc-win32-support.h>
#include <msvc/msvc-disabled-warnings.h>

#ifdef HAVE_BOEHM_GC
/* Only used when building using Boehm GC and only supported on x86 builds */
#define DEFAULT_GC_NAME "Included Boehm (with typed GC)"
#endif

/* Some VES is available at runtime */
#define ENABLE_ILGEN 1

/* Start configure ENABLE_DEFINES picked up from cygconfig.h or other external source, if available */
/* @ENABLE_DEFINES@ */
/* End configure ENABLE_DEFINES picked up from cygconfig.h or other external source, if available */

/* Windows MSVC builds defaults to preemptive suspend. Disable ENABLE_HYBRID_SUSPEND defines. */
#undef ENABLE_HYBRID_SUSPEND

/* No ENABLE_DEFINES below this point */

/* Keep in sync with netcore runtime-preset in configure.ac */
#ifdef ENABLE_NETCORE
#ifndef DISABLE_REMOTING
#define DISABLE_REMOTING 1
#endif
#ifndef DISABLE_SIMD
// FIXME: disable SIMD support for Windows, see https://github.com/dotnet/runtime/issues/1933
#define DISABLE_SIMD 1
#endif
#ifndef DISABLE_REFLECTION_EMIT_SAVE
#define DISABLE_REFLECTION_EMIT_SAVE 1
#endif
#ifndef DISABLE_APPDOMAINS
#define DISABLE_APPDOMAINS 1
#endif
#ifndef DISABLE_CLEANUP
#define DISABLE_CLEANUP 1
#endif
#ifndef DISABLE_DESKTOP_LOADER
#define DISABLE_DESKTOP_LOADER 1
#endif
#ifndef DISABLE_SECURITY
#define DISABLE_SECURITY 1
#endif
#ifndef DISABLE_MDB
#define DISABLE_MDB 1
#endif
#ifndef DISABLE_COM
#define DISABLE_COM 1
#endif
#ifndef DISABLE_GAC
#define DISABLE_GAC 1
#endif
#ifndef DISABLE_PERFCOUNTERS
#define DISABLE_PERFCOUNTERS 1
#endif
#ifndef DISABLE_ATTACH
#define DISABLE_ATTACH 1
#endif
#ifndef DISABLE_DLLMAP
#define DISABLE_DLLMAP 1
#endif
#ifndef DISABLE_CFGDIR_CONFIG
#define DISABLE_CFGDIR_CONFIG 1
#endif
#endif

/* Disable runtime state dumping */
#define DISABLE_CRASH_REPORTING 1

/* String of disabled features */
#define DISABLED_FEATURES "none"

/* Disables the IO portability layer */
#define DISABLE_PORTABILITY 1

/* Start configure DISABLE_DEFINES picked up from cygconfig.h or other external source, if available */
/* @DISABLE_DEFINES@ */
/* End configure DISABLE_DEFINES picked up from cygconfig.h or other external source, if available */

/* No DISABLE_DEFINES below this point */

/* Have access */
#define HAVE_ACCESS 1

/* Define to 1 if you have the `system' function. */
#if HAVE_WINAPI_FAMILY_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#define HAVE_SYSTEM 1
#endif

/* Define to 1 if you have the <complex.h> header file. */
#define HAVE_COMPLEX_H 1

#if defined(HAVE_SGEN_GC) && !defined(HAVE_CONC_GC_AS_DEFAULT)
/* Defaults to concurrent GC */
#define HAVE_CONC_GC_AS_DEFAULT 1
#endif

/* Have /dev/random */
#define HAVE_CRYPT_RNG 1

/* Define to 1 if you have the `getaddrinfo' function. */
#define HAVE_GETADDRINFO 1

/* Define to 1 if you have the `getnameinfo' function. */
#define HAVE_GETNAMEINFO 1

/* Define to 1 if you have the `getprotobyname' function. */
#define HAVE_GETPROTOBYNAME 1

/* Have inet_ntop */
#define HAVE_INET_NTOP 1

/* Define to 1 if you have the `inet_pton' function. */
#define HAVE_INET_PTON 1

/* Define to 1 if you have the <inttypes.h> header file. */
#define HAVE_INTTYPES_H 1

/* Define to 1 if you have the <memory.h> header file. */
#define HAVE_MEMORY_H 1

#if defined(HAVE_SGEN_GC) && !defined(HAVE_MOVING_COLLECTOR)
/* Moving collector */
#define HAVE_MOVING_COLLECTOR 1
#endif

/* Define to 1 if you have the `signal' function. */
#define HAVE_SIGNAL 1

 /* Define to 1 if you have the <signal.h> header file. */
#define HAVE_SIGNAL_H 1

 /* Define to 1 if you have the <stdint.h> header file. */
#define HAVE_STDINT_H 1

/* Define to 1 if you have the <stdlib.h> header file. */
#define HAVE_STDLIB_H 1

/* Define to 1 if you have the <strings.h> header file. */
#define HAVE_STRINGS_H 1

/* Define to 1 if you have the <string.h> header file. */
#define HAVE_STRING_H 1

/* Define to 1 if you have the `strtok_r' function. */
#define HAVE_STRTOK_R 1

/* Have struct ip_mreq */
#define HAVE_STRUCT_IP_MREQ 1

/* Define to 1 if the system has the type `struct sockaddr_in6'. */
#define HAVE_STRUCT_SOCKADDR_IN6 1

/* Define to 1 if you have the <sys/stat.h> header file. */
#define HAVE_SYS_STAT_H 1

/* Define to 1 if you have the <sys/types.h> header file. */
#define HAVE_SYS_TYPES_H 1

/* Define to 1 if you have the <sys/utime.h> header file. */
#define HAVE_SYS_UTIME_H 1

/* Define to 1 if you have the <wchar.h> header file. */
#define HAVE_WCHAR_H 1

/* Define to 1 if you have the <winternl.h> header file. */
#define HAVE_WINTERNL_H 1

#if defined(HAVE_SGEN_GC) && !defined(HAVE_WRITE_BARRIERS)
#define HAVE_WRITE_BARRIERS
#endif

/* Start configure HAVE_DEFINES picked up from cygconfig.h or other external source, if available */
/* @HAVE_DEFINES@ */
/* End configure HAVE_DEFINES picked up from cygconfig.h or other external source, if available */

/* No HAVE_DEFINES below this point */

/* This platform does not support symlinks */
#define HOST_NO_SYMLINKS 1

/* Host Platform is Win32 */
#define HOST_WIN32 1

/* The architecture this is running on */
#if defined(_M_IA64)
#define MONO_ARCHITECTURE "ia64"
#elif defined(_M_AMD64)
#define MONO_ARCHITECTURE "amd64"
#elif defined(_M_IX86)
#define MONO_ARCHITECTURE "x86"
#else
#error Unknown architecture
#endif

/* Version of the corlib-runtime interface */
#define MONO_CORLIB_VERSION "#MONO_CORLIB_VERSION#"

/* Have __thread keyword */
#define MONO_KEYWORD_THREAD __declspec (thread)

/* Length of zero length arrays */
#define MONO_ZERO_LEN_ARRAY 1

/* Name of /dev/random */
#define NAME_DEV_RANDOM ""

/* Name of package */
#define PACKAGE "mono"

/* Define to the address where bug reports for this package should be sent. */
#define PACKAGE_BUGREPORT "https://github.com/mono/mono/issues/new"

/* Define to the full name of this package. */
#define PACKAGE_NAME "mono"

/* Define to the full name and version of this package. */
#define PACKAGE_STRING "mono #MONO_VERSION#"

/* Define to the one symbol short name of this package. */
#define PACKAGE_TARNAME "mono"

/* Define to the home page for this package. */
#define PACKAGE_URL ""

/* Define to the version of this package. */
#define PACKAGE_VERSION "#MONO_VERSION#"

/* The size of `int', as computed by sizeof. */
#define SIZEOF_INT 4

/* The size of `long', as computed by sizeof. */
#define SIZEOF_LONG 4

/* The size of `long long', as computed by sizeof. */
#define SIZEOF_LONG_LONG 8

/* size of target machine integer registers */
#ifdef _WIN64
#define SIZEOF_REGISTER 8
#else
#define SIZEOF_REGISTER 4
#endif

/* The size of `void *', as computed by sizeof. */
#ifdef _WIN64
#define SIZEOF_VOID_P 8
#else
#define SIZEOF_VOID_P 4
#endif

/* Define to 1 if you have the ANSI C header files. */
#define STDC_HEADERS 1

#ifdef _WIN64
#define TARGET_AMD64 1
#define HOST_AMD64 1
#else
#define TARGET_X86 1
#define HOST_X86 1
#endif

/* byte order of target */
#define TARGET_BYTE_ORDER G_BYTE_ORDER

/* wordsize of target */
#define TARGET_SIZEOF_VOID_P SIZEOF_VOID_P

/* Platform is Win32 */
#define TARGET_WIN32 1

/* Version number of package */
#define VERSION "#MONO_VERSION#"

#else

/* Not building under MSVC, use autogen.sh generated config.h */
#include "cygconfig.h"

#endif

#if defined(ENABLE_LLVM) && defined(HOST_WIN32) && defined(TARGET_WIN32) && (!defined(TARGET_AMD64) || !defined(_MSC_VER))
#error LLVM for host=Windows and target=Windows is only supported on x64 MSVC build.
#endif
