/*
 * mono-hwcap-s390x.c: S/390x hardware feature detection
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

#include "mono/utils/mono-hwcap-s390x.h"

#include <signal.h>

gboolean mono_hwcap_s390x_has_ld = FALSE;

#if defined(MONO_CROSS_COMPILE)
void
mono_hwcap_arch_init (void)
{
}
#else
static void
catch_sigill (int sig_no, siginfo_t *info, gpointer act)
{
	mono_hwcap_s390x_has_ld = FALSE;
}

void
mono_hwcap_arch_init (void)
{
	mono_hwcap_s390x_has_ld = TRUE;

	struct sigaction sa, *old_sa;

	/* Determine if we have a long displacement facility
	 * by executing the STY instruction. If it fails, we
	 * catch the SIGILL and assume the answer is no.
	 */
	sa.sa_sigaction = catch_sigill;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_SIGINFO;

	sigaction (SIGILL, &sa, old_sa);

	__asm__ __volatile__ (
		"LGHI\t0,1\n\t"
		"LA\t1,%0\n\t"
		".byte\t0xe3,0x00,0x10,0x00,0x00,0x50\n\t"
		: "=m" (mono_hwcap_s390x_has_ld)
		:
		: "0", "1"
	);

	sigaction (SIGILL, old_sa, NULL);
}
#endif

void
mono_hwcap_print (FILE *f)
{
	g_fprintf (f, "mono_hwcap_s390x_has_ld = %i\n", mono_hwcap_s390x_has_ld);
}
