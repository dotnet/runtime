/**
 * \file
 * Image Loader
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 *
 * This file is used by the interpreter and the JIT engine to locate
 * assemblies.  Used to load AssemblyRef and later to resolve various
 * kinds of `Refs'.
 *
 * TODO:
 *   This should keep track of the assembly versions that we are loading.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/loader-internals.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/lock-tracer.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/exception-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-path.h>

MonoDefaults mono_defaults;

/*
 * This lock protects the hash tables inside MonoImage used by the metadata 
 * loading functions in class.c and loader.c.
 *
 * See domain-internals.h for locking policy in combination with the
 * domain lock.
 */
static MonoCoopMutex loader_mutex;
static mono_mutex_t global_loader_data_mutex;
static gboolean loader_lock_inited;

/* Statistics */
static gint32 inflated_signatures_size;
static gint32 memberref_sig_cache_size;
static gint32 methods_size;
static gint32 signatures_size;

/*
 * This TLS variable holds how many times the current thread has acquired the loader 
 * lock.
 */
static MonoNativeTlsKey loader_lock_nest_id;

#if ENABLE_NETCORE
static int pinvoke_search_directories_count;
static char **pinvoke_search_directories;
#endif

static void dllmap_cleanup (void);
static void cached_module_cleanup(void);

static void dllmap_insert_global (const char *dll, const char *func, const char *tdll, const char *tfunc);
static void dllmap_insert_image (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfun);


/* Class lazy loading functions */
GENERATE_GET_CLASS_WITH_CACHE (appdomain_unloaded_exception, "System", "AppDomainUnloadedException")
GENERATE_TRY_GET_CLASS_WITH_CACHE (appdomain_unloaded_exception, "System", "AppDomainUnloadedException")

static void
global_loader_data_lock (void)
{
	mono_locks_os_acquire (&global_loader_data_mutex, LoaderGlobalDataLock);
}

static void
global_loader_data_unlock (void)
{
	mono_locks_os_release (&global_loader_data_mutex, LoaderGlobalDataLock);
}

void
mono_loader_init ()
{
	static gboolean inited;

	if (!inited) {
		mono_coop_mutex_init_recursive (&loader_mutex);
		mono_os_mutex_init_recursive (&global_loader_data_mutex);
		loader_lock_inited = TRUE;

		mono_native_tls_alloc (&loader_lock_nest_id, NULL);

		mono_counters_init ();
		mono_counters_register ("Inflated signatures size",
								MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &inflated_signatures_size);
		mono_counters_register ("Memberref signature cache size",
								MONO_COUNTER_METADATA | MONO_COUNTER_INT, &memberref_sig_cache_size);
		mono_counters_register ("MonoMethod size",
								MONO_COUNTER_METADATA | MONO_COUNTER_INT, &methods_size);
		mono_counters_register ("MonoMethodSignature size",
								MONO_COUNTER_METADATA | MONO_COUNTER_INT, &signatures_size);

		inited = TRUE;
	}
}

void
mono_loader_cleanup (void)
{
	dllmap_cleanup ();
	cached_module_cleanup ();

	mono_native_tls_free (loader_lock_nest_id);

	mono_coop_mutex_destroy (&loader_mutex);
	mono_os_mutex_destroy (&global_loader_data_mutex);
	loader_lock_inited = FALSE;	
}

/*
 * find_cached_memberref_sig:
 *
 *   Return a cached copy of the memberref signature identified by SIG_IDX.
 * We use a gpointer since the cache stores both MonoTypes and MonoMethodSignatures.
 * A cache is needed since the type/signature parsing routines allocate everything 
 * from a mempool, so without a cache, multiple requests for the same signature would 
 * lead to unbounded memory growth. For normal methods/fields this is not a problem 
 * since the resulting methods/fields are cached, but inflated methods/fields cannot
 * be cached.
 * LOCKING: Acquires the loader lock.
 */
static gpointer
find_cached_memberref_sig (MonoImage *image, guint32 sig_idx)
{
	gpointer res;

	mono_image_lock (image);
	res = g_hash_table_lookup (image->memberref_signatures, GUINT_TO_POINTER (sig_idx));
	mono_image_unlock (image);

	return res;
}

static gpointer
cache_memberref_sig (MonoImage *image, guint32 sig_idx, gpointer sig)
{
	gpointer prev_sig;

	mono_image_lock (image);
	prev_sig = g_hash_table_lookup (image->memberref_signatures, GUINT_TO_POINTER (sig_idx));
	if (prev_sig) {
		/* Somebody got in before us */
		sig = prev_sig;
	}
	else {
		g_hash_table_insert (image->memberref_signatures, GUINT_TO_POINTER (sig_idx), sig);
		/* An approximation based on glib 2.18 */
		mono_atomic_fetch_add_i32 (&memberref_sig_cache_size, sizeof (gpointer) * 4);
	}
	mono_image_unlock (image);

	return sig;
}

static MonoClassField*
field_from_memberref (MonoImage *image, guint32 token, MonoClass **retklass,
		      MonoGenericContext *context, MonoError *error)
{
	MonoClass *klass = NULL;
	MonoClassField *field;
	MonoTableInfo *tables = image->tables;
	MonoType *sig_type;
	guint32 cols[6];
	guint32 nindex, class_index;
	const char *fname;
	const char *ptr;
	guint32 idx = mono_metadata_token_index (token);

	error_init (error);

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
	class_index = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;

	fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

	if (!mono_verifier_verify_memberref_field_signature (image, cols [MONO_MEMBERREF_SIGNATURE], error))
		return NULL;

	switch (class_index) {
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | nindex, error);
		break;
	case MONO_MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref_checked (image, MONO_TOKEN_TYPE_REF | nindex, error);
		break;
	case MONO_MEMBERREF_PARENT_TYPESPEC:
		klass = mono_class_get_and_inflate_typespec_checked (image, MONO_TOKEN_TYPE_SPEC | nindex, context, error);
		break;
	default:
		mono_error_set_bad_image (error, image, "Bad field field '%u' signature 0x%08x", class_index, token);
	}

	if (!klass)
		return NULL;

	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	/* we may want to check the signature here... */

	if (*ptr++ != 0x6) {
		mono_error_set_field_missing (error, klass, fname, NULL, "Bad field signature class token %08x field token %08x", class_index, token);
		return NULL;
	}

	/* FIXME: This needs a cache, especially for generic instances, since
	 * we ask mono_metadata_parse_type_checked () to allocates everything from a mempool.
	 * FIXME part2, mono_metadata_parse_type_checked actually allows for a transient type instead.
	 * FIXME part3, transient types are not 100% transient, so we need to take care of that first.
	 */
	sig_type = (MonoType *)find_cached_memberref_sig (image, cols [MONO_MEMBERREF_SIGNATURE]);
	if (!sig_type) {
		ERROR_DECL (inner_error);
		sig_type = mono_metadata_parse_type_checked (image, NULL, 0, FALSE, ptr, &ptr, inner_error);
		if (sig_type == NULL) {
			mono_error_set_field_missing (error, klass, fname, NULL, "Could not parse field signature %08x due to: %s", token, mono_error_get_message (inner_error));
			mono_error_cleanup (inner_error);
			return NULL;
		}
		sig_type = (MonoType *)cache_memberref_sig (image, cols [MONO_MEMBERREF_SIGNATURE], sig_type);
	}

	mono_class_init_internal (klass); /*FIXME is this really necessary?*/
	if (retklass)
		*retklass = klass;
	field = mono_class_get_field_from_name_full (klass, fname, sig_type);

	if (!field) {
		mono_error_set_field_missing (error, klass, fname, sig_type, "Could not find field in class");
	}

	return field;
}

/**
 * mono_field_from_token:
 * \deprecated use the \c _checked variant
 * Notes: runtime code MUST not use this function
 */
MonoClassField*
mono_field_from_token (MonoImage *image, guint32 token, MonoClass **retklass, MonoGenericContext *context)
{
	ERROR_DECL (error);
	MonoClassField *res = mono_field_from_token_checked (image, token, retklass, context, error);
	mono_error_assert_ok (error);
	return res;
}

MonoClassField*
mono_field_from_token_checked (MonoImage *image, guint32 token, MonoClass **retklass, MonoGenericContext *context, MonoError *error)
{
	MonoClass *k;
	guint32 type;
	MonoClassField *field;

	error_init (error);

	if (image_is_dynamic (image)) {
		MonoClassField *result;
		MonoClass *handle_class;

		*retklass = NULL;
		ERROR_DECL (inner_error);
		result = (MonoClassField *)mono_lookup_dynamic_token_class (image, token, TRUE, &handle_class, context, inner_error);
		mono_error_cleanup (inner_error);
		// This checks the memberref type as well
		if (!result || handle_class != mono_defaults.fieldhandle_class) {
			mono_error_set_bad_image (error, image, "Bad field token 0x%08x", token);
			return NULL;
		}
		*retklass = result->parent;
		return result;
	}

	if ((field = (MonoClassField *)mono_conc_hashtable_lookup (image->field_cache, GUINT_TO_POINTER (token)))) {
		*retklass = field->parent;
		return field;
	}

	if (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF) {
		field = field_from_memberref (image, token, retklass, context, error);
	} else {
		type = mono_metadata_typedef_from_field (image, mono_metadata_token_index (token));
		if (!type) {
			mono_error_set_bad_image (error, image, "Invalid field token 0x%08x", token);
			return NULL;
		}
		k = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | type, error);
		if (!k)
			return NULL;

		mono_class_init_internal (k);
		if (retklass)
			*retklass = k;
		if (mono_class_has_failure (k)) {
			ERROR_DECL (causedby_error);
			mono_error_set_for_class_failure (causedby_error, k);
			mono_error_set_bad_image (error, image, "Could not resolve field token 0x%08x, due to: %s", token, mono_error_get_message (causedby_error));
			mono_error_cleanup (causedby_error);
		} else {
			field = mono_class_get_field (k, token);
			if (!field) {
				mono_error_set_bad_image (error, image, "Could not resolve field token 0x%08x", token);
			}
		}
	}

	if (field && field->parent && !mono_class_is_ginst (field->parent) && !mono_class_is_gtd (field->parent)) {
		mono_image_lock (image);
		mono_conc_hashtable_insert (image->field_cache, GUINT_TO_POINTER (token), field);
		mono_image_unlock (image);
	}

	return field;
}

static gboolean
mono_metadata_signature_vararg_match (MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
	int i;

	if (sig1->hasthis != sig2->hasthis ||
	    sig1->sentinelpos != sig2->sentinelpos)
		return FALSE;

	for (i = 0; i < sig1->sentinelpos; i++) { 
		MonoType *p1 = sig1->params[i];
		MonoType *p2 = sig2->params[i];

		/*if (p1->attrs != p2->attrs)
			return FALSE;
		*/
		if (!mono_metadata_type_equal (p1, p2))
			return FALSE;
	}

	if (!mono_metadata_type_equal (sig1->ret, sig2->ret))
		return FALSE;
	return TRUE;
}

static MonoMethod *
find_method_in_class (MonoClass *klass, const char *name, const char *qname, const char *fqname,
		      MonoMethodSignature *sig, MonoClass *from_class, MonoError *error)
{
 	int i;

	/* Search directly in the metadata to avoid calling setup_methods () */
	error_init (error);

	MonoImage *klass_image = m_class_get_image (klass);
	/* FIXME: !mono_class_is_ginst (from_class) condition causes test failures. */
	if (m_class_get_type_token (klass) && !image_is_dynamic (klass_image) && !m_class_get_methods (klass) && !m_class_get_rank (klass) && klass == from_class && !mono_class_is_ginst (from_class)) {
		int first_idx = mono_class_get_first_method_idx (klass);
		int mcount = mono_class_get_method_count (klass);
		for (i = 0; i < mcount; ++i) {
			guint32 cols [MONO_METHOD_SIZE];
			MonoMethod *method;
			const char *m_name;
			MonoMethodSignature *other_sig;

			mono_metadata_decode_table_row (klass_image, MONO_TABLE_METHOD, first_idx + i, cols, MONO_METHOD_SIZE);

			m_name = mono_metadata_string_heap (klass_image, cols [MONO_METHOD_NAME]);

			if (!((fqname && !strcmp (m_name, fqname)) ||
				  (qname && !strcmp (m_name, qname)) ||
				  (name && !strcmp (m_name, name))))
				continue;

			method = mono_get_method_checked (klass_image, MONO_TOKEN_METHOD_DEF | (first_idx + i + 1), klass, NULL, error);
			if (!mono_error_ok (error)) //bail out if we hit a loader error
				return NULL;
			if (method) {
				other_sig = mono_method_signature_checked (method, error);
				if (!mono_error_ok (error)) //bail out if we hit a loader error
					return NULL;				
				if (other_sig && (sig->call_convention != MONO_CALL_VARARG) && mono_metadata_signature_equal (sig, other_sig))
					return method;
			}
		}
	}

	mono_class_setup_methods (klass); /* FIXME don't swallow the error here. */
	/*
	We can't fail lookup of methods otherwise the runtime will fail with MissingMethodException instead of TypeLoadException.
	See mono/tests/generic-type-load-exception.2.il
	FIXME we should better report this error to the caller
	 */
	if (!m_class_get_methods (klass) || mono_class_has_failure (klass)) {
		ERROR_DECL (cause_error);
		mono_error_set_for_class_failure (cause_error, klass);
		mono_error_set_type_load_class (error, klass, "Could not find method '%s' due to a type load error: %s", name, mono_error_get_message (cause_error));
		mono_error_cleanup (cause_error);
		return NULL;
	}
	int mcount = mono_class_get_method_count (klass);
	MonoMethod **klass_methods = m_class_get_methods (klass);
	for (i = 0; i < mcount; ++i) {
		MonoMethod *m = klass_methods [i];
		MonoMethodSignature *msig;

		/* We must cope with failing to load some of the types. */
		if (!m)
			continue;

		if (!((fqname && !strcmp (m->name, fqname)) ||
		      (qname && !strcmp (m->name, qname)) ||
		      (name && !strcmp (m->name, name))))
			continue;
		msig = mono_method_signature_checked (m, error);
		if (!mono_error_ok (error)) //bail out if we hit a loader error 
			return NULL;

		if (!msig)
			continue;

		if (sig->call_convention == MONO_CALL_VARARG) {
			if (mono_metadata_signature_vararg_match (sig, msig))
				break;
		} else {
			if (mono_metadata_signature_equal (sig, msig))
				break;
		}
	}

	if (i < mcount)
		return mono_class_get_method_by_index (from_class, i);
	return NULL;
}

static MonoMethod *
find_method (MonoClass *in_class, MonoClass *ic, const char* name, MonoMethodSignature *sig, MonoClass *from_class, MonoError *error)
{
	int i;
	char *qname, *fqname, *class_name;
	gboolean is_interface;
	MonoMethod *result = NULL;
	MonoClass *initial_class = in_class;

	error_init (error);
	is_interface = MONO_CLASS_IS_INTERFACE_INTERNAL (in_class);

	if (ic) {
		class_name = mono_type_get_name_full (m_class_get_byval_arg (ic), MONO_TYPE_NAME_FORMAT_IL);

		qname = g_strconcat (class_name, ".", name, NULL);
		const char *ic_name_space = m_class_get_name_space (ic);
		if (ic_name_space && ic_name_space [0])
			fqname = g_strconcat (ic_name_space, ".", class_name, ".", name, NULL);
		else
			fqname = NULL;
	} else
		class_name = qname = fqname = NULL;

	while (in_class) {
		g_assert (from_class);
		result = find_method_in_class (in_class, name, qname, fqname, sig, from_class, error);
		if (result || !mono_error_ok (error))
			goto out;

		if (name [0] == '.' && (!strcmp (name, ".ctor") || !strcmp (name, ".cctor")))
			break;

		/*
		 * This happens when we fail to lazily load the interfaces of one of the types.
		 * On such case we can't just bail out since user code depends on us trying harder.
		 */
		if (m_class_get_interface_offsets_count (from_class) != m_class_get_interface_offsets_count (in_class)) {
			in_class = m_class_get_parent (in_class);
			from_class = m_class_get_parent (from_class);
			continue;
		}

		int in_class_interface_offsets_count = m_class_get_interface_offsets_count (in_class);
		MonoClass **in_class_interfaces_packed = m_class_get_interfaces_packed (in_class);
		MonoClass **from_class_interfaces_packed = m_class_get_interfaces_packed (from_class);
		for (i = 0; i < in_class_interface_offsets_count; i++) {
			MonoClass *in_ic = in_class_interfaces_packed [i];
			MonoClass *from_ic = from_class_interfaces_packed [i];
			char *ic_qname, *ic_fqname, *ic_class_name;
			
			ic_class_name = mono_type_get_name_full (m_class_get_byval_arg (in_ic), MONO_TYPE_NAME_FORMAT_IL);
			ic_qname = g_strconcat (ic_class_name, ".", name, NULL); 
			const char *in_ic_name_space = m_class_get_name_space (in_ic);
			if (in_ic_name_space && in_ic_name_space [0])
				ic_fqname = g_strconcat (in_ic_name_space, ".", ic_class_name, ".", name, NULL);
			else
				ic_fqname = NULL;

			result = find_method_in_class (in_ic, ic ? name : NULL, ic_qname, ic_fqname, sig, from_ic, error);
			g_free (ic_class_name);
			g_free (ic_fqname);
			g_free (ic_qname);
			if (result || !mono_error_ok (error))
				goto out;
		}

		in_class = m_class_get_parent (in_class);
		from_class = m_class_get_parent (from_class);
	}
	g_assert (!in_class == !from_class);

	if (is_interface)
		result = find_method_in_class (mono_defaults.object_class, name, qname, fqname, sig, mono_defaults.object_class, error);

	//we did not find the method
	if (!result && mono_error_ok (error))
		mono_error_set_method_missing (error, initial_class, name, sig, NULL);
		
 out:
	g_free (class_name);
	g_free (fqname);
	g_free (qname);
	return result;
}

static MonoMethodSignature*
inflate_generic_signature_checked (MonoImage *image, MonoMethodSignature *sig, MonoGenericContext *context, MonoError *error)
{
	MonoMethodSignature *res;
	gboolean is_open;
	int i;

	error_init (error);
	if (!context)
		return sig;

	res = (MonoMethodSignature *)g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + ((gint32)sig->param_count) * sizeof (MonoType*));
	res->param_count = sig->param_count;
	res->sentinelpos = -1;
	res->ret = mono_class_inflate_generic_type_checked (sig->ret, context, error);
	if (!mono_error_ok (error))
		goto fail;
	is_open = mono_class_is_open_constructed_type (res->ret);
	for (i = 0; i < sig->param_count; ++i) {
		res->params [i] = mono_class_inflate_generic_type_checked (sig->params [i], context, error);
		if (!mono_error_ok (error))
			goto fail;

		if (!is_open)
			is_open = mono_class_is_open_constructed_type (res->params [i]);
	}
	res->hasthis = sig->hasthis;
	res->explicit_this = sig->explicit_this;
	res->call_convention = sig->call_convention;
	res->pinvoke = sig->pinvoke;
	res->generic_param_count = sig->generic_param_count;
	res->sentinelpos = sig->sentinelpos;
	res->has_type_parameters = is_open;
	res->is_inflated = 1;
	return res;

fail:
	if (res->ret)
		mono_metadata_free_type (res->ret);
	for (i = 0; i < sig->param_count; ++i) {
		if (res->params [i])
			mono_metadata_free_type (res->params [i]);
	}
	g_free (res);
	return NULL;
}

/**
 * mono_inflate_generic_signature:
 *
 * Inflate \p sig with \p context, and return a canonical copy. On error, set \p error, and return NULL.
 */
MonoMethodSignature*
mono_inflate_generic_signature (MonoMethodSignature *sig, MonoGenericContext *context, MonoError *error)
{
	MonoMethodSignature *res, *cached;

	res = inflate_generic_signature_checked (NULL, sig, context, error);
	if (!mono_error_ok (error))
		return NULL;
	cached = mono_metadata_get_inflated_signature (res, context);
	if (cached != res)
		mono_metadata_free_inflated_signature (res);
	return cached;
}

static MonoMethodHeader*
inflate_generic_header (MonoMethodHeader *header, MonoGenericContext *context, MonoError *error)
{
	size_t locals_size = sizeof (gpointer) * header->num_locals;
	size_t clauses_size = header->num_clauses * sizeof (MonoExceptionClause);
	size_t header_size = MONO_SIZEOF_METHOD_HEADER + locals_size + clauses_size; 
	MonoMethodHeader *res = (MonoMethodHeader *)g_malloc0 (header_size);
	res->num_locals = header->num_locals;
	res->clauses = (MonoExceptionClause *) &res->locals [res->num_locals] ;
	memcpy (res->clauses, header->clauses, clauses_size);

	res->code = header->code;
	res->code_size = header->code_size;
	res->max_stack = header->max_stack;
	res->num_clauses = header->num_clauses;
	res->init_locals = header->init_locals;

	res->is_transient = TRUE;

	error_init (error);

	for (int i = 0; i < header->num_locals; ++i) {
		res->locals [i] = mono_class_inflate_generic_type_checked (header->locals [i], context, error);
		goto_if_nok (error, fail);
	}
	if (res->num_clauses) {
		for (int i = 0; i < header->num_clauses; ++i) {
			MonoExceptionClause *clause = &res->clauses [i];
			if (clause->flags != MONO_EXCEPTION_CLAUSE_NONE)
				continue;
			clause->data.catch_class = mono_class_inflate_generic_class_checked (clause->data.catch_class, context, error);
			goto_if_nok (error, fail);
		}
	}
	return res;
fail:
	g_free (res);
	return NULL;
}

/**
 * mono_method_get_signature_full:
 * \p token is the method ref/def/spec token used in a \c call IL instruction.
 * \deprecated use the \c _checked variant
 * Notes: runtime code MUST not use this function
 */
MonoMethodSignature*
mono_method_get_signature_full (MonoMethod *method, MonoImage *image, guint32 token, MonoGenericContext *context)
{
	ERROR_DECL (error);
	MonoMethodSignature *res = mono_method_get_signature_checked (method, image, token, context, error);
	mono_error_cleanup (error);
	return res;
}

MonoMethodSignature*
mono_method_get_signature_checked (MonoMethod *method, MonoImage *image, guint32 token, MonoGenericContext *context, MonoError *error)
{
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	int sig_idx;
	guint32 cols [MONO_MEMBERREF_SIZE];
	MonoMethodSignature *sig;
	const char *ptr;

	error_init (error);

	/* !table is for wrappers: we should really assign their own token to them */
	if (!table || table == MONO_TABLE_METHOD)
		return mono_method_signature_checked (method, error);

	if (table == MONO_TABLE_METHODSPEC) {
		/* the verifier (do_invoke_method) will turn the NULL into a verifier error */
		if ((method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) || !method->is_inflated) {
			mono_error_set_bad_image (error, image, "Method is a pinvoke or open generic");
			return NULL;
		}

		return mono_method_signature_checked (method, error);
	}

	if (mono_class_is_ginst (method->klass))
		return mono_method_signature_checked (method, error);

	if (image_is_dynamic (image)) {
		sig = mono_reflection_lookup_signature (image, method, token, error);
		if (!sig)
			return NULL;
	} else {
		mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
		sig_idx = cols [MONO_MEMBERREF_SIGNATURE];

		sig = (MonoMethodSignature *)find_cached_memberref_sig (image, sig_idx);
		if (!sig) {
			if (!mono_verifier_verify_memberref_method_signature (image, sig_idx, error))
				return NULL;

			ptr = mono_metadata_blob_heap (image, sig_idx);
			mono_metadata_decode_blob_size (ptr, &ptr);

			sig = mono_metadata_parse_method_signature_full (image, NULL, 0, ptr, NULL, error);
			if (!sig)
				return NULL;

			sig = (MonoMethodSignature *)cache_memberref_sig (image, sig_idx, sig);
		}
		/* FIXME: we probably should verify signature compat in the dynamic case too*/
		if (!mono_verifier_is_sig_compatible (image, method, sig)) {
			guint32 klass = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
			const char *fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

			mono_error_set_bad_image (error, image, "Incompatible method signature class token 0x%08x field name %s token 0x%08x", klass, fname, token);
			return NULL;
		}
	}

	if (context) {
		MonoMethodSignature *cached;

		/* This signature is not owned by a MonoMethod, so need to cache */
		sig = inflate_generic_signature_checked (image, sig, context, error);
		if (!mono_error_ok (error))
			return NULL;

		cached = mono_metadata_get_inflated_signature (sig, context);
		if (cached != sig)
			mono_metadata_free_inflated_signature (sig);
		else
			mono_atomic_fetch_add_i32 (&inflated_signatures_size, mono_metadata_signature_size (cached));
		sig = cached;
	}

	g_assert (mono_error_ok (error));
	return sig;
}

/**
 * mono_method_get_signature:
 * \p token is the method_ref/def/spec token used in a call IL instruction.
 * \deprecated use the \c _checked variant
 * Notes: runtime code MUST not use this function
 */
MonoMethodSignature*
mono_method_get_signature (MonoMethod *method, MonoImage *image, guint32 token)
{
	ERROR_DECL (error);
	MonoMethodSignature *res = mono_method_get_signature_checked (method, image, token, NULL, error);
	mono_error_cleanup (error);
	return res;
}

/* this is only for the typespec array methods */
MonoMethod*
mono_method_search_in_array_class (MonoClass *klass, const char *name, MonoMethodSignature *sig)
{
	int i;

	mono_class_setup_methods (klass);
	g_assert (!mono_class_has_failure (klass)); /*FIXME this should not fail, right?*/
	int mcount = mono_class_get_method_count (klass);
	MonoMethod **klass_methods = m_class_get_methods (klass);
	for (i = 0; i < mcount; ++i) {
		MonoMethod *method = klass_methods [i];
		if (strcmp (method->name, name) == 0 && sig->param_count == method->signature->param_count)
			return method;
	}
	return NULL;
}

static MonoMethod *
method_from_memberref (MonoImage *image, guint32 idx, MonoGenericContext *typespec_context,
		       gboolean *used_context, MonoError *error)
{
	MonoClass *klass = NULL;
	MonoMethod *method = NULL;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class_index, sig_idx;
	const char *mname;
	MonoMethodSignature *sig;
	const char *ptr;

	error_init (error);

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, 3);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
	class_index = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
	/*g_print ("methodref: 0x%x 0x%x %s\n", class, nindex,
		mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]));*/

	mname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

	/*
	 * Whether we actually used the `typespec_context' or not.
	 * This is used to tell our caller whether or not it's safe to insert the returned
	 * method into a cache.
	 */
	if (used_context)
		*used_context = class_index == MONO_MEMBERREF_PARENT_TYPESPEC;

	switch (class_index) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref_checked (image, MONO_TOKEN_TYPE_REF | nindex, error);
		if (!klass)
			goto fail;
		break;
	case MONO_MEMBERREF_PARENT_TYPESPEC:
		/*
		 * Parse the TYPESPEC in the parent's context.
		 */
		klass = mono_class_get_and_inflate_typespec_checked (image, MONO_TOKEN_TYPE_SPEC | nindex, typespec_context, error);
		if (!klass)
			goto fail;
		break;
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | nindex, error);
		if (!klass)
			goto fail;
		break;
	case MONO_MEMBERREF_PARENT_METHODDEF: {
		method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | nindex, NULL, NULL, error);
		if (!method)
			goto fail;
		return method;
	}
	default:
		mono_error_set_bad_image (error, image, "Memberref parent unknown: class: %d, index %d", class_index, nindex);
		goto fail;
	}

	g_assert (klass);
	mono_class_init_internal (klass);

	sig_idx = cols [MONO_MEMBERREF_SIGNATURE];

	if (!mono_verifier_verify_memberref_method_signature (image, sig_idx, error))
		goto fail;

	ptr = mono_metadata_blob_heap (image, sig_idx);
	mono_metadata_decode_blob_size (ptr, &ptr);

	sig = (MonoMethodSignature *)find_cached_memberref_sig (image, sig_idx);
	if (!sig) {
		sig = mono_metadata_parse_method_signature_full (image, NULL, 0, ptr, NULL, error);
		if (sig == NULL)
			goto fail;

		sig = (MonoMethodSignature *)cache_memberref_sig (image, sig_idx, sig);
	}

	switch (class_index) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		method = find_method (klass, NULL, mname, sig, klass, error);
		break;

	case MONO_MEMBERREF_PARENT_TYPESPEC: {
		MonoType *type;

		type = m_class_get_byval_arg (klass);

		if (type->type != MONO_TYPE_ARRAY && type->type != MONO_TYPE_SZARRAY) {
			MonoClass *in_class = mono_class_is_ginst (klass) ? mono_class_get_generic_class (klass)->container_class : klass;
			method = find_method (in_class, NULL, mname, sig, klass, error);
			break;
		}

		/* we're an array and we created these methods already in klass in mono_class_init_internal () */
		method = mono_method_search_in_array_class (klass, mname, sig);
		break;
	}
	default:
		mono_error_set_bad_image (error, image,"Memberref parent unknown: class: %d, index %d", class_index, nindex);
		goto fail;
	}

	if (!method && mono_error_ok (error))
		mono_error_set_method_missing (error, klass, mname, sig, "Failed to load due to unknown reasons");

	return method;

fail:
	g_assert (!mono_error_ok (error));
	return NULL;
}

static MonoMethod *
method_from_methodspec (MonoImage *image, MonoGenericContext *context, guint32 idx, MonoError *error)
{
	MonoMethod *method;
	MonoClass *klass;
	MonoTableInfo *tables = image->tables;
	MonoGenericContext new_context;
	MonoGenericInst *inst;
	const char *ptr;
	guint32 cols [MONO_METHODSPEC_SIZE];
	guint32 token, nindex, param_count;

	error_init (error);

	mono_metadata_decode_row (&tables [MONO_TABLE_METHODSPEC], idx - 1, cols, MONO_METHODSPEC_SIZE);
	token = cols [MONO_METHODSPEC_METHOD];
	nindex = token >> MONO_METHODDEFORREF_BITS;

	if (!mono_verifier_verify_methodspec_signature (image, cols [MONO_METHODSPEC_SIGNATURE], error))
		return NULL;

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODSPEC_SIGNATURE]);

	mono_metadata_decode_value (ptr, &ptr);
	ptr++;
	param_count = mono_metadata_decode_value (ptr, &ptr);

	inst = mono_metadata_parse_generic_inst (image, NULL, param_count, ptr, &ptr, error);
	if (!inst)
		return NULL;

	if (context && inst->is_open) {
		inst = mono_metadata_inflate_generic_inst (inst, context, error);
		if (!mono_error_ok (error))
			return NULL;
	}

	if ((token & MONO_METHODDEFORREF_MASK) == MONO_METHODDEFORREF_METHODDEF) {
		method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | nindex, NULL, context, error);
		if (!method)
			return NULL;
	} else {
		method = method_from_memberref (image, nindex, context, NULL, error);
	}

	if (!method)
		return NULL;

	klass = method->klass;

	if (mono_class_is_ginst (klass)) {
		g_assert (method->is_inflated);
		method = ((MonoMethodInflated *) method)->declaring;
	}

	new_context.class_inst = mono_class_is_ginst (klass) ? mono_class_get_generic_class (klass)->context.class_inst : NULL;
	new_context.method_inst = inst;

	method = mono_class_inflate_generic_method_full_checked (method, klass, &new_context, error);
	return method;
}

struct _MonoDllMap {
	char *dll;
	char *target;
	char *func;
	char *target_func;
	MonoDllMap *next;
};

static MonoDllMap *global_dll_map;

static int 
mono_dllmap_lookup_list (MonoDllMap *dll_map, const char *dll, const char* func, const char **rdll, const char **rfunc) {
	int found = 0;

	*rdll = dll;

	if (!dll_map)
		return 0;

	global_loader_data_lock ();

	/* 
	 * we use the first entry we find that matches, since entries from
	 * the config file are prepended to the list and we document that the
	 * later entries win.
	 */
	for (; dll_map; dll_map = dll_map->next) {
		if (dll_map->dll [0] == 'i' && dll_map->dll [1] == ':') {
			if (g_ascii_strcasecmp (dll_map->dll + 2, dll))
				continue;
		} else if (strcmp (dll_map->dll, dll)) {
			continue;
		}
		if (!found && dll_map->target) {
			*rdll = dll_map->target;
			found = 1;
			/* we don't quit here, because we could find a full
			 * entry that matches also function and that has priority.
			 */
		}
		if (dll_map->func && strcmp (dll_map->func, func) == 0) {
			*rdll = dll_map->target;
			*rfunc = dll_map->target_func;
			break;
		}
	}

	global_loader_data_unlock ();
	return found;
}

static int 
mono_dllmap_lookup (MonoImage *assembly, const char *dll, const char* func, const char **rdll, const char **rfunc)
{
	int res;
	if (assembly && assembly->dll_map) {
		res = mono_dllmap_lookup_list (assembly->dll_map, dll, func, rdll, rfunc);
		if (res)
			return res;
	}
	return mono_dllmap_lookup_list (global_dll_map, dll, func, rdll, rfunc);
}

/**
 * mono_dllmap_insert:
 * \param assembly if NULL, this is a global mapping, otherwise the remapping of the dynamic library will only apply to the specified assembly
 * \param dll The name of the external library, as it would be found in the \c DllImport declaration.  If prefixed with <code>i:</code> the matching of the library name is done without case sensitivity
 * \param func if not null, the mapping will only applied to the named function (the value of <code>EntryPoint</code>)
 * \param tdll The name of the library to map the specified \p dll if it matches.
 * \param tfunc The name of the function that replaces the invocation.  If NULL, it is replaced with a copy of \p func.
 *
 * LOCKING: Acquires the loader lock.
 *
 * This function is used to programatically add \c DllImport remapping in either
 * a specific assembly, or as a global remapping.   This is done by remapping
 * references in a \c DllImport attribute from the \p dll library name into the \p tdll
 * name. If the \p dll name contains the prefix <code>i:</code>, the comparison of the 
 * library name is done without case sensitivity.
 *
 * If you pass \p func, this is the name of the \c EntryPoint in a \c DllImport if specified
 * or the name of the function as determined by \c DllImport. If you pass \p func, you
 * must also pass \p tfunc which is the name of the target function to invoke on a match.
 *
 * Example:
 *
 * <code>mono_dllmap_insert (NULL, "i:libdemo.dll", NULL, relocated_demo_path, NULL);</code>
 *
 * The above will remap \c DllImport statements for \c libdemo.dll and \c LIBDEMO.DLL to
 * the contents of \c relocated_demo_path for all assemblies in the Mono process.
 *
 * NOTE: This can be called before the runtime is initialized, for example from
 * \c mono_config_parse.
 */
void
mono_dllmap_insert (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	if (!assembly)
		dllmap_insert_global (dll, func, tdll, tfunc);
	else {
		MONO_ENTER_GC_UNSAFE;
		dllmap_insert_image (assembly, dll, func, tdll, tfunc);
		MONO_EXIT_GC_UNSAFE;
	}
}

void
dllmap_insert_global (const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	MonoDllMap *entry;

		mono_loader_init ();

		entry = (MonoDllMap *)g_malloc0 (sizeof (MonoDllMap));
		entry->dll = dll? g_strdup (dll): NULL;
		entry->target = tdll? g_strdup (tdll): NULL;
		entry->func = func? g_strdup (func): NULL;
		entry->target_func = tfunc? g_strdup (tfunc): (func? g_strdup (func): NULL);

		global_loader_data_lock ();
		entry->next = global_dll_map;
		global_dll_map = entry;
		global_loader_data_unlock ();
}

void
dllmap_insert_image (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	MonoDllMap *entry;
	g_assert (assembly != NULL);

		mono_loader_init ();

		entry = (MonoDllMap *)mono_image_alloc0 (assembly, sizeof (MonoDllMap));
		entry->dll = dll? mono_image_strdup (assembly, dll): NULL;
		entry->target = tdll? mono_image_strdup (assembly, tdll): NULL;
		entry->func = func? mono_image_strdup (assembly, func): NULL;
		entry->target_func = tfunc? mono_image_strdup (assembly, tfunc): (func? mono_image_strdup (assembly, func): NULL);

		mono_image_lock (assembly);
		entry->next = assembly->dll_map;
		assembly->dll_map = entry;
		mono_image_unlock (assembly);
}

static void
free_dllmap (MonoDllMap *map)
{
	while (map) {
		MonoDllMap *next = map->next;

		g_free (map->dll);
		g_free (map->target);
		g_free (map->func);
		g_free (map->target_func);
		g_free (map);
		map = next;
	}
}

static void
dllmap_cleanup (void)
{
	free_dllmap (global_dll_map);
	global_dll_map = NULL;
}

static GHashTable *global_module_map;

static MonoDl*
cached_module_load (const char *name, int flags, char **err)
{
	MonoDl *res;

	if (err)
		*err = NULL;
	global_loader_data_lock ();
	if (!global_module_map)
		global_module_map = g_hash_table_new (g_str_hash, g_str_equal);
	res = (MonoDl *)g_hash_table_lookup (global_module_map, name);
	if (res) {
		global_loader_data_unlock ();
		return res;
	}
	res = mono_dl_open (name, flags, err);
	if (res)
		g_hash_table_insert (global_module_map, g_strdup (name), res);
	global_loader_data_unlock ();
	return res;
}

void
mono_loader_register_module (const char *name, MonoDl *module)
{
	if (!global_module_map)
		global_module_map = g_hash_table_new (g_str_hash, g_str_equal);
	g_hash_table_insert (global_module_map, g_strdup (name), module);
}

static void
remove_cached_module(gpointer key, gpointer value, gpointer user_data)
{
	mono_dl_close((MonoDl*)value);
}

static void
cached_module_cleanup(void)
{
	if (global_module_map != NULL) {
		g_hash_table_foreach(global_module_map, remove_cached_module, NULL);

		g_hash_table_destroy(global_module_map);
		global_module_map = NULL;
	}
}

static MonoDl *internal_module;

static gboolean
is_absolute_path (const char *path)
{
#ifdef HOST_DARWIN
	if (!strncmp (path, "@executable_path/", 17) || !strncmp (path, "@loader_path/", 13) ||
	    !strncmp (path, "@rpath/", 7))
	    return TRUE;
#endif
	return g_path_is_absolute (path);
}

typedef enum {
	LOOKUP_PINVOKE_ERR_OK = 0, /* No error */
	LOOKUP_PINVOKE_ERR_NO_LIB, /* DllNotFoundException */
	LOOKUP_PINVOKE_ERR_NO_SYM, /* EntryPointNotFoundException */
} MonoLookupPInvokeErr;

/* We should just use a MonoError, but mono_lookup_pinvoke_call has this legacy
 * error reporting mechanism where it returns an exception class and a string
 * message.  So instead we return an error code and message, and for internal
 * callers convert it to a MonoError.
 *
 * Don't expose this type to the runtime.  It's just an implementation
 * detail for backward compatability.
 */
typedef struct MonoLookupPInvokeStatus {
	MonoLookupPInvokeErr err_code;
	char *err_arg;
} MonoLookupPInvokeStatus;

static gpointer
lookup_pinvoke_call_impl (MonoMethod *method, MonoLookupPInvokeStatus *status_out);

static void
pinvoke_probe_convert_status_for_api (MonoLookupPInvokeStatus *status, const char **exc_class, const char **exc_arg)
{
	if (!exc_class)
		return;
	switch (status->err_code) {
	case LOOKUP_PINVOKE_ERR_OK:
		*exc_class = NULL;
		*exc_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_LIB:
		*exc_class = "DllNotFoundException";
		*exc_arg = status->err_arg;
		status->err_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_SYM:
		*exc_class = "EntryPointNotFoundException";
		*exc_arg = status->err_arg;
		status->err_arg = NULL;
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
pinvoke_probe_convert_status_to_error (MonoLookupPInvokeStatus *status, MonoError *error)
{
	/* Note: this has to return a MONO_ERROR_GENERIC because mono_mb_emit_exception_for_error only knows how to decode generic errors. */
	switch (status->err_code) {
	case LOOKUP_PINVOKE_ERR_OK:
		return;
	case LOOKUP_PINVOKE_ERR_NO_LIB:
		mono_error_set_generic_error (error, "System", "DllNotFoundException", "%s", status->err_arg);
		g_free (status->err_arg);
		status->err_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_SYM:
		mono_error_set_generic_error (error, "System", "EntryPointNotFoundException", "%s", status->err_arg);
		g_free (status->err_arg);
		status->err_arg = NULL;
		break;
	default:
		g_assert_not_reached ();
	}
}

/**
 * mono_lookup_pinvoke_call:
 */
gpointer
mono_lookup_pinvoke_call (MonoMethod *method, const char **exc_class, const char **exc_arg)
{
	gpointer result;
	MONO_ENTER_GC_UNSAFE;
	MonoLookupPInvokeStatus status;
	memset (&status, 0, sizeof (status));
	result = lookup_pinvoke_call_impl (method, &status);
	pinvoke_probe_convert_status_for_api (&status, exc_class, exc_arg);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

static MonoDl*
pinvoke_probe_for_module (MonoImage *image, const char*new_scope, const char *import, char **found_name_out, char **error_msg_out);

static MonoDl*
pinvoke_probe_for_module_relative_directories (MonoImage *image, const char *file_name, char **found_name_out);

static gpointer
pinvoke_probe_for_symbol (MonoDl *module, MonoMethodPInvoke *piinfo, const char *import, char **error_msg_out);

gpointer
mono_lookup_pinvoke_call_internal (MonoMethod *method, MonoError *error)
{
	gpointer result;
	MonoLookupPInvokeStatus status;
	memset (&status, 0, sizeof (status));
	result = lookup_pinvoke_call_impl (method, &status);
	if (status.err_code)
		pinvoke_probe_convert_status_to_error (&status, error);
	return result;
}

gpointer
lookup_pinvoke_call_impl (MonoMethod *method, MonoLookupPInvokeStatus *status_out)
{
	MonoImage *image = m_class_get_image (method->klass);
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)method;
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [MONO_IMPLMAP_SIZE];
	guint32 scope_token;
	const char *import = NULL;
	const char *orig_scope;
	const char *new_scope;
	char *error_msg;
	char *found_name = NULL;
	MonoDl *module = NULL;
	gboolean cached = FALSE;
	gpointer addr = NULL;

	g_assert (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL);

	g_assert (status_out);

	if (piinfo->addr)
		return piinfo->addr;

	if (image_is_dynamic (m_class_get_image (method->klass))) {
		MonoReflectionMethodAux *method_aux = 
			(MonoReflectionMethodAux *)g_hash_table_lookup (
				((MonoDynamicImage*)m_class_get_image (method->klass))->method_aux_hash, method);
		if (!method_aux)
			return NULL;

		import = method_aux->dllentry;
		orig_scope = method_aux->dll;
	}
	else {
		if (!piinfo->implmap_idx || piinfo->implmap_idx > im->rows)
			return NULL;

		mono_metadata_decode_row (im, piinfo->implmap_idx - 1, im_cols, MONO_IMPLMAP_SIZE);

		if (!im_cols [MONO_IMPLMAP_SCOPE] || im_cols [MONO_IMPLMAP_SCOPE] > mr->rows)
			return NULL;

		piinfo->piflags = im_cols [MONO_IMPLMAP_FLAGS];
		import = mono_metadata_string_heap (image, im_cols [MONO_IMPLMAP_NAME]);
		scope_token = mono_metadata_decode_row_col (mr, im_cols [MONO_IMPLMAP_SCOPE] - 1, MONO_MODULEREF_NAME);
		orig_scope = mono_metadata_string_heap (image, scope_token);
	}

#ifndef ENABLE_NETCORE
	// FIXME: The dllmap remaps System.Native to mono-native
	mono_dllmap_lookup (image, orig_scope, import, &new_scope, &import);
#else
	/* AK: FIXME: dllmap, above doesn't strdup the results, so these leak
	 * since there's no free() */
	new_scope = g_strdup (orig_scope);
	import = g_strdup (import);
#endif

	if (!module) {
		mono_image_lock (image);
		if (!image->pinvoke_scopes) {
			image->pinvoke_scopes = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
			image->pinvoke_scope_filenames = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
		}
		module = (MonoDl *)g_hash_table_lookup (image->pinvoke_scopes, new_scope);
		found_name = (char *)g_hash_table_lookup (image->pinvoke_scope_filenames, new_scope);
		mono_image_unlock (image);
		if (module)
			cached = TRUE;
		if (found_name)
			found_name = g_strdup (found_name);
	}

	if (!module)
		module = pinvoke_probe_for_module (image, new_scope, import, &found_name, &error_msg);

	if (!module) {
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DLLIMPORT,
				"DllImport unable to load library '%s'.",
				error_msg);
		g_free (error_msg);

		status_out->err_code = LOOKUP_PINVOKE_ERR_NO_LIB;
		status_out->err_arg = g_strdup (new_scope);
		return NULL;
	}

	if (!cached) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loaded library '%s'.", found_name);
		mono_image_lock (image);
		if (!g_hash_table_lookup (image->pinvoke_scopes, new_scope)) {
			g_hash_table_insert (image->pinvoke_scopes, g_strdup (new_scope), module);
			g_hash_table_insert (image->pinvoke_scope_filenames, g_strdup (new_scope), g_strdup (found_name));
		}
		mono_image_unlock (image);
	}

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
				"DllImport searching in: '%s' ('%s').", new_scope, found_name);
	g_free (found_name);

	addr = pinvoke_probe_for_symbol (module, piinfo, import, &error_msg);

	if (!addr) {
		g_free (error_msg);
		status_out->err_code = LOOKUP_PINVOKE_ERR_NO_SYM;
		status_out->err_arg = g_strdup (import);
		return NULL;
	}
	piinfo->addr = addr;
	return addr;
}

/**
 * pinvoke_probe_transform_path:
 *
 * Try transforming the library path given in \p new_scope in different ways
 * depending on \p phase
 *
 * \returns \c TRUE if a transformation was applied and the transformed path
 * components are written to the out arguments, or \c FALSE if a transformation
 * did not apply.
 */
static gboolean
pinvoke_probe_transform_path (const char *new_scope, int phase, char **file_name_out, char **base_name_out, char **dir_name_out, gboolean *is_absolute_out)
{
	char *file_name = NULL, *base_name = NULL, *dir_name = NULL;
	gboolean changed = FALSE;
	gboolean is_absolute = is_absolute_path (new_scope);
	switch (phase) {
	case 0:
		/* Try the original name */
		file_name = g_strdup (new_scope);
		changed = TRUE;
		break;
	case 1:
		/* Try trimming the .dll extension */
		if (strstr (new_scope, ".dll") == (new_scope + strlen (new_scope) - 4)) {
			file_name = g_strdup (new_scope);
			file_name [strlen (new_scope) - 4] = '\0';
			changed = TRUE;
		}
		break;
	case 2:
		if (is_absolute) {
			dir_name = g_path_get_dirname (new_scope);
			base_name = g_path_get_basename (new_scope);
			if (strstr (base_name, "lib") != base_name) {
				char *tmp = g_strdup_printf ("lib%s", base_name);       
				g_free (base_name);
				base_name = tmp;
				file_name = g_strdup_printf ("%s%s%s", dir_name, G_DIR_SEPARATOR_S, base_name);
				changed = TRUE;
			}
		} else if (strstr (new_scope, "lib") != new_scope) {
			file_name = g_strdup_printf ("lib%s", new_scope);
			changed = TRUE;
		}
		break;
	case 3:
		if (!is_absolute && mono_dl_get_system_dir ()) {
			dir_name = (char*)mono_dl_get_system_dir ();
			file_name = g_path_get_basename (new_scope);
			base_name = NULL;
			changed = TRUE;
		}
		break;
	default:
#ifndef TARGET_WIN32
		if (!g_ascii_strcasecmp ("user32.dll", new_scope) ||
		    !g_ascii_strcasecmp ("kernel32.dll", new_scope) ||
		    !g_ascii_strcasecmp ("user32", new_scope) ||
		    !g_ascii_strcasecmp ("kernel", new_scope)) {
			file_name = g_strdup ("libMonoSupportW.so");
			changed = TRUE;
		}
#endif
		break;
	}
	if (changed && is_absolute) {
		if (!dir_name)
			dir_name = g_path_get_dirname (file_name);
		if (!base_name)
			base_name = g_path_get_basename (file_name);
	}
	*file_name_out = file_name;
	*base_name_out = base_name;
	*dir_name_out = dir_name;
	*is_absolute_out = is_absolute;
	return changed;
}

static MonoDl*
pinvoke_probe_for_module (MonoImage *image, const char*new_scope, const char *import, char **found_name_out, char **error_msg_out)
{
	char *full_name, *file_name;
	char *error_msg = NULL;
	char *found_name = NULL;
	int i;
	MonoDl *module = NULL;

	g_assert (found_name_out);
	g_assert (error_msg_out);

	if (!module) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport attempting to load: '%s'.", new_scope);

		/* we allow a special name to dlopen from the running process namespace */
		if (strcmp (new_scope, "__Internal") == 0){
			if (internal_module == NULL)
				internal_module = mono_dl_open (NULL, MONO_DL_LAZY, &error_msg);
			module = internal_module;
		}
	}

	/*
	 * Try loading the module using a variety of names
	 */
	for (i = 0; i < 5; ++i) {
		char *base_name = NULL, *dir_name = NULL;
		gboolean is_absolute;

		gboolean changed = pinvoke_probe_transform_path (new_scope, i, &file_name, &base_name, &dir_name, &is_absolute);
		if (!changed)
			continue;
		
		if (!module && is_absolute) {
			module = cached_module_load (file_name, MONO_DL_LAZY, &error_msg);
			if (!module) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s': '%s'.",
							file_name, error_msg);
				g_free (error_msg);
			} else {
				found_name = g_strdup (file_name);
			}
		}

		if (!module && !is_absolute) {
			module = pinvoke_probe_for_module_relative_directories (image, file_name, &found_name);
		}

		if (!module) {
			void *iter = NULL;
			char *file_or_base = is_absolute ? base_name : file_name;
			while ((full_name = mono_dl_build_path (dir_name, file_or_base, &iter))) {
				module = cached_module_load (full_name, MONO_DL_LAZY, &error_msg);
				if (!module) {
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
							"DllImport error loading library '%s': '%s'.",
								full_name, error_msg);
					g_free (error_msg);
				} else {
					found_name = g_strdup (full_name);
				}
				g_free (full_name);
				if (module)
					break;
			}
		}

		if (!module) {
			module = cached_module_load (file_name, MONO_DL_LAZY, &error_msg);
			if (!module) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s': '%s'.",
							file_name, error_msg);
			} else {
				found_name = g_strdup (file_name);
			}
		}

		g_free (file_name);
		if (is_absolute) {
			g_free (base_name);
			g_free (dir_name);
		}

		if (module)
			break;
	}

	*found_name_out = found_name;
	*error_msg_out = error_msg;
	return module;
}

#if ENABLE_NETCORE
void
mono_set_pinvoke_search_directories (int dir_count, char **dirs)
{
	pinvoke_search_directories_count = dir_count;
	pinvoke_search_directories = dirs;
}
#endif

static MonoDl*
pinvoke_probe_for_module_in_directory (const char *mdirname, const char *file_name, char **found_name_out)
{
	void *iter = NULL;
	char *full_name;
	MonoDl* module = NULL;

	while ((full_name = mono_dl_build_path (mdirname, file_name, &iter)) && module == NULL) {
		char *error_msg;
		module = cached_module_load (full_name, MONO_DL_LAZY, &error_msg);
		if (!module) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT, "DllImport error loading library '%s': '%s'.", full_name, error_msg);
			g_free (error_msg);
		} else {
			*found_name_out = g_strdup (full_name);
		}
		g_free (full_name);
	}
	g_free (full_name);

	return module;
}

static MonoDl*
pinvoke_probe_for_module_relative_directories (MonoImage *image, const char *file_name, char **found_name_out)
{
	char *found_name = NULL;
	MonoDl* module = NULL;

	g_assert (found_name_out);

#if ENABLE_NETCORE
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "netcore DllImport handler: wanted '%s'", file_name);

	// Search in predefined directories first
	for (int j = 0; j < pinvoke_search_directories_count && module == NULL; ++j) {
		module = pinvoke_probe_for_module_in_directory (pinvoke_search_directories[j], file_name, &found_name);
	}

	// Fallback to image directory
	if (module == NULL) {
		// TODO: Check DefaultDllImportSearchPathsAttribute, NativeLibrary callback
		char *mdirname = g_path_get_dirname (image->name);
		if (mdirname)
			module = pinvoke_probe_for_module_in_directory (mdirname, file_name, &found_name);
		g_free (mdirname);
	}
#else
			for (int j = 0; j < 3; ++j) {
				char *mdirname = NULL;
				switch (j) {
					case 0:
						mdirname = g_path_get_dirname (image->name);
						break;
					case 1: /* @executable_path@/../lib */
					{
						char buf [4096];
						int binl;
						binl = mono_dl_get_executable_path (buf, sizeof (buf));
						if (binl != -1) {
							char *base, *newbase;
							char *resolvedname;
							buf [binl] = 0;
							resolvedname = mono_path_resolve_symlinks (buf);

							base = g_path_get_dirname (resolvedname);
							newbase = g_path_get_dirname(base);

							// On Android the executable for the application is going to be /system/bin/app_process{32,64} depending on
							// the application's architecture. However, libraries for the different architectures live in different
							// subdirectories of `/system`: `lib` for 32-bit apps and `lib64` for 64-bit ones. Thus appending `/lib` below
							// will fail to load the DSO for a 64-bit app, even if it exists there, because it will have a different
							// architecture. This is the cause of https://github.com/xamarin/xamarin-android/issues/2780 and the ifdef
							// below is the fix.
							mdirname = g_strdup_printf (
#if defined(TARGET_ANDROID) && (defined(TARGET_ARM64) || defined(TARGET_AMD64))
									"%s/lib64",
#else
									"%s/lib",
#endif
									newbase);
							g_free (resolvedname);
							g_free (base);
							g_free (newbase);
						}
						break;
					}
#ifdef __MACH__
					case 2: /* @executable_path@/../Libraries */
					{
						char buf [4096];
						int binl;
						binl = mono_dl_get_executable_path (buf, sizeof (buf));
						if (binl != -1) {
							char *base, *newbase;
							char *resolvedname;
							buf [binl] = 0;
							resolvedname = mono_path_resolve_symlinks (buf);

							base = g_path_get_dirname (resolvedname);
							newbase = g_path_get_dirname(base);
							mdirname = g_strdup_printf ("%s/Libraries", newbase);

							g_free (resolvedname);
							g_free (base);
							g_free (newbase);
						}
						break;
					}
#endif
				}

				if (!mdirname)
					continue;

				module = pinvoke_probe_for_module_in_directory (mdirname, file_name, &found_name);
				g_free (mdirname);
				if (module)
					break;
			}
#endif

	*found_name_out = found_name;
	return module;
}


static gpointer
pinvoke_probe_for_symbol (MonoDl *module, MonoMethodPInvoke *piinfo, const char *import, char **error_msg_out)
{
	char *error_msg = NULL;
	gpointer addr = NULL;

	g_assert (error_msg_out);

#ifdef HOST_WIN32
	if (import && import [0] == '#' && isdigit (import [1])) {
		char *end;
		long id;

		id = strtol (import + 1, &end, 10);
		if (id > 0 && *end == '\0')
			import++;
	}
#endif
	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
				"Searching for '%s'.", import);

	if (piinfo->piflags & PINVOKE_ATTRIBUTE_NO_MANGLE) {
		error_msg = mono_dl_symbol (module, import, &addr); 
	} else {
		/*
		 * Search using a variety of mangled names
		 */
		for (int mangle_stdcall = 0; mangle_stdcall <= 1 && addr == NULL; mangle_stdcall++) {
#if HOST_WIN32 && HOST_X86
			const int max_managle_param_count = (mangle_stdcall == 0) ? 0 : 256;
#else
			const int max_managle_param_count = 0;
#endif
			for (int mangle_charset = 0; mangle_charset <= 1 && addr == NULL; mangle_charset ++) {
				for (int mangle_param_count = 0; mangle_param_count <= max_managle_param_count && addr == NULL; mangle_param_count += 4) {

					char *mangled_name = (char*)import;
					switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
					case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
						/* Try the mangled name first */
						if (mangle_charset == 0)
							mangled_name = g_strconcat (import, "W", NULL);
						break;
					case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
#ifdef HOST_WIN32
						if (mangle_charset == 0)
							mangled_name = g_strconcat (import, "W", NULL);
#else
						/* Try the mangled name last */
						if (mangle_charset == 1)
							mangled_name = g_strconcat (import, "A", NULL);
#endif
						break;
					case PINVOKE_ATTRIBUTE_CHAR_SET_ANSI:
					default:
						/* Try the mangled name last */
						if (mangle_charset == 1)
							mangled_name = g_strconcat (import, "A", NULL);
						break;
					}

#if HOST_WIN32 && HOST_X86
					/* Try the stdcall mangled name */
					/* 
					 * gcc under windows creates mangled names without the underscore, but MS.NET
					 * doesn't support it, so we doesn't support it either.
					 */
					if (mangle_stdcall == 1) {
						MonoMethod *method = &piinfo->method;
						int param_count;
						if (mangle_param_count == 0)
							param_count = mono_method_signature_internal (method)->param_count * sizeof (gpointer);
						else
							/* Try brute force, since it would be very hard to compute the stack usage correctly */
							param_count = mangle_param_count;

						char *mangled_stdcall_name = g_strdup_printf ("_%s@%d", mangled_name, param_count);

						if (mangled_name != import)
							g_free (mangled_name);

						mangled_name = mangled_stdcall_name;
					}
#endif
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
								"Probing '%s'.", mangled_name);

					error_msg = mono_dl_symbol (module, mangled_name, &addr);

					if (addr)
						mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
									"Found as '%s'.", mangled_name);
					else
						mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
									"Could not find '%s' due to '%s'.", mangled_name, error_msg);

					g_free (error_msg);
					error_msg = NULL;

					if (mangled_name != import)
						g_free (mangled_name);
				}
			}
		}
	}

	*error_msg_out = error_msg;
	return addr;
}

/*
 * LOCKING: assumes the loader lock to be taken.
 */
static MonoMethod *
mono_get_method_from_token (MonoImage *image, guint32 token, MonoClass *klass,
			    MonoGenericContext *context, gboolean *used_context, MonoError *error)
{
	MonoMethod *result;
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	MonoGenericContainer *generic_container = NULL, *container = NULL;
	const char *sig = NULL;
	guint32 cols [MONO_TYPEDEF_SIZE];

	error_init (error);

	if (image_is_dynamic (image)) {
		MonoClass *handle_class;

		result = (MonoMethod *)mono_lookup_dynamic_token_class (image, token, TRUE, &handle_class, context, error);
		return_val_if_nok (error, NULL);

		// This checks the memberref type as well
		if (result && handle_class != mono_defaults.methodhandle_class) {
			mono_error_set_bad_image (error, image, "Bad method token 0x%08x on dynamic image", token);
			return NULL;
		}
		return result;
	}

	if (table != MONO_TABLE_METHOD) {
		if (table == MONO_TABLE_METHODSPEC) {
			if (used_context) *used_context = TRUE;
			return method_from_methodspec (image, context, idx, error);
		}
		if (table != MONO_TABLE_MEMBERREF) {
			mono_error_set_bad_image (error, image, "Bad method token 0x%08x.", token);
			return NULL;
		}
		return method_from_memberref (image, idx, context, used_context, error);
	}

	if (used_context) *used_context = FALSE;

	if (idx > image->tables [MONO_TABLE_METHOD].rows) {
		mono_error_set_bad_image (error, image, "Bad method token 0x%08x (out of bounds).", token);
		return NULL;
	}

	if (!klass) {
		guint32 type = mono_metadata_typedef_from_method (image, token);
		if (!type) {
			mono_error_set_bad_image (error, image, "Bad method token 0x%08x (could not find corresponding typedef).", token);
			return NULL;
		}
		klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | type, error);
		if (klass == NULL)
			return NULL;
	}

	mono_metadata_decode_row (&image->tables [MONO_TABLE_METHOD], idx - 1, cols, 6);

	if ((cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
		result = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethodPInvoke));
	} else {
		result = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethod));
		mono_atomic_fetch_add_i32 (&methods_size, sizeof (MonoMethod));
	}

	mono_atomic_inc_i32 (&mono_stats.method_count);

	result->slot = -1;
	result->klass = klass;
	result->flags = cols [2];
	result->iflags = cols [1];
	result->token = token;
	result->name = mono_metadata_string_heap (image, cols [3]);

	/* If a method is abstract and marked as an icall, silently ignore the
	 * icall attribute so that we don't later emit a warning that the icall
	 * can't be found.
	 */
	if ((result->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
	    (result->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		result->iflags &= ~METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [4]);
	/* size = */ mono_metadata_decode_blob_size (sig, &sig);

	container = mono_class_try_get_generic_container (klass);

	/* 
	 * load_generic_params does a binary search so only call it if the method 
	 * is generic.
	 */
	if (*sig & 0x10) {
		generic_container = mono_metadata_load_generic_params (image, token, container, result);
	}
	if (generic_container) {
		result->is_generic = TRUE;
		/*FIXME put this before the image alloc*/
		if (!mono_metadata_load_generic_param_constraints_checked (image, token, generic_container, error))
			return NULL;

		container = generic_container;
	}

	if (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (result->klass == mono_defaults.string_class && !strcmp (result->name, ".ctor"))
			result->string_ctor = 1;
	} else if (cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)result;

#ifdef TARGET_WIN32
		/* IJW is P/Invoke with a predefined function pointer. */
		if (m_image_is_module_handle (image) && (cols [1] & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
			piinfo->addr = mono_image_rva_map (image, cols [0]);
			g_assert (piinfo->addr);
		}
#endif
		piinfo->implmap_idx = mono_metadata_implmap_from_method (image, idx - 1);
		/* Native methods can have no map. */
		if (piinfo->implmap_idx)
			piinfo->piflags = mono_metadata_decode_row_col (&tables [MONO_TABLE_IMPLMAP], piinfo->implmap_idx - 1, MONO_IMPLMAP_FLAGS);
	}

 	if (generic_container)
 		mono_method_set_generic_container (result, generic_container);

	return result;
}

/**
 * mono_get_method:
 */
MonoMethod *
mono_get_method (MonoImage *image, guint32 token, MonoClass *klass)
{
	ERROR_DECL (error);
	MonoMethod *result = mono_get_method_checked (image, token, klass, NULL, error);
	mono_error_cleanup (error);
	return result;
}

/**
 * mono_get_method_full:
 */
MonoMethod *
mono_get_method_full (MonoImage *image, guint32 token, MonoClass *klass,
		      MonoGenericContext *context)
{
	ERROR_DECL (error);
	MonoMethod *result = mono_get_method_checked (image, token, klass, context, error);
	mono_error_cleanup (error);
	return result;
}

MonoMethod *
mono_get_method_checked (MonoImage *image, guint32 token, MonoClass *klass, MonoGenericContext *context, MonoError *error)
{
	MonoMethod *result = NULL;
	gboolean used_context = FALSE;

	/* We do everything inside the lock to prevent creation races */

	error_init (error);

	mono_image_lock (image);

	if (mono_metadata_token_table (token) == MONO_TABLE_METHOD) {
		if (!image->method_cache)
			image->method_cache = g_hash_table_new (NULL, NULL);
		result = (MonoMethod *)g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (mono_metadata_token_index (token)));
	} else if (!image_is_dynamic (image)) {
		if (!image->methodref_cache)
			image->methodref_cache = g_hash_table_new (NULL, NULL);
		result = (MonoMethod *)g_hash_table_lookup (image->methodref_cache, GINT_TO_POINTER (token));
	}
	mono_image_unlock (image);

	if (result)
		return result;


	result = mono_get_method_from_token (image, token, klass, context, &used_context, error);
	if (!result)
		return NULL;

	mono_image_lock (image);
	if (!used_context && !result->is_inflated) {
		MonoMethod *result2 = NULL;

		if (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
			result2 = (MonoMethod *)g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (mono_metadata_token_index (token)));
		else if (!image_is_dynamic (image))
			result2 = (MonoMethod *)g_hash_table_lookup (image->methodref_cache, GINT_TO_POINTER (token));

		if (result2) {
			mono_image_unlock (image);
			return result2;
		}

		if (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
			g_hash_table_insert (image->method_cache, GINT_TO_POINTER (mono_metadata_token_index (token)), result);
		else if (!image_is_dynamic (image))
			g_hash_table_insert (image->methodref_cache, GINT_TO_POINTER (token), result);
	}

	mono_image_unlock (image);

	return result;
}

static MonoMethod*
get_method_constrained (MonoImage *image, MonoMethod *method, MonoClass *constrained_class, MonoGenericContext *context, MonoError *error)
{
	MonoClass *base_class = method->klass;

	error_init (error);

	if (!mono_class_is_assignable_from_internal (base_class, constrained_class)) {
		char *base_class_name = mono_type_get_full_name (base_class);
		char *constrained_class_name = mono_type_get_full_name (constrained_class);
		mono_error_set_invalid_operation (error, "constrained call: %s is not assignable from %s", base_class_name, constrained_class_name);
		g_free (base_class_name);
		g_free (constrained_class_name);
		return NULL;
	}

	/* If the constraining class is actually an interface, we don't learn
	 * anything new by constraining.
	 */
	if (MONO_CLASS_IS_INTERFACE_INTERNAL (constrained_class))
		return method;

	mono_class_setup_vtable (base_class);
	if (mono_class_has_failure (base_class)) {
		mono_error_set_for_class_failure (error, base_class);
		return NULL;
	}

	MonoGenericContext inflated_method_ctx;
	memset (&inflated_method_ctx, 0, sizeof (inflated_method_ctx));
	inflated_method_ctx.class_inst = NULL;
	inflated_method_ctx.method_inst = NULL;
	gboolean inflated_generic_method = FALSE;
	if (method->is_inflated) {
		MonoGenericContext *method_ctx = mono_method_get_context (method);
		/* If method is an instantiation of a generic method definition, ie
		 *   class H<T>  { void M<U> (...) { ... } }
		 * and method is H<C>.M<D>
		 * we will get at the end a refined HSubclass<...>.M<U> and we will need to re-instantiate it with D.
		 * to get HSubclass<...>.M<D>
		 *
		 */
		if (method_ctx->method_inst != NULL) {
			inflated_generic_method = TRUE;
			inflated_method_ctx.method_inst = method_ctx->method_inst;
		}
	}
	int vtable_slot = 0;
	if (!MONO_CLASS_IS_INTERFACE_INTERNAL (base_class)) {
		/*if the base class isn't an interface and the method isn't
		 * virtual, there's nothing to do, we're already on the method
		 * we want to call. */
		if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) == 0)
			return method;
		/* if this isn't an interface method, get the vtable slot and
		 * find the corresponding method in the constrained class,
		 * which is a subclass of the base class. */
		vtable_slot = mono_method_get_vtable_index (method);

		mono_class_setup_vtable (constrained_class);
		if (mono_class_has_failure (constrained_class)) {
			mono_error_set_for_class_failure (error, constrained_class);
			return NULL;
		}
	} else {
		if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) == 0)
			return method;
		mono_class_setup_vtable (constrained_class);
		if (mono_class_has_failure (constrained_class)) {
			mono_error_set_for_class_failure (error, constrained_class);
			return NULL;
		}
			
		/* Get the slot of the method in the interface.  Then get the
		 * interface base in constrained_class */
		int itf_slot = mono_method_get_vtable_index (method);
		g_assert (itf_slot >= 0);
		gboolean variant = FALSE;
		int itf_base = mono_class_interface_offset_with_variance (constrained_class, base_class, &variant);
		vtable_slot = itf_slot + itf_base;
	}
	g_assert (vtable_slot >= 0);

	MonoMethod *res = mono_class_get_vtable_entry (constrained_class, vtable_slot);
	if (res == NULL && mono_class_is_abstract (constrained_class) ) {
		/* Constraining class is abstract, there may not be a refined method. */
		return method;
	}
	g_assert (res != NULL);
	if (inflated_generic_method) {
		g_assert (res->is_generic || res->is_inflated);
		res = mono_class_inflate_generic_method_checked (res, &inflated_method_ctx, error);
		return_val_if_nok (error, NULL);
	}
	return res;
}

MonoMethod *
mono_get_method_constrained_with_method (MonoImage *image, MonoMethod *method, MonoClass *constrained_class,
			     MonoGenericContext *context, MonoError *error)
{
	g_assert (method);

	return get_method_constrained (image, method, constrained_class, context, error);
}

/**
 * mono_get_method_constrained:
 * This is used when JITing the <code>constrained.</code> opcode.
 * \returns The contrained method, which has been inflated
 * as the function return value; and the original CIL-stream method as
 * declared in \p cil_method. The latter is used for verification.
 */
MonoMethod *
mono_get_method_constrained (MonoImage *image, guint32 token, MonoClass *constrained_class,
			     MonoGenericContext *context, MonoMethod **cil_method)
{
	ERROR_DECL (error);
	MonoMethod *result = mono_get_method_constrained_checked (image, token, constrained_class, context, cil_method, error);
	mono_error_cleanup (error);
	return result;
}

MonoMethod *
mono_get_method_constrained_checked (MonoImage *image, guint32 token, MonoClass *constrained_class, MonoGenericContext *context, MonoMethod **cil_method, MonoError *error)
{
	error_init (error);

	*cil_method = mono_get_method_checked (image, token, NULL, context, error);
	if (!*cil_method)
		return NULL;

	return get_method_constrained (image, *cil_method, constrained_class, context, error);
}

/**
 * mono_free_method:
 */
void
mono_free_method  (MonoMethod *method)
{
	MONO_PROFILER_RAISE (method_free, (method));
	
	/* FIXME: This hack will go away when the profiler will support freeing methods */
	if (G_UNLIKELY (mono_profiler_installed ()))
		return;
	
	if (method->signature) {
		/* 
		 * FIXME: This causes crashes because the types inside signatures and
		 * locals are shared.
		 */
		/* mono_metadata_free_method_signature (method->signature); */
		/* g_free (method->signature); */
	}
	
	if (method_is_dynamic (method)) {
		MonoMethodWrapper *mw = (MonoMethodWrapper*)method;
		int i;

		mono_marshal_free_dynamic_wrappers (method);

		mono_image_property_remove (m_class_get_image (method->klass), method);

		g_free ((char*)method->name);
		if (mw->header) {
			g_free ((char*)mw->header->code);
			for (i = 0; i < mw->header->num_locals; ++i)
				g_free (mw->header->locals [i]);
			g_free (mw->header->clauses);
			g_free (mw->header);
		}
		g_free (mw->method_data);
		g_free (method->signature);
		g_free (method);
	}
}

/**
 * mono_method_get_param_names:
 */
void
mono_method_get_param_names (MonoMethod *method, const char **names)
{
	int i, lastp;
	MonoClass *klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;
	MonoMethodSignature *signature;
	guint32 idx;

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	signature = mono_method_signature_internal (method);
	/*FIXME this check is somewhat redundant since the caller usally will have to get the signature to figure out the
	  number of arguments and allocate a properly sized array. */
	if (signature == NULL)
		return;

	if (!signature->param_count)
		return;

	for (i = 0; i < signature->param_count; ++i)
		names [i] = "";

	klass = method->klass;
	if (m_class_get_rank (klass))
		return;

	mono_class_init_internal (klass);

	MonoImage *klass_image = m_class_get_image (klass);
	if (image_is_dynamic (klass_image)) {
		MonoReflectionMethodAux *method_aux = 
			(MonoReflectionMethodAux *)g_hash_table_lookup (
				((MonoDynamicImage*)m_class_get_image (method->klass))->method_aux_hash, method);
		if (method_aux && method_aux->param_names) {
			for (i = 0; i < mono_method_signature_internal (method)->param_count; ++i)
				if (method_aux->param_names [i + 1])
					names [i] = method_aux->param_names [i + 1];
		}
		return;
	}

	if (method->wrapper_type) {
		char **pnames = NULL;

		mono_image_lock (klass_image);
		if (klass_image->wrapper_param_names)
			pnames = (char **)g_hash_table_lookup (klass_image->wrapper_param_names, method);
		mono_image_unlock (klass_image);

		if (pnames) {
			for (i = 0; i < signature->param_count; ++i)
				names [i] = pnames [i];
		}
		return;
	}

	methodt = &klass_image->tables [MONO_TABLE_METHOD];
	paramt = &klass_image->tables [MONO_TABLE_PARAM];
	idx = mono_method_get_index (method);
	if (idx > 0) {
		guint32 cols [MONO_PARAM_SIZE];
		guint param_index;

		param_index = mono_metadata_decode_row_col (methodt, idx - 1, MONO_METHOD_PARAMLIST);

		if (idx < methodt->rows)
			lastp = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);
		else
			lastp = paramt->rows + 1;
		for (i = param_index; i < lastp; ++i) {
			mono_metadata_decode_row (paramt, i -1, cols, MONO_PARAM_SIZE);
			if (cols [MONO_PARAM_SEQUENCE] && cols [MONO_PARAM_SEQUENCE] <= signature->param_count) /* skip return param spec and bounds check*/
				names [cols [MONO_PARAM_SEQUENCE] - 1] = mono_metadata_string_heap (klass_image, cols [MONO_PARAM_NAME]);
		}
	}
}

/**
 * mono_method_get_param_token:
 */
guint32
mono_method_get_param_token (MonoMethod *method, int index)
{
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	guint32 idx;

	mono_class_init_internal (klass);

	MonoImage *klass_image = m_class_get_image (klass);
	g_assert (!image_is_dynamic (klass_image));

	methodt = &klass_image->tables [MONO_TABLE_METHOD];
	idx = mono_method_get_index (method);
	if (idx > 0) {
		guint param_index = mono_metadata_decode_row_col (methodt, idx - 1, MONO_METHOD_PARAMLIST);

		if (index == -1)
			/* Return value */
			return mono_metadata_make_token (MONO_TABLE_PARAM, 0);
		else
			return mono_metadata_make_token (MONO_TABLE_PARAM, param_index + index);
	}

	return 0;
}

/**
 * mono_method_get_marshal_info:
 */
void
mono_method_get_marshal_info (MonoMethod *method, MonoMarshalSpec **mspecs)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;
	MonoMethodSignature *signature;
	guint32 idx;

	signature = mono_method_signature_internal (method);
	g_assert (signature); /*FIXME there is no way to signal error from this function*/

	for (i = 0; i < signature->param_count + 1; ++i)
		mspecs [i] = NULL;

	if (image_is_dynamic (m_class_get_image (method->klass))) {
		MonoReflectionMethodAux *method_aux = 
			(MonoReflectionMethodAux *)g_hash_table_lookup (
				((MonoDynamicImage*)m_class_get_image (method->klass))->method_aux_hash, method);
		if (method_aux && method_aux->param_marshall) {
			MonoMarshalSpec **dyn_specs = method_aux->param_marshall;
			for (i = 0; i < signature->param_count + 1; ++i)
				if (dyn_specs [i]) {
					mspecs [i] = g_new0 (MonoMarshalSpec, 1);
					memcpy (mspecs [i], dyn_specs [i], sizeof (MonoMarshalSpec));
					mspecs [i]->data.custom_data.custom_name = g_strdup (dyn_specs [i]->data.custom_data.custom_name);
					mspecs [i]->data.custom_data.cookie = g_strdup (dyn_specs [i]->data.custom_data.cookie);
				}
		}
		return;
	}

	mono_class_init_internal (klass);

	MonoImage *klass_image = m_class_get_image (klass);
	methodt = &klass_image->tables [MONO_TABLE_METHOD];
	paramt = &klass_image->tables [MONO_TABLE_PARAM];
	idx = mono_method_get_index (method);
	if (idx > 0) {
		guint32 cols [MONO_PARAM_SIZE];
		guint param_index = mono_metadata_decode_row_col (methodt, idx - 1, MONO_METHOD_PARAMLIST);

		if (idx < methodt->rows)
			lastp = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);
		else
			lastp = paramt->rows + 1;

		for (i = param_index; i < lastp; ++i) {
			mono_metadata_decode_row (paramt, i -1, cols, MONO_PARAM_SIZE);

			if (cols [MONO_PARAM_FLAGS] & PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL && cols [MONO_PARAM_SEQUENCE] <= signature->param_count) {
				const char *tp;
				tp = mono_metadata_get_marshal_info (klass_image, i - 1, FALSE);
				g_assert (tp);
				mspecs [cols [MONO_PARAM_SEQUENCE]]= mono_metadata_parse_marshal_spec (klass_image, tp);
			}
		}

		return;
	}
}

/**
 * mono_method_has_marshal_info:
 */
gboolean
mono_method_has_marshal_info (MonoMethod *method)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;
	guint32 idx;

	if (image_is_dynamic (m_class_get_image (method->klass))) {
		MonoReflectionMethodAux *method_aux = 
			(MonoReflectionMethodAux *)g_hash_table_lookup (
				((MonoDynamicImage*)m_class_get_image (method->klass))->method_aux_hash, method);
		MonoMarshalSpec **dyn_specs = method_aux->param_marshall;
		if (dyn_specs) {
			for (i = 0; i < mono_method_signature_internal (method)->param_count + 1; ++i)
				if (dyn_specs [i])
					return TRUE;
		}
		return FALSE;
	}

	mono_class_init_internal (klass);

	methodt = &m_class_get_image (klass)->tables [MONO_TABLE_METHOD];
	paramt = &m_class_get_image (klass)->tables [MONO_TABLE_PARAM];
	idx = mono_method_get_index (method);
	if (idx > 0) {
		guint32 cols [MONO_PARAM_SIZE];
		guint param_index = mono_metadata_decode_row_col (methodt, idx - 1, MONO_METHOD_PARAMLIST);

		if (idx + 1 < methodt->rows)
			lastp = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);
		else
			lastp = paramt->rows + 1;

		for (i = param_index; i < lastp; ++i) {
			mono_metadata_decode_row (paramt, i -1, cols, MONO_PARAM_SIZE);

			if (cols [MONO_PARAM_FLAGS] & PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL)
				return TRUE;
		}
		return FALSE;
	}
	return FALSE;
}

gpointer
mono_method_get_wrapper_data (MonoMethod *method, guint32 id)
{
	void **data;
	g_assert (method != NULL);
	g_assert (method->wrapper_type != MONO_WRAPPER_NONE);

	data = (void **)((MonoMethodWrapper *)method)->method_data;
	g_assert (data != NULL);
	g_assert (id <= GPOINTER_TO_UINT (*data));
	return data [id];
}

typedef struct {
	MonoStackWalk func;
	gpointer user_data;
} StackWalkUserData;

static gboolean
stack_walk_adapter (MonoStackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	StackWalkUserData *d = (StackWalkUserData *)data;

	switch (frame->type) {
	case FRAME_TYPE_DEBUGGER_INVOKE:
	case FRAME_TYPE_MANAGED_TO_NATIVE:
	case FRAME_TYPE_TRAMPOLINE:
	case FRAME_TYPE_INTERP_TO_MANAGED:
	case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
		return FALSE;
	case FRAME_TYPE_MANAGED:
	case FRAME_TYPE_INTERP:
		g_assert (frame->ji);
		return d->func (frame->actual_method, frame->native_offset, frame->il_offset, frame->managed, d->user_data);
		break;
	default:
		g_assert_not_reached ();
		return FALSE;
	}
}

void
mono_stack_walk (MonoStackWalk func, gpointer user_data)
{
	StackWalkUserData ud = { func, user_data };
	mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (stack_walk_adapter, NULL, MONO_UNWIND_LOOKUP_ALL, &ud);
}

/**
 * mono_stack_walk_no_il:
 */
void
mono_stack_walk_no_il (MonoStackWalk func, gpointer user_data)
{
	StackWalkUserData ud = { func, user_data };
	mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (stack_walk_adapter, NULL, MONO_UNWIND_DEFAULT, &ud);
}

typedef struct {
	MonoStackWalkAsyncSafe func;
	gpointer user_data;
} AsyncStackWalkUserData;


static gboolean
async_stack_walk_adapter (MonoStackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	AsyncStackWalkUserData *d = (AsyncStackWalkUserData *)data;

	switch (frame->type) {
	case FRAME_TYPE_DEBUGGER_INVOKE:
	case FRAME_TYPE_MANAGED_TO_NATIVE:
	case FRAME_TYPE_TRAMPOLINE:
	case FRAME_TYPE_INTERP_TO_MANAGED:
	case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
		return FALSE;
	case FRAME_TYPE_MANAGED:
	case FRAME_TYPE_INTERP:
		if (!frame->ji)
			return FALSE;

		MonoMethod *method;
		method = frame->ji->async ? NULL : frame->actual_method;

		return d->func (method, frame->domain, frame->ji->code_start, frame->native_offset, d->user_data);
	default:
		g_assert_not_reached ();
		return FALSE;
	}
}


/**
 * mono_stack_walk_async_safe:
 * Async safe version callable from signal handlers.
 */
void
mono_stack_walk_async_safe (MonoStackWalkAsyncSafe func, void *initial_sig_context, void *user_data)
{
	MonoContext ctx;
	AsyncStackWalkUserData ud = { func, user_data };

	mono_sigctx_to_monoctx (initial_sig_context, &ctx);
	mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (async_stack_walk_adapter, &ctx, MONO_UNWIND_SIGNAL_SAFE, &ud);
}

static gboolean
last_managed (MonoMethod *m, gint no, gint ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = (MonoMethod **)data;
	*dest = m;
	/*g_print ("In %s::%s [%d] [%d]\n", m->klass->name, m->name, no, ilo);*/

	return managed;
}

/**
 * mono_method_get_last_managed:
 */
MonoMethod*
mono_method_get_last_managed (void)
{
	MonoMethod *m = NULL;
	mono_stack_walk_no_il (last_managed, &m);
	return m;
}

static gboolean loader_lock_track_ownership = FALSE;

/**
 * mono_loader_lock:
 *
 * See \c docs/thread-safety.txt for the locking strategy.
 */
void
mono_loader_lock (void)
{
	mono_locks_coop_acquire (&loader_mutex, LoaderLock);
	if (G_UNLIKELY (loader_lock_track_ownership)) {
		mono_native_tls_set_value (loader_lock_nest_id, GUINT_TO_POINTER (GPOINTER_TO_UINT (mono_native_tls_get_value (loader_lock_nest_id)) + 1));
	}
}

/**
 * mono_loader_unlock:
 */
void
mono_loader_unlock (void)
{
	mono_locks_coop_release (&loader_mutex, LoaderLock);
	if (G_UNLIKELY (loader_lock_track_ownership)) {
		mono_native_tls_set_value (loader_lock_nest_id, GUINT_TO_POINTER (GPOINTER_TO_UINT (mono_native_tls_get_value (loader_lock_nest_id)) - 1));
	}
}

/*
 * mono_loader_lock_track_ownership:
 *
 *   Set whenever the runtime should track ownership of the loader lock. If set to TRUE,
 * the mono_loader_lock_is_owned_by_self () can be called to query whenever the current
 * thread owns the loader lock. 
 */
void
mono_loader_lock_track_ownership (gboolean track)
{
	loader_lock_track_ownership = track;
}

/*
 * mono_loader_lock_is_owned_by_self:
 *
 *   Return whenever the current thread owns the loader lock.
 * This is useful to avoid blocking operations while holding the loader lock.
 */
gboolean
mono_loader_lock_is_owned_by_self (void)
{
	g_assert (loader_lock_track_ownership);

	return GPOINTER_TO_UINT (mono_native_tls_get_value (loader_lock_nest_id)) > 0;
}

/*
 * mono_loader_lock_if_inited:
 *
 *   Acquire the loader lock if it has been initialized, no-op otherwise. This can
 * be used in runtime initialization code which can be executed before mono_loader_init ().
 */
void
mono_loader_lock_if_inited (void)
{
	if (loader_lock_inited)
		mono_loader_lock ();
}

void
mono_loader_unlock_if_inited (void)
{
	if (loader_lock_inited)
		mono_loader_unlock ();
}

/**
 * mono_method_signature_checked:
 *
 * Return the signature of the method M. On failure, returns NULL, and ERR is set.
 */
MonoMethodSignature*
mono_method_signature_checked (MonoMethod *m, MonoError *error)
{
	int idx;
	MonoImage* img;
	const char *sig;
	gboolean can_cache_signature;
	MonoGenericContainer *container;
	MonoMethodSignature *signature = NULL, *sig2;
	guint32 sig_offset;

	/* We need memory barriers below because of the double-checked locking pattern */ 

	error_init (error);

	if (m->signature)
		return m->signature;

	img = m_class_get_image (m->klass);

	if (m->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) m;
		/* the lock is recursive */
		signature = mono_method_signature_internal (imethod->declaring);
		signature = inflate_generic_signature_checked (m_class_get_image (imethod->declaring->klass), signature, mono_method_get_context (m), error);
		if (!mono_error_ok (error))
			return NULL;

		mono_atomic_fetch_add_i32 (&inflated_signatures_size, mono_metadata_signature_size (signature));

		mono_image_lock (img);

		mono_memory_barrier ();
		if (!m->signature)
			m->signature = signature;

		mono_image_unlock (img);

		return m->signature;
	}

	g_assert (mono_metadata_token_table (m->token) == MONO_TABLE_METHOD);
	idx = mono_metadata_token_index (m->token);

	sig = mono_metadata_blob_heap (img, sig_offset = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_SIGNATURE));

	g_assert (!mono_class_is_ginst (m->klass));
	container = mono_method_get_generic_container (m);
	if (!container)
		container = mono_class_try_get_generic_container (m->klass);

	/* Generic signatures depend on the container so they cannot be cached */
	/* icall/pinvoke signatures cannot be cached cause we modify them below */
	can_cache_signature = !(m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && !(m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) && !container;

	/* If the method has parameter attributes, that can modify the signature */
	if (mono_metadata_method_has_param_attrs (img, idx))
		can_cache_signature = FALSE;

	if (can_cache_signature) {
		mono_image_lock (img);
		signature = (MonoMethodSignature *)g_hash_table_lookup (img->method_signatures, sig);
		mono_image_unlock (img);
	}

	if (!signature) {
		const char *sig_body;
		/*TODO we should cache the failure result somewhere*/
		if (!mono_verifier_verify_method_signature (img, sig_offset, error))
			return NULL;

		/* size = */ mono_metadata_decode_blob_size (sig, &sig_body);

		signature = mono_metadata_parse_method_signature_full (img, container, idx, sig_body, NULL, error);
		if (!signature)
			return NULL;

		if (can_cache_signature) {
			mono_image_lock (img);
			sig2 = (MonoMethodSignature *)g_hash_table_lookup (img->method_signatures, sig);
			if (!sig2)
				g_hash_table_insert (img->method_signatures, (gpointer)sig, signature);
			mono_image_unlock (img);
		}

		mono_atomic_fetch_add_i32 (&signatures_size, mono_metadata_signature_size (signature));
	}

	/* Verify metadata consistency */
	if (signature->generic_param_count) {
		if (!container || !container->is_method) {
			mono_error_set_method_missing (error, m->klass, m->name, signature, "Signature claims method has generic parameters, but generic_params table says it doesn't for method 0x%08x from image %s", idx, img->name);
			return NULL;
		}
		if (container->type_argc != signature->generic_param_count) {
			mono_error_set_method_missing (error, m->klass, m->name, signature, "Inconsistent generic parameter count.  Signature says %d, generic_params table says %d for method 0x%08x from image %s", signature->generic_param_count, container->type_argc, idx, img->name);
			return NULL;
		}
	} else if (container && container->is_method && container->type_argc) {
		mono_error_set_method_missing (error, m->klass, m->name, signature, "generic_params table claims method has generic parameters, but signature says it doesn't for method 0x%08x from image %s", idx, img->name);
		return NULL;
	}
	if (m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		signature->pinvoke = 1;
#ifdef TARGET_WIN32
		/*
		 * On Windows the default pinvoke calling convention is STDCALL but
		 * we need CDECL since this is actually an icall.
		 */
		signature->call_convention = MONO_CALL_C;
#endif
	} else if (m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		MonoCallConvention conv = (MonoCallConvention)0;
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)m;
		signature->pinvoke = 1;

		switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CALL_CONV_MASK) {
		case 0: /* no call conv, so using default */
		case PINVOKE_ATTRIBUTE_CALL_CONV_WINAPI:
			conv = MONO_CALL_DEFAULT;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_CDECL:
			conv = MONO_CALL_C;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_STDCALL:
			conv = MONO_CALL_STDCALL;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_THISCALL:
			conv = MONO_CALL_THISCALL;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_FASTCALL:
			conv = MONO_CALL_FASTCALL;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_GENERIC:
		case PINVOKE_ATTRIBUTE_CALL_CONV_GENERICINST:
		default: {
			mono_error_set_method_missing (error, m->klass, m->name, signature, "Unsupported calling convention : 0x%04x for method 0x%08x from image %s", piinfo->piflags, idx, img->name);
		}
			return NULL;
		}
		signature->call_convention = conv;
	}

	mono_image_lock (img);

	mono_memory_barrier ();
	if (!m->signature)
		m->signature = signature;

	mono_image_unlock (img);

	return m->signature;
}

/**
 * mono_method_signature_internal:
 * \returns the signature of the method \p m. On failure, returns NULL.
 */
MonoMethodSignature*
mono_method_signature_internal (MonoMethod *m)
{
	ERROR_DECL (error);
	MonoMethodSignature *sig = mono_method_signature_checked (m, error);
	if (sig)
		return sig;
	char *type_name = mono_type_get_full_name (m->klass);
	g_warning ("Could not load signature of %s:%s due to: %s", type_name, m->name, mono_error_get_message (error));
	g_free (type_name);
	mono_error_cleanup (error);
	return NULL;
}

/**
 * mono_method_signature:
 * \returns the signature of the method \p m. On failure, returns NULL.
 */
MonoMethodSignature*
mono_method_signature (MonoMethod *m)
{
	MonoMethodSignature *sig;
	MONO_ENTER_GC_UNSAFE;
	sig = mono_method_signature_internal (m);
	MONO_EXIT_GC_UNSAFE;
	return sig;
}

/**
 * mono_method_get_name:
 */
const char*
mono_method_get_name (MonoMethod *method)
{
	return method->name;
}

/**
 * mono_method_get_class:
 */
MonoClass*
mono_method_get_class (MonoMethod *method)
{
	return method->klass;
}

/**
 * mono_method_get_token:
 */
guint32
mono_method_get_token (MonoMethod *method)
{
	return method->token;
}

gboolean
mono_method_has_no_body (MonoMethod *method)
{
	return ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));
}

// FIXME Replace all internal callers of mono_method_get_header_checked with
// mono_method_get_header_internal; the difference is in error initialization.
MonoMethodHeader*
mono_method_get_header_internal (MonoMethod *method, MonoError *error)
{
	int idx;
	guint32 rva;
	MonoImage* img;
	gpointer loc;
	MonoGenericContainer *container;

	error_init (error);
	img = m_class_get_image (method->klass);

	// FIXME: for internal callers maybe it makes sense to do this check at the call site, not
	// here?
	if (mono_method_has_no_body (method)) {
		mono_error_set_bad_image (error, img, "Method has no body");
		return NULL;
	}

	if (method->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;
		MonoMethodHeader *header, *iheader;

		header = mono_method_get_header_checked (imethod->declaring, error);
		if (!header)
			return NULL;

		iheader = inflate_generic_header (header, mono_method_get_context (method), error);
		mono_metadata_free_mh (header);
		if (!iheader) {
			return NULL;
		}

		return iheader;
	}

	if (method->wrapper_type != MONO_WRAPPER_NONE || method->sre_method) {
		MonoMethodWrapper *mw = (MonoMethodWrapper *)method;
		g_assert (mw->header);
		return mw->header;
	}

	/* 
	 * We don't need locks here: the new header is allocated from malloc memory
	 * and is not stored anywhere in the runtime, the user needs to free it.
	 */
	g_assert (mono_metadata_token_table (method->token) == MONO_TABLE_METHOD);
	idx = mono_metadata_token_index (method->token);
	rva = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_RVA);

	if (!mono_verifier_verify_method_header (img, rva, error))
		return NULL;

	loc = mono_image_rva_map (img, rva);
	if (!loc) {
		mono_error_set_bad_image (error, img, "Method has zero rva");
		return NULL;
	}

	/*
	 * When parsing the types of local variables, we must pass any container available
	 * to ensure that both VAR and MVAR will get the right owner.
	 */
	container = mono_method_get_generic_container (method);
	if (!container)
		container = mono_class_try_get_generic_container (method->klass);
	return mono_metadata_parse_mh_full (img, container, (const char *)loc, error);
}

MonoMethodHeader*
mono_method_get_header_checked (MonoMethod *method, MonoError *error)
// Public function that must initialize MonoError for compatibility.
{
	MONO_API_ERROR_INIT (error);
	return mono_method_get_header_internal (method, error);
}
/**
 * mono_method_get_header:
 */
MonoMethodHeader*
mono_method_get_header (MonoMethod *method)
{
	ERROR_DECL (error);
	MonoMethodHeader *header = mono_method_get_header_checked (method, error);
	mono_error_cleanup (error);
	return header;
}


/**
 * mono_method_get_flags:
 */
guint32
mono_method_get_flags (MonoMethod *method, guint32 *iflags)
{
	if (iflags)
		*iflags = method->iflags;
	return method->flags;
}

/**
 * mono_method_get_index:
 * Find the method index in the metadata \c MethodDef table.
 */
guint32
mono_method_get_index (MonoMethod *method)
{
	MonoClass *klass = method->klass;
	int i;

	if (m_class_get_rank (klass))
		/* constructed array methods are not in the MethodDef table */
		return 0;

	if (method->token)
		return mono_metadata_token_index (method->token);

	mono_class_setup_methods (klass);
	if (mono_class_has_failure (klass))
		return 0;
	int first_idx = mono_class_get_first_method_idx (klass);
	int mcount = mono_class_get_method_count (klass);
	MonoMethod **klass_methods = m_class_get_methods (klass);
	for (i = 0; i < mcount; ++i) {
		if (method == klass_methods [i]) {
			if (m_class_get_image (klass)->uncompressed_metadata)
				return mono_metadata_translate_token_index (m_class_get_image (klass), MONO_TABLE_METHOD, first_idx + i + 1);
			else
				return first_idx + i + 1;
		}
	}
	return 0;
}
