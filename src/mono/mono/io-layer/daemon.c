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
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include <signal.h>
#include <sys/wait.h>
#include <string.h>
#include <sys/time.h>

#ifdef HAVE_POLL
#include <sys/poll.h>
#endif

#include <mono/io-layer/io-layer.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/daemon-messages.h>
#include <mono/io-layer/timefuncs-private.h>
#include <mono/io-layer/daemon-private.h>
#include <mono/io-layer/socket-wrappers.h>

#define LOGDEBUG(...)
#undef DEBUG
// #define LOGDEBUG(...) g_message(__VA_ARGS__)

/* The shared thread codepath doesn't seem to work yet... */
#undef _POSIX_THREAD_PROCESS_SHARED

/* Keep track of the number of clients */
static int nfds=0;

/* Arrays to keep track of channel data for the 
 * daemon and clients indexed by file descriptor
 * value.
 */

typedef struct _channel_data {
	int io_source; /* the ID given back by g_io_add_watch */
	guint32 *open_handles; /* array of open handles for this client */
} ChannelData;

static ChannelData *daemon_channel_data=NULL;
static ChannelData *channels=NULL;
static int channels_length=0;

/* The socket which we listen to new connections on */
static int main_sock;

/* Set to TRUE by the SIGCHLD signal handler */
static volatile gboolean check_processes=FALSE;

/* The file_share_hash is used to emulate the windows file sharing mode */
typedef struct _share_key
{
	dev_t device;
	ino_t inode;
} ShareKey;

typedef struct _share_data
{
	guint32 sharemode;
	guint32 access;
} ShareData;

static GHashTable *file_share_hash = NULL;

static gboolean fd_activity (GIOChannel *channel, GIOCondition condition,
			     gpointer data);
static void check_sharing (dev_t device, ino_t inode);

/* Deletes the shared memory segment.  If we're exiting on error,
 * clients will get EPIPEs.
 */
static void cleanup (void)
{
	int i;
	
#ifdef NEED_LINK_UNLINK
        unlink(_wapi_shared_data[0]->daemon);
#endif 
	for(i=1; i<_wapi_shared_data[0]->num_segments; i++) {
		unlink (_wapi_shm_file (WAPI_SHM_DATA, i));
	}
	unlink (_wapi_shm_file (WAPI_SHM_DATA, 0));
	
	/* There's only one scratch file */
	unlink (_wapi_shm_file (WAPI_SHM_SCRATCH, 0));
}

/* If there is only one socket, and no child processes, we can exit.
 * We test for child processes by counting handle references held by
 * the daemon.
 */
static void maybe_exit (void)
{
	guint32 i;

	LOGDEBUG ("%s: Seeing if we should exit", __func__);

	if(nfds>1) {
		LOGDEBUG ("%s: Still got clients", __func__);
		return;
	}

	/* Prevent new clients from connecting... */
	_wapi_shared_data[0]->daemon_running=DAEMON_CLOSING;

	for(i=0;
	    i<_wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT;
	    i++) {
		if(daemon_channel_data->open_handles[i]>0) {
			LOGDEBUG ("%s: Still got handle references", __func__);
			_wapi_shared_data[0]->daemon_running=DAEMON_RUNNING;
			return;
		}
	}
	
#ifdef HAVE_POLL
	/* Last check, make sure no client came along while we were
	 * checking the handle lists.
	 *
	 * Use poll() directly here, as glib doesn't seem to have any
	 * exposed way of seeing if a file descriptor is ready
	 * (g_io_channel_get_buffer_condition() isn't it.)
	 *
	 * Crappy systems that don't have poll() will just have to
	 * lump it (for them there is still the very slight chance
	 * that someone tried to connect just as the DAEMON_CLOSING
	 * flag was being set.)
	 */
	{
		struct pollfd fds[1];
		
		fds[0].fd=main_sock;
		fds[0].events=POLLIN;
		fds[0].revents=0;
		
		LOGDEBUG ("%s: Last connect check", __func__);

		if(poll (fds, 1, 0)>0) {
			/* Someone did connect, so carry on running */
			LOGDEBUG ("%s: Someone connected", __func__);

			_wapi_shared_data[0]->daemon_running=DAEMON_RUNNING;
			return;
		}
	}
#endif
	
	LOGDEBUG ("%s: Byebye", __func__);
	
	cleanup ();
	exit (0);
}

/*
 * signal_handler:
 * @unused: unused
 *
 * Called if daemon receives a SIGTERM or SIGINT
 */
static void signal_handler (int signo)
{
	LOGDEBUG ("%s: daemon received signal %d", __func__, signo);

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

static guint sharedata_hash (gconstpointer key)
{
	ShareKey *sharekey = (ShareKey *)key;
	
	return(g_int_hash (&(sharekey->inode)));
}

static gboolean sharedata_equal (gconstpointer a, gconstpointer b)
{
	ShareKey *share_a = (ShareKey *)a;
	ShareKey *share_b = (ShareKey *)b;
	
	return(share_a->device == share_b->device &&
	       share_a->inode == share_b->inode);
}

/* Catch this here rather than corrupt the shared data at runtime */
#if MONO_SIZEOF_SUNPATH==0
#error configure failed to discover size of unix socket path
#endif

/*
 * startup:
 *
 * Bind signals, attach to shared memory and set up any internal data
 * structures needed.
 */
static void startup (void)
{
	struct sigaction sa;
	
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
	
#ifdef NEED_LINK_UNLINK
	/* Here's a more portable method... */
	snprintf (_wapi_shared_data[0]->daemon, MONO_SIZEOF_SUNPATH-1,
		  "/tmp/mono-handle-daemon-%d-%ld-%ld", getuid (), random (),
		  time (NULL));
#else
	/* Leave the first byte NULL so we create the socket in the
	 * abstrace namespace, not on the filesystem.  (Lets see how
	 * portable _that_ is :)
	 *
	 * The name is intended to be unique, not cryptographically
	 * secure...
	 */
	snprintf (_wapi_shared_data[0]->daemon+1, MONO_SIZEOF_SUNPATH-2,
		  "mono-handle-daemon-%d-%d-%ld", getuid (), getpid (),
		  time (NULL));
#endif

	file_share_hash = g_hash_table_new_full (sharedata_hash,
						 sharedata_equal, g_free,
						 g_free);
}


/*
 * ref_handle:
 * @channel_data: Channel data for calling client
 * @handle: handle to inc refcnt
 *
 * Increase ref count of handle for the calling client.  Handle 0 is
 * ignored.
 */
static void ref_handle (ChannelData *channel_data, guint32 handle)
{
	guint32 segment, idx;
	
	if(handle==0) {
		return;
	}
	
	_wapi_handle_segment (GUINT_TO_POINTER (handle), &segment, &idx);
	
	_wapi_shared_data[segment]->handles[idx].ref++;
	channel_data->open_handles[handle]++;
	
	LOGDEBUG ("%s: handle 0x%x ref now %d (%d this process)", __func__, handle,
		  _wapi_shared_data[segment]->handles[idx].ref,
		  channel_data->open_handles[handle]);
}

/*
 * unref_handle:
 * @channel_data: Channel data for calling client
 * @handle: handle to inc refcnt
 *
 * Decrease ref count of handle for the calling client. If global ref
 * count reaches 0 it is free'ed. Return TRUE if the local ref count
 * is 0. Handle 0 is ignored.
 */
static gboolean unref_handle (ChannelData *channel_data, guint32 handle)
{
	gboolean destroy=FALSE;
	guint32 segment, idx;
	
	if(handle==0) {
		return(FALSE);
	}
	
	if (channel_data->open_handles[handle] == 0) {
                g_warning("%s: unref on %d called when ref was already 0", __func__, handle);
                return TRUE;
        }

	_wapi_handle_segment (GUINT_TO_POINTER (handle), &segment, &idx);
	
	_wapi_shared_data[segment]->handles[idx].ref--;
	channel_data->open_handles[handle]--;
	
	LOGDEBUG ("%s: handle 0x%x ref now %d (%d this process)", __func__, handle,
		   _wapi_shared_data[segment]->handles[idx].ref,
		   channel_data->open_handles[handle]);

	if (_wapi_shared_data[segment]->handles[idx].ref == 0) {
		gboolean was_file;
		dev_t device = 0;
		ino_t inode = 0;
		
		/* This client has released the handle */
		destroy=TRUE;
	
		if (channel_data->open_handles[handle]!=0) {
			g_warning ("%s: per-process open_handles mismatch, set to %d, should be 0",__func__, channel_data->open_handles[handle]);
		}
		
		LOGDEBUG ("%s: Destroying handle 0x%x", __func__, handle);

		/* if this was a file handle, save the device and
		 * inode numbers so we can scan the share info data
		 * later to see if the last handle to a file has been
		 * closed, and delete the data if so.
		 */
		was_file = (_wapi_shared_data[segment]->handles[idx].type == WAPI_HANDLE_FILE);
		if (was_file) {
			struct _WapiHandle_file *file_handle;
			gboolean ok;
			
			ok = _wapi_lookup_handle (GUINT_TO_POINTER (handle),
						  WAPI_HANDLE_FILE,
						  (gpointer *)&file_handle,
						  NULL);
			if (ok == FALSE) {
				g_warning ("%s: error looking up file handle %x", __func__, handle);
			} else {
				device = file_handle->device;
				inode = file_handle->inode;
			}
		}
		
		_wapi_handle_ops_close_shared (GUINT_TO_POINTER (handle));
		
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
		mono_mutex_destroy (&_wapi_shared_data[segment]->handles[idx].signal_mutex);
		pthread_cond_destroy (&_wapi_shared_data[segment]->handles[idx].signal_cond);
#endif

		memset (&_wapi_shared_data[segment]->handles[idx].u, '\0', sizeof(_wapi_shared_data[segment]->handles[idx].u));
		_wapi_shared_data[segment]->handles[idx].type=WAPI_HANDLE_UNUSED;

		if (was_file) {
			check_sharing (device, inode);
		}
	}

	if(channel_data == daemon_channel_data) {
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
 * Create a new GIOChannel, and add it to the main loop event sources.
 */
static void add_fd(int fd, GMainContext *context)
{
	GIOChannel *io_channel;
	GSource *source;
	guint32 *refs;
	
	io_channel=g_io_channel_unix_new (fd);
	
	/* Turn off all encoding and buffering crap */
	g_io_channel_set_encoding (io_channel, NULL, NULL);
	g_io_channel_set_buffered (io_channel, FALSE);
	
	refs=g_new0 (guint32,_wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT);

	if(fd>=channels_length) {
		/* Add a bit of padding, so we dont resize for _every_
		 * new connection
		 */
		int old_len=channels_length * sizeof(ChannelData);
		
		channels_length=fd+10;
		if(channels==NULL) {
			channels=g_new0 (ChannelData, channels_length);
			/* We rely on the daemon channel being created first.
			 * That's safe, because every other channel is the
			 * result of an accept() on the daemon channel.
			 */
			daemon_channel_data = &channels[fd];
		} else {
			int daemon_index=daemon_channel_data - channels;

			/* Can't use g_renew here, because the unused
			 * elements must be NULL and g_renew doesn't
			 * initialise the memory it returns
			 */
			channels=_wapi_g_renew0 (channels, old_len, channels_length * sizeof(ChannelData));
			daemon_channel_data = channels + daemon_index;
		}

	}

	channels[fd].open_handles=refs;

	source = g_io_create_watch (io_channel, 
				    G_IO_IN|G_IO_ERR|G_IO_HUP|G_IO_NVAL);
	g_source_set_callback (source, (GSourceFunc)fd_activity, 
			       context, NULL);
	channels[fd].io_source=g_source_attach (source, context);
	g_source_unref (source);

	nfds++;
}

/*
 * rem_fd:
 * @channel: GIOChannel to close
 *
 * Closes the IO channel. Closes all handles that it may have open. If
 * only main_sock is left, the daemon is shut down.
 */
static void rem_fd(GIOChannel *channel, ChannelData *channel_data)
{
	guint32 handle_count;
	int i, j, fd;
	
	fd=g_io_channel_unix_get_fd (channel);
	
	if(fd == main_sock) {
		/* We shouldn't be deleting the daemon's fd */
		g_warning ("%s: Deleting daemon fd!", __func__);
		cleanup ();
		exit (-1);
	}
	
	LOGDEBUG ("%s: Removing client fd %d", __func__, fd);

	if (channel_data->io_source == 0) {
		LOGDEBUG ("%s: channel already closed for fd %d", __func__, fd);
		return;
	}


	g_io_channel_shutdown (channel, TRUE, NULL);
	g_source_remove (channel_data->io_source);
	g_io_channel_unref (channel);

	for(i=0;
	    i<_wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT;
	    i++) {
		handle_count=channel_data->open_handles[i];
		
		for(j=0; j<handle_count; j++) {
			LOGDEBUG ("%s: closing handle 0x%x for client at index %d", __func__, i, g_io_channel_unix_get_fd (channel));
			/* Ignore the hint to the client to destroy
			 * the handle private data
			 */
			unref_handle (channel_data, i);
		}
	}
	
	g_free (channel_data->open_handles);
	channel_data->open_handles=NULL;
	channel_data->io_source=0;
	
	nfds--;
	if(nfds==1) {
		/* Just the master socket left, so see if we can
		 * cleanup and exit
		 */
		maybe_exit ();
	}
}

static void sharemode_set (dev_t device, ino_t inode, guint32 sharemode,
			   guint32 access)
{
	ShareKey *sharekey;
	ShareData *sharedata;
	
	sharekey = g_new (ShareKey, 1);
	sharekey->device = device;
	sharekey->inode = inode;

	sharedata = g_new (ShareData, 1);
	sharedata->sharemode = sharemode;
	sharedata->access = access;
	
	/* Setting share mode to include all access bits is really
	 * removing the share info
	 */
	if (sharemode == (FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE)) {
		g_hash_table_remove (file_share_hash, sharekey);
	} else {
		g_hash_table_insert (file_share_hash, sharekey, sharedata);
	}
}

static gboolean sharemode_get (dev_t device, ino_t inode, guint32 *sharemode,
			       guint32 *access)
{
	ShareKey sharekey;
	ShareData *sharedata;
	
	sharekey.device = device;
	sharekey.inode = inode;
	
	sharedata = (ShareData *)g_hash_table_lookup (file_share_hash,
						       &sharekey);
	if (sharedata == NULL) {
		return(FALSE);
	}
	
	*sharemode = sharedata->sharemode;
	*access = sharedata->access;
	
	return(TRUE);
}

static gboolean share_compare (gpointer handle, gpointer user_data)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	ShareKey *sharekey = (ShareKey *)user_data;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				  (gpointer *)&file_handle, NULL);
	if (ok == FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__, handle);
		return(FALSE);
	}
	
	if (file_handle->device == sharekey->device &&
	    file_handle->inode == sharekey->inode) {
		LOGDEBUG ("%s: found one, handle %p", __func__, handle);
		return(TRUE);
	} else {
		return(FALSE);
	}
}

static void check_sharing (dev_t device, ino_t inode)
{
	ShareKey sharekey;
	gpointer file_handle;
	
	LOGDEBUG ("%s: Checking if anything has (dev 0x%llx, inode %lld) still open", __func__, device, inode);

	sharekey.device = device;
	sharekey.inode = inode;
	
	file_handle = _wapi_search_handle (WAPI_HANDLE_FILE, share_compare,
					   &sharekey, NULL, NULL);

	if (file_handle == NULL) {
		/* Delete this share info, as the last handle to it
		 * has been closed
		 */
		LOGDEBUG ("%s: Deleting share data for (dev 0x%llx inode %lld)", __func__, device, inode);
		
		g_hash_table_remove (file_share_hash, &sharekey);
	}
}

static gboolean process_compare (gpointer handle, gpointer user_data)
{
	struct _WapiHandle_process *process_handle;
	gboolean ok;
	pid_t pid;
	guint32 segment, idx;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
		g_warning ("%s: error looking up process handle %p", __func__, handle);
		return(FALSE);
	}

	_wapi_handle_segment (handle, &segment, &idx);
	if (_wapi_shared_data[segment]->handles[idx].signalled) {
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
	guint32 segment, idx;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle, NULL);
	if(ok==FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__, handle);
		return(FALSE);
	}

	if(thread_handle->process_handle==user_data) {
		/* Signal the handle.  Don't use
		 * _wapi_handle_set_signal_state() unless we have
		 * process-shared pthread support.
		 */
		LOGDEBUG ("%s: Set thread handle %p signalled, because its process died", __func__, handle);

		thread_handle->exitstatus=0;

#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
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
		_wapi_handle_segment (handle, &segment, &idx);
		_wapi_shared_data[segment]->handles[idx].signalled=TRUE;
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
	guint32 segment, idx;
	
	process_handle=_wapi_search_handle (WAPI_HANDLE_PROCESS,
					    process_compare,
					    GUINT_TO_POINTER (pid),
					    (gpointer *)&process_handle_data,
					    NULL);
	if(process_handle==0) {
		/*
		 * This may happen if we use Process.EnableRaisingEvents +
		 * process.Exited event and the parent has finished.
		 */
		LOGDEBUG ("%s: Couldn't find handle for process %d!", __func__, pid);
	} else {
		/* Signal the handle.  Don't use
		 * _wapi_handle_set_signal_state() unless we have
		 * process-shared pthread support.
		 */
		struct timeval tv;
		
		LOGDEBUG ("%s: Set process %d exitstatus to %d", __func__, pid,
			   WEXITSTATUS (status));
		
		/* If the child terminated due to the receipt of a signal,
		 * the exit status must be based on WTERMSIG, since WEXITSTATUS
		 * returns 0 in this case.
		 */
		if (WIFSIGNALED(status))
			process_handle_data->exitstatus=128 + WTERMSIG (status);
		else
			process_handle_data->exitstatus=WEXITSTATUS (status);

		/* Ignore errors */
		gettimeofday (&tv, NULL);
		_wapi_timeval_to_filetime (&tv,
					   &process_handle_data->exit_time);

#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
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
		_wapi_handle_segment (process_handle, &segment, &idx);
		_wapi_shared_data[segment]->handles[idx].signalled=TRUE;
#endif /* _POSIX_THREAD_PROCESS_SHARED */
	}

	/* Find all threads that have their process
	 * handle==process_handle.  Ignore the return value, all the
	 * work will be done in the compare func
	 */
	(void)_wapi_search_handle (WAPI_HANDLE_THREAD, process_thread_compare,
				   process_handle, NULL, NULL);

	unref_handle (daemon_channel_data,
		      GPOINTER_TO_UINT (process_handle_data->main_thread));
	unref_handle (daemon_channel_data, GPOINTER_TO_UINT (process_handle));
}

static void process_died (void)
{
	int status;
	pid_t pid;
	
	check_processes=FALSE;

	LOGDEBUG ("%s: Reaping processes", __func__);

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
			LOGDEBUG ( "%s: process %d reaped", __func__, pid);
			process_post_mortem (pid, status);
		}
	}
}


/*
 * send_reply:
 * @channel: channel to send reply to
 * @resp: Package to send
 *
 * Send a package to a client
 */
static void send_reply (GIOChannel *channel, WapiHandleResponse *resp)
{
	/* send message */
	_wapi_daemon_response (g_io_channel_unix_get_fd (channel), resp);
}

static guint32 new_handle_with_shared_check (WapiHandleType type)
{
	guint32 handle = 0;

	while ((handle = _wapi_handle_new_internal (type)) == 0) {
		/* Try and allocate a new shared segment, and have
		 * another go
		 */
		guint32 segment=_wapi_shared_data[0]->num_segments;
		int i;
		
		_wapi_handle_ensure_mapped (segment);
		if(_wapi_shared_data[segment]!=NULL) {
			/* Got a new segment */
			gulong old_len, new_len;
			
			old_len=_wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT * sizeof(guint32);
			_wapi_shared_data[0]->num_segments++;
			new_len=_wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT * sizeof(guint32);

			/* Need to expand all the handle reference
			 * count arrays
			 */

			for(i=0; i<channels_length; i++) {
				if(channels[i].open_handles!=NULL) {
					channels[i].open_handles=_wapi_g_renew0 (channels[i].open_handles, old_len, new_len);
				}
			}
		} else {
			/* Map failed.  Just return 0 meaning "out of
			 * handles"
			 */
			break;
		}
	}
	
	return(handle);
}

/*
 * process_new:
 * @channel: The client making the request
 * @channel_data: Our data for this channel
 * @type: type to init handle to
 *
 * Find a free handle and initialize it to 'type', increase refcnt and
 * send back a reply to the client.
 */
static void process_new (GIOChannel *channel, ChannelData *channel_data,
			 WapiHandleType type)
{
	guint32 handle;
	WapiHandleResponse resp={0};
	
	handle = new_handle_with_shared_check (type);
	
	/* handle might still be set to 0.  This is handled at the
	 * client end
	 */

	ref_handle (channel_data, handle);

	LOGDEBUG ("%s: returning new handle 0x%x", __func__, handle);

	resp.type=WapiHandleResponseType_New;
	resp.u.new.type=type;
	resp.u.new.handle=handle;
			
	send_reply (channel, &resp);
}

/*
 * process_open:
 * @channel: The client making the request
 * @channel_data: Our data for this channel
 * @handle: handle no.
 *
 * Increase refcnt on a previously created handle and send back a
 * response to the client.
 */
static void process_open (GIOChannel *channel, ChannelData *channel_data,
			  guint32 handle)
{
	WapiHandleResponse resp={0};
	guint32 segment, idx;
	struct _WapiHandleShared *shared;

	_wapi_handle_segment (GUINT_TO_POINTER (handle), &segment, &idx);
	shared=&_wapi_shared_data[segment]->handles[idx];
		
	if(shared->type!=WAPI_HANDLE_UNUSED && handle!=0) {
		ref_handle (channel_data, handle);

		LOGDEBUG ("%s: returning new handle 0x%x", __func__, handle);

		resp.type=WapiHandleResponseType_Open;
		resp.u.new.type=shared->type;
		resp.u.new.handle=handle;
			
		send_reply (channel, &resp);

		return;
	}

	resp.type=WapiHandleResponseType_Open;
	resp.u.new.handle=0;
			
	send_reply (channel, &resp);
}

/*
 * process_close:
 * @channel: The client making the request
 * @channel_data: Our data for this channel
 * @handle: handle no.
 *
 * Decrease refcnt on a previously created handle and send back a
 * response to the client with notice of it being destroyed.
 */
static void process_close (GIOChannel *channel, ChannelData *channel_data,
			   guint32 handle)
{
	WapiHandleResponse resp={0};
	
	resp.type=WapiHandleResponseType_Close;
	resp.u.close.destroy=unref_handle (channel_data, handle);

	LOGDEBUG ("%s: unreffing handle 0x%x", __func__, handle);

	send_reply (channel, &resp);
}

/*
 * process_scratch:
 * @channel: The client making the request
 * @length: allocate this much scratch space
 *
 * Allocate some scratch space and send a reply to the client.
 */
static void process_scratch (GIOChannel *channel, guint32 length)
{
	WapiHandleResponse resp={0};
	
	resp.type=WapiHandleResponseType_Scratch;
	resp.u.scratch.idx=_wapi_handle_scratch_store_internal (length, &resp.u.scratch.remap);

	LOGDEBUG ("%s: allocating scratch index 0x%x", __func__, resp.u.scratch.idx);
			
	send_reply (channel, &resp);
}

/*
 * process_scratch_free:
 * @channel: The client making the request
 * @scratch_idx: deallocate this scratch space
 *
 * Deallocate scratch space and send a reply to the client.
 */
static void process_scratch_free (GIOChannel *channel, guint32 scratch_idx)
{
	WapiHandleResponse resp={0};
	
	resp.type=WapiHandleResponseType_ScratchFree;
	_wapi_handle_scratch_delete_internal (scratch_idx);

	LOGDEBUG ("%s: deleting scratch index 0x%x", __func__, scratch_idx);
			
	send_reply (channel, &resp);
}

/*
 * process_process_kill:
 * @channel: The client making the request
 * @process_kill: pid and signal to send to the pid.
 *
 * Sends the specified signal to the process.
 */
static void
process_process_kill (GIOChannel *channel,
		      WapiHandleRequest_ProcessKill process_kill)
{
	WapiHandleResponse resp = {0};

	resp.type = WapiHandleResponseType_ProcessKill;

	LOGDEBUG ("%s: kill (%d, %d)", __func__, process_kill.pid, process_kill.signo);

	if (kill (process_kill.pid, process_kill.signo) == -1) {
		resp.u.process_kill.err = errno;
		LOGDEBUG ("%s: kill (%d, %d) failed: %d", __func__, process_kill.pid, process_kill.signo, resp.u.process_kill.err);
	}

	send_reply (channel, &resp);
}

/*
 * process_process_fork:
 * @channel: The client making the request
 * @process_fork: Describes the process to fork
 * @fds: stdin, stdout, and stderr for the new process
 *
 * Forks a new process, and returns the process and thread data to the
 * client.
 */
static void process_process_fork (GIOChannel *channel, ChannelData *channel_data,
				  WapiHandleRequest_ProcessFork process_fork,
				  int *fds)
{
	WapiHandleResponse resp={0};
	guint32 process_handle, thread_handle;
	struct _WapiHandle_process *process_handle_data;
	struct _WapiHandle_thread *thread_handle_data;
	pid_t pid = 0;
	
	resp.type=WapiHandleResponseType_ProcessFork;
	
	/* Create handles first, so the child process can store exec
	 * errors.  Either handle might be set to 0, if this happens
	 * just reply to the client without bothering to fork.  The
	 * client must check if either handle is 0 and take
	 * appropriate error handling action.
	 */
	process_handle = new_handle_with_shared_check (WAPI_HANDLE_PROCESS);
	ref_handle (daemon_channel_data, process_handle);
	ref_handle (channel_data, process_handle);
	
	thread_handle = new_handle_with_shared_check (WAPI_HANDLE_THREAD);
	ref_handle (daemon_channel_data, thread_handle);
	ref_handle (channel_data, thread_handle);
	
	if(process_handle==0 || thread_handle==0) {
		/* unref_handle() copes with the handle being 0 */
		unref_handle (daemon_channel_data, process_handle);
		unref_handle (channel_data, process_handle);
		unref_handle (daemon_channel_data, thread_handle);
		unref_handle (channel_data, thread_handle);
		process_handle=0;
		thread_handle=0;
	} else {
		char *cmd=NULL, *dir=NULL, **argv, **env;
		GError *gerr=NULL;
		gboolean ret;
		struct timeval tv;
		
		/* Get usable copies of the cmd, dir and env now
		 * rather than in the child process.  This is to
		 * prevent the race condition where the parent can
		 * return the reply to the client, which then promptly
		 * deletes the scratch data before the new process
		 * gets to see it.  Also explode argv here so we can
		 * use it to set the process name.
		 */
		cmd=_wapi_handle_scratch_lookup (process_fork.cmd);
		dir=_wapi_handle_scratch_lookup (process_fork.dir);
		env=_wapi_handle_scratch_lookup_string_array (process_fork.env);

		_wapi_lookup_handle (GUINT_TO_POINTER (process_handle),
				     WAPI_HANDLE_PROCESS,
				     (gpointer *)&process_handle_data,
				     NULL);

		_wapi_lookup_handle (GUINT_TO_POINTER (thread_handle),
				     WAPI_HANDLE_THREAD,
				     (gpointer *)&thread_handle_data,
				     NULL);
		
		ret=g_shell_parse_argv (cmd, NULL, &argv, &gerr);
		if(ret==FALSE) {
			/* FIXME: Could do something with the
			 * GError here
			 */
			process_handle_data->exec_errno=gerr->code;
		} else {
			LOGDEBUG ("%s: forking", __func__);

			/* Fork, exec cmd with args and optional env,
			 * and return the handles with pid and blank
			 * thread id
			 */
			pid=fork ();
			if(pid==-1) {
				process_handle_data->exec_errno=errno;
			} else if (pid==0) {
				/* child */
				int i;
				
				/* should we detach from the process
				 * group? We're already running
				 * without a controlling tty...
				 */

				/* Connect stdin, stdout and stderr */
				dup2 (fds[0], 0);
				dup2 (fds[1], 1);
				dup2 (fds[2], 2);

				if(process_fork.inherit!=TRUE) {
					/* FIXME: do something here */
				}
				
				/* Close all file descriptors */
				for (i = getdtablesize () - 1; i > 2; i--) {
					close (i);
				}
			
				/* pass process and thread handle info
				 * to the child, so it doesn't have to
				 * do an expensive search over the
				 * whole list
				 */
				{
					guint env_count=0;
					
					while(env[env_count]!=NULL) {
						env_count++;
					}

					env=(char **)g_renew (char **, env, env_count+3);
					
					env[env_count]=g_strdup_printf ("_WAPI_PROCESS_HANDLE=%d", process_handle);
					env[env_count+1]=g_strdup_printf ("_WAPI_THREAD_HANDLE=%d", thread_handle);
					env[env_count+2]=NULL;
				}

#ifdef DEBUG
				LOGDEBUG ("%s: exec()ing [%s] in dir [%s]", __func__, cmd, dir);
				{
					i=0;
					while(argv[i]!=NULL) {
						LOGDEBUG ("arg %d: [%s]", i, argv[i]);
						i++;
					}

					i=0;
					while(env[i]!=NULL) {
						LOGDEBUG ("env %d: [%s]", i, env[i]);
						i++;
					}
				}
#endif
			
				/* set cwd */
				if(chdir (dir)==-1) {
					process_handle_data->exec_errno=errno;
					exit (-1);
				}
				
				/* exec */
				execve (argv[0], argv, env);
		
				/* bummer! */
				process_handle_data->exec_errno=errno;
				exit (-1);
			}
		}
		/* parent */

		/* store process name, based on the last section of the cmd */
		{
			char *slash;
			
			/* This should never fail, but it seems it can...
			 */
			if (argv[0] != NULL) {
				slash=strrchr (argv[0], '/');
			
				if(slash!=NULL) {
					process_handle_data->proc_name=_wapi_handle_scratch_store (slash+1, strlen (slash+1));
				} else {
					process_handle_data->proc_name=_wapi_handle_scratch_store (argv[0], strlen (argv[0]));
				}
			} else {
				process_handle_data->proc_name = _wapi_handle_scratch_store (cmd, strlen(cmd));
			}
		}
		
		/* These seem to be the defaults on w2k */
		process_handle_data->min_working_set=204800;
		process_handle_data->max_working_set=1413120;
		
		if(cmd!=NULL) {
			g_free (cmd);
		}
		if(dir!=NULL) {
			g_free (dir);
		}
		g_strfreev (argv);
		g_strfreev (env);
		
		/* store pid */
		process_handle_data->id=pid;
		process_handle_data->main_thread=GUINT_TO_POINTER (thread_handle);
		/* Ignore errors */
		gettimeofday (&tv, NULL);
		_wapi_timeval_to_filetime (&tv,
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

	send_reply (channel, &resp);
}

/*
 * process_set_share:
 * @channel: The client making the request
 * @channel_data: The channel data
 * @set_share: Set share data passed from the client
 *
 * Sets file share info
 */
static void process_set_share (GIOChannel *channel, ChannelData *channel_data,
			       WapiHandleRequest_SetShare set_share)
{
	WapiHandleResponse resp = {0};

	resp.type = WapiHandleResponseType_SetShare;
	
	LOGDEBUG ("%s: Setting share for file (dev:0x%llx, ino:%lld) mode 0x%x access 0x%x", __func__, set_share.device, set_share.inode, set_share.sharemode, set_share.access);
	
	sharemode_set (set_share.device, set_share.inode, set_share.sharemode,
		       set_share.access);
	
	send_reply (channel, &resp);
}

/*
 * process_get_or_set_share:
 * @channel: The client making the request
 * @channel_data: The channel data
 * @get_share: GetOrSetShare data passed from the client
 *
 * Gets a file share status, and sets the status if it doesn't already
 * exist
 */
static void process_get_or_set_share (GIOChannel *channel,
				      ChannelData *channel_data,
				      WapiHandleRequest_GetOrSetShare get_share)
{
	WapiHandleResponse resp = {0};
	
	resp.type = WapiHandleResponseType_GetOrSetShare;
	
	LOGDEBUG ("%s: Getting share status for file (dev:0x%llx, ino:%lld)", __func__, get_share.device, get_share.inode);

	resp.u.get_or_set_share.exists = sharemode_get (get_share.device, get_share.inode, &resp.u.get_or_set_share.sharemode, &resp.u.get_or_set_share.access);
	
	if (resp.u.get_or_set_share.exists) {
		LOGDEBUG ("%s: Share mode: 0x%x", __func__, resp.u.get_or_set_share.sharemode);
	} else {
		LOGDEBUG ("%s: file share info not already known, setting", __func__);
		sharemode_set (get_share.device, get_share.inode,
			       get_share.new_sharemode, get_share.new_access);
	}
	
	send_reply (channel, &resp);
}

/*
 * read_message:
 * @channel: The client to read the request from
 * @open_handles: An array of handles referenced by this client
 *
 * Read a message (A WapiHandleRequest) from a client and dispatch
 * whatever it wants to the process_* calls.  Return TRUE if the message
 * was read successfully, FALSE otherwise.
 */
static gboolean read_message (GIOChannel *channel, ChannelData *channel_data)
{
	WapiHandleRequest req;
	int fds[3]={0, 1, 2};
	int ret;
	gboolean has_fds=FALSE;
	
	/* Reading data */
	ret=_wapi_daemon_request (g_io_channel_unix_get_fd (channel), &req,
				  fds, &has_fds);
	if(ret==0) {
		/* Other end went away */
		LOGDEBUG ("Read 0 bytes on fd %d, closing it",
			   g_io_channel_unix_get_fd (channel));
		rem_fd (channel, channel_data);
		return(FALSE);
	}
	
	LOGDEBUG ("Process request %d", req.type);
	switch(req.type) {
	case WapiHandleRequestType_New:
		process_new (channel, channel_data, req.u.new.type);
		break;
	case WapiHandleRequestType_Open:
#ifdef DEBUG
		g_assert(req.u.open.handle < _wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT);
#endif
		process_open (channel, channel_data, req.u.open.handle);
		break;
	case WapiHandleRequestType_Close:
#ifdef DEBUG
		g_assert(req.u.close.handle < _wapi_shared_data[0]->num_segments * _WAPI_HANDLES_PER_SEGMENT);
#endif
		process_close (channel, channel_data, req.u.close.handle);
		break;
	case WapiHandleRequestType_Scratch:
		process_scratch (channel, req.u.scratch.length);
		break;
	case WapiHandleRequestType_ScratchFree:
		process_scratch_free (channel, req.u.scratch_free.idx);
		break;
	case WapiHandleRequestType_ProcessFork:
		process_process_fork (channel, channel_data,
				      req.u.process_fork, fds);
		break;
	case WapiHandleRequestType_ProcessKill:
		process_process_kill (channel, req.u.process_kill);
		break;
	case WapiHandleRequestType_SetShare:
		process_set_share (channel, channel_data, req.u.set_share);
		break;
	case WapiHandleRequestType_GetOrSetShare:
		process_get_or_set_share (channel, channel_data,
					  req.u.get_or_set_share);
		break;
	case WapiHandleRequestType_Error:
		/* fall through */
	default:
		/* Catch bogus requests */
		/* FIXME: call rem_fd? */
		break;
	}

	if(has_fds==TRUE) {
		LOGDEBUG ("%s: closing %d", __func__, fds[0]);
		LOGDEBUG ("%s: closing %d", __func__, fds[1]);
		LOGDEBUG ("%s: closing %d", __func__, fds[2]);
		
		close (fds[0]);
		close (fds[1]);
		close (fds[2]);
	}

	return(TRUE);
}

/*
 * fd_activity:
 * @channel: The IO channel that is active
 * @condition: The condition that has been satisfied
 * @data: A pointer to an array of handles referenced by this client
 *
 * The callback called by the main loop when there is activity on an
 * IO channel.
 */
static gboolean fd_activity (GIOChannel *channel, GIOCondition condition,
			     gpointer data)
{
	int fd=g_io_channel_unix_get_fd (channel);
	ChannelData *channel_data=&channels[fd];
	GMainContext *context=data;
	
	if(condition & (G_IO_HUP | G_IO_ERR | G_IO_NVAL)) {
		LOGDEBUG ("fd %d error", fd);
		rem_fd (channel, channel_data);
		return(FALSE);
	}

	if(condition & (_IO_PRI)) {
		if(fd==main_sock) {
			int newsock;
			struct sockaddr addr;
			socklen_t addrlen=sizeof(struct sockaddr);
			
			newsock=accept (main_sock, &addr, &addrlen);
			if(newsock==-1) {
				g_critical ("%s accept error: %s", __func__, g_strerror (errno));
				cleanup ();
				exit (-1);
			}

			LOGDEBUG ("accept returning %d", newsock);
			add_fd (newsock, context);
		} else {
			LOGDEBUG ("reading data on fd %d", fd);

			return(read_message (channel, channel_data));
		}
		return(TRUE);
	}
	
	return(FALSE);	/* remove source */
}

/*
 * _wapi_daemon_main:
 *
 * Open socket, create shared mem segment and begin listening for
 * clients.
 */
void _wapi_daemon_main(gpointer data, gpointer scratch)
{
	struct sockaddr_un main_socket_address;
	int ret;
	GMainContext *context;

	LOGDEBUG ("Starting up...");

	_wapi_shared_data[0]=data;
	_wapi_shared_scratch=scratch;
	_wapi_shared_scratch->is_shared=TRUE;
	
	/* Note that we've got the starting segment already */
	_wapi_shared_data[0]->num_segments=1;
	_wapi_shm_mapped_segments=1;

	_wapi_fd_offset_table_size=getdtablesize ();
	_wapi_shared_data[0]->fd_offset_table_size = _wapi_fd_offset_table_size;
	
	startup ();
	
	main_sock=socket(PF_UNIX, SOCK_STREAM, 0);

	main_socket_address.sun_family=AF_UNIX;
	memcpy(main_socket_address.sun_path, _wapi_shared_data[0]->daemon,
	       MONO_SIZEOF_SUNPATH);

	ret=bind(main_sock, (struct sockaddr *)&main_socket_address,
		 sizeof(struct sockaddr_un));
	if(ret==-1) {
		g_critical ("bind failed: %s", g_strerror (errno));
		_wapi_shared_data[0]->daemon_running=DAEMON_DIED_AT_STARTUP;
		exit(-1);
	}

	LOGDEBUG("bound");

	ret=listen(main_sock, 5);
	if(ret==-1) {
		g_critical ("listen failed: %s", g_strerror (errno));
		_wapi_shared_data[0]->daemon_running=DAEMON_DIED_AT_STARTUP;
		exit(-1);
	}
	LOGDEBUG("listening");

	context = g_main_context_new ();

	add_fd(main_sock, context);

	/* We're finished setting up, let everyone else know we're
	 * ready.  From now on, it's up to us to delete the shared
	 * memory segment when appropriate.
	 */
	_wapi_shared_data[0]->daemon_running=DAEMON_RUNNING;

	while(TRUE) {
		if(check_processes==TRUE) {
			process_died ();
		}
		
		LOGDEBUG ("polling");

		/* Block until something happens. We don't use
		 * g_main_loop_run() because we rely on the SIGCHLD
		 * signal interrupting poll() so we can reap child
		 * processes as soon as they die, without burning cpu
		 * time by polling the flag.
		 */
		g_main_context_iteration (context, TRUE);
	}
}
