#include <glib.h>
#include <string.h>
#include <stdio.h>
#include <unistd.h>
#include <pthread.h>
#include "test.h"

/* This test is just to be used with valgrind */
RESULT
test_dir ()
{
	GDir *dir;
	GError *error;
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

	error = NULL;
	dir = g_dir_open (".ljasdslakjd", 9, &error);
	if (dir != NULL)
		return FAILED ("3 opendir should fail");
	if (error == NULL)
		return FAILED ("4 got no error");
	g_error_free (error);
	error = NULL;
	dir = g_dir_open (g_get_tmp_dir (), 9, &error);
	if (dir == NULL)
		return FAILED ("5 opendir should succeed");
	if (error != NULL)
		return FAILED ("6 got an error");

	name = NULL;
	name = g_dir_read_name (dir);
	if (name == NULL)
		return FAILED ("7 didn't read a file name");
	g_dir_close (dir);
	return OK;
}

static Test dir_tests [] = {
	{"g_dir_*", test_dir},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dir_tests_init, dir_tests)

