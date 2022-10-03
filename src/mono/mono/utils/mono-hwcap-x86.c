/**
 * \file
 * x86 hardware feature detection
 *
 * Authors:
 *    Alex Rønne Petersen (alexrp@xamarin.com)
 *    Elijah Taylor (elijahtaylor@google.com)
 *    Miguel de Icaza (miguel@xamarin.com)
 *    Neale Ferguson (Neale.Ferguson@SoftwareAG-usa.com)
 *    Paolo Molaro (lupus@xamarin.com)
 *    Rodrigo Kumpera (kumpera@gmail.com)
 *    Sebastien Pouliot (sebastien@xamarin.com)
 *    Zoltan Varga (vargaz@xamarin.com)
 *
 * Copyright 2003 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc
 * Copyright 2006 Broadcom
 * Copyright 2007-2008 Andreas Faerber
 * Copyright 2011-2013 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mono/utils/mono-hwcap.h"

#if defined(HAVE_UNISTD_H)
#include <unistd.h>
#endif
#if defined(_MSC_VER)
#include <intrin.h>
#endif

gboolean
mono_hwcap_x86_call_cpuidex (int id, int sub_id, int *p_eax, int *p_ebx, int *p_ecx, int *p_edx)
{
#if defined(_MSC_VER)
	int info [4];
#endif

	/* First, make sure we can use cpuid if we're on 32-bit. */
#if defined(TARGET_X86)
	gboolean have_cpuid = FALSE;

#if defined(_MSC_VER)
	__asm {
		pushfd
		pop eax
		mov edx, eax
		xor eax, 0x200000
		push eax
		popfd
		pushfd
		pop eax
		xor eax, edx
		and eax, 0x200000
		mov have_cpuid, eax
	}
#else
	__asm__ __volatile__ (
		"pushfl\n\t"
		"popl\t%%eax\n\t"
		"movl\t%%eax, %%edx\n\t"
		"xorl\t$0x200000, %%eax\n\t"
		"pushl\t%%eax\n\t"
		"popfl\n\t"
		"pushfl\n\t"
		"popl\t%%eax\n\t"
		"xorl\t%%edx, %%eax\n\t"
		"andl\t$0x200000, %%eax\n\t"
		"movl\t%%eax, %0\n\t"
		: "=r" (have_cpuid)
		:
		: "%eax", "%edx"
	);
#endif

	if (!have_cpuid)
		return FALSE;
#endif

	/* Now issue the actual cpuid instruction. We can use
	   MSVC's __cpuid on both 32-bit and 64-bit. */
#if defined(_MSC_VER)
	__cpuidex (info, id, sub_id);
	*p_eax = info [0];
	*p_ebx = info [1];
	*p_ecx = info [2];
	*p_edx = info [3];
#elif defined(TARGET_X86)
	/* This complicated stuff is necessary because EBX
	   may be used by the compiler in PIC mode. */
	__asm__ __volatile__ (
		"xchgl\t%%ebx, %k1\n\t"
		"cpuid\n\t"
		"xchgl\t%%ebx, %k1\n\t"
		: "=a" (*p_eax), "=&r" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
		: "0" (id), "2" (sub_id)
	);
#else
	__asm__ __volatile__ (
		"cpuid\n\t"
		: "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
		: "a" (id), "2" (sub_id)
	);
#endif

	return TRUE;
}

void
mono_hwcap_arch_init (void)
{
	int eax, ebx, ecx, edx;

	if (mono_hwcap_x86_call_cpuidex (1, 0, &eax, &ebx, &ecx, &edx)) {
		if (edx & (1 << 15)) {
			mono_hwcap_x86_has_cmov = TRUE;

			if (edx & 1)
				mono_hwcap_x86_has_fcmov = TRUE;
		}

		if (edx & (1 << 25))
			mono_hwcap_x86_has_sse1 = TRUE;

		if (edx & (1 << 26))
			mono_hwcap_x86_has_sse2 = TRUE;

		if (ecx & (1 << 0))
			mono_hwcap_x86_has_sse3 = TRUE;

		if (ecx & (1 << 9))
			mono_hwcap_x86_has_ssse3 = TRUE;

		if (ecx & (1 << 19))
			mono_hwcap_x86_has_sse41 = TRUE;

		if (ecx & (1 << 20))
			mono_hwcap_x86_has_sse42 = TRUE;

		if (ecx & (1 << 23))
			mono_hwcap_x86_has_popcnt = TRUE;

		if (ecx & (1 << 28))
			mono_hwcap_x86_has_avx = TRUE;
	}

	if (mono_hwcap_x86_call_cpuidex (0x80000000, 0, &eax, &ebx, &ecx, &edx)) {
		if ((unsigned int) eax >= 0x80000001 && ebx == 0x68747541 && ecx == 0x444D4163 && edx == 0x69746E65) {
			if (mono_hwcap_x86_call_cpuidex (0x80000001, 0, &eax, &ebx, &ecx, &edx)) {
				if (ecx & (1 << 6))
					mono_hwcap_x86_has_sse4a = TRUE;
			}
		}
	}

	if (mono_hwcap_x86_call_cpuidex (0x80000001, 0, &eax, &ebx, &ecx, &edx)) {
		if (ecx & (1 << 5))
			mono_hwcap_x86_has_lzcnt = TRUE;
	}
}
