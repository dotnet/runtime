/**
 * \file
 */

#ifndef __MONO_METADATA_VERIFY_INTERNAL_H__
#define __MONO_METADATA_VERIFY_INTERNAL_H__

#include <glib.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>

gboolean mono_verifier_class_is_valid_generic_instantiation (MonoClass *klass);
MONO_COMPONENT_API gboolean mono_verifier_is_method_valid_generic_instantiation (MonoMethod *method);

/*Token validation macros and functions */
#define IS_MEMBER_REF(token) (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF)
#define IS_METHOD_DEF(token) (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
#define IS_METHOD_SPEC(token) (mono_metadata_token_table (token) == MONO_TABLE_METHODSPEC)
#define IS_FIELD_DEF(token) (mono_metadata_token_table (token) == MONO_TABLE_FIELD)

#define IS_TYPE_REF(token) (mono_metadata_token_table (token) == MONO_TABLE_TYPEREF)
#define IS_TYPE_DEF(token) (mono_metadata_token_table (token) == MONO_TABLE_TYPEDEF)
#define IS_TYPE_SPEC(token) (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC)
#define IS_METHOD_DEF_OR_REF_OR_SPEC(token) (IS_METHOD_DEF (token) || IS_MEMBER_REF (token) || IS_METHOD_SPEC (token))
#define IS_TYPE_DEF_OR_REF_OR_SPEC(token) (IS_TYPE_DEF (token) || IS_TYPE_REF (token) || IS_TYPE_SPEC (token))
#define IS_FIELD_DEF_OR_REF(token) (IS_FIELD_DEF (token) || IS_MEMBER_REF (token))

#endif  /* __MONO_METADATA_VERIFY_INTERNAL_H__ */
