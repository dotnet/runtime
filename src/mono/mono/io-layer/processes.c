/*
 * processes.c:  Process handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2011 Novell, Inc.
 * Copyright 2011 Xamarin Inc
 */

#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif
#include <sys/time.h>
#include <fcntl.h>
#ifdef HAVE_SYS_PARAM_H
#include <sys/param.h>
#endif
#include <ctype.h>

#ifdef HAVE_SYS_WAIT_H
#include <sys/wait.h>
#endif
#ifdef HAVE_SYS_RESOURCE_H
#include <sys/resource.h>
#endif

#ifdef HAVE_SYS_MKDEV_H
#include <sys/mkdev.h>
#endif

#ifdef HAVE_UTIME_H
#include <utime.h>
#endif

/* sys/resource.h (for rusage) is required when using osx 10.3 (but not 10.4) */
#ifdef __APPLE__
#include <TargetConditionals.h>
#include <sys/resource.h>
#ifdef HAVE_LIBPROC_H
/* proc_name */
#include <libproc.h>
#endif
#endif

#if defined(PLATFORM_MACOSX)
#define USE_OSX_LOADER
#endif

#if ( defined(__OpenBSD__) || defined(__FreeBSD__) ) && defined(HAVE_LINK_H)
#define USE_BSD_LOADER
#endif

#if defined(__HAIKU__)
#define USE_HAIKU_LOADER
#endif

#if defined(USE_OSX_LOADER) || defined(USE_BSD_LOADER)
#include <sys/proc.h>
#include <sys/sysctl.h>
#  if !defined(__OpenBSD__)
#    include <sys/utsname.h>
#  endif
#  if defined(__FreeBSD__)
#    include <sys/user.h>  /* struct kinfo_proc */
#  endif
#endif

#ifdef PLATFORM_SOLARIS
/* procfs.h cannot be included if this define is set, but it seems to work fine if it is undefined */
#if _FILE_OFFSET_BITS == 64
#undef _FILE_OFFSET_BITS
#include <procfs.h>
#define _FILE_OFFSET_BITS 64
#else
#include <procfs.h>
#endif
#endif

#ifdef __HAIKU__
#include <KernelKit.h>
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/process-private.h>
#include <mono/io-layer/threads.h>
#include <mono/io-layer/io-trace.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-path.h>
#include <mono/io-layer/timefuncs-private.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-once.h>
#include <mono/utils/mono-logger-internals.h>

/* The process' environment strings */
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
#else
extern char **environ;
#endif

static guint32 process_wait (gpointer handle, guint32 timeout, gboolean alertable);
static void process_close (gpointer handle, gpointer data);
static gboolean is_pid_valid (pid_t pid);

#if !(defined(USE_OSX_LOADER) || defined(USE_BSD_LOADER) || defined(USE_HAIKU_LOADER))
static FILE *
open_process_map (int pid, const char *mode);
#endif

struct _WapiHandleOps _wapi_process_ops = {
	process_close,		/* close_shared */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
	process_wait,			/* special_wait */
	NULL				/* prewait */	
};

#if HAVE_SIGACTION
static struct sigaction previous_chld_sa;
#endif
static mono_once_t process_sig_chld_once = MONO_ONCE_INIT;
static void process_add_sigchld_handler (void);

/* The signal-safe logic to use mono_processes goes like this:
 * - The list must be safe to traverse for the signal handler at all times.
 *   It's safe to: prepend an entry (which is a single store to 'mono_processes'),
 *   unlink an entry (assuming the unlinked entry isn't freed and doesn't 
 *   change its 'next' pointer so that it can still be traversed).
 * When cleaning up we first unlink an entry, then we verify that
 * the read lock isn't locked. Then we can free the entry, since
 * we know that nobody is using the old version of the list (including
 * the unlinked entry).
 * We also need to lock when adding and cleaning up so that those two
 * operations don't mess with eachother. (This lock is not used in the
 * signal handler)
 */
static struct MonoProcess *mono_processes = NULL;
static volatile gint32 mono_processes_cleaning_up = 0;
static mono_mutex_t mono_processes_mutex;
static void mono_processes_cleanup (void);

static gpointer current_process;
static char *cli_launcher;

static WapiHandle_process *
lookup_process_handle (gpointer handle)
{
	WapiHandle_process *process_data;
	gboolean ret;

	ret = _wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
							   (gpointer *)&process_data);
	if (!ret)
		return NULL;
	return process_data;
}

/* Check if a pid is valid - i.e. if a process exists with this pid. */
static gboolean
is_pid_valid (pid_t pid)
{
	gboolean result = FALSE;

#if defined(HOST_WATCHOS)
	result = TRUE; // TODO: Rewrite using sysctl
#elif defined(PLATFORM_MACOSX) || defined(__OpenBSD__) || defined(__FreeBSD__)
	if (((kill(pid, 0) == 0) || (errno == EPERM)) && pid != 0)
		result = TRUE;
#elif defined(__HAIKU__)
	team_info teamInfo;
	if (get_team_info ((team_id)pid, &teamInfo) == B_OK)
		result = TRUE;
#else
	char *dir = g_strdup_printf ("/proc/%d", pid);
	if (!access (dir, F_OK))
		result = TRUE;
	g_free (dir);
#endif
	
	return result;
}

static void
process_set_defaults (WapiHandle_process *process_handle)
{
	/* These seem to be the defaults on w2k */
	process_handle->min_working_set = 204800;
	process_handle->max_working_set = 1413120;
	
	_wapi_time_t_to_filetime (time (NULL), &process_handle->create_time);
}

static int
len16 (const gunichar2 *str)
{
	int len = 0;
	
	while (*str++ != 0)
		len++;

	return len;
}

static gunichar2 *
utf16_concat (const gunichar2 *first, ...)
{
	va_list args;
	int total = 0, i;
	const gunichar2 *s;
	gunichar2 *ret;

	va_start (args, first);
	total += len16 (first);
        for (s = va_arg (args, gunichar2 *); s != NULL; s = va_arg(args, gunichar2 *)){
		total += len16 (s);
        }
	va_end (args);

	ret = g_new (gunichar2, total + 1);
	if (ret == NULL)
		return NULL;

	ret [total] = 0;
	i = 0;
	for (s = first; *s != 0; s++)
		ret [i++] = *s;
	va_start (args, first);
	for (s = va_arg (args, gunichar2 *); s != NULL; s = va_arg (args, gunichar2 *)){
		const gunichar2 *p;
		
		for (p = s; *p != 0; p++)
			ret [i++] = *p;
	}
	va_end (args);
	
	return ret;
}

static const gunichar2 utf16_space_bytes [2] = { 0x20, 0 };
static const gunichar2 *utf16_space = utf16_space_bytes; 
static const gunichar2 utf16_quote_bytes [2] = { 0x22, 0 };
static const gunichar2 *utf16_quote = utf16_quote_bytes;

#ifdef DEBUG_ENABLED
/* Useful in gdb */
void
print_utf16 (gunichar2 *str)
{
	char *res;

	res = g_utf16_to_utf8 (str, -1, NULL, NULL, NULL);
	g_print ("%s\n", res);
	g_free (res);
}
#endif

/* Implemented as just a wrapper around CreateProcess () */
gboolean
ShellExecuteEx (WapiShellExecuteInfo *sei)
{
	gboolean ret;
	WapiProcessInformation process_info;
	gunichar2 *args;
	
	if (sei == NULL) {
		/* w2k just segfaults here, but we can do better than
		 * that
		 */
		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	if (sei->lpFile == NULL)
		/* w2k returns TRUE for this, for some reason. */
		return TRUE;
	
	/* Put both executable and parameters into the second argument
	 * to CreateProcess (), so it searches $PATH.  The conversion
	 * into and back out of utf8 is because there is no
	 * g_strdup_printf () equivalent for gunichar2 :-(
	 */
	args = utf16_concat (utf16_quote, sei->lpFile, utf16_quote, sei->lpParameters == NULL ? NULL : utf16_space, sei->lpParameters, NULL);
	if (args == NULL) {
		SetLastError (ERROR_INVALID_DATA);
		return FALSE;
	}
	ret = CreateProcess (NULL, args, NULL, NULL, TRUE,
			     CREATE_UNICODE_ENVIRONMENT, NULL,
			     sei->lpDirectory, NULL, &process_info);
	g_free (args);

	if (!ret && GetLastError () == ERROR_OUTOFMEMORY)
		return ret;
	
	if (!ret) {
		static char *handler;
		static gunichar2 *handler_utf16;
		
		if (handler_utf16 == (gunichar2 *)-1)
			return FALSE;

#ifdef PLATFORM_MACOSX
		handler = g_strdup ("/usr/bin/open");
#else
		/*
		 * On Linux, try: xdg-open, the FreeDesktop standard way of doing it,
		 * if that fails, try to use gnome-open, then kfmclient
		 */
		handler = g_find_program_in_path ("xdg-open");
		if (handler == NULL){
			handler = g_find_program_in_path ("gnome-open");
			if (handler == NULL){
				handler = g_find_program_in_path ("kfmclient");
				if (handler == NULL){
					handler_utf16 = (gunichar2 *) -1;
					return FALSE;
				} else {
					/* kfmclient needs exec argument */
					char *old = handler;
					handler = g_strconcat (old, " exec",
							       NULL);
					g_free (old);
				}
			}
		}
#endif
		handler_utf16 = g_utf8_to_utf16 (handler, -1, NULL, NULL, NULL);
		g_free (handler);

		/* Put quotes around the filename, in case it's a url
		 * that contains #'s (CreateProcess() calls
		 * g_shell_parse_argv(), which deliberately throws
		 * away anything after an unquoted #).  Fixes bug
		 * 371567.
		 */
		args = utf16_concat (handler_utf16, utf16_space, utf16_quote,
				     sei->lpFile, utf16_quote,
				     sei->lpParameters == NULL ? NULL : utf16_space,
				     sei->lpParameters, NULL);
		if (args == NULL) {
			SetLastError (ERROR_INVALID_DATA);
			return FALSE;
		}
		ret = CreateProcess (NULL, args, NULL, NULL, TRUE,
				     CREATE_UNICODE_ENVIRONMENT, NULL,
				     sei->lpDirectory, NULL, &process_info);
		g_free (args);
		if (!ret) {
			if (GetLastError () != ERROR_OUTOFMEMORY)
				SetLastError (ERROR_INVALID_DATA);
			return FALSE;
		}
		/* Shell exec should not return a process handle when it spawned a GUI thing, like a browser. */
		CloseHandle (process_info.hProcess);
		process_info.hProcess = NULL;
	}
	
	if (sei->fMask & SEE_MASK_NOCLOSEPROCESS)
		sei->hProcess = process_info.hProcess;
	else
		CloseHandle (process_info.hProcess);
	
	return ret;
}

static gboolean
is_managed_binary (const char *filename)
{
	int original_errno = errno;
#if defined(HAVE_LARGE_FILE_SUPPORT) && defined(O_LARGEFILE)
	int file = open (filename, O_RDONLY | O_LARGEFILE);
#else
	int file = open (filename, O_RDONLY);
#endif
	off_t new_offset;
	unsigned char buffer[8];
	off_t file_size, optional_header_offset;
	off_t pe_header_offset;
	gboolean managed = FALSE;
	int num_read;
	guint32 first_word, second_word;
	
	/* If we are unable to open the file, then we definitely
	 * can't say that it is managed. The child mono process
	 * probably wouldn't be able to open it anyway.
	 */
	if (file < 0) {
		errno = original_errno;
		return FALSE;
	}

	/* Retrieve the length of the file for future sanity checks. */
	file_size = lseek (file, 0, SEEK_END);
	lseek (file, 0, SEEK_SET);

	/* We know we need to read a header field at offset 60. */
	if (file_size < 64)
		goto leave;

	num_read = read (file, buffer, 2);

	if ((num_read != 2) || (buffer[0] != 'M') || (buffer[1] != 'Z'))
		goto leave;

	new_offset = lseek (file, 60, SEEK_SET);

	if (new_offset != 60)
		goto leave;
	
	num_read = read (file, buffer, 4);

	if (num_read != 4)
		goto leave;
	pe_header_offset =  buffer[0]
		| (buffer[1] <<  8)
		| (buffer[2] << 16)
		| (buffer[3] << 24);
	
	if (pe_header_offset + 24 > file_size)
		goto leave;

	new_offset = lseek (file, pe_header_offset, SEEK_SET);

	if (new_offset != pe_header_offset)
		goto leave;

	num_read = read (file, buffer, 4);

	if ((num_read != 4) || (buffer[0] != 'P') || (buffer[1] != 'E') || (buffer[2] != 0) || (buffer[3] != 0))
		goto leave;

	/*
	 * Verify that the header we want in the optional header data
	 * is present in this binary.
	 */
	new_offset = lseek (file, pe_header_offset + 20, SEEK_SET);

	if (new_offset != pe_header_offset + 20)
		goto leave;

	num_read = read (file, buffer, 2);

	if ((num_read != 2) || ((buffer[0] | (buffer[1] << 8)) < 216))
		goto leave;

	/* Read the CLR header address and size fields. These will be
	 * zero if the binary is not managed.
	 */
	optional_header_offset = pe_header_offset + 24;
	new_offset = lseek (file, optional_header_offset + 208, SEEK_SET);

	if (new_offset != optional_header_offset + 208)
		goto leave;

	num_read = read (file, buffer, 8);
	
	/* We are not concerned with endianness, only with
	 * whether it is zero or not.
	 */
	first_word = *(guint32 *)&buffer[0];
	second_word = *(guint32 *)&buffer[4];
	
	if ((num_read != 8) || (first_word == 0) || (second_word == 0))
		goto leave;
	
	managed = TRUE;

leave:
	close (file);
	errno = original_errno;
	return managed;
}

gboolean
CreateProcessWithLogonW (const gunichar2 *username,
						 const gunichar2 *domain,
						 const gunichar2 *password,
						 const guint32 logonFlags,
						 const gunichar2 *appname,
						 const gunichar2 *cmdline,
						 guint32 create_flags,
						 gpointer env,
						 const gunichar2 *cwd,
						 WapiStartupInfo *startup,
						 WapiProcessInformation *process_info)
{
	/* FIXME: use user information */
	return CreateProcess (appname, cmdline, NULL, NULL, FALSE, create_flags, env, cwd, startup, process_info);
}

static gboolean
is_readable_or_executable (const char *prog)
{
	struct stat buf;
	int a = access (prog, R_OK);
	int b = access (prog, X_OK);
	if (a != 0 && b != 0)
		return FALSE;
	if (stat (prog, &buf))
		return FALSE;
	if (S_ISREG (buf.st_mode))
		return TRUE;
	return FALSE;
}

static gboolean
is_executable (const char *prog)
{
	struct stat buf;
	if (access (prog, X_OK) != 0)
		return FALSE;
	if (stat (prog, &buf))
		return FALSE;
	if (S_ISREG (buf.st_mode))
		return TRUE;
	return FALSE;
}

static void
switch_dir_separators (char *path)
{
	size_t i, pathLength = strlen(path);
	
	/* Turn all the slashes round the right way, except for \' */
	/* There are probably other characters that need to be excluded as well. */
	for (i = 0; i < pathLength; i++) {
		if (path[i] == '\\' && i < pathLength - 1 && path[i+1] != '\'' )
			path[i] = '/';
	}
}

gboolean CreateProcess (const gunichar2 *appname, const gunichar2 *cmdline,
			WapiSecurityAttributes *process_attrs G_GNUC_UNUSED,
			WapiSecurityAttributes *thread_attrs G_GNUC_UNUSED,
			gboolean inherit_handles, guint32 create_flags,
			gpointer new_environ, const gunichar2 *cwd,
			WapiStartupInfo *startup,
			WapiProcessInformation *process_info)
{
#if defined (HAVE_FORK) && defined (HAVE_EXECVE)
	char *cmd = NULL, *prog = NULL, *full_prog = NULL, *args = NULL, *args_after_prog = NULL;
	char *dir = NULL, **env_strings = NULL, **argv = NULL;
	guint32 i, env_count = 0;
	gboolean ret = FALSE;
	gpointer handle = NULL;
	WapiHandle_process process_handle = {0}, *process_handle_data;
	GError *gerr = NULL;
	int in_fd, out_fd, err_fd;
	pid_t pid = 0;
	int thr_ret;
	int startup_pipe [2] = {-1, -1};
	int dummy;
	struct MonoProcess *mono_process;
	gboolean fork_failed = FALSE;

	mono_once (&process_sig_chld_once, process_add_sigchld_handler);

	/* appname and cmdline specify the executable and its args:
	 *
	 * If appname is not NULL, it is the name of the executable.
	 * Otherwise the executable is the first token in cmdline.
	 *
	 * Executable searching:
	 *
	 * If appname is not NULL, it can specify the full path and
	 * file name, or else a partial name and the current directory
	 * will be used.  There is no additional searching.
	 *
	 * If appname is NULL, the first whitespace-delimited token in
	 * cmdline is used.  If the name does not contain a full
	 * directory path, the search sequence is:
	 *
	 * 1) The directory containing the current process
	 * 2) The current working directory
	 * 3) The windows system directory  (Ignored)
	 * 4) The windows directory (Ignored)
	 * 5) $PATH
	 *
	 * Just to make things more interesting, tokens can contain
	 * white space if they are surrounded by quotation marks.  I'm
	 * beginning to understand just why windows apps are generally
	 * so crap, with an API like this :-(
	 */
	if (appname != NULL) {
		cmd = mono_unicode_to_external (appname);
		if (cmd == NULL) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: unicode conversion returned NULL",
				   __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}

		switch_dir_separators(cmd);
	}
	
	if (cmdline != NULL) {
		args = mono_unicode_to_external (cmdline);
		if (args == NULL) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: unicode conversion returned NULL", __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}
	}

	if (cwd != NULL) {
		dir = mono_unicode_to_external (cwd);
		if (dir == NULL) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: unicode conversion returned NULL", __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}

		/* Turn all the slashes round the right way */
		switch_dir_separators(dir);
	}
	

	/* We can't put off locating the executable any longer :-( */
	if (cmd != NULL) {
		char *unquoted;
		if (g_ascii_isalpha (cmd[0]) && (cmd[1] == ':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			g_memmove (cmd, cmd+2, strlen (cmd)-2);
			cmd[strlen (cmd)-2] = '\0';
		}

		unquoted = g_shell_unquote (cmd, NULL);
		if (unquoted[0] == '/') {
			/* Assume full path given */
			prog = g_strdup (unquoted);

			/* Executable existing ? */
			if (!is_readable_or_executable (prog)) {
				MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Couldn't find executable %s",
					   __func__, prog);
				g_free (unquoted);
				SetLastError (ERROR_FILE_NOT_FOUND);
				goto free_strings;
			}
		} else {
			/* Search for file named by cmd in the current
			 * directory
			 */
			char *curdir = g_get_current_dir ();

			prog = g_strdup_printf ("%s/%s", curdir, unquoted);
			g_free (curdir);

			/* And make sure it's readable */
			if (!is_readable_or_executable (prog)) {
				MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Couldn't find executable %s",
					   __func__, prog);
				g_free (unquoted);
				SetLastError (ERROR_FILE_NOT_FOUND);
				goto free_strings;
			}
		}
		g_free (unquoted);

		args_after_prog = args;
	} else {
		char *token = NULL;
		char quote;
		
		/* Dig out the first token from args, taking quotation
		 * marks into account
		 */

		/* First, strip off all leading whitespace */
		args = g_strchug (args);
		
		/* args_after_prog points to the contents of args
		 * after token has been set (otherwise argv[0] is
		 * duplicated)
		 */
		args_after_prog = args;

		/* Assume the opening quote will always be the first
		 * character
		 */
		if (args[0] == '\"' || args [0] == '\'') {
			quote = args [0];
			for (i = 1; args[i] != '\0' && args[i] != quote; i++);
			if (args [i + 1] == '\0' || g_ascii_isspace (args[i+1])) {
				/* We found the first token */
				token = g_strndup (args+1, i-1);
				args_after_prog = g_strchug (args + i + 1);
			} else {
				/* Quotation mark appeared in the
				 * middle of the token.  Just give the
				 * whole first token, quotes and all,
				 * to exec.
				 */
			}
		}
		
		if (token == NULL) {
			/* No quote mark, or malformed */
			for (i = 0; args[i] != '\0'; i++) {
				if (g_ascii_isspace (args[i])) {
					token = g_strndup (args, i);
					args_after_prog = args + i + 1;
					break;
				}
			}
		}

		if (token == NULL && args[0] != '\0') {
			/* Must be just one token in the string */
			token = g_strdup (args);
			args_after_prog = NULL;
		}
		
		if (token == NULL) {
			/* Give up */
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Couldn't find what to exec", __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}
		
		/* Turn all the slashes round the right way. Only for
		 * the prg. name
		 */
		switch_dir_separators(token);

		if (g_ascii_isalpha (token[0]) && (token[1] == ':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			g_memmove (token, token+2, strlen (token)-2);
			token[strlen (token)-2] = '\0';
		}

		if (token[0] == '/') {
			/* Assume full path given */
			prog = g_strdup (token);
			
			/* Executable existing ? */
			if (!is_readable_or_executable (prog)) {
				MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Couldn't find executable %s",
					   __func__, token);
				g_free (token);
				SetLastError (ERROR_FILE_NOT_FOUND);
				goto free_strings;
			}
		} else {
			char *curdir = g_get_current_dir ();

			/* FIXME: Need to record the directory
			 * containing the current process, and check
			 * that for the new executable as the first
			 * place to look
			 */

			prog = g_strdup_printf ("%s/%s", curdir, token);
			g_free (curdir);

			/* I assume X_OK is the criterion to use,
			 * rather than F_OK
			 *
			 * X_OK is too strict *if* the target is a CLR binary
			 */
			if (!is_readable_or_executable (prog)) {
				g_free (prog);
				prog = g_find_program_in_path (token);
				if (prog == NULL) {
					MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Couldn't find executable %s", __func__, token);

					g_free (token);
					SetLastError (ERROR_FILE_NOT_FOUND);
					goto free_strings;
				}
			}
		}

		g_free (token);
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Exec prog [%s] args [%s]", __func__, prog,
		   args_after_prog);
	
	/* Check for CLR binaries; if found, we will try to invoke
	 * them using the same mono binary that started us.
	 */
	if (is_managed_binary (prog)) {
		gunichar2 *newapp = NULL, *newcmd;
		gsize bytes_ignored;

		if (cli_launcher)
			newapp = mono_unicode_from_external (cli_launcher, &bytes_ignored);
		else
			newapp = mono_unicode_from_external ("mono", &bytes_ignored);

		if (newapp != NULL) {
			if (appname != NULL) {
				newcmd = utf16_concat (utf16_quote, newapp, utf16_quote, utf16_space,
						       appname, utf16_space,
						       cmdline, NULL);
			} else {
				newcmd = utf16_concat (utf16_quote, newapp, utf16_quote, utf16_space,
						       cmdline, NULL);
			}
			
			g_free ((gunichar2 *)newapp);
			
			if (newcmd != NULL) {
				ret = CreateProcess (NULL, newcmd,
						     process_attrs,
						     thread_attrs,
						     inherit_handles,
						     create_flags, new_environ,
						     cwd, startup,
						     process_info);
				
				g_free ((gunichar2 *)newcmd);
				
				goto free_strings;
			}
		}
	} else {
		if (!is_executable (prog)) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Executable permisson not set on %s", __func__, prog);
			SetLastError (ERROR_ACCESS_DENIED);
			goto free_strings;
		}
	}

	if (args_after_prog != NULL && *args_after_prog) {
		char *qprog;

		qprog = g_shell_quote (prog);
		full_prog = g_strconcat (qprog, " ", args_after_prog, NULL);
		g_free (qprog);
	} else {
		full_prog = g_shell_quote (prog);
	}

	ret = g_shell_parse_argv (full_prog, NULL, &argv, &gerr);
	if (ret == FALSE) {
		g_message ("CreateProcess: %s\n", gerr->message);
		g_error_free (gerr);
		gerr = NULL;
		goto free_strings;
	}

	if (startup != NULL && startup->dwFlags & STARTF_USESTDHANDLES) {
		in_fd = GPOINTER_TO_UINT (startup->hStdInput);
		out_fd = GPOINTER_TO_UINT (startup->hStdOutput);
		err_fd = GPOINTER_TO_UINT (startup->hStdError);
	} else {
		in_fd = GPOINTER_TO_UINT (GetStdHandle (STD_INPUT_HANDLE));
		out_fd = GPOINTER_TO_UINT (GetStdHandle (STD_OUTPUT_HANDLE));
		err_fd = GPOINTER_TO_UINT (GetStdHandle (STD_ERROR_HANDLE));
	}
	
	process_handle.proc_name = g_strdup (prog);

	process_set_defaults (&process_handle);
	
	handle = _wapi_handle_new (WAPI_HANDLE_PROCESS, &process_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating process handle", __func__);

		ret = FALSE;
		SetLastError (ERROR_OUTOFMEMORY);
		goto free_strings;
	}

	/* new_environ is a block of NULL-terminated strings, which
	 * is itself NULL-terminated. Of course, passing an array of
	 * string pointers would have made things too easy :-(
	 *
	 * If new_environ is not NULL it specifies the entire set of
	 * environment variables in the new process.  Otherwise the
	 * new process inherits the same environment.
	 */
	if (new_environ) {
		gunichar2 *new_environp;

		/* Count the number of strings */
		for (new_environp = (gunichar2 *)new_environ; *new_environp;
		     new_environp++) {
			env_count++;
			while (*new_environp) {
				new_environp++;
			}
		}

		/* +2: one for the process handle value, and the last
		 * one is NULL
		 */
		env_strings = g_new0 (char *, env_count + 2);
		
		/* Copy each environ string into 'strings' turning it
		 * into utf8 (or the requested encoding) at the same
		 * time
		 */
		env_count = 0;
		for (new_environp = (gunichar2 *)new_environ; *new_environp;
		     new_environp++) {
			env_strings[env_count] = mono_unicode_to_external (new_environp);
			env_count++;
			while (*new_environp) {
				new_environp++;
			}
		}
	} else {
		for (i = 0; environ[i] != NULL; i++)
			env_count++;

		/* +2: one for the process handle value, and the last
		 * one is NULL
		 */
		env_strings = g_new0 (char *, env_count + 2);
		
		/* Copy each environ string into 'strings' turning it
		 * into utf8 (or the requested encoding) at the same
		 * time
		 */
		env_count = 0;
		for (i = 0; environ[i] != NULL; i++) {
			env_strings[env_count] = g_strdup (environ[i]);
			env_count++;
		}
	}

	/* Create a pipe to make sure the child doesn't exit before 
	 * we can add the process to the linked list of mono_processes */
	if (pipe (startup_pipe) == -1) {
		/* Could not create the pipe to synchroniz process startup. We'll just not synchronize.
		 * This is just for a very hard to hit race condition in the first place */
		startup_pipe [0] = startup_pipe [1] = -1;
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: new process startup not synchronized. We may not notice if the newly created process exits immediately.", __func__);
	}

	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	pid = fork ();
	if (pid == -1) {
		/* Error */
		SetLastError (ERROR_OUTOFMEMORY);
		ret = FALSE;
		fork_failed = TRUE;
		goto cleanup;
	} else if (pid == 0) {
		/* Child */
		
		if (startup_pipe [0] != -1) {
			/* Wait until the parent has updated it's internal data */
			ssize_t _i G_GNUC_UNUSED = read (startup_pipe [0], &dummy, 1);
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: child: parent has completed its setup", __func__);
			close (startup_pipe [0]);
			close (startup_pipe [1]);
		}
		
		/* should we detach from the process group? */

		/* Connect stdin, stdout and stderr */
		dup2 (in_fd, 0);
		dup2 (out_fd, 1);
		dup2 (err_fd, 2);

		if (inherit_handles != TRUE) {
			/* FIXME: do something here */
		}
		
		/* Close all file descriptors */
		for (i = wapi_getdtablesize () - 1; i > 2; i--)
			close (i);

#ifdef DEBUG_ENABLED
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: exec()ing [%s] in dir [%s]", __func__, cmd,
			   dir == NULL?".":dir);
		for (i = 0; argv[i] != NULL; i++)
			g_message ("arg %d: [%s]", i, argv[i]);
		
		for (i = 0; env_strings[i] != NULL; i++)
			g_message ("env %d: [%s]", i, env_strings[i]);
#endif

		/* set cwd */
		if (dir != NULL && chdir (dir) == -1) {
			/* set error */
			_exit (-1);
		}
		
		/* exec */
		execve (argv[0], argv, env_strings);
		
		/* set error */
		_exit (-1);
	}
	/* parent */
	
	process_handle_data = lookup_process_handle (handle);
	if (!process_handle_data) {
		g_warning ("%s: error looking up process handle %p", __func__,
			   handle);
		_wapi_handle_unref (handle);
		goto cleanup;
	}
	
	process_handle_data->id = pid;

	/* Add our mono_process into the linked list of mono_processes */
	mono_process = (struct MonoProcess *) g_malloc0 (sizeof (struct MonoProcess));
	mono_process->pid = pid;
	mono_process->handle_count = 1;
	if (mono_os_sem_init (&mono_process->exit_sem, 0) != 0) {
		/* If we can't create the exit semaphore, we just don't add anything
		 * to our list of mono processes. Waiting on the process will return 
		 * immediately. */
		g_warning ("%s: could not create exit semaphore for process.", strerror (errno));
		g_free (mono_process);
	} else {
		/* Keep the process handle artificially alive until the process
		 * exits so that the information in the handle isn't lost. */
		_wapi_handle_ref (handle);
		mono_process->handle = handle;

		process_handle_data->mono_process = mono_process;

		mono_os_mutex_lock (&mono_processes_mutex);
		mono_process->next = mono_processes;
		mono_processes = mono_process;
		mono_os_mutex_unlock (&mono_processes_mutex);
	}
	
	if (process_info != NULL) {
		process_info->hProcess = handle;
		process_info->dwProcessId = pid;

		/* FIXME: we might need to handle the thread info some
		 * day
		 */
		process_info->hThread = INVALID_HANDLE_VALUE;
		process_info->dwThreadId = 0;
	}

cleanup:
	_wapi_handle_unlock_shared_handles ();

	if (fork_failed)
		_wapi_handle_unref (handle);

	if (startup_pipe [1] != -1) {
		/* Write 1 byte, doesn't matter what */
		ssize_t _i G_GNUC_UNUSED = write (startup_pipe [1], startup_pipe, 1);
		close (startup_pipe [0]);
		close (startup_pipe [1]);
	}

free_strings:
	if (cmd)
		g_free (cmd);
	if (full_prog)
		g_free (full_prog);
	if (prog)
		g_free (prog);
	if (args)
		g_free (args);
	if (dir)
		g_free (dir);
	if (env_strings)
		g_strfreev (env_strings);
	if (argv)
		g_strfreev (argv);
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: returning handle %p for pid %d", __func__, handle, pid);

	/* Check if something needs to be cleaned up. */
	mono_processes_cleanup ();
	
	return ret;
#else
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
#endif // defined (HAVE_FORK) && defined (HAVE_EXECVE)
}
		
static void
process_set_name (WapiHandle_process *process_handle)
{
	char *progname, *utf8_progname, *slash;
	
	progname = g_get_prgname ();
	utf8_progname = mono_utf8_from_external (progname);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: using [%s] as prog name", __func__, progname);

	if (utf8_progname) {
		slash = strrchr (utf8_progname, '/');
		if (slash)
			process_handle->proc_name = g_strdup (slash+1);
		else
			process_handle->proc_name = g_strdup (utf8_progname);
		g_free (utf8_progname);
	}
}

void
wapi_processes_init (void)
{
	pid_t pid = _wapi_getpid ();
	WapiHandle_process process_handle = {0};

	_wapi_handle_register_capabilities (WAPI_HANDLE_PROCESS,
		(WapiHandleCapability)(WAPI_HANDLE_CAP_WAIT | WAPI_HANDLE_CAP_SPECIAL_WAIT));
	
	process_handle.id = pid;

	process_set_defaults (&process_handle);
	process_set_name (&process_handle);

	current_process = _wapi_handle_new (WAPI_HANDLE_PROCESS,
					    &process_handle);
	g_assert (current_process);

	mono_os_mutex_init (&mono_processes_mutex);
}

gpointer
_wapi_process_duplicate (void)
{
	_wapi_handle_ref (current_process);
	
	return current_process;
}

/* Returns a pseudo handle that doesn't need to be closed afterwards */
gpointer
GetCurrentProcess (void)
{
	return _WAPI_PROCESS_CURRENT;
}

guint32
GetProcessId (gpointer handle)
{
	WapiHandle_process *process_handle;

	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (handle))
		/* This is a pseudo handle */
		return WAPI_HANDLE_TO_PID (handle);
	
	process_handle = lookup_process_handle (handle);
	if (!process_handle) {
		SetLastError (ERROR_INVALID_HANDLE);
		return 0;
	}
	
	return process_handle->id;
}

static gboolean
process_open_compare (gpointer handle, gpointer user_data)
{
	pid_t wanted_pid;
	WapiHandle_process *process_handle;
	pid_t checking_pid;

	g_assert (!WAPI_IS_PSEUDO_PROCESS_HANDLE (handle));
	
	process_handle = lookup_process_handle (handle);
	g_assert (process_handle);
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: looking at process %d", __func__, process_handle->id);

	checking_pid = process_handle->id;

	if (checking_pid == 0)
		return FALSE;
	
	wanted_pid = GPOINTER_TO_UINT (user_data);

	/* It's possible to have more than one process handle with the
	 * same pid, but only the one running process can be
	 * unsignalled
	 */
	if (checking_pid == wanted_pid &&
	    !_wapi_handle_issignalled (handle)) {
		/* If the handle is blown away in the window between
		 * returning TRUE here and _wapi_search_handle pinging
		 * the timestamp, the search will continue
		 */
		return TRUE;
	} else {
		return FALSE;
	}
}

gboolean
CloseProcess (gpointer handle)
{
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (handle))
		return TRUE;
	return CloseHandle (handle);
}

/*
 * The caller owns the returned handle and must call CloseProcess () on it to clean it up.
 */
gpointer
OpenProcess (guint32 req_access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, guint32 pid)
{
	/* Find the process handle that corresponds to pid */
	gpointer handle = NULL;
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: looking for process %d", __func__, pid);

	handle = _wapi_search_handle (WAPI_HANDLE_PROCESS,
				      process_open_compare,
				      GUINT_TO_POINTER (pid), NULL, TRUE);
	if (handle == 0) {
		if (is_pid_valid (pid)) {
			/* Return a pseudo handle for processes we
			 * don't have handles for
			 */
			return WAPI_PID_TO_HANDLE (pid);
		} else {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find pid %d", __func__, pid);

			SetLastError (ERROR_PROC_NOT_FOUND);
	
			return NULL;
		}
	}

	/* _wapi_search_handle () already added a ref */
	return handle;
}

gboolean
GetExitCodeProcess (gpointer process, guint32 *code)
{
	WapiHandle_process *process_handle;
	guint32 pid = -1;
	
	if (!code)
		return FALSE;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		pid = WAPI_HANDLE_TO_PID (process);
		/* This is a pseudo handle, so we don't know what the
		 * exit code was, but we can check whether it's alive or not
		 */
		if (is_pid_valid (pid)) {
			*code = STILL_ACTIVE;
			return TRUE;
		} else {
			return FALSE;
		}
	}

	process_handle = lookup_process_handle (process);
	if (!process_handle) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__, process);
		
		return FALSE;
	}

	if (process_handle->id == _wapi_getpid ()) {
		*code = STILL_ACTIVE;
		return TRUE;
	}

	/* A process handle is only signalled if the process has exited
	 * and has been waited for */

	/* Make sure any process exit has been noticed, before
	 * checking if the process is signalled.  Fixes bug 325463.
	 */
	process_wait (process, 0, TRUE);
	
	if (_wapi_handle_issignalled (process))
		*code = process_handle->exitstatus;
	else
		*code = STILL_ACTIVE;
	
	return TRUE;
}

gboolean
GetProcessTimes (gpointer process, WapiFileTime *create_time,
				 WapiFileTime *exit_time, WapiFileTime *kernel_time,
				 WapiFileTime *user_time)
{
	WapiHandle_process *process_handle;
	gboolean ku_times_set = FALSE;
	
	if (create_time == NULL || exit_time == NULL || kernel_time == NULL ||
		user_time == NULL)
		/* Not sure if w32 allows NULLs here or not */
		return FALSE;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		gpointer pid = GINT_TO_POINTER (WAPI_HANDLE_TO_PID(process));
		gint64 start_ticks, user_ticks, kernel_ticks;

		mono_process_get_times (pid, &start_ticks, &user_ticks, &kernel_ticks);

		_wapi_guint64_to_filetime (start_ticks, create_time);
		_wapi_guint64_to_filetime (user_ticks, kernel_time);
		_wapi_guint64_to_filetime (kernel_ticks, user_time);

		return TRUE;
	}

	process_handle = lookup_process_handle (process);
	if (!process_handle) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__, process);
		
		return FALSE;
	}
	
	*create_time = process_handle->create_time;

	/* A process handle is only signalled if the process has
	 * exited.  Otherwise exit_time isn't set
	 */
	if (_wapi_handle_issignalled (process))
		*exit_time = process_handle->exit_time;

#ifdef HAVE_GETRUSAGE
	if (process_handle->id == getpid ()) {
		struct rusage time_data;
		if (getrusage (RUSAGE_SELF, &time_data) == 0) {
			guint64 tick_val;
			ku_times_set = TRUE;
			tick_val = (guint64)time_data.ru_utime.tv_sec * 10000000 + (guint64)time_data.ru_utime.tv_usec * 10;
			_wapi_guint64_to_filetime (tick_val, user_time);
			tick_val = (guint64)time_data.ru_stime.tv_sec * 10000000 + (guint64)time_data.ru_stime.tv_usec * 10;
			_wapi_guint64_to_filetime (tick_val, kernel_time);
		}
	}
#endif
	if (!ku_times_set) {
		memset (kernel_time, 0, sizeof (WapiFileTime));
		memset (user_time, 0, sizeof (WapiFileTime));
	}

	return TRUE;
}

typedef struct
{
	gpointer address_start;
	gpointer address_end;
	char *perms;
	gpointer address_offset;
	guint64 device;
	guint64 inode;
	char *filename;
} WapiProcModule;

static void free_procmodule (WapiProcModule *mod)
{
	if (mod->perms != NULL) {
		g_free (mod->perms);
	}
	if (mod->filename != NULL) {
		g_free (mod->filename);
	}
	g_free (mod);
}

static gint find_procmodule (gconstpointer a, gconstpointer b)
{
	WapiProcModule *want = (WapiProcModule *)a;
	WapiProcModule *compare = (WapiProcModule *)b;
	
	if ((want->device == compare->device) &&
	    (want->inode == compare->inode)) {
		return(0);
	} else {
		return(1);
	}
}

#if defined(USE_OSX_LOADER)
#include <mach-o/dyld.h>
#include <mach-o/getsect.h>

static GSList *load_modules (void)
{
	GSList *ret = NULL;
	WapiProcModule *mod;
	uint32_t count = _dyld_image_count ();
	int i = 0;

	for (i = 0; i < count; i++) {
#if SIZEOF_VOID_P == 8
		const struct mach_header_64 *hdr;
		const struct section_64 *sec;
#else
		const struct mach_header *hdr;
		const struct section *sec;
#endif
		const char *name;

		name = _dyld_get_image_name (i);
#if SIZEOF_VOID_P == 8
		hdr = (const struct mach_header_64*)_dyld_get_image_header (i);
		sec = getsectbynamefromheader_64 (hdr, SEG_DATA, SECT_DATA);
#else
		hdr = _dyld_get_image_header (i);
		sec = getsectbynamefromheader (hdr, SEG_DATA, SECT_DATA);
#endif

		/* Some dynlibs do not have data sections on osx (#533893) */
		if (sec == 0) {
			continue;
		}
			
		mod = g_new0 (WapiProcModule, 1);
		mod->address_start = GINT_TO_POINTER (sec->addr);
		mod->address_end = GINT_TO_POINTER (sec->addr+sec->size);
		mod->perms = g_strdup ("r--p");
		mod->address_offset = 0;
		mod->device = makedev (0, 0);
		mod->inode = i;
		mod->filename = g_strdup (name); 
		
		if (g_slist_find_custom (ret, mod, find_procmodule) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			free_procmodule (mod);
		}
	}

	ret = g_slist_reverse (ret);
	
	return(ret);
}
#elif defined(USE_BSD_LOADER)
#include <link.h>
static int load_modules_callback (struct dl_phdr_info *info, size_t size, void *ptr)
{
	if (size < offsetof (struct dl_phdr_info, dlpi_phnum)
	    + sizeof (info->dlpi_phnum))
		return (-1);

	struct dl_phdr_info *cpy = calloc(1, sizeof(struct dl_phdr_info));
	if (!cpy)
		return (-1);

	memcpy(cpy, info, sizeof(*info));

	g_ptr_array_add ((GPtrArray *)ptr, cpy);

	return (0);
}

static GSList *load_modules (void)
{
	GSList *ret = NULL;
	WapiProcModule *mod;
	GPtrArray *dlarray = g_ptr_array_new();
	int i;

	if (dl_iterate_phdr(load_modules_callback, dlarray) < 0)
		return (ret);

	for (i = 0; i < dlarray->len; i++) {
		struct dl_phdr_info *info = g_ptr_array_index (dlarray, i);

		mod = g_new0 (WapiProcModule, 1);
		mod->address_start = (gpointer)(info->dlpi_addr + info->dlpi_phdr[0].p_vaddr);
		mod->address_end = (gpointer)(info->dlpi_addr +
                                       info->dlpi_phdr[info->dlpi_phnum - 1].p_vaddr);
		mod->perms = g_strdup ("r--p");
		mod->address_offset = 0;
		mod->inode = i;
		mod->filename = g_strdup (info->dlpi_name); 

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: inode=%d, filename=%s, address_start=%p, address_end=%p", __func__,
				   mod->inode, mod->filename, mod->address_start, mod->address_end);

		free(info);

		if (g_slist_find_custom (ret, mod, find_procmodule) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			free_procmodule (mod);
		}
	}

	g_ptr_array_free (dlarray, TRUE);

	ret = g_slist_reverse (ret);

	return(ret);
}
#elif defined(USE_HAIKU_LOADER)

static GSList *load_modules (void)
{
	GSList *ret = NULL;
	WapiProcModule *mod;
	int32 cookie = 0;
	image_info imageInfo;

	while (get_next_image_info (B_CURRENT_TEAM, &cookie, &imageInfo) == B_OK) {
		mod = g_new0 (WapiProcModule, 1);
		mod->device = imageInfo.device;
		mod->inode = imageInfo.node;
		mod->filename = g_strdup (imageInfo.name);
		mod->address_start = MIN (imageInfo.text, imageInfo.data);
		mod->address_end = MAX ((uint8_t*)imageInfo.text + imageInfo.text_size,
			(uint8_t*)imageInfo.data + imageInfo.data_size);
		mod->perms = g_strdup ("r--p");
		mod->address_offset = 0;

		if (g_slist_find_custom (ret, mod, find_procmodule) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			free_procmodule (mod);
		}
	}

	ret = g_slist_reverse (ret);

	return ret;
}
#else
static GSList *load_modules (FILE *fp)
{
	GSList *ret = NULL;
	WapiProcModule *mod;
	char buf[MAXPATHLEN + 1], *p, *endp;
	char *start_start, *end_start, *prot_start, *offset_start;
	char *maj_dev_start, *min_dev_start, *inode_start, prot_buf[5];
	gpointer address_start, address_end, address_offset;
	guint32 maj_dev, min_dev;
	guint64 inode;
	guint64 device;
	
	while (fgets (buf, sizeof(buf), fp)) {
		p = buf;
		while (g_ascii_isspace (*p)) ++p;
		start_start = p;
		if (!g_ascii_isxdigit (*start_start)) {
			continue;
		}
		address_start = (gpointer)strtoul (start_start, &endp, 16);
		p = endp;
		if (*p != '-') {
			continue;
		}
		
		++p;
		end_start = p;
		if (!g_ascii_isxdigit (*end_start)) {
			continue;
		}
		address_end = (gpointer)strtoul (end_start, &endp, 16);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}
		
		while (g_ascii_isspace (*p)) ++p;
		prot_start = p;
		if (*prot_start != 'r' && *prot_start != '-') {
			continue;
		}
		memcpy (prot_buf, prot_start, 4);
		prot_buf[4] = '\0';
		while (!g_ascii_isspace (*p)) ++p;
		
		while (g_ascii_isspace (*p)) ++p;
		offset_start = p;
		if (!g_ascii_isxdigit (*offset_start)) {
			continue;
		}
		address_offset = (gpointer)strtoul (offset_start, &endp, 16);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}
		
		while(g_ascii_isspace (*p)) ++p;
		maj_dev_start = p;
		if (!g_ascii_isxdigit (*maj_dev_start)) {
			continue;
		}
		maj_dev = strtoul (maj_dev_start, &endp, 16);
		p = endp;
		if (*p != ':') {
			continue;
		}
		
		++p;
		min_dev_start = p;
		if (!g_ascii_isxdigit (*min_dev_start)) {
			continue;
		}
		min_dev = strtoul (min_dev_start, &endp, 16);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}
		
		while (g_ascii_isspace (*p)) ++p;
		inode_start = p;
		if (!g_ascii_isxdigit (*inode_start)) {
			continue;
		}
		inode = (guint64)strtol (inode_start, &endp, 10);
		p = endp;
		if (!g_ascii_isspace (*p)) {
			continue;
		}

		device = makedev ((int)maj_dev, (int)min_dev);
		if ((device == 0) &&
		    (inode == 0)) {
			continue;
		}
		
		while(g_ascii_isspace (*p)) ++p;
		/* p now points to the filename */

		mod = g_new0 (WapiProcModule, 1);
		mod->address_start = address_start;
		mod->address_end = address_end;
		mod->perms = g_strdup (prot_buf);
		mod->address_offset = address_offset;
		mod->device = device;
		mod->inode = inode;
		mod->filename = g_strdup (g_strstrip (p));
		
		if (g_slist_find_custom (ret, mod, find_procmodule) == NULL) {
			ret = g_slist_prepend (ret, mod);
		} else {
			free_procmodule (mod);
		}
	}

	ret = g_slist_reverse (ret);
	
	return(ret);
}
#endif

static gboolean match_procname_to_modulename (char *procname, char *modulename)
{
	char* lastsep = NULL;
	char* lastsep2 = NULL;
	char* pname = NULL;
	char* mname = NULL;
	gboolean result = FALSE;

	if (procname == NULL || modulename == NULL)
		return (FALSE);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: procname=\"%s\", modulename=\"%s\"", __func__, procname, modulename);
	pname = mono_path_resolve_symlinks (procname);
	mname = mono_path_resolve_symlinks (modulename);

	if (!strcmp (pname, mname))
		result = TRUE;

	if (!result) {
		lastsep = strrchr (mname, '/');
		if (lastsep)
			if (!strcmp (lastsep+1, pname))
				result = TRUE;
		if (!result) {
			lastsep2 = strrchr (pname, '/');
			if (lastsep2){
				if (lastsep) {
					if (!strcmp (lastsep+1, lastsep2+1))
						result = TRUE;
				} else {
					if (!strcmp (mname, lastsep2+1))
						result = TRUE;
				}
			}
		}
	}

	g_free (pname);
	g_free (mname);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: result is %d", __func__, result);
	return result;
}

#if !(defined(USE_OSX_LOADER) || defined(USE_BSD_LOADER) || defined(USE_HAIKU_LOADER))
static FILE *
open_process_map (int pid, const char *mode)
{
	FILE *fp = NULL;
	const char *proc_path[] = {
		"/proc/%d/maps",	/* GNU/Linux */
		"/proc/%d/map",		/* FreeBSD */
		NULL
	};
	int i;
	char *filename;

	for (i = 0; fp == NULL && proc_path [i]; i++) {
 		filename = g_strdup_printf (proc_path[i], pid);
		fp = fopen (filename, mode);
		g_free (filename);
	}

	return fp;
}
#endif

static char *get_process_name_from_proc (pid_t pid);

gboolean EnumProcessModules (gpointer process, gpointer *modules,
			     guint32 size, guint32 *needed)
{
	WapiHandle_process *process_handle;
#if !defined(USE_OSX_LOADER) && !defined(USE_BSD_LOADER)
	FILE *fp;
#endif
	GSList *mods = NULL;
	WapiProcModule *module;
	guint32 count, avail = size / sizeof(gpointer);
	int i;
	pid_t pid;
	char *proc_name = NULL;
	
	/* Store modules in an array of pointers (main module as
	 * modules[0]), using the load address for each module as a
	 * token.  (Use 'NULL' as an alternative for the main module
	 * so that the simple implementation can just return one item
	 * for now.)  Get the info from /proc/<pid>/maps on linux,
	 * /proc/<pid>/map on FreeBSD, other systems will have to
	 * implement /dev/kmem reading or whatever other horrid
	 * technique is needed.
	 */
	if (size < sizeof(gpointer))
		return FALSE;

	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		pid = WAPI_HANDLE_TO_PID (process);
		proc_name = get_process_name_from_proc (pid);
	} else {
		process_handle = lookup_process_handle (process);
		if (!process_handle) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__, process);
		
			return FALSE;
		}
		pid = process_handle->id;
		proc_name = g_strdup (process_handle->proc_name);
	}
	
#if defined(USE_OSX_LOADER) || defined(USE_BSD_LOADER) || defined(USE_HAIKU_LOADER)
	mods = load_modules ();
	if (!proc_name) {
		modules[0] = NULL;
		*needed = sizeof(gpointer);
		return TRUE;
	}
#else
	fp = open_process_map (pid, "r");
	if (!fp) {
		/* No /proc/<pid>/maps so just return the main module
		 * shortcut for now
		 */
		modules[0] = NULL;
		*needed = sizeof(gpointer);
		g_free (proc_name);
		return TRUE;
	}
	mods = load_modules (fp);
	fclose (fp);
#endif
	count = g_slist_length (mods);
		
	/* count + 1 to leave slot 0 for the main module */
	*needed = sizeof(gpointer) * (count + 1);

	/*
	 * Use the NULL shortcut, as the first line in
	 * /proc/<pid>/maps isn't the executable, and we need
	 * that first in the returned list. Check the module name 
	 * to see if it ends with the proc name and substitute 
	 * the first entry with it.  FIXME if this turns out to 
	 * be a problem.
	 */
	modules[0] = NULL;
	for (i = 0; i < (avail - 1) && i < count; i++) {
		module = (WapiProcModule *)g_slist_nth_data (mods, i);
		if (modules[0] != NULL)
			modules[i] = module->address_start;
		else if (match_procname_to_modulename (proc_name, module->filename))
			modules[0] = module->address_start;
		else
			modules[i + 1] = module->address_start;
	}
		
	for (i = 0; i < count; i++) {
		free_procmodule ((WapiProcModule *)g_slist_nth_data (mods, i));
	}
	g_slist_free (mods);
	g_free (proc_name);
	
	return TRUE;
}

static char *
get_process_name_from_proc (pid_t pid)
{
#if defined(USE_BSD_LOADER)
	int mib [6];
	size_t size;
	struct kinfo_proc *pi;
#elif defined(USE_OSX_LOADER)
#if !(!defined (__mono_ppc__) && defined (TARGET_OSX))
	size_t size;
	struct kinfo_proc *pi;
	int mib[] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, pid };
#endif
#else
	FILE *fp;
	char *filename = NULL;
#endif
	char buf[256];
	char *ret = NULL;

#if defined(PLATFORM_SOLARIS)
	filename = g_strdup_printf ("/proc/%d/psinfo", pid);
	if ((fp = fopen (filename, "r")) != NULL) {
		struct psinfo info;
		int nread;

		nread = fread (&info, sizeof (info), 1, fp);
		if (nread == 1) {
			ret = g_strdup (info.pr_fname);
		}

		fclose (fp);
	}
	g_free (filename);
#elif defined(USE_OSX_LOADER)
#if !defined (__mono_ppc__) && defined (TARGET_OSX)
	/* No proc name on OSX < 10.5 nor ppc nor iOS */
	memset (buf, '\0', sizeof(buf));
	proc_name (pid, buf, sizeof(buf));

	// Fixes proc_name triming values to 15 characters #32539
	if (strlen (buf) >= MAXCOMLEN - 1) {
		char path_buf [PROC_PIDPATHINFO_MAXSIZE];
		char *name_buf;
		int path_len;

		memset (path_buf, '\0', sizeof(path_buf));
		path_len = proc_pidpath (pid, path_buf, sizeof(path_buf));

		if (path_len > 0 && path_len < sizeof(path_buf)) {
			name_buf = path_buf + path_len;
			for(;name_buf > path_buf; name_buf--) {
				if (name_buf [0] == '/') {
					name_buf++;
					break;
				}
			}

			if (memcmp (buf, name_buf, MAXCOMLEN - 1) == 0)
				ret = g_strdup (name_buf);
		}
	}

	if (ret == NULL && strlen (buf) > 0)
		ret = g_strdup (buf);
#else
	if (sysctl(mib, 4, NULL, &size, NULL, 0) < 0)
		return(ret);

	if ((pi = malloc(size)) == NULL)
		return(ret);

	if (sysctl (mib, 4, pi, &size, NULL, 0) < 0) {
		if (errno == ENOMEM) {
			free(pi);
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Didn't allocate enough memory for kproc info", __func__);
		}
		return(ret);
	}

	if (strlen (pi->kp_proc.p_comm) > 0)
		ret = g_strdup (pi->kp_proc.p_comm);

	free(pi);
#endif
#elif defined(USE_BSD_LOADER)
#if defined(__FreeBSD__)
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_PID;
	mib [3] = pid;
	if (sysctl(mib, 4, NULL, &size, NULL, 0) < 0) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: sysctl() failed: %d", __func__, errno);
		return(ret);
	}

	if ((pi = malloc(size)) == NULL)
		return(ret);

	if (sysctl (mib, 4, pi, &size, NULL, 0) < 0) {
		if (errno == ENOMEM) {
			free(pi);
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Didn't allocate enough memory for kproc info", __func__);
		}
		return(ret);
	}

	if (strlen (pi->ki_comm) > 0)
		ret = g_strdup (pi->ki_comm);
	free(pi);
#elif defined(__OpenBSD__)
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC;
	mib [2] = KERN_PROC_PID;
	mib [3] = pid;
	mib [4] = sizeof(struct kinfo_proc);
	mib [5] = 0;

retry:
	if (sysctl(mib, 6, NULL, &size, NULL, 0) < 0) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: sysctl() failed: %d", __func__, errno);
		return(ret);
	}

	if ((pi = malloc(size)) == NULL)
		return(ret);

	mib[5] = (int)(size / sizeof(struct kinfo_proc));

	if ((sysctl (mib, 6, pi, &size, NULL, 0) < 0) ||
		(size != sizeof (struct kinfo_proc))) {
		if (errno == ENOMEM) {
			free(pi);
			goto retry;
		}
		return(ret);
	}

	if (strlen (pi->p_comm) > 0)
		ret = g_strdup (pi->p_comm);

	free(pi);
#endif
#elif defined(USE_HAIKU_LOADER)
	image_info imageInfo;
	int32 cookie = 0;

	if (get_next_image_info ((team_id)pid, &cookie, &imageInfo) == B_OK) {
		ret = g_strdup (imageInfo.name);
	}
#else
	memset (buf, '\0', sizeof(buf));
	filename = g_strdup_printf ("/proc/%d/exe", pid);
	if (readlink (filename, buf, 255) > 0) {
		ret = g_strdup (buf);
	}
	g_free (filename);

	if (ret != NULL) {
		return(ret);
	}

	filename = g_strdup_printf ("/proc/%d/cmdline", pid);
	if ((fp = fopen (filename, "r")) != NULL) {
		if (fgets (buf, 256, fp) != NULL) {
			ret = g_strdup (buf);
		}
		
		fclose (fp);
	}
	g_free (filename);

	if (ret != NULL) {
		return(ret);
	}
	
	filename = g_strdup_printf ("/proc/%d/stat", pid);
	if ((fp = fopen (filename, "r")) != NULL) {
		if (fgets (buf, 256, fp) != NULL) {
			char *start, *end;
			
			start = strchr (buf, '(');
			if (start != NULL) {
				end = strchr (start + 1, ')');
				
				if (end != NULL) {
					ret = g_strndup (start + 1,
							 end - start - 1);
				}
			}
		}
		
		fclose (fp);
	}
	g_free (filename);
#endif

	return ret;
}

/*
 * wapi_process_get_path:
 *
 *   Return the full path of the executable of the process PID, or NULL if it cannot be determined.
 * Returns malloc-ed memory.
 */
char*
wapi_process_get_path (pid_t pid)
{
#if defined(PLATFORM_MACOSX) && !defined(__mono_ppc__) && defined(TARGET_OSX)
	char buf [PROC_PIDPATHINFO_MAXSIZE];
	int res;

	res = proc_pidpath (pid, buf, sizeof (buf));
	if (res <= 0)
		return NULL;
	if (buf [0] == '\0')
		return NULL;
	return g_strdup (buf);
#else
	return get_process_name_from_proc (pid);
#endif
}

/*
 * wapi_process_set_cli_launcher:
 *
 *   Set the full path of the runtime executable used to launch managed exe's.
 */
void
wapi_process_set_cli_launcher (char *path)
{
	g_free (cli_launcher);
	cli_launcher = path ? g_strdup (path) : NULL;
}

static guint32
get_module_name (gpointer process, gpointer module,
				 gunichar2 *basename, guint32 size,
				 gboolean base)
{
	WapiHandle_process *process_handle;
	pid_t pid;
	gunichar2 *procname;
	char *procname_ext = NULL;
	glong len;
	gsize bytes;
#if !defined(USE_OSX_LOADER) && !defined(USE_BSD_LOADER)
	FILE *fp;
#endif
	GSList *mods = NULL;
	WapiProcModule *found_module;
	guint32 count;
	int i;
	char *proc_name = NULL;
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Getting module base name, process handle %p module %p",
		   __func__, process, module);

	size = size * sizeof (gunichar2); /* adjust for unicode characters */

	if (basename == NULL || size == 0)
		return 0;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		/* This is a pseudo handle */
		pid = (pid_t)WAPI_HANDLE_TO_PID (process);
		proc_name = get_process_name_from_proc (pid);
	} else {
		process_handle = lookup_process_handle (process);
		if (!process_handle) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__,
				   process);
			
			return 0;
		}
		pid = process_handle->id;
		proc_name = g_strdup (process_handle->proc_name);
	}

	/* Look up the address in /proc/<pid>/maps */
#if defined(USE_OSX_LOADER) || defined(USE_BSD_LOADER) || defined(USE_HAIKU_LOADER)
	mods = load_modules ();
#else
	fp = open_process_map (pid, "r");
	if (fp == NULL) {
		if (errno == EACCES && module == NULL && base == TRUE) {
			procname_ext = get_process_name_from_proc (pid);
		} else {
			/* No /proc/<pid>/maps, so just return failure
			 * for now
			 */
			g_free (proc_name);
			return 0;
		}
	} else {
		mods = load_modules (fp);
		fclose (fp);
	}
#endif
	count = g_slist_length (mods);

	/* If module != NULL compare the address.
	 * If module == NULL we are looking for the main module.
	 * The best we can do for now check it the module name end with the process name.
	 */
	for (i = 0; i < count; i++) {
		found_module = (WapiProcModule *)g_slist_nth_data (mods, i);
		if (procname_ext == NULL &&
			((module == NULL && match_procname_to_modulename (proc_name, found_module->filename)) ||
			 (module != NULL && found_module->address_start == module))) {
			if (base)
				procname_ext = g_path_get_basename (found_module->filename);
			else
				procname_ext = g_strdup (found_module->filename);
		}

		free_procmodule (found_module);
	}

	if (procname_ext == NULL) {
		/* If it's *still* null, we might have hit the
		 * case where reading /proc/$pid/maps gives an
		 * empty file for this user.
		 */
		procname_ext = get_process_name_from_proc (pid);
	}

	g_slist_free (mods);
	g_free (proc_name);

	if (procname_ext) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Process name is [%s]", __func__,
			   procname_ext);

		procname = mono_unicode_from_external (procname_ext, &bytes);
		if (procname == NULL) {
			/* bugger */
			g_free (procname_ext);
			return 0;
		}
		
		len = (bytes / 2);
		
		/* Add the terminator */
		bytes += 2;
		
		if (size < bytes) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Size %d smaller than needed (%ld); truncating", __func__, size, bytes);

			memcpy (basename, procname, size);
		} else {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Size %d larger than needed (%ld)",
				   __func__, size, bytes);

			memcpy (basename, procname, bytes);
		}
		
		g_free (procname);
		g_free (procname_ext);
		
		return len;
	}
	
	return 0;
}

static guint32
get_module_filename (gpointer process, gpointer module,
					 gunichar2 *basename, guint32 size)
{
	int pid, len;
	gsize bytes;
	char *path;
	gunichar2 *proc_path;
	
	size *= sizeof (gunichar2); /* adjust for unicode characters */

	if (basename == NULL || size == 0)
		return 0;

	pid = GetProcessId (process);

	path = wapi_process_get_path (pid);
	if (path == NULL)
		return 0;

	proc_path = mono_unicode_from_external (path, &bytes);
	g_free (path);

	if (proc_path == NULL)
		return 0;

	len = (bytes / 2);
	
	/* Add the terminator */
	bytes += 2;

	if (size < bytes) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Size %d smaller than needed (%ld); truncating", __func__, size, bytes);

		memcpy (basename, proc_path, size);
	} else {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Size %d larger than needed (%ld)",
			   __func__, size, bytes);

		memcpy (basename, proc_path, bytes);
	}

	g_free (proc_path);

	return len;
}

guint32
GetModuleBaseName (gpointer process, gpointer module,
				   gunichar2 *basename, guint32 size)
{
	return get_module_name (process, module, basename, size, TRUE);
}

guint32
GetModuleFileNameEx (gpointer process, gpointer module,
					 gunichar2 *filename, guint32 size)
{
	return get_module_filename (process, module, filename, size);
}

gboolean
GetModuleInformation (gpointer process, gpointer module,
					  WapiModuleInfo *modinfo, guint32 size)
{
	WapiHandle_process *process_handle;
	pid_t pid;
#if !defined(USE_OSX_LOADER) && !defined(USE_BSD_LOADER)
	FILE *fp;
#endif
	GSList *mods = NULL;
	WapiProcModule *found_module;
	guint32 count;
	int i;
	gboolean ret = FALSE;
	char *proc_name = NULL;
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Getting module info, process handle %p module %p",
		   __func__, process, module);

	if (modinfo == NULL || size < sizeof (WapiModuleInfo))
		return FALSE;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		pid = (pid_t)WAPI_HANDLE_TO_PID (process);
		proc_name = get_process_name_from_proc (pid);
	} else {
		process_handle = lookup_process_handle (process);
		if (!process_handle) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__,
				   process);
			
			return FALSE;
		}
		pid = process_handle->id;
		proc_name = g_strdup (process_handle->proc_name);
	}

#if defined(USE_OSX_LOADER) || defined(USE_BSD_LOADER) || defined(USE_HAIKU_LOADER)
	mods = load_modules ();
#else
	/* Look up the address in /proc/<pid>/maps */
	if ((fp = open_process_map (pid, "r")) == NULL) {
		/* No /proc/<pid>/maps, so just return failure
		 * for now
		 */
		g_free (proc_name);
		return FALSE;
	}
	mods = load_modules (fp);
	fclose (fp);
#endif
	count = g_slist_length (mods);

	/* If module != NULL compare the address.
	 * If module == NULL we are looking for the main module.
	 * The best we can do for now check it the module name end with the process name.
	 */
	for (i = 0; i < count; i++) {
			found_module = (WapiProcModule *)g_slist_nth_data (mods, i);
			if (ret == FALSE &&
				((module == NULL && match_procname_to_modulename (proc_name, found_module->filename)) ||
				 (module != NULL && found_module->address_start == module))) {
				modinfo->lpBaseOfDll = found_module->address_start;
				modinfo->SizeOfImage = (gsize)(found_module->address_end) - (gsize)(found_module->address_start);
				modinfo->EntryPoint = found_module->address_offset;
				ret = TRUE;
			}

			free_procmodule (found_module);
	}

	g_slist_free (mods);
	g_free (proc_name);

	return ret;
}

gboolean
GetProcessWorkingSetSize (gpointer process, size_t *min, size_t *max)
{
	WapiHandle_process *process_handle;
	
	if (min == NULL || max == NULL)
		/* Not sure if w32 allows NULLs here or not */
		return FALSE;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process))
		/* This is a pseudo handle, so just fail for now */
		return FALSE;
	
	process_handle = lookup_process_handle (process);
	if (!process_handle) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__, process);
		
		return FALSE;
	}

	*min = process_handle->min_working_set;
	*max = process_handle->max_working_set;
	
	return TRUE;
}

gboolean
SetProcessWorkingSetSize (gpointer process, size_t min, size_t max)
{
	WapiHandle_process *process_handle;

	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process))
		/* This is a pseudo handle, so just fail for now
		 */
		return FALSE;

	process_handle = lookup_process_handle (process);
	if (!process_handle) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__, process);
		
		return FALSE;
	}

	process_handle->min_working_set = min;
	process_handle->max_working_set = max;
	
	return TRUE;
}


gboolean
TerminateProcess (gpointer process, gint32 exitCode)
{
#if defined(HAVE_KILL)
	WapiHandle_process *process_handle;
	int signo;
	int ret;
	pid_t pid;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		/* This is a pseudo handle */
		pid = (pid_t)WAPI_HANDLE_TO_PID (process);
	} else {
		process_handle = lookup_process_handle (process);
		if (!process_handle) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Can't find process %p", __func__, process);
			SetLastError (ERROR_INVALID_HANDLE);
			return FALSE;
		}
		pid = process_handle->id;
	}

	signo = (exitCode == -1) ? SIGKILL : SIGTERM;
	ret = kill (pid, signo);
	if (ret == -1) {
		switch (errno) {
		case EINVAL:
			SetLastError (ERROR_INVALID_PARAMETER);
			break;
		case EPERM:
			SetLastError (ERROR_ACCESS_DENIED);
			break;
		case ESRCH:
			SetLastError (ERROR_PROC_NOT_FOUND);
			break;
		default:
			SetLastError (ERROR_GEN_FAILURE);
		}
	}
	
	return (ret == 0);
#else
	g_error ("kill() is not supported by this platform");
	return FALSE;
#endif
}

guint32
GetPriorityClass (gpointer process)
{
#ifdef HAVE_GETPRIORITY
	WapiHandle_process *process_handle;
	int ret;
	pid_t pid;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		/* This is a pseudo handle */
		pid = (pid_t)WAPI_HANDLE_TO_PID (process);
	} else {
		process_handle = lookup_process_handle (process);
		if (!process_handle) {
			SetLastError (ERROR_INVALID_HANDLE);
			return FALSE;
		}
		pid = process_handle->id;
	}

	errno = 0;
	ret = getpriority (PRIO_PROCESS, pid);
	if (ret == -1 && errno != 0) {
		switch (errno) {
		case EPERM:
		case EACCES:
			SetLastError (ERROR_ACCESS_DENIED);
			break;
		case ESRCH:
			SetLastError (ERROR_PROC_NOT_FOUND);
			break;
		default:
			SetLastError (ERROR_GEN_FAILURE);
		}
		return FALSE;
	}

	if (ret == 0)
		return NORMAL_PRIORITY_CLASS;
	else if (ret < -15)
		return REALTIME_PRIORITY_CLASS;
	else if (ret < -10)
		return HIGH_PRIORITY_CLASS;
	else if (ret < 0)
		return ABOVE_NORMAL_PRIORITY_CLASS;
	else if (ret > 10)
		return IDLE_PRIORITY_CLASS;
	else if (ret > 0)
		return BELOW_NORMAL_PRIORITY_CLASS;

	return NORMAL_PRIORITY_CLASS;
#else
	SetLastError (ERROR_NOT_SUPPORTED);
	return 0;
#endif
}

gboolean
SetPriorityClass (gpointer process, guint32  priority_class)
{
#ifdef HAVE_SETPRIORITY
	WapiHandle_process *process_handle;
	int ret;
	int prio;
	pid_t pid;
	
	if (WAPI_IS_PSEUDO_PROCESS_HANDLE (process)) {
		/* This is a pseudo handle */
		pid = (pid_t)WAPI_HANDLE_TO_PID (process);
	} else {
		process_handle = lookup_process_handle (process);
		if (!process_handle) {
			SetLastError (ERROR_INVALID_HANDLE);
			return FALSE;
		}
		pid = process_handle->id;
	}

	switch (priority_class) {
	case IDLE_PRIORITY_CLASS:
		prio = 19;
		break;
	case BELOW_NORMAL_PRIORITY_CLASS:
		prio = 10;
		break;
	case NORMAL_PRIORITY_CLASS:
		prio = 0;
		break;
	case ABOVE_NORMAL_PRIORITY_CLASS:
		prio = -5;
		break;
	case HIGH_PRIORITY_CLASS:
		prio = -11;
		break;
	case REALTIME_PRIORITY_CLASS:
		prio = -20;
		break;
	default:
		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	ret = setpriority (PRIO_PROCESS, pid, prio);
	if (ret == -1) {
		switch (errno) {
		case EPERM:
		case EACCES:
			SetLastError (ERROR_ACCESS_DENIED);
			break;
		case ESRCH:
			SetLastError (ERROR_PROC_NOT_FOUND);
			break;
		default:
			SetLastError (ERROR_GEN_FAILURE);
		}
	}

	return ret == 0;
#else
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
#endif
}

static void
mono_processes_cleanup (void)
{
	struct MonoProcess *mp;
	struct MonoProcess *prev = NULL;
	GSList *finished = NULL;
	GSList *l;
	gpointer unref_handle;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s", __func__);

	/* Ensure we're not in here in multiple threads at once, nor recursive. */
	if (InterlockedCompareExchange (&mono_processes_cleaning_up, 1, 0) != 0)
		return;

	for (mp = mono_processes; mp; mp = mp->next) {
		if (mp->pid == 0 && mp->handle) {
			/* This process has exited and we need to remove the artifical ref
			 * on the handle */
			mono_os_mutex_lock (&mono_processes_mutex);
			unref_handle = mp->handle;
			mp->handle = NULL;
			mono_os_mutex_unlock (&mono_processes_mutex);
			if (unref_handle)
				_wapi_handle_unref (unref_handle);
		}
	}

	/*
	 * Remove processes which exited from the mono_processes list.
	 * We need to synchronize with the sigchld handler here, which runs
	 * asynchronously. The handler requires that the mono_processes list
	 * remain valid.
	 */
	mono_os_mutex_lock (&mono_processes_mutex);

	mp = mono_processes;
	while (mp) {
		if (mp->handle_count == 0 && mp->freeable) {
			/*
			 * Unlink the entry.
			 * This code can run parallel with the sigchld handler, but the
			 * modifications it makes are safe.
			 */
			if (mp == mono_processes)
				mono_processes = mp->next;
			else
				prev->next = mp->next;
			finished = g_slist_prepend (finished, mp);

			mp = mp->next;
		} else {
			prev = mp;
			mp = mp->next;
		}
	}

	mono_memory_barrier ();

	for (l = finished; l; l = l->next) {
		/*
		 * All the entries in the finished list are unlinked from mono_processes, and
		 * they have the 'finished' flag set, which means the sigchld handler is done
		 * accessing them.
		 */
		mp = (MonoProcess *)l->data;
		mono_os_sem_destroy (&mp->exit_sem);
		g_free (mp);
	}
	g_slist_free (finished);

	mono_os_mutex_unlock (&mono_processes_mutex);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s done", __func__);

	InterlockedDecrement (&mono_processes_cleaning_up);
}

static void
process_close (gpointer handle, gpointer data)
{
	WapiHandle_process *process_handle;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s", __func__);

	process_handle = (WapiHandle_process *) data;
	g_free (process_handle->proc_name);
	process_handle->proc_name = NULL;
	if (process_handle->mono_process)
		InterlockedDecrement (&process_handle->mono_process->handle_count);
	mono_processes_cleanup ();
}

#if HAVE_SIGACTION
MONO_SIGNAL_HANDLER_FUNC (static, mono_sigchld_signal_handler, (int _dummy, siginfo_t *info, void *context))
{
	int status;
	int pid;
	struct MonoProcess *p;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "SIG CHILD handler for pid: %i\n", info->si_pid);

	do {
		do {
			pid = waitpid (-1, &status, WNOHANG);
		} while (pid == -1 && errno == EINTR);

		if (pid <= 0)
			break;

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "child ended: %i", pid);

		/*
		 * This can run concurrently with the code in the rest of this module.
		 */
		for (p = mono_processes; p; p = p->next) {
			if (p->pid == pid) {
				break;
			}
		}
		if (p) {
			p->pid = 0; /* this pid doesn't exist anymore, clear it */
			p->status = status;
			mono_os_sem_post (&p->exit_sem);
			mono_memory_barrier ();
			/* Mark this as freeable, the pointer becomes invalid afterwards */
			p->freeable = TRUE;
		}
	} while (1);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "SIG CHILD handler: done looping.");
}

#endif

static void
process_add_sigchld_handler (void)
{
#if HAVE_SIGACTION
	struct sigaction sa;

	sa.sa_sigaction = mono_sigchld_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_NOCLDSTOP | SA_SIGINFO;
	g_assert (sigaction (SIGCHLD, &sa, &previous_chld_sa) != -1);
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "Added SIGCHLD handler");
#endif
}

static guint32
process_wait (gpointer handle, guint32 timeout, gboolean alertable)
{
	WapiHandle_process *process_handle;
	pid_t pid G_GNUC_UNUSED, ret;
	int status;
	guint32 start;
	guint32 now;
	struct MonoProcess *mp;

	/* FIXME: We can now easily wait on processes that aren't our own children,
	 * but WaitFor*Object won't call us for pseudo handles. */
	g_assert ((GPOINTER_TO_UINT (handle) & _WAPI_PROCESS_UNHANDLED) != _WAPI_PROCESS_UNHANDLED);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u)", __func__, handle, timeout);

	process_handle = lookup_process_handle (handle);
	if (!process_handle) {
		g_warning ("%s: error looking up process handle %p", __func__, handle);
		return WAIT_FAILED;
	}

	if (process_handle->exited) {
		/* We've already done this one */
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): Process already exited", __func__, handle, timeout);
		return WAIT_OBJECT_0;
	}

	pid = process_handle->id;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): PID: %d", __func__, handle, timeout, pid);

	/* We don't need to lock mono_processes here, the entry
	 * has a handle_count > 0 which means it will not be freed. */
	mp = process_handle->mono_process;
	if (!mp) {
		pid_t res;

		if (pid == mono_process_current_pid ()) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): waiting on current process", __func__, handle, timeout);
			return WAIT_TIMEOUT;
		}

		/* This path is used when calling Process.HasExited, so
		 * it is only used to poll the state of the process, not
		 * to actually wait on it to exit */
		g_assert (timeout == 0);

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): waiting on non-child process", __func__, handle, timeout);

		res = waitpid (pid, &status, WNOHANG);
		if (res == 0) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): non-child process WAIT_TIMEOUT", __func__, handle, timeout);
			return WAIT_TIMEOUT;
		}
		if (res > 0) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): non-child process waited successfully", __func__, handle, timeout);
			return WAIT_OBJECT_0;
		}

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): non-child process WAIT_FAILED, error : %s (%d))", __func__, handle, timeout, g_strerror (errno), errno);
		return WAIT_FAILED;
	}

	start = mono_msec_ticks ();
	now = start;

	while (1) {
		if (timeout != INFINITE) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): waiting on semaphore for %li ms...", 
				   __func__, handle, timeout, (timeout - (now - start)));
			ret = mono_os_sem_timedwait (&mp->exit_sem, (timeout - (now - start)), alertable ? MONO_SEM_FLAGS_ALERTABLE : MONO_SEM_FLAGS_NONE);
		} else {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): waiting on semaphore forever...", 
				   __func__, handle, timeout);
			ret = mono_os_sem_wait (&mp->exit_sem, alertable ? MONO_SEM_FLAGS_ALERTABLE : MONO_SEM_FLAGS_NONE);
		}

		if (ret == -1 && errno != EINTR && errno != ETIMEDOUT) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): sem_timedwait failure: %s", 
				   __func__, handle, timeout, g_strerror (errno));
			/* Should we return a failure here? */
		}

		if (ret == 0) {
			/* Success, process has exited */
			mono_os_sem_post (&mp->exit_sem);
			break;
		}

		if (timeout == 0) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): WAIT_TIMEOUT (timeout = 0)", __func__, handle, timeout);
			return WAIT_TIMEOUT;
		}

		now = mono_msec_ticks ();
		if (now - start >= timeout) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): WAIT_TIMEOUT", __func__, handle, timeout);
			return WAIT_TIMEOUT;
		}
		
		if (alertable && _wapi_thread_cur_apc_pending ()) {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): WAIT_IO_COMPLETION", __func__, handle, timeout);
			return WAIT_IO_COMPLETION;
		}
	}

	/* Process must have exited */
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): Waited successfully", __func__, handle, timeout);

	ret = _wapi_handle_lock_shared_handles ();
	g_assert (ret == 0);

	status = mp ? mp->status : 0;
	if (WIFSIGNALED (status))
		process_handle->exitstatus = 128 + WTERMSIG (status);
	else
		process_handle->exitstatus = WEXITSTATUS (status);
	_wapi_time_t_to_filetime (time (NULL), &process_handle->exit_time);

	process_handle->exited = TRUE;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s (%p, %u): Setting pid %d signalled, exit status %d",
		   __func__, handle, timeout, process_handle->id, process_handle->exitstatus);

	_wapi_handle_set_signal_state (handle, TRUE, TRUE);

	_wapi_handle_unlock_shared_handles ();

	return WAIT_OBJECT_0;
}

void
wapi_processes_cleanup (void)
{
	g_free (cli_launcher);
}
