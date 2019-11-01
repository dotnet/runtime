// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include "pal_config.h"

#include <assert.h>
#include <errno.h>
#include <stddef.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>
#include <unistd.h>
#include <limits.h>

#ifdef DEBUG
#define assert_err(cond, msg, err) do \
{ \
  if(!(cond)) \
  { \
    fprintf(stderr, "%s (%d): error %d: %s. %s (%s failed)\n", __FILE__, __LINE__, err, msg, strerror(err), #cond); \
    assert(false && "assert_err failed"); \
  } \
} while(0)
#define assert_msg(cond, msg, val) do \
{ \
  if(!(cond)) \
  { \
    fprintf(stderr, "%s (%d): error %d: %s (%s failed)\n", __FILE__, __LINE__, val, msg, #cond); \
    assert(false && "assert_msg failed"); \
  } \
} while(0)
#else // DEBUG
#define assert_err(cond, msg, err)
#define assert_msg(cond, msg, val)
#endif // DEBUG

#define sizeof_member(type,member) sizeof(((type*)NULL)->member)

// See https://stackoverflow.com/questions/51231405
#define CONST_CAST2(TOTYPE, FROMTYPE, X) ((union { FROMTYPE _q; TOTYPE _nq; }){ ._q = (X) }._nq)
#define CONST_CAST(TYPE, X) CONST_CAST2(TYPE, const TYPE, (X))

#define ARRAY_SIZE(a) (sizeof(a)/sizeof(a[0]))

/**
 * Abstraction helper method to safely copy strings using strlcpy or strcpy_s
 * or a different safe copy method, depending on the current platform.
 */
inline static void SafeStringCopy(char* destination, size_t destinationSize, const char* source)
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
* Converts an intptr_t to a file descriptor.
* intptr_t is the type used to marshal file descriptors so we can use SafeHandles effectively.
*/
inline static int ToFileDescriptorUnchecked(intptr_t fd)
{
    return (int)fd;
}

/**
* Converts an intptr_t to a file descriptor.
* intptr_t is the type used to marshal file descriptors so we can use SafeHandles effectively.
*/
inline static int ToFileDescriptor(intptr_t fd)
{
    assert(0 <= fd && fd < sysconf(_SC_OPEN_MAX));

    return ToFileDescriptorUnchecked(fd);
}

static inline bool CheckInterrupted(ssize_t result)
{
    return result < 0 && errno == EINTR;
}

inline static uint32_t Int32ToUint32(int32_t value)
{
    assert(value >= 0);
    return (uint32_t)value;
}

inline static size_t Int32ToSizeT(int32_t value)
{
    assert(value >= 0);
    return (size_t)value;
}

inline static int32_t Uint32ToInt32(uint32_t value)
{
    assert(value <= INT_MAX);
    return (int32_t)value;
}

inline static int32_t SizeTToInt32(size_t value)
{
    assert(value <= INT_MAX);
    return (int32_t)value;
}
