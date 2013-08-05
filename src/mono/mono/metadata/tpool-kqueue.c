/*
 * tpool-kqueue.c: kqueue related stuff
 *
 * Authors:
 *   Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 */

struct _tp_kqueue_data {
	int fd;
};

typedef struct _tp_kqueue_data tp_kqueue_data;
static void tp_kqueue_modify (gpointer p, int fd, int operation, int events, gboolean is_new);
static void tp_kqueue_shutdown (gpointer event_data);
static void tp_kqueue_wait (gpointer event_data);

static gpointer
tp_kqueue_init (SocketIOData *data)
{
	tp_kqueue_data *result;

	result = g_new0 (tp_kqueue_data, 1);
	result->fd = kqueue ();
	if (result->fd == -1)
		return NULL;

	data->shutdown = tp_kqueue_shutdown;
	data->modify = tp_kqueue_modify;
	data->wait = tp_kqueue_wait;
	return result;
}

static void
kevent_change (int kfd, struct kevent *evt, const char *error_str)
{
	if (kevent (kfd, evt, 1, NULL, 0, NULL) == -1) {
		int err = errno;
		g_message ("kqueue(%s): %d %s", error_str, err, g_strerror (err));
	}
}

static void
tp_kqueue_modify (gpointer p, int fd, int operation, int events, gboolean is_new)
{
	SocketIOData *socket_io_data;
	socket_io_data = p;
	tp_kqueue_data *data = socket_io_data->event_data;
	struct kevent evt;

	memset (&evt, 0, sizeof (evt));
	if ((events & MONO_POLLIN) != 0) {
		EV_SET (&evt, fd, EVFILT_READ, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
		kevent_change (data->fd, &evt, "ADD read");
	}

	if ((events & MONO_POLLOUT) != 0) {
		EV_SET (&evt, fd, EVFILT_WRITE, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
		kevent_change (data->fd, &evt, "ADD write");
	}
	LeaveCriticalSection (&socket_io_data->io_lock);
}

static void
tp_kqueue_shutdown (gpointer event_data)
{
	tp_kqueue_data *data = event_data;

	close (data->fd);
	g_free (data);
}

#define KQUEUE_NEVENTS	128
static void
tp_kqueue_wait (gpointer p)
{
	SocketIOData *socket_io_data;
	int kfd;
	struct kevent *events, *evt;
	int ready = 0, i;
	gpointer async_results [KQUEUE_NEVENTS * 2]; // * 2 because each loop can add up to 2 results here
	gint nresults;
	tp_kqueue_data *data;

	socket_io_data = p;
	data = socket_io_data->event_data;
	kfd = data->fd;
	events = g_new0 (struct kevent, KQUEUE_NEVENTS);

	while (1) {
	
		mono_gc_set_skip_thread (TRUE);

		do {
			if (ready == -1) {
				check_for_interruption_critical ();
			}
			ready = kevent (kfd, NULL, 0, events, KQUEUE_NEVENTS, NULL);
		} while (ready == -1 && errno == EINTR);

		mono_gc_set_skip_thread (FALSE);

		if (ready == -1) {
			int err = errno;
			g_free (events);
			if (err != EBADF)
				g_warning ("kevent wait: %d %s", err, g_strerror (err));

			return;
		}

		EnterCriticalSection (&socket_io_data->io_lock);
		if (socket_io_data->inited == 3) {
			g_free (events);
			LeaveCriticalSection (&socket_io_data->io_lock);
			return; /* cleanup called */
		}

		nresults = 0;
		for (i = 0; i < ready; i++) {
			int fd;
			MonoMList *list;
			MonoObject *ares;

			evt = &events [i];
			fd = evt->ident;
			list = mono_g_hash_table_lookup (socket_io_data->sock_to_state, GINT_TO_POINTER (fd));
			if (list != NULL && (evt->filter == EVFILT_READ || (evt->flags & EV_ERROR) != 0)) {
				ares = get_io_event (&list, MONO_POLLIN);
				if (ares != NULL)
					async_results [nresults++] = ares;
			}
			if (list != NULL && (evt->filter == EVFILT_WRITE || (evt->flags & EV_ERROR) != 0)) {
				ares = get_io_event (&list, MONO_POLLOUT);
				if (ares != NULL)
					async_results [nresults++] = ares;
			}

			if (list != NULL) {
				int p;

				mono_g_hash_table_replace (socket_io_data->sock_to_state, GINT_TO_POINTER (fd), list);
				p = get_events_from_list (list);
				if (evt->filter == EVFILT_READ && (p & MONO_POLLIN) != 0) {
					EV_SET (evt, fd, EVFILT_READ, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
					kevent_change (kfd, evt, "READD read");
				}

				if (evt->filter == EVFILT_WRITE && (p & MONO_POLLOUT) != 0) {
					EV_SET (evt, fd, EVFILT_WRITE, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
					kevent_change (kfd, evt, "READD write");
				}
			} else {
				mono_g_hash_table_remove (socket_io_data->sock_to_state, GINT_TO_POINTER (fd));
			}
		}
		LeaveCriticalSection (&socket_io_data->io_lock);
		threadpool_append_jobs (&async_io_tp, (MonoObject **) async_results, nresults);
		mono_gc_bzero (async_results, sizeof (gpointer) * nresults);
	}
}
#undef KQUEUE_NEVENTS

