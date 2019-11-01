// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

extern "C"
{
    void RedirectForThrowControl()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    DWORD getcpuid(DWORD arg, unsigned char result[16])
    {
        DWORD eax;
        __asm("  xor %%ecx, %%ecx\n" \
              "  cpuid\n" \
              "  mov %%eax, 0(%[result])\n" \
              "  mov %%ebx, 4(%[result])\n" \
              "  mov %%ecx, 8(%[result])\n" \
              "  mov %%edx, 12(%[result])\n" \
            : "=a"(eax) /*output in eax*/\
            : "a"(arg), [result]"r"(result) /*inputs - arg in eax, result in any register*/\
            : "rbx", "ecx", "edx", "memory" /* registers that are clobbered, *result is clobbered */
          );
        return eax;
    }
    
    DWORD getextcpuid(DWORD arg1, DWORD arg2, unsigned char result[16])
    {
        DWORD eax;
        __asm("  cpuid\n" \
              "  mov %%eax, 0(%[result])\n" \
              "  mov %%ebx, 4(%[result])\n" \
              "  mov %%ecx, 8(%[result])\n" \
              "  mov %%edx, 12(%[result])\n" \
            : "=a"(eax) /*output in eax*/\
            : "c"(arg1), "a"(arg2), [result]"r"(result) /*inputs - arg1 in ecx, arg2 in eax, result in any register*/\
            : "rbx", "edx", "memory" /* registers that are clobbered, *result is clobbered */
          );
        return eax;
    }

    DWORD xmmYmmStateSupport()
    {
        DWORD eax;
        __asm("  xgetbv\n" \
            : "=a"(eax) /*output in eax*/\
            : "c"(0) /*inputs - 0 in ecx*/\
            : "edx" /* registers that are clobbered*/
          );
        // check OS has enabled both XMM and YMM state support
        return ((eax & 0x06) == 0x06) ? 1 : 0;
    }

    void STDMETHODCALLTYPE JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
    {
    }
};
