/*
 * gclock_nanosleep.c: Clock nanosleep on platforms that have clock_nanosleep().
 *
 * Copyright 2022 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <config.h>
#include <glib.h>
#include <errno.h>

gint
g_clock_nanosleep (clockid_t clockid, gint flags, const struct timespec *request, struct timespec *remain)
{
	gint ret = 0;

#if defined(HAVE_CLOCK_NANOSLEEP) && !defined(__PASE__)
	ret = clock_nanosleep (clockid, flags, request, remain);
#else
	g_assert_not_reached ();
#endif

#ifdef HOST_ANDROID
	// Workaround for incorrect implementation of clock_nanosleep return value on old Android (<=5.1)
	// See https://github.com/xamarin/xamarin-android/issues/6600
	if (ret == -1)
		ret = errno;
#endif

	return ret;
}
