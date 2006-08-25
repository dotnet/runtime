#include <glib.h>
#include <string.h>
#include <stdio.h>
#include "test.h"

RESULT
test_spawn_sync ()
{
	gchar *out;
	gchar *err;
	gint status;
	GError *error = NULL;

	if (!g_spawn_command_line_sync ("ls", &out, &err, &status, &error))
		return FAILED ("Error executing 'ls'");

	if (status != 0)
		return FAILED ("Status is %d", status);

	if (out == NULL || strlen (out) == 0)
		return FAILED ("Didn't get any output from ls!?");

	g_free (out);
	g_free (err);
	return OK;
}

static Test spawn_tests [] = {
	{"g_shell_spawn_sync", test_spawn_sync},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(spawn_tests_init, spawn_tests)

