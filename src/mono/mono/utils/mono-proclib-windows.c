/**
 * \file
 * Windows proclib support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include <windows.h>
#include "mono/utils/mono-proclib.h"

int
mono_process_current_pid ()
{
	return (int) GetCurrentProcessId ();
}

/**
 * mono_cpu_count:
 * \returns the number of processors on the system.
 */
int
mono_cpu_count (void)
{
	SYSTEM_INFO info;
	GetSystemInfo (&info);
	return info.dwNumberOfProcessors;
}

/*
 * This function returns the cpu usage in percentage,
 * normalized on the number of cores.
 *
 * Warning : the percentage returned can be > 100%. This
 * might happens on systems like Android which, for
 * battery and performance reasons, shut down cores and
 * lie about the number of active cores.
 */
#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
gint32
mono_cpu_usage (MonoCpuUsageState *prev)
{
	gint32 cpu_usage = 0;
	gint64 cpu_total_time;
	gint64 cpu_busy_time;
	guint64 idle_time;
	guint64 kernel_time;
	guint64 user_time;

	if (!GetSystemTimes ((FILETIME*) &idle_time, (FILETIME*) &kernel_time, (FILETIME*) &user_time)) {
		g_error ("GetSystemTimes() failed, error code is %d\n", GetLastError ());
		return -1;
	}

	cpu_total_time = (gint64)((user_time - (prev ? prev->user_time : 0)) + (kernel_time - (prev ? prev->kernel_time : 0)));
	cpu_busy_time = (gint64)(cpu_total_time - (idle_time - (prev ? prev->idle_time : 0)));

	if (prev) {
		prev->idle_time = idle_time;
		prev->kernel_time = kernel_time;
		prev->user_time = user_time;
	}

	if (cpu_total_time > 0 && cpu_busy_time > 0)
		cpu_usage = (gint32)(cpu_busy_time * 100 / cpu_total_time);

	return cpu_usage;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */
#endif /* HOST_WIN32*/
