/**
 * \file
 */

#ifndef __MONO_METADATA_VERIFY_INTERNAL_H__
#define __MONO_METADATA_VERIFY_INTERNAL_H__

#include <glib.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>

typedef enum {
	MONO_VERIFIER_MODE_OFF,
	MONO_VERIFIER_PE_ONLY,
	MONO_VERIFIER_MODE_VALID,
	MONO_VERIFIER_MODE_VERIFIABLE,
	MONO_VERIFIER_MODE_STRICT
} MiniVerifierMode;

void mono_verifier_set_mode (MiniVerifierMode mode);
void mono_verifier_enable_verify_all (void);

gboolean mono_verifier_is_enabled_for_image (MonoImage *image);
gboolean mono_verifier_is_enabled_for_method (MonoMethod *method);
gboolean mono_verifier_is_enabled_for_class (MonoClass *klass);
gboolean mono_verifier_is_enabled_for_pe_only (void);

gboolean mono_verifier_is_method_full_trust (MonoMethod *method);
gboolean mono_verifier_is_class_full_trust (MonoClass *klass);
gboolean mono_verifier_class_is_valid_generic_instantiation (MonoClass *klass);
gboolean mono_verifier_is_method_valid_generic_instantiation (MonoMethod *method);

gboolean mono_verifier_verify_class (MonoClass *klass);

GSList* mono_method_verify_with_current_settings (MonoMethod *method, gboolean skip_visibility, gboolean is_fulltrust);

gboolean mono_verifier_verify_pe_data (MonoImage *image, MonoError *error);
gboolean mono_verifier_verify_cli_data (MonoImage *image, MonoError *error);
gboolean mono_verifier_verify_table_data (MonoImage *image, MonoError *error);

gboolean mono_verifier_verify_full_table_data (MonoImage *image, MonoError *error);

gboolean mono_verifier_verify_field_signature (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_method_header (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_method_signature (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_standalone_signature (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_typespec_signature (MonoImage *image, guint32 offset, guint32 token, MonoError *error);
gboolean mono_verifier_verify_methodspec_signature (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_string_signature (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_cattr_blob (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_cattr_content (MonoImage *image, MonoMethod *ctor, const guchar *data, guint32 size, MonoError *error);
gboolean mono_verifier_is_sig_compatible (MonoImage *image, MonoMethod *method, MonoMethodSignature *signature);
gboolean mono_verifier_verify_memberref_method_signature (MonoImage *image, guint32 offset, MonoError *error);
gboolean mono_verifier_verify_memberref_field_signature (MonoImage *image, guint32 offset, MonoError *error);

gboolean mono_verifier_verify_typeref_row (MonoImage *image, guint32 row, MonoError *error);
gboolean mono_verifier_verify_methodimpl_row (MonoImage *image, guint32 row, MonoError *error);
gboolean mono_verifier_is_signature_compatible (MonoMethodSignature *target, MonoMethodSignature *candidate);

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
