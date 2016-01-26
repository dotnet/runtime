//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
// __forceinline implementation of the Interlocked class methods
//

#ifndef __GCENV_INTERLOCKED_INL__
#define __GCENV_INTERLOCKED_INL__

#ifdef _MSC_VER
#include <intrin.h>
#endif // _MSC_VER

// Increment the value of the specified 32-bit variable as an atomic operation.
// Parameters:
//  addend - variable to be incremented
// Return:
//  The resulting incremented value
template <typename T>
__forceinline T Interlocked::Increment(T volatile *addend)
{
#ifdef _MSC_VER
    static_assert(sizeof(long) == sizeof(T), "Size of long must be the same as size of T");
    return _InterlockedIncrement((long*)addend);
#else
    return __sync_add_and_fetch(addend, 1);
#endif
}

// Decrement the value of the specified 32-bit variable as an atomic operation.
// Parameters:
//  addend - variable to be decremented
// Return:
//  The resulting decremented value
template <typename T>
__forceinline T Interlocked::Decrement(T volatile *addend)
{
#ifdef _MSC_VER
    static_assert(sizeof(long) == sizeof(T), "Size of long must be the same as size of T");
    return _InterlockedDecrement((long*)addend);
#else
    return __sync_sub_and_fetch(addend, 1);
#endif
}

// Set a 32-bit variable to the specified value as an atomic operation. 
// Parameters:
//  destination - value to be exchanged
//  value       - value to set the destination to
// Return:
//  The previous value of the destination
template <typename T>
__forceinline T Interlocked::Exchange(T volatile *destination, T value)
{
#ifdef _MSC_VER
    static_assert(sizeof(long) == sizeof(T), "Size of long must be the same as size of T");
    return _InterlockedExchange((long*)destination, value);
#else
    return __sync_swap(destination, value);
#endif
}

// Performs an atomic compare-and-exchange operation on the specified values. 
// Parameters:
//  destination - value to be exchanged
//  exchange    - value to set the destinaton to
//  comparand   - value to compare the destination to before setting it to the exchange.
//                The destination is set only if the destination is equal to the comparand.
// Return:
//  The original value of the destination
template <typename T>
__forceinline T Interlocked::CompareExchange(T volatile *destination, T exchange, T comparand)
{
#ifdef _MSC_VER
    static_assert(sizeof(long) == sizeof(T), "Size of long must be the same as size of T");
    return _InterlockedCompareExchange((long*)destination, exchange, comparand);
#else
    return __sync_val_compare_and_swap(destination, comparand, exchange);
#endif
}

// Perform an atomic addition of two 32-bit values and return the original value of the addend.
// Parameters:
//  addend - variable to be added to
//  value  - value to add
// Return:
//  The previous value of the addend
template <typename T>
__forceinline T Interlocked::ExchangeAdd(T volatile *addend, T value)
{
#ifdef _MSC_VER
    static_assert(sizeof(long) == sizeof(T), "Size of long must be the same as size of T");
    return _InterlockedExchangeAdd((long*)addend, value);
#else
    return __sync_fetch_and_add(addend, value);
#endif
}

// Perform an atomic AND operation on the specified values values
// Parameters:
//  destination - the first operand and the destination
//  value       - second operand
template <typename T>
__forceinline void Interlocked::And(T volatile *destination, T value)
{
#ifdef _MSC_VER
    static_assert(sizeof(long) == sizeof(T), "Size of long must be the same as size of T");
    _InterlockedAnd((long*)destination, value);
#else
    __sync_and_and_fetch(destination, value);
#endif
}

// Perform an atomic OR operation on the specified values values
// Parameters:
//  destination - the first operand and the destination
//  value       - second operand
template <typename T>
__forceinline void Interlocked::Or(T volatile *destination, T value)
{
#ifdef _MSC_VER
    static_assert(sizeof(long) == sizeof(T), "Size of long must be the same as size of T");
    _InterlockedOr((long*)destination, value);
#else
    __sync_or_and_fetch(destination, value);
#endif
}

// Set a pointer variable to the specified value as an atomic operation.
// Parameters:
//  destination - value to be exchanged
//  value       - value to set the destination to
// Return:
//  The previous value of the destination
template <typename T>
__forceinline T Interlocked::ExchangePointer(T volatile * destination, T value)
{
#ifdef _MSC_VER
#ifdef BIT64
    return (T)(TADDR)_InterlockedExchangePointer((void* volatile *)destination, value);
#else
    return (T)(TADDR)_InterlockedExchange((long volatile *)(void* volatile *)destination, (long)(void*)value);
#endif
#else
    return (T)(TADDR)__sync_swap((void* volatile *)destination, value);
#endif
}

template <typename T>
__forceinline T Interlocked::ExchangePointer(T volatile * destination, std::nullptr_t value)
{
#ifdef _MSC_VER
#ifdef BIT64
    return (T)(TADDR)_InterlockedExchangePointer((void* volatile *)destination, value);
#else
    return (T)(TADDR)_InterlockedExchange((long volatile *)(void* volatile *)destination, (long)(void*)value);
#endif
#else
    return (T)(TADDR)__sync_swap((void* volatile *)destination, value);
#endif
}

// Performs an atomic compare-and-exchange operation on the specified pointers. 
// Parameters:
//  destination - value to be exchanged
//  exchange    - value to set the destinaton to
//  comparand   - value to compare the destination to before setting it to the exchange.
//                The destination is set only if the destination is equal to the comparand.
// Return:
//  The original value of the destination
template <typename T>
__forceinline T Interlocked::CompareExchangePointer(T volatile *destination, T exchange, T comparand)
{
#ifdef _MSC_VER
#ifdef BIT64
    return (T)(TADDR)_InterlockedCompareExchangePointer((void* volatile *)destination, exchange, comparand);
#else
    return (T)(TADDR)_InterlockedCompareExchange((long volatile *)(void* volatile *)destination, (long)(void*)exchange, (long)(void*)comparand);
#endif
#else
    return (T)(TADDR)__sync_val_compare_and_swap((void* volatile *)destination, comparand, exchange);
#endif
}

template <typename T>
__forceinline T Interlocked::CompareExchangePointer(T volatile *destination, T exchange, std::nullptr_t comparand)
{
#ifdef _MSC_VER
#ifdef BIT64
    return (T)(TADDR)_InterlockedCompareExchangePointer((void* volatile *)destination, (void*)exchange, (void*)comparand);
#else
    return (T)(TADDR)_InterlockedCompareExchange((long volatile *)(void* volatile *)destination, (long)(void*)exchange, (long)(void*)comparand);
#endif
#else
    return (T)(TADDR)__sync_val_compare_and_swap((void* volatile *)destination, (void*)comparand, (void*)exchange);
#endif
}

#endif // __GCENV_INTERLOCKED_INL__
