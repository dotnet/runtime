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
static RESULT
test_buildpath (void)
{
	char *s;
	const char *buffer = "var/private";
	const char *dir = "/";

	s = g_build_path ("/", "hola///", "//mundo", (const char*)NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("1 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "hola/", "/mundo", (const char*)NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("2 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "hola/", "mundo", (const char*)NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("3 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "hola", "/mundo", (const char*)NULL);
	if (strcmp (s, "hola/mundo") != 0)
		return FAILED ("4 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "/hello", "world/", (const char*)NULL);
	if (strcmp (s, "/hello/world/") != 0)
		return FAILED ("5 Got wrong result, got: %s", s);
	g_free (s);

	/* Now test multi-char-separators */
	s = g_build_path ("**", "hello", "world", (const char*)NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("6 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("**", "hello**", "world", (const char*)NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("7 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("**", "hello**", "**world", (const char*)NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("8 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("**", "hello**", "**world", (const char*)NULL);
	if (strcmp (s, "hello**world") != 0)
		return FAILED ("9 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("1234567890", "hello", "world", (const char*)NULL);
	if (strcmp (s, "hello1234567890world") != 0)
		return FAILED ("10 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("1234567890", "hello1234567890", "1234567890world", (const char*)NULL);
	if (strcmp (s, "hello1234567890world") != 0)
		return FAILED ("11 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("1234567890", "hello12345678901234567890", "1234567890world", (const char*)NULL);
	if (strcmp (s, "hello1234567890world") != 0)
		return FAILED ("12 Got wrong result, got: %s", s);
	g_free (s);

	/* Multiple */
	s = g_build_path ("/", "a", "b", "c", "d", (const char*)NULL);
	if (strcmp (s, "a/b/c/d") != 0)
		return FAILED ("13 Got wrong result, got: %s", s);
	g_free (s);

	s = g_build_path ("/", "/a", "", "/c/", (const char*)NULL);
	if (strcmp (s, "/a/c/") != 0)
		return FAILED ("14 Got wrong result, got: %s", s);
	g_free (s);

	/* Null */
	s = g_build_path ("/", NULL, (const char*)NULL);
	if (s == NULL)
		return FAILED ("must get a non-NULL return");
	if (s [0] != 0)
		return FAILED ("must get an empty string");

	// This is to test the regression introduced by Levi for the Windows support
	// that code errouneously read below the allowed area (in this case dir [-1]).
	// and caused all kinds of random errors.
	dir = "//";
	dir++;
	s = g_build_filename (dir, buffer, (const char*)NULL);
	if (s [0] != '/')
		return FAILED ("Must have a '/' at the start");

	g_free (s);
	return OK;
}

static RESULT
test_buildfname (void)
{
	char *s;

	s = g_build_filename ("a", "b", "c", "d", (const char*)NULL);
#ifdef G_OS_WIN32
	if (strcmp (s, "a\\b\\c\\d") != 0)
#else
	if (strcmp (s, "a/b/c/d") != 0)
#endif
		return FAILED ("1 Got wrong result, got: %s", s);
	g_free (s);

#ifdef G_OS_WIN32
	s = g_build_filename ("C:\\", "a", (const char*)NULL);
	if (strcmp (s, "C:\\a") != 0)
#else
	s = g_build_filename ("/", "a", (const char*)NULL);
	if (strcmp (s, "/a") != 0)
#endif
		return FAILED ("1 Got wrong result, got: %s", s);

#ifndef G_OS_WIN32
	s = g_build_filename ("/", "foo", "/bar", "tolo/", "/meo/", (const char*)NULL);
	if (strcmp (s, "/foo/bar/tolo/meo/") != 0)
		return FAILED ("1 Got wrong result, got: %s", s);
#endif

	return OK;
}

static char *
test_dirname (void)
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

static char *
test_basename (void)
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

#ifndef DISABLE_FILESYSTEM_TESTS
static gchar *
test_cwd (void)
{
	char *dir = g_get_current_dir ();
#ifdef G_OS_WIN32
	const gchar *newdir = "C:\\Windows";
#else
	/*
	 * AIX/PASE have /bin -> /usr/bin, and chdir/getcwd follows links.
	 * Use a directory available on all systems that shouldn't be a link.
	 */
	const gchar *newdir = "/";
#endif

	if (dir == NULL)
		return FAILED ("No current directory?");
	g_free (dir);

	if (chdir (newdir) == -1)
		return FAILED ("No %s?", newdir);

	dir = g_get_current_dir ();
	if (strcmp (dir, newdir) != 0)
		return FAILED("Did not go to %s? Instead in %s", newdir, dir);
	g_free (dir);

	return OK;
}
#else
static gchar *
test_cwd (void)
{
	return OK;
}
#endif

static gchar *
test_misc (void)
{
	const char *tmp = g_get_tmp_dir ();

	if (tmp == NULL)
		return FAILED ("Where did my /tmp go?");

	return OK;
}

static Test path_tests [] = {
	{"g_build_filename", test_buildfname},
	{"g_buildpath", test_buildpath},
	{"g_path_get_dirname", test_dirname},
	{"g_path_get_basename", test_basename},
	{"test_cwd", test_cwd },
	{"test_misc", test_misc },
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(path_tests_init, path_tests)
