/*
 * threadpool.c: global thread pool
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (C) 2001-2003 Ximian, Inc.
 * (c) 2004,2005 Novell, Inc. (http://www.novell.com)
 */

#include <config.h>
#include <glib.h>

#ifdef PLATFORM_WIN32
#define WINVER 0x0500
#define _WIN32_WINNT 0x0500
#define THREADS_PER_CPU	25
#else
#define THREADS_PER_CPU	50
#endif

#include <mono/metadata/domain-internals.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/socket-io.h>
#include <mono/io-layer/io-layer.h>
#include <mono/os/gc_wrapper.h>
#include <errno.h>
#include <sys/time.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>
#include <string.h>

#include <mono/utils/mono-poll.h>
#ifdef HAVE_EPOLL
#include <sys/epoll.h>
#endif

#include "mono/io-layer/socket-wrappers.h"

#include "threadpool.h"

#define THREAD_WANTS_A_BREAK(t) ((t->state & (ThreadState_StopRequested | \
						ThreadState_SuspendRequested)) != 0)

#undef EPOLL_DEBUG

/* maximum number of worker threads */
static int mono_max_worker_threads = THREADS_PER_CPU;
static int mono_min_worker_threads = 0;
static int mono_io_max_worker_threads = THREADS_PER_CPU * 2;

/* current number of worker threads */
static int mono_worker_threads = 0;
static int io_worker_threads = 0;

/* current number of busy threads */
static int busy_worker_threads = 0;
static int busy_io_worker_threads;

/* mono_thread_pool_init called */
static int tp_inited;

/* we use this to store a reference to the AsyncResult to avoid GC */
static MonoGHashTable *ares_htable = NULL;

static CRITICAL_SECTION ares_lock;
static CRITICAL_SECTION io_queue_lock;
static int pending_io_items;

typedef struct {
	CRITICAL_SECTION io_lock; /* access to sock_to_state */
	int inited;
	int pipe [2];
	GHashTable *sock_to_state;

	HANDLE new_sem; /* access to newpfd and write side of the pipe */
	mono_pollfd *newpfd;
	gboolean epoll_disabled;
#ifdef HAVE_EPOLL
	int epollfd;
#endif
} SocketIOData;

static SocketIOData socket_io_data;

/* we append a job */
static HANDLE job_added;
static HANDLE io_job_added;

typedef struct {
	MonoMethodMessage *msg;
	HANDLE             wait_event;
	MonoMethod        *cb_method;
	MonoDelegate      *cb_target;
	MonoObject        *state;
	MonoObject        *res;
	MonoArray         *out_args;
} ASyncCall;

static void async_invoke_thread (gpointer data);
static void append_job (CRITICAL_SECTION *cs, GList **plist, gpointer ar);
static void start_thread_or_queue (MonoAsyncResult *ares);
static void mono_async_invoke (MonoAsyncResult *ares);
static gpointer dequeue_job (CRITICAL_SECTION *cs, GList **plist);

static GList *async_call_queue = NULL;
static GList *async_io_queue = NULL;

static MonoClass *socket_async_call_klass;

#define INIT_POLLFD(a, b, c) {(a)->fd = b; (a)->events = c; (a)->revents = 0;}
enum {
	AIO_OP_FIRST,
	AIO_OP_ACCEPT = 0,
	AIO_OP_CONNECT,
	AIO_OP_RECEIVE,
	AIO_OP_RECEIVEFROM,
	AIO_OP_SEND,
	AIO_OP_SENDTO,
	AIO_OP_LAST
};

static void
socket_io_cleanup (SocketIOData *data)
{
	gint release;

	if (data->inited == 0)
		return;

	EnterCriticalSection (&data->io_lock);
	data->inited = 0;
#ifdef PLATFORM_WIN32
	closesocket (data->pipe [0]);
	closesocket (data->pipe [1]);
#else
	close (data->pipe [0]);
	close (data->pipe [1]);
#endif
	data->pipe [0] = -1;
	data->pipe [1] = -1;
	if (data->new_sem)
		CloseHandle (data->new_sem);
	data->new_sem = NULL;
	g_hash_table_destroy (data->sock_to_state);
	data->sock_to_state = NULL;
	g_list_free (async_io_queue);
	async_io_queue = NULL;
	release = (gint) InterlockedCompareExchange (&io_worker_threads, 0, -1);
	if (io_job_added)
		ReleaseSemaphore (io_job_added, release, NULL);
	g_free (data->newpfd);
	data->newpfd = NULL;
#ifdef HAVE_EPOLL
	if (FALSE == data->epoll_disabled)
		close (data->epollfd);
#endif
	LeaveCriticalSection (&data->io_lock);
}

static int
get_event_from_state (MonoSocketAsyncResult *state)
{
	switch (state->operation) {
	case AIO_OP_ACCEPT:
	case AIO_OP_RECEIVE:
	case AIO_OP_RECEIVEFROM:
		return MONO_POLLIN;
	case AIO_OP_SEND:
	case AIO_OP_SENDTO:
	case AIO_OP_CONNECT:
		return MONO_POLLOUT;
	default: /* Should never happen */
		g_print ("get_event_from_state: unknown value in switch!!!\n");
		return 0;
	}
}

static int
get_events_from_list (GSList *list)
{
	MonoSocketAsyncResult *state;
	int events = 0;

	while (list && list->data) {
		state = (MonoSocketAsyncResult *) list->data;
		events |= get_event_from_state (state);
		list = list->next;
	}

	return events;
}

#define ICALL_RECV(x)	ves_icall_System_Net_Sockets_Socket_Receive_internal (\
				(SOCKET) x->handle, x->buffer, x->offset, x->size,\
				 x->socket_flags, &x->error);

#define ICALL_SEND(x)	ves_icall_System_Net_Sockets_Socket_Send_internal (\
				(SOCKET) x->handle, x->buffer, x->offset, x->size,\
				 x->socket_flags, &x->error);

static void
async_invoke_io_thread (gpointer data)
{
	MonoDomain *domain;
	MonoThread *thread;
	thread = mono_thread_current ();
	thread->threadpool_thread = TRUE;
	thread->state |= ThreadState_Background;

	for (;;) {
		MonoSocketAsyncResult *state;
		MonoAsyncResult *ar;

		state = (MonoSocketAsyncResult *) data;
		if (state) {
			InterlockedDecrement (&pending_io_items);
			ar = state->ares;
			/* worker threads invokes methods in different domains,
			 * so we need to set the right domain here */
			switch (state->operation) {
			case AIO_OP_RECEIVE:
				state->total = ICALL_RECV (state);
				break;
			case AIO_OP_SEND:
				state->total = ICALL_SEND (state);
				break;
			}

			domain = ((MonoObject *)ar)->vtable->domain;
			if (mono_domain_set (domain, FALSE)) {
				mono_thread_push_appdomain_ref (domain);
				mono_async_invoke (ar);
				mono_thread_pop_appdomain_ref ();
			}
			InterlockedDecrement (&busy_io_worker_threads);
		}

		data = dequeue_job (&io_queue_lock, &async_io_queue);
	
		if (!data) {
			guint32 wr;
			int timeout = 10000;
			guint32 start_time = GetTickCount ();
			
			do {
				wr = WaitForSingleObjectEx (io_job_added, (guint32)timeout, TRUE);
				if (THREAD_WANTS_A_BREAK (thread))
					mono_thread_interruption_checkpoint ();
			
				timeout -= GetTickCount () - start_time;
			
				if (wr != WAIT_TIMEOUT)
					data = dequeue_job (&io_queue_lock, &async_io_queue);
			}
			while (!data && timeout > 0);
		}

		if (!data) {
			if (InterlockedDecrement (&io_worker_threads) < 2) {
				/* If we have pending items, keep the thread alive */
				if (InterlockedCompareExchange (&pending_io_items, 0, 0) != 0) {
					InterlockedIncrement (&io_worker_threads);
					continue;
				}
			}
			return;
		}
		
		InterlockedIncrement (&busy_io_worker_threads);
	}

	g_assert_not_reached ();
}

static void
start_io_thread_or_queue (MonoSocketAsyncResult *ares)
{
	int busy, worker;
	MonoDomain *domain;

	busy = (int) InterlockedCompareExchange (&busy_io_worker_threads, 0, -1);
	worker = (int) InterlockedCompareExchange (&io_worker_threads, 0, -1); 
	if (worker <= ++busy &&
	    worker < mono_io_max_worker_threads) {
		InterlockedIncrement (&busy_io_worker_threads);
		InterlockedIncrement (&io_worker_threads);
		domain = ((ares) ? ((MonoObject *) ares)->vtable->domain : mono_domain_get ());
		mono_thread_create (domain, async_invoke_io_thread, ares);
	} else {
		append_job (&io_queue_lock, &async_io_queue, ares);
		ReleaseSemaphore (io_job_added, 1, NULL);
	}
}

static GSList *
process_io_event (GSList *list, int event)
{
	MonoSocketAsyncResult *state;
	GSList *oldlist;

	oldlist = list;
	state = NULL;
	while (list) {
		state = (MonoSocketAsyncResult *) list->data;
		if (get_event_from_state (state) == event)
			break;
		
		list = list->next;
	}

	if (list != NULL) {
		oldlist = g_slist_remove_link (oldlist, list);
		g_slist_free_1 (list);
#ifdef EPOLL_DEBUG
		g_print ("Dispatching event %d on socket %d\n", event, state->handle);
#endif
		InterlockedIncrement (&pending_io_items);
		start_io_thread_or_queue (state);
	}

	return oldlist;
}

static int
mark_bad_fds (mono_pollfd *pfds, int nfds)
{
	int i, ret;
	mono_pollfd *pfd;
	int count = 0;

	for (i = 0; i < nfds; i++) {
		pfd = &pfds [i];
		if (pfd->fd == -1)
			continue;

		ret = mono_poll (pfds, 1, 0);
		if (ret == -1 && errno == EBADF) {
			pfd->revents |= MONO_POLLNVAL;
			count++;
		} else if (ret == 1) {
			count++;
		}
	}

	return count;
}

static void
socket_io_poll_main (gpointer p)
{
#define INITIAL_POLLFD_SIZE	1024
#define POLL_ERRORS (MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL)
	SocketIOData *data = p;
	mono_pollfd *pfds;
	gint maxfd = 1;
	gint allocated;
	gint i;
	MonoThread *thread;

	thread = mono_thread_current ();
	thread->threadpool_thread = TRUE;
	thread->state |= ThreadState_Background;

	allocated = INITIAL_POLLFD_SIZE;
	pfds = g_new0 (mono_pollfd, allocated);
	INIT_POLLFD (pfds, data->pipe [0], MONO_POLLIN);
	for (i = 1; i < allocated; i++)
		INIT_POLLFD (&pfds [i], -1, 0);

	while (1) {
		int nsock = 0;
		mono_pollfd *pfd;
		char one [1];
		GSList *list;

		do {
			if (nsock == -1) {
				if (THREAD_WANTS_A_BREAK (thread))
					mono_thread_interruption_checkpoint ();
			}

			nsock = mono_poll (pfds, maxfd, -1);
		} while (nsock == -1 && errno == EINTR);

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

		if (nsock == -1 && errno == EBADF) {
			pfds->revents = 0; /* Just in case... */
			nsock = mark_bad_fds (pfds, maxfd);
		}

		if ((pfds->revents & POLL_ERRORS) != 0) {
			/* We're supposed to die now, as the pipe has been closed */
			g_free (pfds);
			socket_io_cleanup (data);
			return;
		}

		/* Got a new socket */
		if ((pfds->revents & MONO_POLLIN) != 0) {
			int nread;

			for (i = 1; i < allocated; i++) {
				pfd = &pfds [i];
				if (pfd->fd == -1 || pfd->fd == data->newpfd->fd)
					break;
			}

			if (i == allocated) {
				mono_pollfd *oldfd;

				oldfd = pfds;
				i = allocated;
				allocated = allocated * 2;
				pfds = g_renew (mono_pollfd, oldfd, allocated);
				g_free (oldfd);
				for (; i < allocated; i++)
					INIT_POLLFD (&pfds [i], -1, 0);
			}
#ifndef PLATFORM_WIN32
			nread = read (data->pipe [0], one, 1);
#else
			nread = recv ((SOCKET) data->pipe [0], one, 1, 0);
#endif
			if (nread <= 0) {
				g_free (pfds);
				return; /* we're closed */
			}

			INIT_POLLFD (&pfds [i], data->newpfd->fd, data->newpfd->events);
			ReleaseSemaphore (data->new_sem, 1, NULL);
			if (i >= maxfd)
				maxfd = i + 1;
			nsock--;
		}

		if (nsock == 0)
			continue;

		EnterCriticalSection (&data->io_lock);
		if (data->inited == 0) {
			g_free (pfds);
			return; /* cleanup called */
		}

		for (i = 1; i < maxfd && nsock > 0; i++) {
			pfd = &pfds [i];
			if (pfd->fd == -1 || pfd->revents == 0)
				continue;

			nsock--;
			list = g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (pfd->fd));
			if (list != NULL && (pfd->revents & (MONO_POLLIN | POLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLIN);
			}

			if (list != NULL && (pfd->revents & (MONO_POLLOUT | POLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLOUT);
			}

			if (list != NULL) {
				g_hash_table_replace (data->sock_to_state, GINT_TO_POINTER (pfd->fd), list);
				pfd->events = get_events_from_list (list);
			} else {
				g_hash_table_remove (data->sock_to_state, GINT_TO_POINTER (pfd->fd));
				pfd->fd = -1;
				if (i == maxfd - 1)
					maxfd--;
			}
		}
		LeaveCriticalSection (&data->io_lock);
	}
}

#ifdef HAVE_EPOLL
#define EPOLL_ERRORS (EPOLLERR | EPOLLHUP)
static void
socket_io_epoll_main (gpointer p)
{
	SocketIOData *data;
	int epollfd;
	MonoThread *thread;
	struct epoll_event *events, *evt;
	const int nevents = 512;
	int ready = 0, i;

	data = p;
	epollfd = data->epollfd;
	thread = mono_thread_current ();
	thread->threadpool_thread = TRUE;
	thread->state |= ThreadState_Background;
	events = g_new0 (struct epoll_event, nevents);

	while (1) {
		do {
			if (ready == -1) {
				if (THREAD_WANTS_A_BREAK (thread))
					mono_thread_interruption_checkpoint ();
			}
#ifdef EPOLL_DEBUG
			g_print ("epoll_wait init\n");
#endif
			ready = epoll_wait (epollfd, events, nevents, -1);
#ifdef EPOLL_DEBUG
			{
			int err = errno;
			g_print ("epoll_wait end with %d ready sockets (%d %s).\n", ready, err, (err) ? g_strerror (err) : "");
			errno = err;
			}
#endif
		} while (ready == -1 && errno == EINTR);

		if (ready == -1) {
			int err = errno;
			g_free (events);
			if (err != EBADF)
				g_warning ("epoll_wait: %d %s\n", err, g_strerror (err));

			close (epollfd);
			return;
		}

		EnterCriticalSection (&data->io_lock);
		if (data->inited == 0) {
#ifdef EPOLL_DEBUG
			g_print ("data->inited == 0\n");
#endif
			g_free (events);
			close (epollfd);
			return; /* cleanup called */
		}

		for (i = 0; i < ready; i++) {
			int fd;
			GSList *list;

			evt = &events [i];
			fd = evt->data.fd;
			list = g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (fd));
#ifdef EPOLL_DEBUG
			g_print ("Event %d on %d list length: %d\n", evt->events, fd, g_slist_length (list));
#endif
			if (list != NULL && (evt->events & (EPOLLIN | EPOLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLIN);
			}

			if (list != NULL && (evt->events & (EPOLLOUT | EPOLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLOUT);
			}

			if (list != NULL) {
				g_hash_table_replace (data->sock_to_state, GINT_TO_POINTER (fd), list);
				evt->events = get_events_from_list (list);
#ifdef EPOLL_DEBUG
				g_print ("MOD %d to %d\n", fd, evt->events);
#endif
				if (epoll_ctl (epollfd, EPOLL_CTL_MOD, fd, evt)) {
					if (epoll_ctl (epollfd, EPOLL_CTL_ADD, fd, evt) == -1) {
#ifdef EPOLL_DEBUG
						int err = errno;
						g_message ("epoll_ctl(MOD): %d %s fd: %d events: %d", err, g_strerror (err), fd, evt->events);
						errno = err;
#endif
					}
				}
			} else {
				g_hash_table_remove (data->sock_to_state, GINT_TO_POINTER (fd));
#ifdef EPOLL_DEBUG
				g_print ("DEL %d\n", fd);
#endif
				epoll_ctl (epollfd, EPOLL_CTL_DEL, fd, evt);
			}
		}
		LeaveCriticalSection (&data->io_lock);
	}
}
#endif

#ifdef PLATFORM_WIN32
static void
connect_hack (gpointer x)
{
	struct sockaddr_in *addr = (struct sockaddr_in *) x;
	int count = 0;

	while (connect ((SOCKET) socket_io_data.pipe [1], (SOCKADDR *) addr, sizeof (struct sockaddr_in))) {
		Sleep (500);
		if (++count > 3) {
			g_warning ("Error initializing async. sockets %d.\n", WSAGetLastError ());
			g_assert (WSAGetLastError ());
		}
	}
}
#endif

static void
socket_io_init (SocketIOData *data)
{
#ifdef PLATFORM_WIN32
	struct sockaddr_in server;
	struct sockaddr_in client;
	SOCKET srv;
	int len;
#endif
	int inited;

	inited = InterlockedCompareExchange (&data->inited, -1, -1);
	if (inited == 1)
		return;

	EnterCriticalSection (&data->io_lock);
	inited = InterlockedCompareExchange (&data->inited, -1, -1);
	if (inited == 1) {
		LeaveCriticalSection (&data->io_lock);
		return;
	}

#ifdef HAVE_EPOLL
	data->epoll_disabled = (g_getenv ("MONO_DISABLE_AIO") != NULL);
	if (FALSE == data->epoll_disabled) {
		data->epollfd = epoll_create (256);
		data->epoll_disabled = (data->epollfd == -1);
		if (data->epoll_disabled && g_getenv ("MONO_DEBUG"))
			g_message ("epoll_create() failed. Using plain poll().");
	} else {
		data->epollfd = -1;
	}
#else
	data->epoll_disabled = TRUE;
#endif

#ifndef PLATFORM_WIN32
	if (data->epoll_disabled) {
		if (pipe (data->pipe) != 0) {
			int err = errno;
			perror ("mono");
			g_assert (err);
		}
	} else {
		data->pipe [0] = -1;
		data->pipe [1] = -1;
	}
#else
	srv = socket (AF_INET, SOCK_STREAM, IPPROTO_TCP);
	g_assert (srv != INVALID_SOCKET);
	data->pipe [1] = socket (AF_INET, SOCK_STREAM, IPPROTO_TCP);
	g_assert (data->pipe [1] != INVALID_SOCKET);

	server.sin_family = AF_INET;
	server.sin_addr.s_addr = inet_addr ("127.0.0.1");
	server.sin_port = 0;
	if (bind (srv, (SOCKADDR *) &server, sizeof (server))) {
		g_print ("%d\n", WSAGetLastError ());
		g_assert (1 != 0);
	}

	len = sizeof (server);
	getsockname (srv, (SOCKADDR *) &server, &len);
	listen (srv, 1);
	mono_thread_create (mono_get_root_domain (), connect_hack, &server);
	len = sizeof (server);
	data->pipe [0] = accept (srv, (SOCKADDR *) &client, &len);
	g_assert (data->pipe [0] != INVALID_SOCKET);
	closesocket (srv);
#endif

	data->sock_to_state = g_hash_table_new (g_direct_hash, g_direct_equal);

	if (data->epoll_disabled)
		data->new_sem = CreateSemaphore (NULL, 1, 1, NULL);
	io_job_added = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
	InitializeCriticalSection (&io_queue_lock);
	if (data->epoll_disabled) {
		mono_thread_create (mono_get_root_domain (), socket_io_poll_main, data);
	}
#ifdef HAVE_EPOLL
	else {
		mono_thread_create (mono_get_root_domain (), socket_io_epoll_main, data);
	}
#endif
	InterlockedCompareExchange (&data->inited, 1, 0);
	LeaveCriticalSection (&data->io_lock);
}

static void
socket_io_add_poll (MonoSocketAsyncResult *state)
{
	int events;
	char msg [1];
	GSList *list;
	SocketIOData *data = &socket_io_data;

	WaitForSingleObject (data->new_sem, INFINITE);
	if (data->newpfd == NULL)
		data->newpfd = g_new0 (mono_pollfd, 1);

	EnterCriticalSection (&data->io_lock);
	list = g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (state->handle));
	if (list == NULL) {
		list = g_slist_alloc ();
		list->data = state;
	} else {
		list = g_slist_append (list, state);
	}

	events = get_events_from_list (list);
	INIT_POLLFD (data->newpfd, GPOINTER_TO_INT (state->handle), events);
	g_hash_table_replace (data->sock_to_state, GINT_TO_POINTER (state->handle), list);
	LeaveCriticalSection (&data->io_lock);
	*msg = (char) state->operation;
#ifndef PLATFORM_WIN32
	write (data->pipe [1], msg, 1);
#else
	send ((SOCKET) data->pipe [1], msg, 1, 0);
#endif
}

#ifdef HAVE_EPOLL
static gboolean
socket_io_add_epoll (MonoSocketAsyncResult *state)
{
	GSList *list;
	SocketIOData *data = &socket_io_data;
	struct epoll_event event;
	int epoll_op, ievt;
	int fd;

	memset (&event, 0, sizeof (struct epoll_event));
	fd = GPOINTER_TO_INT (state->handle);
	EnterCriticalSection (&data->io_lock);
	list = g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (fd));
	if (list == NULL) {
		list = g_slist_alloc ();
		list->data = state;
		epoll_op = EPOLL_CTL_ADD;
	} else {
		list = g_slist_append (list, state);
		epoll_op = EPOLL_CTL_MOD;
	}

	ievt = get_events_from_list (list);
	if ((ievt & MONO_POLLIN) != 0)
		event.events |= EPOLLIN;
	if ((ievt & MONO_POLLOUT) != 0)
		event.events |= EPOLLOUT;

	g_hash_table_replace (data->sock_to_state, state->handle, list);
	event.data.fd = fd;
#ifdef EPOLL_DEBUG
	g_print ("%s %d with %d\n", epoll_op == EPOLL_CTL_ADD ? "ADD" : "MOD", fd, event.events);
#endif
	if (epoll_ctl (data->epollfd, epoll_op, fd, &event) == -1) {
		int err = errno;
		if (epoll_op == EPOLL_CTL_ADD && err == EEXIST) {
			epoll_op = EPOLL_CTL_MOD;
			if (epoll_ctl (data->epollfd, epoll_op, fd, &event) == -1) {
				g_message ("epoll_ctl(MOD): %d %s\n", err, g_strerror (err));
			}
		}
	}

	LeaveCriticalSection (&data->io_lock);
	return TRUE;
}
#endif

static void
socket_io_add (MonoAsyncResult *ares, MonoSocketAsyncResult *state)
{
	socket_io_init (&socket_io_data);
	state->ares = ares;
#ifdef HAVE_EPOLL
	if (socket_io_data.epoll_disabled == FALSE) {
		if (socket_io_add_epoll (state))
			return;
	}
#endif
	socket_io_add_poll (state);
}

static gboolean
socket_io_filter (MonoObject *target, MonoObject *state)
{
	gint op;
	MonoSocketAsyncResult *sock_res = (MonoSocketAsyncResult *) state;
	MonoClass *klass;

	if (target == NULL || state == NULL)
		return FALSE;

	klass = InterlockedCompareExchangePointer ((gpointer *) &socket_async_call_klass, NULL, NULL);
	if (klass == NULL) {
		MonoImage *system_assembly = mono_image_loaded ("System");

		if (system_assembly == NULL)
			return FALSE;

		klass = mono_class_from_name (system_assembly, "System.Net.Sockets", "Socket/SocketAsyncCall");
		if (klass == NULL) {
			/* Should never happen... */
			g_print ("socket_io_filter: SocketAsyncCall class not found.\n");
			return FALSE;
		}

		InterlockedCompareExchangePointer ((gpointer *) &socket_async_call_klass, klass, NULL);
	}

	if (target->vtable->klass != klass)
		return FALSE;

	op = sock_res->operation;
	if (op < AIO_OP_FIRST || op >= AIO_OP_LAST)
		return FALSE;

	return TRUE;
}

static void
mono_async_invoke (MonoAsyncResult *ares)
{
	ASyncCall *ac = (ASyncCall *)ares->data;

	ac->msg->exc = NULL;
	ac->res = mono_message_invoke (ares->async_delegate, ac->msg, 
				       &ac->msg->exc, &ac->out_args);

	ares->completed = 1;

	/* call async callback if cb_method != null*/
	if (ac->cb_method) {
		MonoObject *exc = NULL;
		void *pa = &ares;
		mono_runtime_invoke (ac->cb_method, ac->cb_target, pa, &exc);
		if (!ac->msg->exc)
			ac->msg->exc = exc;
	}

	/* notify listeners */
	mono_monitor_enter ((MonoObject *) ares);
	if (ares->handle != NULL) {
		ac->wait_event = ((MonoWaitHandle *) ares->handle)->handle;
		SetEvent (ac->wait_event);
	}
	mono_monitor_exit ((MonoObject *) ares);

	EnterCriticalSection (&ares_lock);
	mono_g_hash_table_remove (ares_htable, ares);
	LeaveCriticalSection (&ares_lock);
}

void
mono_thread_pool_init ()
{
	SYSTEM_INFO info;
	int threads_per_cpu = THREADS_PER_CPU;

	if ((int) InterlockedCompareExchange (&tp_inited, 1, 0) == 1)
		return;

	MONO_GC_REGISTER_ROOT (ares_htable);
	InitializeCriticalSection (&socket_io_data.io_lock);
	InitializeCriticalSection (&ares_lock);
	ares_htable = mono_g_hash_table_new (NULL, NULL);
	job_added = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
	GetSystemInfo (&info);
	if (getenv ("MONO_THREADS_PER_CPU") != NULL) {
		threads_per_cpu = atoi (getenv ("MONO_THREADS_PER_CPU"));
		if (threads_per_cpu <= 0)
			threads_per_cpu = THREADS_PER_CPU;
	}

	mono_max_worker_threads = threads_per_cpu * info.dwNumberOfProcessors;
}

MonoAsyncResult *
mono_thread_pool_add (MonoObject *target, MonoMethodMessage *msg, MonoDelegate *async_callback,
		      MonoObject *state)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	ASyncCall *ac;

#ifdef HAVE_BOEHM_GC
	ac = GC_MALLOC (sizeof (ASyncCall));
#else
	/* We'll leak the event if creaated... */
	ac = g_new0 (ASyncCall, 1);
#endif
	ac->wait_event = NULL;
	ac->msg = msg;
	ac->state = state;

	if (async_callback) {
		ac->cb_method = mono_get_delegate_invoke (((MonoObject *)async_callback)->vtable->klass);
		ac->cb_target = async_callback;
	}

	ares = mono_async_result_new (domain, NULL, ac->state, ac);
	ares->async_delegate = target;

	EnterCriticalSection (&ares_lock);
	mono_g_hash_table_insert (ares_htable, ares, ares);
	LeaveCriticalSection (&ares_lock);

	if (socket_io_filter (target, state)) {
		socket_io_add (ares, (MonoSocketAsyncResult *) state);
		return ares;
	}

	start_thread_or_queue (ares);
	return ares;
}

static void
start_thread_or_queue (MonoAsyncResult *ares)
{
	int busy, worker;
	MonoDomain *domain;

	busy = (int) InterlockedCompareExchange (&busy_worker_threads, 0, -1);
	worker = (int) InterlockedCompareExchange (&mono_worker_threads, 0, -1); 
	if (worker <= ++busy &&
	    worker < mono_max_worker_threads) {
		InterlockedIncrement (&mono_worker_threads);
		InterlockedIncrement (&busy_worker_threads);
		domain = ((MonoObject *) ares)->vtable->domain;
		mono_thread_create (domain, async_invoke_thread, ares);
	} else {
		append_job (&mono_delegate_section, &async_call_queue, ares);
		ReleaseSemaphore (job_added, 1, NULL);
	}
}

MonoObject *
mono_thread_pool_finish (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc)
{
	ASyncCall *ac;

	*exc = NULL;
	*out_args = NULL;

	/* check if already finished */
	mono_monitor_enter ((MonoObject *) ares);
	
	if (ares->endinvoke_called) {
		*exc = (MonoObject *)mono_exception_from_name (mono_defaults.corlib, "System", 
					      "InvalidOperationException");
		mono_monitor_exit ((MonoObject *) ares);
		return NULL;
	}

	ares->endinvoke_called = 1;
	ac = (ASyncCall *)ares->data;

	g_assert (ac != NULL);

	/* wait until we are really finished */
	if (!ares->completed) {
		if (ares->handle == NULL) {
			ac->wait_event = CreateEvent (NULL, TRUE, FALSE, NULL);
			ares->handle = (MonoObject *) mono_wait_handle_new (mono_object_domain (ares), ac->wait_event);
		}
		mono_monitor_exit ((MonoObject *) ares);
		WaitForSingleObjectEx (ac->wait_event, INFINITE, TRUE);
	} else {
		mono_monitor_exit ((MonoObject *) ares);
	}

	*exc = ac->msg->exc;
	*out_args = ac->out_args;

	return ac->res;
}

void
mono_thread_pool_cleanup (void)
{
	gint release;

	EnterCriticalSection (&mono_delegate_section);
	g_list_free (async_call_queue);
	async_call_queue = NULL;
	release = (gint) InterlockedCompareExchange (&mono_worker_threads, 0, -1);
	LeaveCriticalSection (&mono_delegate_section);
	if (job_added)
		ReleaseSemaphore (job_added, release, NULL);

	socket_io_cleanup (&socket_io_data);
}

static void
append_job (CRITICAL_SECTION *cs, GList **plist, gpointer ar)
{
	GList *tmp, *list;

	EnterCriticalSection (cs);
	list = *plist;
	if (list == NULL) {
		list = g_list_append (list, ar); 
	} else {
		for (tmp = list; tmp && tmp->data != NULL; tmp = tmp->next);
		if (tmp == NULL) {
			list = g_list_append (list, ar); 
		} else {
			tmp->data = ar;
		}
	}
	*plist = list;
	LeaveCriticalSection (cs);
}

static gpointer
dequeue_job (CRITICAL_SECTION *cs, GList **plist)
{
	gpointer ar = NULL;
	GList *tmp, *tmp2, *list;

	EnterCriticalSection (cs);
	list = *plist;
	tmp = list;
	if (tmp) {
		ar = tmp->data;
		tmp->data = NULL;
		tmp2 = tmp;
		for (tmp2 = tmp; tmp2->next != NULL; tmp2 = tmp2->next);
		if (tmp2 != tmp) {
			list = tmp->next;
			tmp->next = NULL;
			tmp2->next = tmp;
			tmp->prev = tmp2;
		}
	}
	*plist = list;
	LeaveCriticalSection (cs);

	return ar;
}

static void
async_invoke_thread (gpointer data)
{
	MonoDomain *domain;
	MonoThread *thread;
	int workers, min;
 
	thread = mono_thread_current ();
	thread->threadpool_thread = TRUE;
	thread->state |= ThreadState_Background;

	for (;;) {
		MonoAsyncResult *ar;

		ar = (MonoAsyncResult *) data;
		if (ar) {
			/* worker threads invokes methods in different domains,
			 * so we need to set the right domain here */
			domain = ((MonoObject *)ar)->vtable->domain;
			if (mono_domain_set (domain, FALSE)) {
				mono_thread_push_appdomain_ref (domain);
				mono_async_invoke (ar);
				mono_thread_pop_appdomain_ref ();
			}
			InterlockedDecrement (&busy_worker_threads);
		}

		data = dequeue_job (&mono_delegate_section, &async_call_queue);
	
		if (!data) {
			guint32 wr;
			int timeout = 10000;
			guint32 start_time = GetTickCount ();
			
			do {
				wr = WaitForSingleObjectEx (job_added, (guint32)timeout, TRUE);
				if (THREAD_WANTS_A_BREAK (thread))
					mono_thread_interruption_checkpoint ();
			
				timeout -= GetTickCount () - start_time;
			
				if (wr != WAIT_TIMEOUT)
					data = dequeue_job (&mono_delegate_section, &async_call_queue);
			}
			while (!data && timeout > 0);
		}

		if (!data) {
			workers = (int) InterlockedCompareExchange (&mono_worker_threads, 0, -1); 
			min = (int) InterlockedCompareExchange (&mono_min_worker_threads, 0, -1); 
	
			while (!data && workers <= min) {
				WaitForSingleObjectEx (job_added, INFINITE, TRUE);
				if (THREAD_WANTS_A_BREAK (thread))
					mono_thread_interruption_checkpoint ();
			
				data = dequeue_job (&mono_delegate_section, &async_call_queue);
				workers = (int) InterlockedCompareExchange (&mono_worker_threads, 0, -1); 
				min = (int) InterlockedCompareExchange (&mono_min_worker_threads, 0, -1); 
			}
		}
	
		if (!data) {
			InterlockedDecrement (&mono_worker_threads);
			return;
		}
		
		InterlockedIncrement (&busy_worker_threads);
	}

	g_assert_not_reached ();
}

void
ves_icall_System_Threading_ThreadPool_GetAvailableThreads (gint *workerThreads, gint *completionPortThreads)
{
	gint busy;

	MONO_ARCH_SAVE_REGS;

	busy = (gint) InterlockedCompareExchange (&busy_worker_threads, 0, -1);
	*workerThreads = mono_max_worker_threads - busy;
	*completionPortThreads = 0;
}

void
ves_icall_System_Threading_ThreadPool_GetMaxThreads (gint *workerThreads, gint *completionPortThreads)
{
	MONO_ARCH_SAVE_REGS;

	*workerThreads = mono_max_worker_threads;
	*completionPortThreads = 0;
}

void
ves_icall_System_Threading_ThreadPool_GetMinThreads (gint *workerThreads, gint *completionPortThreads)
{
	gint workers;

	MONO_ARCH_SAVE_REGS;

	workers = (gint) InterlockedCompareExchange (&mono_min_worker_threads, 0, -1);
	*workerThreads = workers;
	*completionPortThreads = 0;
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMinThreads (gint workerThreads, gint completionPortThreads)
{
	MONO_ARCH_SAVE_REGS;

	if (workerThreads < 0 || workerThreads > mono_max_worker_threads)
		return FALSE;
	InterlockedExchange (&mono_min_worker_threads, workerThreads);
	/* FIXME: should actually start the idle threads if needed */
	return TRUE;
}

