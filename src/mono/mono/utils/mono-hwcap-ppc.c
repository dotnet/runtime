/*
 * mono-hwcap-ppc.c: PowerPC hardware feature detection
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

#include "mono/utils/mono-hwcap-ppc.h"

#if defined(__linux__) && defined(HAVE_SYS_AUXV_H)
#include <string.h>
#include <sys/auxv.h>
#endif

gboolean mono_hwcap_ppc_has_icache_snoop = FALSE;
gboolean mono_hwcap_ppc_is_isa_2x = FALSE;
gboolean mono_hwcap_ppc_is_isa_64 = FALSE;
gboolean mono_hwcap_ppc_has_move_fpr_gpr = FALSE;
gboolean mono_hwcap_ppc_has_multiple_ls_units = FALSE;

#if defined(MONO_CROSS_COMPILE)
void
mono_hwcap_arch_init (void)
{
}
#else
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
#endif
}
#endif

void
mono_hwcap_print (FILE* f)
{
	g_fprintf (f, "mono_hwcap_ppc_has_icache_snoop = %i\n", mono_hwcap_ppc_has_icache_snoop);
	g_fprintf (f, "mono_hwcap_ppc_is_isa_2x = %i\n", mono_hwcap_ppc_is_isa_2x);
	g_fprintf (f, "mono_hwcap_ppc_is_isa_64 = %i\n", mono_hwcap_ppc_is_isa_64);
	g_fprintf (f, "mono_hwcap_ppc_has_move_fpr_gpr = %i\n", mono_hwcap_ppc_has_move_fpr_gpr);
	g_fprintf (f, "mono_hwcap_ppc_has_multiple_ls_units = %i\n", mono_hwcap_ppc_has_multiple_ls_units);
}
