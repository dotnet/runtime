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

#ifdef __cplusplus
#include <limits>
#include <type_traits>
#endif

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

#ifdef __cplusplus
#define sizeof_member(type,member) sizeof(type::member)
#else
#define sizeof_member(type,member) sizeof(((type*)NULL)->member)
#endif

#ifdef __cplusplus

/**
 * ResultOf<T> is shorthand for typename std::result_of<T>::type.
 * Equivalent to C++14 std::result_of_t.
 */
template <typename T>
using ResultOf = typename std::result_of<T>::type;

/**
 * EnableIf<B, T> is shorthand for typename std::enable_of<B, T>::type.
 * Equivalent to C++14 std::enable_if_t.
 */
template <bool B, typename T = void>
using EnableIf = typename std::enable_if<B, T>::type;

/**
 * NonVoidResultOf<F> evaluates to non-void return type of F.
 * Causes substitution failure if F returns void.
 */
template <typename F>
using NonVoidResultOf = EnableIf<!std::is_void<ResultOf<F>>::value, ResultOf<F>>;

/**
 * ReplaceVoid<F, T> evaluates to T if F returns void.
 * Causes substitution failure if F does not return void.
 */
template <typename F, typename T>
using ReplaceVoidResultOf = EnableIf<std::is_void<ResultOf<F>>::value, T>;

/**
 * Cast an unsigned integer value to the appropriately sized signed integer type.
 *
 * We use this when we've already ensured that the value is within the
 * signed range, but we don't want to cast to a specific signed type as that could
 * inadvertently defeat the compiler's narrowing conversion warnings
 * (which we treat as error).
 */
template <typename T>
inline typename std::make_signed<T>::type SignedCast(T value)
{
    assert(value <= std::numeric_limits<typename std::make_signed<T>::type>::max());
    return static_cast<typename std::make_signed<T>::type>(value);
}

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
 * Clang doesn't have an ARRAY_SIZE macro so use the solution from
 * MSDN blogs: http://blogs.msdn.com/b/the1/archive/2004/05/07/128242.aspx
 */
template <typename T, size_t N>
char(&_ArraySizeHelper(T(&array)[N]))[N];
#define ARRAY_SIZE(array) (sizeof(_ArraySizeHelper(array)))

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

#endif // __cplusplus

/**
* Converts an intptr_t to a file descriptor.
* intptr_t is the type used to marshal file descriptors so we can use SafeHandles effectively.
*/
inline static int ToFileDescriptorUnchecked(intptr_t fd)
{
#ifdef __cplusplus
    return static_cast<int>(fd);
#else
    return (int)fd;
#endif // __cplusplus
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

#ifdef __cplusplus

/**
* Checks if the IO operation was interupted and needs to be retried.
* Returns true if the operation was interupted; otherwise, false.
*/
template <typename TInt>
static inline bool CheckInterrupted(TInt result)
{
    return result < 0 && errno == EINTR;
}

#endif // __cplusplus
