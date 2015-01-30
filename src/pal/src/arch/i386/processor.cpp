//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    processor.cpp

Abstract:

    Implementation of processor related functions for the Intel x86/x64
    platforms. These functions are processor dependent.



--*/

#include "pal/palinternal.h"

/*++
Function:
YieldProcessor

The YieldProcessor function signals to the processor to give resources
to threads that are waiting for them. This macro is only effective on
processors that support technology allowing multiple threads running
on a single processor, such as Intel's Hyper-Threading technology.

--*/
void
PALAPI
YieldProcessor(
    VOID)
{
    __asm__ __volatile__ (
        "rep\n"
        "nop"
    );
}

