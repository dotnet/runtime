/*
 * Timer
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
#include <glib.h>
#include <sys/time.h>

struct _GTimer {
	struct timeval start;
	struct timeval stop;
};

GTimer *g_timer_new (void)
{
	GTimer *timer;

	timer = g_new0 (GTimer, 1);
	g_timer_start (timer);
	return timer;
}

void
g_timer_destroy (GTimer *timer)
{
	g_return_if_fail (timer != NULL);
	g_free (timer);
}

void
g_timer_start (GTimer *timer)
{
	g_return_if_fail (timer != NULL);
	gettimeofday (&timer->start, NULL);
	memset (&timer->stop, 0, sizeof (struct timeval));
}

void
g_timer_stop (GTimer *timer)
{
	g_return_if_fail (timer != NULL);
	gettimeofday (&timer->stop, NULL);
}

gdouble
g_timer_elapsed (GTimer *timer, gulong *microseconds)
{
	struct timeval tv;
	gulong seconds;
	long usec;
	gdouble result;

	g_return_val_if_fail (timer != NULL, 0.0);

	if (timer->stop.tv_sec == 0 && timer->stop.tv_usec == 0) {
		gettimeofday (&tv, NULL);
	} else {
		tv = timer->stop;
	}

	usec = (tv.tv_usec) - (timer->start.tv_usec);
	seconds = tv.tv_sec - timer->start.tv_sec;
	if (microseconds) {
		if (usec < 0) {
			usec += 1000000;
			seconds--;
		}
		*microseconds = usec;
	}
	result = seconds * 1000000 + usec;
	return (result / 1000000);
}


