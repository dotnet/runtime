/*
 * sgen-major-copying.c: Simple generational GC.
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2005-2010 Novell, Inc (http://www.novell.com)
 *
 * Thread start/stop adapted from Boehm's GC:
 * Copyright (c) 1994 by Xerox Corporation.  All rights reserved.
 * Copyright (c) 1996 by Silicon Graphics.  All rights reserved.
 * Copyright (c) 1998 by Fergus Henderson.  All rights reserved.
 * Copyright (c) 2000-2004 by Hewlett-Packard Company.  All rights reserved.
 *
 * THIS MATERIAL IS PROVIDED AS IS, WITH ABSOLUTELY NO WARRANTY EXPRESSED
 * OR IMPLIED.  ANY USE IS AT YOUR OWN RISK.
 *
 * Permission is hereby granted to use or copy this program
 * for any purpose,  provided the above notices are retained on all copies.
 * Permission to modify the code and to distribute modified code is granted,
 * provided the above notices are retained, and a notice that the code was
 * modified is included with the above copyright notice.
 *
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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

#include "utils/mono-counters.h"

#include "metadata/gc-internal.h"
#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"
#include "metadata/mono-gc.h"
#include "metadata/object-internals.h"
#include "metadata/profiler-private.h"

#define MAJOR_SECTION_SIZE		SGEN_PINNED_CHUNK_SIZE
#define BLOCK_FOR_OBJECT(o)		SGEN_PINNED_CHUNK_FOR_PTR ((o))
#define MAJOR_SECTION_FOR_OBJECT(o)	((GCMemSection*)BLOCK_FOR_OBJECT ((o)))

#define MAJOR_OBJ_IS_IN_TO_SPACE(o)	(MAJOR_SECTION_FOR_OBJECT ((o))->is_to_space)

static int num_major_sections = 0;

static GCMemSection *section_list = NULL;

static SgenPinnedAllocator pinned_allocator;

static gboolean have_swept;

/*
 * used when moving the objects
 */
static char *to_space_bumper = NULL;
static char *to_space_top = NULL;
static GCMemSection *to_space_section = NULL;

/* we get this at init */
static int nursery_bits;
static char *nursery_start;
static char *nursery_end;

#define ptr_in_nursery(p)	(SGEN_PTR_IN_NURSERY ((p), nursery_bits, nursery_start, nursery_end))

#ifdef HEAVY_STATISTICS
static long stat_major_copy_object_failed_forwarded = 0;
static long stat_major_copy_object_failed_pinned = 0;
static long stat_major_copy_object_failed_large_pinned = 0;
static long stat_major_copy_object_failed_to_space = 0;
#endif

static void*
major_alloc_heap (mword nursery_size, mword nursery_align, int the_nursery_bits)
{
	if (nursery_align)
		nursery_start = sgen_alloc_os_memory_aligned (nursery_size, nursery_align, TRUE);
	else
		nursery_start = sgen_alloc_os_memory (nursery_size, TRUE);

	nursery_end = nursery_start + nursery_size;
	nursery_bits = the_nursery_bits;

	return nursery_start;
}

static gboolean
obj_is_from_pinned_alloc (char *p)
{
	return BLOCK_FOR_OBJECT (p)->role == MEMORY_ROLE_PINNED;
}

static void
free_pinned_object (char *obj, size_t size)
{
	sgen_free_pinned (&pinned_allocator, obj, size);
}

/*
 * Allocate a new section of memory to be used as old generation.
 */
static GCMemSection*
alloc_major_section (void)
{
	GCMemSection *section;
	int scan_starts;

	section = sgen_alloc_os_memory_aligned (MAJOR_SECTION_SIZE, MAJOR_SECTION_SIZE, TRUE);
	section->next_data = section->data = (char*)section + SGEN_SIZEOF_GC_MEM_SECTION;
	g_assert (!((mword)section->data & 7));
	section->size = MAJOR_SECTION_SIZE - SGEN_SIZEOF_GC_MEM_SECTION;
	section->end_data = section->data + section->size;
	sgen_update_heap_boundaries ((mword)section->data, (mword)section->end_data);
	DEBUG (3, fprintf (gc_debug_file, "New major heap section: (%p-%p), total: %lld\n", section->data, section->end_data, mono_gc_get_heap_size ()));
	scan_starts = (section->size + SGEN_SCAN_START_SIZE - 1) / SGEN_SCAN_START_SIZE;
	section->scan_starts = sgen_alloc_internal_dynamic (sizeof (char*) * scan_starts, INTERNAL_MEM_SCAN_STARTS);
	section->num_scan_start = scan_starts;
	section->block.role = MEMORY_ROLE_GEN1;
	section->is_to_space = TRUE;

	/* add to the section list */
	section->block.next = section_list;
	section_list = section;

	++num_major_sections;

	return section;
}

static void
free_major_section (GCMemSection *section)
{
	DEBUG (3, fprintf (gc_debug_file, "Freed major section %p (%p-%p)\n", section, section->data, section->end_data));
	sgen_free_internal_dynamic (section->scan_starts,
			(section->size + SGEN_SCAN_START_SIZE - 1) / SGEN_SCAN_START_SIZE * sizeof (char*), INTERNAL_MEM_SCAN_STARTS);
	sgen_free_os_memory (section, MAJOR_SECTION_SIZE);

	--num_major_sections;
}

static void
new_to_space_section (void)
{
	/* FIXME: if the current to_space_section is empty, we don't
	   have to allocate a new one */

	to_space_section = alloc_major_section ();
	to_space_bumper = to_space_section->next_data;
	to_space_top = to_space_section->end_data;
}

static void
to_space_set_next_data (void)
{
	g_assert (to_space_bumper >= to_space_section->next_data && to_space_bumper <= to_space_section->end_data);
	to_space_section->next_data = to_space_bumper;
}

static void
to_space_expand (void)
{
	if (to_space_section) {
		g_assert (to_space_top == to_space_section->end_data);
		to_space_set_next_data ();
	}

	new_to_space_section ();
}

static void*
major_alloc_object (int size, gboolean has_references)
{
	char *dest = to_space_bumper;
	/* Make sure we have enough space available */
	if (dest + size > to_space_top) {
		to_space_expand ();
		(dest) = to_space_bumper;
		DEBUG (8, g_assert (dest + size <= to_space_top));
	}
	to_space_bumper += size;
	DEBUG (8, g_assert (to_space_bumper <= to_space_top));
	to_space_section->scan_starts [(dest - (char*)to_space_section->data)/SGEN_SCAN_START_SIZE] = dest;
	return dest;
}

static void
unset_to_space (void)
{
	/* between collections the to_space_bumper is invalidated
	   because degraded allocations might occur, so we set it to
	   NULL, just to make it explicit */
	to_space_bumper = NULL;

	/* don't unset to_space_section if we implement the FIXME in
	   new_to_space_section */
	to_space_section = NULL;
}

static gboolean
major_is_object_live (char *obj)
{
	mword objsize;

	/* nursery */
	if (ptr_in_nursery (obj))
		return FALSE;

	objsize = SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)obj));

	/* LOS */
	if (objsize > SGEN_MAX_SMALL_OBJ_SIZE)
		return FALSE;

	/* pinned chunk */
	if (obj_is_from_pinned_alloc (obj))
		return FALSE;

	/* now we know it's in a major heap section */
	return MAJOR_SECTION_FOR_OBJECT (obj)->is_to_space;
}

/* size is a multiple of ALLOC_ALIGN */
static void*
major_alloc_small_pinned_obj (size_t size, gboolean has_references)
{
	return sgen_alloc_pinned (&pinned_allocator, size);
}

/*
 * size is already rounded up and we hold the GC lock.
 */
static void*
major_alloc_degraded (MonoVTable *vtable, size_t size)
{
	GCMemSection *section;
	void **p = NULL;
	g_assert (size <= SGEN_MAX_SMALL_OBJ_SIZE);
	HEAVY_STAT (++stat_objects_alloced_degraded);
	HEAVY_STAT (stat_bytes_alloced_degraded += size);
	for (section = section_list; section; section = section->block.next) {
		if ((section->end_data - section->next_data) >= size) {
			p = (void**)section->next_data;
			break;
		}
	}
	if (!p) {
		section = alloc_major_section ();
		section->is_to_space = FALSE;
		/* FIXME: handle OOM */
		p = (void**)section->next_data;
		sgen_register_major_sections_alloced (1);
	}
	section->next_data += size;
	DEBUG (3, fprintf (gc_debug_file, "Allocated (degraded) object %p, vtable: %p (%s), size: %zd in section %p\n", p, vtable, vtable->klass->name, size, section));
	*p = vtable;
	return p;
}

static inline void
pin_major_object (char *obj, SgenGrayQueue *queue)
{
	sgen_pin_object (obj, queue);
}

#include "sgen-major-copy-object.h"

static void
major_copy_or_mark_object (void **obj_slot, SgenGrayQueue *queue)
{
	char *forwarded;
	char *obj = *obj_slot;
	mword objsize;

	DEBUG (9, g_assert (current_collection_generation == GENERATION_OLD));

	HEAVY_STAT (++stat_copy_object_called_major);

	DEBUG (9, fprintf (gc_debug_file, "Precise copy of %p from %p", obj, obj_slot));

	/*
	 * obj must belong to one of:
	 *
	 * 1. the nursery
	 * 2. the LOS
	 * 3. a pinned chunk
	 * 4. a non-to-space section of the major heap
	 * 5. a to-space section of the major heap
	 *
	 * In addition, objects in 1, 2 and 4 might also be pinned.
	 * Objects in 1 and 4 might be forwarded.
	 *
	 * Before we can copy the object we must make sure that we are
	 * allowed to, i.e. that the object not pinned, not already
	 * forwarded, not in the nursery To Space and doesn't belong
	 * to the LOS, a pinned chunk, or a to-space section.
	 *
	 * We are usually called for to-space objects (5) when we have
	 * two remset entries for the same reference.  The first entry
	 * copies the object and updates the reference and the second
	 * calls us with the updated reference that points into
	 * to-space.  There might also be other circumstances where we
	 * get to-space objects.
	 */

	if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
		DEBUG (9, g_assert (((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr));
		DEBUG (9, fprintf (gc_debug_file, " (already forwarded to %p)\n", forwarded));
		HEAVY_STAT (++stat_major_copy_object_failed_forwarded);
		*obj_slot = forwarded;
		return;
	}
	if (SGEN_OBJECT_IS_PINNED (obj)) {
		DEBUG (9, g_assert (((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr));
		DEBUG (9, fprintf (gc_debug_file, " (pinned, no change)\n"));
		HEAVY_STAT (++stat_major_copy_object_failed_pinned);
		return;
	}

	if (ptr_in_nursery (obj)) {
		/* A To Space object is already on its final destination for the current collection. */
		if (sgen_nursery_is_to_space (obj))
			return;
		goto copy;
	}

	/*
	 * At this point we know obj is not pinned, not forwarded and
	 * belongs to 2, 3, 4, or 5.
	 *
	 * LOS object (2) are simple, at least until we always follow
	 * the rule: if objsize > SGEN_MAX_SMALL_OBJ_SIZE, pin the
	 * object and return it.  At the end of major collections, we
	 * walk the los list and if the object is pinned, it is
	 * marked, otherwise it can be freed.
	 *
	 * Pinned chunks (3) and major heap sections (4, 5) both
	 * reside in blocks, which are always aligned, so once we've
	 * eliminated LOS objects, we can just access the block and
	 * see whether it's a pinned chunk or a major heap section.
	 */

	objsize = SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)obj));

	if (G_UNLIKELY (objsize > SGEN_MAX_SMALL_OBJ_SIZE || obj_is_from_pinned_alloc (obj))) {
		if (SGEN_OBJECT_IS_PINNED (obj))
			return;
		DEBUG (9, fprintf (gc_debug_file, " (marked LOS/Pinned %p (%s), size: %td)\n", obj, sgen_safe_name (obj), objsize));
		binary_protocol_pin (obj, (gpointer)SGEN_LOAD_VTABLE (obj), sgen_safe_object_get_size ((MonoObject*)obj));
		SGEN_PIN_OBJECT (obj);
		GRAY_OBJECT_ENQUEUE (queue, obj);
		HEAVY_STAT (++stat_major_copy_object_failed_large_pinned);
		return;
	}

	/*
	 * Now we know the object is in a major heap section.  All we
	 * need to do is check whether it's already in to-space (5) or
	 * not (4).
	 */
	if (MAJOR_OBJ_IS_IN_TO_SPACE (obj)) {
		DEBUG (9, g_assert (objsize <= SGEN_MAX_SMALL_OBJ_SIZE));
		DEBUG (9, fprintf (gc_debug_file, " (already copied)\n"));
		HEAVY_STAT (++stat_major_copy_object_failed_to_space);
		return;
	}

 copy:
	HEAVY_STAT (++stat_objects_copied_major);

	*obj_slot = copy_object_no_checks (obj, queue);
}

#include "sgen-major-scan-object.h"

/* FIXME: later reduce code duplication here with build_nursery_fragments().
 * We don't keep track of section fragments for non-nursery sections yet, so
 * just memset to 0.
 */
static void
build_section_fragments (GCMemSection *section)
{
	int i;
	char *frag_start, *frag_end;
	size_t frag_size;

	/* clear scan starts */
	memset (section->scan_starts, 0, section->num_scan_start * sizeof (gpointer));
	frag_start = section->data;
	section->next_data = section->data;
	for (i = 0; i < section->pin_queue_num_entries; ++i) {
		frag_end = section->pin_queue_start [i];
		/* remove the pin bit from pinned objects */
		SGEN_UNPIN_OBJECT (frag_end);
		if (frag_end >= section->data + section->size) {
			frag_end = section->data + section->size;
		} else {
			section->scan_starts [((char*)frag_end - (char*)section->data)/SGEN_SCAN_START_SIZE] = frag_end;
		}
		frag_size = frag_end - frag_start;
		if (frag_size) {
			binary_protocol_empty (frag_start, frag_size);
			memset (frag_start, 0, frag_size);
		}
		frag_size = SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)section->pin_queue_start [i]));
		frag_start = (char*)section->pin_queue_start [i] + frag_size;
		section->next_data = MAX (section->next_data, frag_start);
	}
	frag_end = section->end_data;
	frag_size = frag_end - frag_start;
	if (frag_size) {
		binary_protocol_empty (frag_start, frag_size);
		memset (frag_start, 0, frag_size);
	}
}

static void
sweep_pinned_objects_callback (char *ptr, size_t size, void *data)
{
	if (SGEN_OBJECT_IS_PINNED (ptr)) {
		SGEN_UNPIN_OBJECT (ptr);
		DEBUG (6, fprintf (gc_debug_file, "Unmarked pinned object %p (%s)\n", ptr, sgen_safe_name (ptr)));
	} else {
		DEBUG (6, fprintf (gc_debug_file, "Freeing unmarked pinned object %p (%s)\n", ptr, sgen_safe_name (ptr)));
		free_pinned_object (ptr, size);
	}
}

static void
sweep_pinned_objects (void)
{
	sgen_pinned_scan_objects (&pinned_allocator, sweep_pinned_objects_callback, NULL);
}

static void
major_iterate_objects (gboolean non_pinned, gboolean pinned, IterateObjectCallbackFunc callback, void *data)
{
	if (non_pinned) {
		GCMemSection *section;
		for (section = section_list; section; section = section->block.next)
			sgen_scan_area_with_callback (section->data, section->end_data, callback, data, FALSE);
	}
	if (pinned)
		sgen_pinned_scan_objects (&pinned_allocator, callback, data);
}

static void
major_free_non_pinned_object (char *obj, size_t size)
{
	memset (obj, 0, size);
}

static void
pin_pinned_object_callback (void *addr, size_t slot_size, SgenGrayQueue *queue)
{
	binary_protocol_pin (addr, (gpointer)SGEN_LOAD_VTABLE (addr), sgen_safe_object_get_size ((MonoObject*)addr));
	if (!SGEN_OBJECT_IS_PINNED (addr))
		sgen_pin_stats_register_object ((char*) addr, sgen_safe_object_get_size ((MonoObject*) addr));
	SGEN_PIN_OBJECT (addr);
	GRAY_OBJECT_ENQUEUE (queue, addr);
	DEBUG (6, fprintf (gc_debug_file, "Marked pinned object %p (%s) from roots\n", addr, sgen_safe_name (addr)));
}

static void
major_find_pin_queue_start_ends (SgenGrayQueue *queue)
{
	GCMemSection *section;

	for (section = section_list; section; section = section->block.next)
		sgen_find_section_pin_queue_start_end (section);
	sgen_pinned_scan_pinned_objects (&pinned_allocator, (IterateObjectCallbackFunc)pin_pinned_object_callback, queue);
}

static void
major_pin_objects (SgenGrayQueue *queue)
{
	GCMemSection *section;

	for (section = section_list; section; section = section->block.next)
		sgen_pin_objects_in_section (section, queue);
}

static void
major_init_to_space (void)
{
	new_to_space_section ();
}

static void
major_sweep (void)
{
	GCMemSection *section, *prev_section;

	to_space_set_next_data ();
	unset_to_space ();

	/* unpin objects from the pinned chunks and free the unmarked ones */
	sweep_pinned_objects ();

	sgen_pinned_update_heap_boundaries (&pinned_allocator);

	/* free the unused sections */
	prev_section = NULL;
	for (section = section_list; section;) {
		GCMemSection *this_section = section;

		/* to_space doesn't need handling here */
		if (section->is_to_space) {
			section->is_to_space = FALSE;
			prev_section = section;
			section = section->block.next;
			goto update;
		}
		/* no pinning object, so the section is free */
		if (!section->pin_queue_num_entries) {
			GCMemSection *to_free;
			g_assert (!section->pin_queue_start);
			if (prev_section)
				prev_section->block.next = section->block.next;
			else
				section_list = section->block.next;
			to_free = section;
			section = section->block.next;
			free_major_section (to_free);
			continue;
		} else {
			DEBUG (6, fprintf (gc_debug_file, "Section %p has still pinned objects (%d)\n", section, section->pin_queue_num_entries));
			build_section_fragments (section);
		}
		prev_section = section;
		section = section->block.next;

	update:
		sgen_update_heap_boundaries ((mword)this_section->data, (mword)this_section->data + this_section->size);
	}

	have_swept = TRUE;
}

static void
major_check_scan_starts (void)
{
	GCMemSection *section;
	for (section = section_list; section; section = section->block.next)
		sgen_check_section_scan_starts (section);
}

static void
major_dump_heap (FILE *heap_dump_file)
{
	GCMemSection *section;
	for (section = section_list; section; section = section->block.next)
		sgen_dump_section (section, "old");
	/* FIXME: dump pinned sections, too */
}

static gint64
major_get_used_size (void)
{
	gint64 tot = 0;
	GCMemSection *section;
	for (section = section_list; section; section = section->block.next) {
		/* this is approximate... */
		tot += section->next_data - section->data;
	}
	return tot;
}

/* only valid during minor collections */
static int old_num_major_sections;

static void
major_start_nursery_collection (void)
{
	old_num_major_sections = num_major_sections;

	if (!to_space_section) {
		new_to_space_section ();
	} else {
		/* we might have done degraded allocation since the
		   last collection */
		g_assert (to_space_bumper <= to_space_section->next_data);
		to_space_bumper = to_space_section->next_data;

		to_space_section->is_to_space = TRUE;
	}
}

static void
major_finish_nursery_collection (void)
{
	GCMemSection *section;
	int sections_alloced;

	to_space_set_next_data ();

	for (section = section_list; section; section = section->block.next)
		section->is_to_space = FALSE;

	sections_alloced = num_major_sections - old_num_major_sections;
	sgen_register_major_sections_alloced (sections_alloced);
}

static void
major_finish_major_collection (void)
{
}

static gboolean
major_ptr_is_in_non_pinned_space (char *ptr)
{
	GCMemSection *section;
	for (section = section_list; section;) {
		if (ptr >= section->data && ptr < section->data + section->size)
			return TRUE;
		section = section->block.next;
	}
	return FALSE;
}

static void
major_report_pinned_memory_usage (void)
{
	sgen_report_pinned_mem_usage (&pinned_allocator);
}

static int
get_num_major_sections (void)
{
	return num_major_sections;
}

void
sgen_copying_init (SgenMajorCollector *collector)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("# major copy_object() failed forwarded", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_copy_object_failed_forwarded);
	mono_counters_register ("# major copy_object() failed pinned", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_copy_object_failed_pinned);
	mono_counters_register ("# major copy_object() failed large or pinned chunk", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_copy_object_failed_large_pinned);
	mono_counters_register ("# major copy_object() failed to space", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_copy_object_failed_to_space);
#endif

	collector->section_size = MAJOR_SECTION_SIZE;
	collector->supports_cardtable = FALSE;
	collector->is_parallel = FALSE;

	collector->have_swept = &have_swept;

	collector->alloc_heap = major_alloc_heap;
	collector->is_object_live = major_is_object_live;
	collector->alloc_small_pinned_obj = major_alloc_small_pinned_obj;
	collector->alloc_degraded = major_alloc_degraded;
	collector->alloc_object = major_alloc_object;
	collector->free_pinned_object = free_pinned_object;
	collector->iterate_objects = major_iterate_objects;
	collector->free_non_pinned_object = major_free_non_pinned_object;
	collector->find_pin_queue_start_ends = major_find_pin_queue_start_ends;
	collector->pin_objects = major_pin_objects;
	collector->pin_major_object = pin_major_object;
	collector->init_to_space = major_init_to_space;
	collector->sweep = major_sweep;
	collector->check_scan_starts = major_check_scan_starts;
	collector->dump_heap = major_dump_heap;
	collector->get_used_size = major_get_used_size;
	collector->start_nursery_collection = major_start_nursery_collection;
	collector->finish_nursery_collection = major_finish_nursery_collection;
	collector->finish_major_collection = major_finish_major_collection;
	collector->ptr_is_in_non_pinned_space = major_ptr_is_in_non_pinned_space;
	collector->obj_is_from_pinned_alloc = obj_is_from_pinned_alloc;
	collector->report_pinned_memory_usage = major_report_pinned_memory_usage;
	collector->get_num_major_sections = get_num_major_sections;
	collector->handle_gc_param = NULL;
	collector->print_gc_param_usage = NULL;

	collector->major_ops.copy_or_mark_object = major_copy_or_mark_object;
	collector->major_ops.scan_object = major_scan_object;
}

#endif
