/*
 * mini-exceptions-native-unwinder.c: libcorkscrew-based native unwinder
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>

#include <mono/metadata/class.h>
#include <mono/metadata/method-builder.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/mono-counters.h>

#include "mini.h"
static gboolean gsharedvt_supported;

void
mono_set_generic_sharing_vt_supported (gboolean supported)
{
	gsharedvt_supported = supported;
}

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

/*
 * mini_is_gsharedvt_type:
 *
 *   Return whenever T references type arguments instantiated with gshared vtypes.
 */
gboolean
mini_is_gsharedvt_type (MonoType *t)
{
	int i;

	if (t->byref)
		return FALSE;
	if ((t->type == MONO_TYPE_VAR || t->type == MONO_TYPE_MVAR) && t->data.generic_param->gshared_constraint && t->data.generic_param->gshared_constraint->type == MONO_TYPE_VALUETYPE)
		return TRUE;
	else if (t->type == MONO_TYPE_GENERICINST) {
		MonoGenericClass *gclass = t->data.generic_class;
		MonoGenericContext *context = &gclass->context;
		MonoGenericInst *inst;

		inst = context->class_inst;
		if (inst) {
			for (i = 0; i < inst->type_argc; ++i)
				if (mini_is_gsharedvt_type (inst->type_argv [i]))
					return TRUE;
		}
		inst = context->method_inst;
		if (inst) {
			for (i = 0; i < inst->type_argc; ++i)
				if (mini_is_gsharedvt_type (inst->type_argv [i]))
					return TRUE;
		}

		return FALSE;
	} else {
		return FALSE;
	}
}

gboolean
mini_is_gsharedvt_klass (MonoClass *klass)
{
	return mini_is_gsharedvt_type (&klass->byval_arg);
}

gboolean
mini_is_gsharedvt_signature (MonoMethodSignature *sig)
{
	int i;

	if (sig->ret && mini_is_gsharedvt_type (sig->ret))
		return TRUE;
	for (i = 0; i < sig->param_count; ++i) {
		if (mini_is_gsharedvt_type (sig->params [i]))
			return TRUE;
	}
	return FALSE;
}

/*
 * mini_is_gsharedvt_variable_type:
 *
 *   Return whenever T refers to a GSHAREDVT type whose size differs depending on the values of type parameters.
 */
gboolean
mini_is_gsharedvt_variable_type (MonoType *t)
{
	if (!mini_is_gsharedvt_type (t))
		return FALSE;
	if (t->type == MONO_TYPE_GENERICINST) {
		MonoGenericClass *gclass = t->data.generic_class;
		MonoGenericContext *context = &gclass->context;
		MonoGenericInst *inst;
		int i;

		if (t->data.generic_class->container_class->byval_arg.type != MONO_TYPE_VALUETYPE || t->data.generic_class->container_class->enumtype)
			return FALSE;

		inst = context->class_inst;
		if (inst) {
			for (i = 0; i < inst->type_argc; ++i)
				if (mini_is_gsharedvt_variable_type (inst->type_argv [i]))
					return TRUE;
		}
		inst = context->method_inst;
		if (inst) {
			for (i = 0; i < inst->type_argc; ++i)
				if (mini_is_gsharedvt_variable_type (inst->type_argv [i]))
					return TRUE;
		}

		return FALSE;
	}
	return TRUE;
}

static gboolean
is_variable_size (MonoType *t)
{
	int i;

	if (t->byref)
		return FALSE;

	if (t->type == MONO_TYPE_VAR || t->type == MONO_TYPE_MVAR) {
		MonoGenericParam *param = t->data.generic_param;

		if (param->gshared_constraint && param->gshared_constraint->type != MONO_TYPE_VALUETYPE && param->gshared_constraint->type != MONO_TYPE_GENERICINST)
			return FALSE;
		if (param->gshared_constraint && param->gshared_constraint->type == MONO_TYPE_GENERICINST)
			return is_variable_size (param->gshared_constraint);
		return TRUE;
	}
	if (t->type == MONO_TYPE_GENERICINST && t->data.generic_class->container_class->byval_arg.type == MONO_TYPE_VALUETYPE) {
		MonoGenericClass *gclass = t->data.generic_class;
		MonoGenericContext *context = &gclass->context;
		MonoGenericInst *inst;

		inst = context->class_inst;
		if (inst) {
			for (i = 0; i < inst->type_argc; ++i)
				if (is_variable_size (inst->type_argv [i]))
					return TRUE;
		}
		inst = context->method_inst;
		if (inst) {
			for (i = 0; i < inst->type_argc; ++i)
				if (is_variable_size (inst->type_argv [i]))
					return TRUE;
		}
	}

	return FALSE;
}

gboolean
mini_is_gsharedvt_sharable_inst (MonoGenericInst *inst)
{
	int i;
	gboolean has_vt = FALSE;

	for (i = 0; i < inst->type_argc; ++i) {
		MonoType *type = inst->type_argv [i];

		if ((MONO_TYPE_IS_REFERENCE (type) || type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) && !mini_is_gsharedvt_type (type)) {
		} else {
			has_vt = TRUE;
		}
	}

	return has_vt;
}

gboolean
mini_is_gsharedvt_sharable_method (MonoMethod *method)
{
	MonoMethodSignature *sig;

	/*
	 * A method is gsharedvt if:
	 * - it has type parameters instantiated with vtypes
	 */
	if (!gsharedvt_supported)
		return FALSE;
	if (method->is_inflated) {
		MonoMethodInflated *inflated = (MonoMethodInflated*)method;
		MonoGenericContext *context = &inflated->context;
		MonoGenericInst *inst;

		if (context->class_inst && context->method_inst) {
			/* At least one inst has to be gsharedvt sharable, and the other normal or gsharedvt sharable */
			gboolean vt1 = mini_is_gsharedvt_sharable_inst (context->class_inst);
			gboolean vt2 = mini_is_gsharedvt_sharable_inst (context->method_inst);

			if ((vt1 && vt2) ||
				(vt1 && mini_generic_inst_is_sharable (context->method_inst, TRUE, FALSE)) ||
				(vt2 && mini_generic_inst_is_sharable (context->class_inst, TRUE, FALSE)))
				;
			else
				return FALSE;
		} else {
			inst = context->class_inst;
			if (inst && !mini_is_gsharedvt_sharable_inst (inst))
				return FALSE;
			inst = context->method_inst;
			if (inst && !mini_is_gsharedvt_sharable_inst (inst))
				return FALSE;
		}
	} else {
		return FALSE;
	}

	sig = mono_method_signature (mono_method_get_declaring_generic_method (method));
	if (!sig)
		return FALSE;

	/*
	if (mini_is_gsharedvt_variable_signature (sig))
		return FALSE;
	*/

	//DEBUG ("GSHAREDVT SHARABLE: %s\n", mono_method_full_name (method, TRUE));

	return TRUE;
}

/*
 * mini_is_gsharedvt_variable_signature:
 *
 *   Return whenever the calling convention used to call SIG varies depending on the values of type parameters used by SIG,
 * i.e. FALSE for swap(T[] arr, int i, int j), TRUE for T get_t ().
 */
gboolean
mini_is_gsharedvt_variable_signature (MonoMethodSignature *sig)
{
	int i;

	if (sig->ret && is_variable_size (sig->ret))
		return TRUE;
	for (i = 0; i < sig->param_count; ++i) {
		MonoType *t = sig->params [i];

		if (is_variable_size (t))
			return TRUE;
	}
	return FALSE;
}
#else

gboolean
mini_is_gsharedvt_type (MonoType *t)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_klass (MonoClass *klass)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_signature (MonoMethodSignature *sig)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_variable_type (MonoType *t)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_sharable_method (MonoMethod *method)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_variable_signature (MonoMethodSignature *sig)
{
	return FALSE;
}

#endif /* !MONO_ARCH_GSHAREDVT_SUPPORTED */
