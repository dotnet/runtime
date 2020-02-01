#include <config.h>
#include <glib.h>
#include <string.h>
#include <stdlib.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdio.h>
#include "test.h"

#define MATCH(pat,string,error_if,msg) \
	spec = g_pattern_spec_new (pat); \
	res = g_pattern_match_string (spec, string); \
	if (res == error_if) \
		return FAILED (msg " returned %s", res ? "TRUE" : "FALSE"); \
	g_pattern_spec_free (spec);

#define TEST_MATCH(pat,string,n) MATCH (pat, string, FALSE, "MATCH " #n)
#define TEST_NO_MATCH(pat,string,n) MATCH (pat, string,TRUE, "NO_MATCH " #n)

static RESULT
test_pattern_spec (void)
{
	GPatternSpec *spec;
	gboolean res;

	/* spec = g_pattern_spec_new (NULL); */
	TEST_MATCH ("*", "hola", 1);
	TEST_MATCH ("hola", "hola", 2);
	TEST_MATCH ("????", "hola", 3);
	TEST_MATCH ("???a", "hola", 4);
	TEST_MATCH ("h??a", "hola", 5);
	TEST_MATCH ("h??*", "hola", 6);
	TEST_MATCH ("h*", "hola", 7);
	TEST_MATCH ("*hola", "hola", 8);
	TEST_MATCH ("*l*", "hola", 9);
	TEST_MATCH ("h*??", "hola", 10);
	TEST_MATCH ("h*???", "hola", 11);
	TEST_MATCH ("?o??", "hola", 12);
	TEST_MATCH ("*h*o*l*a*", "hola", 13);
	TEST_MATCH ("h*o*l*a", "hola", 14);
	TEST_MATCH ("h?*?", "hola", 15);

	TEST_NO_MATCH ("", "hola", 1);
	TEST_NO_MATCH ("?????", "hola", 2);
	TEST_NO_MATCH ("???", "hola", 3);
	TEST_NO_MATCH ("*o", "hola", 4);
	TEST_NO_MATCH ("h", "hola", 5);
	TEST_NO_MATCH ("h*????", "hola", 6);

	return OK;
}

static Test pattern_tests [] = {
	{"g_pattern_spec*", test_pattern_spec},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(pattern_tests_init, pattern_tests)
