/*
 * daemon-messages.h:  Communications to and from the handle daemon
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
 * the daemon)
*/
void _wapi_daemon_request_response (int fd, WapiHandleRequest *req,
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
	ret=send (fd, req, sizeof(WapiHandleRequest), MSG_NOSIGNAL);
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	ret=send (fd, req, sizeof(WapiHandleRequest), 0);
#endif
	if(ret!=sizeof(WapiHandleRequest)) {
		if(errno==EPIPE) {
			g_warning (G_GNUC_PRETTY_FUNCTION ": The handle daemon vanished!");
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
			g_warning (G_GNUC_PRETTY_FUNCTION ": The handle daemon vanished!");
			exit (-1);
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION ": Send error: %s",
				   strerror (errno));
			g_assert_not_reached ();
		}
	}

	pthread_mutex_unlock (&req_mutex);
}

/* Read request on fd (called by the daemon) */
void _wapi_daemon_request (int fd, WapiHandleRequest *req)
{
	int ret;
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);
#endif
	
#ifdef HAVE_MSG_NOSIGNAL
	ret=recv (fd, req, sizeof(WapiHandleRequest), MSG_NOSIGNAL);
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	ret=recv (fd, req, sizeof(WapiHandleRequest), 0);
	signal (SIGPIPE, old_sigpipe);
#endif
	if(ret==-1) {
#ifdef DEBUG
		g_warning (G_GNUC_PRETTY_FUNCTION ": Recv error: %s",
			   strerror (errno));
#endif
		/* The next loop around poll() should tidy up */
	}
}

/* Send response on fd (called by the daemon) */
void _wapi_daemon_response (int fd, WapiHandleResponse *resp)
{
	int ret;
#ifndef HAVE_MSG_NOSIGNAL
	void (*old_sigpipe)(int);
#endif
	
#ifdef HAVE_MSG_NOSIGNAL
	ret=send (fd, resp, sizeof(WapiHandleResponse), MSG_NOSIGNAL);
#else
	old_sigpipe = signal (SIGPIPE, SIG_IGN);
	ret=send (fd, resp, sizeof(WapiHandleResponse), 0);
	signal (SIGPIPE, old_sigpipe);
#endif
	if(ret==-1) {
#ifdef DEBUG
		g_warning (G_GNUC_PRETTY_FUNCTION ": Send error: %s",
			   strerror (errno));
#endif
		/* The next loop around poll() should tidy up */
	}
}
