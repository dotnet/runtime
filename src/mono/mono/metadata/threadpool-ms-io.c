/*
 * threadpool-ms-io.c: Microsoft IO threadpool runtime support
 *
 * Author:
 *	Ludovic Henry (ludovic.henry@xamarin.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>

#ifndef DISABLE_SOCKETS

#include <glib.h>

#if defined(HOST_WIN32)
#include <windows.h>
#else
#include <errno.h>
#include <fcntl.h>
#endif

#if defined(HAVE_EPOLL)
#include <sys/epoll.h>
#elif defined(HAVE_KQUEUE)
#include <sys/types.h>
#include <sys/event.h>
#include <sys/time.h>
#endif

#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/metadata/threadpool-internals.h>
#include <mono/metadata/threadpool-ms.h>
#include <mono/metadata/threadpool-ms-io.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-poll.h>
#include <mono/utils/mono-threads.h>

/* Keep in sync with System.Net.Sockets.MonoSocketRuntimeWorkItem */
struct _MonoSocketRuntimeWorkItem {
	MonoObject object;
	MonoSocketAsyncResult *socket_async_result;
};

/* Keep in sync with System.Net.Sockets.Socket.SocketOperation */
enum {
	AIO_OP_FIRST,
	AIO_OP_ACCEPT = 0,
	AIO_OP_CONNECT,
	AIO_OP_RECEIVE,
	AIO_OP_RECEIVEFROM,
	AIO_OP_SEND,
	AIO_OP_SENDTO,
	AIO_OP_RECV_JUST_CALLBACK,
	AIO_OP_SEND_JUST_CALLBACK,
	AIO_OP_READPIPE,
	AIO_OP_CONSOLE2,
	AIO_OP_DISCONNECT,
	AIO_OP_ACCEPTRECEIVE,
	AIO_OP_RECEIVE_BUFFERS,
	AIO_OP_SEND_BUFFERS,
	AIO_OP_LAST
};

typedef enum {
	BACKEND_EPOLL,
	BACKEND_KQUEUE,
	BACKEND_POLL,
} ThreadPoolIOBackend;

typedef struct {
	gint fd;

	union {
#if defined(HAVE_EPOLL)
		struct {
			struct epoll_event *event;
			gint op;
		} epoll;
#elif defined(HAVE_KQUEUE)
		struct {
			struct kevent *event;
		} kqueue;
#endif
		struct {
			mono_pollfd fd;
		} poll;
	};
} ThreadPoolIOUpdate;

typedef struct {
	MonoGHashTable *states;
	mono_mutex_t states_lock;

	ThreadPoolIOBackend backend;

	ThreadPoolIOUpdate *updates;
	guint updates_size;
	mono_mutex_t updates_lock;

#if !defined(HOST_WIN32)
	gint wakeup_pipes [2];
#else
	SOCKET wakeup_pipes [2];
#endif

	union {
#if defined(HAVE_EPOLL)
		struct {
			gint fd;
			struct epoll_event *events;
		} epoll;
#elif defined(HAVE_KQUEUE)
		struct {
			gint fd;
			struct kevent *events;
		} kqueue;
#endif
		struct {
			mono_pollfd *fds;
			guint fds_size;
			guint fds_max;
		} poll;
	};
} ThreadPoolIO;

static gint32 io_status = STATUS_NOT_INITIALIZED;
static gint32 io_thread_status = STATUS_NOT_INITIALIZED;

static ThreadPoolIO* threadpool_io;

static int
get_events_from_state (MonoSocketAsyncResult *ares)
{
	switch (ares->operation) {
	case AIO_OP_ACCEPT:
	case AIO_OP_RECEIVE:
	case AIO_OP_RECV_JUST_CALLBACK:
	case AIO_OP_RECEIVEFROM:
	case AIO_OP_READPIPE:
	case AIO_OP_ACCEPTRECEIVE:
	case AIO_OP_RECEIVE_BUFFERS:
		return MONO_POLLIN;
	case AIO_OP_SEND:
	case AIO_OP_SEND_JUST_CALLBACK:
	case AIO_OP_SENDTO:
	case AIO_OP_CONNECT:
	case AIO_OP_SEND_BUFFERS:
	case AIO_OP_DISCONNECT:
		return MONO_POLLOUT;
	default:
		g_assert_not_reached ();
	}
}

static MonoSocketAsyncResult*
get_state (MonoMList **list, gint event)
{
	MonoSocketAsyncResult *state = NULL;
	MonoMList *current;

	g_assert (list);

	for (current = *list; current; current = mono_mlist_next (current)) {
		state = (MonoSocketAsyncResult*) mono_mlist_get_data (current);
		if (get_events_from_state ((MonoSocketAsyncResult*) state) == event)
			break;
		state = NULL;
	}

	if (current)
		*list = mono_mlist_remove_item (*list, current);

	return state;
}

static gint
get_events (MonoMList *list)
{
	MonoSocketAsyncResult *ares;
	gint events = 0;

	for (; list; list = mono_mlist_next (list))
		if ((ares = (MonoSocketAsyncResult*) mono_mlist_get_data (list)))
			events |= get_events_from_state (ares);

	return events;
}

static void
polling_thread_wakeup (void)
{
	gchar msg = 'c';
	gint written;

	for (;;) {
#if !defined(HOST_WIN32)
		written = write (threadpool_io->wakeup_pipes [1], &msg, 1);
		if (written == 1)
			break;
		if (written == -1) {
			g_warning ("polling_thread_wakeup: write () failed, error (%d) %s\n", errno, g_strerror (errno));
			break;
		}
#else
		written = send (threadpool_io->wakeup_pipes [1], &msg, 1, 0);
		if (written == 1)
			break;
		if (written == SOCKET_ERROR) {
			g_warning ("polling_thread_wakeup: write () failed, error (%d)\n", WSAGetLastError ());
			break;
		}
#endif
	}
}

static void
polling_thread_drain_wakeup_pipes (void)
{
	gchar buffer [128];
	gint received;

	for (;;) {
#if !defined(HOST_WIN32)
		received = read (threadpool_io->wakeup_pipes [0], buffer, sizeof (buffer));
		if (received == 0)
			break;
		if (received == -1) {
			if (errno != EINTR && errno != EAGAIN)
				g_warning ("poll_thread: read () failed, error (%d) %s\n", errno, g_strerror (errno));
			break;
		}
#else
		received = recv (threadpool_io->wakeup_pipes [0], buffer, sizeof (buffer), 0);
		if (received == 0)
			break;
		if (received == SOCKET_ERROR) {
			if (WSAGetLastError () != WSAEINTR && WSAGetLastError () != WSAEWOULDBLOCK)
				g_warning ("poll_thread: recv () failed, error (%d) %s\n", WSAGetLastError ());
			break;
		}
#endif
	}
}

#if defined(HAVE_EPOLL)

#if defined(HOST_WIN32)
/* We assume that epoll is not available on windows */
#error
#endif

#define EPOLL_NEVENTS 128

static gboolean
epoll_init (void)
{
#ifdef EPOOL_CLOEXEC
	threadpool_io->epoll.fd = epoll_create1 (EPOLL_CLOEXEC);
#else
	threadpool_io->epoll.fd = epoll_create1 (256);
	fcntl (threadpool_io->epoll.fd, F_SETFD, FD_CLOEXEC);
#endif

	if (threadpool_io->epoll.fd == -1) {
#ifdef EPOOL_CLOEXEC
		g_warning ("epoll_init: epoll (EPOLL_CLOEXEC) failed, error (%d) %s\n", errno, g_strerror (errno));
#else
		g_warning ("epoll_init: epoll (256) failed, error (%d) %s\n", errno, g_strerror (errno));
#endif
		return FALSE;
	}

	if (epoll_ctl (threadpool_io->epoll.fd, EPOLL_CTL_ADD, threadpool_io->wakeup_pipes [0], EPOLLIN) == -1) {
		g_warning ("epoll_init: epoll_ctl () failed, error (%d) %s", errno, g_strerror (errno));
		close (threadpool_io->epoll.fd);
		return FALSE;
	}

	threadpool_io->epoll.events = g_new0 (struct epoll_event, EPOLL_NEVENTS);

	return TRUE;
}

static void
epoll_cleanup (void)
{
	g_free (threadpool_io->epoll.events);

	close (threadpool_io->epoll.fd);
}

static void
epoll_update (gint fd, gint events, gboolean is_new)
{
	ThreadPoolIOUpdate *update;
	struct epoll_event *event;
	gchar msg = 'c';

	event = g_new0 (struct epoll_event, 1);
	event->data.fd = fd;
	if ((events & MONO_POLLIN) != 0)
		event->events |= EPOLLIN;
	if ((events & MONO_POLLOUT) != 0)
		event->events |= EPOLLOUT;

	mono_mutex_lock (&threadpool_io->updates_lock);
	threadpool_io->updates_size += 1;
	threadpool_io->updates = g_renew (ThreadPoolIOUpdate, threadpool_io->updates, threadpool_io->updates_size);

	update = &threadpool_io->updates [threadpool_io->updates_size - 1];
	update->fd = fd;
	update->epoll.event = event;
	update->epoll.op = is_new ? EPOLL_CTL_ADD : EPOLL_CTL_MOD;
	mono_mutex_unlock (&threadpool_io->updates_lock);

	polling_thread_wakeup ();
}

static void
epoll_thread_add_update (ThreadPoolIOUpdate *update)
{
	if (epoll_ctl (threadpool_io->epoll.fd, update->epoll.op, update->fd, update->epoll.event) == -1)
		g_warning ("epoll_thread_add_update: epoll_ctl(%s) failed, error (%d) %s", update->epoll.op == EPOLL_CTL_ADD ? "EPOLL_CTL_ADD" : "EPOLL_CTL_MOD", errno, g_strerror (errno));
	g_free (update->epoll.event);
}

static gint
epoll_thread_wait_for_event (void)
{
	gint ready;

	ready = epoll_wait (threadpool_io->epoll.fd, threadpool_io->epoll.events, EPOLL_NEVENTS, -1);
	if (ready == -1) {
		switch (errno) {
		case EINTR:
			check_for_interruption_critical ();
			ready = 0;
			break;
		default:
			g_warning ("epoll_thread_wait_for_event: epoll_wait () failed, error (%d) %s", errno, g_strerror (errno));
			break;
		}
	}

	return ready;
}

static inline gint
epoll_thread_get_fd_at (guint i)
{
	return threadpool_io->epoll.events [i].data.fd;
}

static gboolean
epoll_thread_create_socket_async_results (gint fd, struct epoll_event *epoll_event, MonoMList **list)
{
	g_assert (epoll_event);
	g_assert (list);

	if (!*list) {
		epoll_ctl (threadpool_io->epoll.fd, EPOLL_CTL_DEL, fd, epoll_event);
	} else {
		gint events;

		if ((epoll_event->events & (EPOLLIN | EPOLLERR | EPOLLHUP)) != 0) {
			MonoSocketAsyncResult *io_event = get_state (list, MONO_POLLIN);
			if (io_event)
				mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) io_event)->vtable->domain, io_event);
		}
		if ((epoll_event->events & (EPOLLOUT | EPOLLERR | EPOLLHUP)) != 0) {
			MonoSocketAsyncResult *io_event = get_state (list, MONO_POLLOUT);
			if (io_event)
				mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) io_event)->vtable->domain, io_event);
		}

		events = get_events (*list);
		epoll_event->events = ((events & MONO_POLLOUT) ? EPOLLOUT : 0) | ((events & MONO_POLLIN) ? EPOLLIN : 0);
		if (epoll_ctl (threadpool_io->epoll.fd, EPOLL_CTL_MOD, fd, epoll_event) == -1) {
			if (epoll_ctl (threadpool_io->epoll.fd, EPOLL_CTL_ADD, fd, epoll_event) == -1)
				g_warning ("epoll_thread_create_socket_async_results: epoll_ctl () failed, error (%d) %s", errno, g_strerror (errno));
		}
	}

	return TRUE;
}

#elif defined(HAVE_KQUEUE)

#if defined(HOST_WIN32)
/* We assume that kqueue is not available on windows */
#error
#endif

#define KQUEUE_NEVENTS 128

static gboolean
kqueue_init (void)
{
	struct kevent event;

	threadpool_io->kqueue.fd = kqueue ();
	if (threadpool_io->kqueue.fd == -1) {
		g_warning ("kqueue_init: kqueue () failed, error (%d) %s", errno, g_strerror (errno));
		return FALSE;
	}

	EV_SET (&event, threadpool_io->wakeup_pipes [0], EVFILT_READ, EV_ADD | EV_ENABLE, 0, 0, 0);
	if (kevent (threadpool_io->kqueue.fd, &event, 1, NULL, 0, NULL) == -1) {
		g_warning ("kqueue_init: kevent () failed, error (%d) %s", errno, g_strerror (errno));
		close (threadpool_io->kqueue.fd);
		return FALSE;
	}

	threadpool_io->kqueue.events = g_new0 (struct kevent, KQUEUE_NEVENTS);

	return TRUE;
}

static void
kqueue_cleanup (void)
{
	g_free (threadpool_io->kqueue.events);

	close (threadpool_io->kqueue.fd);
}

static void
kqueue_update (gint fd, gint events, gboolean is_new)
{
	ThreadPoolIOUpdate *update;
	struct kevent *event;

	event = g_new0 (struct kevent, 1);
	if ((events & MONO_POLLIN) != 0)
		EV_SET (event, fd, EVFILT_READ, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
	if ((events & MONO_POLLOUT) != 0)
		EV_SET (event, fd, EVFILT_WRITE, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);

	mono_mutex_lock (&threadpool_io->updates_lock);
	threadpool_io->updates_size += 1;
	threadpool_io->updates = g_renew (ThreadPoolIOUpdate, threadpool_io->updates, threadpool_io->updates_size);

	update = &threadpool_io->updates [threadpool_io->updates_size - 1];
	update->fd = fd;
	update->kqueue.event = event;
	mono_mutex_unlock (&threadpool_io->updates_lock);

	polling_thread_wakeup ();
}

static void
kqueue_thread_add_update (ThreadPoolIOUpdate *update)
{
	if (kevent (threadpool_io->kqueue.fd, update->kqueue.event, 1, NULL, 0, NULL) == -1)
		g_warning ("kqueue_thread_add_update: kevent(update) failed, error (%d) %s", errno, g_strerror (errno));
	g_free (update->kqueue.event);
}

static gint
kqueue_thread_wait_for_event (void)
{
	gint ready;

	ready = kevent (threadpool_io->kqueue.fd, NULL, 0, threadpool_io->kqueue.events, KQUEUE_NEVENTS, NULL);
	if (ready == -1) {
		switch (errno) {
		case EINTR:
			check_for_interruption_critical ();
			ready = 0;
			break;
		default:
			g_warning ("kqueue_thread_wait_for_event: kevent () failed, error (%d) %s", errno, g_strerror (errno));
			break;
		}
	}

	return ready;
}

static inline gint
kqueue_thread_get_fd_at (guint i)
{
	return threadpool_io->kqueue.events [i].ident;
}

static gboolean
kqueue_thread_create_socket_async_results (gint fd, struct kevent *kqueue_event, MonoMList **list)
{
	g_assert (kqueue_event);
	g_assert (list);

	if (*list) {
		gint events;

		if (kqueue_event->filter == EVFILT_READ || (kqueue_event->flags & EV_ERROR) != 0) {
			MonoSocketAsyncResult *io_event = get_state (list, MONO_POLLIN);
			if (io_event)
				mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) io_event)->vtable->domain, io_event);
		}
		if (kqueue_event->filter == EVFILT_WRITE || (kqueue_event->flags & EV_ERROR) != 0) {
			MonoSocketAsyncResult *io_event = get_state (list, MONO_POLLOUT);
			if (io_event)
				mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) io_event)->vtable->domain, io_event);
		}

		events = get_events (*list);
		if (kqueue_event->filter == EVFILT_READ && (events & MONO_POLLIN) != 0) {
			EV_SET (kqueue_event, fd, EVFILT_READ, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
			if (kevent (threadpool_io->kqueue.fd, kqueue_event, 1, NULL, 0, NULL) == -1)
				g_warning ("kqueue_thread_create_socket_async_results: kevent (read) failed, error (%d) %s", errno, g_strerror (errno));
		}
		if (kqueue_event->filter == EVFILT_WRITE && (events & MONO_POLLOUT) != 0) {
			EV_SET (kqueue_event, fd, EVFILT_WRITE, EV_ADD | EV_ENABLE | EV_ONESHOT, 0, 0, 0);
			if (kevent (threadpool_io->kqueue.fd, kqueue_event, 1, NULL, 0, NULL) == -1)
				g_warning ("kqueue_thread_create_socket_async_results: kevent (write) failed, error (%d) %s", errno, g_strerror (errno));
		}
	}

	return TRUE;
}

#endif

#define POLL_NEVENTS 1024

static inline void
POLL_INIT_FD (mono_pollfd *poll_fd, gint fd, gint events)
{
	poll_fd->fd = fd;
	poll_fd->events = events;
	poll_fd->revents = 0;
}

static gboolean
poll_init (void)
{
	guint i;

	threadpool_io->poll.fds_max = 1;
	threadpool_io->poll.fds_size = POLL_NEVENTS;
	threadpool_io->poll.fds = g_new0 (mono_pollfd, threadpool_io->poll.fds_size);

	POLL_INIT_FD (threadpool_io->poll.fds, threadpool_io->wakeup_pipes [0], MONO_POLLIN);
	for (i = threadpool_io->poll.fds_max; i < threadpool_io->poll.fds_size; ++i)
		POLL_INIT_FD (threadpool_io->poll.fds + i, -1, 0);

	return TRUE;
}

static void
poll_cleanup (void)
{
	g_free (threadpool_io->poll.fds);
}

static void
poll_update (gint fd, gint events, gboolean is_new)
{
	ThreadPoolIOUpdate *update;

	mono_mutex_lock (&threadpool_io->updates_lock);
	threadpool_io->updates_size += 1;
	threadpool_io->updates = g_renew (ThreadPoolIOUpdate, threadpool_io->updates, threadpool_io->updates_size);

	update = &threadpool_io->updates [threadpool_io->updates_size - 1];
	update->fd = fd;
	POLL_INIT_FD (&update->poll.fd, fd, events);
	mono_mutex_unlock (&threadpool_io->updates_lock);

	polling_thread_wakeup ();
}

static gint
poll_mark_bad_fds (mono_pollfd *poll_fds, gint poll_fds_size)
{
	gint i;
	gint ret;
	gint ready = 0;
	mono_pollfd *poll_fd;

	for (i = 0; i < poll_fds_size; i++) {
		poll_fd = poll_fds + i;
		if (poll_fd->fd == -1)
			continue;

		ret = mono_poll (poll_fd, 1, 0);
		if (ret == 1)
			ready++;
		if (ret == -1) {
#if !defined(HOST_WIN32)
			if (errno == EBADF)
#else
			if (WSAGetLastError () == WSAEBADF)
#endif
			{
				poll_fd->revents |= MONO_POLLNVAL;
				ready++;
			}
		}
	}

	return ready;
}

static void
poll_thread_add_update (ThreadPoolIOUpdate *update)
{
	gboolean found = FALSE;
	gint j, k;

	for (j = 1; j < threadpool_io->poll.fds_size; ++j) {
		mono_pollfd *poll_fd = threadpool_io->poll.fds + j;
		if (poll_fd->fd == update->poll.fd.fd) {
			found = TRUE;
			break;
		}
	}

	if (!found) {
		for (j = 1; j < threadpool_io->poll.fds_size; ++j) {
			mono_pollfd *poll_fd = threadpool_io->poll.fds + j;
			if (poll_fd->fd == -1)
				break;
		}
	}

	if (j == threadpool_io->poll.fds_size) {
		threadpool_io->poll.fds_size += POLL_NEVENTS;
		threadpool_io->poll.fds = g_renew (mono_pollfd, threadpool_io->poll.fds, threadpool_io->poll.fds_size);
		for (k = j; k < threadpool_io->poll.fds_size; ++k)
			POLL_INIT_FD (threadpool_io->poll.fds + k, -1, 0);
	}

	POLL_INIT_FD (threadpool_io->poll.fds + j, update->poll.fd.fd, update->poll.fd.events);

	if (j >= threadpool_io->poll.fds_max)
		threadpool_io->poll.fds_max = j + 1;
}

static gint
poll_thread_wait_for_event (void)
{
	gint ready;

	ready = mono_poll (threadpool_io->poll.fds, threadpool_io->poll.fds_max, -1);
	if (ready == -1) {
		/* 
		 * Apart from EINTR, we only check EBADF, for the rest:
		 *  EINVAL: mono_poll() 'protects' us from descriptor
		 *      numbers above the limit if using select() by marking
		 *      then as MONO_POLLERR.  If a system poll() is being
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
			check_for_interruption_critical ();
			ready = 0;
			break;
#if !defined(HOST_WIN32)
		case EBADF:
#else
		case WSAEBADF:
#endif
			ready = poll_mark_bad_fds (threadpool_io->poll.fds, threadpool_io->poll.fds_max);
			break;
		default:
#if !defined(HOST_WIN32)
			g_warning ("poll_thread_wait_for_event: mono_poll () failed, error (%d) %s", errno, g_strerror (errno));
#else
			g_warning ("poll_thread_wait_for_event: mono_poll () failed, error (%d)\n", WSAGetLastError ());
#endif
			break;
		}
	}

	return ready;
}

static inline gint
poll_thread_get_fd_at (guint i)
{
	return threadpool_io->poll.fds [i].fd;
}

static gboolean
poll_thread_create_socket_async_results (gint fd, mono_pollfd *poll_fd, MonoMList **list)
{
	g_assert (poll_fd);
	g_assert (list);

	if (fd == -1 || poll_fd->revents == 0)
		return FALSE;

	if (!*list) {
		POLL_INIT_FD (poll_fd, -1, 0);
	} else {
		if ((poll_fd->revents & (MONO_POLLIN | MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL)) != 0) {
			MonoSocketAsyncResult *io_event = get_state (list, MONO_POLLIN);
			if (io_event)
				mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) io_event)->vtable->domain, io_event);
		}
		if ((poll_fd->revents & (MONO_POLLOUT | MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL)) != 0) {
			MonoSocketAsyncResult *io_event = get_state (list, MONO_POLLOUT);
			if (io_event)
				mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) io_event)->vtable->domain, io_event);
		}

		poll_fd->events = get_events (*list);
	}

	return TRUE;
}

static void
polling_thread (gpointer data)
{
	io_thread_status = STATUS_INITIALIZED;

	for (;;) {
		guint i;
		guint max;
		gint ready = 0;

		mono_gc_set_skip_thread (TRUE);

		mono_mutex_lock (&threadpool_io->updates_lock);
		for (i = 0; i < threadpool_io->updates_size; ++i) {
			switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
			case BACKEND_EPOLL:
				epoll_thread_add_update (&threadpool_io->updates [i]);
				break;
#elif defined(HAVE_KQUEUE)
			case BACKEND_KQUEUE:
				kqueue_thread_add_update (&threadpool_io->updates [i]);
				break;
#endif
			case BACKEND_POLL:
				poll_thread_add_update (&threadpool_io->updates [i]);
				break;
			default:
				g_assert_not_reached ();
			}
			
		}
		if (threadpool_io->updates_size > 0) {
			threadpool_io->updates_size = 0;
			threadpool_io->updates = g_renew (ThreadPoolIOUpdate, threadpool_io->updates, threadpool_io->updates_size);
		}
		mono_mutex_unlock (&threadpool_io->updates_lock);

		switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
		case BACKEND_EPOLL:
			ready = epoll_thread_wait_for_event ();
			break;
#elif defined(HAVE_KQUEUE)
		case BACKEND_KQUEUE:
			ready = kqueue_thread_wait_for_event ();
			break;
#endif
		case BACKEND_POLL:
			ready = poll_thread_wait_for_event ();
			break;
		default:
			g_assert_not_reached ();
		}

		mono_gc_set_skip_thread (FALSE);

		if (ready == -1 || mono_runtime_is_shutting_down ())
			break;

		switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
		case BACKEND_EPOLL:
			max = EPOLL_NEVENTS;
			break;
#elif defined(HAVE_KQUEUE)
		case BACKEND_KQUEUE:
			max = KQUEUE_NEVENTS;
			break;
#endif
		case BACKEND_POLL:
			max = threadpool_io->poll.fds_max;
			break;
		default:
			g_assert_not_reached ();
		}

		mono_mutex_lock (&threadpool_io->states_lock);
		for (i = 0; i < max; ++i) {
			MonoMList *list;
			gboolean created;
			gint fd;

			switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
			case BACKEND_EPOLL:
				fd = epoll_thread_get_fd_at (i);
				break;
#elif defined(HAVE_KQUEUE)
			case BACKEND_KQUEUE:
				fd = kqueue_thread_get_fd_at (i);
				break;
#endif
			case BACKEND_POLL:
				fd = poll_thread_get_fd_at (i);
				break;
			default:
				g_assert_not_reached ();
			}

			if (fd == threadpool_io->wakeup_pipes [0]) {
				polling_thread_drain_wakeup_pipes ();
				continue;
			}

			list = mono_g_hash_table_lookup (threadpool_io->states, GINT_TO_POINTER (fd));

			switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
			case BACKEND_EPOLL:
				created = epoll_thread_create_socket_async_results (fd, &threadpool_io->epoll.events [i], &list);
				break;
#elif defined(HAVE_KQUEUE)
			case BACKEND_KQUEUE:
				created = kqueue_thread_create_socket_async_results (fd, &threadpool_io->kqueue.events [i], &list);
				break;
#endif
			case BACKEND_POLL:
				created = poll_thread_create_socket_async_results (fd, &threadpool_io->poll.fds [i], &list);
				break;
			default:
				g_assert_not_reached ();
			}

			if (!created)
				continue;

			if (list)
				mono_g_hash_table_replace (threadpool_io->states, GINT_TO_POINTER (fd), list);
			else
				mono_g_hash_table_remove (threadpool_io->states, GINT_TO_POINTER (fd));

			if (-- ready == 0)
				break;
		}
		mono_mutex_unlock (&threadpool_io->states_lock);
	}

	io_thread_status = STATUS_CLEANED_UP;
}

static void
wakeup_pipes_init (void)
{
#if !defined(HOST_WIN32)
	if (pipe (threadpool_io->wakeup_pipes) == -1)
		g_error ("wakeup_pipes_init: pipe () failed, error (%d) %s\n", errno, g_strerror (errno));
	if (fcntl (threadpool_io->wakeup_pipes [0], F_SETFL, O_NONBLOCK) == -1)
		g_error ("wakeup_pipes_init: fcntl () failed, error (%d) %s\n", errno, g_strerror (errno));
#else
	struct sockaddr_in client;
	struct sockaddr_in server;
	SOCKET server_sock;
	gulong arg;
	gint size;

	server_sock = socket (AF_INET, SOCK_STREAM, IPPROTO_TCP);
	g_assert (server_sock != INVALID_SOCKET);
	threadpool_io->wakeup_pipes [1] = socket (AF_INET, SOCK_STREAM, IPPROTO_TCP);
	g_assert (threadpool_io->wakeup_pipes [1] != INVALID_SOCKET);

	server.sin_family = AF_INET;
	server.sin_addr.s_addr = inet_addr ("127.0.0.1");
	server.sin_port = 0;
	if (bind (server_sock, (SOCKADDR*) &server, sizeof (server)) == SOCKET_ERROR) {
		closesocket (server_sock);
		g_error ("wakeup_pipes_init: bind () failed, error (%d)\n", WSAGetLastError ());
	}

	size = sizeof (server);
	if (getsockname (server_sock, (SOCKADDR*) &server, &size) == SOCKET_ERROR) {
		closesocket (server_sock);
		g_error ("wakeup_pipes_init: getsockname () failed, error (%d)\n", WSAGetLastError ());
	}
	if (listen (server_sock, 1024) == SOCKET_ERROR) {
		closesocket (server_sock);
		g_error ("wakeup_pipes_init: listen () failed, error (%d)\n", WSAGetLastError ());
	}
	if (connect ((SOCKET) threadpool_io->wakeup_pipes [1], (SOCKADDR*) &server, sizeof (server)) == SOCKET_ERROR) {
		closesocket (server_sock);
		g_error ("wakeup_pipes_init: connect () failed, error (%d)\n", WSAGetLastError ());
	}

	size = sizeof (client);
	threadpool_io->wakeup_pipes [0] = accept (server_sock, (SOCKADDR *) &client, &size);
	g_assert (threadpool_io->wakeup_pipes [0] != INVALID_SOCKET);

	arg = 1;
	if (ioctlsocket (threadpool_io->wakeup_pipes [0], FIONBIO, &arg) == SOCKET_ERROR) {
		closesocket (threadpool_io->wakeup_pipes [0]);
		closesocket (server_sock);
		g_error ("wakeup_pipes_init: ioctlsocket () failed, error (%d)\n", WSAGetLastError ());
	}

	closesocket (server_sock);
#endif
}

static void
ensure_initialized (void)
{
	if (io_status >= STATUS_INITIALIZED)
		return;
	if (io_status == STATUS_INITIALIZING || InterlockedCompareExchange (&io_status, STATUS_INITIALIZING, STATUS_NOT_INITIALIZED) != STATUS_NOT_INITIALIZED) {
		while (io_status == STATUS_INITIALIZING)
			mono_thread_info_yield ();
		g_assert (io_status >= STATUS_INITIALIZED);
		return;
	}

	g_assert (!threadpool_io);
	threadpool_io = g_new0 (ThreadPoolIO, 1);
	g_assert (threadpool_io);

	threadpool_io->states = mono_g_hash_table_new_type (g_direct_hash, g_direct_equal, MONO_HASH_VALUE_GC);
	MONO_GC_REGISTER_ROOT_FIXED (threadpool_io->states);
	mono_mutex_init (&threadpool_io->states_lock);

	threadpool_io->updates = NULL;
	threadpool_io->updates_size = 0;
	mono_mutex_init (&threadpool_io->updates_lock);

#if defined(HAVE_EPOLL)
	threadpool_io->backend = BACKEND_EPOLL;
#elif defined(HAVE_KQUEUE)
	threadpool_io->backend = BACKEND_KQUEUE;
#else
	threadpool_io->backend = BACKEND_POLL;
#endif
	if (g_getenv ("MONO_DISABLE_AIO") != NULL)
		threadpool_io->backend = BACKEND_POLL;

	wakeup_pipes_init ();

retry_init_backend:
	switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
	case BACKEND_EPOLL:
		if (!epoll_init ()) {
			threadpool_io->backend = BACKEND_POLL;
			goto retry_init_backend;
		}
		break;
#elif defined(HAVE_KQUEUE)
	case BACKEND_KQUEUE:
		if (!kqueue_init ()) {
			threadpool_io->backend = BACKEND_POLL;
			goto retry_init_backend;
		}
		break;
#endif
	case BACKEND_POLL:
		if (!poll_init ())
			g_error ("ensure_initialized: poll_init () failed");
		break;
	default:
		g_assert_not_reached ();
	}

	if (!mono_thread_create_internal (mono_get_root_domain (), polling_thread, NULL, TRUE, SMALL_STACK))
		g_error ("ensure_initialized: mono_thread_create_internal () failed");

	io_thread_status = STATUS_INITIALIZING;
	mono_memory_write_barrier ();

	io_status = STATUS_INITIALIZED;
}

static void
ensure_cleanedup (void)
{
	if (io_status == STATUS_NOT_INITIALIZED && InterlockedCompareExchange (&io_status, STATUS_CLEANED_UP, STATUS_NOT_INITIALIZED) == STATUS_NOT_INITIALIZED)
		return;
	if (io_status == STATUS_INITIALIZING) {
		while (io_status == STATUS_INITIALIZING)
			mono_thread_info_yield ();
	}
	if (io_status == STATUS_CLEANED_UP)
		return;
	if (io_status == STATUS_CLEANING_UP || InterlockedCompareExchange (&io_status, STATUS_CLEANING_UP, STATUS_INITIALIZED) != STATUS_INITIALIZED) {
		while (io_status == STATUS_CLEANING_UP)
			mono_thread_info_yield ();
		g_assert (io_status == STATUS_CLEANED_UP);
		return;
	}

	/* we make the assumption along the code that we are
	 * cleaning up only if the runtime is shutting down */
	g_assert (mono_runtime_is_shutting_down ());

	polling_thread_wakeup ();
	while (io_thread_status != STATUS_CLEANED_UP)
		usleep (1000);

	MONO_GC_UNREGISTER_ROOT (threadpool_io->states);
	mono_g_hash_table_destroy (threadpool_io->states);
	mono_mutex_destroy (&threadpool_io->states_lock);

	g_free (threadpool_io->updates);
	mono_mutex_destroy (&threadpool_io->updates_lock);

	switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
	case BACKEND_EPOLL:
		epoll_cleanup ();
		break;
#elif defined(HAVE_KQUEUE)
	case BACKEND_KQUEUE:
		kqueue_cleanup ();
		break;
#endif
	case BACKEND_POLL:
		poll_cleanup ();
		break;
	default:
		g_assert_not_reached ();
	}

#if !defined(HOST_WIN32)
	close (threadpool_io->wakeup_pipes [0]);
	close (threadpool_io->wakeup_pipes [1]);
#else
	closesocket (threadpool_io->wakeup_pipes [0]);
	closesocket (threadpool_io->wakeup_pipes [1]);
#endif

	g_assert (threadpool_io);
	g_free (threadpool_io);
	threadpool_io = NULL;
	g_assert (!threadpool_io);

	io_status = STATUS_CLEANED_UP;
}

gboolean
mono_threadpool_ms_is_io (MonoObject *target, MonoObject *state)
{
	static MonoClass *socket_class = NULL;
	static MonoClass *socket_async_class = NULL;
	static MonoClass *process_class = NULL;
	static MonoClass *async_read_handler_class = NULL;
	MonoClass *class;
	MonoSocketAsyncResult *sockares;

	if (!mono_defaults.system)
		mono_defaults.system = mono_image_loaded ("System");
	if (!mono_defaults.system)
		return FALSE;
	g_assert (mono_defaults.system);

	if (!socket_class)
		socket_class = mono_class_from_name (mono_defaults.system, "System.Net.Sockets", "Socket");
	g_assert (socket_class);

	if (!process_class)
		process_class = mono_class_from_name (mono_defaults.system, "System.Diagnostics", "Process");
	g_assert (process_class);

	class = target->vtable->klass;

	if (!socket_async_class) {
		if (class->nested_in && class->nested_in == socket_class && strcmp (class->name, "SocketAsyncCall") == 0)
			socket_async_class = class;
	}

	if (!async_read_handler_class) {
		if (class->nested_in && class->nested_in == process_class && strcmp (class->name, "AsyncReadHandler") == 0)
			async_read_handler_class = class;
	}

	if (class != socket_async_class && class != async_read_handler_class)
		return FALSE;

	sockares = (MonoSocketAsyncResult*) state;
	if (sockares->operation < AIO_OP_FIRST || sockares->operation >= AIO_OP_LAST)
		return FALSE;

	return TRUE;
}

void
mono_threadpool_ms_io_cleanup (void)
{
	ensure_cleanedup ();
}

MonoAsyncResult *
mono_threadpool_ms_io_add (MonoAsyncResult *ares, MonoSocketAsyncResult *sockares)
{
	MonoMList *list;
	gboolean is_new;
	gint events;
	gint fd;

	g_assert (ares);
	g_assert (sockares);

	if (mono_runtime_is_shutting_down ())
		return NULL;

	ensure_initialized ();

	MONO_OBJECT_SETREF (sockares, ares, ares);

	fd = GPOINTER_TO_INT (sockares->handle);

	mono_mutex_lock (&threadpool_io->states_lock);
	g_assert (threadpool_io->states);

	list = mono_g_hash_table_lookup (threadpool_io->states, GINT_TO_POINTER (fd));
	is_new = list == NULL;
	list = mono_mlist_append (list, (MonoObject*) sockares);
	mono_g_hash_table_replace (threadpool_io->states, sockares->handle, list);

	events = get_events (list);

	switch (threadpool_io->backend) {
#if defined(HAVE_EPOLL)
	case BACKEND_EPOLL: {
		epoll_update (fd, events, is_new);
		break;
	}
#elif defined(HAVE_KQUEUE)
	case BACKEND_KQUEUE: {
		kqueue_update (fd, events, is_new);
		break;
	}
#endif
	case BACKEND_POLL: {
		poll_update (fd, events, is_new);
		break;
	}
	default:
		g_assert_not_reached ();
	}

	mono_mutex_unlock (&threadpool_io->states_lock);

	return ares;
}

void
mono_threadpool_ms_io_remove_socket (int fd)
{
	MonoMList *list;

	if (io_status != STATUS_INITIALIZED)
		return;

	mono_mutex_lock (&threadpool_io->states_lock);
	g_assert (threadpool_io->states);
	list = mono_g_hash_table_lookup (threadpool_io->states, GINT_TO_POINTER (fd));
	if (list)
		mono_g_hash_table_remove (threadpool_io->states, GINT_TO_POINTER (fd));
	mono_mutex_unlock (&threadpool_io->states_lock);

	while (list) {
		MonoSocketAsyncResult *sockares, *sockares2;

		sockares = (MonoSocketAsyncResult*) mono_mlist_get_data (list);
		if (sockares->operation == AIO_OP_RECEIVE)
			sockares->operation = AIO_OP_RECV_JUST_CALLBACK;
		else if (sockares->operation == AIO_OP_SEND)
			sockares->operation = AIO_OP_SEND_JUST_CALLBACK;

		sockares2 = get_state (&list, MONO_POLLIN);
		if (sockares2)
			mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) sockares2)->vtable->domain, sockares2);

		if (!list)
			break;

		sockares2 = get_state (&list, MONO_POLLOUT);
		if (sockares2)
			mono_threadpool_io_enqueue_socket_async_result (((MonoObject*) sockares2)->vtable->domain, sockares2);
	}
}

static gboolean
remove_sockstate_for_domain (gpointer key, gpointer value, gpointer user_data)
{
	MonoMList *list;
	gboolean remove = FALSE;

	for (list = value; list; list = mono_mlist_next (list)) {
		MonoObject *data = mono_mlist_get_data (list);
		if (mono_object_domain (data) == user_data) {
			remove = TRUE;
			mono_mlist_set_data (list, NULL);
		}
	}

	//FIXME is there some sort of additional unregistration we need to perform here?
	return remove;
}

void
mono_threadpool_ms_io_remove_domain_jobs (MonoDomain *domain)
{
	if (io_status == STATUS_INITIALIZED) {
		mono_mutex_lock (&threadpool_io->states_lock);
		mono_g_hash_table_foreach_remove (threadpool_io->states, remove_sockstate_for_domain, domain);
		mono_mutex_unlock (&threadpool_io->states_lock);
	}
}

void
mono_threadpool_io_enqueue_socket_async_result (MonoDomain *domain, MonoSocketAsyncResult *sockares)
{
	static MonoClass *socket_runtime_work_item_class = NULL;
	MonoSocketRuntimeWorkItem *srwi;

	g_assert (sockares);

	if (!mono_defaults.system)
		mono_defaults.system = mono_image_loaded ("System");
	g_assert (mono_defaults.system);

	if (!socket_runtime_work_item_class)
		socket_runtime_work_item_class = mono_class_from_name (mono_defaults.system, "System.Net.Sockets", "MonoSocketRuntimeWorkItem");
	g_assert (socket_runtime_work_item_class);

	srwi = (MonoSocketRuntimeWorkItem*) mono_object_new (domain, socket_runtime_work_item_class);
	MONO_OBJECT_SETREF (srwi, socket_async_result, sockares);

	mono_threadpool_ms_enqueue_work_item (domain, (MonoObject*) srwi);
}

void
ves_icall_System_Net_Sockets_MonoSocketRuntimeWorkItem_ExecuteWorkItem (MonoSocketRuntimeWorkItem *rwi)
{
	MonoSocketAsyncResult *sockares;
	MonoAsyncResult *ares;
	MonoObject *exc = NULL;

	g_assert (rwi);

	sockares = rwi->socket_async_result;
	g_assert (sockares);
	g_assert (sockares->ares);

	switch (sockares->operation) {
	case AIO_OP_RECEIVE:
		sockares->total = ves_icall_System_Net_Sockets_Socket_Receive_internal ((SOCKET) (gssize) sockares->handle, sockares->buffer, sockares->offset,
		                                                                            sockares->size, sockares->socket_flags, &sockares->error);
		break;
	case AIO_OP_SEND:
		sockares->total = ves_icall_System_Net_Sockets_Socket_Send_internal ((SOCKET) (gssize) sockares->handle, sockares->buffer, sockares->offset,
		                                                                            sockares->size, sockares->socket_flags, &sockares->error);
		break;
	}

	ares = sockares->ares;
	g_assert (ares);

	mono_async_result_invoke (ares, &exc);

	if (sockares->completed && sockares->callback) {
		MonoAsyncResult *cb_ares;

		/* Don't call mono_async_result_new() to avoid capturing the context */
		cb_ares = (MonoAsyncResult*) mono_object_new (mono_domain_get (), mono_defaults.asyncresult_class);
		MONO_OBJECT_SETREF (cb_ares, async_delegate, sockares->callback);
		MONO_OBJECT_SETREF (cb_ares, async_state, (MonoObject*) sockares);

		mono_threadpool_ms_enqueue_async_result (mono_domain_get (), cb_ares);
	}

	if (exc)
		mono_raise_exception ((MonoException*) exc);
}

#else

gboolean
mono_threadpool_ms_is_io (MonoObject *target, MonoObject *state)
{
	return FALSE;
}

void
mono_threadpool_ms_io_cleanup (void)
{
	g_assert_not_reached ();
}

MonoAsyncResult *
mono_threadpool_ms_io_add (MonoAsyncResult *ares, MonoSocketAsyncResult *sockares)
{
	g_assert_not_reached ();
}

void
mono_threadpool_ms_io_remove_socket (int fd)
{
	g_assert_not_reached ();
}

void
mono_threadpool_ms_io_remove_domain_jobs (MonoDomain *domain)
{
	g_assert_not_reached ();
}

void
mono_threadpool_io_enqueue_socket_async_result (MonoDomain *domain, MonoSocketAsyncResult *sockares)
{
	g_assert_not_reached ();
}

void
ves_icall_System_Net_Sockets_MonoSocketRuntimeWorkItem_ExecuteWorkItem (MonoSocketRuntimeWorkItem *rwi)
{
	g_assert_not_reached ();
}

#endif