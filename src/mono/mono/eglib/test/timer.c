#include <config.h>
#include <glib.h>
#include <string.h>
#include <math.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdlib.h>
#include <stdio.h>

#ifdef G_OS_WIN32
#include <windows.h>
#define sleep(t)                 Sleep((t) * 1000)
#endif

#include "test.h"

static RESULT
test_timer (void)
{
	GTimer *timer;
	gdouble elapsed1, elapsed2;
	gulong usec = 0;

	timer = g_timer_new ();
	sleep (1);
	elapsed1 = g_timer_elapsed (timer, NULL);
	if ((elapsed1 + 0.1) < 1.0)
		return FAILED ("Elapsed time should be around 1s and was %f", elapsed1);

	g_timer_stop (timer);
	elapsed1 = g_timer_elapsed (timer, NULL);
	elapsed2 = g_timer_elapsed (timer, &usec);
	if (fabs (elapsed1 - elapsed2) > 0.000001)
		return FAILED ("The elapsed times are not equal %f - %f.", elapsed1, elapsed2);

	elapsed2 *= 1000000;
	while (elapsed2 > 1000000)
		elapsed2 -= 1000000;

	if (fabs (usec - elapsed2) > 100.0)
		return FAILED ("usecs are wrong.");

	g_timer_destroy (timer);
	return OK;
}

static Test timer_tests [] = {
	{"g_timer", test_timer},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(timer_tests_init, timer_tests)
