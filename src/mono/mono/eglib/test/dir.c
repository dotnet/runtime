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

/* This test is just to be used with valgrind */
static RESULT
test_dir (void)
{
	GDir *dir;
	GError *gerror;
	const gchar *name;

	/*
	dir = g_dir_open (NULL, 0, NULL);
	*/
	dir = g_dir_open ("", 0, NULL);
	if (dir != NULL)
		return FAILED ("1 Should be an error");

	dir = g_dir_open ("", 9, NULL);
	if (dir != NULL)
		return FAILED ("2 Should be an error");

	gerror = NULL;
	dir = g_dir_open (".ljasdslakjd", 9, &gerror);
	if (dir != NULL)
		return FAILED ("3 opendir should fail");
	if (gerror == NULL)
		return FAILED ("4 got no error");
	g_error_free (gerror);
	gerror = NULL;
	dir = g_dir_open (g_get_tmp_dir (), 9, &gerror);
	if (dir == NULL)
		return FAILED ("5 opendir should succeed");
	if (gerror != NULL)
		return FAILED ("6 got an error");
	name = NULL;
	name = g_dir_read_name (dir);
	if (name == NULL)
		return FAILED ("7 didn't read a file name");
	while ((name = g_dir_read_name (dir)) != NULL) {
		if (strcmp (name, ".") == 0)
			return FAILED (". directory found");
		if (strcmp (name, "..") == 0)
			return FAILED (".. directory found");
	}
	g_dir_close (dir);
	return OK;
}

static Test dir_tests [] = {
	{"g_dir_*", test_dir},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dir_tests_init, dir_tests)
