/**
 * \file
 * Microsoft IO threadpool runtime support
 *
 * Author:
 *	Ludovic Henry (ludovic.henry@xamarin.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/threadpool-io.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/w32api.h>

typedef struct {
	gboolean (*init) (gint wakeup_pipe_fd);
	void     (*register_fd) (gint fd, gint events, gboolean is_new);
	void     (*remove_fd) (gint fd);
	gint     (*event_wait) (void (*callback) (gint fd, gint events, gpointer user_data), gpointer user_data);
} ThreadPoolIOBackend;

/* Keep in sync with System.IOOperation in mcs/class/System/System/IOSelector.cs */
enum MonoIOOperation {
	EVENT_IN   = 1 << 0,
	EVENT_OUT  = 1 << 1,
	EVENT_ERR  = 1 << 2, /* not in managed */
};

#include "threadpool-io-epoll.c"
#include "threadpool-io-kqueue.c"
#include "threadpool-io-poll.c"

#define UPDATES_CAPACITY 128

/* Keep in sync with System.IOSelectorJob in mcs/class/System/System/IOSelector.cs */
struct _MonoIOSelectorJob {
	MonoObject object;
	gint32 operation;
	MonoObject *callback;
	MonoObject *state;
};

typedef enum {
	UPDATE_EMPTY = 0,
	UPDATE_ADD,
	UPDATE_REMOVE_SOCKET,
	UPDATE_REMOVE_DOMAIN,
} ThreadPoolIOUpdateType;

typedef struct {
	gint fd;
	MonoIOSelectorJob *job;
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
	MonoCoopMutex updates_lock;
	MonoCoopCond updates_cond;

#if !defined(HOST_WIN32)
	gint wakeup_pipes [2];
#else
	SOCKET wakeup_pipes [2];
#endif
} ThreadPoolIO;

static mono_lazy_init_t io_status = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static gboolean io_selector_running = FALSE;

static ThreadPoolIO* threadpool_io;

static MonoIOSelectorJob*
get_job_for_event (MonoMList **list, gint32 event)
{
	MonoMList *current;

	g_assert (list);

	for (current = *list; current; current = mono_mlist_next (current)) {
		MonoIOSelectorJob *job = (MonoIOSelectorJob*) mono_mlist_get_data (current);
		if (job->operation == event) {
			*list = mono_mlist_remove_item (*list, current);
			return job;
		}
	}

	return NULL;
}

static gint
get_operations_for_jobs (MonoMList *list)
{
	MonoMList *current;
	gint operations = 0;

	for (current = list; current; current = mono_mlist_next (current))
		operations |= ((MonoIOSelectorJob*) mono_mlist_get_data (current))->operation;

	return operations;
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
filter_jobs_for_domain (gpointer key, gpointer value, gpointer user_data)
{
	FilterSockaresForDomainData *data;
	MonoMList *list = (MonoMList *)value, *element;
	MonoDomain *domain;
	MonoGHashTable *states;

	g_assert (user_data);
	data = (FilterSockaresForDomainData *)user_data;
	domain = data->domain;
	states = data->states;

	for (element = list; element; element = mono_mlist_next (element)) {
		MonoIOSelectorJob *job = (MonoIOSelectorJob*) mono_mlist_get_data (element);
		if (mono_object_domain (job) == domain)
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
	MonoError error;

	if (mono_runtime_is_shutting_down ())
		return;

	if (fd == threadpool_io->wakeup_pipes [0]) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_SELECTOR, "io threadpool: wke");
		selector_thread_wakeup_drain_pipes ();
	} else {
		MonoGHashTable *states;
		MonoMList *list = NULL;
		gpointer k;
		gboolean remove_fd = FALSE;
		gint operations;

		g_assert (user_data);
		states = (MonoGHashTable *)user_data;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_SELECTOR, "io threadpool: cal fd %3d, events = %2s | %2s | %3s",
			fd, (events & EVENT_IN) ? "RD" : "..", (events & EVENT_OUT) ? "WR" : "..", (events & EVENT_ERR) ? "ERR" : "...");

		if (!mono_g_hash_table_lookup_extended (states, GINT_TO_POINTER (fd), &k, (gpointer*) &list))
			g_error ("wait_callback: fd %d not found in states table", fd);

		if (list && (events & EVENT_IN) != 0) {
			MonoIOSelectorJob *job = get_job_for_event (&list, EVENT_IN);
			if (job) {
				mono_threadpool_enqueue_work_item (((MonoObject*) job)->vtable->domain, (MonoObject*) job, &error);
				mono_error_assert_ok (&error);
			}

		}
		if (list && (events & EVENT_OUT) != 0) {
			MonoIOSelectorJob *job = get_job_for_event (&list, EVENT_OUT);
			if (job) {
				mono_threadpool_enqueue_work_item (((MonoObject*) job)->vtable->domain, (MonoObject*) job, &error);
				mono_error_assert_ok (&error);
			}
		}

		remove_fd = (events & EVENT_ERR) == EVENT_ERR;
		if (!remove_fd) {
			mono_g_hash_table_replace (states, GINT_TO_POINTER (fd), list);

			operations = get_operations_for_jobs (list);

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_SELECTOR, "io threadpool: res fd %3d, events = %2s | %2s | %3s",
				fd, (operations & EVENT_IN) ? "RD" : "..", (operations & EVENT_OUT) ? "WR" : "..", (operations & EVENT_ERR) ? "ERR" : "...");

			threadpool_io->backend.register_fd (fd, operations, FALSE);
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_SELECTOR, "io threadpool: err fd %d", fd);

			mono_g_hash_table_remove (states, GINT_TO_POINTER (fd));

			threadpool_io->backend.remove_fd (fd);
		}
	}
}

static void
selector_thread_interrupt (gpointer unused)
{
	selector_thread_wakeup ();
}

static gsize WINAPI
selector_thread (gpointer data)
{
	MonoError error;
	MonoGHashTable *states;

	MonoString *thread_name = mono_string_new_checked (mono_get_root_domain (), "Thread Pool I/O Selector", &error);
	mono_error_assert_ok (&error);
	mono_thread_set_name_internal (mono_thread_internal_current (), thread_name, FALSE, TRUE, &error);
	mono_error_assert_ok (&error);

	if (mono_runtime_is_shutting_down ()) {
		io_selector_running = FALSE;
		return 0;
	}

	states = mono_g_hash_table_new_type (g_direct_hash, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_THREAD_POOL, NULL, "Thread Pool I/O State Table");

	while (!mono_runtime_is_shutting_down ()) {
		gint i, j;
		gint res;
		gboolean interrupted = FALSE;

		if (mono_thread_interruption_checkpoint ())
			continue;

		mono_coop_mutex_lock (&threadpool_io->updates_lock);

		for (i = 0; i < threadpool_io->updates_size; ++i) {
			ThreadPoolIOUpdate *update = &threadpool_io->updates [i];

			switch (update->type) {
			case UPDATE_EMPTY:
				break;
			case UPDATE_ADD: {
				gint fd;
				gint operations;
				gpointer k;
				gboolean exists;
				MonoMList *list = NULL;
				MonoIOSelectorJob *job;

				fd = update->data.add.fd;
				g_assert (fd >= 0);

				job = update->data.add.job;
				g_assert (job);

				exists = mono_g_hash_table_lookup_extended (states, GINT_TO_POINTER (fd), &k, (gpointer*) &list);
				list = mono_mlist_append_checked (list, (MonoObject*) job, &error);
				mono_error_assert_ok (&error);
				mono_g_hash_table_replace (states, GINT_TO_POINTER (fd), list);

				operations = get_operations_for_jobs (list);

				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_SELECTOR, "io threadpool: %3s fd %3d, operations = %2s | %2s | %3s",
					exists ? "mod" : "add", fd, (operations & EVENT_IN) ? "RD" : "..", (operations & EVENT_OUT) ? "WR" : "..", (operations & EVENT_ERR) ? "ERR" : "...");

				threadpool_io->backend.register_fd (fd, operations, !exists);

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

					for (; list; list = mono_mlist_remove_item (list, list)) {
						mono_threadpool_enqueue_work_item (mono_object_domain (mono_mlist_get_data (list)), mono_mlist_get_data (list), &error);
						mono_error_assert_ok (&error);
					}

					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_SELECTOR, "io threadpool: del fd %3d", fd);
					threadpool_io->backend.remove_fd (fd);
				}

				break;
			}
			case UPDATE_REMOVE_DOMAIN: {
				MonoDomain *domain;

				domain = update->data.remove_domain.domain;
				g_assert (domain);

				FilterSockaresForDomainData user_data = { .domain = domain, .states = states };
				mono_g_hash_table_foreach (states, filter_jobs_for_domain, &user_data);

				for (j = i + 1; j < threadpool_io->updates_size; ++j) {
					ThreadPoolIOUpdate *update = &threadpool_io->updates [j];
					if (update->type == UPDATE_ADD && mono_object_domain (update->data.add.job) == domain)
						memset (update, 0, sizeof (ThreadPoolIOUpdate));
				}

				break;
			}
			default:
				g_assert_not_reached ();
			}
		}

		mono_coop_cond_broadcast (&threadpool_io->updates_cond);

		if (threadpool_io->updates_size > 0) {
			threadpool_io->updates_size = 0;
			memset (&threadpool_io->updates, 0, UPDATES_CAPACITY * sizeof (ThreadPoolIOUpdate));
		}

		mono_coop_mutex_unlock (&threadpool_io->updates_lock);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_SELECTOR, "io threadpool: wai");

		mono_thread_info_install_interrupt (selector_thread_interrupt, NULL, &interrupted);
		if (interrupted)
			continue;

		res = threadpool_io->backend.event_wait (wait_callback, states);
		if (res == -1)
			break;

		mono_thread_info_uninstall_interrupt (&interrupted);
	}

	mono_g_hash_table_destroy (states);

	mono_coop_mutex_lock (&threadpool_io->updates_lock);

	io_selector_running = FALSE;
	mono_coop_cond_broadcast (&threadpool_io->updates_cond);

	mono_coop_mutex_unlock (&threadpool_io->updates_lock);

	return 0;
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
		mono_coop_cond_wait (&threadpool_io->updates_cond, &threadpool_io->updates_lock);
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

	mono_coop_mutex_init (&threadpool_io->updates_lock);
	mono_coop_cond_init (&threadpool_io->updates_cond);
	mono_gc_register_root ((char *)&threadpool_io->updates [0], sizeof (threadpool_io->updates), MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_THREAD_POOL, NULL, "Thread Pool I/O Update List");

	threadpool_io->updates_size = 0;

	threadpool_io->backend = backend_poll;
	if (g_hasenv ("MONO_ENABLE_AIO")) {
#if defined(HAVE_EPOLL)
		threadpool_io->backend = backend_epoll;
#elif defined(HAVE_KQUEUE)
		threadpool_io->backend = backend_kqueue;
#endif
	}

	wakeup_pipes_init ();

	if (!threadpool_io->backend.init (threadpool_io->wakeup_pipes [0]))
		g_error ("initialize: backend->init () failed");

	mono_coop_mutex_lock (&threadpool_io->updates_lock);

	io_selector_running = TRUE;

	MonoError error;
	if (!mono_thread_create_internal (mono_get_root_domain (), selector_thread, NULL, MONO_THREAD_CREATE_FLAGS_THREADPOOL | MONO_THREAD_CREATE_FLAGS_SMALL_STACK, &error))
		g_error ("initialize: mono_thread_create_internal () failed due to %s", mono_error_get_message (&error));

	mono_coop_mutex_unlock (&threadpool_io->updates_lock);
}

static void
cleanup (void)
{
	// FIXME destroy everything
}

void
mono_threadpool_io_cleanup (void)
{
	mono_lazy_cleanup (&io_status, cleanup);
}

void
ves_icall_System_IOSelector_Add (gpointer handle, MonoIOSelectorJob *job)
{
	ThreadPoolIOUpdate *update;

	g_assert (handle);

	g_assert ((job->operation == EVENT_IN) ^ (job->operation == EVENT_OUT));
	g_assert (job->callback);

	if (mono_runtime_is_shutting_down ())
		return;
	if (mono_domain_is_unloading (mono_object_domain (job)))
		return;

	mono_lazy_initialize (&io_status, initialize);

	mono_coop_mutex_lock (&threadpool_io->updates_lock);

	if (!io_selector_running) {
		mono_coop_mutex_unlock (&threadpool_io->updates_lock);
		return;
	}

	update = update_get_new ();
	update->type = UPDATE_ADD;
	update->data.add.fd = GPOINTER_TO_INT (handle);
	update->data.add.job = job;
	mono_memory_barrier (); /* Ensure this is safely published before we wake up the selector */

	selector_thread_wakeup ();

	mono_coop_mutex_unlock (&threadpool_io->updates_lock);
}

void
ves_icall_System_IOSelector_Remove (gpointer handle)
{
	mono_threadpool_io_remove_socket (GPOINTER_TO_INT (handle));
}

void
mono_threadpool_io_remove_socket (int fd)
{
	ThreadPoolIOUpdate *update;

	if (!mono_lazy_is_initialized (&io_status))
		return;

	mono_coop_mutex_lock (&threadpool_io->updates_lock);

	if (!io_selector_running) {
		mono_coop_mutex_unlock (&threadpool_io->updates_lock);
		return;
	}

	update = update_get_new ();
	update->type = UPDATE_REMOVE_SOCKET;
	update->data.add.fd = fd;
	mono_memory_barrier (); /* Ensure this is safely published before we wake up the selector */

	selector_thread_wakeup ();

	mono_coop_cond_wait (&threadpool_io->updates_cond, &threadpool_io->updates_lock);

	mono_coop_mutex_unlock (&threadpool_io->updates_lock);
}

void
mono_threadpool_io_remove_domain_jobs (MonoDomain *domain)
{
	ThreadPoolIOUpdate *update;

	if (!mono_lazy_is_initialized (&io_status))
		return;

	mono_coop_mutex_lock (&threadpool_io->updates_lock);

	if (!io_selector_running) {
		mono_coop_mutex_unlock (&threadpool_io->updates_lock);
		return;
	}

	update = update_get_new ();
	update->type = UPDATE_REMOVE_DOMAIN;
	update->data.remove_domain.domain = domain;
	mono_memory_barrier (); /* Ensure this is safely published before we wake up the selector */

	selector_thread_wakeup ();

	mono_coop_cond_wait (&threadpool_io->updates_cond, &threadpool_io->updates_lock);

	mono_coop_mutex_unlock (&threadpool_io->updates_lock);
}

#else

void
ves_icall_System_IOSelector_Add (gpointer handle, MonoIOSelectorJob *job)
{
	g_assert_not_reached ();
}

void
ves_icall_System_IOSelector_Remove (gpointer handle)
{
	g_assert_not_reached ();
}

void
mono_threadpool_io_cleanup (void)
{
	g_assert_not_reached ();
}

void
mono_threadpool_io_remove_socket (int fd)
{
	g_assert_not_reached ();
}

void
mono_threadpool_io_remove_domain_jobs (MonoDomain *domain)
{
	g_assert_not_reached ();
}

#endif
