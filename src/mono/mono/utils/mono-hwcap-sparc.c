/**
 * \file
 * SPARC hardware feature detection
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

#include <string.h>
#if !defined(__linux__)
#include <sys/systeminfo.h>
#else
#include <unistd.h>
#endif

void
mono_hwcap_arch_init (void)
{
	char buf [1024];

#if !defined(__linux__)
	g_assert (sysinfo (SI_ISALIST, buf, 1024));
#else
	/* If the page size is 8192, we're on a 64-bit SPARC, which
	 * in turn means a v9 or better.
	 */
	if (getpagesize () == 8192)
		strcpy (buf, "sparcv9");
	else
		strcpy (buf, "sparcv8");
#endif

	mono_hwcap_sparc_is_v9 = strstr (buf, "sparcv9");
}
