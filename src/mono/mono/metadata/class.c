/*
 * class.c: Class management for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 *
 * Possible Optimizations:
 *     in mono_class_create, do not allocate the class right away,
 *     but wait until you know the size of the FieldMap, so that
 *     the class embeds directly the FieldMap after the vtable.
 *
 * 
 */
#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <signal.h>
#include <mono/metadata/image.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/security-manager.h>
#include <mono/os/gc_wrapper.h>

MonoStats mono_stats;

gboolean mono_print_vtable = FALSE;

static MonoClass * mono_class_create_from_typedef (MonoImage *image, guint32 type_token);
static void mono_class_create_generic (MonoInflatedGenericClass *gclass);
static gboolean mono_class_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res);

void (*mono_debugger_start_class_init_func) (MonoClass *klass) = NULL;
void (*mono_debugger_class_init_func) (MonoClass *klass) = NULL;

MonoClass *
mono_class_from_typeref (MonoImage *image, guint32 type_token)
{
	guint32 cols [MONO_TYPEREF_SIZE];
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
	guint32 idx;
	const char *name, *nspace;
	MonoClass *res;
	MonoAssembly **references;
	
	mono_metadata_decode_row (t, (type_token&0xffffff)-1, cols, MONO_TYPEREF_SIZE);

	name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);
	nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);

	idx = cols [MONO_TYPEREF_SCOPE] >> MONO_RESOLTION_SCOPE_BITS;
	switch (cols [MONO_TYPEREF_SCOPE] & MONO_RESOLTION_SCOPE_MASK) {
	case MONO_RESOLTION_SCOPE_MODULE:
		if (!idx)
			g_error ("null ResolutionScope not yet handled");
		/* a typedef in disguise */
		return mono_class_from_name (image, nspace, name);
	case MONO_RESOLTION_SCOPE_MODULEREF:
		return mono_class_from_name (image->modules [idx - 1], nspace, name);
	case MONO_RESOLTION_SCOPE_TYPEREF: {
		MonoClass *enclosing = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | idx);
		GList *tmp;

		if (enclosing->inited) {
			/* Micro-optimization: don't scan the metadata tables if enclosing is already inited */
			for (tmp = enclosing->nested_classes; tmp; tmp = tmp->next) {
				res = tmp->data;
				if (strcmp (res->name, name) == 0)
					return res;
			}
		} else {
			/* Don't call mono_class_init as we might've been called by it recursively */
			int i = mono_metadata_nesting_typedef (enclosing->image, enclosing->type_token, 1);
			while (i) {
				guint32 class_nested = mono_metadata_decode_row_col (&enclosing->image->tables [MONO_TABLE_NESTEDCLASS], i - 1, MONO_NESTED_CLASS_NESTED);
				guint32 string_offset = mono_metadata_decode_row_col (&enclosing->image->tables [MONO_TABLE_TYPEDEF], class_nested - 1, MONO_TYPEDEF_NAME);
				const char *nname = mono_metadata_string_heap (enclosing->image, string_offset);

				if (strcmp (nname, name) == 0)
					return mono_class_create_from_typedef (enclosing->image, MONO_TOKEN_TYPE_DEF | class_nested);

				i = mono_metadata_nesting_typedef (enclosing->image, enclosing->type_token, i + 1);
			}
		}
		g_warning ("TypeRef ResolutionScope not yet handled (%d)", idx);
		return NULL;
	}
	case MONO_RESOLTION_SCOPE_ASSEMBLYREF:
		break;
	}

	references = image->references;
	if (!references [idx-1])
		mono_assembly_load_reference (image, idx - 1);
	if (references [idx - 1] == (gpointer)-1)
		return NULL;

	image = references [idx-1]->image;

	return mono_class_from_name (image, nspace, name);
}

static inline MonoType*
dup_type (MonoType* t, const MonoType *original)
{
	MonoType *r = g_new0 (MonoType, 1);
	*r = *t;
	r->attrs = original->attrs;
	r->byref = original->byref;
	if (t->type == MONO_TYPE_PTR)
		t->data.type = dup_type (t->data.type, original->data.type);
	else if (t->type == MONO_TYPE_ARRAY)
		t->data.array = mono_dup_array_type (t->data.array);
	else if (t->type == MONO_TYPE_FNPTR)
		t->data.method = mono_metadata_signature_deep_dup (t->data.method);
	mono_stats.generics_metadata_size += sizeof (MonoType);
	return r;
}

/* Copy everything mono_metadata_free_array free. */
MonoArrayType *
mono_dup_array_type (MonoArrayType *a)
{
	a = g_memdup (a, sizeof (MonoArrayType));
	if (a->sizes)
		a->sizes = g_memdup (a->sizes, a->numsizes * sizeof (int));
	if (a->lobounds)
		a->lobounds = g_memdup (a->lobounds, a->numlobounds * sizeof (int));
	return a;
}

/* Copy everything mono_metadata_free_method_signature free. */
MonoMethodSignature*
mono_metadata_signature_deep_dup (MonoMethodSignature *sig)
{
	int i;
	
	sig = mono_metadata_signature_dup (sig);
	
	sig->ret = dup_type (sig->ret, sig->ret);
	for (i = 0; i < sig->param_count; ++i)
		sig->params [i] = dup_type (sig->params [i], sig->params [i]);
	
	return sig;
}

static void
_mono_type_get_assembly_name (MonoClass *klass, GString *str)
{
	MonoAssembly *ta = klass->image->assembly;

	g_string_append_printf (
		str, ", %s, Version=%d.%d.%d.%d, Culture=%s, PublicKeyToken=%s",
		ta->aname.name,
		ta->aname.major, ta->aname.minor, ta->aname.build, ta->aname.revision,
		ta->aname.culture && *ta->aname.culture? ta->aname.culture: "neutral",
		ta->aname.public_key_token [0] ? (char *)ta->aname.public_key_token : "null");
}

static void
mono_type_get_name_recurse (MonoType *type, GString *str, gboolean is_recursed,
			    MonoTypeNameFormat format)
{
	MonoClass *klass;
	
	switch (type->type) {
	case MONO_TYPE_ARRAY: {
		int i, rank = type->data.array->rank;
		MonoTypeNameFormat nested_format;

		nested_format = format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED ?
			MONO_TYPE_NAME_FORMAT_FULL_NAME : format;

		mono_type_get_name_recurse (
			&type->data.array->eklass->byval_arg, str, FALSE, nested_format);
		g_string_append_c (str, '[');
		for (i = 1; i < rank; i++)
			g_string_append_c (str, ',');
		g_string_append_c (str, ']');
		if (format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)
			_mono_type_get_assembly_name (type->data.array->eklass, str);
		break;
	}
	case MONO_TYPE_SZARRAY: {
		MonoTypeNameFormat nested_format;

		nested_format = format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED ?
			MONO_TYPE_NAME_FORMAT_FULL_NAME : format;

		mono_type_get_name_recurse (
			&type->data.klass->byval_arg, str, FALSE, nested_format);
		g_string_append (str, "[]");
		if (format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)
			_mono_type_get_assembly_name (type->data.klass, str);
		break;
	}
	case MONO_TYPE_PTR: {
		MonoTypeNameFormat nested_format;

		nested_format = format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED ?
			MONO_TYPE_NAME_FORMAT_FULL_NAME : format;

		mono_type_get_name_recurse (
			type->data.type, str, FALSE, nested_format);
		g_string_append (str, "*");
		if (format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)
			_mono_type_get_assembly_name (type->data.klass, str);
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (type->data.generic_param->name);
		g_string_append (str, type->data.generic_param->name);
		break;
	default:
		klass = mono_class_from_mono_type (type);
		if (klass->nested_in) {
			mono_type_get_name_recurse (
				&klass->nested_in->byval_arg, str, TRUE, format);
			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append_c (str, '.');
			else
				g_string_append_c (str, '+');
		} else if (*klass->name_space) {
			g_string_append (str, klass->name_space);
			g_string_append_c (str, '.');
		}
		if (format == MONO_TYPE_NAME_FORMAT_IL) {
			char *s = strchr (klass->name, '`');
			int len = s ? s - klass->name : strlen (klass->name);

			g_string_append_len (str, klass->name, len);
		} else
			g_string_append (str, klass->name);
		if (is_recursed)
			break;
		if (klass->generic_class) {
			MonoGenericClass *gclass = klass->generic_class;
			MonoTypeNameFormat nested_format;
			int i;

			nested_format = format == MONO_TYPE_NAME_FORMAT_FULL_NAME ?
				MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED : format;

			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append_c (str, '<');
			else
				g_string_append_c (str, '[');
			for (i = 0; i < gclass->inst->type_argc; i++) {
				MonoType *t = gclass->inst->type_argv [i];

				if (i)
					g_string_append_c (str, ',');
				if ((nested_format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED) &&
				    (t->type != MONO_TYPE_VAR) && (type->type != MONO_TYPE_MVAR))
					g_string_append_c (str, '[');
				mono_type_get_name_recurse (
					gclass->inst->type_argv [i], str, FALSE, nested_format);
				if ((nested_format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED) &&
				    (t->type != MONO_TYPE_VAR) && (type->type != MONO_TYPE_MVAR))
					g_string_append_c (str, ']');
			}
			if (format == MONO_TYPE_NAME_FORMAT_IL)	
				g_string_append_c (str, '>');
			else
				g_string_append_c (str, ']');
		} else if (klass->generic_container &&
			   (format != MONO_TYPE_NAME_FORMAT_FULL_NAME) &&
			   (format != MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)) {
			int i;

			if (format == MONO_TYPE_NAME_FORMAT_IL)	
				g_string_append_c (str, '<');
			else
				g_string_append_c (str, '[');
			for (i = 0; i < klass->generic_container->type_argc; i++) {
				if (i)
					g_string_append_c (str, ',');
				g_string_append (str, klass->generic_container->type_params [i].name);
			}
			if (format == MONO_TYPE_NAME_FORMAT_IL)	
				g_string_append_c (str, '>');
			else
				g_string_append_c (str, ']');
		}
		if ((format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED) &&
		    (type->type != MONO_TYPE_VAR) && (type->type != MONO_TYPE_MVAR))
			_mono_type_get_assembly_name (klass, str);
		break;
	}
}

/**
 * mono_type_get_name:
 * @type: a type
 * @format: the format for the return string.
 *
 * 
 * Returns: the string representation in a number of formats:
 *
 * if format is MONO_TYPE_NAME_FORMAT_REFLECTION, the return string is
 * returned in the formatrequired by System.Reflection, this is the
 * inverse of mono_reflection_parse_type ().
 *
 * if format is MONO_TYPE_NAME_FORMAT_IL, it returns a syntax that can
 * be used by the IL assembler.
 *
 * if format is MONO_TYPE_NAME_FORMAT_FULL_NAME
 *
 * if format is MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
 */
char*
mono_type_get_name_full (MonoType *type, MonoTypeNameFormat format)
{
	GString* result;

	if (format == MONO_TYPE_NAME_FORMAT_FULL_NAME &&
	    ((type->type == MONO_TYPE_VAR) || (type->type == MONO_TYPE_MVAR) ||
	     ((type->type == MONO_TYPE_GENERICINST) && type->data.generic_class->inst->is_open)))
		return NULL;

	result = g_string_new ("");

	mono_type_get_name_recurse (type, result, FALSE, format);

	if (type->byref)
		g_string_append_c (result, '&');

	return g_string_free (result, FALSE);
}

/**
 * mono_type_get_full_name:
 * @class: a class
 *
 * Returns: the string representation for type as required by System.Reflection.
 * The inverse of mono_reflection_parse_type ().
 */
char *
mono_type_get_full_name (MonoClass *class)
{
	return mono_type_get_name_full (mono_class_get_type (class), MONO_TYPE_NAME_FORMAT_REFLECTION);
}

char*
mono_type_get_name (MonoType *type)
{
	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_IL);
}

MonoType*
mono_type_get_underlying_type (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype)
			return type->data.klass->enum_basetype;
		break;
	case MONO_TYPE_GENERICINST:
		return mono_type_get_underlying_type (&type->data.generic_class->container_class->byval_arg);
	default:
		break;
	}

	return type;
}

gboolean
mono_class_is_open_constructed_type (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		return TRUE;
	case MONO_TYPE_SZARRAY:
		return mono_class_is_open_constructed_type (&t->data.klass->byval_arg);
	case MONO_TYPE_ARRAY:
		return mono_class_is_open_constructed_type (&t->data.array->eklass->byval_arg);
	case MONO_TYPE_PTR:
		return mono_class_is_open_constructed_type (t->data.type);
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *gclass = t->data.generic_class;
		int i;

		if (mono_class_is_open_constructed_type (&gclass->container_class->byval_arg))
			return TRUE;
		for (i = 0; i < gclass->inst->type_argc; i++)
			if (mono_class_is_open_constructed_type (gclass->inst->type_argv [i]))
				return TRUE;
		return FALSE;
	}
	default:
		return FALSE;
	}
}

static MonoGenericClass *
inflate_generic_class (MonoGenericClass *ogclass, MonoGenericContext *context)
{
	MonoInflatedGenericClass *igclass;
	MonoGenericClass *ngclass, *cached;

	if (ogclass->is_dynamic) {
		MonoDynamicGenericClass *dgclass = g_new0 (MonoDynamicGenericClass, 1);
		igclass = &dgclass->generic_class;
		ngclass = &igclass->generic_class;
		ngclass->is_inflated = 1;
		ngclass->is_dynamic = 1;
	} else {
		igclass = g_new0 (MonoInflatedGenericClass, 1);
		ngclass = &igclass->generic_class;
		ngclass->is_inflated = 1;
	}

	*ngclass = *ogclass;

	ngclass->inst = mono_metadata_inflate_generic_inst (ogclass->inst, context);

	igclass->klass = NULL;

	ngclass->context = g_new0 (MonoGenericContext, 1);
	ngclass->context->container = ngclass->container_class->generic_container;
	ngclass->context->gclass = ngclass;

	mono_loader_lock ();
	cached = mono_metadata_lookup_generic_class (ngclass);
	mono_loader_unlock ();
	if (cached) {
		g_free (ngclass->context);
		g_free (ngclass);
		return cached;
	}

	return ngclass;
}

static MonoType*
inflate_generic_type (MonoType *type, MonoGenericContext *context)
{
	switch (type->type) {
	case MONO_TYPE_MVAR:
		if (context->gmethod && context->gmethod->inst->type_argv)
			return dup_type (
				context->gmethod->inst->type_argv [type->data.generic_param->num],
				type);
		else
			return NULL;
	case MONO_TYPE_VAR:
		if (context->gclass)
			return dup_type (
				context->gclass->inst->type_argv [type->data.generic_param->num],
				type);
		else
			return NULL;
	case MONO_TYPE_SZARRAY: {
		MonoClass *eclass = type->data.klass;
		MonoType *nt, *inflated = inflate_generic_type (&eclass->byval_arg, context);
		if (!inflated)
			return NULL;
		nt = dup_type (type, type);
		nt->data.klass = mono_class_from_mono_type (inflated);
		return nt;
	}
	case MONO_TYPE_ARRAY: {
		MonoClass *eclass = type->data.array->eklass;
		MonoType *nt, *inflated = inflate_generic_type (&eclass->byval_arg, context);
		if (!inflated)
			return NULL;
		nt = dup_type (type, type);
		nt->data.array = g_memdup (nt->data.array, sizeof (MonoArrayType));
		nt->data.array->eklass = mono_class_from_mono_type (inflated);
		return nt;
	}
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *ngclass = inflate_generic_class (type->data.generic_class, context);
		MonoType *nt = dup_type (type, type);
		nt->data.generic_class = ngclass;
		return nt;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = type->data.klass;
		MonoGenericClass *gclass;
		MonoType *nt;

		if (!klass->generic_container)
			return NULL;

		gclass = inflate_generic_class (klass->generic_container->context.gclass, context);

		nt = dup_type (type, type);
		nt->type = MONO_TYPE_GENERICINST;
		nt->data.generic_class = gclass;
		return nt;
	}
	default:
		return NULL;
	}
	return NULL;
}

MonoInflatedGenericClass*
mono_get_inflated_generic_class (MonoGenericClass *gclass)
{
	g_assert (gclass->is_inflated);
	mono_class_create_generic ((MonoInflatedGenericClass *) gclass);
	return (MonoInflatedGenericClass *) gclass;
}

MonoType*
mono_class_inflate_generic_type (MonoType *type, MonoGenericContext *context)
{
	MonoType *inflated = inflate_generic_type (type, context);

	if (!inflated)
		return dup_type (type, type);

	mono_stats.inflated_type_count++;
	return inflated;
}

MonoMethodSignature*
mono_class_inflate_generic_signature (MonoImage *image, MonoMethodSignature *sig, MonoGenericContext *context)
{
	MonoMethodSignature *res;
	gboolean is_open;
	int i;

	if (!context)
		return sig;

	res = mono_metadata_signature_alloc (image, sig->param_count);
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
	return res;
}

static MonoGenericContext *
inflate_generic_context (MonoGenericContext *context, MonoGenericContext *inflate_with)
{
	MonoGenericContext *res = g_new0 (MonoGenericContext, 1);

	res->container = context->container;

	if (context->gclass)
		res->gclass = inflate_generic_class (context->gclass, inflate_with);

	if (context->gmethod) {
		res->gmethod = g_new0 (MonoGenericMethod, 1);
		res->gmethod->generic_class = res->gclass;

		res->gmethod->container = context->gmethod->container;
		res->gmethod->inst = mono_metadata_inflate_generic_inst (context->gmethod->inst, inflate_with);
	} else
		res->gmethod = inflate_with->gmethod;

	if (res->gmethod) {
		res->gmethod->container->parent = res->container;
		res->container = res->gmethod->container;
	}

	return res;
}

/**
 * mono_class_inflate_generic_method:
 *
 * Instantiate method @method with the generic context @context.
 */
MonoMethod*
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericContext *context)
{
	MonoMethodInflated *result;
	MonoGenericContainer *container = context ? context->container : NULL;
	MonoMethodSignature *sig;

	/* The `method' has already been instantiated before -> we need to create a new context. */
	sig = mono_method_signature_full (method, container);
	if (method->is_inflated || sig->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;

		context = inflate_generic_context (imethod->context, context);
		method = imethod->declaring;
	}

	mono_stats.inflated_method_count++;

	/* Just create a copy, but don't actually inflate the method for performance reasons. */
	result = g_new0 (MonoMethodInflated, 1);
	if (sig->pinvoke)
		result->method.pinvoke = *(MonoMethodPInvoke*)method;
	else
		result->method.normal = *(MonoMethodNormal*)method;
	result->method.method.is_inflated = 1;
	result->context = context;
	result->declaring = method;

	if (result->method.method.klass->generic_class)
		result->method.method.klass = result->method.method.klass->generic_class->container_class;

	return (MonoMethod *) result;
}

/**
 * mono_get_inflated_method:
 *
 * For performance reasons, mono_class_inflate_generic_method() does not actually instantiate the
 * method, it just "prepares" it for that.  If you really need to fully instantiate the method
 * (including its signature and header), call this method.
 */
MonoMethod *
mono_get_inflated_method (MonoMethod *method)
{
	MonoMethodInflated *imethod, *res;
	MonoMethodHeader *mh;
	MonoType *dtype;
	MonoClass *rklass;

	if (!method->is_inflated)
		return method;

	imethod = (MonoMethodInflated *) method;
	if (imethod->inflated)
		return (MonoMethod *) imethod->inflated;

	mono_stats.inflated_method_count_2++;
	mono_stats.generics_metadata_size +=
		sizeof (MonoMethodInflated) - sizeof (MonoMethodNormal);

	res = g_new0 (MonoMethodInflated, 1);
	*res = *imethod;
	res->inflated = imethod->inflated = res;

	mh = mono_method_get_header (method);
	if (mh)
		res->method.normal.header = inflate_generic_header (mh, imethod->context);

	dtype = mono_class_inflate_generic_type (&method->klass->byval_arg, imethod->context);
	rklass = res->method.method.klass = mono_class_from_mono_type (dtype);

	res->method.method.signature = mono_class_inflate_generic_signature (
		method->klass->image, mono_method_signature (method), imethod->context);

	return (MonoMethod *) res;
}

/** 
 * mono_class_find_enum_basetype:
 * @class: The enum class
 *
 *   Determine the basetype of an enum by iterating through its fields. We do this
 * in a separate function since it is cheaper than calling mono_class_setup_fields.
 */
static MonoType*
mono_class_find_enum_basetype (MonoClass *class)
{
	MonoImage *m = class->image; 
	const int top = class->field.count;
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELD];
	int i;

	g_assert (class->enumtype);

	/*
	 * Fetch all the field information.
	 */
	for (i = 0; i < top; i++){
		const char *sig;
		guint32 cols [MONO_FIELD_SIZE];
		int idx = class->field.first + i;
		MonoGenericContainer *container = NULL;
		MonoType *ftype;

		mono_metadata_decode_row (t, idx, cols, MONO_FIELD_SIZE);
		sig = mono_metadata_blob_heap (m, cols [MONO_FIELD_SIGNATURE]);
		mono_metadata_decode_value (sig, &sig);
		/* FIELD signature == 0x06 */
		g_assert (*sig == 0x06);
		if (class->generic_container)
			container = class->generic_container;
		else if (class->generic_class) {
			MonoClass *gklass = class->generic_class->container_class;

			container = gklass->generic_container;
			g_assert (container);
		}
		ftype = mono_metadata_parse_type_full (
			m, (MonoGenericContext *) container, MONO_PARSE_FIELD,
			cols [MONO_FIELD_FLAGS], sig + 1, &sig);
		if (class->generic_class) {
			ftype = mono_class_inflate_generic_type (
				ftype, class->generic_class->context);
			ftype->attrs = cols [MONO_FIELD_FLAGS];
		}

		if (class->enumtype && !(cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_STATIC))
			return ftype;
	}

	return NULL;
}

/** 
 * mono_class_setup_fields:
 * @class: The class to initialize
 *
 * Initializes the class->fields.
 * Assumes the loader lock is held.
 */
static void
mono_class_setup_fields (MonoClass *class)
{
	MonoImage *m = class->image; 
	const int top = class->field.count;
	guint32 layout = class->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELD];
	int i, blittable = TRUE, real_size = 0;
	guint32 rva;
	guint32 packing_size = 0;
	gboolean explicit_size;
	MonoClassField *field;

	if (class->size_inited)
		return;

	if (class->inited)
		mono_class_init (class);

	class->instance_size = 0;
	class->class_size = 0;

	if (class->parent) {
		if (!class->parent->size_inited)
			mono_class_setup_fields (class->parent);
		class->instance_size += class->parent->instance_size;
		class->min_align = class->parent->min_align;
		/* we use |= since it may have been set already */
		class->has_references |= class->parent->has_references;
		blittable = class->parent->blittable;
	} else {
		class->instance_size = sizeof (MonoObject);
		class->min_align = 1;
	}

	/* Get the real size */
	explicit_size = mono_metadata_packing_from_typedef (class->image, class->type_token, &packing_size, &real_size);

	if (explicit_size) {
		g_assert ((packing_size & 0xfffffff0) == 0);
		class->packing_size = packing_size;
		real_size += class->instance_size;
	}

	if (!top) {
		if (explicit_size && real_size) {
			class->instance_size = MAX (real_size, class->instance_size);
		}
		class->size_inited = 1;
		class->blittable = blittable;
		return;
	}

	if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT)
		blittable = FALSE;

	/* Prevent infinite loops if the class references itself */
	class->size_inited = 1;

	class->fields = mono_mempool_alloc0 (class->image->mempool, sizeof (MonoClassField) * top);

	/*
	 * Fetch all the field information.
	 */
	for (i = 0; i < top; i++){
		const char *sig;
		guint32 cols [MONO_FIELD_SIZE];
		int idx = class->field.first + i;
		MonoGenericContainer *container = NULL;

		field = &class->fields [i];
		mono_metadata_decode_row (t, idx, cols, MONO_FIELD_SIZE);
		/* The name is needed for fieldrefs */
		field->name = mono_metadata_string_heap (m, cols [MONO_FIELD_NAME]);
		sig = mono_metadata_blob_heap (m, cols [MONO_FIELD_SIGNATURE]);
		mono_metadata_decode_value (sig, &sig);
		/* FIELD signature == 0x06 */
		g_assert (*sig == 0x06);
		if (class->generic_container)
			container = class->generic_container;
		else if (class->generic_class) {
			MonoInflatedField *ifield = g_new0 (MonoInflatedField, 1);
			MonoClass *gklass = class->generic_class->container_class;

			container = gklass->generic_container;
			g_assert (container);

			ifield->generic_type = gklass->fields [i].type;
			field->generic_info = ifield;
		}
		field->type = mono_metadata_parse_type_full (
			m, (MonoGenericContext *) container, MONO_PARSE_FIELD,
			cols [MONO_FIELD_FLAGS], sig + 1, &sig);
		if (mono_field_is_deleted (field))
			continue;
		if (class->generic_class) {
			field->type = mono_class_inflate_generic_type (
				field->type, class->generic_class->context);
			field->type->attrs = cols [MONO_FIELD_FLAGS];
		}

		field->parent = class;

		/* Only do these checks if we still think this type is blittable */
		if (blittable && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			if (field->type->byref || MONO_TYPE_IS_REFERENCE (field->type)) {
				blittable = FALSE;
			} else {
				MonoClass *field_class = mono_class_from_mono_type (field->type);
				if (!field_class || !field_class->blittable)
					blittable = FALSE;
			}
		}

		if (layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
			mono_metadata_field_info (m, idx, &field->offset, NULL, NULL);
			if (field->offset == (guint32)-1 && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
				g_warning ("%s not initialized correctly (missing field layout info for %s)", class->name, field->name);
		}

		if (cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_HAS_FIELD_RVA) {
			mono_metadata_field_info (m, idx, NULL, &rva, NULL);
			if (!rva)
				g_warning ("field %s in %s should have RVA data, but hasn't", field->name, class->name);
			field->data = mono_image_rva_map (class->image, rva);
		}

		if (class->enumtype && !(cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_STATIC)) {
			class->enum_basetype = field->type;
			class->cast_class = class->element_class = mono_class_from_mono_type (class->enum_basetype);
			blittable = class->element_class->blittable;
		}

		/* The def_value of fields is compute lazily during vtable creation */
	}

	if (class == mono_defaults.string_class)
		blittable = FALSE;

	class->blittable = blittable;

	if (class->enumtype && !class->enum_basetype) {
		if (!((strcmp (class->name, "Enum") == 0) && (strcmp (class->name_space, "System") == 0)))
			G_BREAKPOINT ();
	}
	if (explicit_size && real_size) {
		class->instance_size = MAX (real_size, class->instance_size);
	}

	mono_class_layout_fields (class);
}

/** 
 * mono_class_setup_fields_locking:
 * @class: The class to initialize
 *
 * Initializes the class->fields.
 * Aquires the loader lock.
 */
static void
mono_class_setup_fields_locking (MonoClass *class)
{
	mono_loader_lock ();
	mono_class_setup_fields (class);
	mono_loader_unlock ();
}

/*
 * mono_class_has_references:
 *
 *   Returns whenever @klass->has_references is set, initializing it if needed.
 * Aquires the loader lock.
 */
static gboolean
mono_class_has_references (MonoClass *klass)
{
	if (klass->init_pending) {
		/* Be conservative */
		return TRUE;
	} else {
		mono_class_init (klass);

		return klass->has_references;
	}
}

/* useful until we keep track of gc-references in corlib etc. */
#define IS_GC_REFERENCE(t) ((t)->type == MONO_TYPE_U || (t)->type == MONO_TYPE_I || (t)->type == MONO_TYPE_PTR)

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 */
void
mono_class_layout_fields (MonoClass *class)
{
	int i;
	const int top = class->field.count;
	guint32 layout = class->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	guint32 pass, passes, real_size;
	gboolean gc_aware_layout = FALSE;
	MonoClassField *field;

	if (class->generic_container ||
	    (class->generic_class && class->generic_class->inst->is_open))
		return;

	/*
	 * Enable GC aware auto layout: in this mode, reference
	 * fields are grouped together inside objects, increasing collector 
	 * performance.
	 * Requires that all classes whose layout is known to native code be annotated
	 * with [StructLayout (LayoutKind.Sequential)]
	 * Value types have gc_aware_layout disabled by default, as per
	 * what the default is for other runtimes.
	 */
	 /* corlib is missing [StructLayout] directives in many places */
	if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		if (class->image != mono_defaults.corlib &&
			class->byval_arg.type != MONO_TYPE_VALUETYPE)
			gc_aware_layout = TRUE;
	}

	/* Compute klass->has_references */
	/* 
	 * Process non-static fields first, since static fields might recursively
	 * refer to the class itself.
	 */
	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &class->fields [i];

		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			ftype = mono_type_get_underlying_type (field->type);
			if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype)))))
				class->has_references = TRUE;
		}
	}

	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &class->fields [i];

		if (!field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
			ftype = mono_type_get_underlying_type (field->type);
			if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype)))))
				class->has_static_refs = TRUE;
		}
	}

	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &class->fields [i];

		ftype = mono_type_get_underlying_type (field->type);
		if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype))))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				class->has_static_refs = TRUE;
			else
				class->has_references = TRUE;
		}
	}

	/*
	 * Compute field layout and total size (not considering static fields)
	 */

	switch (layout) {
	case TYPE_ATTRIBUTE_AUTO_LAYOUT:
	case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:

		if (gc_aware_layout)
			passes = 2;
		else
			passes = 1;

		if (layout != TYPE_ATTRIBUTE_AUTO_LAYOUT)
			passes = 1;

		if (class->parent)
			real_size = class->parent->instance_size;
		else
			real_size = sizeof (MonoObject);

		for (pass = 0; pass < passes; ++pass) {
			for (i = 0; i < top; i++){
				int size, align;

				field = &class->fields [i];

				if (mono_field_is_deleted (field))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;

				if (gc_aware_layout) {
					/* 
					 * We process fields with reference type in the first pass,
					 * and fields with non-reference type in the second pass.
					 * We use IS_POINTER instead of IS_REFERENCE because in
					 * some internal structures, we store GC_MALLOCed memory
					 * in IntPtr fields...
					 */
					if (MONO_TYPE_IS_POINTER (field->type)) {
						if (pass == 1)
							continue;
					} else {
						if (pass == 0)
							continue;
					}
				}

				if ((top == 1) && (class->instance_size == sizeof (MonoObject)) &&
					(strcmp (field->name, "$PRIVATE$") == 0)) {
					/* This field is a hack inserted by MCS to empty structures */
					continue;
				}

				size = mono_type_size (field->type, &align);
			
				/* FIXME (LAMESPEC): should we also change the min alignment according to pack? */
				align = class->packing_size ? MIN (class->packing_size, align): align;
				class->min_align = MAX (align, class->min_align);
				field->offset = real_size;
				field->offset += align - 1;
				field->offset &= ~(align - 1);
				real_size = field->offset + size;
			}

			class->instance_size = MAX (real_size, class->instance_size);
       
			if (class->instance_size & (class->min_align - 1)) {
				class->instance_size += class->min_align - 1;
				class->instance_size &= ~(class->min_align - 1);
			}
		}
		break;
	case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT:
		real_size = 0;
		for (i = 0; i < top; i++) {
			int size, align;

			field = &class->fields [i];

			/*
			 * There must be info about all the fields in a type if it
			 * uses explicit layout.
			 */

			if (mono_field_is_deleted (field))
				continue;
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			size = mono_type_size (field->type, &align);
			
			/*
			 * When we get here, field->offset is already set by the
			 * loader (for either runtime fields or fields loaded from metadata).
			 * The offset is from the start of the object: this works for both
			 * classes and valuetypes.
			 */
			field->offset += sizeof (MonoObject);

			/*
			 * Calc max size.
			 */
			real_size = MAX (real_size, size + field->offset);
		}
		class->instance_size = MAX (real_size, class->instance_size);
		break;
	}

	class->size_inited = 1;

	/*
	 * Compute static field layout and size
	 */
	for (i = 0; i < top; i++){
		int size, align;

		field = &class->fields [i];
			
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC) || field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
			continue;
		if (mono_field_is_deleted (field))
			continue;

		size = mono_type_size (field->type, &align);
		field->offset = class->class_size;
		field->offset += align - 1;
		field->offset &= ~(align - 1);
		class->class_size = field->offset + size;
	}
}

/*
 * mono_class_setup_methods:
 *
 *   Initializes the 'methods' array in the klass.
 * Calling this method should be avoided if possible since it allocates a lot 
 * of long-living MonoMethod structures.
 */
void
mono_class_setup_methods (MonoClass *class)
{
	int i;
	MonoMethod **methods;

	if (class->methods || class->generic_class)
		return;

	mono_loader_lock ();

	if (class->methods) {
		mono_loader_unlock ();
		return;
	}

	//printf ("INIT: %s.%s\n", class->name_space, class->name);

	if (!class->generic_class && !class->methods) {
		methods = mono_mempool_alloc (class->image->mempool, sizeof (MonoMethod*) * class->method.count);
		for (i = 0; i < class->method.count; ++i) {
			methods [i] = mono_get_method (class->image, MONO_TOKEN_METHOD_DEF | (i + class->method.first + 1), class);
		}
	}

	if (MONO_CLASS_IS_INTERFACE (class))
		for (i = 0; i < class->method.count; ++i)
			methods [i]->slot = i;

	/* Leave this assignment as the last op in this function */
	class->methods = methods;

	mono_loader_unlock ();
}


static void
mono_class_setup_properties (MonoClass *class)
{
	guint startm, endm, i, j;
	guint32 cols [MONO_PROPERTY_SIZE];
	MonoTableInfo *pt = &class->image->tables [MONO_TABLE_PROPERTY];
	MonoTableInfo *msemt = &class->image->tables [MONO_TABLE_METHODSEMANTICS];
	MonoProperty *properties;
	guint32 last;

	if (class->properties)
		return;

	mono_loader_lock ();

	if (class->properties) {
		mono_loader_unlock ();
		return;
	}

	class->property.first = mono_metadata_properties_from_typedef (class->image, mono_metadata_token_index (class->type_token) - 1, &last);
	class->property.count = last - class->property.first;

	if (class->property.count)
		mono_class_setup_methods (class);

	properties = mono_mempool_alloc0 (class->image->mempool, sizeof (MonoProperty) * class->property.count);
	for (i = class->property.first; i < last; ++i) {
		mono_metadata_decode_row (pt, i, cols, MONO_PROPERTY_SIZE);
		properties [i - class->property.first].parent = class;
		properties [i - class->property.first].attrs = cols [MONO_PROPERTY_FLAGS];
		properties [i - class->property.first].name = mono_metadata_string_heap (class->image, cols [MONO_PROPERTY_NAME]);

		startm = mono_metadata_methods_from_property (class->image, i, &endm);
		for (j = startm; j < endm; ++j) {
			mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);
			switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
			case METHOD_SEMANTIC_SETTER:
				properties [i - class->property.first].set = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];
				break;
			case METHOD_SEMANTIC_GETTER:
				properties [i - class->property.first].get = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];
				break;
			default:
				break;
			}
		}
	}

	/* Leave this assignment as the last op in the function */
	class->properties = properties;

	mono_loader_unlock ();
}

static void
mono_class_setup_events (MonoClass *class)
{
	guint startm, endm, i, j;
	guint32 cols [MONO_EVENT_SIZE];
	MonoTableInfo *pt = &class->image->tables [MONO_TABLE_EVENT];
	MonoTableInfo *msemt = &class->image->tables [MONO_TABLE_METHODSEMANTICS];
	guint32 last;
	MonoEvent *events;

	if (class->events)
		return;

	mono_loader_lock ();

	if (class->events) {
		mono_loader_unlock ();
		return;
	}
	class->event.first = mono_metadata_events_from_typedef (class->image, mono_metadata_token_index (class->type_token) - 1, &last);
	class->event.count = last - class->event.first;

	if (class->event.count)
		mono_class_setup_methods (class);

	events = mono_mempool_alloc0 (class->image->mempool, sizeof (MonoEvent) * class->event.count);
	for (i = class->event.first; i < last; ++i) {
		MonoEvent *event = &events [i - class->event.first];
			
		mono_metadata_decode_row (pt, i, cols, MONO_EVENT_SIZE);
		event->parent = class;
		event->attrs = cols [MONO_EVENT_FLAGS];
		event->name = mono_metadata_string_heap (class->image, cols [MONO_EVENT_NAME]);

		startm = mono_metadata_methods_from_event (class->image, i, &endm);
		for (j = startm; j < endm; ++j) {
			mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);
			switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
			case METHOD_SEMANTIC_ADD_ON:
				event->add = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];
				break;
			case METHOD_SEMANTIC_REMOVE_ON:
				event->remove = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];
				break;
			case METHOD_SEMANTIC_FIRE:
				event->raise = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];
				break;
			case METHOD_SEMANTIC_OTHER: {
				int n = 0;

				if (event->other == NULL) {
					event->other = g_new0 (MonoMethod*, 1);
				} else {
					while (event->other [n])
						n++;
					event->other = g_realloc (event->other, (n + 1) * sizeof (MonoMethod*));
				}
				event->other [n] = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];
				break;
			}
			default:
				break;
			}
		}
	}
	/* Leave this assignment as the last op in the function */
	class->events = events;

	mono_loader_unlock ();
}

static guint
mono_get_unique_iid (MonoClass *class)
{
	static GHashTable *iid_hash = NULL;
	static guint iid = 0;

	char *str;
	gpointer value;
	
	g_assert (MONO_CLASS_IS_INTERFACE (class));

	mono_loader_lock ();

	if (!iid_hash)
		iid_hash = g_hash_table_new (g_str_hash, g_str_equal);

	str = g_strdup_printf ("%s|%s.%s\n", class->image->name, class->name_space, class->name);

	if (g_hash_table_lookup_extended (iid_hash, str, NULL, &value)) {
		mono_loader_unlock ();
		g_free (str);
		return GPOINTER_TO_INT (value);
	} else {
		g_hash_table_insert (iid_hash, str, GINT_TO_POINTER (iid));
		++iid;
	}

	mono_loader_unlock ();

	g_assert (iid <= 65535);
	return iid - 1;
}

static void
collect_implemented_interfaces_aux (MonoClass *klass, GPtrArray **res)
{
	int i;
	MonoClass *ic;
	
	for (i = 0; i < klass->interface_count; i++) {
		ic = klass->interfaces [i];

		if (*res == NULL)
			*res = g_ptr_array_new ();
		g_ptr_array_add (*res, ic);
		mono_class_init (ic);

		collect_implemented_interfaces_aux (ic, res);
	}
}

GPtrArray*
mono_class_get_implemented_interfaces (MonoClass *klass)
{
	GPtrArray *res = NULL;

	collect_implemented_interfaces_aux (klass, &res);
	return res;
}

typedef struct _IOffsetInfo IOffsetInfo;
struct _IOffsetInfo {
	IOffsetInfo *next;
	int size;
	int next_free;
	int data [MONO_ZERO_LEN_ARRAY];
};

static IOffsetInfo *cached_offset_info = NULL;
static int next_offset_info_size = 128;

static int*
cache_interface_offsets (int max_iid, int *data)
{
	IOffsetInfo *cached_info;
	int *cached;
	int new_size;
	for (cached_info = cached_offset_info; cached_info; cached_info = cached_info->next) {
		cached = cached_info->data;
		while (cached < cached_info->data + cached_info->size && *cached) {
			if (*cached == max_iid) {
				int i, matched = TRUE;
				cached++;
				for (i = 0; i < max_iid; ++i) {
					if (cached [i] != data [i]) {
						matched = FALSE;
						break;
					}
				}
				if (matched)
					return cached;
				cached += max_iid;
			} else {
				cached += *cached + 1;
			}
		}
	}
	/* find a free slot */
	for (cached_info = cached_offset_info; cached_info; cached_info = cached_info->next) {
		if (cached_info->size - cached_info->next_free >= max_iid + 1) {
			cached = &cached_info->data [cached_info->next_free];
			*cached++ = max_iid;
			memcpy (cached, data, max_iid * sizeof (int));
			cached_info->next_free += max_iid + 1;
			return cached;
		}
	}
	/* allocate a new chunk */
	if (max_iid + 1 < next_offset_info_size) {
		new_size = next_offset_info_size;
		if (next_offset_info_size < 4096)
			next_offset_info_size += next_offset_info_size >> 2;
	} else {
		new_size = max_iid + 1;
	}
	cached_info = g_malloc0 (sizeof (IOffsetInfo) + sizeof (int) * new_size);
	cached_info->size = new_size;
	/*g_print ("allocated %d offset entries at %p (total: %d)\n", new_size, cached_info->data, offset_info_total_size);*/
	cached = &cached_info->data [0];
	*cached++ = max_iid;
	memcpy (cached, data, max_iid * sizeof (int));
	cached_info->next_free += max_iid + 1;
	cached_info->next = cached_offset_info;
	cached_offset_info = cached_info;
	return cached;
}

static int
setup_interface_offsets (MonoClass *class, int cur_slot)
{
	MonoClass *k, *ic;
	int i, max_iid;
	int *cached_data;
	GPtrArray *ifaces;

	/* compute maximum number of slots and maximum interface id */
	max_iid = 0;
	for (k = class; k ; k = k->parent) {
		for (i = 0; i < k->interface_count; i++) {
			ic = k->interfaces [i];

			if (!ic->inited)
				mono_class_init (ic);

			if (max_iid < ic->interface_id)
				max_iid = ic->interface_id;
		}
		ifaces = mono_class_get_implemented_interfaces (k);
		if (ifaces) {
			for (i = 0; i < ifaces->len; ++i) {
				ic = g_ptr_array_index (ifaces, i);
				if (max_iid < ic->interface_id)
					max_iid = ic->interface_id;
			}
			g_ptr_array_free (ifaces, TRUE);
		}
	}

	if (MONO_CLASS_IS_INTERFACE (class)) {
		if (max_iid < class->interface_id)
			max_iid = class->interface_id;
	}
	class->max_interface_id = max_iid;
	/* compute vtable offset for interfaces */
	class->interface_offsets = g_malloc (sizeof (gpointer) * (max_iid + 1));

	for (i = 0; i <= max_iid; i++)
		class->interface_offsets [i] = -1;

	ifaces = mono_class_get_implemented_interfaces (class);
	if (ifaces) {
		for (i = 0; i < ifaces->len; ++i) {
			ic = g_ptr_array_index (ifaces, i);
			class->interface_offsets [ic->interface_id] = cur_slot;
			cur_slot += ic->method.count;
		}
		g_ptr_array_free (ifaces, TRUE);
	}

	for (k = class->parent; k ; k = k->parent) {
		ifaces = mono_class_get_implemented_interfaces (k);
		if (ifaces) {
			for (i = 0; i < ifaces->len; ++i) {
				ic = g_ptr_array_index (ifaces, i);

				if (class->interface_offsets [ic->interface_id] == -1) {
					int io = k->interface_offsets [ic->interface_id];

					g_assert (io >= 0);

					class->interface_offsets [ic->interface_id] = io;
				}
			}
			g_ptr_array_free (ifaces, TRUE);
		}
	}

	if (MONO_CLASS_IS_INTERFACE (class))
		class->interface_offsets [class->interface_id] = cur_slot;

	cached_data = cache_interface_offsets (max_iid + 1, class->interface_offsets);
	g_free (class->interface_offsets);
	class->interface_offsets = cached_data;

	return cur_slot;
}

void
mono_class_setup_vtable (MonoClass *class)
{
	MonoMethod **overrides;
	MonoGenericContext *context;
	int onum = 0;

	if (class->vtable)
		return;

	mono_class_setup_methods (class);

	if (MONO_CLASS_IS_INTERFACE (class))
		return;

	mono_loader_lock ();

	if (class->vtable) {
		mono_loader_unlock ();
		return;
	}

	if (class->generic_class)
		context = class->generic_class->context;
	else
		context = (MonoGenericContext *) class->generic_container;		

	mono_class_get_overrides_full (class->image, class->type_token, &overrides, &onum, context);
	mono_class_setup_vtable_general (class, overrides, onum);
	g_free (overrides);

	mono_loader_unlock ();
}

static void
setup_generic_vtable (MonoClass *class, MonoMethod **overrides, int onum)
{
	MonoClass *gklass;
	int i;

	gklass = class->generic_class->container_class;

	mono_class_init (gklass);
	class->vtable_size = gklass->vtable_size;

	class->vtable = g_new0 (MonoMethod*, class->vtable_size);
	memcpy (class->vtable, gklass->vtable,  sizeof (MonoMethod*) * class->vtable_size);

	for (i = 0; i < class->vtable_size; i++) {
		MonoMethod *m = class->vtable [i];

		if (!m)
			continue;

		m = mono_class_inflate_generic_method (m, class->generic_class->context);
		class->vtable [i] = m;
	}

	class->max_interface_id = gklass->max_interface_id;
	class->interface_offsets = g_new0 (gint, gklass->max_interface_id + 1);
	memcpy (class->interface_offsets, gklass->interface_offsets,
		sizeof (gint) * (gklass->max_interface_id + 1));
}

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 */
void
mono_class_setup_vtable_general (MonoClass *class, MonoMethod **overrides, int onum)
{
	MonoClass *k, *ic;
	MonoMethod **vtable;
	int i, max_vtsize = 0, max_iid, cur_slot = 0;
	GPtrArray *ifaces;
	GHashTable *override_map = NULL;
	gboolean security_enabled = mono_is_security_manager_active ();

	if (class->vtable)
		return;

	if (class->generic_class) {
		setup_generic_vtable (class, overrides, onum);
		return;
	}

	ifaces = mono_class_get_implemented_interfaces (class);
	if (ifaces) {
		for (i = 0; i < ifaces->len; i++) {
			MonoClass *ic = g_ptr_array_index (ifaces, i);
			max_vtsize += ic->method.count;
		}
		g_ptr_array_free (ifaces, TRUE);
	}
	
	if (class->parent) {
		mono_class_init (class->parent);
		mono_class_setup_vtable (class->parent);
		max_vtsize += class->parent->vtable_size;
		cur_slot = class->parent->vtable_size;
	}

	max_vtsize += class->method.count;

	vtable = alloca (sizeof (gpointer) * max_vtsize);
	memset (vtable, 0, sizeof (gpointer) * max_vtsize);

	/* printf ("METAINIT %s.%s\n", class->name_space, class->name); */

	cur_slot = setup_interface_offsets (class, cur_slot);
	max_iid = class->max_interface_id;

	if (class->parent && class->parent->vtable_size)
		memcpy (vtable, class->parent->vtable,  sizeof (gpointer) * class->parent->vtable_size);

	/* override interface methods */
	for (i = 0; i < onum; i++) {
		MonoMethod *decl = overrides [i*2];
		if (MONO_CLASS_IS_INTERFACE (decl->klass)) {
			int dslot;
			mono_class_setup_methods (decl->klass);
			g_assert (decl->slot != -1);
			dslot = decl->slot + class->interface_offsets [decl->klass->interface_id];
			vtable [dslot] = overrides [i*2 + 1];
			vtable [dslot]->slot = dslot;
			if (!override_map)
				override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);

			g_hash_table_insert (override_map, overrides [i * 2], overrides [i * 2 + 1]);
		}
	}

	for (k = class; k ; k = k->parent) {
		int nifaces = 0;

		ifaces = mono_class_get_implemented_interfaces (k);
		if (ifaces)
			nifaces = ifaces->len;
		for (i = 0; i < nifaces; i++) {
			int j, l, io;

			ic = g_ptr_array_index (ifaces, i);
			io = k->interface_offsets [ic->interface_id];

			g_assert (io >= 0);
			g_assert (io <= max_vtsize);

			if (k == class) {
				mono_class_setup_methods (ic);
				for (l = 0; l < ic->method.count; l++) {
					MonoMethod *im = ic->methods [l];						

					if (vtable [io + l] && !(vtable [io + l]->flags & METHOD_ATTRIBUTE_ABSTRACT))
						continue;

					for (j = 0; j < class->method.count; ++j) {
						MonoMethod *cm = class->methods [j];
						if (!(cm->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
						    !((cm->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) ||
						    !(cm->flags & METHOD_ATTRIBUTE_NEW_SLOT))
							continue;
						if (!strcmp(cm->name, im->name) && 
						    mono_metadata_signature_equal (mono_method_signature (cm), mono_method_signature (im))) {

							/* CAS - SecurityAction.InheritanceDemand on interface */
							if (security_enabled && (im->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
								mono_secman_inheritancedemand_method (cm, im);
							}

							g_assert (io + l <= max_vtsize);
							vtable [io + l] = cm;
						}
					}
				}
			} else {
				/* already implemented */
				if (io >= k->vtable_size)
					continue;
			}
				
			for (l = 0; l < ic->method.count; l++) {
				MonoMethod *im = ic->methods [l];						
				MonoClass *k1;

				g_assert (io + l <= max_vtsize);

 				if (vtable [io + l] && !(vtable [io + l]->flags & METHOD_ATTRIBUTE_ABSTRACT))
					continue;
					
				for (k1 = class; k1; k1 = k1->parent) {
					for (j = 0; j < k1->method.count; ++j) {
						MonoMethod *cm = k1->methods [j];

						if (!(cm->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
						    !(cm->flags & METHOD_ATTRIBUTE_PUBLIC))
							continue;
						
						if (!strcmp(cm->name, im->name) && 
						    mono_metadata_signature_equal (mono_method_signature (cm), mono_method_signature (im))) {

							/* CAS - SecurityAction.InheritanceDemand on interface */
							if (security_enabled && (im->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
								mono_secman_inheritancedemand_method (cm, im);
							}

							g_assert (io + l <= max_vtsize);
							vtable [io + l] = cm;
							break;
						}
						
					}
					g_assert (io + l <= max_vtsize);
					if (vtable [io + l] && !(vtable [io + l]->flags & METHOD_ATTRIBUTE_ABSTRACT))
						break;
				}
			}

			for (l = 0; l < ic->method.count; l++) {
				MonoMethod *im = ic->methods [l];						
				char *qname, *fqname, *cname, *the_cname;
				MonoClass *k1;
				
				if (vtable [io + l])
					continue;

				if (ic->generic_class) {
					the_cname = mono_type_get_name_full (&ic->byval_arg, MONO_TYPE_NAME_FORMAT_IL);
					cname = the_cname;
				} else {
					the_cname = NULL;
					cname = (char*)ic->name;
				}
					
				qname = g_strconcat (cname, ".", im->name, NULL);
				if (ic->name_space && ic->name_space [0])
					fqname = g_strconcat (ic->name_space, ".", cname, ".", im->name, NULL);
				else
					fqname = NULL;

				for (k1 = class; k1; k1 = k1->parent) {
					for (j = 0; j < k1->method.count; ++j) {
						MonoMethod *cm = k1->methods [j];

						if (!(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
							continue;

						if (((fqname && !strcmp (cm->name, fqname)) || !strcmp (cm->name, qname)) &&
						    mono_metadata_signature_equal (mono_method_signature (cm), mono_method_signature (im))) {

							/* CAS - SecurityAction.InheritanceDemand on interface */
							if (security_enabled && (im->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
								mono_secman_inheritancedemand_method (cm, im);
							}

							g_assert (io + l <= max_vtsize);
							vtable [io + l] = cm;
							break;
						}
					}
				}
				g_free (the_cname);
				g_free (qname);
				g_free (fqname);
			}

			
			if (!(class->flags & TYPE_ATTRIBUTE_ABSTRACT)) {
				for (l = 0; l < ic->method.count; l++) {
					char *msig;
					MonoMethod *im = ic->methods [l];
					if (im->flags & METHOD_ATTRIBUTE_STATIC)
							continue;
					g_assert (io + l <= max_vtsize);

					/* 
					 * If one of our parents already implements this interface
					 * we can inherit the implementation.
					 */
					if (!(vtable [io + l])) {
						MonoClass *parent = class->parent;

						if ((ic->interface_id <= parent->max_interface_id) && 
							(parent->interface_offsets [ic->interface_id]) &&
							parent->vtable)
							vtable [io + l] = parent->vtable [parent->interface_offsets [ic->interface_id] + l];
					}

					if (!(vtable [io + l])) {
						for (j = 0; j < onum; ++j) {
							g_print (" at slot %d: %s (%d) overrides %s (%d)\n", io+l, overrides [j*2+1]->name, 
								 overrides [j*2+1]->slot, overrides [j*2]->name, overrides [j*2]->slot);
						}
						msig = mono_signature_get_desc (mono_method_signature (im), FALSE);
						printf ("no implementation for interface method %s.%s::%s(%s) in class %s.%s\n",
							ic->name_space, ic->name, im->name, msig, class->name_space, class->name);
						g_free (msig);
						for (j = 0; j < class->method.count; ++j) {
							MonoMethod *cm = class->methods [j];
							msig = mono_signature_get_desc (mono_method_signature (cm), FALSE);
							
							printf ("METHOD %s(%s)\n", cm->name, msig);
							g_free (msig);
						}
						g_assert_not_reached ();
					}
				}
			}
		
			for (l = 0; l < ic->method.count; l++) {
				MonoMethod *im = vtable [io + l];

				if (im) {
					g_assert (io + l <= max_vtsize);
					if (im->slot < 0) {
						/* FIXME: why do we need this ? */
						im->slot = io + l;
						/* g_assert_not_reached (); */
					}
				}
			}
		}
		if (ifaces)
			g_ptr_array_free (ifaces, TRUE);
	} 

	for (i = 0; i < class->method.count; ++i) {
		MonoMethod *cm;
	       
		cm = class->methods [i];
		
		/*
		 * Non-virtual method have no place in the vtable.
		 * This also catches static methods (since they are not virtual).
		 */
		if (!(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
			continue;
		
		/*
		 * If the method is REUSE_SLOT, we must check in the
		 * base class for a method to override.
		 */
		if (!(cm->flags & METHOD_ATTRIBUTE_NEW_SLOT)) {
			int slot = -1;
			for (k = class->parent; k ; k = k->parent) {
				int j;
				for (j = 0; j < k->method.count; ++j) {
					MonoMethod *m1 = k->methods [j];
					if (!(m1->flags & METHOD_ATTRIBUTE_VIRTUAL))
						continue;
					if (!strcmp(cm->name, m1->name) && 
					    mono_metadata_signature_equal (mono_method_signature (cm), mono_method_signature (m1))) {

						/* CAS - SecurityAction.InheritanceDemand */
						if (security_enabled && (m1->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
							mono_secman_inheritancedemand_method (cm, m1);
						}

						slot = k->methods [j]->slot;
						g_assert (cm->slot < max_vtsize);
						if (!override_map)
							override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
						g_hash_table_insert (override_map, m1, cm);
						break;
					}
				}
				if (slot >= 0) 
					break;
			}
			if (slot >= 0)
				cm->slot = slot;
		}

		if (cm->slot < 0)
			cm->slot = cur_slot++;

		if (!(cm->flags & METHOD_ATTRIBUTE_ABSTRACT))
			vtable [cm->slot] = cm;
	}

	/* override non interface methods */
	for (i = 0; i < onum; i++) {
		MonoMethod *decl = overrides [i*2];
		if (!MONO_CLASS_IS_INTERFACE (decl->klass)) {
			g_assert (decl->slot != -1);
			vtable [decl->slot] = overrides [i*2 + 1];
 			overrides [i * 2 + 1]->slot = decl->slot;
			if (!override_map)
				override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
			g_hash_table_insert (override_map, decl, overrides [i * 2 + 1]);
		}
	}

	/*
	 * If a method occupies more than one place in the vtable, and it is
	 * overriden, then change the other occurances too.
	 */
	if (override_map) {
		for (i = 0; i < max_vtsize; ++i)
			if (vtable [i]) {
				MonoMethod *cm = g_hash_table_lookup (override_map, vtable [i]);
				if (cm)
					vtable [i] = cm;
			}

		g_hash_table_destroy (override_map);
	}

	if (class->generic_class) {
		MonoClass *gklass = class->generic_class->container_class;

		mono_class_init (gklass);
		class->vtable_size = gklass->vtable_size;
	} else       
		class->vtable_size = cur_slot;

	class->vtable = g_malloc0 (sizeof (gpointer) * class->vtable_size);
	memcpy (class->vtable, vtable,  sizeof (gpointer) * class->vtable_size);

	if (mono_print_vtable) {
		int icount = 0;

		for (i = 0; i <= max_iid; i++)
			if (class->interface_offsets [i] != -1)
				icount++;

		printf ("VTable %s.%s (size = %d, interfaces = %d)\n", class->name_space, 
			class->name, class->vtable_size, icount); 

		for (i = 0; i < class->vtable_size; ++i) {
			MonoMethod *cm;
	       
			cm = vtable [i];
			if (cm) {
				printf ("  slot %03d(%03d) %s.%s:%s\n", i, cm->slot,
					cm->klass->name_space, cm->klass->name,
					cm->name);
			}
		}


		if (icount) {
			printf ("Interfaces %s.%s (max_iid = %d)\n", class->name_space, 
				class->name, max_iid);
	
			for (i = 0; i < class->interface_count; i++) {
				ic = class->interfaces [i];
				printf ("  slot %03d(%03d) %s.%s\n",  
					class->interface_offsets [ic->interface_id],
					ic->method.count, ic->name_space, ic->name);
			}

			for (k = class->parent; k ; k = k->parent) {
				for (i = 0; i < k->interface_count; i++) {
					ic = k->interfaces [i]; 
					printf ("  slot %03d(%03d) %s.%s\n", 
						class->interface_offsets [ic->interface_id],
						ic->method.count, ic->name_space, ic->name);
				}
			}
		}
	}
}

/**
 * mono_class_init:
 * @class: the class to initialize
 *
 * compute the instance_size, class_size and other infos that cannot be 
 * computed at mono_class_get() time. Also compute a generic vtable and 
 * the method slot numbers. We use this infos later to create a domain
 * specific vtable.  
 */
void
mono_class_init (MonoClass *class)
{
	int i;
	static MonoMethod *default_ghc = NULL;
	static MonoMethod *default_finalize = NULL;
	static int finalize_slot = -1;
	static int ghc_slot = -1;
	MonoCachedClassInfo cached_info;
	gboolean has_cached_info;

	g_assert (class);

	if (class->inited)
		return;

	/*g_print ("Init class %s\n", class->name);*/

	/* We do everything inside the lock to prevent races */
	mono_loader_lock ();

	if (class->inited) {
		mono_loader_unlock ();
		/* Somebody might have gotten in before us */
		return;
	}

	if (class->init_pending) {
		mono_loader_unlock ();
		/* this indicates a cyclic dependency */
		g_error ("pending init %s.%s\n", class->name_space, class->name);
	}

	class->init_pending = 1;

	/* CAS - SecurityAction.InheritanceDemand */
	if (mono_is_security_manager_active () && class->parent && (class->parent->flags & TYPE_ATTRIBUTE_HAS_SECURITY)) {
		mono_secman_inheritancedemand_class (class, class->parent);
	}

	if (mono_debugger_start_class_init_func)
		mono_debugger_start_class_init_func (class);

	mono_stats.initialized_class_count++;

	if (class->generic_class && !class->generic_class->is_dynamic) {
		MonoInflatedGenericClass *gclass;
		MonoClass *gklass;

		gclass = mono_get_inflated_generic_class (class->generic_class);
		gklass = gclass->generic_class.container_class;

		mono_stats.generic_class_count++;

		class->method = gklass->method;
		class->field = gklass->field;

		mono_class_init (gklass);
		mono_class_setup_methods (gklass);
		mono_class_setup_properties (gklass);

		if (MONO_CLASS_IS_INTERFACE (class))
			class->interface_id = mono_get_unique_iid (class);

		g_assert (class->method.count == gklass->method.count);
		class->methods = g_new0 (MonoMethod *, class->method.count);

		for (i = 0; i < class->method.count; i++) {
			MonoMethod *inflated = mono_class_inflate_generic_method (
				gklass->methods [i], gclass->generic_class.context);

			class->methods [i] = mono_get_inflated_method (inflated);
		}

		class->property = gklass->property;
		class->properties = g_new0 (MonoProperty, class->property.count);

		for (i = 0; i < class->property.count; i++) {
			MonoProperty *prop = &class->properties [i];

			*prop = gklass->properties [i];

			if (prop->get)
				prop->get = mono_class_inflate_generic_method (
					prop->get, gclass->generic_class.context);
			if (prop->set)
				prop->set = mono_class_inflate_generic_method (
					prop->set, gclass->generic_class.context);

			prop->parent = class;
		}

		g_assert (class->interface_count == gklass->interface_count);
	}

	if (class->parent && !class->parent->inited)
		mono_class_init (class->parent);

	has_cached_info = mono_class_get_cached_class_info (class, &cached_info);

	if (!class->generic_class && (!has_cached_info || (has_cached_info && cached_info.has_nested_classes))) {
		i = mono_metadata_nesting_typedef (class->image, class->type_token, 1);
		while (i) {
			MonoClass* nclass;
			guint32 cols [MONO_NESTED_CLASS_SIZE];
			mono_metadata_decode_row (&class->image->tables [MONO_TABLE_NESTEDCLASS], i - 1, cols, MONO_NESTED_CLASS_SIZE);
			nclass = mono_class_create_from_typedef (class->image, MONO_TOKEN_TYPE_DEF | cols [MONO_NESTED_CLASS_NESTED]);
			class->nested_classes = g_list_prepend (class->nested_classes, nclass);

			i = mono_metadata_nesting_typedef (class->image, class->type_token, i + 1);
		}
	}

	/*
	 * Computes the size used by the fields, and their locations
	 */
	if (has_cached_info) {
		class->instance_size = cached_info.instance_size;
		class->class_size = cached_info.class_size;
		class->packing_size = cached_info.packing_size;
		class->min_align = cached_info.min_align;
		class->blittable = cached_info.blittable;
		class->has_references = cached_info.has_references;
		class->has_static_refs = cached_info.has_static_refs;
	}
	else
		if (!class->size_inited)
			mono_class_setup_fields (class);

	/* initialize method pointers */
	if (class->rank) {
		MonoMethod *ctor;
		MonoMethodSignature *sig;
		class->method.count = class->rank > 1? 2: 1;
		sig = mono_metadata_signature_alloc (class->image, class->rank);
		sig->ret = &mono_defaults.void_class->byval_arg;
		sig->pinvoke = TRUE;
		for (i = 0; i < class->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;

		ctor = (MonoMethod *) g_new0 (MonoMethodPInvoke, 1);
		ctor->klass = class;
		ctor->flags = METHOD_ATTRIBUTE_PUBLIC | METHOD_ATTRIBUTE_RT_SPECIAL_NAME | METHOD_ATTRIBUTE_SPECIAL_NAME;
		ctor->iflags = METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;
		ctor->signature = sig;
		ctor->name = ".ctor";
		ctor->slot = -1;
		class->methods = g_new (MonoMethod*, class->method.count);
		class->methods [0] = ctor;
		if (class->rank > 1) {
			sig = mono_metadata_signature_alloc (class->image, class->rank * 2);
			sig->ret = &mono_defaults.void_class->byval_arg;
			sig->pinvoke = TRUE;
			for (i = 0; i < class->rank * 2; ++i)
				sig->params [i] = &mono_defaults.int32_class->byval_arg;

			ctor = (MonoMethod *) g_new0 (MonoMethodPInvoke, 1);
			ctor->klass = class;
			ctor->flags = METHOD_ATTRIBUTE_PUBLIC | METHOD_ATTRIBUTE_RT_SPECIAL_NAME | METHOD_ATTRIBUTE_SPECIAL_NAME;
			ctor->iflags = METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;
			ctor->signature = sig;
			ctor->name = ".ctor";
			ctor->slot = -1;
			class->methods [1] = ctor;
		}
	}

	mono_class_setup_supertypes (class);

	if (!default_ghc) {
		if (class == mono_defaults.object_class) { 
			mono_class_setup_vtable (class);		       
			for (i = 0; i < class->vtable_size; ++i) {
				MonoMethod *cm = class->vtable [i];
	       
				if (!strcmp (cm->name, "GetHashCode")) {
					ghc_slot = i;
					break;
				}
			}

			g_assert (ghc_slot > 0);

			default_ghc = class->vtable [ghc_slot];
		}
	}

	if (!default_finalize) {
		if (class == mono_defaults.object_class) { 
			mono_class_setup_vtable (class);		       
			for (i = 0; i < class->vtable_size; ++i) {
				MonoMethod *cm = class->vtable [i];
	       
				if (!strcmp (cm->name, "Finalize")) {
					finalize_slot = i;
					break;
				}
			}

			g_assert (finalize_slot > 0);

			default_finalize = class->vtable [finalize_slot];
		}
	}

	/*
	 * If possible, avoid the creation of the generic vtable by requesting
	 * cached info from the runtime.
	 */
	if (has_cached_info) {
		guint32 cur_slot = 0;

		class->vtable_size = cached_info.vtable_size;
		class->has_finalize = cached_info.has_finalize;
		class->ghcimpl = cached_info.ghcimpl;
		class->has_cctor = cached_info.has_cctor;

		if (class->parent) {
			mono_class_init (class->parent);
			cur_slot = class->parent->vtable_size;
		}

		setup_interface_offsets (class, cur_slot);
	}
	else {
		mono_class_setup_vtable (class);
	
		class->ghcimpl = 1;
		if (class->parent) { 
			MonoMethod *cmethod = class->vtable [ghc_slot];
			if (cmethod->is_inflated)
				cmethod = ((MonoMethodInflated*)cmethod)->declaring;
			if (cmethod == default_ghc) {
				class->ghcimpl = 0;
			}
		}

		/* Object::Finalize should have empty implemenatation */
		class->has_finalize = 0;
		if (class->parent) { 
			MonoMethod *cmethod = class->vtable [finalize_slot];
			if (cmethod->is_inflated)
				cmethod = ((MonoMethodInflated*)cmethod)->declaring;
			if (cmethod != default_finalize) {
				class->has_finalize = 1;
			}
		}

		for (i = 0; i < class->method.count; ++i) {
			MonoMethod *method = class->methods [i];
			if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
				(strcmp (".cctor", method->name) == 0)) {
				class->has_cctor = 1;
				break;
			}
		}
	}

	class->inited = 1;
	class->init_pending = 0;
	
	mono_loader_unlock ();

	if (MONO_CLASS_IS_INTERFACE (class)) {
		/* 
		 * class->interface_offsets is needed for the castclass/isinst code, so
		 * we have to setup them for interfaces, too.
		 */
		setup_interface_offsets (class, 0);
	}

	if (mono_debugger_class_init_func)
		mono_debugger_class_init_func (class);
}

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_mono_type (MonoClass *class)
{
	const char *name = class->name;
	const char *nspace = class->name_space;

	if (MONO_CLASS_IS_INTERFACE (class))
		class->interface_id = mono_get_unique_iid (class);

	class->this_arg.byref = 1;
	class->this_arg.data.klass = class;
	class->this_arg.type = MONO_TYPE_CLASS;
	class->byval_arg.data.klass = class;
	class->byval_arg.type = MONO_TYPE_CLASS;

	if (!strcmp (nspace, "System")) {
		if (!strcmp (name, "ValueType")) {
			/*
			 * do not set the valuetype bit for System.ValueType.
			 * class->valuetype = 1;
			 */
			class->blittable = TRUE;
		} else if (!strcmp (name, "Enum")) {
			/*
			 * do not set the valuetype bit for System.Enum.
			 * class->valuetype = 1;
			 */
			class->valuetype = 0;
			class->enumtype = 0;
		} else if (!strcmp (name, "Object")) {
			class->this_arg.type = class->byval_arg.type = MONO_TYPE_OBJECT;
		} else if (!strcmp (name, "String")) {
			class->this_arg.type = class->byval_arg.type = MONO_TYPE_STRING;
		} else if (!strcmp (name, "TypedReference")) {
			class->this_arg.type = class->byval_arg.type = MONO_TYPE_TYPEDBYREF;
		}
	}
	
	if (class->valuetype) {
		int t = MONO_TYPE_VALUETYPE;
		if (!strcmp (nspace, "System")) {
			switch (*name) {
			case 'B':
				if (!strcmp (name, "Boolean")) {
					t = MONO_TYPE_BOOLEAN;
				} else if (!strcmp(name, "Byte")) {
					t = MONO_TYPE_U1;
					class->blittable = TRUE;						
				}
				break;
			case 'C':
				if (!strcmp (name, "Char")) {
					t = MONO_TYPE_CHAR;
				}
				break;
			case 'D':
				if (!strcmp (name, "Double")) {
					t = MONO_TYPE_R8;
					class->blittable = TRUE;						
				}
				break;
			case 'I':
				if (!strcmp (name, "Int32")) {
					t = MONO_TYPE_I4;
					class->blittable = TRUE;
				} else if (!strcmp(name, "Int16")) {
					t = MONO_TYPE_I2;
					class->blittable = TRUE;
				} else if (!strcmp(name, "Int64")) {
					t = MONO_TYPE_I8;
					class->blittable = TRUE;
				} else if (!strcmp(name, "IntPtr")) {
					t = MONO_TYPE_I;
					class->blittable = TRUE;
				}
				break;
			case 'S':
				if (!strcmp (name, "Single")) {
					t = MONO_TYPE_R4;
					class->blittable = TRUE;						
				} else if (!strcmp(name, "SByte")) {
					t = MONO_TYPE_I1;
					class->blittable = TRUE;
				}
				break;
			case 'U':
				if (!strcmp (name, "UInt32")) {
					t = MONO_TYPE_U4;
					class->blittable = TRUE;
				} else if (!strcmp(name, "UInt16")) {
					t = MONO_TYPE_U2;
					class->blittable = TRUE;
				} else if (!strcmp(name, "UInt64")) {
					t = MONO_TYPE_U8;
					class->blittable = TRUE;
				} else if (!strcmp(name, "UIntPtr")) {
					t = MONO_TYPE_U;
					class->blittable = TRUE;
				}
				break;
			case 'T':
				if (!strcmp (name, "TypedReference")) {
					t = MONO_TYPE_TYPEDBYREF;
					class->blittable = TRUE;
				}
				break;
			case 'V':
				if (!strcmp (name, "Void")) {
					t = MONO_TYPE_VOID;
				}
				break;
			default:
				break;
			}
		}
		class->this_arg.type = class->byval_arg.type = t;
	}
}

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_parent (MonoClass *class, MonoClass *parent)
{
	gboolean system_namespace;

	system_namespace = !strcmp (class->name_space, "System");

	/* if root of the hierarchy */
	if (system_namespace && !strcmp (class->name, "Object")) {
		class->parent = NULL;
		class->instance_size = sizeof (MonoObject);
		return;
	}
	if (!strcmp (class->name, "<Module>")) {
		class->parent = NULL;
		class->instance_size = 0;
		return;
	}

	if (!MONO_CLASS_IS_INTERFACE (class)) {
		class->parent = parent;

		if (!parent)
			g_assert_not_reached (); /* FIXME */

		if (parent->generic_class && !parent->name) {
			/*
			 * If the parent is a generic instance, we may get
			 * called before it is fully initialized, especially
			 * before it has its name.
			 */
			return;
		}

		class->marshalbyref = parent->marshalbyref;
		class->contextbound  = parent->contextbound;
		class->delegate  = parent->delegate;
		
		if (system_namespace) {
			if (*class->name == 'M' && !strcmp (class->name, "MarshalByRefObject"))
				class->marshalbyref = 1;

			if (*class->name == 'C' && !strcmp (class->name, "ContextBoundObject")) 
				class->contextbound  = 1;

			if (*class->name == 'D' && !strcmp (class->name, "Delegate")) 
				class->delegate  = 1;
		}

		if (class->parent->enumtype || ((strcmp (class->parent->name, "ValueType") == 0) && 
						(strcmp (class->parent->name_space, "System") == 0)))
			class->valuetype = 1;
		if (((strcmp (class->parent->name, "Enum") == 0) && (strcmp (class->parent->name_space, "System") == 0))) {
			class->valuetype = class->enumtype = 1;
		}
		/*class->enumtype = class->parent->enumtype; */
		mono_class_setup_supertypes (class);
	} else {
		class->parent = NULL;
	}

}

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_supertypes (MonoClass *class)
{
	int ms;

	if (class->supertypes)
		return;

	if (class->parent && !class->parent->supertypes)
		mono_class_setup_supertypes (class->parent);
	if (class->parent)
		class->idepth = class->parent->idepth + 1;
	else
		class->idepth = 1;

	ms = MAX (MONO_DEFAULT_SUPERTABLE_SIZE, class->idepth);
	class->supertypes = g_new0 (MonoClass *, ms);

	if (class->parent) {
		class->supertypes [class->idepth - 1] = class;
		memcpy (class->supertypes, class->parent->supertypes, class->parent->idepth * sizeof (gpointer));
	} else {
		class->supertypes [0] = class;
	}
}	

/*
 * If we inherit a type parameter from an outer class, set its owner to that class.
 */
static int
set_generic_param_owner (MonoGenericContainer *container, MonoClass *klass, int pos)
{
	MonoGenericContainer *gc;
	int i;

	if (klass->nested_in)
		pos = set_generic_param_owner (container, klass->nested_in, pos);

	if (!klass->generic_container)
		return pos;

	gc = klass->generic_container;
	for (i = pos; i < gc->type_argc; i++)
		container->type_params [i].owner = gc;

	return pos + gc->type_argc;
}

static MonoGenericInst *
get_shared_inst (MonoGenericContainer *container)
{
	MonoGenericInst *nginst;
	int i;

	nginst = g_new0 (MonoGenericInst, 1);
	nginst->type_argc = container->type_argc;
	nginst->type_argv = g_new0 (MonoType *, nginst->type_argc);
	nginst->is_reference = 1;
	nginst->is_open = 1;

	for (i = 0; i < nginst->type_argc; i++) {
		MonoType *t = g_new0 (MonoType, 1);

		t->type = container->is_method ? MONO_TYPE_MVAR : MONO_TYPE_VAR;
		t->data.generic_param = &container->type_params [i];

		nginst->type_argv [i] = t;
	}

	return mono_metadata_lookup_generic_inst (nginst);
}

/*
 * In preparation for implementing shared code.
 */
MonoGenericClass *
mono_get_shared_generic_class (MonoGenericContainer *container, gboolean is_dynamic)
{
	MonoInflatedGenericClass *igclass;
	MonoGenericClass *gclass;

	if (is_dynamic) {
		MonoDynamicGenericClass *dgclass = g_new0 (MonoDynamicGenericClass, 1);
		igclass = &dgclass->generic_class;
		gclass = &igclass->generic_class;
		gclass->is_inflated = 1;
		gclass->is_dynamic = 1;
	} else {
		igclass = g_new0 (MonoInflatedGenericClass, 1);
		gclass = &igclass->generic_class;
		gclass->is_inflated = 1;
	}

	gclass->context = &container->context;
	gclass->container_class = container->klass;
	gclass->inst = get_shared_inst (container);

	if (!is_dynamic) {
		MonoGenericClass *cached = mono_metadata_lookup_generic_class (gclass);

		if (cached) {
			g_free (gclass);
			return cached;
		}
	}

	igclass->klass = container->klass;

	return gclass;
}

/**
 * @image: context where the image is created
 * @type_token:  typedef token
 */
static MonoClass *
mono_class_create_from_typedef (MonoImage *image, guint32 type_token)
{
	MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];
	MonoClass *class, *parent = NULL;
	guint32 cols [MONO_TYPEDEF_SIZE];
	guint32 cols_next [MONO_TYPEDEF_SIZE];
	guint tidx = mono_metadata_token_index (type_token);
	MonoGenericContext *context = NULL;
	const char *name, *nspace;
	guint icount = 0; 
	MonoClass **interfaces;
	guint32 field_last, method_last;

	mono_loader_lock ();

	if ((class = g_hash_table_lookup (image->class_cache, GUINT_TO_POINTER (type_token)))) {
		mono_loader_unlock ();
		return class;
	}

	g_assert (mono_metadata_token_table (type_token) == MONO_TABLE_TYPEDEF);

	mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);
	
	name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
	nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);

	class = g_malloc0 (sizeof (MonoClass));

	class->name = name;
	class->name_space = nspace;

	class->image = image;
	class->type_token = type_token;
	class->flags = cols [MONO_TYPEDEF_FLAGS];

	g_hash_table_insert (image->class_cache, GUINT_TO_POINTER (type_token), class);

	/*
	 * Check whether we're a generic type definition.
	 */
	class->generic_container = mono_metadata_load_generic_params (image, class->type_token, NULL);
	if (class->generic_container) {
		class->generic_container->klass = class;
		context = &class->generic_container->context;

		context->gclass = mono_get_shared_generic_class (context->container, FALSE);
	}

	if (cols [MONO_TYPEDEF_EXTENDS]) {
		parent = mono_class_get_full (
			image, mono_metadata_token_from_dor (cols [MONO_TYPEDEF_EXTENDS]), context);
	}

	mono_class_setup_parent (class, parent);

	mono_class_setup_mono_type (class);

	if (!class->enumtype) {
		mono_metadata_interfaces_from_typedef_full (image, type_token, &interfaces, &icount, context);

		class->interfaces = interfaces;
		class->interface_count = icount;
	}

	if ((class->flags & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_UNICODE_CLASS)
		class->unicode = 1;
	/* fixme: maybe we must set this on windows 
	if ((class->flags & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_AUTO_CLASS)
		class->unicode = 1;
	*/

	class->cast_class = class->element_class = class;

	/*g_print ("Load class %s\n", name);*/

	/*
	 * Compute the field and method lists
	 */
	class->field.first  = cols [MONO_TYPEDEF_FIELD_LIST] - 1;
	class->method.first = cols [MONO_TYPEDEF_METHOD_LIST] - 1;

	if (tt->rows > tidx){		
		mono_metadata_decode_row (tt, tidx, cols_next, MONO_TYPEDEF_SIZE);
		field_last  = cols_next [MONO_TYPEDEF_FIELD_LIST] - 1;
		method_last = cols_next [MONO_TYPEDEF_METHOD_LIST] - 1;
	} else {
		field_last  = image->tables [MONO_TABLE_FIELD].rows;
		method_last = image->tables [MONO_TABLE_METHOD].rows;
	}

	if (cols [MONO_TYPEDEF_FIELD_LIST] && 
	    cols [MONO_TYPEDEF_FIELD_LIST] <= image->tables [MONO_TABLE_FIELD].rows)
		class->field.count = field_last - class->field.first;
	else
		class->field.count = 0;

	if (cols [MONO_TYPEDEF_METHOD_LIST] <= image->tables [MONO_TABLE_METHOD].rows)
		class->method.count = method_last - class->method.first;
	else
		class->method.count = 0;

	/* reserve space to store vector pointer in arrays */
	if (!strcmp (nspace, "System") && !strcmp (name, "Array")) {
		class->instance_size += 2 * sizeof (gpointer);
		g_assert (class->field.count == 0);
	}

	if (class->enumtype) {
		class->enum_basetype = mono_class_find_enum_basetype (class);
		class->cast_class = class->element_class = mono_class_from_mono_type (class->enum_basetype);
	}

	/*
	 * If we're a generic type definition, load the constraints.
	 * We must do this after the class has been constructed to make certain recursive scenarios
	 * work.
	 */
	if (class->generic_container)
		mono_metadata_load_generic_param_constraints (
			image, type_token, class->generic_container);

	if ((type_token = mono_metadata_nested_in_typedef (image, type_token)))
		class->nested_in = mono_class_create_from_typedef (image, type_token);

	/*
	 * If we inherit any type parameters from our containing class, we need to modify
	 * their `owner'.
	 */
	if (class->nested_in && class->generic_container)
		set_generic_param_owner (class->generic_container, class->nested_in, 0);

	mono_loader_unlock ();

	return class;
}

/*
 * Create the `MonoClass' for an instantiation of a generic type.
 * We only do this if we actually need it.
 */
static void
mono_class_create_generic (MonoInflatedGenericClass *gclass)
{
	MonoClass *klass, *gklass;
	int i;

	if (gclass->is_initialized)
		return;
	gclass->is_initialized = TRUE;

	if (!gclass->klass)
		gclass->klass = g_malloc0 (sizeof (MonoClass));
	klass = gclass->klass;

	gklass = gclass->generic_class.container_class;

	klass->nested_in = gklass->nested_in;

	klass->name = gklass->name;
	klass->name_space = gklass->name_space;
	klass->image = gklass->image;
	klass->flags = gklass->flags;

	klass->generic_class = &gclass->generic_class;

	klass->this_arg.type = klass->byval_arg.type = MONO_TYPE_GENERICINST;
	klass->this_arg.data.generic_class = klass->byval_arg.data.generic_class = &gclass->generic_class;
	klass->this_arg.byref = TRUE;

	klass->cast_class = klass->element_class = klass;

	if (gclass->generic_class.is_dynamic) {
		klass->instance_size = gklass->instance_size;
		klass->class_size = gklass->class_size;
		klass->size_inited = 1;
		klass->inited = 1;

		klass->valuetype = gklass->valuetype;

		mono_class_setup_supertypes (klass);
	}

	klass->interface_count = gklass->interface_count;
	klass->interfaces = g_new0 (MonoClass *, klass->interface_count);
	for (i = 0; i < klass->interface_count; i++) {
		MonoType *it = &gklass->interfaces [i]->byval_arg;
		MonoType *inflated = mono_class_inflate_generic_type (
			it, gclass->generic_class.context);
		klass->interfaces [i] = mono_class_from_mono_type (inflated);
	}

	i = mono_metadata_nesting_typedef (klass->image, gklass->type_token, 1);
	while (i) {
		MonoClass* nclass;
		guint32 cols [MONO_NESTED_CLASS_SIZE];
		mono_metadata_decode_row (&klass->image->tables [MONO_TABLE_NESTEDCLASS], i - 1, cols, MONO_NESTED_CLASS_SIZE);
		nclass = mono_class_create_from_typedef (klass->image, MONO_TOKEN_TYPE_DEF | cols [MONO_NESTED_CLASS_NESTED]);
		klass->nested_classes = g_list_prepend (klass->nested_classes, nclass);
		
		i = mono_metadata_nesting_typedef (klass->image, gklass->type_token, i + 1);
	}

	if (gclass->generic_class.is_dynamic) {
		MonoDynamicGenericClass *dgclass = (MonoDynamicGenericClass *) gclass;

		if (dgclass->parent)
			klass->parent = mono_class_from_mono_type (dgclass->parent);
	} else if (gklass->parent) {
		MonoType *inflated = mono_class_inflate_generic_type (
			&gklass->parent->byval_arg, gclass->generic_class.context);

		klass->parent = mono_class_from_mono_type (inflated);
	}

	if (klass->parent)
		mono_class_setup_parent (klass, klass->parent);
}

MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *image, gboolean is_mvar)
{
	MonoClass *klass, **ptr;
	int count, pos, i;

	if (param->pklass)
		return param->pklass;

	klass = param->pklass = g_new0 (MonoClass, 1);

	for (count = 0, ptr = param->constraints; ptr && *ptr; ptr++, count++)
		;

	pos = 0;
	if ((count > 0) && !MONO_CLASS_IS_INTERFACE (param->constraints [0])) {
		klass->parent = param->constraints [0];
		pos++;
	} else if (param->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT)
		klass->parent = mono_class_from_name (mono_defaults.corlib, "System", "ValueType");
	else
		klass->parent = mono_defaults.object_class;

	if (count - pos > 0) {
		klass->interface_count = count - pos;
		klass->interfaces = g_new0 (MonoClass *, count - pos);
		for (i = pos; i < count; i++)
			klass->interfaces [i - pos] = param->constraints [i];
	}

	g_assert (param->name && param->owner);

	klass->name = param->name;
	klass->name_space = "";
	klass->image = image;
	klass->inited = TRUE;
	klass->cast_class = klass->element_class = klass;
	klass->enum_basetype = &klass->element_class->byval_arg;
	klass->flags = TYPE_ATTRIBUTE_PUBLIC;

	klass->this_arg.type = klass->byval_arg.type = is_mvar ? MONO_TYPE_MVAR : MONO_TYPE_VAR;
	klass->this_arg.data.generic_param = klass->byval_arg.data.generic_param = param;
	klass->this_arg.byref = TRUE;

	klass->generic_container = param->owner;

	mono_class_setup_supertypes (klass);

	return klass;
}

static MonoClass *
my_mono_class_from_generic_parameter (MonoGenericParam *param, gboolean is_mvar)
{
	MonoClass *klass;

	if (param->pklass)
		return param->pklass;

	klass = g_new0 (MonoClass, 1);

	if (param->name)
		klass->name = param->name;
	else
		klass->name = g_strdup_printf (is_mvar ? "!!%d" : "!%d", param->num);
	klass->name_space = "";
	klass->image = mono_defaults.corlib;
	klass->inited = TRUE;
	klass->cast_class = klass->element_class = klass;
	klass->enum_basetype = &klass->element_class->byval_arg;
	klass->flags = TYPE_ATTRIBUTE_PUBLIC;

	klass->this_arg.type = klass->byval_arg.type = is_mvar ? MONO_TYPE_MVAR : MONO_TYPE_VAR;
	klass->this_arg.data.generic_param = klass->byval_arg.data.generic_param = param;
	klass->this_arg.byref = TRUE;

	klass->generic_container = param->owner;

	mono_class_setup_supertypes (klass);

	return klass;
}

MonoClass *
mono_ptr_class_get (MonoType *type)
{
	MonoClass *result;
	MonoClass *el_class;
	static GHashTable *ptr_hash = NULL;

	mono_loader_lock ();

	if (!ptr_hash)
		ptr_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	el_class = mono_class_from_mono_type (type);
	if ((result = g_hash_table_lookup (ptr_hash, el_class))) {
		mono_loader_unlock ();
		return result;
	}
	result = g_new0 (MonoClass, 1);

	result->parent = NULL; /* no parent for PTR types */
	result->name_space = el_class->name_space;
	result->name = g_strdup_printf ("%s*", el_class->name);
	result->image = el_class->image;
	result->inited = TRUE;
	result->flags = TYPE_ATTRIBUTE_CLASS | (el_class->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK);
	/* Can pointers get boxed? */
	result->instance_size = sizeof (gpointer);
	result->cast_class = result->element_class = el_class;
	result->enum_basetype = &result->element_class->byval_arg;
	result->blittable = TRUE;

	result->this_arg.type = result->byval_arg.type = MONO_TYPE_PTR;
	result->this_arg.data.type = result->byval_arg.data.type = result->enum_basetype;
	result->this_arg.byref = TRUE;

	mono_class_setup_supertypes (result);

	g_hash_table_insert (ptr_hash, el_class, result);

	mono_loader_unlock ();

	return result;
}

static MonoClass *
mono_fnptr_class_get (MonoMethodSignature *sig)
{
	MonoClass *result;
	static GHashTable *ptr_hash = NULL;

	mono_loader_lock ();

	if (!ptr_hash)
		ptr_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	
	if ((result = g_hash_table_lookup (ptr_hash, sig))) {
		mono_loader_unlock ();
		return result;
	}
	result = g_new0 (MonoClass, 1);

	result->parent = NULL; /* no parent for PTR types */
	result->name = "System";
	result->name_space = "MonoFNPtrFakeClass";
	result->image = NULL; /* need to fix... */
	result->inited = TRUE;
	result->flags = TYPE_ATTRIBUTE_CLASS; /* | (el_class->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK); */
	/* Can pointers get boxed? */
	result->instance_size = sizeof (gpointer);
	result->cast_class = result->element_class = result;
	result->blittable = TRUE;

	result->this_arg.type = result->byval_arg.type = MONO_TYPE_FNPTR;
	result->this_arg.data.method = result->byval_arg.data.method = sig;
	result->this_arg.byref = TRUE;
	result->enum_basetype = &result->element_class->byval_arg;
	result->blittable = TRUE;

	mono_class_setup_supertypes (result);

	g_hash_table_insert (ptr_hash, sig, result);

	mono_loader_unlock ();

	return result;
}

MonoClass *
mono_class_from_mono_type (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_OBJECT:
		return type->data.klass? type->data.klass: mono_defaults.object_class;
	case MONO_TYPE_VOID:
		return type->data.klass? type->data.klass: mono_defaults.void_class;
	case MONO_TYPE_BOOLEAN:
		return type->data.klass? type->data.klass: mono_defaults.boolean_class;
	case MONO_TYPE_CHAR:
		return type->data.klass? type->data.klass: mono_defaults.char_class;
	case MONO_TYPE_I1:
		return type->data.klass? type->data.klass: mono_defaults.sbyte_class;
	case MONO_TYPE_U1:
		return type->data.klass? type->data.klass: mono_defaults.byte_class;
	case MONO_TYPE_I2:
		return type->data.klass? type->data.klass: mono_defaults.int16_class;
	case MONO_TYPE_U2:
		return type->data.klass? type->data.klass: mono_defaults.uint16_class;
	case MONO_TYPE_I4:
		return type->data.klass? type->data.klass: mono_defaults.int32_class;
	case MONO_TYPE_U4:
		return type->data.klass? type->data.klass: mono_defaults.uint32_class;
	case MONO_TYPE_I:
		return type->data.klass? type->data.klass: mono_defaults.int_class;
	case MONO_TYPE_U:
		return type->data.klass? type->data.klass: mono_defaults.uint_class;
	case MONO_TYPE_I8:
		return type->data.klass? type->data.klass: mono_defaults.int64_class;
	case MONO_TYPE_U8:
		return type->data.klass? type->data.klass: mono_defaults.uint64_class;
	case MONO_TYPE_R4:
		return type->data.klass? type->data.klass: mono_defaults.single_class;
	case MONO_TYPE_R8:
		return type->data.klass? type->data.klass: mono_defaults.double_class;
	case MONO_TYPE_STRING:
		return type->data.klass? type->data.klass: mono_defaults.string_class;
	case MONO_TYPE_TYPEDBYREF:
		return type->data.klass? type->data.klass: mono_defaults.typed_reference_class;
	case MONO_TYPE_ARRAY:
		return mono_bounded_array_class_get (type->data.array->eklass, type->data.array->rank, TRUE);
	case MONO_TYPE_PTR:
		return mono_ptr_class_get (type->data.type);
	case MONO_TYPE_FNPTR:
		return mono_fnptr_class_get (type->data.method);
	case MONO_TYPE_SZARRAY:
		return mono_array_class_get (type->data.klass, 1);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		return type->data.klass;
	case MONO_TYPE_GENERICINST: {
		MonoInflatedGenericClass *gclass;
		gclass = mono_get_inflated_generic_class (type->data.generic_class);
		g_assert (gclass->klass);
		return gclass->klass;
	}
	case MONO_TYPE_VAR:
		return my_mono_class_from_generic_parameter (type->data.generic_param, FALSE);
	case MONO_TYPE_MVAR:
		return my_mono_class_from_generic_parameter (type->data.generic_param, TRUE);
	default:
		g_warning ("implement me 0x%02x\n", type->type);
		g_assert_not_reached ();
	}
	
	return NULL;
}

/**
 * @image: context where the image is created
 * @type_spec:  typespec token
 */
static MonoClass *
mono_class_create_from_typespec (MonoImage *image, guint32 type_spec,
				 MonoGenericContext *context)
{
	MonoType *type, *inflated;
	MonoClass *class;

	type = mono_type_create_from_typespec_full (image, context, type_spec);

	switch (type->type) {
	case MONO_TYPE_ARRAY:
		class = mono_bounded_array_class_get (type->data.array->eklass, type->data.array->rank, TRUE);
		break;
	case MONO_TYPE_SZARRAY:
		class = mono_array_class_get (type->data.klass, 1);
		break;
	case MONO_TYPE_PTR:
		class = mono_ptr_class_get (type->data.type);
		break;
	case MONO_TYPE_GENERICINST: {
		MonoInflatedGenericClass *gclass;
		gclass = mono_get_inflated_generic_class (type->data.generic_class);
		g_assert (gclass->klass);
		class = gclass->klass;
		break;
	}
	default:
		/* it seems any type can be stored in TypeSpec as well */
		class = mono_class_from_mono_type (type);
		break;
	}

	if (!class || !context || (!context->gclass && !context->gmethod))
		return class;

	inflated = mono_class_inflate_generic_type (&class->byval_arg, context);

	return mono_class_from_mono_type (inflated);
}

/**
 * mono_bounded_array_class_get:
 * @element_class: element class 
 * @rank: the dimension of the array class
 * @bounded: whenever the array has non-zero bounds
 *
 * Returns: a class object describing the array with element type @element_type and 
 * dimension @rank. 
 */
MonoClass *
mono_bounded_array_class_get (MonoClass *eclass, guint32 rank, gboolean bounded)
{
	MonoImage *image;
	MonoClass *class;
	MonoClass *parent = NULL;
	GSList *list, *rootlist;
	int nsize;
	char *name;
	gboolean corlib_type = FALSE;

	g_assert (rank <= 255);

	if (rank > 1)
		/* bounded only matters for one-dimensional arrays */
		bounded = FALSE;

	image = eclass->image;

	mono_loader_lock ();

	if ((rootlist = list = g_hash_table_lookup (image->array_cache, eclass))) {
		for (; list; list = list->next) {
			class = list->data;
			if ((class->rank == rank) && (class->byval_arg.type == (bounded ? MONO_TYPE_ARRAY : MONO_TYPE_SZARRAY))) {
				mono_loader_unlock ();
				return class;
			}
		}
	}

	/* for the building corlib use System.Array from it */
	if (image->assembly && image->assembly->dynamic && image->assembly_name && strcmp (image->assembly_name, "mscorlib") == 0) {
		parent = mono_class_from_name (image, "System", "Array");
		corlib_type = TRUE;
	} else if (mono_defaults.generic_array_class) {
		MonoType *inflated, **args;

		args = g_new0 (MonoType *, 1);
		args [0] = &eclass->byval_arg;

		inflated = mono_class_bind_generic_parameters (
			&mono_defaults.generic_array_class->byval_arg, 1, args);
		parent = mono_class_from_mono_type (inflated);

		if (!parent->inited)
			mono_class_init (parent);
	} else {
		parent = mono_defaults.array_class;
		if (!parent->inited)
			mono_class_init (parent);
	}

	class = g_malloc0 (sizeof (MonoClass));

	class->image = image;
	class->name_space = eclass->name_space;
	nsize = strlen (eclass->name);
	name = g_malloc (nsize + 2 + rank);
	memcpy (name, eclass->name, nsize);
	name [nsize] = '[';
	if (rank > 1)
		memset (name + nsize + 1, ',', rank - 1);
	name [nsize + rank] = ']';
	name [nsize + rank + 1] = 0;
	class->name = name;
	class->type_token = 0;
	/* all arrays are marked serializable and sealed, bug #42779 */
	class->flags = TYPE_ATTRIBUTE_CLASS | TYPE_ATTRIBUTE_SERIALIZABLE | TYPE_ATTRIBUTE_SEALED |
		(eclass->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK);
	class->parent = parent;
	class->instance_size = mono_class_instance_size (class->parent);
	class->class_size = 0;
	mono_class_setup_supertypes (class);
	if (eclass->generic_class)
		mono_class_init (eclass);
	if (!eclass->size_inited)
		mono_class_setup_fields (eclass);
	class->has_references = MONO_TYPE_IS_REFERENCE (&eclass->byval_arg) || eclass->has_references? TRUE: FALSE;

	class->rank = rank;
	
	if (eclass->enumtype)
		class->cast_class = eclass->element_class;
	else
		class->cast_class = eclass;

	class->element_class = eclass;

	if ((rank > 1) || bounded) {
		MonoArrayType *at = g_new0 (MonoArrayType, 1);
		class->byval_arg.type = MONO_TYPE_ARRAY;
		class->byval_arg.data.array = at;
		at->eklass = eclass;
		at->rank = rank;
		/* FIXME: complete.... */
	} else {
		class->byval_arg.type = MONO_TYPE_SZARRAY;
		class->byval_arg.data.klass = eclass;
	}
	class->this_arg = class->byval_arg;
	class->this_arg.byref = 1;
	if (corlib_type) {
		class->inited = 1;
	}

	class->generic_container = eclass->generic_container;

	list = g_slist_append (rootlist, class);
	g_hash_table_insert (image->array_cache, eclass, list);

	mono_loader_unlock ();

	return class;
}

/**
 * mono_array_class_get:
 * @element_class: element class 
 * @rank: the dimension of the array class
 *
 * Returns: a class object describing the array with element type @element_type and 
 * dimension @rank. 
 */
MonoClass *
mono_array_class_get (MonoClass *eclass, guint32 rank)
{
	return mono_bounded_array_class_get (eclass, rank, FALSE);
}

/**
 * mono_class_instance_size:
 * @klass: a class 
 * 
 * Returns: the size of an object instance
 */
gint32
mono_class_instance_size (MonoClass *klass)
{	
	if (!klass->size_inited)
		mono_class_init (klass);

	return klass->instance_size;
}

/**
 * mono_class_min_align:
 * @klass: a class 
 * 
 * Returns: minimm alignment requirements 
 */
gint32
mono_class_min_align (MonoClass *klass)
{	
	if (!klass->size_inited)
		mono_class_init (klass);

	return klass->min_align;
}

/**
 * mono_class_value_size:
 * @klass: a class 
 *
 * This function is used for value types, and return the
 * space and the alignment to store that kind of value object.
 *
 * Returns: the size of a value of kind @klass
 */
gint32
mono_class_value_size      (MonoClass *klass, guint32 *align)
{
	gint32 size;

	/* fixme: check disable, because we still have external revereces to
	 * mscorlib and Dummy Objects 
	 */
	/*g_assert (klass->valuetype);*/

	size = mono_class_instance_size (klass) - sizeof (MonoObject);

	if (align)
		*align = klass->min_align;

	return size;
}

/**
 * mono_class_data_size:
 * @klass: a class 
 * 
 * Returns: the size of the static class data
 */
gint32
mono_class_data_size (MonoClass *klass)
{	
	if (!klass->inited)
		mono_class_init (klass);

	return klass->class_size;
}

/*
 * Auxiliary routine to mono_class_get_field
 *
 * Takes a field index instead of a field token.
 */
static MonoClassField *
mono_class_get_field_idx (MonoClass *class, int idx)
{
	mono_class_setup_fields_locking (class);

	while (class) {
		if (class->field.count) {
			if ((idx >= class->field.first) && (idx < class->field.first + class->field.count)){
				return &class->fields [idx - class->field.first];
			}
		}
		class = class->parent;
	}
	return NULL;
}

/**
 * mono_class_get_field:
 * @class: the class to lookup the field.
 * @field_token: the field token
 *
 * Returns: A MonoClassField representing the type and offset of
 * the field, or a NULL value if the field does not belong to this
 * class.
 */
MonoClassField *
mono_class_get_field (MonoClass *class, guint32 field_token)
{
	int idx = mono_metadata_token_index (field_token);

	g_assert (mono_metadata_token_code (field_token) == MONO_TOKEN_FIELD_DEF);

	return mono_class_get_field_idx (class, idx - 1);
}

/**
 * mono_class_get_field_from_name:
 * @klass: the class to lookup the field.
 * @name: the field name
 *
 * Search the class @klass and it's parents for a field with the name @name.
 * 
 * Returns: the MonoClassField pointer of the named field or NULL
 */
MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name)
{
	int i;

	mono_class_setup_fields_locking (klass);
	while (klass) {
		for (i = 0; i < klass->field.count; ++i) {
			if (strcmp (name, klass->fields [i].name) == 0)
				return &klass->fields [i];
		}
		klass = klass->parent;
	}
	return NULL;
}

/**
 * mono_class_get_field_token:
 * @field: the field we need the token of
 *
 * Get the token of a field. Note that the tokesn is only valid for the image
 * the field was loaded from. Don't use this function for fields in dynamic types.
 * 
 * Returns: the token representing the field in the image it was loaded from.
 */
guint32
mono_class_get_field_token (MonoClassField *field)
{
	MonoClass *klass = field->parent;
	int i;

	mono_class_setup_fields_locking (klass);
	while (klass) {
		for (i = 0; i < klass->field.count; ++i) {
			if (&klass->fields [i] == field)
				return mono_metadata_make_token (MONO_TABLE_FIELD, klass->field.first + i + 1);
		}
		klass = klass->parent;
	}

	g_assert_not_reached ();
	return 0;
}

guint32
mono_class_get_event_token (MonoEvent *event)
{
	MonoClass *klass = event->parent;
	int i;

	while (klass) {
		for (i = 0; i < klass->event.count; ++i) {
			if (&klass->events [i] == event)
				return mono_metadata_make_token (MONO_TABLE_EVENT, klass->event.first + i + 1);
		}
		klass = klass->parent;
	}

	g_assert_not_reached ();
	return 0;
}

void *
mono_vtable_get_static_field_data (MonoVTable *vt)
{
	return vt->data;
}

MonoProperty*
mono_class_get_property_from_name (MonoClass *klass, const char *name)
{
	while (klass) {
		MonoProperty* p;
		gpointer iter = NULL;
		while ((p = mono_class_get_properties (klass, &iter))) {
			if (! strcmp (name, p->name))
				return p;
		}
		klass = klass->parent;
	}
	return NULL;
}

guint32
mono_class_get_property_token (MonoProperty *prop)
{
	MonoClass *klass = prop->parent;
	while (klass) {
		MonoProperty* p;
		int i = 0;
		gpointer iter = NULL;
		while ((p = mono_class_get_properties (klass, &iter))) {
			if (&klass->properties [i] == prop)
				return mono_metadata_make_token (MONO_TABLE_PROPERTY, klass->property.first + i + 1);
			
			i ++;
		}
		klass = klass->parent;
	}

	g_assert_not_reached ();
	return 0;
}

char *
mono_class_name_from_token (MonoImage *image, guint32 type_token)
{
	const char *name, *nspace;
	if (image->dynamic)
		return g_strdup_printf ("DynamicType 0x%08x", type_token);
	
	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF: {
		guint32 cols [MONO_TYPEDEF_SIZE];
		MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];
		guint tidx = mono_metadata_token_index (type_token);

		mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);
		name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);
		if (strlen (nspace) == 0)
			return g_strdup_printf ("%s", name);
		else
			return g_strdup_printf ("%s.%s", nspace, name);
	}

	case MONO_TOKEN_TYPE_REF: {
		guint32 cols [MONO_TYPEREF_SIZE];
		MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];

		mono_metadata_decode_row (t, (type_token&0xffffff)-1, cols, MONO_TYPEREF_SIZE);
		name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);
		if (strlen (nspace) == 0)
			return g_strdup_printf ("%s", name);
		else
			return g_strdup_printf ("%s.%s", nspace, name);
	}
		
	case MONO_TOKEN_TYPE_SPEC:
		return g_strdup_printf ("Typespec 0x%08x", type_token);
	default:
		g_assert_not_reached ();
	}

	return NULL;
}

static char *
mono_assembly_name_from_token (MonoImage *image, guint32 type_token)
{
	if (image->dynamic)
		return g_strdup_printf ("DynamicAssembly %s", image->name);
	
	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF:
		return mono_stringify_assembly_name (&image->assembly->aname);
		break;
	case MONO_TOKEN_TYPE_REF: {
		MonoAssemblyName aname;
		guint32 cols [MONO_TYPEREF_SIZE];
		MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
		guint32 idx;
	
		mono_metadata_decode_row (t, (type_token&0xffffff)-1, cols, MONO_TYPEREF_SIZE);

		idx = cols [MONO_TYPEREF_SCOPE] >> MONO_RESOLTION_SCOPE_BITS;
		switch (cols [MONO_TYPEREF_SCOPE] & MONO_RESOLTION_SCOPE_MASK) {
		case MONO_RESOLTION_SCOPE_MODULE:
			/* FIXME: */
			return g_strdup ("");
		case MONO_RESOLTION_SCOPE_MODULEREF:
			/* FIXME: */
			return g_strdup ("");
		case MONO_RESOLTION_SCOPE_TYPEREF:
			/* FIXME: */
			return g_strdup ("");
		case MONO_RESOLTION_SCOPE_ASSEMBLYREF:
			mono_assembly_get_assemblyref (image, idx - 1, &aname);
			return mono_stringify_assembly_name (&aname);
		default:
			g_assert_not_reached ();
		}
		break;
	}
	case MONO_TOKEN_TYPE_SPEC:
		/* FIXME: */
		return g_strdup ("");
	default:
		g_assert_not_reached ();
	}

	return NULL;
}

/**
 * mono_class_get:
 * @image: the image where the class resides
 * @type_token: the token for the class
 * @at: an optional pointer to return the array element type
 *
 * Returns: the MonoClass that represents @type_token in @image
 */
static MonoClass *
_mono_class_get (MonoImage *image, guint32 type_token, MonoGenericContext *context)
{
	MonoClass *class = NULL;

	if (image->dynamic)
		return mono_lookup_dynamic_token (image, type_token);

	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF:
		class = mono_class_create_from_typedef (image, type_token);
		break;		
	case MONO_TOKEN_TYPE_REF:
		class = mono_class_from_typeref (image, type_token);
		break;
	case MONO_TOKEN_TYPE_SPEC:
		class = mono_class_create_from_typespec (image, type_token, context);
		break;
	default:
		g_warning ("unknown token type %x", type_token & 0xff000000);
		g_assert_not_reached ();
	}

	if (!class){
		char *name = mono_class_name_from_token (image, type_token);
		char *assembly = mono_assembly_name_from_token (image, type_token);
		mono_loader_set_error_type_load (name, assembly);
	}

	return class;
}

MonoClass *
mono_class_get (MonoImage *image, guint32 type_token)
{
	return _mono_class_get (image, type_token, NULL);
}

MonoClass *
mono_class_get_full (MonoImage *image, guint32 type_token, MonoGenericContext *context)
{
	MonoClass *class = _mono_class_get (image, type_token, context);
	MonoType *inflated;

	if (!class || !context || (!context->gclass && !context->gmethod))
		return class;

	switch (class->byval_arg.type) {
	case MONO_TYPE_GENERICINST:
		if (!class->generic_class->inst->is_open)
			return class;
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		break;
	default:
		return class;
	}

	inflated = inflate_generic_type (&class->byval_arg, context);
	if (!inflated)
		return class;

	return mono_class_from_mono_type (inflated);
}

typedef struct {
	gconstpointer key;
	gpointer value;
} FindUserData;

static void
find_nocase (gpointer key, gpointer value, gpointer user_data)
{
	char *name = (char*)key;
	FindUserData *data = (FindUserData*)user_data;

	if (!data->value && (g_strcasecmp (name, (char*)data->key) == 0))
		data->value = value;
}

/**
 * mono_class_from_name_case:
 * @image: The MonoImage where the type is looked up in, or NULL for looking up in all loaded assemblies
 * @name_space: the type namespace
 * @name: the type short name.
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.   The namespace and name
 * lookups are case insensitive.
 *
 * You can also pass @NULL to the image, and that will lookup for
 * a type with the given namespace and name in all of the loaded
 * assemblies: notice that since there might be a name clash in this
 * case, passing @NULL is not encouraged if you need a precise type.
 *
 */
MonoClass *
mono_class_from_name_case (MonoImage *image, const char* name_space, const char *name)
{
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	const char *n;
	const char *nspace;
	guint32 i, visib;

	if (image->dynamic) {
		guint32 token = 0;
		FindUserData user_data;

		mono_loader_lock ();

		user_data.key = name_space;
		user_data.value = NULL;
		g_hash_table_foreach (image->name_cache, find_nocase, &user_data);

		if (user_data.value) {
			GHashTable *nspace_table = (GHashTable*)user_data.value;

			user_data.key = name;
			user_data.value = NULL;

			g_hash_table_foreach (nspace_table, find_nocase, &user_data);
			
			if (user_data.value)
				token = GPOINTER_TO_UINT (user_data.value);
		}

		mono_loader_unlock ();
		
		if (token)
			return mono_class_get (image, MONO_TOKEN_TYPE_DEF | token);
		else
			return NULL;

	}

	/* add a cache if needed */
	for (i = 1; i <= t->rows; ++i) {
		mono_metadata_decode_row (t, i - 1, cols, MONO_TYPEDEF_SIZE);
		/* nested types are accessed from the nesting name */
		visib = cols [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if (visib > TYPE_ATTRIBUTE_PUBLIC && visib <= TYPE_ATTRIBUTE_NESTED_ASSEMBLY)
			continue;
		n = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);
		if (g_strcasecmp (n, name) == 0 && g_strcasecmp (nspace, name_space) == 0)
			return mono_class_get (image, MONO_TOKEN_TYPE_DEF | i);
	}
	return NULL;
}

static MonoClass*
return_nested_in (MonoClass *class, char *nested) {
	MonoClass *found;
	char *s = strchr (nested, '/');
	GList *tmp;

	if (s) {
		*s = 0;
		s++;
	}
	for (tmp = class->nested_classes; tmp; tmp = tmp->next) {
		found = tmp->data;
		if (strcmp (found->name, nested) == 0) {
			if (s)
				return return_nested_in (found, s);
			return found;
		}
	}
	return NULL;
}


/**
 * mono_class_from_name:
 * @image: The MonoImage where the type is looked up in, or NULL for looking up in all loaded assemblies
 * @name_space: the type namespace
 * @name: the type short name.
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.   
 *
 * You can also pass `NULL' to the image, and that will lookup for
 * a type with the given namespace and name in all of the loaded
 * assemblies: notice that since there might be a name clash in this
 * case, passing NULL is not encouraged if you need a precise type.
 *
 */
MonoClass *
mono_class_from_name (MonoImage *image, const char* name_space, const char *name)
{
	GHashTable *nspace_table;
	MonoImage *loaded_image;
	guint32 token = 0;
	MonoClass *class;
	char *nested;
	char buf [1024];

	if ((nested = strchr (name, '/'))) {
		int pos = nested - name;
		int len = strlen (name);
		if (len > 1023)
			return NULL;
		memcpy (buf, name, len + 1);
		buf [pos] = 0;
		nested = buf + pos + 1;
		name = buf;
	}

	mono_loader_lock ();

	nspace_table = g_hash_table_lookup (image->name_cache, name_space);

	if (nspace_table)
		token = GPOINTER_TO_UINT (g_hash_table_lookup (nspace_table, name));

	mono_loader_unlock ();

	if (!token)
		return NULL;

	if (mono_metadata_token_table (token) == MONO_TABLE_EXPORTEDTYPE) {
		MonoTableInfo  *t = &image->tables [MONO_TABLE_EXPORTEDTYPE];
		guint32 cols [MONO_EXP_TYPE_SIZE];
		guint32 idx, impl;

		idx = mono_metadata_token_index (token);

		mono_metadata_decode_row (t, idx - 1, cols, MONO_EXP_TYPE_SIZE);

		impl = cols [MONO_EXP_TYPE_IMPLEMENTATION];
		if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_FILE) {
			loaded_image = mono_assembly_load_module (image->assembly, impl >> MONO_IMPLEMENTATION_BITS);
			if (!loaded_image)
				return NULL;
			class = mono_class_from_name (loaded_image, name_space, name);
			if (nested)
				return return_nested_in (class, nested);
			return class;
		} else {
			g_error ("not yet implemented");
		}
	}

	token = MONO_TOKEN_TYPE_DEF | token;

	class = mono_class_get (image, token);
	if (nested)
		return return_nested_in (class, nested);
	return class;
}

gboolean
mono_class_is_subclass_of (MonoClass *klass, MonoClass *klassc, 
			   gboolean check_interfaces)
{
 again:
	g_assert (klassc->idepth > 0);
	if (check_interfaces && MONO_CLASS_IS_INTERFACE (klassc) && !MONO_CLASS_IS_INTERFACE (klass)) {
		if ((klassc->interface_id <= klass->max_interface_id) &&
			(klass->interface_offsets [klassc->interface_id] >= 0))
			return TRUE;
	} else if (check_interfaces && MONO_CLASS_IS_INTERFACE (klassc) && MONO_CLASS_IS_INTERFACE (klass)) {
		int i;

		for (i = 0; i < klass->interface_count; i ++) {
			MonoClass *ic =  klass->interfaces [i];
			if (ic == klassc)
				return TRUE;
		}
	} else {
		if (!MONO_CLASS_IS_INTERFACE (klass) && mono_class_has_parent (klass, klassc))
			return TRUE;
	}

	/* 
	 * MS.NET thinks interfaces are a subclass of Object, so we think it as
	 * well.
	 */
	if (klassc == mono_defaults.object_class)
		return TRUE;

	if (klass->generic_class && klass->generic_class->is_dynamic) {
		MonoDynamicGenericClass *dgclass = (MonoDynamicGenericClass *) klass->generic_class;

		if (!dgclass->parent)
			return FALSE;

		if (mono_metadata_type_equal (dgclass->parent, &klassc->byval_arg))
			return TRUE;
		klass = mono_class_from_mono_type (dgclass->parent);
		goto again;
	}
	
	return FALSE;
}

gboolean
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass)
{
	if (!klass->inited)
		mono_class_init (klass);

	if (!oklass->inited)
		mono_class_init (oklass);

	if (klass->generic_class)
		klass = klass->generic_class->container_class;
	if (oklass->generic_class)
		oklass = oklass->generic_class->container_class;

	if (MONO_CLASS_IS_INTERFACE (klass)) {
		if ((oklass->byval_arg.type == MONO_TYPE_VAR) || (oklass->byval_arg.type == MONO_TYPE_MVAR))
			return FALSE;

		/* interface_offsets might not be set for dynamic classes */
		if (oklass->reflection_info && !oklass->interface_offsets)
			/* 
			 * oklass might be a generic type parameter but they have 
			 * interface_offsets set.
			 */
 			return mono_reflection_call_is_assignable_to (oklass, klass);

		if ((klass->interface_id <= oklass->max_interface_id) &&
		    (oklass->interface_offsets [klass->interface_id] != -1))
			return TRUE;
	} else
		if (klass->rank) {
			MonoClass *eclass, *eoclass;

			if (oklass->rank != klass->rank)
				return FALSE;

			/* vectors vs. one dimensional arrays */
			if (oklass->byval_arg.type != klass->byval_arg.type)
				return FALSE;

			eclass = klass->cast_class;
			eoclass = oklass->cast_class;


			/* 
			 * a is b does not imply a[] is b[] when a is a valuetype, and
			 * b is a reference type.
			 */

			if (eoclass->valuetype) {
				if ((eclass == mono_defaults.enum_class) || 
					(eclass == mono_defaults.enum_class->parent) ||
					(eclass == mono_defaults.object_class))
					return FALSE;
			}

			return mono_class_is_assignable_from (klass->cast_class, oklass->cast_class);
		}
	else
		if (klass == mono_defaults.object_class)
			return TRUE;

	/*
	 * Custom version of mono_class_has_parent (oklass, klass)
	 */
	if (oklass->idepth >= klass->idepth) {
		MonoClass *parent = oklass->supertypes [klass->idepth - 1];

		if (parent->generic_class)
			parent = parent->generic_class->container_class;

		return klass == parent;
	}

	return FALSE;
}	

/*
 * mono_class_get_cctor:
 *
 *   Returns the static constructor of @klass if it exists, NULL otherwise.
 */
MonoMethod*
mono_class_get_cctor (MonoClass *klass)
{
	MonoCachedClassInfo cached_info;

	if (!klass->has_cctor)
		return NULL;

	if (mono_class_get_cached_class_info (klass, &cached_info))
		return mono_get_method (klass->image, cached_info.cctor_token, klass);

	return mono_class_get_method_from_name_flags (klass, ".cctor", -1, METHOD_ATTRIBUTE_SPECIAL_NAME);
}

/*
 * mono_class_get_finalizer:
 *
 *   Returns the finalizer method of @klass if it exists, NULL otherwise.
 */
MonoMethod*
mono_class_get_finalizer (MonoClass *klass)
{
	MonoCachedClassInfo cached_info;
	static int finalize_slot = -1;

	if (finalize_slot < 0) {
		int i;
		MonoClass* obj_class = mono_get_object_class ();
		mono_class_setup_vtable (obj_class);
		for (i = 0; i < obj_class->vtable_size; ++i) {
			MonoMethod *cm = obj_class->vtable [i];
	       
			if (!strcmp (mono_method_get_name (cm), "Finalize")) {
				finalize_slot = i;
				break;
			}
		}
	}

	if (!klass->has_finalize)
		return NULL;

	if (mono_class_get_cached_class_info (klass, &cached_info))
		return mono_get_method (cached_info.finalize_image, cached_info.finalize_token, NULL);
	else {
		mono_class_setup_vtable (klass);
		return klass->vtable [finalize_slot];
	}
}

/*
 * mono_class_needs_cctor_run:
 *
 *  Determines whenever the class has a static constructor and whenever it
 * needs to be called when executing CALLER.
 */
gboolean
mono_class_needs_cctor_run (MonoClass *klass, MonoMethod *caller)
{
	MonoMethod *method;

	method = mono_class_get_cctor (klass);
	if (method)
		return (method == caller) ? FALSE : TRUE;
	else
		return TRUE;
}

/**
 * mono_class_array_element_size:
 * @klass: 
 *
 * Returns: the number of bytes an element of type @klass
 * uses when stored into an array.
 */
gint32
mono_class_array_element_size (MonoClass *klass)
{
	MonoType *type = &klass->byval_arg;
	
handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return 1;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return 2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		return 4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY: 
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:   
		return sizeof (gpointer);
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		return 8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			klass = klass->element_class;
			goto handle_enum;
		}
		return mono_class_instance_size (klass) - sizeof (MonoObject);
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in mono_class_array_element_size", type->type);
	}
	return -1;
}

/**
 * mono_array_element_size:
 * @ac: pointer to a #MonoArrayClass
 *
 * Returns: the size of single array element.
 */
gint32
mono_array_element_size (MonoClass *ac)
{
	return mono_class_array_element_size (ac->element_class);
}

gpointer
mono_ldtoken (MonoImage *image, guint32 token, MonoClass **handle_class,
	      MonoGenericContext *context)
{
	if (image->dynamic) {
		MonoClass *tmp_handle_class;
		gpointer obj = mono_lookup_dynamic_token_class (image, token, &tmp_handle_class);

		g_assert (tmp_handle_class);
		if (handle_class)
			*handle_class = tmp_handle_class;

		if (tmp_handle_class == mono_defaults.typehandle_class)
			return &((MonoClass*)obj)->byval_arg;
		else
			return obj;
	}

	switch (token & 0xff000000) {
	case MONO_TOKEN_TYPE_DEF:
	case MONO_TOKEN_TYPE_REF: {
		MonoClass *class;
		if (handle_class)
			*handle_class = mono_defaults.typehandle_class;
		class = mono_class_get_full (image, token, context);
		if (!class)
			return NULL;
		mono_class_init (class);
		/* We return a MonoType* as handle */
		return &class->byval_arg;
	}
	case MONO_TOKEN_TYPE_SPEC: {
		MonoClass *class;
		if (handle_class)
			*handle_class = mono_defaults.typehandle_class;
		class = mono_class_create_from_typespec (image, token, context);
		if (!class)
			return NULL;
		mono_class_init (class);
		return &class->byval_arg;
	}
	case MONO_TOKEN_FIELD_DEF: {
		MonoClass *class;
		guint32 type = mono_metadata_typedef_from_field (image, mono_metadata_token_index (token));
		if (handle_class)
			*handle_class = mono_defaults.fieldhandle_class;
		class = mono_class_get_full (image, MONO_TOKEN_TYPE_DEF | type, context);
		if (!class)
			return NULL;
		mono_class_init (class);
		return mono_class_get_field (class, token);
	}
	case MONO_TOKEN_METHOD_DEF: {
		MonoMethod *meth;
		meth = mono_get_method_full (image, token, NULL, context);
		if (handle_class)
			*handle_class = mono_defaults.methodhandle_class;
		return meth;
	}
	case MONO_TOKEN_MEMBER_REF: {
		guint32 cols [MONO_MEMBERREF_SIZE];
		const char *sig;
		mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], mono_metadata_token_index (token) - 1, cols, MONO_MEMBERREF_SIZE);
		sig = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
		mono_metadata_decode_blob_size (sig, &sig);
		if (*sig == 0x6) { /* it's a field */
			MonoClass *klass;
			MonoClassField *field;
			field = mono_field_from_token (image, token, &klass, context);
			if (handle_class)
				*handle_class = mono_defaults.fieldhandle_class;
			return field;
		} else {
			MonoMethod *meth;
			meth = mono_get_method_full (image, token, NULL, context);
			if (handle_class)
				*handle_class = mono_defaults.methodhandle_class;
			return meth;
		}
	}
	default:
		g_warning ("Unknown token 0x%08x in ldtoken", token);
		break;
	}
	return NULL;
}

/**
 * This function might need to call runtime functions so it can't be part
 * of the metadata library.
 */
static MonoLookupDynamicToken lookup_dynamic = NULL;

void
mono_install_lookup_dynamic_token (MonoLookupDynamicToken func)
{
	lookup_dynamic = func;
}

gpointer
mono_lookup_dynamic_token (MonoImage *image, guint32 token)
{
	MonoClass *handle_class;

	return lookup_dynamic (image, token, &handle_class);
}

gpointer
mono_lookup_dynamic_token_class (MonoImage *image, guint32 token, MonoClass **handle_class)
{
	return lookup_dynamic (image, token, handle_class);
}

static MonoGetCachedClassInfo get_cached_class_info = NULL;

void
mono_install_get_cached_class_info (MonoGetCachedClassInfo func)
{
	get_cached_class_info = func;
}

static gboolean
mono_class_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res)
{
	if (!get_cached_class_info)
		return FALSE;
	else
		return get_cached_class_info (klass, res);
}

MonoImage*
mono_class_get_image (MonoClass *klass)
{
	return klass->image;
}

/**
 * mono_class_get_element_class:
 * @klass: the MonoClass to act on
 *
 * Returns: the element class of an array or an enumeration.
 */
MonoClass*
mono_class_get_element_class (MonoClass *klass)
{
	return klass->element_class;
}

/**
 * mono_class_is_valuetype:
 * @klass: the MonoClass to act on
 *
 * Returns: true if the MonoClass represents a ValueType.
 */
gboolean
mono_class_is_valuetype (MonoClass *klass)
{
	return klass->valuetype;
}

/**
 * mono_class_is_enum:
 * @klass: the MonoClass to act on
 *
 * Returns: true if the MonoClass represents an enumeration.
 */
gboolean
mono_class_is_enum (MonoClass *klass)
{
	return klass->enumtype;
}

/**
 * mono_class_enum_basetype:
 * @klass: the MonoClass to act on
 *
 * Returns: the underlying type representation for an enumeration.
 */
MonoType*
mono_class_enum_basetype (MonoClass *klass)
{
	return klass->enum_basetype;
}

/**
 * mono_class_get_parent
 * @klass: the MonoClass to act on
 *
 * Returns: the parent class for this class.
 */
MonoClass*
mono_class_get_parent (MonoClass *klass)
{
	return klass->parent;
}

/**
 * mono_class_get_nesting_type;
 * @klass: the MonoClass to act on
 *
 * Returns: the container type where this type is nested or NULL if this type is not a nested type.
 */
MonoClass*
mono_class_get_nesting_type (MonoClass *klass)
{
	return klass->nested_in;
}

/**
 * mono_class_get_rank:
 * @klass: the MonoClass to act on
 *
 * Returns: the rank for the array (the number of dimensions).
 */
int
mono_class_get_rank (MonoClass *klass)
{
	return klass->rank;
}

/**
 * mono_class_get_flags:
 * @klass: the MonoClass to act on
 *
 * The type flags from the TypeDef table from the metadata.
 * see the TYPE_ATTRIBUTE_* definitions on tabledefs.h for the
 * different values.
 *
 * Returns: the flags from the TypeDef table.
 */
guint32
mono_class_get_flags (MonoClass *klass)
{
	return klass->flags;
}

/**
 * mono_class_get_name
 * @klass: the MonoClass to act on
 *
 * Returns: the name of the class.
 */
const char*
mono_class_get_name (MonoClass *klass)
{
	return klass->name;
}

/**
 * mono_class_get_namespace:
 * @klass: the MonoClass to act on
 *
 * Returns: the namespace of the class.
 */
const char*
mono_class_get_namespace (MonoClass *klass)
{
	return klass->name_space;
}

/**
 * mono_class_get_type:
 * @klass: the MonoClass to act on
 *
 * This method returns the internal Type representation for the class.
 *
 * Returns: the MonoType from the class.
 */
MonoType*
mono_class_get_type (MonoClass *klass)
{
	return &klass->byval_arg;
}

/**
 * mono_class_get_byref_type:
 * @klass: the MonoClass to act on
 *
 * 
 */
MonoType*
mono_class_get_byref_type (MonoClass *klass)
{
	return &klass->this_arg;
}

/**
 * mono_class_num_fields:
 * @klass: the MonoClass to act on
 *
 * Returns: the number of static and instance fields in the class.
 */
int
mono_class_num_fields (MonoClass *klass)
{
	return klass->field.count;
}

/**
 * mono_class_num_methods:
 * @klass: the MonoClass to act on
 *
 * Returns: the number of methods in the class.
 */
int
mono_class_num_methods (MonoClass *klass)
{
	return klass->method.count;
}

/**
 * mono_class_num_properties
 * @klass: the MonoClass to act on
 *
 * Returns: the number of properties in the class.
 */
int
mono_class_num_properties (MonoClass *klass)
{
	mono_class_setup_properties (klass);

	return klass->property.count;
}

/**
 * mono_class_num_events:
 * @klass: the MonoClass to act on
 *
 * Returns: the number of events in the class.
 */
int
mono_class_num_events (MonoClass *klass)
{
	mono_class_setup_events (klass);

	return klass->event.count;
}

/**
 * mono_class_get_fields:
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the fields in a class.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a @MonoClassField* on each iteration, or NULL when no more fields are available.
 */
MonoClassField*
mono_class_get_fields (MonoClass* klass, gpointer *iter)
{
	MonoClassField* field;
	if (!iter)
		return NULL;
	mono_class_setup_fields_locking (klass);
	if (!*iter) {
		/* start from the first */
		if (klass->field.count) {
			return *iter = &klass->fields [0];
		} else {
			/* no fields */
			return NULL;
		}
	}
	field = *iter;
	field++;
	if (field < &klass->fields [klass->field.count]) {
		return *iter = field;
	}
	return NULL;
}

/**
 * mono_class_get_methods
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the fields in a class.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a MonoMethod on each iteration or NULL when no more methods are available.
 */
MonoMethod*
mono_class_get_methods (MonoClass* klass, gpointer *iter)
{
	MonoMethod** method;
	if (!iter)
		return NULL;
	if (!klass->inited)
		mono_class_init (klass);
	if (!*iter) {
		mono_class_setup_methods (klass);
		/* start from the first */
		if (klass->method.count) {
			*iter = &klass->methods [0];
			return klass->methods [0];
		} else {
			/* no method */
			return NULL;
		}
	}
	method = *iter;
	method++;
	if (method < &klass->methods [klass->method.count]) {
		*iter = method;
		return *method;
	}
	return NULL;
}

/**
 * mono_class_get_properties:
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the properties in a class.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a @MonoProperty* on each invocation, or NULL when no more are available.
 */
MonoProperty*
mono_class_get_properties (MonoClass* klass, gpointer *iter)
{
	MonoProperty* property;
	if (!iter)
		return NULL;
	if (!klass->inited)
		mono_class_init (klass);
	if (!*iter) {
		mono_class_setup_properties (klass);
		/* start from the first */
		if (klass->property.count) {
			return *iter = &klass->properties [0];
		} else {
			/* no fields */
			return NULL;
		}
	}
	property = *iter;
	property++;
	if (property < &klass->properties [klass->property.count]) {
		return *iter = property;
	}
	return NULL;
}

/**
 * mono_class_get_events:
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the properties in a class.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a @MonoEvent* on each invocation, or NULL when no more are available.
 */
MonoEvent*
mono_class_get_events (MonoClass* klass, gpointer *iter)
{
	MonoEvent* event;
	if (!iter)
		return NULL;
	if (!klass->inited)
		mono_class_init (klass);
	if (!*iter) {
		mono_class_setup_events (klass);
		/* start from the first */
		if (klass->event.count) {
			return *iter = &klass->events [0];
		} else {
			/* no fields */
			return NULL;
		}
	}
	event = *iter;
	event++;
	if (event < &klass->events [klass->event.count]) {
		return *iter = event;
	}
	return NULL;
}

/**
 * mono_class_get_interfaces
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the interfaces implemented by this class.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a @Monoclass* on each invocation, or NULL when no more are available.
 */
MonoClass*
mono_class_get_interfaces (MonoClass* klass, gpointer *iter)
{
	MonoClass** iface;
	if (!iter)
		return NULL;
	if (!klass->inited)
		mono_class_init (klass);
	if (!*iter) {
		/* start from the first */
		if (klass->interface_count) {
			*iter = &klass->interfaces [0];
			return klass->interfaces [0];
		} else {
			/* no interface */
			return NULL;
		}
	}
	iface = *iter;
	iface++;
	if (iface < &klass->interfaces [klass->interface_count]) {
		*iter = iface;
		return *iface;
	}
	return NULL;
}

/**
 * mono_class_get_nested_types
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the nested types of a class.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a @Monoclass* on each invocation, or NULL when no more are available.
 */
MonoClass*
mono_class_get_nested_types (MonoClass* klass, gpointer *iter)
{
	GList *item;
	if (!iter)
		return NULL;
	if (!klass->inited)
		mono_class_init (klass);
	if (!*iter) {
		/* start from the first */
		if (klass->nested_classes) {
			*iter = klass->nested_classes;
			return klass->nested_classes->data;
		} else {
			/* no nested types */
			return NULL;
		}
	}
	item = *iter;
	item = item->next;
	if (item) {
		*iter = item;
		return item->data;
	}
	return NULL;
}

/**
 * mono_field_get_name:
 * @field: the MonoClassField to act on
 *
 * Returns: the name of the field.
 */
const char*
mono_field_get_name (MonoClassField *field)
{
	return field->name;
}

/**
 * mono_field_get_type:
 * @field: the MonoClassField to act on
 *
 * Returns: MonoType of the field.
 */
MonoType*
mono_field_get_type (MonoClassField *field)
{
	return field->type;
}

/**
 * mono_field_get_type:
 * @field: the MonoClassField to act on
 *
 * Returns: MonoClass where the field was defined.
 */
MonoClass*
mono_field_get_parent (MonoClassField *field)
{
	return field->parent;
}

/**
 * mono_field_get_flags;
 * @field: the MonoClassField to act on
 *
 * The metadata flags for a field are encoded using the
 * FIELD_ATTRIBUTE_* constants.  See the tabledefs.h file for details.
 *
 * Returns: the flags for the field.
 */
guint32
mono_field_get_flags (MonoClassField *field)
{
	return field->type->attrs;
}

/**
 * mono_property_get_name: 
 * @prop: the MonoProperty to act on
 *
 * Returns: the name of the property
 */
const char*
mono_property_get_name (MonoProperty *prop)
{
	return prop->name;
}

/**
 * mono_property_get_set_method
 * @prop: the MonoProperty to act on.
 *
 * Returns: the setter method of the property (A MonoMethod)
 */
MonoMethod*
mono_property_get_set_method (MonoProperty *prop)
{
	return prop->set;
}

/**
 * mono_property_get_get_method
 * @prop: the MonoProperty to act on.
 *
 * Returns: the setter method of the property (A MonoMethod)
 */
MonoMethod*
mono_property_get_get_method (MonoProperty *prop)
{
	return prop->get;
}

/**
 * mono_property_get_parent:
 * @prop: the MonoProperty to act on.
 *
 * Returns: the MonoClass where the property was defined.
 */
MonoClass*
mono_property_get_parent (MonoProperty *prop)
{
	return prop->parent;
}

/**
 * mono_property_get_flags:
 * @prop: the MonoProperty to act on.
 *
 * The metadata flags for a property are encoded using the
 * PROPERTY_ATTRIBUTE_* constants.  See the tabledefs.h file for details.
 *
 * Returns: the flags for the property.
 */
guint32
mono_property_get_flags (MonoProperty *prop)
{
	return prop->attrs;
}

/**
 * mono_event_get_name:
 * @event: the MonoEvent to act on
 *
 * Returns: the name of the event.
 */
const char*
mono_event_get_name (MonoEvent *event)
{
	return event->name;
}

/**
 * mono_event_get_add_method:
 * @event: The MonoEvent to act on.
 *
 * Returns: the @add' method for the event (a MonoMethod).
 */
MonoMethod*
mono_event_get_add_method (MonoEvent *event)
{
	return event->add;
}

/**
 * mono_event_get_remove_method:
 * @event: The MonoEvent to act on.
 *
 * Returns: the @remove method for the event (a MonoMethod).
 */
MonoMethod*
mono_event_get_remove_method (MonoEvent *event)
{
	return event->remove;
}

/**
 * mono_event_get_raise_method:
 * @event: The MonoEvent to act on.
 *
 * Returns: the @raise method for the event (a MonoMethod).
 */
MonoMethod*
mono_event_get_raise_method (MonoEvent *event)
{
	return event->raise;
}

/**
 * mono_event_get_parent:
 * @event: the MonoEvent to act on.
 *
 * Returns: the MonoClass where the event is defined.
 */
MonoClass*
mono_event_get_parent (MonoEvent *event)
{
	return event->parent;
}

/**
 * mono_event_get_flags
 * @event: the MonoEvent to act on.
 *
 * The metadata flags for an event are encoded using the
 * EVENT_* constants.  See the tabledefs.h file for details.
 *
 * Returns: the flags for the event.
 */
guint32
mono_event_get_flags (MonoEvent *event)
{
	return event->attrs;
}

/**
 * mono_class_get_method_from_name:
 * @klass: where to look for the method
 * @name_space: name of the method
 * @param_count: number of parameters. -1 for any number.
 *
 * Obtains a MonoMethod with a given name and number of parameters.
 * It only works if there are no multiple signatures for any given method name.
 */
MonoMethod *
mono_class_get_method_from_name (MonoClass *klass, const char *name, int param_count)
{
	return mono_class_get_method_from_name_flags (klass, name, param_count, 0);
}

/**
 * mono_class_get_method_from_name_flags:
 * @klass: where to look for the method
 * @name_space: name of the method
 * @param_count: number of parameters. -1 for any number.
 * @flags: flags which must be set in the method
 *
 * Obtains a MonoMethod with a given name and number of parameters.
 * It only works if there are no multiple signatures for any given method name.
 */
MonoMethod *
mono_class_get_method_from_name_flags (MonoClass *klass, const char *name, int param_count, int flags)
{
	MonoMethod *res = NULL;
	int i;

	mono_class_init (klass);

	if (klass->methods) {
		mono_class_setup_methods (klass);
		for (i = 0; i < klass->method.count; ++i) {
			MonoMethod *method = klass->methods [i];

			if (method->name[0] == name [0] && 
				!strcmp (name, method->name) &&
				(param_count == -1 || mono_method_signature (method)->param_count == param_count) &&
				((method->flags & flags) == flags)) {
				res = method;
				break;
			}
		}
	}
	else {
		/* Search directly in the metadata to avoid calling setup_methods () */
		for (i = 0; i < klass->method.count; ++i) {
			guint32 cols [MONO_METHOD_SIZE];
			MonoMethod *method;

			mono_metadata_decode_row (&klass->image->tables [MONO_TABLE_METHOD], klass->method.first + i, cols, MONO_METHOD_SIZE);

			if (!strcmp (mono_metadata_string_heap (klass->image, cols [MONO_METHOD_NAME]), name)) {
				method = mono_get_method (klass->image, MONO_TOKEN_METHOD_DEF | (klass->method.first + i + 1), klass);
				if ((param_count == -1) || mono_method_signature (method)->param_count == param_count) {
					res = method;
					break;
				}
			}
		}
	}

	return res;
}

/**
 * mono_class_set_failure:
 * @klass: class in which the failure was detected
 * @ex_type: the kind of exception/error to be thrown (later)
 * @ex_data: exception data (specific to each type of exception/error)
 *
 * Keep a detected failure informations in the class for later processing.
 * Note that only the first failure is kept.
 */
gboolean
mono_class_set_failure (MonoClass *klass, guint32 ex_type, void *ex_data)
{
	if (klass->exception_type)
		return FALSE;
	klass->exception_type = ex_type;
	klass->exception_data = ex_data;
	return TRUE;
}

/**
 * mono_class_get_exception_for_failure:
 * @klass: class in which the failure was detected
 *
 * Return a constructed MonoException than the caller can then throw
 * using mono_raise_exception - or NULL if no failure is present (or
 * doesn't result in an exception).
 */
MonoException*
mono_class_get_exception_for_failure (MonoClass *klass)
{
	switch (klass->exception_type) {
	case MONO_EXCEPTION_SECURITY_INHERITANCEDEMAND: {
		MonoDomain *domain = mono_domain_get ();
		MonoSecurityManager* secman = mono_security_manager_get_methods ();
		MonoMethod *method = klass->exception_data;
		guint32 error = (method) ? MONO_METADATA_INHERITANCEDEMAND_METHOD : MONO_METADATA_INHERITANCEDEMAND_CLASS;
		MonoObject *exc = NULL;
		gpointer args [4];

		args [0] = &error;
		args [1] = mono_assembly_get_object (domain, mono_image_get_assembly (klass->image));
		args [2] = mono_type_get_object (domain, &klass->byval_arg);
		args [3] = (method) ? mono_method_get_object (domain, method, NULL) : NULL;

		mono_runtime_invoke (secman->inheritsecurityexception, NULL, args, &exc);
		return (MonoException*) exc;
	}
	/* TODO - handle other class related failures */
	default:
		return NULL;
	}
}
