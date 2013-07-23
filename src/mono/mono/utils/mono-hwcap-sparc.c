/*
 * mono-hwcap-sparc.c: SPARC hardware feature detection
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

#include "mono/utils/mono-hwcap-sparc.h"

#include <string.h>

#if !defined(__linux__)
#include <sys/systeminfo.h>
#else
#include <unistd.h>
#endif

gboolean mono_hwcap_sparc_is_v9 = FALSE;

#if defined(MONO_CROSS_COMPILE)
void
mono_hwcap_arch_init (void)
{
}
#else
void
mono_hwcap_arch_init (void)
{
	char buf [1024];

#if !defined(__linux__)
	if (!sysinfo (SI_ISALIST, buf, 1024))
		g_assert_not_reached ();
#else
	/* If the page size is 8192, we're on a 64-bit SPARC, which
	 * in turn means a v9 or better.
	 */
	if (getpagesize () == 8192)
		strcpy (buf, "sparcv9")
	else
		strcpy (buf, "sparcv8")
#endif

	mono_hwcap_sparc_is_v9 = strstr (buf, "sparcv9");
}
#endif

void
mono_hwcap_print (FILE *f)
{
	g_fprintf (f, "mono_hwcap_sparc_is_v9 = %i\n", mono_hwcap_sparc_is_v9);
}
