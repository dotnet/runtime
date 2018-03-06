/**
 * \file
 */

#ifndef __MONO_PUBLIB_H__
#define __MONO_PUBLIB_H__

/* 
 * Minimal general purpose header for use in public mono header files.
 * We can't include config.h, so we use compiler-specific preprocessor
 * directives where needed.
 */

#ifdef  __cplusplus
#define MONO_BEGIN_DECLS  extern "C" {
#define MONO_END_DECLS    }
#else
#define MONO_BEGIN_DECLS
#define MONO_END_DECLS
#endif

MONO_BEGIN_DECLS

/* VS 2010 and later have stdint.h */
#if defined(_MSC_VER)

#if _MSC_VER < 1600

typedef __int8			int8_t;
typedef unsigned __int8		uint8_t;
typedef __int16			int16_t;
typedef unsigned __int16	uint16_t;
typedef __int32			int32_t;
typedef unsigned __int32	uint32_t;
typedef __int64			int64_t;
typedef unsigned __int64	uint64_t;

#else

#include <stdint.h>

#endif

#define MONO_API_EXPORT __declspec(dllexport)
#define MONO_API_IMPORT __declspec(dllimport)

#else

#include <stdint.h>

#if defined (__clang__) || defined (__GNUC__)
#define MONO_API_EXPORT __attribute__ ((__visibility__ ("default")))
#else
#define MONO_API_EXPORT
#endif
#define MONO_API_IMPORT

#endif /* end of compiler-specific stuff */

#include <stdlib.h>

#if defined(MONO_DLL_EXPORT)
	#define MONO_API MONO_API_EXPORT
#elif defined(MONO_DLL_IMPORT)
	#define MONO_API MONO_API_IMPORT
#else
	#define MONO_API
#endif

typedef int32_t		mono_bool;
typedef uint8_t		mono_byte;
typedef uint16_t	mono_unichar2;
typedef uint32_t	mono_unichar4;

typedef void	(*MonoFunc)	(void* data, void* user_data);
typedef void	(*MonoHFunc)	(void* key, void* value, void* user_data);

MONO_API void mono_free (void *);

#define MONO_ALLOCATOR_VTABLE_VERSION 1

typedef struct {
	int version;
	void *(*malloc)      (size_t size);
	void *(*realloc)     (void *mem, size_t count);
	void (*free)        (void *mem);
	void *(*calloc)      (size_t count, size_t size);
} MonoAllocatorVTable;

MONO_API mono_bool
mono_set_allocator_vtable (MonoAllocatorVTable* vtable);


#define MONO_CONST_RETURN const

/*
 * When embedding, you have to define MONO_ZERO_LEN_ARRAY before including any
 * other Mono header file if you use a different compiler from the one used to
 * build Mono.
 */
#ifndef MONO_ZERO_LEN_ARRAY
#ifdef __GNUC__
#define MONO_ZERO_LEN_ARRAY 0
#else
#define MONO_ZERO_LEN_ARRAY 1
#endif
#endif

#if defined (MONO_INSIDE_RUNTIME)

#if defined (__CENTRINEL__)
/* Centrinel is an analyzer that warns about raw pointer to managed objects
 * inside Mono.
 */
#define MONO_RT_MANAGED_ATTR __CENTRINEL_MANAGED_ATTR
#define MONO_RT_CENTRINEL_SUPPRESS __CENTRINEL_SUPPRESS_ATTR(1)
#else
#define MONO_RT_MANAGED_ATTR
#define MONO_RT_CENTRINEL_SUPPRESS
#endif

#if defined (__clang__) || defined (__GNUC__)
// attribute(deprecated(message)) was introduced in gcc 4.5.
// attribute(deprecated))         was introduced in gcc 4.0.
// Compare: https://gcc.gnu.org/onlinedocs/gcc-3.4.6/gcc/Function-Attributes.html
//          https://gcc.gnu.org/onlinedocs/gcc-4.4.0/gcc/Function-Attributes.html
//          https://gcc.gnu.org/onlinedocs/gcc-4.5.0/gcc/Function-Attributes.html
#if defined (__clang__) || (__GNUC__ > 4) || (__GNUC__ == 4 && __GNUC_MINOR__ >= 5)
#define MONO_RT_EXTERNAL_ONLY \
	__attribute__ ((__deprecated__ ("The mono runtime must not call this function."))) \
	MONO_RT_CENTRINEL_SUPPRESS
#elif __GNUC__ >= 4
#define MONO_RT_EXTERNAL_ONLY __attribute__ ((__deprecated__)) MONO_RT_CENTRINEL_SUPPRESS
#else
#define MONO_RT_EXTERNAL_ONLY MONO_RT_CENTRINEL_SUPPRESS
#endif

#if defined (__clang__) || (__GNUC__ > 4) || (__GNUC__ == 4 && __GNUC_MINOR__ >= 2)
// Pragmas for controlling diagnostics appear to be from gcc 4.2.
// This is used in place of configure gcc -Werror=deprecated-declarations:
// 1. To be portable across build systems.
// 2. configure is very sensitive to compiler flags; they break autoconf's probes.
// Though #2 can be mitigated by being late in configure.
#pragma GCC diagnostic error "-Wdeprecated-declarations"
#endif

#else
#define MONO_RT_EXTERNAL_ONLY MONO_RT_CENTRINEL_SUPPRESS
#endif // clang or gcc

#else
#define MONO_RT_EXTERNAL_ONLY
#define MONO_RT_MANAGED_ATTR
#endif /* MONO_INSIDE_RUNTIME */

#if defined (__clang__) || defined (__GNUC__)
#define _MONO_DEPRECATED __attribute__ ((__deprecated__))
#elif defined (_MSC_VER)
#define _MONO_DEPRECATED __declspec (deprecated)
#else
#define _MONO_DEPRECATED
#endif

#define MONO_DEPRECATED MONO_API MONO_RT_EXTERNAL_ONLY _MONO_DEPRECATED

MONO_END_DECLS

#endif /* __MONO_PUBLIB_H__ */
