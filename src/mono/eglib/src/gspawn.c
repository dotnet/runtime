/*
 * Spawning processes.
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <unistd.h>
#include <unistd.h>
#include <sys/time.h>
#include <sys/wait.h>
#include <sys/types.h>
#include <glib.h>

static int
safe_read (int fd, gchar *buffer, gint count, GError **error)
{
	int res;

	do {
		res = read (fd, buffer, count);
	} while (res == -1 && errno == EINTR);

	if (res == -1 && error) {
		*error = g_error_new (G_LOG_DOMAIN, errno, "Error reading from pipe.");
	}
	return res;
}

static int
read_pipes (int outfd, gchar **out_str, int errfd, gchar **err_str, GError **error)
{
	fd_set rfds;
	int res;
	gboolean out_closed;
	gboolean err_closed;
	GString *out = NULL;
	GString *err = NULL;
	gchar *buffer = NULL;
	gint nread;

	out_closed = (outfd < 0);
	err_closed = (errfd < 0);
	if (out_str) {
		*out_str = NULL;
		out = g_string_new ("");
	}	

	if (err_str) {
		*err_str = NULL;
		err = g_string_new ("");
	}	

	do {
		if (out_closed && err_closed)
			break;

		FD_ZERO (&rfds);
		if (!out_closed && outfd >= 0)
			FD_SET (outfd, &rfds);
		if (!err_closed && errfd >= 0)
			FD_SET (errfd, &rfds);

		res = select (MAX (outfd, errfd) + 1, &rfds, NULL, NULL, NULL);
		if (res > 0) {
			if (buffer == NULL)
				buffer = g_malloc (1024);
			if (!out_closed && FD_ISSET (outfd, &rfds)) {
				nread = safe_read (outfd, buffer, 1024, error);
				if (nread < 0) {
					close (errfd);
					close (outfd);
					return -1;
				}
				g_string_append_len (out, buffer, nread);
				if (nread <= 0) {
					out_closed = TRUE;
					close (outfd);
				}
			}

			if (!err_closed && FD_ISSET (errfd, &rfds)) {
				nread = safe_read (errfd, buffer, 1024, error);
				if (nread < 0) {
					close (errfd);
					close (outfd);
					return -1;
				}
				g_string_append_len (err, buffer, nread);
				if (nread <= 0) {
					err_closed = TRUE;
					close (errfd);
				}
			}
		}
	} while (res > 0 || (res == -1 && errno == EINTR));

	g_free (buffer);
	if (out_str)
		*out_str = g_string_free (out, FALSE);

	if (err_str)
		*err_str = g_string_free (err, FALSE);

	return 0;
}

gboolean
g_spawn_command_line_sync (const gchar *command_line,
				gchar **standard_output,
				gchar **standard_error,
				gint *exit_status,
				GError **error)
{
	pid_t pid;
	gchar **argv;
	gint argc;
	int stdout_pipe [2];
	int stderr_pipe [2];
	int status;
	int res;
	
	if (!g_shell_parse_argv (command_line, &argc, &argv, error))
		return FALSE;

	stdout_pipe [0] = -1;
	stdout_pipe [1] = -1;
	stderr_pipe [0] = -1;
	stderr_pipe [1] = -1;
	if (standard_output) {
		if (pipe (stdout_pipe) == -1) {
			if (error)
				*error = g_error_new (G_LOG_DOMAIN, errno, "Error creating out pipe.");
			return FALSE;
		}
	}
	if (standard_error) {
		if (pipe (stderr_pipe) == -1) {
			if (standard_output) {
				close (stdout_pipe [1]);
				close (stdout_pipe [0]);
			}
			if (error)
				*error = g_error_new (G_LOG_DOMAIN, errno, "Error creating out pipe.");
			return FALSE;
		}
	}

	pid = fork ();
	if (pid == 0) {
		gint i;

		if (standard_output) {
			close (stdout_pipe [0]);
			dup2 (stdout_pipe [1], STDOUT_FILENO);
		}

		if (standard_error) {
			close (stderr_pipe [0]);
			dup2 (stderr_pipe [1], STDERR_FILENO);
		}
		for (i = getdtablesize () - 1; i >= 3; i--)
			close (i);

		execv (argv [0], argv);
		exit (1); /* TODO: What now? */
	}

	g_strfreev (argv);
	if (standard_output)
		close (stdout_pipe [1]);

	if (standard_error)
		close (stderr_pipe [1]);

	if (standard_output || standard_error) {
		res = read_pipes (stdout_pipe [0], standard_output, stderr_pipe [0], standard_error, error);
		if (res) {
			waitpid (pid, &status, WNOHANG); /* avoid zombie */
			return FALSE;
		}
	}

	do {
		res = waitpid (pid, &status, 0);
	} while (res == -1 && errno == EINTR);

	/* TODO: What if error? */
	if (WIFEXITED (status) && exit_status) {
		*exit_status = WEXITSTATUS (status);
	}

	return TRUE;
}

