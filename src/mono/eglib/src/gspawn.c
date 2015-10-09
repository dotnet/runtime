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
#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <fcntl.h>
#include <sys/types.h>

#include <glib.h>

#ifdef HAVE_UNISTD_H
#ifndef __USE_GNU
#define __USE_GNU
#endif
#include <unistd.h>
#endif

#ifdef HAVE_SYS_SELECT_H
#include <sys/select.h>
#endif

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#ifdef HAVE_SYS_WAIT_H
#include <sys/wait.h>
#endif

#ifdef HAVE_SYS_RESOURCE_H
#  include <sys/resource.h>
#endif

#ifdef G_OS_WIN32
#include <io.h>
#include <winsock2.h>
#define open _open
#define close _close
#define read _read
#define write _write
/* windows pipe api details: http://msdn2.microsoft.com/en-us/library/edze9h7e(VS.80).aspx */
#define pipe(x) _pipe(x, 256, 0)
#endif

#define set_error(msg, ...) do { if (error != NULL) *error = g_error_new (G_LOG_DOMAIN, 1, msg, __VA_ARGS__); } while (0)
#define set_error_cond(cond,msg, ...) do { if ((cond) && error != NULL) *error = g_error_new (G_LOG_DOMAIN, 1, msg, __VA_ARGS__); } while (0)
#define set_error_status(status,msg, ...) do { if (error != NULL) *error = g_error_new (G_LOG_DOMAIN, status, msg, __VA_ARGS__); } while (0)
#define NO_INTR(var,cmd) do { (var) = (cmd); } while ((var) == -1 && errno == EINTR)
#define CLOSE_PIPE(p) do { close (p [0]); close (p [1]); } while (0)

#if defined(__APPLE__)
#if defined (TARGET_OSX)
/* Apple defines this in crt_externs.h but doesn't provide that header for 
 * arm-apple-darwin9.  We'll manually define the symbol on Apple as it does
 * in fact exist on all implementations (so far) 
 */
gchar ***_NSGetEnviron(void);
#define environ (*_NSGetEnviron())
#else
static char *mono_environ[1] = { NULL };
#define environ mono_environ
#endif /* defined (TARGET_OSX) */
#elif defined(_MSC_VER)
/* MS defines this in stdlib.h */
#else
extern char **environ;
#endif

#ifndef G_OS_WIN32
static int
safe_read (int fd, gchar *buffer, gint count, GError **error)
{
	int res;

	NO_INTR (res, read (fd, buffer, count));
	set_error_cond (res == -1, "%s", "Error reading from pipe.");
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

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4389)
#endif

		FD_ZERO (&rfds);
		if (!out_closed && outfd >= 0)
			FD_SET (outfd, &rfds);
		if (!err_closed && errfd >= 0)
			FD_SET (errfd, &rfds);

#ifdef _MSC_VER
#pragma warning(pop)
#endif

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

static gboolean
create_pipe (int *fds, GError **error)
{
	if (pipe (fds) == -1) {
		set_error ("%s", "Error creating pipe.");
		return FALSE;
	}
	return TRUE;
}
#endif /* G_OS_WIN32 */

static int
write_all (int fd, const void *vbuf, size_t n)
{
	const char *buf = (const char *) vbuf;
	size_t nwritten = 0;
	int w;
	
	do {
		do {
			w = write (fd, buf + nwritten, n - nwritten);
		} while (w == -1 && errno == EINTR);
		
		if (w == -1)
			return -1;
		
		nwritten += w;
	} while (nwritten < n);
	
	return nwritten;
}

#ifndef G_OS_WIN32
int
eg_getdtablesize (void)
{
#ifdef HAVE_GETRLIMIT
	struct rlimit limit;
	int res;

	res = getrlimit (RLIMIT_NOFILE, &limit);
	g_assert (res == 0);
	return limit.rlim_cur;
#else
	return getdtablesize ();
#endif
}
#else
int
eg_getdtablesize (void)
{
	g_error ("Should not be called");
}
#endif

gboolean
g_spawn_command_line_sync (const gchar *command_line,
				gchar **standard_output,
				gchar **standard_error,
				gint *exit_status,
				GError **error)
{
#ifdef G_OS_WIN32
#elif !defined (HAVE_FORK) || !defined (HAVE_EXECV)
	fprintf (stderr, "g_spawn_command_line_sync not supported on this platform\n");
	return FALSE;
#else
	pid_t pid;
	gchar **argv;
	gint argc;
	int stdout_pipe [2] = { -1, -1 };
	int stderr_pipe [2] = { -1, -1 };
	int status;
	int res;
	
	if (!g_shell_parse_argv (command_line, &argc, &argv, error))
		return FALSE;

	if (standard_output && !create_pipe (stdout_pipe, error))
		return FALSE;

	if (standard_error && !create_pipe (stderr_pipe, error)) {
		if (standard_output) {
			CLOSE_PIPE (stdout_pipe);
		}
		return FALSE;
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
		for (i = eg_getdtablesize () - 1; i >= 3; i--)
			close (i);

		/* G_SPAWN_SEARCH_PATH is always enabled for g_spawn_command_line_sync */
		if (!g_path_is_absolute (argv [0])) {
			gchar *arg0;

			arg0 = g_find_program_in_path (argv [0]);
			if (arg0 == NULL) {
				exit (1);
			}
			//g_free (argv [0]);
			argv [0] = arg0;
		}
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

	NO_INTR (res, waitpid (pid, &status, 0));

	/* TODO: What if error? */
	if (WIFEXITED (status) && exit_status) {
		*exit_status = WEXITSTATUS (status);
	}
#endif
	return TRUE;
}

/*
 * This is the only use we have in mono/metadata
!g_spawn_async_with_pipes (NULL, (char**)addr_argv, NULL, G_SPAWN_SEARCH_PATH, NULL, NULL, &child_pid, &ch_in, &ch_out, NULL, NULL)
*/
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
			GError **error)
{
#ifdef G_OS_WIN32
#elif !defined (HAVE_FORK) || !defined (HAVE_EXECVE)
	fprintf (stderr, "g_spawn_async_with_pipes is not supported on this platform\n");
	return FALSE;
#else
	pid_t pid;
	int info_pipe [2];
	int in_pipe [2] = { -1, -1 };
	int out_pipe [2] = { -1, -1 };
	int err_pipe [2] = { -1, -1 };
	int status;

	g_return_val_if_fail (argv != NULL, FALSE); /* Only mandatory arg */

	if (!create_pipe (info_pipe, error))
		return FALSE;

	if (standard_output && !create_pipe (out_pipe, error)) {
		CLOSE_PIPE (info_pipe);
		return FALSE;
	}

	if (standard_error && !create_pipe (err_pipe, error)) {
		CLOSE_PIPE (info_pipe);
		CLOSE_PIPE (out_pipe);
		return FALSE;
	}

	if (standard_input && !create_pipe (in_pipe, error)) {
		CLOSE_PIPE (info_pipe);
		CLOSE_PIPE (out_pipe);
		CLOSE_PIPE (err_pipe);
		return FALSE;
	}

	pid = fork ();
	if (pid == -1) {
		CLOSE_PIPE (info_pipe);
		CLOSE_PIPE (out_pipe);
		CLOSE_PIPE (err_pipe);
		CLOSE_PIPE (in_pipe);
		set_error ("%s", "Error in fork ()");
		return FALSE;
	}

	if (pid == 0) {
		/* No zombie left behind */
		if ((flags & G_SPAWN_DO_NOT_REAP_CHILD) == 0) {
			pid = fork ();
		}

		if (pid != 0) {
			exit (pid == -1 ? 1 : 0);
		}  else {
			gint i;
			int fd;
			gchar *arg0;
			gchar **actual_args;
			gint unused;

			close (info_pipe [0]);
			close (in_pipe [1]);
			close (out_pipe [0]);
			close (err_pipe [0]);

			/* when exec* succeeds, we want to close this fd, which will return
			 * a 0 read on the parent. We're not supposed to keep it open forever.
			 * If exec fails, we still can write the error to it before closing.
			 */
			fcntl (info_pipe [1], F_SETFD, FD_CLOEXEC);

			if ((flags & G_SPAWN_DO_NOT_REAP_CHILD) == 0) {
				pid = getpid ();
				NO_INTR (unused, write_all (info_pipe [1], &pid, sizeof (pid_t)));
			}

			if (working_directory && chdir (working_directory) == -1) {
				int err = errno;
				NO_INTR (unused, write_all (info_pipe [1], &err, sizeof (int)));
				exit (0);
			}

			if (standard_output) {
				dup2 (out_pipe [1], STDOUT_FILENO);
			} else if ((flags & G_SPAWN_STDOUT_TO_DEV_NULL) != 0) {
				fd = open ("/dev/null", O_WRONLY);
				dup2 (fd, STDOUT_FILENO);
			}

			if (standard_error) {
				dup2 (err_pipe [1], STDERR_FILENO);
			} else if ((flags & G_SPAWN_STDERR_TO_DEV_NULL) != 0) {
				fd = open ("/dev/null", O_WRONLY);
				dup2 (fd, STDERR_FILENO);
			}

			if (standard_input) {
				dup2 (in_pipe [0], STDIN_FILENO);
			} else if ((flags & G_SPAWN_CHILD_INHERITS_STDIN) == 0) {
				fd = open ("/dev/null", O_RDONLY);
				dup2 (fd, STDIN_FILENO);
			}

			if ((flags & G_SPAWN_LEAVE_DESCRIPTORS_OPEN) != 0) {
				for (i = eg_getdtablesize () - 1; i >= 3; i--)
					close (i);
			}

			actual_args = ((flags & G_SPAWN_FILE_AND_ARGV_ZERO) == 0) ? argv : argv + 1;
			if (envp == NULL)
				envp = environ;

			if (child_setup)
				child_setup (user_data);

			arg0 = argv [0];
			if (!g_path_is_absolute (arg0) || (flags & G_SPAWN_SEARCH_PATH) != 0) {
				arg0 = g_find_program_in_path (argv [0]);
				if (arg0 == NULL) {
					int err = ENOENT;
					write_all (info_pipe [1], &err, sizeof (int));
					exit (0);
				}
			}

			execve (arg0, actual_args, envp);
			write_all (info_pipe [1], &errno, sizeof (int));
			exit (0);
		}
	} else if ((flags & G_SPAWN_DO_NOT_REAP_CHILD) == 0) {
		int w;
		/* Wait for the first child if two are created */
		NO_INTR (w, waitpid (pid, &status, 0));
		if (status == 1 || w == -1) {
			CLOSE_PIPE (info_pipe);
			CLOSE_PIPE (out_pipe);
			CLOSE_PIPE (err_pipe);
			CLOSE_PIPE (in_pipe);
			set_error ("Error in fork (): %d", status);
			return FALSE;
		}
	}
	close (info_pipe [1]);
	close (in_pipe [0]);
	close (out_pipe [1]);
	close (err_pipe [1]);

	if ((flags & G_SPAWN_DO_NOT_REAP_CHILD) == 0) {
		int x;
		NO_INTR (x, read (info_pipe [0], &pid, sizeof (pid_t))); /* if we read < sizeof (pid_t)... */
	}

	if (child_pid) {
		*child_pid = pid;
	}

	if (read (info_pipe [0], &status, sizeof (int)) != 0) {
		close (info_pipe [0]);
		close (in_pipe [0]);
		close (out_pipe [1]);
		close (err_pipe [1]);
		set_error_status (status, "Error in exec (%d -> %s)", status, strerror (status));
		return FALSE;
	}

	close (info_pipe [0]);
	if (standard_input)
		*standard_input = in_pipe [1];
	if (standard_output)
		*standard_output = out_pipe [0];
	if (standard_error)
		*standard_error = err_pipe [0];
#endif
	return TRUE;
}


