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

int x86_cpu_has_sse2;
int x86_cpu_has_sse42;
int x86_cpu_has_pclmulqdq;

#ifndef _MSC_VER
void x86_check_features(void)
{
    unsigned eax, ebx, ecx, edx;

    eax = 1;
    __asm__ __volatile__ (
#ifdef X86
        "xchg %%ebx, %1\n\t"
#endif
        "cpuid\n\t"
#ifdef X86
        "xchg %1, %%ebx\n\t"
    : "+a" (eax), "=S" (ebx), "=c" (ecx), "=d" (edx)
#else
    : "+a" (eax), "=b" (ebx), "=c" (ecx), "=d" (edx)
#endif
    );

    x86_cpu_has_sse2 = edx & 0x4000000;
    x86_cpu_has_sse42= ecx & 0x100000;
    x86_cpu_has_pclmulqdq = ecx & 0x2;
}
#elif _MSC_VER >= 1400 /* Visual Studio 2005 (first ref on MSDN to __cpuid() */
#include <intrin.h>

void x86_check_features(void)
{
    int regs[4];

    __cpuid(regs, 1);

    x86_cpu_has_sse2 = regs[3] & 0x4000000;
    x86_cpu_has_sse42= regs[2] & 0x100000;
    x86_cpu_has_pclmulqdq = regs[2] & 0x2;
}
#else
void x86_check_features(void)
{
}
#endif
