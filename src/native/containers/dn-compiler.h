// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_COMPILER_H__
#define __DN_COMPILER_H__

#if defined(__GNUC__) && (__GNUC__ > 2)
#define DN_LIKELY(expr) (__builtin_expect ((expr) != 0, 1))
#define DN_UNLIKELY(expr) (__builtin_expect ((expr) != 0, 0))
#else
#define DN_LIKELY(x) (x)
#define DN_UNLIKELY(x) (x)
#endif

#if defined(__GNUC__)
#define DN_NORETURN __attribute__((noreturn))
#elif defined(_MSC_VER)
#define DN_NORETURN __declspec(noreturn)
#else
#define DN_NORETURN /*empty*/
#endif

#if defined(__GNUC__)
#define DN_ATTR_FORMAT_PRINTF(fmt_pos,arg_pos) __attribute((__format__(__printf__,fmt_pos,arg_pos)))
#else
#define DN_ATTR_FORMAT_PRINTF(fmt_pos,arg_pos) /*empty*/
#endif

#endif /* __DN_COMPILER_H__ */
