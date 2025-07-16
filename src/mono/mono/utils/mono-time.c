/**
 * \file
 * Time utility functions.
 * Author: Paolo Molaro (<lupus@ximian.com>)
 * Copyright (C) 2008 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif


#include <mono/utils/mono-time.h>
#include <mono/utils/atomic.h>

#define MTICKS_PER_SEC (10 * 1000 * 1000)

gint64
mono_msec_ticks (void)
{
	return mono_100ns_ticks () / 10 / 1000;
}

#ifdef HOST_WIN32
#include <windows.h>

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
	return GDOUBLE_TO_INT64 ((cur_time - start_time) * (double)MTICKS_PER_SEC / freq.QuadPart);
}

/* Returns the number of 100ns ticks since Jan 1, 1601, UTC timezone */
gint64
mono_100ns_datetime (void)
{
	ULARGE_INTEGER ft;

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
	if (sizeof(ft) != sizeof(FILETIME))
		g_assert_not_reached ();
MONO_RESTORE_WARNING

	GetSystemTimeAsFileTime ((FILETIME*) &ft);
	return ft.QuadPart;
}

#else


#if defined (HAVE_SYS_PARAM_H)
#include <sys/param.h>
#endif

#if defined(HOST_DARWIN)
#include <mach/mach.h>
#include <mach/mach_time.h>
#endif

#include <time.h>

/* Returns the number of 100ns ticks from unspecified time: this should be monotonic */
gint64
mono_100ns_ticks (void)
{
	struct timeval tv;
#if defined(HOST_DARWIN)
	/* http://developer.apple.com/library/mac/#qa/qa1398/_index.html */
	static mach_timebase_info_data_t timebase;
	guint64 now = mach_absolute_time ();
	if (timebase.denom == 0) {
		mach_timebase_info (&timebase);
		timebase.denom *= 100; /* we return 100ns ticks */
	}
	return now * timebase.numer / timebase.denom;
#elif defined(CLOCK_MONOTONIC) && !defined(__PASE__)
	/* !__PASE__ is defined because i 7.1 doesn't have clock_getres */
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
#endif
	if (gettimeofday (&tv, NULL) == 0)
		return ((gint64)tv.tv_sec * 1000000 + tv.tv_usec) * 10;
	return 0;
}

/*
 * Magic number to convert unix epoch start to windows epoch start
 * Jan 1, 1970 into a value which is relative to Jan 1, 1601.
 */
#define EPOCH_ADJUST    ((guint64)11644473600LL)

/* Returns the number of 100ns ticks since 1/1/1601, UTC timezone */
gint64
mono_100ns_datetime (void)
{
	struct timeval tv;
	if (gettimeofday (&tv, NULL) == 0)
		return mono_100ns_datetime_from_timeval (tv);
	return 0;
}

gint64
mono_100ns_datetime_from_timeval (struct timeval tv)
{
	return (((gint64)tv.tv_sec + EPOCH_ADJUST) * 1000000 + tv.tv_usec) * 10;
}

#endif

#if defined(HOST_DARWIN)

void
mono_clock_init (mono_clock_id_t *clk_id)
{
	kern_return_t ret;

	do {
		ret = host_get_clock_service (mach_host_self (), SYSTEM_CLOCK, clk_id);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS)
		g_error ("%s: host_get_clock_service () returned %d", __func__, ret);
}

void
mono_clock_cleanup (mono_clock_id_t clk_id)
{
	kern_return_t ret;

	do {
		ret = mach_port_deallocate (mach_task_self (), clk_id);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS)
		g_error ("%s: mach_port_deallocate () returned %d", __func__, ret);
}

guint64
mono_clock_get_time_ns (mono_clock_id_t clk_id)
{
	kern_return_t ret;
	mach_timespec_t mach_ts;

	do {
		ret = clock_get_time (clk_id, &mach_ts);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS)
		g_error ("%s: clock_get_time () returned %d", __func__, ret);

	return ((guint64) mach_ts.tv_sec * 1000000000) + (guint64) mach_ts.tv_nsec;
}

// TODO: Potentially make this the default?
// Can we assume clock_gettime exists on all modern POSIX systems? Maybe add a better check for it in configure.ac?
#elif defined(__linux__) || defined (TARGET_WASM)

void
mono_clock_init (mono_clock_id_t *clk_id)
{
#ifdef HAVE_CLOCK_MONOTONIC
	*clk_id = CLOCK_MONOTONIC;
#endif
}

void
mono_clock_cleanup (mono_clock_id_t clk_id)
{
}

guint64
mono_clock_get_time_ns (mono_clock_id_t clk_id)
{
#ifdef HAVE_CLOCK_GETTIME
	struct timespec ts;

	if (clock_gettime (clk_id, &ts) == -1)
		g_error ("%s: clock_gettime () returned -1, errno = %d", __func__, errno);

	return ((guint64) ts.tv_sec * 1000000000) + (guint64) ts.tv_nsec;
#else
	return 0;
#endif
}

#else

void
mono_clock_init (mono_clock_id_t *clk_id)
{
	// TODO: need to implement this function for Windows
}

void
mono_clock_cleanup (mono_clock_id_t clk_id)
{
	// TODO: need to implement this function for Windows
}

guint64
mono_clock_get_time_ns (mono_clock_id_t clk_id)
{
	// TODO: need to implement time stamp function for Windows
	return 0;
}

#endif
