/**
 * \file
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Rodrigo Kumpera
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/dynamic-image-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/mono-basic-block.h>
#include <mono/metadata/attrdefs.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-error-internals.h>
#include <string.h>
#include <ctype.h>

/*
 * Returns TRUE if @type is VAR or MVAR
 */
static gboolean
mono_type_is_generic_argument (MonoType *type)
{
	return type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR;
}

/*A side note here. We don't need to check if arguments are broken since this
is only need to be done by the runtime before realizing the type.
*/
static gboolean
is_valid_generic_instantiation (MonoGenericContainer *gc, MonoGenericContext *context, MonoGenericInst *ginst)
{
	ERROR_DECL (error);
	int i;

	if (ginst->type_argc != gc->type_argc)
		return FALSE;

	for (i = 0; i < gc->type_argc; ++i) {
		MonoGenericParamInfo *param_info = mono_generic_container_get_param_info (gc, i);
		MonoClass *paramClass;
		MonoClass **constraints;
		MonoType *param_type = ginst->type_argv [i];

		/*it's not our job to validate type variables*/
		if (mono_type_is_generic_argument (param_type))
			continue;

		paramClass = mono_class_from_mono_type_internal (param_type);


		/* A GTD can't be a generic argument.
		 *
		 * Due to how types are encoded we must check for the case of a genericinst MonoType and GTD MonoClass.
		 * This happens in cases such as: class Foo<T>  { void X() { new Bar<T> (); } }
		 *
		 * Open instantiations can have GTDs as this happens when one type is instantiated with others params
		 * and the former has an expansion into the later. For example:
		 * class B<K> {}
		 * class A<T>: B<K> {}
		 * The type A <K> has a parent B<K>, that is inflated into the GTD B<>.
		 * Since A<K> is open, thus not instantiatable, this is valid.
		 */
		if (mono_class_is_gtd (paramClass) && param_type->type != MONO_TYPE_GENERICINST && !ginst->is_open)
			return FALSE;

		/*it's not safe to call mono_class_init_internal from here*/
		if (mono_class_is_ginst (paramClass) && !m_class_is_inited (paramClass)) {
			if (!mono_verifier_class_is_valid_generic_instantiation (paramClass))
				return FALSE;
		}

		if (!param_info->constraints && !(param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK))
			continue;

		if ((param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) && (!m_class_is_valuetype (paramClass) || mono_class_is_nullable (paramClass)))
			return FALSE;

		if ((param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) && m_class_is_valuetype (paramClass))
			return FALSE;

		if ((param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) && !m_class_is_valuetype (paramClass) && !mono_class_has_default_constructor (paramClass, TRUE))
			return FALSE;

		if (!param_info->constraints)
			continue;

		for (constraints = param_info->constraints; *constraints; ++constraints) {
			MonoClass *ctr = *constraints;
			MonoType *inflated;

			inflated = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (ctr), context, error);
			if (!is_ok (error)) {
				mono_error_cleanup (error);
				return FALSE;
			}
			ctr = mono_class_from_mono_type_internal (inflated);
			mono_metadata_free_type (inflated);

			/*FIXME maybe we need the same this as verifier_class_is_assignable_from*/
			if (!mono_class_is_assignable_from_slow (ctr, paramClass))
				return FALSE;
		}
	}
	return TRUE;
}

gboolean
mono_verifier_class_is_valid_generic_instantiation (MonoClass *klass)
{
	MonoGenericClass *gklass = mono_class_get_generic_class (klass);
	MonoGenericInst *ginst = gklass->context.class_inst;
	MonoGenericContainer *gc = mono_class_get_generic_container (gklass->container_class);
	return is_valid_generic_instantiation (gc, &gklass->context, ginst);
}

gboolean
mono_verifier_is_method_valid_generic_instantiation (MonoMethod *method)
{
	if (!method->is_inflated)
		return TRUE;
	MonoMethodInflated *gmethod = (MonoMethodInflated *)method;
	MonoGenericInst *ginst = gmethod->context.method_inst;
	MonoGenericContainer *gc = mono_method_get_generic_container (gmethod->declaring);
	if (!gc) /*non-generic inflated method - it's part of a generic type  */
		return TRUE;
	return is_valid_generic_instantiation (gc, &gmethod->context, ginst);
}


GSList*
mono_method_verify (MonoMethod *method, int level)
{
	/* The verifier was disabled at compile time */
	return NULL;
}

void
mono_free_verify_list (GSList *list)
{
	/* The verifier was disabled at compile time */
	/* will always be null if verifier is disabled */
}


char*
mono_verify_corlib (void)
{
        return NULL;
}
