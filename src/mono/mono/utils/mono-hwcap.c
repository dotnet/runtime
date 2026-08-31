/**
 * \file
 * Hardware feature detection
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

#include <stdlib.h>
#include <string.h>

#include "mono/utils/mono-hwcap.h"

#define MONO_HWCAP_VAR(NAME) gboolean mono_hwcap_ ## NAME = FALSE;
#include "mono/utils/mono-hwcap-vars.h"
#undef MONO_HWCAP_VAR

static gboolean hwcap_inited = FALSE;

void
mono_hwcap_init (void)
{
	char *verbose = g_getenv ("MONO_VERBOSE_HWCAP");
	char *conservative = g_getenv ("MONO_CONSERVATIVE_HWCAP");

	if (hwcap_inited)
		return;

	if (!conservative || strncmp (conservative, "1", 1))
		mono_hwcap_arch_init ();

	if (verbose && !strncmp (verbose, "1", 1))
		mono_hwcap_print ();

	g_free (verbose);
	g_free (conservative);
}

void
mono_hwcap_print (void)
{
	g_print ("[mono-hwcap] Detected following hardware capabilities:\n\n");

#define MONO_HWCAP_VAR(NAME) g_print ("\t" #NAME " = %s\n", mono_hwcap_ ## NAME ? "yes" : "no");
#include "mono/utils/mono-hwcap-vars.h"
#undef MONO_HWCAP_VAR

	g_print ("\n");
}
