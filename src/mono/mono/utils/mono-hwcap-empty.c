/*
 * mono-hwcap-empty.c: Dummy file with no feature detection
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

#include "mono/utils/mono-hwcap.h"

void
mono_hwcap_init (void)
{
	/* When the runtime is built as a cross compiler, we don't want to do
	 * any CPU feature detection since we're most likely not running on the
	 * same kind of CPU as the one the resulting code will run on.
	 *
	 * This file is also used for architectures that haven't specified a
	 * mono-hwcap-$TARGET.c file in Makefile.am.
	 */
}
