/*
 * sgen-bridge.c: Simple generational GC.
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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

#include <stdlib.h>

#include "sgen-gc.h"
#include "sgen-bridge.h"
#include "sgen-hash-table.h"
#include "sgen-qsort.h"
#include "utils/mono-logger-internal.h"
#include "utils/mono-time.h"
#include "utils/mono-compiler.h"


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
 * FIXME: Optimizations:
 *
 * Don't allocate a scrs array for just one source.  Most objects have
 * just one source, so use the srcs pointer itself.
 */
typedef struct _HashEntry {
	MonoObject *obj;	/* This is a duplicate - it's already stored in the hash table */

	gboolean is_bridge;
	gboolean is_visited;

	int finishing_time;

	DynPtrArray srcs;

	int scc_index;
} HashEntry;

typedef struct _SCC {
	int index;
	int api_index;
	int num_bridge_entries;
	DynIntArray xrefs;		/* these are incoming, not outgoing */
} SCC;

static SgenHashTable hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_HASH_TABLE, INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY, sizeof (HashEntry), mono_aligned_addr_hash, NULL);

static MonoGCBridgeCallbacks bridge_callbacks;

static int current_time;

gboolean bridge_processing_in_progress = FALSE;



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

	new_data = sgen_alloc_internal_dynamic (elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA, TRUE);
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
	int *p = dyn_array_add (&da->array, sizeof (int));
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
	void **p = dyn_array_add (&da->array, sizeof (void*));
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
	return dyn_array_add (&da->array, sizeof (SCC));
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

void
mono_gc_wait_for_bridge_processing (void)
{
	if (!bridge_processing_in_progress)
		return;

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_GC, "GC_BRIDGE waiting for bridge processing to finish");

	sgen_gc_lock ();
	sgen_gc_unlock ();
}

void
mono_gc_register_bridge_callbacks (MonoGCBridgeCallbacks *callbacks)
{
	if (callbacks->bridge_version != SGEN_BRIDGE_VERSION)
		g_error ("Invalid bridge callback version. Expected %d but got %d\n", SGEN_BRIDGE_VERSION, callbacks->bridge_version);

	bridge_callbacks = *callbacks;
}

gboolean
sgen_is_bridge_object (MonoObject *obj)
{
	if ((obj->vtable->gc_bits & SGEN_GC_BIT_BRIDGE_OBJECT) != SGEN_GC_BIT_BRIDGE_OBJECT)
		return FALSE;
	return bridge_callbacks.is_bridge_object (obj);
}

MonoGCBridgeObjectKind
sgen_bridge_class_kind (MonoClass *class)
{
	return bridge_callbacks.bridge_class_kind (class);
}

gboolean
sgen_need_bridge_processing (void)
{
	return bridge_callbacks.cross_references != NULL;
}

static HashEntry*
get_hash_entry (MonoObject *obj, gboolean *existing)
{
	HashEntry *entry = sgen_hash_table_lookup (&hash_table, obj);
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

	return sgen_hash_table_lookup (&hash_table, obj);
}

static void
add_source (HashEntry *entry, HashEntry *src)
{
	dyn_array_ptr_add (&entry->srcs, src);
}

static void
free_data (void)
{
	MonoObject *obj;
	HashEntry *entry;
	int total_srcs = 0;
	int max_srcs = 0;

	SGEN_HASH_TABLE_FOREACH (&hash_table, obj, entry) {
		int entry_size = dyn_array_ptr_size (&entry->srcs);
		total_srcs += entry_size;
		if (entry_size > max_srcs)
			max_srcs = entry_size;
		dyn_array_ptr_uninit (&entry->srcs);
	} SGEN_HASH_TABLE_FOREACH_END;

	sgen_hash_table_clean (&hash_table);

	dyn_array_int_uninit (&merge_array);
	//g_print ("total srcs %d - max %d\n", total_srcs, max_srcs);
}

static HashEntry*
register_bridge_object (MonoObject *obj)
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
object_is_live (MonoObject **objp)
{
	MonoObject *obj = *objp;
	MonoObject *fwd = SGEN_OBJECT_IS_FORWARDED (obj);
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

static int dsf1_passes, dsf2_passes;


#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		MonoObject *dst = (MonoObject*)*(ptr);			\
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
		MonoObject *obj;
		char *start;
		++dsf1_passes;

		obj_entry = dyn_array_ptr_pop (&dfs_stack);
		if (obj_entry) {
			src = dyn_array_ptr_pop (&dfs_stack);

			obj = obj_entry->obj;
			start = (char*)obj;

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

#include "sgen-scan-object.h"
		} else {
			obj_entry = dyn_array_ptr_pop (&dfs_stack);

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
		entry = dyn_array_ptr_pop (&dfs_stack);
		++dsf2_passes;

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

static unsigned long step_1, step_2, step_3, step_4, step_5, step_6, step_7, step_8;
static int fist_pass_links, second_pass_links, sccs_links;
static int max_sccs_links = 0;

void
sgen_bridge_register_finalized_object (MonoObject *obj)
{
	g_assert (sgen_need_bridge_processing ());
	dyn_array_ptr_push (&registered_bridges, obj);
}

void
sgen_bridge_reset_data (void)
{
	dyn_array_ptr_set_size (&registered_bridges, 0);
}

void
sgen_bridge_processing_stw_step (void)
{
	int i;
	int bridge_count;
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);

	if (!dyn_array_ptr_size (&registered_bridges))
		return;

	/*
	 * bridge_processing_in_progress must be set with the world
	 * stopped.  If not there would be race conditions.
	 */
	bridge_processing_in_progress = TRUE;

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
		register_bridge_object (dyn_array_ptr_get (&registered_bridges, i));

	for (i = 0; i < bridge_count; ++i)
		dfs1 (get_hash_entry (dyn_array_ptr_get (&registered_bridges, i), NULL));

	SGEN_TV_GETTIME (atv);
	step_2 = SGEN_TV_ELAPSED (btv, atv);
}

static mono_bool
is_bridge_object_alive (MonoObject *obj, void *data)
{
	SgenHashTable *table = data;
	unsigned char *value = sgen_hash_table_lookup (table, obj);
	if (!value)
		return TRUE;
	return *value;
}

void
sgen_bridge_processing_finish (int generation)
{
	int i, j;
	int num_sccs, num_xrefs;
	int max_entries, max_xrefs;
	int hash_table_size, sccs_size;
	MonoObject *obj;
	HashEntry *entry;
	int num_registered_bridges;
	HashEntry **all_entries;
	MonoGCBridgeSCC **api_sccs;
	MonoGCBridgeXRef *api_xrefs;
	SgenHashTable alive_hash = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE, INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE_ENTRY, 1, mono_aligned_addr_hash, NULL);
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);

	if (!dyn_array_ptr_size (&registered_bridges))
		return;

	g_assert (bridge_processing_in_progress);

	SGEN_TV_GETTIME (atv);

	/* alloc and fill array of all entries */

	all_entries = sgen_alloc_internal_dynamic (sizeof (HashEntry*) * hash_table.num_entries, INTERNAL_MEM_BRIDGE_DATA, TRUE);

	j = 0;
	SGEN_HASH_TABLE_FOREACH (&hash_table, obj, entry) {
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
		HashEntry *entry = all_entries [i];
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

	sccs_size = dyn_array_scc_size (&sccs);

	for (i = 0; i < hash_table.num_entries; ++i) {
		HashEntry *entry = all_entries [i];
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

	api_sccs = sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeSCC*) * num_sccs, INTERNAL_MEM_BRIDGE_DATA, TRUE);
	num_xrefs = 0;
	j = 0;
	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		if (!scc->num_bridge_entries)
			continue;

		api_sccs [j] = sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeSCC) + sizeof (MonoObject*) * scc->num_bridge_entries, INTERNAL_MEM_BRIDGE_DATA, TRUE);
		api_sccs [j]->is_alive = FALSE;
		api_sccs [j]->num_objs = scc->num_bridge_entries;
		scc->num_bridge_entries = 0;
		scc->api_index = j++;

		num_xrefs += dyn_array_int_size (&scc->xrefs);
	}

	SGEN_HASH_TABLE_FOREACH (&hash_table, obj, entry) {
		if (entry->is_bridge) {
			SCC *scc = dyn_array_scc_get_ptr (&sccs, entry->scc_index);
			api_sccs [scc->api_index]->objs [scc->num_bridge_entries++] = entry->obj;
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	api_xrefs = sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeXRef) * num_xrefs, INTERNAL_MEM_BRIDGE_DATA, TRUE);
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

	/* callback */

	bridge_callbacks.cross_references (num_sccs, api_sccs, num_xrefs, api_xrefs);

	/* Release for finalization those objects we no longer care. */
	SGEN_TV_GETTIME (btv);
	step_7 = SGEN_TV_ELAPSED (atv, btv);

	for (i = 0; i < num_sccs; ++i) {
		unsigned char alive = api_sccs [i]->is_alive ? 1 : 0;
		for (j = 0; j < api_sccs [i]->num_objs; ++j) {
			/* Build hash table for nulling weak links. */
			sgen_hash_table_replace (&alive_hash, api_sccs [i]->objs [j], &alive, NULL);

			/* Release for finalization those objects we no longer care. */
			if (!api_sccs [i]->is_alive)
				sgen_mark_bridge_object (api_sccs [i]->objs [j]);
		}
	}

	/* Null weak links to dead objects. */
	sgen_null_links_with_predicate (GENERATION_NURSERY, is_bridge_object_alive, &alive_hash);
	if (generation == GENERATION_OLD)
		sgen_null_links_with_predicate (GENERATION_OLD, is_bridge_object_alive, &alive_hash);

	sgen_hash_table_clean (&alive_hash);

	/* free callback data */

	for (i = 0; i < num_sccs; ++i) {
		sgen_free_internal_dynamic (api_sccs [i],
				sizeof (MonoGCBridgeSCC) + sizeof (MonoObject*) * api_sccs [i]->num_objs,
				INTERNAL_MEM_BRIDGE_DATA);
	}
	sgen_free_internal_dynamic (api_sccs, sizeof (MonoGCBridgeSCC*) * num_sccs, INTERNAL_MEM_BRIDGE_DATA);

	sgen_free_internal_dynamic (api_xrefs, sizeof (MonoGCBridgeXRef) * num_xrefs, INTERNAL_MEM_BRIDGE_DATA);

	SGEN_TV_GETTIME (atv);
	step_8 = SGEN_TV_ELAPSED (btv, atv);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_GC, "GC_BRIDGE num-objects %d num_hash_entries %d sccs size %d init %.2fms df1 %.2fms sort %.2fms dfs2 %.2fms setup-cb %.2fms free-data %.2fms user-cb %.2fms clenanup %.2fms links %d/%d/%d/%d dfs passes %d/%d",
		num_registered_bridges, hash_table_size, dyn_array_scc_size (&sccs),
		step_1 / 1000.0f,
		step_2 / 1000.0f,
		step_3 / 1000.0f,
		step_4 / 1000.0f,
		step_5 / 1000.0f,
		step_6 / 1000.0f,
		step_7 / 1000.0f,
		step_8 / 1000.f,
		fist_pass_links, second_pass_links, sccs_links, max_sccs_links,
		dsf1_passes, dsf2_passes);

	step_1 = 0; /* We must cleanup since this value is used as an accumulator. */

	bridge_processing_in_progress = FALSE;
}

void
sgen_bridge_describe_pointer (MonoObject *obj)
{
	HashEntry *entry;
	int i;

	for (i = 0; i < dyn_array_ptr_size (&registered_bridges); ++i) {
		if (obj == dyn_array_ptr_get (&registered_bridges, i)) {
			printf ("Pointer is a registered bridge object.\n");
			break;
		}
	}

	entry = sgen_hash_table_lookup (&hash_table, obj);
	if (!entry)
		return;

	printf ("Bridge hash table entry %p:\n", entry);
	printf ("  is bridge: %d\n", (int)entry->is_bridge);
	printf ("  is visited: %d\n", (int)entry->is_visited);
}

static const char *bridge_class;

static MonoGCBridgeObjectKind
bridge_test_bridge_class_kind (MonoClass *class)
{
	if (!strcmp (bridge_class, class->name))
		return GC_BRIDGE_TRANSPARENT_BRIDGE_CLASS;
	return GC_BRIDGE_TRANSPARENT_CLASS;
}

static gboolean
bridge_test_is_bridge_object (MonoObject *object)
{
	return TRUE;
}

static void
bridge_test_cross_reference (int num_sccs, MonoGCBridgeSCC **sccs, int num_xrefs, MonoGCBridgeXRef *xrefs)
{
	int i;
	for (i = 0; i < num_sccs; ++i) {
		int j;
	//	g_print ("--- SCC %d\n", i);
		for (j = 0; j < sccs [i]->num_objs; ++j) {
	//		g_print ("  %s\n", sgen_safe_name (sccs [i]->objs [j]));
			if (i & 1) /*retain half of the bridged objects */
				sccs [i]->is_alive = TRUE;
		}
	}
	for (i = 0; i < num_xrefs; ++i) {
		g_assert (xrefs [i].src_scc_index >= 0 && xrefs [i].src_scc_index < num_sccs);
		g_assert (xrefs [i].dst_scc_index >= 0 && xrefs [i].dst_scc_index < num_sccs);
	//	g_print ("%d -> %d\n", xrefs [i].src_scc_index, xrefs [i].dst_scc_index);
	}
}

static MonoClassField *mono_bridge_test_field;

enum {
	BRIDGE_DEAD,
	BRIDGE_ROOT,
	BRIDGE_SAME_SCC,
	BRIDGE_XREF,
};

static gboolean
test_scc (MonoGCBridgeSCC *scc, int i)
{
	int status = BRIDGE_DEAD;
	mono_field_get_value (scc->objs [i], mono_bridge_test_field, &status);
	return status > 0;
}

static void
mark_scc (MonoGCBridgeSCC *scc, int value)
{
	int i;
	for (i = 0; i < scc->num_objs; ++i) {
		if (!test_scc (scc, i)) {
			int status = value;
			mono_field_set_value (scc->objs [i], mono_bridge_test_field, &status);
		}
	}
}

static void
bridge_test_cross_reference2 (int num_sccs, MonoGCBridgeSCC **sccs, int num_xrefs, MonoGCBridgeXRef *xrefs)
{
	int i;
	gboolean modified;

	if (!mono_bridge_test_field) {
		mono_bridge_test_field = mono_class_get_field_from_name (mono_object_get_class (sccs[0]->objs [0]), "__test");
		g_assert (mono_bridge_test_field);
	}

	/*We mark all objects in a scc with live objects as reachable by scc*/
	for (i = 0; i < num_sccs; ++i) {
		int j;
		gboolean live = FALSE;
		for (j = 0; j < sccs [i]->num_objs; ++j) {
			if (test_scc (sccs [i], j)) {
				live = TRUE;
				break;
			}
		}
		if (!live)
			continue;
		for (j = 0; j < sccs [i]->num_objs; ++j) {
			if (!test_scc (sccs [i], j)) {
				int status = BRIDGE_SAME_SCC;
				mono_field_set_value (sccs [i]->objs [j], mono_bridge_test_field, &status);
			}
		}
	}

	/*Now we mark the transitive closure of reachable objects from the xrefs*/
	modified = TRUE;
	while (modified) {
		modified = FALSE;
		/* Mark all objects that are brought to life due to xrefs*/
		for (i = 0; i < num_xrefs; ++i) {
			MonoGCBridgeXRef ref = xrefs [i];
			if (test_scc (sccs [ref.src_scc_index], 0) && !test_scc (sccs [ref.dst_scc_index], 0)) {
				modified = TRUE;
				mark_scc (sccs [ref.dst_scc_index], BRIDGE_XREF);
			}
		}
	}

	/* keep everything in memory, all we want to do is test persistence */
	for (i = 0; i < num_sccs; ++i)
		sccs [i]->is_alive = TRUE;
}

void
sgen_register_test_bridge_callbacks (const char *bridge_class_name)
{
	MonoGCBridgeCallbacks callbacks;
	callbacks.bridge_version = SGEN_BRIDGE_VERSION;
	callbacks.bridge_class_kind = bridge_test_bridge_class_kind;
	callbacks.is_bridge_object = bridge_test_is_bridge_object;
	callbacks.cross_references = bridge_class_name[0] == '2' ? bridge_test_cross_reference2 : bridge_test_cross_reference;
	mono_gc_register_bridge_callbacks (&callbacks);
	bridge_class = bridge_class_name + (bridge_class_name[0] == '2' ? 1 : 0);
}

#endif
