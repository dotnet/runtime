// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

/*++
Function:
XmmYmmStateSupport

Check if OS has enabled both XMM and YMM state support

Return value:
1 if XMM and YMM are enabled, 0 otherwise
--*/
extern "C" unsigned int XmmYmmStateSupport()
{
    unsigned int eax;
    __asm("  mov $1, %%eax\n" \
          "  cpuid\n" \
          "  xor %%eax, %%eax\n" \
          "  and $0x18000000, %%ecx\n" /* check for xsave feature set and that it is enabled by the OS */ \
          "  cmp $0x18000000, %%ecx\n" \
          "  jne end\n" \
          "  xor %%ecx, %%ecx\n" \
          "  xgetbv\n" \
          "end:\n" \
        : "=a"(eax) /* output in eax */ \
        : /* no inputs */ \
        : "eax", "ebx", "ecx", "edx" /* registers that are clobbered */
      );
    // Check OS has enabled both XMM and YMM state support
    return ((eax & 0x06) == 0x06) ? 1 : 0;
}
