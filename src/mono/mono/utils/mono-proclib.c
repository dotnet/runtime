/*
 * Copyright 2008-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 */

#include "config.h"
#include "utils/mono-proclib.h"

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef HOST_WIN32
#include <windows.h>
#include <process.h>
#endif

#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__OpenBSD__) || defined(__NetBSD__)
#include <sys/param.h>
#include <sys/types.h>
#include <sys/sysctl.h>
#include <sys/proc.h>
#if defined(__APPLE__)
#include <mach/mach.h>
#endif
#ifdef HAVE_SYS_USER_H
#include <sys/user.h>
#endif
#ifdef HAVE_STRUCT_KINFO_PROC_KP_PROC
#    define kinfo_pid_member kp_proc.p_pid
#    define kinfo_name_member kp_proc.p_comm
#elif defined(__OpenBSD__)
#    define kinfo_pid_member p_pid
#    define kinfo_name_member p_comm
#else
#define kinfo_pid_member ki_pid
#define kinfo_name_member ki_comm
#endif
#define USE_SYSCTL 1
#endif

/**
 * mono_process_list:
 * @size: a pointer to a location where the size of the returned array is stored
 *
 * Return an array of pid values for the processes currently running on the system.
 * The size of the array is stored in @size.
 */
gpointer*
mono_process_list (int *size)
{
#if USE_SYSCTL
	int res, i;
#ifdef KERN_PROC2
	int mib [6];
	size_t data_len = sizeof (struct kinfo_proc2) * 400;
	struct kinfo_proc2 *processes = malloc (data_len);
#else
	int mib [4];
	size_t data_len = sizeof (struct kinfo_proc) * 400;
	struct kinfo_proc *processes = malloc (data_len);
#endif /* KERN_PROC2 */
	void **buf = NULL;

	if (size)
		*size = 0;
	if (!processes)
		return NULL;

#ifdef KERN_PROC2
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC2;
	mib [2] = KERN_PROC_ALL;
	mib [3] = 0;
	mib [4] = sizeof(struct kinfo_proc2);
	mib [5] = 400; /* XXX */

	res = sysctl (mib, 6, processes, &data_len, NULL, 0);
#else
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_ALL;
	mib [3] = 0;
	
	res = sysctl (mib, 4, processes, &data_len, NULL, 0);
#endif /* KERN_PROC2 */

	if (res < 0) {
		free (processes);
		return NULL;
	}
#ifdef KERN_PROC2
	res = data_len/sizeof (struct kinfo_proc2);
#else
	res = data_len/sizeof (struct kinfo_proc);
#endif /* KERN_PROC2 */
	buf = g_realloc (buf, res * sizeof (void*));
	for (i = 0; i < res; ++i)
		buf [i] = GINT_TO_POINTER (processes [i].kinfo_pid_member);
	free (processes);
	if (size)
		*size = res;
	return buf;
#elif defined(__HAIKU__)
	/* FIXME: Add back the code from 9185fcc305e43428d0f40f3ee37c8a405d41c9ae */
	g_assert_not_reached ();
	return NULL;
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
			buf = g_realloc (buf, count * sizeof (void*));
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
	int len = strlen (item);

	g_snprintf (buf, sizeof (buf), "/proc/%d/status", pid);
	f = fopen (buf, "r");
	if (!f) {
		if (error)
			*error = MONO_PROCESS_ERROR_NOT_FOUND;
		return NULL;
	}
	while ((s = fgets (buf, blen, f))) {
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
		strncpy (rbuf, s, MIN (len, blen));
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

/**
 * mono_process_get_name:
 * @pid: pid of the process
 * @buf: byte buffer where to store the name of the prcoess
 * @len: size of the buffer @buf
 *
 * Return the name of the process identified by @pid, storing it
 * inside @buf for a maximum of len bytes (including the terminating 0).
 */
char*
mono_process_get_name (gpointer pid, char *buf, int len)
{
#if USE_SYSCTL
	int res;
#ifdef KERN_PROC2
	int mib [6];
	size_t data_len = sizeof (struct kinfo_proc2);
	struct kinfo_proc2 processi;
#else
	int mib [4];
	size_t data_len = sizeof (struct kinfo_proc);
	struct kinfo_proc processi;
#endif /* KERN_PROC2 */

	memset (buf, 0, len);

#ifdef KERN_PROC2
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC2;
	mib [2] = KERN_PROC_PID;
	mib [3] = GPOINTER_TO_UINT (pid);
	mib [4] = sizeof(struct kinfo_proc2);
	mib [5] = 400; /* XXX */

	res = sysctl (mib, 6, &processi, &data_len, NULL, 0);

	if (res < 0 || data_len != sizeof (struct kinfo_proc2)) {
		return buf;
	}
#else
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_PID;
	mib [3] = GPOINTER_TO_UINT (pid);
	
	res = sysctl (mib, 4, &processi, &data_len, NULL, 0);
	if (res < 0 || data_len != sizeof (struct kinfo_proc)) {
		return buf;
	}
#endif /* KERN_PROC2 */
	strncpy (buf, processi.kinfo_name_member, len - 1);
	return buf;
#else
	char fname [128];
	FILE *file;
	char *p;
	int r;
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

/*
 * /proc/pid/stat format:
 * pid (cmdname) S 
 * 	[0] ppid pgid sid tty_nr tty_pgrp flags min_flt cmin_flt maj_flt cmaj_flt
 * 	[10] utime stime cutime cstime prio nice threads 0 start_time vsize rss
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

	if (task_for_pid(mach_task_self(), pid, &task) != KERN_SUCCESS)
		RET_ERROR (MONO_PROCESS_ERROR_NOT_FOUND);

	if (task_info(task, TASK_BASIC_INFO, (task_info_t)&t_info, &t_info_count) != KERN_SUCCESS) {
		mach_port_deallocate (mach_task_self (), task);
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	}
	
	if (task_threads(task, &th_array, &th_count) != KERN_SUCCESS) {
		mach_port_deallocate (mach_task_self (), task);
		RET_ERROR (MONO_PROCESS_ERROR_OTHER);
	}
		
	for (i = 0; i < th_count; i++) {
		double thread_user_time, thread_system_time;//, thread_percent;
		
		struct thread_basic_info th_info;
		mach_msg_type_number_t th_info_count = THREAD_BASIC_INFO_COUNT;
		if (thread_info(th_array[i], THREAD_BASIC_INFO, (thread_info_t)&th_info, &th_info_count) == KERN_SUCCESS) {
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
	int len, i;
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

	if (pid == getpid ()) {
		/* task_for_pid () doesn't work on ios, even for the current process */
		task = mach_task_self ();
	} else {
		if (task_for_pid (mach_task_self (), pid, &task) != KERN_SUCCESS)
			RET_ERROR (MONO_PROCESS_ERROR_NOT_FOUND);
	}
	
	if (task_info (task, TASK_BASIC_INFO, (task_info_t)&t_info, &th_count) != KERN_SUCCESS) {
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
 * @pid: pid of the process
 * @data: description of data to return
 *
 * Return a data item of a process like user time, memory use etc,
 * according to the @data argumet.
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
		return get_process_stat_item (rpid, 18, FALSE, error) / get_user_hz ();
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

int
mono_process_current_pid ()
{
#if defined(HAVE_UNISTD_H)
	return (int) getpid ();
#elif defined(HOST_WIN32)
	return (int) GetCurrentProcessId ();
#else
#error getpid
#endif
}

/**
 * mono_cpu_count:
 *
 * Return the number of processors on the system.
 */
int
mono_cpu_count (void)
{
	int count = 0;
#ifdef PLATFORM_ANDROID
	/* Android tries really hard to save power by powering off CPUs on SMP phones which
	 * means the normal way to query cpu count returns a wrong value with userspace API.
	 * Instead we use /sys entries to query the actual hardware CPU count.
	 */
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
#ifdef _SC_NPROCESSORS_ONLN
	count = sysconf (_SC_NPROCESSORS_ONLN);
	if (count > 0)
		return count;
#endif
#ifdef USE_SYSCTL
	{
		int mib [2];
		size_t len = sizeof (int);
		mib [0] = CTL_HW;
		mib [1] = HW_NCPU;
		if (sysctl (mib, 2, &count, &len, NULL, 0) == 0)
			return count;
	}
#endif
#ifdef HOST_WIN32
	{
		SYSTEM_INFO info;
		GetSystemInfo (&info);
		return info.dwNumberOfProcessors;
	}
#endif
	/* FIXME: warn */
	return 1;
}

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
 * @cpu_id: processor number or -1 to get a summary of all the processors
 * @data: type of data to retrieve
 *
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
#ifdef PLATFORM_ANDROID
	/* Some versions of android libc doesn't define atexit () */
	return 0;
#else
	return atexit (func);
#endif
}
