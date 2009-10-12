/*
 * boehm-gc.c: GC implementation using either the installed or included Boehm GC.
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#include "config.h"
#define GC_I_HIDE_POINTERS
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/method-builder.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/dtrace.h>

#if HAVE_BOEHM_GC

#ifdef USE_INCLUDED_LIBGC
#undef TRUE
#undef FALSE
#define THREAD_LOCAL_ALLOC 1
#include "private/pthread_support.h"
#endif

#define GC_NO_DESCRIPTOR ((gpointer)(0 | GC_DS_LENGTH))

static gboolean gc_initialized = FALSE;

static void
mono_gc_warning (char *msg, GC_word arg)
{
	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_GC, msg, (unsigned long)arg);
}

void
mono_gc_base_init (void)
{
	if (gc_initialized)
		return;

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
#ifdef __ia64__
		/*
		 * The calculation above doesn't seem to work on ia64, also we need to set
		 * GC_register_stackbottom as well, but don't know how.
		 */
#else
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
#endif
	}
#elif defined(HAVE_PTHREAD_GET_STACKSIZE_NP) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
		GC_stackbottom = (char*)pthread_get_stackaddr_np (pthread_self ());
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

	GC_no_dls = TRUE;
	GC_init ();
	GC_oom_fn = mono_gc_out_of_memory;
	GC_set_warn_proc (mono_gc_warning);
	GC_finalize_on_demand = 1;
	GC_finalizer_notifier = mono_gc_finalize_notify;

#ifdef HAVE_GC_GCJ_MALLOC
	GC_init_gcj_malloc (5, NULL);
#endif
	mono_gc_enable_events ();
	gc_initialized = TRUE;
}

void
mono_gc_collect (int generation)
{
	MONO_PROBE_GC_BEGIN (generation);

	mono_perfcounters->gc_induced++;
	GC_gcollect ();
	
	MONO_PROBE_GC_END (generation);
#if defined(ENABLE_DTRACE) && defined(__sun__)
	/* This works around a dtrace -G problem on Solaris.
	   Limit its actual use to when the probe is enabled. */
	if (MONO_PROBE_GC_END_ENABLED ())
		sleep(0);
#endif
}

int
mono_gc_max_generation (void)
{
	return 0;
}

int
mono_gc_get_generation  (MonoObject *object)
{
	return 0;
}

int
mono_gc_collection_count (int generation)
{
	return GC_gc_no;
}

void
mono_gc_add_memory_pressure (gint64 value)
{
}

gint64
mono_gc_get_used_size (void)
{
	return GC_get_heap_size () - GC_get_free_bytes ();
}

gint64
mono_gc_get_heap_size (void)
{
	return GC_get_heap_size ();
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

gboolean
mono_gc_is_gc_thread (void)
{
#if GC_VERSION_MAJOR >= 7
	return TRUE;
#elif defined(USE_INCLUDED_LIBGC)
	return GC_thread_is_registered ();
#else
	return TRUE;
#endif
}

extern int GC_thread_register_foreign (void *base_addr);

gboolean
mono_gc_register_thread (void *baseptr)
{
#if GC_VERSION_MAJOR >= 7
	struct GC_stack_base sb;
	int res;

	res = GC_get_stack_base (&sb);
	if (res != GC_SUCCESS) {
		sb.mem_base = baseptr;
#ifdef __ia64__
		/* Can't determine the register stack bounds */
		g_error ("mono_gc_register_thread failed ().\n");
#endif
	}
	res = GC_register_my_thread (&sb);
	if ((res != GC_SUCCESS) && (res != GC_DUPLICATE)) {
		g_warning ("GC_register_my_thread () failed.\n");
		return FALSE;
	}
	return TRUE;
#else
	if (mono_gc_is_gc_thread())
		return TRUE;
#if defined(USE_INCLUDED_LIBGC) && !defined(PLATFORM_WIN32)
	return GC_thread_register_foreign (baseptr);
#else
	return FALSE;
#endif
#endif
}

gboolean
mono_object_is_alive (MonoObject* o)
{
#ifdef USE_INCLUDED_LIBGC
	return GC_is_marked ((gpointer)o);
#else
	return TRUE;
#endif
}

#ifdef USE_INCLUDED_LIBGC

static gint64 gc_start_time;

static void
on_gc_notification (GCEventType event)
{
	if (event == MONO_GC_EVENT_START) {
		mono_perfcounters->gc_collections0++;
		mono_stats.major_gc_count ++;
		gc_start_time = mono_100ns_ticks ();
	} else if (event == MONO_GC_EVENT_END) {
		guint64 heap_size = GC_get_heap_size ();
		guint64 used_size = heap_size - GC_get_free_bytes ();
		mono_perfcounters->gc_total_bytes = used_size;
		mono_perfcounters->gc_committed_bytes = heap_size;
		mono_perfcounters->gc_reserved_bytes = heap_size;
		mono_perfcounters->gc_gen0size = heap_size;
		mono_stats.major_gc_time_usecs += (mono_100ns_ticks () - gc_start_time) / 10;
		mono_trace_message (MONO_TRACE_GC, "gc took %d usecs", (mono_100ns_ticks () - gc_start_time) / 10);
	}
	mono_profiler_gc_event ((MonoGCEvent) event, 0);
}
 
static void
on_gc_heap_resize (size_t new_size)
{
	guint64 heap_size = GC_get_heap_size ();
	mono_perfcounters->gc_committed_bytes = heap_size;
	mono_perfcounters->gc_reserved_bytes = heap_size;
	mono_perfcounters->gc_gen0size = heap_size;
	mono_profiler_gc_heap_resize (new_size);
}

void
mono_gc_enable_events (void)
{
	GC_notify_event = on_gc_notification;
	GC_on_heap_resize = on_gc_heap_resize;
}

#else

void
mono_gc_enable_events (void)
{
}

#endif

int
mono_gc_register_root (char *start, size_t size, void *descr)
{
	/* for some strange reason, they want one extra byte on the end */
	GC_add_roots (start, start + size + 1);

	return TRUE;
}

void
mono_gc_deregister_root (char* addr)
{
#ifndef PLATFORM_WIN32
	/* FIXME: libgc doesn't define this work win32 for some reason */
	/* FIXME: No size info */
	GC_remove_roots (addr, addr + sizeof (gpointer) + 1);
#endif
}

void
mono_gc_weak_link_add (void **link_addr, MonoObject *obj, gboolean track)
{
	/* libgc requires that we use HIDE_POINTER... */
	*link_addr = (void*)HIDE_POINTER (obj);
	GC_GENERAL_REGISTER_DISAPPEARING_LINK (link_addr, obj);
}

void
mono_gc_weak_link_remove (void **link_addr)
{
	GC_unregister_disappearing_link (link_addr);
	*link_addr = NULL;
}

MonoObject*
mono_gc_weak_link_get (void **link_addr)
{
	MonoObject *obj = REVEAL_POINTER (*link_addr);
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
#ifdef HAVE_GC_GCJ_MALLOC
	/* It seems there are issues when the bitmap doesn't fit: play it safe */
	if (numbits >= 30)
		return GC_NO_DESCRIPTOR;
	else
		return (gpointer)GC_make_descriptor ((GC_bitmap)bitmap, numbits);
#else
	return NULL;
#endif
}

void*
mono_gc_alloc_fixed (size_t size, void *descr)
{
	/* To help track down typed allocation bugs */
	/*
	static int count;
	count ++;
	if (count == atoi (getenv ("COUNT2")))
		printf ("HIT!\n");
	if (count > atoi (getenv ("COUNT2")))
		return GC_MALLOC (size);
	*/

	if (descr)
		return GC_MALLOC_EXPLICITLY_TYPED (size, (GC_descr)descr);
	else
		return GC_MALLOC (size);
}

void
mono_gc_free_fixed (void* addr)
{
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

gboolean
mono_gc_pending_finalizers (void)
{
	return GC_should_invoke_finalizers ();
}

/*
 * LOCKING: Assumes the domain_finalizers lock is held.
 */
static void
add_weak_track_handle_internal (MonoDomain *domain, MonoObject *obj, guint32 gchandle)
{
	GSList *refs;

	if (!domain->track_resurrection_objects_hash)
		domain->track_resurrection_objects_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	refs = g_hash_table_lookup (domain->track_resurrection_objects_hash, obj);
	refs = g_slist_prepend (refs, GUINT_TO_POINTER (gchandle));
	g_hash_table_insert (domain->track_resurrection_objects_hash, obj, refs);
}

void
mono_gc_add_weak_track_handle (MonoObject *obj, guint32 handle)
{
	MonoDomain *domain;

	if (!obj)
		return;

	domain = mono_object_get_domain (obj);

	mono_domain_finalizers_lock (domain);

	add_weak_track_handle_internal (domain, obj, handle);

	g_hash_table_insert (domain->track_resurrection_handles_hash, GUINT_TO_POINTER (handle), obj);

	mono_domain_finalizers_unlock (domain);
}

/*
 * LOCKING: Assumes the domain_finalizers lock is held.
 */
static void
remove_weak_track_handle_internal (MonoDomain *domain, MonoObject *obj, guint32 gchandle)
{
	GSList *refs;

	if (!domain->track_resurrection_objects_hash)
		return;

	refs = g_hash_table_lookup (domain->track_resurrection_objects_hash, obj);
	refs = g_slist_remove (refs, GUINT_TO_POINTER (gchandle));
	g_hash_table_insert (domain->track_resurrection_objects_hash, obj, refs);
}

void
mono_gc_change_weak_track_handle (MonoObject *old_obj, MonoObject *obj, guint32 gchandle)
{
	MonoDomain *domain = mono_domain_get ();

	mono_domain_finalizers_lock (domain);

	if (old_obj)
		remove_weak_track_handle_internal (domain, old_obj, gchandle);
	if (obj)
		add_weak_track_handle_internal (domain, obj, gchandle);

	mono_domain_finalizers_unlock (domain);
}

void
mono_gc_remove_weak_track_handle (guint32 gchandle)
{
	MonoDomain *domain = mono_domain_get ();
	MonoObject *obj;

	/* Clean our entries in the two hashes in MonoDomain */

	mono_domain_finalizers_lock (domain);

	/* Get the original object this handle pointed to */
	obj = g_hash_table_lookup (domain->track_resurrection_handles_hash, GUINT_TO_POINTER (gchandle));
	if (obj) {
		g_hash_table_remove (domain->track_resurrection_handles_hash, GUINT_TO_POINTER (gchandle));

		remove_weak_track_handle_internal (domain, obj, gchandle);
	}

	mono_domain_finalizers_unlock (domain);
}

GSList*
mono_gc_remove_weak_track_object (MonoDomain *domain, MonoObject *obj)
{
	GSList *refs = NULL;

	if (domain->track_resurrection_objects_hash) {
		refs = g_hash_table_lookup (domain->track_resurrection_objects_hash, obj);

		if (refs)
			/*
			 * Since we don't run finalizers again for resurrected objects,
			 * no need to keep these around.
			 */
			g_hash_table_remove (domain->track_resurrection_objects_hash, obj);
	}

	return refs;
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
	memmove (dest_ptr, src_ptr, count * sizeof (gpointer));
}

void
mono_gc_wbarrier_generic_store (gpointer ptr, MonoObject* value)
{
	*(void**)ptr = value;
}

void
mono_gc_wbarrier_generic_nostore (gpointer ptr)
{
}

void
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
	memmove (dest, src, count * mono_class_value_size (klass, NULL));
}

void
mono_gc_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	/* do not copy the sync state */
	memcpy ((char*)obj + sizeof (MonoObject), (char*)src + sizeof (MonoObject),
			mono_object_class (obj)->instance_size - sizeof (MonoObject));
}

void
mono_gc_clear_domain (MonoDomain *domain)
{
}

#if defined(USE_INCLUDED_LIBGC) && defined(USE_COMPILER_TLS) && defined(__linux__) && (defined(__i386__) || defined(__x86_64__))
extern __thread MONO_TLS_FAST void* GC_thread_tls;
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
create_allocator (int atype, int offset)
{
	int index_var, bytes_var, my_fl_var, my_entry_var;
	guint32 no_freelist_branch, not_small_enough_branch = 0;
	guint32 size_overflow_branch = 0;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;

	if (atype == ATYPE_STRING) {
		csig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
		csig->ret = &mono_defaults.string_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int32_class->byval_arg;
	} else {
		csig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
	}

	mb = mono_mb_new (mono_defaults.object_class, "Alloc", MONO_WRAPPER_ALLOC);
	bytes_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	if (atype == ATYPE_STRING) {
		/* a string alloator method takes the args: (vtable, len) */
		/* bytes = (sizeof (MonoString) + ((len + 1) * 2)); */
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
		/* bytes = vtable->klass->instance_size */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_byte (mb, MONO_CEE_LDIND_I);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoClass, instance_size));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		/* FIXME: assert instance_size stays a 4 byte integer */
		mono_mb_emit_byte (mb, MONO_CEE_LDIND_U4);
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
	mono_mb_emit_i4 (mb, offset);
	if (atype == ATYPE_FREEPTR || atype == ATYPE_FREEPTR_FOR_BOX || atype == ATYPE_STRING)
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (struct GC_Thread_Rep, ptrfree_freelists));
	else if (atype == ATYPE_NORMAL)
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (struct GC_Thread_Rep, normal_freelists));
	else if (atype == ATYPE_GCJ)
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (struct GC_Thread_Rep, gcj_freelists));
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
	if (atype == ATYPE_STRING) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_string_alloc);
	} else {
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icall (mb, mono_object_new_specific);
	}

	mono_mb_emit_byte (mb, MONO_CEE_RET);

	res = mono_mb_create_method (mb, csig, 8);
	mono_mb_free (mb);
	mono_method_get_header (res)->init_locals = FALSE;
	return res;
}

static MonoMethod* alloc_method_cache [ATYPE_NUM];

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
mono_gc_get_managed_allocator (MonoVTable *vtable, gboolean for_box)
{
	int offset = -1;
	int atype;
	MonoClass *klass = vtable->klass;
	MONO_THREAD_VAR_OFFSET (GC_thread_tls, offset);

	/*g_print ("thread tls: %d\n", offset);*/
	if (offset == -1)
		return NULL;
	if (!SMALL_ENOUGH (klass->instance_size))
		return NULL;
	if (klass->has_finalize || klass->marshalbyref || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return NULL;
	if (klass->rank)
		return NULL;
	if (klass->byval_arg.type == MONO_TYPE_STRING) {
		atype = ATYPE_STRING;
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
	return mono_gc_get_managed_allocator_by_type (atype);
}

/**
 * mono_gc_get_managed_allocator_id:
 *
 *   Return a type for the managed allocator method MANAGED_ALLOC which can later be passed
 * to mono_gc_get_managed_allocator_by_type () to get back this allocator method. This can be
 * used by the AOT code to encode references to managed allocator methods.
 */
int
mono_gc_get_managed_allocator_type (MonoMethod *managed_alloc)
{
	int i;

	mono_loader_lock ();
	for (i = 0; i < ATYPE_NUM; ++i) {
		if (alloc_method_cache [i] == managed_alloc) {
			mono_loader_unlock ();
			return i;
		}
	}
	mono_loader_unlock ();

	return -1;
}

/**
 * mono_gc_get_managed_allocator_by_type:
 *
 *   Return a managed allocator method corresponding to allocator type ATYPE.
 */
MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype)
{
	int offset = -1;
	MonoMethod *res;
	MONO_THREAD_VAR_OFFSET (GC_thread_tls, offset);

	mono_loader_lock ();
	res = alloc_method_cache [atype];
	if (!res)
		res = alloc_method_cache [atype] = create_allocator (atype, offset);
	mono_loader_unlock ();
	return res;
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return ATYPE_NUM;
}

#else

MonoMethod*
mono_gc_get_managed_allocator (MonoVTable *vtable, gboolean for_box)
{
	return NULL;
}

int
mono_gc_get_managed_allocator_type (MonoMethod *managed_alloc)
{
	return -1;
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype)
{
	return NULL;
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return 0;
}

#endif

#endif /* no Boehm GC */

