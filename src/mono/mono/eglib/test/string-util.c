#include <glib.h>
#include <string.h>
#include <stdio.h>
#include "test.h"

/* This test is just to be used with valgrind */
static RESULT
test_strfreev (void)
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

static RESULT
test_concat (void)
{
	gchar *x = g_strconcat ("Hello", ", ", "world", (const char*)NULL);
	if (strcmp (x, "Hello, world") != 0)
		return FAILED("concat failed, got: %s", x);
	g_free (x);
	return OK;
}

static RESULT
test_split (void)
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

	v = g_strsplit ("", ":", 0);
	if (v == NULL)
		return FAILED ("g_strsplit returned NULL");
	g_strfreev (v);

	v = g_strsplit ("/home/miguel/dingus", "/", 0);
	if (v [0][0] != 0)
		return FAILED ("Got a non-empty first element");
	g_strfreev (v);

	v = g_strsplit ("appdomain1, Version=0.0.0.0, Culture=neutral", ",", 4);
	if (strcmp (v [0], "appdomain1") != 0)
		return FAILED ("Invalid value");
	
	if (strcmp (v [1], " Version=0.0.0.0") != 0)
		return FAILED ("Invalid value");
	
	if (strcmp (v [2], " Culture=neutral") != 0)
		return FAILED ("Invalid value");

	if (v [3] != NULL)
		return FAILED ("Expected only 3 elements");
	
	g_strfreev (v);

	v = g_strsplit ("abcXYdefXghiXYjklYmno", "XY", 4);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 0");
	
	if (strcmp (v [1], "defXghi") != 0)
		return FAILED ("Invalid value 1");

	if (strcmp (v [2], "jklYmno") != 0)
		return FAILED ("Invalid value 2");

	if (v [3] != NULL)
		return FAILED ("Expected only 3 elements (1)");
	
	g_strfreev (v);

	v = g_strsplit ("abcXYdefXghiXYjklYmno", "XY", 2);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 3");
	
	if (strcmp (v [1], "defXghiXYjklYmno") != 0)
		return FAILED ("Invalid value 4");

	if (v [2] != NULL)
		return FAILED ("Expected only 2 elements (2)");
	
	g_strfreev (v);

	v = g_strsplit ("abcXYdefXghiXYjklYmnoXY", "XY", 3);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 5");
	
	if (strcmp (v [1], "defXghi") != 0)
		return FAILED ("Invalid value 6");

	if (strcmp (v [2], "jklYmnoXY") != 0)
		return FAILED ("Invalid value 7");

	if (v [3] != NULL)
		return FAILED ("Expected only 3 elements (3)");
	
	g_strfreev (v);

	v = g_strsplit ("abcXYXYXYdefXY", "XY", -1);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 8");

	if (strcmp (v [1], "") != 0)
		return FAILED ("Invalid value 9");

	if (strcmp (v [2], "") != 0)
		return FAILED ("Invalid value 10");
	
	if (strcmp (v [3], "def") != 0)
		return FAILED ("Invalid value 11");

	if (strcmp (v [4], "") != 0)
		return FAILED ("Invalid value 12");

	if (v [5] != NULL)
		return FAILED ("Expected only 5 elements (4)");
	
	g_strfreev (v);

	v = g_strsplit ("XYXYXYabcXYdef", "XY", -1);
	if (strcmp (v [0], "") != 0)
		return FAILED ("Invalid value 13");
	
	if (strcmp (v [1], "") != 0)
		return FAILED ("Invalid value 14");
	
	if (strcmp (v [2], "") != 0)
		return FAILED ("Invalid value 15");
	
	if (strcmp (v [3], "abc") != 0)
		return FAILED ("Invalid value 16");
	
	if (strcmp (v [4], "def") != 0)
		return FAILED ("Invalid value 17");

	if (v [5] != NULL)
		return FAILED ("Expected only 5 elements (5)");
	
	g_strfreev (v);

	v = g_strsplit ("value=", "=", 2);
	if (strcmp (v [0], "value") != 0)
		return FAILED ("Invalid value 18; expected 'value', got '%s'", v [0]);
	if (strcmp (v [1], "") != 0)
		return FAILED ("Invalid value 19; expected '', got '%s'", v [1]);
	if (v [2] != NULL)
		return FAILED ("Expected only 2 elements (6)");

	g_strfreev (v);

	return OK;
}

static RESULT
test_split_set (void)
{
	gchar **v;
	
	v = g_strsplit_set ("abcXYdefXghiXYjklYmno", "XY", 6);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 0");

	if (strcmp (v [1], "") != 0)
		return FAILED ("Invalid value 1");
	
	if (strcmp (v [2], "def") != 0)
		return FAILED ("Invalid value 2");

	if (strcmp (v [3], "ghi") != 0)
		return FAILED ("Invalid value 3");

	if (strcmp (v [4], "") != 0)
		return FAILED ("Invalid value 4");

	if (strcmp (v [5], "jklYmno") != 0)
		return FAILED ("Invalid value 5");

	if (v [6] != NULL)
		return FAILED ("Expected only 6 elements (1)");

	g_strfreev (v);

	v = g_strsplit_set ("abcXYdefXghiXYjklYmno", "XY", 3);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 6");

	if (strcmp (v [1], "") != 0)
		return FAILED ("Invalid value 7");
	
	if (strcmp (v [2], "defXghiXYjklYmno") != 0)
		return FAILED ("Invalid value 8");

	if (v [3] != NULL)
		return FAILED ("Expected only 3 elements (2)");
	
	g_strfreev (v);

	v = g_strsplit_set ("abcXdefYghiXjklYmnoX", "XY", 5);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 9");
	
	if (strcmp (v [1], "def") != 0)
		return FAILED ("Invalid value 10");

	if (strcmp (v [2], "ghi") != 0)
		return FAILED ("Invalid value 11");

	if (strcmp (v [3], "jkl") != 0)
		return FAILED ("Invalid value 12");

	if (strcmp (v [4], "mnoX") != 0)
		return FAILED ("Invalid value 13");

	if (v [5] != NULL)
		return FAILED ("Expected only 5 elements (5)");
	
	g_strfreev (v);

	v = g_strsplit_set ("abcXYXdefXY", "XY", -1);
	if (strcmp (v [0], "abc") != 0)
		return FAILED ("Invalid value 14");

	if (strcmp (v [1], "") != 0)
		return FAILED ("Invalid value 15");

	if (strcmp (v [2], "") != 0)
		return FAILED ("Invalid value 16");
	
	if (strcmp (v [3], "def") != 0)
		return FAILED ("Invalid value 17");

	if (strcmp (v [4], "") != 0)
		return FAILED ("Invalid value 18");

	if (strcmp (v [5], "") != 0)
		return FAILED ("Invalid value 19");

	if (v [6] != NULL)
		return FAILED ("Expected only 6 elements (4)");
	
	g_strfreev (v);

	v = g_strsplit_set ("XYXabcXYdef", "XY", -1);
	if (strcmp (v [0], "") != 0)
		return FAILED ("Invalid value 20");
	
	if (strcmp (v [1], "") != 0)
		return FAILED ("Invalid value 21");
	
	if (strcmp (v [2], "") != 0)
		return FAILED ("Invalid value 22");
	
	if (strcmp (v [3], "abc") != 0)
		return FAILED ("Invalid value 23");

	if (strcmp (v [4], "") != 0)
		return FAILED ("Invalid value 24");
	
	if (strcmp (v [5], "def") != 0)
		return FAILED ("Invalid value 25");

	if (v [6] != NULL)
		return FAILED ("Expected only 6 elements (5)");
	
	g_strfreev (v);

	return OK;
}

static RESULT
test_strreverse (void)
{
	RESULT res = OK;
	gchar *a = g_strdup ("onetwothree");
	gchar *a_target = (char*)"eerhtowteno";
	gchar *b = g_strdup ("onetwothre");
	gchar *b_target = (char*)"erhtowteno";
	gchar *c = g_strdup ("");
	gchar *c_target = (char*)"";

	g_strreverse (a);
	if (strcmp (a, a_target)) {
		res = FAILED("strreverse failed. Expecting: '%s' and got '%s'\n", a, a_target);
		goto cleanup;
	}

	g_strreverse (b);
	if (strcmp (b, b_target)) {
		res = FAILED("strreverse failed. Expecting: '%s' and got '%s'\n", b, b_target);
		goto cleanup;
	}

	g_strreverse (c);
	if (strcmp (c, c_target)) {
		res = FAILED("strreverse failed. Expecting: '%s' and got '%s'\n", b, b_target);
		goto cleanup;
	}

cleanup:
	g_free (c);
	g_free (b);
	g_free (a);
	return res;
}

static RESULT
test_strjoin (void)
{
	char *s;
	
	s = g_strjoin (NULL, "a", "b", (const char*)NULL);
	if (strcmp (s, "ab") != 0)
		return FAILED ("Join of two strings with no separator fails");
	g_free (s);

	s = g_strjoin ("", "a", "b", (const char*)NULL);
	if (strcmp (s, "ab") != 0)
		return FAILED ("Join of two strings with empty separator fails");
	g_free (s);

	s = g_strjoin ("-", "a", "b", (const char*)NULL);
	if (strcmp (s, "a-b") != 0)
		return FAILED ("Join of two strings with separator fails");
	g_free (s);

	s = g_strjoin ("-", "aaaa", "bbbb", "cccc", "dddd", (const char*)NULL);
	if (strcmp (s, "aaaa-bbbb-cccc-dddd") != 0)
		return FAILED ("Join of multiple strings fails");
	g_free (s);

	s = g_strjoin ("-", (const char*)NULL);
	if (s == NULL || (strcmp (s, "") != 0))
		return FAILED ("Failed to join empty arguments");
	g_free (s);

	return OK;
}

static RESULT
test_strchug (void)
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

static RESULT
test_strchomp (void)
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

static RESULT
test_strstrip (void)
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

static RESULT
test_filename_to_uri (void)
{
#ifdef G_OS_WIN32
#else
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
#endif
	
	return OK;
}

#define fileit(so,j) do { s = g_filename_from_uri (so, NULL, NULL); if (strcmp (s, j) != 0) return FAILED("Got %s expected %s", s, j); g_free (s); } while (0);

#define ferrit(so) do { s = g_filename_from_uri (so, NULL, NULL); if (s != NULL) return FAILED ("got %s, expected NULL", s); } while (0);

static RESULT
test_filename_from_uri (void)
{
#ifndef G_OS_WIN32
	char *s;

	fileit ("file:///a", "/a");
	fileit ("file:///%41", "/A");
	fileit ("file:///home/miguel", "/home/miguel");
	fileit ("file:///home/mig%20uel", "/home/mig uel");
	fileit ("file:///home/c%2B%2B", "/home/c++");
	fileit ("file:///home/c%2b%2b", "/home/c++");
	ferrit ("/a");
	ferrit ("a");
	ferrit ("file://a");
	ferrit ("file:a");
	ferrit ("file:///%");
	ferrit ("file:///%0");
	ferrit ("file:///%jj");
#endif
	
	return OK;
}

static RESULT
test_ascii_xdigit_value (void)
{
	int i;
	gchar j;

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

	for (j = '0'; j < '9'; j++) {
		int c = g_ascii_xdigit_value (j);
		if (c  != (j - '0'))
			return FAILED ("Digits %c -> %d", j, c);
	}
	for (j = 'a'; j < 'f'; j++) {
		int c = g_ascii_xdigit_value (j);
		if (c  != (j - 'a' + 10))
			return FAILED ("Lower %c -> %d", j, c);
	}
	for (j = 'A'; j < 'F'; j++) {
		int c = g_ascii_xdigit_value (j);
		if (c  != (j - 'A' + 10))
			return FAILED ("Upper %c -> %d", j, c);
	}
	return OK;
}

#define	G_STR_DELIMITERS "_-|> <."

static void
g_strdelimits (char *a, const char *old, char new)
{
	old = old ? old : G_STR_DELIMITERS;
	while (*old)
		g_strdelimit (a, *old++, new);
}

static RESULT
test_strdelimit (void)
{
	gchar *str;

	str = g_strdup (G_STR_DELIMITERS);
	g_strdelimits (str, NULL, 'a');
	if (0 != strcmp ("aaaaaaa", str))
		return FAILED ("All delimiters: '%s'", str);
	g_free (str);
	str = g_strdup ("hola");
	g_strdelimits (str, "ha", '+');
	if (0 != strcmp ("+ol+", str))
		return FAILED ("2 delimiters: '%s'", str);
	g_free (str);
	return OK;
}

#define NUMBERS "0123456789"

static RESULT
test_strlcpy (void)
{
	const gchar *src = "onetwothree";
	gchar *dest;
	gsize i;

	dest = g_malloc (strlen (src) + 1);
	memset (dest, 0, strlen (src) + 1);
	i = g_strlcpy (dest, src, (gsize)-1);
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

	/* This is a test for g_filename_from_utf8, even if it does not look like it */
	dest = g_filename_from_utf8 (NUMBERS, strlen (NUMBERS), NULL, NULL, NULL);
	if (0 != strcmp (dest, NUMBERS))
		return FAILED ("problem [%s] and [%s]", dest, NUMBERS);
	g_free (dest);
	
	return OK;
}

static RESULT
test_strescape (void)
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

static RESULT
test_ascii_strncasecmp (void)
{
	int n;

	n = g_ascii_strncasecmp ("123", "123", 1);
	if (n != 0)
		return FAILED ("Should have been 0");
	
	n = g_ascii_strncasecmp ("423", "123", 1);
	if (n <= 0)
		return FAILED ("Should have been > 0, got %d", n);

	n = g_ascii_strncasecmp ("123", "423", 1);
	if (n >= 0)
		return FAILED ("Should have been < 0, got %d", n);

	n = g_ascii_strncasecmp ("1", "1", 10);
	if (n != 0)
		return FAILED ("Should have been 0, got %d", n);
	return OK;
}

static RESULT
test_ascii_strdown (void)
{
	const gchar *a = "~09+AaBcDeFzZ$0909EmPAbCdEEEEEZZZZAAA";
	const gchar *b = "~09+aabcdefzz$0909empabcdeeeeezzzzaaa";
	gchar *c;
	gint n, l;

	l = (gint)strlen (b);
	c = g_ascii_strdown (a, l);
	n = g_ascii_strncasecmp (b, c, l);

	if (n != 0) {
		g_free (c);
		return FAILED ("Should have been 0, got %d", n);
	}

	g_free (c);
	return OK;
}

static RESULT
test_strdupv (void)
{
	gchar **one;
	gchar **two;
	gint len;

	one = g_strdupv (NULL);
	if (one)
		return FAILED ("Should have been NULL");

	one = g_malloc (sizeof (gchar *));
	*one = NULL;
	two = g_strdupv (one);
	if (!two)
		FAILED ("Should have been not NULL");
	len = g_strv_length (two);
	if (len)
		FAILED ("Should have been 0");
	g_strfreev (two);
	g_strfreev (one);
	return NULL;
}

static Test strutil_tests [] = {
	{"g_strfreev", test_strfreev},
	{"g_strconcat", test_concat},
	{"g_strsplit", test_split},
	{"g_strsplit_set", test_split_set},
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
	{"g_ascii_strncasecmp", test_ascii_strncasecmp },
	{"g_ascii_strdown", test_ascii_strdown },
	{"g_strdupv", test_strdupv },
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(strutil_tests_init, strutil_tests)
