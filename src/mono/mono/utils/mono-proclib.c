#include "config.h"
#include "utils/mono-proclib.h"

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>

/* FIXME: implement for non-linux */

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
	return buf;
}

/*
 * /proc/pid/stat format:
 * pid (cmdname) S 
 * 	[0] ppid pgid sid tty_nr tty_pgrp flags min_flt cmin_flt maj_flt cmaj_flt
 * 	[10] utime stime cutime cstime prio nice threads start_time vsize rss
 * 	[20] rsslim start_code end_code start_stack esp eip pending blocked sigign sigcatch
 * 	[30] wchan 0 0 exit_signal cpu rt_prio policy
 */

static gint64
get_process_stat_item (int pid, int pos, int sum)
{
	char buf [512];
	char *s, *end;
	FILE *f;
	int len, i;
	gint64 value;

	g_snprintf (buf, sizeof (buf), "/proc/%d/stat", pid);
	f = fopen (buf, "r");
	if (!f)
		return 0;
	len = fread (buf, 1, sizeof (buf), f);
	fclose (f);
	if (len <= 0)
		return 0;
	s = strchr (buf, ')');
	if (!s)
		return 0;
	s++;
	while (g_ascii_isspace (*s)) s++;
	if (!*s)
		return 0;
	/* skip the status char */
	while (*s && !g_ascii_isspace (*s)) s++;
	if (!*s)
		return 0;
	for (i = 0; i < pos; ++i) {
		while (g_ascii_isspace (*s)) s++;
		if (!*s)
			return 0;
		while (*s && !g_ascii_isspace (*s)) s++;
		if (!*s)
			return 0;
	}
	/* we are finally at the needed item */
	value = strtoul (s, &end, 0);
	/* add also the following value */
	if (sum) {
		while (g_ascii_isspace (*s)) s++;
		if (!*s)
			return 0;
		value += strtoul (s, &end, 0);
	}
	return value;
}

static gint64
get_process_stat_time (int pid, int pos, int sum)
{
	static int user_hz = 0;
	gint64 val = get_process_stat_item (pid, pos, sum);
	if (user_hz == 0) {
#ifdef _SC_CLK_TCK
		user_hz = sysconf (_SC_CLK_TCK);
#endif
		if (user_hz == 0)
			user_hz = 100;
	}
	/* return milliseconds */
	return (val * 1000) / user_hz;
}

static gint64
get_pid_status_item (int pid, const char *item)
{
	char buf [256];
	char *s;
	FILE *f;
	int len = strlen (item);

	g_snprintf (buf, sizeof (buf), "/proc/%d/status", pid);
	f = fopen (buf, "r");
	if (!f)
		return 0;
	while ((s = fgets (buf, sizeof (buf), f))) {
		if (*item != *buf)
			continue;
		if (strncmp (buf, item, len))
			continue;
		if (buf [len] != ':')
			continue;
		fclose (f);
		return atoi (buf + len + 1);
	}
	fclose (f);
	return 0;
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
mono_process_get_data (gpointer pid, MonoProcessData data)
{
	gint64 val;
	int rpid = GPOINTER_TO_INT (pid);
	switch (data) {
	case MONO_PROCESS_NUM_THREADS:
		return get_pid_status_item (rpid, "Threads");
	case MONO_PROCESS_USER_TIME:
		return get_process_stat_time (rpid, 12, FALSE);
	case MONO_PROCESS_SYSTEM_TIME:
		return get_process_stat_time (rpid, 13, FALSE);
	case MONO_PROCESS_TOTAL_TIME:
		return get_process_stat_time (rpid, 12, TRUE);
	case MONO_PROCESS_WORKING_SET:
		return get_pid_status_item (rpid, "VmRSS") * 1024;
	case MONO_PROCESS_WORKING_SET_PEAK:
		val = get_pid_status_item (rpid, "VmHWM") * 1024;
		if (val == 0)
			val = get_pid_status_item (rpid, "VmRSS") * 1024;
		return val;
	case MONO_PROCESS_PRIVATE_BYTES:
		return get_pid_status_item (rpid, "VmData") * 1024;
	case MONO_PROCESS_VIRTUAL_BYTES:
		return get_pid_status_item (rpid, "VmSize") * 1024;
	case MONO_PROCESS_VIRTUAL_BYTES_PEAK:
		val = get_pid_status_item (rpid, "VmPeak") * 1024;
		if (val == 0)
			val = get_pid_status_item (rpid, "VmSize") * 1024;
		return val;
	case MONO_PROCESS_FAULTS:
		return get_process_stat_item (rpid, 6, TRUE);
	}
	return 0;
}

