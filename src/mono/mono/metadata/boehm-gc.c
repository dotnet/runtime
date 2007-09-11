/*
 * boehm-gc.c: GC implementation using either the installed or included Boehm GC.
 *
 */

#include "config.h"
#define GC_I_HIDE_POINTERS
#include <mono/os/gc_wrapper.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/opcodes.h>
#include <mono/utils/mono-logger.h>

#if HAVE_BOEHM_GC

#ifdef USE_INCLUDED_LIBGC
#undef TRUE
#undef FALSE
#define THREAD_LOCAL_ALLOC 1
#include "private/pthread_support.h"
#endif

static void
mono_gc_warning (char *msg, GC_word arg)
{
	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_GC, msg, (unsigned long)arg);
}

void
mono_gc_base_init (void)
{
	GC_no_dls = TRUE;
	GC_oom_fn = mono_gc_out_of_memory;
	GC_set_warn_proc (mono_gc_warning);
	GC_finalize_on_demand = 1;
	GC_finalizer_notifier = mono_gc_finalize_notify;
}

void
mono_gc_collect (int generation)
{
	GC_gcollect ();
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
#ifdef USE_INCLUDED_LIBGC
	return GC_thread_is_registered ();
#else
	return TRUE;
#endif
}

extern int GC_thread_register_foreign (void *base_addr);

gboolean
mono_gc_register_thread (void *baseptr)
{
	if (mono_gc_is_gc_thread())
		return TRUE;
#if defined(USE_INCLUDED_LIBGC) && !defined(PLATFORM_WIN32)
	return GC_thread_register_foreign (baseptr);
#else
	return FALSE;
#endif
}

gboolean
mono_object_is_alive (MonoObject* o)
{
#ifdef USE_INCLUDED_LIBGC
	return GC_is_marked (o);
#else
	return TRUE;
#endif
}

#ifdef USE_INCLUDED_LIBGC

static void
on_gc_notification (GCEventType event)
{
	mono_profiler_gc_event ((MonoGCEvent) event, 0);
}
 
static void
on_gc_heap_resize (size_t new_size)
{
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

void
mono_gc_weak_link_add (void **link_addr, MonoObject *obj)
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
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	return NULL;
}

void*
mono_gc_alloc_fixed (size_t size, void *descr)
{
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
mono_gc_wbarrier_arrayref_copy (MonoArray *arr, gpointer slot_ptr, int count)
{
	/* no need to do anything */
}

void
mono_gc_wbarrier_generic_store (gpointer ptr, MonoObject* value)
{
	*(void**)ptr = value;
}

void
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
}

void
mono_gc_wbarrier_object (MonoObject *object)
{
}

#if defined(USE_INCLUDED_LIBGC) && defined(__linux__) && defined(__i386__)
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
	ATYPE_NUM
};

static MonoMethod*
create_allocator (int atype, int offset)
{
	int index_var, bytes_var, my_fl_var, my_entry_var;
	guint32 no_freelist_branch, not_small_enough_branch = 0;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	csig->ret = &mono_defaults.object_class->byval_arg;
	csig->params [0] = &mono_defaults.int_class->byval_arg;
	
	mb = mono_mb_new (mono_defaults.object_class, "Alloc", MONO_WRAPPER_MANAGED_TO_MANAGED);
	bytes_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
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

#if 0
	/* this is needed for strings/arrays only as the other big types are never allocated with this method */
	if (atype != ATYPE_FREEPTR && atype != ATYPE_FREEPTR_FOR_BOX) {
		/* check for size */
		/* if (!SMALL_ENOUGH (bytes)) jump slow_path;*/
		mono_mb_emit_ldloc (mb, bytes_var);
		mono_mb_emit_icon (mb, (NFREELISTS-1) * GRANULARITY);
		not_small_enough_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BGT_UN_S);
	}
#endif

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
	if (atype == ATYPE_FREEPTR || atype == ATYPE_FREEPTR_FOR_BOX)
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
//		g_print ("distance: %d\n", start_loop - (mono_mb_get_label (mb) + 1));
		mono_mb_emit_byte (mb, start_loop - (mono_mb_get_label (mb) + 1));
	} else if (atype == ATYPE_FREEPTR_FOR_BOX) {
		/* need to clear just the sync pointer */
		mono_mb_emit_ldloc (mb, my_entry_var);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoObject, synchronisation));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I);
	}

	/* return my_entry; */
	mono_mb_emit_ldloc (mb, my_entry_var);
	mono_mb_emit_byte (mb, MONO_CEE_RET);
	
	mono_mb_patch_short_branch (mb, no_freelist_branch);
	if (not_small_enough_branch > 0)
		mono_mb_patch_short_branch (mb, not_small_enough_branch);
	/* the slow path: we just call back into the runtime */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icall (mb, mono_object_new_specific);

	mono_mb_emit_byte (mb, MONO_CEE_RET);

	res = mono_mb_create_method (mb, csig, 8);
	mono_mb_free (mb);
	mono_method_get_header (res)->init_locals = FALSE;
	return res;
}

static MonoMethod* alloc_method_cache [ATYPE_NUM];
#define GC_NO_DESCRIPTOR ((gpointer)(0 | GC_DS_LENGTH))

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
	MonoMethod *res;
	MONO_THREAD_VAR_OFFSET (GC_thread_tls, offset);
	/*g_print ("thread tls: %d\n", offset);*/
	if (offset == -1)
		return NULL;
	if (!SMALL_ENOUGH (klass->instance_size))
		return NULL;
	if (klass->has_finalize || klass->marshalbyref || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return NULL;
	if (klass->rank || klass->byval_arg.type == MONO_TYPE_STRING)
		return NULL;
	/* now we have only the simple cases: ptrfree, gcj, non-gcj: we handle only the first as a test */
	if (!klass->has_references) {
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
	mono_loader_lock ();
	res = alloc_method_cache [atype];
	if (!res)
		res = alloc_method_cache [atype] = create_allocator (atype, offset);
	mono_loader_unlock ();
	return res;
}

#else

MonoMethod*
mono_gc_get_managed_allocator (MonoVTable *vtable, gboolean for_box)
{
	return NULL;
}

#endif

#endif /* no Boehm GC */

