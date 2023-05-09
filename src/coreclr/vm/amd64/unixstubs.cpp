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

#ifndef XSTATE_MASK_AVX512
#define XSTATE_MASK_AVX512 (0xE0) /* 0b1110_0000 */
#endif // XSTATE_MASK_AVX512

    DWORD avx512StateSupport()
    {
#if defined(TARGET_OSX)
        // MacOS has specialized behavior where it reports AVX512 support but doesnt
        // actually enable AVX512 until the first instruction is executed and does so
        // on a per thread basis. It does this by catching the faulting instruction and
        // checking for the EVEX encoding. The kmov instructions, despite being part
        // of the AVX512 instruction set are VEX encoded and dont trigger the enablement
        //
        // See https://github.com/apple/darwin-xnu/blob/main/osfmk/i386/fpu.c#L174

        // TODO-AVX512: Enabling this for OSX requires ensuring threads explicitly trigger
        // the AVX-512 enablement so that arbitrary usage doesn't cause downstream problems

        return false;
#else
        DWORD eax;
        __asm("  xgetbv\n" \
            : "=a"(eax) /*output in eax*/\
            : "c"(0) /*inputs - 0 in ecx*/\
            : "edx" /* registers that are clobbered*/
          );
        // check OS has enabled XMM, YMM and ZMM state support
        return ((eax & 0x0E6) == 0x0E6) ? 1 : 0;
#endif
    }

    void STDMETHODCALLTYPE JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
    {
    }
};
