#include <glib.h>
#include <string.h>
#include <stdio.h>
#include <unistd.h>
#include "test.h"

/* This test is just to be used with valgrind */
RESULT
test_buildpath ()
{
	char *s;
	
	s = g_build_path ("/", "hola///", "//mundo", NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("1 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "hola/", "/mundo", NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("2 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "hola/", "mundo", NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("3 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "hola", "/mundo", NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("4 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "/hello", "world/", NULL);
	if (strcmp (s, "/hello/world/") != 0)
		return FAILED ("5 Got wrong result, got: %s", s);
	g_free (s);
	
	/* Now test multi-char-separators */
	s = g_build_path ("**", "hello", "world", NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("6 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("**", "hello**", "world", NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("7 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("**", "hello**", "**world", NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("8 Got wrong result, got: %s", s);
	g_free (s);
	
	s = g_build_path ("**", "hello**", "**world", NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("9 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("1234567890", "hello", "world", NULL);
	if (strcmp (s, "hello1234567890world") != 0)
		return FAILED ("10 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("1234567890", "hello1234567890", "1234567890world", NULL);
	if (strcmp (s, "hello1234567890world") != 0)
		return FAILED ("11 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("1234567890", "hello12345678901234567890", "1234567890world", NULL);
	if (strcmp (s, "hello1234567890world") != 0)
		return FAILED ("12 Got wrong result, got: %s", s);
	g_free (s);

	/* Multiple */
	s = g_build_path ("/", "a", "b", "c", "d", NULL);
	if (strcmp (s, "a/b/c/d") != 0)
		return FAILED ("13 Got wrong result, got: %s", s);
	g_free (s);
	
	return OK;
}

RESULT
test_buildfname ()
{
	char *s;
	
	s = g_build_filename ("a", "b", "c", "d", NULL);
	if (strcmp (s, "a/b/c/d") != 0)
		return FAILED ("1 Got wrong result, got: %s", s);
	g_free (s);
	
	return OK;
}

char *
test_dirname ()
{
	char *s;

	s = g_path_get_dirname ("/home/miguel");
	if (strcmp (s, "/home") != 0)
		return FAILED ("Expected /home, got %s", s);
	g_free (s);

	s = g_path_get_dirname ("/home/dingus/");
	if (strcmp (s, "/home/dingus") != 0)
		return FAILED ("Expected /home/dingus, got %s", s);
	g_free (s);
	
	return OK;
}

char *
test_basename ()
{
	char *s;

	s = g_path_get_basename ("");
	if (strcmp (s, ".") != 0)
		return FAILED ("Expected `.', got %s", s);
	g_free (s);

	s = g_path_get_basename ("/home/dingus/");
	if (strcmp (s, "dingus") != 0)
		return FAILED ("1 Expected dingus, got %s", s);
	g_free (s);

	s = g_path_get_basename ("/home/dingus");
	if (strcmp (s, "dingus") != 0)
		return FAILED ("2 Expected dingus, got %s", s);
	g_free (s);

	return OK;
}

gchar *
test_ppath ()
{
	char *s;
	
	s = g_find_program_in_path ("ls");
	if (s == NULL)
		return FAILED ("No shell on this system (This assumes Unix)?");
	g_free (s);
	return OK;
}

gchar *
test_cwd ()
{
	char *dir = g_get_current_dir ();

	if (dir == NULL)
		return FAILED ("No current directory?");
	g_free (dir);
	
	if (chdir ("/bin") == -1)
		return FAILED ("No /bin?");
	
	dir = g_get_current_dir ();
	if (strcmp (dir, "/bin") != 0)
		return "Did not go to /bin?";
	g_free (dir);
	
	return OK;
}

static Test path_tests [] = {
	{"g_buildpath", test_buildpath},
	{"g_build_filename", test_buildfname},
	{"g_path_get_dirname", test_dirname},
	{"g_path_get_basename", test_basename},
	{"g_find_program_in_path", test_ppath},
	{"test_cwd", test_cwd },
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(path_tests_init, path_tests)

