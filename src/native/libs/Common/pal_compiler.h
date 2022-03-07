// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifndef __cplusplus
#include <stdbool.h>
#endif

#if defined(TARGET_ANDROID)
#include <stdlib.h>
#include <android/log.h>
#endif

#ifndef __has_extension
#define __has_extension(...) 0
#endif

#ifdef static_assert
#define c_static_assert_msg(e, msg) static_assert((e), msg)
#define c_static_assert(e) c_static_assert_msg(e,"")
#elif __has_extension(c_static_assert)
#define c_static_assert_msg(e, msg) _Static_assert((e), msg)
#define c_static_assert(e) c_static_assert_msg(e, "")
#else
#define c_static_assert_msg(e, msg) typedef char __c_static_assert__[(e)?1:-1]
#define c_static_assert(e) c_static_assert_msg(e, "")
#endif

#if defined(TARGET_ANDROID)
static inline void
do_abort_unless (bool condition, const char* fmt, ...)
{
    if (condition) {
        return;
    }

    va_list ap;

    va_start (ap, fmt);
    __android_log_vprint (ANDROID_LOG_FATAL, "DOTNET", fmt, ap);
    va_end (ap);

    abort ();
}
#endif

#define abort_unless(_condition_, _fmt_, ...) do_abort_unless (_condition_, "%s:%d (%s): " _fmt_, __FILE__, __LINE__, __FUNCTION__, ## __VA_ARGS__)
#define abort_if_invalid_pointer_argument(_ptr_) abort_unless ((_ptr_) != NULL, "Parameter '%s' must be a valid pointer", #_ptr_)
#define abort_if_negative_integer_argument(_arg_) abort_unless ((_arg_) > 0, "Parameter '%s' must be larger than 0", #_arg_)

#ifndef PALEXPORT
#ifdef TARGET_UNIX
#define PALEXPORT __attribute__ ((__visibility__ ("default")))
#else
#define PALEXPORT
#endif
#endif // PALEXPORT

#ifndef EXTERN_C
#ifdef __cplusplus
#define EXTERN_C extern "C"
#else
#define EXTERN_C extern
#endif // __cplusplus
#endif // EXTERN_C

#ifndef TYPEOF
#ifdef __cplusplus
#define TYPEOF decltype
#else
#define TYPEOF __typeof
#endif // __cplusplus
#endif // TYPEOF
