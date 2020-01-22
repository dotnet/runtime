/**
 * \file
 * CoreCLR security
 *
 * Author:
 *	Mark Probst <mark.probst@gmail.com>
 *
 * (C) 2007, 2010 Novell, Inc
 */

#ifndef _MONO_METADATA_SECURITY_CORE_CLR_H_
#define _MONO_METADATA_SECURITY_CORE_CLR_H_

#include <glib.h>
#include <mono/metadata/reflection.h>
#include <mono/utils/mono-compiler.h>

typedef enum {
	/* We compare these values as integers, so the order must not
	   be changed. */
	MONO_SECURITY_CORE_CLR_TRANSPARENT = 0,
	MONO_SECURITY_CORE_CLR_SAFE_CRITICAL,
	MONO_SECURITY_CORE_CLR_CRITICAL
} MonoSecurityCoreCLRLevel;

typedef enum {
	//The following flags can be used in combination, and control specific behaviour of the CoreCLR securit system.

	//Default coreclr behaviour, as used in moonlight.
	MONO_SECURITY_CORE_CLR_OPTIONS_DEFAULT = 0,

	//Allow transparent code to execute methods and access fields that are not in platformcode,
	//even if those methods and fields are private or otherwise not visible to the calling code.
	MONO_SECURITY_CORE_CLR_OPTIONS_RELAX_REFLECTION = 1,

	//Allow delegates to be created that point at methods that are not in platformcode,
	//even if those methods and fields are private or otherwise not visible to the calling code.
	MONO_SECURITY_CORE_CLR_OPTIONS_RELAX_DELEGATE = 2
} MonoSecurityCoreCLROptions;

extern gboolean mono_security_core_clr_test;

extern void mono_security_core_clr_check_inheritance (MonoClass *klass);
extern void mono_security_core_clr_check_override (MonoClass *klass, MonoMethod *override, MonoMethod *base);

extern gboolean
mono_security_core_clr_ensure_reflection_access_field (MonoClassField *field, MonoError *error);
extern gboolean
mono_security_core_clr_ensure_reflection_access_method (MonoMethod *method, MonoError *error);
extern gboolean mono_security_core_clr_ensure_delegate_creation (MonoMethod *method, MonoError *error);
extern MonoException* mono_security_core_clr_ensure_dynamic_method_resolved_object (gpointer ref, MonoClass *handle_class);

extern gboolean mono_security_core_clr_can_access_internals (MonoImage *accessing, MonoImage* accessed);

extern MonoException* mono_security_core_clr_is_field_access_allowed (MonoMethod *caller, MonoClassField *field);
extern MonoException* mono_security_core_clr_is_call_allowed (MonoMethod *caller, MonoMethod *callee);

extern MonoSecurityCoreCLRLevel mono_security_core_clr_class_level (MonoClass *klass);
extern MonoSecurityCoreCLRLevel mono_security_core_clr_field_level (MonoClassField *field, gboolean with_class_level);
extern MonoSecurityCoreCLRLevel mono_security_core_clr_method_level (MonoMethod *method, gboolean with_class_level);

extern gboolean mono_security_core_clr_is_platform_image (MonoImage *image);
extern gboolean mono_security_core_clr_determine_platform_image (MonoImage *image);

MONO_API gboolean mono_security_core_clr_require_elevated_permissions (void);

MONO_API void mono_security_core_clr_set_options (MonoSecurityCoreCLROptions options);
MONO_API MonoSecurityCoreCLROptions mono_security_core_clr_get_options (void);

#endif	/* _MONO_METADATA_SECURITY_CORE_CLR_H_ */
