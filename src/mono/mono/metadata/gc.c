/*
 * metadata/gc.c: GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
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
#include <mono/metadata/mono-mlist.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool-ms.h>
#include <mono/sgen/sgen-conf.h>
#include <mono/sgen/sgen-gc.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/marshal.h> /* for mono_delegate_free_ftnptr () */
#include <mono/metadata/attach.h>
#include <mono/metadata/console-io.h>
#include <mono/utils/mono-os-semaphore.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-coop-semaphore.h>

#ifndef HOST_WIN32
#include <pthread.h>
#endif

typedef struct DomainFinalizationReq {
	MonoDomain *domain;
	HANDLE done_event;
} DomainFinalizationReq;

static gboolean gc_disabled = FALSE;

static gboolean finalizing_root_domain = FALSE;

gboolean log_finalizers = FALSE;
gboolean mono_do_not_finalize = FALSE;
gchar **mono_do_not_finalize_class_names = NULL;

#define mono_finalizer_lock() mono_coop_mutex_lock (&finalizer_mutex)
#define mono_finalizer_unlock() mono_coop_mutex_unlock (&finalizer_mutex)
static MonoCoopMutex finalizer_mutex;
static MonoCoopMutex reference_queue_mutex;

static GSList *domains_to_finalize= NULL;
static MonoMList *threads_to_finalize = NULL;

static gboolean finalizer_thread_exited;
/* Uses finalizer_mutex */
static MonoCoopCond exited_cond;

static MonoInternalThread *gc_thread;

static void object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*), MonoError *error);

static void reference_queue_proccess_all (void);
static void mono_reference_queue_cleanup (void);
static void reference_queue_clear_for_domain (MonoDomain *domain);
static HANDLE pending_done_event;

static guint32
guarded_wait (HANDLE handle, guint32 timeout, gboolean alertable)
{
	guint32 result;

	MONO_PREPARE_BLOCKING;
	result = WaitForSingleObjectEx (handle, timeout, alertable);
	MONO_FINISH_BLOCKING;

	return result;
}

static void
add_thread_to_finalize (MonoInternalThread *thread)
{
	mono_finalizer_lock ();
	if (!threads_to_finalize)
		MONO_GC_REGISTER_ROOT_SINGLE (threads_to_finalize, MONO_ROOT_SOURCE_FINALIZER_QUEUE, "finalizable threads list");
	threads_to_finalize = mono_mlist_append (threads_to_finalize, (MonoObject*)thread);
	mono_finalizer_unlock ();
}

static gboolean suspend_finalizers = FALSE;
/* 
 * actually, we might want to queue the finalize requests in a separate thread,
 * but we need to be careful about the execution domain of the thread...
 */
void
mono_gc_run_finalize (void *obj, void *data)
{
	MonoError error;
	MonoObject *exc = NULL;
	MonoObject *o;
#ifndef HAVE_SGEN_GC
	MonoObject *o2;
#endif
	MonoMethod* finalizer = NULL;
	MonoDomain *caller_domain = mono_domain_get ();
	MonoDomain *domain;
	RuntimeInvokeFunction runtime_invoke;

	// This function is called from the innards of the GC, so our best alternative for now is to do polling here
	mono_threads_safepoint ();

	o = (MonoObject*)((char*)obj + GPOINTER_TO_UINT (data));

	if (mono_do_not_finalize) {
		if (!mono_do_not_finalize_class_names)
			return;

		size_t namespace_len = strlen (o->vtable->klass->name_space);
		for (int i = 0; mono_do_not_finalize_class_names [i]; ++i) {
			const char *name = mono_do_not_finalize_class_names [i];
			if (strncmp (name, o->vtable->klass->name_space, namespace_len))
				break;
			if (name [namespace_len] != '.')
				break;
			if (strcmp (name + namespace_len + 1, o->vtable->klass->name))
				break;
			return;
		}
	}

	if (log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_DEBUG, "<%s at %p> Starting finalizer checks.", o->vtable->klass->name, o);

	if (suspend_finalizers)
		return;

	domain = o->vtable->domain;

#ifndef HAVE_SGEN_GC
	mono_domain_finalizers_lock (domain);

	o2 = (MonoObject *)g_hash_table_lookup (domain->finalizable_objects_hash, o);

	mono_domain_finalizers_unlock (domain);

	if (!o2)
		/* Already finalized somehow */
		return;
#endif

	/* make sure the finalizer is not called again if the object is resurrected */
	object_register_finalizer ((MonoObject *)obj, NULL, &error);
	mono_error_assert_ok (&error); /* FIXME don't swallow the error */

	if (log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Registered finalizer as processed.", o->vtable->klass->name, o);

	if (o->vtable->klass == mono_defaults.internal_thread_class) {
		MonoInternalThread *t = (MonoInternalThread*)o;

		if (mono_gc_is_finalizer_internal_thread (t))
			/* Avoid finalizing ourselves */
			return;

		if (t->threadpool_thread && finalizing_root_domain) {
			/* Don't finalize threadpool threads when
			   shutting down - they're finalized when the
			   threadpool shuts down. */
			add_thread_to_finalize (t);
			return;
		}
	}

	if (o->vtable->klass->image == mono_defaults.corlib && !strcmp (o->vtable->klass->name, "DynamicMethod") && finalizing_root_domain) {
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
	mono_domain_set_internal (mono_object_domain (o));

	/* delegates that have a native function pointer allocated are
	 * registered for finalization, but they don't have a Finalize
	 * method, because in most cases it's not needed and it's just a waste.
	 */
	if (o->vtable->klass->delegate) {
		MonoDelegate* del = (MonoDelegate*)o;
		if (del->delegate_trampoline)
			mono_delegate_free_ftnptr ((MonoDelegate*)o);
		mono_domain_set_internal (caller_domain);
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
		mono_domain_set_internal (caller_domain);
		return;
	}

	/* 
	 * To avoid the locking plus the other overhead of mono_runtime_invoke_checked (),
	 * create and precompile a wrapper which calls the finalize method using
	 * a CALLVIRT.
	 */
	if (log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Compiling finalizer.", o->vtable->klass->name, o);

	if (!domain->finalize_runtime_invoke) {
		MonoMethod *invoke = mono_marshal_get_runtime_invoke (mono_class_get_method_from_name_flags (mono_defaults.object_class, "Finalize", 0, 0), TRUE);

		domain->finalize_runtime_invoke = mono_compile_method (invoke);
	}

	runtime_invoke = (RuntimeInvokeFunction)domain->finalize_runtime_invoke;

	mono_runtime_class_init_full (o->vtable, &error);
	mono_error_raise_exception (&error); /* FIXME don't raise here */

	if (G_UNLIKELY (MONO_GC_FINALIZE_INVOKE_ENABLED ())) {
		MONO_GC_FINALIZE_INVOKE ((unsigned long)o, mono_object_get_size (o),
				o->vtable->klass->name_space, o->vtable->klass->name);
	}

	if (log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Calling finalizer.", o->vtable->klass->name, o);

	runtime_invoke (o, NULL, &exc, NULL);

	if (log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Returned from finalizer.", o->vtable->klass->name, o);

	if (exc)
		mono_thread_internal_unhandled_exception (exc);

	mono_domain_set_internal (caller_domain);
}

void
mono_gc_finalize_threadpool_threads (void)
{
	MonoError error;
	while (threads_to_finalize) {
		MonoInternalThread *thread = (MonoInternalThread*) mono_mlist_get_data (threads_to_finalize);

		/* Force finalization of the thread. */
		thread->threadpool_thread = FALSE;
		mono_object_register_finalizer ((MonoObject*)thread, &error);
		mono_error_assert_ok (&error); /* FIXME don't swallow the error */

		mono_gc_run_finalize (thread, NULL);

		threads_to_finalize = mono_mlist_next (threads_to_finalize);
	}
}

gpointer
mono_gc_out_of_memory (size_t size)
{
	/* 
	 * we could allocate at program startup some memory that we could release 
	 * back to the system at this point if we're really low on memory (ie, size is
	 * lower than the memory we set apart)
	 */
	mono_raise_exception (mono_domain_get ()->out_of_memory_ex);

	return NULL;
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
object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*), MonoError *error)
{
	MonoDomain *domain;

	mono_error_init (error);

	if (obj == NULL) {
		mono_error_set_argument_null (error, "obj", "");
		return;
	}

	domain = obj->vtable->domain;

#if HAVE_BOEHM_GC
	if (mono_domain_is_unloading (domain) && (callback != NULL))
		/*
		 * Can't register finalizers in a dying appdomain, since they
		 * could be invoked after the appdomain has been unloaded.
		 */
		return;

	mono_domain_finalizers_lock (domain);

	if (callback)
		g_hash_table_insert (domain->finalizable_objects_hash, obj, obj);
	else
		g_hash_table_remove (domain->finalizable_objects_hash, obj);

	mono_domain_finalizers_unlock (domain);

	mono_gc_register_for_finalization (obj, callback);
#elif defined(HAVE_SGEN_GC)
	/*
	 * If we register finalizers for domains that are unloading we might
	 * end up running them while or after the domain is being cleared, so
	 * the objects will not be valid anymore.
	 */
	if (!mono_domain_is_unloading (domain))
		mono_gc_register_for_finalization (obj, callback);
#endif
}

/**
 * mono_object_register_finalizer:
 * @obj: object to register
 *
 * Records that object @obj has a finalizer, this will call the
 * Finalize method when the garbage collector disposes the object.
 * 
 */
void
mono_object_register_finalizer (MonoObject *obj, MonoError *error)
{
	/* g_print ("Registered finalizer on %p %s.%s\n", obj, mono_object_class (obj)->name_space, mono_object_class (obj)->name); */
	object_register_finalizer (obj, mono_gc_run_finalize, error);
}

/**
 * mono_domain_finalize:
 * @domain: the domain to finalize
 * @timeout: msects to wait for the finalization to complete, -1 to wait indefinitely
 *
 *  Request finalization of all finalizable objects inside @domain. Wait
 * @timeout msecs for the finalization to complete.
 *
 * Returns: TRUE if succeeded, FALSE if there was a timeout
 */

gboolean
mono_domain_finalize (MonoDomain *domain, guint32 timeout) 
{
	DomainFinalizationReq *req;
	guint32 res;
	HANDLE done_event;
	MonoInternalThread *thread = mono_thread_internal_current ();

#if defined(__native_client__)
	return FALSE;
#endif

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

	done_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	if (done_event == NULL) {
		return FALSE;
	}

	req = g_new0 (DomainFinalizationReq, 1);
	req->domain = domain;
	req->done_event = done_event;

	if (domain == mono_get_root_domain ())
		finalizing_root_domain = TRUE;
	
	mono_finalizer_lock ();

	domains_to_finalize = g_slist_append (domains_to_finalize, req);

	mono_finalizer_unlock ();

	/* Tell the finalizer thread to finalize this appdomain */
	mono_gc_finalize_notify ();

	if (timeout == -1)
		timeout = INFINITE;

	while (TRUE) {
		res = guarded_wait (done_event, timeout, TRUE);
		/* printf ("WAIT RES: %d.\n", res); */

		if (res == WAIT_IO_COMPLETION) {
			if ((thread->state & (ThreadState_StopRequested | ThreadState_SuspendRequested)) != 0)
				return FALSE;
		} else if (res == WAIT_TIMEOUT) {
			/* We leak the handle here */
			return FALSE;
		} else {
			break;
		}
	}

	CloseHandle (done_event);

	if (domain == mono_get_root_domain ()) {
		mono_threadpool_ms_cleanup ();
		mono_gc_finalize_threadpool_threads ();
	}

	return TRUE;
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
ves_icall_System_GC_KeepAlive (MonoObject *obj)
{
	/*
	 * Does nothing.
	 */
}

void
ves_icall_System_GC_ReRegisterForFinalize (MonoObject *obj)
{
	MonoError error;

	MONO_CHECK_ARG_NULL (obj,);

	object_register_finalizer (obj, mono_gc_run_finalize, &error);
	mono_error_set_pending_exception (&error);
}

void
ves_icall_System_GC_SuppressFinalize (MonoObject *obj)
{
	MonoError error;

	MONO_CHECK_ARG_NULL (obj,);

	/* delegates have no finalizers, but we register them to deal with the
	 * unmanaged->managed trampoline. We don't let the user suppress it
	 * otherwise we'd leak it.
	 */
	if (obj->vtable->klass->delegate)
		return;

	/* FIXME: Need to handle case where obj has COM Callable Wrapper
	 * generated for it that needs cleaned up, but user wants to suppress
	 * their derived object finalizer. */

	object_register_finalizer (obj, NULL, &error);
	mono_error_set_pending_exception (&error);
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

	ResetEvent (pending_done_event);
	mono_gc_finalize_notify ();
	/* g_print ("Waiting for pending finalizers....\n"); */
	guarded_wait (pending_done_event, INFINITE, TRUE);
	/* g_print ("Done pending....\n"); */
}

void
ves_icall_System_GC_register_ephemeron_array (MonoObject *array)
{
#ifdef HAVE_SGEN_GC
	if (!mono_gc_ephemeron_array_add (array)) {
		mono_set_pending_exception (mono_object_domain (array)->out_of_memory_ex);
		return;
	}
#endif
}

MonoObject*
ves_icall_System_GC_get_ephemeron_tombstone (void)
{
	return mono_domain_get ()->ephemeron_tombstone;
}

MonoObject *
ves_icall_System_GCHandle_GetTarget (guint32 handle)
{
	return mono_gchandle_get_target (handle);
}

/*
 * if type == -1, change the target of the handle, otherwise allocate a new handle.
 */
guint32
ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type)
{
	if (type == -1) {
		mono_gchandle_set_target (handle, obj);
		/* the handle doesn't change */
		return handle;
	}
	switch (type) {
	case HANDLE_WEAK:
		return mono_gchandle_new_weakref (obj, FALSE);
	case HANDLE_WEAK_TRACK:
		return mono_gchandle_new_weakref (obj, TRUE);
	case HANDLE_NORMAL:
		return mono_gchandle_new (obj, FALSE);
	case HANDLE_PINNED:
		return mono_gchandle_new (obj, TRUE);
	default:
		g_assert_not_reached ();
	}
	return 0;
}

void
ves_icall_System_GCHandle_FreeHandle (guint32 handle)
{
	mono_gchandle_free (handle);
}

gpointer
ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle)
{
	MonoObject *obj;

	if (MONO_GC_HANDLE_TYPE (handle) != HANDLE_PINNED)
		return (gpointer)-2;
	obj = mono_gchandle_get_target (handle);
	if (obj) {
		MonoClass *klass = mono_object_class (obj);
		if (klass == mono_defaults.string_class) {
			return mono_string_chars ((MonoString*)obj);
		} else if (klass->rank) {
			return mono_array_addr ((MonoArray*)obj, char, 0);
		} else {
			/* the C# code will check and throw the exception */
			/* FIXME: missing !klass->blittable test, see bug #61134 */
			if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT)
				return (gpointer)-1;
			return (char*)obj + sizeof (MonoObject);
		}
	}
	return NULL;
}

MonoBoolean
mono_gc_GCHandle_CheckCurrentDomain (guint32 gchandle)
{
	return mono_gchandle_is_in_domain (gchandle, mono_domain_get ());
}

static MonoCoopSem finalizer_sem;
static volatile gboolean finished=FALSE;

void
mono_gc_finalize_notify (void)
{
#ifdef DEBUG
	g_message ( "%s: prodding finalizer", __func__);
#endif

	if (mono_gc_is_null ())
		return;

	mono_coop_sem_post (&finalizer_sem);
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
finalize_domain_objects (DomainFinalizationReq *req)
{
	MonoDomain *domain = req->domain;

#if HAVE_SGEN_GC
#define NUM_FOBJECTS 64
	MonoObject *to_finalize [NUM_FOBJECTS];
	int count;
#endif

	/* Process finalizers which are already in the queue */
	mono_gc_invoke_finalizers ();

#ifdef HAVE_BOEHM_GC
	while (g_hash_table_size (domain->finalizable_objects_hash) > 0) {
		int i;
		GPtrArray *objs;
		/* 
		 * Since the domain is unloading, nobody is allowed to put
		 * new entries into the hash table. But finalize_object might
		 * remove entries from the hash table, so we make a copy.
		 */
		objs = g_ptr_array_new ();
		g_hash_table_foreach (domain->finalizable_objects_hash, collect_objects, objs);
		/* printf ("FINALIZING %d OBJECTS.\n", objs->len); */

		for (i = 0; i < objs->len; ++i) {
			MonoObject *o = (MonoObject*)g_ptr_array_index (objs, i);
			/* FIXME: Avoid finalizing threads, etc */
			mono_gc_run_finalize (o, 0);
		}

		g_ptr_array_free (objs, TRUE);
	}
#elif defined(HAVE_SGEN_GC)
	while ((count = mono_gc_finalizers_for_domain (domain, to_finalize, NUM_FOBJECTS))) {
		int i;
		for (i = 0; i < count; ++i) {
			mono_gc_run_finalize (to_finalize [i], 0);
		}
	}
#endif

	/* cleanup the reference queue */
	reference_queue_clear_for_domain (domain);
	
	/* printf ("DONE.\n"); */
	SetEvent (req->done_event);

	/* The event is closed in mono_domain_finalize if we get here */
	g_free (req);
}

static guint32
finalizer_thread (gpointer unused)
{
	gboolean wait = TRUE;

	while (!finished) {
		/* Wait to be notified that there's at least one
		 * finaliser to run
		 */

		g_assert (mono_domain_get () == mono_get_root_domain ());
		mono_gc_set_skip_thread (TRUE);

		if (wait) {
			/* An alertable wait is required so this thread can be suspended on windows */
			mono_coop_sem_wait (&finalizer_sem, MONO_SEM_FLAGS_ALERTABLE);
		}
		wait = TRUE;

		mono_gc_set_skip_thread (FALSE);

		mono_threads_perform_thread_dump ();

		mono_console_handle_async_ops ();

		mono_attach_maybe_start ();

		if (domains_to_finalize) {
			mono_finalizer_lock ();
			if (domains_to_finalize) {
				DomainFinalizationReq *req = (DomainFinalizationReq *)domains_to_finalize->data;
				domains_to_finalize = g_slist_remove (domains_to_finalize, req);
				mono_finalizer_unlock ();

				finalize_domain_objects (req);
			} else {
				mono_finalizer_unlock ();
			}
		}				

		/* If finished == TRUE, mono_gc_cleanup has been called (from mono_runtime_cleanup),
		 * before the domain is unloaded.
		 */
		mono_gc_invoke_finalizers ();

		mono_threads_join_threads ();

		reference_queue_proccess_all ();

		/* Avoid posting the pending done event until there are pending finalizers */
		if (mono_coop_sem_timedwait (&finalizer_sem, 0, MONO_SEM_FLAGS_NONE) == 0) {
			/* Don't wait again at the start of the loop */
			wait = FALSE;
		} else {
			SetEvent (pending_done_event);
		}
	}

	mono_finalizer_lock ();
	finalizer_thread_exited = TRUE;
	mono_coop_cond_signal (&exited_cond);
	mono_finalizer_unlock ();

	return 0;
}

#ifndef LAZY_GC_THREAD_CREATION
static
#endif
void
mono_gc_init_finalizer_thread (void)
{
	gc_thread = mono_thread_create_internal (mono_domain_get (), finalizer_thread, NULL, FALSE, 0);
	ves_icall_System_Threading_Thread_SetName_internal (gc_thread, mono_string_new (mono_domain_get (), "Finalizer"));
}

void
mono_gc_init (void)
{
	mono_coop_mutex_init_recursive (&finalizer_mutex);
	mono_coop_mutex_init_recursive (&reference_queue_mutex);

	mono_counters_register ("Minor GC collections", MONO_COUNTER_GC | MONO_COUNTER_UINT, &gc_stats.minor_gc_count);
	mono_counters_register ("Major GC collections", MONO_COUNTER_GC | MONO_COUNTER_UINT, &gc_stats.major_gc_count);
	mono_counters_register ("Minor GC time", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &gc_stats.minor_gc_time);
	mono_counters_register ("Major GC time", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &gc_stats.major_gc_time);
	mono_counters_register ("Major GC time concurrent", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &gc_stats.major_gc_time_concurrent);

	mono_gc_base_init ();

	if (mono_gc_is_disabled ()) {
		gc_disabled = TRUE;
		return;
	}

	pending_done_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	g_assert (pending_done_event);
	mono_coop_cond_init (&exited_cond);
	mono_coop_sem_init (&finalizer_sem, 0);

#ifndef LAZY_GC_THREAD_CREATION
	mono_gc_init_finalizer_thread ();
#endif
}

void
mono_gc_cleanup (void)
{
#ifdef DEBUG
	g_message ("%s: cleaning up finalizer", __func__);
#endif

	if (mono_gc_is_null ())
		return;

	if (!gc_disabled) {
		finished = TRUE;
		if (mono_thread_internal_current () != gc_thread) {
			gboolean timed_out = FALSE;
			guint32 start_ticks = mono_msec_ticks ();
			guint32 end_ticks = start_ticks + 2000;

			mono_gc_finalize_notify ();
			/* Finishing the finalizer thread, so wait a little bit... */
			/* MS seems to wait for about 2 seconds */
			while (!finalizer_thread_exited) {
				guint32 current_ticks = mono_msec_ticks ();
				guint32 timeout;

				if (current_ticks >= end_ticks)
					break;
				else
					timeout = end_ticks - current_ticks;
				mono_finalizer_lock ();
				if (!finalizer_thread_exited)
					mono_coop_cond_timedwait (&exited_cond, &finalizer_mutex, timeout);
				mono_finalizer_unlock ();
			}

			if (!finalizer_thread_exited) {
				int ret;

				/* Set a flag which the finalizer thread can check */
				suspend_finalizers = TRUE;

				/* Try to abort the thread, in the hope that it is running managed code */
				mono_thread_internal_stop (gc_thread);

				/* Wait for it to stop */
				ret = guarded_wait (gc_thread->handle, 100, TRUE);

				if (ret == WAIT_TIMEOUT) {
					/* 
					 * The finalizer thread refused to die. There is not much we 
					 * can do here, since the runtime is shutting down so the 
					 * state the finalizer thread depends on will vanish.
					 */
					g_warning ("Shutting down finalizer thread timed out.");
					timed_out = TRUE;
				}
			}

			if (!timed_out) {
				int ret;

				/* Wait for the thread to actually exit */
				ret = guarded_wait (gc_thread->handle, INFINITE, TRUE);
				g_assert (ret == WAIT_OBJECT_0);

				mono_thread_join (GUINT_TO_POINTER (gc_thread->tid));
			}
			g_assert (finalizer_thread_exited);
		}
		gc_thread = NULL;
		mono_gc_base_cleanup ();
	}

	mono_reference_queue_cleanup ();

	mono_coop_mutex_destroy (&finalizer_mutex);
	mono_coop_mutex_destroy (&reference_queue_mutex);
}

gboolean
mono_gc_is_finalizer_internal_thread (MonoInternalThread *thread)
{
	return thread == gc_thread;
}

/**
 * mono_gc_is_finalizer_thread:
 * @thread: the thread to test.
 *
 * In Mono objects are finalized asynchronously on a separate thread.
 * This routine tests whether the @thread argument represents the
 * finalization thread.
 * 
 * Returns: TRUE if @thread is the finalization thread.
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
	} while (prev && InterlockedCompareExchangePointer ((volatile gpointer *)prev, element->next, element) != element);
}

static void
ref_list_push (RefQueueEntry **head, RefQueueEntry *value)
{
	RefQueueEntry *current;
	do {
		current = *head;
		value->next = current;
		STORE_STORE_FENCE; /*Must make sure the previous store is visible before the CAS. */
	} while (InterlockedCompareExchangePointer ((volatile gpointer *)head, value, current) != current);
}

static void
reference_queue_proccess (MonoReferenceQueue *queue)
{
	RefQueueEntry **iter = &queue->queue;
	RefQueueEntry *entry;
	while ((entry = *iter)) {
		if (queue->should_be_deleted || !mono_gchandle_get_target (entry->gchandle)) {
			mono_gchandle_free ((guint32)entry->gchandle);
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
				mono_gchandle_free ((guint32)entry->gchandle);
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
 * @callback callback used when processing collected entries.
 *
 * Create a new reference queue used to process collected objects.
 * A reference queue let you add a pair of (managed object, user data)
 * using the mono_gc_reference_queue_add method.
 *
 * Once the managed object is collected @callback will be called
 * in the finalizer thread with 'user data' as argument.
 *
 * The callback is called from the finalizer thread without any locks held.
 * When a AppDomain is unloaded, all callbacks for objects belonging to it
 * will be invoked.
 *
 * @returns the new queue.
 */
MonoReferenceQueue*
mono_gc_reference_queue_new (mono_reference_queue_callback callback)
{
	MonoReferenceQueue *res = g_new0 (MonoReferenceQueue, 1);
	res->callback = callback;

	mono_coop_mutex_lock (&reference_queue_mutex);
	res->next = ref_queues;
	ref_queues = res;
	mono_coop_mutex_unlock (&reference_queue_mutex);

	return res;
}

/**
 * mono_gc_reference_queue_add:
 * @queue the queue to add the reference to.
 * @obj the object to be watched for collection
 * @user_data parameter to be passed to the queue callback
 *
 * Queue an object to be watched for collection, when the @obj is
 * collected, the callback that was registered for the @queue will
 * be invoked with @user_data as argument.
 *
 * @returns false if the queue is scheduled to be freed.
 */
gboolean
mono_gc_reference_queue_add (MonoReferenceQueue *queue, MonoObject *obj, void *user_data)
{
	MonoError error;
	RefQueueEntry *entry;
	if (queue->should_be_deleted)
		return FALSE;

	entry = g_new0 (RefQueueEntry, 1);
	entry->user_data = user_data;
	entry->domain = mono_object_domain (obj);

	entry->gchandle = mono_gchandle_new_weakref (obj, TRUE);
	mono_object_register_finalizer (obj, &error);
	mono_error_assert_ok (&error);

	ref_list_push (&queue->queue, entry);
	return TRUE;
}

/**
 * mono_gc_reference_queue_free:
 * @queue the queue that should be freed.
 *
 * This operation signals that @queue should be freed. This operation is deferred
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
