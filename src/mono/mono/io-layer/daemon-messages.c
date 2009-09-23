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
#include <sys/types.h>
#include <sys/socket.h>
/* Freebsd needs this included explicitly, but it doesn't hurt on Linux */
#ifdef HAVE_SYS_UIO_H
#    include <sys/uio.h>
#endif

#ifndef HAVE_MSG_NOSIGNAL
#include <signal.h>
#endif

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/daemon-messages.h>

/* Solaris doesn't define these */
#ifndef CMSG_LEN
#define CMSG_LEN(size)    (sizeof (struct cmsghdr) + (size))
#endif
#ifndef CMSG_SPACE
#define CMSG_SPACE(size)  (sizeof (struct cmsghdr) + (size))
#endif

#define LOGDEBUG(...)
// #define LOGDEBUG(...) g_message(__VA_ARGS__)

static mono_mutex_t req_mutex;
static mono_once_t attr_key_once = MONO_ONCE_INIT;
static mono_mutexattr_t attr;

static void attr_init (void)
{
	int ret;

	ret = mono_mutexattr_init (&attr);
	g_assert (ret == 0);

	ret = mono_mutexattr_settype (&attr, MONO_MUTEX_RECURSIVE);
	g_assert (ret == 0);

	ret = mono_mutex_init (&req_mutex, &attr);
	g_assert (ret == 0);
}

/* Send request on fd, wait for response (called by applications, not
 * the daemon, indirectly through _wapi_daemon_request_response and
 * _wapi_daemon_request_response_with_fds)
 */
static void _wapi_daemon_request_response_internal (int fd,
						    struct msghdr *msg,
						    WapiHandleResponse *resp)
{
	int ret;
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);
#endif

	mono_once (&attr_key_once, attr_init);

	/* Serialise requests to the daemon from the same process.  We
	 * rely on request turnaround time being minimal anyway, so
	 * performance shouldnt suffer from the mutex.
	 */
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&req_mutex);
	ret = mono_mutex_lock (&req_mutex);
	g_assert (ret == 0);
	
#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret=sendmsg (fd, msg, MSG_NOSIGNAL);
	}
	while (ret==-1 && errno==EINTR);
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	do {
		ret=sendmsg (fd, msg, 0);
	}
	while (ret==-1 && errno==EINTR);
#endif

	if(ret!=sizeof(WapiHandleRequest)) {
		if(errno==EPIPE) {
			g_critical ("%s: The handle daemon vanished!", __func__);
			exit (-1);
		} else {
			g_warning ("%s: Send error: %s", __func__,
				   strerror (errno));
			g_assert_not_reached ();
		}
	}

#ifdef HAVE_MSG_NOSIGNAL
	do {
		ret=recv (fd, resp, sizeof(WapiHandleResponse), MSG_NOSIGNAL);
	}
	while (ret==-1 && errno==EINTR);
#else
	do {
		ret=recv (fd, resp, sizeof(WapiHandleResponse), 0);
	}
	while (ret==-1 && errno==EINTR);
	signal (SIGPIPE, old_sigpipe);
#endif

	if(ret==-1) {
		if(errno==EPIPE) {
			g_critical ("%s: The handle daemon vanished!", __func__);
			exit (-1);
		} else {
			g_warning ("%s: Send error: %s", __func__, strerror (errno));
			g_assert_not_reached ();
		}
	}
		
	ret = mono_mutex_unlock (&req_mutex);
	g_assert (ret == 0);
	
	pthread_cleanup_pop (0);
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
int _wapi_daemon_request (int fd, WapiHandleRequest *req, int *fds,
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
	
	do {
#ifdef HAVE_MSG_NOSIGNAL
		ret=recvmsg (fd, &msg, MSG_NOSIGNAL);
#else
		ret=recvmsg (fd, &msg, 0);
#endif
	}
	while (ret==-1 && errno==EINTR);

	if(ret==-1 || ret!= sizeof(WapiHandleRequest)) {
		/* Make sure we dont do anything with this response */
		req->type=WapiHandleRequestType_Error;
		
		g_warning ("%s: Recv error: %s", __func__, strerror (errno));
		/* The next loop around poll() should tidy up */
	}

#ifdef DEBUG
	if(msg.msg_flags & MSG_OOB) {
		g_message ("%s: OOB data received", __func__);
	}
	if(msg.msg_flags & MSG_CTRUNC) {
		g_message ("%s: ancillary data was truncated", __func__);
	}
	g_message ("%s: msg.msg_controllen=%d", __func__, msg.msg_controllen);
#endif

	cmsg=CMSG_FIRSTHDR (&msg);
	if(cmsg!=NULL && cmsg->cmsg_level==SOL_SOCKET &&
	   cmsg->cmsg_type==SCM_RIGHTS) {
		LOGDEBUG ("%s: cmsg->cmsg_len=%d", __func__, cmsg->cmsg_len);
		LOGDEBUG ("%s: cmsg->level=%d cmsg->type=%d", __func__, cmsg->cmsg_level, cmsg->cmsg_type);

		memcpy (fds, (int *)CMSG_DATA (cmsg), sizeof(int)*3);
		*has_fds=TRUE;

		LOGDEBUG ("%s: fd[0]=%d, fd[1]=%d, fd[2]=%d", __func__, fds[0], fds[1], fds[2]);
	} else {
		LOGDEBUG ("%s: no ancillary data", __func__);
		*has_fds=FALSE;
	}

	return(ret);
}

/* Send response on fd (called by the daemon) */
int _wapi_daemon_response (int fd, WapiHandleResponse *resp)
{
	int ret;

	do {
#ifdef HAVE_MSG_NOSIGNAL
		ret=send (fd, resp, sizeof(WapiHandleResponse), MSG_NOSIGNAL);
#else
		ret=send (fd, resp, sizeof(WapiHandleResponse), 0);
#endif
	}
	while (ret==-1 && errno==EINTR);

#ifdef DEBUG
	if(ret==-1 || ret != sizeof(WapiHandleResponse)) {
		g_warning ("%s: Send error: %s", __func__, strerror (errno));
		/* The next loop around poll() should tidy up */
	}
#endif

	return(ret);
}
