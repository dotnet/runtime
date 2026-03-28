// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __RHASSERT_H__
#define __RHASSERT_H__

#include <minipal/utils.h>

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)

#define ASSERT(expr) \
    { \
    if (!(expr)) { Assert(#expr, __FILE__, __LINE__, NULL); } \
    } \

#define ASSERT_MSG(expr, msg) \
    { \
    if (!(expr)) { Assert(#expr, __FILE__, __LINE__, msg); } \
    } \

#define VERIFY(expr) ASSERT((expr))

#define ASSERT_UNCONDITIONALLY(message) \
    Assert("ASSERT_UNCONDITIONALLY", __FILE__, __LINE__, message); \

void Assert(const char * expr, const char * file, unsigned int line_num, const char * message);

#else

#define ASSERT(expr)

#define ASSERT_MSG(expr, msg)

#define VERIFY(expr) (expr)

#define ASSERT_UNCONDITIONALLY(message)

#endif

#ifndef _ASSERTE
#define _ASSERTE(_expr) ASSERT(_expr)
#endif

#ifndef _ASSERTE_ALL_BUILDS
#define _ASSERTE_ALL_BUILDS(_expr) ASSERT(_expr)
#endif

#define PORTABILITY_ASSERT(message) \
    ASSERT_UNCONDITIONALLY(message); \
    UNREACHABLE(); \

#ifdef assert
#undef assert
#define assert(_expr) ASSERT(_expr)
#endif

#ifdef HOST_WINDOWS
#define RhFailFast() ::RaiseFailFastException(NULL, NULL, FAIL_FAST_GENERATE_EXCEPTION_ADDRESS)
#else
void RhFailFast();
#endif // HOST_WINDOWS

#endif // __RHASSERT_H__
