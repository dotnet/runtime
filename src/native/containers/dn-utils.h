// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_UTILS_H__
#define __DN_UTILS_H__

#ifdef __cplusplus
#ifndef __STDC_LIMIT_MACROS
#define __STDC_LIMIT_MACROS
#endif
#else
#include <stdbool.h>
#endif

#include <stddef.h>
#include <stdint.h>
#include <string.h>

#if defined(_DEBUG)
#include <assert.h>
#define DN_ASSERT(x) assert(x)
#else
#define DN_ASSERT(x)
#endif

#ifndef UINT32_MAX
#define UINT32_MAX ((uint32_t)0xffffffff)
#endif

#ifndef INT32_MAX
#define INT32_MAX ((int32_t)2147483647)
#endif

#if defined(_WIN32)
#define DN_CALLBACK_CALLTYPE __cdecl
#else
#define DN_CALLBACK_CALLTYPE
#endif

#if defined(__GNUC__) && (__GNUC__ > 2)
#define DN_LIKELY(expr) (__builtin_expect ((expr) != 0, 1))
#define DN_UNLIKELY(expr) (__builtin_expect ((expr) != 0, 0))
#else
#define DN_LIKELY(x) (x)
#define DN_UNLIKELY(x) (x)
#endif

#define DN_UNREFERENCED_PARAMETER(expr) (void)(expr)

// Until C11 support, use typedef expression for static assertion.
#define _DN_STATIC_ASSERT_UNQIUE_TYPEDEF0(line) __dn_static_assert_ ## line ## _t
#define _DN_STATIC_ASSERT_UNQIUE_TYPEDEF(line) _DN_STATIC_ASSERT_UNQIUE_TYPEDEF0(line)
#define _DN_STATIC_ASSERT(expr) typedef char _DN_STATIC_ASSERT_UNQIUE_TYPEDEF(__LINE__)[(expr) != 0]

static inline bool
dn_safe_size_t_multiply (size_t lhs, size_t rhs, size_t *result)
{
	if (lhs == 0 || rhs == 0) {
		*result = 0;
		return true;
	}
	
	if (((size_t)(~(size_t)0) / lhs) < rhs)
		return false;

	*result = lhs * rhs;
	return true;
}

static inline bool
dn_safe_uint32_t_add (uint32_t lhs, uint32_t rhs, uint32_t *result)
{
	if((UINT32_MAX - lhs) < rhs)
		return false;

	*result = lhs + rhs;
	return true;
}

#endif /* __DN_UTILS_H__ */
