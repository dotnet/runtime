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
run_group(Group *group)
{
	Test *tests = group->handler();
	int i;
	
	printf("[%s]\n", group->name);

	for(i = 0; tests[i].name != NULL; i++) {
		run_test(&(tests[i]));
	}
}

