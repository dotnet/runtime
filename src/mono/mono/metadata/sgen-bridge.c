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

MonoGCBridgeCallbacks bridge_callbacks;
static SgenBridgeProcessor bridge_processor;
static SgenBridgeProcessor compare_to_bridge_processor;

gboolean bridge_processing_in_progress = FALSE;

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

	if (!bridge_processor.reset_data)
		sgen_old_bridge_init (&bridge_processor);
}

static gboolean
init_bridge_processor (SgenBridgeProcessor *processor, const char *name)
{
	if (!strcmp ("old", name)) {
		memset (processor, 0, sizeof (SgenBridgeProcessor));
		sgen_old_bridge_init (processor);
	} else if (!strcmp ("new", name)) {
		memset (processor, 0, sizeof (SgenBridgeProcessor));
		sgen_new_bridge_init (processor);
	} else if (!strcmp ("tarjan", name)) {
		memset (processor, 0, sizeof (SgenBridgeProcessor));
		sgen_tarjan_bridge_init (processor);
	} else {
		return FALSE;
	}
	return TRUE;
}

void
sgen_set_bridge_implementation (const char *name)
{
	if (!init_bridge_processor (&bridge_processor, name))
		g_warning ("Invalid value for bridge implementation, valid values are: 'new' and 'old'.");
}

gboolean
sgen_is_bridge_object (MonoObject *obj)
{
	if ((obj->vtable->gc_bits & SGEN_GC_BIT_BRIDGE_OBJECT) != SGEN_GC_BIT_BRIDGE_OBJECT)
		return FALSE;
	return bridge_callbacks.is_bridge_object (obj);
}

gboolean
sgen_need_bridge_processing (void)
{
	return bridge_callbacks.cross_references != NULL;
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
	 * bridge_processing_in_progress must be set with the world
	 * stopped.  If not there would be race conditions.
	 */
	bridge_processing_in_progress = TRUE;

	bridge_processor.processing_stw_step ();
	if (compare_bridge_processors ())
		compare_to_bridge_processor.processing_stw_step ();
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
	sgen_null_links_with_predicate (GENERATION_NURSERY, is_bridge_object_alive, &alive_hash);
	if (generation == GENERATION_OLD)
		sgen_null_links_with_predicate (GENERATION_OLD, is_bridge_object_alive, &alive_hash);

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

void
sgen_bridge_processing_finish (int generation)
{
	unsigned long step_8;
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);

	bridge_processor.processing_build_callback_data (generation);
	if (compare_bridge_processors ())
		compare_to_bridge_processor.processing_build_callback_data (generation);

	if (bridge_processor.num_sccs == 0) {
		g_assert (bridge_processor.num_xrefs == 0);
		goto after_callback;
	}

	bridge_callbacks.cross_references (bridge_processor.num_sccs, bridge_processor.api_sccs,
			bridge_processor.num_xrefs, bridge_processor.api_xrefs);

	if (compare_bridge_processors ())
		sgen_compare_bridge_processor_results (&bridge_processor, &compare_to_bridge_processor);

	SGEN_TV_GETTIME (btv);

	null_weak_links_to_dead_objects (&bridge_processor, generation);

	free_callback_data (&bridge_processor);
	if (compare_bridge_processors ())
		free_callback_data (&compare_to_bridge_processor);

	SGEN_TV_GETTIME (atv);
	step_8 = SGEN_TV_ELAPSED (btv, atv);

 after_callback:
	bridge_processor.processing_after_callback (generation);
	if (compare_bridge_processors ())
		compare_to_bridge_processor.processing_after_callback (generation);

	bridge_processing_in_progress = FALSE;
}

MonoGCBridgeObjectKind
sgen_bridge_class_kind (MonoClass *class)
{
	return bridge_processor.class_kind (class);
}

void
sgen_bridge_register_finalized_object (MonoObject *obj)
{
	bridge_processor.register_finalized_object (obj);
	if (compare_bridge_processors ())
		compare_to_bridge_processor.register_finalized_object (obj);
}

void
sgen_bridge_describe_pointer (MonoObject *obj)
{
	if (bridge_processor.describe_pointer)
		bridge_processor.describe_pointer (obj);
}

static void
set_dump_prefix (const char *prefix)
{
	if (!bridge_processor.set_dump_prefix) {
		fprintf (stderr, "Warning: Bridge implementation does not support dumping - ignoring.\n");
		return;
	}

	bridge_processor.set_dump_prefix (prefix);
}

/* Test support code */
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

static void
register_test_bridge_callbacks (const char *bridge_class_name)
{
	MonoGCBridgeCallbacks callbacks;
	callbacks.bridge_version = SGEN_BRIDGE_VERSION;
	callbacks.bridge_class_kind = bridge_test_bridge_class_kind;
	callbacks.is_bridge_object = bridge_test_is_bridge_object;
	callbacks.cross_references = bridge_class_name[0] == '2' ? bridge_test_cross_reference2 : bridge_test_cross_reference;
	mono_gc_register_bridge_callbacks (&callbacks);
	bridge_class = bridge_class_name + (bridge_class_name[0] == '2' ? 1 : 0);
}

gboolean
sgen_bridge_handle_gc_debug (const char *opt)
{
	if (g_str_has_prefix (opt, "bridge=")) {
		opt = strchr (opt, '=') + 1;
		register_test_bridge_callbacks (g_strdup (opt));
	} else if (!strcmp (opt, "enable-bridge-accounting")) {
		bridge_processor.enable_accounting ();
	} else if (g_str_has_prefix (opt, "bridge-dump=")) {
		char *prefix = strchr (opt, '=') + 1;
		set_dump_prefix (prefix);
	} else if (g_str_has_prefix (opt, "bridge-compare-to=")) {
		const char *name = strchr (opt, '=') + 1;
		if (init_bridge_processor (&compare_to_bridge_processor, name)) {
			if (compare_to_bridge_processor.reset_data == bridge_processor.reset_data) {
				g_warning ("Cannot compare bridge implementation to itself - ignoring.");
				memset (&compare_to_bridge_processor, 0, sizeof (SgenBridgeProcessor));
			}
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

#endif
