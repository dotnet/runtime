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

#ifdef HAVE_KW_THREAD
static __thread RememberedSet *remembered_set MONO_TLS_FAST;
#endif
static MonoNativeTlsKey remembered_set_key;
static RememberedSet *global_remset;
static RememberedSet *freed_thread_remsets;
static GenericStoreRememberedSet *generic_store_remsets = NULL;

#ifdef HEAVY_STATISTICS
static int stat_wbarrier_generic_store_remset = 0;

static long long stat_store_remsets = 0;
static long long stat_store_remsets_unique = 0;
static long long stat_saved_remsets_1 = 0;
static long long stat_saved_remsets_2 = 0;
static long long stat_local_remsets_processed = 0;
static long long stat_global_remsets_added = 0;
static long long stat_global_remsets_readded = 0;
static long long stat_global_remsets_processed = 0;
static long long stat_global_remsets_discarded = 0;

#endif

static gboolean global_remset_location_was_not_added (gpointer ptr);


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
	GenericStoreRememberedSet *remset = sgen_alloc_internal (INTERNAL_MEM_STORE_REMSET);
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

/* FIXME: later choose a size that takes into account the RememberedSet struct
 * and doesn't waste any alloc paddin space.
 */
static RememberedSet*
sgen_alloc_remset (int size, gpointer id, gboolean global)
{
	RememberedSet* res = sgen_alloc_internal_dynamic (sizeof (RememberedSet) + (size * sizeof (gpointer)), INTERNAL_MEM_REMSET);
	res->store_next = res->data;
	res->end_set = res->data + size;
	res->next = NULL;
	DEBUG (4, fprintf (gc_debug_file, "Allocated%s remset size %d at %p for %p\n", global ? " global" : "", size, res->data, id));
	return res;
}



static void
sgen_ssb_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value)
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
	rs = sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
#endif
	*(rs->store_next++) = (mword)field_ptr;
	*(void**)field_ptr = value;
	UNLOCK_GC;
}

static void
sgen_ssb_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
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
	rs = sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
#endif
	*(rs->store_next++) = (mword)slot_ptr;
	*(void**)slot_ptr = value;
	UNLOCK_GC;
}

static void
sgen_ssb_wbarrier_arrayref_copy (gpointer dest_ptr, gpointer src_ptr, int count)
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
	rs = sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
#endif
	*(rs->store_next++) = (mword)dest_ptr | REMSET_RANGE;
	*(rs->store_next++) = count;

	UNLOCK_GC;
}

static void
sgen_ssb_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
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
	rs = sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
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

static void
sgen_ssb_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	int size;
	RememberedSet *rs;
	TLAB_ACCESS_INIT;

	size = mono_object_class (obj)->instance_size;

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
	rs = sgen_alloc_remset (rs->end_set - rs->data, (void*)1, FALSE);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;

	#ifdef HAVE_KW_THREAD
	mono_thread_info_current ()->remset = rs;
	#endif
	*(rs->store_next++) = (mword)obj | REMSET_OBJECT;
	UNLOCK_GC;
}

static void
sgen_ssb_wbarrier_generic_nostore (gpointer ptr)
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


#ifdef HEAVY_STATISTICS
static mword*
collect_store_remsets (RememberedSet *remset, mword *bumper)
{
	mword *p = remset->data;
	mword last = 0;
	mword last1 = 0;
	mword last2 = 0;

	while (p < remset->store_next) {
		switch ((*p) & REMSET_TYPE_MASK) {
		case REMSET_LOCATION:
			*bumper++ = *p;
			if (*p == last)
				++stat_saved_remsets_1;
			last = *p;
			if (*p == last1 || *p == last2) {
				++stat_saved_remsets_2;
			} else {
				last2 = last1;
				last1 = *p;
			}
			p += 1;
			break;
		case REMSET_RANGE:
			p += 2;
			break;
		case REMSET_OBJECT:
			p += 1;
			break;
		case REMSET_VTYPE:
			p += 4;
			break;
		default:
			g_assert_not_reached ();
		}
	}

	return bumper;
}

static void
remset_stats (void)
{
	RememberedSet *remset;
	int size = 0;
	SgenThreadInfo *info;
	mword *addresses, *bumper, *p, *r;

	FOREACH_THREAD (info) {
		for (remset = info->remset; remset; remset = remset->next)
			size += remset->store_next - remset->data;
	} END_FOREACH_THREAD
	for (remset = freed_thread_remsets; remset; remset = remset->next)
		size += remset->store_next - remset->data;
	for (remset = global_remset; remset; remset = remset->next)
		size += remset->store_next - remset->data;

	bumper = addresses = sgen_alloc_internal_dynamic (sizeof (mword) * size, INTERNAL_MEM_STATISTICS);

	FOREACH_THREAD (info) {
		for (remset = info->remset; remset; remset = remset->next)
			bumper = collect_store_remsets (remset, bumper);
	} END_FOREACH_THREAD
	for (remset = global_remset; remset; remset = remset->next)
		bumper = collect_store_remsets (remset, bumper);
	for (remset = freed_thread_remsets; remset; remset = remset->next)
		bumper = collect_store_remsets (remset, bumper);

	g_assert (bumper <= addresses + size);

	stat_store_remsets += bumper - addresses;

	sgen_sort_addresses ((void**)addresses, bumper - addresses);
	p = addresses;
	r = addresses + 1;
	while (r < bumper) {
		if (*r != *p)
			*++p = *r;
		++r;
	}

	stat_store_remsets_unique += p - addresses;

	sgen_free_internal_dynamic (addresses, sizeof (mword) * size, INTERNAL_MEM_STATISTICS);
}
#endif


static mword*
handle_remset (mword *p, void *start_nursery, void *end_nursery, gboolean global, SgenGrayQueue *queue)
{
	void **ptr;
	mword count;
	mword desc;

	if (global)
		HEAVY_STAT (++stat_global_remsets_processed);
	else
		HEAVY_STAT (++stat_local_remsets_processed);

	/* FIXME: exclude stack locations */
	switch ((*p) & REMSET_TYPE_MASK) {
	case REMSET_LOCATION:
		ptr = (void**)(*p);
		//__builtin_prefetch (ptr);
		if (((void*)ptr < start_nursery || (void*)ptr >= end_nursery)) {
			gpointer old = *ptr;

			sgen_get_current_object_ops ()->copy_or_mark_object (ptr, queue);
			DEBUG (9, fprintf (gc_debug_file, "Overwrote remset at %p with %p\n", ptr, *ptr));
			if (old)
				binary_protocol_ptr_update (ptr, old, *ptr, (gpointer)LOAD_VTABLE (*ptr), sgen_safe_object_get_size (*ptr));
			if (!global && *ptr >= start_nursery && *ptr < end_nursery) {
				/*
				 * If the object is pinned, each reference to it from nonpinned objects
				 * becomes part of the global remset, which can grow very large.
				 */
				DEBUG (9, fprintf (gc_debug_file, "Add to global remset because of pinning %p (%p %s)\n", ptr, *ptr, sgen_safe_name (*ptr)));
				sgen_add_to_global_remset (ptr);
			}
		} else {
			DEBUG (9, fprintf (gc_debug_file, "Skipping remset at %p holding %p\n", ptr, *ptr));
		}
		return p + 1;
	case REMSET_RANGE: {
		CopyOrMarkObjectFunc copy_func = sgen_get_current_object_ops ()->copy_or_mark_object;

		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery))
			return p + 2;
		count = p [1];
		while (count-- > 0) {
			copy_func (ptr, queue);
			DEBUG (9, fprintf (gc_debug_file, "Overwrote remset at %p with %p (count: %d)\n", ptr, *ptr, (int)count));
			if (!global && *ptr >= start_nursery && *ptr < end_nursery)
				sgen_add_to_global_remset (ptr);
			++ptr;
		}
		return p + 2;
	}
	case REMSET_OBJECT:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery))
			return p + 1;
		sgen_get_current_object_ops ()->scan_object ((char*)ptr, queue);
		return p + 1;
	case REMSET_VTYPE: {
		size_t skip_size;

		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery))
			return p + 4;
		desc = p [1];
		count = p [2];
		skip_size = p [3];
		while (count-- > 0) {
			sgen_get_current_object_ops ()->scan_vtype ((char*)ptr, desc, queue);
			ptr = (void**)((char*)ptr + skip_size);
		}
		return p + 4;
	}
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

static void
sgen_ssb_begin_scan_remsets (void *start_nursery, void *end_nursery, SgenGrayQueue *queue)
{
	RememberedSet *remset;
	mword *p, *next_p, *store_pos;

	/* the global one */
	for (remset = global_remset; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning global remset range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		store_pos = remset->data;
		for (p = remset->data; p < remset->store_next; p = next_p) {
			void **ptr = (void**)p [0];

			/*Ignore previously processed remset.*/
			if (!global_remset_location_was_not_added (ptr)) {
				next_p = p + 1;
				continue;
			}

			next_p = handle_remset (p, start_nursery, end_nursery, TRUE, queue);

			/* 
			 * Clear global remsets of locations which no longer point to the 
			 * nursery. Otherwise, they could grow indefinitely between major 
			 * collections.
			 *
			 * Since all global remsets are location remsets, we don't need to unmask the pointer.
			 */
			if (sgen_ptr_in_nursery (*ptr)) {
				*store_pos ++ = p [0];
				HEAVY_STAT (++stat_global_remsets_readded);
			}
		}

		/* Truncate the remset */
		remset->store_next = store_pos;
	}
}

static void
sgen_ssb_finish_scan_remsets (void *start_nursery, void *end_nursery, SgenGrayQueue *queue)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset;
	GenericStoreRememberedSet *store_remset;
	mword *p;

#ifdef HEAVY_STATISTICS
	remset_stats ();
#endif

	/* the generic store ones */
	store_remset = generic_store_remsets;
	while (store_remset) {
		GenericStoreRememberedSet *next = store_remset->next;

		for (i = 0; i < STORE_REMSET_BUFFER_SIZE - 1; ++i) {
			gpointer addr = store_remset->data [i];
			if (addr)
				handle_remset ((mword*)&addr, start_nursery, end_nursery, FALSE, queue);
		}

		sgen_free_internal (store_remset, INTERNAL_MEM_STORE_REMSET);

		store_remset = next;
	}
	generic_store_remsets = NULL;

	/* the per-thread ones */
	FOREACH_THREAD (info) {
		RememberedSet *next;
		int j;
		for (remset = info->remset; remset; remset = next) {
			DEBUG (4, fprintf (gc_debug_file, "Scanning remset for thread %p, range: %p-%p, size: %td\n", info, remset->data, remset->store_next, remset->store_next - remset->data));
			for (p = remset->data; p < remset->store_next;)
				p = handle_remset (p, start_nursery, end_nursery, FALSE, queue);
			remset->store_next = remset->data;
			next = remset->next;
			remset->next = NULL;
			if (remset != info->remset) {
				DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
				sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
			}
		}
		for (j = 0; j < *info->store_remset_buffer_index_addr; ++j)
			handle_remset ((mword*)*info->store_remset_buffer_addr + j + 1, start_nursery, end_nursery, FALSE, queue);
		clear_thread_store_remset_buffer (info);
	} END_FOREACH_THREAD

	/* the freed thread ones */
	while (freed_thread_remsets) {
		RememberedSet *next;
		remset = freed_thread_remsets;
		DEBUG (4, fprintf (gc_debug_file, "Scanning remset for freed thread, range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;)
			p = handle_remset (p, start_nursery, end_nursery, FALSE, queue);
		next = remset->next;
		DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
		sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
		freed_thread_remsets = next;
	}
}


static void
sgen_ssb_cleanup_thread (SgenThreadInfo *p)
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
	sgen_free_internal (*p->store_remset_buffer_addr, INTERNAL_MEM_STORE_REMSET);

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

static void
sgen_ssb_register_thread (SgenThreadInfo *info)
{
#ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = info;
#endif

	info->remset = sgen_alloc_remset (DEFAULT_REMSET_SIZE, info, FALSE);
	mono_native_tls_set_value (remembered_set_key, info->remset);
#ifdef HAVE_KW_THREAD
	remembered_set = info->remset;
#endif

	STORE_REMSET_BUFFER = sgen_alloc_internal (INTERNAL_MEM_STORE_REMSET);
	STORE_REMSET_BUFFER_INDEX = 0;
}

#ifdef HAVE_KW_THREAD
static void
sgen_ssb_fill_thread_info_for_suspend (SgenThreadInfo *info)
{
	/* update the remset info in the thread data structure */
	info->remset = remembered_set;
}
#endif

static void
sgen_ssb_prepare_for_minor_collection (void)
{
	memset (global_remset_cache, 0, sizeof (global_remset_cache));
}

/*
 * Clear the info in the remembered sets: we're doing a major collection, so
 * the per-thread ones are not needed and the global ones will be reconstructed
 * during the copy.
 */
static void
sgen_ssb_prepare_for_major_collection (void)
{
	SgenThreadInfo *info;
	RememberedSet *remset, *next;
	
	sgen_ssb_prepare_for_minor_collection ();

	/* the global list */
	for (remset = global_remset; remset; remset = next) {
		remset->store_next = remset->data;
		next = remset->next;
		remset->next = NULL;
		if (remset != global_remset) {
			DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
			sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
		}
	}
	/* the generic store ones */
	while (generic_store_remsets) {
		GenericStoreRememberedSet *gs_next = generic_store_remsets->next;
		sgen_free_internal (generic_store_remsets, INTERNAL_MEM_STORE_REMSET);
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
				sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
			}
		}
		clear_thread_store_remset_buffer (info);
	} END_FOREACH_THREAD

	/* the freed thread ones */
	while (freed_thread_remsets) {
		next = freed_thread_remsets->next;
		DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", freed_thread_remsets->data));
		sgen_free_internal_dynamic (freed_thread_remsets, remset_byte_size (freed_thread_remsets), INTERNAL_MEM_REMSET);
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
static gboolean
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

static void
sgen_ssb_record_pointer (gpointer ptr)
{
	RememberedSet *rs;
	gboolean lock = sgen_collection_is_parallel ();
	gpointer obj = *(gpointer*)ptr;

	g_assert (!sgen_ptr_in_nursery (ptr) && sgen_ptr_in_nursery (obj));

	if (lock)
		LOCK_GLOBAL_REMSET;

	if (!global_remset_location_was_not_added (ptr))
		goto done;

	if (G_UNLIKELY (do_pin_stats))
		sgen_pin_stats_register_global_remset (obj);

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
	rs = sgen_alloc_remset (global_remset->end_set - global_remset->data, NULL, TRUE);
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

/*
 * ######################################################################
 * ########  Debug support
 * ######################################################################
 */

static mword*
find_in_remset_loc (mword *p, char *addr, gboolean *found)
{
	void **ptr;
	mword count, desc;
	size_t skip_size;

	switch ((*p) & REMSET_TYPE_MASK) {
	case REMSET_LOCATION:
		if (*p == (mword)addr)
			*found = TRUE;
		return p + 1;
	case REMSET_RANGE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		count = p [1];
		if ((void**)addr >= ptr && (void**)addr < ptr + count)
			*found = TRUE;
		return p + 2;
	case REMSET_OBJECT:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		count = sgen_safe_object_get_size ((MonoObject*)ptr); 
		count = SGEN_ALIGN_UP (count);
		count /= sizeof (mword);
		if ((void**)addr >= ptr && (void**)addr < ptr + count)
			*found = TRUE;
		return p + 1;
	case REMSET_VTYPE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		desc = p [1];
		count = p [2];
		skip_size = p [3];

		/* The descriptor includes the size of MonoObject */
		skip_size -= sizeof (MonoObject);
		skip_size *= count;
		if ((void**)addr >= ptr && (void**)addr < ptr + (skip_size / sizeof (gpointer)))
			*found = TRUE;

		return p + 4;
	default:
		g_assert_not_reached ();
	}
	return NULL;
}
/*
 * Return whenever ADDR occurs in the remembered sets
 */
static gboolean
sgen_ssb_find_address (char *addr)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset;
	GenericStoreRememberedSet *store_remset;
	mword *p;
	gboolean found = FALSE;

	/* the global one */
	for (remset = global_remset; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning global remset range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;) {
			p = find_in_remset_loc (p, addr, &found);
			if (found)
				return TRUE;
		}
	}

	/* the generic store ones */
	for (store_remset = generic_store_remsets; store_remset; store_remset = store_remset->next) {
		for (i = 0; i < STORE_REMSET_BUFFER_SIZE - 1; ++i) {
			if (store_remset->data [i] == addr)
				return TRUE;
		}
	}

	/* the per-thread ones */
	FOREACH_THREAD (info) {
		int j;
		for (remset = info->remset; remset; remset = remset->next) {
			DEBUG (4, fprintf (gc_debug_file, "Scanning remset for thread %p, range: %p-%p, size: %td\n", info, remset->data, remset->store_next, remset->store_next - remset->data));
			for (p = remset->data; p < remset->store_next;) {
				p = find_in_remset_loc (p, addr, &found);
				if (found)
					return TRUE;
			}
		}
		for (j = 0; j < *info->store_remset_buffer_index_addr; ++j) {
			if ((*info->store_remset_buffer_addr) [j + 1] == addr)
				return TRUE;
		}
	} END_FOREACH_THREAD

	/* the freed thread ones */
	for (remset = freed_thread_remsets; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning remset for freed thread, range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;) {
			p = find_in_remset_loc (p, addr, &found);
			if (found)
				return TRUE;
		}
	}

	return FALSE;
}


void
sgen_ssb_init (SgenRemeberedSet *remset)
{
	LOCK_INIT (global_remset_mutex);

	global_remset = sgen_alloc_remset (1024, NULL, FALSE);
	global_remset->next = NULL;

	mono_native_tls_alloc (&remembered_set_key, NULL);

#ifdef HEAVY_STATISTICS
	mono_counters_register ("WBarrier generic store stored", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_generic_store_remset);

	mono_counters_register ("Store remsets", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_store_remsets);
	mono_counters_register ("Unique store remsets", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_store_remsets_unique);
	mono_counters_register ("Saved remsets 1", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_saved_remsets_1);
	mono_counters_register ("Saved remsets 2", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_saved_remsets_2);
	mono_counters_register ("Non-global remsets processed", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_local_remsets_processed);
	mono_counters_register ("Global remsets added", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_added);
	mono_counters_register ("Global remsets re-added", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_readded);
	mono_counters_register ("Global remsets processed", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_processed);
	mono_counters_register ("Global remsets discarded", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_discarded);
#endif

	remset->wbarrier_set_field = sgen_ssb_wbarrier_set_field;
	remset->wbarrier_set_arrayref = sgen_ssb_wbarrier_set_arrayref;
	remset->wbarrier_arrayref_copy = sgen_ssb_wbarrier_arrayref_copy;
	remset->wbarrier_value_copy = sgen_ssb_wbarrier_value_copy;
	remset->wbarrier_object_copy = sgen_ssb_wbarrier_object_copy;
	remset->wbarrier_generic_nostore = sgen_ssb_wbarrier_generic_nostore;
	remset->record_pointer = sgen_ssb_record_pointer;

	remset->begin_scan_remsets = sgen_ssb_begin_scan_remsets;
	remset->finish_scan_remsets = sgen_ssb_finish_scan_remsets;

	remset->register_thread = sgen_ssb_register_thread;
	remset->cleanup_thread = sgen_ssb_cleanup_thread;
#ifdef HAVE_KW_THREAD
	remset->fill_thread_info_for_suspend = sgen_ssb_fill_thread_info_for_suspend;
#endif

	remset->prepare_for_minor_collection = sgen_ssb_prepare_for_minor_collection;
	remset->prepare_for_major_collection = sgen_ssb_prepare_for_major_collection;

	remset->find_address = sgen_ssb_find_address;
}
#endif
