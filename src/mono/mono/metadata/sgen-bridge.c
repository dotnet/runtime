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

#ifdef HAVE_SGEN_GC

#include <stdlib.h>

#include "sgen/sgen-gc.h"
#include "sgen-bridge-internals.h"
#include "sgen/sgen-hash-table.h"
#include "sgen/sgen-qsort.h"
#include "utils/mono-logger-internals.h"

#ifndef DISABLE_SGEN_GC_BRIDGE

typedef enum {
	BRIDGE_PROCESSOR_INVALID,
	BRIDGE_PROCESSOR_NEW,
	BRIDGE_PROCESSOR_TARJAN,
	BRIDGE_PROCESSOR_DEFAULT = BRIDGE_PROCESSOR_TARJAN
} BridgeProcessorSelection;

// Bridge processor type pending / in use
static BridgeProcessorSelection bridge_processor_selection = BRIDGE_PROCESSOR_DEFAULT;
// Most recently requested callbacks
static MonoGCBridgeCallbacks pending_bridge_callbacks;
// Configuration to be passed to bridge processor after init
static SgenBridgeProcessorConfig bridge_processor_config;
// Currently-in-use callbacks
MonoGCBridgeCallbacks mono_bridge_callbacks;

// Bridge processor state
static SgenBridgeProcessor bridge_processor;
// This is used for a special debug feature
static SgenBridgeProcessor compare_to_bridge_processor;

volatile gboolean mono_bridge_processing_in_progress = FALSE;

// FIXME: The current usage pattern for this function is unsafe. Bridge processing could start immediately after unlock
/**
 * mono_gc_wait_for_bridge_processing:
 */
void
mono_gc_wait_for_bridge_processing (void)
{
	MONO_ENTER_GC_UNSAFE;
	mono_gc_wait_for_bridge_processing_internal ();
	MONO_EXIT_GC_UNSAFE;
}

void
mono_gc_wait_for_bridge_processing_internal (void)
{
	if (!mono_bridge_processing_in_progress)
		return;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_GC, "GC_BRIDGE waiting for bridge processing to finish");

	sgen_gc_lock ();
	sgen_gc_unlock ();
}

/**
 * mono_gc_register_bridge_callbacks:
 */
void
mono_gc_register_bridge_callbacks (MonoGCBridgeCallbacks *callbacks)
{
	if (callbacks->bridge_version != SGEN_BRIDGE_VERSION)
		g_error ("Invalid bridge callback version. Expected %d but got %d\n", SGEN_BRIDGE_VERSION, callbacks->bridge_version);

	// Defer assigning to bridge_callbacks until we have the gc lock.
	// Note: This line is unsafe if we are on a separate thread from the one the runtime was initialized on.
	pending_bridge_callbacks = *callbacks;

	// If sgen has started, will assign bridge callbacks and init bridge
	sgen_init_bridge ();
}

static BridgeProcessorSelection
bridge_processor_name (const char *name)
{
	if (!strcmp ("old", name)) {
		g_warning ("The 'old' bridge processor implementation is no longer supported, falling back to the 'new' bridge.");
		return BRIDGE_PROCESSOR_NEW;
	} else if (!strcmp ("new", name)) {
		return BRIDGE_PROCESSOR_NEW;
	} else if (!strcmp ("tarjan", name)) {
		return BRIDGE_PROCESSOR_TARJAN;
	} else {
		return BRIDGE_PROCESSOR_INVALID;
	}
}

static gboolean
bridge_processor_started (void)
{
	return bridge_processor.reset_data != NULL;
}

// Initialize a single bridge processor
static void
init_bridge_processor (SgenBridgeProcessor *processor, BridgeProcessorSelection selection)
{
	memset (processor, 0, sizeof (SgenBridgeProcessor));

	switch (selection) {
		case BRIDGE_PROCESSOR_NEW:
			sgen_new_bridge_init (processor);
			break;
		case BRIDGE_PROCESSOR_TARJAN:
			sgen_tarjan_bridge_init (processor);
			break;
		default:
			g_assert_not_reached ();
	}
}

/*
 * Initializing the sgen bridge consists of setting the bridge callbacks,
 * and initializing the bridge processor. Init should follow these rules:
 *
 *   - Init happens only after sgen is initialized (because we don't
 *     know which bridge processor to initialize until then, and also
 *     to allow bridge processor init to interact with sgen if it wants)
 *
 *   - Init happens only after mono_gc_register_bridge_callbacks is called
 *
 *   - Init should not happen concurrently with a GC (because a GC will
 *     call sgen_need_bridge_processing at various times)
 *
 *   - Initializing the bridge processor should happen only once
 *
 * We call sgen_init_bridge when the callbacks are set, and also when sgen
 * is done initing. Actual initialization then only occurs if it is ready.
 */
void
sgen_init_bridge (void)
{
	if (sgen_gc_initialized ()) {
		// This lock is not initialized until the GC is
		sgen_gc_lock ();

		mono_bridge_callbacks = pending_bridge_callbacks;

		// If a bridge was registered but there is no bridge processor yet
		if (mono_bridge_callbacks.cross_references && !bridge_processor_started ()) {
			init_bridge_processor (&bridge_processor, bridge_processor_selection);

			if (bridge_processor.set_config)
				bridge_processor.set_config (&bridge_processor_config);

			// Config is no longer needed so free its memory
			free (bridge_processor_config.dump_prefix);
			bridge_processor_config.dump_prefix = NULL;
		}

		sgen_gc_unlock ();
	}
}

void
sgen_set_bridge_implementation (const char *name)
{
	BridgeProcessorSelection selection = bridge_processor_name (name);

	if (selection == BRIDGE_PROCESSOR_INVALID)
		g_warning ("Invalid value for bridge processor implementation, valid values are: 'new' or 'tarjan'.");
	else if (bridge_processor_started ())
		g_warning ("Cannot set bridge processor implementation once bridge has already started");
	else
		bridge_processor_selection = selection;
}

gboolean
sgen_is_bridge_object (GCObject *obj)
{
	if ((obj->vtable->gc_bits & SGEN_GC_BIT_BRIDGE_OBJECT) != SGEN_GC_BIT_BRIDGE_OBJECT)
		return FALSE;
	return mono_bridge_callbacks.is_bridge_object (obj);
}

gboolean
sgen_need_bridge_processing (void)
{
	return mono_bridge_callbacks.cross_references != NULL;
}

static gboolean
compare_bridge_processors (void)
{
	return compare_to_bridge_processor.reset_data != NULL;
}

/* Dispatch wrappers */
void
sgen_bridge_reset_data (void)
{
	bridge_processor.reset_data ();
	if (compare_bridge_processors ())
		compare_to_bridge_processor.reset_data ();
}

void
sgen_bridge_processing_stw_step (void)
{
	/*
	 * mono_bridge_processing_in_progress must be set with the world
	 * stopped.  If not there would be race conditions.
	 */
	mono_bridge_processing_in_progress = TRUE;

	bridge_processor.processing_stw_step ();
	if (compare_bridge_processors ())
		compare_to_bridge_processor.processing_stw_step ();
}

static gboolean
is_bridge_object_dead (GCObject *obj, void *data)
{
	SgenHashTable *table = (SgenHashTable *)data;
	unsigned char *value = (unsigned char *)sgen_hash_table_lookup (table, obj);
	if (!value)
		return FALSE;
	return !*value;
}

static void
null_weak_links_to_dead_objects (SgenBridgeProcessor *processor, int generation)
{
	int i, j;
	int num_sccs = processor->num_sccs;
	MonoGCBridgeSCC **api_sccs = processor->api_sccs;
	SgenHashTable alive_hash = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE, INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE_ENTRY, 1, mono_aligned_addr_hash, NULL);

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
	sgen_null_links_if (is_bridge_object_dead, &alive_hash, GENERATION_NURSERY, FALSE);
	sgen_null_links_if (is_bridge_object_dead, &alive_hash, GENERATION_NURSERY, TRUE);
	if (generation == GENERATION_OLD) {
		sgen_null_links_if (is_bridge_object_dead, &alive_hash, GENERATION_OLD, FALSE);
		sgen_null_links_if (is_bridge_object_dead, &alive_hash, GENERATION_OLD, TRUE);
	}

	sgen_hash_table_clean (&alive_hash);
}

static void
free_callback_data (SgenBridgeProcessor *processor)
{
	int i;
	int num_sccs = processor->num_sccs;
	int num_xrefs = processor->num_xrefs;
	MonoGCBridgeSCC **api_sccs = processor->api_sccs;
	MonoGCBridgeXRef *api_xrefs = processor->api_xrefs;

	for (i = 0; i < num_sccs; ++i) {
		sgen_free_internal_dynamic (api_sccs [i],
				sizeof (MonoGCBridgeSCC) + sizeof (MonoObject*) * api_sccs [i]->num_objs,
				INTERNAL_MEM_BRIDGE_DATA);
	}
	sgen_free_internal_dynamic (api_sccs, sizeof (MonoGCBridgeSCC*) * num_sccs, INTERNAL_MEM_BRIDGE_DATA);

	sgen_free_internal_dynamic (api_xrefs, sizeof (MonoGCBridgeXRef) * num_xrefs, INTERNAL_MEM_BRIDGE_DATA);

	processor->num_sccs = 0;
	processor->api_sccs = NULL;
	processor->num_xrefs = 0;
	processor->api_xrefs = NULL;
}

static int
compare_xrefs (const void *a_ptr, const void *b_ptr)
{
	const MonoGCBridgeXRef *a = (const MonoGCBridgeXRef *)a_ptr;
	const MonoGCBridgeXRef *b = (const MonoGCBridgeXRef *)b_ptr;

	if (a->src_scc_index < b->src_scc_index)
		return -1;
	if (a->src_scc_index > b->src_scc_index)
		return 1;

	if (a->dst_scc_index < b->dst_scc_index)
		return -1;
	if (a->dst_scc_index > b->dst_scc_index)
		return 1;

	return 0;
}

/*
static void
dump_processor_state (SgenBridgeProcessor *p)
{
	int i;

	printf ("------\n");
	printf ("SCCS %d\n", p->num_sccs);
	for (i = 0; i < p->num_sccs; ++i) {
		int j;
		MonoGCBridgeSCC *scc = p->api_sccs [i];
		printf ("\tSCC %d:", i);
		for (j = 0; j < scc->num_objs; ++j) {
			MonoObject *obj = scc->objs [j];
			printf (" %p(%s)", obj, SGEN_LOAD_VTABLE (obj)->klass->name);
		}
		printf ("\n");
	}

	printf ("XREFS %d\n", p->num_xrefs);
	for (i = 0; i < p->num_xrefs; ++i)
		printf ("\t%d -> %d\n", p->api_xrefs [i].src_scc_index, p->api_xrefs [i].dst_scc_index);

	printf ("-------\n");
}
*/

static gboolean
sgen_compare_bridge_processor_results (SgenBridgeProcessor *a, SgenBridgeProcessor *b)
{
	int i;
	SgenHashTable obj_to_a_scc = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_DEBUG, INTERNAL_MEM_BRIDGE_DEBUG, sizeof (int), mono_aligned_addr_hash, NULL);
	SgenHashTable b_scc_to_a_scc = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_DEBUG, INTERNAL_MEM_BRIDGE_DEBUG, sizeof (int), g_direct_hash, NULL);
	MonoGCBridgeXRef *a_xrefs, *b_xrefs;
	size_t xrefs_alloc_size;

	// dump_processor_state (a);
	// dump_processor_state (b);

	if (a->num_sccs != b->num_sccs)
		g_error ("SCCS count expected %d but got %d", a->num_sccs, b->num_sccs);
	if (a->num_xrefs != b->num_xrefs)
		g_error ("SCCS count expected %d but got %d", a->num_xrefs, b->num_xrefs);

	/*
	 * First we build a hash of each object in `a` to its respective SCC index within
	 * `a`.  Along the way we also assert that no object is more than one SCC.
	 */
	for (i = 0; i < a->num_sccs; ++i) {
		int j;
		MonoGCBridgeSCC *scc = a->api_sccs [i];

		g_assert (scc->num_objs > 0);

		for (j = 0; j < scc->num_objs; ++j) {
			GCObject *obj = scc->objs [j];
			gboolean new_entry = sgen_hash_table_replace (&obj_to_a_scc, obj, &i, NULL);
			g_assert (new_entry);
		}
	}

	/*
	 * Now we check whether each of the objects in `b` are in `a`, and whether the SCCs
	 * of `b` contain the same sets of objects as those of `a`.
	 *
	 * While we're doing this, build a hash table to map from `b` SCC indexes to `a` SCC
	 * indexes.
	 */
	for (i = 0; i < b->num_sccs; ++i) {
		MonoGCBridgeSCC *scc = b->api_sccs [i];
		MonoGCBridgeSCC *a_scc;
		int *a_scc_index_ptr;
		int a_scc_index;
		int j;
		gboolean new_entry;

		g_assert (scc->num_objs > 0);
		a_scc_index_ptr = (int *)sgen_hash_table_lookup (&obj_to_a_scc, scc->objs [0]);
		g_assert (a_scc_index_ptr);
		a_scc_index = *a_scc_index_ptr;

		//g_print ("A SCC %d -> B SCC %d\n", a_scc_index, i);

		a_scc = a->api_sccs [a_scc_index];
		g_assert (a_scc->num_objs == scc->num_objs);

		for (j = 1; j < scc->num_objs; ++j) {
			a_scc_index_ptr = (int *)sgen_hash_table_lookup (&obj_to_a_scc, scc->objs [j]);
			g_assert (a_scc_index_ptr);
			g_assert (*a_scc_index_ptr == a_scc_index);
		}

		new_entry = sgen_hash_table_replace (&b_scc_to_a_scc, GINT_TO_POINTER (i), &a_scc_index, NULL);
		g_assert (new_entry);
	}

	/*
	 * Finally, check that we have the same xrefs.  We do this by making copies of both
	 * xref arrays, and replacing the SCC indexes in the copy for `b` with the
	 * corresponding indexes in `a`.  Then we sort both arrays and assert that they're
	 * the same.
	 *
	 * At the same time, check that no xref is self-referential and that there are no
	 * duplicate ones.
	 */

	xrefs_alloc_size = a->num_xrefs * sizeof (MonoGCBridgeXRef);
	a_xrefs = (MonoGCBridgeXRef *)sgen_alloc_internal_dynamic (xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG, TRUE);
	b_xrefs = (MonoGCBridgeXRef *)sgen_alloc_internal_dynamic (xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG, TRUE);

	memcpy (a_xrefs, a->api_xrefs, xrefs_alloc_size);
	for (i = 0; i < b->num_xrefs; ++i) {
		MonoGCBridgeXRef *xref = &b->api_xrefs [i];
		int *scc_index_ptr;

		g_assert (xref->src_scc_index != xref->dst_scc_index);

		scc_index_ptr = (int *)sgen_hash_table_lookup (&b_scc_to_a_scc, GINT_TO_POINTER (xref->src_scc_index));
		g_assert (scc_index_ptr);
		b_xrefs [i].src_scc_index = *scc_index_ptr;

		scc_index_ptr = (int *)sgen_hash_table_lookup (&b_scc_to_a_scc, GINT_TO_POINTER (xref->dst_scc_index));
		g_assert (scc_index_ptr);
		b_xrefs [i].dst_scc_index = *scc_index_ptr;
	}

	mono_qsort (a_xrefs, a->num_xrefs, sizeof (MonoGCBridgeXRef), compare_xrefs);
	mono_qsort (b_xrefs, a->num_xrefs, sizeof (MonoGCBridgeXRef), compare_xrefs);

	for (i = 0; i < a->num_xrefs; ++i) {
		g_assert (a_xrefs [i].src_scc_index == b_xrefs [i].src_scc_index);
		g_assert (a_xrefs [i].dst_scc_index == b_xrefs [i].dst_scc_index);
	}

	sgen_hash_table_clean (&obj_to_a_scc);
	sgen_hash_table_clean (&b_scc_to_a_scc);
	sgen_free_internal_dynamic (a_xrefs, xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG);
	sgen_free_internal_dynamic (b_xrefs, xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG);

	return TRUE;
}

void
sgen_bridge_processing_finish (int generation)
{
	bridge_processor.processing_build_callback_data (generation);
	if (compare_bridge_processors ())
		compare_to_bridge_processor.processing_build_callback_data (generation);

	if (bridge_processor.num_sccs == 0) {
		g_assert (bridge_processor.num_xrefs == 0);
		goto after_callback;
	}

	mono_bridge_callbacks.cross_references (bridge_processor.num_sccs, bridge_processor.api_sccs,
			bridge_processor.num_xrefs, bridge_processor.api_xrefs);

	if (compare_bridge_processors ())
		sgen_compare_bridge_processor_results (&bridge_processor, &compare_to_bridge_processor);

	null_weak_links_to_dead_objects (&bridge_processor, generation);

	free_callback_data (&bridge_processor);
	if (compare_bridge_processors ())
		free_callback_data (&compare_to_bridge_processor);

 after_callback:
	bridge_processor.processing_after_callback (generation);
	if (compare_bridge_processors ())
		compare_to_bridge_processor.processing_after_callback (generation);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_GC, "GC_BRIDGE: Complete, was running for %.2fms", mono_time_since_last_stw () / 10000.0f);

	mono_bridge_processing_in_progress = FALSE;
}

MonoGCBridgeObjectKind
sgen_bridge_class_kind (MonoClass *klass)
{
	return bridge_processor.class_kind (klass);
}

void
sgen_bridge_register_finalized_object (GCObject *obj)
{
	bridge_processor.register_finalized_object (obj);
	if (compare_bridge_processors ())
		compare_to_bridge_processor.register_finalized_object (obj);
}

void
sgen_bridge_describe_pointer (GCObject *obj)
{
	if (bridge_processor.describe_pointer)
		bridge_processor.describe_pointer (obj);
}

static void
set_dump_prefix (const char *prefix)
{
	if (bridge_processor_config.dump_prefix)
		free (bridge_processor_config.dump_prefix);
	bridge_processor_config.dump_prefix = strdup (prefix);
}

/* Test support code */
static const char *bridge_class;

static MonoGCBridgeObjectKind
bridge_test_bridge_class_kind (MonoClass *klass)
{
	if (!strcmp (bridge_class, m_class_get_name (klass)) ||
			(m_class_get_parent (klass) && !strcmp (bridge_class, m_class_get_name (m_class_get_parent (klass)))))
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
	mono_field_get_value_internal (scc->objs [i], mono_bridge_test_field, &status);
	return status > 0;
}

static void
mark_scc (MonoGCBridgeSCC *scc, int value)
{
	int i;
	for (i = 0; i < scc->num_objs; ++i) {
		if (!test_scc (scc, i)) {
			int status = value;
			mono_field_set_value_internal (scc->objs [i], mono_bridge_test_field, &status);
		}
	}
}

static void
bridge_test_cross_reference2 (int num_sccs, MonoGCBridgeSCC **sccs, int num_xrefs, MonoGCBridgeXRef *xrefs)
{
	int i;
	gboolean modified;

	if (!mono_bridge_test_field) {
		mono_bridge_test_field = mono_class_get_field_from_name_full (mono_object_class (sccs[0]->objs [0]), "__test", NULL);
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
				mono_field_set_value_internal (sccs [i]->objs [j], mono_bridge_test_field, &status);
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

/* This bridge keeps all peers with __test > 0 */
static void
bridge_test_positive_status (int num_sccs, MonoGCBridgeSCC **sccs, int num_xrefs, MonoGCBridgeXRef *xrefs)
{
	int i;

	if (!mono_bridge_test_field) {
		mono_bridge_test_field = mono_class_get_field_from_name_full (mono_object_class (sccs[0]->objs [0]), "__test", NULL);
		g_assert (mono_bridge_test_field);
	}

	/*We mark all objects in a scc with live objects as reachable by scc*/
	for (i = 0; i < num_sccs; ++i) {
		int j;
		for (j = 0; j < sccs [i]->num_objs; ++j) {
			if (test_scc (sccs [i], j)) {
				sccs [i]->is_alive = TRUE;
				break;
			}
		}
	}
}


static void
register_test_bridge_callbacks (const char *bridge_class_name)
{
	MonoGCBridgeCallbacks callbacks;
	callbacks.bridge_version = SGEN_BRIDGE_VERSION;
	callbacks.bridge_class_kind = bridge_test_bridge_class_kind;
	callbacks.is_bridge_object = bridge_test_is_bridge_object;

	switch (bridge_class_name [0]) {
	case '2':
		bridge_class = bridge_class_name + 1;
		callbacks.cross_references = bridge_test_cross_reference2;
		break;
	case '3':
		bridge_class = bridge_class_name + 1;
		callbacks.cross_references = bridge_test_positive_status;
		break;
	default:
		bridge_class = bridge_class_name;
		callbacks.cross_references = bridge_test_cross_reference;
	}
	mono_gc_register_bridge_callbacks (&callbacks);
}

gboolean
sgen_bridge_handle_gc_param (const char *opt)
{
	g_assert (!bridge_processor_started ());

	if (!strcmp (opt, "bridge-require-precise-merge")) {
		bridge_processor_config.scc_precise_merge = TRUE;
	} else {
		return FALSE;
	}

	return TRUE;
}

gboolean
sgen_bridge_handle_gc_debug (const char *opt)
{
	g_assert (!bridge_processor_started ());

	if (g_str_has_prefix (opt, "bridge=")) {
		opt = strchr (opt, '=') + 1;
		register_test_bridge_callbacks (g_strdup (opt));
	} else if (!strcmp (opt, "enable-bridge-accounting")) {
		bridge_processor_config.accounting = TRUE;
	} else if (g_str_has_prefix (opt, "bridge-dump=")) {
		const char* prefix = strchr (opt, '=') + 1;
		set_dump_prefix(prefix);
	} else if (g_str_has_prefix (opt, "bridge-compare-to=")) {
		const char *name = strchr (opt, '=') + 1;
		BridgeProcessorSelection selection = bridge_processor_name (name);

		if (selection != BRIDGE_PROCESSOR_INVALID) {
			// Compare processor doesn't get config
			init_bridge_processor (&compare_to_bridge_processor, selection);
			bridge_processor_config.disable_non_bridge_scc = TRUE;
		} else {
			g_warning ("Invalid bridge implementation to compare against - ignoring.");
		}
	} else {
		return FALSE;
	}
	return TRUE;
}

void
sgen_bridge_print_gc_debug_usage (void)
{
	fprintf (stderr, "  bridge=<class-name>\n");
	fprintf (stderr, "  enable-bridge-accounting\n");
	fprintf (stderr, "  bridge-dump=<filename-prefix>\n");
	fprintf (stderr, "  bridge-compare-to=<implementation>\n");
}

#else

//UG
volatile gboolean mono_bridge_processing_in_progress = FALSE;

void
mono_gc_wait_for_bridge_processing (void)
{
}

void
mono_gc_wait_for_bridge_processing_internal (void)
{
}

MonoGCBridgeObjectKind
sgen_bridge_class_kind (MonoClass *klass)
{
	return GC_BRIDGE_TRANSPARENT_CLASS;
}

void
sgen_bridge_describe_pointer (GCObject *obj)
{
}

gboolean
sgen_bridge_handle_gc_debug (const char *opt)
{
	return FALSE;
}

gboolean
sgen_bridge_handle_gc_param (const char *opt)
{
	return FALSE;
}

void
sgen_bridge_print_gc_debug_usage (void)
{
}

void
sgen_bridge_processing_finish (int generation)
{
}

void
sgen_bridge_processing_stw_step (void)
{
}

void
sgen_bridge_register_finalized_object (GCObject *obj)
{
}

void
sgen_bridge_reset_data (void)
{
}

void
sgen_init_bridge (void)
{
}

gboolean
sgen_is_bridge_object (GCObject *obj)
{
	return FALSE;
}

gboolean
sgen_need_bridge_processing (void)
{
	return FALSE;
}

void
sgen_set_bridge_implementation (const char *name)
{
	g_error ("Sgen bridge disabled");
}

#endif

#endif
