/*
 * threadpool.c: global thread pool
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2010 Novell, Inc (http://www.novell.com)
 * Copyright 2001 Xamarin Inc (http://www.xamarin.com)
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/profiler-private.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/metadata/mono-perfcounters.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/mono-cq.h>
#include <mono/metadata/mono-wsq.h>
#include <mono/metadata/mono-ptr-array.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-semaphore.h>
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
#ifdef HAVE_KQUEUE
#include <sys/event.h>
#endif


#ifndef DISABLE_SOCKETS
#include "mono/io-layer/socket-wrappers.h"
#endif

#include "threadpool.h"

#define THREAD_WANTS_A_BREAK(t) ((t->state & (ThreadState_StopRequested | \
						ThreadState_SuspendRequested)) != 0)

#define SPIN_TRYLOCK(i) (InterlockedCompareExchange (&(i), 1, 0) == 0)
#define SPIN_LOCK(i) do { \
				if (SPIN_TRYLOCK (i)) \
					break; \
			} while (1)

#define SPIN_UNLOCK(i) i = 0
#define SMALL_STACK (128 * (sizeof (gpointer) / 4) * 1024)

/* DEBUG: prints tp data every 2s */
#undef DEBUG 

/* mono_thread_pool_init called */
static volatile int tp_inited;

enum {
	POLL_BACKEND,
	EPOLL_BACKEND,
	KQUEUE_BACKEND
};

typedef struct {
	CRITICAL_SECTION io_lock; /* access to sock_to_state */
	int inited; // 0 -> not initialized , 1->initializing, 2->initialized, 3->cleaned up
	MonoGHashTable *sock_to_state;

	gint event_system;
	gpointer event_data;
	void (*modify) (gpointer event_data, int fd, int operation, int events, gboolean is_new);
	void (*wait) (gpointer sock_data);
	void (*shutdown) (gpointer event_data);
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
} ASyncCall;

typedef struct {
	MonoSemType lock;
	MonoCQ *queue; /* GC root */
	MonoSemType new_job;
	volatile gint waiting; /* threads waiting for a work item */

	/**/
	volatile gint pool_status; /* 0 -> not initialized, 1 -> initialized, 2 -> cleaning up */
	/* min, max, n and busy -> Interlocked */
	volatile gint min_threads;
	volatile gint max_threads;
	volatile gint nthreads;
	volatile gint busy_threads;

	void (*async_invoke) (gpointer data);
	void *pc_nitems; /* Performance counter for total number of items in added */
	void *pc_nthreads; /* Performance counter for total number of active threads */
	/**/
	volatile gint destroy_thread;
	volatile gint ignore_times; /* Used when there's a thread being created or destroyed */
	volatile gint sp_lock; /* spin lock used to protect ignore_times */
	volatile gint64 last_check;
	volatile gint64 time_sum;
	volatile gint n_sum;
	gint64 averages [2];
	gboolean is_io;
} ThreadPool;

static ThreadPool async_tp;
static ThreadPool async_io_tp;

static void async_invoke_thread (gpointer data);
static MonoObject *mono_async_invoke (ThreadPool *tp, MonoAsyncResult *ares);
static void threadpool_free_queue (ThreadPool *tp);
static void threadpool_append_job (ThreadPool *tp, MonoObject *ar);
static void threadpool_append_jobs (ThreadPool *tp, MonoObject **jobs, gint njobs);
static void threadpool_init (ThreadPool *tp, int min_threads, int max_threads, void (*async_invoke) (gpointer));
static void threadpool_start_idle_threads (ThreadPool *tp);
static void threadpool_kill_idle_threads (ThreadPool *tp);
static gboolean threadpool_start_thread (ThreadPool *tp);
static void monitor_thread (gpointer data);
static void socket_io_cleanup (SocketIOData *data);
static MonoObject *get_io_event (MonoMList **list, gint event);
static int get_events_from_list (MonoMList *list);
static int get_event_from_state (MonoSocketAsyncResult *state);

static MonoClass *async_call_klass;
static MonoClass *socket_async_call_klass;
static MonoClass *process_async_call_klass;

static GPtrArray *wsqs;
CRITICAL_SECTION wsqs_lock;

/* Hooks */
static MonoThreadPoolFunc tp_start_func;
static MonoThreadPoolFunc tp_finish_func;
static gpointer tp_hooks_user_data;
static MonoThreadPoolItemFunc tp_item_begin_func;
static MonoThreadPoolItemFunc tp_item_end_func;
static gpointer tp_item_user_data;

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

#include <mono/metadata/tpool-poll.c>
#ifdef HAVE_EPOLL
#include <mono/metadata/tpool-epoll.c>
#elif defined(HAVE_KQUEUE)
#include <mono/metadata/tpool-kqueue.c>
#endif
/*
 * Functions to check whenever a class is given system class. We need to cache things in MonoDomain since some of the
 * assemblies can be unloaded.
 */

static gboolean
is_system_type (MonoDomain *domain, MonoClass *klass)
{
	if (domain->system_image == NULL)
		domain->system_image = mono_image_loaded ("System");

	return klass->image == domain->system_image;
}

static gboolean
is_corlib_type (MonoDomain *domain, MonoClass *klass)
{
	return klass->image == mono_defaults.corlib;
}

/*
 * Note that we call it is_socket_type() where 'socket' refers to the image
 * that contains the System.Net.Sockets.Socket type.
 * For moonlight there is a System.Net.Sockets.Socket class in both System.dll and System.Net.dll.
*/
static gboolean
is_socket_type (MonoDomain *domain, MonoClass *klass)
{
	static const char *version = NULL;
	static gboolean moonlight;

	if (is_system_type (domain, klass))
		return TRUE;

	/* If moonlight, check if the type is in System.Net.dll too */
	if (version == NULL) {
		version = mono_get_runtime_info ()->framework_version;
		moonlight = !strcmp (version, "2.1");
	}

	if (!moonlight)
		return FALSE;

	if (domain->system_net_dll == NULL)
		domain->system_net_dll = mono_image_loaded ("System.Net");
	
	return klass->image == domain->system_net_dll;
}

#define check_type_cached(domain, ASSEMBLY, _class, _namespace, _name, loc) do { \
	if (*loc) \
		return *loc == _class; \
	if (is_##ASSEMBLY##_type (domain, _class) && !strcmp (_name, _class->name) && !strcmp (_namespace, _class->name_space)) { \
		*loc = _class; \
		return TRUE; \
	} \
	return FALSE; \
} while (0) \

#define check_corlib_type_cached(domain, _class, _namespace, _name, loc) check_type_cached (domain, corlib, _class, _namespace, _name, loc)

#define check_socket_type_cached(domain, _class, _namespace, _name, loc) check_type_cached (domain, socket, _class, _namespace, _name, loc)

#define check_system_type_cached(domain, _class, _namespace, _name, loc) check_type_cached (domain, system, _class, _namespace, _name, loc)

static gboolean
is_corlib_asyncresult (MonoDomain *domain, MonoClass *klass)
{
	check_corlib_type_cached (domain, klass, "System.Runtime.Remoting.Messaging", "AsyncResult", &domain->corlib_asyncresult_class);
}

static gboolean
is_socket (MonoDomain *domain, MonoClass *klass)
{
	check_socket_type_cached (domain, klass, "System.Net.Sockets", "Socket", &domain->socket_class);
}

static gboolean
is_socketasyncresult (MonoDomain *domain, MonoClass *klass)
{
	return (klass->nested_in &&
			is_socket (domain, klass->nested_in) &&
			!strcmp (klass->name, "SocketAsyncResult"));
}

static gboolean
is_socketasynccall (MonoDomain *domain, MonoClass *klass)
{
	return (klass->nested_in &&
			is_socket (domain, klass->nested_in) &&
			!strcmp (klass->name, "SocketAsyncCall"));
}

static gboolean
is_appdomainunloaded_exception (MonoDomain *domain, MonoClass *klass)
{
	check_corlib_type_cached (domain, klass, "System", "AppDomainUnloadedException", &domain->ad_unloaded_ex_class);
}

static gboolean
is_sd_process (MonoDomain *domain, MonoClass *klass)
{
	check_system_type_cached (domain, klass, "System.Diagnostics", "Process", &domain->process_class);
}

static gboolean
is_sdp_asyncreadhandler (MonoDomain *domain, MonoClass *klass)
{

	return (klass->nested_in &&
			is_sd_process (domain, klass->nested_in) &&
		!strcmp (klass->name, "AsyncReadHandler"));
}


#ifdef DISABLE_SOCKETS

#define socket_io_cleanup(x)

static int
get_event_from_state (MonoSocketAsyncResult *state)
{
	g_assert_not_reached ();
	return -1;
}

static int
get_events_from_list (MonoMList *list)
{
	return 0;
}

#else

static void
socket_io_cleanup (SocketIOData *data)
{
	EnterCriticalSection (&data->io_lock);
	if (data->inited != 2) {
		LeaveCriticalSection (&data->io_lock);
		return;
	}
	data->inited = 3;
	data->shutdown (data->event_data);
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
	default: /* Should never happen */
		g_message ("get_event_from_state: unknown value in switch!!!");
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
	MonoDomain *domain;
	int remaining_jobs;

	if (obj == NULL)
		return FALSE;

	domain = obj->vtable->domain;
	remaining_jobs = InterlockedDecrement (&domain->threadpool_jobs);
	if (remaining_jobs == 0 && domain->cleanup_semaphore) {
		ReleaseSemaphore (domain->cleanup_semaphore, 1, NULL);
		return TRUE;
	}
	return FALSE;
}

static MonoObject *
get_io_event (MonoMList **list, gint event)
{
	MonoObject *state;
	MonoMList *current;
	MonoMList *prev;

	current = *list;
	prev = NULL;
	state = NULL;
	while (current) {
		state = mono_mlist_get_data (current);
		if (get_event_from_state ((MonoSocketAsyncResult *) state) == event)
			break;

		state = NULL;
		prev = current;
		current = mono_mlist_next (current);
	}

	if (current) {
		if (prev) {
			mono_mlist_set_next (prev, mono_mlist_next (current));
		} else {
			*list = mono_mlist_next (*list);
		}
	}

	return state;
}

/*
 * select/poll wake up when a socket is closed, but epoll just removes
 * the socket from its internal list without notification.
 */
void
mono_thread_pool_remove_socket (int sock)
{
	MonoMList *list;
	MonoSocketAsyncResult *state;
	MonoObject *ares;

	if (socket_io_data.inited == 0)
		return;

	EnterCriticalSection (&socket_io_data.io_lock);
	if (socket_io_data.sock_to_state == NULL) {
		LeaveCriticalSection (&socket_io_data.io_lock);
		return;
	}
	list = mono_g_hash_table_lookup (socket_io_data.sock_to_state, GINT_TO_POINTER (sock));
	if (list)
		mono_g_hash_table_remove (socket_io_data.sock_to_state, GINT_TO_POINTER (sock));
	LeaveCriticalSection (&socket_io_data.io_lock);
	
	while (list) {
		state = (MonoSocketAsyncResult *) mono_mlist_get_data (list);
		if (state->operation == AIO_OP_RECEIVE)
			state->operation = AIO_OP_RECV_JUST_CALLBACK;
		else if (state->operation == AIO_OP_SEND)
			state->operation = AIO_OP_SEND_JUST_CALLBACK;

		ares = get_io_event (&list, MONO_POLLIN);
		threadpool_append_job (&async_io_tp, ares);
		if (list) {
			ares = get_io_event (&list, MONO_POLLOUT);
			threadpool_append_job (&async_io_tp, ares);
		}
	}
}

static void
init_event_system (SocketIOData *data)
{
#ifdef HAVE_EPOLL
	if (data->event_system == EPOLL_BACKEND) {
		data->event_data = tp_epoll_init (data);
		if (data->event_data == NULL) {
			if (g_getenv ("MONO_DEBUG"))
				g_message ("Falling back to poll()");
			data->event_system = POLL_BACKEND;
		}
	}
#elif defined(HAVE_KQUEUE)
	if (data->event_system == KQUEUE_BACKEND)
		data->event_data = tp_kqueue_init (data);
#endif
	if (data->event_system == POLL_BACKEND)
		data->event_data = tp_poll_init (data);
}

static void
socket_io_init (SocketIOData *data)
{
	int inited;

	if (data->inited >= 2) // 2 -> initialized, 3-> cleaned up
		return;

	inited = InterlockedCompareExchange (&data->inited, 1, 0);
	if (inited >= 1) {
		while (TRUE) {
			if (data->inited >= 2)
				return;
			SleepEx (1, FALSE);
		}
	}

	EnterCriticalSection (&data->io_lock);
	data->sock_to_state = mono_g_hash_table_new_type (g_direct_hash, g_direct_equal, MONO_HASH_VALUE_GC);
#ifdef HAVE_EPOLL
	data->event_system = EPOLL_BACKEND;
#elif defined(HAVE_KQUEUE)
	data->event_system = KQUEUE_BACKEND;
#else
	data->event_system = POLL_BACKEND;
#endif
	if (g_getenv ("MONO_DISABLE_AIO") != NULL)
		data->event_system = POLL_BACKEND;

	init_event_system (data);
	mono_thread_create_internal (mono_get_root_domain (), data->wait, data, TRUE, SMALL_STACK);
	LeaveCriticalSection (&data->io_lock);
	data->inited = 2;
	threadpool_start_thread (&async_io_tp);
}

static void
socket_io_add (MonoAsyncResult *ares, MonoSocketAsyncResult *state)
{
	MonoMList *list;
	SocketIOData *data = &socket_io_data;
	int fd;
	gboolean is_new;
	int ievt;

	socket_io_init (&socket_io_data);
	if (mono_runtime_is_shutting_down () || data->inited == 3 || data->sock_to_state == NULL)
		return;
	if (async_tp.pool_status == 2)
		return;

	MONO_OBJECT_SETREF (state, ares, ares);

	fd = GPOINTER_TO_INT (state->handle);
	EnterCriticalSection (&data->io_lock);
	if (data->sock_to_state == NULL) {
		LeaveCriticalSection (&data->io_lock);
		return;
	}
	list = mono_g_hash_table_lookup (data->sock_to_state, GINT_TO_POINTER (fd));
	if (list == NULL) {
		list = mono_mlist_alloc ((MonoObject*)state);
		is_new = TRUE;
	} else {
		list = mono_mlist_append (list, (MonoObject*)state);
		is_new = FALSE;
	}

	mono_g_hash_table_replace (data->sock_to_state, state->handle, list);
	ievt = get_events_from_list (list);
	LeaveCriticalSection (&data->io_lock);
	data->modify (data->event_data, fd, state->operation, ievt, is_new);
}

#ifndef DISABLE_SOCKETS
static gboolean
socket_io_filter (MonoObject *target, MonoObject *state)
{
	gint op;
	MonoSocketAsyncResult *sock_res;
	MonoClass *klass;
	MonoDomain *domain;

	if (target == NULL || state == NULL)
		return FALSE;

	domain = target->vtable->domain;
	klass = target->vtable->klass;
	if (socket_async_call_klass == NULL && is_socketasynccall (domain, klass))
		socket_async_call_klass = klass;

	if (process_async_call_klass == NULL && is_sdp_asyncreadhandler (domain, klass))
		process_async_call_klass = klass;

	if (klass != socket_async_call_klass && klass != process_async_call_klass)
		return FALSE;

	sock_res = (MonoSocketAsyncResult *) state;
	op = sock_res->operation;
	if (op < AIO_OP_FIRST || op >= AIO_OP_LAST)
		return FALSE;

	return TRUE;
}
#endif /* !DISABLE_SOCKETS */

/* Returns the exception thrown when invoking, if any */
static MonoObject *
mono_async_invoke (ThreadPool *tp, MonoAsyncResult *ares)
{
	ASyncCall *ac = (ASyncCall *)ares->object_data;
	MonoObject *res, *exc = NULL;
	MonoArray *out_args = NULL;
	HANDLE wait_event = NULL;

	if (ares->execution_context) {
		/* use captured ExecutionContext (if available) */
		MONO_OBJECT_SETREF (ares, original_context, mono_thread_get_execution_context ());
		mono_thread_set_execution_context (ares->execution_context);
	} else {
		ares->original_context = NULL;
	}

	if (ac == NULL) {
		/* Fast path from ThreadPool.*QueueUserWorkItem */
		void *pa = ares->async_state;
		res = mono_runtime_delegate_invoke (ares->async_delegate, &pa, &exc);
	} else {
		MonoObject *cb_exc = NULL;

		ac->msg->exc = NULL;
		res = mono_message_invoke (ares->async_delegate, ac->msg, &exc, &out_args);
		MONO_OBJECT_SETREF (ac, res, res);
		MONO_OBJECT_SETREF (ac, msg->exc, exc);
		MONO_OBJECT_SETREF (ac, out_args, out_args);

		mono_monitor_enter ((MonoObject *) ares);
		ares->completed = 1;
		if (ares->handle != NULL)
			wait_event = mono_wait_handle_get_handle ((MonoWaitHandle *) ares->handle);
		mono_monitor_exit ((MonoObject *) ares);
		/* notify listeners */
		if (wait_event != NULL)
			SetEvent (wait_event);

		/* call async callback if cb_method != null*/
		if (ac != NULL && ac->cb_method) {
			void *pa = &ares;
			cb_exc = NULL;
			mono_runtime_invoke (ac->cb_method, ac->cb_target, pa, &cb_exc);
			exc = cb_exc;
		} else {
			exc = NULL;
		}
	}

	/* restore original thread execution context if flow isn't suppressed, i.e. non null */
	if (ares->original_context) {
		mono_thread_set_execution_context (ares->original_context);
		ares->original_context = NULL;
	}
	return exc;
}

static void
threadpool_start_idle_threads (ThreadPool *tp)
{
	int n;
	guint32 stack_size;

	stack_size = (!tp->is_io) ? 0 : SMALL_STACK;
	do {
		while (1) {
			n = tp->nthreads;
			if (n >= tp->min_threads)
				return;
			if (InterlockedCompareExchange (&tp->nthreads, n + 1, n) == n)
				break;
		}
		mono_perfcounter_update_value (tp->pc_nthreads, TRUE, 1);
		mono_thread_create_internal (mono_get_root_domain (), tp->async_invoke, tp, TRUE, stack_size);
		SleepEx (100, TRUE);
	} while (1);
}

static void
threadpool_init (ThreadPool *tp, int min_threads, int max_threads, void (*async_invoke) (gpointer))
{
	memset (tp, 0, sizeof (ThreadPool));
	tp->min_threads = min_threads;
	tp->max_threads = max_threads;
	tp->async_invoke = async_invoke;
	tp->queue = mono_cq_create ();
	MONO_SEM_INIT (&tp->new_job, 0);
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

#ifdef DEBUG
static void
print_pool_info (ThreadPool *tp)
{

//	if (tp->tail - tp->head == 0)
//		return;

	g_print ("Pool status? %d\n", InterlockedCompareExchange (&tp->pool_status, 0, 0));
	g_print ("Min. threads: %d\n", InterlockedCompareExchange (&tp->min_threads, 0, 0));
	g_print ("Max. threads: %d\n", InterlockedCompareExchange (&tp->max_threads, 0, 0));
	g_print ("nthreads: %d\n", InterlockedCompareExchange (&tp->nthreads, 0, 0));
	g_print ("busy threads: %d\n", InterlockedCompareExchange (&tp->busy_threads, 0, 0));
	g_print ("Waiting: %d\n", InterlockedCompareExchange (&tp->waiting, 0, 0));
	g_print ("Queued: %d\n", (tp->tail - tp->head));
	if (tp == &async_tp) {
		int i;
		EnterCriticalSection (&wsqs_lock);
		for (i = 0; i < wsqs->len; i++) {
			g_print ("\tWSQ %d: %d\n", i, mono_wsq_count (g_ptr_array_index (wsqs, i)));
		}
		LeaveCriticalSection (&wsqs_lock);
	} else {
		g_print ("\tSockets: %d\n", mono_g_hash_table_size (socket_io_data.sock_to_state));
	}
	g_print ("-------------\n");
}

static void
signal_handler (int signo)
{
	ThreadPool *tp;

	tp = &async_tp;
	g_print ("\n-----Non-IO-----\n");
	print_pool_info (tp);
	tp = &async_io_tp;
	g_print ("\n-----IO-----\n");
	print_pool_info (tp);
	alarm (2);
}
#endif

static void
monitor_thread (gpointer unused)
{
	ThreadPool *pools [2];
	MonoInternalThread *thread;
	guint32 ms;
	gboolean need_one;
	int i;

	pools [0] = &async_tp;
	pools [1] = &async_io_tp;
	thread = mono_thread_internal_current ();
	ves_icall_System_Threading_Thread_SetName_internal (thread, mono_string_new (mono_domain_get (), "Threadpool monitor"));
	while (1) {
		ms = 500;
		do {
			guint32 ts;
			ts = mono_msec_ticks ();
			if (SleepEx (ms, TRUE) == 0)
				break;
			ms -= (mono_msec_ticks () - ts);
			if (mono_runtime_is_shutting_down ())
				break;
			if (THREAD_WANTS_A_BREAK (thread))
				mono_thread_interruption_checkpoint ();
		} while (ms > 0);

		if (mono_runtime_is_shutting_down ())
			break;

		for (i = 0; i < 2; i++) {
			ThreadPool *tp;
			tp = pools [i];
			if (tp->waiting > 0)
				continue;
			need_one = (mono_cq_count (tp->queue) > 0);
			if (!need_one && !tp->is_io) {
				EnterCriticalSection (&wsqs_lock);
				for (i = 0; wsqs != NULL && i < wsqs->len; i++) {
					MonoWSQ *wsq;
					wsq = g_ptr_array_index (wsqs, i);
					if (mono_wsq_count (wsq) != 0) {
						need_one = TRUE;
						break;
					}
				}
				LeaveCriticalSection (&wsqs_lock);
			}
			if (need_one)
				threadpool_start_thread (tp);
		}
	}
}

void
mono_thread_pool_init ()
{
	gint threads_per_cpu = 1;
	gint thread_count;
	gint cpu_count = mono_cpu_count ();
	int result;

	if (tp_inited == 2)
		return;

	result = InterlockedCompareExchange (&tp_inited, 1, 0);
	if (result == 1) {
		while (1) {
			SleepEx (1, FALSE);
			if (tp_inited == 2)
				return;
		}
	}

	MONO_GC_REGISTER_ROOT_FIXED (socket_io_data.sock_to_state);
	InitializeCriticalSection (&socket_io_data.io_lock);
	if (g_getenv ("MONO_THREADS_PER_CPU") != NULL) {
		threads_per_cpu = atoi (g_getenv ("MONO_THREADS_PER_CPU"));
		if (threads_per_cpu < 1)
			threads_per_cpu = 1;
	}

	thread_count = MIN (cpu_count * threads_per_cpu, 100 * cpu_count);
	threadpool_init (&async_tp, thread_count, MAX (100 * cpu_count, thread_count), async_invoke_thread);
	threadpool_init (&async_io_tp, cpu_count * 2, cpu_count * 4, async_invoke_thread);
	async_io_tp.is_io = TRUE;

	async_call_klass = mono_class_from_name (mono_defaults.corlib, "System", "MonoAsyncCall");
	g_assert (async_call_klass);

	InitializeCriticalSection (&wsqs_lock);
	wsqs = g_ptr_array_sized_new (MAX (100 * cpu_count, thread_count));
	mono_wsq_init ();

	async_tp.pc_nitems = init_perf_counter ("Mono Threadpool", "Work Items Added");
	g_assert (async_tp.pc_nitems);

	async_io_tp.pc_nitems = init_perf_counter ("Mono Threadpool", "IO Work Items Added");
	g_assert (async_io_tp.pc_nitems);

	async_tp.pc_nthreads = init_perf_counter ("Mono Threadpool", "# of Threads");
	g_assert (async_tp.pc_nthreads);

	async_io_tp.pc_nthreads = init_perf_counter ("Mono Threadpool", "# of IO Threads");
	g_assert (async_io_tp.pc_nthreads);
	tp_inited = 2;
#ifdef DEBUG
	signal (SIGALRM, signal_handler);
	alarm (2);
#endif
}

static MonoAsyncResult *
create_simple_asyncresult (MonoObject *target, MonoObject *state)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;

	/* Don't call mono_async_result_new() to avoid capturing the context */
	ares = (MonoAsyncResult *) mono_object_new (domain, mono_defaults.asyncresult_class);
	MONO_OBJECT_SETREF (ares, async_delegate, target);
	MONO_OBJECT_SETREF (ares, async_state, state);
	return ares;
}

void
icall_append_io_job (MonoObject *target, MonoSocketAsyncResult *state)
{
	MonoAsyncResult *ares;

	ares = create_simple_asyncresult (target, (MonoObject *) state);
	socket_io_add (ares, state);
}

MonoAsyncResult *
mono_thread_pool_add (MonoObject *target, MonoMethodMessage *msg, MonoDelegate *async_callback,
		      MonoObject *state)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	ASyncCall *ac;

	ac = (ASyncCall*)mono_object_new (domain, async_call_klass);
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
	threadpool_append_job (&async_tp, (MonoObject *) ares);
	return ares;
}

MonoObject *
mono_thread_pool_finish (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc)
{
	ASyncCall *ac;
	HANDLE wait_event;

	*exc = NULL;
	*out_args = NULL;

	/* check if already finished */
	mono_monitor_enter ((MonoObject *) ares);
	
	if (ares->endinvoke_called) {
		*exc = (MonoObject *) mono_get_exception_invalid_operation (NULL);
		mono_monitor_exit ((MonoObject *) ares);
		return NULL;
	}

	ares->endinvoke_called = 1;
	/* wait until we are really finished */
	if (!ares->completed) {
		if (ares->handle == NULL) {
			wait_event = CreateEvent (NULL, TRUE, FALSE, NULL);
			g_assert(wait_event != 0);
			MONO_OBJECT_SETREF (ares, handle, (MonoObject *) mono_wait_handle_new (mono_object_domain (ares), wait_event));
		} else {
			wait_event = mono_wait_handle_get_handle ((MonoWaitHandle *) ares->handle);
		}
		mono_monitor_exit ((MonoObject *) ares);
		WaitForSingleObjectEx (wait_event, INFINITE, TRUE);
	} else {
		mono_monitor_exit ((MonoObject *) ares);
	}

	ac = (ASyncCall *) ares->object_data;
	g_assert (ac != NULL);
	*exc = ac->msg->exc; /* FIXME: GC add write barrier */
	*out_args = ac->out_args;

	return ac->res;
}

static void
threadpool_kill_idle_threads (ThreadPool *tp)
{
	gint n;

	n = (gint) InterlockedCompareExchange (&tp->max_threads, 0, -1);
	while (n) {
		n--;
		MONO_SEM_POST (&tp->new_job);
	}
}

void
mono_thread_pool_cleanup (void)
{
	if (InterlockedExchange (&async_io_tp.pool_status, 2) == 1) {
		socket_io_cleanup (&socket_io_data); /* Empty when DISABLE_SOCKETS is defined */
		threadpool_kill_idle_threads (&async_io_tp);
	}

	if (async_io_tp.queue != NULL) {
		MONO_SEM_DESTROY (&async_io_tp.new_job);
		threadpool_free_queue (&async_io_tp);
	}


	if (InterlockedExchange (&async_tp.pool_status, 2) == 1) {
		threadpool_kill_idle_threads (&async_tp);
		threadpool_free_queue (&async_tp);
	}

	if (wsqs) {
		EnterCriticalSection (&wsqs_lock);
		mono_wsq_cleanup ();
		if (wsqs)
			g_ptr_array_free (wsqs, TRUE);
		wsqs = NULL;
		LeaveCriticalSection (&wsqs_lock);
		MONO_SEM_DESTROY (&async_tp.new_job);
	}
}

static gboolean
threadpool_start_thread (ThreadPool *tp)
{
	gint n;
	guint32 stack_size;

	stack_size = (!tp->is_io) ? 0 : SMALL_STACK;
	while (!mono_runtime_is_shutting_down () && (n = tp->nthreads) < tp->max_threads) {
		if (InterlockedCompareExchange (&tp->nthreads, n + 1, n) == n) {
			mono_perfcounter_update_value (tp->pc_nthreads, TRUE, 1);
			mono_thread_create_internal (mono_get_root_domain (), tp->async_invoke, tp, TRUE, stack_size);
			return TRUE;
		}
	}

	return FALSE;
}

static void
pulse_on_new_job (ThreadPool *tp)
{
	if (tp->waiting)
		MONO_SEM_POST (&tp->new_job);
}

void
icall_append_job (MonoObject *ar)
{
	threadpool_append_jobs (&async_tp, &ar, 1);
}

static void
threadpool_append_job (ThreadPool *tp, MonoObject *ar)
{
	threadpool_append_jobs (tp, &ar, 1);
}

static void
threadpool_append_jobs (ThreadPool *tp, MonoObject **jobs, gint njobs)
{
	static int job_counter;
	MonoObject *ar;
	gint i;

	if (mono_runtime_is_shutting_down ())
		return;

	if (tp->pool_status == 0 && InterlockedCompareExchange (&tp->pool_status, 1, 0) == 0) {
		if (!tp->is_io) {
			mono_thread_create_internal (mono_get_root_domain (), monitor_thread, NULL, TRUE, SMALL_STACK);
			threadpool_start_thread (tp);
		}
		/* Create on demand up to min_threads to avoid startup penalty for apps that don't use
		 * the threadpool that much
		* mono_thread_create_internal (mono_get_root_domain (), threadpool_start_idle_threads, tp, TRUE, SMALL_STACK);
		*/
	}

	for (i = 0; i < njobs; i++) {
		ar = jobs [i];
		if (ar == NULL || mono_domain_is_unloading (ar->vtable->domain))
			continue; /* Might happen when cleaning domain jobs */
		if (!tp->is_io && (InterlockedIncrement (&job_counter) % 10) == 0) {
			MonoAsyncResult *o = (MonoAsyncResult *) ar;
			o->add_time = mono_100ns_ticks ();
		}
		threadpool_jobs_inc (ar); 
		mono_perfcounter_update_value (tp->pc_nitems, TRUE, 1);
		if (!tp->is_io && mono_wsq_local_push (ar))
			continue;

		mono_cq_enqueue (tp->queue, ar);
	}

	for (i = 0; tp->waiting > 0 && i < MIN(njobs, tp->max_threads); i++)
		pulse_on_new_job (tp);
}

static void
threadpool_clear_queue (ThreadPool *tp, MonoDomain *domain)
{
	MonoObject *obj;
	MonoMList *other;

	other = NULL;
	while (mono_cq_dequeue (tp->queue, &obj)) {
		if (obj == NULL)
			continue;
		if (obj->vtable->domain != domain)
			other = mono_mlist_prepend (other, obj);
		threadpool_jobs_dec (obj);
	}

	while (other) {
		threadpool_append_job (tp, (MonoObject *) mono_mlist_get_data (other));
		other = mono_mlist_next (other);
	}
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
	mono_cq_destroy (tp->queue);
	tp->queue = NULL;
}

gboolean
mono_thread_pool_is_queue_array (MonoArray *o)
{
	// gpointer obj = o;

	// FIXME: need some fix in sgen code.
	return FALSE;
}

static MonoWSQ *
add_wsq (void)
{
	int i;
	MonoWSQ *wsq;

	EnterCriticalSection (&wsqs_lock);
	wsq = mono_wsq_create ();
	if (wsqs == NULL) {
		LeaveCriticalSection (&wsqs_lock);
		return NULL;
	}
	for (i = 0; i < wsqs->len; i++) {
		if (g_ptr_array_index (wsqs, i) == NULL) {
			wsqs->pdata [i] = wsq;
			LeaveCriticalSection (&wsqs_lock);
			return wsq;
		}
	}
	g_ptr_array_add (wsqs, wsq);
	LeaveCriticalSection (&wsqs_lock);
	return wsq;
}

static void
remove_wsq (MonoWSQ *wsq)
{
	gpointer data;

	if (wsq == NULL)
		return;

	EnterCriticalSection (&wsqs_lock);
	if (wsqs == NULL) {
		LeaveCriticalSection (&wsqs_lock);
		return;
	}
	g_ptr_array_remove_fast (wsqs, wsq);
	data = NULL;
	/*
	 * Only clean this up when shutting down, any other case will error out
	 * if we're removing a queue that still has work items.
	 */
	if (mono_runtime_is_shutting_down ()) {
		while (mono_wsq_local_pop (&data)) {
			threadpool_jobs_dec (data);
			data = NULL;
		}
	}
	mono_wsq_destroy (wsq);
	LeaveCriticalSection (&wsqs_lock);
}

static void
try_steal (MonoWSQ *local_wsq, gpointer *data, gboolean retry)
{
	int i;
	int ms;

	if (wsqs == NULL || data == NULL || *data != NULL)
		return;

	ms = 0;
	do {
		if (mono_runtime_is_shutting_down ())
			return;

		EnterCriticalSection (&wsqs_lock);
		for (i = 0; wsqs != NULL && i < wsqs->len; i++) {
			MonoWSQ *wsq;

			wsq = wsqs->pdata [i];
			if (wsq == local_wsq || mono_wsq_count (wsq) == 0)
				continue;
			mono_wsq_try_steal (wsqs->pdata [i], data, ms);
			if (*data != NULL) {
				LeaveCriticalSection (&wsqs_lock);
				return;
			}
		}
		LeaveCriticalSection (&wsqs_lock);
		ms += 10;
	} while (retry && ms < 11);
}

static gboolean
dequeue_or_steal (ThreadPool *tp, gpointer *data, MonoWSQ *local_wsq)
{
	if (mono_runtime_is_shutting_down ())
		return FALSE;
	mono_cq_dequeue (tp->queue, (MonoObject **) data);
	if (!tp->is_io && !*data)
		try_steal (local_wsq, data, FALSE);
	return (*data != NULL);
}

static void
process_idle_times (ThreadPool *tp, gint64 t)
{
	gint64 ticks;
	gint64 avg;
	gboolean compute_avg;
	gint new_threads;
	gint64 per1;

	if (tp->ignore_times || t <= 0)
		return;

	compute_avg = FALSE;
	ticks = mono_100ns_ticks ();
	t = ticks - t;
	SPIN_LOCK (tp->sp_lock);
	if (tp->ignore_times) {
		SPIN_UNLOCK (tp->sp_lock);
		return;
	}
	tp->time_sum += t;
	tp->n_sum++;
	if (tp->last_check == 0)
		tp->last_check = ticks;
	else if (tp->last_check > 0 && (ticks - tp->last_check) > 5000000) {
		tp->ignore_times = 1;
		compute_avg = TRUE;
	}
	SPIN_UNLOCK (tp->sp_lock);

	if (!compute_avg)
		return;

	//printf ("Items: %d Time elapsed: %.3fs\n", tp->n_sum, (ticks - tp->last_check) / 10000.0);
	tp->last_check = ticks;
	new_threads = 0;
	avg = tp->time_sum / tp->n_sum;
	if (tp->averages [1] == 0) {
		tp->averages [1] = avg;
	} else {
		per1 = ((100 * (ABS (avg - tp->averages [1]))) / tp->averages [1]);
		if (per1 > 5) {
			if (avg > tp->averages [1]) {
				if (tp->averages [1] < tp->averages [0]) {
					new_threads = -1;
				} else {
					new_threads = 1;
				}
			} else if (avg < tp->averages [1] && tp->averages [1] < tp->averages [0]) {
				new_threads = 1;
			}
		} else {
			int min, n;
			min = tp->min_threads;
			n = tp->nthreads;
			if ((n - min) < min && tp->busy_threads == n)
				new_threads = 1;
		}
		/*
		if (new_threads != 0) {
			printf ("n: %d per1: %lld avg=%lld avg1=%lld avg0=%lld\n", new_threads, per1, avg, tp->averages [1], tp->averages [0]);
		}
		*/
	}

	tp->time_sum = 0;
	tp->n_sum = 0;

	tp->averages [0] = tp->averages [1];
	tp->averages [1] = avg;
	tp->ignore_times = 0;

	if (new_threads == -1) {
		if (tp->destroy_thread == 0 && InterlockedCompareExchange (&tp->destroy_thread, 1, 0) == 0)
			pulse_on_new_job (tp);
	}
}

static gboolean
should_i_die (ThreadPool *tp)
{
	gboolean result = FALSE;
	if (tp->destroy_thread == 1 && InterlockedCompareExchange (&tp->destroy_thread, 0, 1) == 1)
		result = (tp->nthreads > tp->min_threads);
	return result;
}

static void
async_invoke_thread (gpointer data)
{
	MonoDomain *domain;
	MonoInternalThread *thread;
	MonoWSQ *wsq;
	ThreadPool *tp;
	gboolean must_die;
	const gchar *name;
  
	tp = data;
	wsq = NULL;
	if (!tp->is_io)
		wsq = add_wsq ();

	thread = mono_thread_internal_current ();

	mono_profiler_thread_start (thread->tid);
	name = (tp->is_io) ? "IO Threadpool worker" : "Threadpool worker";
	mono_thread_set_name_internal (thread, mono_string_new (mono_domain_get (), name), FALSE);

	if (tp_start_func)
		tp_start_func (tp_hooks_user_data);

	data = NULL;
	for (;;) {
		MonoAsyncResult *ar;
		MonoClass *klass;
		gboolean is_io_task;
		gboolean is_socket;
		int n_naps = 0;

		is_io_task = FALSE;
		ar = (MonoAsyncResult *) data;
		if (ar) {
			InterlockedIncrement (&tp->busy_threads);
			domain = ((MonoObject *)ar)->vtable->domain;
#ifndef DISABLE_SOCKETS
			klass = ((MonoObject *) data)->vtable->klass;
			is_io_task = !is_corlib_asyncresult (domain, klass);
			is_socket = FALSE;
			if (is_io_task) {
				MonoSocketAsyncResult *state = (MonoSocketAsyncResult *) data;
				is_socket = is_socketasyncresult (domain, klass);
				ar = state->ares;
				switch (state->operation) {
				case AIO_OP_RECEIVE:
					state->total = ICALL_RECV (state);
					break;
				case AIO_OP_SEND:
					state->total = ICALL_SEND (state);
					break;
				}
			}
#endif
			/* worker threads invokes methods in different domains,
			 * so we need to set the right domain here */
			g_assert (domain);

			if (mono_domain_is_unloading (domain) || mono_runtime_is_shutting_down ()) {
				threadpool_jobs_dec ((MonoObject *)ar);
				data = NULL;
				ar = NULL;
				InterlockedDecrement (&tp->busy_threads);
			} else {
				mono_thread_push_appdomain_ref (domain);
				if (threadpool_jobs_dec ((MonoObject *)ar)) {
					data = NULL;
					ar = NULL;
					mono_thread_pop_appdomain_ref ();
					InterlockedDecrement (&tp->busy_threads);
					continue;
				}

				if (mono_domain_set (domain, FALSE)) {
					MonoObject *exc;

					if (tp_item_begin_func)
						tp_item_begin_func (tp_item_user_data);

					if (!is_io_task && ar->add_time > 0)
						process_idle_times (tp, ar->add_time);
					exc = mono_async_invoke (tp, ar);
					if (tp_item_end_func)
						tp_item_end_func (tp_item_user_data);
					if (exc)
						mono_internal_thread_unhandled_exception (exc);
					if (is_socket && tp->is_io) {
						MonoSocketAsyncResult *state = (MonoSocketAsyncResult *) data;

						if (state->completed && state->callback) {
							MonoAsyncResult *cb_ares;
							cb_ares = create_simple_asyncresult ((MonoObject *) state->callback,
												(MonoObject *) state);
							icall_append_job ((MonoObject *) cb_ares);
						}
					}
					mono_domain_set (mono_get_root_domain (), TRUE);
				}
				mono_thread_pop_appdomain_ref ();
				InterlockedDecrement (&tp->busy_threads);
				/* If the callee changes the background status, set it back to TRUE */
				mono_thread_clr_state (thread , ~ThreadState_Background);
				if (!mono_thread_test_state (thread , ThreadState_Background))
					ves_icall_System_Threading_Thread_SetState (thread, ThreadState_Background);
			}
		}

		ar = NULL;
		data = NULL;
		must_die = should_i_die (tp);
		if (!must_die && (tp->is_io || !mono_wsq_local_pop (&data)))
			dequeue_or_steal (tp, &data, wsq);

		n_naps = 0;
		while (!must_die && !data && n_naps < 4) {
			gboolean res;

			InterlockedIncrement (&tp->waiting);

			// Another thread may have added a job into its wsq since the last call to dequeue_or_steal
			// Check all the queues again before entering the wait loop
			dequeue_or_steal (tp, &data, wsq);
			if (data) {
				InterlockedDecrement (&tp->waiting);
				break;
			}

			mono_gc_set_skip_thread (TRUE);

#if defined(__OpenBSD__)
			while (mono_cq_count (tp->queue) == 0 && (res = mono_sem_wait (&tp->new_job, TRUE)) == -1) {// && errno == EINTR) {
#else
			while (mono_cq_count (tp->queue) == 0 && (res = mono_sem_timedwait (&tp->new_job, 2000, TRUE)) == -1) {// && errno == EINTR) {
#endif
				if (mono_runtime_is_shutting_down ())
					break;
				if (THREAD_WANTS_A_BREAK (thread))
					mono_thread_interruption_checkpoint ();
			}
			InterlockedDecrement (&tp->waiting);

			mono_gc_set_skip_thread (FALSE);

			if (mono_runtime_is_shutting_down ())
				break;
			must_die = should_i_die (tp);
			dequeue_or_steal (tp, &data, wsq);
			n_naps++;
		}

		if (!data && !tp->is_io && !mono_runtime_is_shutting_down ()) {
			mono_wsq_local_pop (&data);
			if (data && must_die) {
				InterlockedCompareExchange (&tp->destroy_thread, 1, 0);
				pulse_on_new_job (tp);
			}
		}

		if (!data) {
			gint nt;
			gboolean down;
			while (1) {
				nt = tp->nthreads;
				down = mono_runtime_is_shutting_down ();
				if (!down && nt <= tp->min_threads)
					break;
				if (down || InterlockedCompareExchange (&tp->nthreads, nt - 1, nt) == nt) {
					mono_perfcounter_update_value (tp->pc_nthreads, TRUE, -1);
					if (!tp->is_io) {
						remove_wsq (wsq);
					}

					mono_profiler_thread_end (thread->tid);

					if (tp_finish_func)
						tp_finish_func (tp_hooks_user_data);
					return;
				}
			}
		}
	}

	g_assert_not_reached ();
}

void
ves_icall_System_Threading_ThreadPool_GetAvailableThreads (gint *workerThreads, gint *completionPortThreads)
{
	*workerThreads = async_tp.max_threads - async_tp.busy_threads;
	*completionPortThreads = async_io_tp.max_threads - async_io_tp.busy_threads;
}

void
ves_icall_System_Threading_ThreadPool_GetMaxThreads (gint *workerThreads, gint *completionPortThreads)
{
	*workerThreads = async_tp.max_threads;
	*completionPortThreads = async_io_tp.max_threads;
}

void
ves_icall_System_Threading_ThreadPool_GetMinThreads (gint *workerThreads, gint *completionPortThreads)
{
	*workerThreads = async_tp.min_threads;
	*completionPortThreads = async_io_tp.min_threads;
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMinThreads (gint workerThreads, gint completionPortThreads)
{
	gint max_threads;
	gint max_io_threads;

	max_threads = async_tp.max_threads;
	if (workerThreads <= 0 || workerThreads > max_threads)
		return FALSE;

	max_io_threads = async_io_tp.max_threads;
	if (completionPortThreads <= 0 || completionPortThreads > max_io_threads)
		return FALSE;

	InterlockedExchange (&async_tp.min_threads, workerThreads);
	InterlockedExchange (&async_io_tp.min_threads, completionPortThreads);
	if (workerThreads > async_tp.nthreads)
		mono_thread_create_internal (mono_get_root_domain (), threadpool_start_idle_threads, &async_tp, TRUE, SMALL_STACK);
	if (completionPortThreads > async_io_tp.nthreads)
		mono_thread_create_internal (mono_get_root_domain (), threadpool_start_idle_threads, &async_io_tp, TRUE, SMALL_STACK);
	return TRUE;
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMaxThreads (gint workerThreads, gint completionPortThreads)
{
	gint min_threads;
	gint min_io_threads;
	gint cpu_count;

	cpu_count = mono_cpu_count ();
	min_threads = async_tp.min_threads;
	if (workerThreads < min_threads || workerThreads < cpu_count)
		return FALSE;

	/* We don't really have the concept of completion ports. Do we care here? */
	min_io_threads = async_io_tp.min_threads;
	if (completionPortThreads < min_io_threads || completionPortThreads < cpu_count)
		return FALSE;

	InterlockedExchange (&async_tp.max_threads, workerThreads);
	InterlockedExchange (&async_io_tp.max_threads, completionPortThreads);
	return TRUE;
}

/**
 * mono_install_threadpool_thread_hooks
 * @start_func: the function to be called right after a new threadpool thread is created. Can be NULL.
 * @finish_func: the function to be called right before a thredpool thread is exiting. Can be NULL.
 * @user_data: argument passed to @start_func and @finish_func.
 *
 * @start_fun will be called right after a threadpool thread is created and @finish_func right before a threadpool thread exits.
 * The calls will be made from the thread itself.
 */
void
mono_install_threadpool_thread_hooks (MonoThreadPoolFunc start_func, MonoThreadPoolFunc finish_func, gpointer user_data)
{
	tp_start_func = start_func;
	tp_finish_func = finish_func;
	tp_hooks_user_data = user_data;
}

/**
 * mono_install_threadpool_item_hooks
 * @begin_func: the function to be called before a threadpool work item processing starts.
 * @end_func: the function to be called after a threadpool work item is finished.
 * @user_data: argument passed to @begin_func and @end_func.
 *
 * The calls will be made from the thread itself and from the same AppDomain
 * where the work item was executed.
 *
 */
void
mono_install_threadpool_item_hooks (MonoThreadPoolItemFunc begin_func, MonoThreadPoolItemFunc end_func, gpointer user_data)
{
	tp_item_begin_func = begin_func;
	tp_item_end_func = end_func;
	tp_item_user_data = user_data;
}

void
mono_internal_thread_unhandled_exception (MonoObject* exc)
{
	if (mono_runtime_unhandled_exception_policy_get () == MONO_UNHANDLED_POLICY_CURRENT) {
		gboolean unloaded;
		MonoClass *klass;

		klass = exc->vtable->klass;
		unloaded = is_appdomainunloaded_exception (exc->vtable->domain, klass);
		if (!unloaded && klass != mono_defaults.threadabortexception_class) {
			mono_unhandled_exception (exc);
			if (mono_environment_exitcode_get () == 1)
				exit (255);
		}
		if (klass == mono_defaults.threadabortexception_class)
		 mono_thread_internal_reset_abort (mono_thread_internal_current ());
	}
}
