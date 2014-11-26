/*
 * sgen-pinning.c: The pin queue.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-pinning.h"
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-pointer-queue.h"

static SgenPointerQueue pin_queue;
static size_t last_num_pinned = 0;

#define PIN_HASH_SIZE 1024
static void *pin_hash_filter [PIN_HASH_SIZE];

void
sgen_init_pinning (void)
{
	memset (pin_hash_filter, 0, sizeof (pin_hash_filter));
	pin_queue.mem_type = INTERNAL_MEM_PIN_QUEUE;
}

void
sgen_finish_pinning (void)
{
	last_num_pinned = pin_queue.next_slot;
	sgen_pointer_queue_clear (&pin_queue);
}

void
sgen_pin_stage_ptr (void *ptr)
{
	/*very simple multiplicative hash function, tons better than simple and'ng */ 
	int hash_idx = ((mword)ptr * 1737350767) & (PIN_HASH_SIZE - 1);
	if (pin_hash_filter [hash_idx] == ptr)
		return;

	pin_hash_filter [hash_idx] = ptr;

	sgen_pointer_queue_add (&pin_queue, ptr);
}

gboolean
sgen_find_optimized_pin_queue_area (void *start, void *end, size_t *first_out, size_t *last_out)
{
	size_t first = sgen_pointer_queue_search (&pin_queue, start);
	size_t last = sgen_pointer_queue_search (&pin_queue, end);
	SGEN_ASSERT (0, last == pin_queue.next_slot || pin_queue.data [last] >= end, "Pin queue search gone awry");
	*first_out = first;
	*last_out = last;
	return first != last;
}

void**
sgen_pinning_get_entry (size_t index)
{
	SGEN_ASSERT (0, index <= pin_queue.next_slot, "Pin queue entry out of range");
	return &pin_queue.data [index];
}

void
sgen_find_section_pin_queue_start_end (GCMemSection *section)
{
	SGEN_LOG (6, "Pinning from section %p (%p-%p)", section, section->data, section->end_data);

	sgen_find_optimized_pin_queue_area (section->data, section->end_data,
			&section->pin_queue_first_entry, &section->pin_queue_last_entry);

	SGEN_LOG (6, "Found %zd pinning addresses in section %p",
			section->pin_queue_last_entry - section->pin_queue_first_entry, section);
}

/*This will setup the given section for the while pin queue. */
void
sgen_pinning_setup_section (GCMemSection *section)
{
	section->pin_queue_first_entry = 0;
	section->pin_queue_last_entry = pin_queue.next_slot;
}

void
sgen_pinning_trim_queue_to_section (GCMemSection *section)
{
	SGEN_ASSERT (0, section->pin_queue_first_entry == 0, "Pin queue trimming assumes the whole pin queue is used by the nursery");
	pin_queue.next_slot = section->pin_queue_last_entry;
}

/*
 * This is called when we've run out of memory during a major collection.
 *
 * After collecting potential pin entries and sorting the array, this is what it looks like:
 *
 * +--------------------+---------------------------------------------+--------------------+
 * | major heap entries |               nursery entries               | major heap entries |
 * +--------------------+---------------------------------------------+--------------------+
 *
 * Of course there might not be major heap entries before and/or after the nursery entries,
 * depending on where the major heap sections are in the address space, and whether there
 * were any potential pointers there.
 *
 * When we pin nursery objects, we compact the nursery part of the pin array, which leaves
 * discarded entries after the ones that actually pointed to nursery objects:
 *
 * +--------------------+-----------------+---------------------------+--------------------+
 * | major heap entries | nursery entries | discarded nursery entries | major heap entries |
 * +--------------------+-----------------+---------------------------+--------------------+
 *
 * When, due to being out of memory, we late pin more objects, the pin array looks like
 * this:
 *
 * +--------------------+-----------------+---------------------------+--------------------+--------------+
 * | major heap entries | nursery entries | discarded nursery entries | major heap entries | late entries |
 * +--------------------+-----------------+---------------------------+--------------------+--------------+
 *
 * This function gets rid of the discarded nursery entries by nulling them out.  Note that
 * we can late pin objects not only in the nursery but also in the major heap, which happens
 * when evacuation fails.
 */
void
sgen_pin_queue_clear_discarded_entries (GCMemSection *section, size_t max_pin_slot)
{
	void **start = sgen_pinning_get_entry (section->pin_queue_last_entry);
	void **end = sgen_pinning_get_entry (max_pin_slot);
	void *addr;

	for (; start < end; ++start) {
		addr = *start;
		if ((char*)addr < section->data || (char*)addr > section->end_data)
			break;
		*start = NULL;
	}
}

/* reduce the info in the pin queue, removing duplicate pointers and sorting them */
void
sgen_optimize_pin_queue (void)
{
	sgen_pointer_queue_sort_uniq (&pin_queue);
}

size_t
sgen_get_pinned_count (void)
{
	return pin_queue.next_slot;
}

void
sgen_dump_pin_queue (void)
{
	int i;

	for (i = 0; i < last_num_pinned; ++i) {
		void *ptr = pin_queue.data [i];
		SGEN_LOG (3, "Bastard pinning obj %p (%s), size: %zd", ptr, sgen_safe_name (ptr), sgen_safe_object_get_size (ptr));
	}
}

typedef struct _CementHashEntry CementHashEntry;
struct _CementHashEntry {
	char *obj;
	unsigned int count;
};

static CementHashEntry cement_hash [SGEN_CEMENT_HASH_SIZE];
static CementHashEntry cement_hash_concurrent [SGEN_CEMENT_HASH_SIZE];

static gboolean cement_enabled = TRUE;
static gboolean cement_concurrent = FALSE;

void
sgen_cement_init (gboolean enabled)
{
	cement_enabled = enabled;
}

void
sgen_cement_reset (void)
{
	SGEN_ASSERT (1, !cement_concurrent, "Concurrent cementing cannot simply be reset");

	memset (cement_hash, 0, sizeof (cement_hash));
	binary_protocol_cement_reset ();
}

/*
 * The reason we cannot simply reset cementing at the start of a
 * concurrent collection is that the nursery collections running
 * concurrently must keep pinning the cemented objects, because we
 * don't have the global remsets that point to them anymore - if the
 * nursery collector moved the cemented objects, we'd have invalid
 * pointers in the major heap.
 *
 * What we do instead is to reset cementing at the start of concurrent
 * collections in such a way that nursery collections happening during
 * the major collection still pin the formerly cemented objects.  We
 * have a shadow cementing table for that purpose.  The nursery
 * collections still work with the old cementing table, while the
 * major collector builds up a new cementing table, adding global
 * remsets whenever needed like usual.  When the major collector
 * finishes, the old cementing table is replaced by the new one.
 */

void
sgen_cement_concurrent_start (void)
{
	SGEN_ASSERT (1, !cement_concurrent, "Concurrent cementing has already been started");
	cement_concurrent = TRUE;

	memset (cement_hash_concurrent, 0, sizeof (cement_hash));
}

void
sgen_cement_concurrent_finish (void)
{
	SGEN_ASSERT (1, cement_concurrent, "Concurrent cementing hasn't been started");
	cement_concurrent = FALSE;

	memcpy (cement_hash, cement_hash_concurrent, sizeof (cement_hash));
}

gboolean
sgen_cement_lookup (char *obj)
{
	guint hv = mono_aligned_addr_hash (obj);
	int i = SGEN_CEMENT_HASH (hv);

	SGEN_ASSERT (5, sgen_ptr_in_nursery (obj), "Looking up cementing for non-nursery objects makes no sense");

	if (!cement_enabled)
		return FALSE;

	if (!cement_hash [i].obj)
		return FALSE;
	if (cement_hash [i].obj != obj)
		return FALSE;

	return cement_hash [i].count >= SGEN_CEMENT_THRESHOLD;
}

gboolean
sgen_cement_lookup_or_register (char *obj)
{
	guint hv;
	int i;
	CementHashEntry *hash;
	gboolean concurrent_cementing = sgen_concurrent_collection_in_progress ();

	if (!cement_enabled)
		return FALSE;

	if (concurrent_cementing)
		SGEN_ASSERT (5, cement_concurrent, "Cementing wasn't inited with concurrent flag");

	if (concurrent_cementing)
		hash = cement_hash_concurrent;
	else
		hash = cement_hash;

	hv = mono_aligned_addr_hash (obj);
	i = SGEN_CEMENT_HASH (hv);

	SGEN_ASSERT (5, sgen_ptr_in_nursery (obj), "Can only cement pointers to nursery objects");

	if (!hash [i].obj) {
		SGEN_ASSERT (5, !hash [i].count, "Cementing hash inconsistent");
		hash [i].obj = obj;
	} else if (hash [i].obj != obj) {
		return FALSE;
	}

	if (hash [i].count >= SGEN_CEMENT_THRESHOLD)
		return TRUE;

	++hash [i].count;
	if (hash [i].count == SGEN_CEMENT_THRESHOLD) {
		SGEN_ASSERT (9, SGEN_OBJECT_IS_PINNED (obj), "Can only cement pinned objects");
		SGEN_CEMENT_OBJECT (obj);

		if (G_UNLIKELY (MONO_GC_OBJ_CEMENTED_ENABLED())) {
			MonoVTable *vt G_GNUC_UNUSED = (MonoVTable*)SGEN_LOAD_VTABLE (obj);
			MONO_GC_OBJ_CEMENTED ((mword)obj, sgen_safe_object_get_size ((MonoObject*)obj),
					vt->klass->name_space, vt->klass->name);
		}
		binary_protocol_cement (obj, (gpointer)SGEN_LOAD_VTABLE (obj),
				(int)sgen_safe_object_get_size ((MonoObject*)obj));
	}

	return FALSE;
}

static void
pin_from_hash (CementHashEntry *hash, gboolean has_been_reset)
{
	int i;
	for (i = 0; i < SGEN_CEMENT_HASH_SIZE; ++i) {
		if (!hash [i].count)
			continue;

		if (has_been_reset)
			SGEN_ASSERT (5, hash [i].count >= SGEN_CEMENT_THRESHOLD, "Cementing hash inconsistent");

		sgen_pin_stage_ptr (hash [i].obj);
		/* FIXME: do pin stats if enabled */

		SGEN_CEMENT_OBJECT (hash [i].obj);
	}
}

void
sgen_pin_cemented_objects (void)
{
	pin_from_hash (cement_hash, TRUE);
	if (cement_concurrent)
		pin_from_hash (cement_hash_concurrent, FALSE);
}

void
sgen_cement_clear_below_threshold (void)
{
	int i;
	for (i = 0; i < SGEN_CEMENT_HASH_SIZE; ++i) {
		if (cement_hash [i].count < SGEN_CEMENT_THRESHOLD) {
			cement_hash [i].obj = NULL;
			cement_hash [i].count = 0;
		}
	}
}

#endif /* HAVE_SGEN_GC */
