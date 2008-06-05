/*
 * class.c: Class management for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001-2006 Novell, Inc.
 *
 */
#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <signal.h>
#if !PLATFORM_WIN32
#include <mono/io-layer/atomic.h>
#endif
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
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/verify-internals.h>
#include <mono/utils/mono-counters.h>

MonoStats mono_stats;

gboolean mono_print_vtable = FALSE;

/*
 * Controls whenever mono_class_init () constructs a generic vtable. This is TRUE by
 * default to avoid breaking embedding apps, but set to FALSE by the runtime executable
 * startup code.
 */
gboolean mono_setup_vtable_in_class_init = TRUE;

/* Function supplied by the runtime to find classes by name using information from the AOT file */
static MonoGetClassFromName get_class_from_name = NULL;

static MonoClass * mono_class_create_from_typedef (MonoImage *image, guint32 type_token);
static gboolean mono_class_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res);
static gboolean can_access_type (MonoClass *access_klass, MonoClass *member_klass);
static MonoMethod* find_method_in_metadata (MonoClass *klass, const char *name, int param_count, int flags);
static int generic_array_methods (MonoClass *class);
static void setup_generic_array_ifaces (MonoClass *class, MonoClass *iface, MonoMethod **methods, int pos);

void (*mono_debugger_class_init_func) (MonoClass *klass) = NULL;
void (*mono_debugger_class_loaded_methods_func) (MonoClass *klass) = NULL;

/*
 * mono_class_from_typeref:
 * @image: a MonoImage
 * @type_token: a TypeRef token
 *
 * Creates the MonoClass* structure representing the type defined by
 * the typeref token valid inside @image.
 * Returns: the MonoClass* representing the typeref token, NULL ifcould
 * not be loaded.
 */
MonoClass *
mono_class_from_typeref (MonoImage *image, guint32 type_token)
{
	guint32 cols [MONO_TYPEREF_SIZE];
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
	guint32 idx;
	const char *name, *nspace;
	MonoClass *res;
	MonoImage *module;
	
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
		module = mono_image_load_module (image, idx);
		if (module)
			return mono_class_from_name (module, nspace, name);
		else {
			char *msg = g_strdup_printf ("%s%s%s", nspace, nspace [0] ? "." : "", name);
			char *human_name;
			
			human_name = mono_stringify_assembly_name (&image->assembly->aname);
			mono_loader_set_error_type_load (msg, human_name);
			g_free (msg);
			g_free (human_name);
		
			return NULL;
		}
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
		g_warning ("TypeRef ResolutionScope not yet handled (%d) for %s.%s in image %s", idx, nspace, name, image->name);
		return NULL;
	}
	case MONO_RESOLTION_SCOPE_ASSEMBLYREF:
		break;
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
		mono_loader_set_error_assembly_load (human_name, image->assembly->ref_only);
		g_free (human_name);
		
		return NULL;
	}

	return mono_class_from_name (image->references [idx - 1]->image, nspace, name);
}


static void *
mono_mempool_dup (MonoMemPool *mp, void *data, guint size)
{
	void *res = mono_mempool_alloc (mp, size);
	memcpy (res, data, size);
	return res;
}
	
/* Copy everything mono_metadata_free_array free. */
MonoArrayType *
mono_dup_array_type (MonoMemPool *mp, MonoArrayType *a)
{
	if (mp) {
		mono_loader_lock ();
		a = mono_mempool_dup (mp, a, sizeof (MonoArrayType));
		if (a->sizes)
			a->sizes = mono_mempool_dup (mp, a->sizes, a->numsizes * sizeof (int));
		if (a->lobounds)
			a->lobounds = mono_mempool_dup (mp, a->lobounds, a->numlobounds * sizeof (int));
		mono_loader_unlock ();		
	} else {
		a = g_memdup (a, sizeof (MonoArrayType));
		if (a->sizes)
			a->sizes = g_memdup (a->sizes, a->numsizes * sizeof (int));
		if (a->lobounds)
			a->lobounds = g_memdup (a->lobounds, a->numlobounds * sizeof (int));
	}
	return a;
}

/* Copy everything mono_metadata_free_method_signature free. */
MonoMethodSignature*
mono_metadata_signature_deep_dup (MonoMemPool *mp, MonoMethodSignature *sig)
{
	int i;
	
	sig = mono_metadata_signature_dup_full (mp, sig);
	
	sig->ret = mono_metadata_type_dup (mp, sig->ret);
	for (i = 0; i < sig->param_count; ++i)
		sig->params [i] = mono_metadata_type_dup (mp, sig->params [i]);
	
	return sig;
}

static void
_mono_type_get_assembly_name (MonoClass *klass, GString *str)
{
	MonoAssembly *ta = klass->image->assembly;

	g_string_append_printf (
		str, ", %s, Version=%d.%d.%d.%d, Culture=%s, PublicKeyToken=%s%s",
		ta->aname.name,
		ta->aname.major, ta->aname.minor, ta->aname.build, ta->aname.revision,
		ta->aname.culture && *ta->aname.culture? ta->aname.culture: "neutral",
		ta->aname.public_key_token [0] ? (char *)ta->aname.public_key_token : "null",
		(ta->aname.flags & ASSEMBLYREF_RETARGETABLE_FLAG) ? ", Retargetable=Yes" : "");
}

static inline void
mono_type_name_check_byref (MonoType *type, GString *str)
{
	if (type->byref)
		g_string_append_c (str, '&');
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
		g_assert (type->data.generic_param->name);
		g_string_append (str, type->data.generic_param->name);
	
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
				g_string_append (str, klass->generic_container->type_params [i].name);
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

	result = g_string_new ("");

	mono_type_get_name_recurse (type, result, FALSE, format);

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

/**
 * mono_type_get_name:
 * @type: a type
 *
 * Returns: the string representation for type as it would be represented in IL code.
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
 * Returns: the MonoType for the underlying integer type if @type
 * is an enum and byref is false, otherwise the type itself.
 */
MonoType*
mono_type_get_underlying_type (MonoType *type)
{
	if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype && !type->byref)
		return type->data.klass->enum_basetype;
	if (type->type == MONO_TYPE_GENERICINST && type->data.generic_class->container_class->enumtype && !type->byref)
		return type->data.generic_class->container_class->enum_basetype;
	return type;
}

/*
 * mono_class_is_open_constructed_type:
 * @type: a type
 *
 * Returns TRUE if type represents a generics open constructed type
 * (not all the type parameters required for the instantiation have
 * been provided).
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
	default:
		return FALSE;
	}
}

static MonoType*
inflate_generic_type (MonoType *type, MonoGenericContext *context)
{
	switch (type->type) {
	case MONO_TYPE_MVAR: {
		MonoType *nt;
		int num = type->data.generic_param->num;
		MonoGenericInst *inst = context->method_inst;
		if (!inst || !inst->type_argv)
			return NULL;
		if (num >= inst->type_argc)
			g_error ("MVAR %d (%s) cannot be expanded in this context with %d instantiations", num, type->data.generic_param->name, inst->type_argc);

		/*
		 * Note that the VAR/MVAR cases are different from the rest.  The other cases duplicate @type,
		 * while the VAR/MVAR duplicates a type from the context.  So, we need to ensure that the
		 * ->byref and ->attrs from @type are propagated to the returned type.
		 */
		nt = mono_metadata_type_dup (NULL, inst->type_argv [num]);
		nt->byref = type->byref;
		nt->attrs = type->attrs;
		return nt;
	}
	case MONO_TYPE_VAR: {
		MonoType *nt;
		int num = type->data.generic_param->num;
		MonoGenericInst *inst = context->class_inst;
		if (!inst)
			return NULL;
		if (num >= inst->type_argc)
			g_error ("VAR %d (%s) cannot be expanded in this context with %d instantiations", num, type->data.generic_param->name, inst->type_argc);
		nt = mono_metadata_type_dup (NULL, inst->type_argv [num]);
		nt->byref = type->byref;
		nt->attrs = type->attrs;
		return nt;
	}
	case MONO_TYPE_SZARRAY: {
		MonoClass *eclass = type->data.klass;
		MonoType *nt, *inflated = inflate_generic_type (&eclass->byval_arg, context);
		if (!inflated)
			return NULL;
		nt = mono_metadata_type_dup (NULL, type);
		nt->data.klass = mono_class_from_mono_type (inflated);
		mono_metadata_free_type (inflated);
		return nt;
	}
	case MONO_TYPE_ARRAY: {
		MonoClass *eclass = type->data.array->eklass;
		MonoType *nt, *inflated = inflate_generic_type (&eclass->byval_arg, context);
		if (!inflated)
			return NULL;
		nt = mono_metadata_type_dup (NULL, type);
		nt->data.array = g_memdup (nt->data.array, sizeof (MonoArrayType));
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

		inst = mono_metadata_inflate_generic_inst (gclass->context.class_inst, context);
		if (inst != gclass->context.class_inst)
			gclass = mono_metadata_lookup_generic_class (gclass->container_class, inst, gclass->is_dynamic);

		if (gclass == type->data.generic_class)
			return NULL;

		nt = mono_metadata_type_dup (NULL, type);
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
		inst = mono_metadata_inflate_generic_inst (container->context.class_inst, context);
		if (inst == container->context.class_inst)
			return NULL;

		gclass = mono_metadata_lookup_generic_class (klass, inst, klass->image->dynamic);

		nt = mono_metadata_type_dup (NULL, type);
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
mono_class_get_context (MonoClass *class)
{
       return class->generic_class ? mono_generic_class_get_context (class->generic_class) : NULL;
}

/*
 * mono_class_inflate_generic_type:
 * @type: a type
 * @context: a generics context
 *
 * If @type is a generic type and @context is not NULL, instantiate it using the 
 * generics context @context.
 *
 * Returns: the instantiated type or a copy of @type. The returned MonoType is allocated
 * on the heap and is owned by the caller.
 */
MonoType*
mono_class_inflate_generic_type (MonoType *type, MonoGenericContext *context)
{
	MonoType *inflated = NULL; 

	if (context)
		inflated = inflate_generic_type (type, context);

	if (!inflated)
		return mono_metadata_type_dup (NULL, type);

	mono_stats.inflated_type_count++;
	return inflated;
}

static MonoGenericContext
inflate_generic_context (MonoGenericContext *context, MonoGenericContext *inflate_with)
{
	MonoGenericInst *class_inst = NULL;
	MonoGenericInst *method_inst = NULL;
	MonoGenericContext res;

	if (context->class_inst)
		class_inst = mono_metadata_inflate_generic_inst (context->class_inst, inflate_with);

	if (context->method_inst)
		method_inst = mono_metadata_inflate_generic_inst (context->method_inst, inflate_with);

	res.class_inst = class_inst;
	res.method_inst = method_inst;

	return res;
}

/*
 * mono_class_inflate_generic_method:
 * @method: a generic method
 * @context: a generics context
 *
 * Instantiate the generic method @method using the generics context @context.
 *
 * Returns: the new instantiated method
 */
MonoMethod *
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericContext *context)
{
	return mono_class_inflate_generic_method_full (method, NULL, context);
}

/**
 * mono_class_inflate_generic_method:
 *
 * Instantiate method @method with the generic context @context.
 * BEWARE: All non-trivial fields are invalid, including klass, signature, and header.
 *         Use mono_method_signature () and mono_method_get_header () to get the correct values.
 */
MonoMethod*
mono_class_inflate_generic_method_full (MonoMethod *method, MonoClass *klass_hint, MonoGenericContext *context)
{
	MonoMethod *result;
	MonoMethodInflated *iresult, *cached;
	MonoMethodSignature *sig;
	MonoGenericContext tmp_context;
	gboolean is_mb_open = FALSE;

	/* The `method' has already been instantiated before => we need to peel out the instantiation and create a new context */
	while (method->is_inflated) {
		MonoGenericContext *method_context = mono_method_get_context (method);
		MonoMethodInflated *imethod = (MonoMethodInflated *) method;

		tmp_context = inflate_generic_context (method_context, context);
		context = &tmp_context;

		if (mono_metadata_generic_context_equal (method_context, context))
			return method;

		method = imethod->declaring;
	}

	if (!method->is_generic && !method->klass->generic_container)
		return method;

	/*
	 * The reason for this hack is to fix the behavior of inflating generic methods that come from a MethodBuilder.
	 * What happens is that instantiating a generic MethodBuilder with its own arguments should create a diferent object.
	 * This is opposite to the way non-SRE MethodInfos behave.
	 * 
	 * This happens, for example, when we want to emit a recursive generic method. Given the following C# code:
	 * 
	 * void Example<T> () {
	 *    Example<T> ();
	 * }
	 *  
	 * In Example, the method token must be encoded as: "void Example<!!0>()"
	 * 
	 * The reference to the first generic argument, "!!0", must be explicit otherwise it won't be inflated
	 * properly. To get that we need to inflate the MethodBuilder with its own arguments.
	 * 
	 * On the other hand, inflating a non-SRE generic method with its own arguments should
	 * return itself. For example:
	 * 
	 * MethodInfo m = ... //m is a generic method definition
	 * MethodInfo res = m.MakeGenericMethod (m.GetGenericArguments ());
	 * res == m
	 *
	 * To allow such scenarios we must allow inflation of MethodBuilder to happen in a diferent way than
	 * what happens with regular methods.
	 * 
	 * There is one last touch to this madness, once a TypeBuilder is finished, IOW CreateType() is called,
	 * everything should behave like a regular type or method.
	 * 
	 */
	is_mb_open = method->is_generic &&
		method->klass->image->dynamic && !method->klass->wastypebuilder && /* that is a MethodBuilder from an unfinished TypeBuilder */
		context->method_inst == mono_method_get_generic_container (method)->context.method_inst; /* and it's been instantiated with its own arguments.  */

	iresult = g_new0 (MonoMethodInflated, 1);
	iresult->context = *context;
	iresult->declaring = method;
	iresult->is_mb_open = is_mb_open;

	if (!context->method_inst && method->is_generic)
		iresult->context.method_inst = mono_method_get_generic_container (method)->context.method_inst;

	mono_loader_lock ();
	cached = mono_method_inflated_lookup (iresult, FALSE);
	if (cached) {
		mono_loader_unlock ();
		g_free (iresult);
		return (MonoMethod*)cached;
	}

	mono_stats.inflated_method_count++;

	sig = mono_method_signature (method);
	if (sig->pinvoke) {
		memcpy (&iresult->method.pinvoke, method, sizeof (MonoMethodPInvoke));
	} else {
		memcpy (&iresult->method.normal, method, sizeof (MonoMethodNormal));
		iresult->method.normal.header = NULL;
	}

	result = (MonoMethod *) iresult;
	result->is_inflated = TRUE;
	result->is_generic = FALSE;
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
		MonoType *inflated = inflate_generic_type (&method->klass->byval_arg, context);
		result->klass = inflated ? mono_class_from_mono_type (inflated) : method->klass;
		if (inflated)
			mono_metadata_free_type (inflated);
	}

	mono_method_inflated_lookup (iresult, TRUE);
	mono_loader_unlock ();
	return result;
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

MonoGenericContext*
mono_method_get_context (MonoMethod *method)
{
	MonoMethodInflated *imethod;
	if (!method->is_inflated)
		return NULL;
	imethod = (MonoMethodInflated *) method;
	return &imethod->context;
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

	mono_loader_lock ();
	container = mono_property_hash_lookup (method->klass->image->property_hash, method, MONO_METHOD_PROP_GENERIC_CONTAINER);
	mono_loader_unlock ();
	g_assert (container);

	return container;
}

/*
 * mono_method_set_generic_container:
 *
 *   Sets the generic container of METHOD to CONTAINER.
 * LOCKING: Acquires the loader lock.
 */
void
mono_method_set_generic_container (MonoMethod *method, MonoGenericContainer* container)
{
	g_assert (method->is_generic);

	mono_loader_lock ();
	mono_property_hash_insert (method->klass->image->property_hash, method, MONO_METHOD_PROP_GENERIC_CONTAINER, container);
	mono_loader_unlock ();
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

		/* class->field.first and idx points into the fieldptr table */
		mono_metadata_decode_table_row (m, MONO_TABLE_FIELD, idx, cols, MONO_FIELD_SIZE);
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
		ftype = mono_metadata_parse_type_full (m, container, MONO_PARSE_FIELD, cols [MONO_FIELD_FLAGS], sig + 1, &sig);
		if (!ftype)
			return NULL;
		if (class->generic_class) {
			ftype = mono_class_inflate_generic_type (ftype, mono_class_get_context (class));
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
 * LOCKING: Assumes the loader lock is held.
 */
static void
mono_class_setup_fields (MonoClass *class)
{
	MonoImage *m = class->image; 
	int top = class->field.count;
	guint32 layout = class->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	int i, blittable = TRUE;
	guint32 real_size = 0;
	guint32 packing_size = 0;
	gboolean explicit_size;
	MonoClassField *field;
	MonoGenericContainer *container = NULL;
	MonoClass *gklass = NULL;

	if (class->size_inited)
		return;

	if (class->generic_class && class->generic_class->container_class->image->dynamic && !class->generic_class->container_class->wastypebuilder) {
		/*
		 * This happens when a generic instance of an unfinished generic typebuilder
		 * is used as an element type for creating an array type. We can't initialize
		 * the fields of this class using the fields of gklass, since gklass is not
		 * finished yet, fields could be added to it later.
		 */
		return;
	}

	if (class->generic_class) {
		MonoClass *gklass = class->generic_class->container_class;
		mono_class_setup_fields (gklass);
		top = gklass->field.count;
		class->field.count = gklass->field.count;
	}

	class->instance_size = 0;
	if (!class->rank)
		class->sizes.class_size = 0;

	if (class->parent) {
		/* For generic instances, class->parent might not have been initialized */
		mono_class_init (class->parent);
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

	if (class->generic_container) {
		container = class->generic_container;
	} else if (class->generic_class) {
		gklass = class->generic_class->container_class;
		container = gklass->generic_container;
		g_assert (container);

		mono_class_setup_fields (gklass);
	}

	/*
	 * Fetch all the field information.
	 */
	for (i = 0; i < top; i++){
		int idx = class->field.first + i;
		field = &class->fields [i];

		field->parent = class;

		if (class->generic_class) {
			MonoClassField *gfield = &gklass->fields [i];
			MonoInflatedField *ifield = g_new0 (MonoInflatedField, 1);

			ifield->generic_type = gfield->type;
			field->name = gfield->name;
			field->generic_info = ifield;
			field->type = mono_class_inflate_generic_type (gfield->type, mono_class_get_context (class));
			field->type->attrs = gfield->type->attrs;
			if (mono_field_is_deleted (field))
				continue;
			field->offset = gfield->offset;
			field->data = gfield->data;
		} else {
			guint32 rva;
			const char *sig;
			guint32 cols [MONO_FIELD_SIZE];

			/* class->field.first and idx points into the fieldptr table */
			mono_metadata_decode_table_row (m, MONO_TABLE_FIELD, idx, cols, MONO_FIELD_SIZE);
			/* The name is needed for fieldrefs */
			field->name = mono_metadata_string_heap (m, cols [MONO_FIELD_NAME]);
			sig = mono_metadata_blob_heap (m, cols [MONO_FIELD_SIGNATURE]);
			mono_metadata_decode_value (sig, &sig);
			/* FIELD signature == 0x06 */
			g_assert (*sig == 0x06);
			field->type = mono_metadata_parse_type_full (m, container, MONO_PARSE_FIELD, cols [MONO_FIELD_FLAGS], sig + 1, &sig);
			if (!field->type) {
				mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
				break;
			}
			if (mono_field_is_deleted (field))
				continue;
			if (layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
				guint32 offset;
				mono_metadata_field_info (m, idx, &offset, NULL, NULL);
				field->offset = offset;
				if (field->offset == (guint32)-1 && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
					g_warning ("%s not initialized correctly (missing field layout info for %s)",
						   class->name, field->name);
			}

			if (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) {
				mono_metadata_field_info (m, idx, NULL, &rva, NULL);
				if (!rva)
					g_warning ("field %s in %s should have RVA data, but hasn't", field->name, class->name);
				field->data = mono_image_rva_map (class->image, rva);
			}
		}

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

		if (class->enumtype && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
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

	if (class->exception_type)
		return;
	mono_class_layout_fields (class);
}

/** 
 * mono_class_setup_fields_locking:
 * @class: The class to initialize
 *
 * Initializes the class->fields array of fields.
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
#ifdef HAVE_SGEN_GC
#define IS_GC_REFERENCE(t) FALSE
#else
#define IS_GC_REFERENCE(t) ((t)->type == MONO_TYPE_U && class->image == mono_defaults.corlib)
#endif

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
	/* When we do generic sharing we let type variables stand for reference types. */
	if (!type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR))
		return &mono_defaults.object_class->byval_arg;
	return type;
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
mono_class_layout_fields (MonoClass *class)
{
	int i;
	const int top = class->field.count;
	guint32 layout = class->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	guint32 pass, passes, real_size;
	gboolean gc_aware_layout = FALSE;
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
		if (class->image != mono_defaults.corlib &&
			class->byval_arg.type != MONO_TYPE_VALUETYPE)
			gc_aware_layout = TRUE;
		/* from System.dll, used in metadata/process.h */
		if (strcmp (class->name, "ProcessStartInfo") == 0)
			gc_aware_layout = FALSE;
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
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype)))))
				class->has_references = TRUE;
		}
	}

	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &class->fields [i];

		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype)))))
				class->has_static_refs = TRUE;
		}
	}

	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &class->fields [i];

		ftype = mono_type_get_underlying_type (field->type);
		ftype = mono_type_get_basic_type_from_generic (ftype);
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
				gint32 align;
				guint32 size;
				MonoType *ftype;

				field = &class->fields [i];

				if (mono_field_is_deleted (field))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;

				ftype = mono_type_get_underlying_type (field->type);
				ftype = mono_type_get_basic_type_from_generic (ftype);
				if (gc_aware_layout) {
					if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype))))) {
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
				/* if the field has managed references, we need to force-align it
				 * see bug #77788
				 */
				if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype)))))
					align = MAX (align, sizeof (gpointer));

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
			gint32 align;
			guint32 size;
			MonoType *ftype;

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
			class->min_align = MAX (align, class->min_align);

			/*
			 * When we get here, field->offset is already set by the
			 * loader (for either runtime fields or fields loaded from metadata).
			 * The offset is from the start of the object: this works for both
			 * classes and valuetypes.
			 */
			field->offset += sizeof (MonoObject);
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (MONO_TYPE_IS_REFERENCE (ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && mono_class_has_references (mono_class_from_mono_type (ftype))))) {
				if (field->offset % sizeof (gpointer)) {
					mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
				}
			}

			/*
			 * Calc max size.
			 */
			real_size = MAX (real_size, size + field->offset);
		}
		class->instance_size = MAX (real_size, class->instance_size);
		break;
	}

	if (layout != TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
		/*
		 * For small structs, set min_align to at least the struct size to improve
		 * performance, and since the JIT memset/memcpy code assumes this and generates 
		 * unaligned accesses otherwise. See #78990 for a testcase.
		 */
		if (class->instance_size <= sizeof (MonoObject) + sizeof (gpointer))
			class->min_align = MAX (class->min_align, class->instance_size - sizeof (MonoObject));
	}

	class->size_inited = 1;

	/*
	 * Compute static field layout and size
	 */
	for (i = 0; i < top; i++){
		gint32 align;
		guint32 size;

		field = &class->fields [i];
			
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC) || field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
			continue;
		if (mono_field_is_deleted (field))
			continue;

		size = mono_type_size (field->type, &align);
		field->offset = class->sizes.class_size;
		field->offset += align - 1;
		field->offset &= ~(align - 1);
		class->sizes.class_size = field->offset + size;
	}
}

static MonoMethod*
create_array_method (MonoClass *class, const char *name, MonoMethodSignature *sig)
{
	MonoMethod *method;

	method = (MonoMethod *) mono_mempool_alloc0 (class->image->mempool, sizeof (MonoMethodPInvoke));
	method->klass = class;
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
 *   Initializes the 'methods' array in the klass.
 * Calling this method should be avoided if possible since it allocates a lot 
 * of long-living MonoMethod structures.
 * Methods belonging to an interface are assigned a sequential slot starting
 * from 0.
 */
void
mono_class_setup_methods (MonoClass *class)
{
	int i;
	MonoMethod **methods;

	if (class->methods)
		return;

	mono_loader_lock ();

	if (class->methods) {
		mono_loader_unlock ();
		return;
	}

	if (class->generic_class) {
		MonoClass *gklass = class->generic_class->container_class;

		mono_class_init (gklass);
		mono_class_setup_methods (gklass);

		/* The + 1 makes this always non-NULL to pass the check in mono_class_setup_methods () */
		class->method.count = gklass->method.count;
		methods = g_new0 (MonoMethod *, class->method.count + 1);

		for (i = 0; i < class->method.count; i++) {
			methods [i] = mono_class_inflate_generic_method_full (
				gklass->methods [i], class, mono_class_get_context (class));
		}
	} else if (class->rank) {
		MonoMethod *amethod;
		MonoMethodSignature *sig;
		int count_generic = 0, first_generic = 0;
		int method_num = 0;

		class->method.count = 3 + (class->rank > 1? 2: 1);

		if (class->interface_count) {
			count_generic = generic_array_methods (class);
			first_generic = class->method.count;
			class->method.count += class->interface_count * count_generic;
		}

		methods = mono_mempool_alloc0 (class->image->mempool, sizeof (MonoMethod*) * class->method.count);

		sig = mono_metadata_signature_alloc (class->image, class->rank);
		sig->ret = &mono_defaults.void_class->byval_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < class->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;

		amethod = create_array_method (class, ".ctor", sig);
		methods [method_num++] = amethod;
		if (class->rank > 1) {
			sig = mono_metadata_signature_alloc (class->image, class->rank * 2);
			sig->ret = &mono_defaults.void_class->byval_arg;
			sig->pinvoke = TRUE;
			sig->hasthis = TRUE;
			for (i = 0; i < class->rank * 2; ++i)
				sig->params [i] = &mono_defaults.int32_class->byval_arg;

			amethod = create_array_method (class, ".ctor", sig);
			methods [method_num++] = amethod;
		}
		/* element Get (idx11, [idx2, ...]) */
		sig = mono_metadata_signature_alloc (class->image, class->rank);
		sig->ret = &class->element_class->byval_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < class->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;
		amethod = create_array_method (class, "Get", sig);
		methods [method_num++] = amethod;
		/* element& Address (idx11, [idx2, ...]) */
		sig = mono_metadata_signature_alloc (class->image, class->rank);
		sig->ret = &class->element_class->this_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < class->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;
		amethod = create_array_method (class, "Address", sig);
		methods [method_num++] = amethod;
		/* void Set (idx11, [idx2, ...], element) */
		sig = mono_metadata_signature_alloc (class->image, class->rank + 1);
		sig->ret = &mono_defaults.void_class->byval_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < class->rank; ++i)
			sig->params [i] = &mono_defaults.int32_class->byval_arg;
		sig->params [i] = &class->element_class->byval_arg;
		amethod = create_array_method (class, "Set", sig);
		methods [method_num++] = amethod;

		for (i = 0; i < class->interface_count; i++)
			setup_generic_array_ifaces (class, class->interfaces [i], methods, first_generic + i * count_generic);
	} else {
		methods = mono_mempool_alloc (class->image->mempool, sizeof (MonoMethod*) * class->method.count);
		for (i = 0; i < class->method.count; ++i) {
			int idx = mono_metadata_translate_token_index (class->image, MONO_TABLE_METHOD, class->method.first + i + 1);
			methods [i] = mono_get_method (class->image, MONO_TOKEN_METHOD_DEF | idx, class);
		}
	}

	if (MONO_CLASS_IS_INTERFACE (class))
		for (i = 0; i < class->method.count; ++i)
			methods [i]->slot = i;

	/* Needed because of the double-checking locking pattern */
	mono_memory_barrier ();

	class->methods = methods;

	if (mono_debugger_class_loaded_methods_func)
		mono_debugger_class_loaded_methods_func (class);

	mono_loader_unlock ();
}

/*
 * mono_class_get_method_by_index:
 *
 *   Returns class->methods [index], initializing class->methods if neccesary.
 *
 * LOCKING: Acquires the loader lock.
 */
MonoMethod*
mono_class_get_method_by_index (MonoClass *class, int index)
{
	/* Avoid calling setup_methods () if possible */
	if (class->generic_class && !class->methods) {
		MonoClass *gklass = class->generic_class->container_class;
		MonoMethod *m;

		m = mono_class_inflate_generic_method_full (
				gklass->methods [index], class, mono_class_get_context (class));
		/*
		 * If setup_methods () is called later for this class, no duplicates are created,
		 * since inflate_generic_method guarantees that only one instance of a method
		 * is created for each context.
		 */
		/*
		mono_class_setup_methods (class);
		g_assert (m == class->methods [index]);
		*/
		return m;
	} else {
		mono_class_setup_methods (class);
		g_assert (index >= 0 && index < class->method.count);
		return class->methods [index];
	}
}	

static void
mono_class_setup_properties (MonoClass *class)
{
	guint startm, endm, i, j;
	guint32 cols [MONO_PROPERTY_SIZE];
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

	if (class->generic_class) {
		MonoClass *gklass = class->generic_class->container_class;

		class->property = gklass->property;

		mono_class_init (gklass);
		mono_class_setup_properties (gklass);

		properties = g_new0 (MonoProperty, class->property.count + 1);

		for (i = 0; i < class->property.count; i++) {
			MonoProperty *prop = &properties [i];

			*prop = gklass->properties [i];

			if (prop->get)
				prop->get = mono_class_inflate_generic_method_full (
					prop->get, class, mono_class_get_context (class));
			if (prop->set)
				prop->set = mono_class_inflate_generic_method_full (
					prop->set, class, mono_class_get_context (class));

			prop->parent = class;
		}
	} else {
		class->property.first = mono_metadata_properties_from_typedef (class->image, mono_metadata_token_index (class->type_token) - 1, &last);
		class->property.count = last - class->property.first;

		if (class->property.count)
			mono_class_setup_methods (class);

		properties = mono_mempool_alloc0 (class->image->mempool, sizeof (MonoProperty) * class->property.count);
		for (i = class->property.first; i < last; ++i) {
			mono_metadata_decode_table_row (class->image, MONO_TABLE_PROPERTY, i, cols, MONO_PROPERTY_SIZE);
			properties [i - class->property.first].parent = class;
			properties [i - class->property.first].attrs = cols [MONO_PROPERTY_FLAGS];
			properties [i - class->property.first].name = mono_metadata_string_heap (class->image, cols [MONO_PROPERTY_NAME]);

			startm = mono_metadata_methods_from_property (class->image, i, &endm);
			for (j = startm; j < endm; ++j) {
				MonoMethod *method;

				mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);

				if (class->image->uncompressed_metadata)
					/* It seems like the MONO_METHOD_SEMA_METHOD column needs no remapping */
					method = mono_get_method (class->image, MONO_TOKEN_METHOD_DEF | cols [MONO_METHOD_SEMA_METHOD], class);
				else
					method = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];

				switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
				case METHOD_SEMANTIC_SETTER:
					properties [i - class->property.first].set = method;
					break;
				case METHOD_SEMANTIC_GETTER:
					properties [i - class->property.first].get = method;
					break;
				default:
					break;
				}
			}
		}
	}

	/* Leave this assignment as the last op in the function */
	class->properties = properties;

	mono_loader_unlock ();
}

static MonoMethod**
inflate_method_listz (MonoMethod **methods, MonoClass *class, MonoGenericContext *context)
{
	MonoMethod **om, **retval;
	int count;

	for (om = methods, count = 0; *om; ++om, ++count)
		;

	retval = g_new0 (MonoMethod*, count + 1);
	count = 0;
	for (om = methods, count = 0; *om; ++om, ++count)
		retval [count] = mono_class_inflate_generic_method_full (*om, class, context);

	return retval;
}

static void
mono_class_setup_events (MonoClass *class)
{
	guint startm, endm, i, j;
	guint32 cols [MONO_EVENT_SIZE];
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

	if (class->generic_class) {
		MonoClass *gklass = class->generic_class->container_class;
		MonoGenericContext *context;

		mono_class_setup_events (gklass);
		class->event = gklass->event;

		class->events = g_new0 (MonoEvent, class->event.count);

		if (class->event.count)
			context = mono_class_get_context (class);

		for (i = 0; i < class->event.count; i++) {
			MonoEvent *event = &class->events [i];
			MonoEvent *gevent = &gklass->events [i];

			event->parent = class;
			event->name = gevent->name;
			event->add = gevent->add ? mono_class_inflate_generic_method_full (gevent->add, class, context) : NULL;
			event->remove = gevent->remove ? mono_class_inflate_generic_method_full (gevent->remove, class, context) : NULL;
			event->raise = gevent->raise ? mono_class_inflate_generic_method_full (gevent->raise, class, context) : NULL;
			event->other = gevent->other ? inflate_method_listz (gevent->other, class, context) : NULL;
			event->attrs = gevent->attrs;
		}

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

		mono_metadata_decode_table_row (class->image, MONO_TABLE_EVENT, i, cols, MONO_EVENT_SIZE);
		event->parent = class;
		event->attrs = cols [MONO_EVENT_FLAGS];
		event->name = mono_metadata_string_heap (class->image, cols [MONO_EVENT_NAME]);

		startm = mono_metadata_methods_from_event (class->image, i, &endm);
		for (j = startm; j < endm; ++j) {
			MonoMethod *method;

			mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);

			if (class->image->uncompressed_metadata)
				/* It seems like the MONO_METHOD_SEMA_METHOD column needs no remapping */
				method = mono_get_method (class->image, MONO_TOKEN_METHOD_DEF | cols [MONO_METHOD_SEMA_METHOD], class);
			else
				method = class->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - class->method.first];

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
				int n = 0;

				if (event->other == NULL) {
					event->other = g_new0 (MonoMethod*, 2);
				} else {
					while (event->other [n])
						n++;
					event->other = g_realloc (event->other, (n + 2) * sizeof (MonoMethod*));
				}
				event->other [n] = method;
				/* NULL terminated */
				event->other [n + 1] = NULL;
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

/*
 * Global pool of interface IDs, represented as a bitset.
 * LOCKING: this is supposed to be accessed with the loader lock held.
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
	mono_loader_lock ();
	mono_bitset_sub (global_interface_bitset, bitset);
	mono_loader_unlock ();
}

/*
 * mono_get_unique_iid:
 * @class: interface
 *
 * Assign a unique integer ID to the interface represented by @class.
 * The ID will positive and as small as possible.
 * LOCKING: this is supposed to be called with the loader lock held.
 * Returns: the new ID.
 */
static guint
mono_get_unique_iid (MonoClass *class)
{
	int iid;
	
	g_assert (MONO_CLASS_IS_INTERFACE (class));

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
	if (class->image->interface_bitset) {
		if (iid >= mono_bitset_size (class->image->interface_bitset)) {
			MonoBitSet *new_set = mono_bitset_clone (class->image->interface_bitset, iid + 1);
			mono_bitset_free (class->image->interface_bitset);
			class->image->interface_bitset = new_set;
		}
	} else {
		class->image->interface_bitset = mono_bitset_new (iid + 1, 0);
	}
	mono_bitset_set (class->image->interface_bitset, iid);

	if (mono_print_vtable) {
		int generic_id;
		char *type_name = mono_type_full_name (&class->byval_arg);
		if (class->generic_class && !class->generic_class->context.class_inst->is_open) {
			generic_id = class->generic_class->context.class_inst->id;
			g_assert (generic_id != 0);
		} else {
			generic_id = 0;
		}
		printf ("Interface: assigned id %d to %s|%s|%d\n", iid, class->image->name, type_name, generic_id);
		g_free (type_name);
	}

	g_assert (iid <= 65535);
	return iid;
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

static int
compare_interface_ids (const void *p_key, const void *p_element) {
	const MonoClass *key = p_key;
	const MonoClass *element = *(MonoClass**) p_element;
	
	return (key->interface_id - element->interface_id);
}

int
mono_class_interface_offset (MonoClass *klass, MonoClass *itf) {
	MonoClass **result = bsearch (
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

static void
print_implemented_interfaces (MonoClass *klass) {
	GPtrArray *ifaces = NULL;
	int i;
	int ancestor_level = 0;
	
	printf ("Packed interface table for class %s has size %d\n", klass->name, klass->interface_offsets_count);
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
	for (i = 0; i < ((((klass->max_interface_id + 1) >> 3)) + (((klass->max_interface_id + 1) & 7)? 1 :0)); i++)
		printf (" %02X", klass->interface_bitmap [i]);
	printf ("\n");
	while (klass != NULL) {
		printf ("[LEVEL %d] Implemented interfaces by class %s:\n", ancestor_level, klass->name);
		ifaces = mono_class_get_implemented_interfaces (klass);
		if (ifaces) {
			for (i = 0; i < ifaces->len; i++) {
				MonoClass *ic = g_ptr_array_index (ifaces, i);
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

/* this won't be needed once bug #325495 is completely fixed
 * though we'll need something similar to know which interfaces to allow
 * in arrays when they'll be lazyly created
 */
static MonoClass**
get_implicit_generic_array_interfaces (MonoClass *class, int *num, int *is_enumerator)
{
	MonoClass *eclass = class->element_class;
	static MonoClass* generic_icollection_class = NULL;
	static MonoClass* generic_ienumerable_class = NULL;
	static MonoClass* generic_ienumerator_class = NULL;
	MonoClass *fclass = NULL;
	MonoClass **interfaces = NULL;
	int i, interface_count, real_count;
	int all_interfaces;
	gboolean internal_enumerator;
	gboolean eclass_is_valuetype;

	if (!mono_defaults.generic_ilist_class) {
		*num = 0;
		return NULL;
	}
	internal_enumerator = FALSE;
	eclass_is_valuetype = FALSE;
	if (class->byval_arg.type != MONO_TYPE_SZARRAY) {
		if (class->generic_class && class->nested_in == mono_defaults.array_class && strcmp (class->name, "InternalEnumerator`1") == 0)	 {
			/*
			 * For a Enumerator<T[]> we need to get the list of interfaces for T.
			 */
			eclass = mono_class_from_mono_type (class->generic_class->context.class_inst->type_argv [0]);
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
		generic_icollection_class = mono_class_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "ICollection`1");
		generic_ienumerable_class = mono_class_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "IEnumerable`1");
		generic_ienumerator_class = mono_class_from_name (mono_defaults.corlib,
			"System.Collections.Generic", "IEnumerator`1");
	}

	mono_class_init (eclass);

	/*
	 * Arrays in 2.0 need to implement a number of generic interfaces
	 * (IList`1, ICollection`1, IEnumerable`1 for a number of types depending
	 * on the element class). We collect the types needed to build the
	 * instantiations in interfaces at intervals of 3, because 3 are
	 * the generic interfaces needed to implement.
	 */
	if (eclass->valuetype) {
		if (eclass == mono_defaults.int16_class)
			fclass = mono_defaults.uint16_class;
		else if (eclass == mono_defaults.uint16_class)
			fclass = mono_defaults.int16_class;
		else if (eclass == mono_defaults.int32_class)
			fclass = mono_defaults.uint32_class;
		else if (eclass == mono_defaults.uint32_class)
			fclass = mono_defaults.int32_class;
		else if (eclass == mono_defaults.int64_class)
			fclass = mono_defaults.uint64_class;
		else if (eclass == mono_defaults.uint64_class)
			fclass = mono_defaults.int64_class;
		else if (eclass == mono_defaults.byte_class)
			fclass = mono_defaults.sbyte_class;
		else if (eclass == mono_defaults.sbyte_class)
			fclass = mono_defaults.byte_class;
		else {
			/* No additional interfaces for other value types */
			*num = 0;
			return NULL;
		}

		/* IList, ICollection, IEnumerable */
		real_count = interface_count = 3;
		interfaces = g_malloc0 (sizeof (MonoClass*) * interface_count);
		interfaces [0] = fclass;
		eclass_is_valuetype = TRUE;
	} else {
		int j;
		int idepth = eclass->idepth;
		if (!internal_enumerator)
			idepth--;
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
		/* IList, ICollection, IEnumerable */
		interface_count *= 3;
		real_count = interface_count;
		if (internal_enumerator)
			real_count += idepth + eclass->interface_offsets_count;
		interfaces = g_malloc0 (sizeof (MonoClass*) * real_count);
		if (MONO_CLASS_IS_INTERFACE (eclass)) {
			interfaces [0] = mono_defaults.object_class;
			j = 3;
		} else {
			j = 0;
			for (i = 0; i < idepth; i++) {
				mono_class_init (eclass->supertypes [i]);
				interfaces [j] = eclass->supertypes [i];
				j += 3;
			}
		}
		if (all_interfaces) {
			for (i = 0; i < eclass->interface_offsets_count; i++) {
				interfaces [j] = eclass->interfaces_packed [i];
				j += 3;
			}
		} else {
			for (i = 0; i < eclass->interface_count; i++) {
				interfaces [j] = eclass->interfaces [i];
				j += 3;
			}
		}
	}

	/* instantiate the generic interfaces */
	for (i = 0; i < interface_count; i += 3) {
		MonoType *args [1];
		MonoClass *iface = interfaces [i];

		args [0] = &iface->byval_arg;
		interfaces [i] = mono_class_bind_generic_parameters (
			mono_defaults.generic_ilist_class, 1, args, FALSE);
		//g_print ("%s implements %s\n", class->name, mono_type_get_name_full (&interfaces [i]->byval_arg, 0));
		args [0] = &iface->byval_arg;
		interfaces [i + 1] = mono_class_bind_generic_parameters (
			generic_icollection_class, 1, args, FALSE);
		args [0] = &iface->byval_arg;
		interfaces [i + 2] = mono_class_bind_generic_parameters (
			generic_ienumerable_class, 1, args, FALSE);
		//g_print ("%s implements %s\n", class->name, mono_type_get_name_full (&interfaces [i + 1]->byval_arg, 0));
		//g_print ("%s implements %s\n", class->name, mono_type_get_name_full (&interfaces [i + 2]->byval_arg, 0));
	}
	if (internal_enumerator) {
		int j;
		/* instantiate IEnumerator<iface> */
		for (i = 0; i < interface_count; i++) {
			MonoType *args [1];
			MonoClass *iface = interfaces [i];

			args [0] = &iface->byval_arg;
			interfaces [i] = mono_class_bind_generic_parameters (
				generic_ienumerator_class, 1, args, FALSE);
			/*g_print ("%s implements %s\n", class->name, mono_type_get_name_full (&interfaces [i]->byval_arg, 0));*/
		}
		if (!eclass_is_valuetype) {
			j = interface_count;
			for (i = 0; i < eclass->idepth; i++) {
				MonoType *args [1];
				args [0] = &eclass->supertypes [i]->byval_arg;
				interfaces [j] = mono_class_bind_generic_parameters (
					generic_ienumerator_class, 1, args, FALSE);
				/*g_print ("%s implements %s\n", class->name, mono_type_get_name_full (&interfaces [i]->byval_arg, 0));*/
				j ++;
			}
			for (i = 0; i < eclass->interface_offsets_count; i++) {
				MonoClass *iface = eclass->interfaces_packed [i];
				MonoType *args [1];
				args [0] = &iface->byval_arg;
				interfaces [j] = mono_class_bind_generic_parameters (
					generic_ienumerator_class, 1, args, FALSE);
				/*g_print ("%s implements %s\n", class->name, mono_type_get_name_full (&interfaces [i]->byval_arg, 0));*/
				j ++;
			}
		}
	}
	*num = real_count;
	return interfaces;
}

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 */
static int
setup_interface_offsets (MonoClass *class, int cur_slot)
{
	MonoClass *k, *ic;
	int i, max_iid;
	MonoClass **interfaces_full;
	int *interface_offsets_full;
	GPtrArray *ifaces;
	int interface_offsets_count;
	MonoClass **array_interfaces;
	int num_array_interfaces;
	int is_enumerator = FALSE;

	/* 
	 * get the implicit generic interfaces for either the arrays or for System.Array/InternalEnumerator<T>
	 * implicit interfaces have the property that they are assigned the same slot in the
	 * vtables for compatible interfaces
	 */
	array_interfaces = get_implicit_generic_array_interfaces (class, &num_array_interfaces, &is_enumerator);

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
	for (i = 0; i < num_array_interfaces; ++i) {
		ic = array_interfaces [i];
		mono_class_init (ic);
		if (max_iid < ic->interface_id)
			max_iid = ic->interface_id;
	}

	if (MONO_CLASS_IS_INTERFACE (class)) {
		if (max_iid < class->interface_id)
			max_iid = class->interface_id;
	}
	class->max_interface_id = max_iid;
	/* compute vtable offset for interfaces */
	interfaces_full = g_malloc (sizeof (MonoClass*) * (max_iid + 1));
	interface_offsets_full = g_malloc (sizeof (int) * (max_iid + 1));

	for (i = 0; i <= max_iid; i++) {
		interfaces_full [i] = NULL;
		interface_offsets_full [i] = -1;
	}

	ifaces = mono_class_get_implemented_interfaces (class);
	if (ifaces) {
		for (i = 0; i < ifaces->len; ++i) {
			ic = g_ptr_array_index (ifaces, i);
			interfaces_full [ic->interface_id] = ic;
			interface_offsets_full [ic->interface_id] = cur_slot;
			cur_slot += ic->method.count;
		}
		g_ptr_array_free (ifaces, TRUE);
	}

	for (k = class->parent; k ; k = k->parent) {
		ifaces = mono_class_get_implemented_interfaces (k);
		if (ifaces) {
			for (i = 0; i < ifaces->len; ++i) {
				ic = g_ptr_array_index (ifaces, i);

				if (interface_offsets_full [ic->interface_id] == -1) {
					int io = mono_class_interface_offset (k, ic);

					g_assert (io >= 0);

					interfaces_full [ic->interface_id] = ic;
					interface_offsets_full [ic->interface_id] = io;
				}
			}
			g_ptr_array_free (ifaces, TRUE);
		}
	}

	if (MONO_CLASS_IS_INTERFACE (class)) {
		interfaces_full [class->interface_id] = class;
		interface_offsets_full [class->interface_id] = cur_slot;
	}

	if (num_array_interfaces) {
		if (is_enumerator) {
			int ienumerator_offset;
			g_assert (strcmp (class->interfaces [0]->name, "IEnumerator`1") == 0);
			ienumerator_offset = interface_offsets_full [class->interfaces [0]->interface_id];
			for (i = 0; i < num_array_interfaces; ++i) {
				ic = array_interfaces [i];
				interfaces_full [ic->interface_id] = ic;
				if (strcmp (ic->name, "IEnumerator`1") == 0)
					interface_offsets_full [ic->interface_id] = ienumerator_offset;
				else
					g_assert_not_reached ();
				/*g_print ("type %s has %s offset at %d (%s)\n", class->name, ic->name, interface_offsets_full [ic->interface_id], class->interfaces [0]->name);*/
			}
		} else {
			int ilist_offset, icollection_offset, ienumerable_offset;
			g_assert (strcmp (class->interfaces [0]->name, "IList`1") == 0);
			g_assert (strcmp (class->interfaces [0]->interfaces [0]->name, "ICollection`1") == 0);
			g_assert (strcmp (class->interfaces [0]->interfaces [1]->name, "IEnumerable`1") == 0);
			ilist_offset = interface_offsets_full [class->interfaces [0]->interface_id];
			icollection_offset = interface_offsets_full [class->interfaces [0]->interfaces [0]->interface_id];
			ienumerable_offset = interface_offsets_full [class->interfaces [0]->interfaces [1]->interface_id];
			g_assert (ilist_offset >= 0 && icollection_offset >= 0 && ienumerable_offset >= 0);
			for (i = 0; i < num_array_interfaces; ++i) {
				ic = array_interfaces [i];
				interfaces_full [ic->interface_id] = ic;
				if (ic->generic_class->container_class == mono_defaults.generic_ilist_class)
					interface_offsets_full [ic->interface_id] = ilist_offset;
				else if (strcmp (ic->name, "ICollection`1") == 0)
					interface_offsets_full [ic->interface_id] = icollection_offset;
				else if (strcmp (ic->name, "IEnumerable`1") == 0)
					interface_offsets_full [ic->interface_id] = ienumerable_offset;
				else
					g_assert_not_reached ();
				/*g_print ("type %s has %s offset at %d (%s)\n", class->name, ic->name, interface_offsets_full [ic->interface_id], class->interfaces [0]->name);*/
			}
		}
	}

	for (interface_offsets_count = 0, i = 0; i <= max_iid; i++) {
		if (interface_offsets_full [i] != -1) {
			interface_offsets_count ++;
		}
	}

	/*
	 * We might get called twice: once from mono_class_init () then once from 
	 * mono_class_setup_vtable ().
	 */
	if (class->interfaces_packed) {
		g_assert (class->interface_offsets_count == interface_offsets_count);
	} else {
		class->interface_offsets_count = interface_offsets_count;
		class->interfaces_packed = mono_mempool_alloc (class->image->mempool, sizeof (MonoClass*) * interface_offsets_count);
		class->interface_offsets_packed = mono_mempool_alloc (class->image->mempool, sizeof (guint16) * interface_offsets_count);
		class->interface_bitmap = mono_mempool_alloc0 (class->image->mempool, (sizeof (guint8) * ((max_iid + 1) >> 3)) + (((max_iid + 1) & 7)? 1 :0));
		for (interface_offsets_count = 0, i = 0; i <= max_iid; i++) {
			if (interface_offsets_full [i] != -1) {
				class->interface_bitmap [i >> 3] |= (1 << (i & 7));
				class->interfaces_packed [interface_offsets_count] = interfaces_full [i];
				class->interface_offsets_packed [interface_offsets_count] = interface_offsets_full [i];
				/*if (num_array_interfaces)
				  g_print ("type %s has %s offset at %d\n", mono_type_get_name_full (&class->byval_arg, 0), mono_type_get_name_full (&interfaces_full [i]->byval_arg, 0), interface_offsets_full [i]);*/
				interface_offsets_count ++;
			}
		}
	}
	
	g_free (interfaces_full);
	g_free (interface_offsets_full);
	g_free (array_interfaces);
	
	//printf ("JUST DONE: ");
	//print_implemented_interfaces (class);
 
 	return cur_slot;
}

/*
 * Setup interface offsets for interfaces. Used by Ref.Emit.
 */
void
mono_class_setup_interface_offsets (MonoClass *class)
{
	mono_loader_lock ();

	setup_interface_offsets (class, 0);

	mono_loader_unlock ();
}
 
/*
 * mono_class_setup_vtable:
 *
 *   Creates the generic vtable of CLASS.
 * Initializes the following fields in MonoClass:
 * - vtable
 * - vtable_size
 * Plus all the fields initialized by setup_interface_offsets ().
 * If there is an error during vtable construction, class->exception_type is set.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_vtable (MonoClass *class)
{
	MonoMethod **overrides;
	MonoGenericContext *context;
	guint32 type_token;
	int onum = 0;
	int i;
	gboolean ok = TRUE;

	if (class->vtable)
		return;

	if (MONO_CLASS_IS_INTERFACE (class))
		return;

	mono_class_setup_methods (class);

	mono_loader_lock ();

	if (class->vtable) {
		mono_loader_unlock ();
		return;
	}

	mono_stats.generic_vtable_count ++;

	if (class->generic_class) {
		context = mono_class_get_context (class);
		type_token = class->generic_class->container_class->type_token;
	} else {
		context = (MonoGenericContext *) class->generic_container;		
		type_token = class->type_token;
	}

	if (class->image->dynamic) {
		if (class->generic_class) {
			MonoClass *gklass = class->generic_class->container_class;

			mono_reflection_get_dynamic_overrides (gklass, &overrides, &onum);
			for (i = 0; i < onum; ++i) {
				MonoMethod *override = overrides [(i * 2) + 1];
				MonoMethod *inflated = NULL;
				int j;

				for (j = 0; j < class->method.count; ++j) {
					if (gklass->methods [j] == override) {
						inflated = class->methods [j];
						break;
					}
				}
				g_assert (inflated);
						
				overrides [(i * 2) + 1] = inflated;
			}
		} else {
			mono_reflection_get_dynamic_overrides (class, &overrides, &onum);
		}
	} else {
		/* The following call fails if there are missing methods in the type */
		ok = mono_class_get_overrides_full (class->image, type_token, &overrides, &onum, context);
	}

	if (ok)
		mono_class_setup_vtable_general (class, overrides, onum);
		
	g_free (overrides);

	mono_loader_unlock ();

	return;
}

static void
check_core_clr_override_method (MonoClass *class, MonoMethod *override, MonoMethod *base)
{
	MonoSecurityCoreCLRLevel override_level = mono_security_core_clr_method_level (override, FALSE);
	MonoSecurityCoreCLRLevel base_level = mono_security_core_clr_method_level (base, FALSE);

	if (override_level != base_level && base_level == MONO_SECURITY_CORE_CLR_CRITICAL)
		mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
}


static int __use_new_interface_vtable_code = -1;
static gboolean
use_new_interface_vtable_code (void) {
	if (__use_new_interface_vtable_code == -1) {
		char *env_var = getenv ("MONO_USE_NEW_INTERFACE_VTABLE_CODE");
		if (env_var == NULL) {
			__use_new_interface_vtable_code = TRUE;
		} else {
			if ((strcmp (env_var, "0") == 0) || (strcmp (env_var, "false") == 0) || (strcmp (env_var, "FALSE") == 0)) {
				__use_new_interface_vtable_code = FALSE;
			} else {
				__use_new_interface_vtable_code = TRUE;
			}
		}
	}
	return __use_new_interface_vtable_code;
}


#define DEBUG_INTERFACE_VTABLE_CODE 0
#define TRACE_INTERFACE_VTABLE_CODE 0

#if (TRACE_INTERFACE_VTABLE_CODE|DEBUG_INTERFACE_VTABLE_CODE)
#define DEBUG_INTERFACE_VTABLE(stmt) do {\
	stmt;\
} while (0)
#else
#define DEBUG_INTERFACE_VTABLE(stmt)
#endif

#if TRACE_INTERFACE_VTABLE_CODE
#define TRACE_INTERFACE_VTABLE(stmt) do {\
	stmt;\
} while (0)
#else
#define TRACE_INTERFACE_VTABLE(stmt)
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
check_interface_method_override (MonoClass *class, MonoMethod *im, MonoMethod *cm, gboolean require_newslot, gboolean interface_is_explicitly_implemented_by_class, gboolean slot_is_empty, gboolean security_enabled) {
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
		if (! mono_metadata_signature_equal (mono_method_signature (cm), mono_method_signature (im))) {
			TRACE_INTERFACE_VTABLE (printf ("[SIGNATURE CHECK FAILED  "));
			TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
			TRACE_INTERFACE_VTABLE (printf ("]"));
			return FALSE;
		}
		TRACE_INTERFACE_VTABLE (printf ("[SECURITY CHECKS]"));
		/* CAS - SecurityAction.InheritanceDemand on interface */
		if (security_enabled && (im->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
			mono_secman_inheritancedemand_method (cm, im);
		}

		if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
			check_core_clr_override_method (class, cm, im);
		TRACE_INTERFACE_VTABLE (printf ("[NAME CHECK OK]"));
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
		if (! mono_metadata_signature_equal (mono_method_signature (cm), mono_method_signature (im))) {
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
		if ((ic_name == NULL) || ((strcmp (ic_name, "IEnumerable`1") != 0) && (strcmp (ic_name, "ICollection`1") != 0) && (strcmp (ic_name, "IList`1") != 0))) {
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
		/* CAS - SecurityAction.InheritanceDemand on interface */
		if (security_enabled && (im->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
			mono_secman_inheritancedemand_method (cm, im);
		}

		if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
			check_core_clr_override_method (class, cm, im);
		
		TRACE_INTERFACE_VTABLE (printf ("[INJECTED INTERFACE CHECK OK]"));
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
print_vtable_full (MonoClass *class, MonoMethod** vtable, int size, int first_non_interface_slot, const char *message, gboolean print_interfaces) {
	char *full_name = mono_type_full_name (&class->byval_arg);
	int i;
	int parent_size;
	
	printf ("*** Vtable for class '%s' at \"%s\" (size %d)\n", full_name, message, size);
	
	if (print_interfaces) {
		print_implemented_interfaces (class);
		printf ("* Interfaces for class '%s' done.\nStarting vtable (size %d):\n", full_name, size);
	}
	
	if (class->parent) {
		parent_size = class->parent->vtable_size;
	} else {
		parent_size = 0;
	}
	for (i = 0; i < size; ++i) {
		MonoMethod *cm = vtable [i];
		if (cm) {
			char *cm_name = mono_method_full_name (cm, TRUE);
			char newness = (i < parent_size) ? 'O' : ((i < first_non_interface_slot) ? 'I' : 'N');
			printf ("  [%c][%03d][INDEX %03d] %s\n", newness, i, cm->slot, cm_name);
			g_free (cm_name);
		}
	}

	g_free (full_name);
}
#endif

static void
print_unimplemented_interface_method_info (MonoClass *class, MonoClass *ic, MonoMethod *im, int im_slot, MonoMethod **overrides, int onum) {
	int index;
	char *method_signature;
	
	for (index = 0; index < onum; ++index) {
		g_print (" at slot %d: %s (%d) overrides %s (%d)\n", im_slot, overrides [index*2+1]->name, 
			 overrides [index*2+1]->slot, overrides [index*2]->name, overrides [index*2]->slot);
	}
	method_signature = mono_signature_get_desc (mono_method_signature (im), FALSE);
	printf ("no implementation for interface method %s::%s(%s) in class %s.%s\n",
		mono_type_get_name (&ic->byval_arg), im->name, method_signature, class->name_space, class->name);
	g_free (method_signature);
	for (index = 0; index < class->method.count; ++index) {
		MonoMethod *cm = class->methods [index];
		method_signature = mono_signature_get_desc (mono_method_signature (cm), TRUE);

		printf ("METHOD %s(%s)\n", cm->name, method_signature);
		g_free (method_signature);
	}
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
	GPtrArray *ifaces, *pifaces = NULL;
	GHashTable *override_map = NULL;
	gboolean security_enabled = mono_is_security_manager_active ();
#if (DEBUG_INTERFACE_VTABLE_CODE|TRACE_INTERFACE_VTABLE_CODE)
	int first_non_interface_slot;
#endif

	if (class->vtable)
		return;

	ifaces = mono_class_get_implemented_interfaces (class);
	if (ifaces) {
		for (i = 0; i < ifaces->len; i++) {
			MonoClass *ic = g_ptr_array_index (ifaces, i);
			max_vtsize += ic->method.count;
		}
		g_ptr_array_free (ifaces, TRUE);
		ifaces = NULL;
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
	DEBUG_INTERFACE_VTABLE (first_non_interface_slot = cur_slot);

	if (use_new_interface_vtable_code ()) {
		if (class->parent && class->parent->vtable_size) {
			MonoClass *parent = class->parent;
			int i;
			
			memcpy (vtable, parent->vtable,  sizeof (gpointer) * parent->vtable_size);
			
			// Also inherit parent interface vtables, just as a starting point.
			// This is needed otherwise bug-77127.exe fails when the property methods
			// have different names in the iterface and the class, because for child
			// classes the ".override" information is not used anymore.
			for (i = 0; i < parent->interface_offsets_count; i++) {
				MonoClass *parent_interface = parent->interfaces_packed [i];
				int interface_offset = mono_class_interface_offset (class, parent_interface);
				
				if (interface_offset >= parent->vtable_size) {
					int parent_interface_offset = mono_class_interface_offset (parent, parent_interface);
					int j;
					
					mono_class_setup_methods (parent_interface);
					TRACE_INTERFACE_VTABLE (printf ("    +++ Inheriting interface %s.%s\n", parent_interface->name_space, parent_interface->name));
					for (j = 0; j < parent_interface->method.count; j++) {
						vtable [interface_offset + j] = parent->vtable [parent_interface_offset + j];
						TRACE_INTERFACE_VTABLE (printf ("    --- Inheriting: [%03d][(%03d)+(%03d)] => [%03d][(%03d)+(%03d)]\n",
								parent_interface_offset + j, parent_interface_offset, j,
								interface_offset + j, interface_offset, j));
					}
				}
				
			}
		}
	} else {
		if (class->parent && class->parent->vtable_size)
			memcpy (vtable, class->parent->vtable,  sizeof (gpointer) * class->parent->vtable_size);
	}

	TRACE_INTERFACE_VTABLE (print_vtable_full (class, vtable, cur_slot, first_non_interface_slot, "AFTER INHERITING PARENT VTABLE", TRUE));
	/* override interface methods */
	for (i = 0; i < onum; i++) {
		MonoMethod *decl = overrides [i*2];
		if (MONO_CLASS_IS_INTERFACE (decl->klass)) {
			int dslot;
			mono_class_setup_methods (decl->klass);
			g_assert (decl->slot != -1);
			dslot = decl->slot + mono_class_interface_offset (class, decl->klass);
			vtable [dslot] = overrides [i*2 + 1];
			vtable [dslot]->slot = dslot;
			if (!override_map)
				override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);

			g_hash_table_insert (override_map, overrides [i * 2], overrides [i * 2 + 1]);

			if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
				check_core_clr_override_method (class, vtable [dslot], decl);
		}
	}
	TRACE_INTERFACE_VTABLE (print_overrides (override_map, "AFTER OVERRIDING INTERFACE METHODS"));
	TRACE_INTERFACE_VTABLE (print_vtable_full (class, vtable, cur_slot, first_non_interface_slot, "AFTER OVERRIDING INTERFACE METHODS", FALSE));

	if (use_new_interface_vtable_code ()) {
		// Loop on all implemented interfaces...
		for (i = 0; i < class->interface_offsets_count; i++) {
			MonoClass *parent = class->parent;
			int ic_offset;
			gboolean interface_is_explicitly_implemented_by_class;
			int im_index;
			
			ic = class->interfaces_packed [i];
			ic_offset = mono_class_interface_offset (class, ic);

			mono_class_setup_methods (ic);
			
			// Check if this interface is explicitly implemented (instead of just inherited)
			if (parent != NULL) {
				int implemented_interfaces_index;
				interface_is_explicitly_implemented_by_class = FALSE;
				for (implemented_interfaces_index = 0; implemented_interfaces_index < class->interface_count; implemented_interfaces_index++) {
					if (ic == class->interfaces [implemented_interfaces_index]) {
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
				MonoMethod *override_im = (override_map != NULL) ? g_hash_table_lookup (override_map, im) : NULL;
				
				if (im->flags & METHOD_ATTRIBUTE_STATIC)
					continue;

				// If there is an explicit implementation, just use it right away,
				// otherwise look for a matching method
				if (override_im == NULL) {
					int cm_index;
					
					// First look for a suitable method among the class methods
					for (cm_index = 0; cm_index < class->method.count; cm_index++) {
						MonoMethod *cm = class->methods [cm_index];
						
						TRACE_INTERFACE_VTABLE (printf ("    For slot %d ('%s'.'%s':'%s'), trying method '%s'.'%s':'%s'... [EXPLICIT IMPLEMENTATION = %d][SLOT IS NULL = %d]", im_slot, ic->name_space, ic->name, im->name, cm->klass->name_space, cm->klass->name, cm->name, interface_is_explicitly_implemented_by_class, (vtable [im_slot] == NULL)));
						if ((cm->flags & METHOD_ATTRIBUTE_VIRTUAL) && check_interface_method_override (class, im, cm, TRUE, interface_is_explicitly_implemented_by_class, (vtable [im_slot] == NULL), security_enabled)) {
							TRACE_INTERFACE_VTABLE (printf ("[check ok]: ASSIGNING"));
							vtable [im_slot] = cm;
							/* Why do we need this? */
							if (cm->slot < 0) {
								cm->slot = im_slot;
							}
						}
						TRACE_INTERFACE_VTABLE (printf ("\n"));
					}
					
					// If the slot is still empty, look in all the inherited virtual methods...
					if ((vtable [im_slot] == NULL) && class->parent != NULL) {
						MonoClass *parent = class->parent;
						// Reverse order, so that last added methods are preferred
						for (cm_index = parent->vtable_size - 1; cm_index >= 0; cm_index--) {
							MonoMethod *cm = parent->vtable [cm_index];
							
							TRACE_INTERFACE_VTABLE ((cm != NULL) && printf ("    For slot %d ('%s'.'%s':'%s'), trying (ancestor) method '%s'.'%s':'%s'... ", im_slot, ic->name_space, ic->name, im->name, cm->klass->name_space, cm->klass->name, cm->name));
							if ((cm != NULL) && check_interface_method_override (class, im, cm, FALSE, FALSE, TRUE, security_enabled)) {
								TRACE_INTERFACE_VTABLE (printf ("[everything ok]: ASSIGNING"));
								vtable [im_slot] = cm;
								/* Why do we need this? */
								if (cm->slot < 0) {
									cm->slot = im_slot;
								}
								break;
							}
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
		if (! (class->flags & TYPE_ATTRIBUTE_ABSTRACT)) {
			for (i = 0; i < class->interface_offsets_count; i++) {
				int ic_offset;
				int im_index;
				
				ic = class->interfaces_packed [i];
				ic_offset = mono_class_interface_offset (class, ic);
				
				for (im_index = 0; im_index < ic->method.count; im_index++) {
					MonoMethod *im = ic->methods [im_index];
					int im_slot = ic_offset + im->slot;
					
					if (im->flags & METHOD_ATTRIBUTE_STATIC)
						continue;

					TRACE_INTERFACE_VTABLE (printf ("      [class is not abstract, checking slot %d for interface '%s'.'%s', method %s, slot check is %d]\n",
							im_slot, ic->name_space, ic->name, im->name, (vtable [im_slot] == NULL)));
					if (vtable [im_slot] == NULL) {
						print_unimplemented_interface_method_info (class, ic, im, im_slot, overrides, onum);
						mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
						if (override_map)
							g_hash_table_destroy (override_map);
						return;
					}
				}
			}
		}
	} else {
		for (k = class; k ; k = k->parent) {
			int nifaces = 0;

			ifaces = mono_class_get_implemented_interfaces (k);
			if (ifaces) {
				nifaces = ifaces->len;
				if (k->generic_class) {
					pifaces = mono_class_get_implemented_interfaces (
						k->generic_class->container_class);
					g_assert (pifaces && (pifaces->len == nifaces));
				}
			}
			for (i = 0; i < nifaces; i++) {
				MonoClass *pic = NULL;
				int j, l, io;

				ic = g_ptr_array_index (ifaces, i);
				if (pifaces)
					pic = g_ptr_array_index (pifaces, i);
				g_assert (ic->interface_id <= k->max_interface_id);
				io = mono_class_interface_offset (k, ic);

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

								if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
									check_core_clr_override_method (class, cm, im);

								g_assert (io + l <= max_vtsize);
								vtable [io + l] = cm;
								TRACE_INTERFACE_VTABLE (printf ("    [NOA] Filling slot %d (%d+%d) with method '%s'.'%s':'%s' ", io + l, io, l, cm->klass->name_space, cm->klass->name, cm->name));
								TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
								TRACE_INTERFACE_VTABLE (printf ("\n"));
							}
						}
					}
				} else {
					/* already implemented */
					if (io >= k->vtable_size)
						continue;
				}

				// Override methods with the same fully qualified name
				for (l = 0; l < ic->method.count; l++) {
					MonoMethod *im = ic->methods [l];						
					char *qname, *fqname, *cname, *the_cname;
					MonoClass *k1;
					
					if (vtable [io + l])
						continue;

					if (pic) {
						the_cname = mono_type_get_name_full (&pic->byval_arg, MONO_TYPE_NAME_FORMAT_IL);
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
									mono_metadata_signature_equal (mono_method_signature (cm), mono_method_signature (im)) &&
									((vtable [io + l] == NULL) || mono_class_is_subclass_of (cm->klass, vtable [io + l]->klass, FALSE))) {

								/* CAS - SecurityAction.InheritanceDemand on interface */
								if (security_enabled && (im->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
									mono_secman_inheritancedemand_method (cm, im);
								}

								if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
									check_core_clr_override_method (class, cm, im);

								g_assert (io + l <= max_vtsize);
								vtable [io + l] = cm;
								TRACE_INTERFACE_VTABLE (printf ("    [FQN] Filling slot %d (%d+%d) with method '%s'.'%s':'%s' ", io + l, io, l, cm->klass->name_space, cm->klass->name, cm->name));
								TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
								TRACE_INTERFACE_VTABLE (printf ("\n"));
								break;
							}
						}
					}
					g_free (the_cname);
					g_free (qname);
					g_free (fqname);
				}

				// Override methods with the same name
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

								if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
									check_core_clr_override_method (class, cm, im);

								g_assert (io + l <= max_vtsize);
								vtable [io + l] = cm;
								TRACE_INTERFACE_VTABLE (printf ("    [SQN] Filling slot %d (%d+%d) with method '%s'.'%s':'%s' ", io + l, io, l, cm->klass->name_space, cm->klass->name, cm->name));
								TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
								TRACE_INTERFACE_VTABLE (printf ("\n"));
								break;
							}
							
						}
						g_assert (io + l <= max_vtsize);
						if (vtable [io + l] && !(vtable [io + l]->flags & METHOD_ATTRIBUTE_ABSTRACT))
							break;
					}
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
							
							for (; parent; parent = parent->parent) {
								if (MONO_CLASS_IMPLEMENTS_INTERFACE (parent, ic->interface_id) &&
										parent->vtable) {
									vtable [io + l] = parent->vtable [mono_class_interface_offset (parent, ic) + l];
									TRACE_INTERFACE_VTABLE (printf ("    [INH] Filling slot %d (%d+%d) with method '%s'.'%s':'%s'\n", io + l, io, l, vtable [io + l]->klass->name_space, vtable [io + l]->klass->name, vtable [io + l]->name));
								}
							}
						}

						if (!(vtable [io + l])) {
							for (j = 0; j < onum; ++j) {
								g_print (" at slot %d: %s (%d) overrides %s (%d)\n", io+l, overrides [j*2+1]->name, 
									 overrides [j*2+1]->slot, overrides [j*2]->name, overrides [j*2]->slot);
							}
							msig = mono_signature_get_desc (mono_method_signature (im), FALSE);
							printf ("no implementation for interface method %s::%s(%s) in class %s.%s\n",
								mono_type_get_name (&ic->byval_arg), im->name, msig, class->name_space, class->name);
							g_free (msig);
							for (j = 0; j < class->method.count; ++j) {
								MonoMethod *cm = class->methods [j];
								msig = mono_signature_get_desc (mono_method_signature (cm), TRUE);
								
								printf ("METHOD %s(%s)\n", cm->name, msig);
								g_free (msig);
							}

							mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);

							if (ifaces)
								g_ptr_array_free (ifaces, TRUE);
							if (override_map)
								g_hash_table_destroy (override_map);

							return;
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
	}

	TRACE_INTERFACE_VTABLE (print_vtable_full (class, vtable, cur_slot, first_non_interface_slot, "AFTER SETTING UP INTERFACE METHODS", FALSE));
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
					MonoMethodSignature *cmsig, *m1sig;

					if (!(m1->flags & METHOD_ATTRIBUTE_VIRTUAL))
						continue;

					cmsig = mono_method_signature (cm);
					m1sig = mono_method_signature (m1);

					if (!cmsig || !m1sig) {
						mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
						return;
					}

					if (!strcmp(cm->name, m1->name) && 
					    mono_metadata_signature_equal (cmsig, m1sig)) {

						/* CAS - SecurityAction.InheritanceDemand */
						if (security_enabled && (m1->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
							mono_secman_inheritancedemand_method (cm, m1);
						}

						if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
							check_core_clr_override_method (class, cm, m1);

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

			if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
				check_core_clr_override_method (class, vtable [decl->slot], decl);
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

		class->vtable_size = MAX (gklass->vtable_size, cur_slot);
	} else {
		/* Check that the vtable_size value computed in mono_class_init () is correct */
		if (class->vtable_size)
			g_assert (cur_slot == class->vtable_size);
		class->vtable_size = cur_slot;
	}

	/* Try to share the vtable with our parent. */
	if (class->parent && (class->parent->vtable_size == class->vtable_size) && (memcmp (class->parent->vtable, vtable, sizeof (gpointer) * class->vtable_size) == 0)) {
		mono_memory_barrier ();
		class->vtable = class->parent->vtable;
	} else {
		MonoMethod **tmp = mono_mempool_alloc0 (class->image->mempool, sizeof (gpointer) * class->vtable_size);
		memcpy (tmp, vtable,  sizeof (gpointer) * class->vtable_size);
		mono_memory_barrier ();
		class->vtable = tmp;
	}

	DEBUG_INTERFACE_VTABLE (print_vtable_full (class, class->vtable, class->vtable_size, first_non_interface_slot, "FINALLY", FALSE));
	if (mono_print_vtable) {
		int icount = 0;

		print_implemented_interfaces (class);
		
		for (i = 0; i <= max_iid; i++)
			if (MONO_CLASS_IMPLEMENTS_INTERFACE (class, i))
				icount++;

		printf ("VTable %s (vtable entries = %d, interfaces = %d)\n", mono_type_full_name (&class->byval_arg), 
			class->vtable_size, icount); 

		for (i = 0; i < class->vtable_size; ++i) {
			MonoMethod *cm;
	       
			cm = vtable [i];
			if (cm) {
				printf ("  slot assigned: %03d, slot index: %03d %s\n", i, cm->slot,
					mono_method_full_name (cm, TRUE));
			}
		}


		if (icount) {
			printf ("Interfaces %s.%s (max_iid = %d)\n", class->name_space, 
				class->name, max_iid);
	
			for (i = 0; i < class->interface_count; i++) {
				ic = class->interfaces [i];
				printf ("  slot offset: %03d, method count: %03d, iid: %03d %s\n",  
					mono_class_interface_offset (class, ic),
					ic->method.count, ic->interface_id, mono_type_full_name (&ic->byval_arg));
			}

			for (k = class->parent; k ; k = k->parent) {
				for (i = 0; i < k->interface_count; i++) {
					ic = k->interfaces [i]; 
					printf ("  slot offset: %03d, method count: %03d, iid: %03d %s\n",  
						mono_class_interface_offset (class, ic),
						ic->method.count, ic->interface_id, mono_type_full_name (&ic->byval_arg));
				}
			}
		}
	}
}

static MonoMethod *default_ghc = NULL;
static MonoMethod *default_finalize = NULL;
static int finalize_slot = -1;
static int ghc_slot = -1;

static void
initialize_object_slots (MonoClass *class)
{
	int i;
	if (default_ghc)
		return;
	if (class == mono_defaults.object_class) { 
		mono_class_setup_vtable (class);		       
		for (i = 0; i < class->vtable_size; ++i) {
			MonoMethod *cm = class->vtable [i];
       
			if (!strcmp (cm->name, "GetHashCode"))
				ghc_slot = i;
			else if (!strcmp (cm->name, "Finalize"))
				finalize_slot = i;
		}

		g_assert (ghc_slot > 0);
		default_ghc = class->vtable [ghc_slot];

		g_assert (finalize_slot > 0);
		default_finalize = class->vtable [finalize_slot];
	}
}

static GList*
g_list_prepend_mempool (GList* l, MonoMemPool* mp, gpointer datum)
{
	GList* n = mono_mempool_alloc (mp, sizeof (GList));
	n->next = l;
	n->prev = NULL;
	n->data = datum;
	return n;
}

typedef struct {
	MonoMethod *array_method;
	char *name;
} GenericArrayMethodInfo;

static int generic_array_method_num = 0;
static GenericArrayMethodInfo *generic_array_method_info = NULL;

static int
generic_array_methods (MonoClass *class)
{
	int i, count_generic = 0;
	GList *list = NULL, *tmp;
	if (generic_array_method_num)
		return generic_array_method_num;
	mono_class_setup_methods (class->parent);
	for (i = 0; i < class->parent->method.count; i++) {
		MonoMethod *m = class->parent->methods [i];
		if (!strncmp (m->name, "InternalArray__", 15)) {
			count_generic++;
			list = g_list_prepend (list, m);
		}
	}
	list = g_list_reverse (list);
	generic_array_method_info = g_malloc (sizeof (GenericArrayMethodInfo) * count_generic);
	i = 0;
	for (tmp = list; tmp; tmp = tmp->next) {
		const char *mname, *iname;
		gchar *name;
		MonoMethod *m = tmp->data;
		generic_array_method_info [i].array_method = m;
		if (!strncmp (m->name, "InternalArray__ICollection_", 27)) {
			iname = "System.Collections.Generic.ICollection`1.";
			mname = m->name + 27;
		} else if (!strncmp (m->name, "InternalArray__IEnumerable_", 27)) {
			iname = "System.Collections.Generic.IEnumerable`1.";
			mname = m->name + 27;
		} else if (!strncmp (m->name, "InternalArray__", 15)) {
			iname = "System.Collections.Generic.IList`1.";
			mname = m->name + 15;
		} else {
			g_assert_not_reached ();
		}

		name = mono_mempool_alloc (mono_defaults.corlib->mempool, strlen (iname) + strlen (mname) + 1);
		strcpy (name, iname);
		strcpy (name + strlen (iname), mname);
		generic_array_method_info [i].name = name;
		i++;
	}
	/*g_print ("array generic methods: %d\n", count_generic);*/

	generic_array_method_num = count_generic;
	return generic_array_method_num;
}

static void
setup_generic_array_ifaces (MonoClass *class, MonoClass *iface, MonoMethod **methods, int pos)
{
	MonoGenericContext tmp_context;
	int i;

	tmp_context.class_inst = NULL;
	tmp_context.method_inst = iface->generic_class->context.class_inst;
	//g_print ("setting up array interface: %s\n", mono_type_get_name_full (&iface->byval_arg, 0));

	for (i = 0; i < generic_array_method_num; i++) {
		MonoMethod *m = generic_array_method_info [i].array_method;
		MonoMethod *inflated;

		inflated = mono_class_inflate_generic_method (m, &tmp_context);
		methods [pos++] = mono_marshal_get_generic_array_helper (class, iface, generic_array_method_info [i].name, inflated);
	}
}

static char*
concat_two_strings_with_zero (MonoMemPool *pool, const char *s1, const char *s2)
{
	int len = strlen (s1) + strlen (s2) + 2;
	char *s = mono_mempool_alloc (pool, len);
	int result;

	result = g_snprintf (s, len, "%s%c%s", s1, '\0', s2);
	g_assert (result == len - 1);

	return s;
}

static void
set_failure_from_loader_error (MonoClass *class, MonoLoaderError *error)
{
	gpointer exception_data = NULL;

	switch (error->exception_type) {
	case MONO_EXCEPTION_TYPE_LOAD:
		exception_data = concat_two_strings_with_zero (class->image->mempool, error->class_name, error->assembly_name);
		break;

	case MONO_EXCEPTION_MISSING_METHOD:
		exception_data = concat_two_strings_with_zero (class->image->mempool, error->class_name, error->member_name);
		break;

	case MONO_EXCEPTION_MISSING_FIELD: {
		const char *name_space = error->klass->name_space ? error->klass->name_space : NULL;
		const char *class_name;

		if (name_space)
			class_name = g_strdup_printf ("%s.%s", name_space, error->klass->name);
		else
			class_name = error->klass->name;

		exception_data = concat_two_strings_with_zero (class->image->mempool, class_name, error->member_name);
		
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

		exception_data = concat_two_strings_with_zero (class->image->mempool, msg, error->assembly_name);
		break;
	}

	case MONO_EXCEPTION_BAD_IMAGE:
		exception_data = error->msg;
		break;

	default :
		g_assert_not_reached ();
	}

	mono_class_set_failure (class, error->exception_type, exception_data);
}

static void
check_core_clr_inheritance (MonoClass *class)
{
	MonoSecurityCoreCLRLevel class_level, parent_level;
	MonoClass *parent = class->parent;

	if (!parent)
		return;

	class_level = mono_security_core_clr_class_level (class);
	parent_level = mono_security_core_clr_class_level (parent);

	if (class_level < parent_level)
		mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
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
mono_class_init (MonoClass *class)
{
	int i;
	MonoCachedClassInfo cached_info;
	gboolean has_cached_info;
	int class_init_ok = TRUE;
	
	g_assert (class);

	/* Double-checking locking pattern */
	if (class->inited)
		return class->exception_type == MONO_EXCEPTION_NONE;

	/*g_print ("Init class %s\n", class->name);*/

	/* We do everything inside the lock to prevent races */
	mono_loader_lock ();

	if (class->inited) {
		mono_loader_unlock ();
		/* Somebody might have gotten in before us */
		return class->exception_type == MONO_EXCEPTION_NONE;
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

	if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
		check_core_clr_inheritance (class);

	mono_stats.initialized_class_count++;

	if (class->generic_class && !class->generic_class->is_dynamic) {
		MonoClass *gklass = class->generic_class->container_class;

		mono_stats.generic_class_count++;

		class->method = gklass->method;
		class->field = gklass->field;

		mono_class_init (gklass);
		mono_class_setup_methods (gklass);
		mono_class_setup_properties (gklass);

		if (MONO_CLASS_IS_INTERFACE (class))
			class->interface_id = mono_get_unique_iid (class);

		g_assert (class->interface_count == gklass->interface_count);
	}

	if (class->parent && !class->parent->inited)
		mono_class_init (class->parent);

	has_cached_info = mono_class_get_cached_class_info (class, &cached_info);

	if (!class->generic_class && !class->image->dynamic && (!has_cached_info || (has_cached_info && cached_info.has_nested_classes))) {
		i = mono_metadata_nesting_typedef (class->image, class->type_token, 1);
		while (i) {
			MonoClass* nclass;
			guint32 cols [MONO_NESTED_CLASS_SIZE];
			mono_metadata_decode_row (&class->image->tables [MONO_TABLE_NESTEDCLASS], i - 1, cols, MONO_NESTED_CLASS_SIZE);
			nclass = mono_class_create_from_typedef (class->image, MONO_TOKEN_TYPE_DEF | cols [MONO_NESTED_CLASS_NESTED]);
			class->nested_classes = g_list_prepend_mempool (class->nested_classes, class->image->mempool, nclass);

			i = mono_metadata_nesting_typedef (class->image, class->type_token, i + 1);
		}
	}

	/*
	 * Computes the size used by the fields, and their locations
	 */
	if (has_cached_info) {
		class->instance_size = cached_info.instance_size;
		class->sizes.class_size = cached_info.class_size;
		class->packing_size = cached_info.packing_size;
		class->min_align = cached_info.min_align;
		class->blittable = cached_info.blittable;
		class->has_references = cached_info.has_references;
		class->has_static_refs = cached_info.has_static_refs;
		class->no_special_static_fields = cached_info.no_special_static_fields;
	}
	else
		if (!class->size_inited){
			mono_class_setup_fields (class);
			if (class->exception_type || mono_loader_get_last_error ()){
				class_init_ok = FALSE;
				goto leave;
			}
		}
				
	/* Initialize arrays */
	if (class->rank) {
		class->method.count = 3 + (class->rank > 1? 2: 1);

		if (class->interface_count) {
			int count_generic = generic_array_methods (class);
			class->method.count += class->interface_count * count_generic;
		}
	}

	mono_class_setup_supertypes (class);

	if (!default_ghc)
		initialize_object_slots (class);

	/* 
	 * Initialize the rest of the data without creating a generic vtable if possible.
	 * If possible, also compute vtable_size, so mono_class_create_runtime_vtable () can
	 * also avoid computing a generic vtable.
	 */
	if (has_cached_info) {
		/* AOT case */
		class->vtable_size = cached_info.vtable_size;
		class->has_finalize = cached_info.has_finalize;
		class->ghcimpl = cached_info.ghcimpl;
		class->has_cctor = cached_info.has_cctor;
	} else if (class->rank == 1 && class->byval_arg.type == MONO_TYPE_SZARRAY) {
		static int szarray_vtable_size = 0;

		/* SZARRAY case */
		if (!szarray_vtable_size) {
			mono_class_setup_vtable (class);
			szarray_vtable_size = class->vtable_size;
		} else {
			class->vtable_size = szarray_vtable_size;
		}
	} else if (class->generic_class && !MONO_CLASS_IS_INTERFACE (class)) {
		MonoClass *gklass = class->generic_class->container_class;

		/* Generic instance case */
		class->ghcimpl = gklass->ghcimpl;
		class->has_finalize = gklass->has_finalize;
		class->has_cctor = gklass->has_cctor;

		mono_class_setup_vtable (gklass);
		if (gklass->exception_type)
			goto fail;

		class->vtable_size = gklass->vtable_size;
	} else {
		/* General case */

		/* ghcimpl is not currently used
		class->ghcimpl = 1;
		if (class->parent) { 
			MonoMethod *cmethod = class->vtable [ghc_slot];
			if (cmethod->is_inflated)
				cmethod = ((MonoMethodInflated*)cmethod)->declaring;
			if (cmethod == default_ghc) {
				class->ghcimpl = 0;
			}
		}
		*/

		if (!MONO_CLASS_IS_INTERFACE (class)) {
			MonoMethod *cmethod = NULL;

			if (class->type_token) {
				cmethod = find_method_in_metadata (class, "Finalize", 0, METHOD_ATTRIBUTE_VIRTUAL);
			} else if (class->parent) {
				/* FIXME: Optimize this */
				mono_class_setup_vtable (class);
				if (class->exception_type || mono_loader_get_last_error ())
					goto fail;
				cmethod = class->vtable [finalize_slot];
			}

			if (cmethod) {
				/* Check that this is really the finalizer method */
				mono_class_setup_vtable (class);
				if (class->exception_type || mono_loader_get_last_error ())
					goto fail;

				class->has_finalize = 0;
				if (class->parent) { 
					cmethod = class->vtable [finalize_slot];
					if (cmethod->is_inflated)
						cmethod = ((MonoMethodInflated*)cmethod)->declaring;
					if (cmethod != default_finalize) {
						class->has_finalize = 1;
					}
				}
			}
		}

		/* C# doesn't allow interfaces to have cctors */
		if (!MONO_CLASS_IS_INTERFACE (class) || class->image != mono_defaults.corlib) {
			MonoMethod *cmethod = NULL;

			if (class->type_token) {
				cmethod = find_method_in_metadata (class, ".cctor", 0, METHOD_ATTRIBUTE_SPECIAL_NAME);
				/* The find_method function ignores the 'flags' argument */
				if (cmethod && (cmethod->flags & METHOD_ATTRIBUTE_SPECIAL_NAME))
					class->has_cctor = 1;
			} else {
				mono_class_setup_methods (class);

				for (i = 0; i < class->method.count; ++i) {
					MonoMethod *method = class->methods [i];
					if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
						(strcmp (".cctor", method->name) == 0)) {
						class->has_cctor = 1;
						break;
					}
				}
			}
		}
	}

	if (!mono_setup_vtable_in_class_init) {
		/*
		 * This is an embedding API break, since the caller might assume that 
		 * mono_class_init () constructs a generic vtable, so vtable construction errors
		 * are visible right after the mono_class_init (), and not after 
		 * mono_class_vtable ().
		 */
		if (class->parent) {
			/* This will compute class->parent->vtable_size for some classes */
			mono_class_init (class->parent);
			if (class->parent->exception_type || mono_loader_get_last_error ())
				goto fail;
			if (!class->parent->vtable_size) {
				/* FIXME: Get rid of this somehow */
				mono_class_setup_vtable (class->parent);
				if (class->parent->exception_type || mono_loader_get_last_error ())
					goto fail;
			}
			setup_interface_offsets (class, class->parent->vtable_size);
		} else {
			setup_interface_offsets (class, 0);
		}
	} else {
		mono_class_setup_vtable (class);

		if (MONO_CLASS_IS_INTERFACE (class))
			setup_interface_offsets (class, 0);
	}

	if (mono_verifier_is_enabled_for_class (class) && !mono_verifier_verify_class (class)) {
		mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, concat_two_strings_with_zero (class->image->mempool, class->name, class->image->assembly_name));
		class_init_ok = FALSE;
	}

	goto leave;

 fail:
	class_init_ok = FALSE;

 leave:
	/* Because of the double-checking locking pattern */
	mono_memory_barrier ();
	class->inited = 1;
	class->init_pending = 0;

	if (mono_loader_get_last_error ()) {
		if (class->exception_type == MONO_EXCEPTION_NONE)
			set_failure_from_loader_error (class, mono_loader_get_last_error ());

		mono_loader_clear_error ();
	}

	mono_loader_unlock ();

	if (mono_debugger_class_init_func)
		mono_debugger_class_init_func (class);

	return class_init_ok;
}

static gboolean
is_corlib_image (MonoImage *image)
{
	/* FIXME: allow the dynamic case for our compilers and with full trust */
	if (image->dynamic)
		return image->assembly && !strcmp (image->assembly->aname.name, "mscorlib");
	else
		return image == mono_defaults.corlib;
}

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_mono_type (MonoClass *class)
{
	const char *name = class->name;
	const char *nspace = class->name_space;
	gboolean is_corlib = is_corlib_image (class->image);

	class->this_arg.byref = 1;
	class->this_arg.data.klass = class;
	class->this_arg.type = MONO_TYPE_CLASS;
	class->byval_arg.data.klass = class;
	class->byval_arg.type = MONO_TYPE_CLASS;

	if (is_corlib && !strcmp (nspace, "System")) {
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

		if (is_corlib && !strcmp (nspace, "System")) {
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

	if (MONO_CLASS_IS_INTERFACE (class))
		class->interface_id = mono_get_unique_iid (class);

}

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_parent (MonoClass *class, MonoClass *parent)
{
	gboolean system_namespace;
	gboolean is_corlib = is_corlib_image (class->image);

	system_namespace = !strcmp (class->name_space, "System") && is_corlib;

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
		/* Imported COM Objects always derive from __ComObject. */
		if (MONO_CLASS_IS_IMPORT (class)) {
			mono_init_com_types ();
			if (parent == mono_defaults.object_class)
				parent = mono_defaults.com_object_class;
		}
		if (!parent) {
			/* set the parent to something useful and safe, but mark the type as broken */
			parent = mono_defaults.object_class;
			mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
		}

		class->parent = parent;

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
		if (MONO_CLASS_IS_IMPORT (class))
			class->is_com_object = 1;
		else
			class->is_com_object = parent->is_com_object;
		
		if (system_namespace) {
			if (*class->name == 'M' && !strcmp (class->name, "MarshalByRefObject"))
				class->marshalbyref = 1;

			if (*class->name == 'C' && !strcmp (class->name, "ContextBoundObject")) 
				class->contextbound  = 1;

			if (*class->name == 'D' && !strcmp (class->name, "Delegate")) 
				class->delegate  = 1;
		}

		if (class->parent->enumtype || (is_corlib_image (class->parent->image) && (strcmp (class->parent->name, "ValueType") == 0) && 
						(strcmp (class->parent->name_space, "System") == 0)))
			class->valuetype = 1;
		if (is_corlib_image (class->parent->image) && ((strcmp (class->parent->name, "Enum") == 0) && (strcmp (class->parent->name_space, "System") == 0))) {
			class->valuetype = class->enumtype = 1;
		}
		/*class->enumtype = class->parent->enumtype; */
		mono_class_setup_supertypes (class);
	} else {
		/* initialize com types if COM interfaces are present */
		if (MONO_CLASS_IS_IMPORT (class))
			mono_init_com_types ();
		class->parent = NULL;
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
	class->supertypes = mono_mempool_alloc0 (class->image->mempool, sizeof (MonoClass *) * ms);

	if (class->parent) {
		class->supertypes [class->idepth - 1] = class;
		memcpy (class->supertypes, class->parent->supertypes, class->parent->idepth * sizeof (gpointer));
	} else {
		class->supertypes [0] = class;
	}
}

/**
 * mono_class_create_from_typedef:
 * @image: image where the token is valid
 * @type_token:  typedef token
 *
 * Create the MonoClass* representing the specified type token.
 * @type_token must be a TypeDef token.
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
	guint32 nesting_tokeen;

	mono_loader_lock ();

	if ((class = mono_internal_hash_table_lookup (&image->class_cache, GUINT_TO_POINTER (type_token)))) {
		mono_loader_unlock ();
		return class;
	}

	g_assert (mono_metadata_token_table (type_token) == MONO_TABLE_TYPEDEF);

	mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);
	
	name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
	nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);

	class = mono_mempool_alloc0 (image->mempool, sizeof (MonoClass));

	class->name = name;
	class->name_space = nspace;

	mono_profiler_class_event (class, MONO_PROFILE_START_LOAD);

	class->image = image;
	class->type_token = type_token;
	class->flags = cols [MONO_TYPEDEF_FLAGS];

	mono_internal_hash_table_insert (&image->class_cache, GUINT_TO_POINTER (type_token), class);

	/*
	 * Check whether we're a generic type definition.
	 */
	class->generic_container = mono_metadata_load_generic_params (image, class->type_token, NULL);
	if (class->generic_container) {
		class->generic_container->owner.klass = class;
		context = &class->generic_container->context;
	}

	if (cols [MONO_TYPEDEF_EXTENDS]) {
		parent = mono_class_get_full (
			image, mono_metadata_token_from_dor (cols [MONO_TYPEDEF_EXTENDS]), context);
		if (parent == NULL){
			mono_internal_hash_table_remove (&image->class_cache, GUINT_TO_POINTER (type_token));
			mono_loader_unlock ();
			mono_profiler_class_loaded (class, MONO_PROFILE_FAILED);
			return NULL;
		}
	}

	/* do this early so it's available for interfaces in setup_mono_type () */
	if ((nesting_tokeen = mono_metadata_nested_in_typedef (image, type_token)))
		class->nested_in = mono_class_create_from_typedef (image, nesting_tokeen);

	mono_class_setup_parent (class, parent);

	/* uses ->valuetype, which is initialized by mono_class_setup_parent above */
	mono_class_setup_mono_type (class);

	if (!class->enumtype) {
		if (!mono_metadata_interfaces_from_typedef_full (
			    image, type_token, &interfaces, &icount, context)){
			mono_loader_unlock ();
			mono_profiler_class_loaded (class, MONO_PROFILE_FAILED);
			return NULL;
		}

		class->interfaces = interfaces;
		class->interface_count = icount;
	}

	if ((class->flags & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_UNICODE_CLASS)
		class->unicode = 1;

#if PLATFORM_WIN32
	if ((class->flags & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_AUTO_CLASS)
		class->unicode = 1;
#endif

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
		if (!class->enum_basetype) {
			mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
			mono_loader_unlock ();
			return NULL;
		}
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

	mono_loader_unlock ();

	mono_profiler_class_loaded (class, MONO_PROFILE_OK);

	return class;
}

/** is klass Nullable<T>? */
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

/*
 * Create the `MonoClass' for an instantiation of a generic type.
 * We only do this if we actually need it.
 */
MonoClass*
mono_generic_class_get_class (MonoGenericClass *gclass)
{
	MonoClass *klass, *gklass;
	int i;

	mono_loader_lock ();
	if (gclass->cached_class) {
		mono_loader_unlock ();
		return gclass->cached_class;
	}

	gclass->cached_class = g_malloc0 (sizeof (MonoClass));
	klass = gclass->cached_class;

	gklass = gclass->container_class;

	if (gklass->nested_in) {
		/* 
		 * FIXME: the nested type context should include everything the
		 * nesting context should have, but it may also have additional
		 * generic parameters...
		 */
		MonoType *inflated = mono_class_inflate_generic_type (
			&gklass->nested_in->byval_arg, mono_generic_class_get_context (gclass));
		klass->nested_in = mono_class_from_mono_type (inflated);
		mono_metadata_free_type (inflated);
	}

	klass->name = gklass->name;
	klass->name_space = gklass->name_space;
	
	mono_profiler_class_event (klass, MONO_PROFILE_START_LOAD);
	
	klass->image = gklass->image;
	klass->flags = gklass->flags;
	klass->type_token = gklass->type_token;
	klass->field.count = gklass->field.count;
	klass->property.count = gklass->property.count;

	klass->generic_class = gclass;

	klass->this_arg.type = klass->byval_arg.type = MONO_TYPE_GENERICINST;
	klass->this_arg.data.generic_class = klass->byval_arg.data.generic_class = gclass;
	klass->this_arg.byref = TRUE;
	klass->enumtype = gklass->enumtype;
	klass->valuetype = gklass->valuetype;

	klass->cast_class = klass->element_class = klass;

	if (mono_class_is_nullable (klass))
		klass->cast_class = klass->element_class = mono_class_get_nullable_param (klass);

	klass->interface_count = gklass->interface_count;
	klass->interfaces = g_new0 (MonoClass *, klass->interface_count);
	for (i = 0; i < klass->interface_count; i++) {
		MonoType *it = &gklass->interfaces [i]->byval_arg;
		MonoType *inflated = mono_class_inflate_generic_type (it, mono_generic_class_get_context (gclass));
		klass->interfaces [i] = mono_class_from_mono_type (inflated);
		mono_metadata_free_type (inflated);
	}

	/*
	 * We're not interested in the nested classes of a generic instance.
	 * We use the generic type definition to look for nested classes.
	 */
	klass->nested_classes = NULL;

	if (gklass->parent) {
		MonoType *inflated = mono_class_inflate_generic_type (
			&gklass->parent->byval_arg, mono_generic_class_get_context (gclass));

		klass->parent = mono_class_from_mono_type (inflated);
		mono_metadata_free_type (inflated);
	}

	if (klass->parent)
		mono_class_setup_parent (klass, klass->parent);

	if (klass->enumtype) {
		klass->enum_basetype = gklass->enum_basetype;
		klass->cast_class = gklass->cast_class;
	}

	if (gclass->is_dynamic) {
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
			klass->size_inited = 1;
		}
	}

	mono_profiler_class_loaded (klass, MONO_PROFILE_OK);
	
	mono_loader_unlock ();

	return klass;
}

MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *image, gboolean is_mvar)
{
	MonoClass *klass, **ptr;
	int count, pos, i;

	mono_loader_lock ();

	if (param->pklass) {
		mono_loader_unlock ();
		return param->pklass;
	}

	if (!image && param->owner) {
		if (is_mvar) {
			MonoMethod *method = param->owner->owner.method;
			image = (method && method->klass) ? method->klass->image : NULL;
		} else {
			MonoClass *klass = param->owner->owner.klass;
			// FIXME: 'klass' should not be null
			// 	  But, monodis creates GenericContainers without associating a owner to it
			image = klass ? klass->image : NULL;
		}
	}
	if (!image)
		/* FIXME: */
		image = mono_defaults.corlib;

	klass = param->pklass = mono_mempool_alloc0 (image->mempool, sizeof (MonoClass));

	if (param->name)
		klass->name = param->name;
	else {
		klass->name = mono_mempool_alloc0 (image->mempool, 16);
		sprintf ((char*)klass->name, is_mvar ? "!!%d" : "!%d", param->num);
	}
	klass->name_space = "";
	mono_profiler_class_event (klass, MONO_PROFILE_START_LOAD);
	
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
		klass->interfaces = mono_mempool_alloc0 (image->mempool, sizeof (MonoClass *) * (count - pos));
		for (i = pos; i < count; i++)
			klass->interfaces [i - pos] = param->constraints [i];
	}

	if (!image)
		image = mono_defaults.corlib;

	klass->image = image;

	klass->inited = TRUE;
	klass->cast_class = klass->element_class = klass;
	klass->enum_basetype = &klass->element_class->byval_arg;
	klass->flags = TYPE_ATTRIBUTE_PUBLIC;

	klass->this_arg.type = klass->byval_arg.type = is_mvar ? MONO_TYPE_MVAR : MONO_TYPE_VAR;
	klass->this_arg.data.generic_param = klass->byval_arg.data.generic_param = param;
	klass->this_arg.byref = TRUE;

	if (param->owner) {
		guint32 owner;
		guint32 cols [MONO_GENERICPARAM_SIZE];
		MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAM];
		i = 0;

		if (is_mvar && param->owner->owner.method)
			 i = mono_metadata_get_generic_param_row (image, param->owner->owner.method->token, &owner);
		else if (!is_mvar && param->owner->owner.klass)
			 i = mono_metadata_get_generic_param_row (image, param->owner->owner.klass->type_token, &owner);

		if (i) {
			mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
			do {
				if (cols [MONO_GENERICPARAM_NUMBER] == param->num) {
					klass->sizes.generic_param_token = i | MONO_TOKEN_GENERIC_PARAM;
					break;
				}
				if (++i > tdef->rows)
					break;
				mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
			} while (cols [MONO_GENERICPARAM_OWNER] == owner);
		}
	}

	mono_class_setup_supertypes (klass);

	mono_loader_unlock ();

	mono_profiler_class_loaded (klass, MONO_PROFILE_OK);

	return klass;
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

	mono_loader_lock ();

	if (!image->ptr_cache)
		image->ptr_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);

	if ((result = g_hash_table_lookup (image->ptr_cache, el_class))) {
		mono_loader_unlock ();
		return result;
	}
	result = mono_mempool_alloc0 (image->mempool, sizeof (MonoClass));

	result->parent = NULL; /* no parent for PTR types */
	result->name_space = el_class->name_space;
	name = g_strdup_printf ("%s*", el_class->name);
	result->name = mono_mempool_strdup (image->mempool, name);
	g_free (name);

	mono_profiler_class_event (result, MONO_PROFILE_START_LOAD);

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

	g_hash_table_insert (image->ptr_cache, el_class, result);

	mono_loader_unlock ();

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
	
	if ((result = g_hash_table_lookup (ptr_hash, sig))) {
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

	result->this_arg.type = result->byval_arg.type = MONO_TYPE_FNPTR;
	result->this_arg.data.method = result->byval_arg.data.method = sig;
	result->this_arg.byref = TRUE;
	result->enum_basetype = &result->element_class->byval_arg;
	result->blittable = TRUE;

	mono_class_setup_supertypes (result);

	g_hash_table_insert (ptr_hash, sig, result);

	mono_loader_unlock ();

	mono_profiler_class_loaded (result, MONO_PROFILE_OK);

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
	case MONO_TYPE_GENERICINST:
		return mono_generic_class_get_class (type->data.generic_class);
	case MONO_TYPE_VAR:
		return mono_class_from_generic_parameter (type->data.generic_param, NULL, FALSE);
	case MONO_TYPE_MVAR:
		return mono_class_from_generic_parameter (type->data.generic_param, NULL, TRUE);
	default:
		g_warning ("mono_class_from_mono_type: implement me 0x%02x\n", type->type);
		g_assert_not_reached ();
	}
	
	return NULL;
}

/**
 * mono_type_retrieve_from_typespec
 * @image: context where the image is created
 * @type_spec:  typespec token
 * @context: the generic context used to evaluate generic instantiations in
 */
static MonoType *
mono_type_retrieve_from_typespec (MonoImage *image, guint32 type_spec, MonoGenericContext *context, gboolean *did_inflate)
{
	MonoType *t = mono_type_create_from_typespec (image, type_spec);
	if (!t)
		return NULL;
	if (context && (context->class_inst || context->method_inst)) {
		MonoType *inflated = inflate_generic_type (t, context);
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
mono_class_create_from_typespec (MonoImage *image, guint32 type_spec, MonoGenericContext *context)
{
	MonoClass *ret;
	gboolean inflated = FALSE;
	MonoType *t = mono_type_retrieve_from_typespec (image, type_spec, context, &inflated);
	if (!t)
		return NULL;
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

	if (!image->array_cache)
		image->array_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);

	if ((rootlist = list = g_hash_table_lookup (image->array_cache, eclass))) {
		for (; list; list = list->next) {
			class = list->data;
			if ((class->rank == rank) && (class->byval_arg.type == (((rank > 1) || bounded) ? MONO_TYPE_ARRAY : MONO_TYPE_SZARRAY))) {
				mono_loader_unlock ();
				return class;
			}
		}
	}

	/* for the building corlib use System.Array from it */
	if (image->assembly && image->assembly->dynamic && image->assembly_name && strcmp (image->assembly_name, "mscorlib") == 0) {
		parent = mono_class_from_name (image, "System", "Array");
		corlib_type = TRUE;
	} else {
		parent = mono_defaults.array_class;
		if (!parent->inited)
			mono_class_init (parent);
	}

	class = mono_mempool_alloc0 (image->mempool, sizeof (MonoClass));

	class->image = image;
	class->name_space = eclass->name_space;
	nsize = strlen (eclass->name);
	name = g_malloc (nsize + 2 + rank + 1);
	memcpy (name, eclass->name, nsize);
	name [nsize] = '[';
	if (rank > 1)
		memset (name + nsize + 1, ',', rank - 1);
	if (bounded)
		name [nsize + rank] = '*';
	name [nsize + rank + bounded] = ']';
	name [nsize + rank + bounded + 1] = 0;
	class->name = mono_mempool_strdup (image->mempool, name);
	g_free (name);

	mono_profiler_class_event (class, MONO_PROFILE_START_LOAD);

	class->type_token = 0;
	/* all arrays are marked serializable and sealed, bug #42779 */
	class->flags = TYPE_ATTRIBUTE_CLASS | TYPE_ATTRIBUTE_SERIALIZABLE | TYPE_ATTRIBUTE_SEALED |
		(eclass->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK);
	class->parent = parent;
	class->instance_size = mono_class_instance_size (class->parent);

	if (eclass->enumtype && !eclass->enum_basetype) {
		if (!eclass->reflection_info || eclass->wastypebuilder) {
			g_warning ("Only incomplete TypeBuilder objects are allowed to be an enum without base_type");
			g_assert (eclass->reflection_info && !eclass->wastypebuilder);
		}
		/* element_size -1 is ok as this is not an instantitable type*/
		class->sizes.element_size = -1;
	} else
		class->sizes.element_size = mono_class_array_element_size (eclass);

	mono_class_setup_supertypes (class);

	if (mono_defaults.generic_ilist_class && !bounded && rank == 1) {
		MonoType *args [1];

		/* generic IList, ICollection, IEnumerable */
		class->interface_count = 1;
		class->interfaces = mono_mempool_alloc0 (image->mempool, sizeof (MonoClass*) * class->interface_count);

		args [0] = &eclass->byval_arg;
		class->interfaces [0] = mono_class_bind_generic_parameters (
			mono_defaults.generic_ilist_class, 1, args, FALSE);
	}

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
		MonoArrayType *at = mono_mempool_alloc0 (image->mempool, sizeof (MonoArrayType));
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

	mono_profiler_class_loaded (class, MONO_PROFILE_OK);

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
mono_class_get_field_idx (MonoClass *class, int idx)
{
	mono_class_setup_fields_locking (class);

	while (class) {
		if (class->image->uncompressed_metadata) {
			/* 
			 * class->field.first points to the FieldPtr table, while idx points into the
			 * Field table, so we have to do a search.
			 */
			const char *name = mono_metadata_string_heap (class->image, mono_metadata_decode_row_col (&class->image->tables [MONO_TABLE_FIELD], idx, MONO_FIELD_NAME));
			int i;

			for (i = 0; i < class->field.count; ++i)
				if (class->fields [i].name == name)
					return &class->fields [i];
			g_assert_not_reached ();
		} else {			
			if (class->field.count) {
				if ((idx >= class->field.first) && (idx < class->field.first + class->field.count)){
					return &class->fields [idx - class->field.first];
				}
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

	g_assert (field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT);

	if (!field->data) {
		cindex = mono_metadata_get_constant_index (field->parent->image, mono_class_get_field_token (field), 0);
		g_assert (cindex);
		g_assert (!(field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA));

		mono_metadata_decode_row (&field->parent->image->tables [MONO_TABLE_CONSTANT], cindex - 1, constant_cols, MONO_CONSTANT_SIZE);
		field->def_type = constant_cols [MONO_CONSTANT_TYPE];
		field->data = (gpointer)mono_metadata_blob_heap (field->parent->image, constant_cols [MONO_CONSTANT_VALUE]);
	}

	*def_type = field->def_type;
	return field->data;
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
 * mono_class_get_full:
 * @image: the image where the class resides
 * @type_token: the token for the class
 * @context: the generic context used to evaluate generic instantiations in
 *
 * Returns: the MonoClass that represents @type_token in @image
 */
MonoClass *
mono_class_get_full (MonoImage *image, guint32 type_token, MonoGenericContext *context)
{
	MonoClass *class = NULL;

	if (image->dynamic) {
		int table = mono_metadata_token_table (type_token);

		if (table != MONO_TABLE_TYPEDEF && table != MONO_TABLE_TYPEREF && table != MONO_TABLE_TYPESPEC) {
			mono_loader_set_error_bad_image (g_strdup ("Bad type token."));
			return NULL;
		}
		return mono_lookup_dynamic_token (image, type_token, context);
	}

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


/**
 * mono_type_get_full:
 * @image: the image where the type resides
 * @type_token: the token for the type
 * @context: the generic context used to evaluate generic instantiations in
 *
 * This functions exists to fullfill the fact that sometimes it's desirable to have access to the 
 * 
 * Returns: the MonoType that represents @type_token in @image
 */
MonoType *
mono_type_get_full (MonoImage *image, guint32 type_token, MonoGenericContext *context)
{
	MonoType *type = NULL;
	gboolean inflated = FALSE;

	//FIXME: this will not fix the very issue for which mono_type_get_full exists -but how to do it then?
	if (image->dynamic)
		return mono_class_get_type (mono_lookup_dynamic_token (image, type_token, context));

	if ((type_token & 0xff000000) != MONO_TOKEN_TYPE_SPEC) {
		MonoClass *class = mono_class_get_full (image, type_token, context);
		return class ? mono_class_get_type (class) : NULL;
	}

	type = mono_type_retrieve_from_typespec (image, type_token, context, &inflated);

	if (!type) {
		char *name = mono_class_name_from_token (image, type_token);
		char *assembly = mono_assembly_name_from_token (image, type_token);
		if (inflated)
			mono_metadata_free_type (type);
		mono_loader_set_error_type_load (name, assembly);
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
 * LOCKING: Acquires the loader lock.
 */
void
mono_image_init_name_cache (MonoImage *image)
{
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	const char *name;
	const char *nspace;
	guint32 i, visib, nspace_index;
	GHashTable *name_cache2, *nspace_table;

	mono_loader_lock ();

	image->name_cache = g_hash_table_new (g_str_hash, g_str_equal);

	if (image->dynamic) {
		mono_loader_unlock ();
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
		nspace_table = g_hash_table_lookup (name_cache2, GUINT_TO_POINTER (nspace_index));
		if (!nspace_table) {
			nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
			g_hash_table_insert (image->name_cache, (char*)nspace, nspace_table);
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
			name = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAME]);
			nspace = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAMESPACE]);

			nspace_index = cols [MONO_EXP_TYPE_NAMESPACE];
			nspace_table = g_hash_table_lookup (name_cache2, GUINT_TO_POINTER (nspace_index));
			if (!nspace_table) {
				nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
				g_hash_table_insert (image->name_cache, (char*)nspace, nspace_table);
				g_hash_table_insert (name_cache2, GUINT_TO_POINTER (nspace_index),
									 nspace_table);
			}
			g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (mono_metadata_make_token (MONO_TABLE_EXPORTEDTYPE, i + 1)));
		}
	}

	g_hash_table_destroy (name_cache2);

	mono_loader_unlock ();
}

void
mono_image_add_to_name_cache (MonoImage *image, const char *nspace, 
							  const char *name, guint32 index)
{
	GHashTable *nspace_table;
	GHashTable *name_cache;

	mono_loader_lock ();

	if (!image->name_cache)
		mono_image_init_name_cache (image);

	name_cache = image->name_cache;
	if (!(nspace_table = g_hash_table_lookup (name_cache, nspace))) {
		nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
		g_hash_table_insert (name_cache, (char *)nspace, (char *)nspace_table);
	}
	g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (index));

	mono_loader_unlock ();
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
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.   The namespace and name
 * lookups are case insensitive.
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

		if (!image->name_cache)
			mono_image_init_name_cache (image);

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
		visib = cols [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		/*
		 * Nested types are accessed from the nesting name.  We use the fact that nested types use different visibility flags
		 * than toplevel types, thus avoiding the need to grovel through the NESTED_TYPE table
		 */
		if (visib >= TYPE_ATTRIBUTE_NESTED_PUBLIC && visib <= TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM)
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
 * @image: The MonoImage where the type is looked up in
 * @name_space: the type namespace
 * @name: the type short name.
 *
 * Obtains a MonoClass with a given namespace and a given name which
 * is located in the given MonoImage.   
 */
MonoClass *
mono_class_from_name (MonoImage *image, const char* name_space, const char *name)
{
	GHashTable *nspace_table;
	MonoImage *loaded_image;
	guint32 token = 0;
	int i;
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

	if (get_class_from_name) {
		gboolean res = get_class_from_name (image, name_space, name, &class);
		if (res) {
			if (nested)
				return class ? return_nested_in (class, nested) : NULL;
			else
				return class;
		}
	}

	mono_loader_lock ();

	if (!image->name_cache)
		mono_image_init_name_cache (image);

	nspace_table = g_hash_table_lookup (image->name_cache, name_space);

	if (nspace_table)
		token = GPOINTER_TO_UINT (g_hash_table_lookup (nspace_table, name));

	mono_loader_unlock ();

	if (!token && image->dynamic && image->modules) {
		/* Search modules as well */
		for (i = 0; i < image->module_count; ++i) {
			MonoImage *module = image->modules [i];

			class = mono_class_from_name (module, name_space, name);
			if (class)
				return class;
		}
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
			class = mono_class_from_name (loaded_image, name_space, name);
			if (nested)
				return return_nested_in (class, nested);
			return class;
		} else if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_ASSEMBLYREF) {
			MonoAssembly **references = image->references;
			if (!references [idx - 1])
				mono_assembly_load_reference (image, idx - 1);
			g_assert (references == image->references);
			g_assert (references [idx - 1]);
			if (references [idx - 1] == (gpointer)-1)
				return NULL;			
			else
				/* FIXME: Cycle detection */
				return mono_class_from_name (references [idx - 1]->image, name_space, name);
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
	g_assert (klassc->idepth > 0);
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
mono_class_has_variant_generic_params (MonoClass *klass)
{
	int i;
	MonoGenericContainer *container;

	if (!klass->generic_class)
		return FALSE;

	container = klass->generic_class->container_class->generic_container;

	for (i = 0; i < container->type_argc; ++i)
		if (container->type_params [i].flags & (MONO_GEN_PARAM_VARIANT|MONO_GEN_PARAM_COVARIANT))
			return TRUE;

	return FALSE;
}

/**
 * mono_class_is_assignable_from:
 * @klass: the class to be assigned to
 * @oklass: the source class
 *
 * Return: true if an instance of object oklass can be assigned to an
 * instance of object @klass
 */
gboolean
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass)
{
	if (!klass->inited)
		mono_class_init (klass);

	if (!oklass->inited)
		mono_class_init (oklass);

	if ((klass->byval_arg.type == MONO_TYPE_VAR) || (klass->byval_arg.type == MONO_TYPE_MVAR))
		return klass == oklass;

	if (MONO_CLASS_IS_INTERFACE (klass)) {
		if ((oklass->byval_arg.type == MONO_TYPE_VAR) || (oklass->byval_arg.type == MONO_TYPE_MVAR))
			return FALSE;

		/* interface_offsets might not be set for dynamic classes */
		if (oklass->reflection_info && !oklass->interface_bitmap)
			/* 
			 * oklass might be a generic type parameter but they have 
			 * interface_offsets set.
			 */
 			return mono_reflection_call_is_assignable_to (oklass, klass);

		if (MONO_CLASS_IMPLEMENTS_INTERFACE (oklass, klass->interface_id))
			return TRUE;

		if (mono_class_has_variant_generic_params (klass)) {
			if (oklass->generic_class) {
				int i;
				gboolean match = FALSE;
				MonoClass *container_class1 = klass->generic_class->container_class;
				MonoClass *container_class2 = oklass->generic_class->container_class;

				/* 
				 * Check whenever the generic definition of oklass implements the 
				 * generic definition of klass. The IMPLEMENTS_INTERFACE stuff is not usable
				 * here since the relevant tables are not set up.
				 */
				for (i = 0; i < container_class2->interface_offsets_count; ++i)
					if ((container_class2->interfaces_packed [i] == container_class1) || (container_class2->interfaces_packed [i]->generic_class && (container_class2->interfaces_packed [i]->generic_class->container_class == container_class1)))
						match = TRUE;

				if (match) {
					MonoGenericContainer *container;

					container = klass->generic_class->container_class->generic_container;

					match = TRUE;
					for (i = 0; i < container->type_argc; ++i) {
						MonoClass *param1_class = mono_class_from_mono_type (klass->generic_class->context.class_inst->type_argv [i]);
						MonoClass *param2_class = mono_class_from_mono_type (oklass->generic_class->context.class_inst->type_argv [i]);

						if (param1_class->valuetype != param2_class->valuetype) {
							match = FALSE;
							break;
						}
						/*
						 * The _VARIANT and _COVARIANT constants should read _COVARIANT and
						 * _CONTRAVARIANT, but they are in a public header so we can't fix it.
						 */
						if (param1_class != param2_class) {
							if ((container->type_params [i].flags & MONO_GEN_PARAM_VARIANT) && mono_class_is_assignable_from (param1_class, param2_class))
								;
							else if (((container->type_params [i].flags & MONO_GEN_PARAM_COVARIANT) && mono_class_is_assignable_from (param2_class, param1_class)))
								;
							else {
								match = FALSE;
								break;
							}
						}
					}

					if (match)
						return TRUE;
				}
			}
		}
	} else if (klass->rank) {
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
	} else if (mono_class_is_nullable (klass))
		return (mono_class_is_assignable_from (klass->cast_class, oklass));
	else if (klass == mono_defaults.object_class)
		return TRUE;

	return mono_class_has_parent (oklass, klass);
}	

/**
 * mono_class_get_cctor:
 * @klass: A MonoClass pointer
 *
 * Returns: the static constructor of @klass if it exists, NULL otherwise.
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

/**
 * mono_class_get_finalizer:
 * @klass: The MonoClass pointer
 *
 * Returns: the finalizer method of @klass if it exists, NULL otherwise.
 */
MonoMethod*
mono_class_get_finalizer (MonoClass *klass)
{
	MonoCachedClassInfo cached_info;

	if (!klass->inited)
		mono_class_init (klass);
	if (!klass->has_finalize)
		return NULL;

	if (mono_class_get_cached_class_info (klass, &cached_info))
		return mono_get_method (cached_info.finalize_image, cached_info.finalize_token, NULL);
	else {
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
	g_assert (ac->rank);
	return ac->sizes.element_size;
}

gpointer
mono_ldtoken (MonoImage *image, guint32 token, MonoClass **handle_class,
	      MonoGenericContext *context)
{
	if (image->dynamic) {
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
		type = mono_type_get_full (image, token, context);
		if (!type)
			return NULL;
		mono_class_init (mono_class_from_mono_type (type));
		/* We return a MonoType* as handle */
		return type;
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
	case MONO_TOKEN_METHOD_DEF:
	case MONO_TOKEN_METHOD_SPEC: {
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
 * mono_class_get_type_token
 * @klass: the MonoClass to act on
 *
 * This method returns type token for the class.
 *
 * Returns: the type token for the class.
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
 * mono_field_get_offset;
 * @field: the MonoClassField to act on
 *
 * Returns: the field offset.
 */
guint32
mono_field_get_offset (MonoClassField *field)
{
	return field->offset;
}

/**
 * mono_field_get_data;
 * @field: the MonoClassField to act on
 *
 * Returns: pointer to the metadata constant value or to the field
 * data if it has an RVA flag.
 */
const char *
mono_field_get_data  (MonoClassField *field)
{
  return field->data;
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

static MonoMethod*
find_method_in_metadata (MonoClass *klass, const char *name, int param_count, int flags)
{
	MonoMethod *res = NULL;
	int i;

	/* Search directly in the metadata to avoid calling setup_methods () */
	for (i = 0; i < klass->method.count; ++i) {
		guint32 cols [MONO_METHOD_SIZE];
		MonoMethod *method;

		/* class->method.first points into the methodptr table */
		mono_metadata_decode_table_row (klass->image, MONO_TABLE_METHOD, klass->method.first + i, cols, MONO_METHOD_SIZE);

		if (!strcmp (mono_metadata_string_heap (klass->image, cols [MONO_METHOD_NAME]), name)) {
			method = mono_get_method (klass->image, MONO_TOKEN_METHOD_DEF | (klass->method.first + i + 1), klass);
			if ((param_count == -1) || mono_method_signature (method)->param_count == param_count) {
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

	if (klass->methods || klass->generic_class) {
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
	if (klass->exception_type)
		return FALSE;

	mono_loader_lock ();
	klass->exception_type = ex_type;
	if (ex_data)
		mono_property_hash_insert (klass->image->property_hash, klass, MONO_CLASS_PROP_EXCEPTION_DATA, ex_data);
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
	gpointer res;

	mono_loader_lock ();
	res = mono_property_hash_lookup (klass->image->property_hash, klass, MONO_CLASS_PROP_EXCEPTION_DATA);
	mono_loader_unlock ();
	return res;
}

/**
 * mono_classes_init:
 *
 * Initialize the resources used by this module.
 */
void
mono_classes_init (void)
{
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

	switch (klass->exception_type) {
	case MONO_EXCEPTION_SECURITY_INHERITANCEDEMAND: {
		MonoDomain *domain = mono_domain_get ();
		MonoSecurityManager* secman = mono_security_manager_get_methods ();
		MonoMethod *method = exception_data;
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
		char *class_name = exception_data;
		char *assembly_name = class_name + strlen (class_name) + 1;

		return mono_get_exception_missing_method (class_name, assembly_name);
	}
	case MONO_EXCEPTION_MISSING_FIELD: {
		char *class_name = exception_data;
		char *member_name = class_name + strlen (class_name) + 1;

		return mono_get_exception_missing_field (class_name, member_name);
	}
	case MONO_EXCEPTION_FILE_NOT_FOUND: {
		char *msg_format = exception_data;
		char *assembly_name = msg_format + strlen (msg_format) + 1;
		char *msg = g_strdup_printf (msg_format, assembly_name);
		MonoException *ex;

		ex = mono_get_exception_file_not_found2 (msg, mono_string_new (mono_domain_get (), assembly_name));

		g_free (msg);

		return ex;
	}
	case MONO_EXCEPTION_BAD_IMAGE: {
		return mono_get_exception_bad_image_format (exception_data);
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
	do {
		if (outer_klass == inner_klass)
			return TRUE;
		inner_klass = inner_klass->nested_in;
	} while (inner_klass);
	return FALSE;
}

static MonoClass *
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
static gboolean
mono_class_has_parent_and_ignore_generics (MonoClass *klass, MonoClass *parent)
{
	int i;
	klass = mono_class_get_generic_type_definition (klass);
	parent = mono_class_get_generic_type_definition (parent);
	
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
	for (tmp = accessed->friend_assembly_names; tmp; tmp = tmp->next) {
		MonoAssemblyName *friend = tmp->data;
		/* Be conservative with checks */
		if (!friend->name)
			continue;
		if (strcmp (accessing->aname.name, friend->name))
			continue;
		if (friend->public_key_token [0]) {
			if (!accessing->aname.public_key_token [0])
				continue;
			if (!mono_public_tokens_are_equal (friend->public_key_token, accessing->aname.public_key_token))
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
		if (!can_access_type (access_klass, mono_class_from_mono_type (ginst->type_argv[i])))
			return FALSE;
	}
	return TRUE;
}

static gboolean
can_access_type (MonoClass *access_klass, MonoClass *member_klass)
{
	int access_level = member_klass->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;

	if (member_klass->generic_class && !can_access_instantiation (access_klass, member_klass->generic_class->context.class_inst))
		return FALSE;

	if (is_nesting_type (access_klass, member_klass) || (access_klass->nested_in && is_nesting_type (access_klass->nested_in, member_klass)))
		return TRUE;

	if (member_klass->nested_in && !can_access_type (access_klass, member_klass->nested_in))
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

gboolean
mono_method_can_access_field (MonoMethod *method, MonoClassField *field)
{
	/* FIXME: check all overlapping fields */
	int can = can_access_member (method->klass, field->parent, NULL, field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
	if (!can) {
		MonoClass *nested = method->klass->nested_in;
		while (nested) {
			can = can_access_member (nested, field->parent, NULL, field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK);
			if (can)
				return TRUE;
			nested = nested->nested_in;
		}
	}
	return can;
}

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
 * mono_method_can_access_method_with_context:
 * @method: The caller method 
 * @called: The called method 
 * @context_klass:TThe static type on stack of the owner @called object used
 * 
 * This function must be used with instance calls, as they have more strict family accessibility.
 * It can be used with static mehthod, but context_klass should be NULL.
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

	if (!can_access_type (access_class, member_class) && (!access_class->nested_in || !can_access_type (access_class->nested_in, member_class)))
		return FALSE;

	if (called->is_inflated) {
		MonoMethodInflated * infl = (MonoMethodInflated*)called;
		if (infl->context.method_inst && !can_access_instantiation (access_class, infl->context.method_inst))
		return FALSE;
	}
		
	return TRUE;
}


/*
 * mono_method_can_access_method_with_context:
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

	if (!can_access_type (access_class, member_class) && (!access_class->nested_in || !can_access_type (access_class->nested_in, member_class)))
		return FALSE;
	return TRUE;
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
	}
	return FALSE;
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
 * mono_class_generic_sharing_enabled:
 * @class: a class
 *
 * Returns whether generic sharing is enabled for class.
 *
 * This is a stop-gap measure to slowly introduce generic sharing
 * until we have all the issues sorted out, at which time this
 * function will disappear and generic sharing will always be enabled.
 */
gboolean
mono_class_generic_sharing_enabled (MonoClass *class)
{
#if defined(__i386__) || defined(__x86_64__)
	static int generic_sharing = MONO_GENERIC_SHARING_CORLIB;
#else
	static int generic_sharing = MONO_GENERIC_SHARING_NONE;
#endif
	static gboolean inited = FALSE;

	if (!inited) {
		const char *option;

		if ((option = g_getenv ("MONO_GENERIC_SHARING"))) {
			if (strcmp (option, "corlib") == 0)
				generic_sharing = MONO_GENERIC_SHARING_CORLIB;
			else if (strcmp (option, "all") == 0)
				generic_sharing = MONO_GENERIC_SHARING_ALL;
			else if (strcmp (option, "none") == 0)
				generic_sharing = MONO_GENERIC_SHARING_NONE;
			else
				g_warning ("Unknown generic sharing option `%s'.", option);
		}

		inited = TRUE;
	}

	switch (generic_sharing) {
	case MONO_GENERIC_SHARING_NONE:
		return FALSE;
	case MONO_GENERIC_SHARING_ALL:
		return TRUE;
	case MONO_GENERIC_SHARING_CORLIB :
		return class->image == mono_defaults.corlib;
	default:
		g_assert_not_reached ();
	}
}
