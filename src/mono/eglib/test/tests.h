#include "test.h"

DEFINE_TEST_GROUP_INIT_H(string_tests_init);
DEFINE_TEST_GROUP_INIT_H(strutil_tests_init);
DEFINE_TEST_GROUP_INIT_H(slist_tests_init);
DEFINE_TEST_GROUP_INIT_H(list_tests_init);
DEFINE_TEST_GROUP_INIT_H(hashtable_tests_init);
DEFINE_TEST_GROUP_INIT_H(ptrarray_tests_init);

static Group test_groups [] = {	
	{"string",    string_tests_init}, 
	{"strutil",   strutil_tests_init},
	{"ptrarray",  ptrarray_tests_init},
	{"slist",     slist_tests_init},
	{"list",      list_tests_init},
	{"hashtable", hashtable_tests_init},
	{NULL, NULL}
};

