
#if defined(HAVE_POLL)

#if defined(HAVE_POLL_H)
#include <poll.h>
#elif defined(HAVE_SYS_POLL_H)
#include <sys/poll.h>
#endif

typedef struct pollfd mono_pollfd;

#elif defined(HOST_WIN32)

#include "mswsock.h"

typedef WSAPOLLFD mono_pollfd;

#else
/* poll is not defined */
#error
#endif

static mono_pollfd *poll_fds;
static guint poll_fds_capacity;
static guint poll_fds_size;

static inline void
POLL_INIT_FD (mono_pollfd *poll_fd, gint fd, gint events)
{
	poll_fd->fd = fd;
	poll_fd->events = events;
	poll_fd->revents = 0;
}

static gboolean
poll_init (gint wakeup_pipe_fd)
{
	g_assert (wakeup_pipe_fd >= 0);

	poll_fds_size = 1;
	poll_fds_capacity = 64;

	poll_fds = g_new0 (mono_pollfd, poll_fds_capacity);

	POLL_INIT_FD (&poll_fds [0], wakeup_pipe_fd, POLLIN);

	return TRUE;
}

static void
poll_cleanup (void)
{
	g_free (poll_fds);
}

static void
poll_register_fd (gint fd, gint events, gboolean is_new)
{
	gint i;
	gint poll_event;

	g_assert (fd >= 0);
	g_assert (poll_fds_size <= poll_fds_capacity);

	g_assert ((events & ~(EVENT_IN | EVENT_OUT)) == 0);

	poll_event = 0;
	if (events & EVENT_IN)
		poll_event |= POLLIN;
	if (events & EVENT_OUT)
		poll_event |= POLLOUT;

	for (i = 0; i < poll_fds_size; ++i) {
		if (poll_fds [i].fd == fd) {
			g_assert (!is_new);
			POLL_INIT_FD (&poll_fds [i], fd, poll_event);
			return;
		}
	}

	g_assert (is_new);

	for (i = 0; i < poll_fds_size; ++i) {
		if (poll_fds [i].fd == -1) {
			POLL_INIT_FD (&poll_fds [i], fd, poll_event);
			return;
		}
	}

	poll_fds_size += 1;

	if (poll_fds_size > poll_fds_capacity) {
		poll_fds_capacity *= 2;
		g_assert (poll_fds_size <= poll_fds_capacity);

		poll_fds = g_renew (mono_pollfd, poll_fds, poll_fds_capacity);
	}

	POLL_INIT_FD (&poll_fds [poll_fds_size - 1], fd, poll_event);
}

static void
poll_remove_fd (gint fd)
{
	gint i;

	g_assert (fd >= 0);

	for (i = 0; i < poll_fds_size; ++i) {
		if (poll_fds [i].fd == fd) {
			POLL_INIT_FD (&poll_fds [i], -1, 0);
			return;
		}
	}

	g_assert_not_reached ();
}

static gint
poll_event_wait (void (*callback) (gint fd, gint events, gpointer user_data), gpointer user_data)
{
	gint i, ready;

	for (i = 0; i < poll_fds_size; ++i)
		poll_fds [i].revents = 0;

	mono_gc_set_skip_thread (TRUE);

#if !defined(HOST_WIN32)
	ready = poll (poll_fds, poll_fds_size, -1);
#else
	ready = WSAPoll(poll_fds, poll_fds_size, -1);
	if (ready == SOCKET_ERROR)
		ready = -1;
#endif

	mono_gc_set_skip_thread (FALSE);

	if (ready == -1) {
		/*
		 * Apart from EINTR, we only check EBADF, for the rest:
		 *  EINVAL: mono_poll() 'protects' us from descriptor
		 *      numbers above the limit if using select() by marking
		 *      then as POLLERR.  If a system poll() is being
		 *      used, the number of descriptor we're passing will not
		 *      be over sysconf(_SC_OPEN_MAX), as the error would have
		 *      happened when opening.
		 *
		 *  EFAULT: we own the memory pointed by pfds.
		 *  ENOMEM: we're doomed anyway
		 *
		 */
#if !defined(HOST_WIN32)
		switch (errno)
#else
		switch (WSAGetLastError ())
#endif
		{
#if !defined(HOST_WIN32)
		case EINTR:
#else
		case WSAEINTR:
#endif
		{
			mono_thread_internal_check_for_interruption_critical (mono_thread_internal_current ());
			ready = 0;
			break;
		}
		default:
#if !defined(HOST_WIN32)
			g_error ("poll_event_wait: mono_poll () failed, error (%d) %s", errno, g_strerror (errno));
#else
			g_error ("poll_event_wait: mono_poll () failed, error (%d)\n", WSAGetLastError ());
#endif
			break;
		}
	}

	if (ready == -1)
		return -1;

	for (i = 0; i < poll_fds_size; ++i) {
		gint fd, events = 0;

		if (poll_fds [i].fd == -1)
			continue;
		if (poll_fds [i].revents == 0)
			continue;

		fd = poll_fds [i].fd;
		if (poll_fds [i].revents & (POLLIN | POLLERR | POLLHUP | POLLNVAL))
			events |= EVENT_IN;
		if (poll_fds [i].revents & (POLLOUT | POLLERR | POLLHUP | POLLNVAL))
			events |= EVENT_OUT;

		callback (fd, events, user_data);

		if (--ready == 0)
			break;
	}

	return 0;
}

static ThreadPoolIOBackend backend_poll = {
	.init = poll_init,
	.cleanup = poll_cleanup,
	.register_fd = poll_register_fd,
	.remove_fd = poll_remove_fd,
	.event_wait = poll_event_wait,
};
