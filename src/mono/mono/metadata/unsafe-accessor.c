// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "config.h"
#include <glib.h>
#include <stdlib.h>
#include <stdio.h>
#include "mono/metadata/metadata.h"
#include "mono/metadata/image.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-init.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/object-internals.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/metadata/unsafe-accessor.h"
#include <mono/metadata/debug-helpers.h>


static MonoMethod *
find_method_simple (MonoClass *klass, const char *name, const char *qname, const char *fqname,
		      MonoMethodSignature *sig, MonoClass *from_class, gboolean ignore_cmods, MonoError *error)
{
	MonoMethod *method_maybe = NULL;

	/* Search directly in the metadata to avoid calling setup_methods () */
	error_init (error);

	MonoImage *klass_image = m_class_get_image (klass);
	/* FIXME: !mono_class_is_ginst (from_class) condition causes test failures. */
	if (m_class_get_type_token (klass) && !image_is_dynamic (klass_image) && !m_class_get_methods (klass) && !m_class_get_rank (klass) && klass == from_class && !mono_class_is_ginst (from_class)) {
		int first_idx = mono_class_get_first_method_idx (klass);
		int mcount = mono_class_get_method_count (klass);
		for (int i = 0; i < mcount; ++i) {
			guint32 cols [MONO_METHOD_SIZE];
			MonoMethod *method;
			const char *m_name;
			MonoMethodSignature *other_sig;

			mono_metadata_decode_table_row (klass_image, MONO_TABLE_METHOD, first_idx + i, cols, MONO_METHOD_SIZE);

			m_name = mono_metadata_string_heap (klass_image, cols [MONO_METHOD_NAME]);

			if (!((fqname && !strcmp (m_name, fqname)) ||
				  (qname && !strcmp (m_name, qname)) ||
				  (name && !strcmp (m_name, name))))
				continue;

			method = mono_get_method_checked (klass_image, MONO_TOKEN_METHOD_DEF | (first_idx + i + 1), klass, NULL, error);
			if (!is_ok (error)) //bail out if we hit a loader error
				return NULL;

			// Check method signature
			if (method) {
				other_sig = mono_method_signature_checked (method, error);
				if (!is_ok (error)) //bail out if we hit a loader error
					return NULL;
				if (other_sig) {
					gboolean found = ignore_cmods ? mono_metadata_signature_equal_ignore_custom_modifier (sig, other_sig) : mono_metadata_signature_equal (sig, other_sig);
					if (found) {
						if (method_maybe != NULL) {
							if (ignore_cmods) {
								MonoMethod *precise_match = find_method_simple (klass, name, qname, fqname, sig, from_class, FALSE, error);
								if (precise_match)
									return precise_match;
							}
							mono_error_set_generic_error (error, "System.Reflection", "AmbiguousMatchException", "Ambiguity in binding of UnsafeAccessorAttribute.");
							return NULL;
						}
						method_maybe = method;
					}
				}
			}
		}
		return method_maybe;
	}

	return NULL;
}

typedef struct MethodLookupResultInfo {
	int i;
	MonoMethod *m;
	gboolean matched;
} MethodLookupResultInfo;

static MethodLookupResultInfo *
find_method_slow (MonoClass *klass, const char *name, const char *qname, const char *fqname,
		      MonoMethodSignature *sig, gboolean ignore_cmods, MonoError *error)
{
	gpointer iter = NULL;
	MethodLookupResultInfo *result = (MethodLookupResultInfo *)g_malloc0 (sizeof (MethodLookupResultInfo));
	int i = -1;
	MonoMethod *m = NULL;
	gboolean matched = FALSE;
	result->i = i;
	result->m = m;
	result->matched = matched;

	/* FIXME: metadata-update iterating using
	 * mono_class_get_methods will break if `m` is NULL.  Need to
	 * reconcile with the `if (!m)` "we must cope" comment below.
	 */
	while ((m = mono_class_get_methods (klass, &iter))) {
		i++;
		MonoMethodSignature *msig;

		/* We must cope with failing to load some of the types. */
		if (!m)
			continue;

		if (!((fqname && !strcmp (m->name, fqname)) ||
		      (qname && !strcmp (m->name, qname)) ||
		      (name && !strcmp (m->name, name))))
			continue;
		msig = mono_method_signature_checked (m, error);
		if (!is_ok (error)) //bail out if we hit a loader error
			return NULL;

		if (!msig)
			continue;

		gboolean found = FALSE;
		if (ignore_cmods)
			found = sig->call_convention == MONO_CALL_VARARG ? mono_metadata_signature_equal_vararg_ignore_custom_modifier (sig, msig) : mono_metadata_signature_equal_ignore_custom_modifier (sig, msig);
		else
			found = sig->call_convention == MONO_CALL_VARARG ? mono_metadata_signature_equal_vararg (sig, msig) : mono_metadata_signature_equal (sig, msig);
		
		if (found) {
			if (matched) {
				if (ignore_cmods) {
					MethodLookupResultInfo *precise_match = find_method_slow (klass, name, qname, fqname, sig, FALSE, error);
					if (precise_match->m)
						return precise_match;
				}
				mono_error_set_generic_error (error, "System.Reflection", "AmbiguousMatchException", "Ambiguity in binding of UnsafeAccessorAttribute.");
				result->i = -1;
				result->m = NULL;
				result->matched = FALSE;
				return result;
			}
			matched = TRUE;
			result->i = i;
			result->m = m;
			result->matched = matched;
		}
	}

	return result;
}

static MonoMethod *
find_method_in_class_unsafe_accessor (MonoClass *klass, const char *name, const char *qname, const char *fqname,
		      MonoMethodSignature *sig, MonoClass *from_class, gboolean ignore_cmods, MonoError *error)
{
	MonoMethod *method = NULL;
	if (sig->call_convention != MONO_CALL_VARARG)
		method = find_method_simple (klass, name, qname, fqname, sig, from_class, ignore_cmods, error);
	if (method)
		return method;
	if (!is_ok(error) && mono_error_get_error_code (error) == MONO_ERROR_GENERIC)
		return NULL;

	mono_class_setup_methods (klass); /* FIXME don't swallow the error here. */
	/*
	We can't fail lookup of methods otherwise the runtime will fail with MissingMethodException instead of TypeLoadException.
	See mono/tests/generic-type-load-exception.2.il
	FIXME we should better report this error to the caller
	 */
	if (!m_class_get_methods (klass) || mono_class_has_failure (klass)) {
		ERROR_DECL (cause_error);
		mono_error_set_for_class_failure (cause_error, klass);
		mono_error_set_type_load_class (error, klass, "Could not find method '%s' due to a type load error: %s", name, mono_error_get_message (cause_error));
		mono_error_cleanup (cause_error);
		return NULL;
	}
	
	MethodLookupResultInfo *result = find_method_slow (klass, name, qname, fqname, sig, ignore_cmods, error);
	if (!is_ok(error) && mono_error_get_error_code (error) == MONO_ERROR_GENERIC)
		return NULL;


	g_assert (result != NULL);
	if (result->matched) {
		return result->m;
	}

	g_free (result);
	return NULL;
}

static gboolean
generic_argument_satisfies_constraints (MonoGenericParamInfo *target_info, MonoType *candidate_type, MonoGenericContext *context, MonoError *error)
{
	MonoClass *candidate_class = mono_class_from_mono_type_internal (candidate_type);
	guint16 target_flags = target_info->flags;

	if (candidate_type->type == MONO_TYPE_VAR || candidate_type->type == MONO_TYPE_MVAR) {
		MonoGenericParam *candidate_param = m_type_data_get_generic_param_unchecked (candidate_type);
		MonoGenericParamInfo *candidate_info = mono_generic_param_info (candidate_param);
		guint16 candidate_flags = candidate_info->flags;
		gboolean class_constraint_satisfied = (candidate_flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) != 0;
		gboolean valuetype_constraint_satisfied = (candidate_flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) != 0;

		if ((candidate_flags & GENERIC_PARAMETER_ATTRIBUTE_ALLOW_BYREFLIKE_CONSTRAINTS) &&
			!(target_flags & GENERIC_PARAMETER_ATTRIBUTE_ALLOW_BYREFLIKE_CONSTRAINTS))
			return FALSE;

		if (candidate_info->constraints) {
			for (MonoClass **constraint = candidate_info->constraints; *constraint; ++constraint) {
				MonoClass *constraint_class = *constraint;
				MonoType *constraint_type = m_class_get_byval_arg (constraint_class);
				if (!MONO_CLASS_IS_INTERFACE_INTERNAL (constraint_class)) {
					if (mono_type_is_reference (constraint_type))
						class_constraint_satisfied = TRUE;
					else if (constraint_type->type != MONO_TYPE_VAR && constraint_type->type != MONO_TYPE_MVAR)
						valuetype_constraint_satisfied = TRUE;
				}
			}
		}

		if ((target_flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) && !class_constraint_satisfied)
			return FALSE;
		if ((target_flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) && !valuetype_constraint_satisfied)
			return FALSE;
		if ((target_flags & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) &&
			!(candidate_flags & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) && !valuetype_constraint_satisfied)
			return FALSE;
	} else {
		if (m_class_is_byreflike (candidate_class) && !(target_flags & GENERIC_PARAMETER_ATTRIBUTE_ALLOW_BYREFLIKE_CONSTRAINTS))
			return FALSE;
		if ((target_flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) &&
			(!m_class_is_valuetype (candidate_class) || mono_class_is_nullable (candidate_class)))
			return FALSE;
		if ((target_flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) && m_class_is_valuetype (candidate_class))
			return FALSE;
		if ((target_flags & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) && !m_class_is_valuetype (candidate_class) &&
			(!mono_class_has_default_constructor (candidate_class, TRUE) || mono_class_is_abstract (candidate_class)))
			return FALSE;
	}

	if (target_info->constraints) {
		for (MonoClass **constraint = target_info->constraints; *constraint; ++constraint) {
			MonoType *inflated = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (*constraint), context, error);
			if (!is_ok (error))
				return FALSE;

			MonoClass *inflated_class = mono_class_from_mono_type_internal (inflated);
			gboolean satisfied = mono_class_is_assignable_from_internal (inflated_class, candidate_class);
			mono_metadata_free_type (inflated);
			if (!satisfied)
				return FALSE;
		}
	}

	return TRUE;
}

static gboolean
verify_generic_container_constraints (MonoGenericContainer *target_container, MonoGenericInst *candidate_inst,
	MonoGenericContext *context, const char *message, MonoError *error)
{
	if (!target_container)
		return TRUE;

	if (!candidate_inst || target_container->type_argc != candidate_inst->type_argc) {
		mono_error_set_generic_error (error, "System", "InvalidProgramException", "%s", message);
		return FALSE;
	}

	for (int i = 0; i < target_container->type_argc; ++i) {
		MonoGenericParamInfo *target_info = mono_generic_container_get_param_info (target_container, i);
		if (!generic_argument_satisfies_constraints (target_info, candidate_inst->type_argv [i], context, error)) {
			if (is_ok (error))
				mono_error_set_generic_error (error, "System", "InvalidProgramException", "%s", message);
			return FALSE;
		}
	}

	return TRUE;
}

gboolean
mono_unsafe_accessor_verify_constraints (MonoMethod *accessor_method, MonoClass *target_class, MonoMethod *target_method, MonoError *error)
{
	error_init (error);

	MonoGenericContext context = { NULL, NULL };
	MonoGenericContainer *target_class_container = NULL;
	MonoGenericInst *class_inst = NULL;
	if (mono_class_is_ginst (target_class)) {
		MonoGenericClass *generic_class = mono_class_get_generic_class (target_class);
		target_class_container = mono_class_get_generic_container (generic_class->container_class);
		class_inst = generic_class->context.class_inst;
		context.class_inst = class_inst;
	}

	MonoGenericContainer *accessor_method_container = mono_method_get_generic_container (accessor_method);
	MonoGenericInst *method_inst = accessor_method_container ? accessor_method_container->context.method_inst : NULL;
	context.method_inst = method_inst;

	if (!verify_generic_container_constraints (target_class_container, class_inst, &context,
		"Generic type constraints of the UnsafeAccessor declaration do not match the target.", error))
		return FALSE;

	MonoGenericContainer *target_method_container = mono_method_get_generic_container (target_method);
	if (!verify_generic_container_constraints (target_method_container, method_inst, &context,
		"Generic method constraints of the UnsafeAccessor declaration do not match the target.", error))
		return FALSE;

	return TRUE;
}

MonoMethod*
mono_unsafe_accessor_find_ctor (MonoClass *in_class, MonoMethodSignature *sig, MonoClass *from_class, MonoError *error)
{
	return find_method_in_class_unsafe_accessor (in_class, ".ctor", /*qname*/NULL, /*fqname*/NULL, sig, from_class, TRUE, error);
}

MonoMethod*
mono_unsafe_accessor_find_method (MonoClass *in_class, const char *name, MonoMethodSignature *sig, MonoClass *from_class, MonoError *error)
{
	// This doesn't work for constructors because find_method explicitly disallows ".ctor" and ".cctor"
	return find_method_in_class_unsafe_accessor (in_class, name, /*qname*/NULL, /*fqname*/NULL, sig, from_class, TRUE, error);
}
