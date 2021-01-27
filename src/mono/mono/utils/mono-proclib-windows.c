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
#include <mono/utils/mono-proclib.h>
#include <mono/utils/w32subset.h>

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
 */
#if HAVE_API_SUPPORT_WIN32_GET_SYSTEM_TIMES
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
#elif HAVE_API_SUPPORT_WIN32_GET_SYSTEM_TIME_AS_FILE_TIME && HAVE_API_SUPPORT_WIN32_GET_PROCESS_TIMES
gint32
mono_cpu_usage (MonoCpuUsageState *prev)
{
	gint32 cpu_usage = 0;
	gint64 cpu_total_time;
	gint64 cpu_busy_time;
	guint64 idle_time;
	guint64 kernel_time;
	guint64 user_time;
	guint64 current_time;
	guint64 creation_time;
	guint64 exit_time;

	GetSystemTimeAsFileTime ((FILETIME*)&current_time);
	if (!GetProcessTimes (GetCurrentProcess (), (FILETIME*)&creation_time, (FILETIME*)&exit_time, (FILETIME*)&kernel_time, (FILETIME*)&user_time)) {
		g_error ("GetProcessTimes() failed, error code is %d\n", GetLastError ());
		return -1;
	}

	// GetProcessTimes user_time is a sum of user time spend by all threads in the process.
	// This means that the total user time can be more than real time. In order to adjust for this
	// the total available time that we can be scheduled depends on the number of available cores.
	// For example, having 2 threads running 100% on a 2 core system for 100 ms will return a user_time of 200ms
	// but the current_time - creation_time will only be 100ms but by adjusting the available time based on number of
	// of availalbe cores will gives use the total load of the process.
	guint64 total_available_time = (current_time - creation_time) * mono_cpu_count ();

	idle_time = total_available_time - (kernel_time + user_time);

	cpu_total_time = (gint64)((idle_time - (prev ? prev->idle_time : 0)) + (user_time - (prev ? prev->user_time : 0)) + (kernel_time - (prev ? prev->kernel_time : 0)));
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
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_SYSTEM_TIMES && !HAVE_EXTERN_DEFINED_WIN32_GET_SYSTEM_TIME_AS_FILE_TIME && !HAVE_EXTERN_DEFINED_WIN32_GET_PROCESS_TIMES
gint32
mono_cpu_usage (MonoCpuUsageState *prev)
{
	g_unsupported_api ("GetSystemTimes, GetSystemTimeAsFileTime, GetProcessTimes");
	SetLastError (ERROR_NOT_SUPPORTED);
	return -1;
}
#endif /* HAVE_API_SUPPORT_WIN32_GET_SYSTEM_TIMES */

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_proclib_windows);

#endif /* HOST_WIN32*/
