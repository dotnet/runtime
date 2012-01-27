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
#include "utils/mono-counters.h"

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

#endif