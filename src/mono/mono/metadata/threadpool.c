/*
 * threadpool.c: global thread pool
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/exception.h>
#include <mono/io-layer/io-layer.h>
#include <mono/os/gc_wrapper.h>

#include "threadpool.h"

/* maximum number of worker threads */
int mono_max_worker_threads = 25; /* fixme: should be 25 per available CPU */
/* current number of worker threads */
static int mono_worker_threads = 0;

/* current number of busy threads */
int busy_worker_threads = 0;

/* we use this to store a reference to the AsyncResult to avoid GC */
static MonoGHashTable *ares_htable = NULL;

/* we append a job */
static HANDLE job_added;

typedef struct {
	MonoMethodMessage *msg;
	HANDLE             wait_semaphore;
	MonoMethod        *cb_method;
	MonoDelegate      *cb_target;
	MonoObject        *state;
	MonoObject        *res;
	MonoArray         *out_args;
} ASyncCall;

static void async_invoke_thread (gpointer data);
static void append_job (MonoAsyncResult *ar);

static GList *async_call_queue = NULL;

static void
mono_async_invoke (MonoAsyncResult *ares)
{
	ASyncCall *ac = (ASyncCall *)ares->data;

	ac->msg->exc = NULL;
	ac->res = mono_message_invoke (ares->async_delegate, ac->msg, 
				       &ac->msg->exc, &ac->out_args);

	ares->completed = 1;

	/* notify listeners */
	ReleaseSemaphore (ac->wait_semaphore, 0x7fffffff, NULL);
		
	/* call async callback if cb_method != null*/
	if (ac->cb_method) {
		MonoObject *exc = NULL;
		void *pa = &ares;
		mono_runtime_invoke (ac->cb_method, ac->cb_target, pa, &exc);
		if (!ac->msg->exc)
			ac->msg->exc = exc;
	}

	mono_g_hash_table_remove (ares_htable, ares);
}

MonoAsyncResult *
mono_thread_pool_add (MonoObject *target, MonoMethodMessage *msg, MonoDelegate *async_callback,
		      MonoObject *state)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	ASyncCall *ac;
	int busy, worker;

#ifdef HAVE_BOEHM_GC
	ac = GC_MALLOC (sizeof (ASyncCall));
#else
	/* We'll leak the semaphore... */
	ac = g_new0 (ASyncCall, 1);
#endif
	ac->wait_semaphore = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);	
	ac->msg = msg;
	ac->state = state;

	if (async_callback) {
		ac->cb_method = mono_get_delegate_invoke (((MonoObject *)async_callback)->vtable->klass);
		ac->cb_target = async_callback;
	}

	ares = mono_async_result_new (domain, ac->wait_semaphore, ac->state, ac);
	ares->async_delegate = target;

	if (!ares_htable) {
		ares_htable = mono_g_hash_table_new (NULL, NULL);
		job_added = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
	}

	mono_g_hash_table_insert (ares_htable, ares, ares);

	busy = (int) InterlockedCompareExchange (&busy_worker_threads, 0, -1);
	worker = (int) InterlockedCompareExchange (&mono_worker_threads, 0, -1); 
	if (worker <= ++busy &&
	    worker < mono_max_worker_threads) {
		InterlockedIncrement (&mono_worker_threads);
		InterlockedIncrement (&busy_worker_threads);
		mono_thread_create (domain, async_invoke_thread, ares);
	} else {
		append_job (ares);
		ReleaseSemaphore (job_added, 1, NULL);
	}

	return ares;
}

MonoObject *
mono_thread_pool_finish (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc)
{
	ASyncCall *ac;

	*exc = NULL;
	*out_args = NULL;

	/* check if already finished */
	if (ares->endinvoke_called) {
		*exc = (MonoObject *)mono_exception_from_name (mono_defaults.corlib, "System", 
					      "InvalidOperationException");
		LeaveCriticalSection (&mono_delegate_section);
		return NULL;
	}

	ares->endinvoke_called = 1;
	ac = (ASyncCall *)ares->data;

	g_assert (ac != NULL);

	/* wait until we are really finished */
	WaitForSingleObject (ac->wait_semaphore, INFINITE);

	*exc = ac->msg->exc;
	*out_args = ac->out_args;

	return ac->res;
}

static void
append_job (MonoAsyncResult *ar)
{
	EnterCriticalSection (&mono_delegate_section);
	async_call_queue = g_list_append (async_call_queue, ar); 
	LeaveCriticalSection (&mono_delegate_section);
}

static MonoAsyncResult *
dequeue_job (void)
{
	MonoAsyncResult *ar = NULL;
	GList *tmp = NULL;

	EnterCriticalSection (&mono_delegate_section);
	if (async_call_queue) {
		ar = (MonoAsyncResult *)async_call_queue->data;
		tmp = async_call_queue;
		async_call_queue = g_list_remove_link (tmp, tmp); 
	}
	LeaveCriticalSection (&mono_delegate_section);
	if (tmp)
		g_list_free_1 (tmp);

	return ar;
}

static void
async_invoke_thread (gpointer data)
{
	MonoDomain *domain;
	MonoThread *thread;
 
	thread = mono_thread_current ();
	thread->threadpool_thread = TRUE;
	thread->state |= ThreadState_Background;
	for (;;) {
		gboolean cont;
		MonoAsyncResult *ar;

		ar = (MonoAsyncResult *) data;
		if (ar) {
			/* worker threads invokes methods in different domains,
			 * so we need to set the right domain here */
			domain = ((MonoObject *)ar)->vtable->domain;
			mono_domain_set (domain);

			mono_async_invoke (ar);
			InterlockedDecrement (&busy_worker_threads);
		}

		data = dequeue_job ();
		if (!data && WaitForSingleObject (job_added, 500) != WAIT_TIMEOUT)
			data = dequeue_job ();

		if (!data) {
			InterlockedDecrement (&mono_worker_threads);
			ExitThread (0);
		}
		InterlockedIncrement (&busy_worker_threads);
	}

	g_assert_not_reached ();
}

