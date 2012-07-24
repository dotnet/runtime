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
#include <signal.h>
#include <sys/wait.h>
#include <sys/time.h>
#include <sys/resource.h>
#include <fcntl.h>
#include <sys/param.h>
#include <ctype.h>

#ifdef HAVE_SYS_MKDEV_H
#include <sys/mkdev.h>
#endif

/* sys/resource.h (for rusage) is required when using osx 10.3 (but not 10.4) */
#ifdef __APPLE__
#include <sys/resource.h>
#ifdef HAVE_LIBPROC_H
/* proc_name */
#include <libproc.h>
#endif
#endif

#if defined(PLATFORM_MACOSX) || defined(__OpenBSD__)
#include <sys/proc.h>
#include <sys/sysctl.h>
#  if !defined(__OpenBSD__)
#    include <sys/utsname.h>
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
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/process-private.h>
#include <mono/io-layer/threads.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-path.h>
#include <mono/io-layer/timefuncs-private.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-membar.h>

/* The process' environment strings */
#if defined(__APPLE__) && !defined (__arm__)
/* Apple defines this in crt_externs.h but doesn't provide that header for 
 * arm-apple-darwin9.  We'll manually define the symbol on Apple as it does
 * in fact exist on all implementations (so far) 
 */
gchar ***_NSGetEnviron(void);
#define environ (*_NSGetEnviron())
#else
extern char **environ;
#endif

#if 0
#define DEBUG(...) g_message(__VA_ARGS__)
#define DEBUG_ENABLED 1
#else
#define DEBUG(...)
#endif

static guint32 process_wait (gpointer handle, guint32 timeout, gboolean alertable);
static void process_close (gpointer handle, gpointer data);
static gboolean is_pid_valid (pid_t pid);

#if !defined(__OpenBSD__)
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
static volatile gint32 mono_processes_read_lock = 0;
static volatile gint32 mono_processes_cleaning_up = 0;
static mono_mutex_t mono_processes_mutex;
static void mono_processes_cleanup (void);

static mono_once_t process_current_once=MONO_ONCE_INIT;
static gpointer current_process=NULL;

static mono_once_t process_ops_once=MONO_ONCE_INIT;

static void process_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_PROCESS,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SPECIAL_WAIT);
}


/* Check if a pid is valid - i.e. if a process exists with this pid. */
static gboolean is_pid_valid (pid_t pid)
{
	gboolean result = FALSE;

#if defined(PLATFORM_MACOSX) || defined(__OpenBSD__)
	if (((kill(pid, 0) == 0) || (errno == EPERM)) && pid != 0)
		result = TRUE;
#elif defined(__HAIKU__)
	team_info teamInfo;
	if (get_team_info ((team_id)pid, &teamInfo) == B_OK)
		result = TRUE;
#else
	gchar *dir = g_strdup_printf ("/proc/%d", pid);
	if (!access (dir, F_OK))
		result = TRUE;
	g_free (dir);
#endif
	
	return result;
}

static void process_set_defaults (struct _WapiHandle_process *process_handle)
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

#ifdef PLATFORM_MACOSX

/* 0 = no detection; -1 = not 10.5 or higher;  1 = 10.5 or higher */
static int osx_10_5_or_higher;

static void
detect_osx_10_5_or_higher (void)
{
	struct utsname u;
	char *p;
	int v;
	
	if (uname (&u) != 0){
		osx_10_5_or_higher = 1;
		return;
	}

	p = u.release;
	v = atoi (p);
	
	if (v < 9)
		osx_10_5_or_higher = -1;
	else 
		osx_10_5_or_higher = 1;
}

static gboolean
is_macos_10_5_or_higher (void)
{
	if (osx_10_5_or_higher == 0)
		detect_osx_10_5_or_higher ();
	
	return (osx_10_5_or_higher == 1);
}
#endif

static const gunichar2 utf16_space_bytes [2] = { 0x20, 0 };
static const gunichar2 *utf16_space = utf16_space_bytes; 
static const gunichar2 utf16_quote_bytes [2] = { 0x22, 0 };
static const gunichar2 *utf16_quote = utf16_quote_bytes;

#ifdef DEBUG_ENABLED
/* Useful in gdb */
void
print_utf16 (gunichar2 *str)
{
	gchar *res;

	res = g_utf16_to_utf8 (str, -1, NULL, NULL, NULL);
	g_print ("%s\n", res);
	g_free (res);
}
#endif

/* Implemented as just a wrapper around CreateProcess () */
gboolean ShellExecuteEx (WapiShellExecuteInfo *sei)
{
	gboolean ret;
	WapiProcessInformation process_info;
	gunichar2 *args;
	
	if (sei == NULL) {
		/* w2k just segfaults here, but we can do better than
		 * that
		 */
		SetLastError (ERROR_INVALID_PARAMETER);
		return (FALSE);
	}

	if (sei->lpFile == NULL) {
		/* w2k returns TRUE for this, for some reason. */
		return (TRUE);
	}
	
	/* Put both executable and parameters into the second argument
	 * to CreateProcess (), so it searches $PATH.  The conversion
	 * into and back out of utf8 is because there is no
	 * g_strdup_printf () equivalent for gunichar2 :-(
	 */
	args = utf16_concat (utf16_quote, sei->lpFile, utf16_quote, sei->lpParameters == NULL ? NULL : utf16_space, sei->lpParameters, NULL);
	if (args == NULL){
		SetLastError (ERROR_INVALID_DATA);
		return (FALSE);
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
		if (is_macos_10_5_or_higher ())
			handler = g_strdup ("/usr/bin/open -W");
		else
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
					return (FALSE);
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
		if (args == NULL){
			SetLastError (ERROR_INVALID_DATA);
			return FALSE;
		}
		ret = CreateProcess (NULL, args, NULL, NULL, TRUE,
				     CREATE_UNICODE_ENVIRONMENT, NULL,
				     sei->lpDirectory, NULL, &process_info);
		g_free (args);
		if (!ret){
			SetLastError (ERROR_INVALID_DATA);
			return FALSE;
		}
	}
	
	if (sei->fMask & SEE_MASK_NOCLOSEPROCESS) {
		sei->hProcess = process_info.hProcess;
	} else {
		CloseHandle (process_info.hProcess);
	}
	
	return (ret);
}

static gboolean
is_managed_binary (const gchar *filename)
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

gboolean CreateProcessWithLogonW (const gunichar2 *username,
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
switchDirectorySeparators(gchar *path)
{
	size_t i, pathLength = strlen(path);
	
	/* Turn all the slashes round the right way, except for \' */
	/* There are probably other characters that need to be excluded as well. */
	for (i = 0; i < pathLength; i++)
	{
		if (path[i] == '\\' && i < pathLength - 1 && path[i+1] != '\'' ) {
			path[i] = '/';
		}
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
	gchar *cmd=NULL, *prog = NULL, *full_prog = NULL, *args = NULL, *args_after_prog = NULL, *dir = NULL, **env_strings = NULL, **argv = NULL;
	guint32 i, env_count = 0;
	gboolean ret = FALSE;
	gpointer handle;
	struct _WapiHandle_process process_handle = {0}, *process_handle_data;
	GError *gerr = NULL;
	int in_fd, out_fd, err_fd;
	pid_t pid;
	int thr_ret;
	int startup_pipe [2] = {-1, -1};
	int dummy;
	struct MonoProcess *mono_process;
	
	mono_once (&process_ops_once, process_ops_init);
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
			DEBUG ("%s: unicode conversion returned NULL",
				   __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}

		switchDirectorySeparators(cmd);
	}
	
	if (cmdline != NULL) {
		args = mono_unicode_to_external (cmdline);
		if (args == NULL) {
			DEBUG ("%s: unicode conversion returned NULL", __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}
	}

	if (cwd != NULL) {
		dir = mono_unicode_to_external (cwd);
		if (dir == NULL) {
			DEBUG ("%s: unicode conversion returned NULL", __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}

		/* Turn all the slashes round the right way */
		switchDirectorySeparators(dir);
	}
	

	/* We can't put off locating the executable any longer :-( */
	if (cmd != NULL) {
		gchar *unquoted;
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
			if (!is_executable (prog)) {
				DEBUG ("%s: Couldn't find executable %s",
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

			/* And make sure it's executable */
			if (!is_executable (prog)) {
				DEBUG ("%s: Couldn't find executable %s",
					   __func__, prog);
				g_free (unquoted);
				SetLastError (ERROR_FILE_NOT_FOUND);
				goto free_strings;
			}
		}
		g_free (unquoted);

		args_after_prog = args;
	} else {
		gchar *token = NULL;
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
			DEBUG ("%s: Couldn't find what to exec", __func__);

			SetLastError (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}
		
		/* Turn all the slashes round the right way. Only for
		 * the prg. name
		 */
		switchDirectorySeparators(token);

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
			if (!is_executable (prog)) {
				DEBUG ("%s: Couldn't find executable %s",
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
			 */
			if (!is_executable (prog)) {
				g_free (prog);
				prog = g_find_program_in_path (token);
				if (prog == NULL) {
					DEBUG ("%s: Couldn't find executable %s", __func__, token);

					g_free (token);
					SetLastError (ERROR_FILE_NOT_FOUND);
					goto free_strings;
				}
			}
		}

		g_free (token);
	}

	DEBUG ("%s: Exec prog [%s] args [%s]", __func__, prog,
		   args_after_prog);
	
	/* Check for CLR binaries; if found, we will try to invoke
	 * them using the same mono binary that started us.
	 */
	if (is_managed_binary (prog)) {
		gunichar2 *newapp, *newcmd;
		gsize bytes_ignored;

		newapp = mono_unicode_from_external ("mono", &bytes_ignored);

		if (newapp != NULL) {
			if (appname != NULL) {
				newcmd = utf16_concat (newapp, utf16_space,
						       appname, utf16_space,
						       cmdline, NULL);
			} else {
				newcmd = utf16_concat (newapp, utf16_space,
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
	}

	if (args_after_prog != NULL && *args_after_prog) {
		gchar *qprog;

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
	
	g_strlcpy (process_handle.proc_name, prog,
		   _WAPI_PROC_NAME_MAX_LEN - 1);

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
	if (new_environ != NULL) {
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
		env_strings = g_new0 (gchar *, env_count + 2);
		
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
		for (i = 0; environ[i] != NULL; i++) {
			env_count++;
		}

		/* +2: one for the process handle value, and the last
		 * one is NULL
		 */
		env_strings = g_new0 (gchar *, env_count + 2);
		
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
	/* pass process handle info to the child, so it doesn't have
	 * to do an expensive search over the whole list
	 */
	if (env_strings != NULL) {
		struct _WapiHandleUnshared *handle_data;
		struct _WapiHandle_shared_ref *ref;
		
		handle_data = &_WAPI_PRIVATE_HANDLES(GPOINTER_TO_UINT(handle));
		ref = &handle_data->u.shared;
		
		env_strings[env_count] = g_strdup_printf ("_WAPI_PROCESS_HANDLE_OFFSET=%d", ref->offset);
	}

	/* Create a pipe to make sure the child doesn't exit before 
	 * we can add the process to the linked list of mono_processes */
	if (pipe (startup_pipe) == -1) {
		/* Could not create the pipe to synchroniz process startup. We'll just not synchronize.
		 * This is just for a very hard to hit race condition in the first place */
		startup_pipe [0] = startup_pipe [1] = -1;
		DEBUG ("%s: new process startup not synchronized. We may not notice if the newly created process exits immediately.", __func__);
	}

	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	pid = fork ();
	if (pid == -1) {
		/* Error */
		SetLastError (ERROR_OUTOFMEMORY);
		_wapi_handle_unref (handle);
		goto cleanup;
	} else if (pid == 0) {
		/* Child */
		
		if (startup_pipe [0] != -1) {
			/* Wait until the parent has updated it's internal data */
			read (startup_pipe [0], &dummy, 1);
			DEBUG ("%s: child: parent has completed its setup", __func__);
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
		for (i = getdtablesize () - 1; i > 2; i--) {
			close (i);
		}

#ifdef DEBUG_ENABLED
		DEBUG ("%s: exec()ing [%s] in dir [%s]", __func__, cmd,
			   dir==NULL?".":dir);
		for (i = 0; argv[i] != NULL; i++) {
			g_message ("arg %d: [%s]", i, argv[i]);
		}
		
		for (i = 0; env_strings[i] != NULL; i++) {
			g_message ("env %d: [%s]", i, env_strings[i]);
		}
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
	
	ret = _wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				   (gpointer *)&process_handle_data);
	if (ret == FALSE) {
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
	if (MONO_SEM_INIT (&mono_process->exit_sem, 0) != 0) {
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

		process_handle_data->self = _wapi_getpid ();
		process_handle_data->mono_process = mono_process;

		mono_mutex_lock (&mono_processes_mutex);
		mono_process->next = mono_processes;
		mono_processes = mono_process;
		mono_mutex_unlock (&mono_processes_mutex);
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

	if (startup_pipe [1] != -1) {
		/* Write 1 byte, doesn't matter what */
		write (startup_pipe [1], startup_pipe, 1);
		close (startup_pipe [0]);
		close (startup_pipe [1]);
	}

free_strings:
	if (cmd != NULL) {
		g_free (cmd);
	}
	if (full_prog != NULL) {
		g_free (full_prog);
	}
	if (prog != NULL) {
		g_free (prog);
	}
	if (args != NULL) {
		g_free (args);
	}
	if (dir != NULL) {
		g_free (dir);
	}
	if(env_strings != NULL) {
		g_strfreev (env_strings);
	}
	if (argv != NULL) {
		g_strfreev (argv);
	}
	
	DEBUG ("%s: returning handle %p for pid %d", __func__, handle,
		   pid);

	/* Check if something needs to be cleaned up. */
	mono_processes_cleanup ();
	
	return(ret);
}
		
static void process_set_name (struct _WapiHandle_process *process_handle)
{
	gchar *progname, *utf8_progname, *slash;
	
	progname=g_get_prgname ();
	utf8_progname=mono_utf8_from_external (progname);

	DEBUG ("%s: using [%s] as prog name", __func__, progname);

	if(utf8_progname!=NULL) {
		slash=strrchr (utf8_progname, '/');
		if(slash!=NULL) {
			g_strlcpy (process_handle->proc_name, slash+1,
				   _WAPI_PROC_NAME_MAX_LEN - 1);
		} else {
			g_strlcpy (process_handle->proc_name, utf8_progname,
				   _WAPI_PROC_NAME_MAX_LEN - 1);
		}

		g_free (utf8_progname);
	}
}

extern void _wapi_time_t_to_filetime (time_t timeval, WapiFileTime *filetime);

#if !GLIB_CHECK_VERSION (2,4,0)
#define g_setenv(a,b,c) setenv(a,b,c)
#define g_unsetenv(a) unsetenv(a)
#endif

static void process_set_current (void)
{
	pid_t pid = _wapi_getpid ();
	const char *handle_env;
	struct _WapiHandle_process process_handle = {0};
	
	mono_once (&process_ops_once, process_ops_init);
	
	handle_env = g_getenv ("_WAPI_PROCESS_HANDLE_OFFSET");
	g_unsetenv ("_WAPI_PROCESS_HANDLE_OFFSET");
	
	if (handle_env != NULL) {
		struct _WapiHandle_process *process_handlep;
		gchar *procname = NULL;
		gboolean ok;
		
		current_process = _wapi_handle_new_from_offset (WAPI_HANDLE_PROCESS, atoi (handle_env), TRUE);
		
		DEBUG ("%s: Found my process handle: %p (offset %d 0x%x)",
			   __func__, current_process, atoi (handle_env),
			   atoi (handle_env));

		ok = _wapi_lookup_handle (current_process, WAPI_HANDLE_PROCESS,
					  (gpointer *)&process_handlep);
		if (ok) {
			/* This test will probably break on linuxthreads, but
			 * that should be ancient history on all distros we
			 * care about by now
			 */
			if (process_handlep->id == pid) {
				procname = process_handlep->proc_name;
				if (!strcmp (procname, "mono")) {
					/* Set a better process name */
					DEBUG ("%s: Setting better process name", __func__);
					
					process_set_name (process_handlep);
				} else {
					DEBUG ("%s: Leaving process name: %s", __func__, procname);
				}

				return;
			}

			/* Wrong pid, so drop this handle and fall through to
			 * create a new one
			 */
			_wapi_handle_unref (current_process);
		}
	}

	/* We get here if the handle wasn't specified in the
	 * environment, or if the process ID was wrong, or if the
	 * handle lookup failed (eg if the parent process forked and
	 * quit immediately, and deleted the shared data before the
	 * child got a chance to attach it.)
	 */

	DEBUG ("%s: Need to create my own process handle", __func__);

	process_handle.id = pid;

	process_set_defaults (&process_handle);
	process_set_name (&process_handle);

	current_process = _wapi_handle_new (WAPI_HANDLE_PROCESS,
					    &process_handle);
	if (current_process == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating process handle", __func__);
		return;
	}
}

gpointer _wapi_process_duplicate ()
{
	mono_once (&process_current_once, process_set_current);
	
	_wapi_handle_ref (current_process);
	
	return(current_process);
}

/* Returns a pseudo handle that doesn't need to be closed afterwards */
gpointer GetCurrentProcess (void)
{
	mono_once (&process_current_once, process_set_current);
		
	return(_WAPI_PROCESS_CURRENT);
}

guint32 GetProcessId (gpointer handle)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;

	if ((GPOINTER_TO_UINT (handle) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		return(GPOINTER_TO_UINT (handle) & _WAPI_PROCESS_UNHANDLED_PID_MASK);
	}
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				  (gpointer *)&process_handle);
	if (ok == FALSE) {
		SetLastError (ERROR_INVALID_HANDLE);
		return (0);
	}
	
	return (process_handle->id);
}

guint32 GetCurrentProcessId (void)
{
	mono_once (&process_current_once, process_set_current);
		
	return (GetProcessId (current_process));
}

/* Returns the process id as a convenience to the functions that call this */
static pid_t signal_process_if_gone (gpointer handle)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	
	g_assert ((GPOINTER_TO_UINT (handle) & _WAPI_PROCESS_UNHANDLED) != _WAPI_PROCESS_UNHANDLED);
	
	/* Make sure the process is signalled if it has exited - if
	 * the parent process didn't wait for it then it won't be
	 */
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				  (gpointer *)&process_handle);
	if (ok == FALSE) {
		/* It's possible that the handle has vanished during
		 * the _wapi_search_handle before it gets here, so
		 * don't spam the console with warnings.
		 */
/*		g_warning ("%s: error looking up process handle %p",
  __func__, handle);*/
		
		return (0);
	}
	
	DEBUG ("%s: looking at process %d", __func__, process_handle->id);

	if (kill (process_handle->id, 0) == -1 &&
	    (errno == ESRCH ||
	     errno == EPERM)) {
		/* The process is dead, (EPERM tells us a new process
		 * has that ID, but as it's owned by someone else it
		 * can't be the one listed in our shared memory file)
		 */
		_wapi_shared_handle_set_signal_state (handle, TRUE);
	}

	return (process_handle->id);
}

#ifdef UNUSED_CODE
static gboolean process_enum (gpointer handle, gpointer user_data)
{
	GArray *processes=user_data;
	pid_t pid = signal_process_if_gone (handle);
	int i;
	
	if (pid == 0) {
		return (FALSE);
	}
	
	/* Ignore processes that have already exited (ie they are signalled) */
	if (_wapi_handle_issignalled (handle) == FALSE) {
		DEBUG ("%s: process %d added to array", __func__, pid);

		/* This ensures that duplicates aren't returned (see
		 * the comment above _wapi_search_handle () for why
		 * it's needed
		 */
		for (i = 0; i < processes->len; i++) {
			if (g_array_index (processes, pid_t, i) == pid) {
				/* We've already got this one, return
				 * FALSE to keep searching
				 */
				return (FALSE);
			}
		}
		
		g_array_append_val (processes, pid);
	}
	
	/* Return false to keep searching */
	return(FALSE);
}
#endif /* UNUSED_CODE */

#if defined(PLATFORM_MACOSX) || defined(__OpenBSD__)

gboolean EnumProcesses (guint32 *pids, guint32 len, guint32 *needed)
{
	guint32 count, fit, i, j;
	gint32 err;
	gboolean done;
	size_t proclength, size;
#if defined(__OpenBSD__)
	struct kinfo_proc2 *result;
	int name[6];
	name[0] = CTL_KERN;
	name[1] = KERN_PROC2;
	name[2] = KERN_PROC_ALL;
	name[3] = 0;
	name[4] = sizeof(struct kinfo_proc2);
	name[5] = 0;
#else
	struct kinfo_proc *result;
	static const int name[] = { CTL_KERN, KERN_PROC, KERN_PROC_ALL, 0 };
#endif

	mono_once (&process_current_once, process_set_current);

	result = NULL;
	done = FALSE;

	do {
		proclength = 0;
#if defined(__OpenBSD__)
		size = (sizeof(name) / sizeof(*name));
#else
		size = (sizeof(name) / sizeof(*name)) - 1;
#endif
		err = sysctl ((int *)name, size, NULL, &proclength, NULL, 0);

		if (err == 0) {
			result = malloc (proclength);

			if (result == NULL)
				return FALSE;

#if defined(__OpenBSD__)
			name[5] = (int)(proclength / sizeof(struct kinfo_proc2));
#endif

			err = sysctl ((int *) name, size, result, &proclength, NULL, 0);

			if (err == 0) 
				done = TRUE;
			else
				free (result);
		}
	} while (err == 0 && !done);
	
	if (err != 0) {
 		if (result != NULL) {
			free (result);
			result = NULL;
		}
		return(FALSE);
	}	

#if defined(__OpenBSD__)
	count = proclength / sizeof(struct kinfo_proc2);
#else
	count = proclength / sizeof(struct kinfo_proc);
#endif
	fit = len / sizeof(guint32);
	for (i = 0, j = 0; j< fit && i < count; i++) {
#if defined(__OpenBSD__)
		pids [j++] = result [i].p_pid;
#else
		if (result[i].kp_proc.p_pid > 0) /* Pid 0 not supported */
			pids [j++] = result [i].kp_proc.p_pid;
#endif
	}
	free (result);
	result = NULL;
	*needed = j * sizeof(guint32);
	
	return(TRUE);
}
#elif defined(__HAIKU__)

gboolean EnumProcesses (guint32 *pids, guint32 len, guint32 *needed)
{
	guint32 fit, i = 0;
	int32 cookie = 0;
	team_info teamInfo;

	mono_once (&process_current_once, process_set_current);

	fit = len / sizeof (guint32);
	while (get_next_team_info (&cookie, &teamInfo) == B_OK && i < fit) {
		pids [i++] = teamInfo.team;
	}
	*needed = i * sizeof (guint32);

	return TRUE;
}
#else
gboolean EnumProcesses (guint32 *pids, guint32 len, guint32 *needed)
{
	guint32 fit, i;
	DIR *dir;
	struct dirent *entry;
	
	mono_once (&process_current_once, process_set_current);

	dir = opendir ("/proc");
	if (dir == NULL) {
		return(FALSE);
	}

	i = 0;
	fit = len / sizeof (guint32);
	while(i < fit && (entry = readdir (dir)) != NULL) {
		pid_t pid;
		char *endptr;

		if (!isdigit (entry->d_name[0]))
			continue;

		pid = (pid_t) strtol (entry->d_name, &endptr, 10);
		if (*endptr == '\0')
			pids [i++] = (guint32) pid;
	}
	closedir (dir);
	*needed = i * sizeof(guint32);
	
	return(TRUE);
}
#endif

static gboolean process_open_compare (gpointer handle, gpointer user_data)
{
	pid_t wanted_pid;
	pid_t checking_pid = signal_process_if_gone (handle);

	if (checking_pid == 0) {
		return(FALSE);
	}
	
	wanted_pid = GPOINTER_TO_UINT (user_data);

	/* It's possible to have more than one process handle with the
	 * same pid, but only the one running process can be
	 * unsignalled
	 */
	if (checking_pid == wanted_pid &&
	    _wapi_handle_issignalled (handle) == FALSE) {
		/* If the handle is blown away in the window between
		 * returning TRUE here and _wapi_search_handle pinging
		 * the timestamp, the search will continue
		 */
		return(TRUE);
	} else {
		return(FALSE);
	}
}

gboolean CloseProcess(gpointer handle)
{
	if ((GPOINTER_TO_UINT (handle) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		return(TRUE);
	}

	return CloseHandle (handle);
}

/*
 * The caller owns the returned handle and must call CloseProcess () on it to clean it up.
 */
gpointer OpenProcess (guint32 req_access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, guint32 pid)
{
	/* Find the process handle that corresponds to pid */
	gpointer handle = NULL;
	
	mono_once (&process_current_once, process_set_current);

	DEBUG ("%s: looking for process %d", __func__, pid);

	handle = _wapi_search_handle (WAPI_HANDLE_PROCESS,
				      process_open_compare,
				      GUINT_TO_POINTER (pid), NULL, TRUE);
	if (handle == 0) {
		if (is_pid_valid (pid)) {
			/* Return a pseudo handle for processes we
			 * don't have handles for
			 */
			return GINT_TO_POINTER (_WAPI_PROCESS_UNHANDLED + pid);
		} else {
			DEBUG ("%s: Can't find pid %d", __func__, pid);

			SetLastError (ERROR_PROC_NOT_FOUND);
	
			return(NULL);
		}
	}

	/* _wapi_search_handle () already added a ref */
	return(handle);
}

gboolean GetExitCodeProcess (gpointer process, guint32 *code)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	guint32 pid = -1;
	
	mono_once (&process_current_once, process_set_current);

	if(code==NULL) {
		return(FALSE);
	}
	
	pid = GPOINTER_TO_UINT (process) - _WAPI_PROCESS_UNHANDLED;
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
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

	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle);
	if(ok==FALSE) {
		DEBUG ("%s: Can't find process %p", __func__, process);
		
		return(FALSE);
	}
	
	/* A process handle is only signalled if the process has exited
	 * and has been waited for */

	/* Make sure any process exit has been noticed, before
	 * checking if the process is signalled.  Fixes bug 325463.
	 */
	process_wait (process, 0, TRUE);
	
	if (_wapi_handle_issignalled (process) == TRUE) {
		*code = process_handle->exitstatus;
	} else {
		*code = STILL_ACTIVE;
	}
	
	return(TRUE);
}

gboolean GetProcessTimes (gpointer process, WapiFileTime *create_time,
			  WapiFileTime *exit_time, WapiFileTime *kernel_time,
			  WapiFileTime *user_time)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	gboolean ku_times_set = FALSE;
	
	mono_once (&process_current_once, process_set_current);

	if(create_time==NULL || exit_time==NULL || kernel_time==NULL ||
	   user_time==NULL) {
		/* Not sure if w32 allows NULLs here or not */
		return(FALSE);
	}
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle, so just fail for now
		 */
		return(FALSE);
	}
	
	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle);
	if(ok==FALSE) {
		DEBUG ("%s: Can't find process %p", __func__, process);
		
		return(FALSE);
	}
	
	*create_time=process_handle->create_time;

	/* A process handle is only signalled if the process has
	 * exited.  Otherwise exit_time isn't set
	 */
	if(_wapi_handle_issignalled (process)==TRUE) {
		*exit_time=process_handle->exit_time;
	}

#ifdef HAVE_GETRUSAGE
	if (process_handle->id == getpid ()) {
		struct rusage time_data;
		if (getrusage (RUSAGE_SELF, &time_data) == 0) {
			gint64 tick_val;
			gint64 *tick_val_ptr;
			ku_times_set = TRUE;
			tick_val = time_data.ru_utime.tv_sec * 10000000 + time_data.ru_utime.tv_usec * 10;
			tick_val_ptr = (gint64*)user_time;
			*tick_val_ptr = tick_val;
			tick_val = time_data.ru_stime.tv_sec * 10000000 + time_data.ru_stime.tv_usec * 10;
			tick_val_ptr = (gint64*)kernel_time;
			*tick_val_ptr = tick_val;
		}
	}
#endif
	if (!ku_times_set) {
		memset (kernel_time, 0, sizeof (WapiFileTime));
		memset (user_time, 0, sizeof (WapiFileTime));
	}

	return(TRUE);
}

typedef struct
{
	gpointer address_start;
	gpointer address_end;
	gchar *perms;
	gpointer address_offset;
	dev_t device;
	ino_t inode;
	gchar *filename;
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

#ifdef PLATFORM_MACOSX
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
		intptr_t slide;

		slide = _dyld_get_image_vmaddr_slide (i);
		name = _dyld_get_image_name (i);
		hdr = _dyld_get_image_header (i);
#if SIZEOF_VOID_P == 8
		sec = getsectbynamefromheader_64 (hdr, SEG_DATA, SECT_DATA);
#else
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
		mod->inode = (ino_t) i;
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
#elif defined(__OpenBSD__)
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
		mod->inode = (ino_t) i;
		mod->filename = g_strdup (info->dlpi_name); 

		DEBUG ("%s: inode=%d, filename=%s, address_start=%p, address_end=%p", __func__,
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
#elif defined(__HAIKU__)

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
	gchar buf[MAXPATHLEN + 1], *p, *endp;
	gchar *start_start, *end_start, *prot_start, *offset_start;
	gchar *maj_dev_start, *min_dev_start, *inode_start, prot_buf[5];
	gpointer address_start, address_end, address_offset;
	guint32 maj_dev, min_dev;
	ino_t inode;
	dev_t device;
	
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
		inode = (ino_t)strtol (inode_start, &endp, 10);
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

static gboolean match_procname_to_modulename (gchar *procname, gchar *modulename)
{
	char* lastsep = NULL;
	char* lastsep2 = NULL;
	char* pname = NULL;
	char* mname = NULL;
	gboolean result = FALSE;

	if (procname == NULL || modulename == NULL)
		return (FALSE);

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

	return result;
}

#if !defined(__OpenBSD__)
static FILE *
open_process_map (int pid, const char *mode)
{
	FILE *fp = NULL;
	const gchar *proc_path[] = {
		"/proc/%d/maps",	/* GNU/Linux */
		"/proc/%d/map",		/* FreeBSD */
		NULL
	};
	int i;
	gchar *filename;

	for (i = 0; fp == NULL && proc_path [i]; i++) {
 		filename = g_strdup_printf (proc_path[i], pid);
		fp = fopen (filename, mode);
		g_free (filename);
	}

	return fp;
}
#endif

gboolean EnumProcessModules (gpointer process, gpointer *modules,
			     guint32 size, guint32 *needed)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
#if !defined(__OpenBSD__) && !defined(PLATFORM_MACOSX)
	FILE *fp;
#endif
	GSList *mods = NULL;
	WapiProcModule *module;
	guint32 count, avail = size / sizeof(gpointer);
	int i;
	pid_t pid;
	gchar *proc_name = NULL;
	
	/* Store modules in an array of pointers (main module as
	 * modules[0]), using the load address for each module as a
	 * token.  (Use 'NULL' as an alternative for the main module
	 * so that the simple implementation can just return one item
	 * for now.)  Get the info from /proc/<pid>/maps on linux,
	 * /proc/<pid>/map on FreeBSD, other systems will have to
	 * implement /dev/kmem reading or whatever other horrid
	 * technique is needed.
	 */
	if (size < sizeof(gpointer)) {
		return(FALSE);
	}

	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		pid = (pid_t)(GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED_PID_MASK);
	} else {
		ok = _wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
					  (gpointer *)&process_handle);
		if (ok == FALSE) {
			DEBUG ("%s: Can't find process %p", __func__, process);
		
			return(FALSE);
		}
		pid = process_handle->id;
		proc_name = process_handle->proc_name;
	}
	
#if defined(PLATFORM_MACOSX) || defined(__OpenBSD__) || defined(__HAIKU__)
	{
		mods = load_modules ();
#else
	if ((fp = open_process_map (pid, "r")) == NULL) {
		/* No /proc/<pid>/maps so just return the main module
		 * shortcut for now
		 */
		modules[0] = NULL;
		*needed = sizeof(gpointer);
	} else {
		mods = load_modules (fp);
		fclose (fp);
#endif
		count = g_slist_length (mods);
		
		/* count + 1 to leave slot 0 for the main module */
		*needed = sizeof(gpointer) * (count + 1);

		/* Use the NULL shortcut, as the first line in
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
			free_procmodule (g_slist_nth_data (mods, i));
		}
		g_slist_free (mods);
	}

	return(TRUE);
}

static gchar *get_process_name_from_proc (pid_t pid)
{
#if defined(__OpenBSD__)
	int mib [6];
	size_t size;
	struct kinfo_proc2 *pi;
#elif defined(PLATFORM_MACOSX)
	size_t size;
	struct kinfo_proc *pi;
	int mib[] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, pid };
#else
	FILE *fp;
	gchar *filename = NULL;
#endif
	gchar buf[256];
	gchar *ret = NULL;

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
#elif defined(PLATFORM_MACOSX)
#if (MAC_OS_X_VERSION_MIN_REQUIRED >= MAC_OS_X_VERSION_10_5) && !defined (__mono_ppc__) && !defined(__arm__)
	/* No proc name on OSX < 10.5 nor ppc nor iOS */
	memset (buf, '\0', sizeof(buf));
	proc_name (pid, buf, sizeof(buf));
	if (strlen (buf) > 0)
		ret = g_strdup (buf);
#else
	if (sysctl(mib, 4, NULL, &size, NULL, 0) < 0)
		return(ret);

	if ((pi = malloc(size)) == NULL)
		return(ret);

	if (sysctl (mib, 4, pi, &size, NULL, 0) < 0) {
		if (errno == ENOMEM) {
			free(pi);
			DEBUG ("%s: Didn't allocate enough memory for kproc info", __func__);
		}
		return(ret);
	}

	if (strlen (pi->kp_proc.p_comm) > 0)
		ret = g_strdup (pi->kp_proc.p_comm);

	free(pi);
#endif
#elif defined(__OpenBSD__)
	mib [0] = CTL_KERN;
	mib [1] = KERN_PROC2;
	mib [2] = KERN_PROC_PID;
	mib [3] = pid;
	mib [4] = sizeof(struct kinfo_proc2);
	mib [5] = 0;

retry:
	if (sysctl(mib, 6, NULL, &size, NULL, 0) < 0)
		return(ret);

	if ((pi = malloc(size)) == NULL)
		return(ret);

	mib[5] = (int)(size / sizeof(struct kinfo_proc2));

	if ((sysctl (mib, 6, pi, &size, NULL, 0) < 0) ||
		(size != sizeof (struct kinfo_proc2))) {
		if (errno == ENOMEM) {
			free(pi);
			goto retry;
		}
		return(ret);
	}

	if (strlen (pi->p_comm) > 0)
		ret = g_strdup (pi->p_comm);

	free(pi);
#elif defined(__HAIKU__)
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
			gchar *start, *end;
			
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

static guint32 get_module_name (gpointer process, gpointer module,
				gunichar2 *basename, guint32 size,
				gboolean base)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	pid_t pid;
	gunichar2 *procname;
	gchar *procname_ext = NULL;
	glong len;
	gsize bytes;
#if !defined(__OpenBSD__) && !defined(PLATFORM_MACOSX)
	FILE *fp;
#endif
	GSList *mods = NULL;
	WapiProcModule *found_module;
	guint32 count;
	int i;
	gchar *proc_name = NULL;
	
	mono_once (&process_current_once, process_set_current);

	DEBUG ("%s: Getting module base name, process handle %p module %p",
		   __func__, process, module);

	size = size*sizeof(gunichar2); /* adjust for unicode characters */

	if (basename == NULL || size == 0) {
		return(0);
	}
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		pid = (pid_t)(GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED_PID_MASK);
		proc_name = get_process_name_from_proc (pid);
	} else {
		ok = _wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
					  (gpointer *)&process_handle);
		if (ok == FALSE) {
			DEBUG ("%s: Can't find process %p", __func__,
				   process);
			
			return(0);
		}
		pid = process_handle->id;
		proc_name = g_strdup (process_handle->proc_name);
	}

	/* Look up the address in /proc/<pid>/maps */
#if defined(PLATFORM_MACOSX) || defined(__OpenBSD__) || defined(__HAIKU__)
	{
		mods = load_modules ();
#else
	if ((fp = open_process_map (pid, "r")) == NULL) {
		if (errno == EACCES && module == NULL && base == TRUE) {
			procname_ext = get_process_name_from_proc (pid);
		} else {
			/* No /proc/<pid>/maps, so just return failure
			 * for now
			 */
			g_free (proc_name);
			return(0);
		}
	} else {
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
			if (procname_ext == NULL &&
			    ((module == NULL && match_procname_to_modulename (proc_name, found_module->filename)) ||	
			     (module != NULL && found_module->address_start == module))) {
				if (base) {
					procname_ext = g_path_get_basename (found_module->filename);
				} else {
					procname_ext = g_strdup (found_module->filename);
				}
			}

			free_procmodule (found_module);
		}

		if (procname_ext == NULL)
		{
			/* If it's *still* null, we might have hit the
			 * case where reading /proc/$pid/maps gives an
			 * empty file for this user.
			 */
			procname_ext = get_process_name_from_proc (pid);
		}

		g_slist_free (mods);
		g_free (proc_name);
	}

	if (procname_ext != NULL) {
		DEBUG ("%s: Process name is [%s]", __func__,
			   procname_ext);

		procname = mono_unicode_from_external (procname_ext, &bytes);
		if (procname == NULL) {
			/* bugger */
			g_free (procname_ext);
			return(0);
		}
		
		len = (bytes / 2);
		
		/* Add the terminator */
		bytes += 2;
		
		if (size < bytes) {
			DEBUG ("%s: Size %d smaller than needed (%ld); truncating", __func__, size, bytes);

			memcpy (basename, procname, size);
		} else {
			DEBUG ("%s: Size %d larger than needed (%ld)",
				   __func__, size, bytes);

			memcpy (basename, procname, bytes);
		}
		
		g_free (procname);
		g_free (procname_ext);
		
		return(len);
	}
	
	return(0);
}

guint32 GetModuleBaseName (gpointer process, gpointer module,
			   gunichar2 *basename, guint32 size)
{
	return(get_module_name (process, module, basename, size, TRUE));
}

guint32 GetModuleFileNameEx (gpointer process, gpointer module,
			     gunichar2 *filename, guint32 size)
{
	return(get_module_name (process, module, filename, size, FALSE));
}

gboolean GetModuleInformation (gpointer process, gpointer module,
			       WapiModuleInfo *modinfo, guint32 size)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	pid_t pid;
#if !defined(__OpenBSD__) && !defined(PLATFORM_MACOSX)
	FILE *fp;
#endif
	GSList *mods = NULL;
	WapiProcModule *found_module;
	guint32 count;
	int i;
	gboolean ret = FALSE;
	gchar *proc_name = NULL;
	
	mono_once (&process_current_once, process_set_current);
	
	DEBUG ("%s: Getting module info, process handle %p module %p",
		   __func__, process, module);

	if (modinfo == NULL || size < sizeof(WapiModuleInfo)) {
		return(FALSE);
	}
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		pid = (pid_t)(GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED_PID_MASK);
		proc_name = get_process_name_from_proc (pid);
	} else {
		ok = _wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
					  (gpointer *)&process_handle);
		if (ok == FALSE) {
			DEBUG ("%s: Can't find process %p", __func__,
				   process);
			
			return(FALSE);
		}
		pid = process_handle->id;
		proc_name = g_strdup (process_handle->proc_name);
	}

#if defined(PLATFORM_MACOSX) || defined(__OpenBSD__) || defined(__HAIKU__)
	{
		mods = load_modules ();
#else
	/* Look up the address in /proc/<pid>/maps */
	if ((fp = open_process_map (pid, "r")) == NULL) {
		/* No /proc/<pid>/maps, so just return failure
		 * for now
		 */
		g_free (proc_name);
		return(FALSE);
	} else {
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
			if ( ret == FALSE &&
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
	}

	return(ret);
}

gboolean GetProcessWorkingSetSize (gpointer process, size_t *min, size_t *max)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	
	mono_once (&process_current_once, process_set_current);

	if(min==NULL || max==NULL) {
		/* Not sure if w32 allows NULLs here or not */
		return(FALSE);
	}
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle, so just fail for now
		 */
		return(FALSE);
	}
	
	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle);
	if(ok==FALSE) {
		DEBUG ("%s: Can't find process %p", __func__, process);
		
		return(FALSE);
	}

	*min=process_handle->min_working_set;
	*max=process_handle->max_working_set;
	
	return(TRUE);
}

gboolean SetProcessWorkingSetSize (gpointer process, size_t min, size_t max)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;

	mono_once (&process_current_once, process_set_current);
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle, so just fail for now
		 */
		return(FALSE);
	}

	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle);
	if(ok==FALSE) {
		DEBUG ("%s: Can't find process %p", __func__, process);
		
		return(FALSE);
	}

	process_handle->min_working_set=min;
	process_handle->max_working_set=max;
	
	return(TRUE);
}


gboolean
TerminateProcess (gpointer process, gint32 exitCode)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	int signo;
	int ret;
	pid_t pid;
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		pid = (pid_t)(GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED_PID_MASK);
	} else {
		ok = _wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
					  (gpointer *) &process_handle);

		if (ok == FALSE) {
			DEBUG ("%s: Can't find process %p", __func__,
				   process);
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
}

guint32
GetPriorityClass (gpointer process)
{
#ifdef HAVE_GETPRIORITY
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	int ret;
	pid_t pid;
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		pid = (pid_t)(GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED_PID_MASK);
	} else {
		ok = _wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
					  (gpointer *) &process_handle);

		if (!ok) {
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
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	int ret;
	int prio;
	pid_t pid;
	
	if ((GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED) {
		/* This is a pseudo handle */
		pid = (pid_t)(GPOINTER_TO_UINT (process) & _WAPI_PROCESS_UNHANDLED_PID_MASK);
	} else {
		ok = _wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
					  (gpointer *) &process_handle);

		if (!ok) {
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
	struct MonoProcess *candidate = NULL;
	gpointer unref_handle;
	int spin;

	DEBUG ("%s", __func__);

	/* Ensure we're not in here in multiple threads at once, nor recursive. */
	if (InterlockedCompareExchange (&mono_processes_cleaning_up, 1, 0) != 0)
		return;

	mp = mono_processes;
	while (mp != NULL) {
		if (mp->pid == 0 && mp->handle != NULL) {
			/* This process has exited and we need to remove the artifical ref
			 * on the handle */
			mono_mutex_lock (&mono_processes_mutex);
			unref_handle = mp->handle;
			mp->handle = NULL;
			mono_mutex_unlock (&mono_processes_mutex);
			if (unref_handle)
				_wapi_handle_unref (unref_handle);
			continue;
		}
		mp = mp->next;
	}

	mp = mono_processes;
	spin = 0;
	while (mp != NULL) {
		if ((mp->handle_count == 0 && mp->pid == 0) || candidate != NULL) {
			if (spin > 0) {
				_wapi_handle_spin (spin);
				spin <<= 1;
			}

			/* We've found a candidate */
			mono_mutex_lock (&mono_processes_mutex);
			if (candidate == NULL) {
				/* unlink it */
				if (mp == mono_processes) {
					mono_processes = mp->next;
				} else {
					prev->next = mp->next;
				}
				candidate = mp;
			}

			/* It's still safe to traverse the structure.*/
			mono_memory_barrier ();

			if (mono_processes_read_lock != 0) {
				/* The sigchld handler is watching us. Spin a bit and try again */
				if (spin == 0) {
					spin = 1;
				} else if (spin >= 8) {
					/* Just give up for now */
					mono_mutex_unlock (&mono_processes_mutex);
					break;
				}
			} else {
				/* We've modified the list of processes, and we know the sigchld handler
				 * isn't executing, so even if it executes at any moment, it'll see the
				 * new version of the list. So now we can free the candidate. */
				DEBUG ("%s: freeing candidate %p", __func__, candidate);
				mp = candidate->next;
				MONO_SEM_DESTROY (&candidate->exit_sem);
				g_free (candidate);
				candidate = NULL;
			}

			mono_mutex_unlock (&mono_processes_mutex);

			continue;
		}
		spin = 0;
		prev = mp;
		mp = mp->next;
	}

	DEBUG ("%s done", __func__);

	InterlockedDecrement (&mono_processes_cleaning_up);
}

static void
process_close (gpointer handle, gpointer data)
{
	struct _WapiHandle_process *process_handle;

	DEBUG ("%s", __func__);

	process_handle = (struct _WapiHandle_process *) data;
	if (process_handle->mono_process && process_handle->self == _wapi_getpid ())
		InterlockedDecrement (&process_handle->mono_process->handle_count);
	mono_processes_cleanup ();
}

#if HAVE_SIGACTION
static void
mono_sigchld_signal_handler (int _dummy, siginfo_t *info, void *context)
{
	int status;
	int pid;
	struct MonoProcess *p;

#if DEBUG	
	fprintf (stdout, "SIG CHILD handler for pid: %i\n", info->si_pid);
#endif

	InterlockedIncrement (&mono_processes_read_lock);

	do {
		do {
			pid = waitpid (-1, &status, WNOHANG);
		} while (pid == -1 && errno == EINTR);

		if (pid <= 0)
			break;

#if DEBUG
		fprintf (stdout, "child ended: %i", pid);
#endif
		p = mono_processes;
		while (p != NULL) {
			if (p->pid == pid) {
				p->pid = 0; /* this pid doesn't exist anymore, clear it */
				p->status = status;
				MONO_SEM_POST (&p->exit_sem);
				break;
			}
			p = p->next;
		}
	} while (1);

	InterlockedDecrement (&mono_processes_read_lock);

#if DEBUG
	fprintf (stdout, "SIG CHILD handler: done looping.");
#endif
}
#endif

static void process_add_sigchld_handler (void)
{
#if HAVE_SIGACTION
	struct sigaction sa;

	sa.sa_sigaction = mono_sigchld_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_NOCLDSTOP | SA_SIGINFO;
	g_assert (sigaction (SIGCHLD, &sa, &previous_chld_sa) != -1);
	DEBUG ("Added SIGCHLD handler");
#endif
}

static guint32 process_wait (gpointer handle, guint32 timeout, gboolean alertable)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	pid_t pid, ret;
	int status;
	guint32 start;
	guint32 now;
	struct MonoProcess *mp;
	gboolean spin;
	gpointer current_thread;

	current_thread = _wapi_thread_handle_from_id (pthread_self ());
	if (current_thread == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return WAIT_FAILED;
	}

	/* FIXME: We can now easily wait on processes that aren't our own children,
	 * but WaitFor*Object won't call us for pseudo handles. */
	g_assert ((GPOINTER_TO_UINT (handle) & _WAPI_PROCESS_UNHANDLED) != _WAPI_PROCESS_UNHANDLED);

	DEBUG ("%s (%p, %u)", __func__, handle, timeout);

	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS, (gpointer *)&process_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up process handle %p", __func__, handle);
		return WAIT_FAILED;
	}

	if (process_handle->exited) {
		/* We've already done this one */
		DEBUG ("%s (%p, %u): Process already exited", __func__, handle, timeout);
		return WAIT_OBJECT_0;
	}

	pid = process_handle->id;

	DEBUG ("%s (%p, %u): PID: %d", __func__, handle, timeout, pid);

	/* We don't need to lock mono_processes here, the entry
	 * has a handle_count > 0 which means it will not be freed. */
	mp = process_handle->mono_process;
	if (mp && process_handle->self != _wapi_getpid ()) {
		/* mono_process points to memory in another process' address space: we can't use it */
		mp = NULL;
	}

	start = mono_msec_ticks ();
	now = start;
	spin = mp == NULL;

	while (1) {
		if (mp != NULL) {
			/* We have a semaphore we can wait on */
			if (timeout != INFINITE) {
				DEBUG ("%s (%p, %u): waiting on semaphore for %li ms...", 
					__func__, handle, timeout, (timeout - (now - start)));

				ret = MONO_SEM_TIMEDWAIT_ALERTABLE (&mp->exit_sem, (timeout - (now - start)), alertable);
			} else {
				DEBUG ("%s (%p, %u): waiting on semaphore forever...", 
					__func__, handle, timeout);
				ret = MONO_SEM_WAIT_ALERTABLE (&mp->exit_sem, alertable);
			}

			if (ret == -1 && errno != EINTR && errno != ETIMEDOUT) {
				DEBUG ("%s (%p, %u): sem_timedwait failure: %s", 
					__func__, handle, timeout, g_strerror (errno));
				/* Should we return a failure here? */
			}

			if (ret == 0) {
				/* Success, process has exited */
				MONO_SEM_POST (&mp->exit_sem);
				break;
			}
		} else {
			/* We did not create this process, so we can't waidpid / sem_wait it.
			 * We need to poll for the pid existence */
			DEBUG ("%s (%p, %u): polling on pid...", __func__, handle, timeout);
			if (!is_pid_valid (pid)) {
				/* Success, process has exited */
				break;
			}
		}

		if (timeout == 0) {
			DEBUG ("%s (%p, %u): WAIT_TIMEOUT (timeout = 0)", __func__, handle, timeout);
			return WAIT_TIMEOUT;
		}

		now = mono_msec_ticks ();
		if (now - start >= timeout) {
			DEBUG ("%s (%p, %u): WAIT_TIMEOUT", __func__, handle, timeout);
			return WAIT_TIMEOUT;
		}

		if (spin) {
			/* "timeout - (now - start)" will not underflow, since timeout is always >=0,
			 * and we passed the check just above */
			_wapi_handle_spin (MIN (100, timeout - (now - start)));
		}
		
		if (alertable && _wapi_thread_apc_pending (current_thread)) {
			DEBUG ("%s (%p, %u): WAIT_IO_COMPLETION", __func__, handle, timeout);
			return WAIT_IO_COMPLETION;
		}
	}

	/* Process must have exited */
	DEBUG ("%s (%p, %u): Waited successfully", __func__, handle, timeout);

	ret = _wapi_handle_lock_shared_handles ();
	g_assert (ret == 0);

	status = mp ? mp->status : 0;
	if (WIFSIGNALED (status)) {
		process_handle->exitstatus = 128 + WTERMSIG (status);
	} else {
		process_handle->exitstatus = WEXITSTATUS (status);
	}
	_wapi_time_t_to_filetime (time (NULL), &process_handle->exit_time);

	process_handle->exited = TRUE;

	DEBUG ("%s (%p, %u): Setting pid %d signalled, exit status %d",
		   __func__, handle, timeout, process_handle->id, process_handle->exitstatus);

	_wapi_shared_handle_set_signal_state (handle, TRUE);

	_wapi_handle_unlock_shared_handles ();

	return WAIT_OBJECT_0;
}

