/*
 * sgen-cardtable.c: Card table implementation for sgen
 *
 * Author:
 * 	Rodrigo Kumpera (rkumpera@novell.com)
 *
 * SGen is licensed under the terms of the MIT X11 license
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-ssb.h"
#include "metadata/sgen-protocol.h"
#include "utils/mono-counters.h"

/*A two slots cache for recently inserted remsets */
static gpointer global_remset_cache [2];

static LOCK_DECLARE (global_remset_mutex);

#define LOCK_GLOBAL_REMSET mono_mutex_lock (&global_remset_mutex)
#define UNLOCK_GLOBAL_REMSET mono_mutex_unlock (&global_remset_mutex)


static void
clear_thread_store_remset_buffer (SgenThreadInfo *info)
{
	*info->store_remset_buffer_index_addr = 0;
	/* See the comment at the end of sgen_thread_unregister() */
	if (*info->store_remset_buffer_addr)
		memset (*info->store_remset_buffer_addr, 0, sizeof (gpointer) * STORE_REMSET_BUFFER_SIZE);
}

static size_t
remset_byte_size (RememberedSet *remset)
{
	return sizeof (RememberedSet) + (remset->end_set - remset->data) * sizeof (gpointer);
}

static void
add_generic_store_remset_from_buffer (gpointer *buffer)
{
	GenericStoreRememberedSet *remset = mono_sgen_alloc_internal (INTERNAL_MEM_STORE_REMSET);
	memcpy (remset->data, buffer + 1, sizeof (gpointer) * (STORE_REMSET_BUFFER_SIZE - 1));
	remset->next = generic_store_remsets;
	generic_store_remsets = remset;
}

static void
evacuate_remset_buffer (void)
{
	gpointer *buffer;
	TLAB_ACCESS_INIT;

	buffer = STORE_REMSET_BUFFER;

	add_generic_store_remset_from_buffer (buffer);
	memset (buffer, 0, sizeof (gpointer) * STORE_REMSET_BUFFER_SIZE);

	STORE_REMSET_BUFFER_INDEX = 0;
}


void
mono_sgen_ssb_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	RememberedSet *rs;
	TLAB_ACCESS_INIT;

	LOCK_GC;
	rs = REMEMBERED_SET;
	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)field_ptr;
		*(void**)field_ptr = value;
		UNLOCK_GC;
		return;
	}
	rs = mono_sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
#endif
	*(rs->store_next++) = (mword)field_ptr;
	*(void**)field_ptr = value;
	UNLOCK_GC;
}

void
mono_sgen_ssb_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	RememberedSet *rs;
	TLAB_ACCESS_INIT;

	LOCK_GC;
	rs = REMEMBERED_SET;
	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)slot_ptr;
		*(void**)slot_ptr = value;
		UNLOCK_GC;
		return;
	}
	rs = mono_sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
#endif
	*(rs->store_next++) = (mword)slot_ptr;
	*(void**)slot_ptr = value;
	UNLOCK_GC;
}

void
mono_sgen_ssb_wbarrier_arrayref_copy (gpointer dest_ptr, gpointer src_ptr, int count)
{
	RememberedSet *rs;
	TLAB_ACCESS_INIT;
	LOCK_GC;
	mono_gc_memmove (dest_ptr, src_ptr, count * sizeof (gpointer));

	rs = REMEMBERED_SET;
	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p, %d\n", dest_ptr, count));
	if (rs->store_next + 1 < rs->end_set) {
		*(rs->store_next++) = (mword)dest_ptr | REMSET_RANGE;
		*(rs->store_next++) = count;
		UNLOCK_GC;
		return;
	}
	rs = mono_sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
#endif
	*(rs->store_next++) = (mword)dest_ptr | REMSET_RANGE;
	*(rs->store_next++) = count;

	UNLOCK_GC;
}

void
mono_sgen_ssb_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
	RememberedSet *rs;
	size_t element_size = mono_class_value_size (klass, NULL);
	size_t size = count * element_size;
	TLAB_ACCESS_INIT;

	g_assert (klass->gc_descr_inited);

	LOCK_GC;
	mono_gc_memmove (dest, src, size);
	rs = REMEMBERED_SET;

	if (rs->store_next + 4 < rs->end_set) {
		*(rs->store_next++) = (mword)dest | REMSET_VTYPE;
		*(rs->store_next++) = (mword)klass->gc_descr;
		*(rs->store_next++) = (mword)count;
		*(rs->store_next++) = (mword)element_size;
		UNLOCK_GC;
		return;
	}
	rs = mono_sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
#endif
	*(rs->store_next++) = (mword)dest | REMSET_VTYPE;
	*(rs->store_next++) = (mword)klass->gc_descr;
	*(rs->store_next++) = (mword)count;
	*(rs->store_next++) = (mword)element_size;
	UNLOCK_GC;
}	

void
mono_sgen_ssb_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	int size;
	RememberedSet *rs;
	TLAB_ACCESS_INIT;

	size = mono_object_class (obj)->instance_size;

	HEAVY_STAT (++stat_wbarrier_object_copy);
	rs = REMEMBERED_SET;
	DEBUG (6, fprintf (gc_debug_file, "Adding object remset for %p\n", obj));

	LOCK_GC;
	/* do not copy the sync state */
	mono_gc_memmove ((char*)obj + sizeof (MonoObject), (char*)src + sizeof (MonoObject),
			size - sizeof (MonoObject));

	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)obj | REMSET_OBJECT;
		UNLOCK_GC;
		return;
	}
	rs = mono_sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;

	#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
	#endif
	*(rs->store_next++) = (mword)obj | REMSET_OBJECT;
	UNLOCK_GC;
}

void
mono_sgen_ssb_wbarrier_generic_nostore (gpointer ptr)
{
	gpointer *buffer;
	int index;
	TLAB_ACCESS_INIT;

	LOCK_GC;

	buffer = STORE_REMSET_BUFFER;
	index = STORE_REMSET_BUFFER_INDEX;
	/* This simple optimization eliminates a sizable portion of
	   entries.  Comparing it to the last but one entry as well
	   doesn't eliminate significantly more entries. */
	if (buffer [index] == ptr) {
		UNLOCK_GC;
		return;
	}

	HEAVY_STAT (++stat_wbarrier_generic_store_remset);

	++index;
	if (index >= STORE_REMSET_BUFFER_SIZE) {
		evacuate_remset_buffer ();
		index = STORE_REMSET_BUFFER_INDEX;
		g_assert (index == 0);
		++index;
	}
	buffer [index] = ptr;
	STORE_REMSET_BUFFER_INDEX = index;

	UNLOCK_GC;
}


void
mono_sgen_ssb_cleanup_thread (SgenThreadInfo *p)
{
	RememberedSet *rset;

	if (p->remset) {
		if (freed_thread_remsets) {
			for (rset = p->remset; rset->next; rset = rset->next)
				;
			rset->next = freed_thread_remsets;
			freed_thread_remsets = p->remset;
		} else {
			freed_thread_remsets = p->remset;
		}
	}

	if (*p->store_remset_buffer_index_addr)
		add_generic_store_remset_from_buffer (*p->store_remset_buffer_addr);
	mono_sgen_free_internal (*p->store_remset_buffer_addr, INTERNAL_MEM_STORE_REMSET);

	/*
	 * This is currently not strictly required, but we do it
	 * anyway in case we change thread unregistering:

	 * If the thread is removed from the thread list after
	 * unregistering (this is currently not the case), and a
	 * collection occurs, clear_remsets() would want to memset
	 * this buffer, which would either clobber memory or crash.
	 */
	*p->store_remset_buffer_addr = NULL;
}


void
mono_sgen_ssb_prepare_for_minor_collection (void)
{
	memset (global_remset_cache, 0, sizeof (global_remset_cache));
}

/*
 * Clear the info in the remembered sets: we're doing a major collection, so
 * the per-thread ones are not needed and the global ones will be reconstructed
 * during the copy.
 */
void
mono_sgen_ssb_prepare_for_major_collection (void)
{
	SgenThreadInfo *info;
	RememberedSet *remset, *next;
	
	mono_sgen_ssb_prepare_for_minor_collection ();

	/* the global list */
	for (remset = global_remset; remset; remset = next) {
		remset->store_next = remset->data;
		next = remset->next;
		remset->next = NULL;
		if (remset != global_remset) {
			DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
			mono_sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
		}
	}
	/* the generic store ones */
	while (generic_store_remsets) {
		GenericStoreRememberedSet *gs_next = generic_store_remsets->next;
		mono_sgen_free_internal (generic_store_remsets, INTERNAL_MEM_STORE_REMSET);
		generic_store_remsets = gs_next;
	}
	/* the per-thread ones */
	FOREACH_THREAD (info) {
		for (remset = info->remset; remset; remset = next) {
			remset->store_next = remset->data;
			next = remset->next;
			remset->next = NULL;
			if (remset != info->remset) {
				DEBUG (3, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
				mono_sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
			}
		}
		clear_thread_store_remset_buffer (info);
	} END_FOREACH_THREAD

	/* the freed thread ones */
	while (freed_thread_remsets) {
		next = freed_thread_remsets->next;
		DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", freed_thread_remsets->data));
		mono_sgen_free_internal_dynamic (freed_thread_remsets, remset_byte_size (freed_thread_remsets), INTERNAL_MEM_REMSET);
		freed_thread_remsets = next;
	}
}


/*
 * Tries to check if a given remset location was already added to the global remset.
 * It can
 *
 * A 2 entry, LRU cache of recently saw location remsets.
 *
 * It's hand-coded instead of done using loops to reduce the number of memory references on cache hit.
 *
 * Returns TRUE is the element was added..
 */
gboolean
global_remset_location_was_not_added (gpointer ptr)
{

	gpointer first = global_remset_cache [0], second;
	if (first == ptr) {
		HEAVY_STAT (++stat_global_remsets_discarded);
		return FALSE;
	}

	second = global_remset_cache [1];

	if (second == ptr) {
		/*Move the second to the front*/
		global_remset_cache [0] = second;
		global_remset_cache [1] = first;

		HEAVY_STAT (++stat_global_remsets_discarded);
		return FALSE;
	}

	global_remset_cache [0] = second;
	global_remset_cache [1] = ptr;
	return TRUE;
}

void
mono_sgen_ssb_record_pointer (gpointer ptr)
{
	RememberedSet *rs;
	gboolean lock = mono_sgen_collection_is_parallel ();
	gpointer obj = *(gpointer*)ptr;

	g_assert (!mono_sgen_ptr_in_nursery (ptr) && mono_sgen_ptr_in_nursery (obj));

	if (lock)
		LOCK_GLOBAL_REMSET;

	if (!global_remset_location_was_not_added (ptr))
		goto done;

	if (G_UNLIKELY (do_pin_stats))
		mono_sgen_pin_stats_register_global_remset (obj);

	DEBUG (8, fprintf (gc_debug_file, "Adding global remset for %p\n", ptr));
	binary_protocol_global_remset (ptr, *(gpointer*)ptr, (gpointer)LOAD_VTABLE (obj));

	HEAVY_STAT (++stat_global_remsets_added);

	/* 
	 * FIXME: If an object remains pinned, we need to add it at every minor collection.
	 * To avoid uncontrolled growth of the global remset, only add each pointer once.
	 */
	if (global_remset->store_next + 3 < global_remset->end_set) {
		*(global_remset->store_next++) = (mword)ptr;
		goto done;
	}
	rs = mono_sgen_alloc_remset (global_remset->end_set - global_remset->data, NULL, TRUE);
	rs->next = global_remset;
	global_remset = rs;
	*(global_remset->store_next++) = (mword)ptr;

	{
		int global_rs_size = 0;

		for (rs = global_remset; rs; rs = rs->next) {
			global_rs_size += rs->store_next - rs->data;
		}
		DEBUG (4, fprintf (gc_debug_file, "Global remset now has size %d\n", global_rs_size));
	}

 done:
	if (lock)
		UNLOCK_GLOBAL_REMSET;
}

void
mono_sgen_ssb_init (void)
{
	LOCK_INIT (global_remset_mutex);
}

#endif
