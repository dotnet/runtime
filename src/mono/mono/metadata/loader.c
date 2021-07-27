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
#include <mono/metadata/metadata-update.h>
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
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/jit-info.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-path.h>

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
static gboolean loader_lock_track_ownership;

/*
 * This TLS variable holds how many times the current thread has acquired the loader 
 * lock.
 */
static MonoNativeTlsKey loader_lock_nest_id;

MonoDefaults mono_defaults;

/* Statistics */
static gint32 inflated_signatures_size;
static gint32 memberref_sig_cache_size;
static gint32 methods_size;
static gint32 signatures_size;

void
mono_loader_init ()
{
	static gboolean inited;

	// FIXME: potential race
	if (!inited) {
		mono_coop_mutex_init_recursive (&loader_mutex);
		mono_os_mutex_init_recursive (&global_loader_data_mutex);
		loader_lock_inited = TRUE;

		mono_global_loader_cache_init ();

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
mono_global_loader_data_lock (void)
{
	mono_locks_os_acquire (&global_loader_data_mutex, LoaderGlobalDataLock);
}

void
mono_global_loader_data_unlock (void)
{
	mono_locks_os_release (&global_loader_data_mutex, LoaderGlobalDataLock);
}

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
			if (!is_ok (error)) //bail out if we hit a loader error
				return NULL;
			if (method) {
				other_sig = mono_method_signature_checked (method, error);
				if (!is_ok (error)) //bail out if we hit a loader error
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
		if (!is_ok (error)) //bail out if we hit a loader error 
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

		qname = g_strconcat (class_name, ".", name, (const char*)NULL);
		const char *ic_name_space = m_class_get_name_space (ic);
		if (ic_name_space && ic_name_space [0])
			fqname = g_strconcat (ic_name_space, ".", class_name, ".", name, (const char*)NULL);
		else
			fqname = NULL;
	} else
		class_name = qname = fqname = NULL;

	while (in_class) {
		g_assert (from_class);
		result = find_method_in_class (in_class, name, qname, fqname, sig, from_class, error);
		if (result || !is_ok (error))
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
			ic_qname = g_strconcat (ic_class_name, ".", name, (const char*)NULL);
			const char *in_ic_name_space = m_class_get_name_space (in_ic);
			if (in_ic_name_space && in_ic_name_space [0])
				ic_fqname = g_strconcat (in_ic_name_space, ".", ic_class_name, ".", name, (const char*)NULL);
			else
				ic_fqname = NULL;

			result = find_method_in_class (in_ic, ic ? name : NULL, ic_qname, ic_fqname, sig, from_ic, error);
			g_free (ic_class_name);
			g_free (ic_fqname);
			g_free (ic_qname);
			if (result || !is_ok (error))
				goto out;
		}

		in_class = m_class_get_parent (in_class);
		from_class = m_class_get_parent (from_class);
	}
	g_assert (!in_class == !from_class);

	if (is_interface)
		result = find_method_in_class (mono_defaults.object_class, name, qname, fqname, sig, mono_defaults.object_class, error);

	//we did not find the method
	if (!result && is_ok (error))
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
	if (!is_ok (error))
		goto fail;
	is_open = mono_class_is_open_constructed_type (res->ret);
	for (i = 0; i < sig->param_count; ++i) {
		res->params [i] = mono_class_inflate_generic_type_checked (sig->params [i], context, error);
		if (!is_ok (error))
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
	if (!is_ok (error))
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
			ptr = mono_metadata_blob_heap (image, sig_idx);
			mono_metadata_decode_blob_size (ptr, &ptr);

			sig = mono_metadata_parse_method_signature_full (image, NULL, 0, ptr, NULL, error);
			if (!sig)
				return NULL;

			sig = (MonoMethodSignature *)cache_memberref_sig (image, sig_idx, sig);
		}
	}

	if (context) {
		MonoMethodSignature *cached;

		/* This signature is not owned by a MonoMethod, so need to cache */
		sig = inflate_generic_signature_checked (image, sig, context, error);
		if (!is_ok (error))
			return NULL;

		cached = mono_metadata_get_inflated_signature (sig, context);
		if (cached != sig)
			mono_metadata_free_inflated_signature (sig);
		else
			mono_atomic_fetch_add_i32 (&inflated_signatures_size, mono_metadata_signature_size (cached));
		sig = cached;
	}

	g_assert (is_ok (error));
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

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
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

	if (!method && is_ok (error))
		mono_error_set_method_missing (error, klass, mname, sig, "Failed to load due to unknown reasons");

	return method;

fail:
	g_assert (!is_ok (error));
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

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODSPEC_SIGNATURE]);

	mono_metadata_decode_value (ptr, &ptr);
	ptr++;
	param_count = mono_metadata_decode_value (ptr, &ptr);

	inst = mono_metadata_parse_generic_inst (image, NULL, param_count, ptr, &ptr, error);
	if (!inst)
		return NULL;

	if (context && inst->is_open) {
		inst = mono_metadata_inflate_generic_inst (inst, context, error);
		if (!is_ok (error))
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
	guint32 cols [MONO_METHOD_SIZE];

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

	if (mono_metadata_table_bounds_check (image, MONO_TABLE_METHOD, idx)) {
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

	mono_metadata_decode_row (&image->tables [MONO_TABLE_METHOD], idx - 1, cols, MONO_METHOD_SIZE);

	if ((cols [MONO_METHOD_FLAGS] & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (cols [MONO_METHOD_IMPLFLAGS] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
		result = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethodPInvoke));
	} else {
		result = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethod));
		mono_atomic_fetch_add_i32 (&methods_size, sizeof (MonoMethod));
	}

	mono_atomic_inc_i32 (&mono_stats.method_count);

	result->slot = -1;
	result->klass = klass;
	result->flags = cols [MONO_METHOD_FLAGS];
	result->iflags = cols [MONO_METHOD_IMPLFLAGS];
	result->token = token;
	result->name = mono_metadata_string_heap (image, cols [MONO_METHOD_NAME]);

	/* If a method is abstract and marked as an icall, silently ignore the
	 * icall attribute so that we don't later emit a warning that the icall
	 * can't be found.
	 */
	if ((result->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
	    (result->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		result->iflags &= ~METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [MONO_METHOD_SIGNATURE]);
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

	if (cols [MONO_METHOD_IMPLFLAGS] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (result->klass == mono_defaults.string_class && !strcmp (result->name, ".ctor"))
			result->string_ctor = 1;
	} else if (cols [MONO_METHOD_FLAGS] & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)result;

#ifdef TARGET_WIN32
		/* IJW is P/Invoke with a predefined function pointer. */
		if (m_image_is_module_handle (image) && (cols [MONO_METHOD_IMPLFLAGS] & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
			piinfo->addr = mono_image_rva_map (image, cols [MONO_METHOD_RVA]);
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
	if (!method)
		return;

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

		if (idx < table_info_get_rows (methodt))
			lastp = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);
		else
			lastp = table_info_get_rows (paramt) + 1;
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
			for (i = 0; i < signature->param_count + 1; ++i) {
				if (dyn_specs [i]) {
					mspecs [i] = g_new0 (MonoMarshalSpec, 1);
					memcpy (mspecs [i], dyn_specs [i], sizeof (MonoMarshalSpec));
					if (mspecs [i]->native == MONO_NATIVE_CUSTOM) {
						mspecs [i]->data.custom_data.custom_name = g_strdup (dyn_specs [i]->data.custom_data.custom_name);
						mspecs [i]->data.custom_data.cookie = g_strdup (dyn_specs [i]->data.custom_data.cookie);
					}
				}
			}
		}
		return;
	}

	/* dynamic method added to non-dynamic image */
	if (method->dynamic)
		return;

	mono_class_init_internal (klass);

	MonoImage *klass_image = m_class_get_image (klass);
	methodt = &klass_image->tables [MONO_TABLE_METHOD];
	paramt = &klass_image->tables [MONO_TABLE_PARAM];
	idx = mono_method_get_index (method);
	if (idx > 0) {
		guint32 cols [MONO_PARAM_SIZE];
		guint param_index = mono_metadata_decode_row_col (methodt, idx - 1, MONO_METHOD_PARAMLIST);

		if (idx < table_info_get_rows (methodt))
			lastp = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);
		else
			lastp = table_info_get_rows (paramt) + 1;

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

		if (idx + 1 < table_info_get_rows (methodt))
			lastp = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);
		else
			lastp = table_info_get_rows (paramt) + 1;

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
	case FRAME_TYPE_INTERP_ENTRY:
	case FRAME_TYPE_JIT_ENTRY:
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

		return d->func (method, mono_get_root_domain (), frame->ji->code_start, frame->native_offset, d->user_data);
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

/**
 * mono_method_signature_checked_slow:
 *
 * Return the signature of the method M. On failure, returns NULL, and ERR is set.
 * Call mono_method_signature_checked instead.
 */
MonoMethodSignature*
mono_method_signature_checked_slow (MonoMethod *m, MonoError *error)
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
		if (!is_ok (error))
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
 * mono_method_signature_internal_slow:
 * \returns the signature of the method \p m. On failure, returns NULL.
 * Call mono_method_signature_internal instead.
 */
MonoMethodSignature*
mono_method_signature_internal_slow (MonoMethod *m)
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
	gpointer loc = NULL;
	MonoGenericContainer *container;

	error_init (error);
	img = m_class_get_image (method->klass);

	// FIXME: for internal callers maybe it makes sense to do this check at the call site, not
	// here?
	if (mono_method_has_no_body (method)) {
		if (mono_method_get_is_reabstracted (method))
			mono_error_set_generic_error (error, "System", "EntryPointNotFoundException", "%s", method->name);
		else
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

        if (G_UNLIKELY (img->has_updates))
                loc = mono_metadata_update_get_updated_method_rva (img, idx);

	if (!loc) {
		rva = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_RVA);

		loc = mono_image_rva_map (img, rva);
	}

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
