/*
 * generic-sharing.c: Support functions for generic sharing.
 *
 * Author:
 *   Mark Probst (mark.probst@gmail.com)
 *
 * (C) 2007 Novell, Inc.
 */

#include <config.h>

#include <mono/metadata/class.h>
#include <mono/utils/mono-counters.h>

#include "mini.h"

static int generic_class_lookups = 0;
static int generic_class_lookup_failures = 0;

/*
 * mini_method_get_context:
 * @method: a method
 *
 * Returns the generic context of a method or NULL if it doesn't have
 * one.  For an inflated method that's the context stored in the
 * method.  Otherwise it's in the method's generic container or in the
 * generic container of the method's class.
 */
MonoGenericContext*
mini_method_get_context (MonoMethod *method)
{
	if (method->is_inflated)
		return mono_method_get_context (method);
	if (method->is_generic)
		return &(mono_method_get_generic_container (method)->context);
	if (method->klass->generic_container)
		return &method->klass->generic_container->context;
	return NULL;
}

/*
 * mono_method_check_context_used:
 * @method: a method
 *
 * Checks whether the method's generic context uses a type variable.
 * Returns an int with the bits MONO_GENERIC_CONTEXT_USED_CLASS and
 * MONO_GENERIC_CONTEXT_USED_METHOD set to reflect whether the
 * context's class or method instantiation uses type variables.
 */
int
mono_method_check_context_used (MonoMethod *method)
{
	MonoGenericContext *method_context = mini_method_get_context (method);

	if (!method_context)
		return 0;

	return mono_generic_context_check_used (method_context);
}

static gboolean
generic_inst_is_sharable (MonoGenericInst *inst, gboolean allow_type_vars)
{
	int i;

	for (i = 0; i < inst->type_argc; ++i) {
		MonoType *type = inst->type_argv [i];
		int type_type;

		if (MONO_TYPE_IS_REFERENCE (type))
			continue;

		type_type = mono_type_get_type (type);
		if (allow_type_vars && (type_type == MONO_TYPE_VAR || type_type == MONO_TYPE_MVAR))
			continue;

		return FALSE;
	}

	return TRUE;
}

/*
 * mono_generic_context_is_sharable:
 * @context: a generic context
 *
 * Returns whether the generic context is sharable.  A generic context
 * is sharable iff all of its type arguments are reference type.
 */
gboolean
mono_generic_context_is_sharable (MonoGenericContext *context, gboolean allow_type_vars)
{
	g_assert (context->class_inst || context->method_inst);

	if (context->class_inst && !generic_inst_is_sharable (context->class_inst, allow_type_vars))
		return FALSE;

	if (context->method_inst && !generic_inst_is_sharable (context->method_inst, allow_type_vars))
		return FALSE;

	return TRUE;
}

/*
 * mono_method_is_generic_impl:
 * @method: a method
 *
 * Returns whether the method is either inflated or part of an
 * inflated class.
 */
gboolean
mono_method_is_generic_impl (MonoMethod *method)
{
	return method->klass->generic_class != NULL && method->is_inflated;
}

/*
 * mono_method_is_generic_sharable_impl:
 * @method: a method
 *
 * Returns TRUE iff the method is inflated or part of an inflated
 * class, its context is sharable and it has no constraints on its
 * type parameters.  Otherwise returns FALSE.
 */
gboolean
mono_method_is_generic_sharable_impl (MonoMethod *method)
{
	if (!mono_method_is_generic_impl (method))
		return FALSE;

	if (method->is_inflated) {
		MonoMethodInflated *inflated = (MonoMethodInflated*)method;
		MonoGenericContext *context = &inflated->context;

		if (!mono_generic_context_is_sharable (context, FALSE))
			return FALSE;

		g_assert (inflated->declaring);

		if (inflated->declaring->is_generic) {
			g_assert (mono_method_get_generic_container (inflated->declaring)->type_params);

			if (mono_method_get_generic_container (inflated->declaring)->type_params->constraints)
				return FALSE;
		}
	}

	if (method->klass->generic_class) {
		if (!mono_generic_context_is_sharable (&method->klass->generic_class->context, FALSE))
			return FALSE;

		g_assert (method->klass->generic_class->container_class &&
				method->klass->generic_class->container_class->generic_container &&
				method->klass->generic_class->container_class->generic_container->type_params);

		if (method->klass->generic_class->container_class->generic_container->type_params->constraints)
			return FALSE;
	}

	return TRUE;
}

static gboolean
generic_inst_equal (MonoGenericInst *inst1, MonoGenericInst *inst2)
{
	int i;

	if (!inst1) {
		g_assert (!inst2);
		return TRUE;
	}

	g_assert (inst2);

	if (inst1->type_argc != inst2->type_argc)
		return FALSE;

	for (i = 0; i < inst1->type_argc; ++i)
		if (!mono_metadata_type_equal (inst1->type_argv [i], inst2->type_argv [i]))
			return FALSE;

	return TRUE;
}

/*
 * mono_generic_context_equal_deep:
 * @context1: a generic context
 * @context2: a generic context
 *
 * Returns whether context1's type arguments are equal to context2's
 * type arguments.
 */
gboolean
mono_generic_context_equal_deep (MonoGenericContext *context1, MonoGenericContext *context2)
{
	return generic_inst_equal (context1->class_inst, context2->class_inst) &&
		generic_inst_equal (context1->method_inst, context2->method_inst);
}

/*
 * mini_class_get_container_class:
 * @class: a generic class
 *
 * Returns the class's container class, which is the class itself if
 * it doesn't have generic_class set.
 */
MonoClass*
mini_class_get_container_class (MonoClass *class)
{
	if (class->generic_class)
		return class->generic_class->container_class;

	g_assert (class->generic_container);
	return class;
}

/*
 * mini_class_get_context:
 * @class: a generic class
 *
 * Returns the class's generic context.
 */
MonoGenericContext*
mini_class_get_context (MonoClass *class)
{
	if (class->generic_class)
		return &class->generic_class->context;

	g_assert (class->generic_container);
	return &class->generic_container->context;
}

/*
 * mini_get_basic_type_from_generic:
 * @gsctx: a generic sharing context
 * @type: a type
 *
 * Returns a closed type corresponding to the possibly open type
 * passed to it.
 */
MonoType*
mini_get_basic_type_from_generic (MonoGenericSharingContext *gsctx, MonoType *type)
{
	if (!type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR))
		g_assert (gsctx);

	return mono_type_get_basic_type_from_generic (type);
}

/*
 * mini_type_stack_size:
 * @gsctx: a generic sharing context
 * @t: a type
 * @align: Pointer to an int for returning the alignment
 *
 * Returns the type's stack size and the alignment in *align.  The
 * type is allowed to be open.
 */
int
mini_type_stack_size (MonoGenericSharingContext *gsctx, MonoType *t, int *align)
{
	return mono_type_stack_size_internal (t, align, gsctx != NULL);
}

/*
 * mono_method_get_declaring_generic_method:
 * @method: an inflated method
 *
 * Returns an inflated method's declaring method.
 */
MonoMethod*
mono_method_get_declaring_generic_method (MonoMethod *method)
{
	MonoMethodInflated *inflated;

	g_assert (method->is_inflated);

	inflated = (MonoMethodInflated*)method;

	return inflated->declaring;
}

/*
 * mono_class_generic_class_relation:
 * @klass: the class to be investigated
 * @method_klass: the reference class
 * @generic_context: the generic context of method_klass
 * @arg_num: where a value will be returned
 *
 * Discovers and returns the relation of klass with reference to
 * method_klass.  This can either be MINI_GENERIC_CLASS_RELATION_SELF,
 * meaning that klass is the same as method_klass,
 * MINI_GENERIC_CLASS_RELATION_ARGUMENT, meaning that klass is one of
 * the type arguments of method_klass, or otherwise
 * MINI_GENERIC_CLASS_RELATION_OTHER.  In the case of
 * MINI_GENERIC_CLASS_RELATION_ARGUMENT the number of the argument is
 * returned in *arg_num.
 */
int
mono_class_generic_class_relation (MonoClass *klass, int info_type, MonoClass *method_klass,
	MonoGenericContext *generic_context, int *arg_num)
{
	int i = mono_class_lookup_or_register_other_info (method_klass, &klass->byval_arg, info_type, generic_context);

	if (arg_num)
		*arg_num = i;

	return MINI_GENERIC_CLASS_RELATION_OTHER_TABLE;
}

typedef struct
{
	guint32 token;
	MonoGenericContext *context;
} MonoTokenAndContext;

static guint
token_context_hash (MonoTokenAndContext *tc)
{
	return (guint)((gulong)tc->token | (gulong)tc->context->class_inst | (gulong)tc->context->method_inst);
}

static gboolean
token_context_equal (MonoTokenAndContext *tc1, MonoTokenAndContext *tc2)
{
	if (tc1->token != tc2->token)
		return FALSE;

	return tc1->context->class_inst == tc2->context->class_inst &&
		tc1->context->method_inst == tc2->context->method_inst;
}

/*
 * mono_helper_get_rgctx_other_ptr:
 * @caller_class: the klass of the calling method
 * @vtable: the vtable with the runtime generic context
 * @token: the token which to look up
 * @token_source: what kind of item the token is for
 * @rgctx_type: the kind of value requested
 *
 * Is called from method to look up a token for a given runtime
 * generic sharing context and return some particular information
 * about the looked up class (the class itself, the vtable or the
 * static_data pointer).
 */
gpointer
mono_helper_get_rgctx_other_ptr (MonoClass *caller_class, MonoVTable *vtable,
	guint32 token, guint32 token_source, guint32 rgctx_type, gint32 rgctx_index)
{
	MonoImage *image = caller_class->image;
	MonoClass *klass = vtable->klass;
	MonoClass *result = NULL;
	MonoTokenAndContext tc = { token, &klass->generic_class->context };
	gpointer result_ptr;

	mono_loader_lock ();

	generic_class_lookups++;

	if (!image->generic_class_cache) {
		image->generic_class_cache = g_hash_table_new ((GHashFunc)token_context_hash,
			(GCompareFunc)token_context_equal);
	}

	result = g_hash_table_lookup (image->generic_class_cache, &tc);

	mono_loader_unlock ();

	if (!result) {
		generic_class_lookup_failures++;

		switch (token_source) {
		case MINI_TOKEN_SOURCE_FIELD: {
			MonoClassField *field = mono_field_from_token (image, token, &result, &klass->generic_class->context);

			g_assert (field);
			break;
		}
		case MINI_TOKEN_SOURCE_CLASS:
			result = mono_class_get_full (image, token, &klass->generic_class->context);
			break;
		case MINI_TOKEN_SOURCE_METHOD: {
			MonoMethod *cmethod = mono_get_method_full (image, token, NULL,
				&klass->generic_class->context);
			result = cmethod->klass;
			break;
		}
		default :
			g_assert_not_reached ();
		}

		g_assert (result);

		mono_class_init (result);

		mono_loader_lock ();

		/*
		 * In the meantime another thread might have put this class in
		 * the cache, so check again.
		 */
		if (!g_hash_table_lookup (image->generic_class_cache, &tc)) {
			MonoTokenAndContext *tcp = (MonoTokenAndContext*) mono_mempool_alloc0 (image->mempool,
				sizeof (MonoTokenAndContext));

			*tcp = tc;

			g_hash_table_insert (image->generic_class_cache, tcp, result);
		}

		mono_loader_unlock ();
	}

	g_assert (result);

	switch (rgctx_type) {
	case MONO_RGCTX_INFO_KLASS:
		result_ptr = result;
		break;
	case MONO_RGCTX_INFO_STATIC_DATA: {
		MonoVTable *result_vtable = mono_class_vtable (vtable->domain, result);
		result_ptr = result_vtable->data;
		break;
	}
	case MONO_RGCTX_INFO_VTABLE:
		result_ptr = mono_class_vtable (vtable->domain, result);
		break;
	default:
		g_assert_not_reached ();
	}

	return result_ptr;
}

/*
 * mono_generic_sharing_init:
 *
 * Register the generic sharing counters.
 */
void
mono_generic_sharing_init (void)
{
	mono_counters_register ("Generic class lookups", MONO_COUNTER_GENERICS | MONO_COUNTER_INT,
			&generic_class_lookups);
	mono_counters_register ("Generic class lookup failures", MONO_COUNTER_GENERICS | MONO_COUNTER_INT,
			&generic_class_lookup_failures);
}
