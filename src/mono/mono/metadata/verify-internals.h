#ifndef __MONO_METADATA_VERIFY_INTERNAL_H__
#define __MONO_METADATA_VERIFY_INTERNAL_H__

#include <mono/metadata/metadata.h>

G_BEGIN_DECLS

typedef enum {
	MONO_VERIFIER_MODE_OFF,
	MONO_VERIFIER_MODE_VALID,
	MONO_VERIFIER_MODE_VERIFIABLE,
	MONO_VERIFIER_MODE_STRICT
} MiniVerifierMode;

void mono_verifier_set_mode (MiniVerifierMode mode) MONO_INTERNAL;
void mono_verifier_enable_verify_all (void) MONO_INTERNAL;

gboolean mono_verifier_is_enabled_for_image (MonoImage *image) MONO_INTERNAL;
gboolean mono_verifier_is_enabled_for_method (MonoMethod *method) MONO_INTERNAL;
gboolean mono_verifier_is_enabled_for_class (MonoClass *klass) MONO_INTERNAL;

gboolean mono_verifier_is_method_full_trust (MonoMethod *method) MONO_INTERNAL;
gboolean mono_verifier_is_class_full_trust (MonoClass *klass) MONO_INTERNAL;
gboolean mono_verifier_class_is_valid_generic_instantiation (MonoClass *class) MONO_INTERNAL;
gboolean mono_verifier_is_method_valid_generic_instantiation (MonoMethod *method) MONO_INTERNAL;

gboolean mono_verifier_verify_class (MonoClass *klass) MONO_INTERNAL;

GSList* mono_method_verify_with_current_settings (MonoMethod *method, gboolean skip_visibility) MONO_INTERNAL;

gboolean mono_verifier_verify_pe_data (MonoImage *image, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_cli_data (MonoImage *image, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_table_data (MonoImage *image, GSList **error_list) MONO_INTERNAL;

gboolean mono_verifier_verify_full_table_data (MonoImage *image, GSList **error_list) MONO_INTERNAL;

gboolean mono_verifier_verify_field_signature (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_method_header (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_method_signature (MonoImage *image, guint32 offset, MonoError *error) MONO_INTERNAL;
gboolean mono_verifier_verify_standalone_signature (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_typespec_signature (MonoImage *image, guint32 offset, guint32 token, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_methodspec_signature (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_string_signature (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_cattr_blob (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_cattr_content (MonoImage *image, MonoMethod *ctor, const guchar *data, guint32 size, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_is_sig_compatible (MonoImage *image, MonoMethod *method, MonoMethodSignature *signature) MONO_INTERNAL;
gboolean mono_verifier_verify_memberref_method_signature (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;
gboolean mono_verifier_verify_memberref_field_signature (MonoImage *image, guint32 offset, GSList **error_list) MONO_INTERNAL;

gboolean mono_verifier_verify_typeref_row (MonoImage *image, guint32 row, MonoError *error) MONO_INTERNAL;
gboolean mono_verifier_verify_methodimpl_row (MonoImage *image, guint32 row, MonoError *error) MONO_INTERNAL;
gboolean mono_verifier_is_signature_compatible (MonoMethodSignature *target, MonoMethodSignature *candidate) MONO_INTERNAL;
G_END_DECLS

#endif  /* __MONO_METADATA_VERIFY_INTERNAL_H__ */

