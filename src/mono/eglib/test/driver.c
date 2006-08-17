#include <stdio.h>

#include "test.h"
#include "tests.h"

int main()
{
	run_groups(
		"string",    strutil_tests_init,
		"hashtable", hashtable_tests_init,
		"slist",     slist_tests_init,
		"gstring",   string_tests_init,
		NULL
	);

	return 0;
}

