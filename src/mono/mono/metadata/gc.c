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
#include <errno.h>

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
#include <mono/metadata/threadpool.h>
#include <mono/metadata/threadpool-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/utils/mono-logger-internal.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/marshal.h> /* for mono_delegate_free_ftnptr () */
#include <mono/metadata/attach.h>
#include <mono/metadata/console-io.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-memory-model.h>

#ifndef HOST_WIN32
#include <pthread.h>
#endif

typedef struct DomainFinalizationReq {
	MonoDomain *domain;
	HANDLE done_event;
} DomainFinalizationReq;

#ifdef PLATFORM_WINCE /* FIXME: add accessors to gc.dll API */
extern void (*__imp_GC_finalizer_notifier)(void);
#define GC_finalizer_notifier __imp_GC_finalizer_notifier
extern int __imp_GC_finalize_on_demand;
#define GC_finalize_on_demand __imp_GC_finalize_on_demand
#endif

static gboolean gc_disabled = FALSE;

static gboolean finalizing_root_domain = FALSE;

#define mono_finalizer_lock() EnterCriticalSection (&finalizer_mutex)
#define mono_finalizer_unlock() LeaveCriticalSection (&finalizer_mutex)
static CRITICAL_SECTION finalizer_mutex;
static CRITICAL_SECTION reference_queue_mutex;

static GSList *domains_to_finalize= NULL;
static MonoMList *threads_to_finalize = NULL;

static MonoInternalThread *gc_thread;

static void object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*));

static void mono_gchandle_set_target (guint32 gchandle, MonoObject *obj);

static void reference_queue_proccess_all (void);
static void mono_reference_queue_cleanup (void);
static void reference_queue_clear_for_domain (MonoDomain *domain);
#ifndef HAVE_NULL_GC
static HANDLE pending_done_event;
static HANDLE shutdown_event;
#endif

static void
add_thread_to_finalize (MonoInternalThread *thread)
{
	mono_finalizer_lock ();
	if (!threads_to_finalize)
		MONO_GC_REGISTER_ROOT_SINGLE (threads_to_finalize);
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
	MonoObject *exc = NULL;
	MonoObject *o;
#ifndef HAVE_SGEN_GC
	MonoObject *o2;
#endif
	MonoMethod* finalizer = NULL;
	MonoDomain *caller_domain = mono_domain_get ();
	MonoDomain *domain;
	RuntimeInvokeFunction runtime_invoke;
	GSList *l, *refs = NULL;

	o = (MonoObject*)((char*)obj + GPOINTER_TO_UINT (data));

	if (suspend_finalizers)
		return;

	domain = o->vtable->domain;

#ifndef HAVE_SGEN_GC
	mono_domain_finalizers_lock (domain);

	o2 = g_hash_table_lookup (domain->finalizable_objects_hash, o);

	refs = mono_gc_remove_weak_track_object (domain, o);

	mono_domain_finalizers_unlock (domain);

	if (!o2)
		/* Already finalized somehow */
		return;
#endif

	if (refs) {
		/*
		 * Support for GCHandles of type WeakTrackResurrection:
		 *
		 *   Its not exactly clear how these are supposed to work, or how their
		 * semantics can be implemented. We only implement one crucial thing:
		 * these handles are only cleared after the finalizer has ran.
		 */
		for (l = refs; l; l = l->next) {
			guint32 gchandle = GPOINTER_TO_UINT (l->data);

			mono_gchandle_set_target (gchandle, o);
		}

		g_slist_free (refs);
	}
		
	/* make sure the finalizer is not called again if the object is resurrected */
	object_register_finalizer (obj, NULL);

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

#ifndef DISABLE_COM
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
#endif

	/* 
	 * To avoid the locking plus the other overhead of mono_runtime_invoke (),
	 * create and precompile a wrapper which calls the finalize method using
	 * a CALLVIRT.
	 */
	if (!domain->finalize_runtime_invoke) {
		MonoMethod *invoke = mono_marshal_get_runtime_invoke (mono_class_get_method_from_name_flags (mono_defaults.object_class, "Finalize", 0, 0), TRUE);

		domain->finalize_runtime_invoke = mono_compile_method (invoke);
	}

	runtime_invoke = domain->finalize_runtime_invoke;

	mono_runtime_class_init (o->vtable);

	runtime_invoke (o, NULL, &exc, NULL);

	if (exc)
		mono_internal_thread_unhandled_exception (exc);

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
#if HAVE_BOEHM_GC
	guint offset = 0;
	MonoDomain *domain;

	if (obj == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("obj"));
	
	domain = obj->vtable->domain;

#ifndef GC_DEBUG
	/* This assertion is not valid when GC_DEBUG is defined */
	g_assert (GC_base (obj) == (char*)obj - offset);
#endif

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

	GC_REGISTER_FINALIZER_NO_ORDER ((char*)obj - offset, callback, GUINT_TO_POINTER (offset), NULL, NULL);
#elif defined(HAVE_SGEN_GC)
	if (obj == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("obj"));

	/*
	 * If we register finalizers for domains that are unloading we might
	 * end up running them while or after the domain is being cleared, so
	 * the objects will not be valid anymore.
	 */
	if (!mono_domain_is_unloading (obj->vtable->domain))
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

	if (mono_thread_internal_current () == gc_thread)
		/* We are called from inside a finalizer, not much we can do here */
		return FALSE;

	/* 
	 * No need to create another thread 'cause the finalizer thread
	 * is still working and will take care of running the finalizers
	 */ 
	
#ifndef HAVE_NULL_GC
	if (gc_disabled)
		return TRUE;

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
		res = WaitForSingleObjectEx (done_event, timeout, TRUE);
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
		mono_thread_pool_cleanup ();
		mono_gc_finalize_threadpool_threads ();
	}

	return TRUE;
#else
	/* We don't support domain finalization without a GC */
	return FALSE;
#endif
}

void
ves_icall_System_GC_InternalCollect (int generation)
{
	mono_gc_collect (generation);
}

gint64
ves_icall_System_GC_GetTotalMemory (MonoBoolean forceCollection)
{
	MONO_ARCH_SAVE_REGS;

	if (forceCollection)
		mono_gc_collect (mono_gc_max_generation ());
	return mono_gc_get_used_size ();
}

void
ves_icall_System_GC_KeepAlive (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	/*
	 * Does nothing.
	 */
}

void
ves_icall_System_GC_ReRegisterForFinalize (MonoObject *obj)
{
	if (!obj)
		mono_raise_exception (mono_get_exception_argument_null ("obj"));

	object_register_finalizer (obj, mono_gc_run_finalize);
}

void
ves_icall_System_GC_SuppressFinalize (MonoObject *obj)
{
	if (!obj)
		mono_raise_exception (mono_get_exception_argument_null ("obj"));

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
#ifndef HAVE_NULL_GC
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
	WaitForSingleObjectEx (pending_done_event, INFINITE, TRUE);
	/* g_print ("Done pending....\n"); */
#endif
}

void
ves_icall_System_GC_register_ephemeron_array (MonoObject *array)
{
#ifdef HAVE_SGEN_GC
	if (!mono_gc_ephemeron_array_add (array))
		mono_raise_exception (mono_object_domain (array)->out_of_memory_ex);
#endif
}

MonoObject*
ves_icall_System_GC_get_ephemeron_tombstone (void)
{
	return mono_domain_get ()->ephemeron_tombstone;
}

#define mono_allocator_lock() EnterCriticalSection (&allocator_section)
#define mono_allocator_unlock() LeaveCriticalSection (&allocator_section)
static CRITICAL_SECTION allocator_section;
static CRITICAL_SECTION handle_section;

typedef enum {
	HANDLE_WEAK,
	HANDLE_WEAK_TRACK,
	HANDLE_NORMAL,
	HANDLE_PINNED
} HandleType;

static HandleType mono_gchandle_get_type (guint32 gchandle);

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

	if (mono_gchandle_get_type (handle) != HANDLE_PINNED)
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

typedef struct {
	guint32  *bitmap;
	gpointer *entries;
	guint32   size;
	guint8    type;
	guint     slot_hint : 24; /* starting slot for search */
	/* 2^16 appdomains should be enough for everyone (though I know I'll regret this in 20 years) */
	/* we alloc this only for weak refs, since we can get the domain directly in the other cases */
	guint16  *domain_ids;
} HandleData;

/* weak and weak-track arrays will be allocated in malloc memory 
 */
static HandleData gc_handles [] = {
	{NULL, NULL, 0, HANDLE_WEAK, 0},
	{NULL, NULL, 0, HANDLE_WEAK_TRACK, 0},
	{NULL, NULL, 0, HANDLE_NORMAL, 0},
	{NULL, NULL, 0, HANDLE_PINNED, 0}
};

#define lock_handles(handles) EnterCriticalSection (&handle_section)
#define unlock_handles(handles) LeaveCriticalSection (&handle_section)

static int
find_first_unset (guint32 bitmap)
{
	int i;
	for (i = 0; i < 32; ++i) {
		if (!(bitmap & (1 << i)))
			return i;
	}
	return -1;
}

static void*
make_root_descr_all_refs (int numbits, gboolean pinned)
{
#ifdef HAVE_SGEN_GC
	if (pinned)
		return NULL;
#endif
	return mono_gc_make_root_descr_all_refs (numbits);
}

static guint32
alloc_handle (HandleData *handles, MonoObject *obj, gboolean track)
{
	gint slot, i;
	guint32 res;
	lock_handles (handles);
	if (!handles->size) {
		handles->size = 32;
		if (handles->type > HANDLE_WEAK_TRACK) {
			handles->entries = mono_gc_alloc_fixed (sizeof (gpointer) * handles->size, make_root_descr_all_refs (handles->size, handles->type == HANDLE_PINNED));
		} else {
			handles->entries = g_malloc0 (sizeof (gpointer) * handles->size);
			handles->domain_ids = g_malloc0 (sizeof (guint16) * handles->size);
		}
		handles->bitmap = g_malloc0 (handles->size / 8);
	}
	i = -1;
	for (slot = handles->slot_hint; slot < handles->size / 32; ++slot) {
		if (handles->bitmap [slot] != 0xffffffff) {
			i = find_first_unset (handles->bitmap [slot]);
			handles->slot_hint = slot;
			break;
		}
	}
	if (i == -1 && handles->slot_hint != 0) {
		for (slot = 0; slot < handles->slot_hint; ++slot) {
			if (handles->bitmap [slot] != 0xffffffff) {
				i = find_first_unset (handles->bitmap [slot]);
				handles->slot_hint = slot;
				break;
			}
		}
	}
	if (i == -1) {
		guint32 *new_bitmap;
		guint32 new_size = handles->size * 2; /* always double: we memset to 0 based on this below */

		/* resize and copy the bitmap */
		new_bitmap = g_malloc0 (new_size / 8);
		memcpy (new_bitmap, handles->bitmap, handles->size / 8);
		g_free (handles->bitmap);
		handles->bitmap = new_bitmap;

		/* resize and copy the entries */
		if (handles->type > HANDLE_WEAK_TRACK) {
			gpointer *entries;

			entries = mono_gc_alloc_fixed (sizeof (gpointer) * new_size, make_root_descr_all_refs (new_size, handles->type == HANDLE_PINNED));
			mono_gc_memmove (entries, handles->entries, sizeof (gpointer) * handles->size);

			mono_gc_free_fixed (handles->entries);
			handles->entries = entries;
		} else {
			gpointer *entries;
			guint16 *domain_ids;
			domain_ids = g_malloc0 (sizeof (guint16) * new_size);
			entries = g_malloc (sizeof (gpointer) * new_size);
			/* we disable GC because we could lose some disappearing link updates */
			mono_gc_disable ();
			mono_gc_memmove (entries, handles->entries, sizeof (gpointer) * handles->size);
			mono_gc_bzero (entries + handles->size, sizeof (gpointer) * handles->size);
			memcpy (domain_ids, handles->domain_ids, sizeof (guint16) * handles->size);
			for (i = 0; i < handles->size; ++i) {
				MonoObject *obj = mono_gc_weak_link_get (&(handles->entries [i]));
				if (handles->entries [i])
					mono_gc_weak_link_remove (&(handles->entries [i]));
				/*g_print ("reg/unreg entry %d of type %d at %p to object %p (%p), was: %p\n", i, handles->type, &(entries [i]), obj, entries [i], handles->entries [i]);*/
				if (obj) {
					mono_gc_weak_link_add (&(entries [i]), obj, track);
				}
			}
			g_free (handles->entries);
			g_free (handles->domain_ids);
			handles->entries = entries;
			handles->domain_ids = domain_ids;
			mono_gc_enable ();
		}

		/* set i and slot to the next free position */
		i = 0;
		slot = (handles->size + 1) / 32;
		handles->slot_hint = handles->size + 1;
		handles->size = new_size;
	}
	handles->bitmap [slot] |= 1 << i;
	slot = slot * 32 + i;
	handles->entries [slot] = obj;
	if (handles->type <= HANDLE_WEAK_TRACK) {
		/*FIXME, what to use when obj == null?*/
		handles->domain_ids [slot] = (obj ? mono_object_get_domain (obj) : mono_domain_get ())->domain_id;
		if (obj)
			mono_gc_weak_link_add (&(handles->entries [slot]), obj, track);
	}

	mono_perfcounters->gc_num_handles++;
	unlock_handles (handles);
	/*g_print ("allocated entry %d of type %d to object %p (in slot: %p)\n", slot, handles->type, obj, handles->entries [slot]);*/
	res = (slot << 3) | (handles->type + 1);
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_CREATED, handles->type, res, obj);
	return res;
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
	return alloc_handle (&gc_handles [pinned? HANDLE_PINNED: HANDLE_NORMAL], obj, FALSE);
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
	guint32 handle = alloc_handle (&gc_handles [track_resurrection? HANDLE_WEAK_TRACK: HANDLE_WEAK], obj, track_resurrection);

#ifndef HAVE_SGEN_GC
	if (track_resurrection)
		mono_gc_add_weak_track_handle (obj, handle);
#endif

	return handle;
}

static HandleType
mono_gchandle_get_type (guint32 gchandle)
{
	guint type = (gchandle & 7) - 1;

	return type;
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
	guint slot = gchandle >> 3;
	guint type = (gchandle & 7) - 1;
	HandleData *handles = &gc_handles [type];
	MonoObject *obj = NULL;
	if (type > 3)
		return NULL;
	lock_handles (handles);
	if (slot < handles->size && (handles->bitmap [slot / 32] & (1 << (slot % 32)))) {
		if (handles->type <= HANDLE_WEAK_TRACK) {
			obj = mono_gc_weak_link_get (&handles->entries [slot]);
		} else {
			obj = handles->entries [slot];
		}
	} else {
		/* print a warning? */
	}
	unlock_handles (handles);
	/*g_print ("get target of entry %d of type %d: %p\n", slot, handles->type, obj);*/
	return obj;
}

static void
mono_gchandle_set_target (guint32 gchandle, MonoObject *obj)
{
	guint slot = gchandle >> 3;
	guint type = (gchandle & 7) - 1;
	HandleData *handles = &gc_handles [type];
	MonoObject *old_obj = NULL;

	if (type > 3)
		return;
	lock_handles (handles);
	if (slot < handles->size && (handles->bitmap [slot / 32] & (1 << (slot % 32)))) {
		if (handles->type <= HANDLE_WEAK_TRACK) {
			old_obj = handles->entries [slot];
			if (handles->entries [slot])
				mono_gc_weak_link_remove (&handles->entries [slot]);
			if (obj)
				mono_gc_weak_link_add (&handles->entries [slot], obj, handles->type == HANDLE_WEAK_TRACK);
			/*FIXME, what to use when obj == null?*/
			handles->domain_ids [slot] = (obj ? mono_object_get_domain (obj) : mono_domain_get ())->domain_id;
		} else {
			handles->entries [slot] = obj;
		}
	} else {
		/* print a warning? */
	}
	/*g_print ("changed entry %d of type %d to object %p (in slot: %p)\n", slot, handles->type, obj, handles->entries [slot]);*/
	unlock_handles (handles);

#ifndef HAVE_SGEN_GC
	if (type == HANDLE_WEAK_TRACK)
		mono_gc_change_weak_track_handle (old_obj, obj, gchandle);
#endif
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
	guint slot = gchandle >> 3;
	guint type = (gchandle & 7) - 1;
	HandleData *handles = &gc_handles [type];
	gboolean result = FALSE;
	if (type > 3)
		return FALSE;
	lock_handles (handles);
	if (slot < handles->size && (handles->bitmap [slot / 32] & (1 << (slot % 32)))) {
		if (handles->type <= HANDLE_WEAK_TRACK) {
			result = domain->domain_id == handles->domain_ids [slot];
		} else {
			MonoObject *obj;
			obj = handles->entries [slot];
			if (obj == NULL)
				result = TRUE;
			else
				result = domain == mono_object_domain (obj);
		}
	} else {
		/* print a warning? */
	}
	unlock_handles (handles);
	return result;
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
	guint slot = gchandle >> 3;
	guint type = (gchandle & 7) - 1;
	HandleData *handles = &gc_handles [type];
	if (type > 3)
		return;
#ifndef HAVE_SGEN_GC
	if (type == HANDLE_WEAK_TRACK)
		mono_gc_remove_weak_track_handle (gchandle);
#endif

	lock_handles (handles);
	if (slot < handles->size && (handles->bitmap [slot / 32] & (1 << (slot % 32)))) {
		if (handles->type <= HANDLE_WEAK_TRACK) {
			if (handles->entries [slot])
				mono_gc_weak_link_remove (&handles->entries [slot]);
		} else {
			handles->entries [slot] = NULL;
		}
		handles->bitmap [slot / 32] &= ~(1 << (slot % 32));
	} else {
		/* print a warning? */
	}
	mono_perfcounters->gc_num_handles--;
	/*g_print ("freed entry %d of type %d\n", slot, handles->type);*/
	unlock_handles (handles);
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_DESTROYED, handles->type, gchandle, NULL);
}

/**
 * mono_gchandle_free_domain:
 * @domain: domain that is unloading
 *
 * Function used internally to cleanup any GC handle for objects belonging
 * to the specified domain during appdomain unload.
 */
void
mono_gchandle_free_domain (MonoDomain *domain)
{
	guint type;

	for (type = 0; type < 3; ++type) {
		guint slot;
		HandleData *handles = &gc_handles [type];
		lock_handles (handles);
		for (slot = 0; slot < handles->size; ++slot) {
			if (!(handles->bitmap [slot / 32] & (1 << (slot % 32))))
				continue;
			if (type <= HANDLE_WEAK_TRACK) {
				if (domain->domain_id == handles->domain_ids [slot]) {
					handles->bitmap [slot / 32] &= ~(1 << (slot % 32));
					if (handles->entries [slot])
						mono_gc_weak_link_remove (&handles->entries [slot]);
				}
			} else {
				if (handles->entries [slot] && mono_object_domain (handles->entries [slot]) == domain) {
					handles->bitmap [slot / 32] &= ~(1 << (slot % 32));
					handles->entries [slot] = NULL;
				}
			}
		}
		unlock_handles (handles);
	}

}

MonoBoolean
GCHandle_CheckCurrentDomain (guint32 gchandle)
{
	return mono_gchandle_is_in_domain (gchandle, mono_domain_get ());
}

#ifndef HAVE_NULL_GC

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
	while (!finished) {
		/* Wait to be notified that there's at least one
		 * finaliser to run
		 */

		g_assert (mono_domain_get () == mono_get_root_domain ());

		/* An alertable wait is required so this thread can be suspended on windows */
#ifdef MONO_HAS_SEMAPHORES
		MONO_SEM_WAIT_ALERTABLE (&finalizer_sem, TRUE);
#else
		WaitForSingleObjectEx (finalizer_event, INFINITE, TRUE);
#endif

		mono_threads_perform_thread_dump ();

		mono_console_handle_async_ops ();

#ifndef DISABLE_ATTACH
		mono_attach_maybe_start ();
#endif

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

		reference_queue_proccess_all ();

		SetEvent (pending_done_event);
	}

	SetEvent (shutdown_event);
	return 0;
}

void
mono_gc_init (void)
{
	InitializeCriticalSection (&handle_section);
	InitializeCriticalSection (&allocator_section);

	InitializeCriticalSection (&finalizer_mutex);
	InitializeCriticalSection (&reference_queue_mutex);

	MONO_GC_REGISTER_ROOT_FIXED (gc_handles [HANDLE_NORMAL].entries);
	MONO_GC_REGISTER_ROOT_FIXED (gc_handles [HANDLE_PINNED].entries);

	mono_gc_base_init ();

	if (mono_gc_is_disabled ()) {
		gc_disabled = TRUE;
		return;
	}
	
	finalizer_event = CreateEvent (NULL, FALSE, FALSE, NULL);
	pending_done_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	shutdown_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	if (finalizer_event == NULL || pending_done_event == NULL || shutdown_event == NULL) {
		g_assert_not_reached ();
	}
#ifdef MONO_HAS_SEMAPHORES
	MONO_SEM_INIT (&finalizer_sem, 0);
#endif

	gc_thread = mono_thread_create_internal (mono_domain_get (), finalizer_thread, NULL, FALSE, 0);
	ves_icall_System_Threading_Thread_SetName_internal (gc_thread, mono_string_new (mono_domain_get (), "Finalizer"));
}

void
mono_gc_cleanup (void)
{
#ifdef DEBUG
	g_message ("%s: cleaning up finalizer", __func__);
#endif

	if (!gc_disabled) {
		ResetEvent (shutdown_event);
		finished = TRUE;
		if (mono_thread_internal_current () != gc_thread) {
			mono_gc_finalize_notify ();
			/* Finishing the finalizer thread, so wait a little bit... */
			/* MS seems to wait for about 2 seconds */
			if (WaitForSingleObjectEx (shutdown_event, 2000, FALSE) == WAIT_TIMEOUT) {
				int ret;

				/* Set a flag which the finalizer thread can check */
				suspend_finalizers = TRUE;

				/* Try to abort the thread, in the hope that it is running managed code */
				mono_thread_internal_stop (gc_thread);

				/* Wait for it to stop */
				ret = WaitForSingleObjectEx (gc_thread->handle, 100, TRUE);

				if (ret == WAIT_TIMEOUT) {
					/* 
					 * The finalizer thread refused to die. There is not much we 
					 * can do here, since the runtime is shutting down so the 
					 * state the finalizer thread depends on will vanish.
					 */
					g_warning ("Shutting down finalizer thread timed out.");
				} else {
					/*
					 * FIXME: On unix, when the above wait returns, the thread 
					 * might still be running io-layer code, or pthreads code.
					 */
					Sleep (100);
				}

			}
		}
		gc_thread = NULL;
#ifdef HAVE_BOEHM_GC
		GC_finalizer_notifier = NULL;
#endif
	}

	mono_reference_queue_cleanup ();

	DeleteCriticalSection (&handle_section);
	DeleteCriticalSection (&allocator_section);
	DeleteCriticalSection (&finalizer_mutex);
	DeleteCriticalSection (&reference_queue_mutex);
}

#else

/* Null GC dummy functions */
void
mono_gc_finalize_notify (void)
{
}

void mono_gc_init (void)
{
	InitializeCriticalSection (&handle_section);
}

void mono_gc_cleanup (void)
{
}

#endif

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

/**
 * mono_gc_parse_environment_string_extract_number:
 *
 * @str: points to the first digit of the number
 * @out: pointer to the variable that will receive the value
 *
 * Tries to extract a number from the passed string, taking in to account m, k
 * and g suffixes
 *
 * Returns true if passing was successful
 */
gboolean
mono_gc_parse_environment_string_extract_number (const char *str, glong *out)
{
	char *endptr;
	int len = strlen (str), shift = 0;
	glong val;
	gboolean is_suffix = FALSE;
	char suffix;

	if (!len)
		return FALSE;

	suffix = str [len - 1];

	switch (suffix) {
		case 'g':
		case 'G':
			shift += 10;
		case 'm':
		case 'M':
			shift += 10;
		case 'k':
		case 'K':
			shift += 10;
			is_suffix = TRUE;
			break;
		default:
			if (!isdigit (suffix))
				return FALSE;
			break;
	}

	errno = 0;
	val = strtol (str, &endptr, 10);

	if ((errno == ERANGE && (val == LONG_MAX || val == LONG_MIN))
			|| (errno != 0 && val == 0) || (endptr == str))
		return FALSE;

	if (is_suffix) {
		if (*(endptr + 1)) /* Invalid string. */
			return FALSE;
		val <<= shift;
	}

	*out = val;
	return TRUE;
}

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
#ifdef HAVE_SGEN_GC
		if (queue->should_be_deleted || !mono_gc_weak_link_get (&entry->dis_link)) {
			mono_gc_weak_link_remove (&entry->dis_link);
#else
		if (queue->should_be_deleted || !mono_gchandle_get_target (entry->gchandle)) {
			mono_gchandle_free ((guint32)entry->gchandle);
#endif
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
	EnterCriticalSection (&reference_queue_mutex);
	for (iter = &ref_queues; *iter;) {
		queue = *iter;
		if (!queue->should_be_deleted) {
			iter = &queue->next;
			continue;
		}
		if (queue->queue) {
			LeaveCriticalSection (&reference_queue_mutex);
			reference_queue_proccess (queue);
			goto restart;
		}
		*iter = queue->next;
		g_free (queue);
	}
	LeaveCriticalSection (&reference_queue_mutex);
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
			MonoObject *obj;
#ifdef HAVE_SGEN_GC
			obj = mono_gc_weak_link_get (&entry->dis_link);
			if (obj && mono_object_domain (obj) == domain) {
				mono_gc_weak_link_remove (&entry->dis_link);
#else
			obj = mono_gchandle_get_target (entry->gchandle);
			if (obj && mono_object_domain (obj) == domain) {
				mono_gchandle_free ((guint32)entry->gchandle);
#endif
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
 * @callback callback used when processing dead entries.
 *
 * Create a new reference queue used to process collected objects.
 * A reference queue let you queue a pair (managed object, user data)
 * using the mono_gc_reference_queue_add method.
 *
 * Once the managed object is collected @callback will be called
 * in the finalizer thread with 'user data' as argument.
 *
 * The callback is called without any locks held.
 */
MonoReferenceQueue*
mono_gc_reference_queue_new (mono_reference_queue_callback callback)
{
	MonoReferenceQueue *res = g_new0 (MonoReferenceQueue, 1);
	res->callback = callback;

	EnterCriticalSection (&reference_queue_mutex);
	res->next = ref_queues;
	ref_queues = res;
	LeaveCriticalSection (&reference_queue_mutex);

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
 * be invoked with the @obj and @user_data arguments.
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

#ifdef HAVE_SGEN_GC
	mono_gc_weak_link_add (&entry->dis_link, obj, TRUE);
#else
	entry->gchandle = mono_gchandle_new_weakref (obj, TRUE);
	mono_object_register_finalizer (obj);
#endif

	ref_list_push (&queue->queue, entry);
	return TRUE;
}

/**
 * mono_gc_reference_queue_free:
 * @queue the queue that should be deleted.
 *
 * This operation signals that @queue should be deleted. This operation is deferred
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

#define ptr_mask ((sizeof (void*) - 1))
#define _toi(ptr) ((size_t)ptr)
#define unaligned_bytes(ptr) (_toi(ptr) & ptr_mask)
#define align_down(ptr) ((void*)(_toi(ptr) & ~ptr_mask))
#define align_up(ptr) ((void*) ((_toi(ptr) + ptr_mask) & ~ptr_mask))

/**
 * mono_gc_bzero:
 * @dest: address to start to clear
 * @size: size of the region to clear
 *
 * Zero @size bytes starting at @dest.
 *
 * Use this to zero memory that can hold managed pointers.
 *
 * FIXME borrow faster code from some BSD libc or bionic
 */
void
mono_gc_bzero (void *dest, size_t size)
{
	char *p = (char*)dest;
	char *end = p + size;
	char *align_end = align_up (p);
	char *word_end;

	while (p < align_end)
		*p++ = 0;

	word_end = align_down (end);
	while (p < word_end) {
		*((void**)p) = NULL;
		p += sizeof (void*);
	}

	while (p < end)
		*p++ = 0;
}


/**
 * mono_gc_memmove:
 * @dest: destination of the move
 * @src: source
 * @size: size of the block to move
 *
 * Move @size bytes from @src to @dest.
 * size MUST be a multiple of sizeof (gpointer)
 *
 * FIXME borrow faster code from some BSD libc or bionic
 */
void
mono_gc_memmove (void *dest, const void *src, size_t size)
{
	/*
	 * If dest and src are differently aligned with respect to
	 * pointer size then it makes no sense to do aligned copying.
	 * In fact, we would end up with unaligned loads which is
	 * incorrect on some architectures.
	 */
	if ((char*)dest - (char*)align_down (dest) != (char*)src - (char*)align_down (src)) {
		memmove (dest, src, size);
		return;
	}

	/*
	 * A bit of explanation on why we align only dest before doing word copies.
	 * Pointers to managed objects must always be stored in word aligned addresses, so
	 * even if dest is misaligned, src will be by the same amount - this ensure proper atomicity of reads.
	 */
	if (dest > src && ((size_t)((char*)dest - (char*)src) < size)) {
		char *p = (char*)dest + size;
		char *s = (char*)src + size;
		char *start = (char*)dest;
		char *align_end = MAX((char*)dest, (char*)align_down (p));
		char *word_start;

		while (p > align_end)
			*--p = *--s;

		word_start = align_up (start);
		while (p > word_start) {
			p -= sizeof (void*);
			s -= sizeof (void*);
			*((void**)p) = *((void**)s);
		}

		while (p > start)
			*--p = *--s;
	} else {
		char *p = (char*)dest;
		char *s = (char*)src;
		char *end = p + size;
		char *align_end = MIN ((char*)end, (char*)align_up (p));
		char *word_end;

		while (p < align_end)
			*p++ = *s++;

		word_end = align_down (end);
		while (p < word_end) {
			*((void**)p) = *((void**)s);
			p += sizeof (void*);
			s += sizeof (void*);
		}

		while (p < end)
			*p++ = *s++;
	}
}


