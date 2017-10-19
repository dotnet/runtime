/**
 * \file
 * A pointer array list that doesn't require reallocs
 *
 * Copyright (C) 2016 Xamarin Inc
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

#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-array-list.h"

static void
sgen_array_list_grow (SgenArrayList *array, guint32 old_capacity)
{
	const guint32 new_bucket = sgen_array_list_index_bucket (old_capacity);
	const guint32 growth = sgen_array_list_bucket_size (new_bucket);
	const guint32 new_capacity = old_capacity + growth;
	const guint32 new_bucket_size = sizeof (**array->entries) * growth;
	gpointer *entries;
	if (array->capacity >= new_capacity)
		return;
	if (array->mem_type != -1)
		entries = (gpointer*) sgen_alloc_internal_dynamic (new_bucket_size, array->mem_type, TRUE);
	else
		entries = (gpointer*) g_malloc0 (new_bucket_size);
	if (array->bucket_alloc_callback)
		array->bucket_alloc_callback (entries, new_bucket_size, TRUE);
	/*
	 * The zeroing of the newly allocated bucket must be complete before storing
	 * the new bucket pointer.
	 */
	mono_memory_write_barrier ();
	if (mono_atomic_cas_ptr ((volatile gpointer *)&array->entries [new_bucket], entries, NULL) == NULL) {
		/*
		 * It must not be the case that we succeeded in setting the bucket
		 * pointer, while someone else succeeded in changing the capacity.
		 */
		if (mono_atomic_cas_i32 ((volatile gint32 *)&array->capacity, (gint32)new_capacity, (gint32)old_capacity) != (gint32)old_capacity)
			g_assert_not_reached ();
		array->slot_hint = old_capacity;
		return;
	}
	/* Someone beat us to the allocation. */
	if (array->bucket_alloc_callback)
		array->bucket_alloc_callback (entries, new_bucket_size, FALSE);
	if (array->mem_type != -1)
		sgen_free_internal_dynamic (entries, new_bucket_size, array->mem_type);
	else
		g_free (entries);
}

static guint32
sgen_array_list_find_unset (SgenArrayList *array, guint32 capacity)
{
	if (!array->is_slot_set_func) {
		guint32 next_slot = array->next_slot;
		/* We can't lookup empty slots, use next_slot */
		if (next_slot < capacity)
			return next_slot;
	} else {
		guint32 slot_hint = array->slot_hint;
		guint32 index;
		volatile gpointer *slot;

		SGEN_ARRAY_LIST_FOREACH_SLOT_RANGE(array, slot_hint, capacity, slot, index) {
			if (!array->is_slot_set_func (slot))
				return index;
		} SGEN_ARRAY_LIST_END_FOREACH_SLOT_RANGE;

		SGEN_ARRAY_LIST_FOREACH_SLOT_RANGE (array, 0, slot_hint, slot, index) {
			if (!array->is_slot_set_func (slot))
				return index;
		} SGEN_ARRAY_LIST_END_FOREACH_SLOT_RANGE;
	}

	return -1;
}

static void
sgen_array_list_update_next_slot (SgenArrayList *array, guint32 new_index)
{
	if (!array->set_slot_func) {
		/*
		 * If we don't have a custom setter it means we don't have thread
		 * safety requirements.
		 */
		if (new_index >= array->next_slot)
			array->next_slot = new_index + 1;
	} else {
		guint32 old_next_slot;
		/* Thread safe update */
		do {
			old_next_slot = array->next_slot;
			if (new_index < old_next_slot)
				break;
		} while (mono_atomic_cas_i32 ((volatile gint32 *)&array->next_slot, (gint32)(new_index + 1), (gint32)old_next_slot) != (gint32)old_next_slot);
	}
}

/*
 * Extension for the array list that allows fast allocation and index based fetching
 * of long lived memory of various sizes, without the need of realloc. Not thread safe.
 */
guint32
sgen_array_list_alloc_block (SgenArrayList *array, guint32 slots_to_add)
{
	guint32 new_index = array->next_slot;
	guint32 old_capacity = array->capacity;

	/* FIXME Don't allocate arrays that will be skipped */
	/* There are no empty arrays between next_slot and capacity because we allocate incrementally */
	while ((old_capacity - new_index) < slots_to_add) {
		sgen_array_list_grow (array, old_capacity);
		new_index = old_capacity;
		old_capacity = array->capacity;
	}

	SGEN_ASSERT (0, sgen_array_list_index_bucket (new_index) == sgen_array_list_index_bucket (new_index + slots_to_add - 1),
			"We failed to allocate a continuous block of slots");

	array->next_slot = new_index + slots_to_add;
	/* The slot address will point to the allocated memory */
	return new_index;
}

guint32
sgen_array_list_add (SgenArrayList *array, gpointer ptr, int data, gboolean increase_size_before_set)
{
	guint32 index, capacity;
	volatile gpointer *slot;

	if (!array->capacity)
		sgen_array_list_grow (array, 0);
retry:
	capacity = array->capacity;
	index = sgen_array_list_find_unset (array, capacity);
	if (index == -1) {
		sgen_array_list_grow (array, capacity);
		goto retry;
	}
	array->slot_hint = index;

	if (increase_size_before_set) {
		sgen_array_list_update_next_slot (array, index);
		mono_memory_write_barrier ();
	}

	slot = sgen_array_list_get_slot (array, index);
	if (array->set_slot_func) {
		if (!array->set_slot_func (slot, ptr, data))
			goto retry;
	} else {
		*slot = ptr;
	}

	if (!increase_size_before_set) {
		mono_memory_write_barrier ();
		sgen_array_list_update_next_slot (array, index);
	}

	return index;
}

/*
 * Does a linear search through the pointer array to find `ptr`.  Returns the index if
 * found, otherwise (guint32)-1.
 */
guint32
sgen_array_list_find (SgenArrayList *array, gpointer ptr)
{
	volatile gpointer *slot;

	SGEN_ARRAY_LIST_FOREACH_SLOT (array, slot) {
		if (*slot == ptr)
			return __index;
	} SGEN_ARRAY_LIST_END_FOREACH_SLOT;
	return (guint32)-1;
}

gboolean
sgen_array_list_default_cas_setter (volatile gpointer *slot, gpointer ptr, int data)
{
	if (mono_atomic_cas_ptr (slot, ptr, NULL) == NULL)
		return TRUE;
	return FALSE;
}

gboolean
sgen_array_list_default_is_slot_set (volatile gpointer *slot)
{
	return *slot != NULL;
}

/* Removes all NULL pointers from the array. Not thread safe */
void
sgen_array_list_remove_nulls (SgenArrayList *array)
{
	guint32 start = 0;
	volatile gpointer *slot;
	gboolean skipped = FALSE;

	SGEN_ARRAY_LIST_FOREACH_SLOT (array, slot) {
		if (*slot) {
			*sgen_array_list_get_slot (array, start++) = *slot;
			if (skipped)
				*slot = NULL;
		} else {
			skipped = TRUE;
		}
	} SGEN_ARRAY_LIST_END_FOREACH_SLOT;

	mono_memory_write_barrier ();
	array->next_slot = start;
	array->slot_hint = start;
}

#endif
