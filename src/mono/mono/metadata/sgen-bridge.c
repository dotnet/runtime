/*
 * sgen-bridge.c: Simple generational GC.
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
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

typedef struct {
	int size;
	int elem_size;
	int capacity;
	char *data;
} DynArray;

#define DYN_ARRAY_REF(da,i)	((void*)((da)->data + (i) * (da)->elem_size))
#define DYN_ARRAY_PTR_REF(da,i)	(((void**)(da)->data) [(i)])
#define DYN_ARRAY_INT_REF(da,i)	(((int*)(da)->data) [(i)])

static void
dyn_array_init (DynArray *da, int elem_size)
{
	da->size = 0;
	da->elem_size = elem_size;
	da->capacity = 0;
	da->data = NULL;
}

static void
dyn_array_ptr_init (DynArray *da)
{
	dyn_array_init (da, sizeof (void*));
}

static void
dyn_array_int_init (DynArray *da)
{
	dyn_array_init (da, sizeof (int));
}

static void
dyn_array_uninit (DynArray *da)
{
	if (da->capacity <= 0)
		return;

	mono_sgen_free_internal_dynamic (da->data, da->elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = NULL;
}

static void
dyn_array_ensure_capacity (DynArray *da, int capacity)
{
	int old_capacity = da->capacity;
	char *new_data;

	if (capacity <= old_capacity)
		return;

	if (da->capacity == 0)
		da->capacity = 2;
	while (capacity > da->capacity)
		da->capacity *= 2;

	new_data = mono_sgen_alloc_internal_dynamic (da->elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA);
	memcpy (new_data, da->data, da->elem_size * da->size);
	mono_sgen_free_internal_dynamic (da->data, da->elem_size * old_capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = new_data;
}

static void*
dyn_array_add (DynArray *da)
{
	void *p;

	dyn_array_ensure_capacity (da, da->size + 1);

	p = DYN_ARRAY_REF (da, da->size);
	++da->size;
	return p;
}

static void
dyn_array_ptr_add (DynArray *da, void *ptr)
{
	void **p = dyn_array_add (da);
	*p = ptr;
}

#define dyn_array_ptr_push dyn_array_ptr_add

static void*
dyn_array_ptr_pop (DynArray *da)
{
	void *p;
	g_assert (da->size > 0);
	p = DYN_ARRAY_PTR_REF (da, da->size - 1);
	--da->size;
	return p;
}

static void
dyn_array_int_add (DynArray *da, int x)
{
	int *p = dyn_array_add (da);
	*p = x;
}

/*
static gboolean
dyn_array_ptr_contains (DynArray *da, void *ptr)
{
	int i;
	for (i = 0; i < da->size; ++i)
		if (DYN_ARRAY_PTR_REF (da, i) == ptr)
			return TRUE;
	return FALSE;
}
*/

static gboolean
dyn_array_int_contains (DynArray *da, int x)
{
	int i;
	for (i = 0; i < da->size; ++i)
		if (DYN_ARRAY_INT_REF (da, i) == x)
			return TRUE;
	return FALSE;
}

static void
dyn_array_append (DynArray *dst, DynArray *src)
{
	g_assert (dst->elem_size == src->elem_size);

	dyn_array_ensure_capacity (dst, dst->size + src->size);
	memcpy (DYN_ARRAY_REF (dst, dst->size), DYN_ARRAY_REF (src, 0), src->size * src->elem_size);
	dst->size += src->size;
}

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

	DynArray srcs;

	int scc_index;
} HashEntry;

typedef struct _SCC {
	int index;
	int api_index;
	int num_bridge_entries;
	DynArray xrefs;		/* these are incoming, not outgoing */
} SCC;

static SgenHashTable hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_DATA, INTERNAL_MEM_BRIDGE_DATA, sizeof (HashEntry), mono_aligned_addr_hash, NULL);

static MonoGCBridgeCallbacks bridge_callbacks;

static int current_time;

void
mono_gc_register_bridge_callbacks (MonoGCBridgeCallbacks *callbacks)
{
	bridge_callbacks = *callbacks;
}

gboolean
mono_sgen_need_bridge_processing (void)
{
	return bridge_callbacks.cross_references != NULL;
}

static HashEntry*
get_hash_entry (MonoObject *obj)
{
	HashEntry *entry = mono_sgen_hash_table_lookup (&hash_table, obj);
	HashEntry new_entry;

	if (entry)
		return entry;

	memset (&new_entry, 0, sizeof (HashEntry));

	new_entry.obj = obj;
	dyn_array_ptr_init (&new_entry.srcs);
	new_entry.finishing_time = -1;
	new_entry.scc_index = -1;

	mono_sgen_hash_table_replace (&hash_table, obj, &new_entry);

	return mono_sgen_hash_table_lookup (&hash_table, obj);
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
		total_srcs += entry->srcs.size;
		if (entry->srcs.size > max_srcs)
			max_srcs = entry->srcs.size;
		dyn_array_uninit (&entry->srcs);
	} SGEN_HASH_TABLE_FOREACH_END;

	mono_sgen_hash_table_clean (&hash_table);

	//g_print ("total srcs %d - max %d\n", total_srcs, max_srcs);
}

static void
register_bridge_object (MonoObject *obj)
{
	HashEntry *entry = get_hash_entry (obj);
	g_assert (!entry->is_bridge);
	entry->is_bridge = TRUE;
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
		return mono_sgen_hash_table_lookup (&hash_table, fwd) == NULL;
	}
	if (!mono_sgen_object_is_live (obj))
		return FALSE;
	return mono_sgen_hash_table_lookup (&hash_table, obj) == NULL;
}

static DynArray dfs_stack;

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		MonoObject *dst = (MonoObject*)*(ptr);			\
		if (dst && !object_is_live (&dst)) {			\
			dyn_array_ptr_push (&dfs_stack, obj_entry);	\
			dyn_array_ptr_push (&dfs_stack, get_hash_entry (dst)); \
		}							\
	} while (0)

static void
dfs1 (HashEntry *obj_entry, HashEntry *src)
{
	g_assert (dfs_stack.size == 0);

	dyn_array_ptr_push (&dfs_stack, src);
	dyn_array_ptr_push (&dfs_stack, obj_entry);

	do {
		MonoObject *obj;
		char *start;

		obj_entry = dyn_array_ptr_pop (&dfs_stack);
		if (obj_entry) {
			src = dyn_array_ptr_pop (&dfs_stack);

			obj = obj_entry->obj;
			start = (char*)obj;

			if (src) {
				//g_print ("link %s -> %s\n", mono_sgen_safe_name (src->obj), mono_sgen_safe_name (obj));
				add_source (obj_entry, src);
			} else {
				//g_print ("starting with %s\n", mono_sgen_safe_name (obj));
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

			//g_print ("finish %s\n", mono_sgen_safe_name (obj_entry->obj));
			register_finishing_time (obj_entry, current_time++);
		}
	} while (dfs_stack.size > 0);
}

static void
scc_add_xref (SCC *src, SCC *dst)
{
	g_assert (src != dst);
	g_assert (src->index != dst->index);

	if (dyn_array_int_contains (&dst->xrefs, src->index))
		return;
	if (src->num_bridge_entries) {
		dyn_array_int_add (&dst->xrefs, src->index);
	} else {
		int i;
		// FIXME: uniq here
		dyn_array_append (&dst->xrefs, &src->xrefs);
		for (i = 0; i < dst->xrefs.size; ++i)
			g_assert (DYN_ARRAY_INT_REF (&dst->xrefs, i) != dst->index);
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

static DynArray sccs;
static SCC *current_scc;

static void
dfs2 (HashEntry *entry)
{
	int i;

	g_assert (dfs_stack.size == 0);

	dyn_array_ptr_push (&dfs_stack, entry);

	do {
		entry = dyn_array_ptr_pop (&dfs_stack);

		if (entry->scc_index >= 0) {
			if (entry->scc_index != current_scc->index)
				scc_add_xref (DYN_ARRAY_REF (&sccs, entry->scc_index), current_scc);
			continue;
		}

		scc_add_entry (current_scc, entry);

		for (i = 0; i < entry->srcs.size; ++i)
			dyn_array_ptr_push (&dfs_stack, DYN_ARRAY_PTR_REF (&entry->srcs, i));
	} while (dfs_stack.size > 0);
}

static int
compare_hash_entries (const void *ep1, const void *ep2)
{
	HashEntry *e1 = *(HashEntry**)ep1;
	HashEntry *e2 = *(HashEntry**)ep2;
	return e2->finishing_time - e1->finishing_time;
}

void
mono_sgen_bridge_processing (int num_objs, MonoObject **objs)
{
	MonoObject *obj;
	HashEntry *entry;
	HashEntry **all_entries;
	int j = 0;
	int num_sccs, num_xrefs;
	int max_entries, max_xrefs;
	int i;
	MonoGCBridgeSCC **api_sccs;
	MonoGCBridgeXRef *api_xrefs;

	g_assert (mono_sgen_need_bridge_processing ());

	//g_print ("%d finalized objects\n", num_objs);

	/* remove objects that are not bridge objects */

	for (i = 0; i < num_objs; ++i) {
		MonoObject *obj = objs [i];
		//g_assert (!mono_sgen_object_is_live (obj));
		if (bridge_callbacks.is_bridge_object (obj)) {
			register_bridge_object (obj);
			objs [j++] = obj;
		}
	}
	num_objs = j;

	//g_print ("%d bridge objects\n", num_objs);

	/* first DFS pass */

	dyn_array_ptr_init (&dfs_stack);

	current_time = 0;
	for (i = 0; i < num_objs; ++i)
		dfs1 (get_hash_entry (objs [i]), NULL);

	//g_print ("%d entries - hash size %d\n", num_hash_entries, hash_size);

	/* alloc and fill array of all entries */

	all_entries = mono_sgen_alloc_internal_dynamic (sizeof (HashEntry*) * hash_table.num_entries, INTERNAL_MEM_BRIDGE_DATA);

	j = 0;
	SGEN_HASH_TABLE_FOREACH (&hash_table, obj, entry) {
		g_assert (entry->finishing_time >= 0);
		all_entries [j++] = entry;
	} SGEN_HASH_TABLE_FOREACH_END;
	g_assert (j == hash_table.num_entries);

	/* sort array according to decreasing finishing time */

	qsort (all_entries, hash_table.num_entries, sizeof (HashEntry*), compare_hash_entries);

	/* second DFS pass */

	dyn_array_init (&sccs, sizeof (SCC));
	for (i = 0; i < hash_table.num_entries; ++i) {
		HashEntry *entry = all_entries [i];
		if (entry->scc_index < 0) {
			int index = sccs.size;
			current_scc = dyn_array_add (&sccs);
			current_scc->index = index;
			current_scc->num_bridge_entries = 0;
			current_scc->api_index = -1;
			dyn_array_int_init (&current_scc->xrefs);

			dfs2 (entry);
		}
	}

	//g_print ("%d sccs\n", sccs.size);

	dyn_array_uninit (&dfs_stack);

	/* init data for callback */

	num_sccs = 0;
	for (i = 0; i < sccs.size; ++i) {
		SCC *scc = DYN_ARRAY_REF (&sccs, i);
		g_assert (scc->index == i);
		if (scc->num_bridge_entries)
			++num_sccs;
	}

	api_sccs = mono_sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeSCC*) * num_sccs, INTERNAL_MEM_BRIDGE_DATA);
	num_xrefs = 0;
	j = 0;
	for (i = 0; i < sccs.size; ++i) {
		SCC *scc = DYN_ARRAY_REF (&sccs, i);
		if (!scc->num_bridge_entries)
			continue;

		api_sccs [j] = mono_sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeSCC) + sizeof (MonoObject*) * scc->num_bridge_entries, INTERNAL_MEM_BRIDGE_DATA);
		api_sccs [j]->num_objs = scc->num_bridge_entries;
		scc->num_bridge_entries = 0;
		scc->api_index = j++;

		num_xrefs += scc->xrefs.size;
	}

	SGEN_HASH_TABLE_FOREACH (&hash_table, obj, entry) {
		if (entry->is_bridge) {
			SCC *scc = DYN_ARRAY_REF (&sccs, entry->scc_index);
			api_sccs [scc->api_index]->objs [scc->num_bridge_entries++] = entry->obj;
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	api_xrefs = mono_sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeXRef) * num_xrefs, INTERNAL_MEM_BRIDGE_DATA);
	j = 0;
	for (i = 0; i < sccs.size; ++i) {
		int k;
		SCC *scc = DYN_ARRAY_REF (&sccs, i);
		if (!scc->num_bridge_entries)
			continue;
		for (k = 0; k < scc->xrefs.size; ++k) {
			SCC *src_scc = DYN_ARRAY_REF (&sccs, DYN_ARRAY_INT_REF (&scc->xrefs, k));
			if (!src_scc->num_bridge_entries)
				continue;
			api_xrefs [j].src_scc_index = src_scc->api_index;
			api_xrefs [j].dst_scc_index = scc->api_index;
			++j;
		}
	}

	/* free data */

	j = 0;
	max_entries = max_xrefs = 0;
	for (i = 0; i < sccs.size; ++i) {
		SCC *scc = DYN_ARRAY_REF (&sccs, i);
		if (scc->num_bridge_entries)
			++j;
		if (scc->num_bridge_entries > max_entries)
			max_entries = scc->num_bridge_entries;
		if (scc->xrefs.size > max_xrefs)
			max_xrefs = scc->xrefs.size;
		dyn_array_uninit (&scc->xrefs);

	}
	dyn_array_uninit (&sccs);

	mono_sgen_free_internal_dynamic (all_entries, sizeof (HashEntry*) * hash_table.num_entries, INTERNAL_MEM_BRIDGE_DATA);

	free_data ();

	//g_print ("%d sccs containing bridges - %d max bridge objects - %d max xrefs\n", j, max_entries, max_xrefs);

	/* callback */

	bridge_callbacks.cross_references (num_sccs, api_sccs, num_xrefs, api_xrefs);

	/* free callback data */

	for (i = 0; i < num_sccs; ++i) {
		mono_sgen_free_internal_dynamic (api_sccs [i],
				sizeof (MonoGCBridgeSCC) + sizeof (MonoObject*) * api_sccs [i]->num_objs,
				INTERNAL_MEM_BRIDGE_DATA);
	}
	mono_sgen_free_internal_dynamic (api_sccs, sizeof (MonoGCBridgeSCC*) * num_sccs, INTERNAL_MEM_BRIDGE_DATA);

	mono_sgen_free_internal_dynamic (api_xrefs, sizeof (MonoGCBridgeXRef) * num_xrefs, INTERNAL_MEM_BRIDGE_DATA);
}

static gboolean
bridge_test_is_bridge_object (MonoObject *obj)
{
	return TRUE;
}

static void
bridge_test_cross_reference (int num_sccs, MonoGCBridgeSCC **sccs, int num_xrefs, MonoGCBridgeXRef *xrefs)
{
	int i;
	for (i = 0; i < num_sccs; ++i) {
		int j;
		g_print ("--- SCC %d\n", i);
		for (j = 0; j < sccs [i]->num_objs; ++j)
			g_print ("  %s\n", mono_sgen_safe_name (sccs [i]->objs [j]));
	}
	for (i = 0; i < num_xrefs; ++i) {
		g_assert (xrefs [i].src_scc_index >= 0 && xrefs [i].src_scc_index < num_sccs);
		g_assert (xrefs [i].dst_scc_index >= 0 && xrefs [i].dst_scc_index < num_sccs);
		g_print ("%d -> %d\n", xrefs [i].src_scc_index, xrefs [i].dst_scc_index);
	}
}


void
mono_sgen_register_test_bridge_callbacks (void)
{
	MonoGCBridgeCallbacks callbacks;
	callbacks.is_bridge_object = bridge_test_is_bridge_object;
	callbacks.cross_references = bridge_test_cross_reference;
	mono_gc_register_bridge_callbacks (&callbacks);
}

#endif
