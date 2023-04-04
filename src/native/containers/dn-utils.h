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

#include <dn-rt.h>

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

DN_NORETURN DN_ATTR_FORMAT_PRINTF(1,2) static inline void
dn_failfast_msg(const char* fmt, ...)
{
	va_list args;
	va_start (args, fmt);
	dn_rt_failfast_msgv(fmt, args);
	va_end (args);
}

#ifdef DISABLE_ASSERT_MESSAGES
#define dn_checkfail(cond, format, ...) (DN_LIKELY((cond)) ? 1 : (dn_rt_failfast_nomsg (__FILE__, __LINE__), 0))
#else
#define dn_checkfail(cond,format,...) (DN_LIKELY((cond)) ? 1 : (dn_failfast_msg ("* Assertion at %s:%d, condition `%s' not met, function:%s, " format "\n", __FILE__, __LINE__, #cond, __func__, ##__VA_ARGS__), 0))
#endif

#endif /* __DN_UTILS_H__ */
