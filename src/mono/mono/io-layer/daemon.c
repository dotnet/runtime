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

#include <mono/io-layer/io-layer.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/daemon-messages.h>

#undef DEBUG

static struct pollfd *pollfds=NULL;
static int nfds=0, maxfds=0;
static gpointer *handle_refs=NULL;
static int main_sock;

/* Deletes the shared memory segment.  If we're exiting on error,
 * clients will get EPIPEs.
 */
static void cleanup (void)
{
	_wapi_shm_destroy ();
}

static void signal_handler (int unused)
{
	cleanup ();
	exit (-1);
}

static void startup (void)
{
	struct sigaction sa;
	
	sa.sa_handler=signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags=0;
	sigaction (SIGINT, &sa, NULL);
	sigaction (SIGTERM, &sa, NULL);
	
	_wapi_shared_data=_wapi_shm_attach (TRUE);

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

static void ref_handle (guint32 idx, guint32 handle)
{
	guint32 *open_handles=handle_refs[idx];
	
	_wapi_shared_data->handles[handle].ref++;
	open_handles[handle]++;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": handle 0x%x ref now %d (%d this process)", handle,
		   _wapi_shared_data->handles[handle].ref,
		   open_handles[handle]);
#endif
}

static gboolean unref_handle (guint32 idx, guint32 handle)
{
	guint32 *open_handles=handle_refs[idx];
	gboolean destroy=FALSE;
	
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

	return(destroy);
}

static void add_fd(int fd)
{
	if(nfds==maxfds) {
		/* extend the array */
		maxfds+=10;
		pollfds=g_renew (struct pollfd, pollfds, maxfds);
		handle_refs=g_renew (gpointer, handle_refs, maxfds);
	}

	pollfds[nfds].fd=fd;
	pollfds[nfds].events=POLLIN;
	pollfds[nfds].revents=0;
	
	handle_refs[nfds]=g_new0 (guint32, _WAPI_MAX_HANDLES);

	nfds++;
}

static void rem_fd(int idx)
{
	guint32 *open_handles=handle_refs[idx], handle_count;
	int i, j;
	
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
		/* Just the master socket left, so cleanup and exit */
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Byebye");
#endif

		cleanup ();
		exit (0);
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

static void send_reply (guint32 idx, WapiHandleResponse *resp)
{
	/* send message */
	_wapi_daemon_response (pollfds[idx].fd, resp);
}

static void process_new (guint32 idx, WapiHandleType type)
{
	guint32 handle;
	WapiHandleResponse resp;
	
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

static void process_open (guint32 idx, guint32 handle)
{
	WapiHandleResponse resp;
	struct _WapiHandleShared *shared=&_wapi_shared_data->handles[handle];
		
	if(shared->type!=WAPI_HANDLE_UNUSED) {
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

static void read_message (guint32 idx)
{
	WapiHandleRequest req;
	
	/* Reading data */
	_wapi_daemon_request (pollfds[idx].fd, &req);
	switch(req.type) {
	case WapiHandleRequestType_New:
		process_new (idx, req.u.new.type);
		break;
	case WapiHandleRequestType_Open:
		process_open (idx, req.u.open.handle);
		break;
	case WapiHandleRequestType_Close:
		process_close (idx, req.u.close.handle);
		break;
	case WapiHandleRequestType_Scratch:
		process_scratch (idx, req.u.scratch.length);
		break;
	case WapiHandleRequestType_ScratchFree:
		process_scratch_free (idx, req.u.scratch_free.idx);
		break;
	}
}

int main(int argc, char **argv)
{
	struct sockaddr_un sun;
	int ret;

#ifdef DEBUG
	g_message ("Starting up...");
#endif

	startup ();
	
	main_sock=socket(PF_UNIX, SOCK_STREAM, 0);

	sun.sun_family=AF_UNIX;
	memcpy(sun.sun_path, _wapi_shared_data->daemon, 108);

	ret=bind(main_sock, (struct sockaddr *)&sun,
		 sizeof(struct sockaddr_un));
	if(ret==-1) {
		g_warning ("bind failed: %s", strerror (errno));
		_wapi_shared_data->daemon_running=2;
		exit(-1);
	}

#ifdef DEBUG
	g_message("bound");
#endif

	ret=listen(main_sock, 5);
	if(ret==-1) {
		g_warning ("listen failed: %s", strerror (errno));
		_wapi_shared_data->daemon_running=2;
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
	_wapi_shared_data->daemon_running=1;

	while(TRUE) {
		int i;

#ifdef DEBUG
		g_message ("polling");
#endif

		/* Block until something happens */
		ret=poll(pollfds, nfds, -1);
		if(ret==-1) {
			g_message ("poll error: %s", strerror (errno));
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
				if(pollfds[i].fd==main_sock) {
					int newsock;
					struct sockaddr addr;
					socklen_t addrlen=sizeof(struct sockaddr);
					newsock=accept(main_sock, &addr,
						       &addrlen);
					if(newsock==-1) {
						g_message("accept error: %s",
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
