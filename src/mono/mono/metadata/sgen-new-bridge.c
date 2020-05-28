/**
 * \file
 * Simple generational GC.
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#if defined (HAVE_SGEN_GC) && !defined (DISABLE_SGEN_GC_BRIDGE)

#include <stdlib.h>
#include <errno.h>

#include "sgen/sgen-gc.h"
#include "sgen-bridge-internals.h"
#include "sgen/sgen-hash-table.h"
#include "sgen/sgen-qsort.h"
#include "sgen/sgen-client.h"
#include "tabledefs.h"
#include "utils/mono-logger-internals.h"

#define OPTIMIZATION_COPY
#define OPTIMIZATION_FORWARD
#define OPTIMIZATION_SINGLETON_DYN_ARRAY
#include "sgen-dynarray.h"

//#define NEW_XREFS
#ifdef NEW_XREFS
//#define TEST_NEW_XREFS
#endif

#if !defined(NEW_XREFS) || defined(TEST_NEW_XREFS)
#define OLD_XREFS
#endif

#ifdef NEW_XREFS
#define XREFS new_xrefs
#else
#define XREFS old_xrefs
#endif

/*
 * Bridge data for a single managed object
 *
 * FIXME: Optimizations:
 *
 * Don't allocate a srcs array for just one source.  Most objects have
 * just one source, so use the srcs pointer itself.
 */
typedef struct _HashEntry {
	gboolean is_bridge;

	union {
		struct {
			guint32 is_visited : 1;
			guint32 finishing_time : 31;
			struct _HashEntry *forwarded_to;
		} dfs1;
		struct {
			// Index in sccs array of SCC this object was folded into
			int scc_index;
		} dfs2;
	} v;

	// "Source" managed objects pointing at this destination
	DynPtrArray srcs;
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

	gboolean flag;

	/*
	 * Index in global sccs array of SCCs holding pointers to this SCC
	 *
	 * New and old xrefs are typically mutually exclusive.  Only when TEST_NEW_XREFS is
	 * enabled we do both, and compare the results.  This should only be done for
	 * debugging, obviously.
	 */
#ifdef OLD_XREFS
	DynIntArray old_xrefs;		/* these are incoming, not outgoing */
#endif
#ifdef NEW_XREFS
	DynIntArray new_xrefs;
#endif
} SCC;

static char *dump_prefix = NULL;

// Maps managed objects to corresponding HashEntry stricts
static SgenHashTable hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_HASH_TABLE, INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY, sizeof (HashEntry), mono_aligned_addr_hash, NULL);

static guint32 current_time;

static gboolean bridge_accounting_enabled = FALSE;

static SgenBridgeProcessor *bridge_processor;

/* Core functions */

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

#ifdef NEW_XREFS
static gboolean
dyn_array_int_contains (DynIntArray *da, int x)
{
	int i;
	for (i = 0; i < dyn_array_int_size (da); ++i)
		if (dyn_array_int_get (da, i) == x)
			return TRUE;
	return FALSE;
}
#endif

static void
set_config (const SgenBridgeProcessorConfig *config)
{
	if (config->accounting) {
		SgenHashTable table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_HASH_TABLE, INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY, sizeof (HashEntryWithAccounting), mono_aligned_addr_hash, NULL);
		bridge_accounting_enabled = TRUE;
		hash_table = table;
	}
	if (config->dump_prefix) {
		dump_prefix = strdup (config->dump_prefix);
	}
}

static MonoGCBridgeObjectKind
class_kind (MonoClass *klass)
{
	MonoGCBridgeObjectKind res = mono_bridge_callbacks.bridge_class_kind (klass);

	/* If it's a bridge, nothing we can do about it. */
	if (res == GC_BRIDGE_TRANSPARENT_BRIDGE_CLASS || res == GC_BRIDGE_OPAQUE_BRIDGE_CLASS)
		return res;

	/* Non bridge classes with no pointers will never point to a bridge, so we can savely ignore them. */
	if (!m_class_has_references (klass)) {
		SGEN_LOG (6, "class %s is opaque\n", m_class_get_name (klass));
		return GC_BRIDGE_OPAQUE_CLASS;
	}

	/* Some arrays can be ignored */
	if (m_class_get_rank (klass) == 1) {
		MonoClass *elem_class = m_class_get_element_class (klass);

		/* FIXME the bridge check can be quite expensive, cache it at the class level. */
		/* An array of a sealed type that is not a bridge will never get to a bridge */
		if ((mono_class_get_flags (elem_class) & TYPE_ATTRIBUTE_SEALED) && !m_class_has_references (elem_class) && !mono_bridge_callbacks.bridge_class_kind (elem_class)) {
			SGEN_LOG (6, "class %s is opaque\n", m_class_get_name (klass));
			return GC_BRIDGE_OPAQUE_CLASS;
		}
	}

	return GC_BRIDGE_TRANSPARENT_CLASS;
}

static HashEntry*
get_hash_entry (MonoObject *obj, gboolean *existing)
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

	dyn_array_ptr_init (&new_entry.srcs);
	new_entry.v.dfs1.finishing_time = 0;

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
	MonoObject *obj G_GNUC_UNUSED;
	HashEntry *entry;
	int total_srcs = 0;
	int max_srcs = 0;

	SGEN_HASH_TABLE_FOREACH (&hash_table, MonoObject *, obj, HashEntry *, entry) {
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
register_finishing_time (HashEntry *entry, guint32 t)
{
	g_assert (entry->v.dfs1.finishing_time == 0);
	/* finishing_time has 31 bits, so it must be within signed int32 range. */
	g_assert (t > 0 && t <= G_MAXINT32);
	entry->v.dfs1.finishing_time = t;
}

static int ignored_objects;

static gboolean
is_opaque_object (MonoObject *obj)
{
	if ((obj->vtable->gc_bits & SGEN_GC_BIT_BRIDGE_OPAQUE_OBJECT) == SGEN_GC_BIT_BRIDGE_OPAQUE_OBJECT) {
		SGEN_LOG (6, "ignoring %s\n", m_class_get_name (mono_object_class (obj)));
		++ignored_objects;
		return TRUE;
	}
	return FALSE;
}

static gboolean
object_needs_expansion (MonoObject **objp)
{
	MonoObject *obj = *objp;
	MonoObject *fwd = SGEN_OBJECT_IS_FORWARDED (obj);
	if (fwd) {
		*objp = fwd;
		if (is_opaque_object (fwd))
			return FALSE;
		return sgen_hash_table_lookup (&hash_table, fwd) != NULL;
	}
	if (is_opaque_object (obj))
		return FALSE;
	if (!sgen_object_is_live (obj))
		return TRUE;
	return sgen_hash_table_lookup (&hash_table, obj) != NULL;
}

static HashEntry*
follow_forward (HashEntry *entry)
{
#ifdef OPTIMIZATION_FORWARD
	while (entry->v.dfs1.forwarded_to) {
		HashEntry *next = entry->v.dfs1.forwarded_to;
		if (next->v.dfs1.forwarded_to)
			entry->v.dfs1.forwarded_to = next->v.dfs1.forwarded_to;
		entry = next;
	}
#else
	g_assert (!entry->v.dfs1.forwarded_to);
#endif
	return entry;
}

static DynPtrArray registered_bridges;
static DynPtrArray dfs_stack;

static int dfs1_passes, dfs2_passes;

/*
 * DFS1 maintains a stack, where each two entries are effectively one entry.  (FIXME:
 * Optimize this via pointer tagging.)  There are two different types of entries:
 *
 * entry, src: entry needs to be expanded via scanning, and linked to from src
 * NULL, entry: entry has already been expanded and needs to be finished
 */

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		GCObject *dst = (GCObject*)*(ptr);			\
		if (dst && object_needs_expansion (&dst)) {			\
			++num_links;					\
			dyn_array_ptr_push (&dfs_stack, obj_entry);	\
			dyn_array_ptr_push (&dfs_stack, follow_forward (get_hash_entry (dst, NULL))); \
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
		++dfs1_passes;

		obj_entry = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);
		if (obj_entry) {
			/* obj_entry needs to be expanded */
			src = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);

			if (src)
				g_assert (!src->v.dfs1.forwarded_to);

			obj_entry = follow_forward (obj_entry);

		again:
			g_assert (!obj_entry->v.dfs1.forwarded_to);
			obj = sgen_hash_table_key_for_value_pointer (obj_entry);
			start = (char*)obj;

			if (!obj_entry->v.dfs1.is_visited) {
				int num_links = 0;
				mword desc = sgen_obj_get_descriptor_safe (obj);

				obj_entry->v.dfs1.is_visited = 1;

				/* push the finishing entry on the stack */
				dyn_array_ptr_push (&dfs_stack, obj_entry);
				dyn_array_ptr_push (&dfs_stack, NULL);

#include "sgen/sgen-scan-object.h"

				/*
				 * We can remove non-bridge objects with a single outgoing
				 * link by forwarding links going to it.
				 *
				 * This is the first time we've encountered this object, so
				 * no links to it have yet been added.  We'll keep it that
				 * way by setting the forward pointer, and instead of
				 * continuing processing this object, we start over with the
				 * object it points to.
				 */
#ifdef OPTIMIZATION_FORWARD
				if (!obj_entry->is_bridge && num_links == 1) {
					HashEntry *dst_entry = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);
					HashEntry *obj_entry_again = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);
					g_assert (obj_entry_again == obj_entry);
					g_assert (!dst_entry->v.dfs1.forwarded_to);
					if (obj_entry != dst_entry) {
						obj_entry->v.dfs1.forwarded_to = dst_entry;
						obj_entry = dst_entry;
					}
					goto again;
				}
#endif
			}

			if (src) {
				//g_print ("link %s -> %s\n", sgen_safe_name (src->obj), sgen_safe_name (obj));
				g_assert (!obj_entry->v.dfs1.forwarded_to);
				add_source (obj_entry, src);
			} else {
				//g_print ("starting with %s\n", sgen_safe_name (obj));
			}
		} else {
			/* obj_entry needs to be finished */

			obj_entry = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);

			//g_print ("finish %s\n", sgen_safe_name (obj_entry->obj));
			register_finishing_time (obj_entry, ++current_time);
		}
	} while (dyn_array_ptr_size (&dfs_stack) > 0);
}

static DynSCCArray sccs;
static SCC *current_scc;

/*
 * At the end of bridge processing we need to end up with an (acyclyc) graph of bridge
 * object SCCs, where the links between the nodes (each one an SCC) in that graph represent
 * the presence of a direct or indirect link between those SCCs.  An example:
 *
 *                       D
 *                       |
 *                       v
 *        A -> B -> c -> e -> F
 *
 * A, B, D and F are SCCs that contain bridge objects, c and e don't contain bridge objects.
 * The graph we need to produce from this is:
 *
 *                  D
 *                  |
 *                  v
 *        A -> B -> F
 *
 * Note that we don't need to produce an edge from A to F.  It's sufficient that F is
 * indirectly reachable from A.
 *
 * The old algorithm would create a set, for each SCC, of bridge SCCs that can reach it,
 * directly or indirectly, by merging the ones sets for those that reach it directly.  The
 * sets it would build up are these:
 *
 *   A: {}
 *   B: {A}
 *   c: {B}
 *   D: {}
 *   e: {B,D}
 *   F: {B,D}
 *
 * The merge operations on these sets turned out to be huge time sinks.
 *
 * The new algorithm proceeds in two passes: During DFS2, it only builds up the sets of SCCs
 * that directly point to each SCC:
 *
 *   A: {}
 *   B: {A}
 *   c: {B}
 *   D: {}
 *   e: {c,D}
 *   F: {e}
 *
 * This is the adjacency list for the SCC graph, in other words.  In a separate step
 * afterwards, it does a depth-first traversal of that graph, for each bridge node, to get
 * to the final list.  It uses a flag to avoid traversing any node twice.
 */
static void
scc_add_xref (SCC *src, SCC *dst)
{
	g_assert (src != dst);
	g_assert (src->index != dst->index);

#ifdef NEW_XREFS
	/*
	 * FIXME: Right now we don't even unique the direct ancestors, but just add to the
	 * list.  Doing a containment check slows this algorithm down to almost the speed of
	 * the old one.  Use the flag instead!
	 */
	dyn_array_int_add (&dst->new_xrefs, src->index);
#endif

#ifdef OLD_XREFS
	if (dyn_array_int_is_copy (&dst->old_xrefs)) {
		int i;
		dyn_array_int_ensure_independent (&dst->old_xrefs);
		for (i = 0; i < dyn_array_int_size (&dst->old_xrefs); ++i) {
			int j = dyn_array_int_get (&dst->old_xrefs, i);
			SCC *bridge_scc = dyn_array_scc_get_ptr (&sccs, j);
			g_assert (!bridge_scc->flag);
			bridge_scc->flag = TRUE;
		}
	}

	if (src->num_bridge_entries) {
		if (src->flag)
			return;
		src->flag = TRUE;
		dyn_array_int_add (&dst->old_xrefs, src->index);
#ifdef OPTIMIZATION_COPY
	} else if (dyn_array_int_size (&dst->old_xrefs) == 0) {
		dyn_array_int_copy (&dst->old_xrefs, &src->old_xrefs);
#endif
	} else {
		int i;
		for (i = 0; i < dyn_array_int_size (&src->old_xrefs); ++i) {
			int j = dyn_array_int_get (&src->old_xrefs, i);
			SCC *bridge_scc = dyn_array_scc_get_ptr (&sccs, j);
			g_assert (bridge_scc->num_bridge_entries);
			if (!bridge_scc->flag) {
				bridge_scc->flag = TRUE;
				dyn_array_int_add (&dst->old_xrefs, j);
			}
		}
	}
#endif
}

static void
scc_add_entry (SCC *scc, HashEntry *entry)
{
	g_assert (entry->v.dfs2.scc_index < 0);
	entry->v.dfs2.scc_index = scc->index;
	if (entry->is_bridge)
		++scc->num_bridge_entries;
}

static void
dfs2 (HashEntry *entry)
{
	int i;

	g_assert (dyn_array_ptr_size (&dfs_stack) == 0);

	dyn_array_ptr_push (&dfs_stack, entry);

	do {
		entry = (HashEntry *)dyn_array_ptr_pop (&dfs_stack);
		++dfs2_passes;

		if (entry->v.dfs2.scc_index >= 0) {
			if (entry->v.dfs2.scc_index != current_scc->index)
				scc_add_xref (dyn_array_scc_get_ptr (&sccs, entry->v.dfs2.scc_index), current_scc);
			continue;
		}

		scc_add_entry (current_scc, entry);

		for (i = 0; i < dyn_array_ptr_size (&entry->srcs); ++i)
			dyn_array_ptr_push (&dfs_stack, dyn_array_ptr_get (&entry->srcs, i));
	} while (dyn_array_ptr_size (&dfs_stack) > 0);

#ifdef OLD_XREFS
	/* If xrefs is a copy then we haven't set a single flag. */
	if (dyn_array_int_is_copy (&current_scc->old_xrefs))
		return;
	for (i = 0; i < dyn_array_int_size (&current_scc->old_xrefs); ++i) {
		int j = dyn_array_int_get (&current_scc->old_xrefs, i);
		SCC *bridge_scc = dyn_array_scc_get_ptr (&sccs, j);
		g_assert (bridge_scc->flag);
		bridge_scc->flag = FALSE;
	}
#endif
}

#ifdef NEW_XREFS
static void
gather_xrefs (SCC *scc)
{
	int i;
	for (i = 0; i < dyn_array_int_size (&scc->new_xrefs); ++i) {
		int index = dyn_array_int_get (&scc->new_xrefs, i);
		SCC *src = dyn_array_scc_get_ptr (&sccs, index);
		if (src->flag)
			continue;
		src->flag = TRUE;
		if (src->num_bridge_entries)
			dyn_array_int_add (&merge_array, index);
		else
			gather_xrefs (src);
	}
}

static void
reset_flags (SCC *scc)
{
	int i;
	for (i = 0; i < dyn_array_int_size (&scc->new_xrefs); ++i) {
		int index = dyn_array_int_get (&scc->new_xrefs, i);
		SCC *src = dyn_array_scc_get_ptr (&sccs, index);
		if (!src->flag)
			continue;
		src->flag = FALSE;
		if (!src->num_bridge_entries)
			reset_flags (src);
	}
}
#endif

static void
dump_graph (void)
{
	static int counter = 0;

	MonoObject *obj;
	HashEntry *entry;
	size_t prefix_len = strlen (dump_prefix);
	char *filename = g_newa (char, prefix_len + 64);
	FILE *file;
	int edge_id = 0;

	sprintf (filename, "%s.%d.gexf", dump_prefix, counter++);
	file = fopen (filename, "w");

	if (file == NULL) {
		fprintf (stderr, "Warning: Could not open bridge dump file `%s` for writing: %s\n", filename, strerror (errno));
		return;
	}

	fprintf (file, "<gexf xmlns=\"http://www.gexf.net/1.2draft\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.gexf.net/1.2draft http://www.gexf.net/1.2draft/gexf.xsd\" version=\"1.2\">\n");

	fprintf (file, "<graph defaultedgetype=\"directed\">\n"
			"<attributes class=\"node\">\n"
			"<attribute id=\"0\" title=\"class\" type=\"string\"/>\n"
			"<attribute id=\"1\" title=\"bridge\" type=\"boolean\"/>\n"
			"</attributes>\n");

	fprintf (file, "<nodes>\n");
	SGEN_HASH_TABLE_FOREACH (&hash_table, MonoObject *, obj, HashEntry *, entry) {
		MonoVTable *vt = SGEN_LOAD_VTABLE (obj);
		fprintf (file, "<node id=\"%p\"><attvalues><attvalue for=\"0\" value=\"%s.%s\"/><attvalue for=\"1\" value=\"%s\"/></attvalues></node>\n",
			 obj, m_class_get_name_space (vt->klass), m_class_get_name (vt->klass), entry->is_bridge ? "true" : "false");
	} SGEN_HASH_TABLE_FOREACH_END;
	fprintf (file, "</nodes>\n");

	fprintf (file, "<edges>\n");
	SGEN_HASH_TABLE_FOREACH (&hash_table, MonoObject *, obj, HashEntry *, entry) {
		int i;
		for (i = 0; i < dyn_array_ptr_size (&entry->srcs); ++i) {
			HashEntry *src = (HashEntry *)dyn_array_ptr_get (&entry->srcs, i);
			fprintf (file, "<edge id=\"%d\" source=\"%p\" target=\"%p\"/>\n", edge_id++, sgen_hash_table_key_for_value_pointer (src), obj);
		}
	} SGEN_HASH_TABLE_FOREACH_END;
	fprintf (file, "</edges>\n");

	fprintf (file, "</graph></gexf>\n");

	fclose (file);
}

static int
compare_hash_entries (const HashEntry *e1, const HashEntry *e2)
{
	/* We can cast to signed int here because finishing_time has only 31 bits. */
	return (gint32)e2->v.dfs1.finishing_time - (gint32)e1->v.dfs1.finishing_time;
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
	dyn_array_ptr_empty (&registered_bridges);
}

static void
processing_stw_step (void)
{
	int i;
	int bridge_count;
	MonoObject *obj G_GNUC_UNUSED;
	HashEntry *entry;
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
		register_bridge_object ((MonoObject *)dyn_array_ptr_get (&registered_bridges, i));

	for (i = 0; i < bridge_count; ++i)
		dfs1 (get_hash_entry ((MonoObject *)dyn_array_ptr_get (&registered_bridges, i), NULL));

	/* Remove all forwarded objects. */
	SGEN_HASH_TABLE_FOREACH (&hash_table, MonoObject *, obj, HashEntry *, entry) {
		if (entry->v.dfs1.forwarded_to) {
			g_assert (dyn_array_ptr_size (&entry->srcs) == 0);
			SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
			continue;
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	SGEN_TV_GETTIME (atv);
	step_2 = SGEN_TV_ELAPSED (btv, atv);

	if (dump_prefix)
		dump_graph ();
}

static int num_registered_bridges, hash_table_size;

static void
processing_build_callback_data (int generation)
{
	int i, j;
	int num_sccs, num_xrefs;
	int max_entries, max_xrefs;
	MonoObject *obj G_GNUC_UNUSED;
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
	SGEN_HASH_TABLE_FOREACH (&hash_table, MonoObject *, obj, HashEntry *, entry) {
		g_assert (entry->v.dfs1.finishing_time > 0);
		all_entries [j++] = entry;
		fist_pass_links += dyn_array_ptr_size (&entry->srcs);
	} SGEN_HASH_TABLE_FOREACH_END;
	g_assert (j == hash_table.num_entries);
	hash_table_size = hash_table.num_entries;

	/* sort array according to decreasing finishing time */
	qsort_hash_entries (all_entries, hash_table.num_entries);

	SGEN_HASH_TABLE_FOREACH (&hash_table, MonoObject *, obj, HashEntry *, entry) {
		entry->v.dfs2.scc_index = -1;
	} SGEN_HASH_TABLE_FOREACH_END;

	SGEN_TV_GETTIME (btv);
	step_3 = SGEN_TV_ELAPSED (atv, btv);

	/* second DFS pass */

	dyn_array_scc_init (&sccs);
	for (i = 0; i < hash_table.num_entries; ++i) {
		HashEntry *entry = all_entries [i];
		if (entry->v.dfs2.scc_index < 0) {
			int index = dyn_array_scc_size (&sccs);
			current_scc = dyn_array_scc_add (&sccs);
			current_scc->index = index;
			current_scc->num_bridge_entries = 0;
#ifdef NEW_XREFS
			current_scc->flag = FALSE;
			dyn_array_int_init (&current_scc->new_xrefs);
#endif
#ifdef OLD_XREFS
			dyn_array_int_init (&current_scc->old_xrefs);
#endif
			current_scc->api_index = -1;

			dfs2 (entry);

#ifdef NEW_XREFS
			/*
			 * If a node has only one incoming edge, we just copy the source's
			 * xrefs array, effectively removing the source from the graph.
			 * This takes care of long linked lists.
			 */
			if (!current_scc->num_bridge_entries && dyn_array_int_size (&current_scc->new_xrefs) == 1) {
				SCC *src;
				j = dyn_array_int_get (&current_scc->new_xrefs, 0);
				src = dyn_array_scc_get_ptr (&sccs, j);
				if (src->num_bridge_entries)
					dyn_array_int_set (&current_scc->new_xrefs, 0, j);
				else
					dyn_array_int_copy (&current_scc->new_xrefs, &src->new_xrefs);
			}
#endif
		}
	}

#ifdef NEW_XREFS
#ifdef TEST_NEW_XREFS
	for (j = 0; j < dyn_array_scc_size (&sccs); ++j) {
		SCC *scc = dyn_array_scc_get_ptr (&sccs, j);
		g_assert (!scc->flag);
	}
#endif

	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		g_assert (scc->index == i);
		if (!scc->num_bridge_entries)
			continue;

		dyn_array_int_empty (&merge_array);
		gather_xrefs (scc);
		reset_flags (scc);
		dyn_array_int_copy (&scc->new_xrefs, &merge_array);
		dyn_array_int_ensure_independent (&scc->new_xrefs);

#ifdef TEST_NEW_XREFS
		for (j = 0; j < dyn_array_scc_size (&sccs); ++j) {
			SCC *scc = dyn_array_scc_get_ptr (&sccs, j);
			g_assert (!scc->flag);
		}
#endif
	}

#ifdef TEST_NEW_XREFS
	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		g_assert (scc->index == i);
		if (!scc->num_bridge_entries)
			continue;

		g_assert (dyn_array_int_size (&scc->new_xrefs) == dyn_array_int_size (&scc->old_xrefs));
		for (j = 0; j < dyn_array_int_size (&scc->new_xrefs); ++j)
			g_assert (dyn_array_int_contains (&scc->old_xrefs, dyn_array_int_get (&scc->new_xrefs, j)));
	}
#endif
#endif

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
			HashEntryWithAccounting *entry = (HashEntryWithAccounting*)all_entries [i];

			entry->weight += (double)sgen_safe_object_get_size (sgen_hash_table_key_for_value_pointer (entry));
			w = entry->weight / dyn_array_ptr_size (&entry->entry.srcs);
			for (j = 0; j < dyn_array_ptr_size (&entry->entry.srcs); ++j) {
				HashEntryWithAccounting *other = (HashEntryWithAccounting *)dyn_array_ptr_get (&entry->entry.srcs, j);
				other->weight += w;
			}
		}
		for (i = 0; i < hash_table.num_entries; ++i) {
			HashEntryWithAccounting *entry = (HashEntryWithAccounting*)all_entries [i];
			if (entry->entry.is_bridge) {
				MonoObject *obj = sgen_hash_table_key_for_value_pointer (entry);
				MonoClass *klass = SGEN_LOAD_VTABLE (obj)->klass;
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_GC, "OBJECT %s::%s (%p) weight %f", m_class_get_name_space (klass), m_class_get_name (klass), obj, entry->weight);
			}
		}
	}

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
		sccs_links += dyn_array_int_size (&scc->XREFS);
		max_sccs_links = MAX (max_sccs_links, dyn_array_int_size (&scc->XREFS));
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

		num_xrefs += dyn_array_int_size (&scc->XREFS);
	}

	SGEN_HASH_TABLE_FOREACH (&hash_table, MonoObject *, obj, HashEntry *, entry) {
		if (entry->is_bridge) {
			SCC *scc = dyn_array_scc_get_ptr (&sccs, entry->v.dfs2.scc_index);
			api_sccs [scc->api_index]->objs [scc->num_bridge_entries++] = sgen_hash_table_key_for_value_pointer (entry);
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	api_xrefs = (MonoGCBridgeXRef *)sgen_alloc_internal_dynamic (sizeof (MonoGCBridgeXRef) * num_xrefs, INTERNAL_MEM_BRIDGE_DATA, TRUE);
	j = 0;
	for (i = 0; i < dyn_array_scc_size (&sccs); ++i) {
		int k;
		SCC *scc = dyn_array_scc_get_ptr (&sccs, i);
		if (!scc->num_bridge_entries)
			continue;
		for (k = 0; k < dyn_array_int_size (&scc->XREFS); ++k) {
			SCC *src_scc = dyn_array_scc_get_ptr (&sccs, dyn_array_int_get (&scc->XREFS, k));
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
		if (dyn_array_int_size (&scc->XREFS) > max_xrefs)
			max_xrefs = dyn_array_int_size (&scc->XREFS);
#ifdef NEW_XREFS
		dyn_array_int_uninit (&scc->new_xrefs);
#endif
#ifdef OLD_XREFS
		dyn_array_int_uninit (&scc->old_xrefs);
#endif

	}
	dyn_array_scc_uninit (&sccs);

	sgen_free_internal_dynamic (all_entries, sizeof (HashEntry*) * hash_table.num_entries, INTERNAL_MEM_BRIDGE_DATA);

	free_data ();
	/* Empty the registered bridges array */
	num_registered_bridges = dyn_array_ptr_size (&registered_bridges);
	dyn_array_ptr_empty (&registered_bridges);

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

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_GC, "GC_NEW_BRIDGE num-objects %d num_hash_entries %d sccs size %d init %.2fms df1 %.2fms sort %.2fms dfs2 %.2fms setup-cb %.2fms free-data %.2fms links %d/%d/%d/%d dfs passes %d/%d ignored %d",
		num_registered_bridges, hash_table_size, dyn_array_scc_size (&sccs),
		step_1 / 10000.0f,
		step_2 / 10000.0f,
		step_3 / 10000.0f,
		step_4 / 10000.0f,
		step_5 / 10000.0f,
		step_6 / 10000.0f,
		fist_pass_links, second_pass_links, sccs_links, max_sccs_links,
		dfs1_passes, dfs2_passes, ignored_objects);

	step_1 = 0; /* We must cleanup since this value is used as an accumulator. */
	fist_pass_links = second_pass_links = sccs_links = max_sccs_links = 0;
	dfs1_passes = dfs2_passes = ignored_objects = 0;
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
	printf ("  is visited: %d\n", (int)entry->v.dfs1.is_visited);
}

void
sgen_new_bridge_init (SgenBridgeProcessor *collector)
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

MONO_EMPTY_SOURCE_FILE (sgen_new_bridge);

#endif
