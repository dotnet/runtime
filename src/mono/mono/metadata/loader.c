/*
 * loader.c: Image Loader 
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
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/lock-tracer.h>
#include <mono/metadata/verify-internals.h>
#include <mono/utils/mono-logger-internal.h>
#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-tls.h>

MonoDefaults mono_defaults;

/*
 * This lock protects the hash tables inside MonoImage used by the metadata 
 * loading functions in class.c and loader.c.
 *
 * See domain-internals.h for locking policy in combination with the
 * domain lock.
 */
static CRITICAL_SECTION loader_mutex;
static gboolean loader_lock_inited;

/* Statistics */
static guint32 inflated_signatures_size;
static guint32 memberref_sig_cache_size;
static guint32 methods_size;
static guint32 signatures_size;

/*
 * This TLS variable contains the last type load error encountered by the loader.
 */
MonoNativeTlsKey loader_error_thread_id;

/*
 * This TLS variable holds how many times the current thread has acquired the loader 
 * lock.
 */
MonoNativeTlsKey loader_lock_nest_id;

static void dllmap_cleanup (void);

void
mono_loader_init ()
{
	static gboolean inited;

	if (!inited) {
		InitializeCriticalSection (&loader_mutex);
		loader_lock_inited = TRUE;

		mono_native_tls_alloc (&loader_error_thread_id, NULL);
		mono_native_tls_alloc (&loader_lock_nest_id, NULL);

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

	mono_native_tls_free (loader_error_thread_id);
	mono_native_tls_free (loader_lock_nest_id);

	DeleteCriticalSection (&loader_mutex);
	loader_lock_inited = FALSE;	
}

/*
 * Handling of type load errors should be done as follows:
 *
 *   If something could not be loaded, the loader should call one of the
 * mono_loader_set_error_XXX functions ()
 * with the appropriate arguments, then return NULL to report the failure. The error 
 * should be propagated until it reaches code which can throw managed exceptions. At that
 * point, an exception should be thrown based on the information returned by
 * mono_loader_get_last_error (). Then the error should be cleared by calling 
 * mono_loader_clear_error ().
 */

static void
set_loader_error (MonoLoaderError *error)
{
	mono_native_tls_set_value (loader_error_thread_id, error);
}

/**
 * mono_loader_set_error_assembly_load:
 *
 * Set the loader error for this thread. 
 */
void
mono_loader_set_error_assembly_load (const char *assembly_name, gboolean ref_only)
{
	MonoLoaderError *error;

	if (mono_loader_get_last_error ()) 
		return;

	error = g_new0 (MonoLoaderError, 1);
	error->exception_type = MONO_EXCEPTION_FILE_NOT_FOUND;
	error->assembly_name = g_strdup (assembly_name);
	error->ref_only = ref_only;

	/* 
	 * This is not strictly needed, but some (most) of the loader code still
	 * can't deal with load errors, and this message is more helpful than an
	 * assert.
	 */
	if (ref_only)
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY, "Cannot resolve dependency to assembly '%s' because it has not been preloaded. When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.", assembly_name);
	else
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY, "Could not load file or assembly '%s' or one of its dependencies.", assembly_name);

	set_loader_error (error);
}

/**
 * mono_loader_set_error_type_load:
 *
 * Set the loader error for this thread. 
 */
void
mono_loader_set_error_type_load (const char *class_name, const char *assembly_name)
{
	MonoLoaderError *error;

	if (mono_loader_get_last_error ()) 
		return;

	error = g_new0 (MonoLoaderError, 1);
	error->exception_type = MONO_EXCEPTION_TYPE_LOAD;
	error->class_name = g_strdup (class_name);
	error->assembly_name = g_strdup (assembly_name);

	/* 
	 * This is not strictly needed, but some (most) of the loader code still
	 * can't deal with load errors, and this message is more helpful than an
	 * assert.
	 */
	mono_trace_warning (MONO_TRACE_TYPE, "The class %s could not be loaded, used in %s", class_name, assembly_name);

	set_loader_error (error);
}

/*
 * mono_loader_set_error_method_load:
 *
 *   Set the loader error for this thread. MEMBER_NAME should point to a string
 * inside metadata.
 */
void
mono_loader_set_error_method_load (const char *class_name, const char *member_name)
{
	MonoLoaderError *error;

	/* FIXME: Store the signature as well */
	if (mono_loader_get_last_error ())
		return;

	error = g_new0 (MonoLoaderError, 1);
	error->exception_type = MONO_EXCEPTION_MISSING_METHOD;
	error->class_name = g_strdup (class_name);
	error->member_name = member_name;

	set_loader_error (error);
}

/*
 * mono_loader_set_error_field_load:
 *
 * Set the loader error for this thread. MEMBER_NAME should point to a string
 * inside metadata.
 */
void
mono_loader_set_error_field_load (MonoClass *klass, const char *member_name)
{
	MonoLoaderError *error;

	/* FIXME: Store the signature as well */
	if (mono_loader_get_last_error ())
		return;

	error = g_new0 (MonoLoaderError, 1);
	error->exception_type = MONO_EXCEPTION_MISSING_FIELD;
	error->klass = klass;
	error->member_name = member_name;

	set_loader_error (error);
}

/*
 * mono_loader_set_error_bad_image:
 *
 * Set the loader error for this thread. 
 */
void
mono_loader_set_error_bad_image (char *msg)
{
	MonoLoaderError *error;

	if (mono_loader_get_last_error ())
		return;

	error = g_new0 (MonoLoaderError, 1);
	error->exception_type = MONO_EXCEPTION_BAD_IMAGE;
	error->msg = msg;

	set_loader_error (error);
}	


/*
 * mono_loader_get_last_error:
 *
 *   Returns information about the last type load exception encountered by the loader, or
 * NULL. After use, the exception should be cleared by calling mono_loader_clear_error.
 */
MonoLoaderError*
mono_loader_get_last_error (void)
{
	return (MonoLoaderError*)mono_native_tls_get_value (loader_error_thread_id);
}

/**
 * mono_loader_clear_error:
 *
 * Disposes any loader error messages on this thread
 */
void
mono_loader_clear_error (void)
{
	MonoLoaderError *ex = (MonoLoaderError*)mono_native_tls_get_value (loader_error_thread_id);

	if (ex) {
		g_free (ex->class_name);
		g_free (ex->assembly_name);
		g_free (ex->msg);
		g_free (ex);

		mono_native_tls_set_value (loader_error_thread_id, NULL);
	}
}

/**
 * mono_loader_error_prepare_exception:
 * @error: The MonoLoaderError to turn into an exception
 *
 * This turns a MonoLoaderError into an exception that can be thrown
 * and resets the Mono Loader Error state during this process.
 *
 */
MonoException *
mono_loader_error_prepare_exception (MonoLoaderError *error)
{
	MonoException *ex = NULL;

	switch (error->exception_type) {
	case MONO_EXCEPTION_TYPE_LOAD: {
		char *cname = g_strdup (error->class_name);
		char *aname = g_strdup (error->assembly_name);
		MonoString *class_name;
		
		mono_loader_clear_error ();
		
		class_name = mono_string_new (mono_domain_get (), cname);

		ex = mono_get_exception_type_load (class_name, aname);
		g_free (cname);
		g_free (aname);
		break;
        }
	case MONO_EXCEPTION_MISSING_METHOD: {
		char *cname = g_strdup (error->class_name);
		char *aname = g_strdup (error->member_name);
		
		mono_loader_clear_error ();
		ex = mono_get_exception_missing_method (cname, aname);
		g_free (cname);
		g_free (aname);
		break;
	}
		
	case MONO_EXCEPTION_MISSING_FIELD: {
		char *class_name;
		char *cmembername = g_strdup (error->member_name);
		if (error->klass)
			class_name = mono_type_get_full_name (error->klass);
		else
			class_name = g_strdup ("");

		mono_loader_clear_error ();
		
		ex = mono_get_exception_missing_field (class_name, cmembername);
		g_free (class_name);
		g_free (cmembername);
		break;
        }
	
	case MONO_EXCEPTION_FILE_NOT_FOUND: {
		char *msg;
		char *filename;

		if (error->ref_only)
			msg = g_strdup_printf ("Cannot resolve dependency to assembly '%s' because it has not been preloaded. When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.", error->assembly_name);
		else
			msg = g_strdup_printf ("Could not load file or assembly '%s' or one of its dependencies.", error->assembly_name);
		filename = g_strdup (error->assembly_name);
		/* Has to call this before calling anything which might call mono_class_init () */
		mono_loader_clear_error ();
		ex = mono_get_exception_file_not_found2 (msg, mono_string_new (mono_domain_get (), filename));
		g_free (msg);
		g_free (filename);
		break;
	}

	case MONO_EXCEPTION_BAD_IMAGE: {
		char *msg = g_strdup (error->msg);
		mono_loader_clear_error ();
		ex = mono_get_exception_bad_image_format (msg);
		g_free (msg);
		break;
	}

	default:
		g_assert_not_reached ();
	}

	return ex;
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

	mono_loader_lock ();
	res = g_hash_table_lookup (image->memberref_signatures, GUINT_TO_POINTER (sig_idx));
	mono_loader_unlock ();

	return res;
}

static gpointer
cache_memberref_sig (MonoImage *image, guint32 sig_idx, gpointer sig)
{
	gpointer prev_sig;

	mono_loader_lock ();
	prev_sig = g_hash_table_lookup (image->memberref_signatures, GUINT_TO_POINTER (sig_idx));
	if (prev_sig) {
		/* Somebody got in before us */
		sig = prev_sig;
	}
	else {
		g_hash_table_insert (image->memberref_signatures, GUINT_TO_POINTER (sig_idx), sig);
		/* An approximation based on glib 2.18 */
		memberref_sig_cache_size += sizeof (gpointer) * 4;
	}

	mono_loader_unlock ();

	return sig;
}

static MonoClassField*
field_from_memberref (MonoImage *image, guint32 token, MonoClass **retklass,
		      MonoGenericContext *context)
{
	MonoClass *klass;
	MonoClassField *field;
	MonoTableInfo *tables = image->tables;
	MonoType *sig_type;
	guint32 cols[6];
	guint32 nindex, class, class_table;
	const char *fname;
	const char *ptr;
	guint32 idx = mono_metadata_token_index (token);

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;

	fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

	if (!mono_verifier_verify_memberref_field_signature (image, cols [MONO_MEMBERREF_SIGNATURE], NULL)) {
		mono_loader_set_error_bad_image (g_strdup_printf ("Bad field signature class token 0x%08x field name %s token 0x%08x on image %s", class, fname, token, image->name));
		return NULL;
	}

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		class_table = MONO_TOKEN_TYPE_DEF;
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | nindex);
		break;
	case MONO_MEMBERREF_PARENT_TYPEREF:
		class_table = MONO_TOKEN_TYPE_REF;
		klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
		break;
	case MONO_MEMBERREF_PARENT_TYPESPEC:
		class_table = MONO_TOKEN_TYPE_SPEC;
		klass = mono_class_get_full (image, MONO_TOKEN_TYPE_SPEC | nindex, context);
		break;
	default:
		/*FIXME this must set a loader error!*/
		g_warning ("field load from %x", class);
		return NULL;
	}

	if (!klass) {
		char *name = mono_class_name_from_token (image, class_table | nindex);
		g_warning ("Missing field %s in class %s (type token %d)", fname, name, class_table | nindex);
		mono_loader_set_error_type_load (name, image->assembly_name);
		g_free (name);
		return NULL;
	}

	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	/* we may want to check the signature here... */

	if (*ptr++ != 0x6) {
		g_warning ("Bad field signature class token %08x field name %s token %08x", class, fname, token);
		mono_loader_set_error_field_load (klass, fname);
		return NULL;
	}
	/* FIXME: This needs a cache, especially for generic instances, since
	 * mono_metadata_parse_type () allocates everything from a mempool.
	 */
	sig_type = find_cached_memberref_sig (image, cols [MONO_MEMBERREF_SIGNATURE]);
	if (!sig_type) {
		sig_type = mono_metadata_parse_type (image, MONO_PARSE_TYPE, 0, ptr, &ptr);
		if (sig_type == NULL) {
			mono_loader_set_error_field_load (klass, fname);
			return NULL;
		}
		sig_type = cache_memberref_sig (image, cols [MONO_MEMBERREF_SIGNATURE], sig_type);
	}

	mono_class_init (klass); /*FIXME is this really necessary?*/
	if (retklass)
		*retklass = klass;
	field = mono_class_get_field_from_name_full (klass, fname, sig_type);

	if (!field)
		mono_loader_set_error_field_load (klass, fname);

	return field;
}

MonoClassField*
mono_field_from_token (MonoImage *image, guint32 token, MonoClass **retklass,
		       MonoGenericContext *context)
{
	MonoClass *k;
	guint32 type;
	MonoClassField *field;

	if (image->dynamic) {
		MonoClassField *result;
		MonoClass *handle_class;

		*retklass = NULL;
		result = mono_lookup_dynamic_token_class (image, token, TRUE, &handle_class, context);
		// This checks the memberref type as well
		if (!result || handle_class != mono_defaults.fieldhandle_class) {
			mono_loader_set_error_bad_image (g_strdup_printf ("Bad field token 0x%08x on image %s.", token, image->name));
			return NULL;
		}
		*retklass = result->parent;
		return result;
	}

	mono_loader_lock ();
	if ((field = g_hash_table_lookup (image->field_cache, GUINT_TO_POINTER (token)))) {
		*retklass = field->parent;
		mono_loader_unlock ();
		return field;
	}
	mono_loader_unlock ();

	if (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF)
		field = field_from_memberref (image, token, retklass, context);
	else {
		type = mono_metadata_typedef_from_field (image, mono_metadata_token_index (token));
		if (!type)
			return NULL;
		k = mono_class_get (image, MONO_TOKEN_TYPE_DEF | type);
		if (!k)
			return NULL;
		mono_class_init (k);
		if (retklass)
			*retklass = k;
		field = mono_class_get_field (k, token);
	}

	mono_loader_lock ();
	if (field && field->parent && !field->parent->generic_class && !field->parent->generic_container)
		g_hash_table_insert (image->field_cache, GUINT_TO_POINTER (token), field);
	mono_loader_unlock ();
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
		      MonoMethodSignature *sig, MonoClass *from_class)
{
 	int i;

	/* Search directly in the metadata to avoid calling setup_methods () */

	/* FIXME: !from_class->generic_class condition causes test failures. */
	if (klass->type_token && !klass->image->dynamic && !klass->methods && !klass->rank && klass == from_class && !from_class->generic_class) {
		for (i = 0; i < klass->method.count; ++i) {
			guint32 cols [MONO_METHOD_SIZE];
			MonoMethod *method;
			const char *m_name;
			MonoMethodSignature *other_sig;

			mono_metadata_decode_table_row (klass->image, MONO_TABLE_METHOD, klass->method.first + i, cols, MONO_METHOD_SIZE);

			m_name = mono_metadata_string_heap (klass->image, cols [MONO_METHOD_NAME]);

			if (!((fqname && !strcmp (m_name, fqname)) ||
				  (qname && !strcmp (m_name, qname)) ||
				  (name && !strcmp (m_name, name))))
				continue;

			method = mono_get_method (klass->image, MONO_TOKEN_METHOD_DEF | (klass->method.first + i + 1), klass);
			if (method) {
				other_sig = mono_method_signature (method);
				if (other_sig && (sig->call_convention != MONO_CALL_VARARG) && mono_metadata_signature_equal (sig, other_sig))
					return method;
			}
		}
	}

	mono_class_setup_methods (klass);
	/*
	We can't fail lookup of methods otherwise the runtime will fail with MissingMethodException instead of TypeLoadException.
	See mono/tests/generic-type-load-exception.2.il
	FIXME we should better report this error to the caller
	 */
	if (!klass->methods)
		return NULL;
	for (i = 0; i < klass->method.count; ++i) {
		MonoMethod *m = klass->methods [i];
		MonoMethodSignature *msig;

		/* We must cope with failing to load some of the types. */
		if (!m)
			continue;

		if (!((fqname && !strcmp (m->name, fqname)) ||
		      (qname && !strcmp (m->name, qname)) ||
		      (name && !strcmp (m->name, name))))
			continue;
		msig = mono_method_signature (m);
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

	if (i < klass->method.count)
		return mono_class_get_method_by_index (from_class, i);
	return NULL;
}

static MonoMethod *
find_method (MonoClass *in_class, MonoClass *ic, const char* name, MonoMethodSignature *sig, MonoClass *from_class)
{
	int i;
	char *qname, *fqname, *class_name;
	gboolean is_interface;
	MonoMethod *result = NULL;

	is_interface = MONO_CLASS_IS_INTERFACE (in_class);

	if (ic) {
		class_name = mono_type_get_name_full (&ic->byval_arg, MONO_TYPE_NAME_FORMAT_IL);

		qname = g_strconcat (class_name, ".", name, NULL); 
		if (ic->name_space && ic->name_space [0])
			fqname = g_strconcat (ic->name_space, ".", class_name, ".", name, NULL);
		else
			fqname = NULL;
	} else
		class_name = qname = fqname = NULL;

	while (in_class) {
		g_assert (from_class);
		result = find_method_in_class (in_class, name, qname, fqname, sig, from_class);
		if (result)
			goto out;

		if (name [0] == '.' && (!strcmp (name, ".ctor") || !strcmp (name, ".cctor")))
			break;

		/*
		 * This happens when we fail to lazily load the interfaces of one of the types.
		 * On such case we can't just bail out since user code depends on us trying harder.
		 */
		if (from_class->interface_offsets_count != in_class->interface_offsets_count) {
			in_class = in_class->parent;
			from_class = from_class->parent;
			continue;
		}

		for (i = 0; i < in_class->interface_offsets_count; i++) {
			MonoClass *in_ic = in_class->interfaces_packed [i];
			MonoClass *from_ic = from_class->interfaces_packed [i];
			char *ic_qname, *ic_fqname, *ic_class_name;
			
			ic_class_name = mono_type_get_name_full (&in_ic->byval_arg, MONO_TYPE_NAME_FORMAT_IL);
			ic_qname = g_strconcat (ic_class_name, ".", name, NULL); 
			if (in_ic->name_space && in_ic->name_space [0])
				ic_fqname = g_strconcat (in_ic->name_space, ".", ic_class_name, ".", name, NULL);
			else
				ic_fqname = NULL;

			result = find_method_in_class (in_ic, ic ? name : NULL, ic_qname, ic_fqname, sig, from_ic);
			g_free (ic_class_name);
			g_free (ic_fqname);
			g_free (ic_qname);
			if (result)
				goto out;
		}

		in_class = in_class->parent;
		from_class = from_class->parent;
	}
	g_assert (!in_class == !from_class);

	if (is_interface)
		result = find_method_in_class (mono_defaults.object_class, name, qname, fqname, sig, mono_defaults.object_class);

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

	mono_error_init (error);
	if (!context)
		return sig;

	res = g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + ((gint32)sig->param_count) * sizeof (MonoType*));
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

static MonoMethodHeader*
inflate_generic_header (MonoMethodHeader *header, MonoGenericContext *context)
{
	MonoMethodHeader *res;
	int i;
	res = g_malloc0 (MONO_SIZEOF_METHOD_HEADER + sizeof (gpointer) * header->num_locals);
	res->code = header->code;
	res->code_size = header->code_size;
	res->max_stack = header->max_stack;
	res->num_clauses = header->num_clauses;
	res->init_locals = header->init_locals;
	res->num_locals = header->num_locals;
	res->clauses = header->clauses;
	for (i = 0; i < header->num_locals; ++i)
		res->locals [i] = mono_class_inflate_generic_type (header->locals [i], context);
	if (res->num_clauses) {
		res->clauses = g_memdup (header->clauses, sizeof (MonoExceptionClause) * res->num_clauses);
		for (i = 0; i < header->num_clauses; ++i) {
			MonoExceptionClause *clause = &res->clauses [i];
			if (clause->flags != MONO_EXCEPTION_CLAUSE_NONE)
				continue;
			clause->data.catch_class = mono_class_inflate_generic_class (clause->data.catch_class, context);
		}
	}
	return res;
}

/*
 * token is the method_ref/def/spec token used in a call IL instruction.
 */
MonoMethodSignature*
mono_method_get_signature_full (MonoMethod *method, MonoImage *image, guint32 token, MonoGenericContext *context)
{
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	int sig_idx;
	guint32 cols [MONO_MEMBERREF_SIZE];
	MonoMethodSignature *sig;
	const char *ptr;

	/* !table is for wrappers: we should really assign their own token to them */
	if (!table || table == MONO_TABLE_METHOD)
		return mono_method_signature (method);

	if (table == MONO_TABLE_METHODSPEC) {
		/* the verifier (do_invoke_method) will turn the NULL into a verifier error */
		if ((method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) || !method->is_inflated)
			return NULL;

		return mono_method_signature (method);
	}

	if (method->klass->generic_class)
		return mono_method_signature (method);

#ifndef DISABLE_REFLECTION_EMIT
	if (image->dynamic) {
		sig = mono_reflection_lookup_signature (image, method, token);
	} else {
#endif
		mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
		sig_idx = cols [MONO_MEMBERREF_SIGNATURE];

		sig = find_cached_memberref_sig (image, sig_idx);
		if (!sig) {
			if (!mono_verifier_verify_memberref_method_signature (image, sig_idx, NULL)) {
				guint32 class = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
				const char *fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

				mono_loader_set_error_bad_image (g_strdup_printf ("Bad method signature class token 0x%08x field name %s token 0x%08x on image %s", class, fname, token, image->name));
				return NULL;
			}

			ptr = mono_metadata_blob_heap (image, sig_idx);
			mono_metadata_decode_blob_size (ptr, &ptr);
			sig = mono_metadata_parse_method_signature (image, 0, ptr, NULL);
			if (!sig)
				return NULL;
			sig = cache_memberref_sig (image, sig_idx, sig);
		}
		/* FIXME: we probably should verify signature compat in the dynamic case too*/
		if (!mono_verifier_is_sig_compatible (image, method, sig)) {
			guint32 class = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
			const char *fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

			mono_loader_set_error_bad_image (g_strdup_printf ("Incompatible method signature class token 0x%08x field name %s token 0x%08x on image %s", class, fname, token, image->name));
			return NULL;
		}
#ifndef DISABLE_REFLECTION_EMIT
	}
#endif


	if (context) {
		MonoError error;
		MonoMethodSignature *cached;

		/* This signature is not owned by a MonoMethod, so need to cache */
		sig = inflate_generic_signature_checked (image, sig, context, &error);
		if (!mono_error_ok (&error)) {/*XXX bubble up this and kill one use of loader errors */
			mono_loader_set_error_bad_image (g_strdup_printf ("Could not inflate signature %s", mono_error_get_message (&error)));
			mono_error_cleanup (&error);
			return NULL;
		}

		cached = mono_metadata_get_inflated_signature (sig, context);
		if (cached != sig)
			mono_metadata_free_inflated_signature (sig);
		else
			inflated_signatures_size += mono_metadata_signature_size (cached);
		sig = cached;
	}

	return sig;
}

MonoMethodSignature*
mono_method_get_signature (MonoMethod *method, MonoImage *image, guint32 token)
{
	return mono_method_get_signature_full (method, image, token, NULL);
}

/* this is only for the typespec array methods */
MonoMethod*
mono_method_search_in_array_class (MonoClass *klass, const char *name, MonoMethodSignature *sig)
{
	int i;

	mono_class_setup_methods (klass);
	g_assert (!klass->exception_type); /*FIXME this should not fail, right?*/
	for (i = 0; i < klass->method.count; ++i) {
		MonoMethod *method = klass->methods [i];
		if (strcmp (method->name, name) == 0 && sig->param_count == method->signature->param_count)
			return method;
	}
	return NULL;
}

static MonoMethod *
method_from_memberref (MonoImage *image, guint32 idx, MonoGenericContext *typespec_context,
		       gboolean *used_context)
{
	MonoClass *klass = NULL;
	MonoMethod *method = NULL;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class, sig_idx;
	const char *mname;
	MonoMethodSignature *sig;
	const char *ptr;

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, 3);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
	/*g_print ("methodref: 0x%x 0x%x %s\n", class, nindex,
		mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]));*/

	mname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

	/*
	 * Whether we actually used the `typespec_context' or not.
	 * This is used to tell our caller whether or not it's safe to insert the returned
	 * method into a cache.
	 */
	if (used_context)
		*used_context = class == MONO_MEMBERREF_PARENT_TYPESPEC;

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_REF | nindex);
			g_warning ("Missing method %s in assembly %s, type %s", mname, image->name, name);
			mono_loader_set_error_type_load (name, image->assembly_name);
			g_free (name);
			return NULL;
		}
		break;
	case MONO_MEMBERREF_PARENT_TYPESPEC:
		/*
		 * Parse the TYPESPEC in the parent's context.
		 */
		klass = mono_class_get_full (image, MONO_TOKEN_TYPE_SPEC | nindex, typespec_context);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_SPEC | nindex);
			g_warning ("Missing method %s in assembly %s, type %s", mname, image->name, name);
			mono_loader_set_error_type_load (name, image->assembly_name);
			g_free (name);
			return NULL;
		}
		break;
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | nindex);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_DEF | nindex);
			g_warning ("Missing method %s in assembly %s, type %s", mname, image->name, name);
			mono_loader_set_error_type_load (name, image->assembly_name);
			g_free (name);
			return NULL;
		}
		break;
	case MONO_MEMBERREF_PARENT_METHODDEF:
		return mono_get_method (image, MONO_TOKEN_METHOD_DEF | nindex, NULL);
		
	default:
		{
			/* This message leaks */
			char *message = g_strdup_printf ("Memberref parent unknown: class: %d, index %d", class, nindex);
			mono_loader_set_error_method_load ("", message);
			return NULL;
		}

	}
	g_assert (klass);
	mono_class_init (klass);

	sig_idx = cols [MONO_MEMBERREF_SIGNATURE];

	if (!mono_verifier_verify_memberref_method_signature (image, sig_idx, NULL)) {
		mono_loader_set_error_method_load (klass->name, mname);
		return NULL;
	}

	ptr = mono_metadata_blob_heap (image, sig_idx);
	mono_metadata_decode_blob_size (ptr, &ptr);

	sig = find_cached_memberref_sig (image, sig_idx);
	if (!sig) {
		sig = mono_metadata_parse_method_signature (image, 0, ptr, NULL);
		if (sig == NULL)
			return NULL;

		sig = cache_memberref_sig (image, sig_idx, sig);
	}

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		method = find_method (klass, NULL, mname, sig, klass);
		break;

	case MONO_MEMBERREF_PARENT_TYPESPEC: {
		MonoType *type;

		type = &klass->byval_arg;

		if (type->type != MONO_TYPE_ARRAY && type->type != MONO_TYPE_SZARRAY) {
			MonoClass *in_class = klass->generic_class ? klass->generic_class->container_class : klass;
			method = find_method (in_class, NULL, mname, sig, klass);
			break;
		}

		/* we're an array and we created these methods already in klass in mono_class_init () */
		method = mono_method_search_in_array_class (klass, mname, sig);
		break;
	}
	default:
		g_error ("Memberref parent unknown: class: %d, index %d", class, nindex);
		g_assert_not_reached ();
	}

	if (!method) {
		char *msig = mono_signature_get_desc (sig, FALSE);
		char * class_name = mono_type_get_name (&klass->byval_arg);
		GString *s = g_string_new (mname);
		if (sig->generic_param_count)
			g_string_append_printf (s, "<[%d]>", sig->generic_param_count);
		g_string_append_printf (s, "(%s)", msig);
		g_free (msig);
		msig = g_string_free (s, FALSE);

		g_warning (
			"Missing method %s::%s in assembly %s, referenced in assembly %s",
			class_name, msig, klass->image->name, image->name);
		mono_loader_set_error_method_load (class_name, mname);
		g_free (msig);
		g_free (class_name);
	}

	return method;
}

static MonoMethod *
method_from_methodspec (MonoImage *image, MonoGenericContext *context, guint32 idx)
{
	MonoError error;
	MonoMethod *method;
	MonoClass *klass;
	MonoTableInfo *tables = image->tables;
	MonoGenericContext new_context;
	MonoGenericInst *inst;
	const char *ptr;
	guint32 cols [MONO_METHODSPEC_SIZE];
	guint32 token, nindex, param_count;

	mono_metadata_decode_row (&tables [MONO_TABLE_METHODSPEC], idx - 1, cols, MONO_METHODSPEC_SIZE);
	token = cols [MONO_METHODSPEC_METHOD];
	nindex = token >> MONO_METHODDEFORREF_BITS;

	if (!mono_verifier_verify_methodspec_signature (image, cols [MONO_METHODSPEC_SIGNATURE], NULL))
		return NULL;

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODSPEC_SIGNATURE]);

	mono_metadata_decode_value (ptr, &ptr);
	ptr++;
	param_count = mono_metadata_decode_value (ptr, &ptr);
	g_assert (param_count);

	inst = mono_metadata_parse_generic_inst (image, NULL, param_count, ptr, &ptr);
	if (!inst)
		return NULL;

	if (context && inst->is_open) {
		inst = mono_metadata_inflate_generic_inst (inst, context, &error);
		if (!mono_error_ok (&error)) {
			mono_error_cleanup (&error); /*FIXME don't swallow error message.*/
			return NULL;
		}
	}

	if ((token & MONO_METHODDEFORREF_MASK) == MONO_METHODDEFORREF_METHODDEF)
		method = mono_get_method_full (image, MONO_TOKEN_METHOD_DEF | nindex, NULL, context);
	else
		method = method_from_memberref (image, nindex, context, NULL);

	if (!method)
		return NULL;

	klass = method->klass;

	if (klass->generic_class) {
		g_assert (method->is_inflated);
		method = ((MonoMethodInflated *) method)->declaring;
	}

	new_context.class_inst = klass->generic_class ? klass->generic_class->context.class_inst : NULL;
	new_context.method_inst = inst;

	return mono_class_inflate_generic_method_full (method, klass, &new_context);
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

	mono_loader_lock ();

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
			*rfunc = dll_map->target_func;
			break;
		}
	}

	mono_loader_unlock ();
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
 * @assembly: if NULL, this is a global mapping, otherwise the remapping of the dynamic library will only apply to the specified assembly
 * @dll: The name of the external library, as it would be found in the DllImport declaration.  If prefixed with 'i:' the matching of the library name is done without case sensitivity
 * @func: if not null, the mapping will only applied to the named function (the value of EntryPoint)
 * @tdll: The name of the library to map the specified @dll if it matches.
 * @tfunc: if func is not NULL, the name of the function that replaces the invocation
 *
 * LOCKING: Acquires the loader lock.
 *
 * This function is used to programatically add DllImport remapping in either
 * a specific assembly, or as a global remapping.   This is done by remapping
 * references in a DllImport attribute from the @dll library name into the @tdll
 * name.    If the @dll name contains the prefix "i:", the comparison of the 
 * library name is done without case sensitivity.
 *
 * If you pass @func, this is the name of the EntryPoint in a DllImport if specified
 * or the name of the function as determined by DllImport.    If you pass @func, you
 * must also pass @tfunc which is the name of the target function to invoke on a match.
 *
 * Example:
 * mono_dllmap_insert (NULL, "i:libdemo.dll", NULL, relocated_demo_path, NULL);
 *
 * The above will remap DllImport statments for "libdemo.dll" and "LIBDEMO.DLL" to
 * the contents of relocated_demo_path for all assemblies in the Mono process.
 *
 * NOTE: This can be called before the runtime is initialized, for example from
 * mono_config_parse ().
 */
void
mono_dllmap_insert (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	MonoDllMap *entry;

	mono_loader_init ();

	mono_loader_lock ();

	if (!assembly) {
		entry = g_malloc0 (sizeof (MonoDllMap));
		entry->dll = dll? g_strdup (dll): NULL;
		entry->target = tdll? g_strdup (tdll): NULL;
		entry->func = func? g_strdup (func): NULL;
		entry->target_func = tfunc? g_strdup (tfunc): NULL;
		entry->next = global_dll_map;
		global_dll_map = entry;
	} else {
		entry = mono_image_alloc0 (assembly, sizeof (MonoDllMap));
		entry->dll = dll? mono_image_strdup (assembly, dll): NULL;
		entry->target = tdll? mono_image_strdup (assembly, tdll): NULL;
		entry->func = func? mono_image_strdup (assembly, func): NULL;
		entry->target_func = tfunc? mono_image_strdup (assembly, tfunc): NULL;
		entry->next = assembly->dll_map;
		assembly->dll_map = entry;
	}

	mono_loader_unlock ();
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
	mono_loader_lock ();
	if (!global_module_map)
		global_module_map = g_hash_table_new (g_str_hash, g_str_equal);
	res = g_hash_table_lookup (global_module_map, name);
	if (res) {
		mono_loader_unlock ();
		return res;
	}
	res = mono_dl_open (name, flags, err);
	if (res)
		g_hash_table_insert (global_module_map, g_strdup (name), res);
	mono_loader_unlock ();
	return res;
}

static MonoDl *internal_module;

static gboolean
is_absolute_path (const char *path)
{
#ifdef PLATFORM_MACOSX
	if (!strncmp (path, "@executable_path/", 17) || !strncmp (path, "@loader_path/", 13) ||
	    !strncmp (path, "@rpath/", 7))
	    return TRUE;
#endif
	return g_path_is_absolute (path);
}

gpointer
mono_lookup_pinvoke_call (MonoMethod *method, const char **exc_class, const char **exc_arg)
{
	MonoImage *image = method->klass->image;
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
	char *full_name, *file_name, *found_name = NULL;
	int i;
	MonoDl *module = NULL;
	gboolean cached = FALSE;

	g_assert (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL);

	if (exc_class) {
		*exc_class = NULL;
		*exc_arg = NULL;
	}

	if (piinfo->addr)
		return piinfo->addr;

	if (method->klass->image->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			g_hash_table_lookup (
				((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
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

	mono_dllmap_lookup (image, orig_scope, import, &new_scope, &import);

	if (!module) {
		mono_loader_lock ();
		if (!image->pinvoke_scopes) {
			image->pinvoke_scopes = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
			image->pinvoke_scope_filenames = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
		}
		module = g_hash_table_lookup (image->pinvoke_scopes, new_scope);
		found_name = g_hash_table_lookup (image->pinvoke_scope_filenames, new_scope);
		mono_loader_unlock ();
		if (module)
			cached = TRUE;
		if (found_name)
			found_name = g_strdup (found_name);
	}

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
	for (i = 0; i < 4; ++i) {
		char *base_name = NULL, *dir_name = NULL;
		gboolean is_absolute = is_absolute_path (new_scope);
		
		switch (i) {
		case 0:
			/* Try the original name */
			file_name = g_strdup (new_scope);
			break;
		case 1:
			/* Try trimming the .dll extension */
			if (strstr (new_scope, ".dll") == (new_scope + strlen (new_scope) - 4)) {
				file_name = g_strdup (new_scope);
				file_name [strlen (new_scope) - 4] = '\0';
			}
			else
				continue;
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
					break;
				}
			} else if (strstr (new_scope, "lib") != new_scope) {
				file_name = g_strdup_printf ("lib%s", new_scope);
				break;
			}
			continue;
		default:
#ifndef TARGET_WIN32
			if (!g_ascii_strcasecmp ("user32.dll", new_scope) ||
			    !g_ascii_strcasecmp ("kernel32.dll", new_scope) ||
			    !g_ascii_strcasecmp ("user32", new_scope) ||
			    !g_ascii_strcasecmp ("kernel", new_scope)) {
				file_name = g_strdup ("libMonoSupportW.so");
			} else
#endif
				    continue;
#ifndef TARGET_WIN32
			break;
#endif
		}
		
		if (is_absolute) {
			if (!dir_name)
				dir_name = g_path_get_dirname (file_name);
			if (!base_name)
				base_name = g_path_get_basename (file_name);
		}
		
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
			void *iter = NULL;
			char *mdirname = g_path_get_dirname (image->name);
			while ((full_name = mono_dl_build_path (mdirname, file_name, &iter))) {
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
			g_free (mdirname);
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

	if (!module) {
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DLLIMPORT,
				"DllImport unable to load library '%s'.",
				error_msg);
		g_free (error_msg);

		if (exc_class) {
			*exc_class = "DllNotFoundException";
			*exc_arg = new_scope;
		}
		return NULL;
	}

	if (!cached) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loaded library '%s'.", found_name);
		mono_loader_lock ();
		if (!g_hash_table_lookup (image->pinvoke_scopes, new_scope)) {
			g_hash_table_insert (image->pinvoke_scopes, g_strdup (new_scope), module);
			g_hash_table_insert (image->pinvoke_scope_filenames, g_strdup (new_scope), g_strdup (found_name));
		}
		mono_loader_unlock ();
	}

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
				"DllImport searching in: '%s' ('%s').", new_scope, found_name);
	g_free (found_name);

#ifdef TARGET_WIN32
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
		error_msg = mono_dl_symbol (module, import, &piinfo->addr); 
	} else {
		char *mangled_name = NULL, *mangled_name2 = NULL;
		int mangle_charset;
		int mangle_stdcall;
		int mangle_param_count;
#ifdef TARGET_WIN32
		int param_count;
#endif

		/*
		 * Search using a variety of mangled names
		 */
		for (mangle_charset = 0; mangle_charset <= 1; mangle_charset ++) {
			for (mangle_stdcall = 0; mangle_stdcall <= 1; mangle_stdcall ++) {
				gboolean need_param_count = FALSE;
#ifdef TARGET_WIN32
				if (mangle_stdcall > 0)
					need_param_count = TRUE;
#endif
				for (mangle_param_count = 0; mangle_param_count <= (need_param_count ? 256 : 0); mangle_param_count += 4) {

					if (piinfo->addr)
						continue;

					mangled_name = (char*)import;
					switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
					case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
						/* Try the mangled name first */
						if (mangle_charset == 0)
							mangled_name = g_strconcat (import, "W", NULL);
						break;
					case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
#ifdef TARGET_WIN32
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

#ifdef TARGET_WIN32
					if (mangle_param_count == 0)
						param_count = mono_method_signature (method)->param_count * sizeof (gpointer);
					else
						/* Try brute force, since it would be very hard to compute the stack usage correctly */
						param_count = mangle_param_count;

					/* Try the stdcall mangled name */
					/* 
					 * gcc under windows creates mangled names without the underscore, but MS.NET
					 * doesn't support it, so we doesn't support it either.
					 */
					if (mangle_stdcall == 1)
						mangled_name2 = g_strdup_printf ("_%s@%d", mangled_name, param_count);
					else
						mangled_name2 = mangled_name;
#else
					mangled_name2 = mangled_name;
#endif

					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
								"Probing '%s'.", mangled_name2);

					error_msg = mono_dl_symbol (module, mangled_name2, &piinfo->addr);
					g_free (error_msg);
					error_msg = NULL;

					if (piinfo->addr)
						mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
									"Found as '%s'.", mangled_name2);

					if (mangled_name != mangled_name2)
						g_free (mangled_name2);
					if (mangled_name != import)
						g_free (mangled_name);
				}
			}
		}
	}

	if (!piinfo->addr) {
		g_free (error_msg);
		if (exc_class) {
			*exc_class = "EntryPointNotFoundException";
			*exc_arg = import;
		}
		return NULL;
	}
	return piinfo->addr;
}

/*
 * LOCKING: assumes the loader lock to be taken.
 */
static MonoMethod *
mono_get_method_from_token (MonoImage *image, guint32 token, MonoClass *klass,
			    MonoGenericContext *context, gboolean *used_context)
{
	MonoMethod *result;
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	MonoGenericContainer *generic_container = NULL, *container = NULL;
	const char *sig = NULL;
	int size;
	guint32 cols [MONO_TYPEDEF_SIZE];

	if (image->dynamic) {
		MonoClass *handle_class;

		result = mono_lookup_dynamic_token_class (image, token, TRUE, &handle_class, context);
		// This checks the memberref type as well
		if (result && handle_class != mono_defaults.methodhandle_class) {
			mono_loader_set_error_bad_image (g_strdup_printf ("Bad method token 0x%08x on image %s.", token, image->name));
			return NULL;
		}
		return result;
	}

	if (table != MONO_TABLE_METHOD) {
		if (table == MONO_TABLE_METHODSPEC) {
			if (used_context) *used_context = TRUE;
			return method_from_methodspec (image, context, idx);
		}
		if (table != MONO_TABLE_MEMBERREF) {
			g_warning ("got wrong token: 0x%08x\n", token);
			mono_loader_set_error_bad_image (g_strdup_printf ("Bad method token 0x%08x on image %s.", token, image->name));
			return NULL;
		}
		return method_from_memberref (image, idx, context, used_context);
	}

	if (used_context) *used_context = FALSE;

	if (idx > image->tables [MONO_TABLE_METHOD].rows) {
		mono_loader_set_error_bad_image (g_strdup_printf ("Bad method token 0x%08x on image %s.", token, image->name));
		return NULL;
	}

	mono_metadata_decode_row (&image->tables [MONO_TABLE_METHOD], idx - 1, cols, 6);

	if ((cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
		result = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethodPInvoke));
	} else {
		result = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethod));
		methods_size += sizeof (MonoMethod);
	}

	mono_stats.method_count ++;

	if (!klass) { /*FIXME put this before the image alloc*/
		guint32 type = mono_metadata_typedef_from_method (image, token);
		if (!type)
			return NULL;
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | type);
		if (klass == NULL)
			return NULL;
	}

	result->slot = -1;
	result->klass = klass;
	result->flags = cols [2];
	result->iflags = cols [1];
	result->token = token;
	result->name = mono_metadata_string_heap (image, cols [3]);

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);

	container = klass->generic_container;

	/* 
	 * load_generic_params does a binary search so only call it if the method 
	 * is generic.
	 */
	if (*sig & 0x10)
		generic_container = mono_metadata_load_generic_params (image, token, container);
	if (generic_container) {
		result->is_generic = TRUE;
		generic_container->owner.method = result;
		/*FIXME put this before the image alloc*/
		if (!mono_metadata_load_generic_param_constraints_full (image, token, generic_container))
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
		if (image->is_module_handle && (cols [1] & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
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

MonoMethod *
mono_get_method (MonoImage *image, guint32 token, MonoClass *klass)
{
	return mono_get_method_full (image, token, klass, NULL);
}

MonoMethod *
mono_get_method_full (MonoImage *image, guint32 token, MonoClass *klass,
		      MonoGenericContext *context)
{
	MonoMethod *result;
	gboolean used_context = FALSE;

	/* We do everything inside the lock to prevent creation races */

	mono_image_lock (image);

	if (mono_metadata_token_table (token) == MONO_TABLE_METHOD) {
		if (!image->method_cache)
			image->method_cache = g_hash_table_new (NULL, NULL);
		result = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (mono_metadata_token_index (token)));
	} else {
		if (!image->methodref_cache)
			image->methodref_cache = g_hash_table_new (NULL, NULL);
		result = g_hash_table_lookup (image->methodref_cache, GINT_TO_POINTER (token));
	}
	mono_image_unlock (image);

	if (result)
		return result;

	result = mono_get_method_from_token (image, token, klass, context, &used_context);
	if (!result)
		return NULL;

	mono_image_lock (image);
	if (!used_context && !result->is_inflated) {
		MonoMethod *result2;
		if (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
			result2 = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (mono_metadata_token_index (token)));
		else
			result2 = g_hash_table_lookup (image->methodref_cache, GINT_TO_POINTER (token));

		if (result2) {
			mono_image_unlock (image);
			return result2;
		}

		if (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
			g_hash_table_insert (image->method_cache, GINT_TO_POINTER (mono_metadata_token_index (token)), result);
		else
			g_hash_table_insert (image->methodref_cache, GINT_TO_POINTER (token), result);
	}

	mono_image_unlock (image);

	return result;
}

static MonoMethod *
get_method_constrained (MonoImage *image, MonoMethod *method, MonoClass *constrained_class, MonoGenericContext *context)
{
	MonoMethod *result;
	MonoClass *ic = NULL;
	MonoGenericContext *method_context = NULL;
	MonoMethodSignature *sig, *original_sig;

	mono_class_init (constrained_class);
	original_sig = sig = mono_method_signature (method);
	if (sig == NULL) {
		return NULL;
	}

	if (method->is_inflated && sig->generic_param_count) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;
		sig = mono_method_signature (imethod->declaring); /*We assume that if the inflated method signature is valid, the declaring method is too*/
		method_context = mono_method_get_context (method);

		original_sig = sig;
		/*
		 * We must inflate the signature with the class instantiation to work on
		 * cases where a class inherit from a generic type and the override replaces
		 * any type argument which a concrete type. See #325283.
		 */
		if (method_context->class_inst) {
			MonoError error;
			MonoGenericContext ctx;
			ctx.method_inst = NULL;
			ctx.class_inst = method_context->class_inst;
			/*Fixme, property propagate this error*/
			sig = inflate_generic_signature_checked (method->klass->image, sig, &ctx, &error);
			if (!mono_error_ok (&error)) {
				mono_error_cleanup (&error);
				return NULL;
			}
		}
	}

	if ((constrained_class != method->klass) && (MONO_CLASS_IS_INTERFACE (method->klass)))
		ic = method->klass;

	result = find_method (constrained_class, ic, method->name, sig, constrained_class);
	if (sig != original_sig)
		mono_metadata_free_inflated_signature (sig);

	if (!result) {
		char *m = mono_method_full_name (method, 1);
		g_warning ("Missing method %s.%s.%s in assembly %s method %s", method->klass->name_space,
			   method->klass->name, method->name, image->name, m);
		g_free (m);
		return NULL;
	}

	if (method_context)
		result = mono_class_inflate_generic_method (result, method_context);

	return result;
}

MonoMethod *
mono_get_method_constrained_with_method (MonoImage *image, MonoMethod *method, MonoClass *constrained_class,
			     MonoGenericContext *context)
{
	MonoMethod *result;

	g_assert (method);

	mono_loader_lock ();

	result = get_method_constrained (image, method, constrained_class, context);

	mono_loader_unlock ();
	return result;	
}
/**
 * mono_get_method_constrained:
 *
 * This is used when JITing the `constrained.' opcode.
 *
 * This returns two values: the contrained method, which has been inflated
 * as the function return value;   And the original CIL-stream method as
 * declared in cil_method.  The later is used for verification.
 */
MonoMethod *
mono_get_method_constrained (MonoImage *image, guint32 token, MonoClass *constrained_class,
			     MonoGenericContext *context, MonoMethod **cil_method)
{
	MonoMethod *result;

	mono_loader_lock ();

	*cil_method = mono_get_method_from_token (image, token, NULL, context, NULL);
	if (!*cil_method) {
		mono_loader_unlock ();
		return NULL;
	}

	result = get_method_constrained (image, *cil_method, constrained_class, context);

	mono_loader_unlock ();
	return result;
}

void
mono_free_method  (MonoMethod *method)
{
	if (mono_profiler_get_events () & MONO_PROFILE_METHOD_EVENTS)
		mono_profiler_method_free (method);
	
	/* FIXME: This hack will go away when the profiler will support freeing methods */
	if (mono_profiler_get_events () != MONO_PROFILE_NONE)
		return;
	
	if (method->signature) {
		/* 
		 * FIXME: This causes crashes because the types inside signatures and
		 * locals are shared.
		 */
		/* mono_metadata_free_method_signature (method->signature); */
		/* g_free (method->signature); */
	}
	
	if (method->dynamic) {
		MonoMethodWrapper *mw = (MonoMethodWrapper*)method;
		int i;

		mono_marshal_free_dynamic_wrappers (method);

		mono_image_property_remove (method->klass->image, method);

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

	signature = mono_method_signature (method);
	/*FIXME this check is somewhat redundant since the caller usally will have to get the signature to figure out the
	  number of arguments and allocate a properly sized array. */
	if (signature == NULL)
		return;

	if (!signature->param_count)
		return;

	for (i = 0; i < signature->param_count; ++i)
		names [i] = "";

	klass = method->klass;
	if (klass->rank)
		return;

	mono_class_init (klass);

	if (klass->image->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			g_hash_table_lookup (
				((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		if (method_aux && method_aux->param_names) {
			for (i = 0; i < mono_method_signature (method)->param_count; ++i)
				if (method_aux->param_names [i + 1])
					names [i] = method_aux->param_names [i + 1];
		}
		return;
	}

	if (method->wrapper_type) {
		char **pnames = NULL;

		mono_image_lock (klass->image);
		if (klass->image->wrapper_param_names)
			pnames = g_hash_table_lookup (klass->image->wrapper_param_names, method);
		mono_image_unlock (klass->image);

		if (pnames) {
			for (i = 0; i < signature->param_count; ++i)
				names [i] = pnames [i];
		}
		return;
	}

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	paramt = &klass->image->tables [MONO_TABLE_PARAM];
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
				names [cols [MONO_PARAM_SEQUENCE] - 1] = mono_metadata_string_heap (klass->image, cols [MONO_PARAM_NAME]);
		}
	}
}

guint32
mono_method_get_param_token (MonoMethod *method, int index)
{
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	guint32 idx;

	mono_class_init (klass);

	if (klass->image->dynamic) {
		g_assert_not_reached ();
	}

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
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

void
mono_method_get_marshal_info (MonoMethod *method, MonoMarshalSpec **mspecs)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;
	MonoMethodSignature *signature;
	guint32 idx;

	signature = mono_method_signature (method);
	g_assert (signature); /*FIXME there is no way to signal error from this function*/

	for (i = 0; i < signature->param_count + 1; ++i)
		mspecs [i] = NULL;

	if (method->klass->image->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			g_hash_table_lookup (
				((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
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

	mono_class_init (klass);

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	paramt = &klass->image->tables [MONO_TABLE_PARAM];
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
				tp = mono_metadata_get_marshal_info (klass->image, i - 1, FALSE);
				g_assert (tp);
				mspecs [cols [MONO_PARAM_SEQUENCE]]= mono_metadata_parse_marshal_spec (klass->image, tp);
			}
		}

		return;
	}
}

gboolean
mono_method_has_marshal_info (MonoMethod *method)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;
	guint32 idx;

	if (method->klass->image->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			g_hash_table_lookup (
				((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		MonoMarshalSpec **dyn_specs = method_aux->param_marshall;
		if (dyn_specs) {
			for (i = 0; i < mono_method_signature (method)->param_count + 1; ++i)
				if (dyn_specs [i])
					return TRUE;
		}
		return FALSE;
	}

	mono_class_init (klass);

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	paramt = &klass->image->tables [MONO_TABLE_PARAM];
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

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;
	data = ((MonoMethodWrapper *)method)->method_data;
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
	StackWalkUserData *d = data;

	switch (frame->type) {
	case FRAME_TYPE_DEBUGGER_INVOKE:
	case FRAME_TYPE_MANAGED_TO_NATIVE:
		return FALSE;
	case FRAME_TYPE_MANAGED:
		g_assert (frame->ji);
		return d->func (frame->ji->method, frame->native_offset, frame->il_offset, frame->managed, d->user_data);
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

void
mono_stack_walk_no_il (MonoStackWalk func, gpointer user_data)
{
	StackWalkUserData ud = { func, user_data };
	mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (stack_walk_adapter, NULL, MONO_UNWIND_DEFAULT, &ud);
}

static gboolean
last_managed (MonoMethod *m, gint no, gint ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = data;
	*dest = m;
	/*g_print ("In %s::%s [%d] [%d]\n", m->klass->name, m->name, no, ilo);*/

	return managed;
}

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
 * See docs/thread-safety.txt for the locking strategy.
 */
void
mono_loader_lock (void)
{
	mono_locks_acquire (&loader_mutex, LoaderLock);
	if (G_UNLIKELY (loader_lock_track_ownership)) {
		mono_native_tls_set_value (loader_lock_nest_id, GUINT_TO_POINTER (GPOINTER_TO_UINT (mono_native_tls_get_value (loader_lock_nest_id)) + 1));
	}
}

void
mono_loader_unlock (void)
{
	mono_locks_release (&loader_mutex, LoaderLock);
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
 * mono_method_signature:
 *
 * Return the signature of the method M. On failure, returns NULL, and ERR is set.
 */
MonoMethodSignature*
mono_method_signature_checked (MonoMethod *m, MonoError *error)
{
	int idx;
	int size;
	MonoImage* img;
	const char *sig;
	gboolean can_cache_signature;
	MonoGenericContainer *container;
	MonoMethodSignature *signature = NULL;
	guint32 sig_offset;

	/* We need memory barriers below because of the double-checked locking pattern */ 

	mono_error_init (error);

	if (m->signature)
		return m->signature;

	mono_loader_lock ();

	if (m->signature) {
		mono_loader_unlock ();
		return m->signature;
	}

	if (m->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) m;
		/* the lock is recursive */
		signature = mono_method_signature (imethod->declaring);
		signature = inflate_generic_signature_checked (imethod->declaring->klass->image, signature, mono_method_get_context (m), error);
		if (!mono_error_ok (error)) {
			mono_loader_unlock ();
			return NULL;
		}

		inflated_signatures_size += mono_metadata_signature_size (signature);

		mono_memory_barrier ();
		m->signature = signature;
		mono_loader_unlock ();
		return m->signature;
	}

	g_assert (mono_metadata_token_table (m->token) == MONO_TABLE_METHOD);
	idx = mono_metadata_token_index (m->token);
	img = m->klass->image;

	sig = mono_metadata_blob_heap (img, sig_offset = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_SIGNATURE));

	g_assert (!m->klass->generic_class);
	container = mono_method_get_generic_container (m);
	if (!container)
		container = m->klass->generic_container;

	/* Generic signatures depend on the container so they cannot be cached */
	/* icall/pinvoke signatures cannot be cached cause we modify them below */
	can_cache_signature = !(m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && !(m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) && !container;

	/* If the method has parameter attributes, that can modify the signature */
	if (mono_metadata_method_has_param_attrs (img, idx))
		can_cache_signature = FALSE;

	if (can_cache_signature)
		signature = g_hash_table_lookup (img->method_signatures, sig);

	if (!signature) {
		const char *sig_body;
		/*TODO we should cache the failure result somewhere*/
		if (!mono_verifier_verify_method_signature (img, sig_offset, error)) {
			mono_loader_unlock ();
			return NULL;
		}

		size = mono_metadata_decode_blob_size (sig, &sig_body);

		signature = mono_metadata_parse_method_signature_full (img, container, idx, sig_body, NULL);
		if (!signature) {
			mono_loader_clear_error ();
			mono_loader_unlock ();
			mono_error_set_method_load (error, m->klass, m->name, "");
			return NULL;
		}

		if (can_cache_signature)
			g_hash_table_insert (img->method_signatures, (gpointer)sig, signature);

		signatures_size += mono_metadata_signature_size (signature);
	}

	/* Verify metadata consistency */
	if (signature->generic_param_count) {
		if (!container || !container->is_method) {
			mono_loader_unlock ();
			mono_error_set_method_load (error, m->klass, m->name, "Signature claims method has generic parameters, but generic_params table says it doesn't for method 0x%08x from image %s", idx, img->name);
			return NULL;
		}
		if (container->type_argc != signature->generic_param_count) {
			mono_loader_unlock ();
			mono_error_set_method_load (error, m->klass, m->name, "Inconsistent generic parameter count.  Signature says %d, generic_params table says %d for method 0x%08x from image %s", signature->generic_param_count, container->type_argc, idx, img->name);
			return NULL;
		}
	} else if (container && container->is_method && container->type_argc) {
		mono_loader_unlock ();
		mono_error_set_method_load (error, m->klass, m->name, "generic_params table claims method has generic parameters, but signature says it doesn't for method 0x%08x from image %s", idx, img->name);
		return NULL;
	}
	if (m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		signature->pinvoke = 1;
	else if (m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		MonoCallConvention conv = 0;
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
		default:
			mono_loader_unlock ();
			mono_error_set_method_load (error, m->klass, m->name, "unsupported calling convention : 0x%04x for method 0x%08x from image %s", piinfo->piflags, idx, img->name);
			return NULL;
		}
		signature->call_convention = conv;
	}

	mono_memory_barrier ();
	m->signature = signature;

	mono_loader_unlock ();
	return m->signature;
}

/**
 * mono_method_signature:
 *
 * Return the signature of the method M. On failure, returns NULL.
 */
MonoMethodSignature*
mono_method_signature (MonoMethod *m)
{
	MonoError error;
	MonoMethodSignature *sig;

	sig = mono_method_signature_checked (m, &error);
	if (!sig) {
		char *type_name = mono_type_get_full_name (m->klass);
		g_warning ("Could not load signature of %s:%s due to: %s", type_name, m->name, mono_error_get_message (&error));
		g_free (type_name);
		mono_error_cleanup (&error);
	}

	return sig;
}

const char*
mono_method_get_name (MonoMethod *method)
{
	return method->name;
}

MonoClass*
mono_method_get_class (MonoMethod *method)
{
	return method->klass;
}

guint32
mono_method_get_token (MonoMethod *method)
{
	return method->token;
}

MonoMethodHeader*
mono_method_get_header (MonoMethod *method)
{
	int idx;
	guint32 rva;
	MonoImage* img;
	gpointer loc;
	MonoMethodHeader *header;

	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return NULL;

	if (method->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;
		MonoMethodHeader *header;

		mono_loader_lock ();

		if (imethod->header) {
			mono_loader_unlock ();
			return imethod->header;
		}

		header = mono_method_get_header (imethod->declaring);
		if (!header) {
			mono_loader_unlock ();
			return NULL;
		}

		imethod->header = inflate_generic_header (header, mono_method_get_context (method));
		mono_loader_unlock ();
		mono_metadata_free_mh (header);
		return imethod->header;
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
	img = method->klass->image;
	rva = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_RVA);

	if (!mono_verifier_verify_method_header (img, rva, NULL))
		return NULL;

	loc = mono_image_rva_map (img, rva);
	if (!loc)
		return NULL;

	header = mono_metadata_parse_mh_full (img, mono_method_get_generic_container (method), loc);

	return header;
}

guint32
mono_method_get_flags (MonoMethod *method, guint32 *iflags)
{
	if (iflags)
		*iflags = method->iflags;
	return method->flags;
}

/*
 * Find the method index in the metadata methodDef table.
 */
guint32
mono_method_get_index (MonoMethod *method) {
	MonoClass *klass = method->klass;
	int i;

	if (klass->rank)
		/* constructed array methods are not in the MethodDef table */
		return 0;

	if (method->token)
		return mono_metadata_token_index (method->token);

	mono_class_setup_methods (klass);
	if (klass->exception_type)
		return 0;
	for (i = 0; i < klass->method.count; ++i) {
		if (method == klass->methods [i]) {
			if (klass->image->uncompressed_metadata)
				return mono_metadata_translate_token_index (klass->image, MONO_TABLE_METHOD, klass->method.first + i + 1);
			else
				return klass->method.first + i + 1;
		}
	}
	return 0;
}

