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

static gboolean
generic_inst_uses_type (MonoGenericInst *inst, MonoType *type)
{
	int i;

	if (!inst)
		return FALSE;

	for (i = 0; i < inst->type_argc; ++i)
		if (mono_metadata_type_equal (type, inst->type_argv [i]))
			return TRUE;
	return FALSE;
}

static int context_check_context_used (MonoGenericContext *context, MonoGenericContext *shared_context);

static int
type_check_context_used (MonoType *type, MonoGenericContext *context, gboolean recursive)
{
	int context_used = 0;

	if (generic_inst_uses_type (context->class_inst, type))
		context_used |= MONO_GENERIC_CONTEXT_USED_CLASS;
	if (generic_inst_uses_type (context->method_inst, type))
		context_used |= MONO_GENERIC_CONTEXT_USED_METHOD;

	if (recursive) {
		int t = mono_type_get_type (type);

		if (t == MONO_TYPE_CLASS)
			context_used |= mono_class_check_context_used (mono_type_get_class (type), context);
		else if (t == MONO_TYPE_GENERICINST) {
			MonoGenericClass *gclass = type->data.generic_class;

			context_used |= context_check_context_used (&gclass->context, context);
			context_used |= mono_class_check_context_used (gclass->container_class, context);
		}
	}

	return context_used;
}

static int
inst_check_context_used (MonoGenericInst *inst, MonoGenericContext *context)
{
	int context_used = 0;
	int i;

	if (!inst)
		return 0;

	for (i = 0; i < inst->type_argc; ++i)
		context_used |= type_check_context_used (inst->type_argv [i], context, TRUE);

	return context_used;
}

static int
context_check_context_used (MonoGenericContext *context, MonoGenericContext *shared_context)
{
	int context_used = 0;

	context_used |= inst_check_context_used (context->class_inst, shared_context);
	context_used |= inst_check_context_used (context->method_inst, shared_context);

	return context_used;
}

int
mono_method_check_context_used (MonoMethod *method, MonoGenericContext *context)
{
	MonoGenericContext *method_context = mono_method_get_context (method);

	if (!method_context)
		return 0;

	return context_check_context_used (method_context, context);
}

int
mono_class_check_context_used (MonoClass *class, MonoGenericContext *context)
{
	int context_used = 0;

	context_used |= type_check_context_used (&class->this_arg, context, FALSE);
	context_used |= type_check_context_used (&class->byval_arg, context, FALSE);

	if (class->generic_class)
		context_used |= context_check_context_used (&class->generic_class->context, context);

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

static gboolean
generic_context_is_sharable (MonoGenericContext *context)
{
	g_assert (context->class_inst || context->method_inst);

	if (context->class_inst && !generic_inst_is_sharable (context->class_inst))
		return FALSE;

	if (context->method_inst && !generic_inst_is_sharable (context->method_inst))
		return FALSE;

	return TRUE;
}

gboolean
mono_method_is_generic_impl (MonoMethod *method)
{
	return method->klass->generic_class != NULL && method->is_inflated;
}

gboolean
mono_method_is_generic_sharable_impl (MonoMethod *method)
{
	if (!mono_method_is_generic_impl (method))
		return FALSE;

	if (method->is_inflated) {
		MonoMethodInflated *inflated = (MonoMethodInflated*)method;
		MonoGenericContext *context = &inflated->context;

		if (!generic_context_is_sharable (context))
			return FALSE;

		g_assert (inflated->declaring);

		if (inflated->declaring->generic_container) {
			g_assert (inflated->declaring->generic_container->type_params);

			if (inflated->declaring->generic_container->type_params->constraints)
				return FALSE;
		}
	}

	if (method->klass->generic_class) {
		if (!generic_context_is_sharable (&method->klass->generic_class->context))
			return FALSE;

		g_assert (method->klass->generic_class->container_class &&
				method->klass->generic_class->container_class->generic_container &&
				method->klass->generic_class->container_class->generic_container->type_params);

		if (method->klass->generic_class->container_class->generic_container->type_params->constraints)
			return FALSE;
	}

	return TRUE;
}

static MonoGenericInst*
share_generic_inst (MonoCompile *cfg, MonoGenericInst *inst)
{
	MonoType **type_argv;
	int i;

	if (!inst)
		return NULL;

	type_argv = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoType*) * inst->type_argc);

	for (i = 0; i < inst->type_argc; ++i)
		type_argv [i] = &mono_defaults.object_class->byval_arg;

	return mono_metadata_get_generic_inst (inst->type_argc, type_argv);
}

MonoGenericContext*
mono_make_shared_context (MonoCompile *cfg, MonoGenericContext *context)
{
	MonoGenericContext *shared = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoGenericContext));

	shared->class_inst = share_generic_inst (cfg, context->class_inst);
	shared->method_inst = share_generic_inst (cfg, context->method_inst);

	return shared;
}

MonoType*
mini_get_basic_type_from_generic (MonoCompile *cfg, MonoType *type)
{
	if (!type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR)) {
		/* FIXME: we support sharing only of reference types */
		g_assert (cfg->generic_shared);
		return &mono_defaults.object_class->byval_arg;
	}
	return type;
}
