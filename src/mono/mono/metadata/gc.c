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
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/profiler-private.h>
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

	mono_runtime_invoke (mono_class_get_finalizer (o->vtable->klass), o, NULL, &exc);

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

	if (mono_thread_current () == gc_thread)
		/* We are called from inside a finalizer, not much we can do here */
		return FALSE;

	mono_profiler_appdomain_event (domain, MONO_PROFILE_START_UNLOAD);

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

typedef enum {
	HANDLE_WEAK,
	HANDLE_WEAK_TRACK,
	HANDLE_NORMAL,
	HANDLE_PINNED
} HandleType;

static void mono_gchandle_set_target (guint32 gchandle, MonoObject *obj);

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
	return -1;
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
} HandleData;

/* weak and weak-track arrays will be allocated in malloc memory 
 * (sadly libgc still requires REVEAL/HIDE_POINTER)
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

static guint32
alloc_handle (HandleData *handles, MonoObject *obj)
{
	gint slot, i;
	lock_handles (handles);
	if (!handles->size) {
		handles->size = 32;
		if (handles->type > HANDLE_WEAK_TRACK) {
#ifdef HAVE_BOEHM_GC
			handles->entries = GC_MALLOC (sizeof (gpointer) * handles->size);
#else
			handles->entries = g_malloc0 (sizeof (gpointer) * handles->size);
#endif
		} else {
			handles->entries = g_malloc0 (sizeof (gpointer) * handles->size);
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
#ifdef HAVE_BOEHM_GC
			gpointer *entries;
			entries = GC_MALLOC (sizeof (gpointer) * new_size);
			memcpy (entries, handles->entries, sizeof (gpointer) * handles->size);
			handles->entries = entries;
#else
			handles->entries = g_realloc (handles->entries, sizeof (gpointer) * new_size);
			memset (handles->entries + handles->size, 0, sizeof (gpointer) * handles->size);
#endif
		} else {
#ifdef HAVE_BOEHM_GC
			gpointer *entries;
			entries = g_malloc (sizeof (gpointer) * new_size);
			/* we disable GC because we could lose some disappearing link updates */
			mono_gc_disable ();
			memcpy (entries, handles->entries, sizeof (gpointer) * handles->size);
			memset (entries + handles->size, 0, sizeof (gpointer) * handles->size);
			for (i = 0; i < handles->size; ++i) {
				GC_unregister_disappearing_link (&(handles->entries [i]));
				/*g_print ("reg/unreg entry %d of type %d at %p to object %p (%p), was: %p\n", i, handles->type, &(entries [i]), REVEAL_POINTER (entries [i]), entries [i], handles->entries [i]);*/
				if (entries [i] && entries [i] != (gpointer)-1) {
					GC_GENERAL_REGISTER_DISAPPEARING_LINK (&(entries [i]), REVEAL_POINTER (entries [i]));
				}
			}
			g_free (handles->entries);
			handles->entries = entries;
			mono_gc_enable ();
#else
			handles->entries = g_realloc (handles->entries, sizeof (gpointer) * new_size);
			memset (handles->entries + handles->size, 0, sizeof (gpointer) * handles->size);
#endif
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
#ifdef HAVE_BOEHM_GC
	if (handles->type <= HANDLE_WEAK_TRACK) {
		handles->entries [slot] = (void*)HIDE_POINTER (obj);
		GC_GENERAL_REGISTER_DISAPPEARING_LINK (&(handles->entries [slot]), obj);
	}
#endif

	unlock_handles (handles);
	/*g_print ("allocated entry %d of type %d to object %p (in slot: %p)\n", slot, handles->type, obj, handles->entries [slot]);*/
	return (slot << 2) | handles->type;
}

guint32
mono_gchandle_new (MonoObject *obj, gboolean pinned)
{
	return alloc_handle (&gc_handles [pinned? HANDLE_PINNED: HANDLE_NORMAL], obj);
}

guint32
mono_gchandle_new_weakref (MonoObject *obj, gboolean track_resurrection)
{
	return alloc_handle (&gc_handles [track_resurrection? HANDLE_WEAK_TRACK: HANDLE_WEAK], obj);
}

/* This will return NULL for a collected object if using a weakref handle */
MonoObject*
mono_gchandle_get_target (guint32 gchandle)
{
	guint slot = gchandle >> 2;
	HandleData *handles = &gc_handles [gchandle & 3];
	MonoObject *obj = NULL;
	lock_handles (handles);
	if (slot < handles->size) {
		obj = handles->entries [slot];
		if (handles->type <= HANDLE_WEAK_TRACK) {
			obj = REVEAL_POINTER (obj);
			if (obj == (MonoObject *) -1)
				obj = NULL;
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
	guint slot = gchandle >> 2;
	HandleData *handles = &gc_handles [gchandle & 3];
	lock_handles (handles);
	if (slot < handles->size) {
#ifdef HAVE_BOEHM_GC
		if (handles->type <= HANDLE_WEAK_TRACK) {
			GC_unregister_disappearing_link (&handles->entries [slot]);
			GC_GENERAL_REGISTER_DISAPPEARING_LINK (&(handles->entries [slot]), obj);
			handles->entries [slot] = (void*)HIDE_POINTER (obj);
		} else {
			handles->entries [slot] = obj;
		}
#else
		handles->entries [slot] = obj;
#endif
	} else {
		/* print a warning? */
	}
	/*g_print ("changed entry %d of type %d to object %p (in slot: %p)\n", slot, gchandle & 3, obj, handles->entries [slot]);*/
	unlock_handles (handles);
}

void
mono_gchandle_free (guint32 gchandle)
{
	guint slot = gchandle >> 2;
	HandleData *handles = &gc_handles [gchandle & 3];
	lock_handles (handles);
	if (slot < handles->size) {
#ifdef HAVE_BOEHM_GC
		if (handles->type <= HANDLE_WEAK_TRACK)
			GC_unregister_disappearing_link (&handles->entries [slot]);
#endif
		handles->entries [slot] = NULL;
		handles->bitmap [slot / 32] &= ~(1 << (slot % 32));
	} else {
		/* print a warning? */
	}
	/*g_print ("freed entry %d of type %d\n", slot, gchandle & 3);*/
	unlock_handles (handles);
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

void mono_gc_init (void)
{
	InitializeCriticalSection (&handle_section);
	InitializeCriticalSection (&allocator_section);

	InitializeCriticalSection (&finalizer_mutex);

#ifdef WITH_INCLUDED_LIBGC
	gc_thread_vtable = &mono_gc_thread_vtable;
#endif
	
	MONO_GC_REGISTER_ROOT (gc_handles [HANDLE_NORMAL].entries);
	MONO_GC_REGISTER_ROOT (gc_handles [HANDLE_PINNED].entries);
	GC_no_dls = TRUE;

	GC_oom_fn = mono_gc_out_of_memory;

	GC_set_warn_proc (mono_gc_warning);

#ifdef ENABLE_FINALIZER_THREAD

	if (g_getenv ("GC_DONT_GC")) {
		gc_disabled = TRUE;
		return;
	}
	
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
		if (mono_thread_current () != gc_thread) {
			finalize_notify ();
			/* Finishing the finalizer thread, so wait a little bit... */
			/* MS seems to wait for about 2 seconds */
			if (WaitForSingleObjectEx (shutdown_event, 2000000, FALSE) == WAIT_TIMEOUT) {
				mono_thread_stop (gc_thread);
			}
		}
		gc_thread = NULL;
		GC_finalizer_notifier = NULL;
	}

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

#endif

gboolean
mono_gc_is_finalizer_thread (MonoThread *thread)
{
	return thread == gc_thread;
}


