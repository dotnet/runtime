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
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>

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

static gboolean
get_caller_no_reflection_related (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = data;
	const char *ns;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (m->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	/* quick out (any namespace not starting with an 'S' */
	ns = m->klass->name_space;
	if (!ns || (*ns != 'S')) {
		*dest = m;
		return TRUE;
	}

	/* stop if the method is not part of platform code */
	if (!mono_security_core_clr_is_platform_image (m->klass->image)) {
		*dest = m;
		return TRUE;
	}

	/* any number of calls inside System.Reflection are allowed */
	if (strcmp (ns, "System.Reflection") == 0)
		return FALSE;

	/* any number of calls inside System.Reflection are allowed */
	if (strcmp (ns, "System.Reflection.Emit") == 0)
		return FALSE;

	/* calls from System.Delegate are also possible and allowed */
	if (strcmp (ns, "System") == 0) {
		const char *kname = m->klass->name;
		if ((*kname == 'D') && (strcmp (kname, "Delegate") == 0))
			return FALSE;
		if ((*kname == 'M') && (strcmp (kname, "MulticastDelegate")) == 0)
			return FALSE;
		if ((*kname == 'A') && (strcmp (kname, "Activator") == 0))
			return FALSE;
	}

	if (m == *dest) {
		*dest = NULL;
		return FALSE;
	}

	*dest = m;
	return TRUE;
}

/* walk to the first managed method outside:
 *  - System.Reflection* namespaces
 *  - System.[MulticastDelegate]Delegate or Activator type
 *  - platform code
 *  and return its value, since CoreCLR checks needs to be done on this "real" caller.
 */
static MonoMethod*
get_reflection_caller (void)
{
	MonoMethod *m = mono_method_get_last_managed ();
	mono_stack_walk_no_il (get_caller_no_reflection_related, &m);
	return m;
}

void
mono_security_core_clr_ensure_reflection_access_field (MonoClassField *field)
{
	MonoClass *klass = mono_field_get_parent (field);

	/* under CoreCLR you cannot use the value (get/set) of the reflected field: */
	MonoMethod *caller = get_reflection_caller ();

	/* (a) of a Critical type when called from a Transparent caller */
	if (mono_security_core_clr_class_level (klass) == MONO_SECURITY_CORE_CLR_CRITICAL) {
		if (mono_security_core_clr_method_level (caller, TRUE) == MONO_SECURITY_CORE_CLR_TRANSPARENT)
			mono_raise_exception (mono_get_exception_field_access ());
	}
	/* (b) that are not accessible from the caller pov */
	if (!mono_method_can_access_field_full (caller, field, klass))
		mono_raise_exception (mono_get_exception_field_access ());
}

void
mono_security_core_clr_ensure_reflection_access_method (MonoMethod *method)
{
	MonoMethod *caller = get_reflection_caller ();
	/* CoreCLR restrictions applies to Transparent code/caller */
	if (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT)
		return;

	/* Transparent code cannot invoke, even using reflection, Critical code */
	if (mono_security_core_clr_method_level (method, TRUE) == MONO_SECURITY_CORE_CLR_CRITICAL)
		mono_raise_exception (mono_get_exception_method_access ());

	/* also it cannot invoke a method that is not visible from it's (caller) point of view */
	if (!mono_method_can_access_method_full (caller, method, (method->flags & METHOD_ATTRIBUTE_STATIC) ? NULL : method->klass))
		mono_raise_exception (mono_get_exception_method_access ());
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
