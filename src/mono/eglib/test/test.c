#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h>

#include "test.h"

void 
run_test(Test *test)
{
	char *result; 
	printf("  %s: ", test->name);
	fflush(stdout);
	if((result = test->handler()) == NULL) {
		printf("OK\n");
	} else {
		printf("FAILED (%s)\n", result);
		/* It is ok to leak if the test fails, so we dont force people to use g_strdup */
	}
}

void
run_group(const char *name, LoadGroupHandler group_handler)
{
	Test *tests = group_handler();
	int i;
	
	printf("[%s]\n", name);

	for(i = 0; tests[i].name != NULL; i++) {
		run_test(&(tests[i]));
	}
}

void
run_groups(const char *first_name, LoadGroupHandler first_group_handler, ...)
{
	va_list args;
	va_start(args, first_group_handler);

	run_group(first_name, first_group_handler);
	
	while(1) {
		const char *name;
		LoadGroupHandler group_handler;

		if((name = (const char *)va_arg(args, const char **)) == NULL) {
			break;
		}
		
		if((group_handler = (LoadGroupHandler)va_arg(args, 
			LoadGroupHandler)) == NULL) {
			break;
		}

		run_group(name, group_handler);
	}

	va_end(args);
}

