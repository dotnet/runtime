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

facilityList_t facs;

void
mono_hwcap_arch_init (void)
{
	int lFacs = sizeof(facs) / 8;

	__asm__ ("	lgfr	0,%1\n"
		 "	.insn	s,0xb2b00000,%0\n"
		 : "=m" (facs) : "r" (lFacs) : "0", "cc");
}

void
mono_hwcap_print (FILE *f)
{
}
