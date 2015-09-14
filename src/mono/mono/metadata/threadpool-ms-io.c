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
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-logger-internal.h>

typedef struct {
	gboolean (*init) (gint wakeup_pipe_fd);
	void     (*cleanup) (void);
	void     (*register_fd) (gint fd, gint events, gboolean is_new);
	void     (*remove_fd) (gint fd);
	gint     (*event_wait) (void (*callback) (gint fd, gint events, gpointer user_data), gpointer user_data);
} ThreadPoolIOBackend;

enum {
	EVENT_IN   = 1 << 0,
	EVENT_OUT  = 1 << 1,
	EVENT_ERR  = 1 << 2,
} ThreadPoolIOEvent;

#include "threadpool-ms-io-epoll.c"
#include "threadpool-ms-io-kqueue.c"
#include "threadpool-ms-io-poll.c"

#define UPDATES_CAPACITY 128

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
	UPDATE_EMPTY = 0,
	UPDATE_ADD,
	UPDATE_REMOVE_SOCKET,
	UPDATE_REMOVE_DOMAIN,
} ThreadPoolIOUpdateType;

typedef struct {
	gint fd;
	MonoSocketAsyncResult *sockares;
} ThreadPoolIOUpdate_Add;

typedef struct {
	gint fd;
} ThreadPoolIOUpdate_RemoveSocket;

typedef struct {
	MonoDomain *domain;
} ThreadPoolIOUpdate_RemoveDomain;

typedef struct {
	ThreadPoolIOUpdateType type;
	union {
		ThreadPoolIOUpdate_Add add;
		ThreadPoolIOUpdate_RemoveSocket remove_socket;
		ThreadPoolIOUpdate_RemoveDomain remove_domain;
	} data;
} ThreadPoolIOUpdate;

typedef struct {
	ThreadPoolIOBackend backend;

	ThreadPoolIOUpdate updates [UPDATES_CAPACITY];
	gint updates_size;
	mono_mutex_t updates_lock;
	mono_cond_t updates_cond;

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
		return EVENT_IN;
	case AIO_OP_SEND:
	case AIO_OP_SEND_JUST_CALLBACK:
	case AIO_OP_SENDTO:
	case AIO_OP_CONNECT:
	case AIO_OP_SEND_BUFFERS:
	case AIO_OP_DISCONNECT:
		return EVENT_OUT;
	default:
		g_assert_not_reached ();
	}
}

static MonoSocketAsyncResult*
get_sockares_for_event (MonoMList **list, gint event)
{
	MonoMList *current;

	g_assert (list);

	for (current = *list; current; current = mono_mlist_next (current)) {
		MonoSocketAsyncResult *ares = (MonoSocketAsyncResult*) mono_mlist_get_data (current);
		if (get_events_from_sockares (ares) == event) {
			*list = mono_mlist_remove_item (*list, current);
			return ares;
		}
	}

	return NULL;
}

static gint
get_events (MonoMList *list)
{
	MonoMList *current;
	gint events = 0;

	for (current = list; current; current = mono_mlist_next (current))
		events |= get_events_from_sockares ((MonoSocketAsyncResult*) mono_mlist_get_data (current));

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

typedef struct {
	MonoDomain *domain;
	MonoGHashTable *states;
} FilterSockaresForDomainData;

static void
filter_sockares_for_domain (gpointer key, gpointer value, gpointer user_data)
{
	FilterSockaresForDomainData *data;
	MonoMList *list = value, *element;
	MonoDomain *domain;
	MonoGHashTable *states;

	g_assert (user_data);
	data = user_data;
	domain = data->domain;
	states = data->states;

	for (element = list; element; element = mono_mlist_next (element)) {
		MonoSocketAsyncResult *sockares = (MonoSocketAsyncResult*) mono_mlist_get_data (element);
		if (mono_object_domain (sockares) == domain)
			mono_mlist_set_data (element, NULL);
	}

	/* we skip all the first elements which are NULL */
	for (; list; list = mono_mlist_next (list)) {
		if (mono_mlist_get_data (list))
			break;
	}

	if (list) {
		g_assert (mono_mlist_get_data (list));

		/* we delete all the NULL elements after the first one */
		for (element = list; element;) {
			MonoMList *next;
			if (!(next = mono_mlist_next (element)))
				break;
			if (mono_mlist_get_data (next))
				element = next;
			else
				mono_mlist_set_next (element, mono_mlist_next (next));
		}
	}

	mono_g_hash_table_replace (states, key, list);
}

static void
wait_callback (gint fd, gint events, gpointer user_data)
{
	if (mono_runtime_is_shutting_down ())
		return;

	if (fd == threadpool_io->wakeup_pipes [0]) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_THREADPOOL, "io threadpool: wke");
		selector_thread_wakeup_drain_pipes ();
	} else {
		MonoGHashTable *states;
		MonoMList *list = NULL;
		gpointer k;
		gboolean remove_fd = FALSE;

		g_assert (user_data);
		states = user_data;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_THREADPOOL, "io threadpool: cal fd %3d, events = %2s | %2s | %3s",
			fd, (events & EVENT_IN) ? "RD" : "..", (events & EVENT_OUT) ? "WR" : "..", (events & EVENT_ERR) ? "ERR" : "...");

		if (!mono_g_hash_table_lookup_extended (states, GINT_TO_POINTER (fd), &k, (gpointer*) &list))
			g_error ("wait_callback: fd %d not found in states table", fd);

		if (list && (events & EVENT_IN) != 0) {
			MonoSocketAsyncResult *sockares = get_sockares_for_event (&list, EVENT_IN);
			if (sockares)
				mono_threadpool_ms_enqueue_work_item (((MonoObject*) sockares)->vtable->domain, (MonoObject*) sockares);
		}
		if (list && (events & EVENT_OUT) != 0) {
			MonoSocketAsyncResult *sockares = get_sockares_for_event (&list, EVENT_OUT);
			if (sockares)
				mono_threadpool_ms_enqueue_work_item (((MonoObject*) sockares)->vtable->domain, (MonoObject*) sockares);
		}

		remove_fd = (events & EVENT_ERR) == EVENT_ERR;
		if (!remove_fd) {
			mono_g_hash_table_replace (states, GINT_TO_POINTER (fd), list);

			events = get_events (list);

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_THREADPOOL, "io threadpool: res fd %3d, events = %2s | %2s | %2s",
				fd, (events & EVENT_IN) ? "RD" : "..", (events & EVENT_OUT) ? "WR" : "..", (events & EVENT_ERR) ? "ERR" : "...");

			threadpool_io->backend.register_fd (fd, events, FALSE);
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_THREADPOOL, "io threadpool: err fd %d", fd);

			mono_g_hash_table_remove (states, GINT_TO_POINTER (fd));

			threadpool_io->backend.remove_fd (fd);
		}
	}
}

static void
selector_thread (gpointer data)
{
	MonoGHashTable *states;

	io_selector_running = TRUE;

	if (mono_runtime_is_shutting_down ()) {
		io_selector_running = FALSE;
		return;
	}

	states = mono_g_hash_table_new_type (g_direct_hash, g_direct_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_THREAD_POOL, "i/o thread pool states table");

	for (;;) {
		gint i, j;
		gint res;

		mono_mutex_lock (&threadpool_io->updates_lock);

		for (i = 0; i < threadpool_io->updates_size; ++i) {
			ThreadPoolIOUpdate *update = &threadpool_io->updates [i];

			switch (update->type) {
			case UPDATE_EMPTY:
				break;
			case UPDATE_ADD: {
				gint fd;
				gint events;
				gpointer k;
				gboolean exists;
				MonoMList *list = NULL;
				MonoSocketAsyncResult *sockares;

				fd = update->data.add.fd;
				g_assert (fd >= 0);

				sockares = update->data.add.sockares;
				g_assert (sockares);

				exists = mono_g_hash_table_lookup_extended (states, GINT_TO_POINTER (fd), &k, (gpointer*) &list);
				list = mono_mlist_append (list, (MonoObject*) sockares);
				mono_g_hash_table_replace (states, sockares->handle, list);

				events = get_events (list);

				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_THREADPOOL, "io threadpool: %3s fd %3d, events = %2s | %2s | %2s",
					exists ? "mod" : "add", fd, (events & EVENT_IN) ? "RD" : "..", (events & EVENT_OUT) ? "WR" : "..", (events & EVENT_ERR) ? "ERR" : "...");

				threadpool_io->backend.register_fd (fd, events, !exists);

				break;
			}
			case UPDATE_REMOVE_SOCKET: {
				gint fd;
				gpointer k;
				MonoMList *list = NULL;

				fd = update->data.remove_socket.fd;
				g_assert (fd >= 0);

				if (mono_g_hash_table_lookup_extended (states, GINT_TO_POINTER (fd), &k, (gpointer*) &list)) {
					mono_g_hash_table_remove (states, GINT_TO_POINTER (fd));

					for (j = i + 1; j < threadpool_io->updates_size; ++j) {
						ThreadPoolIOUpdate *update = &threadpool_io->updates [j];
						if (update->type == UPDATE_ADD && update->data.add.fd == fd)
							memset (update, 0, sizeof (ThreadPoolIOUpdate));
					}

					for (; list; list = mono_mlist_remove_item (list, list))
						mono_threadpool_ms_enqueue_work_item (mono_object_domain (mono_mlist_get_data (list)), mono_mlist_get_data (list));

					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_THREADPOOL, "io threadpool: del fd %3d", fd);
					threadpool_io->backend.remove_fd (fd);
				}

				break;
			}
			case UPDATE_REMOVE_DOMAIN: {
				MonoDomain *domain;

				domain = update->data.remove_domain.domain;
				g_assert (domain);

				FilterSockaresForDomainData user_data = { .domain = domain, .states = states };
				mono_g_hash_table_foreach (states, filter_sockares_for_domain, &user_data);

				for (j = i + 1; j < threadpool_io->updates_size; ++j) {
					ThreadPoolIOUpdate *update = &threadpool_io->updates [j];
					if (update->type == UPDATE_ADD && mono_object_domain (update->data.add.sockares) == domain)
						memset (update, 0, sizeof (ThreadPoolIOUpdate));
				}

				break;
			}
			default:
				g_assert_not_reached ();
			}
		}

		mono_cond_broadcast (&threadpool_io->updates_cond);

		if (threadpool_io->updates_size > 0) {
			threadpool_io->updates_size = 0;
			memset (&threadpool_io->updates, 0, UPDATES_CAPACITY * sizeof (ThreadPoolIOUpdate));
		}

		mono_mutex_unlock (&threadpool_io->updates_lock);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_THREADPOOL, "io threadpool: wai");

		res = threadpool_io->backend.event_wait (wait_callback, states);

		if (res == -1 || mono_runtime_is_shutting_down ())
			break;
	}

	mono_g_hash_table_destroy (states);

	io_selector_running = FALSE;
}

/* Locking: threadpool_io->updates_lock must be held */
static ThreadPoolIOUpdate*
update_get_new (void)
{
	ThreadPoolIOUpdate *update = NULL;
	g_assert (threadpool_io->updates_size <= UPDATES_CAPACITY);

	while (threadpool_io->updates_size == UPDATES_CAPACITY) {
		/* we wait for updates to be applied in the selector_thread and we loop
		 * as long as none are available. if it happends too much, then we need
		 * to increase UPDATES_CAPACITY */
		mono_cond_wait (&threadpool_io->updates_cond, &threadpool_io->updates_lock);
	}

	g_assert (threadpool_io->updates_size < UPDATES_CAPACITY);

	update = &threadpool_io->updates [threadpool_io->updates_size ++];

	return update;
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

	mono_mutex_init_recursive (&threadpool_io->updates_lock);
	mono_cond_init (&threadpool_io->updates_cond, 0);
	mono_gc_register_root ((void*)&threadpool_io->updates [0], sizeof (threadpool_io->updates), MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_THREAD_POOL, "i/o thread pool updates list");

	threadpool_io->updates_size = 0;

	threadpool_io->backend = backend_poll;
	if (g_getenv ("MONO_ENABLE_AIO") != NULL) {
#if defined(HAVE_EPOLL)
		threadpool_io->backend = backend_epoll;
#elif defined(HAVE_KQUEUE)
		threadpool_io->backend = backend_kqueue;
#endif
	}

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

	mono_mutex_destroy (&threadpool_io->updates_lock);
	mono_cond_destroy (&threadpool_io->updates_cond);

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

	return class == socket_async_callback_class;
}

static gboolean
is_async_read_handler (MonoImage *system_image, MonoClass *class)
{
	MonoClass *async_read_handler_class = NULL;

	async_read_handler_class = mono_class_from_name (system_image, "System.Diagnostics", "Process/AsyncReadHandler");

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
	if (mono_domain_is_unloading (mono_object_domain (sockares)))
		return NULL;

	mono_lazy_initialize (&io_status, initialize);

	MONO_OBJECT_SETREF (sockares, ares, ares);

	mono_mutex_lock (&threadpool_io->updates_lock);

	update = update_get_new ();
	update->type = UPDATE_ADD;
	update->data.add.fd = GPOINTER_TO_INT (sockares->handle);
	update->data.add.sockares = sockares;
	mono_memory_barrier (); /* Ensure this is safely published before we wake up the selector */

	selector_thread_wakeup ();

	mono_mutex_unlock (&threadpool_io->updates_lock);

	return ares;
}

void
mono_threadpool_ms_io_remove_socket (int fd)
{
	ThreadPoolIOUpdate *update;

	if (!mono_lazy_is_initialized (&io_status))
		return;

	mono_mutex_lock (&threadpool_io->updates_lock);

	update = update_get_new ();
	update->type = UPDATE_REMOVE_SOCKET;
	update->data.add.fd = fd;
	mono_memory_barrier (); /* Ensure this is safely published before we wake up the selector */

	selector_thread_wakeup ();

	mono_cond_wait (&threadpool_io->updates_cond, &threadpool_io->updates_lock);

	mono_mutex_unlock (&threadpool_io->updates_lock);
}

void
mono_threadpool_ms_io_remove_domain_jobs (MonoDomain *domain)
{
	ThreadPoolIOUpdate *update;

	if (!mono_lazy_is_initialized (&io_status))
		return;

	mono_mutex_lock (&threadpool_io->updates_lock);

	update = update_get_new ();
	update->type = UPDATE_REMOVE_DOMAIN;
	update->data.remove_domain.domain = domain;
	mono_memory_barrier (); /* Ensure this is safely published before we wake up the selector */

	selector_thread_wakeup ();

	mono_cond_wait (&threadpool_io->updates_cond, &threadpool_io->updates_lock);

	mono_mutex_unlock (&threadpool_io->updates_lock);
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
