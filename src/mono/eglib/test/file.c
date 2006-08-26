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
	gchar *name;

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
	return OK;
}

static Test file_tests [] = {
	{"g_file_test_contents", test_file_get_contents},
	{"g_file_open_tmp", test_open_tmp},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(file_tests_init, file_tests)

