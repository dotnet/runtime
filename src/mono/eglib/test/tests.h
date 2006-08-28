#include "test.h"

DEFINE_TEST_GROUP_INIT_H(string_tests_init);
DEFINE_TEST_GROUP_INIT_H(strutil_tests_init);
DEFINE_TEST_GROUP_INIT_H(slist_tests_init);
DEFINE_TEST_GROUP_INIT_H(list_tests_init);
DEFINE_TEST_GROUP_INIT_H(hashtable_tests_init);
DEFINE_TEST_GROUP_INIT_H(ptrarray_tests_init);
DEFINE_TEST_GROUP_INIT_H(size_tests_init);
DEFINE_TEST_GROUP_INIT_H(fake_tests_init);
DEFINE_TEST_GROUP_INIT_H(array_tests_init);
DEFINE_TEST_GROUP_INIT_H(queue_tests_init);
DEFINE_TEST_GROUP_INIT_H(path_tests_init);
DEFINE_TEST_GROUP_INIT_H(shell_tests_init);
DEFINE_TEST_GROUP_INIT_H(spawn_tests_init);
DEFINE_TEST_GROUP_INIT_H(timer_tests_init);
DEFINE_TEST_GROUP_INIT_H(file_tests_init);
DEFINE_TEST_GROUP_INIT_H(pattern_tests_init);
DEFINE_TEST_GROUP_INIT_H(dir_tests_init);

static Group test_groups [] = {	
	{"string",    string_tests_init}, 
	{"strutil",   strutil_tests_init},
	{"ptrarray",  ptrarray_tests_init},
	{"slist",     slist_tests_init},
	{"list",      list_tests_init},
	{"hashtable", hashtable_tests_init},
	{"sizes",     size_tests_init},
	{"fake",      fake_tests_init},
	{"array",     array_tests_init},
	{"queue",     queue_tests_init},
	{"path",      path_tests_init},
	{"shell",     shell_tests_init},
	{"spawn",     spawn_tests_init},
	{"timer",     timer_tests_init},
	{"file",      file_tests_init},
	{"pattern",   pattern_tests_init},
	{"dir",       dir_tests_init},
	{NULL, NULL}
};

