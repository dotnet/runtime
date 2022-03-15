// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>

#include <config.h>
#include "mono/utils/mono-compiler.h"

#include "mono/component/hot_reload-internals.h"

#include <glib.h>
#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/mono-hash-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/metadata-update.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/tokentype.h"
#include "mono/utils/mono-coop-mutex.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-lazy-init.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"
#include "mono/metadata/debug-internals.h"
#include "mono/metadata/mono-debug.h"
#include "mono/metadata/debug-mono-ppdb.h"
#include "mono/utils/bsearch.h"


#include <mono/component/hot_reload.h>

#include <mono/utils/mono-compiler.h>

#define ALLOW_CLASS_ADD
#define ALLOW_METHOD_ADD
#define ALLOW_FIELD_ADD
#undef ALLOW_INSTANCE_FIELD_ADD

typedef struct _BaselineInfo BaselineInfo;
typedef struct _DeltaInfo DeltaInfo;

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
hot_reload_effective_table_slow (const MonoTableInfo **t, int idx);

static void
hot_reload_apply_changes (int origin, MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, gconstpointer dpdb_bytes_orig, uint32_t dpdb_length, MonoError *error);

static int
hot_reload_relative_delta_index (MonoImage *image_dmeta, DeltaInfo *delta_info, int token);

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

static int
hot_reload_table_num_rows_slow (MonoImage *image, int table_index);

static GSList*
hot_reload_get_added_members (MonoClass *klass);

static uint32_t
hot_reload_method_parent  (MonoImage *base, uint32_t method_token);

static void*
hot_reload_metadata_linear_search (MonoImage *base_image, MonoTableInfo *base_table, const void *key, BinarySearchComparer comparer);

static uint32_t
hot_reload_field_parent  (MonoImage *base, uint32_t field_token);

static uint32_t
hot_reload_get_field_idx (MonoClassField *field);

static MonoClassField *
hot_reload_get_field (MonoClass *klass, uint32_t fielddef_token);

static gpointer
hot_reload_get_static_field_addr (MonoClassField *field);

static MonoMethod *
hot_reload_find_method_by_name (MonoClass *klass, const char *name, int param_count, int flags, MonoError *error);

static MonoClassMetadataUpdateField *
metadata_update_field_setup_basic_info_and_resolve (MonoImage *image_base, BaselineInfo *base_info, uint32_t generation, DeltaInfo *delta_info, MonoClass *parent_klass, uint32_t fielddef_token, uint32_t field_flags, MonoError *error);

static MonoComponentHotReload fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &hot_reload_available },
	&hot_reload_set_fastpath_data,
	&hot_reload_update_enabled,
	&hot_reload_no_inline,
	&hot_reload_thread_expose_published,
	&hot_reload_get_thread_generation,
	&hot_reload_cleanup_on_close,
	&hot_reload_effective_table_slow,
	&hot_reload_apply_changes,
	&hot_reload_close_except_pools_all,
	&hot_reload_close_all,
	&hot_reload_get_updated_method_rva,
	&hot_reload_table_bounds_check,
	&hot_reload_delta_heap_lookup,
	&hot_reload_get_updated_method_ppdb,
	&hot_reload_has_modified_rows,
	&hot_reload_table_num_rows_slow,
	&hot_reload_method_parent,
	&hot_reload_metadata_linear_search,
	&hot_reload_field_parent,
	&hot_reload_get_field_idx,
	&hot_reload_get_field,
	&hot_reload_get_static_field_addr,
	&hot_reload_find_method_by_name,
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
struct _DeltaInfo {
	uint32_t generation; /* global update ID that added this delta image */
	MonoImage *delta_image; /* DeltaInfo doesn't own the image, the base MonoImage owns the reference */

	/* Maps MethodDef token indices to a pointer into the RVA of the delta IL */
	GHashTable *method_table_update;

	/* Maps MethodDef token indices to a pointer into the RVA of the delta PPDB */
	GHashTable *method_ppdb_table_update;

	// for each table, the row in the EncMap table that has the first token for remapping it?
	uint32_t enc_recs [MONO_TABLE_NUM];
	delta_row_count count [MONO_TABLE_NUM];

	MonoPPDBFile *ppdb_file;

	MonoMemPool *pool; /* mutated tables are allocated here */

	MonoTableInfo mutants[MONO_TABLE_NUM];
};


/* Additional informaiton for baseline MonoImages */
struct _BaselineInfo {
	/* List of DeltaInfos of deltas*/
	GList *delta_info;
	/* Tail of delta_info for fast appends */
	GList *delta_info_last;

	/* Maps MethodDef token indices to a boolean flag that there's an update for the method */
	GHashTable *method_table_update;

	/* TRUE if any published update modified an existing row */
	gboolean any_modified_rows [MONO_TABLE_NUM];

	/* A list of MonoClassMetadataUpdateInfo* that need to be cleaned up */
	GSList *klass_info;

	/* Parents for added methods, fields, etc */
	GHashTable *member_parent; /* maps added methoddef or fielddef tokens to typedef tokens */
};


#define DOTNET_MODIFIABLE_ASSEMBLIES "DOTNET_MODIFIABLE_ASSEMBLIES"

/* See Note: Suppressed Columns */
static guint16 m_SuppressedDeltaColumns [MONO_TABLE_NUM];

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
			modifiable
				= MONO_MODIFIABLE_ASSM_DEBUG;
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
/* Maps each delta MonoImage to its DeltaInfo. Doesn't own the DeltaInfo or the images */
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
klass_info_destroy (gpointer value, gpointer user_data G_GNUC_UNUSED)
{
	MonoClassMetadataUpdateInfo *info = (MonoClassMetadataUpdateInfo *)value;
	/* added_members is allocated from the class mempool, don't free it here */
	/* The MonoClassMetadataUpdateField is allocated from the class mempool, don't free it here */
	g_ptr_array_free (info->added_fields, TRUE);

	if (info->runtime.static_fields) {
		mono_g_hash_table_destroy (info->runtime.static_fields);
		info->runtime.static_fields = NULL;
	}

	mono_coop_mutex_destroy (&info->runtime.static_fields_lock);

	/* The MonoClassMetadataUpdateInfo itself is allocated from the class mempool, don't free it here */
}

static void
baseline_info_destroy (BaselineInfo *info)
{
	if (info->method_table_update)
		g_hash_table_destroy (info->method_table_update);

	if (info->klass_info) {
		g_slist_foreach (info->klass_info, klass_info_destroy, NULL);
		g_slist_free (info->klass_info);
	}
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
delta_info_init (MonoImage *image_dmeta, MonoImage *image_base, MonoPPDBFile *ppdb_file, BaselineInfo *base_info, uint32_t generation, DeltaInfo **prev_last_delta);

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
	mono_ppdb_close (dinfo->ppdb_file);

	if (dinfo->pool)
		mono_mempool_destroy (dinfo->pool);
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
	g_assert (!result || result->delta_image == delta_image);
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
	table_to_image_unlock ();
}

void
hot_reload_close_except_pools_all (MonoImage *base_image)
{
	BaselineInfo *info = baseline_info_lookup (base_image);
	if (!info)
		return;
	for (GList *ptr = info->delta_info; ptr; ptr = ptr->next) {
		DeltaInfo *info = (DeltaInfo *)ptr->data;
		MonoImage *image = info->delta_image;
		if (image) {
			table_to_image_lock ();
			g_hash_table_remove (delta_image_to_info, image);
			table_to_image_unlock ();
			/* if for some reason the image has other references, break the link to this delta_info that is going away */
			if (!mono_image_close_except_pools (image))
			    info->delta_image = NULL;
		}
	}
}

void
hot_reload_close_all (MonoImage *base_image)
{
	BaselineInfo *info = baseline_info_lookup (base_image);
	if (!info)
		return;
	for (GList *ptr = info->delta_info; ptr; ptr = ptr->next) {
		DeltaInfo *info = (DeltaInfo *)ptr->data;
		if (!info)
			continue;
		MonoImage *image = info->delta_image;
		if (image) {
			mono_image_close_finish (image);
		}
		delta_info_destroy (info);
		ptr->data = NULL;
	}
	g_list_free (info->delta_info);
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
		*tbl_index = (int)(((intptr_t) table - (intptr_t) base->tables) / s);
	}
	return TRUE;
}

static MonoImage*
image_open_dmeta_from_data (MonoImage *base_image, uint32_t generation, gconstpointer dmeta_bytes, uint32_t dmeta_length);

static DeltaInfo*
image_append_delta (MonoImage *base, BaselineInfo *base_info, MonoImage *delta, DeltaInfo *delta_info);

/* common method, don't use directly, use add_method_to_baseline, add_field_to_baseline, etc */
static void
add_member_to_baseline (BaselineInfo *base_info, DeltaInfo *delta_info, MonoClass *klass, uint32_t member_token);

static void
add_method_to_baseline (BaselineInfo *base_info, DeltaInfo *delta_info, MonoClass *klass, uint32_t method_token, MonoDebugInformationEnc* pdb_address);

static void
add_field_to_baseline (BaselineInfo *base_info, DeltaInfo *delta_info, MonoClass *klass, uint32_t field_token);

void
hot_reload_init (void)
{
	table_to_image_init ();
	mono_native_tls_alloc (&exposed_generation_id, NULL);

	/* See CMiniMdRW::ApplyDelta in metamodelenc.cpp in CoreCLR */
	m_SuppressedDeltaColumns[MONO_TABLE_EVENTMAP]      = (1 << MONO_EVENT_MAP_EVENTLIST);
        m_SuppressedDeltaColumns[MONO_TABLE_PROPERTYMAP]   = (1 << MONO_PROPERTY_MAP_PROPERTY_LIST);
        m_SuppressedDeltaColumns[MONO_TABLE_METHOD]        = (1 << MONO_METHOD_PARAMLIST);
        m_SuppressedDeltaColumns[MONO_TABLE_TYPEDEF]       = (1 << MONO_TYPEDEF_FIELD_LIST)|(1<<MONO_TYPEDEF_METHOD_LIST);
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

static void
add_class_info_to_baseline (MonoClass *klass, MonoClassMetadataUpdateInfo *klass_info)
{
	MonoImage *image = m_class_get_image (klass);
	BaselineInfo *baseline_info = baseline_info_lookup (image);
	baseline_info->klass_info = g_slist_prepend (baseline_info->klass_info, klass_info);
}

static MonoClassMetadataUpdateInfo *
mono_class_get_or_add_metadata_update_info (MonoClass *klass)
{
	MonoClassMetadataUpdateInfo *info = NULL;
	info = mono_class_get_metadata_update_info (klass);
	if (info)
		return info;
	mono_loader_lock ();
	info = mono_class_get_metadata_update_info (klass);
	if (!info) {
		info = mono_class_alloc0 (klass, sizeof (MonoClassMetadataUpdateInfo));
		add_class_info_to_baseline (klass, info);
		mono_class_set_metadata_update_info (klass, info);
	}
	mono_loader_unlock ();
	g_assert (info);
	return info;
}

/*
 * Given a baseline and an (optional) previous delta, allocate space for new tables for the current delta.
 *
 * Assumes the DeltaInfo:count info has already been calculated and initialized.
 */
static void
delta_info_initialize_mutants (const MonoImage *base, const BaselineInfo *base_info, const DeltaInfo *prev_delta, DeltaInfo *delta)
{
	g_assert (delta->pool);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Initializing mutant tables for image %p (generation %d)", base, delta->generation);
	for (int i = 0; i < MONO_TABLE_NUM; ++i)
	{
		gboolean need_copy = FALSE;
		/* if any generation modified any row of this table, make a copy for the current generation. */
		if (base_info->any_modified_rows [i])
			need_copy = TRUE;
		delta_row_count *count = &delta->count [i];
		guint32 base_rows = table_info_get_rows (&base->tables [i]);
		/* if some previous generation added rows, or we're adding rows, make a copy */
		if (base_rows != count->prev_gen_rows || count->inserted_rows)
			need_copy = TRUE;
		if (!need_copy) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, " Table 0x%02x unchanged (rows: base %d, prev %d, inserted %d), not copied", i, base_rows, count->prev_gen_rows, count->inserted_rows);
			continue;
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, " Table 0x%02x   changed (rows: base %d, prev %d, inserted %d),  IS copied", i, base_rows, count->prev_gen_rows, count->inserted_rows);
		}
		/* The invariant is that once we made a copy in any previous generation, we'll make
		 * a copy in this generation.  So subsequent generations can copy either from the
		 * immediately preceeding generation or from the baseline if the preceeding
		 * generation didn't make a copy. */

		guint32 rows = count->prev_gen_rows + count->inserted_rows;

		const MonoTableInfo *prev_table;
		if (!prev_delta || prev_delta->mutants [i].base == NULL)
			prev_table = &base->tables [i];
		else
			prev_table = &prev_delta->mutants [i];

		g_assert (prev_table != NULL);

		MonoTableInfo *tbl = &delta->mutants [i];
		if (prev_table->rows_ == 0) {
			/* table was empty in the baseline and it was empty in the prior generation, but now we have some rows. Use the format of the mutant table. */
			g_assert (prev_table->row_size == 0);
			tbl->row_size = delta->delta_image->tables [i].row_size;
			tbl->size_bitfield = delta->delta_image->tables [i].size_bitfield;
		} else {
			tbl->row_size = prev_table->row_size;
			tbl->size_bitfield = prev_table->size_bitfield;
		}
		tbl->rows_ = rows;
		g_assert (tbl->rows_ > 0 && tbl->row_size != 0);

		tbl->base = mono_mempool_alloc (delta->pool, tbl->row_size * rows);
		g_assert (table_info_get_rows (prev_table) == count->prev_gen_rows);

		/* copy the old rows  and zero out the new ones */
		memcpy ((char*)tbl->base, prev_table->base, count->prev_gen_rows * tbl->row_size);
		memset (((char*)tbl->base) + count->prev_gen_rows * tbl->row_size, 0, count->inserted_rows * tbl->row_size);
	}
}


/**
 * LOCKING: Assumes the publish_lock is held
 * Returns: The previous latest delta, or NULL if this is the first delta
 */
DeltaInfo *
image_append_delta (MonoImage *base, BaselineInfo *base_info, MonoImage *delta, DeltaInfo *delta_info)
{
	if (!base_info->delta_info) {
		base_info->delta_info = base_info->delta_info_last = g_list_alloc ();
		base_info->delta_info->data = (gpointer)delta_info;
		mono_memory_write_barrier ();
		/* Have to set this here so that passes over the metadata in the updater thread start using the slow path */
		base->has_updates = TRUE;
		return NULL;
	}
	DeltaInfo *prev_last_delta = (DeltaInfo*)base_info->delta_info_last->data;
	g_assert (prev_last_delta->generation < delta_info->generation);
	/* g_list_append returns the given list, not the newly appended */
	GList *l = g_list_append (base_info->delta_info_last, delta_info);
	g_assert (l != NULL && l->next != NULL && l->next->next == NULL);
	base_info->delta_info_last = l->next;

	mono_memory_write_barrier ();
	/* Have to set this here so that passes over the metadata in the updater thread start using the slow path */
	base->has_updates = TRUE;
	return prev_last_delta;
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

/*
 * Finds the latest mutated version of the table given by tbl_index
 *
 * On success returns TRUE, modifies *t and optionally updates *delta_out
 */
static gboolean
effective_table_mutant (MonoImage *base, BaselineInfo *info, int tbl_index, const MonoTableInfo **t, DeltaInfo **delta_out)
{
	GList *ptr =info->delta_info_last;
	uint32_t exposed_gen = hot_reload_get_thread_generation ();
	MonoImage *dmeta = NULL;
	DeltaInfo *delta_info = NULL;

	/* walk backward from the latest image until we find one that matches the current thread's exposed generation */
	do {
		delta_info = (DeltaInfo*)ptr->data;
		dmeta = delta_info->delta_image;
		if (delta_info->generation <= exposed_gen)
			break;
		ptr = ptr->prev;
	} while (ptr);
	if (!ptr)
		return FALSE;
	g_assert (dmeta != NULL);
	g_assert (delta_info != NULL);

	*t = &delta_info->mutants [tbl_index];
	if (delta_out)
		*delta_out = delta_info;
	return TRUE;
}

void
hot_reload_effective_table_slow (const MonoTableInfo **t, int idx)
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

	gboolean success = effective_table_mutant (base, info, tbl_index, t, NULL);

	g_assert (success);
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
hot_reload_relative_delta_index (MonoImage *image_dmeta, DeltaInfo *delta_info, int token)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];

	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);

	int index_map = delta_info->enc_recs [table];
	int encmap_rows = table_info_get_rows (encmap);

	if (!table_info_get_rows (encmap) || !image_dmeta->minimal_delta)
		return mono_metadata_token_index (token);

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
delta_info_init (MonoImage *image_dmeta, MonoImage *image_base, MonoPPDBFile *ppdb_file, BaselineInfo *base_info, uint32_t generation, DeltaInfo **prev_delta_info)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];
	g_assert (!delta_info_lookup (image_dmeta));

	if (!table_info_get_rows (encmap))
		return NULL;

	DeltaInfo *delta_info = g_malloc0 (sizeof (DeltaInfo));

	delta_info->generation = generation;
	delta_info->ppdb_file = ppdb_file;
	delta_info->delta_image = image_dmeta;

	table_to_image_lock ();
	g_hash_table_insert (delta_image_to_info, image_dmeta, delta_info);
	table_to_image_unlock ();

	delta_info->pool = mono_mempool_new ();


	g_assert (prev_delta_info);

	/* base_image takes ownership of 1 refcount ref of dmeta_image */
	*prev_delta_info = image_append_delta (image_base, base_info, image_dmeta, delta_info);

	return delta_info;
}

/* LOCKING: assumes publish_lock is held */
static gboolean
delta_info_compute_table_records (MonoImage *image_dmeta, MonoImage *image_base, BaselineInfo *base_info, DeltaInfo *delta_info)
{
	MonoTableInfo *encmap = &image_dmeta->tables [MONO_TABLE_ENCMAP];

	int table, prev_table = -1, idx;

	/*** Compute logical table sizes ***/
	if (base_info->delta_info == base_info->delta_info_last) {
		/* this is the first update. */
		for (int i = 0; i < MONO_TABLE_NUM; ++i) {
			delta_info->count[i].prev_gen_rows = table_info_get_rows (&image_base->tables[i]);
		}
	} else {
		g_assert (delta_info == (DeltaInfo*)base_info->delta_info_last->data);
		g_assert (base_info->delta_info_last->prev != NULL);
		DeltaInfo *prev_gen_info = (DeltaInfo*)base_info->delta_info_last->prev->data;
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
		if (rid <= delta_info->count[table].prev_gen_rows) {
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

enum MonoEnCFuncCode {
	ENC_FUNC_DEFAULT = 0,
	ENC_FUNC_ADD_METHOD = 1,
	ENC_FUNC_ADD_FIELD = 2,
	ENC_FUNC_ADD_PARAM = 3,
	ENC_FUNC_ADD_PROPERTY = 4,
	ENC_FUNC_ADD_EVENT = 5,
};

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

/*
 * Apply the row from the delta image given by log_token to the cur_delta mutated table.
 *
 */
static void
delta_info_mutate_row (MonoImage *image_dmeta, DeltaInfo *cur_delta, guint32 log_token)
{
	int token_table = mono_metadata_token_table (log_token);
	int token_index = mono_metadata_token_index (log_token); /* 1-based */

	gboolean modified = token_index <= cur_delta->count [token_table].prev_gen_rows;

	int delta_index = hot_reload_relative_delta_index (image_dmeta, cur_delta, log_token);

	/* The complication here is that we want the mutant table to look like the table in
	 * the baseline image with respect to column widths, but the delta tables are generally coming in
	 * uncompressed (4-byte columns).  So we have to copy one column at a time and adjust the
	 * widths as we go.
	 */

	guint32 dst_bitfield = cur_delta->mutants [token_table].size_bitfield;
	guint32 src_bitfield = image_dmeta->tables [token_table].size_bitfield;

	const char *src_base = image_dmeta->tables [token_table].base + (delta_index - 1) * image_dmeta->tables [token_table].row_size;
	char *dst_base = (char*)cur_delta->mutants [token_table].base + (token_index - 1) * cur_delta->mutants [token_table].row_size;

	guint32 src_offset = 0, dst_offset = 0;
	for (int col = 0; col < mono_metadata_table_count (dst_bitfield); ++col) {
		guint32 dst_col_size = mono_metadata_table_size (dst_bitfield, col);
		guint32 src_col_size = mono_metadata_table_size (src_bitfield, col);
		if ((m_SuppressedDeltaColumns [token_table] & (1 << col)) == 0) {
			const char *src = src_base + src_offset;
			char *dst = dst_base + dst_offset;

			/* copy src to dst, via a temporary to adjust for size differences */
			/* FIXME: unaligned access, endianness */
			guint32 tmp;

			switch (src_col_size) {
			case 1:
				tmp = *(guint8*)src;
				break;
			case 2:
				tmp = *(guint16*)src;
				break;
			case 4:
				tmp = *(guint32*)src;
				break;
			default:
				g_assert_not_reached ();
			}

			/* FIXME: unaligned access, endianness */
			switch (dst_col_size) {
			case 1:
				*(guint8*)dst = (guint8)tmp;
				break;
			case 2:
				*(guint16*)dst = (guint16)tmp;
				break;
			case 4:
				*(guint32*)dst = tmp;
				break;
			default:
				g_assert_not_reached ();
			}
		}
		src_offset += src_col_size;
		dst_offset += dst_col_size;
	}
	g_assert (dst_offset == cur_delta->mutants [token_table].row_size);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "mutate: table=0x%02x row=0x%04x delta row=0x%04x %s", token_table, token_index, delta_index, modified ? "Mod" : "Add");
}

static void
prepare_mutated_rows (const MonoTableInfo *table_enclog, MonoImage *image_base, MonoImage *image_dmeta, DeltaInfo *delta_info)
{
	int rows = table_info_get_rows (table_enclog);
	/* Prepare the mutated metadata tables */
	for (int i = 0; i < rows ; ++i) {
		guint32 cols [MONO_ENCLOG_SIZE];
		mono_metadata_decode_row (table_enclog, i, cols, MONO_ENCLOG_SIZE);

		int log_token = cols [MONO_ENCLOG_TOKEN];
		int func_code = cols [MONO_ENCLOG_FUNC_CODE];

		if (func_code != ENC_FUNC_DEFAULT)
			continue;

		delta_info_mutate_row (image_dmeta, delta_info, log_token);
	}
}

/* Run some sanity checks first. If we detect unsupported scenarios, this
 * function will fail and the metadata update should be aborted. This should
 * run before anything in the metadata world is updated. */
static gboolean
apply_enclog_pass1 (MonoImage *image_base, MonoImage *image_dmeta, DeltaInfo *delta_info, gconstpointer dil_data, uint32_t dil_length, MonoError *error)
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

		gboolean is_addition = token_index-1 >= delta_info->count[token_table].prev_gen_rows ;


		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x (%s idx=0x%02x) (base table has 0x%04x rows; prev gen had 0x%04x rows)\t%s\tfunc=0x%02x (\"%s\")\n", i, log_token, mono_meta_table_name (token_table), token_index, table_info_get_rows (&image_base->tables [token_table]), delta_info->count[token_table].prev_gen_rows, (is_addition ? "ADD" : "UPDATE"), func_code, funccode_to_str (func_code));


		if (token_table != MONO_TABLE_METHOD)
			continue;

#ifndef ALLOW_METHOD_ADD

		if (is_addition) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "\tcannot add new method with token 0x%08x", log_token);
			mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: cannot add new method with token 0x%08x", log_token);
			unsupported_edits = TRUE;
		}

#endif

#ifdef ALLOW_METHOD_ADD
		/* adding a new parameter to a new method is ok */
		if (func_code == ENC_FUNC_ADD_PARAM && is_addition)
			continue;
#endif

		g_assert (func_code == 0); /* anything else doesn't make sense here */
	}

	for (int i = 0; i < rows ; ++i) {
		guint32 cols [MONO_ENCLOG_SIZE];
		mono_metadata_decode_row (table_enclog, i, cols, MONO_ENCLOG_SIZE);

		int log_token = cols [MONO_ENCLOG_TOKEN];
		int func_code = cols [MONO_ENCLOG_FUNC_CODE];

		int token_table = mono_metadata_token_table (log_token);
		int token_index = mono_metadata_token_index (log_token);

		gboolean is_addition = token_index-1 >= delta_info->count[token_table].prev_gen_rows ;

		switch (token_table) {
		case MONO_TABLE_ASSEMBLYREF:
			/* okay, supported */
			break;
		case MONO_TABLE_METHOD:
#ifdef ALLOW_METHOD_ADD
			if (func_code == ENC_FUNC_ADD_PARAM)
				continue; /* ok, allowed */
#endif
			/* handled above */
			break;
		case MONO_TABLE_FIELD:
#ifdef ALLOW_FIELD_ADD
			if (func_code == ENC_FUNC_DEFAULT)
				continue; /* ok, allowed */
#else
			/* adding or modifying a field */
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x we do not support adding or modifying fields.", i, log_token);
			mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support adding or modifying fields. token=0x%08x", log_token);
			unsupported_edits = TRUE;
			break;
#endif
		case MONO_TABLE_PROPERTY: {
			/* modifying a property, ok */
			if (!is_addition)
				break;
			/* adding a property */
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x we do not support adding new properties.", i, log_token);
			mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support adding new properties. token=0x%08x", log_token);
			unsupported_edits = TRUE;
			continue;
		}
		case MONO_TABLE_METHODSEMANTICS: {
			if (is_addition) {
				/* new rows are fine, as long as they point at existing methods */
				guint32 sema_cols [MONO_METHOD_SEMA_SIZE];
				int mapped_token = hot_reload_relative_delta_index (image_dmeta, delta_info, mono_metadata_make_token (token_table, token_index));
				g_assert (mapped_token != -1);
				mono_metadata_decode_row (&image_dmeta->tables [MONO_TABLE_METHODSEMANTICS], mapped_token - 1, sema_cols, MONO_METHOD_SEMA_SIZE);

				switch (sema_cols [MONO_METHOD_SEMA_SEMANTICS]) {
				case METHOD_SEMANTIC_GETTER:
				case METHOD_SEMANTIC_SETTER: {
					int prop_method_index = sema_cols [MONO_METHOD_SEMA_METHOD];
					/* ok, if it's pointing to an existing getter/setter */
					gboolean is_prop_method_add = prop_method_index-1 >= delta_info->count[MONO_TABLE_METHOD].prev_gen_rows;
					if (!is_prop_method_add)
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
			if (!is_addition) {
				/* modifying existing rows is ok, as long as the parent and ctor are the same */
				guint32 ca_upd_cols [MONO_CUSTOM_ATTR_SIZE];
				guint32 ca_base_cols [MONO_CUSTOM_ATTR_SIZE];
				int mapped_token = hot_reload_relative_delta_index (image_dmeta, delta_info, mono_metadata_make_token (token_table, token_index));
				g_assert (mapped_token != -1);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x CUSTOM_ATTR update.  mapped index = 0x%08x\n", i, log_token, mapped_token);

				mono_metadata_decode_row (&image_dmeta->tables [MONO_TABLE_CUSTOMATTRIBUTE], mapped_token - 1, ca_upd_cols, MONO_CUSTOM_ATTR_SIZE);
				mono_metadata_decode_row (&image_base->tables [MONO_TABLE_CUSTOMATTRIBUTE], token_index - 1, ca_base_cols, MONO_CUSTOM_ATTR_SIZE);

				/* compare the ca_upd_cols [MONO_CUSTOM_ATTR_PARENT] to ca_base_cols [MONO_CUSTOM_ATTR_PARENT]. */
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x CUSTOM_ATTR update. Old Parent 0x%08x New Parent 0x%08x\n", i, log_token, ca_base_cols [MONO_CUSTOM_ATTR_PARENT], ca_upd_cols [MONO_CUSTOM_ATTR_PARENT]);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x CUSTOM_ATTR update. Old ctor 0x%08x New ctor 0x%08x\n", i, log_token, ca_base_cols [MONO_CUSTOM_ATTR_TYPE], ca_upd_cols [MONO_CUSTOM_ATTR_TYPE]);

				/* TODO: when we support the ChangeCustomAttribute capability, the
				 * parent might become 0 to delete attributes.  It may also be the
				 * case that the MONO_CUSTOM_ATTR_TYPE will change.  Without that
				 * capability, we trust that if the TYPE is not the same token, it
				 * still resolves to the same MonoMethod* (but we can't check it in
				 * pass1 because we haven't added the new AssemblyRefs yet.
				 */
				/* NOTE: Apparently Roslyn sometimes sends NullableContextAttribute
				 * deletions even if the ChangeCustomAttribute capability is unset.
				 * So tacitly accept updates where a custom attribute is deleted
				 * (its parent is set to 0).  Once we support custom attribute
				 * changes, we will support this kind of deletion for real.
				 */
				if (ca_base_cols [MONO_CUSTOM_ATTR_PARENT] != ca_upd_cols [MONO_CUSTOM_ATTR_PARENT] && ca_upd_cols [MONO_CUSTOM_ATTR_PARENT] != 0) {
					mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support patching of existing CA table cols with a different Parent. token=0x%08x", log_token);
					unsupported_edits = TRUE;
					continue;
				}
				break;
			} else  {
				/* Added a row. ok */
				break;
			}
		}
		case MONO_TABLE_PARAM: {
			if (!is_addition) {
				/* We only allow modifications where the parameter name doesn't change. */
				uint32_t base_param [MONO_PARAM_SIZE];
				uint32_t upd_param [MONO_PARAM_SIZE];
				int mapped_token = hot_reload_relative_delta_index (image_dmeta, delta_info, mono_metadata_make_token (token_table, token_index));
				g_assert (mapped_token != -1);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x PARAM update.  mapped index = 0x%08x\n", i, log_token, mapped_token);

				mono_metadata_decode_row (&image_dmeta->tables [MONO_TABLE_PARAM], mapped_token - 1, upd_param, MONO_PARAM_SIZE);
				mono_metadata_decode_row (&image_base->tables [MONO_TABLE_PARAM], token_index - 1, base_param, MONO_PARAM_SIZE);

				const char *base_name = mono_metadata_string_heap (image_base, base_param [MONO_PARAM_NAME]);
				const char *upd_name = mono_metadata_string_heap (image_base, upd_param [MONO_PARAM_NAME]);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x: 0x%08x PARAM update: seq = %d (base = %d), name = '%s' (base = '%s')\n", i, log_token, upd_param [MONO_PARAM_SEQUENCE], base_param [MONO_PARAM_SEQUENCE], upd_name, base_name);
				if (strcmp (base_name, upd_name) != 0 || base_param [MONO_PARAM_SEQUENCE] != upd_param [MONO_PARAM_SEQUENCE]) {
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x we do not support patching of existing PARAM table cols.", i, log_token);
					mono_error_set_type_load_name (error, NULL, image_base->name, "EnC: we do not support patching of existing PARAM table cols. token=0x%08x", log_token);
					unsupported_edits = TRUE;
					continue;
				}
				break;
			} else
				break; /* added a row. ok */
		}
		case MONO_TABLE_TYPEDEF: {
			gboolean new_class G_GNUC_UNUSED = is_addition;
#ifdef ALLOW_METHOD_ADD
			/* only allow adding methods to existing classes for now */
			if (
#ifndef ALLOW_CLASS_ADD
				!new_class &&
#endif
				func_code == ENC_FUNC_ADD_METHOD) {
				/* next record should be a MONO_TABLE_METHOD addition (func == default) */
				g_assert (i + 1 < rows);
				guint32 next_cols [MONO_ENCLOG_SIZE];
				mono_metadata_decode_row (table_enclog, i + 1, next_cols, MONO_ENCLOG_SIZE);
				g_assert (next_cols [MONO_ENCLOG_FUNC_CODE] == ENC_FUNC_DEFAULT);
				int next_token = next_cols [MONO_ENCLOG_TOKEN];
				int next_table = mono_metadata_token_table (next_token);
				int next_index = mono_metadata_token_index (next_token);
				g_assert (next_table == MONO_TABLE_METHOD);
				/* expecting an added method */
				g_assert (next_index-1 >= delta_info->count[next_table].prev_gen_rows);
				i++; /* skip the next record */
				continue;
			}
#endif
#ifdef ALLOW_FIELD_ADD
			if (
#ifndef ALLOW_CLASS_ADD
				!new_class &&
#endif
				func_code == ENC_FUNC_ADD_FIELD) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "row[0x%02x]:0x%08x AddField to klass 0x%08x, skipping next EnClog record", i, log_token, token_index);
				g_assert (i + 1 < rows);
				guint32 next_cols [MONO_ENCLOG_SIZE];
				mono_metadata_decode_row (table_enclog, i + 1, next_cols, MONO_ENCLOG_SIZE);
				g_assert (next_cols [MONO_ENCLOG_FUNC_CODE] == ENC_FUNC_DEFAULT);
				int next_token = next_cols [MONO_ENCLOG_TOKEN];
				int next_table = mono_metadata_token_table (next_token);
				int next_index = mono_metadata_token_index (next_token);
				g_assert (next_table == MONO_TABLE_FIELD);
				/* expecting an added field */
				g_assert (next_index-1 >= delta_info->count[next_table].prev_gen_rows);
				i++; /* skip the next record */
				continue;
			}
#endif
			/* fallthru */
		}
		default:
			if (!is_addition) {
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
			case ENC_FUNC_DEFAULT: /* default */
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
set_delta_method_debug_info (DeltaInfo *delta_info, uint32_t token_index, MonoDebugInformationEnc *pdb_address)
{
	g_hash_table_insert (delta_info->method_ppdb_table_update, GUINT_TO_POINTER (token_index), (gpointer) pdb_address);
}

static void
set_update_method (MonoImage *image_base, BaselineInfo *base_info, uint32_t generation, MonoImage *image_dmeta, DeltaInfo *delta_info, uint32_t token_index, const char* il_address, MonoDebugInformationEnc* pdb_address)
{
	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "setting method 0x%08x in g=%d IL=%p", token_index, generation, (void*)il_address);
	/* FIXME: this is a race if other threads are doing a lookup. */
	g_hash_table_insert (base_info->method_table_update, GUINT_TO_POINTER (token_index), GUINT_TO_POINTER (generation));
	g_hash_table_insert (delta_info->method_table_update, GUINT_TO_POINTER (token_index), (gpointer) il_address);
	set_delta_method_debug_info (delta_info, token_index, pdb_address);
}

static MonoDebugInformationEnc *
hot_reload_get_method_debug_information (MonoPPDBFile *ppdb_file, int idx)
{
	if (!ppdb_file)
		return NULL;

	MonoImage *image_dppdb = ppdb_file->image;
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
				encDebugInfo->idx = i + 1;
				encDebugInfo->ppdb_file = ppdb_file;
				return encDebugInfo;
			}
		}
	}
	return NULL;
}

static void G_GNUC_UNUSED
dump_assembly_ref_names (MonoImage *image)
{
	if (!mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE))
		return;
	for (int i = 0; i < image->nreferences; ++i) {
		ERROR_DECL(local_error);
		MonoAssemblyName aname;
		mono_assembly_get_assemblyref_checked (image, i, &aname, local_error);

		if (is_ok (local_error))
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Reference[%02d] = '%s'", i, aname.name);
		else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Reference[%02d] error '%s'", i, mono_error_get_message (local_error));
			mono_error_cleanup (local_error);
		}
	}
}


/* do actuall enclog application */
static gboolean
apply_enclog_pass2 (MonoImage *image_base, BaselineInfo *base_info, uint32_t generation, MonoImage *image_dmeta, DeltaInfo *delta_info, gconstpointer dil_data, uint32_t dil_length, MonoError *error)
{
	MonoTableInfo *table_enclog = &image_dmeta->tables [MONO_TABLE_ENCLOG];
	int rows = table_info_get_rows (table_enclog);

	/* NOTE: Suppressed colums
	 *
	 * Certain column values in some tables in the deltas are not meant to be applied over the
	 * previous generation. See CMiniMdRW::m_SuppressedDeltaColumns in CoreCLR.  For example the
	 * MONO_METHOD_PARAMLIST column in MONO_TABLE_METHOD is always 0 in an update - for modified
	 * rows the previous value must be carried over. For added rows, it is supposed to be
	 * initialized to the end of the param table and updated with the "Param create" func code
	 * in subsequent EnCLog records.
	 *
	 * For mono's immutable model (where we don't change the baseline image data), we will need
	 * to mutate the delta image tables to incorporate the suppressed column values from the
	 * previous generation.
	 *
	 * For Baseline capabilities, the only suppressed column is MONO_METHOD_PARAMLIST - which we
	 * can ignore because we don't do anything with param updates and the only column we care
	 * about is MONO_METHOD_RVA which gets special case treatment with set_update_method().
	 *
	 * But when we implement additional capabilities (for example UpdateParameters), we will
	 * need to start mutating the delta image tables to pick up the suppressed column values.
	 * Fortunately whether we get the delta from the debugger or from the runtime API, we always
	 * have it in writable memory (and not mmap-ed pages), so we can rewrite the table values.
	 */

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Pass 2 begin: base '%s' delta image=%p", image_base->name, image_dmeta);

#if defined(ALLOW_METHOD_ADD) || defined(ALLOW_FIELD_ADD)
	MonoClass *add_member_klass = NULL;
#endif

	gboolean assemblyref_updated = FALSE;
	for (int i = 0; i < rows ; ++i) {
		guint32 cols [MONO_ENCLOG_SIZE];
		mono_metadata_decode_row (table_enclog, i, cols, MONO_ENCLOG_SIZE);

		int log_token = cols [MONO_ENCLOG_TOKEN];
		int func_code = cols [MONO_ENCLOG_FUNC_CODE];

		int token_table = mono_metadata_token_table (log_token);
		int token_index = mono_metadata_token_index (log_token);

		gboolean is_addition = token_index-1 >= delta_info->count[token_table].prev_gen_rows ;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "enclog i=%d: token=0x%08x (table=%s): %d:\t%s", i, log_token, mono_meta_table_name (token_table), func_code, (is_addition ? "ADD" : "UPDATE"));


		/* TODO: See CMiniMdRW::ApplyDelta for how to drive this.
		 */
		switch (func_code) {
		case ENC_FUNC_DEFAULT: /* default */
			break;
#ifdef ALLOW_METHOD_ADD
		case ENC_FUNC_ADD_METHOD: {
			g_assert (token_table == MONO_TABLE_TYPEDEF);
			MonoClass *klass = mono_class_get_checked (image_base, log_token, error);
			if (!is_ok (error)) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Can't get class with token 0x%08x due to: %s", log_token, mono_error_get_message (error));
				return FALSE;
			}
			add_member_klass = klass;
			break;
		}

		case ENC_FUNC_ADD_PARAM: {
			g_assert (token_table == MONO_TABLE_METHOD);
			break;
		}
#endif
#ifdef ALLOW_FIELD_ADD
		case ENC_FUNC_ADD_FIELD: {
			g_assert (token_table == MONO_TABLE_TYPEDEF);
			MonoClass *klass = mono_class_get_checked (image_base, log_token, error);
			if (!is_ok (error)) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Can't get class with token 0x%08x due to: %s", log_token, mono_error_get_message (error));
				return FALSE;
			}
			add_member_klass = klass;
			break;
		}
#endif
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
			for (GList *l = base_info->delta_info; l; l = l->next) {
				MonoImage *delta_child = ((DeltaInfo*)l->data)->delta_image;
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

#if 0
			dump_assembly_ref_names (image_base);
#endif

			g_free (old_array);
			break;
		}
		case MONO_TABLE_METHOD: {
#ifdef ALLOW_METHOD_ADD
			/* if adding a param, handle it with the next record */
			if (func_code == ENC_FUNC_ADD_PARAM)
				break;

			if (is_addition) {
				if (!add_member_klass)
					g_error ("EnC: new method added but I don't know the class, should be caught by pass1");
				g_assert (add_member_klass);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Adding new method 0x%08x to class %s.%s", log_token, m_class_get_name_space (add_member_klass), m_class_get_name (add_member_klass));
				MonoDebugInformationEnc *method_debug_information = hot_reload_get_method_debug_information (delta_info->ppdb_file, token_index);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Debug info for method 0x%08x has ppdb idx 0x%08x", log_token, method_debug_information ? method_debug_information->idx : 0);
				add_method_to_baseline (base_info, delta_info, add_member_klass, log_token, method_debug_information);
				add_member_klass = NULL;
			}
#endif

			if (!base_info->method_table_update)
				base_info->method_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);
			if (!delta_info->method_table_update)
				delta_info->method_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);
			if (!delta_info->method_ppdb_table_update)

				delta_info->method_ppdb_table_update = g_hash_table_new (g_direct_hash, g_direct_equal);

			int mapped_token = hot_reload_relative_delta_index (image_dmeta, delta_info, mono_metadata_make_token (token_table, token_index));
			int rva = mono_metadata_decode_row_col (&image_dmeta->tables [MONO_TABLE_METHOD], mapped_token - 1, MONO_METHOD_RVA);
			if (rva < dil_length) {
				char *il_address = ((char *) dil_data) + rva;
				MonoDebugInformationEnc *method_debug_information = hot_reload_get_method_debug_information (delta_info->ppdb_file, token_index);
				set_update_method (image_base, base_info, generation, image_dmeta, delta_info, token_index, il_address, method_debug_information);
			} else {
				/* rva points probably into image_base IL stream. can this ever happen? */
				g_print ("TODO: this case is still a bit contrived. token=0x%08x with rva=0x%04x\n", log_token, rva);
			}
#if defined(ALLOW_METHOD_ADD) || defined(ALLOW_FIELD_ADD)
			add_member_klass = NULL;
#endif
			break;
		}
		case MONO_TABLE_FIELD: {
#ifdef ALLOW_FIELD_ADD
			g_assert (is_addition);
			g_assert (add_member_klass);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Adding new field 0x%08x to class %s.%s", log_token, m_class_get_name_space (add_member_klass), m_class_get_name (add_member_klass));

			uint32_t mapped_token = hot_reload_relative_delta_index (image_dmeta, delta_info, log_token);
			uint32_t field_flags = mono_metadata_decode_row_col (&image_dmeta->tables [MONO_TABLE_FIELD], mapped_token - 1, MONO_FIELD_FLAGS);

#ifndef ALLOW_INSTANCE_FIELD_ADD
			if ((field_flags & FIELD_ATTRIBUTE_STATIC) == 0) {
				/* TODO: implement instance (and literal?) fields */
				mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_METADATA_UPDATE, "Adding non-static fields isn't implemented yet (token 0x%08x, class %s.%s)", log_token, m_class_get_name_space (add_member_klass), m_class_get_name (add_member_klass));
				mono_error_set_not_implemented (error, "Adding non-static fields isn't implemented yet (token 0x%08x, class %s.%s)", log_token, m_class_get_name_space (add_member_klass), m_class_get_name (add_member_klass));
				return FALSE;
			}
#endif

			add_field_to_baseline (base_info, delta_info, add_member_klass, log_token);

			/* This actually does more than mono_class_setup_basic_field_info and
			 * resolves MonoClassField:type and sets MonoClassField:offset to -1 to make
			 * it easier to spot that the field is special.
			 */
			metadata_update_field_setup_basic_info_and_resolve (image_base, base_info, generation, delta_info, add_member_klass, log_token, field_flags, error);
			if (!is_ok (error)) {
				mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_METADATA_UPDATE, "Could not setup field (token 0x%08x) due to: %s", log_token, mono_error_get_message (error));
				return FALSE;
			}

			add_member_klass = NULL;
#else
			g_assert_not_reached ();
#endif
			break;
		}
		case MONO_TABLE_TYPEDEF: {
#ifdef ALLOW_CLASS_ADD
			if (is_addition) {
				/* Adding a new class. ok */
				switch (func_code) {
				case ENC_FUNC_DEFAULT:
					/* ok, added a new class */
					/* TODO: do things here */
					break;
				case ENC_FUNC_ADD_METHOD:
				case ENC_FUNC_ADD_FIELD:
					/* ok, adding a new field or method to a new class */
					/* TODO: do we need to do anything special?  Conceptually
					 * this is the same as modifying an existing class -
					 * especially since from the next generation's point of view
					 * that's what adding a field/method will be. */
					break;
				case ENC_FUNC_ADD_PROPERTY:
				case ENC_FUNC_ADD_EVENT:
					g_assert_not_reached (); /* FIXME: implement me */
				default:
					g_assert_not_reached (); /* unknown func_code */
				}
				break;
			}
#endif
			/* modifying an existing class by adding a method or field, etc. */
			g_assert (!is_addition);
#if !defined(ALLOW_METHOD_ADD) && !defined(ALLOW_FIELD_ADD)
			g_assert_not_reached ();
#else
			g_assert (func_code != ENC_FUNC_DEFAULT);
#endif
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
		case MONO_TABLE_PARAM: {
			/* ok, pass1 checked for disallowed modifications */
			/* ALLOW_METHOD_ADD: FIXME: here we would really like to update the method's paramlist column to point to the new params. */
			/* if there were multiple added methods, this comes in as several method
			 * additions, followed by the parameter additions.
			 *
			 * 10: 0x02000002 (TypeDef)          0x00000001 (AddMethod)
			 * 11: 0x06000006 (MethodDef)        0
			 * 12: 0x02000002 (TypeDef)          0x00000001 (AddMethod)
			 * 13: 0x06000007 (MethodDef)        0
			 * 14: 0x06000006 (MethodDef)        0x00000003 (AddParameter)
			 * 15: 0x08000003 (Param)            0
			 * 16: 0x06000006 (MethodDef)        0x00000003 (AddParameter)
			 * 17: 0x08000004 (Param)            0
			 * 18: 0x06000007 (MethodDef)        0x00000003 (AddParameter)
			 * 19: 0x08000005 (Param)            0
			 *
			 * So by the time we see the param additions, the methods are already in.
			 *
			 * FIXME: we need a lookaside table (like member_parent) for every place
			 * that looks at MONO_METHOD_PARAMLIST
			 */
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

static void
dump_methodbody (MonoImage *image)
{
	if (!mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE))
		return;
	MonoTableInfo *t = &image->tables [MONO_TABLE_METHODBODY];
	uint32_t rows = table_info_get_rows (t);
	for (uint32_t i = 0; i < rows; ++i)
	{
		uint32_t cols[MONO_METHODBODY_SIZE];
		mono_metadata_decode_row (t, i, cols, MONO_METHODBODY_SIZE);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, " row[%02d] = doc: 0x%08x seq: 0x%08x", i + 1, cols [MONO_METHODBODY_DOCUMENT], cols [MONO_METHODBODY_SEQ_POINTS]);
	}
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
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "delta IL bytes copied to addr=%p", dil_bytes);

	MonoPPDBFile *ppdb_file = NULL;
	if (dpdb_length > 0)
	{
		MonoImage *image_dpdb = image_open_dmeta_from_data (image_base, generation, dpdb_bytes_orig, dpdb_length);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image string size: 0x%08x", image_dpdb->heap_strings.size);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image user string size: 0x%08x", image_dpdb->heap_us.size);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image blob heap addr: %p", image_dpdb->heap_blob.data);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "pdb image blob heap size: 0x%08x", image_dpdb->heap_blob.size);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "ppdb methodbody: ");
		dump_methodbody (image_dpdb);
		ppdb_file = mono_create_ppdb_file (image_dpdb, FALSE);
		g_assert (ppdb_file->image == image_dpdb);
	}

	BaselineInfo *base_info = baseline_info_lookup_or_add (image_base);

	DeltaInfo *prev_delta_info = NULL;
	DeltaInfo *delta_info = delta_info_init (image_dmeta, image_base, ppdb_file, base_info, generation, &prev_delta_info);


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

	delta_info_initialize_mutants (image_base, base_info, prev_delta_info, delta_info);

	prepare_mutated_rows (table_enclog, image_base, image_dmeta, delta_info);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Populated mutated tables for delta image %p", image_dmeta);


	if (!apply_enclog_pass1 (image_base, image_dmeta, delta_info, dil_bytes, dil_length, error)) {
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

	if (!apply_enclog_pass2 (image_base, base_info, generation, image_dmeta, delta_info, dil_bytes, dil_length, error)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Error applying delta image to base=%s, due to: %s", basename, mono_error_get_message (error));
		hot_reload_update_cancel (generation);
		return;
	}
	mono_error_assert_ok (error);

	MonoAssemblyLoadContext *alc = mono_image_get_alc (image_base);
	hot_reload_update_publish (alc, generation);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, ">>> EnC delta for base=%s (generation %d) applied", basename, generation);
}

static gpointer
get_method_update_rva (MonoImage *image_base, BaselineInfo *base_info, uint32_t idx, gboolean is_pdb)
{
	gpointer loc = NULL;
	uint32_t cur = hot_reload_get_thread_generation ();
	int generation = -1;

	/* Go through all the updates that the current thread can see and see
	 * if they updated the method.	Keep the latest visible update */
	for (GList *ptr = base_info->delta_info; ptr != NULL; ptr = ptr->next) {
		DeltaInfo *delta_info = (DeltaInfo*)ptr->data;
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
		/* Check the member_parent table as a way of checking if the method was added by a later generation. If so, still look for its PPDB info in our update tables */
		uint32_t token = mono_metadata_make_token (MONO_TABLE_METHOD, mono_metadata_token_index (idx));
		if (G_UNLIKELY (loc == 0 && info->member_parent && GPOINTER_TO_UINT (g_hash_table_lookup (info->member_parent, GUINT_TO_POINTER (token))) > 0)) {
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

	GList *list = base_info->delta_info;
	MonoTableInfo *table;
	/* result row, 0-based */
	int ridx;

	uint32_t exposed_gen = hot_reload_get_thread_generation ();
	do {
		if (!list)
			return TRUE;
		DeltaInfo *delta_info = (DeltaInfo*)list->data;
		g_assert (delta_info);
		if (delta_info->generation > exposed_gen)
			return TRUE;
		list = list->next;

		table = &delta_info->mutants [table_index];
		ridx = token_index - 1;
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
	g_assert (base_info->delta_info);

	*image_out = base_image;
	*index_out = orig_index;

	guint32 prev_size = heap->size;

	uint32_t current_gen = hot_reload_get_thread_generation ();
	GList *cur;
	for (cur = base_info->delta_info; cur; cur = cur->next) {
		DeltaInfo *delta_info = (DeltaInfo*)cur->data;
		g_assert (delta_info);
		MonoImage *delta_image = delta_info->delta_image;
		g_assert (delta_image);
		heap = get_heap (delta_image);

		*image_out = delta_image;

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

static int
hot_reload_table_num_rows_slow (MonoImage *base, int table_index)
{
	BaselineInfo *base_info = baseline_info_lookup (base);
	if (!base_info)
		return FALSE;

	uint32_t current_gen = hot_reload_get_thread_generation ();

	int rows = table_info_get_rows (&base->tables [table_index]);
	GList *cur;
	for (cur = base_info->delta_info; cur; cur = cur->next) {
		DeltaInfo *delta_info = (DeltaInfo*)cur->data;
		g_assert (delta_info);
		if (delta_info->generation > current_gen)
			break;
		rows = delta_info->count [table_index].prev_gen_rows + delta_info->count [table_index].inserted_rows;
	}
	return rows;
}

static void
add_member_to_baseline (BaselineInfo *base_info, DeltaInfo *delta_info, MonoClass *klass, uint32_t member_token)
{
	/* Check they really passed a table token, not just a table row index */
	g_assert (mono_metadata_token_table (member_token) != 0);

	if (!base_info->member_parent) {
		base_info->member_parent = g_hash_table_new (g_direct_hash, g_direct_equal);
	}
	MonoClassMetadataUpdateInfo *klass_info = mono_class_get_or_add_metadata_update_info (klass);
	GSList *members = klass_info->added_members;
	klass_info->added_members = g_slist_prepend_mem_manager (m_class_get_mem_manager (klass), members, GUINT_TO_POINTER (member_token));
	g_hash_table_insert (base_info->member_parent, GUINT_TO_POINTER (member_token), GUINT_TO_POINTER (m_class_get_type_token (klass)));
}

static void
add_method_to_baseline (BaselineInfo *base_info, DeltaInfo *delta_info, MonoClass *klass, uint32_t method_token, MonoDebugInformationEnc* pdb_address)
{
	add_member_to_baseline (base_info, delta_info, klass, method_token);

	if (pdb_address)
		set_delta_method_debug_info (delta_info, method_token, pdb_address);
}

static GSList*
hot_reload_get_added_members (MonoClass *klass)
{
	/* FIXME: locking for the GArray? */
	MonoImage *image = m_class_get_image (klass);
	if (!image->has_updates)
		return NULL;
	MonoClassMetadataUpdateInfo *klass_info = mono_class_get_metadata_update_info (klass);
	if (!klass_info)
		return NULL;
	return klass_info->added_members;
}

static uint32_t
hot_reload_member_parent (MonoImage *base_image, uint32_t member_token)
{
	/* make sure they passed a token, not just a table row index */
	g_assert (mono_metadata_token_table (member_token) != 0);

	if (!base_image->has_updates)
		return 0;
	BaselineInfo *base_info = baseline_info_lookup (base_image);
	if (!base_info || base_info->member_parent == NULL)
		return 0;

	uint32_t res = GPOINTER_TO_UINT (g_hash_table_lookup (base_info->member_parent, GUINT_TO_POINTER (member_token)));
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "member_parent lookup: 0x%08x returned 0x%08x\n", member_token, res);

	return res;
}

static uint32_t
hot_reload_method_parent  (MonoImage *base_image, uint32_t method_token)
{
	/* the callers might pass just an index without a table */
	uint32_t lookup_token = mono_metadata_make_token (MONO_TABLE_METHOD, mono_metadata_token_index (method_token));

	return hot_reload_member_parent (base_image, lookup_token);
}

static void
add_field_to_baseline (BaselineInfo *base_info, DeltaInfo *delta_info, MonoClass *klass, uint32_t field_token)
{
	add_member_to_baseline (base_info, delta_info, klass, field_token);
}

static uint32_t
hot_reload_field_parent (MonoImage *base_image, uint32_t field_token)
{
	/* the callers might pass just an index without a table */
	uint32_t lookup_token = mono_metadata_make_token (MONO_TABLE_FIELD, mono_metadata_token_index (field_token));

	return hot_reload_member_parent (base_image, lookup_token);
}


/* HACK - keep in sync with locator_t in metadata/metadata.c */
typedef struct {
	int idx;			/* The index that we are trying to locate */
	int col_idx;		/* The index in the row where idx may be stored */
	MonoTableInfo *t;	/* pointer to the table */
	guint32 result;
} upd_locator_t;

void*
hot_reload_metadata_linear_search (MonoImage *base_image, MonoTableInfo *base_table, const void *key, BinarySearchComparer comparer)
{
	BaselineInfo *base_info = baseline_info_lookup (base_image);
	g_assert (base_info);

	g_assert (base_image->tables < base_table && base_table < &base_image->tables [MONO_TABLE_LAST]);

	int tbl_index;
	{
		size_t s = ALIGN_TO (sizeof (MonoTableInfo), sizeof (gpointer));
		tbl_index = (int)(((intptr_t) base_table - (intptr_t) base_image->tables) / s);
	}

	DeltaInfo *delta_info = NULL;
	const MonoTableInfo *latest_mod_table = base_table;
	gboolean success = effective_table_mutant (base_image, base_info, tbl_index, &latest_mod_table, &delta_info);
	g_assert (success);
	uint32_t rows = table_info_get_rows (latest_mod_table);

	upd_locator_t *loc = (upd_locator_t*)key;
	g_assert (loc);
	loc->result = 0;
	/* HACK: this is so that the locator can compute the row index of the given row. but passing the mutant table to other metadata functions could backfire. */
	loc->t = (MonoTableInfo*)latest_mod_table;
	for (uint32_t idx = 0; idx < rows; ++idx) {
		const char *row = latest_mod_table->base + idx * latest_mod_table->row_size;
		if (!comparer (loc, row))
			return (void*)row;
	}
	return NULL;
}

static uint32_t
hot_reload_get_field_idx (MonoClassField *field)
{
	g_assert (m_field_is_from_update (field));
	MonoClassMetadataUpdateField *field_info = (MonoClassMetadataUpdateField*)field;
	return mono_metadata_token_index (field_info->token);
}

static MonoClassField *
hot_reload_get_field (MonoClass *klass, uint32_t fielddef_token) {
	MonoClassMetadataUpdateInfo *info = mono_class_get_or_add_metadata_update_info (klass);
	g_assert (mono_metadata_token_table (fielddef_token) == MONO_TABLE_FIELD);
	/* FIXME: this needs locking in the multi-threaded case.  There could be an update happening that resizes the array. */
	GPtrArray *added_fields = info->added_fields;
	uint32_t count = added_fields->len;
	for (uint32_t i = 0; i < count; ++i) {
		MonoClassMetadataUpdateField *field = (MonoClassMetadataUpdateField *)g_ptr_array_index (added_fields, i);
		if (field->token == fielddef_token)
			return &field->field;
	}
	return NULL;
}


static MonoClassMetadataUpdateField *
metadata_update_field_setup_basic_info_and_resolve (MonoImage *image_base, BaselineInfo *base_info, uint32_t generation, DeltaInfo *delta_info, MonoClass *parent_klass, uint32_t fielddef_token, uint32_t field_flags, MonoError *error)
{
	// TODO: hang a "pending field" struct off the parent_klass if !parent_klass->fields
	//   In that case we can do things simpler, maybe by just creating the MonoClassField array as usual, and just relying on the normal layout algorithm to make space for the instance.

	if (!m_class_is_inited (parent_klass))
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Adding fielddef 0x%08x to uninited class 0x%08x", fielddef_token, m_class_get_type_token (parent_klass));

	MonoClassMetadataUpdateInfo *parent_info = mono_class_get_or_add_metadata_update_info (parent_klass);

	MonoClassMetadataUpdateField *field = mono_class_alloc0 (parent_klass, sizeof (MonoClassMetadataUpdateField));

	m_field_set_parent (&field->field, parent_klass);
	m_field_set_meta_flags (&field->field, MONO_CLASS_FIELD_META_FLAG_FROM_UPDATE);
	/* It's a special field */
	field->field.offset = -1;
	field->generation = generation;
	field->token = fielddef_token;

	uint32_t name_idx = mono_metadata_decode_table_row_col (image_base, MONO_TABLE_FIELD, mono_metadata_token_index (fielddef_token) - 1, MONO_FIELD_NAME);
	field->field.name = mono_metadata_string_heap (image_base, name_idx);

	mono_field_resolve_type (&field->field, error);
	if (!is_ok (error))
		return NULL;

	if (!parent_info->added_fields) {
		parent_info->added_fields = g_ptr_array_new ();
	}

	g_ptr_array_add (parent_info->added_fields, field);

	return field;
}

static void
ensure_class_runtime_info_inited (MonoClass *klass, MonoClassRuntimeMetadataUpdateInfo *runtime_info)
{
	if (runtime_info->inited)
		return;
	mono_loader_lock ();
	if (runtime_info->inited) {
		mono_loader_unlock ();
		return;
	}

	mono_coop_mutex_init (&runtime_info->static_fields_lock);

	/* FIXME: is it ok to re-use MONO_ROOT_SOURCE_STATIC here? */
	runtime_info->static_fields = mono_g_hash_table_new_type_internal (NULL, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_STATIC, NULL, "Hot Reload Static Fields");

	runtime_info->inited = TRUE;

	mono_loader_unlock ();
}

static void
class_runtime_info_static_fields_lock (MonoClassRuntimeMetadataUpdateInfo *runtime_info)
{
	mono_coop_mutex_lock (&runtime_info->static_fields_lock);
}

static void
class_runtime_info_static_fields_unlock (MonoClassRuntimeMetadataUpdateInfo *runtime_info)
{
	mono_coop_mutex_unlock (&runtime_info->static_fields_lock);
}

static GENERATE_GET_CLASS_WITH_CACHE_DECL (hot_reload_field_store);

static GENERATE_GET_CLASS_WITH_CACHE(hot_reload_field_store, "Mono.HotReload", "FieldStore");


static MonoObject*
create_static_field_storage (MonoType *t, MonoError *error)
{
	MonoClass *klass;
	if (!mono_type_is_reference (t))
		klass = mono_class_from_mono_type_internal (t);
	else
		klass = mono_class_get_hot_reload_field_store_class ();

	return mono_object_new_pinned (klass, error);
}

static gpointer
hot_reload_get_static_field_addr (MonoClassField *field)
{
	g_assert (m_field_is_from_update (field));
	MonoClassMetadataUpdateField *f = (MonoClassMetadataUpdateField *)field;
	g_assert ((f->field.type->attrs & FIELD_ATTRIBUTE_STATIC) != 0);
	g_assert (!m_type_is_byref(f->field.type)); // byref fields only in ref structs, which aren't allowed in EnC updates

	MonoClass *parent = m_field_get_parent (&f->field);
	MonoClassMetadataUpdateInfo *parent_info = mono_class_get_or_add_metadata_update_info (parent);
	MonoClassRuntimeMetadataUpdateInfo *runtime_info = &parent_info->runtime;

	ensure_class_runtime_info_inited (parent, runtime_info);

	MonoObject *obj = NULL;
	class_runtime_info_static_fields_lock (runtime_info);
	obj = (MonoObject*) mono_g_hash_table_lookup (runtime_info->static_fields, GUINT_TO_POINTER (f->token));
	class_runtime_info_static_fields_unlock (runtime_info);
	if (!obj) {
		ERROR_DECL (error);
		obj = create_static_field_storage (f->field.type, error);
		class_runtime_info_static_fields_lock (runtime_info);
		mono_error_assert_ok (error);
		MonoObject *obj2 = (MonoObject*) mono_g_hash_table_lookup (runtime_info->static_fields, GUINT_TO_POINTER (f->token));
		if (!obj2) {
			// Noone else created it, use ours
			mono_g_hash_table_insert_internal (runtime_info->static_fields, GUINT_TO_POINTER (f->token), obj);
		} else {
			/* beaten by another thread, silently drop our storage object and use theirs */
			obj = obj2;
		}
		class_runtime_info_static_fields_unlock (runtime_info);
	}
	g_assert (obj);

	gpointer addr = NULL;
	if (!mono_type_is_reference (f->field.type)) {
		// object is just the boxed value
		addr = mono_object_unbox_internal (obj);
	} else {
		// object is a Mono.HotReload.FieldStore, and the static field value is obj._loc
		MonoHotReloadFieldStoreObject *store = (MonoHotReloadFieldStoreObject *)obj;
		addr = (gpointer)&store->_loc;
	}
	g_assert (addr);

	return addr;
}

static MonoMethod *
hot_reload_find_method_by_name (MonoClass *klass, const char *name, int param_count, int flags, MonoError *error)
{
	GSList *members = hot_reload_get_added_members (klass);
	if (!members)
		return NULL;

	MonoImage *image = m_class_get_image (klass);
	MonoMethod *res = NULL;
	for (GSList *ptr = members; ptr; ptr = ptr->next) {
		uint32_t token = GPOINTER_TO_UINT(ptr->data);
		if (mono_metadata_token_table (token) != MONO_TABLE_METHOD)
			continue;
		uint32_t idx = mono_metadata_token_index (token);
		uint32_t cols [MONO_METHOD_SIZE];
		mono_metadata_decode_table_row (image, MONO_TABLE_METHOD, idx - 1, cols, MONO_METHOD_SIZE);

		if (!strcmp (mono_metadata_string_heap (image, cols [MONO_METHOD_NAME]), name)) {
			ERROR_DECL (local_error);
			MonoMethod *method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | idx, klass, NULL, local_error);
			if (!method) {
				mono_error_cleanup (local_error);
				continue;
			}
			if (param_count == -1) {
				res = method;
				break;
			}
			MonoMethodSignature *sig = mono_method_signature_checked (method, local_error);
			if (!sig) {
				mono_error_cleanup (error);
				continue;
			}
			if ((method->flags & flags) == flags && sig->param_count == param_count) {
				res = method;
				break;
			}
		}
	}

	return res;
}
