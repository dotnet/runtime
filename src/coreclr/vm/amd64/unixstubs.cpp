// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

extern "C"
{
    void RedirectForThrowControl()
    {
        PORTABILITY_ASSERT("Implement for PAL");
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

    DWORD avx512StateSupport()
    {
        DWORD eax;
        __asm("  xgetbv\n" \
            : "=a"(eax) /*output in eax*/\
            : "c"(0) /*inputs - 0 in ecx*/\
            : "edx" /* registers that are clobbered*/
          );
        // check OS has enabled XMM, YMM and ZMM state support
        return ((eax & 0x0E6) == 0x0E6) ? 1 : 0;
    }

    void STDMETHODCALLTYPE JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
    {
    }
};
