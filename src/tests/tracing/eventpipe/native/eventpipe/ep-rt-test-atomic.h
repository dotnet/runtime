// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Copyright (C) 2002 Ximian, Inc.
// Copyright 2012 Xamarin Inc.

#ifndef EP_RT_TEST_ATOMIC_H
#define EP_RT_TEST_ATOMIC_H

#include <limits.h>
#include <stddef.h>
#include <stdint.h>

#if defined(_WIN32) && defined(_MSC_VER)
#include <windows.h>
#include <intrin.h>

static inline void
ep_test_atomic_load_barrier(void)
{
#if defined(_M_ARM) || defined(_M_ARM64)
    MemoryBarrier();
#else
    _ReadWriteBarrier();
#endif
}

static inline int32_t
ep_rt_atomic_inc_int32_t(volatile int32_t *value)
{
    return (int32_t)InterlockedIncrement((volatile LONG *)value);
}

static inline int32_t
ep_rt_atomic_dec_int32_t(volatile int32_t *value)
{
    return (int32_t)InterlockedDecrement((volatile LONG *)value);
}

static inline int64_t
ep_rt_atomic_inc_int64_t(volatile int64_t *value)
{
    return (int64_t)InterlockedIncrement64((volatile LONG64 *)value);
}

static inline int64_t
ep_rt_atomic_dec_int64_t(volatile int64_t *value)
{
    return (int64_t)InterlockedDecrement64((volatile LONG64 *)value);
}

static inline int64_t
ep_rt_atomic_compare_exchange_int64_t(volatile int64_t *target, int64_t expected, int64_t value)
{
    return (int64_t)InterlockedCompareExchange64((volatile LONG64 *)target, value, expected);
}

static inline size_t
ep_rt_atomic_compare_exchange_size_t(volatile size_t *target, size_t expected, size_t value)
{
#ifdef _WIN64
    return (size_t)InterlockedCompareExchange64((volatile LONG64 *)target, (LONG64)value, (LONG64)expected);
#else
    return (size_t)InterlockedCompareExchange((volatile LONG *)target, (LONG)value, (LONG)expected);
#endif
}

static inline uint32_t
ep_rt_volatile_load_uint32_t_without_barrier(const volatile uint32_t *ptr)
{
    return *ptr;
}

static inline void
ep_rt_volatile_store_uint32_t_without_barrier(volatile uint32_t *ptr, uint32_t value)
{
    *ptr = value;
}

static inline int64_t
ep_rt_volatile_load_int64_t_without_barrier(const volatile int64_t *ptr)
{
#ifdef _WIN64
    return *ptr;
#else
    // A plain 64-bit access can tear on a 32-bit host.
    return (int64_t)InterlockedCompareExchange64((volatile LONG64 *)ptr, 0, 0);
#endif
}

static inline void
ep_rt_volatile_store_int64_t_without_barrier(volatile int64_t *ptr, int64_t value)
{
#ifdef _WIN64
    *ptr = value;
#else
    InterlockedExchange64((volatile LONG64 *)ptr, value);
#endif
}

static inline uint32_t
ep_rt_volatile_load_uint32_t(const volatile uint32_t *ptr)
{
    uint32_t value = ep_rt_volatile_load_uint32_t_without_barrier(ptr);
    ep_test_atomic_load_barrier();
    return value;
}

static inline int64_t
ep_rt_volatile_load_int64_t(const volatile int64_t *ptr)
{
    int64_t value = ep_rt_volatile_load_int64_t_without_barrier(ptr);
    ep_test_atomic_load_barrier();
    return value;
}

static inline void
ep_rt_volatile_store_uint32_t(volatile uint32_t *ptr, uint32_t value)
{
    InterlockedExchange((volatile LONG *)ptr, (LONG)value);
}

static inline void
ep_rt_volatile_store_int64_t(volatile int64_t *ptr, int64_t value)
{
    InterlockedExchange64((volatile LONG64 *)ptr, value);
}

static inline void *
ep_rt_volatile_load_ptr_without_barrier(volatile void **ptr)
{
    return *(void *volatile *)ptr;
}

static inline void
ep_rt_volatile_store_ptr_without_barrier(volatile void **ptr, void *value)
{
    *(void *volatile *)ptr = value;
}

static inline void *
ep_rt_volatile_load_ptr(volatile void **ptr)
{
    void *value = ep_rt_volatile_load_ptr_without_barrier(ptr);
    ep_test_atomic_load_barrier();
    return value;
}

static inline void
ep_rt_volatile_store_ptr(volatile void **ptr, void *value)
{
    InterlockedExchangePointer((void *volatile *)ptr, value);
}

#else
#include <stdatomic.h>

_Static_assert(sizeof(atomic_int) == sizeof(int32_t) && ATOMIC_INT_LOCK_FREE == 2,
    "Standalone EventPipe tests require lock-free 32-bit atomics");
_Static_assert(sizeof(atomic_uint) == sizeof(uint32_t),
    "Standalone EventPipe tests require matching atomic integer sizes");
_Static_assert(sizeof(_Atomic(void *)) == sizeof(void *) && ATOMIC_POINTER_LOCK_FREE == 2,
    "Standalone EventPipe tests require lock-free pointer atomics");

#if LONG_MAX == INT64_MAX
typedef atomic_long ep_test_atomic_int64_t;
_Static_assert(ATOMIC_LONG_LOCK_FREE == 2, "Standalone EventPipe tests require lock-free 64-bit atomics");
#elif LLONG_MAX == INT64_MAX
typedef atomic_llong ep_test_atomic_int64_t;
_Static_assert(ATOMIC_LLONG_LOCK_FREE == 2, "Standalone EventPipe tests require lock-free 64-bit atomics");
#else
#error No matching 64-bit atomic integer type
#endif
_Static_assert(sizeof(ep_test_atomic_int64_t) == sizeof(int64_t),
    "Standalone EventPipe tests require matching 64-bit atomic sizes");
_Static_assert(sizeof(_Atomic(size_t)) == sizeof(size_t),
    "Standalone EventPipe tests require matching atomic size_t sizes");

static inline void
ep_test_atomic_seq_cst_barrier(void)
{
#if defined(__aarch64__)
    atomic_thread_fence(memory_order_seq_cst);
#endif
}

static inline int32_t
ep_rt_atomic_inc_int32_t(volatile int32_t *value)
{
    int32_t old = atomic_fetch_add((volatile atomic_int *)value, 1);
    ep_test_atomic_seq_cst_barrier();
    return (int32_t)((uint32_t)old + 1);
}

static inline int32_t
ep_rt_atomic_dec_int32_t(volatile int32_t *value)
{
    int32_t old = atomic_fetch_sub((volatile atomic_int *)value, 1);
    ep_test_atomic_seq_cst_barrier();
    return (int32_t)((uint32_t)old - 1);
}

static inline int64_t
ep_rt_atomic_inc_int64_t(volatile int64_t *value)
{
    int64_t old = atomic_fetch_add((volatile ep_test_atomic_int64_t *)value, 1);
    ep_test_atomic_seq_cst_barrier();
    return (int64_t)((uint64_t)old + 1);
}

static inline int64_t
ep_rt_atomic_dec_int64_t(volatile int64_t *value)
{
    int64_t old = atomic_fetch_sub((volatile ep_test_atomic_int64_t *)value, 1);
    ep_test_atomic_seq_cst_barrier();
    return (int64_t)((uint64_t)old - 1);
}

static inline int64_t
ep_rt_atomic_compare_exchange_int64_t(volatile int64_t *target, int64_t expected, int64_t value)
{
    atomic_compare_exchange_strong((volatile ep_test_atomic_int64_t *)target, &expected, value);
    ep_test_atomic_seq_cst_barrier();
    return expected;
}

static inline size_t
ep_rt_atomic_compare_exchange_size_t(volatile size_t *target, size_t expected, size_t value)
{
    atomic_compare_exchange_strong((volatile _Atomic(size_t) *)target, &expected, value);
    ep_test_atomic_seq_cst_barrier();
    return expected;
}

static inline uint32_t
ep_rt_volatile_load_uint32_t_without_barrier(const volatile uint32_t *ptr)
{
    return atomic_load_explicit((const volatile atomic_uint *)ptr, memory_order_relaxed);
}

static inline void
ep_rt_volatile_store_uint32_t_without_barrier(volatile uint32_t *ptr, uint32_t value)
{
    atomic_store_explicit((volatile atomic_uint *)ptr, value, memory_order_relaxed);
}

static inline int64_t
ep_rt_volatile_load_int64_t_without_barrier(const volatile int64_t *ptr)
{
    return atomic_load_explicit((const volatile ep_test_atomic_int64_t *)ptr, memory_order_relaxed);
}

static inline void
ep_rt_volatile_store_int64_t_without_barrier(volatile int64_t *ptr, int64_t value)
{
    atomic_store_explicit((volatile ep_test_atomic_int64_t *)ptr, value, memory_order_relaxed);
}

static inline uint32_t
ep_rt_volatile_load_uint32_t(const volatile uint32_t *ptr)
{
    ep_test_atomic_seq_cst_barrier();
    return atomic_load((const volatile atomic_uint *)ptr);
}

static inline int64_t
ep_rt_volatile_load_int64_t(const volatile int64_t *ptr)
{
    ep_test_atomic_seq_cst_barrier();
    return atomic_load((const volatile ep_test_atomic_int64_t *)ptr);
}

static inline void
ep_rt_volatile_store_uint32_t(volatile uint32_t *ptr, uint32_t value)
{
    atomic_store((volatile atomic_uint *)ptr, value);
    ep_test_atomic_seq_cst_barrier();
}

static inline void
ep_rt_volatile_store_int64_t(volatile int64_t *ptr, int64_t value)
{
    atomic_store((volatile ep_test_atomic_int64_t *)ptr, value);
    ep_test_atomic_seq_cst_barrier();
}

static inline void *
ep_rt_volatile_load_ptr_without_barrier(volatile void **ptr)
{
    return atomic_load_explicit((const volatile _Atomic(void *) *)ptr, memory_order_relaxed);
}

static inline void
ep_rt_volatile_store_ptr_without_barrier(volatile void **ptr, void *value)
{
    atomic_store_explicit((volatile _Atomic(void *) *)ptr, value, memory_order_relaxed);
}

static inline void *
ep_rt_volatile_load_ptr(volatile void **ptr)
{
    ep_test_atomic_seq_cst_barrier();
    return atomic_load((const volatile _Atomic(void *) *)ptr);
}

static inline void
ep_rt_volatile_store_ptr(volatile void **ptr, void *value)
{
    atomic_store((volatile _Atomic(void *) *)ptr, value);
    ep_test_atomic_seq_cst_barrier();
}
#endif

static inline uint32_t
ep_rt_atomic_inc_uint32_t(volatile uint32_t *value)
{
    return (uint32_t)ep_rt_atomic_inc_int32_t((volatile int32_t *)value);
}

static inline uint32_t
ep_rt_atomic_dec_uint32_t(volatile uint32_t *value)
{
    return (uint32_t)ep_rt_atomic_dec_int32_t((volatile int32_t *)value);
}

static inline uint64_t
ep_rt_volatile_load_uint64_t(const volatile uint64_t *ptr)
{
    return (uint64_t)ep_rt_volatile_load_int64_t((const volatile int64_t *)ptr);
}

static inline uint64_t
ep_rt_volatile_load_uint64_t_without_barrier(const volatile uint64_t *ptr)
{
    return (uint64_t)ep_rt_volatile_load_int64_t_without_barrier((const volatile int64_t *)ptr);
}

static inline void
ep_rt_volatile_store_uint64_t(volatile uint64_t *ptr, uint64_t value)
{
    ep_rt_volatile_store_int64_t((volatile int64_t *)ptr, (int64_t)value);
}

static inline void
ep_rt_volatile_store_uint64_t_without_barrier(volatile uint64_t *ptr, uint64_t value)
{
    ep_rt_volatile_store_int64_t_without_barrier((volatile int64_t *)ptr, (int64_t)value);
}

#endif
