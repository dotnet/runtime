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
#include <mono/metadata/verify-internals.h>
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

/* Note: The above functions are outside this guard so that the public API isn't affected. */

#ifndef DISABLE_SECURITY

/* Class lazy loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (security_critical, "System.Security", "SecurityCriticalAttribute")
static GENERATE_GET_CLASS_WITH_CACHE (security_safe_critical, "System.Security", "SecuritySafeCriticalAttribute")

static MonoClass*
security_critical_attribute (void)
{
	return mono_class_get_security_critical_class ();
}

static MonoClass*
security_safe_critical_attribute (void)
{
	return mono_class_get_security_safe_critical_class ();

}

/* sometime we get a NULL (not found) caller (e.g. get_reflection_caller) */
static char*
get_method_full_name (MonoMethod * method)
{
	return method ? mono_method_full_name (method, TRUE) : g_strdup ("'no caller found'");
}

/*
 * set_type_load_exception_type
 *
 *	Set MONO_EXCEPTION_TYPE_LOAD on the specified 'class' and provide
 *	a descriptive message for the exception. This message is also, 
 *	optionally, being logged (export MONO_LOG_MASK="security") for
 *	debugging purposes.
 */
static void
set_type_load_exception_type (const char *format, MonoClass *klass)
{
	char *type_name = mono_type_get_full_name (klass);
	char *parent_name = mono_type_get_full_name (m_class_get_parent (klass));
	char *message = mono_image_strdup_printf (m_class_get_image (klass), format, type_name, parent_name);

	g_free (parent_name);
	g_free (type_name);
	
	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_SECURITY, "%s", message);
	mono_class_set_type_load_failure (klass, "%s", message);
	// note: do not free string given to mono_class_set_failure
}

/*
 * set_type_load_exception_methods
 *
 *	Set MONO_EXCEPTION_TYPE_LOAD on the 'override' class and provide
 *	a descriptive message for the exception. This message is also, 
 *	optionally, being logged (export MONO_LOG_MASK="security") for
 *	debugging purposes.
 */
static void
set_type_load_exception_methods (const char *format, MonoMethod *override, MonoMethod *base)
{
	char *method_name = get_method_full_name (override);
	char *base_name = get_method_full_name (base);
	char *message = mono_image_strdup_printf (m_class_get_image (override->klass), format, method_name, base_name);

	g_free (base_name);
	g_free (method_name);

	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_SECURITY, "%s", message);
	mono_class_set_type_load_failure (override->klass, "%s", message);
	// note: do not free string given to mono_class_set_failure
}

/* MonoClass is not fully initialized (inited is not yet == 1) when we 
 * check the inheritance rules so we need to look for the default ctor
 * ourselve to avoid recursion (and aborting)
 */
static MonoMethod*
get_default_ctor (MonoClass *klass)
{
	int i;

	mono_class_setup_methods (klass);
	if (!m_class_get_methods (klass))
		return NULL;

	int mcount = mono_class_get_method_count (klass);
	MonoMethod **klass_methods = m_class_get_methods (klass);
	for (i = 0; i < mcount; ++i) {
		MonoMethodSignature *sig;
		MonoMethod *method = klass_methods [i];

		if (!method)
			continue;

		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) == 0)
			continue;
		if ((method->name[0] != '.') || strcmp (".ctor", method->name))
			continue;
		sig = mono_method_signature_internal (method);
		if (sig && (sig->param_count == 0))
			return method;
	}

	return NULL;
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
 *
 *	Furthermore a class MUST have a default constructor if its base 
 *	class has a non-transparent, public or protected, default constructor. 
 *	The same inheritance rule applies to both default constructors.
 *
 *	Reference: message from a SecurityException in SL4RC
 *	Reference: fxcop CA2132 rule
 */
void
mono_security_core_clr_check_inheritance (MonoClass *klass)
{
	MonoSecurityCoreCLRLevel class_level, parent_level;
	MonoClass *parent = m_class_get_parent (klass);

	if (!parent)
		return;

	class_level = mono_security_core_clr_class_level (klass);
	parent_level = mono_security_core_clr_class_level (parent);

	if (class_level < parent_level) {
		set_type_load_exception_type (
			"Inheritance failure for type %s. Parent class %s is more restricted.",
			klass);
	} else {
		MonoMethod *parent_ctor = get_default_ctor (parent);
		if (parent_ctor && ((parent_ctor->flags & METHOD_ATTRIBUTE_PUBLIC) != 0)) {
			class_level = mono_security_core_clr_method_level (get_default_ctor (klass), FALSE);
			parent_level = mono_security_core_clr_method_level (parent_ctor, FALSE);
			if (class_level < parent_level) {
				set_type_load_exception_type (
					"Inheritance failure for type %s. Default constructor security mismatch with %s.",
					klass);
			}
		}
	}
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
mono_security_core_clr_check_override (MonoClass *klass, MonoMethod *override, MonoMethod *base)
{
	MonoSecurityCoreCLRLevel base_level = mono_security_core_clr_method_level (base, FALSE);
	MonoSecurityCoreCLRLevel override_level = mono_security_core_clr_method_level (override, FALSE);
	/* if the base method is decorated with [SecurityCritical] then the overrided method MUST be too */
	if (base_level == MONO_SECURITY_CORE_CLR_CRITICAL) {
		if (override_level != MONO_SECURITY_CORE_CLR_CRITICAL) {
			set_type_load_exception_methods (
				"Override failure for %s over %s. Override MUST be [SecurityCritical].",
				override, base);
		}
	} else {
		/* base is [SecuritySafeCritical] or [SecurityTransparent], override MUST NOT be [SecurityCritical] */
		if (override_level == MONO_SECURITY_CORE_CLR_CRITICAL) {
			set_type_load_exception_methods (
				"Override failure for %s over %s. Override must NOT be [SecurityCritical].", 
				override, base);
		}
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
	MonoMethod **dest = (MonoMethod **)data;
	const char *ns;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (m->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	/* quick out (any namespace not starting with an 'S' */
	ns = m_class_get_name_space (m->klass);
	if (!ns || (*ns != 'S')) {
		*dest = m;
		return TRUE;
	}

	/* stop if the method is not part of platform code */
	if (!mono_security_core_clr_is_platform_image (m_class_get_image (m->klass))) {
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
		const char *kname = m_class_get_name (m->klass);
		if ((*kname == 'A') && (strcmp (kname, "Activator") == 0))
			return FALSE;

		/* unlike most Invoke* cases InvokeMember is not inside System.Reflection[.Emit] but is SecuritySafeCritical */
		if (((*kname == 'T') && (strcmp (kname, "Type") == 0)) || 
			((*kname == 'R') && (strcmp (kname, "RuntimeType")) == 0)) {

			/* if calling InvokeMember then we can't stop the stackwalk here and need to look at the caller */
			if (strcmp (m->name, "InvokeMember") == 0)
				return FALSE;
		}

		/* the security check on the delegate is made at creation time, not at invoke time */
		if (((*kname == 'D') && (strcmp (kname, "Delegate") == 0)) || 
			((*kname == 'M') && (strcmp (kname, "MulticastDelegate")) == 0)) {

			/* if we're invoking then we can stop our stack walk */
			if (strcmp (m->name, "DynamicInvoke") != 0)
				return FALSE;
		}
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
 *	- System.[Multicast]Delegate or Activator type
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
	if (G_UNLIKELY (!m)) {
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_SECURITY, "No caller outside reflection was found");
	}
	return m;
}

typedef struct {
	int depth;
	MonoMethod *caller;
} ElevatedTrustCookie;

/*
 * get_caller_of_elevated_trust_code
 *
 *	Stack walk to find who is calling code requiring Elevated Trust.
 *	If a critical method is found then the caller is platform code
 *	and has elevated trust, otherwise (transparent) a check needs to
 *	be done (on the managed side) to determine if the application is
 *	running with elevated permissions.
 */
static gboolean
get_caller_of_elevated_trust_code (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	ElevatedTrustCookie *cookie = (ElevatedTrustCookie *)data;

	/* skip unmanaged frames and wrappers */
	if (!managed || (m->wrapper_type != MONO_WRAPPER_NONE))
		return FALSE;

	/* end stack walk if we find ourselves outside platform code (we won't find critical code anymore) */
	if (!mono_security_core_clr_is_platform_image (m_class_get_image (m->klass))) {
		cookie->caller = m;
		return TRUE;
	}

	switch (cookie->depth) {
	/* while depth == 0 look for SecurityManager::[Check|Ensure]ElevatedPermissions */
	case 0:
		if (strcmp (m_class_get_name_space (m->klass), "System.Security"))
			return FALSE;
		if (strcmp (m_class_get_name (m->klass), "SecurityManager"))
			return FALSE;
		if ((strcmp (m->name, "EnsureElevatedPermissions")) && strcmp (m->name, "CheckElevatedPermissions"))
			return FALSE;
		cookie->depth = 1;
		break;
	/* while depth == 1 look for the caller to SecurityManager::[Check|Ensure]ElevatedPermissions */
	case 1:
		/* this frame is [SecuritySafeCritical] because it calls [SecurityCritical] [Check|Ensure]ElevatedPermissions */
		/* the next frame will contain the caller(s) we want to check */
		cookie->depth = 2;
		break;
	/* while depth >= 2 look for [safe]critical caller, end stack walk if we find it  */
	default:
		cookie->depth++;
		/* if the caller is transparent then we continue the stack walk */
		if (mono_security_core_clr_method_level (m, TRUE) == MONO_SECURITY_CORE_CLR_TRANSPARENT)
			break;

		/* Security[Safe]Critical code is always allowed to call elevated-trust code */
		cookie->caller = m;
		return TRUE;
	}

	return FALSE;
}

/*
 * mono_security_core_clr_require_elevated_permissions:
 *
 *	Return TRUE if the caller of the current method (the code who 
 *	called SecurityManager.get_RequiresElevatedPermissions) needs
 *	elevated trust to perform an action.
 *
 *	A stack walk is done to find the callers. If one of the callers
 *	is either [SecurityCritical] or [SecuritySafeCritical] then the
 *	action is needed for platform code (i.e. no restriction). 
 *	Otherwise (transparent) the requested action needs elevated trust
 */
gboolean
mono_security_core_clr_require_elevated_permissions (void)
{
	ElevatedTrustCookie cookie;
	cookie.depth = 0;
	cookie.caller = NULL;
	mono_stack_walk_no_il (get_caller_of_elevated_trust_code, &cookie);

	/* return TRUE if the stack walk did not reach far enough or did not find callers */
	if (!cookie.caller || cookie.depth < 3)
		return TRUE;

	/* return TRUE if the caller is transparent, i.e. if elevated trust is required to continue executing the method */
	return (mono_security_core_clr_method_level (cookie.caller, TRUE) == MONO_SECURITY_CORE_CLR_TRANSPARENT);
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
		ERROR_DECL (error);
		MonoClass *klass;

		/* this check can occur before the field's type is resolved (and that can fail) */
		mono_field_get_type_checked (field, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error);
			return FALSE;
		}

		klass = (mono_field_get_flags (field) & FIELD_ATTRIBUTE_STATIC) ? NULL : mono_field_get_parent (field);
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
 * get_argument_exception
 *
 *	Helper function to create an MonoException (ArgumentException in
 *	managed-land) and provide a descriptive message for it. This 
 *	message is also, optionally, being logged (export 
 *	MONO_LOG_MASK="security") for debugging purposes.
 */
static MonoException*
get_argument_exception (const char *format, MonoMethod *caller, MonoMethod *callee)
{
	MonoException *ex;
	char *caller_name = get_method_full_name (caller);
	char *callee_name = get_method_full_name (callee);
	char *message = g_strdup_printf (format, caller_name, callee_name);
	g_free (callee_name);
	g_free (caller_name);

	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_SECURITY, "%s", message);
	ex = mono_get_exception_argument ("method", message);
	g_free (message);

	return ex;
}

/*
 * get_field_access_exception
 *
 *	Helper function to create an MonoException (FieldAccessException
 *	in managed-land) and provide a descriptive message for it. This
 *	message is also, optionally, being logged (export 
 *	MONO_LOG_MASK="security") for debugging purposes.
 */
static MonoException*
get_field_access_exception (const char *format, MonoMethod *caller, MonoClassField *field)
{
	MonoException *ex;
	char *caller_name = get_method_full_name (caller);
	char *field_name = mono_field_full_name (field);
	char *message = g_strdup_printf (format, caller_name, field_name);
	g_free (field_name);
	g_free (caller_name);

	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_SECURITY, "%s", message);
	ex = mono_get_exception_field_access_msg (message);
	g_free (message);

	return ex;
}

/*
 * get_method_access_exception
 *
 *	Helper function to create an MonoException (MethodAccessException
 *	in managed-land) and provide a descriptive message for it. This
 *	message is also, optionally, being logged (export 
 *	MONO_LOG_MASK="security") for debugging purposes.
 */
static MonoException*
get_method_access_exception (const char *format, MonoMethod *caller, MonoMethod *callee)
{
	MonoException *ex;
	char *caller_name = get_method_full_name (caller);
	char *callee_name = get_method_full_name (callee);
	char *message = g_strdup_printf (format, caller_name, callee_name);
	g_free (callee_name);
	g_free (caller_name);

	mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_SECURITY, "%s", message);
	ex = mono_get_exception_method_access_msg (message);
	g_free (message);

	return ex;
}

/*
 * mono_security_core_clr_ensure_reflection_access_field:
 *
 *	Ensure that the specified field can be used with reflection since 
 *	Transparent code cannot access to Critical fields and can only use
 *	them if they are visible from it's point of view.
 *
 *	Returns TRUE if acess is allowed.  Otherwise returns FALSE and sets @error to a FieldAccessException if the field is cannot be accessed.
 */
gboolean
mono_security_core_clr_ensure_reflection_access_field (MonoClassField *field, MonoError *error)
{
	error_init (error);
	MonoMethod *caller = get_reflection_caller ();
	/* CoreCLR restrictions applies to Transparent code/caller */
	if (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT)
		return TRUE;

	if (mono_security_core_clr_get_options () & MONO_SECURITY_CORE_CLR_OPTIONS_RELAX_REFLECTION) {
		if (!mono_security_core_clr_is_platform_image (m_class_get_image (mono_field_get_parent(field))))
			return TRUE;
	}

	/* Transparent code cannot [get|set]value on Critical fields */
	if (mono_security_core_clr_class_level (mono_field_get_parent (field)) == MONO_SECURITY_CORE_CLR_CRITICAL) {
		mono_error_set_exception_instance (error, get_field_access_exception (
			"Transparent method %s cannot get or set Critical field %s.", 
			caller, field));
		return FALSE;
	}

	/* also it cannot access a fields that is not visible from it's (caller) point of view */
	if (!check_field_access (caller, field)) {
		mono_error_set_exception_instance (error, get_field_access_exception (
			"Transparent method %s cannot get or set private/internal field %s.", 
			caller, field));
		return FALSE;
	}
	return TRUE;
}

/*
 * mono_security_core_clr_ensure_reflection_access_method:
 *
 *	Ensure that the specified method can be used with reflection since
 *	Transparent code cannot call Critical methods and can only call them
 *	if they are visible from it's point of view.
 *
 *	If access is allowed returns TRUE.  Returns FALSE and sets @error to a MethodAccessException if the field is cannot be accessed.
 */
gboolean
mono_security_core_clr_ensure_reflection_access_method (MonoMethod *method, MonoError *error)
{
	error_init (error);
	MonoMethod *caller = get_reflection_caller ();
	/* CoreCLR restrictions applies to Transparent code/caller */
	if (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT)
		return TRUE;

	if (mono_security_core_clr_get_options () & MONO_SECURITY_CORE_CLR_OPTIONS_RELAX_REFLECTION) {
		if (!mono_security_core_clr_is_platform_image (m_class_get_image (method->klass)))
			return TRUE;
	}

	/* Transparent code cannot invoke, even using reflection, Critical code */
	if (mono_security_core_clr_method_level (method, TRUE) == MONO_SECURITY_CORE_CLR_CRITICAL) {
		mono_error_set_exception_instance (error, get_method_access_exception (
			"Transparent method %s cannot invoke Critical method %s.", 
			caller, method));
		return FALSE;
	}

	/* also it cannot invoke a method that is not visible from it's (caller) point of view */
	if (!check_method_access (caller, method)) {
		mono_error_set_exception_instance (error, get_method_access_exception (
			"Transparent method %s cannot invoke private/internal method %s.", 
			caller, method));
		return FALSE;
	}
	return TRUE;
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
	if (!mono_security_core_clr_is_platform_image (m_class_get_image (method->klass)))
		return FALSE;

	if (strcmp (m_class_get_name_space (method->klass), "System.Reflection") != 0)
		return FALSE;

	if (strcmp (m_class_get_name (method->klass), "RuntimePropertyInfo") == 0) {
		if ((strcmp (method->name, "GetterAdapterFrame") == 0) || strcmp (method->name, "StaticGetterAdapterFrame") == 0)
			return TRUE;
	} else if (strcmp (m_class_get_name (method->klass), "RuntimeEventInfo") == 0) {
		if ((strcmp (method->name, "AddEventFrame") == 0) || strcmp (method->name, "StaticAddEventAdapterFrame") == 0)
			return TRUE;
	}

	return FALSE;
}

/*
 * mono_security_core_clr_ensure_delegate_creation:
 *
 *	Return TRUE if a delegate can be created on the specified
 *	method.  CoreCLR can also affect the binding, this function may
 *	return (FALSE) and set @error to an ArgumentException.
 *
 *	@error is set to a MethodAccessException if the specified method is not
 *	visible from the caller point of view.
 */
gboolean
mono_security_core_clr_ensure_delegate_creation (MonoMethod *method, MonoError *error)
{
	MonoMethod *caller;

	error_init (error);

	/* note: mscorlib creates delegates to avoid reflection (optimization), we ignore those cases */
	if (can_avoid_corlib_reflection_delegate_optimization (method))
		return TRUE;

	caller = get_reflection_caller ();
	/* if the "real" caller is not Transparent then it do can anything */
	if (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT)
		return TRUE;

	/* otherwise it (as a Transparent caller) cannot create a delegate on a Critical method... */
	if (mono_security_core_clr_method_level (method, TRUE) == MONO_SECURITY_CORE_CLR_CRITICAL) {
		mono_error_set_exception_instance (error, get_argument_exception (
			"Transparent method %s cannot create a delegate on Critical method %s.", 
			caller, method));
		return FALSE;
	}

	if (mono_security_core_clr_get_options () & MONO_SECURITY_CORE_CLR_OPTIONS_RELAX_DELEGATE) {
		if (!mono_security_core_clr_is_platform_image (m_class_get_image (method->klass)))
			return TRUE;
	}

	/* also it cannot create the delegate on a method that is not visible from it's (caller) point of view */
	if (!check_method_access (caller, method)) {
		mono_error_set_exception_instance (error, get_method_access_exception (
			"Transparent method %s cannot create a delegate on private/internal method %s.", 
			caller, method));
		return FALSE;
	}

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
		if (mono_security_core_clr_is_platform_image (m_class_get_image (klass))) {
			MonoMethod *caller = get_reflection_caller ();
			/* XXX Critical code probably can do this / need some test cases (safer off otherwise) XXX */
			if (!check_field_access (caller, field)) {
				return get_field_access_exception (
					"Dynamic method %s cannot create access private/internal field %s.", 
					caller, field);
			}
		}
	} else if (handle_class == mono_defaults.methodhandle_class) {
		MonoMethod *method = (MonoMethod*) ref;
		/* methods coming from platform code have extra protection (accessibility check) */
		if (mono_security_core_clr_is_platform_image (m_class_get_image (method->klass))) {
			MonoMethod *caller = get_reflection_caller ();
			/* XXX Critical code probably can do this / need some test cases (safer off otherwise) XXX */
			if (!check_method_access (caller, method)) {
				return get_method_access_exception (
					"Dynamic method %s cannot create access private/internal method %s.", 
					caller, method);
			}
		}
	}
	return NULL;
}

/*
 * mono_security_core_clr_can_access_internals
 *
 *	Check if we allow [InternalsVisibleTo] to work between two images.
 */
gboolean
mono_security_core_clr_can_access_internals (MonoImage *accessing, MonoImage* accessed)
{
	/* are we trying to access internals of a platform assembly ? if not this is acceptable */
	if (!mono_security_core_clr_is_platform_image (accessed))
		return TRUE;

	/* we can't let everyone with the right name and public key token access the internals of platform code.
	 * (Silverlight can rely on the strongname signature of the assemblies, but Mono does not verify them)
	 * However platform code is fully trusted so it can access the internals of other platform code assemblies */
	if (mono_security_core_clr_is_platform_image (accessing))
		return TRUE;

	/* catch-22: System.Xml needs access to mscorlib's internals (e.g. ArrayList) but is not considered platform code.
	 * Promoting it to platform code would create another issue since (both Mono/Moonlight or MS version of) 
	 * System.Xml.Linq.dll (an SDK, not platform, assembly) needs access to System.Xml.dll internals (either ). 
	 * The solution is to trust, even transparent code, in the plugin directory to access platform code internals */
	if (!accessed->assembly->basedir || !accessing->assembly->basedir)
		return FALSE;
	return (strcmp (accessed->assembly->basedir, accessing->assembly->basedir) == 0);
}

/*
 * mono_security_core_clr_is_field_access_allowed
 *
 *	Return a MonoException (FieldccessException in managed-land) if
 *	the access from "caller" to "field" is not valid under CoreCLR -
 *	i.e. a [SecurityTransparent] method calling a [SecurityCritical]
 *	field.
 */
MonoException*
mono_security_core_clr_is_field_access_allowed (MonoMethod *caller, MonoClassField *field)
{
	/* there's no restriction to access Transparent or SafeCritical fields, so we only check calls to Critical methods */
	if (mono_security_core_clr_class_level (mono_field_get_parent (field)) != MONO_SECURITY_CORE_CLR_CRITICAL)
		return NULL;

	/* caller is Critical! only SafeCritical and Critical callers can access the field, so we throw if caller is Transparent */
	if (!caller || (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT))
		return NULL;

	return get_field_access_exception (
		"Transparent method %s cannot call use Critical field %s.", 
		caller, field);
}

/*
 * mono_security_core_clr_is_call_allowed
 *
 *	Return a MonoException (MethodAccessException in managed-land) if
 *	the call from "caller" to "callee" is not valid under CoreCLR -
 *	i.e. a [SecurityTransparent] method calling a [SecurityCritical]
 *	method.
 */
MonoException*
mono_security_core_clr_is_call_allowed (MonoMethod *caller, MonoMethod *callee)
{
	/* there's no restriction to call Transparent or SafeCritical code, so we only check calls to Critical methods */
	if (mono_security_core_clr_method_level (callee, TRUE) != MONO_SECURITY_CORE_CLR_CRITICAL)
		return NULL;

	/* callee is Critical! only SafeCritical and Critical callers can call it, so we throw if the caller is Transparent */
	if (!caller || (mono_security_core_clr_method_level (caller, TRUE) != MONO_SECURITY_CORE_CLR_TRANSPARENT))
		return NULL;

	return get_method_access_exception (
		"Transparent method %s cannot call Critical method %s.", 
		caller, callee);
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

	return (MonoSecurityCoreCLRLevel)level;
}

/*
 * mono_security_core_clr_class_level_no_platform_check:
 *
 *	Return the MonoSecurityCoreCLRLevel for the specified class, without 
 *	checking for platform code. This help us avoid multiple redundant 
 *	checks, e.g.
 *	- a check for the method and one for the class;
 *	- a check for the class and outer class(es) ...
 */
static MonoSecurityCoreCLRLevel
mono_security_core_clr_class_level_no_platform_check (MonoClass *klass)
{
	ERROR_DECL (error);
	MonoSecurityCoreCLRLevel level = MONO_SECURITY_CORE_CLR_TRANSPARENT;
	MonoCustomAttrInfo *cinfo = mono_custom_attrs_from_class_checked (klass, error);
	mono_error_cleanup (error);
	if (cinfo) {
		level = mono_security_core_clr_level_from_cinfo (cinfo, m_class_get_image (klass));
		mono_custom_attrs_free (cinfo);
	}

	if (level == MONO_SECURITY_CORE_CLR_TRANSPARENT && m_class_get_nested_in (klass))
		level = mono_security_core_clr_class_level_no_platform_check (m_class_get_nested_in (klass));

	return level;
}

/*
 * mono_security_core_clr_class_level:
 *
 *	Return the MonoSecurityCoreCLRLevel for the specified class.
 */
MonoSecurityCoreCLRLevel
mono_security_core_clr_class_level (MonoClass *klass)
{
	/* non-platform code is always Transparent - whatever the attributes says */
	if (!mono_security_core_clr_test && !mono_security_core_clr_is_platform_image (m_class_get_image (klass)))
		return MONO_SECURITY_CORE_CLR_TRANSPARENT;

	return mono_security_core_clr_class_level_no_platform_check (klass);
}

/*
 * mono_security_core_clr_field_level:
 *
 *	Return the MonoSecurityCoreCLRLevel for the specified field.
 *	If with_class_level is TRUE then the type (class) will also be
 *	checked, otherwise this will only report the information about
 *	the field itself.
 */
MonoSecurityCoreCLRLevel
mono_security_core_clr_field_level (MonoClassField *field, gboolean with_class_level)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo;
	MonoSecurityCoreCLRLevel level = MONO_SECURITY_CORE_CLR_TRANSPARENT;

	/* if get_reflection_caller returns NULL then we assume the caller has NO privilege */
	if (!field)
		return level;

	/* non-platform code is always Transparent - whatever the attributes says */
	if (!mono_security_core_clr_test && !mono_security_core_clr_is_platform_image (m_class_get_image (field->parent)))
		return level;

	cinfo = mono_custom_attrs_from_field_checked (field->parent, field, error);
	mono_error_cleanup (error);
	if (cinfo) {
		level = mono_security_core_clr_level_from_cinfo (cinfo, m_class_get_image (field->parent));
		mono_custom_attrs_free (cinfo);
	}

	if (with_class_level && level == MONO_SECURITY_CORE_CLR_TRANSPARENT)
		level = mono_security_core_clr_class_level (field->parent);

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
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo;
	MonoSecurityCoreCLRLevel level = MONO_SECURITY_CORE_CLR_TRANSPARENT;

	/* if get_reflection_caller returns NULL then we assume the caller has NO privilege */
	if (!method)
		return level;

	/* non-platform code is always Transparent - whatever the attributes says */
	if (!mono_security_core_clr_test && !mono_security_core_clr_is_platform_image (m_class_get_image (method->klass)))
		return level;

	cinfo = mono_custom_attrs_from_method_checked (method, error);
	mono_error_cleanup (error);
	if (cinfo) {
		level = mono_security_core_clr_level_from_cinfo (cinfo, m_class_get_image (method->klass));
		mono_custom_attrs_free (cinfo);
	}

	if (with_class_level && level == MONO_SECURITY_CORE_CLR_TRANSPARENT)
		level = mono_security_core_clr_class_level (method->klass);

	return level;
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

#else

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

#endif /* DISABLE_SECURITY */
