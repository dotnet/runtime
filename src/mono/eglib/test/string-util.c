#include <glib.h>
#include <string.h>
#include <stdio.h>
#include "test.h"

/* This test is just to be used with valgrind */
RESULT
test_strfreev ()
{
	gchar **array = g_new (gchar *, 4);
	array [0] = g_strdup ("one");
	array [1] = g_strdup ("two");
	array [2] = g_strdup ("three");
	array [3] = NULL;
	
	g_strfreev (array);
	g_strfreev (NULL);

	return OK;
}

RESULT
test_concat ()
{
	gchar *x = g_strconcat ("Hello", ", ", "world", NULL);
	if (strcmp (x, "Hello, world") != 0)
		return FAILED("concat failed, got: %s", x);
	g_free (x);
	return OK;
}

RESULT
test_split ()
{
	gchar **v = g_strsplit("Hello world, how are we doing today?", " ", 0);
	int i = 0;
	
	if(v == NULL) {
		return "split failed, got NULL vector";
	} else {
		for(i = 0; v[i] != NULL; i++);
		if(i != 7) {
			return FAILED("split failed, expected 7 tokens, got %d\n", i);
		}
	}
	
	g_strfreev(v);
	return OK;
}

RESULT
test_strreverse ()
{
	gchar *a = g_strdup ("onetwothree");
	gchar *a_target = "eerhtowteno";
	gchar *b = g_strdup ("onetwothre");
	gchar *b_target = "erhtowteno";

	g_strreverse (a);
	if (strcmp (a, a_target)) {
		g_free (b);
		g_free (a);
		return FAILED("strreverse failed. Expecting: '%s' and got '%s'\n", a, a_target);
	}

	g_strreverse (b);
	if (strcmp (b, b_target)) {
		g_free (b);
		g_free (a);
		return FAILED("strreverse failed. Expecting: '%s' and got '%s'\n", b, b_target);
	}
	g_free (b);
	g_free (a);
	return OK;
}

RESULT
test_strjoin ()
{
	char *s;
	
	s = g_strjoin (NULL, "a", "b", NULL);
	if (strcmp (s, "ab") != 0)
		return "Join of two strings with no separator fails";
	g_free (s);

	s = g_strjoin ("", "a", "b", NULL);
	if (strcmp (s, "ab") != 0)
		return "Join of two strings with empty separator fails";
	g_free (s);

	s = g_strjoin ("-", "a", "b", NULL);
	if (strcmp (s, "a-b") != 0)
		return "Join of two strings with separator fails";
	g_free (s);

	s = g_strjoin ("-", "aaaa", "bbbb", "cccc", "dddd", NULL);
	if (strcmp (s, "aaaa-bbbb-cccc-dddd") != 0)
		return "Join of multiple strings fails";
	g_free (s);

	s = g_strjoin ("-", NULL);
	if (s == NULL || (strcmp (s, "") != 0))
		return "Failed to join empty arguments";
	g_free (s);

	return OK;
}

RESULT
test_strchug ()
{
	char *str = g_strdup (" \t\n hola");

	g_strchug (str);
	if (strcmp ("hola", str)) {
		fprintf (stderr, "%s\n", str);
		g_free (str);
		return "Failed.";
	}
	g_free (str);
	return OK;
}

RESULT
test_strchomp ()
{
	char *str = g_strdup ("hola  \t");

	g_strchomp (str);
	if (strcmp ("hola", str)) {
		fprintf (stderr, "%s\n", str);
		g_free (str);
		return "Failed.";
	}
	g_free (str);
	return OK;
}

RESULT
test_strstrip ()
{
	char *str = g_strdup (" \t hola   ");

	g_strstrip (str);
	if (strcmp ("hola", str)) {
		fprintf (stderr, "%s\n", str);
		g_free (str);
		return "Failed.";
	}
	g_free (str);
	return OK;
}

static Test strutil_tests [] = {
	{"g_strfreev", test_strfreev},
	{"g_strconcat", test_concat},
	{"g_strsplit", test_split},
	{"g_strreverse", test_strreverse},
	{"g_strjoin", test_strjoin},
	{"g_strchug", test_strchug},
	{"g_strchomp", test_strchomp},
	{"g_strstrip", test_strstrip},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(strutil_tests_init, strutil_tests)

