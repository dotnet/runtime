/*
 * Time utility functions.
 * Author: Paolo Molaro (<lupus@ximian.com>)
 * Copyright (C) 2008 Novell, Inc.
 */

#include <utils/mono-time.h>
#include <stdlib.h>
#include <stdio.h>

#define MTICKS_PER_SEC 10000000

#ifdef HOST_WIN32
#include <windows.h>

guint32
mono_msec_ticks (void)
{
	/* GetTickCount () is reportedly monotonic */
	return GetTickCount ();
}

/* Returns the number of 100ns ticks from unspecified time: this should be monotonic */
gint64
mono_100ns_ticks (void)
{
	static LARGE_INTEGER freq;
	static UINT64 start_time;
	UINT64 cur_time;
	LARGE_INTEGER value;

	if (!freq.QuadPart) {
		if (!QueryPerformanceFrequency (&freq))
			return mono_100ns_datetime ();
		QueryPerformanceCounter (&value);
		start_time = value.QuadPart;
	}
	QueryPerformanceCounter (&value);
	cur_time = value.QuadPart;
	/* we use unsigned numbers and return the difference to avoid overflows */
	return (cur_time - start_time) * (double)MTICKS_PER_SEC / freq.QuadPart;
}

/*
 * Magic number to convert FILETIME base Jan 1, 1601 to DateTime - base Jan, 1, 0001
 */
#define FILETIME_ADJUST ((guint64)504911232000000000LL)

/* Returns the number of 100ns ticks since 1/1/1, UTC timezone */
gint64
mono_100ns_datetime (void)
{
	ULARGE_INTEGER ft;

	if (sizeof(ft) != sizeof(FILETIME))
		g_assert_not_reached ();

	GetSystemTimeAsFileTime ((FILETIME*) &ft);
	return FILETIME_ADJUST + ft.QuadPart;
}

#else

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__OpenBSD__) || defined(__NetBSD__)
#include <sys/param.h>
#include <sys/sysctl.h>
#endif

#if defined(PLATFORM_MACOSX)
#include <mach/mach.h>
#include <mach/mach_time.h>
#endif

#include <time.h>

static gint64
get_boot_time (void)
{
#if defined(PLATFORM_MACOSX) || defined(__FreeBSD__) || defined(__OpenBSD__) || defined(__NetBSD__)
	int mib [2];
	size_t size;
	time_t now;
	struct timeval boottime;

	(void)time(&now);

	mib [0] = CTL_KERN;
	mib [1] = KERN_BOOTTIME;

	size = sizeof(boottime);

	if (sysctl(mib, 2, &boottime, &size, NULL, 0) != -1)
		return (gint64)((now - boottime.tv_sec) * MTICKS_PER_SEC);
#else
	FILE *uptime = fopen ("/proc/uptime", "r");
	if (uptime) {
		double upt;
		if (fscanf (uptime, "%lf", &upt) == 1) {
			gint64 now = mono_100ns_ticks ();
			fclose (uptime);
			return now - (gint64)(upt * MTICKS_PER_SEC);
		}
		fclose (uptime);
	}
#endif
	/* a made up uptime of 300 seconds */
	return (gint64)300 * MTICKS_PER_SEC;
}

/* Returns the number of milliseconds from boot time: this should be monotonic */
guint32
mono_msec_ticks (void)
{
	static gint64 boot_time = 0;
	gint64 now;
	if (!boot_time)
		boot_time = get_boot_time ();
	now = mono_100ns_ticks ();
	/*printf ("now: %llu (boot: %llu) ticks: %llu\n", (gint64)now, (gint64)boot_time, (gint64)(now - boot_time));*/
	return (now - boot_time)/10000;
}

/* Returns the number of 100ns ticks from unspecified time: this should be monotonic */
gint64
mono_100ns_ticks (void)
{
	struct timeval tv;
#ifdef CLOCK_MONOTONIC
	struct timespec tspec;
	static struct timespec tspec_freq = {0};
	static int can_use_clock = 0;
	if (!tspec_freq.tv_nsec) {
		can_use_clock = clock_getres (CLOCK_MONOTONIC, &tspec_freq) == 0;
		/*printf ("resolution: %lu.%lu\n", tspec_freq.tv_sec, tspec_freq.tv_nsec);*/
	}
	if (can_use_clock) {
		if (clock_gettime (CLOCK_MONOTONIC, &tspec) == 0) {
			/*printf ("time: %lu.%lu\n", tspec.tv_sec, tspec.tv_nsec); */
			return ((gint64)tspec.tv_sec * MTICKS_PER_SEC + tspec.tv_nsec / 100);
		}
	}
	
#elif defined(PLATFORM_MACOSX)
	/* http://developer.apple.com/library/mac/#qa/qa1398/_index.html */
	static mach_timebase_info_data_t timebase;
	guint64 now = mach_absolute_time ();
	if (timebase.denom == 0) {
		mach_timebase_info (&timebase);
		timebase.denom *= 100; /* we return 100ns ticks */
	}
	return now * timebase.numer / timebase.denom;
#endif
	if (gettimeofday (&tv, NULL) == 0)
		return ((gint64)tv.tv_sec * 1000000 + tv.tv_usec) * 10;
	return 0;
}

/*
 * Magic number to convert a time which is relative to
 * Jan 1, 1970 into a value which is relative to Jan 1, 0001.
 */
#define EPOCH_ADJUST    ((guint64)62135596800LL)

/* Returns the number of 100ns ticks since 1/1/1, UTC timezone */
gint64
mono_100ns_datetime (void)
{
	struct timeval tv;
	if (gettimeofday (&tv, NULL) == 0)
		return (((gint64)tv.tv_sec + EPOCH_ADJUST) * 1000000 + tv.tv_usec) * 10;
	return 0;
}

#endif

