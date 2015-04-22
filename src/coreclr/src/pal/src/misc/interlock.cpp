//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    interlock.c

Abstract:

    Implementation of Interlocked functions for the Intel x86
    platform. These functions are processor dependent.



--*/

#include "pal/palinternal.h"


//
// We need the following methods to have volatile arguments for compatibility with Win32
//
#undef volatile


/*++
Function:
  InterlockedIncrement

The InterlockedIncrement function increments (increases by one) the
value of the specified variable and checks the resulting value. The
function prevents more than one thread from using the same variable
simultaneously.

Parameters

lpAddend 
       [in/out] Pointer to the variable to increment. 

Return Values

The return value is the resulting incremented value. 

--*/
LONG
PALAPI
InterlockedIncrement(
             IN OUT LONG volatile *lpAddend)
{
    return __sync_add_and_fetch(lpAddend, (LONG)1);
}

LONGLONG
PALAPI
InterlockedIncrement64(
             IN OUT LONGLONG volatile *lpAddend)
{
    return __sync_add_and_fetch(lpAddend, (LONGLONG)1);
}

/*++
Function:
  InterlockedDecrement

The InterlockedDecrement function decrements (decreases by one) the
value of the specified variable and checks the resulting value. The
function prevents more than one thread from using the same variable
simultaneously.

Parameters

lpAddend 
       [in/out] Pointer to the variable to decrement. 

Return Values

The return value is the resulting decremented value.

--*/
LONG
PALAPI
InterlockedDecrement(
             IN OUT LONG volatile *lpAddend)
{
    return __sync_sub_and_fetch(lpAddend, (LONG)1);
}

LONGLONG
PALAPI
InterlockedDecrement64(
             IN OUT LONGLONG volatile *lpAddend)
{
    return __sync_sub_and_fetch(lpAddend, (LONGLONG)1);
}

/*++
Function:
  InterlockedExchange

The InterlockedExchange function atomically exchanges a pair of
values. The function prevents more than one thread from using the same
variable simultaneously.

Parameters

Target 
       [in/out] Pointer to the value to exchange. The function sets
       this variable to Value, and returns its prior value.
Value 
       [in] Specifies a new value for the variable pointed to by Target. 

Return Values

The function returns the initial value pointed to by Target. 

--*/
LONG
PALAPI
InterlockedExchange(
            IN OUT LONG volatile *Target,
            IN LONG Value)
{
    return __sync_swap(Target, Value);
}

LONGLONG
PALAPI
InterlockedExchange64(
            IN OUT LONGLONG volatile *Target,
            IN LONGLONG Value)
{
    return __sync_swap(Target, Value);
}

/*++
Function:
  InterlockedCompareExchange

The InterlockedCompareExchange function performs an atomic comparison
of the specified values and exchanges the values, based on the outcome
of the comparison. The function prevents more than one thread from
using the same variable simultaneously.

If you are exchanging pointer values, this function has been
superseded by the InterlockedCompareExchangePointer function.

Parameters

Destination     [in/out] Specifies the address of the destination value. The sign is ignored. 
Exchange        [in]     Specifies the exchange value. The sign is ignored. 
Comperand       [in]     Specifies the value to compare to Destination. The sign is ignored. 

Return Values

The return value is the initial value of the destination.

--*/
LONG
PALAPI
InterlockedCompareExchange(
               IN OUT LONG volatile *Destination,
               IN LONG Exchange,
               IN LONG Comperand)
{
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}

LONG
PALAPI
InterlockedCompareExchangeAcquire(
               IN OUT LONG volatile *Destination,
               IN LONG Exchange,
               IN LONG Comperand)
{
    // TODO: implement the version with only the acquire semantics
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}

LONG
PALAPI
InterlockedCompareExchangeRelease(
               IN OUT LONG volatile *Destination,
               IN LONG Exchange,
               IN LONG Comperand)
{
    // TODO: implement the version with only the release semantics
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}
               
// See the 32-bit variant in interlock2.s
LONGLONG
PALAPI
InterlockedCompareExchange64(
               IN OUT LONGLONG volatile *Destination,
               IN LONGLONG Exchange,
               IN LONGLONG Comperand)
{
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}

/*++
Function:
InterlockedExchangeAdd

The InterlockedExchangeAdd function atomically adds the value of 'Value'
to the variable that 'Addend' points to.

Parameters

lpAddend
[in/out] Pointer to the variable to to added.

Return Values

The return value is the original value that 'Addend' pointed to.

--*/
LONG
PALAPI
InterlockedExchangeAdd(
               IN OUT LONG volatile *Addend,
               IN LONG Value)
{
    return __sync_fetch_and_add(Addend, Value);
}

LONGLONG
PALAPI
InterlockedExchangeAdd64(
               IN OUT LONGLONG volatile *Addend,
               IN LONGLONG Value)
{
    return __sync_fetch_and_add(Addend, Value);
}
             
LONG
PALAPI
InterlockedAnd(
               IN OUT LONG volatile *Destination,
               IN LONG Value)
{
    return __sync_fetch_and_and(Destination, Value);
}
              
LONG
PALAPI
InterlockedOr(
              IN OUT LONG volatile *Destination,
              IN LONG Value)
{
    return __sync_fetch_and_or(Destination, Value);
}

UCHAR
PALAPI
InterlockedBitTestAndReset(
               IN OUT LONG volatile *Base,
               IN LONG Bit)
{
    return (InterlockedAnd(Base, ~(1 << Bit)) & (1 << Bit)) != 0;
}

UCHAR
PALAPI
InterlockedBitTestAndSet(
               IN OUT LONG volatile *Base,
               IN LONG Bit)
{
    return (InterlockedOr(Base, (1 << Bit)) & (1 << Bit)) != 0;
}

/*++
Function:
MemoryBarrier

The MemoryBarrier function creates a full memory barrier.

--*/
void
PALAPI
MemoryBarrier(
    VOID)
{
    __sync_synchronize();
}

#define volatile DoNotUseVolatileKeyword
