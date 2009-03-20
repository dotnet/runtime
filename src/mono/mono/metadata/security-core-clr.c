/*
 * security-core-clr.c: CoreCLR security
 *
 * Author:
 *	Mark Probst <mark.probst@gmail.com>
 *
 * Copyright 2007-2009 Novell, Inc (http://www.novell.com)
 */

#include <mono/metadata/class-internals.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/verify-internals.h>

#include "security-core-clr.h"

gboolean mono_security_core_clr_test = FALSE;

static MonoClass*
security_critical_attribute (void)
{
	static MonoClass *class = NULL;

	if (!class) {
		class = mono_class_from_name (mono_defaults.corlib, "System.Security", 
			"SecurityCriticalAttribute");
	}
	g_assert (class);
	return class;
}

static MonoClass*
security_safe_critical_attribute (void)
{
	static MonoClass *class = NULL;

	if (!class) {
		class = mono_class_from_name (mono_defaults.corlib, "System.Security", 
			"SecuritySafeCriticalAttribute");
	}
	g_assert (class);
	return class;
}

static MonoSecurityCoreCLRLevel
mono_security_core_clr_level_from_cinfo (MonoCustomAttrInfo *cinfo, MonoImage *image)
{
	int level = MONO_SECURITY_CORE_CLR_TRANSPARENT;

	if (cinfo && mono_custom_attrs_has_attr (cinfo, security_safe_critical_attribute ()))
		level = MONO_SECURITY_CORE_CLR_SAFE_CRITICAL;
	if (cinfo && mono_custom_attrs_has_attr (cinfo, security_critical_attribute ()))
		level = MONO_SECURITY_CORE_CLR_CRITICAL;

	return level;
}

MonoSecurityCoreCLRLevel
mono_security_core_clr_class_level (MonoClass *class)
{
	MonoCustomAttrInfo *cinfo;
	MonoSecurityCoreCLRLevel level = MONO_SECURITY_CORE_CLR_TRANSPARENT;

	/* non-platform code is always Transparent - whatever the attributes says */
	if (!mono_security_core_clr_test && !mono_security_core_clr_is_platform_image (class->image))
		return level;

	cinfo = mono_custom_attrs_from_class (class);
	if (cinfo) {
		level = mono_security_core_clr_level_from_cinfo (cinfo, class->image);
		mono_custom_attrs_free (cinfo);
	}

	if (level == MONO_SECURITY_CORE_CLR_TRANSPARENT && class->nested_in)
		level = mono_security_core_clr_class_level (class->nested_in);

	return level;
}

MonoSecurityCoreCLRLevel
mono_security_core_clr_method_level (MonoMethod *method, gboolean with_class_level)
{
	MonoCustomAttrInfo *cinfo;
	MonoSecurityCoreCLRLevel level = MONO_SECURITY_CORE_CLR_TRANSPARENT;

	/* non-platform code is always Transparent - whatever the attributes says */
	if (!mono_security_core_clr_test && !mono_security_core_clr_is_platform_image (method->klass->image))
		return level;

	cinfo = mono_custom_attrs_from_method (method);
	if (cinfo) {
		level = mono_security_core_clr_level_from_cinfo (cinfo, method->klass->image);
		mono_custom_attrs_free (cinfo);
	}

	if (with_class_level && level == MONO_SECURITY_CORE_CLR_TRANSPARENT)
		level = mono_security_core_clr_class_level (method->klass);

	return level;
}

gboolean
mono_security_core_clr_is_platform_image (MonoImage *image)
{
	const char *prefix = mono_assembly_getrootdir ();
	int prefix_len = strlen (prefix);
	static const char subprefix[] = "/mono/2.1/";
	int subprefix_len = strlen (subprefix);

	if (!image->name)
		return FALSE;
	if (strncmp (prefix, image->name, prefix_len) != 0)
		return FALSE;
	if (strncmp (subprefix, image->name + prefix_len, subprefix_len) != 0)
		return FALSE;
	if (strchr (image->name + prefix_len + subprefix_len, '/'))
		return FALSE;
	return TRUE;
}

void
mono_security_enable_core_clr ()
{
	mono_verifier_set_mode (MONO_VERIFIER_MODE_VERIFIABLE);
	mono_security_set_mode (MONO_SECURITY_MODE_CORE_CLR);
}
