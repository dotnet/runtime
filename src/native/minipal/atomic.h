// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_ATOMIC_H
#define HAVE_MINIPAL_ATOMIC_H

#ifdef HOST_WINDOWS
#include <windows.h>
#else
#include <stdatomic.h>
#if ATOMIC_POINTER_LOCK_FREE != 2 || ATOMIC_INT_LOCK_FREE != 2
#pragma message("C11 atomic pointer and int types are not lock free on targeted platform")
#endif
#endif

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * @brief Atomically loads an int value with memory ordering guarantees.
 *
 * @param ptr   Pointer to the int to load.
 *
 * @return The loaded int value.
 */
static inline int minipal_atomic_load_int(volatile int* value)
{
#ifdef HOST_WINDOWS
    volatile int loaded_value = *value;
    _ReadWriteBarrier();
    return (int)loaded_value;
#else
    atomic_thread_fence(memory_order_seq_cst);
    return (int)atomic_load((volatile atomic_int *)value);
#endif
}

/**
 * @brief Atomically loads a pointer value with memory ordering guarantees.
 *
 * @param ptr   Pointer to the pointer to load.
 *
 * @return The loaded pointer value.
 */
static inline void* minipal_atomic_load_ptr(volatile void** ptr)
{
#ifdef HOST_WINDOWS
    volatile void* loaded_value = *ptr;
    _ReadWriteBarrier();
    return (void*)loaded_value;
#else
    atomic_thread_fence(memory_order_seq_cst);
    return (void*)atomic_load((volatile _Atomic(void *) *)ptr);
#endif
}

/**
 * @brief Atomically stores a pointer value with memory ordering guarantees.
 *
 * @param dest      Pointer to the destination pointer to store to.
 * @param value     The value to store.
 */
static inline void minipal_atomic_store_ptr(volatile void** dest, void* value)
{
#ifdef HOST_WINDOWS
    InterlockedExchangePointer((PVOID volatile *)dest, (PVOID)value);
#else
    atomic_store((volatile _Atomic(void *) *)dest, value);
    atomic_thread_fence(memory_order_seq_cst);
#endif
}

/**
 * @brief Atomically stores an int value with memory ordering guarantees.
 *
 * @param dest      Pointer to the destination int to store to.
 * @param value     The value to store.
 */
static inline void minipal_atomic_store_int(volatile int* dest, int value)
{
#ifdef HOST_WINDOWS
    _InterlockedExchange((LONG volatile *)dest, (LONG)value);
#else
    atomic_store((volatile atomic_int *)dest, value);
    atomic_thread_fence(memory_order_seq_cst);
#endif
}

/**
 * @brief Atomically compares and swaps a pointer value.
 *
 * @param dest  Pointer to the destination pointer to compare and swap.
 * @param exch  The value to store if the comparison succeeds.
 * @param comp  The value to compare against.
 *
 * @return The previous value of the pointer.
 */
static inline void* minipal_atomic_cas_ptr(volatile void** dest, void* exch, void* comp)
{
#ifdef HOST_WINDOWS
    return InterlockedCompareExchangePointer((PVOID volatile *)dest, (PVOID)exch, (PVOID)comp);
#else
    atomic_compare_exchange_strong((volatile _Atomic(void *) *)dest, &comp, exch);
    atomic_thread_fence(memory_order_seq_cst);
    return comp;
#endif
}

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_ATOMIC_H */
