/**
 * \file
 * PowerPC hardware feature detection
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

#if defined(__linux__) && HAVE_GETAUXVAL
#include <string.h>
#include <sys/auxv.h>
#elif defined(_AIX)

#include <sys/systemcfg.h>

#if !defined(POWER_4_ANDUP)
#define POWER_4_ANDUP (POWER_4|POWER_5)
#endif

#if !defined(__power_4_andup)
#define __power_4_andup() (_system_configuration.implementation & POWER_4_ANDUP)
#endif

#if !defined(__power_5_andup)
#define __power_5_andup() (_system_configuration.implementation & POWER_5_ANDUP)
#endif

#endif

void
mono_hwcap_arch_init (void)
{
#if defined(__linux__) && HAVE_GETAUXVAL
	unsigned long hwcap;
	unsigned long platform;

	if ((hwcap = getauxval(AT_HWCAP))) {
		/* PPC_FEATURE_ICACHE_SNOOP */
		if (hwcap & 0x00002000)
			mono_hwcap_ppc_has_icache_snoop = TRUE;

		/* PPC_FEATURE_POWER4, PPC_FEATURE_POWER5, PPC_FEATURE_POWER5_PLUS,
		   PPC_FEATURE_CELL_BE, PPC_FEATURE_PA6T, PPC_FEATURE_ARCH_2_05 */
		if (hwcap & (0x00080000 | 0x00040000 | 0x00020000 | 0x00010000 | 0x00000800 | 0x00001000))
			mono_hwcap_ppc_is_isa_2x = TRUE;

		/* PPC_FEATURE_POWER4, PPC_FEATURE_POWER5, PPC_FEATURE_POWER5_PLUS,
		   PPC_FEATURE_CELL_BE, PPC_FEATURE_PA6T, PPC_FEATURE_ARCH_2_05 */
		if (hwcap & (0x00080000 | 0x00040000 | 0x00020000 | 0x00010000 | 0x00000800 | 0x00001000))
			mono_hwcap_ppc_is_isa_2x = TRUE;

		/* PPC_FEATURE_POWER5, PPC_FEATURE_POWER5_PLUS,
		   PPC_FEATURE_CELL_BE, PPC_FEATURE_PA6T, PPC_FEATURE_ARCH_2_05 */
		if (hwcap & (0x00040000 | 0x00020000 | 0x00010000 | 0x00000800 | 0x00001000))
			mono_hwcap_ppc_is_isa_2_03 = TRUE;

		/* PPC_FEATURE_64 */
		if (hwcap & 0x40000000)
			mono_hwcap_ppc_is_isa_64 = TRUE;

		/* PPC_FEATURE_POWER6_EXT */
		if (hwcap & 0x00000200)
			mono_hwcap_ppc_has_move_fpr_gpr = TRUE;
	}

	if ((platform = getauxval(AT_PLATFORM))) {
		const char *str = (const char *) platform;

		if (!strcmp (str, "ppc970") || (!strncmp (str, "power", 5) && str [5] >= '4' && str [5] <= '7'))
			mono_hwcap_ppc_has_multiple_ls_units = TRUE;
	}
#elif defined(_AIX)
	/* Compatible platforms for Mono (V7R1, 6.1.9) require at least P4. */
	mono_hwcap_ppc_is_isa_64 = TRUE;
	mono_hwcap_ppc_is_isa_2x = TRUE;
	if (__power_5_andup()) {
		mono_hwcap_ppc_is_isa_2_03 = TRUE;
		mono_hwcap_ppc_has_icache_snoop = TRUE;
	}
	/* not on POWER8 */
	if (__power_4() || __power_5() || __power_6() || __power_7())
		mono_hwcap_ppc_has_multiple_ls_units = TRUE;
	/*
	 * This instruction is only available in POWER6 "raw mode" and unlikely
	 * to work; I couldn't get it to work on the POWER6s I tried.
	 */
	/*
	if (__power_6())
		mono_hwcap_ppc_has_move_fpr_gpr = TRUE;
	 */
#endif
}
