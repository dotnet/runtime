// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#include <config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-init-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/custom-attrs-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/metadata-update.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/unlocked.h>

static MonoNativeTlsKey preload_visiting_classes_tls_id;

static void
preload_visiting_classes_dtor (void *arg)
{
	GHashTable *hash = (GHashTable*)arg;
	if (hash)
		g_hash_table_destroy (hash);
}

void
mono_class_preload_init(void)
{
	/* FIXME: on Win32 we don't have dtors */
	mono_native_tls_alloc (&preload_visiting_classes_tls_id, preload_visiting_classes_dtor);
}

static inline GHashTable *
preload_visiting_hash (void)
{
	GHashTable *hash = (GHashTable*)mono_native_tls_get_value (preload_visiting_classes_tls_id);
	if (!hash) {
		hash = g_hash_table_new (g_direct_hash, g_direct_equal);
		mono_native_tls_set_value (preload_visiting_classes_tls_id, hash);
	}
	return hash;
}

static gboolean
preload_is_visiting (MonoClass *klass)
{
	GHashTable *hash = preload_visiting_hash ();
	return g_hash_table_lookup (hash, klass) != NULL;
}

static void
preload_begin_visiting (MonoClass *klass)
{
	GHashTable *hash = preload_visiting_hash ();
	g_hash_table_insert (hash, klass, klass);
}

static void
preload_done_visiting (MonoClass *klass)
{
	GHashTable *hash = preload_visiting_hash ();
	g_hash_table_remove (hash, klass);
	mono_trace(G_LOG_LEVEL_DEBUG, MONO_TRACE_TYPE, "Done preloading '%s.%s'", m_class_get_name_space (klass), m_class_get_name (klass));
}

/*
 * Class preloading: Assembly loading without the loader lock.
 *
 * Historically, Mono used a single global loader lock to protect a class that was being created (in
 * mono_class_create_from_typedef).  In order to correctly set up the parent of a class, the loader
 * lock was held recursively, and the parent class was also initialized.  This can trigger assembly
 * loading, and assembly loading can trigger managed callbacks.  As a result we would call managed
 * code while holding the loader lock, which could create deadlocks (for example if the managed code
 * waited for another thread that needed to do class initialization).
 *
 * What we do now is we allocate a MonoClass in the MONO_CLASS_READY_BAREBONES state and then try to
 * pre-load its parent and interfaces, without holding the loader lock.  However we need to avoid
 * cycles (both invalid IL like: class SubClass : SubClass; and also valid IL like class SubClass :
 * ParentClass<SubClass>).  So we use a thread-local visited hash.  When we're done preloading a
 * class we put it into the MONO_CLASS_READY_APPROX_PARENT state to signal that it has been fully
 * pre-loaded.
 *
 * At that point we can take the global loader lock and proceed with normal initialization, without
 * triggering assembly loading callbacks.
 *
 * Note that while we're doing the preloading, we can't form a generic instance yet - we can preload
 * the gtd, and we can preload the instantiation types, but we can't call
 * mono_class_from_mono_type_internal on a MonoType for a ginst (and similarly for array, pointer
 * etc types) because we dont' have incremental initialization there.  The preloading has to be
 * careful not to try and fully initialize any types.
 *
 */

static void
preload_visit_classkind (MonoClass *klass);
static void
preload_visit_classdef (MonoClass *klass);
static void
preload_visit_parent (MonoClass *klass);
static void
preload_visit_interfaces (MonoClass *klass);
static void
preload_visit_class_generic_param_constraints (MonoClass *klass);
static void
preload_visit_dor (MonoImage *image, MonoGenericContainer *container, uint32_t def_or_ref_or_spec_token);
static void
preload_visit_interfaces_from_typedef (MonoImage *meta, guint32 index, uint32_t **interface_tokens, guint *count);
static void
preload_visit_generic_param_constraints (MonoImage *image, guint32 token, MonoGenericContainer *container);
static void
preload_visit_gparam_constraints (MonoImage *image, guint32 owner, MonoGenericContainer *container);
static void
preload_visit_typedef (MonoImage *image, uint32_t typedef_index);
static void
preload_visit_typeref (MonoImage *image, uint32_t typeref_index);
static void
preload_visit_typespec (MonoImage *image, uint32_t typespec_index);
static void
preload_visit_type_and_cmods (MonoImage *image, MonoType *ty);
static void
preload_visit_and_inflate_mono_type (MonoImage *image, MonoType *ty);
static uint32_t
preload_resolve_from_name (MonoImage *image, const char *nspace, const char *name);
static void
preload_visit_generic_class (MonoImage *image, MonoGenericClass *generic_class);


void
mono_class_preload_class (MonoClass *klass)
{
	preload_visit_classdef (klass);
}

static void
preload_visit_classdef (MonoClass *klass)
{
	/* we should only come here without the loader lock */
	g_assert (!mono_loader_lock_tracking() || !mono_loader_lock_is_owned_by_self());
	/* if someone already visited this class, don't visit it again */
	if (m_class_ready_level_at_least (klass, MONO_CLASS_READY_APPROX_PARENT))
		return;
	
	/* corlib classes are already loaded and corlib doesn't depend on any other assemblies. */
	/* micro-optimization: don't bother adding them to the visiting hash, just get out quickly. */
	if (m_class_get_image (klass) == mono_defaults.corlib) {
		m_class_set_ready_level_at_least (klass, MONO_CLASS_READY_APPROX_PARENT);
		return;
	}

	int class_kind = m_class_get_class_kind (klass);
	g_assert (class_kind == MONO_CLASS_DEF || class_kind == MONO_CLASS_GTD);

	/* if we're already visiting this class, don't go again. */
	if (preload_is_visiting (klass))
		return;

	/* mark the that we started visiting the class */
	/* WISH: it would be nice if we could avoid visiting a class is another thread already put
           it into the PRELOAD_STARTED state.  But that seems hard: the loser thread would need to
           somehow wait for the other thread to finish preloading the class.  On the other hand if
           we have a loop, we don't want both threads to wait for each other and deadlock.  Simplest
           approach is just to let both threads explore everything and use a thread-local visiting
           table to avoid looping in any one thread. */
	preload_begin_visiting (klass);

	preload_visit_parent (klass);
	preload_visit_interfaces (klass);
	if (class_kind == MONO_CLASS_GTD)
		preload_visit_class_generic_param_constraints (klass);

	m_class_set_ready_level_at_least (klass, MONO_CLASS_READY_APPROX_PARENT);
	preload_done_visiting (klass);
}

static void
preload_visit_classkind (MonoClass *klass)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		preload_visit_classdef (klass);
		break;
	case MONO_CLASS_GINST:
		if (m_class_ready_level_at_least (klass, MONO_CLASS_READY_APPROX_PARENT))
			break;
		preload_visit_type_and_cmods (m_class_get_image (klass), m_class_get_byval_arg (klass));
		break;
	case MONO_CLASS_GPARAM:
		// don't expect to do anything - the MonoGenericContainer constraints should've been
		// visited in the class def.
		break;
	case MONO_CLASS_ARRAY:
		// assumption: mono_class_create_array_type always sets m_class_get_byval_arg(klass) at BAREBONES, so that we can get the
		// element type from byval_arg
		g_assert (m_class_get_byval_arg (klass)->type == MONO_TYPE_SZARRAY || m_class_get_byval_arg (klass)->type == MONO_TYPE_ARRAY);
		preload_visit_type_and_cmods (m_class_get_image (klass), m_class_get_byval_arg (klass));
		break;
	case MONO_CLASS_POINTER:
		if (m_class_get_byval_arg (klass)->type == MONO_TYPE_PTR) {
			g_assert_not_reached (); // TODO: visit a pointer type
		} else {
			g_assert (m_class_get_byval_arg (klass)->type == MONO_TYPE_FNPTR);
			g_assert_not_reached (); // TODO: visit a fnptr type
		}
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
preload_visit_parent (MonoClass *klass)
{
	/* maybe the parent is already initialized */
	if (m_class_get_parent (klass) && m_class_ready_level_at_least (m_class_get_parent (klass), MONO_CLASS_READY_APPROX_PARENT))
		return;
	uint32_t klass_token = m_class_get_type_token (klass);
	MonoImage *image = m_class_get_image (klass);
	uint32_t parent_token = mono_metadata_token_from_dor (mono_metadata_decode_row_col (&image->tables [MONO_TABLE_TYPEDEF], mono_metadata_token_index (klass_token) - 1, MONO_TYPEDEF_EXTENDS));
	MonoGenericContainer *container = mono_class_try_get_generic_container (klass);
	preload_visit_dor (image, container, parent_token);
}

static void
preload_visit_interfaces (MonoClass *klass)
{
	if (m_class_is_interfaces_inited (klass))
		return; // TODO: assert that all the interefaces are at least APPROX_PARENT ?
	MonoImage *image = m_class_get_image (klass);
	uint32_t klass_token = m_class_get_type_token (klass);
	MonoGenericContainer *container = mono_class_try_get_generic_container (klass);
	guint count = 0;
	uint32_t *itf_tokens = NULL;
	preload_visit_interfaces_from_typedef (image, klass_token, &itf_tokens, &count);
	for (int i = 0; i < count; i++) {
		preload_visit_dor (image, container, itf_tokens[i]);
	}
	g_free (itf_tokens);
}

static void
preload_visit_class_generic_param_constraints (MonoClass *klass)
{
	MonoImage *image = m_class_get_image (klass);
	uint32_t klass_token = m_class_get_type_token (klass);
	MonoGenericContainer *container = mono_class_try_get_generic_container (klass);
	preload_visit_generic_param_constraints (image, klass_token, container);
}

/* FIXME: don't copy/aste from metadata.c */
typedef struct {
	guint32 idx;			/* The index that we are trying to locate */
	guint32 col_idx;		/* The index in the row where idx may be stored */
	MonoTableInfo *t;		/* pointer to the table */
	guint32 result;
} locator_t;

/* FIXME: don't copy/aste from metadata.c */
static int
table_locator (const void *a, const void *b)
{
	locator_t *loc = (locator_t *) a;
	const char *bb = (const char *) b;
	guint32 table_index = GPTRDIFF_TO_INT ((bb - loc->t->base) / loc->t->row_size);
	guint32 col;

	col = mono_metadata_decode_row_col (loc->t, table_index, loc->col_idx);

	if (loc->idx == col) {
		loc->result = table_index;
		return 0;
	}
	if (loc->idx < col)
		return -1;
	else
		return 1;
}


/* FIXME: don't copy/paste mono_metadata_interfaces_from_typedef_full, make an iterator function */
static void
preload_visit_interfaces_from_typedef (MonoImage *meta, guint32 index, uint32_t **interface_tokens, guint *count)
{
	gboolean heap_alloc_result = TRUE;
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_INTERFACEIMPL];
	locator_t loc;
	guint32 start, pos;
	guint32 cols [MONO_INTERFACEIMPL_SIZE];
	uint32_t *result;

	*interface_tokens = NULL;
	*count = 0;

	if (!tdef->base && !meta->has_updates)
		return;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_INTERFACEIMPL_CLASS;
	loc.t = tdef;
	loc.result = 0;

	gboolean found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;

	if (!found && !meta->has_updates)
		return;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found && !mono_metadata_update_metadata_linear_search (meta, tdef, &loc, table_locator)) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "NO Found interfaces for class 0x%08x", index);
			return;
		}
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Found interfaces for class 0x%08x starting at 0x%08x", index, loc.result);
	}

	start = loc.result;
	/*
	 * We may end up in the middle of the rows...
	 */
	while (start > 0) {
		if (loc.idx == mono_metadata_decode_row_col (tdef, start - 1, MONO_INTERFACEIMPL_CLASS))
			start--;
		else
			break;
	}
	pos = start;
	guint32 rows = mono_metadata_table_num_rows (meta, MONO_TABLE_INTERFACEIMPL);
	while (pos < rows) {
		mono_metadata_decode_row (tdef, pos, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		++pos;
	}

	if (heap_alloc_result)
		result = g_new0 (uint32_t, pos - start);
	else
		g_assert_not_reached ();

	pos = start;
	while (pos < rows) {
		mono_metadata_decode_row (tdef, pos, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		uint32_t token = mono_metadata_token_from_dor (cols [MONO_INTERFACEIMPL_INTERFACE]);
		result [pos - start] = token;
		++pos;
	}
	*count = pos - start;
	*interface_tokens = result;
	return;
}

/* FIXME: don't copy/paste from mono_metadata_load_generic_param_constraints_checked, make an interator */
static void
preload_visit_generic_param_constraints (MonoImage *image, guint32 token, MonoGenericContainer *container)
{

	guint32 start_row, owner;

	if (! (start_row = mono_metadata_get_generic_param_row (image, token, &owner)))
		return;
	for (int i = 0; i < container->type_argc; i++) {
		preload_visit_gparam_constraints (image, start_row + i, container);
	}
}

/* FIXME: don't copy/paste from metadata.c get_constraints, make an iterator */
static void
preload_visit_gparam_constraints (MonoImage *image, guint32 owner, MonoGenericContainer *container)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAMCONSTRAINT];
	guint32 cols [MONO_GENPARCONSTRAINT_SIZE];
	locator_t loc;
	guint32 i, token, start;

	/* FIXME: metadata-update */
	guint32 rows = table_info_get_rows (tdef);

	loc.idx = owner;
	loc.col_idx = MONO_GENPARCONSTRAINT_GENERICPAR;
	loc.t = tdef;
	loc.result = 0;

	gboolean is_found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;
	if (!is_found && !image->has_updates)
		return;

	if (is_found) {
		/* Find the first entry by searching backwards */
		while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_GENPARCONSTRAINT_GENERICPAR) == owner))
			loc.result --;
		start = loc.result;
	} else {
		start = 0;
	}

	for (i = start; i < rows; ++i) {
		mono_metadata_decode_row (tdef, i, cols, MONO_GENPARCONSTRAINT_SIZE);
		if (cols [MONO_GENPARCONSTRAINT_GENERICPAR] == owner) {
			token = mono_metadata_token_from_dor (cols [MONO_GENPARCONSTRAINT_CONSTRAINT]);
			preload_visit_dor (image, container, token);
		} else {
			break;
		}
	}
}

static void
preload_visit_dor (MonoImage *image, MonoGenericContainer *container, uint32_t def_or_ref_or_spec_token)
{
	int table = mono_metadata_token_table (def_or_ref_or_spec_token);
	uint32_t index = mono_metadata_token_index (def_or_ref_or_spec_token);
	switch (table) {
	case MONO_TABLE_TYPEDEF:
		preload_visit_typedef (image, index);
		break;
	case MONO_TABLE_TYPEREF:
		preload_visit_typeref (image, index);
		break;
	case MONO_TABLE_TYPESPEC:
		preload_visit_typespec (image, index);
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
preload_visit_typedef (MonoImage *image, uint32_t typedef_index)
{
	ERROR_DECL (preload_error);
	MonoClass *barebones = mono_class_create_from_typedef_at_level (image, typedef_index, MONO_CLASS_READY_BAREBONES, preload_error);
	if (!is_ok (preload_error)) {
		mono_error_cleanup (preload_error);
		return;
	}
	preload_visit_classdef (barebones);
}

/* FIXME: share more code with mono_class_from_typeref_checked */
static gboolean
preload_resolve_typeref (MonoImage *image, uint32_t type_token, MonoImage **resolved_image, uint32_t *resolved_typedef)
{
	guint32 cols [MONO_TYPEREF_SIZE];
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
	guint32 idx;
	const char *name, *nspace;
	MonoImage *module;

	*resolved_image = NULL;
	*resolved_typedef = 0;

	mono_metadata_decode_row (t, (type_token&0xffffff)-1, cols, MONO_TYPEREF_SIZE);

	name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);
	nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);

	idx = cols [MONO_TYPEREF_SCOPE] >> MONO_RESOLUTION_SCOPE_BITS;
	switch (cols [MONO_TYPEREF_SCOPE] & MONO_RESOLUTION_SCOPE_MASK) {
	case MONO_RESOLUTION_SCOPE_MODULE:
		/*
		LAMESPEC The spec says that a null module resolution scope should go through the exported type table.
		This is not the observed behavior of existing implementations.
		The defacto behavior is that it's just a typedef in disguise.
		*/
		/* a typedef in disguise */
		*resolved_image = image;
		*resolved_typedef = preload_resolve_from_name (image, nspace, name);
		return TRUE;

	case MONO_RESOLUTION_SCOPE_MODULEREF: {
			ERROR_DECL(loader_error);
			module = mono_image_load_module_checked (image, idx, loader_error);
			if (!is_ok (loader_error)) {
				mono_error_cleanup (loader_error);
				return FALSE;
			}
			*resolved_image = module;
			if (module) {
				*resolved_typedef = preload_resolve_from_name (module, nspace, name);
				return TRUE;
			} else {
				return FALSE;
			}
		}
	case MONO_RESOLUTION_SCOPE_TYPEREF: {
		if (idx == mono_metadata_token_index (type_token)) {
			// mono_error_set_bad_image (error, image, "Image with self-referencing typeref token %08x.", type_token);
			return FALSE;
		}

		MonoImage *enclosing_image = NULL;
		guint32 enclosing_type_token = 0;
		if (!preload_resolve_typeref (image, MONO_TOKEN_TYPE_REF | idx, &enclosing_image, &enclosing_type_token))
			return FALSE;

		/* Don't call mono_class_init_internal as we might've been called by it recursively */
		int i = mono_metadata_nesting_typedef (enclosing_image, enclosing_type_token, 1);
		while (i) {
			guint32 class_nested = mono_metadata_decode_row_col (&enclosing_image->tables [MONO_TABLE_NESTEDCLASS], i - 1, MONO_NESTED_CLASS_NESTED);
			guint32 string_offset = mono_metadata_decode_row_col (&enclosing_image->tables [MONO_TABLE_TYPEDEF], class_nested - 1, MONO_TYPEDEF_NAME);
			const char *nname = mono_metadata_string_heap (enclosing_image, string_offset);

			if (strcmp (nname, name) == 0) {
				*resolved_image = enclosing_image;
				*resolved_typedef = MONO_TOKEN_TYPE_DEF | class_nested;
				return TRUE;
			}
			i = mono_metadata_nesting_typedef (enclosing_image, enclosing_type_token, i + 1);
		}
		return FALSE;
	}
	case MONO_RESOLUTION_SCOPE_ASSEMBLYREF:
		break;
	}

	if (mono_metadata_table_bounds_check (image, MONO_TABLE_ASSEMBLYREF, idx)) {
		return FALSE;
	}

	if (!image->references || !image->references [idx - 1])
		mono_assembly_load_reference (image, idx - 1);
	g_assert (image->references [idx - 1]);

	/* If the assembly did not load, register this as a type load exception */
	if (image->references [idx - 1] == REFERENCE_MISSING){
		return FALSE;
	}

	*resolved_image = image->references [idx - 1]->image;
	*resolved_typedef = preload_resolve_from_name (image->references [idx - 1]->image, nspace, name);
	return TRUE;
}

static void
preload_visit_typeref (MonoImage *image, uint32_t typeref_index)
{
	MonoImage *resolved_image = NULL;
	uint32_t resolved_typedef = 0;
	if (!preload_resolve_typeref  (image, typeref_index, &resolved_image, &resolved_typedef))
		return;
	if (!resolved_typedef)
		return;
	preload_visit_typedef (resolved_image, resolved_typedef);
}

static uint32_t
preload_resolve_from_name (MonoImage *image, const char *nspace, const char *name)
{
	// TODO: finish this, see mono_class_from_name_checked_aux,
	// maybe we can just do this simple version since there shouldn't be netmodules
	uint32_t token = 0;
	mono_image_init_name_cache (image);
	GHashTable *nspace_table = (GHashTable *)g_hash_table_lookup (image->name_cache, nspace);

	if (nspace_table)
		token = GPOINTER_TO_UINT (g_hash_table_lookup (nspace_table, name));

	if (token)
		return token;
	g_error ("Token for %s.%s from (%s) not found in cache.\n", nspace, name, image->name);
}

/**
 * Preload all the classes in the given type and its custom modifiers.  If we see generic vars,
 * inflate them as we go.
 */
static void
preload_visit_type_and_cmods (MonoImage *image, MonoType *ty)
{
	preload_visit_and_inflate_mono_type (image, ty);
	if (G_UNLIKELY (ty->has_cmods)) {
		g_assert_not_reached (); // TODO: visit the cmods
	}
}

/**
 * Preload all the classes in the given type
 */
static void
preload_visit_and_inflate_mono_type (MonoImage *image, MonoType *ty)
{
	switch (ty->type) {
	case MONO_TYPE_VOID:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_TYPEDBYREF:
		// done
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		preload_visit_classdef (ty->data.klass);
		break;
	case MONO_TYPE_SZARRAY:
		preload_visit_classkind (ty->data.klass);
		break;
	case MONO_TYPE_ARRAY:
		g_assert_not_reached(); // TODO finish me
	case MONO_TYPE_PTR: {
		MonoType *etype = ty->data.type;
		preload_visit_type_and_cmods (image, etype);
		break;
	}
	case MONO_TYPE_FNPTR:
		g_assert_not_reached(); // TODO finish me
	case MONO_TYPE_MVAR:
		g_assert_not_reached(); // don't expect to see method vars during class preloading
	case MONO_TYPE_VAR:
		/* nothing to do, the generic context has already been preloaded */
		break;
	case MONO_TYPE_GENERICINST:
		preload_visit_generic_class (image, ty->data.generic_class);
		break;
	default:
		g_assert_not_reached();
	}
}

static void
preload_visit_typespec (MonoImage *image, uint32_t typespec_index)
{
	// see mono_type_create_from_typespec_checked and do_mono_metadata_parse_generic_class and mono_metadata_parse_generic_inst
	// TODO: finish this
	// N.B. we might need to make the parser return barebones types
	ERROR_DECL (typeload_error);
	MonoType * t = mono_type_create_from_typespec_at_level (image, typespec_index, MONO_CLASS_READY_BAREBONES, typeload_error);
	if (!is_ok (typeload_error)) {
		mono_error_cleanup (typeload_error);
		return;
	}
	
	preload_visit_type_and_cmods (image, t);
}

static void
preload_visit_generic_class (MonoImage *image, MonoGenericClass *generic_class)
{
	if (generic_class->cached_class) {
		/* if there's a cached MonoClass, for this generic instance, see if it's already
                   sufficiently initialized. */
		if (m_class_ready_level_at_least (generic_class->cached_class, MONO_CLASS_READY_EXACT_PARENT)) // FIXME: preload: APPROX_PARENT should be good enough
			return;
	}

	MonoClass *gtd = generic_class->container_class;

	preload_visit_classkind (gtd);
	MonoGenericInst *class_inst = generic_class->context.class_inst;
	g_assert (class_inst != NULL);
	g_assert (generic_class->context.method_inst == NULL);
	for (int i = 0; i < class_inst->type_argc; i++) {
		preload_visit_type_and_cmods (image, class_inst->type_argv[i]);
	}
}
