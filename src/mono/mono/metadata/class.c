/*
 * class.c: Class management for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
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
#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/attrdefs.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-string.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/atomic.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/checked-build.h>

MonoStats mono_stats;

gboolean mono_print_vtable = FALSE;
gboolean mono_align_small_structs = FALSE;

/* Statistics */
guint32 inflated_classes, inflated_classes_size, inflated_methods_size;
guint32 classes_size, class_ext_size;

/* Low level lock which protects data structures in this module */
static mono_mutex_t classes_mutex;

/* Function supplied by the runtime to find classes by name using information from the AOT file */
static MonoGetClassFromName get_class_from_name = NULL;

static MonoClass * mono_class_create_from_typedef (MonoImage *image, guint32 type_token, MonoError *error);
static gboolean mono_class_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res);
static gboolean can_access_type (MonoClass *access_klass, MonoClass *member_klass);
static MonoMethod* find_method_in_metadata (MonoClass *klass, const char *name, int param_count, int flags);
static int generic_array_methods (MonoClass *klass);
static void setup_generic_array_ifaces (MonoClass *klass, MonoClass *iface, MonoMethod **methods, int pos);

static MonoMethod* mono_class_get_virtual_methods (MonoClass* klass, gpointer *iter);
static char* mono_assembly_name_from_token (MonoImage *image, guint32 type_token);
static void mono_field_resolve_type (MonoClassField *field, MonoError *error);
static guint32 mono_field_resolve_flags (MonoClassField *field);
static void mono_class_setup_vtable_full (MonoClass *klass, GList *in_setup);
static void mono_generic_class_setup_parent (MonoClass *klass, MonoClass *gklass);

/*
We use gclass recording to allow recursive system f types to be referenced by a parent.

Given the following type hierarchy:

class TextBox : TextBoxBase<TextBox> {}
class TextBoxBase<T> : TextInput<TextBox> where T : TextBoxBase<T> {}
class TextInput<T> : Input<T> where T: TextInput<T> {}
class Input<T> {}

The runtime tries to load TextBoxBase<>.
To load TextBoxBase<> to do so it must resolve the parent which is TextInput<TextBox>.
To instantiate TextInput<TextBox> it must resolve TextInput<> and TextBox.
To load TextBox it must resolve the parent which is TextBoxBase<TextBox>.

At this point the runtime must instantiate TextBoxBase<TextBox>. Both types are partially loaded 
at this point, iow, both are registered in the type map and both and a NULL parent. This means
that the resulting generic instance will have a NULL parent, which is wrong and will cause breakage.

To fix that what we do is to record all generic instantes created while resolving the parent of
any generic type definition and, after resolved, correct the parent field if needed.

*/
static int record_gclass_instantiation;
static GSList *gclass_recorded_list;
typedef gboolean (*gclass_record_func) (MonoClass*, void*);

static inline void
classes_lock (void)
{
	mono_locks_os_acquire (&classes_mutex, ClassesLock);
}

static inline void
classes_unlock (void)
{
	mono_locks_os_release (&classes_mutex, ClassesLock);
}

/* 
 * LOCKING: loader lock must be held until pairing disable_gclass_recording is called.
*/
static void
enable_gclass_recording (void)
{
	++record_gclass_instantiation;
}

/* 
 * LOCKING: loader lock must be held since pairing enable_gclass_recording was called.
*/
static void
disable_gclass_recording (gclass_record_func func, void *user_data)
{
	GSList **head = &gclass_recorded_list;

	g_assert (record_gclass_instantiation > 0);
	--record_gclass_instantiation;

	while (*head) {
		GSList *node = *head;
		if (func ((MonoClass*)node->data, user_data)) {
			*head = node->next;
			g_slist_free_1 (node);
		} else {
			head = &node->next;
		}
	}

	/* We automatically discard all recorded gclasses when disabled. */
	if (!record_gclass_instantiation && gclass_recorded_list) {
		g_slist_free (gclass_recorded_list);
		gclass_recorded_list = NULL;
	}
}

/**
 * mono_class_from_typeref:
 * @image: a MonoImage
 * @type_token: a TypeRef token
 *
 * Creates the MonoClass* structure representing the type defined by
 * the typeref token valid inside @image.
 * Returns: The MonoClass* representing the typeref token, NULL ifcould
 * not be loaded.
 */
MonoClass *
mono_class_from_typeref (MonoImage *image, guint32 type_token)
{
	MonoError error;
	MonoClass *klass = mono_class_from_typeref_checked (image, type_token, &error);
	g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/
	return klass;
}

/**
 * mono_class_from_typeref_checked:
 * @image: a MonoImage
 * @type_token: a TypeRef token
 * @error: error return code, if any.
 *
 * Creates the MonoClass* structure representing the type defined by
 * the typeref token valid inside @image.
 *
 * Returns: The MonoClass* representing the typeref token, NULL if it could
 * not be loaded with the @error value filled with the information about the
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

	mono_error_init (error);

	if (!mono_verifier_verify_typeref_row (image, (type_token & 0xffffff) - 1, error))
		return NULL;

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
		module = mono_image_load_module (image, idx);
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

		if (enclosing->nested_classes_inited && enclosing->ext) {
			/* Micro-optimization: don't scan the metadata tables if enclosing is already inited */
			for (tmp = enclosing->ext->nested_classes; tmp; tmp = tmp->next) {
				res = (MonoClass *)tmp->data;
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
					return mono_class_create_from_typedef (enclosing->image, MONO_TOKEN_TYPE_DEF | class_nested, error);

				i = mono_metadata_nesting_typedef (enclosing->image, enclosing->type_token, i + 1);
			}
		}
		g_warning ("TypeRef ResolutionScope not yet handled (%d) for %s.%s in image %s", idx, nspace, name, image->name);
		goto done;
	}
	case MONO_RESOLUTION_SCOPE_ASSEMBLYREF:
		break;
	}

	if (idx > image->tables [MONO_TABLE_ASSEMBLYREF].rows) {
		mono_error_set_bad_image (error, image, "Image with invalid assemblyref token %08x.", idx);
		return NULL;
	}

	if (!image->references || !image->references [idx - 1])
		mono_assembly_load_reference (image, idx - 1);
	g_assert (image->references [idx - 1]);

	/* If the assembly did not load, register this as a type load exception */
	if (image->references [idx - 1] == REFERENCE_MISSING){
		MonoAssemblyName aname;
		char *human_name;
		
		mono_assembly_get_assemblyref (image, idx - 1, &aname);
		human_name = mono_stringify_assembly_name (&aname);
		mono_error_set_assembly_load_simple (error, human_name, image->assembly ? image->assembly->ref_only : FALSE);
		return NULL;
	}

	res = mono_class_from_name_checked (image->references [idx - 1]->image, nspace, name, error);

done:
	/* Generic case, should be avoided for when a better error is possible. */
	if (!res && mono_error_ok (error)) {
		if (mono_loader_get_last_error ()) { /*FIXME plug the above to not leak errors*/
			mono_error_set_from_loader_error (error);
		} else {
			char *name = mono_class_name_from_token (image, type_token);
			char *assembly = mono_assembly_name_from_token (image, type_token);
			mono_error_set_type_load_name (error, name, assembly, "Could not resolve type with token %08x", type_token);
		}
	}
	mono_loader_assert_no_error ();
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
	MonoAssembly *ta = klass->image->assembly;
	char *name;

	name = mono_stringify_assembly_name (&ta->aname);
	g_string_append_printf (str, ", %s", name);
	g_free (name);
}

static inline void
mono_type_name_check_byref (MonoType *type, GString *str)
{
	if (type->byref)
		g_string_append_c (str, '&');
}

/**
 * mono_identifier_escape_type_name_chars:
 * @str: a destination string
 * @identifier: an IDENTIFIER in internal form
 *
 * Returns: str.
 *
 * The displayed form of the identifier is appended to str.
 *
 * The displayed form of an identifier has the characters ,+&*[]\
 * that have special meaning in type names escaped with a preceeding
 * backslash (\) character.
 */
static GString*
mono_identifier_escape_type_name_chars (GString* str, const char* identifier)
{
	if (!identifier)
		return str;

	size_t n = str->len;
	// reserve space for common case: there will be no escaped characters.
	g_string_set_size(str, n + strlen(identifier));
	g_string_set_size(str, n);

	for (const char* s = identifier; *s != 0 ; s++) {
		switch (*s) {
		case ',':
		case '+':
		case '&':
		case '*':
		case '[':
		case ']':
		case '\\':
			g_string_append_c (str, '\\');
			g_string_append_c (str, *s);
			break;
		default:
			g_string_append_c (str, *s);
			break;
		}
	}
	return str;
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
		if (rank == 1)
			g_string_append_c (str, '*');
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
			&type->data.klass->byval_arg, str, FALSE, nested_format);
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
			_mono_type_get_assembly_name (mono_class_from_mono_type (type->data.type), str);
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (!mono_generic_param_info (type->data.generic_param))
			g_string_append_printf (str, "%s%d", type->type == MONO_TYPE_VAR ? "!" : "!!", type->data.generic_param->num);
		else
			g_string_append (str, mono_generic_param_info (type->data.generic_param)->name);

		mono_type_name_check_byref (type, str);

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
			if (format == MONO_TYPE_NAME_FORMAT_IL)
				g_string_append (str, klass->name_space);
			else
				mono_identifier_escape_type_name_chars (str, klass->name_space);
			g_string_append_c (str, '.');
		}
		if (format == MONO_TYPE_NAME_FORMAT_IL) {
			char *s = strchr (klass->name, '`');
			int len = s ? s - klass->name : strlen (klass->name);
			g_string_append_len (str, klass->name, len);
		} else {
			mono_identifier_escape_type_name_chars (str, klass->name);
		}
		if (is_recursed)
			break;
		if (klass->generic_class) {
			MonoGenericClass *gclass = klass->generic_class;
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
				g_string_append (str, mono_generic_container_get_param_info (klass->generic_container, i)->name);
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
 * @type: a type
 * @format: the format for the return string.
 *
 * 
 * Returns: The string representation in a number of formats:
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

	result = g_string_new ("");

	mono_type_get_name_recurse (type, result, FALSE, format);

	return g_string_free (result, FALSE);
}

/**
 * mono_type_get_full_name:
 * @class: a class
 *
 * Returns: The string representation for type as required by System.Reflection.
 * The inverse of mono_reflection_parse_type ().
 */
char *
mono_type_get_full_name (MonoClass *klass)
{
	return mono_type_get_name_full (mono_class_get_type (klass), MONO_TYPE_NAME_FORMAT_REFLECTION);
}

/**
 * mono_type_get_name:
 * @type: a type
 *
 * Returns: The string representation for type as it would be represented in IL code.
 */
char*
mono_type_get_name (MonoType *type)
{
	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_IL);
}

/*
 * mono_type_get_underlying_type:
 * @type: a type
 *
 * Returns: The MonoType for the underlying integer type if @type
 * is an enum and byref is false, otherwise the type itself.
 */
MonoType*
mono_type_get_underlying_type (MonoType *type)
{
	if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype && !type->byref)
		return mono_class_enum_basetype (type->data.klass);
	if (type->type == MONO_TYPE_GENERICINST && type->data.generic_class->container_class->enumtype && !type->byref)
		return mono_class_enum_basetype (type->data.generic_class->container_class);
	return type;
}

/**
 * mono_class_is_open_constructed_type:
 * @type: a type
 *
 * Returns: TRUE if type represents a generics open constructed type.
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
		return mono_class_is_open_constructed_type (&t->data.klass->byval_arg);
	case MONO_TYPE_ARRAY:
		return mono_class_is_open_constructed_type (&t->data.array->eklass->byval_arg);
	case MONO_TYPE_PTR:
		return mono_class_is_open_constructed_type (t->data.type);
	case MONO_TYPE_GENERICINST:
		return t->data.generic_class->context.class_inst->is_open;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		return t->data.klass->generic_container != NULL;
	default:
		return FALSE;
	}
}

/*
This is a simple function to catch the most common bad instances of generic types.
Specially those that might lead to further failures in the runtime.
*/
static gboolean
is_valid_generic_argument (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_VOID:
	//case MONO_TYPE_TYPEDBYREF:
		return FALSE;
	default:
		return TRUE;
	}
}

static MonoType*
inflate_generic_type (MonoImage *image, MonoType *type, MonoGenericContext *context, MonoError *error)
{
	mono_error_init (error);

	switch (type->type) {
	case MONO_TYPE_MVAR: {
		MonoType *nt;
		int num = mono_type_get_generic_param_num (type);
		MonoGenericInst *inst = context->method_inst;
		if (!inst)
			return NULL;
		if (num >= inst->type_argc) {
			MonoGenericParamInfo *info = mono_generic_param_info (type->data.generic_param);
			mono_error_set_bad_image (error, image, "MVAR %d (%s) cannot be expanded in this context with %d instantiations",
				num, info ? info->name : "", inst->type_argc);
			return NULL;
		}

		if (!is_valid_generic_argument (inst->type_argv [num])) {
			MonoGenericParamInfo *info = mono_generic_param_info (type->data.generic_param);
			mono_error_set_bad_image (error, image, "MVAR %d (%s) cannot be expanded with type 0x%x",
				num, info ? info->name : "", inst->type_argv [num]->type);
			return NULL;			
		}
		/*
		 * Note that the VAR/MVAR cases are different from the rest.  The other cases duplicate @type,
		 * while the VAR/MVAR duplicates a type from the context.  So, we need to ensure that the
		 * ->byref and ->attrs from @type are propagated to the returned type.
		 */
		nt = mono_metadata_type_dup (image, inst->type_argv [num]);
		nt->byref = type->byref;
		nt->attrs = type->attrs;
		return nt;
	}
	case MONO_TYPE_VAR: {
		MonoType *nt;
		int num = mono_type_get_generic_param_num (type);
		MonoGenericInst *inst = context->class_inst;
		if (!inst)
			return NULL;
		if (num >= inst->type_argc) {
			MonoGenericParamInfo *info = mono_generic_param_info (type->data.generic_param);
			mono_error_set_bad_image (error, image, "VAR %d (%s) cannot be expanded in this context with %d instantiations",
				num, info ? info->name : "", inst->type_argc);
			return NULL;
		}
		if (!is_valid_generic_argument (inst->type_argv [num])) {
			MonoGenericParamInfo *info = mono_generic_param_info (type->data.generic_param);
			mono_error_set_bad_image (error, image, "VAR %d (%s) cannot be expanded with type 0x%x",
				num, info ? info->name : "", inst->type_argv [num]->type);
			return NULL;			
		}
		nt = mono_metadata_type_dup (image, inst->type_argv [num]);
		nt->byref = type->byref;
		nt->attrs = type->attrs;
		return nt;
	}
	case MONO_TYPE_SZARRAY: {
		MonoClass *eclass = type->data.klass;
		MonoType *nt, *inflated = inflate_generic_type (NULL, &eclass->byval_arg, context, error);
		if (!inflated || !mono_error_ok (error))
			return NULL;
		nt = mono_metadata_type_dup (image, type);
		nt->data.klass = mono_class_from_mono_type (inflated);
		mono_metadata_free_type (inflated);
		return nt;
	}
	case MONO_TYPE_ARRAY: {
		MonoClass *eclass = type->data.array->eklass;
		MonoType *nt, *inflated = inflate_generic_type (NULL, &eclass->byval_arg, context, error);
		if (!inflated || !mono_error_ok (error))
			return NULL;
		nt = mono_metadata_type_dup (image, type);
		nt->data.array->eklass = mono_class_from_mono_type (inflated);
		mono_metadata_free_type (inflated);
		return nt;
	}
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *gclass = type->data.generic_class;
		MonoGenericInst *inst;
		MonoType *nt;
		if (!gclass->context.class_inst->is_open)
			return NULL;

		inst = mono_metadata_inflate_generic_inst (gclass->context.class_inst, context, error);
		return_val_if_nok (error, NULL);

		if (inst != gclass->context.class_inst)
			gclass = mono_metadata_lookup_generic_class (gclass->container_class, inst, gclass->is_dynamic);

		if (gclass == type->data.generic_class)
			return NULL;

		nt = mono_metadata_type_dup (image, type);
		nt->data.generic_class = gclass;
		return nt;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = type->data.klass;
		MonoGenericContainer *container = klass->generic_container;
		MonoGenericInst *inst;
		MonoGenericClass *gclass = NULL;
		MonoType *nt;

		if (!container)
			return NULL;

		/* We can't use context->class_inst directly, since it can have more elements */
		inst = mono_metadata_inflate_generic_inst (container->context.class_inst, context, error);
		return_val_if_nok (error, NULL);

		if (inst == container->context.class_inst)
			return NULL;

		gclass = mono_metadata_lookup_generic_class (klass, inst, image_is_dynamic (klass->image));

		nt = mono_metadata_type_dup (image, type);
		nt->type = MONO_TYPE_GENERICINST;
		nt->data.generic_class = gclass;
		return nt;
	}
	default:
		return NULL;
	}
	return NULL;
}

MonoGenericContext *
mono_generic_class_get_context (MonoGenericClass *gclass)
{
	return &gclass->context;
}

MonoGenericContext *
mono_class_get_context (MonoClass *klass)
{
       return klass->generic_class ? mono_generic_class_get_context (klass->generic_class) : NULL;
}

/*
 * mono_class_get_generic_container:
 *
 *   Return the generic container of KLASS which should be a generic type definition.
 */
MonoGenericContainer*
mono_class_get_generic_container (MonoClass *klass)
{
	g_assert (klass->is_generic);

	return klass->generic_container;
}

/*
 * mono_class_get_generic_class:
 *
 *   Return the MonoGenericClass of KLASS, which should be a generic instance.
 */
MonoGenericClass*
mono_class_get_generic_class (MonoClass *klass)
{
	g_assert (klass->is_inflated);

	return klass->generic_class;
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
	mono_error_init (error);

	if (context)
		inflated = inflate_generic_type (image, type, context, error);
	return_val_if_nok (error, NULL);

	if (!inflated) {
		MonoType *shared = mono_metadata_get_shared_type (type);

		if (shared) {
			return shared;
		} else {
			return mono_metadata_type_dup (image, type);
		}
	}

	mono_stats.inflated_type_count++;
	return inflated;
}

/*
 * mono_class_inflate_generic_type:
 * @type: a type
 * @context: a generics context
 *
 * If @type is a generic type and @context is not NULL, instantiate it using the 
 * generics context @context.
 *
 * Returns: The instantiated type or a copy of @type. The returned MonoType is allocated
 * on the heap and is owned by the caller. Returns NULL on error.
 *
 * @deprecated Please use mono_class_inflate_generic_type_checked instead
 */
MonoType*
mono_class_inflate_generic_type (MonoType *type, MonoGenericContext *context)
{
	MonoError error;
	MonoType *result;
	result = mono_class_inflate_generic_type_checked (type, context, &error);

	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		return NULL;
	}
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

	mono_error_init (error);
	if (context) {
		inflated = inflate_generic_type (image, type, context, error);
		return_val_if_nok (error, NULL);
	}

	if (!inflated)
		return type;

	mono_stats.inflated_type_count++;
	return inflated;
}

MonoClass*
mono_class_inflate_generic_class_checked (MonoClass *gklass, MonoGenericContext *context, MonoError *error)
{
	MonoClass *res;
	MonoType *inflated;

	inflated = mono_class_inflate_generic_type_checked (&gklass->byval_arg, context, error);
	return_val_if_nok (error, NULL);

	res = mono_class_from_mono_type (inflated);
	mono_metadata_free_type (inflated);

	return res;
}
/*
 * mono_class_inflate_generic_class:
 *
 *   Inflate the class GKLASS with CONTEXT.
 */
MonoClass*
mono_class_inflate_generic_class (MonoClass *gklass, MonoGenericContext *context)
{
	MonoError error;
	MonoClass *res;

	res = mono_class_inflate_generic_class_checked (gklass, context, &error);
	g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/

	return res;
}



static MonoGenericContext
inflate_generic_context (MonoGenericContext *context, MonoGenericContext *inflate_with, MonoError *error)
{
	MonoGenericInst *class_inst = NULL;
	MonoGenericInst *method_inst = NULL;
	MonoGenericContext res = { NULL, NULL };

	mono_error_init (error);

	if (context->class_inst) {
		class_inst = mono_metadata_inflate_generic_inst (context->class_inst, inflate_with, error);
		if (!mono_error_ok (error))
			goto fail;
	}

	if (context->method_inst) {
		method_inst = mono_metadata_inflate_generic_inst (context->method_inst, inflate_with, error);
		if (!mono_error_ok (error))
			goto fail;
	}

	res.class_inst = class_inst;
	res.method_inst = method_inst;
fail:
	return res;
}

/*
 * mono_class_inflate_generic_method:
 * @method: a generic method
 * @context: a generics context
 *
 * Instantiate the generic method @method using the generics context @context.
 *
 * Returns: The new instantiated method
 */
MonoMethod *
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericContext *context)
{
	return mono_class_inflate_generic_method_full (method, NULL, context);
}

MonoMethod *
mono_class_inflate_generic_method_checked (MonoMethod *method, MonoGenericContext *context, MonoError *error)
{
	return mono_class_inflate_generic_method_full_checked (method, NULL, context, error);
}

/**
 * mono_class_inflate_generic_method_full:
 *
 * Instantiate method @method with the generic context @context.
 * BEWARE: All non-trivial fields are invalid, including klass, signature, and header.
 *         Use mono_method_signature () and mono_method_get_header () to get the correct values.
 */
MonoMethod*
mono_class_inflate_generic_method_full (MonoMethod *method, MonoClass *klass_hint, MonoGenericContext *context)
{
	MonoError error;
	MonoMethod *res = mono_class_inflate_generic_method_full_checked (method, klass_hint, context, &error);
	if (!mono_error_ok (&error))
		/*FIXME do proper error handling - on this case, kill this function. */
		g_error ("Could not inflate generic method due to %s", mono_error_get_message (&error)); 

	return res;
}

/**
 * mono_class_inflate_generic_method_full_checked:
 * Same as mono_class_inflate_generic_method_full but return failure using @error.
 */
MonoMethod*
mono_class_inflate_generic_method_full_checked (MonoMethod *method, MonoClass *klass_hint, MonoGenericContext *context, MonoError *error)
{
	MonoMethod *result;
	MonoMethodInflated *iresult, *cached;
	MonoMethodSignature *sig;
	MonoGenericContext tmp_context;

	mono_error_init (error);

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
		(method->klass->generic_container && context->class_inst)))
		return method;

	iresult = g_new0 (MonoMethodInflated, 1);
	iresult->context = *context;
	iresult->declaring = method;

	if (!context->method_inst && method->is_generic)
		iresult->context.method_inst = mono_method_get_generic_container (method)->context.method_inst;

	if (!context->class_inst) {
		g_assert (!iresult->declaring->klass->generic_class);
		if (iresult->declaring->klass->generic_container)
			iresult->context.class_inst = iresult->declaring->klass->generic_container->context.class_inst;
		else if (iresult->declaring->klass->generic_class)
			iresult->context.class_inst = iresult->declaring->klass->generic_class->context.class_inst;
	}
	/* This can happen with some callers like mono_object_get_virtual_method () */
	if (!iresult->declaring->klass->generic_container && !iresult->declaring->klass->generic_class)
		iresult->context.class_inst = NULL;

	MonoImageSet *set = mono_metadata_get_image_set_for_method (iresult);

	// check cache
	mono_image_set_lock (set);
	cached = (MonoMethodInflated *)g_hash_table_lookup (set->gmethod_cache, iresult);
	mono_image_set_unlock (set);

	if (cached) {
		g_free (iresult);
		return (MonoMethod*)cached;
	}

	mono_stats.inflated_method_count++;

	inflated_methods_size += sizeof (MonoMethodInflated);

	sig = mono_method_signature (method);
	if (!sig) {
		char *name = mono_type_get_full_name (method->klass);
		mono_error_set_bad_image (error, method->klass->image, "Could not resolve signature of method %s:%s", name, method->name);
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

	if (!context->method_inst) {
		/* Set the generic_container of the result to the generic_container of method */
		MonoGenericContainer *generic_container = mono_method_get_generic_container (method);

		if (generic_container) {
			result->is_generic = 1;
			mono_method_set_generic_container (result, generic_container);
		}
	}

	if (!klass_hint || !klass_hint->generic_class ||
	    klass_hint->generic_class->container_class != method->klass ||
	    klass_hint->generic_class->context.class_inst != context->class_inst)
		klass_hint = NULL;

	if (method->klass->generic_container)
		result->klass = klass_hint;

	if (!result->klass) {
		MonoType *inflated = inflate_generic_type (NULL, &method->klass->byval_arg, context, error);
		if (!mono_error_ok (error)) 
			goto fail;

		result->klass = inflated ? mono_class_from_mono_type (inflated) : method->klass;
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
	mono_image_set_lock (set);
	cached = (MonoMethodInflated *)g_hash_table_lookup (set->gmethod_cache, iresult);
	if (!cached) {
		g_hash_table_insert (set->gmethod_cache, iresult, iresult);
		iresult->owner = set;
		cached = iresult;
	}
	mono_image_set_unlock (set);

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
	if (method->klass->generic_container)
		return &method->klass->generic_container->context;
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

	container = (MonoGenericContainer *)mono_image_property_lookup (method->klass->image, method, MONO_METHOD_PROP_GENERIC_CONTAINER);
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

	mono_image_property_insert (method->klass->image, method, MONO_METHOD_PROP_GENERIC_CONTAINER, container);
}

/** 
 * mono_class_find_enum_basetype:
 * @class: The enum class
 *
 *   Determine the basetype of an enum by iterating through its fields. We do this
 * in a separate function since it is cheaper than calling mono_class_setup_fields.
 */
static MonoType*
mono_class_find_enum_basetype (MonoClass *klass, MonoError *error)
{
	MonoGenericContainer *container = NULL;
	MonoImage *m = klass->image;
	const int top = klass->field.count;
	int i;

	g_assert (klass->enumtype);

	mono_error_init (error);

	if (klass->generic_container)
		container = klass->generic_container;
	else if (klass->generic_class) {
		MonoClass *gklass = klass->generic_class->container_class;

		container = gklass->generic_container;
		g_assert (container);
	}

	/*
	 * Fetch all the field information.
	 */
	for (i = 0; i < top; i++){
		const char *sig;
		guint32 cols [MONO_FIELD_SIZE];
		int idx = klass->field.first + i;
		MonoType *ftype;

		/* klass->field.first and idx points into the fieldptr table */
		mono_metadata_decode_table_row (m, MONO_TABLE_FIELD, idx, cols, MONO_FIELD_SIZE);

		if (cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_STATIC) //no need to decode static fields
			continue;

		if (!mono_verifier_verify_field_signature (klass->image, cols [MONO_FIELD_SIGNATURE], NULL)) {
			mono_error_set_bad_image (error, klass->image, "Invalid field signature %x", cols [MONO_FIELD_SIGNATURE]);
			goto fail;
		}

		sig = mono_metadata_blob_heap (m, cols [MONO_FIELD_SIGNATURE]);
		mono_metadata_decode_value (sig, &sig);
		/* FIELD signature == 0x06 */
		if (*sig != 0x06) {
			mono_error_set_bad_image (error, klass->image, "Invalid field signature %x, expected 0x6 but got %x", cols [MONO_FIELD_SIGNATURE], *sig);
			goto fail;
		}

		ftype = mono_metadata_parse_type_checked (m, container, cols [MONO_FIELD_FLAGS], FALSE, sig + 1, &sig, error);
		if (!ftype)
			goto fail;

		if (klass->generic_class) {
			//FIXME do we leak here?
			ftype = mono_class_inflate_generic_type_checked (ftype, mono_class_get_context (klass), error);
			if (!mono_error_ok (error))
				goto fail;
			ftype->attrs = cols [MONO_FIELD_FLAGS];
		}

		return ftype;
	}
	mono_error_set_type_load_class (error, klass, "Could not find base type");

fail:
	mono_loader_assert_no_error ();
	return NULL;
}

/*
 * Checks for MonoClass::exception_type without resolving all MonoType's into MonoClass'es
 */
static gboolean
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
		return mono_class_has_failure (mono_generic_class_get_class (type->data.generic_class));
	default:
		return FALSE;
	}
}

/*
 * mono_class_alloc:
 *
 *   Allocate memory for some data belonging to CLASS, either from its image's mempool,
 * or from the heap.
 */
static gpointer
mono_class_alloc (MonoClass *klass, int size)
{
	if (klass->generic_class)
		return mono_image_set_alloc (klass->generic_class->owner, size);
	else
		return mono_image_alloc (klass->image, size);
}

static gpointer
mono_class_alloc0 (MonoClass *klass, int size)
{
	gpointer res;

	res = mono_class_alloc (klass, size);
	memset (res, 0, size);
	return res;
}

#define mono_class_new0(klass,struct_type, n_structs)		\
    ((struct_type *) mono_class_alloc0 ((klass), ((gsize) sizeof (struct_type)) * ((gsize) (n_structs))))

/**
 * mono_class_setup_basic_field_info:
 * @class: The class to initialize
 *
 * Initializes the klass->fields.
 * LOCKING: Assumes the loader lock is held.
 */
static void
mono_class_setup_basic_field_info (MonoClass *klass)
{
	MonoClassField *field;
	MonoClass *gtd;
	MonoImage *image;
	int i, top;

	if (klass->fields)
		return;

	gtd = klass->generic_class ? mono_class_get_generic_type_definition (klass) : NULL;
	image = klass->image;
	top = klass->field.count;

	if (klass->generic_class && image_is_dynamic (klass->generic_class->container_class->image) && !klass->generic_class->container_class->wastypebuilder) {
		/*
		 * This happens when a generic instance of an unfinished generic typebuilder
		 * is used as an element type for creating an array type. We can't initialize
		 * the fields of this class using the fields of gklass, since gklass is not
		 * finished yet, fields could be added to it later.
		 */
		return;
	}

	if (gtd) {
		mono_class_setup_basic_field_info (gtd);

		top = gtd->field.count;
		klass->field.first = gtd->field.first;
		klass->field.count = gtd->field.count;
	}

	klass->fields = (MonoClassField *)mono_class_alloc0 (klass, sizeof (MonoClassField) * top);

	/*
	 * Fetch all the field information.
	 */
	for (i = 0; i < top; i++){
		field = &klass->fields [i];
		field->parent = klass;

		if (gtd) {
			field->name = mono_field_get_name (&gtd->fields [i]);
		} else {
			int idx = klass->field.first + i;
			/* klass->field.first and idx points into the fieldptr table */
			guint32 name_idx = mono_metadata_decode_table_row_col (image, MONO_TABLE_FIELD, idx, MONO_FIELD_NAME);
			/* The name is needed for fieldrefs */
			field->name = mono_metadata_string_heap (image, name_idx);
		}
	}
}

/** 
 * mono_class_setup_fields:
 * @class: The class to initialize
 *
 * Initializes the klass->fields.
 * LOCKING: Assumes the loader lock is held.
 */
static void
mono_class_setup_fields (MonoClass *klass)
{
	MonoError error;
	MonoImage *m = klass->image;
	int top;
	guint32 layout = klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	int i, blittable = TRUE;
	guint32 real_size = 0;
	guint32 packing_size = 0;
	gboolean explicit_size;
	MonoClassField *field;
	MonoGenericContainer *container = NULL;
	MonoClass *gtd = klass->generic_class ? mono_class_get_generic_type_definition (klass) : NULL;

	/*
	 * FIXME: We have a race condition here.  It's possible that this function returns
	 * to its caller with `instance_size` set to `0` instead of the actual size.  This
	 * is not a problem when the function is called recursively on the same class,
	 * because the size will be initialized by the outer invocation.  What follows is a
	 * description of how it can occur in other cases, too.  There it is a problem,
	 * because it can lead to the GC being asked to allocate an object of size `0`,
	 * which SGen chokes on.  The race condition is triggered infrequently by
	 * `tests/sgen-suspend.cs`.
	 *
	 * This function is called for a class whenever one of its subclasses is inited.
	 * For example, it's called for every subclass of Object.  What it does is this:
	 *
	 *     if (klass->setup_fields_called)
	 *         return;
	 *     ...
	 *     klass->instance_size = 0;
	 *     ...
	 *     klass->setup_fields_called = 1;
	 *     ... critical point
	 *     klass->instance_size = actual_instance_size;
	 *
	 * The last two steps are sometimes reversed, but that only changes the way in which
	 * the race condition works.
	 *
	 * Assume thread A goes through this function and makes it to the critical point.
	 * Now thread B runs the function and, since `setup_fields_called` is set, returns
	 * immediately, but `instance_size` is incorrect.
	 *
	 * The other case looks like this:
	 *
	 *     if (klass->setup_fields_called)
	 *         return;
	 *     ... critical point X
	 *     klass->instance_size = 0;
	 *     ... critical point Y
	 *     klass->instance_size = actual_instance_size;
	 *     ...
	 *     klass->setup_fields_called = 1;
	 *
	 * Assume thread A goes through the function and makes it to critical point X.  Now
	 * thread B runs through the whole of the function, returning, assuming
	 * `instance_size` is set.  At that point thread A gets to run and makes it to
	 * critical point Y, at which time `instance_size` is `0` again, invalidating thread
	 * B's assumption.
	 */
	if (klass->setup_fields_called)
		return;

	if (klass->generic_class && image_is_dynamic (klass->generic_class->container_class->image) && !klass->generic_class->container_class->wastypebuilder) {
		/*
		 * This happens when a generic instance of an unfinished generic typebuilder
		 * is used as an element type for creating an array type. We can't initialize
		 * the fields of this class using the fields of gklass, since gklass is not
		 * finished yet, fields could be added to it later.
		 */
		return;
	}

	mono_class_setup_basic_field_info (klass);
	top = klass->field.count;

	if (gtd) {
		mono_class_setup_fields (gtd);
		if (mono_class_has_failure (gtd)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			return;
		}
	}

	klass->instance_size = 0;
	if (!klass->rank)
		klass->sizes.class_size = 0;

	if (klass->parent) {
		/* For generic instances, klass->parent might not have been initialized */
		mono_class_init (klass->parent);
		if (!klass->parent->size_inited) {
			mono_class_setup_fields (klass->parent);
			if (mono_class_has_failure (klass->parent)) {
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
				return;
			}
		}
		klass->instance_size += klass->parent->instance_size;
		klass->min_align = klass->parent->min_align;
		/* we use |= since it may have been set already */
		klass->has_references |= klass->parent->has_references;
		blittable = klass->parent->blittable;
	} else {
		klass->instance_size = sizeof (MonoObject);
		klass->min_align = 1;
	}

	/* We can't really enable 16 bytes alignment until the GC supports it.
	The whole layout/instance size code must be reviewed because we do alignment calculation in terms of the
	boxed instance, which leads to unexplainable holes at the beginning of an object embedding a simd type.
	Bug #506144 is an example of this issue.

	 if (klass->simd_type)
		klass->min_align = 16;
	 */
	/* Get the real size */
	explicit_size = mono_metadata_packing_from_typedef (klass->image, klass->type_token, &packing_size, &real_size);

	if (explicit_size) {
		if ((packing_size & 0xffffff00) != 0) {
			char *err_msg = g_strdup_printf ("Could not load struct '%s' with packing size %d >= 256", klass->name, packing_size);
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, err_msg);
			return;
		}
		klass->packing_size = packing_size;
		real_size += klass->instance_size;
	}

	if (!top) {
		if (explicit_size && real_size) {
			klass->instance_size = MAX (real_size, klass->instance_size);
		}
		klass->blittable = blittable;
		mono_memory_barrier ();
		klass->size_inited = 1;
		klass->fields_inited = 1;
		klass->setup_fields_called = 1;
		return;
	}

	if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT && !(mono_is_corlib_image (klass->image) && !strcmp (klass->name_space, "System") && !strcmp (klass->name, "ValueType")))
		blittable = FALSE;

	/* Prevent infinite loops if the class references itself */
	klass->setup_fields_called = 1;

	if (klass->generic_container) {
		container = klass->generic_container;
	} else if (gtd) {
		container = gtd->generic_container;
		g_assert (container);
	}

	/*
	 * Fetch all the field information.
	 */
	for (i = 0; i < top; i++){
		int idx = klass->field.first + i;
		field = &klass->fields [i];

		field->parent = klass;

		if (!field->type) {
			mono_field_resolve_type (field, &error);
			if (!mono_error_ok (&error)) {
				/*mono_field_resolve_type already failed class*/
				mono_error_cleanup (&error);
				return;
			}
			if (!field->type)
				g_error ("could not resolve %s:%s\n", mono_type_get_full_name(klass), field->name);
			g_assert (field->type);
		}

		if (mono_field_is_deleted (field))
			continue;
		if (gtd) {
			MonoClassField *gfield = &gtd->fields [i];
			field->offset = gfield->offset;
		} else {
			if (layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
				guint32 offset;
				mono_metadata_field_info (m, idx, &offset, NULL, NULL);
				field->offset = offset;

				if (field->offset == (guint32)-1 && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
					mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Missing field layout info for %s", field->name));
					break;
				}
				if (field->offset < -1) { /*-1 is used to encode special static fields */
					mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Invalid negative field offset %d for %s", field->offset, field->name));
					break;
				}
				if (klass->generic_container) {
					mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Generic class cannot have explicit layout."));
					break;
				}
			}
		}

		/* Only do these checks if we still think this type is blittable */
		if (blittable && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			if (field->type->byref || MONO_TYPE_IS_REFERENCE (field->type)) {
				blittable = FALSE;
			} else {
				MonoClass *field_class = mono_class_from_mono_type (field->type);
				if (field_class) {
					mono_class_setup_fields (field_class);
					if (mono_class_has_failure (field_class)) {
						mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
						break;
					}
				}
				if (!field_class || !field_class->blittable)
					blittable = FALSE;
			}
		}

		if (klass->enumtype && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			klass->cast_class = klass->element_class = mono_class_from_mono_type (field->type);
			blittable = klass->element_class->blittable;
		}

		if (mono_type_has_exceptions (field->type)) {
			char *class_name = mono_type_get_full_name (klass);
			char *type_name = mono_type_full_name (field->type);

			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			g_warning ("Invalid type %s for instance field %s:%s", type_name, class_name, field->name);
			g_free (class_name);
			g_free (type_name);
			break;
		}
		/* The def_value of fields is compute lazily during vtable creation */
	}

	if (klass == mono_defaults.string_class)
		blittable = FALSE;

	klass->blittable = blittable;

	if (klass->enumtype && !mono_class_enum_basetype (klass)) {
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
		return;
	}
	if (explicit_size && real_size) {
		klass->instance_size = MAX (real_size, klass->instance_size);
	}

	if (mono_class_has_failure (klass))
		return;
	mono_class_layout_fields (klass);

	/*valuetypes can't be neither bigger than 1Mb or empty. */
	if (klass->valuetype && (klass->instance_size <= 0 || klass->instance_size > (0x100000 + sizeof (MonoObject))))
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);

	mono_memory_barrier ();
	klass->fields_inited = 1;
}

/** 
 * mono_class_setup_fields_locking:
 * @class: The class to initialize
 *
 * Initializes the klass->fields array of fields.
 * Aquires the loader lock.
 */
void
mono_class_setup_fields_locking (MonoClass *klass)
{
	/* This can be checked without locks */
	if (klass->fields_inited)
		return;
	mono_loader_lock ();
	mono_class_setup_fields (klass);
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
	if (!type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) &&
		(!type->data.generic_param->gshared_constraint || type->data.generic_param->gshared_constraint->type == MONO_TYPE_OBJECT))
		return &mono_defaults.object_class->byval_arg;
	return type;
}

static gboolean
type_has_references (MonoClass *klass, MonoType *ftype)
{
	if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (klass, ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype)))))
		return TRUE;
	if (!ftype->byref && (ftype->type == MONO_TYPE_VAR || ftype->type == MONO_TYPE_MVAR)) {
		MonoGenericParam *gparam = ftype->data.generic_param;

		if (gparam->gshared_constraint)
			return mono_class_has_references (mono_class_from_mono_type (gparam->gshared_constraint));
	}
	return FALSE;
}

/*
 * mono_class_layout_fields:
 * @class: a class
 *
 * Compute the placement of fields inside an object or struct, according to
 * the layout rules and set the following fields in @class:
 *  - has_references (if the class contains instance references firled or structs that contain references)
 *  - has_static_refs (same, but for static fields)
 *  - instance_size (size of the object in memory)
 *  - class_size (size needed for the static fields)
 *  - size_inited (flag set when the instance_size is set)
 *
 * LOCKING: this is supposed to be called with the loader lock held.
 */
void
mono_class_layout_fields (MonoClass *klass)
{
	int i;
	const int top = klass->field.count;
	guint32 layout = klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	guint32 pass, passes, real_size;
	gboolean gc_aware_layout = FALSE;
	gboolean has_static_fields = FALSE;
	MonoClassField *field;

	/*
	 * When we do generic sharing we need to have layout
	 * information for open generic classes (either with a generic
	 * context containing type variables or with a generic
	 * container), so we don't return in that case anymore.
	 */

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
		if (!klass->valuetype)
			gc_aware_layout = TRUE;
	}

	/* Compute klass->has_references */
	/* 
	 * Process non-static fields first, since static fields might recursively
	 * refer to the class itself.
	 */
	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &klass->fields [i];

		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (type_has_references (klass, ftype))
				klass->has_references = TRUE;
		}
	}

	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &klass->fields [i];

		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (type_has_references (klass, ftype))
				klass->has_static_refs = TRUE;
		}
	}

	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &klass->fields [i];

		ftype = mono_type_get_underlying_type (field->type);
		ftype = mono_type_get_basic_type_from_generic (ftype);
		if (type_has_references (klass, ftype)) {
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				klass->has_static_refs = TRUE;
			else
				klass->has_references = TRUE;
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

		if (klass->parent) {
			mono_class_setup_fields (klass->parent);
			if (mono_class_has_failure (klass->parent)) {
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
				return;
			}
			real_size = klass->parent->instance_size;
		} else {
			real_size = sizeof (MonoObject);
		}

		for (pass = 0; pass < passes; ++pass) {
			for (i = 0; i < top; i++){
				gint32 align;
				guint32 size;
				MonoType *ftype;

				field = &klass->fields [i];

				if (mono_field_is_deleted (field))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;

				ftype = mono_type_get_underlying_type (field->type);
				ftype = mono_type_get_basic_type_from_generic (ftype);
				if (gc_aware_layout) {
					if (type_has_references (klass, ftype)) {
						if (pass == 1)
							continue;
					} else {
						if (pass == 0)
							continue;
					}
				}

				if ((top == 1) && (klass->instance_size == sizeof (MonoObject)) &&
					(strcmp (mono_field_get_name (field), "$PRIVATE$") == 0)) {
					/* This field is a hack inserted by MCS to empty structures */
					continue;
				}

				size = mono_type_size (field->type, &align);
			
				/* FIXME (LAMESPEC): should we also change the min alignment according to pack? */
				align = klass->packing_size ? MIN (klass->packing_size, align): align;
				/* if the field has managed references, we need to force-align it
				 * see bug #77788
				 */
				if (type_has_references (klass, ftype))
					align = MAX (align, sizeof (gpointer));

				klass->min_align = MAX (align, klass->min_align);
				field->offset = real_size;
				if (align) {
					field->offset += align - 1;
					field->offset &= ~(align - 1);
				}
				/*TypeBuilders produce all sort of weird things*/
				g_assert (image_is_dynamic (klass->image) || field->offset > 0);
				real_size = field->offset + size;
			}

			klass->instance_size = MAX (real_size, klass->instance_size);
       
			if (klass->instance_size & (klass->min_align - 1)) {
				klass->instance_size += klass->min_align - 1;
				klass->instance_size &= ~(klass->min_align - 1);
			}
		}
		break;
	case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT: {
		guint8 *ref_bitmap;

		real_size = 0;
		for (i = 0; i < top; i++) {
			gint32 align;
			guint32 size;
			MonoType *ftype;

			field = &klass->fields [i];

			/*
			 * There must be info about all the fields in a type if it
			 * uses explicit layout.
			 */
			if (mono_field_is_deleted (field))
				continue;
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			size = mono_type_size (field->type, &align);
			align = klass->packing_size ? MIN (klass->packing_size, align): align;
			klass->min_align = MAX (align, klass->min_align);

			/*
			 * When we get here, field->offset is already set by the
			 * loader (for either runtime fields or fields loaded from metadata).
			 * The offset is from the start of the object: this works for both
			 * classes and valuetypes.
			 */
			field->offset += sizeof (MonoObject);
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (type_has_references (klass, ftype)) {
				if (field->offset % sizeof (gpointer)) {
					mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
				}
			}

			/*
			 * Calc max size.
			 */
			real_size = MAX (real_size, size + field->offset);
		}

		if (klass->has_references) {
			ref_bitmap = g_new0 (guint8, real_size / sizeof (gpointer));

			/* Check for overlapping reference and non-reference fields */
			for (i = 0; i < top; i++) {
				MonoType *ftype;

				field = &klass->fields [i];

				if (mono_field_is_deleted (field))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;
				ftype = mono_type_get_underlying_type (field->type);
				if (MONO_TYPE_IS_REFERENCE (ftype))
					ref_bitmap [field->offset / sizeof (gpointer)] = 1;
			}
			for (i = 0; i < top; i++) {
				field = &klass->fields [i];

				if (mono_field_is_deleted (field))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;

				// FIXME: Too much code does this
#if 0
				if (!MONO_TYPE_IS_REFERENCE (field->type) && ref_bitmap [field->offset / sizeof (gpointer)]) {
					char *err_msg = g_strdup_printf ("Could not load type '%s' because it contains an object field at offset %d that is incorrectly aligned or overlapped by a non-object field.", klass->name, field->offset);
					mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, err_msg);
				}
#endif
			}
			g_free (ref_bitmap);
		}

		klass->instance_size = MAX (real_size, klass->instance_size);
		if (klass->instance_size & (klass->min_align - 1)) {
			klass->instance_size += klass->min_align - 1;
			klass->instance_size &= ~(klass->min_align - 1);
		}
		break;
	}
	}

	if (layout != TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
		/*
		 * This leads to all kinds of problems with nested structs, so only
		 * enable it when a MONO_DEBUG property is set.
		 *
		 * For small structs, set min_align to at least the struct size to improve
		 * performance, and since the JIT memset/memcpy code assumes this and generates 
		 * unaligned accesses otherwise. See #78990 for a testcase.
		 */
		if (mono_align_small_structs) {
			if (klass->instance_size <= sizeof (MonoObject) + sizeof (gpointer))
				klass->min_align = MAX (klass->min_align, klass->instance_size - sizeof (MonoObject));
		}
	}

	mono_memory_barrier ();
	klass->size_inited = 1;

	/*
	 * Compute static field layout and size
	 */
	for (i = 0; i < top; i++){
		gint32 align;
		guint32 size;

		field = &klass->fields [i];
			
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC) || field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
			continue;
		if (mono_field_is_deleted (field))
			continue;

		if (mono_type_has_exceptions (field->type)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			break;
		}

		has_static_fields = TRUE;

		size = mono_type_size (field->type, &align);
		field->offset = klass->sizes.class_size;
		/*align is always non-zero here*/
		field->offset += align - 1;
		field->offset &= ~(align - 1);
		klass->sizes.class_size = field->offset + size;
	}

	if (has_static_fields && klass->sizes.class_size == 0)
		/* Simplify code which depends on class_size != 0 if the class has static fields */
		klass->sizes.class_size = 8;
}

static MonoMethod*
create_array_method (MonoClass *klass, const char *name, MonoMethodSignature *sig)
{
	MonoMethod *method;

	method = (MonoMethod *) mono_image_alloc0 (klass->image, sizeof (MonoMethodPInvoke));
	method->klass = klass;
	method->flags = METHOD_ATTRIBUTE_PUBLIC;
	method->iflags = METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;
	method->signature = sig;
	method->name = name;
	method->slot = -1;
	/* .ctor */
	if (name [0] == '.') {
		method->flags |= METHOD_ATTRIBUTE_RT_SPECIAL_NAME | METHOD_ATTRIBUTE_SPECIAL_NAME;
	} else {
		method->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
	}
	return method;
}

/*
 * mono_class_setup_methods:
 * @class: a class
 *
 *   Initializes the 'methods' array in CLASS.
 * Calling this method should be avoided if possible since it allocates a lot 
 * of long-living MonoMethod structures.
 * Methods belonging to an interface are assigned a sequential slot starting
 * from 0.
 *
 * On failure this function sets klass->exception_type
 */
void
mono_class_setup_methods (MonoClass *klass)
{
	int i, count;
	MonoMethod **methods;

	if (klass->methods)
		return;

	if (klass->generic_class) {
		MonoError error;
		MonoClass *gklass = klass->generic_class->container_class;

		mono_class_init (gklass);
		if (!mono_class_has_failure (gklass))
			mono_class_setup_methods (gklass);
		if (mono_class_has_failure (gklass)) {
			/* FIXME make exception_data less opaque so it's possible to dup it here */
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Generic type definition failed to load"));
			return;
		}

		/* The + 1 makes this always non-NULL to pass the check in mono_class_setup_methods () */
		count = gklass->method.count;
		methods = (MonoMethod **)mono_class_alloc0 (klass, sizeof (MonoMethod*) * (count + 1));

		for (i = 0; i < count; i++) {
			methods [i] = mono_class_inflate_generic_method_full_checked (
				gklass->methods [i], klass, mono_class_get_context (klass), &error);
			if (!mono_error_ok (&error)) {
				char *method = mono_method_full_name (gklass->methods [i], TRUE);
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Could not inflate method %s due to %s", method, mono_error_get_message (&error)));

				g_free (method);
				mono_error_cleanup (&error);
				return;				
			}
		}
	} else if (klass->rank) {
		MonoError error;
		MonoMethod *amethod;
		MonoMethodSignature *sig;
		int count_generic = 0, first_generic = 0;
		int method_num = 0;
		gboolean jagged_ctor = FALSE;

		count = 3 + (klass->rank > 1? 2: 1);

		mono_class_setup_interfaces (klass, &error);
		g_assert (mono_error_ok (&error)); /*FIXME can this fail for array types?*/

		if (klass->rank == 1 && klass->element_class->rank) {
			jagged_ctor = TRUE;
			klass->method.count ++;
		}

		if (klass->interface_count) {
			count_generic = generic_array_methods (klass);
			first_generic = count;
			count += klass->interface_count * count_generic;
		}

		methods = (MonoMethod **)mono_class_alloc0 (klass, sizeof (MonoMethod*) * count);

		sig = mono_metadata_signature_alloc (klass->image, klass->rank);
		sig->ret = &mono_defaults.void_class->byval_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;

		amethod = create_array_method (klass, ".ctor", sig);
		methods [method_num++] = amethod;
		if (klass->rank > 1) {
			sig = mono_metadata_signature_alloc (klass->image, klass->rank * 2);
			sig->ret = &mono_defaults.void_class->byval_arg;
			sig->pinvoke = TRUE;
			sig->hasthis = TRUE;
			for (i = 0; i < klass->rank * 2; ++i)
				sig->params [i] = &mono_defaults.int32_class->byval_arg;

			amethod = create_array_method (klass, ".ctor", sig);
			methods [method_num++] = amethod;
		}

		if (jagged_ctor) {
			/* Jagged arrays have an extra ctor in .net which creates an array of arrays */
			sig = mono_metadata_signature_alloc (klass->image, klass->rank + 1);
			sig->ret = &mono_defaults.void_class->byval_arg;
			sig->pinvoke = TRUE;
			sig->hasthis = TRUE;
			for (i = 0; i < klass->rank + 1; ++i)
				sig->params [i] = &mono_defaults.int32_class->byval_arg;
			amethod = create_array_method (klass, ".ctor", sig);
			methods [method_num++] = amethod;
		}

		/* element Get (idx11, [idx2, ...]) */
		sig = mono_metadata_signature_alloc (klass->image, klass->rank);
		sig->ret = &klass->element_class->byval_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;
		amethod = create_array_method (klass, "Get", sig);
		methods [method_num++] = amethod;
		/* element& Address (idx11, [idx2, ...]) */
		sig = mono_metadata_signature_alloc (klass->image, klass->rank);
		sig->ret = &klass->element_class->this_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;
		amethod = create_array_method (klass, "Address", sig);
		methods [method_num++] = amethod;
		/* void Set (idx11, [idx2, ...], element) */
		sig = mono_metadata_signature_alloc (klass->image, klass->rank + 1);
		sig->ret = &mono_defaults.void_class->byval_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;
		sig->params [i] = &klass->element_class->byval_arg;
		amethod = create_array_method (klass, "Set", sig);
		methods [method_num++] = amethod;

		for (i = 0; i < klass->interface_count; i++)
			setup_generic_array_ifaces (klass, klass->interfaces [i], methods, first_generic + i * count_generic);
	} else {
		MonoError error;

		count = klass->method.count;
		methods = (MonoMethod **)mono_class_alloc (klass, sizeof (MonoMethod*) * count);
		for (i = 0; i < count; ++i) {
			int idx = mono_metadata_translate_token_index (klass->image, MONO_TABLE_METHOD, klass->method.first + i + 1);
			methods [i] = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | idx, klass, NULL, &error);
			if (!methods [i]) {
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Could not load method %d due to %s", i, mono_error_get_message (&error)));
				mono_error_cleanup (&error);
			}
		}
	}

	if (MONO_CLASS_IS_INTERFACE (klass)) {
		int slot = 0;
		/*Only assign slots to virtual methods as interfaces are allowed to have static methods.*/
		for (i = 0; i < count; ++i) {
			if (methods [i]->flags & METHOD_ATTRIBUTE_VIRTUAL)
				methods [i]->slot = slot++;
		}
	}

	mono_image_lock (klass->image);

	if (!klass->methods) {
		klass->method.count = count;

		/* Needed because of the double-checking locking pattern */
		mono_memory_barrier ();

		klass->methods = methods;
	}

	mono_image_unlock (klass->image);
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
	MonoError error;
	/* Avoid calling setup_methods () if possible */
	if (klass->generic_class && !klass->methods) {
		MonoClass *gklass = klass->generic_class->container_class;
		MonoMethod *m;

		m = mono_class_inflate_generic_method_full_checked (
				gklass->methods [index], klass, mono_class_get_context (klass), &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
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
		g_assert (index >= 0 && index < klass->method.count);
		return klass->methods [index];
	}
}	

/*
 * mono_class_get_inflated_method:
 *
 *   Given an inflated class CLASS and a method METHOD which should be a method of
 * CLASS's generic definition, return the inflated method corresponding to METHOD.
 */
MonoMethod*
mono_class_get_inflated_method (MonoClass *klass, MonoMethod *method)
{
	MonoClass *gklass = klass->generic_class->container_class;
	int i;

	g_assert (method->klass == gklass);

	mono_class_setup_methods (gklass);
	g_assert (!mono_class_has_failure (gklass)); /*FIXME do proper error handling*/

	for (i = 0; i < gklass->method.count; ++i) {
		if (gklass->methods [i] == method) {
			if (klass->methods) {
				return klass->methods [i];
			} else {
				MonoError error;
				MonoMethod *result = mono_class_inflate_generic_method_full_checked (gklass->methods [i], klass, mono_class_get_context (klass), &error);
				g_assert (mono_error_ok (&error)); /* FIXME don't swallow this error */
				return result;
			}
		}
	}

	return NULL;
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

	if (klass->rank == 1) {
		/* 
		 * szarrays do not overwrite any methods of Array, so we can avoid
		 * initializing their vtables in some cases.
		 */
		mono_class_setup_vtable (klass->parent);
		if (offset < klass->parent->vtable_size)
			return klass->parent->vtable [offset];
	}

	if (klass->generic_class) {
		MonoError error;
		MonoClass *gklass = klass->generic_class->container_class;
		mono_class_setup_vtable (gklass);
		m = gklass->vtable [offset];

		m = mono_class_inflate_generic_method_full_checked (m, klass, mono_class_get_context (klass), &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow this error */
	} else {
		mono_class_setup_vtable (klass);
		if (mono_class_has_failure (klass))
			return NULL;
		m = klass->vtable [offset];
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

	return klass->vtable_size;
}

/*
 * mono_class_setup_properties:
 *
 *   Initialize klass->ext.property and klass->ext.properties.
 *
 * This method can fail the class.
 */
static void
mono_class_setup_properties (MonoClass *klass)
{
	guint startm, endm, i, j;
	guint32 cols [MONO_PROPERTY_SIZE];
	MonoTableInfo *msemt = &klass->image->tables [MONO_TABLE_METHODSEMANTICS];
	MonoProperty *properties;
	guint32 last;
	int first, count;

	if (klass->ext && klass->ext->properties)
		return;

	if (klass->generic_class) {
		MonoClass *gklass = klass->generic_class->container_class;

		mono_class_init (gklass);
		mono_class_setup_properties (gklass);
		if (mono_class_has_failure (gklass)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Generic type definition failed to load"));
			return;
		}

		properties = mono_class_new0 (klass, MonoProperty, gklass->ext->property.count + 1);

		for (i = 0; i < gklass->ext->property.count; i++) {
			MonoError error;
			MonoProperty *prop = &properties [i];

			*prop = gklass->ext->properties [i];

			if (prop->get)
				prop->get = mono_class_inflate_generic_method_full_checked (
					prop->get, klass, mono_class_get_context (klass), &error);
			if (prop->set)
				prop->set = mono_class_inflate_generic_method_full_checked (
					prop->set, klass, mono_class_get_context (klass), &error);

			g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/
			prop->parent = klass;
		}

		first = gklass->ext->property.first;
		count = gklass->ext->property.count;
	} else {
		first = mono_metadata_properties_from_typedef (klass->image, mono_metadata_token_index (klass->type_token) - 1, &last);
		count = last - first;

		if (count) {
			mono_class_setup_methods (klass);
			if (mono_class_has_failure (klass))
				return;
		}

		properties = (MonoProperty *)mono_class_alloc0 (klass, sizeof (MonoProperty) * count);
		for (i = first; i < last; ++i) {
			mono_metadata_decode_table_row (klass->image, MONO_TABLE_PROPERTY, i, cols, MONO_PROPERTY_SIZE);
			properties [i - first].parent = klass;
			properties [i - first].attrs = cols [MONO_PROPERTY_FLAGS];
			properties [i - first].name = mono_metadata_string_heap (klass->image, cols [MONO_PROPERTY_NAME]);

			startm = mono_metadata_methods_from_property (klass->image, i, &endm);
			for (j = startm; j < endm; ++j) {
				MonoMethod *method;

				mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);

				if (klass->image->uncompressed_metadata) {
					MonoError error;
					/* It seems like the MONO_METHOD_SEMA_METHOD column needs no remapping */
					method = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | cols [MONO_METHOD_SEMA_METHOD], klass, NULL, &error);
					mono_error_cleanup (&error); /* FIXME don't swallow this error */
				} else {
					method = klass->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - klass->method.first];
				}

				switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
				case METHOD_SEMANTIC_SETTER:
					properties [i - first].set = method;
					break;
				case METHOD_SEMANTIC_GETTER:
					properties [i - first].get = method;
					break;
				default:
					break;
				}
			}
		}
	}

	mono_class_alloc_ext (klass);

	mono_image_lock (klass->image);

	if (klass->ext->properties) {
		/* We leak 'properties' which was allocated from the image mempool */
		mono_image_unlock (klass->image);
		return;
	}

	klass->ext->property.first = first;
	klass->ext->property.count = count;

	/* Flush any pending writes as we do double checked locking on klass->ext->properties */
	mono_memory_barrier ();

	/* Leave this assignment as the last op in the function */
	klass->ext->properties = properties;

	mono_image_unlock (klass->image);
}

static MonoMethod**
inflate_method_listz (MonoMethod **methods, MonoClass *klass, MonoGenericContext *context)
{
	MonoMethod **om, **retval;
	int count;

	for (om = methods, count = 0; *om; ++om, ++count)
		;

	retval = g_new0 (MonoMethod*, count + 1);
	count = 0;
	for (om = methods, count = 0; *om; ++om, ++count) {
		MonoError error;
		retval [count] = mono_class_inflate_generic_method_full_checked (*om, klass, context, &error);
		g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/
	}

	return retval;
}

/*This method can fail the class.*/
static void
mono_class_setup_events (MonoClass *klass)
{
	int first, count;
	guint startm, endm, i, j;
	guint32 cols [MONO_EVENT_SIZE];
	MonoTableInfo *msemt = &klass->image->tables [MONO_TABLE_METHODSEMANTICS];
	guint32 last;
	MonoEvent *events;

	if (klass->ext && klass->ext->events)
		return;

	if (klass->generic_class) {
		MonoClass *gklass = klass->generic_class->container_class;
		MonoGenericContext *context = NULL;

		mono_class_setup_events (gklass);
		if (mono_class_has_failure (gklass)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Generic type definition failed to load"));
			return;
		}

		first = gklass->ext->event.first;
		count = gklass->ext->event.count;

		events = mono_class_new0 (klass, MonoEvent, count);

		if (count)
			context = mono_class_get_context (klass);

		for (i = 0; i < count; i++) {
			MonoError error;
			MonoEvent *event = &events [i];
			MonoEvent *gevent = &gklass->ext->events [i];

			mono_error_init (&error); //since we do conditional calls, we must ensure the default value is ok

			event->parent = klass;
			event->name = gevent->name;
			event->add = gevent->add ? mono_class_inflate_generic_method_full_checked (gevent->add, klass, context, &error) : NULL;
			g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/
			event->remove = gevent->remove ? mono_class_inflate_generic_method_full_checked (gevent->remove, klass, context, &error) : NULL;
			g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/
			event->raise = gevent->raise ? mono_class_inflate_generic_method_full_checked (gevent->raise, klass, context, &error) : NULL;
			g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/

#ifndef MONO_SMALL_CONFIG
			event->other = gevent->other ? inflate_method_listz (gevent->other, klass, context) : NULL;
#endif
			event->attrs = gevent->attrs;
		}
	} else {
		first = mono_metadata_events_from_typedef (klass->image, mono_metadata_token_index (klass->type_token) - 1, &last);
		count = last - first;

		if (count) {
			mono_class_setup_methods (klass);
			if (mono_class_has_failure (klass)) {
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Generic type definition failed to load"));
				return;
			}
		}

		events = (MonoEvent *)mono_class_alloc0 (klass, sizeof (MonoEvent) * count);
		for (i = first; i < last; ++i) {
			MonoEvent *event = &events [i - first];

			mono_metadata_decode_table_row (klass->image, MONO_TABLE_EVENT, i, cols, MONO_EVENT_SIZE);
			event->parent = klass;
			event->attrs = cols [MONO_EVENT_FLAGS];
			event->name = mono_metadata_string_heap (klass->image, cols [MONO_EVENT_NAME]);

			startm = mono_metadata_methods_from_event (klass->image, i, &endm);
			for (j = startm; j < endm; ++j) {
				MonoMethod *method;

				mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);

				if (klass->image->uncompressed_metadata) {
					MonoError error;
					/* It seems like the MONO_METHOD_SEMA_METHOD column needs no remapping */
					method = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | cols [MONO_METHOD_SEMA_METHOD], klass, NULL, &error);
					mono_error_cleanup (&error); /* FIXME don't swallow this error */
				} else {
					method = klass->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - klass->method.first];
				}

				switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
				case METHOD_SEMANTIC_ADD_ON:
					event->add = method;
					break;
				case METHOD_SEMANTIC_REMOVE_ON:
					event->remove = method;
					break;
				case METHOD_SEMANTIC_FIRE:
					event->raise = method;
					break;
				case METHOD_SEMANTIC_OTHER: {
#ifndef MONO_SMALL_CONFIG
					int n = 0;

					if (event->other == NULL) {
						event->other = g_new0 (MonoMethod*, 2);
					} else {
						while (event->other [n])
							n++;
						event->other = (MonoMethod **)g_realloc (event->other, (n + 2) * sizeof (MonoMethod*));
					}
					event->other [n] = method;
					/* NULL terminated */
					event->other [n + 1] = NULL;
#endif
					break;
				}
				default:
					break;
				}
			}
		}
	}

	mono_class_alloc_ext (klass);

	mono_image_lock (klass->image);

	if (klass->ext->events) {
		mono_image_unlock (klass->image);
		return;
	}

	klass->ext->event.first = first;
	klass->ext->event.count = count;

	/* Flush any pending writes as we do double checked locking on klass->ext.events */
	mono_memory_barrier ();

	/* Leave this assignment as the last op in the function */
	klass->ext->events = events;

	mono_image_unlock (klass->image);
}

/*
 * Global pool of interface IDs, represented as a bitset.
 * LOCKING: Protected by the classes lock.
 */
static MonoBitSet *global_interface_bitset = NULL;

/*
 * mono_unload_interface_ids:
 * @bitset: bit set of interface IDs
 *
 * When an image is unloaded, the interface IDs associated with
 * the image are put back in the global pool of IDs so the numbers
 * can be reused.
 */
void
mono_unload_interface_ids (MonoBitSet *bitset)
{
	classes_lock ();
	mono_bitset_sub (global_interface_bitset, bitset);
	classes_unlock ();
}

void
mono_unload_interface_id (MonoClass *klass)
{
	if (global_interface_bitset && klass->interface_id) {
		classes_lock ();
		mono_bitset_clear (global_interface_bitset, klass->interface_id);
		classes_unlock ();
	}
}

/**
 * mono_get_unique_iid:
 * @class: interface
 *
 * Assign a unique integer ID to the interface represented by @class.
 * The ID will positive and as small as possible.
 * LOCKING: Acquires the classes lock.
 * Returns: The new ID.
 */
static guint
mono_get_unique_iid (MonoClass *klass)
{
	int iid;
	
	g_assert (MONO_CLASS_IS_INTERFACE (klass));

	classes_lock ();

	if (!global_interface_bitset) {
		global_interface_bitset = mono_bitset_new (128, 0);
	}

	iid = mono_bitset_find_first_unset (global_interface_bitset, -1);
	if (iid < 0) {
		int old_size = mono_bitset_size (global_interface_bitset);
		MonoBitSet *new_set = mono_bitset_clone (global_interface_bitset, old_size * 2);
		mono_bitset_free (global_interface_bitset);
		global_interface_bitset = new_set;
		iid = old_size;
	}
	mono_bitset_set (global_interface_bitset, iid);
	/* set the bit also in the per-image set */
	if (!klass->generic_class) {
		if (klass->image->interface_bitset) {
			if (iid >= mono_bitset_size (klass->image->interface_bitset)) {
				MonoBitSet *new_set = mono_bitset_clone (klass->image->interface_bitset, iid + 1);
				mono_bitset_free (klass->image->interface_bitset);
				klass->image->interface_bitset = new_set;
			}
		} else {
			klass->image->interface_bitset = mono_bitset_new (iid + 1, 0);
		}
		mono_bitset_set (klass->image->interface_bitset, iid);
	}

	classes_unlock ();

#ifndef MONO_SMALL_CONFIG
	if (mono_print_vtable) {
		int generic_id;
		char *type_name = mono_type_full_name (&klass->byval_arg);
		if (klass->generic_class && !klass->generic_class->context.class_inst->is_open) {
			generic_id = klass->generic_class->context.class_inst->id;
			g_assert (generic_id != 0);
		} else {
			generic_id = 0;
		}
		printf ("Interface: assigned id %d to %s|%s|%d\n", iid, klass->image->name, type_name, generic_id);
		g_free (type_name);
	}
#endif

	g_assert (iid <= 65535);
	return iid;
}

static void
collect_implemented_interfaces_aux (MonoClass *klass, GPtrArray **res, MonoError *error)
{
	int i;
	MonoClass *ic;

	mono_class_setup_interfaces (klass, error);
	return_if_nok (error);

	for (i = 0; i < klass->interface_count; i++) {
		ic = klass->interfaces [i];

		if (*res == NULL)
			*res = g_ptr_array_new ();
		g_ptr_array_add (*res, ic);
		mono_class_init (ic);
		if (mono_class_has_failure (ic)) {
			mono_error_set_type_load_class (error, ic, "Error Loading class");
			return;
		}

		collect_implemented_interfaces_aux (ic, res, error);
		return_if_nok (error);
	}
}

GPtrArray*
mono_class_get_implemented_interfaces (MonoClass *klass, MonoError *error)
{
	GPtrArray *res = NULL;

	collect_implemented_interfaces_aux (klass, &res, error);
	if (!mono_error_ok (error)) {
		if (res)
			g_ptr_array_free (res, TRUE);
		return NULL;
	}
	return res;
}

static int
compare_interface_ids (const void *p_key, const void *p_element) {
	const MonoClass *key = (const MonoClass *)p_key;
	const MonoClass *element = *(const MonoClass **)p_element;
	
	return (key->interface_id - element->interface_id);
}

/*FIXME verify all callers if they should switch to mono_class_interface_offset_with_variance*/
int
mono_class_interface_offset (MonoClass *klass, MonoClass *itf) {
	MonoClass **result = (MonoClass **)mono_binary_search (
			itf,
			klass->interfaces_packed,
			klass->interface_offsets_count,
			sizeof (MonoClass *),
			compare_interface_ids);
	if (result) {
		return klass->interface_offsets_packed [result - (klass->interfaces_packed)];
	} else {
		return -1;
	}
}

/**
 * mono_class_interface_offset_with_variance:
 * 
 * Return the interface offset of @itf in @klass. Sets @non_exact_match to TRUE if the match required variance check
 * If @itf is an interface with generic variant arguments, try to find the compatible one.
 *
 * Note that this function is responsible for resolving ambiguities. Right now we use whatever ordering interfaces_packed gives us.
 *
 * FIXME figure out MS disambiguation rules and fix this function.
 */
int
mono_class_interface_offset_with_variance (MonoClass *klass, MonoClass *itf, gboolean *non_exact_match) {
	int i = mono_class_interface_offset (klass, itf);
	*non_exact_match = FALSE;
	if (i >= 0)
		return i;
	
	if (!mono_class_has_variant_generic_params (itf))
		return -1;

	for (i = 0; i < klass->interface_offsets_count; i++) {
		if (mono_class_is_variant_compatible (itf, klass->interfaces_packed [i], FALSE)) {
			*non_exact_match = TRUE;
			return klass->interface_offsets_packed [i];
		}
	}

	return -1;
}

static void
print_implemented_interfaces (MonoClass *klass) {
	char *name;
	MonoError error;
	GPtrArray *ifaces = NULL;
	int i;
	int ancestor_level = 0;

	name = mono_type_get_full_name (klass);
	printf ("Packed interface table for class %s has size %d\n", name, klass->interface_offsets_count);
	g_free (name);

	for (i = 0; i < klass->interface_offsets_count; i++)
		printf ("  [%03d][UUID %03d][SLOT %03d][SIZE  %03d] interface %s.%s\n", i,
				klass->interfaces_packed [i]->interface_id,
				klass->interface_offsets_packed [i],
				klass->interfaces_packed [i]->method.count,
				klass->interfaces_packed [i]->name_space,
				klass->interfaces_packed [i]->name );
	printf ("Interface flags: ");
	for (i = 0; i <= klass->max_interface_id; i++)
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (klass, i))
			printf ("(%d,T)", i);
		else
			printf ("(%d,F)", i);
	printf ("\n");
	printf ("Dump interface flags:");
#ifdef COMPRESSED_INTERFACE_BITMAP
	{
		const uint8_t* p = klass->interface_bitmap;
		i = klass->max_interface_id;
		while (i > 0) {
			printf (" %d x 00 %02X", p [0], p [1]);
			i -= p [0] * 8;
			i -= 8;
		}
	}
#else
	for (i = 0; i < ((((klass->max_interface_id + 1) >> 3)) + (((klass->max_interface_id + 1) & 7)? 1 :0)); i++)
		printf (" %02X", klass->interface_bitmap [i]);
#endif
	printf ("\n");
	while (klass != NULL) {
		printf ("[LEVEL %d] Implemented interfaces by class %s:\n", ancestor_level, klass->name);
		ifaces = mono_class_get_implemented_interfaces (klass, &error);
		if (!mono_error_ok (&error)) {
			printf ("  Type failed due to %s\n", mono_error_get_message (&error));
			mono_error_cleanup (&error);
		} else if (ifaces) {
			for (i = 0; i < ifaces->len; i++) {
				MonoClass *ic = (MonoClass *)g_ptr_array_index (ifaces, i);
				printf ("  [UIID %d] interface %s\n", ic->interface_id, ic->name);
				printf ("  [%03d][UUID %03d][SLOT %03d][SIZE  %03d] interface %s.%s\n", i,
						ic->interface_id,
						mono_class_interface_offset (klass, ic),
						ic->method.count,
						ic->name_space,
						ic->name );
			}
			g_ptr_array_free (ifaces, TRUE);
		}
		ancestor_level ++;
		klass = klass->parent;
	}
}

static MonoClass*
inflate_class_one_arg (MonoClass *gtype, MonoClass *arg0)
{
	MonoType *args [1];
	args [0] = &arg0->byval_arg;

	return mono_class_bind_generic_parameters (gtype, 1, args, FALSE);
}

static MonoClass*
array_class_get_if_rank (MonoClass *klass, guint rank)
{
	return rank ? mono_array_class_get (klass, rank) : klass;
}

static void
fill_valuetype_array_derived_types (MonoClass **valuetype_types, MonoClass *eclass, int rank)
{
	valuetype_types [0] = eclass;
	if (eclass == mono_defaults.int16_class)
		valuetype_types [1] = mono_defaults.uint16_class;
	else if (eclass == mono_defaults.uint16_class)
		valuetype_types [1] = mono_defaults.int16_class;
	else if (eclass == mono_defaults.int32_class)
		valuetype_types [1] = mono_defaults.uint32_class;
	else if (eclass == mono_defaults.uint32_class)
		valuetype_types [1] = mono_defaults.int32_class;
	else if (eclass == mono_defaults.int64_class)
		valuetype_types [1] = mono_defaults.uint64_class;
	else if (eclass == mono_defaults.uint64_class)
		valuetype_types [1] = mono_defaults.int64_class;
	else if (eclass == mono_defaults.byte_class)
		valuetype_types [1] = mono_defaults.sbyte_class;
	else if (eclass == mono_defaults.sbyte_class)
		valuetype_types [1] = mono_defaults.byte_class;
	else if (eclass->enumtype && mono_class_enum_basetype (eclass))
		valuetype_types [1] = mono_class_from_mono_type (mono_class_enum_basetype (eclass));
}

/* this won't be needed once bug #325495 is completely fixed
 * though we'll need something similar to know which interfaces to allow
 * in arrays when they'll be lazyly created
 * 
 * FIXME: System.Array/InternalEnumerator don't need all this interface fabrication machinery.
 * MS returns diferrent types based on which instance is called. For example:
 * 	object obj = new byte[10][];
 *	Type a = ((IEnumerable<byte[]>)obj).GetEnumerator ().GetType ();
 *	Type b = ((IEnumerable<IList<byte>>)obj).GetEnumerator ().GetType ();
 * 	a != b ==> true
 * 
 * Fixing this should kill quite some code, save some bits and improve compatibility.
 */
static MonoClass**
get_implicit_generic_array_interfaces (MonoClass *klass, int *num, int *is_enumerator)
{
	MonoClass *eclass = klass->element_class;
	static MonoClass* generic_icollection_class = NULL;
	static MonoClass* generic_ienumerable_class = NULL;
	static MonoClass* generic_ienumerator_class = NULL;
	static MonoClass* generic_ireadonlylist_class = NULL;
	static MonoClass* generic_ireadonlycollection_class = NULL;
	MonoClass *valuetype_types[2] = { NULL, NULL };
	MonoClass **interfaces = NULL;
	int i, nifaces, interface_count, real_count, original_rank;
	int all_interfaces;
	gboolean internal_enumerator;
	gboolean eclass_is_valuetype;

	if (!mono_defaults.generic_ilist_class) {
		*num = 0;
		return NULL;
	}
	internal_enumerator = FALSE;
	eclass_is_valuetype = FALSE;
	original_rank = eclass->rank;
	if (klass->byval_arg.type != MONO_TYPE_SZARRAY) {
		if (klass->generic_class && klass->nested_in == mono_defaults.array_class && strcmp (klass->name, "InternalEnumerator`1") == 0)	 {
			/*
			 * For a Enumerator<T[]> we need to get the list of interfaces for T.
			 */
			eclass = mono_class_from_mono_type (klass->generic_class->context.class_inst->type_argv [0]);
			original_rank = eclass->rank;
			if (!eclass->rank)
				eclass = eclass->element_class;
			internal_enumerator = TRUE;
			*is_enumerator = TRUE;
		} else {
			*num = 0;
			return NULL;
		}
	}

	/* 
	 * with this non-lazy impl we can't implement all the interfaces so we do just the minimal stuff
	 * for deep levels of arrays of arrays (string[][] has all the interfaces, string[][][] doesn't)
	 */
	all_interfaces = eclass->rank && eclass->element_class->rank? FALSE: TRUE;

	if (!generic_icollection_class) {
		generic_icollection_class = mono_class_load_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "ICollection`1");
		generic_ienumerable_class = mono_class_load_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "IEnumerable`1");
		generic_ienumerator_class = mono_class_load_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "IEnumerator`1");
		generic_ireadonlylist_class = mono_class_load_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "IReadOnlyList`1");
		generic_ireadonlycollection_class = mono_class_load_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "IReadOnlyCollection`1");
	}

	mono_class_init (eclass);

	/*
	 * Arrays in 2.0 need to implement a number of generic interfaces
	 * (IList`1, ICollection`1, IEnumerable`1 for a number of types depending
	 * on the element class). For net 4.5, we also need to implement IReadOnlyList`1/IReadOnlyCollection`1.
	 * We collect the types needed to build the
	 * instantiations in interfaces at intervals of 3/5, because 3/5 are
	 * the generic interfaces needed to implement.
	 *
	 * On 4.5, as an optimization, we don't expand ref classes for the variant generic interfaces
	 * (IEnumerator, IReadOnlyList and IReadOnlyColleciton). The regular dispatch code can handle those cases.
	 */
	if (eclass->valuetype) {
		nifaces = generic_ireadonlylist_class ? 5 : 3;
		fill_valuetype_array_derived_types (valuetype_types, eclass, original_rank);

		/* IList, ICollection, IEnumerable, IReadOnlyList`1 */
		real_count = interface_count = valuetype_types [1] ? (nifaces * 2) : nifaces;
		if (internal_enumerator) {
			++real_count;
			if (valuetype_types [1])
				++real_count;
		}

		interfaces = (MonoClass **)g_malloc0 (sizeof (MonoClass*) * real_count);
		interfaces [0] = valuetype_types [0];
		if (valuetype_types [1])
			interfaces [nifaces] = valuetype_types [1];

		eclass_is_valuetype = TRUE;
	} else {
		int j;
		int idepth = eclass->idepth;
		if (!internal_enumerator)
			idepth--;
		nifaces = generic_ireadonlylist_class ? 2 : 3;

		// FIXME: This doesn't seem to work/required for generic params
		if (!(eclass->this_arg.type == MONO_TYPE_VAR || eclass->this_arg.type == MONO_TYPE_MVAR || (image_is_dynamic (eclass->image) && !eclass->wastypebuilder)))
			mono_class_setup_interface_offsets (eclass);

		interface_count = all_interfaces? eclass->interface_offsets_count: eclass->interface_count;
		/* we add object for interfaces and the supertypes for the other
		 * types. The last of the supertypes is the element class itself which we
		 * already created the explicit interfaces for (so we include it for IEnumerator
		 * and exclude it for arrays).
		 */
		if (MONO_CLASS_IS_INTERFACE (eclass))
			interface_count++;
		else
			interface_count += idepth;
		if (eclass->rank && eclass->element_class->valuetype) {
			fill_valuetype_array_derived_types (valuetype_types, eclass->element_class, original_rank);
			if (valuetype_types [1])
				++interface_count;
		}
		/* IList, ICollection, IEnumerable, IReadOnlyList */
		interface_count *= nifaces;
		real_count = interface_count;
		if (internal_enumerator) {
			real_count += (MONO_CLASS_IS_INTERFACE (eclass) ? 1 : idepth) + eclass->interface_offsets_count;
			if (valuetype_types [1])
				++real_count;
		}
		interfaces = (MonoClass **)g_malloc0 (sizeof (MonoClass*) * real_count);
		if (MONO_CLASS_IS_INTERFACE (eclass)) {
			interfaces [0] = mono_defaults.object_class;
			j = nifaces;
		} else {
			j = 0;
			for (i = 0; i < idepth; i++) {
				mono_class_init (eclass->supertypes [i]);
				interfaces [j] = eclass->supertypes [i];
				j += nifaces;
			}
		}
		if (all_interfaces) {
			for (i = 0; i < eclass->interface_offsets_count; i++) {
				interfaces [j] = eclass->interfaces_packed [i];
				j += nifaces;
			}
		} else {
			for (i = 0; i < eclass->interface_count; i++) {
				interfaces [j] = eclass->interfaces [i];
				j += nifaces;
			}
		}
		if (valuetype_types [1]) {
			interfaces [j] = array_class_get_if_rank (valuetype_types [1], original_rank);
			j += nifaces;
		}
	}

	/* instantiate the generic interfaces */
	for (i = 0; i < interface_count; i += nifaces) {
		MonoClass *iface = interfaces [i];

		interfaces [i + 0] = inflate_class_one_arg (mono_defaults.generic_ilist_class, iface);
		interfaces [i + 1] = inflate_class_one_arg (generic_icollection_class, iface);

		if (eclass->valuetype) {
			interfaces [i + 2] = inflate_class_one_arg (generic_ienumerable_class, iface);
			if (generic_ireadonlylist_class) {
				interfaces [i + 3] = inflate_class_one_arg (generic_ireadonlylist_class, iface);
				interfaces [i + 4] = inflate_class_one_arg (generic_ireadonlycollection_class, iface);
			}
		} else {
			if (!generic_ireadonlylist_class)
				interfaces [i + 2] = inflate_class_one_arg (generic_ienumerable_class, iface);
		}
	}
	if (internal_enumerator) {
		int j;
		/* instantiate IEnumerator<iface> */
		for (i = 0; i < interface_count; i++) {
			interfaces [i] = inflate_class_one_arg (generic_ienumerator_class, interfaces [i]);
		}
		j = interface_count;
		if (!eclass_is_valuetype) {
			if (MONO_CLASS_IS_INTERFACE (eclass)) {
				interfaces [j] = inflate_class_one_arg (generic_ienumerator_class, mono_defaults.object_class);
				j ++;
			} else {
				for (i = 0; i < eclass->idepth; i++) {
					interfaces [j] = inflate_class_one_arg (generic_ienumerator_class, eclass->supertypes [i]);
					j ++;
				}
			}
			for (i = 0; i < eclass->interface_offsets_count; i++) {
				interfaces [j] = inflate_class_one_arg (generic_ienumerator_class, eclass->interfaces_packed [i]);
				j ++;
			}
		} else {
			interfaces [j++] = inflate_class_one_arg (generic_ienumerator_class, array_class_get_if_rank (valuetype_types [0], original_rank));
		}
		if (valuetype_types [1])
			interfaces [j] = inflate_class_one_arg (generic_ienumerator_class, array_class_get_if_rank (valuetype_types [1], original_rank));
	}
#if 0
	{
	char *type_name = mono_type_get_name_full (&klass->byval_arg, 0);
	for (i = 0; i  < real_count; ++i) {
		char *name = mono_type_get_name_full (&interfaces [i]->byval_arg, 0);
		g_print ("%s implements %s\n", type_name, name);
		g_free (name);
	}
	g_free (type_name);
	}
#endif
	*num = real_count;
	return interfaces;
}

static int
find_array_interface (MonoClass *klass, const char *name)
{
	int i;
	for (i = 0; i < klass->interface_count; ++i) {
		if (strcmp (klass->interfaces [i]->name, name) == 0)
			return i;
	}
	return -1;
}

/*
 * Return the number of virtual methods.
 * Even for interfaces we can't simply return the number of methods as all CLR types are allowed to have static methods.
 * Return -1 on failure.
 * FIXME It would be nice if this information could be cached somewhere.
 */
static int
count_virtual_methods (MonoClass *klass)
{
	int i, count = 0;
	guint32 flags;
	klass = mono_class_get_generic_type_definition (klass); /*We can find this information by looking at the GTD*/

	if (klass->methods || !MONO_CLASS_HAS_STATIC_METADATA (klass)) {
		mono_class_setup_methods (klass);
		if (mono_class_has_failure (klass))
			return -1;

		for (i = 0; i < klass->method.count; ++i) {
			flags = klass->methods [i]->flags;
			if (flags & METHOD_ATTRIBUTE_VIRTUAL)
				++count;
		}
	} else {
		for (i = 0; i < klass->method.count; ++i) {
			flags = mono_metadata_decode_table_row_col (klass->image, MONO_TABLE_METHOD, klass->method.first + i, MONO_METHOD_FLAGS);

			if (flags & METHOD_ATTRIBUTE_VIRTUAL)
				++count;
		}
	}
	return count;
}

static int
find_interface (int num_ifaces, MonoClass **interfaces_full, MonoClass *ic)
{
	int m, l = 0;
	if (!num_ifaces)
		return -1;
	while (1) {
		if (l > num_ifaces)
			return -1;
		m = (l + num_ifaces) / 2;
		if (interfaces_full [m] == ic)
			return m;
		if (l == num_ifaces)
			return -1;
		if (!interfaces_full [m] || interfaces_full [m]->interface_id > ic->interface_id) {
			num_ifaces = m - 1;
		} else {
			l =  m + 1;
		}
	}
}

static int
find_interface_offset (int num_ifaces, MonoClass **interfaces_full, int *interface_offsets_full, MonoClass *ic)
{
	int i = find_interface (num_ifaces, interfaces_full, ic);
	if (i >= 0)
		return interface_offsets_full [i];
	return -1;
}

static mono_bool
set_interface_and_offset (int num_ifaces, MonoClass **interfaces_full, int *interface_offsets_full, MonoClass *ic, int offset, mono_bool force_set)
{
	int i = find_interface (num_ifaces, interfaces_full, ic);
	if (i >= 0) {
		if (!force_set)
			return TRUE;
		interface_offsets_full [i] = offset;
		return FALSE;
	}
	for (i = 0; i < num_ifaces; ++i) {
		if (interfaces_full [i]) {
			int end;
			if (interfaces_full [i]->interface_id < ic->interface_id)
				continue;
			end = i + 1;
			while (end < num_ifaces && interfaces_full [end]) end++;
			memmove (interfaces_full + i + 1, interfaces_full + i, sizeof (MonoClass*) * (end - i));
			memmove (interface_offsets_full + i + 1, interface_offsets_full + i, sizeof (int) * (end - i));
		}
		interfaces_full [i] = ic;
		interface_offsets_full [i] = offset;
		break;
	}
	return FALSE;
}

#ifdef COMPRESSED_INTERFACE_BITMAP

/*
 * Compressed interface bitmap design.
 *
 * Interface bitmaps take a large amount of memory, because their size is
 * linear with the maximum interface id assigned in the process (each interface
 * is assigned a unique id as it is loaded). The number of interface classes
 * is high because of the many implicit interfaces implemented by arrays (we'll
 * need to lazy-load them in the future).
 * Most classes implement a very small number of interfaces, so the bitmap is
 * sparse. This bitmap needs to be checked by interface casts, so access to the
 * needed bit must be fast and doable with few jit instructions.
 *
 * The current compression format is as follows:
 * *) it is a sequence of one or more two-byte elements
 * *) the first byte in the element is the count of empty bitmap bytes
 * at the current bitmap position
 * *) the second byte in the element is an actual bitmap byte at the current
 * bitmap position
 *
 * As an example, the following compressed bitmap bytes:
 * 	0x07 0x01 0x00 0x7
 * correspond to the following bitmap:
 * 	0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x01 0x07
 *
 * Each two-byte element can represent up to 2048 bitmap bits, but as few as a single
 * bitmap byte for non-sparse sequences. In practice the interface bitmaps created
 * during a gmcs bootstrap are reduced to less tha 5% of the original size.
 */

/**
 * mono_compress_bitmap:
 * @dest: destination buffer
 * @bitmap: bitmap buffer
 * @size: size of @bitmap in bytes
 *
 * This is a mono internal function.
 * The @bitmap data is compressed into a format that is small but
 * still searchable in few instructions by the JIT and runtime.
 * The compressed data is stored in the buffer pointed to by the
 * @dest array. Passing a #NULL value for @dest allows to just compute
 * the size of the buffer.
 * This compression algorithm assumes the bits set in the bitmap are
 * few and far between, like in interface bitmaps.
 * Returns: The size of the compressed bitmap in bytes.
 */
int
mono_compress_bitmap (uint8_t *dest, const uint8_t *bitmap, int size)
{
	int numz = 0;
	int res = 0;
	const uint8_t *end = bitmap + size;
	while (bitmap < end) {
		if (*bitmap || numz == 255) {
			if (dest) {
				*dest++ = numz;
				*dest++ = *bitmap;
			}
			res += 2;
			numz = 0;
			bitmap++;
			continue;
		}
		bitmap++;
		numz++;
	}
	if (numz) {
		res += 2;
		if (dest) {
			*dest++ = numz;
			*dest++ = 0;
		}
	}
	return res;
}

/**
 * mono_class_interface_match:
 * @bitmap: a compressed bitmap buffer
 * @id: the index to check in the bitmap
 *
 * This is a mono internal function.
 * Checks if a bit is set in a compressed interface bitmap. @id must
 * be already checked for being smaller than the maximum id encoded in the
 * bitmap.
 *
 * Returns: A non-zero value if bit @id is set in the bitmap @bitmap,
 * #FALSE otherwise.
 */
int
mono_class_interface_match (const uint8_t *bitmap, int id)
{
	while (TRUE) {
		id -= bitmap [0] * 8;
		if (id < 8) {
			if (id < 0)
				return 0;
			return bitmap [1] & (1 << id);
		}
		bitmap += 2;
		id -= 8;
	}
}
#endif

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 * Return -1 on failure and set exception_type
 */
static int
setup_interface_offsets (MonoClass *klass, int cur_slot, gboolean overwrite)
{
	MonoError error;
	MonoClass *k, *ic;
	int i, j, max_iid, num_ifaces;
	MonoClass **interfaces_full = NULL;
	int *interface_offsets_full = NULL;
	GPtrArray *ifaces;
	GPtrArray **ifaces_array = NULL;
	int interface_offsets_count;
	MonoClass **array_interfaces = NULL;
	int num_array_interfaces;
	int is_enumerator = FALSE;

	mono_class_setup_supertypes (klass);
	/* 
	 * get the implicit generic interfaces for either the arrays or for System.Array/InternalEnumerator<T>
	 * implicit interfaces have the property that they are assigned the same slot in the
	 * vtables for compatible interfaces
	 */
	array_interfaces = get_implicit_generic_array_interfaces (klass, &num_array_interfaces, &is_enumerator);

	/* compute maximum number of slots and maximum interface id */
	max_iid = 0;
	num_ifaces = num_array_interfaces; /* this can include duplicated ones */
	ifaces_array = g_new0 (GPtrArray *, klass->idepth);
	for (j = 0; j < klass->idepth; j++) {
		k = klass->supertypes [j];
		num_ifaces += k->interface_count;
		for (i = 0; i < k->interface_count; i++) {
			ic = k->interfaces [i];

			if (!ic->inited)
				mono_class_init (ic);

			if (max_iid < ic->interface_id)
				max_iid = ic->interface_id;
		}
		ifaces = mono_class_get_implemented_interfaces (k, &error);
		if (!mono_error_ok (&error)) {
			char *name = mono_type_get_full_name (k);
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Error getting the interfaces of %s due to %s", name, mono_error_get_message (&error)));
			g_free (name);
			mono_error_cleanup (&error);
			cur_slot = -1;
			goto end;
		}
		if (ifaces) {
			num_ifaces += ifaces->len;
			for (i = 0; i < ifaces->len; ++i) {
				ic = (MonoClass *)g_ptr_array_index (ifaces, i);
				if (max_iid < ic->interface_id)
					max_iid = ic->interface_id;
			}
			ifaces_array [j] = ifaces;
		}
	}

	for (i = 0; i < num_array_interfaces; ++i) {
		ic = array_interfaces [i];
		mono_class_init (ic);
		if (max_iid < ic->interface_id)
			max_iid = ic->interface_id;
	}

	if (MONO_CLASS_IS_INTERFACE (klass)) {
		num_ifaces++;
		if (max_iid < klass->interface_id)
			max_iid = klass->interface_id;
	}
	klass->max_interface_id = max_iid;
	/* compute vtable offset for interfaces */
	interfaces_full = (MonoClass **)g_malloc0 (sizeof (MonoClass*) * num_ifaces);
	interface_offsets_full = (int *)g_malloc (sizeof (int) * num_ifaces);

	for (i = 0; i < num_ifaces; i++) {
		interface_offsets_full [i] = -1;
	}

	/* skip the current class */
	for (j = 0; j < klass->idepth - 1; j++) {
		k = klass->supertypes [j];
		ifaces = ifaces_array [j];

		if (ifaces) {
			for (i = 0; i < ifaces->len; ++i) {
				int io;
				ic = (MonoClass *)g_ptr_array_index (ifaces, i);
				
				/*Force the sharing of interface offsets between parent and subtypes.*/
				io = mono_class_interface_offset (k, ic);
				g_assert (io >= 0);
				set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, ic, io, TRUE);
			}
		}
	}

	g_assert (klass == klass->supertypes [klass->idepth - 1]);
	ifaces = ifaces_array [klass->idepth - 1];
	if (ifaces) {
		for (i = 0; i < ifaces->len; ++i) {
			int count;
			ic = (MonoClass *)g_ptr_array_index (ifaces, i);
			if (set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, ic, cur_slot, FALSE))
				continue;
			count = count_virtual_methods (ic);
			if (count == -1) {
				char *name = mono_type_get_full_name (ic);
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Error calculating interface offset of %s", name));
				g_free (name);
				cur_slot = -1;
				goto end;
			}
			cur_slot += count;
		}
	}

	if (MONO_CLASS_IS_INTERFACE (klass))
		set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, klass, cur_slot, TRUE);

	if (num_array_interfaces) {
		if (is_enumerator) {
			int ienumerator_idx = find_array_interface (klass, "IEnumerator`1");
			int ienumerator_offset = find_interface_offset (num_ifaces, interfaces_full, interface_offsets_full, klass->interfaces [ienumerator_idx]);
			g_assert (ienumerator_offset >= 0);
			for (i = 0; i < num_array_interfaces; ++i) {
				ic = array_interfaces [i];
				if (strcmp (ic->name, "IEnumerator`1") == 0)
					set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, ic, ienumerator_offset, TRUE);
				else
					g_assert_not_reached ();
				/*g_print ("type %s has %s offset at %d (%s)\n", klass->name, ic->name, interface_offsets_full [ic->interface_id], klass->interfaces [0]->name);*/
			}
		} else {
			int ilist_offset, icollection_offset, ienumerable_offset, ireadonlylist_offset, ireadonlycollection_offset;
			int ilist_iface_idx = find_array_interface (klass, "IList`1");
			MonoClass* ilist_class = klass->interfaces [ilist_iface_idx];
			int ireadonlylist_iface_idx = find_array_interface (klass, "IReadOnlyList`1");
			MonoClass* ireadonlylist_class = ireadonlylist_iface_idx != -1 ? klass->interfaces [ireadonlylist_iface_idx] : NULL;
			int icollection_iface_idx = find_array_interface (ilist_class, "ICollection`1");
			int ienumerable_iface_idx = find_array_interface (ilist_class, "IEnumerable`1");
			int ireadonlycollection_iface_idx = ireadonlylist_iface_idx != -1 ? find_array_interface (ireadonlylist_class, "IReadOnlyCollection`1") : -1;
			ilist_offset = find_interface_offset (num_ifaces, interfaces_full, interface_offsets_full, klass->interfaces [ilist_iface_idx]);
			icollection_offset = find_interface_offset (num_ifaces, interfaces_full, interface_offsets_full, ilist_class->interfaces [icollection_iface_idx]);
			ienumerable_offset = find_interface_offset (num_ifaces, interfaces_full, interface_offsets_full, ilist_class->interfaces [ienumerable_iface_idx]);
			ireadonlylist_offset = ireadonlylist_iface_idx != -1 ? find_interface_offset (num_ifaces, interfaces_full, interface_offsets_full, klass->interfaces [ireadonlylist_iface_idx]) : -1;
			ireadonlycollection_offset = ireadonlycollection_iface_idx != -1 ? find_interface_offset (num_ifaces, interfaces_full, interface_offsets_full, ireadonlylist_class->interfaces [ireadonlycollection_iface_idx]) : -1;
			g_assert (ilist_offset >= 0 && icollection_offset >= 0 && ienumerable_offset >= 0);
			for (i = 0; i < num_array_interfaces; ++i) {
				int offset;
				ic = array_interfaces [i];
				if (ic->generic_class->container_class == mono_defaults.generic_ilist_class)
					offset = ilist_offset;
				else if (strcmp (ic->name, "ICollection`1") == 0)
					offset = icollection_offset;
				else if (strcmp (ic->name, "IEnumerable`1") == 0)
					offset = ienumerable_offset;
				else if (strcmp (ic->name, "IReadOnlyList`1") == 0)
					offset = ireadonlylist_offset;
				else if (strcmp (ic->name, "IReadOnlyCollection`1") == 0)
					offset = ireadonlycollection_offset;
				else
					g_assert_not_reached ();
				set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, ic, offset, TRUE);
				/*g_print ("type %s has %s offset at %d (%s)\n", klass->name, ic->name, offset, klass->interfaces [0]->name);*/
			}
		}
	}

	for (interface_offsets_count = 0, i = 0; i < num_ifaces; i++) {
		if (interface_offsets_full [i] != -1) {
			interface_offsets_count ++;
		}
	}

	/*
	 * We might get called multiple times:
	 * - mono_class_init ()
	 * - mono_class_setup_vtable ().
	 * - mono_class_setup_interface_offsets ().
	 * mono_class_setup_interface_offsets () passes 0 as CUR_SLOT, so the computed interface offsets will be invalid. This
	 * means we have to overwrite those when called from other places (#4440).
	 */
	if (klass->interfaces_packed) {
		if (!overwrite)
			g_assert (klass->interface_offsets_count == interface_offsets_count);
	} else {
		uint8_t *bitmap;
		int bsize;
		klass->interface_offsets_count = interface_offsets_count;
		klass->interfaces_packed = (MonoClass **)mono_class_alloc (klass, sizeof (MonoClass*) * interface_offsets_count);
		klass->interface_offsets_packed = (guint16 *)mono_class_alloc (klass, sizeof (guint16) * interface_offsets_count);
		bsize = (sizeof (guint8) * ((max_iid + 1) >> 3)) + (((max_iid + 1) & 7)? 1 :0);
#ifdef COMPRESSED_INTERFACE_BITMAP
		bitmap = g_malloc0 (bsize);
#else
		bitmap = (uint8_t *)mono_class_alloc0 (klass, bsize);
#endif
		for (i = 0; i < interface_offsets_count; i++) {
			int id = interfaces_full [i]->interface_id;
			bitmap [id >> 3] |= (1 << (id & 7));
			klass->interfaces_packed [i] = interfaces_full [i];
			klass->interface_offsets_packed [i] = interface_offsets_full [i];
			/*if (num_array_interfaces)
			  g_print ("type %s has %s offset at %d\n", mono_type_get_name_full (&klass->byval_arg, 0), mono_type_get_name_full (&interfaces_full [i]->byval_arg, 0), interface_offsets_full [i]);*/
		}
#ifdef COMPRESSED_INTERFACE_BITMAP
		i = mono_compress_bitmap (NULL, bitmap, bsize);
		klass->interface_bitmap = mono_class_alloc0 (klass, i);
		mono_compress_bitmap (klass->interface_bitmap, bitmap, bsize);
		g_free (bitmap);
#else
		klass->interface_bitmap = bitmap;
#endif
	}

end:
	g_free (interfaces_full);
	g_free (interface_offsets_full);
	g_free (array_interfaces);
	for (i = 0; i < klass->idepth; i++) {
		ifaces = ifaces_array [i];
		if (ifaces)
			g_ptr_array_free (ifaces, TRUE);
	}
	g_free (ifaces_array);
	
	//printf ("JUST DONE: ");
	//print_implemented_interfaces (klass);
 
 	return cur_slot;
}

/*
 * Setup interface offsets for interfaces. 
 * Initializes:
 * - klass->max_interface_id
 * - klass->interface_offsets_count
 * - klass->interfaces_packed
 * - klass->interface_offsets_packed
 * - klass->interface_bitmap
 *
 * This function can fail @class.
 */
void
mono_class_setup_interface_offsets (MonoClass *klass)
{
	mono_loader_lock ();

	setup_interface_offsets (klass, 0, FALSE);

	mono_loader_unlock ();
}

/*Checks if @klass has @parent as one of it's parents type gtd
 *
 * For example:
 * 	Foo<T>
 *	Bar<T> : Foo<Bar<Bar<T>>>
 *
 */
static gboolean
mono_class_has_gtd_parent (MonoClass *klass, MonoClass *parent)
{
	klass = mono_class_get_generic_type_definition (klass);
	parent = mono_class_get_generic_type_definition (parent);
	mono_class_setup_supertypes (klass);
	mono_class_setup_supertypes (parent);

	return klass->idepth >= parent->idepth &&
		mono_class_get_generic_type_definition (klass->supertypes [parent->idepth - 1]) == parent;
}

gboolean
mono_class_check_vtable_constraints (MonoClass *klass, GList *in_setup)
{
	MonoGenericInst *ginst;
	int i;
	if (!klass->generic_class) {
		mono_class_setup_vtable_full (klass, in_setup);
		return !mono_class_has_failure (klass);
	}

	mono_class_setup_vtable_full (mono_class_get_generic_type_definition (klass), in_setup);
	if (mono_class_has_failure (klass->generic_class->container_class)) {
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Failed to load generic definition vtable"));
		return FALSE;
	}

	ginst = klass->generic_class->context.class_inst;
	for (i = 0; i < ginst->type_argc; ++i) {
		MonoClass *arg;
		if (ginst->type_argv [i]->type != MONO_TYPE_GENERICINST)
			continue;
		arg = mono_class_from_mono_type (ginst->type_argv [i]);
		/*Those 2 will be checked by mono_class_setup_vtable itself*/
		if (mono_class_has_gtd_parent (klass, arg) || mono_class_has_gtd_parent (arg, klass))
			continue;
		if (!mono_class_check_vtable_constraints (arg, in_setup)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Failed to load generic parameter %d", i));
			return FALSE;
		}
	}
	return TRUE;
}
 
/*
 * mono_class_setup_vtable:
 *
 *   Creates the generic vtable of CLASS.
 * Initializes the following fields in MonoClass:
 * - vtable
 * - vtable_size
 * Plus all the fields initialized by setup_interface_offsets ().
 * If there is an error during vtable construction, klass->exception_type is set.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_vtable (MonoClass *klass)
{
	mono_class_setup_vtable_full (klass, NULL);
}

static void
mono_class_setup_vtable_full (MonoClass *klass, GList *in_setup)
{
	MonoMethod **overrides;
	MonoGenericContext *context;
	guint32 type_token;
	int onum = 0;
	gboolean ok = TRUE;

	if (klass->vtable)
		return;

	if (MONO_CLASS_IS_INTERFACE (klass)) {
		/* This sets method->slot for all methods if this is an interface */
		mono_class_setup_methods (klass);
		return;
	}

	if (mono_class_has_failure (klass))
		return;

	if (g_list_find (in_setup, klass))
		return;

	mono_loader_lock ();

	if (klass->vtable) {
		mono_loader_unlock ();
		return;
	}

	mono_stats.generic_vtable_count ++;
	in_setup = g_list_prepend (in_setup, klass);

	if (klass->generic_class) {
		if (!mono_class_check_vtable_constraints (klass, in_setup)) {
			mono_loader_unlock ();
			g_list_remove (in_setup, klass);
			return;
		}

		context = mono_class_get_context (klass);
		type_token = klass->generic_class->container_class->type_token;
	} else {
		context = (MonoGenericContext *) klass->generic_container;
		type_token = klass->type_token;
	}

	if (image_is_dynamic (klass->image)) {
		/* Generic instances can have zero method overrides without causing any harm.
		 * This is true since we don't do layout all over again for them, we simply inflate
		 * the layout of the parent.
		 */
		mono_reflection_get_dynamic_overrides (klass, &overrides, &onum);
	} else {
		/* The following call fails if there are missing methods in the type */
		/* FIXME it's probably a good idea to avoid this for generic instances. */
		ok = mono_class_get_overrides_full (klass->image, type_token, &overrides, &onum, context);
	}

	if (ok)
		mono_class_setup_vtable_general (klass, overrides, onum, in_setup);
	else
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Could not load list of method overrides"));
		
	g_free (overrides);

	mono_loader_unlock ();
	g_list_remove (in_setup, klass);

	return;
}

#define DEBUG_INTERFACE_VTABLE_CODE 0
#define TRACE_INTERFACE_VTABLE_CODE 0
#define VERIFY_INTERFACE_VTABLE_CODE 0
#define VTABLE_SELECTOR (1)

#if (TRACE_INTERFACE_VTABLE_CODE|DEBUG_INTERFACE_VTABLE_CODE)
#define DEBUG_INTERFACE_VTABLE(stmt) do {\
	if (!(VTABLE_SELECTOR)) break; \
	stmt;\
} while (0)
#else
#define DEBUG_INTERFACE_VTABLE(stmt)
#endif

#if TRACE_INTERFACE_VTABLE_CODE
#define TRACE_INTERFACE_VTABLE(stmt) do {\
	if (!(VTABLE_SELECTOR)) break; \
	stmt;\
} while (0)
#else
#define TRACE_INTERFACE_VTABLE(stmt)
#endif

#if VERIFY_INTERFACE_VTABLE_CODE
#define VERIFY_INTERFACE_VTABLE(stmt) do {\
	if (!(VTABLE_SELECTOR)) break; \
	stmt;\
} while (0)
#else
#define VERIFY_INTERFACE_VTABLE(stmt)
#endif


#if (TRACE_INTERFACE_VTABLE_CODE|DEBUG_INTERFACE_VTABLE_CODE)
static char*
mono_signature_get_full_desc (MonoMethodSignature *sig, gboolean include_namespace)
{
	int i;
	char *result;
	GString *res = g_string_new ("");
	
	g_string_append_c (res, '(');
	for (i = 0; i < sig->param_count; ++i) {
		if (i > 0)
			g_string_append_c (res, ',');
		mono_type_get_desc (res, sig->params [i], include_namespace);
	}
	g_string_append (res, ")=>");
	if (sig->ret != NULL) {
		mono_type_get_desc (res, sig->ret, include_namespace);
	} else {
		g_string_append (res, "NULL");
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}
static void
print_method_signatures (MonoMethod *im, MonoMethod *cm) {
	char *im_sig = mono_signature_get_full_desc (mono_method_signature (im), TRUE);
	char *cm_sig = mono_signature_get_full_desc (mono_method_signature (cm), TRUE);
	printf ("(IM \"%s\", CM \"%s\")", im_sig, cm_sig);
	g_free (im_sig);
	g_free (cm_sig);
	
}

#endif
static gboolean
is_wcf_hack_disabled (void)
{
	static gboolean disabled;
	static gboolean inited = FALSE;
	if (!inited) {
		disabled = g_getenv ("MONO_DISABLE_WCF_HACK") != NULL;
		inited = TRUE;
	}
	return disabled;
}

static gboolean
check_interface_method_override (MonoClass *klass, MonoMethod *im, MonoMethod *cm, gboolean require_newslot, gboolean interface_is_explicitly_implemented_by_class, gboolean slot_is_empty)
{
	MonoMethodSignature *cmsig, *imsig;
	if (strcmp (im->name, cm->name) == 0) {
		if (! (cm->flags & METHOD_ATTRIBUTE_PUBLIC)) {
			TRACE_INTERFACE_VTABLE (printf ("[PUBLIC CHECK FAILED]"));
			return FALSE;
		}
		if (! slot_is_empty) {
			if (require_newslot) {
				if (! interface_is_explicitly_implemented_by_class) {
					TRACE_INTERFACE_VTABLE (printf ("[NOT EXPLICIT IMPLEMENTATION IN FULL SLOT REFUSED]"));
					return FALSE;
				}
				if (! (cm->flags & METHOD_ATTRIBUTE_NEW_SLOT)) {
					TRACE_INTERFACE_VTABLE (printf ("[NEWSLOT CHECK FAILED]"));
					return FALSE;
				}
			} else {
				TRACE_INTERFACE_VTABLE (printf ("[FULL SLOT REFUSED]"));
			}
		}
		cmsig = mono_method_signature (cm);
		imsig = mono_method_signature (im);
		if (!cmsig || !imsig) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Could not resolve the signature of a virtual method"));
			return FALSE;
		}

		if (! mono_metadata_signature_equal (cmsig, imsig)) {
			TRACE_INTERFACE_VTABLE (printf ("[SIGNATURE CHECK FAILED  "));
			TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
			TRACE_INTERFACE_VTABLE (printf ("]"));
			return FALSE;
		}
		TRACE_INTERFACE_VTABLE (printf ("[SECURITY CHECKS]"));
		if (mono_security_core_clr_enabled ())
			mono_security_core_clr_check_override (klass, cm, im);

		TRACE_INTERFACE_VTABLE (printf ("[NAME CHECK OK]"));
		if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (cm, im, NULL)) {
			char *body_name = mono_method_full_name (cm, TRUE);
			char *decl_name = mono_method_full_name (im, TRUE);
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Method %s overrides method '%s' which is not accessible", body_name, decl_name));
			g_free (body_name);
			g_free (decl_name);
			return FALSE;
		}

		return TRUE;
	} else {
		MonoClass *ic = im->klass;
		const char *ic_name_space = ic->name_space;
		const char *ic_name = ic->name;
		char *subname;
		
		if (! require_newslot) {
			TRACE_INTERFACE_VTABLE (printf ("[INJECTED METHOD REFUSED]"));
			return FALSE;
		}
		if (cm->klass->rank == 0) {
			TRACE_INTERFACE_VTABLE (printf ("[RANK CHECK FAILED]"));
			return FALSE;
		}
		cmsig = mono_method_signature (cm);
		imsig = mono_method_signature (im);
		if (!cmsig || !imsig) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Could not resolve the signature of a virtual method"));
			return FALSE;
		}

		if (! mono_metadata_signature_equal (cmsig, imsig)) {
			TRACE_INTERFACE_VTABLE (printf ("[(INJECTED) SIGNATURE CHECK FAILED  "));
			TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
			TRACE_INTERFACE_VTABLE (printf ("]"));
			return FALSE;
		}
		if (mono_class_get_image (ic) != mono_defaults.corlib) {
			TRACE_INTERFACE_VTABLE (printf ("[INTERFACE CORLIB CHECK FAILED]"));
			return FALSE;
		}
		if ((ic_name_space == NULL) || (strcmp (ic_name_space, "System.Collections.Generic") != 0)) {
			TRACE_INTERFACE_VTABLE (printf ("[INTERFACE NAMESPACE CHECK FAILED]"));
			return FALSE;
		}
		if ((ic_name == NULL) || ((strcmp (ic_name, "IEnumerable`1") != 0) && (strcmp (ic_name, "ICollection`1") != 0) && (strcmp (ic_name, "IList`1") != 0) && (strcmp (ic_name, "IReadOnlyList`1") != 0) && (strcmp (ic_name, "IReadOnlyCollection`1") != 0))) {
			TRACE_INTERFACE_VTABLE (printf ("[INTERFACE NAME CHECK FAILED]"));
			return FALSE;
		}
		
		subname = strstr (cm->name, ic_name_space);
		if (subname != cm->name) {
			TRACE_INTERFACE_VTABLE (printf ("[ACTUAL NAMESPACE CHECK FAILED]"));
			return FALSE;
		}
		subname += strlen (ic_name_space);
		if (subname [0] != '.') {
			TRACE_INTERFACE_VTABLE (printf ("[FIRST DOT CHECK FAILED]"));
			return FALSE;
		}
		subname ++;
		if (strstr (subname, ic_name) != subname) {
			TRACE_INTERFACE_VTABLE (printf ("[ACTUAL CLASS NAME CHECK FAILED]"));
			return FALSE;
		}
		subname += strlen (ic_name);
		if (subname [0] != '.') {
			TRACE_INTERFACE_VTABLE (printf ("[SECOND DOT CHECK FAILED]"));
			return FALSE;
		}
		subname ++;
		if (strcmp (subname, im->name) != 0) {
			TRACE_INTERFACE_VTABLE (printf ("[METHOD NAME CHECK FAILED]"));
			return FALSE;
		}
		
		TRACE_INTERFACE_VTABLE (printf ("[SECURITY CHECKS (INJECTED CASE)]"));
		if (mono_security_core_clr_enabled ())
			mono_security_core_clr_check_override (klass, cm, im);

		TRACE_INTERFACE_VTABLE (printf ("[INJECTED INTERFACE CHECK OK]"));
		if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (cm, im, NULL)) {
			char *body_name = mono_method_full_name (cm, TRUE);
			char *decl_name = mono_method_full_name (im, TRUE);
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Method %s overrides method '%s' which is not accessible", body_name, decl_name));
			g_free (body_name);
			g_free (decl_name);
			return FALSE;
		}
		
		return TRUE;
	}
}

#if (TRACE_INTERFACE_VTABLE_CODE|DEBUG_INTERFACE_VTABLE_CODE)
static void
foreach_override (gpointer key, gpointer value, gpointer user_data) {
	MonoMethod *method = key;
	MonoMethod *override = value;
	MonoClass *method_class = mono_method_get_class (method);
	MonoClass *override_class = mono_method_get_class (override);
	
	printf ("  Method '%s.%s:%s' has override '%s.%s:%s'\n",
			mono_class_get_namespace (method_class), mono_class_get_name (method_class), mono_method_get_name (method),
			mono_class_get_namespace (override_class), mono_class_get_name (override_class), mono_method_get_name (override));
}
static void
print_overrides (GHashTable *override_map, const char *message) {
	if (override_map) {
		printf ("Override map \"%s\" START:\n", message);
		g_hash_table_foreach (override_map, foreach_override, NULL);
		printf ("Override map \"%s\" END.\n", message);
	} else {
		printf ("Override map \"%s\" EMPTY.\n", message);
	}
}
static void
print_vtable_full (MonoClass *klass, MonoMethod** vtable, int size, int first_non_interface_slot, const char *message, gboolean print_interfaces) {
	char *full_name = mono_type_full_name (&klass->byval_arg);
	int i;
	int parent_size;
	
	printf ("*** Vtable for class '%s' at \"%s\" (size %d)\n", full_name, message, size);
	
	if (print_interfaces) {
		print_implemented_interfaces (klass);
		printf ("* Interfaces for class '%s' done.\nStarting vtable (size %d):\n", full_name, size);
	}
	
	if (klass->parent) {
		parent_size = klass->parent->vtable_size;
	} else {
		parent_size = 0;
	}
	for (i = 0; i < size; ++i) {
		MonoMethod *cm = vtable [i];
		char *cm_name = cm ? mono_method_full_name (cm, TRUE) : g_strdup ("nil");
		char newness = (i < parent_size) ? 'O' : ((i < first_non_interface_slot) ? 'I' : 'N');

		printf ("  [%c][%03d][INDEX %03d] %s [%p]\n", newness, i, cm ? cm->slot : - 1, cm_name, cm);
		g_free (cm_name);
	}

	g_free (full_name);
}
#endif

#if VERIFY_INTERFACE_VTABLE_CODE
static int
mono_method_try_get_vtable_index (MonoMethod *method)
{
	if (method->is_inflated && (method->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		MonoMethodInflated *imethod = (MonoMethodInflated*)method;
		if (imethod->declaring->is_generic)
			return imethod->declaring->slot;
	}
	return method->slot;
}

static void
mono_class_verify_vtable (MonoClass *klass)
{
	int i;
	char *full_name = mono_type_full_name (&klass->byval_arg);

	printf ("*** Verifying VTable of class '%s' \n", full_name);
	g_free (full_name);
	full_name = NULL;
	
	if (!klass->methods)
		return;

	for (i = 0; i < klass->method.count; ++i) {
		MonoMethod *cm = klass->methods [i];
		int slot;

		if (!(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
			continue;

		g_free (full_name);
		full_name = mono_method_full_name (cm, TRUE);

		slot = mono_method_try_get_vtable_index (cm);
		if (slot >= 0) {
			if (slot >= klass->vtable_size) {
				printf ("\tInvalid method %s at index %d with vtable of length %d\n", full_name, slot, klass->vtable_size);
				continue;
			}

			if (slot >= 0 && klass->vtable [slot] != cm && (klass->vtable [slot])) {
				char *other_name = klass->vtable [slot] ? mono_method_full_name (klass->vtable [slot], TRUE) : g_strdup ("[null value]");
				printf ("\tMethod %s has slot %d but vtable has %s on it\n", full_name, slot, other_name);
				g_free (other_name);
			}
		} else
			printf ("\tVirtual method %s does n't have an assigned slot\n", full_name);
	}
	g_free (full_name);
}
#endif

static void
print_unimplemented_interface_method_info (MonoClass *klass, MonoClass *ic, MonoMethod *im, int im_slot, MonoMethod **overrides, int onum) {
	int index;
	char *method_signature;
	char *type_name;
	
	for (index = 0; index < onum; ++index) {
		mono_trace_warning (MONO_TRACE_TYPE, " at slot %d: %s (%d) overrides %s (%d)\n", im_slot, overrides [index*2+1]->name, 
			 overrides [index*2+1]->slot, overrides [index*2]->name, overrides [index*2]->slot);
	}
	method_signature = mono_signature_get_desc (mono_method_signature (im), FALSE);
	type_name = mono_type_full_name (&klass->byval_arg);
	mono_trace_warning (MONO_TRACE_TYPE, "no implementation for interface method %s::%s(%s) in class %s\n",
		mono_type_get_name (&ic->byval_arg), im->name, method_signature, type_name);
	g_free (method_signature);
	g_free (type_name);
	mono_class_setup_methods (klass);
	if (mono_class_has_failure (klass)) {
		char *name = mono_type_get_full_name (klass);
		mono_trace_warning (MONO_TRACE_TYPE, "CLASS %s failed to resolve methods\n", name);
		g_free (name);
		return;
	}
	for (index = 0; index < klass->method.count; ++index) {
		MonoMethod *cm = klass->methods [index];
		method_signature = mono_signature_get_desc (mono_method_signature (cm), TRUE);

		mono_trace_warning (MONO_TRACE_TYPE, "METHOD %s(%s)\n", cm->name, method_signature);
		g_free (method_signature);
	}
}

static MonoMethod*
mono_method_get_method_definition (MonoMethod *method)
{
	while (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;
	return method;
}

static gboolean
verify_class_overrides (MonoClass *klass, MonoMethod **overrides, int onum)
{
	int i;

	for (i = 0; i < onum; ++i) {
		MonoMethod *decl = overrides [i * 2];
		MonoMethod *body = overrides [i * 2 + 1];

		if (mono_class_get_generic_type_definition (body->klass) != mono_class_get_generic_type_definition (klass)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Method belongs to a different class than the declared one"));
			return FALSE;
		}

		if (!(body->flags & METHOD_ATTRIBUTE_VIRTUAL) || (body->flags & METHOD_ATTRIBUTE_STATIC)) {
			if (body->flags & METHOD_ATTRIBUTE_STATIC)
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Method must not be static to override a base type"));
			else
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Method must be virtual to override a base type"));
			return FALSE;
		}

		if (!(decl->flags & METHOD_ATTRIBUTE_VIRTUAL) || (decl->flags & METHOD_ATTRIBUTE_STATIC)) {
			if (body->flags & METHOD_ATTRIBUTE_STATIC)
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Cannot override a static method in a base type"));
			else
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Cannot override a non virtual method in a base type"));
			return FALSE;
		}

		if (!mono_class_is_assignable_from_slow (decl->klass, klass)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Method overrides a class or interface that is not extended or implemented by this type"));
			return FALSE;
		}

		body = mono_method_get_method_definition (body);
		decl = mono_method_get_method_definition (decl);

		if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (body, decl, NULL)) {
			char *body_name = mono_method_full_name (body, TRUE);
			char *decl_name = mono_method_full_name (decl, TRUE);
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Method %s overrides method '%s' which is not accessible", body_name, decl_name));
			g_free (body_name);
			g_free (decl_name);
			return FALSE;
		}
	}
	return TRUE;
}

static gboolean
mono_class_need_stelemref_method (MonoClass *klass)
{
	return klass->rank == 1 && MONO_TYPE_IS_REFERENCE (&klass->element_class->byval_arg);
}

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 */
void
mono_class_setup_vtable_general (MonoClass *klass, MonoMethod **overrides, int onum, GList *in_setup)
{
	MonoError error;
	MonoClass *k, *ic;
	MonoMethod **vtable;
	int i, max_vtsize = 0, max_iid, cur_slot = 0;
	GPtrArray *ifaces = NULL;
	GHashTable *override_map = NULL;
	MonoMethod *cm;
#if (DEBUG_INTERFACE_VTABLE_CODE|TRACE_INTERFACE_VTABLE_CODE)
	int first_non_interface_slot;
#endif
	GSList *virt_methods = NULL, *l;
	int stelemref_slot = 0;

	if (klass->vtable)
		return;

	if (overrides && !verify_class_overrides (klass, overrides, onum))
		return;

	ifaces = mono_class_get_implemented_interfaces (klass, &error);
	if (!mono_error_ok (&error)) {
		char *name = mono_type_get_full_name (klass);
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Could not resolve %s interfaces due to %s", name, mono_error_get_message (&error)));
		g_free (name);
		mono_error_cleanup (&error);
		return;
	} else if (ifaces) {
		for (i = 0; i < ifaces->len; i++) {
			MonoClass *ic = (MonoClass *)g_ptr_array_index (ifaces, i);
			max_vtsize += ic->method.count;
		}
		g_ptr_array_free (ifaces, TRUE);
		ifaces = NULL;
	}
	
	if (klass->parent) {
		mono_class_init (klass->parent);
		mono_class_setup_vtable_full (klass->parent, in_setup);

		if (mono_class_has_failure (klass->parent)) {
			char *name = mono_type_get_full_name (klass->parent);
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Parent %s failed to load", name));
			g_free (name);
			return;
		}

		max_vtsize += klass->parent->vtable_size;
		cur_slot = klass->parent->vtable_size;
	}

	max_vtsize += klass->method.count;

	/*Array have a slot for stelemref*/
	if (mono_class_need_stelemref_method (klass)) {
		stelemref_slot = cur_slot;
		++max_vtsize;
		++cur_slot;
	}

	vtable = (MonoMethod **)alloca (sizeof (gpointer) * max_vtsize);
	memset (vtable, 0, sizeof (gpointer) * max_vtsize);

	/* printf ("METAINIT %s.%s\n", klass->name_space, klass->name); */

	cur_slot = setup_interface_offsets (klass, cur_slot, TRUE);
	if (cur_slot == -1) /*setup_interface_offsets fails the type.*/
		return;

	max_iid = klass->max_interface_id;
	DEBUG_INTERFACE_VTABLE (first_non_interface_slot = cur_slot);

	/* Optimized version for generic instances */
	if (klass->generic_class) {
		MonoError error;
		MonoClass *gklass = klass->generic_class->container_class;
		MonoMethod **tmp;

		mono_class_setup_vtable_full (gklass, in_setup);
		if (mono_class_has_failure (gklass)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			return;
		}

		tmp = (MonoMethod **)mono_class_alloc0 (klass, sizeof (gpointer) * gklass->vtable_size);
		klass->vtable_size = gklass->vtable_size;
		for (i = 0; i < gklass->vtable_size; ++i)
			if (gklass->vtable [i]) {
				MonoMethod *inflated = mono_class_inflate_generic_method_full_checked (gklass->vtable [i], klass, mono_class_get_context (klass), &error);
				if (!mono_error_ok (&error)) {
					char *err_msg = g_strdup_printf ("Could not inflate method due to %s", mono_error_get_message (&error));
					mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, err_msg);
					g_free (err_msg);
					mono_error_cleanup (&error);
					return;
				}
				tmp [i] = inflated;
				tmp [i]->slot = gklass->vtable [i]->slot;
			}
		mono_memory_barrier ();
		klass->vtable = tmp;

		/* Have to set method->slot for abstract virtual methods */
		if (klass->methods && gklass->methods) {
			for (i = 0; i < klass->method.count; ++i)
				if (klass->methods [i]->slot == -1)
					klass->methods [i]->slot = gklass->methods [i]->slot;
		}

		return;
	}

	if (klass->parent && klass->parent->vtable_size) {
		MonoClass *parent = klass->parent;
		int i;
		
		memcpy (vtable, parent->vtable,  sizeof (gpointer) * parent->vtable_size);
		
		// Also inherit parent interface vtables, just as a starting point.
		// This is needed otherwise bug-77127.exe fails when the property methods
		// have different names in the iterface and the class, because for child
		// classes the ".override" information is not used anymore.
		for (i = 0; i < parent->interface_offsets_count; i++) {
			MonoClass *parent_interface = parent->interfaces_packed [i];
			int interface_offset = mono_class_interface_offset (klass, parent_interface);
			/*FIXME this is now dead code as this condition will never hold true.
			Since interface offsets are inherited then the offset of an interface implemented
			by a parent will never be the out of it's vtable boundary.
			*/
			if (interface_offset >= parent->vtable_size) {
				int parent_interface_offset = mono_class_interface_offset (parent, parent_interface);
				int j;
				
				mono_class_setup_methods (parent_interface); /*FIXME Just kill this whole chunk of dead code*/
				TRACE_INTERFACE_VTABLE (printf ("    +++ Inheriting interface %s.%s\n", parent_interface->name_space, parent_interface->name));
				for (j = 0; j < parent_interface->method.count && !mono_class_has_failure (klass); j++) {
					vtable [interface_offset + j] = parent->vtable [parent_interface_offset + j];
					TRACE_INTERFACE_VTABLE (printf ("    --- Inheriting: [%03d][(%03d)+(%03d)] => [%03d][(%03d)+(%03d)]\n",
							parent_interface_offset + j, parent_interface_offset, j,
							interface_offset + j, interface_offset, j));
				}
			}
			
		}
	}

	/*Array have a slot for stelemref*/
	if (mono_class_need_stelemref_method (klass)) {
		MonoMethod *method = mono_marshal_get_virtual_stelemref (klass);
		if (!method->slot)
			method->slot = stelemref_slot;
		else
			g_assert (method->slot == stelemref_slot);

		vtable [stelemref_slot] = method;
	}

	TRACE_INTERFACE_VTABLE (print_vtable_full (klass, vtable, cur_slot, first_non_interface_slot, "AFTER INHERITING PARENT VTABLE", TRUE));
	/* override interface methods */
	for (i = 0; i < onum; i++) {
		MonoMethod *decl = overrides [i*2];
		if (MONO_CLASS_IS_INTERFACE (decl->klass)) {
			int dslot;
			dslot = mono_method_get_vtable_slot (decl);
			if (dslot == -1) {
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
				return;
			}

			dslot += mono_class_interface_offset (klass, decl->klass);
			vtable [dslot] = overrides [i*2 + 1];
			vtable [dslot]->slot = dslot;
			if (!override_map)
				override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);

			g_hash_table_insert (override_map, overrides [i * 2], overrides [i * 2 + 1]);

			if (mono_security_core_clr_enabled ())
				mono_security_core_clr_check_override (klass, vtable [dslot], decl);
		}
	}
	TRACE_INTERFACE_VTABLE (print_overrides (override_map, "AFTER OVERRIDING INTERFACE METHODS"));
	TRACE_INTERFACE_VTABLE (print_vtable_full (klass, vtable, cur_slot, first_non_interface_slot, "AFTER OVERRIDING INTERFACE METHODS", FALSE));

	/*
	 * Create a list of virtual methods to avoid calling 
	 * mono_class_get_virtual_methods () which is slow because of the metadata
	 * optimization.
	 */
	{
		gpointer iter = NULL;
		MonoMethod *cm;

		virt_methods = NULL;
		while ((cm = mono_class_get_virtual_methods (klass, &iter))) {
			virt_methods = g_slist_prepend (virt_methods, cm);
		}
		if (mono_class_has_failure (klass))
			goto fail;
	}
	
	// Loop on all implemented interfaces...
	for (i = 0; i < klass->interface_offsets_count; i++) {
		MonoClass *parent = klass->parent;
		int ic_offset;
		gboolean interface_is_explicitly_implemented_by_class;
		int im_index;
		
		ic = klass->interfaces_packed [i];
		ic_offset = mono_class_interface_offset (klass, ic);

		mono_class_setup_methods (ic);
		if (mono_class_has_failure (ic))
			goto fail;
		
		// Check if this interface is explicitly implemented (instead of just inherited)
		if (parent != NULL) {
			int implemented_interfaces_index;
			interface_is_explicitly_implemented_by_class = FALSE;
			for (implemented_interfaces_index = 0; implemented_interfaces_index < klass->interface_count; implemented_interfaces_index++) {
				if (ic == klass->interfaces [implemented_interfaces_index]) {
					interface_is_explicitly_implemented_by_class = TRUE;
					break;
				}
			}
		} else {
			interface_is_explicitly_implemented_by_class = TRUE;
		}
		
		// Loop on all interface methods...
		for (im_index = 0; im_index < ic->method.count; im_index++) {
			MonoMethod *im = ic->methods [im_index];
			int im_slot = ic_offset + im->slot;
			MonoMethod *override_im = (override_map != NULL) ? (MonoMethod *)g_hash_table_lookup (override_map, im) : NULL;
			
			if (im->flags & METHOD_ATTRIBUTE_STATIC)
				continue;

			TRACE_INTERFACE_VTABLE (printf ("\tchecking iface method %s\n", mono_method_full_name (im,1)));

			// If there is an explicit implementation, just use it right away,
			// otherwise look for a matching method
			if (override_im == NULL) {
				int cm_index;
				MonoMethod *cm;

				// First look for a suitable method among the class methods
				for (l = virt_methods; l; l = l->next) {
					cm = (MonoMethod *)l->data;
					TRACE_INTERFACE_VTABLE (printf ("    For slot %d ('%s'.'%s':'%s'), trying method '%s'.'%s':'%s'... [EXPLICIT IMPLEMENTATION = %d][SLOT IS NULL = %d]", im_slot, ic->name_space, ic->name, im->name, cm->klass->name_space, cm->klass->name, cm->name, interface_is_explicitly_implemented_by_class, (vtable [im_slot] == NULL)));
					if (check_interface_method_override (klass, im, cm, TRUE, interface_is_explicitly_implemented_by_class, (vtable [im_slot] == NULL))) {
						TRACE_INTERFACE_VTABLE (printf ("[check ok]: ASSIGNING"));
						vtable [im_slot] = cm;
						/* Why do we need this? */
						if (cm->slot < 0) {
							cm->slot = im_slot;
						}
					}
					TRACE_INTERFACE_VTABLE (printf ("\n"));
					if (mono_class_has_failure (klass))  /*Might be set by check_interface_method_override*/
						goto fail;
				}
				
				// If the slot is still empty, look in all the inherited virtual methods...
				if ((vtable [im_slot] == NULL) && klass->parent != NULL) {
					MonoClass *parent = klass->parent;
					// Reverse order, so that last added methods are preferred
					for (cm_index = parent->vtable_size - 1; cm_index >= 0; cm_index--) {
						MonoMethod *cm = parent->vtable [cm_index];
						
						TRACE_INTERFACE_VTABLE ((cm != NULL) && printf ("    For slot %d ('%s'.'%s':'%s'), trying (ancestor) method '%s'.'%s':'%s'... ", im_slot, ic->name_space, ic->name, im->name, cm->klass->name_space, cm->klass->name, cm->name));
						if ((cm != NULL) && check_interface_method_override (klass, im, cm, FALSE, FALSE, TRUE)) {
							TRACE_INTERFACE_VTABLE (printf ("[everything ok]: ASSIGNING"));
							vtable [im_slot] = cm;
							/* Why do we need this? */
							if (cm->slot < 0) {
								cm->slot = im_slot;
							}
							break;
						}
						if (mono_class_has_failure (klass)) /*Might be set by check_interface_method_override*/
							goto fail;
						TRACE_INTERFACE_VTABLE ((cm != NULL) && printf ("\n"));
					}
				}
			} else {
				g_assert (vtable [im_slot] == override_im);
			}
		}
	}
	
	// If the class is not abstract, check that all its interface slots are full.
	// The check is done here and not directly at the end of the loop above because
	// it can happen (for injected generic array interfaces) that the same slot is
	// processed multiple times (those interfaces have overlapping slots), and it
	// will not always be the first pass the one that fills the slot.
	if (! (klass->flags & TYPE_ATTRIBUTE_ABSTRACT)) {
		for (i = 0; i < klass->interface_offsets_count; i++) {
			int ic_offset;
			int im_index;
			
			ic = klass->interfaces_packed [i];
			ic_offset = mono_class_interface_offset (klass, ic);
			
			for (im_index = 0; im_index < ic->method.count; im_index++) {
				MonoMethod *im = ic->methods [im_index];
				int im_slot = ic_offset + im->slot;
				
				if (im->flags & METHOD_ATTRIBUTE_STATIC)
					continue;

				TRACE_INTERFACE_VTABLE (printf ("      [class is not abstract, checking slot %d for interface '%s'.'%s', method %s, slot check is %d]\n",
						im_slot, ic->name_space, ic->name, im->name, (vtable [im_slot] == NULL)));
				if (vtable [im_slot] == NULL) {
					print_unimplemented_interface_method_info (klass, ic, im, im_slot, overrides, onum);
					goto fail;
				}
			}
		}
	}

	TRACE_INTERFACE_VTABLE (print_vtable_full (klass, vtable, cur_slot, first_non_interface_slot, "AFTER SETTING UP INTERFACE METHODS", FALSE));
	for (l = virt_methods; l; l = l->next) {
		cm = (MonoMethod *)l->data;
		/*
		 * If the method is REUSE_SLOT, we must check in the
		 * base class for a method to override.
		 */
		if (!(cm->flags & METHOD_ATTRIBUTE_NEW_SLOT)) {
			int slot = -1;
			for (k = klass->parent; k ; k = k->parent) {
				gpointer k_iter;
				MonoMethod *m1;

				k_iter = NULL;
				while ((m1 = mono_class_get_virtual_methods (k, &k_iter))) {
					MonoMethodSignature *cmsig, *m1sig;

					cmsig = mono_method_signature (cm);
					m1sig = mono_method_signature (m1);

					if (!cmsig || !m1sig) {
						mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
						return;
					}

					if (!strcmp(cm->name, m1->name) && 
					    mono_metadata_signature_equal (cmsig, m1sig)) {

						if (mono_security_core_clr_enabled ())
							mono_security_core_clr_check_override (klass, cm, m1);

						slot = mono_method_get_vtable_slot (m1);
						if (slot == -1)
							goto fail;

						if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (cm, m1, NULL)) {
							char *body_name = mono_method_full_name (cm, TRUE);
							char *decl_name = mono_method_full_name (m1, TRUE);
							mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Method %s overrides method '%s' which is not accessible", body_name, decl_name));
							g_free (body_name);
							g_free (decl_name);
							goto fail;
						}

						g_assert (cm->slot < max_vtsize);
						if (!override_map)
							override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
						TRACE_INTERFACE_VTABLE (printf ("adding iface override from %s [%p] to %s [%p]\n",
							mono_method_full_name (m1, 1), m1,
							mono_method_full_name (cm, 1), cm));
						g_hash_table_insert (override_map, m1, cm);
						break;
					}
				}
				if (mono_class_has_failure (k))
					goto fail;
				
				if (slot >= 0) 
					break;
			}
			if (slot >= 0)
				cm->slot = slot;
		}

		/*Non final newslot methods must be given a non-interface vtable slot*/
		if ((cm->flags & METHOD_ATTRIBUTE_NEW_SLOT) && !(cm->flags & METHOD_ATTRIBUTE_FINAL) && cm->slot >= 0)
			cm->slot = -1;

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
			TRACE_INTERFACE_VTABLE (printf ("adding explicit override from %s [%p] to %s [%p]\n", 
				mono_method_full_name (decl, 1), decl,
				mono_method_full_name (overrides [i * 2 + 1], 1), overrides [i * 2 + 1]));
			g_hash_table_insert (override_map, decl, overrides [i * 2 + 1]);

			if (mono_security_core_clr_enabled ())
				mono_security_core_clr_check_override (klass, vtable [decl->slot], decl);
		}
	}

	/*
	 * If a method occupies more than one place in the vtable, and it is
	 * overriden, then change the other occurances too.
	 */
	if (override_map) {
		MonoMethod *cm;

		for (i = 0; i < max_vtsize; ++i)
			if (vtable [i]) {
				TRACE_INTERFACE_VTABLE (printf ("checking slot %d method %s[%p] for overrides\n", i, mono_method_full_name (vtable [i], 1), vtable [i]));

				cm = (MonoMethod *)g_hash_table_lookup (override_map, vtable [i]);
				if (cm)
					vtable [i] = cm;
			}

		g_hash_table_destroy (override_map);
		override_map = NULL;
	}

	g_slist_free (virt_methods);
	virt_methods = NULL;

	/* Ensure that all vtable slots are filled with concrete instance methods */
	if (!(klass->flags & TYPE_ATTRIBUTE_ABSTRACT)) {
		for (i = 0; i < cur_slot; ++i) {
			if (vtable [i] == NULL || (vtable [i]->flags & (METHOD_ATTRIBUTE_ABSTRACT | METHOD_ATTRIBUTE_STATIC))) {
				char *type_name = mono_type_get_full_name (klass);
				char *method_name = vtable [i] ? mono_method_full_name (vtable [i], TRUE) : g_strdup ("none");
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Type %s has invalid vtable method slot %d with method %s", type_name, i, method_name));
				g_free (type_name);
				g_free (method_name);
				return;
			}
		}
	}

	if (klass->generic_class) {
		MonoClass *gklass = klass->generic_class->container_class;

		mono_class_init (gklass);

		klass->vtable_size = MAX (gklass->vtable_size, cur_slot);
	} else {
		/* Check that the vtable_size value computed in mono_class_init () is correct */
		if (klass->vtable_size)
			g_assert (cur_slot == klass->vtable_size);
		klass->vtable_size = cur_slot;
	}

	/* Try to share the vtable with our parent. */
	if (klass->parent && (klass->parent->vtable_size == klass->vtable_size) && (memcmp (klass->parent->vtable, vtable, sizeof (gpointer) * klass->vtable_size) == 0)) {
		mono_memory_barrier ();
		klass->vtable = klass->parent->vtable;
	} else {
		MonoMethod **tmp = (MonoMethod **)mono_class_alloc0 (klass, sizeof (gpointer) * klass->vtable_size);
		memcpy (tmp, vtable,  sizeof (gpointer) * klass->vtable_size);
		mono_memory_barrier ();
		klass->vtable = tmp;
	}

	DEBUG_INTERFACE_VTABLE (print_vtable_full (klass, klass->vtable, klass->vtable_size, first_non_interface_slot, "FINALLY", FALSE));
	if (mono_print_vtable) {
		int icount = 0;

		print_implemented_interfaces (klass);
		
		for (i = 0; i <= max_iid; i++)
			if (MONO_CLASS_IMPLEMENTS_INTERFACE (klass, i))
				icount++;

		printf ("VTable %s (vtable entries = %d, interfaces = %d)\n", mono_type_full_name (&klass->byval_arg),
			klass->vtable_size, icount);

		for (i = 0; i < cur_slot; ++i) {
			MonoMethod *cm;
	       
			cm = vtable [i];
			if (cm) {
				printf ("  slot assigned: %03d, slot index: %03d %s\n", i, cm->slot,
					mono_method_full_name (cm, TRUE));
			}
		}


		if (icount) {
			printf ("Interfaces %s.%s (max_iid = %d)\n", klass->name_space,
				klass->name, max_iid);
	
			for (i = 0; i < klass->interface_count; i++) {
				ic = klass->interfaces [i];
				printf ("  slot offset: %03d, method count: %03d, iid: %03d %s\n",  
					mono_class_interface_offset (klass, ic),
					count_virtual_methods (ic), ic->interface_id, mono_type_full_name (&ic->byval_arg));
			}

			for (k = klass->parent; k ; k = k->parent) {
				for (i = 0; i < k->interface_count; i++) {
					ic = k->interfaces [i]; 
					printf ("  parent slot offset: %03d, method count: %03d, iid: %03d %s\n",  
						mono_class_interface_offset (klass, ic),
						count_virtual_methods (ic), ic->interface_id, mono_type_full_name (&ic->byval_arg));
				}
			}
		}
	}

	VERIFY_INTERFACE_VTABLE (mono_class_verify_vtable (klass));
	return;

fail:
	{
	char *name = mono_type_get_full_name (klass);
	mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("VTable setup of type %s failed", name));
	g_free (name);
	if (override_map)
		g_hash_table_destroy (override_map);
	if (virt_methods)
		g_slist_free (virt_methods);
	}
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
			int i;

			if (!method->klass->generic_class) {
				g_assert (method->is_inflated);
				return mono_method_get_vtable_slot (((MonoMethodInflated*)method)->declaring);
			}

			/* This can happen for abstract methods of generic instances due to the shortcut code in mono_class_setup_vtable_general (). */
			g_assert (method->klass->generic_class);
			gklass = method->klass->generic_class->container_class;
			mono_class_setup_methods (method->klass);
			g_assert (method->klass->methods);
			for (i = 0; i < method->klass->method.count; ++i) {
				if (method->klass->methods [i] == method)
					break;
			}
			g_assert (i < method->klass->method.count);
			g_assert (gklass->methods);
			method->slot = gklass->methods [i]->slot;
		}
		g_assert (method->slot != -1);
	}
	return method->slot;
}

/**
 * mono_method_get_vtable_index:
 * @method: a method
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

static MonoMethod *default_ghc = NULL;
static MonoMethod *default_finalize = NULL;
static int finalize_slot = -1;
static int ghc_slot = -1;

static void
initialize_object_slots (MonoClass *klass)
{
	int i;
	if (default_ghc)
		return;
	if (klass == mono_defaults.object_class) {
		mono_class_setup_vtable (klass);
		for (i = 0; i < klass->vtable_size; ++i) {
			MonoMethod *cm = klass->vtable [i];
       
			if (!strcmp (cm->name, "GetHashCode"))
				ghc_slot = i;
			else if (!strcmp (cm->name, "Finalize"))
				finalize_slot = i;
		}

		g_assert (ghc_slot > 0);
		default_ghc = klass->vtable [ghc_slot];

		g_assert (finalize_slot > 0);
		default_finalize = klass->vtable [finalize_slot];
	}
}

typedef struct {
	MonoMethod *array_method;
	char *name;
} GenericArrayMethodInfo;

static int generic_array_method_num = 0;
static GenericArrayMethodInfo *generic_array_method_info = NULL;

static int
generic_array_methods (MonoClass *klass)
{
	int i, count_generic = 0;
	GList *list = NULL, *tmp;
	if (generic_array_method_num)
		return generic_array_method_num;
	mono_class_setup_methods (klass->parent); /*This is setting up System.Array*/
	g_assert (!mono_class_has_failure (klass->parent)); /*So hitting this assert is a huge problem*/
	for (i = 0; i < klass->parent->method.count; i++) {
		MonoMethod *m = klass->parent->methods [i];
		if (!strncmp (m->name, "InternalArray__", 15)) {
			count_generic++;
			list = g_list_prepend (list, m);
		}
	}
	list = g_list_reverse (list);
	generic_array_method_info = (GenericArrayMethodInfo *)mono_image_alloc (mono_defaults.corlib, sizeof (GenericArrayMethodInfo) * count_generic);
	i = 0;
	for (tmp = list; tmp; tmp = tmp->next) {
		const char *mname, *iname;
		gchar *name;
		MonoMethod *m = (MonoMethod *)tmp->data;
		const char *ireadonlylist_prefix = "InternalArray__IReadOnlyList_";
		const char *ireadonlycollection_prefix = "InternalArray__IReadOnlyCollection_";

		generic_array_method_info [i].array_method = m;
		if (!strncmp (m->name, "InternalArray__ICollection_", 27)) {
			iname = "System.Collections.Generic.ICollection`1.";
			mname = m->name + 27;
		} else if (!strncmp (m->name, "InternalArray__IEnumerable_", 27)) {
			iname = "System.Collections.Generic.IEnumerable`1.";
			mname = m->name + 27;
		} else if (!strncmp (m->name, ireadonlylist_prefix, strlen (ireadonlylist_prefix))) {
			iname = "System.Collections.Generic.IReadOnlyList`1.";
			mname = m->name + strlen (ireadonlylist_prefix);
		} else if (!strncmp (m->name, ireadonlycollection_prefix, strlen (ireadonlycollection_prefix))) {
			iname = "System.Collections.Generic.IReadOnlyCollection`1.";
			mname = m->name + strlen (ireadonlycollection_prefix);
		} else if (!strncmp (m->name, "InternalArray__", 15)) {
			iname = "System.Collections.Generic.IList`1.";
			mname = m->name + 15;
		} else {
			g_assert_not_reached ();
		}

		name = (gchar *)mono_image_alloc (mono_defaults.corlib, strlen (iname) + strlen (mname) + 1);
		strcpy (name, iname);
		strcpy (name + strlen (iname), mname);
		generic_array_method_info [i].name = name;
		i++;
	}
	/*g_print ("array generic methods: %d\n", count_generic);*/

	generic_array_method_num = count_generic;
	g_list_free (list);
	return generic_array_method_num;
}

static void
setup_generic_array_ifaces (MonoClass *klass, MonoClass *iface, MonoMethod **methods, int pos)
{
	MonoGenericContext tmp_context;
	int i;

	tmp_context.class_inst = NULL;
	tmp_context.method_inst = iface->generic_class->context.class_inst;
	//g_print ("setting up array interface: %s\n", mono_type_get_name_full (&iface->byval_arg, 0));

	for (i = 0; i < generic_array_method_num; i++) {
		MonoError error;
		MonoMethod *m = generic_array_method_info [i].array_method;
		MonoMethod *inflated;

		inflated = mono_class_inflate_generic_method_checked (m, &tmp_context, &error);
		g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/
		methods [pos++] = mono_marshal_get_generic_array_helper (klass, iface, generic_array_method_info [i].name, inflated);
	}
}

static char*
concat_two_strings_with_zero (MonoImage *image, const char *s1, const char *s2)
{
	int null_length = strlen ("(null)");
	int len = (s1 ? strlen (s1) : null_length) + (s2 ? strlen (s2) : null_length) + 2;
	char *s = (char *)mono_image_alloc (image, len);
	int result;

	result = g_snprintf (s, len, "%s%c%s", s1 ? s1 : "(null)", '\0', s2 ? s2 : "(null)");
	g_assert (result == len - 1);

	return s;
}

static void
set_failure_from_loader_error (MonoClass *klass, MonoLoaderError *error)
{
	gpointer exception_data = NULL;

	switch (error->exception_type) {
	case MONO_EXCEPTION_TYPE_LOAD:
		exception_data = concat_two_strings_with_zero (klass->image, error->class_name, error->assembly_name);
		break;

	case MONO_EXCEPTION_MISSING_METHOD:
		exception_data = concat_two_strings_with_zero (klass->image, error->class_name, error->member_name);
		break;

	case MONO_EXCEPTION_MISSING_FIELD: {
		const char *name_space = error->klass->name_space ? error->klass->name_space : NULL;
		const char *class_name;

		if (name_space)
			class_name = g_strdup_printf ("%s.%s", name_space, error->klass->name);
		else
			class_name = error->klass->name;

		exception_data = concat_two_strings_with_zero (klass->image, class_name, error->member_name);
		
		if (name_space)
			g_free ((void*)class_name);
		break;
	}

	case MONO_EXCEPTION_FILE_NOT_FOUND: {
		const char *msg;

		if (error->ref_only)
			msg = "Cannot resolve dependency to assembly '%s' because it has not been preloaded. When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.";
		else
			msg = "Could not load file or assembly '%s' or one of its dependencies.";

		exception_data = concat_two_strings_with_zero (klass->image, msg, error->assembly_name);
		break;
	}

	case MONO_EXCEPTION_BAD_IMAGE:
		exception_data = error->msg;
		break;

	default :
		g_assert_not_reached ();
	}

	mono_class_set_failure (klass, error->exception_type, exception_data);
}

/**
 * mono_class_init:
 * @class: the class to initialize
 *
 *   Compute the instance_size, class_size and other infos that cannot be 
 * computed at mono_class_get() time. Also compute vtable_size if possible. 
 * Returns TRUE on success or FALSE if there was a problem in loading
 * the type (incorrect assemblies, missing assemblies, methods, etc). 
 *
 * LOCKING: Acquires the loader lock.
 */
gboolean
mono_class_init (MonoClass *klass)
{
	int i;
	MonoCachedClassInfo cached_info;
	gboolean has_cached_info;
	
	g_assert (klass);

	/* Double-checking locking pattern */
	if (klass->inited || mono_class_has_failure (klass))
		return !mono_class_has_failure (klass);

	/*g_print ("Init class %s\n", mono_type_get_full_name (klass));*/

	/* We do everything inside the lock to prevent races */
	mono_loader_lock ();

	if (klass->inited || mono_class_has_failure (klass)) {
		mono_loader_unlock ();
		/* Somebody might have gotten in before us */
		return !mono_class_has_failure (klass);
	}

	if (klass->init_pending) {
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Recursive type definition detected"));
		goto leave;
	}

	klass->init_pending = 1;

	if (mono_verifier_is_enabled_for_class (klass) && !mono_verifier_verify_class (klass)) {
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, concat_two_strings_with_zero (klass->image, klass->name, klass->image->assembly_name));
		goto leave;
	}


	if (klass->byval_arg.type == MONO_TYPE_ARRAY || klass->byval_arg.type == MONO_TYPE_SZARRAY) {
		MonoClass *element_class = klass->element_class;
		if (!element_class->inited) 
			mono_class_init (element_class);
		if (mono_class_has_failure (element_class)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			goto leave;
		}
	}

	mono_stats.initialized_class_count++;

	if (klass->generic_class && !klass->generic_class->is_dynamic) {
		MonoClass *gklass = klass->generic_class->container_class;

		mono_stats.generic_class_count++;

		klass->method = gklass->method;
		klass->field = gklass->field;

		mono_class_init (gklass);
		// FIXME: Why is this needed ?
		if (!mono_class_has_failure (gklass))
			mono_class_setup_methods (gklass);
		if (mono_class_has_failure (gklass)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Generic Type Defintion failed to init"));
			goto leave;
		}

		if (MONO_CLASS_IS_INTERFACE (klass))
			klass->interface_id = mono_get_unique_iid (klass);
	}

	if (klass->parent && !klass->parent->inited)
		mono_class_init (klass->parent);

	has_cached_info = mono_class_get_cached_class_info (klass, &cached_info);

	if (klass->generic_class || image_is_dynamic (klass->image) || !klass->type_token || (has_cached_info && !cached_info.has_nested_classes))
		klass->nested_classes_inited = TRUE;

	/*
	 * Computes the size used by the fields, and their locations
	 */
	if (has_cached_info) {
		klass->instance_size = cached_info.instance_size;
		klass->sizes.class_size = cached_info.class_size;
		klass->packing_size = cached_info.packing_size;
		klass->min_align = cached_info.min_align;
		klass->blittable = cached_info.blittable;
		klass->has_references = cached_info.has_references;
		klass->has_static_refs = cached_info.has_static_refs;
		klass->no_special_static_fields = cached_info.no_special_static_fields;
	}
	else
		if (!klass->size_inited){
			mono_class_setup_fields (klass);
			if (mono_class_has_failure (klass) || mono_loader_get_last_error ())
				goto leave;
		}
				
	/* Initialize arrays */
	if (klass->rank) {
		klass->method.count = 3 + (klass->rank > 1? 2: 1);

		if (klass->interface_count) {
			int count_generic = generic_array_methods (klass);
			klass->method.count += klass->interface_count * count_generic;
		}
	}

	mono_class_setup_supertypes (klass);

	if (!default_ghc)
		initialize_object_slots (klass);

	/* 
	 * Initialize the rest of the data without creating a generic vtable if possible.
	 * If possible, also compute vtable_size, so mono_class_create_runtime_vtable () can
	 * also avoid computing a generic vtable.
	 */
	if (has_cached_info) {
		/* AOT case */
		klass->vtable_size = cached_info.vtable_size;
		klass->has_finalize = cached_info.has_finalize;
		klass->has_finalize_inited = TRUE;
		klass->ghcimpl = cached_info.ghcimpl;
		klass->has_cctor = cached_info.has_cctor;
	} else if (klass->rank == 1 && klass->byval_arg.type == MONO_TYPE_SZARRAY) {
		/* SZARRAY can have 2 vtable layouts, with and without the stelemref method.
		 * The first slot if for array with.
		 */
		static int szarray_vtable_size[2] = { 0 };

		int slot = MONO_TYPE_IS_REFERENCE (&klass->element_class->byval_arg) ? 0 : 1;

		/* SZARRAY case */
		if (!szarray_vtable_size [slot]) {
			mono_class_setup_vtable (klass);
			szarray_vtable_size [slot] = klass->vtable_size;
		} else {
			klass->vtable_size = szarray_vtable_size[slot];
		}
	} else if (klass->generic_class && !MONO_CLASS_IS_INTERFACE (klass)) {
		MonoClass *gklass = klass->generic_class->container_class;

		/* Generic instance case */
		klass->ghcimpl = gklass->ghcimpl;
		klass->has_cctor = gklass->has_cctor;

		mono_class_setup_vtable (gklass);
		if (mono_class_has_failure (gklass)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			goto leave;
		}

		klass->vtable_size = gklass->vtable_size;
	} else {
		/* General case */

		/* ghcimpl is not currently used
		klass->ghcimpl = 1;
		if (klass->parent) {
			MonoMethod *cmethod = klass->vtable [ghc_slot];
			if (cmethod->is_inflated)
				cmethod = ((MonoMethodInflated*)cmethod)->declaring;
			if (cmethod == default_ghc) {
				klass->ghcimpl = 0;
			}
		}
		*/

		/* C# doesn't allow interfaces to have cctors */
		if (!MONO_CLASS_IS_INTERFACE (klass) || klass->image != mono_defaults.corlib) {
			MonoMethod *cmethod = NULL;

			if (klass->type_token && !image_is_dynamic(klass->image)) {
				cmethod = find_method_in_metadata (klass, ".cctor", 0, METHOD_ATTRIBUTE_SPECIAL_NAME);
				/* The find_method function ignores the 'flags' argument */
				if (cmethod && (cmethod->flags & METHOD_ATTRIBUTE_SPECIAL_NAME))
					klass->has_cctor = 1;
			} else {
				mono_class_setup_methods (klass);
				if (mono_class_has_failure (klass))
					goto leave;

				for (i = 0; i < klass->method.count; ++i) {
					MonoMethod *method = klass->methods [i];
					if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
						(strcmp (".cctor", method->name) == 0)) {
						klass->has_cctor = 1;
						break;
					}
				}
			}
		}
	}

	if (klass->parent) {
		int first_iface_slot;
		/* This will compute klass->parent->vtable_size for some classes */
		mono_class_init (klass->parent);
		if (mono_class_has_failure (klass->parent)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			goto leave;
		}
		if (mono_loader_get_last_error ())
			goto leave;
		if (!klass->parent->vtable_size) {
			/* FIXME: Get rid of this somehow */
			mono_class_setup_vtable (klass->parent);
			if (mono_class_has_failure (klass->parent)) {
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
				goto leave;
			}
			if (mono_loader_get_last_error ())
				goto leave;
		}
		first_iface_slot = klass->parent->vtable_size;
		if (mono_class_need_stelemref_method (klass))
			++first_iface_slot;
		setup_interface_offsets (klass, first_iface_slot, TRUE);
	} else {
		setup_interface_offsets (klass, 0, TRUE);
	}

	if (mono_security_core_clr_enabled ())
		mono_security_core_clr_check_inheritance (klass);

	if (mono_loader_get_last_error ()) {
		if (!mono_class_has_failure (klass)) {
			set_failure_from_loader_error (klass, mono_loader_get_last_error ());
		}
		mono_loader_clear_error ();
	}

	if (klass->generic_class && !mono_verifier_class_is_valid_generic_instantiation (klass))
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Invalid generic instantiation"));

	goto leave;

 leave:
	/* Because of the double-checking locking pattern */
	mono_memory_barrier ();
	klass->inited = 1;
	klass->init_pending = 0;

	mono_loader_unlock ();

	return !mono_class_has_failure (klass);
}

/*
 * mono_class_has_finalizer:
 *
 *   Return whenever KLASS has a finalizer, initializing klass->has_finalizer in the
 * process.
 */
gboolean
mono_class_has_finalizer (MonoClass *klass)
{
	gboolean has_finalize = FALSE;

	if (klass->has_finalize_inited)
		return klass->has_finalize;

	/* Interfaces and valuetypes are not supposed to have finalizers */
	if (!(MONO_CLASS_IS_INTERFACE (klass) || klass->valuetype)) {
		MonoMethod *cmethod = NULL;

		if (klass->rank == 1 && klass->byval_arg.type == MONO_TYPE_SZARRAY) {
		} else if (klass->generic_class) {
			MonoClass *gklass = klass->generic_class->container_class;

			has_finalize = mono_class_has_finalizer (gklass);
		} else if (klass->parent && klass->parent->has_finalize) {
			has_finalize = TRUE;
		} else {
			if (klass->parent) {
				/*
				 * Can't search in metadata for a method named Finalize, because that
				 * ignores overrides.
				 */
				mono_class_setup_vtable (klass);
				if (mono_class_has_failure (klass) || mono_loader_get_last_error ())
					cmethod = NULL;
				else
					cmethod = klass->vtable [finalize_slot];
			}

			if (cmethod) {
				g_assert (klass->vtable_size > finalize_slot);

				if (klass->parent) {
					if (cmethod->is_inflated)
						cmethod = ((MonoMethodInflated*)cmethod)->declaring;
					if (cmethod != default_finalize)
						has_finalize = TRUE;
				}
			}
		}
	}

	mono_image_lock (klass->image);

	if (!klass->has_finalize_inited) {
		klass->has_finalize = has_finalize ? 1 : 0;

		mono_memory_barrier ();
		klass->has_finalize_inited = TRUE;
	}

	mono_image_unlock (klass->image);

	return klass->has_finalize;
}

gboolean
mono_is_corlib_image (MonoImage *image)
{
	/* FIXME: allow the dynamic case for our compilers and with full trust */
	if (image_is_dynamic (image))
		return image->assembly && !strcmp (image->assembly->aname.name, "mscorlib");
	else
		return image == mono_defaults.corlib;
}

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_mono_type (MonoClass *klass)
{
	const char *name = klass->name;
	const char *nspace = klass->name_space;
	gboolean is_corlib = mono_is_corlib_image (klass->image);

	klass->this_arg.byref = 1;
	klass->this_arg.data.klass = klass;
	klass->this_arg.type = MONO_TYPE_CLASS;
	klass->byval_arg.data.klass = klass;
	klass->byval_arg.type = MONO_TYPE_CLASS;

	if (is_corlib && !strcmp (nspace, "System")) {
		if (!strcmp (name, "ValueType")) {
			/*
			 * do not set the valuetype bit for System.ValueType.
			 * klass->valuetype = 1;
			 */
			klass->blittable = TRUE;
		} else if (!strcmp (name, "Enum")) {
			/*
			 * do not set the valuetype bit for System.Enum.
			 * klass->valuetype = 1;
			 */
			klass->valuetype = 0;
			klass->enumtype = 0;
		} else if (!strcmp (name, "Object")) {
			klass->byval_arg.type = MONO_TYPE_OBJECT;
			klass->this_arg.type = MONO_TYPE_OBJECT;
		} else if (!strcmp (name, "String")) {
			klass->byval_arg.type = MONO_TYPE_STRING;
			klass->this_arg.type = MONO_TYPE_STRING;
		} else if (!strcmp (name, "TypedReference")) {
			klass->byval_arg.type = MONO_TYPE_TYPEDBYREF;
			klass->this_arg.type = MONO_TYPE_TYPEDBYREF;
		}
	}

	if (klass->valuetype) {
		int t = MONO_TYPE_VALUETYPE;

		if (is_corlib && !strcmp (nspace, "System")) {
			switch (*name) {
			case 'B':
				if (!strcmp (name, "Boolean")) {
					t = MONO_TYPE_BOOLEAN;
				} else if (!strcmp(name, "Byte")) {
					t = MONO_TYPE_U1;
					klass->blittable = TRUE;						
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
					klass->blittable = TRUE;						
				}
				break;
			case 'I':
				if (!strcmp (name, "Int32")) {
					t = MONO_TYPE_I4;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "Int16")) {
					t = MONO_TYPE_I2;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "Int64")) {
					t = MONO_TYPE_I8;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "IntPtr")) {
					t = MONO_TYPE_I;
					klass->blittable = TRUE;
				}
				break;
			case 'S':
				if (!strcmp (name, "Single")) {
					t = MONO_TYPE_R4;
					klass->blittable = TRUE;						
				} else if (!strcmp(name, "SByte")) {
					t = MONO_TYPE_I1;
					klass->blittable = TRUE;
				}
				break;
			case 'U':
				if (!strcmp (name, "UInt32")) {
					t = MONO_TYPE_U4;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "UInt16")) {
					t = MONO_TYPE_U2;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "UInt64")) {
					t = MONO_TYPE_U8;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "UIntPtr")) {
					t = MONO_TYPE_U;
					klass->blittable = TRUE;
				}
				break;
			case 'T':
				if (!strcmp (name, "TypedReference")) {
					t = MONO_TYPE_TYPEDBYREF;
					klass->blittable = TRUE;
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
		klass->byval_arg.type = (MonoTypeEnum)t;
		klass->this_arg.type = (MonoTypeEnum)t;
	}

	if (MONO_CLASS_IS_INTERFACE (klass))
		klass->interface_id = mono_get_unique_iid (klass);

}

#ifndef DISABLE_COM
/*
 * COM initialization is delayed until needed.
 * However when a [ComImport] attribute is present on a type it will trigger
 * the initialization. This is not a problem unless the BCL being executed 
 * lacks the types that COM depends on (e.g. Variant on Silverlight).
 */
static void
init_com_from_comimport (MonoClass *klass)
{
	/* we don't always allow COM initialization under the CoreCLR (e.g. Moonlight does not require it) */
	if (mono_security_core_clr_enabled ()) {
		/* but some other CoreCLR user could requires it for their platform (i.e. trusted) code */
		if (!mono_security_core_clr_determine_platform_image (klass->image)) {
			/* but it can not be made available for application (i.e. user code) since all COM calls
			 * are considered native calls. In this case we fail with a TypeLoadException (just like
			 * Silverlight 2 does */
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			return;
		}
	}

	/* FIXME : we should add an extra checks to ensure COM can be initialized properly before continuing */
}
#endif /*DISABLE_COM*/

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_parent (MonoClass *klass, MonoClass *parent)
{
	gboolean system_namespace;
	gboolean is_corlib = mono_is_corlib_image (klass->image);

	system_namespace = !strcmp (klass->name_space, "System") && is_corlib;

	/* if root of the hierarchy */
	if (system_namespace && !strcmp (klass->name, "Object")) {
		klass->parent = NULL;
		klass->instance_size = sizeof (MonoObject);
		return;
	}
	if (!strcmp (klass->name, "<Module>")) {
		klass->parent = NULL;
		klass->instance_size = 0;
		return;
	}

	if (!MONO_CLASS_IS_INTERFACE (klass)) {
		/* Imported COM Objects always derive from __ComObject. */
#ifndef DISABLE_COM
		if (MONO_CLASS_IS_IMPORT (klass)) {
			init_com_from_comimport (klass);
			if (parent == mono_defaults.object_class)
				parent = mono_class_get_com_object_class ();
		}
#endif
		if (!parent) {
			/* set the parent to something useful and safe, but mark the type as broken */
			parent = mono_defaults.object_class;
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
		}

		klass->parent = parent;

		if (parent->generic_class && !parent->name) {
			/*
			 * If the parent is a generic instance, we may get
			 * called before it is fully initialized, especially
			 * before it has its name.
			 */
			return;
		}

#ifndef DISABLE_REMOTING
		klass->marshalbyref = parent->marshalbyref;
		klass->contextbound  = parent->contextbound;
#endif

		klass->delegate  = parent->delegate;

		if (MONO_CLASS_IS_IMPORT (klass) || mono_class_is_com_object (parent))
			mono_class_set_is_com_object (klass);
		
		if (system_namespace) {
#ifndef DISABLE_REMOTING
			if (klass->name [0] == 'M' && !strcmp (klass->name, "MarshalByRefObject"))
				klass->marshalbyref = 1;

			if (klass->name [0] == 'C' && !strcmp (klass->name, "ContextBoundObject")) 
				klass->contextbound  = 1;
#endif
			if (klass->name [0] == 'D' && !strcmp (klass->name, "Delegate")) 
				klass->delegate  = 1;
		}

		if (klass->parent->enumtype || (mono_is_corlib_image (klass->parent->image) && (strcmp (klass->parent->name, "ValueType") == 0) && 
						(strcmp (klass->parent->name_space, "System") == 0)))
			klass->valuetype = 1;
		if (mono_is_corlib_image (klass->parent->image) && ((strcmp (klass->parent->name, "Enum") == 0) && (strcmp (klass->parent->name_space, "System") == 0))) {
			klass->valuetype = klass->enumtype = 1;
		}
		/*klass->enumtype = klass->parent->enumtype; */
	} else {
		/* initialize com types if COM interfaces are present */
#ifndef DISABLE_COM
		if (MONO_CLASS_IS_IMPORT (klass))
			init_com_from_comimport (klass);
#endif
		klass->parent = NULL;
	}

}

/*
 * mono_class_setup_supertypes:
 * @class: a class
 *
 * Build the data structure needed to make fast type checks work.
 * This currently sets two fields in @class:
 *  - idepth: distance between @class and System.Object in the type
 *    hierarchy + 1
 *  - supertypes: array of classes: each element has a class in the hierarchy
 *    starting from @class up to System.Object
 * 
 * LOCKING: This function is atomic, in case of contention we waste memory.
 */
void
mono_class_setup_supertypes (MonoClass *klass)
{
	int ms;
	MonoClass **supertypes;

	mono_atomic_load_acquire (supertypes, MonoClass **, &klass->supertypes);
	if (supertypes)
		return;

	if (klass->parent && !klass->parent->supertypes)
		mono_class_setup_supertypes (klass->parent);
	if (klass->parent)
		klass->idepth = klass->parent->idepth + 1;
	else
		klass->idepth = 1;

	ms = MAX (MONO_DEFAULT_SUPERTABLE_SIZE, klass->idepth);
	supertypes = (MonoClass **)mono_class_alloc0 (klass, sizeof (MonoClass *) * ms);

	if (klass->parent) {
		CHECKED_METADATA_WRITE_PTR ( supertypes [klass->idepth - 1] , klass );

		int supertype_idx;
		for (supertype_idx = 0; supertype_idx < klass->parent->idepth; supertype_idx++)
			CHECKED_METADATA_WRITE_PTR ( supertypes [supertype_idx] , klass->parent->supertypes [supertype_idx] );
	} else {
		CHECKED_METADATA_WRITE_PTR ( supertypes [0] , klass );
	}

	CHECKED_METADATA_WRITE_PTR_ATOMIC ( klass->supertypes , supertypes );
}

static gboolean
fix_gclass_incomplete_instantiation (MonoClass *gclass, void *user_data)
{
	MonoClass *gtd = (MonoClass*)user_data;
	/* Only try to fix generic instances of @gtd */
	if (gclass->generic_class->container_class != gtd)
		return FALSE;

	/* Check if the generic instance has no parent. */
	if (gtd->parent && !gclass->parent)
		mono_generic_class_setup_parent (gclass, gtd);

	return TRUE;
}

static void
mono_class_set_failure_and_error (MonoClass *klass, MonoError *error, const char *msg)
{
	mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup (msg));
	mono_error_set_type_load_class (error, klass, msg);
}

static void
mono_class_set_failure_from_loader_error (MonoClass *klass, MonoError *error, char *msg)
{
	MonoLoaderError *lerror = mono_loader_get_last_error ();

	if (lerror) {
		set_failure_from_loader_error (klass, lerror);
		mono_error_set_from_loader_error (error);
		if (msg)
			g_free (msg);
	} else {
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, msg);
		mono_error_set_type_load_class (error, klass, msg);
	}
}

/**
 * mono_class_create_from_typedef:
 * @image: image where the token is valid
 * @type_token:  typedef token
 * @error:  used to return any error found while creating the type
 *
 * Create the MonoClass* representing the specified type token.
 * @type_token must be a TypeDef token.
 *
 * FIXME: don't return NULL on failure, just the the caller figure it out.
 */
static MonoClass *
mono_class_create_from_typedef (MonoImage *image, guint32 type_token, MonoError *error)
{
	MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];
	MonoClass *klass, *parent = NULL;
	guint32 cols [MONO_TYPEDEF_SIZE];
	guint32 cols_next [MONO_TYPEDEF_SIZE];
	guint tidx = mono_metadata_token_index (type_token);
	MonoGenericContext *context = NULL;
	const char *name, *nspace;
	guint icount = 0; 
	MonoClass **interfaces;
	guint32 field_last, method_last;
	guint32 nesting_tokeen;

	mono_error_init (error);

	if (mono_metadata_token_table (type_token) != MONO_TABLE_TYPEDEF || tidx > tt->rows) {
		mono_error_set_bad_image (error, image, "Invalid typedef token %x", type_token);
		mono_loader_assert_no_error ();
		return NULL;
	}

	mono_loader_lock ();

	if ((klass = (MonoClass *)mono_internal_hash_table_lookup (&image->class_cache, GUINT_TO_POINTER (type_token)))) {
		mono_loader_unlock ();
		mono_loader_assert_no_error ();
		return klass;
	}

	mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);
	
	name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
	nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);

	klass = (MonoClass *)mono_image_alloc0 (image, sizeof (MonoClass));

	klass->name = name;
	klass->name_space = nspace;

	mono_profiler_class_event (klass, MONO_PROFILE_START_LOAD);

	klass->image = image;
	klass->type_token = type_token;
	klass->flags = cols [MONO_TYPEDEF_FLAGS];

	mono_internal_hash_table_insert (&image->class_cache, GUINT_TO_POINTER (type_token), klass);

	classes_size += sizeof (MonoClass);

	/*
	 * Check whether we're a generic type definition.
	 */
	klass->generic_container = mono_metadata_load_generic_params (image, klass->type_token, NULL);
	if (klass->generic_container) {
		klass->is_generic = 1;
		klass->generic_container->owner.klass = klass;
		klass->generic_container->is_anonymous = FALSE; // Owner class is now known, container is no longer anonymous
		context = &klass->generic_container->context;
	}

	if (klass->generic_container)
		enable_gclass_recording ();

	if (cols [MONO_TYPEDEF_EXTENDS]) {
		MonoClass *tmp;
		guint32 parent_token = mono_metadata_token_from_dor (cols [MONO_TYPEDEF_EXTENDS]);

		if (mono_metadata_token_table (parent_token) == MONO_TABLE_TYPESPEC) {
			/*WARNING: this must satisfy mono_metadata_type_hash*/
			klass->this_arg.byref = 1;
			klass->this_arg.data.klass = klass;
			klass->this_arg.type = MONO_TYPE_CLASS;
			klass->byval_arg.data.klass = klass;
			klass->byval_arg.type = MONO_TYPE_CLASS;
		}
		parent = mono_class_get_checked (image, parent_token, error);
		if (parent && context) /* Always inflate */
			parent = mono_class_inflate_generic_class_checked (parent, context, error);

		if (parent == NULL) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup (mono_error_get_message (error)));
			goto parent_failure;
		}

		for (tmp = parent; tmp; tmp = tmp->parent) {
			if (tmp == klass) {
				mono_class_set_failure_and_error (klass, error, "Cycle found while resolving parent");
				goto parent_failure;
			}
			if (klass->generic_container && tmp->generic_class && tmp->generic_class->container_class == klass) {
				mono_class_set_failure_and_error (klass, error, "Parent extends generic instance of this type");
				goto parent_failure;
			}
		}
	}

	mono_class_setup_parent (klass, parent);

	/* uses ->valuetype, which is initialized by mono_class_setup_parent above */
	mono_class_setup_mono_type (klass);

	if (klass->generic_container)
		disable_gclass_recording (fix_gclass_incomplete_instantiation, klass);

	/* 
	 * This might access klass->byval_arg for recursion generated by generic constraints,
	 * so it has to come after setup_mono_type ().
	 */
	if ((nesting_tokeen = mono_metadata_nested_in_typedef (image, type_token))) {
		klass->nested_in = mono_class_create_from_typedef (image, nesting_tokeen, error);
		if (!mono_error_ok (error)) {
			/*FIXME implement a mono_class_set_failure_from_mono_error */
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup (mono_error_get_message (error)));
			mono_loader_unlock ();
			mono_profiler_class_loaded (klass, MONO_PROFILE_FAILED);
			mono_loader_assert_no_error ();
			return NULL;
		}
	}

	if ((klass->flags & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_UNICODE_CLASS)
		klass->unicode = 1;

#ifdef HOST_WIN32
	if ((klass->flags & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_AUTO_CLASS)
		klass->unicode = 1;
#endif

	klass->cast_class = klass->element_class = klass;

	if (!klass->enumtype) {
		if (!mono_metadata_interfaces_from_typedef_full (
			    image, type_token, &interfaces, &icount, FALSE, context, error)){

			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup (mono_error_get_message (error)));
			mono_loader_unlock ();
			mono_profiler_class_loaded (klass, MONO_PROFILE_FAILED);
			return NULL;
		}

		klass->interfaces = interfaces;
		klass->interface_count = icount;
		klass->interfaces_inited = 1;
	}

	/*g_print ("Load class %s\n", name);*/

	/*
	 * Compute the field and method lists
	 */
	klass->field.first  = cols [MONO_TYPEDEF_FIELD_LIST] - 1;
	klass->method.first = cols [MONO_TYPEDEF_METHOD_LIST] - 1;

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
		klass->field.count = field_last - klass->field.first;
	else
		klass->field.count = 0;

	if (cols [MONO_TYPEDEF_METHOD_LIST] <= image->tables [MONO_TABLE_METHOD].rows)
		klass->method.count = method_last - klass->method.first;
	else
		klass->method.count = 0;

	/* reserve space to store vector pointer in arrays */
	if (mono_is_corlib_image (image) && !strcmp (nspace, "System") && !strcmp (name, "Array")) {
		klass->instance_size += 2 * sizeof (gpointer);
		g_assert (klass->field.count == 0);
	}

	if (klass->enumtype) {
		MonoType *enum_basetype = mono_class_find_enum_basetype (klass, error);
		if (!enum_basetype) {
			/*set it to a default value as the whole runtime can't handle this to be null*/
			klass->cast_class = klass->element_class = mono_defaults.int32_class;
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup (mono_error_get_message (error)));
			mono_loader_unlock ();
			mono_profiler_class_loaded (klass, MONO_PROFILE_FAILED);
			mono_loader_assert_no_error ();
			return NULL;
		}
		klass->cast_class = klass->element_class = mono_class_from_mono_type (enum_basetype);
	}

	/*
	 * If we're a generic type definition, load the constraints.
	 * We must do this after the class has been constructed to make certain recursive scenarios
	 * work.
	 */
	if (klass->generic_container && !mono_metadata_load_generic_param_constraints_checked (image, type_token, klass->generic_container, error)) {
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup_printf ("Could not load generic parameter constrains due to %s", mono_error_get_message (error)));
		mono_loader_unlock ();
		mono_profiler_class_loaded (klass, MONO_PROFILE_FAILED);
		mono_loader_assert_no_error ();
		return NULL;
	}

	if (klass->image->assembly_name && !strcmp (klass->image->assembly_name, "Mono.Simd") && !strcmp (nspace, "Mono.Simd")) {
		if (!strncmp (name, "Vector", 6))
			klass->simd_type = !strcmp (name + 6, "2d") || !strcmp (name + 6, "2ul") || !strcmp (name + 6, "2l") || !strcmp (name + 6, "4f") || !strcmp (name + 6, "4ui") || !strcmp (name + 6, "4i") || !strcmp (name + 6, "8s") || !strcmp (name + 6, "8us") || !strcmp (name + 6, "16b") || !strcmp (name + 6, "16sb");
	}

	mono_loader_unlock ();

	mono_profiler_class_loaded (klass, MONO_PROFILE_OK);
	mono_loader_assert_no_error ();

	return klass;

parent_failure:
	mono_class_setup_mono_type (klass);
	mono_loader_unlock ();
	mono_profiler_class_loaded (klass, MONO_PROFILE_FAILED);
	mono_loader_assert_no_error ();
	return NULL;
}

/** Is klass a Nullable<T> ginst? */
gboolean
mono_class_is_nullable (MonoClass *klass)
{
       return klass->generic_class != NULL &&
               klass->generic_class->container_class == mono_defaults.generic_nullable_class;
}


/** if klass is T? return T */
MonoClass*
mono_class_get_nullable_param (MonoClass *klass)
{
       g_assert (mono_class_is_nullable (klass));
       return mono_class_from_mono_type (klass->generic_class->context.class_inst->type_argv [0]);
}

static void
mono_generic_class_setup_parent (MonoClass *klass, MonoClass *gtd)
{
	if (gtd->parent) {
		MonoError error;
		MonoGenericClass *gclass = klass->generic_class;

		klass->parent = mono_class_inflate_generic_class_checked (gtd->parent, mono_generic_class_get_context (gclass), &error);
		if (!mono_error_ok (&error)) {
			/*Set parent to something safe as the runtime doesn't handle well this kind of failure.*/
			klass->parent = mono_defaults.object_class;
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			mono_error_cleanup (&error);
		}
	}
	if (klass->parent)
		mono_class_setup_parent (klass, klass->parent);

	if (klass->enumtype) {
		klass->cast_class = gtd->cast_class;
		klass->element_class = gtd->element_class;
	}
}


/*
 * Create the `MonoClass' for an instantiation of a generic type.
 * We only do this if we actually need it.
 */
MonoClass*
mono_generic_class_get_class (MonoGenericClass *gclass)
{
	MonoClass *klass, *gklass;

	if (gclass->cached_class)
		return gclass->cached_class;

	mono_loader_lock ();
	if (gclass->cached_class) {
		mono_loader_unlock ();
		return gclass->cached_class;
	}

	klass = (MonoClass *)mono_image_set_alloc0 (gclass->owner, sizeof (MonoClass));

	gklass = gclass->container_class;

	if (record_gclass_instantiation > 0)
		gclass_recorded_list = g_slist_append (gclass_recorded_list, klass);

	if (gklass->nested_in) {
		/* The nested_in type should not be inflated since it's possible to produce a nested type with less generic arguments*/
		klass->nested_in = gklass->nested_in;
	}

	klass->name = gklass->name;
	klass->name_space = gklass->name_space;
	
	mono_profiler_class_event (klass, MONO_PROFILE_START_LOAD);
	
	klass->image = gklass->image;
	klass->flags = gklass->flags;
	klass->type_token = gklass->type_token;
	klass->field.count = gklass->field.count;

	klass->is_inflated = 1;
	klass->generic_class = gclass;

	klass->byval_arg.type = MONO_TYPE_GENERICINST;
	klass->this_arg.type = klass->byval_arg.type;
	klass->this_arg.data.generic_class = klass->byval_arg.data.generic_class = gclass;
	klass->this_arg.byref = TRUE;
	klass->enumtype = gklass->enumtype;
	klass->valuetype = gklass->valuetype;

	klass->cast_class = klass->element_class = klass;

	if (mono_class_is_nullable (klass))
		klass->cast_class = klass->element_class = mono_class_get_nullable_param (klass);

	/*
	 * We're not interested in the nested classes of a generic instance.
	 * We use the generic type definition to look for nested classes.
	 */

	mono_generic_class_setup_parent (klass, gklass);

	if (gclass->is_dynamic) {
		/*
		 * We don't need to do any init workf with unbaked typebuilders. Generic instances created at this point will be later unregistered and/or fixed.
		 * This is to avoid work that would probably give wrong results as fields change as we build the TypeBuilder.
		 * See remove_instantiations_of_and_ensure_contents in reflection.c and its usage in reflection.c to understand the fixup stage of SRE banking.
		*/
		if (!gklass->wastypebuilder)
			klass->inited = 1;

		mono_class_setup_supertypes (klass);

		if (klass->enumtype) {
			/*
			 * For enums, gklass->fields might not been set, but instance_size etc. is 
			 * already set in mono_reflection_create_internal_class (). For non-enums,
			 * these will be computed normally in mono_class_layout_fields ().
			 */
			klass->instance_size = gklass->instance_size;
			klass->sizes.class_size = gklass->sizes.class_size;
			mono_memory_barrier ();
			klass->size_inited = 1;
		}
	}

	mono_memory_barrier ();
	gclass->cached_class = klass;

	mono_profiler_class_loaded (klass, MONO_PROFILE_OK);

	inflated_classes ++;
	inflated_classes_size += sizeof (MonoClass);
	
	mono_loader_unlock ();

	return klass;
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
		result = klass->image;
	}
	g_assert (result);
	return result;
}

MonoImage *
get_image_for_generic_param (MonoGenericParam *param)
{
	MonoGenericContainer *container = mono_generic_param_owner (param);
	g_assert_checked (container);
	return get_image_for_container (container);
}

// Make a string in the designated image consisting of a single integer.
#define INT_STRING_SIZE 16
char *
make_generic_name_string (MonoImage *image, int num)
{
	char *name = (char *)mono_image_alloc0 (image, INT_STRING_SIZE);
	g_snprintf (name, INT_STRING_SIZE, "%d", num);
	return name;
}

// This is called by mono_class_from_generic_parameter_internal when a new class must be created.
// pinfo is derived from param by the caller for us.
static MonoClass*
make_generic_param_class (MonoGenericParam *param, MonoGenericParamInfo *pinfo)
{
	MonoClass *klass, **ptr;
	int count, pos, i;
	MonoGenericContainer *container = mono_generic_param_owner (param);
	g_assert_checked (container);

	MonoImage *image = get_image_for_container (container);
	gboolean is_mvar = container->is_method;
	gboolean is_anonymous = container->is_anonymous;

	klass = (MonoClass *)mono_image_alloc0 (image, sizeof (MonoClass));
	classes_size += sizeof (MonoClass);

	if (pinfo) {
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name , pinfo->name );
	} else {
		int n = mono_generic_param_num (param);
		CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->name , make_generic_name_string (image, n) );
	}

	if (is_anonymous) {
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name_space ,  "" );
	} else if (is_mvar) {
		MonoMethod *omethod = container->owner.method;
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name_space , (omethod && omethod->klass) ? omethod->klass->name_space : "" );
	} else {
		MonoClass *oklass = container->owner.klass;
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name_space , oklass ? oklass->name_space : "" );
	}

	mono_profiler_class_event (klass, MONO_PROFILE_START_LOAD);

	// Count non-NULL items in pinfo->constraints
	count = 0;
	if (pinfo)
		for (ptr = pinfo->constraints; ptr && *ptr; ptr++, count++)
			;

	pos = 0;
	if ((count > 0) && !MONO_CLASS_IS_INTERFACE (pinfo->constraints [0])) {
		CHECKED_METADATA_WRITE_PTR ( klass->parent , pinfo->constraints [0] );
		pos++;
	} else if (pinfo && pinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) {
		CHECKED_METADATA_WRITE_PTR ( klass->parent , mono_class_load_from_name (mono_defaults.corlib, "System", "ValueType") );
	} else {
		CHECKED_METADATA_WRITE_PTR ( klass->parent , mono_defaults.object_class );
	}

	if (count - pos > 0) {
		klass->interface_count = count - pos;
		CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->interfaces , (MonoClass **)mono_image_alloc0 (image, sizeof (MonoClass *) * (count - pos)) );
		klass->interfaces_inited = TRUE;
		for (i = pos; i < count; i++)
			CHECKED_METADATA_WRITE_PTR ( klass->interfaces [i - pos] , pinfo->constraints [i] );
	}

	CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->image , image );

	klass->inited = TRUE;
	CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->cast_class ,    klass );
	CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->element_class , klass );
	klass->flags = TYPE_ATTRIBUTE_PUBLIC;

	klass->byval_arg.type = is_mvar ? MONO_TYPE_MVAR : MONO_TYPE_VAR;
	klass->this_arg.type = klass->byval_arg.type;
	CHECKED_METADATA_WRITE_PTR ( klass->this_arg.data.generic_param ,  param );
	CHECKED_METADATA_WRITE_PTR ( klass->byval_arg.data.generic_param , param );
	klass->this_arg.byref = TRUE;

	/* We don't use type_token for VAR since only classes can use it (not arrays, pointer, VARs, etc) */
	klass->sizes.generic_param_token = pinfo ? pinfo->token : 0;

	/*Init these fields to sane values*/
	klass->min_align = 1;
	/*
	 * This makes sure the the value size of this class is equal to the size of the types the gparam is
	 * constrained to, the JIT depends on this.
	 */
	klass->instance_size = sizeof (MonoObject) + mono_type_stack_size_internal (&klass->byval_arg, NULL, TRUE);
	mono_memory_barrier ();
	klass->size_inited = 1;
	klass->setup_fields_called = 1;

	mono_class_setup_supertypes (klass);

	if (count - pos > 0) {
		mono_class_setup_vtable (klass->parent);
		if (mono_class_has_failure (klass->parent))
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Failed to setup parent interfaces"));
		else
			setup_interface_offsets (klass, klass->parent->vtable_size, TRUE);
	}

	return klass;
}

#define FAST_CACHE_SIZE 16

/*
 * get_anon_gparam_class and set_anon_gparam_class are helpers for mono_class_from_generic_parameter_internal.
 * The latter will sometimes create MonoClasses for anonymous generic params. To prevent this being wasteful,
 * we cache the MonoClasses.
 * FIXME: It would be better to instead cache anonymous MonoGenericParams, and allow anonymous params to point directly to classes using the pklass field.
 * LOCKING: Takes the image lock depending on @take_lock.
 */
static MonoClass *
get_anon_gparam_class (MonoGenericParam *param, gboolean take_lock)
{
	int n = mono_generic_param_num (param);
	MonoImage *image = get_image_for_generic_param (param);
	gboolean is_mvar = mono_generic_param_owner (param)->is_method;
	MonoClass *klass = NULL;
	GHashTable *ht;

	g_assert (image);

	// For params with a small num and no constraints, we use a "fast" cache which does simple num lookup in an array.
	// For high numbers or constraints we have to use pointer hashes.
	if (param->gshared_constraint) {
		ht = is_mvar ? image->mvar_cache_constrained : image->var_cache_constrained;
		if (ht) {
			if (take_lock)
				mono_image_lock (image);
			klass = (MonoClass *)g_hash_table_lookup (ht, param);
			if (take_lock)
				mono_image_unlock (image);
		}
		return klass;
	}

	if (n < FAST_CACHE_SIZE) {
		if (is_mvar)
			return image->mvar_cache_fast ? image->mvar_cache_fast [n] : NULL;
		else
			return image->var_cache_fast ? image->var_cache_fast [n] : NULL;
	} else {
		ht = is_mvar ? image->mvar_cache_slow : image->var_cache_slow;
		if (ht) {
			if (take_lock)
				mono_image_lock (image);
			klass = (MonoClass *)g_hash_table_lookup (ht, GINT_TO_POINTER (n));
			if (take_lock)
				mono_image_unlock (image);
		}
		return klass;
	}
}

/*
 * LOCKING: Image lock (param->image) must be held
 */
static void
set_anon_gparam_class (MonoGenericParam *param, MonoClass *klass)
{
	int n = mono_generic_param_num (param);
	MonoImage *image = get_image_for_generic_param (param);
	gboolean is_mvar = mono_generic_param_owner (param)->is_method;

	g_assert (image);

	if (param->gshared_constraint) {
		GHashTable *ht = is_mvar ? image->mvar_cache_constrained : image->var_cache_constrained;
		if (!ht) {
			ht = g_hash_table_new ((GHashFunc)mono_metadata_generic_param_hash, (GEqualFunc)mono_metadata_generic_param_equal);
			mono_memory_barrier ();
			if (is_mvar)
				image->mvar_cache_constrained = ht;
			else
				image->var_cache_constrained = ht;
		}
		g_hash_table_insert (ht, param, klass);
	} else if (n < FAST_CACHE_SIZE) {
		if (is_mvar) {
			/* Requires locking to avoid droping an already published class */
			if (!image->mvar_cache_fast)
				image->mvar_cache_fast = (MonoClass **)mono_image_alloc0 (image, sizeof (MonoClass*) * FAST_CACHE_SIZE);
			image->mvar_cache_fast [n] = klass;
		} else {
			if (!image->var_cache_fast)
				image->var_cache_fast = (MonoClass **)mono_image_alloc0 (image, sizeof (MonoClass*) * FAST_CACHE_SIZE);
			image->var_cache_fast [n] = klass;
		}
	} else {
		GHashTable *ht = is_mvar ? image->mvar_cache_slow : image->var_cache_slow;
		if (!ht) {
			ht = is_mvar ? image->mvar_cache_slow : image->var_cache_slow;
			if (!ht) {
				ht = g_hash_table_new (NULL, NULL);
				mono_memory_barrier ();
				if (is_mvar)
					image->mvar_cache_slow = ht;
				else
					image->var_cache_slow = ht;
			}
		}
		g_hash_table_insert (ht, GINT_TO_POINTER (n), klass);
	}
}

/*
 * LOCKING: Acquires the image lock (@image).
 */
MonoClass *
mono_class_from_generic_parameter_internal (MonoGenericParam *param)
{
	MonoImage *image = get_image_for_generic_param (param);
	MonoGenericParamInfo *pinfo = mono_generic_param_info (param);
	MonoClass *klass, *klass2;

	// If a klass already exists for this object and is cached, return it.
	if (pinfo) // Non-anonymous
		klass = pinfo->pklass;
	else     // Anonymous
		klass = get_anon_gparam_class (param, TRUE);

	if (klass)
		return klass;

	// Create a new klass
	klass = make_generic_param_class (param, pinfo);

	// Now we need to cache the klass we created.
	// But since we wait to grab the lock until after creating the klass, we need to check to make sure
	// another thread did not get in and cache a klass ahead of us. In that case, return their klass
	// and allow our newly-created klass object to just leak.
	mono_memory_barrier ();

	mono_image_lock (image);

    // Here "klass2" refers to the klass potentially created by the other thread.
	if (pinfo) // Repeat check from above
		klass2 = pinfo->pklass;
	else
		klass2 = get_anon_gparam_class (param, FALSE);

	if (klass2) {
		klass = klass2;
	} else {
		// Cache here
		if (pinfo)
			pinfo->pklass = klass;
		else
			set_anon_gparam_class (param, klass);
	}
	mono_image_unlock (image);

	/* FIXME: Should this go inside 'make_generic_param_klass'? */
	if (klass2)
		mono_profiler_class_loaded (klass2, MONO_PROFILE_FAILED); // Alert profiler about botched class create
	else
		mono_profiler_class_loaded (klass, MONO_PROFILE_OK);

	return klass;
}

/**
 * mono_class_from_generic_parameter:
 * @param: Parameter to find/construct a class for.
 * @arg2: Is ignored.
 * @arg3: Is ignored.
 */
MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *arg2 G_GNUC_UNUSED, gboolean arg3 G_GNUC_UNUSED)
{
	return mono_class_from_generic_parameter_internal (param);
}


MonoClass *
mono_ptr_class_get (MonoType *type)
{
	MonoClass *result;
	MonoClass *el_class;
	MonoImage *image;
	char *name;

	el_class = mono_class_from_mono_type (type);
	image = el_class->image;

	mono_image_lock (image);
	if (image->ptr_cache) {
		if ((result = (MonoClass *)g_hash_table_lookup (image->ptr_cache, el_class))) {
			mono_image_unlock (image);
			return result;
		}
	}
	mono_image_unlock (image);
	
	result = (MonoClass *)mono_image_alloc0 (image, sizeof (MonoClass));

	classes_size += sizeof (MonoClass);

	result->parent = NULL; /* no parent for PTR types */
	result->name_space = el_class->name_space;
	name = g_strdup_printf ("%s*", el_class->name);
	result->name = mono_image_strdup (image, name);
	g_free (name);

	mono_profiler_class_event (result, MONO_PROFILE_START_LOAD);

	result->image = el_class->image;
	result->inited = TRUE;
	result->flags = TYPE_ATTRIBUTE_CLASS | (el_class->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK);
	/* Can pointers get boxed? */
	result->instance_size = sizeof (gpointer);
	result->cast_class = result->element_class = el_class;
	result->blittable = TRUE;

	result->byval_arg.type = MONO_TYPE_PTR;
	result->this_arg.type = result->byval_arg.type;
	result->this_arg.data.type = result->byval_arg.data.type = &result->element_class->byval_arg;
	result->this_arg.byref = TRUE;

	mono_class_setup_supertypes (result);

	mono_image_lock (image);
	if (image->ptr_cache) {
		MonoClass *result2;
		if ((result2 = (MonoClass *)g_hash_table_lookup (image->ptr_cache, el_class))) {
			mono_image_unlock (image);
			mono_profiler_class_loaded (result, MONO_PROFILE_FAILED);
			return result2;
		}
	} else {
		image->ptr_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);
	}
	g_hash_table_insert (image->ptr_cache, el_class, result);
	mono_image_unlock (image);

	mono_profiler_class_loaded (result, MONO_PROFILE_OK);

	return result;
}

static MonoClass *
mono_fnptr_class_get (MonoMethodSignature *sig)
{
	MonoClass *result;
	static GHashTable *ptr_hash = NULL;

	/* FIXME: These should be allocate from a mempool as well, but which one ? */

	mono_loader_lock ();

	if (!ptr_hash)
		ptr_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	
	if ((result = (MonoClass *)g_hash_table_lookup (ptr_hash, sig))) {
		mono_loader_unlock ();
		return result;
	}
	result = g_new0 (MonoClass, 1);

	result->parent = NULL; /* no parent for PTR types */
	result->name_space = "System";
	result->name = "MonoFNPtrFakeClass";

	mono_profiler_class_event (result, MONO_PROFILE_START_LOAD);

	result->image = mono_defaults.corlib; /* need to fix... */
	result->inited = TRUE;
	result->flags = TYPE_ATTRIBUTE_CLASS; /* | (el_class->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK); */
	/* Can pointers get boxed? */
	result->instance_size = sizeof (gpointer);
	result->cast_class = result->element_class = result;
	result->blittable = TRUE;

	result->byval_arg.type = MONO_TYPE_FNPTR;
	result->this_arg.type = result->byval_arg.type;
	result->this_arg.data.method = result->byval_arg.data.method = sig;
	result->this_arg.byref = TRUE;
	result->blittable = TRUE;

	mono_class_setup_supertypes (result);

	g_hash_table_insert (ptr_hash, sig, result);

	mono_loader_unlock ();

	mono_profiler_class_loaded (result, MONO_PROFILE_OK);

	return result;
}

/**
 * mono_class_from_mono_type:
 * @type: describes the type to return
 *
 * This returns a MonoClass for the specified MonoType, the value is never NULL.
 */
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
	case MONO_TYPE_GENERICINST:
		return mono_generic_class_get_class (type->data.generic_class);
	case MONO_TYPE_MVAR:
	case MONO_TYPE_VAR:
		return mono_class_from_generic_parameter_internal (type->data.generic_param);
	default:
		g_warning ("mono_class_from_mono_type: implement me 0x%02x\n", type->type);
		g_assert_not_reached ();
	}

	// Yes, this returns NULL, even if it is documented as not doing so, but there
	// is no way for the code to make it this far, due to the assert above.
	return NULL;
}

/**
 * mono_type_retrieve_from_typespec
 * @image: context where the image is created
 * @type_spec:  typespec token
 * @context: the generic context used to evaluate generic instantiations in
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

		if (!mono_error_ok (error)) {
			mono_loader_assert_no_error ();
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
 * @image: context where the image is created
 * @type_spec:  typespec token
 * @context: the generic context used to evaluate generic instantiations in
 */
static MonoClass *
mono_class_create_from_typespec (MonoImage *image, guint32 type_spec, MonoGenericContext *context, MonoError *error)
{
	MonoClass *ret;
	gboolean inflated = FALSE;
	MonoType *t = mono_type_retrieve_from_typespec (image, type_spec, context, &inflated, error);
	return_val_if_nok (error, NULL);
	ret = mono_class_from_mono_type (t);
	if (inflated)
		mono_metadata_free_type (t);
	return ret;
}

/**
 * mono_bounded_array_class_get:
 * @element_class: element class 
 * @rank: the dimension of the array class
 * @bounded: whenever the array has non-zero bounds
 *
 * Returns: A class object describing the array with element type @element_type and 
 * dimension @rank. 
 */
MonoClass *
mono_bounded_array_class_get (MonoClass *eclass, guint32 rank, gboolean bounded)
{
	MonoImage *image;
	MonoClass *klass;
	MonoClass *parent = NULL;
	GSList *list, *rootlist = NULL;
	int nsize;
	char *name;
	gboolean corlib_type = FALSE;

	g_assert (rank <= 255);

	if (rank > 1)
		/* bounded only matters for one-dimensional arrays */
		bounded = FALSE;

	image = eclass->image;

	if (rank == 1 && !bounded) {
		/* 
		 * This case is very frequent not just during compilation because of calls 
		 * from mono_class_from_mono_type (), mono_array_new (), 
		 * Array:CreateInstance (), etc, so use a separate cache + a separate lock.
		 */
		mono_os_mutex_lock (&image->szarray_cache_lock);
		if (!image->szarray_cache)
			image->szarray_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);
		klass = (MonoClass *)g_hash_table_lookup (image->szarray_cache, eclass);
		mono_os_mutex_unlock (&image->szarray_cache_lock);
		if (klass)
			return klass;

		mono_loader_lock ();
	} else {
		mono_loader_lock ();

		if (!image->array_cache)
			image->array_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);

		if ((rootlist = list = (GSList *)g_hash_table_lookup (image->array_cache, eclass))) {
			for (; list; list = list->next) {
				klass = (MonoClass *)list->data;
				if ((klass->rank == rank) && (klass->byval_arg.type == (((rank > 1) || bounded) ? MONO_TYPE_ARRAY : MONO_TYPE_SZARRAY))) {
					mono_loader_unlock ();
					return klass;
				}
			}
		}
	}

	/* for the building corlib use System.Array from it */
	if (image->assembly && assembly_is_dynamic (image->assembly) && image->assembly_name && strcmp (image->assembly_name, "mscorlib") == 0) {
		parent = mono_class_load_from_name (image, "System", "Array");
		corlib_type = TRUE;
	} else {
		parent = mono_defaults.array_class;
		if (!parent->inited)
			mono_class_init (parent);
	}

	klass = (MonoClass *)mono_image_alloc0 (image, sizeof (MonoClass));

	klass->image = image;
	klass->name_space = eclass->name_space;
	nsize = strlen (eclass->name);
	name = (char *)g_malloc (nsize + 2 + rank + 1);
	memcpy (name, eclass->name, nsize);
	name [nsize] = '[';
	if (rank > 1)
		memset (name + nsize + 1, ',', rank - 1);
	if (bounded)
		name [nsize + rank] = '*';
	name [nsize + rank + bounded] = ']';
	name [nsize + rank + bounded + 1] = 0;
	klass->name = mono_image_strdup (image, name);
	g_free (name);

	mono_profiler_class_event (klass, MONO_PROFILE_START_LOAD);

	classes_size += sizeof (MonoClass);

	klass->type_token = 0;
	/* all arrays are marked serializable and sealed, bug #42779 */
	klass->flags = TYPE_ATTRIBUTE_CLASS | TYPE_ATTRIBUTE_SERIALIZABLE | TYPE_ATTRIBUTE_SEALED | TYPE_ATTRIBUTE_PUBLIC;
	klass->parent = parent;
	klass->instance_size = mono_class_instance_size (klass->parent);

	if (eclass->byval_arg.type == MONO_TYPE_TYPEDBYREF || eclass->byval_arg.type == MONO_TYPE_VOID) {
		/*Arrays of those two types are invalid.*/
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
	} else if (eclass->enumtype && !mono_class_enum_basetype (eclass)) {
		if (!eclass->ref_info_handle || eclass->wastypebuilder) {
			g_warning ("Only incomplete TypeBuilder objects are allowed to be an enum without base_type");
			g_assert (eclass->ref_info_handle && !eclass->wastypebuilder);
		}
		/* element_size -1 is ok as this is not an instantitable type*/
		klass->sizes.element_size = -1;
	} else
		klass->sizes.element_size = mono_class_array_element_size (eclass);

	mono_class_setup_supertypes (klass);

	if (eclass->generic_class)
		mono_class_init (eclass);
	if (!eclass->size_inited)
		mono_class_setup_fields (eclass);
	if (mono_class_has_failure (eclass)) /*FIXME we fail the array type, but we have to let other fields be set.*/
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);

	klass->has_references = MONO_TYPE_IS_REFERENCE (&eclass->byval_arg) || eclass->has_references? TRUE: FALSE;

	klass->rank = rank;
	
	if (eclass->enumtype)
		klass->cast_class = eclass->element_class;
	else
		klass->cast_class = eclass;

	switch (klass->cast_class->byval_arg.type) {
	case MONO_TYPE_I1:
		klass->cast_class = mono_defaults.byte_class;
		break;
	case MONO_TYPE_U2:
		klass->cast_class = mono_defaults.int16_class;
		break;
	case MONO_TYPE_U4:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		klass->cast_class = mono_defaults.int32_class;
		break;
	case MONO_TYPE_U8:
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		klass->cast_class = mono_defaults.int64_class;
		break;
	default:
		break;
	}

	klass->element_class = eclass;

	if ((rank > 1) || bounded) {
		MonoArrayType *at = (MonoArrayType *)mono_image_alloc0 (image, sizeof (MonoArrayType));
		klass->byval_arg.type = MONO_TYPE_ARRAY;
		klass->byval_arg.data.array = at;
		at->eklass = eclass;
		at->rank = rank;
		/* FIXME: complete.... */
	} else {
		klass->byval_arg.type = MONO_TYPE_SZARRAY;
		klass->byval_arg.data.klass = eclass;
	}
	klass->this_arg = klass->byval_arg;
	klass->this_arg.byref = 1;
	if (corlib_type) {
		klass->inited = 1;
	}

	klass->generic_container = eclass->generic_container;

	if (rank == 1 && !bounded) {
		MonoClass *prev_class;

		mono_os_mutex_lock (&image->szarray_cache_lock);
		prev_class = (MonoClass *)g_hash_table_lookup (image->szarray_cache, eclass);
		if (prev_class)
			/* Someone got in before us */
			klass = prev_class;
		else
			g_hash_table_insert (image->szarray_cache, eclass, klass);
		mono_os_mutex_unlock (&image->szarray_cache_lock);
	} else {
		list = g_slist_append (rootlist, klass);
		g_hash_table_insert (image->array_cache, eclass, list);
	}

	mono_loader_unlock ();

	mono_profiler_class_loaded (klass, MONO_PROFILE_OK);

	return klass;
}

/**
 * mono_array_class_get:
 * @element_class: element class 
 * @rank: the dimension of the array class
 *
 * Returns: A class object describing the array with element type @element_type and 
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
 * Use to get the size of a class in bytes.
 *
 * Returns: The size of an object instance
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
 * Use to get the computed minimum alignment requirements for the specified class.
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
 * Returns: The size of the static class data
 */
gint32
mono_class_data_size (MonoClass *klass)
{	
	if (!klass->inited)
		mono_class_init (klass);
	/* This can happen with dynamically created types */
	if (!klass->fields_inited)
		mono_class_setup_fields_locking (klass);

	/* in arrays, sizes.class_size is unioned with element_size
	 * and arrays have no static fields
	 */
	if (klass->rank)
		return 0;
	return klass->sizes.class_size;
}

/*
 * Auxiliary routine to mono_class_get_field
 *
 * Takes a field index instead of a field token.
 */
static MonoClassField *
mono_class_get_field_idx (MonoClass *klass, int idx)
{
	mono_class_setup_fields_locking (klass);
	if (mono_class_has_failure (klass))
		return NULL;

	while (klass) {
		if (klass->image->uncompressed_metadata) {
			/* 
			 * klass->field.first points to the FieldPtr table, while idx points into the
			 * Field table, so we have to do a search.
			 */
			/*FIXME this is broken for types with multiple fields with the same name.*/
			const char *name = mono_metadata_string_heap (klass->image, mono_metadata_decode_row_col (&klass->image->tables [MONO_TABLE_FIELD], idx, MONO_FIELD_NAME));
			int i;

			for (i = 0; i < klass->field.count; ++i)
				if (mono_field_get_name (&klass->fields [i]) == name)
					return &klass->fields [i];
			g_assert_not_reached ();
		} else {			
			if (klass->field.count) {
				if ((idx >= klass->field.first) && (idx < klass->field.first + klass->field.count)){
					return &klass->fields [idx - klass->field.first];
				}
			}
		}
		klass = klass->parent;
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
mono_class_get_field (MonoClass *klass, guint32 field_token)
{
	int idx = mono_metadata_token_index (field_token);

	g_assert (mono_metadata_token_code (field_token) == MONO_TOKEN_FIELD_DEF);

	return mono_class_get_field_idx (klass, idx - 1);
}

/**
 * mono_class_get_field_from_name:
 * @klass: the class to lookup the field.
 * @name: the field name
 *
 * Search the class @klass and it's parents for a field with the name @name.
 * 
 * Returns: The MonoClassField pointer of the named field or NULL
 */
MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name)
{
	return mono_class_get_field_from_name_full (klass, name, NULL);
}

/**
 * mono_class_get_field_from_name_full:
 * @klass: the class to lookup the field.
 * @name: the field name
 * @type: the type of the fields. This optional.
 *
 * Search the class @klass and it's parents for a field with the name @name and type @type.
 *
 * If @klass is an inflated generic type, the type comparison is done with the equivalent field
 * of its generic type definition.
 *
 * Returns: The MonoClassField pointer of the named field or NULL
 */
MonoClassField *
mono_class_get_field_from_name_full (MonoClass *klass, const char *name, MonoType *type)
{
	int i;

	mono_class_setup_fields_locking (klass);
	if (mono_class_has_failure (klass))
		return NULL;

	while (klass) {
		for (i = 0; i < klass->field.count; ++i) {
			MonoClassField *field = &klass->fields [i];

			if (strcmp (name, mono_field_get_name (field)) != 0)
				continue;

			if (type) {
				MonoType *field_type = mono_metadata_get_corresponding_field_from_generic_type_definition (field)->type;
				if (!mono_metadata_type_equal_full (type, field_type, TRUE))
					continue;
			}
			return field;
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
 * Returns: The token representing the field in the image it was loaded from.
 */
guint32
mono_class_get_field_token (MonoClassField *field)
{
	MonoClass *klass = field->parent;
	int i;

	mono_class_setup_fields_locking (klass);

	while (klass) {
		if (!klass->fields)
			return 0;
		for (i = 0; i < klass->field.count; ++i) {
			if (&klass->fields [i] == field) {
				int idx = klass->field.first + i + 1;

				if (klass->image->uncompressed_metadata)
					idx = mono_metadata_translate_token_index (klass->image, MONO_TABLE_FIELD, idx);
				return mono_metadata_make_token (MONO_TABLE_FIELD, idx);
			}
		}
		klass = klass->parent;
	}

	g_assert_not_reached ();
	return 0;
}

static int
mono_field_get_index (MonoClassField *field)
{
	int index = field - field->parent->fields;

	g_assert (index >= 0 && index < field->parent->field.count);

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
	MonoClass *klass = field->parent;

	g_assert (field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT);

	if (!klass->ext || !klass->ext->field_def_values) {
		MonoFieldDefaultValue *def_values;

		mono_class_alloc_ext (klass);

		def_values = (MonoFieldDefaultValue *)mono_class_alloc0 (klass, sizeof (MonoFieldDefaultValue) * klass->field.count);

		mono_image_lock (klass->image);
		mono_memory_barrier ();
		if (!klass->ext->field_def_values)
			klass->ext->field_def_values = def_values;
		mono_image_unlock (klass->image);
	}

	field_index = mono_field_get_index (field);
		
	if (!klass->ext->field_def_values [field_index].data) {
		cindex = mono_metadata_get_constant_index (field->parent->image, mono_class_get_field_token (field), 0);
		if (!cindex)
			return NULL;

		g_assert (!(field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA));

		mono_metadata_decode_row (&field->parent->image->tables [MONO_TABLE_CONSTANT], cindex - 1, constant_cols, MONO_CONSTANT_SIZE);
		klass->ext->field_def_values [field_index].def_type = (MonoTypeEnum)constant_cols [MONO_CONSTANT_TYPE];
		klass->ext->field_def_values [field_index].data = (const char *)mono_metadata_blob_heap (field->parent->image, constant_cols [MONO_CONSTANT_VALUE]);
	}

	*def_type = klass->ext->field_def_values [field_index].def_type;
	return klass->ext->field_def_values [field_index].data;
}

static int
mono_property_get_index (MonoProperty *prop)
{
	int index = prop - prop->parent->ext->properties;

	g_assert (index >= 0 && index < prop->parent->ext->property.count);

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

	g_assert (property->attrs & PROPERTY_ATTRIBUTE_HAS_DEFAULT);
	/*
	 * We don't cache here because it is not used by C# so it's quite rare, but
	 * we still do the lookup in klass->ext because that is where the data
	 * is stored for dynamic assemblies.
	 */

	if (image_is_dynamic (klass->image)) {
		int prop_index = mono_property_get_index (property);
		if (klass->ext->prop_def_values && klass->ext->prop_def_values [prop_index].data) {
			*def_type = klass->ext->prop_def_values [prop_index].def_type;
			return klass->ext->prop_def_values [prop_index].data;
		}
		return NULL;
	}
	cindex = mono_metadata_get_constant_index (klass->image, mono_class_get_property_token (property), 0);
	if (!cindex)
		return NULL;

	mono_metadata_decode_row (&klass->image->tables [MONO_TABLE_CONSTANT], cindex - 1, constant_cols, MONO_CONSTANT_SIZE);
	*def_type = (MonoTypeEnum)constant_cols [MONO_CONSTANT_TYPE];
	return (const char *)mono_metadata_blob_heap (klass->image, constant_cols [MONO_CONSTANT_VALUE]);
}

guint32
mono_class_get_event_token (MonoEvent *event)
{
	MonoClass *klass = event->parent;
	int i;

	while (klass) {
		if (klass->ext) {
			for (i = 0; i < klass->ext->event.count; ++i) {
				if (&klass->ext->events [i] == event)
					return mono_metadata_make_token (MONO_TABLE_EVENT, klass->ext->event.first + i + 1);
			}
		}
		klass = klass->parent;
	}

	g_assert_not_reached ();
	return 0;
}

/**
 * mono_class_get_property_from_name:
 * @klass: a class
 * @name: name of the property to lookup in the specified class
 *
 * Use this method to lookup a property in a class
 * Returns: the MonoProperty with the given name, or NULL if the property
 * does not exist on the @klass.
 */
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

/**
 * mono_class_get_property_token:
 * @prop: MonoProperty to query
 *
 * Returns: The ECMA token for the specified property.
 */
guint32
mono_class_get_property_token (MonoProperty *prop)
{
	MonoClass *klass = prop->parent;
	while (klass) {
		MonoProperty* p;
		int i = 0;
		gpointer iter = NULL;
		while ((p = mono_class_get_properties (klass, &iter))) {
			if (&klass->ext->properties [i] == prop)
				return mono_metadata_make_token (MONO_TABLE_PROPERTY, klass->ext->property.first + i + 1);
			
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
	if (image_is_dynamic (image))
		return g_strdup_printf ("DynamicType 0x%08x", type_token);
	
	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF: {
		guint32 cols [MONO_TYPEDEF_SIZE];
		MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];
		guint tidx = mono_metadata_token_index (type_token);

		if (tidx > tt->rows)
			return g_strdup_printf ("Invalid type token 0x%08x", type_token);

		mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);
		name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);
		if (strlen (nspace) == 0)
			return g_strdup_printf ("%s", name);
		else
			return g_strdup_printf ("%s.%s", nspace, name);
	}

	case MONO_TOKEN_TYPE_REF: {
		MonoError error;
		guint32 cols [MONO_TYPEREF_SIZE];
		MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
		guint tidx = mono_metadata_token_index (type_token);

		if (tidx > t->rows)
			return g_strdup_printf ("Invalid type token 0x%08x", type_token);

		if (!mono_verifier_verify_typeref_row (image, tidx - 1, &error)) {
			char *msg = g_strdup_printf ("Invalid type token 0x%08x due to '%s'", type_token, mono_error_get_message (&error));
			mono_error_cleanup (&error);
			return msg;
		}

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
		MonoError error;
		MonoAssemblyName aname;
		guint32 cols [MONO_TYPEREF_SIZE];
		MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
		guint32 idx = mono_metadata_token_index (type_token);

		if (idx > t->rows)
			return g_strdup_printf ("Invalid type token 0x%08x", type_token);
	
		if (!mono_verifier_verify_typeref_row (image, idx - 1, &error)) {
			char *msg = g_strdup_printf ("Invalid type token 0x%08x due to '%s'", type_token, mono_error_get_message (&error));
			mono_error_cleanup (&error);
			return msg;
		}
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
 * @image: the image where the class resides
 * @type_token: the token for the class
 * @context: the generic context used to evaluate generic instantiations in
 * @deprecated: Functions that expose MonoGenericContext are going away in mono 4.0
 *
 * Returns: The MonoClass that represents @type_token in @image
 */
MonoClass *
mono_class_get_full (MonoImage *image, guint32 type_token, MonoGenericContext *context)
{
	MonoError error;
	MonoClass *klass;
	klass = mono_class_get_checked (image, type_token, &error);

	if (klass && context && mono_metadata_token_table (type_token) == MONO_TABLE_TYPESPEC)
		klass = mono_class_inflate_generic_class_checked (klass, context, &error);

	g_assert (mono_error_ok (&error)); /* FIXME deprecate this function and forbit the runtime from using it. */
	return klass;
}


MonoClass *
mono_class_get_and_inflate_typespec_checked (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error)
{
	MonoClass *klass;

	mono_error_init (error);
	klass = mono_class_get_checked (image, type_token, error);

	if (klass && context && mono_metadata_token_table (type_token) == MONO_TABLE_TYPESPEC)
		klass = mono_class_inflate_generic_class_checked (klass, context, error);

	return klass;
}
/**
 * mono_class_get_checked:
 * @image: the image where the class resides
 * @type_token: the token for the class
 * @error: error object to return any error
 *
 * Returns: The MonoClass that represents @type_token in @image, or NULL on error.
 */
MonoClass *
mono_class_get_checked (MonoImage *image, guint32 type_token, MonoError *error)
{
	MonoClass *klass = NULL;

	mono_error_init (error);

	if (image_is_dynamic (image)) {
		int table = mono_metadata_token_table (type_token);

		if (table != MONO_TABLE_TYPEDEF && table != MONO_TABLE_TYPEREF && table != MONO_TABLE_TYPESPEC) {
			mono_error_set_bad_image (error, image,"Bad token table for dynamic image: %x", table);
			return NULL;
		}
		klass = (MonoClass *)mono_lookup_dynamic_token (image, type_token, NULL); /*FIXME proper error handling*/
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
	if (!klass && mono_error_ok (error)) {
		char *name = mono_class_name_from_token (image, type_token);
		char *assembly = mono_assembly_name_from_token (image, type_token);
		mono_error_set_type_load_name (error, name, assembly, "Could not resolve type with token %08x", type_token);
	}

	return klass;
}


/**
 * mono_type_get_checked:
 * @image: the image where the type resides
 * @type_token: the token for the type
 * @context: the generic context used to evaluate generic instantiations in
 * @error: Error handling context
 *
 * This functions exists to fullfill the fact that sometimes it's desirable to have access to the 
 * 
 * Returns: The MonoType that represents @type_token in @image
 */
MonoType *
mono_type_get_checked (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error)
{
	MonoType *type = NULL;
	gboolean inflated = FALSE;

	mono_error_init (error);

	//FIXME: this will not fix the very issue for which mono_type_get_full exists -but how to do it then?
	if (image_is_dynamic (image))
		return mono_class_get_type ((MonoClass *)mono_lookup_dynamic_token (image, type_token, context));

	if ((type_token & 0xff000000) != MONO_TOKEN_TYPE_SPEC) {
		MonoClass *klass = mono_class_get_checked (image, type_token, error);

		if (!klass) {
			mono_loader_assert_no_error ();
			return NULL;
		}

		g_assert (klass);
		return mono_class_get_type (klass);
	}

	type = mono_type_retrieve_from_typespec (image, type_token, context, &inflated, error);

	if (!type) {
		mono_loader_assert_no_error ();
		return NULL;
	}

	if (inflated) {
		MonoType *tmp = type;
		type = mono_class_get_type (mono_class_from_mono_type (type));
		/* FIXME: This is a workaround fo the fact that a typespec token sometimes reference to the generic type definition.
		 * A MonoClass::byval_arg of a generic type definion has type CLASS.
		 * Some parts of mono create a GENERICINST to reference a generic type definition and this generates confict with byval_arg.
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
 * @image: image where the class token will be looked up.
 * @type_token: a type token from the image
 *
 * Returns the MonoClass with the given @type_token on the @image
 */
MonoClass *
mono_class_get (MonoImage *image, guint32 type_token)
{
	return mono_class_get_full (image, type_token, NULL);
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

	for (i = 1; i <= t->rows; ++i) {
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

		for (i = 0; i < t->rows; ++i) {
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

/*FIXME Only dynamic assemblies should allow this operation.*/
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
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
 * @deprecated: use the mono_class_from_name_case_checked variant instead.
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.   The namespace and name
 * lookups are case insensitive.
 */
MonoClass *
mono_class_from_name_case (MonoImage *image, const char* name_space, const char *name)
{
	MonoError error;
	MonoClass *res = mono_class_from_name_case_checked (image, name_space, name, &error);
	mono_error_cleanup (&error);

	return res;
}

/**
 * mono_class_from_name_case:
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
 * @error: if 
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.   The namespace and name
 * lookups are case insensitive.
 *
 * Returns: The MonoClass if the given namespace and name were found, or NULL if it
 * was not found.   The @error object will contain information about the problem
 * in that case.
 */
MonoClass *
mono_class_from_name_case_checked (MonoImage *image, const char *name_space, const char *name, MonoError *error)
{
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	const char *n;
	const char *nspace;
	guint32 i, visib;

	mono_error_init (error);

	if (image_is_dynamic (image)) {
		guint32 token = 0;
		FindUserData user_data;

		mono_image_init_name_cache (image);
		mono_image_lock (image);

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

		mono_image_unlock (image);
		
		if (token)
			return mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | token, error);
		else
			return NULL;

	}

	/* add a cache if needed */
	for (i = 1; i <= t->rows; ++i) {
		mono_metadata_decode_row (t, i - 1, cols, MONO_TYPEDEF_SIZE);
		visib = cols [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		/*
		 * Nested types are accessed from the nesting name.  We use the fact that nested types use different visibility flags
		 * than toplevel types, thus avoiding the need to grovel through the NESTED_TYPE table
		 */
		if (visib >= TYPE_ATTRIBUTE_NESTED_PUBLIC && visib <= TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM)
			continue;
		n = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);
		if (mono_utf8_strcasecmp (n, name) == 0 && mono_utf8_strcasecmp (nspace, name_space) == 0)
			return mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | i, error);
	}
	return NULL;
}

static MonoClass*
return_nested_in (MonoClass *klass, char *nested)
{
	MonoClass *found;
	char *s = strchr (nested, '/');
	gpointer iter = NULL;

	if (s) {
		*s = 0;
		s++;
	}

	while ((found = mono_class_get_nested_types (klass, &iter))) {
		if (strcmp (found->name, nested) == 0) {
			if (s)
				return return_nested_in (found, s);
			return found;
		}
	}
	return NULL;
}

static MonoClass*
search_modules (MonoImage *image, const char *name_space, const char *name, MonoError *error)
{
	MonoTableInfo *file_table = &image->tables [MONO_TABLE_FILE];
	MonoImage *file_image;
	MonoClass *klass;
	int i;

	mono_error_init (error);

	/* 
	 * The EXPORTEDTYPES table only contains public types, so have to search the
	 * modules as well.
	 * Note: image->modules contains the contents of the MODULEREF table, while
	 * the real module list is in the FILE table.
	 */
	for (i = 0; i < file_table->rows; i++) {
		guint32 cols [MONO_FILE_SIZE];
		mono_metadata_decode_row (file_table, i, cols, MONO_FILE_SIZE);
		if (cols [MONO_FILE_FLAGS] == FILE_CONTAINS_NO_METADATA)
			continue;

		file_image = mono_image_load_file_for_image (image, i + 1);
		if (file_image) {
			klass = mono_class_from_name_checked (file_image, name_space, name, error);
			if (klass || !is_ok (error))
				return klass;
		}
	}

	return NULL;
}

static MonoClass *
mono_class_from_name_checked_aux (MonoImage *image, const char* name_space, const char *name, GHashTable* visited_images, MonoError *error)
{
	GHashTable *nspace_table;
	MonoImage *loaded_image;
	guint32 token = 0;
	int i;
	MonoClass *klass;
	char *nested;
	char buf [1024];

	mono_error_init (error);

	// Checking visited images avoids stack overflows when cyclic references exist.
	if (g_hash_table_lookup (visited_images, image))
		return NULL;

	g_hash_table_insert (visited_images, image, GUINT_TO_POINTER(1));

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

	/* FIXME: get_class_from_name () can't handle types in the EXPORTEDTYPE table */
	if (get_class_from_name && image->tables [MONO_TABLE_EXPORTEDTYPE].rows == 0) {
		gboolean res = get_class_from_name (image, name_space, name, &klass);
		if (res) {
			if (!klass) {
				klass = search_modules (image, name_space, name, error);
				if (!is_ok (error))
					return NULL;
			}
			if (nested)
				return klass ? return_nested_in (klass, nested) : NULL;
			else
				return klass;
		}
	}

	mono_image_init_name_cache (image);
	mono_image_lock (image);

	nspace_table = (GHashTable *)g_hash_table_lookup (image->name_cache, name_space);

	if (nspace_table)
		token = GPOINTER_TO_UINT (g_hash_table_lookup (nspace_table, name));

	mono_image_unlock (image);

	if (!token && image_is_dynamic (image) && image->modules) {
		/* Search modules as well */
		for (i = 0; i < image->module_count; ++i) {
			MonoImage *module = image->modules [i];

			klass = mono_class_from_name_checked (module, name_space, name, error);
			if (klass || !is_ok (error))
				return klass;
		}
	}

	if (!token) {
		klass = search_modules (image, name_space, name, error);
		if (klass || !is_ok (error))
			return klass;
	}

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
			klass = mono_class_from_name_checked_aux (loaded_image, name_space, name, visited_images, error);
			if (nested)
				return klass ? return_nested_in (klass, nested) : NULL;
			return klass;
		} else if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_ASSEMBLYREF) {
			guint32 assembly_idx;

			assembly_idx = impl >> MONO_IMPLEMENTATION_BITS;

			mono_assembly_load_reference (image, assembly_idx - 1);
			g_assert (image->references [assembly_idx - 1]);
			if (image->references [assembly_idx - 1] == (gpointer)-1)
				return NULL;			
			klass = mono_class_from_name_checked_aux (image->references [assembly_idx - 1]->image, name_space, name, visited_images, error);
			if (nested)
				return klass ? return_nested_in (klass, nested) : NULL;
			return klass;
		} else {
			g_assert_not_reached ();
		}
	}

	token = MONO_TOKEN_TYPE_DEF | token;

	klass = mono_class_get_checked (image, token, error);
	if (nested)
		return return_nested_in (klass, nested);
	return klass;
}

/**
 * mono_class_from_name_checked:
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
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

	klass = mono_class_from_name_checked_aux (image, name_space, name, visited_images, error);

	g_hash_table_destroy (visited_images);

	return klass;
}

/**
 * mono_class_from_name:
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.
 *
 * To reference nested classes, use the "/" character as a separator.
 * For example use "Foo/Bar" to reference the class Bar that is nested
 * inside Foo, like this: "class Foo { class Bar {} }".
 */
MonoClass *
mono_class_from_name (MonoImage *image, const char* name_space, const char *name)
{
	MonoError error;
	MonoClass *klass;

	klass = mono_class_from_name_checked (image, name_space, name, &error);
	mono_error_cleanup (&error); /* FIXME Don't swallow the error */

	return klass;
}

/**
 * mono_class_load_from_name:
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
 *
 * This function works exactly like mono_class_from_name but it will abort if the class is not found.
 * This function should be used by the runtime for critical types to which there's no way to recover but crash
 * If they are missing. Thing of System.Object or System.String.
 */
MonoClass *
mono_class_load_from_name (MonoImage *image, const char* name_space, const char *name)
{
	MonoError error;
	MonoClass *klass;

	klass = mono_class_from_name_checked (image, name_space, name, &error);
	if (!klass)
		g_error ("Runtime critical type %s.%s not found", name_space, name);
	if (!mono_error_ok (&error))
		g_error ("Could not load runtime critical type %s.%s due to %s", name_space, name, mono_error_get_message (&error));
	return klass;
}

/**
 * mono_class_try_load_from_name:
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
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
	MonoError error;
	MonoClass *klass;

	klass = mono_class_from_name_checked (image, name_space, name, &error);
	if (!mono_error_ok (&error))
		g_error ("Could not load runtime critical type %s.%s due to %s", name_space, name, mono_error_get_message (&error));
	return klass;
}


/**
 * mono_class_is_subclass_of:
 * @klass: class to probe if it is a subclass of another one
 * @klassc: the class we suspect is the base class
 * @check_interfaces: whether we should perform interface checks
 *
 * This method determines whether @klass is a subclass of @klassc.
 *
 * If the @check_interfaces flag is set, then if @klassc is an interface
 * this method return TRUE if the @klass implements the interface or
 * if @klass is an interface, if one of its base classes is @klass.
 *
 * If @check_interfaces is false then, then if @klass is not an interface
 * then it returns TRUE if the @klass is a subclass of @klassc.
 *
 * if @klass is an interface and @klassc is System.Object, then this function
 * return true.
 *
 */
gboolean
mono_class_is_subclass_of (MonoClass *klass, MonoClass *klassc, 
			   gboolean check_interfaces)
{
/*FIXME test for interfaces with variant generic arguments*/
	
	if (check_interfaces && MONO_CLASS_IS_INTERFACE (klassc) && !MONO_CLASS_IS_INTERFACE (klass)) {
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (klass, klassc->interface_id))
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

	if (!klass->generic_class)
		return FALSE;

	container = klass->generic_class->container_class->generic_container;

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
		mono_type_is_generic_argument (&target->byval_arg) &&
		mono_type_is_generic_argument (&candidate->byval_arg)) {
		MonoGenericParam *gparam = candidate->byval_arg.data.generic_param;
		MonoGenericParamInfo *pinfo = mono_generic_param_info (gparam);

		if (!pinfo || (pinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) == 0)
			return FALSE;
	}
	if (!mono_class_is_assignable_from (target, candidate))
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
	MonoGenericContainer *container = klass_gtd->generic_container;

	if (klass == oklass)
		return TRUE;

	/*Viable candidates are instances of the same generic interface*/
	if (mono_class_get_generic_type_definition (oklass) != klass_gtd || oklass == klass_gtd)
		return FALSE;

	klass_argv = &klass->generic_class->context.class_inst->type_argv [0];
	oklass_argv = &oklass->generic_class->context.class_inst->type_argv [0];

	for (j = 0; j < container->type_argc; ++j) {
		MonoClass *param1_class = mono_class_from_mono_type (klass_argv [j]);
		MonoClass *param2_class = mono_class_from_mono_type (oklass_argv [j]);

		if (param1_class->valuetype != param2_class->valuetype || (param1_class->valuetype && param1_class != param2_class))
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
	if (target->byval_arg.type != candidate->byval_arg.type)
		return FALSE;

	gparam = target->byval_arg.data.generic_param;
	ogparam = candidate->byval_arg.data.generic_param;
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

			if (mono_type_is_reference (&cc->byval_arg) && !MONO_CLASS_IS_INTERFACE (cc))
				class_constraint_satisfied = TRUE;
			else if (!mono_type_is_reference (&cc->byval_arg) && !MONO_CLASS_IS_INTERFACE (cc))
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

			/*
			 * A constraint from @target might inflate into @candidate itself and in that case we don't need
			 * check it's constraints since it satisfy the constraint by itself.
			 */
			if (mono_metadata_type_equal (&tc->byval_arg, &candidate->byval_arg))
				continue;

			if (!cinfo->constraints)
				return FALSE;

			for (candidate_class = cinfo->constraints; *candidate_class; ++candidate_class) {
				MonoClass *cc = *candidate_class;

				if (mono_class_is_assignable_from (tc, cc))
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
				if (mono_type_is_generic_argument (&cc->byval_arg)) {
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
			if (mono_class_is_assignable_from (target, cc))
				return TRUE;
		}
	}
	return FALSE;
}

/**
 * mono_class_is_assignable_from:
 * @klass: the class to be assigned to
 * @oklass: the source class
 *
 * Returns: TRUE if an instance of object oklass can be assigned to an
 * instance of object @klass
 */
gboolean
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass)
{
	/*FIXME this will cause a lot of irrelevant stuff to be loaded.*/
	if (!klass->inited)
		mono_class_init (klass);

	if (!oklass->inited)
		mono_class_init (oklass);

	if (mono_class_has_failure (klass) || mono_class_has_failure  (oklass))
		return FALSE;

	if (mono_type_is_generic_argument (&klass->byval_arg)) {
		if (!mono_type_is_generic_argument (&oklass->byval_arg))
			return FALSE;
		return mono_gparam_is_assignable_from (klass, oklass);
	}

	if (MONO_CLASS_IS_INTERFACE (klass)) {
		if ((oklass->byval_arg.type == MONO_TYPE_VAR) || (oklass->byval_arg.type == MONO_TYPE_MVAR)) {
			MonoGenericParam *gparam = oklass->byval_arg.data.generic_param;
			MonoClass **constraints = mono_generic_container_get_param_info (gparam->owner, gparam->num)->constraints;
			int i;

			if (constraints) {
				for (i = 0; constraints [i]; ++i) {
					if (mono_class_is_assignable_from (klass, constraints [i]))
						return TRUE;
				}
			}

			return FALSE;
		}

		/* interface_offsets might not be set for dynamic classes */
		if (oklass->ref_info_handle && !oklass->interface_bitmap)
			/* 
			 * oklass might be a generic type parameter but they have 
			 * interface_offsets set.
			 */
 			return mono_reflection_call_is_assignable_to (oklass, klass);
		if (!oklass->interface_bitmap)
			/* Happens with generic instances of not-yet created dynamic types */
			return FALSE;
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (oklass, klass->interface_id))
			return TRUE;

		if (mono_class_has_variant_generic_params (klass)) {
			MonoError error;
			int i;
			mono_class_setup_interfaces (oklass, &error);
			if (!mono_error_ok (&error)) {
				mono_error_cleanup (&error);
				return FALSE;
			}

			/*klass is a generic variant interface, We need to extract from oklass a list of ifaces which are viable candidates.*/
			for (i = 0; i < oklass->interface_offsets_count; ++i) {
				MonoClass *iface = oklass->interfaces_packed [i];

				if (mono_class_is_variant_compatible (klass, iface, FALSE))
					return TRUE;
			}
		}
		return FALSE;
	} else if (klass->delegate) {
		if (mono_class_has_variant_generic_params (klass) && mono_class_is_variant_compatible (klass, oklass, FALSE))
			return TRUE;
	}else if (klass->rank) {
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
	} else if (mono_class_is_nullable (klass)) {
		if (mono_class_is_nullable (oklass))
			return mono_class_is_assignable_from (klass->cast_class, oklass->cast_class);
		else
			return mono_class_is_assignable_from (klass->cast_class, oklass);
	} else if (klass == mono_defaults.object_class)
		return TRUE;

	return mono_class_has_parent (oklass, klass);
}	

/*Check if @oklass is variant compatible with @klass.*/
static gboolean
mono_class_is_variant_compatible_slow (MonoClass *klass, MonoClass *oklass)
{
	int j;
	MonoType **klass_argv, **oklass_argv;
	MonoClass *klass_gtd = mono_class_get_generic_type_definition (klass);
	MonoGenericContainer *container = klass_gtd->generic_container;

	/*Viable candidates are instances of the same generic interface*/
	if (mono_class_get_generic_type_definition (oklass) != klass_gtd || oklass == klass_gtd)
		return FALSE;

	klass_argv = &klass->generic_class->context.class_inst->type_argv [0];
	oklass_argv = &oklass->generic_class->context.class_inst->type_argv [0];

	for (j = 0; j < container->type_argc; ++j) {
		MonoClass *param1_class = mono_class_from_mono_type (klass_argv [j]);
		MonoClass *param2_class = mono_class_from_mono_type (oklass_argv [j]);

		if (param1_class->valuetype != param2_class->valuetype)
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
	MonoError error;
	int i;
	gboolean is_variant = mono_class_has_variant_generic_params (target);

	if (is_variant && MONO_CLASS_IS_INTERFACE (candidate)) {
		if (mono_class_is_variant_compatible_slow (target, candidate))
			return TRUE;
	}

	do {
		if (candidate == target)
			return TRUE;

		/*A TypeBuilder can have more interfaces on tb->interfaces than on candidate->interfaces*/
		if (image_is_dynamic (candidate->image) && !candidate->wastypebuilder) {
			MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mono_class_get_ref_info (candidate);
			int j;
			if (tb && tb->interfaces) {
				for (j = mono_array_length (tb->interfaces) - 1; j >= 0; --j) {
					MonoReflectionType *iface = mono_array_get (tb->interfaces, MonoReflectionType*, j);
					MonoClass *iface_class;

					/* we can't realize the type here since it can do pretty much anything. */
					if (!iface->type)
						continue;
					iface_class = mono_class_from_mono_type (iface->type);
					if (iface_class == target)
						return TRUE;
					if (is_variant && mono_class_is_variant_compatible_slow (target, iface_class))
						return TRUE;
					if (mono_class_implement_interface_slow (target, iface_class))
						return TRUE;
				}
			}
		} else {
			/*setup_interfaces don't mono_class_init anything*/
			/*FIXME this doesn't handle primitive type arrays.
			ICollection<sbyte> x byte [] won't work because candidate->interfaces, for byte[], won't have IList<sbyte>.
			A possible way to fix this would be to move that to setup_interfaces from setup_interface_offsets.
			*/
			mono_class_setup_interfaces (candidate, &error);
			if (!mono_error_ok (&error)) {
				mono_error_cleanup (&error);
				return FALSE;
			}

			for (i = 0; i < candidate->interface_count; ++i) {
				if (candidate->interfaces [i] == target)
					return TRUE;
				
				if (is_variant && mono_class_is_variant_compatible_slow (target, candidate->interfaces [i]))
					return TRUE;

				 if (mono_class_implement_interface_slow (target, candidate->interfaces [i]))
					return TRUE;
			}
		}
		candidate = candidate->parent;
	} while (candidate);

	return FALSE;
}

/*
 * Check if @oklass can be assigned to @klass.
 * This function does the same as mono_class_is_assignable_from but is safe to be used from mono_class_init context.
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
	if (MONO_CLASS_IS_INTERFACE (target))
		return mono_class_implement_interface_slow (target, candidate);

 	if (target->delegate && mono_class_has_variant_generic_params (target))
		return mono_class_is_variant_compatible (target, candidate, FALSE);

	if (target->rank) {
		MonoClass *eclass, *eoclass;

		if (target->rank != candidate->rank)
			return FALSE;

		/* vectors vs. one dimensional arrays */
		if (target->byval_arg.type != candidate->byval_arg.type)
			return FALSE;

		eclass = target->cast_class;
		eoclass = candidate->cast_class;

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

		return mono_class_is_assignable_from_slow (target->cast_class, candidate->cast_class);
	}
	/*FIXME properly handle nullables */
	/*FIXME properly handle (M)VAR */
	return FALSE;
}

/**
 * mono_class_get_cctor:
 * @klass: A MonoClass pointer
 *
 * Returns: The static constructor of @klass if it exists, NULL otherwise.
 */
MonoMethod*
mono_class_get_cctor (MonoClass *klass)
{
	MonoCachedClassInfo cached_info;

	if (image_is_dynamic (klass->image)) {
		/* 
		 * has_cctor is not set for these classes because mono_class_init () is
		 * not run for them.
		 */
		return mono_class_get_method_from_name_flags (klass, ".cctor", -1, METHOD_ATTRIBUTE_SPECIAL_NAME);
	}

	if (!klass->has_cctor)
		return NULL;

	if (mono_class_get_cached_class_info (klass, &cached_info)) {
		MonoError error;
		MonoMethod *result = mono_get_method_checked (klass->image, cached_info.cctor_token, klass, NULL, &error);
		if (!mono_error_ok (&error))
			g_error ("Could not lookup class cctor from cached metadata due to %s", mono_error_get_message (&error));
		return result;
	}

	if (klass->generic_class && !klass->methods)
		return mono_class_get_inflated_method (klass, mono_class_get_cctor (klass->generic_class->container_class));

	return mono_class_get_method_from_name_flags (klass, ".cctor", -1, METHOD_ATTRIBUTE_SPECIAL_NAME);
}

/**
 * mono_class_get_finalizer:
 * @klass: The MonoClass pointer
 *
 * Returns: The finalizer method of @klass if it exists, NULL otherwise.
 */
MonoMethod*
mono_class_get_finalizer (MonoClass *klass)
{
	MonoCachedClassInfo cached_info;

	if (!klass->inited)
		mono_class_init (klass);
	if (!mono_class_has_finalizer (klass))
		return NULL;

	if (mono_class_get_cached_class_info (klass, &cached_info)) {
		MonoError error;
		MonoMethod *result = mono_get_method_checked (cached_info.finalize_image, cached_info.finalize_token, NULL, NULL, &error);
		if (!mono_error_ok (&error))
			g_error ("Could not lookup finalizer from cached metadata due to %s", mono_error_get_message (&error));
		return result;
	}else {
		mono_class_setup_vtable (klass);
		return klass->vtable [finalize_slot];
	}
}

/**
 * mono_class_needs_cctor_run:
 * @klass: the MonoClass pointer
 * @caller: a MonoMethod describing the caller
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
 * @klass: 
 *
 * Returns: The number of bytes an element of type @klass
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
		return sizeof (gpointer);
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		return 8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = mono_class_enum_basetype (type->data.klass);
			klass = klass->element_class;
			goto handle_enum;
		}
		return mono_class_instance_size (klass) - sizeof (MonoObject);
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
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
 * @ac: pointer to a #MonoArrayClass
 *
 * Returns: The size of single array element.
 */
gint32
mono_array_element_size (MonoClass *ac)
{
	g_assert (ac->rank);
	return ac->sizes.element_size;
}

gpointer
mono_ldtoken (MonoImage *image, guint32 token, MonoClass **handle_class,
	      MonoGenericContext *context)
{
	MonoError error;
	gpointer res = mono_ldtoken_checked (image, token, handle_class, context, &error);
	g_assert (mono_error_ok (&error));
	return res;
}

gpointer
mono_ldtoken_checked (MonoImage *image, guint32 token, MonoClass **handle_class,
	      MonoGenericContext *context, MonoError *error)
{
	mono_error_init (error);

	if (image_is_dynamic (image)) {
		MonoClass *tmp_handle_class;
		gpointer obj = mono_lookup_dynamic_token_class (image, token, TRUE, &tmp_handle_class, context);

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
	case MONO_TOKEN_TYPE_REF:
	case MONO_TOKEN_TYPE_SPEC: {
		MonoType *type;
		if (handle_class)
			*handle_class = mono_defaults.typehandle_class;
		type = mono_type_get_checked (image, token, context, error);
		if (!type)
			return NULL;

		mono_class_init (mono_class_from_mono_type (type));
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

		mono_class_init (klass);
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
mono_lookup_dynamic_token (MonoImage *image, guint32 token, MonoGenericContext *context)
{
	MonoClass *handle_class;

	return lookup_dynamic (image, token, TRUE, &handle_class, context);
}

gpointer
mono_lookup_dynamic_token_class (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context)
{
	return lookup_dynamic (image, token, valid_token, handle_class, context);
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

void
mono_install_get_class_from_name (MonoGetClassFromName func)
{
	get_class_from_name = func;
}

/**
 * mono_class_get_image:
 *
 * Use this method to get the `MonoImage*` where this class came from.
 *
 * Returns: The image where this class is defined.
 */
MonoImage*
mono_class_get_image (MonoClass *klass)
{
	return klass->image;
}

/**
 * mono_class_get_element_class:
 * @klass: the MonoClass to act on
 *
 * Use this function to get the element class of an array.
 *
 * Returns: The element class of an array.
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
 * Use this method to determine if the provided `MonoClass*` represents a value type,
 * or a reference type.
 *
 * Returns: TRUE if the MonoClass represents a ValueType, FALSE if it represents a reference type.
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
 * Use this function to determine if the provided `MonoClass*` represents an enumeration.
 *
 * Returns: TRUE if the MonoClass represents an enumeration.
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
 * Use this function to get the underlying type for an enumeration value.
 * 
 * Returns: The underlying type representation for an enumeration.
 */
MonoType*
mono_class_enum_basetype (MonoClass *klass)
{
	if (klass->element_class == klass)
		/* SRE or broken types */
		return NULL;
	else
		return &klass->element_class->byval_arg;
}

/**
 * mono_class_get_parent
 * @klass: the MonoClass to act on
 *
 * Returns: The parent class for this class.
 */
MonoClass*
mono_class_get_parent (MonoClass *klass)
{
	return klass->parent;
}

/**
 * mono_class_get_nesting_type:
 * @klass: the MonoClass to act on
 *
 * Use this function to obtain the class that the provided `MonoClass*` is nested on.
 *
 * If the return is NULL, this indicates that this class is not nested.
 *
 * Returns: The container type where this type is nested or NULL if this type is not a nested type.
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
 * Returns: The rank for the array (the number of dimensions).
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
 * Returns: The flags from the TypeDef table.
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
 * Returns: The name of the class.
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
 * Returns: The namespace of the class.
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
 * Returns: The MonoType from the class.
 */
MonoType*
mono_class_get_type (MonoClass *klass)
{
	return &klass->byval_arg;
}

/**
 * mono_class_get_type_token:
 * @klass: the MonoClass to act on
 *
 * This method returns type token for the class.
 *
 * Returns: The type token for the class.
 */
guint32
mono_class_get_type_token (MonoClass *klass)
{
  return klass->type_token;
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
 * Returns: The number of static and instance fields in the class.
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
 * Returns: The number of methods in the class.
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
 * Returns: The number of properties in the class.
 */
int
mono_class_num_properties (MonoClass *klass)
{
	mono_class_setup_properties (klass);

	return klass->ext->property.count;
}

/**
 * mono_class_num_events:
 * @klass: the MonoClass to act on
 *
 * Returns: The number of events in the class.
 */
int
mono_class_num_events (MonoClass *klass)
{
	mono_class_setup_events (klass);

	return klass->ext->event.count;
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
	if (!*iter) {
		mono_class_setup_fields_locking (klass);
		if (mono_class_has_failure (klass))
			return NULL;
		/* start from the first */
		if (klass->field.count) {
			*iter = &klass->fields [0];
			return &klass->fields [0];
		} else {
			/* no fields */
			return NULL;
		}
	}
	field = (MonoClassField *)*iter;
	field++;
	if (field < &klass->fields [klass->field.count]) {
		*iter = field;
		return field;
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
	if (!*iter) {
		mono_class_setup_methods (klass);

		/*
		 * We can't fail lookup of methods otherwise the runtime will burst in flames on all sort of places.
		 * FIXME we should better report this error to the caller
		 */
		if (!klass->methods)
			return NULL;
		/* start from the first */
		if (klass->method.count) {
			*iter = &klass->methods [0];
			return klass->methods [0];
		} else {
			/* no method */
			return NULL;
		}
	}
	method = (MonoMethod **)*iter;
	method++;
	if (method < &klass->methods [klass->method.count]) {
		*iter = method;
		return *method;
	}
	return NULL;
}

/*
 * mono_class_get_virtual_methods:
 *
 *   Iterate over the virtual methods of KLASS.
 *
 * LOCKING: Assumes the loader lock is held (because of the klass->methods check).
 */
static MonoMethod*
mono_class_get_virtual_methods (MonoClass* klass, gpointer *iter)
{
	MonoMethod** method;
	if (!iter)
		return NULL;
	if (klass->methods || !MONO_CLASS_HAS_STATIC_METADATA (klass)) {
		if (!*iter) {
			mono_class_setup_methods (klass);
			/*
			 * We can't fail lookup of methods otherwise the runtime will burst in flames on all sort of places.
			 * FIXME we should better report this error to the caller
			 */
			if (!klass->methods)
				return NULL;
			/* start from the first */
			method = &klass->methods [0];
		} else {
			method = (MonoMethod **)*iter;
			method++;
		}
		while (method < &klass->methods [klass->method.count]) {
			if (*method && ((*method)->flags & METHOD_ATTRIBUTE_VIRTUAL))
				break;
			method ++;
		}
		if (method < &klass->methods [klass->method.count]) {
			*iter = method;
			return *method;
		} else {
			return NULL;
		}
	} else {
		/* Search directly in metadata to avoid calling setup_methods () */
		MonoMethod *res = NULL;
		int i, start_index;

		if (!*iter) {
			start_index = 0;
		} else {
			start_index = GPOINTER_TO_UINT (*iter);
		}

		for (i = start_index; i < klass->method.count; ++i) {
			guint32 flags;

			/* klass->method.first points into the methodptr table */
			flags = mono_metadata_decode_table_row_col (klass->image, MONO_TABLE_METHOD, klass->method.first + i, MONO_METHOD_FLAGS);

			if (flags & METHOD_ATTRIBUTE_VIRTUAL)
				break;
		}

		if (i < klass->method.count) {
			MonoError error;
			res = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | (klass->method.first + i + 1), klass, NULL, &error);
			mono_error_cleanup (&error); /* FIXME don't swallow the error */

			/* Add 1 here so the if (*iter) check fails */
			*iter = GUINT_TO_POINTER (i + 1);
			return res;
		} else {
			return NULL;
		}
	}
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
	if (!*iter) {
		mono_class_setup_properties (klass);
		/* start from the first */
		if (klass->ext->property.count) {
			*iter = &klass->ext->properties [0];
			return (MonoProperty *)*iter;
		} else {
			/* no fields */
			return NULL;
		}
	}
	property = (MonoProperty *)*iter;
	property++;
	if (property < &klass->ext->properties [klass->ext->property.count]) {
		*iter = property;
		return (MonoProperty *)*iter;
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
	if (!*iter) {
		mono_class_setup_events (klass);
		/* start from the first */
		if (klass->ext->event.count) {
			*iter = &klass->ext->events [0];
			return (MonoEvent *)*iter;
		} else {
			/* no fields */
			return NULL;
		}
	}
	event = (MonoEvent *)*iter;
	event++;
	if (event < &klass->ext->events [klass->ext->event.count]) {
		*iter = event;
		return (MonoEvent *)*iter;
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
	MonoError error;
	MonoClass** iface;
	if (!iter)
		return NULL;
	if (!*iter) {
		if (!klass->inited)
			mono_class_init (klass);
		if (!klass->interfaces_inited) {
			mono_class_setup_interfaces (klass, &error);
			if (!mono_error_ok (&error)) {
				mono_error_cleanup (&error);
				return NULL;
			}
		}
		/* start from the first */
		if (klass->interface_count) {
			*iter = &klass->interfaces [0];
			return klass->interfaces [0];
		} else {
			/* no interface */
			return NULL;
		}
	}
	iface = (MonoClass **)*iter;
	iface++;
	if (iface < &klass->interfaces [klass->interface_count]) {
		*iter = iface;
		return *iface;
	}
	return NULL;
}

static void
setup_nested_types (MonoClass *klass)
{
	MonoError error;
	GList *classes, *nested_classes, *l;
	int i;

	if (klass->nested_classes_inited)
		return;

	if (!klass->type_token)
		klass->nested_classes_inited = TRUE;

	i = mono_metadata_nesting_typedef (klass->image, klass->type_token, 1);
	classes = NULL;
	while (i) {
		MonoClass* nclass;
		guint32 cols [MONO_NESTED_CLASS_SIZE];
		mono_metadata_decode_row (&klass->image->tables [MONO_TABLE_NESTEDCLASS], i - 1, cols, MONO_NESTED_CLASS_SIZE);
		nclass = mono_class_create_from_typedef (klass->image, MONO_TOKEN_TYPE_DEF | cols [MONO_NESTED_CLASS_NESTED], &error);
		if (!mono_error_ok (&error)) {
			/*FIXME don't swallow the error message*/
			mono_error_cleanup (&error);

			i = mono_metadata_nesting_typedef (klass->image, klass->type_token, i + 1);
			continue;
		}

		classes = g_list_prepend (classes, nclass);

		i = mono_metadata_nesting_typedef (klass->image, klass->type_token, i + 1);
	}

	mono_class_alloc_ext (klass);

	nested_classes = NULL;
	for (l = classes; l; l = l->next)
		nested_classes = g_list_prepend_image (klass->image, nested_classes, l->data);
	g_list_free (classes);

	mono_image_lock (klass->image);

	mono_memory_barrier ();
	if (!klass->nested_classes_inited) {
		klass->ext->nested_classes = nested_classes;
		mono_memory_barrier ();
		klass->nested_classes_inited = TRUE;
	}

	mono_image_unlock (klass->image);
}

/**
 * mono_class_get_nested_types
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the nested types of a class.
 * This works only if @klass is non-generic, or a generic type definition.
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
	if (!klass->nested_classes_inited)
		setup_nested_types (klass);

	if (!*iter) {
		/* start from the first */
		if (klass->ext && klass->ext->nested_classes) {
			*iter = klass->ext->nested_classes;
			return (MonoClass *)klass->ext->nested_classes->data;
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
 * @klass: the MonoClass to act on
 *
 * Returns: TRUE if the MonoClass represents a System.Delegate.
 */
mono_bool
mono_class_is_delegate (MonoClass *klass)
{
	return klass->delegate;
}

/**
 * mono_class_implements_interface
 * @klass: The MonoClass to act on
 * @interface: The interface to check if @klass implements.
 *
 * Returns: TRUE if @klass implements @interface.
 */
mono_bool
mono_class_implements_interface (MonoClass* klass, MonoClass* iface)
{
	return mono_class_is_assignable_from (iface, klass);
}

/**
 * mono_field_get_name:
 * @field: the MonoClassField to act on
 *
 * Returns: The name of the field.
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
	MonoError error;
	MonoType *type = mono_field_get_type_checked (field, &error);
	if (!mono_error_ok (&error)) {
		mono_trace_warning (MONO_TRACE_TYPE, "Could not load field's type due to %s", mono_error_get_message (&error));
		mono_error_cleanup (&error);
	}
	return type;
}


/**
 * mono_field_get_type_checked:
 * @field: the MonoClassField to act on
 * @error: used to return any erro found while retrieving @field type
 *
 * Returns: MonoType of the field.
 */
MonoType*
mono_field_get_type_checked (MonoClassField *field, MonoError *error)
{
	mono_error_init (error);
	if (!field->type)
		mono_field_resolve_type (field, error);
	return field->type;
}

/**
 * mono_field_get_parent:
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
 * Returns: The flags for the field.
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
 * @field: the MonoClassField to act on
 *
 * Returns: The field offset.
 */
guint32
mono_field_get_offset (MonoClassField *field)
{
	return field->offset;
}

static const char *
mono_field_get_rva (MonoClassField *field)
{
	guint32 rva;
	int field_index;
	MonoClass *klass = field->parent;
	MonoFieldDefaultValue *field_def_values;

	g_assert (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA);

	if (!klass->ext || !klass->ext->field_def_values) {
		mono_class_alloc_ext (klass);

		field_def_values = (MonoFieldDefaultValue *)mono_class_alloc0 (klass, sizeof (MonoFieldDefaultValue) * klass->field.count);

		mono_image_lock (klass->image);
		if (!klass->ext->field_def_values)
			klass->ext->field_def_values = field_def_values;
		mono_image_unlock (klass->image);
	}

	field_index = mono_field_get_index (field);
		
	if (!klass->ext->field_def_values [field_index].data && !image_is_dynamic (klass->image)) {
		mono_metadata_field_info (field->parent->image, klass->field.first + field_index, NULL, &rva, NULL);
		if (!rva)
			g_warning ("field %s in %s should have RVA data, but hasn't", mono_field_get_name (field), field->parent->name);
		klass->ext->field_def_values [field_index].data = mono_image_rva_map (field->parent->image, rva);
	}

	return klass->ext->field_def_values [field_index].data;
}

/**
 * mono_field_get_data:
 * @field: the MonoClassField to act on
 *
 * Returns: A pointer to the metadata constant value or to the field
 * data if it has an RVA flag.
 */
const char *
mono_field_get_data (MonoClassField *field)
{
	if (field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT) {
		MonoTypeEnum def_type;

		return mono_class_get_field_default_value (field, &def_type);
	} else if (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) {
		return mono_field_get_rva (field);
	} else {
		return NULL;
	}
}

/**
 * mono_property_get_name: 
 * @prop: the MonoProperty to act on
 *
 * Returns: The name of the property
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
 * Returns: The setter method of the property (A MonoMethod)
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
 * Returns: The setter method of the property (A MonoMethod)
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
 * Returns: The MonoClass where the property was defined.
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
 * Returns: The flags for the property.
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
 * Returns: The name of the event.
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
 * Returns: The @add' method for the event (a MonoMethod).
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
 * Returns: The @remove method for the event (a MonoMethod).
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
 * Returns: The @raise method for the event (a MonoMethod).
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
 * Returns: The MonoClass where the event is defined.
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
 * Returns: The flags for the event.
 */
guint32
mono_event_get_flags (MonoEvent *event)
{
	return event->attrs;
}

/**
 * mono_class_get_method_from_name:
 * @klass: where to look for the method
 * @name: name of the method
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

static MonoMethod*
find_method_in_metadata (MonoClass *klass, const char *name, int param_count, int flags)
{
	MonoMethod *res = NULL;
	int i;

	/* Search directly in the metadata to avoid calling setup_methods () */
	for (i = 0; i < klass->method.count; ++i) {
		MonoError error;
		guint32 cols [MONO_METHOD_SIZE];
		MonoMethod *method;
		MonoMethodSignature *sig;

		/* klass->method.first points into the methodptr table */
		mono_metadata_decode_table_row (klass->image, MONO_TABLE_METHOD, klass->method.first + i, cols, MONO_METHOD_SIZE);

		if (!strcmp (mono_metadata_string_heap (klass->image, cols [MONO_METHOD_NAME]), name)) {
			method = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | (klass->method.first + i + 1), klass, NULL, &error);
			if (!method) {
				mono_error_cleanup (&error); /* FIXME don't swallow the error */
				continue;
			}
			if (param_count == -1) {
				res = method;
				break;
			}
			sig = mono_method_signature_checked (method, &error);
			if (!sig) {
				mono_error_cleanup (&error); /* FIXME don't swallow the error */
				continue;
			}
			if (sig->param_count == param_count) {
				res = method;
				break;
			}
		}
	}

	return res;
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

	if (klass->generic_class && !klass->methods) {
		res = mono_class_get_method_from_name_flags (klass->generic_class->container_class, name, param_count, flags);
		if (res) {
			MonoError error;
			res = mono_class_inflate_generic_method_full_checked (res, klass, mono_class_get_context (klass), &error);
			if (!mono_error_ok (&error))
				mono_error_cleanup (&error); /*FIXME don't swallow the error */
		}
		return res;
	}

	if (klass->methods || !MONO_CLASS_HAS_STATIC_METADATA (klass)) {
		mono_class_setup_methods (klass);
		/*
		We can't fail lookup of methods otherwise the runtime will burst in flames on all sort of places.
		See mono/tests/array_load_exception.il
		FIXME we should better report this error to the caller
		 */
		if (!klass->methods)
			return NULL;
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
	    res = find_method_in_metadata (klass, name, param_count, flags);
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
 *
 * LOCKING: Acquires the loader lock.
 */
gboolean
mono_class_set_failure (MonoClass *klass, guint32 ex_type, void *ex_data)
{
	if (mono_class_has_failure (klass))
		return FALSE;

	mono_loader_lock ();
	klass->exception_type = ex_type;
	if (ex_data)
		mono_image_property_insert (klass->image, klass, MONO_CLASS_PROP_EXCEPTION_DATA, ex_data);
	mono_loader_unlock ();

	return TRUE;
}

/*
 * mono_class_get_exception_data:
 *
 *   Return the exception_data property of KLASS.
 *
 * LOCKING: Acquires the loader lock.
 */
gpointer
mono_class_get_exception_data (MonoClass *klass)
{
	return mono_image_property_lookup (klass->image, klass, MONO_CLASS_PROP_EXCEPTION_DATA);
}

/**
 * mono_classes_init:
 *
 * Initialize the resources used by this module.
 */
void
mono_classes_init (void)
{
	mono_os_mutex_init (&classes_mutex);

	mono_counters_register ("Inflated methods size",
							MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &inflated_methods_size);
	mono_counters_register ("Inflated classes",
							MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &inflated_classes);
	mono_counters_register ("Inflated classes size",
							MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &inflated_classes_size);
	mono_counters_register ("MonoClass size",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &classes_size);
	mono_counters_register ("MonoClassExt size",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_ext_size);
}

/**
 * mono_classes_cleanup:
 *
 * Free the resources used by this module.
 */
void
mono_classes_cleanup (void)
{
	if (global_interface_bitset)
		mono_bitset_free (global_interface_bitset);
	global_interface_bitset = NULL;
	mono_os_mutex_destroy (&classes_mutex);
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
	gpointer exception_data = mono_class_get_exception_data (klass);

	switch (mono_class_get_failure(klass)) {
	case MONO_EXCEPTION_TYPE_LOAD: {
		MonoString *name;
		MonoException *ex;
		char *str = mono_type_get_full_name (klass);
		char *astr = klass->image->assembly? mono_stringify_assembly_name (&klass->image->assembly->aname): NULL;
		name = mono_string_new (mono_domain_get (), str);
		g_free (str);
		ex = mono_get_exception_type_load (name, astr);
		g_free (astr);
		return ex;
	}
	case MONO_EXCEPTION_MISSING_METHOD: {
		char *class_name = (char *)exception_data;
		char *assembly_name = class_name + strlen (class_name) + 1;

		return mono_get_exception_missing_method (class_name, assembly_name);
	}
	case MONO_EXCEPTION_MISSING_FIELD: {
		char *class_name = (char *)exception_data;
		char *member_name = class_name + strlen (class_name) + 1;

		return mono_get_exception_missing_field (class_name, member_name);
	}
	case MONO_EXCEPTION_FILE_NOT_FOUND: {
		char *msg_format = (char *)exception_data;
		char *assembly_name = msg_format + strlen (msg_format) + 1;
		char *msg = g_strdup_printf (msg_format, assembly_name);
		MonoException *ex;

		ex = mono_get_exception_file_not_found2 (msg, mono_string_new (mono_domain_get (), assembly_name));

		g_free (msg);

		return ex;
	}
	case MONO_EXCEPTION_BAD_IMAGE: {
		return mono_get_exception_bad_image_format ((const char *)exception_data);
	}
	default: {
		MonoLoaderError *error;
		MonoException *ex;
		
		error = mono_loader_get_last_error ();
		if (error != NULL){
			ex = mono_loader_error_prepare_exception (error);
			return ex;
		}
		
		/* TODO - handle other class related failures */
		return NULL;
	}
	}
}

static gboolean
is_nesting_type (MonoClass *outer_klass, MonoClass *inner_klass)
 {
	outer_klass = mono_class_get_generic_type_definition (outer_klass);
	inner_klass = mono_class_get_generic_type_definition (inner_klass);
	do {
		if (outer_klass == inner_klass)
			return TRUE;
		inner_klass = inner_klass->nested_in;
	} while (inner_klass);
	return FALSE;
}

MonoClass *
mono_class_get_generic_type_definition (MonoClass *klass)
{
	return klass->generic_class ? klass->generic_class->container_class : klass;
}

/*
 * Check if @klass is a subtype of @parent ignoring generic instantiations.
 * 
 * Generic instantiations are ignored for all super types of @klass.
 * 
 * Visibility checks ignoring generic instantiations.  
 */
gboolean
mono_class_has_parent_and_ignore_generics (MonoClass *klass, MonoClass *parent)
{
	int i;
	klass = mono_class_get_generic_type_definition (klass);
	parent = mono_class_get_generic_type_definition (parent);
	mono_class_setup_supertypes (klass);

	for (i = 0; i < klass->idepth; ++i) {
		if (parent == mono_class_get_generic_type_definition (klass->supertypes [i]))
			return TRUE;
	}
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
	if (!mono_class_has_parent_and_ignore_generics (access_klass, member_klass))
		return FALSE;

	if (context_klass == NULL)
		return TRUE;
	/*if access_klass is not member_klass context_klass must be type compat*/
	if (access_klass != member_klass && !mono_class_has_parent_and_ignore_generics (context_klass, access_klass))
		return FALSE;
	return TRUE;
}

static gboolean
can_access_internals (MonoAssembly *accessing, MonoAssembly* accessed)
{
	GSList *tmp;
	if (accessing == accessed)
		return TRUE;
	if (!accessed || !accessing)
		return FALSE;

	/* extra safety under CoreCLR - the runtime does not verify the strongname signatures
	 * anywhere so untrusted friends are not safe to access platform's code internals */
	if (mono_security_core_clr_enabled ()) {
		if (!mono_security_core_clr_can_access_internals (accessing->image, accessed->image))
			return FALSE;
	}

	mono_assembly_load_friends (accessed);
	for (tmp = accessed->friend_assembly_names; tmp; tmp = tmp->next) {
		MonoAssemblyName *friend_ = (MonoAssemblyName *)tmp->data;
		/* Be conservative with checks */
		if (!friend_->name)
			continue;
		if (strcmp (accessing->aname.name, friend_->name))
			continue;
		if (friend_->public_key_token [0]) {
			if (!accessing->aname.public_key_token [0])
				continue;
			if (!mono_public_tokens_are_equal (friend_->public_key_token, accessing->aname.public_key_token))
				continue;
		}
		return TRUE;
	}
	return FALSE;
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
		if (klass->generic_class && klass->generic_class->container_class)
			return klass->generic_class->container_class;
		klass = klass->parent;
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
			if (!can_access_type (access_klass, mono_class_from_mono_type (type->data.type)))
				return FALSE;
			break;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			if (!can_access_type (access_klass, mono_class_from_mono_type (type)))
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

	if (access_klass->image->assembly && access_klass->image->assembly->corlib_internal)
		return TRUE;

	if (access_klass->element_class && !access_klass->enumtype)
		access_klass = access_klass->element_class;

	if (member_klass->element_class && !member_klass->enumtype)
		member_klass = member_klass->element_class;

	access_level = member_klass->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;

	if (member_klass->byval_arg.type == MONO_TYPE_VAR || member_klass->byval_arg.type == MONO_TYPE_MVAR)
		return TRUE;

	if (member_klass->generic_class && !can_access_instantiation (access_klass, member_klass->generic_class->context.class_inst))
		return FALSE;

	if (is_nesting_type (access_klass, member_klass) || (access_klass->nested_in && is_nesting_type (access_klass->nested_in, member_klass)))
		return TRUE;

	if (member_klass->nested_in && !can_access_type (access_klass, member_klass->nested_in))
		return FALSE;

	/*Non nested type with nested visibility. We just fail it.*/
	if (access_level >= TYPE_ATTRIBUTE_NESTED_PRIVATE && access_level <= TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM && member_klass->nested_in == NULL)
		return FALSE;

	switch (access_level) {
	case TYPE_ATTRIBUTE_NOT_PUBLIC:
		return can_access_internals (access_klass->image->assembly, member_klass->image->assembly);

	case TYPE_ATTRIBUTE_PUBLIC:
		return TRUE;

	case TYPE_ATTRIBUTE_NESTED_PUBLIC:
		return TRUE;

	case TYPE_ATTRIBUTE_NESTED_PRIVATE:
		return is_nesting_type (member_klass, access_klass);

	case TYPE_ATTRIBUTE_NESTED_FAMILY:
		return mono_class_has_parent_and_ignore_generics (access_klass, member_klass->nested_in); 

	case TYPE_ATTRIBUTE_NESTED_ASSEMBLY:
		return can_access_internals (access_klass->image->assembly, member_klass->image->assembly);

	case TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM:
		return can_access_internals (access_klass->image->assembly, member_klass->nested_in->image->assembly) &&
			mono_class_has_parent_and_ignore_generics (access_klass, member_klass->nested_in);

	case TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM:
		return can_access_internals (access_klass->image->assembly, member_klass->nested_in->image->assembly) ||
			mono_class_has_parent_and_ignore_generics (access_klass, member_klass->nested_in);
	}
	return FALSE;
}

/* FIXME: check visibility of type, too */
static gboolean
can_access_member (MonoClass *access_klass, MonoClass *member_klass, MonoClass* context_klass, int access_level)
{
	MonoClass *member_generic_def;
	if (access_klass->image->assembly && access_klass->image->assembly->corlib_internal)
		return TRUE;

	if (((access_klass->generic_class && access_klass->generic_class->container_class) ||
					access_klass->generic_container) && 
			(member_generic_def = get_generic_definition_class (member_klass))) {
		MonoClass *access_container;

		if (access_klass->generic_container)
			access_container = access_klass;
		else
			access_container = access_klass->generic_class->container_class;

		if (can_access_member (access_container, member_generic_def, context_klass, access_level))
			return TRUE;
	}

	/* Partition I 8.5.3.2 */
	/* the access level values are the same for fields and methods */
	switch (access_level) {
	case FIELD_ATTRIBUTE_COMPILER_CONTROLLED:
		/* same compilation unit */
		return access_klass->image == member_klass->image;
	case FIELD_ATTRIBUTE_PRIVATE:
		return access_klass == member_klass;
	case FIELD_ATTRIBUTE_FAM_AND_ASSEM:
		if (is_valid_family_access (access_klass, member_klass, context_klass) &&
		    can_access_internals (access_klass->image->assembly, member_klass->image->assembly))
			return TRUE;
		return FALSE;
	case FIELD_ATTRIBUTE_ASSEMBLY:
		return can_access_internals (access_klass->image->assembly, member_klass->image->assembly);
	case FIELD_ATTRIBUTE_FAMILY:
		if (is_valid_family_access (access_klass, member_klass, context_klass))
			return TRUE;
		return FALSE;
	case FIELD_ATTRIBUTE_FAM_OR_ASSEM:
		if (is_valid_family_access (access_klass, member_klass, context_klass))
			return TRUE;
		return can_access_internals (access_klass->image->assembly, member_klass->image->assembly);
	case FIELD_ATTRIBUTE_PUBLIC:
		return TRUE;
	}
	return FALSE;
}

/**
 * mono_method_can_access_field:
 * @method: Method that will attempt to access the field
 * @field: the field to access
 *
 * Used to determine if a method is allowed to access the specified field.
 *
 * Returns: TRUE if the given @method is allowed to access the @field while following
 * the accessibility rules of the CLI.
 */
gboolean
mono_method_can_access_field (MonoMethod *method, MonoClassField *field)
{
	/* FIXME: check all overlapping fields */
	int can = can_access_member (method->klass, field->parent, NULL, mono_field_get_type (field)->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = method->klass->nested_in;
		while (nested) {
			can = can_access_member (nested, field->parent, NULL, mono_field_get_type (field)->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
			if (can)
				return TRUE;
			nested = nested->nested_in;
		}
	}
	return can;
}

/**
 * mono_method_can_access_method:
 * @method: Method that will attempt to access the other method
 * @called: the method that we want to probe for accessibility.
 *
 * Used to determine if the @method is allowed to access the specified @called method.
 *
 * Returns: TRUE if the given @method is allowed to invoke the @called while following
 * the accessibility rules of the CLI.
 */
gboolean
mono_method_can_access_method (MonoMethod *method, MonoMethod *called)
{
	int can = can_access_member (method->klass, called->klass, NULL, called->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = method->klass->nested_in;
		while (nested) {
			can = can_access_member (nested, called->klass, NULL, called->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK);
			if (can)
				return TRUE;
			nested = nested->nested_in;
		}
	}
	/* 
	 * FIXME:
	 * with generics calls to explicit interface implementations can be expressed
	 * directly: the method is private, but we must allow it. This may be opening
	 * a hole or the generics code should handle this differently.
	 * Maybe just ensure the interface type is public.
	 */
	if ((called->flags & METHOD_ATTRIBUTE_VIRTUAL) && (called->flags & METHOD_ATTRIBUTE_FINAL))
		return TRUE;
	return can;
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
	MonoClass *access_class = method->klass;
	MonoClass *member_class = called->klass;
	int can = can_access_member (access_class, member_class, context_klass, called->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = access_class->nested_in;
		while (nested) {
			can = can_access_member (nested, member_class, context_klass, called->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK);
			if (can)
				break;
			nested = nested->nested_in;
		}
	}

	if (!can)
		return FALSE;

	can = can_access_type (access_class, member_class);
	if (!can) {
		MonoClass *nested = access_class->nested_in;
		while (nested) {
			can = can_access_type (nested, member_class);
			if (can)
				break;
			nested = nested->nested_in;
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
	MonoClass *member_class = field->parent;
	/* FIXME: check all overlapping fields */
	int can = can_access_member (access_class, member_class, context_klass, field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = access_class->nested_in;
		while (nested) {
			can = can_access_member (nested, member_class, context_klass, field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
			if (can)
				break;
			nested = nested->nested_in;
		}
	}

	if (!can)
		return FALSE;

	can = can_access_type (access_class, member_class);
	if (!can) {
		MonoClass *nested = access_class->nested_in;
		while (nested) {
			can = can_access_type (nested, member_class);
			if (can)
				break;
			nested = nested->nested_in;
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
 * @type: The MonoType to check
 *
 * Returns: TRUE if the type can be used as the basetype of an enum
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
		return TRUE;
	default:
		return FALSE;
	}
}

/**
 * mono_class_is_valid_enum:
 * @klass: An enum class to be validated
 *
 * This method verify the required properties an enum should have.
 *  
 * Returns: TRUE if the informed enum class is valid 
 *
 * FIXME: TypeBuilder enums are allowed to implement interfaces, but since they cannot have methods, only empty interfaces are possible
 * FIXME: enum types are not allowed to have a cctor, but mono_reflection_create_runtime_class sets has_cctor to 1 for all types
 * FIXME: TypeBuilder enums can have any kind of static fields, but the spec is very explicit about that (P II 14.3)
 */
gboolean mono_class_is_valid_enum (MonoClass *klass) {
	MonoClassField * field;
	gpointer iter = NULL;
	gboolean found_base_field = FALSE;

	g_assert (klass->enumtype);
	/* we cannot test against mono_defaults.enum_class, or mcs won't be able to compile the System namespace*/
	if (!klass->parent || strcmp (klass->parent->name, "Enum") || strcmp (klass->parent->name_space, "System") ) {
		return FALSE;
	}

	if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) != TYPE_ATTRIBUTE_AUTO_LAYOUT)
		return FALSE;

	while ((field = mono_class_get_fields (klass, &iter))) {
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

	if (klass->method.count > 0) 
		return FALSE;

	return TRUE;
}

gboolean
mono_generic_class_is_generic_type_definition (MonoGenericClass *gklass)
{
	return gklass->context.class_inst == gklass->container_class->generic_container->context.class_inst;
}

/*
 * mono_class_setup_interface_id:
 *
 * Initializes MonoClass::interface_id if required.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_interface_id (MonoClass *klass)
{
	mono_loader_lock ();
	if (MONO_CLASS_IS_INTERFACE (klass) && !klass->interface_id)
		klass->interface_id = mono_get_unique_iid (klass);
	mono_loader_unlock ();
}

/*
 * mono_class_alloc_ext:
 *
 *   Allocate klass->ext if not already done.
 */
void
mono_class_alloc_ext (MonoClass *klass)
{
	MonoClassExt *ext;

	if (klass->ext)
		return;

	ext = (MonoClassExt *)mono_class_alloc0 (klass, sizeof (MonoClassExt));
	mono_image_lock (klass->image);
	mono_memory_barrier ();
	if (!klass->ext)
		klass->ext = ext;
	class_ext_size += sizeof (MonoClassExt);
	mono_image_unlock (klass->image);
}

/*
 * mono_class_setup_interfaces:
 *
 *   Initialize klass->interfaces/interfaces_count.
 * LOCKING: Acquires the loader lock.
 * This function can fail the type.
 */
void
mono_class_setup_interfaces (MonoClass *klass, MonoError *error)
{
	int i, interface_count;
	MonoClass **interfaces;

	mono_error_init (error);

	if (klass->interfaces_inited)
		return;

	if (klass->rank == 1 && klass->byval_arg.type != MONO_TYPE_ARRAY) {
		MonoType *args [1];

		/* generic IList, ICollection, IEnumerable */
		interface_count = mono_defaults.generic_ireadonlylist_class ? 2 : 1;
		interfaces = (MonoClass **)mono_image_alloc0 (klass->image, sizeof (MonoClass*) * interface_count);

		args [0] = &klass->element_class->byval_arg;
		interfaces [0] = mono_class_bind_generic_parameters (
			mono_defaults.generic_ilist_class, 1, args, FALSE);
		if (interface_count > 1)
			interfaces [1] = mono_class_bind_generic_parameters (
			   mono_defaults.generic_ireadonlylist_class, 1, args, FALSE);
	} else if (klass->generic_class) {
		MonoClass *gklass = klass->generic_class->container_class;

		mono_class_setup_interfaces (gklass, error);
		if (!mono_error_ok (error)) {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Could not setup the interfaces"));
			return;
		}

		interface_count = gklass->interface_count;
		interfaces = mono_class_new0 (klass, MonoClass *, interface_count);
		for (i = 0; i < interface_count; i++) {
			interfaces [i] = mono_class_inflate_generic_class_checked (gklass->interfaces [i], mono_generic_class_get_context (klass->generic_class), error);
			if (!mono_error_ok (error)) {
				mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup ("Could not setup the interfaces"));
				return;
			}
		}
	} else {
		interface_count = 0;
		interfaces = NULL;
	}

	mono_image_lock (klass->image);

	if (!klass->interfaces_inited) {
		klass->interface_count = interface_count;
		klass->interfaces = interfaces;

		mono_memory_barrier ();

		klass->interfaces_inited = TRUE;
	}

	mono_image_unlock (klass->image);
}

static void
mono_field_resolve_type (MonoClassField *field, MonoError *error)
{
	MonoClass *klass = field->parent;
	MonoImage *image = klass->image;
	MonoClass *gtd = klass->generic_class ? mono_class_get_generic_type_definition (klass) : NULL;
	int field_idx = field - klass->fields;

	mono_error_init (error);

	if (gtd) {
		MonoClassField *gfield = &gtd->fields [field_idx];
		MonoType *gtype = mono_field_get_type_checked (gfield, error);
		if (!mono_error_ok (error)) {
			char *err_msg = g_strdup_printf ("Could not load field %d type due to: %s", field_idx, mono_error_get_message (error));
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, err_msg);
		}

		field->type = mono_class_inflate_generic_type_no_copy (image, gtype, mono_class_get_context (klass), error);
		if (!mono_error_ok (error)) {
			char *err_msg = g_strdup_printf ("Could not load field %d type due to: %s", field_idx, mono_error_get_message (error));
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, err_msg);
		}
	} else {
		const char *sig;
		guint32 cols [MONO_FIELD_SIZE];
		MonoGenericContainer *container = NULL;
		int idx = klass->field.first + field_idx;

		/*FIXME, in theory we do not lazy load SRE fields*/
		g_assert (!image_is_dynamic (image));

		if (klass->generic_container) {
			container = klass->generic_container;
		} else if (gtd) {
			container = gtd->generic_container;
			g_assert (container);
		}

		/* klass->field.first and idx points into the fieldptr table */
		mono_metadata_decode_table_row (image, MONO_TABLE_FIELD, idx, cols, MONO_FIELD_SIZE);

		if (!mono_verifier_verify_field_signature (image, cols [MONO_FIELD_SIGNATURE], NULL)) {
			mono_error_set_type_load_class (error, klass, "Could not verify field %s signature", field->name);;
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, g_strdup (mono_error_get_message (error)));
			return;
		}

		sig = mono_metadata_blob_heap (image, cols [MONO_FIELD_SIGNATURE]);

		mono_metadata_decode_value (sig, &sig);
		/* FIELD signature == 0x06 */
		g_assert (*sig == 0x06);

		field->type = mono_metadata_parse_type_checked (image, container, cols [MONO_FIELD_FLAGS], FALSE, sig + 1, &sig, error);
		if (!field->type) {
			char *err_msg = g_strdup_printf ("Could not load field %d type due to: %s", field_idx, mono_error_get_message (error));
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, err_msg);
		}
	}
}

static guint32
mono_field_resolve_flags (MonoClassField *field)
{
	MonoClass *klass = field->parent;
	MonoImage *image = klass->image;
	MonoClass *gtd = klass->generic_class ? mono_class_get_generic_type_definition (klass) : NULL;
	int field_idx = field - klass->fields;


	if (gtd) {
		MonoClassField *gfield = &gtd->fields [field_idx];
		return mono_field_get_flags (gfield);
	} else {
		int idx = klass->field.first + field_idx;

		/*FIXME, in theory we do not lazy load SRE fields*/
		g_assert (!image_is_dynamic (image));

		return mono_metadata_decode_table_row_col (image, MONO_TABLE_FIELD, idx, MONO_FIELD_FLAGS);
	}
}

/**
 * mono_class_setup_basic_field_info:
 * @class: The class to initialize
 *
 * Initializes the klass->fields array of fields.
 * Aquires the loader lock.
 */
static void
mono_class_setup_basic_field_info_locking (MonoClass *klass)
{
	mono_loader_lock ();
	mono_class_setup_basic_field_info (klass);
	mono_loader_unlock ();
}

/**
 * mono_class_get_fields_lazy:
 * @klass: the MonoClass to act on
 *
 * This routine is an iterator routine for retrieving the fields in a class.
 * Only minimal information about fields are loaded. Accessors must be used
 * for all MonoClassField returned.
 *
 * You must pass a gpointer that points to zero and is treated as an opaque handle to
 * iterate over all of the elements.  When no more values are
 * available, the return value is NULL.
 *
 * Returns: a @MonoClassField* on each iteration, or NULL when no more fields are available.
 */
MonoClassField*
mono_class_get_fields_lazy (MonoClass* klass, gpointer *iter)
{
	MonoClassField* field;
	if (!iter)
		return NULL;
	if (!*iter) {
		mono_class_setup_basic_field_info_locking (klass);
		if (!klass->fields)
			return NULL;
		/* start from the first */
		if (klass->field.count) {
			*iter = &klass->fields [0];
			return (MonoClassField *)*iter;
		} else {
			/* no fields */
			return NULL;
		}
	}
	field = (MonoClassField *)*iter;
	field++;
	if (field < &klass->fields [klass->field.count]) {
		*iter = field;
		return (MonoClassField *)*iter;
	}
	return NULL;
}

char*
mono_class_full_name (MonoClass *klass)
{
	return mono_type_full_name (&klass->byval_arg);
}

/* Declare all shared lazy type lookup functions */
GENERATE_TRY_GET_CLASS_WITH_CACHE (safehandle, System.Runtime.InteropServices, SafeHandle)
