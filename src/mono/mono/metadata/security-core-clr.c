/**
 * \file
 * CoreCLR security
 *
 * Authors:
 *	Mark Probst <mark.probst@gmail.com>
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright 2007-2010 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/mono-logger-internals.h>

#include "security-core-clr.h"

gboolean mono_security_core_clr_test = FALSE;

static MonoSecurityCoreCLROptions security_core_clr_options = MONO_SECURITY_CORE_CLR_OPTIONS_DEFAULT;

/**
 * mono_security_core_clr_set_options:
 * \param options the new options for the coreclr system to use
 *
 * By default, the CoreCLRs security model forbids execution trough reflection of methods not visible from the calling code.
 * Even if the method being called is not in a platform assembly. For non moonlight CoreCLR users this restriction does not
 * make a lot of sense, since the author could have just changed the non platform assembly to allow the method to be called.
 * This function allows specific relaxations from the default behaviour to be set.
 *
 * Use \c MONO_SECURITY_CORE_CLR_OPTIONS_DEFAULT for the default coreclr coreclr behaviour as used in Moonlight.
 *
 * Use \c MONO_SECURITY_CORE_CLR_OPTIONS_RELAX_REFLECTION to allow transparent code to execute methods and access 
 * fields that are not in platformcode, even if those methods and fields are private or otherwise not visible to the calling code.
 *
 * Use \c MONO_SECURITY_CORE_CLR_OPTIONS_RELAX_DELEGATE to allow delegates to be created that point at methods that are not in
 * platformcode even if those methods and fields are private or otherwise not visible to the calling code.
 *
 */
void
mono_security_core_clr_set_options (MonoSecurityCoreCLROptions options) {
	security_core_clr_options = options;
}

/**
 * mono_security_core_clr_get_options:
 *
 * Retrieves the current options used by the coreclr system.
 */

MonoSecurityCoreCLROptions
mono_security_core_clr_get_options ()
{
	return security_core_clr_options;
}

/*
 * default_platform_check:
 *
 *	Default platform check. Always TRUE for current corlib (minimum 
 *	trust-able subset) otherwise return FALSE. Any real CoreCLR host
 *	should provide its own callback to define platform code (i.e.
 *	this default is meant for test only).
 */
static gboolean
default_platform_check (const char *image_name)
{
	if (mono_defaults.corlib) {
		return (strcmp (mono_defaults.corlib->name, image_name) == 0);
	} else {
		/* this can get called even before we load corlib (e.g. the EXE itself) */
		const char *corlib = MONO_ASSEMBLY_CORLIB_NAME ".dll";
		int ilen = strlen (image_name);
		int clen = strlen (corlib);
		return ((ilen >= clen) && (strcmp (corlib, image_name + ilen - clen) == 0));
	}
}

static MonoCoreClrPlatformCB platform_callback = default_platform_check;

/*
 * mono_security_core_clr_determine_platform_image:
 *
 *  Call the supplied callback (from mono_security_set_core_clr_platform_callback) 
 *  to determine if this image represents platform code.
 */
gboolean
mono_security_core_clr_determine_platform_image (MonoImage *image)
{
	return platform_callback (image->name);
}

/*
 * mono_security_set_core_clr_platform_callback:
 *
 *  Set the callback function that will be used to determine if an image
 *  is part, or not, of the platform code.
 */
void
mono_security_set_core_clr_platform_callback (MonoCoreClrPlatformCB callback)
{
	platform_callback = callback;
}

/*
 * mono_security_core_clr_is_platform_image:
 *
 *   Return the (cached) boolean value indicating if this image represent platform code
 */
gboolean
mono_security_core_clr_is_platform_image (MonoImage *image)
{
	return image->core_clr_platform_code;
}

void
mono_security_core_clr_check_inheritance (MonoClass *klass)
{
}

void
mono_security_core_clr_check_override (MonoClass *klass, MonoMethod *override, MonoMethod *base)
{
}

gboolean
mono_security_core_clr_require_elevated_permissions (void)
{
	return FALSE;
}

gboolean
mono_security_core_clr_ensure_reflection_access_field (MonoClassField *field, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_security_core_clr_ensure_reflection_access_method (MonoMethod *method, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_security_core_clr_ensure_delegate_creation (MonoMethod *method, MonoError *error)
{
	error_init (error);
	return TRUE;
}

MonoException*
mono_security_core_clr_ensure_dynamic_method_resolved_object (gpointer ref, MonoClass *handle_class)
{
	return NULL;
}

gboolean
mono_security_core_clr_can_access_internals (MonoImage *accessing, MonoImage* accessed)
{
	return TRUE;
}

MonoException*
mono_security_core_clr_is_field_access_allowed (MonoMethod *caller, MonoClassField *field)
{
	return NULL;
}

MonoException*
mono_security_core_clr_is_call_allowed (MonoMethod *caller, MonoMethod *callee)
{
	return NULL;
}

MonoSecurityCoreCLRLevel
mono_security_core_clr_class_level (MonoClass *klass)
{
	return MONO_SECURITY_CORE_CLR_TRANSPARENT;
}

MonoSecurityCoreCLRLevel
mono_security_core_clr_field_level (MonoClassField *field, gboolean with_class_level)
{
	return MONO_SECURITY_CORE_CLR_TRANSPARENT;
}

MonoSecurityCoreCLRLevel
mono_security_core_clr_method_level (MonoMethod *method, gboolean with_class_level)
{
	return MONO_SECURITY_CORE_CLR_TRANSPARENT;
}

void
mono_security_enable_core_clr ()
{
}
