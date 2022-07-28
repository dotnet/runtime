// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __RHASSERT_H__
#define __RHASSERT_H__

#ifdef _MSC_VER
#define ASSUME(expr) __assume(expr)
#else  // _MSC_VER
#define ASSUME(expr) do { if (!(expr)) __builtin_unreachable(); } while (0)
#endif // _MSC_VER

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

#define PORTABILITY_ASSERT(message) \
    ASSERT_UNCONDITIONALLY(message); \
    ASSUME(0); \

#define UNREACHABLE() \
    ASSERT_UNCONDITIONALLY("UNREACHABLE"); \
    ASSUME(0); \

#define UNREACHABLE_MSG(message) \
    ASSERT_UNCONDITIONALLY(message); \
    ASSUME(0);  \

#define FAIL_FAST_GENERATE_EXCEPTION_ADDRESS 0x1

#define RhFailFast() PalRaiseFailFastException(NULL, NULL, FAIL_FAST_GENERATE_EXCEPTION_ADDRESS)

#endif // __RHASSERT_H__
