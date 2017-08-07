#include <config.h>
#include <glib.h>
#include <string.h>
#include <stdio.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef G_OS_UNIX
#include <pthread.h>
#endif
#include "test.h"

#ifdef G_OS_WIN32
#include <direct.h>
#define chdir _chdir
#endif

/* This test is just to be used with valgrind */
RESULT
test_buildpath ()
{
	char *s;
	char *buffer = "var/private";
	char *dir = "/";
	
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

	s = g_build_path ("/", "/a", "", "/c/", NULL);
	if (strcmp (s, "/a/c/") != 0)
		return FAILED ("14 Got wrong result, got: %s", s);
	g_free (s);

	/* Null */
	s = g_build_path ("/", NULL, NULL);
	if (s == NULL)
		return FAILED ("must get a non-NULL return");
	if (s [0] != 0)
		return FAILED ("must get an empty string");

	// This is to test the regression introduced by Levi for the Windows support
	// that code errouneously read below the allowed area (in this case dir [-1]).
	// and caused all kinds of random errors.
	dir = "//";
	dir++;
	s = g_build_filename (dir, buffer, NULL);
	if (s [0] != '/')
		return FAILED ("Must have a '/' at the start");

	g_free (s);
	return OK;
}

RESULT
test_buildfname ()
{
	char *s;
	
	s = g_build_filename ("a", "b", "c", "d", NULL);
#ifdef G_OS_WIN32
	if (strcmp (s, "a\\b\\c\\d") != 0)
#else
	if (strcmp (s, "a/b/c/d") != 0)
#endif
		return FAILED ("1 Got wrong result, got: %s", s);
	g_free (s);

#ifdef G_OS_WIN32
	s = g_build_filename ("C:\\", "a", NULL);
	if (strcmp (s, "C:\\a") != 0)
#else
	s = g_build_filename ("/", "a", NULL);
	if (strcmp (s, "/a") != 0)
#endif
		return FAILED ("1 Got wrong result, got: %s", s);

#ifndef G_OS_WIN32
	s = g_build_filename ("/", "foo", "/bar", "tolo/", "/meo/", NULL);
	if (strcmp (s, "/foo/bar/tolo/meo/") != 0)
		return FAILED ("1 Got wrong result, got: %s", s);
#endif
	
	return OK;
}

char *
test_dirname ()
{
	char *s;

#ifdef G_OS_WIN32
	s = g_path_get_dirname ("c:\\home\\miguel");
	if (strcmp (s, "c:\\home") != 0)
		return FAILED ("Expected c:\\home, got %s", s);
	g_free (s);

	s = g_path_get_dirname ("c:/home/miguel");
	if (strcmp (s, "c:/home") != 0)
		return FAILED ("Expected c:/home, got %s", s);
	g_free (s);

	s = g_path_get_dirname ("c:\\home\\dingus\\");
	if (strcmp (s, "c:\\home\\dingus") != 0)
		return FAILED ("Expected c:\\home\\dingus, got %s", s);
	g_free (s);

	s = g_path_get_dirname ("dir.c");
	if (strcmp (s, ".") != 0)
		return FAILED ("Expected `.', got %s", s);
	g_free (s);

	s = g_path_get_dirname ("c:\\index.html");
	if (strcmp (s, "c:") != 0)
		return FAILED ("Expected [c:], got [%s]", s);
#else
	s = g_path_get_dirname ("/home/miguel");
	if (strcmp (s, "/home") != 0)
		return FAILED ("Expected /home, got %s", s);
	g_free (s);

	s = g_path_get_dirname ("/home/dingus/");
	if (strcmp (s, "/home/dingus") != 0)
		return FAILED ("Expected /home/dingus, got %s", s);
	g_free (s);

	s = g_path_get_dirname ("dir.c");
	if (strcmp (s, ".") != 0)
		return FAILED ("Expected `.', got %s", s);
	g_free (s);

	s = g_path_get_dirname ("/index.html");
	if (strcmp (s, "/") != 0)
		return FAILED ("Expected [/], got [%s]", s);
#endif	
	return OK;
}

char *
test_basename ()
{
	char *s;

#ifdef G_OS_WIN32
	s = g_path_get_basename ("");
	if (strcmp (s, ".") != 0)
		return FAILED ("Expected `.', got %s", s);
	g_free (s);

	s = g_path_get_basename ("c:\\home\\dingus\\");
	if (strcmp (s, "dingus") != 0)
		return FAILED ("1 Expected dingus, got %s", s);
	g_free (s);

	s = g_path_get_basename ("c:/home/dingus/");
	if (strcmp (s, "dingus") != 0)
		return FAILED ("1 Expected dingus, got %s", s);
	g_free (s);

	s = g_path_get_basename ("c:\\home\\dingus");
	if (strcmp (s, "dingus") != 0)
		return FAILED ("2 Expected dingus, got %s", s);
	g_free (s);

	s = g_path_get_basename ("c:/home/dingus");
	if (strcmp (s, "dingus") != 0)
		return FAILED ("2 Expected dingus, got %s", s);
	g_free (s);
#else
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
#endif
	return OK;
}

gchar *
test_ppath ()
{
	char *s;
#ifdef G_OS_WIN32
	const gchar *searchfor = "explorer.exe";
#else
	const gchar *searchfor = "ls";
#endif
	s = g_find_program_in_path (searchfor);
	if (s == NULL)
		return FAILED ("No %s on this system?", searchfor);
	g_free (s);
	return OK;
}

gchar *
test_ppath2 ()
{
	char *s;
	const char *path = g_getenv ("PATH");
#ifdef G_OS_WIN32
	const gchar *searchfor = "test_eglib.exe";
#else
	const gchar *searchfor = "test-glib";
#endif
	
	g_setenv ("PATH", "", TRUE);
	s = g_find_program_in_path ("ls");
	if (s != NULL) {
		g_setenv ("PATH", path, TRUE);
		return FAILED ("Found something interesting here: %s", s);
	}
	g_free (s);
	s = g_find_program_in_path (searchfor);
	if (s == NULL) {
		g_setenv ("PATH", path, TRUE);
		return FAILED ("It should find '%s' in the current directory.", searchfor);
	}
	g_free (s);
	g_setenv ("PATH", path, TRUE);
	return OK;
}

#ifndef DISABLE_FILESYSTEM_TESTS
gchar *
test_cwd ()
{
	char *dir = g_get_current_dir ();
#ifdef G_OS_WIN32
	const gchar *newdir = "C:\\Windows";
#else
	const gchar *newdir = "/bin";
#endif

	if (dir == NULL)
		return FAILED ("No current directory?");
	g_free (dir);
	
	if (chdir (newdir) == -1)
		return FAILED ("No %s?", newdir);
	
	dir = g_get_current_dir ();
	if (strcmp (dir, newdir) != 0)
		return FAILED("Did not go to %s?", newdir);
	g_free (dir);
	
	return OK;
}
#else
gchar *
test_cwd ()
{
	return OK;
}
#endif

gchar *
test_misc ()
{
	const char *home = g_get_home_dir ();
	const char *tmp = g_get_tmp_dir ();
	
	if (home == NULL)
		return FAILED ("Where did my home go?");

	if (tmp == NULL)
		return FAILED ("Where did my /tmp go?");

	return OK;
}

static Test path_tests [] = {
	{"g_build_filename", test_buildfname},
	{"g_buildpath", test_buildpath},
	{"g_path_get_dirname", test_dirname},
	{"g_path_get_basename", test_basename},
	{"g_find_program_in_path", test_ppath},
	{"g_find_program_in_path2", test_ppath2},
	{"test_cwd", test_cwd },
	{"test_misc", test_misc },
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(path_tests_init, path_tests)


