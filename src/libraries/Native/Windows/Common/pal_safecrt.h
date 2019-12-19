// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"
#include "pal_config.h"

#include <string.h>
#include <assert.h>
#include <errno.h>
#include <stdbool.h>

// Multiplies a and b into result.
// Returns true if safe, false if overflows.
inline static bool multiply_s(size_t a, size_t b, size_t* result)
{
#if HAVE_BUILTIN_MUL_OVERFLOW
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
#if HAVE_BUILTIN_MUL_OVERFLOW
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
