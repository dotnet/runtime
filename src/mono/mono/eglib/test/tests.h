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
DEFINE_TEST_GROUP_INIT_H(markup_tests_init);
DEFINE_TEST_GROUP_INIT_H(unicode_tests_init);
DEFINE_TEST_GROUP_INIT_H(utf8_tests_init);
DEFINE_TEST_GROUP_INIT_H(endian_tests_init);
DEFINE_TEST_GROUP_INIT_H(module_tests_init);
DEFINE_TEST_GROUP_INIT_H(memory_tests_init);

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
	{"markup",    markup_tests_init},
#if !DISABLE_PROCESS_TESTS 
	{"spawn",     spawn_tests_init},
	{"module",    module_tests_init},
#endif
#if !DISABLE_FILESYSTEM_TESTS
	{"file",      file_tests_init},
#endif
	{"timer",     timer_tests_init},
	{"pattern",   pattern_tests_init},
	{"dir",       dir_tests_init},
	{"unicode",   unicode_tests_init},
	{"utf8",      utf8_tests_init},
	{"endian",    endian_tests_init},
	{"memory",    memory_tests_init},
	{NULL, NULL}
};

