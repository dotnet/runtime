/**
 * \file
 * Class management for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <mono/metadata/image.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/attrdefs.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/metadata-update.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-string.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/atomic.h>
#include <mono/utils/unlocked.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/checked-build.h>

MonoStats mono_stats;

/* Statistics */
extern gint32 mono_inflated_methods_size;

/* Function supplied by the runtime to find classes by name using information from the AOT file */
static MonoGetClassFromName get_class_from_name = NULL;

static gboolean can_access_type (MonoClass *access_klass, MonoClass *member_klass);

static char* mono_assembly_name_from_token (MonoImage *image, guint32 type_token);
static guint32 mono_field_resolve_flags (MonoClassField *field);

static MonoClass *
mono_class_from_name_checked_aux (MonoImage *image, const char* name_space, const char *name, GHashTable* visited_images, gboolean case_sensitive, MonoError *error);

GENERATE_GET_CLASS_WITH_CACHE (valuetype, "System", "ValueType")
GENERATE_TRY_GET_CLASS_WITH_CACHE (handleref, "System.Runtime.InteropServices", "HandleRef")

#define CTOR_REQUIRED_FLAGS (METHOD_ATTRIBUTE_SPECIAL_NAME | METHOD_ATTRIBUTE_RT_SPECIAL_NAME)
#define CTOR_INVALID_FLAGS (METHOD_ATTRIBUTE_STATIC)

// define to print types whenever custom modifiers are appended during inflation
#undef DEBUG_INFLATE_CMODS

static
MonoImage *
mono_method_get_image (MonoMethod *method)
{
	return m_class_get_image (method->klass);
}

/**
 * mono_class_from_typeref:
 * \param image a MonoImage
 * \param type_token a TypeRef token
 *
 * Creates the \c MonoClass* structure representing the type defined by
 * the typeref token valid inside \p image.
 * \returns The \c MonoClass* representing the typeref token, or NULL if it could
 * not be loaded.
 */
MonoClass *
mono_class_from_typeref (MonoImage *image, guint32 type_token)
{
	ERROR_DECL (error);
	MonoClass *klass = mono_class_from_typeref_checked (image, type_token, error);
	g_assert (is_ok (error)); /*FIXME proper error handling*/
	return klass;
}

/**
 * mono_class_from_typeref_checked:
 * \param image a MonoImage
 * \param type_token a TypeRef token
 * \param error error return code, if any.
 *
 * Creates the \c MonoClass* structure representing the type defined by
 * the typeref token valid inside \p image.
 *
 * \returns The \c MonoClass* representing the typeref token, NULL if it could
 * not be loaded with the \p error value filled with the information about the
 * error.
 */
MonoClass *
mono_class_from_typeref_checked (MonoImage *image, guint32 type_token, MonoError *error)
{
	guint32 cols [MONO_TYPEREF_SIZE];
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
	guint32 idx;
	const char *name, *nspace;
	MonoClass *res = NULL;
	MonoImage *module;

	error_init (error);

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
		res = mono_class_from_name_checked (image, nspace, name, error);
		goto done;

	case MONO_RESOLUTION_SCOPE_MODULEREF:
		module = mono_image_load_module_checked (image, idx, error);
		if (module)
			res = mono_class_from_name_checked (module, nspace, name, error);
		goto done;

	case MONO_RESOLUTION_SCOPE_TYPEREF: {
		MonoClass *enclosing;
		GList *tmp;

		if (idx == mono_metadata_token_index (type_token)) {
			mono_error_set_bad_image (error, image, "Image with self-referencing typeref token %08x.", type_token);
			return NULL;
		}

		enclosing = mono_class_from_typeref_checked (image, MONO_TOKEN_TYPE_REF | idx, error);
		return_val_if_nok (error, NULL);

		GList *nested_classes = mono_class_get_nested_classes_property (enclosing);
		if (m_class_is_nested_classes_inited (enclosing) && nested_classes) {
			/* Micro-optimization: don't scan the metadata tables if enclosing is already inited */
			for (tmp = nested_classes; tmp; tmp = tmp->next) {
				res = (MonoClass *)tmp->data;
				if (strcmp (m_class_get_name (res), name) == 0)
					return res;
			}
		} else {
			MonoImage *enclosing_image = m_class_get_image (enclosing);
			guint32 enclosing_type_token = m_class_get_type_token (enclosing);
			/* Don't call mono_class_init_internal as we might've been called by it recursively */
			int i = mono_metadata_nesting_typedef (enclosing_image, enclosing_type_token, 1);
			while (i) {
				guint32 class_nested = mono_metadata_decode_row_col (&enclosing_image->tables [MONO_TABLE_NESTEDCLASS], i - 1, MONO_NESTED_CLASS_NESTED);
				guint32 string_offset = mono_metadata_decode_row_col (&enclosing_image->tables [MONO_TABLE_TYPEDEF], class_nested - 1, MONO_TYPEDEF_NAME);
				const char *nname = mono_metadata_string_heap (enclosing_image, string_offset);

				if (strcmp (nname, name) == 0)
					return mono_class_create_from_typedef (enclosing_image, MONO_TOKEN_TYPE_DEF | class_nested, error);

				i = mono_metadata_nesting_typedef (enclosing_image, enclosing_type_token, i + 1);
			}
		}
		g_warning ("TypeRef ResolutionScope not yet handled (%d) for %s.%s in image %s", idx, nspace, name, image->name);
		goto done;
	}
	case MONO_RESOLUTION_SCOPE_ASSEMBLYREF:
		break;
	}

	if (mono_metadata_table_bounds_check (image, MONO_TABLE_ASSEMBLYREF, idx)) {
		mono_error_set_bad_image (error, image, "Image with invalid assemblyref token %08x.", idx);
		return NULL;
	}

	if (!image->references || !image->references [idx - 1])
		mono_assembly_load_reference (image, idx - 1);
	g_assert (image->references [idx - 1]);

	/* If the assembly did not load, register this as a type load exception */
	if (image->references [idx - 1] == REFERENCE_MISSING){
		MonoAssemblyName aname;
		memset (&aname, 0, sizeof (MonoAssemblyName));
		char *human_name;

		mono_assembly_get_assemblyref (image, idx - 1, &aname);
		human_name = mono_stringify_assembly_name (&aname);
		mono_error_set_simple_file_not_found (error, human_name);
		g_free (human_name);
		return NULL;
	}

	res = mono_class_from_name_checked (image->references [idx - 1]->image, nspace, name, error);

done:
	/* Generic case, should be avoided for when a better error is possible. */
	if (!res && is_ok (error)) {
		char *name = mono_class_name_from_token (image, type_token);
		char *assembly = mono_assembly_name_from_token (image, type_token);
		mono_error_set_type_load_name (error, name, assembly, "Could not resolve type with token %08x from typeref (expected class '%s' in assembly '%s')", type_token, name, assembly);
	}
	return res;
}


static void *
mono_image_memdup (MonoImage *image, void *data, guint size)
{
	void *res = mono_image_alloc (image, size);
	memcpy (res, data, size);
	return res;
}

/* Copy everything mono_metadata_free_array free. */
MonoArrayType *
mono_dup_array_type (MonoImage *image, MonoArrayType *a)
{
	if (image) {
		a = (MonoArrayType *)mono_image_memdup (image, a, sizeof (MonoArrayType));
		if (a->sizes)
			a->sizes = (int *)mono_image_memdup (image, a->sizes, a->numsizes * sizeof (int));
		if (a->lobounds)
			a->lobounds = (int *)mono_image_memdup (image, a->lobounds, a->numlobounds * sizeof (int));
	} else {
		a = (MonoArrayType *)g_memdup (a, sizeof (MonoArrayType));
		if (a->sizes)
			a->sizes = (int *)g_memdup (a->sizes, a->numsizes * sizeof (int));
		if (a->lobounds)
			a->lobounds = (int *)g_memdup (a->lobounds, a->numlobounds * sizeof (int));
	}
	return a;
}

/* Copy everything mono_metadata_free_method_signature free. */
MonoMethodSignature*
mono_metadata_signature_deep_dup (MonoImage *image, MonoMethodSignature *sig)
{
	int i;

	sig = mono_metadata_signature_dup_full (image, sig);

	sig->ret = mono_metadata_type_dup (image, sig->ret);
	for (i = 0; i < sig->param_count; ++i)
		sig->params [i] = mono_metadata_type_dup (image, sig->params [i]);

	return sig;
}

static void
_mono_type_get_assembly_name (MonoClass *klass, GString *str)
{
	MonoAssembly *ta = m_class_get_image (klass)->assembly;
	char *name;

	name = mono_stringify_assembly_name (&ta->aname);
	g_string_append_printf (str, ", %s", name);
	g_free (name);
}

static void
mono_type_name_check_byref (MonoType *type, GString *str)
{
	if (m_type_is_byref (type))
		g_string_append_c (str, '&');
}

static char*
escape_special_chars (const char* identifier)
{
	size_t id_len = strlen (identifier);
	// Assume the worst case, and thus only allocate once
	char *res = g_malloc (id_len * 2 + 1);
	char *res_ptr = res;
	for (const char *s = identifier; *s != 0; s++) {
		switch (*s) {
		case ',':
		case '+':
		case '&':
		case '*':
		case '[':
		case ']':
		case '\\':
			*res_ptr++ = '\\';
			break;
		}
		*res_ptr++ = *s;
	}
	*res_ptr = '\0';
	return res;
}

/**
 * mono_identifier_escape_type_name_chars:
 * \param identifier the display name of a mono type
 *
 * \returns The name in external form, that is with escaping backslashes.
 *
 * The displayed form of an identifier has the characters ,+&*[]\
 * that have special meaning in type names escaped with a preceeding
 * backslash (\) character.
 */
char*
mono_identifier_escape_type_name_chars (const char* identifier)
{
	if (!identifier)
		return NULL;

	// If the string has any special characters escape the whole thing, otherwise just return the input
	for (const char *s = identifier; *s != 0; s++) {
		switch (*s) {
		case ',':
		case '+':
		case '&':
		case '*':
		case '[':
		case ']':
		case '\\':
			return escape_special_chars (identifier);
		}
	}

	return g_strdup (identifier);
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
			m_class_get_byval_arg (type->data.array->eklass), str, FALSE, nested_format);
		g_string_append_c (str, '[');
		if (rank == 1)
			g_string_append_c (str, '*');
		else if (rank > 64)
			// Only taken in an error path, runtime will not load arrays of more than 32 dimensions
			g_string_append_printf (str, "%d", rank);
		else
			for (i = 1; i < rank; i++)
				g_string_append_c (str, ',');
		g_string_append_c (str, ']');

		mono_type_name_check_byref (type, str);

		if (format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)
			_mono_type_get_assembly_name (type->data.array->eklass, str);
		break;
	}
	case MONO_TYPE_SZARRAY: {
		MonoTypeNameFormat nested_format;

		nested_format = format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED ?
			MONO_TYPE_NAME_FORMAT_FULL_NAME : format;

		mono_type_get_name_recurse (
			m_class_get_byval_arg (type->data.klass), str, FALSE, nested_format);
		g_string_append (str, "[]");

		mono_type_name_check_byref (type, str);

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
		g_string_append_c (str, '*');

		mono_type_name_check_byref (type, str);

		if (format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)
			_mono_type_get_assembly_name (mono_class_from_mono_type_internal (type->data.type), str);
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (!mono_generic_param_name (type->data.generic_param))
			g_string_append_printf (str, "%s%d", type->type == MONO_TYPE_VAR ? "!" : "!!", type->data.generic_param->num);
		else
			g_string_append (str, mono_generic_param_name (type->data.generic_param));

		mono_type_name_check_byref (type, str);

		break;
	default:
		klass = mono_class_from_mono_type_internal (type);
		if (m_class_get_nested_in (klass)) {
			mono_type_get_name_recurse (
				m_class_get_byval_arg (m_class_get_nested_in (klass)), str, TRUE, format);
			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append_c (str, '.');
			else
				g_string_append_c (str, '+');
		} else if (*m_class_get_name_space (klass)) {
			const char *klass_name_space = m_class_get_name_space (klass);
			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append (str, klass_name_space);
			else {
				char *escaped = mono_identifier_escape_type_name_chars (klass_name_space);
				g_string_append (str, escaped);
				g_free (escaped);
			}
			g_string_append_c (str, '.');
		}
		const char *klass_name = m_class_get_name (klass);
		if (format == MONO_TYPE_NAME_FORMAT_IL) {
			const char *s = strchr (klass_name, '`');
			gssize len = s ? (s - klass_name) : (gssize)strlen (klass_name);
			g_string_append_len (str, klass_name, len);
		} else {
			char *escaped = mono_identifier_escape_type_name_chars (klass_name);
			g_string_append (str, escaped);
			g_free (escaped);
		}
		if (is_recursed)
			break;
		if (mono_class_is_ginst (klass)) {
			MonoGenericClass *gclass = mono_class_get_generic_class (klass);
			MonoGenericInst *inst = gclass->context.class_inst;
			MonoTypeNameFormat nested_format;
			int i;

			nested_format = format == MONO_TYPE_NAME_FORMAT_FULL_NAME ?
				MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED : format;

			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append_c (str, '<');
			else
				g_string_append_c (str, '[');
			for (i = 0; i < inst->type_argc; i++) {
				MonoType *t = inst->type_argv [i];

				if (i)
					g_string_append_c (str, ',');
				if ((nested_format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED) &&
				    (t->type != MONO_TYPE_VAR) && (type->type != MONO_TYPE_MVAR))
					g_string_append_c (str, '[');
				mono_type_get_name_recurse (inst->type_argv [i], str, FALSE, nested_format);
				if ((nested_format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED) &&
				    (t->type != MONO_TYPE_VAR) && (type->type != MONO_TYPE_MVAR))
					g_string_append_c (str, ']');
			}
			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append_c (str, '>');
			else
				g_string_append_c (str, ']');
		} else if (mono_class_is_gtd (klass) &&
			   (format != MONO_TYPE_NAME_FORMAT_FULL_NAME) &&
			   (format != MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)) {
			int i;

			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append_c (str, '<');
			else
				g_string_append_c (str, '[');
			for (i = 0; i < mono_class_get_generic_container (klass)->type_argc; i++) {
				if (i)
					g_string_append_c (str, ',');
				g_string_append (str, mono_generic_container_get_param_info (mono_class_get_generic_container (klass), i)->name);
			}
			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append_c (str, '>');
			else
				g_string_append_c (str, ']');
		}

		mono_type_name_check_byref (type, str);

		if ((format == MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED) &&
		    (type->type != MONO_TYPE_VAR) && (type->type != MONO_TYPE_MVAR))
			_mono_type_get_assembly_name (klass, str);
		break;
	}
}

/**
 * mono_type_get_name_full:
 * \param type a type
 * \param format the format for the return string.
 *
 *
 * \returns The string representation in a number of formats:
 *
 * if \p format is \c MONO_TYPE_NAME_FORMAT_REFLECTION, the return string is
 * returned in the format required by \c System.Reflection, this is the
 * inverse of mono_reflection_parse_type().
 *
 * if \p format is \c MONO_TYPE_NAME_FORMAT_IL, it returns a syntax that can
 * be used by the IL assembler.
 *
 * if \p format is \c MONO_TYPE_NAME_FORMAT_FULL_NAME
 *
 * if \p format is \c MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
 */
char*
mono_type_get_name_full (MonoType *type, MonoTypeNameFormat format)
{
	GString* result;

	result = g_string_new ("");

	mono_type_get_name_recurse (type, result, FALSE, format);

	return g_string_free (result, FALSE);
}

/**
 * mono_type_get_full_name:
 * \param class a class
 *
 * \returns The string representation for type as required by System.Reflection.
 * The inverse of mono_reflection_parse_type().
 */
char *
mono_type_get_full_name (MonoClass *klass)
{
	return mono_type_get_name_full (m_class_get_byval_arg (klass), MONO_TYPE_NAME_FORMAT_REFLECTION);
}

/**
 * mono_type_get_name:
 * \param type a type
 * \returns The string representation for type as it would be represented in IL code.
 */
char*
mono_type_get_name (MonoType *type)
{
	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_IL);
}

/**
 * mono_type_get_underlying_type:
 * \param type a type
 * \returns The \c MonoType for the underlying integer type if \p type
 * is an enum and byref is false, otherwise the type itself.
 */
MonoType*
mono_type_get_underlying_type (MonoType *type)
{
	if (type->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (type->data.klass) && !m_type_is_byref (type))
		return mono_class_enum_basetype_internal (type->data.klass);
	if (type->type == MONO_TYPE_GENERICINST && m_class_is_enumtype (type->data.generic_class->container_class) && !m_type_is_byref (type))
		return mono_class_enum_basetype_internal (type->data.generic_class->container_class);
	return type;
}

/**
 * mono_class_is_open_constructed_type:
 * \param type a type
 *
 * \returns TRUE if type represents a generics open constructed type.
 * IOW, not all type parameters required for the instantiation have
 * been provided or it's a generic type definition.
 *
 * An open constructed type means it's a non realizable type. Not to
 * be mixed up with an abstract type - we can't cast or dispatch to
 * an open type, for example.
 */
gboolean
mono_class_is_open_constructed_type (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		return TRUE;
	case MONO_TYPE_SZARRAY:
		return mono_class_is_open_constructed_type (m_class_get_byval_arg (t->data.klass));
	case MONO_TYPE_ARRAY:
		return mono_class_is_open_constructed_type (m_class_get_byval_arg (t->data.array->eklass));
	case MONO_TYPE_PTR:
		return mono_class_is_open_constructed_type (t->data.type);
	case MONO_TYPE_GENERICINST:
		return t->data.generic_class->context.class_inst->is_open;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		return mono_class_is_gtd (t->data.klass);
	default:
		return FALSE;
	}
}

/*
This is a simple function to catch the most common bad instances of generic types.
Specially those that might lead to further failures in the runtime.
*/
gboolean
mono_type_is_valid_generic_argument (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_VOID:
	case MONO_TYPE_TYPEDBYREF:
		return FALSE;
	case MONO_TYPE_VALUETYPE:
		return !m_class_is_byreflike (type->data.klass);
	default:
		return TRUE;
	}
}

static gboolean
can_inflate_gparam_with (MonoGenericParam *gparam, MonoType *type)
{
	if (!mono_type_is_valid_generic_argument (type))
		return FALSE;
#if 0
	/* Avoid inflating gparams with valuetype constraints with ref types during gsharing */
	MonoGenericParamInfo *info = mono_generic_param_info (gparam);
	if (info && (info->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT)) {
		if (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) {
			MonoGenericParam *inst_gparam = type->data.generic_param;
			if (inst_gparam->gshared_constraint && inst_gparam->gshared_constraint->type == MONO_TYPE_OBJECT)
				return FALSE;
		}
	}
#endif
	return TRUE;
}

static MonoType*
inflate_generic_custom_modifiers (MonoImage *image, const MonoType *type, MonoGenericContext *context, MonoError *error);

static MonoType*
inflate_generic_type (MonoImage *image, MonoType *type, MonoGenericContext *context, MonoError *error)
{
	gboolean changed = FALSE;
	error_init (error);

	/* C++/CLI (and some Roslyn tests) constructs method signatures like:
	 *     void .CL1`1.Test(!0 modopt(System.Nullable`1<!0>))
	 * where !0 has a custom modifier which itself mentions the type variable.
	 * So we need to potentially inflate the modifiers.
	 */
	if (type->has_cmods) {
		MonoType *new_type = inflate_generic_custom_modifiers (image, type, context, error);
		return_val_if_nok (error, NULL);
		if (new_type != NULL) {
			type = new_type;
			changed = TRUE;
		}
	}

	switch (type->type) {
	case MONO_TYPE_MVAR: {
		MonoType *nt;
		int num = mono_type_get_generic_param_num (type);
		MonoGenericInst *inst = context->method_inst;
		if (!inst) {
			if (!changed)
				return NULL;
			else
				return type;
		}
		MonoGenericParam *gparam = type->data.generic_param;
		if (num >= inst->type_argc) {
			const char *pname = mono_generic_param_name (gparam);
			mono_error_set_bad_image (error, image, "MVAR %d (%s) cannot be expanded in this context with %d instantiations",
				num, pname ? pname : "", inst->type_argc);
			return NULL;
		}

		if (!can_inflate_gparam_with (gparam, inst->type_argv [num])) {
			const char *pname = mono_generic_param_name (gparam);
			mono_error_set_bad_image (error, image, "MVAR %d (%s) cannot be expanded with type 0x%x",
				num, pname ? pname : "", inst->type_argv [num]->type);
			return NULL;
		}
		/*
		 * Note that the VAR/MVAR cases are different from the rest.  The other cases duplicate @type,
		 * while the VAR/MVAR duplicates a type from the context.  So, we need to ensure that the
		 * ->byref__ and ->attrs from @type are propagated to the returned type.
		 */
		nt = mono_metadata_type_dup_with_cmods (image, inst->type_argv [num], type);
		nt->byref__ = type->byref__;
		nt->attrs = type->attrs;
		return nt;
	}
	case MONO_TYPE_VAR: {
		MonoType *nt;
		int num = mono_type_get_generic_param_num (type);
		MonoGenericInst *inst = context->class_inst;
		if (!inst) {
			if (!changed)
				return NULL;
			else
				return type;
		}
		MonoGenericParam *gparam = type->data.generic_param;
		if (num >= inst->type_argc) {
			const char *pname = mono_generic_param_name (gparam);
			mono_error_set_bad_image (error, image, "VAR %d (%s) cannot be expanded in this context with %d instantiations",
				num, pname ? pname : "", inst->type_argc);
			return NULL;
		}
		if (!can_inflate_gparam_with (gparam, inst->type_argv [num])) {
			const char *pname = mono_generic_param_name (gparam);
			mono_error_set_bad_image (error, image, "VAR %d (%s) cannot be expanded with type 0x%x",
				num, pname ? pname : "", inst->type_argv [num]->type);
			return NULL;
		}

#ifdef DEBUG_INFLATE_CMODS
		gboolean append_cmods;
		append_cmods = FALSE;
		if (type->has_cmods && inst->type_argv[num]->has_cmods) {
			char *tname = mono_type_full_name (type);
			char *vname = mono_type_full_name (inst->type_argv[num]);
			printf ("\n\n\tsubstitution for '%s' with '%s' yields...\n", tname, vname);
			g_free (tname);
			g_free (vname);
			append_cmods = TRUE;
		}
#endif

		nt = mono_metadata_type_dup_with_cmods (image, inst->type_argv [num], type);
		nt->byref__ = type->byref__ || inst->type_argv[num]->byref__;
		nt->attrs = type->attrs;
#ifdef DEBUG_INFLATE_CMODS
		if (append_cmods) {
			char *ntname = mono_type_full_name (nt);
			printf ("\tyields '%s'\n\n\n", ntname);
			g_free (ntname);
		}
#endif
		return nt;
	}
	case MONO_TYPE_SZARRAY: {
		MonoClass *eclass = type->data.klass;
		MonoType *nt, *inflated = inflate_generic_type (NULL, m_class_get_byval_arg (eclass), context, error);
		if ((!inflated && !changed) || !is_ok (error))
			return NULL;
		if (!inflated)
			return type;
		nt = mono_metadata_type_dup (image, type);
		nt->data.klass = mono_class_from_mono_type_internal (inflated);
		mono_metadata_free_type (inflated);
		return nt;
	}
	case MONO_TYPE_ARRAY: {
		MonoClass *eclass = type->data.array->eklass;
		MonoType *nt, *inflated = inflate_generic_type (NULL, m_class_get_byval_arg (eclass), context, error);
		if ((!inflated && !changed) || !is_ok (error))
			return NULL;
		if (!inflated)
			return type;
		nt = mono_metadata_type_dup (image, type);
		nt->data.array->eklass = mono_class_from_mono_type_internal (inflated);
		mono_metadata_free_type (inflated);
		return nt;
	}
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *gclass = type->data.generic_class;
		MonoGenericInst *inst;
		MonoType *nt;
		if (!gclass->context.class_inst->is_open) {
			if (!changed)
				return NULL;
			else
				return type;
		}

		inst = mono_metadata_inflate_generic_inst (gclass->context.class_inst, context, error);
		return_val_if_nok (error, NULL);

		if (inst != gclass->context.class_inst)
			gclass = mono_metadata_lookup_generic_class (gclass->container_class, inst, gclass->is_dynamic);

		if (gclass == type->data.generic_class) {
			if (!changed)
				return NULL;
			else
				return type;
		}

		nt = mono_metadata_type_dup (image, type);
		nt->data.generic_class = gclass;
		return nt;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = type->data.klass;
		MonoGenericContainer *container = mono_class_try_get_generic_container (klass);
		MonoGenericInst *inst;
		MonoGenericClass *gclass = NULL;
		MonoType *nt;

		if (!container) {
			if (!changed)
				return NULL;
			else
				return type;
		}

		/* We can't use context->class_inst directly, since it can have more elements */
		inst = mono_metadata_inflate_generic_inst (container->context.class_inst, context, error);
		return_val_if_nok (error, NULL);

		if (inst == container->context.class_inst) {
			if (!changed)
				return NULL;
			else
				return type;
		}

		gclass = mono_metadata_lookup_generic_class (klass, inst, image_is_dynamic (m_class_get_image (klass)));

		nt = mono_metadata_type_dup (image, type);
		nt->type = MONO_TYPE_GENERICINST;
		nt->data.generic_class = gclass;
		return nt;
	}
	case MONO_TYPE_PTR: {
		MonoType *nt, *inflated = inflate_generic_type (image, type->data.type, context, error);
		if ((!inflated && !changed) || !is_ok (error))
			return NULL;
		if (!inflated && changed)
			return type;
		nt = mono_metadata_type_dup (image, type);
		nt->data.type = inflated;
		return nt;
	}
	default:
		if (!changed)
			return NULL;
		else
			return type;
	}
	return NULL;
}

static MonoType*
inflate_generic_custom_modifiers (MonoImage *image, const MonoType *type, MonoGenericContext *context, MonoError *error)
{
	MonoType *result = NULL;
	g_assert (type->has_cmods);
	int count = mono_type_custom_modifier_count (type);
	gboolean changed = FALSE;

	/* Try not to blow up the stack. See comment on MONO_MAX_EXPECTED_CMODS. */
	g_assert (count < MONO_MAX_EXPECTED_CMODS);
	size_t aggregate_size = mono_sizeof_aggregate_modifiers (count);
	MonoAggregateModContainer *candidate_mods = g_alloca (aggregate_size);
	memset (candidate_mods, 0, aggregate_size);
	candidate_mods->count = count;

	for (int i = 0; i < count; ++i) {
		gboolean required;
		MonoType *cmod_old = mono_type_get_custom_modifier (type, i, &required, error);
		goto_if_nok (error, leave);
		MonoType *cmod_new = inflate_generic_type (NULL, cmod_old, context, error);
		goto_if_nok (error, leave);
		if (cmod_new)
			changed = TRUE;
		candidate_mods->modifiers [i].required = required;
		candidate_mods->modifiers [i].type = cmod_new;
	}

	if (changed) {
		/* if we're going to make a new type, fill in any modifiers that weren't affected by inflation with copies of the original values. */
		for (int i = 0; i < count; ++i) {
			if (candidate_mods->modifiers [i].type == NULL) {
				candidate_mods->modifiers [i].type = mono_metadata_type_dup (NULL, mono_type_get_custom_modifier (type, i, NULL, error));

				/* it didn't error in the first loop, so should be ok now, too */
				mono_error_assert_ok (error);
			}
		}
	}
#ifdef DEBUG_INFLATE_CMODS
	if (changed) {
		char *full_name = mono_type_full_name ((MonoType*)type);
		printf ("\n\n\tcustom modifier on '%s' affected by subsititution\n\n\n", full_name);
		g_free (full_name);
	}
#endif
	if (changed) {
		MonoType *new_type = g_alloca (mono_sizeof_type_with_mods (count, TRUE));
		/* first init just the non-modifier portion of new_type before populating the
		 * new modifiers */
		memcpy (new_type, type, MONO_SIZEOF_TYPE);
		mono_type_with_mods_init (new_type, count, TRUE);
		mono_type_set_amods (new_type, mono_metadata_get_canonical_aggregate_modifiers (candidate_mods));
		result =  mono_metadata_type_dup (image, new_type);
	}

leave:
	for (int i = 0; i < count; ++i) {
		if (candidate_mods->modifiers [i].type)
			mono_metadata_free_type (candidate_mods->modifiers [i].type);
	}

	return result;
}

MonoGenericContext *
mono_generic_class_get_context (MonoGenericClass *gclass)
{
	return &gclass->context;
}

MonoGenericContext *
mono_class_get_context (MonoClass *klass)
{
	MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
	return gklass ? mono_generic_class_get_context (gklass) : NULL;
}

/*
 * mono_class_inflate_generic_type_with_mempool:
 * @mempool: a mempool
 * @type: a type
 * @context: a generics context
 * @error: error context
 *
 * The same as mono_class_inflate_generic_type, but allocates the MonoType
 * from mempool if it is non-NULL.  If it is NULL, the MonoType is
 * allocated on the heap and is owned by the caller.
 * The returned type can potentially be the same as TYPE, so it should not be
 * modified by the caller, and it should be freed using mono_metadata_free_type ().
 */
MonoType*
mono_class_inflate_generic_type_with_mempool (MonoImage *image, MonoType *type, MonoGenericContext *context, MonoError *error)
{
	MonoType *inflated = NULL;
	error_init (error);

	if (context)
		inflated = inflate_generic_type (image, type, context, error);
	return_val_if_nok (error, NULL);

	if (!inflated) {
		MonoType *shared = mono_metadata_get_shared_type (type);

		if (shared && !type->has_cmods) {
			return shared;
		} else {
			return mono_metadata_type_dup (image, type);
		}
	}

	UnlockedIncrement (&mono_stats.inflated_type_count);
	return inflated;
}

/**
 * mono_class_inflate_generic_type:
 * \param type a type
 * \param context a generics context
 * \deprecated Please use \c mono_class_inflate_generic_type_checked instead
 *
 * If \p type is a generic type and \p context is not NULL, instantiate it using the
 * generics context \p context.
 *
 * \returns The instantiated type or a copy of \p type. The returned \c MonoType is allocated
 * on the heap and is owned by the caller. Returns NULL on error.
 */
MonoType*
mono_class_inflate_generic_type (MonoType *type, MonoGenericContext *context)
{
	ERROR_DECL (error);
	MonoType *result;
	result = mono_class_inflate_generic_type_checked (type, context, error);
	mono_error_cleanup (error);
	return result;
}

/*
 * mono_class_inflate_generic_type:
 * @type: a type
 * @context: a generics context
 * @error: error context to use
 *
 * If @type is a generic type and @context is not NULL, instantiate it using the
 * generics context @context.
 *
 * Returns: The instantiated type or a copy of @type. The returned MonoType is allocated
 * on the heap and is owned by the caller.
 */
MonoType*
mono_class_inflate_generic_type_checked (MonoType *type, MonoGenericContext *context, MonoError *error)
{
	return mono_class_inflate_generic_type_with_mempool (NULL, type, context, error);
}

/*
 * mono_class_inflate_generic_type_no_copy:
 *
 *   Same as inflate_generic_type_with_mempool, but return TYPE if no inflation
 * was done.
 */
static MonoType*
mono_class_inflate_generic_type_no_copy (MonoImage *image, MonoType *type, MonoGenericContext *context, MonoError *error)
{
	MonoType *inflated = NULL;

	error_init (error);
	if (context) {
		inflated = inflate_generic_type (image, type, context, error);
		return_val_if_nok (error, NULL);
	}

	if (!inflated)
		return type;

	UnlockedIncrement (&mono_stats.inflated_type_count);
	return inflated;
}

/*
 * mono_class_inflate_generic_class:
 *
 *   Inflate the class @gklass with @context. Set @error on failure.
 */
MonoClass*
mono_class_inflate_generic_class_checked (MonoClass *gklass, MonoGenericContext *context, MonoError *error)
{
	MonoClass *res;
	MonoType *inflated;

	inflated = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (gklass), context, error);
	return_val_if_nok (error, NULL);

	res = mono_class_from_mono_type_internal (inflated);
	mono_metadata_free_type (inflated);

	return res;
}

static MonoGenericContext
inflate_generic_context (MonoGenericContext *context, MonoGenericContext *inflate_with, MonoError *error)
{
	MonoGenericInst *class_inst = NULL;
	MonoGenericInst *method_inst = NULL;
	MonoGenericContext res = { NULL, NULL };

	error_init (error);

	if (context->class_inst) {
		class_inst = mono_metadata_inflate_generic_inst (context->class_inst, inflate_with, error);
		if (!is_ok (error))
			goto fail;
	}

	if (context->method_inst) {
		method_inst = mono_metadata_inflate_generic_inst (context->method_inst, inflate_with, error);
		if (!is_ok (error))
			goto fail;
	}

	res.class_inst = class_inst;
	res.method_inst = method_inst;
fail:
	return res;
}

/**
 * mono_class_inflate_generic_method:
 * \param method a generic method
 * \param context a generics context
 *
 * Instantiate the generic method \p method using the generics context \p context.
 *
 * \returns The new instantiated method
 */
MonoMethod *
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericContext *context)
{
	ERROR_DECL (error);
	MonoMethod *res = mono_class_inflate_generic_method_full_checked (method, NULL, context, error);
	mono_error_assert_msg_ok (error, "Could not inflate generic method");
	return res;
}

MonoMethod *
mono_class_inflate_generic_method_checked (MonoMethod *method, MonoGenericContext *context, MonoError *error)
{
	return mono_class_inflate_generic_method_full_checked (method, NULL, context, error);
}

static gboolean
inflated_method_equal (gconstpointer a, gconstpointer b)
{
	const MonoMethodInflated *ma = (const MonoMethodInflated *)a;
	const MonoMethodInflated *mb = (const MonoMethodInflated *)b;
	if (ma->declaring != mb->declaring)
		return FALSE;
	return mono_metadata_generic_context_equal (&ma->context, &mb->context);
}

static guint
inflated_method_hash (gconstpointer a)
{
	const MonoMethodInflated *ma = (const MonoMethodInflated *)a;
	return (mono_metadata_generic_context_hash (&ma->context) ^ mono_aligned_addr_hash (ma->declaring));
}

static void
free_inflated_method (MonoMethodInflated *imethod)
{
	MonoMethod *method = (MonoMethod*)imethod;

	if (method->signature)
		mono_metadata_free_inflated_signature (method->signature);

	if (method->wrapper_type)
		g_free (((MonoMethodWrapper*)method)->method_data);

	g_free (method);
}

/**
 * mono_class_inflate_generic_method_full_checked:
 * Instantiate method \p method with the generic context \p context.
 * On failure returns NULL and sets \p error.
 *
 * BEWARE: All non-trivial fields are invalid, including klass, signature, and header.
 *         Use mono_method_signature_internal () and mono_method_get_header () to get the correct values.
 */
MonoMethod*
mono_class_inflate_generic_method_full_checked (MonoMethod *method, MonoClass *klass_hint, MonoGenericContext *context, MonoError *error)
{
	MonoMethod *result;
	MonoMethodInflated *iresult, *cached;
	MonoMethodSignature *sig;
	MonoGenericContext tmp_context;

	error_init (error);

	/* The `method' has already been instantiated before => we need to peel out the instantiation and create a new context */
	while (method->is_inflated) {
		MonoGenericContext *method_context = mono_method_get_context (method);
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;

		tmp_context = inflate_generic_context (method_context, context, error);
		return_val_if_nok (error, NULL);

		context = &tmp_context;

		if (mono_metadata_generic_context_equal (method_context, context))
			return method;

		method = imethod->declaring;
	}

	/*
	 * A method only needs to be inflated if the context has argument for which it is
	 * parametric. Eg:
	 *
	 * class Foo<T> { void Bar(); } - doesn't need to be inflated if only mvars' are supplied
	 * class Foo { void Bar<T> (); } - doesn't need to be if only vars' are supplied
	 *
	 */
	if (!((method->is_generic && context->method_inst) ||
		(mono_class_is_gtd (method->klass) && context->class_inst)))
		return method;

	iresult = g_new0 (MonoMethodInflated, 1);
	iresult->context = *context;
	iresult->declaring = method;

	if (!context->method_inst && method->is_generic)
		iresult->context.method_inst = mono_method_get_generic_container (method)->context.method_inst;

	if (!context->class_inst) {
		g_assert (!mono_class_is_ginst (iresult->declaring->klass));
		if (mono_class_is_gtd (iresult->declaring->klass))
			iresult->context.class_inst = mono_class_get_generic_container (iresult->declaring->klass)->context.class_inst;
	}
	/* This can happen with some callers like mono_object_get_virtual_method_internal () */
	if (!mono_class_is_gtd (iresult->declaring->klass) && !mono_class_is_ginst (iresult->declaring->klass))
		iresult->context.class_inst = NULL;

	MonoMemoryManager *mm = mono_metadata_get_mem_manager_for_method (iresult);

	// check cache
	mono_mem_manager_lock (mm);
	if (!mm->gmethod_cache)
		mm->gmethod_cache = g_hash_table_new_full (inflated_method_hash, inflated_method_equal, NULL, (GDestroyNotify)free_inflated_method);
	cached = (MonoMethodInflated *)g_hash_table_lookup (mm->gmethod_cache, iresult);
	mono_mem_manager_unlock (mm);

	if (cached) {
		g_free (iresult);
		return (MonoMethod*)cached;
	}

	UnlockedIncrement (&mono_stats.inflated_method_count);

	UnlockedAdd (&mono_inflated_methods_size,  sizeof (MonoMethodInflated));

	sig = mono_method_signature_internal (method);
	if (!sig) {
		char *name = mono_type_get_full_name (method->klass);
		mono_error_set_bad_image (error, mono_method_get_image (method), "Could not resolve signature of method %s:%s", name, method->name);
		g_free (name);
		goto fail;
	}

	if (sig->pinvoke) {
		memcpy (&iresult->method.pinvoke, method, sizeof (MonoMethodPInvoke));
	} else {
		memcpy (&iresult->method.method, method, sizeof (MonoMethod));
	}

	result = (MonoMethod *) iresult;
	result->is_inflated = TRUE;
	result->is_generic = FALSE;
	result->sre_method = FALSE;
	result->signature = NULL;

	if (method->wrapper_type) {
		MonoMethodWrapper *mw = (MonoMethodWrapper*)method;
		MonoMethodWrapper *resw = (MonoMethodWrapper*)result;
		int len = GPOINTER_TO_INT (((void**)mw->method_data) [0]);

		resw->method_data = (void **)g_malloc (sizeof (gpointer) * (len + 1));
		memcpy (resw->method_data, mw->method_data, sizeof (gpointer) * (len + 1));
	}

	if (iresult->context.method_inst) {
		MonoGenericInst *method_inst = iresult->context.method_inst;
		/* Set the generic_container of the result to the generic_container of method */
		MonoGenericContainer *generic_container = mono_method_get_generic_container (method);

		if (generic_container && method_inst == generic_container->context.method_inst) {
			result->is_generic = 1;
			mono_method_set_generic_container (result, generic_container);
		}

		/* Check that the method is not instantiated with any invalid types */
		for (int i = 0; i < method_inst->type_argc; i++) {
			if (!mono_type_is_valid_generic_argument (method_inst->type_argv [i])) {
				mono_error_set_bad_image (error, mono_method_get_image (method), "MVAR %d cannot be expanded with type 0x%x",
							  i, method_inst->type_argv [i]->type);
				goto fail;
			}
		}
	}

	if (klass_hint) {
		MonoGenericClass *gklass_hint = mono_class_try_get_generic_class (klass_hint);
		if (gklass_hint && (gklass_hint->container_class != method->klass || gklass_hint->context.class_inst != context->class_inst))
			klass_hint = NULL;
	}

	if (mono_class_is_gtd (method->klass))
		result->klass = klass_hint;

	if (!result->klass) {
		MonoType *inflated = inflate_generic_type (NULL, m_class_get_byval_arg (method->klass), context, error);
		if (!is_ok (error))
			goto fail;

		result->klass = inflated ? mono_class_from_mono_type_internal (inflated) : method->klass;
		if (inflated)
			mono_metadata_free_type (inflated);
	}

	/*
	 * FIXME: This should hold, but it doesn't:
	 *
	 * if (result->is_inflated && mono_method_get_context (result)->method_inst &&
	 *		mono_method_get_context (result)->method_inst == mono_method_get_generic_container (((MonoMethodInflated*)result)->declaring)->context.method_inst) {
	 * 	g_assert (result->is_generic);
	 * }
	 *
	 * Fixing this here causes other things to break, hence a very
	 * ugly hack in mini-trampolines.c - see
	 * is_generic_method_definition().
	 */

	// check cache
	mono_mem_manager_lock (mm);
	cached = (MonoMethodInflated *)g_hash_table_lookup (mm->gmethod_cache, iresult);
	if (!cached) {
		g_hash_table_insert (mm->gmethod_cache, iresult, iresult);
		iresult->owner = mm;
		cached = iresult;
	}
	mono_mem_manager_unlock (mm);

	return (MonoMethod*)cached;

fail:
	g_free (iresult);
	return NULL;
}

/**
 * mono_get_inflated_method:
 *
 * Obsolete.  We keep it around since it's mentioned in the public API.
 */
MonoMethod*
mono_get_inflated_method (MonoMethod *method)
{
	return method;
}

/*
 * mono_method_get_context_general:
 * @method: a method
 * @uninflated: handle uninflated methods?
 *
 * Returns the generic context of a method or NULL if it doesn't have
 * one.  For an inflated method that's the context stored in the
 * method.  Otherwise it's in the method's generic container or in the
 * generic container of the method's class.
 */
MonoGenericContext*
mono_method_get_context_general (MonoMethod *method, gboolean uninflated)
{
	if (method->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;
		return &imethod->context;
	}
	if (!uninflated)
		return NULL;
	if (method->is_generic)
		return &(mono_method_get_generic_container (method)->context);
	if (mono_class_is_gtd (method->klass))
		return &mono_class_get_generic_container (method->klass)->context;
	return NULL;
}

/*
 * mono_method_get_context:
 * @method: a method
 *
 * Returns the generic context for method if it's inflated, otherwise
 * NULL.
 */
MonoGenericContext*
mono_method_get_context (MonoMethod *method)
{
	return mono_method_get_context_general (method, FALSE);
}

/*
 * mono_method_get_generic_container:
 *
 *   Returns the generic container of METHOD, which should be a generic method definition.
 * Returns NULL if METHOD is not a generic method definition.
 * LOCKING: Acquires the loader lock.
 */
MonoGenericContainer*
mono_method_get_generic_container (MonoMethod *method)
{
	MonoGenericContainer *container;

	if (!method->is_generic)
		return NULL;

	container = (MonoGenericContainer *)mono_image_property_lookup (mono_method_get_image (method), method, MONO_METHOD_PROP_GENERIC_CONTAINER);
	g_assert (container);

	return container;
}

/*
 * mono_method_set_generic_container:
 *
 *   Sets the generic container of METHOD to CONTAINER.
 * LOCKING: Acquires the image lock.
 */
void
mono_method_set_generic_container (MonoMethod *method, MonoGenericContainer* container)
{
	g_assert (method->is_generic);

	mono_image_property_insert (mono_method_get_image (method), method, MONO_METHOD_PROP_GENERIC_CONTAINER, container);
}

/**
 * mono_method_set_verification_success:
 *
 * Sets a bit indicating that the method has been verified.
 *
 * LOCKING: acquires the image lock.
 */
void
mono_method_set_verification_success (MonoMethod *method)
{
	g_assert (!method->is_inflated);

	mono_image_property_insert (mono_method_get_image (method), method, MONO_METHOD_PROP_VERIFICATION_SUCCESS, GUINT_TO_POINTER(1));
}

/**
 * mono_method_get_verification_sucess:
 *
 * Returns \c TRUE if the method has been verified successfully.
 *
 * LOCKING: acquires the image lock.
 */
gboolean
mono_method_get_verification_success (MonoMethod *method)
{
	if (method->is_inflated)
		method = ((MonoMethodInflated *)method)->declaring;

	gpointer value = mono_image_property_lookup (mono_method_get_image (method), method, MONO_METHOD_PROP_VERIFICATION_SUCCESS);

	return value != NULL;
}

/**
 * mono_method_lookup_infrequent_bits:
 *
 * Looks for existing \c MonoMethodDefInfrequentBits struct associated with
 * this method definition.  Unlike \c mono_method_get_infrequent bits, this
 * does not allocate a new struct if one doesn't exist.
 *
 * LOCKING: Acquires the image lock
 */
const MonoMethodDefInfrequentBits*
mono_method_lookup_infrequent_bits (MonoMethod *method)
{
	g_assert (!method->is_inflated);

	return (const MonoMethodDefInfrequentBits*)mono_image_property_lookup (mono_method_get_image (method), method, MONO_METHOD_PROP_INFREQUENT_BITS);
}

/**
 * mono_method_get_infrequent_bits:
 *
 * Looks for an existing, or allocates a new \c MonoMethodDefInfrequentBits struct for this method definition.
 * Method must not be inflated.
 *
 * Unlike \c mono_method_lookup_infrequent_bits, this will allocate a new
 * struct if the method didn't have one.
 *
 * LOCKING: Acquires the image lock
 */
MonoMethodDefInfrequentBits *
mono_method_get_infrequent_bits (MonoMethod *method)
{
	g_assert (!method->is_inflated);
	MonoImage *image = mono_method_get_image (method);
	MonoMethodDefInfrequentBits *infrequent_bits = NULL;
	mono_image_lock (image);
	infrequent_bits = (MonoMethodDefInfrequentBits *)mono_image_property_lookup (image, method, MONO_METHOD_PROP_INFREQUENT_BITS);
	if (!infrequent_bits) {
		infrequent_bits = (MonoMethodDefInfrequentBits *)mono_image_alloc0 (image, sizeof (MonoMethodDefInfrequentBits));
		mono_image_property_insert (image, method, MONO_METHOD_PROP_INFREQUENT_BITS, infrequent_bits);
	}
	mono_image_unlock (image);
	return infrequent_bits;
}

gboolean
mono_method_get_is_reabstracted (MonoMethod *method)
{
	if (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;
	const MonoMethodDefInfrequentBits *infrequent_bits = mono_method_lookup_infrequent_bits (method);
	return infrequent_bits != NULL && infrequent_bits->is_reabstracted;
}

gboolean
mono_method_get_is_covariant_override_impl (MonoMethod *method)
{
	if (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;
	const MonoMethodDefInfrequentBits *infrequent_bits = mono_method_lookup_infrequent_bits (method);
	return infrequent_bits != NULL && infrequent_bits->is_covariant_override_impl;
}

/**
 * mono_method_set_is_reabstracted:
 *
 * Sets the \c MonoMethodDefInfrequentBits:is_reabstracted bit for this method
 * definition.  The bit means that the method is a default interface method
 * that used to have a default implementation in an ancestor interface, but is
 * now abstract once again.
 *
 * LOCKING: Assumes the loader lock is held
 */
void
mono_method_set_is_reabstracted (MonoMethod *method)
{
	mono_method_get_infrequent_bits (method)->is_reabstracted = 1;
}

/**
 * mono_method_set_is_covariant_override_impl:
 *
 * Sets the \c MonoMethodDefInfrequentBits:is_covariant_override_impl bit for
 * this method definition.  The bit means that the method is an override with a
 * signature that is not equal to the signature of the method that it is
 * overriding.
 *
 * LOCKING: Assumes the loader lock is held
 */
void
mono_method_set_is_covariant_override_impl (MonoMethod *method)
{
	mono_method_get_infrequent_bits (method)->is_covariant_override_impl = 1;
}

/**
 * mono_class_find_enum_basetype:
 * \param class The enum class
 *
 *   Determine the basetype of an enum by iterating through its fields. We do this
 * in a separate function since it is cheaper than calling mono_class_setup_fields.
 */
MonoType*
mono_class_find_enum_basetype (MonoClass *klass, MonoError *error)
{
	MonoGenericContainer *container = NULL;
	MonoImage *image = m_class_get_image (klass);
	const int top = mono_class_get_field_count (klass);
	int i, first_field_idx;

	g_assert (m_class_is_enumtype (klass));

	error_init (error);

	container = mono_class_try_get_generic_container (klass);
	if (mono_class_is_ginst (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		container = mono_class_get_generic_container (gklass);
		g_assert (container);
	}

	/*
	 * Fetch all the field information.
	 */
	first_field_idx = mono_class_get_first_field_idx (klass);
	/*
	 * metadata-update: adding new enum fields isn't supported, so when this code runs, all the
	 * fields are contiguous in metadata.
	 */
	for (i = 0; i < top; i++){
		const char *sig;
		guint32 cols [MONO_FIELD_SIZE];
		int idx = first_field_idx + i;
		MonoType *ftype;

		/* first_field_idx and idx points into the fieldptr table */
		mono_metadata_decode_table_row (image, MONO_TABLE_FIELD, idx, cols, MONO_FIELD_SIZE);

		if (cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_STATIC) //no need to decode static fields
			continue;

		sig = mono_metadata_blob_heap (image, cols [MONO_FIELD_SIGNATURE]);
		mono_metadata_decode_value (sig, &sig);
		/* FIELD signature == 0x06 */
		if (*sig != 0x06) {
			mono_error_set_bad_image (error, image, "Invalid field signature %x, expected 0x6 but got %x", cols [MONO_FIELD_SIGNATURE], *sig);
			goto fail;
		}

		ftype = mono_metadata_parse_type_checked (image, container, cols [MONO_FIELD_FLAGS], FALSE, sig + 1, &sig, error);
		if (!ftype)
			goto fail;

		if (mono_class_is_ginst (klass)) {
			//FIXME do we leak here?
			ftype = mono_class_inflate_generic_type_checked (ftype, mono_class_get_context (klass), error);
			if (!is_ok (error))
				goto fail;
			ftype->attrs = cols [MONO_FIELD_FLAGS];
		}

		return ftype;
	}
	mono_error_set_type_load_class (error, klass, "Could not find base type");

fail:
	return NULL;
}

/*
 * Checks for MonoClass::has_failure without resolving all MonoType's into MonoClass'es
 */
gboolean
mono_type_has_exceptions (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_SZARRAY:
		return mono_class_has_failure (type->data.klass);
	case MONO_TYPE_ARRAY:
		return mono_class_has_failure (type->data.array->eklass);
	case MONO_TYPE_GENERICINST:
		return mono_class_has_failure (mono_class_create_generic_inst (type->data.generic_class));
	default:
		return FALSE;
	}
}

void
mono_error_set_for_class_failure (MonoError *oerror, const MonoClass *klass)
{
	g_assert (mono_class_has_failure (klass));
	MonoErrorBoxed *box = mono_class_get_exception_data ((MonoClass*)klass);
	mono_error_set_from_boxed (oerror, box);
}

/*
 * mono_class_alloc:
 *
 *   Allocate memory for data belonging to CLASS.
 */
gpointer
mono_class_alloc (MonoClass *klass, int size)
{
	return m_class_alloc (klass, size);
}

gpointer
(mono_class_alloc0) (MonoClass *klass, int size)
{
	return m_class_alloc0 (klass, size);
}

#define mono_class_new0(klass,struct_type, n_structs)		\
    ((struct_type *) mono_class_alloc0 ((klass), ((gsize) sizeof (struct_type)) * ((gsize) (n_structs))))

/**
 * mono_class_set_failure_causedby_class:
 * \param klass the class that is failing
 * \param caused_by the class that caused the failure
 * \param msg Why \p klass is failing.
 *
 * If \p caused_by has a failure, sets a TypeLoadException failure on
 * \p klass with message "\p msg, due to: {\p caused_by message}".
 *
 * \returns TRUE if a failiure was set, or FALSE if \p caused_by doesn't have a failure.
 */
gboolean
mono_class_set_type_load_failure_causedby_class (MonoClass *klass, const MonoClass *caused_by, const gchar* msg)
{
	if (mono_class_has_failure (caused_by)) {
		ERROR_DECL (cause_error);
		mono_error_set_for_class_failure (cause_error, caused_by);
		mono_class_set_type_load_failure (klass, "%s, due to: %s", msg, mono_error_get_message (cause_error));
		mono_error_cleanup (cause_error);
		return TRUE;
	} else {
		return FALSE;
	}
}


/*
 * mono_type_get_basic_type_from_generic:
 * @type: a type
 *
 * Returns a closed type corresponding to the possibly open type
 * passed to it.
 */
MonoType*
mono_type_get_basic_type_from_generic (MonoType *type)
{
	/* When we do generic sharing we let type variables stand for reference/primitive types. */
	if (!m_type_is_byref (type) && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) &&
		(!type->data.generic_param->gshared_constraint || type->data.generic_param->gshared_constraint->type == MONO_TYPE_OBJECT))
		return mono_get_object_type ();
	return type;
}

/*
 * mono_class_get_method_by_index:
 *
 *   Returns klass->methods [index], initializing klass->methods if neccesary.
 *
 * LOCKING: Acquires the loader lock.
 */
MonoMethod*
mono_class_get_method_by_index (MonoClass *klass, int index)
{
	ERROR_DECL (error);

	MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
	/* Avoid calling setup_methods () if possible */
	if (gklass && !m_class_get_methods (klass)) {
		MonoMethod *m;

		m = mono_class_inflate_generic_method_full_checked (
			m_class_get_methods (gklass->container_class) [index], klass, mono_class_get_context (klass), error);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
		/*
		 * If setup_methods () is called later for this class, no duplicates are created,
		 * since inflate_generic_method guarantees that only one instance of a method
		 * is created for each context.
		 */
		/*
		mono_class_setup_methods (klass);
		g_assert (m == klass->methods [index]);
		*/
		return m;
	} else {
		mono_class_setup_methods (klass);
		if (mono_class_has_failure (klass)) /*FIXME do proper error handling*/
			return NULL;
		g_assert (index >= 0 && index < mono_class_get_method_count (klass));
		return m_class_get_methods (klass) [index];
	}
}

/**
 * mono_class_get_inflated_method:
 * \param klass an inflated class
 * \param method a method of \p klass's generic definition
 * \param error set on error
 *
 * Given an inflated class \p klass and a method \p method which should be a
 * method of \p klass's generic definition, return the inflated method
 * corresponding to \p method.
 *
 * On failure sets \p error and returns NULL.
 */
MonoMethod*
mono_class_get_inflated_method (MonoClass *klass, MonoMethod *method, MonoError *error)
{
	MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;
	int i, mcount;

	g_assert (method->klass == gklass);

	mono_class_setup_methods (gklass);
	if (mono_class_has_failure (gklass)) {
		mono_error_set_for_class_failure (error, gklass);
		return NULL;
	}

	MonoMethod **gklass_methods = m_class_get_methods (gklass);
	mcount = mono_class_get_method_count (gklass);
	for (i = 0; i < mcount; ++i) {
		if (gklass_methods [i] == method) {
			MonoMethod *inflated_method = NULL;
			MonoMethod **klass_methods = m_class_get_methods (klass);
			if (klass_methods) {
				inflated_method = klass_methods [i];
			} else {
				inflated_method = mono_class_inflate_generic_method_full_checked (gklass_methods [i], klass, mono_class_get_context (klass), error);
				return_val_if_nok (error, NULL);
			}
			g_assert (inflated_method);
			return inflated_method;
		}
	}

	g_assert_not_reached ();
}

/*
 * mono_class_get_vtable_entry:
 *
 *   Returns klass->vtable [offset], computing it if neccesary. Returns NULL on failure.
 * LOCKING: Acquires the loader lock.
 */
MonoMethod*
mono_class_get_vtable_entry (MonoClass *klass, int offset)
{
	MonoMethod *m;

	if (m_class_get_rank (klass) == 1) {
		MonoClass *klass_parent = m_class_get_parent (klass);
		/*
		 * szarrays do not overwrite any methods of Array, so we can avoid
		 * initializing their vtables in some cases.
		 */
		mono_class_setup_vtable (klass_parent);
		if (offset < m_class_get_vtable_size (klass_parent))
			return m_class_get_vtable (klass_parent) [offset];
	}

	if (mono_class_is_ginst (klass)) {
		ERROR_DECL (error);
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;
		mono_class_setup_vtable (gklass);
		m = m_class_get_vtable (gklass) [offset];

		m = mono_class_inflate_generic_method_full_checked (m, klass, mono_class_get_context (klass), error);
		g_assert (is_ok (error)); /* FIXME don't swallow this error */
	} else {
		mono_class_setup_vtable (klass);
		if (mono_class_has_failure (klass))
			return NULL;
		m = m_class_get_vtable (klass) [offset];
	}

	return m;
}

/*
 * mono_class_get_vtable_size:
 *
 *   Return the vtable size for KLASS.
 */
int
mono_class_get_vtable_size (MonoClass *klass)
{
	mono_class_setup_vtable (klass);

	return m_class_get_vtable_size (klass);
}

static void
collect_implemented_interfaces_aux (MonoClass *klass, GPtrArray **res, GHashTable **ifaces, MonoError *error)
{
	int i;
	MonoClass *ic;

	mono_class_setup_interfaces (klass, error);
	return_if_nok (error);

	MonoClass **klass_interfaces = m_class_get_interfaces (klass);
	for (i = 0; i < m_class_get_interface_count (klass); i++) {
		ic = klass_interfaces [i];

		if (*res == NULL)
			*res = g_ptr_array_new ();
		if (*ifaces == NULL)
			*ifaces = g_hash_table_new (NULL, NULL);
		if (g_hash_table_lookup (*ifaces, ic))
			continue;
		/* A gparam is not an implemented interface for the purposes of
		 * mono_class_get_implemented_interfaces */
		if (mono_class_is_gparam (ic))
			continue;
		g_ptr_array_add (*res, ic);
		g_hash_table_insert (*ifaces, ic, ic);
		mono_class_init_internal (ic);
		if (mono_class_has_failure (ic)) {
			mono_error_set_type_load_class (error, ic, "Error Loading class");
			return;
		}

		collect_implemented_interfaces_aux (ic, res, ifaces, error);
		return_if_nok (error);
	}
}

GPtrArray*
mono_class_get_implemented_interfaces (MonoClass *klass, MonoError *error)
{
	GPtrArray *res = NULL;
	GHashTable *ifaces = NULL;

	collect_implemented_interfaces_aux (klass, &res, &ifaces, error);
	if (ifaces)
		g_hash_table_destroy (ifaces);
	if (!is_ok (error)) {
		if (res)
			g_ptr_array_free (res, TRUE);
		return NULL;
	}
	return res;
}

/*FIXME verify all callers if they should switch to mono_class_interface_offset_with_variance*/
int
mono_class_interface_offset (MonoClass *klass, MonoClass *itf)
{
	int i;
	MonoClass **klass_interfaces_packed = m_class_get_interfaces_packed (klass);
	for (i = m_class_get_interface_offsets_count (klass) -1 ; i >= 0 ; i-- ){
		MonoClass *result = klass_interfaces_packed[i];
		if (m_class_get_interface_id(result) == m_class_get_interface_id(itf)) {
			return m_class_get_interface_offsets_packed (klass) [i];
		}
	}
	return -1;
}

/**
 * mono_class_interface_offset_with_variance:
 *
 * Return the interface offset of \p itf in \p klass. Sets \p non_exact_match to TRUE if the match required variance check
 * If \p itf is an interface with generic variant arguments, try to find the compatible one.
 *
 * Note that this function is responsible for resolving ambiguities. Right now we use whatever ordering interfaces_packed gives us.
 *
 * FIXME figure out MS disambiguation rules and fix this function.
 */
int
mono_class_interface_offset_with_variance (MonoClass *klass, MonoClass *itf, gboolean *non_exact_match)
{
	int i = mono_class_interface_offset (klass, itf);
	*non_exact_match = FALSE;
	if (i >= 0)
		return i;

	int klass_interface_offsets_count = m_class_get_interface_offsets_count (klass);

	if (m_class_is_array_special_interface  (itf) && m_class_get_rank (klass) < 2) {
		MonoClass *gtd = mono_class_get_generic_type_definition (itf);
		int found = -1;

		for (i = 0; i < klass_interface_offsets_count; i++) {
			if (mono_class_is_variant_compatible (itf, m_class_get_interfaces_packed (klass) [i], FALSE)) {
				found = i;
				*non_exact_match = TRUE;
				break;
			}

		}
		if (found != -1)
			return m_class_get_interface_offsets_packed (klass) [found];

		for (i = 0; i < klass_interface_offsets_count; i++) {
			if (mono_class_get_generic_type_definition (m_class_get_interfaces_packed (klass) [i]) == gtd) {
				found = i;
				*non_exact_match = TRUE;
				break;
			}
		}

		if (found == -1)
			return -1;

		return m_class_get_interface_offsets_packed (klass) [found];
	}

	if (!mono_class_has_variant_generic_params (itf))
		return -1;

	for (i = 0; i < klass_interface_offsets_count; i++) {
		if (mono_class_is_variant_compatible (itf, m_class_get_interfaces_packed (klass) [i], FALSE)) {
			*non_exact_match = TRUE;
			return m_class_get_interface_offsets_packed (klass) [i];
		}
	}

	return -1;
}


/*
 * mono_method_get_vtable_slot:
 *
 *   Returns method->slot, computing it if neccesary. Return -1 on failure.
 * LOCKING: Acquires the loader lock.
 *
 * FIXME Use proper MonoError machinery here.
 */
int
mono_method_get_vtable_slot (MonoMethod *method)
{
	if (method->slot == -1) {
		mono_class_setup_vtable (method->klass);
		if (mono_class_has_failure (method->klass))
			return -1;
		if (method->slot == -1) {
			MonoClass *gklass;
			int i, mcount;

			if (!mono_class_is_ginst (method->klass)) {
				g_assert (method->is_inflated);
				return mono_method_get_vtable_slot (((MonoMethodInflated*)method)->declaring);
			}

			/* This can happen for abstract methods of generic instances due to the shortcut code in mono_class_setup_vtable_general (). */
			g_assert (mono_class_is_ginst (method->klass));
			gklass = mono_class_get_generic_class (method->klass)->container_class;
			mono_class_setup_methods (method->klass);
			MonoMethod **klass_methods = m_class_get_methods (method->klass);
			g_assert (klass_methods);
			mcount = mono_class_get_method_count (method->klass);
			for (i = 0; i < mcount; ++i) {
				if (klass_methods [i] == method)
					break;
			}
			g_assert (i < mcount);
			g_assert (m_class_get_methods (gklass));
			method->slot = m_class_get_methods (gklass) [i]->slot;
		}
		g_assert (method->slot != -1);
	}
	return method->slot;
}

/**
 * mono_method_get_vtable_index:
 * \param method a method
 *
 * Returns the index into the runtime vtable to access the method or,
 * in the case of a virtual generic method, the virtual generic method
 * thunk. Returns -1 on failure.
 *
 * FIXME Use proper MonoError machinery here.
 */
int
mono_method_get_vtable_index (MonoMethod *method)
{
	if (method->is_inflated && (method->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		MonoMethodInflated *imethod = (MonoMethodInflated*)method;
		if (imethod->declaring->is_generic)
			return mono_method_get_vtable_slot (imethod->declaring);
	}
	return mono_method_get_vtable_slot (method);
}

/*
 * mono_class_has_finalizer:
 *
 *   Return whenever KLASS has a finalizer, initializing klass->has_finalizer in the
 * process.
 *
 * LOCKING: Acquires the loader lock;
 */
gboolean
mono_class_has_finalizer (MonoClass *klass)
{
	if (!m_class_is_has_finalize_inited (klass))
		mono_class_setup_has_finalizer (klass);

	return m_class_has_finalize (klass);
}

gboolean
mono_is_corlib_image (MonoImage *image)
{
	return image == mono_defaults.corlib;
}


/** Is klass a Nullable<T> ginst? */
gboolean
mono_class_is_nullable (MonoClass *klass)
{
	MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
	return gklass && gklass->container_class == mono_defaults.generic_nullable_class;
}

/** if klass is T? return T */
MonoClass*
mono_class_get_nullable_param_internal (MonoClass *klass)
{
	g_assert (mono_class_is_nullable (klass));
	return mono_class_from_mono_type_internal (mono_class_get_generic_class (klass)->context.class_inst->type_argv [0]);
}

MonoClass*
mono_class_get_nullable_param (MonoClass *klass)
{
	MonoClass *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_get_nullable_param_internal (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

gboolean
mono_type_is_primitive (MonoType *type)
{
	return (type->type >= MONO_TYPE_BOOLEAN && type->type <= MONO_TYPE_R8) ||
			type-> type == MONO_TYPE_I || type->type == MONO_TYPE_U;
}

static MonoImage *
get_image_for_container (MonoGenericContainer *container)
{
	MonoImage *result;
	if (container->is_anonymous) {
		result = container->owner.image;
	} else {
		MonoClass *klass;
		if (container->is_method) {
			MonoMethod *method = container->owner.method;
			g_assert_checked (method);
			klass = method->klass;
		} else {
			klass = container->owner.klass;
		}
		g_assert_checked (klass);
		result = m_class_get_image (klass);
	}
	g_assert (result);
	return result;
}

MonoImage *
mono_get_image_for_generic_param (MonoGenericParam *param)
{
	MonoGenericContainer *container = mono_generic_param_owner (param);
	g_assert_checked (container);
	return get_image_for_container (container);
}

// Make a string in the designated image consisting of a single integer.
#define INT_STRING_SIZE 16
char *
mono_make_generic_name_string (MonoImage *image, int num)
{
	char *name = (char *)mono_image_alloc0 (image, INT_STRING_SIZE);
	g_snprintf (name, INT_STRING_SIZE, "%d", num);
	return name;
}

/**
 * mono_class_from_generic_parameter:
 * \param param Parameter to find/construct a class for.
 * \param arg2 Is ignored.
 * \param arg3 Is ignored.
 */
MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *arg2 G_GNUC_UNUSED, gboolean arg3 G_GNUC_UNUSED)
{
	return mono_class_create_generic_parameter (param);
}

/**
 * mono_ptr_class_get:
 */
MonoClass *
mono_ptr_class_get (MonoType *type)
{
	return mono_class_create_ptr (type);
}

/**
 * mono_class_from_mono_type:
 * \param type describes the type to return
 * \returns a \c MonoClass for the specified \c MonoType, the value is never NULL.
 */
MonoClass *
mono_class_from_mono_type (MonoType *type)
{
	MonoClass *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_from_mono_type_internal (type);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoClass *
mono_class_from_mono_type_internal (MonoType *type)
{
	g_assert (type);
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
		return mono_class_create_bounded_array (type->data.array->eklass, type->data.array->rank, TRUE);
	case MONO_TYPE_PTR:
		return mono_class_create_ptr (type->data.type);
	case MONO_TYPE_FNPTR:
		return mono_class_create_fnptr (type->data.method);
	case MONO_TYPE_SZARRAY:
		return mono_class_create_array (type->data.klass, 1);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		return type->data.klass;
	case MONO_TYPE_GENERICINST:
		return mono_class_create_generic_inst (type->data.generic_class);
	case MONO_TYPE_MVAR:
	case MONO_TYPE_VAR:
		return mono_class_create_generic_parameter (type->data.generic_param);
	default:
		g_warning ("mono_class_from_mono_type_internal: implement me 0x%02x\n", type->type);
		g_assert_not_reached ();
	}

	// Yes, this returns NULL, even if it is documented as not doing so, but there
	// is no way for the code to make it this far, due to the assert above.
	return NULL;
}

/**
 * mono_type_retrieve_from_typespec
 * \param image context where the image is created
 * \param type_spec  typespec token
 * \param context the generic context used to evaluate generic instantiations in
 */
static MonoType *
mono_type_retrieve_from_typespec (MonoImage *image, guint32 type_spec, MonoGenericContext *context, gboolean *did_inflate, MonoError *error)
{
	MonoType *t = mono_type_create_from_typespec_checked (image, type_spec, error);

	*did_inflate = FALSE;

	if (!t)
		return NULL;

	if (context && (context->class_inst || context->method_inst)) {
		MonoType *inflated = inflate_generic_type (NULL, t, context, error);

		if (!is_ok (error)) {
			return NULL;
		}

		if (inflated) {
			t = inflated;
			*did_inflate = TRUE;
		}
	}
	return t;
}

/**
 * mono_class_create_from_typespec
 * \param image context where the image is created
 * \param type_spec typespec token
 * \param context the generic context used to evaluate generic instantiations in
 */
static MonoClass *
mono_class_create_from_typespec (MonoImage *image, guint32 type_spec, MonoGenericContext *context, MonoError *error)
{
	MonoClass *ret;
	gboolean inflated = FALSE;
	MonoType *t = mono_type_retrieve_from_typespec (image, type_spec, context, &inflated, error);
	return_val_if_nok (error, NULL);
	ret = mono_class_from_mono_type_internal (t);
	if (inflated)
		mono_metadata_free_type (t);
	return ret;
}

/**
 * mono_bounded_array_class_get:
 * \param element_class element class
 * \param rank the dimension of the array class
 * \param bounded whenever the array has non-zero bounds
 * \returns A class object describing the array with element type \p element_type and
 * dimension \p rank.
 */
MonoClass *
mono_bounded_array_class_get (MonoClass *eclass, guint32 rank, gboolean bounded)
{
	return mono_class_create_bounded_array (eclass, rank, bounded);
}

/**
 * mono_array_class_get:
 * \param element_class element class
 * \param rank the dimension of the array class
 * \returns A class object describing the array with element type \p element_type and
 * dimension \p rank.
 */
MonoClass *
mono_array_class_get (MonoClass *eclass, guint32 rank)
{
	return mono_class_create_array (eclass, rank);
}

/**
 * mono_class_instance_size:
 * \param klass a class
 *
 * Use to get the size of a class in bytes.
 *
 * \returns The size of an object instance
 */
gint32
mono_class_instance_size (MonoClass *klass)
{
	if (!m_class_is_size_inited (klass))
		mono_class_init_internal (klass);

	return m_class_get_instance_size (klass);
}

/**
 * mono_class_min_align:
 * \param klass a class
 *
 * Use to get the computed minimum alignment requirements for the specified class.
 *
 * Returns: minimum alignment requirements
 */
gint32
mono_class_min_align (MonoClass *klass)
{
	if (!m_class_is_size_inited (klass))
		mono_class_init_internal (klass);

	return m_class_get_min_align (klass);
}

/**
 * mono_class_data_size:
 * \param klass a class
 *
 * \returns The size of the static class data
 */
gint32
mono_class_data_size (MonoClass *klass)
{
	if (!m_class_is_inited (klass))
		mono_class_init_internal (klass);
	/* This can happen with dynamically created types */
	if (!m_class_is_fields_inited (klass))
		mono_class_setup_fields (klass);

	/* in arrays, sizes.class_size is unioned with element_size
	 * and arrays have no static fields
	 */
	if (m_class_get_rank (klass))
		return 0;
	return m_class_get_sizes (klass).class_size;
}

/*
 * Auxiliary routine to mono_class_get_field
 *
 * Takes a field index instead of a field token.
 */
static MonoClassField *
mono_class_get_field_idx (MonoClass *klass, int idx)
{
	mono_class_setup_fields (klass);
	if (mono_class_has_failure (klass))
		return NULL;

	while (klass) {
		int first_field_idx = mono_class_get_first_field_idx (klass);
		int fcount = mono_class_get_field_count (klass);
		MonoImage *klass_image = m_class_get_image (klass);
		MonoClassField *klass_fields = m_class_get_fields (klass);
		if (klass_image->uncompressed_metadata) {
			/*
			 * first_field_idx points to the FieldPtr table, while idx points into the
			 * Field table, so we have to do a search.
			 */
			/*FIXME this is broken for types with multiple fields with the same name.*/
			const char *name = mono_metadata_string_heap (klass_image, mono_metadata_decode_row_col (&klass_image->tables [MONO_TABLE_FIELD], idx, MONO_FIELD_NAME));
			int i;

			for (i = 0; i < fcount; ++i)
				if (mono_field_get_name (&klass_fields [i]) == name)
					return &klass_fields [i];
			g_assert_not_reached ();
		} else {
			if (fcount) {
				if ((idx >= first_field_idx) && (idx < first_field_idx + fcount)){
					return &klass_fields [idx - first_field_idx];
				}
			}
			if (G_UNLIKELY (m_class_get_image (klass)->has_updates && mono_class_has_metadata_update_info (klass))) {
				uint32_t token = mono_metadata_make_token (MONO_TABLE_FIELD, idx + 1);
				return mono_metadata_update_get_field (klass, token);
			}
		}
		klass = m_class_get_parent (klass);
	}
	return NULL;
}

/**
 * mono_class_get_field:
 * \param class the class to lookup the field.
 * \param field_token the field token
 *
 * \returns A \c MonoClassField representing the type and offset of
 * the field, or a NULL value if the field does not belong to this
 * class.
 */
MonoClassField *
mono_class_get_field (MonoClass *klass, guint32 field_token)
{
	int idx = mono_metadata_token_index (field_token);

	g_assert (mono_metadata_token_code (field_token) == MONO_TOKEN_FIELD_DEF);

	return mono_class_get_field_idx (klass, idx - 1);
}

/**
 * mono_class_get_field_from_name:
 * \param klass the class to lookup the field.
 * \param name the field name
 *
 * Search the class \p klass and its parents for a field with the name \p name.
 *
 * \returns The \c MonoClassField pointer of the named field or NULL
 */
MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name)
{
	MonoClassField *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_get_field_from_name_full (klass, name, NULL);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_get_field_from_name_full:
 * \param klass the class to lookup the field.
 * \param name the field name
 * \param type the type of the fields. This optional.
 *
 * Search the class \p klass and it's parents for a field with the name \p name and type \p type.
 *
 * If \p klass is an inflated generic type, the type comparison is done with the equivalent field
 * of its generic type definition.
 *
 * \returns The MonoClassField pointer of the named field or NULL
 */
MonoClassField *
mono_class_get_field_from_name_full (MonoClass *klass, const char *name, MonoType *type)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_class_setup_fields (klass);
	if (mono_class_has_failure (klass))
		return NULL;

	while (klass) {
		gpointer iter = NULL;
		MonoClassField *field;
		while ((field = mono_class_get_fields_internal (klass, &iter))) {
			if (strcmp (name, mono_field_get_name (field)) != 0)
				continue;

			if (type) {
				MonoType *field_type = mono_metadata_get_corresponding_field_from_generic_type_definition (field)->type;
				if (!mono_metadata_type_equal_full (type, field_type, TRUE))
					continue;
			}
			return field;
		}
		klass = m_class_get_parent (klass);
	}
	return NULL;
}

/**
 * mono_class_get_field_token:
 * \param field the field we need the token of
 *
 * Get the token of a field. Note that the tokesn is only valid for the image
 * the field was loaded from. Don't use this function for fields in dynamic types.
 *
 * \returns The token representing the field in the image it was loaded from.
 */
guint32
mono_class_get_field_token (MonoClassField *field)
{
	MonoClass *klass = m_field_get_parent (field);
	int i;

	mono_class_setup_fields (klass);

	while (klass) {
		MonoClassField *klass_fields = m_class_get_fields (klass);
		if (!klass_fields)
			return 0;
		int first_field_idx = mono_class_get_first_field_idx (klass);
		int fcount = mono_class_get_field_count (klass);
		for (i = 0; i < fcount; ++i) {
			if (&klass_fields [i] == field) {
				int idx = first_field_idx + i + 1;

				if (m_class_get_image (klass)->uncompressed_metadata)
					idx = mono_metadata_translate_token_index (m_class_get_image (klass), MONO_TABLE_FIELD, idx);
				return mono_metadata_make_token (MONO_TABLE_FIELD, idx);
			}
		}
		if (G_UNLIKELY (m_class_get_image (klass)->has_updates)) {
			/* TODO: metadata-update: check if the field was added. */
			g_assert_not_reached ();
		}
		klass = m_class_get_parent (klass);
	}

	g_assert_not_reached ();
	return 0;
}

static int
mono_field_get_index (MonoClassField *field)
{
	g_assert (!m_field_is_from_update (field));
	int index = field - m_class_get_fields (m_field_get_parent (field));
	g_assert (index >= 0 && index < mono_class_get_field_count (m_field_get_parent (field)));

	return index;
}

/*
 * mono_class_get_field_default_value:
 *
 * Return the default value of the field as a pointer into the metadata blob.
 */
const char*
mono_class_get_field_default_value (MonoClassField *field, MonoTypeEnum *def_type)
{
	guint32 cindex;
	guint32 constant_cols [MONO_CONSTANT_SIZE];
	int field_index;
	MonoClass *klass = m_field_get_parent (field);
	MonoFieldDefaultValue *def_values;

	g_assert (field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT);

	def_values = mono_class_get_field_def_values (klass);
	if (!def_values) {
		def_values = (MonoFieldDefaultValue *)mono_class_alloc0 (klass, sizeof (MonoFieldDefaultValue) * mono_class_get_field_count (klass));

		mono_class_set_field_def_values (klass, def_values);
	}

	field_index = mono_field_get_index (field);

	if (!def_values [field_index].data) {
		MonoImage *field_parent_image = m_class_get_image (m_field_get_parent (field));
		cindex = mono_metadata_get_constant_index (field_parent_image, mono_class_get_field_token (field), 0);
		if (!cindex)
			return NULL;

		g_assert (!(field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA));

		mono_metadata_decode_row (&field_parent_image->tables [MONO_TABLE_CONSTANT], cindex - 1, constant_cols, MONO_CONSTANT_SIZE);
		def_values [field_index].def_type = (MonoTypeEnum)constant_cols [MONO_CONSTANT_TYPE];
		mono_memory_barrier ();
		def_values [field_index].data = (const char *)mono_metadata_blob_heap (field_parent_image, constant_cols [MONO_CONSTANT_VALUE]);
	}

	*def_type = def_values [field_index].def_type;
	return def_values [field_index].data;
}

static int
mono_property_get_index (MonoProperty *prop)
{
	MonoClassPropertyInfo *info = mono_class_get_property_info (prop->parent);
	int index = prop - info->properties;

	g_assert (index >= 0 && index < info->count);

	return index;
}

/*
 * mono_class_get_property_default_value:
 *
 * Return the default value of the field as a pointer into the metadata blob.
 */
const char*
mono_class_get_property_default_value (MonoProperty *property, MonoTypeEnum *def_type)
{
	guint32 cindex;
	guint32 constant_cols [MONO_CONSTANT_SIZE];
	MonoClass *klass = property->parent;
	MonoImage *klass_image = m_class_get_image (klass);

	g_assert (property->attrs & PROPERTY_ATTRIBUTE_HAS_DEFAULT);
	/*
	 * We don't cache here because it is not used by C# so it's quite rare, but
	 * we still do the lookup in klass->ext because that is where the data
	 * is stored for dynamic assemblies.
	 */

	if (image_is_dynamic (klass_image)) {
		MonoClassPropertyInfo *info = mono_class_get_property_info (klass);
		int prop_index = mono_property_get_index (property);
		if (info->def_values && info->def_values [prop_index].data) {
			*def_type = info->def_values [prop_index].def_type;
			return info->def_values [prop_index].data;
		}
		return NULL;
	}
	cindex = mono_metadata_get_constant_index (klass_image, mono_class_get_property_token (property), 0);
	if (!cindex)
		return NULL;

	mono_metadata_decode_row (&klass_image->tables [MONO_TABLE_CONSTANT], cindex - 1, constant_cols, MONO_CONSTANT_SIZE);
	*def_type = (MonoTypeEnum)constant_cols [MONO_CONSTANT_TYPE];
	return (const char *)mono_metadata_blob_heap (klass_image, constant_cols [MONO_CONSTANT_VALUE]);
}

/**
 * mono_class_get_event_token:
 */
guint32
mono_class_get_event_token (MonoEvent *event)
{
	MonoClass *klass = event->parent;
	int i;

	while (klass) {
		MonoClassEventInfo *info = mono_class_get_event_info (klass);
		if (info) {
			for (i = 0; i < info->count; ++i) {
				/* TODO: metadata-update: get tokens for added props, too */
				g_assert (!m_event_is_from_update (&info->events[i]));
				if (&info->events [i] == event)
					return mono_metadata_make_token (MONO_TABLE_EVENT, info->first + i + 1);
			}
		}
		klass = m_class_get_parent (klass);
	}

	g_assert_not_reached ();
	return 0;
}

MonoProperty*
mono_class_get_property_from_name_internal (MonoClass *klass, const char *name)
{
	MONO_REQ_GC_UNSAFE_MODE;
	while (klass) {
		MonoProperty* p;
		gpointer iter = NULL;
		while ((p = mono_class_get_properties (klass, &iter))) {
			if (! strcmp (name, p->name))
				return p;
		}
		klass = m_class_get_parent (klass);
	}
	return NULL;
}

/**
 * mono_class_get_property_token:
 * \param prop MonoProperty to query
 *
 * \returns The ECMA token for the specified property.
 */
guint32
mono_class_get_property_token (MonoProperty *prop)
{
	MonoClass *klass = prop->parent;
	while (klass) {
		MonoProperty* p;
		int i = 0;
		gpointer iter = NULL;
		MonoClassPropertyInfo *info = mono_class_get_property_info (klass);
		while ((p = mono_class_get_properties (klass, &iter))) {
			/* TODO: metadata-update: get tokens for added props, too */
			g_assert (!m_property_is_from_update (p));
			if (&info->properties [i] == prop)
				return mono_metadata_make_token (MONO_TABLE_PROPERTY, info->first + i + 1);

			i ++;
		}
		klass = m_class_get_parent (klass);
	}

	g_assert_not_reached ();
	return 0;
}

/**
 * mono_class_name_from_token:
 */
char *
mono_class_name_from_token (MonoImage *image, guint32 type_token)
{
	const char *name, *nspace;
	if (image_is_dynamic (image))
		return g_strdup_printf ("DynamicType 0x%08x", type_token);

	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF: {
		guint tidx = mono_metadata_token_index (type_token);

		if (mono_metadata_table_bounds_check (image, MONO_TABLE_TYPEDEF, tidx))
			return g_strdup_printf ("Invalid type token 0x%08x", type_token);

		guint32 cols [MONO_TYPEDEF_SIZE];
		MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];

		mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);
		name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);
		if (strlen (nspace) == 0)
			return g_strdup_printf ("%s", name);
		else
			return g_strdup_printf ("%s.%s", nspace, name);
	}

	case MONO_TOKEN_TYPE_REF: {
		guint tidx = mono_metadata_token_index (type_token);

		if (mono_metadata_table_bounds_check (image, MONO_TABLE_TYPEREF, tidx))
			return g_strdup_printf ("Invalid type token 0x%08x", type_token);

		guint32 cols [MONO_TYPEREF_SIZE];
		MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];

		mono_metadata_decode_row (t, tidx-1, cols, MONO_TYPEREF_SIZE);
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
		return g_strdup_printf ("Invalid type token 0x%08x", type_token);
	}
}

static char *
mono_assembly_name_from_token (MonoImage *image, guint32 type_token)
{
	if (image_is_dynamic (image))
		return g_strdup_printf ("DynamicAssembly %s", image->name);

	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF:
		if (image->assembly)
			return mono_stringify_assembly_name (&image->assembly->aname);
		else if (image->assembly_name)
			return g_strdup (image->assembly_name);
		return g_strdup_printf ("%s", image->name ? image->name : "[Could not resolve assembly name");
	case MONO_TOKEN_TYPE_REF: {
		MonoAssemblyName aname;
		memset (&aname, 0, sizeof (MonoAssemblyName));
		guint32 cols [MONO_TYPEREF_SIZE];
		MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
		guint32 idx = mono_metadata_token_index (type_token);

		if (mono_metadata_table_bounds_check (image, MONO_TABLE_TYPEREF, idx))
			return g_strdup_printf ("Invalid type token 0x%08x", type_token);

		mono_metadata_decode_row (t, idx-1, cols, MONO_TYPEREF_SIZE);

		idx = cols [MONO_TYPEREF_SCOPE] >> MONO_RESOLUTION_SCOPE_BITS;
		switch (cols [MONO_TYPEREF_SCOPE] & MONO_RESOLUTION_SCOPE_MASK) {
		case MONO_RESOLUTION_SCOPE_MODULE:
			/* FIXME: */
			return g_strdup ("");
		case MONO_RESOLUTION_SCOPE_MODULEREF:
			/* FIXME: */
			return g_strdup ("");
		case MONO_RESOLUTION_SCOPE_TYPEREF:
			/* FIXME: */
			return g_strdup ("");
		case MONO_RESOLUTION_SCOPE_ASSEMBLYREF:
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
 * mono_class_get_full:
 * \param image the image where the class resides
 * \param type_token the token for the class
 * \param context the generic context used to evaluate generic instantiations in
 * \deprecated Functions that expose \c MonoGenericContext are going away in mono 4.0
 * \returns The \c MonoClass that represents \p type_token in \p image
 */
MonoClass *
mono_class_get_full (MonoImage *image, guint32 type_token, MonoGenericContext *context)
{
	ERROR_DECL (error);
	MonoClass *klass;
	klass = mono_class_get_checked (image, type_token, error);

	if (klass && context && mono_metadata_token_table (type_token) == MONO_TABLE_TYPESPEC)
		klass = mono_class_inflate_generic_class_checked (klass, context, error);

	mono_error_assert_ok (error);
	return klass;
}


MonoClass *
mono_class_get_and_inflate_typespec_checked (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error)
{
	MonoClass *klass;

	error_init (error);
	klass = mono_class_get_checked (image, type_token, error);

	if (klass && context && mono_metadata_token_table (type_token) == MONO_TABLE_TYPESPEC)
		klass = mono_class_inflate_generic_class_checked (klass, context, error);

	return klass;
}
/**
 * mono_class_get_checked:
 * \param image the image where the class resides
 * \param type_token the token for the class
 * \param error error object to return any error
 *
 * \returns The MonoClass that represents \p type_token in \p image, or NULL on error.
 */
MonoClass *
mono_class_get_checked (MonoImage *image, guint32 type_token, MonoError *error)
{
	MonoClass *klass = NULL;

	error_init (error);

	if (image_is_dynamic (image)) {
		int table = mono_metadata_token_table (type_token);

		if (table != MONO_TABLE_TYPEDEF && table != MONO_TABLE_TYPEREF && table != MONO_TABLE_TYPESPEC) {
			mono_error_set_bad_image (error, image,"Bad token table for dynamic image: %x", table);
			return NULL;
		}
		klass = (MonoClass *)mono_lookup_dynamic_token (image, type_token, NULL, error);
		goto done;
	}

	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF:
		klass = mono_class_create_from_typedef (image, type_token, error);
		break;
	case MONO_TOKEN_TYPE_REF:
		klass = mono_class_from_typeref_checked (image, type_token, error);
		break;
	case MONO_TOKEN_TYPE_SPEC:
		klass = mono_class_create_from_typespec (image, type_token, NULL, error);
		break;
	default:
		mono_error_set_bad_image (error, image, "Unknown type token %x", type_token & 0xff000000);
	}

done:
	/* Generic case, should be avoided for when a better error is possible. */
	if (!klass && is_ok (error)) {
		char *name = mono_class_name_from_token (image, type_token);
		char *assembly = mono_assembly_name_from_token (image, type_token);
		mono_error_set_type_load_name (error, name, assembly, "Could not resolve type with token %08x (expected class '%s' in assembly '%s')", type_token, name, assembly);
	}

	return klass;
}


/**
 * mono_type_get_checked:
 * \param image the image where the type resides
 * \param type_token the token for the type
 * \param context the generic context used to evaluate generic instantiations in
 * \param error Error handling context
 *
 * This functions exists to fullfill the fact that sometimes it's desirable to have access to the
 *
 * \returns The MonoType that represents \p type_token in \p image
 */
MonoType *
mono_type_get_checked (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error)
{
	MonoType *type = NULL;
	gboolean inflated = FALSE;

	error_init (error);

	//FIXME: this will not fix the very issue for which mono_type_get_full exists -but how to do it then?
	if (image_is_dynamic (image)) {
		MonoClass *klass = (MonoClass *)mono_lookup_dynamic_token (image, type_token, context, error);
		return_val_if_nok (error, NULL);
		return m_class_get_byval_arg (klass);
	}

	if ((type_token & 0xff000000) != MONO_TOKEN_TYPE_SPEC) {
		MonoClass *klass = mono_class_get_checked (image, type_token, error);

		if (!klass)
			return NULL;
		if (m_class_has_failure (klass)) {
			mono_error_set_for_class_failure (error, klass);
			return NULL;
		}
		return m_class_get_byval_arg (klass);
	}

	type = mono_type_retrieve_from_typespec (image, type_token, context, &inflated, error);

	if (!type) {
		return NULL;
	}

	if (inflated) {
		MonoType *tmp = type;
		type = m_class_get_byval_arg (mono_class_from_mono_type_internal (type));
		/* FIXME: This is a workaround fo the fact that a typespec token sometimes reference to the generic type definition.
		 * A MonoClass::_byval_arg of a generic type definion has type CLASS.
		 * Some parts of mono create a GENERICINST to reference a generic type definition and this generates confict with _byval_arg.
		 *
		 * The long term solution is to chaise this places and make then set MonoType::type correctly.
		 * */
		if (type->type != tmp->type)
			type = tmp;
		else
			mono_metadata_free_type (tmp);
	}
	return type;
}

/**
 * mono_class_get:
 * \param image image where the class token will be looked up.
 * \param type_token a type token from the image
 * \returns the \c MonoClass with the given \p type_token on the \p image
 */
MonoClass *
mono_class_get (MonoImage *image, guint32 type_token)
{
	MonoClass *result;
	MONO_ENTER_GC_UNSAFE;
	ERROR_DECL (error);
	result = mono_class_get_checked (image, type_token, error);
	mono_error_assert_ok (error);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_image_init_name_cache:
 *
 *  Initializes the class name cache stored in image->name_cache.
 *
 * LOCKING: Acquires the corresponding image lock.
 */
void
mono_image_init_name_cache (MonoImage *image)
{
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	const char *name;
	const char *nspace;
	guint32 i, visib, nspace_index;
	GHashTable *name_cache2, *nspace_table, *the_name_cache;

	if (image->name_cache)
		return;

	the_name_cache = g_hash_table_new (g_str_hash, g_str_equal);

	if (image_is_dynamic (image)) {
		mono_image_lock (image);
		if (image->name_cache) {
			/* Somebody initialized it before us */
			g_hash_table_destroy (the_name_cache);
		} else {
			mono_atomic_store_release (&image->name_cache, the_name_cache);
		}
		mono_image_unlock (image);
		return;
	}

	/* Temporary hash table to avoid lookups in the nspace_table */
	name_cache2 = g_hash_table_new (NULL, NULL);

	/* FIXME: metadata-update */
	int rows = table_info_get_rows (t);
	for (i = 1; i <= rows; ++i) {
		mono_metadata_decode_row (t, i - 1, cols, MONO_TYPEDEF_SIZE);
		visib = cols [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		/*
		 * Nested types are accessed from the nesting name.  We use the fact that nested types use different visibility flags
		 * than toplevel types, thus avoiding the need to grovel through the NESTED_TYPE table
		 */
		if (visib >= TYPE_ATTRIBUTE_NESTED_PUBLIC && visib <= TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM)
			continue;
		name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);

		nspace_index = cols [MONO_TYPEDEF_NAMESPACE];
		nspace_table = (GHashTable *)g_hash_table_lookup (name_cache2, GUINT_TO_POINTER (nspace_index));
		if (!nspace_table) {
			nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
			g_hash_table_insert (the_name_cache, (char*)nspace, nspace_table);
			g_hash_table_insert (name_cache2, GUINT_TO_POINTER (nspace_index),
								 nspace_table);
		}
		g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (i));
	}

	/* Load type names from EXPORTEDTYPES table */
	{
		MonoTableInfo  *t = &image->tables [MONO_TABLE_EXPORTEDTYPE];
		guint32 cols [MONO_EXP_TYPE_SIZE];
		int i;

		rows = table_info_get_rows (t);
		for (i = 0; i < rows; ++i) {
			mono_metadata_decode_row (t, i, cols, MONO_EXP_TYPE_SIZE);

			guint32 impl = cols [MONO_EXP_TYPE_IMPLEMENTATION];
			if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_EXP_TYPE)
				/* Nested type */
				continue;

			name = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAME]);
			nspace = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAMESPACE]);

			nspace_index = cols [MONO_EXP_TYPE_NAMESPACE];
			nspace_table = (GHashTable *)g_hash_table_lookup (name_cache2, GUINT_TO_POINTER (nspace_index));
			if (!nspace_table) {
				nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
				g_hash_table_insert (the_name_cache, (char*)nspace, nspace_table);
				g_hash_table_insert (name_cache2, GUINT_TO_POINTER (nspace_index),
									 nspace_table);
			}
			g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (mono_metadata_make_token (MONO_TABLE_EXPORTEDTYPE, i + 1)));
		}
	}

	g_hash_table_destroy (name_cache2);

	mono_image_lock (image);
	if (image->name_cache) {
		/* Somebody initialized it before us */
		g_hash_table_destroy (the_name_cache);
	} else {
		mono_atomic_store_release (&image->name_cache, the_name_cache);
	}
	mono_image_unlock (image);
}

/*FIXME Only dynamic assemblies or metadata-update should allow this operation.*/
/**
 * mono_image_add_to_name_cache:
 */
void
mono_image_add_to_name_cache (MonoImage *image, const char *nspace,
							  const char *name, guint32 index)
{
	GHashTable *nspace_table;
	GHashTable *name_cache;
	guint32 old_index;

	mono_image_init_name_cache (image);
	mono_image_lock (image);

	name_cache = image->name_cache;
	if (!(nspace_table = (GHashTable *)g_hash_table_lookup (name_cache, nspace))) {
		nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
		g_hash_table_insert (name_cache, (char *)nspace, (char *)nspace_table);
	}

	if ((old_index = GPOINTER_TO_UINT (g_hash_table_lookup (nspace_table, (char*) name))))
		g_error ("overrwritting old token %x on image %s for type %s::%s", old_index, image->name, nspace, name);

	g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (index));

	mono_image_unlock (image);
}

typedef struct {
	gconstpointer key;
	GSList *values;
} FindAllUserData;

static void
find_all_nocase (gpointer key, gpointer value, gpointer user_data)
{
	char *name = (char*)key;
	FindAllUserData *data = (FindAllUserData*)user_data;
	if (mono_utf8_strcasecmp (name, (char*)data->key) == 0)
		data->values = g_slist_prepend (data->values, value);
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

	if (!data->value && (mono_utf8_strcasecmp (name, (char*)data->key) == 0))
		data->value = value;
}

/**
 * mono_class_from_name_case:
 * \param image The MonoImage where the type is looked up in
 * \param name_space the type namespace
 * \param name the type short name.
 * \deprecated use the mono_class_from_name_case_checked variant instead.
 *
 * Obtains a \c MonoClass with a given namespace and a given name which
 * is located in the given \c MonoImage.   The namespace and name
 * lookups are case insensitive.
 */
MonoClass *
mono_class_from_name_case (MonoImage *image, const char* name_space, const char *name)
{
	ERROR_DECL (error);
	MonoClass *res = mono_class_from_name_case_checked (image, name_space, name, error);
	mono_error_cleanup (error);

	return res;
}

/**
 * mono_class_from_name_case_checked:
 * \param image The MonoImage where the type is looked up in
 * \param name_space the type namespace
 * \param name the type short name.
 * \param error if
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.   The namespace and name
 * lookups are case insensitive.
 *
 * \returns The MonoClass if the given namespace and name were found, or NULL if it
 * was not found.   The \p error object will contain information about the problem
 * in that case.
 */
MonoClass *
mono_class_from_name_case_checked (MonoImage *image, const char *name_space, const char *name, MonoError *error)
{
	MonoClass *klass;
	GHashTable *visited_images;

	visited_images = g_hash_table_new (g_direct_hash, g_direct_equal);

	klass = mono_class_from_name_checked_aux (image, name_space, name, visited_images, FALSE, error);

	g_hash_table_destroy (visited_images);

	return klass;
}

static MonoClass*
return_nested_in (MonoClass *klass, char *nested, gboolean case_sensitive)
{
	MonoClass *found;
	char *s = strchr (nested, '/');
	gpointer iter = NULL;

	if (s) {
		*s = 0;
		s++;
	}

	while ((found = mono_class_get_nested_types (klass, &iter))) {
		const char *name = m_class_get_name (found);
		gint strcmp_result;
		if (case_sensitive)
			strcmp_result = strcmp (name, nested);
		else
			strcmp_result = mono_utf8_strcasecmp (name, nested);

		if (strcmp_result == 0) {
			if (s)
				return return_nested_in (found, s, case_sensitive);
			return found;
		}
	}
	return NULL;
}

static MonoClass*
search_modules (MonoImage *image, const char *name_space, const char *name, gboolean case_sensitive, MonoError *error)
{
	MonoTableInfo *file_table = &image->tables [MONO_TABLE_FILE];
	MonoImage *file_image;
	MonoClass *klass;
	int i;

	error_init (error);

	/*
	 * The EXPORTEDTYPES table only contains public types, so have to search the
	 * modules as well.
	 * Note: image->modules contains the contents of the MODULEREF table, while
	 * the real module list is in the FILE table.
	 */
	int rows = table_info_get_rows (file_table);
	for (i = 0; i < rows; i++) {
		guint32 cols [MONO_FILE_SIZE];
		mono_metadata_decode_row (file_table, i, cols, MONO_FILE_SIZE);
		if (cols [MONO_FILE_FLAGS] == FILE_CONTAINS_NO_METADATA)
			continue;

		file_image = mono_image_load_file_for_image_checked (image, i + 1, error);
		if (file_image) {
			if (case_sensitive)
				klass = mono_class_from_name_checked (file_image, name_space, name, error);
			else
				klass = mono_class_from_name_case_checked (file_image, name_space, name, error);

			if (klass || !is_ok (error))
				return klass;
		}
	}

	return NULL;
}

static MonoClass *
mono_class_from_name_checked_aux (MonoImage *image, const char* name_space, const char *name, GHashTable* visited_images, gboolean case_sensitive, MonoError *error)
{
	GHashTable *nspace_table = NULL;
	MonoImage *loaded_image = NULL;
	guint32 token = 0;
	int i;
	MonoClass *klass;
	char *nested;
	char buf [1024];

	error_init (error);

	// Checking visited images avoids stack overflows when cyclic references exist.
	if (g_hash_table_lookup (visited_images, image))
		return NULL;

	g_hash_table_insert (visited_images, image, GUINT_TO_POINTER(1));

	if ((nested = (char*)strchr (name, '/'))) {
		int pos = nested - name;
		size_t len = strlen (name);
		if (len > 1023)
			return NULL;
		memcpy (buf, name, len + 1);
		buf [pos] = 0;
		nested = buf + pos + 1;
		name = buf;
	}

	/* FIXME: get_class_from_name () can't handle types in the EXPORTEDTYPE table */
	// The AOT cache in get_class_from_name is case-sensitive, so don't bother with it for case-insensitive lookups
	if (get_class_from_name && table_info_get_rows (&image->tables [MONO_TABLE_EXPORTEDTYPE]) == 0 && case_sensitive) {
		gboolean res = get_class_from_name (image, name_space, name, &klass);
		if (res) {
			if (!klass) {
				klass = search_modules (image, name_space, name, case_sensitive, error);
				if (!is_ok (error))
					return NULL;
			}
			if (nested)
				return klass ? return_nested_in (klass, nested, case_sensitive) : NULL;
			else
				return klass;
		}
	}

	mono_image_init_name_cache (image);
	mono_image_lock (image);

	if (case_sensitive) {
		nspace_table = (GHashTable *)g_hash_table_lookup (image->name_cache, name_space);

		if (nspace_table)
			token = GPOINTER_TO_UINT (g_hash_table_lookup (nspace_table, name));
	} else {
		FindAllUserData all_user_data = { name_space, NULL };
		FindUserData user_data = { name, NULL };
		GSList *values;

		// We're forced to check all matching namespaces, not just the first one found,
		// because our desired type could be in any of the ones that match case-insensitively.
		g_hash_table_foreach (image->name_cache, find_all_nocase, &all_user_data);

		values = all_user_data.values;
		while (values && !user_data.value) {
			nspace_table = (GHashTable*)values->data;
			g_hash_table_foreach (nspace_table, find_nocase, &user_data);
			values = values->next;
		}

		g_slist_free (all_user_data.values);

		if (user_data.value)
			token = GPOINTER_TO_UINT (user_data.value);
	}

	mono_image_unlock (image);

	if (!token && image_is_dynamic (image) && image->modules) {
		/* Search modules as well */
		for (i = 0; i < image->module_count; ++i) {
			MonoImage *module = image->modules [i];

			if (case_sensitive)
				klass = mono_class_from_name_checked (module, name_space, name, error);
			else
				klass = mono_class_from_name_case_checked (module, name_space, name, error);

			if (klass || !is_ok (error))
				return klass;
		}
	}

	if (!token) {
		klass = search_modules (image, name_space, name, case_sensitive, error);
		if (klass || !is_ok (error))
			return klass;
		return NULL;
	}

	if (mono_metadata_token_table (token) == MONO_TABLE_EXPORTEDTYPE) {
		MonoTableInfo  *t = &image->tables [MONO_TABLE_EXPORTEDTYPE];
		guint32 cols [MONO_EXP_TYPE_SIZE];
		guint32 idx, impl;

		idx = mono_metadata_token_index (token);

		mono_metadata_decode_row (t, idx - 1, cols, MONO_EXP_TYPE_SIZE);

		impl = cols [MONO_EXP_TYPE_IMPLEMENTATION];
		if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_FILE) {
			loaded_image = mono_assembly_load_module_checked (image->assembly, impl >> MONO_IMPLEMENTATION_BITS, error);
			if (!loaded_image)
				return NULL;
			klass = mono_class_from_name_checked_aux (loaded_image, name_space, name, visited_images, case_sensitive, error);
			if (nested)
				return klass ? return_nested_in (klass, nested, case_sensitive) : NULL;
			return klass;
		} else if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_ASSEMBLYREF) {
			guint32 assembly_idx;

			assembly_idx = impl >> MONO_IMPLEMENTATION_BITS;

			mono_assembly_load_reference (image, assembly_idx - 1);
			g_assert (image->references [assembly_idx - 1]);
			if (image->references [assembly_idx - 1] == (gpointer)-1)
				return NULL;
			klass = mono_class_from_name_checked_aux (image->references [assembly_idx - 1]->image, name_space, name, visited_images, case_sensitive, error);
			if (nested)
				return klass ? return_nested_in (klass, nested, case_sensitive) : NULL;
			return klass;
		} else {
			g_assert_not_reached ();
		}
	}

	token = MONO_TOKEN_TYPE_DEF | token;

	klass = mono_class_get_checked (image, token, error);
	if (nested)
		return return_nested_in (klass, nested, case_sensitive);
	return klass;
}

/**
 * mono_class_from_name_checked:
 * \param image The MonoImage where the type is looked up in
 * \param name_space the type namespace
 * \param name the type short name.
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.
 *
 * Works like mono_class_from_name, but error handling is tricky. It can return NULL and have no error
 * set if the class was not found or it will return NULL and set the error if there was a loading error.
 */
MonoClass *
mono_class_from_name_checked (MonoImage *image, const char* name_space, const char *name, MonoError *error)
{
	MonoClass *klass;
	GHashTable *visited_images;

	visited_images = g_hash_table_new (g_direct_hash, g_direct_equal);

	klass = mono_class_from_name_checked_aux (image, name_space, name, visited_images, TRUE, error);

	g_hash_table_destroy (visited_images);

	return klass;
}

/**
 * mono_class_from_name:
 * \param image The \c MonoImage where the type is looked up in
 * \param name_space the type namespace
 * \param name the type short name.
 *
 * Obtains a \c MonoClass with a given namespace and a given name which
 * is located in the given \c MonoImage.
 *
 * To reference nested classes, use the "/" character as a separator.
 * For example use \c "Foo/Bar" to reference the class \c Bar that is nested
 * inside \c Foo, like this: "class Foo { class Bar {} }".
 */
MonoClass *
mono_class_from_name (MonoImage *image, const char* name_space, const char *name)
{
	MonoClass *klass;
	MONO_ENTER_GC_UNSAFE;
	ERROR_DECL (error);

	klass = mono_class_from_name_checked (image, name_space, name, error);
	mono_error_cleanup (error); /* FIXME Don't swallow the error */
	MONO_EXIT_GC_UNSAFE;
	return klass;
}

/**
 * mono_class_load_from_name:
 * \param image The MonoImage where the type is looked up in
 * \param name_space the type namespace
 * \param name the type short name.
 *
 * This function works exactly like mono_class_from_name but it will abort if the class is not found.
 * This function should be used by the runtime for critical types to which there's no way to recover but crash
 * if they are missing. For example, System.Object or System.String.
 */
MonoClass *
mono_class_load_from_name (MonoImage *image, const char* name_space, const char *name)
{
	ERROR_DECL (error);
	MonoClass *klass;

	klass = mono_class_from_name_checked (image, name_space, name, error);
	if (!klass)
		g_error ("Runtime critical type %s.%s not found", name_space, name);
	mono_error_assertf_ok (error, "Could not load runtime critical type %s.%s", name_space, name);
	return klass;
}

/**
 * mono_class_try_load_from_name:
 * \param image The MonoImage where the type is looked up in
 * \param name_space the type namespace
 * \param name the type short name.
 *
 * This function tries to load a type, returning the class was found or NULL otherwise.
 * This function should be used by the runtime when probing for optional types, those that could have being linked out.
 *
 * Big design consideration. This function aborts if there was an error loading the type. This prevents us from missing
 * a type that we would otherwise assume to be available but was not due some error.
 *
 */
MonoClass*
mono_class_try_load_from_name (MonoImage *image, const char* name_space, const char *name)
{
	ERROR_DECL (error);
	MonoClass *klass;

	klass = mono_class_from_name_checked (image, name_space, name, error);
	mono_error_assertf_ok (error, "Could not load runtime critical type %s.%s", name_space, name);
	return klass;
}

static gboolean
mono_interface_implements_interface (MonoClass *interface_implementer, MonoClass *interface_implemented)
{
	int i;
	ERROR_DECL (error);
	mono_class_setup_interfaces (interface_implementer, error);
	if (!is_ok (error)) {
		mono_error_cleanup  (error);
		return FALSE;
	}
	MonoClass **klass_interfaces = m_class_get_interfaces (interface_implementer);
	for (i = 0; i < m_class_get_interface_count (interface_implementer); i++) {
		MonoClass *ic = klass_interfaces [i];
		if (mono_class_is_ginst (ic))
			ic = mono_class_get_generic_type_definition (ic);
		if (ic == interface_implemented)
			return TRUE;
	}
	return FALSE;
}

gboolean
mono_class_is_subclass_of_internal (MonoClass *klass, MonoClass *klassc,
				    gboolean check_interfaces)
{
	MONO_REQ_GC_UNSAFE_MODE;
	/* FIXME test for interfaces with variant generic arguments */
	if (check_interfaces) {
		mono_class_init_internal (klass);
		mono_class_init_internal (klassc);
	}

	if (check_interfaces && MONO_CLASS_IS_INTERFACE_INTERNAL (klassc) && !MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (klass, m_class_get_interface_id (klassc)))
			return TRUE;
	} else if (check_interfaces && MONO_CLASS_IS_INTERFACE_INTERNAL (klassc) && MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {
		int i;

		MonoClass **klass_interfaces = m_class_get_interfaces (klass);
		for (i = 0; i < m_class_get_interface_count (klass); i ++) {
			MonoClass *ic =  klass_interfaces [i];
			if (ic == klassc)
				return TRUE;
		}
	} else {
		if (!MONO_CLASS_IS_INTERFACE_INTERNAL (klass) && mono_class_has_parent (klass, klassc))
			return TRUE;
	}

	/*
	 * MS.NET thinks interfaces are a subclass of Object, so we think it as
	 * well.
	 */
	if (klassc == mono_defaults.object_class)
		return TRUE;

	return FALSE;
}

static gboolean
mono_type_is_generic_argument (MonoType *type)
{
	return type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR;
}

gboolean
mono_class_has_variant_generic_params (MonoClass *klass)
{
	int i;
	MonoGenericContainer *container;

	if (!mono_class_is_ginst (klass))
		return FALSE;

	container = mono_class_get_generic_container (mono_class_get_generic_class (klass)->container_class);

	for (i = 0; i < container->type_argc; ++i)
		if (mono_generic_container_get_param_info (container, i)->flags & (MONO_GEN_PARAM_VARIANT|MONO_GEN_PARAM_COVARIANT))
			return TRUE;

	return FALSE;
}

static gboolean
mono_gparam_is_reference_conversible (MonoClass *target, MonoClass *candidate, gboolean check_for_reference_conv)
{
	if (target == candidate)
		return TRUE;

	if (check_for_reference_conv &&
		mono_type_is_generic_argument (m_class_get_byval_arg (target)) &&
		mono_type_is_generic_argument (m_class_get_byval_arg (candidate))) {
		MonoGenericParam *gparam = m_class_get_byval_arg (candidate)->data.generic_param;
		MonoGenericParamInfo *pinfo = mono_generic_param_info (gparam);

		if (!pinfo || (pinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) == 0)
			return FALSE;
	}
	if (!mono_class_is_assignable_from_internal (target, candidate))
		return FALSE;
	return TRUE;
}

/**
 * @container the generic container from the GTD
 * @klass: the class to be assigned to
 * @oklass: the source class
 *
 * Both @klass and @oklass must be instances of the same generic interface.
 *
 * Returns: TRUE if @klass can be assigned to a @klass variable
 */
gboolean
mono_class_is_variant_compatible (MonoClass *klass, MonoClass *oklass, gboolean check_for_reference_conv)
{
	int j;
	MonoType **klass_argv, **oklass_argv;
	MonoClass *klass_gtd = mono_class_get_generic_type_definition (klass);
	MonoGenericContainer *container = mono_class_get_generic_container (klass_gtd);

	if (klass == oklass)
		return TRUE;

	/*Viable candidates are instances of the same generic interface*/
	if (mono_class_get_generic_type_definition (oklass) != klass_gtd || oklass == klass_gtd)
		return FALSE;

	klass_argv = &mono_class_get_generic_class (klass)->context.class_inst->type_argv [0];
	oklass_argv = &mono_class_get_generic_class (oklass)->context.class_inst->type_argv [0];

	for (j = 0; j < container->type_argc; ++j) {
		MonoClass *param1_class = mono_class_from_mono_type_internal (klass_argv [j]);
		MonoClass *param2_class = mono_class_from_mono_type_internal (oklass_argv [j]);

		if (m_class_is_valuetype (param1_class) != m_class_is_valuetype (param2_class) || (m_class_is_valuetype (param1_class) && param1_class != param2_class))
			return FALSE;

		/*
		 * The _VARIANT and _COVARIANT constants should read _COVARIANT and
		 * _CONTRAVARIANT, but they are in a public header so we can't fix it.
		 */
		if (param1_class != param2_class) {
			if (mono_generic_container_get_param_info (container, j)->flags & MONO_GEN_PARAM_VARIANT) {
				if (!mono_gparam_is_reference_conversible (param1_class, param2_class, check_for_reference_conv))
					return FALSE;
			} else if (mono_generic_container_get_param_info (container, j)->flags & MONO_GEN_PARAM_COVARIANT) {
				if (!mono_gparam_is_reference_conversible (param2_class, param1_class, check_for_reference_conv))
					return FALSE;
			} else
				return FALSE;
		}
	}
	return TRUE;
}

static gboolean
mono_gparam_is_assignable_from (MonoClass *target, MonoClass *candidate)
{
	MonoGenericParam *gparam, *ogparam;
	MonoGenericParamInfo *tinfo, *cinfo;
	MonoClass **candidate_class;
	gboolean class_constraint_satisfied, valuetype_constraint_satisfied;
	int tmask, cmask;

	if (target == candidate)
		return TRUE;
	MonoType *target_byval_arg = m_class_get_byval_arg (target);
	MonoType *candidate_byval_arg = m_class_get_byval_arg (candidate);
	if (target_byval_arg->type != candidate_byval_arg->type)
		return FALSE;

	gparam = target_byval_arg->data.generic_param;
	ogparam = candidate_byval_arg->data.generic_param;
	tinfo = mono_generic_param_info (gparam);
	cinfo = mono_generic_param_info (ogparam);

	class_constraint_satisfied = FALSE;
	valuetype_constraint_satisfied = FALSE;

	/*candidate must have a super set of target's special constraints*/
	tmask = tinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK;
	cmask = cinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK;

	if (cinfo->constraints) {
		for (candidate_class = cinfo->constraints; *candidate_class; ++candidate_class) {
			MonoClass *cc = *candidate_class;
			MonoType *cc_byval_arg = m_class_get_byval_arg (cc);

			if (mono_type_is_reference (cc_byval_arg) && !MONO_CLASS_IS_INTERFACE_INTERNAL (cc))
				class_constraint_satisfied = TRUE;
			else if (!mono_type_is_reference (cc_byval_arg) && !MONO_CLASS_IS_INTERFACE_INTERNAL (cc))
				valuetype_constraint_satisfied = TRUE;
		}
	}
	class_constraint_satisfied |= (cmask & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) != 0;
	valuetype_constraint_satisfied |= (cmask & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) != 0;

	if ((tmask & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) && !class_constraint_satisfied)
		return FALSE;
	if ((tmask & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) && !valuetype_constraint_satisfied)
		return FALSE;
	if ((tmask & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) && !((cmask & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) ||
		valuetype_constraint_satisfied)) {
		return FALSE;
	}


	/*candidate type constraints must be a superset of target's*/
	if (tinfo->constraints) {
		MonoClass **target_class;
		for (target_class = tinfo->constraints; *target_class; ++target_class) {
			MonoClass *tc = *target_class;
			MonoType *tc_byval_arg = m_class_get_byval_arg (tc);

			/*
			 * A constraint from @target might inflate into @candidate itself and in that case we don't need
			 * check it's constraints since it satisfy the constraint by itself.
			 */
			if (mono_metadata_type_equal (tc_byval_arg, candidate_byval_arg))
				continue;

			if (!cinfo->constraints)
				return FALSE;

			for (candidate_class = cinfo->constraints; *candidate_class; ++candidate_class) {
				MonoClass *cc = *candidate_class;

				if (mono_class_is_assignable_from_internal (tc, cc))
					break;

				/*
				 * This happens when we have the following:
				 *
				 * Bar<K> where K : IFace
				 * Foo<T, U> where T : U where U : IFace
				 * 	...
				 * 	Bar<T> <- T here satisfy K constraint transitively through to U's constraint
				 *
				 */
				if (mono_type_is_generic_argument (m_class_get_byval_arg (cc))) {
					if (mono_gparam_is_assignable_from (target, cc))
						break;
				}
			}
			if (!*candidate_class)
				return FALSE;
		}
	}

	/*candidate itself must have a constraint that satisfy target*/
	if (cinfo->constraints) {
		for (candidate_class = cinfo->constraints; *candidate_class; ++candidate_class) {
			MonoClass *cc = *candidate_class;
			if (mono_class_is_assignable_from_internal (target, cc))
				return TRUE;
		}
	}
	return FALSE;
}

static MonoType*
mono_type_get_underlying_type_ignore_byref (MonoType *type)
{
	if (type->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (type->data.klass))
		return mono_class_enum_basetype_internal (type->data.klass);
	if (type->type == MONO_TYPE_GENERICINST && m_class_is_enumtype (type->data.generic_class->container_class))
		return mono_class_enum_basetype_internal (type->data.generic_class->container_class);
	return type;
}

/**
 * mono_byref_type_is_assignable_from:
 * \param type The type assignee
 * \param ctype The type being assigned
 * \param signature_assignment whether this is a signature assginment check according to ECMA rules, or reflection
 *
 * Given two byref types, returns \c TRUE if values of the second type are assignable to locations of the first type.
 *
 * The \p signature_assignment parameter affects comparing T& and U& where T and U are both reference types.  Reflection
 * does an IsAssignableFrom check for T and U here, but ECMA I.8.7.2 says that the verification types of T and U must be
 * identical. If \p signature_assignment is \c TRUE we do an ECMA check, otherwise, reflection.
 */
gboolean
mono_byref_type_is_assignable_from (MonoType *type, MonoType *ctype, gboolean signature_assignment)
{
	g_assert (m_type_is_byref (type));
	g_assert (m_type_is_byref (ctype));
	MonoType *t = mono_type_get_underlying_type_ignore_byref (type);
	MonoType *ot = mono_type_get_underlying_type_ignore_byref (ctype);

	MonoClass *klass = mono_class_from_mono_type_internal (t);
	MonoClass *klassc = mono_class_from_mono_type_internal (ot);

	if (mono_type_is_primitive (t)) {
		return mono_type_is_primitive (ot) && m_class_get_instance_size (klass) == m_class_get_instance_size (klassc);
	} else if (t->type == MONO_TYPE_VAR || t->type == MONO_TYPE_MVAR) {
		return t->type == ot->type && t->data.generic_param->num == ot->data.generic_param->num;
	} else if (t->type == MONO_TYPE_PTR || t->type == MONO_TYPE_FNPTR) {
		return t->type == ot->type;
	} else {
		if (ot->type == MONO_TYPE_VAR || ot->type == MONO_TYPE_MVAR)
			return FALSE;

		if (m_class_is_valuetype (klass))
			return klass == klassc;
		if (m_class_is_valuetype (klassc))
			return FALSE;
		/*
		 * assignment compatability for location types, ECMA I.8.7.2 - two managed pointer types T& and U& are
		 * assignment compatible if the verification types of T and U are identical.
		 */
		if (signature_assignment)
			return klass == klassc;
		/* the reflection IsAssignableFrom does a subtype comparison here for reference types only */
		return mono_class_is_assignable_from_internal (klass, klassc);
	}
}

/**
 * mono_class_is_assignable_from_internal:
 * \param klass the class to be assigned to
 * \param oklass the source class
 *
 * \returns TRUE if an instance of class \p oklass can be assigned to an
 * instance of class \p klass
 */
gboolean
mono_class_is_assignable_from_internal (MonoClass *klass, MonoClass *oklass)
{
	gboolean result = FALSE;
	ERROR_DECL (error);
	mono_class_is_assignable_from_checked (klass, oklass, &result, error);
	mono_error_cleanup (error);
	return result;
}

/**
 * mono_class_is_assignable_from:
 * \param klass the class to be assigned to
 * \param oklass the source class
 *
 * \returns TRUE if an instance of class \p oklass can be assigned to an
 * instance of class \p klass
 */
mono_bool
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass)
{
	gboolean result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_is_assignable_from_internal (klass, oklass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/*
 * ECMA I.8.7.3 general assignment compatability is defined in terms of an "intermediate type"
 * whereas ECMA I.8.7.1 assignment compatability for signature types is defined in terms of a "reduced type".
 *
 * This matters when we're comparing arrays of IntPtr.  IntPtr[] is generally
 * assignable to int[] or long[], depending on architecture.  But for signature
 * compatability, IntPtr[] is distinct from both of them.
 *
 * Similarly for ulong* and IntPtr*, etc.
 */
static MonoClass*
composite_type_to_reduced_element_type (MonoClass *array_klass)
{
	switch (m_class_get_byval_arg (m_class_get_element_class (array_klass))->type) {
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		return mono_defaults.int_class;
	default:
		return m_class_get_cast_class (array_klass);
	}
}

static void
mono_class_is_assignable_from_general (MonoClass *klass, MonoClass *oklass, gboolean signature_assignment, gboolean *result, MonoError *error);

/**
 * mono_class_is_assignable_from_checked:
 * \param klass the class to be assigned to
 * \param oklass the source class
 * \param result set if there was no error
 * \param error set if there was an error
 *
 * Sets \p result to TRUE if an instance of class \p oklass can be assigned to
 * an instance of class \p klass or FALSE if it cannot.  On error, no \p error
 * is set and \p result is not valid.
 */
void
mono_class_is_assignable_from_checked (MonoClass *klass, MonoClass *oklass, gboolean *result, MonoError *error)
{
	const gboolean for_sig = FALSE;
	mono_class_is_assignable_from_general (klass, oklass, for_sig, result, error);
}

void
mono_class_signature_is_assignable_from (MonoClass *klass, MonoClass *oklass, gboolean *result, MonoError *error)
{
	const gboolean for_sig = TRUE;
	mono_class_is_assignable_from_general (klass, oklass, for_sig, result, error);
}

void
mono_class_is_assignable_from_general (MonoClass *klass, MonoClass *oklass, gboolean signature_assignment, gboolean *result, MonoError *error)
{
	g_assert (result);
	if (klass == oklass) {
		*result = TRUE;
		return;
	}

	MONO_REQ_GC_UNSAFE_MODE;
	/*FIXME this will cause a lot of irrelevant stuff to be loaded.*/
	if (!m_class_is_inited (klass))
		mono_class_init_internal (klass);

	if (!m_class_is_inited (oklass))
		mono_class_init_internal (oklass);

	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		*result = FALSE;
		return;
	}

	if (mono_class_has_failure (oklass)) {
		mono_error_set_for_class_failure (error, oklass);
		*result = FALSE;
		return;
	}

	MonoType *klass_byval_arg = m_class_get_byval_arg (klass);
	MonoType *oklass_byval_arg = m_class_get_byval_arg (oklass);

	if (mono_type_is_generic_argument (klass_byval_arg)) {
		if (!mono_type_is_generic_argument (oklass_byval_arg)) {
			*result = FALSE;
			return;
		}
		*result = mono_gparam_is_assignable_from (klass, oklass);
		return;
	}

	/* This can happen if oklass is a tyvar that has a constraint which is another tyvar which in turn
	 * has a constraint which is a class type:
	 *
	 *  class Foo { }
	 *  class G<T1, T2> where T1 : T2 where T2 : Foo { }
	 *
	 * In this case, Foo is assignable from T1.
	 */
	if (mono_type_is_generic_argument (oklass_byval_arg)) {
		MonoGenericParam *gparam = oklass_byval_arg->data.generic_param;
		MonoClass **constraints = mono_generic_container_get_param_info (gparam->owner, gparam->num)->constraints;
		int i;

		if (constraints) {
			for (i = 0; constraints [i]; ++i) {
				if (mono_class_is_assignable_from_internal (klass, constraints [i])) {
					*result = TRUE;
					return;
				}
			}
		}

		*result = mono_class_has_parent (oklass, klass);
		return;
	}

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {

		/* interface_offsets might not be set for dynamic classes */
		if (mono_class_get_ref_info_handle (oklass) && !m_class_get_interface_bitmap (oklass)) {
			/*
			 * oklass might be a generic type parameter but they have
			 * interface_offsets set.
			 */
			gboolean assign_result = mono_reflection_call_is_assignable_to (oklass, klass, error);
			return_if_nok (error);
			*result = assign_result;
			return;
		}
		if (!m_class_get_interface_bitmap (oklass)) {
			/* Happens with generic instances of not-yet created dynamic types */
			*result = FALSE;
			return;
		}
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (oklass, m_class_get_interface_id (klass))) {
			*result = TRUE;
			return;
		}

		if (m_class_is_array_special_interface (klass) && m_class_get_rank (oklass) == 1) {
			if (mono_class_is_gtd (klass)) {
				/* klass is an array special gtd like
				 * IList`1<>, and oklass is X[] for some X.
				 * Moreover we know that X isn't !0 (the gparam
				 * of IList`1) because in that case we would
				 * have returned TRUE for
				 * MONO_CLASS_IMPLEMENTS_INTERFACE, above.
				 */
				*result = FALSE;
				return;
			}
			// FIXME: IEnumerator`1 should not be an array special interface.
			// The correct fix is to make
			// ((IEnumerable<U>) (new T[] {...})).GetEnumerator()
			// return an IEnumerator<U> (like .NET does) instead of IEnumerator<T>
			// and to stop marking IEnumerable`1 as an array_special_interface.
			if (mono_class_get_generic_type_definition (klass) == mono_defaults.generic_ienumerator_class) {
				*result = FALSE;
				return;
			}

			//XXX we could offset this by having the cast target computed at JIT time
			//XXX we could go even further and emit a wrapper that would do the extra type check
			MonoClass *iface_klass = mono_class_from_mono_type_internal (mono_class_get_generic_class (klass)->context.class_inst->type_argv [0]);
			MonoClass *obj_klass = m_class_get_cast_class (oklass); //This gets us the cast class of element type of the array

			// If the target we're trying to cast to is a valuetype, we must account of weird valuetype equivalences such as IntEnum <> int or uint <> int
			// We can't apply it for ref types as this would go wrong with arrays - IList<byte[]> would have byte tested
			if (!mono_class_is_nullable (iface_klass)) {
				if (m_class_is_valuetype (iface_klass))
					iface_klass = m_class_get_cast_class (iface_klass);

				//array covariant casts only operates on scalar to scalar
				//This is so int[] can't be casted to IComparable<int>[]
				if (!(m_class_is_valuetype (obj_klass) && !m_class_is_valuetype (iface_klass)) && mono_class_is_assignable_from_internal (iface_klass, obj_klass)) {
					*result = TRUE;
					return;
				}
			}
		}

		if (mono_class_has_variant_generic_params (klass)) {
			int i;
			mono_class_setup_interfaces (oklass, error);
			return_if_nok (error);

			/*klass is a generic variant interface, We need to extract from oklass a list of ifaces which are viable candidates.*/
			for (i = 0; i < m_class_get_interface_offsets_count (oklass); ++i) {
				MonoClass *iface = m_class_get_interfaces_packed (oklass) [i];

				if (mono_class_is_variant_compatible (klass, iface, FALSE)) {
					*result = TRUE;
					return;
				}
			}
		}

		*result = FALSE;
		return;
	} else if (m_class_is_delegate (klass)) {
		if (mono_class_has_variant_generic_params (klass) && mono_class_is_variant_compatible (klass, oklass, FALSE)) {
			*result = TRUE;
			return;
		}
	} else if (m_class_get_rank (klass)) {
		MonoClass *eclass, *eoclass;

		if (m_class_get_rank (oklass) != m_class_get_rank (klass)) {
			*result = FALSE;
			return;
		}

		/* vectors vs. one dimensional arrays */
		if (oklass_byval_arg->type != klass_byval_arg->type) {
			*result = FALSE;
			return;
		}

		if (signature_assignment) {
			eclass = composite_type_to_reduced_element_type (klass);
			eoclass = composite_type_to_reduced_element_type (oklass);
		} else {
			eclass = m_class_get_cast_class (klass);
			eoclass = m_class_get_cast_class (oklass);
		}


		/*
		 * a is b does not imply a[] is b[] when a is a valuetype, and
		 * b is a reference type.
		 */

		if (m_class_is_valuetype (eoclass)) {
			if ((eclass == mono_defaults.enum_class) ||
			    (eclass == m_class_get_parent (mono_defaults.enum_class)) ||
			    (!m_class_is_valuetype (eclass))) {
				*result = FALSE;
				return;
			}
		}

		/*
		 * a is b does not imply a[] is b[] in the case where b is an interface and
		 * a is a generic parameter, unless a has an additional class constraint.
		 * For example (C#):
		 * ```
		 * interface I {}
		 * class G<T> where T : I {}
		 * class H<U> where U : class, I {}
		 * public class P {
		 *     public static void Main() {
		 *         var t = typeof(G<>).GetTypeInfo().GenericTypeParameters[0].MakeArrayType();
		 *         var i = typeof(I).MakeArrayType();
		 *         var u = typeof(H<>).GetTypeInfo().GenericTypeParameters[0].MakeArrayType();
		 *         Console.WriteLine("I[] assignable from T[] ? {0}", i.IsAssignableFrom(t));
		 *         Console.WriteLine("I[] assignable from U[] ? {0}", i.IsAssignableFrom(u));
		 *     }
		 * }
		 * ```
		 * This should print:
		 * I[] assignable from T[] ? False
		 * I[] assignable from U[] ? True
		 */

		if (MONO_CLASS_IS_INTERFACE_INTERNAL (eclass)) {
			MonoType *eoclass_byval_arg = m_class_get_byval_arg (eoclass);
			if (mono_type_is_generic_argument (eoclass_byval_arg)) {
				MonoGenericParam *eoparam = eoclass_byval_arg->data.generic_param;
				MonoGenericParamInfo *eoinfo = mono_generic_param_info (eoparam);
				int eomask = eoinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK;
				// check for class constraint
				if ((eomask & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) == 0) {
					*result = FALSE;
					return;
				}
			}
		}

		if (mono_class_is_nullable (eclass) ^ mono_class_is_nullable (eoclass)) {
			*result = FALSE;
			return;
		}

		mono_class_is_assignable_from_checked (eclass, eoclass, result, error);
		return;
	} else if (mono_class_is_nullable (klass)) {
		if (mono_class_is_nullable (oklass))
			mono_class_is_assignable_from_checked (m_class_get_cast_class (klass), m_class_get_cast_class (oklass), result, error);
		else
			mono_class_is_assignable_from_checked (m_class_get_cast_class (klass), oklass, result, error);
		return;
	} else if (m_class_get_class_kind (klass) == MONO_CLASS_POINTER) {
		if (m_class_get_class_kind (oklass) != MONO_CLASS_POINTER) {
			*result = FALSE;
			return;
		}

		if (m_class_get_byval_arg (klass)->type == MONO_TYPE_FNPTR) {
			/*
			 * if both klass and oklass are fnptr, and they're equal, we would have returned at the
			 * beginning.
			 */
			/* Is this right? or do we need to look at signature compatability? */
			*result = FALSE;
			return;
		}

		if (m_class_get_byval_arg (oklass)->type != MONO_TYPE_PTR) {
			*result = FALSE;
		}
		g_assert (m_class_get_byval_arg (klass)->type == MONO_TYPE_PTR);

		MonoClass *eclass;
		MonoClass *eoclass;
		if (signature_assignment) {
			eclass = composite_type_to_reduced_element_type (klass);
			eoclass = composite_type_to_reduced_element_type (oklass);
		} else {
			eclass = m_class_get_cast_class (klass);
			eoclass = m_class_get_cast_class (oklass);
		}

		*result = (eclass == eoclass);
		return;

	} else if (klass == mono_defaults.object_class) {
		if (m_class_get_class_kind (oklass) == MONO_CLASS_POINTER)
			*result = FALSE;
		else
			*result = TRUE;
		return;
	}

	*result = mono_class_has_parent (oklass, klass);
}

/*Check if @oklass is variant compatible with @klass.*/
static gboolean
mono_class_is_variant_compatible_slow (MonoClass *klass, MonoClass *oklass)
{
	int j;
	MonoType **klass_argv, **oklass_argv;
	MonoClass *klass_gtd = mono_class_get_generic_type_definition (klass);
	MonoGenericContainer *container = mono_class_get_generic_container (klass_gtd);

	/*Viable candidates are instances of the same generic interface*/
	if (mono_class_get_generic_type_definition (oklass) != klass_gtd || oklass == klass_gtd)
		return FALSE;

	klass_argv = &mono_class_get_generic_class (klass)->context.class_inst->type_argv [0];
	oklass_argv = &mono_class_get_generic_class (oklass)->context.class_inst->type_argv [0];

	for (j = 0; j < container->type_argc; ++j) {
		MonoClass *param1_class = mono_class_from_mono_type_internal (klass_argv [j]);
		MonoClass *param2_class = mono_class_from_mono_type_internal (oklass_argv [j]);

		if (m_class_is_valuetype (param1_class) != m_class_is_valuetype (param2_class))
			return FALSE;

		/*
		 * The _VARIANT and _COVARIANT constants should read _COVARIANT and
		 * _CONTRAVARIANT, but they are in a public header so we can't fix it.
		 */
		if (param1_class != param2_class) {
			if (mono_generic_container_get_param_info (container, j)->flags & MONO_GEN_PARAM_VARIANT) {
				if (!mono_class_is_assignable_from_slow (param1_class, param2_class))
					return FALSE;
			} else if (mono_generic_container_get_param_info (container, j)->flags & MONO_GEN_PARAM_COVARIANT) {
				if (!mono_class_is_assignable_from_slow (param2_class, param1_class))
					return FALSE;
			} else
				return FALSE;
		}
	}
	return TRUE;
}
/*Check if @candidate implements the interface @target*/
static gboolean
mono_class_implement_interface_slow (MonoClass *target, MonoClass *candidate)
{
	ERROR_DECL (error);
	int i;
	gboolean is_variant = mono_class_has_variant_generic_params (target);

	if (is_variant && MONO_CLASS_IS_INTERFACE_INTERNAL (candidate)) {
		if (mono_class_is_variant_compatible_slow (target, candidate))
			return TRUE;
	}

	do {
		if (candidate == target)
			return TRUE;

		/*A TypeBuilder can have more interfaces on tb->interfaces than on candidate->interfaces*/
		if (image_is_dynamic (m_class_get_image (candidate)) && !m_class_was_typebuilder (candidate)) {
			MonoReflectionTypeBuilder *tb = mono_class_get_ref_info_raw (candidate); /* FIXME use handles */
			int j;
			if (tb && tb->interfaces) {
				for (j = mono_array_length_internal (tb->interfaces) - 1; j >= 0; --j) {
					MonoReflectionType *iface = mono_array_get_internal (tb->interfaces, MonoReflectionType*, j);
					MonoClass *iface_class;

					/* we can't realize the type here since it can do pretty much anything. */
					if (!iface->type)
						continue;
					iface_class = mono_class_from_mono_type_internal (iface->type);
					if (iface_class == target)
						return TRUE;
					if (is_variant && mono_class_is_variant_compatible_slow (target, iface_class))
						return TRUE;
					if (mono_class_implement_interface_slow (target, iface_class))
						return TRUE;
				}
			}
		} else {
			/*setup_interfaces don't mono_class_init_internal anything*/
			/*FIXME this doesn't handle primitive type arrays.
			ICollection<sbyte> x byte [] won't work because candidate->interfaces, for byte[], won't have IList<sbyte>.
			A possible way to fix this would be to move that to setup_interfaces from setup_interface_offsets.
			*/
			mono_class_setup_interfaces (candidate, error);
			if (!is_ok (error)) {
				mono_error_cleanup (error);
				return FALSE;
			}

			int candidate_interface_count = m_class_get_interface_count (candidate);
			MonoClass **candidate_interfaces = m_class_get_interfaces (candidate);
			for (i = 0; i < candidate_interface_count; ++i) {
				if (candidate_interfaces [i] == target)
					return TRUE;

				if (is_variant && mono_class_is_variant_compatible_slow (target, candidate_interfaces [i]))
					return TRUE;

				if (mono_class_implement_interface_slow (target, candidate_interfaces [i]))
					return TRUE;
			}
		}
		candidate = m_class_get_parent (candidate);
	} while (candidate);

	return FALSE;
}

/*
 * Check if @oklass can be assigned to @klass.
 * This function does the same as mono_class_is_assignable_from_internal but is safe to be used from mono_class_init_internal context.
 */
gboolean
mono_class_is_assignable_from_slow (MonoClass *target, MonoClass *candidate)
{
	if (candidate == target)
		return TRUE;
	if (target == mono_defaults.object_class)
		return TRUE;

	if (mono_class_has_parent (candidate, target))
		return TRUE;

	/*If target is not an interface there is no need to check them.*/
	if (MONO_CLASS_IS_INTERFACE_INTERNAL (target))
		return mono_class_implement_interface_slow (target, candidate);

	if (m_class_is_delegate (target) && mono_class_has_variant_generic_params (target))
		return mono_class_is_variant_compatible (target, candidate, FALSE);

	if (m_class_get_rank (target)) {
		MonoClass *eclass, *eoclass;

		if (m_class_get_rank (target) != m_class_get_rank (candidate))
			return FALSE;

		/* vectors vs. one dimensional arrays */
		if (m_class_get_byval_arg (target)->type != m_class_get_byval_arg (candidate)->type)
			return FALSE;

		eclass = m_class_get_cast_class (target);
		eoclass = m_class_get_cast_class (candidate);

		/*
		 * a is b does not imply a[] is b[] when a is a valuetype, and
		 * b is a reference type.
		 */

		if (m_class_is_valuetype (eoclass)) {
			if ((eclass == mono_defaults.enum_class) ||
			    (eclass == m_class_get_parent (mono_defaults.enum_class)) ||
				(eclass == mono_defaults.object_class))
				return FALSE;
		}

		return mono_class_is_assignable_from_slow (eclass, eoclass);
	}
	/*FIXME properly handle nullables */
	/*FIXME properly handle (M)VAR */
	return FALSE;
}

/**
 * mono_generic_param_get_base_type:
 *
 * Return the base type of the given generic parameter from its constraints.
 *
 * Could be another generic parameter, or it could be Object or ValueType.
 */
MonoClass*
mono_generic_param_get_base_type (MonoClass *klass)
{
	MonoType *type = m_class_get_byval_arg (klass);
	g_assert (mono_type_is_generic_argument (type));

	MonoGenericParam *gparam = type->data.generic_param;

	g_assert (gparam->owner && !gparam->owner->is_anonymous);

	MonoClass **constraints = mono_generic_container_get_param_info (gparam->owner, gparam->num)->constraints;

	MonoClass *base_class = mono_defaults.object_class;

	if (constraints) {
		int i;
		for (i = 0; constraints [i]; ++i) {
			MonoClass *constraint = constraints[i];

			if (MONO_CLASS_IS_INTERFACE_INTERNAL (constraint))
				continue;

			MonoType *constraint_type = m_class_get_byval_arg (constraint);
			if (mono_type_is_generic_argument (constraint_type)) {
				MonoGenericParam *constraint_param = constraint_type->data.generic_param;
				MonoGenericParamInfo *constraint_info = mono_generic_param_info (constraint_param);
				if ((constraint_info->flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) == 0 &&
				    (constraint_info->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) == 0)
					continue;
			}

			base_class = constraint;
		}

	}

	if (base_class == mono_defaults.object_class)
	{
		MonoGenericParamInfo *gparam_info = mono_generic_param_info (gparam);
		if ((gparam_info->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) != 0) {
			base_class = mono_class_get_valuetype_class ();
		}
	}

	return base_class;
}

/**
 * mono_class_get_cctor:
 * \param klass A MonoClass pointer
 *
 * \returns The static constructor of \p klass if it exists, NULL otherwise.
 */
MonoMethod*
mono_class_get_cctor (MonoClass *klass)
{
	MonoMethod *result = NULL;
	ERROR_DECL (error);
	MonoCachedClassInfo cached_info;

	if (image_is_dynamic (m_class_get_image (klass))) {
		/*
		 * has_cctor is not set for these classes because mono_class_init_internal () is
		 * not run for them.
		 */
		result = mono_class_get_method_from_name_checked (klass, ".cctor", -1, METHOD_ATTRIBUTE_SPECIAL_NAME, error);
		mono_error_assert_msg_ok (error, "Could not lookup class cctor in dynamic image");
		return result;
	}

	mono_class_init_internal (klass);

	if (!m_class_has_cctor (klass))
		return result;

	if (mono_class_is_ginst (klass) && !m_class_get_methods (klass)) {
		result = mono_class_get_inflated_method (klass, mono_class_get_cctor (mono_class_get_generic_class (klass)->container_class), error);
		mono_error_assert_msg_ok (error, "Could not lookup inflated class cctor"); /* FIXME do proper error handling */
		return result;
	}

	if (mono_class_get_cached_class_info (klass, &cached_info)) {
		result = mono_get_method_checked (m_class_get_image (klass), cached_info.cctor_token, klass, NULL, error);
		mono_error_assert_msg_ok (error, "Could not lookup class cctor from cached metadata");
		return result;
	}

	result = mono_class_get_method_from_name_checked (klass, ".cctor", -1, METHOD_ATTRIBUTE_SPECIAL_NAME, error);
	mono_error_assert_msg_ok (error, "Could not lookup class cctor");
	return result;
}

/**
 * mono_class_get_finalizer:
 * \param klass: The MonoClass pointer
 *
 * \returns The finalizer method of \p klass if it exists, NULL otherwise.
 */
MonoMethod*
mono_class_get_finalizer (MonoClass *klass)
{
	MonoCachedClassInfo cached_info;

	if (!m_class_is_inited (klass))
		mono_class_init_internal (klass);
	if (!mono_class_has_finalizer (klass))
		return NULL;

	if (mono_class_get_cached_class_info (klass, &cached_info)) {
		ERROR_DECL (error);
		MonoMethod *result = mono_get_method_checked (cached_info.finalize_image, cached_info.finalize_token, NULL, NULL, error);
		mono_error_assert_msg_ok (error, "Could not lookup finalizer from cached metadata");
		return result;
	}else {
		mono_class_setup_vtable (klass);
		return m_class_get_vtable (klass) [mono_class_get_object_finalize_slot ()];
	}
}

/**
 * mono_class_needs_cctor_run:
 * \param klass the MonoClass pointer
 * \param caller a MonoMethod describing the caller
 *
 * Determines whenever the class has a static constructor and whenever it
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
		return FALSE;
}

/**
 * mono_class_array_element_size:
 * \param klass
 *
 * \returns The number of bytes an element of type \p klass uses when stored into an array.
 */
gint32
mono_class_array_element_size (MonoClass *klass)
{
	MonoType *type = m_class_get_byval_arg (klass);

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
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return TARGET_SIZEOF_VOID_P;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		return 8;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			type = mono_class_enum_basetype_internal (type->data.klass);
			klass = m_class_get_element_class (klass);
			goto handle_enum;
		}
		return mono_class_value_size (klass, NULL);
	case MONO_TYPE_GENERICINST:
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		goto handle_enum;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR: {
		int align;

		return mono_type_size (type, &align);
	}
	case MONO_TYPE_VOID:
		return 0;

	default:
		g_error ("unknown type 0x%02x in mono_class_array_element_size", type->type);
	}
	return -1;
}

/**
 * mono_array_element_size:
 * \param ac pointer to a \c MonoArrayClass
 *
 * \returns The size of single array element.
 *
 * LOCKING: Acquires the loader lock.
 */
gint32
mono_array_element_size (MonoClass *ac)
{
	g_assert (m_class_get_rank (ac));
	if (G_UNLIKELY (!m_class_is_size_inited (ac))) {
		mono_class_setup_fields (ac);
	}
	return m_class_get_sizes (ac).element_size;
}

/**
 * mono_ldtoken:
 */
gpointer
mono_ldtoken (MonoImage *image, guint32 token, MonoClass **handle_class,
	      MonoGenericContext *context)
{
	gpointer res;
	MONO_ENTER_GC_UNSAFE;
	ERROR_DECL (error);
	res = mono_ldtoken_checked (image, token, handle_class, context, error);
	mono_error_assert_ok (error);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

gpointer
mono_ldtoken_checked (MonoImage *image, guint32 token, MonoClass **handle_class,
	      MonoGenericContext *context, MonoError *error)
{
	error_init (error);

	if (image_is_dynamic (image)) {
		MonoClass *tmp_handle_class;
		gpointer obj = mono_lookup_dynamic_token_class (image, token, TRUE, &tmp_handle_class, context, error);

		mono_error_assert_ok (error);
		g_assert (tmp_handle_class);
		if (handle_class)
			*handle_class = tmp_handle_class;

		if (tmp_handle_class == mono_defaults.typehandle_class)
			return m_class_get_byval_arg ((MonoClass*)obj);
		else
			return obj;
	}

	switch (token & 0xff000000) {
	case MONO_TOKEN_TYPE_DEF:
	case MONO_TOKEN_TYPE_REF:
	case MONO_TOKEN_TYPE_SPEC: {
		MonoType *type;
		MonoClass *klass;
		if (handle_class)
			*handle_class = mono_defaults.typehandle_class;
		type = mono_type_get_checked (image, token, context, error);
		if (!type)
			return NULL;

		klass = mono_class_from_mono_type_internal (type);
		mono_class_init_internal (klass);
		if (mono_class_has_failure (klass)) {
			mono_error_set_for_class_failure (error, klass);
			return NULL;
		}

		/* We return a MonoType* as handle */
		return type;
	}
	case MONO_TOKEN_FIELD_DEF: {
		MonoClass *klass;
		guint32 type = mono_metadata_typedef_from_field (image, mono_metadata_token_index (token));
		if (!type) {
			mono_error_set_bad_image (error, image, "Bad ldtoken %x", token);
			return NULL;
		}
		if (handle_class)
			*handle_class = mono_defaults.fieldhandle_class;
		klass = mono_class_get_and_inflate_typespec_checked (image, MONO_TOKEN_TYPE_DEF | type, context, error);
		if (!klass)
			return NULL;

		mono_class_init_internal (klass);
		return mono_class_get_field (klass, token);
	}
	case MONO_TOKEN_METHOD_DEF:
	case MONO_TOKEN_METHOD_SPEC: {
		MonoMethod *meth;
		meth = mono_get_method_checked (image, token, NULL, context, error);
		if (handle_class)
			*handle_class = mono_defaults.methodhandle_class;
		if (!meth)
			return NULL;

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
			field = mono_field_from_token_checked (image, token, &klass, context, error);
			if (handle_class)
				*handle_class = mono_defaults.fieldhandle_class;
			return field;
		} else {
			MonoMethod *meth;
			meth = mono_get_method_checked (image, token, NULL, context, error);
			if (handle_class)
				*handle_class = mono_defaults.methodhandle_class;
			return meth;
		}
	}
	default:
		mono_error_set_bad_image (error, image, "Bad ldtoken %x", token);
	}
	return NULL;
}

gpointer
mono_lookup_dynamic_token (MonoImage *image, guint32 token, MonoGenericContext *context, MonoError *error)
{
	MonoClass *handle_class;
	error_init (error);
	return mono_reflection_lookup_dynamic_token (image, token, TRUE, &handle_class, context, error);
}

gpointer
mono_lookup_dynamic_token_class (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error)
{
	return mono_reflection_lookup_dynamic_token (image, token, valid_token, handle_class, context, error);
}

static MonoGetCachedClassInfo get_cached_class_info = NULL;

void
mono_install_get_cached_class_info (MonoGetCachedClassInfo func)
{
	get_cached_class_info = func;
}

gboolean
mono_class_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res)
{
	if (!get_cached_class_info)
		return FALSE;
	else
		return get_cached_class_info (klass, res);
}

void
mono_install_get_class_from_name (MonoGetClassFromName func)
{
	get_class_from_name = func;
}

/**
 * mono_class_get_image:
 *
 * Use this method to get the \c MonoImage* where this class came from.
 *
 * \returns The image where this class is defined.
 */
MonoImage*
mono_class_get_image (MonoClass *klass)
{
	return m_class_get_image (klass);
}

/**
 * mono_class_get_element_class:
 * \param klass the \c MonoClass to act on
 *
 * Use this function to get the element class of an array.
 *
 * \returns The element class of an array.
 */
MonoClass*
mono_class_get_element_class (MonoClass *klass)
{
	MonoClass *result;
	MONO_ENTER_GC_UNSAFE;
	result = m_class_get_element_class (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_is_valuetype:
 * \param klass the \c MonoClass to act on
 *
 * Use this method to determine if the provided \c MonoClass* represents a value type,
 * or a reference type.
 *
 * \returns TRUE if the \c MonoClass represents a \c ValueType, FALSE if it represents a reference type.
 */
gboolean
mono_class_is_valuetype (MonoClass *klass)
{
	gboolean result;
	MONO_ENTER_GC_UNSAFE;
	result = m_class_is_valuetype (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_is_enum:
 * \param klass the \c MonoClass to act on
 *
 * Use this function to determine if the provided \c MonoClass* represents an enumeration.
 *
 * \returns TRUE if the \c MonoClass represents an enumeration.
 */
gboolean
mono_class_is_enum (MonoClass *klass)
{
	gboolean result;
	MONO_ENTER_GC_UNSAFE;
	result = m_class_is_enumtype (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_enum_basetype_internal:
 * \param klass the \c MonoClass to act on
 *
 * Use this function to get the underlying type for an enumeration value.
 *
 * \returns The underlying type representation for an enumeration.
 */
MonoType*
mono_class_enum_basetype_internal (MonoClass *klass)
{
	if (m_class_get_element_class (klass) == klass)
		/* SRE or broken types */
		return NULL;
	return m_class_get_byval_arg (m_class_get_element_class (klass));
}

/**
 * mono_class_enum_basetype:
 * \param klass the \c MonoClass to act on
 *
 * Use this function to get the underlying type for an enumeration value.
 *
 * \returns The underlying type representation for an enumeration.
 */
MonoType*
mono_class_enum_basetype (MonoClass *klass)
{
	MonoType *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_class_enum_basetype_internal (klass);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_class_get_parent
 * \param klass the \c MonoClass to act on
 *
 * \returns The parent class for this class.
 */
MonoClass*
mono_class_get_parent (MonoClass *klass)
{
	MonoClass *result;
	MONO_ENTER_GC_UNSAFE;
	result = m_class_get_parent (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_get_nesting_type:
 * \param klass the \c MonoClass to act on
 *
 * Use this function to obtain the class that the provided \c MonoClass* is nested on.
 *
 * If the return is NULL, this indicates that this class is not nested.
 *
 * \returns The container type where this type is nested or NULL if this type is not a nested type.
 */
MonoClass*
mono_class_get_nesting_type (MonoClass *klass)
{
	return m_class_get_nested_in (klass);
}

/**
 * mono_class_get_rank:
 * \param klass the MonoClass to act on
 *
 * \returns The rank for the array (the number of dimensions).
 */
int
mono_class_get_rank (MonoClass *klass)
{
	return m_class_get_rank (klass);
}

/**
 * mono_class_get_name
 * \param klass the \c MonoClass to act on
 *
 * \returns The name of the class.
 */
const char*
mono_class_get_name (MonoClass *klass)
{
	const char *result;
	MONO_ENTER_GC_UNSAFE;
	result = m_class_get_name (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_get_namespace:
 * \param klass the \c MonoClass to act on
 *
 * \returns The namespace of the class.
 */
const char*
mono_class_get_namespace (MonoClass *klass)
{
	const char *result;
	MONO_ENTER_GC_UNSAFE;
	result = m_class_get_name_space (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_get_type:
 * \param klass the \c MonoClass to act on
 *
 * This method returns the internal \c MonoType representation for the class.
 *
 * \returns The \c MonoType from the class.
 */
MonoType*
mono_class_get_type (MonoClass *klass)
{
	return m_class_get_byval_arg (klass);
}

/**
 * mono_class_get_type_token:
 * \param klass the \c MonoClass to act on
 *
 * This method returns type token for the class.
 *
 * \returns The type token for the class.
 */
guint32
mono_class_get_type_token (MonoClass *klass)
{
	return m_class_get_type_token (klass);
}

/**
 * mono_class_get_byref_type:
 * \param klass the \c MonoClass to act on
 *
 *
 */
MonoType*
mono_class_get_byref_type (MonoClass *klass)
{
	return m_class_get_this_arg (klass);
}

/**
 * mono_class_num_fields:
 * \param klass the \c MonoClass to act on
 *
 * \returns The number of static and instance fields in the class.
 */
int
mono_class_num_fields (MonoClass *klass)
{
	return mono_class_get_field_count (klass);
}

/**
 * mono_class_num_methods:
 * \param klass the \c MonoClass to act on
 *
 * \returns The number of methods in the class.
 */
int
mono_class_num_methods (MonoClass *klass)
{
	return mono_class_get_method_count (klass);
}

/**
 * mono_class_num_properties
 * \param klass the \c MonoClass to act on
 *
 * \returns The number of properties in the class.
 */
int
mono_class_num_properties (MonoClass *klass)
{
	mono_class_setup_properties (klass);

	return mono_class_get_property_info (klass)->count;
}

/**
 * mono_class_num_events:
 * \param klass the \c MonoClass to act on
 *
 * \returns The number of events in the class.
 */
int
mono_class_num_events (MonoClass *klass)
{
	mono_class_setup_events (klass);

	return mono_class_get_event_info (klass)->count;
}

/**
 * mono_class_get_fields:
 * \param klass the \c MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the fields in a class.
 *
 * You must pass a \c gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * \returns a \c MonoClassField* on each iteration, or NULL when no more fields are available.
 */
MonoClassField*
mono_class_get_fields (MonoClass* klass, gpointer *iter)
{
	MonoClassField *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_get_fields_internal (klass, iter);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoClassField*
mono_class_get_fields_internal (MonoClass *klass, gpointer *iter)
{
	if (!iter)
		return NULL;
	MonoImage *image = m_class_get_image (klass);
	if (!*iter) {
		mono_class_setup_fields (klass);
		if (mono_class_has_failure (klass))
			return NULL;
		/* start from the first */
		if (mono_class_get_field_count (klass)) {
			MonoClassField *klass_fields = m_class_get_fields (klass);
			uint32_t idx = 0;
			*iter = GUINT_TO_POINTER (idx + 1);
			return &klass_fields [0];
		} else {
			/* no fields */
			if (G_LIKELY (!image->has_updates))
				return NULL;
			else
				*iter = 0;
		}
	}
	// invariant: idx is one past the field we previously returned
	uint32_t idx = GPOINTER_TO_UINT(*iter);
	if (idx < mono_class_get_field_count (klass)) {
		MonoClassField *field = &m_class_get_fields (klass) [idx];
		++idx;
		*iter = GUINT_TO_POINTER (idx);
		return field;
	}
	if (G_UNLIKELY (image->has_updates)) {
		return mono_metadata_update_added_fields_iter (klass, FALSE, iter);
	}
	return NULL;
}

/**
 * mono_class_get_methods:
 * \param klass the \c MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the fields in a class.
 *
 * You must pass a \c gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * \returns a \c MonoMethod on each iteration or NULL when no more methods are available.
 */
MonoMethod*
mono_class_get_methods (MonoClass* klass, gpointer *iter)
{
	if (!iter)
		return NULL;
	MonoImage *image = m_class_get_image (klass);
	if (!*iter) {
		mono_class_setup_methods (klass);

		MonoMethod **klass_methods = m_class_get_methods (klass);
		/*
		 * We can't fail lookup of methods otherwise the runtime will burst in flames on all sort of places.
		 * FIXME we should better report this error to the caller
		 */
		if (!klass_methods && !image->has_updates)
			return NULL;
		uint32_t idx = 0;
		/* start from the first */
		if (mono_class_get_method_count (klass)) {
			// idx is 1 more than the method we just returned
			*iter = GUINT_TO_POINTER (idx + 1);
			return klass_methods [0];
		} else {
			/* no method */
			if (G_LIKELY (!image->has_updates))
				return NULL;
			else
				*iter = 0;
		}
	}
	// idx is 1 more than the method we returned on the previous iteration
	uint32_t idx = GPOINTER_TO_UINT (*iter);
	if (idx < mono_class_get_method_count (klass)) {
		// if we're still in range, return the next method and advance iter 1 past it.
		MonoMethod *method = m_class_get_methods (klass) [idx];
		idx++;
		*iter = GUINT_TO_POINTER (idx);
		return method;
	}
	if (G_UNLIKELY (image->has_updates))
		return mono_metadata_update_added_methods_iter (klass, iter);
	return NULL;
}

/**
 * mono_class_get_properties:
 * \param klass the \c MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the properties in a class.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a \c MonoProperty* on each invocation, or NULL when no more are available.
 */
MonoProperty*
mono_class_get_properties (MonoClass* klass, gpointer *iter)
{
	MonoProperty* property;
	if (!iter)
		return NULL;
	if (!*iter) {
		mono_class_setup_properties (klass);
		MonoClassPropertyInfo *info = mono_class_get_property_info (klass);
		/* start from the first */
		if (info->count) {
			*iter = &info->properties [0];
			return (MonoProperty *)*iter;
		} else {
			/* no fields */
			return NULL;
		}
	}
	property = (MonoProperty *)*iter;
	property++;
	MonoClassPropertyInfo *info = mono_class_get_property_info (klass);
	if (property < &info->properties [info->count]) {
		*iter = property;
		return (MonoProperty *)*iter;
	}
	return NULL;
}

/**
 * mono_class_get_events:
 * \param klass the \c MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the properties in a class.
 *
 * You must pass a \c gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * \returns a \c MonoEvent* on each invocation, or NULL when no more are available.
 */
MonoEvent*
mono_class_get_events (MonoClass* klass, gpointer *iter)
{
	MonoEvent* event;
	if (!iter)
		return NULL;
	if (!*iter) {
		mono_class_setup_events (klass);
		MonoClassEventInfo *info = mono_class_get_event_info (klass);
		/* start from the first */
		if (info->count) {
			*iter = &info->events [0];
			return (MonoEvent *)*iter;
		} else {
			/* no fields */
			return NULL;
		}
	}
	event = (MonoEvent *)*iter;
	event++;
	MonoClassEventInfo *info = mono_class_get_event_info (klass);
	if (event < &info->events [info->count]) {
		*iter = event;
		return (MonoEvent *)*iter;
	}
	return NULL;
}

/**
 * mono_class_get_interfaces
 * \param klass the \c MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the interfaces implemented by this class.
 *
 * You must pass a \c gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * \returns a \c MonoClass* on each invocation, or NULL when no more are available.
 */
MonoClass*
mono_class_get_interfaces (MonoClass* klass, gpointer *iter)
{
	ERROR_DECL (error);
	MonoClass** iface;
	if (!iter)
		return NULL;
	if (!*iter) {
		if (!m_class_is_inited (klass))
			mono_class_init_internal (klass);
		if (!m_class_is_interfaces_inited (klass)) {
			mono_class_setup_interfaces (klass, error);
			if (!is_ok (error)) {
				mono_error_cleanup (error);
				return NULL;
			}
		}
		/* start from the first */
		if (m_class_get_interface_count (klass)) {
			*iter = &m_class_get_interfaces (klass) [0];
			return m_class_get_interfaces (klass) [0];
		} else {
			/* no interface */
			return NULL;
		}
	}
	iface = (MonoClass **)*iter;
	iface++;
	if (iface < &m_class_get_interfaces (klass) [m_class_get_interface_count (klass)]) {
		*iter = iface;
		return *iface;
	}
	return NULL;
}

/**
 * mono_class_get_nested_types
 * \param klass the \c MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the nested types of a class.
 * This works only if \p klass is non-generic, or a generic type definition.
 *
 * You must pass a \c gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * \returns a \c Monoclass* on each invocation, or NULL when no more are available.
 */
MonoClass*
mono_class_get_nested_types (MonoClass* klass, gpointer *iter)
{
	GList *item;

	if (!iter)
		return NULL;
	if (!m_class_is_nested_classes_inited (klass))
		mono_class_setup_nested_types (klass);

	if (!*iter) {
		GList *nested_classes = mono_class_get_nested_classes_property (klass);
		/* start from the first */
		if (nested_classes) {
			*iter = nested_classes;
			return (MonoClass *)nested_classes->data;
		} else {
			/* no nested types */
			return NULL;
		}
	}
	item = (GList *)*iter;
	item = item->next;
	if (item) {
		*iter = item;
		return (MonoClass *)item->data;
	}
	return NULL;
}


/**
 * mono_class_is_delegate
 * \param klass the \c MonoClass to act on
 *
 * \returns TRUE if the \c MonoClass represents a \c System.Delegate.
 */
mono_bool
mono_class_is_delegate (MonoClass *klass)
{
	mono_bool result;
	MONO_ENTER_GC_UNSAFE;
	result = m_class_is_delegate (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_class_implements_interface
 * \param klass The MonoClass to act on
 * \param interface The interface to check if \p klass implements.
 *
 * \returns TRUE if \p klass implements \p interface.
 */
mono_bool
mono_class_implements_interface (MonoClass* klass, MonoClass* iface)
{
	mono_bool result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_is_assignable_from_internal (iface, klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

static mono_bool
class_implements_interface_ignore_generics (MonoClass* klass, MonoClass* iface)
{
	int i;
	ERROR_DECL (error);
	if (mono_class_is_ginst (iface))
		iface = mono_class_get_generic_type_definition (iface);
	while (klass != NULL) {
		if (mono_class_is_assignable_from_internal (iface, klass))
			return TRUE;
		mono_class_setup_interfaces (klass, error);
		if (!is_ok (error)) {
			mono_error_cleanup  (error);
			return FALSE;
		}
		MonoClass **klass_interfaces = m_class_get_interfaces (klass);
		for (i = 0; i < m_class_get_interface_count (klass); i++) {
			MonoClass *ic = klass_interfaces [i];
			if (mono_class_is_ginst (ic))
				ic = mono_class_get_generic_type_definition (ic);
			if (ic == iface) {
				return TRUE;
			}
		}
		klass = m_class_get_parent (klass);
	}
	return FALSE;
}


/**
 * mono_field_get_name:
 * \param field the \c MonoClassField to act on
 *
 * \returns The name of the field.
 */
const char*
mono_field_get_name (MonoClassField *field)
{
	return field->name;
}

/**
 * mono_field_get_type_internal:
 * \param field the \c MonoClassField to act on
 * \returns \c MonoType of the field.
 */
MonoType*
mono_field_get_type_internal (MonoClassField *field)
{
	MonoType *type = field->type;
	if (type)
		return type;

	ERROR_DECL (error);
	type = mono_field_get_type_checked (field, error);
	if (!is_ok (error)) {
		mono_trace_warning (MONO_TRACE_TYPE, "Could not load field's type due to %s", mono_error_get_message (error));
		mono_error_cleanup (error);
	}
	return type;
}

/**
 * mono_field_get_type:
 * \param field the \c MonoClassField to act on
 * \returns \c MonoType of the field.
 */
MonoType*
mono_field_get_type (MonoClassField *field)
{
	MonoType *type = field->type;
	if (type)
		return type;

	MONO_ENTER_GC_UNSAFE;
	type = mono_field_get_type_internal (field);
	MONO_EXIT_GC_UNSAFE;
	return type;
}

/**
 * mono_field_get_type_checked:
 * \param field the \c MonoClassField to act on
 * \param error used to return any error found while retrieving \p field type
 *
 * \returns \c MonoType of the field.
 */
MonoType*
mono_field_get_type_checked (MonoClassField *field, MonoError *error)
{
	error_init (error);
	MonoType *type = field->type;
	if (type)
		return type;
	mono_field_resolve_type (field, error);
	return field->type;
}

/**
 * mono_field_get_parent:
 * \param field the \c MonoClassField to act on
 *
 * \returns \c MonoClass where the field was defined.
 */
MonoClass*
mono_field_get_parent (MonoClassField *field)
{
	return m_field_get_parent (field);
}

/**
 * mono_field_get_flags;
 * \param field the \c MonoClassField to act on
 *
 * The metadata flags for a field are encoded using the
 * \c FIELD_ATTRIBUTE_* constants.  See the \c tabledefs.h file for details.
 *
 * \returns The flags for the field.
 */
guint32
mono_field_get_flags (MonoClassField *field)
{
	if (!field->type)
		return mono_field_resolve_flags (field);
	return field->type->attrs;
}

/**
 * mono_field_get_offset:
 * \param field the \c MonoClassField to act on
 *
 * \returns The field offset.
 */
guint32
mono_field_get_offset (MonoClassField *field)
{
	mono_class_setup_fields(m_field_get_parent (field));
	return field->offset;
}

const char *
mono_field_get_rva (MonoClassField *field, int swizzle)
{
	guint32 rva;
	int field_index;
	MonoClass *klass = m_field_get_parent (field);
	MonoFieldDefaultValue *def_values;

	g_assert (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA);

	/* TODO: metadata-update: make this work. */
	g_assert (!m_field_is_from_update (field));

	def_values = mono_class_get_field_def_values_with_swizzle (klass, swizzle);
	if (!def_values) {
		def_values = (MonoFieldDefaultValue *)mono_class_alloc0 (klass, sizeof (MonoFieldDefaultValue) * mono_class_get_field_count (klass));

		mono_class_set_field_def_values_with_swizzle (klass, def_values, swizzle);
	}

	field_index = mono_field_get_index (field);

	if (!def_values [field_index].data) {
		const char *rvaData;

		if (!image_is_dynamic (m_class_get_image (klass))) {
			int first_field_idx = mono_class_get_first_field_idx (klass);
			mono_metadata_field_info (m_class_get_image (m_field_get_parent (field)), first_field_idx + field_index, NULL, &rva, NULL);
			if (!rva)
				g_warning ("field %s in %s should have RVA data, but hasn't", mono_field_get_name (field), m_class_get_name (m_field_get_parent (field)));

			rvaData = mono_image_rva_map (m_class_get_image (m_field_get_parent (field)), rva);
		} else {
			rvaData = mono_field_get_data (field);
		}

		if (rvaData == NULL)
			return NULL;

		if (swizzle != 1) {
			int dummy;
			int dataSizeInBytes =  mono_type_size (field->type, &dummy);
			char *swizzledRvaData = mono_class_alloc0 (klass, dataSizeInBytes);

#define SWAP(n) {								\
	guint ## n *data = (guint ## n *) swizzledRvaData; \
	guint ## n *src = (guint ## n *) rvaData; 				\
	int i,									\
	    nEnt = (dataSizeInBytes / sizeof(guint ## n));					\
										\
	for (i = 0; i < nEnt; i++) {						\
		data[i] = read ## n (&src[i]);					\
	} 									\
}
			if (swizzle == 2) {
				SWAP (16);
			} else if (swizzle == 4) {
				SWAP (32);
			} else {
				SWAP (64);
			}
#undef SWAP
			def_values [field_index].data = swizzledRvaData;
		} else {
			def_values [field_index].data = rvaData;
		}
	}

	return def_values [field_index].data;
}

/**
 * mono_field_get_data:
 * \param field the \c MonoClassField to act on
 *
 * \returns A pointer to the metadata constant value or to the field
 * data if it has an RVA flag.
 */
const char *
mono_field_get_data (MonoClassField *field)
{
	if (field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT) {
		MonoTypeEnum def_type;

		return mono_class_get_field_default_value (field, &def_type);
	} else if (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) {
		return mono_field_get_rva (field, 1);
	} else {
		return NULL;
	}
}

/**
 * mono_property_get_name:
 * \param prop the \c MonoProperty to act on
 * \returns The name of the property
 */
const char*
mono_property_get_name (MonoProperty *prop)
{
	return prop->name;
}

/**
 * mono_property_get_set_method
 * \param prop the \c MonoProperty to act on.
 * \returns The setter method of the property, a \c MonoMethod.
 */
MonoMethod*
mono_property_get_set_method (MonoProperty *prop)
{
	return prop->set;
}

/**
 * mono_property_get_get_method
 * \param prop the MonoProperty to act on.
 * \returns The getter method of the property (A \c MonoMethod)
 */
MonoMethod*
mono_property_get_get_method (MonoProperty *prop)
{
	return prop->get;
}

/**
 * mono_property_get_parent:
 * \param prop the \c MonoProperty to act on.
 * \returns The \c MonoClass where the property was defined.
 */
MonoClass*
mono_property_get_parent (MonoProperty *prop)
{
	return prop->parent;
}

/**
 * mono_property_get_flags:
 * \param prop the \c MonoProperty to act on.
 *
 * The metadata flags for a property are encoded using the
 * \c PROPERTY_ATTRIBUTE_* constants.  See the \c tabledefs.h file for details.
 *
 * \returns The flags for the property.
 */
guint32
mono_property_get_flags (MonoProperty *prop)
{
	return prop->attrs & ~MONO_PROPERTY_META_FLAG_MASK;
}

/**
 * mono_event_get_name:
 * \param event the MonoEvent to act on
 * \returns The name of the event.
 */
const char*
mono_event_get_name (MonoEvent *event)
{
	return event->name;
}

/**
 * mono_event_get_add_method:
 * \param event The \c MonoEvent to act on.
 * \returns The \c add method for the event, a \c MonoMethod.
 */
MonoMethod*
mono_event_get_add_method (MonoEvent *event)
{
	return event->add;
}

/**
 * mono_event_get_remove_method:
 * \param event The \c MonoEvent to act on.
 * \returns The \c remove method for the event, a \c MonoMethod.
 */
MonoMethod*
mono_event_get_remove_method (MonoEvent *event)
{
	return event->remove;
}

/**
 * mono_event_get_raise_method:
 * \param event The \c MonoEvent to act on.
 * \returns The \c raise method for the event, a \c MonoMethod.
 */
MonoMethod*
mono_event_get_raise_method (MonoEvent *event)
{
	return event->raise;
}

/**
 * mono_event_get_parent:
 * \param event the MonoEvent to act on.
 * \returns The \c MonoClass where the event is defined.
 */
MonoClass*
mono_event_get_parent (MonoEvent *event)
{
	return event->parent;
}

/**
 * mono_event_get_flags
 * \param event the \c MonoEvent to act on.
 *
 * The metadata flags for an event are encoded using the
 * \c EVENT_* constants.  See the \c tabledefs.h file for details.
 *
 * \returns The flags for the event.
 */
guint32
mono_event_get_flags (MonoEvent *event)
{
	return event->attrs & ~MONO_EVENT_META_FLAG_MASK;
}

/**
 * mono_class_get_method_from_name:
 * \param klass where to look for the method
 * \param name name of the method
 * \param param_count number of parameters. -1 for any number.
 *
 * Obtains a \c MonoMethod with a given name and number of parameters.
 * It only works if there are no multiple signatures for any given method name.
 */
MonoMethod *
mono_class_get_method_from_name (MonoClass *klass, const char *name, int param_count)
{
	MonoMethod *result;
	MONO_ENTER_GC_UNSAFE;
	ERROR_DECL (error);
	result = mono_class_get_method_from_name_checked (klass, name, param_count, 0, error);
	mono_error_cleanup (error);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoMethod*
mono_find_method_in_metadata (MonoClass *klass, const char *name, int param_count, int flags)
{
	MonoImage *klass_image = m_class_get_image (klass);
	MonoMethod *res = NULL;
	int i;

	/* Search directly in the metadata to avoid calling setup_methods () */
	int first_idx = mono_class_get_first_method_idx (klass);
	int mcount = mono_class_get_method_count (klass);
	for (i = 0; i < mcount; ++i) {
		ERROR_DECL (error);
		guint32 cols [MONO_METHOD_SIZE];
		MonoMethod *method;
		MonoMethodSignature *sig;

		/* first_idx points into the methodptr table */
		mono_metadata_decode_table_row (klass_image, MONO_TABLE_METHOD, first_idx + i, cols, MONO_METHOD_SIZE);

		if (!strcmp (mono_metadata_string_heap (klass_image, cols [MONO_METHOD_NAME]), name)) {
			method = mono_get_method_checked (klass_image, MONO_TOKEN_METHOD_DEF | (first_idx + i + 1), klass, NULL, error);
			if (!method) {
				mono_error_cleanup (error); /* FIXME don't swallow the error */
				continue;
			}
			if (param_count == -1) {
				res = method;
				break;
			}
			sig = mono_method_signature_checked (method, error);
			if (!sig) {
				mono_error_cleanup (error); /* FIXME don't swallow the error */
				continue;
			}
			if (sig->param_count == param_count) {
				res = method;
				break;
			}
		}
	}

	if (G_UNLIKELY (!res && klass_image->has_updates)) {
		if (mono_class_has_metadata_update_info (klass)) {
			ERROR_DECL (error);
			res = mono_metadata_update_find_method_by_name (klass, name, param_count, flags, error);
			mono_error_cleanup (error);
		}
	}

	return res;
}

/**
 * mono_class_get_method_from_name_flags:
 * \param klass where to look for the method
 * \param name_space name of the method
 * \param param_count number of parameters. -1 for any number.
 * \param flags flags which must be set in the method
 *
 * Obtains a \c MonoMethod with a given name and number of parameters.
 * It only works if there are no multiple signatures for any given method name.
 */
MonoMethod *
mono_class_get_method_from_name_flags (MonoClass *klass, const char *name, int param_count, int flags)
{
	MonoMethod *method;
	MONO_ENTER_GC_UNSAFE;
	ERROR_DECL (error);
	method = mono_class_get_method_from_name_checked (klass, name, param_count, flags, error);
	mono_error_cleanup (error);
	MONO_EXIT_GC_UNSAFE;
	return method;
}

/**
 * mono_class_get_method_from_name_checked:
 * \param klass where to look for the method
 * \param name_space name of the method
 * \param param_count number of parameters. -1 for any number.
 * \param flags flags which must be set in the method
 * \param error
 *
 * Obtains a \c MonoMethod with a given name and number of parameters.
 * It only works if there are no multiple signatures for any given method name.
 */
MonoMethod *
mono_class_get_method_from_name_checked (MonoClass *klass, const char *name,
	int param_count, int flags, MonoError *error)
{
	MonoMethod *res = NULL;
	int i;

	mono_class_init_internal (klass);

	if (mono_class_is_ginst (klass) && !m_class_get_methods (klass)) {
		res = mono_class_get_method_from_name_checked (mono_class_get_generic_class (klass)->container_class, name, param_count, flags, error);

		if (res)
			res = mono_class_inflate_generic_method_full_checked (res, klass, mono_class_get_context (klass), error);

		return res;
	}

	if (m_class_get_methods (klass) || !MONO_CLASS_HAS_STATIC_METADATA (klass)) {
		mono_class_setup_methods (klass);
		/*
		We can't fail lookup of methods otherwise the runtime will burst in flames on all sort of places.
		See mono/tests/array_load_exception.il
		FIXME we should better report this error to the caller
		 */
		MonoMethod **klass_methods = m_class_get_methods (klass);
		gboolean has_updates = m_class_get_image (klass)->has_updates;
		if (!klass_methods && !has_updates)
			return NULL;
		int mcount = mono_class_get_method_count (klass);
		for (i = 0; i < mcount; ++i) {
			MonoMethod *method = klass_methods [i];

			if (method->name[0] == name [0] &&
				!strcmp (name, method->name) &&
				(param_count == -1 || mono_method_signature_internal (method)->param_count == param_count) &&
				((method->flags & flags) == flags)) {
				res = method;
				break;
			}
		}
		if (G_UNLIKELY (!res && has_updates && mono_class_has_metadata_update_info (klass))) {
			res = mono_metadata_update_find_method_by_name (klass, name, param_count, flags, error);
		}
	}
	else {
	    res = mono_find_method_in_metadata (klass, name, param_count, flags);
	}

	return res;
}

gboolean
mono_class_has_failure (const MonoClass *klass)
{
	g_assert (klass != NULL);
	return m_class_has_failure ((MonoClass*)klass) != 0;
}


/**
 * mono_class_set_type_load_failure:
 * \param klass class in which the failure was detected
 * \param fmt \c printf -style error message string.
 *
 * Collect detected failure informaion in the class for later processing.
 * The error is stored as a MonoErrorBoxed as with mono_error_set_type_load_class()
 * Note that only the first failure is kept.
 *
 * LOCKING: Acquires the loader lock.
 *
 * \returns FALSE if a failure was already set on the class, or TRUE otherwise.
 */
gboolean
mono_class_set_type_load_failure (MonoClass *klass, const char * fmt, ...)
{
	ERROR_DECL (prepare_error);
	va_list args;

	if (mono_class_has_failure (klass))
		return FALSE;

	va_start (args, fmt);
	mono_error_vset_type_load_class (prepare_error, klass, fmt, args);
	va_end (args);

	MonoErrorBoxed *box = mono_error_box (prepare_error, m_class_get_image (klass));
	mono_error_cleanup (prepare_error);
	return mono_class_set_failure (klass, box);
}

/**
 * mono_class_get_exception_for_failure:
 * \param klass class in which the failure was detected
 *
 * \returns a constructed MonoException than the caller can then throw
 * using mono_raise_exception - or NULL if no failure is present (or
 * doesn't result in an exception).
 */
MonoException*
mono_class_get_exception_for_failure (MonoClass *klass)
{
	if (!mono_class_has_failure (klass))
		return NULL;
	ERROR_DECL (unboxed_error);
	mono_error_set_for_class_failure (unboxed_error, klass);
	return mono_error_convert_to_exception (unboxed_error);
}

static gboolean
is_nesting_type (MonoClass *outer_klass, MonoClass *inner_klass)
 {
	outer_klass = mono_class_get_generic_type_definition (outer_klass);
	inner_klass = mono_class_get_generic_type_definition (inner_klass);
	do {
		if (outer_klass == inner_klass)
			return TRUE;
		inner_klass = m_class_get_nested_in (inner_klass);
	} while (inner_klass);
	return FALSE;
}

MonoClass *
mono_class_get_generic_type_definition (MonoClass *klass)
{
	MonoGenericClass *gklass =  mono_class_try_get_generic_class (klass);
	return gklass ? gklass->container_class : klass;
}

/*
 * Check if @klass is a subtype of @parent ignoring generic instantiations.
 *
 * Generic instantiations are ignored for all super types of @klass.
 *
 * Visibility checks ignoring generic instantiations.
 *
 * Class implementing interface visibility checks ignore generic instantiations
 */
gboolean
mono_class_has_parent_and_ignore_generics (MonoClass *klass, MonoClass *parent)
{
	int i;
	klass = mono_class_get_generic_type_definition (klass);
	parent = mono_class_get_generic_type_definition (parent);
	mono_class_setup_supertypes (klass);

	for (i = 0; i < m_class_get_idepth (klass); ++i) {
		if (parent == mono_class_get_generic_type_definition (m_class_get_supertypes (klass) [i]))
			return TRUE;
	}

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (parent) && class_implements_interface_ignore_generics (klass, parent))
		return TRUE;

	return FALSE;
}
/*
 * Subtype can only access parent members with family protection if the site object
 * is subclass of Subtype. For example:
 * class A { protected int x; }
 * class B : A {
 * 	void valid_access () {
 * 		B b;
 * 		b.x = 0;
 *  }
 *  void invalid_access () {
 *		A a;
 * 		a.x = 0;
 *  }
 * }
 * */
static gboolean
is_valid_family_access (MonoClass *access_klass, MonoClass *member_klass, MonoClass *context_klass)
{
	if (MONO_CLASS_IS_INTERFACE_INTERNAL (member_klass) && !MONO_CLASS_IS_INTERFACE_INTERNAL (access_klass)) {
		/* Can happen with default interface methods */
		if (!class_implements_interface_ignore_generics (access_klass, member_klass))
			return FALSE;
	} else if (member_klass != access_klass && MONO_CLASS_IS_INTERFACE_INTERNAL (member_klass) && MONO_CLASS_IS_INTERFACE_INTERNAL (access_klass)) {
		/* Can happen with default interface methods */
		if (!mono_interface_implements_interface (access_klass, member_klass))
			return FALSE;
	} else {
		if (!mono_class_has_parent_and_ignore_generics (access_klass, member_klass))
			return FALSE;
	}

	if (context_klass == NULL)
		return TRUE;
	/*if access_klass is not member_klass context_klass must be type compat*/
	if (access_klass != member_klass && !mono_class_has_parent_and_ignore_generics (context_klass, access_klass))
		return FALSE;
	return TRUE;
}

static gboolean
ignores_access_checks_to (MonoAssembly *accessing, MonoAssembly *accessed)
{
	if (!accessing || !accessed)
		return FALSE;

	mono_assembly_load_friends (accessing);
	for (GSList *tmp = accessing->ignores_checks_assembly_names; tmp; tmp = tmp->next) {
		MonoAssemblyName *victim = (MonoAssemblyName *)tmp->data;
		if (!victim->name)
			continue;
		if (!g_ascii_strcasecmp (accessed->aname.name, victim->name))
			return TRUE;
	}
	return FALSE;
}

static gboolean
can_access_internals (MonoAssembly *accessing, MonoAssembly* accessed)
{
	GSList *tmp;
	if (accessing == accessed)
		return TRUE;
	if (!accessed || !accessing)
		return FALSE;

	mono_assembly_load_friends (accessed);
	for (tmp = accessed->friend_assembly_names; tmp; tmp = tmp->next) {
		MonoAssemblyName *friend_ = (MonoAssemblyName *)tmp->data;
		/* Be conservative with checks */
		if (!friend_->name)
			continue;
		if (g_ascii_strcasecmp (accessing->aname.name, friend_->name))
			continue;
		if (friend_->public_key_token [0]) {
			if (!accessing->aname.public_key_token [0])
				continue;
			if (!mono_public_tokens_are_equal (friend_->public_key_token, accessing->aname.public_key_token))
				continue;
		}
		return TRUE;
	}
	return ignores_access_checks_to (accessing, accessed);
}

/*
 * If klass is a generic type or if it is derived from a generic type, return the
 * MonoClass of the generic definition
 * Returns NULL if not found
 */
static MonoClass*
get_generic_definition_class (MonoClass *klass)
{
	while (klass) {
		MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
		if (gklass && gklass->container_class)
			return gklass->container_class;
		klass = m_class_get_parent (klass);
	}
	return NULL;
}

static gboolean
can_access_instantiation (MonoClass *access_klass, MonoGenericInst *ginst)
{
	int i;
	for (i = 0; i < ginst->type_argc; ++i) {
		MonoType *type = ginst->type_argv[i];
		switch (type->type) {
		case MONO_TYPE_SZARRAY:
			if (!can_access_type (access_klass, type->data.klass))
				return FALSE;
			break;
		case MONO_TYPE_ARRAY:
			if (!can_access_type (access_klass, type->data.array->eklass))
				return FALSE;
			break;
		case MONO_TYPE_PTR:
			if (!can_access_type (access_klass, mono_class_from_mono_type_internal (type->data.type)))
				return FALSE;
			break;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			if (!can_access_type (access_klass, mono_class_from_mono_type_internal (type)))
				return FALSE;
		default:
			break;
		}
	}
	return TRUE;
}

static gboolean
can_access_type (MonoClass *access_klass, MonoClass *member_klass)
{
	int access_level;

	if (access_klass == member_klass)
		return TRUE;

	MonoAssembly *access_klass_assembly = m_class_get_image (access_klass)->assembly;
	MonoAssembly *member_klass_assembly = m_class_get_image (member_klass)->assembly;

	if (m_class_get_element_class (access_klass) && !m_class_is_enumtype (access_klass)) {
		access_klass = m_class_get_element_class (access_klass);
		access_klass_assembly = m_class_get_image (access_klass)->assembly;
	}

	if (m_class_get_element_class (member_klass) && !m_class_is_enumtype (member_klass)) {
		member_klass = m_class_get_element_class (member_klass);
		member_klass_assembly = m_class_get_image (member_klass)->assembly;
	}

	access_level = mono_class_get_flags (member_klass) & TYPE_ATTRIBUTE_VISIBILITY_MASK;

	if (mono_type_is_generic_argument (m_class_get_byval_arg (member_klass)))
		return TRUE;

	if (mono_class_is_ginst (member_klass) && !can_access_instantiation (access_klass, mono_class_get_generic_class (member_klass)->context.class_inst))
		return FALSE;

	if (is_nesting_type (access_klass, member_klass) || (m_class_get_nested_in (access_klass) && is_nesting_type (m_class_get_nested_in (access_klass), member_klass)))
		return TRUE;

	/*Non nested type with nested visibility. We just fail it.*/
	if (access_level >= TYPE_ATTRIBUTE_NESTED_PRIVATE && access_level <= TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM && m_class_get_nested_in (member_klass) == NULL)
		return FALSE;

	MonoClass *member_klass_nested_in = m_class_get_nested_in (member_klass);
	switch (access_level) {
	case TYPE_ATTRIBUTE_NOT_PUBLIC:
		return can_access_internals (access_klass_assembly, member_klass_assembly);

	case TYPE_ATTRIBUTE_PUBLIC:
		return TRUE;

	case TYPE_ATTRIBUTE_NESTED_PUBLIC:
		return member_klass_nested_in && can_access_type (access_klass, member_klass_nested_in);

	case TYPE_ATTRIBUTE_NESTED_PRIVATE:
		if (is_nesting_type (member_klass, access_klass) && member_klass_nested_in && can_access_type (access_klass, member_klass_nested_in))
			return TRUE;
		return ignores_access_checks_to (access_klass_assembly, member_klass_assembly);

	case TYPE_ATTRIBUTE_NESTED_FAMILY:
		return mono_class_has_parent_and_ignore_generics (access_klass, m_class_get_nested_in (member_klass));

	case TYPE_ATTRIBUTE_NESTED_ASSEMBLY:
		return can_access_internals (access_klass_assembly, member_klass_assembly) && member_klass_nested_in && can_access_type (access_klass, member_klass_nested_in);

	case TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM:
		return can_access_internals (access_klass_assembly, m_class_get_image (member_klass_nested_in)->assembly) &&
			mono_class_has_parent_and_ignore_generics (access_klass, member_klass_nested_in);

	case TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM:
		return can_access_internals (access_klass_assembly, m_class_get_image (member_klass_nested_in)->assembly) ||
			mono_class_has_parent_and_ignore_generics (access_klass, member_klass_nested_in);
	}
	return FALSE;
}

/* FIXME: check visibility of type, too */
static gboolean
can_access_member (MonoClass *access_klass, MonoClass *member_klass, MonoClass* context_klass, int access_level)
{
	MonoClass *member_generic_def;
	MonoAssembly *access_klass_assembly = m_class_get_image (access_klass)->assembly;

	MonoGenericClass *access_gklass = mono_class_try_get_generic_class (access_klass);
	if (((access_gklass && access_gklass->container_class) ||
					mono_class_is_gtd (access_klass)) &&
			(member_generic_def = get_generic_definition_class (member_klass))) {
		MonoClass *access_container;

		if (mono_class_is_gtd (access_klass))
			access_container = access_klass;
		else
			access_container = access_gklass->container_class;

		if (can_access_member (access_container, member_generic_def, context_klass, access_level))
			return TRUE;
	}

	MonoImage *member_klass_image = m_class_get_image (member_klass);
	/* Partition I 8.5.3.2 */
	/* the access level values are the same for fields and methods */
	switch (access_level) {
	case FIELD_ATTRIBUTE_COMPILER_CONTROLLED:
		/* same compilation unit */
		return m_class_get_image (access_klass) == member_klass_image;
	case FIELD_ATTRIBUTE_PRIVATE:
		return (access_klass == member_klass) || ignores_access_checks_to (access_klass_assembly, member_klass_image->assembly);
	case FIELD_ATTRIBUTE_FAM_AND_ASSEM:
		if (is_valid_family_access (access_klass, member_klass, context_klass) &&
		    can_access_internals (access_klass_assembly, member_klass_image->assembly))
			return TRUE;
		return FALSE;
	case FIELD_ATTRIBUTE_ASSEMBLY:
		return can_access_internals (access_klass_assembly, member_klass_image->assembly);
	case FIELD_ATTRIBUTE_FAMILY:
		if (is_valid_family_access (access_klass, member_klass, context_klass))
			return TRUE;
		return FALSE;
	case FIELD_ATTRIBUTE_FAM_OR_ASSEM:
		if (is_valid_family_access (access_klass, member_klass, context_klass))
			return TRUE;
		return can_access_internals (access_klass_assembly, member_klass_image->assembly);
	case FIELD_ATTRIBUTE_PUBLIC:
		return TRUE;
	}
	return FALSE;
}

/**
 * mono_method_can_access_field:
 * \param method Method that will attempt to access the field
 * \param field the field to access
 *
 * Used to determine if a method is allowed to access the specified field.
 *
 * \returns TRUE if the given \p method is allowed to access the \p field while following
 * the accessibility rules of the CLI.
 */
gboolean
mono_method_can_access_field (MonoMethod *method, MonoClassField *field)
{
	/* FIXME: check all overlapping fields */
	int can = can_access_member (method->klass, m_field_get_parent (field), NULL, mono_field_get_type_internal (field)->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = m_class_get_nested_in (method->klass);
		while (nested) {
			can = can_access_member (nested, m_field_get_parent (field), NULL, mono_field_get_type_internal (field)->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
			if (can)
				return TRUE;
			nested = m_class_get_nested_in (nested);
		}
	}
	return can;
}

static MonoMethod*
mono_method_get_method_definition (MonoMethod *method)
{
	while (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;
	return method;
}

/**
 * mono_method_can_access_method:
 * \param method Method that will attempt to access the other method
 * \param called the method that we want to probe for accessibility.
 *
 * Used to determine if the \p method is allowed to access the specified \p called method.
 *
 * \returns TRUE if the given \p method is allowed to invoke the \p called while following
 * the accessibility rules of the CLI.
 */
gboolean
mono_method_can_access_method (MonoMethod *method, MonoMethod *called)
{
	method = mono_method_get_method_definition (method);
	called = mono_method_get_method_definition (called);
	return mono_method_can_access_method_full (method, called, NULL);
}

/*
 * mono_method_can_access_method_full:
 * @method: The caller method
 * @called: The called method
 * @context_klass: The static type on stack of the owner @called object used
 *
 * This function must be used with instance calls, as they have more strict family accessibility.
 * It can be used with static methods, but context_klass should be NULL.
 *
 * Returns: TRUE if caller have proper visibility and acessibility to @called
 */
gboolean
mono_method_can_access_method_full (MonoMethod *method, MonoMethod *called, MonoClass *context_klass)
{
	/* Wrappers are except from access checks */
	if (method->wrapper_type != MONO_WRAPPER_NONE || called->wrapper_type != MONO_WRAPPER_NONE)
		return TRUE;

	MonoClass *access_class = method->klass;
	MonoClass *member_class = called->klass;
	int can = can_access_member (access_class, member_class, context_klass, called->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = m_class_get_nested_in (access_class);
		while (nested) {
			can = can_access_member (nested, member_class, context_klass, called->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK);
			if (can)
				break;
			nested = m_class_get_nested_in (nested);
		}
	}

	if (!can)
		return FALSE;

	can = can_access_type (access_class, member_class);
	if (!can) {
		MonoClass *nested = m_class_get_nested_in (access_class);
		while (nested) {
			can = can_access_type (nested, member_class);
			if (can)
				break;
			nested = m_class_get_nested_in (nested);
		}
	}

	if (!can)
		return FALSE;

	if (called->is_inflated) {
		MonoMethodInflated * infl = (MonoMethodInflated*)called;
		if (infl->context.method_inst && !can_access_instantiation (access_class, infl->context.method_inst))
			return FALSE;
	}

	return TRUE;
}


/*
 * mono_method_can_access_field_full:
 * @method: The caller method
 * @field: The accessed field
 * @context_klass: The static type on stack of the owner @field object used
 *
 * This function must be used with instance fields, as they have more strict family accessibility.
 * It can be used with static fields, but context_klass should be NULL.
 *
 * Returns: TRUE if caller have proper visibility and acessibility to @field
 */
gboolean
mono_method_can_access_field_full (MonoMethod *method, MonoClassField *field, MonoClass *context_klass)
{
	MonoClass *access_class = method->klass;
	MonoClass *member_class = m_field_get_parent (field);
	/* FIXME: check all overlapping fields */
	int can = can_access_member (access_class, member_class, context_klass, field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = m_class_get_nested_in (access_class);
		while (nested) {
			can = can_access_member (nested, member_class, context_klass, field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
			if (can)
				break;
			nested = m_class_get_nested_in (nested);
		}
	}

	if (!can)
		return FALSE;

	can = can_access_type (access_class, member_class);
	if (!can) {
		MonoClass *nested = m_class_get_nested_in (access_class);
		while (nested) {
			can = can_access_type (nested, member_class);
			if (can)
				break;
			nested = m_class_get_nested_in (nested);
		}
	}

	if (!can)
		return FALSE;
	return TRUE;
}

/*
 * mono_class_can_access_class:
 * @source_class: The source class
 * @target_class: The accessed class
 *
 * This function returns is @target_class is visible to @source_class
 *
 * Returns: TRUE if source have proper visibility and acessibility to target
 */
gboolean
mono_class_can_access_class (MonoClass *source_class, MonoClass *target_class)
{
	return can_access_type (source_class, target_class);
}

/**
 * mono_type_is_valid_enum_basetype:
 * \param type The MonoType to check
 * \returns TRUE if the type can be used as the basetype of an enum
 */
gboolean mono_type_is_valid_enum_basetype (MonoType * type) {
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_R8:
	case MONO_TYPE_R4:
		return TRUE;
	default:
		return FALSE;
	}
}

/**
 * mono_class_is_valid_enum:
 * \param klass An enum class to be validated
 *
 * This method verify the required properties an enum should have.
 *
 * FIXME: TypeBuilder enums are allowed to implement interfaces, but since they cannot have methods, only empty interfaces are possible
 * FIXME: enum types are not allowed to have a cctor, but mono_reflection_create_runtime_class sets has_cctor to 1 for all types
 * FIXME: TypeBuilder enums can have any kind of static fields, but the spec is very explicit about that (P II 14.3)
 *
 * \returns TRUE if the informed enum class is valid
 */
gboolean
mono_class_is_valid_enum (MonoClass *klass)
{
	MonoClassField * field;
	gpointer iter = NULL;
	gboolean found_base_field = FALSE;

	g_assert (m_class_is_enumtype (klass));
	MonoClass *klass_parent = m_class_get_parent (klass);
	/* we cannot test against mono_defaults.enum_class, or mcs won't be able to compile the System namespace*/
	if (!klass_parent || strcmp (m_class_get_name (klass_parent), "Enum") || strcmp (m_class_get_name_space (klass_parent), "System") ) {
		return FALSE;
	}

	if (!mono_class_is_auto_layout (klass))
		return FALSE;

	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			if (found_base_field)
				return FALSE;
			found_base_field = TRUE;
			if (!mono_type_is_valid_enum_basetype (field->type))
				return FALSE;
		}
	}

	if (!found_base_field)
		return FALSE;

	if (mono_class_get_method_count (klass) > 0)
		return FALSE;

	return TRUE;
}

gboolean
mono_generic_class_is_generic_type_definition (MonoGenericClass *gklass)
{
	return gklass->context.class_inst == mono_class_get_generic_container (gklass->container_class)->context.class_inst;
}

void
mono_field_resolve_type (MonoClassField *field, MonoError *error)
{
	MonoClass *klass = m_field_get_parent (field);
	MonoImage *image = m_class_get_image (klass);
	MonoClass *gtd = mono_class_is_ginst (klass) ? mono_class_get_generic_type_definition (klass) : NULL;
	MonoType *ftype;
	int field_idx;

	if (G_UNLIKELY (m_field_is_from_update (field))) {
		field_idx = -1;
	} else {
		field_idx = field - m_class_get_fields (klass);
	}

	error_init (error);

	if (gtd) {
		g_assert (field_idx != -1);
		MonoClassField *gfield = &m_class_get_fields (gtd) [field_idx];
		MonoType *gtype = mono_field_get_type_checked (gfield, error);
		if (!is_ok (error)) {
			char *full_name = mono_type_get_full_name (gtd);
			mono_class_set_type_load_failure (klass, "Could not load generic type of field '%s:%s' (%d) due to: %s", full_name, gfield->name, field_idx, mono_error_get_message (error));
			g_free (full_name);
		}

		ftype = mono_class_inflate_generic_type_no_copy (image, gtype, mono_class_get_context (klass), error);
		if (!is_ok (error)) {
			char *full_name = mono_type_get_full_name (klass);
			mono_class_set_type_load_failure (klass, "Could not load instantiated type of field '%s:%s' (%d) due to: %s", full_name, field->name, field_idx, mono_error_get_message (error));
			g_free (full_name);
		}
	} else {
		const char *sig;
		guint32 cols [MONO_FIELD_SIZE];
		MonoGenericContainer *container = NULL;
		int idx;

		if (G_UNLIKELY (m_field_is_from_update (field))) {
			idx = mono_metadata_update_get_field_idx (field) - 1;
		} else {
			idx = mono_class_get_first_field_idx (klass) + field_idx;
		}

		/*FIXME, in theory we do not lazy load SRE fields*/
		g_assert (!image_is_dynamic (image));

		if (mono_class_is_gtd (klass)) {
			container = mono_class_get_generic_container (klass);
		} else if (gtd) {
			container = mono_class_get_generic_container (gtd);
			g_assert (container);
		}

		/* first_field_idx and idx points into the fieldptr table */
		mono_metadata_decode_table_row (image, MONO_TABLE_FIELD, idx, cols, MONO_FIELD_SIZE);

		sig = mono_metadata_blob_heap (image, cols [MONO_FIELD_SIGNATURE]);

		mono_metadata_decode_value (sig, &sig);
		/* FIELD signature == 0x06 */
		g_assert (*sig == 0x06);

		ftype = mono_metadata_parse_type_checked (image, container, cols [MONO_FIELD_FLAGS], FALSE, sig + 1, &sig, error);
		if (!ftype) {
			char *full_name = mono_type_get_full_name (klass);
			mono_class_set_type_load_failure (klass, "Could not load type of field '%s:%s' (%d) due to: %s", full_name, field->name, field_idx, mono_error_get_message (error));
			g_free (full_name);
		}
	}
	mono_memory_barrier ();
	field->type = ftype;
}

static guint32
mono_field_resolve_flags (MonoClassField *field)
{
	/* Fields in metadata updates are pre-resolved, so this method should not be called. */
	g_assert (!m_field_is_from_update (field));

	MonoClass *klass = m_field_get_parent (field);
	MonoImage *image = m_class_get_image (klass);
	MonoClass *gtd = mono_class_is_ginst (klass) ? mono_class_get_generic_type_definition (klass) : NULL;
	int field_idx = field - m_class_get_fields (klass);

	if (gtd) {
		MonoClassField *gfield = &m_class_get_fields (gtd) [field_idx];
		return mono_field_get_flags (gfield);
	} else {
		int idx = mono_class_get_first_field_idx (klass) + field_idx;

		/*FIXME, in theory we do not lazy load SRE fields*/
		g_assert (!image_is_dynamic (image));

		return mono_metadata_decode_table_row_col (image, MONO_TABLE_FIELD, idx, MONO_FIELD_FLAGS);
	}
}

/**
 * mono_class_get_fields_lazy:
 * \param klass the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the fields in a class.
 * Only minimal information about fields are loaded. Accessors must be used
 * for all MonoClassField returned.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * \returns a \c MonoClassField* on each iteration, or NULL when no more fields are available.
 */
MonoClassField*
mono_class_get_fields_lazy (MonoClass* klass, gpointer *iter)
{
	if (!iter)
		return NULL;
	MonoImage *image = m_class_get_image (klass);
	if (!*iter) {
		mono_class_setup_basic_field_info (klass);
		MonoClassField *klass_fields = m_class_get_fields (klass);
		if (!klass_fields)
			return NULL;
		/* start from the first */
		if (mono_class_get_field_count (klass)) {
			uint32_t idx = 0;
			*iter = GUINT_TO_POINTER (idx+1);
			return &klass_fields [0];
		} else {
			/* no fields */
			if (G_LIKELY(!image->has_updates))
				return NULL;
			else
				*iter = 0;
		}
	}
	// invariant: idx is one past the field we previously returned
	uint32_t idx = GPOINTER_TO_UINT(*iter);
	if (idx < mono_class_get_field_count (klass)) {
		MonoClassField *field = &m_class_get_fields (klass) [idx];
		++idx;
		*iter = GUINT_TO_POINTER (idx);
		return field;
	}
	if (G_UNLIKELY (image->has_updates)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "Lazy iterating added fields %s", m_class_get_name(klass));
		return mono_metadata_update_added_fields_iter (klass, TRUE, iter);
	}
	return NULL;
}

char*
mono_class_full_name (MonoClass *klass)
{
	return mono_type_full_name (m_class_get_byval_arg (klass));
}

/* Declare all shared lazy type lookup functions */
GENERATE_TRY_GET_CLASS_WITH_CACHE (safehandle, "System.Runtime.InteropServices", "SafeHandle")

/**
 * mono_method_get_base_method:
 * \param method a method
 * \param definition if true, get the definition
 * \param error set on failure
 *
 * Given a virtual method associated with a subclass, return the corresponding
 * method from an ancestor.  If \p definition is FALSE, returns the method in the
 * superclass of the given method.  If \p definition is TRUE, return the method
 * in the ancestor class where it was first declared.  The type arguments will
 * be inflated in the ancestor classes.  If the method is not associated with a
 * class, or isn't virtual, returns the method itself.  On failure returns NULL
 * and sets \p error.
 */
MonoMethod*
mono_method_get_base_method (MonoMethod *method, gboolean definition, MonoError *error)
{
	MonoClass *klass, *parent;
	MonoGenericContext *generic_inst = NULL;
	MonoMethod *result = NULL;
	int slot;

	if (method->klass == NULL)
		return method;

	if (!(method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
	    MONO_CLASS_IS_INTERFACE_INTERNAL (method->klass) ||
	    method->flags & METHOD_ATTRIBUTE_NEW_SLOT)
		return method;

	slot = mono_method_get_vtable_slot (method);
	if (slot == -1)
		return method;

	klass = method->klass;
	if (mono_class_is_gtd (klass)) {
		/* If we get a GTD like Foo`2 replace look instead at its instantiation with its own generic params: Foo`2<!0, !1>. */
		/* In particular we want generic_inst to be initialized to <!0,
		 * !1> so that we can inflate parent classes correctly as we go
		 * up the class hierarchy. */
		MonoType *ty = mono_class_gtd_get_canonical_inst (klass);
		g_assert (ty->type == MONO_TYPE_GENERICINST);
		MonoGenericClass *gklass = ty->data.generic_class;
		generic_inst = mono_generic_class_get_context (gklass);
		klass = gklass->container_class;
	} else if (mono_class_is_ginst (klass)) {
		generic_inst = mono_class_get_context (klass);
		klass = mono_class_get_generic_class (klass)->container_class;
	}

retry:
	if (definition) {
		/* At the end of the loop, klass points to the eldest class that has this virtual function slot. */
		for (parent = m_class_get_parent (klass); parent != NULL; parent = m_class_get_parent (parent)) {
			/* on entry, klass is either a plain old non-generic class and generic_inst == NULL
			   or klass is the generic container class and generic_inst is the instantiation.

			   when we go to the parent, if the parent is an open constructed type, we need to
			   replace the type parameters by the definitions from the generic_inst, and then take it
			   apart again into the klass and the generic_inst.

			   For cases like this:
			   class C<T> : B<T, int> {
			       public override void Foo () { ... }
			   }
			   class B<U,V> : A<HashMap<U,V>> {
			       public override void Foo () { ... }
			   }
			   class A<X> {
			       public virtual void Foo () { ... }
			   }

			   if at each iteration the parent isn't open, we can skip inflating it.  if at some
			   iteration the parent isn't generic (after possible inflation), we set generic_inst to
			   NULL;
			*/
			MonoGenericContext *parent_inst = NULL;
			if (mono_class_is_open_constructed_type (m_class_get_byval_arg (parent))) {
				parent = mono_class_inflate_generic_class_checked (parent, generic_inst, error);
				return_val_if_nok  (error, NULL);
			}
			if (mono_class_is_ginst (parent)) {
				parent_inst = mono_class_get_context (parent);
				parent = mono_class_get_generic_class (parent)->container_class;
			}

			mono_class_setup_vtable (parent);
			if (m_class_get_vtable_size (parent) <= slot)
				break;
			klass = parent;
			generic_inst = parent_inst;
		}
	} else {
		/* When we get here, possibly after a retry, if generic_inst is
		 * set, then the class is must be a gtd */
		g_assert (generic_inst == NULL || mono_class_is_gtd (klass));

		klass = m_class_get_parent (klass);
		if (!klass)
			return method;
		if (mono_class_is_open_constructed_type (m_class_get_byval_arg (klass))) {
			klass = mono_class_inflate_generic_class_checked (klass, generic_inst, error);
			return_val_if_nok (error, NULL);

			generic_inst = NULL;
		}
		if (mono_class_is_ginst (klass)) {
			generic_inst = mono_class_get_context (klass);
			klass = mono_class_get_generic_class (klass)->container_class;
		}

	}

	if (generic_inst) {
		klass = mono_class_inflate_generic_class_checked (klass, generic_inst, error);
		return_val_if_nok (error, NULL);
		generic_inst = NULL;
	}

	if (klass == method->klass)
		return method;

	/*This is possible if definition == FALSE.
	 * Do it here to be really sure we don't read invalid memory.
	 */
	if (slot >= m_class_get_vtable_size (klass))
		return method;

	mono_class_setup_vtable (klass);

	result = m_class_get_vtable (klass) [slot];
	if (result == NULL) {
		/* It is an abstract method */
		gboolean found = FALSE;
		gpointer iter = NULL;
		while ((result = mono_class_get_methods (klass, &iter))) {
			if (result->slot == slot) {
				found = TRUE;
				break;
			}
		}
		/* found might be FALSE if we looked in an abstract class
		 * that doesn't override an abstract method of its
		 * parent:
		 *   abstract class Base {
		 *     public abstract void Foo ();
		 *   }
		 *   abstract class Derived : Base { }
		 *   class Child : Derived {
		 *     public override void Foo () { }
		 *  }
		 *
		 *  if m was Child.Foo and we ask for the base method,
		 *  then we get here with klass == Derived and found == FALSE
		 */
		/* but it shouldn't be the case that if we're looking
		 * for the definition and didn't find a result; the
		 * loop above should've taken us as far as we could
		 * go! */
		g_assert (!(definition && !found));
		if (!found)
			goto retry;
	}

	g_assert (result != NULL);
	return result;
}

gboolean
mono_method_is_constructor (MonoMethod *method)
{
	return ((method->flags & CTOR_REQUIRED_FLAGS) == CTOR_REQUIRED_FLAGS &&
			!(method->flags & CTOR_INVALID_FLAGS) &&
			!strcmp (".ctor", method->name));
}

gboolean
mono_class_has_default_constructor (MonoClass *klass, gboolean public_only)
{
	MonoMethod *method;
	int i;

	mono_class_setup_methods (klass);
	if (mono_class_has_failure (klass))
		return FALSE;

	int mcount = mono_class_get_method_count (klass);
	MonoMethod **klass_methods = m_class_get_methods (klass);
	for (i = 0; i < mcount; ++i) {
		method = klass_methods [i];
		if (mono_method_is_constructor (method) &&
			mono_method_signature_internal (method) &&
			mono_method_signature_internal (method)->param_count == 0 &&
			(!public_only || (method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC))
			return TRUE;
	}
	return FALSE;
}
