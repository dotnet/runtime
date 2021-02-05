/**
 * \file
 * Routines for publishing metadata updates
 *
 * Copyright 2020 Microsoft
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include "mono/utils/mono-compiler.h"

#ifdef ENABLE_METADATA_UPDATE

#include <glib.h>
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/metadata-update.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/tokentype.h"
#include "mono/utils/mono-coop-mutex.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-lazy-init.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"

/* TLS value is a uint32_t of the latest published generation that the thread can see */
static MonoNativeTlsKey exposed_generation_id;

#if 1
#define UPDATE_DEBUG(stmt) do { stmt; } while (0)
#else
#define UPDATE_DEBUG(stmt) /*empty */
#endif

/*
 * set to non-zero if at least one update has been published.
 */
int mono_metadata_update_has_updates_private;

/* For each delta image, for each table:
 * - the total logical number of rows for the previous generation
 * - the number of modified rows in the current generation
 * - the number of inserted rows in the current generation
 *
 * In each delta, the physical tables contain the rows that modify existing rows of a prior generation,
 * followed by inserted rows.
 * https://github.com/dotnet/runtime/blob/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/Ecma335/MetadataAggregator.cs#L324
 * 
 * The total logical number of rows in a table for a particular generation is
 *    prev_gen_rows + inserted_rows.
 */
typedef struct _delta_row_count {
	guint32 prev_gen_rows;
	guint32 modified_rows;
	guint32 inserted_rows; 
} delta_row_count;

typedef struct _DeltaInfo {
	// for each table, the row in the EncMap table that has the first token for remapping it?
	uint32_t enc_recs [MONO_TABLE_NUM];
	delta_row_count count [MONO_TABLE_NUM];
} DeltaInfo;


static void
mono_metadata_update_ee_init (MonoError *error);

/* Maps each MonoTableInfo* to the MonoImage that it belongs to.  This is
 * mapping the base image MonoTableInfos to the base MonoImage.  We don't need
 * this for deltas.
 */
static GHashTable *table_to_image, *delta_image_to_info;
/* Low-level lock to protects table_to_image and delta_image_to_info */
/* FIXME: use concurrent hash tables so that readers don't have to lock. */
static MonoCoopMutex table_to_image_mutex;

static void
table_to_image_lock (void)
{
	mono_coop_mutex_lock (&table_to_image_mutex);
}

static void
table_to_image_unlock (void)
{
	mono_coop_mutex_unlock (&table_to_image_mutex);
}


static void
delta_info_destroy (DeltaInfo *dinfo)
{
	g_free (dinfo);
}

static DeltaInfo *
delta_info_lookup_locked (MonoImage *delta_image)
{
	return (DeltaInfo*)g_hash_table_lookup (delta_image_to_info, delta_image);
}

static DeltaInfo *
delta_info_lookup (MonoImage *delta_image)
{
	DeltaInfo *result;
	table_to_image_lock ();
	result = delta_info_lookup_locked (delta_image);
	table_to_image_unlock ();
	return result;
}

static void
table_to_image_init (void)
{
	mono_coop_mutex_init (&table_to_image_mutex);
	table_to_image = g_hash_table_new (NULL, NULL);
	delta_image_to_info = g_hash_table_new (NULL, NULL);
}

static gboolean
remove_base_image (gpointer key, gpointer value, gpointer user_data)
{
	MonoImage *base_image = (MonoImage*)user_data;
	MonoImage *value_image = (MonoImage*)value;
	return (value_image == base_image);
}

void
mono_metadata_update_cleanup_on_close (MonoImage *image)
{
	table_to_image_lock ();
	/* remove all keys (delta images) that map to the given image (base image) */
	g_hash_table_foreach_remove (table_to_image, remove_base_image, (gpointer)image);
	/* remove delta image info */
	DeltaInfo *delta_info = delta_info_lookup_locked (image);
	if (delta_info) {
		g_hash_table_remove (delta_image_to_info, image);
		delta_info_destroy (delta_info);
	}
	table_to_image_unlock ();
}

static void
table_to_image_add (MonoImage *base_image)
{
	/* If at least one table from this image is already here, they all are */
	if (g_hash_table_contains (table_to_image, &base_image->tables[MONO_TABLE_MODULE]))
		return;
	table_to_image_lock ();
	if (g_hash_table_contains (table_to_image, &base_image->tables[MONO_TABLE_MODULE])) {
	        table_to_image_unlock ();
		return;
	}
	for (int idx = 0; idx < MONO_TABLE_NUM; ++idx) {
		MonoTableInfo *table = &base_image->tables[idx];
		g_hash_table_insert (table_to_image, table, base_image);
	}
	table_to_image_unlock ();
}

MonoImage *
mono_table_info_get_base_image (const MonoTableInfo *t)
{
	MonoImage *image = (MonoImage *) g_hash_table_lookup (table_to_image, t);
	return image;
}

static MonoImage*
mono_image_open_dmeta_from_data (MonoImage *base_image, uint32_t generation, gconstpointer dmeta_bytes, uint32_t dmeta_length, MonoImageOpenStatus *status);

static void
mono_image_append_delta (MonoImage *base, MonoImage *delta);

static int
metadata_update_local_generation (MonoImage *base, MonoImage *delta);

void
mono_metadata_update_init (void)
{
	mono_metadata_update_has_updates_private = 0;
	table_to_image_init ();
	mono_native_tls_alloc (&exposed_generation_id, NULL);
}

void
mono_metadata_update_cleanup (void)
{
	mono_native_tls_free (exposed_generation_id);
}

/* Inform the execution engine that updates are coming */
static void
mono_metadata_update_ee_init (MonoError *error)
{
	static gboolean inited = FALSE;

	if (inited)
		return;
	if (mono_get_runtime_callbacks ()->metadata_update_init)
		mono_get_runtime_callbacks ()->metadata_update_init (error);
	inited = TRUE;
}

static
void
mono_metadata_update_invoke_hook (MonoDomain *domain, MonoAssemblyLoadContext *alc, uint32_t generation)
{
	if (mono_get_runtime_callbacks ()->metadata_update_published)
		mono_get_runtime_callbacks ()->metadata_update_published (domain, alc, generation);
}

static uint32_t update_published, update_alloc_frontier;
static MonoCoopMutex publish_mutex;

static void
publish_lock (void)
{
	mono_coop_mutex_lock (&publish_mutex);
}

static void
publish_unlock (void)
{
	mono_coop_mutex_unlock (&publish_mutex);
}

static mono_lazy_init_t metadata_update_lazy_init;

static void
initialize (void)
{
	mono_coop_mutex_init (&publish_mutex);
}

static void
thread_set_exposed_generation (uint32_t value)
{
	mono_native_tls_set_value (exposed_generation_id, GUINT_TO_POINTER((guint)value));
}

/**
 * LOCKING: assumes the publish_lock is held
 */
static void
metadata_update_set_has_updates (void)
{
	mono_metadata_update_has_updates_private = 1;
}

uint32_t
mono_metadata_update_prepare (MonoDomain *domain) {
	mono_lazy_initialize (&metadata_update_lazy_init, initialize);
	/*
	 * TODO: assert that the updater isn't depending on current metadata, else publishing might block.
	 */
	publish_lock ();
	uint32_t alloc_gen = ++update_alloc_frontier;
	/* Have to set this here so the updater starts using the slow path of metadata lookups */
	metadata_update_set_has_updates ();
	/* Expose the alloc frontier to the updater thread */
	thread_set_exposed_generation (alloc_gen);
	return alloc_gen;
}

gboolean
mono_metadata_update_available (void) {
	return update_published < update_alloc_frontier;
}

/**
 * mono_metadata_update_thread_expose_published:
 *
 * Allow the current thread to see the latest published deltas.
 *
 * Returns the current published generation that the thread will see.
 */
uint32_t
mono_metadata_update_thread_expose_published (void)
{
	mono_memory_read_barrier ();
	uint32_t thread_current_gen = update_published;
	thread_set_exposed_generation (thread_current_gen);
	return thread_current_gen;
}

/**
 * mono_metadata_update_get_thread_generation:
 *
 * Return the published generation that the current thread is allowed to see.
 * May be behind the latest published generation if the thread hasn't called
 * \c mono_metadata_update_thread_expose_published in a while.
 */
uint32_t
mono_metadata_update_get_thread_generation (void)
{
	return (uint32_t)GPOINTER_TO_UINT(mono_native_tls_get_value(exposed_generation_id));
}

gboolean
mono_metadata_wait_for_update (uint32_t timeout_ms)
{
	/* TODO: give threads a way to voluntarily wait for an update to be published. */
	g_assert_not_reached ();
}

void
mono_metadata_update_publish (MonoDomain *domain, MonoAssemblyLoadContext *alc, uint32_t generation) {
	g_assert (update_published < generation && generation <= update_alloc_frontier);
	/* TODO: wait for all threads that are using old metadata to update. */
	mono_metadata_update_invoke_hook (domain, alc, generation);
	update_published = update_alloc_frontier;
	mono_memory_write_barrier ();
	publish_unlock ();
}

void
mono_metadata_update_cancel (uint32_t generation)
{
	g_assert (update_alloc_frontier == generation);
	g_assert (update_alloc_frontier > 0);
	g_assert (update_alloc_frontier - 1 >= update_published);
	--update_alloc_frontier;
	/* Roll back exposed generation to the last published one */
	thread_set_exposed_generation (update_published);
	publish_unlock ();
}

/**
 * LOCKING: Assumes the publish_lock is held
 */
void
mono_image_append_delta (MonoImage *base, MonoImage *delta)
{
	if (!base->delta_image) {
		base->delta_image = base->delta_image_last = g_list_alloc ();
		base->delta_image->data = (gpointer)delta;
		return;
	}
	g_assert (((MonoImage*)base->delta_image_last->data)->generation < delta->generation);
	/* FIXME: g_list_append returns the previous end of the list, not the newly appended element! */
	base->delta_image_last = g_list_append (base->delta_image_last, delta);
}

/**
 * LOCKING: assumes the publish_lock is held
 */
MonoImage*
mono_image_open_dmeta_from_data (MonoImage *base_image, uint32_t generation, gconstpointer dmeta_bytes, uint32_t dmeta_length, MonoImageOpenStatus *status)
{
	MonoAssemblyLoadContext *alc = mono_image_get_alc (base_image);
	MonoImage *dmeta_image = mono_image_open_from_data_internal (alc, (char*)dmeta_bytes, dmeta_length, TRUE, status, FALSE, TRUE, NULL, NULL);

	dmeta_image->generation = generation;

	/* base_image takes ownership of 1 refcount ref of dmeta_image */
	mono_image_append_delta (base_image, dmeta_image);

	return dmeta_image;
}

static const char *
scope_to_string (uint32_t tok)
{
	const char *scope;
	switch (tok & MONO_RESOLUTION_SCOPE_MASK) {
	case MONO_RESOLUTION_SCOPE_MODULE:
		scope = ".";
		break;
	case MONO_RESOLUTION_SCOPE_MODULEREF:
		scope = "M";
		break;
	case MONO_RESOLUTION_SCOPE_TYPEREF:
		scope = "T";
		break;
	case MONO_RESOLUTION_SCOPE_ASSEMBLYREF:
		scope = "A";
		break;
	default:
		g_assert_not_reached ();
	}
	return scope;
}

static void
dump_update_summary (MonoImage *image_base, MonoImage *image_dmeta)
{
	int rows;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "dmeta tables:");
	for (int idx = 0; idx < MONO_TABLE_NUM; ++idx) {
		if (image_dmeta->tables [idx].base)
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "\t0x%02x \"%s\"", idx, mono_meta_table_name (idx));
	}
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "================================");

	rows = mono_image_get_table_rows (image_base, MONO_TABLE_TYPEREF);
	for (int i = 1; i <= rows; ++i) {
		guint32 cols [MONO_TYPEREF_SIZE];
		mono_metadata_decode_row (&image_base->tables [MONO_TABLE_TYPEREF], i - 1, cols, MONO_TYPEREF_SIZE);
		const char *scope = scope_to_string (cols [MONO_TYPEREF_SCOPE]);
		const char *name = mono_metadata_string_heap (image_base, cols [MONO_TYPEREF_NAME]);
		const char *nspace = mono_metadata_string_heap (image_base, cols [MONO_TYPEREF_NAMESPACE]);

		if (!name)
			name = "<N/A>";
		if (!nspace)
			nspace = "<N/A>";

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base  typeref i=%d (token=0x%08x) -> scope=%s, namespace=%s, name=%s", i, MONO_TOKEN_TYPE_REF | i, scope, nspace, name);
	}
	if (!image_dmeta->minimal_delta) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "--------------------------------");

		rows = mono_image_get_table_rows (image_dmeta, MONO_TABLE_TYPEREF);
		for (int i = 1; i <= rows; ++i) {
			guint32 cols [MONO_TYPEREF_SIZE];
			mono_metadata_decode_row (&image_dmeta->tables [MONO_TABLE_TYPEREF], i - 1, cols, MONO_TYPEREF_SIZE);
			const char *scope = scope_to_string (cols [MONO_TYPEREF_SCOPE]);
			const char *name = mono_metadata_string_heap (image_base, cols [MONO_TYPEREF_NAME]);
			const char *nspace = mono_metadata_string_heap (image_base, cols [MONO_TYPEREF_NAMESPACE]);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "dmeta typeref i=%d (token=0x%08x) -> scope=%s, nspace=%s, name=%s", i, MONO_TOKEN_TYPE_REF | i, scope, nspace, name);
		}
	}
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "================================");

	rows = mono_image_get_table_rows (image_base, MONO_TABLE_METHOD);
	for (int i = 1; i <= rows ; ++i) {
		guint32 cols [MONO_METHOD_SIZE];
		mono_metadata_decode_row_raw (&image_base->tables [MONO_TABLE_METHOD], i - 1, cols, MONO_METHOD_SIZE);
		const char *name = mono_metadata_string_heap (image_base, cols [MONO_METHOD_NAME]);
		guint32 rva = cols [MONO_METHOD_RVA];
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base  method i=%d (token=0x%08x), rva=%d/0x%04x, name=%s", i, MONO_TOKEN_METHOD_DEF | i, rva, rva, name);
	}
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "--------------------------------");

	rows = mono_image_get_table_rows (image_dmeta, MONO_TABLE_METHOD);
	for (int i = 1; i <= rows ; ++i) {
		guint32 cols [MONO_METHOD_SIZE];
		mono_metadata_decode_row_raw (&image_dmeta->tables [MONO_TABLE_METHOD], i - 1, cols, MONO_METHOD_SIZE);
		const char *name = mono_metadata_string_heap (image_base, cols [MONO_METHOD_NAME]);
		guint32 rva = cols [MONO_METHOD_RVA];
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "dmeta method i=%d (token=0x%08x), rva=%d/0x%04x, name=%s", i, MONO_TOKEN_METHOD_DEF | i, rva, rva, name);
	}
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "================================");

	rows = mono_image_get_table_rows (image_base, MONO_TABLE_STANDALONESIG);
	for (int i = 1; i <= rows; ++i) {
		guint32 cols [MONO_STAND_ALONE_SIGNATURE_SIZE];
		mono_metadata_decode_row (&image_base->tables [MONO_TABLE_STANDALONESIG], i - 1, cols, MONO_STAND_ALONE_SIGNATURE_SIZE);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base  standalonesig i=%d (token=0x%08x) -> 0x%08x", i, MONO_TOKEN_SIGNATURE | i, cols [MONO_STAND_ALONE_SIGNATURE]);
	}

	if (!image_dmeta->minimal_delta) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "--------------------------------");

		rows = mono_image_get_table_rows (image_dmeta, MONO_TABLE_STANDALONESIG);
		for (int i = 1; i <= rows; ++i) {
			guint32 cols [MONO_STAND_ALONE_SIGNATURE_SIZE];
			mono_metadata_decode_row_raw (&image_dmeta->tables [MONO_TABLE_STANDALONESIG], i - 1, cols, MONO_STAND_ALONE_SIGNATURE_SIZE);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "dmeta standalonesig i=%d (token=0x%08x) -> 0x%08x", i, MONO_TOKEN_SIGNATURE | i, cols [MONO_STAND_ALONE_SIGNATURE]);
		}
	}
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "================================");

}

void
mono_image_effective_table_slow (const MonoTableInfo **t, int *idx)
{
	if (G_LIKELY (*idx < (*t)->rows))
		return;

	/* FIXME: don't let any thread other than the updater thread see values from a delta image
	 * with a generation past update_published
	 */

	MonoImage *base = mono_table_info_get_base_image (*t);
	if (!base || !base->delta_image)
		return;

	GList *list = base->delta_image;
	MonoImage *dmeta;
	int ridx;
	MonoTableInfo *table;

	/* Invariant: `*t` must be a `MonoTableInfo` of the base image. */
	g_assert (base->tables < *t && *t < &base->tables [MONO_TABLE_LAST]);

	size_t s = ALIGN_TO (sizeof (MonoTableInfo), sizeof (gpointer));
	int tbl_index = ((intptr_t) *t - (intptr_t) base->tables) / s;

	/* FIXME: I don't understand how ReplaceMethodOften works - it always has a 
	 * EnCMap  entry 2: 0x06000002 (MethodDef) for every revision.  Shouldn't the number of methodDef rows be going up?

	 * Apparently not - because conceptually the EnC log is saying to overwrite the existing rows.
	 */

	/* FIXME: so if the tables are conceptually mutated by each delta, we can't just stop at the
	 * first lookup that gets a relative index in the right range, can we? that will always be
	 * the oldest delta.
	 */

	/* FIXME: the other problem is that the EnClog is a sequence of actions to MUTATE rows.  So when looking up an existing row we have to be able to make it so that naive callers decoding that row see the updated data.
	 *
	 * That's the main thing that PAss1 should eb doing for us.
	 *
	 * I think we can't get away from mutating.  The format is just too geared toward it.
	 *
	 * We should make the mutations atomic, though.  (And I guess the heap extension is probably unavoidable)
	 *
	 * 1. Keep a table of inv
	 */

	do {
		g_assertf (list, "couldn't find idx=0x%08x in assembly=%s", *idx, dmeta && dmeta->name ? dmeta->name : "unknown image");
		dmeta = (MonoImage*)list->data;
		list = list->next;
		table = &dmeta->tables [tbl_index];
		ridx = mono_image_relative_delta_index (dmeta, mono_metadata_make_token (tbl_index, *idx + 1)) - 1;
	} while (ridx < 0 || ridx >= table->rows);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "effective table for %s: 0x%08x -> 0x%08x (gen %d)", mono_meta_table_name (tbl_index), *idx, ridx, metadata_update_local_generation (base, dmeta));

	*t = table;
	*idx = ridx;
}

/*
 * The ENCMAP table contains the base of the relative offset.
 *
 * Example:
 * Say you have a base image with a METHOD table having 5 entries.  The minimal
 * delta image adds another one, so it would be indexed with token
 * `MONO_TOKEN_METHOD_DEF | 6`. However, the minimal delta image only has this
 * single entry, and thus this would be an out-of-bounds access. That's where
 * the ENCMAP table comes into play: It will have an entry
 * `MONO_TOKEN_METHOD_DEF | 5`, so before accessing the new entry in the
 * minimal delta image, it has to be substracted. Thus the new relative index
 * is `1`, and no out-of-bounds acccess anymore.
 *
 * One can assume that ENCMAP is sorted (todo: verify this claim).
 *
 * BTW, `enc_recs` is just a pre-computed map to make the lookup for the
 * relative index faster.
 */
int
mono_image_relative_delta_index (MonoImage *image_dmeta, int token)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);

	/* this helper expects and returns as "index origin = 1" */
	g_assert (index > 0);

	if (!encmap->rows || !image_dmeta->minimal_delta)
		return mono_metadata_token_index (token);

	DeltaInfo *delta_info = delta_info_lookup (image_dmeta);
	g_assert (delta_info);

	int index_map = delta_info->enc_recs [table];
	guint32 cols[MONO_ENCMAP_SIZE];
	mono_metadata_decode_row (encmap, index_map - 1, cols, MONO_ENCMAP_SIZE);
	int map_entry = cols [MONO_ENCMAP_TOKEN];

	while (mono_metadata_token_table (map_entry) == table && mono_metadata_token_index (map_entry) < index && index_map < encmap->rows) {
		mono_metadata_decode_row (encmap, ++index_map - 1, cols, MONO_ENCMAP_SIZE);
		map_entry = cols [MONO_ENCMAP_TOKEN];
	}

	if (mono_metadata_token_table (map_entry) == table) {
#if 0
		g_assert (mono_metadata_token_index (map_entry) == index);
#endif
		if (mono_metadata_token_index (map_entry) != index)
			if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE))
				g_print ("warning: map_entry=0x%08x != index=0x%08x. is this a problem?\n", map_entry, index);
	}

	int return_val = index_map - delta_info->enc_recs [table] + 1;
	g_assert (return_val > 0);
	return return_val;
}

static DeltaInfo*
delta_info_init (MonoImage *image_dmeta, MonoImage *image_base)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];
	int table, prev_table = -1, idx;

	g_assert (!delta_info_lookup (image_dmeta));

	if (!encmap->rows)
		return NULL;

	DeltaInfo *delta_info = g_malloc0 (sizeof (DeltaInfo));

	/*** Compute logical table sizes ***/
	if (image_base->delta_image == image_base->delta_image_last) {
		/* this is the first update. */
		for (int i = 0; i < MONO_TABLE_NUM; ++i) {
			delta_info->count[i].prev_gen_rows = image_base->tables[i].rows;
		}
	} else {
		/* Current image_dmeta is image_base->delta_image_last->data,
		 * find its predecessor
		 */
		MonoImage *prev_delta = NULL;
		g_assert (image_base->delta_image_last->prev != NULL);
		prev_delta = (MonoImage*)image_base->delta_image_last->prev->data;
		DeltaInfo *prev_gen_info = delta_info_lookup (prev_delta);
		for (int i = 0; i < MONO_TABLE_NUM; ++i) {
			delta_info->count[i].prev_gen_rows = prev_gen_info->count[i].prev_gen_rows + prev_gen_info->count[i].inserted_rows;
		}
	}


	/* TODO: while going through the tables, update delta_info->count[tbl].{modified,inserted}_rows */

	for (idx = 1; idx <= encmap->rows; ++idx) {
		guint32 cols[MONO_ENCMAP_SIZE];
		mono_metadata_decode_row (encmap, idx - 1, cols, MONO_ENCMAP_SIZE);
		uint32_t tok = cols [MONO_ENCMAP_TOKEN];
		table = mono_metadata_token_table (tok);
		uint32_t rid = mono_metadata_token_index (tok);
		g_assert (table >= 0 && table < MONO_TABLE_NUM);
		g_assert (table != MONO_TABLE_ENCLOG);
		g_assert (table != MONO_TABLE_ENCMAP);
		g_assert (table >= prev_table);
		/* FIXME: check bounds - is it < or <=. */
		if (rid < delta_info->count[table].prev_gen_rows)
			delta_info->count[table].modified_rows++;
		else
			delta_info->count[table].inserted_rows++;
		if (table == prev_table)
			continue;
		while (prev_table < table) {
			prev_table++;
			delta_info->enc_recs [prev_table] = idx;
		}
	}
	g_assert (prev_table < MONO_TABLE_NUM - 1);
	while (prev_table < MONO_TABLE_NUM - 1) {
		prev_table++;
		delta_info->enc_recs [prev_table] = idx;
	}

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE)) {
		for (int i = 0 ; i < MONO_TABLE_NUM; ++i) {
			if (!image_dmeta->tables [i].base)
				continue;

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "enc_recs [%02x] / %s = 0x%02x\t(inserted: %03d, modified: %03d)", i, mono_meta_table_name (i), delta_info->enc_recs[i], delta_info->count[i].inserted_rows, delta_info->count[i].modified_rows);
		}
	}

	table_to_image_lock ();
	g_hash_table_insert (delta_image_to_info, image_dmeta, delta_info);
	table_to_image_unlock ();


	return delta_info;
}

static const char*
funccode_to_str (int func_code)
{
	switch (func_code) {
		case 0: return "Func default";
		case 1: return "Method Create";
		case 2: return "Field Create";
		case 3: return "Param Create";
		case 4: return "Property Create";
		case 5: return "Event Create";
		default: g_assert_not_reached ();
	}
	return NULL;
}

/* Run some sanity checks first. If we detect unsupported scenarios, this
 * function will fail and the metadata update should be aborted. This should
 * run before anything in the metadata world is updated. */
static gboolean
apply_enclog_pass1 (MonoImage *image_base, MonoImage *image_dmeta, gconstpointer dil_data, uint32_t dil_length, MonoError *error)
{
	MonoTableInfo *table_enclog = &image_dmeta->tables [MONO_TABLE_ENCLOG];
	int rows = table_enclog->rows;

	gboolean unsupported_edits = FALSE;

	/* hack: make a pass over it, looking only for table method updates, in
	 * order to give more meaningful error messages first */

	for (int i = 0; i < rows ; ++i) {
		guint32 cols [MONO_ENCLOG_SIZE];
		mono_metadata_decode_row (table_enclog, i, cols, MONO_ENCLOG_SIZE);

		// FIXME: the top bit 0x8000000 of log_token is some kind of
		// indicator see IsRecId in metamodelrw.cpp and
		// MDInternalRW::EnumDeltaTokensInit which skips over those
		// records when EditAndContinueModule::ApplyEditAndContinue is
		// iterating.
		int log_token = cols [MONO_ENCLOG_TOKEN];
		int func_code = cols [MONO_ENCLOG_FUNC_CODE];

		int token_table = mono_metadata_token_table (log_token);
		int token_index = mono_metadata_token_index (log_token);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x (%s idx=0x%02x) (base table has 0x%04x rows)\tfunc=0x%02x\n", i, log_token, mono_meta_table_name (token_table), token_index, image_base->tables [token_table].rows, func_code);


		if (token_table != MONO_TABLE_METHOD)
			continue;

		if (token_index > image_base->tables [token_table].rows) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "\tcannot add new method with token 0x%08x", log_token);
			mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: cannot add new method with token 0x%08x", log_token);
			unsupported_edits = TRUE;
		}

		g_assert (func_code == 0); /* anything else doesn't make sense here */
	}

	for (int i = 0; i < rows ; ++i) {
		guint32 cols [MONO_ENCLOG_SIZE];
		mono_metadata_decode_row (table_enclog, i, cols, MONO_ENCLOG_SIZE);

		int log_token = cols [MONO_ENCLOG_TOKEN];
		int func_code = cols [MONO_ENCLOG_FUNC_CODE];

		int token_table = mono_metadata_token_table (log_token);
		int token_index = mono_metadata_token_index (log_token);

		if (token_table == MONO_TABLE_ASSEMBLYREF) {
			/* okay, supported */
		} else if (token_table == MONO_TABLE_METHOD) {
			/* handled above */
		} else {
			if (token_index <= image_base->tables [token_table].rows) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x we do not support patching of existing table cols.", i, log_token);
				mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support patching of existing table cols. token=0x%08x", log_token);
				unsupported_edits = TRUE;
				continue;
			}
		}


		/*
		 * So the way a non-default func_code works is that it's attached to the EnCLog
		 * record preceeding the new member defintion (so e.g. an addMethod code will be on
		 * the preceeding MONO_TABLE_TYPEDEF enc record that identifies the parent type).
		 */
		switch (func_code) {
			case 0: /* default */
				break;
			default:
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x FunCode %d (%s) not supported (token=0x%08x)", i, log_token, func_code, funccode_to_str (func_code), log_token);
				mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: FuncCode %d (%s) not supported (token=0x%08x)", func_code, funccode_to_str (func_code), log_token);
				unsupported_edits = TRUE;
				continue;
		}
	}
	return !unsupported_edits;
}

static void
set_update_method (MonoImage *image_base, uint32_t generation, MonoImage *image_dmeta, uint32_t token_index, const char* il_address)
{
	/* FIXME: this is a race if other threads are doing a lookup. */
	g_hash_table_insert (image_base->method_table_update, GUINT_TO_POINTER (token_index), GUINT_TO_POINTER (generation));
	g_hash_table_insert (image_dmeta->method_table_update, GUINT_TO_POINTER (token_index), (gpointer) il_address);
}

/* do actuall enclog application */
static gboolean
apply_enclog_pass2 (MonoImage *image_base, uint32_t generation, MonoImage *image_dmeta, gconstpointer dil_data, uint32_t dil_length, MonoError *error)
{
	MonoTableInfo *table_enclog = &image_dmeta->tables [MONO_TABLE_ENCLOG];
	int rows = table_enclog->rows;

	gboolean assemblyref_updated = FALSE;
	for (int i = 0; i < rows ; ++i) {
		guint32 cols [MONO_ENCLOG_SIZE];
		mono_metadata_decode_row (table_enclog, i, cols, MONO_ENCLOG_SIZE);

		int log_token = cols [MONO_ENCLOG_TOKEN];
		int func_code = cols [MONO_ENCLOG_FUNC_CODE];

		int token_table = mono_metadata_token_table (log_token);
		int token_index = mono_metadata_token_index (log_token);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "enclog i=%d: token=0x%08x (table=%s): %d", i, log_token, mono_meta_table_name (token_table), func_code);

		/* TODO: See CMiniMdRW::ApplyDelta for how to drive this.
		 */
		switch (func_code) {
			case 0: /* default */
				break;
			default:
				g_error ("EnC: unsupported FuncCode, should be caught by pass1");
				break;
		}

		if (token_table == MONO_TABLE_ASSEMBLYREF) {
			g_assert (token_index > image_base->tables [token_table].rows);

			if (assemblyref_updated)
				continue;

			assemblyref_updated = TRUE;

			/* FIXME: use DeltaInfo:prev_gen_rows instead of looping */
			/* TODO: do we know that there will never be modified rows in ASSEMBLYREF? */
			int old_rows = image_base->tables [MONO_TABLE_ASSEMBLYREF].rows;
			for (GList *l = image_base->delta_image; l; l = l->next) {
				MonoImage *delta_child = l->data;
				old_rows += delta_child->tables [MONO_TABLE_ASSEMBLYREF].rows;
			}
			int new_rows = image_dmeta->tables [MONO_TABLE_ASSEMBLYREF].rows;

			old_rows -= new_rows;
			g_assert (new_rows > 0);
			g_assert (old_rows > 0);

			/* TODO: this can end bad with code around assembly.c:mono_assembly_load_reference */
			mono_image_lock (image_base);
			MonoAssembly **old_array = image_base->references;
			g_assert (image_base->nreferences == old_rows);

			image_base->references = g_new0 (MonoAssembly *, old_rows + new_rows + 1);
			memcpy (image_base->references, old_array, sizeof (gpointer) * (old_rows + 1));
			image_base->nreferences = old_rows + new_rows;
			mono_image_unlock (image_base);

			g_free (old_array);
		} else if (token_table == MONO_TABLE_METHOD) {
			if (token_index > image_base->tables [token_table].rows) {
				g_error ("EnC: new method added, should be caught by pass1");
			}

			if (!image_base->method_table_update)
				image_base->method_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);
			if (!image_dmeta->method_table_update)
				image_dmeta->method_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);

			int mapped_token = mono_image_relative_delta_index (image_dmeta, mono_metadata_make_token (token_table, token_index));
			int rva = mono_metadata_decode_row_col (&image_dmeta->tables [MONO_TABLE_METHOD], mapped_token - 1, MONO_METHOD_RVA);
			if (rva < dil_length) {
				char *il_address = ((char *) dil_data) + rva;
				set_update_method (image_base, generation, image_dmeta, token_index, il_address);
			} else {
				/* rva points probably into image_base IL stream. can this ever happen? */
				g_print ("TODO: this case is still a bit contrived. token=0x%08x with rva=0x%04x\n", log_token, rva);
			}
		} else if (token_table == MONO_TABLE_TYPEDEF) {
			/* TODO: throw? */
			/* TODO: happens changing the class (adding field or method). we ignore it, but dragons are here */

			/* existing entries are supposed to be patched */
			g_assert (token_index <= image_base->tables [token_table].rows);
		} else {
			g_assert (token_index > image_base->tables [token_table].rows);
			if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE))
				g_print ("todo: do something about this table index: 0x%02x\n", token_table);
		}
	}
	return TRUE;
}

/**
 *
 * LOCKING: Takes the publish_lock
 */
void
mono_image_load_enc_delta (MonoDomain *domain, MonoImage *image_base, gconstpointer dmeta_bytes, uint32_t dmeta_length, gconstpointer dil_bytes, uint32_t dil_length, MonoError *error)
{
	mono_metadata_update_ee_init (error);
	if (!is_ok (error))
		return;

	const char *basename = image_base->filename;
	/* FIXME:
	 * (1) do we need to memcpy dmeta_bytes ? (maybe)
	 * (2) do we need to memcpy dil_bytes ? (pretty sure, yes)
	 */

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE)) {
		g_print ("LOADING basename=%s delta update.\ndelta image=%p & dil=%p\n", basename, dmeta_bytes, dil_bytes);
		/* TODO: add a non-async version of mono_dump_mem */
		mono_dump_mem (dmeta_bytes, dmeta_length);
		mono_dump_mem (dil_bytes, dil_length);
	}

	uint32_t generation = mono_metadata_update_prepare (domain);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image string size: 0x%08x", image_base->heap_strings.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image user string size: 0x%08x", image_base->heap_us.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image blob heap addr: %p", image_base->heap_blob.data);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image blob heap size: 0x%08x", image_base->heap_blob.size);

	MonoImageOpenStatus status;
	MonoImage *image_dmeta = mono_image_open_dmeta_from_data (image_base, generation, dmeta_bytes, dmeta_length, &status);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image string size: 0x%08x", image_dmeta->heap_strings.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image user string size: 0x%08x", image_dmeta->heap_us.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image blob heap addr: %p", image_dmeta->heap_blob.data);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image blob heap size: 0x%08x", image_dmeta->heap_blob.size);
	g_assert (image_dmeta);
	g_assert (status == MONO_IMAGE_OK);

	if (image_dmeta->minimal_delta) {
		guint32 idx = mono_metadata_decode_row_col (&image_dmeta->tables [MONO_TABLE_MODULE], 0, MONO_MODULE_NAME);

		const char *module_name = NULL;
		module_name = mono_metadata_string_heap (image_base, idx);

		/* Set the module name now that we know the base String heap size */
		g_assert (!image_dmeta->module_name);
		image_dmeta->module_name = module_name;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "applied dmeta name: '%s'\n", module_name);
	}

	MonoTableInfo *table_enclog = &image_dmeta->tables [MONO_TABLE_ENCLOG];
	MonoTableInfo *table_encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];

	if (!table_enclog->rows && !table_encmap->rows) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "No enclog or encmap in delta image for base=%s, nothing to do", basename);
		mono_metadata_update_cancel (generation);
		return;
	}

	if (!apply_enclog_pass1 (image_base, image_dmeta, dil_bytes, dil_length, error)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Error on sanity-checking delta image to base=%s, due to: %s", basename, mono_error_get_message (error));
		mono_metadata_update_cancel (generation);
		return;
	}

	/* if there are updates, start tracking the tables of the base image, if we weren't already. */
	if (table_enclog->rows)
		table_to_image_add (image_base);

	delta_info_init (image_dmeta, image_base);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base  guid: %s", image_base->guid);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "dmeta guid: %s", image_dmeta->guid);

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE))
		dump_update_summary (image_base, image_dmeta);

	if (!apply_enclog_pass2 (image_base, generation, image_dmeta, dil_bytes, dil_length, error)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Error applying delta image to base=%s, due to: %s", basename, mono_error_get_message (error));
		mono_metadata_update_cancel (generation);
		return;
	}
	mono_error_assert_ok (error);

	MonoAssemblyLoadContext *alc = mono_image_get_alc (image_base);
	mono_metadata_update_publish (domain, alc, generation);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, ">>> EnC delta for base=%s (generation %d) applied", basename, generation);
}

/*
 * Returns how many times the base image was updated upto and including the given delta.
 */
static int
metadata_update_local_generation (MonoImage *base, MonoImage *delta)
{
	if (delta == base)
		return 0;
	int index = g_list_index (base->delta_image, delta);
	g_assert (index != -1);
	return 1 + index;
}

/*
 * Returns how many times the given base image has been updated so far.
 *
 * NOTE: doesn't look at update_published or update_alloc_frontier, and therefore only usable by the
 * update originator.
 */
static int
metadata_update_count_updates (MonoImage *base)
{
	if (!base->delta_image_last)
		return 0;
	else
		return metadata_update_local_generation (base, (MonoImage*)base->delta_image_last->data);
}
#else /* ENABLE_METADATA_UPDATE */
MONO_EMPTY_SOURCE_FILE (metadata_update);
#endif /* ENABLE_METADATA_UPDATE */

