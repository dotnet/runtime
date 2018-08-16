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
#include "config.h"
#include <glib.h>
#include <windows.h>

struct _GTimer {
	guint64 start;
	guint64 stop;
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

	QueryPerformanceCounter ((LARGE_INTEGER*)&timer->start);
}

void
g_timer_stop (GTimer *timer)
{
	g_return_if_fail (timer != NULL);

	QueryPerformanceCounter ((LARGE_INTEGER*)&timer->stop);
}

gdouble
g_timer_elapsed (GTimer *timer, gulong *microseconds)
{
	static guint64 freq = 0;
	guint64 delta, stop;

	if (freq == 0) {
		if (!QueryPerformanceFrequency ((LARGE_INTEGER *)&freq))
			freq = 1;
	}

	if (timer->stop == 0) {
		QueryPerformanceCounter ((LARGE_INTEGER*)&stop);
	}
	else {
		stop = timer->stop;
	}

	delta = stop - timer->start;

	if (microseconds)
		*microseconds = (gulong) (delta * (1000000.0 / freq));

	return (gdouble) delta / (gdouble) freq;
}


