/*
 * sgen-os-coop.c: SGen Cooperative backend support.
 *
 * Author:
 *	João Matos (joao.matos@xamarin.com)
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#ifdef HAVE_SGEN_GC


#include <glib.h>
#include "sgen/sgen-gc.h"
#include "sgen/sgen-archdep.h"
#include "sgen/sgen-protocol.h"
#include "metadata/object-internals.h"
#include "metadata/gc-internals.h"


#if defined(USE_COOP_GC)

gboolean
sgen_resume_thread (SgenThreadInfo *info)
{
	g_error ("FIXME");
	return FALSE;
}

gboolean
sgen_suspend_thread (SgenThreadInfo *info)
{
	g_error ("FIXME");
	return FALSE;
}

void
sgen_wait_for_suspend_ack (int count)
{
}

/* LOCKING: assumes the GC lock is held */
int
sgen_thread_handshake (BOOL suspend)
{
	g_error ("FIXME");
	return 0;
}

void
sgen_os_init (void)
{
}

int
mono_gc_get_suspend_signal (void)
{
	return -1;
}

int
mono_gc_get_restart_signal (void)
{
	return -1;
}
#else
	#ifdef _MSC_VER
		// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
		void __mono_win32_sgen_os_coop_quiet_lnk4221(void) {}
	#endif
#endif /* USE_COOP_GC */
#endif
