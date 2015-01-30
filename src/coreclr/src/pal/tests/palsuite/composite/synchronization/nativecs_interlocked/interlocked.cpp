//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//


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
