/*
 * security-manager.c:  Security Manager (Unmanaged side)
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 */

#include "security-manager.h"


/* Internal stuff */

static MonoSecurityManager secman;
static MonoBoolean mono_security_manager_activated = FALSE;
static MonoBoolean mono_security_manager_enabled = TRUE;
static MonoBoolean mono_security_manager_execution = TRUE;
static MonoSecurityMode mono_security_mode = MONO_SECURITY_MODE_NONE;


/* Public stuff */

void
mono_security_set_mode (MonoSecurityMode mode)
{
	mono_security_mode = mode;
}

MonoSecurityMode
mono_security_get_mode (void)
{
	return mono_security_mode;
}

MonoSecurityManager*
mono_security_manager_get_methods (void)
{
	/* Already initialized ? */
	if (secman.securitymanager)
		return &secman;

	/* Initialize */
	secman.securitymanager = mono_class_from_name (mono_defaults.corlib, 
		"System.Security", "SecurityManager");
	g_assert (secman.securitymanager);
	if (!secman.securitymanager->inited)
		mono_class_init (secman.securitymanager);
		
	secman.demand = mono_class_get_method_from_name (secman.securitymanager,
		"InternalDemand", 2);	
	g_assert (secman.demand);

	secman.demandchoice = mono_class_get_method_from_name (secman.securitymanager,
		"InternalDemandChoice", 2);	
	g_assert (secman.demandchoice);

	secman.demandunmanaged = mono_class_get_method_from_name (secman.securitymanager,
		"DemandUnmanaged", 0);
	g_assert (secman.demandunmanaged);

	secman.inheritancedemand = mono_class_get_method_from_name (secman.securitymanager,
		"InheritanceDemand", 3);	
	g_assert (secman.inheritancedemand);

	secman.inheritsecurityexception = mono_class_get_method_from_name (secman.securitymanager,
		"InheritanceDemandSecurityException", 4);	
	g_assert (secman.inheritsecurityexception);

	secman.linkdemand = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemand", 3);
	g_assert (secman.linkdemand);

	secman.linkdemandunmanaged = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemandUnmanaged", 1);
	g_assert (secman.linkdemandunmanaged);

	secman.linkdemandfulltrust = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemandFullTrust", 1);
	g_assert (secman.linkdemandfulltrust);

	secman.linkdemandsecurityexception = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemandSecurityException", 2);
	g_assert (secman.linkdemandsecurityexception);

	secman.allowpartiallytrustedcallers = mono_class_from_name (mono_defaults.corlib, "System.Security", 
		"AllowPartiallyTrustedCallersAttribute");
	g_assert (secman.allowpartiallytrustedcallers);

	secman.suppressunmanagedcodesecurity = mono_class_from_name (mono_defaults.corlib, "System.Security", 
		"SuppressUnmanagedCodeSecurityAttribute");
	g_assert (secman.suppressunmanagedcodesecurity);

	return &secman;
}

static gboolean
mono_secman_inheritance_check (MonoClass *klass, MonoDeclSecurityActions *demands)
{
	MonoSecurityManager* secman = mono_security_manager_get_methods ();
	MonoDomain *domain = mono_domain_get ();
	MonoAssembly *assembly = mono_image_get_assembly (klass->image);
	MonoReflectionAssembly *refass = mono_assembly_get_object (domain, assembly);
	MonoObject *res;
	gpointer args [3];

	args [0] = domain->domain;
	args [1] = refass;
	args [2] = demands;

	res = mono_runtime_invoke (secman->inheritancedemand, NULL, args, NULL);
	return (*(MonoBoolean *) mono_object_unbox (res));
}

void
mono_secman_inheritancedemand_class (MonoClass *klass, MonoClass *parent)
{
	MonoDeclSecurityActions demands;

	/* don't hide previous results -and- don't calc everything for nothing */
	if (klass->exception_type != 0)
		return;

	/* short-circuit corlib as it is fully trusted (within itself)
	 * and because this cause major recursion headaches */
	if ((klass->image == mono_defaults.corlib) && (parent->image == mono_defaults.corlib))
		return;

	/* Check if there are an InheritanceDemand on the parent class */
	if (mono_declsec_get_inheritdemands_class (parent, &demands)) {
		/* If so check the demands on the klass (inheritor) */
		if (!mono_secman_inheritance_check (klass, &demands)) {
			/* Keep flags in MonoClass to be able to throw a SecurityException later (if required) */
			mono_class_set_failure (klass, MONO_EXCEPTION_SECURITY_INHERITANCEDEMAND, NULL);
		}
	}
}

void
mono_secman_inheritancedemand_method (MonoMethod *override, MonoMethod *base)
{
	MonoDeclSecurityActions demands;

	/* don't hide previous results -and- don't calc everything for nothing */
	if (override->klass->exception_type != 0)
		return;

	/* short-circuit corlib as it is fully trusted (within itself)
	 * and because this cause major recursion headaches */
	if ((override->klass->image == mono_defaults.corlib) && (base->klass->image == mono_defaults.corlib))
		return;

	/* Check if there are an InheritanceDemand on the base (virtual) method */
	if (mono_declsec_get_inheritdemands_method (base, &demands)) {
		/* If so check the demands on the overriding method */
		if (!mono_secman_inheritance_check (override->klass, &demands)) {
			/* Keep flags in MonoClass to be able to throw a SecurityException later (if required) */
			mono_class_set_failure (override->klass, MONO_EXCEPTION_SECURITY_INHERITANCEDEMAND, base);
		}
	}
}


/*
 * Note: The security manager is activate once when executing the Mono. This 
 * is not meant to be a turn on/off runtime switch.
 */
void
mono_activate_security_manager (void)
{
	mono_security_manager_activated = TRUE;
}

gboolean
mono_is_security_manager_active (void)
{
	return mono_security_manager_activated;
}

/*
 * @publickey	An encoded (with header) public key
 * @size	The length of the public key
 *
 * returns TRUE if the public key is the ECMA "key", FALSE otherwise
 *
 * ECMA key isn't a real public key - it's simply an empty (but valid) header
 * so it's length (16) and value (00000000000000000400000000000000) are 
 * constants.
 */
gboolean 
mono_is_ecma_key (const char *publickey, int size)
{
	int i;
	if ((publickey == NULL) || (size != MONO_ECMA_KEY_LENGTH) || (publickey [8] != 0x04))
		return FALSE;

	for (i=0; i < size; i++) {
		if ((publickey [i] != 0x00) && (i != 8))
			return FALSE;
	}
	return TRUE;
}

/*
 * Context propagation is required when:
 * (a) the security manager is active (1.x and later)
 * (b) other contexts needs to be propagated (2.x and later)
 *
 * returns NULL if no context propagation is required, else the returns the
 * MonoMethod to call to Capture the ExecutionContext.
 */
MonoMethod*
mono_get_context_capture_method (void)
{
	static MonoMethod *method = NULL;

	if (!mono_security_manager_activated) {
		if (mono_image_get_assembly (mono_defaults.corlib)->aname.major < 2)
			return NULL;
	}

	/* older corlib revisions won't have the class (nor the method) */
	if (mono_defaults.executioncontext_class && !method) {
		mono_class_init (mono_defaults.executioncontext_class);
		method = mono_class_get_method_from_name (mono_defaults.executioncontext_class, "Capture", 0);
	}

	return method;
}


/* System.Security icalls */

MonoBoolean
ves_icall_System_Security_SecurityManager_get_SecurityEnabled (void)
{
	if (!mono_security_manager_activated)
		return FALSE;
	return mono_security_manager_enabled;
}

void
ves_icall_System_Security_SecurityManager_set_SecurityEnabled (MonoBoolean value)
{
	/* value can be changed only if the security manager is activated */
	if (mono_security_manager_activated) {
		mono_security_manager_enabled = value;
	}
}

MonoBoolean
ves_icall_System_Security_SecurityManager_get_CheckExecutionRights (void)
{
	if (!mono_security_manager_activated)
		return FALSE;
	return mono_security_manager_execution;
}

void
ves_icall_System_Security_SecurityManager_set_CheckExecutionRights (MonoBoolean value)
{
	/* value can be changed only id the security manager is activated */
	if (mono_security_manager_activated) {
		mono_security_manager_execution = value;
	}
}

MonoBoolean
ves_icall_System_Security_SecurityManager_GetLinkDemandSecurity (MonoReflectionMethod *m, MonoDeclSecurityActions *kactions, MonoDeclSecurityActions *mactions)
{
	MonoMethod *method = m->method;
	/* we want the original as the wrapper is "free" of the security informations */
	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE || method->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
		method = mono_marshal_method_from_wrapper (method);
	}

	mono_class_init (method->klass);

	/* if either the method or it's class has security (any type) */
	if ((method->flags & METHOD_ATTRIBUTE_HAS_SECURITY) || (method->klass->flags & TYPE_ATTRIBUTE_HAS_SECURITY)) {
		memset (kactions, 0, sizeof (MonoDeclSecurityActions));
		memset (mactions, 0, sizeof (MonoDeclSecurityActions));

		/* get any linkdemand (either on the method or it's class) */
		return mono_declsec_get_linkdemands (method, kactions, mactions);
	}
	return FALSE;
}
