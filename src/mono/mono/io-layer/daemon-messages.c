#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>
#include <string.h>

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
	
	/* Serialise requests to the daemon from the same process.  We
	 * rely on request turnaround time being minimal anyway, so
	 * performance shouldnt suffer from the mutex.
	 */
	pthread_mutex_lock (&req_mutex);
	
	ret=send (fd, req, sizeof(WapiHandleRequest), MSG_NOSIGNAL);
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

	ret=recv (fd, resp, sizeof(WapiHandleResponse), MSG_NOSIGNAL);
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
	
	ret=recv (fd, req, sizeof(WapiHandleRequest), MSG_NOSIGNAL);
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
	
	ret=send (fd, resp, sizeof(WapiHandleResponse), MSG_NOSIGNAL);
	if(ret==-1) {
#ifdef DEBUG
		g_warning (G_GNUC_PRETTY_FUNCTION ": Send error: %s",
			   strerror (errno));
#endif
		/* The next loop around poll() should tidy up */
	}
}
