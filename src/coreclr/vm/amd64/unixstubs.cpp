// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

extern "C"
{
    void RedirectForThrowControl()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

#if !__has_builtin(__cpuid)
    void __cpuid(int cpuInfo[4], int function_id)
    {
        // Based on the Clang implementation provided in cpuid.h:
        // https://github.com/llvm/llvm-project/blob/master/clang/lib/Headers/cpuid.h

        __asm("  cpuid\n" \
            : "=a"(cpuInfo[0]), "=b"(cpuInfo[1]), "=c"(cpuInfo[2]), "=d"(cpuInfo[3]) \
            : "0"(function_id)
        );
    }
#endif

#if !__has_builtin(__cpuidex)
    void __cpuidex(int cpuInfo[4], int function_id, int subFunction_id)
    {
        // Based on the Clang implementation provided in cpuid.h:
        // https://github.com/llvm/llvm-project/blob/master/clang/lib/Headers/cpuid.h

        __asm("  cpuid\n" \
            : "=a"(cpuInfo[0]), "=b"(cpuInfo[1]), "=c"(cpuInfo[2]), "=d"(cpuInfo[3]) \
            : "0"(function_id), "2"(subFunction_id)
        );
    }
#endif

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
