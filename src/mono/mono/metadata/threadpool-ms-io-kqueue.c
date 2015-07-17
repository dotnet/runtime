
#if defined(HAVE_KQUEUE)

#include <sys/types.h>
#include <sys/event.h>
#include <sys/time.h>

#if defined(HOST_WIN32)
/* We assume that kqueue is not available on windows */
#error
#endif

#define KQUEUE_NEVENTS 128

static gint kqueue_fd;
static struct kevent *kqueue_events;

static gboolean
kqueue_init (gint wakeup_pipe_fd)
{
	struct kevent event;

	kqueue_fd = kqueue ();
	if (kqueue_fd == -1) {
		g_warning ("kqueue_init: kqueue () failed, error (%d) %s", errno, g_strerror (errno));
		return FALSE;
	}

	EV_SET (&event, wakeup_pipe_fd, EVFILT_READ, EV_ADD | EV_ENABLE, 0, 0, 0);
	if (kevent (kqueue_fd, &event, 1, NULL, 0, NULL) == -1) {
		g_warning ("kqueue_init: kevent () failed, error (%d) %s", errno, g_strerror (errno));
		close (kqueue_fd);
		return FALSE;
	}

	kqueue_events = g_new0 (struct kevent, KQUEUE_NEVENTS);

	return TRUE;
}

static void
kqueue_cleanup (void)
{
	g_free (kqueue_events);
	close (kqueue_fd);
}

static void
kqueue_register_fd (gint fd, gint events, gboolean is_new)
{
	struct kevent event;

	if (events == 0)
		return;

	if ((events & MONO_POLLIN) != 0)
		EV_SET (&event, fd, EVFILT_READ, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
	if ((events & MONO_POLLOUT) != 0)
		EV_SET (&event, fd, EVFILT_WRITE, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);

	if (kevent (kqueue_fd, &event, 1, NULL, 0, NULL) == -1)
		g_warning ("kqueue_register_fd: kevent(update) failed, error (%d) %s", errno, g_strerror (errno));
}

static gint
kqueue_event_wait (void)
{
	gint ready;

	ready = kevent (kqueue_fd, NULL, 0, kqueue_events, KQUEUE_NEVENTS, NULL);
	if (ready == -1) {
		switch (errno) {
		case EINTR:
			mono_thread_internal_check_for_interruption_critical (mono_thread_internal_current ());
			ready = 0;
			break;
		default:
			g_warning ("kqueue_event_wait: kevent () failed, error (%d) %s", errno, g_strerror (errno));
			break;
		}
	}

	return ready;
}

static gint
kqueue_event_get_fd_at (gint i, gint *events)
{
	g_assert (events);

	*events = ((kqueue_events [i].filter == EVFILT_READ || (kqueue_events [i].flags & EV_ERROR) != 0) ? MONO_POLLIN : 0)
	            | ((kqueue_events [i].filter == EVFILT_WRITE || (kqueue_events [i].flags & EV_ERROR) != 0) ? MONO_POLLOUT : 0);

	return kqueue_events [i].ident;
}

static gint
kqueue_event_get_fd_max (void)
{
	return KQUEUE_NEVENTS;
}

static ThreadPoolIOBackend backend_kqueue = {
	.init = kqueue_init,
	.cleanup = kqueue_cleanup,
	.register_fd = kqueue_register_fd,
	.event_wait = kqueue_event_wait,
	.event_get_fd_max = kqueue_event_get_fd_max,
	.event_get_fd_at = kqueue_event_get_fd_at,
};

#endif
