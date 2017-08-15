/**
 * \file
 * Copyright 2008-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include "utils/mono-proclib.h"
#include "utils/mono-time.h"

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SCHED_GETAFFINITY
#include <sched.h>
#endif

#if defined(_POSIX_VERSION)
#ifdef HAVE_SYS_ERRNO_H
#include <sys/errno.h>
#endif
#include <sys/param.h>
#include <errno.h>
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif
#ifdef HAVE_SYS_SYSCTL_H
#include <sys/sysctl.h>
#endif
#include <sys/resource.h>
#endif
#if defined(__HAIKU__)
#include <os/kernel/OS.h>
#endif
#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__OpenBSD__) || defined(__NetBSD__)
#include <sys/proc.h>
#if defined(__APPLE__)
#include <mach/mach.h>
#endif
#ifdef HAVE_SYS_USER_H
#include <sys/user.h>
#endif
#ifdef HAVE_STRUCT_KINFO_PROC_KP_PROC
#    define kinfo_starttime_member kp_proc.p_starttime
#    define kinfo_pid_member kp_proc.p_pid
#    define kinfo_name_member kp_proc.p_comm
#elif defined(__NetBSD__)
#    define kinfo_starttime_member p_ustart_sec
#    define kinfo_pid_member p_pid
#    define kinfo_name_member p_comm
#elif defined(__OpenBSD__)
// Can not figure out how to get the proc's start time on OpenBSD
#    undef kinfo_starttime_member 
#    define kinfo_pid_member p_pid
#    define kinfo_name_member p_comm
#else
#define kinfo_starttime_member ki_start
#define kinfo_pid_member ki_pid
#define kinfo_name_member ki_comm
#endif
#define USE_SYSCTL 1
#endif

#ifdef HAVE_SCHED_GETAFFINITY
#  ifndef GLIBC_HAS_CPU_COUNT
static int
CPU_COUNT(cpu_set_t *set)
{
	int i, count = 0;

	for (int i = 0; i < CPU_SETSIZE; i++)
		if (CPU_ISSET(i, set))
			count++;
	return count;
}
#  endif
#endif

/**
 * mono_process_list:
 * \param size a pointer to a location where the size of the returned array is stored
 * \returns an array of pid values for the processes currently running on the system.
 * The size of the array is stored in \p size.
 */
gpointer*
mono_process_list (int *size)
{
#if USE_SYSCTL
	int res, i;
#ifdef KERN_PROC2
	int mib [6];
	size_t data_len = sizeof (struct kinfo_proc2) * 400;
	struct kinfo_proc2 *processes = g_malloc (data_len);
#else
	int mib [4];
	size_t data_len = sizeof (struct kinfo_proc) * 16;
	struct kinfo_proc *processes;
	int limit = 8;
#endif /* KERN_PROC2 */
	void **buf = NULL;

	if (size)
		*size = 0;

#ifdef KERN_PROC2
	if (!processes)
		return NULL;

	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC2;
	mib [2] = KERN_PROC_ALL;
	mib [3] = 0;
	mib [4] = sizeof(struct kinfo_proc2);
	mib [5] = 400; /* XXX */

	res = sysctl (mib, 6, processes, &data_len, NULL, 0);
	if (res < 0) {
		g_free (processes);
		return NULL;
	}
#else
	processes = NULL;
	while (limit) {
		mib [0] = CTL_KERN;
		mib [1] = KERN_PROC;
		mib [2] = KERN_PROC_ALL;
		mib [3] = 0;

		res = sysctl (mib, 3, NULL, &data_len, NULL, 0);
		if (res)
			return NULL;
		processes = (struct kinfo_proc *) g_malloc (data_len);
		res = sysctl (mib, 3, processes, &data_len, NULL, 0);
		if (res < 0) {
			g_free (processes);
			if (errno != ENOMEM)
				return NULL;
			limit --;
		} else {
			break;
		}
	}
#endif /* KERN_PROC2 */

#ifdef KERN_PROC2
	res = data_len/sizeof (struct kinfo_proc2);
#else
	res = data_len/sizeof (struct kinfo_proc);
#endif /* KERN_PROC2 */
	buf = (void **) g_realloc (buf, res * sizeof (void*));
	for (i = 0; i < res; ++i)
		buf [i] = GINT_TO_POINTER (processes [i].kinfo_pid_member);
	g_free (processes);
	if (size)
		*size = res;
	return buf;
#elif defined(__HAIKU__)
	int32 cookie = 0;
	int32 i = 0;
	team_info ti;
	system_info si;

	get_system_info(&si);
	void **buf = g_calloc(si.used_teams, sizeof(void*));

	while (get_next_team_info(&cookie, &ti) == B_OK && i < si.used_teams) {
		buf[i++] = GINT_TO_POINTER (ti.team);
	}
	*size = i;

	return buf;
#else
	const char *name;
	void **buf = NULL;
	int count = 0;
	int i = 0;
	GDir *dir = g_dir_open ("/proc/", 0, NULL);
	if (!dir) {
		if (size)
			*size = 0;
		return NULL;
	}
	while ((name = g_dir_read_name (dir))) {
		int pid;
		char *nend;
		pid = strtol (name, &nend, 10);
		if (pid <= 0 || nend == name || *nend)
			continue;
		if (i >= count) {
			if (!count)
				count = 16;
			else
				count *= 2;
			buf = (void **)g_realloc (buf, count * sizeof (void*));
		}
		buf [i++] = GINT_TO_POINTER (pid);
	}
	g_dir_close (dir);
	if (size)
		*size = i;
	return buf;
#endif
}

static G_GNUC_UNUSED char*
get_pid_status_item_buf (int pid, const char *item, char *rbuf, int blen, MonoProcessError *error)
{
	char buf [256];
	char *s;
	FILE *f;
	size_t len = strlen (item);

	g_snprintf (buf, sizeof (buf), "/proc/%d/status", pid);
	f = fopen (buf, "r");
	if (!f) {
		if (error)
			*error = MONO_PROCESS_ERROR_NOT_FOUND;
		return NULL;
	}
	while ((s = fgets (buf, sizeof (buf), f))) {
		if (*item != *buf)
			continue;
		if (strncmp (buf, item, len))
			continue;
		s = buf + len;
		while (g_ascii_isspace (*s)) s++;
		if (*s++ != ':')
			continue;
		while (g_ascii_isspace (*s)) s++;
		fclose (f);
		len = strlen (s);
		memcpy (rbuf, s, MIN (len, blen));
		rbuf [MIN (len, blen) - 1] = 0;
		if (error)
			*error = MONO_PROCESS_ERROR_NONE;
		return rbuf;
	}
	fclose (f);
	if (error)
		*error = MONO_PROCESS_ERROR_OTHER;
	return NULL;
}

#if USE_SYSCTL

#ifdef KERN_PROC2
#define KINFO_PROC struct kinfo_proc2
#else
#define KINFO_PROC struct kinfo_proc
#endif

static gboolean
sysctl_kinfo_proc (gpointer pid, KINFO_PROC* processi)
{
	int res;
	size_t data_len = sizeof (KINFO_PROC);

#ifdef KERN_PROC2
	int mib [6];
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC2;
	mib [2] = KERN_PROC_PID;
	mib [3] = GPOINTER_TO_UINT (pid);
	mib [4] = sizeof(KINFO_PROC);
	mib [5] = 400; /* XXX */

	res = sysctl (mib, 6, processi, &data_len, NULL, 0);
#else
	int mib [4];
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_PID;
	mib [3] = GPOINTER_TO_UINT (pid);

	res = sysctl (mib, 4, processi, &data_len, NULL, 0);
#endif /* KERN_PROC2 */

	if (res < 0 || data_len != sizeof (KINFO_PROC))
		return FALSE;

	return TRUE;
}
#endif /* USE_SYSCTL */

/**
 * mono_process_get_name:
 * \param pid pid of the process
 * \param buf byte buffer where to store the name of the prcoess
 * \param len size of the buffer \p buf
 * \returns the name of the process identified by \p pid, storing it
 * inside \p buf for a maximum of len bytes (including the terminating 0).
 */
char*
mono_process_get_name (gpointer pid, char *buf, int len)
{
#if USE_SYSCTL
	KINFO_PROC processi;

	memset (buf, 0, len);

	if (sysctl_kinfo_proc (pid, &processi))
		memcpy (buf, processi.kinfo_name_member, len - 1);

	return buf;
#else
	char fname [128];
	FILE *file;
	char *p;
	size_t r;
	sprintf (fname, "/proc/%d/cmdline", GPOINTER_TO_INT (pid));
	buf [0] = 0;
	file = fopen (fname, "r");
	if (!file)
		return buf;
	r = fread (buf, 1, len - 1, file);
	fclose (file);
	buf [r] = 0;
	p = strrchr (buf, '/');
	if (p)
		return p + 1;
	if (r == 0) {
		return get_pid_status_item_buf (GPOINTER_TO_INT (pid), "Name", buf, len, NULL);
	}
	return buf;
#endif
}

void
mono_process_get_times (gpointer pid, gint64 *start_time, gint64 *user_time, gint64 *kernel_time)
{
	if (user_time)
		*user_time = mono_process_get_data (pid, MONO_PROCESS_USER_TIME);

	if (kernel_time)
		*kernel_time = mono_process_get_data (pid, MONO_PROCESS_SYSTEM_TIME);

	if (start_time) {
		*start_time = 0;

#if USE_SYSCTL && defined(kinfo_starttime_member)
		{
			KINFO_PROC processi;

			if (sysctl_kinfo_proc (pid, &processi)) {
#if defined(__NetBSD__)
				struct timeval tv;
				tv.tv_sec = processi.kinfo_starttime_member;
				tv.tv_usec = processi.p_ustart_usec;
				*start_time = mono_100ns_datetime_from_timeval(tv);
#else
				*start_time = mono_100ns_datetime_from_timeval (processi.kinfo_starttime_member);
#endif
			}
		}
#endif

		if (*start_time == 0) {
			static guint64 boot_time = 0;
			if (!boot_time)
				boot_time = mono_100ns_datetime () - mono_msec_boottime () * 10000;

			*start_time = boot_time + mono_process_get_data (pid, MONO_PROCESS_ELAPSED);
		}
	}
}

/*
 * /proc/pid/stat format:
 * pid (cmdname) S 
 * 	[0] ppid pgid sid tty_nr tty_pgrp flags min_flt cmin_flt maj_flt cmaj_flt
 * 	[10] utime stime cutime cstime prio nice threads 0 start_time vsize
 * 	[20] rss rsslim start_code end_code start_stack esp eip pending blocked sigign
 * 	[30] sigcatch wchan 0 0 exit_signal cpu rt_prio policy
 */

#define RET_ERROR(err) do {	\
		if (error) *error = (err);	\
		return 0;			\
	} while (0)

static gint64
get_process_stat_item (int pid, int pos, int sum, MonoProcessError *error)
{
#if defined(__APPLE__) 
	double process_user_time = 0, process_system_time = 0;//, process_percent = 0;
	task_t task;
	struct task_basic_info t_info;
	mach_msg_type_number_t t_info_count = TASK_BASIC_INFO_COUNT, th_count;
	thread_array_t th_array;
	size_t i;
	kern_return_t ret;

	if (pid == getpid ()) {
		/* task_for_pid () doesn't work on ios, even for the current process */
		task = mach_task_self ();
	} else {
		do {
			ret = task_for_pid (mach_task_self (), pid, &task);
		} while (ret == KERN_ABORTED);

		if (ret != KERN_SUCCESS)
			RET_ERROR (MONO_PROCESS_ERROR_NOT_FOUND);
	}

	do {
		ret = task_info (task, TASK_BASIC_INFO, (task_info_t)&t_info, &t_info_count);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS) {
		if (pid != getpid ())
			mach_port_deallocate (mach_task_self (), task);
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	}

	do {
		ret = task_threads (task, &th_array, &th_count);
	} while (ret == KERN_ABORTED);
	
	if (ret  != KERN_SUCCESS) {
		if (pid != getpid ())
			mach_port_deallocate (mach_task_self (), task);
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	}
		
	for (i = 0; i < th_count; i++) {
		double thread_user_time, thread_system_time;//, thread_percent;
		
		struct thread_basic_info th_info;
		mach_msg_type_number_t th_info_count = THREAD_BASIC_INFO_COUNT;
		do {
			ret = thread_info(th_array[i], THREAD_BASIC_INFO, (thread_info_t)&th_info, &th_info_count);
		} while (ret == KERN_ABORTED);

		if (ret == KERN_SUCCESS) {
			thread_user_time = th_info.user_time.seconds + th_info.user_time.microseconds / 1e6;
			thread_system_time = th_info.system_time.seconds + th_info.system_time.microseconds / 1e6;
			//thread_percent = (double)th_info.cpu_usage / TH_USAGE_SCALE;
			
			process_user_time += thread_user_time;
			process_system_time += thread_system_time;
			//process_percent += th_percent;
		}
	}
	
	for (i = 0; i < th_count; i++)
		mach_port_deallocate(task, th_array[i]);

	if (pid != getpid ())
		mach_port_deallocate (mach_task_self (), task);

	process_user_time += t_info.user_time.seconds + t_info.user_time.microseconds / 1e6;
	process_system_time += t_info.system_time.seconds + t_info.system_time.microseconds / 1e6;
    
	if (pos == 10 && sum == TRUE)
		return (gint64)((process_user_time + process_system_time) * 10000000);
	else if (pos == 10)
		return (gint64)(process_user_time * 10000000);
	else if (pos == 11)
		return (gint64)(process_system_time * 10000000);
		
	return 0;
#else
	char buf [512];
	char *s, *end;
	FILE *f;
	size_t len;
	int i;
	gint64 value;

	g_snprintf (buf, sizeof (buf), "/proc/%d/stat", pid);
	f = fopen (buf, "r");
	if (!f)
		RET_ERROR (MONO_PROCESS_ERROR_NOT_FOUND);
	len = fread (buf, 1, sizeof (buf), f);
	fclose (f);
	if (len <= 0)
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	s = strchr (buf, ')');
	if (!s)
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	s++;
	while (g_ascii_isspace (*s)) s++;
	if (!*s)
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	/* skip the status char */
	while (*s && !g_ascii_isspace (*s)) s++;
	if (!*s)
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	for (i = 0; i < pos; ++i) {
		while (g_ascii_isspace (*s)) s++;
		if (!*s)
			RET_ERROR (MONO_PROCESS_ERROR_OTHER);
		while (*s && !g_ascii_isspace (*s)) s++;
		if (!*s)
			RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	}
	/* we are finally at the needed item */
	value = strtoul (s, &end, 0);
	/* add also the following value */
	if (sum) {
		while (g_ascii_isspace (*s)) s++;
		if (!*s)
			RET_ERROR (MONO_PROCESS_ERROR_OTHER);
		value += strtoul (s, &end, 0);
	}
	if (error)
		*error = MONO_PROCESS_ERROR_NONE;
	return value;
#endif
}

static int
get_user_hz (void)
{
	static int user_hz = 0;
	if (user_hz == 0) {
#ifdef _SC_CLK_TCK
		user_hz = sysconf (_SC_CLK_TCK);
#endif
		if (user_hz == 0)
			user_hz = 100;
	}
	return user_hz;
}

static gint64
get_process_stat_time (int pid, int pos, int sum, MonoProcessError *error)
{
	gint64 val = get_process_stat_item (pid, pos, sum, error);
#if defined(__APPLE__)
	return val;
#else
	/* return 100ns ticks */
	return (val * 10000000) / get_user_hz ();
#endif
}

static gint64
get_pid_status_item (int pid, const char *item, MonoProcessError *error, int multiplier)
{
#if defined(__APPLE__)
	// ignore the multiplier
	
	gint64 ret;
	task_t task;
	struct task_basic_info t_info;
	mach_msg_type_number_t th_count = TASK_BASIC_INFO_COUNT;
	kern_return_t mach_ret;

	if (pid == getpid ()) {
		/* task_for_pid () doesn't work on ios, even for the current process */
		task = mach_task_self ();
	} else {
		do {
			mach_ret = task_for_pid (mach_task_self (), pid, &task);
		} while (mach_ret == KERN_ABORTED);

		if (mach_ret != KERN_SUCCESS)
			RET_ERROR (MONO_PROCESS_ERROR_NOT_FOUND);
	}

	do {
		mach_ret = task_info (task, TASK_BASIC_INFO, (task_info_t)&t_info, &th_count);
	} while (mach_ret == KERN_ABORTED);

	if (mach_ret != KERN_SUCCESS) {
		if (pid != getpid ())
			mach_port_deallocate (mach_task_self (), task);
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	}

	if (strcmp (item, "VmRSS") == 0 || strcmp (item, "VmHWM") == 0 || strcmp (item, "VmData") == 0)
		ret = t_info.resident_size;
	else if (strcmp (item, "VmSize") == 0 || strcmp (item, "VmPeak") == 0)
		ret = t_info.virtual_size;
	else if (strcmp (item, "Threads") == 0)
		ret = th_count;
	else
		ret = 0;

	if (pid != getpid ())
		mach_port_deallocate (mach_task_self (), task);
	
	return ret;
#else
	char buf [64];
	char *s;

	s = get_pid_status_item_buf (pid, item, buf, sizeof (buf), error);
	if (s)
		return ((gint64) atol (s)) * multiplier;
	return 0;
#endif
}

/**
 * mono_process_get_data:
 * \param pid pid of the process
 * \param data description of data to return
 * \returns a data item of a process like user time, memory use etc,
 * according to the \p data argumet.
 */
gint64
mono_process_get_data_with_error (gpointer pid, MonoProcessData data, MonoProcessError *error)
{
	gint64 val;
	int rpid = GPOINTER_TO_INT (pid);

	if (error)
		*error = MONO_PROCESS_ERROR_OTHER;

	switch (data) {
	case MONO_PROCESS_NUM_THREADS:
		return get_pid_status_item (rpid, "Threads", error, 1);
	case MONO_PROCESS_USER_TIME:
		return get_process_stat_time (rpid, 10, FALSE, error);
	case MONO_PROCESS_SYSTEM_TIME:
		return get_process_stat_time (rpid, 11, FALSE, error);
	case MONO_PROCESS_TOTAL_TIME:
		return get_process_stat_time (rpid, 10, TRUE, error);
	case MONO_PROCESS_WORKING_SET:
		return get_pid_status_item (rpid, "VmRSS", error, 1024);
	case MONO_PROCESS_WORKING_SET_PEAK:
		val = get_pid_status_item (rpid, "VmHWM", error, 1024);
		if (val == 0)
			val = get_pid_status_item (rpid, "VmRSS", error, 1024);
		return val;
	case MONO_PROCESS_PRIVATE_BYTES:
		return get_pid_status_item (rpid, "VmData", error, 1024);
	case MONO_PROCESS_VIRTUAL_BYTES:
		return get_pid_status_item (rpid, "VmSize", error, 1024);
	case MONO_PROCESS_VIRTUAL_BYTES_PEAK:
		val = get_pid_status_item (rpid, "VmPeak", error, 1024);
		if (val == 0)
			val = get_pid_status_item (rpid, "VmSize", error, 1024);
		return val;
	case MONO_PROCESS_FAULTS:
		return get_process_stat_item (rpid, 6, TRUE, error);
	case MONO_PROCESS_ELAPSED:
		return get_process_stat_time (rpid, 18, FALSE, error);
	case MONO_PROCESS_PPID:
		return get_process_stat_time (rpid, 0, FALSE, error);
	case MONO_PROCESS_PAGED_BYTES:
		return get_pid_status_item (rpid, "VmSwap", error, 1024);

		/* Nothing yet */
	case MONO_PROCESS_END:
		return 0;
	}
	return 0;
}

gint64
mono_process_get_data (gpointer pid, MonoProcessData data)
{
	MonoProcessError error;
	return mono_process_get_data_with_error (pid, data, &error);
}

#ifndef HOST_WIN32
int
mono_process_current_pid ()
{
#if defined(HAVE_UNISTD_H)
	return (int) getpid ();
#else
#error getpid
#endif
}
#endif /* !HOST_WIN32 */

/**
 * mono_cpu_count:
 * \returns the number of processors on the system.
 */
#ifndef HOST_WIN32
int
mono_cpu_count (void)
{
#ifdef HOST_ANDROID
	/* Android tries really hard to save power by powering off CPUs on SMP phones which
	 * means the normal way to query cpu count returns a wrong value with userspace API.
	 * Instead we use /sys entries to query the actual hardware CPU count.
	 */
	int count = 0;
	char buffer[8] = {'\0'};
	int present = open ("/sys/devices/system/cpu/present", O_RDONLY);
	/* Format of the /sys entry is a cpulist of indexes which in the case
	 * of present is always of the form "0-(n-1)" when there is more than
	 * 1 core, n being the number of CPU cores in the system. Otherwise
	 * the value is simply 0
	 */
	if (present != -1 && read (present, (char*)buffer, sizeof (buffer)) > 3)
		count = strtol (((char*)buffer) + 2, NULL, 10);
	if (present != -1)
		close (present);
	if (count > 0)
		return count + 1;
#endif

#if defined(HOST_ARM) || defined (HOST_ARM64)

/*
 * Recap from Alexander Köplinger <alex.koeplinger@outlook.com>:
 *
 * When we merged the change from PR #2722, we started seeing random failures on ARM in
 * the MonoTests.System.Threading.ThreadPoolTests.SetAndGetMaxThreads and
 * MonoTests.System.Threading.ManualResetEventSlimTests.Constructor_Defaults tests. Both
 * of those tests are dealing with Environment.ProcessorCount to verify some implementation
 * details.
 *
 * It turns out that on the Jetson TK1 board we use on public Jenkins and on ARM kernels
 * in general, the value returned by sched_getaffinity (or _SC_NPROCESSORS_ONLN) doesn't
 * contain CPUs/cores that are powered off for power saving reasons. This is contrary to
 * what happens on x86, where even cores in deep-sleep state are returned [1], [2]. This
 * means that we would get a processor count of 1 at one point in time and a higher value
 * when load increases later on as the system wakes CPUs.
 *
 * Various runtime pieces like the threadpool and also user code however relies on the
 * value returned by Environment.ProcessorCount e.g. for deciding how many parallel tasks
 * to start, thereby limiting the performance when that code thinks we only have one CPU.
 *
 * Talking to a few people, this was the reason why we changed to _SC_NPROCESSORS_CONF in
 * mono#1688 and why we added a special case for Android in mono@de3addc to get the "real"
 * number of processors in the system.
 *
 * Because of those issues Android/Dalvik also switched from _ONLN to _SC_NPROCESSORS_CONF
 * for the Java API Runtime.availableProcessors() too [3], citing:
 * > Traditionally this returned the number currently online, but many mobile devices are
 * able to take unused cores offline to save power, so releases newer than Android 4.2 (Jelly
 * Bean) return the maximum number of cores that could be made available if there were no
 * power or heat constraints.
 *
 * The problem with sticking to _SC_NPROCESSORS_CONF however is that it breaks down in
 * constrained environments like Docker or with an explicit CPU affinity set by the Linux
 * `taskset` command, They'd get a higher CPU count than can be used, start more threads etc.
 * which results in unnecessary context switches and overloaded systems. That's why we need
 * to respect sched_getaffinity.
 *
 * So while in an ideal world we would be able to rely on sched_getaffinity/_SC_NPROCESSORS_ONLN
 * to return the number of theoretically available CPUs regardless of power saving measures
 * everywhere, we can't do this on ARM.
 *
 * I think the pragmatic solution is the following:
 * * use sched_getaffinity (+ fallback to _SC_NPROCESSORS_ONLN in case of error) on x86. This
 * ensures we're inline with what OpenJDK [4] and CoreCLR [5] do
 * * use _SC_NPROCESSORS_CONF exclusively on ARM (I think we could eventually even get rid of
 * the HOST_ANDROID special case)
 *
 * Helpful links:
 *
 * [1] https://sourceware.org/ml/libc-alpha/2013-07/msg00383.html
 * [2] https://lists.01.org/pipermail/powertop/2012-September/000433.html
 * [3] https://android.googlesource.com/platform/libcore/+/750dc634e56c58d1d04f6a138734ac2b772900b5%5E1..750dc634e56c58d1d04f6a138734ac2b772900b5/
 * [4] https://bugs.openjdk.java.net/browse/JDK-6515172
 * [5] https://github.com/dotnet/coreclr/blob/7058273693db2555f127ce16e6b0c5b40fb04867/src/pal/src/misc/sysinfo.cpp#L148
 */

#ifdef _SC_NPROCESSORS_CONF
	{
		int count = sysconf (_SC_NPROCESSORS_CONF);
		if (count > 0)
			return count;
	}
#endif

#else

#ifdef HAVE_SCHED_GETAFFINITY
	{
		cpu_set_t set;
		if (sched_getaffinity (mono_process_current_pid (), sizeof (set), &set) == 0)
			return CPU_COUNT (&set);
	}
#endif
#ifdef _SC_NPROCESSORS_ONLN
	{
		int count = sysconf (_SC_NPROCESSORS_ONLN);
		if (count > 0)
			return count;
	}
#endif

#endif /* defined(HOST_ARM) || defined (HOST_ARM64) */

#ifdef USE_SYSCTL
	{
		int count;
		int mib [2];
		size_t len = sizeof (int);
		mib [0] = CTL_HW;
		mib [1] = HW_NCPU;
		if (sysctl (mib, 2, &count, &len, NULL, 0) == 0)
			return count;
	}
#endif
	/* FIXME: warn */
	return 1;
}
#endif /* !HOST_WIN32 */

static void
get_cpu_times (int cpu_id, gint64 *user, gint64 *systemt, gint64 *irq, gint64 *sirq, gint64 *idle)
{
	char buf [256];
	char *s;
	int hz = get_user_hz ();
	guint64	user_ticks = 0, nice_ticks = 0, system_ticks = 0, idle_ticks = 0, irq_ticks = 0, sirq_ticks = 0;
	FILE *f = fopen ("/proc/stat", "r");
	if (!f)
		return;
	if (cpu_id < 0)
		hz *= mono_cpu_count ();
	while ((s = fgets (buf, sizeof (buf), f))) {
		char *data = NULL;
		if (cpu_id < 0 && strncmp (s, "cpu", 3) == 0 && g_ascii_isspace (s [3])) {
			data = s + 4;
		} else if (cpu_id >= 0 && strncmp (s, "cpu", 3) == 0 && strtol (s + 3, &data, 10) == cpu_id) {
			if (data == s + 3)
				continue;
			data++;
		} else {
			continue;
		}
		
		user_ticks = strtoull (data, &data, 10);
		nice_ticks = strtoull (data, &data, 10);
		system_ticks = strtoull (data, &data, 10);
		idle_ticks = strtoull (data, &data, 10);
		/* iowait_ticks = strtoull (data, &data, 10); */
		irq_ticks = strtoull (data, &data, 10);
		sirq_ticks = strtoull (data, &data, 10);
		break;
	}
	fclose (f);

	if (user)
		*user = (user_ticks + nice_ticks) * 10000000 / hz;
	if (systemt)
		*systemt = (system_ticks) * 10000000 / hz;
	if (irq)
		*irq = (irq_ticks) * 10000000 / hz;
	if (sirq)
		*sirq = (sirq_ticks) * 10000000 / hz;
	if (idle)
		*idle = (idle_ticks) * 10000000 / hz;
}

/**
 * mono_cpu_get_data:
 * \param cpu_id processor number or -1 to get a summary of all the processors
 * \param data type of data to retrieve
 * Get data about a processor on the system, like time spent in user space or idle time.
 */
gint64
mono_cpu_get_data (int cpu_id, MonoCpuData data, MonoProcessError *error)
{
	gint64 value = 0;

	if (error)
		*error = MONO_PROCESS_ERROR_NONE;
	switch (data) {
	case MONO_CPU_USER_TIME:
		get_cpu_times (cpu_id, &value, NULL, NULL, NULL, NULL);
		break;
	case MONO_CPU_PRIV_TIME:
		get_cpu_times (cpu_id, NULL, &value, NULL, NULL, NULL);
		break;
	case MONO_CPU_INTR_TIME:
		get_cpu_times (cpu_id, NULL, NULL, &value, NULL, NULL);
		break;
	case MONO_CPU_DCP_TIME:
		get_cpu_times (cpu_id, NULL, NULL, NULL, &value, NULL);
		break;
	case MONO_CPU_IDLE_TIME:
		get_cpu_times (cpu_id, NULL, NULL, NULL, NULL, &value);
		break;

	case MONO_CPU_END:
		/* Nothing yet */
		return 0;
	}
	return value;
}

int
mono_atexit (void (*func)(void))
{
#ifdef HOST_ANDROID
	/* Some versions of android libc doesn't define atexit () */
	return 0;
#else
	return atexit (func);
#endif
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
#ifndef HOST_WIN32
gint32
mono_cpu_usage (MonoCpuUsageState *prev)
{
	gint32 cpu_usage = 0;
	gint64 cpu_total_time;
	gint64 cpu_busy_time;
	struct rusage resource_usage;
	gint64 current_time;
	gint64 kernel_time;
	gint64 user_time;

	if (getrusage (RUSAGE_SELF, &resource_usage) == -1) {
		g_error ("getrusage() failed, errno is %d (%s)\n", errno, strerror (errno));
		return -1;
	}

	current_time = mono_100ns_ticks ();
	kernel_time = resource_usage.ru_stime.tv_sec * 1000 * 1000 * 10 + resource_usage.ru_stime.tv_usec * 10;
	user_time = resource_usage.ru_utime.tv_sec * 1000 * 1000 * 10 + resource_usage.ru_utime.tv_usec * 10;

	cpu_busy_time = (user_time - (prev ? prev->user_time : 0)) + (kernel_time - (prev ? prev->kernel_time : 0));
	cpu_total_time = (current_time - (prev ? prev->current_time : 0)) * mono_cpu_count ();

	if (prev) {
		prev->kernel_time = kernel_time;
		prev->user_time = user_time;
		prev->current_time = current_time;
	}

	if (cpu_total_time > 0 && cpu_busy_time > 0)
		cpu_usage = (gint32)(cpu_busy_time * 100 / cpu_total_time);

	return cpu_usage;
}
#endif /* !HOST_WIN32 */
