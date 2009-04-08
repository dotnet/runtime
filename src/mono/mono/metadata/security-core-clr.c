/*
 * security-core-clr.c: CoreCLR security
 *
 * Authors:
 *	Mark Probst <mark.probst@gmail.com>
 *	Sebastien Pouliot  <sebastien@ximian.com>
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

/*
 * mono_security_core_clr_check_inheritance:
 *
 *	Determine if the specified class can inherit from its parent using 
 * 	the CoreCLR inheritance rules.
 *
 *	Base Type	Allow Derived Type
 *	------------	------------------
 *	Transparent	Transparent, SafeCritical, Critical
 *	SafeCritical	SafeCritical, Critical
 *	Critical	Critical
 *
 *	Reference: http://msdn.microsoft.com/en-us/magazine/cc765416.aspx#id0190030
 */
void
mono_security_core_clr_check_inheritance (MonoClass *class)
{
	MonoSecurityCoreCLRLevel class_level, parent_level;
	MonoClass *parent = class->parent;

	if (!parent)
		return;

	class_level = mono_security_core_clr_class_level (class);
	parent_level = mono_security_core_clr_class_level (parent);

	if (class_level < parent_level)
		mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
}

/*
 * mono_security_core_clr_check_override:
 *
 *	Determine if the specified override can "legally" override the 
 *	specified base method using the CoreCLR inheritance rules.
 *
 *	Base (virtual/interface)	Allowed override
 *	------------------------	-------------------------
 *	Transparent			Transparent, SafeCritical
 *	SafeCritical			Transparent, SafeCritical
 *	Critical			Critical
 *
 *	Reference: http://msdn.microsoft.com/en-us/magazine/cc765416.aspx#id0190030
 */
void
mono_security_core_clr_check_override (MonoClass *class, MonoMethod *override, MonoMethod *base)
{
	MonoSecurityCoreCLRLevel base_level = mono_security_core_clr_method_level (base, FALSE);
	MonoSecurityCoreCLRLevel override_level = mono_security_core_clr_method_level (override, FALSE);
	/* if the base method is decorated with [SecurityCritical] then the overrided method MUST be too */
	if (base_level == MONO_SECURITY_CORE_CLR_CRITICAL) {
		if (override_level != MONO_SECURITY_CORE_CLR_CRITICAL)
			mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
	} else {
		/* base is [SecuritySafeCritical] or [SecurityTransparent], override MUST NOT be [SecurityCritical] */
		if (override_level == MONO_SECURITY_CORE_CLR_CRITICAL)
			mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
	}
}

/*
 * get_caller_no_reflection_related:
 *
 *	Find the first managed caller that is either:
 *	(a) located outside the platform code assemblies; or
 *	(b) not related to reflection and delegates
 *
 *	Returns TRUE to stop the stackwalk, FALSE to continue to the next frame.
 */
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

/*
 * get_reflection_caller:
 * 
 *	Walk to the first managed method outside:
 *	- System.Reflection* namespaces
 *	- System.[MulticastDelegate]Delegate or Activator type
 *	- platform code
 *	and return a pointer to its MonoMethod.
 *
 *	This is required since CoreCLR checks needs to be done on this "real" caller.
 */
static MonoMethod*
get_reflection_caller (void)
{
	MonoMethod *m = NULL;
	mono_stack_walk_no_il (get_caller_no_reflection_related, &m);
	if (!m)
		g_warning ("could not find a caller outside reflection");
	return m;
}

/*
 * check_field_access:
 *
 *	Return TRUE if the caller method can access the specified field, FALSE otherwise.
 */
static gboolean
check_field_access (MonoMethod *caller, MonoClassField *field)
{
	/* if get_reflection_caller returns NULL then we assume the caller has NO privilege */
	if (caller) {
		MonoClass *klass = (mono_field_get_flags (field) & FIELD_ATTRIBUTE_STATIC) ? NULL : mono_field_get_parent (field);
		return mono_method_can_access_field_full (caller, field, klass);
	}
	return FALSE;
}

/*
 * check_method_access:
 *
 *	Return TRUE if the caller method can access the specified callee method, FALSE otherwise.
 */
static gboolean
check_method_access (MonoMethod *caller, MonoMethod *callee)
{
	/* if get_reflection_caller returns NULL then we assume the caller has NO privilege */
	if (caller) {
		MonoClass *klass = (callee->flags & METHOD_ATTRIBUTE_STATIC) ? NULL : callee->klass;
		return mono_method_can_access_method_full (caller, callee, klass);
	}
	return FALSE;
}

/*
 * mono_security_core_clr_ensure_reflection_access_field:
 *
 *	Ensure that the specified field can be used with reflection since 
 *	Transparent code cannot access to Critical fields and can only use
 *	them if they are visible from it's point of view.
 *
 *	A FieldAccessException is thrown if the field is cannot be accessed.
 */
void
mono_security_core_clr_ensure_reflection_access_field (MonoClassField *field)
{
	MonoMethod *caller = get_reflection_caller ();
	/* CoreCLR restrictions applies to Transparent code/caller */
	if (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT)
		return;

	/* Transparent code cannot [get|set]value on Critical fields */
	if (mono_security_core_clr_class_level (mono_field_get_parent (field)) == MONO_SECURITY_CORE_CLR_CRITICAL)
		mono_raise_exception (mono_get_exception_field_access ());

	/* also it cannot access a fields that is not visible from it's (caller) point of view */
	if (!check_field_access (caller, field))
		mono_raise_exception (mono_get_exception_field_access ());
}

/*
 * mono_security_core_clr_ensure_reflection_access_method:
 *
 *	Ensure that the specified method can be used with reflection since
 *	Transparent code cannot call Critical methods and can only call them
 *	if they are visible from it's point of view.
 *
 *	A MethodAccessException is thrown if the field is cannot be accessed.
 */
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
	if (!check_method_access (caller, method))
		mono_raise_exception (mono_get_exception_method_access ());
}

/*
 * can_avoid_corlib_reflection_delegate_optimization:
 *
 *	Mono's mscorlib use delegates to optimize PropertyInfo and EventInfo
 *	reflection calls. This requires either a bunch of additional, and not
 *	really required, [SecuritySafeCritical] in the class libraries or 
 *	(like this) a way to skip them. As a bonus we also avoid the stack
 *	walk to find the caller.
 *
 *	Return TRUE if we can skip this "internal" delegate creation, FALSE
 *	otherwise.
 */
static gboolean
can_avoid_corlib_reflection_delegate_optimization (MonoMethod *method)
{
	if (!mono_security_core_clr_is_platform_image (method->klass->image))
		return FALSE;

	if (strcmp (method->klass->name_space, "System.Reflection") != 0)
		return FALSE;

	if (strcmp (method->klass->name, "MonoProperty") == 0) {
		if ((strcmp (method->name, "GetterAdapterFrame") == 0) || strcmp (method->name, "StaticGetterAdapterFrame") == 0)
			return TRUE;
	} else if (strcmp (method->klass->name, "EvenInfo") == 0) {
		if ((strcmp (method->name, "AddEventFrame") == 0) || strcmp (method->name, "StaticAddEventAdapterFrame") == 0)
			return TRUE;
	}

	return FALSE;
}

/*
 * mono_security_core_clr_ensure_delegate_creation:
 *
 *	Return TRUE if a delegate can be created on the specified method. 
 *	CoreCLR also affect the binding, so throwOnBindFailure must be 
 * 	FALSE to let this function return (FALSE) normally, otherwise (if
 *	throwOnBindFailure is TRUE) itwill throw an ArgumentException.
 *
 *	A MethodAccessException is thrown if the specified method is not
 *	visible from the caller point of view.
 */
gboolean
mono_security_core_clr_ensure_delegate_creation (MonoMethod *method, gboolean throwOnBindFailure)
{
	MonoMethod *caller;

	/* note: mscorlib creates delegates to avoid reflection (optimization), we ignore those cases */
	if (can_avoid_corlib_reflection_delegate_optimization (method))
		return TRUE;

	caller = get_reflection_caller ();
	/* if the "real" caller is not Transparent then it do can anything */
	if (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT)
		return TRUE;

	/* otherwise it (as a Transparent caller) cannot create a delegate on a Critical method... */
	if (mono_security_core_clr_method_level (method, TRUE) == MONO_SECURITY_CORE_CLR_CRITICAL) {
		/* but this throws only if 'throwOnBindFailure' is TRUE */
		if (!throwOnBindFailure)
			return FALSE;

		mono_raise_exception (mono_get_exception_argument ("method", "Transparent code cannot call Critical code"));
	}
	
	/* also it cannot create the delegate on a method that is not visible from it's (caller) point of view */
	if (!check_method_access (caller, method))
		mono_raise_exception (mono_get_exception_method_access ());

	return TRUE;
}

/*
 * mono_security_core_clr_ensure_dynamic_method_resolved_object:
 *
 *	Called from mono_reflection_create_dynamic_method (reflection.c) to add some extra checks required for CoreCLR.
 *	Dynamic methods needs to check to see if the objects being used (e.g. methods, fields) comes from platform code
 *	and do an accessibility check in this case. Otherwise (i.e. user/application code) can be used without this extra
 *	accessbility check.
 */
MonoException*
mono_security_core_clr_ensure_dynamic_method_resolved_object (gpointer ref, MonoClass *handle_class)
{
	/* XXX find/create test cases for other handle_class XXX */
	if (handle_class == mono_defaults.fieldhandle_class) {
		MonoClassField *field = (MonoClassField*) ref;
		MonoClass *klass = mono_field_get_parent (field);
		/* fields coming from platform code have extra protection (accessibility check) */
		if (mono_security_core_clr_is_platform_image (klass->image)) {
			MonoMethod *caller = get_reflection_caller ();
			/* XXX Critical code probably can do this / need some test cases (safer off otherwise) XXX */
			if (!check_field_access (caller, field))
				return mono_get_exception_field_access ();
		}
	} else if (handle_class == mono_defaults.methodhandle_class) {
		MonoMethod *method = (MonoMethod*) ref;
		/* methods coming from platform code have extra protection (accessibility check) */
		if (mono_security_core_clr_is_platform_image (method->klass->image)) {
			MonoMethod *caller = get_reflection_caller ();
			/* XXX Critical code probably can do this / need some test cases (safer off otherwise) XXX */
			if (!check_method_access (caller, method))
				return mono_get_exception_method_access ();
		}
	}
	return NULL;
}

/*
 * mono_security_core_clr_level_from_cinfo:
 *
 *	Return the MonoSecurityCoreCLRLevel that match the attribute located
 *	in the specified custom attributes. If no attribute is present it 
 *	defaults to MONO_SECURITY_CORE_CLR_TRANSPARENT, which is the default
 *	level for all code under the CoreCLR.
 */
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

/*
 * mono_security_core_clr_class_level:
 *
 *	Return the MonoSecurityCoreCLRLevel for the specified class.
 */
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

/*
 * mono_security_core_clr_method_level:
 *
 *	Return the MonoSecurityCoreCLRLevel for the specified method.
 *	If with_class_level is TRUE then the type (class) will also be
 *	checked, otherwise this will only report the information about
 *	the method itself.
 */
MonoSecurityCoreCLRLevel
mono_security_core_clr_method_level (MonoMethod *method, gboolean with_class_level)
{
	MonoCustomAttrInfo *cinfo;
	MonoSecurityCoreCLRLevel level = MONO_SECURITY_CORE_CLR_TRANSPARENT;

	/* if get_reflection_caller returns NULL then we assume the caller has NO privilege */
	if (!method)
		return level;

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

/*
 * default_platform_check:
 *
 *	Default platform check. Always return FALSE.
 */
static gboolean
default_platform_check (const char *image_name)
{
	return FALSE;
}

static MonoCoreClrPlatformCB platform_callback = default_platform_check;

/*
 * mono_security_core_clr_determine_platform_image:
 *
 *	Call the supplied callback (from mono_security_set_core_clr_platform_callback) 
 *	to determine if this image represents platform code.
 */
gboolean
mono_security_core_clr_determine_platform_image (MonoImage *image)
{
	return platform_callback (image->name);
}

/*
 * mono_security_enable_core_clr:
 *
 *   Enable the verifier and the CoreCLR security model
 */
void
mono_security_enable_core_clr ()
{
	mono_verifier_set_mode (MONO_VERIFIER_MODE_VERIFIABLE);
	mono_security_set_mode (MONO_SECURITY_MODE_CORE_CLR);
}

/*
 * mono_security_set_core_clr_platform_callback:
 *
 *	Set the callback function that will be used to determine if an image
 *	is part, or not, of the platform code.
 */
void
mono_security_set_core_clr_platform_callback (MonoCoreClrPlatformCB callback)
{
	platform_callback = callback;
}

