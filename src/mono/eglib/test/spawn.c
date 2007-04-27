#include <config.h>
#include <glib.h>
#include <string.h>
#include <stdio.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include "test.h"

#ifdef G_OS_WIN32
#include <io.h>
#define read _read
#define close _close
#endif

RESULT
test_spawn_sync ()
{
	gchar *out;
	gchar *err;
	gint status = -1;
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

RESULT
test_spawn_async ()
{
	/*
gboolean
g_spawn_async_with_pipes (const gchar *working_directory,
			gchar **argv,
			gchar **envp,
			GSpawnFlags flags,
			GSpawnChildSetupFunc child_setup,
			gpointer user_data,
			GPid *child_pid,
			gint *standard_input,
			gint *standard_output,
			gint *standard_error,
			GError **error) */
	char *argv [15];
	int stdout_fd = -1;
	char buffer [512];
	pid_t child_pid = 0;

	memset (argv, 0, 15 * sizeof (char *));
	argv [0] = "ls";
	if (!g_spawn_async_with_pipes (NULL, argv, NULL, G_SPAWN_SEARCH_PATH, NULL, NULL, &child_pid, NULL, &stdout_fd, NULL, NULL))
		return FAILED ("1 Failed to run ls");
	if (child_pid == 0)
		return FAILED ("2 child pid not returned");
	if (stdout_fd == -1)
		return FAILED ("3 out fd is -1");

	while (read (stdout_fd, buffer, 512) > 0);
	close (stdout_fd);

	return OK;
}

static Test spawn_tests [] = {
	{"g_shell_spawn_sync", test_spawn_sync},
	{"g_shell_spawn_async_with_pipes", test_spawn_async},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(spawn_tests_init, spawn_tests)


