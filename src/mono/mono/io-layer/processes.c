/*
 * processes.c:  Process handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#if HAVE_BOEHM_GC
#include <mono/os/gc_wrapper.h>
#include "mono/utils/mono-hash.h"
#endif
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <unistd.h>
#include <signal.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/process-private.h>
#include <mono/io-layer/threads.h>
#include <mono/utils/strenc.h>

/* The process' environment strings */
extern char **environ;

#undef DEBUG

static void process_close_shared (gpointer handle);

struct _WapiHandleOps _wapi_process_ops = {
	process_close_shared,		/* close_shared */
	NULL,				/* close_private */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
};

static mono_once_t process_current_once=MONO_ONCE_INIT;
static gpointer current_process=NULL;

static mono_once_t process_ops_once=MONO_ONCE_INIT;

static void process_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_PROCESS,
					    WAPI_HANDLE_CAP_WAIT);
}

static void process_close_shared (gpointer handle G_GNUC_UNUSED)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;

	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up process handle %p", handle);
		return;
	}

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": closing process handle %p with id %d", handle,
		   process_handle->id);
#endif

	if(process_handle->proc_name!=0) {
		_wapi_handle_scratch_delete (process_handle->proc_name);
		process_handle->proc_name=0;
	}
}

gboolean CreateProcess (const gunichar2 *appname, gunichar2 *cmdline,
			WapiSecurityAttributes *process_attrs G_GNUC_UNUSED,
			WapiSecurityAttributes *thread_attrs G_GNUC_UNUSED,
			gboolean inherit_handles, guint32 create_flags,
			gpointer new_environ, const gunichar2 *cwd,
			WapiStartupInfo *startup,
			WapiProcessInformation *process_info)
{
	gchar *cmd=NULL, *prog = NULL, *full_prog = NULL, *args=NULL, *args_after_prog=NULL, *dir=NULL;
	guint32 env=0, stored_dir=0, stored_prog=0, i;
	gboolean ret=FALSE;
	gpointer stdin_handle, stdout_handle, stderr_handle;
	guint32 pid, tid;
	gpointer process_handle, thread_handle;
	struct _WapiHandle_process *process_handle_data;
	
	mono_once (&process_ops_once, process_ops_init);
	
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
	if(appname!=NULL) {
		cmd=mono_unicode_to_external (appname);
		if(cmd==NULL) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": unicode conversion returned NULL");
#endif

			SetLastError(ERROR_PATH_NOT_FOUND);
			goto cleanup;
		}

		/* Turn all the slashes round the right way */
		for(i=0; i<strlen (cmd); i++) {
			if(cmd[i]=='\\') {
				cmd[i]='/';
			}
		}
	}
	
	if(cmdline!=NULL) {
		args=mono_unicode_to_external (cmdline);
		if(args==NULL) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": unicode conversion returned NULL");
#endif

			SetLastError(ERROR_PATH_NOT_FOUND);
			goto cleanup;
		}
	}

	if(cwd!=NULL) {
		dir=mono_unicode_to_external (cwd);
		if(dir==NULL) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": unicode conversion returned NULL");
#endif

			SetLastError(ERROR_PATH_NOT_FOUND);
			goto cleanup;
		}

		/* Turn all the slashes round the right way */
		for(i=0; i<strlen (dir); i++) {
			if(dir[i]=='\\') {
				dir[i]='/';
			}
		}
	} else {
		dir=g_get_current_dir ();
	}
	stored_dir=_wapi_handle_scratch_store (dir, strlen (dir));
	
	
	/* new_environ is a block of NULL-terminated strings, which
	 * is itself NULL-terminated. Of course, passing an array of
	 * string pointers would have made things too easy :-(
	 *
	 * If new_environ is not NULL it specifies the entire set of
	 * environment variables in the new process.  Otherwise the
	 * new process inherits the same environment.
	 */
	if(new_environ!=NULL) {
		gchar **strings;
		guint32 count=0;
		gunichar2 *new_environp;

		/* Count the number of strings */
		for(new_environp=(gunichar2 *)new_environ; *new_environp;
		    new_environp++) {
			count++;
			while(*new_environp) {
				new_environp++;
			}
		}
		strings=g_new0 (gchar *, count + 1); /* +1 -> last one is NULL */
		
		/* Copy each environ string into 'strings' turning it
		 * into utf8 (or the requested encoding) at the same
		 * time
		 */
		count=0;
		for(new_environp=(gunichar2 *)new_environ; *new_environp;
		    new_environp++) {
			strings[count]=mono_unicode_to_external (new_environp);
			count++;
			while(*new_environp) {
				new_environp++;
			}
		}

		env=_wapi_handle_scratch_store_string_array (strings);

		g_strfreev (strings);
	} else {
		/* Use the existing environment */
		env=_wapi_handle_scratch_store_string_array (environ);
	}

	/* We can't put off locating the executable any longer :-( */
	if(cmd!=NULL) {
		gchar *unquoted;
		if(g_ascii_isalpha (cmd[0]) && (cmd[1]==':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			g_memmove (cmd, cmd+2, strlen (cmd)-2);
			cmd[strlen (cmd)-2]='\0';
		}

		unquoted = g_shell_unquote (cmd, NULL);
		if(unquoted[0]=='/') {
			/* Assume full path given */
			prog=g_strdup (unquoted);

			/* Executable existing ? */
			if(access (prog, X_OK)!=0) {
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION ": Couldn't find executable %s", prog);
#endif
				g_free (prog);
				g_free (unquoted);
				SetLastError (ERROR_FILE_NOT_FOUND);
				goto cleanup;
			}
		} else {
			/* Search for file named by cmd in the current
			 * directory
			 */
			char *curdir=g_get_current_dir ();

			prog=g_strdup_printf ("%s/%s", curdir, unquoted);
			g_free (unquoted);
			g_free (curdir);
		}

		args_after_prog=args;
	} else {
		gchar *token=NULL;
		char quote;
		
		/* Dig out the first token from args, taking quotation
		 * marks into account
		 */

		/* First, strip off all leading whitespace */
		args=g_strchug (args);
		
		/* args_after_prog points to the contents of args
		 * after token has been set (otherwise argv[0] is
		 * duplicated)
		 */
		args_after_prog=args;

		/* Assume the opening quote will always be the first
		 * character
		 */
		if(args[0]=='\"' || args [0] == '\'') {
			quote = args [0];
			for(i=1; args[i]!='\0' && args[i]!=quote; i++);
			if(g_ascii_isspace (args[i+1])) {
				/* We found the first token */
				token=g_strndup (args+1, i-1);
				args_after_prog=args+i;
			} else {
				/* Quotation mark appeared in the
				 * middle of the token.  Just give the
				 * whole first token, quotes and all,
				 * to exec.
				 */
			}
		}
		
		if(token==NULL) {
			/* No quote mark, or malformed */
			for(i=0; args[i]!='\0'; i++) {
				if(g_ascii_isspace (args[i])) {
					token=g_strndup (args, i);
					args_after_prog=args+i+1;
					break;
				}
			}
		}

		if(token==NULL && args[0]!='\0') {
			/* Must be just one token in the string */
			token=g_strdup (args);
			args_after_prog=NULL;
		}
		
		if(token==NULL) {
			/* Give up */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Couldn't find what to exec");
#endif

			SetLastError(ERROR_PATH_NOT_FOUND);
			goto cleanup;
		}
		
		/* Turn all the slashes round the right way. Only for the prg. name */
		for(i=0; i < strlen (token); i++) {
			if (token[i]=='\\') {
				token[i]='/';
			}
		}

		if(g_ascii_isalpha (token[0]) && (token[1]==':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			g_memmove (token, token+2, strlen (token)-2);
			token[strlen (token)-2]='\0';
		}

		if(token[0]=='/') {
			/* Assume full path given */
			prog=g_strdup (token);
			
			/* Executable existing ? */
			if(access (prog, X_OK)!=0) {
				g_free (prog);
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION ": Couldn't find executable %s", token);
#endif
				g_free (token);
				SetLastError (ERROR_FILE_NOT_FOUND);
				goto cleanup;
			}

		} else {
			char *curdir=g_get_current_dir ();

			/* FIXME: Need to record the directory
			 * containing the current process, and check
			 * that for the new executable as the first
			 * place to look
			 */

			prog=g_strdup_printf ("%s/%s", curdir, token);
			g_free (curdir);

			/* I assume X_OK is the criterion to use,
			 * rather than F_OK
			 */
			if(access (prog, X_OK)!=0) {
				g_free (prog);
				prog=g_find_program_in_path (token);
				if(prog==NULL) {
#ifdef DEBUG
					g_message (G_GNUC_PRETTY_FUNCTION ": Couldn't find executable %s", token);
#endif

					g_free (token);
					SetLastError (ERROR_FILE_NOT_FOUND);
					goto cleanup;
				}
			}
		}

		g_free (token);
	}

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Exec prog [%s] args [%s]", prog,
		   args_after_prog);
#endif
	
	if(args_after_prog!=NULL && *args_after_prog) {
		gchar *qprog;

		qprog = g_shell_quote (prog);
		full_prog=g_strconcat (qprog, " ", args_after_prog, NULL);
		g_free (qprog);
	} else {
		full_prog=g_shell_quote (prog);
	}
	
	stored_prog=_wapi_handle_scratch_store (full_prog, strlen (full_prog));

	if(startup!=NULL && startup->dwFlags & STARTF_USESTDHANDLES) {
		stdin_handle=startup->hStdInput;
		stdout_handle=startup->hStdOutput;
		stderr_handle=startup->hStdError;
	} else {
		stdin_handle=GetStdHandle (STD_INPUT_HANDLE);
		stdout_handle=GetStdHandle (STD_OUTPUT_HANDLE);
		stderr_handle=GetStdHandle (STD_ERROR_HANDLE);
	}
	
	ret=_wapi_handle_process_fork (stored_prog, env, stored_dir,
				       inherit_handles, create_flags,
				       stdin_handle, stdout_handle,
				       stderr_handle, &process_handle,
				       &thread_handle, &pid, &tid);
	
	if(ret==TRUE && process_info!=NULL) {
		process_info->hProcess=process_handle;
		process_info->hThread=thread_handle;
		process_info->dwProcessId=pid;
		process_info->dwThreadId=tid;
		/* Wait for possible execve failure */
		if (WaitForSingleObjectEx (process_handle, 500, FALSE) != WAIT_TIMEOUT) {
			_wapi_lookup_handle (GUINT_TO_POINTER (process_handle),
					     WAPI_HANDLE_PROCESS,
					     (gpointer *) &process_handle_data,
					     NULL);
		
			if (process_handle_data && process_handle_data->exec_errno != 0) {
				ret = FALSE;
				SetLastError (ERROR_PATH_NOT_FOUND);
			}
		}
	} else if (ret==FALSE) {
		/* FIXME: work out a better error code
		 */
		SetLastError (ERROR_PATH_NOT_FOUND);
	}

cleanup:
	if(cmd!=NULL) {
		g_free (cmd);
	}
	if(full_prog!=NULL) {
		g_free (prog);
	}
	if(stored_prog!=0) {
		_wapi_handle_scratch_delete (stored_prog);
	}
	if(args!=NULL) {
		g_free (args);
	}
	if(dir!=NULL) {
		g_free (dir);
	}
	if(stored_dir!=0) {
		_wapi_handle_scratch_delete (stored_dir);
	}
	if(env!=0) {
		_wapi_handle_scratch_delete_string_array (env);
	}
	
	return(ret);
}
		
static void process_set_name (struct _WapiHandle_process *process_handle)
{
	gchar *progname, *utf8_progname, *slash;
	
	progname=g_get_prgname ();
	utf8_progname=mono_utf8_from_external (progname);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": using [%s] as prog name",
		   progname);
#endif

	if(utf8_progname!=NULL) {
		slash=strrchr (utf8_progname, '/');
		if(slash!=NULL) {
			process_handle->proc_name=_wapi_handle_scratch_store (slash+1, strlen (slash+1));
		} else {
			process_handle->proc_name=_wapi_handle_scratch_store (utf8_progname, strlen (utf8_progname));
		}

		g_free (utf8_progname);
	}
}

extern void _wapi_time_t_to_filetime (time_t timeval, WapiFileTime *filetime);

static void process_set_current (void)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	pid_t pid=getpid ();
	char *handle_env;
	
	handle_env=getenv ("_WAPI_PROCESS_HANDLE");
	if(handle_env==NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Need to create my own process handle");
#endif

		current_process=_wapi_handle_new (WAPI_HANDLE_PROCESS);
		if(current_process==_WAPI_HANDLE_INVALID) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": error creating process handle");
			return;
		}

		ok=_wapi_lookup_handle (current_process, WAPI_HANDLE_PROCESS,
					(gpointer *)&process_handle, NULL);
		if(ok==FALSE) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": error looking up process handle %p",
				   current_process);
			return;
		}

		process_handle->id=pid;
		
		/* These seem to be the defaults on w2k */
		process_handle->min_working_set=204800;
		process_handle->max_working_set=1413120;

		_wapi_time_t_to_filetime (time (NULL), &process_handle->create_time);

		process_set_name (process_handle);
		
		/* Make sure the new handle has a reference so it wont go away
		 * until this process exits
		 */
		_wapi_handle_ref (current_process);
	} else {
		guchar *procname;
		
		current_process=GUINT_TO_POINTER (atoi (handle_env));

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Found my process handle: %p", current_process);
#endif

		ok=_wapi_lookup_handle (current_process, WAPI_HANDLE_PROCESS,
					(gpointer *)&process_handle, NULL);
		if(ok==FALSE) {
			g_warning (G_GNUC_PRETTY_FUNCTION
				   ": error looking up process handle %p",
				   current_process);
			return;
		}

		procname=_wapi_handle_scratch_lookup (process_handle->proc_name);
		if(procname!=NULL) {
			if(!strcmp (procname, "mono")) {
				/* Set a better process name */
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION ": Setting better process name");
#endif

				_wapi_handle_scratch_delete (process_handle->proc_name);
				process_set_name (process_handle);
			} else {
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": Leaving process name: %s",
					   procname);
#endif
			}
			
			g_free (procname);
		}
	}
}

/* Returns a pseudo handle that doesn't need to be closed afterwards */
gpointer GetCurrentProcess (void)
{
	mono_once (&process_current_once, process_set_current);
		
	return((gpointer)-1);
}

guint32 GetCurrentProcessId (void)
{
	struct _WapiHandle_process *current_process_handle;
	gboolean ok;
	
	mono_once (&process_current_once, process_set_current);
		
	ok=_wapi_lookup_handle (current_process, WAPI_HANDLE_PROCESS,
				(gpointer *)&current_process_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up current process handle %p",
			   current_process);
		/* No failure return is defined.  PID 0 is invalid.
		 * This should only be reached when something else has
		 * gone badly wrong anyway.
		 */
		return(0);
	}
	
	return(current_process_handle->id);
}

static gboolean process_enum (gpointer handle, gpointer user_data)
{
	GPtrArray *processes=user_data;
	
	/* Ignore processes that have already exited (ie they are signalled) */
	if(_wapi_handle_issignalled (handle)==FALSE) {
		g_ptr_array_add (processes, handle);
	}
	
	/* Return false to keep searching */
	return(FALSE);
}

gboolean EnumProcesses (guint32 *pids, guint32 len, guint32 *needed)
{
	GPtrArray *processes=g_ptr_array_new ();
	guint32 fit, i;
	
	mono_once (&process_current_once, process_set_current);
	
	_wapi_search_handle (WAPI_HANDLE_PROCESS, process_enum, processes,
			     NULL, NULL);
	
	fit=len/sizeof(guint32);
	for(i=0; i<fit && i<processes->len; i++) {
		struct _WapiHandle_process *process_handle;
		gboolean ok;

		ok=_wapi_lookup_handle (g_ptr_array_index (processes, i),
					WAPI_HANDLE_PROCESS,
					(gpointer *)&process_handle, NULL);
		if(ok==FALSE) {
			g_warning (G_GNUC_PRETTY_FUNCTION ": error looking up process handle %p", g_ptr_array_index (processes, i));
			g_ptr_array_free (processes, FALSE);
			return(FALSE);
		}

		pids[i]=process_handle->id;
	}

	g_ptr_array_free (processes, FALSE);
	
	*needed=i*sizeof(guint32);
	
	return(TRUE);
}

static gboolean process_open_compare (gpointer handle, gpointer user_data)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	pid_t pid;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up process handle %p", handle);
		return(FALSE);
	}

	pid=GPOINTER_TO_UINT (user_data);

	/* It's possible to have more than one process handle with the
	 * same pid, but only the one running process can be
	 * unsignalled
	 */
	if(process_handle->id==pid &&
	   _wapi_handle_issignalled (handle)==FALSE) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}

gpointer OpenProcess (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, guint32 pid)
{
	/* Find the process handle that corresponds to pid */
	gpointer handle;
	
	mono_once (&process_current_once, process_set_current);

	handle=_wapi_search_handle (WAPI_HANDLE_PROCESS, process_open_compare,
				    GUINT_TO_POINTER (pid), NULL, NULL);
	if(handle==0) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Can't find pid %d", pid);
#endif

		/* Set an error code */
	
		return(NULL);
	}

	_wapi_handle_ref (handle);
	
	return(handle);
}

gboolean GetExitCodeProcess (gpointer process, guint32 *code)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	
	mono_once (&process_current_once, process_set_current);

	if(code==NULL) {
		return(FALSE);
	}
	
	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Can't find process %p",
			   process);
#endif
		
		return(FALSE);
	}
	
	/* A process handle is only signalled if the process has exited */
	if(_wapi_handle_issignalled (process)==TRUE) {
		*code=process_handle->exitstatus;
	} else {
		*code=STILL_ACTIVE;
	}
	
	return(TRUE);
}

gboolean GetProcessTimes (gpointer process, WapiFileTime *create_time,
			  WapiFileTime *exit_time, WapiFileTime *kernel_time,
			  WapiFileTime *user_time)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	
	mono_once (&process_current_once, process_set_current);

	if(create_time==NULL || exit_time==NULL || kernel_time==NULL ||
	   user_time==NULL) {
		/* Not sure if w32 allows NULLs here or not */
		return(FALSE);
	}
	
	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Can't find process %p",
			   process);
#endif
		
		return(FALSE);
	}
	
	*create_time=process_handle->create_time;

	/* A process handle is only signalled if the process has
	 * exited.  Otherwise exit_time isn't set
	 */
	if(_wapi_handle_issignalled (process)==TRUE) {
		*exit_time=process_handle->exit_time;
	}
	
	return(TRUE);
}

gboolean EnumProcessModules (gpointer process, gpointer *modules,
			     guint32 size, guint32 *needed)
{
	/* Store modules in an array of pointers (main module as
	 * modules[0]), using the load address for each module as a
	 * token.  (Use 'NULL' as an alternative for the main module
	 * so that the simple implementation can just return one item
	 * for now.)  Get the info from /proc/<pid>/maps on linux,
	 * other systems will have to implement /dev/kmem reading or
	 * whatever other horrid technique is needed.
	 */
	if(size<sizeof(gpointer)) {
		return(FALSE);
	}
	
#ifdef linux
	modules[0]=NULL;
	*needed=sizeof(gpointer);
#else
	modules[0]=NULL;
	*needed=sizeof(gpointer);
#endif
	
	return(TRUE);
}

guint32 GetModuleBaseName (gpointer process, gpointer module,
			   gunichar2 *basename, guint32 size)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	
	mono_once (&process_current_once, process_set_current);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": Getting module base name, process handle %p module %p",
		   process, module);
#endif

	if(basename==NULL || size==0) {
		return(FALSE);
	}
	
	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Can't find process %p",
			   process);
#endif
		
		return(FALSE);
	}

	if(module==NULL) {
		/* Shorthand for the main module, which has the
		 * process name recorded in the handle data
		 */
		pid_t pid;
		gunichar2 *procname;
		guchar *procname_utf8;
		glong len, bytes;
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Returning main module name");
#endif

		pid=process_handle->id;
		procname_utf8=_wapi_handle_scratch_lookup (process_handle->proc_name);
	
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Process name is [%s]",
			   procname_utf8);
#endif

		procname=g_utf8_to_utf16 (procname_utf8, -1, NULL, &len, NULL);
		if(procname==NULL) {
			/* bugger */
			g_free (procname_utf8);
			return(0);
		}

		/* Add the terminator, and convert chars to bytes */
		bytes=(len+1)*2;
		
		if(size<bytes) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": Size %d smaller than needed (%ld); truncating", size, bytes);
#endif

			memcpy (basename, procname, size);
		} else {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Size %d larger than needed (%ld)",
				   size, bytes);
#endif

			memcpy (basename, procname, bytes);
		}
		
		g_free (procname_utf8);
		g_free (procname);

		return(len);
	} else {
		/* Look up the address in /proc/<pid>/maps */
	}
	
	return(0);
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
	
	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Can't find process %p",
			   process);
#endif
		
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

	ok=_wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Can't find process %p",
			   process);
#endif
		
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
	gint signo;
	gint err;

	ok = _wapi_lookup_handle (process, WAPI_HANDLE_PROCESS,
				  (gpointer *) &process_handle, NULL);

	if (ok == FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Can't find process %p",
			   process);
#endif
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	signo = (exitCode == -1) ? SIGKILL : SIGTERM;
	return _wapi_handle_process_kill (process_handle->id, signo, &err);
}

