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
