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
#include <stddef.h>
#include <sys/time.h>
#include <getopt.h>

#include "test.h"
#include "tests.h"

static void print_help(char *s)
{
	gint i;
	
	printf("Usage: %s [options] [test1 test2 ... testN]\n\n", s);
	printf(" options are:\n");
	printf("   --help|-h          show this help\n");
	printf("   --time|-t          time the tests\n");
	printf("   --iterations|-i    number of times to run tests\n");
	printf("   --quiet|-q         do not print test results; if -t\n");
	printf("                      is passed, the time will print\n\n");
	printf(" test1..testN  name of test to run (all run by default)\n\n");
	printf(" available tests:\n");

	for(i = 0; test_groups[i].name != NULL; i++) {
		printf("   %s\n", test_groups[i].name);
	}

	printf("\n");
}

gint main(gint argc, gchar **argv)
{
	gint i, j, c, iterations = 1;
	GList *tests_to_run = NULL;
	double time_start, time_end;
	struct timeval tp;
	gboolean report_time = FALSE;
	gboolean quiet = FALSE;
	
	static struct option long_options [] = {
		{"help", no_argument, 0, 'h'},
		{"time", no_argument, 0, 't'},
		{"iterations", required_argument, 0, 'i'},
		{0, 0, 0, 0}
	};

	while((c = getopt_long(argc, argv, "htqi:", long_options, NULL)) != -1) {			switch(c) {
			case 'h':
				print_help(argv[0]);
				return 1;
			case 't':
				report_time = TRUE;
				break;
			case 'i':
				iterations = atoi(optarg);
				break;
			case 'q':
				quiet = TRUE;
				break;
		}
	}

	for(i = optind; i < argc; i++) {
		if(argv[i][0] == '-') {
			continue;
		}

		tests_to_run = g_list_append(tests_to_run, argv[i]);
	}

	gettimeofday(&tp, NULL);
	time_start = (double)tp.tv_sec + (1.e-6) * tp.tv_usec;

	for(i = 0; i < iterations; i++) {
		for(j = 0; test_groups[j].name != NULL; j++) {
			gboolean run = TRUE;
			
			if(tests_to_run != NULL) {
				gint k, n;
				run = FALSE;
				for(k = 0, n = g_list_length(tests_to_run); k < n; k++) {
					if(strcmp((char *)g_list_nth_data(tests_to_run, k), 
						test_groups[j].name) == 0) {
						run = TRUE;
						break;
					}
				}
			}
			
			if(run) {
				gint total, passed;
				run_group(&(test_groups[j]), &total, &passed, quiet);
				if(!quiet) {
					printf("  -- %d / %d (%g%%) --\n", passed, total,
						((gdouble)passed / (gdouble)total) * 100.0);
				}
			}
		}
	}
	
	gettimeofday(&tp, NULL);
	time_end = (double)tp.tv_sec + (1.e-6) * tp.tv_usec;
	
	if(report_time) {
		gdouble duration = time_end - time_start;
		printf("Total Time: %gs\n", duration);
	}

	if(tests_to_run != NULL) {
		g_list_free(tests_to_run);
	}

	return 0;
}

