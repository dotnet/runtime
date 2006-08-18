/*
 * EGLib Unit Test Driver
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
 
#include <stdio.h>
#include <glib.h>

#include "test.h"
#include "tests.h"

static void print_help(char *s)
{
	gint i;
	
	printf("Usage: %s [options] [iterations [test1 test2 ... testN]]\n\n", s);
	printf(" options are:\n");
	printf("   --help      show this help\n\n");
	printf(" iterations:   number of times to run tests\n");
	printf(" test1..testN  name of test to run (all run by default)\n\n");
	printf(" available tests:\n");

	for(i = 0; test_groups[i].name != NULL; i++) {
		printf("   %s\n", test_groups[i].name);
	}

	printf("\n");
}

gint main(gint argc, gchar **argv)
{
	gint i, j, k, iterations = 1, tests_to_run_count = 0;
	gchar **tests_to_run = NULL;
	
	if(argc > 1) {
		for(i = 1; i < argc; i++) {
			if(strcmp(argv[i], "--help") == 0) {
				print_help(argv[0]);
				return 1;
			}
		}
	
		iterations = atoi(argv[1]);
		tests_to_run_count = argc - 2;

		if(tests_to_run_count > 0) {
			tests_to_run = (gchar **)g_new0(gchar *, tests_to_run_count + 1);

			for(i = 0; i < tests_to_run_count; i++) {
				tests_to_run[i] = argv[i + 2];
			}

			tests_to_run[tests_to_run_count] = NULL;
		}
	}

	for(i = 0; i < iterations; i++) {
		for(j = 0; test_groups[j].name != NULL; j++) {
			gboolean run = TRUE;
			
			if(tests_to_run != NULL) {
				run = FALSE;
				for(k = 0; tests_to_run[k] != NULL; k++) {
					if(strcmp(tests_to_run[k], test_groups[j].name) == 0) {
						run = TRUE;
						break;
					}
				}
			}
			
			if(run) {
				gint total, passed;
				run_group(&(test_groups[j]), &total, &passed);
				printf("  -- %d / %d (%g%%) --\n", passed, total,
					((gdouble)passed / (gdouble)total) * 100.0);
			}
		}
	}

	if(tests_to_run != NULL) {
		g_free(tests_to_run);
	}

	return 0;
}

