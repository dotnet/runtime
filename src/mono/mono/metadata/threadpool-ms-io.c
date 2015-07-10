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

#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/metadata/threadpool-ms.h>
#include <mono/metadata/threadpool-ms-io.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-poll.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-lazy-init.h>

typedef struct {
	gboolean (*init) (gint wakeup_pipe_fd);
	void     (*cleanup) (void);
	void     (*update_add) (gint fd, gint events, gboolean is_new);
	gint     (*event_wait) (void);
	gint     (*event_get_fd_max) (void);
	gint     (*event_get_fd_at) (gint i, gint *events);
	void     (*event_reset_fd_at) (gint i, gint events);
} ThreadPoolIOBackend;

#include "threadpool-ms-io-epoll.c"
#include "threadpool-ms-io-kqueue.c"
#include "threadpool-ms-io-poll.c"

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

typedef struct {
	MonoSocketAsyncResult *sockares;
} ThreadPoolIOUpdate;

typedef struct {
	ThreadPoolIOBackend backend;

	mono_mutex_t lock;

	MonoGHashTable *states;

	ThreadPoolIOUpdate *updates;
	guint updates_size;
	guint updates_capacity;

#if !defined(HOST_WIN32)
	gint wakeup_pipes [2];
#else
	SOCKET wakeup_pipes [2];
#endif
} ThreadPoolIO;

static mono_lazy_init_t io_status = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static gboolean io_selector_running = FALSE;

static ThreadPoolIO* threadpool_io;

static int
get_events_from_sockares (MonoSocketAsyncResult *ares)
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
get_sockares_for_event (MonoMList **list, gint event)
{
	MonoSocketAsyncResult *state = NULL;
	MonoMList *current;

	g_assert (list);

	for (current = *list; current; current = mono_mlist_next (current)) {
		state = (MonoSocketAsyncResult*) mono_mlist_get_data (current);
		if (get_events_from_sockares ((MonoSocketAsyncResult*) state) == event)
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
			events |= get_events_from_sockares (ares);

	return events;
}

static void
selector_thread_wakeup (void)
{
	gchar msg = 'c';
	gint written;

	for (;;) {
#if !defined(HOST_WIN32)
		written = write (threadpool_io->wakeup_pipes [1], &msg, 1);
		if (written == 1)
			break;
		if (written == -1) {
			g_warning ("selector_thread_wakeup: write () failed, error (%d) %s\n", errno, g_strerror (errno));
			break;
		}
#else
		written = send (threadpool_io->wakeup_pipes [1], &msg, 1, 0);
		if (written == 1)
			break;
		if (written == SOCKET_ERROR) {
			g_warning ("selector_thread_wakeup: write () failed, error (%d)\n", WSAGetLastError ());
			break;
		}
#endif
	}
}

static void
selector_thread_wakeup_drain_pipes (void)
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
				g_warning ("selector_thread_wakeup_drain_pipes: read () failed, error (%d) %s\n", errno, g_strerror (errno));
			break;
		}
#else
		received = recv (threadpool_io->wakeup_pipes [0], buffer, sizeof (buffer), 0);
		if (received == 0)
			break;
		if (received == SOCKET_ERROR) {
			if (WSAGetLastError () != WSAEINTR && WSAGetLastError () != WSAEWOULDBLOCK)
				g_warning ("selector_thread_wakeup_drain_pipes: recv () failed, error (%d) %s\n", WSAGetLastError ());
			break;
		}
#endif
	}
}

static void
selector_thread (gpointer data)
{
	io_selector_running = TRUE;

	if (mono_runtime_is_shutting_down ()) {
		io_selector_running = FALSE;
		return;
	}

	for (;;) {
		guint i;
		guint max;
		gint ready = 0;

		mono_mutex_lock (&threadpool_io->lock);

		for (i = 0; i < threadpool_io->updates_size; ++i) {
			ThreadPoolIOUpdate *update;
			MonoMList *list;

			update = &threadpool_io->updates [i];

			g_assert (update->sockares);

			list = mono_g_hash_table_lookup (threadpool_io->states, update->sockares->handle);
			list = mono_mlist_append (list, (MonoObject*) update->sockares);
			mono_g_hash_table_replace (threadpool_io->states, update->sockares->handle, list);

			threadpool_io->backend.update_add (GPOINTER_TO_INT (update->sockares->handle), get_events (list), mono_mlist_next (list) == NULL);
		}
		if (threadpool_io->updates_size > 0) {
			ThreadPoolIOUpdate *updates_old;

			threadpool_io->updates_size = 0;
			threadpool_io->updates_capacity = 128;

			updates_old = threadpool_io->updates;

			threadpool_io->updates = mono_gc_alloc_fixed (sizeof (ThreadPoolIOUpdate) * threadpool_io->updates_capacity, MONO_GC_DESCRIPTOR_NULL);
			g_assert (threadpool_io->updates);

			mono_gc_free_fixed (updates_old);
		}

		mono_mutex_unlock (&threadpool_io->lock);

		mono_gc_set_skip_thread (TRUE);

		ready = threadpool_io->backend.event_wait ();

		mono_gc_set_skip_thread (FALSE);

		if (ready == -1 || mono_runtime_is_shutting_down ())
			break;

		mono_mutex_lock (&threadpool_io->lock);

		max = threadpool_io->backend.event_get_fd_max ();

		for (i = 0; i < max && ready > 0; ++i) {
			gint events;
			gint fd = threadpool_io->backend.event_get_fd_at (i, &events);

			if (fd == -1)
				continue;

			if (fd == threadpool_io->wakeup_pipes [0]) {
				selector_thread_wakeup_drain_pipes ();
			} else {
				MonoMList *list = mono_g_hash_table_lookup (threadpool_io->states, GINT_TO_POINTER (fd));

				if (list && (events & MONO_POLLIN) != 0) {
					MonoSocketAsyncResult *sockares = get_sockares_for_event (&list, MONO_POLLIN);
					if (sockares)
						mono_threadpool_ms_enqueue_work_item (((MonoObject*) sockares)->vtable->domain, (MonoObject*) sockares);
				}
				if (list && (events & MONO_POLLOUT) != 0) {
					MonoSocketAsyncResult *sockares = get_sockares_for_event (&list, MONO_POLLOUT);
					if (sockares)
						mono_threadpool_ms_enqueue_work_item (((MonoObject*) sockares)->vtable->domain, (MonoObject*) sockares);
				}

				if (!list)
					mono_g_hash_table_remove (threadpool_io->states, GINT_TO_POINTER (fd));
				else
					mono_g_hash_table_replace (threadpool_io->states, GINT_TO_POINTER (fd), list);

				threadpool_io->backend.event_reset_fd_at (i, get_events (list));
			}

			ready -= 1;
		}

		mono_mutex_unlock (&threadpool_io->lock);
	}

	io_selector_running = FALSE;
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
initialize (void)
{
	g_assert (!threadpool_io);
	threadpool_io = g_new0 (ThreadPoolIO, 1);
	g_assert (threadpool_io);

	mono_mutex_init (&threadpool_io->lock);

	threadpool_io->states = mono_g_hash_table_new_type (g_direct_hash, g_direct_equal, MONO_HASH_VALUE_GC);
	MONO_GC_REGISTER_ROOT_FIXED (threadpool_io->states);

	threadpool_io->updates = NULL;
	threadpool_io->updates_size = 0;
	threadpool_io->updates_capacity = 0;

#if defined(HAVE_EPOLL)
	threadpool_io->backend = backend_epoll;
#elif defined(HAVE_KQUEUE)
	threadpool_io->backend = backend_kqueue;
#else
	threadpool_io->backend = backend_poll;
#endif
	if (g_getenv ("MONO_DISABLE_AIO") != NULL)
		threadpool_io->backend = backend_poll;

	wakeup_pipes_init ();

	if (!threadpool_io->backend.init (threadpool_io->wakeup_pipes [0]))
		g_error ("initialize: backend->init () failed");

	if (!mono_thread_create_internal (mono_get_root_domain (), selector_thread, NULL, TRUE, SMALL_STACK))
		g_error ("initialize: mono_thread_create_internal () failed");
}

static void
cleanup (void)
{
	/* we make the assumption along the code that we are
	 * cleaning up only if the runtime is shutting down */
	g_assert (mono_runtime_is_shutting_down ());

	selector_thread_wakeup ();
	while (io_selector_running)
		g_usleep (1000);

	mono_mutex_destroy (&threadpool_io->lock);

	MONO_GC_UNREGISTER_ROOT (threadpool_io->states);
	mono_g_hash_table_destroy (threadpool_io->states);

	if (threadpool_io->updates)
		mono_gc_free_fixed (threadpool_io->updates);

	threadpool_io->backend.cleanup ();

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
}

static gboolean
is_socket_async_callback (MonoImage *system_image, MonoClass *class)
{
	MonoClass *socket_async_callback_class = NULL;

	socket_async_callback_class = mono_class_from_name (system_image, "System.Net.Sockets", "SocketAsyncCallback");
	g_assert (socket_async_callback_class);

	return class == socket_async_callback_class;
}

static gboolean
is_async_read_handler (MonoImage *system_image, MonoClass *class)
{
	MonoClass *async_read_handler_class = NULL;

	async_read_handler_class = mono_class_from_name (system_image, "System.Diagnostics", "Process/AsyncReadHandler");
	g_assert (async_read_handler_class);

	return class == async_read_handler_class;
}

gboolean
mono_threadpool_ms_is_io (MonoObject *target, MonoObject *state)
{
	MonoImage *system_image;
	MonoSocketAsyncResult *sockares;

	system_image = mono_image_loaded ("System");
	if (!system_image)
		return FALSE;

	if (!is_socket_async_callback (system_image, target->vtable->klass) && !is_async_read_handler (system_image, target->vtable->klass))
		return FALSE;

	sockares = (MonoSocketAsyncResult*) state;
	if (sockares->operation < AIO_OP_FIRST || sockares->operation >= AIO_OP_LAST)
		return FALSE;

	return TRUE;
}

void
mono_threadpool_ms_io_cleanup (void)
{
	mono_lazy_cleanup (&io_status, cleanup);
}

MonoAsyncResult *
mono_threadpool_ms_io_add (MonoAsyncResult *ares, MonoSocketAsyncResult *sockares)
{
	ThreadPoolIOUpdate *update;

	g_assert (ares);
	g_assert (sockares);

	if (mono_runtime_is_shutting_down ())
		return NULL;

	mono_lazy_initialize (&io_status, initialize);

	MONO_OBJECT_SETREF (sockares, ares, ares);

	mono_mutex_lock (&threadpool_io->lock);

	threadpool_io->updates_size += 1;
	if (threadpool_io->updates_size > threadpool_io->updates_capacity) {
		ThreadPoolIOUpdate *updates_new, *updates_old;
		gint updates_new_capacity, updates_old_capacity;

		updates_old_capacity = threadpool_io->updates_capacity;
		updates_new_capacity = updates_old_capacity + 128;

		updates_old = threadpool_io->updates;
		updates_new = mono_gc_alloc_fixed (sizeof (ThreadPoolIOUpdate) * updates_new_capacity, MONO_GC_DESCRIPTOR_NULL);
		g_assert (updates_new);

		if (updates_old)
			memcpy (updates_new, updates_old, sizeof (ThreadPoolIOUpdate) * updates_old_capacity);

		threadpool_io->updates = updates_new;
		threadpool_io->updates_capacity = updates_new_capacity;

		if (updates_old)
			mono_gc_free_fixed (updates_old);
	}

	update = &threadpool_io->updates [threadpool_io->updates_size - 1];
	update->sockares = sockares;

	mono_mutex_unlock (&threadpool_io->lock);

	selector_thread_wakeup ();

	return ares;
}

void
mono_threadpool_ms_io_remove_socket (int fd)
{
	MonoMList *list;
	gint i;

	if (!mono_lazy_is_initialized (&io_status))
		return;

	mono_mutex_lock (&threadpool_io->lock);

	g_assert (threadpool_io->states);

	list = mono_g_hash_table_lookup (threadpool_io->states, GINT_TO_POINTER (fd));
	if (list)
		mono_g_hash_table_remove (threadpool_io->states, GINT_TO_POINTER (fd));

	for (i = 0; i < threadpool_io->updates_size; ++i) {
		ThreadPoolIOUpdate *update = &threadpool_io->updates [i];

		g_assert (update->sockares);

		if (GPOINTER_TO_INT (update->sockares->handle) == fd) {
			if (i < threadpool_io->updates_size - 1)
				memmove (threadpool_io->updates + i, threadpool_io->updates + i + 1, sizeof (ThreadPoolIOUpdate) * threadpool_io->updates_size - i - 1);
			memset (threadpool_io->updates + threadpool_io->updates_size - 1, 0, sizeof (ThreadPoolIOUpdate));

			threadpool_io->updates_size -= 1;
		}
	}

	mono_mutex_unlock (&threadpool_io->lock);

	for (; list; list = mono_mlist_remove_item (list, list)) {
		MonoSocketAsyncResult *sockares = (MonoSocketAsyncResult*) mono_mlist_get_data (list);

		if (!sockares)
			continue;

		switch (sockares->operation) {
		case AIO_OP_RECEIVE:
			sockares->operation = AIO_OP_RECV_JUST_CALLBACK;
			break;
		case AIO_OP_SEND:
			sockares->operation = AIO_OP_SEND_JUST_CALLBACK;
			break;
		}

		mono_threadpool_ms_enqueue_work_item (((MonoObject*) sockares)->vtable->domain, (MonoObject*) sockares);
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
	gint i;

	if (!mono_lazy_is_initialized (&io_status))
		return;

	mono_mutex_lock (&threadpool_io->lock);

	mono_g_hash_table_foreach_remove (threadpool_io->states, remove_sockstate_for_domain, domain);

	for (i = 0; i < threadpool_io->updates_size; ++i) {
		ThreadPoolIOUpdate *update = &threadpool_io->updates [i];

		g_assert (update->sockares);

		if (mono_object_domain (update->sockares) == domain) {
			if (i < threadpool_io->updates_size - 1)
				memmove (threadpool_io->updates + i, threadpool_io->updates + i + 1, sizeof (ThreadPoolIOUpdate) * threadpool_io->updates_size - i - 1);
			memset (threadpool_io->updates + threadpool_io->updates_size - 1, 0, sizeof (ThreadPoolIOUpdate));

			threadpool_io->updates_size -= 1;
		}
	}

	mono_mutex_unlock (&threadpool_io->lock);
}

void
icall_append_io_job (MonoObject *target, MonoSocketAsyncResult *state)
{
	MonoAsyncResult *ares;

	/* Don't call mono_async_result_new() to avoid capturing the context */
	ares = (MonoAsyncResult *) mono_object_new (mono_domain_get (), mono_defaults.asyncresult_class);
	MONO_OBJECT_SETREF (ares, async_delegate, target);
	MONO_OBJECT_SETREF (ares, async_state, state);

	mono_threadpool_ms_io_add (ares, state);
	return;
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
icall_append_io_job (MonoObject *target, MonoSocketAsyncResult *state)
{
	g_assert_not_reached ();
}

#endif