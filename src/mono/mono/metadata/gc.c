/*
 * metadata/gc.c: GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/gc-internal.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/utils/mono-logger.h>
#define GC_I_HIDE_POINTERS
#include <mono/os/gc_wrapper.h>

#ifndef HIDE_POINTER
#define HIDE_POINTER(v)         (v)
#define REVEAL_POINTER(v)       (v)
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

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

static int finalize_slot = -1;

static gboolean gc_disabled = FALSE;

static CRITICAL_SECTION finalizer_mutex;

static GSList *domains_to_finalize= NULL;

static MonoThread *gc_thread;

static void object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*));

#if HAVE_BOEHM_GC
static void finalize_notify (void);
static HANDLE pending_done_event;
static HANDLE shutdown_event;
static HANDLE thread_started_event;
#endif

/* 
 * actually, we might want to queue the finalize requests in a separate thread,
 * but we need to be careful about the execution domain of the thread...
 */
static void
run_finalize (void *obj, void *data)
{
	MonoObject *exc = NULL;
	MonoObject *o, *o2;
	o = (MonoObject*)((char*)obj + GPOINTER_TO_UINT (data));

	if (finalize_slot < 0) {
		int i;
		MonoClass* obj_class = mono_get_object_class ();
		for (i = 0; i < obj_class->vtable_size; ++i) {
			MonoMethod *cm = obj_class->vtable [i];
	       
			if (!strcmp (mono_method_get_name (cm), "Finalize")) {
				finalize_slot = i;
				break;
			}
		}
	}

	mono_domain_lock (o->vtable->domain);

	o2 = g_hash_table_lookup (o->vtable->domain->finalizable_objects_hash, o);

	mono_domain_unlock (o->vtable->domain);

	if (!o2)
		/* Already finalized somehow */
		return;

	/* make sure the finalizer is not called again if the object is resurrected */
	object_register_finalizer (obj, NULL);

	if (o->vtable->klass == mono_get_thread_class ())
		if (mono_gc_is_finalizer_thread ((MonoThread*)o))
			/* Avoid finalizing ourselves */
			return;

	/* speedup later... and use a timeout */
	/* g_print ("Finalize run on %p %s.%s\n", o, mono_object_class (o)->name_space, mono_object_class (o)->name); */

	/* Use _internal here, since this thread can enter a doomed appdomain */
	mono_domain_set_internal (mono_object_domain (o));		

	mono_runtime_invoke (o->vtable->klass->vtable [finalize_slot], o, NULL, &exc);

	if (exc) {
		/* fixme: do something useful */
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

#ifndef GC_DEBUG
	/* This assertion is not valid when GC_DEBUG is defined */
	g_assert (GC_base (obj) == (char*)obj - offset);
#endif

	if (mono_domain_is_unloading (obj->vtable->domain) && (callback != NULL))
		/*
		 * Can't register finalizers in a dying appdomain, since they
		 * could be invoked after the appdomain has been unloaded.
		 */
		return;

	mono_domain_lock (obj->vtable->domain);

	if (callback)
		g_hash_table_insert (obj->vtable->domain->finalizable_objects_hash, obj,
							 obj);
	else
		g_hash_table_remove (obj->vtable->domain->finalizable_objects_hash, obj);

	mono_domain_unlock (obj->vtable->domain);

	GC_REGISTER_FINALIZER_NO_ORDER ((char*)obj - offset, callback, GUINT_TO_POINTER (offset), NULL, NULL);
#endif
}

void
mono_object_register_finalizer (MonoObject *obj)
{
	/* g_print ("Registered finalizer on %p %s.%s\n", obj, mono_object_class (obj)->name_space, mono_object_class (obj)->name); */
	object_register_finalizer (obj, run_finalize);
}

/*
 * mono_domain_finalize:
 *
 *  Request finalization of all finalizable objects inside @domain. Wait
 * @timeout msecs for the finalization to complete.
 * Returns: TRUE if succeeded, FALSE if there was a timeout
 */

gboolean
mono_domain_finalize (MonoDomain *domain, guint32 timeout) 
{
	DomainFinalizationReq *req;
	guint32 res;
	HANDLE done_event;

	/* 
	 * No need to create another thread 'cause the finalizer thread
	 * is still working and will take care of running the finalizers
	 */ 
	
#if HAVE_BOEHM_GC
	if (gc_disabled)
		return TRUE;

	GC_gcollect ();

	done_event = CreateEvent (NULL, TRUE, FALSE, NULL);

	req = g_new0 (DomainFinalizationReq, 1);
	req->domain = domain;
	req->done_event = done_event;
	
	EnterCriticalSection (&finalizer_mutex);

	domains_to_finalize = g_slist_append (domains_to_finalize, req);

	LeaveCriticalSection (&finalizer_mutex);

	/* Tell the finalizer thread to finalize this appdomain */
	finalize_notify ();

	res = WaitForSingleObjectEx (done_event, timeout, TRUE);

	/* printf ("WAIT RES: %d.\n", res); */
	if (res == WAIT_TIMEOUT) {
		/* We leak the handle here */
		return FALSE;
	}

	CloseHandle (done_event);
	return TRUE;
#else
	/* We don't support domain finalization without a GC */
	return FALSE;
#endif
}

void
ves_icall_System_GC_InternalCollect (int generation)
{
	MONO_ARCH_SAVE_REGS;

#if HAVE_BOEHM_GC
	GC_gcollect ();
#endif
}

gint64
ves_icall_System_GC_GetTotalMemory (MonoBoolean forceCollection)
{
	MONO_ARCH_SAVE_REGS;

#if HAVE_BOEHM_GC
	if (forceCollection)
		GC_gcollect ();
	return GC_get_heap_size () - GC_get_free_bytes ();
#else
	return 0;
#endif
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
	MONO_ARCH_SAVE_REGS;

	object_register_finalizer (obj, run_finalize);
}

void
ves_icall_System_GC_SuppressFinalize (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	object_register_finalizer (obj, NULL);
}

void
ves_icall_System_GC_WaitForPendingFinalizers (void)
{
	MONO_ARCH_SAVE_REGS;
	
#if HAVE_BOEHM_GC
	if (!GC_should_invoke_finalizers ())
		return;

	if (mono_thread_current () == gc_thread)
		/* Avoid deadlocks */
		return;

	ResetEvent (pending_done_event);
	finalize_notify ();
	/* g_print ("Waiting for pending finalizers....\n"); */
	WaitForSingleObjectEx (pending_done_event, INFINITE, TRUE);
	/* g_print ("Done pending....\n"); */
#else
#endif
}

static CRITICAL_SECTION allocator_section;
static CRITICAL_SECTION handle_section;
static guint32 next_handle = 0;
static gpointer *gc_handles = NULL;
static guint8 *gc_handle_types = NULL;
static guint32 array_size = 0;

/*
 * The handle type is encoded in the lower two bits of the handle value:
 * 0 -> normal
 * 1 -> pinned
 * 2 -> weak
 */

typedef enum {
	HANDLE_WEAK,
	HANDLE_WEAK_TRACK,
	HANDLE_NORMAL,
	HANDLE_PINNED
} HandleType;

/*
 * FIXME: make thread safe and reuse the array entries.
 */
MonoObject *
ves_icall_System_GCHandle_GetTarget (guint32 handle)
{
	MonoObject *obj;
	gint32 type;

	MONO_ARCH_SAVE_REGS;

	if (gc_handles) {
		type = handle & 0x3;
		EnterCriticalSection (&handle_section);
		g_assert (type == gc_handle_types [handle >> 2]);
		obj = gc_handles [handle >> 2];
		LeaveCriticalSection (&handle_section);
		if (!obj)
			return NULL;

		if ((type == HANDLE_WEAK) || (type == HANDLE_WEAK_TRACK))
			return REVEAL_POINTER (obj);
		else
			return obj;
	}
	return NULL;
}

guint32
ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type)
{
	gpointer val = obj;
	guint32 h, idx;

	MONO_ARCH_SAVE_REGS;

	EnterCriticalSection (&handle_section);
	/* Indexes start from 1 since 0 means the handle is not allocated */
	idx = ++next_handle;
	if (idx >= array_size) {
		gpointer *new_array;
		guint8 *new_type_array;
		if (!array_size)
			array_size = 16;
#if HAVE_BOEHM_GC
		new_array = GC_MALLOC (sizeof (gpointer) * (array_size * 2));
		new_type_array = GC_MALLOC (sizeof (guint8) * (array_size * 2));
#else
		new_array = g_malloc0 (sizeof (gpointer) * (array_size * 2));
		new_type_array = g_malloc0 (sizeof (guint8) * (array_size * 2));
#endif
		if (gc_handles) {
			int i;
			memcpy (new_array, gc_handles, sizeof (gpointer) * array_size);
			memcpy (new_type_array, gc_handle_types, sizeof (guint8) * array_size);
			/* need to re-register links for weak refs. test if GC_realloc needs the same */
			for (i = 0; i < array_size; ++i) {
#if 0 /* This breaks the threaded finalizer, by causing segfaults deep
       * inside libgc.  I assume it will also break without the
       * threaded finalizer, just that the stress test (bug 31333)
       * deadlocks too early without it.  Reverting to the previous
       * version here stops the segfault.
       */
				if ((gc_handle_types[i] == HANDLE_WEAK) || (gc_handle_types[i] == HANDLE_WEAK_TRACK)) { /* all and only disguised pointers have it set */
#else
				if (((gulong)new_array [i]) & 0x1) {
#endif
#if HAVE_BOEHM_GC
					if (gc_handles [i] != (gpointer)-1)
						GC_unregister_disappearing_link (&(gc_handles [i]));
					if (new_array [i] != (gpointer)-1)
						GC_GENERAL_REGISTER_DISAPPEARING_LINK (&(new_array [i]), REVEAL_POINTER (new_array [i]));
#endif
				}
			}
		}
		array_size *= 2;
#ifndef HAVE_BOEHM_GC
		g_free (gc_handles);
		g_free (gc_handle_types);
#endif
		gc_handles = new_array;
		gc_handle_types = new_type_array;
	}

	/* resuse the type from the old target */
	if (type == -1)
		type =  handle & 0x3;
	h = (idx << 2) | type;
	switch (type) {
	case HANDLE_WEAK:
	case HANDLE_WEAK_TRACK:
		val = (gpointer)HIDE_POINTER (val);
		gc_handles [idx] = val;
		gc_handle_types [idx] = type;
#if HAVE_BOEHM_GC
		if (gc_handles [idx] != (gpointer)-1)
			GC_GENERAL_REGISTER_DISAPPEARING_LINK (&(gc_handles [idx]), obj);
#endif
		break;
	default:
		gc_handles [idx] = val;
		gc_handle_types [idx] = type;
		break;
	}
	LeaveCriticalSection (&handle_section);
	return h;
}

void
ves_icall_System_GCHandle_FreeHandle (guint32 handle)
{
	int idx = handle >> 2;
	int type = handle & 0x3;

	MONO_ARCH_SAVE_REGS;

	EnterCriticalSection (&handle_section);

#ifdef HAVE_BOEHM_GC
	g_assert (type == gc_handle_types [idx]);
	if ((type == HANDLE_WEAK) || (type == HANDLE_WEAK_TRACK)) {
		if (gc_handles [idx] != (gpointer)-1)
			GC_unregister_disappearing_link (&(gc_handles [idx]));
	}
#endif

	gc_handles [idx] = (gpointer)-1;
	gc_handle_types [idx] = (guint8)-1;
	LeaveCriticalSection (&handle_section);
}

gpointer
ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle)
{
	MonoObject *obj;
	int type = handle & 0x3;

	MONO_ARCH_SAVE_REGS;

	if (gc_handles) {
		EnterCriticalSection (&handle_section);
		obj = gc_handles [handle >> 2];
		g_assert (gc_handle_types [handle >> 2] == type);
		LeaveCriticalSection (&handle_section);
		if ((type == HANDLE_WEAK) || (type == HANDLE_WEAK_TRACK)) {
			obj = REVEAL_POINTER (obj);
			if (obj == (MonoObject *) -1)
				return NULL;
		}
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
	}
	return NULL;
}

guint32
mono_gchandle_new (MonoObject *obj, gboolean pinned)
{
	return ves_icall_System_GCHandle_GetTargetHandle (obj, 0, pinned? HANDLE_PINNED: HANDLE_NORMAL);
}

guint32
mono_gchandle_new_weakref (MonoObject *obj, gboolean track_resurrection)
{
	return ves_icall_System_GCHandle_GetTargetHandle (obj, 0, track_resurrection? HANDLE_WEAK_TRACK: HANDLE_WEAK);
}

/* This will return NULL for a collected object if using a weakref handle */
MonoObject*
mono_gchandle_get_target (guint32 gchandle)
{
	return ves_icall_System_GCHandle_GetTarget (gchandle);
}

void
mono_gchandle_free (guint32 gchandle)
{
	ves_icall_System_GCHandle_FreeHandle (gchandle);
}

#if HAVE_BOEHM_GC

static HANDLE finalizer_event;
static volatile gboolean finished=FALSE;

static void finalize_notify (void)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": prodding finalizer");
#endif

	SetEvent (finalizer_event);
}

static void
collect_objects (gpointer key, gpointer value, gpointer user_data)
{
	GPtrArray *arr = (GPtrArray*)user_data;
	g_ptr_array_add (arr, key);
}

/*
 * finalize_domain_objects:
 *
 *  Run the finalizers of all finalizable objects in req->domain.
 */
static void
finalize_domain_objects (DomainFinalizationReq *req)
{
	int i;
	GPtrArray *objs;
	MonoDomain *domain = req->domain;
	
	while (g_hash_table_size (domain->finalizable_objects_hash) > 0) {
		/* 
		 * Since the domain is unloading, nobody is allowed to put
		 * new entries into the hash table. But finalize_object might
		 * remove entries from the hash table, so we make a copy.
		 */
		objs = g_ptr_array_new ();
		g_hash_table_foreach (domain->finalizable_objects_hash, 
							  collect_objects, objs);
		/* printf ("FINALIZING %d OBJECTS.\n", objs->len); */

		for (i = 0; i < objs->len; ++i) {
			MonoObject *o = (MonoObject*)g_ptr_array_index (objs, i);
			/* FIXME: Avoid finalizing threads, etc */
			run_finalize (o, 0);
		}

		g_ptr_array_free (objs, TRUE);
	}

	/* Process finalizers which are already in the queue */
	GC_invoke_finalizers ();

	/* printf ("DONE.\n"); */
	SetEvent (req->done_event);

	/* The event is closed in mono_domain_finalize if we get here */
	g_free (req);
}

static guint32 finalizer_thread (gpointer unused)
{
	gc_thread = mono_thread_current ();

	SetEvent (thread_started_event);

	while(!finished) {
		/* Wait to be notified that there's at least one
		 * finaliser to run
		 */
		WaitForSingleObjectEx (finalizer_event, INFINITE, TRUE);

		if (domains_to_finalize) {
			EnterCriticalSection (&finalizer_mutex);
			if (domains_to_finalize) {
				DomainFinalizationReq *req = domains_to_finalize->data;
				domains_to_finalize = g_slist_remove (domains_to_finalize, req);
				LeaveCriticalSection (&finalizer_mutex);

				finalize_domain_objects (req);
			}
			else
				LeaveCriticalSection (&finalizer_mutex);
		}				

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": invoking finalizers");
#endif

		/* If finished == TRUE, mono_gc_cleanup has been called (from mono_runtime_cleanup),
		 * before the domain is unloaded.
		 *
		 * There is a bug in GC_invoke_finalizer () in versions <= 6.2alpha4:
		 * the 'mem_freed' variable is not initialized when there are no
		 * objects to finalize, which leads to strange behavior later on.
		 * The check is necessary to work around that bug.
		 */
		if (GC_should_invoke_finalizers ()) {
			GC_invoke_finalizers ();
		}

		SetEvent (pending_done_event);
	}

	SetEvent (shutdown_event);
	return(0);
}

/* 
 * Enable or disable the separate finalizer thread.
 * It's currently disabled because it still requires some
 * work in the rest of the runtime.
 */
#define ENABLE_FINALIZER_THREAD

#ifdef WITH_INCLUDED_LIBGC
/* from threads.c */
extern void mono_gc_stop_world (void);
extern void mono_gc_start_world (void);
extern void mono_gc_push_all_stacks (void);

static void mono_gc_lock (void)
{
	EnterCriticalSection (&allocator_section);
}

static void mono_gc_unlock (void)
{
	LeaveCriticalSection (&allocator_section);
}

static GCThreadFunctions mono_gc_thread_vtable = {
	NULL,

	mono_gc_lock,
	mono_gc_unlock,

	mono_gc_stop_world,
	NULL,
	mono_gc_push_all_stacks,
	mono_gc_start_world
};
#endif /* WITH_INCLUDED_LIBGC */

static void
mono_gc_warning (char *msg, GC_word arg)
{
	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_GC, msg, (unsigned long)arg);
}

static gboolean
mono_running_on_valgrind (void)
{
#ifdef HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND)
			return TRUE;
		else
			return FALSE;
#else
		return FALSE;
#endif
}


void mono_gc_init (void)
{
	InitializeCriticalSection (&handle_section);
	InitializeCriticalSection (&allocator_section);

	InitializeCriticalSection (&finalizer_mutex);

#ifdef WITH_INCLUDED_LIBGC
	gc_thread_vtable = &mono_gc_thread_vtable;
#endif
	
	MONO_GC_REGISTER_ROOT (gc_handles);
	MONO_GC_REGISTER_ROOT (gc_handle_types);
	GC_no_dls = TRUE;

	GC_oom_fn = mono_gc_out_of_memory;

	GC_set_warn_proc (mono_gc_warning);

#ifdef ENABLE_FINALIZER_THREAD

	if (g_getenv ("GC_DONT_GC")) {
		gc_disabled = TRUE;
		return;
	}
	
	/* valgrind does not play nicely with the GC,
	 * so, turn it off when we are under vg.
	 */
	/*if (mono_running_on_valgrind ()) {
		g_warning ("You are running under valgrind. Currently, valgrind does "
		           "not support the GC. This program will run with the GC "
			   "turned off. Your program may take up a fair amount of "
			   "memory. Also, finalizers will not be run.");
		
		gc_disabled = TRUE;
		return;
	}*/
	
	finalizer_event = CreateEvent (NULL, FALSE, FALSE, NULL);
	pending_done_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	shutdown_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	thread_started_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	if (finalizer_event == NULL || pending_done_event == NULL || shutdown_event == NULL || thread_started_event == NULL) {
		g_assert_not_reached ();
	}

	GC_finalize_on_demand = 1;
	GC_finalizer_notifier = finalize_notify;

	mono_thread_create (mono_domain_get (), finalizer_thread, NULL);
	/*
	 * Wait until the finalizer thread sets gc_thread since its value is needed
	 * by mono_thread_attach ()
	 */
	WaitForSingleObjectEx (thread_started_event, INFINITE, FALSE);
#endif
}

void mono_gc_cleanup (void)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": cleaning up finalizer");
#endif

#ifdef ENABLE_FINALIZER_THREAD
	if (!gc_disabled) {
		ResetEvent (shutdown_event);
		finished = TRUE;
		finalize_notify ();
		/* Finishing the finalizer thread, so wait a little bit... */
		/* MS seems to wait for about 2 seconds */
		if (WaitForSingleObjectEx (shutdown_event, 2000, FALSE) == WAIT_TIMEOUT) {
			mono_thread_stop (gc_thread);
		}
	}

#endif
}

void
mono_gc_disable (void)
{
#ifdef HAVE_GC_ENABLE
	GC_disable ();
#else
	g_assert_not_reached ();
#endif
}

void
mono_gc_enable (void)
{
#ifdef HAVE_GC_ENABLE
	GC_enable ();
#else
	g_assert_not_reached ();
#endif
}

#else

/* no Boehm GC support. */
void mono_gc_init (void)
{
	InitializeCriticalSection (&handle_section);
}

void mono_gc_cleanup (void)
{
}

void
mono_gc_disable (void)
{
}

void
mono_gc_enable (void)
{
}

#endif

gboolean
mono_gc_is_finalizer_thread (MonoThread *thread)
{
	return thread == gc_thread;
}


