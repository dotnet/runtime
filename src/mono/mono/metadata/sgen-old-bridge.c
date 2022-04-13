/**
 * \file
 * Simple generational GC.
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#if defined (HAVE_SGEN_GC) && !defined (DISABLE_SGEN_GC_BRIDGE)

#include <stdlib.h>

#include "sgen/sgen-gc.h"
#include "sgen-bridge-internals.h"
#include "sgen/sgen-hash-table.h"
#include "sgen/sgen-qsort.h"
#include "sgen/sgen-client.h"
#include "utils/mono-logger-internals.h"

typedef struct {
	int size;
	int capacity;
	char *data;
} DynArray;

/*Specializations*/

typedef struct {
	DynArray array;
} DynIntArray;

typedef struct {
	DynArray array;
} DynPtrArray;

typedef struct {
	DynArray array;
} DynSCCArray;

/*
 * Bridge data for a single managed object
 *
 * FIXME: Optimizations:
 *
 * Don't allocate a srcs array for just one source.  Most objects have
 * just one source, so use the srcs pointer itself.
 */
typedef struct _HashEntry {
	GCObject *obj;	/* This is a duplicate - it's already stored in the hash table */

	gboolean is_bridge;
	gboolean is_visited;

	int finishing_time;

	// "Source" managed objects pointing at this destination
	DynPtrArray srcs;

	// Index in sccs array of SCC this object was folded into
	int scc_index;
} HashEntry;

typedef struct {
	HashEntry entry;
	double weight;
} HashEntryWithAccounting;

// The graph of managed objects/HashEntries is reduced to a graph of strongly connected components
typedef struct _SCC {
	int index;
	int api_index;

	// How many bridged objects does this SCC hold references to?
	int num_bridge_entries;

	// Index in global sccs array of SCCs holding pointers to this SCC
	DynIntArray xrefs;		/* these are incoming, not outgoing */
} SCC;

// Maps managed objects to corresponding HashEntry stricts
static SgenHashTable hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_OLD_BRIDGE_HASH_TABLE, INTERNAL_MEM_OLD_BRIDGE_HASH_TABLE_ENTRY, sizeof (HashEntry), mono_aligned_addr_hash, NULL);

static int current_time;

static gboolean bridge_accounting_enabled = FALSE;

static SgenBridgeProcessor *bridge_processor;

/* Core functions */
/* public */

/* private */

static void
dyn_array_init (DynArray *da)
{
	da->size = 0;
	da->capacity = 0;
	da->data = NULL;
}

static void
dyn_array_uninit (DynArray *da, int elem_size)
{
	if (da->capacity <= 0)
		return;

	sgen_free_internal_dynamic (da->data, elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = NULL;
}

static void
dyn_array_ensure_capacity (DynArray *da, int capacity, int elem_size)
{
	int old_capacity = da->capacity;
	char *new_data;

	if (capacity <= old_capacity)
		return;

	if (da->capacity == 0)
		da->capacity = 2;
	while (capacity > da->capacity)
		da->capacity *= 2;

	new_data = (char *)sgen_alloc_internal_dynamic (elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA, TRUE);
	memcpy (new_data, da->data, elem_size * da->size);
	sgen_free_internal_dynamic (da->data, elem_size * old_capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = new_data;
}

static void*
dyn_array_add (DynArray *da, int elem_size)
{
	void *p;

	dyn_array_ensure_capacity (da, da->size + 1, elem_size);

	p = da->data + da->size * elem_size;
	++da->size;
	return p;
}

/* int */
static void
dyn_array_int_init (DynIntArray *da)
{
	dyn_array_init (&da->array);
}

static void
dyn_array_int_uninit (DynIntArray *da)
{
	dyn_array_uninit (&da->array, sizeof (int));
}

static int
dyn_array_int_size (DynIntArray *da)
{
	return da->array.size;
}

static void
dyn_array_int_set_size (DynIntArray *da, int size)
{
	da->array.size = size;
}

static void
dyn_array_int_add (DynIntArray *da, int x)
{
	int *p = (int *)dyn_array_add (&da->array, sizeof (int));
	*p = x;
}

static int
dyn_array_int_get (DynIntArray *da, int x)
{
	return ((int*)da->array.data)[x];
}

static void
dyn_array_int_set (DynIntArray *da, int idx, int val)
{
	((int*)da->array.data)[idx] = val;
}

static void
dyn_array_int_ensure_capacity (DynIntArray *da, int capacity)
{
	dyn_array_ensure_capacity (&da->array, capacity, sizeof (int));
}

static void
dyn_array_int_set_all (DynIntArray *dst, DynIntArray *src)
{
	dyn_array_int_ensure_capacity (dst, src->array.size);
	memcpy (dst->array.data, src->array.data, src->array.size * sizeof (int));
	dst->array.size = src->array.size;
}

/* ptr */

static void
dyn_array_ptr_init (DynPtrArray *da)
{
	dyn_array_init (&da->array);
}

static void
dyn_array_ptr_uninit (DynPtrArray *da)
{
	dyn_array_uninit (&da->array, sizeof (void*));
}

static int
dyn_array_ptr_size (DynPtrArray *da)
{
	return da->array.size;
}

static void
dyn_array_ptr_set_size (DynPtrArray *da, int size)
{
	da->array.size = size;
}

static void*
dyn_array_ptr_get (DynPtrArray *da, int x)
{
	return ((void**)da->array.data)[x];
}

static void
dyn_array_ptr_add (DynPtrArray *da, void *ptr)
{
	void **p = (void **)dyn_array_add (&da->array, sizeof (void*));
	*p = ptr;
}

#define dyn_array_ptr_push dyn_array_ptr_add

static void*
dyn_array_ptr_pop (DynPtrArray *da)
{
	void *p;
	int size = da->array.size;
	g_assert (size > 0);
	p = dyn_array_ptr_get (da, size - 1);
	--da->array.size;
	return p;
}

/*SCC */

static void
dyn_array_scc_init (DynSCCArray *da)
{
	dyn_array_init (&da->array);
}

static void
dyn_array_scc_uninit (DynSCCArray *da)
{
	dyn_array_uninit (&da->array, sizeof (SCC));
}

static int
dyn_array_scc_size (DynSCCArray *da)
{
	return da->array.size;
}

static SCC*
dyn_array_scc_add (DynSCCArray *da)
{
	return (SCC *)dyn_array_add (&da->array, sizeof (SCC));
}

static SCC*
dyn_array_scc_get_ptr (DynSCCArray *da, int x)
{
	return &((SCC*)da->array.data)[x];
}

/* Merge code*/

static DynIntArray merge_array;

static gboolean
dyn_array_int_contains (DynIntArray *da, int x)
{
	int i;
	for (i = 0; i < dyn_array_int_size (da); ++i)
		if (dyn_array_int_get (da, i) == x)
			return TRUE;
	return FALSE;
}


static void
dyn_array_int_merge (DynIntArray *dst, DynIntArray *src)
{
	int i, j;

	dyn_array_int_ensure_capacity (&merge_array, dyn_array_int_size (dst) + dyn_array_int_size (src));
	dyn_array_int_set_size (&merge_array, 0);

	for (i = j = 0; i < dyn_array_int_size (dst) || j < dyn_array_int_size (src); ) {
		if (i < dyn_array_int_size (dst) && j < dyn_array_int_size (src)) {
			int a = dyn_array_int_get (dst, i);
			int b = dyn_array_int_get (src, j);
			if (a < b) {
				dyn_array_int_add (&merge_array, a);
				++i;
			} else if (a == b) {
				dyn_array_int_add (&merge_array, a);
				++i;
				++j;
			} else {
				dyn_array_int_add (&merge_array, b);
				++j;
			}
		} else if (i < dyn_array_int_size (dst)) {
			dyn_array_int_add (&merge_array, dyn_array_int_get (dst, i));
			++i;
		} else {
			dyn_array_int_add (&merge_array, dyn_array_int_get (src, j));
			++j;
		}
	}

	if (dyn_array_int_size (&merge_array) > dyn_array_int_size (dst)) {
		dyn_array_int_set_all (dst, &merge_array);
	}
}

static void
dyn_array_int_merge_one (DynIntArray *array, int value)
{
	int i;
	int tmp;
	int size = dyn_array_int_size (array);

	for (i = 0; i < size; ++i) {
		if (dyn_array_int_get (array, i) == value)
			return;
		else if (dyn_array_int_get (array, i) > value)
			break;
	}

	dyn_array_int_ensure_capacity (array, size + 1);

	if (i < size) {
		tmp = dyn_array_int_get (array, i);
		for (; i < size; ++i) {
			dyn_array_int_set (array, i, value);
			value = tmp;
			tmp = dyn_array_int_get (array, i + 1);
		}
		dyn_array_int_set (array, size, value);
	} else {
		dyn_array_int_set (array, size, value);
	}

	dyn_array_int_set_size (array, size + 1);
}


static void
set_config (const SgenBridgeProcessorConfig *config)
{
	if (config->accounting) {
		SgenHashTable table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_HASH_TABLE, INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY, sizeof (HashEntryWithAccounting), mono_aligned_addr_hash, NULL);
		bridge_accounting_enabled = TRUE;
		hash_table = table;
	}
}

static MonoGCBridgeObjectKind
class_kind (MonoClass *klass)
{
	return mono_bridge_callbacks.bridge_class_kind (klass);
}

static HashEntry*
get_hash_entry (GCObject *obj, gboolean *existing)
{
	HashEntry *entry = (HashEntry *)sgen_hash_table_lookup (&hash_table, obj);
	HashEntry new_entry;

	if (entry) {
		if (existing)
			*existing = TRUE;
		return entry;
	}
	if (existing)
		*existing = FALSE;

	memset (&new_entry, 0, sizeof (HashEntry));

	new_entry.obj = obj;
	dyn_array_ptr_init (&new_entry.srcs);
	new_entry.finishing_time = -1;
	new_entry.scc_index = -1;

	sgen_hash_table_replace (&hash_table, obj, &new_entry, NULL);

	return (HashEntry *)sgen_hash_table_lookup (&hash_table, obj);
}

static void
add_source (HashEntry *entry, HashEntry *src)
{
	dyn_array_ptr_add (&entry->srcs, src);
}

static void
free_data (void)
{
	GCObject *obj G_GNUC_UNUSED;
	HashEntry *entry;
	int max_srcs = 0;

	SGEN_HASH_TABLE_FOREACH (&hash_table, GCObject *, obj, HashEntry *, entry) {
		int entry_size = dyn_array_ptr_size (&entry->srcs);
		if (entry_size > max_srcs)
			max_srcs = entry_size;
		dyn_array_ptr_uninit (&entry->srcs);
	} SGEN_HASH_TABLE_FOREACH_END;

	sgen_hash_table_clean (&hash_table);

	dyn_array_int_uninit (&merge_array);
}

static HashEntry*
register_bridge_object (GCObject *obj)
{
	HashEntry *entry = get_hash_entry (obj, NULL);
	entry->is_bridge = TRUE;
	return entry;
}

static void
register_finishing_time (HashEntry *entry, int t)
{
	g_assert (entry->finishing_time < 0);
	entry->finishing_time = t;
}

static gboolean
object_is_live (GCObject **objp)
{
	GCObject *obj = *objp;
	GCObject *fwd = SGEN_OBJECT_IS_FORWARDED (obj);
	if (fwd) {
		*objp = fwd;
		return sgen_hash_table_lookup (&hash_table, fwd) == NULL;
	}
	if (!sgen_object_is_live (obj))
		return FALSE;
	return sgen_hash_table_lookup (&hash_table, obj) == NULL;
}

static DynPtrArray registered_bridges;
static DynPtrArray dfs_stack;

static int dfs1_passes, dfs2_passes;


#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		GCObject *dst = (GCObject*)*(ptr);			\
		if (dst && !object_is_live (&dst)) {			\
			dyn_array_ptr_push (&dfs_stack, obj_entry);	\
			dyn_array_ptr_push (&dfs_stack, get_hash_entry (dst, NULL)); \
		}							\
	} while (0)

static void
dfs1 (HashEntry *obj_entry)
{
	HashEntry *src;
	g_assert (dyn_array_ptr_size (&dfs_stack) == 0);

	dyn_array_ptr_push (&dfs_stack, NULL);
	dyn_array_ptr_push (&dfs_stack, obj_entry);

	do {
		GCObject *obj;
		++dfs1_passes;

		obj_entry = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);
		if (obj_entry) {
			char *start;
			mword desc;
			src = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);

			obj = obj_entry->obj;
			desc = sgen_obj_get_descriptor_safe (obj);

			if (src) {
				//g_print ("link %s -> %s\n", sgen_safe_name (src->obj), sgen_safe_name (obj));
				add_source (obj_entry, src);
			} else {
				//g_print ("starting with %s\n", sgen_safe_name (obj));
			}

			if (obj_entry->is_visited)
				continue;

			obj_entry->is_visited = TRUE;

			dyn_array_ptr_push (&dfs_stack, obj_entry);
			/* NULL marks that the next entry is to be finished */
			dyn_array_ptr_push (&dfs_stack, NULL);

			start = (char*)obj;
#include "sgen/sgen-scan-object.h"
		} else {
			obj_entry = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);

			//g_print ("finish %s\n", sgen_safe_name (obj_entry->obj));
			register_finishing_time (obj_entry, current_time++);
		}
	} while (dyn_array_ptr_size (&dfs_stack) > 0);
}

static void
scc_add_xref (SCC *src, SCC *dst)
{
	g_assert (src != dst);
	g_assert (src->index != dst->index);

	if (dyn_array_int_contains (&dst->xrefs, src->index))
		return;
	if (src->num_bridge_entries) {
		dyn_array_int_merge_one (&dst->xrefs, src->index);
	} else {
		int i;
		dyn_array_int_merge (&dst->xrefs, &src->xrefs);
		for (i = 0; i < dyn_array_int_size (&dst->xrefs); ++i)
			g_assert (dyn_array_int_get (&dst->xrefs, i) != dst->index);
	}
}

static void
scc_add_entry (SCC *scc, HashEntry *entry)
{
	g_assert (entry->scc_index < 0);
	entry->scc_index = scc->index;
	if (entry->is_bridge)
		++scc->num_bridge_entries;
}

static DynSCCArray sccs;
static SCC *current_scc;

static void
dfs2 (HashEntry *entry)
{
	int i;

	g_assert (dyn_array_ptr_size (&dfs_stack) == 0);

	dyn_array_ptr_push (&dfs_stack, entry);

	do {
		entry = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);
		++dfs2_passes;

		if (entry->scc_index >= 0) {
			if (entry->scc_index != current_scc->index)
				scc_add_xref (dyn_array_scc_get_ptr (&sccs, entry->scc_index), current_scc);
			continue;
		}

		scc_add_entry (current_scc, entry);

		for (i = 0; i < dyn_array_ptr_size (&entry->srcs); ++i)
			dyn_array_ptr_push (&dfs_stack, dyn_array_ptr_get (&entry->srcs, i));
	} while (dyn_array_ptr_size (&dfs_stack) > 0);
}

static int
compare_hash_entries (const HashEntry *e1, const HashEntry *e2)
{
	return e2->finishing_time - e1->finishing_time;
}

DEF_QSORT_INLINE(hash_entries, HashEntry*, compare_hash_entries)

static gint64 step_1, step_2, step_3, step_4, step_5, step_6;
static int fist_pass_links, second_pass_links, sccs_links;
static int max_sccs_links = 0;

static void
register_finalized_object (GCObject *obj)
{
	g_assert (sgen_need_bridge_processing ());
	dyn_array_ptr_push (&registered_bridges, obj);
}

static void
reset_data (void)
{
	dyn_array_ptr_set_size (&registered_bridges, 0);
}

static void
processing_stw_step (void)
{
	int i;
	int bridge_count;
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);

	if (!dyn_array_ptr_size (&registered_bridges))
		return;

	SGEN_TV_GETTIME (btv);

	/* first DFS pass */

	dyn_array_ptr_init (&dfs_stack);
	dyn_array_int_init (&merge_array);

	current_time = 0;
	/*
	First we insert all bridges into the hash table and then we do dfs1.

	It must be done in 2 steps since the bridge arrays doesn't come in reverse topological order,
	which means that we can have entry N pointing to entry N + 1.

	If we dfs1 entry N before N + 1 is registered we'll not consider N + 1 for this bridge
	pass and not create the required xref between the two.
	*/
	bridge_count = dyn_array_ptr_size (&registered_bridges);
	for (i = 0; i < bridge_count ; ++i)
		register_bridge_object ((GCObject *)dyn_array_ptr_get (&registered_bridges, i));

	for (i = 0; i < bridge_count; ++i)
		dfs1 (get_hash_entry ((GCObject *)dyn_array_ptr_get (&registered_bridges, i), NULL));

	SGEN_TV_GETTIME (atv);
	step_2 = SGEN_TV_ELAPSED (btv, atv);
}

static int num_registered_bridges, hash_table_size;

static void
processing_build_callback_data (int generation)
{
	int i, j;
	int num_sccs, num_xrefs;
	int max_entries, max_xrefs;
	GCObject *obj G_GNUC_UNUSED;
	HashEntry *entry;
	HashEntry **all_entries;
	MonoGCBridgeSCC **api_sccs;
	MonoGCBridgeXRef *api_xrefs;
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);

	g_assert (bridge_processor->num_sccs == 0 && bridge_processor->num_xrefs == 0);
	g_assert (!bridge_processor->api_sccs && !bridge_processor->api_xrefs);

	if (!dyn_array_ptr_size (&registered_bridges))
		return;

	g_assert (mono_bridge_processing_in_progress);

	SGEN_TV_GETTIME (atv);

	/* alloc and fill array of all entries */

	all_entries = (HashEntry **)sgen_alloc_internal_dynamic (sizeof (HashEntry*) * hash_table.num_entries, INTERNAL_MEM_BRIDGE_DATA, TRUE);

	j = 0;
	SGEN_HASH_TABLE_FOREACH (&hash_table, GCObject *, obj, HashEntry *, entry) {
		g_assert (entry->finishing_time >= 0);
		all_entries [j++] = entry;
		fist_pass_links += dyn_array_ptr_size (&entry->srcs);
	} SGEN_HASH_TABLE_FOREACH_END;
	g_assert (j == hash_table.num_entries);
	hash_table_size = hash_table.num_entries;

	/* sort array according to decreasing finishing time */
	qsort_hash_entries (all_entries, hash_table.num_entries);

	SGEN_TV_GETTIME (btv);
	step_3 = SGEN_TV_ELAPSED (atv, btv);

	/* second DFS pass */

	dyn_array_scc_init (&sccs);
	for (i = 0; i < hash_table.num_entries; ++i) {
		entry = all_entries [i];
		if (entry->scc_index < 0) {
			int index = dyn_array_scc_size (&sccs);
			current_scc = dyn_array_scc_add (&sccs);
			current_scc->index = index;
			current_scc->num_bridge_entries = 0;
			current_scc->api_index = -1;
			dyn_array_int_init (&current_scc->xrefs);

			dfs2 (entry);
		}
	}

	/*
	 * Compute the weight of each object. The weight of an object is its size plus the size of all
	 * objects it points do. When the an object is pointed by multiple objects we distribute it's weight
	 * equally among them. This distribution gives a rough estimate of the real impact of making the object
	 * go away.
	 *
	 * The reasoning for this model is that complex graphs with single roots will have a bridge with very high
	 * value in comparison to others.
	 *
	 * The all_entries array has all objects topologically sorted. To correctly propagate the weights it must be
	 * done in reverse topological order - so we calculate the weight of the pointed-to objects before processing
	 * pointer-from objects.
	 *
	 * We log those objects in the opposite order for no particular reason. The other constrain is that it should use the same
	 * direction as the other logging loop that records live/dead information.
	 */
	if (bridge_accounting_enabled) {
		for (i = hash_table.num_entries - 1; i >= 0; --i) {
			double w;
			HashEntryWithAccounting *entry_acc = (HashEntryWithAccounting*)all_entries [i];

			entry_acc->weight += (double)sgen_safe_object_get_size (entry_acc->entry.obj);
			w = entry_acc->weight / dyn_array_ptr_size (&entry_acc->entry.srcs);
			for (j = 0; j < dyn_array_ptr_size (&entry_acc->entry.srcs); ++j) {
				HashEntryWithAccounting *other = (HashEntryWithAccounting *)dyn_array_ptr_get (&entry_acc->entry.srcs, j);
				other->weight += w;
			}
		}
		for (i = 0; i < hash_table.num_entries; ++i) {
			HashEntryWithAccounting *entry_acc = (HashEntryWithAccounting*)all_entries [i];
			if (entry_acc->entry.is_bridge) {
				MonoClass *klass = SGEN_LOAD_VTABLE (entry_acc->entry.obj)->klass;
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_GC, "OBJECT %s::%s (%p) weight %f", m_class_get_name_space (klass), m_class_get_name (klass), entry_acc->entry.obj, entry_acc->weight);
			}
		}
	}

	for (i = 0; i < hash_table.num_entries; ++i) {
		entry = all_entries [i];
		second_pass_links += dyn_array_ptr_size (&entry->srcs);
	}

	SGEN_TV_GETTIME (atv);
	step_4 = SGEN_TV_ELAPSED (btv, atv);

	//g_print ("%d sccs\n", sccs.size);

	dyn_array_ptr_uninit (&dfs_stack);

	/* init data for callback */

	num_sccs = 0;
	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		g_assert (scc->index == i);
		if (scc->num_bridge_entries)
			++num_sccs;
		sccs_links += dyn_array_int_size (&scc->xrefs);
		max_sccs_links = MAX (max_sccs_links, dyn_array_int_size (&scc->xrefs));
	}

	api_sccs = (MonoGCBridgeSCC **)sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeSCC*) * num_sccs, INTERNAL_MEM_BRIDGE_DATA, TRUE);
	num_xrefs = 0;
	j = 0;
	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		if (!scc->num_bridge_entries)
			continue;

		api_sccs [j] = (MonoGCBridgeSCC *)sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeSCC) + sizeof (MonoObject*) * scc->num_bridge_entries, INTERNAL_MEM_BRIDGE_DATA, TRUE);
		api_sccs [j]->is_alive = FALSE;
		api_sccs [j]->num_objs = scc->num_bridge_entries;
		scc->num_bridge_entries = 0;
		scc->api_index = j++;

		num_xrefs += dyn_array_int_size (&scc->xrefs);
	}

	SGEN_HASH_TABLE_FOREACH (&hash_table, GCObject *, obj, HashEntry *, entry) {
		if (entry->is_bridge) {
			SCC *scc = dyn_array_scc_get_ptr (&sccs, entry->scc_index);
			api_sccs [scc->api_index]->objs [scc->num_bridge_entries++] = (MonoObject*)entry->obj;
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	api_xrefs = (MonoGCBridgeXRef *)sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeXRef) * num_xrefs, INTERNAL_MEM_BRIDGE_DATA, TRUE);
	j = 0;
	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		int k;
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		if (!scc->num_bridge_entries)
			continue;
		for (k = 0; k < dyn_array_int_size (&scc->xrefs); ++k) {
			SCC *src_scc = dyn_array_scc_get_ptr (&sccs, dyn_array_int_get (&scc->xrefs, k));
			if (!src_scc->num_bridge_entries)
				continue;
			api_xrefs [j].src_scc_index = src_scc->api_index;
			api_xrefs [j].dst_scc_index = scc->api_index;
			++j;
		}
	}

	SGEN_TV_GETTIME (btv);
	step_5 = SGEN_TV_ELAPSED (atv, btv);

	/* free data */

	j = 0;
	max_entries = max_xrefs = 0;
	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		if (scc->num_bridge_entries)
			++j;
		if (scc->num_bridge_entries > max_entries)
			max_entries = scc->num_bridge_entries;
		if (dyn_array_int_size (&scc->xrefs) > max_xrefs)
			max_xrefs = dyn_array_int_size (&scc->xrefs);
		dyn_array_int_uninit (&scc->xrefs);

	}
	dyn_array_scc_uninit (&sccs);

	sgen_free_internal_dynamic (all_entries, sizeof (HashEntry*) * hash_table.num_entries, INTERNAL_MEM_BRIDGE_DATA);

	free_data ();
	/* Empty the registered bridges array */
	num_registered_bridges = dyn_array_ptr_size (&registered_bridges);
	dyn_array_ptr_set_size (&registered_bridges, 0);

	SGEN_TV_GETTIME (atv);
	step_6 = SGEN_TV_ELAPSED (btv, atv);

	//g_print ("%d sccs containing bridges - %d max bridge objects - %d max xrefs\n", j, max_entries, max_xrefs);

	bridge_processor->num_sccs = num_sccs;
	bridge_processor->api_sccs = api_sccs;
	bridge_processor->num_xrefs = num_xrefs;
	bridge_processor->api_xrefs = api_xrefs;
}

static void
processing_after_callback (int generation)
{
	int i, j;
	int num_sccs = bridge_processor->num_sccs;
	MonoGCBridgeSCC **api_sccs = bridge_processor->api_sccs;

	if (bridge_accounting_enabled) {
		for (i = 0; i < num_sccs; ++i) {
			for (j = 0; j < api_sccs [i]->num_objs; ++j) {
				GCVTable vtable = SGEN_LOAD_VTABLE (api_sccs [i]->objs [j]);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_GC,
					"OBJECT %s.%s (%p) SCC [%d] %s",
						sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable), api_sccs [i]->objs [j],
						i,
						api_sccs [i]->is_alive  ? "ALIVE" : "DEAD");
			}
		}
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_GC, "GC_OLD_BRIDGE num-objects %d num_hash_entries %d sccs size %d init %.2fms df1 %.2fms sort %.2fms dfs2 %.2fms setup-cb %.2fms free-data %.2fms links %d/%d/%d/%d dfs passes %d/%d",
		num_registered_bridges, hash_table_size, dyn_array_scc_size (&sccs),
		step_1 / 10000.0f,
		step_2 / 10000.0f,
		step_3 / 10000.0f,
		step_4 / 10000.0f,
		step_5 / 10000.0f,
		step_6 / 10000.0f,
		fist_pass_links, second_pass_links, sccs_links, max_sccs_links,
		dfs1_passes, dfs2_passes);

	step_1 = 0; /* We must cleanup since this value is used as an accumulator. */
	fist_pass_links = second_pass_links = sccs_links = max_sccs_links = 0;
	dfs1_passes = dfs2_passes = 0;
}

static void
describe_pointer (GCObject *obj)
{
	HashEntry *entry;
	int i;

	for (i = 0; i < dyn_array_ptr_size (&registered_bridges); ++i) {
		if (obj == dyn_array_ptr_get (&registered_bridges, i)) {
			printf ("Pointer is a registered bridge object.\n");
			break;
		}
	}

	entry = (HashEntry *)sgen_hash_table_lookup (&hash_table, obj);
	if (!entry)
		return;

	printf ("Bridge hash table entry %p:\n", entry);
	printf ("  is bridge: %d\n", (int)entry->is_bridge);
	printf ("  is visited: %d\n", (int)entry->is_visited);
}

void
sgen_old_bridge_init (SgenBridgeProcessor *collector)
{
	collector->reset_data = reset_data;
	collector->processing_stw_step = processing_stw_step;
	collector->processing_build_callback_data = processing_build_callback_data;
	collector->processing_after_callback = processing_after_callback;
	collector->class_kind = class_kind;
	collector->register_finalized_object = register_finalized_object;
	collector->describe_pointer = describe_pointer;
	collector->set_config = set_config;

	bridge_processor = collector;
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (sgen_old_bridge);

#endif
