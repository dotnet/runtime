/*
 * mono-threads-windows.c: Low-level threading, windows version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include "config.h"

#if defined(HOST_WIN32)

#include <mono/utils/mono-threads.h>


void
mono_threads_init_platform (void)
{
}

void
mono_threads_core_interrupt (MonoThreadInfo *info)
{
	g_assert (0);
}

void
mono_threads_core_self_suspend (MonoThreadInfo *info)
{
	g_assert (0);
}

gboolean
mono_threads_core_suspend (MonoThreadInfo *info)
{
	g_assert (0);
}

gboolean
mono_threads_core_resume (MonoThreadInfo *info)
{
	g_assert (0);
}

void
mono_threads_platform_register (MonoThreadInfo *info)
{
}

void
mono_threads_platform_free (MonoThreadInfo *info)
{
}

#endif
