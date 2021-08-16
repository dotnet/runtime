/*
 * x86 feature check
 *
 * Copyright (C) 2013 Intel Corporation. All rights reserved.
 * Author:
 *  Jim Kukunas
 *
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include "x86.h"

#ifdef ZLIB_X86

int x86_cpu_has_sse2;
int x86_cpu_has_sse42;
int x86_cpu_has_pclmul;

#ifdef _MSC_VER
#include <intrin.h>
#else
#include <cpuid.h>
#endif

#ifndef bit_SSE2
# define bit_SSE2 0x4000000
#endif

#ifndef bit_SSE4_2
# define bit_SSE4_2 0x100000
#endif

#ifndef bit_PCLMUL
# define bit_PCLMUL 0x2
#endif

void x86_check_features(void)
{
    static int once;

    enum reg { A = 0, B = 1, C = 2, D = 3};
    int regs[4];

    if (once != 0)
        return;
    once = 1;

#ifdef _MSC_VER
    __cpuid(regs, 1);
#else
    __cpuid(1, regs[A], regs[B], regs[C], regs[D]);
#endif

    x86_cpu_has_sse2 = regs[D] & bit_SSE2;
    x86_cpu_has_sse42= regs[C] & bit_SSE4_2;
    x86_cpu_has_pclmul=regs[C] & bit_PCLMUL;
}

#endif
