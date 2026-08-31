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
#if defined(HAVE_UNISTD_H)
#include <unistd.h>
#endif

#include <mono/utils/mono-publib.h>

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
#include <float.h>

#define popen		_popen
#define pclose		_pclose
#include <direct.h>
#define mkdir(x)	_mkdir(x)

#define __func__ __FUNCTION__

#include <stddef.h>
#include <stdint.h>

// ssize_t and SSIZE_MAX are Posix, define for Windows.
typedef ptrdiff_t ssize_t;
#ifndef SSIZE_MAX
#define SSIZE_MAX INTPTR_MAX
#endif

#endif /* _MSC_VER */

// Quiet Visual Studio linker warning, LNK4221: This object file does not define any previously
// undefined public symbols, so it will not be used by any link operation that consumes this library.
// And other linkers, e.g. older Apple.
#define MONO_EMPTY_SOURCE_FILE(x) extern const char mono_quash_linker_empty_file_warning_ ## x; \
				  const char mono_quash_linker_empty_file_warning_ ## x = 0;

#ifdef _MSC_VER
#define MONO_PRAGMA_WARNING_PUSH() __pragma(warning (push))
#define MONO_PRAGMA_WARNING_DISABLE(x) __pragma(warning (disable:x))
#define MONO_PRAGMA_WARNING_POP() __pragma(warning (pop))

#define MONO_DISABLE_WARNING(x) \
		MONO_PRAGMA_WARNING_PUSH() \
		MONO_PRAGMA_WARNING_DISABLE(x)

#define MONO_RESTORE_WARNING \
		MONO_PRAGMA_WARNING_POP()
#else
#define MONO_PRAGMA_WARNING_PUSH()
#define MONO_PRAGMA_WARNING_DISABLE(x)
#define MONO_PRAGMA_WARNING_POP()
#define MONO_DISABLE_WARNING(x)
#define MONO_RESTORE_WARNING
#endif

/* Used to mark internal functions used by the profiler modules */
#define MONO_PROFILER_API MONO_API

/* Used to mark internal functions used by the CoreFX PAL library */
#define MONO_PAL_API MONO_API

/* Mono components */

/* Used to mark internal functions used by dynamically linked runtime components */
#define MONO_COMPONENT_API MONO_API

#ifdef COMPILING_COMPONENT_DYNAMIC
#define MONO_COMPONENT_EXPORT_ENTRYPOINT MONO_EXTERN_C MONO_API_EXPORT
#else
#define MONO_COMPONENT_EXPORT_ENTRYPOINT /* empty */
#endif


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

#if defined (__clang__)
#define MONO_NO_OPTIMIZATION __attribute__ ((optnone))
#elif __GNUC__ > 4 || (__GNUC__ == 4 && __GNUC_MINOR__ >= 4)
#define MONO_NO_OPTIMIZATION __attribute__ ((optimize("O0")))
#else
#define MONO_NO_OPTIMIZATION /* nothing */
#endif

#if defined (__GNUC__) && defined (__GNUC_MINOR__) && defined (__GNUC_PATCHLEVEL__)
#define MONO_GNUC_VERSION (__GNUC__ * 10000 + __GNUC_MINOR__ * 100 + __GNUC_PATCHLEVEL__)
#endif

#if defined(__has_feature)

#if __has_feature(thread_sanitizer)
#define MONO_HAS_CLANG_THREAD_SANITIZER 1
#else
#define MONO_HAS_CLANG_THREAD_SANITIZER 0
#endif

#if __has_feature(address_sanitizer)
#define MONO_HAS_CLANG_ADDRESS_SANITIZER 1
#else
#define MONO_HAS_CLANG_ADDRESS_SANITIZER 0
#endif

#else
#define MONO_HAS_CLANG_THREAD_SANITIZER 0
#define MONO_HAS_CLANG_ADDRESS_SANITIZER 0
#endif

/* Used to tell Clang's ThreadSanitizer to not report data races that occur within a certain function */
#if MONO_HAS_CLANG_THREAD_SANITIZER
#define MONO_NO_SANITIZE_THREAD __attribute__ ((no_sanitize("thread")))
#else
#define MONO_NO_SANITIZE_THREAD
#endif

/* Used to tell Clang's AddressSanitizer to turn off instrumentation for a certain function */
#if MONO_HAS_CLANG_ADDRESS_SANITIZER
#define MONO_NO_SANITIZE_ADDRESS __attribute__ ((no_sanitize("address")))
#else
#define MONO_NO_SANITIZE_ADDRESS
#endif

#endif /* __UTILS_MONO_COMPILER_H__*/
