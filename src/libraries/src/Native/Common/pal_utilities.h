// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"
#include "pal_config.h"

#include <assert.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>
#include <type_traits>

/**
 * Cast a positive value typed as a signed integer to the
 * appropriately sized unsigned integer type.
 *
 * We use this when we've already ensured that the value is positive,
 * but we don't want to cast to a specific unsigned type as that could
 * inadvertently defeat the compiler's narrowing conversion warnings
 * (which we treat as error).
 */
template <typename T>
inline typename std::make_unsigned<T>::type UnsignedCast(T value)
{
    assert(value >= 0);
    return static_cast<typename std::make_unsigned<T>::type>(value);
}

/**
 * Abstraction helper method to safely copy strings using strlcpy or strcpy_s
 * or a different safe copy method, depending on the current platform.
 */
inline void SafeStringCopy(char* destination, size_t destinationSize, const char* source)
{
#if HAVE_STRCPY_S
    strcpy_s(destination, destinationSize, source);
#elif HAVE_STRLCPY
    strlcpy(destination, source, destinationSize);
#else
    snprintf(destination, destinationSize, "%s", source);
#endif
}

/**
 * Overload of SafeStringCopy that takes a signed int32_t as buffer
 * size. Asserts that its positive, but defensively treats the size
 * as 0 (no-op) if it's negative.
 */
inline void SafeStringCopy(char* destination, int32_t destinationSize, const char* source)
{
    assert(destinationSize >= 0);

    if (destinationSize > 0)
    {
        size_t unsignedSize = UnsignedCast(destinationSize);
        SafeStringCopy(destination, unsignedSize, source);
    }
}
