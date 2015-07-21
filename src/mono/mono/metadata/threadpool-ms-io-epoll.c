
#if defined(HAVE_EPOLL)

#include <sys/epoll.h>

#if defined(HOST_WIN32)
/* We assume that epoll is not available on windows */
#error
#endif

#define EPOLL_NEVENTS 128

static gint epoll_fd;
static struct epoll_event *epoll_events;

static gboolean
epoll_init (gint wakeup_pipe_fd)
{
	struct epoll_event event;

#ifdef EPOOL_CLOEXEC
	epoll_fd = epoll_create1 (EPOLL_CLOEXEC);
#else
	epoll_fd = epoll_create (256);
	fcntl (epoll_fd, F_SETFD, FD_CLOEXEC);
#endif

	if (epoll_fd == -1) {
#ifdef EPOOL_CLOEXEC
		g_error ("epoll_init: epoll (EPOLL_CLOEXEC) failed, error (%d) %s\n", errno, g_strerror (errno));
#else
		g_error ("epoll_init: epoll (256) failed, error (%d) %s\n", errno, g_strerror (errno));
#endif
		return FALSE;
	}

	event.events = EPOLLIN;
	event.data.fd = wakeup_pipe_fd;
	if (epoll_ctl (epoll_fd, EPOLL_CTL_ADD, event.data.fd, &event) == -1) {
		g_error ("epoll_init: epoll_ctl () failed, error (%d) %s", errno, g_strerror (errno));
		close (epoll_fd);
		return FALSE;
	}

	epoll_events = g_new0 (struct epoll_event, EPOLL_NEVENTS);

	return TRUE;
}

static void
epoll_cleanup (void)
{
	g_free (epoll_events);
	close (epoll_fd);
}

static void
epoll_register_fd (gint fd, gint events, gboolean is_new)
{
	if (events == 0) {
		if (!is_new && epoll_ctl (epoll_fd, EPOLL_CTL_DEL, fd, NULL) == -1)
			g_error ("epoll_register_fd: epoll_ctl (EPOLL_CTL_DEL) failed, error (%d) %s", errno, g_strerror (errno));
	} else {
		struct epoll_event event;

#ifndef EPOLLONESHOT
/* it was only defined on android in May 2013 */
#define EPOLLONESHOT 0x40000000
#endif

		event.data.fd = fd;
		event.events = EPOLLONESHOT;
		if ((events & MONO_POLLIN) != 0)
			event.events |= EPOLLIN;
		if ((events & MONO_POLLOUT) != 0)
			event.events |= EPOLLOUT;

		if (epoll_ctl (epoll_fd, is_new ? EPOLL_CTL_ADD : EPOLL_CTL_MOD, event.data.fd, &event) == -1)
			g_error ("epoll_register_fd: epoll_ctl(%s) failed, error (%d) %s", is_new ? "EPOLL_CTL_ADD" : "EPOLL_CTL_MOD", errno, g_strerror (errno));
	}
}

static gint
epoll_event_wait (void)
{
	gint ready;

	memset (epoll_events, 0, sizeof (struct epoll_event) * EPOLL_NEVENTS);

	ready = epoll_wait (epoll_fd, epoll_events, EPOLL_NEVENTS, -1);
	if (ready == -1) {
		switch (errno) {
		case EINTR:
			mono_thread_internal_check_for_interruption_critical (mono_thread_internal_current ());
			ready = 0;
			break;
		default:
			g_error ("epoll_event_wait: epoll_wait () failed, error (%d) %s", errno, g_strerror (errno));
			break;
		}
	}

	return ready;
}

static gint
epoll_event_get_fd_max (void)
{
	return EPOLL_NEVENTS;
}

static gint
epoll_event_get_fd_at (gint i, gint *events)
{
	g_assert (events);

	*events = ((epoll_events [i].events & (EPOLLIN | EPOLLERR | EPOLLHUP)) ? MONO_POLLIN : 0)
	            | ((epoll_events [i].events & (EPOLLOUT | EPOLLERR | EPOLLHUP)) ? MONO_POLLOUT : 0);

	return epoll_events [i].data.fd;
}

static ThreadPoolIOBackend backend_epoll = {
	.init = epoll_init,
	.cleanup = epoll_cleanup,
	.register_fd = epoll_register_fd,
	.event_wait = epoll_event_wait,
	.event_get_fd_max = epoll_event_get_fd_max,
	.event_get_fd_at = epoll_event_get_fd_at,
};

#endif
