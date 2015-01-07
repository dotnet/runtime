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

#include <mono/metadata/gc-internal.h>
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
#include <mono/utils/mono-logger-internal.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/marshal.h> /* for mono_delegate_free_ftnptr () */
#include <mono/metadata/attach.h>
#include <mono/metadata/console-io.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/atomic.h>

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
gboolean do_not_finalize = FALSE;

#define mono_finalizer_lock() mono_mutex_lock (&finalizer_mutex)
#define mono_finalizer_unlock() mono_mutex_unlock (&finalizer_mutex)
static mono_mutex_t finalizer_mutex;
static mono_mutex_t reference_queue_mutex;

static GSList *domains_to_finalize= NULL;
static MonoMList *threads_to_finalize = NULL;

static gboolean finalizer_thread_exited;
/* Uses finalizer_mutex */
static mono_cond_t exited_cond;

static MonoInternalThread *gc_thread;

static void object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*));

static void mono_gchandle_set_target (guint32 gchandle, MonoObject *obj);

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
	if (do_not_finalize)
		return;

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
	MONO_SUSPEND_CHECK ();

	o = (MonoObject*)((char*)obj + GPOINTER_TO_UINT (data));

	if (log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_DEBUG, "<%s at %p> Starting finalizer checks.", o->vtable->klass->name, o);

	if (suspend_finalizers)
		return;

	domain = o->vtable->domain;

#ifndef HAVE_SGEN_GC
	mono_domain_finalizers_lock (domain);

	o2 = g_hash_table_lookup (domain->finalizable_objects_hash, o);

	mono_domain_finalizers_unlock (domain);

	if (!o2)
		/* Already finalized somehow */
		return;
#endif

	/* make sure the finalizer is not called again if the object is resurrected */
	object_register_finalizer (obj, NULL);

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
	 * To avoid the locking plus the other overhead of mono_runtime_invoke (),
	 * create and precompile a wrapper which calls the finalize method using
	 * a CALLVIRT.
	 */
	if (log_finalizers)
		g_log ("mono-gc-finalizers", G_LOG_LEVEL_MESSAGE, "<%s at %p> Compiling finalizer.", o->vtable->klass->name, o);

	if (!domain->finalize_runtime_invoke) {
		MonoMethod *invoke = mono_marshal_get_runtime_invoke (mono_class_get_method_from_name_flags (mono_defaults.object_class, "Finalize", 0, 0), TRUE);

		domain->finalize_runtime_invoke = mono_compile_method (invoke);
	}

	runtime_invoke = domain->finalize_runtime_invoke;

	mono_runtime_class_init (o->vtable);

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
	while (threads_to_finalize) {
		MonoInternalThread *thread = (MonoInternalThread*) mono_mlist_get_data (threads_to_finalize);

		/* Force finalization of the thread. */
		thread->threadpool_thread = FALSE;
		mono_object_register_finalizer ((MonoObject*)thread);

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
object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*))
{
	MonoDomain *domain;

	if (obj == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("obj"));

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
	if (!mono_domain_is_unloading (domain)) {
		MONO_TRY_BLOCKING;
		mono_gc_register_for_finalization (obj, callback);
		MONO_FINISH_TRY_BLOCKING;
	}
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
mono_object_register_finalizer (MonoObject *obj)
{
	/* g_print ("Registered finalizer on %p %s.%s\n", obj, mono_object_class (obj)->name_space, mono_object_class (obj)->name); */
	object_register_finalizer (obj, mono_gc_run_finalize);
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
	MONO_CHECK_ARG_NULL (obj,);

	object_register_finalizer (obj, mono_gc_run_finalize);
}

void
ves_icall_System_GC_SuppressFinalize (MonoObject *obj)
{
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

	object_register_finalizer (obj, NULL);
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

#define mono_allocator_lock() mono_mutex_lock (&allocator_section)
#define mono_allocator_unlock() mono_mutex_unlock (&allocator_section)
static mono_mutex_t allocator_section;

#define GC_HANDLE_TYPE_SHIFT (3)
#define GC_HANDLE_TYPE_MASK ((1 << GC_HANDLE_TYPE_SHIFT) - 1)
#define GC_HANDLE_TYPE(x) (((x) & GC_HANDLE_TYPE_MASK) - 1)
#define GC_HANDLE_INDEX(x) ((x) >> GC_HANDLE_TYPE_SHIFT)
#define GC_HANDLE(index, type) (((index) << GC_HANDLE_TYPE_SHIFT) | (((type) & GC_HANDLE_TYPE_MASK) + 1))

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

	if (GC_HANDLE_TYPE (handle) != HANDLE_PINNED)
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
ves_icall_Mono_Runtime_SetGCAllowSynchronousMajor (MonoBoolean flag)
{
	return mono_gc_set_allow_synchronous_major (flag);
}

#define BUCKETS (32 - GC_HANDLE_TYPE_SHIFT)
#define MIN_BUCKET_BITS (5)
#define MIN_BUCKET_SIZE (1 << MIN_BUCKET_BITS)

/*
 * A table of GC handle data, implementing a simple lock-free bitmap allocator.
 *
 * 'entries' is an array of pointers to buckets of increasing size. The first
 * bucket has size 'MIN_BUCKET_SIZE', and each bucket is twice the size of the
 * previous, i.e.:
 *
 *           |-------|-- MIN_BUCKET_SIZE
 *    [0] -> xxxxxxxx
 *    [1] -> xxxxxxxxxxxxxxxx
 *    [2] -> xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
 *    ...
 *
 * The size of the spine, 'BUCKETS', is chosen so that the maximum number of
 * entries is no less than the maximum index value of a GC handle.
 *
 * Each entry in a bucket is a pointer with two tag bits: if
 * 'GC_HANDLE_OCCUPIED' returns true for a slot, then the slot is occupied; if
 * so, then 'GC_HANDLE_VALID' gives whether the entry refers to a valid (1) or
 * NULL (0) object reference. If the reference is valid, then the pointer is an
 * object pointer. If the reference is NULL, and 'GC_HANDLE_TYPE_IS_WEAK' is
 * true for 'type', then the pointer is a domain pointer--this allows us to
 * retrieve the domain ID of an expired weak reference.
 *
 * Finally, 'slot_hint' denotes the position of the last allocation, so that the
 * whole array needn't be searched on every allocation.
 */

typedef struct {
	volatile gpointer *volatile entries [BUCKETS];
	volatile guint32 capacity;
	volatile guint32 slot_hint;
	guint8 type;
} HandleData;

static inline guint
bucket_size (guint index)
{
	return 1 << (index + MIN_BUCKET_BITS);
}

/* Computes floor(log2(index + MIN_BUCKET_SIZE)) - 1, giving the index
 * of the bucket containing a slot.
 */
static inline guint
index_bucket (guint index)
{
#ifdef __GNUC__
	return CHAR_BIT * sizeof (index) - __builtin_clz (index + MIN_BUCKET_SIZE) - 1 - MIN_BUCKET_BITS;
#else
	guint count = 0;
	index += MIN_BUCKET_SIZE;
	while (index) {
		++count;
		index >>= 1;
	}
	return count - 1 - MIN_BUCKET_BITS;
#endif
}

static inline void
bucketize (guint index, guint *bucket, guint *offset)
{
	*bucket = index_bucket (index);
	*offset = index - bucket_size (*bucket) + MIN_BUCKET_SIZE;
}

static inline gboolean
try_set_slot (volatile gpointer *slot, MonoObject *obj, gpointer old, GCHandleType type)
{
    if (obj)
		return InterlockedCompareExchangePointer (slot, MONO_GC_HANDLE_OBJECT_POINTER (obj, GC_HANDLE_TYPE_IS_WEAK (type)), old) == old;
    return InterlockedCompareExchangePointer (slot, MONO_GC_HANDLE_DOMAIN_POINTER (mono_domain_get (), GC_HANDLE_TYPE_IS_WEAK (type)), old) == old;
}

/* Try to claim a slot by setting its occupied bit. */
static inline gboolean
try_occupy_slot (HandleData *handles, guint bucket, guint offset, MonoObject *obj, gboolean track)
{
	volatile gpointer *link_addr = &(handles->entries [bucket] [offset]);
	if (MONO_GC_HANDLE_OCCUPIED (*link_addr))
		return FALSE;
	return try_set_slot (link_addr, obj, NULL, handles->type);
}

#define EMPTY_HANDLE_DATA(type) { { NULL }, 0, 0, (type) }

/* weak and weak-track arrays will be allocated in malloc memory 
 */
static HandleData gc_handles [] = {
	EMPTY_HANDLE_DATA (HANDLE_WEAK),
	EMPTY_HANDLE_DATA (HANDLE_WEAK_TRACK),
	EMPTY_HANDLE_DATA (HANDLE_NORMAL),
	EMPTY_HANDLE_DATA (HANDLE_PINNED)
};

static HandleData *
gc_handles_for_type (GCHandleType type)
{
	g_assert (type < HANDLE_TYPE_MAX);
	return &gc_handles [type];
}

static void
mark_gc_handles (void *addr, MonoGCMarkFunc mark_func, void *gc_data)
{
	HandleData *handles = gc_handles_for_type (HANDLE_NORMAL);
	size_t bucket, offset;
	const guint max_bucket = index_bucket (handles->capacity);
	for (bucket = 0; bucket < max_bucket; ++bucket) {
		volatile gpointer *entries = handles->entries [bucket];
		for (offset = 0; offset < bucket_size (bucket); ++offset) {
			volatile gpointer *entry = &entries [offset];
			gpointer hidden = *entry;
			gpointer revealed = MONO_GC_REVEAL_POINTER (hidden, FALSE);
			if (!MONO_GC_HANDLE_IS_OBJECT_POINTER (hidden))
				continue;
			mark_func ((MonoObject **)&revealed, gc_data);
			g_assert (revealed);
			*entry = MONO_GC_HANDLE_OBJECT_POINTER (revealed, FALSE);
		}
	}
}

static guint
handle_data_find_unset (HandleData *handles, guint32 begin, guint32 end)
{
	guint index;
	gint delta = begin < end ? +1 : -1;
	for (index = begin; index < end; index += delta) {
		guint bucket, offset;
		volatile gpointer *entries;
		bucketize (index, &bucket, &offset);
		entries = handles->entries [bucket];
		g_assert (entries);
		if (!MONO_GC_HANDLE_OCCUPIED (entries [offset]))
			return index;
	}
	return -1;
}

/* Adds a bucket if necessary and possible. */
static void
handle_data_grow (HandleData *handles, guint32 old_capacity)
{
	const guint new_bucket = index_bucket (old_capacity);
	const guint32 growth = bucket_size (new_bucket);
	const guint32 new_capacity = old_capacity + growth;
	gpointer *entries;
	const size_t new_bucket_size = sizeof (**handles->entries) * growth;
	if (handles->capacity >= new_capacity)
		return;
	entries = g_malloc0 (new_bucket_size);
#ifdef HAVE_BOEHM_GC
	if (!GC_HANDLE_TYPE_IS_WEAK (handles->type))
		mono_gc_register_root ((char *)entries, new_bucket_size, handles->type == HANDLE_PINNED ? NULL : mono_gc_make_root_descr_all_refs (new_bucket_size * CHAR_BIT), MONO_ROOT_SOURCE_GC_HANDLE, "gc handles table");
#endif
#ifdef HAVE_SGEN_GC
	if (handles->type == HANDLE_PINNED)
		mono_gc_register_root ((char *)entries, new_bucket_size, MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_GC_HANDLE, "gc handles table");
#endif
	if (InterlockedCompareExchangePointer ((volatile gpointer *)&handles->entries [new_bucket], entries, NULL) == NULL) {
		if (InterlockedCompareExchange ((volatile gint32 *)&handles->capacity, new_capacity, old_capacity) != old_capacity)
			g_assert_not_reached ();
		handles->slot_hint = old_capacity;
		mono_memory_write_barrier ();
		return;
	}
	/* Someone beat us to the allocation. */
#ifdef HAVE_BOEHM_GC
	mono_gc_deregister_root ((char *)entries);
#endif
	g_free (entries);
}

static guint32
alloc_handle (HandleData *handles, MonoObject *obj, gboolean track)
{
	guint index;
	guint32 res;
	guint bucket, offset;
	guint32 capacity;
	guint32 slot_hint;
	if (!handles->capacity)
		handle_data_grow (handles, 0);
retry:
	capacity = handles->capacity;
	slot_hint = handles->slot_hint;
	index = handle_data_find_unset (handles, slot_hint, capacity);
	if (index == -1)
		index = handle_data_find_unset (handles, 0, slot_hint);
	if (index == -1) {
		handle_data_grow (handles, capacity);
		goto retry;
	}
	handles->slot_hint = index;
	bucketize (index, &bucket, &offset);
	if (!try_occupy_slot (handles, bucket, offset, obj, track))
		goto retry;
	if (obj && GC_HANDLE_TYPE_IS_WEAK (handles->type))
		mono_gc_weak_link_register (&handles->entries [bucket] [offset], obj, track);
	/* Ensure that a GC handle cannot be given to another thread without the slot having been set. */
	mono_memory_write_barrier ();
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_num_handles++;
#endif
	res = GC_HANDLE (index, handles->type);
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_CREATED, handles->type, res, obj);
	return res;
}

/*
 * Maps a function over all GC handles.
 * This assumes that the world is stopped!
 */
void
mono_gchandle_iterate (GCHandleType handle_type, int max_generation, gpointer callback(gpointer, GCHandleType, gpointer), gpointer user)
{
	HandleData *handle_data = gc_handles_for_type (handle_type);
	size_t bucket, offset;
	guint max_bucket = index_bucket (handle_data->capacity);
	/* If a new bucket has been allocated, but the capacity has not yet been
	 * increased, nothing can yet have been allocated in the bucket because the
	 * world is stopped, so we shouldn't miss any handles during iteration.
	 */
	for (bucket = 0; bucket < max_bucket; ++bucket) {
		volatile gpointer *entries = handle_data->entries [bucket];
		for (offset = 0; offset < bucket_size (bucket); ++offset) {
			gpointer hidden = entries [offset];
			gpointer revealed;
			gpointer result;
			/* Table must contain no garbage pointers. */
			gboolean occupied = MONO_GC_HANDLE_OCCUPIED (hidden);
			g_assert (hidden ? occupied : !occupied);
			if (!occupied || !MONO_GC_HANDLE_VALID (hidden))
				continue;
			revealed = MONO_GC_REVEAL_POINTER (hidden, GC_HANDLE_TYPE_IS_WEAK (handle_type));
			g_assert (revealed);
			if (mono_gc_object_older_than (revealed, max_generation))
				continue;
			result = callback (revealed, handle_type, user);
			if (result)
				g_assert (MONO_GC_HANDLE_OCCUPIED (result));
			entries [offset] = result;
		}
	}
}

/**
 * mono_gchandle_new:
 * @obj: managed object to get a handle for
 * @pinned: whether the object should be pinned
 *
 * This returns a handle that wraps the object, this is used to keep a
 * reference to a managed object from the unmanaged world and preventing the
 * object from being disposed.
 * 
 * If @pinned is false the address of the object can not be obtained, if it is
 * true the address of the object can be obtained.  This will also pin the
 * object so it will not be possible by a moving garbage collector to move the
 * object. 
 * 
 * Returns: a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new (MonoObject *obj, gboolean pinned)
{
	return alloc_handle (gc_handles_for_type (pinned ? HANDLE_PINNED : HANDLE_NORMAL), obj, FALSE);
}

/**
 * mono_gchandle_new_weakref:
 * @obj: managed object to get a handle for
 * @pinned: whether the object should be pinned
 *
 * This returns a weak handle that wraps the object, this is used to
 * keep a reference to a managed object from the unmanaged world.
 * Unlike the mono_gchandle_new the object can be reclaimed by the
 * garbage collector.  In this case the value of the GCHandle will be
 * set to zero.
 * 
 * If @pinned is false the address of the object can not be obtained, if it is
 * true the address of the object can be obtained.  This will also pin the
 * object so it will not be possible by a moving garbage collector to move the
 * object. 
 * 
 * Returns: a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new_weakref (MonoObject *obj, gboolean track_resurrection)
{
	guint32 handle = alloc_handle (gc_handles_for_type (track_resurrection ? HANDLE_WEAK_TRACK : HANDLE_WEAK), obj, track_resurrection);

	return handle;
}

static MonoObject *
link_get (volatile gpointer *link_addr, gboolean is_weak)
{
	void *volatile *link_addr_volatile;
	void *ptr;
	MonoObject *obj;
retry:
	link_addr_volatile = link_addr;
	ptr = (void*)*link_addr_volatile;
	/*
	 * At this point we have a hidden pointer.  If the GC runs
	 * here, it will not recognize the hidden pointer as a
	 * reference, and if the object behind it is not referenced
	 * elsewhere, it will be freed.  Once the world is restarted
	 * we reveal the pointer, giving us a pointer to a freed
	 * object.  To make sure we don't return it, we load the
	 * hidden pointer again.  If it's still the same, we can be
	 * sure the object reference is valid.
	 */
	if (ptr && MONO_GC_HANDLE_IS_OBJECT_POINTER (ptr))
		obj = (MonoObject *)MONO_GC_REVEAL_POINTER (ptr, is_weak);
	else
		return NULL;

	/* Note [dummy use]:
	 *
	 * If a GC happens here, obj needs to be on the stack or in a
	 * register, so we need to prevent this from being reordered
	 * wrt the check.
	 */
	mono_gc_dummy_use (obj);
	mono_memory_barrier ();

	if (is_weak)
		mono_gc_ensure_weak_links_accessible ();

	if ((void*)*link_addr_volatile != ptr)
		goto retry;

	return obj;
}

/**
 * mono_gchandle_get_target:
 * @gchandle: a GCHandle's handle.
 *
 * The handle was previously created by calling mono_gchandle_new or
 * mono_gchandle_new_weakref. 
 *
 * Returns a pointer to the MonoObject represented by the handle or
 * NULL for a collected object if using a weakref handle.
 */
MonoObject*
mono_gchandle_get_target (guint32 gchandle)
{
	guint index = GC_HANDLE_INDEX (gchandle);
	guint type = GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	guint bucket, offset;
	g_assert (index < handles->capacity);
	bucketize (index, &bucket, &offset);
	return link_get (&handles->entries [bucket] [offset], GC_HANDLE_TYPE_IS_WEAK (type));
}

static void
mono_gchandle_set_target (guint32 gchandle, MonoObject *obj)
{
	guint index = GC_HANDLE_INDEX (gchandle);
	guint type = GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	gboolean track = handles->type == HANDLE_WEAK_TRACK;
	guint bucket, offset;
	gpointer slot;

	g_assert (index < handles->capacity);
	bucketize (index, &bucket, &offset);

retry:
	slot = handles->entries [bucket] [offset];
	g_assert (MONO_GC_HANDLE_OCCUPIED (slot));
	if (!try_set_slot (&handles->entries [bucket] [offset], obj, slot, GC_HANDLE_TYPE_IS_WEAK (handles->type)))
		goto retry;
	if (MONO_GC_HANDLE_IS_OBJECT_POINTER (slot))
		mono_gc_weak_link_unregister (&handles->entries [bucket] [offset], track);
	if (obj)
		mono_gc_weak_link_register (&handles->entries [bucket] [offset], obj, track);
}

static MonoDomain *
mono_gchandle_slot_domain (volatile gpointer *slot_addr, gboolean is_weak)
{
	gpointer slot;
	MonoDomain *domain;
retry:
	slot = *slot_addr;
	if (!MONO_GC_HANDLE_OCCUPIED (slot))
		return NULL;
	if (MONO_GC_HANDLE_IS_OBJECT_POINTER (slot)) {
		MonoObject *obj = MONO_GC_REVEAL_POINTER (slot, is_weak);
		/* See note [dummy use]. */
		mono_gc_dummy_use (obj);
		if (*slot_addr != slot)
			goto retry;
		return mono_object_domain (obj);
	}
	domain = MONO_GC_REVEAL_POINTER (slot, is_weak);
	/* See note [dummy use]. */
	mono_gc_dummy_use (domain);
	if (*slot_addr != slot)
		goto retry;
	return domain;
}

static MonoDomain *
gchandle_domain (guint32 gchandle) {
	guint index = GC_HANDLE_INDEX (gchandle);
	guint type = GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	guint bucket, offset;
	if (index >= handles->capacity)
		return NULL;
	bucketize (index, &bucket, &offset);
	return mono_gchandle_slot_domain (&handles->entries [bucket] [offset], GC_HANDLE_TYPE_IS_WEAK (type));
}

/**
 * mono_gchandle_is_in_domain:
 * @gchandle: a GCHandle's handle.
 * @domain: An application domain.
 *
 * Returns: true if the object wrapped by the @gchandle belongs to the specific @domain.
 */
gboolean
mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain)
{
	return domain->domain_id == gchandle_domain (gchandle)->domain_id;
}

/**
 * mono_gchandle_free:
 * @gchandle: a GCHandle's handle.
 *
 * Frees the @gchandle handle.  If there are no outstanding
 * references, the garbage collector can reclaim the memory of the
 * object wrapped. 
 */
void
mono_gchandle_free (guint32 gchandle)
{
	guint index = GC_HANDLE_INDEX (gchandle);
	guint type = GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	guint bucket, offset;
	bucketize (index, &bucket, &offset);
	if (index < handles->capacity && MONO_GC_HANDLE_OCCUPIED (handles->entries [bucket] [offset])) {
		if (GC_HANDLE_TYPE_IS_WEAK (handles->type))
			mono_gc_weak_link_unregister (&handles->entries [bucket] [offset], handles->type == HANDLE_WEAK_TRACK);
		handles->entries [bucket] [offset] = NULL;
	} else {
		/* print a warning? */
	}
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_num_handles--;
#endif
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_DESTROYED, handles->type, gchandle, NULL);
}

/**
 * mono_gchandle_free_domain:
 * @unloading: domain that is unloading
 *
 * Function used internally to cleanup any GC handle for objects belonging
 * to the specified domain during appdomain unload.
 */
void
mono_gchandle_free_domain (MonoDomain *unloading)
{
	guint type;
	/* All non-pinned handle types. */
	for (type = HANDLE_TYPE_MIN; type < HANDLE_PINNED; ++type) {
		const gboolean is_weak = GC_HANDLE_TYPE_IS_WEAK (type);
		guint index;
		HandleData *handles = gc_handles_for_type (type);
		guint32 capacity = handles->capacity;
		for (index = 0; index < capacity; ++index) {
			guint bucket, offset;
			gpointer slot;
			bucketize (index, &bucket, &offset);
			MonoObject *obj = NULL;
			MonoDomain *domain;
			volatile gpointer *slot_addr = &handles->entries [bucket] [offset];
			/* NB: This should have the same behavior as mono_gchandle_slot_domain(). */
		retry:
			slot = *slot_addr;
			if (!MONO_GC_HANDLE_OCCUPIED (slot))
				continue;
			if (MONO_GC_HANDLE_IS_OBJECT_POINTER (slot)) {
				obj = MONO_GC_REVEAL_POINTER (slot, is_weak);
				if (*slot_addr != slot)
					goto retry;
				domain = mono_object_domain (obj);
			} else {
				domain = MONO_GC_REVEAL_POINTER (slot, is_weak);
			}
			if (unloading->domain_id == domain->domain_id) {
				if (GC_HANDLE_TYPE_IS_WEAK (type) && MONO_GC_REVEAL_POINTER (slot, is_weak))
					mono_gc_weak_link_unregister (&handles->entries [bucket] [offset], handles->type == HANDLE_WEAK_TRACK);
				*slot_addr = NULL;
			}
			/* See note [dummy use]. */
			mono_gc_dummy_use (obj);
		}
	}

}

MonoBoolean
mono_gc_GCHandle_CheckCurrentDomain (guint32 gchandle)
{
	return mono_gchandle_is_in_domain (gchandle, mono_domain_get ());
}

#ifdef MONO_HAS_SEMAPHORES
static MonoSemType finalizer_sem;
#endif
static HANDLE finalizer_event;
static volatile gboolean finished=FALSE;

void
mono_gc_finalize_notify (void)
{
#ifdef DEBUG
	g_message ( "%s: prodding finalizer", __func__);
#endif

	if (mono_gc_is_null ())
		return;

#ifdef MONO_HAS_SEMAPHORES
	MONO_SEM_POST (&finalizer_sem);
#else
	SetEvent (finalizer_event);
#endif
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
		MONO_PREPARE_BLOCKING;

		if (wait) {
		/* An alertable wait is required so this thread can be suspended on windows */
#ifdef MONO_HAS_SEMAPHORES
			MONO_SEM_WAIT_ALERTABLE (&finalizer_sem, TRUE);
#else
			WaitForSingleObjectEx (finalizer_event, INFINITE, TRUE);
#endif
		}
		wait = TRUE;
		MONO_FINISH_BLOCKING;
		mono_gc_set_skip_thread (FALSE);

		mono_threads_perform_thread_dump ();

		mono_console_handle_async_ops ();

		mono_attach_maybe_start ();

		if (domains_to_finalize) {
			mono_finalizer_lock ();
			if (domains_to_finalize) {
				DomainFinalizationReq *req = domains_to_finalize->data;
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

#ifdef MONO_HAS_SEMAPHORES
		/* Avoid posting the pending done event until there are pending finalizers */
		if (MONO_SEM_TIMEDWAIT (&finalizer_sem, 0) == 0)
			/* Don't wait again at the start of the loop */
			wait = FALSE;
		else
			SetEvent (pending_done_event);
#else
			SetEvent (pending_done_event);
#endif
	}

	mono_finalizer_lock ();
	finalizer_thread_exited = TRUE;
	mono_cond_signal (&exited_cond);
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
	mono_mutex_init_recursive (&allocator_section);

	mono_mutex_init_recursive (&finalizer_mutex);
	mono_mutex_init_recursive (&reference_queue_mutex);

	mono_counters_register ("Minor GC collections", MONO_COUNTER_GC | MONO_COUNTER_UINT, &gc_stats.minor_gc_count);
	mono_counters_register ("Major GC collections", MONO_COUNTER_GC | MONO_COUNTER_UINT, &gc_stats.major_gc_count);
	mono_counters_register ("Minor GC time", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &gc_stats.minor_gc_time);
	mono_counters_register ("Major GC time", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &gc_stats.major_gc_time);
	mono_counters_register ("Major GC time concurrent", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &gc_stats.major_gc_time_concurrent);

	mono_gc_base_init ();

#ifdef HAVE_SGEN_GC
	mono_gc_register_root ((char *)&gc_handles [0], sizeof (gc_handles), mono_gc_make_root_descr_user (mark_gc_handles), MONO_ROOT_SOURCE_GC_HANDLE, "gc handles table");
#endif

	if (mono_gc_is_disabled ()) {
		gc_disabled = TRUE;
		return;
	}
	
	finalizer_event = CreateEvent (NULL, FALSE, FALSE, NULL);
	g_assert (finalizer_event);
	pending_done_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	g_assert (pending_done_event);
	mono_cond_init (&exited_cond, 0);
#ifdef MONO_HAS_SEMAPHORES
	MONO_SEM_INIT (&finalizer_sem, 0);
#endif

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
				MONO_PREPARE_BLOCKING;
				mono_finalizer_lock ();
				if (!finalizer_thread_exited)
					mono_cond_timedwait_ms (&exited_cond, &finalizer_mutex, timeout);
				mono_finalizer_unlock ();
				MONO_FINISH_BLOCKING;
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
		}
		gc_thread = NULL;
		mono_gc_base_cleanup ();
	}

	mono_reference_queue_cleanup ();

	mono_mutex_destroy (&allocator_section);
	mono_mutex_destroy (&finalizer_mutex);
	mono_mutex_destroy (&reference_queue_mutex);
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
 * Returns true if @thread is the finalization thread.
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

#ifndef HAVE_SGEN_GC
void*
mono_gc_alloc_mature (MonoVTable *vtable)
{
	return mono_object_new_specific (vtable);
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
	} while (prev && InterlockedCompareExchangePointer ((void*)prev, element->next, element) != element);
}

static void
ref_list_push (RefQueueEntry **head, RefQueueEntry *value)
{
	RefQueueEntry *current;
	do {
		current = *head;
		value->next = current;
		STORE_STORE_FENCE; /*Must make sure the previous store is visible before the CAS. */
	} while (InterlockedCompareExchangePointer ((void*)head, value, current) != current);
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
	mono_mutex_lock (&reference_queue_mutex);
	for (iter = &ref_queues; *iter;) {
		queue = *iter;
		if (!queue->should_be_deleted) {
			iter = &queue->next;
			continue;
		}
		if (queue->queue) {
			mono_mutex_unlock (&reference_queue_mutex);
			reference_queue_proccess (queue);
			goto restart;
		}
		*iter = queue->next;
		g_free (queue);
	}
	mono_mutex_unlock (&reference_queue_mutex);
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

	mono_mutex_lock (&reference_queue_mutex);
	res->next = ref_queues;
	ref_queues = res;
	mono_mutex_unlock (&reference_queue_mutex);

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
	RefQueueEntry *entry;
	if (queue->should_be_deleted)
		return FALSE;

	entry = g_new0 (RefQueueEntry, 1);
	entry->user_data = user_data;
	entry->domain = mono_object_domain (obj);

	entry->gchandle = mono_gchandle_new_weakref (obj, TRUE);
	mono_object_register_finalizer (obj);

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
