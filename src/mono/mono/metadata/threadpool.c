/*
 * threadpool.c: global thread pool
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
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
int mono_worker_threads = 0;

/* current number of busy threads */
static int busy_worker_threads = 0;

/* we use this to store a reference to the AsyncResult to avoid GC */
static MonoGHashTable *ares_htable = NULL;

typedef struct {
	MonoMethodMessage *msg;
	HANDLE             wait_semaphore;
	MonoMethod        *cb_method;
	MonoDelegate      *cb_target;
	MonoObject        *state;
	MonoObject        *res;
	MonoArray         *out_args;
} ASyncCall;

static void async_invoke_thread (void);

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
	gboolean do_create = FALSE;
	int busy;

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

	EnterCriticalSection (&mono_delegate_section);	
	if (!ares_htable)
		ares_htable = mono_g_hash_table_new (NULL, NULL);

	mono_g_hash_table_insert (ares_htable, ares, ares);

	async_call_queue = g_list_append (async_call_queue, ares); 

	busy = (int) InterlockedCompareExchange (&busy_worker_threads, 0, -1);
	if (mono_worker_threads <= ++busy &&
	    mono_worker_threads < mono_max_worker_threads) {
		mono_worker_threads++;
		do_create = TRUE;
	}
	LeaveCriticalSection (&mono_delegate_section);

	if (do_create)
		mono_thread_create (domain, async_invoke_thread, NULL);

	return ares;
}

MonoObject *
mono_thread_pool_finish (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc)
{
	ASyncCall *ac;

	*exc = NULL;
	*out_args = NULL;

	EnterCriticalSection (&mono_delegate_section);	
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
	LeaveCriticalSection (&mono_delegate_section);

	/* wait until we are really finished */
	WaitForSingleObject (ac->wait_semaphore, INFINITE);

	*exc = ac->msg->exc;
	*out_args = ac->out_args;

	return ac->res;
}

static void
async_invoke_thread ()
{
	MonoDomain *domain;
	MonoThread *thread;
 
	thread = mono_thread_current ();
	thread->threadpool_thread = TRUE;
	thread->state |= ThreadState_Background;
	for (;;) {
		gboolean cont;
		MonoAsyncResult *ar;

		ar = NULL;
		InterlockedIncrement (&busy_worker_threads);
		EnterCriticalSection (&mono_delegate_section);
		
		if (async_call_queue) {
			GList *tmp;

			ar = (MonoAsyncResult *)async_call_queue->data;
			tmp = async_call_queue;
			async_call_queue = g_list_remove_link (async_call_queue, async_call_queue); 
			g_list_free_1 (tmp);
		}

		LeaveCriticalSection (&mono_delegate_section);

		if (ar) {
			/* worker threads invokes methods in different domains,
			 * so we need to set the right domain here */
			domain = ((MonoObject *)ar)->vtable->domain;
			mono_domain_set (domain);

			mono_async_invoke (ar);
		}

		InterlockedDecrement (&busy_worker_threads);

		EnterCriticalSection (&mono_delegate_section);
		cont = (async_call_queue != NULL);
		LeaveCriticalSection (&mono_delegate_section);
		if (cont)
			continue;

		Sleep (500);
		EnterCriticalSection (&mono_delegate_section);
		if (!async_call_queue) {
			mono_worker_threads--;
			LeaveCriticalSection (&mono_delegate_section);
			ExitThread (0);
		}
		LeaveCriticalSection (&mono_delegate_section);
	}

	g_assert_not_reached ();
}

