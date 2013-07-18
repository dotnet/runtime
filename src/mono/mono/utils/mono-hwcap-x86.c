/*
 * mono-hwcap-x86.c: x86 hardware feature detection
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
 */

#include "mono/utils/mono-hwcap-x86.h"

#if defined(HAVE_UNISTD_H)
#include <unistd.h>
#endif

#if defined(TARGET_X86)
#include "mono/utils/mono-codeman.h"

typedef void (* CpuidFunc) (int id, int *p_eax, int *p_ebx, int *p_ecx, int *p_edx);

static MonoCodeManager *code_man;
static CpuidFunc func;

#if defined(__native_client__)
static const guchar cpuid_impl [] = {
	0x55,								/* push   %ebp */
	0x89, 0xe5,							/* mov    %esp, %ebp */
	0x53,								/* push   %ebx */
	0x8b, 0x45, 0x08,					/* mov    0x8 (%ebp), %eax */
	0x0f, 0xa2,							/* cpuid   */
	0x50,								/* push   %eax */
	0x8b, 0x45, 0x10,					/* mov    0x10 (%ebp), %eax */
	0x89, 0x18,							/* mov    %ebx, (%eax) */
	0x8b, 0x45, 0x14,					/* mov    0x14 (%ebp), %eax */
	0x89, 0x08,							/* mov    %ecx, (%eax) */
	0x8b, 0x45, 0x18,					/* mov    0x18 (%ebp), %eax */
	0x89, 0x10,							/* mov    %edx, (%eax) */
	0x58,								/* pop    %eax */
	0x8b, 0x55, 0x0c,					/* mov    0xc (%ebp), %edx */
	0x89, 0x02,							/* mov    %eax, (%edx) */
	0x5b,								/* pop    %ebx */
	0xc9,								/* leave   */
	0x59, 0x83, 0xe1, 0xe0, 0xff, 0xe1,	/* naclret */
	0xf4, 0xf4, 0xf4, 0xf4, 0xf4, 0xf4,	/* padding, to provide bundle aligned version */
	0xf4, 0xf4, 0xf4, 0xf4, 0xf4, 0xf4,
	0xf4, 0xf4, 0xf4, 0xf4, 0xf4, 0xf4,
	0xf4, 0xf4, 0xf4, 0xf4, 0xf4, 0xf4,
	0xf4,
};
#else
static const guchar cpuid_impl [] = {
	0x55,								/* push   %ebp */
	0x89, 0xe5,							/* mov    %esp, %ebp */
	0x53,								/* push   %ebx */
	0x8b, 0x45, 0x08,					/* mov    0x8 (%ebp), %eax */
	0x0f, 0xa2,							/* cpuid   */
	0x50,								/* push   %eax */
	0x8b, 0x45, 0x10,					/* mov    0x10 (%ebp), %eax */
	0x89, 0x18,							/* mov    %ebx, (%eax) */
	0x8b, 0x45, 0x14,					/* mov    0x14 (%ebp), %eax */
	0x89, 0x08,							/* mov    %ecx, (%eax) */
	0x8b, 0x45, 0x18,					/* mov    0x18 (%ebp), %eax */
	0x89, 0x10,							/* mov    %edx, (%eax) */
	0x58,								/* pop    %eax */
	0x8b, 0x55, 0x0c,					/* mov    0xc (%ebp), %edx */
	0x89, 0x02,							/* mov    %eax, (%edx) */
	0x5b,								/* pop    %ebx */
	0xc9,								/* leave   */
	0xc3,								/* ret     */
};
#endif

#endif

gboolean mono_hwcap_x86_is_xen = FALSE;
gboolean mono_hwcap_x86_has_cmov = FALSE;
gboolean mono_hwcap_x86_has_fcmov = FALSE;
gboolean mono_hwcap_x86_has_sse1 = FALSE;
gboolean mono_hwcap_x86_has_sse2 = FALSE;
gboolean mono_hwcap_x86_has_sse3 = FALSE;
gboolean mono_hwcap_x86_has_ssse3 = FALSE;
gboolean mono_hwcap_x86_has_sse41 = FALSE;
gboolean mono_hwcap_x86_has_sse42 = FALSE;
gboolean mono_hwcap_x86_has_sse4a = FALSE;

static gboolean
cpuid (int id, int *p_eax, int *p_ebx, int *p_ecx, int *p_edx)
{
#if defined(TARGET_X86)

#if defined(__native_client__)
	func (id, p_eax, p_ebx, p_ecx, p_edx);

	return TRUE;
#else
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
		"pushfl\n"
		"popl %%eax\n"
		"movl %%eax, %%edx\n"
		"xorl $0x200000, %%eax\n"
		"pushl %%eax\n"
		"popfl\n"
		"pushfl\n"
		"popl %%eax\n"
		"xorl %%edx, %%eax\n"
		"andl $0x200000, %%eax\n"
		"movl %%eax, %0"
		: "=r" (have_cpuid)
		:
		: "%eax", "%edx"
	);
#endif

	if (have_cpuid) {
		func (id, p_eax, p_ebx, p_ecx, p_edx);

		return TRUE;
	}

	return FALSE;
#endif

#else

#if defined(_MSC_VER)
	int info [4];
	__cpuid (info, id);
	*p_eax = info [0];
	*p_ebx = info [1];
	*p_ecx = info [2];
	*p_edx = info [3];
#else
	__asm__ __volatile__ (
		"cpuid"
		: "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
		: "a" (id)
	);
#endif

	return TRUE;
#endif
}

void
mono_hwcap_arch_init (void)
{
#if defined(TARGET_X86)
	code_man = mono_code_manager_new ();

#if defined(__native_client__)
	gpointer ptr = mono_code_manager_reserve (code_man, sizeof (cpuid_impl));
	memcpy (ptr, cpuid_impl, sizeof (cpuid_impl));
	gpointer end_ptr = ptr + sizeof (cpuid_impl);

	guint8 *code = nacl_code_manager_get_dest (code_man, ptr);
	mono_code_manager_commit (code_man, ptr, sizeof (cpuid_impl), end_ptr - ptr);

	func = (CpuidFunc) code;
#else
	gpointer ptr = mono_code_manager_reserve (code_man, sizeof (cpuid_impl));
	memcpy (ptr, cpuid_impl, sizeof (cpuid_impl));

	func = (CpuidFunc) ptr;
#endif

#endif

	int eax, ebx, ecx, edx;

	if (cpuid (1, &eax, &ebx, &ecx, &edx)) {
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
	}

	if (cpuid (0x80000000, &eax, &ebx, &ecx, &edx)) {
		if ((unsigned int) eax >= 0x80000001 && ebx == 0x68747541 && ecx == 0x444D4163 && edx == 0x69746E65) {
			if (cpuid (0x80000001, &eax, &ebx, &ecx, &edx)) {
				if (ecx & (1 << 6))
					mono_hwcap_x86_has_sse4a = TRUE;
			}
		}
	}

#if defined(TARGET_X86)
	mono_code_manager_destroy (code_man);
#endif

#if defined(HAVE_UNISTD_H)
	mono_hwcap_x86_is_xen = !access ("/proc/xen", F_OK);
#endif
}
