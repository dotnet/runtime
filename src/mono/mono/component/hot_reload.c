// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>

#include <config.h>
#include "mono/utils/mono-compiler.h"

#include <glib.h>
#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/metadata-update.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/tokentype.h"
#include "mono/utils/mono-coop-mutex.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-lazy-init.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"
#include "mono/metadata/mono-debug.h"


#include <mono/component/hot_reload.h>

#include <mono/utils/mono-compiler.h>

static void
hot_reload_init (void);

static bool
hot_reload_available (void);

static void
hot_reload_set_fastpath_data (MonoMetadataUpdateData *data);

static gboolean
hot_reload_update_enabled (int *modifiable_assemblies_out);

static gboolean
hot_reload_no_inline (MonoMethod *caller, MonoMethod *callee);

static uint32_t
hot_reload_thread_expose_published (void);

static uint32_t
hot_reload_get_thread_generation (void);

static void
hot_reload_cleanup_on_close (MonoImage *image);

static void
hot_reload_effective_table_slow (const MonoTableInfo **t, int *idx);

static void
hot_reload_apply_changes (int origin, MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, gconstpointer dpdb_bytes_orig, uint32_t dpdb_length, MonoError *error);

static int
hot_reload_relative_delta_index (MonoImage *image_dmeta, int token);

static void
hot_reload_close_except_pools_all (MonoImage *base_image);

static void
hot_reload_close_all (MonoImage *base_image);

static gpointer
hot_reload_get_updated_method_rva (MonoImage *base_image, uint32_t idx);

static gboolean
hot_reload_table_bounds_check (MonoImage *base_image, int table_index, int token_index);

static gboolean
hot_reload_delta_heap_lookup (MonoImage *base_image, MetadataHeapGetterFunc get_heap, uint32_t orig_index, MonoImage **image_out, uint32_t *index_out);

static gpointer 
hot_reload_get_updated_method_ppdb (MonoImage *base_image, uint32_t idx);

static gboolean
hot_reload_has_modified_rows (const MonoTableInfo *table);

static MonoComponentHotReload fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &hot_reload_available },
	&hot_reload_set_fastpath_data,
	&hot_reload_update_enabled,
	&hot_reload_no_inline,
	&hot_reload_thread_expose_published,
	&hot_reload_get_thread_generation,
	&hot_reload_cleanup_on_close,
	&hot_reload_effective_table_slow,
	&hot_reload_relative_delta_index,
	&hot_reload_apply_changes,
	&hot_reload_close_except_pools_all,
	&hot_reload_close_all,
	&hot_reload_get_updated_method_rva,
	&hot_reload_table_bounds_check,
	&hot_reload_delta_heap_lookup,
	&hot_reload_get_updated_method_ppdb,
	&hot_reload_has_modified_rows,
};

MonoComponentHotReload *
mono_component_hot_reload_init (void)
{
	hot_reload_init ();
	return &fn_table;
}

static bool
hot_reload_available (void)
{
	return true;
}

static MonoMetadataUpdateData* metadata_update_data_ptr;

static void
hot_reload_set_fastpath_data (MonoMetadataUpdateData *ptr)
{
	metadata_update_data_ptr = ptr;
}

/* TLS value is a uint32_t of the latest published generation that the thread can see */
static MonoNativeTlsKey exposed_generation_id;

#if 1
#define UPDATE_DEBUG(stmt) do { stmt; } while (0)
#else
#define UPDATE_DEBUG(stmt) /*empty */
#endif

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

/* Additional informaiton for MonoImages representing deltas */
typedef struct _DeltaInfo {
	uint32_t generation; /* global update ID that added this delta image */

	/* Maps MethodDef token indices to a pointer into the RVA of the delta IL */
	GHashTable *method_table_update;

	/* Maps MethodDef token indices to a pointer into the RVA of the delta PPDB */
	GHashTable *method_ppdb_table_update;

	// for each table, the row in the EncMap table that has the first token for remapping it?
	uint32_t enc_recs [MONO_TABLE_NUM];
	delta_row_count count [MONO_TABLE_NUM];
} DeltaInfo;


/* Additional informaiton for baseline MonoImages */
typedef struct _BaselineInfo {
	/* List of MonoImages of deltas.  Parent image owns 1 refcount ref of the delta image */
	GList *delta_image;
	/* Tail of delta_image for fast appends */
	GList *delta_image_last;

	/* Maps MethodDef token indices to a boolean flag that there's an update for the method */
	GHashTable *method_table_update;

	/* TRUE if any published update modified an existing row */
	gboolean any_modified_rows [MONO_TABLE_NUM];
} BaselineInfo;

#define DOTNET_MODIFIABLE_ASSEMBLIES "DOTNET_MODIFIABLE_ASSEMBLIES"

/**
 * mono_metadata_update_enable:
 * \param modifiable_assemblies_out: set to MonoModifiableAssemblies value
 *
 * Returns \c TRUE if metadata updates are enabled at runtime.	False otherwise.
 *
 * If \p modifiable_assemblies_out is not \c NULL, it's set on return.
 *
 * The result depends on the value of the DOTNET_MODIFIABLE_ASSEMBLIES
 * environment variable.  "debug" means debuggable assemblies are modifiable,
 * all other values are ignored and metadata updates are disabled.
 */
gboolean
hot_reload_update_enabled (int *modifiable_assemblies_out)
{
	static gboolean inited = FALSE;
	static int modifiable = MONO_MODIFIABLE_ASSM_NONE;

	if (!inited) {
		char *val = g_getenv (DOTNET_MODIFIABLE_ASSEMBLIES);
		if (val && !g_strcasecmp (val, "debug")) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Metadata update enabled for debuggable assemblies");
			modifiable = MONO_MODIFIABLE_ASSM_DEBUG;
		}
		g_free (val);
		inited = TRUE;
	}
	if (modifiable_assemblies_out)
		*modifiable_assemblies_out = modifiable;
	return modifiable != MONO_MODIFIABLE_ASSM_NONE;
}

static gboolean
assembly_update_supported (MonoAssembly *assm)
{
	int modifiable = 0;
	if (!hot_reload_update_enabled (&modifiable))
		return FALSE;
	if (modifiable == MONO_MODIFIABLE_ASSM_DEBUG &&
	    mono_assembly_is_jit_optimizer_disabled (assm))
		return TRUE;
	return FALSE;
}

/**
 * hot_reload_update_no_inline:
 * \param caller: the calling method
 * \param callee: the method being called
 *
 * Returns \c TRUE if \p callee should not be inlined into \p caller.
 *
 * If metadata updates are enabled either for the caller or callee's module,
 * the callee should not be inlined.
 *
 */
gboolean
hot_reload_no_inline (MonoMethod *caller, MonoMethod *callee)
{
	if (!hot_reload_update_enabled (NULL))
		return FALSE;
	MonoAssembly *caller_assm = m_class_get_image(caller->klass)->assembly;
	MonoAssembly *callee_assm = m_class_get_image(callee->klass)->assembly;
	return mono_assembly_is_jit_optimizer_disabled (caller_assm) || mono_assembly_is_jit_optimizer_disabled (callee_assm);
}

/* Maps each MonoTableInfo* to the MonoImage that it belongs to.  This is
 * mapping the base image MonoTableInfos to the base MonoImage.  We don't need
 * this for deltas.
 */
static GHashTable *table_to_image;
/* Maps each delta MonoImage to its DeltaInfo */
static GHashTable *delta_image_to_info;
/* Maps each baseline MonoImage to its BaselineInfo */
static GHashTable *baseline_image_to_info;
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


static BaselineInfo *
baseline_info_init (MonoImage *image_base)
{
	BaselineInfo *baseline_info = g_malloc0 (sizeof (BaselineInfo));

	return baseline_info;
}

static void
baseline_info_destroy (BaselineInfo *info)
{
	if (info->method_table_update)
		g_hash_table_destroy (info->method_table_update);
	g_free (info);
}

static BaselineInfo *
baseline_info_lookup_or_add (MonoImage *base_image)
{
	BaselineInfo *info;
	table_to_image_lock ();
	info = g_hash_table_lookup (baseline_image_to_info, base_image);
	if (!info) {
		info = baseline_info_init (base_image);
		g_hash_table_insert (baseline_image_to_info, base_image, info);
	}
	table_to_image_unlock ();
	return info;
}

static void
baseline_info_remove (MonoImage *base_image)
{
	table_to_image_lock ();
	g_hash_table_remove (baseline_image_to_info, base_image);
	table_to_image_unlock ();
}

static BaselineInfo *
baseline_info_lookup (MonoImage *base_image)
{
	BaselineInfo *info;
	table_to_image_lock ();
	info = g_hash_table_lookup (baseline_image_to_info, base_image);
	table_to_image_unlock ();
	return info;
}

static DeltaInfo*
delta_info_init (MonoImage *image_dmeta, MonoImage *image_base, BaselineInfo *base_info, uint32_t generation);

static void
free_ppdb_entry (gpointer key, gpointer val, gpointer user_data)
{
	g_free (val);
}

static void
delta_info_destroy (DeltaInfo *dinfo)
{
	if (dinfo->method_table_update)
		g_hash_table_destroy (dinfo->method_table_update);
	if (dinfo->method_ppdb_table_update) {
		g_hash_table_foreach (dinfo->method_ppdb_table_update, free_ppdb_entry, NULL);
		g_hash_table_destroy (dinfo->method_ppdb_table_update);
	}
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
	baseline_image_to_info = g_hash_table_new (NULL, NULL);
}

static gboolean
remove_base_image (gpointer key, gpointer value, gpointer user_data)
{
	MonoImage *base_image = (MonoImage*)user_data;
	MonoImage *value_image = (MonoImage*)value;
	return (value_image == base_image);
}

void
hot_reload_cleanup_on_close (MonoImage *image)
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

void
hot_reload_close_except_pools_all (MonoImage *base_image)
{
	BaselineInfo *info = baseline_info_lookup (base_image);
	if (!info)
		return;
	for (GList *ptr = info->delta_image; ptr; ptr = ptr->next) {
		MonoImage *image = (MonoImage *)ptr->data;
		if (image) {
			if (!mono_image_close_except_pools (image))
			    ptr->data = NULL;
		}
	}
}

void
hot_reload_close_all (MonoImage *base_image)
{
	BaselineInfo *info = baseline_info_lookup (base_image);
	if (!info)
		return;
	for (GList *ptr = info->delta_image; ptr; ptr = ptr->next) {
		MonoImage *image = (MonoImage *)ptr->data;
		if (image)
			mono_image_close_finish (image);
	}
	g_list_free (info->delta_image);
	baseline_info_remove (base_image);
	baseline_info_destroy (info);
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

static MonoImage *
table_info_get_base_image (const MonoTableInfo *t)
{
	MonoImage *image = (MonoImage *) g_hash_table_lookup (table_to_image, t);
	return image;
}

/* Given a table, find the base image that it came from and its table index */
static gboolean
table_info_find_in_base (const MonoTableInfo *table, MonoImage **base_out, int *tbl_index)
{
	g_assert (base_out);
	*base_out = NULL;
	MonoImage *base = table_info_get_base_image (table);
	if (!base)
		return FALSE;

	*base_out = base;

	/* Invariant: `table` must be a `MonoTableInfo` of the base image. */
	g_assert (base->tables < table && table < &base->tables [MONO_TABLE_LAST]);

	if (tbl_index) {
		size_t s = ALIGN_TO (sizeof (MonoTableInfo), sizeof (gpointer));
		*tbl_index = ((intptr_t) table - (intptr_t) base->tables) / s;
	}
	return TRUE;
}

static MonoImage*
image_open_dmeta_from_data (MonoImage *base_image, uint32_t generation, gconstpointer dmeta_bytes, uint32_t dmeta_length);

static void
image_append_delta (MonoImage *base, BaselineInfo *base_info, MonoImage *delta, DeltaInfo *delta_info);

static int
metadata_update_local_generation (MonoImage *base, BaselineInfo *base_info, MonoImage *delta);

void
hot_reload_init (void)
{
	table_to_image_init ();
	mono_native_tls_alloc (&exposed_generation_id, NULL);
}

static
void
hot_reload_update_published_invoke_hook (MonoAssemblyLoadContext *alc, uint32_t generation)
{
	if (mono_get_runtime_callbacks ()->metadata_update_published)
		mono_get_runtime_callbacks ()->metadata_update_published (alc, generation);
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
hot_reload_set_has_updates (void)
{
	g_assert (metadata_update_data_ptr != NULL);
	metadata_update_data_ptr->has_updates = 1;
}

static uint32_t
hot_reload_update_prepare (void)
{
	mono_lazy_initialize (&metadata_update_lazy_init, initialize);
	/*
	 * TODO: assert that the updater isn't depending on current metadata, else publishing might block.
	 */
	publish_lock ();
	uint32_t alloc_gen = ++update_alloc_frontier;
	/* Have to set this here so the updater starts using the slow path of metadata lookups */
	hot_reload_set_has_updates ();
	/* Expose the alloc frontier to the updater thread */
	thread_set_exposed_generation (alloc_gen);
	return alloc_gen;
}

static gboolean G_GNUC_UNUSED
hot_reload_update_available (void)
{
	return update_published < update_alloc_frontier;
}

/**
 * hot_reaload_thread_expose_published:
 *
 * Allow the current thread to see the latest published deltas.
 *
 * Returns the current published generation that the thread will see.
 */
uint32_t
hot_reload_thread_expose_published (void)
{
	mono_memory_read_barrier ();
	uint32_t thread_current_gen = update_published;
	thread_set_exposed_generation (thread_current_gen);
	return thread_current_gen;
}

/**
 * hot_reload_get_thread_generation:
 *
 * Return the published generation that the current thread is allowed to see.
 * May be behind the latest published generation if the thread hasn't called
 * \c mono_metadata_update_thread_expose_published in a while.
 */
uint32_t
hot_reload_get_thread_generation (void)
{
	return (uint32_t)GPOINTER_TO_UINT(mono_native_tls_get_value(exposed_generation_id));
}

static gboolean G_GNUC_UNUSED
hot_reload_wait_for_update (uint32_t timeout_ms)
{
	/* TODO: give threads a way to voluntarily wait for an update to be published. */
	g_assert_not_reached ();
}

static void
hot_reload_update_publish (MonoAssemblyLoadContext *alc, uint32_t generation)
{
	g_assert (update_published < generation && generation <= update_alloc_frontier);
	/* TODO: wait for all threads that are using old metadata to update. */
	hot_reload_update_published_invoke_hook (alc, generation);
	update_published = update_alloc_frontier;
	mono_memory_write_barrier ();
	publish_unlock ();
}

static void
hot_reload_update_cancel (uint32_t generation)
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
image_append_delta (MonoImage *base, BaselineInfo *base_info, MonoImage *delta, DeltaInfo *delta_info)
{
	if (!base_info->delta_image) {
		base_info->delta_image = base_info->delta_image_last = g_list_alloc ();
		base_info->delta_image->data = (gpointer)delta;
		mono_memory_write_barrier ();
		/* Have to set this here so that passes over the metadata in the updater thread start using the slow path */
		base->has_updates = TRUE;
		return;
	}
	g_assert (delta_info_lookup(((MonoImage*)base_info->delta_image_last->data))->generation < delta_info->generation);
	/* g_list_append returns the given list, not the newly appended */
	GList *l = g_list_append (base_info->delta_image_last, delta);
	g_assert (l != NULL && l->next != NULL && l->next->next == NULL);
	base_info->delta_image_last = l->next;

	mono_memory_write_barrier ();
	/* Have to set this here so that passes over the metadata in the updater thread start using the slow path */
	base->has_updates = TRUE;

}

/**
 * LOCKING: assumes the publish_lock is held
 */
MonoImage*
image_open_dmeta_from_data (MonoImage *base_image, uint32_t generation, gconstpointer dmeta_bytes, uint32_t dmeta_length)
{
	MonoImageOpenStatus status;
	MonoAssemblyLoadContext *alc = mono_image_get_alc (base_image);
	MonoImage *dmeta_image = mono_image_open_from_data_internal (alc, (char*)dmeta_bytes, dmeta_length, TRUE, &status, TRUE, NULL, NULL);
	g_assert (dmeta_image != NULL);
	g_assert (status == MONO_IMAGE_OK);

	return dmeta_image;
}

static gpointer
open_dil_data (MonoImage *base_image G_GNUC_UNUSED, gconstpointer dil_src, uint32_t dil_length)
{
	/* TODO: find a better memory manager.	But this way we at least won't lose the IL data. */
	MonoMemoryManager *mem_manager = (MonoMemoryManager *)mono_alc_get_default ()->memory_manager;

	gpointer dil_copy = mono_mem_manager_alloc (mem_manager, dil_length);
	memcpy (dil_copy, dil_src, dil_length);
	return dil_copy;
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
hot_reload_effective_table_slow (const MonoTableInfo **t, int *idx)
{
	/* FIXME: don't let any thread other than the updater thread see values from a delta image
	 * with a generation past update_published
	 */

	MonoImage *base;
	int tbl_index;
	if (!table_info_find_in_base (*t, &base, &tbl_index))
		return;
	BaselineInfo *info = baseline_info_lookup (base);
	if (!info)
		return;

	gboolean any_modified = info->any_modified_rows[tbl_index];

	if (G_LIKELY (*idx < table_info_get_rows (*t) && !any_modified))
		return;

	GList *list = info->delta_image;
	MonoImage *dmeta;
	int ridx;
	MonoTableInfo *table;
	int g = 0;

	/* Candidate: the last delta that had updates for the requested row */
	MonoImage *cand_dmeta = NULL;
	MonoTableInfo *cand_table = NULL;
	int cand_ridx = -1;
	int cand_g = 0;

	gboolean cont;
	do {
		g_assertf (list, "couldn't find idx=0x%08x in assembly=%s", *idx, dmeta && dmeta->name ? dmeta->name : "unknown image");
		dmeta = (MonoImage*)list->data;
		list = list->next;
		table = &dmeta->tables [tbl_index];
		int rel_row = hot_reload_relative_delta_index (dmeta, mono_metadata_make_token (tbl_index, *idx + 1));
		g_assert (rel_row == -1 || (rel_row > 0 && rel_row <= table_info_get_rows (table)));
		g++;
		if (rel_row != -1) {
			cand_dmeta = dmeta;
			cand_table = table;
			cand_ridx = rel_row - 1;
			cand_g = g;
		}
		ridx = rel_row - 1;
		if (!any_modified) {
			/* if the table only got additions, not modifications, don't continue after we find the first image that has the right number of rows */
			cont = ridx < 0 || ridx >= table_info_get_rows (table);
		} else {
			/* otherwise, keep going in case a later generation modified the row again */
			cont = list != NULL;
		}
	} while (cont);

	if (cand_ridx != -1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "effective table for %s: 0x%08x -> 0x%08x (rows = 0x%08x) (gen %d, g %d)", mono_meta_table_name (tbl_index), *idx, cand_ridx, table_info_get_rows (cand_table), metadata_update_local_generation (base, info, cand_dmeta), cand_g);

		*t = cand_table;
		*idx = cand_ridx;
	}
}

/*
 * The ENCMAP table contains the base of the relative offset.
 *
 * Returns -1 if the token does not resolve in this generation's ENCMAP.
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
hot_reload_relative_delta_index (MonoImage *image_dmeta, int token)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);

	/* this helper expects and returns as "index origin = 1" */
	g_assert (index > 0);

	if (!table_info_get_rows (encmap) || !image_dmeta->minimal_delta)
		return mono_metadata_token_index (token);

	DeltaInfo *delta_info = delta_info_lookup (image_dmeta);
	g_assert (delta_info);

	int index_map = delta_info->enc_recs [table];
	int encmap_rows = table_info_get_rows (encmap);

	/* if the table didn't have any updates in this generation and the
	 * table index is bigger than the last table that got updates,
	 * enc_recs will point past the last row */
	if (index_map - 1 == encmap_rows)
		return -1;
	guint32 cols[MONO_ENCMAP_SIZE];
	mono_metadata_decode_row (encmap, index_map - 1, cols, MONO_ENCMAP_SIZE);
	int map_entry = cols [MONO_ENCMAP_TOKEN];

	/* we're looking at the beginning of a sequence of encmap rows that are all the
	 * modifications+additions for the table we are looking for (or we're looking at an entry
	 * for the next table after the one we wanted).  the map entries will have tokens in
	 * increasing order.  skip over the rows where the tokens are not the one we want, until we
	 * hit the rows for the next table or we hit the end of the encmap */
	while (mono_metadata_token_table (map_entry) == table && mono_metadata_token_index (map_entry) < index && index_map < encmap_rows) {
		mono_metadata_decode_row (encmap, ++index_map - 1, cols, MONO_ENCMAP_SIZE);
		map_entry = cols [MONO_ENCMAP_TOKEN];
	}

	if (mono_metadata_token_table (map_entry) == table) {
		if (mono_metadata_token_index (map_entry) == index) {
			/* token resolves to this generation */
			int return_val = index_map - delta_info->enc_recs [table] + 1;
			g_assert (return_val > 0 && return_val <= table_info_get_rows (&image_dmeta->tables[table]));
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "relative index for token 0x%08x -> table 0x%02x row 0x%08x", token, table, return_val);
			return return_val;
		} else {
			/* Otherwise we stopped either: because we saw an an entry for a row after
			 * the one we wanted - we were looking for a modification, but the encmap
			 * has an addition; or, because we saw the last entry in the encmap and it
			 * still wasn't for a row as high as the one we wanted.  either way, the
			 * update we want is not in the delta we're looking at.
			 */
			g_assert ((mono_metadata_token_index (map_entry) > index) || (mono_metadata_token_index (map_entry) < index && index_map == encmap_rows));
			return -1;
		}
	} else {
		/* otherwise there are no more encmap entries for this table, and we didn't see the
		 * index, so there was no modification/addition for that index in this delta. */
		g_assert (mono_metadata_token_table (map_entry) > table);
		return -1;
	}
}

/* LOCKING: assumes publish_lock is held */
static DeltaInfo*
delta_info_init (MonoImage *image_dmeta, MonoImage *image_base, BaselineInfo *base_info, uint32_t generation)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];
	g_assert (!delta_info_lookup (image_dmeta));

	if (!table_info_get_rows (encmap))
		return NULL;

	DeltaInfo *delta_info = g_malloc0 (sizeof (DeltaInfo));

	delta_info->generation = generation;

	table_to_image_lock ();
	g_hash_table_insert (delta_image_to_info, image_dmeta, delta_info);
	table_to_image_unlock ();

	/* base_image takes ownership of 1 refcount ref of dmeta_image */
	image_append_delta (image_base, base_info, image_dmeta, delta_info);

	return delta_info;
}

/* LOCKING: assumes publish_lock is held */
static gboolean
delta_info_compute_table_records (MonoImage *image_dmeta, MonoImage *image_base, BaselineInfo *base_info, DeltaInfo *delta_info)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];

	int table, prev_table = -1, idx;

	/*** Compute logical table sizes ***/
	if (base_info->delta_image == base_info->delta_image_last) {
		/* this is the first update. */
		for (int i = 0; i < MONO_TABLE_NUM; ++i) {
			delta_info->count[i].prev_gen_rows = table_info_get_rows (&image_base->tables[i]);
		}
	} else {
		/* Current image_dmeta is image_base->delta_image_last->data,
		 * find its predecessor
		 */
		MonoImage *prev_delta = NULL;
		g_assert (base_info->delta_image_last->prev != NULL);
		prev_delta = (MonoImage*)base_info->delta_image_last->prev->data;
		DeltaInfo *prev_gen_info = delta_info_lookup (prev_delta);
		for (int i = 0; i < MONO_TABLE_NUM; ++i) {
			delta_info->count[i].prev_gen_rows = prev_gen_info->count[i].prev_gen_rows + prev_gen_info->count[i].inserted_rows;
		}
	}


	/* TODO: while going through the tables, update delta_info->count[tbl].{modified,inserted}_rows */

	int encmap_rows = table_info_get_rows (encmap);
	for (idx = 1; idx <= encmap_rows; ++idx) {
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
		if (rid < delta_info->count[table].prev_gen_rows) {
			base_info->any_modified_rows[table] = TRUE;
			delta_info->count[table].modified_rows++;
		} else
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

	return TRUE;
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
	int rows = table_info_get_rows (table_enclog);

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

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x (%s idx=0x%02x) (base table has 0x%04x rows)\tfunc=0x%02x\n", i, log_token, mono_meta_table_name (token_table), token_index, table_info_get_rows (&image_base->tables [token_table]), func_code);


		if (token_table != MONO_TABLE_METHOD)
			continue;

		if (token_index > table_info_get_rows (&image_base->tables [token_table])) {
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

		switch (token_table) {
		case MONO_TABLE_ASSEMBLYREF:
			/* okay, supported */
			break;
		case MONO_TABLE_METHOD:
			/* handled above */
			break;
		case MONO_TABLE_PROPERTY: {
			/* modifying a property, ok */
			if (token_index <= table_info_get_rows (&image_base->tables [token_table]))
				break;
			/* adding a property */
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x we do not support adding new properties.", i, log_token);
			mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support adding new properties. token=0x%08x", log_token);
			unsupported_edits = TRUE;
			continue;
		}
		case MONO_TABLE_METHODSEMANTICS: {
			/* FIXME: this should get the current table size, not the base stable size */
			if (token_index > table_info_get_rows (&image_base->tables [token_table])) {
				/* new rows are fine, as long as they point at existing methods */
				guint32 sema_cols [MONO_METHOD_SEMA_SIZE];
				int mapped_token = hot_reload_relative_delta_index (image_dmeta, mono_metadata_make_token (token_table, token_index));
				g_assert (mapped_token != -1);
				mono_metadata_decode_row (&image_dmeta->tables [MONO_TABLE_METHODSEMANTICS], mapped_token - 1, sema_cols, MONO_METHOD_SEMA_SIZE);

				switch (sema_cols [MONO_METHOD_SEMA_SEMANTICS]) {
				case METHOD_SEMANTIC_GETTER:
				case METHOD_SEMANTIC_SETTER: {
					int prop_method_index = sema_cols [MONO_METHOD_SEMA_METHOD];
					/* ok, if it's pointing to an existing getter/setter */
					if (prop_method_index < table_info_get_rows (&image_base->tables [MONO_TABLE_METHOD]))
						break;
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x adding new getter/setter method 0x%08x to a property is not supported", i, log_token, prop_method_index);
					mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support adding a new getter or setter to a property, token=0x%08x", log_token);
					unsupported_edits = TRUE;
					continue;
				}
				default:
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x adding new non-getter/setter property or event methods is not supported.", i, log_token);
					mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support adding new non-getter/setter property or event methods. token=0x%08x", log_token);
					unsupported_edits = TRUE;
					continue;
				}
			} else {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x we do not support patching of existing table cols.", i, log_token);
				mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support patching of existing table cols. token=0x%08x", log_token);
				unsupported_edits = TRUE;
				continue;
			}
		}
		case MONO_TABLE_CUSTOMATTRIBUTE: {
			/* FIXME: this should get the current table size, not the base stable size */
			if (token_index <= table_info_get_rows (&image_base->tables [token_table])) {
				/* modifying existing rows is ok, as long as the parent and ctor are the same */
				guint32 ca_upd_cols [MONO_CUSTOM_ATTR_SIZE];
				guint32 ca_base_cols [MONO_CUSTOM_ATTR_SIZE];
				int mapped_token = hot_reload_relative_delta_index (image_dmeta, mono_metadata_make_token (token_table, token_index));
				g_assert (mapped_token != -1);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x CUSTOM_ATTR update.  mapped index = 0x%08x\n", i, log_token, mapped_token);

				mono_metadata_decode_row (&image_dmeta->tables [MONO_TABLE_CUSTOMATTRIBUTE], mapped_token - 1, ca_upd_cols, MONO_CUSTOM_ATTR_SIZE);
				mono_metadata_decode_row (&image_base->tables [MONO_TABLE_CUSTOMATTRIBUTE], token_index - 1, ca_base_cols, MONO_CUSTOM_ATTR_SIZE);

				/* compare the ca_upd_cols [MONO_CUSTOM_ATTR_PARENT] to ca_base_cols [MONO_CUSTOM_ATTR_PARENT]. */
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x CUSTOM_ATTR update. Old Parent 0x%08x New Parent 0x%08x\n", i, log_token, ca_base_cols [MONO_CUSTOM_ATTR_PARENT], ca_upd_cols [MONO_CUSTOM_ATTR_PARENT]);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x CUSTOM_ATTR update. Old ctor 0x%08x New ctor 0x%08x\n", i, log_token, ca_base_cols [MONO_CUSTOM_ATTR_TYPE], ca_upd_cols [MONO_CUSTOM_ATTR_TYPE]);

				if (ca_base_cols [MONO_CUSTOM_ATTR_PARENT] != ca_upd_cols [MONO_CUSTOM_ATTR_PARENT] ||
				    ca_base_cols [MONO_CUSTOM_ATTR_TYPE] != ca_upd_cols [MONO_CUSTOM_ATTR_TYPE]) {
					mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support patching of existing CA table cols with a different Parent or Type. token=0x%08x", log_token);
					unsupported_edits = TRUE;
					continue;
				}
				break;
			} else  {
				/* Added a row. ok */
				break;
			}
		}
		default:
			/* FIXME: this bounds check is wrong for cumulative updates - need to look at the DeltaInfo:count.prev_gen_rows */
			if (token_index <= table_info_get_rows (&image_base->tables [token_table])) {
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
set_update_method (MonoImage *image_base, BaselineInfo *base_info, uint32_t generation, MonoImage *image_dmeta, DeltaInfo *delta_info, uint32_t token_index, const char* il_address, MonoDebugInformationEnc* pdb_address)
{
	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "setting method 0x%08x in g=%d IL=%p", token_index, generation, (void*)il_address);
	/* FIXME: this is a race if other threads are doing a lookup. */
	g_hash_table_insert (base_info->method_table_update, GUINT_TO_POINTER (token_index), GUINT_TO_POINTER (generation));
	g_hash_table_insert (delta_info->method_table_update, GUINT_TO_POINTER (token_index), (gpointer) il_address);
	g_hash_table_insert (delta_info->method_ppdb_table_update, GUINT_TO_POINTER (token_index), (gpointer) pdb_address);
}

static MonoDebugInformationEnc *
hot_reload_get_method_debug_information (MonoImage *image_dppdb, int idx)
{
	if (!image_dppdb)
		return NULL;
		
	MonoTableInfo *table_encmap = &image_dppdb->tables [MONO_TABLE_ENCMAP];
	int rows = table_info_get_rows (table_encmap);
	for (int i = 0; i < rows ; ++i) {
		guint32 cols [MONO_ENCMAP_SIZE];
		mono_metadata_decode_row (table_encmap, i, cols, MONO_ENCMAP_SIZE);
		int map_token = cols [MONO_ENCMAP_TOKEN];
		int token_table = mono_metadata_token_table (map_token);
		if (token_table == MONO_TABLE_METHODBODY) {
			int token_index = mono_metadata_token_index (map_token);
			if (token_index == idx)	{
				MonoDebugInformationEnc *encDebugInfo = g_new0 (MonoDebugInformationEnc, 1);
				encDebugInfo->idx = i;
				encDebugInfo->image = image_dppdb;				
				return encDebugInfo;
			}
		}
	}
	return NULL;
}

/* do actuall enclog application */
static gboolean
apply_enclog_pass2 (MonoImage *image_base, BaselineInfo *base_info, uint32_t generation, MonoImage *image_dmeta, MonoImage *image_dppdb, DeltaInfo *delta_info, gconstpointer dil_data, uint32_t dil_length, MonoError *error)
{
	MonoTableInfo *table_enclog = &image_dmeta->tables [MONO_TABLE_ENCLOG];
	int rows = table_info_get_rows (table_enclog);

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

		switch (token_table) {
		case MONO_TABLE_ASSEMBLYREF: {
			g_assert (token_index > table_info_get_rows (&image_base->tables [token_table]));

			if (assemblyref_updated)
				continue;

			assemblyref_updated = TRUE;

			/* FIXME: use DeltaInfo:prev_gen_rows instead of looping */
			/* TODO: do we know that there will never be modified rows in ASSEMBLYREF? */
			int old_rows = table_info_get_rows (&image_base->tables [MONO_TABLE_ASSEMBLYREF]);
			for (GList *l = base_info->delta_image; l; l = l->next) {
				MonoImage *delta_child = l->data;
				old_rows += table_info_get_rows (&delta_child->tables [MONO_TABLE_ASSEMBLYREF]);
			}
			int new_rows = table_info_get_rows (&image_dmeta->tables [MONO_TABLE_ASSEMBLYREF]);

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
			break;
		}
		case MONO_TABLE_METHOD: {
			if (token_index > table_info_get_rows (&image_base->tables [token_table])) {
				g_error ("EnC: new method added, should be caught by pass1");
			}

			if (!base_info->method_table_update)
				base_info->method_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);
			if (!delta_info->method_table_update)
				delta_info->method_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);
			if (!delta_info->method_ppdb_table_update)
			
				delta_info->method_ppdb_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);

			int mapped_token = hot_reload_relative_delta_index (image_dmeta, mono_metadata_make_token (token_table, token_index));
			int rva = mono_metadata_decode_row_col (&image_dmeta->tables [MONO_TABLE_METHOD], mapped_token - 1, MONO_METHOD_RVA);
			if (rva < dil_length) {
				char *il_address = ((char *) dil_data) + rva;
				MonoDebugInformationEnc *method_debug_information = hot_reload_get_method_debug_information (image_dppdb, token_index);
				set_update_method (image_base, base_info, generation, image_dmeta, delta_info, token_index, il_address, method_debug_information);
			} else {
				/* rva points probably into image_base IL stream. can this ever happen? */
				g_print ("TODO: this case is still a bit contrived. token=0x%08x with rva=0x%04x\n", log_token, rva);
			}
			break;
		}
		case MONO_TABLE_TYPEDEF: {
			/* TODO: throw? */
			/* TODO: happens changing the class (adding field or method). we ignore it, but dragons are here */

			/* existing entries are supposed to be patched */
			g_assert (token_index <= table_info_get_rows (&image_base->tables [token_table]));
			break;
		}
		case MONO_TABLE_PROPERTY: {
			/* allow updates to existing properties. */
			/* FIXME: use DeltaInfo:prev_gen_rows instead of image_base */
			g_assert (token_index <= table_info_get_rows (&image_base->tables [token_table]));
			/* assuming that property attributes and type haven't changed. */
			break;
		}
		case MONO_TABLE_CUSTOMATTRIBUTE: {
			/* ok, pass1 checked for disallowed modifications */
			break;
		}
		default: {
			g_assert (token_index > table_info_get_rows (&image_base->tables [token_table]));
			if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE))
				g_print ("todo: do something about this table index: 0x%02x\n", token_table);
		}
		}
	}
	return TRUE;
}

/**
 *
 * LOCKING: Takes the publish_lock
 */
void
hot_reload_apply_changes (int origin, MonoImage *image_base, gconstpointer dmeta_bytes, uint32_t dmeta_length, gconstpointer dil_bytes_orig, uint32_t dil_length, gconstpointer dpdb_bytes_orig, uint32_t dpdb_length, MonoError *error)
{
	if (!assembly_update_supported (image_base->assembly)) {
		mono_error_set_invalid_operation (error, "The assembly can not be edited or changed.");
		return;
	}

        static int first_origin = -1;

        if (first_origin < 0) {
                first_origin = origin;
        }

        if (first_origin != origin) {
                mono_error_set_not_supported (error, "Applying deltas through the debugger and System.Reflection.Metadata.MetadataUpdater.ApplyUpdate simultaneously is not supported");
                return;
        }

	const char *basename = image_base->filename;

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE)) {
		g_print ("LOADING basename=%s delta update.\ndelta image=%p & dil=%p\n", basename, dmeta_bytes, dil_bytes_orig);
                mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "delta image size 0x%08x, delta IL size 0x%08x\n", dmeta_length, dil_length);
#if 0
		mono_dump_mem (dmeta_bytes, dmeta_length);
		mono_dump_mem (dil_bytes_orig, dil_length);
#endif
	}

	uint32_t generation = hot_reload_update_prepare ();

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image string size: 0x%08x", image_base->heap_strings.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image user string size: 0x%08x", image_base->heap_us.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image blob heap addr: %p", image_base->heap_blob.data);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base image blob heap size: 0x%08x", image_base->heap_blob.size);

	/* makes a copy of dmeta_bytes */
	MonoImage *image_dmeta = image_open_dmeta_from_data (image_base, generation, dmeta_bytes, dmeta_length);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image string size: 0x%08x", image_dmeta->heap_strings.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image user string size: 0x%08x", image_dmeta->heap_us.size);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image blob heap addr: %p", image_dmeta->heap_blob.data);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta image blob heap size: 0x%08x", image_dmeta->heap_blob.size);
	g_assert (image_dmeta);

	/* makes a copy of dil_bytes_orig */
	gpointer dil_bytes = open_dil_data (image_base, dil_bytes_orig, dil_length);

	MonoImage *image_dpdb = NULL;
	if (dpdb_length > 0)
	{
		image_dpdb = image_open_dmeta_from_data (image_base, generation, dpdb_bytes_orig, dpdb_length);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image string size: 0x%08x", image_dpdb->heap_strings.size);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image user string size: 0x%08x", image_dpdb->heap_us.size);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image blob heap addr: %p", image_dpdb->heap_blob.data);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image blob heap size: 0x%08x", image_dpdb->heap_blob.size);
	}

	BaselineInfo *base_info = baseline_info_lookup_or_add (image_base);

	DeltaInfo *delta_info = delta_info_init (image_dmeta, image_base, base_info, generation);


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

	if (!table_info_get_rows (table_enclog) && !table_info_get_rows (table_encmap)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "No enclog or encmap in delta image for base=%s, nothing to do", basename);
		hot_reload_update_cancel (generation);
		return;
	}

	/* Process EnCMap and compute number of added/modified rows from this
	 * delta.  This enables computing row indexes relative to the delta.
	 * We use it in pass1 to bail out early if the EnCLog has unsupported
	 * edits.
	 */
	if (!delta_info_compute_table_records (image_dmeta, image_base, base_info, delta_info)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Error on computing delta table info (base=%s)", basename);
		hot_reload_update_cancel (generation);
		return;
	}


	if (!apply_enclog_pass1 (image_base, image_dmeta, dil_bytes, dil_length, error)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Error on sanity-checking delta image to base=%s, due to: %s", basename, mono_error_get_message (error));
		hot_reload_update_cancel (generation);
		return;
	}

	/* if there are updates, start tracking the tables of the base image, if we weren't already. */
	if (table_info_get_rows (table_enclog))
		table_to_image_add (image_base);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "base  guid: %s", image_base->guid);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "dmeta guid: %s", image_dmeta->guid);

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE))
		dump_update_summary (image_base, image_dmeta);

	if (!apply_enclog_pass2 (image_base, base_info, generation, image_dmeta, image_dpdb, delta_info, dil_bytes, dil_length, error)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Error applying delta image to base=%s, due to: %s", basename, mono_error_get_message (error));
		hot_reload_update_cancel (generation);
		return;
	}
	mono_error_assert_ok (error);

	MonoAssemblyLoadContext *alc = mono_image_get_alc (image_base);
	hot_reload_update_publish (alc, generation);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, ">>> EnC delta for base=%s (generation %d) applied", basename, generation);
}

/*
 * Returns how many times the base image was updated upto and including the given delta.
 */
static int
metadata_update_local_generation (MonoImage *base, BaselineInfo *base_info, MonoImage *delta)
{
	if (delta == base)
		return 0;
	int index = g_list_index (base_info->delta_image, delta);
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
	BaselineInfo *base_info = baseline_info_lookup (base);
	if (!base_info || !base_info->delta_image_last)
		return 0;
	else
		return metadata_update_local_generation (base, base_info, (MonoImage*)base_info->delta_image_last->data);
}

static gpointer
get_method_update_rva (MonoImage *image_base, BaselineInfo *base_info, uint32_t idx, gboolean is_pdb)
{
	gpointer loc = NULL;
	uint32_t cur = hot_reload_get_thread_generation ();
	int generation = -1;
	
	/* Go through all the updates that the current thread can see and see
	 * if they updated the method.	Keep the latest visible update */
	for (GList *ptr = base_info->delta_image; ptr != NULL; ptr = ptr->next) {
		MonoImage *image_delta = (MonoImage*) ptr->data;
		DeltaInfo *delta_info = delta_info_lookup (image_delta);
		g_assert (delta_info);
		if (delta_info->generation > cur)
			break;
		GHashTable *table = NULL;
		if (is_pdb)
			table = delta_info->method_ppdb_table_update;
		else
			table = delta_info->method_table_update;
		if (table) {
			gpointer result = g_hash_table_lookup (table, GUINT_TO_POINTER (idx));
			/* if it's not in the table of a later generation, the
			 * later generation didn't modify the method
			 */
			if (result != NULL) {
				loc = result;
				generation = delta_info->generation;
			}
		}
	}
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "method lookup idx=0x%08x returned gen=%d il=%p", idx, generation, loc);
	return loc;
}

gpointer 
hot_reload_get_updated_method_ppdb (MonoImage *base_image, uint32_t idx)
{
	BaselineInfo *info = baseline_info_lookup (base_image);
	if (!info)
		return NULL;
	gpointer loc = NULL;
	/* EnC case */
	if (G_UNLIKELY (info->method_table_update)) {
		uint32_t gen = GPOINTER_TO_UINT (g_hash_table_lookup (info->method_table_update, GUINT_TO_POINTER (idx)));
		if (G_UNLIKELY (gen > 0)) {
			loc = get_method_update_rva (base_image, info, idx, TRUE);
		}
	}
	return loc;
}

gpointer
hot_reload_get_updated_method_rva (MonoImage *base_image, uint32_t idx)
{
	BaselineInfo *info = baseline_info_lookup (base_image);
	if (!info)
		return NULL;
	gpointer loc = NULL;
	/* EnC case */
	if (G_UNLIKELY (info->method_table_update)) {
		uint32_t gen = GPOINTER_TO_UINT (g_hash_table_lookup (info->method_table_update, GUINT_TO_POINTER (idx)));
		if (G_UNLIKELY (gen > 0)) {
			loc = get_method_update_rva (base_image, info, idx, FALSE);
		}
	}
	return loc;
}


/* returns TRUE if token index is out of bounds */
gboolean
hot_reload_table_bounds_check (MonoImage *base_image, int table_index, int token_index)
{
	BaselineInfo *base_info = baseline_info_lookup (base_image);
	g_assert (base_info);

	GList *list = base_info->delta_image;
	MonoImage *dmeta;
	MonoTableInfo *table;
	/* result row, 0-based */
	int ridx;

	int original_token = mono_metadata_make_token (table_index, token_index);

	uint32_t exposed_gen = hot_reload_get_thread_generation ();
	do {
		if (!list)
			return TRUE;
		dmeta = list->data;
		DeltaInfo *delta_info = delta_info_lookup (dmeta);
		g_assert (delta_info);
		if (delta_info->generation > exposed_gen)
			return TRUE;
		list = list->next;
		table = &dmeta->tables [table_index];
		/* mono_image_relative_delta_index returns a 1-based index */
		ridx = hot_reload_relative_delta_index (dmeta, original_token) - 1;
	} while (ridx < 0 || ridx >= table_info_get_rows (table));

	return FALSE;
}

gboolean
hot_reload_delta_heap_lookup (MonoImage *base_image, MetadataHeapGetterFunc get_heap, uint32_t orig_index, MonoImage **image_out, uint32_t *index_out)
{
	g_assert (image_out);
	g_assert (index_out);
	MonoStreamHeader *heap = get_heap (base_image);
	g_assert (orig_index >= heap->size);
	BaselineInfo *base_info = baseline_info_lookup (base_image);
	g_assert (base_info);
	g_assert (base_info->delta_image);

	*image_out = base_image;
	*index_out = orig_index;

	guint32 prev_size = heap->size;

	uint32_t current_gen = hot_reload_get_thread_generation ();
	GList *cur;
	for (cur = base_info->delta_image; cur; cur = cur->next) {
		MonoImage *delta_image = (MonoImage*)cur->data;
		heap = get_heap (delta_image);

		*image_out = delta_image;

		DeltaInfo *delta_info = delta_info_lookup (delta_image);
		if (delta_info->generation > current_gen)
			return FALSE;

		/* FIXME: for non-minimal deltas we should just look in the last published image. */
		if (G_LIKELY (delta_image->minimal_delta))
			*index_out -= prev_size;
		if (*index_out < heap->size)
			break;
		prev_size = heap->size;
	}
	return (cur != NULL);
}

static gboolean
hot_reload_has_modified_rows (const MonoTableInfo *table)
{
	MonoImage *base;
	int tbl_index;
	if (!table_info_find_in_base (table, &base, &tbl_index))
	    return FALSE;
	BaselineInfo *info = baseline_info_lookup (base);
	if (!info)
		return FALSE;
	return info->any_modified_rows[tbl_index];
}

