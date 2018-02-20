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

#if defined(__linux__) && defined(HAVE_SYS_AUXV_H)
#include <string.h>
#include <sys/auxv.h>
#elif defined(_AIX)
#include <sys/systemcfg.h>
#endif

void
mono_hwcap_arch_init (void)
{
#if defined(__linux__) && defined(HAVE_SYS_AUXV_H)
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
	if (__cpu64())
		mono_hwcap_ppc_is_isa_64 = TRUE;
	if (__power_4_andup())
		mono_hwcap_ppc_is_isa_2x = TRUE;
	if (__power_5_andup())
		mono_hwcap_ppc_has_icache_snoop = TRUE;
	/* not on POWER8 */
	if (__power_4() || __power_5() || __power_6() || __power_7())
		mono_hwcap_ppc_has_multiple_ls_units = TRUE;
	/*
	 * I dont see a way to get extended POWER6 and the PV_6_1
	 * def seems to be trigged on the POWER6 here despite not
	 * having these extended instructions, so POWER7 it is
	 */
	/*
	 * WARNING: reports that this doesn't actually work, try
	 * to re-enable after more investigation
	 */
	/*
	if (__power_7_andup())
		mono_hwcap_ppc_has_move_fpr_gpr = TRUE;
	 */
#endif
}
