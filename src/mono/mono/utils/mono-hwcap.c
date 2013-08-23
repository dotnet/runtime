/*
 * mono-hwcap.c: Hardware feature detection
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

#include <stdlib.h>

#include "mono/utils/mono-hwcap.h"

static gboolean hwcap_inited = FALSE;

void
mono_hwcap_init (void)
{
	const char *verbose = g_getenv ("MONO_VERBOSE_HWCAP");

	if (hwcap_inited)
		return;

	mono_hwcap_arch_init ();

	if (verbose)
		mono_hwcap_print (stdout);
}
