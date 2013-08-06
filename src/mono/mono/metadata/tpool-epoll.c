/*
 * tpool-epoll.c: epoll related stuff
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 */

struct _tp_epoll_data {
	int epollfd;
};

typedef struct _tp_epoll_data tp_epoll_data;
static void tp_epoll_modify (gpointer p, int fd, int operation, int events, gboolean is_new);
static void tp_epoll_shutdown (gpointer event_data);
static void tp_epoll_wait (gpointer event_data);

static gpointer
tp_epoll_init (SocketIOData *data)
{
	tp_epoll_data *result;

	result = g_new0 (tp_epoll_data, 1);
#ifdef EPOLL_CLOEXEC
	result->epollfd = epoll_create1 (EPOLL_CLOEXEC);
#else
	result->epollfd = epoll_create (256); /* The number does not really matter */
	fcntl (result->epollfd, F_SETFD, FD_CLOEXEC);
#endif
	if (result->epollfd == -1) {
		int err = errno;
		if (g_getenv ("MONO_DEBUG")) {
#ifdef EPOLL_CLOEXEC
			g_message ("epoll_create1(EPOLL_CLOEXEC) failed: %d %s", err, g_strerror (err));
#else
			g_message ("epoll_create(256) failed: %d %s", err, g_strerror (err));
#endif
		}

		return NULL;
	}

	data->shutdown = tp_epoll_shutdown;
	data->modify = tp_epoll_modify;
	data->wait = tp_epoll_wait;
	return result;
}

static void
tp_epoll_modify (gpointer p, int fd, int operation, int events, gboolean is_new)
{
	SocketIOData *socket_io_data;
	tp_epoll_data *data;
	struct epoll_event evt;
	int epoll_op;

	socket_io_data = p;
	data = socket_io_data->event_data;

	memset (&evt, 0, sizeof (evt));
	evt.data.fd = fd;
	if ((events & MONO_POLLIN) != 0)
		evt.events |= EPOLLIN;
	if ((events & MONO_POLLOUT) != 0)
		evt.events |= EPOLLOUT;

	epoll_op = (is_new) ? EPOLL_CTL_ADD : EPOLL_CTL_MOD;
	if (epoll_ctl (data->epollfd, epoll_op, fd, &evt) == -1) {
		int err = errno;
		if (epoll_op == EPOLL_CTL_ADD && err == EEXIST) {
			epoll_op = EPOLL_CTL_MOD;
			if (epoll_ctl (data->epollfd, epoll_op, fd, &evt) == -1) {
				g_message ("epoll_ctl(MOD): %d %s", err, g_strerror (err));
			}
		}
	}
	LeaveCriticalSection (&socket_io_data->io_lock);
}

static void
tp_epoll_shutdown (gpointer event_data)
{
	tp_epoll_data *data = event_data;

	close (data->epollfd);
	g_free (data);
}

#define EPOLL_ERRORS (EPOLLERR | EPOLLHUP)
#define EPOLL_NEVENTS	128
static void
tp_epoll_wait (gpointer p)
{
	SocketIOData *socket_io_data;
	int epollfd;
	struct epoll_event *events, *evt;
	int ready = 0, i;
	gpointer async_results [EPOLL_NEVENTS * 2]; // * 2 because each loop can add up to 2 results here
	gint nresults;
	tp_epoll_data *data;

	socket_io_data = p;
	data = socket_io_data->event_data;
	epollfd = data->epollfd;
	events = g_new0 (struct epoll_event, EPOLL_NEVENTS);

	while (1) {
		mono_gc_set_skip_thread (TRUE);

		do {
			if (ready == -1) {
				check_for_interruption_critical ();
			}
			ready = epoll_wait (epollfd, events, EPOLL_NEVENTS, -1);
		} while (ready == -1 && errno == EINTR);

		mono_gc_set_skip_thread (FALSE);

		if (ready == -1) {
			int err = errno;
			g_free (events);
			if (err != EBADF)
				g_warning ("epoll_wait: %d %s", err, g_strerror (err));

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
			fd = evt->data.fd;
			list = mono_g_hash_table_lookup (socket_io_data->sock_to_state, GINT_TO_POINTER (fd));
			if (list != NULL && (evt->events & (EPOLLIN | EPOLL_ERRORS)) != 0) {
				ares = get_io_event (&list, MONO_POLLIN);
				if (ares != NULL)
					async_results [nresults++] = ares;
			}

			if (list != NULL && (evt->events & (EPOLLOUT | EPOLL_ERRORS)) != 0) {
				ares = get_io_event (&list, MONO_POLLOUT);
				if (ares != NULL)
					async_results [nresults++] = ares;
			}

			if (list != NULL) {
				int p;

				mono_g_hash_table_replace (socket_io_data->sock_to_state, GINT_TO_POINTER (fd), list);
				p = get_events_from_list (list);
				evt->events = (p & MONO_POLLOUT) ? EPOLLOUT : 0;
				evt->events |= (p & MONO_POLLIN) ? EPOLLIN : 0;
				if (epoll_ctl (epollfd, EPOLL_CTL_MOD, fd, evt) == -1) {
					if (epoll_ctl (epollfd, EPOLL_CTL_ADD, fd, evt) == -1) {
						int err = errno;
						g_message ("epoll(ADD): %d %s", err, g_strerror (err));
					}
				}
			} else {
				mono_g_hash_table_remove (socket_io_data->sock_to_state, GINT_TO_POINTER (fd));
				epoll_ctl (epollfd, EPOLL_CTL_DEL, fd, evt);
			}
		}
		LeaveCriticalSection (&socket_io_data->io_lock);
		threadpool_append_jobs (&async_io_tp, (MonoObject **) async_results, nresults);
		mono_gc_bzero (async_results, sizeof (gpointer) * nresults);
	}
}
#undef EPOLL_NEVENTS
#undef EPOLL_ERRORS
