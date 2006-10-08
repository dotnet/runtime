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
	const gchar *to_split = "Hello world, how are we doing today?";
	gint i;
	gchar **v;
	
	v= g_strsplit(to_split, " ", 0);
	
	if(v == NULL) {
		return FAILED("split failed, got NULL vector (1)");
	}
	
	for(i = 0; v[i] != NULL; i++);
	if(i != 7) {
		return FAILED("split failed, expected 7 tokens, got %d", i);
	}
	
	g_strfreev(v);

	v = g_strsplit(to_split, ":", -1);
	if(v == NULL) {
		return FAILED("split failed, got NULL vector (2)");
	}

	for(i = 0; v[i] != NULL; i++);
	if(i != 1) {
		return FAILED("split failed, expected 1 token, got %d", i);
	}

	if(strcmp(v[0], to_split) != 0) {
		return FAILED("expected vector[0] to be '%s' but it was '%s'",
			to_split, v[0]);
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
		return FAILED ("Join of two strings with no separator fails");
	g_free (s);

	s = g_strjoin ("", "a", "b", NULL);
	if (strcmp (s, "ab") != 0)
		return FAILED ("Join of two strings with empty separator fails");
	g_free (s);

	s = g_strjoin ("-", "a", "b", NULL);
	if (strcmp (s, "a-b") != 0)
		return FAILED ("Join of two strings with separator fails");
	g_free (s);

	s = g_strjoin ("-", "aaaa", "bbbb", "cccc", "dddd", NULL);
	if (strcmp (s, "aaaa-bbbb-cccc-dddd") != 0)
		return FAILED ("Join of multiple strings fails");
	g_free (s);

	s = g_strjoin ("-", NULL);
	if (s == NULL || (strcmp (s, "") != 0))
		return FAILED ("Failed to join empty arguments");
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
		return FAILED ("Failed.");
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
		return FAILED ("Failed.");
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
		return FAILED ("Failed.");
	}
	g_free (str);
	return OK;
}

#define urit(so,j) do { s = g_filename_to_uri (so, NULL, NULL); if (strcmp (s, j) != 0) return FAILED("Got %s expected %s", s, j); g_free (s); } while (0);

#define errit(so) do { s = g_filename_to_uri (so, NULL, NULL); if (s != NULL) return FAILED ("got %s, expected NULL", s); } while (0);

RESULT
test_filename_to_uri ()
{
	char *s;

	urit ("/a", "file:///a");
	urit ("/home/miguel", "file:///home/miguel");
	urit ("/home/mig uel", "file:///home/mig%20uel");
	urit ("/\303\241", "file:///%C3%A1");
	urit ("/\303\241/octal", "file:///%C3%A1/octal");
	urit ("/%", "file:///%25");
	urit ("/\001\002\003\004\005\006\007\010\011\012\013\014\015\016\017\020\021\022\023\024\025\026\027\030\031\032\033\034\035\036\037\040", "file:///%01%02%03%04%05%06%07%08%09%0A%0B%0C%0D%0E%0F%10%11%12%13%14%15%16%17%18%19%1A%1B%1C%1D%1E%1F%20");
	urit ("/!$&'()*+,-./", "file:///!$&'()*+,-./");
	urit ("/\042\043\045", "file:///%22%23%25");
	urit ("/0123456789:=", "file:///0123456789:=");
	urit ("/\073\074\076\077", "file:///%3B%3C%3E%3F");
	urit ("/\133\134\135\136_\140\173\174\175", "file:///%5B%5C%5D%5E_%60%7B%7C%7D");
	urit ("/\173\174\175\176\177\200", "file:///%7B%7C%7D~%7F%80");
	urit ("/@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", "file:///@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
	errit ("a");
	errit ("./hola");
	
	return OK;
}

#define fileit(so,j) do { s = g_filename_from_uri (so, NULL, NULL); if (strcmp (s, j) != 0) return FAILED("Got %s expected %s", s, j); g_free (s); } while (0);

#define ferrit(so) do { s = g_filename_from_uri (so, NULL, NULL); if (s != NULL) return FAILED ("got %s, expected NULL", s); } while (0);

RESULT
test_filename_from_uri ()
{
	char *s;

	fileit ("file:///a", "/a");
	fileit ("file:///%41", "/A");
	fileit ("file:///home/miguel", "/home/miguel");
	fileit ("file:///home/mig%20uel", "/home/mig uel");
	ferrit ("/a");
	ferrit ("a");
	ferrit ("file://a");
	ferrit ("file:a");
	ferrit ("file:///%");
	ferrit ("file:///%0");
	ferrit ("file:///%jj");
	
	return OK;
}

RESULT
test_ascii_xdigit_value ()
{
	int i;

	i = g_ascii_xdigit_value ('9' + 1);
	if (i != -1)
		return FAILED ("'9' + 1");
	i = g_ascii_xdigit_value ('0' - 1);
	if (i != -1)
		return FAILED ("'0' - 1");
	i = g_ascii_xdigit_value ('a' - 1);
	if (i != -1)
		return FAILED ("'a' - 1");
	i = g_ascii_xdigit_value ('f' + 1);
	if (i != -1)
		return FAILED ("'f' + 1");
	i = g_ascii_xdigit_value ('A' - 1);
	if (i != -1)
		return FAILED ("'A' - 1");
	i = g_ascii_xdigit_value ('F' + 1);
	if (i != -1)
		return FAILED ("'F' + 1");

	for (i = '0'; i < '9'; i++) {
		int c = g_ascii_xdigit_value (i);
		if (c  != (i - '0'))
			return FAILED ("Digits %c -> %d", i, c);
	}
	for (i = 'a'; i < 'f'; i++) {
		int c = g_ascii_xdigit_value (i);
		if (c  != (i - 'a' + 10))
			return FAILED ("Lower %c -> %d", i, c);
	}
	for (i = 'A'; i < 'F'; i++) {
		int c = g_ascii_xdigit_value (i);
		if (c  != (i - 'A' + 10))
			return FAILED ("Upper %c -> %d", i, c);
	}
	return OK;
}

RESULT
test_strdelimit ()
{
	gchar *str;

	str = g_strdup (G_STR_DELIMITERS);
	str = g_strdelimit (str, NULL, 'a');
	if (0 != strcmp ("aaaaaaa", str))
		return FAILED ("All delimiters: '%s'", str);
	g_free (str);
	str = g_strdup ("hola");
	str = g_strdelimit (str, "ha", '+');
	if (0 != strcmp ("+ol+", str))
		return FAILED ("2 delimiters: '%s'", str);
	g_free (str);
	return OK;
}

RESULT
test_strlcpy ()
{
	const gchar *src = "onetwothree";
	gchar *dest;
	int i;

	dest = g_malloc (strlen (src) + 1);
	memset (dest, 0, strlen (src) + 1);
	i = g_strlcpy (dest, src, -1);
	if (i != strlen (src))
		return FAILED ("Test1 got %d", i);

	if (0 != strcmp (dest, src))
		return FAILED ("Src and dest not equal");

	i = g_strlcpy (dest, src, 3);
	if (i != strlen (src))
		return FAILED ("Test1 got %d", i);
	if (0 != strcmp (dest, "on"))
		return FAILED ("Test2");

	i = g_strlcpy (dest, src, 1);
	if (i != strlen (src))
		return FAILED ("Test3 got %d", i);
	if (*dest != '\0')
		return FAILED ("Test4");

	i = g_strlcpy (dest, src, 12345);
	if (i != strlen (src))
		return FAILED ("Test4 got %d", i);
	if (0 != strcmp (dest, src))
		return FAILED ("Src and dest not equal 2");
	g_free (dest);
	return OK;
}

RESULT
test_strescape ()
{
	gchar *str;

	str = g_strescape ("abc", NULL);
	if (strcmp ("abc", str))
		return FAILED ("#1");
	str = g_strescape ("\t\b\f\n\r\\\"abc", NULL);
	if (strcmp ("\\t\\b\\f\\n\\r\\\\\\\"abc", str))
		return FAILED ("#2 %s", str);
	str = g_strescape ("\001abc", NULL);
	if (strcmp ("\\001abc", str))
		return FAILED ("#3 %s", str);
	str = g_strescape ("\001abc", "\001");
	if (strcmp ("\001abc", str))
		return FAILED ("#3 %s", str);
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
	{"g_filename_to_uri", test_filename_to_uri},
	{"g_filename_from_uri", test_filename_from_uri},
	{"g_ascii_xdigit_value", test_ascii_xdigit_value},
	{"g_strdelimit", test_strdelimit},
	{"g_strlcpy", test_strlcpy},
	{"g_strescape", test_strescape},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(strutil_tests_init, strutil_tests)

