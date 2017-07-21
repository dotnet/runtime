/**
 * \file
 */

#ifndef __UTILS_MONO_COMPILER_H__
#define __UTILS_MONO_COMPILER_H__

/*
 * This file includes macros used in the runtime to encapsulate different
 * compiler behaviours.
 */
#include <config.h>

#ifdef __GNUC__
#define MONO_ATTR_USED __attribute__ ((__used__))
#else
#define MONO_ATTR_USED
#endif

#ifdef __GNUC__
#define MONO_ATTR_FORMAT_PRINTF(fmt_pos,arg_pos) __attribute__ ((__format__(__printf__,fmt_pos,arg_pos)))
#else
#define MONO_ATTR_FORMAT_PRINTF(fmt_pos,arg_pos)
#endif

/* Deal with Microsoft C compiler differences */
#ifdef _MSC_VER

#include <math.h>

#if _MSC_VER < 1800 /* VS 2013 */
#define strtoull _strtoui64
#endif

#include <float.h>
#define trunc(x)	(((x) < 0) ? ceil((x)) : floor((x)))
#if _MSC_VER < 1800 /* VS 2013 */
#define isnan(x)	_isnan(x)
#define isinf(x)	(_isnan(x) ? 0 : (_fpclass(x) == _FPCLASS_NINF) ? -1 : (_fpclass(x) == _FPCLASS_PINF) ? 1 : 0)
#define isnormal(x)	_finite(x)
#endif

#define popen		_popen
#define pclose		_pclose

#include <direct.h>
#define mkdir(x)	_mkdir(x)

#define __func__ __FUNCTION__

#include <BaseTsd.h>
typedef SSIZE_T ssize_t;

/*
 * SSIZE_MAX is not defined in MSVC, so define it here.
 *
 * These values come from MinGW64, and are public domain.
 *
 */
#ifndef SSIZE_MAX
#ifdef _WIN64
#define SSIZE_MAX _I64_MAX
#else
#define SSIZE_MAX INT_MAX
#endif
#endif

#endif /* _MSC_VER */

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
#define MONO_EMPTY_SOURCE_FILE(x) void __mono_win32_ ## x ## _quiet_lnk4221 (void) {}
#else
#define MONO_EMPTY_SOURCE_FILE(x)
#endif

#if !defined(_MSC_VER) && !defined(PLATFORM_SOLARIS) && !defined(_WIN32) && !defined(__CYGWIN__) && !defined(MONOTOUCH) && HAVE_VISIBILITY_HIDDEN
#if MONO_LLVM_LOADED
#define MONO_LLVM_INTERNAL MONO_API
#else
#define MONO_LLVM_INTERNAL
#endif
#else
#define MONO_LLVM_INTERNAL 
#endif

/* Used to mark internal functions used by the profiler modules */
#define MONO_PROFILER_API MONO_API

#ifdef __GNUC__
#define MONO_ALWAYS_INLINE __attribute__ ((__always_inline__))
#elif defined(_MSC_VER)
#define MONO_ALWAYS_INLINE __forceinline
#else
#define MONO_ALWAYS_INLINE
#endif

#ifdef __GNUC__
#define MONO_NEVER_INLINE __attribute__ ((__noinline__))
#elif defined(_MSC_VER)
#define MONO_NEVER_INLINE __declspec(noinline)
#else
#define MONO_NEVER_INLINE
#endif

#ifdef __GNUC__
#define MONO_COLD __attribute__ ((__cold__))
#else
#define MONO_COLD
#endif

#if defined (__GNUC__) && defined (__GNUC_MINOR__) && defined (__GNUC_PATCHLEVEL__)
#define MONO_GNUC_VERSION (__GNUC__ * 10000 + __GNUC_MINOR__ * 100 + __GNUC_PATCHLEVEL__)
#endif

/* Used to tell clang's ThreadSanitizer to not report data races that occur within a certain function */
#if defined(__has_feature)
#if __has_feature(thread_sanitizer)
#define MONO_NO_SANITIZE_THREAD __attribute__ ((no_sanitize("thread")))
#else
#define MONO_NO_SANITIZE_THREAD
#endif
#else
#define MONO_NO_SANITIZE_THREAD
#endif

#endif /* __UTILS_MONO_COMPILER_H__*/

