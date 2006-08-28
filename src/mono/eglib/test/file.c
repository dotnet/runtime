#include <glib.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>
#include <stdio.h>
#include "test.h"

RESULT
test_file_get_contents ()
{
	GError *error;
	gchar *content;
	gboolean ret;
	gsize length;

	/*
	filename != NULL
	ret = g_file_get_contents (NULL, NULL, NULL, NULL);
	contents != NULL
	ret = g_file_get_contents ("", NULL, NULL, NULL);
	error no such file and fails for 'error' not being null too
	ret = g_file_get_contents ("", &content, NULL, &error);
	*/

	error = NULL;
	ret = g_file_get_contents ("", &content, NULL, &error);
	if (ret)
		return FAILED ("HAH!");
	if (error == NULL)
		return FAILED ("Got nothing as error.");
	if (content != NULL)
		return FAILED ("Content is uninitialized");

	g_error_free (error);
	error = NULL;
	ret = g_file_get_contents ("/etc/hosts", &content, &length, &error);
	if (!ret)
		return FAILED ("The error is %d %s\n", error->code, error->message);
	if (error != NULL)
		return FAILED ("Got an error returning TRUE");
	if (content == NULL)
		return FAILED ("Content is NULL");
	if (strlen (content) != length)
		return FAILED ("length is %d but the string is %d", length, strlen (content));
	g_free (content);

	return OK;
}

RESULT
test_open_tmp ()
{
	GError *error;
	gint fd;
	gchar *name = GINT_TO_POINTER (-1);

	fd = g_file_open_tmp (NULL, NULL, NULL);
	if (fd < 0)
		return FAILED ("Default failed.");
	close (fd);
	error = NULL;
	fd = g_file_open_tmp ("invalidtemplate", NULL, &error);
	if (fd != -1)
		return FAILED ("The template was invalid and accepted");
	if (error == NULL)
		return FAILED ("No error returned.");
	g_error_free (error);

	error = NULL;
	fd = g_file_open_tmp ("i/nvalidtemplate", &name, &error);
	if (fd != -1)
		return FAILED ("The template was invalid and accepted");
	if (error == NULL)
		return FAILED ("No error returned.");
	if (name == NULL)
		return FAILED ("'name' is not reset");
	g_error_free (error);

	error = NULL;
	fd = g_file_open_tmp ("valid-XXXXXX", &name, &error);
	if (fd == -1)
		return FAILED ("This should be valid");
	if (error != NULL)
		return FAILED ("No error returned.");
	if (name == NULL)
		return FAILED ("No name returned.");
	close (fd);
	unlink (name);
	g_free (name);
	return OK;
}

RESULT
test_file ()
{
	gboolean res;
	const gchar *tmp;
	gchar *path, *sympath;
	gint ignored;

	res = g_file_test (NULL, 0);
	if (res)
		return FAILED ("Should return FALSE HERE");

	res = g_file_test ("file.c", 0);
	if (res)
		return FAILED ("Should return FALSE HERE");

	tmp = g_get_tmp_dir ();
	res = g_file_test (tmp, G_FILE_TEST_EXISTS);
	if (!res)
		return FAILED ("tmp does not exist.");
	res = g_file_test (tmp, G_FILE_TEST_IS_REGULAR);
	if (res)
		return FAILED ("tmp is regular");

	res = g_file_test (tmp, G_FILE_TEST_IS_DIR);
	if (!res)
		return FAILED ("tmp is not a directory");
	res = g_file_test (tmp, G_FILE_TEST_IS_EXECUTABLE);
	if (!res)
		return FAILED ("tmp is not a executable");

	res = g_file_test (tmp, G_FILE_TEST_EXISTS | G_FILE_TEST_IS_SYMLINK);
	if (!res)
		return FAILED ("2 tmp does not exist.");
	res = g_file_test (tmp, G_FILE_TEST_IS_REGULAR | G_FILE_TEST_IS_SYMLINK);
	if (res)
		return FAILED ("2 tmp is regular");

	res = g_file_test (tmp, G_FILE_TEST_IS_DIR | G_FILE_TEST_IS_SYMLINK);
	if (!res)
		return FAILED ("2 tmp is not a directory");
	res = g_file_test (tmp, G_FILE_TEST_IS_EXECUTABLE | G_FILE_TEST_IS_SYMLINK);
	if (!res)
		return FAILED ("2 tmp is not a executable");

	close (g_file_open_tmp (NULL, &path, NULL)); /* create an empty file */
	res = g_file_test (path, G_FILE_TEST_EXISTS);
	if (!res)
		return FAILED ("3 %s should exist", path);
	res = g_file_test (path, G_FILE_TEST_IS_REGULAR);
	/* This is strange. Empty file is reported as not existing! */
	if (!res)
		return FAILED ("3 %s IS_REGULAR", path);
	res = g_file_test (path, G_FILE_TEST_IS_DIR);
	if (res)
		return FAILED ("3 %s should not be a directory", path);
	res = g_file_test (path, G_FILE_TEST_IS_EXECUTABLE);
	if (res)
		return FAILED ("3 %s should not be executable", path);
	res = g_file_test (path, G_FILE_TEST_IS_SYMLINK);
	if (res)
		return FAILED ("3 %s should not be a symlink", path);

	sympath = g_strconcat (path, "-link", NULL);
	ignored = symlink (path, sympath);
	res = g_file_test (sympath, G_FILE_TEST_EXISTS);
	if (!res)
		return FAILED ("4 %s should not exist", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_REGULAR);
	if (!res)
		return FAILED ("4 %s should not be a regular file", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_DIR);
	if (res)
		return FAILED ("4 %s should not be a directory", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_EXECUTABLE);
	if (res)
		return FAILED ("4 %s should not be executable", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_SYMLINK);
	if (!res)
		return FAILED ("4 %s should be a symlink", sympath);

	unlink (path);

	res = g_file_test (sympath, G_FILE_TEST_EXISTS);
	if (res)
		return FAILED ("5 %s should exist", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_REGULAR);
	if (res)
		return FAILED ("5 %s should be a regular file", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_DIR);
	if (res)
		return FAILED ("5 %s should not be a directory", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_EXECUTABLE);
	if (res)
		return FAILED ("5 %s should not be executable", sympath);
	res = g_file_test (sympath, G_FILE_TEST_IS_SYMLINK);
	if (!res)
		return FAILED ("5 %s should be a symlink", sympath);
	unlink (sympath);
	g_free (path);
	g_free (sympath);
	return OK;
}

static Test file_tests [] = {
	{"g_file_get_contents", test_file_get_contents},
	{"g_file_open_tmp", test_open_tmp},
	{"g_file_test", test_file},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(file_tests_init, file_tests)

