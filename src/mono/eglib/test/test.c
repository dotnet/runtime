/*
 * EGLib Unit Group/Test Runners
 *
 * Author:
 *   Aaron Bockover (abockover@novell.com)
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

#define _GNU_SOURCE
#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h>
#include <sys/time.h>
#include <glib.h>

#include "test.h"

static gchar *last_result = NULL;

gboolean 
run_test(Test *test, gchar **result_out)
{
	gchar *result; 

	if((result = test->handler()) == NULL) {
		*result_out = NULL;
		return TRUE;
	} else {
		*result_out = result;	
		return FALSE;
	}
}

gboolean
run_group(Group *group, gint iterations, gboolean quiet, gboolean time)
{
	Test *tests = group->handler();
	gint i, j, _passed = 0;
	gdouble start_time_group, start_time_test;
	
	if(!quiet) {
		printf("[%s] (%dx)\n", group->name, iterations);
	}

	start_time_group = get_timestamp();

	for(i = 0; tests[i].name != NULL; i++) {
		gchar *result;
		gboolean iter_pass;
		
		if(!quiet) {
			printf("  %s: ", tests[i].name);
		}

		start_time_test = get_timestamp();
		
		for(j = 0; j < iterations; j++) {
			iter_pass = run_test(&(tests[i]), &result);
			if(!iter_pass) {
				break;
			}
		}

		if(iter_pass) {
			_passed++;
			if(!quiet) {
				printf("OK (%g)\n", get_timestamp() - start_time_test);
			}
		} else  {			
			if(!quiet) {
				printf("FAILED (%s)\n", result);
			}
			if(last_result == result) {
				last_result = NULL;
				g_free(result);
			}
		}
	}

	if(!quiet) {
		printf("  -- %d / %d (%g%%, %g)--\n", _passed, i,
			((gdouble)_passed / (gdouble)i) * 100.0,
			get_timestamp() - start_time_group);
	}

	return _passed == i;
}

RESULT
FAILED(const gchar *format, ...)
{
	gchar *ret;
	va_list args;
	gint n;

	va_start(args, format);
	n = vasprintf(&ret, format, args);
	va_end(args);

	if(n == -1) {
		last_result = NULL;
		return NULL;
	}

	last_result = ret;
	return ret;
}

gdouble
get_timestamp()
{
	struct timeval tp;
	gettimeofday(&tp, NULL);
	return (gdouble)tp.tv_sec + (1.e-6) * tp.tv_usec;
}

