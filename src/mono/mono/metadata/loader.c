/*
 * loader.c: Image Loader 
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright (C) 2002-2006 Novell, Inc.
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
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-membar.h>

MonoDefaults mono_defaults;

/*
 * This lock protects the hash tables inside MonoImage used by the metadata 
 * loading functions in class.c and loader.c.
 */
static CRITICAL_SECTION loader_mutex;


/*
 * This TLS variable contains the last type load error encountered by the loader.
 */
guint32 loader_error_thread_id;

void
mono_loader_init ()
{
	InitializeCriticalSection (&loader_mutex);

	loader_error_thread_id = TlsAlloc ();
}

void
mono_loader_cleanup (void)
{
	TlsFree (loader_error_thread_id);

	/*DeleteCriticalSection (&loader_mutex);*/
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
	TlsSetValue (loader_error_thread_id, error);
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
		g_warning ("Cannot resolve dependency to assembly '%s' because it has not been preloaded. When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.", assembly_name);
	else
		g_warning ("Could not load file or assembly '%s' or one of its dependencies.", assembly_name);

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
	g_warning ("The class %s could not be loaded, used in %s", class_name, assembly_name);

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
	return (MonoLoaderError*)TlsGetValue (loader_error_thread_id);
}

/**
 * mono_loader_clear_error:
 *
 * Disposes any loader error messages on this thread
 */
void
mono_loader_clear_error (void)
{
	MonoLoaderError *ex = (MonoLoaderError*)TlsGetValue (loader_error_thread_id);

	if (ex) {
		g_free (ex->class_name);
		g_free (ex->assembly_name);
		g_free (ex->msg);
		g_free (ex);
	
		TlsSetValue (loader_error_thread_id, NULL);
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
		char *cnspace = g_strdup (*error->klass->name_space ? error->klass->name_space : "");
		char *cname = g_strdup (error->klass->name);
		char *cmembername = g_strdup (error->member_name);
                char *class_name;

		mono_loader_clear_error ();
		class_name = g_strdup_printf ("%s%s%s", cnspace, cnspace ? "." : "", cname);
		
		ex = mono_get_exception_missing_field (class_name, cmembername);
		g_free (class_name);
		g_free (cname);
		g_free (cmembername);
		g_free (cnspace);
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

static MonoClassField*
field_from_memberref (MonoImage *image, guint32 token, MonoClass **retklass,
		      MonoGenericContext *context)
{
	MonoClass *klass;
	MonoClassField *field;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class;
	const char *fname;
	const char *ptr;
	guint32 idx = mono_metadata_token_index (token);

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;

	fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	/* we may want to check the signature here... */

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | nindex);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_DEF | nindex);
			g_warning ("Missing field %s in class %s (typedef index %d)", fname, name, nindex);
			mono_loader_set_error_type_load (name, image->assembly_name);
			g_free (name);
			return NULL;
		}
		mono_class_init (klass);
		if (retklass)
			*retklass = klass;
		field = mono_class_get_field_from_name (klass, fname);
		break;
	case MONO_MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_REF | nindex);
			g_warning ("missing field %s in class %s (typeref index %d)", fname, name, nindex);
			mono_loader_set_error_type_load (name, image->assembly_name);
			g_free (name);
			return NULL;
		}
		mono_class_init (klass);
		if (retklass)
			*retklass = klass;
		field = mono_class_get_field_from_name (klass, fname);
		break;
	case MONO_MEMBERREF_PARENT_TYPESPEC: {
		/*guint32 bcols [MONO_TYPESPEC_SIZE];
		guint32 len;
		MonoType *type;

		mono_metadata_decode_row (&tables [MONO_TABLE_TYPESPEC], nindex - 1, 
					  bcols, MONO_TYPESPEC_SIZE);
		ptr = mono_metadata_blob_heap (image, bcols [MONO_TYPESPEC_SIGNATURE]);
		len = mono_metadata_decode_value (ptr, &ptr);	
		type = mono_metadata_parse_type (image, MONO_PARSE_TYPE, 0, ptr, &ptr);

		klass = mono_class_from_mono_type (type);
		mono_class_init (klass);
		g_print ("type in sig: %s\n", klass->name);*/
		klass = mono_class_get_full (image, MONO_TOKEN_TYPE_SPEC | nindex, context);
		//FIXME can't klass be null?
		mono_class_init (klass);
		if (retklass)
			*retklass = klass;
		field = mono_class_get_field_from_name (klass, fname);
		break;
	}
	default:
		g_warning ("field load from %x", class);
		return NULL;
	}

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
		if (result && handle_class != mono_defaults.fieldhandle_class) {
			mono_loader_set_error_bad_image (g_strdup ("Bad field token."));
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
	if (field && !field->parent->generic_class && !field->parent->generic_container)
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
find_method_in_class (MonoClass *in_class, const char *name, const char *qname, const char *fqname,
		      MonoMethodSignature *sig, MonoClass *from_class)
{
	int i;

	mono_class_setup_methods (in_class);
	for (i = 0; i < in_class->method.count; ++i) {
		MonoMethod *m = in_class->methods [i];

		if (!((fqname && !strcmp (m->name, fqname)) ||
		      (qname && !strcmp (m->name, qname)) ||
		      (name && !strcmp (m->name, name))))
			continue;

		if (sig->call_convention == MONO_CALL_VARARG) {
			if (mono_metadata_signature_vararg_match (sig, mono_method_signature (m)))
				break;
		} else {
			if (mono_metadata_signature_equal (sig, mono_method_signature (m)))
				break;
		}
	}

	if (i < in_class->method.count)
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

		g_assert (from_class->interface_count == in_class->interface_count);
		for (i = 0; i < in_class->interface_count; i++) {
			MonoClass *in_ic = in_class->interfaces [i];
			MonoClass *from_ic = from_class->interfaces [i];
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
inflate_generic_signature (MonoImage *image, MonoMethodSignature *sig, MonoGenericContext *context)
{
	MonoMethodSignature *res;
	gboolean is_open;
	int i;

	if (!context)
		return sig;

	res = g_malloc0 (sizeof (MonoMethodSignature) + ((gint32)sig->param_count - MONO_ZERO_LEN_ARRAY) * sizeof (MonoType*));
	res->param_count = sig->param_count;
	res->sentinelpos = -1;
	res->ret = mono_class_inflate_generic_type (sig->ret, context);
	is_open = mono_class_is_open_constructed_type (res->ret);
	for (i = 0; i < sig->param_count; ++i) {
		res->params [i] = mono_class_inflate_generic_type (sig->params [i], context);
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
}

static MonoMethodHeader*
inflate_generic_header (MonoMethodHeader *header, MonoGenericContext *context)
{
	MonoMethodHeader *res;
	int i;
	res = g_malloc0 (sizeof (MonoMethodHeader) + sizeof (gpointer) * header->num_locals);
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
			MonoType *t;
			if (clause->flags != MONO_EXCEPTION_CLAUSE_NONE)
				continue;
			t = mono_class_inflate_generic_type (&clause->data.catch_class->byval_arg, context);
			clause->data.catch_class = mono_class_from_mono_type (t);
			mono_metadata_free_type (t);
		}
	}
	return res;
}

/*
 * token is the method_ref or method_def token used in a call IL instruction.
 */
MonoMethodSignature*
mono_method_get_signature_full (MonoMethod *method, MonoImage *image, guint32 token, MonoGenericContext *context)
{
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	guint32 cols [MONO_MEMBERREF_SIZE];
	MonoMethodSignature *sig, *prev_sig;
	const char *ptr;

	/* !table is for wrappers: we should really assign their own token to them */
	if (!table || table == MONO_TABLE_METHOD)
		return mono_method_signature (method);

	if (table == MONO_TABLE_METHODSPEC) {
		g_assert (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) &&
			  !(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
			  mono_method_signature (method));
		g_assert (method->is_inflated);

		return mono_method_signature (method);
	}

	if (method->klass->generic_class)
		return mono_method_signature (method);

	if (image->dynamic)
		/* FIXME: This might be incorrect for vararg methods */
		return mono_method_signature (method);

	mono_loader_lock ();
	sig = g_hash_table_lookup (image->memberref_signatures, GUINT_TO_POINTER (token));
	mono_loader_unlock ();
	if (!sig) {
		mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);

		ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
		mono_metadata_decode_blob_size (ptr, &ptr);
		sig = mono_metadata_parse_method_signature (image, 0, ptr, NULL);

		mono_loader_lock ();
		prev_sig = g_hash_table_lookup (image->memberref_signatures, GUINT_TO_POINTER (token));
		if (prev_sig) {
			/* Somebody got in before us */
			sig = prev_sig;
		}
		else
			g_hash_table_insert (image->memberref_signatures, GUINT_TO_POINTER (token), sig);
		mono_loader_unlock ();
	}

	if (context) {
		MonoMethodSignature *cached;

		/* This signature is not owned by a MonoMethod, so need to cache */
		sig = inflate_generic_signature (image, sig, context);
		cached = mono_metadata_get_inflated_signature (sig, context);
		if (cached != sig)
			mono_metadata_free_inflated_signature (sig);
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
static MonoMethod*
search_in_array_class (MonoClass *klass, const char *name, MonoMethodSignature *sig)
{
	int i;
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
	guint32 nindex, class;
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

	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);

	sig = mono_metadata_parse_method_signature (image, 0, ptr, NULL);
	if (sig == NULL)
		return NULL;

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		method = find_method (klass, NULL, mname, sig, klass);
		break;

	case MONO_MEMBERREF_PARENT_TYPESPEC: {
		MonoType *type;
		MonoMethod *result;

		type = &klass->byval_arg;

		if (type->type != MONO_TYPE_ARRAY && type->type != MONO_TYPE_SZARRAY) {
			MonoClass *in_class = klass->generic_class ? klass->generic_class->container_class : klass;
			method = find_method (in_class, NULL, mname, sig, klass);
			break;
		}

		/* we're an array and we created these methods already in klass in mono_class_init () */
		result = search_in_array_class (klass, mname, sig);
		if (result)
			return result;

		g_assert_not_reached ();
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
	mono_metadata_free_method_signature (sig);

	return method;
}

static MonoMethod *
method_from_methodspec (MonoImage *image, MonoGenericContext *context, guint32 idx)
{
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

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODSPEC_SIGNATURE]);

	mono_metadata_decode_value (ptr, &ptr);
	ptr++;
	param_count = mono_metadata_decode_value (ptr, &ptr);
	g_assert (param_count);

	inst = mono_metadata_parse_generic_inst (image, NULL, param_count, ptr, &ptr);
	if (context && inst->is_open)
		inst = mono_metadata_inflate_generic_inst (inst, context);

	if ((token & MONO_METHODDEFORREF_MASK) == MONO_METHODDEFORREF_METHODDEF)
		method = mono_get_method_full (image, MONO_TOKEN_METHOD_DEF | nindex, NULL, context);
	else
		method = method_from_memberref (image, nindex, context, NULL);

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

void
mono_dllmap_insert (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc) {
	MonoDllMap *entry;

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
		MonoMemPool *mpool = assembly->mempool;
		entry = mono_mempool_alloc0 (mpool, sizeof (MonoDllMap));
		entry->dll = dll? mono_mempool_strdup (mpool, dll): NULL;
		entry->target = tdll? mono_mempool_strdup (mpool, tdll): NULL;
		entry->func = func? mono_mempool_strdup (mpool, func): NULL;
		entry->target_func = tfunc? mono_mempool_strdup (mpool, tfunc): NULL;
		entry->next = assembly->dll_map;
		assembly->dll_map = entry;
	}

	mono_loader_unlock ();
}

static GHashTable *global_module_map;

static MonoDl*
cached_module_load (const char *name, int flags, char **err)
{
	MonoDl *res;
	mono_loader_lock ();
	if (!global_module_map)
		global_module_map = g_hash_table_new (g_str_hash, g_str_equal);
	res = g_hash_table_lookup (global_module_map, name);
	if (res) {
		*err = NULL;
		mono_loader_unlock ();
		return res;
	}
	res = mono_dl_open (name, flags, err);
	if (res)
		g_hash_table_insert (global_module_map, g_strdup (name), res);
	mono_loader_unlock ();
	return res;
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
	char *full_name, *file_name;
	int i;
	MonoDl *module = NULL;

	g_assert (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL);

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
		if (!piinfo->implmap_idx)
			return NULL;

		mono_metadata_decode_row (im, piinfo->implmap_idx - 1, im_cols, MONO_IMPLMAP_SIZE);

		piinfo->piflags = im_cols [MONO_IMPLMAP_FLAGS];
		import = mono_metadata_string_heap (image, im_cols [MONO_IMPLMAP_NAME]);
		scope_token = mono_metadata_decode_row_col (mr, im_cols [MONO_IMPLMAP_SCOPE] - 1, MONO_MODULEREF_NAME);
		orig_scope = mono_metadata_string_heap (image, scope_token);
	}

	mono_dllmap_lookup (image, orig_scope, import, &new_scope, &import);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
			"DllImport attempting to load: '%s'.", new_scope);

	if (exc_class) {
		*exc_class = NULL;
		*exc_arg = NULL;
	}

	/* we allow a special name to dlopen from the running process namespace */
	if (strcmp (new_scope, "__Internal") == 0)
		module = mono_dl_open (NULL, MONO_DL_LAZY, &error_msg);

	/*
	 * Try loading the module using a variety of names
	 */
	for (i = 0; i < 4; ++i) {
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
			if (strstr (new_scope, "lib") != new_scope) {
				file_name = g_strdup_printf ("lib%s", new_scope);
			}
			else
				continue;
			break;
		default:
#ifndef PLATFORM_WIN32
			if (!g_ascii_strcasecmp ("user32.dll", new_scope) ||
			    !g_ascii_strcasecmp ("kernel32.dll", new_scope) ||
			    !g_ascii_strcasecmp ("user32", new_scope) ||
			    !g_ascii_strcasecmp ("kernel", new_scope)) {
				file_name = g_strdup ("libMonoSupportW.so");
			} else
#endif
				    continue;
#ifndef PLATFORM_WIN32
			break;
#endif
		}

		if (!module) {
			void *iter = NULL;
			while ((full_name = mono_dl_build_path (NULL, file_name, &iter))) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport loading location: '%s'.", full_name);
				module = cached_module_load (full_name, MONO_DL_LAZY, &error_msg);
				if (!module) {
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
							"DllImport error loading library: '%s'.",
							error_msg);
					g_free (error_msg);
				}
				g_free (full_name);
				if (module)
					break;
			}
		}

		if (!module) {
			void *iter = NULL;
			while ((full_name = mono_dl_build_path (".", file_name, &iter))) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loading library: '%s'.", full_name);
				module = cached_module_load (full_name, MONO_DL_LAZY, &error_msg);
				if (!module) {
					mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s'.",
						error_msg);
					g_free (error_msg);
				}
				g_free (full_name);
				if (module)
					break;
			}
		}

		if (!module) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loading: '%s'.", file_name);
			module = cached_module_load (file_name, MONO_DL_LAZY, &error_msg);
			if (!module) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s'.",
						error_msg);
			}
		}

		g_free (file_name);

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

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
				"Searching for '%s'.", import);

	if (piinfo->piflags & PINVOKE_ATTRIBUTE_NO_MANGLE) {
		error_msg = mono_dl_symbol (module, import, &piinfo->addr); 
	} else {
		char *mangled_name = NULL, *mangled_name2 = NULL;
		int mangle_charset;
		int mangle_stdcall;
		int mangle_param_count;
#ifdef PLATFORM_WIN32
		int param_count;
#endif

		/*
		 * Search using a variety of mangled names
		 */
		for (mangle_charset = 0; mangle_charset <= 1; mangle_charset ++) {
			for (mangle_stdcall = 0; mangle_stdcall <= 1; mangle_stdcall ++) {
				gboolean need_param_count = FALSE;
#ifdef PLATFORM_WIN32
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
#ifdef PLATFORM_WIN32
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

#ifdef PLATFORM_WIN32
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
	int size, i;
	guint32 cols [MONO_TYPEDEF_SIZE];

	if (image->dynamic) {
		MonoClass *handle_class;

		result = mono_lookup_dynamic_token_class (image, token, TRUE, &handle_class, context);
		// This checks the memberref type as well
		if (result && handle_class != mono_defaults.methodhandle_class) {
			mono_loader_set_error_bad_image (g_strdup ("Bad method token."));
			return NULL;
		}
		return result;
	}

	if (table != MONO_TABLE_METHOD) {
		if (table == MONO_TABLE_METHODSPEC) {
			if (used_context) *used_context = TRUE;
			return method_from_methodspec (image, context, idx);
		}
		if (table != MONO_TABLE_MEMBERREF)
			g_print("got wrong token: 0x%08x\n", token);
		g_assert (table == MONO_TABLE_MEMBERREF);
		return method_from_memberref (image, idx, context, used_context);
	}

	if (used_context) *used_context = FALSE;

	if (idx > image->tables [MONO_TABLE_METHOD].rows) {
		mono_loader_set_error_bad_image (g_strdup ("Bad method token."));
		return NULL;
	}

	mono_metadata_decode_row (&image->tables [MONO_TABLE_METHOD], idx - 1, cols, 6);

	if ((cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		result = (MonoMethod *)mono_mempool_alloc0 (image->mempool, sizeof (MonoMethodPInvoke));
	else
		result = (MonoMethod *)mono_mempool_alloc0 (image->mempool, sizeof (MonoMethodNormal));

	mono_stats.method_count ++;

	if (!klass) {
		guint32 type = mono_metadata_typedef_from_method (image, token);
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

	container = klass->generic_container;
	generic_container = mono_metadata_load_generic_params (image, token, container);
	if (generic_container) {
		result->is_generic = TRUE;
		generic_container->owner.method = result;

		mono_metadata_load_generic_param_constraints (image, token, generic_container);

		for (i = 0; i < generic_container->type_argc; i++)
			mono_class_from_generic_parameter (&generic_container->type_params [i], image, TRUE);

		container = generic_container;
	}


	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);

	if (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (result->klass == mono_defaults.string_class && !strcmp (result->name, ".ctor"))
			result->string_ctor = 1;
	} else if (cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)result;

#ifdef PLATFORM_WIN32
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

static gpointer
get_method_token (gpointer value)
{
	MonoMethod *m = (MonoMethod*)value;

	return GUINT_TO_POINTER (m->token);
}

MonoMethod *
mono_get_method_full (MonoImage *image, guint32 token, MonoClass *klass,
		      MonoGenericContext *context)
{
	MonoMethod *result;
	gboolean used_context = FALSE;

	/* We do everything inside the lock to prevent creation races */

	mono_loader_lock ();

	if (mono_metadata_token_table (token) == MONO_TABLE_METHOD) {
		if (!image->method_cache)
			image->method_cache = mono_value_hash_table_new (NULL, NULL, get_method_token);
		result = mono_value_hash_table_lookup (image->method_cache, GINT_TO_POINTER (token));
	} else {
		if (!image->methodref_cache)
			image->methodref_cache = g_hash_table_new (NULL, NULL);
		result = g_hash_table_lookup (image->methodref_cache, GINT_TO_POINTER (token));
	}
	if (result) {
		mono_loader_unlock ();
		return result;
	}

	result = mono_get_method_from_token (image, token, klass, context, &used_context);

	//printf ("GET: %s\n", mono_method_full_name (result, TRUE));

#if 0
	g_message (G_STRLOC ": %s - %d - %d", mono_method_full_name (result, TRUE),
		   result->is_inflated, used_context);
#endif

	/*
	 * `used_context' specifies whether or not mono_get_method_from_token() actually
	 * used the `context' to get the method.  See bug #80969.
	 */

	if (!used_context && !(result && result->is_inflated) && result) {
		if (mono_metadata_token_table (token) == MONO_TABLE_METHOD) {
			mono_value_hash_table_insert (image->method_cache, GINT_TO_POINTER (token), result);
		} else {
			g_hash_table_insert (image->methodref_cache, GINT_TO_POINTER (token), result);
		}
	}

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
	MonoMethod *method, *result;
	MonoClass *ic = NULL;
	MonoGenericContext *class_context = NULL, *method_context = NULL;
	MonoMethodSignature *sig, *original_sig;

	mono_loader_lock ();

	*cil_method = mono_get_method_from_token (image, token, NULL, context, NULL);
	if (!*cil_method) {
		mono_loader_unlock ();
		return NULL;
	}

	mono_class_init (constrained_class);
	method = *cil_method;
	original_sig = sig = mono_method_signature (method);

	if (method->is_inflated && sig->generic_param_count) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;
		sig = mono_method_signature (imethod->declaring);
		method_context = mono_method_get_context (method);

		original_sig = sig;
		/*
		 * We must inflate the signature with the class instantiation to work on
		 * cases where a class inherit from a generic type and the override replaces
		 * and type argument which a concrete type. See #325283.
		 */
		if (method_context->class_inst) {
			MonoGenericContext ctx;
			ctx.method_inst = NULL;
			ctx.class_inst = method_context->class_inst;
		
			sig = inflate_generic_signature (method->klass->image, sig, &ctx);
		}
	}

	if ((constrained_class != method->klass) && (method->klass->interface_id != 0))
		ic = method->klass;

	if (constrained_class->generic_class)
		class_context = mono_class_get_context (constrained_class);

	result = find_method (constrained_class, ic, method->name, sig, constrained_class);
	if (sig != original_sig)
		mono_metadata_free_inflated_signature (sig);

	if (!result) {
		g_warning ("Missing method %s.%s.%s in assembly %s token %x", method->klass->name_space,
			   method->klass->name, method->name, image->name, token);
		mono_loader_unlock ();
		return NULL;
	}

	if (class_context)
		result = mono_class_inflate_generic_method (result, class_context);
	if (method_context)
		result = mono_class_inflate_generic_method (result, method_context);

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

		mono_loader_lock ();
		mono_property_hash_remove_object (method->klass->image->property_hash, method);
		mono_loader_unlock ();

		g_free ((char*)method->name);
		if (mw->method.header) {
			g_free ((char*)mw->method.header->code);
			for (i = 0; i < mw->method.header->num_locals; ++i)
				g_free (mw->method.header->locals [i]);
			g_free (mw->method.header->clauses);
			g_free (mw->method.header);
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
	guint32 idx;

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	if (!mono_method_signature (method)->param_count)
		return;
	for (i = 0; i < mono_method_signature (method)->param_count; ++i)
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
			if (cols [MONO_PARAM_SEQUENCE]) /* skip return param spec */
				names [cols [MONO_PARAM_SEQUENCE] - 1] = mono_metadata_string_heap (klass->image, cols [MONO_PARAM_NAME]);
		}
		return;
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
	guint32 idx;

	for (i = 0; i < mono_method_signature (method)->param_count + 1; ++i)
		mspecs [i] = NULL;

	if (method->klass->image->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			g_hash_table_lookup (
				((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		if (method_aux && method_aux->param_marshall) {
			MonoMarshalSpec **dyn_specs = method_aux->param_marshall;
			for (i = 0; i < mono_method_signature (method)->param_count + 1; ++i)
				if (dyn_specs [i]) {
					mspecs [i] = g_new0 (MonoMarshalSpec, 1);
					memcpy (mspecs [i], dyn_specs [i], sizeof (MonoMarshalSpec));
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

			if (cols [MONO_PARAM_FLAGS] & PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL) {
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

	data = ((MonoMethodWrapper *)method)->method_data;
	g_assert (data != NULL);
	g_assert (id <= GPOINTER_TO_UINT (*data));
	return data [id];
}

static void
default_stack_walk (MonoStackWalk func, gboolean do_il_offset, gpointer user_data) {
	g_error ("stack walk not installed");
}

static MonoStackWalkImpl stack_walk = default_stack_walk;

void
mono_stack_walk (MonoStackWalk func, gpointer user_data)
{
	stack_walk (func, TRUE, user_data);
}

void
mono_stack_walk_no_il (MonoStackWalk func, gpointer user_data)
{
	stack_walk (func, FALSE, user_data);
}

void
mono_install_stack_walk (MonoStackWalkImpl func)
{
	stack_walk = func;
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
	stack_walk (last_managed, FALSE, &m);
	return m;
}

void
mono_loader_lock (void)
{
	EnterCriticalSection (&loader_mutex);
}

void
mono_loader_unlock (void)
{
	LeaveCriticalSection (&loader_mutex);
}

/**
 * mono_method_signature:
 *
 * Return the signature of the method M. On failure, returns NULL.
 */
MonoMethodSignature*
mono_method_signature (MonoMethod *m)
{
	int idx;
	int size;
	MonoImage* img;
	const char *sig;
	gboolean can_cache_signature;
	MonoGenericContainer *container;
	MonoMethodSignature *signature = NULL;
	int *pattrs;

	/* We need memory barriers below because of the double-checked locking pattern */ 

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
		signature = inflate_generic_signature (imethod->declaring->klass->image, signature, mono_method_get_context (m));
		mono_memory_barrier ();
		m->signature = signature;
		mono_loader_unlock ();
		return m->signature;
	}

	g_assert (mono_metadata_token_table (m->token) == MONO_TABLE_METHOD);
	idx = mono_metadata_token_index (m->token);
	img = m->klass->image;

	sig = mono_metadata_blob_heap (img, mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_SIGNATURE));

	g_assert (!m->klass->generic_class);
	container = mono_method_get_generic_container (m);
	if (!container)
		container = m->klass->generic_container;

	/* Generic signatures depend on the container so they cannot be cached */
	/* icall/pinvoke signatures cannot be cached cause we modify them below */
	can_cache_signature = !(m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && !(m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) && !container;

	/* If the method has parameter attributes, that can modify the signature */
	pattrs = mono_metadata_get_param_attrs (img, idx);
	if (pattrs) {
		can_cache_signature = FALSE;
		g_free (pattrs);
	}

	if (can_cache_signature)
		signature = g_hash_table_lookup (img->method_signatures, sig);

	if (!signature) {
		const char *sig_body;

		size = mono_metadata_decode_blob_size (sig, &sig_body);

		signature = mono_metadata_parse_method_signature_full (img, container, idx, sig_body, NULL);
		if (!signature) {
			mono_loader_unlock ();
			return NULL;
		}

		if (can_cache_signature)
			g_hash_table_insert (img->method_signatures, (gpointer)sig, signature);
	}

	/* Verify metadata consistency */
	if (signature->generic_param_count) {
		if (!container || !container->is_method)
			g_error ("Signature claims method has generic parameters, but generic_params table says it doesn't");
		if (container->type_argc != signature->generic_param_count)
			g_error ("Inconsistent generic parameter count.  Signature says %d, generic_params table says %d",
				 signature->generic_param_count, container->type_argc);
	} else if (container && container->is_method && container->type_argc)
		g_error ("generic_params table claims method has generic parameters, but signature says it doesn't");

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
			g_warning ("unsupported calling convention : 0x%04x", piinfo->piflags);
			g_assert_not_reached ();
		}
		signature->call_convention = conv;
	}

	mono_memory_barrier ();
	m->signature = signature;

	mono_loader_unlock ();
	return m->signature;
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
	MonoMethodNormal* mn = (MonoMethodNormal*) method;

	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return NULL;

#ifdef G_LIKELY
	if (G_LIKELY (mn->header))
#else
	if (mn->header)
#endif
		return mn->header;

	mono_loader_lock ();

	if (mn->header) {
		mono_loader_unlock ();
		return mn->header;
	}

	if (method->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;
		MonoMethodHeader *header;
		/* the lock is recursive */
		header = mono_method_get_header (imethod->declaring);
		mn->header = inflate_generic_header (header, mono_method_get_context (method));
		mono_loader_unlock ();
		return mn->header;
	}

	g_assert (mono_metadata_token_table (method->token) == MONO_TABLE_METHOD);
	idx = mono_metadata_token_index (method->token);
	img = method->klass->image;
	rva = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_RVA);
	loc = mono_image_rva_map (img, rva);

	g_assert (loc);

	mn->header = mono_metadata_parse_mh_full (img, mono_method_get_generic_container (method), loc);

	mono_loader_unlock ();
	return mn->header;
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

	if (method->token)
		return mono_metadata_token_index (method->token);

	mono_class_setup_methods (klass);
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

