// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_ATOMIC_H
#define HAVE_MINIPAL_ATOMIC_H

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>

#ifdef HOST_WINDOWS
#include <windows.h>
#include <intrin.h>
#else
// GCC/Clang
#endif

#ifdef __cplusplus
extern "C" {
#endif

static inline uint32_t minipal_atomic_load_u32(const volatile uint32_t *ptr)
{
#ifdef HOST_WINDOWS
    return *(const volatile uint32_t *)ptr;
#else
    return __atomic_load_n(ptr, __ATOMIC_ACQUIRE);
#endif
}

static inline void minipal_atomic_increment_u32(volatile uint32_t *ptr)
{
#ifdef HOST_WINDOWS
    InterlockedIncrement((volatile long*)ptr);
#else
    __atomic_fetch_add(ptr, 1u, __ATOMIC_RELAXED);
#endif
}

static inline void* minipal_atomic_load_ptr(void * volatile *ptr)
{
#ifdef HOST_WINDOWS
    return (void*)InterlockedCompareExchangePointer(ptr, NULL, NULL);
#else
    return __atomic_load_n(ptr, __ATOMIC_ACQUIRE);
#endif
}

static inline void* minipal_atomic_exchange_ptr(void * volatile *ptr, void *value)
{
#ifdef HOST_WINDOWS
    return InterlockedExchangePointer(ptr, value);
#else
    return __atomic_exchange_n(ptr, value, __ATOMIC_ACQ_REL);
#endif
}

static inline void minipal_atomic_store_u32(volatile uint32_t *ptr, uint32_t value)
{
#ifdef HOST_WINDOWS
    *(volatile uint32_t *)ptr = value;
#else
    __atomic_store_n(ptr, value, __ATOMIC_RELEASE);
#endif
}

static inline size_t minipal_atomic_load_size(const volatile size_t *ptr)
{
#ifdef HOST_WINDOWS
    return *(const volatile size_t *)ptr;
#else
    return __atomic_load_n(ptr, __ATOMIC_ACQUIRE);
#endif
}

static inline void minipal_atomic_store_size(volatile size_t *ptr, size_t value)
{
#ifdef HOST_WINDOWS
    *(volatile size_t *)ptr = value;
#else
    __atomic_store_n(ptr, value, __ATOMIC_RELEASE);
#endif
}

static inline size_t minipal_atomic_add_size(volatile size_t *ptr, size_t value)
{
#ifdef HOST_WINDOWS
#if defined(_WIN64)
    return (size_t)_InterlockedExchangeAdd64((volatile long long *)ptr, (long long)value);
#else
    return (size_t)_InterlockedExchangeAdd((volatile long *)ptr, (long)value);
#endif
#else
    return __atomic_fetch_add(ptr, value, __ATOMIC_ACQ_REL);
#endif
}

static inline size_t minipal_atomic_sub_size(volatile size_t *ptr, size_t value)
{
#ifdef HOST_WINDOWS
#if defined(_WIN64)
    return (size_t)_InterlockedExchangeAdd64((volatile long long *)ptr, -(long long)value);
#else
    return (size_t)_InterlockedExchangeAdd((volatile long *)ptr, -(long)value);
#endif
#else
    return __atomic_fetch_sub(ptr, value, __ATOMIC_ACQ_REL);
#endif
}

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_ATOMIC_H
