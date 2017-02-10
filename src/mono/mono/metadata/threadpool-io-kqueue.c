/**
 * \file
 */

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

static gint
KQUEUE_INIT_FD (gint fd, gint events, gint flags)
{
	struct kevent event;
	EV_SET (&event, fd, events, flags, 0, 0, 0);
	return kevent (kqueue_fd, &event, 1, NULL, 0, NULL);
}

static gboolean
kqueue_init (gint wakeup_pipe_fd)
{
	kqueue_fd = kqueue ();
	if (kqueue_fd == -1) {
		g_error ("kqueue_init: kqueue () failed, error (%d) %s", errno, g_strerror (errno));
		return FALSE;
	}

	if (KQUEUE_INIT_FD (wakeup_pipe_fd, EVFILT_READ, EV_ADD | EV_ENABLE) == -1) {
		g_error ("kqueue_init: kevent () failed, error (%d) %s", errno, g_strerror (errno));
		close (kqueue_fd);
		return FALSE;
	}

	kqueue_events = g_new0 (struct kevent, KQUEUE_NEVENTS);

	return TRUE;
}

static void
kqueue_register_fd (gint fd, gint events, gboolean is_new)
{
	if (events & EVENT_IN) {
		if (KQUEUE_INIT_FD (fd, EVFILT_READ, EV_ADD | EV_ENABLE) == -1)
			g_error ("kqueue_register_fd: kevent(read,enable) failed, error (%d) %s", errno, g_strerror (errno));
	} else {
		if (KQUEUE_INIT_FD (fd, EVFILT_READ, EV_ADD | EV_DISABLE) == -1)
			g_error ("kqueue_register_fd: kevent(read,disable) failed, error (%d) %s", errno, g_strerror (errno));
	}
	if (events & EVENT_OUT) {
		if (KQUEUE_INIT_FD (fd, EVFILT_WRITE, EV_ADD | EV_ENABLE) == -1)
			g_error ("kqueue_register_fd: kevent(write,enable) failed, error (%d) %s", errno, g_strerror (errno));
	} else {
		if (KQUEUE_INIT_FD (fd, EVFILT_WRITE, EV_ADD | EV_DISABLE) == -1)
			g_error ("kqueue_register_fd: kevent(write,disable) failed, error (%d) %s", errno, g_strerror (errno));
	}
}

static void
kqueue_remove_fd (gint fd)
{
	/* FIXME: a race between closing and adding operation in the Socket managed code trigger a ENOENT error */
	if (KQUEUE_INIT_FD (fd, EVFILT_READ, EV_DELETE) == -1)
		g_error ("kqueue_register_fd: kevent(read,delete) failed, error (%d) %s", errno, g_strerror (errno));
	if (KQUEUE_INIT_FD (fd, EVFILT_WRITE, EV_DELETE) == -1)
		g_error ("kqueue_register_fd: kevent(write,delete) failed, error (%d) %s", errno, g_strerror (errno));
}

static gint
kqueue_event_wait (void (*callback) (gint fd, gint events, gpointer user_data), gpointer user_data)
{
	gint i, ready;

	memset (kqueue_events, 0, sizeof (struct kevent) * KQUEUE_NEVENTS);

	mono_gc_set_skip_thread (TRUE);

	MONO_ENTER_GC_SAFE;
	ready = kevent (kqueue_fd, NULL, 0, kqueue_events, KQUEUE_NEVENTS, NULL);
	MONO_EXIT_GC_SAFE;

	mono_gc_set_skip_thread (FALSE);

	if (ready == -1) {
		switch (errno) {
		case EINTR:
			ready = 0;
			break;
		default:
			g_error ("kqueue_event_wait: kevent () failed, error (%d) %s", errno, g_strerror (errno));
			break;
		}
	}

	if (ready == -1)
		return -1;

	for (i = 0; i < ready; ++i) {
		gint fd, events = 0;

		fd = kqueue_events [i].ident;
		if (kqueue_events [i].filter == EVFILT_READ || (kqueue_events [i].flags & EV_ERROR) != 0)
			events |= EVENT_IN;
		if (kqueue_events [i].filter == EVFILT_WRITE || (kqueue_events [i].flags & EV_ERROR) != 0)
			events |= EVENT_OUT;

		callback (fd, events, user_data);
	}

	return 0;
}

static ThreadPoolIOBackend backend_kqueue = {
	.init = kqueue_init,
	.register_fd = kqueue_register_fd,
	.remove_fd = kqueue_remove_fd,
	.event_wait = kqueue_event_wait,
};

#endif
