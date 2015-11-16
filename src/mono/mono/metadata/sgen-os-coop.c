/*
 * sgen-os-coop.c: SGen Cooperative backend support.
 *
 * Author:
 *	João Matos (joao.matos@xamarin.com)
 * Copyright (C) 2015 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
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

#endif
#endif