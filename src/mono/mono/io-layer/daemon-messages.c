/*
 * daemon-messages.c:  Communications to and from the handle daemon
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>
#include <string.h>

#ifndef HAVE_MSG_NOSIGNAL
#include <signal.h>
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/daemon-messages.h>

/* Send request on fd, wait for response (called by applications, not
 * the daemon, indirectly through _wapi_daemon_request_response and
 * _wapi_daemon_request_response_with_fds)
 */
static void _wapi_daemon_request_response_internal (int fd,
						    struct msghdr *msg,
						    WapiHandleResponse *resp)
{
	static pthread_mutex_t req_mutex=PTHREAD_MUTEX_INITIALIZER;
	int ret;
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);
#endif

	/* Serialise requests to the daemon from the same process.  We
	 * rely on request turnaround time being minimal anyway, so
	 * performance shouldnt suffer from the mutex.
	 */
	pthread_mutex_lock (&req_mutex);
	
#ifdef HAVE_MSG_NOSIGNAL
	ret=sendmsg (fd, msg, MSG_NOSIGNAL);
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	ret=sendmsg (fd, msg, 0);
#endif
	if(ret!=sizeof(WapiHandleRequest)) {
		if(errno==EPIPE) {
			g_critical (G_GNUC_PRETTY_FUNCTION ": The handle daemon vanished!");
			exit (-1);
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION ": Send error: %s",
				   strerror (errno));
			g_assert_not_reached ();
		}
	}

#ifdef HAVE_MSG_NOSIGNAL
	ret=recv (fd, resp, sizeof(WapiHandleResponse), MSG_NOSIGNAL);
#else
	ret=recv (fd, resp, sizeof(WapiHandleResponse), 0);
	signal (SIGPIPE, old_sigpipe);
#endif
	if(ret==-1) {
		if(errno==EPIPE) {
			g_critical (G_GNUC_PRETTY_FUNCTION ": The handle daemon vanished!");
			exit (-1);
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION ": Send error: %s",
				   strerror (errno));
			g_assert_not_reached ();
		}
	}

	pthread_mutex_unlock (&req_mutex);
}

/* Send request on fd with filedescriptors, wait for response (called
 * by applications, not the daemon)
 */
void _wapi_daemon_request_response_with_fds (int fd, WapiHandleRequest *req,
					     WapiHandleResponse *resp,
					     int in_fd, int out_fd, int err_fd)
{
	struct msghdr msg={0};
	struct cmsghdr *cmsg;
	struct iovec iov;
	char cmsgdata[CMSG_SPACE (sizeof(int)*3)];
	int *fdptr;
	
	msg.msg_name=NULL;
	msg.msg_namelen=0;
	msg.msg_iov=&iov;
	msg.msg_iovlen=1;
	msg.msg_control=cmsgdata;
	msg.msg_controllen=sizeof(cmsgdata);
	msg.msg_flags=0;

	iov.iov_base=req;
	iov.iov_len=sizeof(WapiHandleRequest);

	cmsg=CMSG_FIRSTHDR (&msg);
	cmsg->cmsg_len=CMSG_LEN (sizeof(int)*3);
	cmsg->cmsg_level=SOL_SOCKET;
	cmsg->cmsg_type=SCM_RIGHTS;
	fdptr=(int *)CMSG_DATA (cmsg);
	fdptr[0]=in_fd;
	fdptr[1]=out_fd;
	fdptr[2]=err_fd;

	msg.msg_controllen=CMSG_SPACE (sizeof(int)*3);

	_wapi_daemon_request_response_internal (fd, &msg, resp);
}

/* Send request on fd, wait for response (called by applications, not
 * the daemon)
 */
void _wapi_daemon_request_response (int fd, WapiHandleRequest *req,
				    WapiHandleResponse *resp)
{
	struct msghdr msg={0};
	struct iovec iov;
	
	msg.msg_name=NULL;
	msg.msg_namelen=0;
	msg.msg_iov=&iov;
	msg.msg_iovlen=1;
	msg.msg_control=NULL;
	msg.msg_controllen=0;
	msg.msg_flags=0;

	iov.iov_base=req;
	iov.iov_len=sizeof(WapiHandleRequest);

	_wapi_daemon_request_response_internal (fd, &msg, resp);
}

/* Read request on fd (called by the daemon) */
void _wapi_daemon_request (int fd, WapiHandleRequest *req, int *fds,
			   gboolean *has_fds)
{
	int ret;
	struct msghdr msg;
	struct iovec iov;
	struct cmsghdr *cmsg;
	guchar cmsgdata[CMSG_SPACE (sizeof(int)*3)];
		    
	msg.msg_name=NULL;
	msg.msg_namelen=0;
	msg.msg_iov=&iov;
	msg.msg_iovlen=1;
	msg.msg_control=cmsgdata;
	msg.msg_controllen=sizeof(cmsgdata);
	msg.msg_flags=0;
	iov.iov_base=req;
	iov.iov_len=sizeof(WapiHandleRequest);
	
#ifdef HAVE_MSG_NOSIGNAL
	ret=recvmsg (fd, &msg, MSG_NOSIGNAL);
#else
	ret=recvmsg (fd, &msg, MSG_NOSIGNAL);
#endif
	if(ret==-1 || ret!= sizeof(WapiHandleRequest)) {
		/* Make sure we dont do anything with this response */
		req->type=WapiHandleRequestType_Error;
		
#ifdef DEBUG
		g_warning (G_GNUC_PRETTY_FUNCTION ": Recv error: %s",
			   strerror (errno));
#endif
		/* The next loop around poll() should tidy up */
	}

#ifdef DEBUG
	if(msg.msg_flags & MSG_OOB) {
		g_message (G_GNUC_PRETTY_FUNCTION ": OOB data received");
	}
	if(msg.msg_flags & MSG_CTRUNC) {
		g_message (G_GNUC_PRETTY_FUNCTION ": ancillary data was truncated");
	}
	g_message (G_GNUC_PRETTY_FUNCTION ": msg.msg_controllen=%d",
		   msg.msg_controllen);
#endif

	cmsg=CMSG_FIRSTHDR (&msg);
	if(cmsg!=NULL && cmsg->cmsg_level==SOL_SOCKET &&
	   cmsg->cmsg_type==SCM_RIGHTS) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": cmsg->cmsg_len=%d",
			   cmsg->cmsg_len);
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": cmsg->level=%d cmsg->type=%d", cmsg->cmsg_level,
			   cmsg->cmsg_type);
#endif

		memcpy (fds, (int *)CMSG_DATA (cmsg), sizeof(int)*3);
		*has_fds=TRUE;

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": fd[0]=%d, fd[1]=%d, fd[2]=%d", fds[0], fds[1],
			   fds[2]);
#endif
	} else {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": no ancillary data");
#endif
		*has_fds=FALSE;
	}
}

/* Send response on fd (called by the daemon) */
void _wapi_daemon_response (int fd, WapiHandleResponse *resp)
{
	int ret;
	
#ifdef HAVE_MSG_NOSIGNAL
	ret=send (fd, resp, sizeof(WapiHandleResponse), MSG_NOSIGNAL);
#else
	ret=send (fd, resp, sizeof(WapiHandleResponse), 0);
#endif
#ifdef DEBUG
	if(ret==-1 || ret != sizeof(WapiHandleResponse)) {
		g_warning (G_GNUC_PRETTY_FUNCTION ": Send error: %s",
			   strerror (errno));
		/* The next loop around poll() should tidy up */
	}
#endif
}
