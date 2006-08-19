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
#include <glib.h>

#include "test.h"

static gchar *last_result = NULL;

gboolean 
run_test(Test *test, gboolean quiet)
{
	gchar *result; 
	
	if(!quiet) {
		printf("  %s: ", test->name);
		fflush(stdout);
	}
	
	if((result = test->handler()) == NULL) {
		if(!quiet) {
			printf("OK\n");
		}
		
		return TRUE;
	} else {
		if(!quiet) {
			printf("FAILED (%s)\n", result);
		}
		
		if(last_result == result) {
			last_result = NULL;
			g_free(result);
		}
		
		return FALSE;
	}
}

void
run_group(Group *group, gint *total, gint *passed, gboolean quiet)
{
	Test *tests = group->handler();
	gint i, _passed = 0;

	if(!quiet) {
		printf("[%s]\n", group->name);
	}

	for(i = 0; tests[i].name != NULL; i++) {
		_passed += run_test(&(tests[i]), quiet);
	}

	if(total != NULL) {
		*total = i;
	}

	if(passed != NULL) {
		*passed = _passed;
	}
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

