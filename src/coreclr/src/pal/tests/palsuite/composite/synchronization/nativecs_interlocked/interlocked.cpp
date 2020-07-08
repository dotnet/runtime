// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


typedef long LONG;

extern "C" { 
LONG InterlockedCompareExchange(
    LONG volatile *Destination,
    LONG Exchange,
    LONG Comperand)
{
#ifdef i386
    LONG result;

    __asm__ __volatile__(
             "lock; cmpxchgl %2,(%1)"
             : "=a" (result)
             : "r" (Destination), "r" (Exchange), "0" (Comperand)
             : "memory"
        );

    return result;
#endif
}
}
