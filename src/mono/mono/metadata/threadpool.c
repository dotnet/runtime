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
#include <unistd.h>
#include <string.h>

#include <mono/utils/mono-poll.h>

#include "threadpool.h"

/* maximum number of worker threads */
int mono_max_worker_threads = THREADS_PER_CPU;
static int mono_min_worker_threads = 0;

/* current number of worker threads */
static int mono_worker_threads = 0;

/* current number of busy threads */
static int busy_worker_threads = 0;

/* mono_thread_pool_init called */
static int tp_inited;

/* we use this to store a reference to the AsyncResult to avoid GC */
static MonoGHashTable *ares_htable = NULL;

static CRITICAL_SECTION ares_lock;

typedef struct {
	CRITICAL_SECTION io_lock; /* access to sock_to_state */
	int inited;
	int pipe [2];
	GHashTable *sock_to_state;

	HANDLE new_sem; /* access to newpfd and write side of the pipe */
	mono_pollfd *newpfd;
} SocketIOData;

static SocketIOData socket_io_data;

/* we append a job */
static HANDLE job_added;

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
static void append_job (MonoAsyncResult *ar);
static void start_thread_or_queue (MonoAsyncResult *ares);

static GList *async_call_queue = NULL;

static MonoClass *socket_async_call_klass;

#define INIT_POLLFD(a, b, c) {(a)->fd = b; (a)->events = c; (a)->revents = 0;}
enum {
	AIO_FIRST,
	AIO_ACCEPT = 0,
	AIO_CONNECT,
	AIO_RECEIVE,
	AIO_RECEIVEFROM,
	AIO_SEND,
	AIO_SENDTO,
	AIO_LAST
};

static void
socket_io_cleanup (SocketIOData *data)
{
	if (data->inited == 0)
		return;

	EnterCriticalSection (&data->io_lock);
	data->inited = 0;
	close (data->pipe [0]);
	data->pipe [0] = -1;
	close (data->pipe [1]);
	data->pipe [1] = -1;
	CloseHandle (data->new_sem);
	data->new_sem = NULL;
	g_hash_table_destroy (data->sock_to_state);
	data->sock_to_state = NULL;
	LeaveCriticalSection (&data->io_lock);
}

static int
get_event_from_state (MonoSocketAsyncResult *state)
{
	switch (state->operation) {
	case AIO_ACCEPT:
	case AIO_RECEIVE:
	case AIO_RECEIVEFROM:
		return MONO_POLLIN;
	case AIO_SEND:
	case AIO_SENDTO:
	case AIO_CONNECT:
		return MONO_POLLOUT;
	default: /* Should never happen */
		g_print ("socket_io_add: unknown value in switch!!!\n");
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
		start_thread_or_queue (state->ares);
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
socket_io_main (gpointer p)
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
				if ((thread->state & ThreadState_StopRequested) != 0)
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

			read (data->pipe [0], one, 1);
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

static void
socket_io_init (SocketIOData *data)
{
	if (pipe (data->pipe) != 0) {
		int err = errno;
		perror ("mono");
		g_assert (err);
	}

	data->sock_to_state = g_hash_table_new (g_direct_hash, g_direct_equal);
	data->new_sem = CreateSemaphore (NULL, 1, 1, NULL);
	mono_thread_create (mono_get_root_domain (), socket_io_main, data);
}

static void
socket_io_add (MonoAsyncResult *ares, MonoSocketAsyncResult *state)
{
	int events;
	char msg [1];
	GSList *list;
	SocketIOData *data = &socket_io_data;

	state->ares = ares;
	if (InterlockedCompareExchange (&data->inited, -1, -1) == 0) {
		EnterCriticalSection (&data->io_lock);
		if (0 == data->inited) {
			socket_io_init (data);
			data->inited = 1;
		}
		LeaveCriticalSection (&data->io_lock);
	}

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
	write (data->pipe [1], msg, 1);
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
	if (op < AIO_FIRST || op >= AIO_LAST)
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
		append_job (ares);
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
	release = (gint) InterlockedCompareExchange (&busy_worker_threads, 0, -1);
	LeaveCriticalSection (&mono_delegate_section);
	if (job_added)
		ReleaseSemaphore (job_added, release, NULL);

	socket_io_cleanup (&socket_io_data);
}

static void
append_job (MonoAsyncResult *ar)
{
	GList *tmp;

	EnterCriticalSection (&mono_delegate_section);
	if (async_call_queue == NULL) {
		async_call_queue = g_list_append (async_call_queue, ar); 
	} else {
		for (tmp = async_call_queue; tmp && tmp->data != NULL; tmp = tmp->next);
		if (tmp == NULL) {
			async_call_queue = g_list_append (async_call_queue, ar); 
		} else {
			tmp->data = ar;
		}
	}
	LeaveCriticalSection (&mono_delegate_section);
}

static MonoAsyncResult *
dequeue_job (void)
{
	MonoAsyncResult *ar = NULL;
	GList *tmp, *tmp2;

	EnterCriticalSection (&mono_delegate_section);
	tmp = async_call_queue;
	if (tmp) {
		ar = (MonoAsyncResult *) tmp->data;
		tmp->data = NULL;
		tmp2 = tmp;
		for (tmp2 = tmp; tmp2->next != NULL; tmp2 = tmp2->next);
		if (tmp2 != tmp) {
			async_call_queue = tmp->next;
			tmp->next = NULL;
			tmp2->next = tmp;
			tmp->prev = tmp2;
		}
	}
	LeaveCriticalSection (&mono_delegate_section);

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

		data = dequeue_job ();
	
		if (!data) {
			guint32 wr;
			int timeout = 10000;
			guint32 start_time = GetTickCount ();
			
			do {
				wr = WaitForSingleObjectEx (job_added, (guint32)timeout, TRUE);
				if ((thread->state & ThreadState_StopRequested)!=0)
					mono_thread_interruption_checkpoint ();
			
				timeout -= GetTickCount () - start_time;
			
				if (wr != WAIT_TIMEOUT)
					data = dequeue_job ();
			}
			while (!data && timeout > 0);
		}

		if (!data) {
			workers = (int) InterlockedCompareExchange (&mono_worker_threads, 0, -1); 
			min = (int) InterlockedCompareExchange (&mono_min_worker_threads, 0, -1); 
	
			while (!data && workers <= min) {
				WaitForSingleObjectEx (job_added, INFINITE, TRUE);
				if ((thread->state & ThreadState_StopRequested)!=0)
					mono_thread_interruption_checkpoint ();
			
				data = dequeue_job ();
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

static void
overlapped_callback (guint32 error, guint32 numbytes, WapiOverlapped *overlapped)
{
	MonoFSAsyncResult *ares;
	MonoThread *thread;
 
	MONO_ARCH_SAVE_REGS;

	ares = (MonoFSAsyncResult *) overlapped->handle1;
	ares->completed = TRUE;
	if (ares->bytes_read != -1)
		ares->bytes_read = numbytes;
	else
		ares->count = numbytes;

	thread = mono_thread_attach (mono_object_domain (ares));
	if (ares->async_callback != NULL) {
		gpointer p [1];

		*p = ares;
		mono_runtime_invoke (ares->async_callback->method_info->method, NULL, p, NULL);
	}

	SetEvent (ares->wait_handle->handle);
	mono_thread_detach (thread);
	g_free (overlapped);
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_BindHandle (gpointer handle)
{
	MONO_ARCH_SAVE_REGS;

#ifdef PLATFORM_WIN32
	return FALSE;
#else
	if (!BindIoCompletionCallback (handle, overlapped_callback, 0)) {
		gint error = GetLastError ();
		MonoException *exc;
		gchar *msg;

		if (error == ERROR_INVALID_PARAMETER) {
			exc = mono_get_exception_argument (NULL, "Invalid parameter.");
		} else {
			msg = g_strdup_printf ("Win32 error %d.", error);
			exc = mono_exception_from_name_msg (mono_defaults.corlib,
							    "System",
							    "ApplicationException", msg);
			g_free (msg);
		}

		mono_raise_exception (exc);
	}

	return TRUE;
#endif
}

