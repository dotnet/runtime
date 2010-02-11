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

/*
 * mono_get_generic_context_from_code:
 *
 *   Return the runtime generic context belonging to the method whose native code
 * contains CODE.
 */
MonoGenericSharingContext*
mono_get_generic_context_from_code (guint8 *code)
{
	MonoJitInfo *jit_info = mini_jit_info_table_find (mono_domain_get (), (char*)code, NULL);

	g_assert (jit_info);

	return mono_jit_info_get_generic_sharing_context (jit_info);
}

MonoGenericContext*
mini_method_get_context (MonoMethod *method)
{
	return mono_method_get_context_general (method, TRUE);
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
	int context_used = 0;

	if (!method_context) {
		/* It might be a method of an array of an open generic type */
		if (method->klass->rank)
			context_used = mono_class_check_context_used (method->klass);
	} else {
		context_used = mono_generic_context_check_used (method_context);
		context_used |= mono_class_check_context_used (method->klass);
	}

	return context_used;
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
 * mini_type_get_underlying_type:
 *
 *   Return the underlying type of TYPE, taking into account enums, byref and generic
 * sharing.
 */
MonoType*
mini_type_get_underlying_type (MonoGenericSharingContext *gsctx, MonoType *type)
{
	if (type->byref)
		return &mono_defaults.int_class->byval_arg;
	return mono_type_get_basic_type_from_generic (mono_type_get_underlying_type (type));
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
	gboolean allow_open = TRUE;

	// FIXME: Some callers might not pass in a gsctx
	//allow_open = gsctx != NULL;
	return mono_type_stack_size_internal (t, align, allow_open);
}

/*
 * mini_type_stack_size_full:
 *
 *   Same as mini_type_stack_size, but handle pinvoke data types as well.
 */
int
mini_type_stack_size_full (MonoGenericSharingContext *gsctx, MonoType *t, guint32 *align, gboolean pinvoke)
{
	int size;

	if (pinvoke) {
		size = mono_type_native_stack_size (t, align);
	} else {
		int ialign;

		if (align) {
			size = mini_type_stack_size (gsctx, t, &ialign);
			*align = ialign;
		} else {
			size = mini_type_stack_size (gsctx, t, NULL);
		}
	}
	
	return size;
}

/*
 * mono_generic_sharing_init:
 *
 * Register the generic sharing counters.
 */
void
mono_generic_sharing_init (void)
{
}
