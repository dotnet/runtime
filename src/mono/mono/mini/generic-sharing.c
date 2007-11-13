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

#include "mini.h"

static int context_check_context_used (MonoGenericContext *context);

static int
type_check_context_used (MonoType *type, gboolean recursive)
{
	int t = mono_type_get_type (type);
	int context_used = 0;

	if (t == MONO_TYPE_VAR)
		context_used |= MONO_GENERIC_CONTEXT_USED_CLASS;
	else if (t == MONO_TYPE_MVAR)
		context_used |= MONO_GENERIC_CONTEXT_USED_METHOD;
	else if (recursive) {
		if (t == MONO_TYPE_CLASS)
			context_used |= mono_class_check_context_used (mono_type_get_class (type));
		else if (t == MONO_TYPE_GENERICINST) {
			MonoGenericClass *gclass = type->data.generic_class;

			context_used |= context_check_context_used (&gclass->context);
			g_assert (gclass->container_class->generic_container);
		}
	}

	return context_used;
}

static int
inst_check_context_used (MonoGenericInst *inst)
{
	int context_used = 0;
	int i;

	if (!inst)
		return 0;

	for (i = 0; i < inst->type_argc; ++i)
		context_used |= type_check_context_used (inst->type_argv [i], TRUE);

	return context_used;
}

static int
context_check_context_used (MonoGenericContext *context)
{
	int context_used = 0;

	context_used |= inst_check_context_used (context->class_inst);
	context_used |= inst_check_context_used (context->method_inst);

	return context_used;
}

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
	if (method->generic_container)
		return &method->generic_container->context;
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

	return context_check_context_used (method_context);
}

/*
 * mono_class_check_context_used:
 * @class: a class
 *
 * Checks whether the class's generic context uses a type variable.
 * Returns an int with the bit MONO_GENERIC_CONTEXT_USED_CLASS set to
 * reflect whether the context's class instantiation uses type
 * variables.
 */
int
mono_class_check_context_used (MonoClass *class)
{
	int context_used = 0;

	context_used |= type_check_context_used (&class->this_arg, FALSE);
	context_used |= type_check_context_used (&class->byval_arg, FALSE);

	if (class->generic_class)
		context_used |= context_check_context_used (&class->generic_class->context);
	else if (class->generic_container)
		context_used |= context_check_context_used (&class->generic_container->context);

	return context_used;
}

static gboolean
generic_inst_is_sharable (MonoGenericInst *inst)
{
	int i;

	for (i = 0; i < inst->type_argc; ++i) {
		int type = mono_type_get_type (inst->type_argv [i]);

		if (type != MONO_TYPE_CLASS && type != MONO_TYPE_STRING && type != MONO_TYPE_OBJECT &&
				type != MONO_TYPE_SZARRAY && type != MONO_TYPE_ARRAY)
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
mono_generic_context_is_sharable (MonoGenericContext *context)
{
	g_assert (context->class_inst || context->method_inst);

	if (context->class_inst && !generic_inst_is_sharable (context->class_inst))
		return FALSE;

	if (context->method_inst && !generic_inst_is_sharable (context->method_inst))
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

		if (!mono_generic_context_is_sharable (context))
			return FALSE;

		g_assert (inflated->declaring);

		if (inflated->declaring->generic_container) {
			g_assert (inflated->declaring->generic_container->type_params);

			if (inflated->declaring->generic_container->type_params->constraints)
				return FALSE;
		}
	}

	if (method->klass->generic_class) {
		if (!mono_generic_context_is_sharable (&method->klass->generic_class->context))
			return FALSE;

		g_assert (method->klass->generic_class->container_class &&
				method->klass->generic_class->container_class->generic_container &&
				method->klass->generic_class->container_class->generic_container->type_params);

		if (method->klass->generic_class->container_class->generic_container->type_params->constraints)
			return FALSE;
	}

	return TRUE;
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

