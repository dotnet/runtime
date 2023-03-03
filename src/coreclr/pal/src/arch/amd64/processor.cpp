// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        : "ebx", "ecx", "edx" /* registers that are clobbered */
      );
    // Check OS has enabled both XMM and YMM state support
    return ((eax & 0x06) == 0x06) ? 1 : 0;
}

/*++
Function:
Avx512StateSupport

Check if OS has enabled XMM, YMM and ZMM state support

Return value:
1 if XMM, YMM and ZMM are enabled, 0 otherwise
--*/
extern "C" unsigned int Avx512StateSupport()
{
    unsigned int eax;
    __asm("  mov $1, %%eax\n" \
          "  cpuid\n" \
          "  xor %%eax, %%eax\n" \
          "  and $0x18000000, %%ecx\n" /* check for xsave feature set and that it is enabled by the OS */ \
          "  cmp $0x18000000, %%ecx\n" \
          "  jne endz\n" \
          "  xor %%ecx, %%ecx\n" \
          "  xgetbv\n" \
          "endz:\n" \
        : "=a"(eax) /* output in eax */ \
        : /* no inputs */ \
        : "ebx", "ecx", "edx" /* registers that are clobbered */
      );
    // Check OS has enabled XMM, YMM and ZMM state support
    return ((eax & 0x0E6) == 0x0E6) ? 1 : 0;
}
