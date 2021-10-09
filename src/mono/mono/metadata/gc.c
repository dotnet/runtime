/**
 * \file
 * GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/sgen/sgen-conf.h>
#include <mono/sgen/sgen-gc.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/metadata/marshal.h> /* for mono_delegate_free_ftnptr () */
#include <mono/utils/mono-os-semaphore.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/w32api.h>
#include <mono/utils/unlocked.h>
#include <mono/utils/mono-os-wait.h>
#include <mono/utils/mono-lazy-init.h>
#ifndef HOST_WIN32
#include <pthread.h>
#endif
#include "external-only.h"
#include "icall-decl.h"

#if _MSC_VER
#pragma warning(disable:4312) // FIXME pointer cast to different size
#endif

typedef struct DomainFinalizationReq {
	gint32 ref;
	MonoDomain *domain;
	MonoCoopSem done;
} DomainFinalizationReq;

static gboolean gc_disabled;

static gboolean finalizing_root_domain;

gboolean mono_log_finalizers;
gboolean mono_do_not_finalize;
static volatile gboolean suspend_finalizers;
gchar **mono_do_not_finalize_class_names ;

#define mono_finalizer_lock() mono_coop_mutex_lock (&finalizer_mutex)
#define mono_finalizer_unlock() mono_coop_mutex_unlock (&finalizer_mutex)
static MonoCoopMutex finalizer_mutex;
static MonoCoopMutex reference_queue_mutex;
static mono_lazy_init_t reference_queue_mutex_inited = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static GSList *domains_to_finalize;

static gboolean finalizer_thread_exited;
/* Uses finalizer_mutex */
static MonoCoopCond exited_cond;

static MonoInternalThread *gc_thread;
#ifndef HOST_WASM
static RuntimeInvokeFunction finalize_runtime_invoke;
#endif

/*
 * This must be a GHashTable, since these objects can't be finalized
 * if the hashtable contains a GC visible reference to them.
 */
static GHashTable *finalizable_objects_hash;
static mono_mutex_t finalizable_objects_hash_lock;

#ifdef TARGET_WIN32
static HANDLE pending_done_event;
#else
static gboolean pending_done;
static MonoCoopCond pending_done_cond;
static MonoCoopMutex pending_done_mutex;
#endif

#define finalizers_lock() mono_os_mutex_lock (&finalizable_objects_hash_lock);
#define finalizers_unlock() mono_os_mutex_unlock (&finalizable_objects_hash_lock);

static void object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*));

static void reference_queue_proccess_all (void);
static void mono_reference_queue_cleanup (void);
static void reference_queue_clear_for_domain (MonoDomain *domain);
static void mono_runtime_do_background_work (void);

static MonoThreadInfoWaitRet
guarded_wait (MonoThreadHandle *thread_handle, guint32 timeout, gboolean alertable)
{
	MonoThreadInfoWaitRet result;

	MONO_ENTER_GC_SAFE;
	result = mono_thread_info_wait_one_handle (thread_handle, timeout, alertable);
	MONO_EXIT_GC_SAFE;

	return result;
}

typedef struct {
	MonoCoopCond *cond;
	MonoCoopMutex *mutex;
} BreakCoopAlertableWaitUD;

static void
break_coop_alertable_wait (gpointer user_data)
{
	BreakCoopAlertableWaitUD *ud = (BreakCoopAlertableWaitUD*)user_data;

	mono_coop_mutex_lock (ud->mutex);
	mono_coop_cond_signal (ud->cond);
	mono_coop_mutex_unlock (ud->mutex);

	g_free (ud);
}

/*
 * coop_cond_timedwait_alertable:
 *
 *   Wait on COND/MUTEX. If ALERTABLE is non-null, the wait can be interrupted.
 * In that case, *ALERTABLE will be set to TRUE, and 0 is returned.
 */
static gint
coop_cond_timedwait_alertable (MonoCoopCond *cond, MonoCoopMutex *mutex, guint32 timeout_ms, gboolean *alertable)
{
	BreakCoopAlertableWaitUD *ud;
	int res;

	if (alertable) {
		ud = g_new0 (BreakCoopAlertableWaitUD, 1);
		ud->cond = cond;
		ud->mutex = mutex;

		mono_thread_info_install_interrupt (break_coop_alertable_wait, ud, alertable);
		if (*alertable) {
			g_free (ud);
			return 0;
		}
	}
	res = mono_coop_cond_timedwait (cond, mutex, timeout_ms);
	if (alertable) {
		mono_thread_info_uninstall_interrupt (alertable);
		if (*alertable)
			return 0;
		else {
			/* the interrupt token has not been taken by another
			 * thread, so it's our responsability to free it up. */
			g_free (ud);
		}
	}
	return res;
}

/* 
 * actually, we might want to queue the finalize requests in a separate thread,
 * but we need to be careful about the execution domain of the thread...
 */
void
mono_gc_run_finalize (void *obj, void *data)
{
	ERROR_DECL (error);
	MonoObject *exc = NULL;
	MonoObject *o;
#ifndef HAVE_SGEN_GC
	MonoObject *o2;
#endif
	MonoMethod* finalizer = NULL;
	MonoDomain *caller_domain = mono_domain_get ();
	MonoDomain *domain;

	// This function is called from the innards of the GC, so our best alternative for now is to do polling here
	mono_threads_safepoint ();

	o = (MonoObject*)((char*)obj + GPOINTER_TO_UINT (data));

	const char *o_ns = m_class_get_name_space (mono_object_class (o));
	const char *o_name = m_class_get_name (mono_object_class (o));

	if (mono_do_not_finalize) {
		if (!mono_do_not_finalize_class_names)
			return;

		size_t namespace_len = strlen (o_ns);
		for (int i = 0; mono_do_not_finalize_class_names [i]; ++i) {
			const char *name = mono_do_not_finalize_class_names [i];
			if (strncmp (name, o_ns, namespace_len))
				break;
			if (name [namespace_len] != '.')
				break;
			if (strcmp (name + namespace_len + 1, o_name))
				break;
			return;
		}
	}

	if (mono_log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_DEBUG, "<%s at %p> Starting finalizer checks.", o_name, o);

	if (suspend_finalizers)
		return;

	domain = o->vtable->domain;

#ifndef HAVE_SGEN_GC
	finalizers_lock ();

	o2 = (MonoObject *)g_hash_table_lookup (finalizable_objects_hash, o);

	finalizers_unlock ();

	if (!o2)
		/* Already finalized somehow */
		return;
#endif

	/* make sure the finalizer is not called again if the object is resurrected */
	object_register_finalizer ((MonoObject *)obj, NULL);

	if (mono_log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Registered finalizer as processed.", o_name, o);

	if (o->vtable->klass == mono_defaults.internal_thread_class) {
		MonoInternalThread *t = (MonoInternalThread*)o;

		if (mono_gc_is_finalizer_internal_thread (t))
			/* Avoid finalizing ourselves */
			return;
	}

	if (m_class_get_image (mono_object_class (o)) == mono_defaults.corlib && !strcmp (o_name, "DynamicMethod") && finalizing_root_domain) {
		/*
		 * These can't be finalized during unloading/shutdown, since that would
		 * free the native code which can still be referenced by other
		 * finalizers.
		 * FIXME: This is not perfect, objects dying at the same time as 
		 * dynamic methods can still reference them even when !shutdown.
		 */
		return;
	}

	if (mono_runtime_get_no_exec ())
		return;

	/* speedup later... and use a timeout */
	/* g_print ("Finalize run on %p %s.%s\n", o, mono_object_class (o)->name_space, mono_object_class (o)->name); */

	/* Use _internal here, since this thread can enter a doomed appdomain */
	mono_domain_set_internal_with_options (mono_object_domain (o), TRUE);

	/* delegates that have a native function pointer allocated are
	 * registered for finalization, but they don't have a Finalize
	 * method, because in most cases it's not needed and it's just a waste.
	 */
	if (m_class_is_delegate (mono_object_class (o))) {
		MonoDelegate* del = (MonoDelegate*)o;
		if (del->delegate_trampoline)
			mono_delegate_free_ftnptr ((MonoDelegate*)o);
		mono_domain_set_internal_with_options (caller_domain, TRUE);
		return;
	}

	finalizer = mono_class_get_finalizer (o->vtable->klass);

	/* If object has a CCW but has no finalizer, it was only
	 * registered for finalization in order to free the CCW.
	 * Else it needs the regular finalizer run.
	 * FIXME: what to do about ressurection and suppression
	 * of finalizer on object with CCW.
	 */
	if (mono_marshal_free_ccw (o) && !finalizer) {
		mono_domain_set_internal_with_options (caller_domain, TRUE);
		return;
	}

	/* 
	 * To avoid the locking plus the other overhead of mono_runtime_invoke_checked (),
	 * create and precompile a wrapper which calls the finalize method using
	 * a CALLVIRT.
	 */
	if (mono_log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Compiling finalizer.", o_name, o);

#ifndef HOST_WASM
	if (!finalize_runtime_invoke) {
		MonoMethod *finalize_method = mono_class_get_method_from_name_checked (mono_defaults.object_class, "Finalize", 0, 0, error);
		mono_error_assert_ok (error);
		MonoMethod *invoke = mono_marshal_get_runtime_invoke (finalize_method, TRUE);

		finalize_runtime_invoke = (RuntimeInvokeFunction)mono_compile_method_checked (invoke, error);
		mono_error_assert_ok (error); /* expect this not to fail */
	}

	RuntimeInvokeFunction runtime_invoke = finalize_runtime_invoke;
#endif

	mono_runtime_class_init_full (o->vtable, error);
	goto_if_nok (error, unhandled_error);

	if (G_UNLIKELY (MONO_GC_FINALIZE_INVOKE_ENABLED ())) {
		MONO_GC_FINALIZE_INVOKE ((unsigned long)o, mono_object_get_size_internal (o),
				o_ns, o_name);
	}

	if (mono_log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Calling finalizer.", o_name, o);

	MONO_PROFILER_RAISE (gc_finalizing_object, (o));

#ifdef HOST_WASM
	if (finalizer) { // null finalizers work fine when using the vcall invoke as Object has an empty one
		gpointer params [1];
		params [0] = NULL;
		mono_runtime_try_invoke (finalizer, o, params, &exc, error);
	}
#else
	runtime_invoke (o, NULL, &exc, NULL);
#endif

	MONO_PROFILER_RAISE (gc_finalized_object, (o));

	if (mono_log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Returned from finalizer.", o_name, o);

unhandled_error:
	if (!is_ok (error))
		exc = (MonoObject*)mono_error_convert_to_exception (error);
	if (exc)
		mono_thread_internal_unhandled_exception (exc);

	mono_domain_set_internal_with_options (caller_domain, TRUE);
}

/*
 * Some of our objects may point to a different address than the address returned by GC_malloc()
 * (because of the GetHashCode hack), but we need to pass the real address to register_finalizer.
 * This also means that in the callback we need to adjust the pointer to get back the real
 * MonoObject*.
 * We also need to be consistent in the use of the GC_debug* variants of malloc and register_finalizer, 
 * since that, too, can cause the underlying pointer to be offset.
 */
static void
object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*))
{
	g_assert (obj != NULL);

#if HAVE_BOEHM_GC
	finalizers_lock ();

	if (callback)
		g_hash_table_insert (finalizable_objects_hash, obj, obj);
	else
		g_hash_table_remove (finalizable_objects_hash, obj);

	finalizers_unlock ();

	mono_gc_register_for_finalization (obj, callback);
#elif defined(HAVE_SGEN_GC)
	mono_gc_register_for_finalization (obj, callback);
#endif
}

/**
 * mono_object_register_finalizer:
 * \param obj object to register
 *
 * Records that object \p obj has a finalizer, this will call the
 * Finalize method when the garbage collector disposes the object.
 * 
 */
void
mono_object_register_finalizer_handle (MonoObjectHandle obj)
{
	/* g_print ("Registered finalizer on %p %s.%s\n", obj, mono_handle_class (obj)->name_space, mono_handle_class (obj)->name); */
	object_register_finalizer (MONO_HANDLE_RAW (obj), mono_gc_run_finalize);
}

static void
mono_object_unregister_finalizer_handle (MonoObjectHandle obj)
{
	/* g_print ("Unregistered finalizer on %p %s.%s\n", obj, mono_handle_class (obj)->name_space, mono_handle_class (obj)->name); */
	object_register_finalizer (MONO_HANDLE_RAW (obj), NULL);
}

void
mono_object_register_finalizer (MonoObject *obj)
{
	/* g_print ("Registered finalizer on %p %s.%s\n", obj, mono_object_class (obj)->name_space, mono_object_class (obj)->name); */
	object_register_finalizer (obj, mono_gc_run_finalize);
}

/**
 * mono_domain_finalize:
 * \param domain the domain to finalize
 * \param timeout msecs to wait for the finalization to complete, \c -1 to wait indefinitely
 *
 * Request finalization of all finalizable objects inside \p domain. Wait
 * \p timeout msecs for the finalization to complete.
 *
 * \returns TRUE if succeeded, FALSE if there was a timeout
 */
gboolean
mono_domain_finalize (MonoDomain *domain, guint32 timeout) 
{
	DomainFinalizationReq *req;
	MonoInternalThread *thread = mono_thread_internal_current ();
	gint res;
	gboolean ret;
	gint64 start;

	if (mono_thread_internal_current () == gc_thread)
		/* We are called from inside a finalizer, not much we can do here */
		return FALSE;

	/* 
	 * No need to create another thread 'cause the finalizer thread
	 * is still working and will take care of running the finalizers
	 */ 
	
	if (gc_disabled)
		return TRUE;

	/* We don't support domain finalization without a GC */
	if (mono_gc_is_null ())
		return FALSE;

	mono_gc_collect (mono_gc_max_generation ());

	req = g_new0 (DomainFinalizationReq, 1);
	req->ref = 2;
	req->domain = domain;
	mono_coop_sem_init (&req->done, 0);

	if (domain == mono_get_root_domain ())
		finalizing_root_domain = TRUE;
	
	mono_finalizer_lock ();

	domains_to_finalize = g_slist_append (domains_to_finalize, req);

	mono_finalizer_unlock ();

	/* Tell the finalizer thread to finalize this appdomain */
	mono_gc_finalize_notify ();

	if (timeout == -1)
		timeout = MONO_INFINITE_WAIT;
	if (timeout != MONO_INFINITE_WAIT)
		start = mono_msec_ticks ();

	ret = TRUE;

	for (;;) {
		if (timeout == MONO_INFINITE_WAIT) {
			res = mono_coop_sem_wait (&req->done, MONO_SEM_FLAGS_ALERTABLE);
		} else {
			gint64 elapsed = mono_msec_ticks () - start;
			if (elapsed >= timeout) {
				ret = FALSE;
				break;
			}

			res = mono_coop_sem_timedwait (&req->done, timeout - elapsed, MONO_SEM_FLAGS_ALERTABLE);
		}

		if (res == MONO_SEM_TIMEDWAIT_RET_SUCCESS) {
			break;
		} else if (res == MONO_SEM_TIMEDWAIT_RET_ALERTED) {
			if ((thread->state & (ThreadState_AbortRequested | ThreadState_SuspendRequested)) != 0) {
				ret = FALSE;
				break;
			}
		} else if (res == MONO_SEM_TIMEDWAIT_RET_TIMEDOUT) {
			ret = FALSE;
			break;
		} else {
			g_error ("%s: unknown result %d", __func__, res);
		}
	}

	if (!ret) {
		/* Try removing the req from domains_to_finalize:
		 *  - if it's not found: the domain is being finalized,
		 *     so we the ref count is already decremented
		 *  - if it's found: the domain is not yet being finalized,
		 *     so we can safely decrement the ref */

		gboolean found;

		mono_finalizer_lock ();

		found = g_slist_index (domains_to_finalize, req) != -1;
		if (found)
			domains_to_finalize = g_slist_remove (domains_to_finalize, req);

		mono_finalizer_unlock ();

		if (found) {
			/* We have to decrement it wherever we
			 * remove it from domains_to_finalize */
			if (mono_atomic_dec_i32 (&req->ref) != 1)
				g_error ("%s: req->ref should be 1, as we are the first one to decrement it", __func__);
		}

		goto done;
	}

done:
	if (mono_atomic_dec_i32 (&req->ref) == 0) {
		mono_coop_sem_destroy (&req->done);
		g_free (req);
	}

	return ret;
}

void
ves_icall_System_GC_InternalCollect (int generation)
{
	mono_gc_collect (generation);
}

gint64
ves_icall_System_GC_GetTotalMemory (MonoBoolean forceCollection)
{
	if (forceCollection)
		mono_gc_collect (mono_gc_max_generation ());
	return mono_gc_get_used_size ();
}

void
ves_icall_System_GC_GetGCMemoryInfo (
	gint64 *high_memory_load_threshold_bytes,
	gint64 *memory_load_bytes,
	gint64 *total_available_memory_bytes,
	gint64 *total_committed_bytes,
	gint64 *heap_size_bytes,
	gint64 *fragmented_bytes)
{
	mono_gc_get_gcmemoryinfo (high_memory_load_threshold_bytes, memory_load_bytes, total_available_memory_bytes, total_committed_bytes, heap_size_bytes, fragmented_bytes);
}

void
ves_icall_System_GC_ReRegisterForFinalize (MonoObjectHandle obj, MonoError *error)
{
	MONO_CHECK_ARG_NULL_HANDLE (obj,);

	mono_object_register_finalizer_handle (obj);
}

void
ves_icall_System_GC_SuppressFinalize (MonoObjectHandle obj, MonoError *error)
{
	MONO_CHECK_ARG_NULL_HANDLE (obj,);

	/* delegates have no finalizers, but we register them to deal with the
	 * unmanaged->managed trampoline. We don't let the user suppress it
	 * otherwise we'd leak it.
	 */
	if (m_class_is_delegate (mono_handle_class (obj)))
		return;

	/* FIXME: Need to handle case where obj has COM Callable Wrapper
	 * generated for it that needs cleaned up, but user wants to suppress
	 * their derived object finalizer. */

	mono_object_unregister_finalizer_handle (obj);
}

void
ves_icall_System_GC_WaitForPendingFinalizers (void)
{
	if (mono_gc_is_null ())
		return;

	if (!mono_gc_pending_finalizers ())
		return;

	if (mono_thread_internal_current () == gc_thread)
		/* Avoid deadlocks */
		return;

	/*
	If the finalizer thread is not live, lets pretend no finalizers are pending since the current thread might
	be the one responsible for starting it up.
	*/
	if (gc_thread == NULL)
		return;

#ifdef TARGET_WIN32
	ResetEvent (pending_done_event);
	mono_gc_finalize_notify ();
	/* g_print ("Waiting for pending finalizers....\n"); */
	mono_coop_win32_wait_for_single_object_ex (pending_done_event, INFINITE, TRUE);
	/* g_print ("Done pending....\n"); */
#else
	gboolean alerted = FALSE;
	mono_coop_mutex_lock (&pending_done_mutex);
	pending_done = FALSE;
	mono_gc_finalize_notify ();
	while (!pending_done) {
		coop_cond_timedwait_alertable (&pending_done_cond, &pending_done_mutex, MONO_INFINITE_WAIT, &alerted);
		if (alerted)
			break;
	}
	mono_coop_mutex_unlock (&pending_done_mutex);
#endif
}

void
ves_icall_System_GC_register_ephemeron_array (MonoObjectHandle array, MonoError *error)
{
	if (!mono_gc_ephemeron_array_add (MONO_HANDLE_RAW (array)))
		mono_error_set_out_of_memory (error, "");
}

MonoObjectHandle
ves_icall_System_GC_get_ephemeron_tombstone (MonoError *error)
{
	return MONO_HANDLE_NEW (MonoObject, mono_domain_get ()->ephemeron_tombstone);
}

MonoGCHandle
ves_icall_System_GCHandle_InternalAlloc (MonoObjectHandle obj, gint32 type, MonoError *error)
{
	MonoGCHandle handle = NULL;

	switch (type) {
	case HANDLE_WEAK:
		handle = mono_gchandle_new_weakref_from_handle (obj);
		break;
	case HANDLE_WEAK_TRACK:
		handle = mono_gchandle_new_weakref_from_handle_track_resurrection (obj);
		break;
	case HANDLE_NORMAL:
		handle = mono_gchandle_from_handle (obj, FALSE);
		break;
	case HANDLE_PINNED:
		handle = mono_gchandle_from_handle (obj, TRUE);
		break;
	default:
		g_assert_not_reached ();
	}
	return handle;
}

void
ves_icall_System_GCHandle_InternalFree (MonoGCHandle handle, MonoError *error)
{
	mono_gchandle_free_internal (handle);
}

MonoObjectHandle
ves_icall_System_GCHandle_InternalGet (MonoGCHandle handle, MonoError *error)
{
	return mono_gchandle_get_target_handle (handle);
}

void
ves_icall_System_GCHandle_InternalSet (MonoGCHandle handle, MonoObjectHandle obj, MonoError *error)
{
	mono_gchandle_set_target_handle (handle, obj);
}

static MonoCoopSem finalizer_sem;
static volatile gboolean finished;

/*
 * mono_gc_finalize_notify:
 *
 *   Notify the finalizer thread that finalizers etc.
 * are available to be processed.
 * This is async signal safe.
 */
void
mono_gc_finalize_notify (void)
{
#ifdef DEBUG
	g_message ( "%s: prodding finalizer", __func__);
#endif

	if (mono_gc_is_null ())
		return;

#ifdef HOST_WASM
	mono_threads_schedule_background_job (mono_runtime_do_background_work);
#else
	mono_coop_sem_post (&finalizer_sem);
#endif
}

/*
This is the number of entries allowed in the hazard free queue before
we explicitly cycle the finalizer thread to trigger pumping the queue.

It was picked empirically by running the corlib test suite in a stress
scenario where all hazard entries are queued.

In this extreme scenario we double the number of times we cycle the finalizer
thread compared to just GC calls.

Entries are usually in the order of 100's of bytes each, so we're limiting
floating garbage to be in the order of a dozen kb.
*/
static gboolean finalizer_thread_pulsed;
#define HAZARD_QUEUE_OVERFLOW_SIZE 20

static void
hazard_free_queue_is_too_big (size_t size)
{
	if (size < HAZARD_QUEUE_OVERFLOW_SIZE)
		return;

	if (finalizer_thread_pulsed || mono_atomic_cas_i32 (&finalizer_thread_pulsed, TRUE, FALSE))
		return;

	mono_gc_finalize_notify ();
}

static void
hazard_free_queue_pump (void)
{
	mono_thread_hazardous_try_free_all ();
	finalizer_thread_pulsed = FALSE;
}

#ifdef HAVE_BOEHM_GC

static void
collect_objects (gpointer key, gpointer value, gpointer user_data)
{
	GPtrArray *arr = (GPtrArray*)user_data;
	g_ptr_array_add (arr, key);
}

#endif

/*
 * finalize_domain_objects:
 *
 *  Run the finalizers of all finalizable objects in req->domain.
 */
static void
finalize_domain_objects (void)
{
	DomainFinalizationReq *req = NULL;
	MonoDomain *domain;

	if (UnlockedReadPointer ((gpointer volatile*)&domains_to_finalize)) {
		mono_finalizer_lock ();
		if (domains_to_finalize) {
			req = (DomainFinalizationReq *)domains_to_finalize->data;
			domains_to_finalize = g_slist_remove (domains_to_finalize, req);
		}
		mono_finalizer_unlock ();
	}

	if (!req)
		return;

	domain = req->domain;

	/* Process finalizers which are already in the queue */
	mono_gc_invoke_finalizers ();

#ifdef HAVE_BOEHM_GC
	while (g_hash_table_size (finalizable_objects_hash) > 0) {
		int i;
		GPtrArray *objs;
		/* 
		 * Since the domain is unloading, nobody is allowed to put
		 * new entries into the hash table. But finalize_object might
		 * remove entries from the hash table, so we make a copy.
		 */
		objs = g_ptr_array_new ();
		g_hash_table_foreach (finalizable_objects_hash, collect_objects, objs);
		/* printf ("FINALIZING %d OBJECTS.\n", objs->len); */

		for (i = 0; i < objs->len; ++i) {
			MonoObject *o = (MonoObject*)g_ptr_array_index (objs, i);
			/* FIXME: Avoid finalizing threads, etc */
			mono_gc_run_finalize (o, 0);
		}

		g_ptr_array_free (objs, TRUE);
	}
#elif defined(HAVE_SGEN_GC)
	mono_gc_finalize_domain (domain);
	mono_gc_invoke_finalizers ();
#endif

	/* cleanup the reference queue */
	reference_queue_clear_for_domain (domain);
	
	/* printf ("DONE.\n"); */
	mono_coop_sem_post (&req->done);

	if (mono_atomic_dec_i32 (&req->ref) == 0) {
		/* mono_domain_finalize already returned, and
		 * doesn't hold a reference to req anymore. */
		mono_coop_sem_destroy (&req->done);
		g_free (req);
	}
}


static void
mono_runtime_do_background_work (void)
{
	mono_threads_perform_thread_dump ();

	mono_threads_exiting ();

	finalize_domain_objects ();

	MONO_PROFILER_RAISE (gc_finalizing, ());

	/* If finished == TRUE, mono_gc_cleanup has been called (from mono_runtime_cleanup),
	 * before the domain is unloaded.
	 */
	mono_gc_invoke_finalizers ();

	MONO_PROFILER_RAISE (gc_finalized, ());

	mono_threads_join_threads ();

	reference_queue_proccess_all ();

	hazard_free_queue_pump ();
}

static gsize WINAPI
finalizer_thread (gpointer unused)
{
	gboolean wait = TRUE;
	gboolean did_init_from_native = FALSE;

	mono_thread_set_name_constant_ignore_error (mono_thread_internal_current (), "Finalizer", MonoSetThreadNameFlag_None);

	/* Register a hazard free queue pump callback */
	mono_hazard_pointer_install_free_queue_size_callback (hazard_free_queue_is_too_big);

	while (!finished) {
		/* Wait to be notified that there's at least one
		 * finaliser to run
		 */

		g_assert (mono_domain_get () == mono_get_root_domain ());
		mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NO_GC);

		if (wait) {
			/* An alertable wait is required so this thread can be suspended on windows */
			mono_coop_sem_wait (&finalizer_sem, MONO_SEM_FLAGS_ALERTABLE);
		}
		wait = TRUE;

		mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NONE);

		/* The Finalizer thread doesn't initialize during creation because base managed
			libraries may not be loaded yet. However, the first time the Finalizer is
			to run managed finalizer, we can take this opportunity to initialize. */
		if (!did_init_from_native) {
			did_init_from_native = TRUE;
			mono_thread_init_from_native ();
		}

		mono_runtime_do_background_work ();

		/* Avoid posting the pending done event until there are pending finalizers */
		if (mono_coop_sem_timedwait (&finalizer_sem, 0, MONO_SEM_FLAGS_NONE) == MONO_SEM_TIMEDWAIT_RET_SUCCESS) {
			/* Don't wait again at the start of the loop */
			wait = FALSE;
		} else {
#ifdef TARGET_WIN32
			SetEvent (pending_done_event);
#else
			mono_coop_mutex_lock (&pending_done_mutex);
			pending_done = TRUE;
			mono_coop_cond_signal (&pending_done_cond);
			mono_coop_mutex_unlock (&pending_done_mutex);
#endif
		}
	}

	/* If the initialization from native was done, do the clean up */
	if (did_init_from_native) {
		mono_thread_cleanup_from_native ();
	}

	mono_finalizer_lock ();
	finalizer_thread_exited = TRUE;
	mono_coop_cond_signal (&exited_cond);
	mono_finalizer_unlock ();

	return 0;
}

static void
init_finalizer_thread (void)
{
	ERROR_DECL (error);
	gc_thread = mono_thread_create_internal ((MonoThreadStart)finalizer_thread, NULL, MONO_THREAD_CREATE_FLAGS_NONE, error);
	mono_error_assert_ok (error);
}

/**
 * mono_gc_init_finalizer_thread:
 *
 * If the runtime is compiled with --with-lazy-gc-thread-creation, this
 * function must be called by embedders to create the finalizer. Otherwise, the
 * function does nothing and the runtime creates the finalizer thread
 * automatically.
 */
void
mono_gc_init_finalizer_thread (void)
{
#ifndef LAZY_GC_THREAD_CREATION
	/* do nothing */
#else
	MONO_ENTER_GC_UNSAFE;
	init_finalizer_thread ();
	MONO_EXIT_GC_UNSAFE;
#endif
}

static void
reference_queue_mutex_init (void)
{
	mono_coop_mutex_init_recursive (&reference_queue_mutex);
}

void
mono_gc_init (void)
{
	mono_lazy_initialize (&reference_queue_mutex_inited, reference_queue_mutex_init);
	mono_coop_mutex_init_recursive (&finalizer_mutex);
	mono_os_mutex_init_recursive (&finalizable_objects_hash_lock);
	finalizable_objects_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	mono_counters_register ("Minor GC collections", MONO_COUNTER_GC | MONO_COUNTER_INT, &mono_gc_stats.minor_gc_count);
	mono_counters_register ("Major GC collections", MONO_COUNTER_GC | MONO_COUNTER_INT, &mono_gc_stats.major_gc_count);
	mono_counters_register ("Minor GC time", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &mono_gc_stats.minor_gc_time);
	mono_counters_register ("Major GC time", MONO_COUNTER_GC | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_gc_stats.major_gc_time);
	mono_counters_register ("Major GC time concurrent", MONO_COUNTER_GC | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_gc_stats.major_gc_time_concurrent);

	mono_gc_base_init ();

	if (mono_gc_is_disabled ()) {
		gc_disabled = TRUE;
		return;
	}

#ifdef TARGET_WIN32
	pending_done_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	g_assert (pending_done_event);
#else
	mono_coop_cond_init (&pending_done_cond);
	mono_coop_mutex_init (&pending_done_mutex);
#endif

	mono_coop_cond_init (&exited_cond);
	mono_coop_sem_init (&finalizer_sem, 0);

#ifndef LAZY_GC_THREAD_CREATION
	if (!mono_runtime_get_no_exec ())
		init_finalizer_thread ();
#endif
}

gboolean
mono_gc_is_finalizer_internal_thread (MonoInternalThread *thread)
{
	return thread == gc_thread;
}

/**
 * mono_gc_is_finalizer_thread:
 * \param thread the thread to test.
 *
 * In Mono objects are finalized asynchronously on a separate thread.
 * This routine tests whether the \p thread argument represents the
 * finalization thread.
 * 
 * \returns TRUE if \p thread is the finalization thread.
 */
gboolean
mono_gc_is_finalizer_thread (MonoThread *thread)
{
	return mono_gc_is_finalizer_internal_thread (thread->internal_thread);
}

#if defined(__MACH__)
static pthread_t mach_exception_thread;

void
mono_gc_register_mach_exception_thread (pthread_t thread)
{
	mach_exception_thread = thread;
}

pthread_t
mono_gc_get_mach_exception_thread (void)
{
	return mach_exception_thread;
}
#endif

static MonoReferenceQueue *ref_queues;

static void
ref_list_remove_element (RefQueueEntry **prev, RefQueueEntry *element)
{
	do {
		/* Guard if head is changed concurrently. */
		while (*prev != element)
			prev = &(*prev)->next;
	} while (prev && mono_atomic_cas_ptr ((volatile gpointer *)prev, element->next, element) != element);
}

static void
ref_list_push (RefQueueEntry **head, RefQueueEntry *value)
{
	RefQueueEntry *current;
	do {
		current = *head;
		value->next = current;
		STORE_STORE_FENCE; /*Must make sure the previous store is visible before the CAS. */
	} while (mono_atomic_cas_ptr ((volatile gpointer *)head, value, current) != current);
}

static void
reference_queue_proccess (MonoReferenceQueue *queue)
{
	RefQueueEntry **iter = &queue->queue;
	RefQueueEntry *entry;
	while ((entry = *iter)) {
		if (queue->should_be_deleted || !mono_gchandle_get_target_internal (entry->gchandle)) {
			mono_gchandle_free_internal (entry->gchandle);
			ref_list_remove_element (iter, entry);
			queue->callback (entry->user_data);
			g_free (entry);
		} else {
			iter = &entry->next;
		}
	}
}

static void
reference_queue_proccess_all (void)
{
	MonoReferenceQueue **iter;
	MonoReferenceQueue *queue = ref_queues;
	for (; queue; queue = queue->next)
		reference_queue_proccess (queue);

restart:
	mono_coop_mutex_lock (&reference_queue_mutex);
	for (iter = &ref_queues; *iter;) {
		queue = *iter;
		if (!queue->should_be_deleted) {
			iter = &queue->next;
			continue;
		}
		if (queue->queue) {
			mono_coop_mutex_unlock (&reference_queue_mutex);
			reference_queue_proccess (queue);
			goto restart;
		}
		*iter = queue->next;
		g_free (queue);
	}
	mono_coop_mutex_unlock (&reference_queue_mutex);
}

static void
mono_reference_queue_cleanup (void)
{
	MonoReferenceQueue *queue = ref_queues;
	for (; queue; queue = queue->next)
		queue->should_be_deleted = TRUE;
	reference_queue_proccess_all ();
}

static void
reference_queue_clear_for_domain (MonoDomain *domain)
{
	MonoReferenceQueue *queue = ref_queues;
	for (; queue; queue = queue->next) {
		RefQueueEntry **iter = &queue->queue;
		RefQueueEntry *entry;
		while ((entry = *iter)) {
			if (entry->domain == domain) {
				mono_gchandle_free_internal (entry->gchandle);
				ref_list_remove_element (iter, entry);
				queue->callback (entry->user_data);
				g_free (entry);
			} else {
				iter = &entry->next;
			}
		}
	}
}
/**
 * mono_gc_reference_queue_new:
 * \param callback callback used when processing collected entries.
 *
 * Create a new reference queue used to process collected objects.
 * A reference queue let you add a pair of (managed object, user data)
 * using the \c mono_gc_reference_queue_add method.
 *
 * Once the managed object is collected \p callback will be called
 * in the finalizer thread with 'user data' as argument.
 *
 * The callback is called from the finalizer thread without any locks held.
 * When an AppDomain is unloaded, all callbacks for objects belonging to it
 * will be invoked.
 *
 * \returns the new queue.
 */
MonoReferenceQueue*
mono_gc_reference_queue_new (mono_reference_queue_callback callback)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (MonoReferenceQueue*, mono_gc_reference_queue_new_internal (callback));
}

MonoReferenceQueue*
mono_gc_reference_queue_new_internal (mono_reference_queue_callback callback)
{
	MonoReferenceQueue *res = g_new0 (MonoReferenceQueue, 1);
	res->callback = callback;

	mono_lazy_initialize (&reference_queue_mutex_inited, reference_queue_mutex_init);
	mono_coop_mutex_lock (&reference_queue_mutex);
	res->next = ref_queues;
	ref_queues = res;
	mono_coop_mutex_unlock (&reference_queue_mutex);

	return res;
}

/**
 * mono_gc_reference_queue_add:
 * \param queue the queue to add the reference to.
 * \param obj the object to be watched for collection
 * \param user_data parameter to be passed to the queue callback
 *
 * Queue an object to be watched for collection, when the \p obj is
 * collected, the callback that was registered for the \p queue will
 * be invoked with \p user_data as argument.
 *
 * \returns FALSE if the queue is scheduled to be freed.
 */
gboolean
mono_gc_reference_queue_add (MonoReferenceQueue *queue, MonoObject *obj, void *user_data)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (gboolean, mono_gc_reference_queue_add_internal (queue, obj, user_data));
}

gboolean
mono_gc_reference_queue_add_internal (MonoReferenceQueue *queue, MonoObject *obj, void *user_data)
{
	RefQueueEntry *entry;
	if (queue->should_be_deleted)
		return FALSE;

	g_assert (obj != NULL);

	entry = g_new0 (RefQueueEntry, 1);
	entry->user_data = user_data;
	entry->domain = mono_object_domain (obj);

	entry->gchandle = mono_gchandle_new_weakref_internal (obj, TRUE);
#ifndef HAVE_SGEN_GC
	mono_object_register_finalizer (obj);
#endif

	ref_list_push (&queue->queue, entry);
	return TRUE;
}

/**
 * mono_gc_reference_queue_free:
 * \param queue the queue that should be freed.
 *
 * This operation signals that \p queue should be freed. This operation is deferred
 * as it happens on the finalizer thread.
 *
 * After this call, no further objects can be queued. It's the responsibility of the
 * caller to make sure that no further attempt to access queue will be made.
 */
void
mono_gc_reference_queue_free (MonoReferenceQueue *queue)
{
	queue->should_be_deleted = TRUE;
}

MonoObjectHandle
mono_gc_alloc_handle_pinned_obj (MonoVTable *vtable, gsize size)
{
	return MONO_HANDLE_NEW (MonoObject, mono_gc_alloc_pinned_obj (vtable, size));
}

MonoObjectHandle
mono_gc_alloc_handle_obj (MonoVTable *vtable, gsize size)
{
	return MONO_HANDLE_NEW (MonoObject, mono_gc_alloc_obj (vtable, size));
}

MonoArrayHandle
mono_gc_alloc_handle_vector (MonoVTable *vtable, gsize size, gsize max_length)
{
	return MONO_HANDLE_NEW (MonoArray, mono_gc_alloc_vector (vtable, size, max_length));
}

MonoArrayHandle
mono_gc_alloc_handle_array (MonoVTable *vtable, gsize size, gsize max_length, gsize bounds_size)
{
	return MONO_HANDLE_NEW (MonoArray, mono_gc_alloc_array (vtable, size, max_length, bounds_size));
}

MonoStringHandle
mono_gc_alloc_handle_string (MonoVTable *vtable, gsize size, gint32 len)
{
	return MONO_HANDLE_NEW (MonoString, mono_gc_alloc_string (vtable, size, len));
}

MonoObjectHandle
mono_gc_alloc_handle_mature (MonoVTable *vtable, gsize size)
{
	return MONO_HANDLE_NEW (MonoObject, mono_gc_alloc_mature (vtable, size));
}

void
mono_gc_register_object_with_weak_fields (MonoObjectHandle obj)
{
	mono_gc_register_obj_with_weak_fields (MONO_HANDLE_RAW (obj));
}

/**
 * mono_gc_wbarrier_object_copy_handle:
 *
 * Write barrier to call when \p obj is the result of a clone or copy of an object.
 */
void
mono_gc_wbarrier_object_copy_handle (MonoObjectHandle obj, MonoObjectHandle src)
{
	mono_gc_wbarrier_object_copy_internal (MONO_HANDLE_RAW (obj), MONO_HANDLE_RAW (src));
}
