/*
 * threadpool.c: global thread pool
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#include <config.h>
#include <glib.h>

#define THREADS_PER_CPU	10 /* 8 + THREADS_PER_CPU * number of CPUs = max threads */
#define THREAD_EXIT_TIMEOUT 1000
#define INITIAL_QUEUE_LENGTH 128

#include <mono/metadata/domain-internals.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/mono-perfcounters.h>
#include <mono/metadata/socket-io.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/gc-internal.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-proclib.h>
#include <errno.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#include <sys/types.h>
#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <string.h>
#ifdef HAVE_SYS_SOCKET_H
#include <sys/socket.h>
#endif
#include <mono/utils/mono-poll.h>
#ifdef HAVE_EPOLL
#include <sys/epoll.h>
#endif

#ifndef DISABLE_SOCKETS
#include "mono/io-layer/socket-wrappers.h"
#endif

#include "threadpool.h"

#define THREAD_WANTS_A_BREAK(t) ((t->state & (ThreadState_StopRequested | \
						ThreadState_SuspendRequested)) != 0)

#undef EPOLL_DEBUG
//
/* map of CounterSample.cs */
struct _MonoCounterSample {
	gint64 rawValue;
	gint64 baseValue;
	gint64 counterFrequency;
	gint64 systemFrequency;
	gint64 timeStamp;
	gint64 timeStamp100nSec;
	gint64 counterTimeStamp;
	int counterType;
};

/* mono_thread_pool_init called */
static int tp_inited;

static int pending_io_items;

typedef struct {
	CRITICAL_SECTION io_lock; /* access to sock_to_state */
	int inited;
	int pipe [2];
	MonoGHashTable *sock_to_state;

	HANDLE new_sem; /* access to newpfd and write side of the pipe */
	mono_pollfd *newpfd;
	gboolean epoll_disabled;
#ifdef HAVE_EPOLL
	int epollfd;
#endif
} SocketIOData;

static SocketIOData socket_io_data;

/* Keep in sync with the System.MonoAsyncCall class which provides GC tracking */
typedef struct {
	MonoObject         object;
	MonoMethodMessage *msg;
	MonoMethod        *cb_method;
	MonoDelegate      *cb_target;
	MonoObject        *state;
	MonoObject        *res;
	MonoArray         *out_args;
	/* This is a HANDLE, we use guint64 so the managed object layout remains constant */
	guint64           wait_event;
} ASyncCall;

typedef struct {
	CRITICAL_SECTION lock;
	MonoArray *array;
	int first_elem;
	int next_elem;

	/**/
	GQueue *idle_threads;
	int idle_started; /* Have we started the idle threads? Interlocked */
	/* min, max, n and busy -> Interlocked */
	int min_threads;
	int max_threads;
	int nthreads;
	int busy_threads;

	void (*async_invoke) (gpointer data);
	void *pc_nitems; /* Performance counter for total number of items in added */
	/* We don't need the rate here since we can compute the different ourselves */
	/* void *perfc_rate; */
	MonoCounterSample last_sample;

} ThreadPool;

static ThreadPool async_tp;
static ThreadPool async_io_tp;

typedef struct {
	HANDLE wait_handle;
	gpointer data;
	gint timeout;
	gboolean die;
} IdleThreadData;
 
static void async_invoke_thread (gpointer data);
static void mono_async_invoke (MonoAsyncResult *ares);
static void threadpool_free_queue (ThreadPool *tp);
static void threadpool_append_job (ThreadPool *tp, MonoObject *ar);
static void *threadpool_queue_idle_thread (ThreadPool *tp, IdleThreadData *it);
static void threadpool_init (ThreadPool *tp, int min_threads, int max_threads, void (*async_invoke) (gpointer));
static void threadpool_start_idle_threads (ThreadPool *tp);
static void threadpool_kill_idle_threads (ThreadPool *tp);

static MonoClass *async_call_klass;
static MonoClass *socket_async_call_klass;
static MonoClass *process_async_call_klass;

#define INIT_POLLFD(a, b, c) {(a)->fd = b; (a)->events = c; (a)->revents = 0;}
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
	AIO_OP_LAST
};

#ifdef DISABLE_SOCKETS
#define socket_io_cleanup(x)
#else
static void
socket_io_cleanup (SocketIOData *data)
{
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
	mono_g_hash_table_destroy (data->sock_to_state);
	data->sock_to_state = NULL;
	EnterCriticalSection (&async_io_tp.lock);
	threadpool_free_queue (&async_io_tp);
	threadpool_kill_idle_threads (&async_io_tp);
	LeaveCriticalSection (&async_io_tp.lock);
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
	case AIO_OP_RECV_JUST_CALLBACK:
	case AIO_OP_RECEIVEFROM:
	case AIO_OP_READPIPE:
		return MONO_POLLIN;
	case AIO_OP_SEND:
	case AIO_OP_SEND_JUST_CALLBACK:
	case AIO_OP_SENDTO:
	case AIO_OP_CONNECT:
		return MONO_POLLOUT;
	default: /* Should never happen */
		g_print ("get_event_from_state: unknown value in switch!!!\n");
		return 0;
	}
}

static int
get_events_from_list (MonoMList *list)
{
	MonoSocketAsyncResult *state;
	int events = 0;

	while (list && (state = (MonoSocketAsyncResult *)mono_mlist_get_data (list))) {
		events |= get_event_from_state (state);
		list = mono_mlist_next (list);
	}

	return events;
}

#define ICALL_RECV(x)	ves_icall_System_Net_Sockets_Socket_Receive_internal (\
				(SOCKET)(gssize)x->handle, x->buffer, x->offset, x->size,\
				 x->socket_flags, &x->error);

#define ICALL_SEND(x)	ves_icall_System_Net_Sockets_Socket_Send_internal (\
				(SOCKET)(gssize)x->handle, x->buffer, x->offset, x->size,\
				 x->socket_flags, &x->error);

#endif /* !DISABLE_SOCKETS */

static void
threadpool_jobs_inc (MonoObject *obj)
{
	if (obj)
		InterlockedIncrement (&obj->vtable->domain->threadpool_jobs);
}

static gboolean
threadpool_jobs_dec (MonoObject *obj)
{
	MonoDomain *domain = obj->vtable->domain;
	int remaining_jobs = InterlockedDecrement (&domain->threadpool_jobs);
	if (remaining_jobs == 0 && domain->cleanup_semaphore) {
		ReleaseSemaphore (domain->cleanup_semaphore, 1, NULL);
		return TRUE;
	}
	return FALSE;
}

#ifndef DISABLE_SOCKETS
static void
async_invoke_io_thread (gpointer data)
{
	MonoDomain *domain;
	MonoInternalThread *thread;
	const gchar *version;
	IdleThreadData idle_data = {0};
  
	idle_data.timeout = INFINITE;
	idle_data.wait_handle = CreateEvent (NULL, FALSE, FALSE, NULL);

	thread = mono_thread_internal_current ();

	version = mono_get_runtime_info ()->framework_version;
	for (;;) {
		MonoSocketAsyncResult *state;
		MonoAsyncResult *ar;

		state = (MonoSocketAsyncResult *) data;
		if (state) {
			InterlockedDecrement (&pending_io_items);
			ar = state->ares;
			switch (state->operation) {
			case AIO_OP_RECEIVE:
				state->total = ICALL_RECV (state);
				break;
			case AIO_OP_SEND:
				state->total = ICALL_SEND (state);
				break;
			}

			/* worker threads invokes methods in different domains,
			 * so we need to set the right domain here */
			domain = ((MonoObject *)ar)->vtable->domain;

			g_assert (domain);

			if (domain->state == MONO_APPDOMAIN_UNLOADED || domain->state == MONO_APPDOMAIN_UNLOADING) {
				threadpool_jobs_dec ((MonoObject *)ar);
				data = NULL;
			} else {
				mono_thread_push_appdomain_ref (domain);
				if (threadpool_jobs_dec ((MonoObject *)ar)) {
					data = NULL;
					mono_thread_pop_appdomain_ref ();
					continue;
				}
				if (mono_domain_set (domain, FALSE)) {
					ASyncCall *ac;

					mono_async_invoke (ar);
					ac = (ASyncCall *) ar->object_data;
					/*
					if (ac->msg->exc != NULL)
						mono_unhandled_exception (ac->msg->exc);
					*/
					mono_domain_set (mono_get_root_domain (), TRUE);
				}
				mono_thread_pop_appdomain_ref ();
				InterlockedDecrement (&async_io_tp.busy_threads);
				/* If the callee changes the background status, set it back to TRUE */
				if (*version != '1' && !mono_thread_test_state (thread , ThreadState_Background))
					ves_icall_System_Threading_Thread_SetState (thread, ThreadState_Background);
			}
		}

		data = threadpool_queue_idle_thread (&async_io_tp, &idle_data);
		while (!idle_data.die && !data) {
			guint32 wr;
			wr = WaitForSingleObjectEx (idle_data.wait_handle, idle_data.timeout, TRUE);
			if (THREAD_WANTS_A_BREAK (thread))
				mono_thread_interruption_checkpoint ();
		
			if (wr != WAIT_TIMEOUT && wr != WAIT_IO_COMPLETION) {
				data = idle_data.data;
				idle_data.data = NULL;
				break; /* We have to exit */
			}
		}

		if (!data) {
			InterlockedDecrement (&async_io_tp.nthreads);
			CloseHandle (idle_data.wait_handle);
			idle_data.wait_handle = NULL;
			return;
		}
		
		InterlockedIncrement (&async_io_tp.busy_threads);
	}

	g_assert_not_reached ();
}

static MonoMList *
process_io_event (MonoMList *list, int event)
{
	MonoSocketAsyncResult *state;
	MonoMList *oldlist;

	oldlist = list;
	state = NULL;
	while (list) {
		state = (MonoSocketAsyncResult *) mono_mlist_get_data (list);
		if (get_event_from_state (state) == event)
			break;
		
		list = mono_mlist_next (list);
	}

	if (list != NULL) {
		oldlist = mono_mlist_remove_item (oldlist, list);
#ifdef EPOLL_DEBUG
		g_print ("Dispatching event %d on socket %p\n", event, state->handle);
#endif
		InterlockedIncrement (&pending_io_items);
		threadpool_append_job (&async_io_tp, (MonoObject *) state);
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

		ret = mono_poll (pfd, 1, 0);
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
	MonoInternalThread *thread;

	thread = mono_thread_internal_current ();

	allocated = INITIAL_POLLFD_SIZE;
	pfds = g_new0 (mono_pollfd, allocated);
	INIT_POLLFD (pfds, data->pipe [0], MONO_POLLIN);
	for (i = 1; i < allocated; i++)
		INIT_POLLFD (&pfds [i], -1, 0);

	while (1) {
		int nsock = 0;
		mono_pollfd *pfd;
		char one [1];
		MonoMList *list;

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
			LeaveCriticalSection (&data->io_lock);
			return; /* cleanup called */
		}

		for (i = 1; i < maxfd && nsock > 0; i++) {
			pfd = &pfds [i];
			if (pfd->fd == -1 || pfd->revents == 0)
				continue;

			nsock--;
			list = mono_g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (pfd->fd));
			if (list != NULL && (pfd->revents & (MONO_POLLIN | POLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLIN);
			}

			if (list != NULL && (pfd->revents & (MONO_POLLOUT | POLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLOUT);
			}

			if (list != NULL) {
				mono_g_hash_table_replace (data->sock_to_state, GINT_TO_POINTER (pfd->fd), list);
				pfd->events = get_events_from_list (list);
			} else {
				mono_g_hash_table_remove (data->sock_to_state, GINT_TO_POINTER (pfd->fd));
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
	MonoInternalThread *thread;
	struct epoll_event *events, *evt;
	const int nevents = 512;
	int ready = 0, i;

	data = p;
	epollfd = data->epollfd;
	thread = mono_thread_internal_current ();
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
			MonoMList *list;

			evt = &events [i];
			fd = evt->data.fd;
			list = mono_g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (fd));
#ifdef EPOLL_DEBUG
			g_print ("Event %d on %d list length: %d\n", evt->events, fd, mono_mlist_length (list));
#endif
			if (list != NULL && (evt->events & (EPOLLIN | EPOLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLIN);
			}

			if (list != NULL && (evt->events & (EPOLLOUT | EPOLL_ERRORS)) != 0) {
				list = process_io_event (list, MONO_POLLOUT);
			}

			if (list != NULL) {
				mono_g_hash_table_replace (data->sock_to_state, GINT_TO_POINTER (fd), list);
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
				mono_g_hash_table_remove (data->sock_to_state, GINT_TO_POINTER (fd));
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

/*
 * select/poll wake up when a socket is closed, but epoll just removes
 * the socket from its internal list without notification.
 */
void
mono_thread_pool_remove_socket (int sock)
{
	MonoMList *list, *next;
	MonoSocketAsyncResult *state;

	if (socket_io_data.inited == FALSE)
		return;

	EnterCriticalSection (&socket_io_data.io_lock);
	list = mono_g_hash_table_lookup (socket_io_data.sock_to_state, GINT_TO_POINTER (sock));
	if (list) {
		mono_g_hash_table_remove (socket_io_data.sock_to_state, GINT_TO_POINTER (sock));
	}
	LeaveCriticalSection (&socket_io_data.io_lock);
	
	while (list) {
		state = (MonoSocketAsyncResult *) mono_mlist_get_data (list);
		if (state->operation == AIO_OP_RECEIVE)
			state->operation = AIO_OP_RECV_JUST_CALLBACK;
		else if (state->operation == AIO_OP_SEND)
			state->operation = AIO_OP_SEND_JUST_CALLBACK;

		next = mono_mlist_remove_item (list, list);
		list = process_io_event (list, MONO_POLLIN);
		if (list)
			process_io_event (list, MONO_POLLOUT);

		list = next;
	}
}

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
	data->sock_to_state = mono_g_hash_table_new_type (g_direct_hash, g_direct_equal, MONO_HASH_VALUE_GC);
	mono_thread_create_internal (mono_get_root_domain (), threadpool_start_idle_threads, &async_io_tp, TRUE);

	if (data->epoll_disabled) {
		data->new_sem = CreateSemaphore (NULL, 1, 1, NULL);
		g_assert (data->new_sem != NULL);
	}
	if (data->epoll_disabled) {
		mono_thread_create_internal (mono_get_root_domain (), socket_io_poll_main, data, TRUE);
	}
#ifdef HAVE_EPOLL
	else {
		mono_thread_create_internal (mono_get_root_domain (), socket_io_epoll_main, data, TRUE);
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
	MonoMList *list;
	SocketIOData *data = &socket_io_data;
	int w;

#if defined(PLATFORM_MACOSX) || defined(PLATFORM_BSD) || defined(PLATFORM_WIN32) || defined(PLATFORM_SOLARIS)
	/* select() for connect() does not work well on the Mac. Bug #75436. */
	/* Bug #77637 for the BSD 6 case */
	/* Bug #78888 for the Windows case */
	if (state->operation == AIO_OP_CONNECT && state->blocking == TRUE) {
		threadpool_append_job (&async_io_tp, (MonoObject *) state);
		return;
	}
#endif
	WaitForSingleObject (data->new_sem, INFINITE);
	if (data->newpfd == NULL)
		data->newpfd = g_new0 (mono_pollfd, 1);

	EnterCriticalSection (&data->io_lock);
	/* FIXME: 64 bit issue: handle can be a pointer on windows? */
	list = mono_g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (state->handle));
	if (list == NULL) {
		list = mono_mlist_alloc ((MonoObject*)state);
	} else {
		list = mono_mlist_append (list, (MonoObject*)state);
	}

	events = get_events_from_list (list);
	INIT_POLLFD (data->newpfd, GPOINTER_TO_INT (state->handle), events);
	mono_g_hash_table_replace (data->sock_to_state, GINT_TO_POINTER (state->handle), list);
	LeaveCriticalSection (&data->io_lock);
	*msg = (char) state->operation;
#ifndef PLATFORM_WIN32
	w = write (data->pipe [1], msg, 1);
	w = w;
#else
	send ((SOCKET) data->pipe [1], msg, 1, 0);
#endif
}

#ifdef HAVE_EPOLL
static gboolean
socket_io_add_epoll (MonoSocketAsyncResult *state)
{
	MonoMList *list;
	SocketIOData *data = &socket_io_data;
	struct epoll_event event;
	int epoll_op, ievt;
	int fd;

	memset (&event, 0, sizeof (struct epoll_event));
	fd = GPOINTER_TO_INT (state->handle);
	EnterCriticalSection (&data->io_lock);
	list = mono_g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (fd));
	if (list == NULL) {
		list = mono_mlist_alloc ((MonoObject*)state);
		epoll_op = EPOLL_CTL_ADD;
	} else {
		list = mono_mlist_append (list, (MonoObject*)state);
		epoll_op = EPOLL_CTL_MOD;
	}

	ievt = get_events_from_list (list);
	if ((ievt & MONO_POLLIN) != 0)
		event.events |= EPOLLIN;
	if ((ievt & MONO_POLLOUT) != 0)
		event.events |= EPOLLOUT;

	mono_g_hash_table_replace (data->sock_to_state, state->handle, list);
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
	MONO_OBJECT_SETREF (state, ares, ares);
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

	if (socket_async_call_klass == NULL) {
		klass = target->vtable->klass;
		/* Check if it's SocketAsyncCall in System.Net.Sockets
		 * FIXME: check the assembly is signed correctly for extra care
		 */
		if (klass->name [0] == 'S' && strcmp (klass->name, "SocketAsyncCall") == 0 
				&& strcmp (mono_image_get_name (klass->image), "System") == 0
				&& klass->nested_in && strcmp (klass->nested_in->name, "Socket") == 0)
			socket_async_call_klass = klass;
	}

	if (process_async_call_klass == NULL) {
		klass = target->vtable->klass;
		/* Check if it's AsyncReadHandler in System.Diagnostics.Process
		 * FIXME: check the assembly is signed correctly for extra care
		 */
		if (klass->name [0] == 'A' && strcmp (klass->name, "AsyncReadHandler") == 0 
				&& strcmp (mono_image_get_name (klass->image), "System") == 0
				&& klass->nested_in && strcmp (klass->nested_in->name, "Process") == 0)
			process_async_call_klass = klass;
	}
	/* return both when socket_async_call_klass has not been seen yet and when
	 * the object is not an instance of the class.
	 */
	if (target->vtable->klass != socket_async_call_klass && target->vtable->klass != process_async_call_klass)
		return FALSE;

	op = sock_res->operation;
	if (op < AIO_OP_FIRST || op >= AIO_OP_LAST)
		return FALSE;

	return TRUE;
}
#endif /* !DISABLE_SOCKETS */

static void
mono_async_invoke (MonoAsyncResult *ares)
{
	ASyncCall *ac = (ASyncCall *)ares->object_data;
	MonoObject *res, *exc = NULL;
	MonoArray *out_args = NULL;
	HANDLE wait_event = NULL;

	ares->completed = 1;

	if (ares->execution_context) {
		/* use captured ExecutionContext (if available) */
		MONO_OBJECT_SETREF (ares, original_context, mono_thread_get_execution_context ());
		mono_thread_set_execution_context (ares->execution_context);
	} else {
		ares->original_context = NULL;
	}

	ac->msg->exc = NULL;
	res = mono_message_invoke (ares->async_delegate, ac->msg, &exc, &out_args);
	MONO_OBJECT_SETREF (ac, res, res);
	MONO_OBJECT_SETREF (ac, msg->exc, exc);
	MONO_OBJECT_SETREF (ac, out_args, out_args);

	/* call async callback if cb_method != null*/
	if (ac->cb_method) {
		MonoObject *exc = NULL;
		void *pa = &ares;
		mono_runtime_invoke (ac->cb_method, ac->cb_target, pa, &exc);
		/* 'exc' will be the previous ac->msg->exc if not NULL and not
		 * catched. If catched, this will be set to NULL and the
		 * exception will not be printed. */
		MONO_OBJECT_SETREF (ac->msg, exc, exc);
	}

	/* restore original thread execution context if flow isn't suppressed, i.e. non null */
	if (ares->original_context) {
		mono_thread_set_execution_context (ares->original_context);
		ares->original_context = NULL;
	}

	/* notify listeners */
	mono_monitor_enter ((MonoObject *) ares);
	if (ares->handle != NULL) {
		ac->wait_event = (gsize) mono_wait_handle_get_handle ((MonoWaitHandle *) ares->handle);
		wait_event = (HANDLE)(gsize) ac->wait_event;
	}
	mono_monitor_exit ((MonoObject *) ares);
	if (wait_event != NULL)
		SetEvent (wait_event);
}

static void
threadpool_start_idle_threads (ThreadPool *tp)
{
	int needed;
	int existing;

	needed = (int) InterlockedCompareExchange (&tp->min_threads, 0, -1); 
	do {
		existing = (int) InterlockedCompareExchange (&tp->nthreads, 0, -1); 
		if (existing >= needed)
			break;
		InterlockedIncrement (&tp->nthreads);
		mono_thread_create_internal (mono_get_root_domain (), tp->async_invoke, NULL, TRUE);
		SleepEx (250, TRUE);
	} while (1);
}

static void
threadpool_init (ThreadPool *tp, int min_threads, int max_threads, void (*async_invoke) (gpointer))
{
	memset (tp, 0, sizeof (ThreadPool));
	InitializeCriticalSection (&tp->lock);
	tp->min_threads = min_threads;
	tp->max_threads = max_threads;
	tp->async_invoke = async_invoke;
	tp->idle_threads = g_queue_new ();
}

static void *
init_perf_counter (const char *category, const char *counter)
{
	MonoString *category_str;
	MonoString *counter_str;
	MonoString *machine;
	MonoDomain *root;
	MonoBoolean custom;
	int type;

	if (category == NULL || counter == NULL)
		return NULL;
	root = mono_get_root_domain ();
	category_str = mono_string_new (root, category);
	counter_str = mono_string_new (root, counter);
	machine = mono_string_new (root, ".");
	return mono_perfcounter_get_impl (category_str, counter_str, NULL, machine, &type, &custom);
}

void
mono_thread_pool_init ()
{
	int threads_per_cpu = THREADS_PER_CPU;
	int cpu_count;
	int n;

	if ((int) InterlockedCompareExchange (&tp_inited, 1, 0) == 1)
		return;

	MONO_GC_REGISTER_ROOT (socket_io_data.sock_to_state);
	InitializeCriticalSection (&socket_io_data.io_lock);
	if (g_getenv ("MONO_THREADS_PER_CPU") != NULL) {
		threads_per_cpu = atoi (g_getenv ("MONO_THREADS_PER_CPU"));
		if (threads_per_cpu < THREADS_PER_CPU)
			threads_per_cpu = THREADS_PER_CPU;
	}

	cpu_count = mono_cpu_count ();
	n = 8 + 2 * cpu_count; /* 8 is minFreeThreads for ASP.NET */
	threadpool_init (&async_tp, n, n + threads_per_cpu * cpu_count, async_invoke_thread);
#ifndef DISABLE_SOCKET
	threadpool_init (&async_io_tp, 2 * cpu_count, 8 * cpu_count, async_invoke_io_thread);
#endif

	async_call_klass = mono_class_from_name (mono_defaults.corlib, "System", "MonoAsyncCall");
	g_assert (async_call_klass);

	async_tp.pc_nitems = init_perf_counter ("Mono Threadpool", "Work Items Added");
	g_assert (async_tp.pc_nitems);
	mono_perfcounter_get_sample (async_tp.pc_nitems, FALSE, &async_tp.last_sample);

	async_io_tp.pc_nitems = init_perf_counter ("Mono Threadpool", "IO Work Items Added");
	g_assert (async_io_tp.pc_nitems);
	mono_perfcounter_get_sample (async_io_tp.pc_nitems, FALSE, &async_io_tp.last_sample);
}

MonoAsyncResult *
mono_thread_pool_add (MonoObject *target, MonoMethodMessage *msg, MonoDelegate *async_callback,
		      MonoObject *state)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	ASyncCall *ac;

	ac = (ASyncCall*)mono_object_new (mono_domain_get (), async_call_klass);
	MONO_OBJECT_SETREF (ac, msg, msg);
	MONO_OBJECT_SETREF (ac, state, state);

	if (async_callback) {
		ac->cb_method = mono_get_delegate_invoke (((MonoObject *)async_callback)->vtable->klass);
		MONO_OBJECT_SETREF (ac, cb_target, async_callback);
	}

	ares = mono_async_result_new (domain, NULL, ac->state, NULL, (MonoObject*)ac);
	MONO_OBJECT_SETREF (ares, async_delegate, target);

#ifndef DISABLE_SOCKETS
	if (socket_io_filter (target, state)) {
		socket_io_add (ares, (MonoSocketAsyncResult *) state);
		return ares;
	}
#endif
	if (InterlockedCompareExchange (&async_tp.idle_started, 1, 0) == 0)
		mono_thread_create_internal (mono_get_root_domain (), threadpool_start_idle_threads, &async_tp, TRUE);
	
	threadpool_append_job (&async_tp, (MonoObject *) ares);
	return ares;
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
	ac = (ASyncCall *)ares->object_data;

	g_assert (ac != NULL);

	/* wait until we are really finished */
	if (!ares->completed) {
		if (ares->handle == NULL) {
			ac->wait_event = (gsize)CreateEvent (NULL, TRUE, FALSE, NULL);
			g_assert(ac->wait_event != 0);
			MONO_OBJECT_SETREF (ares, handle, (MonoObject *) mono_wait_handle_new (mono_object_domain (ares), (gpointer)(gsize)ac->wait_event));
		}
		mono_monitor_exit ((MonoObject *) ares);
		WaitForSingleObjectEx ((gpointer)(gsize)ac->wait_event, INFINITE, TRUE);
	} else {
		mono_monitor_exit ((MonoObject *) ares);
	}

	*exc = ac->msg->exc; /* FIXME: GC add write barrier */
	*out_args = ac->out_args;

	return ac->res;
}

static void
threadpool_kill_idle_threads (ThreadPool *tp)
{
	IdleThreadData *it;

	if (!tp || !tp->idle_threads)
		return;

	while ((it = g_queue_pop_head (tp->idle_threads)) != NULL) {
		it->data = NULL;
		it->die = TRUE;
		SetEvent (it->wait_handle);
	}
	g_queue_free (tp->idle_threads);
	tp->idle_threads = NULL;
}

void
mono_thread_pool_cleanup (void)
{
	EnterCriticalSection (&async_tp.lock);
	threadpool_free_queue (&async_tp);
	threadpool_kill_idle_threads (&async_tp);
	LeaveCriticalSection (&async_tp.lock);
	socket_io_cleanup (&socket_io_data); /* Empty when DISABLE_SOCKETS is defined */
	/* Do we want/need these?
	DeleteCriticalSection (&async_tp.lock);
	DeleteCriticalSection (&async_tp.table_lock);
	DeleteCriticalSection (&socket_io_data.io_lock);
	*/
}

static void
null_array (MonoArray *a, int first, int last)
{
	/* We must null the old array because it might
	   contain cross-appdomain references, which
	   will crash the GC when the domains are
	   unloaded. */
	memset (mono_array_addr (a, MonoObject*, first), 0, sizeof (MonoObject*) * (last - first));
}

/* Caller must enter &tp->lock */
static MonoObject*
dequeue_job_nolock (ThreadPool *tp)
{
	MonoObject *ar;
	int count;

	if (!tp->array || tp->first_elem == tp->next_elem)
		return NULL;
	ar = mono_array_get (tp->array, MonoObject*, tp->first_elem);
	mono_array_set (tp->array, MonoObject*, tp->first_elem, NULL);
	tp->first_elem++;
	count = tp->next_elem - tp->first_elem;
	/* reduce the size of the array if it's mostly empty */
	if (mono_array_length (tp->array) > INITIAL_QUEUE_LENGTH && count < (mono_array_length (tp->array) / 3)) {
		MonoArray *newa = mono_array_new_cached (mono_get_root_domain (), mono_defaults.object_class, mono_array_length (tp->array) / 2);
		mono_array_memcpy_refs (newa, 0, tp->array, tp->first_elem, count);
		null_array (tp->array, tp->first_elem, tp->next_elem);
		tp->array = newa;
		tp->first_elem = 0;
		tp->next_elem = count;
	}
	return ar;
}

/* Call after entering &tp->lock */
static int
signal_idle_threads (ThreadPool *tp)
{
	IdleThreadData *it;
	int result = 0;
	int njobs;

	njobs = tp->next_elem - tp->first_elem;
	while (njobs > 0 && (it = g_queue_pop_head (tp->idle_threads)) != NULL) {
		it->data = dequeue_job_nolock (tp);
		if (it->data == NULL)
			break; /* Should never happen */
		result++;
		njobs--;
		it->timeout = INFINITE;
		SetEvent (it->wait_handle);
	}
	return njobs;
}

/* Call after entering &tp->lock */
static gboolean
threadpool_start_thread (ThreadPool *tp, gpointer arg)
{
	gint max;
	gint n;

	max = (gint) InterlockedCompareExchange (&tp->max_threads, 0, -1);
	n = (gint) InterlockedCompareExchange (&tp->nthreads, 0, -1);
	if (max <= n)
		return FALSE;
	InterlockedIncrement (&tp->nthreads);
	mono_thread_create_internal (mono_get_root_domain (), tp->async_invoke, arg, TRUE);
	return TRUE;
}

/*
static const char *
get_queue_name (ThreadPool *tp)
{
	if (tp == &async_tp)
		return "TP";
	if (tp == &async_io_tp)
		return "IO";
	return "(Unknown)";
}
*/

static gpointer
threadpool_queue_idle_thread (ThreadPool *tp, IdleThreadData *it)
{
	/*
	MonoCounterSample sample;
	float rate;
	*/
	gpointer result = NULL;
	CRITICAL_SECTION *cs = &tp->lock;

	EnterCriticalSection (cs);
	/*
	if (mono_100ns_ticks () - tp->last_sample.timeStamp > 10000 * 1000) {
		float elapsed_ticks;
		mono_perfcounter_get_sample (tp->pc_nitems, FALSE, &sample);

		elapsed_ticks = (float) (sample.timeStamp - tp->last_sample.timeStamp);
		rate = ((float) (sample.rawValue - tp->last_sample.rawValue)) / elapsed_ticks * 10000000;
		printf ("Queue: %s NThreads: %d Rate: %.2f Total items: %lld Time(ms): %.2f\n", get_queue_name (tp),
						InterlockedCompareExchange (&tp->nthreads, 0, -1), rate,
						sample.rawValue - tp->last_sample.rawValue, elapsed_ticks / 10000);
		memcpy (&tp->last_sample, &sample, sizeof (sample));
	}
	*/

	it->data = result = dequeue_job_nolock (tp);
	if (result != NULL) {
		signal_idle_threads (tp);
	} else {
		int min, n;
		min = (gint) InterlockedCompareExchange (&tp->min_threads, 0, -1);
		n = (gint) InterlockedCompareExchange (&tp->nthreads, 0, -1);
		if (n <= min) {
			g_queue_push_tail (tp->idle_threads, it);
		} else {
			/* TODO: figure out when threads should be told to die */
			/* it->die = TRUE; */
			g_queue_push_tail (tp->idle_threads, it);
		}
	}
	LeaveCriticalSection (cs);
	return result;
}

static void
threadpool_append_job (ThreadPool *tp, MonoObject *ar)
{
	CRITICAL_SECTION *cs;

	cs = &tp->lock;
	threadpool_jobs_inc (ar); 
	EnterCriticalSection (cs);
	if (ar->vtable->domain->state == MONO_APPDOMAIN_UNLOADING ||
			ar->vtable->domain->state == MONO_APPDOMAIN_UNLOADED) {
		LeaveCriticalSection (cs);
		return;
	}

	mono_perfcounter_update_value (tp->pc_nitems, TRUE, 1);
	if (tp->array && (tp->next_elem < mono_array_length (tp->array))) {
		mono_array_setref (tp->array, tp->next_elem, ar);
		tp->next_elem++;
		if (signal_idle_threads (tp) > 0 && threadpool_start_thread (tp, ar)) {
			tp->next_elem--;
			mono_array_setref (tp->array, tp->next_elem, NULL);
		}
		LeaveCriticalSection (cs);
		return;
	}

	if (!tp->array) {
		MONO_GC_REGISTER_ROOT (tp->array);
		tp->array = mono_array_new_cached (mono_get_root_domain (), mono_defaults.object_class, INITIAL_QUEUE_LENGTH);
	} else {
		int count = tp->next_elem - tp->first_elem;
		/* slide the array or create a larger one if it's full */
		if (tp->first_elem) {
			mono_array_memcpy_refs (tp->array, 0, tp->array, tp->first_elem, count);
			null_array (tp->array, count, tp->next_elem);
		} else {
			MonoArray *newa = mono_array_new_cached (mono_get_root_domain (), mono_defaults.object_class, mono_array_length (tp->array) * 2);
			mono_array_memcpy_refs (newa, 0, tp->array, tp->first_elem, count);
			null_array (tp->array, count, tp->next_elem);
			tp->array = newa;
		}
		tp->first_elem = 0;
		tp->next_elem = count;
	}
	mono_array_setref (tp->array, tp->next_elem, ar);
	tp->next_elem++;
	if (signal_idle_threads (tp) > 0 && threadpool_start_thread (tp, ar)) {
		tp->next_elem--;
		mono_array_setref (tp->array, tp->next_elem, NULL);
	}
	LeaveCriticalSection (cs);
}


static void
threadpool_clear_queue (ThreadPool *tp, MonoDomain *domain)
{
	int i, count = 0;
	EnterCriticalSection (&tp->lock);
	/*remove*/
	for (i = tp->first_elem; i < tp->next_elem; ++i) {
		MonoObject *obj = mono_array_get (tp->array, MonoObject*, i);
		if (obj->vtable->domain == domain) {
			mono_array_set (tp->array, MonoObject*, i, NULL);
			InterlockedDecrement (&domain->threadpool_jobs);
			++count;
		}
	}
	/*compact*/
	if (count) {
		int idx = 0;
		for (i = tp->first_elem; i < tp->next_elem; ++i) {
			MonoObject *obj = mono_array_get (tp->array, MonoObject*, i);
			if (obj)
				mono_array_set (tp->array, MonoObject*, idx++, obj);
		}
		tp->first_elem = 0;
		tp->next_elem = count;
	}
	LeaveCriticalSection (&tp->lock);
}

/*
 * Clean up the threadpool of all domain jobs.
 * Can only be called as part of the domain unloading process as
 * it will wait for all jobs to be visible to the interruption code. 
 */
gboolean
mono_thread_pool_remove_domain_jobs (MonoDomain *domain, int timeout)
{
	HANDLE sem_handle;
	int result = TRUE;
	guint32 start_time = 0;

	g_assert (domain->state == MONO_APPDOMAIN_UNLOADING);

	threadpool_clear_queue (&async_tp, domain);
	threadpool_clear_queue (&async_io_tp, domain);

	/*
	 * There might be some threads out that could be about to execute stuff from the given domain.
	 * We avoid that by setting up a semaphore to be pulsed by the thread that reaches zero.
	 */
	sem_handle = CreateSemaphore (NULL, 0, 1, NULL);
	
	domain->cleanup_semaphore = sem_handle;
	/*
	 * The memory barrier here is required to have global ordering between assigning to cleanup_semaphone
	 * and reading threadpool_jobs.
	 * Otherwise this thread could read a stale version of threadpool_jobs and wait forever.
	 */
	mono_memory_write_barrier ();

	if (domain->threadpool_jobs && timeout != -1)
		start_time = mono_msec_ticks ();
	while (domain->threadpool_jobs) {
		WaitForSingleObject (sem_handle, timeout);
		if (timeout != -1 && (mono_msec_ticks () - start_time) > timeout) {
			result = FALSE;
			break;
		}
	}

	domain->cleanup_semaphore = NULL;
	CloseHandle (sem_handle);
	return result;
}

static void
threadpool_free_queue (ThreadPool *tp)
{
	if (tp->array)
		null_array (tp->array, tp->first_elem, tp->next_elem);
	tp->array = NULL;
	tp->first_elem = tp->next_elem = 0;
}

gboolean
mono_thread_pool_is_queue_array (MonoArray *o)
{
	return o == async_tp.array || o == async_io_tp.array;
}

static void
async_invoke_thread (gpointer data)
{
	MonoDomain *domain;
	MonoInternalThread *thread;
	const gchar *version;
	IdleThreadData idle_data = {0};
  
	idle_data.timeout = INFINITE;
	idle_data.wait_handle = CreateEvent (NULL, FALSE, FALSE, NULL);
 
	thread = mono_thread_internal_current ();
	version = mono_get_runtime_info ()->framework_version;
	for (;;) {
		MonoAsyncResult *ar;

		ar = (MonoAsyncResult *) data;
		if (ar) {
			/* worker threads invokes methods in different domains,
			 * so we need to set the right domain here */
			domain = ((MonoObject *)ar)->vtable->domain;

			g_assert (domain);

			if (domain->state == MONO_APPDOMAIN_UNLOADED || domain->state == MONO_APPDOMAIN_UNLOADING) {
				threadpool_jobs_dec ((MonoObject *)ar);
				data = NULL;
			} else {
				mono_thread_push_appdomain_ref (domain);
				if (threadpool_jobs_dec ((MonoObject *)ar)) {
					data = NULL;
					mono_thread_pop_appdomain_ref ();
					continue;
				}

				if (mono_domain_set (domain, FALSE)) {
					ASyncCall *ac;

					mono_async_invoke (ar);
					ac = (ASyncCall *) ar->object_data;
					/*
					if (ac->msg->exc != NULL)
						mono_unhandled_exception (ac->msg->exc);
					*/
					mono_domain_set (mono_get_root_domain (), TRUE);
				}
				mono_thread_pop_appdomain_ref ();
				InterlockedDecrement (&async_tp.busy_threads);
				/* If the callee changes the background status, set it back to TRUE */
				if (*version != '1' && !mono_thread_test_state (thread , ThreadState_Background))
					ves_icall_System_Threading_Thread_SetState (thread, ThreadState_Background);
			}
		}
		data = threadpool_queue_idle_thread (&async_tp, &idle_data);
		while (!idle_data.die && !data) {
			guint32 wr;
			wr = WaitForSingleObjectEx (idle_data.wait_handle, idle_data.timeout, TRUE);
			if (THREAD_WANTS_A_BREAK (thread))
				mono_thread_interruption_checkpoint ();
		
			if (wr != WAIT_TIMEOUT && wr != WAIT_IO_COMPLETION) {
				data = idle_data.data;
				break; /* We have to exit */
			}
		}
		idle_data.data = NULL;

		if (!data) {
			InterlockedDecrement (&async_tp.nthreads);
			CloseHandle (idle_data.wait_handle);
			idle_data.wait_handle = NULL;
			return;
		}
		
		InterlockedIncrement (&async_tp.busy_threads);
	}

	g_assert_not_reached ();
}

void
ves_icall_System_Threading_ThreadPool_GetAvailableThreads (gint *workerThreads, gint *completionPortThreads)
{
	gint busy, busy_io;

	MONO_ARCH_SAVE_REGS;

	busy = (gint) InterlockedCompareExchange (&async_tp.busy_threads, 0, -1);
	busy_io = (gint) InterlockedCompareExchange (&async_io_tp.busy_threads, 0, -1);
	*workerThreads = async_tp.max_threads - busy;
	*completionPortThreads = async_io_tp.max_threads - busy_io;
}

void
ves_icall_System_Threading_ThreadPool_GetMaxThreads (gint *workerThreads, gint *completionPortThreads)
{
	MONO_ARCH_SAVE_REGS;

	*workerThreads = (gint) InterlockedCompareExchange (&async_tp.max_threads, 0, -1);
	*completionPortThreads = (gint) InterlockedCompareExchange (&async_io_tp.max_threads, 0, -1);
}

void
ves_icall_System_Threading_ThreadPool_GetMinThreads (gint *workerThreads, gint *completionPortThreads)
{
	gint workers, workers_io;

	MONO_ARCH_SAVE_REGS;

	workers = (gint) InterlockedCompareExchange (&async_tp.min_threads, 0, -1);
	workers_io = (gint) InterlockedCompareExchange (&async_io_tp.min_threads, 0, -1);

	*workerThreads = workers;
	*completionPortThreads = workers_io;
}

static void
start_idle_threads (void)
{
	threadpool_start_idle_threads (&async_tp);
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMinThreads (gint workerThreads, gint completionPortThreads)
{
	MONO_ARCH_SAVE_REGS;

	if (workerThreads < 0 || workerThreads > async_tp.max_threads)
		return FALSE;

	if (completionPortThreads < 0 || completionPortThreads > async_io_tp.max_threads)
		return FALSE;

	InterlockedExchange (&async_tp.min_threads, workerThreads);
	InterlockedExchange (&async_io_tp.min_threads, completionPortThreads);
	mono_thread_create_internal (mono_get_root_domain (), start_idle_threads, NULL, TRUE);
	return TRUE;
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMaxThreads (gint workerThreads, gint completionPortThreads)
{
	MONO_ARCH_SAVE_REGS;

	if (workerThreads < async_tp.max_threads)
		return FALSE;

	/* We don't really have the concept of completion ports. Do we care here? */
	if (completionPortThreads < async_io_tp.max_threads)
		return FALSE;

	InterlockedExchange (&async_tp.max_threads, workerThreads);
	InterlockedExchange (&async_io_tp.max_threads, completionPortThreads);
	return TRUE;
}

