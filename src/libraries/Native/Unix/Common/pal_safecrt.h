// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

#include <string.h>
#include <assert.h>
#include <errno.h>
#include <stdbool.h>

#ifdef __cplusplus

#include <safemath/safemath.h> // remove once __builtin_*_overflow builtins are available everywhere

// Multiplies a and b into result.
// Returns true if safe, false if overflows.
template <typename T>
inline static bool multiply_s(T a, T b, T* result)
{
    return
#if __has_builtin(__builtin_mul_overflow)
        !__builtin_mul_overflow(a, b, result);
#else
        ClrSafeInt<T>::multiply(a, b, *result);
#endif
}


// Adds a and b into result.
// Returns true if safe, false if overflows.
template <typename T>
inline static bool add_s(T a, T b, T* result)
{
    return
#if __has_builtin(__builtin_add_overflow)
        !__builtin_add_overflow(a, b, result);
#else
        ClrSafeInt<T>::addition(a, b, *result);
#endif
}

#else // __cplusplus

// Multiplies a and b into result.
// Returns true if safe, false if overflows.
inline static bool multiply_s(size_t a, size_t b, size_t* result)
{
#if __has_builtin(__builtin_mul_overflow)
    return !__builtin_mul_overflow(a, b, result);
#else
    if (a == 0 || b == 0)
    {
        *result = 0;
        return true;
    }

    if(((size_t)~((size_t)0)) / a < b)
    {
        //overflow
        return false;
    }
    //ok
    *result = a * b;
    return true;
#endif
}


// Adds a and b into result.
// Returns true if safe, false if overflows.
inline static bool add_s(size_t a, size_t b, size_t* result)
{
#if __has_builtin(__builtin_add_overflow)
    return !__builtin_add_overflow(a, b, result);
#else
    if(((size_t)~((size_t)0)) - a < b)
    {
        //overflow
        return false;
    }
    //ok
    *result = a + b;
    return true;
#endif
}

#endif // __cplusplus

BEGIN_EXTERN_C

typedef int errno_t;

inline static errno_t memcpy_s(void* dst, size_t sizeInBytes, const void* src, size_t count)
{
    if (count > 0)
    {
        assert(dst != NULL);
        assert(src != NULL);
        assert(sizeInBytes >= count);
        assert( // should be using memmove if this fails
            ((const char*)dst + count <= (const char*)src) ||
            ((const char*)src + count <= (const char*)dst));

        if (dst == NULL)
        {
            return EINVAL;
        }

        if (src == NULL || sizeInBytes < count)
        {
            memset(dst, 0, sizeInBytes);
            return src == NULL ? EINVAL : ERANGE;
        }
  
        memcpy(dst, src, count);
    }

    return 0;
}

END_EXTERN_C
