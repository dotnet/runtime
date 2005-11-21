/*
 * loader.c: Image Loader 
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc.
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
#include <gmodule.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include <mono/utils/mono-logger.h>

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

/*
 * Handling of type load errors should be done as follows:
 *
 *   If something could not be loaded, the loader should call one of the
 * mono_loader_set_error_XXX functions ()
 * with the appropriate arguments, then return NULL to report the failure. The error 
 * should be propagated until it reaches code which can throw managed exceptions. At that
 * point, an exception should be thrown based on the information returned by
 * mono_loader_get_error (). Then the error should be cleared by calling 
 * mono_loader_clear_error ().
 */

static void
set_loader_error (MonoLoaderError *error)
{
	TlsSetValue (loader_error_thread_id, error);
}	

/*
 * mono_loader_set_error_type_load:
 *
 *   Set the loader error for this thread. CLASS_NAME and ASSEMBLY_NAME should be
 * dynamically allocated strings whose ownership is passed to this function.
 */
void
mono_loader_set_error_type_load (char *class_name, char *assembly_name)
{
	MonoLoaderError *error;

	if (mono_loader_get_last_error ()) {
		g_free (class_name);
		g_free (assembly_name);
		return;
	}

	error = g_new0 (MonoLoaderError, 1);
	error->kind = MONO_LOADER_ERROR_TYPE;
	error->class_name = class_name;
	error->assembly_name = assembly_name;

	/* 
	 * This is not strictly needed, but some (most) of the loader code still
	 * can't deal with load errors, and this message is more helpful than an
	 * assert.
	 */
	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_TYPE, "The class %s could not be loaded, used in %s", class_name, assembly_name);
	
	set_loader_error (error);
}

/*
 * mono_loader_set_error_method_load:
 *
 *   Set the loader error for this thread. MEMBER_NAME should point to a string
 * inside metadata.
 */
void
mono_loader_set_error_method_load (MonoClass *klass, const char *member_name)
{
	MonoLoaderError *error;

	/* FIXME: Store the signature as well */
	if (mono_loader_get_last_error ())
		return;

	error = g_new0 (MonoLoaderError, 1);
	error->kind = MONO_LOADER_ERROR_METHOD;
	error->klass = klass;
	error->member_name = member_name;

	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_TYPE, "Missing member %s in type %s, assembly %s", member_name, mono_class_get_name (klass), klass->image->name);
	
	set_loader_error (error);
}

/*
 * mono_loader_set_error_field_load:
 *
 *   Set the loader error for this thread. MEMBER_NAME should point to a string
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
	error->kind = MONO_LOADER_ERROR_FIELD;
	error->klass = klass;
	error->member_name = member_name;
	
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

void
mono_loader_clear_error (void)
{
	MonoLoaderError *ex = (MonoLoaderError*)TlsGetValue (loader_error_thread_id);

	g_assert (ex);	

	g_free (ex->class_name);
	g_free (ex->assembly_name);
	g_free (ex);

	TlsSetValue (loader_error_thread_id, NULL);
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

	if (image->dynamic) {
		MonoClassField *result = mono_lookup_dynamic_token (image, token);
		*retklass = result->parent;
		return result;
	}

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;

	fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);
	
	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	/* we may want to check the signature here... */

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_REF | nindex);
			g_warning ("Missing field %s in class %s (typeref index %d)", fname, name, nindex);
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
		MonoClassField *result = mono_lookup_dynamic_token (image, token);
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
		mono_class_init (k);
		if (!k)
			return NULL;
		if (retklass)
			*retklass = k;
		field = mono_class_get_field (k, token);
	}

	mono_loader_lock ();
	if (field && !field->parent->generic_class)
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
find_method_in_class (MonoClass *klass, const char *name, const char *qname,
		      const char *fqname, MonoMethodSignature *sig, MonoGenericContainer *container)
{
	int i;

	mono_class_setup_methods (klass);
	for (i = 0; i < klass->method.count; ++i) {
		MonoMethod *m = klass->methods [i];

		if (!((fqname && !strcmp (m->name, fqname)) ||
		      (qname && !strcmp (m->name, qname)) || !strcmp (m->name, name)))
			continue;

		if (sig->call_convention == MONO_CALL_VARARG) {
			if (mono_metadata_signature_vararg_match (sig, mono_method_signature (m)))
				return m;
		} else {
			MonoMethodSignature *msig = mono_method_signature_full (m, container);
			if (mono_metadata_signature_equal (sig, msig))
				return m;
		}
	}

	return NULL;
}

static MonoMethod *
find_method (MonoClass *klass, MonoClass *ic, const char* name, MonoMethodSignature *sig,
	     MonoGenericContainer *container)
{
	int i;
	char *qname, *fqname, *class_name;
	gboolean is_interface;
	MonoMethod *result = NULL;

	is_interface = MONO_CLASS_IS_INTERFACE (klass);

	if (ic) {
		class_name = mono_type_get_name_full (&ic->byval_arg, MONO_TYPE_NAME_FORMAT_IL);

		qname = g_strconcat (class_name, ".", name, NULL); 
		if (ic->name_space && ic->name_space [0])
			fqname = g_strconcat (ic->name_space, ".", class_name, ".", name, NULL);
		else
			fqname = NULL;
	} else
		class_name = qname = fqname = NULL;

	while (klass) {
		result = find_method_in_class (klass, name, qname, fqname, sig, container);
		if (result)
			goto out;

		if (name [0] == '.' && (!strcmp (name, ".ctor") || !strcmp (name, ".cctor")))
			break;

		for (i = 0; i < klass->interface_count; i++) {
			MonoClass *ic = klass->interfaces [i];

			result = find_method_in_class (ic, name, qname, fqname, sig, container);
			if (result)
				goto out;
		}

		klass = klass->parent;
	}

	if (is_interface)
		result = find_method_in_class (
			mono_defaults.object_class, name, qname, fqname, sig, container);

 out:
	g_free (class_name);
	g_free (fqname);
	g_free (qname);
	return result;
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
		sig = mono_metadata_parse_method_signature_full (
			image, context ? context->container : NULL, 0, ptr, NULL);

		mono_loader_lock ();
		prev_sig = g_hash_table_lookup (image->memberref_signatures, GUINT_TO_POINTER (token));
		if (prev_sig) {
			/* Somebody got in before us */
			/* FIXME: Free sig */
			sig = prev_sig;
		}
		else
			g_hash_table_insert (image->memberref_signatures, GUINT_TO_POINTER (token), sig);
		mono_loader_unlock ();
	}

	sig = mono_class_inflate_generic_signature (image, sig, context);

	return sig;
}

MonoMethodSignature*
mono_method_get_signature (MonoMethod *method, MonoImage *image, guint32 token)
{
	return mono_method_get_signature_full (method, image, token, NULL);
}

static MonoMethod *
method_from_memberref (MonoImage *image, guint32 idx, MonoGenericContext *context,
		       MonoGenericContext *typespec_context)
{
	MonoClass *klass = NULL;
	MonoMethod *method = NULL;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class;
	const char *mname;
	MonoMethodSignature *sig;
	MonoGenericContainer *container;
	const char *ptr;

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, 3);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
	/*g_print ("methodref: 0x%x 0x%x %s\n", class, nindex,
		mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]));*/

	mname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_REF | nindex);
			g_warning ("Missing method %s in assembly %s, type %s", mname, image->name, name);
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
			g_free (name);
			return NULL;
		}
		break;
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | nindex);
		if (!klass) {
			char *name = mono_class_name_from_token (image, MONO_TOKEN_TYPE_DEF | nindex);
			g_warning ("Missing method %s in assembly %s, type %s", mname, image->name, name);
			g_free (name);
			return NULL;
		}
		break;
	case MONO_MEMBERREF_PARENT_METHODDEF:
		return mono_get_method (image, MONO_TOKEN_METHOD_DEF | nindex, NULL);
	default:
		g_error ("Memberref parent unknown: class: %d, index %d", class, nindex);
		g_assert_not_reached ();
	}
	g_assert (klass);
	mono_class_init (klass);

	/*
	 * Generics: Correctly set the context before parsing the method.
	 *
	 * For MONO_MEMBERREF_PARENT_TYPEDEF or MONO_MEMBERREF_PARENT_TYPEREF, our parent
	 * is "context-free"; ie. `klass' will always be the same generic instantation.
	 *
	 * For MONO_MEMBERREF_PARENT_TYPESPEC, we have to parse the typespec in the
	 * `typespec_context' and thus `klass' may have different generic instantiations.
	 *
	 * After parsing the `klass', we have to distinguish two cases:
	 * Either this class introduces new type parameters (or a new generic instantiation)
	 * or it does not.  If it does, we parse the signature in this new context; otherwise
	 * we parse it in the current context.
	 *
	 */
	if (klass->generic_class)
		context = klass->generic_class->context;
	else if (klass->generic_container)
		context = (MonoGenericContext *) klass->generic_container;
	container = context ? context->container : NULL;

	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	sig = mono_metadata_parse_method_signature_full (image, container, 0, ptr, NULL);
	sig = mono_class_inflate_generic_signature (image, sig, context);

	switch (class) {
	case MONO_MEMBERREF_PARENT_TYPEREF:
		method = find_method (klass, NULL, mname, sig, container);
		if (!method)
			mono_loader_set_error_method_load (klass, mname);
		mono_metadata_free_method_signature (sig);
		break;
	case MONO_MEMBERREF_PARENT_TYPESPEC: {
		MonoType *type;
		MonoMethod *result;

		type = &klass->byval_arg;

		if (type->type != MONO_TYPE_ARRAY && type->type != MONO_TYPE_SZARRAY) {
			method = find_method (klass, NULL, mname, sig, container);
			if (!method)
				g_warning ("Missing method %s in assembly %s, type %s", mname, image->name, mono_class_get_name (klass));
			else if (klass->generic_class && (klass != method->klass))
				method = mono_class_inflate_generic_method (
					method, klass->generic_class->context);
			mono_metadata_free_method_signature (sig);
			break;
		}

		result = (MonoMethod *)g_new0 (MonoMethodPInvoke, 1);
		result->klass = klass;
		result->iflags = METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;
		result->signature = sig;
		result->name = mname;

		if (!strcmp (mname, ".ctor")) {
			/* we special-case this in the runtime. */
			return result;
		}
		
		if (!strcmp (mname, "Set")) {
			g_assert (sig->hasthis);
			g_assert (type->data.array->rank + 1 == sig->param_count);
			result->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
			return result;
		}

		if (!strcmp (mname, "Get")) {
			g_assert (sig->hasthis);
			g_assert (type->data.array->rank == sig->param_count);
			result->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
			return result;
		}

		if (!strcmp (mname, "Address")) {
			g_assert (sig->hasthis);
			g_assert (type->data.array->rank == sig->param_count);
			result->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
			return result;
		}

		g_assert_not_reached ();
		break;
	}
	case MONO_MEMBERREF_PARENT_TYPEDEF:
		method = find_method (klass, NULL, mname, sig, container);
		if (!method)
			g_warning ("Missing method %s in assembly %s, type %s", mname, image->name, mono_class_get_name (klass));
		mono_metadata_free_method_signature (sig);
		break;
	default:
		g_error ("Memberref parent unknown: class: %d, index %d", class, nindex);
		g_assert_not_reached ();
	}

	return method;
}

static MonoMethod *
method_from_methodspec (MonoImage *image, MonoGenericContext *context, guint32 idx)
{
	MonoMethod *method, *inflated;
	MonoTableInfo *tables = image->tables;
	MonoGenericContext *new_context = NULL;
	MonoGenericMethod *gmethod;
	MonoGenericContainer *container = NULL;
	const char *ptr;
	guint32 cols [MONO_METHODSPEC_SIZE];
	guint32 token, nindex, param_count;
	int i;

	mono_metadata_decode_row (&tables [MONO_TABLE_METHODSPEC], idx - 1, cols, MONO_METHODSPEC_SIZE);
	token = cols [MONO_METHODSPEC_METHOD];
	nindex = token >> MONO_METHODDEFORREF_BITS;

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODSPEC_SIGNATURE]);
	
	mono_metadata_decode_value (ptr, &ptr);
	ptr++;
	param_count = mono_metadata_decode_value (ptr, &ptr);
	g_assert (param_count);

	/*
	 * Check whether we're in a generic type or method.
	 */
	container = context ? context->container : NULL;
	if (container && (container->is_method || container->is_signature)) {
		/*
		 * If we're in a generic method, use the containing class'es context.
		 */
		container = container->parent;
	}

	/*
	 * Be careful with the two contexts here:
	 *
	 * ----------------------------------------
	 * class Foo<S> {
	 *   static void Hello<T> (S s, T t) { }
	 *
	 *   static void Test<U> (U u) {
	 *     Foo<U>.Hello<string> (u, "World");
	 *   }
	 * }
	 * ----------------------------------------
	 *
	 * Let's assume we're currently JITing Foo<int>.Test<long>
	 * (ie. `S' is instantiated as `int' and `U' is instantiated as `long').
	 *
	 * The call to Hello() is encoded with a MethodSpec with a TypeSpec as parent
	 * (MONO_MEMBERREF_PARENT_TYPESPEC).
	 *
	 * The TypeSpec is encoded as `Foo<!!0>', so we need to parse it in the current
	 * context (S=int, U=long) to get the correct `Foo<long>'.
	 * 
	 * After that, we parse the memberref signature in the new context
	 * (S=int, T=uninstantiated) and get the open generic method `Foo<long>.Hello<T>'.
	 *
	 */
	if ((token & MONO_METHODDEFORREF_MASK) == MONO_METHODDEFORREF_METHODDEF)
		method = mono_get_method_full (image, MONO_TOKEN_METHOD_DEF | nindex, NULL, context);
	else
		method = method_from_memberref (image, nindex, container, context);

	method = mono_get_inflated_method (method);

	container = method->generic_container;
	g_assert (container);

	gmethod = g_new0 (MonoGenericMethod, 1);
	gmethod->generic_class = method->klass->generic_class;
	gmethod->container = container;

	new_context = g_new0 (MonoGenericContext, 1);
	new_context->container = container;
	new_context->gmethod = gmethod;
	if (container->parent)
		new_context->gclass = container->parent->context.gclass;

	/*
	 * When parsing the methodspec signature, we're in the old context again:
	 *
	 * ----------------------------------------
	 * class Foo {
	 *   static void Hello<T> (T t) { }
	 *
	 *   static void Test<U> (U u) {
	 *     Foo.Hello<U> (u);
	 *   }
	 * }
	 * ----------------------------------------
	 *
	 * Let's assume we're currently JITing "Foo.Test<float>".
	 *
	 * In this case, we already parsed the memberref as "Foo.Hello<T>" and the methodspec
	 * signature is "<!!0>".  This means that we must instantiate the method type parameter
	 * `T' from the new method with the method type parameter `U' from the current context;
	 * ie. instantiate the method as `Foo.Hello<float>.
	 */

	gmethod->inst = mono_metadata_parse_generic_inst (image, context, param_count, ptr, &ptr);

	if (context)
		gmethod->inst = mono_metadata_inflate_generic_inst (gmethod->inst, context);

	if (!container->method_hash)
		container->method_hash = g_hash_table_new (
			(GHashFunc)mono_metadata_generic_method_hash, (GEqualFunc)mono_metadata_generic_method_equal);

	inflated = g_hash_table_lookup (container->method_hash, gmethod);
	if (inflated) {
		g_free (gmethod);
		g_free (new_context);
		return inflated;
	}

	context = new_context;

	mono_stats.generics_metadata_size += sizeof (MonoGenericMethod) +
		sizeof (MonoGenericContext) + param_count * sizeof (MonoType);

	inflated = mono_class_inflate_generic_method (method, new_context);
	g_hash_table_insert (container->method_hash, gmethod, inflated);

	new_context->gclass = inflated->klass->generic_class;
	return inflated;
}

typedef struct MonoDllMap MonoDllMap;

struct MonoDllMap {
	char *name;
	char *target;
	char *dll;
	MonoDllMap *next;
};

static GHashTable *global_dll_map;

static int 
mono_dllmap_lookup_hash (GHashTable *dll_map, const char *dll, const char* func, const char **rdll, const char **rfunc) {
	MonoDllMap *map, *tmp;

	*rdll = dll;

	if (!dll_map)
		return 0;

	mono_loader_lock ();

	map = g_hash_table_lookup (dll_map, dll);
	if (!map) {
		mono_loader_unlock ();
		return 0;
	}
	*rdll = map->target? map->target: dll;
		
	for (tmp = map->next; tmp; tmp = tmp->next) {
		if (strcmp (func, tmp->name) == 0) {
			*rfunc = tmp->name;
			if (tmp->dll)
				*rdll = tmp->dll;
			mono_loader_unlock ();
			return 1;
		}
	}
	*rfunc = func;
	mono_loader_unlock ();
	return 1;
}

static int 
mono_dllmap_lookup (MonoImage *assembly, const char *dll, const char* func, const char **rdll, const char **rfunc)
{
	int res;
	if (assembly && assembly->dll_map) {
		res = mono_dllmap_lookup_hash (assembly->dll_map, dll, func, rdll, rfunc);
		if (res)
			return res;
	}
	return mono_dllmap_lookup_hash (global_dll_map, dll, func, rdll, rfunc);
}

void
mono_dllmap_insert (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc) {
	MonoDllMap *map, *entry;
	GHashTable *dll_map = NULL;

	mono_loader_lock ();

	if (!assembly) {
		if (!global_dll_map)
			global_dll_map = g_hash_table_new (g_str_hash, g_str_equal);
		dll_map = global_dll_map;
	} else {
		if (!assembly->dll_map)
			assembly->dll_map = g_hash_table_new (g_str_hash, g_str_equal);
		dll_map = assembly->dll_map;
	}

	map = g_hash_table_lookup (dll_map, dll);
	if (!map) {
		map = g_new0 (MonoDllMap, 1);
		map->dll = g_strdup (dll);
		if (tdll)
			map->target = g_strdup (tdll);
		g_hash_table_insert (dll_map, map->dll, map);
	}
	if (func) {
		entry = g_new0 (MonoDllMap, 1);
		entry->name = g_strdup (func);
		if (tfunc)
			entry->target = g_strdup (tfunc);
		if (tdll && map->target && strcmp (map->target, tdll))
			entry->dll = g_strdup (tdll);
		entry->next = map->next;
		map->next = entry;
	}

	mono_loader_unlock ();
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
	char *full_name, *file_name;
	int i;
	GModule *gmodule = NULL;

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
		gmodule = g_module_open (NULL, G_MODULE_BIND_LAZY);
		
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

		if (!gmodule) {
			full_name = g_module_build_path (NULL, file_name);
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loading location: '%s'.", full_name);
			gmodule = g_module_open (full_name, G_MODULE_BIND_LAZY);
			if (!gmodule) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library: '%s'.",
						g_module_error ());
			}
			g_free (full_name);
		}

		if (!gmodule) {
			full_name = g_module_build_path (".", file_name);
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loading library: '%s'.", full_name);
			gmodule = g_module_open (full_name, G_MODULE_BIND_LAZY);
			if (!gmodule) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s'.",
						g_module_error ());
			}
			g_free (full_name);
		}

		if (!gmodule) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
					"DllImport loading: '%s'.", file_name);
			gmodule=g_module_open (file_name, G_MODULE_BIND_LAZY);
			if (!gmodule) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
						"DllImport error loading library '%s'.",
						g_module_error ());
			}
		}

		g_free (file_name);

		if (gmodule)
			break;
	}

	if (!gmodule) {
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DLLIMPORT,
				"DllImport unable to load library '%s'.",
				g_module_error ());

		if (exc_class) {
			*exc_class = "DllNotFoundException";
			*exc_arg = new_scope;
		}
		return NULL;
	}

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT,
				"Searching for '%s'.", import);

	if (piinfo->piflags & PINVOKE_ATTRIBUTE_NO_MANGLE) {
		g_module_symbol (gmodule, import, &piinfo->addr); 
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

					g_module_symbol (gmodule, mangled_name2, &piinfo->addr);

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
		if (exc_class) {
			*exc_class = "EntryPointNotFoundException";
			*exc_arg = import;
		}
		return NULL;
	}
	return piinfo->addr;
}

static MonoMethod *
mono_get_method_from_token (MonoImage *image, guint32 token, MonoClass *klass,
			    MonoGenericContext *context)
{
	MonoMethod *result;
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	MonoGenericContainer *generic_container = NULL, *container = NULL;
	const char *sig = NULL;
	int size, i;
	guint32 cols [MONO_TYPEDEF_SIZE];

	if (image->dynamic)
		return mono_lookup_dynamic_token (image, token);

	if (table != MONO_TABLE_METHOD) {
		if (table == MONO_TABLE_METHODSPEC)
			return method_from_methodspec (image, context, idx);
		if (table != MONO_TABLE_MEMBERREF)
			g_print("got wrong token: 0x%08x\n", token);
		g_assert (table == MONO_TABLE_MEMBERREF);
		result = method_from_memberref (image, idx, context, context);

		return result;
	}

	mono_metadata_decode_row (&tables [table], idx - 1, cols, 6);

	if ((cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		result = (MonoMethod *)g_new0 (MonoMethodPInvoke, 1);
	else 
		result = (MonoMethod *)g_new0 (MonoMethodNormal, 1);
	
	result->slot = -1;
	result->klass = klass;
	result->flags = cols [2];
	result->iflags = cols [1];
	result->token = token;
	result->name = mono_metadata_string_heap (image, cols [3]);

	if (klass)
		container = klass->generic_container;

	generic_container = mono_metadata_load_generic_params (image, token, container);
	if (generic_container) {
		mono_metadata_load_generic_param_constraints (image, token, generic_container);
		container = generic_container;
	}

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);
	
	/* there are generic params, or a container. FIXME: be lazy here for generics*/
	if (* sig & 0x10 || container) {
		result->signature = mono_metadata_parse_method_signature_full (
			image, container, idx, sig, NULL);

		if (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
			result->signature->pinvoke = 1;
	}

	if (!result->klass) {
		guint32 type = mono_metadata_typedef_from_method (image, token);
		result->klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | type);
	}

	if (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (result->klass == mono_defaults.string_class && !strcmp (result->name, ".ctor"))
			result->string_ctor = 1;
	} else if ((cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) && (!(cols [1] & METHOD_IMPL_ATTRIBUTE_NATIVE))) {
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)result;
		MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
		
		piinfo->implmap_idx = mono_metadata_implmap_from_method (image, idx - 1);
		piinfo->piflags = mono_metadata_decode_row_col (im, piinfo->implmap_idx - 1, MONO_IMPLMAP_FLAGS);
	}

	if (result->signature && result->signature->generic_param_count) {
		MonoMethodSignature *sig = result->signature;

		for (i = 0; i < sig->generic_param_count; i++) {
			generic_container->type_params [i].method = result;

			mono_class_from_generic_parameter (
				&generic_container->type_params [i], image, TRUE);
		}

		if (sig->ret->type == MONO_TYPE_MVAR) {
			int num = sig->ret->data.generic_param->num;
			sig->ret->data.generic_param = &generic_container->type_params [num];
		}

		for (i = 0; i < sig->param_count; i++) {
			MonoType *t = sig->params [i];
			if (t->type == MONO_TYPE_MVAR) {
				int num = t->data.generic_param->num;
				sig->params [i]->data.generic_param = &generic_container->type_params [num];
			}
		}
	}
		
	/* FIXME: lazyness for generics too, but how? */
	if (!result->klass->dummy && !(result->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
	    !(cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
	    !(result->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) && container) {
		gpointer loc = mono_image_rva_map (image, cols [0]);
		g_assert (loc);
		((MonoMethodNormal *) result)->header = mono_metadata_parse_mh_full (
			image, (MonoGenericContext *) container, loc);
	}
		
	result->generic_container = generic_container;

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

	/* We do everything inside the lock to prevent creation races */

	mono_loader_lock ();

	if ((result = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (token)))) {
		mono_loader_unlock ();
		return result;
	}

	result = mono_get_method_from_token (image, token, klass, context);

	//printf ("GET: %s\n", mono_method_full_name (result, TRUE));

	if (!(result && result->is_inflated))
		g_hash_table_insert (image->method_cache, GINT_TO_POINTER (token), result);

	mono_loader_unlock ();

	return result;
}

/**
 * mono_get_method_constrained:
 *
 * This is used when JITing the `constrained.' opcode.
 */
MonoMethod *
mono_get_method_constrained (MonoImage *image, guint32 token, MonoClass *constrained_class,
			     MonoGenericContext *context)
{
	MonoMethod *method, *result;
	MonoClass *ic = NULL;
	MonoGenericClass *gclass = NULL;

	mono_loader_lock ();

	method = mono_get_method_from_token (image, token, NULL, context);
	if (!method) {
		mono_loader_unlock ();
		return NULL;
	}

	mono_class_init (constrained_class);
	method = mono_get_inflated_method (method);

	if ((constrained_class != method->klass) && (method->klass->interface_id != 0))
		ic = method->klass;

	if (constrained_class->generic_class)
		gclass = constrained_class->generic_class;

	result = find_method (
		constrained_class, ic, method->name, mono_method_signature (method),
		context ? context->container : NULL);
	if (!result)
		g_warning ("Missing method %s in assembly %s token %x", method->name,
			   image->name, token);

	if (gclass)
		result = mono_class_inflate_generic_method (result, gclass->context);

	mono_loader_unlock ();
	return result;
}

void
mono_free_method  (MonoMethod *method)
{
	if (method->signature) {
		/* 
		 * FIXME: This causes crashes because the types inside signatures and
		 * locals are shared.
		 */
		/* mono_metadata_free_method_signature (method->signature); */
		g_free (method->signature);
	}

	if (method->dynamic) {
		MonoMethodWrapper *mw = (MonoMethodWrapper*)method;

		g_free ((char*)method->name);
		if (mw->method.header)
			g_free ((char*)mw->method.header->code);
		g_free (mw->method_data);
	}

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && ((MonoMethodNormal *)method)->header) {
		/* FIXME: Ditto */
		/* mono_metadata_free_mh (((MonoMethodNormal *)method)->header); */
		g_free (((MonoMethodNormal*)method)->header);
	}

	g_free (method);
}

void
mono_method_get_param_names (MonoMethod *method, const char **names)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;
	guint32 idx;

	if (!mono_method_signature (method)->param_count)
		return;
	for (i = 0; i < mono_method_signature (method)->param_count; ++i)
		names [i] = "";

	if (klass->generic_class) /* copy the names later */
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
		guint param_index = mono_metadata_decode_row_col (methodt, idx - 1, MONO_METHOD_PARAMLIST);

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

	if (klass->generic_class)
		g_assert_not_reached ();

	mono_class_init (klass);

	if (klass->image->dynamic) {
		g_assert_not_reached ();
	}

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	idx = mono_method_get_index (method);
	if (idx > 0) {
		guint param_index = mono_metadata_decode_row_col (methodt, idx - 1, MONO_METHOD_PARAMLIST);

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

MonoMethodSignature* 
mono_method_signature (MonoMethod *m)
{
	m = mono_get_inflated_method (m);
	return mono_method_signature_full (m, NULL);
}

MonoMethodSignature* 
mono_method_signature_full (MonoMethod *m, MonoGenericContainer *container)
{
	int idx;
	int size;
	MonoImage* img;
	const char *sig;
	
	if (m->signature)
		return m->signature;
		
	mono_loader_lock ();
	
	if (m->signature) {
		mono_loader_unlock ();
		return m->signature;
	}
	
	g_assert (mono_metadata_token_table (m->token) == MONO_TABLE_METHOD);
	idx = mono_metadata_token_index (m->token);
	img = m->klass->image;
	
	sig = mono_metadata_blob_heap (img, mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_SIGNATURE));
	size = mono_metadata_decode_blob_size (sig, &sig);
	
	m->signature = mono_metadata_parse_method_signature_full (img, container, idx, sig, NULL);
	
	if (m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		m->signature->pinvoke = 1;
	else if ((m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) && (!(m->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE))) {
		MonoCallConvention conv = 0;
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)m;
		m->signature->pinvoke = 1;
		
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
		m->signature->call_convention = conv;
	}
	
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
	
	if (method->klass->dummy || (method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
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
	
	g_assert (mono_metadata_token_table (method->token) == MONO_TABLE_METHOD);
	idx = mono_metadata_token_index (method->token);
	img = method->klass->image;
	rva = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_RVA);
	loc = mono_image_rva_map (img, rva);
	
	g_assert (loc);
	
	mn->header = mono_metadata_parse_mh_full (img, (MonoGenericContext *) method->generic_container, loc);
	
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
		if (method == klass->methods [i])
			return klass->method.first + 1 + i;
	}
	return 0;
}

