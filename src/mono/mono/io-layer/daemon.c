/*
 * daemon.c:  The handle daemon
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <stdio.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/poll.h>
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include <signal.h>
#include <sys/wait.h>

#include <mono/io-layer/io-layer.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/daemon-messages.h>
#include <mono/io-layer/timefuncs-private.h>
#include <mono/io-layer/daemon-private.h>

#undef DEBUG

/* Array to hold the file descriptor we are currently polling */
static struct pollfd *pollfds=NULL;
/* Keep track of the size and usage of pollfds */
static int nfds=0, maxfds=0;
/* Array to keep track of arrays of handles that belong to a given
 * client. handle_refs[0] is used by the daemon itself
 */
static guint32 **handle_refs=NULL;
/* The socket which we listen to new connections on */
static int main_sock;

/* Set to TRUE by the SIGCHLD signal handler */
static volatile gboolean check_processes=FALSE;

/* Deletes the shared memory segment.  If we're exiting on error,
 * clients will get EPIPEs.
 */
static void cleanup (void)
{
	_wapi_shm_destroy ();
}

/* If there is only one socket, and no child processes, we can exit.
 * We test for child processes by counting handle references held by
 * the daemon.
 */
static void maybe_exit (void)
{
	guint32 *open_handles=handle_refs[0], i;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Seeing if we should exit");
#endif

	if(nfds>1) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Still got clients");
#endif
		return;
	}

	for(i=0; i<_WAPI_MAX_HANDLES; i++) {
		if(open_handles[i]>0) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Still got handle references");
#endif
			return;
		}
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Byebye");
#endif

	cleanup ();
	exit (0);
}

/*
 * signal_handler:
 * @unused: unused
 *
 * Called if daemon receives a SIGTERM or SIGINT
 */
static void signal_handler (int unused)
{
	cleanup ();
	exit (-1);
}

/*
 * sigchld_handler:
 * @unused: unused
 *
 * Called if daemon receives a SIGCHLD, and notes that a process needs
 * to be wait()ed for.
 */
static void sigchld_handler (int unused)
{
	/* Notice that a child process died */
	check_processes=TRUE;
}

/*
 * startup:
 *
 * Bind signals and attach to shared memory
 */
static void startup (void)
{
	struct sigaction sa;
	gboolean success;
	int shm_id;
	
	sa.sa_handler=signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags=0;
	sigaction (SIGINT, &sa, NULL);
	sigaction (SIGTERM, &sa, NULL);
	
#ifndef HAVE_MSG_NOSIGNAL
	sa.sa_handler=SIG_IGN;
	sigaction (SIGPIPE, &sa, NULL);
#endif

	sa.sa_handler=sigchld_handler;
	sa.sa_flags=SA_NOCLDSTOP;
	sigaction (SIGCHLD, &sa, NULL);
	
	_wapi_shared_data=_wapi_shm_attach (TRUE, &success, &shm_id);
	if(success==FALSE) {
		g_error ("Failed to attach shared memory! (tried shared memory ID 0x%x)", shm_id);
		exit (-1);
	}

	/* Leave the first byte NULL so we create the socket in the
	 * abstrace namespace, not on the filesystem.  (Lets see how
	 * portable _that_ is :)
	 *
	 * The name is intended to be unique, not cryptographically
	 * secure...
	 */
	snprintf (_wapi_shared_data->daemon+1, 106,
		  "mono-handle-daemon-%d-%d-%ld", getuid (), getpid (),
		  time (NULL));
}


/*
 * ref_handle:
 * @idx: idx into pollfds
 * @handle: handle to inc refcnt
 *
 * Increase ref count of handle for idx's filedescr. and the
 * shm. Handle 0 is ignored.
 */
static void ref_handle (guint32 idx, guint32 handle)
{
	guint32 *open_handles=handle_refs[idx];
	
	if(handle==0) {
		return;
	}
	
	_wapi_shared_data->handles[handle].ref++;
	open_handles[handle]++;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": handle 0x%x ref now %d (%d this process)", handle,
		   _wapi_shared_data->handles[handle].ref,
		   open_handles[handle]);
#endif
}

/*
 * unref_handle:
 * @idx: idx into pollfds
 * @handle: handle to inc refcnt
 *
 * Decrease ref count of handle for idx's filedescr. and the shm. If
 * global ref count reaches 0 it is free'ed. Return TRUE if the local
 * ref count is 0. Handle 0 is ignored.
 */
static gboolean unref_handle (guint32 idx, guint32 handle)
{
	guint32 *open_handles=handle_refs[idx];
	gboolean destroy=FALSE;
	
	if(handle==0) {
		return(FALSE);
	}
	
	if (open_handles[handle] == 0) {
                g_warning(G_GNUC_PRETTY_FUNCTION
                          ": unref on %d called when ref was already 0", 
                          handle);
                return TRUE;
        }

	_wapi_shared_data->handles[handle].ref--;
	open_handles[handle]--;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": handle 0x%x ref now %d (%d this process)", handle,
		   _wapi_shared_data->handles[handle].ref,
		   open_handles[handle]);
#endif

	if(open_handles[handle]==0) {
		/* This client has released the handle */
		destroy=TRUE;
	}
	
	if(_wapi_shared_data->handles[handle].ref==0) {
		if (open_handles[handle]!=0) {
			g_warning (G_GNUC_PRETTY_FUNCTION ": per-process open_handles mismatch, set to %d, should be 0", open_handles[handle]);
		}
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Destroying handle 0x%x",
			   handle);
#endif
		
		_wapi_handle_ops_close_shared (GUINT_TO_POINTER (handle));
		
		_wapi_shared_data->handles[handle].type=WAPI_HANDLE_UNUSED;
		mono_mutex_destroy (&_wapi_shared_data->handles[handle].signal_mutex);
		pthread_cond_destroy (&_wapi_shared_data->handles[handle].signal_cond);
		memset (&_wapi_shared_data->handles[handle].u, '\0', sizeof(_wapi_shared_data->handles[handle].u));
	}

	if(idx==0) {
		/* The daemon released a reference, so see if it's
		 * ready to exit
		 */
		maybe_exit ();
	}
	
	return(destroy);
}

/*
 * add_fd:
 * @fd: Filehandle to add
 *
 * Add filedescriptor to the pollfds array, expand if necessary
 */
static void add_fd(int fd)
{
	if(nfds==maxfds) {
		/* extend the array */
		maxfds+=10;
		/* no need to memset the extra memory, we init it
		 * before use anyway */
		pollfds=g_renew (struct pollfd, pollfds, maxfds);
		handle_refs=g_renew (guint32*, handle_refs, maxfds);
	}

	pollfds[nfds].fd=fd;
	pollfds[nfds].events=POLLIN;
	pollfds[nfds].revents=0;
	
	handle_refs[nfds]=g_new0 (guint32, _WAPI_MAX_HANDLES);

	nfds++;
}

/*
 * rem_fd:
 * @idx: idx into pollfds to remove
 *
 * Close filedescriptor and remove it from the pollfds array. Closes
 * all handles that it may have open. If only main_sock is open, the
 * daemon is shut down.
 */
static void rem_fd(int idx)
{
	guint32 *open_handles=handle_refs[idx], handle_count;
	int i, j;
	
	if(idx==0) {
		/* We shouldn't be deleting the daemon's fd */
		g_warning (G_GNUC_PRETTY_FUNCTION ": Deleting daemon fd!");
		cleanup ();
		exit (-1);
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Removing client at %d", idx);
#endif

	close(pollfds[idx].fd);

	for(i=0; i<_WAPI_MAX_HANDLES; i++) {
		handle_count=open_handles[i];
		
		for(j=0; j<handle_count; j++) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": closing handle 0x%x for client at index %d", i, idx);
#endif
			/* Ignore the hint to the client to destroy
			 * the handle private data
			 */
			unref_handle (idx, i);
		}
	}
	
	nfds--;
	if(nfds==1) {
		/* Just the master socket left, so see if we can
		 * cleanup and exit
		 */
		maybe_exit ();
	}
	
	memset(&pollfds[idx], '\0', sizeof(struct pollfd));
	g_free (handle_refs[idx]);
	
	if(idx<nfds) {
		memmove(&pollfds[idx], &pollfds[idx+1],
			sizeof(struct pollfd) * (nfds-idx));
		memmove (&handle_refs[idx], &handle_refs[idx+1],
			 sizeof(guint32) * (nfds-idx));
	}
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

static gboolean process_thread_compare (gpointer handle, gpointer user_data)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		return(FALSE);
	}

	if(thread_handle->process_handle==user_data) {
		/* Signal the handle.  Don't use
		 * _wapi_handle_set_signal_state() unless we have
		 * process-shared pthread support.
		 */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Set thread handle %p signalled, because its process died", handle);
#endif

		thread_handle->exitstatus=0;

#ifdef _POSIX_THREAD_PROCESS_SHARED
		_wapi_handle_lock_handle (handle);
		_wapi_handle_set_signal_state (handle, TRUE, TRUE);
		_wapi_handle_unlock_handle (handle);
#else
		/* Just tweak the signal state directly.  This is not
		 * recommended behaviour, but it works for threads
		 * because they can never become unsignalled.  There
		 * are some nasty kludges in the handle waiting code
		 * to cope with missing condition signals for when
		 * process-shared pthread support is missing.
		 */
		_wapi_shared_data->handles[GPOINTER_TO_UINT (handle)].signalled=TRUE;
#endif /* _POSIX_THREAD_PROCESS_SHARED */
	}
	
	/* Return false to keep searching */
	return(FALSE);
}

/* Find the handle associated with pid, mark it dead and record exit
 * status.  Finds all thread handles associated with this process
 * handle, and marks those signalled too, with exitstatus '0'.  It
 * also drops the daemon's reference to the handle, and the thread
 * pointed at by main_thread.
 */
static void process_post_mortem (pid_t pid, int status)
{
	gpointer process_handle;
	struct _WapiHandle_process *process_handle_data;
	
	process_handle=_wapi_search_handle (WAPI_HANDLE_PROCESS,
					    process_compare,
					    GUINT_TO_POINTER (pid),
					    (gpointer *)&process_handle_data,
					    NULL);
	if(process_handle==0) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": Couldn't find handle for process %d!", pid);
	} else {
		/* Signal the handle.  Don't use
		 * _wapi_handle_set_signal_state() unless we have
		 * process-shared pthread support.
		 */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Set process %d exitstatus to %d", pid,
			   WEXITSTATUS (status));
#endif
		
		/* Technically WEXITSTATUS is only valid if the
		 * process exited normally, but I don't care if the
		 * process caught a signal or not.
		 */
		process_handle_data->exitstatus=WEXITSTATUS (status);
		_wapi_time_t_to_filetime (time (NULL),
					  &process_handle_data->exit_time);

#ifdef _POSIX_THREAD_PROCESS_SHARED
		_wapi_handle_lock_handle (process_handle);
		_wapi_handle_set_signal_state (process_handle, TRUE, TRUE);
		_wapi_handle_unlock_handle (process_handle);
#else
		/* Just tweak the signal state directly.  This is not
		 * recommended behaviour, but it works for processes
		 * because they can never become unsignalled.  There
		 * are some nasty kludges in the handle waiting code
		 * to cope with missing condition signals for when
		 * process-shared pthread support is missing.
		 */
		_wapi_shared_data->handles[GPOINTER_TO_UINT (process_handle)].signalled=TRUE;
#endif /* _POSIX_THREAD_PROCESS_SHARED */
	}

	/* Find all threads that have their process
	 * handle==process_handle.  Ignore the return value, all the
	 * work will be done in the compare func
	 */
	(void)_wapi_search_handle (WAPI_HANDLE_THREAD, process_thread_compare,
				   process_handle, NULL, NULL);

	unref_handle (0, GPOINTER_TO_UINT (process_handle_data->main_thread));
	unref_handle (0, GPOINTER_TO_UINT (process_handle));
}

static void process_died (void)
{
	int status;
	pid_t pid;
	
	check_processes=FALSE;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Reaping processes");
#endif

	while(TRUE) {
		pid=waitpid (-1, &status, WNOHANG);
		if(pid==0 || pid==-1) {
			/* Finished waiting.  I was checking pid==-1
			 * separately but was getting ECHILD when
			 * there were no more child processes (which
			 * doesnt seem to conform to the man page)
			 */
			return;
		} else {
			/* pid contains the ID of a dead process */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": process %d reaped", pid);
#endif
			process_post_mortem (pid, status);
		}
	}
}


/*
 * send_reply:
 * @idx: idx into pollfds.
 * @rest: Package to send
 *
 * Send a package to a client
 */
static void send_reply (guint32 idx, WapiHandleResponse *resp)
{
	/* send message */
	_wapi_daemon_response (pollfds[idx].fd, resp);
}

/*
 * process_new:
 * @idx: idx into pollfds.
 * @type: type to init handle to
 *
 * Find a free handle and initialize it to 'type', increase refcnt and
 * send back a reply to the client.
 */
static void process_new (guint32 idx, WapiHandleType type)
{
	guint32 handle;
	WapiHandleResponse resp;
	
	/* handle might be set to 0.  This is handled at the client end */
	handle=_wapi_handle_new_internal (type);
	ref_handle (idx, handle);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning new handle 0x%x",
		   handle);
#endif

	resp.type=WapiHandleResponseType_New;
	resp.u.new.type=type;
	resp.u.new.handle=handle;
			
	send_reply (idx, &resp);
}

/*
 * process_open:
 * @idx: idx into pollfds.
 * @handle: handle no.
 *
 * Increase refcnt on a previously created handle and send back a
 * response to the client.
 */
static void process_open (guint32 idx, guint32 handle)
{
	WapiHandleResponse resp;
	struct _WapiHandleShared *shared=&_wapi_shared_data->handles[handle];
		
	if(shared->type!=WAPI_HANDLE_UNUSED && handle!=0) {
		ref_handle (idx, handle);

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": returning new handle 0x%x", handle);
#endif

		resp.type=WapiHandleResponseType_Open;
		resp.u.new.type=shared->type;
		resp.u.new.handle=handle;
			
		send_reply (idx, &resp);

		return;
	}

	resp.type=WapiHandleResponseType_Open;
	resp.u.new.handle=0;
			
	send_reply (idx, &resp);
}

/*
 * process_close:
 * @idx: idx into pollfds.
 * @handle: handle no.
 *
 * Decrease refcnt on a previously created handle and send back a
 * response to the client with notice of it being destroyed.
 */
static void process_close (guint32 idx, guint32 handle)
{
	WapiHandleResponse resp;
	
	resp.type=WapiHandleResponseType_Close;
	resp.u.close.destroy=unref_handle (idx, handle);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unreffing handle 0x%x", handle);
#endif
			
	send_reply (idx, &resp);
}

/*
 * process_scratch:
 * @idx: idx into pollfds.
 * @length: allocate this much scratch space
 *
 * Allocate some scratch space and send a reply to the client.
 */
static void process_scratch (guint32 idx, guint32 length)
{
	WapiHandleResponse resp;
	
	resp.type=WapiHandleResponseType_Scratch;
	resp.u.scratch.idx=_wapi_handle_scratch_store_internal (length);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": allocating scratch index 0x%x",
		   resp.u.scratch.idx);
#endif
			
	send_reply (idx, &resp);
}

/*
 * process_scratch_free:
 * @idx: idx into pollfds.
 * @scratch_idx: deallocate this scratch space
 *
 * Deallocate scratch space and send a reply to the client.
 */
static void process_scratch_free (guint32 idx, guint32 scratch_idx)
{
	WapiHandleResponse resp;
	
	resp.type=WapiHandleResponseType_ScratchFree;
	_wapi_handle_scratch_delete_internal (scratch_idx);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": deleting scratch index 0x%x",
		   scratch_idx);
#endif
			
	send_reply (idx, &resp);
}

static void process_process_fork (guint32 idx,
				  WapiHandleRequest_ProcessFork process_fork,
				  int *fds)
{
	WapiHandleResponse resp;
	guint32 process_handle, thread_handle;
	struct _WapiHandle_process *process_handle_data;
	struct _WapiHandle_thread *thread_handle_data;
	pid_t pid;
	
	resp.type=WapiHandleResponseType_ProcessFork;
	
	/* Create handles first, so the child process can store exec
	 * errors.  Either handle might be set to 0, if this happens
	 * just reply to the client without bothering to fork.  The
	 * client must check if either handle is 0 and take
	 * appropriate error handling action.
	 */
	process_handle=_wapi_handle_new_internal (WAPI_HANDLE_PROCESS);
	ref_handle (0, process_handle);
	ref_handle (idx, process_handle);
	
	thread_handle=_wapi_handle_new_internal (WAPI_HANDLE_THREAD);
	ref_handle (0, thread_handle);
	ref_handle (idx, thread_handle);
	
	if(process_handle==0 || thread_handle==0) {
		/* unref_handle() copes with the handle being 0 */
		unref_handle (0, process_handle);
		unref_handle (idx, process_handle);
		unref_handle (0, thread_handle);
		unref_handle (idx, thread_handle);
		process_handle=0;
		thread_handle=0;
	} else {
		char *cmd=NULL, *args=NULL;
			
		/* Get usable copies of the cmd and args now rather
		 * than in the child process.  This is to prevent the
		 * race condition where the parent can return the
		 * reply to the client, which then promptly deletes
		 * the scratch data before the new process gets to see
		 * it.
		 */
		cmd=_wapi_handle_scratch_lookup_as_string (process_fork.cmd);
		if(process_fork.args!=0) {
			args=_wapi_handle_scratch_lookup_as_string (process_fork.args);
		}

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": forking");
#endif

		_wapi_lookup_handle (GUINT_TO_POINTER (process_handle),
				     WAPI_HANDLE_PROCESS,
				     (gpointer *)&process_handle_data, NULL);

		_wapi_lookup_handle (GUINT_TO_POINTER (thread_handle),
				     WAPI_HANDLE_THREAD,
				     (gpointer *)&thread_handle_data, NULL);

		/* Fork, exec cmd with args and optional env, and
		 * return the handles with pid and blank thread id
		 */
		pid=fork ();
		if(pid==-1) {
			process_handle_data->exec_errno=errno;
		} else if (pid==0) {
			/* child */
			char **argv, *full_args;
			GError *gerr=NULL;
			gboolean ret;
			
			/* should we detach from the process group? 
			 * We're already running without a controlling
			 * tty...
			 */
			if(process_fork.inherit!=TRUE) {
				/* Close all file descriptors */
			}

			/* Connect stdin, stdout and stderr */
			dup2 (fds[0], 0);
			dup2 (fds[1], 1);
			dup2 (fds[2], 2);
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": exec()ing [%s] args [%s]", cmd, args);
#endif		
		
			if(args!=NULL) {
				full_args=g_strconcat (cmd, " ", args, NULL);
			} else {
				full_args=g_strdup (cmd);
			}
			ret=g_shell_parse_argv (full_args, NULL, &argv, &gerr);
			
			g_free (full_args);

			if(ret==FALSE) {
				/* FIXME: Could do something with the
				 * GError here
				 */
				process_handle_data->exec_errno=gerr->code;
				exit (-1);
			}
			

#ifdef DEBUG
			{
				int i=0;
				while(argv[i]!=NULL) {
					g_message ("arg %d: [%s]", i, argv[i]);
					i++;
				}
			}
#endif
			
			
			/* exec */
			execv (cmd, argv);
		
			/* bummer! */
			process_handle_data->exec_errno=errno;
			exit (-1);
		}
		/* parent */
		
		/* store pid */
		process_handle_data->id=pid;
		process_handle_data->main_thread=GUINT_TO_POINTER (thread_handle);
		_wapi_time_t_to_filetime (time (NULL),
					  &process_handle_data->create_time);
		
		/* FIXME: if env==0, inherit the env from the current
		 * process
		 */
		process_handle_data->env=process_fork.env;

		thread_handle_data->process_handle=GUINT_TO_POINTER (process_handle);

		resp.u.process_fork.pid=pid;
	}
			
	resp.u.process_fork.process_handle=process_handle;
	resp.u.process_fork.thread_handle=thread_handle;

	send_reply (idx, &resp);
}

/*
 * read_message:
 * @idx: idx into pollfds.
 *
 * Read a message (A WapiHandleRequest) from a client and dispatch
 * whatever it wants to the process_* calls.
 */
static void read_message (guint32 idx)
{
	WapiHandleRequest req;
	int fds[3]={0, 1, 2};
	gboolean has_fds=FALSE;
	
	/* Reading data */
	_wapi_daemon_request (pollfds[idx].fd, &req, fds, &has_fds);
	switch(req.type) {
	case WapiHandleRequestType_New:
		process_new (idx, req.u.new.type);
		break;
	case WapiHandleRequestType_Open:
#ifdef DEBUG
		g_assert(req.u.open.handle < _WAPI_MAX_HANDLES);
#endif
		process_open (idx, req.u.open.handle);
		break;
	case WapiHandleRequestType_Close:
#ifdef DEBUG
		g_assert(req.u.close.handle < _WAPI_MAX_HANDLES);
#endif
		process_close (idx, req.u.close.handle);
		break;
	case WapiHandleRequestType_Scratch:
		process_scratch (idx, req.u.scratch.length);
		break;
	case WapiHandleRequestType_ScratchFree:
		process_scratch_free (idx, req.u.scratch_free.idx);
		break;
	case WapiHandleRequestType_ProcessFork:
		process_process_fork (idx, req.u.process_fork, fds);
		break;
	case WapiHandleRequestType_Error:
		/* fall through */
	default:
		/* Catch bogus requests */
		/* FIXME: call rem_fd? */
		break;
	}

	if(has_fds==TRUE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": closing %d", fds[0]);
		g_message (G_GNUC_PRETTY_FUNCTION ": closing %d", fds[1]);
		g_message (G_GNUC_PRETTY_FUNCTION ": closing %d", fds[2]);
#endif
		
		close (fds[0]);
		close (fds[1]);
		close (fds[2]);
	}
}

/*
 * _wapi_daemon_main:
 *
 * Open socket, create shared mem segment and begin listening for
 * clients.
 */
void _wapi_daemon_main(void)
{
	struct sockaddr_un main_socket_address;
	int ret;

#ifdef DEBUG
	g_message ("Starting up...");
#endif

	startup ();
	
	main_sock=socket(PF_UNIX, SOCK_STREAM, 0);

	main_socket_address.sun_family=AF_UNIX;
	memcpy(main_socket_address.sun_path, _wapi_shared_data->daemon, sizeof (main_socket_address.sun_path));

	ret=bind(main_sock, (struct sockaddr *)&main_socket_address,
		 sizeof(struct sockaddr_un));
	if(ret==-1) {
		g_critical ("bind failed: %s", strerror (errno));
		_wapi_shared_data->daemon_running=DAEMON_DIED_AT_STARTUP;
		exit(-1);
	}

#ifdef DEBUG
	g_message("bound");
#endif

	ret=listen(main_sock, 5);
	if(ret==-1) {
		g_critical ("listen failed: %s", strerror (errno));
		_wapi_shared_data->daemon_running=DAEMON_DIED_AT_STARTUP;
		exit(-1);
	}

#ifdef DEBUG
	g_message("listening");
#endif

	add_fd(main_sock);

	/* We're finished setting up, let everyone else know we're
	 * ready.  From now on, it's up to us to delete the shared
	 * memory segment when appropriate.
	 */
	_wapi_shared_data->daemon_running=DAEMON_RUNNING;

	while(TRUE) {
		int i;

		if(check_processes==TRUE) {
			process_died ();
		}
		
#ifdef DEBUG
		g_message ("polling");
#endif

		/* Block until something happens */
		ret=poll(pollfds, nfds, -1);
		if(ret==-1 && errno!=EINTR) {
			g_critical ("poll error: %s", strerror (errno));
			cleanup ();
			exit(-1);
		}

		for(i=0; i<nfds; i++) {
			if(((pollfds[i].revents&POLLHUP)==POLLHUP) ||
			   ((pollfds[i].revents&POLLERR)==POLLERR) ||
			   ((pollfds[i].revents&POLLNVAL)==POLLNVAL)) {
#ifdef DEBUG
			   	g_message ("fd[%d] %d error", i,
					   pollfds[i].fd);
#endif
				rem_fd(i);
			} else if((pollfds[i].revents&POLLIN)==POLLIN) {
				/* If a client is connecting, accept
				 * it and begin listening to that
				 * socket too.  Otherwise it must be a
				 * client we already have that wants
				 * something.
				 */
				/* FIXME: Make sure i==0 too? */
				if(pollfds[i].fd==main_sock) {
					int newsock;
					struct sockaddr addr;
					socklen_t addrlen=sizeof(struct sockaddr);
					newsock=accept(main_sock, &addr,
						       &addrlen);
					if(newsock==-1) {
						g_critical("accept error: %s",
							   strerror (errno));
						cleanup ();
						exit(-1);
					}
#ifdef DEBUG
					g_message ("accept returning %d",
						   newsock);
#endif
					add_fd(newsock);
				} else {
#ifdef DEBUG
					g_message ("reading data on fd %d",
						   pollfds[i].fd);
#endif
					read_message (i);
				}
			}
		}
	}
}
