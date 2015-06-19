
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
kqueue_update_add (gint fd, gint events, gboolean is_new)
{
	struct kevent event;

	if ((events & MONO_POLLIN) != 0)
		EV_SET (&event, fd, EVFILT_READ, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
	if ((events & MONO_POLLOUT) != 0)
		EV_SET (&event, fd, EVFILT_WRITE, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);

	if (kevent (kqueue_fd, &event, 1, NULL, 0, NULL) == -1)
		g_warning ("kqueue_update_add: kevent(update) failed, error (%d) %s", errno, g_strerror (errno));
}

static gint
kqueue_event_wait (void)
{
	gint ready;

	ready = kevent (kqueue_fd, NULL, 0, kqueue_events, KQUEUE_NEVENTS, NULL);
	if (ready == -1) {
		switch (errno) {
		case EINTR:
			check_for_interruption_critical ();
			ready = 0;
			break;
		default:
			g_warning ("kqueue_event_wait: kevent () failed, error (%d) %s", errno, g_strerror (errno));
			break;
		}
	}

	return ready;
}

static inline gint
kqueue_event_fd_at (guint i)
{
	return kqueue_events [i].ident;
}

static gint
kqueue_event_max (void)
{
	return KQUEUE_NEVENTS;
}

static gboolean
kqueue_event_create_sockares_at (guint i, gint fd, MonoMList **list)
{
	struct kevent *kqueue_event;

	g_assert (list);

	kqueue_event = &kqueue_events [i];
	g_assert (kqueue_event);

	g_assert (fd == kqueue_event->ident);

	if (*list && (kqueue_event->filter == EVFILT_READ || (kqueue_event->flags & EV_ERROR) != 0)) {
		MonoSocketAsyncResult *io_event = get_sockares_for_event (list, MONO_POLLIN);
		if (io_event)
			mono_threadpool_ms_enqueue_work_item (((MonoObject*) io_event)->vtable->domain, (MonoObject*) io_event);
	}
	if (*list && (kqueue_event->filter == EVFILT_WRITE || (kqueue_event->flags & EV_ERROR) != 0)) {
		MonoSocketAsyncResult *io_event = get_sockares_for_event (list, MONO_POLLOUT);
		if (io_event)
			mono_threadpool_ms_enqueue_work_item (((MonoObject*) io_event)->vtable->domain, (MonoObject*) io_event);
	}

	if (*list) {
		gint events = get_events (*list);
		if (kqueue_event->filter == EVFILT_READ && (events & MONO_POLLIN) != 0) {
			EV_SET (kqueue_event, fd, EVFILT_READ, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
			if (kevent (kqueue_fd, kqueue_event, 1, NULL, 0, NULL) == -1)
				g_warning ("kqueue_event_create_sockares_at: kevent (read) failed, error (%d) %s", errno, g_strerror (errno));
		}
		if (kqueue_event->filter == EVFILT_WRITE && (events & MONO_POLLOUT) != 0) {
			EV_SET (kqueue_event, fd, EVFILT_WRITE, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
			if (kevent (kqueue_fd, kqueue_event, 1, NULL, 0, NULL) == -1)
				g_warning ("kqueue_event_create_sockares_at: kevent (write) failed, error (%d) %s", errno, g_strerror (errno));
		}
	}

	return TRUE;
}

static ThreadPoolIOBackend backend_kqueue = {
	.init = kqueue_init,
	.cleanup = kqueue_cleanup,
	.update_add = kqueue_update_add,
	.event_wait = kqueue_event_wait,
	.event_max = kqueue_event_max,
	.event_fd_at = kqueue_event_fd_at,
	.event_create_sockares_at = kqueue_event_create_sockares_at,
};

#endif
