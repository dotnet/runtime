// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

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
