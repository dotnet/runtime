/**
 * \file
 * GC implementation using malloc: will leak everything, just for testing.
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include <glib.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/w32handle.h>
#include <mono/metadata/abi-details.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-counters.h>
#include <mono/metadata/null-gc-handles.h>

#ifdef HAVE_NULL_GC

static gboolean gc_inited = FALSE;

void
mono_gc_base_init (void)
{
	if (gc_inited)
		return;

	mono_counters_init ();

#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif

	mono_thread_callbacks_init ();
	mono_thread_info_init (sizeof (MonoThreadInfo));

	mono_thread_info_attach ();

	null_gc_handles_init ();

	gc_inited = TRUE;
}

void
mono_gc_base_cleanup (void)
{
}

void
mono_gc_init_icalls (void)
{
}

void
mono_gc_collect (int generation)
{
}

int
mono_gc_max_generation (void)
{
	return 0;
}

guint64
mono_gc_get_allocated_bytes_for_current_thread (void)
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
	return 0;
}

void
mono_gc_stop_world ()
{
	g_assert ("mono_gc_stop_world is not supported in null GC");
}

void
mono_gc_restart_world ()
{
	g_assert ("mono_gc_restart_world is not supported in null GC");
}

void
mono_gc_add_memory_pressure (gint64 value)
{
}

/* maybe track the size, not important, though */
int64_t
mono_gc_get_used_size (void)
{
	return 1024*1024;
}

int64_t
mono_gc_get_heap_size (void)
{
	return 2*1024*1024;
}

gboolean
mono_gc_is_gc_thread (void)
{
	return TRUE;
}

int
mono_gc_walk_heap (int flags, MonoGCReferences callback, void *data)
{
	return 1;
}

gboolean
mono_object_is_alive (MonoObject* o)
{
	return TRUE;
}

int
mono_gc_register_root (char *start, size_t size, void *descr, MonoGCRootSource source, void *key, const char *msg)
{
	return TRUE;
}

int
mono_gc_register_root_wbarrier (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg)
{
	return TRUE;
}


void
mono_gc_deregister_root (char* addr)
{
}

void*
mono_gc_make_descr_for_string (gsize *bitmap, int numbits)
{
	return NULL;
}

void*
mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size)
{
	return NULL;
}

void*
mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size)
{
	return NULL;
}

void*
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	return NULL;
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

MonoObject*
mono_gc_alloc_fixed (size_t size, void *descr, MonoGCRootSource source, void *key, const char *msg)
{
	return (MonoObject*)g_malloc0 (size);
}

MonoObject*
mono_gc_alloc_fixed_no_descriptor (size_t size, MonoGCRootSource source, void *key, const char *msg)
{
	return mono_gc_alloc_fixed (size, NULL, source, key, msg);
}

void
mono_gc_free_fixed (void* addr)
{
	g_free (addr);
}

MonoObject*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *obj = g_calloc (1, size);

	obj->vtable = vtable;

	return obj;
}

MonoArray*
mono_gc_alloc_pinned_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	return mono_gc_alloc_vector (vtable, size, max_length);
}

MonoArray*
mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	MonoArray *obj = g_calloc (1, size);

	obj->obj.vtable = vtable;
	obj->max_length = max_length;

	return obj;
}

MonoArray*
mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size)
{
	MonoArray *obj = g_calloc (1, size);

	obj->obj.vtable = vtable;
	obj->max_length = max_length;

	if (bounds_size)
		obj->bounds = (MonoArrayBounds *) ((char *) obj + size - bounds_size);

	return obj;
}

MonoString*
mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len)
{
	MonoString *obj = g_calloc (1, size);

	obj->object.vtable = vtable;
	obj->length = len;
	obj->chars [len] = 0;

	return obj;
}

MonoObject*
mono_gc_alloc_mature (MonoVTable *vtable, size_t size)
{
	return mono_gc_alloc_obj (vtable, size);
}

MonoObject*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	return mono_gc_alloc_obj (vtable, size);
}

void
mono_gc_wbarrier_set_field_internal (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	*(void**)field_ptr = value;
}

void
mono_gc_wbarrier_set_arrayref_internal (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	*(void**)slot_ptr = value;
}

void
mono_gc_wbarrier_arrayref_copy_internal (gpointer dest_ptr, gconstpointer src_ptr, int count)
{
	mono_gc_memmove_aligned (dest_ptr, src_ptr, count * sizeof (gpointer));
}

void
mono_gc_wbarrier_generic_store_internal (void volatile* ptr, MonoObject* value)
{
	*(void**)ptr = value;
}

void
mono_gc_wbarrier_generic_store_atomic_internal (gpointer ptr, MonoObject *value)
{
	mono_atomic_store_ptr (ptr, value);
}

void
mono_gc_wbarrier_generic_nostore_internal (gpointer ptr)
{
}

void
mono_gc_wbarrier_value_copy_internal (gpointer dest, gconstpointer src, int count, MonoClass *klass)
{
	mono_gc_memmove_atomic (dest, src, count * mono_class_value_size (klass, NULL));
}

void
mono_gc_wbarrier_object_copy_internal (MonoObject* obj, MonoObject *src)
{
	/* do not copy the sync state */
	mono_gc_memmove_aligned (mono_object_get_data (obj), (char*)src + MONO_ABI_SIZEOF (MonoObject),
			mono_object_class (obj)->instance_size - MONO_ABI_SIZEOF (MonoObject));
}

gboolean
mono_gc_is_critical_method (MonoMethod *method)
{
	return FALSE;
}

gpointer
mono_gc_thread_attach (MonoThreadInfo* info)
{
	info->handle_stack = mono_handle_stack_alloc ();
	return info;
}

void
mono_gc_thread_detach (MonoThreadInfo *p)
{
}

void
mono_gc_thread_detach_with_lock (MonoThreadInfo *p)
{
	mono_handle_stack_free (p->handle_stack);
}

gboolean
mono_gc_thread_in_critical_region (MonoThreadInfo *info)
{
	return FALSE;
}

int
mono_gc_get_aligned_size_for_allocator (int size)
{
	return size;
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

const char *
mono_gc_get_gc_name (void)
{
	return "null";
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
	return -1;
}

int
mono_gc_get_restart_signal (void)
{
	return -1;
}

MonoMethod*
mono_gc_get_specific_write_barrier (gboolean is_concurrent)
{
	g_assert_not_reached ();
	return NULL;
}

MonoMethod*
mono_gc_get_write_barrier (void)
{
	g_assert_not_reached ();
	return NULL;
}

void*
mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data)
{
	return func (data);
}

char*
mono_gc_get_description (void)
{
	return g_strdup (DEFAULT_GC_NAME);
}

void
mono_gc_set_desktop_mode (void)
{
}

gboolean
mono_gc_is_moving (void)
{
	return FALSE;
}

gboolean
mono_gc_is_disabled (void)
{
	return FALSE;
}

void
mono_gc_wbarrier_range_copy (gpointer _dest, gconstpointer _src, int size)
{
	g_assert_not_reached ();
}

MonoRangeCopyFunction
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

guint8*
mono_gc_get_target_card_table (int *shift_bits, target_mgreg_t *card_mask)
{
	*shift_bits = 0;
	*card_mask = 0;
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

int
mono_gc_get_los_limit (void)
{
	return G_MAXINT;
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

#ifndef HOST_WIN32
int
mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg)
{
	return pthread_create (new_thread, attr, start_routine, arg);
}
#endif

void
mono_gc_skip_thread_changing (gboolean skip)
{
	// No STW, nothing needs to be done.
}

void
mono_gc_skip_thread_changed (gboolean skip)
{
}

#ifdef HOST_WIN32
BOOL APIENTRY mono_gc_dllmain (HMODULE module_handle, DWORD reason, LPVOID reserved)
{
	return TRUE;
}
#endif

MonoVTable *
mono_gc_get_vtable (MonoObject *obj)
{
	// No pointer tagging.
	return obj->vtable;
}

guint
mono_gc_get_vtable_bits (MonoClass *klass)
{
	return 0;
}

void
mono_gc_register_altstack (gpointer stack, gint32 stack_size, gpointer altstack, gint32 altstack_size)
{
}

gboolean
mono_gc_is_null (void)
{
	return TRUE;
}

int
mono_gc_invoke_finalizers (void)
{
	return 0;
}

MonoBoolean
mono_gc_pending_finalizers (void)
{
	return FALSE;
}

gboolean
mono_gc_ephemeron_array_add (MonoObject *obj)
{
	return TRUE;
}

guint64 mono_gc_get_total_allocated_bytes (MonoBoolean precise) 
{
	return 0;
}

void
mono_gc_get_gcmemoryinfo (gint64* high_memory_load_threshold_bytes,
						  gint64* memory_load_bytes,
						  gint64* total_available_memory_bytes,
						  gint64* heap_size_bytes,
						  gint64* fragmented_bytes)
{
	*high_memory_load_threshold_bytes = 0;
	*memory_load_bytes = 0;
	*total_available_memory_bytes = 0;
	*heap_size_bytes = 0;
	*fragmented_bytes = 0;
}

#else

MONO_EMPTY_SOURCE_FILE (null_gc);
#endif /* HAVE_NULL_GC */
