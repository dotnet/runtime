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
#include <gc/gc.h>
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

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/unicode.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/process-private.h>
#include <mono/io-layer/threads.h>

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
#ifdef DEBUG
	struct _WapiHandle_process *process_handle;
	gboolean ok;

	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up process handle %p", handle);
		return;
	}

	g_message (G_GNUC_PRETTY_FUNCTION
		   ": closing process handle %p with id %d", handle,
		   process_handle->id);
#endif
}

gboolean CreateProcess (const gunichar2 *appname, gunichar2 *cmdline,
			WapiSecurityAttributes *process_attrs G_GNUC_UNUSED,
			WapiSecurityAttributes *thread_attrs G_GNUC_UNUSED,
			gboolean inherit_handles, guint32 create_flags,
			gpointer environ, const gunichar2 *cwd,
			WapiStartupInfo *startup,
			WapiProcessInformation *process_info)
{
	gchar *cmd=NULL, *prog, *args=NULL, *dir=NULL;
	gunichar2 *environp;
	guint32 env=0, stored_dir=0, stored_prog=0, stored_args=0;
	guint32 env_count=0, i;
	gboolean ret=FALSE;
	gpointer stdin_handle, stdout_handle, stderr_handle;
	guint32 pid, tid;
	gpointer process_handle, thread_handle;
	
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
		cmd=_wapi_unicode_to_utf8 (appname);
		if(cmd==NULL) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": unicode conversion returned NULL");
#endif

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
		args=_wapi_unicode_to_utf8 (cmdline);
		if(args==NULL) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": unicode conversion returned NULL");
#endif

			goto cleanup;
		}

		/* Turn all the slashes round the right way */
		for(i=0; i<strlen (args); i++) {
			if(args[i]=='\\') {
				args[i]='/';
			}
		}
	}

	if(cwd!=NULL) {
		dir=_wapi_unicode_to_utf8 (cwd);
		if(dir==NULL) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": unicode conversion returned NULL");
#endif

			goto cleanup;
		}

		/* Turn all the slashes round the right way */
		for(i=0; i<strlen (dir); i++) {
			if(dir[i]=='\\') {
				dir[i]='/';
			}
		}
		stored_dir=_wapi_handle_scratch_store (dir, strlen (dir));
	}
	
	/* environ is a block of NULL-terminated strings, which is
	 * itself NULL-terminated. Of course, passing an array of
	 * string pointers would have made things too easy :-(
	 */
	/* Not sure whether I should turn the w32 env block into
	 * proper env vars, or just leave it to be read back by other
	 * w32 emulation functions.
	 */
	if(environ!=NULL) {
		/* env_count counts bytes, not chars */
		for(environp=(gunichar2 *)environ; *environp;
		    env_count+=2, environp++) {
			while(*environp) {
				env_count+=2;
				environp++;
			}
		}

		env=_wapi_handle_scratch_store (environ, env_count);
	}

	/* We can't put off locating the executable any longer :-( */
	if(cmd!=NULL) {
		if(g_ascii_isalpha (cmd[0]) && (cmd[1]==':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			memmove (cmd, cmd+2, strlen (cmd)-2);
			cmd[strlen (cmd)-2]='\0';
		}

		if(cmd[0]=='/') {
			/* Assume full path given */
			prog=g_strdup (cmd);
		} else {
			/* Search for file named by cmd in the current
			 * directory
			 */
			char *curdir=g_get_current_dir ();

			prog=g_strdup_printf ("%s/%s", curdir, cmd);
			g_free (curdir);
		}
	} else {
		gchar *token=NULL;
		
		/* Dig out the first token from args, taking quotation
		 * marks into account
		 */

		/* FIXME: move the contents of args down when token
		 * has been set (otherwise argv[0] is duplicated)
		 */
		/* Assume the opening quote will always be the first
		 * character
		 */
		if(args[0]=='\"') {
			for(i=1; args[i]!='\0' && args[i]!='\"'; i++);
			if(g_ascii_isspace (args[i+1])) {
				/* We found the first token */
				token=g_strndup (args+1, i-1);
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
					break;
				}
			}
		}

		if(token==NULL && args[0]!='\0') {
			/* Must be just one token in the string */
			token=g_strdup (args);
		}
		
		if(token==NULL) {
			/* Give up */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Couldn't find what to exec");
#endif

			goto cleanup;
		}
		
		if(g_ascii_isalpha (token[0]) && (token[1]==':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			memmove (token, token+2, strlen (token)-2);
			token[strlen (token)-2]='\0';
		}

		if(token[0]=='/') {
			/* Assume full path given */
			prog=g_strdup (token);
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
					goto cleanup;
				}
			}
		}

		g_free (token);
	}

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Exec prog [%s] args [%s]",
		   prog, args);
#endif
	
	stored_prog=_wapi_handle_scratch_store (prog, strlen (prog));
	stored_args=_wapi_handle_scratch_store (args, strlen (args));
	
	stdin_handle=GetStdHandle (STD_INPUT_HANDLE);
	stdout_handle=GetStdHandle (STD_OUTPUT_HANDLE);
	stderr_handle=GetStdHandle (STD_ERROR_HANDLE);
	
	if(startup!=NULL) {
		if(startup->dwFlags & STARTF_USESTDHANDLES) {
			stdin_handle=startup->hStdInput;
			stdout_handle=startup->hStdOutput;
			stderr_handle=startup->hStdError;
		}
	}
	
	ret=_wapi_handle_process_fork (stored_prog, stored_args, env,
				       stored_dir, inherit_handles,
				       create_flags, stdin_handle,
				       stdout_handle, stderr_handle,
				       &process_handle, &thread_handle, &pid,
				       &tid);
	
	if(ret==TRUE && process_info!=NULL) {
		process_info->hProcess=process_handle;
		process_info->hThread=thread_handle;
		process_info->dwProcessId=pid;
		process_info->dwThreadId=tid;
	}
	
cleanup:
	if(cmd!=NULL) {
		g_free (cmd);
	}
	if(prog!=NULL) {
		g_free (prog);
	}
	if(stored_prog!=0) {
		_wapi_handle_scratch_delete (stored_prog);
	}
	if(args!=NULL) {
		g_free (args);
	}
	if(stored_args!=0) {
		_wapi_handle_scratch_delete (stored_args);
	}
	if(dir!=NULL) {
		g_free (dir);
	}
	if(stored_dir!=0) {
		_wapi_handle_scratch_delete (stored_dir);
	}
	if(env!=0) {
		_wapi_handle_scratch_delete (env);
	}
	
	return(ret);
}

static gboolean process_compare (gpointer handle, gpointer user_data)
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
	if(process_handle->id==pid) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}
	
static void process_set_current (void)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	pid_t pid=getpid ();
	
	current_process=_wapi_search_handle (WAPI_HANDLE_PROCESS,
					     process_compare,
					     GUINT_TO_POINTER (pid),
					     (gpointer *)&process_handle,
					     NULL);
	if(current_process==0) {
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
	} else {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Found my process handle");
#endif
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
	mono_once (&process_current_once, process_set_current);
		
	return(getpid ());
}

static gboolean process_enum (gpointer handle, gpointer user_data)
{
	GPtrArray *processes=user_data;
	
	g_ptr_array_add (processes, handle);
	
	/* Return false to keep searching */
	return(FALSE);
}

gboolean EnumProcesses (guint32 *pids, guint32 len, guint32 *needed)
{
	GPtrArray *processes=g_ptr_array_new ();
	guint32 fit, i;
	
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
