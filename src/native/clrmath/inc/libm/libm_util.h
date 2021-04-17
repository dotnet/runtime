// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _LIBM_UTIL_H_
#define _LIBM_UTIL_H_

// Various methods have Windows vs non-Windows paths for POSIX compliance
// These paths shouldn't make a difference for managed code, but we'll do the right thing for each platform

#ifdef TARGET_WINDOWS
#define WINDOWS 1
#elif defined(WINDOWS)
#error "WINDOWS is defined but TARGET_WINDOWS is not"
#endif

// We want to follow the IEEE 754 spec
#define FOLLOW_IEEE754_LOGB 1

#include "libm/libm_util_amd.h"

typedef double double_t;
typedef float float_t;

// We want to just call libm_sqrt and avoid the actual assembly/intrinsics
#define ASMSQRT(x, y)  y = clrmath_sqrt(x)
#define ASMSQRTF(x, y) y = clrmath_sqrtf(x)

#if defined(_MSC_VER)
#define ALIGN(x)    __declspec(align(x))
#define likely(x)   (x)
#define unlikely(x) (x)
#else
#define ALIGN(x)    __attribute__((aligned((x))))
#define likely(x)   __builtin_expect(!!(x), 1)
#define unlikely(x) __builtin_expect(x, 0)
#endif

#define ALM_PREFIX clrmath

#if defined(ALM_SUFFIX)
#define ALM_PROTO(x)      ALM_MAKE_PROTO_SFX(ALM_PREFIX, x, ALM_SUFFIX)
#else
#define ALM_PROTO(x)      ALM_MAKE_PROTO(ALM_PREFIX, x)
#endif

#define ALM_MAKE_PROTO_SFX(pfx, fn, sfx)        __ALM_MAKE_PROTO_SFX(pfx, fn, sfx)
#define ALM_MAKE_PROTO(pfx, fn)                 __ALM_MAKE_PROTO(pfx, fn)

#define __ALM_MAKE_PROTO_SFX(pfx, fn, sfx)        pfx##_##fn##_##sfx
#define __ALM_MAKE_PROTO(pfx, fn)                 pfx##_##fn

#define ALM_PROTO_OPT(x)      clrmath_##x

#define FN_PROTOTYPE(x)       clrmath_##x
#define FN_PROTOTYPE_BAS64(x) clrmath_##x
#define FN_PROTOTYPE_REF(x)   clrmath_##x

#define EDOM   33
#define	ERANGE 34

#ifndef _HUGE_ENUF
// _HUGE_ENUF * _HUGE_ENUF must overflow
#define _HUGE_ENUF 1e+300
#endif // _HUGE_ENUF

#ifndef INFINITY
#define INFINITY ((float)(_HUGE_ENUF * _HUGE_ENUF))
#endif // INFINITY

#ifndef UNREFERENCED_PARAMETER
#define UNREFERENCED_PARAMETER(P) (void)(P)
#endif // UNREFERENCED_PARAMETER

#endif // _LIBM_UTIL_H_
