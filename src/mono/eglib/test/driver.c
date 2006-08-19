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
#include <getopt.h>

#include "test.h"
#include "tests.h"

typedef struct _StringArray {
	gchar **strings;
	gint length;
} StringArray;

static StringArray *
string_array_append(StringArray *array, gchar *string)
{
	if(array == NULL) {
		array = g_new0(StringArray, 1);
		array->length = 1;
		array->strings = g_malloc(sizeof(gchar *) * 2);
	} else {
		array->length++;
		array->strings = g_realloc(array->strings, sizeof(gchar *) 
			* (array->length + 1));
	}
	
	array->strings[array->length - 1] = string;
	array->strings[array->length] = NULL;

	return array;
}

static void
string_array_free(StringArray *array)
{
	g_free(array->strings);
	g_free(array);
}

static void print_help(char *s)
{
	gint i;
	
	printf("Usage: %s [OPTION]... [TESTGROUP]...\n\n", s);
	printf("OPTIONS are:\n");
	printf("  -h, --help          show this help\n");
	printf("  -t, --time          time the tests\n");
	printf("  -i, --iterations    number of times to run tests\n");
	printf("  -q, --quiet         do not print test results; "
		"final time always prints\n");
	printf("  -n                  print final time without labels, "
		"nice for scripts\n\n");
	printf("TESTGROUPS available:\n");

	for(i = 0; test_groups[i].name != NULL; i++) {
		printf("  %s\n", test_groups[i].name);
	}

	printf("\n");
}

gint main(gint argc, gchar **argv)
{
	gint i, j, c, iterations = 1;
	StringArray *tests_to_run = NULL;
	gdouble time_start;
	gboolean report_time = FALSE;
	gboolean quiet = FALSE;
	gboolean global_failure = FALSE;
	gboolean no_final_time_labels = FALSE;
	
	static struct option long_options [] = {
		{"help",       no_argument,       0, 'h'},
		{"time",       no_argument,       0, 't'},
		{"quiet",      no_argument,       0, 'q'},
		{"iterations", required_argument, 0, 'i'},
		{0, 0, 0, 0}
	};

	while((c = getopt_long(argc, argv, "htqni:", long_options, NULL)) != -1) {			switch(c) {
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
			case 'n':
				no_final_time_labels = TRUE;
				break;
		}
	}

	for(i = optind; i < argc; i++) {
		if(argv[i][0] == '-') {
			continue;
		}

		tests_to_run = string_array_append(tests_to_run, argv[i]);
	}

	time_start = get_timestamp();
	
	for(j = 0; test_groups[j].name != NULL; j++) {
		gboolean run = TRUE;
			
		if(tests_to_run != NULL) {
			gint k;
			run = FALSE;
			for(k = 0; k < tests_to_run->length; k++) {
				if(strcmp((char *)tests_to_run->strings[k], 
					test_groups[j].name) == 0) {
					run = TRUE;
					break;
				}
			}
		}
			
		if(run) {
			gboolean passed = run_group(&(test_groups[j]), 
				iterations, quiet, report_time);
			if(!passed && !global_failure) {
				global_failure = TRUE;
			}
		}
	}
	
	if(!quiet) {
		printf("=============================\n");
		printf("Overall result: %s\n", global_failure ? "FAILED" : "OK");
	}
	
	if(report_time) {
		gdouble duration = get_timestamp() - time_start;
		if(no_final_time_labels) {
			printf("%g\n", duration);
		} else {
			printf("%s Total Time: %g\n", DRIVER_NAME, duration);
		}
	}

	if(tests_to_run != NULL) {
		string_array_free(tests_to_run);
	}

	return 0;
}

