// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_ATOMIC_H
#define HAVE_MINIPAL_ATOMIC_H

#ifdef HOST_WINDOWS
#include <windows.h>
#else
#include <stdatomic.h>
#if ATOMIC_POINTER_LOCK_FREE != 2
#pragma message("C11 atomic pointer types are not lock free on targeted platform")
#endif
#endif

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * @brief Atomic compares and exchange a pointer value.
 *
 * @param dest  Pointer to the destination pointer to compare and swap.
 * @param exch  The value to store if the comparison succeeds.
 * @param comp  The value to compare against.
 *
 * @return The previous value of the pointer.
 */
static inline void* minipal_atomic_compare_exchange_ptr(volatile void** dest, void* exch, void* comp)
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
