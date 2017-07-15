/**
 * \file
 * GC implementation using either the installed or included Boehm GC.
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011-2012 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#include <string.h>

#define GC_I_HIDE_POINTERS
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/method-builder.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/sgen-toggleref.h>
#include <mono/metadata/w32handle.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/gc_wrapper.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-compiler.h>

#if HAVE_BOEHM_GC

#undef TRUE
#undef FALSE
#define THREAD_LOCAL_ALLOC 1
#include "private/pthread_support.h"

#if defined(PLATFORM_MACOSX) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
void *pthread_get_stackaddr_np(pthread_t);
#endif

#define GC_NO_DESCRIPTOR ((gpointer)(0 | GC_DS_LENGTH))
/*Boehm max heap cannot be smaller than 16MB*/
#define MIN_BOEHM_MAX_HEAP_SIZE_IN_MB 16
#define MIN_BOEHM_MAX_HEAP_SIZE (MIN_BOEHM_MAX_HEAP_SIZE_IN_MB << 20)

static gboolean gc_initialized = FALSE;
static mono_mutex_t mono_gc_lock;

typedef void (*GC_push_other_roots_proc)(void);

static GC_push_other_roots_proc default_push_other_roots;
static GHashTable *roots;

static void
mono_push_other_roots(void);

static void
register_test_toggleref_callback (void);

#define BOEHM_GC_BIT_FINALIZER_AWARE 1
static MonoGCFinalizerCallbacks fin_callbacks;

/* GC Handles */

static mono_mutex_t handle_section;
#define lock_handles(handles) mono_os_mutex_lock (&handle_section)
#define unlock_handles(handles) mono_os_mutex_unlock (&handle_section)

typedef struct {
	guint32  *bitmap;
	gpointer *entries;
	guint32   size;
	guint8    type;
	guint     slot_hint : 24; /* starting slot for search in bitmap */
	/* 2^16 appdomains should be enough for everyone (though I know I'll regret this in 20 years) */
	/* we alloc this only for weak refs, since we can get the domain directly in the other cases */
	guint16  *domain_ids;
} HandleData;

#define EMPTY_HANDLE_DATA(type) {NULL, NULL, 0, (type), 0, NULL}

/* weak and weak-track arrays will be allocated in malloc memory 
 */
static HandleData gc_handles [] = {
	EMPTY_HANDLE_DATA (HANDLE_WEAK),
	EMPTY_HANDLE_DATA (HANDLE_WEAK_TRACK),
	EMPTY_HANDLE_DATA (HANDLE_NORMAL),
	EMPTY_HANDLE_DATA (HANDLE_PINNED)
};

static void
mono_gc_warning (char *msg, GC_word arg)
{
	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_GC, msg, (unsigned long)arg);
}

static void on_gc_notification (GC_EventType event);
static void on_gc_heap_resize (size_t new_size);

void
mono_gc_base_init (void)
{
	char *env;

	if (gc_initialized)
		return;

	mono_counters_init ();

#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif

	/*
	 * Handle the case when we are called from a thread different from the main thread,
	 * confusing libgc.
	 * FIXME: Move this to libgc where it belongs.
	 *
	 * we used to do this only when running on valgrind,
	 * but it happens also in other setups.
	 */
#if defined(HAVE_PTHREAD_GETATTR_NP) && defined(HAVE_PTHREAD_ATTR_GETSTACK)
	{
		size_t size;
		void *sstart;
		pthread_attr_t attr;
		pthread_getattr_np (pthread_self (), &attr);
		pthread_attr_getstack (&attr, &sstart, &size);
		pthread_attr_destroy (&attr); 
		/*g_print ("stackbottom pth is: %p\n", (char*)sstart + size);*/
		/* apparently with some linuxthreads implementations sstart can be NULL,
		 * fallback to the more imprecise method (bug# 78096).
		 */
		if (sstart) {
			GC_stackbottom = (char*)sstart + size;
		} else {
			int dummy;
			gsize stack_bottom = (gsize)&dummy;
			stack_bottom += 4095;
			stack_bottom &= ~4095;
			GC_stackbottom = (char*)stack_bottom;
		}
	}
#elif defined(HAVE_PTHREAD_GET_STACKSIZE_NP) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
		GC_stackbottom = (char*)pthread_get_stackaddr_np (pthread_self ());
#elif defined(__OpenBSD__)
#  include <pthread_np.h>
	{
		stack_t ss;
		int rslt;

		rslt = pthread_stackseg_np(pthread_self(), &ss);
		g_assert (rslt == 0);

		GC_stackbottom = (char*)ss.ss_sp;
	}
#else
	{
		int dummy;
		gsize stack_bottom = (gsize)&dummy;
		stack_bottom += 4095;
		stack_bottom &= ~4095;
		/*g_print ("stackbottom is: %p\n", (char*)stack_bottom);*/
		GC_stackbottom = (char*)stack_bottom;
	}
#endif

	roots = g_hash_table_new (NULL, NULL);
	default_push_other_roots = GC_push_other_roots;
	GC_push_other_roots = mono_push_other_roots;

#if !defined(PLATFORM_ANDROID)
	/* If GC_no_dls is set to true, GC_find_limit is not called. This causes a seg fault on Android. */
	GC_no_dls = TRUE;
#endif
	{
		if ((env = g_getenv ("MONO_GC_DEBUG"))) {
			char **opts = g_strsplit (env, ",", -1);
			for (char **ptr = opts; ptr && *ptr; ptr ++) {
				char *opt = *ptr;
				if (!strcmp (opt, "do-not-finalize")) {
					mono_do_not_finalize = 1;
				} else if (!strcmp (opt, "log-finalizers")) {
					log_finalizers = 1;
				}
			}
			g_free (env);
		}
	}

	GC_init ();

	GC_set_warn_proc (mono_gc_warning);
	GC_finalize_on_demand = 1;
	GC_finalizer_notifier = mono_gc_finalize_notify;

	GC_init_gcj_malloc (5, NULL);
	GC_allow_register_threads ();

	if ((env = g_getenv ("MONO_GC_PARAMS"))) {
		char **ptr, **opts = g_strsplit (env, ",", -1);
		for (ptr = opts; *ptr; ++ptr) {
			char *opt = *ptr;
			if (g_str_has_prefix (opt, "max-heap-size=")) {
				size_t max_heap;

				opt = strchr (opt, '=') + 1;
				if (*opt && mono_gc_parse_environment_string_extract_number (opt, &max_heap)) {
					if (max_heap < MIN_BOEHM_MAX_HEAP_SIZE) {
						fprintf (stderr, "max-heap-size must be at least %dMb.\n", MIN_BOEHM_MAX_HEAP_SIZE_IN_MB);
						exit (1);
					}
					GC_set_max_heap_size (max_heap);
				} else {
					fprintf (stderr, "max-heap-size must be an integer.\n");
					exit (1);
				}
				continue;
			} else if (g_str_has_prefix (opt, "toggleref-test")) {
				register_test_toggleref_callback ();
				continue;
			} else {
				/* Could be a parameter for sgen */
				/*
				fprintf (stderr, "MONO_GC_PARAMS must be a comma-delimited list of one or more of the following:\n");
				fprintf (stderr, "  max-heap-size=N (where N is an integer, possibly with a k, m or a g suffix)\n");
				exit (1);
				*/
			}
		}
		g_free (env);
		g_strfreev (opts);
	}

	mono_thread_callbacks_init ();
	mono_thread_info_init (sizeof (MonoThreadInfo));
	mono_os_mutex_init (&mono_gc_lock);
	mono_os_mutex_init_recursive (&handle_section);

	mono_thread_info_attach ();

	GC_set_on_collection_event (on_gc_notification);
	GC_on_heap_resize = on_gc_heap_resize;

	gc_initialized = TRUE;
}

void
mono_gc_base_cleanup (void)
{
	GC_finalizer_notifier = NULL;
}

/**
 * mono_gc_collect:
 * \param generation GC generation identifier
 *
 * Perform a garbage collection for the given generation, higher numbers
 * mean usually older objects. Collecting a high-numbered generation
 * implies collecting also the lower-numbered generations.
 * The maximum value for \p generation can be retrieved with a call to
 * \c mono_gc_max_generation, so this function is usually called as:
 *
 * <code>mono_gc_collect (mono_gc_max_generation ());</code>
 */
void
mono_gc_collect (int generation)
{
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_induced++;
#endif
	GC_gcollect ();
}

/**
 * mono_gc_max_generation:
 *
 * Get the maximum generation number used by the current garbage
 * collector. The value will be 0 for the Boehm collector, 1 or more
 * for the generational collectors.
 *
 * Returns: the maximum generation number.
 */
int
mono_gc_max_generation (void)
{
	return 0;
}

/**
 * mono_gc_get_generation:
 * \param object a managed object
 *
 * Get the garbage collector's generation that \p object belongs to.
 * Use this has a hint only.
 *
 * \returns a garbage collector generation number
 */
int
mono_gc_get_generation  (MonoObject *object)
{
	return 0;
}

/**
 * mono_gc_collection_count:
 * \param generation a GC generation number
 *
 * Get how many times a garbage collection has been performed
 * for the given \p generation number.
 *
 * \returns the number of garbage collections
 */
int
mono_gc_collection_count (int generation)
{
	return GC_gc_no;
}

/**
 * mono_gc_add_memory_pressure:
 * \param value amount of bytes
 *
 * Adjust the garbage collector's view of how many bytes of memory
 * are indirectly referenced by managed objects (for example unmanaged
 * memory holding image or other binary data).
 * This is a hint only to the garbage collector algorithm.
 * Note that negative amounts of p value will decrease the memory
 * pressure.
 */
void
mono_gc_add_memory_pressure (gint64 value)
{
}

/**
 * mono_gc_get_used_size:
 *
 * Get the approximate amount of memory used by managed objects.
 *
 * Returns: the amount of memory used in bytes
 */
int64_t
mono_gc_get_used_size (void)
{
	return GC_get_heap_size () - GC_get_free_bytes ();
}

/**
 * mono_gc_get_heap_size:
 *
 * Get the amount of memory used by the garbage collector.
 *
 * Returns: the size of the heap in bytes
 */
int64_t
mono_gc_get_heap_size (void)
{
	return GC_get_heap_size ();
}

gboolean
mono_gc_is_gc_thread (void)
{
	return GC_thread_is_registered ();
}

gpointer
mono_gc_thread_attach (MonoThreadInfo* info)
{
	struct GC_stack_base sb;
	int res;

	/* TODO: use GC_get_stack_base instead of baseptr. */
	sb.mem_base = info->stack_end;
	res = GC_register_my_thread (&sb);
	if (res == GC_UNIMPLEMENTED)
	    return NULL; /* Cannot happen with GC v7+. */

	info->handle_stack = mono_handle_stack_alloc ();

	return info;
}

void
mono_gc_thread_detach_with_lock (MonoThreadInfo *p)
{
	MonoNativeThreadId tid;

	tid = mono_thread_info_get_tid (p);

	if (p->runtime_thread)
		mono_threads_add_joinable_thread ((gpointer)tid);

	mono_handle_stack_free (p->handle_stack);
}

gboolean
mono_gc_thread_in_critical_region (MonoThreadInfo *info)
{
	return FALSE;
}

gboolean
mono_object_is_alive (MonoObject* o)
{
	return GC_is_marked ((ptr_t)o);
}

int
mono_gc_walk_heap (int flags, MonoGCReferences callback, void *data)
{
	return 1;
}

static gint64 gc_start_time;

static void
on_gc_notification (GC_EventType event)
{
	MonoProfilerGCEvent e;

	switch (event) {
	case GC_EVENT_PRE_STOP_WORLD:
		e = MONO_GC_EVENT_PRE_STOP_WORLD;
		MONO_GC_WORLD_STOP_BEGIN ();
		break;

	case GC_EVENT_POST_STOP_WORLD:
		e = MONO_GC_EVENT_POST_STOP_WORLD;
		MONO_GC_WORLD_STOP_END ();
		break;

	case GC_EVENT_PRE_START_WORLD:
		e = MONO_GC_EVENT_PRE_START_WORLD;
		MONO_GC_WORLD_RESTART_BEGIN (1);
		break;

	case GC_EVENT_POST_START_WORLD:
		e = MONO_GC_EVENT_POST_START_WORLD;
		MONO_GC_WORLD_RESTART_END (1);
		break;

	case GC_EVENT_START:
		e = MONO_GC_EVENT_START;
		MONO_GC_BEGIN (1);
#ifndef DISABLE_PERFCOUNTERS
		if (mono_perfcounters)
			mono_perfcounters->gc_collections0++;
#endif
		gc_stats.major_gc_count ++;
		gc_start_time = mono_100ns_ticks ();
		break;

	case GC_EVENT_END:
		e = MONO_GC_EVENT_END;
		MONO_GC_END (1);
#if defined(ENABLE_DTRACE) && defined(__sun__)
		/* This works around a dtrace -G problem on Solaris.
		   Limit its actual use to when the probe is enabled. */
		if (MONO_GC_END_ENABLED ())
			sleep(0);
#endif

#ifndef DISABLE_PERFCOUNTERS
		if (mono_perfcounters) {
			guint64 heap_size = GC_get_heap_size ();
			guint64 used_size = heap_size - GC_get_free_bytes ();
			mono_perfcounters->gc_total_bytes = used_size;
			mono_perfcounters->gc_committed_bytes = heap_size;
			mono_perfcounters->gc_reserved_bytes = heap_size;
			mono_perfcounters->gc_gen0size = heap_size;
		}
#endif
		gc_stats.major_gc_time += mono_100ns_ticks () - gc_start_time;
		mono_trace_message (MONO_TRACE_GC, "gc took %" G_GINT64_FORMAT " usecs", (mono_100ns_ticks () - gc_start_time) / 10);
		break;
	default:
		break;
	}

	switch (event) {
	case GC_EVENT_MARK_START:
	case GC_EVENT_MARK_END:
	case GC_EVENT_RECLAIM_START:
	case GC_EVENT_RECLAIM_END:
		break;
	default:
		MONO_PROFILER_RAISE (gc_event, (e, 0));
		break;
	}

	switch (event) {
	case GC_EVENT_PRE_STOP_WORLD:
		mono_thread_info_suspend_lock ();
		MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED, 0));
		break;
	case GC_EVENT_POST_START_WORLD:
		mono_thread_info_suspend_unlock ();
		MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_POST_START_WORLD_UNLOCKED, 0));
		break;
	default:
		break;
	}
}

 
static void
on_gc_heap_resize (size_t new_size)
{
	guint64 heap_size = GC_get_heap_size ();
#ifndef DISABLE_PERFCOUNTERS
	if (mono_perfcounters) {
		mono_perfcounters->gc_committed_bytes = heap_size;
		mono_perfcounters->gc_reserved_bytes = heap_size;
		mono_perfcounters->gc_gen0size = heap_size;
	}
#endif

	MONO_PROFILER_RAISE (gc_resize, (new_size));
}

typedef struct {
	char *start;
	char *end;
} RootData;

static gpointer
register_root (gpointer arg)
{
	RootData* root_data = arg;
	g_hash_table_insert (roots, root_data->start, root_data->end);
	return NULL;
}

int
mono_gc_register_root (char *start, size_t size, void *descr, MonoGCRootSource source, const char *msg)
{
	RootData root_data;
	root_data.start = start;
	/* Boehm root processing requires one byte past end of region to be scanned */
	root_data.end = start + size + 1;
	GC_call_with_alloc_lock (register_root, &root_data);

	return TRUE;
}

int
mono_gc_register_root_wbarrier (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, const char *msg)
{
	return mono_gc_register_root (start, size, descr, source, msg);
}

static gpointer
deregister_root (gpointer arg)
{
	gboolean removed = g_hash_table_remove (roots, arg);
	g_assert (removed);
	return NULL;
}

void
mono_gc_deregister_root (char* addr)
{
	GC_call_with_alloc_lock (deregister_root, addr);
}

static void
push_root (gpointer key, gpointer value, gpointer user_data)
{
	GC_push_all (key, value);
}

static void
mono_push_other_roots (void)
{
	g_hash_table_foreach (roots, push_root, NULL);
	if (default_push_other_roots)
		default_push_other_roots ();
}

static void
mono_gc_weak_link_add (void **link_addr, MonoObject *obj, gboolean track)
{
	/* libgc requires that we use HIDE_POINTER... */
	*link_addr = (void*)HIDE_POINTER (obj);
	if (track)
		GC_REGISTER_LONG_LINK (link_addr, obj);
	else
		GC_GENERAL_REGISTER_DISAPPEARING_LINK (link_addr, obj);
}

static void
mono_gc_weak_link_remove (void **link_addr, gboolean track)
{
	if (track)
		GC_unregister_long_link (link_addr);
	else
		GC_unregister_disappearing_link (link_addr);
	*link_addr = NULL;
}

static gpointer
reveal_link (gpointer link_addr)
{
	void **link_a = (void **)link_addr;
	return REVEAL_POINTER (*link_a);
}

static MonoObject *
mono_gc_weak_link_get (void **link_addr)
{
	MonoObject *obj = (MonoObject *)GC_call_with_alloc_lock (reveal_link, link_addr);
	if (obj == (MonoObject *) -1)
		return NULL;
	return obj;
}

void*
mono_gc_make_descr_for_string (gsize *bitmap, int numbits)
{
	return mono_gc_make_descr_from_bitmap (bitmap, numbits);
}

void*
mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size)
{
	return mono_gc_make_descr_from_bitmap (bitmap, numbits);
}

void*
mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size)
{
	/* libgc has no usable support for arrays... */
	return GC_NO_DESCRIPTOR;
}

void*
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	/* It seems there are issues when the bitmap doesn't fit: play it safe */
	if (numbits >= 30)
		return GC_NO_DESCRIPTOR;
	else
		return (gpointer)GC_make_descriptor ((GC_bitmap)bitmap, numbits);
}

void*
mono_gc_make_vector_descr (void)
{
	return NULL;
}

void*
mono_gc_make_root_descr_all_refs (int numbits)
{
	return NULL;
}

void*
mono_gc_alloc_fixed (size_t size, void *descr, MonoGCRootSource source, const char *msg)
{
	return GC_MALLOC_UNCOLLECTABLE (size);
}

void
mono_gc_free_fixed (void* addr)
{
	GC_FREE (addr);
}

void *
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *obj;

	if (!vtable->klass->has_references) {
		obj = (MonoObject *)GC_MALLOC_ATOMIC (size);
		if (G_UNLIKELY (!obj))
			return NULL;

		obj->vtable = vtable;
		obj->synchronisation = NULL;

		memset ((char *) obj + sizeof (MonoObject), 0, size - sizeof (MonoObject));
	} else if (vtable->gc_descr != GC_NO_DESCRIPTOR) {
		obj = (MonoObject *)GC_GCJ_MALLOC (size, vtable);
		if (G_UNLIKELY (!obj))
			return NULL;
	} else {
		obj = (MonoObject *)GC_MALLOC (size);
		if (G_UNLIKELY (!obj))
			return NULL;

		obj->vtable = vtable;
	}

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (obj));

	return obj;
}

void *
mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	MonoArray *obj;

	if (!vtable->klass->has_references) {
		obj = (MonoArray *)GC_MALLOC_ATOMIC (size);
		if (G_UNLIKELY (!obj))
			return NULL;

		obj->obj.vtable = vtable;
		obj->obj.synchronisation = NULL;

		memset ((char *) obj + sizeof (MonoObject), 0, size - sizeof (MonoObject));
	} else if (vtable->gc_descr != GC_NO_DESCRIPTOR) {
		obj = (MonoArray *)GC_GCJ_MALLOC (size, vtable);
		if (G_UNLIKELY (!obj))
			return NULL;
	} else {
		obj = (MonoArray *)GC_MALLOC (size);
		if (G_UNLIKELY (!obj))
			return NULL;

		obj->obj.vtable = vtable;
	}

	obj->max_length = max_length;

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (&obj->obj));

	return obj;
}

void *
mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size)
{
	MonoArray *obj;

	if (!vtable->klass->has_references) {
		obj = (MonoArray *)GC_MALLOC_ATOMIC (size);
		if (G_UNLIKELY (!obj))
			return NULL;

		obj->obj.vtable = vtable;
		obj->obj.synchronisation = NULL;

		memset ((char *) obj + sizeof (MonoObject), 0, size - sizeof (MonoObject));
	} else if (vtable->gc_descr != GC_NO_DESCRIPTOR) {
		obj = (MonoArray *)GC_GCJ_MALLOC (size, vtable);
		if (G_UNLIKELY (!obj))
			return NULL;
	} else {
		obj = (MonoArray *)GC_MALLOC (size);
		if (G_UNLIKELY (!obj))
			return NULL;

		obj->obj.vtable = vtable;
	}

	obj->max_length = max_length;

	if (bounds_size)
		obj->bounds = (MonoArrayBounds *) ((char *) obj + size - bounds_size);

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (&obj->obj));

	return obj;
}

void *
mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len)
{
	MonoString *obj = (MonoString *)GC_MALLOC_ATOMIC (size);
	if (G_UNLIKELY (!obj))
		return NULL;

	obj->object.vtable = vtable;
	obj->object.synchronisation = NULL;
	obj->length = len;
	obj->chars [len] = 0;

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (&obj->object));

	return obj;
}

void*
mono_gc_alloc_mature (MonoVTable *vtable, size_t size)
{
	return mono_gc_alloc_obj (vtable, size);
}

void*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	return mono_gc_alloc_obj (vtable, size);
}

int
mono_gc_invoke_finalizers (void)
{
	/* There is a bug in GC_invoke_finalizer () in versions <= 6.2alpha4:
	 * the 'mem_freed' variable is not initialized when there are no
	 * objects to finalize, which leads to strange behavior later on.
	 * The check is necessary to work around that bug.
	 */
	if (GC_should_invoke_finalizers ())
		return GC_invoke_finalizers ();
	return 0;
}

MonoBoolean
mono_gc_pending_finalizers (void)
{
	return GC_should_invoke_finalizers ();
}

void
mono_gc_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	*(void**)field_ptr = value;
}

void
mono_gc_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	*(void**)slot_ptr = value;
}

void
mono_gc_wbarrier_arrayref_copy (gpointer dest_ptr, gpointer src_ptr, int count)
{
	mono_gc_memmove_aligned (dest_ptr, src_ptr, count * sizeof (gpointer));
}

void
mono_gc_wbarrier_generic_store (gpointer ptr, MonoObject* value)
{
	*(void**)ptr = value;
}

void
mono_gc_wbarrier_generic_store_atomic (gpointer ptr, MonoObject *value)
{
	InterlockedWritePointer ((volatile gpointer *)ptr, value);
}

void
mono_gc_wbarrier_generic_nostore (gpointer ptr)
{
}

void
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
	mono_gc_memmove_atomic (dest, src, count * mono_class_value_size (klass, NULL));
}

void
mono_gc_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	/* do not copy the sync state */
	mono_gc_memmove_aligned ((char*)obj + sizeof (MonoObject), (char*)src + sizeof (MonoObject),
			mono_object_class (obj)->instance_size - sizeof (MonoObject));
}

void
mono_gc_clear_domain (MonoDomain *domain)
{
}

void
mono_gc_suspend_finalizers (void)
{
}

int
mono_gc_get_suspend_signal (void)
{
	return GC_get_suspend_signal ();
}

int
mono_gc_get_restart_signal (void)
{
	return GC_get_thr_restart_signal ();
}

#if defined(USE_COMPILER_TLS) && defined(__linux__) && (defined(__i386__) || defined(__x86_64__))
extern __thread void* GC_thread_tls;
#include "metadata-internals.h"

static int
shift_amount (int v)
{
	int i = 0;
	while (!(v & (1 << i)))
		i++;
	return i;
}

enum {
	ATYPE_FREEPTR,
	ATYPE_FREEPTR_FOR_BOX,
	ATYPE_NORMAL,
	ATYPE_GCJ,
	ATYPE_STRING,
	ATYPE_NUM
};

static MonoMethod*
create_allocator (int atype, int tls_key, gboolean slowpath)
{
	int index_var, bytes_var, my_fl_var, my_entry_var;
	guint32 no_freelist_branch, not_small_enough_branch = 0;
	guint32 size_overflow_branch = 0;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;
	const char *name = NULL;
	WrapperInfo *info;

	g_assert_not_reached ();

	if (atype == ATYPE_FREEPTR) {
		name = slowpath ? "SlowAllocPtrfree" : "AllocPtrfree";
	} else if (atype == ATYPE_FREEPTR_FOR_BOX) {
		name = slowpath ? "SlowAllocPtrfreeBox" : "AllocPtrfreeBox";
	} else if (atype == ATYPE_NORMAL) {
		name = slowpath ? "SlowAlloc" : "Alloc";
	} else if (atype == ATYPE_GCJ) {
		name = slowpath ? "SlowAllocGcj" : "AllocGcj";
	} else if (atype == ATYPE_STRING) {
		name = slowpath ? "SlowAllocString" : "AllocString";
	} else {
		g_assert_not_reached ();
	}

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);

	if (atype == ATYPE_STRING) {
		csig->ret = &mono_defaults.string_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int32_class->byval_arg;
	} else {
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int32_class->byval_arg;
	}

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ALLOC);

	if (slowpath)
		goto always_slowpath;

	bytes_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	if (atype == ATYPE_STRING) {
		/* a string alloator method takes the args: (vtable, len) */
		/* bytes = (offsetof (MonoString, chars) + ((len + 1) * 2)); */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_SHL);
		// sizeof (MonoString) might include padding
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoString, chars));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_stloc (mb, bytes_var);
	} else {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_stloc (mb, bytes_var);
	}

	/* this is needed for strings/arrays only as the other big types are never allocated with this method */
	if (atype == ATYPE_STRING) {
		/* check for size */
		/* if (!SMALL_ENOUGH (bytes)) jump slow_path;*/
		mono_mb_emit_ldloc (mb, bytes_var);
		mono_mb_emit_icon (mb, (NFREELISTS-1) * GRANULARITY);
		not_small_enough_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BGT_UN_S);
		/* check for overflow */
		mono_mb_emit_ldloc (mb, bytes_var);
		mono_mb_emit_icon (mb, sizeof (MonoString));
		size_overflow_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BLE_UN_S);
	}

	/* int index = INDEX_FROM_BYTES(bytes); */
	index_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	
	mono_mb_emit_ldloc (mb, bytes_var);
	mono_mb_emit_icon (mb, GRANULARITY - 1);
	mono_mb_emit_byte (mb, MONO_CEE_ADD);
	mono_mb_emit_icon (mb, shift_amount (GRANULARITY));
	mono_mb_emit_byte (mb, MONO_CEE_SHR_UN);
	mono_mb_emit_icon (mb, shift_amount (sizeof (gpointer)));
	mono_mb_emit_byte (mb, MONO_CEE_SHL);
	/* index var is already adjusted into bytes */
	mono_mb_emit_stloc (mb, index_var);

	my_fl_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	my_entry_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* my_fl = ((GC_thread)tsd) -> ptrfree_freelists + index; */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, 0x0D); /* CEE_MONO_TLS */
	mono_mb_emit_i4 (mb, tls_key);
	if (atype == ATYPE_FREEPTR || atype == ATYPE_FREEPTR_FOR_BOX || atype == ATYPE_STRING)
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (struct GC_Thread_Rep, tlfs)
					+ G_STRUCT_OFFSET (struct thread_local_freelists,
							   ptrfree_freelists));
	else if (atype == ATYPE_NORMAL)
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (struct GC_Thread_Rep, tlfs)
					+ G_STRUCT_OFFSET (struct thread_local_freelists,
							   normal_freelists));
	else if (atype == ATYPE_GCJ)
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (struct GC_Thread_Rep, tlfs)
					+ G_STRUCT_OFFSET (struct thread_local_freelists,
							   gcj_freelists));
	else
		g_assert_not_reached ();
	mono_mb_emit_byte (mb, MONO_CEE_ADD);
	mono_mb_emit_ldloc (mb, index_var);
	mono_mb_emit_byte (mb, MONO_CEE_ADD);
	mono_mb_emit_stloc (mb, my_fl_var);

	/* my_entry = *my_fl; */
	mono_mb_emit_ldloc (mb, my_fl_var);
	mono_mb_emit_byte (mb, MONO_CEE_LDIND_I);
	mono_mb_emit_stloc (mb, my_entry_var);

	/* if (EXPECT((word)my_entry >= HBLKSIZE, 1)) { */
	mono_mb_emit_ldloc (mb, my_entry_var);
	mono_mb_emit_icon (mb, HBLKSIZE);
	no_freelist_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BLT_UN_S);

	/* ptr_t next = obj_link(my_entry); *my_fl = next; */
	mono_mb_emit_ldloc (mb, my_fl_var);
	mono_mb_emit_ldloc (mb, my_entry_var);
	mono_mb_emit_byte (mb, MONO_CEE_LDIND_I);
	mono_mb_emit_byte (mb, MONO_CEE_STIND_I);

	/* set the vtable and clear the words in the object */
	mono_mb_emit_ldloc (mb, my_entry_var);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, MONO_CEE_STIND_I);

	if (atype == ATYPE_FREEPTR) {
		int start_var, end_var, start_loop;
		/* end = my_entry + bytes; start = my_entry + sizeof (gpointer);
		 */
		start_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		end_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_ldloc (mb, my_entry_var);
		mono_mb_emit_ldloc (mb, bytes_var);
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_stloc (mb, end_var);
		mono_mb_emit_ldloc (mb, my_entry_var);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoObject, synchronisation));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_stloc (mb, start_var);
		/*
		 * do {
		 * 	*start++ = NULL;
		 * } while (start < end);
		 */
		start_loop = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, start_var);
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I);
		mono_mb_emit_ldloc (mb, start_var);
		mono_mb_emit_icon (mb, sizeof (gpointer));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_stloc (mb, start_var);

		mono_mb_emit_ldloc (mb, start_var);
		mono_mb_emit_ldloc (mb, end_var);
		mono_mb_emit_byte (mb, MONO_CEE_BLT_UN_S);
		mono_mb_emit_byte (mb, start_loop - (mono_mb_get_label (mb) + 1));
	} else if (atype == ATYPE_FREEPTR_FOR_BOX || atype == ATYPE_STRING) {
		/* need to clear just the sync pointer */
		mono_mb_emit_ldloc (mb, my_entry_var);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoObject, synchronisation));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I);
	}

	if (atype == ATYPE_STRING) {
		/* need to set length and clear the last char */
		/* s->length = len; */
		mono_mb_emit_ldloc (mb, my_entry_var);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoString, length));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I4);
		/* s->chars [len] = 0; */
		mono_mb_emit_ldloc (mb, my_entry_var);
		mono_mb_emit_ldloc (mb, bytes_var);
		mono_mb_emit_icon (mb, 2);
		mono_mb_emit_byte (mb, MONO_CEE_SUB);
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I2);
	}

	/* return my_entry; */
	mono_mb_emit_ldloc (mb, my_entry_var);
	mono_mb_emit_byte (mb, MONO_CEE_RET);
	
	mono_mb_patch_short_branch (mb, no_freelist_branch);
	if (not_small_enough_branch > 0)
		mono_mb_patch_short_branch (mb, not_small_enough_branch);
	if (size_overflow_branch > 0)
		mono_mb_patch_short_branch (mb, size_overflow_branch);

	/* the slow path: we just call back into the runtime */
 always_slowpath:
	if (atype == ATYPE_STRING) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, ves_icall_string_alloc);
	} else {
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icall (mb, ves_icall_object_new_specific);
	}

	mono_mb_emit_byte (mb, MONO_CEE_RET);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.alloc.gc_name = "boehm";
	info->d.alloc.alloc_type = atype;
	mb->init_locals = FALSE;

	res = mono_mb_create (mb, csig, 8, info);
	mono_mb_free (mb);

	return res;
}

static MonoMethod* alloc_method_cache [ATYPE_NUM];
static MonoMethod* slowpath_alloc_method_cache [ATYPE_NUM];

gboolean
mono_gc_is_critical_method (MonoMethod *method)
{
	int i;

	for (i = 0; i < ATYPE_NUM; ++i)
		if (method == alloc_method_cache [i] || method == slowpath_alloc_method_cache [i])
			return TRUE;

	return FALSE;
}

/*
 * If possible, generate a managed method that can quickly allocate objects in class
 * @klass. The method will typically have an thread-local inline allocation sequence.
 * The signature of the called method is:
 * 	object allocate (MonoVTable *vtable)
 * Some of the logic here is similar to mono_class_get_allocation_ftn () i object.c,
 * keep in sync.
 * The thread local alloc logic is taken from libgc/pthread_support.c.
 */

MonoMethod*
mono_gc_get_managed_allocator (MonoClass *klass, gboolean for_box, gboolean known_instance_size)
{
	int atype;

	/*
	 * Tls implementation changed, we jump to tls native getters/setters.
	 * Is boehm managed allocator ok with this ? Do we even care ?
	 */
	return NULL;

	if (!SMALL_ENOUGH (klass->instance_size))
		return NULL;
	if (mono_class_has_finalizer (klass) || mono_class_is_marshalbyref (klass))
		return NULL;
	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		return NULL;
	if (klass->rank)
		return NULL;
	if (mono_class_is_open_constructed_type (&klass->byval_arg))
		return NULL;
	if (klass->byval_arg.type == MONO_TYPE_STRING) {
		atype = ATYPE_STRING;
	} else if (!known_instance_size) {
		return NULL;
	} else if (!klass->has_references) {
		if (for_box)
			atype = ATYPE_FREEPTR_FOR_BOX;
		else
			atype = ATYPE_FREEPTR;
	} else {
		return NULL;
		/*
		 * disabled because we currently do a runtime choice anyway, to
		 * deal with multiple appdomains.
		if (vtable->gc_descr != GC_NO_DESCRIPTOR)
			atype = ATYPE_GCJ;
		else
			atype = ATYPE_NORMAL;
		*/
	}
	return mono_gc_get_managed_allocator_by_type (atype, MANAGED_ALLOCATOR_REGULAR);
}

MonoMethod*
mono_gc_get_managed_array_allocator (MonoClass *klass)
{
	return NULL;
}

/**
 * mono_gc_get_managed_allocator_by_type:
 *
 *   Return a managed allocator method corresponding to allocator type ATYPE.
 */
MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype, ManagedAllocatorVariant variant)
{
	MonoMethod *res;
	gboolean slowpath = variant != MANAGED_ALLOCATOR_REGULAR;
	MonoMethod **cache = slowpath ? slowpath_alloc_method_cache : alloc_method_cache;

	return NULL;

	res = cache [atype];
	if (res)
		return res;

	res = create_allocator (atype, -1, slowpath);
	mono_os_mutex_lock (&mono_gc_lock);
	if (cache [atype]) {
		mono_free_method (res);
		res = cache [atype];
	} else {
		mono_memory_barrier ();
		cache [atype] = res;
	}
	mono_os_mutex_unlock (&mono_gc_lock);
	return res;
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return ATYPE_NUM;
}

MonoMethod*
mono_gc_get_write_barrier (void)
{
	g_assert_not_reached ();
	return NULL;
}

#else

gboolean
mono_gc_is_critical_method (MonoMethod *method)
{
	return FALSE;
}

MonoMethod*
mono_gc_get_managed_allocator (MonoClass *klass, gboolean for_box, gboolean known_instance_size)
{
	return NULL;
}

MonoMethod*
mono_gc_get_managed_array_allocator (MonoClass *klass)
{
	return NULL;
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype, ManagedAllocatorVariant variant)
{
	return NULL;
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return 0;
}

MonoMethod*
mono_gc_get_write_barrier (void)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

MonoMethod*
mono_gc_get_specific_write_barrier (gboolean is_concurrent)
{
	g_assert_not_reached ();
	return NULL;
}

int
mono_gc_get_aligned_size_for_allocator (int size)
{
	return size;
}

const char *
mono_gc_get_gc_name (void)
{
	return "boehm";
}

void*
mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data)
{
	return GC_call_with_alloc_lock (func, data);
}

char*
mono_gc_get_description (void)
{
	return g_strdup (DEFAULT_GC_NAME);
}

void
mono_gc_set_desktop_mode (void)
{
	GC_dont_expand = 1;
}

gboolean
mono_gc_is_moving (void)
{
	return FALSE;
}

gboolean
mono_gc_is_disabled (void)
{
	if (GC_dont_gc || g_hasenv ("GC_DONT_GC"))
		return TRUE;
	else
		return FALSE;
}

void
mono_gc_wbarrier_range_copy (gpointer _dest, gpointer _src, int size)
{
	g_assert_not_reached ();
}

void*
mono_gc_get_range_copy_func (void)
{
	return &mono_gc_wbarrier_range_copy;
}

guint8*
mono_gc_get_card_table (int *shift_bits, gpointer *card_mask)
{
	g_assert_not_reached ();
	return NULL;
}

gboolean
mono_gc_card_table_nursery_check (void)
{
	g_assert_not_reached ();
	return TRUE;
}

void*
mono_gc_get_nursery (int *shift_bits, size_t *size)
{
	return NULL;
}

gboolean
mono_gc_precise_stack_mark_enabled (void)
{
	return FALSE;
}

FILE *
mono_gc_get_logfile (void)
{
	return NULL;
}

void
mono_gc_params_set (const char* options)
{
}

void
mono_gc_debug_set (const char* options)
{
}

void
mono_gc_conservatively_scan_area (void *start, void *end)
{
	g_assert_not_reached ();
}

void *
mono_gc_scan_object (void *obj, void *gc_data)
{
	g_assert_not_reached ();
	return NULL;
}

gsize*
mono_gc_get_bitmap_for_descr (void *descr, int *numbits)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_gc_set_gc_callbacks (MonoGCCallbacks *callbacks)
{
}

void
mono_gc_set_stack_end (void *stack_end)
{
}

void mono_gc_set_skip_thread (gboolean value)
{
}

void
mono_gc_register_for_finalization (MonoObject *obj, void *user_data)
{
	guint offset = 0;

#ifndef GC_DEBUG
	/* This assertion is not valid when GC_DEBUG is defined */
	g_assert (GC_base (obj) == (char*)obj - offset);
#endif

	GC_REGISTER_FINALIZER_NO_ORDER ((char*)obj - offset, (GC_finalization_proc)user_data, GUINT_TO_POINTER (offset), NULL, NULL);
}

#ifndef HOST_WIN32
int
mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg)
{
	/* it is being replaced by GC_pthread_create on some
	 * platforms, see libgc/include/gc_pthread_redirects.h */
	return pthread_create (new_thread, attr, start_routine, arg);
}
#endif

#ifdef HOST_WIN32
BOOL APIENTRY mono_gc_dllmain (HMODULE module_handle, DWORD reason, LPVOID reserved)
{
	return GC_DllMain (module_handle, reason, reserved);
}
#endif

guint
mono_gc_get_vtable_bits (MonoClass *klass)
{
	if (fin_callbacks.is_class_finalization_aware) {
		if (fin_callbacks.is_class_finalization_aware (klass))
			return BOEHM_GC_BIT_FINALIZER_AWARE;
	}
	return 0;
}

/*
 * mono_gc_register_altstack:
 *
 *   Register the dimensions of the normal stack and altstack with the collector.
 * Currently, STACK/STACK_SIZE is only used when the thread is suspended while it is on an altstack.
 */
void
mono_gc_register_altstack (gpointer stack, gint32 stack_size, gpointer altstack, gint32 altstack_size)
{
	GC_register_altstack (stack, stack_size, altstack, altstack_size);
}

int
mono_gc_get_los_limit (void)
{
	return G_MAXINT;
}

void
mono_gc_set_string_length (MonoString *str, gint32 new_length)
{
	mono_unichar2 *new_end = str->chars + new_length;
	
	/* zero the discarded string. This null-delimits the string and allows 
	 * the space to be reclaimed by SGen. */
	 
	memset (new_end, 0, (str->length - new_length + 1) * sizeof (mono_unichar2));
	str->length = new_length;
}

gboolean
mono_gc_user_markers_supported (void)
{
	return FALSE;
}

void *
mono_gc_make_root_descr_user (MonoGCRootMarkFunc marker)
{
	g_assert_not_reached ();
	return NULL;
}

/* Toggleref support */

void
mono_gc_toggleref_add (MonoObject *object, mono_bool strong_ref)
{
	if (GC_toggleref_add ((GC_PTR)object, (int)strong_ref) != GC_SUCCESS)
	    g_error ("GC_toggleref_add failed\n");
}

void
mono_gc_toggleref_register_callback (MonoToggleRefStatus (*proccess_toggleref) (MonoObject *obj))
{
	GC_set_toggleref_func ((GC_ToggleRefStatus (*) (GC_PTR obj)) proccess_toggleref);
}

/* Test support code */

static MonoToggleRefStatus
test_toggleref_callback (MonoObject *obj)
{
	static MonoClassField *mono_toggleref_test_field;
	MonoToggleRefStatus status = MONO_TOGGLE_REF_DROP;

	if (!mono_toggleref_test_field) {
		mono_toggleref_test_field = mono_class_get_field_from_name (mono_object_get_class (obj), "__test");
		g_assert (mono_toggleref_test_field);
	}

	mono_field_get_value (obj, mono_toggleref_test_field, &status);
	printf ("toggleref-cb obj %d\n", status);
	return status;
}

static void
register_test_toggleref_callback (void)
{
	mono_gc_toggleref_register_callback (test_toggleref_callback);
}

static gboolean
is_finalization_aware (MonoObject *obj)
{
	MonoVTable *vt = obj->vtable;
	return (vt->gc_bits & BOEHM_GC_BIT_FINALIZER_AWARE) == BOEHM_GC_BIT_FINALIZER_AWARE;
}

static void
fin_notifier (MonoObject *obj)
{
	if (is_finalization_aware (obj))
		fin_callbacks.object_queued_for_finalization (obj);
}

void
mono_gc_register_finalizer_callbacks (MonoGCFinalizerCallbacks *callbacks)
{
	if (callbacks->version != MONO_GC_FINALIZER_EXTENSION_VERSION)
		g_error ("Invalid finalizer callback version. Expected %d but got %d\n", MONO_GC_FINALIZER_EXTENSION_VERSION, callbacks->version);

	fin_callbacks = *callbacks;

	GC_set_await_finalize_proc ((void (*) (GC_PTR))fin_notifier);
}

#define BITMAP_SIZE (sizeof (*((HandleData *)NULL)->bitmap) * CHAR_BIT)

static inline gboolean
slot_occupied (HandleData *handles, guint slot) {
	return handles->bitmap [slot / BITMAP_SIZE] & (1 << (slot % BITMAP_SIZE));
}

static inline void
vacate_slot (HandleData *handles, guint slot) {
	handles->bitmap [slot / BITMAP_SIZE] &= ~(1 << (slot % BITMAP_SIZE));
}

static inline void
occupy_slot (HandleData *handles, guint slot) {
	handles->bitmap [slot / BITMAP_SIZE] |= 1 << (slot % BITMAP_SIZE);
}

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

static void
handle_data_alloc_entries (HandleData *handles)
{
	handles->size = 32;
	if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
		handles->entries = (void **)g_malloc0 (sizeof (*handles->entries) * handles->size);
		handles->domain_ids = (guint16 *)g_malloc0 (sizeof (*handles->domain_ids) * handles->size);
	} else {
		handles->entries = (void **)mono_gc_alloc_fixed (sizeof (*handles->entries) * handles->size, NULL, MONO_ROOT_SOURCE_GC_HANDLE, "gc handles table");
	}
	handles->bitmap = (guint32 *)g_malloc0 (handles->size / CHAR_BIT);
}

static gint
handle_data_next_unset (HandleData *handles)
{
	gint slot;
	for (slot = handles->slot_hint; slot < handles->size / BITMAP_SIZE; ++slot) {
		if (handles->bitmap [slot] == 0xffffffff)
			continue;
		handles->slot_hint = slot;
		return find_first_unset (handles->bitmap [slot]);
	}
	return -1;
}

static gint
handle_data_first_unset (HandleData *handles)
{
	gint slot;
	for (slot = 0; slot < handles->slot_hint; ++slot) {
		if (handles->bitmap [slot] == 0xffffffff)
			continue;
		handles->slot_hint = slot;
		return find_first_unset (handles->bitmap [slot]);
	}
	return -1;
}

/* Returns the index of the current slot in the bitmap. */
static void
handle_data_grow (HandleData *handles, gboolean track)
{
	guint32 *new_bitmap;
	guint32 new_size = handles->size * 2; /* always double: we memset to 0 based on this below */

	/* resize and copy the bitmap */
	new_bitmap = (guint32 *)g_malloc0 (new_size / CHAR_BIT);
	memcpy (new_bitmap, handles->bitmap, handles->size / CHAR_BIT);
	g_free (handles->bitmap);
	handles->bitmap = new_bitmap;

	/* resize and copy the entries */
	if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
		gpointer *entries;
		guint16 *domain_ids;
		gint i;
		domain_ids = (guint16 *)g_malloc0 (sizeof (*handles->domain_ids) * new_size);
		entries = (void **)g_malloc0 (sizeof (*handles->entries) * new_size);
		memcpy (domain_ids, handles->domain_ids, sizeof (*handles->domain_ids) * handles->size);
		for (i = 0; i < handles->size; ++i) {
			MonoObject *obj = mono_gc_weak_link_get (&(handles->entries [i]));
			if (obj) {
				mono_gc_weak_link_add (&(entries [i]), obj, track);
				mono_gc_weak_link_remove (&(handles->entries [i]), track);
			} else {
				g_assert (!handles->entries [i]);
			}
		}
		g_free (handles->entries);
		g_free (handles->domain_ids);
		handles->entries = entries;
		handles->domain_ids = domain_ids;
	} else {
		gpointer *entries;
		entries = (void **)mono_gc_alloc_fixed (sizeof (*handles->entries) * new_size, NULL, MONO_ROOT_SOURCE_GC_HANDLE, "gc handles table");
		mono_gc_memmove_aligned (entries, handles->entries, sizeof (*handles->entries) * handles->size);
		mono_gc_free_fixed (handles->entries);
		handles->entries = entries;
	}
	handles->slot_hint = handles->size / BITMAP_SIZE;
	handles->size = new_size;
}

static guint32
alloc_handle (HandleData *handles, MonoObject *obj, gboolean track)
{
	gint slot, i;
	guint32 res;
	lock_handles (handles);
	if (!handles->size)
		handle_data_alloc_entries (handles);
	i = handle_data_next_unset (handles);
	if (i == -1 && handles->slot_hint != 0)
		i = handle_data_first_unset (handles);
	if (i == -1) {
		handle_data_grow (handles, track);
		i = 0;
	}
	slot = handles->slot_hint * BITMAP_SIZE + i;
	occupy_slot (handles, slot);
	handles->entries [slot] = NULL;
	if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
		/*FIXME, what to use when obj == null?*/
		handles->domain_ids [slot] = (obj ? mono_object_get_domain (obj) : mono_domain_get ())->domain_id;
		if (obj)
			mono_gc_weak_link_add (&(handles->entries [slot]), obj, track);
	} else {
		handles->entries [slot] = obj;
	}

#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_num_handles++;
#endif
	unlock_handles (handles);
	res = MONO_GC_HANDLE (slot, handles->type);
	MONO_PROFILER_RAISE (gc_handle_created, (res, handles->type, obj));
	return res;
}

/**
 * mono_gchandle_new:
 * \param obj managed object to get a handle for
 * \param pinned whether the object should be pinned
 *
 * This returns a handle that wraps the object, this is used to keep a
 * reference to a managed object from the unmanaged world and preventing the
 * object from being disposed.
 * 
 * If \p pinned is false the address of the object can not be obtained, if it is
 * true the address of the object can be obtained.  This will also pin the
 * object so it will not be possible by a moving garbage collector to move the
 * object. 
 * 
 * \returns a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new (MonoObject *obj, gboolean pinned)
{
	return alloc_handle (&gc_handles [pinned? HANDLE_PINNED: HANDLE_NORMAL], obj, FALSE);
}

/**
 * mono_gchandle_new_weakref:
 * \param obj managed object to get a handle for
 * \param track_resurrection Determines how long to track the object, if this is set to TRUE, the object is tracked after finalization, if FALSE, the object is only tracked up until the point of finalization.
 *
 * This returns a weak handle that wraps the object, this is used to
 * keep a reference to a managed object from the unmanaged world.
 * Unlike the \c mono_gchandle_new the object can be reclaimed by the
 * garbage collector.  In this case the value of the GCHandle will be
 * set to zero.
 * 
 * If \p track_resurrection is TRUE the object will be tracked through
 * finalization and if the object is resurrected during the execution
 * of the finalizer, then the returned weakref will continue to hold
 * a reference to the object.   If \p track_resurrection is FALSE, then
 * the weak reference's target will become NULL as soon as the object
 * is passed on to the finalizer.
 * 
 * \returns a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new_weakref (MonoObject *obj, gboolean track_resurrection)
{
	return alloc_handle (&gc_handles [track_resurrection? HANDLE_WEAK_TRACK: HANDLE_WEAK], obj, track_resurrection);
}

/**
 * mono_gchandle_get_target:
 * \param gchandle a GCHandle's handle.
 *
 * The handle was previously created by calling \c mono_gchandle_new or
 * \c mono_gchandle_new_weakref.
 *
 * \returns A pointer to the \c MonoObject* represented by the handle or
 * NULL for a collected object if using a weakref handle.
 */
MonoObject*
mono_gchandle_get_target (guint32 gchandle)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	MonoObject *obj = NULL;
	if (type >= HANDLE_TYPE_MAX)
		return NULL;

	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			obj = mono_gc_weak_link_get (&handles->entries [slot]);
		} else {
			obj = (MonoObject *)handles->entries [slot];
		}
	} else {
		/* print a warning? */
	}
	unlock_handles (handles);
	/*g_print ("get target of entry %d of type %d: %p\n", slot, handles->type, obj);*/
	return obj;
}

void
mono_gchandle_set_target (guint32 gchandle, MonoObject *obj)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	MonoObject *old_obj = NULL;

	g_assert (type < HANDLE_TYPE_MAX);
	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			old_obj = (MonoObject *)handles->entries [slot];
			if (handles->entries [slot])
				mono_gc_weak_link_remove (&handles->entries [slot], handles->type == HANDLE_WEAK_TRACK);
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
}

gboolean
mono_gc_is_null (void)
{
	return FALSE;
}

/**
 * mono_gchandle_is_in_domain:
 * \param gchandle a GCHandle's handle.
 * \param domain An application domain.
 *
 * Use this function to determine if the \p gchandle points to an
 * object allocated in the specified \p domain.
 *
 * \returns TRUE if the object wrapped by the \p gchandle belongs to the specific \p domain.
 */
gboolean
mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	gboolean result = FALSE;

	if (type >= HANDLE_TYPE_MAX)
		return FALSE;

	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			result = domain->domain_id == handles->domain_ids [slot];
		} else {
			MonoObject *obj;
			obj = (MonoObject *)handles->entries [slot];
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
 * \param gchandle a GCHandle's handle.
 *
 * Frees the \p gchandle handle.  If there are no outstanding
 * references, the garbage collector can reclaim the memory of the
 * object wrapped. 
 */
void
mono_gchandle_free (guint32 gchandle)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	if (type >= HANDLE_TYPE_MAX)
		return;

	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			if (handles->entries [slot])
				mono_gc_weak_link_remove (&handles->entries [slot], handles->type == HANDLE_WEAK_TRACK);
		} else {
			handles->entries [slot] = NULL;
		}
		vacate_slot (handles, slot);
	} else {
		/* print a warning? */
	}
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_num_handles--;
#endif
	/*g_print ("freed entry %d of type %d\n", slot, handles->type);*/
	unlock_handles (handles);
	MONO_PROFILER_RAISE (gc_handle_deleted, (gchandle, handles->type));
}

/**
 * mono_gchandle_free_domain:
 * \param domain domain that is unloading
 *
 * Function used internally to cleanup any GC handle for objects belonging
 * to the specified domain during appdomain unload.
 */
void
mono_gchandle_free_domain (MonoDomain *domain)
{
	guint type;

	for (type = HANDLE_TYPE_MIN; type < HANDLE_PINNED; ++type) {
		guint slot;
		HandleData *handles = &gc_handles [type];
		lock_handles (handles);
		for (slot = 0; slot < handles->size; ++slot) {
			if (!slot_occupied (handles, slot))
				continue;
			if (MONO_GC_HANDLE_TYPE_IS_WEAK (type)) {
				if (domain->domain_id == handles->domain_ids [slot]) {
					vacate_slot (handles, slot);
					if (handles->entries [slot])
						mono_gc_weak_link_remove (&handles->entries [slot], handles->type == HANDLE_WEAK_TRACK);
				}
			} else {
				if (handles->entries [slot] && mono_object_domain (handles->entries [slot]) == domain) {
					vacate_slot (handles, slot);
					handles->entries [slot] = NULL;
				}
			}
		}
		unlock_handles (handles);
	}

}
#else

MONO_EMPTY_SOURCE_FILE (boehm_gc);
#endif /* no Boehm GC */
