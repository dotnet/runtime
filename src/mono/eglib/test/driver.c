#include <stdio.h>

#include "test.h"

#include "string-util.h"
#include "hashtable.h"
#include "slist.h"

int main()
{
	run_groups(
		"string",    string_tests_init,
		"hashtable", hashtable_tests_init,
		"slist",     slist_tests_init,
		NULL
	);

	return 0;
}

