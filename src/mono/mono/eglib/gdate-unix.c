/*
 * gdate-unix.c: Date and time utility functions.
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include "config.h"
#include <stdio.h>
#include <glib.h>
#include <time.h>
#include <errno.h>
#include <sys/time.h>

void
g_get_current_time (GTimeVal *result)
{
	struct timeval tv;

	g_return_if_fail (result != NULL);
	gettimeofday (&tv, NULL);
	result->tv_sec = tv.tv_sec;
	result->tv_usec = tv.tv_usec;
}

void
g_usleep (gulong microseconds)
{
#if defined(HAVE_CLOCK_NANOSLEEP) && !defined(__PASE__)
	struct timespec target;

	/*
	 * Use clock_nanosleep () with absolute time to prevent time drifting problems
	 * when nanosleep () is interrupted by signals.
	 */
	int ret = clock_gettime (CLOCK_MONOTONIC, &target);
	g_assert (ret == 0);

	target.tv_sec += microseconds / 1000000;
	target.tv_nsec += (microseconds % 1000000) * 1000;
	if (target.tv_nsec >= 1000000000) {
		target.tv_nsec -= 1000000000;
		target.tv_sec ++;
	}

	do {
		ret = clock_nanosleep (CLOCK_MONOTONIC, TIMER_ABSTIME, &target, NULL);
		if (ret != 0 && ret != EINTR)
			g_error ("%s: clock_nanosleep () returned %d", __func__, ret);
	} while (ret == EINTR);

#else
	struct timespec rem, req;

	req.tv_sec = microseconds / 1000000;
	req.tv_nsec = (microseconds % 1000000) * 1000;

	while (nanosleep (&req, &rem) == -1 && errno == EINTR)
		req = rem;
#endif
}
